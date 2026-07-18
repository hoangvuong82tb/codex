using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class AppWindowControl : UserControl
{
    private Point? _dragStartPoint;
    private double _startLeft;
    private double _startTop;

    public AppWindowControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty WindowProperty = DependencyProperty.Register(
        nameof(Window),
        typeof(AppWindowViewModel),
        typeof(AppWindowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActivateCommandProperty = DependencyProperty.Register(
        nameof(ActivateCommand),
        typeof(ICommand),
        typeof(AppWindowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MinimizeCommandProperty = DependencyProperty.Register(
        nameof(MinimizeCommand),
        typeof(ICommand),
        typeof(AppWindowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ToggleMaximizeCommandProperty = DependencyProperty.Register(
        nameof(ToggleMaximizeCommand),
        typeof(ICommand),
        typeof(AppWindowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand),
        typeof(ICommand),
        typeof(AppWindowControl),
        new PropertyMetadata(null));

    public AppWindowViewModel? Window
    {
        get => (AppWindowViewModel?)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    public ICommand? ActivateCommand
    {
        get => (ICommand?)GetValue(ActivateCommandProperty);
        set => SetValue(ActivateCommandProperty, value);
    }

    public ICommand? MinimizeCommand
    {
        get => (ICommand?)GetValue(MinimizeCommandProperty);
        set => SetValue(MinimizeCommandProperty, value);
    }

    public ICommand? ToggleMaximizeCommand
    {
        get => (ICommand?)GetValue(ToggleMaximizeCommandProperty);
        set => SetValue(ToggleMaximizeCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window is null)
        {
            return;
        }

        ActivateCommand?.Execute(Window);

        if (e.ClickCount == 2)
        {
            ToggleMaximizeCommand?.Execute(Window);
            return;
        }

        if (Window.IsMaximized)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(System.Windows.Application.Current.MainWindow);
        _startLeft = Window.Left;
        _startTop = Window.Top;
        Mouse.Capture(TitleBar);
    }

    private void RootBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window is null)
        {
            return;
        }

        ActivateCommand?.Execute(Window);
    }

    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        var shellViewModel = GetShellViewModel();
        if (_dragStartPoint is null || Window is null || shellViewModel is null)
        {
            return;
        }

        var position = e.GetPosition(System.Windows.Application.Current.MainWindow);
        var offsetX = position.X - _dragStartPoint.Value.X;
        var offsetY = position.Y - _dragStartPoint.Value.Y;
        shellViewModel.MoveWindow(Window, _startLeft + offsetX, _startTop + offsetY);
    }

    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = null;
        Mouse.Capture(null);
    }

    private void ResizeByEdges(double deltaLeft, double deltaTop, double deltaRight, double deltaBottom)
    {
        var shellViewModel = GetShellViewModel();
        if (Window is null || Window.IsMaximized || shellViewModel is null)
        {
            return;
        }

        ActivateCommand?.Execute(Window);
        shellViewModel.ResizeWindowEdges(Window, deltaLeft, deltaTop, deltaRight, deltaBottom);
    }

    private void ResizeLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(e.HorizontalChange, 0, 0, 0);
    }

    private void ResizeRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(0, 0, e.HorizontalChange, 0);
    }

    private void ResizeTopThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(0, e.VerticalChange, 0, 0);
    }

    private void ResizeBottomThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(0, 0, 0, e.VerticalChange);
    }

    private void ResizeTopLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(e.HorizontalChange, e.VerticalChange, 0, 0);
    }

    private void ResizeTopRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(0, e.VerticalChange, e.HorizontalChange, 0);
    }

    private void ResizeBottomLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(e.HorizontalChange, 0, 0, e.VerticalChange);
    }

    private void ResizeBottomRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeByEdges(0, 0, e.HorizontalChange, e.VerticalChange);
    }

    private DesktopShellViewModel? GetShellViewModel()
    {
        var hostWindow = System.Windows.Window.GetWindow(this);
        return hostWindow?.DataContext as DesktopShellViewModel;
    }
}
