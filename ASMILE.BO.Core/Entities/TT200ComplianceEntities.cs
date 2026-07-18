namespace ASMILE.BO.Core.Entities;

public sealed class TT200AccountMapping : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string TT200Category { get; set; } = string.Empty;
    public string TT200ReportSection { get; set; } = string.Empty;
    public string TT200ClosingGroup { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class TT200ReportTemplate : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
    public ICollection<TT200ReportLine> Lines { get; set; } = [];
}

public sealed class TT200ReportLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TT200ReportTemplateId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string LineCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Formula { get; set; }
    public string? MappingJson { get; set; }
    public TT200ReportTemplate? Template { get; set; }
}

public sealed class TT200ValidationRule : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Module { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TT200ComplianceIssue : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Module { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AffectedEntity { get; set; }
    public string? MetadataJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TT200DeclarationPeriod : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime FromDateUtc { get; set; }
    public DateTime ToDateUtc { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class TT200ComplianceSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public string DeclarationStatus { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TT200ExportManifest : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string PeriodCode { get; set; } = string.Empty;
    public string? ExportedBy { get; set; }
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public string Hash { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string ComplianceVersion { get; set; } = "TT200.v1";
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}
