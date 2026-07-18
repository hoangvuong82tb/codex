using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class WarehousePercentTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var stock = TryGetDecimal(values.ElementAtOrDefault(0));
        var total = TryGetDecimal(values.ElementAtOrDefault(1));
        var percent = total > 0 ? Math.Round(stock / total * 100m, 0, MidpointRounding.AwayFromZero) : 0m;

        return $"{FormatNumber(stock)} ({percent:0}%)";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => targetTypes.Select(_ => Binding.DoNothing).ToArray();

    private static decimal TryGetDecimal(object? value)
    {
        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => (decimal)doubleValue,
            float floatValue => (decimal)floatValue,
            int intValue => intValue,
            long longValue => longValue,
            short shortValue => shortValue,
            _ => 0m
        };
    }

    private static string FormatNumber(decimal number)
    {
        var numberCulture = CultureInfo.GetCultureInfo("en-US");
        var format = number == decimal.Truncate(number) ? "N0" : "#,0.####";
        return number.ToString(format, numberCulture);
    }
}
