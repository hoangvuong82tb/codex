using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Core.FrontOffice.Reservation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class CheckInWindow : Window
{
    private readonly IServiceScope? _serviceScope;
    private readonly ASMILE.BO.App.Services.IReceptionUiService? _receptionService;

    public CheckInWindow(ReservationWorkspaceViewModel workspace)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        DataContext = workspace;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        _receptionService = (_serviceScope?.ServiceProvider ?? services)
            ?.GetService<ASMILE.BO.App.Services.IReceptionUiService>();
        Loaded += async (_, _) => await workspace.RefreshAsync();
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e) => _serviceScope?.Dispose();

    private bool _checkInInProgress;

    private async void OnContinueClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as ReservationWorkspaceViewModel;
        var reservation = vm?.SelectedReservation;
        if (_checkInInProgress || vm is null || reservation is null) return;
        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Guaranteed))
        {
            vm.ErrorMessage = "Chỉ đặt phòng đã xác nhận hoặc bảo đảm mới có thể check-in.";
            return;
        }

        var svc = _receptionService;
        if (svc is null)
        {
            vm.ErrorMessage = "Không thể kết nối dịch vụ check-in. Vui lòng tải lại ứng dụng.";
            return;
        }

        var button = sender as Button;
        _checkInInProgress = true;
        vm.IsSaving = true;
        vm.ErrorMessage = string.Empty;
        vm.InfoMessage = string.Empty;
        if (button is not null) button.IsEnabled = false;
        try
        {
            var result = await svc.CheckInReservationAsync(reservation.ReservationId, reservation.RowVersion ?? [], System.Threading.CancellationToken.None);
            if (result.IsFailure)
            {
                vm.ErrorMessage = CheckInErrorMessage(result.Error?.Code, result.Error?.Message);
                return;
            }

            await vm.RefreshWithStatusAsync(ReservationStatus.CheckedIn, reservation.ReservationId);
            if (string.IsNullOrWhiteSpace(vm.ErrorMessage))
                vm.InfoMessage = $"Check-in hoàn tất cho {reservation.GuestName} — phòng {result.Value!.RoomNumber}. Trạng thái đặt phòng: Đã check-in.";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Không thể hoàn tất check-in: {ex.Message}";
        }
        finally
        {
            _checkInInProgress = false;
            vm.IsSaving = false;
            if (button is not null)
                button.IsEnabled = vm.SelectedReservation?.Status is ReservationStatus.Confirmed or ReservationStatus.Guaranteed;
        }
    }

    private static string CheckInErrorMessage(string? code, string? fallback) => code switch
    {
        "STAY_NO_ROOM_AVAILABLE" => "Không còn phòng trống đúng hạng cho thời gian lưu trú này. Hãy phân phòng hoặc đổi ngày trước khi check-in.",
        "STAY_INVALID_CHECKIN_TIME" => "Ngày đi của đặt phòng đã qua hoặc thời điểm check-in không hợp lệ.",
        "STAY_ROOM_NOT_ASSIGNABLE" or "STAY_ROOM_ALREADY_OCCUPIED" => "Phòng được gán không còn sẵn sàng để nhận khách.",
        "STAY_ROOM_RESERVATION_CONFLICT" => "Phòng được gán đang bị trùng với một đặt phòng khác.",
        "STAY_RESERVATION_NOT_ELIGIBLE" => "Đặt phòng không còn ở trạng thái được phép check-in.",
        "STAY_GUEST_REQUIRED" => "Đặt phòng chưa có tên khách. Hãy cập nhật hồ sơ khách trước khi check-in.",
        "STAY_CONCURRENCY_CONFLICT" => "Đặt phòng vừa được cập nhật ở nơi khác. Hãy làm mới danh sách rồi thử lại.",
        "STAY_CHECKIN_ALREADY_COMPLETED" => "Đặt phòng đã check-in nhưng không tìm thấy hồ sơ lưu trú đang hoạt động. Vui lòng kiểm tra nhật ký.",
        _ => string.IsNullOrWhiteSpace(fallback) ? "Không thể hoàn tất check-in. Vui lòng kiểm tra lại đặt phòng." : fallback
    };
}
