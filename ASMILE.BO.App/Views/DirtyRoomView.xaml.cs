using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class DirtyRoomView : UserControl
{
    public DirtyRoomView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DirtyRoomViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnRoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.DirtyRoomViewModel vm || sender is not DataGrid grid) return;
        if (grid.SelectedItem is ViewModels.DirtyRoomDto room) vm.SelectRoomCommand.Execute(room);
        else vm.CloseDetailCommand.Execute(null);
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ViewModels.DirtyDashboardKpi kpi && DataContext is ViewModels.DirtyRoomViewModel vm)
        { vm.ApplyKpiFilterCommand.Execute(kpi); e.Handled = true; }
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;
        var tabs = new[] { TabInfo, TabCK, TabPhoto, TabHistory };
        var panels = new[] { DetailTab0, DetailTab1, DetailTab2, DetailTab3 };
        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx) { tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7ED")); tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFAD39")); tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED7900")); panels[i].Visibility = Visibility.Visible; }
            else { tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")); tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")); tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")); panels[i].Visibility = Visibility.Collapsed; }
        }
        if (DataContext is ViewModels.DirtyRoomViewModel vm) vm.SelectedDetailTab = idx;
    }
}
