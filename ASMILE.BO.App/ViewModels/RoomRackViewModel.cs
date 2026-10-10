using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ASMILE.BO.Application.FrontOffice.RoomRack;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class RoomRackViewModel : ViewModelBase
{
    private readonly IRoomRackQueryService _service;
    private readonly IServiceProvider? _serviceProvider;
    private CancellationTokenSource? _refreshCts;

    public ObservableCollection<RoomRackDto> Rooms { get; } = new();
    public ObservableCollection<RoomRackAlertDto> Alerts { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private RoomRackDto? _selectedRoom;
    [ObservableProperty] private bool _isDrawerOpen;
    [ObservableProperty] private int _drawerTabIndex;
    [ObservableProperty] private bool _isAlertDrawerOpen;
    [ObservableProperty] private bool _isLegendOpen = true;
    [ObservableProperty] private bool _isContextMenuOpen;

    // Filters
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterOccupancy;
    [ObservableProperty] private string? _filterHousekeeping;
    [ObservableProperty] private bool? _filterVip;
    [ObservableProperty] private bool? _filterArrivingToday;
    [ObservableProperty] private bool? _filterDepartingToday;

    // Stats
    [ObservableProperty] private int _totalRooms;
    [ObservableProperty] private int _occupiedRooms;
    [ObservableProperty] private int _vacantCleanRooms;
    [ObservableProperty] private int _dirtyRooms;
    [ObservableProperty] private int _oooRooms;
    [ObservableProperty] private int _alertCount;
    [ObservableProperty] private string _lastRefreshTime = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedRoom is not null;
    public bool HasAlerts => AlertCount > 0;
    public bool CanCheckIn => SelectedRoom is { OccupancyStatus: "Reserved", HousekeepingStatus: "Clean" or "Inspected", InventoryStatus: "Available" };
    public bool CanCheckOut => SelectedRoom is { OccupancyStatus: "Occupied" };
    public bool CanAssignRoom => SelectedRoom is { OccupancyStatus: "Vacant", InventoryStatus: "Available" };

    public string[] Buildings { get; } = ["Tat ca", "Toa A"];
    public string[] Floors { get; } = ["Tat ca", "Tang 1", "Tang 2", "Tang 3"];
    public string[] OccupancyFilters { get; } = ["Tat ca", "Vacant", "Reserved", "Occupied", "DueOut"];
    public string[] HousekeepingFilters { get; } = ["Tat ca", "Clean", "Dirty", "Cleaning", "Inspected", "OutOfOrder", "OutOfService"];

    public RoomRackViewModel(IRoomRackQueryService service, IServiceProvider? serviceProvider = null)
    {
        _service = service;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var previousCts = Interlocked.Exchange(ref _refreshCts, requestCts);
        previousCts?.Cancel();
        var requestToken = requestCts.Token;
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            var f = new RoomRackFilterDto(Norm(FilterBuilding), Norm(FilterFloor), null, Norm(FilterOccupancy), Norm(FilterHousekeeping), null, FilterVip, FilterArrivingToday, FilterDepartingToday);
            var result = await _service.GetRoomRackAsync(new GetRoomRackQuery(f, SearchText, new PageRequest(1, 5000)), requestToken);
            if (result.IsFailure) { ErrorMessage = "Khong the tai so do phong."; return; }
            Rooms.Clear();
            foreach (var r in result.Value!.Items) Rooms.Add(r);
            TotalRooms = Rooms.Count;
            OccupiedRooms = Rooms.Count(r => r.OccupancyStatus == "Occupied");
            VacantCleanRooms = Rooms.Count(r => r.HousekeepingStatus == "Clean" && r.OccupancyStatus == "Vacant");
            DirtyRooms = Rooms.Count(r => r.HousekeepingStatus == "Dirty");
            OooRooms = Rooms.Count(r => r.InventoryStatus is "OutOfOrder" or "OutOfService");
            var alerts = await _service.GetAlertsAsync(new GetRoomRackAlertsQuery(Guid.Empty), requestToken);
            Alerts.Clear();
            if (alerts.IsSuccess) foreach (var a in alerts.Value!) Alerts.Add(a);
            AlertCount = Alerts.Count;
            LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally
        {
            IsLoading = false;
            Interlocked.CompareExchange(ref _refreshCts, null, requestCts);
        }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private void SelectRoom(RoomRackDto? room) { SelectedRoom = room; IsDrawerOpen = room is not null; DrawerTabIndex = 0; NotifyContextActions(); }
    [RelayCommand] private void CloseDrawer() { IsDrawerOpen = false; SelectedRoom = null; }
    [RelayCommand] private void ShowTab(int index) => DrawerTabIndex = index;
    [RelayCommand] private void ToggleAlerts() => IsAlertDrawerOpen = !IsAlertDrawerOpen;
    [RelayCommand] private void ToggleLegend() => IsLegendOpen = !IsLegendOpen;

    // Quick Actions. The rack DTO is intentionally read-only and does not carry
    // a StayId/ReservationId, so these actions open the real workflow for the
    // selected room instead of pretending that a mutation was completed.
    [RelayCommand] private void QuickCheckIn()
    {
        if (!CanCheckIn) return;
        try
        {
            var workspace = _serviceProvider?.GetRequiredService<ReservationWorkspaceViewModel>();
            if (workspace is null) { InfoMessage = "Chưa cấu hình được luồng Check-in thật."; return; }
            workspace.SelectedStatus = Core.FrontOffice.Reservation.ReservationStatus.Confirmed;
            ShowOrActivate(new Views.CheckInWindow(workspace));
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở Check-in: {ex.Message}"; }
    }

    [RelayCommand] private void QuickCheckOut()
    {
        if (!CanCheckOut) return;
        try { ShowOrActivate(new Views.CheckoutWindow()); }
        catch (Exception ex) { ErrorMessage = $"Không thể mở Check-out: {ex.Message}"; }
    }

    [RelayCommand] private void QuickOpenFolio()
    {
        if (!HasSelection) return;
        try { ShowOrActivate(new Views.FolioWorkspaceWindow()); }
        catch (Exception ex) { ErrorMessage = $"Không thể mở Folio: {ex.Message}"; }
    }

    [RelayCommand] private void QuickGuestProfile()
    {
        if (!HasSelection) return;
        try { ShowOrActivate(new Views.CurrentGuestsWindow()); }
        catch (Exception ex) { ErrorMessage = $"Không thể mở hồ sơ khách: {ex.Message}"; }
    }

    [RelayCommand] private void QuickHousekeeping()
    {
        if (!HasSelection) return;
        try { ShowOrActivate(new Views.HousekeepingRoomStatusWindow()); }
        catch (Exception ex) { ErrorMessage = $"Không thể mở Housekeeping: {ex.Message}"; }
    }

    [RelayCommand] private void QuickAssignRoom()
    {
        if (!CanAssignRoom) return;
        try
        {
            var workspace = _serviceProvider?.GetRequiredService<ReservationWorkspaceViewModel>();
            if (workspace is null) { InfoMessage = "Chưa cấu hình được luồng phân phòng thật."; return; }
            ShowOrActivate(new Views.RoomAssignmentWindow(workspace));
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở phân phòng: {ex.Message}"; }
    }

    private static void ShowOrActivate(Window window)
    {
        var existing = global::System.Windows.Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(candidate => candidate.GetType() == window.GetType() && candidate.IsLoaded);
        if (existing is not null)
        {
            window.Close();
            if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        window.Owner = global::System.Windows.Application.Current?.MainWindow;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Show();
    }

    // Keyboard
    public void HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Escape: if (IsDrawerOpen) CloseDrawer(); else if (IsAlertDrawerOpen) IsAlertDrawerOpen = false; break;
            case Key.F5: RefreshCommand.Execute(null); break;
            case Key.F3: SearchCommand.Execute(null); break;
            case Key.D1: ShowTab(0); break;
            case Key.D2: ShowTab(1); break;
            case Key.D3: ShowTab(2); break;
            case Key.D4: ShowTab(3); break;
            case Key.D5: ShowTab(4); break;
        }
    }

    private static string? Norm(string? s) => s is "Tat ca" or "" ? null : s;
    partial void OnSelectedRoomChanged(RoomRackDto? value) { OnPropertyChanged(nameof(HasSelection)); NotifyContextActions(); }
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnAlertCountChanged(int value) => OnPropertyChanged(nameof(HasAlerts));
    private void NotifyContextActions() { OnPropertyChanged(nameof(CanCheckIn)); OnPropertyChanged(nameof(CanCheckOut)); OnPropertyChanged(nameof(CanAssignRoom)); }
    public void Dispose() => _refreshCts?.Cancel();
}
