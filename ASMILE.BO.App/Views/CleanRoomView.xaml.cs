using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class CleanRoomView : UserControl
{
    public CleanRoomView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.CleanRoomViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnTreeItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is ViewModels.CleanRoomViewModel vm && e.NewValue is ViewModels.CleanTreeItem item)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnRoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.CleanRoomViewModel vm || sender is not DataGrid grid) return;
        if (grid.SelectedItem is ViewModels.CleanRoomDto room) vm.SelectRoomCommand.Execute(room);
        else vm.CloseDetailCommand.Execute(null);
    }

    private void OnRoomDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.CleanRoomViewModel vm && vm.SelectedRoom is not null)
            vm.SelectRoomCommand.Execute(vm.SelectedRoom);
        e.Handled = true;
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ViewModels.CleanRoomKpi kpi && DataContext is ViewModels.CleanRoomViewModel vm)
        { vm.ApplyKpiFilterCommand.Execute(kpi); e.Handled = true; }
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;
        var tabs = new[] { TabOverview, TabClean, TabInspect, TabGuest, TabHistory };
        var panels = new[] { DetailTab0, DetailTab1, DetailTab2, DetailTab3, DetailTab4 };
        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx)
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ED86E"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18B33F"));
                panels[i].Visibility = Visibility.Visible;
            }
            else
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                panels[i].Visibility = Visibility.Collapsed;
            }
        }
        if (DataContext is ViewModels.CleanRoomViewModel vm) vm.SelectedDetailTab = idx;
    }
}
