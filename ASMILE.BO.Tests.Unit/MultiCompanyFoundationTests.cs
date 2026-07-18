using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class MultiCompanyFoundationTests
{
    private static readonly Guid CompanyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BranchA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid BranchB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task CompanyScopedQueries_ReturnCurrentCompanyAndLegacyRowsOnly()
    {
        var databaseName = NewDatabaseName();

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false))
        {
            db.CashFunds.Add(CreateCashFund("A-FUND"));
            await db.SaveChangesAsync();

            var legacy = CreateCashFund("LEGACY-FUND");
            db.CashFunds.Add(legacy);
            await db.SaveChangesAsync();

            legacy.CompanyId = null;
            await db.SaveChangesAsync();
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false))
        {
            db.CashFunds.Add(CreateCashFund("B-FUND"));
            await db.SaveChangesAsync();
        }

        await using var companyA = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false);
        await using var companyB = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false);

        var companyAFunds = await companyA.CashFunds.OrderBy(x => x.FundCode).Select(x => x.FundCode).ToListAsync();
        var companyBFunds = await companyB.CashFunds.OrderBy(x => x.FundCode).Select(x => x.FundCode).ToListAsync();

        companyAFunds.Should().Equal("A-FUND", "LEGACY-FUND");
        companyBFunds.Should().Equal("B-FUND", "LEGACY-FUND");
    }

    [Fact]
    public async Task PermissionService_DeniesCrossCompanyAccess()
    {
        var user = new TestCurrentUserService(UserId, ["dashboard.view"]);
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), NewDatabaseName(), seedAccountingCore: false);
        db.UserCompanyAccesses.Add(new UserCompanyAccess { UserId = UserId, CompanyId = CompanyA, IsActive = true, IsDefault = true });
        await db.SaveChangesAsync();

        var service = new PermissionService(db, user, new TestCompanyContext(CompanyA));

        var allowed = await service.HasPermissionAsync("dashboard.view", CompanyA);
        var denied = await service.HasPermissionAsync("dashboard.view", CompanyB);

        allowed.Should().BeTrue();
        denied.Should().BeFalse();
    }

    [Fact]
    public async Task WorkspaceService_IsolatesPinnedModulesByCompany()
    {
        var databaseName = NewDatabaseName();
        var user = new TestCurrentUserService(UserId, []);

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false))
        {
            var service = new WorkspaceService(db, user, new TestCompanyContext(CompanyA));
            await service.SavePinnedModulesAsync(["dashboard", "cash"]);
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false))
        {
            var service = new WorkspaceService(db, user, new TestCompanyContext(CompanyB));
            await service.SavePinnedModulesAsync(["reports"]);
        }

        await using var companyADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false);
        await using var companyBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false);

        var companyAWorkspace = await new WorkspaceService(companyADb, user, new TestCompanyContext(CompanyA)).LoadWorkspaceAsync();
        var companyBWorkspace = await new WorkspaceService(companyBDb, user, new TestCompanyContext(CompanyB)).LoadWorkspaceAsync();

        companyAWorkspace.PinnedModules.Should().Equal("dashboard", "cash");
        companyBWorkspace.PinnedModules.Should().Equal("reports");
    }

    [Fact]
    public async Task WorkspaceService_IsolatesPinnedModulesByBranch()
    {
        var databaseName = NewDatabaseName();
        var user = new TestCurrentUserService(UserId, []);

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchA), databaseName, seedAccountingCore: false))
        {
            var service = new WorkspaceService(db, user, new TestCompanyContext(CompanyA, BranchA));
            await service.SavePinnedModulesAsync(["dashboard", "cash"]);
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchB), databaseName, seedAccountingCore: false))
        {
            var service = new WorkspaceService(db, user, new TestCompanyContext(CompanyA, BranchB));
            await service.SavePinnedModulesAsync(["reports"]);
        }

        await using var branchADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchA), databaseName, seedAccountingCore: false);
        await using var branchBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA, BranchB), databaseName, seedAccountingCore: false);

        var branchAWorkspace = await new WorkspaceService(branchADb, user, new TestCompanyContext(CompanyA, BranchA)).LoadWorkspaceAsync();
        var branchBWorkspace = await new WorkspaceService(branchBDb, user, new TestCompanyContext(CompanyA, BranchB)).LoadWorkspaceAsync();

        branchAWorkspace.PinnedModules.Should().Equal("dashboard", "cash");
        branchBWorkspace.PinnedModules.Should().Equal("reports");
    }

    [Fact]
    public async Task CurrentCompanyContext_PersistsActiveCompanySession()
    {
        var databaseName = NewDatabaseName();
        var companyId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var branchId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var user = new TestCurrentUserService(UserId, []);

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUserService>(user);
        services.AddSingleton<ICompanyContextAccessor, CompanyContextAccessor>();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<ICurrentCompanyContext, CurrentCompanyContext>();

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Companies.Add(new Company
            {
                Id = companyId,
                Code = "CMP",
                Name = "Company Persist",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                ModifiedAtUtc = DateTime.UtcNow
            });
            db.Branches.Add(new Branch
            {
                Id = branchId,
                CompanyId = companyId,
                Code = "BR",
                Name = "Branch Persist",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                ModifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var companyContext = provider.GetRequiredService<ICurrentCompanyContext>();
        await companyContext.EnsureInitializedAsync();
        await companyContext.SetCompanyAsync(companyId);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await verifyDb.ActiveCompanySessions.IgnoreQueryFilters().SingleAsync(x => x.UserId == UserId);

        session.CompanyId.Should().Be(companyId);
        session.BranchId.Should().BeNull();
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyContextAccessor_RestoresNestedScopes()
    {
        var accessor = new CompanyContextAccessor(new TestCurrentUserService(UserId, []));
        accessor.SetCurrentScope(CompanyA, "Company A", BranchA, "Branch A");

        using (accessor.EnterCompanyScope(CompanyB, "Company B"))
        {
            accessor.CurrentCompanyId.Should().Be(CompanyB);
            accessor.CurrentBranchId.Should().BeNull();

            using (accessor.EnterBranchScope(BranchB, "Branch B"))
            {
                accessor.CurrentCompanyId.Should().Be(CompanyB);
                accessor.CurrentBranchId.Should().Be(BranchB);
            }

            accessor.CurrentBranchId.Should().BeNull();
        }

        accessor.CurrentCompanyId.Should().Be(CompanyA);
        accessor.CurrentBranchId.Should().Be(BranchA);
    }

    [Fact]
    public async Task ReportEngineHistory_IsCompanyIsolated()
    {
        var databaseName = NewDatabaseName();

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false))
        {
            db.ReportExecutions.Add(CreateReportExecution("REPORT_A", DateTime.UtcNow.AddMinutes(-1)));
            await db.SaveChangesAsync();
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false))
        {
            db.ReportExecutions.Add(CreateReportExecution("REPORT_B", DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        await using var companyADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false);
        await using var companyBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false);

        var companyAReports = await new ReportEngine(companyADb).GetExecutionHistoryAsync();
        var companyBReports = await new ReportEngine(companyBDb).GetExecutionHistoryAsync();

        companyAReports.Select(x => x.ReportCode).Should().Equal("REPORT_A");
        companyBReports.Select(x => x.ReportCode).Should().Equal("REPORT_B");
    }

    [Fact]
    public async Task AuditLogs_AreCompanyIsolated()
    {
        var databaseName = NewDatabaseName();

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false))
        {
            db.AuditLogs.Add(CreateAuditLog("COMPANY_A_ACTION"));
            await db.SaveChangesAsync();
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false))
        {
            db.AuditLogs.Add(CreateAuditLog("COMPANY_B_ACTION"));
            await db.SaveChangesAsync();
        }

        await using var companyADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false);
        await using var companyBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false);

        var companyAActions = await companyADb.AuditLogs.Select(x => x.Action).ToListAsync();
        var companyBActions = await companyBDb.AuditLogs.Select(x => x.Action).ToListAsync();

        companyAActions.Should().Equal("COMPANY_A_ACTION");
        companyBActions.Should().Equal("COMPANY_B_ACTION");
    }

    [Fact]
    public async Task JournalHeadersAndLines_AreCompanyIsolated()
    {
        var databaseName = NewDatabaseName();

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false))
        {
            db.JournalHeaders.Add(CreateJournal("JV-A", 100m));
            await db.SaveChangesAsync();
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false))
        {
            db.JournalHeaders.Add(CreateJournal("JV-B", 200m));
            await db.SaveChangesAsync();
        }

        await using var companyADb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyA), databaseName, seedAccountingCore: false);
        await using var companyBDb = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyB), databaseName, seedAccountingCore: false);

        var companyAJournals = await companyADb.JournalHeaders.Include(x => x.Lines).ToListAsync();
        var companyBJournals = await companyBDb.JournalHeaders.Include(x => x.Lines).ToListAsync();

        companyAJournals.Select(x => x.JournalNo).Should().Equal("JV-A");
        companyBJournals.Select(x => x.JournalNo).Should().Equal("JV-B");
        companyAJournals.Single().Lines.Select(x => x.Description).Should().OnlyContain(x => x == "JV-A");
        companyBJournals.Single().Lines.Select(x => x.Description).Should().OnlyContain(x => x == "JV-B");
    }

    private static string NewDatabaseName() => $"asmile-multicompany-{Guid.NewGuid():N}";

    private static CashFund CreateCashFund(string code) => new()
    {
        Id = Guid.NewGuid(),
        FundCode = code,
        FundName = code,
        CurrencyCode = "VND",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static ReportExecution CreateReportExecution(string reportCode, DateTime startedAt) => new()
    {
        Id = Guid.NewGuid(),
        ReportCode = reportCode,
        Status = "Completed",
        StartedAt = startedAt,
        CompletedAt = startedAt.AddSeconds(1),
        RowCount = 1
    };

    private static AuditLog CreateAuditLog(string action) => new()
    {
        Action = action,
        Detail = action,
        EntityName = "UnitTest",
        EntityId = Guid.NewGuid().ToString("N"),
        Username = "test.user",
        CreatedAt = DateTime.UtcNow
    };

    private static JournalHeader CreateJournal(string journalNo, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        JournalNo = journalNo,
        JournalDate = new DateTime(2026, 5, 10),
        PostingDate = new DateTime(2026, 5, 10),
        SourceType = "ManualJournal",
        SourceNo = journalNo,
        Description = journalNo,
        PeriodCode = "2026-05",
        Status = JournalStatus.Draft,
        TotalDebit = amount,
        TotalCredit = amount,
        CreatedBy = "test",
        CreatedAt = DateTime.UtcNow,
        Lines =
        [
            new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 1,
                AccountCode = "1111",
                AccountName = "Cash",
                DebitAmount = amount,
                Description = journalNo
            },
            new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 2,
                AccountCode = "5111",
                AccountName = "Revenue",
                CreditAmount = amount,
                Description = journalNo
            }
        ]
    };

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId, Guid? branchId = null)
        {
            CompanyId = companyId;
            BranchId = branchId;
            CompanyName = companyId == CompanyA ? "Company A" : "Company B";
            BranchName = branchId.HasValue ? "Selected Branch" : null;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName { get; private set; }
        public string? BranchName { get; private set; }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            CompanyName = companyId == CompanyA ? "Company A" : "Company B";
            BranchId = null;
            BranchName = null;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            BranchName = branchId.HasValue ? "Selected Branch" : null;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>(
            [
                new() { Id = CompanyA, Code = "A", Name = "Company A", IsDefault = true },
                new() { Id = CompanyB, Code = "B", Name = "Company B" }
            ]);

        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>(
            [
                new() { Id = BranchA, CompanyId = CompanyA, Code = "A-MAIN", Name = "Company A Main", IsDefault = true },
                new() { Id = BranchB, CompanyId = CompanyB, Code = "B-MAIN", Name = "Company B Main", IsDefault = true }
            ]);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly HashSet<string> _permissions;

        public TestCurrentUserService(Guid userId, IEnumerable<string> permissions)
        {
            _permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
            CurrentUser = new UserAccountDto
            {
                Id = userId,
                UserName = "test.user",
                DisplayName = "Test User",
                Roles = [],
                IsActive = true
            };
            CurrentSessionId = Guid.NewGuid();
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => _permissions.Contains(permissionCode);

        public void SetCurrentUser(LoginResultDto login)
        {
            CurrentUser = new UserAccountDto
            {
                Id = login.UserId,
                UserName = login.UserName,
                DisplayName = login.DisplayName,
                Roles = login.Roles,
                IsActive = true,
                MustChangePassword = login.MustChangePassword
            };
            CurrentSessionId = login.SessionId;
        }

        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }
}
