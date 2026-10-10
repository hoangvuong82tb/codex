namespace ASMILE.BO.Core.Security;

public enum SecurityEventType
{
    LoginSuccess,
    LoginFailed,
    OpenSettings,
    SaveSetting,
    TestConnection,
    RunMigration,
    RunMaintenance,
    BackupStarted,
    BackupCompleted,
    RestoreRequested,
    RestoreCompleted,
    RestoreFailed,
    RestartService,
    ExportDiagnostics,
    OpenSensitiveFolder,
    PrintDocument,
    ExportDocument,
    PermissionDenied,
    SearchExecuted,
    SecurityError,
    DangerousActionConfirmed,
    DangerousActionRejected,
    BackupFailed,
    BackupVerificationPassed,
    BackupVerificationFailed,
    UpdateStagingStarted,
    UpdateStagingCompleted,
    UpdateStagingFailed
}
