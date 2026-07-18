using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ClosedXML.Excel;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.Services;

public sealed class WarehouseImportExportService : IWarehouseImportExportService
{
    private static readonly string[] Headers =
    [
        "Mã kho",
        "Tên kho",
        "Mã kho cha",
        "Bộ phận",
        "Người chịu trách nhiệm",
        "Ghi chú",
        "Kho tiêu hao",
        "Kích hoạt"
    ];

    public async Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>
        {
            Headers,
            new[] { "KHO-TONG", "Kho tổng", string.Empty, "Hành chính", "Nguyễn Văn A", "Kho chính", "Không", "Có" },
            new[] { "KHO-BEP", "Kho bếp", "KHO-TONG", "Bếp", "Trần Thị B", "Kho nguyên liệu bếp", "Có", "Có" }
        };

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "WarehouseTemplate", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            await File.WriteAllLinesAsync(filePath, rows.Select(BuildCsvLine), new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task ExportCurrentViewAsync(IEnumerable<WarehouseDto> warehouses, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>
        {
            Headers
        };

        foreach (var row in warehouses)
        {
            rows.Add(
            [
                row.WarehouseCode,
                row.WarehouseName,
                row.ParentWarehouseCode,
                row.DepartmentName,
                row.ResponsiblePersonName,
                row.Note,
                row.IsConsumptionWarehouse ? "Có" : "Không",
                row.IsActive ? "Có" : "Không"
            ]);
        }

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "Warehouses", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            await File.WriteAllLinesAsync(filePath, rows.Select(BuildCsvLine), new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task<IReadOnlyList<WarehouseImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default)
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

    private static async Task<IReadOnlyList<WarehouseImportRow>> ReadCsvRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return [];
        }

        var rows = new List<WarehouseImportRow>();
        var startIndex = LooksLikeHeader(ParseCsvLine(lines[0])) ? 1 : 0;
        for (var index = startIndex; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = ParseCsvLine(line);
            rows.Add(new WarehouseImportRow
            {
                RowNumber = index + 1,
                WarehouseCode = GetColumn(columns, 0),
                WarehouseName = GetColumn(columns, 1),
                ParentWarehouseCode = GetColumn(columns, 2),
                DepartmentName = GetColumn(columns, 3),
                ResponsiblePersonName = GetColumn(columns, 4),
                Note = GetColumn(columns, 5),
                IsConsumptionWarehouseRaw = GetColumn(columns, 6),
                IsActiveRaw = GetColumn(columns, 7)
            });
        }

        return rows;
    }

    private static Task<IReadOnlyList<WarehouseImportRow>> ReadExcelRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbookStream = OpenExcelReadShared(filePath, cancellationToken);
        using var workbook = new XLWorkbook(workbookStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidOperationException("File Excel không có worksheet.");

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return Task.FromResult<IReadOnlyList<WarehouseImportRow>>([]);
        }

        var firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;

        var firstColumns = Enumerable.Range(1, Headers.Length)
            .Select(col => worksheet.Cell(firstRow, col).GetString().Trim())
            .ToList();
        var startRow = LooksLikeHeader(firstColumns) ? firstRow + 1 : firstRow;

        var rows = new List<WarehouseImportRow>();
        for (var rowIndex = startRow; rowIndex <= lastRow; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var columns = Enumerable.Range(1, Headers.Length)
                .Select(col => worksheet.Cell(rowIndex, col).GetFormattedString().Trim())
                .ToList();

            if (columns.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new WarehouseImportRow
            {
                RowNumber = rowIndex,
                WarehouseCode = GetColumn(columns, 0),
                WarehouseName = GetColumn(columns, 1),
                ParentWarehouseCode = GetColumn(columns, 2),
                DepartmentName = GetColumn(columns, 3),
                ResponsiblePersonName = GetColumn(columns, 4),
                Note = GetColumn(columns, 5),
                IsConsumptionWarehouseRaw = GetColumn(columns, 6),
                IsActiveRaw = GetColumn(columns, 7)
            });
        }

        return Task.FromResult<IReadOnlyList<WarehouseImportRow>>(rows);
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
            var headerRange = worksheet.Range(1, 1, 1, rows[0].Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF3FF");
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static bool LooksLikeHeader(IReadOnlyList<string> columns)
    {
        if (columns.Count == 0)
        {
            return false;
        }

        var first = columns[0].ToLowerInvariant();
        return first.Contains("mã") || first.Contains("ma") || first.Contains("kho");
    }

    private static string BuildCsvLine(IEnumerable<string> values)
    {
        static string Escape(string value)
        {
            var safe = value ?? string.Empty;
            if (safe.Contains('"'))
            {
                safe = safe.Replace("\"", "\"\"");
            }

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
