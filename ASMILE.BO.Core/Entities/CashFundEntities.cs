namespace ASMILE.BO.Core.Entities;

public sealed class CashFund : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public string FundName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public string? AccountCode { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<CashTransaction> Transactions { get; set; } = new List<CashTransaction>();
    public ICollection<CashCount> Counts { get; set; } = new List<CashCount>();
}

public sealed class CashTransaction : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CashFundId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public string? CounterAccountCode { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
    public byte[]? RowVersion { get; set; }
    public CashFund? CashFund { get; set; }
}

public sealed class CashCount : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CashFundId { get; set; }
    public string CountNo { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public decimal BookBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public CashFund? CashFund { get; set; }
    public ICollection<CashCountLine> Lines { get; set; } = new List<CashCountLine>();
}

public sealed class CashCountLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CashCountId { get; set; }
    public decimal Denomination { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public CashCount? CashCount { get; set; }
}


