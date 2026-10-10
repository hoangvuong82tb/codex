[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Publisher,

    [string]$MakensisPath,

    [string]$SevenZipPath,

    [switch]$Production,

    [switch]$InstallerTestMode,

    [ValidateRange(0, 100000)]
    [int]$TestFailAfterPayloadFile = 0,

    [switch]$TestCrashAfterMetadata,
    [string]$TestCrashMarkerPath,

    [string]$TestRegistryKey,

    [string]$TestMetadataRegistryKey,

    [switch]$TestRecoveryOnly,

    [string]$SigningCertificateThumbprint,

    [string]$TimestampUrl
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$installerSource = Join-Path $repoRoot 'installer\ASMILE-BO.nsi'
$verifier = Join-Path $PSScriptRoot 'verify_commercial_package.ps1'
$installerVerifier = Join-Path $PSScriptRoot 'verify_commercial_installer.ps1'

function ConvertTo-NsisEscapedValue {
    param([AllowEmptyString()][string]$Value)
    if ($Value -match '["\x00-\x1F]') {
        throw 'Payload paths cannot contain quotes or control characters.'
    }
    return $Value.Replace('$', '$$')
}

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'Version must contain four numeric components, for example 1.2.3.0.'
}
[void][version]$Version
if ([string]::IsNullOrWhiteSpace($Publisher) -or $Publisher -match '["$`r`n]') {
    throw 'Publisher must be non-empty and cannot contain quotes, dollar signs, or line breaks.'
}
if ($Production -and ([string]::IsNullOrWhiteSpace($SigningCertificateThumbprint) -or [string]::IsNullOrWhiteSpace($TimestampUrl))) {
    throw 'Production builds require SigningCertificateThumbprint and TimestampUrl.'
}
if ($Production -and $InstallerTestMode) {
    throw 'InstallerTestMode cannot be combined with Production signing.'
}
if (($TestFailAfterPayloadFile -gt 0 -or $TestCrashAfterMetadata -or $TestRecoveryOnly) -and -not $InstallerTestMode) {
    throw 'Installer fault injection and recovery-only mode are permitted only in InstallerTestMode.'
}
if ($InstallerTestMode -and $TestFailAfterPayloadFile -eq 0 -and -not $TestCrashAfterMetadata -and -not $TestRecoveryOnly) {
    throw 'InstallerTestMode requires fault injection or recovery-only mode so it cannot produce a normal installable test build.'
}
if ($InstallerTestMode -and $TestRegistryKey -notmatch '^Software\\ASMILE-BO-Tests\\[0-9a-f]{32}$') {
    throw 'InstallerTestMode requires a unique Software\ASMILE-BO-Tests\<32-hex-guid> registry key.'
}
if ($InstallerTestMode -and $TestMetadataRegistryKey -notmatch '^Software\\ASMILE-BO-TestMetadata\\[0-9a-f]{32}$') {
    throw 'InstallerTestMode requires a unique Software\ASMILE-BO-TestMetadata\<32-hex-guid> registry key.'
}
if ($TestCrashAfterMetadata) {
    if ([string]::IsNullOrWhiteSpace($TestCrashMarkerPath) -or $TestCrashMarkerPath -match '["\x00-\x1F]') {
        throw 'Crash injection requires a marker path without quotes or control characters.'
    }
    $resolvedMarkerPath = [System.IO.Path]::GetFullPath($TestCrashMarkerPath)
    if (-not $resolvedMarkerPath.StartsWith([System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Crash injection marker must be inside the system temporary directory.'
    }
}
if ($Production -and $TimestampUrl -notmatch '^https://') {
    throw 'Production timestamp URL must use HTTPS.'
}
if (-not (Test-Path -LiteralPath $installerSource -PathType Leaf)) {
    throw "Installer source is missing: $installerSource"
}

$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
& $verifier -PublishDirectory $publishPath
$publishedExe = Get-Item -LiteralPath (Join-Path $publishPath 'ASMILE.BO.App.exe')
if ($publishedExe.VersionInfo.FileVersion -ne $Version) {
    throw "Installer version $Version does not match published app file version $($publishedExe.VersionInfo.FileVersion)."
}
if ([string]::IsNullOrWhiteSpace($MakensisPath)) {
    $makensisCommand = Get-Command 'makensis.exe' -ErrorAction SilentlyContinue
    if (-not $makensisCommand) { $makensisCommand = Get-Command 'makensis' -ErrorAction SilentlyContinue }
    if (-not $makensisCommand) {
        throw 'NSIS compiler (makensis) was not found. Install a reviewed NSIS 3.x distribution and rerun; no installer was produced.'
    }
    $MakensisPath = $makensisCommand.Source
}
$makensisPath = (Resolve-Path -LiteralPath $MakensisPath).Path
$compilerVersionOutput = (& $makensisPath /VERSION 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $compilerVersionOutput -ne 'v3.13') {
    throw "This release pipeline is pinned to NSIS v3.13; detected '$compilerVersionOutput'."
}

if ([string]::IsNullOrWhiteSpace($SevenZipPath)) {
    $sevenZipCommand = Get-Command '7z.exe' -ErrorAction SilentlyContinue
    if (-not $sevenZipCommand) { $sevenZipCommand = Get-Command '7z' -ErrorAction SilentlyContinue }
    if (-not $sevenZipCommand -and (Test-Path -LiteralPath 'C:\Program Files\7-Zip\7z.exe')) {
        $SevenZipPath = 'C:\Program Files\7-Zip\7z.exe'
    }
    elseif ($sevenZipCommand) {
        $SevenZipPath = $sevenZipCommand.Source
    }
    else {
        throw '7-Zip CLI (7z) is required to verify the compiled NSIS payload; no installer was produced.'
    }
}
$sevenZipPath = (Resolve-Path -LiteralPath $SevenZipPath).Path

$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$separator = [System.IO.Path]::DirectorySeparatorChar
if ($InstallerTestMode -and -not $outputPath.StartsWith($tempRoot + $separator, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'InstallerTestMode output must stay inside the system temporary directory.'
}
$publishPrefix = $publishPath.TrimEnd([char[]]@('\', '/')) + $separator
$outputPrefix = $outputPath.TrimEnd([char[]]@('\', '/')) + $separator
if ([string]::Equals($outputPath.TrimEnd([char[]]@('\', '/')), $publishPath.TrimEnd([char[]]@('\', '/')), [StringComparison]::OrdinalIgnoreCase) -or
    $outputPath.StartsWith($publishPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    $publishPath.StartsWith($outputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory and PublishDirectory must not be the same or nested inside one another.'
}

$certificate = $null
$signToolPath = $null
if ($Production) {
    $thumbprint = ($SigningCertificateThumbprint -replace '\s', '').ToUpperInvariant()
    $certificate = @(
        Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -CodeSigningCert -ErrorAction SilentlyContinue |
            Where-Object { $_.Thumbprint -eq $thumbprint -and $_.HasPrivateKey } |
            Select-Object -First 1
    ) | Select-Object -First 1
    if (-not $certificate) {
        throw 'The requested code-signing certificate with private key was not found in CurrentUser\My or LocalMachine\My.'
    }
    $signToolCommand = Get-Command 'signtool.exe' -ErrorAction SilentlyContinue
    if (-not $signToolCommand) { throw 'signtool.exe is required for production signing but was not found.' }
    $signToolPath = $signToolCommand.Source
}

[void](New-Item -ItemType Directory -Path $outputPath -Force)
$installerPath = Join-Path $outputPath "ASMILE-BO-Setup-$Version.exe"
$installerNoticePath = Join-Path $outputPath "ASMILE-BO-Setup-$Version-THIRD-PARTY-NOTICES.txt"
if (Test-Path -LiteralPath $installerPath) {
    throw "Refusing to overwrite an existing release artifact: $installerPath"
}
if (Test-Path -LiteralPath $installerNoticePath) {
    throw "Refusing to overwrite an existing release notice: $installerNoticePath"
}
$noticeSourcePath = Join-Path $repoRoot 'installer\THIRD-PARTY-NOTICES.txt'
if (-not (Test-Path -LiteralPath $noticeSourcePath -PathType Leaf)) {
    throw "Installer third-party notice is missing: $noticeSourcePath"
}

$stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("asmile-installer-" + [guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $stageRoot)
$stagePublish = Join-Path $stageRoot 'publish'
[void](New-Item -ItemType Directory -Path $stagePublish)
$payloadInclude = Join-Path $stageRoot 'Payload.nsh'
$buildPassed = $false

try {
    Get-ChildItem -LiteralPath $publishPath -Force | Copy-Item -Destination $stagePublish -Recurse -Force

    if ($Production) {
        & $signToolPath sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 (Join-Path $stagePublish 'ASMILE.BO.App.exe')
        if ($LASTEXITCODE -ne 0) { throw "Signing the application executable failed with exit code $LASTEXITCODE." }
        & $signToolPath verify /pa /all (Join-Path $stagePublish 'ASMILE.BO.App.exe')
        if ($LASTEXITCODE -ne 0) { throw 'Application executable signature verification failed.' }
    }

    $stageEntries = @(Get-ChildItem -LiteralPath $stagePublish -Recurse -Force)
    if (@($stageEntries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) {
        throw 'Publish payload contains a reparse point; refusing to package paths that may escape the staging tree.'
    }
    $files = @($stageEntries | Where-Object { -not $_.PSIsContainer } | Sort-Object FullName)
    if ($files.Count -eq 0) { throw 'Publish directory is empty.' }
    $payloadRows = [System.Collections.Generic.List[object]]::new()
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($stagePublish.Length).TrimStart([char[]]@('\', '/')).Replace('/', '\')
        if ($relative -match '["\x00-\x1F]') { throw "Payload path contains characters NSIS cannot represent safely: $relative" }
        $payloadRows.Add([pscustomobject]@{ RelativePath = $relative; Parent = [System.IO.Path]::GetDirectoryName($relative) })
    }

    $nsh = [System.Collections.Generic.List[string]]::new()
    $nsh.Add('!macro InstallPayload')
    $payloadIndex = 0
    foreach ($row in $payloadRows) {
        $payloadIndex++
        $parent = if ([string]::IsNullOrEmpty($row.Parent)) { '' } else { $row.Parent + '\' }
        $source = Join-Path $stagePublish ($row.RelativePath.Replace('\', [System.IO.Path]::DirectorySeparatorChar))
        $nsh.Add('  ClearErrors')
        $nsh.Add(('  SetOutPath "$INSTDIR\{0}"' -f (ConvertTo-NsisEscapedValue $parent)))
        if ([string]::IsNullOrEmpty($row.Parent)) { $nsh[$nsh.Count - 1] = '  SetOutPath "$INSTDIR"' }
        $nsh.Add('  IfErrors install_payload_failed')
        $nsh.Add('  ClearErrors')
        $quotedSource = '"' + (ConvertTo-NsisEscapedValue $source) + '"'
        $nsh.Add(('  File {0}' -f $quotedSource))
        $nsh.Add('  IfErrors install_payload_failed')
        if ($InstallerTestMode -and $TestFailAfterPayloadFile -eq $payloadIndex) {
            $nsh.Add('  ; Deterministic test-only fault injection after this payload write.')
            $nsh.Add('  SetErrors')
            $nsh.Add('  IfErrors install_payload_failed')
        }
    }
    $nsh.Add('!macroend')
    $nsh.Add('')

    # Include the generated uninstaller in the transaction: an upgrade must
    # be able to restore the previous uninstaller if replacing it fails.
    $transactionRows = @($payloadRows.ToArray()) + [pscustomobject]@{
        RelativePath = 'Uninstall.exe'
        Parent = ''
    }

    $nsh.Add('!macro BackupPayload')
    $transactionIndex = 0
    foreach ($row in $transactionRows) {
        $transactionIndex++
        $relative = ConvertTo-NsisEscapedValue $row.RelativePath
        $parent = if ([string]::IsNullOrEmpty($row.Parent)) { '' } else { $row.Parent + '\' }
        $backupDirectory = if ([string]::IsNullOrEmpty($row.Parent)) {
            '$RollbackDir\files'
        } else {
            '$RollbackDir\files\' + (ConvertTo-NsisEscapedValue $row.Parent)
        }
        $missingDirectory = if ([string]::IsNullOrEmpty($row.Parent)) {
            '$RollbackDir\missing'
        } else {
            '$RollbackDir\missing\' + (ConvertTo-NsisEscapedValue $row.Parent)
        }
        $existingLabel = 'backup_existing_{0:D4}' -f $transactionIndex
        $missingLabel = 'backup_missing_{0:D4}' -f $transactionIndex
        $doneLabel = 'backup_done_{0:D4}' -f $transactionIndex

        $nsh.Add(('  IfFileExists "$INSTDIR\{0}" {1} {2}' -f $relative, $existingLabel, $missingLabel))
        $nsh.Add(($existingLabel + ':'))
        $nsh.Add(('  ClearErrors'))
        $nsh.Add(('  CreateDirectory "{0}"' -f $backupDirectory))
        $nsh.Add('  IfErrors install_backup_failed')
        $nsh.Add(('  CopyFiles /SILENT "$INSTDIR\{0}" "{1}"' -f $relative, $backupDirectory))
        $nsh.Add('  IfErrors install_backup_failed')
        $nsh.Add(('  Goto {0}' -f $doneLabel))
        $nsh.Add(($missingLabel + ':'))
        $nsh.Add('  ClearErrors')
        $nsh.Add(('  CreateDirectory "{0}"' -f $missingDirectory))
        $nsh.Add('  IfErrors install_backup_failed')
        $nsh.Add(('  FileOpen $R9 "$RollbackDir\missing\{0}" w' -f $relative))
        $nsh.Add('  IfErrors install_backup_failed')
        $nsh.Add('  FileClose $R9')
        $nsh.Add('  IfErrors install_backup_failed')
        $nsh.Add(($doneLabel + ':'))
    }
    $nsh.Add('!macroend')
    $nsh.Add('')

    $nsh.Add('!macro RollbackPayload ERROR_LABEL')
    $transactionIndex = 0
    foreach ($row in $transactionRows) {
        $transactionIndex++
        $relative = ConvertTo-NsisEscapedValue $row.RelativePath
        $parent = if ([string]::IsNullOrEmpty($row.Parent)) { '' } else { $row.Parent + '\' }
        $backupFile = if ([string]::IsNullOrEmpty($row.Parent)) {
            '$RollbackDir\files\' + $relative
        } else {
            '$RollbackDir\files\' + (ConvertTo-NsisEscapedValue $row.Parent) + '\' + $relative.Substring($row.Parent.Length + 1)
        }
        $missingFile = if ([string]::IsNullOrEmpty($row.Parent)) {
            '$RollbackDir\missing\' + $relative
        } else {
            '$RollbackDir\missing\' + (ConvertTo-NsisEscapedValue $row.Parent) + '\' + $relative.Substring($row.Parent.Length + 1)
        }
        $restoreLabel = 'rollback_restore_{0:D4}' -f $transactionIndex
        $deleteLabel = 'rollback_delete_{0:D4}' -f $transactionIndex
        $doneLabel = 'rollback_done_{0:D4}' -f $transactionIndex

        $nsh.Add(('  IfFileExists "{0}" {1}' -f $backupFile, $restoreLabel))
        $nsh.Add(('  IfFileExists "{0}" {1}' -f $missingFile, $deleteLabel))
        $nsh.Add('  Goto ${ERROR_LABEL}')
        $nsh.Add(($restoreLabel + ':'))
        $nsh.Add('  ClearErrors')
        if ([string]::IsNullOrEmpty($row.Parent)) {
            $nsh.Add('  SetOutPath "$INSTDIR"')
        } else {
            $nsh.Add(('  SetOutPath "$INSTDIR\{0}"' -f (ConvertTo-NsisEscapedValue $row.Parent)))
        }
        $nsh.Add('  IfErrors ${ERROR_LABEL}')
        $nsh.Add(('  CopyFiles /SILENT "{0}" "$INSTDIR\{1}"' -f $backupFile, (ConvertTo-NsisEscapedValue $parent)))
        $nsh.Add('  IfErrors ${ERROR_LABEL}')
        $nsh.Add(('  Goto {0}' -f $doneLabel))
        $nsh.Add(($deleteLabel + ':'))
        $nsh.Add('  ClearErrors')
        $nsh.Add(('  Delete "$INSTDIR\{0}"' -f $relative))
        $nsh.Add('  IfErrors ${ERROR_LABEL}')
        $nsh.Add(($doneLabel + ':'))
    }
    $nsh.Add('!macroend')
    $nsh.Add('')

    $nsh.Add('!macro UninstallPayload')
    foreach ($row in $payloadRows) {
        $nsh.Add(('  Delete "$INSTDIR\{0}"' -f (ConvertTo-NsisEscapedValue $row.RelativePath)))
    }
    $directories = @($payloadRows | ForEach-Object Parent | Where-Object { -not [string]::IsNullOrEmpty($_) } | Sort-Object -Unique)
    $directories = @($directories | Sort-Object { ($_ -split '\\').Count } -Descending)
    foreach ($directory in $directories) {
        $nsh.Add(('  RMDir "$INSTDIR\{0}"' -f (ConvertTo-NsisEscapedValue $directory)))
    }
    $nsh.Add('!macroend')
    [System.IO.File]::WriteAllLines($payloadInclude, $nsh.ToArray(), [System.Text.UTF8Encoding]::new($false))

    if ($TestFailAfterPayloadFile -gt $payloadRows.Count) {
        throw "TestFailAfterPayloadFile ($TestFailAfterPayloadFile) exceeds payload file count ($($payloadRows.Count))."
    }
    $compilerArgs = @(
        "/DAPP_VERSION=$Version",
        "/DPUBLISHER=$Publisher",
        "/DPAYLOAD_INCLUDE=$payloadInclude",
        "/DOUTPUT_PATH=$installerPath",
        $installerSource
    )
    if ($InstallerTestMode) { $compilerArgs = @('/DASMILE_INSTALLER_TEST_MODE') + $compilerArgs }
    if ($InstallerTestMode) { $compilerArgs = @("/DTEST_REGISTRY_KEY=$TestRegistryKey") + $compilerArgs }
    if ($InstallerTestMode) { $compilerArgs = @("/DTEST_METADATA_REGISTRY_KEY=$TestMetadataRegistryKey") + $compilerArgs }
    if ($TestRecoveryOnly) { $compilerArgs = @('/DASMILE_INSTALLER_TEST_RECOVERY_ONLY') + $compilerArgs }
    if ($TestCrashAfterMetadata) {
        $compilerArgs = @('/DASMILE_INSTALLER_TEST_CRASH_AFTER_METADATA') + $compilerArgs
        $compilerArgs = @('/DTEST_CRASH_MARKER=' + (ConvertTo-NsisEscapedValue $resolvedMarkerPath)) + $compilerArgs
    }
    & $makensisPath @compilerArgs
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
        throw "NSIS compilation failed (exit code $LASTEXITCODE)."
    }

    if ($Production) {
        & $signToolPath sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $installerPath
        if ($LASTEXITCODE -ne 0) { throw "Signing the installer failed with exit code $LASTEXITCODE." }
        & $signToolPath verify /pa /all $installerPath
        if ($LASTEXITCODE -ne 0) { throw 'Installer signature verification failed.' }
    }

    Copy-Item -LiteralPath $noticeSourcePath -Destination $installerNoticePath
    & $installerVerifier -InstallerPath $installerPath -PublishDirectory $publishPath -SevenZipPath $sevenZipPath -RequireSigned:$Production
    if ($LASTEXITCODE -ne 0) { throw 'Compiled installer payload verification failed.' }

    $installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash
    Write-Output 'INSTALLER_BUILD=PASS'
    Write-Output "PRODUCTION_SIGNED=$([bool]$Production)"
    Write-Output "INSTALLER_SHA256=$installerHash"
    Write-Output "INSTALLER_PATH=$installerPath"
    Write-Output "INSTALLER_NOTICES_PATH=$installerNoticePath"
    $buildPassed = $true
}
finally {
    if (-not $buildPassed) {
        if (Test-Path -LiteralPath $installerPath -PathType Leaf) { Remove-Item -LiteralPath $installerPath -Force }
        if (Test-Path -LiteralPath $installerNoticePath -PathType Leaf) { Remove-Item -LiteralPath $installerNoticePath -Force }
    }
    if (Test-Path -LiteralPath $stageRoot) {
        Remove-Item -LiteralPath $stageRoot -Recurse -Force
    }
}
