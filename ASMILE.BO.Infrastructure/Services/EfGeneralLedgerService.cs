using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EfGeneralLedgerService : IGeneralLedgerService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;
    private readonly IJournalService _journalService;

    public EfGeneralLedgerService(AppDbContext dbContext, IAuditLogService auditLogService, ISettingsService settingsService, IJournalService journalService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
        _journalService = journalService;
    }

    public async Task<IReadOnlyList<LedgerAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultAccountsAsync(cancellationToken);
        var dbAccounts = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .OrderBy(x => x.SortOrder == 0 ? int.MaxValue : x.SortOrder)
            .ThenBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);

        if (dbAccounts.Count > 0)
        {
            return dbAccounts.Select(MapAccount).ToList();
        }

        var entries = await LoadAllEntriesAsync(cancellationToken);
        return entries
            .SelectMany(x => new[] { x.DebitAccount, x.CreditAccount })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Select(x => new LedgerAccountDto
            {
                Id = x,
                AccountCode = x,
                AccountName = GetAccountName(x),
                ParentAccountCode = x.Length > 3 ? x[..3] : null,
                AccountType = GetAccountType(x),
                IsActive = true
            })
            .ToList();
    }

    public async Task SaveAccountsAsync(IReadOnlyList<LedgerAccountDto> accounts, CancellationToken cancellationToken = default)
    {
        await EnsureLedgerAccountsTableAsync(cancellationToken);

        foreach (var item in accounts.Where(x => !string.IsNullOrWhiteSpace(x.AccountCode)))
        {
            var code = item.AccountCode.Trim();
            var existing = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(x => x.AccountCode == code, cancellationToken);
            if (existing is null)
            {
            existing = new ASMILE.BO.Core.Entities.LedgerAccount { Id = Guid.NewGuid(), AccountCode = code };
                _dbContext.LedgerAccounts.Add(existing);
            }

            existing.SortOrder = item.SortOrder;
            existing.AccountName = string.IsNullOrWhiteSpace(item.AccountName) ? $"Tài khoản {code}" : item.AccountName.Trim();
            existing.ReportName = string.IsNullOrWhiteSpace(item.ReportName) ? existing.AccountName : item.ReportName.Trim();
            existing.ParentAccountCode = string.IsNullOrWhiteSpace(item.ParentAccountCode) ? null : item.ParentAccountCode.Trim();
            existing.AccountLevel = item.AccountLevel <= 0 ? GetAccountLevel(code, existing.ParentAccountCode) : item.AccountLevel;
            existing.AccountKind = NormalizeAccountKind(item.AccountKind, code, existing.ParentAccountCode);
            existing.AccountNature = NormalizeAccountNature(item.AccountNature);
            existing.AccountType = item.AccountType.ToString();
            existing.IsCashAccount = item.IsCashAccount;
            existing.HasForeignCurrency = item.HasForeignCurrency;
            existing.FollowWarehouse = item.FollowWarehouse;
            existing.CustomerDebt = item.CustomerDebt;
            existing.SupplierDebt = item.SupplierDebt;
            existing.AllowFund = item.AllowFund;
            existing.AccountingStandardCode = NormalizeAccountingStandardCode(item.AccountingStandardCode);
            existing.AllowPosting = item.AllowPosting;
            existing.IsSystemAccount = item.IsSystemAccount;
            existing.IsEditable = item.IsEditable;
            existing.IsDeletable = item.IsDeletable;
            existing.NormalBalance = NormalizeNormalBalance(item.NormalBalance, code);
            existing.AccountCategory = NormalizeAccountCategory(item.AccountCategory, code);
            existing.RequireCustomer = item.RequireCustomer;
            existing.RequireSupplier = item.RequireSupplier;
            existing.RequireWarehouse = item.RequireWarehouse;
            existing.RequireItem = item.RequireItem;
            existing.RequireDepartment = item.RequireDepartment;
            existing.RequireEmployee = item.RequireEmployee;
            existing.RequireCurrency = item.RequireCurrency;
            existing.IsFinancialAccount = item.IsFinancialAccount;
            existing.IsManagementAccount = item.IsManagementAccount;
            existing.CreatedAt = item.CreatedAt == default ? DateTime.Now : item.CreatedAt;
            existing.UpdatedAt = DateTime.Now;
            existing.IsActive = item.IsActive;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("LedgerAccountsSaved", "Finance.GeneralLedger", $"Count={accounts.Count}", cancellationToken);
    }

    public async Task DeleteAccountAsync(string accountCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
        {
            return;
        }

        await EnsureLedgerAccountsTableAsync(cancellationToken);

        var account = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(x => x.AccountCode == accountCode.Trim(), cancellationToken);
        if (account is null)
        {
            return;
        }

        if (account.IsSystemAccount || !account.IsDeletable)
        {
            throw new InvalidOperationException("Không thể xóa tài khoản hệ thống hoặc tài khoản không cho phép xóa.");
        }

        var hasJournal = await _dbContext.JournalLines.AnyAsync(x => x.AccountCode == account.AccountCode, cancellationToken);
        if (hasJournal)
        {
            throw new InvalidOperationException("Không thể xóa tài khoản đã có phát sinh bút toán.");
        }

        _dbContext.LedgerAccounts.Remove(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("LedgerAccountDeleted", "Finance.GeneralLedger", $"Account={accountCode}", cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerEntriesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (await HasPersistedJournalsAsync(cancellationToken))
        {
            var persistedEntries = await LoadPersistedEntriesAsync(filter, cancellationToken);
            return persistedEntries
                .OrderByDescending(x => x.EntryDate)
                .ThenByDescending(x => x.EntryNo)
                .ToList();
        }

        // Legacy fallback until all modules post persisted journal.
        var entries = await LoadAllEntriesAsync(cancellationToken);
        return ApplyFilter(entries, filter)
            .OrderByDescending(x => x.EntryDate)
            .ThenByDescending(x => x.EntryNo)
            .ToList();
    }

    public async Task<IReadOnlyList<LedgerBalanceDto>> GetLedgerBalancesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (await HasPersistedJournalsAsync(cancellationToken))
        {
            return await GetPersistedLedgerBalancesAsync(filter, cancellationToken);
        }

        // Legacy fallback until all modules post persisted journal.
        var entries = await LoadAllEntriesAsync(cancellationToken);
        var accountFilter = filter.AccountCode?.Trim();
        if (!string.IsNullOrWhiteSpace(accountFilter))
        {
            entries = entries
                .Where(x => string.Equals(x.DebitAccount, accountFilter, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(x.CreditAccount, accountFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (filter.DocumentType.HasValue)
        {
            entries = entries.Where(x => x.DocumentType == filter.DocumentType.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            entries = entries.Where(x =>
                    x.EntryNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.DebitAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    x.CreditAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var openingLines = filter.FromDate.HasValue
            ? entries.Where(x => x.EntryDate.Date < filter.FromDate.Value.Date)
            : Enumerable.Empty<LedgerEntryDto>();

        var periodLines = entries.AsEnumerable();
        if (filter.FromDate.HasValue)
        {
            periodLines = periodLines.Where(x => x.EntryDate.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            periodLines = periodLines.Where(x => x.EntryDate.Date <= filter.ToDate.Value.Date);
        }

        var rows = new Dictionary<string, LedgerBalanceDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in openingLines)
        {
            AddBalance(rows, entry.DebitAccount, entry.Amount, 0, opening: true);
            AddBalance(rows, entry.CreditAccount, 0, entry.Amount, opening: true);
        }

        foreach (var entry in periodLines)
        {
            AddBalance(rows, entry.DebitAccount, entry.Amount, 0, opening: false);
            AddBalance(rows, entry.CreditAccount, 0, entry.Amount, opening: false);
        }

        foreach (var row in rows.Values)
        {
            var debitTotal = row.OpeningDebit + row.PeriodDebit;
            var creditTotal = row.OpeningCredit + row.PeriodCredit;
            row.ClosingDebit = Math.Max(0, debitTotal - creditTotal);
            row.ClosingCredit = Math.Max(0, creditTotal - debitTotal);
        }

        return rows.Values.OrderBy(x => x.AccountCode).ToList();
    }

    public async Task CreateManualEntryAsync(CreateLedgerEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền bút toán phải lớn hơn 0.");
        }

        var debitAccount = request.DebitAccount.Trim();
        var creditAccount = request.CreditAccount.Trim();
        if (string.IsNullOrWhiteSpace(debitAccount) || string.IsNullOrWhiteSpace(creditAccount))
        {
            throw new InvalidOperationException("Tài khoản Nợ và tài khoản Có là bắt buộc.");
        }

        if (string.Equals(debitAccount, creditAccount, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Tài khoản Nợ và tài khoản Có phải khác nhau.");
        }

        var accounts = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(x => x.AccountCode == debitAccount || x.AccountCode == creditAccount)
            .ToDictionaryAsync(x => x.AccountCode, StringComparer.OrdinalIgnoreCase, cancellationToken);
        if (!accounts.TryGetValue(debitAccount, out var debitLedgerAccount))
        {
            throw new InvalidOperationException($"Không tìm thấy tài khoản Nợ {debitAccount}.");
        }

        if (!accounts.TryGetValue(creditAccount, out var creditLedgerAccount))
        {
            throw new InvalidOperationException($"Không tìm thấy tài khoản Có {creditAccount}.");
        }

        var postingDate = request.EntryDate == default ? DateTime.Today : request.EntryDate.Date;
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = GenerateJournalNo(),
            JournalDate = postingDate,
            PostingDate = postingDate,
            SourceType = "ManualJournal",
            SourceNo = string.IsNullOrWhiteSpace(request.DocumentNo) ? string.Empty : request.DocumentNo.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Bút toán thủ công" : request.Description.Trim(),
            PeriodCode = postingDate.ToString("yyyy-MM"),
            Status = JournalStatus.Draft,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    LineNo = 1,
                    AccountCode = debitAccount,
                    AccountName = debitLedgerAccount.AccountName,
                    DebitAmount = request.Amount,
                    CreditAmount = 0,
                    CurrencyCode = "VND",
                    ExchangeRate = 1,
                    Description = request.Description
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    LineNo = 2,
                    AccountCode = creditAccount,
                    AccountName = creditLedgerAccount.AccountName,
                    DebitAmount = 0,
                    CreditAmount = request.Amount,
                    CurrencyCode = "VND",
                    ExchangeRate = 1,
                    Description = request.Description
                }
            }
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        var posted = await _journalService.PostDraftAsync(draft.Id, "system", cancellationToken);
        await _auditLogService.LogAsync(
            "LedgerManualEntryPosted",
            "Finance.GeneralLedger",
            $"Journal={posted.JournalNo}; Document={request.DocumentNo}; Debit={debitAccount}; Credit={creditAccount}; Amount={request.Amount:N0}",
            cancellationToken);
    }

    public async Task PostFromDocumentAsync(LedgerDocumentType documentType, string documentId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("LedgerPostedFromDocument", "Finance.GeneralLedger", $"DocType={documentType}; DocId={documentId}", cancellationToken);
    }

    public async Task PrintLedgerAsync(string accountCode, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("LedgerPrint", "Finance.GeneralLedger", $"Account={accountCode}", cancellationToken);
    }

    public async Task ExportLedgerAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("LedgerExport", "Finance.GeneralLedger", $"Account={filter.AccountCode}; From={filter.FromDate:yyyy-MM-dd}; To={filter.ToDate:yyyy-MM-dd}", cancellationToken);
    }

    private async Task<bool> HasPersistedJournalsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.JournalHeaders
            .AsNoTracking()
            .AnyAsync(x => x.Status == JournalStatus.Posted, cancellationToken);
    }

    private async Task<List<LedgerEntryDto>> LoadPersistedEntriesAsync(LedgerFilterDto filter, CancellationToken cancellationToken)
    {
        var journals = await BuildPersistedJournalQuery(filter, includeDateFilter: true, postedOnly: false)
            .Include(x => x.Lines.OrderBy(line => line.LineNo))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var periodCodes = journals.Select(x => x.PeriodCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var lockedPeriods = await _dbContext.AccountingPeriods
            .AsNoTracking()
            .Where(x => periodCodes.Contains(x.PeriodCode) && x.Status != AccountingPeriodStatus.Open)
            .Select(x => x.PeriodCode)
            .ToListAsync(cancellationToken);
        var lockedPeriodSet = lockedPeriods.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = new List<LedgerEntryDto>();
        foreach (var journal in journals)
        {
            var debitAccounts = journal.Lines
                .Where(x => x.DebitAmount > 0)
                .Select(x => x.AccountCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var creditAccounts = journal.Lines
                .Where(x => x.CreditAmount > 0)
                .Select(x => x.AccountCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var line in journal.Lines.OrderBy(x => x.LineNo))
            {
                var isDebit = line.DebitAmount > 0;
                var amount = isDebit ? line.DebitAmount : line.CreditAmount;
                if (amount <= 0)
                {
                    continue;
                }

                var counterAccounts = isDebit ? creditAccounts : debitAccounts;
                rows.Add(new LedgerEntryDto
                {
                    Id = $"Journal:{journal.Id:N}:{line.Id:N}",
                    JournalId = journal.Id,
                    JournalLineId = line.Id,
                    EntryNo = journal.JournalNo,
                    EntryDate = journal.PostingDate,
                    DocumentNo = string.IsNullOrWhiteSpace(journal.SourceNo) ? journal.JournalNo : journal.SourceNo,
                    DocumentType = MapSourceTypeToDocumentType(journal.SourceType),
                    Description = string.IsNullOrWhiteSpace(line.Description) ? journal.Description : line.Description,
                    DebitAccount = isDebit ? line.AccountCode : string.Join(", ", counterAccounts),
                    CreditAccount = isDebit ? string.Join(", ", counterAccounts) : line.AccountCode,
                    Amount = amount,
                    DebitAmount = line.DebitAmount,
                    CreditAmount = line.CreditAmount,
                    SourceType = journal.SourceType,
                    SourceNo = journal.SourceNo,
                    Status = journal.Status.ToString(),
                    StatusDisplay = ToVietnameseJournalStatus(journal.Status),
                    IsLockedPeriod = lockedPeriodSet.Contains(journal.PeriodCode),
                    Currency = string.IsNullOrWhiteSpace(line.CurrencyCode) ? "VND" : line.CurrencyCode
                });
            }
        }

        return rows;
    }

    private async Task<IReadOnlyList<LedgerBalanceDto>> GetPersistedLedgerBalancesAsync(LedgerFilterDto filter, CancellationToken cancellationToken)
    {
        var journals = await BuildPersistedJournalQuery(filter, includeDateFilter: false, postedOnly: true)
            .Include(x => x.Lines)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var accountMetadata = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .ToDictionaryAsync(x => x.AccountCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var rows = new Dictionary<string, LedgerBalanceDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var journal in journals)
        {
            var isOpening = filter.FromDate.HasValue && journal.PostingDate.Date < filter.FromDate.Value.Date;
            if (filter.FromDate.HasValue && journal.PostingDate.Date >= filter.FromDate.Value.Date)
            {
                isOpening = false;
            }

            if (filter.ToDate.HasValue && journal.PostingDate.Date > filter.ToDate.Value.Date)
            {
                continue;
            }

            foreach (var line in journal.Lines)
            {
                if (!string.IsNullOrWhiteSpace(filter.AccountCode)
                    && !string.Equals(line.AccountCode, filter.AccountCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddBalance(rows, line.AccountCode, line.DebitAmount, line.CreditAmount, opening: isOpening);
                if (rows.TryGetValue(line.AccountCode, out var balance) && !string.IsNullOrWhiteSpace(line.AccountName))
                {
                    balance.AccountName = line.AccountName;
                    if (accountMetadata.TryGetValue(line.AccountCode, out var account))
                    {
                        balance.AccountLevel = account.AccountLevel;
                        balance.AllowPosting = account.AllowPosting;
                        balance.AccountingStandardCode = account.AccountingStandardCode;
                        balance.IsActive = account.IsActive;
                    }
                }
            }
        }

        foreach (var row in rows.Values)
        {
            var debitTotal = row.OpeningDebit + row.PeriodDebit;
            var creditTotal = row.OpeningCredit + row.PeriodCredit;
            row.ClosingDebit = Math.Max(0, debitTotal - creditTotal);
            row.ClosingCredit = Math.Max(0, creditTotal - debitTotal);
        }

        return rows.Values.OrderBy(x => x.AccountCode).ToList();
    }

    private IQueryable<JournalHeader> BuildPersistedJournalQuery(LedgerFilterDto filter, bool includeDateFilter, bool postedOnly)
    {
        var query = _dbContext.JournalHeaders
            .Where(x => postedOnly ? x.Status == JournalStatus.Posted : x.Status != JournalStatus.Cancelled);

        if (includeDateFilter && filter.FromDate.HasValue)
        {
            var fromDate = filter.FromDate.Value.Date;
            query = query.Where(x => x.PostingDate >= fromDate);
        }

        if (includeDateFilter && filter.ToDate.HasValue)
        {
            var toDate = filter.ToDate.Value.Date;
            query = query.Where(x => x.PostingDate <= toDate);
        }

        if (filter.DocumentType.HasValue)
        {
            var sourceTypes = MapDocumentTypeToSourceTypes(filter.DocumentType.Value);
            query = query.Where(x => sourceTypes.Contains(x.SourceType));
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceType))
        {
            var sourceType = filter.SourceType.Trim();
            query = query.Where(x => x.SourceType == sourceType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<JournalStatus>(filter.Status.Trim(), true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountCode))
        {
            var accountCode = filter.AccountCode.Trim();
            query = query.Where(x => x.Lines.Any(line => line.AccountCode == accountCode));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x =>
                x.JournalNo.Contains(keyword)
                || x.SourceNo.Contains(keyword)
                || x.Description.Contains(keyword)
                || x.Lines.Any(line => line.AccountCode.Contains(keyword)
                    || line.AccountName.Contains(keyword)
                    || (line.Description != null && line.Description.Contains(keyword))));
        }

        return query;
    }

    private static LedgerDocumentType MapSourceTypeToDocumentType(string sourceType) => sourceType switch
    {
        "StockReceipt" => LedgerDocumentType.ImportVoucher,
        "StockIssue" => LedgerDocumentType.ExportVoucher,
        "PaymentReceipt" => LedgerDocumentType.PaymentReceipt,
        "PaymentVoucher" => LedgerDocumentType.PaymentVoucher,
        "ManualJournal" => LedgerDocumentType.Adjustment,
        _ => LedgerDocumentType.Adjustment
    };

    private static string[] MapDocumentTypeToSourceTypes(LedgerDocumentType documentType) => documentType switch
    {
        LedgerDocumentType.ImportVoucher => ["StockReceipt"],
        LedgerDocumentType.ExportVoucher => ["StockIssue"],
        LedgerDocumentType.PaymentReceipt => ["PaymentReceipt"],
        LedgerDocumentType.PaymentVoucher => ["PaymentVoucher"],
        LedgerDocumentType.Adjustment => ["ManualJournal", "Reversal"],
        _ => [documentType.ToString()]
    };

    private static string ToVietnameseJournalStatus(JournalStatus status) => status switch
    {
        JournalStatus.Draft => "Nháp",
        JournalStatus.Posted => "Đã ghi sổ",
        JournalStatus.Reversed => "Đã đảo",
        JournalStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static string GenerateJournalNo()
        => $"JV-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 99999):D5}";

    private async Task<List<LedgerEntryDto>> LoadAllEntriesAsync(CancellationToken cancellationToken)
    {
        var rows = new List<LedgerEntryDto>();
        var accounts = await LoadAccountSettingsAsync();

        var receipts = await _dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .ToListAsync(cancellationToken);

        foreach (var receipt in receipts)
        {
            if (receipt.TotalAmount <= 0)
            {
                continue;
            }

            AddEntry(rows,
                $"PN-{receipt.Id:D6}",
                receipt.ReceiptDate,
                receipt.ReceiptNo,
                LedgerDocumentType.ImportVoucher,
                string.IsNullOrWhiteSpace(receipt.Description) ? $"Phiếu nhập {receipt.ReceiptNo}" : receipt.Description,
                NormalizeAccount(receipt.DebitAccountCode, accounts.InventoryAccount),
                NormalizeAccount(receipt.CreditAccountCode, accounts.PayableAccount),
                receipt.TotalAmount,
                "StockReceipt",
                receipt.Id.ToString());
        }

        var issues = await _dbContext.StockIssues
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .ToListAsync(cancellationToken);

        foreach (var issue in issues)
        {
            if (issue.TotalAmount <= 0)
            {
                continue;
            }

            AddEntry(rows,
                $"PX-{issue.Id:D6}",
                issue.IssueDate,
                issue.IssueNo,
                issue.ToWarehouseId.HasValue ? LedgerDocumentType.Adjustment : LedgerDocumentType.ExportVoucher,
                string.IsNullOrWhiteSpace(issue.Description) ? $"Phiếu xuất {issue.IssueNo}" : issue.Description,
                NormalizeAccount(issue.DebitAccountCode, accounts.CostOfGoodsSoldAccount),
                NormalizeAccount(issue.CreditAccountCode, accounts.InventoryAccount),
                issue.TotalAmount,
                "StockIssue",
                issue.Id.ToString());
        }

        var stockDocumentCodes = receipts.Select(x => x.ReceiptNo)
            .Concat(issues.Select(x => x.IssueNo))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var documents = await _dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Active || x.Status == DocumentStatus.Locked)
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            if (document.Amount <= 0 || stockDocumentCodes.Contains(document.Code))
            {
                continue;
            }

            var mapped = MapBusinessDocumentAccounts(document, accounts);
            if (mapped is null)
            {
                continue;
            }

            AddEntry(rows,
                $"CT-{document.CreatedAt:yyyyMMddHHmmss}-{rows.Count + 1:D4}",
                document.DocumentDate,
                document.Code,
                mapped.Value.documentType,
                string.IsNullOrWhiteSpace(document.Title) ? document.Code : document.Title,
                mapped.Value.debitAccount,
                mapped.Value.creditAccount,
                document.Amount,
                "BusinessDocument",
                document.Id.ToString("N"));
        }

        return rows;
    }

    private async Task EnsureDefaultAccountsAsync(CancellationToken cancellationToken)
    {
        await EnsureLedgerAccountsTableAsync(cancellationToken);

        var existingAccounts = await _dbContext.LedgerAccounts.ToListAsync(cancellationToken);
        if (existingAccounts.Any(x => x.AccountCode.Length > 3))
        {
            return;
        }

        if (existingAccounts.Count > 0)
        {
            _dbContext.LedgerAccounts.RemoveRange(existingAccounts);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var defaults = GetDefaultLedgerAccounts();

        foreach (var account in defaults)
        {
            _dbContext.LedgerAccounts.Add(new ASMILE.BO.Core.Entities.LedgerAccount
            {
                Id = Guid.NewGuid(),
                SortOrder = account.SortOrder,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                ReportName = account.ReportName,
                ParentAccountCode = account.ParentAccountCode,
                AccountLevel = account.AccountLevel,
                AccountKind = account.AccountKind,
                AccountNature = account.AccountNature,
                AccountType = account.AccountType.ToString(),
                IsCashAccount = account.IsCashAccount,
                HasForeignCurrency = account.HasForeignCurrency,
                FollowWarehouse = account.FollowWarehouse,
                CustomerDebt = account.CustomerDebt,
                SupplierDebt = account.SupplierDebt,
                AllowFund = account.AllowFund,
                AccountingStandardCode = NormalizeAccountingStandardCode(account.AccountingStandardCode),
                AllowPosting = account.AllowPosting,
                IsSystemAccount = account.IsSystemAccount,
                IsEditable = account.IsEditable,
                IsDeletable = account.IsDeletable,
                NormalBalance = NormalizeNormalBalance(account.NormalBalance, account.AccountCode),
                AccountCategory = NormalizeAccountCategory(account.AccountCategory, account.AccountCode),
                RequireCustomer = account.RequireCustomer,
                RequireSupplier = account.RequireSupplier,
                RequireWarehouse = account.RequireWarehouse,
                RequireItem = account.RequireItem,
                RequireDepartment = account.RequireDepartment,
                RequireEmployee = account.RequireEmployee,
                RequireCurrency = account.RequireCurrency,
                IsFinancialAccount = account.IsFinancialAccount,
                IsManagementAccount = account.IsManagementAccount,
                CreatedAt = account.CreatedAt == default ? DateTime.Now : account.CreatedAt,
                UpdatedAt = account.UpdatedAt == default ? DateTime.Now : account.UpdatedAt,
                IsActive = account.IsActive
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLedgerAccountsTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
IF OBJECT_ID(N'[dbo].[LedgerAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LedgerAccounts]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_LedgerAccounts] PRIMARY KEY,
        [AccountCode] nvarchar(50) NOT NULL,
        [AccountName] nvarchar(255) NOT NULL,
        [ReportName] nvarchar(255) NOT NULL CONSTRAINT [DF_LedgerAccounts_ReportName] DEFAULT(N''),
        [ParentAccountCode] nvarchar(50) NULL,
        [SortOrder] int NOT NULL CONSTRAINT [DF_LedgerAccounts_SortOrder] DEFAULT(0),
        [AccountLevel] int NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountLevel] DEFAULT(1),
        [AccountKind] nvarchar(10) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountKind] DEFAULT(N'D'),
        [AccountNature] nvarchar(10) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountNature] DEFAULT(N'B'),
        [AccountType] nvarchar(40) NOT NULL,
        [IsCashAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsCashAccount] DEFAULT(0),
        [HasForeignCurrency] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_HasForeignCurrency] DEFAULT(0),
        [FollowWarehouse] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_FollowWarehouse] DEFAULT(1),
        [CustomerDebt] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_CustomerDebt] DEFAULT(1),
        [SupplierDebt] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_SupplierDebt] DEFAULT(1),
        [AllowFund] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_AllowFund] DEFAULT(1),
        [AccountingStandardCode] nvarchar(20) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountingStandardCode] DEFAULT(N'TT99'),
        [AllowPosting] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_AllowPosting] DEFAULT(1),
        [IsSystemAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsSystemAccount] DEFAULT(0),
        [IsEditable] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsEditable] DEFAULT(1),
        [IsDeletable] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsDeletable] DEFAULT(1),
        [NormalBalance] nvarchar(20) NOT NULL CONSTRAINT [DF_LedgerAccounts_NormalBalance] DEFAULT(N'Debit'),
        [AccountCategory] nvarchar(50) NULL,
        [RequireCustomer] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireCustomer] DEFAULT(0),
        [RequireSupplier] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireSupplier] DEFAULT(0),
        [RequireWarehouse] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireWarehouse] DEFAULT(0),
        [RequireItem] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireItem] DEFAULT(0),
        [RequireDepartment] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireDepartment] DEFAULT(0),
        [RequireEmployee] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireEmployee] DEFAULT(0),
        [RequireCurrency] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireCurrency] DEFAULT(0),
        [IsFinancialAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsFinancialAccount] DEFAULT(1),
        [IsManagementAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsManagementAccount] DEFAULT(0),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_LedgerAccounts_CreatedAt] DEFAULT(SYSDATETIME()),
        [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_LedgerAccounts_UpdatedAt] DEFAULT(SYSDATETIME()),
        [IsActive] bit NOT NULL
    );
END;

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'SortOrder') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [SortOrder] int NOT NULL CONSTRAINT [DF_LedgerAccounts_SortOrder] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AccountLevel') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AccountLevel] int NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountLevel] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AccountKind') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AccountKind] nvarchar(10) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountKind] DEFAULT(N'D');

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'ReportName') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [ReportName] nvarchar(255) NOT NULL CONSTRAINT [DF_LedgerAccounts_ReportName] DEFAULT(N'');

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AccountNature') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AccountNature] nvarchar(10) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountNature] DEFAULT(N'B');

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsCashAccount') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsCashAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsCashAccount] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'HasForeignCurrency') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [HasForeignCurrency] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_HasForeignCurrency] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'FollowWarehouse') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [FollowWarehouse] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_FollowWarehouse] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'CustomerDebt') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [CustomerDebt] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_CustomerDebt] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'SupplierDebt') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [SupplierDebt] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_SupplierDebt] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AllowFund') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AllowFund] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_AllowFund] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AccountingStandardCode') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AccountingStandardCode] nvarchar(20) NOT NULL CONSTRAINT [DF_LedgerAccounts_AccountingStandardCode] DEFAULT(N'TT99');

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AllowPosting') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AllowPosting] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_AllowPosting] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsSystemAccount') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsSystemAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsSystemAccount] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsEditable') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsEditable] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsEditable] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsDeletable') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsDeletable] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsDeletable] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'NormalBalance') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [NormalBalance] nvarchar(20) NOT NULL CONSTRAINT [DF_LedgerAccounts_NormalBalance] DEFAULT(N'Debit');

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'AccountCategory') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [AccountCategory] nvarchar(50) NULL;

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireCustomer') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireCustomer] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireCustomer] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireSupplier') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireSupplier] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireSupplier] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireWarehouse') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireWarehouse] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireWarehouse] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireItem') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireItem] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireItem] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireDepartment') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireDepartment] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireDepartment] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireEmployee') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireEmployee] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireEmployee] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'RequireCurrency') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [RequireCurrency] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_RequireCurrency] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsFinancialAccount') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsFinancialAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsFinancialAccount] DEFAULT(1);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'IsManagementAccount') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [IsManagementAccount] bit NOT NULL CONSTRAINT [DF_LedgerAccounts_IsManagementAccount] DEFAULT(0);

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'CreatedAt') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_LedgerAccounts_CreatedAt] DEFAULT(SYSDATETIME());

IF COL_LENGTH(N'[dbo].[LedgerAccounts]', N'UpdatedAt') IS NULL
    ALTER TABLE [dbo].[LedgerAccounts] ADD [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_LedgerAccounts_UpdatedAt] DEFAULT(SYSDATETIME());

UPDATE [dbo].[LedgerAccounts]
SET [AllowPosting] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE [AllowPosting] END,
    [IsDeletable] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE [IsDeletable] END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_LedgerAccounts_AccountCode'
      AND [object_id] = OBJECT_ID(N'[dbo].[LedgerAccounts]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_LedgerAccounts_AccountCode]
    ON [dbo].[LedgerAccounts]([AccountCode]);
END;
""";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static LedgerAccountDto MapAccount(ASMILE.BO.Core.Entities.LedgerAccount account)
    {
        return new LedgerAccountDto
        {
            Id = account.Id.ToString("N"),
            SortOrder = account.SortOrder,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            ReportName = account.ReportName,
            ParentAccountCode = account.ParentAccountCode,
            AccountLevel = account.AccountLevel <= 0 ? GetAccountLevel(account.AccountCode, account.ParentAccountCode) : account.AccountLevel,
            AccountKind = NormalizeAccountKind(account.AccountKind, account.AccountCode, account.ParentAccountCode),
            AccountNature = NormalizeAccountNature(account.AccountNature),
            AccountType = Enum.TryParse<LedgerAccountType>(account.AccountType, true, out var type) ? type : GetAccountType(account.AccountCode),
            IsCashAccount = account.IsCashAccount,
            HasForeignCurrency = account.HasForeignCurrency,
            FollowWarehouse = account.FollowWarehouse,
            CustomerDebt = account.CustomerDebt,
            SupplierDebt = account.SupplierDebt,
            AllowFund = account.AllowFund,
            AccountingStandardCode = NormalizeAccountingStandardCode(account.AccountingStandardCode),
            AllowPosting = account.AllowPosting,
            IsSystemAccount = account.IsSystemAccount,
            IsEditable = account.IsEditable,
            IsDeletable = account.IsDeletable,
            NormalBalance = NormalizeNormalBalance(account.NormalBalance, account.AccountCode),
            AccountCategory = NormalizeAccountCategory(account.AccountCategory, account.AccountCode),
            RequireCustomer = account.RequireCustomer,
            RequireSupplier = account.RequireSupplier,
            RequireWarehouse = account.RequireWarehouse,
            RequireItem = account.RequireItem,
            RequireDepartment = account.RequireDepartment,
            RequireEmployee = account.RequireEmployee,
            RequireCurrency = account.RequireCurrency,
            IsFinancialAccount = account.IsFinancialAccount,
            IsManagementAccount = account.IsManagementAccount,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            IsActive = account.IsActive
        };
    }

    private static IEnumerable<LedgerEntryDto> ApplyFilter(IEnumerable<LedgerEntryDto> rows, LedgerFilterDto filter)
    {
        var query = rows;
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x =>
                x.EntryNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.DebitAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.CreditAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountCode))
        {
            query = query.Where(x => string.Equals(x.DebitAccount, filter.AccountCode, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(x.CreditAccount, filter.AccountCode, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.EntryDate.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.EntryDate.Date <= filter.ToDate.Value.Date);
        }

        if (filter.DocumentType.HasValue)
        {
            query = query.Where(x => x.DocumentType == filter.DocumentType.Value);
        }

        return query;
    }

    private static void AddBalance(Dictionary<string, LedgerBalanceDto> rows, string accountCode, decimal debit, decimal credit, bool opening)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
        {
            return;
        }

        if (!rows.TryGetValue(accountCode, out var row))
        {
            row = new LedgerBalanceDto
            {
                AccountCode = accountCode,
                AccountName = GetAccountName(accountCode)
            };
            rows[accountCode] = row;
        }

        if (opening)
        {
            row.OpeningDebit += debit;
            row.OpeningCredit += credit;
        }
        else
        {
            row.PeriodDebit += debit;
            row.PeriodCredit += credit;
        }
    }

    private static void AddEntry(
        List<LedgerEntryDto> rows,
        string entryNo,
        DateTime entryDate,
        string documentNo,
        LedgerDocumentType documentType,
        string description,
        string debitAccount,
        string creditAccount,
        decimal amount,
        string source,
        string sourceId)
    {
        rows.Add(new LedgerEntryDto
        {
            Id = $"{source}:{sourceId}:{debitAccount}:{creditAccount}",
            EntryNo = entryNo,
            EntryDate = entryDate,
            DocumentNo = documentNo,
            DocumentType = documentType,
            Description = description,
            DebitAccount = debitAccount,
            CreditAccount = creditAccount,
            Amount = amount,
            SourceType = source,
            SourceNo = documentNo,
            Status = "Legacy",
            StatusDisplay = "Dữ liệu cũ",
            Currency = "VND"
        });
    }

    private async Task<GeneralLedgerAccountSettings> LoadAccountSettingsAsync()
    {
        async Task<string> GetValueAsync(string key, string fallback)
        {
            var value = await _settingsService.GetValueAsync(key, SettingScope.App, "general-ledger");
            return string.IsNullOrWhiteSpace(value?.Value) ? fallback : value.Value.Trim();
        }

        return new GeneralLedgerAccountSettings
        {
            CashAccount = await GetValueAsync("GeneralLedger.CashAccount", "111"),
            BankAccount = await GetValueAsync("GeneralLedger.BankAccount", "112"),
            ReceivableAccount = await GetValueAsync("GeneralLedger.ReceivableAccount", "131"),
            InventoryAccount = await GetValueAsync("GeneralLedger.InventoryAccount", "156"),
            PayableAccount = await GetValueAsync("GeneralLedger.PayableAccount", "331"),
            RevenueAccount = await GetValueAsync("GeneralLedger.RevenueAccount", "511"),
            CostOfGoodsSoldAccount = await GetValueAsync("GeneralLedger.CostOfGoodsSoldAccount", "632"),
            AdjustmentAccount = await GetValueAsync("GeneralLedger.AdjustmentAccount", "338")
        };
    }

    private static (LedgerDocumentType documentType, string debitAccount, string creditAccount)? MapBusinessDocumentAccounts(ASMILE.BO.Core.Entities.BusinessDocument document, GeneralLedgerAccountSettings accounts)
    {
        return document.EntityType switch
        {
            BusinessEntityType.Customer => document.DocumentType switch
            {
                BusinessDocumentType.Invoice or BusinessDocumentType.ExportVoucher => (LedgerDocumentType.ExportVoucher, accounts.ReceivableAccount, accounts.RevenueAccount),
                BusinessDocumentType.PaymentReceipt or BusinessDocumentType.PaymentVoucher => (LedgerDocumentType.PaymentReceipt, accounts.CashAccount, accounts.ReceivableAccount),
                BusinessDocumentType.AdvancePayment => (LedgerDocumentType.AdvancePayment, accounts.CashAccount, accounts.ReceivableAccount),
                _ => null
            },
            BusinessEntityType.Supplier => document.DocumentType switch
            {
                BusinessDocumentType.Invoice or BusinessDocumentType.ImportVoucher => (LedgerDocumentType.ImportVoucher, accounts.InventoryAccount, accounts.PayableAccount),
                BusinessDocumentType.PaymentVoucher => (LedgerDocumentType.PaymentVoucher, accounts.PayableAccount, accounts.CashAccount),
                BusinessDocumentType.AdvancePayment => (LedgerDocumentType.AdvancePayment, accounts.PayableAccount, accounts.CashAccount),
                _ => null
            },
            _ => null
        };
    }

    private static string NormalizeAccount(string? accountCode, string fallback)
        => string.IsNullOrWhiteSpace(accountCode) ? fallback : accountCode.Trim();

    private static string GetAccountName(string accountCode) => accountCode switch
    {
        "111" => "Tiền mặt",
        "112" => "Tiền gửi ngân hàng",
        "131" => "Phải thu khách hàng",
        "138" => "Phải thu khác",
        "152" => "Nguyên liệu, vật liệu",
        "153" => "Công cụ, dụng cụ",
        "156" => "Hàng hóa",
        "331" => "Phải trả người bán",
        "338" => "Phải trả, phải nộp khác",
        "411" => "Vốn góp của chủ sở hữu",
        "421" => "Lợi nhuận sau thuế chưa phân phối",
        "511" => "Doanh thu bán hàng",
        "515" => "Doanh thu hoạt động tài chính",
        "632" => "Giá vốn hàng bán",
        "642" => "Chi phí quản lý",
        "711" => "Thu nhập khác",
        _ => $"Tài khoản {accountCode}"
    };

    private static LedgerAccountType GetAccountType(string accountCode)
    {
        if (accountCode.StartsWith('1')) return LedgerAccountType.Asset;
        if (accountCode.StartsWith('2') || accountCode.StartsWith('3')) return LedgerAccountType.Liability;
        if (accountCode.StartsWith('4')) return LedgerAccountType.Equity;
        if (accountCode.StartsWith('5') || accountCode.StartsWith('7')) return LedgerAccountType.Revenue;
        if (accountCode.StartsWith('6') || accountCode.StartsWith('8')) return LedgerAccountType.Expense;
        return LedgerAccountType.Other;
    }

    private static int GetAccountLevel(string accountCode, string? parentAccountCode)
    {
        if (!string.IsNullOrWhiteSpace(parentAccountCode))
        {
            return parentAccountCode.Trim().Length switch
            {
                <= 3 => 2,
                4 => 3,
                _ => 4
            };
        }

        return accountCode.Trim().Length switch
        {
            <= 3 => 1,
            4 => 2,
            5 => 3,
            _ => 4
        };
    }

    private static string NormalizeAccountKind(string? accountKind, string accountCode, string? parentAccountCode)
    {
        var value = (accountKind ?? string.Empty).Trim().ToUpperInvariant();
        if (value is "H" or "D")
        {
            return value;
        }

        return string.IsNullOrWhiteSpace(parentAccountCode) || accountCode.Length <= 4 ? "H" : "D";
    }

    private static string NormalizeAccountNature(string? accountNature)
    {
        var value = (accountNature ?? string.Empty).Trim().ToUpperInvariant();
        return value is "D" or "C" or "B" or "P" or "M" ? value : "B";
    }

    private static string NormalizeAccountingStandardCode(string? value)
    {
        var code = (value ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(code) ? "TT99" : code;
    }

    private static string NormalizeNormalBalance(string? value, string accountCode)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Equals("Debit", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Credit", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            return char.ToUpperInvariant(normalized[0]) + normalized[1..].ToLowerInvariant();
        }

        return accountCode.StartsWith('2') || accountCode.StartsWith('3') || accountCode.StartsWith('4') || accountCode.StartsWith('5') || accountCode.StartsWith('7')
            ? "Credit"
            : "Debit";
    }

    private static string NormalizeAccountCategory(string? value, string accountCode)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized is "Asset" or "Liability" or "Equity" or "Revenue" or "Expense" or "Other")
        {
            return normalized;
        }

        if (accountCode.StartsWith('1')) return "Asset";
        if (accountCode.StartsWith('2') || accountCode.StartsWith('3')) return "Liability";
        if (accountCode.StartsWith('4')) return "Equity";
        if (accountCode.StartsWith('5') || accountCode.StartsWith('7')) return "Revenue";
        if (accountCode.StartsWith('6') || accountCode.StartsWith('8')) return "Expense";
        return "Other";
    }

    private static IReadOnlyList<LedgerAccountDto> GetDefaultLedgerAccounts()
    {
        var rows = new (int order, string code, string name, string? parent, int level, string kind)[]
        {
            (6, "111", "Tiền Mặt", null, 1, "H"),
            (7, "1111", "Tiền Mặt Việt nam", "111", 2, "H"),
            (8, "11111", "Quỹ tiền mặt VNĐ Kế toán", "1111", 3, "D"),
            (9, "11112", "Quỹ tiền mặt VNĐ Phòng Sales", "1111", 3, "D"),
            (10, "1112", "Tiền mặt Ngoại tệ", "111", 2, "H"),
            (11, "11121", "Quỹ Kế toán Ngoại tệ (USD)", "1112", 3, "H"),
            (12, "111211", "Quỹ tiền mặt USD - KE TOAN", "11121", 4, "D"),
            (13, "112", "Tiền gửi Ngân hàng", null, 1, "H"),
            (14, "1121", "Tiền gửi Ngân hàng VNĐ", "112", 2, "H"),
            (15, "11211", "Tài Khoản VIETCOMBANK (VNĐ)", "1121", 3, "D"),
            (16, "11213", "Tiền gửi ngân hàng Ngoại Thương 2", "1121", 3, "D"),
            (17, "11215", "Tài khoản Agribank MC - GD2", "1121", 3, "D"),
            (18, "1122", "Tiền gửi Ngân hàng Ngoại tệ", "112", 2, "H"),
            (19, "11221", "Tài khoản USD - VCB", "1122", 3, "D"),
            (20, "11223", "Tài khoản USD - Agribank MC", "1122", 3, "D"),
            (21, "113", "Tiền đang chuyển", null, 1, "H"),
            (22, "1131", "Tiền đang chuyển", "113", 2, "D"),
            (23, "1132", "Thanh toan the qua UOB", "113", 2, "D"),
            (24, "121", "Đầu tư chứng khoán ngắn hạn", null, 1, "H"),
            (25, "1211", "Cổ phiếu", "121", 2, "D"),
            (26, "1212", "Trái phiếu, tín phiếu, kỳ phiếu", "121", 2, "D"),
            (27, "131", "Phải thu khách hàng", null, 1, "H"),
            (28, "1311", "Phải thu khách hàng trong nước", "131", 2, "D"),
            (29, "156", "Hàng hóa", null, 1, "H"),
            (30, "1561", "Giá mua hàng hóa", "156", 2, "D"),
            (31, "331", "Phải trả người bán", null, 1, "H"),
            (32, "3311", "Phải trả nhà cung cấp trong nước", "331", 2, "D"),
            (33, "338", "Phải trả, phải nộp khác", null, 1, "H"),
            (34, "511", "Doanh thu bán hàng", null, 1, "H"),
            (35, "5111", "Doanh thu bán hàng hóa", "511", 2, "D"),
            (36, "632", "Giá vốn hàng bán", null, 1, "H"),
            (37, "6321", "Giá vốn hàng hóa", "632", 2, "D")
        };

        return rows.Select(x => new LedgerAccountDto
        {
            Id = Guid.NewGuid().ToString("N"),
            SortOrder = x.order,
            AccountCode = x.code,
            AccountName = x.name,
            ParentAccountCode = x.parent,
            AccountLevel = x.level,
            AccountKind = x.kind,
            AccountType = GetAccountType(x.code),
            AccountingStandardCode = "TT99",
            AllowPosting = !x.kind.Equals("H", StringComparison.OrdinalIgnoreCase),
            IsEditable = true,
            IsDeletable = !x.kind.Equals("H", StringComparison.OrdinalIgnoreCase),
            NormalBalance = NormalizeNormalBalance(null, x.code),
            AccountCategory = NormalizeAccountCategory(null, x.code),
            IsFinancialAccount = !x.code.StartsWith('9'),
            IsManagementAccount = x.code.StartsWith('9'),
            IsActive = true
        }).ToList();
    }
}

internal sealed class GeneralLedgerAccountSettings
{
    public string CashAccount { get; init; } = "111";
    public string BankAccount { get; init; } = "112";
    public string ReceivableAccount { get; init; } = "131";
    public string InventoryAccount { get; init; } = "156";
    public string PayableAccount { get; init; } = "331";
    public string RevenueAccount { get; init; } = "511";
    public string CostOfGoodsSoldAccount { get; init; } = "632";
    public string AdjustmentAccount { get; init; } = "338";
}

