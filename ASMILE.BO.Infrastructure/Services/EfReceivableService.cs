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

public sealed class EfReceivableService : IReceivableService
{
    private static readonly BusinessDocumentType[] ReceivableDebitTypes =
    [
        BusinessDocumentType.ExportVoucher,
        BusinessDocumentType.Invoice
    ];

    private static readonly BusinessDocumentType[] ReceivableCreditTypes =
    [
        BusinessDocumentType.PaymentVoucher,
        BusinessDocumentType.AdvancePayment
    ];

    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly string _branchCode;

    public EfReceivableService(AppDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _branchCode = ResolveBranchCode();
    }

    public async Task<IReadOnlyList<ReceivableSummaryDto>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerName)
            .Select(x => new ReceivableSummaryDto
            {
                CustomerId = x.Id.ToString(),
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                Status = x.IsActive ? DebtStatus.Paid : DebtStatus.Cancelled
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default)
    {
        var people = await _dbContext.People
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.FullName)
            .ToListAsync(cancellationToken);

        var documentUsers = await _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Customer
                        && (ReceivableDebitTypes.Contains(x.DocumentType) || ReceivableCreditTypes.Contains(x.DocumentType))
                        && x.CreatedBy != string.Empty)
            .Select(x => x.CreatedBy)
            .Distinct()
            .ToListAsync(cancellationToken);

        return people
            .Concat(documentUsers)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<IReadOnlyList<ReceivableSummaryDto>> GetDebtSummariesAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ReceivableTransactions.AsNoTracking().AnyAsync(cancellationToken))
        {
            return await GetPersistedDebtSummariesAsync(filter, cancellationToken);
        }

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerName)
            .ToListAsync(cancellationToken);

        var docsQuery = _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Customer
                        && (ReceivableDebitTypes.Contains(x.DocumentType) || ReceivableCreditTypes.Contains(x.DocumentType)));

        if (filter.FromDate.HasValue)
            docsQuery = docsQuery.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            docsQuery = docsQuery.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);
        if (!string.IsNullOrWhiteSpace(filter.Owner))
            docsQuery = docsQuery.Where(x => x.CreatedBy == filter.Owner);

        var docs = await docsQuery.ToListAsync(cancellationToken);

        var keyword = filter.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            customers = customers.Where(x =>
                    x.CustomerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    docs.Any(d => d.EntityId == ToEntityGuid(x.Id) && d.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var rows = new List<ReceivableSummaryDto>(customers.Count);
        foreach (var customer in customers)
        {
            var customerGuid = ToEntityGuid(customer.Id);
            var customerDocs = docs.Where(x => x.EntityId == customerGuid).OrderBy(x => x.DocumentDate).ThenBy(x => x.Code).ToList();
            var debit = customerDocs.Where(x => ReceivableDebitTypes.Contains(x.DocumentType)).Sum(x => x.Amount);
            var credit = customerDocs.Where(x => ReceivableCreditTypes.Contains(x.DocumentType)).Sum(x => x.Amount);
            var closing = debit - credit;
            var overdue = closing > 0 && customerDocs.Any(x => ReceivableDebitTypes.Contains(x.DocumentType) && GetDueDate(x).Date < DateTime.Today)
                ? closing
                : 0m;

            var status = overdue > 0
                ? DebtStatus.Overdue
                : closing <= 0
                    ? DebtStatus.Paid
                    : credit > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;

            if (filter.Status.HasValue && status != filter.Status.Value)
                continue;
            if (filter.OverdueOnly && overdue <= 0)
                continue;

            rows.Add(new ReceivableSummaryDto
            {
                CustomerId = customer.Id.ToString(),
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                OpeningBalance = 0,
                TotalDebit = debit,
                TotalCredit = credit,
                ClosingBalance = closing,
                OverdueAmount = overdue,
                LastTransactionDate = customerDocs.LastOrDefault()?.DocumentDate,
                Status = status
            });
        }

        return rows.OrderByDescending(x => x.ClosingBalance).ToList();
    }

    public async Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ReceivableTransactions.AsNoTracking().AnyAsync(cancellationToken))
        {
            return await GetPersistedTransactionsAsync(filter, cancellationToken);
        }

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .ToDictionaryAsync(x => ToEntityGuid(x.Id), cancellationToken);

        var query = _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Customer
                        && (ReceivableDebitTypes.Contains(x.DocumentType) || ReceivableCreditTypes.Contains(x.DocumentType)));

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);
        if (!string.IsNullOrWhiteSpace(filter.Owner))
            query = query.Where(x => x.CreatedBy == filter.Owner);

        var docs = await query
            .OrderBy(x => x.EntityId)
            .ThenBy(x => x.DocumentDate)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var keyword = filter.Keyword?.Trim();
        var runningByCustomer = new Dictionary<Guid, decimal>();
        var rows = new List<ReceivableTransactionDto>(docs.Count);

        foreach (var doc in docs)
        {
            if (!customers.TryGetValue(doc.EntityId, out var customer))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(keyword)
                && !customer.CustomerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !customer.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !doc.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !doc.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var row = MapTransaction(doc, customer, runningByCustomer);
            if (filter.Status.HasValue && row.Status != filter.Status.Value)
            {
                continue;
            }

            if (filter.OverdueOnly && row.Status != DebtStatus.Overdue)
            {
                continue;
            }

            rows.Add(row);
        }

        return rows.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
    }

    public async Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsByCustomerAsync(string customerId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ReceivableTransactions.AsNoTracking().AnyAsync(cancellationToken))
        {
            return await GetPersistedTransactionsByCustomerAsync(customerId, fromDate, toDate, cancellationToken);
        }

        if (!int.TryParse(customerId, out var customerPk))
            return [];

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == customerPk, cancellationToken);
        if (customer is null) return [];

        var customerGuid = ToEntityGuid(customerPk);
        var query = _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Customer
                        && x.EntityId == customerGuid
                        && (ReceivableDebitTypes.Contains(x.DocumentType) || ReceivableCreditTypes.Contains(x.DocumentType)));

        if (fromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);

        var docs = await query.OrderBy(x => x.DocumentDate).ThenBy(x => x.Code).ToListAsync(cancellationToken);

        var running = 0m;
        var result = new List<ReceivableTransactionDto>(docs.Count);
        foreach (var doc in docs)
        {
            var runningByCustomer = new Dictionary<Guid, decimal> { [customerGuid] = running };
            var row = MapTransaction(doc, customer, runningByCustomer);
            running = row.BalanceAmount;
            result.Add(row);
        }

        return result.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
    }

    public async Task<string> GetNextReceiptNoAsync(CancellationToken cancellationToken = default)
        => await GenerateNextCodeAsync("PT", _branchCode, "receipt-no", cancellationToken);

    public async Task<string> CreatePaymentReceiptAsync(CreatePaymentReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(request.CustomerId, out var customerPk))
            return string.Empty;

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == customerPk, cancellationToken);
        if (customer is null)
            return string.Empty;

        var code = await GenerateNextCodeAsync("PT", _branchCode, "receipt-no", cancellationToken);
        var doc = new BusinessDocument
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = string.IsNullOrWhiteSpace(request.Note) ? "Phiếu thu công nợ" : request.Note,
            Module = BusinessModule.Accounting,
            EntityType = BusinessEntityType.Customer,
            EntityId = ToEntityGuid(customer.Id),
            DocumentType = BusinessDocumentType.PaymentVoucher,
            DocumentDate = request.ReceiptDate,
            Amount = request.Amount,
            Currency = "VND",
            VirtualPath = $@"D:\ACCOUNTING\Khách hàng\{customer.CustomerName}\Công nợ\{request.ReceiptDate:yyyy}\{request.ReceiptDate:MM}",
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
        await _auditLogService.LogAsync("ReceivablePaymentCreated", "Finance.Receivable", $"Customer={customer.CustomerCode}; Amount={request.Amount:N0}", cancellationToken);
        return code;
    }

    public async Task<IReadOnlyList<ReceivableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default)
        => await GetDebtSummariesAsync(new ReceivableFilterDto { OverdueOnly = true }, cancellationToken);

    public Task PrintDebtStatementAsync(string customerId, CancellationToken cancellationToken = default)
        => _auditLogService.LogAsync("ReceivablePrint", "Finance.Receivable", $"Customer={customerId}", cancellationToken);

    public Task ExportDebtStatementAsync(string customerId, CancellationToken cancellationToken = default)
        => _auditLogService.LogAsync("ReceivableExport", "Finance.Receivable", $"Customer={customerId}", cancellationToken);

    private async Task<IReadOnlyList<ReceivableSummaryDto>> GetPersistedDebtSummariesAsync(ReceivableFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.ReceivableTransactions.AsNoTracking().AsQueryable();
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);

        var transactions = await query.ToListAsync(cancellationToken);
        var customerIds = transactions.Select(x => DecodeEntityInt(x.CustomerId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var keyword = filter.Keyword?.Trim();
        var rows = new List<ReceivableSummaryDto>();
        foreach (var group in transactions.GroupBy(x => x.CustomerId))
        {
            var customerPk = DecodeEntityInt(group.Key);
            customers.TryGetValue(customerPk ?? 0, out var customer);
            var customerCode = customer?.CustomerCode ?? group.Key.ToString("N");
            var customerName = customer?.CustomerName ?? customerCode;

            if (!string.IsNullOrWhiteSpace(keyword)
                && !customerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !customerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !group.Any(x => x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var totalDebit = group.Sum(x => x.DebitAmount);
            var totalCredit = group.Sum(x => x.CreditAmount);
            var closing = totalDebit - totalCredit;
            var overdue = group.Where(x => x.BalanceAmount > 0 && x.DueDate.HasValue && x.DueDate.Value.Date < DateTime.Today).Sum(x => x.BalanceAmount);
            var status = ToDebtStatus(closing, overdue, totalCredit);

            if (filter.Status.HasValue && status != filter.Status.Value)
                continue;
            if (filter.OverdueOnly && overdue <= 0)
                continue;

            rows.Add(new ReceivableSummaryDto
            {
                CustomerId = customerPk?.ToString() ?? group.Key.ToString("N"),
                CustomerCode = customerCode,
                CustomerName = customerName,
                OpeningBalance = 0,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = closing,
                OverdueAmount = overdue,
                LastTransactionDate = group.Max(x => (DateTime?)x.DocumentDate),
                Status = status
            });
        }

        return rows.OrderByDescending(x => x.ClosingBalance).ToList();
    }

    private async Task<IReadOnlyList<ReceivableTransactionDto>> GetPersistedTransactionsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.ReceivableTransactions.AsNoTracking().AsQueryable();
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= filter.ToDate.Value.Date);

        var transactions = await query.OrderBy(x => x.CustomerId).ThenBy(x => x.DocumentDate).ThenBy(x => x.DocumentNo).ToListAsync(cancellationToken);
        var customerIds = transactions.Select(x => DecodeEntityInt(x.CustomerId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var keyword = filter.Keyword?.Trim();
        var runningByCustomer = new Dictionary<Guid, decimal>();
        var rows = new List<ReceivableTransactionDto>(transactions.Count);
        foreach (var transaction in transactions)
        {
            var customerPk = DecodeEntityInt(transaction.CustomerId);
            customers.TryGetValue(customerPk ?? 0, out var customer);
            var customerCode = customer?.CustomerCode ?? transaction.CustomerId.ToString("N");
            var customerName = customer?.CustomerName ?? customerCode;

            if (!string.IsNullOrWhiteSpace(keyword)
                && !customerCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !customerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !transaction.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !(transaction.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true))
            {
                continue;
            }

            runningByCustomer.TryGetValue(transaction.CustomerId, out var running);
            running += transaction.DebitAmount - transaction.CreditAmount;
            runningByCustomer[transaction.CustomerId] = running;

            var status = MapDebtStatus(transaction.Status, Math.Max(0, running), transaction.DueDate);
            if (filter.Status.HasValue && status != filter.Status.Value)
                continue;
            if (filter.OverdueOnly && status != DebtStatus.Overdue)
                continue;

            rows.Add(ToDto(transaction, customerPk?.ToString() ?? transaction.CustomerId.ToString("N"), customerCode, customerName, Math.Max(0, running), status));
        }

        return rows.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
    }

    private async Task<IReadOnlyList<ReceivableTransactionDto>> GetPersistedTransactionsByCustomerAsync(string customerId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        if (!int.TryParse(customerId, out var customerPk))
            return [];

        var customerGuid = PostingSourceIds.FromInt32(customerPk);
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == customerPk, cancellationToken);
        if (customer is null)
            return [];

        var query = _dbContext.ReceivableTransactions.AsNoTracking().Where(x => x.CustomerId == customerGuid);
        if (fromDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);

        var transactions = await query.OrderBy(x => x.DocumentDate).ThenBy(x => x.DocumentNo).ToListAsync(cancellationToken);
        var running = 0m;
        var rows = new List<ReceivableTransactionDto>(transactions.Count);
        foreach (var transaction in transactions)
        {
            running += transaction.DebitAmount - transaction.CreditAmount;
            rows.Add(ToDto(transaction, customer.Id.ToString(), customer.CustomerCode, customer.CustomerName, Math.Max(0, running), MapDebtStatus(transaction.Status, Math.Max(0, running), transaction.DueDate)));
        }

        return rows.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo).ToList();
    }

    private static Guid ToEntityGuid(int id) => GuidUtility.Create(GuidUtility.UrlNamespace, $"customer:{id}");

    private static int? DecodeEntityInt(Guid id)
        => PostingSourceIds.TryToInt32(id, out var value) ? value : null;

    private static ReceivableTransactionDto MapTransaction(BusinessDocument doc, Customer customer, Dictionary<Guid, decimal> runningByCustomer)
    {
        runningByCustomer.TryGetValue(doc.EntityId, out var running);
        var debit = ReceivableDebitTypes.Contains(doc.DocumentType) ? doc.Amount : 0m;
        var credit = ReceivableCreditTypes.Contains(doc.DocumentType) ? doc.Amount : 0m;
        running += debit - credit;
        runningByCustomer[doc.EntityId] = running;

        var dueDate = ReceivableDebitTypes.Contains(doc.DocumentType) ? GetDueDate(doc) : (DateTime?)null;
        var status = running <= 0
            ? DebtStatus.Paid
            : dueDate.HasValue && dueDate.Value.Date < DateTime.Today ? DebtStatus.Overdue : credit > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Open;

        return new ReceivableTransactionDto
        {
            Id = doc.Id.ToString("N"),
            CustomerId = customer.Id.ToString(),
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            DocumentNo = doc.Code,
            DocumentType = MapReceivableDocumentType(doc.DocumentType),
            TransactionDate = doc.DocumentDate,
            DueDate = dueDate,
            Description = string.IsNullOrWhiteSpace(doc.Title) ? doc.Code : doc.Title,
            DebitAmount = debit,
            CreditAmount = credit,
            BalanceAmount = Math.Max(0, running),
            Currency = doc.Currency,
            CreatedBy = doc.CreatedBy,
            Status = status
        };
    }

    private static DateTime GetDueDate(BusinessDocument doc) => doc.DocumentDate.Date.AddDays(30);

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

    private static ReceivableDocumentType MapReceivableDocumentType(BusinessDocumentType documentType) => documentType switch
    {
        BusinessDocumentType.Invoice => ReceivableDocumentType.SalesInvoice,
        BusinessDocumentType.ExportVoucher => ReceivableDocumentType.ExportVoucher,
        BusinessDocumentType.PaymentVoucher => ReceivableDocumentType.PaymentReceipt,
        BusinessDocumentType.AdvancePayment => ReceivableDocumentType.PaymentReceipt,
        _ => ReceivableDocumentType.Adjustment
    };

    private static ReceivableTransactionDto ToDto(ReceivableTransaction transaction, string customerId, string customerCode, string customerName, decimal balance, DebtStatus status)
        => new()
        {
            Id = transaction.Id.ToString("N"),
            CustomerId = customerId,
            CustomerCode = customerCode,
            CustomerName = customerName,
            DocumentNo = transaction.DocumentNo,
            DocumentType = MapReceivableDocumentType(transaction.SourceType),
            TransactionDate = transaction.DocumentDate,
            DueDate = transaction.DueDate,
            Description = transaction.Description ?? transaction.DocumentNo,
            DebitAmount = transaction.DebitAmount,
            CreditAmount = transaction.CreditAmount,
            BalanceAmount = balance,
            Currency = transaction.CurrencyCode,
            CreatedBy = string.Empty,
            Status = status
        };

    private static ReceivableDocumentType MapReceivableDocumentType(string sourceType) => sourceType switch
    {
        "BusinessDocument" => ReceivableDocumentType.SalesInvoice,
        "PaymentReceipt" => ReceivableDocumentType.PaymentReceipt,
        _ => ReceivableDocumentType.Adjustment
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

internal static class GuidUtility
{
    public static readonly Guid UrlNamespace = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    public static Guid Create(Guid namespaceId, string name)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
        var hash = System.Security.Cryptography.MD5.HashData(namespaceBytes.Concat(nameBytes).ToArray());
        hash[6] = (byte)((hash[6] & 0x0F) | (3 << 4));
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        SwapByteOrder(hash);
        return new Guid(hash);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
