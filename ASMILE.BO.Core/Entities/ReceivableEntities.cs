namespace ASMILE.BO.Core.Entities;

public sealed class ReceivableTransaction : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CustomerId { get; set; }
    /// <summary>Optional hotel property scope for Front Office-originated receivables.</summary>
    public Guid? PropertyId { get; set; }
    /// <summary>Optional hotel folio source for Front Office-originated receivables.</summary>
    public Guid? FolioId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public Guid? JournalLineId { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CustomerDebtSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal OverdueAmount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

public sealed class PaymentReceipt
{
    public Guid Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
