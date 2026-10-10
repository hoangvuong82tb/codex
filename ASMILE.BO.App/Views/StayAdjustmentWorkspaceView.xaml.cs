using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Serilog;

namespace ASMILE.BO.App.Views;

public partial class StayAdjustmentWorkspaceView : UserControl
{
    public StayAdjustmentWorkspaceView() => InitializeComponent();

    private void OnRoomMove(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RoomMoveOpenError.Visibility = Visibility.Collapsed;

        try
        {
            var existingWindow = System.Windows.Application.Current.Windows
                .OfType<RoomMoveWindow>()
                .FirstOrDefault(window => window.IsLoaded);

            if (existingWindow is not null)
            {
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                existingWindow.Focus();
                return;
            }

            var owner = GetVisibleOwner();
            var roomMoveWindow = new RoomMoveWindow(null)
            {
                WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };
            if (owner is not null)
            {
                roomMoveWindow.Owner = owner;
            }
            roomMoveWindow.Show();
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Room Move window open failed. ExceptionType={ExceptionType}; Message={Message}; StackTrace={StackTrace}; InnerException={InnerException}; Method={Method}",
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace,
                exception.InnerException?.ToString() ?? "(null)",
                nameof(OnRoomMove));

            RoomMoveOpenErrorText.Text = "Không thể mở màn hình Đổi phòng. Chi tiết kỹ thuật đã được ghi vào nhật ký.";
            RoomMoveOpenError.Visibility = Visibility.Visible;
        }
    }

    private void OnExtendStay(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        RoomMoveOpenError.Visibility = Visibility.Collapsed;
        try
        {
            var existing = System.Windows.Application.Current.Windows.OfType<ExtendStayWindow>().FirstOrDefault(w => w.IsLoaded);
            if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
            var owner = GetVisibleOwner();
            var window = new ExtendStayWindow(null)
            {
                WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };
            if (owner is not null)
            {
                window.Owner = owner;
            }
            window.Show();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Extend Stay window open failed. ExceptionType={ExceptionType}; Message={Message}; Method={Method}", exception.GetType().FullName, exception.Message, nameof(OnExtendStay));
            RoomMoveOpenErrorText.Text = "Không thể mở màn hình Gia hạn lưu trú. Chi tiết kỹ thuật đã được ghi vào nhật ký.";
            RoomMoveOpenError.Visibility = Visibility.Visible;
        }
    }
    private void OnMoveAndExtend(object s, RoutedEventArgs e)
    {
        e.Handled = true;
        RoomMoveOpenError.Visibility = Visibility.Collapsed;
        try
        {
            var existing = System.Windows.Application.Current.Windows.OfType<RoomMoveExtendWindow>().FirstOrDefault(w => w.IsLoaded);
            if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
            var owner = GetVisibleOwner();
            var window = new RoomMoveExtendWindow(null)
            {
                WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };
            if (owner is not null)
            {
                window.Owner = owner;
            }
            window.Show();
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Room Move Extend window open failed. ExceptionType={ExceptionType}; Message={Message}; StackTrace={StackTrace}; Inner={InnerException}; Method={Method}",
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace,
                exception.InnerException?.ToString() ?? "(null)",
                nameof(OnMoveAndExtend));
            RoomMoveOpenErrorText.Text = "Không thể mở màn hình Đổi phòng + Gia hạn. Chi tiết kỹ thuật đã được ghi vào nhật ký.";
            RoomMoveOpenError.Visibility = Visibility.Visible;
        }
    }
    private void OnModifyInfo(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        RoomMoveOpenError.Visibility = Visibility.Collapsed;
        try
        {
            var existing = System.Windows.Application.Current.Windows.OfType<StayInformationAdjustmentWindow>().FirstOrDefault(w => w.IsLoaded);
            if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
            var owner = GetVisibleOwner();
            var window = new StayInformationAdjustmentWindow
            {
                WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };
            if (owner is not null)
            {
                window.Owner = owner;
            }
            window.Show();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Stay information adjustment window open failed. ExceptionType={ExceptionType}; Message={Message}; Method={Method}", exception.GetType().FullName, exception.Message, nameof(OnModifyInfo));
            RoomMoveOpenErrorText.Text = "Không thể mở màn hình Điều chỉnh thông tin. Chi tiết kỹ thuật đã được ghi vào nhật ký.";
            RoomMoveOpenError.Visibility = Visibility.Visible;
        }
    }

    private Window? GetVisibleOwner()
    {
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current?.MainWindow;
        return owner is { IsVisible: true } ? owner : null;
    }
}
