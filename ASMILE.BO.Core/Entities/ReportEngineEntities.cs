namespace ASMILE.BO.Core.Entities;

public sealed class ReportTemplate : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string? QueryDefinition { get; set; }
    public string? LayoutDefinition { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ReportDefinition> Definitions { get; set; } = new List<ReportDefinition>();
    public ICollection<ReportParameter> Parameters { get; set; } = new List<ReportParameter>();
}

public sealed class ReportDefinition : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid ReportTemplateId { get; set; }
    public int VersionNo { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public ReportTemplate? ReportTemplate { get; set; }
}

public sealed class ReportParameter : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid ReportTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public ReportTemplate? ReportTemplate { get; set; }
}

public sealed class ReportExecution : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExecutedBy { get; set; }
}
