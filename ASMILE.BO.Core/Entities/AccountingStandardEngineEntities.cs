namespace ASMILE.BO.Core.Entities;

public sealed class AccountingStandardTemplate
{
    public Guid Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AccountTemplate
{
    public Guid Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public int AccountLevel { get; set; }
    public string? AccountKind { get; set; }
    public string NormalBalance { get; set; } = "Debit";
    public string AccountCategory { get; set; } = string.Empty;
    public bool AllowPosting { get; set; } = true;
    public bool IsSystemAccount { get; set; } = true;
    public bool IsFinancialAccount { get; set; } = true;
    public bool IsManagementAccount { get; set; }
    public bool RequireCustomer { get; set; }
    public bool RequireSupplier { get; set; }
    public bool RequireWarehouse { get; set; }
    public bool RequireItem { get; set; }
    public bool RequireDepartment { get; set; }
    public bool RequireEmployee { get; set; }
    public bool RequireCurrency { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FinancialStatementMapping : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string ReportCode { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public string? AccountFilter { get; set; }
    public string? AccountRoleFilter { get; set; }
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CompanyAccountingProfile : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string AccountingStandardCode { get; set; } = "TT99";
    public int FiscalYearStartMonth { get; set; } = 1;
    public int FiscalYearStartDay { get; set; } = 1;
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
