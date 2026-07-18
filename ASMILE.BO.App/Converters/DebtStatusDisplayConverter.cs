using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMILE.BO.App.Converters;

public sealed class DebtStatusDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DebtStatus status)
        {
            return "Tất cả";
        }

        return status switch
        {
            DebtStatus.Open => "Mở",
            DebtStatus.PartiallyPaid => "Thanh toán một phần",
            DebtStatus.Paid => "Đã thanh toán",
            DebtStatus.Overdue => "Quá hạn",
            DebtStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class DebtStatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DebtStatus status)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        var color = status switch
        {
            DebtStatus.Paid => "#16A34A",
            DebtStatus.PartiallyPaid => "#2563EB",
            DebtStatus.Overdue => "#DC2626",
            DebtStatus.Cancelled => "#64748B",
            _ => "#F59E0B"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
