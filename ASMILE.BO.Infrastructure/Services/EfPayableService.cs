using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EfPayableService : IPayableService
{
    private sealed class PayableDocRow
    {
        public string SupplierCode { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public BusinessDocumentType DocumentType { get; init; }
        public DateTime DocumentDate { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "VND";
    }

    private static readonly BusinessDocumentType[] PayableCreditTypes =
    [
        BusinessDocumentType.ImportVoucher,
        BusinessDocumentType.Invoice
    ];

    private static readonly BusinessDocumentType[] PayableDebitTypes =
    [
        BusinessDocumentType.PaymentVoucher,
        BusinessDocumentType.AdvancePayment
    ];

    private static string NormalizeKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly string _branchCode;
    private readonly SemaphoreSlim _dbOperationGate = new(1, 1);

    public EfPayableService(AppDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _branchCode = ResolveBranchCode();
    }

    public async Task<IReadOnlyList<PayableSummaryDto>> GetDebtSummariesAsync(PayableFilterDto filter, CancellationToken cancellationToken = default)
    {
        await _dbOperationGate.WaitAsync(cancellationToken);
        try
        {
        if (await _dbContext.PayableTransactions.AsNoTracking().AnyAsync(cancellationToken))
        {
            return await GetPersistedDebtSummariesAsync(filter, cancellationToken);
        }

        var suppliers = await _dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .ToListAsync(cancellationToken);

        var docsQuery = _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && (PayableCreditTypes.Contains(x.DocumentType) ||
                            (PayableDebitTypes.Contains(x.DocumentType) &&
                             (x.Status == DocumentStatus.Active || x.Status == DocumentStatus.Locked))));

        if (filter.FromDate.HasValue)
            docsQuery = docsQuery.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            docsQuery = docsQuery.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);

        var docs = await docsQuery.ToListAsync(cancellationToken);

        var receiptQuery = _dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved);
        if (filter.FromDate.HasValue)
            receiptQuery = receiptQuery.Where(x => x.ReceiptDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            receiptQuery = receiptQuery.Where(x => x.ReceiptDate.Date <= filter.ToDate.Value.Date);

        var approvedReceipts = await receiptQuery
            .Select(x => new
            {
                x.ReceiptNo,
                x.ReceiptDate,
                x.SupplierCode,
                x.SupplierName,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var importVoucherCodesInBusinessDocs = docs
            .Where(x => x.DocumentType == BusinessDocumentType.ImportVoucher)
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => NormalizeKey(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keyword = filter.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            suppliers = suppliers.Where(x =>
                    x.SupplierCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.SupplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    docs.Any(d => d.EntityId == ToEntityGuid(x.Id) && d.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    approvedReceipts.Any(r =>
                        (string.Equals(NormalizeKey(r.SupplierCode), NormalizeKey(x.SupplierCode), StringComparison.OrdinalIgnoreCase)
                         || string.Equals(NormalizeKey(r.SupplierName), NormalizeKey(x.SupplierName), StringComparison.OrdinalIgnoreCase))
                        && r.ReceiptNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var rows = new List<PayableSummaryDto>(suppliers.Count);
        foreach (var supplier in suppliers)
        {
            var supplierGuid = ToEntityGuid(supplier.Id);
            var supplierDocs = docs
                .Where(x => x.EntityId == supplierGuid)
                .Select(x => new PayableDocRow
                {
                    SupplierCode = supplier.SupplierCode,
                    SupplierName = supplier.SupplierName,
                    Code = x.Code,
                    Title = string.IsNullOrWhiteSpace(x.Title) ? x.Code : x.Title,
                    DocumentType = x.DocumentType,
                    DocumentDate = x.DocumentDate,
                    Amount = x.Amount,
                    Currency = x.Currency
                })
                .ToList();

            // Bổ sung phiếu nhập đã duyệt chưa được đồng bộ vào BusinessDocuments
            // để công nợ phải trả luôn phản ánh đúng dữ liệu thực tế phiếu nhập.
            var supplierCodeKey = NormalizeKey(supplier.SupplierCode);
            var supplierNameKey = NormalizeKey(supplier.SupplierName);

            var supplierReceiptDocs = approvedReceipts
                .Where(r =>
                    (string.Equals(NormalizeKey(r.SupplierCode), supplierCodeKey, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(NormalizeKey(r.SupplierName), supplierNameKey, StringComparison.OrdinalIgnoreCase))
                    && !importVoucherCodesInBusinessDocs.Contains(NormalizeKey(r.ReceiptNo)))
                .Select(r => new PayableDocRow
                {
                    SupplierCode = supplier.SupplierCode,
                    SupplierName = supplier.SupplierName,
                    Code = r.ReceiptNo,
                    Title = $"Phiếu nhập {r.ReceiptNo}",
                    DocumentType = BusinessDocumentType.ImportVoucher,
                    DocumentDate = r.ReceiptDate,
                    Amount = r.TotalAmount,
                    Currency = "VND"
                });

            var allSupplierDocs = supplierDocs
                .Concat(supplierReceiptDocs)
                .OrderBy(x => x.DocumentDate)
                .ThenBy(x => x.Code)
                .ToList();

            var credit = allSupplierDocs.Where(x => PayableCreditTypes.Contains(x.DocumentType)).Sum(x => x.Amount);
            var debit = allSupplierDocs.Where(x => PayableDebitTypes.Contains(x.DocumentType)).Sum(x => x.Amount);
            var closing = credit - debit;
            var overdue = closing > 0 && allSupplierDocs.Any(x => PayableCreditTypes.Contains(x.DocumentType) && x.DocumentDate.Date < DateTime.Today.AddDays(-30))
                ? closing
                : 0m;

            var status = overdue > 0
                ? DebtStatus.Overdue
                : closing <= 0
                    ? DebtStatus.Paid
                    : debit > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;

            if (filter.Status.HasValue && status != filter.Status.Value)
                continue;
            if (filter.OverdueOnly && overdue <= 0)
                continue;

            rows.Add(new PayableSummaryDto
            {
                SupplierId = supplier.Id.ToString(),
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.SupplierName,
                OpeningBalance = 0,
                TotalPayable = credit,
                TotalPaid = debit,
                ClosingBalance = closing,
                OverdueAmount = overdue,
                LastTransactionDate = allSupplierDocs.LastOrDefault()?.DocumentDate,
                Status = status
            });
        }

        return rows.OrderByDescending(x => x.ClosingBalance).ToList();
        }
        finally
        {
            _dbOperationGate.Release();
        }
    }

    public async Task<IReadOnlyList<PayableTransactionDto>> GetTransactionsBySupplierAsync(string supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        await _dbOperationGate.WaitAsync(cancellationToken);
        try
        {
        if (await _dbContext.PayableTransactions.AsNoTracking().AnyAsync(cancellationToken))
        {
            return await GetPersistedTransactionsBySupplierAsync(supplierId, fromDate, toDate, cancellationToken);
        }

        if (!int.TryParse(supplierId, out var supplierPk))
            return [];

        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == supplierPk, cancellationToken);
        if (supplier is null) return [];

        var supplierGuid = ToEntityGuid(supplierPk);
        var query = _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && x.EntityId == supplierGuid
                        && (PayableCreditTypes.Contains(x.DocumentType) || PayableDebitTypes.Contains(x.DocumentType)));

        if (fromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);

        var docs = await query.OrderBy(x => x.DocumentDate).ThenBy(x => x.Code).ToListAsync(cancellationToken);

        var importVoucherCodesInBusinessDocs = docs
            .Where(x => x.DocumentType == BusinessDocumentType.ImportVoucher)
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => NormalizeKey(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var receiptQuery = _dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved);
        if (fromDate.HasValue)
            receiptQuery = receiptQuery.Where(x => x.ReceiptDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            receiptQuery = receiptQuery.Where(x => x.ReceiptDate.Date <= toDate.Value.Date);

        var supplierCodeKeyTx = NormalizeKey(supplier.SupplierCode);
        var supplierNameKeyTx = NormalizeKey(supplier.SupplierName);

        var approvedReceipts = await receiptQuery
            .Select(x => new
            {
                x.ReceiptNo,
                x.ReceiptDate,
                x.SupplierCode,
                x.SupplierName,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        approvedReceipts = approvedReceipts
            .Where(r =>
                string.Equals(NormalizeKey(r.SupplierCode), supplierCodeKeyTx, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeKey(r.SupplierName), supplierNameKeyTx, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allDocs = docs
            .Select(x => new PayableDocRow
            {
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.SupplierName,
                Code = x.Code,
                Title = string.IsNullOrWhiteSpace(x.Title) ? x.Code : x.Title,
                DocumentType = x.DocumentType,
                DocumentDate = x.DocumentDate,
                Amount = x.Amount,
                Currency = x.Currency
            })
            .Concat(approvedReceipts
                .Where(r => !importVoucherCodesInBusinessDocs.Contains(NormalizeKey(r.ReceiptNo)))
                .Select(r => new PayableDocRow
                {
                    SupplierCode = supplier.SupplierCode,
                    SupplierName = supplier.SupplierName,
                    Code = r.ReceiptNo,
                    Title = $"Phiếu nhập {r.ReceiptNo}",
                    DocumentType = BusinessDocumentType.ImportVoucher,
                    DocumentDate = r.ReceiptDate,
                    Amount = r.TotalAmount,
                    Currency = "VND"
                }))
            .OrderBy(x => x.DocumentDate)
            .ThenBy(x => x.Code)
            .ToList();

        var running = 0m;
        var result = new List<PayableTransactionDto>(allDocs.Count);
        foreach (var doc in allDocs)
        {
            var credit = PayableCreditTypes.Contains(doc.DocumentType) ? doc.Amount : 0m;
            var debit = PayableDebitTypes.Contains(doc.DocumentType) ? doc.Amount : 0m;
            running += credit - debit;

            var status = running <= 0
                ? DebtStatus.Paid
                : doc.DocumentDate.Date < DateTime.Today.AddDays(-30) ? DebtStatus.Overdue : DebtStatus.PartiallyPaid;

            result.Add(new PayableTransactionDto
            {
                Id = GuidUtility.Create(GuidUtility.UrlNamespace, $"payable:{supplier.Id}:{doc.Code}:{doc.DocumentDate:yyyyMMdd}").ToString("N"),
                SupplierId = supplier.Id.ToString(),
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.SupplierName,
                DocumentNo = doc.Code,
                DocumentType = MapPayableDocumentType(doc.DocumentType),
                TransactionDate = doc.DocumentDate,
                DueDate = null,
                Description = string.IsNullOrWhiteSpace(doc.Title) ? doc.Code : doc.Title,
                DebitAmount = debit,
                CreditAmount = credit,
                BalanceAmount = Math.Max(0, running),
                Currency = doc.Currency,
                Status = status
            });
        }

        return result.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
        }
        finally
        {
            _dbOperationGate.Release();
        }
    }

    public async Task<string> GetNextPaymentVoucherNoAsync(CancellationToken cancellationToken = default)
        => await GenerateNextCodeAsync("PC", _branchCode, "payment-voucher-no", cancellationToken);

    public async Task<string> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(request.SupplierId, out var supplierPk))
            return string.Empty;

        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierPk, cancellationToken);
        if (supplier is null)
            return string.Empty;

        var code = await GenerateNextCodeAsync("PC", _branchCode, "payment-voucher-no", cancellationToken);
        var doc = new BusinessDocument
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = string.IsNullOrWhiteSpace(request.Note) ? "Phiếu chi công nợ" : request.Note,
            Module = BusinessModule.Accounting,
            EntityType = BusinessEntityType.Supplier,
            EntityId = ToEntityGuid(supplier.Id),
            DocumentType = BusinessDocumentType.PaymentVoucher,
            DocumentDate = request.PaymentDate,
            Amount = request.Amount,
            Currency = "VND",
            VirtualPath = $@"D:\ACCOUNTING\Nhà cung cấp\{supplier.SupplierName}\04. Thanh toán - Tạm ứng\{request.PaymentDate:yyyy}\{request.PaymentDate:MM}",
            PhysicalFilePath = string.Empty,
            FileName = $"{code}.txt",
            FileExtension = ".txt",
            FileSize = 0,
            Status = DocumentStatus.Draft,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.BusinessDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("PayablePaymentCreated", "Finance.Payable", $"Supplier={supplier.SupplierCode}; Amount={request.Amount:N0}", cancellationToken);
        return code;
    }

    public async Task<bool?> GetPaymentVoucherApprovedAsync(string documentNo, CancellationToken cancellationToken = default)
    {
        await _dbOperationGate.WaitAsync(cancellationToken);
        try
        {
        if (string.IsNullOrWhiteSpace(documentNo))
            return null;

        var doc = await _dbContext.BusinessDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DocumentType == BusinessDocumentType.PaymentVoucher &&
                     x.Code == documentNo.Trim(),
                cancellationToken);

        if (doc is null)
            return null;

        return doc.Status == DocumentStatus.Active || doc.Status == DocumentStatus.Locked;
        }
        finally
        {
            _dbOperationGate.Release();
        }
    }

    public async Task SetPaymentVoucherApprovedAsync(string documentNo, bool approved, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentNo))
            return;

        var doc = await _dbContext.BusinessDocuments
            .FirstOrDefaultAsync(
                x => x.DocumentType == BusinessDocumentType.PaymentVoucher &&
                     x.Code == documentNo.Trim(),
                cancellationToken);

        if (doc is null)
            return;

        doc.Status = approved ? DocumentStatus.Active : DocumentStatus.Draft;
        doc.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync(
            approved ? "PayablePaymentApproved" : "PayablePaymentPending",
            "Finance.Payable",
            $"Document={doc.Code}",
            cancellationToken);
    }

    public async Task CreateAdvancePaymentAsync(CreateAdvancePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(request.SupplierId, out var supplierPk))
            return;

        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierPk, cancellationToken);
        if (supplier is null)
            return;

        var code = $"TU-{DateTime.Now:yyyyMMddHHmmss}";
        var doc = new BusinessDocument
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = string.IsNullOrWhiteSpace(request.Note) ? "Tạm ứng nhà cung cấp" : request.Note,
            Module = BusinessModule.Accounting,
            EntityType = BusinessEntityType.Supplier,
            EntityId = ToEntityGuid(supplier.Id),
            DocumentType = BusinessDocumentType.AdvancePayment,
            DocumentDate = request.AdvanceDate,
            Amount = request.Amount,
            Currency = "VND",
            VirtualPath = $@"D:\ACCOUNTING\Nhà cung cấp\{supplier.SupplierName}\04. Thanh toán - Tạm ứng\{request.AdvanceDate:yyyy}\{request.AdvanceDate:MM}",
            PhysicalFilePath = string.Empty,
            FileName = $"{code}.txt",
            FileExtension = ".txt",
            FileSize = 0,
            Status = DocumentStatus.Active,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.BusinessDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("PayableAdvanceCreated", "Finance.Payable", $"Supplier={supplier.SupplierCode}; Amount={request.Amount:N0}", cancellationToken);
    }

    public async Task<IReadOnlyList<PayableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default)
        => await GetDebtSummariesAsync(new PayableFilterDto { OverdueOnly = true }, cancellationToken);

    public Task PrintDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default)
        => _auditLogService.LogAsync("PayablePrint", "Finance.Payable", $"Supplier={supplierId}", cancellationToken);

    public Task ExportDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default)
        => _auditLogService.LogAsync("PayableExport", "Finance.Payable", $"Supplier={supplierId}", cancellationToken);

    private async Task<IReadOnlyList<PayableSummaryDto>> GetPersistedDebtSummariesAsync(PayableFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.PayableTransactions.AsNoTracking().AsQueryable();
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);

        var transactions = await query.ToListAsync(cancellationToken);
        var supplierIds = transactions.Select(x => DecodeEntityInt(x.SupplierId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var suppliers = await _dbContext.Suppliers.AsNoTracking()
            .Where(x => supplierIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var keyword = filter.Keyword?.Trim();
        var rows = new List<PayableSummaryDto>();
        foreach (var group in transactions.GroupBy(x => x.SupplierId))
        {
            var supplierPk = DecodeEntityInt(group.Key);
            suppliers.TryGetValue(supplierPk ?? 0, out var supplier);
            var supplierCode = supplier?.SupplierCode ?? group.Key.ToString("N");
            var supplierName = supplier?.SupplierName ?? supplierCode;

            if (!string.IsNullOrWhiteSpace(keyword)
                && !supplierCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !supplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !group.Any(x => x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var totalPayable = group.Sum(x => x.CreditAmount);
            var totalPaid = group.Sum(x => x.DebitAmount);
            var closing = totalPayable - totalPaid;
            var overdue = group.Where(x => x.BalanceAmount > 0 && x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today).Sum(x => x.BalanceAmount);
            var status = ToDebtStatus(closing, overdue, totalPaid);

            if (filter.Status.HasValue && status != filter.Status.Value)
                continue;
            if (filter.OverdueOnly && overdue <= 0)
                continue;

            rows.Add(new PayableSummaryDto
            {
                SupplierId = supplierPk?.ToString() ?? group.Key.ToString("N"),
                SupplierCode = supplierCode,
                SupplierName = supplierName,
                OpeningBalance = 0,
                TotalPayable = totalPayable,
                TotalPaid = totalPaid,
                ClosingBalance = closing,
                OverdueAmount = overdue,
                LastTransactionDate = group.Max(x => (DateTime?)x.DocumentDate),
                Status = status
            });
        }

        return rows.OrderByDescending(x => x.ClosingBalance).ToList();
    }

    private async Task<IReadOnlyList<PayableTransactionDto>> GetPersistedTransactionsBySupplierAsync(string supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        if (!int.TryParse(supplierId, out var supplierPk))
            return [];

        var supplierGuid = PostingSourceIds.FromInt32(supplierPk);
        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == supplierPk, cancellationToken);
        if (supplier is null)
            return [];

        var query = _dbContext.PayableTransactions.AsNoTracking().Where(x => x.SupplierId == supplierGuid);
        if (fromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);

        var transactions = await query.OrderBy(x => x.DocumentDate).ThenBy(x => x.DocumentNo).ToListAsync(cancellationToken);
        var running = 0m;
        var rows = new List<PayableTransactionDto>(transactions.Count);
        foreach (var transaction in transactions)
        {
            running += transaction.CreditAmount - transaction.DebitAmount;
            rows.Add(new PayableTransactionDto
            {
                Id = transaction.Id.ToString("N"),
                SupplierId = supplier.Id.ToString(),
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.SupplierName,
                DocumentNo = transaction.DocumentNo,
                DocumentType = MapPayableDocumentType(transaction.SourceType),
                TransactionDate = transaction.DocumentDate,
                DueDate = transaction.DueDate,
                Description = transaction.Description ?? transaction.DocumentNo,
                DebitAmount = transaction.DebitAmount,
                CreditAmount = transaction.CreditAmount,
                BalanceAmount = Math.Max(0, running),
                Currency = transaction.CurrencyCode,
                Status = MapDebtStatus(transaction.Status, Math.Max(0, running), transaction.DueDate)
            });
        }

        return rows.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
    }

    private static Guid ToEntityGuid(int id) => GuidUtility.Create(GuidUtility.UrlNamespace, $"supplier:{id}");

    private static int? DecodeEntityInt(Guid id)
        => PostingSourceIds.TryToInt32(id, out var value) ? value : null;

    private async Task<string> GenerateNextCodeAsync(string prefix, string branchCode, string lockName, CancellationToken cancellationToken)
    {
        var isSqlServer = _dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
        if (isSqlServer)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000",
                new object[] { $"asmile:{lockName}:{branchCode}" });

            var next = await GetNextSequenceCoreAsync(prefix, branchCode, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return BuildCode(prefix, branchCode, next);
        }

        var fallbackNext = await GetNextSequenceCoreAsync(prefix, branchCode, cancellationToken);
        return BuildCode(prefix, branchCode, fallbackNext);
    }

    private async Task<int> GetNextSequenceCoreAsync(string prefix, string branchCode, CancellationToken cancellationToken)
    {
        var regex = new Regex($@"^{Regex.Escape(prefix)}-{Regex.Escape(branchCode)}(?<seq>\d{{7}})$", RegexOptions.IgnoreCase);
        var codes = await _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.Code.StartsWith($"{prefix}-{branchCode}"))
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;

            var match = regex.Match(code.Trim());
            if (!match.Success)
                continue;

            if (int.TryParse(match.Groups["seq"].Value, out var seq))
            {
                max = Math.Max(max, seq);
            }
        }

        return max + 1;
    }

    private static string BuildCode(string prefix, string branchCode, int sequence)
    {
        return $"{prefix}-{branchCode}{sequence:0000000}";
    }

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

    private static PayableDocumentType MapPayableDocumentType(BusinessDocumentType documentType) => documentType switch
    {
        BusinessDocumentType.Invoice => PayableDocumentType.PurchaseInvoice,
        BusinessDocumentType.ImportVoucher => PayableDocumentType.ImportVoucher,
        BusinessDocumentType.PaymentVoucher => PayableDocumentType.PaymentVoucher,
        BusinessDocumentType.AdvancePayment => PayableDocumentType.AdvancePayment,
        _ => PayableDocumentType.Adjustment
    };

    private static PayableDocumentType MapPayableDocumentType(string sourceType) => sourceType switch
    {
        "StockReceipt" => PayableDocumentType.ImportVoucher,
        "PaymentVoucher" => PayableDocumentType.PaymentVoucher,
        "AdvancePayment" => PayableDocumentType.AdvancePayment,
        _ => PayableDocumentType.Adjustment
    };

    private static DebtStatus ToDebtStatus(decimal closing, decimal overdue, decimal paid)
    {
        if (overdue > 0)
            return DebtStatus.Overdue;
        if (closing <= 0)
            return DebtStatus.Paid;
        return paid > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;
    }

    private static DebtStatus MapDebtStatus(string status, decimal balance, DateTime? dueDate)
    {
        if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return DebtStatus.Cancelled;
        if (balance <= 0 || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
            return DebtStatus.Paid;
        if (dueDate.HasValue && dueDate.Value.Date < DateTime.Today)
            return DebtStatus.Overdue;
        if (string.Equals(status, "PartiallyPaid", StringComparison.OrdinalIgnoreCase))
            return DebtStatus.PartiallyPaid;
        return DebtStatus.Open;
    }
}
