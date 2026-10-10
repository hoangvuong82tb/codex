# Provision the SQL Server backup directory

ASMILE's `.bak` files are written by the SQL Server database engine, not by the desktop application process. The desktop application also needs access to list, verify, restore, and apply retention to those files. A successful path check by the application does not prove the SQL Server service can write there.

## Local SQL Server instance

Open an elevated PowerShell session on the SQL Server host and run a dry run first:

```powershell
.\scripts\provision_sql_backup_directory.ps1 `
  -InstanceName SQLEXPRESS `
  -ApplicationIdentity 'DOMAIN\AsmileOperators' `
  -WhatIf
```

After confirming the instance, account, and destination, rerun without `-WhatIf`:

```powershell
.\scripts\provision_sql_backup_directory.ps1 `
  -InstanceName SQLEXPRESS `
  -ApplicationIdentity 'DOMAIN\AsmileOperators'
```

The default destination is `%ProgramData%\ASMILE-BO\Backups`. The script stops inheriting permissions at the backup directory, preserves existing unrelated entries (including broad read-only access), removes write access for `Everyone`, `Authenticated Users`, and `BUILTIN\Users`, then grants `Modify` only to the SQL Server service identity discovered from Service Control Manager and the explicitly supplied application identity. It changes only the target directory DACL; it does not alter the parent `%ProgramData%` ACL or the audit SACL. Review the `-WhatIf` output and the listed service/application identities before applying. For the default SQL Server instance, use `-InstanceName MSSQLSERVER`; for another named instance, provide its instance name. Use `-BackupDirectory` to choose a different local folder.

Set `Backup.BackupPath` in System Settings to exactly the provisioned destination, then run a real backup and verify it before enabling scheduled backups in production.

## SQL Server on another machine / SMB share

Use a UNC path in System Settings. Provision NTFS permissions on the file server for the SQL Server service identity and application identity, and separately grant the required SMB share permissions. The script above intentionally rejects UNC paths because it cannot safely configure both permission layers for a remote share.

## Operational checks

- Do not grant `Everyone` or broad `Users` write access to a backup folder.
- Run the provisioner elevated on the SQL Server host. After applying it, inspect the target ACL and test a real backup under the SQL Server service identity before enabling scheduled backups.
- Confirm that the SQL Server service identity can write and the application identity can read/write/delete for backup verification, restore staging, and retention.
- Keep backups outside the database data directory where practical; protect the destination and include it in an independent off-host backup plan.
- A successful LocalDB integration test does not verify permissions for a SQL Server service account or a remote SMB share.
