using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Services;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockPayableService : IPayableService
{
    private readonly IAuditLogService _auditLogService;
    private readonly List<PayableTransactionDto> _transactions;
    private readonly string _branchCode;
    private readonly Dictionary<string, bool> _approvalMap = new(StringComparer.OrdinalIgnoreCase);

    public MockPayableService(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        _transactions = BuildSeed();
        _branchCode = ResolveBranchCode();
    }

    public Task<IReadOnlyList<PayableSummaryDto>> GetDebtSummariesAsync(PayableFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _transactions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.SupplierCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.SupplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue) query = query.Where(x => x.TransactionDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) query = query.Where(x => x.TransactionDate.Date <= filter.ToDate.Value.Date);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.OverdueOnly) query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today && x.BalanceAmount > 0);

        var result = query.GroupBy(x => new { x.SupplierId, x.SupplierCode, x.SupplierName })
            .Select(g =>
            {
                var rows = g.OrderBy(x => x.TransactionDate).ToList();
                var debit = rows.Sum(x => x.DebitAmount);
                var credit = rows.Sum(x => x.CreditAmount);
                var closing = credit - debit;
                var overdue = rows.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today && x.BalanceAmount > 0).Sum(x => x.BalanceAmount);
                var status = overdue > 0 ? DebtStatus.Overdue : closing <= 0 ? DebtStatus.Paid : debit > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;

                return new PayableSummaryDto
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierCode = g.Key.SupplierCode,
                    SupplierName = g.Key.SupplierName,
                    OpeningBalance = rows.FirstOrDefault()?.BalanceAmount ?? 0,
                    TotalPayable = credit,
                    TotalPaid = debit,
                    ClosingBalance = closing,
                    OverdueAmount = overdue,
                    LastTransactionDate = rows.Max(x => x.TransactionDate),
                    Status = status
                };
            })
            .OrderByDescending(x => x.ClosingBalance)
            .ToList();

        return Task.FromResult<IReadOnlyList<PayableSummaryDto>>(result);
    }

    public Task<IReadOnlyList<PayableTransactionDto>> GetTransactionsBySupplierAsync(string supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _transactions.Where(x => x.SupplierId == supplierId);
        if (fromDate.HasValue) query = query.Where(x => x.TransactionDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.TransactionDate.Date <= toDate.Value.Date);
        return Task.FromResult<IReadOnlyList<PayableTransactionDto>>(query.OrderByDescending(x => x.TransactionDate).ToList());
    }

    public Task<string> GetNextPaymentVoucherNoAsync(CancellationToken cancellationToken = default)
        => Task.FromResult($"PC-{_branchCode}{(_transactions.Count + 1):0000000}");

    public async Task<string> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default)
    {
        var supplierRows = _transactions.Where(x => x.SupplierId == request.SupplierId).OrderBy(x => x.TransactionDate).ToList();
        if (supplierRows.Count == 0)
        {
            return string.Empty;
        }

        var latest = supplierRows.Last();
        var payableBefore = supplierRows.Sum(x => x.CreditAmount - x.DebitAmount);
        var docNo = await GetNextPaymentVoucherNoAsync(cancellationToken);
        _transactions.Add(new PayableTransactionDto
        {
            Id = Guid.NewGuid().ToString("N"),
            SupplierId = latest.SupplierId,
            SupplierCode = latest.SupplierCode,
            SupplierName = latest.SupplierName,
            DocumentNo = docNo,
            DocumentType = PayableDocumentType.PaymentVoucher,
            TransactionDate = request.PaymentDate,
            Description = string.IsNullOrWhiteSpace(request.Note) ? "Thanh toan cong no" : request.Note,
            DebitAmount = request.Amount,
            CreditAmount = 0,
            BalanceAmount = Math.Max(0, payableBefore - request.Amount),
            Status = DebtStatus.PartiallyPaid
        });

        await _auditLogService.LogAsync("PayablePaymentCreated", "Finance.Payable", $"Supplier={latest.SupplierCode}; Amount={request.Amount:0.##}", cancellationToken);
        _approvalMap[docNo] = false;
        return _transactions.Last().DocumentNo;
    }

    public Task<bool?> GetPaymentVoucherApprovedAsync(string documentNo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentNo))
            return Task.FromResult<bool?>(null);

        if (_approvalMap.TryGetValue(documentNo.Trim(), out var approved))
            return Task.FromResult<bool?>(approved);

        var existing = _transactions.FirstOrDefault(x =>
            x.DocumentType == PayableDocumentType.PaymentVoucher &&
            string.Equals(x.DocumentNo, documentNo.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return Task.FromResult<bool?>(null);

        return Task.FromResult<bool?>(true);
    }

    public Task SetPaymentVoucherApprovedAsync(string documentNo, bool approved, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(documentNo))
        {
            _approvalMap[documentNo.Trim()] = approved;
        }
        return Task.CompletedTask;
    }

    public async Task CreateAdvancePaymentAsync(CreateAdvancePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var supplierRows = _transactions.Where(x => x.SupplierId == request.SupplierId).OrderBy(x => x.TransactionDate).ToList();
        if (supplierRows.Count == 0)
        {
            return;
        }

        var latest = supplierRows.Last();
        _transactions.Add(new PayableTransactionDto
        {
            Id = Guid.NewGuid().ToString("N"),
            SupplierId = latest.SupplierId,
            SupplierCode = latest.SupplierCode,
            SupplierName = latest.SupplierName,
            DocumentNo = $"TU{DateTime.Now:yyMMddHHmm}",
            DocumentType = PayableDocumentType.AdvancePayment,
            TransactionDate = request.AdvanceDate,
            Description = string.IsNullOrWhiteSpace(request.Note) ? "Tam ung NCC" : request.Note,
            DebitAmount = request.Amount,
            CreditAmount = 0,
            BalanceAmount = Math.Max(0, latest.BalanceAmount - request.Amount),
            Status = DebtStatus.PartiallyPaid
        });

        await _auditLogService.LogAsync("PayableAdvanceCreated", "Finance.Payable", $"Supplier={latest.SupplierCode}; Amount={request.Amount:0.##}", cancellationToken);
    }

    public async Task<IReadOnlyList<PayableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetDebtSummariesAsync(new PayableFilterDto { OverdueOnly = true }, cancellationToken);
        return all.Where(x => x.OverdueAmount > 0).ToList();
    }

    public async Task PrintDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("PayablePrint", "Finance.Payable", $"Supplier={supplierId}", cancellationToken);
    }

    public async Task ExportDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("PayableExport", "Finance.Payable", $"Supplier={supplierId}; Path=D:/ACCOUNTING/Nha cung cap/{supplierId}/Cong no", cancellationToken);
    }

    private static List<PayableTransactionDto> BuildSeed() =>
    [
        new() { Id = "ap1", SupplierId = "s1", SupplierCode = "NCC001", SupplierName = "Cong ty ABC", DocumentNo = "PN00024", DocumentType = PayableDocumentType.ImportVoucher, TransactionDate = DateTime.Today.AddDays(-18), DueDate = DateTime.Today.AddDays(-3), Description = "Nhap hang thang", DebitAmount = 0, CreditAmount = 135_000_000, BalanceAmount = 135_000_000, Status = DebtStatus.Overdue },
        new() { Id = "ap2", SupplierId = "s1", SupplierCode = "NCC001", SupplierName = "Cong ty ABC", DocumentNo = "UNC00012", DocumentType = PayableDocumentType.PaymentVoucher, TransactionDate = DateTime.Today.AddDays(-9), Description = "Thanh toan lan 1", DebitAmount = 45_000_000, CreditAmount = 0, BalanceAmount = 90_000_000, Status = DebtStatus.PartiallyPaid },
        new() { Id = "ap3", SupplierId = "s2", SupplierCode = "NCC002", SupplierName = "Cong ty Minh Phat", DocumentNo = "PN00028", DocumentType = PayableDocumentType.PurchaseInvoice, TransactionDate = DateTime.Today.AddDays(-11), DueDate = DateTime.Today.AddDays(7), Description = "Hoa don mua vat tu", DebitAmount = 0, CreditAmount = 62_000_000, BalanceAmount = 62_000_000, Status = DebtStatus.Open },
        new() { Id = "ap4", SupplierId = "s3", SupplierCode = "NCC003", SupplierName = "Cong ty An Khang", DocumentNo = "PN00030", DocumentType = PayableDocumentType.ImportVoucher, TransactionDate = DateTime.Today.AddDays(-27), DueDate = DateTime.Today.AddDays(-2), Description = "Nhap bo sung", DebitAmount = 0, CreditAmount = 80_000_000, BalanceAmount = 80_000_000, Status = DebtStatus.Overdue },
        new() { Id = "ap5", SupplierId = "s3", SupplierCode = "NCC003", SupplierName = "Cong ty An Khang", DocumentNo = "TU00004", DocumentType = PayableDocumentType.AdvancePayment, TransactionDate = DateTime.Today.AddDays(-5), Description = "Tam ung dot 1", DebitAmount = 20_000_000, CreditAmount = 0, BalanceAmount = 60_000_000, Status = DebtStatus.PartiallyPaid }
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
