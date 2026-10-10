using System.Linq;
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

    private void OnCollectPayment(object s, MouseButtonEventArgs e) => Open<CashierPaymentWindow>(e);
    private void OnDeposit(object s, MouseButtonEventArgs e) => Open<DepositManagementWindow>(e);
    private void OnRefund(object s, MouseButtonEventArgs e) => Open<RefundManagementWindow>(e);
    private void OnFolio(object s, MouseButtonEventArgs e) => Open<FolioWorkspaceWindow>(e);
    private void OnSplitFolio(object s, MouseButtonEventArgs e) => Open<SplitFolioWindow>(e);
    private void OnMergeFolio(object s, MouseButtonEventArgs e) => Open<MergeFolioWindow>(e);
    private void OnTransferItem(object s, MouseButtonEventArgs e) => Open<TransferPostingWindow>(e);
    private void OnPrintProforma(object s, MouseButtonEventArgs e) => Open<ProformaFolioWindow>(e);

    private void Open<T>(MouseButtonEventArgs e) where T : Window, new()
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new T { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }

    private void OnClose(object s, RoutedEventArgs e) => Close();
}
