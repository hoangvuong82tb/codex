namespace ASMILE.BO.Core.Entities;

public sealed class DataRetentionPolicy : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public int ArchiveAfterDays { get; set; }
    public int DeleteAfterDays { get; set; }
    public bool CompressArchivedPayload { get; set; }
    public bool EncryptArchive { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ArchiveBatch : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int ItemCount { get; set; }
    public long PayloadSizeBytes { get; set; }
    public string? Hash { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public ICollection<ArchiveBatchItem> Items { get; set; } = [];
}

public sealed class ArchiveBatchItem : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArchiveBatchId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Status { get; set; } = "Archived";
    public long PayloadSizeBytes { get; set; }
    public string? Hash { get; set; }
    public string? MetadataJson { get; set; }
    public string? PayloadJson { get; set; }
    public byte[]? CompressedPayload { get; set; }
    public string CompressionType { get; set; } = "None";
    public DateTime ArchivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public ArchiveBatch? ArchiveBatch { get; set; }
}

public sealed class RecoveryCheckpoint : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string? MigrationVersion { get; set; }
    public string? SchedulerStateJson { get; set; }
    public string? WorkspaceMetadataJson { get; set; }
    public string? AuditSessionMetadataJson { get; set; }
    public string? MetadataJson { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class RecoveryOperation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? RecoveryCheckpointId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? MetadataJson { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorMessage { get; set; }
    public RecoveryCheckpoint? RecoveryCheckpoint { get; set; }
}

public sealed class ArchiveExportManifest : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string? FilePath { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}
