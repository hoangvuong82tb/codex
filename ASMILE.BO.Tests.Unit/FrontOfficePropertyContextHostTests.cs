using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FrontOfficePropertyContextHostTests
{
    [Fact]
    public async Task CTX_U_001_and_003_no_selection_blocks_multi_property_context()
    {
        var harness = new ContextHarness([Branch("A"), Branch("B")]);

        await harness.Host.InitializeAsync();

        harness.Host.HasCurrentProperty.Should().BeFalse();
        harness.Accessor.Current.Should().BeNull();
        harness.Host.ErrorCode.Should().Be("FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");
    }

    [Fact]
    public async Task CTX_U_002_and_006_single_property_auto_selects_and_creates_context()
    {
        var branch = Branch("MAIN");
        var harness = new ContextHarness([branch]);

        await harness.Host.InitializeAsync();

        harness.Host.CurrentProperty!.PropertyId.Should().Be(branch.Id);
        harness.Accessor.Current!.Property.PropertyId.Value.Should().Be(branch.Id);
        harness.Accessor.Current.Actor.ActorId!.Value.Value.Should().Be(harness.User.CurrentUser!.Id);
    }

    [Fact]
    public async Task CTX_U_004_and_007_unauthorized_property_is_rejected_and_switch_replaces_context()
    {
        var first = Branch("A");
        var second = Branch("B");
        var harness = new ContextHarness([first, second]);

        await harness.Host.SetCurrentPropertyAsync(first.Id);
        var firstVersion = harness.Host.Version;
        await harness.Host.SetCurrentPropertyAsync(second.Id);

        harness.Host.CurrentProperty!.PropertyId.Should().Be(second.Id);
        harness.Accessor.Current!.Property.PropertyId.Value.Should().Be(second.Id);
        harness.Host.Version.Should().BeGreaterThan(firstVersion);
        await harness.Host.SetCurrentPropertyAsync(Guid.NewGuid());
        harness.Host.HasCurrentProperty.Should().BeFalse();
        harness.Host.ErrorCode.Should().Be("FRONT_OFFICE_PROPERTY_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task CTX_U_008_and_009_clear_property_disposes_execution_context()
    {
        var branch = Branch("MAIN");
        var harness = new ContextHarness([branch]);
        await harness.Host.InitializeAsync();

        await harness.Host.ClearCurrentPropertyAsync();

        harness.Host.HasCurrentProperty.Should().BeFalse();
        harness.Accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task CTX_U_010_restored_branch_is_revalidated_before_context_creation()
    {
        var branch = Branch("RESTORE");
        var harness = new ContextHarness([branch]);
        await harness.Company.SetBranchAsync(branch.Id);

        await harness.Host.InitializeAsync();

        harness.Host.CurrentProperty!.PropertyId.Should().Be(branch.Id);
        harness.Accessor.Current!.Property.PropertyId.Value.Should().Be(branch.Id);
    }

    [Fact]
    public async Task CTX_U_016_rapid_switch_ends_on_latest_property()
    {
        var first = Branch("A");
        var second = Branch("B");
        var harness = new ContextHarness([first, second]);

        await Task.WhenAll(harness.Host.SetCurrentPropertyAsync(first.Id), harness.Host.SetCurrentPropertyAsync(second.Id));

        harness.Host.CurrentProperty!.PropertyId.Should().Be(second.Id);
        harness.Accessor.Current!.Property.PropertyId.Value.Should().Be(second.Id);
    }

    [Fact]
    public async Task CTX_U_017_unauthenticated_session_denies_context_creation()
    {
        var harness = new ContextHarness([Branch("MAIN")], authenticated: false);

        await harness.Host.InitializeAsync();

        harness.Host.HasCurrentProperty.Should().BeFalse();
        harness.Accessor.Current.Should().BeNull();
    }

    private static BranchContextOptionDto Branch(string code) => new() { Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), Code = code, Name = $"Branch {code}", IsDefault = code == "MAIN" };

    private sealed class ContextHarness : IDisposable
    {
        public ContextHarness(IReadOnlyList<BranchContextOptionDto> branches, bool authenticated = true)
        {
            Company = new FakeCompanyContext(branches);
            User = new FakeCurrentUser(authenticated);
            Accessor = new FrontOfficeExecutionContextAccessor();
            Host = new FrontOfficePropertyContextHost(Company, User, Accessor, new TestClock());
        }

        public FakeCompanyContext Company { get; }
        public FakeCurrentUser User { get; }
        public FrontOfficeExecutionContextAccessor Accessor { get; }
        public FrontOfficePropertyContextHost Host { get; }
        public void Dispose() => Host.Dispose();
    }

    private sealed class FakeCompanyContext(IReadOnlyList<BranchContextOptionDto> branches) : ICurrentCompanyContext
    {
        public Guid? CompanyId { get; private set; } = branches.FirstOrDefault()?.CompanyId;
        public Guid? BranchId { get; private set; }
        public string CompanyName => "Test Company";
        public string? BranchName => branches.FirstOrDefault(x => x.Id == BranchId)?.Name;
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) { CompanyId = companyId; BranchId = null; return Task.CompletedTask; }
        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default) { BranchId = branchId; return Task.CompletedTask; }
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult(branches);
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public FakeCurrentUser(bool authenticated)
        {
            if (!authenticated) return;
            CurrentUser = new UserAccountDto { Id = Guid.NewGuid(), UserName = "front-office", DisplayName = "Front Office", Roles = ["FrontOffice"] };
            CurrentSessionId = Guid.NewGuid();
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => IsAuthenticated;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { CurrentUser = null; CurrentSessionId = null; }
    }

    private sealed class TestClock : IClock { public DateTime UtcNow => new(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc); }
}
