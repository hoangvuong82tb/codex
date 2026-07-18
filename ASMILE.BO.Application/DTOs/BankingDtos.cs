namespace ASMILE.BO.Application.DTOs;

public sealed class BankAccountDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; }
}

public sealed class BankAccountRequest
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public string? AccountCode { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BankTransactionDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
}

public sealed class BankTransactionRequest
{
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public DateTime? ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public string? CounterAccountCode { get; set; }
    public string? PartnerType { get; set; }
    public Guid? PartnerId { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class BankStatementDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public string StatementNo { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<BankStatementLineDto> Lines { get; set; } = Array.Empty<BankStatementLineDto>();
}

public sealed class BankStatementLineDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Balance { get; set; }
    public Guid? MatchedBankTransactionId { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
}

public sealed class BankReconciliationDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public string ReconciliationNo { get; set; } = string.Empty;
    public DateTime ReconciliationDate { get; set; }
    public decimal BookBalance { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
}

public sealed class BankReconciliationLineDto
{
    public Guid Id { get; set; }
    public Guid BankReconciliationId { get; set; }
    public Guid? BankTransactionId { get; set; }
    public string? BankTransactionNo { get; set; }
    public Guid? BankStatementLineId { get; set; }
    public DateTime? StatementDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string MatchStatus => BankTransactionId.HasValue && DifferenceAmount == 0 ? "Matched" : "Unmatched";
    public string? Note { get; set; }
}

public sealed class BankBookEntryDto
{
    public DateTime Date { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string? SourceType { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BankSummaryDto
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalReceipts { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ClosingBalance { get; set; }
    public int PostedTransactionCount { get; set; }
}
