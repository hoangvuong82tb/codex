using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class CleaningInProgressView : UserControl
{
    public CleaningInProgressView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.CleaningInProgressViewModel vm)
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
    }

    private void OnTreeItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is ViewModels.CleaningInProgressViewModel vm && e.NewValue is ViewModels.CleaningTreeItem item)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnRoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.CleaningInProgressViewModel vm || sender is not DataGrid grid) return;
        if (grid.SelectedItem is ViewModels.CleaningInProgressDto room) vm.SelectRoomCommand.Execute(room);
        else vm.CloseDetailCommand.Execute(null);
    }

    private void OnRoomDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // The selected room details are already rendered by the real bound detail panel.
        // Do not show a fabricated modal here; double-click should remain a no-op beyond selection.
        e.Handled = true;
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ViewModels.CleaningKpi kpi && DataContext is ViewModels.CleaningInProgressViewModel vm)
        { vm.ApplyKpiFilterCommand.Execute(kpi); e.Handled = true; }
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ViewModels.CleaningInProgressDto room && DataContext is ViewModels.CleaningInProgressViewModel vm)
        { vm.SelectRoomCommand.Execute(room); e.Handled = true; }
    }

    private void OnDetailTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var idx)) return;
        var tabs = new[] { TabOverview, TabCleaning, TabChecklist, TabTimeline, TabHistory };
        var panels = new[] { DetailTab0, DetailTab1, DetailTab2, DetailTab3, DetailTab4 };
        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == idx)
            {
                tabs[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                tabs[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                tabs[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
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
        if (DataContext is ViewModels.CleaningInProgressViewModel vm) vm.SelectedDetailTab = idx;
    }
}
