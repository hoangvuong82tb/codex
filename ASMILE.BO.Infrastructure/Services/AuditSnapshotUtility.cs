using System.Collections;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ASMILE.BO.Infrastructure.Services;

internal static class AuditSnapshotUtility
{
    public const int DefaultCompressionThresholdBytes = 8 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "Secret",
        "SecretKey",
        "Token",
        "AccessToken",
        "RefreshToken",
        "ConnectionString",
        "ApiKey",
        "PrivateKey"
    };

    public static AuditSnapshotPayload Build(object? before, object? after, int compressionThresholdBytes = DefaultCompressionThresholdBytes)
    {
        var beforeMap = Flatten(before);
        var afterMap = Flatten(after);
        var diffMap = BuildDiff(beforeMap, afterMap);
        var beforeJson = beforeMap.Count == 0 ? null : JsonSerializer.Serialize(beforeMap, JsonOptions);
        var afterJson = afterMap.Count == 0 ? null : JsonSerializer.Serialize(afterMap, JsonOptions);
        var diffJson = diffMap.Count == 0 ? null : JsonSerializer.Serialize(diffMap, JsonOptions);
        var hash = ComputeHash(beforeJson, afterJson, diffJson);
        var payloadJson = JsonSerializer.Serialize(new SnapshotEnvelope(beforeJson, afterJson, diffJson), JsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        if (payloadBytes.Length > compressionThresholdBytes)
        {
            return new AuditSnapshotPayload(null, null, null, hash, Compress(payloadBytes), "GZip", payloadBytes.Length, "SHA256", true);
        }

        return new AuditSnapshotPayload(beforeJson, afterJson, diffJson, hash, null, "None", payloadBytes.Length, "SHA256", true);
    }

    public static AuditSnapshotPayload Read(string? beforeJson, string? afterJson, string? diffJson, string snapshotHash, byte[]? compressedPayload, string? compressionType, long snapshotSize, string? hashAlgorithm)
    {
        if (compressedPayload is { Length: > 0 } && string.Equals(compressionType, "GZip", StringComparison.OrdinalIgnoreCase))
        {
            var envelope = JsonSerializer.Deserialize<SnapshotEnvelope>(Encoding.UTF8.GetString(Decompress(compressedPayload)), JsonOptions);
            beforeJson = envelope?.BeforeJson;
            afterJson = envelope?.AfterJson;
            diffJson = envelope?.DiffJson;
        }

        var hash = ComputeHash(beforeJson, afterJson, diffJson);
        var verified = string.Equals(hash, snapshotHash, StringComparison.OrdinalIgnoreCase);
        return new AuditSnapshotPayload(beforeJson, afterJson, diffJson, snapshotHash, compressedPayload, compressionType ?? "None", snapshotSize, hashAlgorithm ?? "SHA256", verified);
    }

    public static string? TryReadSimpleValue(object? source, string propertyName)
    {
        if (source is null || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property is null || !property.CanRead)
        {
            return null;
        }

        var value = property.GetValue(source);
        return value switch
        {
            null => null,
            string text => text,
            Guid guid => guid.ToString("D"),
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
            DateTimeOffset offset => offset.ToUniversalTime().ToString("O"),
            Enum @enum => @enum.ToString(),
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => null
        };
    }

    public static string ToStableJson(object? value)
    {
        var flattened = Flatten(value);
        return JsonSerializer.Serialize(flattened, JsonOptions);
    }

    private static Dictionary<string, object?> Flatten(object? value)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (value is null)
        {
            return result;
        }

        FlattenInto(result, value, string.Empty, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        return result;
    }

    private static void FlattenInto(Dictionary<string, object?> result, object value, string prefix, HashSet<object> visited, int depth)
    {
        if (depth > 3 || !visited.Add(value))
        {
            return;
        }

        var type = value.GetType();
        if (IsSimple(type))
        {
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                result[prefix] = Normalize(value);
            }

            return;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                var items = new List<object?>();
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        items.Add(null);
                        continue;
                    }

                    if (IsSimple(item.GetType()))
                    {
                        items.Add(Normalize(item));
                    }
                }

                if (items.Count > 0)
                {
                    result[prefix] = items;
                }
            }

            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || SensitiveNames.Contains(property.Name))
            {
                continue;
            }

            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
            {
                continue;
            }

            var propertyType = propertyValue.GetType();
            if (IsSimple(propertyType))
            {
                result[BuildPath(prefix, property.Name)] = Normalize(propertyValue);
                continue;
            }

            if (propertyValue is IEnumerable childEnumerable && propertyValue is not string)
            {
                var items = new List<object?>();
                foreach (var item in childEnumerable)
                {
                    if (item is null)
                    {
                        items.Add(null);
                        continue;
                    }

                    if (IsSimple(item.GetType()))
                    {
                        items.Add(Normalize(item));
                    }
                }

                if (items.Count > 0)
                {
                    result[BuildPath(prefix, property.Name)] = items;
                }

                continue;
            }

            FlattenInto(result, propertyValue, BuildPath(prefix, property.Name), visited, depth + 1);
        }
    }

    private static IReadOnlyList<AuditDiffItem> BuildDiff(IReadOnlyDictionary<string, object?> before, IReadOnlyDictionary<string, object?> after)
    {
        var keys = before.Keys.Concat(after.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var items = new List<AuditDiffItem>();
        foreach (var key in keys)
        {
            before.TryGetValue(key, out var beforeValue);
            after.TryGetValue(key, out var afterValue);
            if (AreEqual(beforeValue, afterValue))
            {
                continue;
            }

            items.Add(new AuditDiffItem
            {
                Path = key,
                Before = beforeValue,
                After = afterValue
            });
        }

        return items;
    }

    private static string ComputeHash(string? beforeJson, string? afterJson, string? diffJson)
    {
        var payload = $"{beforeJson ?? string.Empty}|{afterJson ?? string.Empty}|{diffJson ?? string.Empty}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static byte[] Compress(byte[] payload)
    {
        using var target = new MemoryStream();
        using (var gzip = new GZipStream(target, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(payload, 0, payload.Length);
        }

        return target.ToArray();
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var source = new MemoryStream(payload);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var target = new MemoryStream();
        gzip.CopyTo(target);
        return target.ToArray();
    }

    private static bool AreEqual(object? left, object? right)
        => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions);

    private static object? Normalize(object value)
        => value switch
        {
            string text => text,
            Guid guid => guid.ToString("D"),
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
            DateTimeOffset offset => offset.ToUniversalTime().ToString("O"),
            Enum @enum => @enum.ToString(),
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value
        };

    private static bool IsSimple(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || type == typeof(string)
           || type == typeof(Guid)
           || type == typeof(DateTime)
           || type == typeof(DateTimeOffset)
           || type == typeof(decimal)
           || type == typeof(TimeSpan);

    private static string BuildPath(string prefix, string name)
        => string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix}.{name}";

    private sealed class AuditDiffItem
    {
        public string Path { get; set; } = string.Empty;
        public object? Before { get; set; }
        public object? After { get; set; }
    }

    private sealed record SnapshotEnvelope(string? BeforeJson, string? AfterJson, string? DiffJson);

    public sealed record AuditSnapshotPayload(
        string? BeforeJson,
        string? AfterJson,
        string? DiffJson,
        string SnapshotHash,
        byte[]? CompressedPayload,
        string CompressionType,
        long SnapshotSize,
        string HashAlgorithm,
        bool HashVerified);
}
