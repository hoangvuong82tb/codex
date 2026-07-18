using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public static class StockReceiptPrintDocumentFactory
{
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;
    private const double PageMargin = 48;

    public static FixedDocument Build(StockReceiptDto receipt)
    {
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

        var company = new TextBlock { Text = "ASMILE BO", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brush("#0F172A") };
        Grid.SetRow(company, 0);
        content.Children.Add(company);

        var title = new TextBlock
        {
            Text = "PHIẾU NHẬP KHO",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#0F172A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 16)
        };
        Grid.SetRow(title, 1);
        content.Children.Add(title);

        var infoGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddInfo(infoGrid, 0, 0, "Nhà cung cấp:", receipt.SupplierName, 2, true);
        AddInfo(infoGrid, 1, 0, "Số phiếu:", receipt.ReceiptNo);
        AddInfo(infoGrid, 1, 1, "Ngày chứng từ:", receipt.ReceiptDate.ToString("dd/MM/yyyy"));
        AddInfo(infoGrid, 2, 0, "Người nhận hàng:", receipt.ReceiverName);
        AddInfo(infoGrid, 2, 1, "Tổng tiền:", receipt.TotalAmount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")));
        Grid.SetRow(infoGrid, 2);
        content.Children.Add(infoGrid);

        var table = BuildDetailsTable(receipt);
        Grid.SetRow(table, 3);
        content.Children.Add(table);

        var signGrid = new Grid { Margin = new Thickness(0, 28, 0, 0) };
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signGrid.Children.Add(SignBox("Người lập phiếu"));
        var signMiddle = SignBox("Người giao hàng");
        Grid.SetColumn(signMiddle, 1);
        signGrid.Children.Add(signMiddle);
        var signRight = SignBox("Người nhận hàng");
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

    private static UIElement BuildDetailsTable(StockReceiptDto receipt)
    {
        var widths = new[] { 40d, 85d, 190d, 65d, 60d, 75d, 72d, 106d };
        var tableWidth = widths.Sum() + 2;
        var outer = new Border
        {
            BorderBrush = Brush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = tableWidth
        };

        var grid = new Grid();
        foreach (var w in widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        for (var i = 0; i < receipt.Details.Count; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

        var headers = new[] { "STT", "Mã hàng", "Tên hàng hóa", "ĐV tính", "Kho", "Số lượng", "Đơn giá", "Thành tiền" };
        for (var c = 0; c < headers.Length; c++)
        {
            var header = Cell(headers[c], true, TextAlignment.Center);
            header.Background = Brush("#F1F5F9");
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c);
            grid.Children.Add(header);
        }

        for (var i = 0; i < receipt.Details.Count; i++)
        {
            var d = receipt.Details[i];
            var row = i + 1;
            var cells = new[]
            {
                d.LineNo.ToString(),
                d.ItemCode,
                d.ItemName,
                d.Unit,
                d.WarehouseCode,
                d.Quantity.ToString("#,##0.####", CultureInfo.GetCultureInfo("vi-VN")),
                d.UnitPrice.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")),
                d.Amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
            };

            for (var c = 0; c < cells.Length; c++)
            {
                var align = c <= 4 ? TextAlignment.Left : TextAlignment.Right;
                if (c == 0) align = TextAlignment.Center;
                var cell = Cell(cells[c], false, align);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }

        outer.Child = grid;
        return outer;
    }

    private static Border Cell(string text, bool isHeader, TextAlignment align) => new()
    {
        BorderBrush = Brush("#CBD5E1"),
        BorderThickness = new Thickness(0, 0, 1, 1),
        Padding = new Thickness(6, 4, 6, 4),
        Child = new TextBlock
        {
            Text = text,
            FontSize = isHeader ? 12.5 : 12,
            FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#0F172A"),
            TextWrapping = TextWrapping.Wrap
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
        while (infoGrid.RowDefinitions.Count <= row) infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
