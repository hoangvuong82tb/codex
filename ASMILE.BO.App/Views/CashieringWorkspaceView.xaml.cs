using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class CashieringWorkspaceView : UserControl
{
    public CashieringWorkspaceView() => InitializeComponent();
    private void OnCollect(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<CashierPaymentWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new CashierPaymentWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnDeposit(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<DepositManagementWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new DepositManagementWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnRefund(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<RefundManagementWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new RefundManagementWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnFolio(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<FolioWorkspaceWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new FolioWorkspaceWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnSplit(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<SplitFolioWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new SplitFolioWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnMerge(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<MergeFolioWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new MergeFolioWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnTransfer(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<TransferPostingWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new TransferPostingWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
    private void OnPrint(object s, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var existing = System.Windows.Application.Current.Windows.OfType<ProformaFolioWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        var owner = Window.GetWindow(this) ?? System.Windows.Application.Current.MainWindow;
        new ProformaFolioWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false }.Show();
    }
}
