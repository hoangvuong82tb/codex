using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class StayNightsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is DateOnly arrival && values[1] is DateOnly departure)
            return Math.Max(0, departure.DayNumber - arrival.DayNumber);
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
