using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class ReceivableDocumentTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ReceivableDocumentType type)
        {
            return string.Empty;
        }

        return type switch
        {
            ReceivableDocumentType.OpeningBalance => "Số dư đầu kỳ",
            ReceivableDocumentType.SalesInvoice => "Hóa đơn bán hàng",
            ReceivableDocumentType.ExportVoucher => "Phiếu xuất",
            ReceivableDocumentType.PaymentReceipt => "Phiếu thu",
            ReceivableDocumentType.Adjustment => "Điều chỉnh",
            ReceivableDocumentType.DebtStatement => "Đối chiếu công nợ",
            _ => type.ToString()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
