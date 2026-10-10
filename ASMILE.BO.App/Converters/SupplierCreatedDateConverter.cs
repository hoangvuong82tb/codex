using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class SupplierCreatedDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int id && id > 0)
        {
            return DateTime.Today.AddDays(-(id * 2)).ToString("dd/MM/yyyy", culture);
        }

        return DateTime.Today.ToString("dd/MM/yyyy", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
