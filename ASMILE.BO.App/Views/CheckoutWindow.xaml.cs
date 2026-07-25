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

    private void OnPrepCheckout(object sender, MouseButtonEventArgs e) => Msg("Chuẩn bị Check-out");
    private void OnQuickCheckout(object sender, MouseButtonEventArgs e) => Msg("Check-out nhanh");
    private void OnGroupCheckout(object sender, MouseButtonEventArgs e) => Msg("Check-out đoàn");
    private void OnExpressCheckout(object sender, MouseButtonEventArgs e) => Msg("Express Check-out");
    private void OnCheckFolio(object sender, MouseButtonEventArgs e) => Msg("Kiểm tra Folio");
    private void Msg(string action) => MessageBox.Show($"Mockup: {action}. Chưa kết nối backend.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
