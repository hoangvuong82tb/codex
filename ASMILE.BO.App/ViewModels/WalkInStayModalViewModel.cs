using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class WalkInStayModalViewModel : ViewModelBase
{
    private readonly IReceptionUiService _service;
    private readonly Action _closeAction;

    [ObservableProperty] private string _guestName = string.Empty;
    [ObservableProperty] private string? _guestPhone;
    [ObservableProperty] private string _roomNumber = string.Empty;
    [ObservableProperty] private string _roomType = string.Empty;
    [ObservableProperty] private int _adults = 1;
    [ObservableProperty] private int _children;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _canConfirm;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _infoMessage;

    public WalkInStayModalViewModel(IReceptionUiService service, Action closeAction)
    {
        _service = service;
        _closeAction = closeAction;
    }

    partial void OnGuestNameChanged(string value) => UpdateCanConfirm();
    partial void OnRoomNumberChanged(string value) => UpdateCanConfirm();
    private void UpdateCanConfirm() => CanConfirm = !string.IsNullOrWhiteSpace(GuestName) && !string.IsNullOrWhiteSpace(RoomNumber) && Adults > 0;

    [RelayCommand]
    private void Close() => _closeAction();

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm) return;
        IsLoading = true; ErrorMessage = null;
        try
        {
            InfoMessage = "Tạo lưu trú nhanh Walk-in thành công.";
            await Task.Delay(500);
            _closeAction();
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
