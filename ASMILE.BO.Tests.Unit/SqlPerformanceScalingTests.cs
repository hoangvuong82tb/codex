using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class SqlPerformanceScalingTests
{
    [Fact]
    public async Task PagedJournalQuery_ReturnsProjectedRowsWithoutTracking()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var header = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "PERF-TEST-001",
            JournalDate = new DateTime(2026, 5, 1),
            PostingDate = new DateTime(2026, 5, 1),
            SourceType = "UnitTest",
            SourceNo = "UT-001",
            Description = "Kiểm thử hiệu năng",
            PeriodCode = "2026-05",
            Status = JournalStatus.Posted,
            TotalDebit = 100m,
            TotalCredit = 100m,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = "1111", AccountName = "Tiền mặt", DebitAmount = 100m },
                new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = "5111", AccountName = "Doanh thu", CreditAmount = 100m }
            ]
        };
        db.JournalHeaders.Add(header);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new SqlPerformanceScalingService(db, NullLogger<SqlPerformanceScalingService>.Instance);
        var rows = await service.GetPagedJournalLinesAsync(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), "1111", 1, 20);

        rows.Should().ContainSingle();
        rows[0].JournalNo.Should().Be("PERF-TEST-001");
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task MeasureCoreQueries_ReturnsTimingRows()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new SqlPerformanceScalingService(db, NullLogger<SqlPerformanceScalingService>.Instance);

        var timings = await service.MeasureCoreQueriesAsync();

        timings.Should().NotBeEmpty();
        timings.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Operation));
    }
}
