using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class StayAdjustmentWindow : Window
{
    public StayAdjustmentWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnRoomMove(object sender, MouseButtonEventArgs e) => Open<RoomMoveWindow>(e);
    private void OnExtendStay(object sender, MouseButtonEventArgs e) => Open<ExtendStayWindow>(e);
    private void OnMoveAndExtend(object sender, MouseButtonEventArgs e) => Open<RoomMoveExtendWindow>(e);
    private void OnModifyInfo(object sender, MouseButtonEventArgs e) => Open<StayInformationAdjustmentWindow>(e);

    private void Open<T>(MouseButtonEventArgs e) where T : Window, new()
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new T { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
