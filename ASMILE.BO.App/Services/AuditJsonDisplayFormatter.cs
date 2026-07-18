using System.Text.Encodings.Web;
using System.Text.Json;

namespace ASMILE.BO.App.Services;

public static class AuditJsonDisplayFormatter
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string FormatForDisplay(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return string.Empty;

        // Step 1: Try parsing as a JSON object/array directly
        if (TryPrettyPrint(rawJson, out var pretty))
            return pretty;

        // Step 2: Try parsing as a JSON string that contains a JSON object/array (unwrap one level)
        if (TryUnwrapAndPrettyPrint(rawJson, out var unwrapped))
            return unwrapped;

        // Step 3: Try parsing as a JSON string that contains ANOTHER JSON string (unwrap two levels — handles triple serialization)
        if (TryDoubleUnwrapAndPrettyPrint(rawJson, out var doubleUnwrapped))
            return doubleUnwrapped;

        // Fallback: return raw text
        return rawJson;
    }

    public static string SerializePretty<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value, PrettyOptions);
        }
        catch
        {
            return value?.ToString() ?? string.Empty;
        }
    }

    public static string SerializeAnonymousObject(object? value)
    {
        if (value is null) return string.Empty;
        return FormatForDisplay(JsonSerializer.Serialize(value, PrettyOptions));
    }

    private static bool TryPrettyPrint(string raw, out string result)
    {
        result = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            result = JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryUnwrapAndPrettyPrint(string raw, out string result)
    {
        result = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                    return TryPrettyPrint(inner, out result);
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDoubleUnwrapAndPrettyPrint(string raw, out string result)
    {
        result = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                    return TryUnwrapAndPrettyPrint(inner, out result);
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
