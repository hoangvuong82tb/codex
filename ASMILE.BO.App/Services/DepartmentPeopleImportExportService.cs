using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ClosedXML.Excel;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.Services;

public sealed class DepartmentPeopleImportExportService : IDepartmentPeopleImportExportService
{
    private static readonly string[] DepartmentHeaders =
    [
        "Mã bộ phận",
        "Tên bộ phận",
        "Mã bộ phận cha",
        "Cấp",
        "Ghi chú",
        "Kích hoạt"
    ];

    private static readonly string[] PersonHeaders =
    [
        "Mã nhân sự",
        "Tên nhân sự",
        "Mã bộ phận",
        "Chức vụ",
        "Điện thoại",
        "Email",
        "Ghi chú",
        "Kích hoạt"
    ];

    public async Task ExportDepartmentTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = new List<string[]>
        {
            DepartmentHeaders,
            new[] { "BP-001", "Khối kinh doanh", string.Empty, "1", "Bộ phận cấp 1", "Có" },
            new[] { "BP-002", "Nhóm bán lẻ", "BP-001", "2", "Bộ phận con", "Có" }
        };

        await WriteRowsAsync(filePath, "DepartmentTemplate", rows, cancellationToken);
    }

    public async Task ExportPersonTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = new List<string[]>
        {
            PersonHeaders,
            new[] { "NS-001", "Nguyễn Văn A", "BP-001", "Trưởng bộ phận", "0901234567", "a@company.com", "Nhân sự mẫu", "Có" },
            new[] { "NS-002", "Trần Thị B", "BP-002", "Nhân viên", "0912345678", "b@company.com", string.Empty, "Không" }
        };

        await WriteRowsAsync(filePath, "PeopleTemplate", rows, cancellationToken);
    }

    public async Task ExportCurrentViewAsync(IEnumerable<PersonDto> people, string filePath, CancellationToken cancellationToken = default)
    {
        var rows = new List<string[]> { PersonHeaders };
        foreach (var row in people)
        {
            rows.Add(new[]
            {
                row.PersonCode,
                row.FullName,
                row.DepartmentCode,
                row.Position,
                row.Phone,
                row.Email,
                row.Note,
                row.IsActive ? "Có" : "Không"
            });
        }

        await WriteRowsAsync(filePath, "People", rows, cancellationToken);
    }

    public async Task<DepartmentPeopleImportPayload> ReadImportPayloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var payload = new DepartmentPeopleImportPayload();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".xlsx")
        {
            using var workbook = new XLWorkbook(filePath);
            foreach (var sheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadWorksheet(sheet, payload);
            }

            return payload;
        }

        if (extension == ".csv")
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var rows = lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(ParseCsvLine).ToList();
            ReadRows(rows, payload);
            return payload;
        }

        throw new NotSupportedException("Định dạng file không hỗ trợ. Chỉ hỗ trợ .xlsx hoặc .csv.");
    }

    private static void ReadWorksheet(IXLWorksheet worksheet, DepartmentPeopleImportPayload payload)
    {
        var range = worksheet.RangeUsed();
        if (range is null)
        {
            return;
        }

        var firstRow = range.RangeAddress.FirstAddress.RowNumber;
        var lastRow = range.RangeAddress.LastAddress.RowNumber;
        var maxColumns = Math.Max(DepartmentHeaders.Length, PersonHeaders.Length);

        var rows = new List<List<string>>();
        for (var row = firstRow; row <= lastRow; row++)
        {
            var columns = Enumerable.Range(1, maxColumns)
                .Select(col => worksheet.Cell(row, col).GetFormattedString().Trim())
                .ToList();
            if (columns.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(columns);
        }

        ReadRows(rows, payload);
    }

    private static void ReadRows(IReadOnlyList<List<string>> rows, DepartmentPeopleImportPayload payload)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var header = rows[0];
        var isDepartmentFile = DetectDepartmentHeader(header);
        var isPersonFile = DetectPersonHeader(header);
        var startRow = (isDepartmentFile || isPersonFile) ? 1 : 0;

        for (var i = startRow; i < rows.Count; i++)
        {
            var cols = rows[i];
            if (isDepartmentFile)
            {
                payload.Departments.Add(new DepartmentImportRow
                {
                    DepartmentCode = GetColumn(cols, 0),
                    DepartmentName = GetColumn(cols, 1),
                    ParentDepartmentCode = GetColumn(cols, 2),
                    LevelRaw = GetColumn(cols, 3),
                    Note = GetColumn(cols, 4),
                    IsActiveRaw = GetColumn(cols, 5)
                });
            }
            else
            {
                payload.People.Add(new PersonImportRow
                {
                    PersonCode = GetColumn(cols, 0),
                    FullName = GetColumn(cols, 1),
                    DepartmentCode = GetColumn(cols, 2),
                    Position = GetColumn(cols, 3),
                    Phone = GetColumn(cols, 4),
                    Email = GetColumn(cols, 5),
                    Note = GetColumn(cols, 6),
                    IsActiveRaw = GetColumn(cols, 7)
                });
            }
        }
    }

    private static bool DetectDepartmentHeader(IReadOnlyList<string> columns)
    {
        var first = columns.Count > 0 ? columns[0].ToLowerInvariant() : string.Empty;
        return first.Contains("bộ phận") || first.Contains("bo phan") || first.Contains("department");
    }

    private static bool DetectPersonHeader(IReadOnlyList<string> columns)
    {
        var first = columns.Count > 0 ? columns[0].ToLowerInvariant() : string.Empty;
        return first.Contains("nhân sự") || first.Contains("nhan su") || first.Contains("person");
    }

    private static async Task WriteRowsAsync(string filePath, string worksheetName, IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            var lines = rows.Select(BuildCsvLine).ToList();
            await File.WriteAllLinesAsync(filePath, lines, new UTF8Encoding(true), cancellationToken);
            return;
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            for (var colIndex = 0; colIndex < rows[rowIndex].Length; colIndex++)
            {
                worksheet.Cell(rowIndex + 1, colIndex + 1).Value = rows[rowIndex][colIndex] ?? string.Empty;
            }
        }

        if (rows.Count > 0)
        {
            var header = worksheet.Range(1, 1, 1, rows[0].Length);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
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
}
