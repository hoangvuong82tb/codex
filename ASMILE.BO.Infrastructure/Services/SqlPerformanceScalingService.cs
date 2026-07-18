using System.Diagnostics;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class SqlPerformanceScalingService : ISqlPerformanceScalingService
{
    private static readonly Func<AppDbContext, DateTime, DateTime, string?, int, int, IAsyncEnumerable<JournalLinePerformanceDto>> PagedJournalLinesQuery =
        EF.CompileAsyncQuery((AppDbContext dbContext, DateTime fromDate, DateTime toDate, string? accountCode, int skip, int take) =>
            dbContext.JournalLines
                .AsNoTracking()
                .Where(x => x.JournalHeader != null
                    && x.JournalHeader.Status == JournalStatus.Posted
                    && x.JournalHeader.PostingDate >= fromDate
                    && x.JournalHeader.PostingDate <= toDate
                    && (accountCode == null || x.AccountCode == accountCode))
                .OrderBy(x => x.JournalHeader!.PostingDate)
                .ThenBy(x => x.JournalHeader!.JournalNo)
                .ThenBy(x => x.LineNo)
                .Skip(skip)
                .Take(take)
                .Select(x => new JournalLinePerformanceDto
                {
                    JournalHeaderId = x.JournalHeaderId,
                    JournalLineId = x.Id,
                    PostingDate = x.JournalHeader!.PostingDate,
                    JournalNo = x.JournalHeader.JournalNo,
                    SourceType = x.JournalHeader.SourceType,
                    SourceNo = x.JournalHeader.SourceNo,
                    AccountCode = x.AccountCode,
                    AccountName = x.AccountName,
                    DebitAmount = x.DebitAmount,
                    CreditAmount = x.CreditAmount
                }));

    private readonly AppDbContext _dbContext;
    private readonly ILogger<SqlPerformanceScalingService> _logger;

    public SqlPerformanceScalingService(AppDbContext dbContext, ILogger<SqlPerformanceScalingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JournalLinePerformanceDto>> GetPagedJournalLinesAsync(
        DateTime fromDate,
        DateTime toDate,
        string? accountCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 5_000);
        var skip = (page - 1) * pageSize;
        var normalizedAccount = string.IsNullOrWhiteSpace(accountCode) ? null : accountCode.Trim();

        var rows = new List<JournalLinePerformanceDto>(pageSize);
        await foreach (var row in PagedJournalLinesQuery(_dbContext, fromDate, toDate, normalizedAccount, skip, pageSize)
            .WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        return rows;
    }

    public async Task<IReadOnlyList<PerformanceTimingDto>> MeasureCoreQueriesAsync(CancellationToken cancellationToken = default)
    {
        var timings = new List<PerformanceTimingDto>
        {
            await MeasureAsync("Journal posted page", async ct =>
            {
                var rows = await GetPagedJournalLinesAsync(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1), null, 1, 500, ct);
                return rows.Count;
            }, cancellationToken),
            await MeasureAsync("Report history page", ct => _dbContext.ReportExecutions.AsNoTracking().OrderByDescending(x => x.StartedAt).Take(200).CountAsync(ct), cancellationToken),
            await MeasureAsync("Inventory costing open layers", ct => _dbContext.InventoryCostLayers.AsNoTracking().Where(x => x.Status == "Open").Take(1_000).CountAsync(ct), cancellationToken),
            await MeasureAsync("AR open balance", ct => _dbContext.ReceivableTransactions.AsNoTracking().Where(x => x.Status != "Cancelled" && x.BalanceAmount != 0).Take(1_000).CountAsync(ct), cancellationToken),
            await MeasureAsync("AP open balance", ct => _dbContext.PayableTransactions.AsNoTracking().Where(x => x.Status != "Cancelled" && x.BalanceAmount != 0).Take(1_000).CountAsync(ct), cancellationToken),
            await MeasureAsync("Audit latest page", ct => _dbContext.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(500).CountAsync(ct), cancellationToken)
        };

        return timings;
    }

    public async Task<PerformanceTimingDto> SeedJournalLinesForScaleTestAsync(DataScaleRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lineTarget = request.JournalLineCount is 100_000 or 1_000_000
            ? request.JournalLineCount
            : Math.Clamp(request.JournalLineCount, 1_000, 1_000_000);
        var batchSize = Math.Clamp(request.BatchSize, 500, 10_000);
        var stopwatch = Stopwatch.StartNew();
        var createdLines = 0;

        while (createdLines < lineTarget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var headersInBatch = Math.Min(batchSize, lineTarget - createdLines) / 2;
            if (headersInBatch == 0)
            {
                headersInBatch = 1;
            }

            for (var i = 0; i < headersInBatch && createdLines < lineTarget; i++)
            {
                var ordinal = createdLines / 2 + 1;
                var amount = 10_000m + ordinal;
                var header = new JournalHeader
                {
                    Id = Guid.NewGuid(),
                    JournalNo = $"PERF-{ordinal:000000000}",
                    JournalDate = request.StartDate.AddDays(ordinal % 365),
                    PostingDate = request.StartDate.AddDays(ordinal % 365),
                    SourceType = "PerformanceSeed",
                    SourceNo = $"PERF-{ordinal:000000000}",
                    Description = "Dữ liệu kiểm thử tải lớn.",
                    PeriodCode = request.StartDate.AddDays(ordinal % 365).ToString("yyyy-MM"),
                    Status = JournalStatus.Posted,
                    TotalDebit = amount,
                    TotalCredit = amount,
                    CreatedBy = request.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    PostedBy = request.CreatedBy,
                    PostedAt = DateTime.UtcNow,
                    Lines =
                    [
                        new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = "1111", AccountName = "Tiền mặt", DebitAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m },
                        new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = "5111", AccountName = "Doanh thu", CreditAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m }
                    ]
                };

                _dbContext.JournalHeaders.Add(header);
                createdLines += 2;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }

        stopwatch.Stop();
        var timing = new PerformanceTimingDto
        {
            Operation = $"Seed {lineTarget:N0} journal lines",
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            RowCount = createdLines,
            MeasuredAt = DateTime.UtcNow,
            Notes = "Dev helper cho kịch bản 100k/1m dòng journal."
        };
        _logger.LogInformation("Performance seed finished in {ElapsedMs} ms with {RowCount} journal lines.", timing.ElapsedMilliseconds, timing.RowCount);
        return timing;
    }

    private async Task<PerformanceTimingDto> MeasureAsync(string operation, Func<CancellationToken, Task<int>> action, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var rowCount = await action(cancellationToken);
        stopwatch.Stop();
        var timing = new PerformanceTimingDto
        {
            Operation = operation,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            RowCount = rowCount,
            MeasuredAt = DateTime.UtcNow
        };
        _logger.LogInformation("Performance timing {Operation}: {ElapsedMs} ms, {RowCount} rows.", operation, timing.ElapsedMilliseconds, rowCount);
        return timing;
    }
}
