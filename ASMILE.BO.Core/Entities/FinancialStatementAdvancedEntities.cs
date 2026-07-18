namespace ASMILE.BO.Core.Entities;

public sealed class FinancialTemplate : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string StandardCode { get; set; } = "TT99";
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatementType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
    public ICollection<FinancialTemplateVersion> Versions { get; set; } = new List<FinancialTemplateVersion>();
}

public sealed class FinancialTemplateVersion : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FinancialTemplateId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public bool IsActiveVersion { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveToUtc { get; set; }
    public string? HierarchyJson { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public FinancialTemplate? FinancialTemplate { get; set; }
    public ICollection<FinancialStatementFormula> Formulas { get; set; } = new List<FinancialStatementFormula>();
}

public sealed class FinancialStatementFormula : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FinancialTemplateVersionId { get; set; }
    public string LineCode { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public FinancialTemplateVersion? FinancialTemplateVersion { get; set; }
}

public sealed class FinancialStatementSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FinancialTemplateVersionId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = "{}";
    public string SnapshotHash { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public FinancialTemplateVersion? FinancialTemplateVersion { get; set; }
}
