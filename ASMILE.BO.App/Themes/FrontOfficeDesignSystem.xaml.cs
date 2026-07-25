using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Themes;

public partial class FrontOfficeDesignSystem : ResourceDictionary
{
    private static Calendar? FindParentCalendar(DependencyObject d)
    {
        while (d != null && d is not Calendar)
            d = VisualTreeHelper.GetParent(d);
        return d as Calendar;
    }

    private void OnCalendarToday(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var cal = FindParentCalendar(btn);
            if (cal != null) cal.SelectedDate = DateTime.Today;
        }
    }

    private void OnCalendarTomorrow(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var cal = FindParentCalendar(btn);
            if (cal != null) cal.SelectedDate = DateTime.Today.AddDays(1);
        }
    }

    private void OnCalendarClear(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var cal = FindParentCalendar(btn);
            if (cal != null) cal.SelectedDate = null;
        }
    }
}
