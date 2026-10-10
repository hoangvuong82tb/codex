using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class ArLedgerView : UserControl
{
    public ArLedgerView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArLedgerViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is ArKpi kpi
            && DataContext is ArLedgerViewModel vm)
            vm.ApplyKpiFilterCommand.Execute(kpi);
    }

    private void OnTreeItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is ArTreeItem item
            && DataContext is ArLedgerViewModel vm)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnItemCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is ArLedgerItemDto dto
            && DataContext is ArLedgerViewModel vm)
            vm.SelectItemCommand.Execute(dto);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ArLedgerViewModel vm && vm.SelectedItem is not null)
            vm.IsDetailOpen = true;
    }

    private void OnDetailTabClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is string tag
            && int.TryParse(tag, out var tab) && DataContext is ArLedgerViewModel vm)
            vm.SelectedDetailTab = tab;
    }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ArLedgerViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}