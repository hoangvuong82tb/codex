using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMILE.BO.App.Converters;

public class OccupancyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() switch
    { "Occupied" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "Reserved" => new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)), "DueOut" => new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)), _ => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)) };
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class HousekeepingToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() switch
    { "Dirty" => new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)), "Cleaning" => new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)), "Inspected" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "OutOfOrder" or "OutOfService" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), _ => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)) };
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value is Visibility v ? v == Visibility.Visible : throw new NotImplementedException();
}

public class TabIndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) => value is int i && parameter is string s && int.TryParse(s, out var tab) && i == tab ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is string s ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s)) : new SolidColorBrush(System.Windows.Media.Colors.Gray);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() ?? "7";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v?.ToString() ?? "7";
}

public class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? new System.Windows.Thickness(2) : new System.Windows.Thickness(0);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class RoutingToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "SplitPercentage" => Visibility.Visible, _ => Visibility.Collapsed };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class RoutingToFixedVisConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "SplitFixed" => Visibility.Visible, _ => Visibility.Collapsed };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ScoreToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is int s ? (s >= 80 ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)) : s >= 50 ? new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)) : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))) : new SolidColorBrush(Colors.Gray);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class WeekendToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)) : new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ActiveToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)) : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ActiveToTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? "BAT" : "TAT";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is decimal d && d > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Open" => new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)), "Assigned" => new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)), "InProgress" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "Completed" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), "Cancelled" => new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)), _ => new SolidColorBrush(Colors.Gray) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class PriorityToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Emergency" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "Urgent" => new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)), "High" => new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)), "Normal" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), _ => new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class FeedbackTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Complaint" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "Compliment" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), "Suggestion" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "Maintenance" => new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)), _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class LevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Diamond" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "Platinum" => new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)), "Gold" => new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)), "Silver" => new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)), _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class EarnRedeemColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Earn" or "Adjust" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), "Redeem" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class SeverityToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Critical" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "Warning" => new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)), "Success" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), _ => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ActionToColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value?.ToString() switch { "Create" or "CheckIn" or "Complete" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), "Update" or "Assign" => new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), "Delete" or "Cancel" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "Payment" or "Refund" => new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x4A)), _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) };
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
