using ASMILE.BO.Application.DTOs;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Controls;

public partial class ExplorerItemControl : UserControl
{
    public ExplorerItemControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty NodeProperty = DependencyProperty.Register(
        nameof(Node),
        typeof(VirtualExplorerNodeDto),
        typeof(ExplorerItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(ExplorerItemControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty ShowSubPathProperty = DependencyProperty.Register(
        nameof(ShowSubPath),
        typeof(bool),
        typeof(ExplorerItemControl),
        new PropertyMetadata(false));

    public VirtualExplorerNodeDto? Node
    {
        get => (VirtualExplorerNodeDto?)GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowSubPath
    {
        get => (bool)GetValue(ShowSubPathProperty);
        set => SetValue(ShowSubPathProperty, value);
    }
}

