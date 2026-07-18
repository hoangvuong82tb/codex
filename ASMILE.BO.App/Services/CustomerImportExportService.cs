using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ClosedXML.Excel;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.Services;

public sealed class CustomerImportExportService : ICustomerImportExportService
{
    private static readonly string[] TemplateHeaders =
    [
        "Mã KH",
        "Tên khách hàng",
        "Điện thoại",
        "Tài khoản ngân hàng",
        "Địa chỉ",
        "Ghi chú",
        "Kích hoạt"
    ];

    public async Task ExportTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>
        {
            TemplateHeaders,
            new[] { "KH-001", "Khách hàng A", "0901234567", "123456789012", "123 Đường Lê Lợi", "Dữ liệu mẫu", "Có" },
            new[] { "KH-002", "Khách hàng B", "0912345678", "987654321000", "45 Nguyễn Huệ", "Dữ liệu mẫu", "Không" }
        };

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "CustomerTemplate", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            var lines = rows.Select(BuildCsvLine).ToList();
            await File.WriteAllLinesAsync(filePath, lines, new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task ExportCurrentViewAsync(IEnumerable<SupplierDto> customers, string filePath, SupplierExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new SupplierExportOptions();

        var rows = new List<string[]>();
        if (options.IncludeHeader)
        {
            rows.Add(new[] { "Mã KH", "Tên khách hàng", "Điện thoại", "Trạng thái" });
        }

        foreach (var row in customers)
        {
            rows.Add(
            [
                row.SupplierCode,
                row.SupplierName,
                row.Phone,
                row.IsActive ? "Kích hoạt" : "Không kích hoạt"
            ]);
        }

        if (IsExcelFile(filePath))
        {
            await WriteExcelAsync(filePath, "Customers", rows, cancellationToken);
            return;
        }

        if (IsCsvFile(filePath))
        {
            var lines = rows.Select(BuildCsvLine).ToList();
            await File.WriteAllLinesAsync(filePath, lines, new UTF8Encoding(true), cancellationToken);
            return;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    public async Task<IReadOnlyList<SupplierImportRow>> ReadImportRowsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".xlsx")
        {
            return await ReadExcelRowsAsync(filePath, cancellationToken);
        }

        if (extension == ".csv")
        {
            return await ReadCsvRowsAsync(filePath, cancellationToken);
        }

        if (extension == ".xls")
        {
            throw new NotSupportedException("Định dạng .xls không được hỗ trợ. Vui lòng lưu thành .xlsx hoặc .csv.");
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    private static async Task<IReadOnlyList<SupplierImportRow>> ReadCsvRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return [];
        }

        var rows = new List<SupplierImportRow>();
        var startIndex = LooksLikeHeader(ParseCsvLine(lines[0])) ? 1 : 0;

        for (var index = startIndex; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = ParseCsvLine(line);
            rows.Add(new SupplierImportRow
            {
                RowNumber = index + 1,
                SupplierCode = GetColumn(columns, 0),
                SupplierName = GetColumn(columns, 1),
                Phone = GetColumn(columns, 2),
                BankAccountNo = GetColumn(columns, 3),
                Address = GetColumn(columns, 4),
                Note = GetColumn(columns, 5),
                IsActiveRaw = GetColumn(columns, 6)
            });
        }

        return rows;
    }

    private static Task<IReadOnlyList<SupplierImportRow>> ReadExcelRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbookStream = OpenExcelReadShared(filePath, cancellationToken);
        using var workbook = new XLWorkbook(workbookStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidOperationException("File Excel không có worksheet.");

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return Task.FromResult<IReadOnlyList<SupplierImportRow>>([]);
        }

        var firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;

        var firstColumns = Enumerable.Range(1, TemplateHeaders.Length)
            .Select(col => worksheet.Cell(firstRow, col).GetString().Trim())
            .ToList();
        var startRow = LooksLikeHeader(firstColumns) ? firstRow + 1 : firstRow;

        var rows = new List<SupplierImportRow>();
        for (var rowIndex = startRow; rowIndex <= lastRow; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var columns = Enumerable.Range(1, TemplateHeaders.Length)
                .Select(col => worksheet.Cell(rowIndex, col).GetFormattedString().Trim())
                .ToList();

            if (columns.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new SupplierImportRow
            {
                RowNumber = rowIndex,
                SupplierCode = GetColumn(columns, 0),
                SupplierName = GetColumn(columns, 1),
                Phone = GetColumn(columns, 2),
                BankAccountNo = GetColumn(columns, 3),
                Address = GetColumn(columns, 4),
                Note = GetColumn(columns, 5),
                IsActiveRaw = GetColumn(columns, 6)
            });
        }

        return Task.FromResult<IReadOnlyList<SupplierImportRow>>(rows);
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
                var cell = worksheet.Cell(rowIndex + 1, colIndex + 1);
                cell.Value = row[colIndex] ?? string.Empty;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
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

    private static bool IsExcelFile(string filePath)
        => string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase);

    private static bool IsCsvFile(string filePath)
        => string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase);

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

    private static bool LooksLikeHeader(IReadOnlyList<string> columns)
    {
        if (columns.Count == 0)
        {
            return false;
        }

        var first = columns[0].ToLowerInvariant();
        return first.Contains("mã") || first.Contains("ma") || first.Contains("customer") || first.Contains("kh");
    }

    private static string GetColumn(IReadOnlyList<string> columns, int index)
        => columns.Count > index ? columns[index].Trim() : string.Empty;

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
