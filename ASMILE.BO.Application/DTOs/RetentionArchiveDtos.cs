namespace ASMILE.BO.Application.DTOs;

public sealed class DataRetentionPolicyDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public int ArchiveAfterDays { get; set; }
    public int DeleteAfterDays { get; set; }
    public bool CompressArchivedPayload { get; set; }
    public bool EncryptArchive { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ArchiveBatchDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public long PayloadSizeBytes { get; set; }
    public string? Hash { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class ArchiveBatchItemDto
{
    public Guid Id { get; set; }
    public Guid ArchiveBatchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long PayloadSizeBytes { get; set; }
    public string? Hash { get; set; }
    public string? MetadataJson { get; set; }
    public string? PayloadJson { get; set; }
    public byte[]? CompressedPayload { get; set; }
    public string CompressionType { get; set; } = string.Empty;
    public DateTime ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class RecoveryCheckpointDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MigrationVersion { get; set; }
    public string? SchedulerStateJson { get; set; }
    public string? WorkspaceMetadataJson { get; set; }
    public string? AuditSessionMetadataJson { get; set; }
    public string? MetadataJson { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class RecoveryOperationDto
{
    public Guid Id { get; set; }
    public Guid? RecoveryCheckpointId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ArchiveExportManifestDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? Hash { get; set; }
    public long PayloadSizeBytes { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class RetentionPreviewDto
{
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int ExpiredCount { get; set; }
    public long EstimatedPayloadSizeBytes { get; set; }
    public int PolicyCount { get; set; }
}

public sealed class ArchiveIntegrityResultDto
{
    public Guid ArchiveBatchId { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Hash { get; set; }
    public int ItemCount { get; set; }
}

public sealed class RecoveryTimelineEntryDto
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorMessage { get; set; }
}
