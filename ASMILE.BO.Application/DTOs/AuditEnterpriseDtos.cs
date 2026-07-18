using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Application.DTOs;

public sealed class AuditSessionDto
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public long DurationMs { get; set; }
    public AuditSessionStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ClientMachine { get; set; }
    public string? AppVersion { get; set; }
    public string? DeviceId { get; set; }
    public string? Platform { get; set; }
    public string? CompletedReason { get; set; }
    public string? SessionMetadataJson { get; set; }
}

public sealed class AuditCorrelationDto
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ParentCorrelationId { get; set; }
    public Guid? AuditSessionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AuditEntitySnapshotDto
{
    public Guid Id { get; set; }
    public int? AuditLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? DiffJson { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public string CompressionType { get; set; } = "None";
    public long SnapshotSize { get; set; }
    public string HashAlgorithm { get; set; } = "SHA256";
    public bool HashVerified { get; set; }
    public bool HashMismatchDetected { get; set; }
}

public sealed class AuditReplayEntryDto
{
    public Guid Id { get; set; }
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
    public DateTime TimestampUtc { get; set; }
}

public sealed class AuditReplaySummaryDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<string> RouteTrail { get; set; } = [];
    public IReadOnlyList<AuditReplayEntryDto> Entries { get; set; } = [];
}

public sealed class AuditExportHistoryDto
{
    public Guid Id { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string? FilterJson { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? ExportHash { get; set; }
    public int RowCount { get; set; }
    public string HashAlgorithm { get; set; } = "SHA256";
    public string? CorrelationCoverageJson { get; set; }
    public string? IntegrityManifestJson { get; set; }
}

public sealed class AuditExportRequestDto
{
    public string ExportType { get; set; } = "Json";
    public string? CorrelationId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? FilterJson { get; set; }
    public string? FilePath { get; set; }
}

public sealed class AuditExportResultDto
{
    public Guid ExportHistoryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string? ExportHash { get; set; }
    public string? IntegrityManifestJson { get; set; }
}

public sealed class AuditTimelineNodeDto
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ParentCorrelationId { get; set; }
    public int SequenceNo { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

public sealed class AuditReplayStateFrameDto
{
    public int SequenceNo { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? StateJson { get; set; }
    public DateTime TimestampUtc { get; set; }
}

public sealed class AuditReplayDiffResultDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public IReadOnlyList<AuditTimelineNodeDto> Timeline { get; set; } = [];
    public IReadOnlyList<AuditReplayStateFrameDto> StateFrames { get; set; } = [];
    public IReadOnlyList<AuditEntitySnapshotDto> Snapshots { get; set; } = [];
}
