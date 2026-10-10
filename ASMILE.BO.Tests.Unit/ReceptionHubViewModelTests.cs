using System.ComponentModel;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReceptionHubViewModelTests
{
    [Fact]
    public void REC_HUB_U_001_catalog_contains_the_current_reception_operations()
    {
        using var hub = CreateHub();

        hub.Cards.Should().HaveCount(13);
        hub.Cards.Where(card => card.IsReadOnly).Should().BeEmpty();
        hub.Cards.Where(card => card.IsComingSoon).Should().BeEmpty();
        hub.Cards.Select(card => card.Id).Should().Contain([
            "room-status", "check-in", "walk-in", "stay-adjustment", "checkout",
            "stay-profile", "locked-room", "current-guests", "arrivals", "departures",
            "housekeeping-message", "exchange-rate", "group-check-in"]);
        hub.Cards.Select(card => card.Id).Should().NotContain("room-key-reader");
        hub.Cards.Should().NotContain(card => card.Id.Contains("report", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task REC_HUB_U_002_to_004_read_only_cards_open_matching_modal_and_active_cards_remain_enabled()
    {
        var modal = new FakeModalService();
        using var hub = CreateHub(modal: modal);

        await ((IAsyncRelayCommand)hub.Cards.Single(card => card.Id == "locked-room").Command).ExecuteAsync(null);
        modal.OpenCalls.Should().ContainSingle().Which.Should().Be((ReceptionWorkspaceKind.LockedRooms, "locked-room"));

        modal.Close();
        await ((IAsyncRelayCommand)hub.Cards.Single(card => card.Id == "stay-profile").Command).ExecuteAsync(null);
        modal.OpenCalls.Should().Contain((ReceptionWorkspaceKind.StayProfiles, "stay-profile"));

        hub.Cards.Single(card => card.Id == "walk-in").IsComingSoon.Should().BeFalse();
        hub.Cards.Single(card => card.Id == "walk-in").IsEnabled.Should().BeTrue();
        hub.IsComingSoonDialogOpen.Should().BeFalse();
        modal.OpenCalls.Should().HaveCount(2);
    }

    [Fact]
    public async Task REC_HUB_U_003_read_only_cards_use_the_real_query_workspace_when_di_is_available()
    {
        var modal = new FakeModalService();
        using var hub = CreateHub(modal: modal, serviceProvider: new EmptyServiceProvider());

        await ((IAsyncRelayCommand)hub.Cards.Single(card => card.Id == "arrivals").Command).ExecuteAsync(null);

        modal.OpenCalls.Should().ContainSingle().Which.Should().Be((ReceptionWorkspaceKind.ArrivalsToday, "arrivals"));
    }

    [Fact]
    public async Task REC_HUB_U_004_checkout_card_uses_the_real_checkout_modal()
    {
        var checkout = new FakeCheckOutModalService();
        using var hub = CreateHub(checkOutModalService: checkout);

        await ((IAsyncRelayCommand)hub.Cards.Single(card => card.Id == "checkout").Command).ExecuteAsync(null);

        checkout.OpenCalls.Should().ContainSingle().Which.Should().Be("checkout");
        hub.ActiveWorkspace.Should().BeNull();
    }

    [Fact]
    public async Task REC_HUB_U_005_back_closes_modal_and_returns_to_parent_without_changing_context()
    {
        var host = new FakePropertyContextHost();
        var modal = new FakeModalService();
        using var hub = CreateHub(host: host, modal: modal);
        var parentCalls = 0;
        hub.ReturnToParentAsync = () => { parentCalls++; return Task.CompletedTask; };

        await hub.BackCommand.ExecuteAsync(null);

        modal.CloseCalls.Should().Be(1);
        parentCalls.Should().Be(1);
        host.CurrentProperty.Should().NotBeNull();
    }

    [Fact]
    public void REC_HUB_U_006_property_change_closes_modal_resets_permission_scope_and_has_no_repository_dependency()
    {
        var host = new FakePropertyContextHost();
        var permissions = new FakePermissions();
        var modal = new FakeModalService();
        using var hub = CreateHub(permissions, host, modal);

        host.Raise(null, "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");

        modal.CloseCalls.Should().Be(1);
        permissions.ResetCalls.Should().Be(1);
        hub.HasPropertyContext.Should().BeFalse();
        typeof(ReceptionHubViewModel).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType.Name)
            .Should().NotContain(name => name.Contains("Repository", StringComparison.OrdinalIgnoreCase));
    }

    private static ReceptionHubViewModel CreateHub(FakePermissions? permissions = null, FakePropertyContextHost? host = null, FakeModalService? modal = null, IServiceProvider? serviceProvider = null, ILockedRoomWindowService? lockedRoomWindowService = null, ICheckOutStayModalService? checkOutModalService = null)
        => new(permissions ?? new FakePermissions(), host ?? new FakePropertyContextHost(), modal ?? new FakeModalService(), serviceProvider!, lockedRoomWindowService!, checkOutModalService: checkOutModalService);

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class FakeCheckOutModalService : ICheckOutStayModalService
    {
        public CheckOutStayModalViewModel? ActiveModal => null;
        public bool IsOpen { get; private set; }
        public List<string> OpenCalls { get; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        public Task<bool> OpenAsync(string sourceCardId, CancellationToken cancellationToken = default)
        {
            IsOpen = true;
            OpenCalls.Add(sourceCardId);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
            return Task.FromResult(true);
        }
        public void Close() => IsOpen = false;
        public void Dispose() { }
    }

    private sealed class FakePermissions : IReceptionHubPermissionReader
    {
        public bool HasContext => HasContextValue;
        public bool CanView => CanViewValue;
        public bool HasContextValue { get; set; } = true;
        public bool CanViewValue { get; set; } = true;
        public int ResetCalls { get; private set; }
        public void ResetForContextChange() => ResetCalls++;
        public void Dispose() { }
    }

    private sealed class FakeModalService : IReceptionWorkspaceModalService
    {
        public ReceptionReadOnlyWorkspaceViewModel? ActiveModal => null;
        public bool IsOpen { get; private set; }
        public string? FocusRestoreCardId => null;
        public int CloseCalls { get; private set; }
        public List<(ReceptionWorkspaceKind Kind, string SourceCardId)> OpenCalls { get; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        public Task<bool> OpenAsync(ReceptionWorkspaceKind kind, string sourceCardId, CancellationToken cancellationToken = default)
        {
            if (IsOpen) return Task.FromResult(false);
            IsOpen = true;
            OpenCalls.Add((kind, sourceCardId));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
            return Task.FromResult(true);
        }
        public void Close()
        {
            CloseCalls++;
            IsOpen = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
        }
        public void Dispose() { }
    }

    private sealed class FakePropertyContextHost : IFrontOfficePropertyContextHost
    {
        private readonly FrontOfficePropertyOption property = new(Guid.NewGuid(), "MAIN", "Main Branch", "MAIN - Main Branch", true);
        public FrontOfficePropertyOption? CurrentProperty { get; private set; }
        public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => [property];
        public bool HasCurrentProperty => CurrentProperty is not null;
        public int Version { get; private set; } = 1;
        public string? ErrorCode { get; private set; }
        public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged;
        public FakePropertyContextHost() => CurrentProperty = property;
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Raise(FrontOfficePropertyOption? current, string? errorCode = null)
        {
            var previous = CurrentProperty;
            CurrentProperty = current;
            ErrorCode = errorCode;
            Version++;
            PropertyChanged?.Invoke(this, new FrontOfficePropertyContextChangedEventArgs(previous, current, Version, errorCode));
        }
        public void Dispose() { }
    }
}
