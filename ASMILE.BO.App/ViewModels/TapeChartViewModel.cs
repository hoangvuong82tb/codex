using System.Collections.ObjectModel;
using System.Windows.Input;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class TapeChartRoomDto
{
    public Guid HotelRoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public string? Building { get; set; }
    public string InventoryStatus { get; set; } = "Available";
    public bool IsOoo => InventoryStatus is "OutOfOrder" or "OutOfService";
    public List<TapeChartBar> Bars { get; set; } = new();
    public double RowY { get; set; }
    public double RowHeight { get; set; }
}

public sealed class TapeChartBar
{
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public double BarLeft { get; set; }
    public double BarWidth { get; set; }
    public string ColorCode { get; set; } = "#2563EB";
    public bool HasConflict { get; set; }
    public string? ConflictReason { get; set; }
    public string? Company { get; set; }
    public string? GroupName { get; set; }
    public decimal? DepositBalance { get; set; }
    public decimal? FolioBalance { get; set; }
    public string? Notes { get; set; }
    public decimal DailyRate { get; set; }
    public decimal? PackageAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal TotalAmount => DailyRate * (DepartureDate.DayNumber - ArrivalDate.DayNumber) - (DiscountAmount ?? 0) + (PackageAmount ?? 0);
}

public sealed partial class TapeChartViewModel : ViewModelBase
{
    private readonly IAvailabilityEngine? _availability;
    private readonly IFrontOfficePropertyContextHost _propertyContextHost;

    public ObservableCollection<TapeChartRoomDto> Rooms { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _zoomDays = 7;
    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private TapeChartBar? _selectedBar;
    [ObservableProperty] private TapeChartRoomDto? _dragSourceRoom;
    [ObservableProperty] private TapeChartBar? _dragBar;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private int _totalRooms;
    [ObservableProperty] private int _totalReservations;
    [ObservableProperty] private int _conflictCount;
    [ObservableProperty] private string _lastRefreshTime = string.Empty;

    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private bool? _filterVip;
    [ObservableProperty] private string? _filterRoomType;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedBar is not null;
    public bool HasConflicts => ConflictCount > 0;
    public string[] ZoomOptions { get; } = ["1", "3", "7", "14", "30"];
    public string[] StatusFilters { get; } = ["Tat ca", "Confirmed", "InHouse", "CheckedOut", "NoShow", "Cancelled"];
    public string[] BuildingFilters { get; } = ["Tat ca", "Toa A"];
    public string[] FloorFilters { get; } = ["Tat ca", "Tang 1", "Tang 2", "Tang 3"];
    public DateTime[] DateHeaders => Enumerable.Range(0, ZoomDays).Select(i => StartDate.AddDays(i).Date).ToArray();

    public TapeChartViewModel(IAvailabilityEngine? availability = null, IFrontOfficePropertyContextHost? propertyContextHost = null)
    {
        _availability = availability;
        _propertyContextHost = propertyContextHost ?? throw new ArgumentNullException(nameof(propertyContextHost));
        StartDate = DateTime.Today.AddDays(-1);
    }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Rooms.Clear();
            if (_availability is not null && _propertyContextHost.HasCurrentProperty && _propertyContextHost.CurrentProperty is not null)
            {
                var propertyId = _propertyContextHost.CurrentProperty.PropertyId;
                var businessDate = DateOnly.FromDateTime(DateTime.Today);
                var fromDate = DateOnly.FromDateTime(StartDate);

                var availability = await _availability.GetRoomAvailabilityAsync(propertyId, businessDate, ct);

                foreach (var roomAvail in availability.OrderBy(r => r.RoomNumber))
                {
                    var room = new TapeChartRoomDto
                    {
                        HotelRoomId = roomAvail.HotelRoomId,
                        RoomNumber = roomAvail.RoomNumber,
                        RoomTypeName = roomAvail.RoomTypeName,
                        Floor = roomAvail.Floor,
                        Building = roomAvail.Building,
                        InventoryStatus = roomAvail.State.ToString()
                    };

                    if (roomAvail.ReservationNumber is not null)
                    {
                        var bar = new TapeChartBar
                        {
                            ReservationNumber = roomAvail.ReservationNumber,
                            GuestName = roomAvail.GuestName ?? "Unknown",
                            ArrivalDate = roomAvail.ArrivalDate ?? businessDate,
                            DepartureDate = roomAvail.DepartureDate ?? businessDate.AddDays(1),
                            Status = roomAvail.State.ToString(),
                            RoomNumber = roomAvail.RoomNumber,
                            IsVip = roomAvail.IsVip,
                            ColorCode = roomAvail.State switch
                            {
                                Core.FrontOffice.Availability.RoomAvailabilityState.OccupiedClean => "#16A34A",
                                Core.FrontOffice.Availability.RoomAvailabilityState.OccupiedDirty => "#EA580C",
                                Core.FrontOffice.Availability.RoomAvailabilityState.Reserved => "#2563EB",
                                Core.FrontOffice.Availability.RoomAvailabilityState.ExpectedArrival => "#7C3AED",
                                Core.FrontOffice.Availability.RoomAvailabilityState.ExpectedDeparture => "#F2994A",
                                Core.FrontOffice.Availability.RoomAvailabilityState.DueOut => "#DC2626",
                                _ => "#2563EB"
                            }
                        };
                        room.Bars.Add(bar);
                    }

                    Rooms.Add(room);
                }
            }

            DetectConflicts();
            TotalRooms = Rooms.Count;
            TotalReservations = Rooms.Sum(r => r.Bars.Count);
            ConflictCount = Rooms.Sum(r => r.Bars.Count(b => b.HasConflict));
            LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void DetectConflicts()
    {
        foreach (var r in Rooms)
        {
            foreach (var b in r.Bars)
            {
                b.HasConflict = false; b.ConflictReason = null;
                if (r.IsOoo) { b.HasConflict = true; b.ConflictReason = $"Phong {r.InventoryStatus}"; b.ColorCode = "#DC2626"; continue; }
                var others = Rooms.Where(x => x != r).SelectMany(x => x.Bars)
                    .Where(x => x.Status is "Confirmed" or "InHouse" or "Reserved" or "Guaranteed").ToList();
                foreach (var o in others)
                {
                    if (b.ArrivalDate < o.DepartureDate && b.DepartureDate > o.ArrivalDate && b.RoomNumber == o.RoomNumber)
                    { b.HasConflict = true; b.ConflictReason = $"Trung voi {o.GuestName} ({o.ReservationNumber})"; b.ColorCode = "#DC2626"; }
                }
            }
        }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectBar(TapeChartBar? bar) { SelectedBar = bar; IsDetailOpen = bar is not null; }

    [RelayCommand] private void StartDrag(TapeChartRoomDto room) { DragSourceRoom = room; }
    [RelayCommand] private void CancelDrag() { DragSourceRoom = null; DragBar = null; }

    [RelayCommand]
    private void DropOnRoom(TapeChartRoomDto targetRoom)
    {
        if (DragSourceRoom is null || DragSourceRoom == targetRoom) return;
        if (DragBar is null) { var bar = DragSourceRoom.Bars.FirstOrDefault(); if (bar is null) return; DragBar = bar; }
        if (targetRoom.IsOoo) { InfoMessage = $"Khong the chuyen den phong {targetRoom.RoomNumber}: phong {targetRoom.InventoryStatus}."; return; }
        var oldRoom = DragSourceRoom.RoomNumber;
        DragSourceRoom.Bars.Remove(DragBar);
        DragBar.RoomNumber = targetRoom.RoomNumber;
        targetRoom.Bars.Add(DragBar);
        DetectConflicts();
        InfoMessage = $"Da chuyen {DragBar.GuestName} tu phong {oldRoom} den phong {targetRoom.RoomNumber}.";
        DragSourceRoom = null; DragBar = null;
    }

    [RelayCommand]
    private void ExtendStay(int days)
    {
        if (SelectedBar is null) return;
        SelectedBar.DepartureDate = SelectedBar.DepartureDate.AddDays(days);
        if (SelectedBar.DepartureDate <= SelectedBar.ArrivalDate) { SelectedBar.DepartureDate = SelectedBar.ArrivalDate.AddDays(1); InfoMessage = "Ngay tra phai lon hon ngay nhan."; return; }
        DetectConflicts(); InfoMessage = $"Da keo dai luu tru cua {SelectedBar.GuestName} den {SelectedBar.DepartureDate:dd/MM}.";
    }

    [RelayCommand]
    private void ShortenStay(int days)
    {
        if (SelectedBar is null) return;
        SelectedBar.DepartureDate = SelectedBar.DepartureDate.AddDays(-days);
        if (SelectedBar.DepartureDate <= SelectedBar.ArrivalDate) { SelectedBar.DepartureDate = SelectedBar.ArrivalDate.AddDays(1); InfoMessage = "Ngay tra phai lon hon ngay nhan."; return; }
        DetectConflicts(); InfoMessage = $"Da rut ngan luu tru cua {SelectedBar.GuestName} den {SelectedBar.DepartureDate:dd/MM}.";
    }

    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedBar = null; }
    [RelayCommand] private void SetZoom(string days) { if (int.TryParse(days, out var d)) { ZoomDays = d; _ = LoadAsync(CancellationToken.None); } }
    [RelayCommand] private void NavigateDay(int offset) { StartDate = StartDate.AddDays(offset); _ = LoadAsync(CancellationToken.None); }
    [RelayCommand] private void NavigateToday() { StartDate = DateTime.Today.AddDays(-1); _ = LoadAsync(CancellationToken.None); }

    public void HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Escape: if (IsDetailOpen) CloseDetail(); else CancelDrag(); break;
            case Key.F5: RefreshCommand.Execute(null); break;
            case Key.Right: NavigateDayCommand.Execute(1); break;
            case Key.Left: NavigateDayCommand.Execute(-1); break;
        }
    }

    partial void OnSelectedBarChanged(TapeChartBar? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
    partial void OnConflictCountChanged(int v) => OnPropertyChanged(nameof(HasConflicts));
    partial void OnZoomDaysChanged(int v) { OnPropertyChanged(nameof(DateHeaders)); _ = LoadAsync(CancellationToken.None); }
}
