namespace ASMILE.BO.Application.DTOs;

public sealed class LedgerFilterDto
{
    public string Keyword { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public LedgerDocumentType? DocumentType { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class LedgerAccountDto
{
    public string Id { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string? ParentAccountCode { get; set; }
    public int AccountLevel { get; set; }
    public string AccountKind { get; set; } = "D";
    public string AccountNature { get; set; } = "B";
    public LedgerAccountType AccountType { get; set; }
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

public sealed class LedgerEntryDto
{
    public string Id { get; set; } = string.Empty;
    public Guid? JournalId { get; set; }
    public Guid? JournalLineId { get; set; }
    public string EntryNo { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public LedgerDocumentType DocumentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DebitAccount { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsLockedPeriod { get; set; }
    public string Currency { get; set; } = "VND";
}

public sealed class LedgerBalanceDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public int AccountLevel { get; set; }
    public bool AllowPosting { get; set; } = true;
    public string AccountingStandardCode { get; set; } = "TT99";
    public bool IsActive { get; set; } = true;
}

public sealed class CreateLedgerEntryRequest
{
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public string DocumentNo { get; set; } = string.Empty;
    public LedgerDocumentType DocumentType { get; set; } = LedgerDocumentType.Adjustment;
    public string Description { get; set; } = string.Empty;
    public string DebitAccount { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
