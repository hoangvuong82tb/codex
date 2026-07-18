using ASMILE.BO.Application.DTOs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public static class StockIssuePrintDocumentFactory
{
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;
    private const double PageMargin = 42;

    public static FixedDocument Build(StockIssueDto issue)
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
            Text = "PHIẾU XUẤT KHO",
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
        AddInfo(infoGrid, 0, 0, "Diễn giải:", issue.Description, 2, true);
        AddInfo(infoGrid, 1, 0, "Số phiếu:", issue.IssueNo);
        AddInfo(infoGrid, 1, 1, "Ngày chứng từ:", issue.IssueDate.ToString("dd/MM/yyyy"));
        AddInfo(infoGrid, 2, 0, "Kho xuất:", issue.WarehouseCode);
        AddInfo(infoGrid, 2, 1, "Kho nhận:", issue.ToWarehouseCode);
        AddInfo(infoGrid, 3, 0, "Bộ phận:", issue.DepartmentCode);
        AddInfo(infoGrid, 3, 1, "Tổng tiền:", issue.TotalAmount.ToString("N0", viVn), 1, true);
        Grid.SetRow(infoGrid, 2);
        content.Children.Add(infoGrid);

        var table = BuildDetailsTable(issue, viVn);
        Grid.SetRow(table, 3);
        content.Children.Add(table);

        var signGrid = new Grid { Margin = new Thickness(0, 24, 0, 0) };
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

    private static UIElement BuildDetailsTable(StockIssueDto issue, CultureInfo culture)
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
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });  // STT
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });  // Mã hàng
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Tên hàng hóa
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });  // ĐV tính
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });  // KX
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });  // KN
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });  // Số lượng
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });  // Đơn giá
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) }); // Thành tiền

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        for (var i = 0; i < issue.Details.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        }

        var headers = new[] { "STT", "Mã hàng", "Tên hàng hóa", "ĐV tính", "KX", "KN", "Số lượng", "Đơn giá", "Thành tiền" };
        for (var c = 0; c < headers.Length; c++)
        {
            var header = Cell(headers[c], true, TextAlignment.Center);
            header.Background = Brush("#F1F5F9");
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c);
            grid.Children.Add(header);
        }

        for (var i = 0; i < issue.Details.Count; i++)
        {
            var d = issue.Details[i];
            var row = i + 1;
            var cells = new[]
            {
                d.LineNo.ToString(culture),
                d.ItemCode,
                d.ItemName,
                d.Unit,
                d.WarehouseCode,
                issue.ToWarehouseCode,
                d.Quantity.ToString("#,##0.####", culture),
                d.UnitPrice.ToString("N0", culture),
                d.Amount.ToString("N0", culture)
            };

            for (var c = 0; c < cells.Length; c++)
            {
                var align = c <= 5 ? TextAlignment.Left : TextAlignment.Right;
                if (c == 0)
                {
                    align = TextAlignment.Center;
                }

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
        Padding = new Thickness(4, 4, 4, 4),
        Child = new TextBlock
        {
            Text = text ?? string.Empty,
            FontSize = isHeader ? 12.5 : 12,
            FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#0F172A"),
            TextWrapping = TextWrapping.NoWrap
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
