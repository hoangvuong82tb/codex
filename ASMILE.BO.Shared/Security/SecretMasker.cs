using System.Data.Common;

namespace ASMILE.BO.Shared.Security;

public static class SecretMasker
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Pwd", "User Id", "UID", "Token", "ApiKey", "Secret"
    };

    public static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= 4)
        {
            return "****";
        }

        return $"{value[..2]}****{value[^2..]}";
    }

    public static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var keys = builder.Keys.Cast<object>().Select(k => k.ToString() ?? string.Empty).ToList();
            foreach (var key in keys)
            {
                if (SensitiveKeys.Contains(key))
                {
                    builder[key] = "***";
                }
            }

            return builder.ConnectionString;
        }
        catch
        {
            return "***";
        }
    }

    public static IDictionary<string, string> SanitizeMetadata(IDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in metadata)
        {
            if (SensitiveKeys.Contains(kv.Key) || kv.Key.Contains("password", StringComparison.OrdinalIgnoreCase)
                || kv.Key.Contains("token", StringComparison.OrdinalIgnoreCase)
                || kv.Key.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || kv.Key.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                result[kv.Key] = "***";
            }
            else
            {
                result[kv.Key] = kv.Value;
            }
        }

        return result;
    }
}
