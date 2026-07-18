namespace ASMILE.BO.Application.DTOs;

public sealed class ReceivableFilterDto
{
    public string Keyword { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DebtStatus? Status { get; set; }
    public string Owner { get; set; } = string.Empty;
    public bool OverdueOnly { get; set; }
}

public sealed class ReceivableSummaryDto
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
    public DebtStatus Status { get; set; }
}

public sealed class ReceivableTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public ReceivableDocumentType DocumentType { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string CreatedBy { get; set; } = string.Empty;
    public DebtStatus Status { get; set; }
}

public sealed class CreatePaymentReceiptRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string BankAccount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
