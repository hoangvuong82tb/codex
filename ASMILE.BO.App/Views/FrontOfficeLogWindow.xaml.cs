using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeLogWindow : Window
{
    public FrontOfficeLogWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnShiftLog(object s, MouseButtonEventArgs e) => Open<ShiftLogWindow>(e);
    private void OnIncidentLog(object s, MouseButtonEventArgs e) => Open<IncidentLogWindow>(e);
    private void OnLostFound(object s, MouseButtonEventArgs e) => Open<LostFoundWindow>(e);
    private void OnVipNote(object s, MouseButtonEventArgs e) => Open<VipNotesWindow>(e);
    private void OnShiftHandover(object s, MouseButtonEventArgs e) => Open<ShiftHandoverWindow>(e);

    private void Open<T>(MouseButtonEventArgs e) where T : Window, new()
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var w = new T { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false };
        w.Show();
    }

    private void OnClose(object s, RoutedEventArgs e) => Close();
}
