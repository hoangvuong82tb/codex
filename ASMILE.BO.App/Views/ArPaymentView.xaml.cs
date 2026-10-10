using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ArPaymentView : UserControl
{
    public ArPaymentView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is ArPaymentViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is ArPaymentKpi kpi && DataContext is ArPaymentViewModel vm)
        {
            if (kpi.FilterKey is not null && kpi.FilterValue is not null)
            {
                switch (kpi.FilterKey)
                {
                    case "Status": vm.FilterStatus = kpi.FilterValue; break;
                    case "Method": vm.FilterMethod = kpi.FilterValue; break;
                }
            }
        }
    }

    private void OnTreeItemClick(object sender, MouseButtonEventArgs e)
    { if (sender is System.Windows.FrameworkElement el && el.Tag is ArPaymentTreeItem item && DataContext is ArPaymentViewModel vm) vm.SelectTreeItemCommand.Execute(item); }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    { if (DataContext is ArPaymentViewModel vm && vm.SelectedItem is not null) vm.IsDetailOpen = true; }

    private void OnDetailTabClick(object sender, System.Windows.RoutedEventArgs e)
    { if (sender is System.Windows.FrameworkElement el && el.Tag is string tag && int.TryParse(tag, out var tab) && DataContext is ArPaymentViewModel vm) vm.SelectedDetailTab = tab; }

    private void OnDashboardTabClick(object sender, System.Windows.RoutedEventArgs e)
    { if (sender is System.Windows.FrameworkElement el && el.Tag is string tag && int.TryParse(tag, out var tab) && DataContext is ArPaymentViewModel vm) vm.SetDashboardTabCommand.Execute(tab); }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is ArPaymentViewModel vm) vm.InfoMessage = string.Empty; }
}