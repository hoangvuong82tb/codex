namespace ASMILE.BO.Core.Entities;

public sealed class AccountBalanceSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class FinancialStatementExecution : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string StatementType { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? JsonData { get; set; }
}

public sealed class ClosingEntry : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public int FiscalYear { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ClosingEntryLine> Lines { get; set; } = new List<ClosingEntryLine>();
}

public sealed class ClosingEntryLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ClosingEntryId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public ClosingEntry? ClosingEntry { get; set; }
}
