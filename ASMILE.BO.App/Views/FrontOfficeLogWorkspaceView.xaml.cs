using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeLogWorkspaceView : UserControl
{
    public FrontOfficeLogWorkspaceView() => InitializeComponent();
    private void OnShiftLog(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ShiftLogWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        ShowChildWindow(new ShiftLogWindow(), this);
    }
    private void OnIncident(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<IncidentLogWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        ShowChildWindow(new IncidentLogWindow(), this);
    }
    private void OnLostFound(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<LostFoundWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        ShowChildWindow(new LostFoundWindow(), this);
    }
    private void OnVipNote(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<VipNotesWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        ShowChildWindow(new VipNotesWindow(), this);
    }
    private void OnHandover(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ShiftHandoverWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        ShowChildWindow(new ShiftHandoverWindow(), this);
    }

    private static void ShowChildWindow(Window window, DependencyObject source)
    {
        var owner = Window.GetWindow(source);
        if (owner is not { IsVisible: true })
        {
            owner = System.Windows.Application.Current?.MainWindow is { IsVisible: true } main
                ? main
                : null;
        }

        window.ResizeMode = ResizeMode.CanResize;
        window.ShowInTaskbar = false;
        if (owner is not null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.Show();
    }
}
