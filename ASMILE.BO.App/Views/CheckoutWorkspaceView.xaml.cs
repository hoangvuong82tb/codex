using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CheckoutWorkspaceView : UserControl
{
    public CheckoutWorkspaceView() => InitializeComponent();
    private void OnPrepCheckout(object s, MouseButtonEventArgs e) => M("Chuẩn bị Check-out");
    private void OnQuickCheckout(object s, MouseButtonEventArgs e) => M("Check-out nhanh");
    private void OnGroupCheckout(object s, MouseButtonEventArgs e) => M("Check-out đoàn");
    private void OnExpressCheckout(object s, MouseButtonEventArgs e) => M("Express Check-out");
    private void OnCheckFolio(object s, MouseButtonEventArgs e) => M("Kiểm tra Folio");
    private static void M(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Information);
}
