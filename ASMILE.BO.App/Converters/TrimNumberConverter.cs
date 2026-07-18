using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class TrimNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (!TryGetDecimal(value, out var number))
        {
            return value.ToString() ?? string.Empty;
        }

        var numberCulture = CultureInfo.GetCultureInfo("en-US");
        var format = number == decimal.Truncate(number) ? "N0" : "#,0.####";
        var text = number.ToString(format, numberCulture);

        return parameter?.ToString() == "Plus" && number > 0
            ? $"+{text}"
            : text;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return targetType == typeof(decimal?) ? null! : 0m;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return targetType == typeof(decimal?) ? null! : 0m;
        }

        text = text.TrimStart('+');
        return decimal.TryParse(text, NumberStyles.Number, culture, out var number)
            ? number
            : Binding.DoNothing;
    }

    private static bool TryGetDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case decimal decimalValue:
                number = decimalValue;
                return true;
            case double doubleValue:
                number = (decimal)doubleValue;
                return true;
            case float floatValue:
                number = (decimal)floatValue;
                return true;
            case int intValue:
                number = intValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case short shortValue:
                number = shortValue;
                return true;
            default:
                number = 0m;
                return false;
        }
    }
}
