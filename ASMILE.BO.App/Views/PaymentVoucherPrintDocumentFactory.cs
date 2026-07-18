using ASMILE.BO.App.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public static class PaymentVoucherPrintDocumentFactory
{
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;
    private const double PageMargin = 42;

    public static FixedDocument Build(PaymentVoucherDetailModuleViewModel voucher)
    {
        var viVn = CultureInfo.GetCultureInfo("vi-VN");
        var fixedDocument = new FixedDocument();
        var page = new FixedPage
        {
            Width = A4Width,
            Height = A4Height,
            Background = Brushes.White
        };

        var content = new Grid
        {
            Width = A4Width - (PageMargin * 2),
            Margin = new Thickness(PageMargin)
        };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var company = new TextBlock
        {
            Text = "ASMILE BO",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#0F172A")
        };
        Grid.SetRow(company, 0);
        content.Children.Add(company);

        var printedAt = new TextBlock
        {
            Text = "Ngày in: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            FontSize = 11.5,
            Foreground = Brush("#475569"),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetRow(printedAt, 0);
        content.Children.Add(printedAt);

        var title = new TextBlock
        {
            Text = "PHIẾU CHI",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#0F172A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 14)
        };
        Grid.SetRow(title, 1);
        content.Children.Add(title);

        var infoGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddInfo(infoGrid, 0, 0, "Nhà cung cấp:", $"{voucher.SupplierName} ({voucher.SupplierCode})", 2, true);
        AddInfo(infoGrid, 1, 0, "Số phiếu:", voucher.DocumentNo);
        AddInfo(infoGrid, 1, 1, "Ngày chứng từ:", voucher.VoucherDate.ToString("dd/MM/yyyy"));
        AddInfo(infoGrid, 2, 0, "Trạng thái:", voucher.Status);
        AddInfo(infoGrid, 2, 1, "Tổng tiền:", voucher.TotalAmount.ToString("N0", viVn), 1, true);
        AddInfo(infoGrid, 3, 0, "Tài khoản nợ/có:", $"{voucher.DebitAccountCode} / {voucher.CreditAccountCode}");
        Grid.SetRow(infoGrid, 2);
        content.Children.Add(infoGrid);

        var table = BuildDetailsTable(voucher, viVn);
        Grid.SetRow(table, 3);
        content.Children.Add(table);

        var signGrid = new Grid { Margin = new Thickness(0, 24, 0, 0) };
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.Children.Add(SignBox("Người lập phiếu"));
        var signMiddle = SignBox("Kế toán");
        Grid.SetColumn(signMiddle, 1);
        signGrid.Children.Add(signMiddle);
        var signRight = SignBox("Người nhận tiền");
        Grid.SetColumn(signRight, 2);
        signGrid.Children.Add(signRight);
        Grid.SetRow(signGrid, 4);
        content.Children.Add(signGrid);

        page.Children.Add(content);
        var pageContent = new PageContent();
        ((System.Windows.Markup.IAddChild)pageContent).AddChild(page);
        fixedDocument.Pages.Add(pageContent);
        return fixedDocument;
    }

    private static UIElement BuildDetailsTable(PaymentVoucherDetailModuleViewModel voucher, CultureInfo culture)
    {
        var tableWidth = A4Width - (PageMargin * 2);
        var outer = new Border
        {
            BorderBrush = Brush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = tableWidth
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        for (var i = 0; i < voucher.Details.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var headers = new[] { "STT", "Chứng từ", "Diễn giải", "Phương thức", "Thành tiền" };
        for (var c = 0; c < headers.Length; c++)
        {
            var header = Cell(headers[c], true, TextAlignment.Center);
            header.Background = Brush("#F1F5F9");
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c);
            grid.Children.Add(header);
        }

        for (var i = 0; i < voucher.Details.Count; i++)
        {
            var d = voucher.Details[i];
            var row = i + 1;
            var cells = new[]
            {
                d.LineNo.ToString(culture),
                d.DocumentNo,
                d.Description,
                d.Method,
                d.Amount.ToString("N0", culture)
            };

            for (var c = 0; c < cells.Length; c++)
            {
                var align = c == 4 ? TextAlignment.Right : (c == 0 ? TextAlignment.Center : TextAlignment.Left);
                var cell = Cell(cells[c], false, align, c == 2);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }

        outer.Child = grid;
        return outer;
    }

    private static Border Cell(string text, bool isHeader, TextAlignment align, bool wrap = false) => new()
    {
        BorderBrush = Brush("#CBD5E1"),
        BorderThickness = new Thickness(0, 0, 1, 1),
        Padding = new Thickness(4, 4, 4, 4),
        Child = new TextBlock
        {
            Text = text ?? string.Empty,
            FontSize = isHeader ? 12.5 : 12,
            FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#0F172A"),
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
        }
    };

    private static TextBlock SignBox(string label) => new()
    {
        Text = $"{label}\n(Ký, ghi rõ họ tên)",
        TextAlignment = TextAlignment.Center,
        FontSize = 12,
        Foreground = Brush("#334155")
    };

    private static void AddInfo(Grid infoGrid, int row, int col, string label, string? value, int columnSpan = 1, bool bold = false)
    {
        while (infoGrid.RowDefinitions.Count <= row)
        {
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var text = new TextBlock
        {
            Margin = new Thickness(0, 2, 18, 2),
            FontSize = 13,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Brush("#1E293B"),
            Text = $"{label} {value ?? string.Empty}"
        };
        Grid.SetRow(text, row);
        Grid.SetColumn(text, col);
        Grid.SetColumnSpan(text, columnSpan);
        infoGrid.Children.Add(text);
    }

    private static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
}
