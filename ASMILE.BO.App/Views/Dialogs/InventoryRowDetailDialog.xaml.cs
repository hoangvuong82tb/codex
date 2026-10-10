using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class InventoryRowDetailDialog : Window
{
    public InventoryRowDetailDialog(object row)
    {
        InitializeComponent();
        DataContext = InventoryRowDetailModel.From(row);
    }
}

public sealed class InventoryRowDetailModel
{
    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("en-US");

    public string Title { get; init; } = "Chi tiết";
    public string Subtitle { get; init; } = string.Empty;
    public string BadgeText { get; init; } = "Chi tiết";
    public Brush BadgeBackground { get; init; } = Brush("#E8F1FF");
    public Brush BadgeForeground { get; init; } = Brush("#2563EB");
    public ObservableCollection<InventoryDetailSection> Sections { get; } = new();

    public static InventoryRowDetailModel From(object row)
    {
        return row switch
        {
            InventoryStockBalanceDto stock => FromStock(stock),
            InventoryMovementDto movement => FromMovement(movement),
            InventoryDocumentDto document => FromDocument(document),
            InventoryLowStockAlertDto alert => FromAlert(alert),
            _ => FromUnknown(row)
        };
    }

    private static InventoryRowDetailModel FromStock(InventoryStockBalanceDto x)
    {
        var model = new InventoryRowDetailModel
        {
            Title = $"{x.ProductCode} - {x.ProductName}",
            Subtitle = $"Kho {x.WarehouseCode} • {x.WarehouseName} • Đơn vị {x.Unit}",
            BadgeText = x.Status,
            BadgeBackground = x.Status.Contains("Âm", StringComparison.OrdinalIgnoreCase) ? Brush("#FEE2E2") : x.Status.Contains("thấp", StringComparison.OrdinalIgnoreCase) ? Brush("#FEF3C7") : Brush("#DCFCE7"),
            BadgeForeground = x.Status.Contains("Âm", StringComparison.OrdinalIgnoreCase) ? Brush("#DC2626") : x.Status.Contains("thấp", StringComparison.OrdinalIgnoreCase) ? Brush("#B45309") : Brush("#15803D")
        };

        model.Sections.Add(Section("Thông tin hàng hóa",
            Row("Mã hàng", x.ProductCode),
            Row("Tên hàng", x.ProductName),
            Row("Nhóm hàng", x.ItemGroup),
            Row("Kho", $"{x.WarehouseCode} - {x.WarehouseName}"),
            Row("Đơn vị tính", x.Unit)));
        model.Sections.Add(Section("Số lượng tồn kho",
            Row("Đầu kỳ", N(x.OpeningQuantity)),
            Row("Nhập trong kỳ", N(x.TotalIn)),
            Row("Xuất trong kỳ", N(x.TotalOut)),
            Row("Tồn cuối", N(x.ClosingQuantity)),
            Row("Ngưỡng tối thiểu", N(x.MinStockThreshold))));
        model.Sections.Add(Section("Giá trị",
            Row("Giá bình quân", N(x.AverageCost)),
            Row("Giá trị tồn đầu", N(x.OpeningValue)),
            Row("Giá trị nhập", N(x.InValue)),
            Row("Giá trị xuất", N(x.OutValue)),
            Row("Giá trị tồn cuối", N(x.ClosingValue))));
        return model;
    }

    private static InventoryRowDetailModel FromMovement(InventoryMovementDto x)
    {
        var model = new InventoryRowDetailModel
        {
            Title = $"{x.DocumentNo} - {x.ProductCode}",
            Subtitle = $"{x.MovementDate:dd/MM/yyyy} • {DocumentTypeText(x.DocumentType)} • Kho {x.WarehouseCode}",
            BadgeText = DocumentTypeText(x.DocumentType),
            BadgeBackground = x.QuantityIn > 0 ? Brush("#DCFCE7") : x.QuantityOut > 0 ? Brush("#FEE2E2") : Brush("#E8F1FF"),
            BadgeForeground = x.QuantityIn > 0 ? Brush("#15803D") : x.QuantityOut > 0 ? Brush("#DC2626") : Brush("#2563EB")
        };

        model.Sections.Add(Section("Chứng từ",
            Row("Số chứng từ", x.DocumentNo),
            Row("Ngày", x.MovementDate.ToString("dd/MM/yyyy")),
            Row("Loại", DocumentTypeText(x.DocumentType)),
            Row("Diễn giải", x.Description),
            Row("Người tạo", x.CreatedBy)));
        model.Sections.Add(Section("Hàng hóa và kho",
            Row("Mã hàng", x.ProductCode),
            Row("Tên hàng", x.ProductName),
            Row("Nhóm hàng", x.ItemGroup),
            Row("Kho", $"{x.WarehouseCode} - {x.WarehouseName}"),
            Row("Đơn vị", x.Unit)));
        model.Sections.Add(Section("Số lượng và giá trị",
            Row("Nhập", N(x.QuantityIn)),
            Row("Xuất", N(x.QuantityOut)),
            Row("Tồn sau giao dịch", N(x.BalanceQuantity)),
            Row("Đơn giá", N(x.UnitPrice)),
            Row("Thành tiền", N(x.Amount))));
        return model;
    }

    private static InventoryRowDetailModel FromDocument(InventoryDocumentDto x)
    {
        var model = new InventoryRowDetailModel
        {
            Title = x.DocumentNo,
            Subtitle = $"{x.DocumentDate:dd/MM/yyyy} • {DocumentTypeText(x.DocumentType)} • Kho {x.WarehouseCode}",
            BadgeText = x.Status,
            BadgeBackground = Brush("#DCFCE7"),
            BadgeForeground = Brush("#15803D")
        };

        model.Sections.Add(Section("Thông tin chứng từ",
            Row("Số chứng từ", x.DocumentNo),
            Row("Ngày", x.DocumentDate.ToString("dd/MM/yyyy")),
            Row("Loại", DocumentTypeText(x.DocumentType)),
            Row("Kho", $"{x.WarehouseCode} - {x.WarehouseName}"),
            Row("Trạng thái", x.Status)));
        model.Sections.Add(Section("Tổng hợp",
            Row("Tổng mặt hàng", N(x.TotalItems)),
            Row("Tổng số lượng", N(x.TotalQuantity)),
            Row("Tổng giá trị", N(x.TotalAmount))));
        return model;
    }

    private static InventoryRowDetailModel FromAlert(InventoryLowStockAlertDto x)
    {
        var critical = x.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase);
        var model = new InventoryRowDetailModel
        {
            Title = $"{x.ProductCode} - {x.ProductName}",
            Subtitle = $"Kho {x.WarehouseCode} • Cảnh báo tồn kho",
            BadgeText = SeverityText(x.Severity),
            BadgeBackground = critical ? Brush("#FEE2E2") : Brush("#FEF3C7"),
            BadgeForeground = critical ? Brush("#DC2626") : Brush("#B45309")
        };

        model.Sections.Add(Section("Cảnh báo",
            Row("Mã hàng", x.ProductCode),
            Row("Tên hàng", x.ProductName),
            Row("Kho", x.WarehouseCode),
            Row("Tồn hiện tại", N(x.CurrentStock)),
            Row("Ngưỡng tối thiểu", N(x.MinStockThreshold)),
            Row("Mức tồn mục tiêu", N(x.TargetStockThreshold)),
            Row("Số lượng thiếu", N(x.MissingQuantity))));
        model.Sections.Add(Section("Gợi ý xử lý",
            Row("Nhà cung cấp cấu hình", x.SuggestedSupplier),
            Row("Gợi ý nhập thêm", N(x.SuggestedPurchaseQty)),
            Row("Đã ẩn cảnh báo", x.IsHidden ? "Có" : "Không")));
        return model;
    }

    private static InventoryRowDetailModel FromUnknown(object row)
    {
        var model = new InventoryRowDetailModel { Title = "Chi tiết dòng", Subtitle = row.GetType().Name };
        model.Sections.Add(Section("Dữ liệu", Row("Kiểu dữ liệu", row.GetType().FullName ?? row.GetType().Name), Row("Giá trị", row.ToString() ?? string.Empty)));
        return model;
    }

    private static InventoryDetailSection Section(string title, params InventoryDetailRow[] rows)
        => new(title, rows);

    private static InventoryDetailRow Row(string label, string value)
        => new(label, value);

    private static string N(decimal value)
    {
        var format = value == decimal.Truncate(value) ? "N0" : "#,0.####";
        return value.ToString(format, NumberCulture);
    }

    private static string N(int value) => value.ToString("N0", NumberCulture);

    private static string DocumentTypeText(InventoryDocumentType type)
    {
        return type switch
        {
            InventoryDocumentType.OpeningStock => "Tồn đầu kỳ",
            InventoryDocumentType.ImportVoucher => "Phiếu nhập",
            InventoryDocumentType.ExportVoucher => "Phiếu xuất",
            InventoryDocumentType.AdjustmentIn => "Điều chỉnh tăng",
            InventoryDocumentType.AdjustmentOut => "Điều chỉnh giảm",
            InventoryDocumentType.TransferIn => "Chuyển kho đến",
            InventoryDocumentType.TransferOut => "Chuyển kho đi",
            InventoryDocumentType.StockTake => "Kiểm kê",
            _ => type.ToString()
        };
    }

    private static string SeverityText(string severity)
    {
        return severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "Nghiêm trọng"
            : severity.Equals("Warning", StringComparison.OrdinalIgnoreCase)
                ? "Cảnh báo"
                : severity;
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}

public sealed record InventoryDetailSection(string Title, IReadOnlyList<InventoryDetailRow> Rows);
public sealed record InventoryDetailRow(string Label, string Value);
