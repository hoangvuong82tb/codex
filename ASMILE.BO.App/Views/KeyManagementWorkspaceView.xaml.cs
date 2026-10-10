using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class KeyManagementWorkspaceView : UserControl
{
    public KeyManagementWorkspaceView() => InitializeComponent();
    private void OnIssue(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<IssueKeyCardWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new IssueKeyCardWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnReissue(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ReissueKeyCardWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new ReissueKeyCardWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnRevoke(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ReturnKeyCardWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new ReturnKeyCardWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnLock(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<DisableKeyCardWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new DisableKeyCardWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnLog(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<KeyCardAuditLogWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new KeyCardAuditLogWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
}
