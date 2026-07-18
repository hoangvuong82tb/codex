namespace ASMILE.BO.Core.Entities;

public sealed class ReconciliationSession : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ReconciliationMatch : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid SessionId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public decimal MatchScore { get; set; }
    public bool IsFuzzy { get; set; }
    public string Status { get; set; } = "Matched";
    public string? MetadataJson { get; set; }
}

public sealed class ReconciliationRule : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsFuzzy { get; set; }
    public bool IsActive { get; set; } = true;
    public string DefinitionJson { get; set; } = "{}";
}

public sealed class ReconciliationSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = "{}";
    public string SnapshotHash { get; set; } = string.Empty;
}

public sealed class ReconciliationAnomaly : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid SessionId { get; set; }
    public string Severity { get; set; } = "Warning";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
}
