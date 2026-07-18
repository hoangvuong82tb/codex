using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class WorkspaceService : IWorkspaceService
{
    private const int MaxRecentItems = 20;
    private const string Category = "enterprise-shell";
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentCompanyContext _companyContext;

    public WorkspaceService(AppDbContext dbContext, ICurrentUserService currentUserService, ICurrentCompanyContext companyContext)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _companyContext = companyContext;
    }

    public async Task<WorkspaceStateDto> LoadWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return new WorkspaceStateDto();
        }

        await _companyContext.EnsureInitializedAsync(cancellationToken);
        var companyId = _companyContext.CompanyId;
        var branchId = _companyContext.BranchId;
        var preferences = await _dbContext.UserWorkspacePreferences
            .AsNoTracking()
            .ApplyCompanyBranchScope(companyId, branchId)
            .Where(x => x.UserId == userId.Value && x.Category == Category)
            .ToListAsync(cancellationToken);

        if (preferences.Count == 0)
        {
            preferences = await _dbContext.UserWorkspacePreferences
                .AsNoTracking()
                .ApplyCompanyBranchScope(companyId, null)
                .Where(x => x.UserId == userId.Value && x.Category == Category)
                .ToListAsync(cancellationToken);
        }

        if (preferences.Count == 0)
        {
            preferences = await _dbContext.UserWorkspacePreferences
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value && x.Category == Category && x.CompanyId == null && x.BranchId == null)
                .ToListAsync(cancellationToken);
        }

        return new WorkspaceStateDto
        {
            PinnedModules = ReadList(preferences, "pinned-modules"),
            FavoriteReports = ReadList(preferences, "favorite-reports"),
            RecentModules = ReadList(preferences, "recent-modules"),
            RecentReports = ReadList(preferences, "recent-reports"),
            RecentDrilldowns = ReadList(preferences, "recent-drilldowns"),
            LastOpenedTabs = ReadList(preferences, "last-opened-tabs"),
            ShellLayoutJson = preferences.FirstOrDefault(x => x.PreferenceKey == "shell-layout")?.ValueJson ?? string.Empty
        };
    }

    public Task SavePinnedModulesAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default)
    {
        return SaveListAsync("pinned-modules", DistinctOrdered(moduleKeys), cancellationToken);
    }

    public Task AddRecentModuleAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        return AddRecentAsync("recent-modules", moduleKey, cancellationToken);
    }

    public Task AddFavoriteReportAsync(string reportCode, CancellationToken cancellationToken = default)
    {
        return AddRecentAsync("favorite-reports", reportCode, cancellationToken);
    }

    public Task AddRecentReportAsync(string reportCode, CancellationToken cancellationToken = default)
    {
        return AddRecentAsync("recent-reports", reportCode, cancellationToken);
    }

    public Task AddRecentDrilldownAsync(string drilldownKey, CancellationToken cancellationToken = default)
    {
        return AddRecentAsync("recent-drilldowns", drilldownKey, cancellationToken);
    }

    public Task SaveLastOpenedTabsAsync(IReadOnlyList<string> moduleKeys, CancellationToken cancellationToken = default)
    {
        return SaveListAsync("last-opened-tabs", DistinctOrdered(moduleKeys).Take(MaxRecentItems).ToArray(), cancellationToken);
    }

    public Task SaveShellLayoutAsync(string layoutJson, CancellationToken cancellationToken = default)
    {
        return SaveValueAsync("shell-layout", layoutJson ?? string.Empty, cancellationToken);
    }

    public async Task<WorkspaceSettingsDto> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        var state = await LoadWorkspaceAsync(cancellationToken);
        var settingsJson = await LoadValueAsync("settings", cancellationToken);
        WorkspaceSettingsDto? settings = null;
        if (!string.IsNullOrWhiteSpace(settingsJson))
        {
            try
            {
                settings = JsonSerializer.Deserialize<WorkspaceSettingsDto>(settingsJson);
            }
            catch (JsonException)
            {
                settings = null;
            }
        }

        settings ??= new WorkspaceSettingsDto();
        settings.PinnedModules = state.PinnedModules;
        settings.FavoriteReports = state.FavoriteReports;
        return settings;
    }

    public async Task SaveSettingsAsync(WorkspaceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        await SavePinnedModulesAsync(settings.PinnedModules, cancellationToken);
        await SaveListAsync("favorite-reports", DistinctOrdered(settings.FavoriteReports), cancellationToken);
        await SaveValueAsync("settings", JsonSerializer.Serialize(settings), cancellationToken);
    }

    public async Task ClearWorkspaceCacheAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return;
        }

        await _companyContext.EnsureInitializedAsync(cancellationToken);
        var companyId = _companyContext.CompanyId;
        var branchId = _companyContext.BranchId;
        var rows = await _dbContext.UserWorkspacePreferences
            .ApplyCompanyBranchScope(companyId, branchId)
            .Where(x => x.UserId == userId.Value && x.Category == Category)
            .ToListAsync(cancellationToken);
        _dbContext.UserWorkspacePreferences.RemoveRange(rows);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddRecentAsync(string key, string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var state = await LoadWorkspaceAsync(cancellationToken);
        var current = key switch
        {
            "recent-modules" => state.RecentModules,
            "favorite-reports" => state.FavoriteReports,
            "recent-reports" => state.RecentReports,
            "recent-drilldowns" => state.RecentDrilldowns,
            _ => []
        };

        var merged = new[] { value }
            .Concat(current)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxRecentItems)
            .ToArray();

        await SaveListAsync(key, merged, cancellationToken);
    }

    private Task SaveListAsync(string key, IReadOnlyList<string> values, CancellationToken cancellationToken)
    {
        return SaveValueAsync(key, JsonSerializer.Serialize(values), cancellationToken);
    }

    private async Task SaveValueAsync(string key, string valueJson, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return;
        }

        await _companyContext.EnsureInitializedAsync(cancellationToken);
        var companyId = _companyContext.CompanyId;
        var branchId = _companyContext.BranchId;
        var preference = await _dbContext.UserWorkspacePreferences
            .ApplyCompanyBranchScope(companyId, branchId)
            .FirstOrDefaultAsync(x => x.UserId == userId.Value && x.Category == Category && x.PreferenceKey == key, cancellationToken);

        if (preference is null)
        {
            preference = new UserWorkspacePreference
            {
                UserId = userId.Value,
                CompanyId = companyId,
                BranchId = branchId,
                Category = Category,
                PreferenceKey = key
            };
            _dbContext.UserWorkspacePreferences.Add(preference);
        }

        preference.ValueJson = valueJson;
        preference.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> LoadValueAsync(string key, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return string.Empty;
        }

        await _companyContext.EnsureInitializedAsync(cancellationToken);
        var companyId = _companyContext.CompanyId;
        var branchId = _companyContext.BranchId;
        var value = await _dbContext.UserWorkspacePreferences
            .AsNoTracking()
            .ApplyCompanyBranchScope(companyId, branchId)
            .Where(x => x.UserId == userId.Value && x.Category == Category && x.PreferenceKey == key)
            .Select(x => x.ValueJson)
            .FirstOrDefaultAsync(cancellationToken);

        return value ?? await _dbContext.UserWorkspacePreferences
            .AsNoTracking()
            .ApplyCompanyBranchScope(companyId, null)
            .Where(x => x.UserId == userId.Value && x.Category == Category && x.PreferenceKey == key)
            .Select(x => x.ValueJson)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _dbContext.UserWorkspacePreferences
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value && x.Category == Category && x.PreferenceKey == key && x.CompanyId == null && x.BranchId == null)
                .Select(x => x.ValueJson)
                .FirstOrDefaultAsync(cancellationToken)
            ?? string.Empty;
    }

    private Guid? GetUserId()
    {
        return _currentUserService.CurrentUser?.Id;
    }

    private static IReadOnlyList<string> ReadList(IReadOnlyList<UserWorkspacePreference> preferences, string key)
    {
        var json = preferences.FirstOrDefault(x => x.PreferenceKey == key)?.ValueJson;
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> DistinctOrdered(IEnumerable<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
