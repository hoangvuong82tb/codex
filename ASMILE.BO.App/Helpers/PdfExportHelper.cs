using System.IO;
using System.Globalization;
using System.Text;
using Microsoft.Win32;

namespace ASMILE.BO.App.Helpers;

/// <summary>
/// Small, dependency-free PDF writer for tabular operational exports.
/// It deliberately writes a bounded preview (the CSV export remains the
/// complete machine-readable export) so a report action always produces a
/// real file instead of claiming that a renderer exists when it does not.
/// </summary>
public static class PdfExportHelper
{
    public static string? Export(
        string title,
        string filePrefix,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var dialog = new SaveFileDialog
        {
            Title = $"Xuất PDF - {title}",
            FileName = $"{SanitizeFileName(filePrefix)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            Filter = "PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return null;

        var bytes = BuildPdf(title, headers, rows);
        File.WriteAllBytes(dialog.FileName, bytes);
        return dialog.FileName;
    }

    private static byte[] BuildPdf(
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var lines = new List<string>
        {
            title,
            $"Ngay lap: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"So dong: {rows.Count}",
            string.Join(" | ", headers.Select(StripPdfText))
        };

        lines.AddRange(rows.Take(42).Select(row =>
            string.Join(" | ", row.Select(value => StripPdfText(value ?? string.Empty)))));
        if (rows.Count > 42)
            lines.Add($"... con {rows.Count - 42} dong. Dung CSV de xem day du.");

        var content = new StringBuilder();
        content.AppendLine("BT /F1 8 Tf 28 560 Td 10 TL");
        foreach (var line in lines)
            content.Append('(').Append(EscapePdf(line)).AppendLine(") Tj T*");
        content.Append("ET");

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var streamBytes = Encoding.ASCII.GetBytes(content.ToString());
        objects.Add($"<< /Length {streamBytes.Length} >>\nstream\n{content}\nendstream");

        using var memory = new MemoryStream();
        using var writer = new StreamWriter(memory, Encoding.ASCII, leaveOpen: true);
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        foreach (var (obj, index) in objects.Select((value, index) => (value, index)))
        {
            writer.Flush();
            offsets.Add(memory.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(obj);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = memory.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();
        return memory.ToArray();
    }

    private static string StripPdfText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(c <= 127 ? c : '?');
        }

        return builder.ToString();
    }

    private static string EscapePdf(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "report" : sanitized;
    }
}
