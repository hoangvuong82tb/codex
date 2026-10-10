[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PublishDirectory,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$MakensisPath,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$SevenZipPath
)

$ErrorActionPreference = 'Stop'
$builder = Join-Path $PSScriptRoot 'build_commercial_installer.ps1'
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory -ErrorAction Stop).Path
$compilerPath = (Resolve-Path -LiteralPath $MakensisPath -ErrorAction Stop).Path
$sevenZipFullPath = (Resolve-Path -LiteralPath $SevenZipPath -ErrorAction Stop).Path
$syntheticThumbprint = '0000000000000000000000000000000000000000'

$matchingCertificate = @(
    Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -CodeSigningCert -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $syntheticThumbprint -and $_.HasPrivateKey }
)
if ($matchingCertificate.Count -gt 0) {
    throw 'The synthetic signing thumbprint unexpectedly exists; refusing to execute the production-gate test.'
}

$outputPath = Join-Path ([System.IO.Path]::GetTempPath()) ('asmile-production-gate-' + [guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $outputPath) {
    throw "Unique production-gate test path already exists: $outputPath"
}

$expectedFailure = $false
try {
    & $builder `
        -PublishDirectory $publishPath `
        -OutputDirectory $outputPath `
        -Version '1.0.0.0' `
        -Publisher 'ASMILE BO' `
        -MakensisPath $compilerPath `
        -SevenZipPath $sevenZipFullPath `
        -Production `
        -SigningCertificateThumbprint $syntheticThumbprint `
        -TimestampUrl 'https://timestamp.example.invalid'
}
catch {
    $expectedFailure = $_.Exception.Message -like '*code-signing certificate with private key was not found*'
    if (-not $expectedFailure) {
        throw
    }
}

if (-not $expectedFailure) {
    throw 'Production build did not fail closed on the missing signing certificate.'
}
if (Test-Path -LiteralPath $outputPath) {
    throw "Production preflight created output despite missing certificate: $outputPath"
}

Write-Output 'INSTALLER_PRODUCTION_GATE=PASS'
Write-Output 'MISSING_SIGNING_CERTIFICATE=REJECTED'
Write-Output 'OUTPUT_DIRECTORY_CREATED=False'
