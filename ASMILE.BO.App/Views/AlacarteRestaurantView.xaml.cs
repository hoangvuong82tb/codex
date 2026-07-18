using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.App.Views;

public partial class AlacarteRestaurantView : UserControl
{
    private const double WheelScrollFactor = 1.15d;
    private bool _isDraggingRoomSetupDialog;
    private Point _roomSetupDialogDragStart;
    private double _roomSetupDialogLeftStart;
    private double _roomSetupDialogTopStart;
    private bool _isDraggingOrderItemPickerDialog;
    private Point _orderItemPickerDialogDragStart;
    private double _orderItemPickerDialogLeftStart;
    private double _orderItemPickerDialogTopStart;
    private bool _isDraggingServingPlanDialog;
    private Point _servingPlanDialogDragStart;
    private double _servingPlanDialogLeftStart;
    private double _servingPlanDialogTopStart;
    private const double DialogEdgeMargin = 16d;

    public AlacarteRestaurantView()
    {
        InitializeComponent();
    }

    private void AlacarteRestaurantView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureDialogCanvasPositions();
    }

    private void RoomCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is DiningRoomDto room && DataContext is AlacarteRestaurantViewModel vm)
        {
            vm.OpenRoomInOrdersCommand.Execute(room);
        }
    }

    private void SettingRoomGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid
            || DataContext is not AlacarteRestaurantViewModel vm)
        {
            return;
        }

        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.DataContext is not AlacarteSettingArea settingRoom)
        {
            return;
        }

        var room = vm.Rooms.FirstOrDefault(x => x.Id == settingRoom.Id)
                   ?? vm.Rooms.FirstOrDefault(x => string.Equals(x.Code, settingRoom.Code, StringComparison.OrdinalIgnoreCase))
                   ?? vm.Rooms.FirstOrDefault(x => string.Equals(x.Name, settingRoom.Name, StringComparison.OrdinalIgnoreCase));
        if (room is null)
        {
            return;
        }

        vm.OpenRoomInOrdersCommand.Execute(room);
        e.Handled = true;
    }

    private void SummaryTableCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AlacarteRestaurantViewModel vm)
        {
            return;
        }

        if (vm.SelectedOrderSummaryTable is not null)
        {
            vm.SelectOpenOrderTableCommand.Execute(vm.SelectedOrderSummaryTable);
        }

        if (e.ClickCount == 2)
        {
            vm.OpenSummaryTableSetupCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void RoomTableCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button
            || button.DataContext is not DiningTableDto table
            || DataContext is not AlacarteRestaurantViewModel vm)
        {
            return;
        }

        vm.OpenRoomTableSetupCommand.Execute(table);
        e.Handled = true;
    }

    private void RoomSetupDialogHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingRoomSetupDialog = true;
        _roomSetupDialogDragStart = e.GetPosition(RoomSetupCanvas);
        _roomSetupDialogLeftStart = double.IsNaN(Canvas.GetLeft(RoomSetupDialogCard)) ? 0 : Canvas.GetLeft(RoomSetupDialogCard);
        _roomSetupDialogTopStart = double.IsNaN(Canvas.GetTop(RoomSetupDialogCard)) ? 0 : Canvas.GetTop(RoomSetupDialogCard);
        Mouse.Capture(RoomSetupCanvas);
        e.Handled = true;
    }

    private void OrderItemPickerDialogHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        _isDraggingOrderItemPickerDialog = true;
        _orderItemPickerDialogDragStart = e.GetPosition(OrderItemPickerCanvasRoot);
        _orderItemPickerDialogLeftStart = GetCanvasLeft(OrderItemPickerDialogCard);
        _orderItemPickerDialogTopStart = GetCanvasTop(OrderItemPickerDialogCard);
        Mouse.Capture(OrderItemPickerCanvasRoot);
        e.Handled = true;
    }

    private void ServingPlanDialogHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        _isDraggingServingPlanDialog = true;
        _servingPlanDialogDragStart = e.GetPosition(ServingPlanCanvasRoot);
        _servingPlanDialogLeftStart = GetCanvasLeft(ServingPlanDialogCard);
        _servingPlanDialogTopStart = GetCanvasTop(ServingPlanDialogCard);
        Mouse.Capture(ServingPlanCanvasRoot);
        e.Handled = true;
    }

    private void RoomSetupCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingRoomSetupDialog)
        {
            return;
        }

        var currentPoint = e.GetPosition(RoomSetupCanvas);
        var delta = currentPoint - _roomSetupDialogDragStart;
        var nextLeft = Math.Max(16, _roomSetupDialogLeftStart + delta.X);
        var nextTop = Math.Max(16, _roomSetupDialogTopStart + delta.Y);
        var availableWidth = Math.Max(RoomSetupCanvas.ActualWidth, ActualWidth);
        var availableHeight = Math.Max(RoomSetupCanvas.ActualHeight, ActualHeight);
        var maxLeft = Math.Max(16, availableWidth - RoomSetupDialogCard.ActualWidth - 16);
        var maxTop = Math.Max(16, availableHeight - RoomSetupDialogCard.ActualHeight - 16);
        Canvas.SetLeft(RoomSetupDialogCard, Math.Min(nextLeft, maxLeft));
        Canvas.SetTop(RoomSetupDialogCard, Math.Min(nextTop, maxTop));
    }

    private void RoomSetupCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingRoomSetupDialog)
        {
            return;
        }

        EndRoomSetupDialogDrag();
        e.Handled = true;
    }

    private void OrderItemPickerCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingOrderItemPickerDialog)
        {
            return;
        }

        if (OrderItemPickerCanvasRoot is null)
        {
            return;
        }

        var currentPoint = e.GetPosition(OrderItemPickerCanvasRoot);
        var delta = currentPoint - _orderItemPickerDialogDragStart;
        var nextLeft = _orderItemPickerDialogLeftStart + delta.X;
        var nextTop = _orderItemPickerDialogTopStart + delta.Y;
        SetOrderItemPickerOffsets(nextLeft, nextTop);
    }

    private void ServingPlanCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingServingPlanDialog)
        {
            return;
        }

        if (ServingPlanCanvasRoot is null)
        {
            return;
        }

        var currentPoint = e.GetPosition(ServingPlanCanvasRoot);
        var delta = currentPoint - _servingPlanDialogDragStart;
        var nextLeft = _servingPlanDialogLeftStart + delta.X;
        var nextTop = _servingPlanDialogTopStart + delta.Y;
        SetServingPlanOffsets(nextLeft, nextTop);
    }

    private void OrderItemPickerCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingOrderItemPickerDialog)
        {
            return;
        }

        EndOrderItemPickerDialogDrag();
        e.Handled = true;
    }

    private void ServingPlanCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingServingPlanDialog)
        {
            return;
        }

        EndServingPlanDialogDrag();
        e.Handled = true;
    }

    private void RoomSetupCanvas_LostMouseCapture(object sender, MouseEventArgs e)
    {
        EndRoomSetupDialogDrag();
    }

    private void OrderItemPickerCanvas_LostMouseCapture(object sender, MouseEventArgs e)
    {
        EndOrderItemPickerDialogDrag();
    }

    private void ServingPlanCanvas_LostMouseCapture(object sender, MouseEventArgs e)
    {
        EndServingPlanDialogDrag();
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            ScrollByWheel(sv, e);
            e.Handled = true;
        }
    }

    private void ItemsControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var parent = ((System.Windows.FrameworkElement)sender).Parent;
        while (parent is not null && parent is not ScrollViewer)
            parent = ((System.Windows.FrameworkElement)parent).Parent;
        if (parent is ScrollViewer sv)
        {
            ScrollByWheel(sv, e);
            e.Handled = true;
        }
    }

    private void QuoteArea_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        var scrollViewer = FindAncestor<ScrollViewer>(source);

        if (scrollViewer is null || !IsQuoteScrollViewer(scrollViewer) || !CanScroll(scrollViewer))
        {
            scrollViewer = ResolveQuoteScrollViewer(e);
        }

        if (scrollViewer is null)
        {
            return;
        }

        ScrollByWheel(scrollViewer, e);
        e.Handled = true;
    }

    private ScrollViewer? ResolveQuoteScrollViewer(MouseWheelEventArgs e)
    {
        if (IsMouseOverElement(e, QuotePreviewScrollViewer) && CanScroll(QuotePreviewScrollViewer))
        {
            return QuotePreviewScrollViewer;
        }

        if (QuoteFoodScrollViewer.IsVisible && CanScroll(QuoteFoodScrollViewer))
        {
            return QuoteFoodScrollViewer;
        }

        if (QuoteServiceScrollViewer.IsVisible && CanScroll(QuoteServiceScrollViewer))
        {
            return QuoteServiceScrollViewer;
        }

        if (QuotePackageScrollViewer.IsVisible && CanScroll(QuotePackageScrollViewer))
        {
            return QuotePackageScrollViewer;
        }

        return CanScroll(QuotePreviewScrollViewer) ? QuotePreviewScrollViewer : null;
    }

    private static bool IsMouseOverElement(MouseEventArgs e, FrameworkElement element)
    {
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var point = e.GetPosition(element);
        return point.X >= 0
               && point.Y >= 0
               && point.X <= element.ActualWidth
               && point.Y <= element.ActualHeight;
    }

    private static bool CanScroll(ScrollViewer? scrollViewer)
    {
        return scrollViewer is not null && scrollViewer.ScrollableHeight > 0;
    }

    private static void ScrollByWheel(ScrollViewer scrollViewer, MouseWheelEventArgs e)
    {
        var targetOffset = scrollViewer.VerticalOffset - (e.Delta / WheelScrollFactor);
        scrollViewer.ScrollToVerticalOffset(Math.Clamp(targetOffset, 0, scrollViewer.ScrollableHeight));
    }

    private bool IsQuoteScrollViewer(ScrollViewer scrollViewer)
    {
        return ReferenceEquals(scrollViewer, QuoteFoodScrollViewer)
               || ReferenceEquals(scrollViewer, QuoteServiceScrollViewer)
               || ReferenceEquals(scrollViewer, QuotePackageScrollViewer)
               || ReferenceEquals(scrollViewer, QuotePreviewScrollViewer);
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private void EndRoomSetupDialogDrag()
    {
        _isDraggingRoomSetupDialog = false;
        if (Mouse.Captured == RoomSetupCanvas)
        {
            Mouse.Capture(null);
        }
    }

    private void EndOrderItemPickerDialogDrag()
    {
        _isDraggingOrderItemPickerDialog = false;
        if (Mouse.Captured == OrderItemPickerCanvasRoot)
        {
            Mouse.Capture(null);
        }
    }

    private void EndServingPlanDialogDrag()
    {
        _isDraggingServingPlanDialog = false;
        if (Mouse.Captured == ServingPlanCanvasRoot)
        {
            Mouse.Capture(null);
        }
    }

    private void OrderItemPickerResizeTopLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeOrderItemPickerFromCorner(e.HorizontalChange, e.VerticalChange, resizeFromLeft: true, resizeFromTop: true);
    }

    private void OrderItemPickerResizeTopRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeOrderItemPickerFromCorner(e.HorizontalChange, e.VerticalChange, resizeFromLeft: false, resizeFromTop: true);
    }

    private void OrderItemPickerResizeBottomLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeOrderItemPickerFromCorner(e.HorizontalChange, e.VerticalChange, resizeFromLeft: true, resizeFromTop: false);
    }

    private void OrderItemPickerResizeBottomRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeOrderItemPickerFromCorner(e.HorizontalChange, e.VerticalChange, resizeFromLeft: false, resizeFromTop: false);
    }

    private void ServingPlanResizeRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeServingPlan(e.HorizontalChange, 0);
    }

    private void ServingPlanResizeBottomThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeServingPlan(0, e.VerticalChange);
    }

    private void ServingPlanResizeCornerThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeServingPlan(e.HorizontalChange, e.VerticalChange);
    }

    private void ResizeOrderItemPickerFromCorner(double horizontalChange, double verticalChange, bool resizeFromLeft, bool resizeFromTop)
    {
        if (OrderItemPickerCanvasRoot is null)
        {
            return;
        }

        var availableWidth = Math.Max(OrderItemPickerCanvasRoot.ActualWidth, ActualWidth);
        var availableHeight = Math.Max(OrderItemPickerCanvasRoot.ActualHeight, ActualHeight);
        var minWidth = OrderItemPickerDialogCard.MinWidth;
        var minHeight = OrderItemPickerDialogCard.MinHeight;
        var left = GetCanvasLeft(OrderItemPickerDialogCard);
        var top = GetCanvasTop(OrderItemPickerDialogCard);
        var width = double.IsNaN(OrderItemPickerDialogCard.Width) ? OrderItemPickerDialogCard.ActualWidth : OrderItemPickerDialogCard.Width;
        var height = double.IsNaN(OrderItemPickerDialogCard.Height) ? OrderItemPickerDialogCard.ActualHeight : OrderItemPickerDialogCard.Height;
        var right = left + width;
        var bottom = top + height;

        if (resizeFromLeft)
        {
            var maxLeft = right - minWidth;
            left = Math.Clamp(left + horizontalChange, DialogEdgeMargin, maxLeft);
            width = right - left;
        }
        else
        {
            var maxWidth = Math.Max(minWidth, availableWidth - left - DialogEdgeMargin);
            width = Math.Clamp(width + horizontalChange, minWidth, maxWidth);
        }

        if (resizeFromTop)
        {
            var maxTop = bottom - minHeight;
            top = Math.Clamp(top + verticalChange, DialogEdgeMargin, maxTop);
            height = bottom - top;
        }
        else
        {
            var maxHeight = Math.Max(minHeight, availableHeight - top - DialogEdgeMargin);
            height = Math.Clamp(height + verticalChange, minHeight, maxHeight);
        }

        OrderItemPickerDialogCard.Width = width;
        OrderItemPickerDialogCard.Height = height;
        SetOrderItemPickerOffsets(left, top);
    }

    private void ResizeServingPlan(double horizontalChange, double verticalChange)
    {
        if (ServingPlanCanvasRoot is null)
        {
            return;
        }

        var maxWidth = Math.Max(ServingPlanDialogCard.MinWidth, Math.Max(ActualWidth, ServingPlanCanvasRoot.ActualWidth) - (DialogEdgeMargin * 2d));
        var maxHeight = Math.Max(ServingPlanDialogCard.MinHeight, Math.Max(ActualHeight, ServingPlanCanvasRoot.ActualHeight) - (DialogEdgeMargin * 2d));
        ServingPlanDialogCard.Width = Math.Clamp(ServingPlanDialogCard.Width + horizontalChange, ServingPlanDialogCard.MinWidth, maxWidth);
        ServingPlanDialogCard.Height = Math.Clamp(ServingPlanDialogCard.Height + verticalChange, ServingPlanDialogCard.MinHeight, maxHeight);
        SetServingPlanOffsets(GetCanvasLeft(ServingPlanDialogCard), GetCanvasTop(ServingPlanDialogCard));
    }

    private void SetOrderItemPickerOffsets(double left, double top)
    {
        if (OrderItemPickerCanvasRoot is null)
        {
            return;
        }

        var availableWidth = Math.Max(OrderItemPickerCanvasRoot.ActualWidth, ActualWidth);
        var availableHeight = Math.Max(OrderItemPickerCanvasRoot.ActualHeight, ActualHeight);
        var maxLeft = Math.Max(DialogEdgeMargin, availableWidth - OrderItemPickerDialogCard.ActualWidth - DialogEdgeMargin);
        var maxTop = Math.Max(DialogEdgeMargin, availableHeight - OrderItemPickerDialogCard.ActualHeight - DialogEdgeMargin);
        Canvas.SetLeft(OrderItemPickerDialogCard, Math.Clamp(left, DialogEdgeMargin, maxLeft));
        Canvas.SetTop(OrderItemPickerDialogCard, Math.Clamp(top, DialogEdgeMargin, maxTop));
    }

    private void SetServingPlanOffsets(double left, double top)
    {
        if (ServingPlanCanvasRoot is null)
        {
            return;
        }

        var availableWidth = Math.Max(ServingPlanCanvasRoot.ActualWidth, ActualWidth);
        var availableHeight = Math.Max(ServingPlanCanvasRoot.ActualHeight, ActualHeight);
        var maxLeft = Math.Max(DialogEdgeMargin, availableWidth - ServingPlanDialogCard.ActualWidth - DialogEdgeMargin);
        var maxTop = Math.Max(DialogEdgeMargin, availableHeight - ServingPlanDialogCard.ActualHeight - DialogEdgeMargin);
        Canvas.SetLeft(ServingPlanDialogCard, Math.Clamp(left, DialogEdgeMargin, maxLeft));
        Canvas.SetTop(ServingPlanDialogCard, Math.Clamp(top, DialogEdgeMargin, maxTop));
    }

    private void EnsureDialogCanvasPositions()
    {
        if (OrderItemPickerDialogCard is not null)
        {
            Canvas.SetLeft(OrderItemPickerDialogCard, GetCanvasLeft(OrderItemPickerDialogCard, 170d));
            Canvas.SetTop(OrderItemPickerDialogCard, GetCanvasTop(OrderItemPickerDialogCard, 72d));
        }

        if (ServingPlanDialogCard is not null)
        {
            Canvas.SetLeft(ServingPlanDialogCard, GetCanvasLeft(ServingPlanDialogCard, 230d));
            Canvas.SetTop(ServingPlanDialogCard, GetCanvasTop(ServingPlanDialogCard, 86d));
        }
    }

    private static double GetCanvasLeft(FrameworkElement element, double fallback = 0d)
    {
        var value = Canvas.GetLeft(element);
        return double.IsNaN(value) ? fallback : value;
    }

    private static double GetCanvasTop(FrameworkElement element, double fallback = 0d)
    {
        var value = Canvas.GetTop(element);
        return double.IsNaN(value) ? fallback : value;
    }

    private void TestKitchenPrinterButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AlacarteRestaurantViewModel vm && vm.TestKitchenPrinterCommand.CanExecute(null))
        {
            vm.TestKitchenPrinterCommand.Execute(null);
        }
    }

    private void TestBarPrinterButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AlacarteRestaurantViewModel vm && vm.TestBarPrinterCommand.CanExecute(null))
        {
            vm.TestBarPrinterCommand.Execute(null);
        }
    }

    private void TestReceiptPrinterButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AlacarteRestaurantViewModel vm && vm.TestReceiptPrinterCommand.CanExecute(null))
        {
            vm.TestReceiptPrinterCommand.Execute(null);
        }
    }
}
