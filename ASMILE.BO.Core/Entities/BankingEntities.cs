namespace ASMILE.BO.Core.Entities;

public sealed class BankAccount : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public string? AccountCode { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    public ICollection<BankStatement> Statements { get; set; } = new List<BankStatement>();
}

public sealed class BankTransaction : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BankAccountId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? CounterAccountCode { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
    public byte[]? RowVersion { get; set; }
    public BankAccount? BankAccount { get; set; }
}

public sealed class BankStatement : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BankAccountId { get; set; }
    public string StatementNo { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }
    public string Status { get; set; } = "Draft";
    public BankAccount? BankAccount { get; set; }
    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}

public sealed class BankStatementLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BankStatementId { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Balance { get; set; }
    public Guid? MatchedBankTransactionId { get; set; }
    public string MatchStatus { get; set; } = "Unmatched";
    public BankStatement? BankStatement { get; set; }
}

public sealed class BankReconciliation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BankAccountId { get; set; }
    public string ReconciliationNo { get; set; } = string.Empty;
    public DateTime ReconciliationDate { get; set; }
    public decimal BookBalance { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public ICollection<BankReconciliationLine> Lines { get; set; } = new List<BankReconciliationLine>();
}

public sealed class BankReconciliationLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BankReconciliationId { get; set; }
    public Guid? BankTransactionId { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string? Note { get; set; }
    public BankReconciliation? BankReconciliation { get; set; }
}
