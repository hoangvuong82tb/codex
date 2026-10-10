using System.Windows.Controls;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class MinibarView : UserControl
{
    public MinibarView() => InitializeComponent();

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MinibarViewModel vm)
        {
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
            if (System.Windows.Window.GetWindow(this) is MinibarWindow window
                && window.InitialRoomId is Guid roomId)
                vm.SelectRoomById(roomId);
        }
    }

    private void OnKpiClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is MinibarKpi kpi && DataContext is MinibarViewModel vm)
            vm.ApplyKpiFilterCommand.Execute(kpi);
    }

    private void OnTreeItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is MinibarTreeItem item && DataContext is MinibarViewModel vm)
            vm.SelectTreeItemCommand.Execute(item);
    }

    private void OnRoomCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is MinibarRoomDto dto && DataContext is MinibarViewModel vm)
            vm.SelectRoomCommand.Execute(dto);
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MinibarViewModel vm && vm.SelectedRoom is not null)
            vm.IsDetailOpen = true;
    }

    private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not MinibarRoomDto dto || DataContext is not MinibarViewModel vm)
            return;

        vm.SelectRoomCommand.Execute(dto);
    }

    private void OnDetailTabClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement el && el.Tag is string tag && int.TryParse(tag, out var tab) && DataContext is MinibarViewModel vm)
            vm.SelectedDetailTab = tab;
    }

    private void OnDismissInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MinibarViewModel vm)
            vm.InfoMessage = string.Empty;
    }
}
