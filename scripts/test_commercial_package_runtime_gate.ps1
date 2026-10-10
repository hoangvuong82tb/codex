[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SelfContainedPublishDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'ASMILE.BO.App\ASMILE.BO.App.csproj'
$verifierPath = Join-Path $PSScriptRoot 'verify_commercial_package.ps1'
$verifiedPublish = (Resolve-Path -LiteralPath $SelfContainedPublishDirectory).Path
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$frameworkDependentPublish = Join-Path $tempRoot ('asmile-framework-dependent-gate-' + [guid]::NewGuid().ToString('N'))
$frameworkDependentFullPath = [System.IO.Path]::GetFullPath($frameworkDependentPublish)

if (-not $frameworkDependentFullPath.StartsWith($tempRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Framework-dependent test publish path escaped the temporary directory.'
}

& $verifierPath -PublishDirectory $verifiedPublish | Out-Host

try {
    & dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained false --no-restore --output $frameworkDependentPublish --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Framework-dependent regression publish failed with exit code $LASTEXITCODE."
    }

    $rejection = $null
    try {
        & $verifierPath -PublishDirectory $frameworkDependentFullPath | Out-Null
    }
    catch {
        $rejection = $_.Exception.Message
    }

    if ($rejection -notmatch 'PACKAGE_CHECK_FAILED: SelfContainedRuntime') {
        throw "Expected the verifier to reject a framework-dependent package at SelfContainedRuntime; got '$rejection'."
    }

    Write-Output 'FRAMEWORK_DEPENDENT_REJECTED=PASS'
    Write-Output "REJECTION=$rejection"
}
finally {
    if (Test-Path -LiteralPath $frameworkDependentFullPath -PathType Container) {
        $resolvedTemp = [System.IO.Path]::GetFullPath($tempRoot).TrimEnd([char[]]@('\', '/'))
        $resolvedTarget = (Resolve-Path -LiteralPath $frameworkDependentFullPath).Path
        if (-not $resolvedTarget.StartsWith($resolvedTemp + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing to remove regression output outside the temporary directory.'
        }
        Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
    }
}
