using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Integration.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Integration;

public sealed class AccountingFlowIntegrationTests
{
    [Fact]
    public async Task JournalClosingAndFinancialStatements_StayBalanced()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var journalService = TestDbContextFactory.JournalService(db);
        await PostAsync(journalService, "FLOW-REV", "1111", "5111", 500m);
        await PostAsync(journalService, "FLOW-EXP", "642", "1111", 200m);

        var closingService = new ClosingService(db, journalService, TestDbContextFactory.PeriodService(db), TestDbContextFactory.AuditLogService(db), TestDbContextFactory.AuditService(db));
        var fsService = new FinancialStatementService(db);
        await closingService.ClosePeriodAsync("2026-05", "tester");
        var trial = await fsService.BuildSnapshotsAsync("2026-05");
        var balanceSheet = await fsService.GenerateBalanceSheetAsync("2026-05");
        var income = await fsService.GenerateIncomeStatementAsync("2026-05");

        trial.Sum(x => x.PeriodDebit).Should().Be(trial.Sum(x => x.PeriodCredit));
        balanceSheet.IsBalanced.Should().BeTrue();
        income.ProfitBeforeTax.Should().NotBe(decimal.MinValue);
    }

    private static async Task PostAsync(JournalService service, string no, string debit, string credit, decimal amount)
    {
        var draft = await service.CreateDraftAsync(new JournalHeader
        {
            Id = Guid.NewGuid(), JournalNo = no, JournalDate = new DateTime(2026, 5, 12), PostingDate = new DateTime(2026, 5, 12),
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
