using System.Globalization;
using System.IO;
using System.Text;

namespace ASMILE.BO.App.Helpers;

public static class CsvExportHelper
{
    public static string Export(string filePrefix, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var exportDir = Path.Combine(AppContext.BaseDirectory, "exports", "accounting");
        Directory.CreateDirectory(exportDir);
        var filePath = Path.Combine(exportDir, $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
        writer.WriteLine(string.Join(',', headers.Select(Escape)));
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',', row.Select(Escape)));
        }

        return filePath;
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }

    public static string F(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
}
