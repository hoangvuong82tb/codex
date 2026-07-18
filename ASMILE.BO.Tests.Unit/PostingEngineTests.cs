using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class PostingEngineTests
{
    [Fact]
    public async Task PostDraftAsync_PostsBalancedManualJournal()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var journalService = TestDbContextFactory.JournalService(db);
        var journal = CreateManualJournal("JV-TEST-001", new DateTime(2026, 5, 10), 100m);

        var draft = await journalService.CreateDraftAsync(journal);
        var posted = await journalService.PostDraftAsync(draft.Id, "tester");

        posted.Status.Should().Be(JournalStatus.Posted);
        posted.TotalDebit.Should().Be(100m);
        posted.TotalCredit.Should().Be(100m);
        db.AuditLogs.Should().Contain(x => x.EntityName == "JournalHeader" && x.Action == "Post");
    }

    [Fact]
    public async Task PostDraftAsync_RejectsDuplicatePosting()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var journalService = TestDbContextFactory.JournalService(db);
        var journal = await journalService.CreateDraftAsync(CreateManualJournal("JV-TEST-002", new DateTime(2026, 5, 10), 50m));
        await journalService.PostDraftAsync(journal.Id, "tester");

        var act = () => journalService.PostDraftAsync(journal.Id, "tester");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PostDraftAsync_RejectsLockedPeriod()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var period = db.AccountingPeriods.Single(x => x.PeriodCode == "2026-05");
        period.Status = AccountingPeriodStatus.Locked;
        await db.SaveChangesAsync();
        var journalService = TestDbContextFactory.JournalService(db);

        var act = () => journalService.CreateDraftAsync(CreateManualJournal("JV-TEST-003", new DateTime(2026, 5, 10), 50m));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReverseAsync_CreatesReversalJournal()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var journalService = TestDbContextFactory.JournalService(db);
        var journal = await journalService.CreateDraftAsync(CreateManualJournal("JV-TEST-004", new DateTime(2026, 5, 10), 80m));
        var posted = await journalService.PostDraftAsync(journal.Id, "tester");

        var reversal = await journalService.ReverseAsync(posted.Id, "test reversal", "tester");

        reversal.Status.Should().Be(JournalStatus.Posted);
        reversal.SourceType.Should().Be("Reversal");
        reversal.Lines.Sum(x => x.DebitAmount).Should().Be(80m);
        reversal.Lines.Sum(x => x.CreditAmount).Should().Be(80m);
        (await journalService.GetJournalAsync(posted.Id))!.Status.Should().Be(JournalStatus.Reversed);
    }

    private static JournalHeader CreateManualJournal(string no, DateTime date, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        JournalNo = no,
        JournalDate = date,
        PostingDate = date,
        SourceType = "ManualJournal",
        SourceNo = no,
        Description = "Manual test journal",
        PeriodCode = "2026-05",
        Status = JournalStatus.Draft,
        CreatedBy = "tester",
        CreatedAt = DateTime.UtcNow,
        Lines =
        [
            new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = "1111", AccountName = "Tiền mặt", DebitAmount = amount, CreditAmount = 0m, CurrencyCode = "VND", ExchangeRate = 1m, Description = "Debit" },
            new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = "421", AccountName = "Lợi nhuận", DebitAmount = 0m, CreditAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m, Description = "Credit" }
        ]
    };
}

