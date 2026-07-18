using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Shared.Security;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EditableSettingItemViewModel = ASMILE.BO.App.ViewModels.Settings.EditableSettingItemViewModel;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using Serilog;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private const string ModuleIconRoot = @"D:\ASMILE-BO\Windows_Icon";

    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IDatabaseAdminService _databaseAdminService;
    private readonly IBackupService _backupService;
    private readonly IServiceStatusService _serviceStatusService;
    private readonly ISecuritySettingsService _securitySettingsService;
    private readonly INotificationService _notificationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IPathSecurityService _pathSecurityService;
    private readonly IUserAccountService _userAccountService;
    private readonly IRoleService _roleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IPrinterSettingsService _printerSettingsService;
    private readonly ISettingsService _settingsService;
    private readonly IAppModuleService _appModuleService;
    private readonly IGeneralLedgerService _generalLedgerService;
    private readonly ISettingsValidator _settingsValidator;
    private readonly ISettingsChangeNotifier _settingsChangeNotifier;
    private readonly IModuleSettingsService _moduleSettingsService;
    private readonly ISettingsValidationService _moduleSettingsValidationService;
    private readonly IToastNotificationService _toastNotificationService;

    private readonly Dictionary<string, SettingsPageViewModel> _pages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SettingScope> _scopeMap = new();
    private readonly Dictionary<string, string?> _scopeTargetMap = new();
    private bool _isLoadingAppAdvanced;
    private bool _isNavigatingDetail;
    private string _generalLedgerSettingsSummary = "Đang tải dữ liệu";
    private string _generalLedgerSettingsValue = "Cấu hình";
    private string _generalLedgerAccountsSummary = "Đang tải dữ liệu";
    private string _generalLedgerAccountsValue = "Danh mục";
    private string _generalLedgerPostingSummary = "Đang tải dữ liệu";
    private string _generalLedgerPostingValue = "Hạch toán";
    private string _generalLedgerPeriodSummary = "Kỳ kế toán hiện tại";
    private string _generalLedgerPeriodValue = DateTime.Today.ToString("MM/yyyy");
    private string _generalLedgerReportsSummary = "Báo cáo sổ cái, cân đối phát sinh và nhật ký thao tác";
    private string _generalLedgerReportsValue = "Sẵn sàng";

    public ObservableCollection<SettingsCategoryItem> Categories { get; } = [];
    public ObservableCollection<SettingsCategoryItem> FilteredCategories { get; } = [];
    public ObservableCollection<SettingItemViewModel> VisibleRows { get; } = [];

    public ObservableCollection<BackupRecordDto> BackupRecords { get; } = [];
    public ObservableCollection<ServiceStatusDto> ServiceStatuses { get; } = [];
    public ObservableCollection<AppSettingsItemViewModel> AppSettings { get; } = [];
    public ObservableCollection<UserAccountDto> SecurityUsers { get; } = [];
    public ObservableCollection<RoleDto> SecurityRoles { get; } = [];
    public ObservableCollection<PermissionDto> SecurityPermissions { get; } = [];
    public ObservableCollection<LoginAuditDto> SecurityLoginAudits { get; } = [];
    public ObservableCollection<PrinterDeviceDto> AvailablePrinters { get; } = [];
    public ObservableCollection<PrinterPermissionDto> PrinterPermissions { get; } = [];
    public ObservableCollection<ExplorerBreadcrumbSegment> BreadcrumbSegments { get; } = [];

    public ObservableCollection<EditableSettingItemViewModel> EditableItems { get; } = [];
    public ObservableCollection<LedgerAccountDto> LedgerAccounts { get; } = [];
    public TablePagerViewModel<LedgerAccountDto> LedgerAccountsPager { get; } = new() { ItemName = "tài khoản" };
    public ObservableCollection<LedgerAccountDto> LedgerAccountOptions { get; } = [];
    public ObservableCollection<GeneralLedgerPostingMapViewModel> GeneralLedgerPostingMaps { get; } = [];
    public ObservableCollection<KeyValuePair<LedgerAccountType, string>> LedgerAccountTypeOptions { get; } =
    [
        new(LedgerAccountType.Asset, "Tài sản"),
        new(LedgerAccountType.Liability, "Nợ phải trả"),
        new(LedgerAccountType.Equity, "Vốn chủ sở hữu"),
        new(LedgerAccountType.Revenue, "Doanh thu"),
        new(LedgerAccountType.Expense, "Chi phí"),
        new(LedgerAccountType.Other, "Khác")
    ];
    public ObservableCollection<AppModuleDto> InstalledApps { get; } = [];
    public ObservableCollection<AppIconOptionViewModel> ModuleIconOptions { get; } = [];
    public ObservableCollection<ModuleSettingSectionViewModel> ModuleSettingSections { get; } = [];
    public ObservableCollection<ModuleSettingSectionViewModel> FilteredModuleSettingSections { get; } = [];
    public ObservableCollection<string> ModuleValidationErrors { get; } = [];
    public ObservableCollection<string> InstalledAppStatusFilters { get; } = ["Tất cả", "Đang bật", "Tạm tắt"];
    public SettingItemViewModel? HomeDatabaseRow { get; private set; }
    public SettingItemViewModel? HomeBackupRow { get; private set; }
    public SettingItemViewModel? HomeSecurityRow { get; private set; }

    [ObservableProperty] private SettingsCategoryItem? selectedCategory;
    [ObservableProperty] private SettingsPageViewModel? currentPage;
    [ObservableProperty] private SettingsDetailViewModel? currentDetail;
    [ObservableProperty] private bool isDetailMode;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string breadcrumb = "Cài đặt > Home";
    [ObservableProperty] private string pageTitle = "Home";
    [ObservableProperty] private string pageSubtitle = "Trung tâm cấu hình hệ thống ASMILE BO.";

    [ObservableProperty] private string machineName = Environment.MachineName;
    [ObservableProperty] private string localIp = "127.0.0.1";
    [ObservableProperty] private string currentUser = "Administrator";
    [ObservableProperty] private string timezone = TimeZoneInfo.Local.DisplayName;
    [ObservableProperty] private string sqlServerName = @".\SQLEXPRESS";
    [ObservableProperty] private string databaseName = "ASMILE_BO";
    [ObservableProperty] private string connectionStatus = "Connected";

    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool hasEditableForm;
    [ObservableProperty] private bool isLedgerAccountSettingsVisible;
    [ObservableProperty] private bool isGeneralLedgerSettingsVisible;
    [ObservableProperty] private bool isModuleSettingsVisible;
    [ObservableProperty] private bool isModuleSettingsLoading;
    [ObservableProperty] private bool isModuleSettingsSaving;
    [ObservableProperty] private bool moduleHasUnsavedChanges;
    [ObservableProperty] private string moduleSettingsSearchText = string.Empty;
    [ObservableProperty] private string moduleSettingsAppKey = string.Empty;
    [ObservableProperty] private string moduleSettingsTitle = string.Empty;
    [ObservableProperty] private string moduleSettingsDescription = string.Empty;
    [ObservableProperty] private string moduleSettingsIconGlyph = "\uE713";
    [ObservableProperty] private string moduleSettingsStatus = "Đang bật";
    [ObservableProperty] private string installedAppStatusFilter = "Tất cả";
    [ObservableProperty] private bool isGeneralLedgerDetailFormVisible;
    [ObservableProperty] private bool isGeneralLedgerPanelOpen;
    [ObservableProperty] private string generalLedgerPanelTitle = "Cài đặt sổ cái";
    [ObservableProperty] private string generalLedgerPanelKind = "core";
    [ObservableProperty] private string ledgerDataSource = "SQL Server";
    [ObservableProperty] private string ledgerCurrentPeriod = DateTime.Today.ToString("MM/yyyy");
    [ObservableProperty] private string ledgerPostingMethod = "Nhật ký chung";
    [ObservableProperty] private string ledgerRoundingMode = "Làm tròn đến đồng";
    [ObservableProperty] private string ledgerDefaultCurrency = "VND";
    [ObservableProperty] private bool ledgerAllowVoiceRecording;
    [ObservableProperty] private bool ledgerAllowEditPostedEntries;
    [ObservableProperty] private bool ledgerAllowDeleteEntries;
    [ObservableProperty] private bool ledgerRequireBalancedBeforeSave = true;
    [ObservableProperty] private bool ledgerAppEnabled = true;
    [ObservableProperty] private bool ledgerShowOnDesktop = true;
    [ObservableProperty] private bool ledgerAllowQuickPosting = true;
    [ObservableProperty] private bool ledgerAutoBalanceCheck = true;
    [ObservableProperty] private string ledgerPeriodFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy");
    [ObservableProperty] private string ledgerPeriodToDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)).ToString("dd/MM/yyyy");
    [ObservableProperty] private string ledgerPeriodStatus = "Đang mở";
    [ObservableProperty] private string ledgerLastLockedDate = "Chưa khóa sổ";
    [ObservableProperty] private string ledgerLockedBy = "-";
    [ObservableProperty] private string ledgerDefaultGeneralLedgerReport = "Sổ cái chi tiết tài khoản";
    [ObservableProperty] private string ledgerDefaultTrialBalanceReport = "Bảng cân đối phát sinh";
    [ObservableProperty] private string ledgerDefaultExportFormat = "Excel";
    [ObservableProperty] private bool ledgerAllowPrintReports = true;
    [ObservableProperty] private bool ledgerAllowExportExcel = true;
    [ObservableProperty] private bool ledgerAllowExportPdf = true;
    [ObservableProperty] private bool ledgerShowAccountantSignature = true;
    [ObservableProperty] private bool ledgerShowDirectorSignature = true;
    [ObservableProperty] private string ledgerReportFooterNote = "Báo cáo được lập từ dữ liệu sổ cái.";
    [ObservableProperty] private bool isGeneralLedgerChildBreadcrumbVisible;
    [ObservableProperty] private string generalLedgerChildBreadcrumbTitle = string.Empty;
    [ObservableProperty] private LedgerAccountDto? selectedLedgerAccount;
    [ObservableProperty] private AppModuleDto? selectedInstalledApp;
    [ObservableProperty] private AppModuleDto? selectedIconModule;
    [ObservableProperty] private AppIconOptionViewModel? selectedModuleIconOption;
    [ObservableProperty] private bool isAppearanceIconPickerVisible;

    public bool IsHomePage => string.Equals(CurrentPage?.Key, "home", StringComparison.OrdinalIgnoreCase) && !IsDetailMode;
    public bool IsRowsPage => !IsHomePage && !IsSystemPage && !IsAppsPage && !IsDetailMode;
    public bool IsSystemPage => string.Equals(CurrentPage?.Key, "system", StringComparison.OrdinalIgnoreCase) && !IsDetailMode;
    public bool IsAppsPage => string.Equals(CurrentPage?.Key, "apps", StringComparison.OrdinalIgnoreCase) && !IsDetailMode;
    public bool IsSqlConnected => string.Equals(ConnectionStatus, "Connected", StringComparison.OrdinalIgnoreCase);
    public bool IsLedgerCorePanel => IsGeneralLedgerDetailFormVisible && string.Equals(GeneralLedgerPanelKind, "core", StringComparison.OrdinalIgnoreCase);
    public bool IsLedgerPostingPanel => IsGeneralLedgerDetailFormVisible && string.Equals(GeneralLedgerPanelKind, "posting", StringComparison.OrdinalIgnoreCase);
    public bool IsLedgerPeriodPanel => IsGeneralLedgerDetailFormVisible && string.Equals(GeneralLedgerPanelKind, "period", StringComparison.OrdinalIgnoreCase);
    public bool IsLedgerReportsPanel => IsGeneralLedgerDetailFormVisible && string.Equals(GeneralLedgerPanelKind, "reports", StringComparison.OrdinalIgnoreCase);
    public bool HasModuleValidationErrors => ModuleValidationErrors.Count > 0;
    public bool IsInstalledAppsDetailVisible => IsDetailMode && string.Equals(CurrentDetail?.Title, "Ứng dụng đã cài", StringComparison.OrdinalIgnoreCase);
    public string? CurrentModuleIconPath => ResolveIconPath(SelectedIconModule?.Icon)
        ?? _navigationService.GetModuleCatalog().FirstOrDefault(x => string.Equals(x.Key, SelectedIconModule?.AppKey, StringComparison.OrdinalIgnoreCase))?.IconPath;
    public string CurrentModuleIconName => string.IsNullOrWhiteSpace(CurrentModuleIconPath)
        ? "Chưa có icon"
        : Path.GetFileNameWithoutExtension(CurrentModuleIconPath);

    public SettingsViewModel(
        ISystemSettingsService systemSettingsService,
        IDatabaseAdminService databaseAdminService,
        IBackupService backupService,
        IServiceStatusService serviceStatusService,
        ISecuritySettingsService securitySettingsService,
        INotificationService notificationService,
        IAuthorizationService authorizationService,
        ISecurityAuditService securityAuditService,
        IPathSecurityService pathSecurityService,
        IUserAccountService userAccountService,
        IRoleService roleService,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IPrinterSettingsService printerSettingsService,
        ISettingsService settingsService,
        IAppModuleService appModuleService,
        IGeneralLedgerService generalLedgerService,
        ISettingsValidator settingsValidator,
        ISettingsChangeNotifier settingsChangeNotifier,
        IModuleSettingsService moduleSettingsService,
        ISettingsValidationService moduleSettingsValidationService,
        IToastNotificationService toastNotificationService)
    {
        _systemSettingsService = systemSettingsService;
        _databaseAdminService = databaseAdminService;
        _backupService = backupService;
        _serviceStatusService = serviceStatusService;
        _securitySettingsService = securitySettingsService;
        _notificationService = notificationService;
        _authorizationService = authorizationService;
        _securityAuditService = securityAuditService;
        _pathSecurityService = pathSecurityService;
        _userAccountService = userAccountService;
        _roleService = roleService;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _printerSettingsService = printerSettingsService;
        _settingsService = settingsService;
        _appModuleService = appModuleService;
        _generalLedgerService = generalLedgerService;
        _settingsValidator = settingsValidator;
        _settingsChangeNotifier = settingsChangeNotifier;
        _moduleSettingsService = moduleSettingsService;
        _moduleSettingsValidationService = moduleSettingsValidationService;
        _toastNotificationService = toastNotificationService;
        _settingsChangeNotifier.SettingChanged += OnSettingsChanged;

        BuildCategories();
        BuildPages();
        BuildAppSettings();
        BuildModuleIconOptions();
        SelectedCategory = Categories.FirstOrDefault();
        _ = LoadAsync();
    }

    [RelayCommand]
    private void SelectCategory(SettingsCategoryItem? item)
    {
        if (item is null) return;
        ApplyCategorySelection(item);
        SelectedCategory = item;
    }

    [RelayCommand]
    public async Task NavigateToSettingsDetail(SettingItemViewModel? row)
    {
        if (_isNavigatingDetail)
        {
            return;
        }

        var route = row?.TargetPageKey ?? row?.Id ?? string.Empty;
        try
        {
            _isNavigatingDetail = true;
            if (row is null || CurrentPage is null) return;

            if (route.StartsWith("general-ledger-toggle-", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyGeneralLedgerToggleAsync(route);
                return;
            }

            if (IsGeneralLedgerSettingsVisible && route.StartsWith("general-ledger-", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(route, "general-ledger-accounts", StringComparison.OrdinalIgnoreCase))
                {
                    await LoadLedgerAccountSettingsAsync();
                    CurrentDetail = BuildLedgerAccountDetail();
                    IsGeneralLedgerSettingsVisible = false;
                    IsDetailMode = true;
                    RaisePageFlags();
                    return;
                }

                await OpenGeneralLedgerDetailAsync(route);
                return;
            }

            if (string.Equals(route, "general-ledger-accounts", StringComparison.OrdinalIgnoreCase))
            {
                await LoadLedgerAccountSettingsAsync();
                CurrentDetail = BuildLedgerAccountDetail();
                IsGeneralLedgerSettingsVisible = false;
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(route, "general-ledger-settings", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "general-ledger-posting-settings", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "general-ledger-period", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "general-ledger-reports", StringComparison.OrdinalIgnoreCase))
            {
                await OpenGeneralLedgerDetailAsync(route);
                return;
            }

            if (route.StartsWith("app-advanced:", StringComparison.OrdinalIgnoreCase))
            {
                var appKey = route["app-advanced:".Length..];
                if (!string.IsNullOrWhiteSpace(appKey))
                {
                    if (!string.Equals(appKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
                    {
                        await OpenModuleSettingsAsync(appKey);
                        return;
                    }

                    await LoadAppAdvancedEditorAsync(appKey);
                    var app = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, appKey, StringComparison.OrdinalIgnoreCase));
                    CurrentDetail = BuildAppAdvancedDetail(CurrentPage, appKey, app?.Name ?? appKey, app?.Description ?? "Cấu hình ứng dụng");
                    IsDetailMode = true;
                    RaisePageFlags();
                    return;
                }
            }

            if (string.Equals(route, "apps-installed", StringComparison.OrdinalIgnoreCase))
            {
                InstalledApps.Clear();
                foreach (var app in await _appModuleService.GetAppsAsync())
                {
                    InstalledApps.Add(app);
                }
            }

            if (string.Equals(route, "users-action-add", StringComparison.OrdinalIgnoreCase))
            {
                await AddUserAsync();
                return;
            }
            if (string.Equals(route, "users-action-reset", StringComparison.OrdinalIgnoreCase))
            {
                await ResetFirstUserPasswordAsync();
                return;
            }
            if (string.Equals(route, "users-action-unlock", StringComparison.OrdinalIgnoreCase))
            {
                await UnlockFirstUserAsync();
                return;
            }
            if (string.Equals(route, "users-action-save-permissions", StringComparison.OrdinalIgnoreCase))
            {
                await SaveDefaultRolePermissionsAsync();
                return;
            }

            await LoadEditableDetailAsync(route);
            CurrentDetail = BuildDetail(CurrentPage, row);
            IsDetailMode = true;
            RaisePageFlags();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Settings navigation failed. RowId={RowId}, TargetPageKey={TargetPageKey}, Page={PageKey}", row?.Id, row?.TargetPageKey, CurrentPage?.Key);
            IsDetailMode = false;
            CurrentDetail = null;
            HasEditableForm = false;
            EditableItems.Clear();
            _notificationService.Show("Không mở được mục cài đặt này. Vui lòng kiểm tra cấu hình hoặc khởi động lại ứng dụng.");
        }
        finally
        {
            _isNavigatingDetail = false;
        }
    }

    [RelayCommand] private void Search() { ApplyCategoryFilter(); ApplyRowsFilter(); }
    [RelayCommand] private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private void BackFromDetail()
    {
        IsDetailMode = false;
        CurrentDetail = null;
        EditableItems.Clear();
        HasEditableForm = false;
        IsLedgerAccountSettingsVisible = false;
        IsGeneralLedgerSettingsVisible = false;
        IsModuleSettingsVisible = false;
        ModuleSettingSections.Clear();
        FilteredModuleSettingSections.Clear();
        ModuleValidationErrors.Clear();
        OnPropertyChanged(nameof(HasModuleValidationErrors));
        ModuleHasUnsavedChanges = false;
        ModuleSettingsSearchText = string.Empty;
        IsGeneralLedgerDetailFormVisible = false;
        IsGeneralLedgerPanelOpen = false;
        IsGeneralLedgerChildBreadcrumbVisible = false;
        GeneralLedgerChildBreadcrumbTitle = string.Empty;
        IsDirty = false;
        RaisePageFlags();
    }

    [RelayCommand]
    private void NavigateBreadcrumb(string? target)
    {
        if (string.Equals(target, "none", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(target, "apps-installed", StringComparison.OrdinalIgnoreCase))
        {
            _ = OpenInstalledAppsAsync();
            return;
        }

        if (string.Equals(target, "general-ledger", StringComparison.OrdinalIgnoreCase))
        {
            _ = OpenGeneralLedgerMainSettingsAsync();
            return;
        }

        if (string.Equals(target, "category", StringComparison.OrdinalIgnoreCase) && SelectedCategory is not null)
        {
            ApplyCategorySelection(SelectedCategory);
            return;
        }

        if (string.Equals(target, "root", StringComparison.OrdinalIgnoreCase))
        {
            BackFromDetail();
        }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        var stamp = DateTime.Now.ToString("HHmmss");
        await _userAccountService.CreateUserAsync(new CreateUserRequestDto
        {
            UserName = $"user{stamp}",
            DisplayName = $"User {stamp}",
            InitialPassword = $"Abc@{stamp}",
            Email = $"user{stamp}@asmile.local",
            Phone = "0900000000",
            IsActive = true,
            MustChangePassword = true,
            RoleIds = SecurityRoles.Take(1).Select(x => x.Id).ToList()
        });
        await LoadAsync();
        _notificationService.Show("Đã thêm user mới.");
    }

    [RelayCommand]
    private async Task ResetFirstUserPasswordAsync()
    {
        var user = SecurityUsers.FirstOrDefault();
        if (user is null) return;
        await _userAccountService.ResetPasswordAsync(user.Id, "Abc@123", true);
        _notificationService.Show($"Đã reset mật khẩu cho {user.UserName}.");
    }

    [RelayCommand]
    private async Task UnlockFirstUserAsync()
    {
        var user = SecurityUsers.FirstOrDefault(x => x.IsLocked) ?? SecurityUsers.FirstOrDefault();
        if (user is null) return;
        await _userAccountService.UnlockUserAsync(user.Id);
        await LoadAsync();
        _notificationService.Show($"Đã mở khóa user {user.UserName}.");
    }

    [RelayCommand]
    private async Task SaveDefaultRolePermissionsAsync()
    {
        var role = SecurityRoles.FirstOrDefault();
        if (role is null) return;
        await _roleService.SaveRolePermissionsAsync(role.Id, SecurityPermissions.Take(8).Select(x => x.Code).ToList());
        _notificationService.Show($"Đã lưu permission cho role {role.Name}.");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var ok = await _databaseAdminService.TestConnectionAsync();
        ConnectionStatus = ok ? "Connected" : "Failed";
        _notificationService.Show(ok ? "Kết nối SQL thành công." : "Kết nối SQL thất bại.");
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        var created = await _backupService.BackupNowAsync();
        BackupRecords.Insert(0, created);
        _notificationService.Show($"Đã tạo backup: {created.FileName}");
    }

    [RelayCommand]
    private async Task SaveEditableAsync()
    {
        var requests = new List<SaveSettingValueRequest>();
        foreach (var item in EditableItems)
        {
            if (!_scopeMap.TryGetValue(item.Key, out var scope)) continue;
            var target = _scopeTargetMap.TryGetValue(item.Key, out var t) ? t : null;
            var def = (await _settingsService.GetDefinitionsAsync(scope, target)).FirstOrDefault(x => x.Key == item.Key);
            if (def is null) continue;
            var vr = _settingsValidator.Validate(def, item.Value);
            if (!vr.IsValid)
            {
                item.HasError = true;
                item.ErrorMessage = vr.ErrorMessage;
                return;
            }
            if (item.IsDirty)
            {
                requests.Add(new SaveSettingValueRequest { Key = item.Key, Scope = scope, ScopeTarget = target, Value = item.Value, Reason = "UI save" });
            }
        }

        if (requests.Count > 0)
        {
            await _settingsService.SaveValuesAsync(requests);
        }

        if (SelectedInstalledApp is not null)
        {
            await _appModuleService.UpdateAppAsync(new UpdateAppModuleRequest
            {
                AppKey = SelectedInstalledApp.AppKey,
                Icon = SelectedInstalledApp.Icon,
                IsEnabled = SelectedInstalledApp.IsEnabled,
                ShowOnDesktop = SelectedInstalledApp.ShowOnDesktop,
                PinToTaskbar = SelectedInstalledApp.PinToTaskbar,
                AllowClientAccess = SelectedInstalledApp.AllowClientAccess,
                RequiredPermission = SelectedInstalledApp.RequiredPermission,
                Reason = "Update app options"
            });
        }

        _notificationService.Show("Đã lưu cài đặt.");
        await ReloadEditableDetailAsync();
    }

    [RelayCommand]
    private async Task ResetEditableAsync()
    {
        foreach (var item in EditableItems)
        {
            if (_scopeMap.TryGetValue(item.Key, out var scope))
            {
                var target = _scopeTargetMap.TryGetValue(item.Key, out var t) ? t : null;
                await _settingsService.ResetToDefaultAsync(item.Key, scope, target);
            }
        }
        await ReloadEditableDetailAsync();
    }

    [RelayCommand]
    private void CancelEditable()
    {
        if (IsLedgerAccountSettingsVisible)
        {
            _ = ReloadEditableDetailAsync();
            return;
        }

        foreach (var item in EditableItems)
        {
            item.Value = item.OriginalValue;
            item.HasError = false;
            item.ErrorMessage = null;
        }
        IsDirty = false;
    }

    [RelayCommand]
    private void AddLedgerAccount()
    {
        var account = new LedgerAccountDto
        {
            Id = Guid.NewGuid().ToString("N"),
            SortOrder = LedgerAccounts.Count == 0 ? 1 : LedgerAccounts.Max(x => x.SortOrder) + 1,
            AccountCode = string.Empty,
            AccountName = "Tài khoản mới",
            ReportName = string.Empty,
            AccountLevel = 1,
            AccountKind = "D",
            AccountNature = "B",
            AccountType = LedgerAccountType.Asset,
            FollowWarehouse = true,
            CustomerDebt = true,
            SupplierDebt = true,
            AllowFund = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true
        };

        LedgerAccounts.Add(account);
        LedgerAccountsPager.SetSource(LedgerAccounts, resetPage: false);
        SelectedLedgerAccount = account;
        IsDirty = true;
    }

    [RelayCommand]
    private async Task DeleteLedgerAccountAsync()
    {
        if (SelectedLedgerAccount is null)
        {
            return;
        }

        var selected = SelectedLedgerAccount;
        if (!string.IsNullOrWhiteSpace(selected.AccountCode))
        {
            await _generalLedgerService.DeleteAccountAsync(selected.AccountCode);
        }

        LedgerAccounts.Remove(selected);
        LedgerAccountsPager.SetSource(LedgerAccounts, resetPage: false);
        SelectedLedgerAccount = LedgerAccounts.FirstOrDefault();
        IsDirty = true;
        _notificationService.Show("Đã xóa tài khoản khỏi danh mục.");
    }

    [RelayCommand]
    private async Task ImportLedgerAccountsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file danh mục tài khoản",
            Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|Tất cả file (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var imported = string.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase)
                ? ImportLedgerAccountsFromCsv(dialog.FileName)
                : ImportLedgerAccountsFromExcel(dialog.FileName);

            var count = UpsertLedgerAccounts(imported);
            IsDirty = true;
            await SaveLedgerAccountsAsync();
            _notificationService.Show($"Đã nhập {count} tài khoản từ file.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import ledger accounts failed. File={FileName}", dialog.FileName);
            _notificationService.Show("Không nhập được file tài khoản. Kiểm tra đúng cột Mã tài khoản và Tên tài khoản.");
        }
    }

    [RelayCommand]
    private async Task SaveLedgerAccountsAsync()
    {
        var validAccounts = LedgerAccounts
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && !string.IsNullOrWhiteSpace(x.AccountName))
            .Select(x =>
            {
                x.AccountCode = x.AccountCode.Trim();
                x.AccountName = x.AccountName.Trim();
                x.ReportName = string.IsNullOrWhiteSpace(x.ReportName) ? x.AccountName : x.ReportName.Trim();
                x.ParentAccountCode = string.IsNullOrWhiteSpace(x.ParentAccountCode) ? null : x.ParentAccountCode.Trim();
                x.AccountLevel = x.AccountLevel <= 0 ? GetLedgerAccountLevel(x.AccountCode, x.ParentAccountCode) : x.AccountLevel;
                x.AccountKind = NormalizeLedgerAccountKind(x.AccountKind, x.AccountCode, x.ParentAccountCode);
                x.AccountNature = NormalizeLedgerAccountNature(x.AccountNature);
                x.CreatedAt = x.CreatedAt == default ? DateTime.Now : x.CreatedAt;
                x.UpdatedAt = DateTime.Now;
                return x;
            })
            .ToList();

        if (validAccounts.Count == 0)
        {
            _notificationService.Show("Chưa có tài khoản hợp lệ để lưu.");
            return;
        }

        var duplicate = validAccounts
            .GroupBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
        {
            _notificationService.Show($"Mã tài khoản bị trùng: {duplicate.Key}");
            return;
        }

        await _generalLedgerService.SaveAccountsAsync(validAccounts);
        LedgerAccounts.Clear();
        foreach (var account in await _generalLedgerService.GetAccountsAsync())
        {
            LedgerAccounts.Add(account);
        }

        LedgerAccountsPager.SetSource(LedgerAccounts);
        SelectedLedgerAccount = LedgerAccounts.FirstOrDefault();
        IsDirty = false;
        _notificationService.Show("Đã lưu danh mục tài khoản.");
    }

    [RelayCommand]
    private void ExportLedgerAccounts()
    {
        if (LedgerAccounts.Count == 0)
        {
            _notificationService.Show("Không có tài khoản để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất danh mục tài khoản",
            FileName = $"Danh_muc_tai_khoan_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Danh mục tài khoản");
            sheet.Cell(1, 1).Value = "Mã tài khoản";
            sheet.Cell(1, 2).Value = "Tên tài khoản";
            sheet.Cell(1, 3).Value = "Tài khoản cha";
            sheet.Cell(1, 4).Value = "Cấp";
            sheet.Cell(1, 5).Value = "Loại";
            sheet.Cell(1, 6).Value = "Tính chất";
            sheet.Cell(1, 7).Value = "Kích hoạt";
            sheet.Cell(1, 8).Value = "TK tiền mặt";
            sheet.Cell(1, 9).Value = "Có ngoại tệ";
            sheet.Cell(1, 10).Value = "Theo kho";
            sheet.Cell(1, 11).Value = "Công nợ KH";
            sheet.Cell(1, 12).Value = "Công nợ NCC";
            sheet.Cell(1, 13).Value = "Quỹ";
            sheet.Cell(1, 14).Value = "Ngày tạo";
            sheet.Cell(1, 15).Value = "Ngày sửa";
            sheet.Cell(1, 16).Value = "Tên báo cáo";
            sheet.Cell(1, 17).Value = "Nhóm tài khoản";

            var row = 2;
            foreach (var account in LedgerAccounts.OrderBy(x => x.SortOrder == 0 ? int.MaxValue : x.SortOrder).ThenBy(x => x.AccountCode))
            {
                sheet.Cell(row, 1).Value = account.AccountCode;
                sheet.Cell(row, 2).Value = account.AccountName;
                sheet.Cell(row, 3).Value = account.ParentAccountCode ?? string.Empty;
                sheet.Cell(row, 4).Value = account.AccountLevel;
                sheet.Cell(row, 5).Value = account.AccountKind;
                sheet.Cell(row, 6).Value = account.AccountNature;
                sheet.Cell(row, 7).Value = account.IsActive;
                sheet.Cell(row, 8).Value = account.IsCashAccount;
                sheet.Cell(row, 9).Value = account.HasForeignCurrency;
                sheet.Cell(row, 10).Value = account.FollowWarehouse;
                sheet.Cell(row, 11).Value = account.CustomerDebt;
                sheet.Cell(row, 12).Value = account.SupplierDebt;
                sheet.Cell(row, 13).Value = account.AllowFund;
                sheet.Cell(row, 14).Value = account.CreatedAt;
                sheet.Cell(row, 15).Value = account.UpdatedAt;
                sheet.Cell(row, 16).Value = account.ReportName;
                sheet.Cell(row, 17).Value = GetLedgerAccountTypeDisplay(account.AccountType);
                row++;
            }

            var range = sheet.Range(1, 1, Math.Max(1, row - 1), 17);
            range.CreateTable();
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            _notificationService.Show($"Đã xuất danh mục tài khoản: {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Export ledger accounts failed. File={FileName}", dialog.FileName);
            _notificationService.Show("Không xuất được file Excel danh mục tài khoản.");
        }
    }

    private static IReadOnlyList<LedgerAccountDto> ImportLedgerAccountsFromExcel(string fileName)
    {
        using var workbook = new XLWorkbook(fileName);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.FirstRowUsed();
        if (headerRow is null)
        {
            return [];
        }

        var headers = headerRow.CellsUsed()
            .ToDictionary(x => NormalizeHeader(x.GetString()), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

        var codeColumn = FindExcelHeader(headers, "sotaikhoan", "mataikhoan", "accno", "accountcode", "code");
        var nameColumn = FindExcelHeader(headers, "tentaikhoan", "accname", "accountname", "name");
        if (codeColumn == 0 || nameColumn == 0)
        {
            throw new InvalidOperationException("File Excel cần có cột Mã tài khoản và Tên tài khoản.");
        }

        var reportColumn = FindExcelHeader(headers, "tenbaocao", "rptname", "reportname");
        var parentColumn = FindExcelHeader(headers, "tkcha", "taikhoancha", "accparent", "parentaccountcode", "parent");
        var levelColumn = FindExcelHeader(headers, "cap", "acclevel", "level", "accountlevel");
        var kindColumn = FindExcelHeader(headers, "loai", "acctype", "accountkind", "hachdinh", "hd");
        var natureColumn = FindExcelHeader(headers, "tinhchat", "accpl", "accountnature");
        var typeColumn = FindExcelHeader(headers, "nhomtaikhoan", "loaitaikhoan", "accounttype", "type");
        var activeColumn = FindExcelHeader(headers, "kichhoat", "accactive", "trangthai", "isactive", "active");
        var cashColumn = FindExcelHeader(headers, "tktienmat", "iscash", "iscashaccount");
        var foreignColumn = FindExcelHeader(headers, "congoaite", "isfcurr", "hasforeigncurrency");
        var warehouseColumn = FindExcelHeader(headers, "owwarehouse", "theokho", "followwarehouse");
        var customerDebtColumn = FindExcelHeader(headers, "customerdebt", "congnokh");
        var supplierDebtColumn = FindExcelHeader(headers, "vsupplierdebt", "supplierdebt", "congnoncc");
        var fundColumn = FindExcelHeader(headers, "allowfund", "quy");

        var sortOrder = 0;
        return sheet.RowsUsed()
            .Skip(1)
            .Select(row => BuildLedgerAccount(
                row.Cell(codeColumn).GetString(),
                row.Cell(nameColumn).GetString(),
                reportColumn == 0 ? null : row.Cell(reportColumn).GetString(),
                parentColumn == 0 ? null : row.Cell(parentColumn).GetString(),
                levelColumn == 0 ? null : row.Cell(levelColumn).GetString(),
                kindColumn == 0 ? null : row.Cell(kindColumn).GetString(),
                natureColumn == 0 ? null : row.Cell(natureColumn).GetString(),
                typeColumn == 0 ? null : row.Cell(typeColumn).GetString(),
                activeColumn == 0 ? null : row.Cell(activeColumn).GetString(),
                cashColumn == 0 ? null : row.Cell(cashColumn).GetString(),
                foreignColumn == 0 ? null : row.Cell(foreignColumn).GetString(),
                warehouseColumn == 0 ? null : row.Cell(warehouseColumn).GetString(),
                customerDebtColumn == 0 ? null : row.Cell(customerDebtColumn).GetString(),
                supplierDebtColumn == 0 ? null : row.Cell(supplierDebtColumn).GetString(),
                fundColumn == 0 ? null : row.Cell(fundColumn).GetString(),
                ++sortOrder))
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && !string.IsNullOrWhiteSpace(x.AccountName))
            .ToList();
    }

    private static IReadOnlyList<LedgerAccountDto> ImportLedgerAccountsFromCsv(string fileName)
    {
        var lines = File.ReadAllLines(fileName);
        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0])
            .Select((value, index) => new { Header = NormalizeHeader(value), Index = index })
            .ToDictionary(x => x.Header, x => x.Index, StringComparer.OrdinalIgnoreCase);

        var codeIndex = FindCsvHeader(headers, "sotaikhoan", "mataikhoan", "accno", "accountcode", "code");
        var nameIndex = FindCsvHeader(headers, "tentaikhoan", "accname", "accountname", "name");
        if (codeIndex < 0 || nameIndex < 0)
        {
            throw new InvalidOperationException("File CSV cần có cột Mã tài khoản và Tên tài khoản.");
        }

        var reportIndex = FindCsvHeader(headers, "tenbaocao", "rptname", "reportname");
        var parentIndex = FindCsvHeader(headers, "tkcha", "taikhoancha", "accparent", "parentaccountcode", "parent");
        var levelIndex = FindCsvHeader(headers, "cap", "acclevel", "level", "accountlevel");
        var kindIndex = FindCsvHeader(headers, "loai", "acctype", "accountkind", "hachdinh", "hd");
        var natureIndex = FindCsvHeader(headers, "tinhchat", "accpl", "accountnature");
        var typeIndex = FindCsvHeader(headers, "nhomtaikhoan", "loaitaikhoan", "accounttype", "type");
        var activeIndex = FindCsvHeader(headers, "kichhoat", "accactive", "trangthai", "isactive", "active");
        var cashIndex = FindCsvHeader(headers, "tktienmat", "iscash", "iscashaccount");
        var foreignIndex = FindCsvHeader(headers, "congoaite", "isfcurr", "hasforeigncurrency");
        var warehouseIndex = FindCsvHeader(headers, "owwarehouse", "theokho", "followwarehouse");
        var customerDebtIndex = FindCsvHeader(headers, "customerdebt", "congnokh");
        var supplierDebtIndex = FindCsvHeader(headers, "vsupplierdebt", "supplierdebt", "congnoncc");
        var fundIndex = FindCsvHeader(headers, "allowfund", "quy");

        var sortOrder = 0;
        return lines.Skip(1)
            .Select(SplitCsvLine)
            .Where(values => values.Count > Math.Max(codeIndex, nameIndex))
            .Select(values => BuildLedgerAccount(
                values[codeIndex],
                values[nameIndex],
                reportIndex >= 0 && values.Count > reportIndex ? values[reportIndex] : null,
                parentIndex >= 0 && values.Count > parentIndex ? values[parentIndex] : null,
                levelIndex >= 0 && values.Count > levelIndex ? values[levelIndex] : null,
                kindIndex >= 0 && values.Count > kindIndex ? values[kindIndex] : null,
                natureIndex >= 0 && values.Count > natureIndex ? values[natureIndex] : null,
                typeIndex >= 0 && values.Count > typeIndex ? values[typeIndex] : null,
                activeIndex >= 0 && values.Count > activeIndex ? values[activeIndex] : null,
                cashIndex >= 0 && values.Count > cashIndex ? values[cashIndex] : null,
                foreignIndex >= 0 && values.Count > foreignIndex ? values[foreignIndex] : null,
                warehouseIndex >= 0 && values.Count > warehouseIndex ? values[warehouseIndex] : null,
                customerDebtIndex >= 0 && values.Count > customerDebtIndex ? values[customerDebtIndex] : null,
                supplierDebtIndex >= 0 && values.Count > supplierDebtIndex ? values[supplierDebtIndex] : null,
                fundIndex >= 0 && values.Count > fundIndex ? values[fundIndex] : null,
                ++sortOrder))
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && !string.IsNullOrWhiteSpace(x.AccountName))
            .ToList();
    }

    private int UpsertLedgerAccounts(IEnumerable<LedgerAccountDto> imported)
    {
        var count = 0;
        foreach (var account in imported)
        {
            var existing = LedgerAccounts.FirstOrDefault(x => string.Equals(x.AccountCode, account.AccountCode, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                LedgerAccounts.Add(account);
            }
            else
            {
                existing.AccountName = account.AccountName;
                existing.ReportName = account.ReportName;
                existing.ParentAccountCode = account.ParentAccountCode;
                existing.SortOrder = account.SortOrder;
                existing.AccountLevel = account.AccountLevel;
                existing.AccountKind = account.AccountKind;
                existing.AccountNature = account.AccountNature;
                existing.AccountType = account.AccountType;
                existing.IsCashAccount = account.IsCashAccount;
                existing.HasForeignCurrency = account.HasForeignCurrency;
                existing.FollowWarehouse = account.FollowWarehouse;
                existing.CustomerDebt = account.CustomerDebt;
                existing.SupplierDebt = account.SupplierDebt;
                existing.AllowFund = account.AllowFund;
                existing.CreatedAt = account.CreatedAt;
                existing.UpdatedAt = account.UpdatedAt;
                existing.IsActive = account.IsActive;
            }

            count++;
        }

        return count;
    }

    private static LedgerAccountDto BuildLedgerAccount(
        string code,
        string name,
        string? reportName,
        string? parent,
        string? level,
        string? kind,
        string? nature,
        string? type,
        string? active,
        string? isCash,
        string? hasForeignCurrency,
        string? followWarehouse,
        string? customerDebt,
        string? supplierDebt,
        string? allowFund,
        int sortOrder)
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            SortOrder = sortOrder,
            AccountCode = code.Trim(),
            AccountName = name.Trim(),
            ReportName = string.IsNullOrWhiteSpace(reportName) ? name.Trim() : reportName.Trim(),
            ParentAccountCode = string.IsNullOrWhiteSpace(parent) ? null : parent.Trim(),
            AccountLevel = int.TryParse(level, out var parsedLevel) && parsedLevel > 0 ? parsedLevel : GetLedgerAccountLevel(code.Trim(), parent),
            AccountKind = NormalizeLedgerAccountKind(kind, code.Trim(), parent),
            AccountNature = NormalizeLedgerAccountNature(nature),
            AccountType = ParseLedgerAccountType(type, code),
            IsCashAccount = ParseBooleanFlag(isCash),
            HasForeignCurrency = ParseBooleanFlag(hasForeignCurrency),
            FollowWarehouse = ParseBooleanFlag(followWarehouse, defaultValue: true),
            CustomerDebt = ParseBooleanFlag(customerDebt, defaultValue: true),
            SupplierDebt = ParseBooleanFlag(supplierDebt, defaultValue: true),
            AllowFund = ParseBooleanFlag(allowFund, defaultValue: true),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = ParseActive(active)
        };

    private static string GetLedgerAccountTypeDisplay(LedgerAccountType type) => type switch
    {
        LedgerAccountType.Asset => "Tài sản",
        LedgerAccountType.Liability => "Nợ phải trả",
        LedgerAccountType.Equity => "Vốn chủ sở hữu",
        LedgerAccountType.Revenue => "Doanh thu",
        LedgerAccountType.Expense => "Chi phí",
        _ => "Khác"
    };

    private static int GetLedgerAccountLevel(string accountCode, string? parentAccountCode)
    {
        if (!string.IsNullOrWhiteSpace(parentAccountCode))
        {
            return parentAccountCode.Trim().Length switch
            {
                <= 3 => 2,
                4 => 3,
                _ => 4
            };
        }

        return accountCode.Trim().Length switch
        {
            <= 3 => 1,
            4 => 2,
            5 => 3,
            _ => 4
        };
    }

    private static string NormalizeLedgerAccountKind(string? accountKind, string accountCode, string? parentAccountCode)
    {
        var value = (accountKind ?? string.Empty).Trim().ToUpperInvariant();
        if (value is "H" or "D")
        {
            return value;
        }

        return string.IsNullOrWhiteSpace(parentAccountCode) || accountCode.Trim().Length <= 4 ? "H" : "D";
    }

    private static string NormalizeLedgerAccountNature(string? accountNature)
    {
        var value = (accountNature ?? string.Empty).Trim().ToUpperInvariant();
        return value is "D" or "C" or "B" or "P" or "M" ? value : "B";
    }

    private static LedgerAccountType ParseLedgerAccountType(string? value, string code)
    {
        var normalized = NormalizeHeader(value ?? string.Empty);
        return normalized switch
        {
            "taisan" or "asset" => LedgerAccountType.Asset,
            "nophaitra" or "liability" => LedgerAccountType.Liability,
            "vonchusohuu" or "equity" => LedgerAccountType.Equity,
            "doanhthu" or "revenue" => LedgerAccountType.Revenue,
            "chiphi" or "expense" => LedgerAccountType.Expense,
            _ => code.StartsWith('1') ? LedgerAccountType.Asset
                : code.StartsWith('2') || code.StartsWith('3') ? LedgerAccountType.Liability
                : code.StartsWith('4') ? LedgerAccountType.Equity
                : code.StartsWith('5') || code.StartsWith('7') ? LedgerAccountType.Revenue
                : code.StartsWith('6') || code.StartsWith('8') ? LedgerAccountType.Expense
                : LedgerAccountType.Other
        };
    }

    private static bool ParseActive(string? value)
    {
        var normalized = NormalizeHeader(value ?? string.Empty);
        return string.IsNullOrWhiteSpace(normalized)
            || normalized is "1" or "true" or "yes" or "dangdung" or "dangsudung" or "hoatdong";
    }

    private static bool ParseBooleanFlag(string? value, bool defaultValue = false)
    {
        var normalized = NormalizeHeader(value ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return defaultValue;
        }

        return normalized is "1" or "true" or "yes" or "co" or "x" or "checked" or "dangdung" or "dangsudung";
    }

    private static int FindExcelHeader(IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(name, out var index))
            {
                return index;
            }
        }

        return 0;
    }

    private static int FindCsvHeader(IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(name, out var index))
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormalizeHeader(string value)
    {
        var formD = value.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var chars = formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray())
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private static List<string> SplitCsvLine(string line)
        => line.Split(',').Select(x => x.Trim().Trim('"')).ToList();

    partial void OnSearchTextChanged(string value)
    {
        SearchText = InputSanitizer.SanitizeSearchKeyword(value);
        ApplyCategoryFilter();
        ApplyRowsFilter();
        RefreshInstalledAppsDetail();
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (string.Equals(e.Value.Key, "AppModule.Icon", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = LoadAsync();
    }

    partial void OnInstalledAppStatusFilterChanged(string value)
    {
        RefreshInstalledAppsDetail();
    }

    partial void OnSelectedCategoryChanged(SettingsCategoryItem? value)
    {
        if (value is not null) ApplyCategorySelection(value);
    }

    partial void OnSelectedInstalledAppChanged(AppModuleDto? value)
    {
        if (!_isLoadingAppAdvanced && value is not null)
        {
            _ = LoadAppAdvancedEditorAsync(value.AppKey);
        }
    }

    partial void OnSelectedIconModuleChanged(AppModuleDto? value)
    {
        if (value is null)
        {
            SelectedModuleIconOption = null;
            return;
        }

        SelectedModuleIconOption = FindIconOption(value.Icon) ?? ModuleIconOptions.FirstOrDefault();
        OnPropertyChanged(nameof(CurrentModuleIconPath));
        OnPropertyChanged(nameof(CurrentModuleIconName));
    }

    partial void OnCurrentDetailChanged(SettingsDetailViewModel? value)
    {
        if (value is not null)
        {
            SetBreadcrumbFromDetail(value);
        }
        OnPropertyChanged(nameof(IsInstalledAppsDetailVisible));
    }

    partial void OnModuleSettingsSearchTextChanged(string value)
    {
        ModuleSettingsSearchText = InputSanitizer.SanitizeSearchKeyword(value);
        ApplyModuleSettingsFilter();
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        if (!string.IsNullOrWhiteSpace(ModuleSettingsAppKey))
        {
            await OpenModuleSettingsAsync(ModuleSettingsAppKey);
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveModuleSettingsAsync();
    }

    [RelayCommand]
    private async Task ResetDefault()
    {
        await ResetModuleSettingsAsync();
    }

    [RelayCommand]
    private async Task SaveModuleIconAsync()
    {
        if (SelectedIconModule is null || SelectedModuleIconOption is null)
        {
            _notificationService.Show("Vui lòng chọn module và icon cần áp dụng.");
            return;
        }

        SelectedIconModule.Icon = SelectedModuleIconOption.IconPath;
        await _appModuleService.UpdateAppAsync(new UpdateAppModuleRequest
        {
            AppKey = SelectedIconModule.AppKey,
            Icon = SelectedIconModule.Icon,
            IsEnabled = SelectedIconModule.IsEnabled,
            ShowOnDesktop = SelectedIconModule.ShowOnDesktop,
            PinToTaskbar = SelectedIconModule.PinToTaskbar,
            AllowClientAccess = SelectedIconModule.AllowClientAccess,
            RequiredPermission = SelectedIconModule.RequiredPermission,
            Reason = "Update module icon"
        });

        _settingsChangeNotifier.NotifyChanged(new SettingValueDto
        {
            Key = "AppModule.Icon",
            Scope = SettingScope.System,
            Value = SelectedIconModule.AppKey,
            UpdatedAt = DateTime.Now,
            UpdatedBy = _currentUserService.CurrentUser?.UserName ?? "Administrator"
        }, false);

        OnPropertyChanged(nameof(CurrentModuleIconPath));
        OnPropertyChanged(nameof(CurrentModuleIconName));
        _notificationService.Show("Đã lưu icon module.");
        RefreshInstalledAppsDetail();
    }

    [RelayCommand]
    private void Back()
    {
        BackFromDetail();
    }

    public async Task OpenAppearanceIconSettingsAsync(string? appKey)
    {
        var appearanceCategory = Categories.FirstOrDefault(x => string.Equals(x.Key, "appearance", StringComparison.OrdinalIgnoreCase));
        if (appearanceCategory is not null)
        {
            SelectedCategory = appearanceCategory;
        }

        if (CurrentPage is null || !string.Equals(CurrentPage.Key, "appearance", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var row = CurrentPage.Rows.FirstOrDefault(x => string.Equals(x.TargetPageKey, "appearance-main", StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        CurrentDetail = BuildDetail(CurrentPage, row);
        IsDetailMode = true;
        RaisePageFlags();
        await LoadEditableDetailAsync("appearance-main");

        if (!string.IsNullOrWhiteSpace(appKey))
        {
            SelectedIconModule = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, appKey, StringComparison.OrdinalIgnoreCase))
                ?? SelectedIconModule;
        }
    }

    [RelayCommand]
    private async Task BackToInstalledApps()
    {
        var appsPage = _pages.TryGetValue("apps", out var page) ? page : CurrentPage;
        if (appsPage is null)
        {
            BackFromDetail();
            return;
        }

        CurrentPage = appsPage;
        SelectedCategory = Categories.FirstOrDefault(x => string.Equals(x.Key, "apps", StringComparison.OrdinalIgnoreCase)) ?? SelectedCategory;
        InstalledApps.Clear();
        foreach (var app in await _appModuleService.GetAppsAsync())
        {
            InstalledApps.Add(app);
        }

        var row = appsPage.Rows.FirstOrDefault(x => string.Equals(x.TargetPageKey, "apps-installed", StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            BackFromDetail();
            return;
        }

        ClearDetailEditorStates();
        CurrentDetail = BuildDetail(appsPage, row);
        IsDetailMode = true;
        RaisePageFlags();
    }

    private async Task OpenModuleSettingsAsync(string appKey)
    {
        IsModuleSettingsLoading = true;
        try
        {
            EditableItems.Clear();
            ModuleSettingSections.Clear();
            FilteredModuleSettingSections.Clear();
            ModuleValidationErrors.Clear();
            HasEditableForm = false;
            IsLedgerAccountSettingsVisible = false;
            IsGeneralLedgerSettingsVisible = false;
            IsGeneralLedgerDetailFormVisible = false;
            IsGeneralLedgerPanelOpen = false;
            IsGeneralLedgerChildBreadcrumbVisible = false;

            var definition = _moduleSettingsService.GetDefinition(appKey);
            if (definition is null)
            {
                _toastNotificationService.Error("Chưa có cấu hình module này.");
                return;
            }

            InstalledApps.Clear();
            foreach (var app in await _appModuleService.GetAppsAsync())
            {
                InstalledApps.Add(app);
            }

            var moduleApp = FindInstalledApp(definition.AppKey) ?? FindInstalledApp(appKey);
            var state = await _moduleSettingsService.LoadAsync(definition.AppKey);
            var ledgerEnabled = FindInstalledApp("general-ledger")?.IsEnabled ?? true;

            ModuleSettingsAppKey = definition.AppKey;
            ModuleSettingsTitle = definition.Title;
            ModuleSettingsDescription = definition.Description;
            ModuleSettingsIconGlyph = definition.IconGlyph;
            ModuleSettingsStatus = moduleApp?.IsEnabled == false ? "Tạm tắt" : "Đang bật";

            if (!definition.Sections.Any(x => string.Equals(x.Key, "status", StringComparison.OrdinalIgnoreCase)))
            {
                var statusSectionDefinition = new ModuleSettingSectionDefinition(
                    "status",
                    "Trạng thái module",
                    "Bật/tắt module và hiển thị trên desktop",
                    "\uE73E",
                    [
                        new ModuleSettingDefinition("enabled", "Bật ứng dụng", $"Cho phép module {definition.Title.ToLowerInvariant()} hoạt động", ModuleSettingKind.Toggle, moduleApp?.IsEnabled == false ? "false" : "true"),
                        new ModuleSettingDefinition("showOnDesktop", "Hiển thị Desktop", $"Hiện biểu tượng {definition.Title.ToLowerInvariant()} trên desktop", ModuleSettingKind.Toggle, moduleApp?.ShowOnDesktop == false ? "false" : "true")
                    ]);
                var statusSection = new ModuleSettingSectionViewModel(statusSectionDefinition);
                foreach (var itemDefinition in statusSectionDefinition.Settings)
                {
                    state.Values.TryGetValue(itemDefinition.Key, out var savedValue);
                    var item = new ModuleSettingItemViewModel(itemDefinition, savedValue ?? itemDefinition.DefaultValue);
                    item.PropertyChanged += (_, __) => UpdateModuleDirtyState();
                    statusSection.Items.Add(item);
                }
                ModuleSettingSections.Add(statusSection);
            }

            foreach (var sectionDefinition in definition.Sections)
            {
                var section = new ModuleSettingSectionViewModel(sectionDefinition);
                foreach (var itemDefinition in sectionDefinition.Settings)
                {
                    state.Values.TryGetValue(itemDefinition.Key, out var savedValue);
                    var item = new ModuleSettingItemViewModel(itemDefinition, savedValue ?? itemDefinition.DefaultValue);
                    if (string.Equals(itemDefinition.DependsOnModuleKey, "general-ledger", StringComparison.OrdinalIgnoreCase) && !ledgerEnabled)
                    {
                        item.IsEnabled = false;
                        item.DisabledReason = "Cần bật module Sổ cái để sử dụng chức năng này.";
                    }
                    item.PropertyChanged += (_, __) => UpdateModuleDirtyState();
                    section.Items.Add(item);
                }
                ModuleSettingSections.Add(section);
            }

            CurrentDetail = new SettingsDetailViewModel
            {
                Title = definition.Title,
                Subtitle = definition.Description,
                Breadcrumb = $"Cài đặt > Ứng dụng > Ứng dụng đã cài > {definition.Title}"
            };

            IsModuleSettingsVisible = true;
            IsDetailMode = true;
            ModuleHasUnsavedChanges = false;
            ApplyModuleSettingsFilter();
            RaisePageFlags();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OpenModuleSettings failed for {AppKey}", appKey);
            _toastNotificationService.Error("Không mở được cấu hình module.");
        }
        finally
        {
            IsModuleSettingsLoading = false;
        }
    }

    private async Task SaveModuleSettingsAsync()
    {
        if (!IsModuleSettingsVisible || string.IsNullOrWhiteSpace(ModuleSettingsAppKey))
        {
            return;
        }

        IsModuleSettingsSaving = true;
        try
        {
            ModuleValidationErrors.Clear();
            OnPropertyChanged(nameof(HasModuleValidationErrors));
            var definition = _moduleSettingsService.GetDefinition(ModuleSettingsAppKey);
            if (definition is null)
            {
                _toastNotificationService.Error("Không tìm thấy định nghĩa cấu hình module.");
                return;
            }

            var values = ModuleSettingSections
                .SelectMany(x => x.Items)
                .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var errors = _moduleSettingsValidationService.Validate(definition, values);
            foreach (var item in ModuleSettingSections.SelectMany(x => x.Items))
            {
                item.ValidationError = errors.TryGetValue(item.Key, out var error) ? error : string.Empty;
            }
            foreach (var error in errors.Values.Distinct())
            {
                ModuleValidationErrors.Add(error);
            }
            OnPropertyChanged(nameof(HasModuleValidationErrors));

            if (errors.Count > 0)
            {
                _toastNotificationService.Error("Cấu hình chưa hợp lệ. Vui lòng kiểm tra các trường báo lỗi.");
                return;
            }

            await _moduleSettingsService.SaveAsync(ModuleSettingsAppKey, values);
            foreach (var item in ModuleSettingSections.SelectMany(x => x.Items))
            {
                item.AcceptChanges();
            }
            ModuleHasUnsavedChanges = false;
            _toastNotificationService.Success("Đã lưu cấu hình module.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SaveModuleSettings failed for {AppKey}", ModuleSettingsAppKey);
            _toastNotificationService.Error("Lưu cấu hình thất bại.");
        }
        finally
        {
            IsModuleSettingsSaving = false;
        }
    }

    private async Task ResetModuleSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(ModuleSettingsAppKey))
        {
            return;
        }

        await _moduleSettingsService.ResetDefaultAsync(ModuleSettingsAppKey);
        await OpenModuleSettingsAsync(ModuleSettingsAppKey);
        _toastNotificationService.Success("Đã khôi phục cấu hình mặc định.");
    }

    private void ApplyModuleSettingsFilter()
    {
        FilteredModuleSettingSections.Clear();
        var keyword = (ModuleSettingsSearchText ?? string.Empty).Trim();
        foreach (var section in ModuleSettingSections)
        {
            var sectionMatches = string.IsNullOrWhiteSpace(keyword)
                || section.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || section.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            var items = section.Items.Where(item => sectionMatches
                || item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Value.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            if (items.Count == 0)
            {
                continue;
            }

            var cloneDefinition = new ModuleSettingSectionDefinition(section.Key, section.Title, section.Description, section.IconGlyph, []);
            var clone = new ModuleSettingSectionViewModel(cloneDefinition);
            foreach (var item in items)
            {
                clone.Items.Add(item);
            }
            FilteredModuleSettingSections.Add(clone);
        }
    }

    private void UpdateModuleDirtyState()
    {
        ModuleHasUnsavedChanges = ModuleSettingSections.SelectMany(x => x.Items).Any(x => x.IsDirty);
    }

    private AppModuleDto? FindInstalledApp(string appKey)
    {
        var normalized = _moduleSettingsService.NormalizeAppKey(appKey);
        return InstalledApps.FirstOrDefault(app =>
            string.Equals(_moduleSettingsService.NormalizeAppKey(app.AppKey), normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(app.AppKey, appKey, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshInstalledAppsDetail()
    {
        if (CurrentPage is null || !IsInstalledAppsDetailVisible)
        {
            return;
        }

        var row = CurrentPage.Rows.FirstOrDefault(x => string.Equals(x.TargetPageKey, "apps-installed", StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        CurrentDetail = BuildDetail(CurrentPage, row);
        OnPropertyChanged(nameof(IsInstalledAppsDetailVisible));
    }

    private async Task LoadAsync()
    {
        var connection = await _databaseAdminService.GetPrimaryConnectionAsync();
        SqlServerName = connection.Host;
        DatabaseName = connection.DatabaseName;
        ConnectionStatus = connection.Status;

        CurrentUser = _currentUserService.CurrentUser?.DisplayName ?? CurrentUser;

        BackupRecords.Clear();
        foreach (var b in await _backupService.GetRecentAsync()) BackupRecords.Add(b);

        ServiceStatuses.Clear();
        foreach (var s in await _serviceStatusService.GetServicesAsync()) ServiceStatuses.Add(s);

        SecurityUsers.Clear();
        foreach (var user in await _userAccountService.GetUsersAsync()) SecurityUsers.Add(user);

        SecurityRoles.Clear();
        foreach (var role in await _roleService.GetRolesAsync()) SecurityRoles.Add(role);

        SecurityPermissions.Clear();
        foreach (var permission in await _roleService.GetPermissionsAsync()) SecurityPermissions.Add(permission);

        SecurityLoginAudits.Clear();
        foreach (var audit in await _userAccountService.GetLoginAuditsAsync()) SecurityLoginAudits.Add(audit);

        AvailablePrinters.Clear();
        foreach (var printer in await _printerSettingsService.GetAvailablePrintersAsync()) AvailablePrinters.Add(printer);

        PrinterPermissions.Clear();
        foreach (var permission in await _printerSettingsService.GetPrinterPermissionsAsync()) PrinterPermissions.Add(permission);

        RefreshDynamicRows();
        ApplyRowsFilter();
    }

    private void RefreshDynamicRows()
    {
        var lastBackup = BackupRecords.FirstOrDefault()?.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Chưa có";
        var appCount = InstalledApps.Count > 0 ? InstalledApps.Count : AppSettings.Count;

        SetRowValue("home-db", ConnectionStatus);
        SetRowValue("home-api", ServiceStatuses.FirstOrDefault(x => x.ServiceName.Contains("API", StringComparison.OrdinalIgnoreCase))?.Status ?? "Running");
        SetRowValue("home-backup", lastBackup);
        SetRowValue("home-vfc", "Ready");
        SetRowValue("home-apps", $"{appCount} modules");
        SetRowValue("home-security", "Configured");

        SetRowValue("db-connection", SqlServerName);
        SetRowValue("net-api", "Running");
        SetRowValue("backup-schedule", lastBackup);
        SetRowValue("vfc-drives", "5 drives");
    }

    private void SetRowValue(string rowId, string value)
    {
        foreach (var page in _pages.Values)
        {
            var row = page.Rows.FirstOrDefault(x => string.Equals(x.Id, rowId, StringComparison.OrdinalIgnoreCase));
            if (row is not null)
            {
                row.Value = value;
            }
        }
    }

    private void BuildCategories()
    {
        Categories.Clear();
        Categories.Add(new SettingsCategoryItem("home", "Home", "\uE80F", "Tổng quan cấu hình và trạng thái."));
        Categories.Add(new SettingsCategoryItem("system", "Hệ thống", "\uE770", "Máy chủ, chế độ vận hành và hiệu năng."));
        Categories.Add(new SettingsCategoryItem("database", "Cơ sở dữ liệu", "\uE9D2", "SQL Server, schema và bảo trì dữ liệu."));
        Categories.Add(new SettingsCategoryItem("network", "Kết nối mạng", "\uE839", "API, NAS, POS/FO client và ports."));
        Categories.Add(new SettingsCategoryItem("vfc", "Virtual File Center", "\uE8B7", "Virtual drives, mapping, index và retention."));
        Categories.Add(new SettingsCategoryItem("apps", "Ứng dụng", "\uE71D", "Cấu hình chi tiết cho từng module/app."));
        Categories.Add(new SettingsCategoryItem("users", "Người dùng & phân quyền", "\uE716", "Users, roles, policy và permission matrix."));
        Categories.Add(new SettingsCategoryItem("clients", "Máy trạm & dịch vụ", "\uE943", "Client registry, sync service, service health."));
        Categories.Add(new SettingsCategoryItem("printing", "In ấn & mẫu biểu", "\uE749", "Printer, template và export."));
        Categories.Add(new SettingsCategoryItem("backup", "Sao lưu & phục hồi", "\uE7B8", "Backup schedule, restore và verify."));
        Categories.Add(new SettingsCategoryItem("security", "Nhật ký & bảo mật", "\uE72E", "Audit, log và chính sách bảo mật."));
        Categories.Add(new SettingsCategoryItem("appearance", "Giao diện", "\uE790", "Theme, wallpaper, taskbar, snap."));
        Categories.Add(new SettingsCategoryItem("maintenance", "Cập nhật & bảo trì", "\uE895", "Version, update, maintenance tools."));
        Categories.Add(new SettingsCategoryItem("about", "Thông tin hệ thống", "\uE946", "About, runtime, diagnostics."));
        ApplyCategoryFilter();
    }

    private void BuildPages()
    {
        _pages.Clear();
        _pages["home"] = Page("home", "Home", "Trung tâm cài đặt", "\uE80F", [
            Row("home-db", "\uE9D2", "SQL Server connection", "Kết nối cơ sở dữ liệu trung tâm", targetPageKey: "database-sql-connection"),
            Row("home-api", "\uE968", "API Server", "Base URL, port, HTTPS", targetPageKey: "network-api-server"),
            Row("home-backup", "\uE7B8", "Backup schedule", "Lịch backup tự động", targetPageKey: "backup-schedule"),
            Row("home-vfc", "\uE8B7", "Virtual drives", "Trạng thái ổ ảo", targetPageKey: "vfc-virtual-drives"),
            Row("home-apps", "\uE71D", "Ứng dụng đã cài", "Danh sách module ASMILE", targetPageKey: "apps-installed"),
            Row("home-security", "\uE72E", "Nhật ký & bảo mật", "Audit và log retention", targetPageKey: "logs-security")
        ]);
        _pages["system"] = Page("system", "Hệ thống", "Thông tin máy chủ và vận hành", "\uE770", [
            Row("system-server", "\uE946", "Thông tin máy chủ", "Tên máy, IP, tài khoản, múi giờ"),
            Row("system-mode", "\uE7EF", "Chế độ vận hành", "Central Server, maintenance mode"),
            Row("system-folders", "\uE8B7", "Thư mục hệ thống", "Data root, temp, export"),
            Row("system-performance", "\uEA80", "Hiệu năng", "Cache, background tasks, index"),
            Row("system-storage", "\uE7F1", "Lưu trữ", "Virtual drives, file storage"),
            Row("system-remote", "\uE968", "Remote access", "Cho phép máy trạm/API kết nối")
        ]);
        _pages["database"] = Page("database", "Cơ sở dữ liệu", "Cấu hình SQL và schema", "\uE9D2", [
            Row("db-connection", "\uE9D2", "SQL Server connection", "Server, database, auth mode", targetPageKey: "database-sql-connection"),
            Row("db-info", "\uE8F1", "Database information", "Size, collation, compatibility"),
            Row("db-schema", "\uE713", "Schema & migrations", "Version, pending migrations"),
            Row("db-objects", "\uE8A5", "Tables / Views / Stored procedures", "Kiểm tra schema objects"),
            Row("db-maint", "\uE9D9", "Maintenance", "Reindex, optimize, cleanup"),
            Row("db-integrity", "\uE9E9", "Integrity check", "DBCC, consistency scan"),
            Row("db-security", "\uE72E", "Connection string security", "Encrypt và credential policy")
        ]);
        _pages["network"] = Page("network", "Kết nối mạng", "API, NAS và clients", "\uE839", [
            Row("net-api", "\uE968", "API Server", "Base URL, port, HTTPS", targetPageKey: "network-api-server"),
            Row("net-discovery", "\uE895", "Network discovery", "IP range, scan interval"),
            Row("net-shared", "\uE8D1", "Shared folders / NAS", "NAS path và mapping"),
            Row("net-printer", "\uE749", "Printer server", "Printer host và queue"),
            Row("net-pos", "\uE7F4", "POS terminals", "Trạng thái POS endpoints"),
            Row("net-fo", "\uE8FA", "FO clients", "Trạng thái kết nối FO"),
            Row("net-fw", "\uE7BA", "Firewall/Ports", "Port rules và profile")
        ]);
        _pages["vfc"] = Page("vfc", "Virtual File Center", "Cấu hình VFC", "\uE8B7", [
            Row("vfc-drives", "\uE7F1", "Virtual drives", "System/Accounting/Sales/HR/Logs paths", targetPageKey: "vfc-virtual-drives"),
            Row("vfc-mapping", "\uE838", "Folder mapping", "Virtual path -> physical root"),
            Row("vfc-index", "\uE81E", "Document index", "Index status và rebuild"),
            Row("vfc-root", "\uE8B7", "File storage root", "Root path và permission"),
            Row("vfc-retention", "\uE823", "Retention policy", "File/log retention"),
            Row("vfc-rebuild", "\uE895", "Rebuild virtual index", "Rebuild toàn bộ chỉ mục"),
            Row("vfc-repair", "\uEA39", "Repair broken links", "Sửa liên kết hỏng")
        ]);
        _pages["apps"] = Page("apps", "Ứng dụng", "Danh sách module", "\uE71D", [
            Row("apps-installed", "\uE719", "Ứng dụng đã cài", "Mở cấu hình nâng cao theo từng ứng dụng", targetPageKey: "apps-installed"),
            Row("apps-default", "\uE8B0", "Default apps", "App mặc định theo nghiệp vụ"),
            Row("apps-startup", "\uE7E8", "Startup apps", "Tự động mở app khi khởi động"),
            Row("apps-permissions", "\uE72E", "App permissions", "Quyền truy cập theo nhóm"),
            Row("apps-paths", "\uE8B7", "App data paths", "Data/export/template paths"),
            Row("apps-advanced", "\uE9CE", "Advanced app settings", "Cache, reset, diagnostics")
        ]);
        _pages["users"] = Page("users", "Người dùng & phân quyền", "Quản lý user/role", "\uE716", [
            Row("users-list", "\uE716", "Users", "User list and account actions"),
            Row("users-roles", "\uE902", "Roles", "Role definitions"),
            Row("users-matrix", "\uE8D9", "Permission matrix", "Role permission matrix"),
            Row("users-policy", "\uE72E", "Login policy", "Password policy và lockout"),
            Row("users-approval", "\uE8FB", "Admin approval", "Yêu cầu duyệt thao tác nhạy cảm"),
            Row("users-session", "\uE823", "Session timeout", "Timeout phiên làm việc"),
            Row("users-audit", "\uE7BA", "Audit user activity", "Theo dõi thao tác người dùng")
        ]);
        _pages["clients"] = Page("clients", "Máy trạm & dịch vụ", "Client registry và sync", "\uE943", [
            Row("clients-reg", "\uE8D7", "Registered clients", "Danh sách máy trạm"),
            Row("clients-pos", "\uE7F4", "POS terminals", "POS endpoints và sync"),
            Row("clients-fo", "\uE8FA", "FO clients", "FO status"),
            Row("clients-wh", "\uE7F1", "Warehouse clients", "Warehouse client status"),
            Row("clients-sync", "\uE895", "Sync service", "Interval và conflict strategy"),
            Row("clients-print", "\uE749", "Print service", "Print queue health"),
            Row("clients-health", "\uEA39", "Service health", "Heartbeat và cảnh báo")
        ]);
        _pages["printing"] = Page("printing", "In ấn & mẫu biểu", "Máy in và template", "\uE749", [
            Row("print-default", "\uE749", "Default printers", "Phiếu nhập/xuất/hóa đơn"),
            Row("print-voucher", "\uE8A5", "Voucher templates", "Mẫu phiếu nhập/xuất"),
            Row("print-invoice", "\uE8A5", "Invoice templates", "Mẫu hóa đơn"),
            Row("print-debt", "\uE8A5", "Debt report templates", "Mẫu công nợ"),
            Row("print-hr", "\uE8A5", "HR templates", "Mẫu nhân sự"),
            Row("print-export", "\uE8A4", "Export PDF/Excel", "Quy tắc xuất file")
        ]);
        _pages["backup"] = Page("backup", "Sao lưu & phục hồi", "Cấu hình backup", "\uE7B8", [
            Row("backup-schedule", "\uE787", "Backup schedule", "Enable, schedule, retention", targetPageKey: "backup-schedule"),
            Row("backup-now", "\uE898", "Backup now", "Chạy backup ngay"),
            Row("backup-history", "\uE81C", "Backup history", "Lịch sử backup"),
            Row("backup-db", "\uE7B8", "Restore database", "Khôi phục database"),
            Row("backup-files", "\uE8B7", "Restore files", "Khôi phục file"),
            Row("backup-policy", "\uE823", "Retention policy", "Số bản giữ lại"),
            Row("backup-verify", "\uE9CE", "Verify backup", "Kiểm tra toàn vẹn backup")
        ]);
        _pages["security"] = Page("security", "Nhật ký & bảo mật", "Cấu hình bảo mật", "\uE72E", [
            Row("sec-audit", "\uE7BA", "Audit log", "Theo dõi thao tác nhạy cảm", targetPageKey: "logs-security"),
            Row("sec-error", "\uEA39", "Error log", "Nhật ký lỗi", targetPageKey: "logs-security"),
            Row("sec-login", "\uE77B", "Login log", "Nhật ký đăng nhập", targetPageKey: "logs-security"),
            Row("sec-sync", "\uE895", "Sync log", "Nhật ký đồng bộ", targetPageKey: "logs-security"),
            Row("sec-sensitive", "\uE72E", "Sensitive actions", "Yêu cầu admin confirmation", targetPageKey: "logs-security"),
            Row("sec-confirm", "\uE8FB", "Security confirmation", "Xác nhận thao tác nguy hiểm", targetPageKey: "logs-security"),
            Row("sec-export", "\uE8A4", "Export logs", "Xuất log điều tra", targetPageKey: "logs-security")
        ]);
        _pages["appearance"] = Page("appearance", "Giao diện", "Cấu hình UI", "\uE790", [
            Row("ui-theme", "\uE790", "Theme", "Light/Dark/System", targetPageKey: "appearance-main"),
            Row("ui-wallpaper", "\uE91B", "Wallpaper", "Ảnh nền desktop", targetPageKey: "appearance-main"),
            Row("ui-icons", "\uE8FD", "Desktop icons", "Hiện/ẩn biểu tượng", targetPageKey: "appearance-main"),
            Row("ui-taskbar", "\uE7C5", "Taskbar", "Hiện/ẩn taskbar", targetPageKey: "appearance-main"),
            Row("ui-window", "\uE8D6", "Window behavior", "Remember position", targetPageKey: "appearance-main"),
            Row("ui-snap", "\uE92E", "Snap/grid", "Căn lưới cửa sổ", targetPageKey: "appearance-main")
        ]);
        _pages["maintenance"] = Page("maintenance", "Cập nhật & bảo trì", "Tools bảo trì", "\uE895", [
            Row("maint-version", "\uE81E", "Current version", "ASMILE BO version"),
            Row("maint-check", "\uE895", "Check for updates", "Kiểm tra cập nhật"),
            Row("maint-channel", "\uE8B0", "Update channel", "Stable/Beta/Internal"),
            Row("maint-tools", "\uE9CE", "Maintenance tools", "Bộ công cụ bảo trì"),
            Row("maint-cache", "\uE74D", "Clear cache", "Xóa cache module"),
            Row("maint-index", "\uE81E", "Rebuild indexes", "Rebuild chỉ mục"),
            Row("maint-repair", "\uEA39", "Repair data links", "Sửa liên kết lỗi")
        ]);
        _pages["about"] = Page("about", "Thông tin hệ thống", "Thông tin sản phẩm", "\uE946", [
            Row("about-product", "\uE946", "About ASMILE BO", "Enterprise Shell info"),
            Row("about-license", "\uE8A5", "License", "Trạng thái bản quyền"),
            Row("about-runtime", "\uE943", "Runtime", ".NET runtime và dependency"),
            Row("about-os", "\uE7F8", "OS", "Phiên bản Windows"),
            Row("about-machine", "\uE770", "Machine", "Machine name và hardware"),
            Row("about-diag", "\uE9CE", "Diagnostics", "Export diagnostic package")
        ]);

        if (_pages.TryGetValue("home", out var homePage))
        {
            HomeDatabaseRow = homePage.Rows.FirstOrDefault(x => x.Id == "home-db");
            HomeBackupRow = homePage.Rows.FirstOrDefault(x => x.Id == "home-backup");
            HomeSecurityRow = homePage.Rows.FirstOrDefault(x => x.Id == "home-security");
            OnPropertyChanged(nameof(HomeDatabaseRow));
            OnPropertyChanged(nameof(HomeBackupRow));
            OnPropertyChanged(nameof(HomeSecurityRow));
        }
    }

    private void BuildAppSettings()
    {
        var iconBase = @"D:\ASMILE-BO\Windows_Icon\Dock Icons\ICO's";
        AppSettings.Clear();
        var apps = new[]
        {
            ("overview", "Tổng quan", "Dashboard tổng hợp", "dashboard.ico"),
            ("vfc", "Virtual File Center", "Quản lý file ảo", "folder.ico"),
            ("inventory", "Quản lý xuất nhập tồn", "Nghiệp vụ nhập xuất tồn", "devices.ico"),
            ("warehouse", "Quản lý kho", "Danh mục và tồn kho", "archive.ico"),
            ("receipt", "Phiếu nhập", "Quản lý chứng từ nhập kho", "download.ico"),
            ("issue", "Phiếu xuất", "Quản lý chứng từ xuất kho", "file.ico"),
            ("ar", "Công nợ phải thu", "Theo dõi công nợ khách hàng", "book.ico"),
            ("ap", "Công nợ phải trả", "Theo dõi công nợ nhà cung cấp", "books.ico"),
            ("customer", "Quản lý khách hàng", "Danh mục và thông tin khách hàng", "address-book.ico"),
            ("supplier", "Quản lý nhà cung cấp", "Danh mục nhà cung cấp", "components.ico"),
            ("hr", "Bộ phận & Nhân sự", "Quản lý bộ phận, nhân sự", "device-desktop.ico"),
            ("settings", "Cài đặt hệ thống", "Thiết lập hệ thống trung tâm", "adjustments.ico"),
            ("pos", "POS Client", "Ứng dụng POS tại điểm bán", "device-tablet.ico"),
            ("fo", "FO Client", "Ứng dụng FO phục vụ", "device-mobile.ico"),
            ("accounting", "Accounting Client", "Ứng dụng kế toán", "calculator.ico"),
            ("wh-client", "Warehouse Client", "Ứng dụng kho", "device-laptop.ico")
        };

        foreach (var app in apps)
        {
            AppSettings.Add(new AppSettingsItemViewModel
            {
                AppKey = app.Item1,
                AppName = app.Item2,
                Description = app.Item3,
                IconPath = Path.Combine(iconBase, app.Item4),
                DataPath = $@"D:\ACCOUNTING\Apps\{app.Item2}",
                ExportPath = $@"D:\ACCOUNTING\Exports\{app.Item2}",
                TemplatePath = $@"D:\ACCOUNTING\Templates\{app.Item2}",
                PermissionGroup = "Admin"
            });
        }
    }

    private SettingsPageViewModel Page(string key, string title, string subtitle, string icon, IEnumerable<SettingItemViewModel> rows)
    {
        var page = new SettingsPageViewModel(key, title, subtitle, icon);
        foreach (var row in rows) page.Rows.Add(row);
        return page;
    }

    private static SettingItemViewModel Row(string id, string icon, string title, string description, string value = "", string badge = "", bool isSensitive = false, string? targetPageKey = null)
    {
        return new SettingItemViewModel(id, icon, title, description, value, badge, targetPageKey ?? id)
        {
            IsSensitive = isSensitive,
            RequiredPermission = Permission.ViewSystemSettings
        };
    }

    private void ApplyCategoryFilter()
    {
        var k = (SearchText ?? string.Empty).Trim();
        FilteredCategories.Clear();
        var src = string.IsNullOrWhiteSpace(k)
            ? Categories
            : Categories.Where(c => c.Title.Contains(k, StringComparison.OrdinalIgnoreCase) || c.Description.Contains(k, StringComparison.OrdinalIgnoreCase));

        foreach (var c in src) FilteredCategories.Add(c);
        if (SelectedCategory is null || !FilteredCategories.Contains(SelectedCategory))
        {
            SelectedCategory = FilteredCategories.FirstOrDefault() ?? Categories.FirstOrDefault();
        }
    }

    private void ApplyRowsFilter()
    {
        VisibleRows.Clear();
        if (CurrentPage is null) return;

        var k = (SearchText ?? string.Empty).Trim();
        var rows = string.IsNullOrWhiteSpace(k)
            ? CurrentPage.Rows
            : CurrentPage.Rows.Where(x => x.SearchKeywords.Contains(k, StringComparison.OrdinalIgnoreCase) || x.Title.Contains(k, StringComparison.OrdinalIgnoreCase));

        foreach (var row in rows) VisibleRows.Add(row);
    }

    private SettingsDetailViewModel BuildDetail(SettingsPageViewModel page, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > {page.Title} > {row.Title}"
        };

        if (string.Equals(row.TargetPageKey, "apps-installed", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Ứng dụng đã cài", "Danh sách module ASMILE");
            var canonicalApps = BuildInstalledAppCatalog();

            var appSearch = (SearchText ?? string.Empty).Trim();
            foreach (var app in canonicalApps)
            {
                var installed = FindInstalledApp(app.AppKey);
                var value = installed?.IsEnabled == false ? "Tạm tắt" : "Đang bật";
                if (!string.Equals(InstalledAppStatusFilter, "Tất cả", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(value, InstalledAppStatusFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(appSearch)
                    && !app.Title.Contains(appSearch, StringComparison.OrdinalIgnoreCase)
                    && !app.Description.Contains(appSearch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                card.Items.Add(new SettingItemViewModel($"app-advanced-{app.AppKey}", app.Icon, app.Title, app.Description, value, targetPageKey: $"app-advanced:{app.AppKey}"));
            }
            detail.Cards.Add(card);
            return detail;
        }

        if (page.Key == "users" && row.Id == "users-list")
        {
            var usersCard = new SettingCardViewModel("Người dùng", "Danh sách tài khoản hệ thống");
            foreach (var user in SecurityUsers)
            {
                usersCard.Items.Add(new SettingItemViewModel($"user-{user.Id}", "\uE77B", user.UserName, user.DisplayName, user.IsLocked ? "Khóa" : (user.IsActive ? "Hoạt động" : "Ngưng")));
            }
            detail.Cards.Add(usersCard);

            var actions = new SettingCardViewModel("User actions", "Tạo/sửa/khóa/mở khóa/reset mật khẩu");
            actions.Items.Add(new SettingItemViewModel("user-act-add", "\uE710", "Add user", "Tạo user mới với mật khẩu khởi tạo", "Ready", targetPageKey: "users-action-add"));
            actions.Items.Add(new SettingItemViewModel("user-act-reset", "\uE72E", "Reset password", "Đặt lại mật khẩu + MustChangePassword", "Ready", targetPageKey: "users-action-reset"));
            actions.Items.Add(new SettingItemViewModel("user-act-unlock", "\uE785", "Unlock user", "Mở khóa user sau khi đăng nhập sai nhiều lần", "Ready", targetPageKey: "users-action-unlock"));
            detail.Cards.Add(actions);
            return detail;
        }

        if (page.Key == "users" && row.Id == "users-roles")
        {
            var rolesCard = new SettingCardViewModel("Vai trò", "Danh sách vai trò");
            foreach (var role in SecurityRoles)
            {
                rolesCard.Items.Add(new SettingItemViewModel($"role-{role.Id}", "\uE902", role.Name, role.Description ?? string.Empty, role.IsSystem ? "Hệ thống" : "Tùy chỉnh"));
            }
            detail.Cards.Add(rolesCard);
            return detail;
        }

        if (page.Key == "users" && row.Id == "users-matrix")
        {
            var matrixCard = new SettingCardViewModel("Permission matrix", "Vai trò và quyền thao tác");
            foreach (var role in SecurityRoles)
            {
                matrixCard.Items.Add(new SettingItemViewModel($"perm-role-{role.Id}", "\uE902", role.Name, "View/Create/Edit/Delete/Approve", "Configured"));
            }
            matrixCard.Items.Add(new SettingItemViewModel("perm-save", "\uE74E", "Save role permissions", "Lưu bộ quyền mặc định", "Apply", targetPageKey: "users-action-save-permissions"));
            detail.Cards.Add(matrixCard);
            return detail;
        }

        var generic = new SettingCardViewModel("Cấu hình đang được chuẩn bị", "Mục này chưa có setting definition để chỉnh trực tiếp.");
        generic.Items.Add(new SettingItemViewModel("todo1", "\uE8A4", "TODO", "Bổ sung definitions trong SettingsRegistry để bật chế độ chỉnh sửa thật.", "Pending"));
        generic.Items.Add(new SettingItemViewModel("todo2", "\uE8FD", row.Title, row.Description, row.Value));
        generic.Items[0].IsClickable = false;
        generic.Items[1].IsClickable = false;
        detail.Cards.Add(generic);
        return detail;
    }

    private SettingsDetailViewModel BuildAppAdvancedDetail(SettingsPageViewModel page, string appKey, string appName, string appDescription)
    {
        if (string.Equals(appKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
        {
            var ledgerDetail = new SettingsDetailViewModel
            {
                Title = "Sổ cái",
                Subtitle = "Cài đặt chính cho kế toán tổng hợp, tài khoản, hạch toán và báo cáo.",
                Breadcrumb = "Cài đặt > Ứng dụng > Ứng dụng đã cài > Sổ cái"
            };

            var accounting = new SettingCardViewModel("Cài đặt chính", "Các thiết lập nghiệp vụ của sổ cái");
            accounting.Items.Add(new SettingItemViewModel("general-ledger-settings", "\uE713", "Cài đặt sổ cái", _generalLedgerSettingsSummary, _generalLedgerSettingsValue, targetPageKey: "general-ledger-settings"));
            accounting.Items.Add(new SettingItemViewModel("general-ledger-accounts", "\uE8EC", "Cài đặt tài khoản", _generalLedgerAccountsSummary, _generalLedgerAccountsValue, targetPageKey: "general-ledger-accounts"));
            accounting.Items.Add(new SettingItemViewModel("general-ledger-posting-settings", "\uE8A5", "Thiết lập hạch toán tự động", _generalLedgerPostingSummary, _generalLedgerPostingValue, targetPageKey: "general-ledger-posting-settings"));
            accounting.Items.Add(new SettingItemViewModel("general-ledger-period", "\uE787", "Kỳ kế toán", _generalLedgerPeriodSummary, _generalLedgerPeriodValue, targetPageKey: "general-ledger-period"));
            accounting.Items.Add(new SettingItemViewModel("general-ledger-reports", "\uE9D9", "Báo cáo sổ cái", _generalLedgerReportsSummary, _generalLedgerReportsValue, targetPageKey: "general-ledger-reports"));
            ledgerDetail.Cards.Add(accounting);

            var status = new SettingCardViewModel("Trạng thái ứng dụng", "Thiết lập chung");
            var ledgerApp = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, appKey, StringComparison.OrdinalIgnoreCase));
            status.Items.Add(new SettingItemViewModel("gl-status-enabled", "\uE73E", "Bật ứng dụng", "Cho phép module sổ cái hoạt động", ledgerApp?.IsEnabled == true ? "Bật" : "Tắt", targetPageKey: "general-ledger-toggle-enabled") { IsToggle = true, ToggleValue = ledgerApp?.IsEnabled == true });
            status.Items.Add(new SettingItemViewModel("gl-status-desktop", "\uE8FD", "Hiển thị Desktop", "Hiện biểu tượng sổ cái trên desktop", ledgerApp?.ShowOnDesktop == true ? "Bật" : "Tắt", targetPageKey: "general-ledger-toggle-desktop") { IsToggle = true, ToggleValue = ledgerApp?.ShowOnDesktop == true });
            status.Items.Add(new SettingItemViewModel("gl-status-quick-posting", "\uE8A5", "Cho phép hạch toán nhanh", "Cho phép tạo bút toán nhanh từ chứng từ bán hàng, mua hàng, thu chi.", LedgerAllowQuickPosting ? "Bật" : "Tắt", targetPageKey: "general-ledger-toggle-quick-posting") { IsToggle = true, ToggleValue = LedgerAllowQuickPosting });
            status.Items.Add(new SettingItemViewModel("gl-status-auto-balance", "\uE9D9", "Tự động kiểm tra cân đối", "Tự kiểm tra Nợ/Có sau khi thêm hoặc sửa bút toán.", LedgerAutoBalanceCheck ? "Bật" : "Tắt", targetPageKey: "general-ledger-toggle-auto-balance") { IsToggle = true, ToggleValue = LedgerAutoBalanceCheck });
            ledgerDetail.Cards.Add(status);

            return ledgerDetail;
        }

        var detail = new SettingsDetailViewModel
        {
            Title = $"{appName} > Tùy chọn nâng cao",
            Subtitle = appDescription,
            Breadcrumb = $"Cài đặt > Ứng dụng > Ứng dụng đã cài > {appName}"
        };

        var general = new SettingCardViewModel("Trạng thái ứng dụng", "Thiết lập chung");
        var selected = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, appKey, StringComparison.OrdinalIgnoreCase));
        general.Items.Add(new SettingItemViewModel("g1", "\uE73E", "Bật ứng dụng", "Cho phép module hoạt động", selected?.IsEnabled == true ? "Bật" : "Tắt"));
        general.Items.Add(new SettingItemViewModel("g2", "\uE8FD", "Hiển thị Desktop", "Hiện biểu tượng trên desktop", selected?.ShowOnDesktop == true ? "Bật" : "Tắt"));
        general.Items.Add(new SettingItemViewModel("g3", "\uE7C5", "Ghim taskbar", "Hiện trên thanh taskbar", selected?.PinToTaskbar == true ? "Bật" : "Tắt"));
        general.Items.Add(new SettingItemViewModel("g4", "\uE968", "Cho phép client", "Client được truy cập module", selected?.AllowClientAccess == true ? "Bật" : "Tắt"));
        detail.Cards.Add(general);

        var paths = new SettingCardViewModel("Đường dẫn dữ liệu", "Dữ liệu ứng dụng");
        paths.Items.Add(new SettingItemViewModel("p1", "\uE8B7", "Data folder", "Thư mục dữ liệu", $@"D:\ACCOUNTING\Apps\{appName}"));
        paths.Items.Add(new SettingItemViewModel("p2", "\uE8A4", "Export folder", "Thư mục xuất", $@"D:\ACCOUNTING\Exports\{appName}"));
        paths.Items.Add(new SettingItemViewModel("p3", "\uE8A5", "Template folder", "Thư mục mẫu", $@"D:\ACCOUNTING\Templates\{appName}"));
        detail.Cards.Add(paths);

        return detail;
    }

    private List<(string AppKey, string Title, string Description, string Icon)> BuildInstalledAppCatalog()
    {
        var catalog = new Dictionary<string, (string AppKey, string Title, string Description, string Icon)>(StringComparer.OrdinalIgnoreCase);

        void AddOrUpdate(string appKey, string title, string description, string icon)
        {
            if (string.IsNullOrWhiteSpace(appKey))
            {
                return;
            }

            var normalized = _moduleSettingsService.NormalizeAppKey(appKey);
            if (!catalog.ContainsKey(normalized))
            {
                catalog[normalized] = (normalized, title, description, icon);
            }
        }

        foreach (var def in _moduleSettingsService.GetDefinitions())
        {
            AddOrUpdate(def.AppKey, def.Title, def.Description, def.IconGlyph);
        }

        foreach (var app in InstalledApps)
        {
            var iconGlyph = string.IsNullOrWhiteSpace(app.Icon) || IsIconFileReference(app.Icon) ? "\uE8A5" : app.Icon;
            AddOrUpdate(app.AppKey, app.Name, app.Description ?? string.Empty, iconGlyph);
        }

        AddOrUpdate("general-ledger", "Sổ cái", "Cài đặt kế toán tổng hợp, tài khoản, hạch toán và báo cáo", "\uE8A5");
        AddOrUpdate("training-data", "Dữ liệu mẫu", "Onboarding, reset và seed demo", "\uE8B8");

        return catalog.Values
            .OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool IsIconFileReference(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return false;
        }

        var extension = Path.GetExtension(icon);
        return string.Equals(extension, ".ico", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase);
    }

    private void BuildModuleIconOptions()
    {
        ModuleIconOptions.Clear();

        var iconRoot = ModuleIconRoot;
        if (!Directory.Exists(iconRoot))
        {
            return;
        }

        var files = Directory.EnumerateFiles(iconRoot, "*.ico", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(iconRoot, "*.png", SearchOption.AllDirectories))
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ModuleIconOptions.Add(new AppIconOptionViewModel(file));
        }
    }

    private AppIconOptionViewModel? FindIconOption(string? icon)
    {
        var path = ResolveIconPath(icon);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return ModuleIconOptions.FirstOrDefault(x => string.Equals(x.IconPath, path, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveIconPath(string? icon)
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

    private async Task LoadAppearanceIconPickerAsync()
    {
        InstalledApps.Clear();
        foreach (var app in await _appModuleService.GetAppsAsync())
        {
            InstalledApps.Add(app);
        }

        SelectedIconModule = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, "menu-recipe", StringComparison.OrdinalIgnoreCase))
            ?? InstalledApps.FirstOrDefault();
    }

    private async Task ReloadEditableDetailAsync()
    {
        if (IsLedgerAccountSettingsVisible)
        {
            await LoadLedgerAccountSettingsAsync();
            return;
        }

        if (SelectedInstalledApp is not null)
        {
            await LoadAppAdvancedEditorAsync(SelectedInstalledApp.AppKey);
        }
    }

    private async Task LoadEditableDetailAsync(string rowRoute)
    {
        ClearDetailEditorStates();
        _scopeMap.Clear();
        _scopeTargetMap.Clear();

        var map = new Dictionary<string, string[]>
        {
            ["database-sql-connection"] = ["Database.ServerName", "Database.DatabaseName", "Database.AuthenticationMode", "Database.UserName", "Database.Password", "Database.CommandTimeout", "Database.EnableAutoMigration"],
            ["network-api-server"] = ["Network.ApiBaseUrl", "Network.ApiPort", "Network.EnableHttps", "Network.AllowClientDiscovery", "Network.ScanIntervalSeconds"],
            ["backup-schedule"] = ["Backup.EnableAutoBackup", "Backup.BackupPath", "Backup.Schedule", "Backup.RetentionCount", "Backup.CompressBackup", "Backup.VerifyAfterBackup"],
            ["vfc-virtual-drives"] = ["VFC.EnableVirtualDrives", "VFC.SystemDriveEnabled", "VFC.SystemDrivePath", "VFC.AccountingDriveEnabled", "VFC.AccountingDrivePath", "VFC.SalesDriveEnabled", "VFC.SalesDrivePath", "VFC.HrDriveEnabled", "VFC.HrDrivePath", "VFC.LogDriveEnabled", "VFC.LogDrivePath"],
            ["logs-security"] = ["Logs.EnableAuditLog", "Logs.EnableErrorLog", "Logs.LogLevel", "Logs.RetentionDays", "Security.RequireAdminConfirmation"],
            ["users-session"] = ["Security.SessionTimeoutMinutes", "Security.MaxFailedLoginAttempts"],
            ["sec-sensitive"] = ["Security.RequireAdminConfirmation"],
            ["appearance-main"] = ["Appearance.Theme", "Appearance.WallpaperPath", "Appearance.ShowDesktopIcons", "Appearance.ShowTaskbar"]
        };

            IsAppearanceIconPickerVisible = string.Equals(rowRoute, "appearance-main", StringComparison.OrdinalIgnoreCase);
            if (!map.TryGetValue(rowRoute, out var keys))
            {
                IsDirty = false;
                return;
            }

            foreach (var key in keys)
            {
                var scope = SettingScope.System;
                var target = (string?)null;
                var def = (await _settingsService.GetDefinitionsAsync(scope, target)).FirstOrDefault(x => x.Key == key);
                if (def is null)
                {
                    scope = SettingScope.User;
                    target = _currentUserService.CurrentUser?.Id.ToString();
                    def = (await _settingsService.GetDefinitionsAsync(scope, target)).FirstOrDefault(x => x.Key == key);
                }
                if (def is null) continue;

                var val = (await _settingsService.GetValueAsync(key, scope, target))?.Value ?? def.DefaultValue;
                var vm = new EditableSettingItemViewModel(def, val);
                vm.PropertyChanged += (_, __) => IsDirty = EditableItems.Any(x => x.IsDirty) || SelectedInstalledApp is not null;

                if (def.ValueType == SettingValueType.Enum)
                {
                    if (key == "Database.AuthenticationMode") { vm.Options.Add("Sql"); vm.Options.Add("Windows"); }
                    else if (key == "Appearance.Theme") { vm.Options.Add("Light"); vm.Options.Add("Dark"); vm.Options.Add("System"); }
                    else if (key == "Logs.LogLevel") { vm.Options.Add("Debug"); vm.Options.Add("Information"); vm.Options.Add("Warning"); vm.Options.Add("Error"); }
                }

                EditableItems.Add(vm);
                _scopeMap[key] = scope;
                _scopeTargetMap[key] = target;
            }

        HasEditableForm = EditableItems.Count > 0;
        if (IsAppearanceIconPickerVisible)
        {
            await LoadAppearanceIconPickerAsync();
        }
        IsDirty = false;
    }

    private async Task LoadAppAdvancedEditorAsync(string appKey)
    {
        if (_isLoadingAppAdvanced)
        {
            return;
        }

        _isLoadingAppAdvanced = true;
        try
        {
        EditableItems.Clear();
        LedgerAccounts.Clear();
        _scopeMap.Clear();
        _scopeTargetMap.Clear();
        InstalledApps.Clear();

        var apps = await _appModuleService.GetAppsAsync();
        foreach (var app in apps) InstalledApps.Add(app);
        SelectedInstalledApp = apps.FirstOrDefault(x => x.AppKey == appKey);

        if (string.Equals(appKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
        {
            await LoadGeneralLedgerMainSettingsSummaryAsync();
            IsLedgerAccountSettingsVisible = false;
            IsGeneralLedgerSettingsVisible = false;
            IsGeneralLedgerDetailFormVisible = false;
            IsGeneralLedgerPanelOpen = false;
            IsGeneralLedgerChildBreadcrumbVisible = false;
            GeneralLedgerChildBreadcrumbTitle = string.Empty;
            HasEditableForm = false;
            IsDirty = false;
            return;
        }

        await LoadAppSettingDefinitionsEditorAsync(appKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LoadAppAdvancedEditor failed for appKey={AppKey}", appKey);
            EditableItems.Clear();
            HasEditableForm = false;
            IsLedgerAccountSettingsVisible = false;
            IsGeneralLedgerSettingsVisible = false;
            IsGeneralLedgerDetailFormVisible = false;
            IsGeneralLedgerPanelOpen = false;
            IsGeneralLedgerChildBreadcrumbVisible = false;
            GeneralLedgerChildBreadcrumbTitle = string.Empty;
        }
        finally
        {
            _isLoadingAppAdvanced = false;
        }
    }

    private async Task OpenGeneralLedgerMainSettingsAsync()
    {
        await LoadAppAdvancedEditorAsync("general-ledger");
        await LoadGeneralLedgerConfigurationAsync();
        var app = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, "general-ledger", StringComparison.OrdinalIgnoreCase));
        CurrentDetail = BuildAppAdvancedDetail(CurrentPage!, "general-ledger", app?.Name ?? "Sổ cái", app?.Description ?? "Kế toán tổng hợp");
        IsGeneralLedgerChildBreadcrumbVisible = false;
        GeneralLedgerChildBreadcrumbTitle = string.Empty;
        IsGeneralLedgerSettingsVisible = true;
        IsGeneralLedgerDetailFormVisible = false;
        IsGeneralLedgerPanelOpen = false;
        HasEditableForm = false;
        IsDetailMode = true;
        RaisePageFlags();
    }

    private async Task OpenGeneralLedgerPanelAsync(string route)
    {
        await LoadGeneralLedgerConfigurationAsync();
        GeneralLedgerPanelKind = route switch
        {
            "general-ledger-posting-settings" => "posting",
            "general-ledger-period" => "period",
            "general-ledger-reports" => "reports",
            _ => "core"
        };
        GeneralLedgerPanelTitle = GeneralLedgerPanelKind switch
        {
            "posting" => "Thiết lập hạch toán tự động",
            "period" => "Kỳ kế toán",
            "reports" => "Báo cáo sổ cái",
            _ => "Cài đặt sổ cái"
        };
        if (CurrentDetail is not null)
        {
            SetBreadcrumbFromDetail(new SettingsDetailViewModel
            {
                Title = CurrentDetail.Title,
                Subtitle = CurrentDetail.Subtitle,
                Breadcrumb = $"Cài đặt > Ứng dụng > Ứng dụng đã cài > Sổ cái > {GeneralLedgerPanelTitle}"
            });
        }
        IsGeneralLedgerPanelOpen = true;
        RaiseGeneralLedgerPanelFlags();
    }

    private async Task OpenGeneralLedgerDetailAsync(string route)
    {
        await LoadGeneralLedgerConfigurationAsync();
        GeneralLedgerPanelKind = route switch
        {
            "general-ledger-posting-settings" => "posting",
            "general-ledger-period" => "period",
            "general-ledger-reports" => "reports",
            _ => "core"
        };
        GeneralLedgerPanelTitle = GeneralLedgerPanelKind switch
        {
            "posting" => "Thiết lập hạch toán tự động",
            "period" => "Kỳ kế toán",
            "reports" => "Báo cáo sổ cái",
            _ => "Cài đặt sổ cái"
        };

        CurrentDetail = new SettingsDetailViewModel
        {
            Title = GeneralLedgerPanelTitle,
            Subtitle = GeneralLedgerPanelKind switch
            {
                "posting" => "Cấu hình tài khoản mặc định cho các nghiệp vụ hạch toán tự động.",
                "period" => "Thiết lập kỳ làm việc, khóa kỳ và trạng thái kỳ kế toán.",
                "reports" => "Cấu hình mẫu in, xuất Excel/PDF và chữ ký trên báo cáo.",
                _ => "Cấu hình dữ liệu, kỳ kế toán và quy tắc ghi sổ của sổ cái."
            },
            Breadcrumb = $"Cài đặt > Ứng dụng > Ứng dụng đã cài > Sổ cái > {GeneralLedgerPanelTitle}"
        };
        IsGeneralLedgerSettingsVisible = true;
        IsGeneralLedgerDetailFormVisible = true;
        IsGeneralLedgerPanelOpen = false;
        HasEditableForm = false;
        IsLedgerAccountSettingsVisible = false;
        IsDetailMode = true;
        SetBreadcrumbFromDetail(CurrentDetail);
        RaiseGeneralLedgerPanelFlags();
        RaisePageFlags();
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerCore()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName) || string.IsNullOrWhiteSpace(LedgerCurrentPeriod))
        {
            _notificationService.Show("Vui lòng nhập đủ tên database và kỳ kế toán hiện tại.");
            return;
        }

        await SaveGeneralLedgerSettingsAsync([
            ("GeneralLedger.DataSource", LedgerDataSource),
            ("GeneralLedger.DatabaseName", DatabaseName),
            ("GeneralLedger.CurrentPeriod", LedgerCurrentPeriod),
            ("GeneralLedger.PostingMethod", LedgerPostingMethod),
            ("GeneralLedger.RoundingMode", LedgerRoundingMode),
            ("GeneralLedger.DefaultCurrency", LedgerDefaultCurrency),
            ("GeneralLedger.AllowVoiceRecording", BoolText(LedgerAllowVoiceRecording)),
            ("GeneralLedger.AllowEditPostedEntries", BoolText(LedgerAllowEditPostedEntries)),
            ("GeneralLedger.AllowDeleteEntries", BoolText(LedgerAllowDeleteEntries)),
            ("GeneralLedger.RequireBalancedBeforeSave", BoolText(LedgerRequireBalancedBeforeSave))
        ]);
        _notificationService.Show("Đã lưu cài đặt sổ cái.");
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerPosting()
    {
        if (GeneralLedgerPostingMaps.Any(x => string.IsNullOrWhiteSpace(x.SelectedAccountCode)))
        {
            _notificationService.Show("Vui lòng chọn đủ tài khoản mặc định.");
            return;
        }

        await SaveGeneralLedgerSettingsAsync(GeneralLedgerPostingMaps.Select(x => (Key: x.SettingKey, Value: (string?)x.SelectedAccountCode)));
        await LoadGeneralLedgerMainSettingsSummaryAsync();
        _notificationService.Show("Đã lưu thiết lập hạch toán tự động.");
    }

    [RelayCommand]
    private async Task RestoreGeneralLedgerPostingDefaults()
    {
        foreach (var map in GeneralLedgerPostingMaps)
        {
            map.SelectedAccountCode = map.DefaultAccountCode;
        }

        await SaveGeneralLedgerPosting();
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerPeriod()
    {
        if (!DateTime.TryParse(LedgerPeriodFromDate, out _) || !DateTime.TryParse(LedgerPeriodToDate, out _))
        {
            _notificationService.Show("Ngày kỳ kế toán không hợp lệ.");
            return;
        }

        await SaveGeneralLedgerSettingsAsync([
            ("GeneralLedger.CurrentPeriod", LedgerCurrentPeriod),
            ("GeneralLedger.PeriodFromDate", LedgerPeriodFromDate),
            ("GeneralLedger.PeriodToDate", LedgerPeriodToDate),
            ("GeneralLedger.PeriodStatus", LedgerPeriodStatus),
            ("GeneralLedger.LastLockedDate", LedgerLastLockedDate),
            ("GeneralLedger.LockedBy", LedgerLockedBy)
        ]);
        _notificationService.Show("Đã lưu kỳ kế toán.");
    }

    [RelayCommand]
    private async Task LockGeneralLedgerPeriod()
    {
        var dialog = new SettingsConfirmDialog(
            "Xác nhận khóa kỳ",
            "Sau khi khóa kỳ, người dùng không thể thêm/sửa/xóa bút toán trong kỳ này.");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        LedgerPeriodStatus = "Đã khóa";
        LedgerLastLockedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        LedgerLockedBy = CurrentUser;
        // TODO: enforce this lock in ledger entry create/update/delete services when backend write guards are added.
        await SaveGeneralLedgerPeriod();
    }

    [RelayCommand]
    private async Task UnlockGeneralLedgerPeriod()
    {
        LedgerPeriodStatus = "Đang mở";
        await SaveGeneralLedgerPeriod();
    }

    [RelayCommand]
    private async Task CreateGeneralLedgerPeriod()
    {
        LedgerCurrentPeriod = DateTime.Today.ToString("MM/yyyy");
        LedgerPeriodFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy");
        LedgerPeriodToDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)).ToString("dd/MM/yyyy");
        LedgerPeriodStatus = "Đang mở";
        await SaveGeneralLedgerPeriod();
    }

    [RelayCommand]
    private async Task MoveNextGeneralLedgerPeriod()
    {
        var baseDate = DateTime.TryParse($"01/{LedgerCurrentPeriod}", out var parsed) ? parsed : DateTime.Today;
        var next = baseDate.AddMonths(1);
        LedgerCurrentPeriod = next.ToString("MM/yyyy");
        LedgerPeriodFromDate = new DateTime(next.Year, next.Month, 1).ToString("dd/MM/yyyy");
        LedgerPeriodToDate = new DateTime(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month)).ToString("dd/MM/yyyy");
        LedgerPeriodStatus = "Đang mở";
        await SaveGeneralLedgerPeriod();
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerReports()
    {
        if (string.IsNullOrWhiteSpace(LedgerDefaultGeneralLedgerReport) || string.IsNullOrWhiteSpace(LedgerDefaultTrialBalanceReport))
        {
            _notificationService.Show("Vui lòng nhập đủ mẫu báo cáo mặc định.");
            return;
        }

        await SaveGeneralLedgerSettingsAsync([
            ("GeneralLedger.DefaultGeneralLedgerReport", LedgerDefaultGeneralLedgerReport),
            ("GeneralLedger.DefaultTrialBalanceReport", LedgerDefaultTrialBalanceReport),
            ("GeneralLedger.DefaultExportFormat", LedgerDefaultExportFormat),
            ("GeneralLedger.AllowPrintReports", BoolText(LedgerAllowPrintReports)),
            ("GeneralLedger.AllowExportExcel", BoolText(LedgerAllowExportExcel)),
            ("GeneralLedger.AllowExportPdf", BoolText(LedgerAllowExportPdf)),
            ("GeneralLedger.ShowAccountantSignature", BoolText(LedgerShowAccountantSignature)),
            ("GeneralLedger.ShowDirectorSignature", BoolText(LedgerShowDirectorSignature)),
            ("GeneralLedger.ReportFooterNote", LedgerReportFooterNote)
        ]);
        _notificationService.Show("Đã lưu thiết lập báo cáo sổ cái.");
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerStatus()
    {
        await SaveGeneralLedgerSettingsAsync([
            ("GeneralLedger.AllowQuickPosting", BoolText(LedgerAllowQuickPosting)),
            ("GeneralLedger.AutoBalanceCheck", BoolText(LedgerAutoBalanceCheck))
        ]);

        var app = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
            ?? await _appModuleService.GetAppAsync("general-ledger");
        if (app is not null)
        {
            await _appModuleService.UpdateAppAsync(new UpdateAppModuleRequest
            {
                AppKey = "general-ledger",
                Icon = app.Icon,
                IsEnabled = LedgerAppEnabled,
                ShowOnDesktop = LedgerShowOnDesktop,
                PinToTaskbar = app.PinToTaskbar,
                AllowClientAccess = app.AllowClientAccess,
                RequiredPermission = app.RequiredPermission,
                Reason = "Update general ledger status settings"
            });
        }

        await LoadAppAdvancedEditorAsync("general-ledger");
        await LoadGeneralLedgerConfigurationAsync();
        CurrentDetail = BuildAppAdvancedDetail(CurrentPage!, "general-ledger", "Sổ cái", "Kế toán tổng hợp");
        IsGeneralLedgerSettingsVisible = true;
        IsGeneralLedgerDetailFormVisible = false;
        IsGeneralLedgerPanelOpen = false;
        IsLedgerAccountSettingsVisible = false;
        HasEditableForm = false;
        IsDetailMode = true;
        SetBreadcrumbFromDetail(CurrentDetail);
        RaiseGeneralLedgerPanelFlags();
        RaisePageFlags();
        _notificationService.Show("Đã lưu trạng thái ứng dụng sổ cái.");
    }

    private async Task ApplyGeneralLedgerToggleAsync(string route)
    {
        switch (route)
        {
            case "general-ledger-toggle-enabled":
                LedgerAppEnabled = !LedgerAppEnabled;
                break;
            case "general-ledger-toggle-desktop":
                LedgerShowOnDesktop = !LedgerShowOnDesktop;
                break;
            case "general-ledger-toggle-quick-posting":
                LedgerAllowQuickPosting = !LedgerAllowQuickPosting;
                break;
            case "general-ledger-toggle-auto-balance":
                LedgerAutoBalanceCheck = !LedgerAutoBalanceCheck;
                break;
        }

        await SaveGeneralLedgerStatus();
    }

    [RelayCommand]
    private async Task CancelGeneralLedgerPanel()
    {
        if (IsGeneralLedgerDetailFormVisible)
        {
            await OpenGeneralLedgerMainSettingsAsync();
            return;
        }

        IsGeneralLedgerPanelOpen = false;
        if (CurrentDetail is not null)
        {
            SetBreadcrumbFromDetail(CurrentDetail);
        }
        RaiseGeneralLedgerPanelFlags();
    }

    private async Task LoadGeneralLedgerConfigurationAsync()
    {
        async Task<string> GetAppSettingAsync(string key, string fallback)
        {
            var value = await _settingsService.GetValueAsync(key, SettingScope.App, "general-ledger");
            return string.IsNullOrWhiteSpace(value?.Value) ? fallback : value.Value.Trim();
        }

        LedgerDataSource = await GetAppSettingAsync("GeneralLedger.DataSource", "SQL Server");
        DatabaseName = await GetAppSettingAsync("GeneralLedger.DatabaseName", DatabaseName);
        LedgerCurrentPeriod = await GetAppSettingAsync("GeneralLedger.CurrentPeriod", DateTime.Today.ToString("MM/yyyy"));
        LedgerPostingMethod = await GetAppSettingAsync("GeneralLedger.PostingMethod", "Nhật ký chung");
        LedgerRoundingMode = await GetAppSettingAsync("GeneralLedger.RoundingMode", "Làm tròn đến đồng");
        LedgerDefaultCurrency = await GetAppSettingAsync("GeneralLedger.DefaultCurrency", "VND");
        LedgerAllowVoiceRecording = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowVoiceRecording", "false"));
        LedgerAllowEditPostedEntries = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowEditPostedEntries", "false"));
        LedgerAllowDeleteEntries = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowDeleteEntries", "false"));
        LedgerRequireBalancedBeforeSave = ParseBool(await GetAppSettingAsync("GeneralLedger.RequireBalancedBeforeSave", "true"));
        LedgerAllowQuickPosting = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowQuickPosting", "true"));
        LedgerAutoBalanceCheck = ParseBool(await GetAppSettingAsync("GeneralLedger.AutoBalanceCheck", "true"));

        LedgerPeriodFromDate = await GetAppSettingAsync("GeneralLedger.PeriodFromDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"));
        LedgerPeriodToDate = await GetAppSettingAsync("GeneralLedger.PeriodToDate", new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)).ToString("dd/MM/yyyy"));
        LedgerPeriodStatus = await GetAppSettingAsync("GeneralLedger.PeriodStatus", "Đang mở");
        LedgerLastLockedDate = await GetAppSettingAsync("GeneralLedger.LastLockedDate", "Chưa khóa sổ");
        LedgerLockedBy = await GetAppSettingAsync("GeneralLedger.LockedBy", "-");

        LedgerDefaultGeneralLedgerReport = await GetAppSettingAsync("GeneralLedger.DefaultGeneralLedgerReport", "Sổ cái chi tiết tài khoản");
        LedgerDefaultTrialBalanceReport = await GetAppSettingAsync("GeneralLedger.DefaultTrialBalanceReport", "Bảng cân đối phát sinh");
        LedgerDefaultExportFormat = await GetAppSettingAsync("GeneralLedger.DefaultExportFormat", "Excel");
        LedgerAllowPrintReports = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowPrintReports", "true"));
        LedgerAllowExportExcel = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowExportExcel", "true"));
        LedgerAllowExportPdf = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowExportPdf", "true"));
        LedgerShowAccountantSignature = ParseBool(await GetAppSettingAsync("GeneralLedger.ShowAccountantSignature", "true"));
        LedgerShowDirectorSignature = ParseBool(await GetAppSettingAsync("GeneralLedger.ShowDirectorSignature", "true"));
        LedgerReportFooterNote = await GetAppSettingAsync("GeneralLedger.ReportFooterNote", "Báo cáo được lập từ dữ liệu sổ cái.");

        var app = InstalledApps.FirstOrDefault(x => string.Equals(x.AppKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
            ?? await _appModuleService.GetAppAsync("general-ledger");
        LedgerAppEnabled = app?.IsEnabled ?? true;
        LedgerShowOnDesktop = app?.ShowOnDesktop ?? true;

        var accounts = await _generalLedgerService.GetAccountsAsync();
        LedgerAccountOptions.Clear();
        foreach (var account in accounts.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.AccountCode))
        {
            LedgerAccountOptions.Add(account);
        }

        GeneralLedgerPostingMaps.Clear();
        AddPostingMap("Tiền mặt", "GeneralLedger.CashAccount", "111", await GetAppSettingAsync("GeneralLedger.CashAccount", "111"));
        AddPostingMap("Tiền gửi ngân hàng", "GeneralLedger.BankAccount", "112", await GetAppSettingAsync("GeneralLedger.BankAccount", "112"));
        AddPostingMap("Phải thu khách hàng", "GeneralLedger.ReceivableAccount", "131", await GetAppSettingAsync("GeneralLedger.ReceivableAccount", "131"));
        AddPostingMap("Hàng hóa", "GeneralLedger.InventoryAccount", "156", await GetAppSettingAsync("GeneralLedger.InventoryAccount", "156"));
        AddPostingMap("Phải trả nhà cung cấp", "GeneralLedger.PayableAccount", "331", await GetAppSettingAsync("GeneralLedger.PayableAccount", "331"));
        AddPostingMap("Doanh thu bán hàng", "GeneralLedger.RevenueAccount", "511", await GetAppSettingAsync("GeneralLedger.RevenueAccount", "511"));
        AddPostingMap("Giá vốn hàng bán", "GeneralLedger.CostOfGoodsSoldAccount", "632", await GetAppSettingAsync("GeneralLedger.CostOfGoodsSoldAccount", "632"));
        AddPostingMap("Điều chỉnh khác", "GeneralLedger.AdjustmentAccount", "338", await GetAppSettingAsync("GeneralLedger.AdjustmentAccount", "338"));
    }

    private void AddPostingMap(string businessName, string settingKey, string defaultAccountCode, string selectedAccountCode)
    {
        GeneralLedgerPostingMaps.Add(new GeneralLedgerPostingMapViewModel
        {
            BusinessName = businessName,
            SettingKey = settingKey,
            DefaultAccountCode = defaultAccountCode,
            SelectedAccountCode = selectedAccountCode,
            Status = "Đang dùng"
        });
    }

    private async Task SaveGeneralLedgerSettingsAsync(IEnumerable<(string Key, string? Value)> values)
    {
        var requests = values.Select(x => new SaveSettingValueRequest
        {
            Key = x.Key,
            Scope = SettingScope.App,
            ScopeTarget = "general-ledger",
            Value = x.Value,
            Reason = "Update general ledger settings"
        }).ToList();

        await _settingsService.SaveValuesAsync(requests);
    }

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var result) ? result : string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

    private static string BoolText(bool value) => value ? "true" : "false";

    partial void OnGeneralLedgerPanelKindChanged(string value) => RaiseGeneralLedgerPanelFlags();
    partial void OnIsGeneralLedgerPanelOpenChanged(bool value) => RaiseGeneralLedgerPanelFlags();
    partial void OnIsGeneralLedgerDetailFormVisibleChanged(bool value) => RaiseGeneralLedgerPanelFlags();

    private void RaiseGeneralLedgerPanelFlags()
    {
        OnPropertyChanged(nameof(IsLedgerCorePanel));
        OnPropertyChanged(nameof(IsLedgerPostingPanel));
        OnPropertyChanged(nameof(IsLedgerPeriodPanel));
        OnPropertyChanged(nameof(IsLedgerReportsPanel));
    }

    private async Task LoadLedgerAccountSettingsAsync()
    {
        EditableItems.Clear();
        LedgerAccounts.Clear();
        _scopeMap.Clear();
        _scopeTargetMap.Clear();

        var accounts = await _generalLedgerService.GetAccountsAsync();
        foreach (var account in accounts)
        {
            LedgerAccounts.Add(account);
        }

        LedgerAccountsPager.SetSource(LedgerAccounts);
        SelectedLedgerAccount = LedgerAccounts.FirstOrDefault();
        IsLedgerAccountSettingsVisible = true;
        IsGeneralLedgerChildBreadcrumbVisible = true;
        GeneralLedgerChildBreadcrumbTitle = "Cài đặt tài khoản";
        HasEditableForm = false;
        IsDirty = false;
    }

    private async Task LoadGeneralLedgerMainSettingsSummaryAsync()
    {
        async Task<string> GetAppSettingAsync(string key, string fallback)
        {
            var value = await _settingsService.GetValueAsync(key, SettingScope.App, "general-ledger");
            return string.IsNullOrWhiteSpace(value?.Value) ? fallback : value.Value.Trim();
        }

        var cash = await GetAppSettingAsync("GeneralLedger.CashAccount", "111");
        var bank = await GetAppSettingAsync("GeneralLedger.BankAccount", "112");
        var receivable = await GetAppSettingAsync("GeneralLedger.ReceivableAccount", "131");
        var inventory = await GetAppSettingAsync("GeneralLedger.InventoryAccount", "156");
        var payable = await GetAppSettingAsync("GeneralLedger.PayableAccount", "331");
        var revenue = await GetAppSettingAsync("GeneralLedger.RevenueAccount", "511");
        var cogs = await GetAppSettingAsync("GeneralLedger.CostOfGoodsSoldAccount", "632");
        var adjustment = await GetAppSettingAsync("GeneralLedger.AdjustmentAccount", "338");

        var accounts = await _generalLedgerService.GetAccountsAsync();
        var activeAccounts = accounts.Count(x => x.IsActive);
        _generalLedgerAccountsSummary = $"{accounts.Count:N0} tài khoản trong danh mục, {activeAccounts:N0} tài khoản đang sử dụng.";
        _generalLedgerAccountsValue = $"{accounts.Count:N0} tài khoản";

        var now = DateTime.Today;
        var monthFilter = new LedgerFilterDto
        {
            FromDate = new DateTime(now.Year, now.Month, 1),
            ToDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month))
        };
        var entries = await _generalLedgerService.GetLedgerEntriesAsync(monthFilter);

        _generalLedgerSettingsSummary = $"Nguồn SQL Server, kỳ {now:MM/yyyy}, {entries.Count:N0} bút toán trong kỳ, danh mục {accounts.Count:N0} tài khoản.";
        _generalLedgerSettingsValue = "Đang mở";
        _generalLedgerPostingSummary = $"Mặc định: tiền mặt {cash}, ngân hàng {bank}, phải thu {receivable}, tồn kho {inventory}, phải trả {payable}, doanh thu {revenue}, giá vốn {cogs}, điều chỉnh {adjustment}.";
        _generalLedgerPostingValue = "8 tài khoản";
        _generalLedgerPeriodSummary = $"Kỳ kế toán {now:MM/yyyy}, từ ngày 01/{now:MM/yyyy} đến ngày {DateTime.DaysInMonth(now.Year, now.Month):00}/{now:MM/yyyy}.";
        _generalLedgerPeriodValue = "Đang mở";
        _generalLedgerReportsSummary = $"Dữ liệu kỳ này có {entries.Count:N0} bút toán, dùng cho sổ cái, cân đối phát sinh và báo cáo tổng hợp.";
        _generalLedgerReportsValue = $"{entries.Count:N0} bút toán";
    }

    private SettingsDetailViewModel BuildLedgerAccountDetail()
    {
        return new SettingsDetailViewModel
        {
            Title = "Cài đặt tài khoản",
            Subtitle = "Quản lý danh mục tài khoản kế toán, thêm/xóa tài khoản và nhập danh mục từ file Excel.",
            Breadcrumb = "Cài đặt > Ứng dụng > Ứng dụng đã cài > Sổ cái > Cài đặt tài khoản"
        };
    }

    private async Task LoadAppSettingDefinitionsEditorAsync(string appKey)
    {
        IsLedgerAccountSettingsVisible = false;
        IsGeneralLedgerChildBreadcrumbVisible = false;
        GeneralLedgerChildBreadcrumbTitle = string.Empty;
        HasEditableForm = false;
        EditableItems.Clear();
        _scopeMap.Clear();
        _scopeTargetMap.Clear();

        var defs = await _appModuleService.GetAppSettingDefinitionsAsync(appKey);
        var values = await _appModuleService.GetAppSettingsAsync(appKey);
        foreach (var def in defs)
        {
            var val = values.FirstOrDefault(x => x.Key == def.Key)?.Value ?? def.DefaultValue;
            var vm = new EditableSettingItemViewModel(def, val);
            vm.PropertyChanged += (_, __) => IsDirty = EditableItems.Any(x => x.IsDirty) || SelectedInstalledApp is not null;
            EditableItems.Add(vm);
            _scopeMap[def.Key] = SettingScope.App;
            _scopeTargetMap[def.Key] = appKey;
        }

        HasEditableForm = EditableItems.Count > 0;
        IsDirty = false;
    }

    private void ApplyCategorySelection(SettingsCategoryItem value)
    {
        foreach (var c in Categories) c.IsSelected = false;
        value.IsSelected = true;
        IsDetailMode = false;
        CurrentDetail = null;
        ClearDetailEditorStates();

        if (_pages.TryGetValue(value.Key, out var page))
        {
            CurrentPage = page;
            Breadcrumb = $"Cài đặt > {page.Title}";
            PageTitle = page.Title;
            PageSubtitle = page.Subtitle;
            SetCategoryBreadcrumb(page);
        }

        ApplyRowsFilter();
        RaisePageFlags();
    }

    private async Task OpenInstalledAppsAsync()
    {
        ClearDetailEditorStates();
        InstalledApps.Clear();
        foreach (var app in await _appModuleService.GetAppsAsync())
        {
            InstalledApps.Add(app);
        }

        if (CurrentPage is null || !_pages.TryGetValue("apps", out var appsPage))
        {
            return;
        }

        CurrentPage = appsPage;
        SelectedCategory = Categories.FirstOrDefault(x => x.Key == "apps") ?? SelectedCategory;
        var row = CurrentPage.Rows.FirstOrDefault(x => string.Equals(x.TargetPageKey, "apps-installed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Id, "apps-installed", StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        CurrentDetail = BuildDetail(CurrentPage, row);
        IsDetailMode = true;
        RaisePageFlags();
    }

    private void ClearDetailEditorStates()
    {
        EditableItems.Clear();
        ModuleSettingSections.Clear();
        FilteredModuleSettingSections.Clear();
        ModuleValidationErrors.Clear();
        HasEditableForm = false;
        IsLedgerAccountSettingsVisible = false;
        IsGeneralLedgerSettingsVisible = false;
        IsGeneralLedgerDetailFormVisible = false;
        IsGeneralLedgerPanelOpen = false;
        IsGeneralLedgerChildBreadcrumbVisible = false;
        GeneralLedgerChildBreadcrumbTitle = string.Empty;
        IsModuleSettingsVisible = false;
        ModuleHasUnsavedChanges = false;
        IsAppearanceIconPickerVisible = false;
        ModuleSettingsSearchText = string.Empty;
        ModuleSettingsAppKey = string.Empty;
        ModuleSettingsTitle = string.Empty;
        ModuleSettingsDescription = string.Empty;
        OnPropertyChanged(nameof(HasModuleValidationErrors));
    }

    private void SetCategoryBreadcrumb(SettingsPageViewModel page)
    {
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Cài đặt", "root"));
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(page.Title, "category") { ShowSeparator = true });
        Breadcrumb = $"Cài đặt > {page.Title}";
    }

    private void SetBreadcrumbFromDetail(SettingsDetailViewModel detail)
    {
        BreadcrumbSegments.Clear();
        var parts = detail.Breadcrumb.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var title = parts[i];
            var target = ResolveBreadcrumbTarget(title, i == parts.Length - 1);
            BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(title, target) { ShowSeparator = i > 0 });
        }

        Breadcrumb = detail.Breadcrumb;
    }

    private string ResolveBreadcrumbTarget(string title, bool isLast)
    {
        if (string.Equals(title, "Cài đặt", StringComparison.OrdinalIgnoreCase))
        {
            return "root";
        }

        if (SelectedCategory is not null && string.Equals(title, SelectedCategory.Title, StringComparison.OrdinalIgnoreCase))
        {
            return "category";
        }

        if (string.Equals(title, "Ứng dụng", StringComparison.OrdinalIgnoreCase))
        {
            return "category";
        }

        if (string.Equals(title, "Ứng dụng đã cài", StringComparison.OrdinalIgnoreCase))
        {
            return "apps-installed";
        }

        if (string.Equals(title, "Sổ cái", StringComparison.OrdinalIgnoreCase))
        {
            return "general-ledger";
        }

        return "none";
    }

    private void RaisePageFlags()
    {
        OnPropertyChanged(nameof(IsHomePage));
        OnPropertyChanged(nameof(IsRowsPage));
        OnPropertyChanged(nameof(IsSystemPage));
        OnPropertyChanged(nameof(IsAppsPage));
    }
}

public sealed partial class GeneralLedgerPostingMapViewModel : ObservableObject
{
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string settingKey = string.Empty;
    [ObservableProperty] private string defaultAccountCode = string.Empty;
    [ObservableProperty] private string selectedAccountCode = string.Empty;
    [ObservableProperty] private string status = "Đang dùng";
}

public sealed class AppIconOptionViewModel
{
    public AppIconOptionViewModel(string iconPath)
    {
        IconPath = Path.GetFullPath(iconPath);
        Name = Path.GetFileNameWithoutExtension(iconPath);
        Folder = new DirectoryInfo(Path.GetDirectoryName(iconPath) ?? string.Empty).Name;
        DisplayName = $"{Name} ({Folder})";
    }

    public string IconPath { get; }
    public string Name { get; }
    public string Folder { get; }
    public string DisplayName { get; }
}


