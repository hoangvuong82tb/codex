[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DepsJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$PackageRoot = (Join-Path $env:USERPROFILE '.nuget\packages')
)

$ErrorActionPreference = 'Stop'
$depsPath = (Resolve-Path -LiteralPath $DepsJsonPath).Path
$deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
$packageRootPath = (Resolve-Path -LiteralPath $PackageRoot).Path
$packages = [System.Collections.Generic.List[object]]::new()
$licenseDocuments = [System.Collections.Generic.List[object]]::new()
$seenDocuments = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($library in $deps.libraries.PSObject.Properties | Sort-Object Name) {
    if ($library.Value.type -ne 'package') {
        continue
    }

    $identity = $library.Name -split '/', 2
    if ($identity.Count -ne 2) {
        throw "Invalid NuGet package identity in deps manifest: $($library.Name)"
    }

    $packageId = $identity[0]
    $version = $identity[1]
    $packageDirectory = Join-Path (Join-Path $packageRootPath $packageId.ToLowerInvariant()) $version
    $nuspecPath = Join-Path $packageDirectory ($packageId + '.nuspec')
    if (-not (Test-Path -LiteralPath $nuspecPath -PathType Leaf)) {
        throw "NuGet metadata is missing for published package $($library.Name): $nuspecPath"
    }

    [xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
    $metadata = $nuspec.package.metadata
    $licenseNode = $metadata.license
    $license = if ($licenseNode) { [string]$licenseNode.InnerText.Trim() } else { '' }
    $licenseType = if ($licenseNode) { [string]$licenseNode.type } else { '' }
    $licenseUrl = [string]$metadata.licenseUrl
    if ([string]::IsNullOrWhiteSpace($license) -and [string]::IsNullOrWhiteSpace($licenseUrl)) {
        throw "Published package has no declared license metadata: $($library.Name)"
    }

    $projectUrl = [string]$metadata.projectUrl
    $repositoryUrl = [string]$metadata.repository.url
    $copyright = [string]$metadata.copyright
    $authors = [string]$metadata.authors
    $packages.Add([pscustomobject]@{
        Identity = $library.Name
        License = if ($license) { $license } else { 'Legacy license URL' }
        LicenseType = $licenseType
        Authors = $authors
        LicenseUrl = $licenseUrl
        ProjectUrl = $projectUrl
        RepositoryUrl = $repositoryUrl
        Copyright = $copyright
    })

    $documentPaths = [System.Collections.Generic.List[string]]::new()
    if ($licenseType -eq 'file') {
        $declaredLicensePath = Join-Path $packageDirectory $license
        if (-not (Test-Path -LiteralPath $declaredLicensePath -PathType Leaf)) {
            throw "Declared license file is missing for $($library.Name): $declaredLicensePath"
        }
        $documentPaths.Add($declaredLicensePath)
    }

    $packageLegalFiles = Get-ChildItem -LiteralPath $packageDirectory -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^(LICENSE|LICENCE|NOTICE|COPYING)(\.|$)' }
    foreach ($file in $packageLegalFiles) {
        $documentPaths.Add($file.FullName)
    }

    foreach ($documentPath in $documentPaths | Select-Object -Unique) {
        $documentText = Get-Content -LiteralPath $documentPath -Raw
        $sha256 = [Security.Cryptography.SHA256]::Create()
        try {
            $hashBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($documentText))
            $documentHash = [BitConverter]::ToString($hashBytes).Replace('-', '')
        }
        finally {
            $sha256.Dispose()
        }
        if ($seenDocuments.Add($documentHash)) {
            $licenseDocuments.Add([pscustomobject]@{
                Package = $library.Name
                FileName = [IO.Path]::GetFileName($documentPath)
                Text = $documentText.TrimEnd()
            })
        }
    }
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('THIRD-PARTY NOTICES')
$lines.Add('')
$lines.Add('This file inventories the NuGet runtime packages in the published ASMILE.BO dependency manifest.')
$lines.Add('Package metadata and license files are reproduced from the corresponding local NuGet packages.')
$lines.Add('Canonical SPDX license texts referenced by runtime package license expressions are included below. Packages without package-specific license files may still require separate upstream notice and attribution review.')
$lines.Add('This inventory does not replace legal review of applicable third-party terms or customer-facing distribution agreements.')
$lines.Add('')
$lines.Add('RUNTIME PACKAGE INVENTORY')
$lines.Add('')
foreach ($package in $packages) {
    $details = [System.Collections.Generic.List[string]]::new()
    $details.Add("License=$($package.License)")
    if ($package.Authors) { $details.Add("Authors=$($package.Authors)") }
    if ($package.Copyright) { $details.Add("Copyright=$($package.Copyright)") }
    else { $details.Add('Copyright=not declared in NuGet metadata; verify upstream attribution') }
    if ($package.ProjectUrl) { $details.Add("Project=$($package.ProjectUrl)") }
    if ($package.RepositoryUrl) { $details.Add("Source=$($package.RepositoryUrl)") }
    if ($package.LicenseUrl) { $details.Add("LicenseUrl=$($package.LicenseUrl)") }
    $lines.Add("- $($package.Identity) | $($details -join '; ')")
}

$lines.Add('')
$lines.Add('LICENSE AND NOTICE FILES INCLUDED FROM PACKAGES')
$lines.Add('')
foreach ($document in $licenseDocuments) {
    $lines.Add("----- BEGIN $($document.Package) / $($document.FileName) -----")
    $lines.Add($document.Text)
    $lines.Add("----- END $($document.Package) / $($document.FileName) -----")
    $lines.Add('')
}

# These package/version-specific notices were verified against the upstream
# release source or license file. Keep this mapping explicit and offline-safe;
# do not infer copyright holders from the NuGet Authors field.
$verifiedAttributions = @(
    [pscustomobject]@{ Identity = 'ClosedXML.Parser/1.2.0'; Notice = ('Copyright (c) 2023, Jan Havl' + [char]0x00ED + [char]0x010D + 'ek'); Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML.Parser/1.2.0/LICENSE.txt' },
    [pscustomobject]@{ Identity = 'ClosedXML/0.104.2'; Notice = 'Copyright (c) 2016 ClosedXML'; Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML/0.104.2/LICENSE' },
    [pscustomobject]@{ Identity = 'ExcelNumberFormat/1.1.0'; Notice = 'Copyright (c) 2017 andersnm'; Source = 'https://raw.githubusercontent.com/andersnm/ExcelNumberFormat/v1.1.0/LICENSE' },
    [pscustomobject]@{ Identity = 'QRCoder/1.6.0'; Notice = 'Copyright (c) 2013-2018 Raffael Herrmann'; Source = 'https://raw.githubusercontent.com/Shane32/QRCoder/v1.6.0/LICENSE.txt' },
    [pscustomobject]@{ Identity = 'Serilog.Extensions.Hosting/8.0.0'; Notice = 'Copyright 2020 Serilog Contributors'; Source = 'https://github.com/serilog/serilog-extensions-hosting/blob/v8.0.0/src/Serilog.Extensions.Hosting/SerilogHostBuilderExtensions.cs' },
    [pscustomobject]@{ Identity = 'Serilog.Extensions.Logging/8.0.0'; Notice = 'Copyright (c) .NET Foundation. All rights reserved.'; Source = 'https://github.com/serilog/serilog-extensions-logging/blob/v8.0.0/src/Serilog.Extensions.Logging/Extensions/Logging/SerilogLogger.cs' },
    [pscustomobject]@{ Identity = 'Serilog.Settings.Configuration/8.0.0'; Notice = 'Copyright 2013-2016 Serilog Contributors'; Source = 'https://github.com/serilog/serilog-settings-configuration/blob/v8.0.0/src/Serilog.Settings.Configuration/ConfigurationLoggerConfigurationExtensions.cs' },
    [pscustomobject]@{ Identity = 'Serilog.Sinks.File/5.0.0'; Notice = 'Copyright 2013-2016 Serilog Contributors'; Source = 'https://github.com/serilog/serilog-sinks-file/blob/v5.0.0/src/Serilog.Sinks.File/Sinks/File/FileSink.cs' }
)
$includedAttributionCount = 0
$lines.Add('')
$lines.Add('VERIFIED UPSTREAM COPYRIGHT AND ATTRIBUTION NOTICES')
$lines.Add('')
foreach ($attribution in $verifiedAttributions) {
    if (-not ($packages | Where-Object { $_.Identity -eq $attribution.Identity })) {
        continue
    }
    $includedAttributionCount++
    $lines.Add("----- BEGIN $($attribution.Identity) upstream attribution -----")
    $lines.Add($attribution.Notice)
    $lines.Add("Source: $($attribution.Source)")
    $lines.Add("----- END $($attribution.Identity) upstream attribution -----")
    $lines.Add('')
}

$versionedPackageLicenses = @(
    [pscustomobject]@{ Identity = 'ClosedXML.Parser/1.2.0'; FileName = 'ClosedXML.Parser-1.2.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML.Parser/1.2.0/LICENSE.txt' },
    [pscustomobject]@{ Identity = 'ClosedXML/0.104.2'; FileName = 'ClosedXML-0.104.2-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/ClosedXML/ClosedXML/0.104.2/LICENSE' },
    [pscustomobject]@{ Identity = 'ExcelNumberFormat/1.1.0'; FileName = 'ExcelNumberFormat-1.1.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/andersnm/ExcelNumberFormat/v1.1.0/LICENSE' },
    [pscustomobject]@{ Identity = 'QRCoder/1.6.0'; FileName = 'QRCoder-1.6.0-LICENSE.txt'; Source = 'https://raw.githubusercontent.com/Shane32/QRCoder/v1.6.0/LICENSE.txt' }
)
$versionedLicenseCount = 0
$lines.Add('')
$lines.Add('VERSION-PINNED UPSTREAM PACKAGE LICENSE FILES')
$lines.Add('')
foreach ($mapping in $versionedPackageLicenses) {
    if (-not ($packages | Where-Object { $_.Identity -eq $mapping.Identity })) {
        continue
    }

    $sourceLicensePath = Join-Path (Join-Path (Split-Path -Parent $PSScriptRoot) 'licenses\third-party\packages') $mapping.FileName
    if (-not (Test-Path -LiteralPath $sourceLicensePath -PathType Leaf)) {
        throw "Version-pinned upstream license for $($mapping.Identity) is missing: $sourceLicensePath"
    }

    $versionedLicenseCount++
    $lines.Add("----- BEGIN $($mapping.Identity) upstream license -----")
    $lines.Add("Source: $($mapping.Source)")
    $lines.Add((Get-Content -LiteralPath $sourceLicensePath -Raw -Encoding UTF8).TrimEnd())
    $lines.Add("----- END $($mapping.Identity) upstream license -----")
    $lines.Add('')
}

$canonicalLicenseMappings = @(
    [pscustomobject]@{
        SpdxId = 'MIT'
        ExpressionPattern = '(?<![A-Za-z0-9.-])MIT(?![A-Za-z0-9.-])'
        FileName = 'MIT.txt'
    },
    [pscustomobject]@{
        SpdxId = 'Apache-2.0'
        ExpressionPattern = '(?<![A-Za-z0-9.-])Apache-2\.0(?![A-Za-z0-9.-])'
        FileName = 'Apache-2.0.txt'
    }
)
$canonicalLicenseCount = 0
foreach ($mapping in $canonicalLicenseMappings) {
    $applicablePackages = @($packages | Where-Object {
        $_.LicenseType -eq 'expression' -and $_.License -match $mapping.ExpressionPattern
    })
    if ($applicablePackages.Count -eq 0) {
        continue
    }

    $canonicalLicensePath = Join-Path (Join-Path (Split-Path -Parent $PSScriptRoot) 'licenses\third-party') $mapping.FileName
    if (-not (Test-Path -LiteralPath $canonicalLicensePath -PathType Leaf)) {
        throw "Canonical SPDX license text $($mapping.SpdxId) is missing: $canonicalLicensePath"
    }

    $canonicalLicenseCount++
    $lines.Add("----- BEGIN SPDX $($mapping.SpdxId) License -----")
    $lines.Add("Applies to runtime packages declaring a license expression containing $($mapping.SpdxId):")
    foreach ($package in $applicablePackages) {
        $lines.Add("- $($package.Identity)")
    }
    $lines.Add('')
    $lines.Add((Get-Content -LiteralPath $canonicalLicensePath -Raw).TrimEnd())
    $lines.Add("----- END SPDX $($mapping.SpdxId) License -----")
    $lines.Add('')
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
[IO.File]::WriteAllText($OutputPath, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

Write-Output ("THIRD_PARTY_PACKAGE_COUNT=" + $packages.Count)
Write-Output ("THIRD_PARTY_LICENSE_DOCUMENT_COUNT=" + $licenseDocuments.Count)
Write-Output ("THIRD_PARTY_CANONICAL_LICENSE_COUNT=" + $canonicalLicenseCount)
Write-Output ("THIRD_PARTY_VERIFIED_ATTRIBUTION_COUNT=" + $includedAttributionCount)
Write-Output ("THIRD_PARTY_VERSIONED_LICENSE_COUNT=" + $versionedLicenseCount)
Write-Output ("THIRD_PARTY_NOTICES=" + (Resolve-Path -LiteralPath $OutputPath).Path)
