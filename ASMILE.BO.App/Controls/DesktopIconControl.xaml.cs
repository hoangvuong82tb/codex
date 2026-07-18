using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ASMILE.BO.App.Controls;

public partial class DesktopIconControl : UserControl
{
    private const double DragThreshold = 4;

    private Point _dragMouseStart;
    private double _dragIconStartLeft;
    private double _dragIconStartTop;
    private bool _isDragging;
    private bool _suppressNextClick;

    public DesktopIconControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register(
        nameof(Module),
        typeof(AppModuleInfo),
        typeof(DesktopIconControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(
        nameof(OpenCommand),
        typeof(ICommand),
        typeof(DesktopIconControl),
        new PropertyMetadata(null));

    public AppModuleInfo? Module
    {
        get => (AppModuleInfo?)GetValue(ModuleProperty);
        set => SetValue(ModuleProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    private void IconButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Module is null)
        {
            return;
        }

        _dragMouseStart = e.GetPosition(GetHostElement());
        _dragIconStartLeft = Module.DesktopLeft;
        _dragIconStartTop = Module.DesktopTop;
        _isDragging = false;

        IconButton.CaptureMouse();
    }

    private void IconButton_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Module is null)
        {
            return;
        }

        var vm = GetShellViewModel();
        if (vm is null)
        {
            return;
        }

        foreach (var module in vm.AllModules)
        {
            module.IsSelected = false;
        }

        Module.IsSelected = true;
        vm.SelectedDesktopModule = Module;
    }

    private void IconButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (Module is null || !IconButton.IsMouseCaptured)
        {
            return;
        }

        var host = GetHostElement();
        var vm = GetShellViewModel();
        if (vm is null)
        {
            return;
        }

        var pos = e.GetPosition(host);
        var deltaX = pos.X - _dragMouseStart.X;
        var deltaY = pos.Y - _dragMouseStart.Y;

        if (!_isDragging && (Math.Abs(deltaX) > DragThreshold || Math.Abs(deltaY) > DragThreshold))
        {
            _isDragging = true;
            _suppressNextClick = true;
        }

        if (_isDragging)
        {
            vm.MoveDesktopIcon(Module, _dragIconStartLeft + deltaX, _dragIconStartTop + deltaY);
        }
    }

    private void IconButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IconButton.IsMouseCaptured)
        {
            IconButton.ReleaseMouseCapture();
        }

        if (_isDragging && Module is not null)
        {
            var vm = GetShellViewModel();
            vm?.SnapDesktopIconToGrid(Module);
        }

        _isDragging = false;
    }

    private void IconButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Module is null)
        {
            return;
        }

        _suppressNextClick = false;

        if (OpenCommand?.CanExecute(Module) == true)
        {
            OpenCommand.Execute(Module);
            TransferFocusToShellWindow();
        }

        e.Handled = true;
    }

    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        if (Module is null)
        {
            return;
        }

        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            e.Handled = true;
            return;
        }

        if (OpenCommand?.CanExecute(Module) == true)
        {
            OpenCommand.Execute(Module);
            TransferFocusToShellWindow();
        }
    }

    private void IconContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (Module is null || sender is not MenuItem item || item.Tag is not string action)
        {
            return;
        }

        var vm = GetShellViewModel();
        if (vm is null)
        {
            return;
        }

        switch (action)
        {
            case "open":
                vm.OpenDesktopModuleCommand.Execute(Module);
                TransferFocusToShellWindow();
                break;
            case "toggle-pin":
                vm.ToggleModuleTaskbarPinCommand.Execute(Module);
                break;
            case "snap-grid":
                vm.SnapModuleIconToGridCommand.Execute(Module);
                break;
            case "hide":
                vm.HideModuleFromDesktopCommand.Execute(Module);
                break;
            case "change-icon":
                vm.OpenModuleSettingsCommand.Execute(Module);
                break;
            case "copy-name":
                vm.CopyModuleNameCommand.Execute(Module);
                break;
            case "properties":
                vm.ShowModulePropertiesCommand.Execute(Module);
                break;
        }

        e.Handled = true;
    }

    private void TransferFocusToShellWindow()
    {
        Dispatcher.BeginInvoke(() =>
        {
            var hostWindow = Window.GetWindow(this);
            if (hostWindow is null)
            {
                return;
            }

            hostWindow.Focus();
            Keyboard.Focus(hostWindow);
        }, DispatcherPriority.Background);
    }

    private DesktopShellViewModel? GetShellViewModel()
    {
        var hostWindow = Window.GetWindow(this);
        return hostWindow?.DataContext as DesktopShellViewModel;
    }

    private IInputElement GetHostElement()
    {
        return (IInputElement?)Window.GetWindow(this) ?? this;
    }
}
