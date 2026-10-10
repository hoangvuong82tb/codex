using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CheckoutWorkspaceView : UserControl
{
    public CheckoutWorkspaceView() => InitializeComponent();

    private void OnPrepCheckout(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<CheckoutPreparationWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new CheckoutPreparationWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnQuickCheckout(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<QuickCheckoutWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new QuickCheckoutWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnGroupCheckout(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<GroupCheckoutWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new GroupCheckoutWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnExpressCheckout(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ExpressCheckoutWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new ExpressCheckoutWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnCheckFolio(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<FolioReviewWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new FolioReviewWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
}
