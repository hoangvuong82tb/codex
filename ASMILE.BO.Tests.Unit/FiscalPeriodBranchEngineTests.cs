using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class FiscalPeriodBranchEngineTests
{
    private static readonly Guid CompanyId = Guid.Parse("10101010-1010-1010-1010-101010101010");
    private static readonly Guid BranchA = Guid.Parse("20202020-2020-2020-2020-2020202020a1");
    private static readonly Guid BranchB = Guid.Parse("20202020-2020-2020-2020-2020202020b2");

    [Fact]
    public async Task BranchPeriodLock_BlocksPostingForLockedBranchOnly()
    {
        var databaseName = NewDatabaseName();
        await using (var setupDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchA), databaseName, seedAccountingCore: false))
        {
            var periodService = CreatePeriodService(setupDb, BranchA);
            await periodService.LockPeriodAsync("2026-05", BranchA, "controller", "Branch close");
        }

        await using (var branchADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchA), databaseName, seedAccountingCore: false))
        {
            var journalService = CreateJournalService(branchADb, BranchA);
            var draft = await journalService.CreateDraftAsync(CreateJournal("BR-A-LOCKED", 100m));

            var act = () => journalService.PostDraftAsync(draft.Id, "poster");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*locked*");
        }

        await using (var branchBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchB), databaseName, seedAccountingCore: false))
        {
            var journalService = CreateJournalService(branchBDb, BranchB);
            var draft = await journalService.CreateDraftAsync(CreateJournal("BR-B-OPEN", 100m));

            var posted = await journalService.PostDraftAsync(draft.Id, "poster");

            posted.Status.Should().Be(JournalStatus.Posted);
        }
    }

    [Fact]
    public async Task GeneralLedgerReport_UsesSelectedBranchScope()
    {
        var databaseName = NewDatabaseName();
        await using (var branchADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchA), databaseName, seedAccountingCore: false))
        {
            branchADb.JournalHeaders.Add(CreateJournal("BR-A-REPORT", 100m, JournalStatus.Posted));
            await branchADb.SaveChangesAsync();
        }

        await using (var branchBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchB), databaseName, seedAccountingCore: false))
        {
            branchBDb.JournalHeaders.Add(CreateJournal("BR-B-REPORT", 200m, JournalStatus.Posted));
            await branchBDb.SaveChangesAsync();
        }

        await using var reportDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId, BranchA), databaseName, seedAccountingCore: false);
        var result = await new ReportEngine(reportDb).ExecuteAsync(new ReportRequestDto
        {
            ReportCode = "GENERAL_LEDGER",
            Parameters =
            {
                ["fromDate"] = "2026-05-01",
                ["toDate"] = "2026-05-31"
            }
        });

        result.Rows.Select(x => x.Values["JournalNo"]).Should().OnlyContain(x => Equals(x, "BR-A-REPORT"));
        result.Summary["TotalDebit"].Should().Be(100m);
    }

    [Fact]
    public async Task CreateFiscalYearPeriods_CreatesTwelveOpenPeriods()
    {
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId), seedAccountingCore: false);
        var service = CreatePeriodService(db, null);

        var periods = await service.CreateFiscalYearPeriodsAsync(2027, "controller");
        var fiscalYear = await db.FiscalYears.SingleAsync(x => x.Year == 2027);

        periods.Should().HaveCount(12);
        periods.Select(x => x.PeriodCode).Should().StartWith("2027-01").And.EndWith("2027-12");
        periods.Should().OnlyContain(x => x.Status == AccountingPeriodStatus.Open);
        fiscalYear.FiscalYearCode.Should().Be("2027");
    }

    private static string NewDatabaseName() => $"asmile-fiscal-branch-{Guid.NewGuid():N}";

    private static AccountingPeriodService CreatePeriodService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, Guid? branchId)
        => new(db, TestDbContextFactory.AuditLogService(db), new TestCompanyContext(CompanyId, branchId), new TestCurrentUserService([]));

    private static JournalService CreateJournalService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, Guid? branchId)
        => new(db, CreatePeriodService(db, branchId), TestDbContextFactory.AuditLogService(db));

    private static JournalHeader CreateJournal(string journalNo, decimal amount, JournalStatus status = JournalStatus.Draft) => new()
    {
        Id = Guid.NewGuid(),
        JournalNo = journalNo,
        JournalDate = new DateTime(2026, 5, 10),
        PostingDate = new DateTime(2026, 5, 10),
        SourceType = "UnitTest",
        SourceNo = journalNo,
        Description = journalNo,
        PeriodCode = "2026-05",
        Status = status,
        TotalDebit = amount,
        TotalCredit = amount,
        CreatedBy = "test",
        CreatedAt = DateTime.UtcNow,
        PostedBy = status == JournalStatus.Posted ? "test" : null,
        PostedAt = status == JournalStatus.Posted ? DateTime.UtcNow : null,
        Lines =
        [
            new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = "1111", AccountName = "Cash", DebitAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m },
            new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = "5111", AccountName = "Revenue", CreditAmount = amount, CurrencyCode = "VND", ExchangeRate = 1m }
        ]
    };

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId, Guid? branchId = null)
        {
            CompanyId = companyId;
            BranchId = branchId;
            BranchName = branchId.HasValue ? "Branch" : null;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName { get; private set; } = "Company";
        public string? BranchName { get; private set; }
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            BranchId = null;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            BranchName = branchId.HasValue ? "Branch" : null;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([new() { Id = CompanyId!.Value, Code = "CO", Name = CompanyName, IsDefault = true }]);

        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(IReadOnlyList<string> roles)
        {
            CurrentUser = new UserAccountDto
            {
                Id = Guid.NewGuid(),
                UserName = "period.user",
                DisplayName = "Period User",
                Roles = roles,
                IsActive = true
            };
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => false;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }
}
