namespace ASMILE.BO.Application.DTOs;

public sealed class CashFundDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; }
}

public sealed class CashFundRequest
{
    public string FundCode { get; set; } = string.Empty;
    public string FundName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public string? AccountCode { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CashTransactionDto
{
    public Guid Id { get; set; }
    public Guid CashFundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public string FundName { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
}

public sealed class CashTransactionRequest
{
    public Guid FundId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public string? CounterAccountCode { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class CashBookEntryDto
{
    public DateTime Date { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string? SourceType { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class CashCountDto
{
    public Guid Id { get; set; }
    public Guid CashFundId { get; set; }
    public string CountNo { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public decimal BookBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public IReadOnlyList<CashCountLineDto> Lines { get; set; } = Array.Empty<CashCountLineDto>();
}

public sealed class CashCountLineDto
{
    public Guid Id { get; set; }
    public decimal Denomination { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

public sealed class CashCountRequest
{
    public Guid CashFundId { get; set; }
    public DateTime CountDate { get; set; } = DateTime.Today;
    public IReadOnlyList<CashCountLineDto> Lines { get; set; } = Array.Empty<CashCountLineDto>();
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public string? CounterAccountCode { get; set; }
}

public sealed class CashSummaryDto
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalReceipts { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ClosingBalance { get; set; }
    public int PostedTransactionCount { get; set; }
}



