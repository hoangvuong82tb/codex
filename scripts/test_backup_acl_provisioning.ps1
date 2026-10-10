[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot 'BackupAclProvisioning.psm1'
Import-Module -Name $modulePath -Force -ErrorAction Stop

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/'))
$testDirectory = Join-Path $tempRoot ('asmile-backup-acl-test-' + [guid]::NewGuid().ToString('N'))
$testFullPath = [System.IO.Path]::GetFullPath($testDirectory)
if (-not $testFullPath.StartsWith($tempRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'ACL test directory escaped the temporary directory.'
}

[void](New-Item -ItemType Directory -Path $testFullPath)
try {
    $volumeRoot = [System.IO.Path]::GetPathRoot($testFullPath)
    $rootRejected = $false
    try {
        Resolve-AsmileBackupDirectoryPath -Path $volumeRoot | Out-Null
    }
    catch {
        $rootRejected = $_.Exception.Message -like '*volume root*'
    }
    if (-not $rootRejected) {
        throw 'The path validator did not reject a volume root.'
    }

    $relativeRejected = $false
    try {
        Resolve-AsmileBackupDirectoryPath -Path '.\relative-backup' | Out-Null
    }
    catch {
        $relativeRejected = $_.Exception.Message -like '*fully qualified local path*'
    }
    if (-not $relativeRejected) {
        throw 'The path validator did not reject a relative path.'
    }

    $uncRejected = $false
    try {
        Resolve-AsmileBackupDirectoryPath -Path '\\server\share\backup' | Out-Null
    }
    catch {
        $uncRejected = $_.Exception.Message -like '*UNC or device path*'
    }
    if (-not $uncRejected) {
        throw 'The path validator did not reject a UNC path.'
    }

    $reparseTarget = Join-Path $testFullPath 'reparse-target'
    $reparsePath = Join-Path $testFullPath 'reparse-link'
    [void](New-Item -ItemType Directory -Path $reparseTarget)
    [void](New-Item -ItemType Junction -Path $reparsePath -Target $reparseTarget)
    $reparseRejected = $false
    try {
        Resolve-AsmileBackupDirectoryPath -Path (Join-Path $reparsePath 'backups') | Out-Null
    }
    catch {
        $reparseRejected = $_.Exception.Message -like '*reparse point*'
    }
    if (-not $reparseRejected) {
        throw 'The path validator did not reject a path beneath a junction.'
    }
    if (Test-Path -LiteralPath (Join-Path $reparseTarget 'backups')) {
        throw 'Path validation unexpectedly created a target through a junction.'
    }
    Remove-Item -LiteralPath $reparsePath -Force

    $broadSids = @('S-1-1-0', 'S-1-5-11', 'S-1-5-32-545')
    $acl = Get-Acl -LiteralPath $testFullPath
    foreach ($sidValue in $broadSids) {
        $sid = [System.Security.Principal.SecurityIdentifier]::new($sidValue)
        $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
            $sid,
            [System.Security.AccessControl.FileSystemRights]::Modify,
            [System.Security.AccessControl.InheritanceFlags]::ContainerInherit `
                -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow)
        [void]$acl.AddAccessRule($rule)
    }
    Set-Acl -LiteralPath $testFullPath -AclObject $acl

    $currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    $managedIdentities = [System.Collections.Generic.List[string]]::new()
    $managedIdentities.Add($currentIdentity)
    $sqlService = Get-CimInstance -ClassName Win32_Service -Filter "Name='MSSQL`$SQLEXPRESS'" -ErrorAction SilentlyContinue
    if ($sqlService -and -not [string]::IsNullOrWhiteSpace($sqlService.StartName)) {
        $managedIdentities.Add($sqlService.StartName)
    }
    $managedIdentities = @($managedIdentities | Select-Object -Unique)
    $result = Protect-AsmileBackupDirectoryAcl -Path $testFullPath -ManagedIdentities $managedIdentities
    if ($result.RemovedBroadWriteRules -lt 3) {
        throw "Expected to remove at least three broad write rules, removed $($result.RemovedBroadWriteRules)."
    }
    if ($result.BroadWriteRulesRemaining -ne 0) {
        throw 'Broad write access remained after ACL provisioning.'
    }

    $finalAcl = Get-Acl -LiteralPath $testFullPath
    if (-not $finalAcl.AreAccessRulesProtected) {
        throw 'Backup ACL still inherits permissions from its parent.'
    }

    $writeRightsMask = [System.Security.AccessControl.FileSystemRights]::WriteData `
        -bor [System.Security.AccessControl.FileSystemRights]::AppendData `
        -bor [System.Security.AccessControl.FileSystemRights]::WriteAttributes `
        -bor [System.Security.AccessControl.FileSystemRights]::WriteExtendedAttributes `
        -bor [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles `
        -bor [System.Security.AccessControl.FileSystemRights]::Delete `
        -bor [System.Security.AccessControl.FileSystemRights]::ChangePermissions `
        -bor [System.Security.AccessControl.FileSystemRights]::TakeOwnership
    $remainingBroad = @($finalAcl.Access | Where-Object {
        $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow `
            -and $_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value -in $broadSids `
            -and ($_.FileSystemRights -band $writeRightsMask) -ne 0
    })
    if ($remainingBroad.Count -gt 0) {
        throw 'The test directory still grants Modify to a broad principal.'
    }
    foreach ($sidValue in $broadSids) {
        $broadRead = @($finalAcl.Access | Where-Object {
            $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow `
                -and $_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value -eq $sidValue `
                -and ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::ReadAndExecute) `
                    -eq [System.Security.AccessControl.FileSystemRights]::ReadAndExecute
        })
        if ($broadRead.Count -eq 0) {
            throw "The provisioner did not preserve read access for broad principal '$sidValue'."
        }
    }

    foreach ($identity in $managedIdentities) {
        $managedSid = ([System.Security.Principal.NTAccount]::new($identity)).Translate(
            [System.Security.Principal.SecurityIdentifier]).Value
        $managedModify = @($finalAcl.Access | Where-Object {
            $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow `
                -and $_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value -eq $managedSid `
                -and ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Modify) `
                    -eq [System.Security.AccessControl.FileSystemRights]::Modify
        })
        if ($managedModify.Count -eq 0) {
            throw "The managed identity '$identity' did not receive Modify access."
        }
    }

    $broadIdentityRejected = $false
    try {
        Protect-AsmileBackupDirectoryAcl -Path $testFullPath -ManagedIdentities @('Everyone') | Out-Null
    }
    catch {
        $broadIdentityRejected = $_.Exception.Message -like '*Refusing broad backup identity*'
    }
    if (-not $broadIdentityRejected) {
        throw 'The provisioner did not reject a broad application identity.'
    }

    $again = Protect-AsmileBackupDirectoryAcl -Path $testFullPath -ManagedIdentities $managedIdentities
    if ($again.RemovedBroadWriteRules -ne 0 -or $again.BroadWriteRulesRemaining -ne 0) {
        throw 'ACL provisioning is not idempotent.'
    }

    Write-Output 'BACKUP_ACL_POLICY=PASS'
    Write-Output 'BACKUP_PATH_ROOT_REJECTED=True'
    Write-Output 'BACKUP_PATH_RELATIVE_REJECTED=True'
    Write-Output 'BACKUP_PATH_UNC_REJECTED=True'
    Write-Output 'BACKUP_PATH_REPARSE_REJECTED=True'
    Write-Output 'BACKUP_PATH_REPARSE_TARGET_UNTOUCHED=True'
    Write-Output "REMOVED_BROAD_WRITE_RULES=$($result.RemovedBroadWriteRules)"
    Write-Output 'ACL_PROVISIONING_IDEMPOTENT=True'
    Write-Output 'MANAGED_IDENTITY_MODIFY=True'
    Write-Output "MANAGED_IDENTITY_COUNT=$($managedIdentities.Count)"
    Write-Output 'BROAD_READ_PRESERVED=True'
    Write-Output 'BROAD_IDENTITY_REJECTED=True'
}
finally {
    if (Test-Path -LiteralPath $testFullPath -PathType Container) {
        Remove-Item -LiteralPath $testFullPath -Recurse -Force
    }
}
