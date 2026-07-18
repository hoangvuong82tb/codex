using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Core.Entities.Settings;

namespace ASMILE.BO.Application.Interfaces.Settings;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget);
    Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget);
    Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget);
    Task SaveValueAsync(SaveSettingValueRequest request);
    Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests);
    Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget);
}

public interface IAppModuleService
{
    Task<IReadOnlyList<AppModuleDto>> GetAppsAsync();
    Task<AppModuleDto?> GetAppAsync(string appKey);
    Task UpdateAppAsync(UpdateAppModuleRequest request);
    Task<IReadOnlyList<SettingDefinitionDto>> GetAppSettingDefinitionsAsync(string appKey);
    Task<IReadOnlyList<SettingValueDto>> GetAppSettingsAsync(string appKey);
    Task SaveAppSettingsAsync(string appKey, IReadOnlyList<SaveSettingValueRequest> requests);
}

public interface ISettingsRegistry
{
    IReadOnlyList<SettingDefinitionDto> GetDefaultDefinitions();
    IReadOnlyList<AppModuleDto> GetDefaultApps();
}

public interface ISettingsAuditService
{
    Task LogChangeAsync(SettingChangeLogDto log);
}

public interface ISettingsValidator
{
    ValidationResult Validate(SettingDefinitionDto definition, string? value);
}

public interface ISettingsChangeNotifier
{
    event EventHandler<SettingChangedEventArgs>? SettingChanged;
    void NotifyChanged(SettingValueDto value, bool requiresRestart);
}

public sealed class SettingChangedEventArgs : EventArgs
{
    public SettingChangedEventArgs(SettingValueDto value, bool requiresRestart)
    {
        Value = value;
        RequiresRestart = requiresRestart;
    }

    public SettingValueDto Value { get; }
    public bool RequiresRestart { get; }
}
