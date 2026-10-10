using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace ASMILE.BO.Tests.Unit;

public sealed class NavigationScopeRegressionTests
{
    [Theory]
    [InlineData("dashboard")]
    [InlineData("restaurant-banquet")]
    [InlineData("inventory")]
    [InlineData("customers")]
    public async Task NAV_U_001_003_004_005_non_front_office_routes_do_not_require_property_context(string route)
    {
        var harness = new NavigationHarness(hasProperty: false);

        var result = await harness.Navigation.OpenModuleAsync(route);

        result.Success.Should().BeTrue();
        harness.Windows.OpenWindows.Should().ContainSingle(x => x.Module.Key == route);
    }

    [Fact]
    public async Task FO_NAV_U_001_002_003_004_no_property_returns_safe_failure_without_resolving_or_scoping_reservation()
    {
        var harness = new NavigationHarness(hasProperty: false);

        var result = await harness.Navigation.OpenModuleAsync("front-office-reservations");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Vui long chon chi nhanh truoc khi mo module Dat phong.");
        harness.ScopeFactory.Scopes.Should().BeEmpty();
        harness.Windows.OpenWindows.Should().BeEmpty();
    }

    [Fact]
    public async Task NAV_U_006_007_008_module_view_model_is_resolved_in_a_scope_that_lives_until_window_close()
    {
        var harness = new NavigationHarness(hasProperty: false);

        var result = await harness.Navigation.OpenModuleAsync("restaurant-banquet");
        var window = harness.Windows.OpenWindows.Single();
        var moduleScope = harness.ScopeFactory.Scopes[0];

        result.Success.Should().BeTrue();
        moduleScope.Disposed.Should().BeFalse();

        harness.Windows.Close(window);

        moduleScope.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task NAV_U_009_unknown_route_returns_a_placeholder_without_crashing_the_shell()
    {
        var harness = new NavigationHarness(hasProperty: false);

        var result = await harness.Navigation.OpenModuleAsync("unknown-route");

        result.Success.Should().BeTrue();
        harness.Windows.OpenWindows.Should().ContainSingle();
        harness.Windows.OpenWindows.Single().ContentViewModel.Should().BeOfType<ModulePlaceholderViewModel>();
    }

    private sealed class NavigationHarness
    {
        public NavigationHarness(bool hasProperty)
        {
            Windows = new WindowManagerService();
            ScopeFactory = new TrackingScopeFactory();
            Navigation = new NavigationService(Windows, ScopeFactory, new TestCorrelationService(), new TestPropertyContextHost(hasProperty));
        }

        public WindowManagerService Windows { get; }
        public TrackingScopeFactory ScopeFactory { get; }
        public NavigationService Navigation { get; }
    }

    private sealed class TrackingScopeFactory : IServiceScopeFactory
    {
        public List<TrackingScope> Scopes { get; } = [];

        public IServiceScope CreateScope()
        {
            var scope = new TrackingScope();
            Scopes.Add(scope);
            return scope;
        }
    }

    private sealed class TrackingScope : IServiceScope
    {
        public bool Disposed { get; private set; }
        public IServiceProvider ServiceProvider { get; } = new NavigationScopeProvider();

        public void Dispose() => Disposed = true;
    }

    private sealed class NavigationScopeProvider : IServiceProvider
    {
        private readonly TestWorkspaceService _workspace = new();
        private readonly TestAuditService _audit = new();

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IWorkspaceService)) return _workspace;
            if (serviceType == typeof(IAuditService)) return _audit;
            if (serviceType.IsClass && serviceType.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            {
                return RuntimeHelpers.GetUninitializedObject(serviceType);
            }

            return null;
        }
    }

    private sealed class TestPropertyContextHost(bool hasProperty) : IFrontOfficePropertyContextHost
    {
        public FrontOfficePropertyOption? CurrentProperty => null;
        public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => [];
        public bool HasCurrentProperty => hasProperty;
        public int Version => 0;
        public string? ErrorCode => hasProperty ? null : "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED";
        public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged { add { } remove { } }
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class TestCorrelationService : IAuditCorrelationService
    {
        public string? CurrentCorrelationId => null;
        public string? ParentCorrelationId => null;
        public string EnsureCorrelationId(string? correlationId = null) => correlationId ?? "nav-test";
        public IDisposable BeginScope(string? correlationId = null) => EmptyDisposable.Instance;
        public Task<T> WithCorrelationAsync<T>(string? correlationId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task WithCorrelationAsync(string? correlationId, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();
        public void Dispose() { }
    }

    private sealed class TestWorkspaceService : IWorkspaceService
    {
        public Task<WorkspaceStateDto> LoadWorkspaceAsync(CancellationToken cancellationToken = default) => Task.FromResult<WorkspaceStateDto>(null!);
        public Task SavePinnedModulesAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRecentModuleAsync(string moduleKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddFavoriteReportAsync(string reportCode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRecentReportAsync(string reportCode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRecentDrilldownAsync(string drilldownKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveLastOpenedTabsAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveStartupModulesAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveShellLayoutAsync(string layoutJson, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<WorkspaceSettingsDto> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<WorkspaceSettingsDto>(null!);
        public Task SaveSettingsAsync(WorkspaceSettingsDto settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearWorkspaceCacheAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestAuditService : IAuditService
    {
        public Task LogAsync(string entityName, string entityId, string action, object? oldValues = null, object? newValues = null, string? userName = null, string? ipAddress = null, string? correlationId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AuditLog>> QueryAsync(string? entityName = null, string? entityId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AuditLog>>([]);
    }
}
