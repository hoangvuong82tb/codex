namespace ASMILE.BO.Core.Entities;

public sealed class DataIntegrityCheckpoint : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CheckpointCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string SnapshotHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = "{}";
}

public sealed class DataIntegrityScanResult : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public bool IsCorrupted { get; set; }
    public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
}

public sealed class DataRepairOperation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string? RequestedBy { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}
