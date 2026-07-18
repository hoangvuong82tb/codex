using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMILE.BO.App.Converters;

public sealed class WarehouseChartColorConverter : IValueConverter
{
    private static readonly Brush[] Palette =
    [
        Create("#4FA7D9"),
        Create("#57B8BA"),
        Create("#6EC07D"),
        Create("#F2B652"),
        Create("#EB8A83"),
        Create("#3B82F6"),
        Create("#A78BFA"),
        Create("#22C55E")
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var index = value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => 0
        };

        if (index < 0)
        {
            index = 0;
        }

        return Palette[index % Palette.Length];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static SolidColorBrush Create(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
