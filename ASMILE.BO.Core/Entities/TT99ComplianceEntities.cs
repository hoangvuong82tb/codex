namespace ASMILE.BO.Core.Entities;

public sealed class TT99AccountMapping : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string TT99Category { get; set; } = string.Empty;
    public string TT99ReportSection { get; set; } = string.Empty;
    public string TT99DeclarationGroup { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class TT99ReportTemplate : ICompanyScoped, IBranchScoped
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
    public ICollection<TT99ReportLine> Lines { get; set; } = [];
}

public sealed class TT99ReportLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TT99ReportTemplateId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string LineCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Formula { get; set; }
    public string? MappingJson { get; set; }
    public TT99ReportTemplate? Template { get; set; }
}

public sealed class TT99ValidationRule : ICompanyScoped, IBranchScoped
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

public sealed class TT99ComplianceIssue : ICompanyScoped, IBranchScoped
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

public sealed class TT99DeclarationPeriod : ICompanyScoped, IBranchScoped
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

public sealed class TT99ComplianceSnapshot : ICompanyScoped, IBranchScoped
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

public sealed class TT99ExportManifest : ICompanyScoped, IBranchScoped
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
    public string ComplianceVersion { get; set; } = "TT99.v1";
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}
