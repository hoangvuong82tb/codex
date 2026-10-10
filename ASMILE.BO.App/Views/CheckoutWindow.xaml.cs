using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CheckoutWindow : Window
{
    public CheckoutWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnPrepCheckout(object sender, MouseButtonEventArgs e) => Open<CheckoutPreparationWindow>(e);
    private void OnQuickCheckout(object sender, MouseButtonEventArgs e) => Open<QuickCheckoutWindow>(e);
    private void OnGroupCheckout(object sender, MouseButtonEventArgs e) => Open<GroupCheckoutWindow>(e);
    private void OnExpressCheckout(object sender, MouseButtonEventArgs e) => Open<ExpressCheckoutWindow>(e);
    private void OnCheckFolio(object sender, MouseButtonEventArgs e) => Open<FolioReviewWindow>(e);

    private void Open<T>(MouseButtonEventArgs e) where T : Window, new()
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new T { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
