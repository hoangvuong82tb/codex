using ASMILE.BO.App.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Controls;

public partial class ExplorerDriveItem : UserControl
{
    public ExplorerDriveItem()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateBarWidth();
    }

    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item),
        typeof(ExplorerDriveItemModel),
        typeof(ExplorerDriveItem),
        new PropertyMetadata(null, OnItemChanged));

    public static readonly DependencyProperty UsedBarWidthProperty = DependencyProperty.Register(
        nameof(UsedBarWidth),
        typeof(double),
        typeof(ExplorerDriveItem),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty UsedBarBrushProperty = DependencyProperty.Register(
        nameof(UsedBarBrush),
        typeof(Brush),
        typeof(ExplorerDriveItem),
        new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"))));

    public ExplorerDriveItemModel? Item
    {
        get => (ExplorerDriveItemModel?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public double UsedBarWidth
    {
        get => (double)GetValue(UsedBarWidthProperty);
        set => SetValue(UsedBarWidthProperty, value);
    }

    public Brush UsedBarBrush
    {
        get => (Brush)GetValue(UsedBarBrushProperty);
        set => SetValue(UsedBarBrushProperty, value);
    }

    private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ExplorerDriveItem control)
        {
            return;
        }

        control.UpdateBarWidth();
        control.UsedBarBrush = control.Item?.IsAlert == true
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
    }

    private void UpdateBarWidth()
    {
        var percent = Item?.UsedPercent ?? 0;
        percent = Math.Clamp(percent, 0.01, 1.0);
        var available = Math.Max(100, ActualWidth - 78);
        UsedBarWidth = available * percent;
    }
}
