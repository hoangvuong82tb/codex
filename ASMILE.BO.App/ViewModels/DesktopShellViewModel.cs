using ASMILE.BO.App.Infrastructure;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Core.Entities.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class DesktopShellViewModel : ViewModelBase, IDisposable
{
    private const string ModuleIconRoot = @"D:\ASMILE-BO\Windows_Icon";
    private const double DesktopIconStartX = 18d;
    private const double DesktopIconStartY = 18d;
    private const double DesktopIconStepX = 108d;
    private const double DesktopIconStepY = 102d;
    private const double DesktopIconWidth = 104d;
    private const double DesktopIconHeight = 96d;
    private const double DesktopIconOuterPadding = 14d;
    private const double DesktopTaskbarReservedHeight = 86d;

    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IShellStateService _shellStateService;
    private readonly IDesktopLayoutService _desktopLayoutService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentCompanyContext _currentCompanyContext;
    private readonly IFrontOfficePropertyContextHost _frontOfficePropertyContextHost;
    private readonly IGlobalSearchService _globalSearchService;
    private readonly ISettingsChangeNotifier _settingsChangeNotifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _toastTimer;
    private readonly DispatcherTimer _clockTimer;
    private readonly SemaphoreSlim _navigationGate = new(1, 1);
    private CancellationTokenSource? _searchCancellation;
    private bool _disposed;
    private bool _loadingCompanyContext;

    public ObservableCollection<AppModuleInfo> AllModules { get; } = new();
    public ObservableCollection<AppModuleInfo> RecentModules { get; } = new();
    public ObservableCollection<AppModuleInfo> FavoriteModules { get; } = new();
    public ObservableCollection<GlobalSearchResultDto> GlobalSearchResults { get; } = new();
    public ObservableCollection<StartRecommendedItem> RecommendedItems { get; } = new();
    public ObservableCollection<CompanyContextOptionDto> AvailableCompanies { get; } = new();
    public ObservableCollection<BranchContextOptionDto> AvailableBranches { get; } = new();

    public ObservableCollection<AppWindowViewModel> OpenWindows => _windowManagerService.OpenWindows;

    public IEnumerable<AppModuleInfo> DesktopModules => AreDesktopIconsVisible ? AllModules.Where(x => x.ShowOnDesktop) : [];
    public IEnumerable<AppModuleInfo> PinnedModules => AllModules.Where(x => x.IsPinned);
    public IEnumerable<AppModuleInfo> TaskbarPinnedModules => PinnedModules.Take(8);
    public IEnumerable<AppModuleInfo> StartFilteredPinnedModules => PinnedModules.Where(ApplyStartFilter);
    public IEnumerable<AppModuleInfo> StartFilteredAllModules => AllModules.Where(ApplyStartFilter);
    public IEnumerable<AppModuleInfo> ShellReportModules => AllModules
        .Where(x => x.Key.Contains("report", StringComparison.OrdinalIgnoreCase)
                    || x.Title.Contains("báo cáo", StringComparison.OrdinalIgnoreCase)
                    || x.Title.Contains("report", StringComparison.OrdinalIgnoreCase))
        .Take(6);
    public IEnumerable<AppWindowViewModel> TaskbarRunningWindows => OpenWindows.Where(x => !x.Module.IsPinned);

    [ObservableProperty]
    private AppModuleInfo? selectedDesktopModule;

    [ObservableProperty]
    private AppWindowViewModel? activeWindow;

    [ObservableProperty]
    private bool isStartMenuOpen;

    [ObservableProperty]
    private bool areDesktopIconsVisible = true;

    [ObservableProperty]
    private string startSearchText = string.Empty;

    [ObservableProperty]
    private string globalSearchText = string.Empty;

    [ObservableProperty]
    private bool isGlobalSearchOpen;

    [ObservableProperty]
    private string breadcrumbText = "ASMILE BO";

    [ObservableProperty]
    private string shellStatusMessage = "Sẵn sàng";

    [ObservableProperty]
    private bool isShellBusy;

    [ObservableProperty]
    private string toastMessage = string.Empty;

    [ObservableProperty]
    private bool isToastVisible;

    [ObservableProperty]
    private string currentUserName = "Administrator";

    [ObservableProperty]
    private string currentCompanyStatus = "Default Company";

    [ObservableProperty]
    private CompanyContextOptionDto? selectedCompany;

    [ObservableProperty]
    private BranchContextOptionDto? selectedBranch;

    [ObservableProperty]
    private string systemClock = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private string systemDate = DateTime.Now.ToString("dd/MM/yyyy");

    [ObservableProperty]
    private double workspaceWidth = 1320;

    [ObservableProperty]
    private double workspaceHeight = 760;

    public DesktopShellViewModel(
        INavigationService navigationService,
        INotificationService notificationService,
        IWindowManagerService windowManagerService,
        IShellStateService shellStateService,
        IDesktopLayoutService desktopLayoutService,
        ICurrentUserService currentUserService,
        ICurrentCompanyContext currentCompanyContext,
        IFrontOfficePropertyContextHost frontOfficePropertyContextHost,
        IGlobalSearchService globalSearchService,
        ISettingsChangeNotifier settingsChangeNotifier,
        IServiceScopeFactory scopeFactory)
    {
        _navigationService = navigationService;
        _notificationService = notificationService;
        _windowManagerService = windowManagerService;
        _shellStateService = shellStateService;
        _desktopLayoutService = desktopLayoutService;
        _currentUserService = currentUserService;
        _currentCompanyContext = currentCompanyContext;
        _frontOfficePropertyContextHost = frontOfficePropertyContextHost;
        _globalSearchService = globalSearchService;
        _settingsChangeNotifier = settingsChangeNotifier;
        _scopeFactory = scopeFactory;

        IsStartMenuOpen = _shellStateService.IsStartMenuOpen;
        WorkspaceWidth = _desktopLayoutService.WorkspaceWidth;
        WorkspaceHeight = _desktopLayoutService.WorkspaceHeight;
        CurrentUserName = _currentUserService.CurrentUser?.DisplayName ?? "Administrator";

        OpenWindows.CollectionChanged += OnOpenWindowsCollectionChanged;

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += (_, _) =>
        {
            IsToastVisible = false;
            _toastTimer.Stop();
        };

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            SystemClock = DateTime.Now.ToString("HH:mm");
            SystemDate = DateTime.Now.ToString("dd/MM/yyyy");
        };
        _clockTimer.Start();

        _notificationService.MessageRaised += OnMessageRaised;
        _navigationService.Navigated += OnNavigated;
        _settingsChangeNotifier.SettingChanged += OnSettingChanged;

        QueueShellTask(InitializeShellAsync);
    }

    partial void OnSelectedCompanyChanged(CompanyContextOptionDto? value)
    {
        if (_loadingCompanyContext || value is null || value.Id == _currentCompanyContext.CompanyId)
        {
            return;
        }

        QueueShellTask(async () =>
        {
            await _currentCompanyContext.SetCompanyAsync(value.Id);
            await _frontOfficePropertyContextHost.InitializeAsync();
            await LoadCompanyContextAsync();
            await RefreshWorkspaceListsAsync();
            CurrentCompanyStatus = BuildCompanyStatus();
            ShellStatusMessage = $"Company: {_currentCompanyContext.CompanyName}";
        });
    }

    partial void OnSelectedBranchChanged(BranchContextOptionDto? value)
    {
        if (_loadingCompanyContext)
        {
            return;
        }

        QueueShellTask(async () =>
        {
            if (value is null)
            {
                await _frontOfficePropertyContextHost.ClearCurrentPropertyAsync();
            }
            else
            {
                await _frontOfficePropertyContextHost.SetCurrentPropertyAsync(value.Id);
            }
            CurrentCompanyStatus = BuildCompanyStatus();
            ShellStatusMessage = value is null ? $"Company: {_currentCompanyContext.CompanyName}" : $"Branch: {value.Name}";
        });
    }

    partial void OnStartSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(StartFilteredPinnedModules));
        OnPropertyChanged(nameof(StartFilteredAllModules));
    }

    partial void OnGlobalSearchTextChanged(string value)
    {
        if (!string.Equals(StartSearchText, value, StringComparison.Ordinal))
        {
            StartSearchText = value;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        QueueShellTask(() => RunDebouncedSearchAsync(value, _searchCancellation.Token));
    }

    partial void OnIsStartMenuOpenChanged(bool value)
    {
        _shellStateService.IsStartMenuOpen = value;
    }

    partial void OnAreDesktopIconsVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(DesktopModules));
    }

    [RelayCommand]
    private void ToggleStartMenu()
    {
        IsStartMenuOpen = !IsStartMenuOpen;
    }

    [RelayCommand]
    private void CloseStartMenu()
    {
        IsStartMenuOpen = false;
        IsGlobalSearchOpen = false;
    }

    [RelayCommand]
    private async Task OpenModule(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        await OpenModuleByKeyAsync(module.Key);
        IsStartMenuOpen = false;
    }

    [RelayCommand]
    private async Task OpenModuleKey(string? moduleKey)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            return;
        }

        await OpenModuleByKeyAsync(moduleKey);
        IsStartMenuOpen = false;
    }

    [RelayCommand]
    private async Task OpenDesktopModule(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        SelectDesktopModule(module);
        await OpenModuleByKeyAsync(module.Key);
    }

    [RelayCommand]
    private async Task DesktopIconClick(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        foreach (var item in AllModules)
        {
            item.IsSelected = false;
        }

        SelectDesktopModule(module);
        await OpenModuleByKeyAsync(module.Key);
    }

    [RelayCommand]
    private async Task RefreshDesktop()
    {
        await LoadAppearanceSettingsAsync();
        await BuildModuleCatalogAsync();
        await RestoreWorkspaceAsync();
        BuildRecommendedItems();
        SyncShellState();
        ShowToast("Đã làm mới desktop.");
    }

    [RelayCommand]
    private void ArrangeDesktopIcons()
    {
        ArrangeDesktopIconsCore();
        SyncShellState();
        ShowToast("Đã sắp xếp biểu tượng desktop.");
    }

    [RelayCommand]
    private async Task ToggleDesktopIcons()
    {
        AreDesktopIconsVisible = !AreDesktopIconsVisible;
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        await settingsService.SaveValueAsync(new SaveSettingValueRequest
        {
            Key = "Appearance.ShowDesktopIcons",
            Scope = SettingScope.User,
            ScopeTarget = _currentUserService.CurrentUser?.Id.ToString(),
            Value = AreDesktopIconsVisible ? "true" : "false",
            Reason = "Desktop context menu"
        });
        ShowToast(AreDesktopIconsVisible ? "Đã hiện biểu tượng desktop." : "Đã ẩn biểu tượng desktop.");
    }

    [RelayCommand]
    private async Task PinModuleToTaskbar(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        module.IsPinned = true;
        await SavePinnedModulesAsync();
        OnModuleCollectionsChanged();
        ShowToast($"Đã ghim {module.Title} vào taskbar.");
    }

    [RelayCommand]
    private async Task UnpinModuleFromTaskbar(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        module.IsPinned = false;
        await SavePinnedModulesAsync();
        OnModuleCollectionsChanged();
        ShowToast($"Đã bỏ ghim {module.Title} khỏi taskbar.");
    }

    [RelayCommand]
    private async Task ToggleModuleTaskbarPin(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        if (module.IsPinned)
        {
            await UnpinModuleFromTaskbar(module);
            return;
        }

        await PinModuleToTaskbar(module);
    }

    [RelayCommand]
    private async Task HideModuleFromDesktop(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        module.ShowOnDesktop = false;
        await SaveModuleDesktopVisibilityAsync(module, false);
        OnModuleCollectionsChanged();
        ShowToast($"Đã ẩn {module.Title} khỏi desktop.");
    }

    [RelayCommand]
    private void SnapModuleIconToGrid(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        SnapDesktopIconToGrid(module);
        ShowToast("Đã căn biểu tượng vào lưới.");
    }

    [RelayCommand]
    private void CopyModuleName(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        Clipboard.SetText(module.Title);
        ShowToast("Đã sao chép tên module.");
    }

    [RelayCommand]
    private void ShowModuleProperties(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        SelectDesktopModule(module);
        ShowToast($"{module.Title} | Route: {module.Key} | {(module.IsPinned ? "Đang ghim taskbar" : "Chưa ghim taskbar")}");
    }

    [RelayCommand]
    private async Task OpenModuleSettings(AppModuleInfo? module)
    {
        await OpenModuleByKeyAsync("settings");
        if (module is not null)
        {
            var settingsWindow = OpenWindows.FirstOrDefault(x => string.Equals(x.Module.Key, "settings", StringComparison.OrdinalIgnoreCase));
            if (settingsWindow?.ContentViewModel is SettingsViewModel settingsViewModel)
            {
                await settingsViewModel.OpenAppearanceIconSettingsAsync(module.Key);
            }

            ShowToast($"Mở cài đặt icon cho {module.Title}.");
        }
    }

    [RelayCommand]
    private async Task OpenAppearanceSettings()
    {
        await OpenModuleByKeyAsync("settings");
        var settingsWindow = OpenWindows.FirstOrDefault(x => string.Equals(x.Module.Key, "settings", StringComparison.OrdinalIgnoreCase));
        if (settingsWindow?.ContentViewModel is SettingsViewModel settingsViewModel)
        {
            await settingsViewModel.OpenAppearanceIconSettingsAsync(null);
        }

        ShowToast("Mở Cài đặt > Giao diện để chỉnh desktop.");
    }

    [RelayCommand]
    private async Task TaskbarModuleInvoke(AppModuleInfo? module)
    {
        if (module is null)
        {
            return;
        }

        var existing = OpenWindows.FirstOrDefault(x => x.Module.Key == module.Key);
        if (existing is null)
        {
            await OpenModuleByKeyAsync(module.Key);
            return;
        }

        _windowManagerService.ToggleFromTaskbar(existing);
        SyncShellState();
    }

    [RelayCommand]
    private void ToggleTaskbarWindow(AppWindowViewModel? window)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.ToggleFromTaskbar(window);
        SyncShellState();
    }

    [RelayCommand]
    private void ActivateWindow(AppWindowViewModel? window)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.Activate(window);
        SyncShellState();
    }

    [RelayCommand]
    private void MinimizeWindow(AppWindowViewModel? window)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.Minimize(window);
        SyncShellState();
    }

    [RelayCommand]
    private void ToggleMaximizeWindow(AppWindowViewModel? window)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.ToggleMaximize(window);
        SyncShellState();
    }

    [RelayCommand]
    private void CloseWindow(AppWindowViewModel? window)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.Close(window);
        QueueShellTask(SaveOpenTabsAsync);
        SyncShellState();
    }

    [RelayCommand]
    private async Task QuickAction(string actionKey)
    {
        switch (actionKey)
        {
            case "all-apps":
                StartSearchText = string.Empty;
                break;
            case "open-settings":
                await OpenModuleByKeyAsync("settings");
                break;
            case "open-reports":
                await OpenModuleByKeyAsync("stock-report");
                break;
            case "logout":
                IsStartMenuOpen = false;
                await _frontOfficePropertyContextHost.ClearCurrentPropertyAsync();
                ShowToast("Đã đăng xuất (mô phỏng).");
                break;
        }
    }

    [RelayCommand]
    private async Task OpenRecommendedItem(StartRecommendedItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.ModuleKey))
        {
            await OpenModuleByKeyAsync(item.ModuleKey);
            IsStartMenuOpen = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.QuickActionKey))
        {
            await QuickAction(item.QuickActionKey);
        }
    }

    [RelayCommand]
    private async Task OpenGlobalSearchResult(GlobalSearchResultDto? result)
    {
        if (result is null)
        {
            return;
        }

        IsGlobalSearchOpen = false;
        GlobalSearchText = string.Empty;

        if (result.TargetType.Equals("journal", StringComparison.OrdinalIgnoreCase) && result.TargetId.HasValue)
        {
            await _navigationService.OpenJournalAsync(result.TargetId.Value);
        }
        else if (result.TargetType.Equals("voucher", StringComparison.OrdinalIgnoreCase) && result.TargetId.HasValue && !string.IsNullOrWhiteSpace(result.SourceType))
        {
            await _navigationService.OpenVoucherAsync(result.SourceType, result.TargetId.Value);
        }
        else if (result.TargetType.Equals("report", StringComparison.OrdinalIgnoreCase))
        {
            await _navigationService.OpenReportAsync(result.TargetRoute);
        }
        else
        {
            await OpenModuleByKeyAsync(result.TargetRoute);
        }

        await RefreshWorkspaceListsAsync();
        SyncShellState();
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await _navigationService.GoBackAsync();
        SyncShellState();
    }

    [RelayCommand]
    private async Task GoForward()
    {
        await _navigationService.GoForwardAsync();
        SyncShellState();
    }

    [RelayCommand]
    private void FocusGlobalSearch()
    {
        IsGlobalSearchOpen = true;
    }

    [RelayCommand]
    private void NextTab()
    {
        ActivateRelativeTab(1);
    }

    [RelayCommand]
    private void PreviousTab()
    {
        ActivateRelativeTab(-1);
    }

    public void UpdateWorkspaceBounds(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        WorkspaceWidth = width;
        WorkspaceHeight = height;
        _desktopLayoutService.UpdateBounds(width, height);
        _windowManagerService.UpdateWorkspaceBounds(width, height);

        if (AllModules.Any(x => x.ShowOnDesktop && x.DesktopTop > GetMaxDesktopIconTop()))
        {
            ArrangeDesktopIconsCore();
        }

        SyncShellState();
    }

    public void MoveWindow(AppWindowViewModel? window, double left, double top)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.MoveWindow(window, left, top);
        SyncShellState();
    }

    public void ResizeWindow(AppWindowViewModel? window, double width, double height)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.ResizeWindow(window, width, height);
        SyncShellState();
    }

    public void ResizeWindowEdges(AppWindowViewModel? window, double deltaLeft, double deltaTop, double deltaRight, double deltaBottom)
    {
        if (window is null)
        {
            return;
        }

        _windowManagerService.ResizeWindowEdges(window, deltaLeft, deltaTop, deltaRight, deltaBottom);
        SyncShellState();
    }

    public void MoveDesktopIcon(AppModuleInfo module, double left, double top)
    {
        if (module is null || !module.ShowOnDesktop)
        {
            return;
        }

        var minLeft = DesktopIconOuterPadding;
        var minTop = DesktopIconOuterPadding;
        var maxLeft = Math.Max(minLeft, WorkspaceWidth - DesktopIconWidth - DesktopIconOuterPadding);
        var maxTop = GetMaxDesktopIconTop();

        module.DesktopLeft = Math.Clamp(left, minLeft, maxLeft);
        module.DesktopTop = Math.Clamp(top, minTop, maxTop);
    }

    public void SnapDesktopIconToGrid(AppModuleInfo module)
    {
        if (module is null || !module.ShowOnDesktop)
        {
            return;
        }

        var col = Math.Max(0, (int)Math.Round((module.DesktopLeft - DesktopIconStartX) / DesktopIconStepX));
        var row = Math.Max(0, (int)Math.Round((module.DesktopTop - DesktopIconStartY) / DesktopIconStepY));
        var rowsPerColumn = GetDesktopRowsPerColumn();

        if (row >= rowsPerColumn)
        {
            col += row / rowsPerColumn;
            row %= rowsPerColumn;
        }

        MoveDesktopIcon(module, DesktopIconStartX + (col * DesktopIconStepX), DesktopIconStartY + (row * DesktopIconStepY));
    }

    public void RefreshAfterLogin()
    {
        CurrentUserName = _currentUserService.CurrentUser?.DisplayName ?? "Administrator";
        QueueShellTask(InitializeShellAsync);
    }

    private async Task InitializeShellAsync()
    {
        IsShellBusy = true;
        try
        {
            await LoadCompanyContextAsync();
            await _frontOfficePropertyContextHost.InitializeAsync();
            await LoadCompanyContextAsync();
            await LoadAppearanceSettingsAsync();
            await BuildModuleCatalogAsync();
            await RestoreWorkspaceAsync();
            BuildRecommendedItems();
            ArrangeDesktopIconsCore();

            if (!OpenWindows.Any())
            {
                var restoreTargets = await _navigationService.RestoreWorkspaceAsync();
                var firstRestored = restoreTargets.FirstOrDefault(x => AllModules.Any(m => m.Key == x.Target.Route));
                await OpenModuleByKeyAsync(firstRestored?.Target.Route ?? "dashboard");
            }
            else
            {
                SyncShellState();
            }
        }
        finally
        {
            IsShellBusy = false;
        }
    }

    private async Task LoadCompanyContextAsync()
    {
        _loadingCompanyContext = true;
        try
        {
            await _currentCompanyContext.EnsureInitializedAsync();
            AvailableCompanies.Clear();
            foreach (var company in await _currentCompanyContext.GetCompaniesAsync())
            {
                AvailableCompanies.Add(company);
            }

            AvailableBranches.Clear();
            foreach (var branch in await _currentCompanyContext.GetBranchesAsync())
            {
                AvailableBranches.Add(branch);
            }

            SelectedCompany = AvailableCompanies.FirstOrDefault(x => x.Id == _currentCompanyContext.CompanyId);
            SelectedBranch = AvailableBranches.FirstOrDefault(x => x.Id == _currentCompanyContext.BranchId);
            CurrentCompanyStatus = BuildCompanyStatus();
        }
        finally
        {
            _loadingCompanyContext = false;
        }
    }

    private string BuildCompanyStatus()
    {
        return string.IsNullOrWhiteSpace(_currentCompanyContext.BranchName)
            ? _currentCompanyContext.CompanyName
            : $"{_currentCompanyContext.CompanyName} / {_currentCompanyContext.BranchName}";
    }

    private async Task BuildModuleCatalogAsync()
    {
        var previousState = AllModules.ToDictionary(x => x.Key, x => new
        {
            x.IsPinned,
            x.ShowOnDesktop,
            x.IsSelected,
            x.IsRunning,
            x.IsActiveOnTaskbar,
            x.IsMinimizedOnTaskbar,
            x.DesktopLeft,
            x.DesktopTop
        }, StringComparer.OrdinalIgnoreCase);
        AllModules.Clear();
        using var scope = _scopeFactory.CreateScope();
        var appModuleService = scope.ServiceProvider.GetRequiredService<IAppModuleService>();
        var appModules = (await appModuleService.GetAppsAsync()).ToDictionary(x => x.AppKey, StringComparer.OrdinalIgnoreCase);

        foreach (var module in _navigationService.GetModuleCatalog())
        {
            var iconPath = appModules.TryGetValue(module.Key, out var app)
                ? ResolveModuleIconPath(app.Icon) ?? module.IconPath
                : module.IconPath;
            var displayModule = new AppModuleInfo(
                module.Key,
                module.Title,
                module.IconGlyph,
                module.Subtitle,
                module.AccentHex,
                module.IsPinned,
                module.ShowOnDesktop,
                iconPath);
            if (previousState.TryGetValue(displayModule.Key, out var state))
            {
                displayModule.IsPinned = state.IsPinned;
                displayModule.ShowOnDesktop = state.ShowOnDesktop;
                displayModule.IsSelected = state.IsSelected;
                displayModule.IsRunning = state.IsRunning;
                displayModule.IsActiveOnTaskbar = state.IsActiveOnTaskbar;
                displayModule.IsMinimizedOnTaskbar = state.IsMinimizedOnTaskbar;
                displayModule.DesktopLeft = state.DesktopLeft;
                displayModule.DesktopTop = state.DesktopTop;
            }

            if (await HasModulePermissionAsync(displayModule))
            {
                AllModules.Add(displayModule);
            }
        }

        OnModuleCollectionsChanged();
    }

    private static string? ResolveModuleIconPath(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }

        if (File.Exists(icon))
        {
            return Path.GetFullPath(icon);
        }

        var iconRoot = ModuleIconRoot;
        var fileName = Path.GetFileName(icon);
        if (string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(iconRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(iconRoot, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }

    private async Task RestoreWorkspaceAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var state = await workspace.LoadWorkspaceAsync();
        var pinned = state.PinnedModules.Count > 0
            ? new HashSet<string>(state.PinnedModules, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(AllModules.Where(x => x.IsPinned).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);

        foreach (var module in AllModules)
        {
            module.IsPinned = pinned.Contains(module.Key);
        }

        RefreshModuleCollection(RecentModules, state.RecentModules);
        RefreshModuleCollection(FavoriteModules, state.PinnedModules);
        OnModuleCollectionsChanged();
    }

    private async Task OpenModuleByKeyAsync(string key)
    {
        if (!await _navigationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
        var module = AllModules.FirstOrDefault(x => x.Key == key);
        if (module is null)
        {
            return;
        }

        var permission = await CheckModulePermissionAsync(module);
        if (!permission.IsAllowed)
        {
            ShellStatusMessage = permission.Reason;
            ShowToast(permission.Reason);
            return;
        }

        var result = await _navigationService.OpenModuleAsync(module.Key);
        if (result.Success)
        {
            BreadcrumbText = $"ASMILE BO > {module.Title}";
            ShellStatusMessage = $"Đã mở {module.Title}";
            await SaveOpenTabsAsync();
            await RefreshWorkspaceListsAsync();
        }
        else if (!string.IsNullOrWhiteSpace(result.Message))
        {
            ShellStatusMessage = result.Message;
            ShowToast(result.Message);
        }

        SyncShellState();
        }
        finally
        {
            _navigationGate.Release();
        }
    }

    private async Task<bool> HasModulePermissionAsync(AppModuleInfo module)
    {
        var result = await CheckModulePermissionAsync(module);
        return result.IsAllowed;
    }

    private async Task<PermissionCheckResultDto> CheckModulePermissionAsync(AppModuleInfo module)
    {
        if (string.Equals(module.Key, "front-office-reservations", StringComparison.OrdinalIgnoreCase)
            && !_frontOfficePropertyContextHost.HasCurrentProperty)
        {
            return new PermissionCheckResultDto
            {
                IsAllowed = false,
                PermissionCode = "FrontOffice.Reservation.View",
                Reason = "Vui long chon chi nhanh truoc khi mo module Dat phong."
            };
        }

        using var scope = _scopeFactory.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissionService.CheckAsync(_navigationService.GetPermissionCode(module.Key));
    }

    private async Task RefreshWorkspaceListsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var state = await workspace.LoadWorkspaceAsync();
        RefreshModuleCollection(RecentModules, state.RecentModules);
        RefreshModuleCollection(FavoriteModules, state.PinnedModules);
    }

    private async Task SaveOpenTabsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        await workspace.SaveLastOpenedTabsAsync(OpenWindows.Select(x => x.Module.Key).ToArray());
    }

    private async Task RunDebouncedSearchAsync(string value, CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);
        GlobalSearchResults.Clear();

        if (string.IsNullOrWhiteSpace(value))
        {
            IsGlobalSearchOpen = false;
            return;
        }

        IsGlobalSearchOpen = true;
        var results = await _globalSearchService.SearchAsync(value, cancellationToken);
        foreach (var result in results)
        {
            GlobalSearchResults.Add(result);
        }
    }

    private void RefreshModuleCollection(ObservableCollection<AppModuleInfo> target, IReadOnlyList<string> keys)
    {
        target.Clear();
        foreach (var key in keys)
        {
            var module = AllModules.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (module is not null && target.All(x => x.Key != module.Key))
            {
                target.Add(module);
            }
        }
    }

    private bool ApplyStartFilter(AppModuleInfo module)
    {
        if (string.IsNullOrWhiteSpace(StartSearchText))
        {
            return true;
        }

        return module.Title.Contains(StartSearchText, StringComparison.OrdinalIgnoreCase)
               || module.Subtitle.Contains(StartSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void BuildRecommendedItems()
    {
        RecommendedItems.Clear();
        AddRecommendedIfAllowed("\uE80F", "Mở Tổng quan", "Truy cập dashboard vận hành", "dashboard");
        AddRecommendedIfAllowed("\uE8A7", "Phiếu nhập gần đây", "Tạo và kiểm tra chứng từ nhập", "receipts");
        AddRecommendedIfAllowed("\uE9D2", "Báo cáo tồn kho", "Theo dõi tồn kho toàn hệ thống", "stock-report");
        AddRecommendedIfAllowed("\uE8A5", "Danh sách khách hàng", "Tra cứu hồ sơ và lịch sử giao dịch", "customers");
        AddRecommendedIfAllowed("\uE713", "Cài đặt hệ thống", "Điều chỉnh tham số vận hành", "settings");
    }

    private void AddRecommendedIfAllowed(string glyph, string title, string subtitle, string moduleKey)
    {
        if (AllModules.Any(x => x.Key == moduleKey))
        {
            RecommendedItems.Add(new StartRecommendedItem(glyph, title, subtitle, moduleKey: moduleKey));
        }
    }

    private void ArrangeDesktopIconsCore()
    {
        var rowsPerColumn = GetDesktopRowsPerColumn();
        var desktopApps = AllModules.Where(x => x.ShowOnDesktop).ToList();
        for (var i = 0; i < desktopApps.Count; i++)
        {
            var col = i / rowsPerColumn;
            var row = i % rowsPerColumn;
            desktopApps[i].DesktopLeft = DesktopIconStartX + (col * DesktopIconStepX);
            desktopApps[i].DesktopTop = DesktopIconStartY + (row * DesktopIconStepY);
        }
    }

    private double GetMaxDesktopIconTop()
    {
        return Math.Max(DesktopIconOuterPadding, WorkspaceHeight - DesktopIconHeight - DesktopTaskbarReservedHeight);
    }

    private int GetDesktopRowsPerColumn()
    {
        var usableHeight = Math.Max(DesktopIconHeight, GetMaxDesktopIconTop() - DesktopIconStartY + DesktopIconHeight);
        return Math.Max(1, (int)Math.Floor(usableHeight / DesktopIconStepY));
    }

    private void SelectDesktopModule(AppModuleInfo module)
    {
        foreach (var item in AllModules)
        {
            item.IsSelected = false;
        }

        module.IsSelected = true;
        SelectedDesktopModule = module;
    }

    private async Task LoadAppearanceSettingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var value = await settingsService.GetValueAsync("Appearance.ShowDesktopIcons", SettingScope.User, _currentUserService.CurrentUser?.Id.ToString());
        AreDesktopIconsVisible = !string.Equals(value?.Value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SavePinnedModulesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        await workspace.SavePinnedModulesAsync(AllModules.Where(x => x.IsPinned).Select(x => x.Key).ToArray());
    }

    private async Task SaveModuleDesktopVisibilityAsync(AppModuleInfo module, bool showOnDesktop)
    {
        using var scope = _scopeFactory.CreateScope();
        var appModuleService = scope.ServiceProvider.GetRequiredService<IAppModuleService>();
        var app = await appModuleService.GetAppAsync(module.Key);
        if (app is null)
        {
            return;
        }

        await appModuleService.UpdateAppAsync(new UpdateAppModuleRequest
        {
            AppKey = app.AppKey,
            Icon = app.Icon,
            IsEnabled = app.IsEnabled,
            ShowOnDesktop = showOnDesktop,
            PinToTaskbar = app.PinToTaskbar,
            AllowClientAccess = app.AllowClientAccess,
            RequiredPermission = app.RequiredPermission,
            Reason = "Desktop context menu"
        });
    }

    private void ActivateRelativeTab(int offset)
    {
        if (OpenWindows.Count == 0)
        {
            return;
        }

        var activeIndex = ActiveWindow is null ? -1 : OpenWindows.IndexOf(ActiveWindow);
        var nextIndex = activeIndex < 0 ? 0 : (activeIndex + offset + OpenWindows.Count) % OpenWindows.Count;
        _windowManagerService.Activate(OpenWindows[nextIndex]);
        SyncShellState();
    }

    private void OnOpenWindowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (AppWindowViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnWindowPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (AppWindowViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnWindowPropertyChanged;
            }
        }

        SyncShellState();
    }

    private void OnWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppWindowViewModel.IsActive)
            or nameof(AppWindowViewModel.IsMinimized)
            or nameof(AppWindowViewModel.State))
        {
            SyncShellState();
        }
    }

    private void SyncShellState()
    {
        ActiveWindow = OpenWindows
            .Where(x => x.IsActive && !x.IsMinimized)
            .OrderByDescending(x => x.ZIndex)
            .FirstOrDefault();

        foreach (var module in AllModules)
        {
            var window = OpenWindows.FirstOrDefault(x => x.Module.Key == module.Key);
            module.IsRunning = window is not null;
            module.IsActiveOnTaskbar = window is not null && window.IsActive && !window.IsMinimized;
            module.IsMinimizedOnTaskbar = window is not null && window.IsMinimized;
        }

        OnPropertyChanged(nameof(TaskbarRunningWindows));
    }

    private void OnNavigated(string route)
    {
        var module = AllModules.FirstOrDefault(x => x.Key == route);
        if (module is not null)
        {
            BreadcrumbText = $"ASMILE BO > {module.Title}";
        }
    }

    private void OnMessageRaised(string message)
    {
        ShowToast(message);
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        if (string.Equals(e.Value.Key, "Appearance.ShowDesktopIcons", StringComparison.OrdinalIgnoreCase))
        {
            AreDesktopIconsVisible = !string.Equals(e.Value.Value, "false", StringComparison.OrdinalIgnoreCase);
            return;
        }

        if (!string.Equals(e.Value.Key, "AppModule.Icon", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        QueueShellTask(async () =>
        {
            await BuildModuleCatalogAsync();
            await RestoreWorkspaceAsync();
            SyncShellState();
        });
    }

    private void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void OnModuleCollectionsChanged()
    {
        OnPropertyChanged(nameof(DesktopModules));
        OnPropertyChanged(nameof(PinnedModules));
        OnPropertyChanged(nameof(TaskbarPinnedModules));
        OnPropertyChanged(nameof(StartFilteredPinnedModules));
        OnPropertyChanged(nameof(StartFilteredAllModules));
        OnPropertyChanged(nameof(ShellReportModules));
    }

    private void QueueShellTask(Func<Task> taskFactory)
    {
        SafeFireAndForget.Run(() => RunShellTaskAsync(taskFactory), _notificationService, "Thao tác shell không thành công.");
    }

    private async Task RunShellTaskAsync(Func<Task> taskFactory)
    {
        try
        {
            await taskFactory();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShellStatusMessage = UserExceptionMapper.Map(ex);
            ShowToast("Thao tác shell không thành công. Vui lòng kiểm tra nhật ký.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _navigationGate.Dispose();
        _toastTimer.Stop();
        _clockTimer.Stop();
        _notificationService.MessageRaised -= OnMessageRaised;
        _navigationService.Navigated -= OnNavigated;
        _settingsChangeNotifier.SettingChanged -= OnSettingChanged;
        OpenWindows.CollectionChanged -= OnOpenWindowsCollectionChanged;
        foreach (var window in OpenWindows)
        {
            window.PropertyChanged -= OnWindowPropertyChanged;
        }
    }
}
