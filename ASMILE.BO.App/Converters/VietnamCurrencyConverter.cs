using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class VietnamCurrencyConverter : IValueConverter
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var amount = value is decimal decimalValue
            ? decimalValue
            : System.Convert.ToDecimal(value ?? 0m, CultureInfo.InvariantCulture);
        return string.Equals(parameter?.ToString(), "Input", StringComparison.OrdinalIgnoreCase)
            ? amount.ToString("N0", VietnameseCulture)
            : $"{amount.ToString("N0", VietnameseCulture)} đ";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.Trim().Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;
        if (decimal.TryParse(text, NumberStyles.Number, VietnameseCulture, out var amount))
            return amount;

        var digits = new string(text.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out amount) ? amount : 0m;
    }
}
