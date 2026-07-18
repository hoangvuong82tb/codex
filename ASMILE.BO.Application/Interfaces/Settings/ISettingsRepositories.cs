using ASMILE.BO.Core.Entities.Settings;

namespace ASMILE.BO.Application.Interfaces.Settings;

public interface ISettingsRepository
{
    Task<IReadOnlyList<SettingDefinition>> GetDefinitionsAsync();
    Task<IReadOnlyList<SettingValue>> GetValuesAsync();
    Task<SettingValue?> GetValueAsync(string key, SettingScope scope, string? scopeTarget);
    Task UpsertValueAsync(SettingValue value);
    Task UpsertDefinitionAsync(SettingDefinition definition);
}

public interface IAppModuleRepository
{
    Task<IReadOnlyList<AppModule>> GetAppsAsync();
    Task<AppModule?> GetAppAsync(string appKey);
    Task UpsertAppAsync(AppModule app);
}

public interface ISettingChangeLogRepository
{
    Task AddAsync(SettingChangeLog log);
    Task<IReadOnlyList<SettingChangeLog>> GetRecentAsync(int take);
}
