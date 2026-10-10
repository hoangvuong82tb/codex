using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

public sealed class EfSettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
    private readonly AppDbContext? _legacyDbContext;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public EfSettingsRepository(AppDbContext dbContext)
    {
        _legacyDbContext = dbContext;
    }

    public EfSettingsRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<SettingDefinition>> GetDefinitionsAsync()
    {
        await _operationLock.WaitAsync();
        try
        {
            return await WithContextAsync(db => db.SettingDefinitions
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Key)
                .ToListAsync());
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<IReadOnlyList<SettingValue>> GetValuesAsync()
    {
        await _operationLock.WaitAsync();
        try
        {
            return await WithContextAsync(db => db.SettingValues
                .AsNoTracking()
                .OrderBy(x => x.DefinitionKey)
                .ThenBy(x => x.Scope)
                .ThenBy(x => x.ScopeTarget)
                .ToListAsync());
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<SettingValue?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
    {
        await _operationLock.WaitAsync();
        try
        {
            return await WithContextAsync(db => db.SettingValues
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DefinitionKey == key
                    && x.Scope == scope
                    && (x.ScopeTarget ?? string.Empty) == (scopeTarget ?? string.Empty)));
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task UpsertValueAsync(SettingValue value)
        => await UpsertValuesAsync([value]);

    public async Task UpsertValuesAsync(IReadOnlyList<SettingValue> values)
        => await CommitValuesAndAuditAsync(values, []);

    public async Task CommitValuesAndAuditAsync(
        IReadOnlyList<SettingValue> values,
        IReadOnlyList<SettingChangeLog> changeLogs)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(changeLogs);
        if (values.Count == 0 && changeLogs.Count == 0) return;

        for (var index = 0; index < values.Count; index++)
        {
            var duplicate = values.Take(index).Any(existing =>
                existing.Scope == values[index].Scope
                && string.Equals(existing.DefinitionKey, values[index].DefinitionKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.ScopeTarget ?? string.Empty, values[index].ScopeTarget ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new InvalidOperationException($"Lô lưu có khóa cấu hình trùng: '{values[index].DefinitionKey}'.");
        }

        await _operationLock.WaitAsync();
        try
        {
            await WithContextAsync(async db =>
            {
                foreach (var value in values)
                {
                    var existing = await db.SettingValues
                        .FirstOrDefaultAsync(x => x.DefinitionKey == value.DefinitionKey
                            && x.Scope == value.Scope
                            && (x.ScopeTarget ?? string.Empty) == (value.ScopeTarget ?? string.Empty));

                    if (existing is null)
                    {
                        db.SettingValues.Add(value);
                    }
                    else
                    {
                        existing.Value = value.Value;
                        existing.IsEncrypted = value.IsEncrypted;
                        existing.UpdatedAt = value.UpdatedAt;
                        existing.UpdatedBy = value.UpdatedBy;
                    }
                }

                if (changeLogs.Count > 0)
                    db.SettingChangeLogs.AddRange(changeLogs);

                // Settings and their audit trail must commit or roll back as
                // one unit; relational EF providers wrap this SaveChanges call
                // in a transaction when multiple rows are affected.
                await db.SaveChangesAsync();
            });
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task UpsertDefinitionAsync(SettingDefinition definition)
    {
        await _operationLock.WaitAsync();
        try
        {
            await WithContextAsync(async db =>
            {
                var existing = await db.SettingDefinitions
                    .FirstOrDefaultAsync(x => x.Key == definition.Key);

                if (existing is null)
                {
                    db.SettingDefinitions.Add(definition);
                }
                else
                {
                    existing.DisplayName = definition.DisplayName;
                    existing.Description = definition.Description;
                    existing.Category = definition.Category;
                    existing.Group = definition.Group;
                    existing.Scope = definition.Scope;
                    existing.ValueType = definition.ValueType;
                    existing.DefaultValue = definition.DefaultValue;
                    existing.IsRequired = definition.IsRequired;
                    existing.IsSensitive = definition.IsSensitive;
                    existing.IsReadOnly = definition.IsReadOnly;
                    existing.RequiresRestart = definition.RequiresRestart;
                    existing.RequiredPermission = definition.RequiredPermission;
                    existing.SortOrder = definition.SortOrder;
                }

                await db.SaveChangesAsync();
            });
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await action(dbContext);
        }

        return await action(_legacyDbContext
            ?? throw new InvalidOperationException("Settings repository has no DbContext provider."));
    }

    private async Task WithContextAsync(Func<AppDbContext, Task> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await action(dbContext);
            return;
        }

        await action(_legacyDbContext
            ?? throw new InvalidOperationException("Settings repository has no DbContext provider."));
    }
}

public sealed class EfAppModuleRepository : IAppModuleRepository
{
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
    private readonly AppDbContext? _legacyDbContext;

    public EfAppModuleRepository(AppDbContext dbContext)
    {
        _legacyDbContext = dbContext;
    }

    public EfAppModuleRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<AppModule>> GetAppsAsync()
    {
        return await WithContextAsync(db => db.AppModules
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.AppKey)
            .ToListAsync());
    }

    public Task<AppModule?> GetAppAsync(string appKey)
    {
        return WithContextAsync(db => db.AppModules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppKey == appKey));
    }

    public async Task UpsertAppAsync(AppModule app)
    {
        await WithContextAsync(async db =>
        {
            var existing = await db.AppModules
                .FirstOrDefaultAsync(x => x.AppKey == app.AppKey);

            if (existing is null)
            {
                db.AppModules.Add(app);
            }
            else
            {
                existing.Name = app.Name;
                existing.Description = app.Description;
                existing.Icon = app.Icon;
                existing.Version = app.Version;
                existing.IsEnabled = app.IsEnabled;
                existing.ShowOnDesktop = app.ShowOnDesktop;
                existing.PinToTaskbar = app.PinToTaskbar;
                existing.AllowClientAccess = app.AllowClientAccess;
                existing.RequiredPermission = app.RequiredPermission;
                existing.SortOrder = app.SortOrder;
            }

            await db.SaveChangesAsync();
        });
    }

    private async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await action(dbContext);
        }

        return await action(_legacyDbContext
            ?? throw new InvalidOperationException("App module repository has no DbContext provider."));
    }

    private async Task WithContextAsync(Func<AppDbContext, Task> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await action(dbContext);
            return;
        }

        await action(_legacyDbContext
            ?? throw new InvalidOperationException("App module repository has no DbContext provider."));
    }
}

public sealed class EfSettingChangeLogRepository : ISettingChangeLogRepository
{
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
    private readonly AppDbContext? _legacyDbContext;

    public EfSettingChangeLogRepository(AppDbContext dbContext)
    {
        _legacyDbContext = dbContext;
    }

    public EfSettingChangeLogRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(SettingChangeLog log)
    {
        await WithContextAsync(async db =>
        {
            db.SettingChangeLogs.Add(log);
            await db.SaveChangesAsync();
        });
    }

    public async Task<IReadOnlyList<SettingChangeLog>> GetRecentAsync(int take)
    {
        return await WithContextAsync(db => db.SettingChangeLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ChangedAt)
            .Take(Math.Max(0, take))
            .ToListAsync());
    }

    private async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            return await action(dbContext);
        }

        return await action(_legacyDbContext
            ?? throw new InvalidOperationException("Setting change log repository has no DbContext provider."));
    }

    private async Task WithContextAsync(Func<AppDbContext, Task> action)
    {
        if (_dbContextFactory is not null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await action(dbContext);
            return;
        }

        await action(_legacyDbContext
            ?? throw new InvalidOperationException("Setting change log repository has no DbContext provider."));
    }
}
