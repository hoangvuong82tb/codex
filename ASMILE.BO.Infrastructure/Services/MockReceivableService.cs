using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Services;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockReceivableService : IReceivableService
{
    private readonly IAuditLogService _auditLogService;
    private readonly List<ReceivableTransactionDto> _transactions;
    private readonly string _branchCode;

    public MockReceivableService(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        _transactions = BuildSeed();
        _branchCode = ResolveBranchCode();
    }

    public Task<IReadOnlyList<ReceivableSummaryDto>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<ReceivableSummaryDto>)_transactions
            .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
            .Select(g => new ReceivableSummaryDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerCode = g.Key.CustomerCode,
                CustomerName = g.Key.CustomerName,
                Status = DebtStatus.Open
            })
            .OrderBy(x => x.CustomerName)
            .ToList());

    public Task<IReadOnlyList<string>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<string>)new[] { "Admin", "Kế toán 1", "Kế toán 2" }.ToList());

    public Task<IReadOnlyList<ReceivableSummaryDto>> GetDebtSummariesAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        return Task.FromResult<IReadOnlyList<ReceivableSummaryDto>>(query.GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
            .Select(g =>
            {
                var rows = g.OrderBy(x => x.TransactionDate).ToList();
                var debit = rows.Sum(x => x.DebitAmount);
                var credit = rows.Sum(x => x.CreditAmount);
                var overdue = rows.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today && x.BalanceAmount > 0).Sum(x => x.BalanceAmount);
                var closing = debit - credit;
                var status = overdue > 0 ? DebtStatus.Overdue : closing <= 0 ? DebtStatus.Paid : credit > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;
                return new ReceivableSummaryDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerCode = g.Key.CustomerCode,
                    CustomerName = g.Key.CustomerName,
                    OpeningBalance = rows.FirstOrDefault()?.BalanceAmount ?? 0,
                    TotalDebit = debit,
                    TotalCredit = credit,
                    ClosingBalance = closing,
                    OverdueAmount = overdue,
                    LastTransactionDate = rows.Max(x => x.TransactionDate),
                    Status = status
                };
            })
            .OrderByDescending(x => x.ClosingBalance)
            .ToList());
    }

    public Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReceivableTransactionDto>>(BuildQuery(filter).OrderByDescending(x => x.TransactionDate).ToList());

    public Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsByCustomerAsync(string customerId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _transactions.Where(x => x.CustomerId == customerId);
        if (fromDate.HasValue) query = query.Where(x => x.TransactionDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.TransactionDate.Date <= toDate.Value.Date);
        return Task.FromResult<IReadOnlyList<ReceivableTransactionDto>>(query.OrderByDescending(x => x.TransactionDate).ToList());
    }

    public Task<string> GetNextReceiptNoAsync(CancellationToken cancellationToken = default)
        => Task.FromResult($"PT-{_branchCode}{(_transactions.Count + 1):0000000}");

    public async Task<string> CreatePaymentReceiptAsync(CreatePaymentReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var customerRows = _transactions.Where(x => x.CustomerId == request.CustomerId).OrderBy(x => x.TransactionDate).ToList();
        if (customerRows.Count == 0)
        {
            return string.Empty;
        }

        var latest = customerRows.Last();
        var closingBefore = customerRows.Sum(x => x.DebitAmount - x.CreditAmount);
        _transactions.Add(new ReceivableTransactionDto
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerId = latest.CustomerId,
            CustomerCode = latest.CustomerCode,
            CustomerName = latest.CustomerName,
            DocumentNo = await GetNextReceiptNoAsync(cancellationToken),
            DocumentType = ReceivableDocumentType.PaymentReceipt,
            TransactionDate = request.ReceiptDate,
            DueDate = null,
            Description = string.IsNullOrWhiteSpace(request.Note) ? "Thu tien cong no" : request.Note,
            DebitAmount = 0,
            CreditAmount = request.Amount,
            BalanceAmount = Math.Max(0, closingBefore - request.Amount),
            Status = DebtStatus.PartiallyPaid
        });

        await _auditLogService.LogAsync("ReceivablePaymentCreated", "Finance.Receivable", $"Customer={latest.CustomerCode}; Amount={request.Amount:0.##}", cancellationToken);
        return _transactions.Last().DocumentNo;
    }

    public async Task<IReadOnlyList<ReceivableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetDebtSummariesAsync(new ReceivableFilterDto { OverdueOnly = true }, cancellationToken);
        return all.Where(x => x.OverdueAmount > 0).ToList();
    }

    public async Task PrintDebtStatementAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("ReceivablePrint", "Finance.Receivable", $"Customer={customerId}", cancellationToken);
    }

    public async Task ExportDebtStatementAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("ReceivableExport", "Finance.Receivable", $"Customer={customerId}; Path=D:/ACCOUNTING/Khach hang/{customerId}/Cong no", cancellationToken);
    }

    private IEnumerable<ReceivableTransactionDto> BuildQuery(ReceivableFilterDto filter)
    {
        var query = _transactions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.CustomerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue) query = query.Where(x => x.TransactionDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) query = query.Where(x => x.TransactionDate.Date <= filter.ToDate.Value.Date);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.OverdueOnly) query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today && x.BalanceAmount > 0);
        return query;
    }

    private static List<ReceivableTransactionDto> BuildSeed() =>
    [
        new() { Id = "ar1", CustomerId = "c1", CustomerCode = "KH001", CustomerName = "Cong ty An Phat", DocumentNo = "PX00018", DocumentType = ReceivableDocumentType.ExportVoucher, TransactionDate = DateTime.Today.AddDays(-20), DueDate = DateTime.Today.AddDays(-5), Description = "Ban hang dot 1", DebitAmount = 120_000_000, CreditAmount = 0, BalanceAmount = 120_000_000, Status = DebtStatus.Overdue },
        new() { Id = "ar2", CustomerId = "c1", CustomerCode = "KH001", CustomerName = "Cong ty An Phat", DocumentNo = "PT00001", DocumentType = ReceivableDocumentType.PaymentReceipt, TransactionDate = DateTime.Today.AddDays(-12), Description = "Thu tien lan 1", DebitAmount = 0, CreditAmount = 50_000_000, BalanceAmount = 70_000_000, Status = DebtStatus.PartiallyPaid },
        new() { Id = "ar3", CustomerId = "c2", CustomerCode = "KH002", CustomerName = "Cua hang Minh Tam", DocumentNo = "PX00021", DocumentType = ReceivableDocumentType.SalesInvoice, TransactionDate = DateTime.Today.AddDays(-10), DueDate = DateTime.Today.AddDays(10), Description = "Hoa don ban le", DebitAmount = 45_000_000, CreditAmount = 0, BalanceAmount = 45_000_000, Status = DebtStatus.Open },
        new() { Id = "ar4", CustomerId = "c3", CustomerCode = "KH003", CustomerName = "Cong ty Hoang Gia", DocumentNo = "PX00024", DocumentType = ReceivableDocumentType.ExportVoucher, TransactionDate = DateTime.Today.AddDays(-30), DueDate = DateTime.Today.AddDays(-1), Description = "Ban theo hop dong", DebitAmount = 80_000_000, CreditAmount = 0, BalanceAmount = 80_000_000, Status = DebtStatus.Overdue },
        new() { Id = "ar5", CustomerId = "c3", CustomerCode = "KH003", CustomerName = "Cong ty Hoang Gia", DocumentNo = "PT00008", DocumentType = ReceivableDocumentType.PaymentReceipt, TransactionDate = DateTime.Today.AddDays(-3), Description = "Thu 1 phan", DebitAmount = 0, CreditAmount = 20_000_000, BalanceAmount = 60_000_000, Status = DebtStatus.PartiallyPaid }
    ];

    private static string ResolveBranchCode()
    {
        var raw = Environment.GetEnvironmentVariable("ASMILE_BRANCH_CODE");
        var cleaned = new string((raw ?? "HN").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "HN";
        }

        return cleaned.Length > 4 ? cleaned[..4] : cleaned;
    }
}
