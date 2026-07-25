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

            var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
            var roomMoveWindow = new RoomMoveWindow
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false
            };
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

    private void OnExtendStay(object s, MouseButtonEventArgs e) => new ExtendStayWindow().Show();
    private void OnMoveAndExtend(object s, MouseButtonEventArgs e) => new RoomMoveExtendWindow().Show();
    private void OnModifyInfo(object s, MouseButtonEventArgs e) => new StayInformationAdjustmentWindow().Show();
}
