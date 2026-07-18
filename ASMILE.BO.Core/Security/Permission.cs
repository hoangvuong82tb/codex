namespace ASMILE.BO.Core.Security;

public enum Permission
{
    ViewSystemSettings,
    EditSystemSettings,
    ManageDatabase,
    RunMigration,
    BackupDatabase,
    RestoreDatabase,
    ManageUsers,
    ViewAuditLogs,
    ManageServices,
    ExportData,
    PrintDocuments,
    OpenSensitiveFolder
}
