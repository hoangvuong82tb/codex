using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CashieringWorkspaceView : UserControl
{
    public CashieringWorkspaceView() => InitializeComponent();
    private void OnCollect(object s, MouseButtonEventArgs e) => M("Thu tiền");
    private void OnDeposit(object s, MouseButtonEventArgs e) => M("Đặt cọc");
    private void OnRefund(object s, MouseButtonEventArgs e) => M("Hoàn tiền");
    private void OnFolio(object s, MouseButtonEventArgs e) => M("Folio");
    private void OnSplit(object s, MouseButtonEventArgs e) => M("Tách Folio");
    private void OnMerge(object s, MouseButtonEventArgs e) => M("Gộp Folio");
    private void OnTransfer(object s, MouseButtonEventArgs e) => M("Chuyển khoản mục");
    private void OnPrint(object s, MouseButtonEventArgs e) => M("In tạm tính");
    private static void M(string a) => MessageBox.Show($"Mockup: {a}. Chưa kết nối backend.", "Thu ngân / Folio", MessageBoxButton.OK, MessageBoxImage.Information);
}
