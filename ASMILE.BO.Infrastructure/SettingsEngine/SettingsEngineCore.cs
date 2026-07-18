using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Shared.Security;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

internal static class SettingsMapper
{
    public static SettingDefinitionDto ToDto(this SettingDefinition x) => new()
    {
        Key = x.Key,
        DisplayName = x.DisplayName,
        Description = x.Description,
        Category = x.Category,
        Group = x.Group,
        Scope = x.Scope,
        ValueType = x.ValueType,
        DefaultValue = x.DefaultValue,
        IsRequired = x.IsRequired,
        IsSensitive = x.IsSensitive,
        IsReadOnly = x.IsReadOnly,
        RequiresRestart = x.RequiresRestart,
        RequiredPermission = x.RequiredPermission,
        SortOrder = x.SortOrder
    };

    public static SettingValueDto ToDto(this SettingValue x, string? valueOverride = null) => new()
    {
        Key = x.DefinitionKey,
        Scope = x.Scope,
        ScopeTarget = x.ScopeTarget,
        Value = valueOverride ?? x.Value,
        IsEncrypted = x.IsEncrypted,
        UpdatedAt = x.UpdatedAt,
        UpdatedBy = x.UpdatedBy
    };
}

public sealed class SettingsRepository : ISettingsRepository
{
    private static readonly List<SettingDefinition> Definitions = [];
    private static readonly List<SettingValue> Values = [];
    private static readonly object Sync = new();

    public Task<IReadOnlyList<SettingDefinition>> GetDefinitionsAsync()
    {
        lock (Sync)
        {
            return Task.FromResult<IReadOnlyList<SettingDefinition>>(Definitions.Select(Clone).ToList());
        }
    }

    public Task<IReadOnlyList<SettingValue>> GetValuesAsync()
    {
        lock (Sync)
        {
            return Task.FromResult<IReadOnlyList<SettingValue>>(Values.Select(Clone).ToList());
        }
    }

    public Task<SettingValue?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
    {
        lock (Sync)
        {
            var row = Values.FirstOrDefault(x => x.DefinitionKey == key && x.Scope == scope && string.Equals(x.ScopeTarget ?? string.Empty, scopeTarget ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(row is null ? null : Clone(row));
        }
    }

    public Task UpsertValueAsync(SettingValue value)
    {
        lock (Sync)
        {
            var idx = Values.FindIndex(x => x.DefinitionKey == value.DefinitionKey && x.Scope == value.Scope && string.Equals(x.ScopeTarget ?? string.Empty, value.ScopeTarget ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) Values[idx] = Clone(value); else Values.Add(Clone(value));
        }
        return Task.CompletedTask;
    }

    public Task UpsertDefinitionAsync(SettingDefinition definition)
    {
        lock (Sync)
        {
            var idx = Definitions.FindIndex(x => x.Key == definition.Key);
            if (idx >= 0) Definitions[idx] = Clone(definition); else Definitions.Add(Clone(definition));
        }
        return Task.CompletedTask;
    }

    private static SettingDefinition Clone(SettingDefinition x) => new()
    {
        Id = x.Id, Key = x.Key, DisplayName = x.DisplayName, Description = x.Description, Category = x.Category, Group = x.Group,
        Scope = x.Scope, ValueType = x.ValueType, DefaultValue = x.DefaultValue, IsRequired = x.IsRequired, IsSensitive = x.IsSensitive,
        IsReadOnly = x.IsReadOnly, RequiresRestart = x.RequiresRestart, RequiredPermission = x.RequiredPermission, SortOrder = x.SortOrder, CreatedAt = x.CreatedAt
    };

    private static SettingValue Clone(SettingValue x) => new()
    {
        Id = x.Id, DefinitionKey = x.DefinitionKey, Scope = x.Scope, ScopeTarget = x.ScopeTarget, Value = x.Value,
        IsEncrypted = x.IsEncrypted, UpdatedAt = x.UpdatedAt, UpdatedBy = x.UpdatedBy
    };
}

public sealed class AppModuleRepository : IAppModuleRepository
{
    private static readonly List<AppModule> Apps = [];
    private static readonly object Sync = new();

    public Task<IReadOnlyList<AppModule>> GetAppsAsync()
    {
        lock (Sync) return Task.FromResult<IReadOnlyList<AppModule>>(Apps.Select(Clone).ToList());
    }

    public Task<AppModule?> GetAppAsync(string appKey)
    {
        lock (Sync)
        {
            var app = Apps.FirstOrDefault(x => x.AppKey == appKey);
            return Task.FromResult(app is null ? null : Clone(app));
        }
    }

    public Task UpsertAppAsync(AppModule app)
    {
        lock (Sync)
        {
            var idx = Apps.FindIndex(x => x.AppKey == app.AppKey);
            if (idx >= 0) Apps[idx] = Clone(app); else Apps.Add(Clone(app));
        }
        return Task.CompletedTask;
    }

    private static AppModule Clone(AppModule x) => new()
    {
        Id = x.Id, AppKey = x.AppKey, Name = x.Name, Description = x.Description, Icon = x.Icon, Version = x.Version,
        IsEnabled = x.IsEnabled, ShowOnDesktop = x.ShowOnDesktop, PinToTaskbar = x.PinToTaskbar, AllowClientAccess = x.AllowClientAccess,
        RequiredPermission = x.RequiredPermission, SortOrder = x.SortOrder
    };
}

public sealed class SettingChangeLogRepository : ISettingChangeLogRepository
{
    private static readonly List<SettingChangeLog> Logs = [];
    private static readonly object Sync = new();

    public Task AddAsync(SettingChangeLog log)
    {
        lock (Sync) Logs.Add(log);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SettingChangeLog>> GetRecentAsync(int take)
    {
        lock (Sync) return Task.FromResult<IReadOnlyList<SettingChangeLog>>(Logs.OrderByDescending(x => x.ChangedAt).Take(take).ToList());
    }
}

public sealed class SettingsChangeNotifier : ISettingsChangeNotifier
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;
    public void NotifyChanged(SettingValueDto value, bool requiresRestart) => SettingChanged?.Invoke(this, new SettingChangedEventArgs(value, requiresRestart));
}

public sealed class SettingsAuditService : ISettingsAuditService
{
    private readonly ISettingChangeLogRepository _repo;

    public SettingsAuditService(ISettingChangeLogRepository repo) => _repo = repo;

    public Task LogChangeAsync(SettingChangeLogDto log)
    {
        return _repo.AddAsync(new SettingChangeLog
        {
            SettingKey = log.SettingKey,
            Scope = log.Scope,
            ScopeTarget = log.ScopeTarget,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            ChangedBy = log.ChangedBy,
            ChangedAt = log.ChangedAt,
            Reason = log.Reason
        });
    }
}

public sealed class SettingsValidator : ISettingsValidator
{
    public ValidationResult Validate(SettingDefinitionDto definition, string? value)
    {
        if (definition.IsRequired && string.IsNullOrWhiteSpace(value)) return ValidationResult.Fail("Giá trị bắt buộc.");
        if (definition.Key == "Backup.RetentionCount" && (!int.TryParse(value, out var ret) || ret < 1)) return ValidationResult.Fail("Retention count phải >= 1.");
        if (definition.Key == "Security.SessionTimeoutMinutes" && (!int.TryParse(value, out var timeout) || timeout < 5)) return ValidationResult.Fail("Session timeout phải >= 5 phút.");
        if (definition.Key == "Security.MaxFailedLoginAttempts" && (!int.TryParse(value, out var attempt) || attempt is < 3 or > 10)) return ValidationResult.Fail("Max failed attempts phải từ 3 đến 10.");
        if (definition.Key == "Network.ApiPort" && (!int.TryParse(value, out var port) || port is < 1 or > 65535)) return ValidationResult.Fail("Port phải từ 1 đến 65535.");
        if (definition.ValueType == SettingValueType.Number && !string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _)) return ValidationResult.Fail("Giá trị số không hợp lệ.");
        if (definition.ValueType == SettingValueType.Decimal && !string.IsNullOrWhiteSpace(value) && !decimal.TryParse(value, out _)) return ValidationResult.Fail("Giá trị decimal không hợp lệ.");
        if (definition.ValueType == SettingValueType.Path && !string.IsNullOrWhiteSpace(value) && (value.Contains("..") || value.Contains("*"))) return ValidationResult.Fail("Path không hợp lệ.");
        return ValidationResult.Success();
    }
}

