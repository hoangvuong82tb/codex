using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace ASMILE.BO.App.Models;

public sealed partial class AppWindowViewModel : ObservableObject
{
    public AppWindowViewModel(AppModuleInfo module, object contentViewModel, double left, double top)
    {
        Id = Guid.NewGuid();
        Module = module;
        ContentViewModel = contentViewModel;
        Left = left;
        Top = top;
        RestoreLeft = left;
        RestoreTop = top;
    }

    public Guid Id { get; }
    public AppModuleInfo Module { get; }
    public object ContentViewModel { get; }
    internal IDisposable? ServiceScope { get; set; }

    [ObservableProperty]
    private double left;

    [ObservableProperty]
    private double top;

    [ObservableProperty]
    private double width = 1080;

    [ObservableProperty]
    private double height = 680;

    [ObservableProperty]
    private double minWidth = 720;

    [ObservableProperty]
    private double minHeight = 460;

    [ObservableProperty]
    private int zIndex;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private bool isMinimized;

    [ObservableProperty]
    private bool isMaximized;

    [ObservableProperty]
    private AppWindowState state = AppWindowState.Opened;

    public double RestoreLeft { get; set; }

    public double RestoreTop { get; set; }

    public double RestoreWidth { get; set; } = 1080;

    public double RestoreHeight { get; set; } = 680;

    public Visibility WindowVisibility => IsMinimized ? Visibility.Collapsed : Visibility.Visible;

    public bool IsInactive => !IsActive;

    partial void OnIsMinimizedChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowVisibility));
    }

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsInactive));
    }
}
