using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Services;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockGeneralLedgerService : IGeneralLedgerService
{
    private readonly IAuditLogService _auditLogService;
    private readonly List<LedgerAccountDto> _accounts;
    private readonly List<LedgerEntryDto> _entries;

    public MockGeneralLedgerService(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        _accounts = BuildAccounts();
        _entries = BuildEntries();
    }

    public Task<IReadOnlyList<LedgerAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LedgerAccountDto>>(_accounts.OrderBy(x => x.AccountCode).ToList());

    public Task SaveAccountsAsync(IReadOnlyList<LedgerAccountDto> accounts, CancellationToken cancellationToken = default)
    {
        _accounts.Clear();
        _accounts.AddRange(accounts);
        return Task.CompletedTask;
    }

    public Task DeleteAccountAsync(string accountCode, CancellationToken cancellationToken = default)
    {
        _accounts.RemoveAll(x => string.Equals(x.AccountCode, accountCode, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LedgerEntryDto>> GetLedgerEntriesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _entries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.EntryNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.DebitAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.CreditAccount.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountCode))
        {
            query = query.Where(x => x.DebitAccount == filter.AccountCode || x.CreditAccount == filter.AccountCode);
        }

        if (filter.FromDate.HasValue) query = query.Where(x => x.EntryDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) query = query.Where(x => x.EntryDate.Date <= filter.ToDate.Value.Date);
        if (filter.DocumentType.HasValue) query = query.Where(x => x.DocumentType == filter.DocumentType.Value);

        return Task.FromResult<IReadOnlyList<LedgerEntryDto>>(query.OrderByDescending(x => x.EntryDate).ThenByDescending(x => x.EntryNo).ToList());
    }

    public async Task<IReadOnlyList<LedgerBalanceDto>> GetLedgerBalancesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default)
    {
        var entries = await GetLedgerEntriesAsync(filter, cancellationToken);
        var lines = new List<(string account, string accountName, decimal debit, decimal credit)>();

        foreach (var entry in entries)
        {
            var debitName = _accounts.FirstOrDefault(x => x.AccountCode == entry.DebitAccount)?.AccountName ?? entry.DebitAccount;
            var creditName = _accounts.FirstOrDefault(x => x.AccountCode == entry.CreditAccount)?.AccountName ?? entry.CreditAccount;
            lines.Add((entry.DebitAccount, debitName, entry.Amount, 0));
            lines.Add((entry.CreditAccount, creditName, 0, entry.Amount));
        }

        var balances = lines.GroupBy(x => new { x.account, x.accountName })
            .Select(g => new LedgerBalanceDto
            {
                AccountCode = g.Key.account,
                AccountName = g.Key.accountName,
                OpeningDebit = 0,
                OpeningCredit = 0,
                PeriodDebit = g.Sum(x => x.debit),
                PeriodCredit = g.Sum(x => x.credit),
                ClosingDebit = Math.Max(0, g.Sum(x => x.debit) - g.Sum(x => x.credit)),
                ClosingCredit = Math.Max(0, g.Sum(x => x.credit) - g.Sum(x => x.debit))
            })
            .OrderBy(x => x.AccountCode)
            .ToList();

        return balances;
    }

    public async Task CreateManualEntryAsync(CreateLedgerEntryRequest request, CancellationToken cancellationToken = default)
    {
        var nextNo = $"GL{DateTime.Now:yyMMddHHmm}";
        _entries.Add(new LedgerEntryDto
        {
            Id = Guid.NewGuid().ToString("N"),
            EntryNo = nextNo,
            EntryDate = request.EntryDate,
            DocumentNo = request.DocumentNo,
            DocumentType = request.DocumentType,
            Description = request.Description,
            DebitAccount = request.DebitAccount,
            CreditAccount = request.CreditAccount,
            Amount = request.Amount
        });

        await _auditLogService.LogAsync("LedgerManualEntryCreated", "Finance.GeneralLedger", $"EntryNo={nextNo}; Amount={request.Amount:0.##}", cancellationToken);
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
        await _auditLogService.LogAsync("LedgerExport", "Finance.GeneralLedger", $"Export path D:/ACCOUNTING/So Cai/{DateTime.Today:yyyy/MM}", cancellationToken);
    }

    private static List<LedgerAccountDto> BuildAccounts() =>
    [
        new() { Id = "1", AccountCode = "111", AccountName = "Tien mat", AccountType = LedgerAccountType.Asset, IsActive = true },
        new() { Id = "2", AccountCode = "112", AccountName = "Tien gui ngan hang", AccountType = LedgerAccountType.Asset, IsActive = true },
        new() { Id = "3", AccountCode = "131", AccountName = "Phai thu khach hang", AccountType = LedgerAccountType.Asset, IsActive = true },
        new() { Id = "4", AccountCode = "156", AccountName = "Hang hoa", AccountType = LedgerAccountType.Asset, IsActive = true },
        new() { Id = "5", AccountCode = "331", AccountName = "Phai tra nha cung cap", AccountType = LedgerAccountType.Liability, IsActive = true },
        new() { Id = "6", AccountCode = "511", AccountName = "Doanh thu ban hang", AccountType = LedgerAccountType.Revenue, IsActive = true },
        new() { Id = "7", AccountCode = "632", AccountName = "Gia von hang ban", AccountType = LedgerAccountType.Expense, IsActive = true },
        new() { Id = "8", AccountCode = "642", AccountName = "Chi phi quan ly", AccountType = LedgerAccountType.Expense, IsActive = true }
    ];

    private static List<LedgerEntryDto> BuildEntries() =>
    [
        new() { Id = "e1", EntryNo = "GL0001", EntryDate = DateTime.Today.AddDays(-15), DocumentNo = "PX00018", DocumentType = LedgerDocumentType.ExportVoucher, Description = "Ghi nhan doanh thu", DebitAccount = "131", CreditAccount = "511", Amount = 120_000_000 },
        new() { Id = "e2", EntryNo = "GL0002", EntryDate = DateTime.Today.AddDays(-15), DocumentNo = "PX00018", DocumentType = LedgerDocumentType.ExportVoucher, Description = "Ket chuyen gia von", DebitAccount = "632", CreditAccount = "156", Amount = 73_000_000 },
        new() { Id = "e3", EntryNo = "GL0003", EntryDate = DateTime.Today.AddDays(-12), DocumentNo = "PN00024", DocumentType = LedgerDocumentType.ImportVoucher, Description = "Nhap hang", DebitAccount = "156", CreditAccount = "331", Amount = 135_000_000 },
        new() { Id = "e4", EntryNo = "GL0004", EntryDate = DateTime.Today.AddDays(-9), DocumentNo = "UNC00012", DocumentType = LedgerDocumentType.PaymentVoucher, Description = "Thanh toan NCC", DebitAccount = "331", CreditAccount = "112", Amount = 45_000_000 },
        new() { Id = "e5", EntryNo = "GL0005", EntryDate = DateTime.Today.AddDays(-7), DocumentNo = "PT00001", DocumentType = LedgerDocumentType.PaymentReceipt, Description = "Thu tien KH", DebitAccount = "111", CreditAccount = "131", Amount = 50_000_000 }
    ];
}
