using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class VietnameseTextRepairConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Repair(value?.ToString() ?? string.Empty);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;

    public static string Repair(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !LooksCorrupted(text))
        {
            return text;
        }

        try
        {
            var bytes = text.Select(ch => ch <= 255 ? (byte)ch : (byte)'?').ToArray();
            var repaired = Encoding.UTF8.GetString(bytes);
            return LooksCorrupted(repaired) ? text : repaired;
        }
        catch
        {
            return text;
        }
    }

    private static bool LooksCorrupted(string text)
        => text.Contains('Ã')
           || text.Contains('Â')
           || text.Contains('Ä')
           || text.Contains("áº", StringComparison.Ordinal)
           || text.Contains("á»", StringComparison.Ordinal)
           || text.Contains("Æ", StringComparison.Ordinal);
}
