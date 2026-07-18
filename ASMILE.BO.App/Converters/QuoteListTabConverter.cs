using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMILE.BO.App.Converters;

public sealed class QuoteTabBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);
        return isActive ? new SolidColorBrush(Color.FromRgb(37, 99, 235))   // #2563EB
                        : new SolidColorBrush(Colors.White);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class QuoteTabFgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);
        return isActive ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class QuoteTabBrConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);
        return isActive ? new SolidColorBrush(Color.FromRgb(37, 99, 235))   // #2563EB
                        : new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class QuoteTabLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tab = System.Convert.ToInt32(value);
        return tab switch { 0 => "đã duyệt", 1 => "chưa duyệt", 2 => "đang phục vụ", _ => "đã phục vụ" };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class IsAlreadyAddedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        var itemId = values[0] is Guid g ? g : Guid.Empty;
        var ids = values[1] as IEnumerable<Guid>;
        return ids?.Contains(itemId) == true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
