using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class DeniedReservationViewModel : ViewModelBase
{
    private readonly IReservationDenialService service;
    private readonly IFrontOfficeExecutionContextAccessor contextAccessor;

    public DeniedReservationViewModel(IReservationDenialService service, IFrontOfficeExecutionContextAccessor contextAccessor)
    {
        this.service = service;
        this.contextAccessor = contextAccessor;
    }

    public ObservableCollection<ReservationDenialDto> Items { get; } = new();
    public IReadOnlyList<ReservationDenialReason> Reasons { get; } = Enum.GetValues<ReservationDenialReason>();
    public IReadOnlyList<ReservationDenialStatus?> Statuses { get; } = new ReservationDenialStatus?[] { null, ReservationDenialStatus.Open, ReservationDenialStatus.Recovered, ReservationDenialStatus.Closed };

    [ObservableProperty] private ReservationDenialDto? selectedItem;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private ReservationDenialStatus? selectedStatus;
    [ObservableProperty] private string guestName = string.Empty;
    [ObservableProperty] private string guestPhone = string.Empty;
    [ObservableProperty] private DateTime arrivalDate = DateTime.Today;
    [ObservableProperty] private DateTime departureDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private int adultCount = 1;
    [ObservableProperty] private int childCount;
    [ObservableProperty] private string source = "Direct";
    [ObservableProperty] private ReservationDenialReason reason = ReservationDenialReason.NoAvailability;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasSelection => SelectedItem is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);

    partial void OnSelectedItemChanged(ReservationDenialDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        ErrorMessage = string.Empty;
        if (contextAccessor.Current is not { } context) { ErrorMessage = "Chưa chọn Property cho Front Office."; return; }
        IsLoading = true;
        try
        {
            Items.Clear();
            var rows = await service.GetAllAsync(context.Property.PropertyId.Value, NullIfEmpty(Keyword), SelectedStatus, ct);
            foreach (var row in rows) Items.Add(row);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải danh sách từ chối: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task CreateAsync(CancellationToken ct = default)
    {
        ErrorMessage = string.Empty;
        if (contextAccessor.Current is not { } context) { ErrorMessage = "Chưa chọn Property cho Front Office."; return; }
        if (string.IsNullOrWhiteSpace(GuestName)) { ErrorMessage = "Vui lòng nhập tên khách."; return; }
        if (DepartureDate.Date <= ArrivalDate.Date) { ErrorMessage = "Ngày đi phải sau ngày đến."; return; }
        if (AdultCount < 1 || ChildCount < 0) { ErrorMessage = "Số lượng khách không hợp lệ."; return; }
        IsLoading = true;
        try
        {
            await service.CreateAsync(context.Property.PropertyId.Value, GuestName, NullIfEmpty(GuestPhone), DateOnly.FromDateTime(ArrivalDate), DateOnly.FromDateTime(DepartureDate), AdultCount, ChildCount, null, string.IsNullOrWhiteSpace(Source) ? "Direct" : Source.Trim(), Reason, NullIfEmpty(Notes), ct);
            GuestName = string.Empty; GuestPhone = string.Empty; Notes = string.Empty;
            InfoMessage = "Đã ghi nhận yêu cầu từ chối vào dữ liệu thật.";
            await RefreshAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể ghi nhận yêu cầu: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task CloseSelectedAsync(CancellationToken ct = default)
    {
        if (SelectedItem is null || contextAccessor.Current is not { } context) return;
        ErrorMessage = string.Empty; IsLoading = true;
        try
        {
            await service.CloseAsync(context.Property.PropertyId.Value, SelectedItem.DenialId, ct);
            InfoMessage = "Đã đóng yêu cầu từ chối."; SelectedItem = null;
            await RefreshAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể đóng yêu cầu: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
