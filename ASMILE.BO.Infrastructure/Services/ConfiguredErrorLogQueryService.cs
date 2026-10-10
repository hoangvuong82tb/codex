using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Storage;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads a bounded tail from the single configured Serilog file sink. This is
/// intentionally read-only and does not search arbitrary folders or files.
/// </summary>
public sealed class ConfiguredErrorLogQueryService : IErrorLogQueryService
{
    private const int MaxReadBytesPerFile = 1024 * 1024;
    private const int MaxTotalReadBytes = 4 * 1024 * 1024;
    private const int MaxFilesToRead = 14;
    private static readonly Regex EventHeader = new(
        @"^\[(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) (?<level>[A-Z]{3})\]\s?(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SecretAssignment = new(
        @"(?i)(?<key>password|passwd|pwd|token|api[_-]?key|client[_-]?secret)\s*[:=]\s*(?<value>[^\s;,""']+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex UrlCredentials = new(
        @"(?i)(https?://)[^/@\s:]+:[^/@\s]+@",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IConfiguration _configuration;

    public ConfiguredErrorLogQueryService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<ErrorLogSnapshotDto> GetRecentErrorsAsync(int maxEntries = 100, CancellationToken cancellationToken = default)
    {
        maxEntries = Math.Clamp(maxEntries, 1, 200);
        var sinks = _configuration.GetSection("Serilog:WriteTo")
            .GetChildren()
            .Where(x => string.Equals(x["Name"], "File", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (sinks.Length != 1)
        {
            return Empty(false, false, sinks.Length == 0
                ? "Không có file sink trong cấu hình Serilog."
                : "Trang nhật ký chỉ hỗ trợ đúng một file sink; cấu hình hiện tại có nhiều sink.");
        }

        var configuredPath = sinks[0]["Args:path"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredPath))
            return Empty(false, false, "File sink chưa khai báo đường dẫn.");

        string fullPath;
        try
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
            fullPath = Path.GetFullPath(Path.IsPathRooted(expandedPath)
                ? expandedPath
                : Path.Combine(ApplicationDataPaths.UserDataRoot, expandedPath));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return Empty(true, false, "Đường dẫn file sink không hợp lệ.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        var configuredFileName = Path.GetFileName(fullPath);
        var extension = Path.GetExtension(configuredFileName);
        var prefix = Path.GetFileNameWithoutExtension(configuredFileName);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(prefix))
            return Empty(true, false, "Không xác định được thư mục file log.");
        var rolledFilePattern = BuildRolledFilePattern(prefix, extension, sinks[0]["Args:rollingInterval"]);

        try
        {
            if (!Directory.Exists(directory))
                return Empty(true, true, "Thư mục log chưa được tạo hoặc chưa có file log.");

            var searchPattern = $"{prefix}*{extension}";
            var files = Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    var name = Path.GetFileName(path);
                    return rolledFilePattern.IsMatch(name);
                })
                .Select(path => new FileInfo(path))
                .Where(info => (info.Attributes & FileAttributes.ReparsePoint) == 0)
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .Take(MaxFilesToRead)
                .ToArray();

            if (files.Length == 0)
                return Empty(true, true, "Chưa có file log trong thư mục được cấu hình.");

            var entries = new List<ErrorLogEntryDto>();
            var remainingBytes = MaxTotalReadBytes;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (remainingBytes <= 0) break;
                var bytesToRead = (int)Math.Min(Math.Min(file.Length, MaxReadBytesPerFile), remainingBytes);
                remainingBytes -= bytesToRead;
                await ReadFileTailAsync(file.FullName, bytesToRead, entries, cancellationToken).ConfigureAwait(false);
            }

            var recent = entries
                .AsEnumerable()
                .Reverse()
                .OrderByDescending(x => x.Timestamp)
                .Take(maxEntries)
                .ToArray();
            return new ErrorLogSnapshotDto(true, true,
                recent.Length == 0 ? "Đã đọc file log; không có sự kiện Error/Fatal trong phần dữ liệu gần nhất." : $"Đọc {recent.Length} sự kiện Error/Fatal từ file sink đã cấu hình.",
                recent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            return Empty(true, false, $"Không thể đọc file log ({ex.GetType().Name}).");
        }
    }

    private static async Task ReadFileTailAsync(
        string path,
        int byteLimit,
        List<ErrorLogEntryDto> entries,
        CancellationToken cancellationToken)
    {
        if (byteLimit <= 0) return;

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 16 * 1024, useAsync: true);
        var offset = Math.Max(0, stream.Length - byteLimit);
        stream.Seek(offset, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        if (offset > 0)
            await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false); // discard a possibly partial first line

        DateTime? timestamp = null;
        string? level = null;
        var message = new StringBuilder();

        void Flush()
        {
            if (timestamp.HasValue && level is "ERR" or "FTL")
            {
                var text = Redact(message.ToString().Trim());
                if (text.Length > 2000) text = text[..2000] + "…";
                entries.Add(new ErrorLogEntryDto(timestamp.Value, level, text));
            }
        }

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            var match = EventHeader.Match(line);
            if (match.Success)
            {
                Flush();
                timestamp = DateTime.TryParseExact(
                    match.Groups["timestamp"].Value,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed)
                    ? parsed
                    : null;
                level = match.Groups["level"].Value;
                message.Clear();
                message.Append(match.Groups["message"].Value);
            }
            else if (timestamp.HasValue && level is "ERR" or "FTL")
            {
                if (message.Length < 2000)
                    message.AppendLine().Append(line.AsSpan(0, Math.Min(line.Length, 2000 - message.Length)));
            }
        }

        Flush();
    }

    private static string Redact(string value)
    {
        var sanitized = SecretAssignment.Replace(value, match => $"{match.Groups["key"].Value}=***");
        return UrlCredentials.Replace(sanitized, "$1***:***@");
    }

    private static ErrorLogSnapshotDto Empty(bool configured, bool available, string status)
        => new(configured, available, status, Array.Empty<ErrorLogEntryDto>());

    private static Regex BuildRolledFilePattern(string prefix, string extension, string? rollingInterval)
    {
        var datePattern = rollingInterval?.Trim().ToLowerInvariant() switch
        {
            "year" => @"\d{4}",
            "month" => @"\d{6}",
            "day" => @"\d{8}",
            "hour" => @"\d{10}",
            "minute" => @"\d{12}",
            _ => null
        };
        var rolledPattern = datePattern is null
            ? string.Empty
            : Regex.Escape(prefix) + datePattern + Regex.Escape(extension);
        var configuredPattern = Regex.Escape(prefix + extension);
        var pattern = datePattern is null
            ? $"^(?:{configuredPattern})$"
            : $"^(?:{configuredPattern}|{rolledPattern})$";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
