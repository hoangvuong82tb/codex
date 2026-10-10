using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

/// <summary>
/// Describes logging switches that are fixed by the process configuration.
/// User-configurable retention is enforced by ApplicationLogRetentionService.
/// </summary>
public sealed class RuntimeLoggingSettings
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    public RuntimeLoggingSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var fileSinks = configuration.GetSection("Serilog:WriteTo")
            .GetChildren()
            .Where(sink => string.Equals(sink["Name"], "File", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var errorLogEnabled = fileSinks.Count > 0;
        _values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            // SecurityAuditService always writes audit events to AuditLogs;
            // there is intentionally no user-facing kill switch.
            ["Logs.EnableAuditLog"] = "true",
            // Errors are part of the configured rolling application file log.
            ["Logs.EnableErrorLog"] = errorLogEnabled ? "true" : "false"
        };
    }

    public IReadOnlyDictionary<string, string?> Values => _values;

    public bool TryGetValue(string key, out string? value)
        => _values.TryGetValue(key, out value);

}
