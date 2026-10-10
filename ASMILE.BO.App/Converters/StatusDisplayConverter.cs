using System.Globalization;
using System.Windows.Data;
using ASMILE.BO.App.Services;

namespace ASMILE.BO.App.Converters;

public sealed class StatusDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var mode = parameter?.ToString() ?? "status";
        return mode switch
        {
            "status" => OperationsDisplayTextTranslator.TranslateStatus(text),
            "eventType" => OperationsDisplayTextTranslator.TranslateEventType(text),
            "severity" => OperationsDisplayTextTranslator.TranslateSeverity(text),
            "module" => OperationsDisplayTextTranslator.TranslateModule(text),
            "message" => OperationsDisplayTextTranslator.TranslateMessage(text),
            _ => OperationsDisplayTextTranslator.TranslateStatus(text)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
