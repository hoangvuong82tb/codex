namespace ASMILE.BO.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseMigrationMode MigrationMode { get; set; } = DatabaseMigrationMode.Disabled;
    public bool AllowApplyPendingMigrations { get; set; }
    public bool FailOnPendingMigrations { get; set; } = true;
    public string BackupDirectory { get; set; } = "Backups";
    public bool AllowDatabaseRestore { get; set; }
}

public enum DatabaseMigrationMode
{
    Disabled,
    ValidateOnly,
    ApplyPendingMigrations,
    DevelopmentAutoMigrate
}

