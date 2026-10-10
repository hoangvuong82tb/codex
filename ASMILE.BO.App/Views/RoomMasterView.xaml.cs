using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class RoomMasterView : UserControl
{
    public RoomMasterView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.RoomMasterViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnTreeItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is ViewModels.RoomMasterViewModel vm && e.NewValue is ViewModels.RoomTreeItem item)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnRoomCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.DataContext is not ViewModels.RoomMasterDto room) return;
        if (DataContext is ViewModels.RoomMasterViewModel vm)
            vm.SelectRoomCommand.Execute(room);
        e.Handled = true;
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ViewModels.RoomMasterKpi kpi &&
            DataContext is ViewModels.RoomMasterViewModel vm)
        {
            vm.ApplyKpiFilterCommand.Execute(kpi);
            e.Handled = true;
        }
    }

    private void OnRoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.RoomMasterViewModel vm || sender is not DataGrid grid) return;
        if (grid.SelectedItem is ViewModels.RoomMasterDto room)
            vm.SelectRoomCommand.Execute(room);
        else
            vm.CloseDetailCommand.Execute(null);
    }

    private void OnRoomDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // The live inspector panel is already opened by selection change and is
        // backed by RoomMasterViewModel. Do not open the old static profile dialog.
        if (DataContext is ViewModels.RoomMasterViewModel vm && vm.SelectedRoom is not null)
            vm.SelectRoomCommand.Execute(vm.SelectedRoom);

        e.Handled = true;
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;

        var tabs = new[] { TabOverview, TabGuest, TabHK, TabFacility, TabHistory, TabRemark };
        var panels = new[] { DetailTab0, DetailTab1, DetailTab2, DetailTab3, DetailTab4, DetailTab5 };

        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx)
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C9B8"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A58F"));
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

        if (DataContext is ViewModels.RoomMasterViewModel vm)
            vm.SelectedDetailTab = idx;
    }
}
