using ASMILE.BO.App.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ASMILE.BO.App.Controls;

public partial class MetricBarChart : UserControl
{
    private const double PlotHeight = 126d;
    private const double BarMaxHeight = 108d;
    private static readonly Brush InBrush = CreateBrush("#66C282");
    private static readonly Brush OutBrush = CreateBrush("#F08A83");
    private static readonly Brush GridBrush = CreateBrush("#ECF0F7");
    private static readonly Brush AxisTextBrush = CreateBrush("#4B5563");

    public ObservableCollection<MetricBarPair> Bars { get; } = new();
    private INotifyCollectionChanged? _inSeriesNotifier;
    private INotifyCollectionChanged? _outSeriesNotifier;
    private decimal _axisMax = 500m;

    public MetricBarChart()
    {
        InitializeComponent();
        Loaded += (_, _) => RebuildBars();
        Unloaded += (_, _) =>
        {
            SetSeriesNotifier(ref _inSeriesNotifier, null);
            SetSeriesNotifier(ref _outSeriesNotifier, null);
        };
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(MetricBarChart), new PropertyMetadata(string.Empty));

    public string InSeriesTitle
    {
        get => (string)GetValue(InSeriesTitleProperty);
        set => SetValue(InSeriesTitleProperty, value);
    }

    public static readonly DependencyProperty InSeriesTitleProperty =
        DependencyProperty.Register(nameof(InSeriesTitle), typeof(string), typeof(MetricBarChart), new PropertyMetadata("Nhập"));

    public string OutSeriesTitle
    {
        get => (string)GetValue(OutSeriesTitleProperty);
        set => SetValue(OutSeriesTitleProperty, value);
    }

    public static readonly DependencyProperty OutSeriesTitleProperty =
        DependencyProperty.Register(nameof(OutSeriesTitle), typeof(string), typeof(MetricBarChart), new PropertyMetadata("Xuất"));

    public bool HasOutSeries
    {
        get => (bool)GetValue(HasOutSeriesProperty);
        set => SetValue(HasOutSeriesProperty, value);
    }

    public static readonly DependencyProperty HasOutSeriesProperty =
        DependencyProperty.Register(nameof(HasOutSeries), typeof(bool), typeof(MetricBarChart), new PropertyMetadata(true));

    public IEnumerable<MetricPoint>? ItemsSource
    {
        get => (IEnumerable<MetricPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<MetricPoint>), typeof(MetricBarChart), new PropertyMetadata(null, OnSeriesChanged));

    public IEnumerable<MetricPoint>? OutItemsSource
    {
        get => (IEnumerable<MetricPoint>?)GetValue(OutItemsSourceProperty);
        set => SetValue(OutItemsSourceProperty, value);
    }

    public static readonly DependencyProperty OutItemsSourceProperty =
        DependencyProperty.Register(nameof(OutItemsSource), typeof(IEnumerable<MetricPoint>), typeof(MetricBarChart), new PropertyMetadata(null, OnSeriesChanged));

    public ICommand? ItemClickCommand
    {
        get => (ICommand?)GetValue(ItemClickCommandProperty);
        set => SetValue(ItemClickCommandProperty, value);
    }

    public static readonly DependencyProperty ItemClickCommandProperty =
        DependencyProperty.Register(nameof(ItemClickCommand), typeof(ICommand), typeof(MetricBarChart), new PropertyMetadata(null));

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MetricBarChart chart)
        {
            if (e.Property == ItemsSourceProperty)
            {
                chart.SetSeriesNotifier(ref chart._inSeriesNotifier, e.NewValue as INotifyCollectionChanged);
            }
            else if (e.Property == OutItemsSourceProperty)
            {
                chart.SetSeriesNotifier(ref chart._outSeriesNotifier, e.NewValue as INotifyCollectionChanged);
            }

            chart.RebuildBars();
        }
    }

    private void SetSeriesNotifier(ref INotifyCollectionChanged? field, INotifyCollectionChanged? notifier)
    {
        if (field is not null)
        {
            field.CollectionChanged -= OnSeriesCollectionChanged;
        }

        field = notifier;

        if (field is not null)
        {
            field.CollectionChanged += OnSeriesCollectionChanged;
        }
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildBars();
    }

    private void RebuildBars()
    {
        Bars.Clear();

        var inRows = (ItemsSource ?? Enumerable.Empty<MetricPoint>()).ToList();
        var outRows = (OutItemsSource ?? Enumerable.Empty<MetricPoint>()).ToList();

        var labels = inRows.Select(x => x.Label)
            .Concat(outRows.Select(x => x.Label))
            .Distinct()
            .ToList();
        HasOutSeries = outRows.Any();

        var maxValue = new[]
        {
            inRows.Select(x => x.Value).DefaultIfEmpty(0m).Max(),
            outRows.Select(x => x.Value).DefaultIfEmpty(0m).Max()
        }.Max();

        _axisMax = CalculateAxisMax(maxValue);

        foreach (var label in labels)
        {
            var inValue = inRows.FirstOrDefault(x => x.Label == label)?.Value ?? 0m;
            var outValue = outRows.FirstOrDefault(x => x.Label == label)?.Value ?? 0m;

            Bars.Add(new MetricBarPair
            {
                Label = label,
                InValue = inValue,
                OutValue = outValue,
                InHeight = CalculateHeight(inValue, _axisMax),
                OutHeight = CalculateHeight(outValue, _axisMax)
            });
        }

        QueueRender();
    }

    private void QueueRender()
    {
        if (!IsLoaded)
        {
            return;
        }

        Dispatcher.BeginInvoke(RenderChart, DispatcherPriority.Background);
    }

    private void RenderChart()
    {
        if (PlotCanvas.ActualWidth <= 0)
        {
            return;
        }

        PlotCanvas.Children.Clear();
        AxisLabelPanel.Children.Clear();
        DateLabelsPanel.Children.Clear();
        DateLabelsPanel.Columns = Math.Max(1, Bars.Count);

        RenderAxisAndGrid();
        RenderBars();
        RenderDateLabels();
    }

    private void RenderAxisAndGrid()
    {
        var values = Enumerable.Range(0, 6)
            .Select(i => _axisMax - (_axisMax / 5m * i))
            .ToList();

        for (var i = 0; i < values.Count; i++)
        {
            var y = i == values.Count - 1
                ? PlotHeight - 1
                : i * (PlotHeight / 5d);

            PlotCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = PlotCanvas.ActualWidth,
                Y1 = y,
                Y2 = y,
                Stroke = GridBrush,
                StrokeThickness = 1
            });

            var label = new TextBlock
            {
                Text = FormatAxis(values[i]),
                FontSize = 11,
                Foreground = AxisTextBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = i == values.Count - 1 ? VerticalAlignment.Bottom : VerticalAlignment.Top,
                Margin = new Thickness(0, Math.Max(0, y - 8), 0, 0)
            };
            AxisLabelPanel.Children.Add(label);
        }
    }

    private void RenderBars()
    {
        if (Bars.Count == 0)
        {
            return;
        }

        var groupWidth = PlotCanvas.ActualWidth / Bars.Count;
        var barWidth = Math.Min(13d, Math.Max(8d, groupWidth * 0.18d));
        var gap = 4d;

        for (var i = 0; i < Bars.Count; i++)
        {
            var bar = Bars[i];
            var groupLeft = i * groupWidth;
            var center = groupLeft + groupWidth / 2d;
            var inLeft = center - barWidth - gap / 2d;
            var outLeft = center + gap / 2d;

            AddBar(inLeft, barWidth, bar.InHeight, InBrush, $"Nhập: {bar.InValue:N0}");
            AddBar(outLeft, barWidth, bar.OutHeight, OutBrush, $"Xuất: {bar.OutValue:N0}");
        }
    }

    private void AddBar(double left, double width, double height, Brush brush, string tooltip)
    {
        if (height <= 0d)
        {
            return;
        }

        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = brush,
            RadiusX = 2,
            RadiusY = 2,
            ToolTip = tooltip
        };

        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, PlotHeight - height);
        PlotCanvas.Children.Add(rect);
    }

    private void RenderDateLabels()
    {
        foreach (var bar in Bars)
        {
            DateLabelsPanel.Children.Add(new TextBlock
            {
                Text = bar.Label,
                FontSize = 11,
                Foreground = AxisTextBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }
    }

    private void OnPlotCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueueRender();
    }

    private static double CalculateHeight(decimal value, decimal axisMax)
    {
        if (value <= 0m || axisMax <= 0m)
        {
            return 0d;
        }

        var height = (double)(value / axisMax * (decimal)BarMaxHeight);
        return Math.Max(10d, height);
    }

    private static decimal CalculateAxisMax(decimal maxValue)
    {
        if (maxValue <= 0m)
        {
            return 500m;
        }

        if (maxValue <= 500m)
        {
            return 500m;
        }

        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)maxValue)));
        var normalized = maxValue / magnitude;
        var nice = normalized switch
        {
            <= 1m => 1m,
            <= 2m => 2m,
            <= 2.5m => 2.5m,
            <= 5m => 5m,
            _ => 10m
        };

        return nice * magnitude;
    }

    private static string FormatAxis(decimal value)
    {
        return value == decimal.Truncate(value)
            ? value.ToString("N0", CultureInfo.GetCultureInfo("en-US"))
            : value.ToString("#,0.##", CultureInfo.GetCultureInfo("en-US"));
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}

public sealed class MetricBarPair
{
    public string Label { get; set; } = string.Empty;
    public decimal InValue { get; set; }
    public decimal OutValue { get; set; }
    public double InHeight { get; set; }
    public double OutHeight { get; set; }
}
