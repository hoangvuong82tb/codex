using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class BanquetRestaurantView : UserControl
{
    private FrameworkElement? _draggedRoomLayoutElement;
    private BanquetRoomLayoutItemVm? _draggedRoomLayoutItem;
    private Point _roomLayoutDragOffset;

    public BanquetRestaurantView()
    {
        InitializeComponent();
    }

    private void RoomLayoutItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not BanquetRoomLayoutItemVm item)
            return;
        _draggedRoomLayoutElement = element;
        _draggedRoomLayoutItem = item;
        var position = e.GetPosition(RoomMapCanvas);
        _roomLayoutDragOffset = new Point(position.X - item.X, position.Y - item.Y);
        if (DataContext is BanquetRestaurantViewModel viewModel)
        {
            viewModel.CaptureRoomLayoutSnapshot();
            viewModel.SelectedRoomLayoutItem = item;
        }
        RoomMapCanvas.Focus();
        element.CaptureMouse();
        e.Handled = true;
    }

    private void RoomLayoutItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedRoomLayoutElement is null || _draggedRoomLayoutItem is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        var position = e.GetPosition(RoomMapCanvas);
        var x = position.X - _roomLayoutDragOffset.X;
        var y = position.Y - _roomLayoutDragOffset.Y;
        if (DataContext is BanquetRestaurantViewModel viewModel)
        {
            x = viewModel.SnapRoomLayoutCoordinate(x);
            y = viewModel.SnapRoomLayoutCoordinate(y);
        }
        _draggedRoomLayoutItem.X = Math.Clamp(x, 0, 760 - _draggedRoomLayoutItem.Width);
        _draggedRoomLayoutItem.Y = Math.Clamp(y, 0, 520 - _draggedRoomLayoutItem.Height);
    }

    private void RoomLayoutItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _draggedRoomLayoutElement?.ReleaseMouseCapture();
        if (_draggedRoomLayoutItem is not null && DataContext is BanquetRestaurantViewModel viewModel)
            viewModel.CommitRoomLayoutChange();
        _draggedRoomLayoutElement = null;
        _draggedRoomLayoutItem = null;
        e.Handled = true;
    }

    private void RoomMapCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not BanquetRestaurantViewModel viewModel)
            return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.Key == Key.Z) viewModel.UndoRoomLayoutCommand.Execute(null);
            else if (e.Key == Key.Y) viewModel.RedoRoomLayoutCommand.Execute(null);
            else if (e.Key == Key.D) viewModel.DuplicateRoomLayoutItemCommand.Execute(null);
            else return;
            e.Handled = true;
            return;
        }
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        switch (e.Key)
        {
            case Key.Left: viewModel.NudgeSelectedRoomLayoutItem(-step, 0); break;
            case Key.Right: viewModel.NudgeSelectedRoomLayoutItem(step, 0); break;
            case Key.Up: viewModel.NudgeSelectedRoomLayoutItem(0, -step); break;
            case Key.Down: viewModel.NudgeSelectedRoomLayoutItem(0, step); break;
            case Key.Delete: viewModel.RemoveRoomLayoutItemCommand.Execute(viewModel.SelectedRoomLayoutItem); break;
            default: return;
        }
        e.Handled = true;
    }
}
