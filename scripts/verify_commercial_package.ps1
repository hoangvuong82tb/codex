[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [switch]$RequireRunningProcess
)

$ErrorActionPreference = 'Stop'
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$checks = [System.Collections.Generic.List[string]]::new()

function Require-Check {
    param(
        [bool]$Condition,
        [string]$Name,
        [string]$Detail
    )

    if (-not $Condition) {
        throw "PACKAGE_CHECK_FAILED: $Name - $Detail"
    }

    $checks.Add($Name)
}

$exePath = Join-Path $publishPath 'ASMILE.BO.App.exe'
$settingsPath = Join-Path $publishPath 'appsettings.json'
$depsPath = Join-Path $publishPath 'ASMILE.BO.App.deps.json'
$runtimeConfigPath = Join-Path $publishPath 'ASMILE.BO.App.runtimeconfig.json'
$noticesPath = Join-Path $publishPath 'THIRD-PARTY-NOTICES.txt'

Require-Check (Test-Path -LiteralPath $exePath -PathType Leaf) 'Executable' 'ASMILE.BO.App.exe is missing.'
Require-Check (Test-Path -LiteralPath $settingsPath -PathType Leaf) 'RuntimeSettings' 'appsettings.json is missing.'
Require-Check (Test-Path -LiteralPath $depsPath -PathType Leaf) 'DependencyManifest' 'ASMILE.BO.App.deps.json is missing.'
Require-Check (Test-Path -LiteralPath $runtimeConfigPath -PathType Leaf) 'RuntimeConfig' 'ASMILE.BO.App.runtimeconfig.json is missing.'
Require-Check (Test-Path -LiteralPath $noticesPath -PathType Leaf) 'ThirdPartyNotices' 'THIRD-PARTY-NOTICES.txt is missing.'

$deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
$runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Raw | ConvertFrom-Json
$runtimeOptions = $runtimeConfig.runtimeOptions
$includedFrameworks = @($runtimeOptions.includedFrameworks)
$includedFrameworkNames = @($includedFrameworks | ForEach-Object { $_.name })
$selfContainedFrameworkNames = @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')
$missingFrameworkNames = @($selfContainedFrameworkNames | Where-Object { $includedFrameworkNames -notcontains $_ })
$hasSharedFrameworkRequirement = @($runtimeOptions.PSObject.Properties.Name | Where-Object { $_ -in 'framework', 'frameworks' }).Count -gt 0
$hasSelfContainedRuntime = $includedFrameworks.Count -ge 2 -and -not $hasSharedFrameworkRequirement -and $missingFrameworkNames.Count -eq 0
Require-Check $hasSelfContainedRuntime 'SelfContainedRuntime' 'Commercial Windows packages must include both .NETCore.App and WindowsDesktop.App instead of requiring a shared machine runtime.'
$runtimeTargetName = [string]$deps.runtimeTarget.name
Require-Check ($runtimeTargetName -match '(?i)/win-x64$') 'WindowsX64Runtime' "Commercial installer payload must target win-x64; found runtime target '$runtimeTargetName'."
$runtimePackageCount = @($deps.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' }).Count
$notices = Get-Content -LiteralPath $noticesPath -Raw -Encoding UTF8
$noticePackageCount = [regex]::Matches($notices, '(?m)^- [^\s/]+/[^\s|]+ \| License=').Count
Require-Check ($noticePackageCount -eq $runtimePackageCount) 'RuntimePackageLicenseInventory' "Expected $runtimePackageCount published NuGet packages in notices, found $noticePackageCount."
Require-Check ($notices.Contains('Microsoft.Data.SqlClient.SNI.runtime/')) 'SqlClientSniPackageNotice' 'Microsoft.Data.SqlClient.SNI.runtime is missing from third-party notices.'
Require-Check ($notices.Contains('MICROSOFT SOFTWARE LICENSE TERMS')) 'SqlClientSniLicenseText' 'The Microsoft.Data.SqlClient.SNI.runtime license text is missing from third-party notices.'

$licenseSourceDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) 'licenses\third-party'
foreach ($license in @(
    [pscustomobject]@{ SpdxId = 'MIT'; FileName = 'MIT.txt' },
    [pscustomobject]@{ SpdxId = 'Apache-2.0'; FileName = 'Apache-2.0.txt' }
)) {
    $licenseSourcePath = Join-Path $licenseSourceDirectory $license.FileName
    Require-Check (Test-Path -LiteralPath $licenseSourcePath -PathType Leaf) "CanonicalLicenseSource$($license.SpdxId.Replace('-', ''))" "The canonical $($license.SpdxId) license source is missing."
    $canonicalLicenseText = [regex]::Replace((Get-Content -LiteralPath $licenseSourcePath -Raw), '\s+', ' ').Trim()
    $normalizedNotices = [regex]::Replace($notices, '\s+', ' ').Trim()
    $licenseMarker = "----- BEGIN SPDX $($license.SpdxId) License -----"
    Require-Check ($notices.Contains($licenseMarker) -and $normalizedNotices.IndexOf($canonicalLicenseText, [StringComparison]::Ordinal) -ge 0) "CanonicalLicenseIncluded$($license.SpdxId.Replace('-', ''))" "The complete canonical $($license.SpdxId) license text is missing from third-party notices."
}

$verifiedAttributions = @(
    [pscustomobject]@{ Identity = 'ClosedXML.Parser/1.2.0'; Notice = ('Copyright (c) 2023, Jan Havl' + [char]0x00ED + [char]0x010D + 'ek') },
    [pscustomobject]@{ Identity = 'ClosedXML/0.104.2'; Notice = 'Copyright (c) 2016 ClosedXML' },
    [pscustomobject]@{ Identity = 'ExcelNumberFormat/1.1.0'; Notice = 'Copyright (c) 2017 andersnm' },
    [pscustomobject]@{ Identity = 'QRCoder/1.6.0'; Notice = 'Copyright (c) 2013-2018 Raffael Herrmann' },
    [pscustomobject]@{ Identity = 'Serilog.Extensions.Hosting/8.0.0'; Notice = 'Copyright 2020 Serilog Contributors' },
    [pscustomobject]@{ Identity = 'Serilog.Extensions.Logging/8.0.0'; Notice = 'Copyright (c) .NET Foundation. All rights reserved.' },
    [pscustomobject]@{ Identity = 'Serilog.Settings.Configuration/8.0.0'; Notice = 'Copyright 2013-2016 Serilog Contributors' },
    [pscustomobject]@{ Identity = 'Serilog.Sinks.File/5.0.0'; Notice = 'Copyright 2013-2016 Serilog Contributors' }
)
$publishedIdentities = @($deps.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | ForEach-Object Name)
$expectedAttributions = @($verifiedAttributions | Where-Object { $publishedIdentities -contains $_.Identity })
foreach ($attribution in $expectedAttributions) {
    $attributionMarker = "----- BEGIN $($attribution.Identity) upstream attribution -----"
    $attributionPresent = $notices.Contains($attributionMarker) -and $notices.Contains($attribution.Notice)
    Require-Check $attributionPresent "UpstreamAttribution$($attribution.Identity.Replace('/', '').Replace('.', '').Replace('-', ''))" "Verified upstream attribution for $($attribution.Identity) is missing from THIRD-PARTY-NOTICES.txt."
}
$actualAttributionCount = [regex]::Matches($notices, '(?m)^----- BEGIN [^\r\n]+ upstream attribution -----\r?$').Count
Require-Check ($actualAttributionCount -eq $expectedAttributions.Count) 'UpstreamAttributionInventory' "Expected $($expectedAttributions.Count) version-specific attributions, found $actualAttributionCount."

$versionedLicenseMappings = @(
    [pscustomobject]@{ Identity = 'ClosedXML.Parser/1.2.0'; FileName = 'ClosedXML.Parser-1.2.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML.Parser/1.2.0/LICENSE.txt' },
    [pscustomobject]@{ Identity = 'ClosedXML/0.104.2'; FileName = 'ClosedXML-0.104.2-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML/0.104.2/LICENSE' },
    [pscustomobject]@{ Identity = 'ExcelNumberFormat/1.1.0'; FileName = 'ExcelNumberFormat-1.1.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/andersnm/ExcelNumberFormat/v1.1.0/LICENSE' },
    [pscustomobject]@{ Identity = 'QRCoder/1.6.0'; FileName = 'QRCoder-1.6.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/Shane32/QRCoder/v1.6.0/LICENSE.txt' }
)
$expectedVersionedLicenses = @($versionedLicenseMappings | Where-Object { $publishedIdentities -contains $_.Identity })
foreach ($mapping in $expectedVersionedLicenses) {
    $sourceLicensePath = Join-Path (Join-Path $licenseSourceDirectory 'packages') $mapping.FileName
    Require-Check (Test-Path -LiteralPath $sourceLicensePath -PathType Leaf) "VersionedLicenseSource$($mapping.Identity.Replace('/', '').Replace('.', '').Replace('-', ''))" "Version-pinned license source for $($mapping.Identity) is missing."
    $licenseMarker = "----- BEGIN $($mapping.Identity) upstream license -----"
    $licenseEndMarker = "----- END $($mapping.Identity) upstream license -----"
    $licenseText = [regex]::Replace((Get-Content -LiteralPath $sourceLicensePath -Raw -Encoding UTF8), '\s+', ' ').Trim()
    $licenseStart = $notices.IndexOf($licenseMarker, [StringComparison]::Ordinal)
    $licenseEnd = if ($licenseStart -ge 0) { $notices.IndexOf($licenseEndMarker, $licenseStart, [StringComparison]::Ordinal) } else { -1 }
    $licenseSection = if ($licenseStart -ge 0 -and $licenseEnd -gt $licenseStart) { $notices.Substring($licenseStart, $licenseEnd - $licenseStart) } else { '' }
    $normalizedLicenseSection = [regex]::Replace($licenseSection, '\s+', ' ')
    Require-Check ($licenseSection.Contains("Source: $($mapping.Source)") -and $normalizedLicenseSection.IndexOf($licenseText, [StringComparison]::Ordinal) -ge 0) "VersionedLicenseIncluded$($mapping.Identity.Replace('/', '').Replace('.', '').Replace('-', ''))" "Complete upstream license text and version-pinned source for $($mapping.Identity) are missing from notices."
}
$actualVersionedLicenseCount = [regex]::Matches($notices, '(?m)^----- BEGIN [^\r\n]+ upstream license -----\r?$').Count
Require-Check ($actualVersionedLicenseCount -eq $expectedVersionedLicenses.Count) 'VersionedLicenseInventory' "Expected $($expectedVersionedLicenses.Count) version-pinned package licenses, found $actualVersionedLicenseCount."

$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
Require-Check ($settings.Database.MigrationMode -eq 'ValidateOnly') 'MigrationMode' 'Release package must validate migrations only.'
Require-Check ($settings.Database.AllowApplyPendingMigrations -eq $false) 'ApplyMigrationsDisabled' 'Release package must not apply pending migrations.'
Require-Check ($settings.Database.FailOnPendingMigrations -eq $true) 'PendingMigrationFailClosed' 'Release package must fail closed when migrations are pending.'
Require-Check ([string]::IsNullOrWhiteSpace([string]$settings.Database.ConnectionString)) 'ConnectionStringExternalized' 'Connection string must be supplied outside the package.'

$pdbCount = @(Get-ChildItem -LiteralPath $publishPath -Recurse -File -Filter '*.pdb').Count
Require-Check ($pdbCount -eq 0) 'DebugSymbolsAbsent' "Found $pdbCount PDB file(s)."

$textFiles = @(Get-ChildItem -LiteralPath $publishPath -Recurse -File | Where-Object {
    $_.Extension -in '.json', '.config', '.xml', '.yaml', '.yml', '.txt'
})
$secretPattern = '(?i)(Password\s*=|Pwd\s*=|"(password|clientsecret|secret)"\s*:\s*"\S+)'
$secretMatches = @($textFiles | Select-String -Pattern $secretPattern -ErrorAction SilentlyContinue)
Require-Check ($secretMatches.Count -eq 0) 'NoPlaintextSecrets' 'A likely plaintext secret was found in a package text file.'

if ($RequireRunningProcess) {
    $running = @(Get-Process ASMILE.BO.App -ErrorAction SilentlyContinue | Where-Object {
        try { [string]::Equals($_.Path, $exePath, [StringComparison]::OrdinalIgnoreCase) } catch { $false }
    })
    Require-Check ($running.Count -ge 1) 'RunningExecutable' 'No running process matches the published executable.'
}

$hash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash
$artifactFiles = @(Get-ChildItem -LiteralPath $publishPath -Recurse -File)
$relativePaths = [string[]]@($artifactFiles | ForEach-Object {
    $_.FullName.Substring($publishPath.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
})
[Array]::Sort($relativePaths, [StringComparer]::OrdinalIgnoreCase)

$inventory = [System.Text.StringBuilder]::new()
foreach ($relativePath in $relativePaths) {
    $filePath = Join-Path $publishPath ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $fileHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
    $fileLength = (Get-Item -LiteralPath $filePath).Length
    [void]$inventory.Append($relativePath).Append("`t").Append($fileLength).Append("`t").Append($fileHash).Append("`n")
}
$treeBytes = [System.Text.UTF8Encoding]::new($false).GetBytes($inventory.ToString())
$treeHasher = [System.Security.Cryptography.SHA256]::Create()
try {
    $treeHash = [System.BitConverter]::ToString($treeHasher.ComputeHash($treeBytes)).Replace('-', '')
}
finally {
    $treeHasher.Dispose()
}

Write-Output 'COMMERCIAL_PACKAGE=PASS'
Write-Output ("CHECK_COUNT=" + $checks.Count)
Write-Output ("PDB_COUNT=" + $pdbCount)
Write-Output ("EXE_SHA256=" + $hash)
Write-Output ("PUBLISH_FILE_COUNT=" + $artifactFiles.Count)
Write-Output ("PUBLISH_TREE_SHA256=" + $treeHash)
Write-Output ("PUBLISH_DIRECTORY=" + $publishPath)
