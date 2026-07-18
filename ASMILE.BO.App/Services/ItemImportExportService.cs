using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ClosedXML.Excel;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.Services;

public sealed class ItemImportExportService : IItemImportExportService
{
    private static readonly string[] Headers =
    [
        "Mã hàng",
        "Tên hàng",
        "Đơn vị",
        "Nhóm hàng",
        "Mã NCC",
        "Tên NCC",
        "Kích hoạt"
    ];

    public async Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>
        {
            Headers,
            new[] { "HH-001", "Gạo thơm", "Kg", "Nguyên liệu", "NCC-001", "Nhà cung cấp A", "Có" },
            new[] { "HH-002", "Dầu ăn", "Chai", "Nguyên liệu", "NCC-002", "Nhà cung cấp B", "Có" }
        };

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "ItemTemplate", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            await File.WriteAllLinesAsync(filePath, rows.Select(BuildCsvLine), new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task ExportCurrentViewAsync(IEnumerable<ItemDto> items, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = new List<string[]> { Headers };

        foreach (var row in items)
        {
            rows.Add(
            [
                row.ItemCode,
                row.ItemName,
                row.Unit,
                row.GroupName,
                row.SupplierCode,
                row.SupplierName,
                row.IsActive ? "Có" : "Không"
            ]);
        }

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "Items", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            await File.WriteAllLinesAsync(filePath, rows.Select(BuildCsvLine), new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task<IReadOnlyList<ItemImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => await ReadExcelRowsAsync(filePath, cancellationToken),
            ".csv" => await ReadCsvRowsAsync(filePath, cancellationToken),
            ".xls" => throw new NotSupportedException("Định dạng .xls không hỗ trợ. Vui lòng dùng .xlsx hoặc .csv."),
            _ => throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.")
        };
    }

    private static async Task<IReadOnlyList<ItemImportRow>> ReadCsvRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return [];
        }

        var rows = new List<ItemImportRow>();
        var startIndex = LooksLikeHeader(ParseCsvLine(lines[0])) ? 1 : 0;
        for (var index = startIndex; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = ParseCsvLine(line);
            rows.Add(new ItemImportRow
            {
                RowNumber = index + 1,
                ItemCode = GetColumn(cols, 0),
                ItemName = GetColumn(cols, 1),
                Unit = GetColumn(cols, 2),
                GroupName = GetColumn(cols, 3),
                SupplierCode = GetColumn(cols, 4),
                SupplierName = GetColumn(cols, 5),
                IsActiveRaw = GetColumn(cols, 6)
            });
        }

        return rows;
    }

    private static Task<IReadOnlyList<ItemImportRow>> ReadExcelRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var workbookStream = OpenExcelReadShared(filePath, cancellationToken);
        using var workbook = new XLWorkbook(workbookStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidOperationException("File Excel không có worksheet.");

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return Task.FromResult<IReadOnlyList<ItemImportRow>>([]);
        }

        var firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
        var firstColumns = Enumerable.Range(1, Headers.Length)
            .Select(col => worksheet.Cell(firstRow, col).GetString().Trim())
            .ToList();
        var startRow = LooksLikeHeader(firstColumns) ? firstRow + 1 : firstRow;

        var rows = new List<ItemImportRow>();
        for (var rowIndex = startRow; rowIndex <= lastRow; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cols = Enumerable.Range(1, Headers.Length)
                .Select(col => worksheet.Cell(rowIndex, col).GetFormattedString().Trim())
                .ToList();

            if (cols.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ItemImportRow
            {
                RowNumber = rowIndex,
                ItemCode = GetColumn(cols, 0),
                ItemName = GetColumn(cols, 1),
                Unit = GetColumn(cols, 2),
                GroupName = GetColumn(cols, 3),
                SupplierCode = GetColumn(cols, 4),
                SupplierName = GetColumn(cols, 5),
                IsActiveRaw = GetColumn(cols, 6)
            });
        }

        return Task.FromResult<IReadOnlyList<ItemImportRow>>(rows);
    }

    private static Task WriteExcelAsync(string filePath, string worksheetName, IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var colIndex = 0; colIndex < row.Length; colIndex++)
            {
                worksheet.Cell(rowIndex + 1, colIndex + 1).Value = row[colIndex] ?? string.Empty;
            }
        }

        if (rows.Count > 0)
        {
            var header = worksheet.Range(1, 1, 1, rows[0].Length);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF3FF");
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static bool LooksLikeHeader(IReadOnlyList<string> columns)
    {
        if (columns.Count == 0) return false;
        var first = columns[0].ToLowerInvariant();
        return first.Contains("mã") || first.Contains("ma") || first.Contains("hàng") || first.Contains("item");
    }

    private static string BuildCsvLine(IEnumerable<string> values)
    {
        static string Escape(string value)
        {
            var safe = value ?? string.Empty;
            if (safe.Contains('"')) safe = safe.Replace("\"", "\"\"");
            return safe.Contains(',') || safe.Contains('"') || safe.Contains('\n') || safe.Contains('\r')
                ? $"\"{safe}\""
                : safe;
        }

        return string.Join(",", values.Select(Escape));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private static string GetColumn(IReadOnlyList<string> columns, int index)
        => columns.Count > index ? columns[index].Trim() : string.Empty;

    private static bool IsExcelFile(string filePath)
        => string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase);

    private static bool IsCsvFile(string filePath)
        => string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase);

    private static MemoryStream OpenExcelReadShared(string filePath, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        var memory = new MemoryStream();
        fileStream.CopyTo(memory);
        cancellationToken.ThrowIfCancellationRequested();
        memory.Position = 0;
        return memory;
    }
}
