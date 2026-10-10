[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string]$MakensisPath,

    [Parameter(Mandatory = $true)]
    [string]$SevenZipPath
)

$ErrorActionPreference = 'Stop'
$builder = Join-Path $PSScriptRoot 'build_commercial_installer.ps1'
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$separator = [System.IO.Path]::DirectorySeparatorChar
$testRoot = Join-Path $tempRoot ('ASMILE-Installer-Rollback-Test-' + [guid]::NewGuid().ToString('N'))
$outputRoot = Join-Path $testRoot 'output'
$crashOutputRoot = Join-Path $testRoot 'crash-output'
$recoveryOutputRoot = Join-Path $testRoot 'recovery-output'
$installRoot = Join-Path $testRoot 'installed'
$crashMarker = Join-Path $testRoot 'crash-ready.signal'
$testRegistryKey = 'Software\ASMILE-BO-Tests\' + [guid]::NewGuid().ToString('N')
$testMetadataRegistryKey = 'Software\ASMILE-BO-TestMetadata\' + [guid]::NewGuid().ToString('N')
$testPassed = $false

try {
    [void](New-Item -ItemType Directory -Path $outputRoot -Force)
    [void](New-Item -ItemType Directory -Path $installRoot -Force)
    $metadataPath = "HKCU:\$testMetadataRegistryKey"
    [void](New-Item -Path $metadataPath -Force)
    New-ItemProperty -LiteralPath $metadataPath -Name 'InstallLocation' -Value $installRoot -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $metadataPath -Name 'DisplayVersion' -Value '0.9.0.0' -PropertyType String -Force | Out-Null
    New-ItemProperty -LiteralPath $metadataPath -Name 'RollbackTestMarker' -Value 'preserve-me' -PropertyType String -Force | Out-Null
    $publishedExe = Get-Item -LiteralPath (Join-Path $PublishDirectory 'ASMILE.BO.App.exe')
    $version = $publishedExe.VersionInfo.FileVersion

    & $builder `
        -PublishDirectory $PublishDirectory `
        -OutputDirectory $outputRoot `
        -Version $version `
        -Publisher 'BO' `
        -MakensisPath $MakensisPath `
        -SevenZipPath $SevenZipPath `
        -TestRegistryKey $testRegistryKey `
        -TestMetadataRegistryKey $testMetadataRegistryKey `
        -InstallerTestMode `
        -TestFailAfterPayloadFile 3

    $installer = Join-Path $outputRoot "ASMILE-BO-Setup-$version.exe"
    if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw 'Test installer was not produced.' }
    $payloadFiles = @(Get-ChildItem -LiteralPath $PublishDirectory -Recurse -File | Sort-Object FullName)
    if ($payloadFiles.Count -lt 3) { throw 'Publish payload must contain at least three files.' }

    $relativePaths = @($payloadFiles[0..2] | ForEach-Object {
        $_.FullName.Substring((Resolve-Path -LiteralPath $PublishDirectory).Path.Length).TrimStart([char[]]@('\', '/'))
    })
    $seedContent1 = 'prior-version-file-1'
    $seedContent3 = 'prior-version-file-3'
    foreach ($index in @(0, 2)) {
        $destination = Join-Path $installRoot $relativePaths[$index]
        [void](New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force)
        $content = if ($index -eq 0) { $seedContent1 } else { $seedContent3 }
        [System.IO.File]::WriteAllText($destination, $content)
    }
    $uninstaller = Join-Path $installRoot 'Uninstall.exe'
    [System.IO.File]::WriteAllText($uninstaller, 'prior-uninstaller')
    $operatorMarker = Join-Path $installRoot 'operator-file.keep'
    [System.IO.File]::WriteAllText($operatorMarker, 'must-not-be-touched')

    $process = Start-Process -FilePath $installer -ArgumentList @('/S', "/D=$installRoot") -PassThru -NoNewWindow
    if (-not $process.WaitForExit(120000)) {
        $process.Kill()
        throw 'Fault-injected installer did not exit within two minutes; test did not complete cleanly.'
    }
    $process.Refresh()
    foreach ($index in @(0, 2)) {
        $destination = Join-Path $installRoot $relativePaths[$index]
        $expected = if ($index -eq 0) { $seedContent1 } else { $seedContent3 }
        if (-not (Test-Path -LiteralPath $destination -PathType Leaf) -or
            [System.IO.File]::ReadAllText($destination) -ne $expected) {
            throw "Rollback did not restore pre-existing payload file: $($relativePaths[$index])"
        }
    }

    $secondPayloadPath = Join-Path $installRoot $relativePaths[1]
    if (Test-Path -LiteralPath $secondPayloadPath) {
        throw "Rollback left a newly-created payload file behind: $($relativePaths[1])"
    }
    if ([System.IO.File]::ReadAllText($uninstaller) -ne 'prior-uninstaller') {
        throw 'Rollback changed the pre-existing Uninstall.exe.'
    }
    if ([System.IO.File]::ReadAllText($operatorMarker) -ne 'must-not-be-touched') {
        throw 'Rollback changed an unmanaged operator file.'
    }
    $metadata = Get-ItemProperty -LiteralPath $metadataPath
    if ($metadata.DisplayVersion -ne '0.9.0.0' -or
        $metadata.RollbackTestMarker -ne 'preserve-me' -or
        $metadata.PSObject.Properties['InstallTestAddedValue']) {
        throw 'Write-failure rollback did not restore existing installer metadata.'
    }

    $recoveryDirectories = @(Get-ChildItem -LiteralPath $installRoot -Directory -Force | Where-Object { $_.Name -match '^ns.*\.tmp$' })
    if ($recoveryDirectories.Count -gt 0) {
        throw "Rollback recovery directory was not cleaned after successful restore: $($recoveryDirectories[0].FullName)"
    }

    # Simulate process termination after live payload files have changed, then
    # start a recovery-only installer and verify exact restoration.
    [void](New-Item -ItemType Directory -Path $crashOutputRoot -Force)
    [void](New-Item -ItemType Directory -Path $recoveryOutputRoot -Force)
    & $builder `
        -PublishDirectory $PublishDirectory `
        -OutputDirectory $crashOutputRoot `
        -Version $version `
        -Publisher 'BO' `
        -MakensisPath $MakensisPath `
        -SevenZipPath $SevenZipPath `
        -TestRegistryKey $testRegistryKey `
        -TestMetadataRegistryKey $testMetadataRegistryKey `
        -InstallerTestMode `
        -TestCrashAfterMetadata `
        -TestCrashMarkerPath $crashMarker
    $crashInstaller = Join-Path $crashOutputRoot "ASMILE-BO-Setup-$version.exe"
    if (-not (Test-Path -LiteralPath $crashInstaller -PathType Leaf)) { throw 'Crash-test installer was not produced.' }
    $crashProcess = Start-Process -FilePath $crashInstaller -ArgumentList @('/S', "/D=$installRoot") -PassThru -NoNewWindow
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    while (-not (Test-Path -LiteralPath $crashMarker -PathType Leaf) -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 100
        $crashProcess.Refresh()
        if ($crashProcess.HasExited) { throw "Crash-test installer exited before its marker (exit $($crashProcess.ExitCode))." }
    }
    if (-not (Test-Path -LiteralPath $crashMarker -PathType Leaf)) {
        $crashProcess.Kill()
        throw 'Crash-test installer did not reach the fault-injection marker.'
    }
    $crashProcess.Kill()
    if (-not $crashProcess.WaitForExit(30000)) { throw 'Crash-test installer did not terminate after forced stop.' }

    $pendingRecoveryPath = "HKCU:\$testRegistryKey"
    $crashJournal = Get-ItemProperty -LiteralPath $pendingRecoveryPath
    $crashRecoveryDirectory = [string]$crashJournal.PendingRecoveryPath
    if ([string]::IsNullOrWhiteSpace($crashRecoveryDirectory) -or
        -not (Test-Path -LiteralPath (Join-Path $crashRecoveryDirectory 'transaction.active') -PathType Leaf)) {
        throw 'Interrupted installation did not retain an active recovery journal.'
    }
    $metadata = Get-ItemProperty -LiteralPath $metadataPath
    if ($metadata.DisplayVersion -ne $version) {
        throw 'Crash injection did not occur after installer metadata had changed.'
    }

    & $builder `
        -PublishDirectory $PublishDirectory `
        -OutputDirectory $recoveryOutputRoot `
        -Version $version `
        -Publisher 'BO' `
        -MakensisPath $MakensisPath `
        -SevenZipPath $SevenZipPath `
        -TestRegistryKey $testRegistryKey `
        -TestMetadataRegistryKey $testMetadataRegistryKey `
        -InstallerTestMode `
        -TestRecoveryOnly
    $recoveryInstaller = Join-Path $recoveryOutputRoot "ASMILE-BO-Setup-$version.exe"
    $recoveryProcess = Start-Process -FilePath $recoveryInstaller -ArgumentList @('/S', "/D=$installRoot") -PassThru -NoNewWindow
    if (-not $recoveryProcess.WaitForExit(120000)) {
        $recoveryProcess.Kill()
        throw 'Recovery-only installer did not exit within two minutes.'
    }
    foreach ($index in @(0, 2)) {
        $destination = Join-Path $installRoot $relativePaths[$index]
        $expected = if ($index -eq 0) { $seedContent1 } else { $seedContent3 }
        if (-not (Test-Path -LiteralPath $destination -PathType Leaf) -or
            [System.IO.File]::ReadAllText($destination) -ne $expected) {
            throw "Crash recovery did not restore pre-existing payload file: $($relativePaths[$index])"
        }
    }
    if (Test-Path -LiteralPath (Join-Path $installRoot $relativePaths[1])) {
        throw 'Crash recovery did not remove a newly-created payload file.'
    }
    if ([System.IO.File]::ReadAllText($uninstaller) -ne 'prior-uninstaller' -or
        [System.IO.File]::ReadAllText($operatorMarker) -ne 'must-not-be-touched') {
        throw 'Crash recovery changed the prior uninstaller or an unmanaged file.'
    }
    $metadata = Get-ItemProperty -LiteralPath $metadataPath
    if ($metadata.DisplayVersion -ne '0.9.0.0' -or
        $metadata.InstallLocation -ne $installRoot -or
        $metadata.RollbackTestMarker -ne 'preserve-me' -or
        $metadata.PSObject.Properties['InstallTestAddedValue']) {
        throw 'Crash recovery did not restore the previous installer registry metadata.'
    }
    if (Test-Path -LiteralPath $pendingRecoveryPath) {
        $pendingValues = Get-ItemProperty -LiteralPath $pendingRecoveryPath
        if ($pendingValues.PendingRecoveryPath) { throw 'Crash recovery did not clear its registry journal.' }
    }
    $crashRecoveryDirectories = @(Get-ChildItem -LiteralPath $installRoot -Directory -Force | Where-Object { $_.Name -match '^ns.*\.tmp$' })
    if ($crashRecoveryDirectories.Count -gt 0) {
        throw 'Crash recovery left the transaction directory behind.'
    }

    Write-Output 'INSTALLER_ROLLBACK_TEST=PASS'
    Write-Output 'PREEXISTING_FILES=RESTORED'
    Write-Output 'NEW_FILES=REMOVED'
    Write-Output 'UNMANAGED_FILE=PRESERVED'
    Write-Output 'CRASH_RECOVERY=RESTORED'
    Write-Output 'RECOVERY_JOURNAL=CLEARED'
    Write-Output 'TEST_INSTALLER_EXITED_NATURALLY=True'
    $testPassed = $true
}
finally {
    $resolvedTempRoot = [System.IO.Path]::GetFullPath($tempRoot).TrimEnd([char[]]@('\', '/'))
    $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedTestRoot.StartsWith($resolvedTempRoot + $separator, [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $resolvedTestRoot) -notmatch '^ASMILE-Installer-Rollback-Test-[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected installer test path: $resolvedTestRoot"
    }
    if ($testRegistryKey -notmatch '^Software\\ASMILE-BO-Tests\\[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected installer test registry key: $testRegistryKey"
    }
    if ($testMetadataRegistryKey -notmatch '^Software\\ASMILE-BO-TestMetadata\\[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected installer test metadata key: $testMetadataRegistryKey"
    }
    $registryPath = "HKCU:\$testRegistryKey"
    if (Test-Path -LiteralPath $registryPath) {
        Remove-Item -LiteralPath $registryPath -Recurse -Force
    }
    $metadataRegistryPath = "HKCU:\$testMetadataRegistryKey"
    if (Test-Path -LiteralPath $metadataRegistryPath) {
        Remove-Item -LiteralPath $metadataRegistryPath -Recurse -Force
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
    if (-not $testPassed) { Write-Output 'INSTALLER_ROLLBACK_TEST=FAIL' }
}
