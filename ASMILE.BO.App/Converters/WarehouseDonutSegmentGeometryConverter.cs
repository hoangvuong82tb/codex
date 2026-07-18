using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.App.Converters;

public sealed class WarehouseDonutSegmentGeometryConverter : IMultiValueConverter
{
    private const double Center = 63;
    private const double Radius = 51;
    private const double StartAngle = -90;
    private const double SegmentGapDegrees = 1.4;

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 3 ||
            values[0] is not InventoryWarehouseSummaryDto current ||
            values[1] is not IEnumerable summaries ||
            !TryGetIndex(values[2], out var index))
        {
            return Geometry.Empty;
        }

        var items = summaries.OfType<InventoryWarehouseSummaryDto>().ToList();
        if (items.Count == 0 || index < 0 || index >= items.Count)
        {
            return Geometry.Empty;
        }

        var total = items.Sum(x => Math.Max(0m, x.TotalStock));
        var stock = Math.Max(0m, current.TotalStock);
        if (total <= 0 || stock <= 0)
        {
            return Geometry.Empty;
        }

        var previous = items.Take(index).Sum(x => Math.Max(0m, x.TotalStock));
        var start = StartAngle + DecimalToDegrees(previous / total);
        var sweep = DecimalToDegrees(stock / total);

        if (items.Count > 1)
        {
            sweep = Math.Max(0.1, sweep - SegmentGapDegrees);
        }
        else
        {
            sweep = Math.Min(359.9, sweep);
        }

        return CreateArcGeometry(start, sweep);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => targetTypes.Select(_ => Binding.DoNothing).ToArray();

    private static bool TryGetIndex(object? value, out int index)
    {
        switch (value)
        {
            case int intValue:
                index = intValue;
                return true;
            case long longValue:
                index = (int)longValue;
                return true;
            default:
                index = 0;
                return false;
        }
    }

    private static double DecimalToDegrees(decimal ratio)
        => (double)ratio * 360d;

    private static Geometry CreateArcGeometry(double startAngle, double sweepAngle)
    {
        var start = PointOnCircle(startAngle);
        var end = PointOnCircle(startAngle + sweepAngle);

        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            IsFilled = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(Radius, Radius),
            RotationAngle = 0,
            IsLargeArc = sweepAngle > 180,
            SweepDirection = SweepDirection.Clockwise,
            IsStroked = true
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    private static Point PointOnCircle(double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180d;
        return new Point(
            Center + Radius * Math.Cos(radians),
            Center + Radius * Math.Sin(radians));
    }
}
