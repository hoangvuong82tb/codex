using ASMILE.BO.App.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Controls;

public partial class StartPinnedAppTileControl : UserControl
{
    public StartPinnedAppTileControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register(
        nameof(Module),
        typeof(AppModuleInfo),
        typeof(StartPinnedAppTileControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(
        nameof(OpenCommand),
        typeof(ICommand),
        typeof(StartPinnedAppTileControl),
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
}
