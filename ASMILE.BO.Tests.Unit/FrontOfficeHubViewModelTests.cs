using System.ComponentModel;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FrontOfficeHubViewModelTests
{
    [Fact]
    public void FO_HUB_U_001_renders_eleven_cards_with_two_active_entry_points()
    {
        using var hub = CreateHub();

        hub.Cards.Should().HaveCount(11);
        hub.Cards.Single(x => x.Id == "reservation").StatusText.Should().Be("Đang hoạt động");
        hub.Cards.Single(x => x.Id == "reception").StatusText.Should().Be("Đang hoạt động");
        hub.Cards.Count(x => x.StatusText == "Sắp có").Should().Be(9);
    }

    [Fact]
    public async Task FO_HUB_U_002_reservation_card_opens_existing_reservation_route()
    {
        var navigation = new FakeNavigationService();
        using var hub = CreateHub(navigation: navigation);

        await ((IAsyncRelayCommand)hub.Cards.Single(x => x.Id == "reservation").Command).ExecuteAsync(null);

        navigation.OpenedRoutes.Should().ContainSingle().Which.Should().Be("front-office-reservations");
    }

    [Fact]
    public async Task FO_MAIN_U_003_005_006_009_010_011_012_reservation_stays_in_the_front_office_window_and_back_returns_to_main_hub()
    {
        var navigation = new FakeNavigationService();
        var propertyContext = new FakePropertyContextHost();
        var windows = new WindowManagerService();
        using var reservation = new ReservationHubViewModel(new FakePermissions(), propertyContext, new FakeModalService(), navigation);
        using var hub = new FrontOfficeHubViewModel(
            new FakePermissions(),
            propertyContext,
            navigation,
            new FakeCurrentUserService(),
            new TestServiceProvider(reservation),
            windows);
        var window = windows.OpenOrActivate(
            new AppModuleInfo("front-office", "Front Office", "", "", "#2563EB", false, false),
            () => hub);
        var currentProperty = propertyContext.CurrentProperty;

        await ((IAsyncRelayCommand)hub.Cards.Single(x => x.Id == "reservation").Command).ExecuteAsync(null);

        hub.ActiveWorkspace.Should().BeSameAs(reservation);
        navigation.OpenedRoutes.Should().BeEmpty();
        windows.OpenWindows.Should().ContainSingle().Which.Should().BeSameAs(window);
        window.Module.Title.Should().Be("Đặt phòng");
        propertyContext.CurrentProperty.Should().BeSameAs(currentProperty);

        await reservation.BackCommand.ExecuteAsync(null);

        hub.IsMainHubOpen.Should().BeTrue();
        hub.ActiveWorkspace.Should().BeNull();
        windows.OpenWindows.Should().ContainSingle().Which.Should().BeSameAs(window);
        window.Module.Title.Should().Be("Front Office");
        propertyContext.CurrentProperty.Should().BeSameAs(currentProperty);
    }

    [Fact]
    public async Task FO_MAIN_U_013_to_017_reception_stays_in_the_front_office_window_and_back_returns_to_main_hub()
    {
        var navigation = new FakeNavigationService();
        var propertyContext = new FakePropertyContextHost();
        var windows = new WindowManagerService();
        using var reservation = new ReservationHubViewModel(new FakePermissions(), propertyContext, new FakeModalService(), navigation);
        using var reception = new ReceptionHubViewModel(new FakeReceptionPermissions(), propertyContext, new FakeReceptionModalService(), navigation);
        using var hub = new FrontOfficeHubViewModel(
            new FakePermissions(),
            propertyContext,
            navigation,
            new FakeCurrentUserService(),
            new TestServiceProvider(reservation, reception),
            windows);
        var window = windows.OpenOrActivate(new AppModuleInfo("front-office", "Front Office", "", "", "#2563EB", false, false), () => hub);
        var currentProperty = propertyContext.CurrentProperty;

        await ((IAsyncRelayCommand)hub.Cards.Single(x => x.Id == "reception").Command).ExecuteAsync(null);

        hub.ActiveWorkspace.Should().BeSameAs(reception);
        windows.OpenWindows.Should().ContainSingle().Which.Should().BeSameAs(window);
        window.Module.Title.Should().Be("Lễ tân");
        propertyContext.CurrentProperty.Should().BeSameAs(currentProperty);

        await reception.BackCommand.ExecuteAsync(null);

        hub.ActiveWorkspace.Should().BeNull();
        window.Module.Title.Should().Be("Front Office");
        propertyContext.CurrentProperty.Should().BeSameAs(currentProperty);
    }

    [Fact]
    public void FO_HUB_U_003_coming_soon_card_opens_inline_dialog()
    {
        using var hub = CreateHub();

        hub.Cards.Single(x => x.Id == "cashier").Command.Execute(null);

        hub.IsComingSoonDialogOpen.Should().BeTrue();
        hub.ComingSoonTitle.Should().Be("Thu ngân");
    }

    [Fact]
    public void FO_MAIN_U_007_008_coming_soon_does_not_navigate_and_opens_the_safe_overlay()
    {
        var navigation = new FakeNavigationService();
        using var hub = CreateHub(navigation: navigation);

        hub.Cards.Single(x => x.Id == "cashier").Command.Execute(null);

        navigation.OpenedRoutes.Should().BeEmpty();
        hub.IsComingSoonDialogOpen.Should().BeTrue();
    }

    [Fact]
    public void FO_HUB_U_004_reservation_permission_disables_only_the_active_card()
    {
        using var hub = CreateHub(permissions: new FakePermissions { CanViewValue = false });

        hub.Cards.Single(x => x.Id == "reservation").IsEnabled.Should().BeFalse();
        hub.Cards.Single(x => x.Id == "reception").IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task FO_HUB_U_005_back_from_reservation_returns_to_navigation_history()
    {
        var navigation = new FakeNavigationService();
        using var reservation = new ReservationHubViewModel(new FakePermissions(), new FakePropertyContextHost(), new FakeModalService(), navigation);

        await reservation.BackCommand.ExecuteAsync(null);

        navigation.BackCalls.Should().Be(1);
    }

    [Fact]
    public void FO_HUB_U_006_main_hub_does_not_depend_on_reservation_repository_or_change_property_id()
    {
        var constructorTypes = typeof(FrontOfficeHubViewModel).GetConstructors().Single().GetParameters().Select(x => x.ParameterType.Name);

        constructorTypes.Should().NotContain(x => x.Contains("Repository", StringComparison.OrdinalIgnoreCase));
        typeof(FrontOfficeHubViewModel).GetProperties().Select(x => x.Name).Should().NotContain("PropertyId");
    }

    private static FrontOfficeHubViewModel CreateHub(FakePermissions? permissions = null, FakeNavigationService? navigation = null)
        => new(permissions ?? new FakePermissions(), new FakePropertyContextHost(), navigation ?? new FakeNavigationService(), new FakeCurrentUserService());

    private sealed class FakePermissions : IReservationHubPermissionReader
    {
        public bool HasContext => true;
        public bool CanView => CanViewValue;
        public bool CanCreate => true;
        public bool CanUpdate => true;
        public bool CanAssignRoom => true;
        public bool CanChangeStatus => true;
        public bool CanCancel => true;
        public bool CanViewValue { get; set; } = true;
        public void Dispose() { }
    }

    private sealed class FakePropertyContextHost : IFrontOfficePropertyContextHost
    {
        private readonly FrontOfficePropertyOption property = new(Guid.NewGuid(), "MAIN", "Main Branch", "MAIN - Main Branch", true);
        public FrontOfficePropertyOption? CurrentProperty => property;
        public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => [property];
        public bool HasCurrentProperty => true;
        public int Version => 1;
        public string? ErrorCode => null;
        public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged { add { } remove { } }
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public event Action<string>? Navigated;
        public List<string> OpenedRoutes { get; } = [];
        public int BackCalls { get; private set; }
        public void Navigate(string key) => Navigated?.Invoke(key);
        public IReadOnlyList<AppModuleInfo> GetModuleCatalog() => [];
        public string GetPermissionCode(string route) => "FrontOffice.Reservation.View";
        public Task<NavigationResult> OpenModuleAsync(string route, CancellationToken cancellationToken = default) { OpenedRoutes.Add(route); return Task.FromResult(new NavigationResult { Success = true, Route = route }); }
        public Task<NavigationResult> OpenJournalAsync(Guid journalId, CancellationToken cancellationToken = default) => Task.FromResult(new NavigationResult { Success = true });
        public Task<NavigationResult> OpenVoucherAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult(new NavigationResult { Success = true });
        public Task<NavigationResult> OpenReportAsync(string reportCode, CancellationToken cancellationToken = default) => Task.FromResult(new NavigationResult { Success = true });
        public Task<NavigationResult> OpenInNewTabAsync(NavigationRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new NavigationResult { Success = true });
        public Task<NavigationResult> GoBackAsync(CancellationToken cancellationToken = default) { BackCalls++; return Task.FromResult(new NavigationResult { Success = true, Route = "front-office" }); }
        public Task<NavigationResult> GoForwardAsync(CancellationToken cancellationToken = default) => Task.FromResult(new NavigationResult { Success = true });
        public Task<IReadOnlyList<NavigationHistoryItem>> RestoreWorkspaceAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<NavigationHistoryItem>>([]);
    }

    private sealed class FakeModalService : IReservationWorkspaceModalService
    {
        public ReservationModalWorkspaceViewModel? ActiveModal => null;
        public bool IsOpen => false;
        public string? FocusRestoreCardId => null;
        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public Task<bool> OpenAsync(ReservationModalKind kind, string sourceCardId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void Close() { }
        public void Dispose() { }
    }

    private sealed class TestServiceProvider(ReservationHubViewModel reservation, ReceptionHubViewModel? reception = null) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ReservationHubViewModel) ? reservation
            : serviceType == typeof(ReceptionHubViewModel) ? reception
            : null;
    }

    private sealed class FakeReceptionPermissions : IReceptionHubPermissionReader
    {
        public bool HasContext => true;
        public bool CanView => true;
        public void ResetForContextChange() { }
        public void Dispose() { }
    }

    private sealed class FakeReceptionModalService : IReceptionWorkspaceModalService
    {
        public ReceptionReadOnlyWorkspaceViewModel? ActiveModal => null;
        public bool IsOpen => false;
        public string? FocusRestoreCardId => null;
        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public Task<bool> OpenAsync(ReceptionWorkspaceKind kind, string sourceCardId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void Close() { }
        public void Dispose() { }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser => null;
        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }
}
