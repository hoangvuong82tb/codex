using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class LedgerDocumentTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LedgerDocumentType documentType)
        {
            return "Tất cả";
        }

        return documentType switch
        {
            LedgerDocumentType.ImportVoucher => "Phiếu nhập",
            LedgerDocumentType.ExportVoucher => "Phiếu xuất",
            LedgerDocumentType.PaymentReceipt => "Phiếu thu",
            LedgerDocumentType.PaymentVoucher => "Phiếu chi",
            LedgerDocumentType.AdvancePayment => "Tạm ứng",
            LedgerDocumentType.Adjustment => "Điều chỉnh",
            LedgerDocumentType.OpeningBalance => "Số dư đầu kỳ",
            _ => "Khác"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class LedgerAccountTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LedgerAccountType accountType)
        {
            return "-";
        }

        return accountType switch
        {
            LedgerAccountType.Asset => "Tài sản",
            LedgerAccountType.Liability => "Nợ phải trả",
            LedgerAccountType.Equity => "Vốn chủ sở hữu",
            LedgerAccountType.Revenue => "Doanh thu",
            LedgerAccountType.Expense => "Chi phí",
            LedgerAccountType.Other => "Khác",
            _ => "Khác"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
