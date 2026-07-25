using System.Collections.ObjectModel;
using System.Windows.Input;
using ASMILE.BO.Application.FrontOffice.RoomRack;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class RoomRackViewModel : ViewModelBase
{
    private readonly IRoomRackQueryService _service;
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

    public RoomRackViewModel(IRoomRackQueryService service) => _service = service;

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            var f = new RoomRackFilterDto(Norm(FilterBuilding), Norm(FilterFloor), null, Norm(FilterOccupancy), Norm(FilterHousekeeping), null, FilterVip, FilterArrivingToday, FilterDepartingToday);
            var result = await _service.GetRoomRackAsync(new GetRoomRackQuery(f, SearchText, new PageRequest(1, 5000)), ct);
            if (result.IsFailure) { ErrorMessage = "Khong the tai so do phong."; return; }
            Rooms.Clear();
            foreach (var r in result.Value!.Items) Rooms.Add(r);
            TotalRooms = Rooms.Count;
            OccupiedRooms = Rooms.Count(r => r.OccupancyStatus == "Occupied");
            VacantCleanRooms = Rooms.Count(r => r.HousekeepingStatus == "Clean" && r.OccupancyStatus == "Vacant");
            DirtyRooms = Rooms.Count(r => r.HousekeepingStatus == "Dirty");
            OooRooms = Rooms.Count(r => r.InventoryStatus is "OutOfOrder" or "OutOfService");
            var alerts = await _service.GetAlertsAsync(new GetRoomRackAlertsQuery(Guid.Empty), ct);
            Alerts.Clear();
            if (alerts.IsSuccess) foreach (var a in alerts.Value!) Alerts.Add(a);
            AlertCount = Alerts.Count;
            LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private void SelectRoom(RoomRackDto? room) { SelectedRoom = room; IsDrawerOpen = room is not null; DrawerTabIndex = 0; NotifyContextActions(); }
    [RelayCommand] private void CloseDrawer() { IsDrawerOpen = false; SelectedRoom = null; }
    [RelayCommand] private void ShowTab(int index) => DrawerTabIndex = index;
    [RelayCommand] private void ToggleAlerts() => IsAlertDrawerOpen = !IsAlertDrawerOpen;
    [RelayCommand] private void ToggleLegend() => IsLegendOpen = !IsLegendOpen;

    // Quick Actions
    [RelayCommand] private void QuickCheckIn() { InfoMessage = $"Check-in: {SelectedRoom?.RoomNumber}"; }
    [RelayCommand] private void QuickCheckOut() { InfoMessage = $"Check-out: {SelectedRoom?.RoomNumber}"; }
    [RelayCommand] private void QuickOpenFolio() { InfoMessage = $"Folio: {SelectedRoom?.RoomNumber}"; }
    [RelayCommand] private void QuickGuestProfile() { InfoMessage = $"Guest: {SelectedRoom?.GuestName}"; }
    [RelayCommand] private void QuickHousekeeping() { InfoMessage = $"Housekeeping: {SelectedRoom?.RoomNumber}"; }
    [RelayCommand] private void QuickAssignRoom() { InfoMessage = $"Assign: {SelectedRoom?.RoomNumber}"; }

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
    partial void OnSelectedRoomChanged(RoomRackDto? v) { OnPropertyChanged(nameof(HasSelection)); NotifyContextActions(); }
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
    partial void OnAlertCountChanged(int v) => OnPropertyChanged(nameof(HasAlerts));
    private void NotifyContextActions() { OnPropertyChanged(nameof(CanCheckIn)); OnPropertyChanged(nameof(CanCheckOut)); OnPropertyChanged(nameof(CanAssignRoom)); }
    public void Dispose() => _refreshCts?.Cancel();
}
