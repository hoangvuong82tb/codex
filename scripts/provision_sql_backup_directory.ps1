[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $InstanceName = 'SQLEXPRESS',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $BackupDirectory = (Join-Path $env:ProgramData 'ASMILE-BO\Backups'),

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ApplicationIdentity
)

$ErrorActionPreference = 'Stop'
$aclProvisioningModule = Join-Path $PSScriptRoot 'BackupAclProvisioning.psm1'
if (-not (Test-Path -LiteralPath $aclProvisioningModule -PathType Leaf)) {
    throw "Backup ACL provisioning module is missing: $aclProvisioningModule"
}
Import-Module -Name $aclProvisioningModule -Force -ErrorAction Stop

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $WhatIfPreference -and -not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this provisioning script from an elevated PowerShell session.'
}

$resolvedPath = Resolve-AsmileBackupDirectoryPath -Path $BackupDirectory

$serviceName = if ([string]::IsNullOrWhiteSpace($InstanceName) -or $InstanceName -eq 'MSSQLSERVER') {
    'MSSQLSERVER'
} else {
    "MSSQL`$$InstanceName"
}
$service = Get-CimInstance -ClassName Win32_Service -Filter "Name='$serviceName'" -ErrorAction Stop
if ($null -eq $service) {
    throw "SQL Server engine service '$serviceName' was not found. Verify InstanceName and run this script on the SQL Server host."
}

$accounts = @($service.StartName, $ApplicationIdentity) |
    ForEach-Object { $_.Trim() } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique

$aclResult = $null
if ($PSCmdlet.ShouldProcess($resolvedPath, "Protect backup ACL and grant Modify to $($accounts -join ', ')")) {
    New-Item -ItemType Directory -Path $resolvedPath -Force -ErrorAction Stop | Out-Null
    $aclResult = Protect-AsmileBackupDirectoryAcl -Path $resolvedPath -ManagedIdentities $accounts
}

[pscustomobject]@{
    SqlService       = $service.Name
    SqlServiceAccount = $service.StartName
    ApplicationIdentity = $ApplicationIdentity
    BackupDirectory  = $resolvedPath
    RemovedBroadWriteRules = if ($aclResult) { $aclResult.RemovedBroadWriteRules } else { 0 }
    BroadWriteRulesRemaining = if ($aclResult) { $aclResult.BroadWriteRulesRemaining } else { 'Not evaluated by WhatIf' }
    Action           = if ($WhatIfPreference) { 'WhatIf: no ACL changes made' } else { 'Directory and ACL provisioned' }
}
