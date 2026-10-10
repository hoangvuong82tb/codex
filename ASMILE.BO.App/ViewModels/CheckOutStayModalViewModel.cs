using System.Collections.ObjectModel;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Stay;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;
public sealed partial class CheckOutStayModalViewModel(IReceptionUiService service) : ViewModelBase
{
    private Action? close;
    public ObservableCollection<HotelStayDto> Stays { get; } = new();
    [ObservableProperty] private HotelStayDto? selectedStay;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage); public bool HasSelection => SelectedStay is not null; public bool CanConfirm => HasSelection && !IsSaving; public bool CanReload => !IsSaving;
    public async Task InitializeAsync(Action closeAction, CancellationToken ct) { close = closeAction; await ReloadAsync(ct); }
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await ReloadAsync(ct);
    [RelayCommand(CanExecute = nameof(CanReload))] private async Task ReloadAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand(CanExecute = nameof(CanConfirm))] private async Task ConfirmAsync(CancellationToken ct)
    { if (SelectedStay is null || IsSaving) return; IsSaving = true; ErrorMessage = string.Empty; try { var result = await service.CheckOutAsync(SelectedStay.HotelStayId, SelectedStay.RowVersion ?? [], ct); if (result.IsFailure) { ErrorMessage = Map(result.Error?.Code); return; } InfoMessage = $"Khach da duoc tra phong thanh cong. {result.Value!.StayNumber} - Phong {result.Value.RoomNumber}"; await LoadAsync(ct); } finally { IsSaving = false; } }
    [RelayCommand] private void Close() => close?.Invoke();
    private async Task LoadAsync(CancellationToken ct) { IsLoading = true; ErrorMessage = string.Empty; try { var result = await service.GetInHouseForCheckOutAsync(SearchText, ct); if (result.IsFailure) { ErrorMessage = Map(result.Error?.Code); return; } Stays.Clear(); foreach (var stay in result.Value!) Stays.Add(stay); SelectedStay = Stays.FirstOrDefault(); } finally { IsLoading = false; } }
    partial void OnSelectedStayChanged(HotelStayDto? value) { OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(CanConfirm)); ConfirmCommand.NotifyCanExecuteChanged(); }
    partial void OnIsSavingChanged(bool value) { OnPropertyChanged(nameof(CanConfirm)); OnPropertyChanged(nameof(CanReload)); ConfirmCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged(); }
    private static string Map(string? code) => code switch { "STAY_NOT_IN_HOUSE" => "Ho so nay khong con o trang thai dang luu tru.", "STAY_CONCURRENCY_CONFLICT" => "Ho so vua duoc cap nhat boi nguoi dung khac. Vui long tai lai.", "STAY_PERMISSION_DENIED" => "Ban khong co quyen thuc hien tra phong.", "STAY_INVALID_CHECKOUT_TIME" => "Thoi gian tra phong khong hop le.", _ => "Khong tim thay ho so luu tru." };
}
