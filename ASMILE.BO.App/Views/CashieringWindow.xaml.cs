using System.Windows;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CashieringWindow : Window
{
    public CashieringWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void OnCollectPayment(object s, MouseButtonEventArgs e) => Msg("Thu tiền");
    private void OnDeposit(object s, MouseButtonEventArgs e) => Msg("Đặt cọc");
    private void OnRefund(object s, MouseButtonEventArgs e) => Msg("Hoàn tiền");
    private void OnFolio(object s, MouseButtonEventArgs e) => Msg("Folio");
    private void OnSplitFolio(object s, MouseButtonEventArgs e) => Msg("Tách Folio");
    private void OnMergeFolio(object s, MouseButtonEventArgs e) => Msg("Gộp Folio");
    private void OnTransferItem(object s, MouseButtonEventArgs e) => Msg("Chuyển khoản mục");
    private void OnPrintProforma(object s, MouseButtonEventArgs e) => Msg("In tạm tính");
    private void Msg(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Thu ngân / Folio", MessageBoxButton.OK, MessageBoxImage.Information);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}
