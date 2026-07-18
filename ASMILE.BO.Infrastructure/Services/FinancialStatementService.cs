using System.Globalization;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class FinancialStatementService : IFinancialStatementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;

    public FinancialStatementService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AccountBalanceSnapshotDto>> BuildSnapshotsAsync(string period, bool rebuild = true, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var range = PeriodRange.From(period);
        var lines = await LoadJournalLinesUntilAsync(range.ToDate, cancellationToken);
        var snapshots = lines
            .GroupBy(x => x.AccountCode)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var openingDebit = group.Where(x => x.PostingDate < range.FromDate).Sum(x => x.DebitAmount);
                var openingCredit = group.Where(x => x.PostingDate < range.FromDate).Sum(x => x.CreditAmount);
                var periodDebit = group.Where(x => x.PostingDate >= range.FromDate).Sum(x => x.DebitAmount);
                var periodCredit = group.Where(x => x.PostingDate >= range.FromDate).Sum(x => x.CreditAmount);
                var openingNet = openingDebit - openingCredit;
                var closingNet = openingNet + periodDebit - periodCredit;

                return new AccountBalanceSnapshot
                {
                    Id = Guid.NewGuid(),
                    FiscalYear = range.FiscalYear,
                    Period = range.Period,
                    AccountCode = group.Key,
                    OpeningDebit = Math.Max(openingNet, 0m),
                    OpeningCredit = Math.Max(-openingNet, 0m),
                    PeriodDebit = periodDebit,
                    PeriodCredit = periodCredit,
                    ClosingDebit = Math.Max(closingNet, 0m),
                    ClosingCredit = Math.Max(-closingNet, 0m),
                    SnapshotDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
            })
            .ToList();

        if (rebuild)
        {
            var existingSnapshots = await _dbContext.AccountBalanceSnapshots
                .Where(x => x.Period == range.Period)
                .ToListAsync(cancellationToken);
            _dbContext.AccountBalanceSnapshots.RemoveRange(existingSnapshots);
        }

        if (snapshots.Count > 0)
        {
            _dbContext.AccountBalanceSnapshots.AddRange(snapshots);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return snapshots.Select(ToDto).ToList();
    }

    public async Task<BalanceSheetDto> GenerateBalanceSheetAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default)
    {
        var range = PeriodRange.From(period);
        var snapshots = await GetOrBuildSnapshotEntitiesAsync(range, cancellationToken);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);

        var assets = new List<StatementLineDto>();
        var liabilities = new List<StatementLineDto>();
        var equity = new List<StatementLineDto>();

        foreach (var snapshot in snapshots.OrderBy(x => x.AccountCode))
        {
            accounts.TryGetValue(snapshot.AccountCode, out var account);
            var category = ResolveCategory(snapshot.AccountCode, account?.AccountCategory);
            var amount = category is "Liability" or "Equity"
                ? snapshot.ClosingCredit - snapshot.ClosingDebit
                : snapshot.ClosingDebit - snapshot.ClosingCredit;

            if (amount == 0)
            {
                continue;
            }

            var line = new StatementLineDto
            {
                LineCode = snapshot.AccountCode,
                LineName = account?.AccountName ?? snapshot.AccountCode,
                Amount = amount
            };

            if (category == "Liability") liabilities.Add(line);
            else if (category == "Equity") equity.Add(line);
            else assets.Add(line);
        }

        var totalAssets = assets.Sum(x => x.Amount);
        var totalLiabilitiesAndEquity = liabilities.Sum(x => x.Amount) + equity.Sum(x => x.Amount);
        var difference = totalAssets - totalLiabilitiesAndEquity;
        var warnings = Math.Abs(difference) <= 0.01m
            ? Array.Empty<string>()
            : new[] { $"Bảng cân đối đang lệch {difference:N2} VND." };

        var dto = new BalanceSheetDto
        {
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            GeneratedAt = DateTime.UtcNow,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity,
            TotalAssets = totalAssets,
            TotalLiabilitiesAndEquity = totalLiabilitiesAndEquity,
            Difference = difference,
            IsBalanced = Math.Abs(difference) <= 0.01m,
            Warnings = warnings
        };

        await SaveExecutionAsync("BalanceSheet", range, dto, generatedBy, cancellationToken);
        return dto;
    }

    public async Task<IncomeStatementDto> GenerateIncomeStatementAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default)
    {
        var range = PeriodRange.From(period);
        var lines = await LoadJournalLinesBetweenAsync(range.FromDate, range.ToDate, cancellationToken);
        var accounts = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, cancellationToken);

        decimal revenue = 0m, cogs = 0m, operatingExpenses = 0m, otherIncome = 0m, otherExpense = 0m;
        foreach (var line in lines)
        {
            accounts.TryGetValue(line.AccountCode, out var account);
            var category = ResolveCategory(line.AccountCode, account?.AccountCategory);
            var accountCode = line.AccountCode;

            if (category == "Revenue" || accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal))
            {
                var amount = line.CreditAmount - line.DebitAmount;
                if (accountCode.StartsWith("7", StringComparison.Ordinal)) otherIncome += amount;
                else revenue += amount;
            }
            else if (category == "Expense" || accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal))
            {
                var amount = line.DebitAmount - line.CreditAmount;
                if (accountCode.StartsWith("632", StringComparison.Ordinal)) cogs += amount;
                else if (accountCode.StartsWith("8", StringComparison.Ordinal)) otherExpense += amount;
                else operatingExpenses += amount;
            }
        }

        var grossProfit = revenue - cogs;
        var profitBeforeTax = grossProfit - operatingExpenses + otherIncome - otherExpense;
        var dto = new IncomeStatementDto
        {
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            GeneratedAt = DateTime.UtcNow,
            Revenue = revenue,
            CostOfGoodsSold = cogs,
            GrossProfit = grossProfit,
            OperatingExpenses = operatingExpenses,
            OtherIncome = otherIncome,
            OtherExpense = otherExpense,
            ProfitBeforeTax = profitBeforeTax,
            ProfitAfterTax = profitBeforeTax,
            Lines =
            [
                new() { LineCode = "REVENUE", LineName = "Doanh thu", Amount = revenue },
                new() { LineCode = "COGS", LineName = "Giá vốn hàng bán", Amount = cogs },
                new() { LineCode = "GROSS_PROFIT", LineName = "Lợi nhuận gộp", Amount = grossProfit },
                new() { LineCode = "OPERATING_EXPENSES", LineName = "Chi phí hoạt động", Amount = operatingExpenses },
                new() { LineCode = "OTHER_INCOME", LineName = "Thu nhập khác", Amount = otherIncome },
                new() { LineCode = "OTHER_EXPENSE", LineName = "Chi phí khác", Amount = otherExpense },
                new() { LineCode = "PROFIT_BEFORE_TAX", LineName = "Lợi nhuận trước thuế", Amount = profitBeforeTax },
                new() { LineCode = "PROFIT_AFTER_TAX", LineName = "Lợi nhuận sau thuế tạm tính", Amount = profitBeforeTax, Note = "Chưa có module thuế TNDN." }
            ],
            Warnings = ["Báo cáo sử dụng mapping generic từ tài khoản phát sinh; mapping TT99/TT200 chi tiết sẽ hoàn thiện sau."]
        };

        await SaveExecutionAsync("IncomeStatement", range, dto, generatedBy, cancellationToken);
        return dto;
    }

    public async Task<CashFlowDto> GenerateCashFlowAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default)
    {
        var range = PeriodRange.From(period);
        var cashAccounts = await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.IsActive && (x.RoleCode == "Cash" || x.RoleCode == "Bank"))
            .Select(x => x.AccountCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        var lines = await LoadJournalLinesBetweenAsync(range.FromDate, range.ToDate, cancellationToken);
        var operating = lines
            .Where(x => cashAccounts.Contains(x.AccountCode))
            .Sum(x => x.DebitAmount - x.CreditAmount);

        var dto = new CashFlowDto
        {
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            GeneratedAt = DateTime.UtcNow,
            OperatingCashFlow = operating,
            InvestingCashFlow = 0m,
            FinancingCashFlow = 0m,
            NetCashFlow = operating,
            Lines =
            [
                new() { LineCode = "OPERATING", LineName = "Lưu chuyển tiền từ hoạt động kinh doanh", Amount = operating },
                new() { LineCode = "INVESTING", LineName = "Lưu chuyển tiền từ hoạt động đầu tư", Amount = 0m },
                new() { LineCode = "FINANCING", LineName = "Lưu chuyển tiền từ hoạt động tài chính", Amount = 0m }
            ],
            Warnings = ["Cash Flow mới là skeleton, chưa phân loại đầy đủ trực tiếp/gián tiếp."]
        };

        await SaveExecutionAsync("CashFlow", range, dto, generatedBy, cancellationToken);
        return dto;
    }

    public async Task<FinancialStatementSummaryDto> GetSummaryAsync(string period, CancellationToken cancellationToken = default)
    {
        var balanceSheet = await GenerateBalanceSheetAsync(period, null, cancellationToken);
        var incomeStatement = await GenerateIncomeStatementAsync(period, null, cancellationToken);
        return new FinancialStatementSummaryDto
        {
            Period = balanceSheet.Period,
            TotalAssets = balanceSheet.TotalAssets,
            TotalLiabilitiesAndEquity = balanceSheet.TotalLiabilitiesAndEquity,
            ProfitBeforeTax = incomeStatement.ProfitBeforeTax,
            IsBalanced = balanceSheet.IsBalanced,
            Warnings = balanceSheet.Warnings.Concat(incomeStatement.Warnings).ToList()
        };
    }

    private async Task<IReadOnlyList<AccountBalanceSnapshot>> GetOrBuildSnapshotEntitiesAsync(PeriodRange range, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(x => x.Period == range.Period)
            .ToListAsync(cancellationToken);

        if (snapshots.Count > 0)
        {
            return snapshots;
        }

        await BuildSnapshotsAsync(range.Period, true, null, cancellationToken);
        return await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(x => x.Period == range.Period)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<JournalLineProjection>> LoadJournalLinesUntilAsync(DateTime toDate, CancellationToken cancellationToken)
        => await LoadJournalLinesAsync(null, toDate, cancellationToken);

    private async Task<List<JournalLineProjection>> LoadJournalLinesBetweenAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        => await LoadJournalLinesAsync(fromDate, toDate, cancellationToken);

    private async Task<List<JournalLineProjection>> LoadJournalLinesAsync(DateTime? fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.JournalHeaders
            .AsNoTracking()
            .Where(x => x.Status == JournalStatus.Posted && x.PostingDate <= toDate);
        if (fromDate.HasValue)
        {
            query = query.Where(x => x.PostingDate >= fromDate.Value);
        }

        var headers = await query
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);

        return headers
            .SelectMany(header => header.Lines.Select(line => new JournalLineProjection(
                header.PostingDate,
                line.AccountCode,
                line.AccountName,
                line.DebitAmount,
                line.CreditAmount)))
            .ToList();
    }

    private async Task SaveExecutionAsync<T>(string statementType, PeriodRange range, T dto, string? generatedBy, CancellationToken cancellationToken)
    {
        _dbContext.FinancialStatementExecutions.Add(new FinancialStatementExecution
        {
            Id = Guid.NewGuid(),
            StatementType = statementType,
            FiscalYear = range.FiscalYear,
            Period = range.Period,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = generatedBy,
            Status = "Completed",
            JsonData = JsonSerializer.Serialize(dto, JsonOptions)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveCategory(string accountCode, string? category)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return category;
        }

        if (accountCode.StartsWith("3", StringComparison.Ordinal)) return "Liability";
        if (accountCode.StartsWith("4", StringComparison.Ordinal)) return "Equity";
        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "Revenue";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "Expense";
        return "Asset";
    }

    private static AccountBalanceSnapshotDto ToDto(AccountBalanceSnapshot snapshot) => new()
    {
        FiscalYear = snapshot.FiscalYear,
        Period = snapshot.Period,
        AccountCode = snapshot.AccountCode,
        OpeningDebit = snapshot.OpeningDebit,
        OpeningCredit = snapshot.OpeningCredit,
        PeriodDebit = snapshot.PeriodDebit,
        PeriodCredit = snapshot.PeriodCredit,
        ClosingDebit = snapshot.ClosingDebit,
        ClosingCredit = snapshot.ClosingCredit,
        SnapshotDate = snapshot.SnapshotDate
    };

    private sealed record JournalLineProjection(DateTime PostingDate, string AccountCode, string AccountName, decimal DebitAmount, decimal CreditAmount);

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

            throw new InvalidOperationException("Kỳ báo cáo phải có định dạng yyyy-MM hoặc MM/yyyy.");
        }
    }
}
