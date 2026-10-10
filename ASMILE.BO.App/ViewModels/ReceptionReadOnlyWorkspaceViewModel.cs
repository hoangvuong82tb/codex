using System.Collections.ObjectModel;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Large modal workspace for data supplied by existing, read-only Front Office queries.</summary>
public sealed partial class ReceptionReadOnlyWorkspaceViewModel : ViewModelBase
{
    private readonly IReceptionUiService receptionUiService;
    private Action? close;

    public ReceptionReadOnlyWorkspaceViewModel(IReceptionUiService receptionUiService) => this.receptionUiService = receptionUiService;

    public ObservableCollection<ReceptionReadOnlyRow> Rows { get; } = new();
    [ObservableProperty] private ReceptionWorkspaceKind kind;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string subtitle = string.Empty;
    [ObservableProperty] private string iconGlyph = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasRows => Rows.Count > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasRows;
    public string EmptyMessage => Kind switch
    {
        ReceptionWorkspaceKind.LockedRooms => "Không có phòng đang ngừng khai thác hoặc tạm dừng.",
        ReceptionWorkspaceKind.ArrivalsToday => "Không có khách dự kiến đến trong ngày hôm nay.",
        ReceptionWorkspaceKind.StayProfiles => "Không có hồ sơ lưu trú trong phạm vi chi nhánh hiện tại.",
        ReceptionWorkspaceKind.CurrentInHouse => "Chưa có khách đang lưu trú trong phạm vi chi nhánh hiện tại.",
        _ => "Không có khách dự kiến đi trong ngày hôm nay."
    };

    public async Task InitializeAsync(ReceptionWorkspaceKind workspaceKind, Action closeAction, CancellationToken cancellationToken)
    {
        Kind = workspaceKind;
        close = closeAction;
        (Title, Subtitle, IconGlyph) = workspaceKind switch
        {
            ReceptionWorkspaceKind.LockedRooms => ("Thông tin phòng khóa", "Tra cứu phòng ngừng khai thác hoặc tạm dừng từ dữ liệu phòng hiện có.", "\uE72E"),
            ReceptionWorkspaceKind.ArrivalsToday => ("Khách đến hôm nay", "Danh sách đặt phòng có ngày đến trùng ngày nghiệp vụ hiện tại.", "\uE8D4"),
            ReceptionWorkspaceKind.StayProfiles => ("Hồ sơ lưu trú", "Danh sách Stay theo chi nhánh hiện tại; chỉ đọc.", "\uE77B"),
            ReceptionWorkspaceKind.CurrentInHouse => ("Khách hiện tại", "Danh sách Stay InHouse theo chi nhánh hiện tại; chỉ đọc.", "\uE77B"),
            _ => ("Khách đi hôm nay", "Danh sách đặt phòng có ngày đi trùng ngày nghiệp vụ hiện tại.", "\uE8D3")
        };
        OnPropertyChanged(nameof(EmptyMessage));
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    [RelayCommand]
    private void Close() => close?.Invoke();

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = Kind switch
            {
                ReceptionWorkspaceKind.LockedRooms => await receptionUiService.GetLockedRoomsAsync(cancellationToken),
                ReceptionWorkspaceKind.ArrivalsToday => await receptionUiService.GetArrivalsTodayAsync(cancellationToken),
                ReceptionWorkspaceKind.StayProfiles => await receptionUiService.GetStayProfilesAsync(cancellationToken),
                ReceptionWorkspaceKind.CurrentInHouse => await receptionUiService.GetCurrentInHouseAsync(cancellationToken),
                _ => await receptionUiService.GetDeparturesTodayAsync(cancellationToken)
            };
            if (result.IsFailure)
            {
                ErrorMessage = result.Error?.Message ?? "Không thể tải dữ liệu tra cứu lễ tân.";
                return;
            }

            Rows.Clear();
            foreach (var row in result.Value!) Rows.Add(row);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            ErrorMessage = "Không thể tải dữ liệu tra cứu lễ tân. Vui lòng thử lại hoặc kiểm tra nhật ký hệ thống.";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
}
