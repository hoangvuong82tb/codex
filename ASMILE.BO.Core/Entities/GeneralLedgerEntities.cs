namespace ASMILE.BO.Core.Entities;

public sealed class LedgerAccount
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string? ParentAccountCode { get; set; }
    public int AccountLevel { get; set; }
    public string AccountKind { get; set; } = "D";
    public string AccountNature { get; set; } = "B";
    public string AccountType { get; set; } = string.Empty;
    public bool IsCashAccount { get; set; }
    public bool HasForeignCurrency { get; set; }
    public bool FollowWarehouse { get; set; } = true;
    public bool CustomerDebt { get; set; } = true;
    public bool SupplierDebt { get; set; } = true;
    public bool AllowFund { get; set; } = true;
    public string AccountingStandardCode { get; set; } = "TT99";
    public bool AllowPosting { get; set; } = true;
    public bool IsSystemAccount { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsDeletable { get; set; } = true;
    public string NormalBalance { get; set; } = "Debit";
    public string? AccountCategory { get; set; }
    public bool RequireCustomer { get; set; }
    public bool RequireSupplier { get; set; }
    public bool RequireWarehouse { get; set; }
    public bool RequireItem { get; set; }
    public bool RequireDepartment { get; set; }
    public bool RequireEmployee { get; set; }
    public bool RequireCurrency { get; set; }
    public bool IsFinancialAccount { get; set; } = true;
    public bool IsManagementAccount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class LedgerEntry
{
    public Guid Id { get; set; }
    public string EntryNo { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DebitAccount { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string ReferenceEntityType { get; set; } = string.Empty;
    public string ReferenceEntityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class LedgerBalance
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
}
