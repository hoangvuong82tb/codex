using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingRoomStatusView : UserControl
{
    public HousekeepingRoomStatusView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.HousekeepingRoomStatusViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnRoomCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not ViewModels.RoomStatusDto room) return;
        if (DataContext is ViewModels.HousekeepingRoomStatusViewModel vm)
            vm.SelectRoomCommand.Execute(room);
        e.Handled = true;
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ViewModels.RoomStatusKpi kpi &&
            DataContext is ViewModels.HousekeepingRoomStatusViewModel vm)
        {
            vm.ApplyKpiFilterCommand.Execute(kpi);
            e.Handled = true;
        }
    }

    private void OnMainAreaClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ViewModels.HousekeepingRoomStatusViewModel vm || !vm.IsDetailOpen)
            return;

        var source = e.OriginalSource as DependencyObject;
        if (source is not null &&
            (ReferenceEquals(source, RightDetailPanel) || RightDetailPanel.IsAncestorOf(source) || FindAncestor<DataGrid>(source) is not null ||
             FindAncestor<Button>(source) is not null || FindAncestor<TextBox>(source) is not null ||
             FindAncestor<ComboBox>(source) is not null || FindAncestor<CheckBox>(source) is not null))
            return;

        vm.CloseDetailCommand.Execute(null);
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match) return match;
            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void OnRoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.HousekeepingRoomStatusViewModel vm || sender is not DataGrid grid) return;
        if (grid.SelectedItem is ViewModels.RoomStatusDto room)
            vm.SelectRoomCommand.Execute(room);
        else
            vm.CloseDetailCommand.Execute(null);
    }

    private void OnRoomDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // The selected room status is already shown in the real detail panel.
        e.Handled = true;
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;

        var tabs = new[] { TabRoom, TabGuest, TabHK, TabAmenity, TabTimeline };
        var panels = new[] { DetailTab0, DetailTab1, DetailTab2, DetailTab3, DetailTab4 };

        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx)
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A8EFF"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF"));
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

        if (DataContext is ViewModels.HousekeepingRoomStatusViewModel vm)
            vm.SelectedDetailTab = idx;
    }

    private void OnDismissInfo(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.HousekeepingRoomStatusViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}
