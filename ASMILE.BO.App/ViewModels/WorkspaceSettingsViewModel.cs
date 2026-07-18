using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class WorkspaceSettingsViewModel : ViewModelBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly INavigationService _navigationService;
    private readonly List<WorkspaceModuleSettingRow> _allModules = new();

    public ObservableCollection<WorkspaceModuleSettingRow> Modules { get; } = new();
    public ObservableCollection<WorkspaceModuleSettingRow> FilteredModules { get; } = new();
    public ObservableCollection<string> FavoriteReports { get; } = new();

    [ObservableProperty]
    private int recentHistoryLimit = 20;

    [ObservableProperty]
    private bool restoreLastSession = true;

    [ObservableProperty]
    private string defaultLandingModule = "dashboard";

    [ObservableProperty]
    private string gridDensity = "Comfortable";

    [ObservableProperty]
    private bool compactMode;

    [ObservableProperty]
    private string newFavoriteReport = string.Empty;

    [ObservableProperty]
    private string moduleSearchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Sẵn sàng";

    public WorkspaceSettingsViewModel(IWorkspaceService workspaceService, INavigationService navigationService)
    {
        _workspaceService = workspaceService;
        _navigationService = navigationService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task Load()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        var settings = new WorkspaceSettingsDto
        {
            PinnedModules = _allModules.Where(x => x.IsPinned).Select(x => x.Key).ToArray(),
            FavoriteReports = FavoriteReports.ToArray(),
            RecentHistoryLimit = Math.Clamp(RecentHistoryLimit, 5, 100),
            RestoreLastSession = RestoreLastSession,
            DefaultLandingModule = DefaultLandingModule,
            GridDensity = GridDensity,
            CompactMode = CompactMode
        };
        await _workspaceService.SaveSettingsAsync(settings);
        StatusMessage = "Đã lưu thiết lập không gian làm việc.";
    }

    [RelayCommand]
    private void AddFavoriteReport()
    {
        if (string.IsNullOrWhiteSpace(NewFavoriteReport)
            || FavoriteReports.Any(x => string.Equals(x, NewFavoriteReport, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        FavoriteReports.Insert(0, NewFavoriteReport.Trim());
        NewFavoriteReport = string.Empty;
    }

    [RelayCommand]
    private void RemoveFavoriteReport(string? reportCode)
    {
        if (reportCode is not null)
        {
            FavoriteReports.Remove(reportCode);
        }
    }

    [RelayCommand]
    private async Task ClearWorkspaceCache()
    {
        await _workspaceService.ClearWorkspaceCacheAsync();
        await LoadAsync();
        StatusMessage = "Đã xóa bộ nhớ đệm workspace.";
    }

    partial void OnModuleSearchTextChanged(string value)
    {
        ApplyModuleFilter();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var settings = await _workspaceService.LoadSettingsAsync();
            RecentHistoryLimit = settings.RecentHistoryLimit;
            RestoreLastSession = settings.RestoreLastSession;
            DefaultLandingModule = settings.DefaultLandingModule;
            GridDensity = settings.GridDensity;
            CompactMode = settings.CompactMode;

            var pinned = new HashSet<string>(settings.PinnedModules, StringComparer.OrdinalIgnoreCase);
            Modules.Clear();
            FilteredModules.Clear();
            _allModules.Clear();
            foreach (var module in _navigationService.GetModuleCatalog().OrderBy(x => x.Title))
            {
                var row = new WorkspaceModuleSettingRow(module, pinned.Contains(module.Key));
                Modules.Add(row);
                _allModules.Add(row);
            }
            ApplyModuleFilter();

            FavoriteReports.Clear();
            foreach (var report in settings.FavoriteReports)
            {
                FavoriteReports.Add(report);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyModuleFilter()
    {
        var keyword = ModuleSearchText.Trim();
        var rows = string.IsNullOrWhiteSpace(keyword)
            ? _allModules
            : _allModules.Where(x =>
                x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Subtitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        FilteredModules.Clear();
        foreach (var row in rows)
        {
            FilteredModules.Add(row);
        }
    }
}

public sealed partial class WorkspaceModuleSettingRow : ObservableObject
{
    public WorkspaceModuleSettingRow(AppModuleInfo module, bool isPinned)
    {
        Key = module.Key;
        Title = module.Title;
        Subtitle = module.Subtitle;
        IsPinned = isPinned;
    }

    public string Key { get; }
    public string Title { get; }
    public string Subtitle { get; }

    [ObservableProperty]
    private bool isPinned;
}
