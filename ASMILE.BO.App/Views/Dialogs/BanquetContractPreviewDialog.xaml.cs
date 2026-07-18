using ASMILE.BO.Application.DTOs;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class BanquetContractPreviewDialog : Window
{
    private readonly BanquetContractDto _contract;
    private readonly BanquetEventDto _event;
    private readonly IReadOnlyList<BanquetEventMenuDto> _menus;
    private readonly IReadOnlyList<BanquetServiceLineDto> _services;

    public BanquetContractPreviewDialog(BanquetContractDto contract, BanquetEventDto evt, IEnumerable<BanquetEventMenuDto> menus, IEnumerable<BanquetServiceLineDto> services)
    {
        _contract = contract;
        _event = evt;
        _menus = menus.ToArray();
        _services = services.ToArray();
        InitializeComponent();
        BuildPreviewVisual();
    }

    private void BuildPreviewVisual()
    {
        PreviewPanel.Children.Clear();
        AddPreviewText("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", 15, FontWeights.Bold, TextAlignment.Center);
        AddPreviewText("Độc lập - Tự do - Hạnh phúc", 13, FontWeights.Bold, TextAlignment.Center);
        AddPreviewText("____________________", 11, FontWeights.Normal, TextAlignment.Center, new Thickness(0, 0, 0, 18));
        AddPreviewText("HỢP ĐỒNG CUNG CẤP DỊCH VỤ TIỆC", 20, FontWeights.Bold, TextAlignment.Center);
        AddPreviewText($"Số: {_contract.ContractNo}", 12, FontWeights.Normal, TextAlignment.Center, new Thickness(0, 3, 0, 18));
        AddPreviewText($"Ngày lập: {(_contract.SignDate ?? DateTime.Today):dd/MM/yyyy}", 12, FontWeights.Normal, TextAlignment.Right, new Thickness(0, 0, 0, 14));
        AddPreviewText("Căn cứ Bộ luật Dân sự năm 2015 và nhu cầu, khả năng của các bên; hai bên thống nhất giao kết hợp đồng với các điều khoản sau:", 12, FontWeights.Normal, TextAlignment.Justify, new Thickness(0, 0, 0, 8), FontStyles.Italic);

        AddSection("I. THÔNG TIN CÁC BÊN");
        AddInfoTable(new[]
        {
            ("BÊN A", "Đơn vị cung cấp dịch vụ - thông tin pháp lý theo hồ sơ doanh nghiệp đang cấu hình"),
            ("BÊN B", _event.CustomerName ?? "Chưa cập nhật"),
            ("Điện thoại", _event.Phone),
            ("Mã sự kiện", _event.EventNo)
        });

        AddSection("II. THÔNG TIN SỰ KIỆN");
        AddInfoTable(new[]
        {
            ("Loại sự kiện", _event.EventTypeDisplay),
            ("Thời gian", $"{_event.StartTime:hh\\:mm} - {_event.EndTime:hh\\:mm}, ngày {_event.EventDate:dd/MM/yyyy}"),
            ("Địa điểm", _event.RoomName ?? "Theo thỏa thuận"),
            ("Quy mô", $"{_event.GuestCount} khách; {_event.TableCount} bàn chính; {_event.ExtraTableCount} bàn dự kiến phát sinh")
        });

        AddSection("III. THỰC ĐƠN");
        AddDetailTable("Tên món", _menus.Select((x, i) => (i + 1, x.ItemName ?? x.PackageName ?? "Món ăn", x.Qty, x.UnitPrice, x.Amount)).ToArray(), "Chưa có thực đơn.");
        AddSection("IV. DỊCH VỤ BỔ SUNG");
        AddDetailTable("Dịch vụ", _services.Select((x, i) => (i + 1, x.Description, x.Qty, x.UnitPrice, x.Amount)).ToArray(), "Chưa có dịch vụ bổ sung.");

        AddSection("V. GIÁ TRỊ VÀ THANH TOÁN");
        AddInfoTable(new[]
        {
            ("Tổng giá trị hợp đồng", $"{_contract.TotalAmount:N0} đ"),
            ("Đã đặt cọc", $"{_contract.DepositAmount:N0} đ"),
            ("Còn phải thanh toán", $"{_contract.RemainingAmount:N0} đ")
        }, true);

        AddSection("VI. QUYỀN, NGHĨA VỤ VÀ HIỆU LỰC");
        AddPreviewText("Bên A cung cấp dịch vụ đúng nội dung, chất lượng và thời gian đã thỏa thuận. Bên B cung cấp thông tin chính xác, phối hợp tổ chức và thanh toán đầy đủ, đúng hạn.", 12, FontWeights.Normal, TextAlignment.Justify);
        AddPreviewText("Mọi thay đổi phải được xác nhận bằng văn bản hoặc thông điệp dữ liệu có thể truy xuất. Tranh chấp được ưu tiên giải quyết bằng thương lượng; nếu không thành, các bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật Việt Nam.", 12, FontWeights.Normal, TextAlignment.Justify);
        if (!string.IsNullOrWhiteSpace(_contract.Note))
        {
            AddSection("VII. ĐIỀU KHOẢN BỔ SUNG");
            AddPreviewText(_contract.Note.Trim(), 12, FontWeights.Normal, TextAlignment.Justify);
        }

        var signatures = new Grid { Margin = new Thickness(0, 30, 0, 0) };
        signatures.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signatures.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        signatures.Children.Add(SignatureBlock("ĐẠI DIỆN BÊN A", 0));
        signatures.Children.Add(SignatureBlock("ĐẠI DIỆN BÊN B", 1));
        PreviewPanel.Children.Add(signatures);
    }

    private void AddSection(string text) => AddPreviewText(text, 13, FontWeights.Bold, TextAlignment.Left, new Thickness(0, 16, 0, 6));

    private void AddPreviewText(string text, double size, FontWeight weight, TextAlignment alignment, Thickness? margin = null, FontStyle? style = null)
    {
        PreviewPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = size,
            FontWeight = weight,
            FontStyle = style ?? FontStyles.Normal,
            TextAlignment = alignment,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20,
            Margin = margin ?? new Thickness(0, 3, 0, 3),
            Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
        });
    }

    private void AddInfoTable(IEnumerable<(string Label, string Value)> rows, bool emphasizeLast = false)
    {
        var wrapper = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var allRows = rows.ToArray();
        for (var i = 0; i < allRows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var label = PreviewCell(allRows[i].Label, FontWeights.SemiBold, TextAlignment.Left, new SolidColorBrush(Color.FromRgb(248, 250, 252)));
            Grid.SetRow(label, i); Grid.SetColumn(label, 0); grid.Children.Add(label);
            var value = PreviewCell(allRows[i].Value, emphasizeLast && i == allRows.Length - 1 ? FontWeights.Bold : FontWeights.Normal, TextAlignment.Left, Brushes.White);
            Grid.SetRow(value, i); Grid.SetColumn(value, 1); grid.Children.Add(value);
        }
        wrapper.Child = grid; PreviewPanel.Children.Add(wrapper);
    }

    private void AddDetailTable(string itemHeader, IReadOnlyList<(int Index, string Name, decimal Qty, decimal UnitPrice, decimal Amount)> rows, string emptyText)
    {
        var wrapper = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8) };
        var grid = new Grid();
        foreach (var width in new[] { 44d, 1d, 70d, 110d, 120d }) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = width == 1d ? new GridLength(1, GridUnitType.Star) : new GridLength(width) });
        var headers = new[] { "STT", itemHeader, "SL", "Đơn giá", "Thành tiền" };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var col = 0; col < headers.Length; col++) { var cell = PreviewCell(headers[col], FontWeights.Bold, col >= 2 ? TextAlignment.Right : TextAlignment.Center, new SolidColorBrush(Color.FromRgb(234, 241, 249))); Grid.SetColumn(cell, col); grid.Children.Add(cell); }
        if (rows.Count == 0)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var empty = PreviewCell(emptyText, FontWeights.Normal, TextAlignment.Center, Brushes.White); Grid.SetRow(empty, 1); Grid.SetColumn(empty, 0); Grid.SetColumnSpan(empty, 5); grid.Children.Add(empty);
        }
        else
        {
            for (var row = 0; row < rows.Count; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var values = new[] { rows[row].Index.ToString(), rows[row].Name, rows[row].Qty.ToString("0.##"), $"{rows[row].UnitPrice:N0}", $"{rows[row].Amount:N0}" };
                for (var col = 0; col < values.Length; col++) { var cell = PreviewCell(values[col], FontWeights.Normal, col >= 2 ? TextAlignment.Right : TextAlignment.Left, row % 2 == 1 ? new SolidColorBrush(Color.FromRgb(248, 250, 252)) : Brushes.White); Grid.SetRow(cell, row + 1); Grid.SetColumn(cell, col); grid.Children.Add(cell); }
            }
        }
        wrapper.Child = grid; PreviewPanel.Children.Add(wrapper);
    }

    private static Border PreviewCell(string text, FontWeight weight, TextAlignment alignment, Brush background)
        => new() { Background = background, BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), BorderThickness = new Thickness(.5), Padding = new Thickness(7, 5, 7, 5), Child = new TextBlock { Text = text, FontFamily = new FontFamily("Times New Roman"), FontSize = 12, FontWeight = weight, TextAlignment = alignment, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) } };

    private static StackPanel SignatureBlock(string title, int column)
    {
        var panel = new StackPanel { Margin = column == 0 ? new Thickness(0, 0, 18, 0) : new Thickness(18, 0, 0, 0) };
        panel.Children.Add(new TextBlock { Text = title, FontFamily = new FontFamily("Times New Roman"), FontSize = 13, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        panel.Children.Add(new TextBlock { Text = "(Ký, ghi rõ họ tên)", FontFamily = new FontFamily("Times New Roman"), FontSize = 11, FontStyle = FontStyles.Italic, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 78) });
        Grid.SetColumn(panel, column); return panel;
    }

    private FlowDocument BuildDocument()
    {
        var document = new FlowDocument
        {
            PageWidth = 720,
            PagePadding = new Thickness(44, 38, 44, 44),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
        };

        document.Blocks.Add(Centered("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", 15, FontWeights.Bold));
        document.Blocks.Add(Centered("Độc lập - Tự do - Hạnh phúc", 13, FontWeights.Bold));
        document.Blocks.Add(Centered("____________________", 12, FontWeights.Normal));
        document.Blocks.Add(Centered("HỢP ĐỒNG CUNG CẤP DỊCH VỤ TIỆC", 20, FontWeights.Bold, new Thickness(0, 18, 0, 3)));
        document.Blocks.Add(Centered($"Số: {_contract.ContractNo}", 12, FontWeights.Normal, new Thickness(0, 0, 0, 18)));

        document.Blocks.Add(Paragraph("Căn cứ Bộ luật Dân sự năm 2015 và nhu cầu, khả năng của các bên;", italic: true));
        document.Blocks.Add(Paragraph($"Hôm nay, ngày {(_contract.SignDate ?? DateTime.Today):dd/MM/yyyy}, các bên thống nhất giao kết hợp đồng với các nội dung sau:"));
        document.Blocks.Add(Heading("I. THÔNG TIN CÁC BÊN"));
        document.Blocks.Add(KeyValueTable(new[]
        {
            ("BÊN A", "Đơn vị cung cấp dịch vụ - thông tin pháp lý theo hồ sơ doanh nghiệp đang cấu hình"),
            ("BÊN B", _event.CustomerName ?? "Chưa cập nhật"),
            ("Điện thoại", _event.Phone),
            ("Mã sự kiện", _event.EventNo)
        }));

        document.Blocks.Add(Heading("II. THÔNG TIN SỰ KIỆN"));
        document.Blocks.Add(KeyValueTable(new[]
        {
            ("Loại sự kiện", _event.EventTypeDisplay),
            ("Thời gian", $"{_event.StartTime:hh\\:mm} - {_event.EndTime:hh\\:mm}, ngày {_event.EventDate:dd/MM/yyyy}"),
            ("Địa điểm", _event.RoomName ?? "Theo thỏa thuận"),
            ("Quy mô", $"{_event.GuestCount} khách, {_event.TableCount} bàn chính; {_event.ExtraTableCount} bàn dự kiến phát sinh")
        }));

        document.Blocks.Add(Heading("III. THỰC ĐƠN"));
        document.Blocks.Add(BuildMenuTable());
        document.Blocks.Add(Heading("IV. DỊCH VỤ BỔ SUNG"));
        document.Blocks.Add(BuildServiceTable());
        document.Blocks.Add(Heading("V. GIÁ TRỊ VÀ THANH TOÁN"));
        document.Blocks.Add(BuildPaymentTable());
        document.Blocks.Add(Heading("VI. QUYỀN, NGHĨA VỤ VÀ GIẢI QUYẾT TRANH CHẤP"));
        document.Blocks.Add(Paragraph("Bên A cung cấp dịch vụ đúng nội dung, chất lượng và thời gian đã thỏa thuận. Bên B cung cấp thông tin chính xác, phối hợp tổ chức và thanh toán đầy đủ, đúng hạn."));
        document.Blocks.Add(Paragraph("Mọi thay đổi phải được xác nhận bằng văn bản hoặc thông điệp dữ liệu có thể truy xuất. Tranh chấp được ưu tiên giải quyết bằng thương lượng; nếu không thành, các bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật Việt Nam."));
        document.Blocks.Add(Paragraph("Hợp đồng có hiệu lực từ ngày ký hoặc xác nhận hợp lệ của hai bên. Giá trị cuối cùng được đối chiếu theo số lượng và dịch vụ thực tế phát sinh."));

        if (!string.IsNullOrWhiteSpace(_contract.Note))
        {
            document.Blocks.Add(Heading("VII. ĐIỀU KHOẢN BỔ SUNG"));
            document.Blocks.Add(Paragraph(_contract.Note.Trim()));
        }

        document.Blocks.Add(BuildSignatureTable());
        return document;
    }

    private Table BuildMenuTable()
    {
        var table = CreateTable(new[] { .55d, 4.2d, .8d, 1.35d, 1.55d }, new[] { "STT", "Tên món", "SL", "Đơn giá", "Thành tiền" });
        if (_menus.Count == 0) AddRow(table, new[] { "", "Chưa có thực đơn", "", "", "" });
        else for (var i = 0; i < _menus.Count; i++) AddRow(table, new[] { (i + 1).ToString(), _menus[i].ItemName ?? _menus[i].PackageName ?? "Món ăn", _menus[i].Qty.ToString("0.##"), $"{_menus[i].UnitPrice:N0}", $"{_menus[i].Amount:N0}" }, i % 2 == 1);
        return table;
    }

    private Table BuildServiceTable()
    {
        var table = CreateTable(new[] { .55d, 4.2d, .8d, 1.35d, 1.55d }, new[] { "STT", "Dịch vụ", "SL", "Đơn giá", "Thành tiền" });
        if (_services.Count == 0) AddRow(table, new[] { "", "Chưa có dịch vụ bổ sung", "", "", "" });
        else for (var i = 0; i < _services.Count; i++) AddRow(table, new[] { (i + 1).ToString(), _services[i].Description, _services[i].Qty.ToString("0.##"), $"{_services[i].UnitPrice:N0}", $"{_services[i].Amount:N0}" }, i % 2 == 1);
        return table;
    }

    private Table BuildPaymentTable()
        => KeyValueTable(new[] { ("Tổng giá trị hợp đồng", $"{_contract.TotalAmount:N0} đ"), ("Đã đặt cọc", $"{_contract.DepositAmount:N0} đ"), ("Còn phải thanh toán", $"{_contract.RemainingAmount:N0} đ") });

    private static Table BuildSignatureTable()
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 28, 0, 0) };
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        var row = new TableRow();
        row.Cells.Add(SignatureCell("ĐẠI DIỆN BÊN A"));
        row.Cells.Add(SignatureCell("ĐẠI DIỆN BÊN B"));
        group.Rows.Add(row); table.RowGroups.Add(group); return table;
    }

    private static TableCell SignatureCell(string title)
        => new(new Paragraph(new Run(title)) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold }) { Padding = new Thickness(10, 6, 10, 90) };

    private static Table KeyValueTable(IEnumerable<(string Key, string Value)> rows)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(180) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        foreach (var (key, value) in rows)
        {
            var row = new TableRow();
            row.Cells.Add(Cell(key, FontWeights.SemiBold)); row.Cells.Add(Cell(value)); group.Rows.Add(row);
        }
        table.RowGroups.Add(group); return table;
    }

    private static Table CreateTable(IEnumerable<double> widths, IEnumerable<string> headers)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 5, 0, 12) };
        foreach (var width in widths) table.Columns.Add(new TableColumn { Width = new GridLength(width, GridUnitType.Star) });
        var group = new TableRowGroup(); var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(235, 241, 249)) };
        foreach (var text in headers) header.Cells.Add(Cell(text, FontWeights.Bold, TextAlignment.Center));
        group.Rows.Add(header); table.RowGroups.Add(group); return table;
    }

    private static void AddRow(Table table, IReadOnlyList<string> values, bool alternate = false)
    {
        var row = new TableRow { Background = alternate ? new SolidColorBrush(Color.FromRgb(248, 250, 252)) : Brushes.White };
        for (var i = 0; i < values.Count; i++) row.Cells.Add(Cell(values[i], FontWeights.Normal, i >= 2 ? TextAlignment.Right : TextAlignment.Left));
        table.RowGroups[0].Rows.Add(row);
    }

    private static TableCell Cell(string text, FontWeight? weight = null, TextAlignment alignment = TextAlignment.Left)
        => new(new Paragraph(new Run(text)) { Margin = new Thickness(0), FontWeight = weight ?? FontWeights.Normal, TextAlignment = alignment }) { Padding = new Thickness(7, 5, 7, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), BorderThickness = new Thickness(0.5) };

    private static Paragraph Heading(string text) => new(new Run(text)) { FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 13, 0, 5) };
    private static Paragraph Paragraph(string text, bool italic = false) => new(new Run(text)) { TextAlignment = TextAlignment.Justify, FontStyle = italic ? FontStyles.Italic : FontStyles.Normal, Margin = new Thickness(0, 4, 0, 4), LineHeight = 20 };
    private static Paragraph Centered(string text, double size, FontWeight weight, Thickness? margin = null) => new(new Run(text)) { TextAlignment = TextAlignment.Center, FontSize = size, FontWeight = weight, Margin = margin ?? new Thickness(0, 2, 0, 2) };

    private void SaveExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Excel Workbook|*.xlsx", FileName = $"HopDong_{_contract.ContractNo}.xlsx" };
        if (dialog.ShowDialog(this) != true) return;
        using var workbook = new XLWorkbook();
        BuildContractWorksheet(workbook);
        BuildAppendixWorksheet(workbook);
        workbook.SaveAs(dialog.FileName);
        MessageBox.Show(this, "Đã lưu hợp đồng Excel thành công.", "Hợp đồng", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BuildContractWorksheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Hợp đồng");
        sheet.Style.Font.FontName = "Times New Roman";
        sheet.Style.Font.FontSize = 11;
        sheet.Column("A").Width = 7;
        sheet.Column("B").Width = 28;
        sheet.Column("C").Width = 15;
        sheet.Column("D").Width = 18;
        sheet.Column("E").Width = 20;
        var row = 1;
        WriteMergedTitle(sheet, ref row, "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", 14);
        WriteMergedTitle(sheet, ref row, "Độc lập - Tự do - Hạnh phúc", 12);
        WriteMergedTitle(sheet, ref row, "____________________", 10);
        row++;
        WriteMergedTitle(sheet, ref row, "HỢP ĐỒNG CUNG CẤP DỊCH VỤ TIỆC", 17);
        WriteMergedTitle(sheet, ref row, $"Số: {_contract.ContractNo}", 11);
        WriteMergedTitle(sheet, ref row, $"Ngày lập: {(_contract.SignDate ?? DateTime.Today):dd/MM/yyyy}", 11);
        row++;
        WriteMergedParagraph(sheet, ref row, "Căn cứ Bộ luật Dân sự năm 2015 và nhu cầu, khả năng của các bên; hai bên thống nhất giao kết hợp đồng với các điều khoản sau:", italic: true);

        WriteSection(sheet, ref row, "I. THÔNG TIN CÁC BÊN");
        WriteKeyValue(sheet, ref row, "BÊN A", "Đơn vị cung cấp dịch vụ - thông tin pháp lý theo hồ sơ doanh nghiệp đang cấu hình");
        WriteKeyValue(sheet, ref row, "BÊN B", _event.CustomerName ?? "Chưa cập nhật");
        WriteKeyValue(sheet, ref row, "Điện thoại", _event.Phone);
        WriteKeyValue(sheet, ref row, "Mã sự kiện", _event.EventNo);

        WriteSection(sheet, ref row, "II. ĐỐI TƯỢNG VÀ THÔNG TIN SỰ KIỆN");
        WriteKeyValue(sheet, ref row, "Loại sự kiện", _event.EventTypeDisplay);
        WriteKeyValue(sheet, ref row, "Thời gian", $"{_event.StartTime:hh\\:mm} - {_event.EndTime:hh\\:mm}, ngày {_event.EventDate:dd/MM/yyyy}");
        WriteKeyValue(sheet, ref row, "Địa điểm", _event.RoomName ?? "Theo thỏa thuận");
        WriteKeyValue(sheet, ref row, "Quy mô", $"{_event.GuestCount} khách; {_event.TableCount} bàn chính; {_event.ExtraGuestCount} khách và {_event.ExtraTableCount} bàn dự kiến phát sinh");
        if (!string.IsNullOrWhiteSpace(_event.Note)) WriteKeyValue(sheet, ref row, "Yêu cầu sự kiện", _event.Note);

        WriteSection(sheet, ref row, "III. THỰC ĐƠN");
        var menuHeaderRow = row;
        WriteExcelTableHeader(sheet, row++, new[] { "STT", "Tên món", "Số lượng", "Đơn giá", "Thành tiền" });
        if (_menus.Count == 0) WriteEmptyTableRow(sheet, row++, "Chưa có thực đơn");
        for (var i = 0; i < _menus.Count; i++) WriteExcelRow(sheet, row++, i + 1, _menus[i].ItemName ?? _menus[i].PackageName ?? "Món ăn", _menus[i].Qty, _menus[i].UnitPrice, _menus[i].Amount);
        WriteTableTotal(sheet, ref row, "Tổng thực đơn", _menus.Sum(x => x.Amount));

        WriteSection(sheet, ref row, "IV. DỊCH VỤ BỔ SUNG");
        var serviceHeaderRow = row;
        WriteExcelTableHeader(sheet, row++, new[] { "STT", "Dịch vụ", "Số lượng", "Đơn giá", "Thành tiền" });
        if (_services.Count == 0) WriteEmptyTableRow(sheet, row++, "Chưa có dịch vụ bổ sung");
        for (var i = 0; i < _services.Count; i++) WriteExcelRow(sheet, row++, i + 1, _services[i].Description, _services[i].Qty, _services[i].UnitPrice, _services[i].Amount);
        WriteTableTotal(sheet, ref row, "Tổng dịch vụ", _services.Sum(x => x.Amount));

        WriteSection(sheet, ref row, "V. GIÁ TRỊ VÀ THANH TOÁN");
        WriteMoneyRow(sheet, ref row, "Tổng giá trị hợp đồng", _contract.TotalAmount, true);
        WriteMoneyRow(sheet, ref row, "Đã đặt cọc", _contract.DepositAmount);
        WriteMoneyRow(sheet, ref row, "Còn phải thanh toán", _contract.RemainingAmount, true);
        WriteMergedParagraph(sheet, ref row, "Giá trị cuối cùng được đối chiếu theo số lượng và dịch vụ thực tế phát sinh, có xác nhận của hai bên.");

        WriteSection(sheet, ref row, "VI. QUYỀN VÀ NGHĨA VỤ CÁC BÊN");
        WriteClause(sheet, ref row, "6.1", "Bên A cung cấp dịch vụ đúng nội dung, chất lượng, thời gian đã thỏa thuận và bảo đảm an toàn trong phạm vi trách nhiệm.");
        WriteClause(sheet, ref row, "6.2", "Bên B cung cấp thông tin chính xác, phối hợp tổ chức và thanh toán đầy đủ, đúng hạn.");
        WriteClause(sheet, ref row, "6.3", "Hai bên có trách nhiệm thông báo kịp thời các thay đổi có thể ảnh hưởng đến việc thực hiện hợp đồng.");

        WriteSection(sheet, ref row, "VII. THAY ĐỔI, HỦY DỊCH VỤ VÀ BẤT KHẢ KHÁNG");
        WriteClause(sheet, ref row, "7.1", "Mọi thay đổi phải được xác nhận bằng văn bản hoặc thông điệp dữ liệu có thể truy xuất.");
        WriteClause(sheet, ref row, "7.2", "Trách nhiệm hoàn hoặc hủy cọc và các chi phí phát sinh thực hiện theo thỏa thuận bổ sung và quy định pháp luật.");
        WriteClause(sheet, ref row, "7.3", "Bên bị ảnh hưởng bởi sự kiện bất khả kháng phải thông báo kịp thời và phối hợp hạn chế thiệt hại.");

        WriteSection(sheet, ref row, "VIII. GIẢI QUYẾT TRANH CHẤP VÀ HIỆU LỰC");
        WriteClause(sheet, ref row, "8.1", "Tranh chấp được ưu tiên giải quyết bằng thương lượng; nếu không đạt thỏa thuận, các bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật Việt Nam.");
        WriteClause(sheet, ref row, "8.2", "Hợp đồng có hiệu lực từ ngày ký hoặc xác nhận hợp lệ của hai bên và được lập thành các bản có giá trị pháp lý như nhau.");

        if (!string.IsNullOrWhiteSpace(_contract.Note))
        {
            WriteSection(sheet, ref row, "IX. ĐIỀU KHOẢN BỔ SUNG");
            WriteMergedParagraph(sheet, ref row, _contract.Note.Trim(), minHeight: 54);
        }

        row += 2;
        var signature = sheet.Range(row, 1, row, 5);
        signature.Style.Font.Bold = true;
        sheet.Range(row, 1, row, 2).Merge().Value = "ĐẠI DIỆN BÊN A";
        sheet.Range(row, 4, row, 5).Merge().Value = "ĐẠI DIỆN BÊN B";
        sheet.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Range(row, 4, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;
        sheet.Range(row, 1, row, 2).Merge().Value = "(Ký, ghi rõ họ tên, đóng dấu nếu có)";
        sheet.Range(row, 4, row, 5).Merge().Value = "(Ký, ghi rõ họ tên)";
        sheet.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Range(row, 4, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Row(row + 1).Height = 72;

        sheet.SheetView.FreezeRows(menuHeaderRow - 1);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.Margins.Top = .55;
        sheet.PageSetup.Margins.Bottom = .55;
        sheet.PageSetup.Margins.Left = .45;
        sheet.PageSetup.Margins.Right = .45;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 7);
        sheet.PageSetup.PrintAreas.Add(1, 1, row + 2, 5);
        var usedRange = sheet.RangeUsed();
        if (usedRange is not null) usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        _ = serviceHeaderRow;
    }

    private void BuildAppendixWorksheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Phụ lục chi tiết");
        sheet.Style.Font.FontName = "Segoe UI";
        sheet.Style.Font.FontSize = 10;
        sheet.Column("A").Width = 8;
        sheet.Column("B").Width = 36;
        sheet.Column("C").Width = 18;
        sheet.Column("D").Width = 14;
        sheet.Column("E").Width = 18;
        sheet.Column("F").Width = 20;
        var row = 1;
        var title = sheet.Range(row, 1, row, 6).Merge();
        title.Value = $"PHỤ LỤC HỢP ĐỒNG {_contract.ContractNo}";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row += 2;
        WriteAppendixHeader(sheet, row++, new[] { "STT", "Nội dung", "Phân loại", "Số lượng", "Đơn giá", "Thành tiền" });
        var index = 1;
        foreach (var menu in _menus)
        {
            WriteAppendixRow(sheet, row++, index++, menu.ItemName ?? menu.PackageName ?? "Món ăn", "Thực đơn", menu.Qty, menu.UnitPrice, menu.Amount);
        }
        foreach (var service in _services)
        {
            WriteAppendixRow(sheet, row++, index++, service.Description, "Dịch vụ", service.Qty, service.UnitPrice, service.Amount);
        }
        var total = sheet.Range(row, 1, row, 5).Merge();
        total.Value = "TỔNG CỘNG";
        total.Style.Font.Bold = true;
        total.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        sheet.Cell(row, 6).Value = _menus.Sum(x => x.Amount) + _services.Sum(x => x.Amount);
        sheet.Cell(row, 6).Style.Font.Bold = true;
        sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]";
        sheet.Range(row, 1, row, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.SheetView.FreezeRows(3);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(1, 0);
    }

    private static void WriteMergedTitle(IXLWorksheet sheet, ref int row, string text, double size) { var range = sheet.Range(row, 1, row, 5).Merge(); range.Value = text; range.Style.Font.Bold = true; range.Style.Font.FontSize = size; range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; sheet.Row(row).Height = size + 7; row++; }
    private static void WriteMergedParagraph(IXLWorksheet sheet, ref int row, string text, bool italic = false, double minHeight = 34) { var range = sheet.Range(row, 1, row, 5).Merge(); range.Value = text; range.Style.Alignment.WrapText = true; range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Justify; range.Style.Font.Italic = italic; sheet.Row(row).Height = minHeight; row++; }
    private static void WriteSection(IXLWorksheet sheet, ref int row, string text) { row++; var range = sheet.Range(row, 1, row, 5).Merge(); range.Value = text; range.Style.Font.Bold = true; range.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE8F6"); range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; sheet.Row(row).Height = 24; row++; }
    private static void WriteKeyValue(IXLWorksheet sheet, ref int row, string key, string value) { sheet.Cell(row, 1).Value = key; sheet.Cell(row, 1).Style.Font.Bold = true; sheet.Range(row, 2, row, 5).Merge().Value = value; sheet.Range(row, 1, row, 5).Style.Border.BottomBorder = XLBorderStyleValues.Hair; sheet.Range(row, 2, row, 5).Style.Alignment.WrapText = true; sheet.Row(row).Height = 22; row++; }
    private static void WriteMoneyRow(IXLWorksheet sheet, ref int row, string key, decimal value, bool bold = false) { sheet.Range(row, 1, row, 4).Merge().Value = key; sheet.Cell(row, 5).Value = value; sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]"; sheet.Range(row, 1, row, 5).Style.Border.BottomBorder = XLBorderStyleValues.Thin; sheet.Range(row, 1, row, 5).Style.Font.Bold = bold; row++; }
    private static void WriteClause(IXLWorksheet sheet, ref int row, string number, string text) { sheet.Cell(row, 1).Value = number; sheet.Cell(row, 1).Style.Font.Bold = true; sheet.Range(row, 2, row, 5).Merge().Value = text; sheet.Range(row, 2, row, 5).Style.Alignment.WrapText = true; sheet.Range(row, 2, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Justify; sheet.Row(row).AdjustToContents(30, 90); row++; }
    private static void WriteExcelTableHeader(IXLWorksheet sheet, int row, IReadOnlyList<string> headers) { for (var i = 0; i < headers.Count; i++) sheet.Cell(row, i + 1).Value = headers[i]; var range = sheet.Range(row, 1, row, headers.Count); range.Style.Font.Bold = true; range.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF1F9"); range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; range.Style.Border.InsideBorder = XLBorderStyleValues.Thin; sheet.Row(row).Height = 22; }
    private static void WriteExcelRow(IXLWorksheet sheet, int row, int index, string name, decimal qty, decimal price, decimal amount) { sheet.Cell(row, 1).Value = index; sheet.Cell(row, 2).Value = name; sheet.Cell(row, 3).Value = qty; sheet.Cell(row, 4).Value = price; sheet.Cell(row, 5).Value = amount; sheet.Range(row, 3, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; sheet.Range(row, 4, row, 5).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]"; sheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; sheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin; sheet.Range(row, 1, row, 5).Style.Alignment.WrapText = true; sheet.Row(row).Height = 21; }
    private static void WriteEmptyTableRow(IXLWorksheet sheet, int row, string text) { var range = sheet.Range(row, 1, row, 5).Merge(); range.Value = text; range.Style.Font.Italic = true; range.Style.Font.FontColor = XLColor.Gray; range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; }
    private static void WriteTableTotal(IXLWorksheet sheet, ref int row, string label, decimal total) { sheet.Range(row, 1, row, 4).Merge().Value = label; sheet.Range(row, 1, row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; sheet.Range(row, 1, row, 5).Style.Font.Bold = true; sheet.Cell(row, 5).Value = total; sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]"; sheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; row++; }
    private static void WriteAppendixHeader(IXLWorksheet sheet, int row, IReadOnlyList<string> headers) { for (var i = 0; i < headers.Count; i++) sheet.Cell(row, i + 1).Value = headers[i]; var range = sheet.Range(row, 1, row, headers.Count); range.Style.Font.Bold = true; range.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE8F6"); range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; range.Style.Border.InsideBorder = XLBorderStyleValues.Thin; }
    private static void WriteAppendixRow(IXLWorksheet sheet, int row, int index, string name, string category, decimal qty, decimal price, decimal amount) { sheet.Cell(row, 1).Value = index; sheet.Cell(row, 2).Value = name; sheet.Cell(row, 3).Value = category; sheet.Cell(row, 4).Value = qty; sheet.Cell(row, 5).Value = price; sheet.Cell(row, 6).Value = amount; sheet.Range(row, 5, row, 6).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]"; sheet.Range(row, 1, row, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; sheet.Range(row, 1, row, 6).Style.Border.InsideBorder = XLBorderStyleValues.Thin; }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true) dialog.PrintDocument(((IDocumentPaginatorSource)BuildDocument()).DocumentPaginator, $"Hợp đồng {_contract.ContractNo}");
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
