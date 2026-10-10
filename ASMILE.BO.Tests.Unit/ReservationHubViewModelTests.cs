using System.ComponentModel;
using System.Windows.Input;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationHubViewModelTests
{
    [Fact]
    public void HUB_RES_U_001_loads_cards_in_display_order()
    {
        using var hub = CreateHub();

        hub.Cards.Select(x => x.DisplayOrder).Should().BeInAscendingOrder();
        hub.Cards.Select(x => x.Id).Should().ContainInOrder("new", "edit", "assign", "confirm", "cancel");
        hub.Cards.Should().NotContain(x => x.Id == "search");
        hub.Cards.Should().NotContain(x => x.Id.Contains("report", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HUB_RES_U_002_to_005_permission_cards_are_hidden_without_the_required_permission()
    {
        using var hub = CreateHub(new FakePermissions { CanCreateValue = false, CanAssignValue = false, CanCancelValue = false });

        hub.Cards.Single(x => x.Id == "new").IsVisible.Should().BeFalse();
        hub.Cards.Single(x => x.Id == "assign").IsVisible.Should().BeFalse();
        hub.Cards.Single(x => x.Id == "cancel").IsVisible.Should().BeFalse();
        hub.Cards.Single(x => x.Id == "edit").IsVisible.Should().BeTrue();
    }

    [Fact]
    public void HUB_RES_U_006_to_008_web_card_is_enabled_and_search_card_is_not_duplicated()
    {
        var modal = new FakeModalService();
        using var hub = CreateHub(modal: modal);

        var web = hub.Cards.Single(x => x.Id == "web");
        web.IsEnabled.Should().BeTrue();
        web.IsComingSoon.Should().BeFalse();
        web.BadgeText.Should().Be("Đang hoạt động");
        web.DisabledReason.Should().BeNull();
        modal.OpenCalls.Should().BeEmpty();

        hub.Cards.Should().NotContain(x => x.Id == "search");
        hub.Cards.Single(x => x.Id == "edit").Title.Should().Be("Sửa đặt phòng");
    }

    [Fact]
    public async Task HUB_RES_U_007_card_command_opens_the_matching_modal()
    {
        var modal = new FakeModalService();
        using var hub = CreateHub(modal: modal);

        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)hub.Cards.Single(x => x.Id == "new").Command).ExecuteAsync(null);

        modal.OpenCalls.Should().ContainSingle(x => x.Kind == ReservationModalKind.New && x.SourceCardId == "new");
    }

    [Fact]
    public void HUB_RES_U_011_to_012_property_change_closes_modal_and_hub_has_no_property_id_input()
    {
        var host = new FakePropertyContextHost();
        var modal = new FakeModalService();
        using var hub = new ReservationHubViewModel(new FakePermissions(), host, modal, null!);

        host.Raise(null, "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");

        modal.CloseCalls.Should().Be(1);
        hub.HasPropertyContext.Should().BeFalse();
        typeof(ReservationHubViewModel).GetProperties().Select(x => x.Name).Should().NotContain("PropertyId");
    }

    [Fact]
    public void HUB_RES_U_013_hub_has_no_repository_dependency()
    {
        var constructorTypes = typeof(ReservationHubViewModel).GetConstructors().Single().GetParameters().Select(x => x.ParameterType.Name);

        constructorTypes.Should().NotContain(x => x.Contains("Repository", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HUB_RES_U_015_refresh_rebuilds_permission_aware_cards()
    {
        var permissions = new FakePermissions { CanCreateValue = false };
        using var hub = CreateHub(permissions);
        hub.Cards.Single(x => x.Id == "new").IsVisible.Should().BeFalse();

        permissions.CanCreateValue = true;
        hub.RefreshCommand.Execute(null);

        hub.Cards.Single(x => x.Id == "new").IsVisible.Should().BeTrue();
    }

    private static ReservationHubViewModel CreateHub(FakePermissions? permissions = null, FakeModalService? modal = null, IServiceProvider? serviceProvider = null)
        => new(permissions ?? new FakePermissions(), new FakePropertyContextHost(), modal ?? new FakeModalService(), serviceProvider!);

    private sealed class FakePermissions : IReservationHubPermissionReader
    {
        public bool HasContext => true;
        public bool CanView => true;
        public bool CanCreate => CanCreateValue;
        public bool CanUpdate => true;
        public bool CanAssignRoom => CanAssignValue;
        public bool CanChangeStatus => true;
        public bool CanCancel => CanCancelValue;
        public bool CanCreateValue { get; set; } = true;
        public bool CanAssignValue { get; set; } = true;
        public bool CanCancelValue { get; set; } = true;
        public void Dispose() { }
    }

    private sealed class FakeModalService : IReservationWorkspaceModalService
    {
        public ReservationModalWorkspaceViewModel? ActiveModal => null;
        public bool IsOpen { get; private set; }
        public string? FocusRestoreCardId { get; private set; }
        public int CloseCalls { get; private set; }
        public List<(ReservationModalKind Kind, string SourceCardId)> OpenCalls { get; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        public Task<bool> OpenAsync(ReservationModalKind kind, string sourceCardId, CancellationToken cancellationToken = default)
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
            FocusRestoreCardId = OpenCalls.LastOrDefault().SourceCardId;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FocusRestoreCardId)));
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
