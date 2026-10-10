using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class InHouseStayCard : ObservableObject
{
    public Guid HotelStayId { get; init; }
    [ObservableProperty] private string _stayNumber = string.Empty;
    [ObservableProperty] private string _guestName = string.Empty;
    [ObservableProperty] private string _roomNumber = string.Empty;
    [ObservableProperty] private string _roomType = string.Empty;
    [ObservableProperty] private DateOnly _arrivalDate;
    [ObservableProperty] private DateOnly _departureDate;
    [ObservableProperty] private int _adults;
    [ObservableProperty] private int _children;
    [ObservableProperty] private HotelStayStatus _status;
    [ObservableProperty] private bool _isVip;
    [ObservableProperty] private bool _isDoNotDisturb;
    [ObservableProperty] private bool _hasPendingCharges;
    [ObservableProperty] private string _source = string.Empty;
    [ObservableProperty] private DateTime _createdAtUtc;
    [ObservableProperty] private int _nightsRemaining;
    [ObservableProperty] private string _statusColor = "#16A34A";
    [ObservableProperty] private byte[]? _rowVersion;

    public string GuestDisplay => IsVip ? $"★ {GuestName}" : GuestName;
    public string StayPeriodDisplay => $"{ArrivalDate:dd/MM} → {DepartureDate:dd/MM}";
    public string OccupancyDisplay => $"{Adults}A{(Children > 0 ? $" + {Children}C" : "")}";
    public string StatusDisplay => Status switch
    {
        HotelStayStatus.InHouse => "In House",
        HotelStayStatus.CheckedIn => "Checked In",
        HotelStayStatus.RoomMoved => "Room Moved",
        HotelStayStatus.Extended => "Extended",
        HotelStayStatus.ExpectedDeparture => "Due Out",
        _ => Status.ToString()
    };

    public static InHouseStayCard FromDto(HotelStayDto dto) => new()
    {
        HotelStayId = dto.HotelStayId, StayNumber = dto.StayNumber, GuestName = dto.GuestName,
        RoomNumber = dto.RoomNumber, RoomType = dto.RoomTypeCode, ArrivalDate = dto.ScheduledArrivalDate,
        DepartureDate = dto.ScheduledDepartureDate, Adults = dto.Adults, Children = dto.Children,
        Status = dto.Status, Source = dto.Source, CreatedAtUtc = dto.CreatedAtUtc,
        NightsRemaining = dto.ScheduledDepartureDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber,
        StatusColor = dto.Status switch
        {
            HotelStayStatus.ExpectedDeparture => "#EA580C",
            HotelStayStatus.InHouse => "#16A34A",
            HotelStayStatus.Extended => "#2563EB",
            _ => "#64748B"
        },
        RowVersion = dto.RowVersion
    };
}

public sealed partial class InHouseWorkspaceViewModel : ViewModelBase
{
    private readonly IReceptionUiService _service;

    [ObservableProperty] private ObservableCollection<InHouseStayCard> _stays = [];
    [ObservableProperty] private InHouseStayCard? _selectedStay;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _totalInHouse;
    [ObservableProperty] private int _totalDueOut;
    [ObservableProperty] private int _totalVip;
    [ObservableProperty] private int _totalDnd;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public InHouseWorkspaceViewModel(IReceptionUiService service)
    {
        _service = service;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            var stays = await _service.GetCurrentInHouseStaysAsync(50, CancellationToken.None);
            if (stays.IsSuccess && stays.Value is not null)
            {
                Stays = new ObservableCollection<InHouseStayCard>(stays.Value.Select(InHouseStayCard.FromDto));
                TotalInHouse = Stays.Count(s => s.Status is HotelStayStatus.CheckedIn
                    or HotelStayStatus.InHouse
                    or HotelStayStatus.RoomMoved
                    or HotelStayStatus.Extended);
                TotalDueOut = Stays.Count(s => s.Status == HotelStayStatus.ExpectedDeparture);
                TotalVip = Stays.Count(s => s.IsVip);
                TotalDnd = Stays.Count(s => s.IsDoNotDisturb);
                HasData = Stays.Count > 0;
            }
            else
            {
                ClearStayResults();
                ErrorMessage = stays.Error?.Message ?? "Không tải được danh sách khách đang lưu trú.";
            }
        }
        catch (Exception ex)
        {
            ClearStayResults();
            ErrorMessage = $"Không thể tải danh sách: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void ClearStayResults()
    {
        Stays.Clear();
        SelectedStay = null;
        TotalInHouse = 0;
        TotalDueOut = 0;
        TotalVip = 0;
        TotalDnd = 0;
        HasData = false;
    }

    partial void OnSelectedStayChanged(InHouseStayCard? value)
    {
        HasSelection = value is not null;
    }

    [RelayCommand]
    private async Task ToggleDoNotDisturbAsync(InHouseStayCard? stay)
    {
        if (stay is null) return;
        var result = await _service.ToggleDndAsync(stay.HotelStayId, !stay.IsDoNotDisturb, stay.RowVersion, CancellationToken.None);
        if (result.IsSuccess) { stay.IsDoNotDisturb = !stay.IsDoNotDisturb; InfoMessage = stay.IsDoNotDisturb ? "Đã bật DND" : "Đã tắt DND"; }
        else ErrorMessage = result.Error?.Message ?? "Không thể thay đổi DND.";
    }

    [RelayCommand]
    private void OpenCheckOut(InHouseStayCard? stay) { if (stay is not null) InfoMessage = $"Check-out: {stay.GuestName} - {stay.RoomNumber}"; }

    [RelayCommand]
    private void OpenFolio(InHouseStayCard? stay) { if (stay is not null) InfoMessage = $"Mở folio: {stay.GuestName} - {stay.StayNumber}"; }

    [RelayCommand]
    private void OpenRoomMove(InHouseStayCard? stay) { if (stay is not null) InfoMessage = $"Chuyển phòng: {stay.GuestName} - {stay.RoomNumber}"; }

    [RelayCommand]
    private async Task RequestWakeUpCallAsync(InHouseStayCard? stay)
    {
        if (stay is null) return;
        var result = await _service.RequestWakeUpCallAsync(stay.HotelStayId, DateTime.Now.AddDays(1).Date.AddHours(6), null, stay.RowVersion, CancellationToken.None);
        InfoMessage = result.IsSuccess ? $"Đã đặt báo thức cho {stay.GuestName}" : "Không thể đặt báo thức.";
    }

    public void Dispose() { }
}
