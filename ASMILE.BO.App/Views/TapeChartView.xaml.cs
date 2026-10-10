using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class TapeChartView : UserControl
{
    private Point _dragStart;

    public TapeChartView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is ViewModels.TapeChartViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }

    private void OnKeyDown(object sender, KeyEventArgs e)
    { if (DataContext is ViewModels.TapeChartViewModel vm) vm.HandleKey(e.Key); }

    // Drag source: room row
    private void OnRoomMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item || item.DataContext is not ViewModels.TapeChartRoomDto room) return;
        if (DataContext is not ViewModels.TapeChartViewModel vm) return;
        _dragStart = e.GetPosition(null);
        vm.StartDragCommand.Execute(room);
        DragDrop.DoDragDrop(item, room, DragDropEffects.Move);
        vm.CancelDragCommand.Execute(null);
    }

    // Drop target: room row
    private void OnRoomDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListBoxItem item || item.DataContext is not ViewModels.TapeChartRoomDto target) return;
        if (DataContext is not ViewModels.TapeChartViewModel vm) return;
        vm.DropOnRoomCommand.Execute(target);
    }

    // Bar click → select
    private void OnBarClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not ViewModels.TapeChartBar bar) return;
        if (DataContext is ViewModels.TapeChartViewModel vm) vm.SelectBarCommand.Execute(bar);
    }
}
