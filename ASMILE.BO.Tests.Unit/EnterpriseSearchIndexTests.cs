using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class EnterpriseSearchIndexTests
{
    private static readonly Guid UserId = Guid.Parse("40404040-4040-4040-4040-404040404040");

    [Fact]
    public async Task Search_RanksExactTitleBeforeLowerScoreMatches()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new EnterpriseSearchService(db, new TestCurrentUserService(UserId));
        await service.UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Customer", EntityId = "1", Title = "ACME", Category = "Customer", TargetRoute = "customers", SearchText = "ACME customer", BaseScore = 10 });
        await service.UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Supplier", EntityId = "2", Title = "ACME Parts", Category = "Supplier", TargetRoute = "suppliers", SearchText = "ACME supplier", BaseScore = 20 });

        var results = await service.SearchAsync("ACME");

        results.Should().HaveCount(2);
        results[0].Title.Should().Be("ACME");
    }

    [Fact]
    public async Task Search_IsCompanyIsolated()
    {
        var databaseName = $"search-{Guid.NewGuid():N}";
        var companyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var companyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(companyA), databaseName, seedAccountingCore: false))
        {
            await new EnterpriseSearchService(db, new TestCurrentUserService(UserId)).UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Journal", EntityId = "A", Title = "JV-A", Category = "Journal", TargetRoute = "general-ledger", SearchText = "JV-A" });
        }
        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(companyB), databaseName, seedAccountingCore: false))
        {
            await new EnterpriseSearchService(db, new TestCurrentUserService(UserId)).UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Journal", EntityId = "B", Title = "JV-B", Category = "Journal", TargetRoute = "general-ledger", SearchText = "JV-B" });
        }

        await using var dbA = TestDbContextFactory.CreateContext(new TestCompanyContext(companyA), databaseName, seedAccountingCore: false);
        var results = await new EnterpriseSearchService(dbA, new TestCurrentUserService(UserId)).SearchAsync("JV");

        results.Select(x => x.Title).Should().Equal("JV-A");
    }

    [Fact]
    public async Task RecentSearches_ArePersistedPerUser()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new EnterpriseSearchService(db, new TestCurrentUserService(UserId));
        await service.RecordRecentSearchAsync("cash report");

        var recent = await service.GetRecentSearchesAsync();

        recent.Should().ContainSingle(x => x.Query == "cash report");
    }

    [Fact]
    public async Task Search_HonorsCancellation()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new EnterpriseSearchService(db, new TestCurrentUserService(UserId));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.SearchAsync("anything", cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Search_ToleratesMinorTypos()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var service = new EnterpriseSearchService(db, new TestCurrentUserService(UserId));
        await service.UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Workflow", EntityId = "W-1", Title = "Distributed Lock", Category = "Workflow", TargetRoute = "operations-center", SearchText = "distributed lock" });

        var results = await service.SearchAsync("distibuted lock");

        results.Should().ContainSingle(x => x.Title == "Distributed Lock");
    }

    [Fact]
    public async Task Search_IsBranchIsolated()
    {
        var databaseName = $"branch-search-{Guid.NewGuid():N}";
        var companyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var branchA = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        var branchB = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(companyId, branchA), databaseName, seedAccountingCore: false))
        {
            await new EnterpriseSearchService(db, new TestCurrentUserService(UserId)).UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Report", EntityId = "A", Title = "Branch A report", Category = "Report", TargetRoute = "reports", SearchText = "branch a" });
        }

        await using (var db = TestDbContextFactory.CreateContext(new TestCompanyContext(companyId, branchB), databaseName, seedAccountingCore: false))
        {
            await new EnterpriseSearchService(db, new TestCurrentUserService(UserId)).UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Report", EntityId = "B", Title = "Branch B report", Category = "Report", TargetRoute = "reports", SearchText = "branch b" });
        }

        await using var dbA = TestDbContextFactory.CreateContext(new TestCompanyContext(companyId, branchA), databaseName, seedAccountingCore: false);
        var results = await new EnterpriseSearchService(dbA, new TestCurrentUserService(UserId)).SearchAsync("branch");

        results.Select(x => x.Title).Should().Equal("Branch A report");
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(Guid userId)
        {
            CurrentUser = new UserAccountDto { Id = userId, UserName = "search.user", DisplayName = "Search User", Roles = [], IsActive = true };
        }
        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => false;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() => CurrentUser = null;
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId) => CompanyId = companyId;
        public TestCompanyContext(Guid companyId, Guid branchId)
        {
            CompanyId = companyId;
            BranchId = branchId;
        }
        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName => "Test Company";
        public string? BranchName => null;
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) { CompanyId = companyId; return Task.CompletedTask; }
        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default) { BranchId = branchId; return Task.CompletedTask; }
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }
}
