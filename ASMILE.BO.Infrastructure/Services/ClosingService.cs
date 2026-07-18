using System.Globalization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ClosingService : IClosingService
{
    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;

    public ClosingService(
        AppDbContext dbContext,
        IJournalService journalService,
        IAccountingPeriodService accountingPeriodService,
        IAuditLogService? auditLogService = null,
        IAuditService? auditService = null,
        IAuditCorrelationService? auditCorrelationService = null)
    {
        _dbContext = dbContext;
        _journalService = journalService;
        _accountingPeriodService = accountingPeriodService;
        _auditLogService = auditLogService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
    }

    public async Task<ClosingEntryDto> PreviewClosingAsync(string period, CancellationToken cancellationToken = default)
    {
        var range = PeriodRange.From(period);
        var incomeSummaryAccount = await ResolveAccountByRoleAsync("IncomeSummary", cancellationToken);
        var retainedEarningsAccount = await ResolveAccountByRoleAsync("RetainedEarnings", cancellationToken);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);
        var lines = await LoadPeriodLinesAsync(range, cancellationToken);
        var journalLines = BuildClosingJournalLines(lines, accounts, incomeSummaryAccount, await GetAccountNameAsync(incomeSummaryAccount, cancellationToken), retainedEarningsAccount, await GetAccountNameAsync(retainedEarningsAccount, cancellationToken), range);
        if (journalLines.Count == 0)
        {
            throw new InvalidOperationException($"Kỳ {range.Period} không có phát sinh doanh thu/chi phí để kết chuyển.");
        }

        return new ClosingEntryDto
        {
            Id = Guid.Empty,
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            ClosingDate = range.ToDate.Date,
            Status = "Preview",
            Notes = "Preview closing only",
            Lines = journalLines
                .OrderBy(x => x.LineNo)
                .Select(x => new ClosingEntryLineDto
                {
                    AccountCode = x.AccountCode,
                    DebitAmount = x.DebitAmount,
                    CreditAmount = x.CreditAmount
                })
                .ToList()
        };
    }

    public async Task<ClosingEntryDto> ClosePeriodAsync(string period, string? closedBy = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var range = PeriodRange.From(period);
        await _accountingPeriodService.ValidatePostingDateAsync(range.ToDate.Date, cancellationToken);

        var duplicate = await _dbContext.ClosingEntries
            .AnyAsync(x => x.Period == range.Period && x.Status == "Posted", cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Kỳ {range.Period} đã có bút toán kết chuyển.");
        }

        var incomeSummaryAccount = await ResolveAccountByRoleAsync("IncomeSummary", cancellationToken);
        var retainedEarningsAccount = await ResolveAccountByRoleAsync("RetainedEarnings", cancellationToken);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);
        var lines = await LoadPeriodLinesAsync(range, cancellationToken);

        var incomeSummaryName = await GetAccountNameAsync(incomeSummaryAccount, cancellationToken);
        var retainedEarningsName = await GetAccountNameAsync(retainedEarningsAccount, cancellationToken);
        var journalLines = BuildClosingJournalLines(lines, accounts, incomeSummaryAccount, incomeSummaryName, retainedEarningsAccount, retainedEarningsName, range);
        if (journalLines.Count == 0)
        {
            throw new InvalidOperationException($"Kỳ {range.Period} không có phát sinh doanh thu/chi phí để kết chuyển.");
        }

        var user = string.IsNullOrWhiteSpace(closedBy) ? "system" : closedBy.Trim();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var closingEntry = new ClosingEntry
        {
            Id = Guid.NewGuid(),
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            ClosingDate = range.ToDate.Date,
            Status = "Draft",
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ClosingEntries.Add(closingEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = $"CL-{range.Period.Replace("-", string.Empty, StringComparison.Ordinal)}-{DateTime.UtcNow:HHmmss}",
            JournalDate = range.ToDate.Date,
            PostingDate = range.ToDate.Date,
            SourceType = "Closing",
            SourceId = closingEntry.Id,
            SourceNo = $"CLOSE-{range.Period}",
            Description = $"Kết chuyển kỳ {range.Period}",
            PeriodCode = range.Period,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = journalLines
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        var posted = await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);

        closingEntry.JournalHeaderId = posted.Id;
        closingEntry.Status = "Posted";
        var closingLines = posted.Lines
            .OrderBy(x => x.LineNo)
            .Select(x => new ClosingEntryLine
            {
                Id = Guid.NewGuid(),
                ClosingEntryId = closingEntry.Id,
                AccountCode = x.AccountCode,
                DebitAmount = x.DebitAmount,
                CreditAmount = x.CreditAmount
            })
            .ToList();
        _dbContext.ClosingEntryLines.AddRange(closingLines);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await LogAsync("Closing.Post", $"Kết chuyển kỳ {range.Period} bằng bút toán {posted.JournalNo}.", user, cancellationToken);
        await AuditAsync("ClosingEntry", closingEntry.Id.ToString(), "Post", null, new { closingEntry.Period, closingEntry.Status, closingEntry.JournalHeaderId }, user, cancellationToken);
        return ToDto(closingEntry);
    }

    public async Task<IReadOnlyList<ClosingEntryDto>> GetClosingEntriesAsync(string? period = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClosingEntries.AsNoTracking()
            .Include(x => x.Lines)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(period))
        {
            var range = PeriodRange.From(period);
            query = query.Where(x => x.Period == range.Period);
        }

        return await query
            .OrderByDescending(x => x.ClosingDate)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClosingEntryDto> ReverseClosingAsync(Guid closingEntryId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var closingEntry = await _dbContext.ClosingEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == closingEntryId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy bút toán kết chuyển.");

        if (closingEntry.Status != "Posted" || closingEntry.JournalHeaderId is null)
        {
            throw new InvalidOperationException("Chỉ đảo được bút toán kết chuyển đã ghi sổ.");
        }

        var user = string.IsNullOrWhiteSpace(reversedBy) ? "system" : reversedBy.Trim();
        await _journalService.ReverseAsync(closingEntry.JournalHeaderId.Value, reason, user, cancellationToken);
        closingEntry.Status = "Reversed";
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Closing.Reverse", $"Đảo bút toán kết chuyển kỳ {closingEntry.Period}. Lý do: {reason}", user, cancellationToken);
        await AuditAsync("ClosingEntry", closingEntry.Id.ToString(), "Reverse", null, new { closingEntry.Period, closingEntry.Status, Reason = reason }, user, cancellationToken);
        return ToDto(closingEntry);
    }

    private async Task<List<JournalLineProjection>> LoadPeriodLinesAsync(PeriodRange range, CancellationToken cancellationToken)
    {
        var headers = await _dbContext.JournalHeaders
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == JournalStatus.Posted && x.PostingDate >= range.FromDate && x.PostingDate <= range.ToDate)
            .ToListAsync(cancellationToken);

        return headers
            .SelectMany(header => header.Lines.Select(line => new JournalLineProjection(line.AccountCode, line.AccountName, line.DebitAmount, line.CreditAmount)))
            .ToList();
    }

    private async Task<string> ResolveAccountByRoleAsync(string roleCode, CancellationToken cancellationToken)
    {
        var account = await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.IsActive && x.RoleCode == roleCode)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.AccountingStandardCode == "TT99" ? 0 : 1)
            .Select(x => x.AccountCode)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(account)
            ? throw new InvalidOperationException($"Thiếu AccountRoleMapping cho vai trò {roleCode}.")
            : account;
    }

    private async Task<string> GetAccountNameAsync(string accountCode, CancellationToken cancellationToken)
    {
        return await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(x => x.AccountCode == accountCode)
            .Select(x => x.AccountName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? accountCode;
    }

    private static void AddClosingLine(List<JournalLine> lines, ref int lineNo, string accountCode, string accountName, decimal debit, decimal credit, string description)
    {
        if (debit == 0 && credit == 0)
        {
            return;
        }

        lines.Add(new JournalLine
        {
            Id = Guid.NewGuid(),
            LineNo = lineNo++,
            AccountCode = accountCode,
            AccountName = accountName,
            DebitAmount = debit,
            CreditAmount = credit,
            CurrencyCode = "VND",
            ExchangeRate = 1m,
            Description = description
        });
    }

    private static List<JournalLine> BuildClosingJournalLines(
        IReadOnlyList<JournalLineProjection> lines,
        IReadOnlyDictionary<string, LedgerAccount> accounts,
        string incomeSummaryAccount,
        string incomeSummaryName,
        string retainedEarningsAccount,
        string retainedEarningsName,
        PeriodRange range)
    {
        var journalLines = new List<JournalLine>();
        var lineNo = 1;
        var revenueTotal = 0m;
        var expenseTotal = 0m;

        foreach (var group in lines.GroupBy(x => x.AccountCode).OrderBy(x => x.Key))
        {
            accounts.TryGetValue(group.Key, out var account);
            var category = ResolveCategory(group.Key, account?.AccountCategory);
            var netCredit = group.Sum(x => x.CreditAmount - x.DebitAmount);

            if (category == "Revenue")
            {
                if (netCredit == 0) continue;
                revenueTotal += netCredit;
                AddClosingLine(journalLines, ref lineNo, group.Key, account?.AccountName ?? group.Key, netCredit > 0 ? netCredit : 0m, netCredit < 0 ? -netCredit : 0m, $"Kết chuyển doanh thu {group.Key}");
            }
            else if (category == "Expense")
            {
                var netDebit = group.Sum(x => x.DebitAmount - x.CreditAmount);
                if (netDebit == 0) continue;
                expenseTotal += netDebit;
                AddClosingLine(journalLines, ref lineNo, group.Key, account?.AccountName ?? group.Key, netDebit < 0 ? -netDebit : 0m, netDebit > 0 ? netDebit : 0m, $"Kết chuyển chi phí {group.Key}");
            }
        }

        if (revenueTotal != 0)
        {
            AddClosingLine(journalLines, ref lineNo, incomeSummaryAccount, incomeSummaryName, revenueTotal < 0 ? -revenueTotal : 0m, revenueTotal > 0 ? revenueTotal : 0m, $"Kết chuyển doanh thu vào xác định kết quả {range.Period}");
        }

        if (expenseTotal != 0)
        {
            AddClosingLine(journalLines, ref lineNo, incomeSummaryAccount, incomeSummaryName, expenseTotal > 0 ? expenseTotal : 0m, expenseTotal < 0 ? -expenseTotal : 0m, $"Kết chuyển chi phí vào xác định kết quả {range.Period}");
        }

        var netProfit = revenueTotal - expenseTotal;
        if (netProfit > 0)
        {
            AddClosingLine(journalLines, ref lineNo, incomeSummaryAccount, incomeSummaryName, netProfit, 0m, $"Kết chuyển lãi kỳ {range.Period}");
            AddClosingLine(journalLines, ref lineNo, retainedEarningsAccount, retainedEarningsName, 0m, netProfit, $"Kết chuyển lãi kỳ {range.Period}");
        }
        else if (netProfit < 0)
        {
            var loss = Math.Abs(netProfit);
            AddClosingLine(journalLines, ref lineNo, retainedEarningsAccount, retainedEarningsName, loss, 0m, $"Kết chuyển lỗ kỳ {range.Period}");
            AddClosingLine(journalLines, ref lineNo, incomeSummaryAccount, incomeSummaryName, 0m, loss, $"Kết chuyển lỗ kỳ {range.Period}");
        }

        return journalLines;
    }

    private static string ResolveCategory(string accountCode, string? category)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return category;
        }

        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "Revenue";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "Expense";
        return "Other";
    }

    private static ClosingEntryDto ToDto(ClosingEntry closingEntry) => new()
    {
        Id = closingEntry.Id,
        FiscalYear = closingEntry.FiscalYear,
        Period = closingEntry.Period,
        ClosingDate = closingEntry.ClosingDate,
        JournalHeaderId = closingEntry.JournalHeaderId,
        Status = closingEntry.Status,
        Notes = closingEntry.Notes,
        Lines = closingEntry.Lines
            .OrderBy(x => x.AccountCode)
            .Select(x => new ClosingEntryLineDto
            {
                AccountCode = x.AccountCode,
                DebitAmount = x.DebitAmount,
                CreditAmount = x.CreditAmount
            })
            .ToList()
    };

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

    private sealed record JournalLineProjection(string AccountCode, string AccountName, decimal DebitAmount, decimal CreditAmount);

    private sealed record PeriodRange(string Period, int FiscalYear, DateTime FromDate, DateTime ToDate)
    {
        public static PeriodRange From(string period)
        {
            var normalized = NormalizePeriod(period);
            var firstDay = DateTime.ParseExact($"{normalized}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var lastDay = firstDay.AddMonths(1).AddDays(-1).Date.AddDays(1).AddTicks(-1);
            return new PeriodRange(normalized, firstDay.Year, firstDay, lastDay);
        }

        private static string NormalizePeriod(string period)
        {
            var trimmed = period.Trim();
            if (DateTime.TryParseExact(trimmed, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ymd))
            {
                return ymd.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse($"01/{trimmed}", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var dmy))
            {
                return dmy.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException("Kỳ kết chuyển phải có định dạng yyyy-MM hoặc MM/yyyy.");
        }
    }
}

