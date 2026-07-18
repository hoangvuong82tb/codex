using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ClosingServiceTests
{
    [Fact]
    public async Task ClosePeriodAsync_CreatesPersistedClosingJournal()
    {
        await using var db = TestDbContextFactory.CreateContext();
        await PostJournalAsync(db, "JV-REV", "1111", "5111", 100m);
        await PostJournalAsync(db, "JV-EXP", "642", "1111", 30m);
        var service = CreateService(db);

        var closing = await service.ClosePeriodAsync("2026-05", "tester");

        closing.Status.Should().Be("Posted");
        closing.JournalHeaderId.Should().NotBeNull();
        db.JournalHeaders.Single(x => x.Id == closing.JournalHeaderId).Status.Should().Be(JournalStatus.Posted);
    }

    [Fact]
    public async Task ReverseClosingAsync_CreatesReversalJournal()
    {
        await using var db = TestDbContextFactory.CreateContext();
        await PostJournalAsync(db, "JV-REV2", "1111", "5111", 100m);
        var service = CreateService(db);
        var closing = await service.ClosePeriodAsync("2026-05", "tester");

        var reversed = await service.ReverseClosingAsync(closing.Id, "test", "tester");

        reversed.Status.Should().Be("Reversed");
        db.JournalHeaders.Count(x => x.SourceType == "Reversal").Should().BeGreaterThan(0);
    }

    private static ClosingService CreateService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
        => new(db, TestDbContextFactory.JournalService(db), TestDbContextFactory.PeriodService(db), TestDbContextFactory.AuditLogService(db), TestDbContextFactory.AuditService(db));

    private static async Task PostJournalAsync(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, string no, string debit, string credit, decimal amount)
    {
        var service = TestDbContextFactory.JournalService(db);
        var draft = await service.CreateDraftAsync(new JournalHeader
        {
            Id = Guid.NewGuid(), JournalNo = no, JournalDate = new DateTime(2026, 5, 10), PostingDate = new DateTime(2026, 5, 10),
            SourceType = "ManualJournal", SourceNo = no, Description = no, PeriodCode = "2026-05", Status = JournalStatus.Draft,
            CreatedBy = "tester", CreatedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = debit, AccountName = debit, DebitAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m },
                new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = credit, AccountName = credit, CreditAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m }
            ]
        });
        await service.PostDraftAsync(draft.Id, "tester");
    }
}
