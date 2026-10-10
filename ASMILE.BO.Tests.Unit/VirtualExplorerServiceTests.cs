using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class VirtualExplorerServiceTests
{
    [Fact]
    public async Task RefreshAsync_delegates_to_the_real_data_repository_when_authorized()
    {
        var expected = new VirtualExplorerRefreshResultDto
        {
            RootNodeCount = 3,
            DriveNodeCount = 5,
            DocumentCount = 12,
            RefreshedAtUtc = DateTime.UtcNow
        };
        var repository = new FakeVirtualExplorerRepository(expected);
        var service = CreateService(repository, "VFC.View");
        using var cancellation = new CancellationTokenSource();

        var result = await service.RefreshAsync(cancellation.Token);

        result.Should().BeSameAs(expected);
        repository.RefreshCallCount.Should().Be(1);
        repository.LastCancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Root_children_search_and_direct_node_access_require_vfc_permission()
    {
        var repository = new FakeVirtualExplorerRepository(new());
        var service = CreateService(repository);

        (await service.GetRootAsync()).Should().BeEmpty();
        (await service.GetChildrenAsync("This PC")).Should().BeEmpty();
        (await service.SearchAsync("customer")).Should().BeEmpty();
        (await service.GetNodeAsync(@"This PC\D: ACCOUNTING")).Should().BeNull();
        (await service.RefreshAsync()).DriveNodeCount.Should().Be(0);

        repository.ReadCallCount.Should().Be(0);
        repository.RefreshCallCount.Should().Be(0);
    }

    [Fact]
    public async Task This_pc_children_are_filtered_by_each_modules_permission()
    {
        var repository = new FakeVirtualExplorerRepository(new())
        {
            Children =
            [
                Node("This PC\\C: SYSTEM", ASMILE.BO.Core.Enums.BusinessModule.System),
                Node("This PC\\D: ACCOUNTING", ASMILE.BO.Core.Enums.BusinessModule.Accounting),
                Node("This PC\\E: SALES", ASMILE.BO.Core.Enums.BusinessModule.Sales),
                Node("This PC\\F: HR", ASMILE.BO.Core.Enums.BusinessModule.HR)
            ]
        };
        var service = CreateService(repository, "VFC.View", "System.View", "Sales.View");

        var result = await service.GetChildrenAsync("This PC");

        result.Select(x => x.VirtualPath).Should().BeEquivalentTo(
            "This PC\\C: SYSTEM",
            "This PC\\E: SALES");
    }

    [Fact]
    public async Task Direct_accounting_path_and_search_results_cannot_bypass_module_permissions()
    {
        var accounting = Node(@"This PC\D: ACCOUNTING\Khách hàng", Core.Enums.BusinessModule.Accounting);
        var sales = Node(@"This PC\E: SALES\POS", Core.Enums.BusinessModule.Sales);
        var repository = new FakeVirtualExplorerRepository(new())
        {
            SearchResults = [accounting, sales],
            Node = accounting
        };
        var service = CreateService(repository, "VFC.View", "Sales.View");

        (await service.GetChildrenAsync(@"This PC\D: ACCOUNTING")).Should().BeEmpty();
        (await service.GetNodeAsync(accounting.VirtualPath)).Should().BeNull();
        (await service.SearchAsync("customer")).Select(x => x.VirtualPath)
            .Should().ContainSingle().Which.Should().Be(sales.VirtualPath);
    }

    [Fact]
    public async Task Salary_folder_requires_payroll_permission_in_addition_to_hr_view()
    {
        var salaryPath = @"This PC\F: HR\Lương";
        var repository = new FakeVirtualExplorerRepository(new())
        {
            Children = [Node(salaryPath, ASMILE.BO.Core.Enums.BusinessModule.HR)]
        };
        var withoutPayrollPermission = CreateService(repository, "VFC.View", "HR.View");

        (await withoutPayrollPermission.GetChildrenAsync(@"This PC\F: HR")).Should().BeEmpty();

        var withPayrollPermission = CreateService(repository, "VFC.View", "HR.View", "HR.PayrollView");
        (await withPayrollPermission.GetChildrenAsync(@"This PC\F: HR"))
            .Select(x => x.VirtualPath).Should().Contain(salaryPath);
    }

    private static VirtualExplorerService CreateService(
        FakeVirtualExplorerRepository repository,
        params string[] permissions)
        => new(repository, new FakeAuthorizationService(permissions));

    private static VirtualExplorerNodeDto Node(string path, ASMILE.BO.Core.Enums.BusinessModule module)
        => new() { Id = path, Name = path.Split('\\').Last(), DisplayName = path.Split('\\').Last(), VirtualPath = path, Module = module };

    private sealed class FakeVirtualExplorerRepository(VirtualExplorerRefreshResultDto refreshResult)
        : IVirtualExplorerRepository
    {
        public int RefreshCallCount { get; private set; }
        public int ReadCallCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public IReadOnlyList<VirtualExplorerNodeDto> Children { get; init; } = Array.Empty<VirtualExplorerNodeDto>();
        public IReadOnlyList<VirtualExplorerNodeDto> SearchResults { get; init; } = Array.Empty<VirtualExplorerNodeDto>();
        public VirtualExplorerNodeDto? Node { get; init; }

        public Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync(CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return Task.FromResult<IReadOnlyList<VirtualExplorerNodeDto>>(Array.Empty<VirtualExplorerNodeDto>());
        }

        public Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return Task.FromResult(Children);
        }

        public Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return Task.FromResult(SearchResults);
        }

        public Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return Task.FromResult(Node);
        }

        public Task<VirtualExplorerRefreshResultDto> RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(refreshResult);
        }
    }

    private sealed class FakeAuthorizationService(IEnumerable<string> permissions) : IAuthorizationService
    {
        private readonly HashSet<string> _permissions = new(permissions, StringComparer.OrdinalIgnoreCase);

        public bool HasPermission(string permissionCode) => _permissions.Contains(permissionCode);
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(HasPermission(permissionCode));
        public bool HasPermission(ASMILE.BO.Core.Security.Permission permission) => HasPermission(permission.ToString());
        public void Demand(ASMILE.BO.Core.Security.Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(false);
    }
}
