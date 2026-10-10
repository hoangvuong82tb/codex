using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;

namespace ASMILE.BO.App.Helpers;

/// <summary>
/// Creates a user-selected, formatted Excel workbook for tabular operational reports.
/// The caller supplies already-authorized data; this helper does not query or transform
/// business records.
/// </summary>
public static class ExcelExportHelper
{
    public static string? Export(
        string title,
        string filePrefix,
        string summary,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Chọn vị trí lưu báo cáo Excel",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"{SanitizeFileName(filePrefix)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return null;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Báo cáo");
        var lastColumn = Math.Max(1, headers.Count);

        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, lastColumn).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("102E5A");

        sheet.Cell(2, 1).Value = summary;
        sheet.Range(2, 1, 2, lastColumn).Merge();
        sheet.Cell(2, 1).Style.Alignment.WrapText = true;
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("475569");

        for (var index = 0; index < headers.Count; index++)
            sheet.Cell(4, index + 1).Value = headers[index];

        var headerRange = sheet.Range(4, 1, 4, lastColumn);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("1D4ED8");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                sheet.Cell(rowIndex + 5, columnIndex + 1).Value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
        }

        var lastRow = Math.Max(4, rows.Count + 4);
        var tableRange = sheet.Range(4, 1, lastRow, lastColumn);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("CBD5E1");
        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("E2E8F0");
        sheet.SheetView.FreezeRows(4);
        sheet.Range(4, 1, lastRow, lastColumn).SetAutoFilter();

        for (var column = 1; column <= lastColumn; column++)
        {
            var worksheetColumn = sheet.Column(column);
            worksheetColumn.AdjustToContents(1, Math.Min(lastRow, 1004));
            worksheetColumn.Width = Math.Clamp(worksheetColumn.Width, 12d, 48d);
        }
        sheet.Row(2).Height = 32;

        workbook.SaveAs(dialog.FileName);
        return dialog.FileName;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
        return string.IsNullOrWhiteSpace(safe) ? "bao_cao" : safe;
    }
}
