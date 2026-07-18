using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Core.Entities;

public sealed class AuditSession : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public long DurationMs { get; set; }
    public AuditSessionStatus Status { get; set; } = AuditSessionStatus.Started;
    public string Source { get; set; } = string.Empty;
    public string? ClientMachine { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceId { get; set; }
    public string? Platform { get; set; }
    public string? CompletedReason { get; set; }
    public string? SessionMetadataJson { get; set; }
}

public sealed class AuditCorrelation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = string.Empty;
    public string? ParentCorrelationId { get; set; }
    public Guid? AuditSessionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEntitySnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? AuditLogId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? DiffJson { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public byte[]? CompressedPayload { get; set; }
    public string CompressionType { get; set; } = "None";
    public long SnapshotSize { get; set; }
    public string HashAlgorithm { get; set; } = "SHA256";
    public AuditLog? AuditLog { get; set; }
}

public sealed class AuditReplayEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = string.Empty;
    public string? ParentCorrelationId { get; set; }
    public Guid? AuditSessionId { get; set; }
    public int SequenceNo { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditExportHistory : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RequestedByUserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string? FilterJson { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ExportHash { get; set; }
    public int RowCount { get; set; }
    public string HashAlgorithm { get; set; } = "SHA256";
    public string? CorrelationCoverageJson { get; set; }
    public string? IntegrityManifestJson { get; set; }
}
