using Microsoft.Data.SqlClient;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

/// <summary>
/// Safe, non-secret view of the connection configuration used by this process.
/// The original connection string and password are deliberately not retained.
/// </summary>
public sealed class RuntimeDatabaseSettings
{
    private const string HiddenSecret = "••••••••";
    private readonly IReadOnlyDictionary<string, string?> _values;

    private RuntimeDatabaseSettings(IReadOnlyDictionary<string, string?> values)
    {
        _values = values;
    }

    public static RuntimeDatabaseSettings FromConnectionString(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var authentication = builder.IntegratedSecurity
                ? "Windows"
                : builder.ContainsKey("Authentication")
                    ? Convert.ToString(builder["Authentication"]) ?? "Sql"
                    : "Sql";
            var hasPassword = !string.IsNullOrEmpty(builder.Password);

            return new RuntimeDatabaseSettings(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database.ServerName"] = builder.DataSource,
                ["Database.DatabaseName"] = builder.InitialCatalog,
                ["Database.AuthenticationMode"] = authentication,
                ["Database.UserName"] = builder.IntegratedSecurity
                    ? "Windows account"
                    : builder.UserID,
                ["Database.Password"] = hasPassword ? HiddenSecret : string.Empty,
                ["Database.ConnectionString"] = "Đã cấu hình · thông tin xác thực được ẩn"
            });
        }
        catch (ArgumentException)
        {
            // Never include the input in diagnostics: it can contain credentials.
            var unavailable = "Không thể đọc cấu hình kết nối runtime";
            return new RuntimeDatabaseSettings(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database.ServerName"] = unavailable,
                ["Database.DatabaseName"] = unavailable,
                ["Database.AuthenticationMode"] = unavailable,
                ["Database.UserName"] = unavailable,
                ["Database.Password"] = string.Empty,
                ["Database.ConnectionString"] = "Đã cấu hình · không thể phân tích an toàn"
            });
        }
    }

    public bool TryGetValue(string key, out string? value)
        => _values.TryGetValue(key, out value);

    public IReadOnlyDictionary<string, string?> Values => _values;
}
