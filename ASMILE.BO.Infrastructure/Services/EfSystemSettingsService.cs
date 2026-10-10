using System.Globalization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads the legacy AppSettings table through the real EF context.
/// The table stores only key/value pairs, so metadata that is not persisted
/// there is intentionally reported with explicit source defaults.
/// </summary>
public sealed class EfSystemSettingsService : ISystemSettingsService
{
    private readonly AppDbContext _dbContext;

    public EfSystemSettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.AppSettings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    private static SystemSettingDto Map(Core.Entities.AppSetting row)
    {
        return new SystemSettingDto
        {
            Id = row.Id,
            Key = row.Key,
            Value = IsSensitiveKey(row.Key) ? "[REDACTED]" : row.Value,
            Group = GetGroup(row.Key),
            DataType = InferDataType(row.Value),
            // The legacy AppSettings table stores only Key and Value. Do not
            // fabricate change metadata or encryption state that is not stored.
            Description = null,
            IsEncrypted = null,
            UpdatedAt = null,
            UpdatedBy = null
        };
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        return normalized.Contains("password", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("token", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("apikey", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("connectionstring", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("credential", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetGroup(string key)
    {
        var separator = key.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? key[..separator] : "General";
    }

    private static string InferDataType(string value)
    {
        if (bool.TryParse(value, out _))
        {
            return "bool";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return "int";
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            return "decimal";
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            return "datetime";
        }

        return "string";
    }
}
