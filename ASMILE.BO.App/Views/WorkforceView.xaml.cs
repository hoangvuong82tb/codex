using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class WorkforceView : UserControl
{
    public WorkforceView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is WorkforceViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is WorkforceKpi kpi && DataContext is WorkforceViewModel vm)
            vm.ApplyKpiFilterCommand.Execute(kpi);
    }

    private void OnTreeItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is WorkforceTreeItem item && DataContext is WorkforceViewModel vm)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnStaffCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is WorkforceStaffDto dto && DataContext is WorkforceViewModel vm)
            vm.SelectStaffCommand.Execute(dto);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is WorkforceViewModel vm && vm.SelectedStaff is not null)
            vm.IsDetailOpen = true;
    }

    private void OnDetailTabClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is string tag && int.TryParse(tag, out var tab) && DataContext is WorkforceViewModel vm)
            vm.SelectedDetailTab = tab;
    }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is WorkforceViewModel vm) vm.InfoMessage = string.Empty;
    }
}