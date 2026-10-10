using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class KeyManagementWindow : Window
{
    public KeyManagementWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnIssueKey(object s, MouseButtonEventArgs e) => Open<IssueKeyCardWindow>(e);
    private void OnReissueKey(object s, MouseButtonEventArgs e) => Open<ReissueKeyCardWindow>(e);
    private void OnRevokeKey(object s, MouseButtonEventArgs e) => Open<ReturnKeyCardWindow>(e);
    private void OnLockKey(object s, MouseButtonEventArgs e) => Open<DisableKeyCardWindow>(e);
    private void OnKeyLog(object s, MouseButtonEventArgs e) => Open<KeyCardAuditLogWindow>(e);

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
