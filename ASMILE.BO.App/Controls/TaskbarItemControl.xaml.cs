using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Controls;

public partial class TaskbarItemControl : UserControl
{
    public TaskbarItemControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
        nameof(IconSource),
        typeof(ImageSource),
        typeof(TaskbarItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.Register(
        nameof(IndicatorBrush),
        typeof(Brush),
        typeof(TaskbarItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(TaskbarItemControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(TaskbarItemControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsMinimizedProperty = DependencyProperty.Register(
        nameof(IsMinimized),
        typeof(bool),
        typeof(TaskbarItemControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty InvokeCommandProperty = DependencyProperty.Register(
        nameof(InvokeCommand),
        typeof(ICommand),
        typeof(TaskbarItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty InvokeCommandParameterProperty = DependencyProperty.Register(
        nameof(InvokeCommandParameter),
        typeof(object),
        typeof(TaskbarItemControl),
        new PropertyMetadata(null));

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public Brush? IndicatorBrush
    {
        get => (Brush?)GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    public ICommand? InvokeCommand
    {
        get => (ICommand?)GetValue(InvokeCommandProperty);
        set => SetValue(InvokeCommandProperty, value);
    }

    public object? InvokeCommandParameter
    {
        get => GetValue(InvokeCommandParameterProperty);
        set => SetValue(InvokeCommandParameterProperty, value);
    }
}
