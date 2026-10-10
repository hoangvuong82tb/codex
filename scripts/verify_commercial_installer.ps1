[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallerPath,

    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [string]$SevenZipPath,

    [switch]$RequireSigned
)

$ErrorActionPreference = 'Stop'
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw "Installer is missing: $InstallerPath" }
if (-not (Test-Path -LiteralPath $publish -PathType Container)) { throw "Publish directory is missing: $PublishDirectory" }

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
        throw '7-Zip CLI (7z) is required to inspect the compiled NSIS payload.'
    }
}
$sevenZip = (Resolve-Path -LiteralPath $SevenZipPath).Path
if (-not (Test-Path -LiteralPath $sevenZip -PathType Leaf)) { throw "7-Zip executable is missing: $SevenZipPath" }

$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($RequireSigned -and $signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
    throw "A production installer must have a valid Authenticode signature; found '$($signature.Status)'."
}

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$extractRoot = Join-Path $tempRoot ('asmile-installer-verify-' + [guid]::NewGuid().ToString('N'))
$extractFullPath = [System.IO.Path]::GetFullPath($extractRoot)
if (-not $extractFullPath.StartsWith($tempRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Verification extraction path escaped the temporary directory.'
}
[void](New-Item -ItemType Directory -Path $extractFullPath)

try {
    & $sevenZip x -y ("-o" + $extractFullPath) $installer | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "7-Zip could not extract the NSIS archive (exit code $LASTEXITCODE)." }

    $archivePayload = @(Get-ChildItem -LiteralPath $extractFullPath -Recurse -File | Where-Object {
        $_.Name -ne 'Uninstall.exe' -and $_.FullName -notmatch '[\\/]\$PLUGINSDIR[\\/]'
    })
    $publishedPayload = @(Get-ChildItem -LiteralPath $publish -Recurse -File)
    $toRelativePath = {
        param([System.IO.FileInfo]$File, [string]$Root)
        $File.FullName.Substring($Root.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
    }

    $archiveByPath = @{}
    foreach ($file in $archivePayload) {
        $relative = & $toRelativePath $file $extractFullPath
        if ($archiveByPath.ContainsKey($relative)) { throw "Duplicate path found in installer payload: $relative" }
        $archiveByPath[$relative] = $file
    }
    $publishByPath = @{}
    foreach ($file in $publishedPayload) {
        $relative = & $toRelativePath $file $publish
        if ($publishByPath.ContainsKey($relative)) { throw "Duplicate path found in publish payload: $relative" }
        $publishByPath[$relative] = $file
    }

    $missing = @($publishByPath.Keys | Where-Object { -not $archiveByPath.ContainsKey($_) })
    $extra = @($archiveByPath.Keys | Where-Object { -not $publishByPath.ContainsKey($_) })
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        throw "Installer inventory differs from publish. Missing: $($missing -join ', '); extra: $($extra -join ', ')."
    }

    $archiveExePath = Join-Path $extractFullPath 'ASMILE.BO.App.exe'
    if (-not (Test-Path -LiteralPath $archiveExePath -PathType Leaf)) { throw 'ASMILE.BO.App.exe is absent from the installer.' }
    $publishExe = Get-Item -LiteralPath (Join-Path $publish 'ASMILE.BO.App.exe')
    $archiveExe = Get-Item -LiteralPath $archiveExePath
    if ($archiveExe.VersionInfo.FileVersion -ne $publishExe.VersionInfo.FileVersion) {
        throw 'The executable FileVersion inside the installer differs from the verified publish.'
    }
    if ((Get-Item -LiteralPath $installer).VersionInfo.FileVersion -ne $publishExe.VersionInfo.FileVersion) {
        throw 'The installer FileVersion differs from the verified publish executable.'
    }
    $appSignature = Get-AuthenticodeSignature -LiteralPath $archiveExePath
    if ($RequireSigned -and $appSignature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "A production app executable must have a valid Authenticode signature; found '$($appSignature.Status)'."
    }

    $signedHashExceptions = 0
    foreach ($relative in $publishByPath.Keys) {
        $sourceFile = $publishByPath[$relative]
        $archiveFile = $archiveByPath[$relative]
        if ($sourceFile.Length -ne $archiveFile.Length) {
            if ($RequireSigned -and $relative -ieq 'ASMILE.BO.App.exe' -and $appSignature.Status -eq [System.Management.Automation.SignatureStatus]::Valid) {
                $signedHashExceptions++
                continue
            }
            throw "Payload size mismatch for $relative."
        }
        $sourceHash = (Get-FileHash -LiteralPath $sourceFile.FullName -Algorithm SHA256).Hash
        $archiveHash = (Get-FileHash -LiteralPath $archiveFile.FullName -Algorithm SHA256).Hash
        if ($sourceHash -ne $archiveHash) {
            if ($RequireSigned -and $relative -ieq 'ASMILE.BO.App.exe' -and $appSignature.Status -eq [System.Management.Automation.SignatureStatus]::Valid) {
                $signedHashExceptions++
                continue
            }
            throw "Payload SHA-256 mismatch for $relative."
        }
    }

    $expectedNotice = Join-Path (Split-Path -Parent $PSScriptRoot) 'installer\THIRD-PARTY-NOTICES.txt'
    $emittedNotice = Join-Path (Split-Path -Parent $installer) (([System.IO.Path]::GetFileNameWithoutExtension($installer)) + '-THIRD-PARTY-NOTICES.txt')
    if (-not (Test-Path -LiteralPath $emittedNotice -PathType Leaf)) {
        throw 'The installer third-party notice sidecar is missing.'
    }
    if ((Get-FileHash -LiteralPath $expectedNotice -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $emittedNotice -Algorithm SHA256).Hash) {
        throw 'The installer third-party notice does not match the reviewed source notice.'
    }

    Write-Output 'COMMERCIAL_INSTALLER=PASS'
    Write-Output ("PAYLOAD_FILE_COUNT=" + $publishedPayload.Count)
    Write-Output ("SIGNED_APP_HASH_EXCEPTIONS=" + $signedHashExceptions)
    Write-Output ("APP_SIGNATURE=" + $appSignature.Status)
    Write-Output ("INSTALLER_SIGNATURE=" + $signature.Status)
    Write-Output ("INSTALLER_SHA256=" + (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash)
}
finally {
    if (Test-Path -LiteralPath $extractFullPath -PathType Container) {
        Remove-Item -LiteralPath $extractFullPath -Recurse -Force
    }
}
