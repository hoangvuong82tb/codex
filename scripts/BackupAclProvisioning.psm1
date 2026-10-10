Set-StrictMode -Version Latest

$script:BroadPrincipalSids = @(
    'S-1-1-0',        # Everyone
    'S-1-5-11',       # Authenticated Users
    'S-1-5-32-545'    # Builtin Users
)
$script:WriteRightsMask = [System.Security.AccessControl.FileSystemRights]::WriteData `
    -bor [System.Security.AccessControl.FileSystemRights]::AppendData `
    -bor [System.Security.AccessControl.FileSystemRights]::WriteAttributes `
    -bor [System.Security.AccessControl.FileSystemRights]::WriteExtendedAttributes `
    -bor [System.Security.AccessControl.FileSystemRights]::CreateFiles `
    -bor [System.Security.AccessControl.FileSystemRights]::CreateDirectories `
    -bor [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles `
    -bor [System.Security.AccessControl.FileSystemRights]::Delete `
    -bor [System.Security.AccessControl.FileSystemRights]::ChangePermissions `
    -bor [System.Security.AccessControl.FileSystemRights]::TakeOwnership

function Resolve-AsmileIdentitySid {
    [CmdletBinding()]
    param([Parameter(Mandatory)][System.Security.Principal.IdentityReference]$Identity)

    try {
        return $Identity.Translate([System.Security.Principal.SecurityIdentifier]).Value
    }
    catch [System.Security.Principal.IdentityNotMappedException] {
        return $null
    }
}

function Resolve-AsmileBackupDirectoryPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Path)

    $expandedPath = [Environment]::ExpandEnvironmentVariables($Path.Trim())
    if ($expandedPath.StartsWith('\\', [StringComparison]::Ordinal)) {
        throw 'Backup ACL target cannot be a UNC or device path.'
    }
    if ($expandedPath -notmatch '^[A-Za-z]:[\\/]') {
        throw "Backup ACL target must be a fully qualified local path: $expandedPath"
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($expandedPath)
    $root = [System.IO.Path]::GetPathRoot($resolvedPath)
    $trimmedResolvedPath = $resolvedPath.TrimEnd([char[]]@([char]92, [char]47))
    $trimmedRoot = $root.TrimEnd([char[]]@([char]92, [char]47))
    if ([string]::IsNullOrWhiteSpace($root) -or [string]::Equals($trimmedResolvedPath, $trimmedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to replace ACLs on a volume root: $resolvedPath"
    }

    $drive = [System.IO.DriveInfo]::new($root)
    if ($drive.DriveType -eq [System.IO.DriveType]::Network) {
        throw 'Backup ACL target must be on a local drive, not a mapped network drive.'
    }

    $currentPath = $root
    $relativePath = $resolvedPath.Substring($root.Length)
    foreach ($segment in ($relativePath -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $currentPath = [System.IO.Path]::Combine($currentPath, $segment)
        if (-not (Test-Path -LiteralPath $currentPath)) {
            break
        }

        $item = Get-Item -LiteralPath $currentPath -Force -ErrorAction Stop
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing backup ACL target beneath a reparse point: $currentPath"
        }
    }

    return $resolvedPath
}

function Test-AsmileBroadWriteRule {
    [CmdletBinding()]
    param([Parameter(Mandatory)][System.Security.AccessControl.FileSystemAccessRule]$Rule)

    if ($Rule.AccessControlType -ne [System.Security.AccessControl.AccessControlType]::Allow) {
        return $false
    }

    $sid = Resolve-AsmileIdentitySid -Identity $Rule.IdentityReference
    return $sid -in $script:BroadPrincipalSids `
        -and ($Rule.FileSystemRights -band $script:WriteRightsMask) -ne 0
}

function Protect-AsmileBackupDirectoryAcl {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Path,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string[]]$ManagedIdentities
    )

    $resolvedPath = Resolve-AsmileBackupDirectoryPath -Path $Path
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Container)) {
        throw "Backup ACL target is not a directory: $resolvedPath"
    }

    $identitySids = @{}
    foreach ($identity in $ManagedIdentities) {
        $account = [System.Security.Principal.NTAccount]::new($identity)
        $sid = Resolve-AsmileIdentitySid -Identity $account
        if ([string]::IsNullOrWhiteSpace($sid)) {
            throw "Could not resolve managed backup identity '$identity'."
        }
        if ($sid -in $script:BroadPrincipalSids) {
            throw "Refusing broad backup identity '$identity'; supply a named service account or operator group instead."
        }
        $identitySids[$sid] = $identity
    }

    $directory = [System.IO.DirectoryInfo]::new($resolvedPath)
    # Use PowerShell's ACL cmdlets instead of FileSystemAclExtensions. The
    # latter isn't available in Windows PowerShell's .NET Framework runtime,
    # which is the supported host for the elevated deployment script.
    # AccessControlSections.Access is intentional: changing the DACL must not
    # request SeSecurityPrivilege to read or write the directory's SACL.
    $accessSections = [System.Security.AccessControl.AccessControlSections]::Access
    $acl = $directory.GetAccessControl($accessSections)
    # Stop inheriting future broad write grants from ProgramData while keeping
    # existing unrelated entries. Broad read-only entries are preserved below.
    $acl.SetAccessRuleProtection($true, $true)
    $removedBroadWriteRules = 0
    foreach ($rule in @($acl.Access)) {
        if (-not (Test-AsmileBroadWriteRule -Rule $rule)) {
            continue
        }

        $acl.RemoveAccessRuleSpecific($rule)
        $removedBroadWriteRules++
        $readRights = $rule.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::ReadAndExecute
        if ($readRights -ne [System.Security.AccessControl.FileSystemRights]0) {
            $readOnlyRule = [System.Security.AccessControl.FileSystemAccessRule]::new(
                $rule.IdentityReference,
                $readRights,
                $rule.InheritanceFlags,
                $rule.PropagationFlags,
                $rule.AccessControlType)
            [void]$acl.AddAccessRule($readOnlyRule)
        }
    }

    foreach ($identity in $identitySids.GetEnumerator()) {
        $hasModify = @($acl.Access | Where-Object {
            $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow `
                -and (Resolve-AsmileIdentitySid -Identity $_.IdentityReference) -eq $identity.Key `
                -and (($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Modify) `
                    -eq [System.Security.AccessControl.FileSystemRights]::Modify) `
                -and ($_.InheritanceFlags -band [System.Security.AccessControl.InheritanceFlags]::ContainerInherit) `
                    -eq [System.Security.AccessControl.InheritanceFlags]::ContainerInherit `
                -and ($_.InheritanceFlags -band [System.Security.AccessControl.InheritanceFlags]::ObjectInherit) `
                    -eq [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
        }).Count -gt 0

        if (-not $hasModify) {
            $modifyRule = [System.Security.AccessControl.FileSystemAccessRule]::new(
                $identity.Value,
                [System.Security.AccessControl.FileSystemRights]::Modify,
                [System.Security.AccessControl.InheritanceFlags]::ContainerInherit `
                    -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit,
                [System.Security.AccessControl.PropagationFlags]::None,
                [System.Security.AccessControl.AccessControlType]::Allow)
            [void]$acl.AddAccessRule($modifyRule)
        }
    }

    $directory.SetAccessControl($acl)

    $verifiedAcl = $directory.GetAccessControl($accessSections)
    $remainingBroadWrites = @($verifiedAcl.Access | Where-Object { Test-AsmileBroadWriteRule -Rule $_ })
    if ($remainingBroadWrites.Count -gt 0) {
        throw "Broad principals still have write access to the backup directory: $($remainingBroadWrites.IdentityReference -join ', ')."
    }

    [pscustomobject]@{
        Path = $resolvedPath
        ManagedIdentities = @($identitySids.Values | Sort-Object -Unique)
        RemovedBroadWriteRules = $removedBroadWriteRules
        InheritanceProtected = $true
        BroadWriteRulesRemaining = 0
    }
}

Export-ModuleMember -Function Protect-AsmileBackupDirectoryAcl, Resolve-AsmileBackupDirectoryPath
