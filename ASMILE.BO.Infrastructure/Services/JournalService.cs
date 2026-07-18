using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class JournalService : IJournalService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService? _auditLogService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IDebtTransactionService? _debtTransactionService;
    private readonly ITaxService? _taxService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;

    public JournalService(AppDbContext dbContext, IAccountingPeriodService accountingPeriodService, IAuditLogService? auditLogService = null, IDebtTransactionService? debtTransactionService = null, ITaxService? taxService = null, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null)
    {
        _dbContext = dbContext;
        _accountingPeriodService = accountingPeriodService;
        _auditLogService = auditLogService;
        _debtTransactionService = debtTransactionService;
        _taxService = taxService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
    }

    public async Task<JournalHeader?> GetJournalAsync(Guid journalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JournalHeaders
            .Include(x => x.Lines.OrderBy(line => line.LineNo))
            .FirstOrDefaultAsync(x => x.Id == journalId, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalHeader>> GetJournalsBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JournalHeaders
            .Include(x => x.Lines.OrderBy(line => line.LineNo))
            .Where(x => x.SourceType == sourceType && x.SourceId == sourceId)
            .OrderByDescending(x => x.PostingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<JournalHeader> CreateDraftAsync(JournalHeader journal, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        PrepareJournal(journal, JournalStatus.Draft);
        var validation = await ValidateAsync(journal, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors.Select(x => x.Message)));
        }

        _dbContext.JournalHeaders.Add(journal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Journal.CreateDraft", $"Created draft journal {journal.JournalNo}.", journal.CreatedBy, cancellationToken);
        return journal;
    }

    public async Task<JournalHeader> PostDraftAsync(Guid journalId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var journal = await GetJournalAsync(journalId, cancellationToken)
            ?? throw new InvalidOperationException("Journal not found.");

        if (journal.Status != JournalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft journals can be posted.");
        }

        PrepareJournal(journal, JournalStatus.Draft);
        var validation = await ValidateAsync(journal, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors.Select(x => x.Message)));
        }

        await _accountingPeriodService.ValidatePostingDateAsync(journal.PostingDate, cancellationToken);

        journal.Status = JournalStatus.Posted;
        journal.PostedBy = postedBy ?? journal.CreatedBy;
        journal.PostedAt = DateTime.UtcNow;

        EnsurePostingSource(journal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_debtTransactionService is not null)
        {
            await _debtTransactionService.SyncFromJournalAsync(journal.Id, cancellationToken);
        }
        if (_taxService is not null)
        {
            await _taxService.SyncVatFromJournalAsync(journal.Id, cancellationToken);
        }

        await LogAsync("Journal.Post", $"Posted journal {journal.JournalNo}.", journal.PostedBy ?? "system", cancellationToken);
        await AuditAsync("JournalHeader", journal.Id.ToString(), "Post", null, new { journal.JournalNo, journal.Status, journal.TotalDebit, journal.TotalCredit }, journal.PostedBy, cancellationToken);
        return journal;
    }

    public async Task<JournalHeader> ReverseAsync(Guid journalId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var original = await GetJournalAsync(journalId, cancellationToken)
            ?? throw new InvalidOperationException("Journal not found.");

        if (original.Status != JournalStatus.Posted)
        {
            throw new InvalidOperationException("Only posted journals can be reversed.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(original.PostingDate, cancellationToken);
        await _accountingPeriodService.ValidatePostingDateAsync(DateTime.UtcNow.Date, cancellationToken);

        var user = string.IsNullOrWhiteSpace(reversedBy) ? "system" : reversedBy;
        var reversal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = await GenerateJournalNoAsync(cancellationToken),
            JournalDate = DateTime.UtcNow.Date,
            PostingDate = DateTime.UtcNow.Date,
            SourceType = "Reversal",
            SourceId = original.Id,
            SourceNo = original.JournalNo,
            Description = $"Đảo bút toán: {original.JournalNo} - {reason}",
            PeriodCode = DateTime.UtcNow.ToString("yyyy-MM"),
            Status = JournalStatus.Posted,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            PostedBy = user,
            PostedAt = DateTime.UtcNow,
            Lines = original.Lines
                .OrderBy(x => x.LineNo)
                .Select((line, index) => new JournalLine
                {
                    Id = Guid.NewGuid(),
                    LineNo = index + 1,
                    AccountCode = line.AccountCode,
                    AccountName = line.AccountName,
                    DebitAmount = line.CreditAmount,
                    CreditAmount = line.DebitAmount,
                    CurrencyCode = line.CurrencyCode,
                    ExchangeRate = line.ExchangeRate,
                    ObjectType = line.ObjectType,
                    ObjectId = line.ObjectId,
                    CustomerId = line.CustomerId,
                    SupplierId = line.SupplierId,
                    WarehouseId = line.WarehouseId,
                    ItemId = line.ItemId,
                    DepartmentId = line.DepartmentId,
                    EmployeeId = line.EmployeeId,
                    Description = $"Đảo dòng {line.LineNo}: {line.Description}"
                })
                .ToList()
        };

        PrepareJournal(reversal, JournalStatus.Posted);
        var validation = await ValidateAsync(reversal, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors.Select(x => x.Message)));
        }

        original.Status = JournalStatus.Reversed;
        original.ReversedBy = user;
        original.ReversedAt = DateTime.UtcNow;
        _dbContext.JournalHeaders.Add(reversal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_debtTransactionService is not null)
        {
            await _debtTransactionService.SyncFromJournalAsync(reversal.Id, cancellationToken);
        }
        if (_taxService is not null)
        {
            await _taxService.SyncVatFromJournalAsync(reversal.Id, cancellationToken);
        }

        await LogAsync("Journal.Reverse", $"Reversed journal {original.JournalNo} with {reversal.JournalNo}. Reason: {reason}", user, cancellationToken);
        await AuditAsync("JournalHeader", original.Id.ToString(), "Reverse", new { original.JournalNo, PreviousStatus = JournalStatus.Posted }, new { original.JournalNo, original.Status, ReversalJournalId = reversal.Id, Reason = reason }, user, cancellationToken);
        return reversal;
    }

    public async Task<JournalValidationResult> ValidateAsync(JournalHeader journal, CancellationToken cancellationToken = default)
    {
        var result = new JournalValidationResult();
        var lines = journal.Lines.OrderBy(x => x.LineNo).ToList();

        if (lines.Count < 2)
        {
            result.Add("JOURNAL_MIN_LINES", "Journal must have at least 2 lines.");
        }

        if (string.IsNullOrWhiteSpace(journal.JournalNo))
        {
            result.Add("JOURNAL_NO_REQUIRED", "JournalNo is required.");
        }
        else
        {
            var duplicate = await _dbContext.JournalHeaders
                .AnyAsync(x => x.Id != journal.Id && x.JournalNo == journal.JournalNo, cancellationToken);
            if (duplicate)
            {
                result.Add("JOURNAL_NO_DUPLICATED", $"JournalNo already exists: {journal.JournalNo}.");
            }
        }

        var periodCode = string.IsNullOrWhiteSpace(journal.PeriodCode)
            ? journal.PostingDate.ToString("yyyy-MM")
            : journal.PeriodCode;
        var period = await _dbContext.AccountingPeriods.FirstOrDefaultAsync(x => x.PeriodCode == periodCode, cancellationToken);
        if (period is { Status: AccountingPeriodStatus.Locked or AccountingPeriodStatus.Closed })
        {
            result.Add("PERIOD_LOCKED", $"Accounting period {periodCode} is {period.Status}.");
        }

        var ledgerHasData = await _dbContext.LedgerAccounts.AnyAsync(cancellationToken);
        foreach (var line in lines)
        {
            if (line.DebitAmount < 0 || line.CreditAmount < 0)
            {
                result.Add("NEGATIVE_AMOUNT", "Debit/Credit amount cannot be negative.", line.LineNo);
            }

            if (line.DebitAmount > 0 && line.CreditAmount > 0)
            {
                result.Add("BOTH_DEBIT_CREDIT", "A journal line cannot have both debit and credit amounts.", line.LineNo);
            }

            if (line.DebitAmount == 0 && line.CreditAmount == 0)
            {
                result.Add("ZERO_AMOUNT", "A journal line must have debit or credit amount.", line.LineNo);
            }

            if (string.IsNullOrWhiteSpace(line.AccountCode))
            {
                result.Add("ACCOUNT_REQUIRED", "AccountCode is required.", line.LineNo);
                continue;
            }

            if (ledgerHasData)
            {
                var account = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(x => x.AccountCode == line.AccountCode, cancellationToken);
                if (account is null)
                {
                    result.Add("ACCOUNT_NOT_FOUND", $"Account does not exist: {line.AccountCode}.", line.LineNo);
                }
                else
                {
                    ValidateAccountMetadata(result, line, account);
                }
            }
        }

        var totalDebit = lines.Sum(x => x.DebitAmount);
        var totalCredit = lines.Sum(x => x.CreditAmount);
        if (totalDebit != totalCredit)
        {
            result.Add("JOURNAL_NOT_BALANCED", $"Total debit {totalDebit:N2} does not equal total credit {totalCredit:N2}.");
        }

        return result;
    }

    private static void ValidateAccountMetadata(JournalValidationResult result, JournalLine line, LedgerAccount account)
    {
        if (!account.AllowPosting)
        {
            result.Add("ACCOUNT_POSTING_DISABLED", $"Tài khoản {account.AccountCode} không cho phép hạch toán trực tiếp.", line.LineNo);
        }

        if (account.RequireCustomer && line.CustomerId is null)
        {
            result.Add("CUSTOMER_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn khách hàng.", line.LineNo);
        }

        if (account.RequireSupplier && line.SupplierId is null)
        {
            result.Add("SUPPLIER_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn nhà cung cấp.", line.LineNo);
        }

        if (account.RequireWarehouse && line.WarehouseId is null)
        {
            result.Add("WAREHOUSE_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn kho.", line.LineNo);
        }

        if (account.RequireItem && line.ItemId is null)
        {
            result.Add("ITEM_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn hàng hóa.", line.LineNo);
        }

        if (account.RequireDepartment && line.DepartmentId is null)
        {
            result.Add("DEPARTMENT_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn bộ phận.", line.LineNo);
        }

        if (account.RequireEmployee && line.EmployeeId is null)
        {
            result.Add("EMPLOYEE_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn nhân sự.", line.LineNo);
        }

        if (account.RequireCurrency && string.IsNullOrWhiteSpace(line.CurrencyCode))
        {
            result.Add("CURRENCY_REQUIRED", $"Tài khoản {account.AccountCode} bắt buộc chọn tiền tệ.", line.LineNo);
        }
    }

    private void PrepareJournal(JournalHeader journal, JournalStatus status)
    {
        if (journal.Id == Guid.Empty)
        {
            journal.Id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(journal.JournalNo))
        {
            journal.JournalNo = GenerateJournalNo();
        }

        if (journal.JournalDate == default)
        {
            journal.JournalDate = journal.PostingDate == default ? DateTime.UtcNow.Date : journal.PostingDate.Date;
        }

        if (journal.PostingDate == default)
        {
            journal.PostingDate = journal.JournalDate == default ? DateTime.UtcNow.Date : journal.JournalDate.Date;
        }

        if (string.IsNullOrWhiteSpace(journal.PeriodCode))
        {
            journal.PeriodCode = journal.PostingDate.ToString("yyyy-MM");
        }

        if (journal.CreatedAt == default)
        {
            journal.CreatedAt = DateTime.UtcNow;
        }

        journal.Status = status;
        var orderedLines = journal.Lines.OrderBy(x => x.LineNo).ToList();
        for (var i = 0; i < orderedLines.Count; i++)
        {
            var line = orderedLines[i];
            if (line.Id == Guid.Empty)
            {
                line.Id = Guid.NewGuid();
            }

            line.JournalHeaderId = journal.Id;
            line.LineNo = line.LineNo <= 0 ? i + 1 : line.LineNo;
            line.CurrencyCode = string.IsNullOrWhiteSpace(line.CurrencyCode) ? "VND" : line.CurrencyCode;
            line.ExchangeRate = line.ExchangeRate <= 0 ? 1m : line.ExchangeRate;
            line.CompanyId ??= journal.CompanyId;
            line.BranchId ??= journal.BranchId;
        }

        journal.TotalDebit = orderedLines.Sum(x => x.DebitAmount);
        journal.TotalCredit = orderedLines.Sum(x => x.CreditAmount);
        journal.Difference = journal.TotalDebit - journal.TotalCredit;
    }

    private void EnsurePostingSource(JournalHeader journal)
    {
        if (!journal.SourceId.HasValue || string.IsNullOrWhiteSpace(journal.SourceType))
        {
            return;
        }

        var exists = _dbContext.PostingSources.Any(x => x.SourceType == journal.SourceType && x.SourceId == journal.SourceId.Value);
        if (exists)
        {
            return;
        }

        _dbContext.PostingSources.Add(new PostingSource
        {
            Id = Guid.NewGuid(),
            SourceType = journal.SourceType,
            SourceId = journal.SourceId.Value,
            SourceNo = journal.SourceNo,
            JournalHeaderId = journal.Id,
            Status = journal.Status.ToString(),
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<string> GenerateJournalNoAsync(CancellationToken cancellationToken)
    {
        string journalNo;
        do
        {
            journalNo = GenerateJournalNo();
        }
        while (await _dbContext.JournalHeaders.AnyAsync(x => x.JournalNo == journalNo, cancellationToken));

        return journalNo;
    }

    private static string GenerateJournalNo()
        => $"JV-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 99999):D5}";

    private async Task LogAsync(string action, string detail, string username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, username, cancellationToken);
        }
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, string? userName, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, userName, correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }
}
