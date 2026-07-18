namespace ASMILE.BO.Application.DTOs;

public sealed class PayableFilterDto
{
    public string Keyword { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DebtStatus? Status { get; set; }
    public bool OverdueOnly { get; set; }
}

public sealed class PayableSummaryDto
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal OverdueAmount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public DebtStatus Status { get; set; }
}

public sealed class PayableTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public PayableDocumentType DocumentType { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DebtStatus Status { get; set; }
}

public sealed class CreatePaymentVoucherRequest
{
    public string SupplierId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public string BankAccount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public sealed class CreateAdvancePaymentRequest
{
    public string SupplierId { get; set; } = string.Empty;
    public DateTime AdvanceDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
