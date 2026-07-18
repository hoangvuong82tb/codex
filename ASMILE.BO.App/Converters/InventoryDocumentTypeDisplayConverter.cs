using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class InventoryDocumentTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => "Tất cả loại chứng từ",
            InventoryDocumentType.OpeningStock => "Tồn đầu kỳ",
            InventoryDocumentType.ImportVoucher => "Phiếu nhập",
            InventoryDocumentType.ExportVoucher => "Phiếu xuất",
            InventoryDocumentType.AdjustmentIn => "Điều chỉnh tăng",
            InventoryDocumentType.AdjustmentOut => "Điều chỉnh giảm",
            InventoryDocumentType.TransferIn => "Chuyển kho đến",
            InventoryDocumentType.TransferOut => "Chuyển kho đi",
            InventoryDocumentType.StockTake => "Kiểm kê",
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
