using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Shared.Security;
using ASMILE.BO.Shared.Storage;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EditableSettingItemViewModel = ASMILE.BO.App.ViewModels.Settings.EditableSettingItemViewModel;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private static string ModuleIconRoot => ApplicationAssetPaths.IconRoot;

    // Keep the settings landing pages backed by the persisted settings engine.
    // Routes not listed here intentionally remain action/status pages rather
    // than pretending that a read-only placeholder is an editable form.
    private static readonly IReadOnlyDictionary<string, string[]> EditableSettingsByRoute =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["db-command-policy"] = ["Database.CommandTimeout"],
            ["system-folders"] = ["Storage.DataRootPath", "Storage.ExportRootPath", "Backup.BackupPath"],
            ["system-storage"] = ["Storage.DataRootPath", "VFC.EnableVirtualDrives"],
            ["network-api-server"] = ["Network.ApiBaseUrl", "Network.ApiPort", "Network.EnableHttps"],
            ["net-pos"] = ["POS.ApiEndpoint"],
            ["net-fo"] = ["FO.ApiEndpoint"],
            ["backup-schedule"] = ["Backup.EnableAutoBackup", "Backup.BackupPath", "Backup.Schedule", "Backup.RetentionCount", "Backup.CompressBackup", "Backup.VerifyAfterBackup"],
            ["vfc-virtual-drives"] = ["VFC.EnableVirtualDrives", "VFC.SystemDriveEnabled", "VFC.SystemDrivePath", "VFC.AccountingDriveEnabled", "VFC.AccountingDrivePath", "VFC.SalesDriveEnabled", "VFC.SalesDrivePath", "VFC.HrDriveEnabled", "VFC.HrDrivePath", "VFC.LogDriveEnabled", "VFC.LogDrivePath"],
            ["vfc-drives"] = ["VFC.EnableVirtualDrives", "VFC.SystemDriveEnabled", "VFC.AccountingDriveEnabled", "VFC.SalesDriveEnabled", "VFC.HrDriveEnabled", "VFC.LogDriveEnabled"],
            ["vfc-mapping"] = ["VFC.SystemDrivePath", "VFC.AccountingDrivePath", "VFC.SalesDrivePath", "VFC.HrDrivePath", "VFC.LogDrivePath"],
            ["vfc-root"] = ["Storage.DataRootPath"],
            ["logs-security"] = ["Logs.LogLevel", "Logs.RetentionDays", "Security.RequireAdminConfirmation"],
            ["sec-error"] = ["Logs.LogLevel", "Logs.RetentionDays"],
            ["sec-login"] = ["Security.SessionTimeoutMinutes", "Security.MaxFailedLoginAttempts"],
            ["users-session"] = ["Security.SessionTimeoutMinutes", "Security.MaxFailedLoginAttempts"],
            ["sec-sensitive"] = ["Security.RequireAdminConfirmation"],
            ["sec-confirm"] = ["Security.RequireAdminConfirmation"],
            ["users-policy"] = ["Security.MaxFailedLoginAttempts", "Security.SessionTimeoutMinutes", "Security.RequireAdminConfirmation"],
            ["users-approval"] = ["Security.RequireAdminConfirmation"],
            ["appearance-main"] = ["Appearance.Theme", "Appearance.WallpaperPath", "Appearance.ShowDesktopIcons", "Appearance.ShowTaskbar", "Appearance.SnapDesktopIconsToGrid"],
            ["ui-theme"] = ["Appearance.Theme"],
            ["ui-wallpaper"] = ["Appearance.WallpaperPath"],
            ["ui-icons"] = ["Appearance.ShowDesktopIcons"],
            ["ui-taskbar"] = ["Appearance.ShowTaskbar"],
            ["ui-snap"] = ["Appearance.SnapDesktopIconsToGrid"],
            ["clients-pos"] = ["POS.ApiEndpoint"],
            ["clients-fo"] = ["FO.ApiEndpoint"],
            ["maint-channel"] = ["Maintenance.UpdateChannel"]
        };

    private static readonly IReadOnlyDictionary<string, string> HotelConfigurationRoutes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["hotel-room-types"] = "hotel-room-types",
            ["hotel-rooms"] = "hotel-room-configuration-master",
            ["hotel-buildings"] = "hotel-building-master",
            ["hotel-room-floors"] = "hotel-floor-master",
            ["hotel-areas"] = "hotel-area-master",
            ["hotel-property-profile"] = "hotel-property-profile",
            ["hotel-property-documents"] = "hotel-property-documents",
            ["hotel-stay-policies"] = "hotel-stay-policies",
            ["hotel-check-in-out-rules"] = "hotel-check-in-out-rules",
            ["hotel-room-move-upgrade"] = "hotel-room-move-upgrade",
            ["hotel-room-status-rules"] = "hotel-room-status-rules",
            ["hotel-housekeeping-supplies"] = "hotel-housekeeping-supply-catalog",
            ["hotel-minibar-items"] = "hotel-minibar-items",
            ["hotel-rate-engine"] = "hotel-rate-engine",
            ["hotel-rate-calendar"] = "hotel-rate-calendar",
            ["hotel-promotions-packages"] = "hotel-promotions-packages",
            ["hotel-guest-types"] = "hotel-guest-types",
            ["hotel-profile-types"] = "hotel-profile-types",
            ["hotel-vip-levels"] = "hotel-vip-levels",
            ["hotel-guest-preferences"] = "hotel-guest-preferences",
            ["hotel-reservation-types"] = "hotel-reservation-types",
            ["hotel-booking-sources"] = "hotel-booking-sources",
            ["hotel-booking-channels"] = "hotel-booking-channels",
            ["hotel-market-segments"] = "hotel-market-segments",
            ["hotel-folio-types"] = "hotel-folio-types",
            ["hotel-charge-codes"] = "hotel-charge-codes",
            ["hotel-payment-methods"] = "hotel-payment-methods",
            ["hotel-deposit-policies"] = "hotel-deposit-policies",
            ["hotel-discount-policies"] = "hotel-discount-policies",
            ["hotel-tax-service-charge"] = "hotel-tax-service-charge",
            ["hotel-currency-exchange"] = "hotel-currency-exchange",
            ["hotel-housekeeping-rules"] = "hotel-housekeeping-rules",
            ["hotel-wakeup-rules"] = "hotel-wakeup-rules",
            ["hotel-guest-request-catalog"] = "hotel-guest-request-catalog",
            ["hotel-complaint-catalog"] = "hotel-complaint-catalog",
            ["hotel-companies"] = "hotel-companies",
            ["hotel-travel-agents"] = "hotel-travel-agents",
            ["hotel-commissions"] = "hotel-commissions",
            ["hotel-groups"] = "hotel-groups",
            ["hotel-cashier-shift"] = "hotel-cashier-shift",
            ["hotel-night-audit"] = "hotel-night-audit",
            ["hotel-approval-matrix"] = "hotel-approval-matrix",
            ["hotel-fo-permissions"] = "hotel-fo-permissions",
            ["hotel-forms-printers"] = "hotel-forms-printers",
            // The generic Printing page is an entry point into the same
            // persisted hotel form/printer profile workspace.  Do not keep a
            // second placeholder provider for these aliases.
            ["print-voucher"] = "hotel-forms-printers",
            ["print-invoice"] = "hotel-forms-printers",
            ["print-debt"] = "hotel-forms-printers",
            ["print-hr"] = "hotel-forms-printers",
            ["hotel-messaging-integrations"] = "hotel-messaging-integrations",
            ["hotel-audit-correction"] = "hotel-audit-correction",
            ["hotel-business-rules"] = "hotel-business-rules"
        };

    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IDatabaseAdminService _databaseAdminService;
    private readonly IBackupService _backupService;
    private readonly IFileBackupService _fileBackupService;
    private readonly IArchiveService _archiveService;
    private readonly IDataIntegrityService _dataIntegrityService;
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
    private readonly ISettingChangeLogRepository _settingChangeLogRepository;
    private readonly IAppModuleService _appModuleService;
    private readonly IGeneralLedgerService _generalLedgerService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly ISettingsValidator _settingsValidator;
    private readonly ISettingsChangeNotifier _settingsChangeNotifier;
    private readonly IModuleSettingsService _moduleSettingsService;
    private readonly ISettingsValidationService _moduleSettingsValidationService;
    private readonly IToastNotificationService _toastNotificationService;
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, SettingsPageViewModel> _pages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SettingScope> _scopeMap = new();
    private readonly Dictionary<string, string?> _scopeTargetMap = new();
    private readonly Dictionary<Guid, IReadOnlyList<string>> _rolePermissionCodes = [];
    private UpdatePackageStageResultDto? _lastStagedUpdate;
    private bool _isLoadingAppAdvanced;
    private bool _isNavigatingDetail;
    private bool _isBackupOperationRunning;
    private int _editableDetailLoadVersion;
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
    private string _virtualDriveStatus = "Chưa kiểm tra";
    private string _backupScheduleStatus = "Chưa kiểm tra";
    private string _apiRuntimeStatus = "Chưa kiểm tra";
    private string _storageDataRootPath = string.Empty;
    private bool _storageDataRootPathIsPersisted;

    public ObservableCollection<SettingsCategoryItem> Categories { get; } = [];
    public ObservableCollection<SettingsCategoryItem> FilteredCategories { get; } = [];
    public ObservableCollection<SettingItemViewModel> VisibleRows { get; } = [];

    public ObservableCollection<BackupRecordDto> BackupRecords { get; } = [];
    public ObservableCollection<FileBackupRecordDto> FileBackupRecords { get; } = [];
    public ObservableCollection<ServiceStatusDto> ServiceStatuses { get; } = [];
    public ObservableCollection<string> RecentActivities { get; } = [];
    public ObservableCollection<UserAccountDto> SecurityUsers { get; } = [];
    public ObservableCollection<RoleDto> SecurityRoles { get; } = [];
    public ObservableCollection<PermissionDto> SecurityPermissions { get; } = [];
    public ObservableCollection<LoginAuditDto> SecurityLoginAudits { get; } = [];
    public ObservableCollection<PrinterDeviceDto> AvailablePrinters { get; } = [];
    public ObservableCollection<PrinterPermissionDto> PrinterPermissions { get; } = [];
    public ObservableCollection<ClientRegistryEntryDto> ClientRegistryEntries { get; } = [];
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
    [ObservableProperty] private object? embeddedModuleContent;
    [ObservableProperty] private string embeddedModuleTitle = string.Empty;
    [ObservableProperty] private bool isEmbeddedModuleMode;
    [ObservableProperty] private bool isDetailMode;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string breadcrumb = "Cài đặt > Home";
    [ObservableProperty] private string pageTitle = "Home";
    [ObservableProperty] private string pageSubtitle = "Trung tâm cấu hình hệ thống ASMILE BO.";

    [ObservableProperty] private string machineName = Environment.MachineName;
    [ObservableProperty] private string localIp = "Chưa kiểm tra";
    [ObservableProperty] private string currentUser = "Chưa xác định";
    [ObservableProperty] private string timezone = TimeZoneInfo.Local.DisplayName;
    [ObservableProperty] private string sqlServerName = "Chưa kiểm tra";
    [ObservableProperty] private string databaseName = "Chưa kiểm tra";
    [ObservableProperty] private string connectionStatus = "Chưa kiểm tra";
    [ObservableProperty] private string databaseConfigServer = string.Empty;
    [ObservableProperty] private string databaseConfigName = string.Empty;
    [ObservableProperty] private string databaseConfigAuthenticationMode = "Windows";
    [ObservableProperty] private string databaseConfigUserName = string.Empty;
    [ObservableProperty] private string databaseConfigPassword = string.Empty;
    [ObservableProperty] private string databaseConfigMessage = string.Empty;
    [ObservableProperty] private bool isDatabaseConnectionEditorVisible;
    [ObservableProperty] private bool isDatabaseConnectionManagedExternally;
    [ObservableProperty] private string systemHealthDatabase = "SQL Server: Chưa kiểm tra";
    [ObservableProperty] private string systemHealthApi = "API endpoint probe: Chưa kiểm tra";
    [ObservableProperty] private string systemHealthBackup = "SQL backup file: Chưa có";
    [ObservableProperty] private string systemHealthFileBackup = "File backup đã kiểm tra: Chưa có";
    [ObservableProperty] private string systemHealthAudit = "Audit log: Chưa có dữ liệu";
    [ObservableProperty] private string systemHealthMigrations = "Migrations: Chưa kiểm tra";
    [ObservableProperty] private string systemHealthEInvoice = "E-invoice: Chưa kiểm tra";
    [ObservableProperty] private bool hasRecentActivities;

    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool hasEditableForm;
    [ObservableProperty] private bool showDetailCardsWithEditableForm;
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
    public bool IsSqlAuthenticationSelected => string.Equals(DatabaseConfigAuthenticationMode, "Sql", StringComparison.OrdinalIgnoreCase);

    partial void OnDatabaseConfigAuthenticationModeChanged(string value)
        => OnPropertyChanged(nameof(IsSqlAuthenticationSelected));
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
        IFileBackupService fileBackupService,
        IArchiveService archiveService,
        IDataIntegrityService dataIntegrityService,
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
        ISettingChangeLogRepository settingChangeLogRepository,
        IAppModuleService appModuleService,
        IGeneralLedgerService generalLedgerService,
        IAccountingPeriodService accountingPeriodService,
        ISettingsValidator settingsValidator,
        ISettingsChangeNotifier settingsChangeNotifier,
        IModuleSettingsService moduleSettingsService,
        ISettingsValidationService moduleSettingsValidationService,
        IToastNotificationService toastNotificationService,
        IServiceProvider serviceProvider)
    {
        _systemSettingsService = systemSettingsService;
        _databaseAdminService = databaseAdminService;
        _backupService = backupService;
        _fileBackupService = fileBackupService;
        _archiveService = archiveService;
        _dataIntegrityService = dataIntegrityService;
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
        _settingChangeLogRepository = settingChangeLogRepository;
        _appModuleService = appModuleService;
        _generalLedgerService = generalLedgerService;
        _accountingPeriodService = accountingPeriodService;
        _settingsValidator = settingsValidator;
        _settingsChangeNotifier = settingsChangeNotifier;
        _moduleSettingsService = moduleSettingsService;
        _moduleSettingsValidationService = moduleSettingsValidationService;
        _toastNotificationService = toastNotificationService;
        _serviceProvider = serviceProvider;
        _settingsChangeNotifier.SettingChanged += OnSettingsChanged;

        BuildCategories();
        BuildPages();
        BuildModuleIconOptions();
        var initialCategory = Categories.FirstOrDefault();
        if (initialCategory is not null)
        {
            SelectedCategory = initialCategory;

            // The generated property setter skips the change callback when the
            // selection was already assigned by a view/restore path. Keep the
            // initial page invariant explicit so Home never renders the
            // non-home fallback while CurrentPage is still null.
            if (CurrentPage is null)
            {
                ApplyCategorySelection(initialCategory);
            }
        }
        _ = LoadAsync();
    }

    /// <summary>
    /// Selects a settings area for callers that enter the central settings
    /// workspace from another module (for example Front Office setup).
    /// </summary>
    public bool OpenCategory(string key)
    {
        var item = Categories.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            return false;
        }

        SelectCategoryForNavigation(item);
        return true;
    }

    private void SelectCategoryForNavigation(SettingsCategoryItem item)
    {
        if (ReferenceEquals(SelectedCategory, item))
        {
            ApplyCategorySelection(item);
            return;
        }

        SelectedCategory = item;
    }

    [RelayCommand]
    private Task OpenWorkstationSettings() => NavigateToCategoryDetailAsync("clients", "clients-reg");

    [RelayCommand]
    private Task OpenMachineInfo() => NavigateToCategoryDetailAsync("system", "system-server");

    [RelayCommand]
    private Task OpenInfrastructureHealth() => NavigateToCategoryDetailAsync("database", "db-info");

    [RelayCommand]
    private Task OpenBackupConfiguration() => NavigateToCategoryDetailAsync("backup", "backup-schedule");

    [RelayCommand]
    private Task OpenServiceStatus() => NavigateToCategoryDetailAsync("clients", "clients-health");

    private async Task NavigateToCategoryDetailAsync(string categoryKey, string rowId)
    {
        if (!OpenCategory(categoryKey) || CurrentPage is null)
        {
            _notificationService.Show("Không tìm thấy mục cài đặt được yêu cầu.");
            return;
        }

        var row = CurrentPage.Rows.FirstOrDefault(x => string.Equals(x.Id, rowId, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            _notificationService.Show("Mục cài đặt chưa được cấu hình.");
            return;
        }

        await NavigateToSettingsDetail(row);
    }

    [RelayCommand]
    public async Task NavigateToSettingsDetail(SettingItemViewModel? row)
    {
        if (_isNavigatingDetail)
        {
            return;
        }

        var route = row is null ? string.Empty : ResolveSettingsRowRoute(row);
        try
        {
            _isNavigatingDetail = true;
            if (row is null || CurrentPage is null) return;
            IsDatabaseConnectionEditorVisible = false;

            const string readOnlyInfoPrefix = "settings-info:";
            if (route.StartsWith(readOnlyInfoPrefix, StringComparison.OrdinalIgnoreCase))
            {
                ClearDetailEditorStates();
                var infoDetail = new SettingsDetailViewModel
                {
                    Title = row.Title,
                    Subtitle = row.Description,
                    Breadcrumb = $"Cài đặt > {CurrentPage.Title} > {row.Title}"
                };
                var infoCard = new SettingCardViewModel(
                    "Thông tin từ hệ thống",
                    "Giá trị này được cung cấp bởi runtime hoặc provider hiện tại; không có cấu hình ghi tương ứng trong ứng dụng.");
                var infoItem = new SettingItemViewModel(
                    "settings-info-value",
                    "\uE8A4",
                    row.Title,
                    row.Description,
                    row.Value,
                    row.Badge);
                infoItem.IsClickable = false;
                infoItem.IsReadOnly = true;
                infoCard.Items.Add(infoItem);
                infoDetail.Cards.Add(infoCard);
                CurrentDetail = infoDetail;
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            const string categoryRoutePrefix = "settings-category:";
            if (route.StartsWith(categoryRoutePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var target = route[categoryRoutePrefix.Length..].Split(':', 2, StringSplitOptions.TrimEntries);
                if (target.Length != 2 || !OpenCategory(target[0]) || CurrentPage is null)
                {
                    _notificationService.Show("Không tìm thấy nhóm cài đặt được yêu cầu.");
                    return;
                }

                row = CurrentPage.Rows.FirstOrDefault(x => string.Equals(x.Id, target[1], StringComparison.OrdinalIgnoreCase));
                if (row is null)
                {
                    _notificationService.Show("Mục cài đặt đích chưa được cấu hình.");
                    return;
                }

                route = ResolveSettingsRowRoute(row);
            }

            if (string.Equals(CurrentPage.Key, "printing", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "print-export", StringComparison.OrdinalIgnoreCase))
            {
                await OpenExportPipelineSettingsAsync();
                return;
            }

            const string revokeClientSessionPrefix = "client-session-revoke-";
            if (route.StartsWith(revokeClientSessionPrefix, StringComparison.OrdinalIgnoreCase)
                && Guid.TryParseExact(route[revokeClientSessionPrefix.Length..], "N", out var sessionId))
            {
                await RevokeClientSessionAsync(sessionId);
                return;
            }

            const string networkEndpointRefreshPrefix = "network-endpoint-refresh:";
            if (route.StartsWith(networkEndpointRefreshPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var endpointRoute = route[networkEndpointRefreshPrefix.Length..];
                var endpointRow = CurrentPage.Rows.FirstOrDefault(x =>
                    string.Equals(x.TargetPageKey, endpointRoute, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Id, endpointRoute, StringComparison.OrdinalIgnoreCase));
                if (endpointRow is null)
                {
                    _notificationService.Show("Không tìm thấy endpoint cần kiểm tra lại.");
                    return;
                }

                CurrentDetail = await BuildNetworkEndpointDetailAsync(endpointRoute, endpointRow);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            // Security rows share a landing target for navigation, but their
            // editable scope is intentionally per-row. Keep the selected row
            // as the route so Audit/Login/Sensitive actions do not open one
            // oversized editor with unrelated settings.
            if (string.Equals(CurrentPage.Key, "security", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "logs-security", StringComparison.OrdinalIgnoreCase)
                && row.Id.StartsWith("sec-", StringComparison.OrdinalIgnoreCase))
            {
                route = row.Id;
            }

            if (string.Equals(route, "apps-default-open-windows", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "apps-startup-open-windows", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "windows-settings-about", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "windows-settings-date-time", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "windows-settings-firewall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "windows-settings-printers", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "windows-settings-services", StringComparison.OrdinalIgnoreCase))
            {
                var isStartupApps = string.Equals(route, "apps-startup-open-windows", StringComparison.OrdinalIgnoreCase);
                var isAbout = string.Equals(route, "windows-settings-about", StringComparison.OrdinalIgnoreCase);
                var isDateTime = string.Equals(route, "windows-settings-date-time", StringComparison.OrdinalIgnoreCase);
                var isFirewall = string.Equals(route, "windows-settings-firewall", StringComparison.OrdinalIgnoreCase);
                var isPrinters = string.Equals(route, "windows-settings-printers", StringComparison.OrdinalIgnoreCase);
                var isServices = string.Equals(route, "windows-settings-services", StringComparison.OrdinalIgnoreCase);
                try
                {
                    var settingsLauncher = _serviceProvider.GetService<IWindowsSettingsLauncher>();
                    if (settingsLauncher is null)
                    {
                        _notificationService.Show("Windows Settings launcher chưa được đăng ký.");
                    }
                    else
                    {
                        if (isAbout) settingsLauncher.OpenAboutPage();
                        else if (isDateTime) settingsLauncher.OpenDateTimePage();
                        else if (isFirewall) settingsLauncher.OpenFirewallPage();
                        else if (isPrinters) settingsLauncher.OpenPrintersPage();
                        else if (isServices) settingsLauncher.OpenServicesConsole();
                        else if (isStartupApps) settingsLauncher.OpenStartupAppsPage();
                        else settingsLauncher.OpenDefaultAppsPage();
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
                {
                    var pageName = isAbout ? "About" : isDateTime ? "Date & time" : isFirewall ? "Windows Firewall" : isPrinters ? "Printers & scanners" : isServices ? "Services" : isStartupApps ? "Startup apps" : "Default apps";
                    Log.Error(ex, "Could not open Windows {SettingsPage} settings.", pageName);
                    var settingsPath = isAbout
                        ? "System > About"
                        : isDateTime
                            ? "Time & language > Date & time"
                            : isFirewall
                                    ? "Privacy & security > Windows Security > Firewall & network protection"
                                    : isServices
                                        ? "Windows Tools > Services"
                                    : isPrinters
                                    ? "Bluetooth & devices > Printers & scanners"
                                    : $"Apps > {pageName}";
                    _notificationService.Show($"Không mở được {pageName} của Windows. Hãy mở Settings > {settingsPath} thủ công.");
                }

                return;
            }

            if (string.Equals(route, "hotel-room-configuration", StringComparison.OrdinalIgnoreCase))
            {
                CurrentDetail = BuildHotelRoomConfigurationDetail();
                IsEmbeddedModuleMode = false;
                IsDetailMode = true;
                SetBreadcrumbFromDetail(CurrentDetail);
                RaisePageFlags();
                return;
            }

            if (HotelConfigurationRoutes.TryGetValue(route, out var hotelRoute))
            {
                await OpenHotelConfigurationInWorkspaceAsync(hotelRoute);
                return;
            }

            if (route.StartsWith("general-ledger-toggle-", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyGeneralLedgerToggleAsync(route);
                return;
            }

            const string shellStartupTogglePrefix = "apps-shell-startup-toggle:";
            if (route.StartsWith(shellStartupTogglePrefix, StringComparison.OrdinalIgnoreCase))
            {
                await ToggleShellStartupModuleAsync(route[shellStartupTogglePrefix.Length..]);
                return;
            }

            const string appModuleSettingsPrefix = "app-module-settings:";
            if (route.StartsWith(appModuleSettingsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var appKey = route[appModuleSettingsPrefix.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(appKey))
                {
                    await OpenModuleSettingsAsync(appKey);
                    return;
                }
            }

            if (route.StartsWith("app-module-toggle-", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyAppModuleToggleAsync(route);
                return;
            }

            if (string.Equals(route, "system-mode-maintenance-toggle", StringComparison.OrdinalIgnoreCase))
            {
                await ToggleSystemMaintenanceModeAsync();
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

            if (string.Equals(CurrentPage.Key, "database", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "db-command-policy", StringComparison.OrdinalIgnoreCase))
            {
                await LoadEditableDetailAsync(route);
                CurrentDetail = BuildDetail(CurrentPage, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "database", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(route, "db-info", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-schema", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-objects", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-maint", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-integrity", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-security", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-maint-run", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "db-integrity-checkpoint", StringComparison.OrdinalIgnoreCase)))
            {
                CurrentDetail = await BuildDatabaseDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(route, "database-sql-connection", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(CurrentPage.Key, "database", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(route, "db-connection", StringComparison.OrdinalIgnoreCase)))
            {
                ClearDetailEditorStates();
                CurrentDetail = await BuildDatabaseConnectionDetailAsync(row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "security", StringComparison.OrdinalIgnoreCase)
                && (route.StartsWith("sec-", StringComparison.OrdinalIgnoreCase)))
            {
                await LoadEditableDetailAsync(route);
                CurrentDetail = await BuildSecurityOperationalDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "vfc", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(route, "vfc-index", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "vfc-retention", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "vfc-rebuild", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "vfc-rebuild-run", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "vfc-repair", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "vfc-repair-queue", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.Equals(route, "vfc-retention", StringComparison.OrdinalIgnoreCase))
                {
                    await OpenVfcRetentionPolicyAsync();
                    return;
                }

                CurrentDetail = await BuildVfcMaintenanceDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "system", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(route, "system-mode", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "system-server", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "system-remote", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "system-performance", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.Equals(route, "system-mode", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentDetail = await BuildSystemModeDetailAsync(row);
                    IsDetailMode = true;
                    RaisePageFlags();
                    return;
                }

                CurrentDetail = string.Equals(route, "system-remote", StringComparison.OrdinalIgnoreCase)
                    ? await BuildSystemRemoteDetailAsync(row)
                    : await BuildSystemOperationalDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if ((string.Equals(CurrentPage.Key, "network", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(route, "net-printer", StringComparison.OrdinalIgnoreCase))
                || (string.Equals(CurrentPage.Key, "printing", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(route, "print-default", StringComparison.OrdinalIgnoreCase)))
            {
                var printerDetail = new SettingsDetailViewModel
                {
                    Title = row.Title,
                    Subtitle = row.Description,
                    Breadcrumb = $"Cài đặt > {(string.Equals(CurrentPage.Key, "printing", StringComparison.OrdinalIgnoreCase) ? "In ấn & mẫu biểu" : "Kết nối mạng")} > {row.Title}"
                };
                CurrentDetail = BuildPrinterDetail(printerDetail, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(route, "network-api-server", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(CurrentPage.Key, "network", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(route, "net-pos", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(route, "net-fo", StringComparison.OrdinalIgnoreCase))))
            {
                ClearDetailEditorStates();
                await LoadEditableDetailAsync(route);
                CurrentDetail = await BuildNetworkEndpointDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "network", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(route, "net-shared", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "net-discovery", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "net-fw", StringComparison.OrdinalIgnoreCase)))
            {
                CurrentDetail = await BuildNetworkOperationalDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "clients", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "clients-health", StringComparison.OrdinalIgnoreCase))
            {
                CurrentDetail = await BuildClientHealthDetailAsync(row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "apps", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(route, "apps-default", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-shell-startup", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-startup", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-startup-windows", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-permissions", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-paths", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "apps-advanced", StringComparison.OrdinalIgnoreCase)))
            {
                CurrentDetail = await BuildAppsInfrastructureDetailAsync(row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "maintenance", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "maint-channel", StringComparison.OrdinalIgnoreCase))
            {
                await LoadEditableDetailAsync(route);
                CurrentDetail = BuildDetail(CurrentPage, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "maintenance", StringComparison.OrdinalIgnoreCase)
                && IsMaintenanceDetailRoute(route))
            {
                if (string.Equals(route, "maint-update-stage", StringComparison.OrdinalIgnoreCase))
                {
                    await StageVerifiedUpdateAsync();
                    return;
                }

                CurrentDetail = await BuildMaintenanceDetailAsync(route, row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "about", StringComparison.OrdinalIgnoreCase))
            {
                CurrentDetail = await BuildAboutDetailAsync(row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }

            if (string.Equals(CurrentPage.Key, "backup", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(route, "backup-schedule", StringComparison.OrdinalIgnoreCase))
                {
                    await LoadEditableDetailAsync(route);
                    CurrentDetail = BuildDetail(CurrentPage, row);
                    IsDetailMode = true;
                    RaisePageFlags();
                    return;
                }

                if (string.Equals(route, "backup-now", StringComparison.OrdinalIgnoreCase))
                {
                    await BackupNowAsync();
                    await OpenBackupDetailAsync();
                    return;
                }

                if (string.Equals(route, "backup-files-now", StringComparison.OrdinalIgnoreCase))
                {
                    await BackupFilesNowAsync();
                    await OpenBackupDetailAsync();
                    return;
                }

                if (string.Equals(route, "backup-history", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "backup-db", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "backup-files", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "backup-policy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(route, "backup-verify", StringComparison.OrdinalIgnoreCase))
                {
                    await OpenBackupDetailAsync();
                    return;
                }

                if (route.StartsWith("backup-verify:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(route["backup-verify:".Length..], out var backupId))
                {
                    await OpenBackupDetailAsync(await VerifyBackupAsync(backupId));
                    return;
                }

                if (route.StartsWith("backup-restore:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(route["backup-restore:".Length..], out var restoreBackupId))
                {
                    await RestoreBackupAsync(restoreBackupId);
                    return;
                }

                if (route.StartsWith("backup-file-verify:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(route["backup-file-verify:".Length..], out var fileBackupId))
                {
                    await VerifyFileBackupAsync(fileBackupId);
                    await OpenBackupDetailAsync();
                    return;
                }

                if (route.StartsWith("backup-file-restore:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(route["backup-file-restore:".Length..], out var restoreFileBackupId))
                {
                    await RestoreFileBackupAsync(restoreFileBackupId);
                    return;
                }
            }

            if (route.StartsWith("app-advanced:", StringComparison.OrdinalIgnoreCase))
            {
                var appKey = route["app-advanced:".Length..];
                if (!string.IsNullOrWhiteSpace(appKey))
                {
                    if (!string.Equals(appKey, "general-ledger", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prefer the persisted Settings Engine editor whenever
                        // this app owns writable app-scoped definitions. This
                        // is the real runtime path for inventory thresholds and
                        // POS/FO endpoints; the module catalog can still own
                        // additional operational settings for apps that do not
                        // expose writable Settings Engine values.
                        var appDefinitions = await _appModuleService.GetAppSettingDefinitionsAsync(appKey);
                        if (HasWritableAppSettingDefinitions(appDefinitions))
                        {
                            await LoadAppAdvancedEditorAsync(appKey);
                            var configuredApp = InstalledApps.FirstOrDefault(x =>
                                string.Equals(x.AppKey, appKey, StringComparison.OrdinalIgnoreCase));
                            CurrentDetail = BuildAppAdvancedDetail(
                                CurrentPage,
                                appKey,
                                configuredApp?.Name ?? appKey,
                                configuredApp?.Description ?? "Cấu hình ứng dụng");
                            ShowDetailCardsWithEditableForm = true;
                            IsDetailMode = true;
                            SetBreadcrumbFromDetail(CurrentDetail);
                            RaisePageFlags();
                            return;
                        }

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
                await UnlockSelectedUserAsync();
                return;
            }
            if (string.Equals(CurrentPage.Key, "users", StringComparison.OrdinalIgnoreCase)
                && string.Equals(route, "users-audit", StringComparison.OrdinalIgnoreCase))
            {
                CurrentDetail = await BuildSecurityOperationalDetailAsync("sec-audit", row);
                IsDetailMode = true;
                RaisePageFlags();
                return;
            }
            const string editUserPrefix = "users-edit:";
            if (route.StartsWith(editUserPrefix, StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(route[editUserPrefix.Length..], out var userId))
            {
                await EditUserAsync(userId);
                return;
            }
            if (string.Equals(route, "users-role-manager", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route, "users-permission-matrix", StringComparison.OrdinalIgnoreCase))
            {
                OpenSecurityAdminWorkspace(
                    string.Equals(route, "users-permission-matrix", StringComparison.OrdinalIgnoreCase)
                        ? "Ma trận phân quyền"
                        : "Quản lý vai trò & quyền");
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

    private static string ResolveSettingsRowRoute(SettingItemViewModel row)
        => string.IsNullOrWhiteSpace(row.TargetPageKey) ? row.Id : row.TargetPageKey;

    private async Task OpenBackupDetailAsync((string FilePath, bool? IsValid)? verificationResult = null)
    {
        BackupRecords.Clear();
        foreach (var backup in await _backupService.GetRecentAsync())
        {
            BackupRecords.Add(backup);
        }

        if (verificationResult is { } result)
        {
            var verifiedBackup = BackupRecords.FirstOrDefault(x => string.Equals(x.FilePath, result.FilePath, StringComparison.OrdinalIgnoreCase));
            if (verifiedBackup is not null)
            {
                verifiedBackup.Status = result.IsValid switch
                {
                    true => "Đã xác thực",
                    false => "Không hợp lệ",
                    _ => "Không xác thực được"
                };
            }
        }

        FileBackupRecords.Clear();
        foreach (var backup in await _fileBackupService.GetRecentAsync())
        {
            FileBackupRecords.Add(backup);
        }

        var detail = BuildBackupDetail();
        CurrentDetail = detail;
        IsDetailMode = true;
        SetBreadcrumbFromDetail(detail);
        RaisePageFlags();
    }

    private async Task VerifyFileBackupAsync(int backupId)
    {
        var backup = FileBackupRecords.FirstOrDefault(x => x.Id == backupId);
        if (backup is null)
        {
            _notificationService.Show("Không tìm thấy file backup cần kiểm tra.");
            return;
        }

        try
        {
            var valid = await _fileBackupService.ValidateAsync(backup.FilePath);
            await LogBackupAuditSafelyAsync(
                valid ? SecurityEventType.BackupVerificationPassed : SecurityEventType.BackupVerificationFailed,
                "FileBackupVerification",
                backup.FileName,
                valid ? "File backup vượt qua kiểm tra manifest và SHA-256." : "File backup không vượt qua kiểm tra manifest hoặc SHA-256.");
            _notificationService.Show(valid
                ? $"File backup hợp lệ: {backup.FileName}"
                : $"File backup không hợp lệ: {backup.FileName}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "File backup verification failed. BackupId={BackupId}", backupId);
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupVerificationFailed,
                "FileBackupVerification",
                backup.FileName,
                "Xác thực file backup thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không xác thực được file backup. Vui lòng kiểm tra manifest và thư mục dữ liệu.");
        }
    }

    private async Task RestoreFileBackupAsync(int backupId)
    {
        if (_isBackupOperationRunning)
        {
            return;
        }

        if (!_authorizationService.HasPermission(Permission.RestoreDatabase))
        {
            await _securityAuditService.LogAsync(
                SecurityEventType.PermissionDenied,
                "Settings",
                "FileRestore",
                backupId.ToString(CultureInfo.InvariantCulture),
                "Restore file bị từ chối do thiếu quyền Database.Restore.");
            _notificationService.Show("Bạn không có quyền khôi phục file trong cấu hình hiện tại.");
            return;
        }

        var backup = FileBackupRecords.FirstOrDefault(x => x.Id == backupId);
        if (backup is null)
        {
            _notificationService.Show("Không tìm thấy file backup cần khôi phục.");
            return;
        }

        try
        {
            _isBackupOperationRunning = true;
            if (!await _fileBackupService.ValidateAsync(backup.FilePath))
            {
                await _securityAuditService.LogAsync(
                    SecurityEventType.RestoreFailed,
                    "Settings",
                    "FileRestore",
                    backup.FileName,
                    "Restore file bị dừng vì manifest hoặc hash không hợp lệ.");
                _notificationService.Show($"File backup không hợp lệ, không thể khôi phục: {backup.FileName}");
                return;
            }

            var requireConfirmation = await _securitySettingsService.GetRequireAdminConfirmationAsync();
            var confirmed = await SettingsConfirmationPolicy.ConfirmIfRequiredAsync(
                requireConfirmation,
                () => _authorizationService.ConfirmDangerousActionAsync(
                    "Xác nhận khôi phục file",
                    $"Thao tác này sẽ ghi đè các file trùng tên trong {backup.FileName}. File hiện có không bị xóa. Tiếp tục?"));
            if (!confirmed)
            {
                await _securityAuditService.LogAsync(
                    SecurityEventType.RestoreRequested,
                    "Settings",
                    "FileRestore",
                    backup.FileName,
                    "Người vận hành đã hủy xác nhận restore file.");
                return;
            }

            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreRequested,
                "Settings",
                "FileRestore",
                backup.FileName,
                requireConfirmation
                    ? "Đã xác nhận yêu cầu restore file."
                    : "Thực hiện theo chính sách không yêu cầu xác nhận bổ sung; quyền Database.Restore vẫn được kiểm tra.");

            var result = await _fileBackupService.RestoreAsync(backup.FilePath, overwriteExisting: true);
            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreCompleted,
                "Settings",
                "FileRestore",
                backup.FileName,
                $"Restore file hoàn tất: {result.RestoredFiles} file phục hồi, {result.SkippedFiles} file bỏ qua.");
            _notificationService.Show($"Đã khôi phục {result.RestoredFiles} file; bỏ qua {result.SkippedFiles} file.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "File restore failed from settings. BackupId={BackupId}", backupId);
            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreFailed,
                "Settings",
                "FileRestore",
                backup.FileName,
                "Restore file thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không khôi phục được file. Vui lòng kiểm tra audit log và cấu hình thư mục.");
        }
        finally
        {
            _isBackupOperationRunning = false;
        }
    }

    private async Task<(string FilePath, bool? IsValid)?> VerifyBackupAsync(int backupId)
    {
        var backup = BackupRecords.FirstOrDefault(x => x.Id == backupId);
        if (backup is null)
        {
            _notificationService.Show("Không tìm thấy bản backup cần kiểm tra.");
            return null;
        }

        try
        {
            var valid = await _backupService.ValidateBackupAsync(backup.FilePath);
            await LogBackupAuditSafelyAsync(
                valid ? SecurityEventType.BackupVerificationPassed : SecurityEventType.BackupVerificationFailed,
                "DatabaseBackupVerification",
                backup.FileName,
                valid ? "SQL backup vượt qua RESTORE VERIFYONLY WITH CHECKSUM." : "SQL backup không vượt qua RESTORE VERIFYONLY WITH CHECKSUM.");
            _notificationService.Show(valid
                ? $"Backup hợp lệ: {backup.FileName}"
                : $"Backup không hợp lệ: {backup.FileName}");
            return (backup.FilePath, valid);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database backup verification failed. BackupId={BackupId}", backupId);
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupVerificationFailed,
                "DatabaseBackupVerification",
                backup.FileName,
                "Xác thực SQL backup thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không xác thực được backup. Vui lòng kiểm tra SQL Server và file backup.");
            return (backup.FilePath, null);
        }
    }

    private async Task RestoreBackupAsync(int backupId)
    {
        if (_isBackupOperationRunning)
        {
            return;
        }

        if (!_authorizationService.HasPermission(Permission.RestoreDatabase))
        {
            await _securityAuditService.LogAsync(
                SecurityEventType.PermissionDenied,
                "Settings",
                "DatabaseRestore",
                backupId.ToString(CultureInfo.InvariantCulture),
                "Restore database bị từ chối do thiếu quyền Database.Restore.");
            _notificationService.Show("Bạn không có quyền Database.Restore để khôi phục database.");
            return;
        }

        var backup = BackupRecords.FirstOrDefault(x => x.Id == backupId);
        if (backup is null)
        {
            _notificationService.Show("Không tìm thấy bản backup cần khôi phục.");
            return;
        }

        try
        {
            _isBackupOperationRunning = true;
            var valid = await _backupService.ValidateBackupAsync(backup.FilePath);
            if (!valid)
            {
                await _securityAuditService.LogAsync(
                    SecurityEventType.RestoreFailed,
                    "Settings",
                    "DatabaseRestore",
                    backup.FileName,
                    "Restore database bị dừng vì backup không vượt qua kiểm tra checksum.");
                _notificationService.Show($"Backup không hợp lệ, không thể khôi phục: {backup.FileName}");
                return;
            }

            var requireConfirmation = await _securitySettingsService.GetRequireAdminConfirmationAsync();
            var confirmed = await SettingsConfirmationPolicy.ConfirmIfRequiredAsync(
                requireConfirmation,
                () => _authorizationService.ConfirmDangerousActionAsync(
                    "Xác nhận khôi phục database",
                    $"Thao tác này sẽ ghi đè database đang chạy bằng bản backup:\n\n{backup.FileName}\n\nChỉ tiếp tục nếu đã dừng giao dịch, có approval và đã kiểm tra kế hoạch rollback. Tiếp tục?"));
            if (!confirmed)
            {
                await _securityAuditService.LogAsync(
                    SecurityEventType.RestoreRequested,
                    "Settings",
                    "DatabaseRestore",
                    backup.FileName,
                    "Người vận hành đã hủy xác nhận restore database.");
                return;
            }

            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreRequested,
                "Settings",
                "DatabaseRestore",
                backup.FileName,
                requireConfirmation
                    ? "Đã xác nhận yêu cầu restore database."
                    : "Thực hiện theo chính sách không yêu cầu xác nhận bổ sung; quyền Database.Restore vẫn được kiểm tra.");

            await _backupService.RestoreAsync(backup.FilePath);
            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreCompleted,
                "Settings",
                "DatabaseRestore",
                backup.FileName,
                "Restore database hoàn tất; ứng dụng cần được khởi động lại để tạo lại DbContext.");
            _notificationService.Show("Khôi phục database hoàn tất. Vui lòng đóng và mở lại ứng dụng trước khi tiếp tục nghiệp vụ.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database restore failed from settings. BackupId={BackupId}", backupId);
            await _securityAuditService.LogAsync(
                SecurityEventType.RestoreFailed,
                "Settings",
                "DatabaseRestore",
                backup.FileName,
                "Restore database thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không khôi phục được database. Vui lòng kiểm tra audit log và nhật ký SQL Server.");
        }
        finally
        {
            _isBackupOperationRunning = false;
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
        if (SecurityRoles.Count == 0)
        {
            _notificationService.Show("Chưa có vai trò hoạt động để gán cho người dùng.");
            return;
        }

        var dialog = new AddEditUserDialog(SecurityRoles.ToList()) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Request is null) return;
        await _userAccountService.CreateUserAsync(dialog.Request);
        await LoadAsync();
        _notificationService.Show("Đã thêm user mới.");
    }

    private async Task EditUserAsync(Guid userId)
    {
        var user = SecurityUsers.FirstOrDefault(x => x.Id == userId)
            ?? await _userAccountService.GetUserByIdAsync(userId);
        if (user is null)
        {
            _notificationService.Show("Không tìm thấy tài khoản cần chỉnh sửa.");
            return;
        }

        var dialog = new EditUserDialog(user) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true || dialog.Request is null) return;

        await _userAccountService.UpdateUserAsync(dialog.Request);
        await LoadAsync();
        _notificationService.Show($"Đã cập nhật tài khoản {user.UserName}.");
    }

    private void OpenSecurityAdminWorkspace(string title)
    {
        var viewModel = _serviceProvider.GetRequiredService<SecurityAdminViewModel>();
        ClearDetailEditorStates();
        CurrentDetail = null;
        IsDetailMode = false;
        EmbeddedModuleTitle = title;
        EmbeddedModuleContent = viewModel;
        IsEmbeddedModuleMode = true;
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Cài đặt", "root"));
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Người dùng & phân quyền", "category") { ShowSeparator = true });
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(title, "embedded") { ShowSeparator = true });
        Breadcrumb = $"Cài đặt > Người dùng & phân quyền > {title}";
        RaisePageFlags();
    }

    [RelayCommand]
    private async Task ResetFirstUserPasswordAsync()
    {
        if (SecurityUsers.Count == 0) return;
        var dialog = new ResetPasswordDialog(SecurityUsers.ToList()) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true || dialog.SelectedUser is not { } user) return;
        await _userAccountService.ResetPasswordAsync(user.Id, dialog.NewPassword, dialog.MustChangePassword);
        await LoadAsync();
        _notificationService.Show($"Đã reset mật khẩu cho {user.UserName}.");
    }

    [RelayCommand]
    private async Task UnlockSelectedUserAsync()
    {
        var lockedUsers = SecurityUsers.Where(x => x.IsLocked).ToList();
        if (lockedUsers.Count == 0)
        {
            _notificationService.Show("Không có tài khoản nào đang bị khóa.");
            return;
        }

        var dialog = new UnlockUserDialog(lockedUsers) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() != true || dialog.SelectedUser is not { } user) return;
        await _userAccountService.UnlockUserAsync(user.Id);
        await LoadAsync();
        _notificationService.Show($"Đã mở khóa user {user.UserName}.");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var ok = await _databaseAdminService.TestConnectionAsync();
        ConnectionStatus = ok ? "Connected" : "Failed";
        _notificationService.Show(ok ? "Kết nối SQL thành công." : "Kết nối SQL thất bại.");
    }

    [RelayCommand]
    private async Task TestDatabaseConfigurationAsync()
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<IDatabaseConnectionConfigurationService>();
            var result = await service.TestAsync(CreateDatabaseConfigurationRequest());
            DatabaseConfigMessage = result.Message;
            _notificationService.Show(result.Message);
        }
        catch (ArgumentException ex)
        {
            DatabaseConfigMessage = ex.Message;
            _notificationService.Show(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveDatabaseConfigurationAsync()
    {
        if (!_authorizationService.HasPermission(Permission.ManageServices))
        {
            _notificationService.Show("Bạn không có quyền thay đổi cấu hình kết nối hệ thống.");
            return;
        }

        try
        {
            var service = _serviceProvider.GetRequiredService<IDatabaseConnectionConfigurationService>();
            await service.SaveAsync(CreateDatabaseConfigurationRequest());
            DatabaseConfigPassword = string.Empty;
            DatabaseConfigMessage = "Đã kiểm tra và lưu cấu hình an toàn. Khởi động lại ứng dụng để chuyển sang kết nối mới.";
            _notificationService.Show(DatabaseConfigMessage);
        }
        catch (ArgumentException ex)
        {
            DatabaseConfigMessage = ex.Message;
            _notificationService.Show(ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DatabaseConfigMessage = "Không lưu được cấu hình kết nối. Hãy kiểm tra quyền ghi thư mục cấu hình người dùng.";
            _notificationService.Show(DatabaseConfigMessage);
            Log.Warning(ex, "Could not save database connection configuration.");
        }
    }

    private DatabaseConnectionConfigurationRequest CreateDatabaseConfigurationRequest()
        => new(DatabaseConfigServer, DatabaseConfigName, DatabaseConfigAuthenticationMode,
            DatabaseConfigUserName, DatabaseConfigPassword);

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        if (_isBackupOperationRunning)
        {
            return;
        }

        try
        {
            _isBackupOperationRunning = true;
            await LogBackupAuditSafelyAsync(SecurityEventType.BackupStarted, "DatabaseBackup", null, "Bắt đầu tạo SQL Server backup.");
            var created = await _backupService.BackupNowAsync();
            BackupRecords.Insert(0, created);
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupCompleted,
                "DatabaseBackup",
                created.FileName,
                "Tạo SQL Server backup hoàn tất.",
                new Dictionary<string, string>
                {
                    ["VerificationStatus"] = created.Status,
                    ["SizeBytes"] = created.Size.ToString(CultureInfo.InvariantCulture)
                });
            _notificationService.Show(string.Equals(created.Status, "Đã xác thực", StringComparison.OrdinalIgnoreCase)
                ? $"Đã tạo và xác thực backup: {created.FileName}"
                : $"Đã tạo backup: {created.FileName}. Bản backup chưa được xác thực; cần kiểm tra trước khi restore.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database backup failed from settings.");
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupFailed,
                "DatabaseBackup",
                null,
                "Tạo SQL Server backup thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không tạo được backup. Vui lòng kiểm tra kết nối SQL Server và thư mục backup.");
        }
        finally
        {
            _isBackupOperationRunning = false;
        }
    }

    [RelayCommand]
    private async Task BackupFilesNowAsync()
    {
        if (_isBackupOperationRunning)
        {
            return;
        }

        try
        {
            _isBackupOperationRunning = true;
            await LogBackupAuditSafelyAsync(SecurityEventType.BackupStarted, "FileBackup", null, "Bắt đầu tạo file backup.");
            var created = await _fileBackupService.BackupNowAsync();
            FileBackupRecords.Insert(0, created);
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupCompleted,
                "FileBackup",
                created.FileName,
                "Tạo file backup có manifest/hash hoàn tất.",
                new Dictionary<string, string>
                {
                    ["FileCount"] = created.FileCount.ToString(CultureInfo.InvariantCulture),
                    ["SizeBytes"] = created.Size.ToString(CultureInfo.InvariantCulture)
                });
            _notificationService.Show($"Đã tạo và xác thực file backup: {created.FileName} ({created.FileCount} file)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "File backup failed from settings.");
            await LogBackupAuditSafelyAsync(
                SecurityEventType.BackupFailed,
                "FileBackup",
                null,
                "Tạo file backup thất bại.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không tạo được file backup. Vui lòng kiểm tra Storage.DataRootPath/VFC và thư mục backup.");
        }
        finally
        {
            _isBackupOperationRunning = false;
        }
    }

    private async Task LogBackupAuditSafelyAsync(
        SecurityEventType eventType,
        string entityType,
        string? entityId,
        string message,
        IDictionary<string, string>? metadata = null)
    {
        try
        {
            await _securityAuditService.LogAsync(eventType, "Settings", entityType, entityId, message, metadata);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not write backup security audit event {EventType} for {EntityType}.", eventType, entityType);
        }
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
            // Keep the UI guard defensive: a read-only definition must never
            // reach SaveValuesAsync, even if a stale binding marks the item
            // dirty or a future view forgets to disable its editor.
            if (item.IsReadOnly || def.IsReadOnly) continue;
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
            try
            {
                await _settingsService.SaveValuesAsync(requests);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Saving settings failed. Keys={Keys}", string.Join(",", requests.Select(x => x.Key)));
                _notificationService.Show($"Không thể lưu cài đặt: {ex.Message}");
                return;
            }
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
            NotifyAppModuleChanged(SelectedInstalledApp.AppKey);
        }

        if (requests.Count == 0 && SelectedInstalledApp is null)
        {
            _notificationService.Show("Không có thay đổi cấu hình nào có thể lưu ở mục này.");
            return;
        }

        _notificationService.Show("Đã lưu cài đặt.");
        await ReloadEditableDetailAsync();
    }

    [RelayCommand]
    private async Task ResetEditableAsync()
    {
        try
        {
            foreach (var item in EditableItems)
            {
                if (item.IsReadOnly) continue;
                if (_scopeMap.TryGetValue(item.Key, out var scope))
                {
                    var target = _scopeTargetMap.TryGetValue(item.Key, out var t) ? t : null;
                    await _settingsService.ResetToDefaultAsync(item.Key, scope, target);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Resetting settings failed.");
            _notificationService.Show($"Không thể khôi phục cài đặt: {ex.Message}");
            return;
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
            UpdatedBy = _currentUserService.CurrentUser?.UserName ?? "system"
        }, false);

        OnPropertyChanged(nameof(CurrentModuleIconPath));
        OnPropertyChanged(nameof(CurrentModuleIconName));
        _notificationService.Show("Đã lưu icon module.");
        RefreshInstalledAppsDetail();
    }

    private void NotifyAppModuleChanged(string appKey)
    {
        _settingsChangeNotifier.NotifyChanged(new SettingValueDto
        {
            Key = "AppModule.Configuration",
            Scope = SettingScope.System,
            Value = appKey,
            UpdatedAt = DateTime.Now,
            UpdatedBy = _currentUserService.CurrentUser?.UserName ?? "system"
        }, false);
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

            var targetCategoryKey = ResolveModuleSettingsCategory(appKey);
            if (targetCategoryKey is not null)
            {
                var targetCategory = Categories.FirstOrDefault(x =>
                    string.Equals(x.Key, targetCategoryKey, StringComparison.OrdinalIgnoreCase));
                if (targetCategory is not null)
                {
                    SelectCategoryForNavigation(targetCategory);
                    return;
                }
            }

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
        catch (ModuleSettingsValidationException ex)
        {
            Log.Warning(ex, "Invalid module setting {SettingKey} for {AppKey}", ex.Key, ModuleSettingsAppKey);
            var item = ModuleSettingSections.SelectMany(x => x.Items)
                .FirstOrDefault(x => string.Equals(x.Key, ex.Key, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
            {
                item.ValidationError = ex.Message;
            }
            ModuleValidationErrors.Clear();
            ModuleValidationErrors.Add(ex.Message);
            OnPropertyChanged(nameof(HasModuleValidationErrors));
            _toastNotificationService.Error("Cấu hình chưa hợp lệ. Vui lòng kiểm tra trường báo lỗi.");
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

    private static string? ResolveModuleSettingsCategory(string appKey)
        => string.Equals(appKey?.Trim(), "virtual-file-center", StringComparison.OrdinalIgnoreCase)
           || string.Equals(appKey?.Trim(), "vfc", StringComparison.OrdinalIgnoreCase)
            ? "vfc"
            : null;

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
        var failedSections = new List<string>();

        async Task TryLoadSectionAsync(string section, Func<Task> load)
        {
            try
            {
                await load();
            }
            catch (Exception ex)
            {
                failedSections.Add(section);
                Log.Warning(ex, "Could not load settings dashboard section {Section}.", section);
            }
        }

        try
        {
            // Paint a complete Home shell with honest pending values before
            // any provider/database call. The dashboard must remain useful
            // even when SQL or Windows probes are slow.
            CurrentUser = _currentUserService.CurrentUser?.DisplayName ?? "Chưa xác định";
            SystemHealthMigrations = "Migrations: Đang kiểm tra";
            SystemHealthEInvoice = "E-invoice: Đang kiểm tra";
            RefreshDynamicRows();
            ApplyRowsFilter();

            await TryLoadSectionAsync("database", async () =>
            {
                var connection = await _databaseAdminService.GetPrimaryConnectionAsync();
                SqlServerName = connection.Host;
                DatabaseName = connection.DatabaseName;
                ConnectionStatus = connection.Status;
                RefreshDynamicRows();
            });

            await TryLoadSectionAsync("infrastructure health", async () =>
            {
                var healthService = _serviceProvider.GetService<IInfrastructureHealthService>();
                if (healthService is null)
                {
                    SystemHealthMigrations = FormatHomeHealthStatus("Migrations", null);
                    SystemHealthEInvoice = FormatHomeHealthStatus("E-invoice", null);
                    return;
                }

                var report = await healthService.CheckAsync();
                SystemHealthMigrations = FormatHomeHealthStatus(
                    "Migrations",
                    report.Checks.FirstOrDefault(x => string.Equals(x.Name, "Migrations", StringComparison.OrdinalIgnoreCase)));
                SystemHealthEInvoice = FormatHomeHealthStatus(
                    "E-invoice",
                    report.Checks.FirstOrDefault(x => string.Equals(x.Name, "E-invoice", StringComparison.OrdinalIgnoreCase)));
            });

            CurrentUser = _currentUserService.CurrentUser?.DisplayName ?? "Chưa xác định";

            await TryLoadSectionAsync("database backups", async () =>
            {
                var records = await _backupService.GetRecentAsync();
                BackupRecords.Clear();
                foreach (var record in records) BackupRecords.Add(record);
            });

            await TryLoadSectionAsync("file backups", async () =>
            {
                var records = await _fileBackupService.GetRecentAsync();
                FileBackupRecords.Clear();
                foreach (var record in records) FileBackupRecords.Add(record);
            });

            await TryLoadSectionAsync("service status", async () =>
            {
                var statuses = await _serviceStatusService.GetServicesAsync();
                ServiceStatuses.Clear();
                foreach (var status in statuses) ServiceStatuses.Add(status);
            });

            await TryLoadSectionAsync("client registry", async () =>
            {
                var clientRegistry = _serviceProvider.GetService<IClientRegistryService>();
                if (clientRegistry is null) return;
                var clients = await clientRegistry.GetActiveClientsAsync();
                ClientRegistryEntries.Clear();
                foreach (var client in clients)
                {
                    ClientRegistryEntries.Add(client);
                }
            });

            await TryLoadSectionAsync("installed applications", async () =>
            {
                var apps = await _appModuleService.GetAppsAsync();
                InstalledApps.Clear();
                foreach (var app in apps) InstalledApps.Add(app);
            });

            await TryLoadSectionAsync("system paths and endpoints", async () =>
            {
                var dataRootSetting = await _settingsService.GetValueAsync("Storage.DataRootPath", SettingScope.System, null);
                _storageDataRootPath = dataRootSetting?.Value?.Trim() ?? string.Empty;
                _storageDataRootPathIsPersisted = dataRootSetting?.IsPersisted == true;
            });

            _apiRuntimeStatus = "Chưa kiểm tra";
            await TryLoadSectionAsync("API endpoint health probe", async () =>
            {
                var virtualExplorer = _serviceProvider.GetService<IVirtualExplorerService>();
                if (virtualExplorer is null)
                {
                    _apiRuntimeStatus = ResolveApiStatus(providerAvailable: false, probeStatus: null);
                    return;
                }

                var networkNodes = await virtualExplorer.GetChildrenAsync("Network");
                var apiNode = networkNodes.FirstOrDefault(x =>
                    string.Equals(x.DisplayName, "API Server", StringComparison.OrdinalIgnoreCase));
                _apiRuntimeStatus = ResolveApiStatus(providerAvailable: true, probeStatus: apiNode?.Status);
            });

            _backupScheduleStatus = "Chưa kiểm tra";
            await TryLoadSectionAsync("backup scheduler", async () =>
            {
                var scheduler = _serviceProvider.GetService<IBackgroundSchedulerService>();
                if (scheduler is null)
                {
                    _backupScheduleStatus = FormatBackupScheduleStatus(hasProvider: false, schedule: null);
                    return;
                }

                var schedule = (await scheduler.GetDefinitionsAsync())
                    .FirstOrDefault(x => string.Equals(x.Code, "DATABASE_BACKUP", StringComparison.OrdinalIgnoreCase));
                _backupScheduleStatus = FormatBackupScheduleStatus(hasProvider: true, schedule: schedule);
            });

            _virtualDriveStatus = "Chưa kiểm tra";
            await TryLoadSectionAsync("virtual drives", async () =>
            {
                var virtualExplorerService = _serviceProvider.GetService<IVirtualExplorerService>();
                if (virtualExplorerService is null) return;
                var driveNodes = await virtualExplorerService.GetChildrenAsync("This PC");
                var driveCount = driveNodes.Count(x => x.NodeType == VirtualNodeType.Drive);
                _virtualDriveStatus = driveCount > 0 ? $"{driveCount} drives" : "Chưa có drive";
            });

            await TryLoadSectionAsync("security users", async () =>
            {
                var users = await _userAccountService.GetUsersAsync();
                SecurityUsers.Clear();
                foreach (var user in users) SecurityUsers.Add(user);
            });

            await TryLoadSectionAsync("security roles and permissions", async () =>
            {
                var roles = await _roleService.GetRolesAsync();
                SecurityRoles.Clear();
                foreach (var role in roles) SecurityRoles.Add(role);

                _rolePermissionCodes.Clear();
                foreach (var role in SecurityRoles)
                {
                    _rolePermissionCodes[role.Id] = await _roleService.GetRolePermissionCodesAsync(role.Id);
                }
            });

            await TryLoadSectionAsync("security permission catalog", async () =>
            {
                var permissions = await _roleService.GetPermissionsAsync();
                SecurityPermissions.Clear();
                foreach (var permission in permissions) SecurityPermissions.Add(permission);
            });

            await TryLoadSectionAsync("login audit", async () =>
            {
                var audits = await _userAccountService.GetLoginAuditsAsync();
                SecurityLoginAudits.Clear();
                foreach (var audit in audits) SecurityLoginAudits.Add(audit);
            });

            await TryLoadSectionAsync("recent settings activity", async () =>
            {
                var changes = await _settingChangeLogRepository.GetRecentAsync(3);
                RecentActivities.Clear();
                foreach (var change in changes)
                {
                    var changedAt = change.ChangedAt.ToLocalTime().ToString("HH:mm");
                    var actor = string.IsNullOrWhiteSpace(change.ChangedBy) ? "system" : change.ChangedBy;
                    RecentActivities.Add($"{changedAt}  {change.SettingKey} · {actor}");
                }
                HasRecentActivities = RecentActivities.Count > 0;
            });

            await TryLoadSectionAsync("printers", async () =>
            {
                var printers = await _printerSettingsService.GetAvailablePrintersAsync();
                AvailablePrinters.Clear();
                foreach (var printer in printers) AvailablePrinters.Add(printer);
            });

            await TryLoadSectionAsync("printer permissions", async () =>
            {
                var permissions = await _printerSettingsService.GetPrinterPermissionsAsync();
                PrinterPermissions.Clear();
                foreach (var permission in permissions) PrinterPermissions.Add(permission);
            });

            if (failedSections.Count > 0)
            {
                _notificationService.Show($"Không thể tải một số dữ liệu cài đặt ({failedSections.Count} mục). Các nguồn khác vẫn được tải; xem log để biết chi tiết.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Loading system settings dashboard failed; previously loaded real data will remain visible.");
            _notificationService.Show("Không thể tải đầy đủ dữ liệu cài đặt. Dữ liệu đã tải được vẫn được giữ lại.");
        }
        finally
        {
            RefreshDynamicRows();
            ApplyRowsFilter();
        }
    }

    private void RefreshDynamicRows()
    {
        var lastBackup = BackupRecords.FirstOrDefault();
        var appCount = InstalledApps.Count;
        var apiStatus = _apiRuntimeStatus;
        var securityStatus = SecurityUsers.Count > 0
            ? $"{SecurityUsers.Count} users"
            : "Chưa có dữ liệu";

        SystemHealthDatabase = $"SQL Server: {ConnectionStatus}";
        SystemHealthApi = $"API endpoint probe: {apiStatus}";
        SystemHealthBackup = FormatHomeSqlBackupStatus(lastBackup);
        SystemHealthFileBackup = FormatHomeFileBackupStatus(FileBackupRecords.FirstOrDefault());
        SystemHealthAudit = SecurityLoginAudits.Count > 0
            ? $"Audit đăng nhập: {SecurityLoginAudits.Count} bản ghi"
            : "Audit đăng nhập: Chưa có dữ liệu";

        SetRowValue("home-db", ConnectionStatus);
        SetRowValue("home-api", apiStatus);
        SetRowValue("home-backup", lastBackup?.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Chưa có");
        SetRowValue("home-vfc", _virtualDriveStatus);
        SetRowValue("home-apps", $"{appCount} modules");
        SetRowValue("home-security", securityStatus);

        SetRowValue("db-connection", SqlServerName);
        SetRowValue("net-api", apiStatus);
        SetRowValue("backup-schedule", _backupScheduleStatus);
        SetRowValue("vfc-drives", _virtualDriveStatus);
    }

    private static string ResolveApiStatus(bool providerAvailable, string? probeStatus)
    {
        if (!providerAvailable)
        {
            return "Probe không khả dụng";
        }

        return string.IsNullOrWhiteSpace(probeStatus) ? "Probe không trả kết quả" : probeStatus;
    }

    private static string FormatHomeHealthStatus(string component, InfrastructureHealthCheckDto? check)
    {
        if (check is null)
        {
            return $"{component}: Không khả dụng";
        }

        var status = check.Status switch
        {
            "Healthy" => "OK",
            "Warning" => "Cảnh báo",
            "Unhealthy" => "Lỗi",
            _ => "Không xác định"
        };

        if (!string.Equals(check.Status, "Warning", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(check.Message))
        {
            return $"{component}: {status}";
        }

        var detail = check.Message.Trim();
        if (detail.Length > 88)
        {
            detail = detail[..85] + "…";
        }

        return $"{component}: {status} — {detail}";
    }

    private static string FormatHomeSqlBackupStatus(BackupRecordDto? backup)
    {
        if (backup is null)
        {
            return "SQL backup file: Chưa tìm thấy";
        }

        return $"SQL backup file: {backup.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
    }

    private static string FormatHomeFileBackupStatus(FileBackupRecordDto? backup)
    {
        if (backup is null)
        {
            return "File backup đã kiểm tra: Chưa có";
        }

        return $"File backup đã kiểm tra: {backup.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm} · {backup.FileCount:N0} tệp";
    }

    private static string GetRunningProductVersion()
        => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Chưa xác định";

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
        Categories.Add(new SettingsCategoryItem("hotel", "Khách sạn & Front Office", "\uE787", "Tập trung dữ liệu nền, giá, minibar và quy tắc vận hành khách sạn."));
        Categories.Add(new SettingsCategoryItem("system", "Hệ thống", "\uE770", "Máy chủ, chế độ vận hành và hiệu năng."));
        Categories.Add(new SettingsCategoryItem("database", "Cơ sở dữ liệu", "\uE9D2", "SQL Server, schema và bảo trì dữ liệu."));
        Categories.Add(new SettingsCategoryItem("network", "Kết nối mạng", "\uE839", "API, NAS, POS/FO client và ports."));
        Categories.Add(new SettingsCategoryItem("vfc", "Virtual File Center", "\uE8B7", "Virtual drives, mapping, index và retention."));
        Categories.Add(new SettingsCategoryItem("apps", "Ứng dụng", "\uE71D", "Cấu hình chi tiết cho từng module/app."));
        Categories.Add(new SettingsCategoryItem("users", "Người dùng & phân quyền", "\uE716", "Users, roles, policy và permission matrix."));
        Categories.Add(new SettingsCategoryItem("clients", "Máy trạm & dịch vụ", "\uE943", "Phiên máy trạm, sync service và service health; chưa có trusted device registry."));
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
            Row("home-api", "\uE968", "API endpoint probe", "Đích HTTP để kiểm tra kết nối; không khởi chạy API listener", targetPageKey: "network-api-server"),
            Row("home-backup", "\uE7B8", "Backup schedule", "Lịch backup tự động", targetPageKey: "backup-schedule"),
            Row("home-vfc", "\uE8B7", "Virtual drives", "Trạng thái ổ ảo", targetPageKey: "vfc-virtual-drives"),
            Row("home-apps", "\uE71D", "Ứng dụng đã cài", "Danh sách module ASMILE", targetPageKey: "apps-installed"),
            Row("home-security", "\uE72E", "Nhật ký & bảo mật", "Audit và log retention", targetPageKey: "logs-security")
        ]);
        _pages["hotel"] = Page("hotel", "Khách sạn & Front Office", "Trung tâm cấu hình nghiệp vụ khách sạn", "\uE787", [
            Row("hotel-room-configuration", "\uE8FA", "Cấu hình phòng", "Hạng phòng, phòng, tầng và tòa nhà; không trộn với tra cứu đặt phòng.", targetPageKey: "hotel-room-configuration"),
            Row("hotel-rate-engine", "\uE8C8", "Khung giá & giá phòng", "Thêm/sửa rate plan, giá theo ngày, mùa vụ và quy tắc giá.", targetPageKey: "hotel-rate-engine"),
            Row("hotel-housekeeping-supplies", "\uE8B7", "Định mức đồ vải & tiện nghi", "Master mã, nhóm, PAR và ngưỡng vận hành theo property; không mở tồn kho tác nghiệp.", targetPageKey: "hotel-housekeeping-supplies"),
            Row("hotel-minibar-items", "\uE14C", "Hàng hóa & minibar", "Cấu hình minibar theo cơ sở: mã tồn kho, giá/VAT, định mức, mã tính tiền và hiệu lực.", targetPageKey: "hotel-minibar-items")
        ]);
        _pages["system"] = Page("system", "Hệ thống", "Thông tin máy chủ và vận hành", "\uE770", [
            Row("system-server", "\uE946", "Thông tin máy chủ", "Tên máy, IP, tài khoản, múi giờ"),
            Row("system-mode", "\uE7EF", "Chế độ vận hành", "Chặn đăng nhập mới khi bảo trì; vai trò server/workstation chưa được hỗ trợ."),
            Row("system-folders", "\uE8B7", "Thư mục hệ thống", "Data root, temp, export"),
            Row("system-performance", "\uEA80", "Hiệu năng", "Uptime, bộ nhớ runtime, dịch vụ và tình trạng hạ tầng"),
            Row("system-storage", "\uE7F1", "Lưu trữ", "Virtual drives, file storage"),
            Row("system-remote", "\uE968", "Kết nối API & truy cập ngoài", "Endpoint probe và trạng thái firewall; không cấu hình listener API trong ứng dụng.")
        ]);
        _pages["database"] = Page("database", "Cơ sở dữ liệu", "Cấu hình SQL và schema", "\uE9D2", [
            Row("db-connection", "\uE9D2", "SQL Server connection", "Server, database, auth mode", targetPageKey: "database-sql-connection"),
            Row("db-command-policy", "\uE9D2", "Command timeout", "Thời gian chờ lệnh EF Core tới SQL Server"),
            Row("db-info", "\uE8F1", "Database information", "Size, collation, compatibility"),
            Row("db-schema", "\uE713", "Schema & migrations", "Version, pending migrations"),
            Row("db-objects", "\uE8A5", "Tables / Views / Stored procedures", "Kiểm tra schema objects"),
            Row("db-maint", "\uE9D9", "Maintenance", "Reindex, optimize, cleanup"),
            Row("db-integrity", "\uE9E9", "Integrity check", "DBCC, consistency scan"),
            Row("db-security", "\uE72E", "Connection string security", "Encrypt và credential policy")
        ]);
        _pages["network"] = Page("network", "Kết nối mạng", "API, NAS và clients", "\uE839", [
            Row("net-api", "\uE968", "API endpoint probe", "Base URL, port và HTTPS dùng bởi probe VFC; không cấu hình API listener", targetPageKey: "network-api-server"),
            Row("net-discovery", "\uE895", "Endpoint & phiên máy trạm", "Trạng thái endpoint đã cấu hình và phiên đăng nhập; chưa có LAN discovery provider."),
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
            Row("apps-shell-startup", "\uE7E8", "Khởi động cùng ASMILE BO", "Chọn module nội bộ tự mở khi shell bắt đầu", targetPageKey: "apps-shell-startup"),
            Row("apps-startup-windows", "\uE7E8", "Ứng dụng khởi động cùng Windows", "Xem ứng dụng tự khởi chạy của PC; Windows quản lý trạng thái bật/tắt", targetPageKey: "apps-startup-windows"),
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
        _pages["clients"] = Page("clients", "Máy trạm & dịch vụ", "Phiên đăng nhập và dịch vụ; định danh thiết bị chưa được xác thực", "\uE943", [
            Row("clients-reg", "\uE8D7", "Phiên máy trạm đang hoạt động", "Session đang hiệu lực; chưa phải thiết bị đã đăng ký"),
            Row("clients-pos", "\uE7F4", "POS terminals", "POS endpoints và sync"),
            Row("clients-fo", "\uE8FA", "FO clients", "FO status"),
            Row("clients-wh", "\uE7F1", "Warehouse clients", "Warehouse client status"),
            Row("clients-sync", "\uE895", "Sync service", "Trạng thái service; interval/conflict policy chưa có provider runtime."),
            Row("clients-print", "\uE749", "Print service", "Trạng thái Windows Print Spooler và lối mở cấu hình máy in."),
            Row("clients-health", "\uEA39", "Service health", "Database, migrations và trạng thái sẵn sàng hóa đơn điện tử")
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
            Row("ui-snap", "\uE92E", "Căn biểu tượng desktop vào lưới", "Bật/tắt tự căn vị trí biểu tượng khi kéo thả.", targetPageKey: "appearance-main")
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

    private SettingsPageViewModel Page(string key, string title, string subtitle, string icon, IEnumerable<SettingItemViewModel> rows)
    {
        var page = new SettingsPageViewModel(key, title, subtitle, icon);
        foreach (var row in rows) page.Rows.Add(row);
        return page;
    }

    // Every settings landing row must be navigable. Read-only rows still open
    // their status/detail page; editability is decided by the destination's
    // real provider, not by disabling the landing item.
    private static SettingItemViewModel Row(string id, string icon, string title, string description, string value = "", string badge = "", bool isSensitive = false, string? targetPageKey = null)
    {
        return new SettingItemViewModel(id, icon, title, description, value, badge, targetPageKey ?? id)
        {
            IsSensitive = isSensitive,
            IsClickable = true,
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

    private SettingsDetailViewModel BuildBackupDetail()
    {
        var detail = new SettingsDetailViewModel
        {
            Title = "Sao lưu & phục hồi",
            Subtitle = "Tạo, kiểm tra và khôi phục có kiểm soát backup database và file dữ liệu đã cấu hình.",
            Breadcrumb = "Cài đặt > Sao lưu & phục hồi"
        };

        var actions = new SettingCardViewModel("Thao tác an toàn", "Backup database và file được tạo từ provider thật; thao tác restore luôn qua quyền, xác nhận và audit.");
        actions.Items.Add(new SettingItemViewModel("backup-now", "\uE898", "Backup ngay", "Tạo COPY_ONLY backup; chỉ ghi nhận đã xác thực khi RESTORE VERIFYONLY WITH CHECKSUM thành công.", "Sẵn sàng", targetPageKey: "backup-now"));
        actions.Items.Add(new SettingItemViewModel("backup-files-now", "\uE8B7", "Backup file ngay", "Đóng gói các thư mục Storage/VFC đã cấu hình, kèm manifest và SHA-256 từng file.", "Sẵn sàng", targetPageKey: "backup-files-now"));
        actions.Items.Add(new SettingItemViewModel("backup-schedule", "\uE787", "Lịch backup", "Cấu hình đường dẫn, lịch chạy, retention và verify sau backup.", "Cấu hình", targetPageKey: "backup-schedule"));

        var canRestore = _authorizationService.HasPermission(Permission.RestoreDatabase);
        var restoreStatus = canRestore ? "Chờ phê duyệt" : "Thiếu quyền";
        var restoreDescription = canRestore
            ? "Có quyền Database.Restore; thao tác vẫn bị khóa mặc định và chỉ mở sau khi bật cấu hình restore, xác nhận quản trị và hoàn tất drill trên staging."
            : "Thiếu quyền Database.Restore; thao tác bị khóa mặc định để tránh ghi đè dữ liệu production ngoài quy trình vận hành.";
        var restore = new SettingItemViewModel(
            "backup-db",
            "\uE7B8",
            "Restore database",
            restoreDescription,
            restoreStatus,
            targetPageKey: "settings-info:backup-db-restore-policy");
        restore.IsReadOnly = true;
        actions.Items.Add(restore);

        var canRestoreFiles = canRestore && FileBackupRecords.Count > 0;
        var restoreFiles = new SettingItemViewModel(
            "backup-files",
            "\uE8B7",
            "Restore files",
            canRestore
                ? "Khôi phục additive từ file backup đã xác thực; không xóa file hiện có."
                : "Thiếu quyền Database.Restore; thao tác khôi phục file bị khóa.",
            canRestoreFiles ? "Chờ xác nhận" : canRestore ? "Chưa có backup" : "Thiếu quyền",
            targetPageKey: canRestoreFiles ? "backup-files" : "settings-info:backup-files-restore-policy");
        restoreFiles.IsReadOnly = !canRestoreFiles;
        restoreFiles.IsDanger = canRestoreFiles;
        actions.Items.Add(restoreFiles);
        detail.Cards.Add(actions);

        var restoreActions = new SettingCardViewModel("Khôi phục database có kiểm soát", "Chỉ khả dụng với quyền Database.Restore, xác nhận quản trị và backup đã vượt qua checksum.");
        if (!canRestore)
        {
            AddReadOnlyVfcItem(restoreActions, "backup-restore-denied", "Không đủ quyền khôi phục", "Cấp quyền Database.Restore cho vai trò được phê duyệt trước khi thao tác.", "Bị khóa");
        }
        else if (BackupRecords.Count == 0)
        {
            AddReadOnlyVfcItem(restoreActions, "backup-restore-empty", "Chưa có backup để khôi phục", "Hãy tạo và xác thực ít nhất một bản backup trước khi yêu cầu restore.", "Trống");
        }
        else
        {
            foreach (var backup in BackupRecords)
            {
                var restoreItem = new SettingItemViewModel(
                    $"restore-{backup.Id}",
                    "\uE7B8",
                    $"Khôi phục {backup.FileName}",
                    $"{backup.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} · {FormatBytes(backup.Size)} · sẽ xác nhận checksum và hỏi lại trước khi ghi đè",
                    "Yêu cầu restore",
                    targetPageKey: $"backup-restore:{backup.Id}");
                restoreItem.IsDanger = true;
                restoreActions.Items.Add(restoreItem);
            }
        }
        detail.Cards.Add(restoreActions);

        var history = new SettingCardViewModel("Lịch sử backup", "File mới tìm thấy mặc định là chưa xác thực; bấm để chạy RESTORE VERIFYONLY WITH CHECKSUM.");
        if (BackupRecords.Count == 0)
        {
            AddReadOnlyVfcItem(history, "backup-history-empty", "Chưa có backup", "Chưa tìm thấy file asmile_backup_*.bak trong thư mục backup được phép.", "Trống");
        }
        else
        {
            foreach (var backup in BackupRecords)
            {
                history.Items.Add(new SettingItemViewModel(
                    $"backup-{backup.Id}",
                    "\uE81C",
                    backup.FileName,
                    $"{backup.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} · {FormatBytes(backup.Size)} · Bấm để kiểm tra checksum",
                    backup.Status,
                    targetPageKey: $"backup-verify:{backup.Id}"));
            }
        }

        detail.Cards.Add(history);

        var fileHistory = new SettingCardViewModel("Lịch sử backup file", "Manifest và SHA-256 được kiểm tra trước khi cho phép khôi phục.");
        if (FileBackupRecords.Count == 0)
        {
            AddReadOnlyVfcItem(fileHistory, "file-backup-history-empty", "Chưa có file backup", "Tạo backup file để lưu Storage.DataRootPath và các đường dẫn VFC đã cấu hình.", "Trống");
        }
        else
        {
            foreach (var backup in FileBackupRecords)
            {
                fileHistory.Items.Add(new SettingItemViewModel(
                    $"file-backup-{backup.Id}",
                    "\uE81C",
                    backup.FileName,
                    $"{backup.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} · {FormatBytes(backup.Size)} · {backup.FileCount} file · Bấm để kiểm tra hash",
                    backup.Status,
                    targetPageKey: $"backup-file-verify:{backup.Id}"));

                if (canRestore)
                {
                    var restoreFile = new SettingItemViewModel(
                        $"file-restore-{backup.Id}",
                        "\uE7B8",
                        $"Khôi phục {backup.FileName}",
                        "Ghi đè file trùng tên sau khi xác nhận quản trị; không xóa file ngoài manifest.",
                        "Yêu cầu restore",
                        targetPageKey: $"backup-file-restore:{backup.Id}");
                    restoreFile.IsDanger = true;
                    fileHistory.Items.Add(restoreFile);
                }
            }
        }

        detail.Cards.Add(fileHistory);
        return detail;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.0} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024):0.0} MB";
        return $"{bytes / (1024d * 1024 * 1024):0.0} GB";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1) return $"{duration.Days} ngày {duration.Hours} giờ";
        if (duration.TotalHours >= 1) return $"{duration.Hours} giờ {duration.Minutes} phút";
        return $"{duration.Minutes} phút {duration.Seconds} giây";
    }

    private SettingsDetailViewModel BuildDetail(SettingsPageViewModel page, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > {page.Title} > {row.Title}"
        };

        if (string.Equals(page.Key, "printing", StringComparison.OrdinalIgnoreCase))
        {
            return BuildPrinterDetail(detail, row);
        }

        if (string.Equals(page.Key, "clients", StringComparison.OrdinalIgnoreCase))
        {
            return BuildClientServiceDetail(detail, row);
        }

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
                usersCard.Items.Add(new SettingItemViewModel(
                    $"user-{user.Id}", "\uE77B", user.UserName,
                    $"{user.DisplayName} · {(user.Roles.Count == 0 ? "Chưa gán vai trò" : string.Join(", ", user.Roles))}{(string.IsNullOrWhiteSpace(user.Email) ? string.Empty : $" · {user.Email}")}",
                    user.IsLocked ? "Khóa" : (user.IsActive ? "Hoạt động" : "Ngưng"),
                    targetPageKey: $"users-edit:{user.Id:D}"));
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
                rolesCard.Items.Add(new SettingItemViewModel($"role-{role.Id}", "\uE902", role.Name, role.Description ?? string.Empty, role.IsSystem ? "Hệ thống" : "Tùy chỉnh", targetPageKey: "users-role-manager"));
            }
            rolesCard.Items.Add(new SettingItemViewModel("users-role-manager-open", "\uE8D7", "Mở trình quản trị vai trò", "Chỉnh sửa vai trò, gán/gỡ quyền và gán người dùng; thay đổi được lưu qua dịch vụ phân quyền.", "Quản lý", targetPageKey: "users-role-manager"));
            detail.Cards.Add(rolesCard);
            return detail;
        }

        if (page.Key == "users" && row.Id == "users-matrix")
        {
            var matrixCard = new SettingCardViewModel("Permission matrix", "Quyền đã gán thực tế cho từng vai trò");
            foreach (var role in SecurityRoles)
            {
                var codes = _rolePermissionCodes.TryGetValue(role.Id, out var assigned)
                    ? assigned
                    : Array.Empty<string>();
                var permissionSummary = codes.Count == 0
                    ? "Chưa được gán quyền"
                    : string.Join(", ", codes.Take(6)) + (codes.Count > 6 ? $" … (+{codes.Count - 6})" : string.Empty);
                matrixCard.Items.Add(new SettingItemViewModel(
                    $"perm-role-{role.Id}",
                    "\uE902",
                    role.Name,
                    permissionSummary,
                    $"{codes.Count} quyền",
                    targetPageKey: "users-permission-matrix"));
            }
            matrixCard.Items.Add(new SettingItemViewModel("users-permission-matrix-open", "\uE8D7", "Chỉnh sửa ma trận quyền", "Mở trình quản trị để gán hoặc gỡ quyền theo vai trò.", "Quản lý", targetPageKey: "users-permission-matrix"));
            detail.Cards.Add(matrixCard);
            return detail;
        }

        // Unknown/dynamic rows must fail closed without exposing developer
        // terminology or presenting a fake editable surface to operators.
        var generic = new SettingCardViewModel("Cấu hình chỉ đọc", "Mục này chưa có setting definition để chỉnh trực tiếp. Các dòng vẫn mở thông tin chi tiết; không tạo giá trị cấu hình giả.");
        AddReadOnlyVfcItem(generic, "read-only-status", "Trạng thái", "Không có cấu hình runtime tương ứng trong Settings Engine.", "Chỉ đọc");
        AddReadOnlyVfcItem(generic, "read-only-detail", row.Title, row.Description, row.Value);
        detail.Cards.Add(generic);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildDatabaseDetailAsync(string route, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Cơ sở dữ liệu > {row.Title}"
        };

        if (string.Equals(route, "db-maint-run", StringComparison.OrdinalIgnoreCase))
        {
            await _databaseAdminService.RunMaintenanceAsync();
            _notificationService.Show("Đã hoàn tất database maintenance preflight.");
            route = "db-maint";
        }

        if (string.Equals(route, "db-integrity-checkpoint", StringComparison.OrdinalIgnoreCase))
        {
            var checkpointId = await _dataIntegrityService.CreateCheckpointAsync("database-integrity");
            _notificationService.Show($"Đã tạo checkpoint toàn vẹn dữ liệu: {checkpointId}");
            route = "db-integrity";
        }

        var connection = await _databaseAdminService.GetPrimaryConnectionAsync();
        var connectionCard = new SettingCardViewModel("Kết nối hiện tại", "Thông tin đọc trực tiếp từ DbConnection và CanConnectAsync");
        AddReadOnlyVfcItem(
            connectionCard,
            "db-connection-type",
            "Provider",
            connection.Type,
            connection.Status,
            "database-sql-connection");
        AddReadOnlyVfcItem(connectionCard, "db-connection-host", "Server", connection.Host, connection.Port.ToString(), "database-sql-connection");
        AddReadOnlyVfcItem(connectionCard, "db-connection-database", "Database", connection.DatabaseName, $"Kiểm tra {connection.LastCheckedAt:yyyy-MM-dd HH:mm}", "database-sql-connection");
        detail.Cards.Add(connectionCard);

        if (string.Equals(route, "db-info", StringComparison.OrdinalIgnoreCase))
        {
            var health = _serviceProvider.GetService<IInfrastructureHealthService>();
            if (health is not null)
            {
                var report = await health.CheckAsync();
                var healthCard = new SettingCardViewModel("Infrastructure health", "Kết quả kiểm tra runtime hiện tại");
                AddReadOnlyVfcItem(healthCard, "db-health-status", "Tổng trạng thái", report.Status, $"Kiểm tra {report.CheckedAt:yyyy-MM-dd HH:mm}");
                foreach (var check in report.Checks)
                {
                    AddReadOnlyVfcItem(healthCard, $"db-health-{check.Name}", check.Name, check.Message, check.Status);
                }
                detail.Cards.Add(healthCard);
            }

            var actions = new SettingCardViewModel("Thao tác cấu hình", "Thông tin runtime ở trên chỉ đọc; dùng các thao tác này để thay đổi cấu hình hoặc chạy lại kiểm tra.");
            actions.Items.Add(new SettingItemViewModel(
                "db-info-open-connection", "\uE774", "Chỉnh cấu hình kết nối", "Thay máy chủ, database hoặc phương thức xác thực; kiểm tra trước khi lưu.", "Mở cấu hình", targetPageKey: "database-sql-connection"));
            actions.Items.Add(new SettingItemViewModel(
                "db-info-refresh", "\uE72C", "Kiểm tra lại tình trạng", "Tải lại kết nối và infrastructure health từ runtime.", "Làm mới", targetPageKey: "db-info"));
            detail.Cards.Add(actions);

            return detail;
        }

        if (string.Equals(route, "db-schema", StringComparison.OrdinalIgnoreCase))
        {
            var migration = _serviceProvider.GetService<IDatabaseMigrationService>();
            var card = new SettingCardViewModel("Schema & migrations", "Trạng thái đọc từ migration service");
            if (migration is null)
            {
                AddReadOnlyVfcItem(card, "db-schema-provider-missing", "Chưa có migration provider", "Không suy diễn version schema khi thiếu provider.", "Chưa sẵn sàng");
            }
            else
            {
                var status = await migration.ValidatePendingMigrationsAsync();
                AddReadOnlyVfcItem(card, "db-schema-connect", "Kết nối", "Khả năng truy cập database khi kiểm tra migration.", status.CanConnect ? "Connected" : "Disconnected");
                AddReadOnlyVfcItem(card, "db-schema-pending", "Migration chờ áp dụng", "Danh sách migration chưa được áp dụng.", $"{status.PendingMigrations.Count}");
                AddReadOnlyVfcItem(card, "db-schema-applied", "Migration đã áp dụng", "Danh sách migration đã ghi nhận.", $"{status.AppliedMigrations.Count}");
                AddReadOnlyVfcItem(card, "db-schema-seed", "Cảnh báo seed", "Cảnh báo nhất quán seed trả về từ migration service.", $"{status.SeedWarnings.Count}");
            }
            detail.Cards.Add(card);
            var schemaActions = new SettingCardViewModel("Thao tác", "Migration là dữ liệu trạng thái; kiểm tra lại để nạp kết quả mới nhất.");
            schemaActions.Items.Add(new SettingItemViewModel(
                "db-schema-refresh", "\uE72C", "Kiểm tra migration lại", "Đọc lại trạng thái kết nối và danh sách migration từ provider.", "Làm mới", targetPageKey: "db-schema"));
            detail.Cards.Add(schemaActions);
            return detail;
        }

        if (string.Equals(route, "db-integrity", StringComparison.OrdinalIgnoreCase))
        {
            var card = await BuildIntegrityCardAsync("Database integrity scan", "Kết quả đọc từ DataIntegrityScanResults");
            var actions = new SettingCardViewModel("Thao tác an toàn", "Checkpoint không thay đổi dữ liệu nghiệp vụ");
            actions.Items.Add(new SettingItemViewModel(
                "db-integrity-checkpoint-action",
                "\uE8A4",
                "Tạo checkpoint",
                "Ghi fingerprint checkpoint vào database để đối chiếu về sau.",
                "Tạo checkpoint",
                targetPageKey: "db-integrity-checkpoint"));
            detail.Cards.Add(card);
            detail.Cards.Add(actions);
            return detail;
        }

        if (string.Equals(route, "db-maint", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Maintenance", "Preflight thực tế; không tự ý chạy lệnh phá hủy");
            AddReadOnlyVfcItem(card, "db-maint-scope", "Phạm vi hiện tại", "DatabaseAdminService kiểm tra kết nối và seed consistency.", connection.Status);
            card.Items.Add(new SettingItemViewModel(
                "db-maint-run-action",
                "\uE9D9",
                "Chạy maintenance preflight",
                "Thực hiện kiểm tra kết nối và seed consistency; không reindex/drop/cleanup khi chưa có policy cụ thể.",
                "Chạy kiểm tra",
                targetPageKey: "db-maint-run"));
            detail.Cards.Add(card);
            return detail;
        }

        if (string.Equals(route, "db-security", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Connection security", "Thông tin cấu hình được đọc từ Settings Engine; không hiển thị secret");
            var auth = await _settingsService.GetValueAsync("Database.AuthenticationMode", SettingScope.System, null);
            var user = await _settingsService.GetValueAsync("Database.UserName", SettingScope.System, null);
            var password = await _settingsService.GetValueAsync("Database.Password", SettingScope.System, null);
            AddReadOnlyVfcItem(card, "db-security-auth", "Authentication", auth?.Value ?? "Chưa cấu hình", "Settings Engine", "database-sql-connection");
            AddReadOnlyVfcItem(card, "db-security-user", "User name", user?.Value ?? "Chưa cấu hình", "Không phải secret", "database-sql-connection");
            AddReadOnlyVfcItem(card, "db-security-password", "Password", string.IsNullOrWhiteSpace(password?.Value) ? "Chưa cấu hình" : "Đã cấu hình", "Không hiển thị giá trị", "database-sql-connection");
            detail.Cards.Add(card);
            var actions = new SettingCardViewModel("Chỉnh thông tin kết nối", "Thông tin xác thực được giữ kín; thay đổi tại trình cấu hình SQL có kiểm tra kết nối.");
            actions.Items.Add(new SettingItemViewModel(
                "db-security-edit-connection", "\uE774", "Mở cấu hình SQL Server", "Chỉnh server, database và phương thức xác thực mà không lộ mật khẩu đã lưu.", "Chỉnh sửa", targetPageKey: "database-sql-connection"));
            detail.Cards.Add(actions);
            return detail;
        }

        if (string.Equals(route, "db-objects", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Schema objects", "Metadata đọc trực tiếp từ DbConnection; không suy diễn từ model");
            try
            {
                var objects = await _databaseAdminService.GetDatabaseObjectsAsync();
                foreach (var group in objects.GroupBy(x => x.ObjectType, StringComparer.OrdinalIgnoreCase))
                {
                    AddReadOnlyVfcItem(card, $"db-objects-count-{group.Key}", group.Key, "Số object provider trả về.", $"{group.Count()}");
                }

                foreach (var item in objects.Take(30))
                {
                    AddReadOnlyVfcItem(card, $"db-object-{item.ObjectType}-{item.Schema}-{item.Name}", item.Name, item.Schema, item.ObjectType);
                }

                if (objects.Count == 0)
                {
                    AddReadOnlyVfcItem(card, "db-objects-empty", "Không có metadata", "Provider không trả về table/view/procedure nào.", "Chưa có dữ liệu");
                }
            }
            catch (Exception ex) when (ex is DbException or InvalidOperationException or NotSupportedException or TimeoutException)
            {
                AddReadOnlyVfcItem(card, "db-objects-error", "Không đọc được metadata", $"Provider trả về lỗi {ex.GetType().Name}; xem log vận hành để biết chi tiết.", "Lỗi provider");
            }

            detail.Cards.Add(card);
            var actions = new SettingCardViewModel("Thao tác", "Danh sách object là metadata chỉ đọc; có thể nạp lại từ database.");
            actions.Items.Add(new SettingItemViewModel(
                "db-objects-refresh", "\uE72C", "Tải lại schema objects", "Đọc lại tables, views và stored procedures từ DbConnection hiện tại.", "Làm mới", targetPageKey: "db-objects"));
            detail.Cards.Add(actions);
            return detail;
        }

        var unavailable = new SettingCardViewModel("Database metadata", "Chưa có provider metadata riêng trong Settings");
        AddReadOnlyVfcItem(unavailable, "db-metadata-provider", row.Title, "Không dựng danh sách table/view/procedure nếu chưa có truy vấn metadata được kiểm chứng.", "Chỉ đọc");
        detail.Cards.Add(unavailable);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildDatabaseConnectionDetailAsync(SettingItemViewModel row)
    {
        var configurationService = _serviceProvider.GetRequiredService<IDatabaseConnectionConfigurationService>();
        var editable = configurationService.GetCurrent();
        DatabaseConfigServer = editable.Server;
        DatabaseConfigName = editable.Database;
        DatabaseConfigAuthenticationMode = editable.AuthenticationMode;
        DatabaseConfigUserName = editable.UserName;
        DatabaseConfigPassword = string.Empty;
        IsDatabaseConnectionManagedExternally = new[]
        {
            Environment.GetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("Database__ConnectionString"),
            Environment.GetEnvironmentVariable("Database:ConnectionString")
        }.Any(value => !string.IsNullOrWhiteSpace(value));
        DatabaseConfigMessage = IsDatabaseConnectionManagedExternally
            ? "Hiện đang dùng cấu hình deployment. Cấu hình lưu từ đây sẽ được ưu tiên sau khi khởi động lại."
            : "Thay đổi chỉ có hiệu lực sau khi kiểm tra kết nối, lưu và khởi động lại ứng dụng.";
        IsDatabaseConnectionEditorVisible = true;

        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = "Chỉnh cấu hình SQL Server, kiểm tra kết nối rồi lưu. Ứng dụng sẽ dùng cấu hình mới sau khi khởi động lại.",
            Breadcrumb = $"Cài đặt > Cơ sở dữ liệu > {row.Title}"
        };

        var connection = await _databaseAdminService.GetPrimaryConnectionAsync();
        ConnectionStatus = connection.Status;
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildSecurityOperationalDetailAsync(string route, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Nhật ký & bảo mật > {row.Title}"
        };
        var card = new SettingCardViewModel("Dữ liệu bảo mật", "Chỉ hiển thị bản ghi do provider thật trả về");
        if (!_authorizationService.HasPermission(Permission.ViewAuditLogs))
        {
            AddReadOnlyVfcItem(card, "security-log-access-denied", "Không đủ quyền xem nhật ký", "Cần quyền Logs.View để đọc audit, login, đồng bộ hoặc export log.", "Bị hạn chế");
            detail.Cards.Add(card);
            return detail;
        }

        if (string.Equals(route, "sec-audit", StringComparison.OrdinalIgnoreCase))
        {
            if (_serviceProvider.GetService<IAuditService>() is not { } auditService)
            {
                AddReadOnlyVfcItem(card, "security-audit-provider-missing", "Audit provider", "Chưa đăng ký dịch vụ truy vấn AuditLogs.", "Chưa sẵn sàng");
            }
            else
            {
                try
                {
                    var records = await auditService.QueryAsync();
                    AddAuditLogRows(card, records);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not load persisted settings audit log.");
                    AddReadOnlyVfcItem(card, "security-audit-query-error", "Không tải được audit log", "Truy vấn AuditLogs thất bại; không hiển thị dữ liệu cache như dữ liệu mới.", ex.GetType().Name);
                }
            }
        }
        else if (string.Equals(route, "sec-login", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var records = await _userAccountService.GetLoginAuditsAsync();
                AddLoginAuditRows(card, records);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not load persisted login audit log.");
                AddReadOnlyVfcItem(card, "security-login-query-error", "Không tải được login log", "Truy vấn LoginAudits thất bại; không hiển thị dữ liệu cache như dữ liệu mới.", ex.GetType().Name);
            }
        }
        else if (string.Equals(route, "sec-error", StringComparison.OrdinalIgnoreCase))
        {
            var errorLogService = _serviceProvider.GetService<IErrorLogQueryService>();
            if (errorLogService is null)
            {
                AddReadOnlyVfcItem(card, "security-error-log-provider-missing", "Error log provider", "Chưa đăng ký provider đọc file sink Serilog đã cấu hình.", "Chưa sẵn sàng");
            }
            else
            {
                var snapshot = await errorLogService.GetRecentErrorsAsync(100);
                AddReadOnlyVfcItem(card, "security-error-log-status", "Nguồn log", snapshot.Status, snapshot.IsAvailable ? "Đã kiểm tra" : "Không khả dụng");
                foreach (var entry in snapshot.Entries)
                {
                    AddReadOnlyVfcItem(
                        card,
                        $"security-error-{entry.Timestamp.Ticks}-{entry.Level}",
                        $"{entry.Level} · {entry.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
                        entry.Message,
                        "Log ứng dụng");
                }
            }
        }
        else if (string.Equals(route, "sec-sensitive", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(route, "sec-confirm", StringComparison.OrdinalIgnoreCase))
        {
            if (_serviceProvider.GetService<IAuditService>() is not { } auditService)
            {
                AddReadOnlyVfcItem(card, "security-confirmation-provider-missing", "Audit provider", "Chưa đăng ký dịch vụ truy vấn lịch sử xác nhận từ AuditLogs.", "Chưa sẵn sàng");
            }
            else
            {
                try
                {
                    var confirmed = await auditService.QueryAsync(action: SecurityEventType.DangerousActionConfirmed.ToString());
                    var rejected = await auditService.QueryAsync(action: SecurityEventType.DangerousActionRejected.ToString());
                    var decisions = confirmed
                        .Select(record => new { Record = record, WasConfirmed = true })
                        .Concat(rejected.Select(record => new { Record = record, WasConfirmed = false }))
                        .OrderByDescending(item => item.Record.CreatedAt)
                        .Take(30)
                        .ToList();

                    if (decisions.Count == 0)
                    {
                        AddReadOnlyVfcItem(card, "security-confirmation-empty", "Chưa có quyết định xác nhận", "Lịch sử sẽ xuất hiện sau khi người dùng xác nhận hoặc từ chối một thao tác được bảo vệ.", "Chưa có dữ liệu");
                    }
                    else
                    {
                        foreach (var decision in decisions)
                        {
                            var record = decision.Record;
                            AddReadOnlyVfcItem(
                                card,
                                $"security-confirmation-{record.Id}",
                                $"{(decision.WasConfirmed ? "Đã xác nhận" : "Đã từ chối")} · {record.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
                                $"Người thực hiện: {record.Username}",
                                decision.WasConfirmed ? "Đã xác nhận" : "Đã từ chối");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not load persisted dangerous-action confirmation history.");
                    AddReadOnlyVfcItem(card, "security-confirmation-query-error", "Không tải được lịch sử xác nhận", "Truy vấn AuditLogs thất bại; không hiển thị dữ liệu mẫu hoặc cache như dữ liệu mới.", ex.GetType().Name);
                }
            }
        }
        else if (string.Equals(route, "sec-sync", StringComparison.OrdinalIgnoreCase))
        {
            var syncServices = ServiceStatuses
                .Where(x => x.ServiceName.Contains("sync", StringComparison.OrdinalIgnoreCase)
                            || x.DisplayName.Contains("sync", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (syncServices.Count == 0)
            {
                AddReadOnlyVfcItem(card, "security-sync-empty", "Chưa có sync service", "Không có service heartbeat tên sync; không dựng log đồng bộ giả.", "Chưa có dữ liệu");
            }
            else
            {
                foreach (var service in syncServices)
                {
                    AddReadOnlyVfcItem(card, $"security-sync-{service.Id}", service.DisplayName, service.ServiceName, service.Status);
                }
            }

            var scheduler = _serviceProvider.GetService<IBackgroundSchedulerService>();
            if (scheduler is not null)
            {
                try
                {
                    var dashboard = await scheduler.GetDashboardAsync();
                    AddReadOnlyVfcItem(card, "security-sync-scheduler-running", "Scheduler đang chạy", "Số job background đang thực thi từ scheduler persisted.", dashboard.RunningJobs.ToString(CultureInfo.InvariantCulture));
                    AddReadOnlyVfcItem(card, "security-sync-scheduler-queued", "Scheduler đang xếp hàng", "Số job đang chờ worker xử lý.", dashboard.QueuedJobs.ToString(CultureInfo.InvariantCulture));
                    AddReadOnlyVfcItem(card, "security-sync-scheduler-failed", "Scheduler lỗi", "Số job failed được đọc từ execution store.", dashboard.FailedJobs.ToString(CultureInfo.InvariantCulture));
                    AddReadOnlyVfcItem(card, "security-sync-scheduler-next", "Lịch chạy kế tiếp", "Số lịch chạy kế tiếp do scheduler trả về.", dashboard.NextScheduledRuns.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
                {
                    AddReadOnlyVfcItem(card, "security-sync-scheduler-error", "Scheduler dashboard", $"Không đọc được execution store ({ex.GetType().Name}).", "Lỗi provider");
                }
            }
            AddReadOnlyVfcItem(card, "security-sync-log-provider", "Sync log", "Chưa có repository riêng cho lịch sử payload đồng bộ.", "Chỉ đọc");
        }
        else
        {
            var export = _serviceProvider.GetService<IExportPipelineService>();
            if (export is null)
            {
                AddReadOnlyVfcItem(card, "security-export-provider", "Export log", "Chưa đăng ký export pipeline provider.", "Chưa sẵn sàng");
            }
            else
            {
                var executions = await export.GetExecutionsAsync(50);
                if (executions.Count == 0)
                {
                    AddReadOnlyVfcItem(card, "security-export-empty", "Lịch sử tác vụ export", "Cơ sở dữ liệu chưa có bản ghi lần chạy export.", "Chưa có dữ liệu");
                }
                else
                {
                    foreach (var execution in executions.Take(20))
                    {
                        AddReadOnlyVfcItem(card, $"security-export-{execution.Id}", execution.ExportCode, $"Đưa vào hàng đợi: {execution.QueuedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}", execution.Status);
                    }
                }
            }
            AddReadOnlyVfcItem(card, "security-export-action", "Phạm vi dữ liệu", "Trang này chỉ đọc lịch sử tác vụ export, không phải nhật ký kiểm toán và không tự tạo file export.", "Chỉ đọc");
        }

        detail.Cards.Add(card);
        return detail;
    }

    private static void AddAuditLogRows(SettingCardViewModel card, IReadOnlyList<AuditLog> records)
    {
        if (records.Count == 0)
        {
            AddReadOnlyVfcItem(card, "security-audit-empty", "Chưa có audit log", "AuditLogs chưa có bản ghi trong phạm vi hiện tại.", "0 bản ghi");
            return;
        }

        foreach (var record in records.Take(100))
        {
            var entity = string.Join("/", new[] { record.EntityName, record.EntityId }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var actor = string.IsNullOrWhiteSpace(record.Username) ? "system" : record.Username;
            var detail = string.IsNullOrWhiteSpace(record.Detail) ? record.Action : record.Detail;
            var address = string.IsNullOrWhiteSpace(record.IpAddress) ? "Không có IP" : record.IpAddress;
            AddReadOnlyVfcItem(
                card,
                $"security-audit-{record.Id}",
                string.IsNullOrWhiteSpace(entity) ? record.Action : entity,
                $"{record.Action} · {detail}",
                $"{record.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {actor} · {address}");
        }
    }

    private static void AddLoginAuditRows(SettingCardViewModel card, IReadOnlyList<LoginAuditDto> records)
    {
        if (records.Count == 0)
        {
            AddReadOnlyVfcItem(card, "security-login-empty", "Chưa có lịch sử đăng nhập", "LoginAudits chưa có bản ghi trong phạm vi hiện tại.", "0 bản ghi");
            return;
        }

        foreach (var record in records.Take(100))
        {
            var outcome = record.Success ? "Đăng nhập thành công" : "Đăng nhập thất bại";
            var reason = record.Success || string.IsNullOrWhiteSpace(record.FailureReason)
                ? outcome
                : $"{outcome} · {record.FailureReason}";
            var machine = string.IsNullOrWhiteSpace(record.MachineName) ? "Không có tên máy" : record.MachineName;
            var address = string.IsNullOrWhiteSpace(record.IpAddress) ? "Không có IP" : record.IpAddress;
            AddReadOnlyVfcItem(
                card,
                $"security-login-{record.CreatedAt.Ticks}-{record.UserName}",
                string.IsNullOrWhiteSpace(record.UserName) ? "Tài khoản không xác định" : record.UserName,
                $"{reason} · {machine} · {address}",
                record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    private async Task<SettingsDetailViewModel> BuildNetworkOperationalDetailAsync(string route, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Kết nối mạng > {row.Title}"
        };
        var card = new SettingCardViewModel("Cấu hình mạng", "Đọc giá trị đã lưu và kiểm tra trực tiếp bằng provider của Windows");

        if (string.Equals(route, "net-shared", StringComparison.OrdinalIgnoreCase))
        {
            AddReadOnlyVfcItem(
                card,
                "network-data-root",
                "Data root",
                FormatSettingsPathValue(_storageDataRootPath, _storageDataRootPathIsPersisted),
                FormatSettingsPathStatus(_storageDataRootPath, _storageDataRootPathIsPersisted),
                "system-folders");
            var uncPathStatuses = new List<string>();
            foreach (var key in new[] { "VFC.SystemDrivePath", "VFC.AccountingDrivePath", "VFC.SalesDrivePath", "VFC.HrDrivePath", "VFC.LogDrivePath" })
            {
                var value = await _settingsService.GetValueAsync(key, SettingScope.System, null);
                var configuredPath = value?.Value?.Trim() ?? string.Empty;
                AddReadOnlyVfcItem(
                    card,
                    $"network-{key}",
                    key,
                    FormatSettingsPathValue(configuredPath, value?.IsPersisted == true),
                    FormatSettingsPathStatus(configuredPath, value?.IsPersisted == true),
                    "vfc-mapping");
                if (value?.IsPersisted == true && configuredPath.StartsWith(@"\\", StringComparison.Ordinal))
                    uncPathStatuses.Add(DescribePhysicalPath(configuredPath));
            }
            var uncSummary = uncPathStatuses.Count == 0
                ? "Chưa có UNC đã lưu"
                : $"{uncPathStatuses.Count} UNC · {string.Join(", ", uncPathStatuses.Distinct(StringComparer.OrdinalIgnoreCase))}";
            AddReadOnlyVfcItem(
                card,
                "network-nas-provider",
                "NAS/SMB",
                "Chỉ kiểm tra khả năng truy cập cơ bản của đường dẫn UNC đã lưu; không kiểm tra quyền ghi và không lưu credential mạng.",
                uncSummary);
        }
        else if (string.Equals(route, "net-discovery", StringComparison.OrdinalIgnoreCase))
        {
            AddReadOnlyVfcItem(
                card,
                "network-discovery-provider",
                "LAN discovery provider",
                "Chưa có discovery/sync service nào tiêu thụ Network.AllowClientDiscovery hoặc Network.ScanIntervalSeconds. Giá trị legacy không được trình bày như cấu hình đang có hiệu lực.",
                "Chưa triển khai");
            AddReadOnlyVfcItem(
                card,
                "network-discovery-sessions",
                "Phiên máy trạm đang hoạt động",
                "Đếm từ ActiveCompanySessions; đây là phiên đăng nhập, không phải kết quả quét mạng.",
                ClientRegistryEntries.Count.ToString(CultureInfo.InvariantCulture),
                "settings-category:clients:clients-reg");

            var endpointCard = new SettingCardViewModel(
                "Endpoint đã cấu hình",
                "Probe trực tiếp các đích API/POS/FO đã cấu hình; không dò subnet hoặc tự phát hiện máy trong LAN.");
            var virtualExplorer = _serviceProvider.GetService<IVirtualExplorerService>();
            if (virtualExplorer is null)
            {
                AddReadOnlyVfcItem(
                    endpointCard,
                    "network-discovery-endpoints-provider",
                    "Network probe",
                    "Virtual File Center provider chưa được đăng ký; không thể xác nhận trạng thái endpoint.",
                    "Chưa sẵn sàng");
            }
            else
            {
                try
                {
                    var networkNodes = await virtualExplorer.GetChildrenAsync("Network");
                    foreach (var (displayName, title, targetPage) in new[]
                    {
                        ("API Server", "API Server", "network-api-server"),
                        ("POS Terminals", "POS terminals", "net-pos"),
                        ("FO Clients", "FO clients", "net-fo")
                    })
                    {
                        var endpoint = networkNodes.FirstOrDefault(x =>
                            string.Equals(x.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
                        AddReadOnlyVfcItem(
                            endpointCard,
                            $"network-discovery-endpoint-{displayName.Replace(' ', '-').ToLowerInvariant()}",
                            title,
                            endpoint?.Description ?? "Probe không trả về endpoint này; không suy diễn trạng thái.",
                            endpoint?.Status ?? "Chưa có dữ liệu",
                            targetPage);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not load configured network endpoint probes.");
                    AddReadOnlyVfcItem(
                        endpointCard,
                        "network-discovery-endpoints-error",
                        "Network probe",
                        "Không lấy được trạng thái runtime. Hãy mở từng endpoint để xem hoặc chỉnh cấu hình đã lưu.",
                        ex.GetType().Name);
                }
            }
            detail.Cards.Add(endpointCard);
        }
        else
        {
            var firewallService = ServiceStatuses.FirstOrDefault(x =>
                x.ServiceName.Equals("MpsSvc", StringComparison.OrdinalIgnoreCase)
                || x.DisplayName.Contains("Windows Defender Firewall", StringComparison.OrdinalIgnoreCase)
                || x.DisplayName.Contains("Windows Firewall", StringComparison.OrdinalIgnoreCase));
            if (firewallService is not null)
            {
                AddReadOnlyVfcItem(card, "network-firewall-service", firewallService.DisplayName, "Trạng thái đọc từ Windows Service Control Manager.", firewallService.Status);
            }
            else
            {
                AddReadOnlyVfcItem(card, "network-firewall-service-missing", "Firewall service", "Không đọc được service MpsSvc từ provider hiện tại.", "Chưa có dữ liệu");
            }

            var firewallPolicy = _serviceProvider.GetService<IWindowsFirewallService>();
            if (firewallPolicy is null)
            {
                AddReadOnlyVfcItem(card, "network-firewall-provider", "Firewall rules", "Chưa đăng ký Windows Firewall policy provider.", "Chưa sẵn sàng");
            }
            else
            {
                var snapshot = await firewallPolicy.GetSnapshotAsync();
                var ruleCount = snapshot.IsTruncated ? $">={snapshot.RuleCount}" : snapshot.RuleCount.ToString(CultureInfo.InvariantCulture);
                AddReadOnlyVfcItem(card, "network-firewall-provider", "Firewall provider", snapshot.ProviderStatus, snapshot.IsAvailable ? $"{ruleCount} rules" : "Không khả dụng");
                foreach (var rule in snapshot.Rules.Take(20))
                {
                    var target = string.IsNullOrWhiteSpace(rule.LocalPorts) ? rule.Protocol : rule.LocalPorts;
                    AddReadOnlyVfcItem(card, $"network-firewall-rule-{rule.Name}", rule.Name, $"{rule.Direction} · {rule.Action} · {target}", rule.IsEnabled ? "Enabled" : "Disabled");
                }
            }
            AddReadOnlyVfcItem(card, "network-firewall-scope", "Phạm vi an toàn", "Màn hình không tự mở port hoặc thay đổi firewall khi chưa có policy và quyền vận hành riêng.", "Chỉ đọc");
        }

        detail.Cards.Add(card);
        var actions = new SettingCardViewModel(
            "Thao tác cấu hình",
            string.Equals(route, "net-shared", StringComparison.OrdinalIgnoreCase)
                ? "Thay đổi data root hoặc mapping tại form có lưu thật; trạng thái truy cập bên trên chỉ là phép kiểm tra đọc."
                : string.Equals(route, "net-discovery", StringComparison.OrdinalIgnoreCase)
                    ? "LAN discovery chưa được triển khai. Mở từng endpoint để sửa cấu hình thật hoặc xem danh sách phiên máy trạm đang đăng nhập."
                : "Rule firewall là cấu hình của Windows; mở trang quản trị hệ điều hành để xem hoặc thay đổi.");
        if (string.Equals(route, "net-shared", StringComparison.OrdinalIgnoreCase))
        {
            actions.Items.Add(new SettingItemViewModel(
                "network-shared-edit-root", "\uE8B7", "Chỉnh data root", "Mở form cấu hình đường dẫn dữ liệu.", "Chỉnh sửa", targetPageKey: "system-folders"));
            actions.Items.Add(new SettingItemViewModel(
                "network-shared-edit-mapping", "\uE838", "Chỉnh mapping VFC", "Thay đổi các thư mục vật lý được ánh xạ vào ổ ảo.", "Chỉnh sửa", targetPageKey: "vfc-mapping"));
        }
        else if (string.Equals(route, "net-discovery", StringComparison.OrdinalIgnoreCase))
        {
            actions.Items.Add(new SettingItemViewModel(
                "network-discovery-open-sessions", "\uE8D7", "Xem phiên máy trạm", "Mở danh sách phiên client đang hoạt động từ database.", "Mở danh sách", targetPageKey: "settings-category:clients:clients-reg"));
            actions.Items.Add(new SettingItemViewModel(
                "network-discovery-open-api", "\uE968", "Cấu hình API endpoint", "Chỉnh URL/port/HTTPS dùng cho phép probe endpoint.", "Chỉnh endpoint", targetPageKey: "network-api-server"));
        }
        else
        {
            actions.Items.Add(new SettingItemViewModel(
                "network-firewall-open-windows", "\uE7BA", "Mở Windows Firewall", "Mở Windows Security để quản lý firewall; thay đổi rule có thể ảnh hưởng kết nối máy trạm.", "Mở Windows Security", targetPageKey: "windows-settings-firewall"));
        }
        detail.Cards.Add(actions);
        return detail;
    }

    private static string FormatSettingsValueSource(string? value, bool isPersisted)
        => isPersisted
            ? string.IsNullOrWhiteSpace(value) ? "Đã lưu giá trị trống" : "Đã lưu trong Settings Engine"
            : string.IsNullOrWhiteSpace(value) ? "Mặc định trống · chưa lưu" : "Mặc định · chưa lưu";

    private static string FormatEndpointForDisplay(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return "Chưa cấu hình";

        var candidate = endpoint.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
            candidate = $"https://{candidate}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "URL không hợp lệ · giá trị đã được ẩn";
        }

        var safeUri = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;
        return safeUri.GetLeftPart(UriPartial.Authority);
    }

    private async Task<SettingsDetailViewModel> BuildNetworkEndpointDetailAsync(string route, SettingItemViewModel row)
    {
        ShowDetailCardsWithEditableForm = true;
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Kết nối mạng > {row.Title}"
        };
        var (settingKey, nodeName) = route.ToLowerInvariant() switch
        {
            "net-pos" => ("POS.ApiEndpoint", "POS Terminals"),
            "net-fo" => ("FO.ApiEndpoint", "FO Clients"),
            _ => ("Network.ApiBaseUrl", "API endpoint probe")
        };
        var scope = settingKey.StartsWith("Network.", StringComparison.OrdinalIgnoreCase)
            ? SettingScope.System
            : SettingScope.App;
        var scopeTarget = scope == SettingScope.App ? ResolveAppSettingTarget(settingKey) : null;
        var endpointSetting = await _settingsService.GetValueAsync(settingKey, scope, scopeTarget);
        var settingsCard = new SettingCardViewModel(
            "Endpoint hiện hành",
            "Nguồn là Settings Engine; URL chỉ hiển thị host, không hiển thị userinfo, query hoặc token.");
        AddReadOnlyVfcItem(
            settingsCard,
            "network-endpoint-value",
            "Endpoint",
            FormatEndpointForDisplay(endpointSetting?.Value),
            FormatSettingsValueSource(endpointSetting?.Value, endpointSetting?.IsPersisted == true),
            route);

        if (settingKey == "Network.ApiBaseUrl")
        {
            foreach (var (key, label) in new[]
            {
                ("Network.ApiPort", "Cổng API"),
                ("Network.EnableHttps", "HTTPS")
            })
            {
                var setting = await _settingsService.GetValueAsync(key, SettingScope.System, null);
                AddReadOnlyVfcItem(
                    settingsCard,
                    $"network-setting-{key}",
                    label,
                    setting?.Value ?? "Chưa cấu hình",
                    FormatSettingsValueSource(setting?.Value, setting?.IsPersisted == true),
                    route);
            }
        }

        detail.Cards.Add(settingsCard);

        var runtimeCard = new SettingCardViewModel(
            "Kiểm tra runtime",
            "Kết quả lấy từ probe VFC; Settings Engine hiện không khởi động hoặc thay đổi cấu hình endpoint của service.");
        var virtualExplorer = _serviceProvider.GetService<IVirtualExplorerService>();
        if (virtualExplorer is null)
        {
            AddReadOnlyVfcItem(runtimeCard, "network-endpoint-provider", "Network probe", "Virtual Explorer provider chưa được đăng ký.", "Chưa sẵn sàng");
        }
        else
        {
            try
            {
                var nodes = await virtualExplorer.GetChildrenAsync("Network");
                var node = nodes.FirstOrDefault(x => string.Equals(x.DisplayName, nodeName, StringComparison.OrdinalIgnoreCase));
                if (node is null)
                {
                    AddReadOnlyVfcItem(runtimeCard, "network-endpoint-missing", nodeName, "Probe không trả về endpoint này; không suy diễn trạng thái.", "Chưa có dữ liệu");
                }
                else
                {
                    AddReadOnlyVfcItem(
                        runtimeCard,
                        $"network-endpoint-{node.Id}",
                        nodeName,
                        node.Description ?? "Probe không trả về mô tả.",
                        node.Status ?? "Không xác định");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not load network endpoint probe for {EndpointName}.", nodeName);
                AddReadOnlyVfcItem(runtimeCard, "network-endpoint-error", nodeName, "Không lấy được trạng thái runtime từ Network probe.", ex.GetType().Name);
            }
        }

        detail.Cards.Add(runtimeCard);
        var actions = new SettingCardViewModel(
            "Thao tác",
            "Cấu hình được lưu thật và dùng cho kiểm tra kết nối từ VFC; việc cài đặt hoặc khởi chạy dịch vụ API vẫn do deployment quản lý.");
        actions.Items.Add(new SettingItemViewModel(
            "network-endpoint-refresh",
            "\uE72C",
            "Kiểm tra endpoint lại",
            "Chạy lại probe runtime và tải giá trị endpoint hiện tại.",
            "Kiểm tra lại",
            targetPageKey: $"network-endpoint-refresh:{route}"));
        detail.Cards.Add(actions);
        return detail;
    }

    private static string DescribePhysicalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Chưa cấu hình";
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(path.Trim());
        var isUncPath = expandedPath.StartsWith(@"\\", StringComparison.Ordinal);
        if (!Path.IsPathFullyQualified(expandedPath))
        {
            return "Path không tuyệt đối";
        }

        try
        {
            if (!Directory.Exists(expandedPath))
            {
                return isUncPath ? "UNC/SMB không truy cập được" : "Không tồn tại/không truy cập được";
            }

            // Directory.Exists can be true even when the current identity
            // cannot enumerate the directory. Probe read/list access without
            // creating files; these settings do not imply write permission.
            _ = Directory.EnumerateFileSystemEntries(expandedPath).Take(1).ToArray();

            var root = Path.GetPathRoot(expandedPath);
            if (!isUncPath && !string.IsNullOrWhiteSpace(root))
            {
                try
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady)
                    {
                        return $"Đọc được nội dung · còn trống {FormatBytes(drive.AvailableFreeSpace)}";
                    }
                }
                catch (IOException)
                {
                    return "Đọc được nội dung · không đọc được dung lượng";
                }
                catch (UnauthorizedAccessException)
                {
                    return "Đọc được nội dung · bị từ chối quyền đọc dung lượng";
                }
            }

            return isUncPath ? "UNC/SMB đọc được nội dung" : "Đọc được nội dung";
        }
        catch (UnauthorizedAccessException)
        {
            return isUncPath ? "UNC/SMB bị từ chối quyền đọc" : "Có path nhưng bị từ chối quyền đọc";
        }
        catch (IOException)
        {
            return isUncPath ? "UNC/SMB không đọc được nội dung" : "Không đọc được nội dung thư mục";
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return "Lỗi kiểm tra path";
        }
    }

    private static string FormatSettingsPathValue(string path, bool isPersisted)
    {
        if (string.IsNullOrWhiteSpace(path))
            return isPersisted ? "Đã lưu giá trị trống" : "Mặc định trống · chưa lưu";

        return isPersisted ? path : $"{path} · mặc định chưa lưu";
    }

    private static string FormatSettingsPathStatus(string path, bool isPersisted)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Chưa cấu hình đường dẫn";

        var pathStatus = DescribePhysicalPath(path);
        return isPersisted ? pathStatus : $"Mặc định chưa lưu · {pathStatus}";
    }

    private static string FormatSqlBackupPathStatus(string path, bool isPersisted)
    {
        var appPathStatus = FormatSettingsPathStatus(path, isPersisted);
        if (string.IsNullOrWhiteSpace(path))
        {
            return appPathStatus;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(path.Trim());
        var sqlAccessHint = expandedPath.StartsWith(@"\\", StringComparison.Ordinal)
            ? "Cần xác nhận quyền SQL Server: tài khoản dịch vụ phải ghi được vào share này."
            : "Kiểm tra đọc chỉ phản ánh quyền của ứng dụng; cần chạy backup thật để xác nhận quyền SQL Server ghi file.";
        return $"Ứng dụng: {appPathStatus} · {sqlAccessHint}";
    }

    private static string FormatBackupScheduleStatus(bool hasProvider, BackgroundSchedulerDefinitionDto? schedule)
    {
        if (!hasProvider)
        {
            return "Scheduler không khả dụng";
        }

        if (schedule is null)
        {
            return "Chưa có job backup";
        }

        if (schedule.IsPaused)
        {
            return "Tạm dừng";
        }

        if (!schedule.IsEnabled)
        {
            return "Đang tắt";
        }

        if (!schedule.NextRunAtUtc.HasValue)
        {
            return "Đã bật · chưa có lần chạy kế tiếp";
        }

        return $"Đang bật · lần tới {schedule.NextRunAtUtc.Value.ToLocalTime():dd/MM HH:mm}";
    }

    private async Task<SettingCardViewModel> BuildIntegrityCardAsync(string title, string subtitle)
    {
        var issues = await _dataIntegrityService.ScanAsync();
        var card = new SettingCardViewModel(title, subtitle);
        if (issues.Count == 0)
        {
            AddReadOnlyVfcItem(card, "integrity-empty", "Chưa có kết quả scan", "Database chưa có bản ghi scan toàn vẹn dữ liệu.", "Chưa có dữ liệu");
        }
        else
        {
            foreach (var issue in issues.Take(20))
            {
                var status = issue.Id == Guid.Empty ? "Info" : issue.Severity;
                AddReadOnlyVfcItem(card, $"integrity-{issue.Id}", issue.Module, issue.Message, status);
            }
        }

        return card;
    }

    private async Task<SettingsDetailViewModel> BuildSystemRemoteDetailAsync(SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Hệ thống > {row.Title}"
        };
        var endpoint = new SettingCardViewModel(
            "Endpoint ứng dụng đang dùng để probe",
            "Các giá trị này chỉ cấu hình URL mà VFC kiểm tra; chúng không khởi chạy hoặc mở cổng API trên máy chủ.");
        var endpointUrl = await _settingsService.GetValueAsync("Network.ApiBaseUrl", SettingScope.System, null);
        var endpointPort = await _settingsService.GetValueAsync("Network.ApiPort", SettingScope.System, null);
        var endpointHttps = await _settingsService.GetValueAsync("Network.EnableHttps", SettingScope.System, null);
        AddReadOnlyVfcItem(endpoint, "system-remote-endpoint", "API endpoint", FormatEndpointForDisplay(endpointUrl?.Value), FormatSettingsValueSource(endpointUrl?.Value, endpointUrl?.IsPersisted == true), "network-api-server");
        AddReadOnlyVfcItem(endpoint, "system-remote-port", "Cổng đã cấu hình", endpointPort?.Value ?? "Chưa cấu hình", FormatSettingsValueSource(endpointPort?.Value, endpointPort?.IsPersisted == true), "network-api-server");
        AddReadOnlyVfcItem(endpoint, "system-remote-https", "HTTPS", endpointHttps?.Value ?? "Chưa cấu hình", FormatSettingsValueSource(endpointHttps?.Value, endpointHttps?.IsPersisted == true), "network-api-server");
        detail.Cards.Add(endpoint);

        var firewall = new SettingCardViewModel("Windows Firewall", "Chính sách và trạng thái được quản lý bởi Windows, không phải cờ cấu hình riêng của ASMILE.");
        var firewallService = ServiceStatuses.FirstOrDefault(x =>
            x.ServiceName.Equals("MpsSvc", StringComparison.OrdinalIgnoreCase)
            || x.DisplayName.Contains("Windows Defender Firewall", StringComparison.OrdinalIgnoreCase)
            || x.DisplayName.Contains("Windows Firewall", StringComparison.OrdinalIgnoreCase));
        AddReadOnlyVfcItem(
            firewall,
            "system-remote-firewall-service",
            "Firewall service",
            firewallService?.DisplayName ?? "Không đọc được Windows Firewall service.",
            firewallService?.Status ?? "Chưa có dữ liệu",
            "windows-settings-firewall");

        var firewallProvider = _serviceProvider.GetService<IWindowsFirewallService>();
        if (firewallProvider is null)
        {
            AddReadOnlyVfcItem(firewall, "system-remote-firewall-provider", "Firewall rules", "Provider đọc Windows Firewall chưa được đăng ký.", "Chưa sẵn sàng");
        }
        else
        {
            var snapshot = await firewallProvider.GetSnapshotAsync();
            var count = snapshot.IsTruncated ? $">={snapshot.RuleCount}" : snapshot.RuleCount.ToString(CultureInfo.InvariantCulture);
            AddReadOnlyVfcItem(firewall, "system-remote-firewall-rules", "Firewall rules", snapshot.ProviderStatus, snapshot.IsAvailable ? $"{count} rules" : "Không khả dụng", "windows-settings-firewall");
        }
        detail.Cards.Add(firewall);

        var actions = new SettingCardViewModel("Nơi chỉnh cấu hình", "Mở đúng provider sở hữu cấu hình để tránh lưu một giá trị không có tác dụng runtime.");
        actions.Items.Add(new SettingItemViewModel(
            "system-remote-edit-endpoint",
            "\uE968",
            "Chỉnh endpoint probe",
            "Lưu endpoint dùng bởi phép kiểm tra kết nối VFC.",
            "Mở editor",
            targetPageKey: "network-api-server"));
        actions.Items.Add(new SettingItemViewModel(
            "system-remote-open-firewall",
            "\uE7BA",
            "Quản lý firewall",
            "Mở Windows Security để thay đổi policy firewall có hiệu lực trên máy.",
            "Mở Windows Security",
            targetPageKey: "windows-settings-firewall"));
        detail.Cards.Add(actions);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildSystemOperationalDetailAsync(string route, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Hệ thống > {row.Title}"
        };
        var connection = await _databaseAdminService.GetPrimaryConnectionAsync();

        if (string.Equals(route, "system-server", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Máy chủ runtime", "Giá trị lấy từ Windows runtime và database connection thật");
            AddReadOnlyVfcItem(card, "system-machine", "Machine", Environment.MachineName, Environment.UserName, "windows-settings-about");
            AddReadOnlyVfcItem(card, "system-os", "Operating system", RuntimeInformation.OSDescription, Environment.OSVersion.VersionString, "windows-settings-about");
            AddReadOnlyVfcItem(card, "system-timezone", "Múi giờ", TimeZoneInfo.Local.Id, TimeZoneInfo.Local.DisplayName, "windows-settings-date-time");
            AddReadOnlyVfcItem(card, "system-database", "Database", connection.DatabaseName, connection.Status, "database-sql-connection");
            detail.Cards.Add(card);

            var actions = new SettingCardViewModel("Thay đổi cấu hình", "Thông tin máy/Windows là dữ liệu runtime; mở đúng nơi quản lý để chỉnh sửa.");
            actions.Items.Add(new SettingItemViewModel(
                "system-open-about", "\uE8A5", "Tên máy & thông tin Windows", "Mở Windows Settings > System > About. Đổi tên máy có thể cần khởi động lại Windows.", "Mở Windows Settings", targetPageKey: "windows-settings-about"));
            actions.Items.Add(new SettingItemViewModel(
                "system-open-timezone", "\uE787", "Múi giờ Windows", "Mở Windows Settings > Time & language > Date & time.", "Mở Windows Settings", targetPageKey: "windows-settings-date-time"));
            actions.Items.Add(new SettingItemViewModel(
                "system-open-database", "\uE9D2", "Cấu hình kết nối cơ sở dữ liệu", "Chỉnh và kiểm tra cấu hình SQL Server dùng bởi ứng dụng.", "Mở cấu hình", targetPageKey: "database-sql-connection"));
            detail.Cards.Add(actions);
            return detail;
        }

        var health = _serviceProvider.GetService<IInfrastructureHealthService>();
        var performance = new SettingCardViewModel("Runtime health", "Không dùng số liệu hiệu năng mẫu");
        AddReadOnlyVfcItem(performance, "system-uptime", "Process uptime", "Thời gian process đang chạy.", FormatDuration(TimeSpan.FromMilliseconds(Environment.TickCount64)));
        AddReadOnlyVfcItem(performance, "system-memory", "GC memory limit", "Giới hạn bộ nhớ runtime do .NET cung cấp.", FormatBytes(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
        AddReadOnlyVfcItem(performance, "system-services", "Windows services", "Số service được đọc từ ServiceController provider.", $"{ServiceStatuses.Count}", "windows-settings-services");
        if (health is not null)
        {
            var report = await health.CheckAsync();
            AddReadOnlyVfcItem(performance, "system-health", "Infrastructure status", "Kết quả health check hiện tại.", report.Status);
            foreach (var check in report.Checks.Take(10))
            {
                AddReadOnlyVfcItem(performance, $"system-check-{check.Name}", check.Name, check.Message, check.Status);
            }
        }
        detail.Cards.Add(performance);

        var configuration = new SettingCardViewModel(
            "Cấu hình liên quan",
            "Các số liệu runtime bên trên chỉ đọc; mở mục tương ứng để thay đổi cấu hình đang được hỗ trợ.");
        configuration.Items.Add(new SettingItemViewModel(
            "system-performance-storage",
            "\uE7F1",
            "Lưu trữ & ổ đĩa ảo",
            "Chỉnh data root và trạng thái các ổ logic VFC.",
            "Mở cấu hình",
            targetPageKey: "settings-category:system:system-storage"));
        configuration.Items.Add(new SettingItemViewModel(
            "system-performance-backup",
            "\uE7B8",
            "Lịch backup",
            "Chỉnh lịch, thư mục đích và retention của backup database.",
            "Mở cấu hình",
            targetPageKey: "settings-category:backup:backup-schedule"));
        configuration.Items.Add(new SettingItemViewModel(
            "system-performance-vfc",
            "\uE8B7",
            "Chính sách lưu trữ VFC",
            "Quản lý policy lưu trữ dữ liệu đã được lưu trong database.",
            "Mở cấu hình",
            targetPageKey: "settings-category:vfc:vfc-retention"));
        detail.Cards.Add(configuration);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildSystemModeDetailAsync(SettingItemViewModel row)
    {
        var maintenanceValue = await _settingsService.GetValueAsync("System.MaintenanceMode", SettingScope.System, null);
        var isMaintenanceMode = bool.TryParse(maintenanceValue?.Value, out var enabled) && enabled;
        var canManageMaintenance = _authorizationService.HasPermission(Permission.ManageServices);
        return BuildSystemModeDetail(row, isMaintenanceMode, canManageMaintenance);
    }

    private static SettingsDetailViewModel BuildSystemModeDetail(
        SettingItemViewModel row,
        bool isMaintenanceMode,
        bool canManageMaintenance)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Hệ thống > {row.Title}"
        };
        var card = new SettingCardViewModel(
            "Vai trò triển khai",
            "Maintenance mode chặn đăng nhập mới; các phiên đang hoạt động cần được đăng xuất riêng trước khi bảo trì.");
        AddReadOnlyVfcItem(
            card,
            "system-mode-deployment",
            "Máy chủ trung tâm / máy trạm",
            "Không suy đoán vai trò từ tên máy hoặc cấu hình cục bộ.",
            "Chưa được triển khai");
        var maintenance = new SettingItemViewModel(
            "system-mode-maintenance",
            "\uE72E",
            "Maintenance mode",
            canManageMaintenance
                ? "Chặn hoặc mở đăng nhập mới cho người không có quyền System.Maintenance. Phiên hiện tại không bị ngắt."
                : "Chỉ người có quyền System.Maintenance mới được thay đổi trạng thái.",
            isMaintenanceMode ? "Đang bật" : "Đang tắt",
            isMaintenanceMode ? "Bảo trì" : "Bình thường",
            "system-mode-maintenance-toggle")
        {
            IsToggle = true,
            ToggleValue = isMaintenanceMode,
            IsClickable = canManageMaintenance,
            IsReadOnly = !canManageMaintenance,
            RequiredPermission = Permission.ManageServices
        };
        card.Items.Add(maintenance);
        detail.Cards.Add(card);
        return detail;
    }

    private async Task ToggleSystemMaintenanceModeAsync()
    {
        if (!_authorizationService.HasPermission(Permission.ManageServices))
        {
            _notificationService.Show("Bạn không có quyền bật hoặc tắt maintenance mode.");
            return;
        }

        try
        {
            var current = await _settingsService.GetValueAsync("System.MaintenanceMode", SettingScope.System, null);
            var currentlyEnabled = bool.TryParse(current?.Value, out var enabled) && enabled;
            var nextEnabled = !currentlyEnabled;
            var confirmed = await _authorizationService.ConfirmDangerousActionAsync(
                nextEnabled ? "Bật maintenance mode" : "Tắt maintenance mode",
                nextEnabled
                    ? "Đăng nhập mới bị chặn; phiên hiện tại không có quyền bảo trì sẽ về màn hình đăng nhập trong tối đa 30 giây. Hãy lưu công việc trước khi tiếp tục."
                    : "Cho phép người dùng đăng nhập mới trở lại? Tiếp tục?");
            if (!confirmed)
            {
                return;
            }

            await _settingsService.SaveValueAsync(new SaveSettingValueRequest
            {
                Key = "System.MaintenanceMode",
                Scope = SettingScope.System,
                Value = nextEnabled ? "true" : "false",
                Reason = nextEnabled ? "Maintenance mode enabled" : "Maintenance mode disabled"
            });

            if (CurrentPage?.Rows.FirstOrDefault(x => x.Id == "system-mode") is { } modeRow)
            {
                CurrentDetail = await BuildSystemModeDetailAsync(modeRow);
            }
            _notificationService.Show(nextEnabled
                ? "Maintenance mode đã bật; đăng nhập mới bị giới hạn theo quyền bảo trì."
                : "Maintenance mode đã tắt; đăng nhập mới được mở lại.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change system maintenance mode.");
            _notificationService.Show("Không thể thay đổi maintenance mode. Kiểm tra quyền và nhật ký hệ thống.");
        }
    }

    private async Task<SettingsDetailViewModel> BuildAppsInfrastructureDetailAsync(SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Ứng dụng > {row.Title}"
        };

        if (string.Equals(row.Id, "apps-permissions", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Quyền ứng dụng", "Role và permission đọc từ store bảo mật thật");
            foreach (var role in SecurityRoles)
            {
                var count = _rolePermissionCodes.TryGetValue(role.Id, out var codes) ? codes.Count : 0;
                AddReadOnlyVfcItem(card, $"app-permission-{role.Id}", role.Name, role.Description ?? "", $"{count} quyền");
            }
            if (SecurityRoles.Count == 0)
            {
                AddReadOnlyVfcItem(card, "app-permissions-empty", "Chưa có role", "Không dựng permission matrix khi database chưa trả về role.", "Chưa có dữ liệu");
            }
            detail.Cards.Add(card);
            var permissionActions = new SettingCardViewModel("Chỉnh quyền", "Danh sách quyền là dữ liệu đã lưu; mở ma trận phân quyền để thay đổi role và quyền.");
            permissionActions.Items.Add(new SettingItemViewModel(
                "app-permissions-open-matrix", "\uE8D9", "Mở ma trận phân quyền", "Chỉnh quyền theo role trong nhóm Người dùng & phân quyền.", "Quản lý quyền", targetPageKey: "settings-category:users:users-matrix"));
            detail.Cards.Add(permissionActions);
            return detail;
        }

        if (string.Equals(row.Id, "apps-paths", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Đường dẫn ứng dụng", "Chỉ hiển thị path đã lưu; không tự ghép thư mục ảo");
            AddReadOnlyVfcItem(
                card,
                "app-data-root",
                "Data root",
                FormatSettingsPathValue(_storageDataRootPath, _storageDataRootPathIsPersisted),
                FormatSettingsPathStatus(_storageDataRootPath, _storageDataRootPathIsPersisted),
                "system-folders");
            var exportRootSetting = await _settingsService.GetValueAsync("Storage.ExportRootPath", SettingScope.System, null);
            var exportRoot = exportRootSetting?.Value?.Trim() ?? string.Empty;
            AddReadOnlyVfcItem(
                card,
                "app-export-root",
                "Export root hệ thống",
                FormatSettingsPathValue(exportRoot, exportRootSetting?.IsPersisted == true),
                FormatSettingsPathStatus(exportRoot, exportRootSetting?.IsPersisted == true),
                "system-folders");
            foreach (var key in new[]
            {
                "Backup.BackupPath",
                "VFC.SystemDrivePath",
                "VFC.AccountingDrivePath",
                "VFC.SalesDrivePath",
                "VFC.HrDrivePath",
                "VFC.LogDrivePath"
            })
            {
                var setting = await _settingsService.GetValueAsync(key, SettingScope.System, null);
                var configuredPath = setting?.Value?.Trim() ?? string.Empty;
                var isPersisted = setting?.IsPersisted == true;
                AddReadOnlyVfcItem(
                    card,
                    $"app-path-{key}",
                    key,
                    FormatSettingsPathValue(configuredPath, isPersisted),
                    string.Equals(key, "Backup.BackupPath", StringComparison.OrdinalIgnoreCase)
                        ? FormatSqlBackupPathStatus(configuredPath, isPersisted)
                        : FormatSettingsPathStatus(configuredPath, isPersisted),
                    string.Equals(key, "Backup.BackupPath", StringComparison.OrdinalIgnoreCase)
                        ? "backup-schedule"
                        : "vfc-mapping");
            }
            AddReadOnlyVfcItem(card, "app-path-provider", "Template mapping theo module", "Chưa có provider persistence riêng cho mapping từng module; đây không phải export root chung.", "Chưa hỗ trợ");
            detail.Cards.Add(card);
            var pathActions = new SettingCardViewModel("Chỉnh đường dẫn", "Các đường dẫn hệ thống và VFC có editor lưu thật; mapping template riêng theo module chưa được triển khai.");
            pathActions.Items.Add(new SettingItemViewModel(
                "app-path-edit-root", "\uE8B7", "Chỉnh data root", "Mở form đường dẫn dữ liệu hệ thống.", "Chỉnh sửa", targetPageKey: "system-folders"));
            pathActions.Items.Add(new SettingItemViewModel(
                "app-path-edit-backup", "\uE7B8", "Chỉnh thư mục backup", "Mở lịch backup để thay đổi thư mục đích và policy.", "Chỉnh sửa", targetPageKey: "backup-schedule"));
            pathActions.Items.Add(new SettingItemViewModel(
                "app-path-edit-vfc", "\uE8B7", "Chỉnh mapping VFC", "Mở form mapping các ổ ảo tới thư mục vật lý.", "Chỉnh sửa", targetPageKey: "vfc-mapping"));
            detail.Cards.Add(pathActions);
            return detail;
        }

        if (string.Equals(row.Id, "apps-default", StringComparison.OrdinalIgnoreCase))
        {
            var windowsDefaultsCard = new SettingCardViewModel(
                "Liên kết ứng dụng mặc định của Windows",
                "Windows quản lý ứng dụng mặc định theo loại tệp và giao thức; ASMILE không ghi registry association thay cho hệ điều hành.");
            windowsDefaultsCard.Items.Add(new SettingItemViewModel(
                "apps-default-open-windows",
                "\uE8A7",
                "Mở Default apps của Windows",
                "Xem hoặc thay đổi ứng dụng mặc định trong trang Settings chính thức của Windows.",
                "Mở",
                targetPageKey: "apps-default-open-windows"));
            detail.Cards.Add(windowsDefaultsCard);

            var card = new SettingCardViewModel("Module ASMILE khả dụng trên desktop", "Danh sách module nội bộ đang bật và được phép hiển thị trên desktop; đây không phải liên kết ứng dụng mặc định của Windows.");
            var desktopApps = InstalledApps
                .Where(x => x.IsEnabled && x.ShowOnDesktop)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var runningProductVersion = GetRunningProductVersion();
            AddReadOnlyVfcItem(card, "app-default-count", "Ứng dụng khả dụng", "Số module đang bật và được phép hiển thị trên desktop.", $"{desktopApps.Count}");
            foreach (var app in desktopApps.Take(50))
            {
                var item = new SettingItemViewModel(
                    $"app-default-{app.AppKey}", "\uE71D", app.Name,
                    $"{app.Description ?? app.AppKey} Phiên bản sản phẩm đang chạy: {runningProductVersion}.",
                    "Mở cấu hình", targetPageKey: $"app-advanced:{app.AppKey}");
                card.Items.Add(item);
            }
            if (desktopApps.Count == 0)
            {
                AddReadOnlyVfcItem(card, "app-default-empty", "Chưa có ứng dụng mặc định", "Catalog AppModules không trả về module đang bật và hiển thị trên desktop.", "Chưa có dữ liệu");
            }
            detail.Cards.Add(card);
            return detail;
        }

        if (string.Equals(row.Id, "apps-shell-startup", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel(
                "Module mở cùng ASMILE BO",
                "Lựa chọn được lưu theo workspace/người dùng; chúng chỉ mở module bên trong shell, không thay đổi Windows Startup.");
            var workspaceService = _serviceProvider.GetService<IWorkspaceService>();
            if (workspaceService is null)
            {
                AddReadOnlyVfcItem(card, "apps-shell-startup-provider", "Workspace", "Chưa đăng ký dịch vụ lưu lựa chọn module khởi động.", "Chưa sẵn sàng");
            }
            else
            {
                var state = await workspaceService.LoadWorkspaceAsync();
                var startupKeys = new HashSet<string>(state.StartupModules, StringComparer.OrdinalIgnoreCase);
                var shellModuleKeys = new HashSet<string>(
                    _navigationService.GetModuleCatalog().Select(x => x.Key),
                    StringComparer.OrdinalIgnoreCase);
                var availableApps = InstalledApps
                    .Where(x => x.IsEnabled
                        && shellModuleKeys.Contains(x.AppKey)
                        && !string.Equals(x.AppKey, "settings", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var app in availableApps)
                {
                    var isSelected = startupKeys.Contains(app.AppKey);
                    card.Items.Add(new SettingItemViewModel(
                        $"apps-shell-startup-{app.AppKey}",
                        "\uE71D",
                        app.Name,
                        app.Description ?? app.AppKey,
                        isSelected ? "Mở cùng shell" : "Không tự mở",
                        targetPageKey: $"apps-shell-startup-toggle:{app.AppKey}")
                    {
                        IsToggle = true,
                        ToggleValue = isSelected
                    });
                }

                if (availableApps.Count == 0)
                {
                    AddReadOnlyVfcItem(card, "apps-shell-startup-empty", "Chưa có module khả dụng", "Catalog AppModules chưa có module đang bật để khởi chạy trong shell.", "Chưa có dữ liệu");
                }
            }

            detail.Cards.Add(card);
            return detail;
        }

        if (string.Equals(row.Id, "apps-startup-windows", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Id, "apps-startup", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Ứng dụng khởi động cùng Windows", "Các entry hiện diện trong Registry và Startup folder của PC này; trạng thái bật/tắt hiệu lực do Windows quản lý.");
            card.Items.Add(new SettingItemViewModel(
                "apps-startup-open-windows",
                "\uE7E8",
                "Mở Startup apps của Windows",
                "Xem trạng thái bật/tắt thực tế và quản lý ứng dụng khởi động trong Windows Settings.",
                "Mở",
                targetPageKey: "apps-startup-open-windows"));
            var startupService = _serviceProvider.GetService<IWindowsStartupAppsService>();
            if (startupService is null)
            {
                AddReadOnlyVfcItem(card, "app-startup-provider", "Windows startup provider", "Chưa đăng ký provider đọc startup entries.", "Chưa sẵn sàng");
            }
            else
            {
                var snapshot = await startupService.GetSnapshotAsync();
                AddReadOnlyVfcItem(card, "app-startup-provider", "Windows startup provider", snapshot.ProviderStatus, snapshot.IsAvailable ? $"{snapshot.Entries.Count} entries" : "Không khả dụng");
                foreach (var entry in snapshot.Entries.Take(50))
                {
                    AddReadOnlyVfcItem(card, $"app-startup-{entry.Source}-{entry.Name}", entry.Name, entry.Command, $"Entry hiện diện · {entry.Source}");
                }
                if (snapshot.Entries.Count == 0)
                {
                    AddReadOnlyVfcItem(card, "app-startup-empty", "Không có startup entry", "Windows không trả về ứng dụng tự khởi động trong các nguồn đã kiểm tra.", "Chưa có dữ liệu");
                }
            }
            detail.Cards.Add(card);
            return detail;
        }

        var status = new SettingCardViewModel("Nguồn dữ liệu module", "Danh sách module lấy từ AppModules persisted");
        AddReadOnlyVfcItem(status, "app-module-count", "Installed modules", "Số module đọc từ IAppModuleService.", $"{InstalledApps.Count}");
        AddReadOnlyVfcItem(status, "app-feature-provider", row.Title, "Schema cấu hình module được quản lý qua ModuleSettingsService/Settings Engine.", "Chỉ đọc");
        detail.Cards.Add(status);
        var actions = new SettingCardViewModel("Thao tác", "Mở danh mục module để chỉnh thông tin được hỗ trợ và lưu vào AppModules.");
        actions.Items.Add(new SettingItemViewModel(
            "app-advanced-open-installed", "\uE719", "Quản lý ứng dụng đã cài", "Chọn module để mở cấu hình nâng cao.", "Mở danh sách", targetPageKey: "apps-installed"));
        detail.Cards.Add(actions);
        return detail;
    }

    private async Task ToggleShellStartupModuleAsync(string moduleKey)
    {
        moduleKey = moduleKey.Trim();
        var app = InstalledApps.FirstOrDefault(x =>
            x.IsEnabled
            && string.Equals(x.AppKey, moduleKey, StringComparison.OrdinalIgnoreCase)
            && _navigationService.GetModuleCatalog().Any(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase)));
        var workspaceService = _serviceProvider.GetService<IWorkspaceService>();
        if (app is null || workspaceService is null)
        {
            _notificationService.Show("Không tìm thấy module hoặc dịch vụ lưu workspace.");
            return;
        }

        try
        {
            var state = await workspaceService.LoadWorkspaceAsync();
            var startupModules = state.StartupModules.ToList();
            var existingIndex = startupModules.FindIndex(x => string.Equals(x, app.AppKey, StringComparison.OrdinalIgnoreCase));
            var willStart = existingIndex < 0;
            if (willStart)
            {
                startupModules.Add(app.AppKey);
            }
            else
            {
                startupModules.RemoveAt(existingIndex);
            }

            await workspaceService.SaveStartupModulesAsync(startupModules);
            var startupRow = CurrentPage?.Rows.FirstOrDefault(x => string.Equals(x.Id, "apps-shell-startup", StringComparison.OrdinalIgnoreCase));
            if (startupRow is not null)
            {
                CurrentDetail = await BuildAppsInfrastructureDetailAsync(startupRow);
                IsDetailMode = true;
                SetBreadcrumbFromDetail(CurrentDetail);
                RaisePageFlags();
            }

            _notificationService.Show(willStart
                ? $"{app.Name} sẽ mở cùng ASMILE BO từ lần khởi động tiếp theo."
                : $"Đã bỏ {app.Name} khỏi danh sách mở cùng ASMILE BO.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update startup module preference for {ModuleKey}.", app.AppKey);
            _notificationService.Show("Không lưu được lựa chọn module khởi động. Kiểm tra quyền và kết nối dữ liệu.");
        }
    }

    private async Task<SettingsDetailViewModel> BuildMaintenanceDetailAsync(string route, SettingItemViewModel row)
    {
        if (string.Equals(route, "maint-cache-run", StringComparison.OrdinalIgnoreCase))
        {
            var workspace = _serviceProvider.GetService<IWorkspaceService>();
            if (workspace is null)
            {
                throw new InvalidOperationException("Workspace cache provider is not registered.");
            }

            await workspace.ClearWorkspaceCacheAsync();
            _notificationService.Show("Đã xóa workspace cache của người dùng hiện tại.");
            route = "maint-cache";
        }

        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Cập nhật & bảo trì > {row.Title}"
        };
        var isUpdateCheck = string.Equals(route, "maint-check", StringComparison.OrdinalIgnoreCase);
        var card = isUpdateCheck
            ? new SettingCardViewModel(
                "Kiểm tra bản cập nhật",
                "Xác minh manifest đã ký; nếu có bản mới và đủ quyền, có thể tải gói vào staging sau khi kiểm tra SHA-256. Chưa cài đặt hoặc rollback gói.")
            : new SettingCardViewModel("Bảo trì runtime", "Trạng thái thật và provider hiện có");
        if (string.Equals(route, "maint-version", StringComparison.OrdinalIgnoreCase))
        {
            AddReadOnlyVfcItem(card, "maint-version-value", "Phiên bản", Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Chưa xác định", "Entry assembly");
            AddReadOnlyVfcItem(card, "maint-runtime", ".NET runtime", RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture.ToString());
        }
        else if (string.Equals(route, "maint-cache", StringComparison.OrdinalIgnoreCase))
        {
            card.Items.Add(new SettingItemViewModel("maint-cache-run-action", "\uE74D", "Xóa workspace cache", "Xóa preference cache của user hiện tại qua WorkspaceService.", "Thực hiện", targetPageKey: "maint-cache-run"));
        }
        else if (string.Equals(route, "maint-tools", StringComparison.OrdinalIgnoreCase))
        {
            var health = _serviceProvider.GetService<IInfrastructureHealthService>();
            if (health is not null)
            {
                var report = await health.CheckAsync();
                AddReadOnlyVfcItem(card, "maint-health", "Infrastructure health", "Kết quả provider health hiện tại.", report.Status);
            }
            AddReadOnlyVfcItem(card, "maint-tools-provider", "Tools provider", "Các thao tác bảo trì phá hủy phải đi qua service và quyền riêng; chưa bật command tổng quát.", "Chỉ đọc");
        }
        else if (string.Equals(route, "maint-index", StringComparison.OrdinalIgnoreCase))
        {
            AddReadOnlyVfcItem(
                card,
                "maint-index-mode",
                "Virtual File Center index",
                "Cây tài liệu được dựng on-demand từ filesystem và dữ liệu nghiệp vụ; không có chỉ mục nền riêng để báo tiến độ giả.",
                "On-demand");
            card.Items.Add(new SettingItemViewModel(
                "maint-index-open-vfc",
                "\uE81E",
                "Xem trạng thái chỉ mục VFC",
                "Mở trang đọc trạng thái từ VirtualExplorerService và kiểm tra tính toàn vẹn hiện tại.",
                "Xem chi tiết",
                targetPageKey: "settings-category:vfc:vfc-index"));
            card.Items.Add(new SettingItemViewModel(
                "maint-index-rebuild-vfc",
                "\uE895",
                "Làm mới cây VFC",
                "Chạy thao tác làm mới cache VFC hiện có; không tạo hoặc xóa dữ liệu nghiệp vụ.",
                "Mở thao tác",
                targetPageKey: "settings-category:vfc:vfc-rebuild"));
        }
        else if (string.Equals(route, "maint-repair", StringComparison.OrdinalIgnoreCase))
        {
            AddReadOnlyVfcItem(
                card,
                "maint-repair-scope",
                "Phạm vi sửa liên kết",
                "VFC chỉ tạo yêu cầu vào DataRepairOperations; không tự sửa hoặc xóa tệp khi chưa có worker xử lý.",
                "Có kiểm soát");
            card.Items.Add(new SettingItemViewModel(
                "maint-repair-open-vfc",
                "\uEA39",
                "Xem và tạo yêu cầu repair VFC",
                "Mở quy trình repair có ghi hàng đợi và trạng thái thật trong cơ sở dữ liệu.",
                "Mở quy trình",
                targetPageKey: "settings-category:vfc:vfc-repair"));
        }
        else if (string.Equals(route, "maint-check", StringComparison.OrdinalIgnoreCase))
        {
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);
            var channelSetting = await _settingsService.GetValueAsync("Maintenance.UpdateChannel", SettingScope.System, null);
            var channel = string.IsNullOrWhiteSpace(channelSetting?.Value) ? "Stable" : channelSetting.Value.Trim();
            var updateService = _serviceProvider.GetService<IUpdateManifestService>();
            if (updateService is null)
            {
                AddReadOnlyVfcItem(card, "maint-update-provider", "Signed update provider", "Chưa đăng ký provider kiểm tra update manifest.", "Chưa sẵn sàng");
            }
            else
            {
                var result = await updateService.CheckAsync(channel, currentVersion);
                AddReadOnlyVfcItem(card, "maint-update-provider", "Signed update provider", result.Detail ?? "Provider đã trả về kết quả.", result.Status);
                AddReadOnlyVfcItem(card, "maint-update-current", "Phiên bản hiện tại", result.CurrentVersion, $"Channel: {channel}");
                if (!string.IsNullOrWhiteSpace(result.LatestVersion))
                {
                    AddReadOnlyVfcItem(card, "maint-update-latest", "Phiên bản trong manifest", result.LatestVersion, result.IsUpdateAvailable ? "Có bản mới" : "Đã cập nhật");
                }
                if (!string.IsNullOrWhiteSpace(result.PackageUri))
                {
                    AddReadOnlyVfcItem(
                        card,
                        "maint-update-package",
                        "Nguồn gói phát hành",
                        "URI do manifest ký cung cấp; tệp chỉ được staging sau khi kiểm tra lại manifest và SHA-256.",
                        $"{result.PackageUri} · SHA-256: {result.PackageSha256 ?? "Thiếu"}");
                }

                if (_lastStagedUpdate is { Success: true } staged
                    && string.Equals(staged.Version, result.LatestVersion, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(staged.PackageSha256, result.PackageSha256, StringComparison.OrdinalIgnoreCase))
                {
                    AddReadOnlyVfcItem(
                        card,
                        "maint-update-staged",
                        "Gói đã tải và xác minh",
                        "Tệp đang nằm trong staging; ứng dụng chưa giải nén hoặc cài đặt gói.",
                        $"{staged.FilePath} · {staged.SizeBytes:N0} bytes");
                }

                if (result.IsSignatureValid && result.IsUpdateAvailable
                    && _authorizationService.HasPermission(Permission.ManageServices))
                {
                    var stageAction = new SettingItemViewModel(
                        "maint-update-stage-action",
                        "\uE896",
                        "Tải và xác minh gói",
                        "Tải tối đa 1 GiB vào thư mục staging cục bộ; không cài đặt hoặc thay thế file đang chạy.",
                        "Tải gói",
                        targetPageKey: "maint-update-stage")
                    {
                        RequiredPermission = Permission.ManageServices,
                        RequiresConfirmation = true
                    };
                    card.Items.Add(stageAction);
                }
            }
        }
        else
        {
            var description = route switch
            {
                _ => "Mục bảo trì này chưa có provider runtime tương ứng."
            };
            AddReadOnlyVfcItem(card, "maint-provider-missing", row.Title, description, "Chưa sẵn sàng");
        }
        detail.Cards.Add(card);
        return detail;
    }

    private static bool IsMaintenanceDetailRoute(string route) =>
        route.Equals("maint-version", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-check", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-update-stage", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-channel", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-tools", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-cache", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-cache-run", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-index", StringComparison.OrdinalIgnoreCase)
        || route.Equals("maint-repair", StringComparison.OrdinalIgnoreCase);

    private async Task StageVerifiedUpdateAsync()
    {
        if (!_authorizationService.HasPermission(Permission.ManageServices))
        {
            _notificationService.Show("Bạn không có quyền tải gói cập nhật hệ thống.");
            return;
        }

        var confirmed = await _authorizationService.ConfirmDangerousActionAsync(
            "Tải gói cập nhật",
            "Ứng dụng sẽ tải tối đa 1 GiB từ nguồn HTTPS trong manifest đã ký và chỉ lưu tệp sau khi SHA-256 khớp. Gói sẽ chưa được cài đặt. Tiếp tục?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var stagingService = _serviceProvider.GetService<IUpdatePackageStagingService>();
            if (stagingService is null)
            {
                _notificationService.Show("Dịch vụ staging gói cập nhật chưa được đăng ký.");
                return;
            }

            var channelSetting = await _settingsService.GetValueAsync("Maintenance.UpdateChannel", SettingScope.System, null);
            var channel = string.IsNullOrWhiteSpace(channelSetting?.Value) ? "Stable" : channelSetting.Value.Trim();
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);
            await LogUpdateAuditSafelyAsync(
                SecurityEventType.UpdateStagingStarted,
                channel,
                currentVersion.ToString(),
                "Bắt đầu tải và xác minh gói cập nhật.");
            _lastStagedUpdate = await stagingService.StageLatestAsync(channel, currentVersion);
            var staged = _lastStagedUpdate;
            await LogUpdateAuditSafelyAsync(
                staged.Success ? SecurityEventType.UpdateStagingCompleted : SecurityEventType.UpdateStagingFailed,
                channel,
                staged.Version ?? currentVersion.ToString(),
                staged.Success ? "Tải và xác minh gói cập nhật hoàn tất; chưa cài đặt." : "Không thể staging gói cập nhật.",
                new Dictionary<string, string>
                {
                    ["Status"] = staged.Status,
                    ["SizeBytes"] = staged.SizeBytes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    ["PackageSha256"] = staged.PackageSha256 ?? string.Empty
                });
            _notificationService.Show(_lastStagedUpdate.Success
                ? $"Đã tải và xác minh gói {_lastStagedUpdate.Version}: {_lastStagedUpdate.FilePath}"
                : _lastStagedUpdate.Detail ?? "Không thể staging gói cập nhật.");

            if (CurrentPage?.Key == "maintenance"
                && CurrentPage.Rows.FirstOrDefault(x => x.Id == "maint-check") is { } updateRow)
            {
                CurrentDetail = await BuildMaintenanceDetailAsync("maint-check", updateRow);
                IsDetailMode = true;
                RaisePageFlags();
            }
        }
        catch (OperationCanceledException)
        {
            await LogUpdateAuditSafelyAsync(
                SecurityEventType.UpdateStagingFailed,
                null,
                null,
                "Tải gói cập nhật bị hủy hoặc quá thời gian.",
                new Dictionary<string, string> { ["Status"] = "CanceledOrTimedOut" });
            _notificationService.Show("Tải gói cập nhật bị hủy hoặc quá thời gian; tệp tạm sẽ được dọn dẹp.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not stage a signed application update package.");
            await LogUpdateAuditSafelyAsync(
                SecurityEventType.UpdateStagingFailed,
                null,
                null,
                "Tải hoặc xác minh gói cập nhật gặp lỗi.",
                new Dictionary<string, string> { ["ErrorType"] = ex.GetType().Name });
            _notificationService.Show("Không thể tải gói cập nhật. Kiểm tra kết nối, dung lượng và nhật ký hệ thống.");
        }
    }

    private async Task LogUpdateAuditSafelyAsync(
        SecurityEventType eventType,
        string? channel,
        string? version,
        string message,
        IDictionary<string, string>? metadata = null)
    {
        metadata ??= new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(channel)) metadata["Channel"] = channel;
        if (!string.IsNullOrWhiteSpace(version)) metadata["Version"] = version;

        try
        {
            await _securityAuditService.LogAsync(eventType, "Settings", "UpdateStaging", version, message, metadata);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not persist update-staging audit event. EventType={EventType}", eventType);
        }
    }

    private async Task<SettingsDetailViewModel> BuildAboutDetailAsync(SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Thông tin hệ thống > {row.Title}"
        };
        var card = new SettingCardViewModel("Runtime information", "Thông tin đọc từ process và Windows hiện tại");
        switch (row.Id)
        {
            case "about-product":
                AddReadOnlyVfcItem(card, "about-product-name", "Product", "ASMILE BO", "Enterprise Shell");
                AddReadOnlyVfcItem(card, "about-version", "Version", Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Chưa xác định", "Entry assembly");
                break;
            case "about-runtime":
                AddReadOnlyVfcItem(card, "about-framework", ".NET", RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture.ToString());
                AddReadOnlyVfcItem(card, "about-os-runtime", "OS", RuntimeInformation.OSDescription, Environment.OSVersion.VersionString);
                break;
            case "about-os":
                AddReadOnlyVfcItem(card, "about-os-name", "Operating system", RuntimeInformation.OSDescription, Environment.OSVersion.VersionString);
                break;
            case "about-machine":
                AddReadOnlyVfcItem(card, "about-machine-name", "Machine", Environment.MachineName, Environment.UserName);
                AddReadOnlyVfcItem(card, "about-process-arch", "Process architecture", RuntimeInformation.ProcessArchitecture.ToString(), "Runtime");
                break;
            case "about-license":
                var licenseService = _serviceProvider.GetService<ILicenseService>();
                if (licenseService is null)
                {
                    AddReadOnlyVfcItem(card, "about-license-provider", "License provider", "Chưa đăng ký license provider.", "Chưa sẵn sàng");
                }
                else
                {
                    var license = await licenseService.GetStatusAsync();
                    AddReadOnlyVfcItem(card, "about-license-provider", "License provider", license.Detail ?? license.ProviderStatus, license.Status);
                    if (!string.IsNullOrWhiteSpace(license.LicenseId))
                    {
                        AddReadOnlyVfcItem(card, "about-license-id", "License ID", license.LicenseId, license.CustomerName ?? "Không có customer");
                    }
                    if (license.ExpiresAtUtc.HasValue)
                    {
                        AddReadOnlyVfcItem(card, "about-license-expiry", "Hết hạn", license.ExpiresAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), license.IsValid ? "Còn hiệu lực" : "Không hợp lệ");
                    }
                    if (license.Features.Count > 0)
                    {
                        AddReadOnlyVfcItem(card, "about-license-features", "Features", string.Join(", ", license.Features), $"{license.Features.Count} features");
                    }
                }
                break;
            case "about-diag":
                AddReadOnlyVfcItem(card, "about-diagnostics-provider", "Diagnostics export", "Diagnostics package được cung cấp ở Operations Center; mục này không tạo file ngầm.", "Chỉ đọc");
                break;
            default:
                AddReadOnlyVfcItem(card, "about-read-only", row.Title, row.Description, "Chỉ đọc");
                break;
        }
        detail.Cards.Add(card);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildVfcMaintenanceDetailAsync(string route, SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Virtual File Center > {row.Title}"
        };

        if (string.Equals(route, "vfc-retention", StringComparison.OrdinalIgnoreCase))
        {
            var previews = await _archiveService.PreviewRetentionAsync();
            var card = new SettingCardViewModel("Retention thực tế", "Policy và số lượng hết hạn đọc từ database hiện tại");
            if (previews.Count == 0)
            {
                AddReadOnlyVfcItem(card, "retention-empty", "Chưa có retention policy", "Database chưa trả về policy retention đang bật.", "Chưa có dữ liệu");
            }
            else
            {
                foreach (var preview in previews)
                {
                    var size = FormatBytes(preview.EstimatedPayloadSizeBytes);
                    AddReadOnlyVfcItem(
                        card,
                        $"retention-{preview.Module}-{preview.EntityName}",
                        $"{preview.Module} · {preview.EntityName}",
                        $"{preview.PolicyCount} policy · ước tính {size} dữ liệu hết hạn",
                        $"{preview.ExpiredCount} bản ghi");
                }
            }

            detail.Cards.Add(card);
            var actions = new SettingCardViewModel("Thao tác", "Policy được chỉnh trong màn hình quản lý retention; preview phản ánh cấu hình đã lưu.");
            actions.Items.Add(new SettingItemViewModel(
                "retention-refresh", "\uE72C", "Nạp lại policy", "Mở lại màn hình policy đang lưu trong database.", "Mở", targetPageKey: "vfc-retention"));
            detail.Cards.Add(actions);
            return detail;
        }

        if (string.Equals(route, "vfc-rebuild", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Làm mới cây VFC", "Nạp lại cache đọc từ nguồn dữ liệu thật hiện tại");
            card.Items.Add(new SettingItemViewModel(
                "vfc-rebuild-run",
                "\uE72C",
                "Làm mới cây VFC",
                "Xóa cache tài liệu, đọc lại dữ liệu nghiệp vụ và dựng lại root/drive theo cơ chế on-demand.",
                "Chạy ngay",
                targetPageKey: "vfc-rebuild-run"));
            detail.Cards.Add(card);
            return detail;
        }

        if (string.Equals(route, "vfc-rebuild-run", StringComparison.OrdinalIgnoreCase))
        {
            var refreshService = _serviceProvider.GetService<IVirtualExplorerService>();
            if (refreshService is null)
            {
                var missingCard = new SettingCardViewModel("Làm mới cây VFC", "Không resolve được VirtualExplorerService");
                AddReadOnlyVfcItem(
                    missingCard,
                    "vfc-rebuild-service-missing",
                    "Không thể làm mới VFC",
                    "DI chưa cung cấp VirtualExplorerService; không hiển thị số liệu giả.",
                    "Không khả dụng");
                detail.Cards.Add(missingCard);
                return detail;
            }

            var refresh = await refreshService.RefreshAsync();
            _notificationService.Show($"Đã làm mới VFC: {refresh.DocumentCount:N0} tài liệu, {refresh.DriveNodeCount:N0} drive.");
            var card = new SettingCardViewModel("Kết quả làm mới", "Số liệu trả về từ VirtualExplorerService");
            AddReadOnlyVfcItem(card, "vfc-refresh-documents", "Tài liệu", "Tổng số tài liệu sau khi đọc lại các nguồn nghiệp vụ.", $"{refresh.DocumentCount:N0}");
            AddReadOnlyVfcItem(card, "vfc-refresh-drives", "Drive", "Số drive được dựng lại từ cấu hình VFC và filesystem.", $"{refresh.DriveNodeCount:N0}");
            AddReadOnlyVfcItem(card, "vfc-refresh-at", "Thời điểm", "Thời điểm làm mới theo UTC.", refresh.RefreshedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            detail.Cards.Add(card);
            return detail;
        }

        IReadOnlyList<VirtualExplorerNodeDto> rootNodes = Array.Empty<VirtualExplorerNodeDto>();
        IReadOnlyList<VirtualExplorerNodeDto> driveNodes = Array.Empty<VirtualExplorerNodeDto>();
        var virtualExplorerService = _serviceProvider.GetService<IVirtualExplorerService>();
        if (virtualExplorerService is not null)
        {
            rootNodes = await virtualExplorerService.GetRootAsync();
            driveNodes = await virtualExplorerService.GetChildrenAsync("This PC");
        }

        if (string.Equals(route, "vfc-index", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Cây VFC thực tế", "Đọc trực tiếp từ VirtualExplorerService và dữ liệu document hiện tại");
            AddReadOnlyVfcItem(card, "vfc-root-count", "Root nodes", "Các node gốc mà VFC đang cung cấp.", $"{rootNodes.Count} node");
            AddReadOnlyVfcItem(card, "vfc-drive-count", "Drive nodes", "Các drive được materialize từ cấu hình VFC và filesystem.", $"{driveNodes.Count} drive");
            AddReadOnlyVfcItem(card, "vfc-index-mode", "Cơ chế index", "Cây virtual được tạo khi đọc; không có số liệu index mẫu hoặc đếm giả.", "On-demand");
            detail.Cards.Add(card);
            await AddVfcIntegrityCardAsync(detail);
            return detail;
        }

        if (string.Equals(route, "vfc-repair", StringComparison.OrdinalIgnoreCase))
        {
            var card = new SettingCardViewModel("Repair broken links", "Kiểm tra kết quả scan đã lưu trước khi tạo yêu cầu xử lý");
            AddReadOnlyVfcItem(card, "vfc-repair-scope", "Phạm vi", "Yêu cầu repair được ghi theo company/branch hiện tại.", "VFC broken links");
            var request = new SettingItemViewModel(
                "vfc-repair-request",
                "\uEA39",
                "Tạo yêu cầu repair",
                "Chỉ xếp hàng yêu cầu vào DataRepairOperations; không tự ý sửa hoặc xóa file.",
                "Xếp hàng",
                targetPageKey: "vfc-repair-queue");
            card.Items.Add(request);
            detail.Cards.Add(card);
            await AddVfcIntegrityCardAsync(detail);
            return detail;
        }

        if (string.Equals(route, "vfc-repair-queue", StringComparison.OrdinalIgnoreCase))
        {
            var operationId = await _dataIntegrityService.QueueRepairAsync("vfc-broken-links");
            _notificationService.Show($"Đã xếp hàng yêu cầu repair VFC: {operationId}");
            var card = new SettingCardViewModel("Yêu cầu repair", "Yêu cầu đã được ghi vào database để worker xử lý");
            AddReadOnlyVfcItem(card, "vfc-repair-operation", "Operation ID", "Mã yêu cầu được lưu trong DataRepairOperations.", operationId.ToString());
            AddReadOnlyVfcItem(card, "vfc-repair-status", "Trạng thái", "Settings chỉ tạo hàng đợi; chưa báo hoàn tất khi chưa có worker thực thi.", "Queued");
            detail.Cards.Add(card);
            await AddVfcIntegrityCardAsync(detail);
            return detail;
        }

        return detail;
    }

    private async Task AddVfcIntegrityCardAsync(SettingsDetailViewModel detail)
    {
        var issues = await _dataIntegrityService.ScanAsync();
        var card = new SettingCardViewModel("Integrity scan gần nhất", "Kết quả đọc từ DataIntegrityScanResults");
        if (issues.Count == 0)
        {
            AddReadOnlyVfcItem(card, "vfc-integrity-empty", "Chưa có kết quả scan", "Database chưa có bản ghi scan toàn vẹn dữ liệu.", "Chưa có dữ liệu");
        }
        else
        {
            foreach (var issue in issues.Take(20))
            {
                var status = issue.Id == Guid.Empty ? "Info" : issue.Severity;
                AddReadOnlyVfcItem(card, $"vfc-integrity-{issue.Id}", issue.Module, issue.Message, status);
            }
        }

        detail.Cards.Add(card);
    }

    private static void AddReadOnlyVfcItem(
        SettingCardViewModel card,
        string id,
        string title,
        string description,
        string value,
        string? targetPageKey = null)
    {
        var hasDestination = !string.IsNullOrWhiteSpace(targetPageKey);
        var resolvedTarget = hasDestination ? targetPageKey! : $"settings-info:{id}";
        var item = new SettingItemViewModel(id, "\uE8A4", title, description, value, targetPageKey: resolvedTarget);
        item.IsReadOnly = !hasDestination;
        item.IsClickable = true;
        card.Items.Add(item);
    }

    private SettingsDetailViewModel BuildClientServiceDetail(SettingsDetailViewModel detail, SettingItemViewModel row)
    {
        var services = new SettingCardViewModel(
            "Trạng thái dịch vụ Windows",
            "Trạng thái hiện tại đọc từ Windows Service Control Manager; đây không phải heartbeat ứng dụng.");
        var candidates = row.Id switch
        {
            "clients-pos" => ServiceStatuses.Where(x => x.ServiceName.Contains("POS", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("POS", StringComparison.OrdinalIgnoreCase)),
            "clients-fo" => ServiceStatuses.Where(x => x.ServiceName.Contains("FO", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("FO", StringComparison.OrdinalIgnoreCase)),
            "clients-wh" => ServiceStatuses.Where(x => x.ServiceName.Contains("Warehouse", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("Warehouse", StringComparison.OrdinalIgnoreCase)),
            "clients-sync" => ServiceStatuses.Where(x => x.ServiceName.Contains("sync", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("sync", StringComparison.OrdinalIgnoreCase)),
            "clients-print" => ServiceStatuses.Where(x => x.ServiceName.Contains("print", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("print", StringComparison.OrdinalIgnoreCase) || x.ServiceName.Contains("spooler", StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains("spooler", StringComparison.OrdinalIgnoreCase)),
            _ => ServiceStatuses.AsEnumerable()
        };

        var serviceRows = candidates.ToList();
        if (serviceRows.Count == 0)
        {
            AddReadOnlyVfcItem(
                services,
                "client-service-empty",
                "Không có Windows service phù hợp",
                "Windows Service Control Manager không trả về dịch vụ tương ứng; không suy diễn trạng thái endpoint/client.",
                "Chưa có dữ liệu");
        }
        else
        {
            foreach (var service in serviceRows)
            {
            AddReadOnlyVfcItem(
                    services,
                    $"client-service-{service.Id}",
                    string.IsNullOrWhiteSpace(service.DisplayName) ? service.ServiceName : service.DisplayName,
                    $"{service.ServiceName} · nguồn: Windows Service Control Manager",
                    service.Status,
                    "windows-settings-services");
            }
        }
        detail.Cards.Add(services);

        if (string.Equals(row.Id, "clients-reg", StringComparison.OrdinalIgnoreCase))
        {
            var registry = new SettingCardViewModel("Phiên máy trạm đang hoạt động", "Dữ liệu đọc từ ActiveCompanySessions persisted. Tên máy và version là thông tin khai báo từ phiên, chưa phải định danh thiết bị đáng tin cậy.");
            if (ClientRegistryEntries.Count == 0)
            {
                AddReadOnlyVfcItem(
                    registry,
                    "client-registry-empty",
                    "Chưa có máy trạm hoạt động",
                    "Không có session client active trong company/branch hiện tại; hệ thống không dựng máy trạm mẫu.",
                    "Chưa có dữ liệu");
            }
            else
            {
                foreach (var client in ClientRegistryEntries)
                {
                    var machine = string.IsNullOrWhiteSpace(client.MachineName) ? "Máy chưa định danh" : client.MachineName;
                    var source = string.IsNullOrWhiteSpace(client.Source) ? "Nguồn chưa định danh" : client.Source;
                    var version = string.IsNullOrWhiteSpace(client.AppVersion) ? "Chưa có version" : client.AppVersion;
                    var lastActivity = client.LastActivityAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    AddReadOnlyVfcItem(
                        registry,
                        $"client-registry-{client.Id}",
                        machine,
                        $"Phiên đăng nhập · {source} · {version} · hoạt động cuối {lastActivity}",
                        client.Status);

                    if (client.SessionId is Guid sessionId
                        && sessionId != _currentUserService.CurrentSessionId
                        && _authorizationService.HasPermission(Permission.ManageUsers))
                    {
                        var revoke = new SettingItemViewModel(
                            $"client-session-revoke-{sessionId:N}",
                            "\uE8BB",
                            "Thu hồi phiên đăng nhập",
                            "Đăng xuất phiên này trên mọi phạm vi công ty và ghi nhận thao tác vào nhật ký kiểm toán.",
                            "Thu hồi",
                            targetPageKey: $"client-session-revoke-{sessionId:N}")
                        {
                            IsDanger = true,
                            RequiresConfirmation = true,
                            RequiredPermission = Permission.ManageUsers
                        };
                        registry.Items.Add(revoke);
                    }
                }
            }
            detail.Cards.Add(registry);
        }

        var actions = new SettingCardViewModel(
            "Thao tác máy trạm",
            row.Id switch
            {
                "clients-pos" => "Endpoint POS được xem tại Kết nối mạng; hiện chưa có POS client runtime để đổi endpoint có hiệu lực.",
                "clients-fo" => "Endpoint FO được xem tại Kết nối mạng; hiện chưa có FO client runtime để đổi endpoint có hiệu lực.",
                "clients-reg" => "Phiên và trạng thái là dữ liệu runtime; có thể nạp lại hoặc thu hồi phiên khác khi đủ quyền.",
                "clients-sync" => "Chỉ hiển thị Windows services có tên sync; chưa có provider lưu interval/conflict policy. Có thể quản lý trạng thái và startup type trong Services của Windows.",
                "clients-print" => "Trạng thái service đọc từ Windows; cấu hình máy in được quản lý trong Windows Printers & scanners.",
                _ => "Trạng thái dịch vụ là dữ liệu Windows/runtime; có thể nạp lại để lấy kết quả mới nhất."
            });
        if (string.Equals(row.Id, "clients-pos", StringComparison.OrdinalIgnoreCase))
        {
            actions.Items.Add(new SettingItemViewModel(
                "clients-pos-open-endpoint", "\uE7F4", "Mở endpoint POS", "Xem endpoint và chạy lại probe.", "Mở chi tiết", targetPageKey: "settings-category:network:net-pos"));
        }
        else if (string.Equals(row.Id, "clients-fo", StringComparison.OrdinalIgnoreCase))
        {
            actions.Items.Add(new SettingItemViewModel(
                "clients-fo-open-endpoint", "\uE8FA", "Mở endpoint FO", "Xem endpoint và chạy lại probe.", "Mở chi tiết", targetPageKey: "settings-category:network:net-fo"));
        }
        else if (string.Equals(row.Id, "clients-print", StringComparison.OrdinalIgnoreCase))
        {
            actions.Items.Add(new SettingItemViewModel(
                "clients-print-open-windows", "\uE749", "Mở Printers & scanners", "Quản lý máy in, hàng đợi và máy in mặc định trong Windows.", "Mở Windows Settings", targetPageKey: "windows-settings-printers"));
            actions.Items.Add(new SettingItemViewModel(
                "clients-print-open-services", "\uE943", "Quản lý Print Spooler", "Mở Windows Services để xem startup type hoặc trạng thái dịch vụ in.", "Mở Services", targetPageKey: "windows-settings-services"));
        }
        else
        {
            actions.Items.Add(new SettingItemViewModel(
                "clients-refresh", "\uE72C", "Nạp lại trạng thái", "Đọc lại service Windows và phiên máy trạm từ nguồn hiện tại.", "Làm mới", targetPageKey: row.Id));
            if (!string.Equals(row.Id, "clients-reg", StringComparison.OrdinalIgnoreCase))
            {
                actions.Items.Add(new SettingItemViewModel(
                    "clients-open-services", "\uE943", "Quản lý Windows services", "Mở Services để chỉnh startup type hoặc quản lý service. Quyền thay đổi phụ thuộc Windows.", "Mở Services", targetPageKey: "windows-settings-services"));
            }
        }
        detail.Cards.Add(actions);
        return detail;
    }

    private async Task<SettingsDetailViewModel> BuildClientHealthDetailAsync(SettingItemViewModel row)
    {
        var detail = new SettingsDetailViewModel
        {
            Title = row.Title,
            Subtitle = row.Description,
            Breadcrumb = $"Cài đặt > Máy trạm & dịch vụ > {row.Title}"
        };
        var card = new SettingCardViewModel(
            "Kiểm tra hạ tầng thật",
            "Kết quả lấy trực tiếp từ InfrastructureHealthService; không thay thế heartbeat của máy trạm.");
        var healthService = _serviceProvider.GetService<IInfrastructureHealthService>();
        if (healthService is null)
        {
            AddReadOnlyVfcItem(card, "clients-health-provider", "Health provider", "Infrastructure health provider chưa được đăng ký.", "Chưa sẵn sàng");
        }
        else
        {
            var report = await healthService.CheckAsync();
            AddReadOnlyVfcItem(
                card,
                "clients-health-overall",
                "Tổng trạng thái",
                $"Kiểm tra lúc {report.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
                report.Status);

            foreach (var check in report.Checks)
            {
                AddReadOnlyVfcItem(
                    card,
                    $"clients-health-{check.Name}",
                    check.Name,
                    FormatHomeHealthStatus(check.Name, check),
                    check.Status);
            }
        }
        detail.Cards.Add(card);

        var actions = new SettingCardViewModel(
            "Thao tác",
            "Báo cáo sức khỏe chỉ đọc; mở đúng trang cấu hình để thay đổi thông tin có thể chỉnh sửa.");
        actions.Items.Add(new SettingItemViewModel(
            "clients-health-refresh",
            "\uE72C",
            "Kiểm tra lại",
            "Chạy lại các health check từ database và provider hiện tại.",
            "Làm mới",
            targetPageKey: "clients-health"));
        actions.Items.Add(new SettingItemViewModel(
            "clients-health-open-database",
            "\uE9D2",
            "Cấu hình SQL Server",
            "Mở màn hình kiểm tra và chỉnh cấu hình kết nối SQL Server.",
            "Mở cấu hình",
            targetPageKey: "database-sql-connection"));
        detail.Cards.Add(actions);
        return detail;
    }

    private async Task RevokeClientSessionAsync(Guid sessionId)
    {
        if (!_authorizationService.HasPermission(Permission.ManageUsers))
        {
            _notificationService.Show("Bạn không có quyền quản lý phiên máy trạm.");
            return;
        }

        if (sessionId == _currentUserService.CurrentSessionId)
        {
            _notificationService.Show("Không thể thu hồi phiên đăng nhập hiện tại từ màn hình này.");
            return;
        }

        var confirmed = await _authorizationService.ConfirmDangerousActionAsync(
            "Thu hồi phiên máy trạm",
            "Phiên đăng nhập này sẽ bị vô hiệu hóa ngay trên máy chủ. Ứng dụng ở máy đó sẽ yêu cầu đăng nhập lại trong tối đa 30 giây. Tiếp tục?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var clientRegistry = _serviceProvider.GetService<IClientRegistryService>();
            if (clientRegistry is null)
            {
                _notificationService.Show("Dịch vụ quản lý phiên máy trạm chưa sẵn sàng.");
                return;
            }

            if (!await clientRegistry.RevokeSessionAsync(sessionId))
            {
                _notificationService.Show("Phiên không còn hoạt động hoặc không thuộc phạm vi công ty hiện tại.");
            }
            else
            {
                _notificationService.Show("Đã thu hồi phiên máy trạm. Máy khách sẽ bị đăng xuất khi kiểm tra phiên tiếp theo.");
            }

            var activeSessions = await clientRegistry.GetActiveClientsAsync();
            ClientRegistryEntries.Clear();
            foreach (var activeSession in activeSessions)
            {
                ClientRegistryEntries.Add(activeSession);
            }

            if (CurrentPage?.Key == "clients"
                && CurrentPage.Rows.FirstOrDefault(x => x.Id == "clients-reg") is { } registryRow)
            {
                CurrentDetail = BuildDetail(CurrentPage, registryRow);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to revoke workstation login session {SessionId}.", sessionId);
            _notificationService.Show("Không thể thu hồi phiên máy trạm. Kiểm tra quyền và nhật ký hệ thống.");
        }
    }

    private SettingsDetailViewModel BuildPrinterDetail(SettingsDetailViewModel detail, SettingItemViewModel row)
    {
        var printers = new SettingCardViewModel("Máy in Windows", "Danh sách đọc trực tiếp từ Windows Print Spooler");
        if (AvailablePrinters.Count == 0)
        {
            AddReadOnlyVfcItem(
                printers,
                "printer-empty",
                "Không có máy in được phát hiện",
                "Không có dữ liệu máy in từ hệ điều hành hiện tại; hệ thống không dựng máy in mẫu.",
                "Chưa có dữ liệu");
        }
        else
        {
            foreach (var printer in AvailablePrinters)
            {
                AddReadOnlyVfcItem(
                    printers,
                    $"printer-{printer.Name}",
                    printer.Name,
                    printer.SharePath,
                    printer.Status);
            }
        }
        detail.Cards.Add(printers);

        var permissions = new SettingCardViewModel("Quyền máy in", "ACL đọc trực tiếp từ Windows; chưa có thao tác ghi quyền trong ứng dụng");
        if (PrinterPermissions.Count == 0)
        {
            AddReadOnlyVfcItem(
                permissions,
                "printer-permissions-empty",
                "Chưa đọc được ACL máy in",
                "Không có kết quả ACL từ Windows; không suy diễn quyền từ danh sách máy in.",
                "Chỉ đọc");
        }
        else
        {
            foreach (var group in PrinterPermissions
                         .GroupBy(x => x.PrinterName, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var readableRows = group.Count(x => string.Equals(x.ReadStatus, "Read", StringComparison.OrdinalIgnoreCase));
                var unavailableRows = group.Count() - readableRows;
                var status = unavailableRows == 0
                    ? $"{readableRows} ACL"
                    : $"{readableRows} ACL · {unavailableRows} lỗi đọc";
                AddReadOnlyVfcItem(
                    permissions,
                    $"printer-acl-{group.Key}",
                    group.Key,
                    string.Join("; ", group.Where(x => !string.IsNullOrWhiteSpace(x.Principal)).Take(4).Select(x => $"{x.Principal}: {x.AccessType} {x.Rights}")),
                    status);
            }
        }
        detail.Cards.Add(permissions);

        if (string.Equals(row.Id, "print-default", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Id, "net-printer", StringComparison.OrdinalIgnoreCase))
        {
            var actions = new SettingCardViewModel("Thay đổi máy in", "Danh sách và ACL được đọc từ Windows; thay đổi driver, queue hoặc máy in mặc định trong hệ điều hành.");
            actions.Items.Add(new SettingItemViewModel(
                "printer-open-windows-settings", "\uE749", "Mở Printers & scanners", "Quản lý máy in và chọn default printer trong Windows.", "Mở Windows Settings", targetPageKey: "windows-settings-printers"));
            detail.Cards.Add(actions);
        }

        if (!string.Equals(row.Id, "print-default", StringComparison.OrdinalIgnoreCase))
        {
            var templates = new SettingCardViewModel(
                "Mẫu biểu",
                "Mở hồ sơ mẫu biểu và máy in được lưu theo cơ sở khách sạn.");
            templates.Items.Add(new SettingItemViewModel(
                "printer-template-provider",
                "\uE8A5",
                "Mẫu biểu & máy in theo khách sạn",
                "Chỉnh hồ sơ mẫu biểu đã lưu trong workspace cấu hình khách sạn.",
                "Mở workspace",
                targetPageKey: "hotel-forms-printers"));
            detail.Cards.Add(templates);
        }

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
        general.Items.Add(new SettingItemViewModel(
            "g1", "\uE73E", "Bật ứng dụng", "Cho phép module hoạt động", selected?.IsEnabled == true ? "Bật" : "Tắt",
            targetPageKey: "app-module-toggle-enabled")
        { IsToggle = true, ToggleValue = selected?.IsEnabled == true });
        general.Items.Add(new SettingItemViewModel(
            "g2", "\uE8FD", "Hiển thị Desktop", "Hiện biểu tượng trên desktop", selected?.ShowOnDesktop == true ? "Bật" : "Tắt",
            targetPageKey: "app-module-toggle-desktop")
        { IsToggle = true, ToggleValue = selected?.ShowOnDesktop == true });
        general.Items.Add(new SettingItemViewModel(
            "g3", "\uE7C5", "Ghim taskbar", "Hiện trên thanh taskbar", selected?.PinToTaskbar == true ? "Bật" : "Tắt",
            targetPageKey: "app-module-toggle-taskbar")
        { IsToggle = true, ToggleValue = selected?.PinToTaskbar == true });
        AddReadOnlyVfcItem(
            general,
            "g4",
            "Chính sách truy cập máy trạm",
            "Chưa áp dụng: hệ thống chưa phân biệt vai trò máy chủ trung tâm và máy trạm. Cờ legacy được giữ nguyên, không dùng để cấp quyền.",
            "Chưa áp dụng");
        detail.Cards.Add(general);

        var moduleDefinition = _moduleSettingsService.GetDefinition(appKey);
        var operationalSettings = moduleDefinition?.Sections
            .SelectMany(section => section.Settings)
            .Where(setting => string.IsNullOrWhiteSpace(setting.DisabledDescription))
            .ToArray() ?? [];
        if (operationalSettings.Length > 0)
        {
            var moduleSettings = new SettingCardViewModel(
                "Cấu hình nghiệp vụ module",
                "Các thiết lập riêng được module hỗ trợ; lưu qua provider cấu hình module.");
            moduleSettings.Items.Add(new SettingItemViewModel(
                $"app-module-settings-{appKey}",
                "\uE713",
                "Mở cấu hình nghiệp vụ",
                $"Có {operationalSettings.Length} thiết lập module đang được hỗ trợ để chỉnh sửa.",
                "Mở cấu hình",
                targetPageKey: $"app-module-settings:{appKey}"));
            detail.Cards.Add(moduleSettings);
        }

        var paths = new SettingCardViewModel("Đường dẫn runtime", "Chỉ hiển thị đường dẫn đã được lưu trong Settings Engine");
        AddReadOnlyVfcItem(
            paths,
            "p1",
            "Data root",
            FormatSettingsPathValue(_storageDataRootPath, _storageDataRootPathIsPersisted),
            FormatSettingsPathStatus(_storageDataRootPath, _storageDataRootPathIsPersisted),
            "system-folders");
        AddReadOnlyVfcItem(
            paths,
            "p-export",
            "Thư mục xuất dữ liệu",
            ApplicationDataPaths.UserExportsDirectory,
            "Đang được các luồng export của ứng dụng sử dụng",
            "system-folders");

        AddReadOnlyVfcItem(
            paths,
            "p2",
            "Thư mục riêng của ứng dụng",
            "Chưa có cấu hình runtime riêng cho module này; không tự suy diễn từ tên ứng dụng.",
            "Chưa cấu hình");
        detail.Cards.Add(paths);

        return detail;
    }

    private SettingsDetailViewModel BuildHotelConfigurationDetail()
    {
        var detail = new SettingsDetailViewModel
        {
            Title = "Khách sạn & Front Office",
            Subtitle = "Trung tâm cấu hình dữ liệu nền, giá, tài chính và quy tắc Front Office.",
            Breadcrumb = "Cài đặt > Khách sạn & Front Office"
        };

        var property = new SettingCardViewModel(
            "Cơ sở lưu trú",
            "Thông tin pháp lý và lịch nghiệp vụ dùng chung cho toàn bộ property");
        property.Items.Add(new SettingItemViewModel("hotel-property-profile", "\uE7EF", "Thông tin cơ sở", "Mã cơ sở, tên, địa chỉ, liên hệ, mã số thuế, múi giờ, tiền tệ và business date.", "Mở workspace dữ liệu", targetPageKey: "hotel-property-profile"));
        property.Items.Add(new SettingItemViewModel("hotel-property-documents", "\uE8A5", "Giấy phép & hồ sơ", "Giấy phép kinh doanh, lưu trú, thời hạn và tài liệu tham chiếu theo cơ sở.", "Mở workspace dữ liệu", targetPageKey: "hotel-property-documents"));
        detail.Cards.Add(property);

        var space = new SettingCardViewModel(
            "Không gian & phòng",
            "Mỗi danh mục được quản lý độc lập, liên kết bằng khóa thật và theo property");
        space.Items.Add(new SettingItemViewModel(
            "hotel-room-types", "\uE8EC", "Hạng phòng",
            "Thêm, sửa, kích hoạt hoặc tạm ngưng hạng phòng; thiết lập sức chứa chuẩn/tối đa.",
            "Mở workspace dữ liệu", targetPageKey: "hotel-room-types"));
        space.Items.Add(new SettingItemViewModel(
            "hotel-rooms", "\uE8FA", "Quản lý phòng",
            "Thêm phòng, sửa hồ sơ, gắn hạng phòng, tòa nhà và tầng; quản lý thuộc tính phòng.",
            "Mở workspace dữ liệu", targetPageKey: "hotel-rooms"));
        space.Items.Add(new SettingItemViewModel(
            "hotel-buildings", "\uE80F", "Tòa nhà",
            "Danh mục độc lập: mã, tên, thứ tự, mô tả và trạng thái sử dụng.",
            "Mở workspace dữ liệu", targetPageKey: "hotel-buildings"));
        space.Items.Add(new SettingItemViewModel(
            "hotel-room-floors", "\uE787", "Tầng",
            "Danh mục độc lập theo tòa nhà: mã, tên, số tầng, thứ tự và trạng thái sử dụng.",
            "Mở workspace dữ liệu", targetPageKey: "hotel-room-floors"));
        space.Items.Add(new SettingItemViewModel(
            "hotel-areas", "\uE8B7", "Khu vực",
            "Danh mục độc lập: mã, loại khu vực, tòa nhà/tầng, thứ tự và mô tả.",
            "Mở workspace dữ liệu", targetPageKey: "hotel-areas"));
        space.Items.Add(new SettingItemViewModel("hotel-room-status-rules", "\uE8FB", "Trạng thái & quy tắc phòng", "Tách FO status, housekeeping status, OOS/OOS reason và quy tắc chuyển trạng thái theo property.", "Mở workspace dữ liệu", targetPageKey: "hotel-room-status-rules"));
        detail.Cards.Add(space);

        var guest = new SettingCardViewModel(
            "Khách & hồ sơ",
            "Danh mục và rule bắt buộc cho hồ sơ khách, không trộn với màn hình tác nghiệp");
        guest.Items.Add(new SettingItemViewModel("hotel-guest-types", "\uE716", "Loại khách", "Individual, Couple, Family, Group, Corporate, VIP; trường bắt buộc và giá trị mặc định cho reservation.", "Mở workspace dữ liệu", targetPageKey: "hotel-guest-types"));
        guest.Items.Add(new SettingItemViewModel("hotel-profile-types", "\uE8D4", "Loại hồ sơ", "Individual, Company, Travel Agent, OTA, Government, Group, VIP; trường bắt buộc và default nghiệp vụ.", "Mở workspace dữ liệu", targetPageKey: "hotel-profile-types"));
        guest.Items.Add(new SettingItemViewModel("hotel-vip-levels", "\uE734", "Hạng VIP", "Cấp VIP, Welcome, Amenity, Upgrade, Discount, Priority và Notification.", "Mở workspace dữ liệu", targetPageKey: "hotel-vip-levels"));
        guest.Items.Add(new SettingItemViewModel("hotel-guest-preferences", "\uE8A5", "Sở thích & yêu cầu chuẩn", "Ngôn ngữ, giường, tầng, view, dị ứng và lựa chọn dùng lại.", "Mở workspace dữ liệu", targetPageKey: "hotel-guest-preferences"));
        detail.Cards.Add(guest);

        var reservation = new SettingCardViewModel(
            "Đặt phòng & giá",
            "Danh mục bán phòng và quy tắc giá; không đưa màn Tra cứu đặt phòng vào đây");
        reservation.Items.Add(new SettingItemViewModel("hotel-reservation-types", "\uE8FB", "Loại đặt phòng", "Loại reservation, xác nhận, giữ phòng, cọc, bảo đảm, cutoff, minimum/maximum stay và overbooking.", "Mở workspace dữ liệu", targetPageKey: "hotel-reservation-types"));
        reservation.Items.Add(new SettingItemViewModel("hotel-booking-sources", "\uE8D1", "Nguồn đặt phòng", "Walk-in, phone, email, website, OTA, corporate, tracking và hoa hồng.", "Mở workspace dữ liệu", targetPageKey: "hotel-booking-sources"));
        reservation.Items.Add(new SettingItemViewModel("hotel-booking-channels", "\uE968", "Kênh đặt phòng", "Direct, website, booking engine, OTA, GDS, agent, corporate và call center.", "Mở workspace dữ liệu", targetPageKey: "hotel-booking-channels"));
        reservation.Items.Add(new SettingItemViewModel("hotel-market-segments", "\uE8C8", "Phân khúc thị trường", "FIT, group, corporate, leisure, wholesale, contract, OTA, long stay và crew.", "Mở workspace dữ liệu", targetPageKey: "hotel-market-segments"));
        reservation.Items.Add(new SettingItemViewModel(
            "hotel-rate-engine",
            "\uE8C8", "Khung giá & giá phòng",
            "Rate plan, lịch giá, mùa/sự kiện và quy tắc tăng giảm giá.",
            "Mở workspace cấu hình",
            targetPageKey: "hotel-rate-engine"));
        reservation.Items.Add(new SettingItemViewModel("hotel-rate-calendar", "\uE787", "Lịch giá & mùa vụ", "Lịch giá theo ngày, mùa/sự kiện và quy tắc áp dụng; dùng chung Rate Engine để không tạo bản sao dữ liệu giá.", "Mở workspace cấu hình", targetPageKey: "hotel-rate-calendar"));
        reservation.Items.Add(new SettingItemViewModel("hotel-promotions-packages", "\uE8C8", "Khuyến mại & package", "Điều kiện áp dụng, dịch vụ kèm theo, booking window, blackout date và giới hạn sử dụng.", "Mở workspace dữ liệu", targetPageKey: "hotel-promotions-packages"));
        reservation.Items.Add(new SettingItemViewModel("hotel-stay-policies", "\uE72E", "Chính sách lưu trú", "Cancellation, no-show, deposit, trẻ em, thú cưng và thời hạn áp dụng.", "Mở workspace dữ liệu", targetPageKey: "hotel-stay-policies"));
        reservation.Items.Add(new SettingItemViewModel("hotel-check-in-out-rules", "\uE787", "Quy tắc nhận/trả phòng", "Early/late, grace time, charge code, giấy tờ, thanh toán và vai trò phê duyệt theo property.", "Mở workspace dữ liệu", targetPageKey: "hotel-check-in-out-rules"));
        reservation.Items.Add(new SettingItemViewModel("hotel-room-move-upgrade", "\uE8FA", "Đổi phòng & nâng/hạ hạng", "Cho phép đổi/nâng/hạ, vai trò duyệt, cách xử lý chênh lệch, số bậc và yêu cầu phòng sẵn sàng HK.", "Mở workspace dữ liệu", targetPageKey: "hotel-room-move-upgrade"));
        detail.Cards.Add(reservation);

        var finance = new SettingCardViewModel(
            "Tài chính Front Office",
            "Thiết lập giao dịch FO và mapping sang BO; giao dịch posted chỉ được điều chỉnh bằng correction");
        finance.Items.Add(new SettingItemViewModel("hotel-folio-types", "\uE8A5", "Loại Folio", "Guest, Company, Master, Group, City Ledger; tự tạo, routing, split, merge và transfer.", "Mở workspace dữ liệu", targetPageKey: "hotel-folio-types"));
        finance.Items.Add(new SettingItemViewModel("hotel-charge-codes", "\uE8C8", "Charge code", "Mã dịch vụ, giá, thuế, phí dịch vụ, revenue/tax account, cost center và quyền posting/discount/void.", "Mở workspace dữ liệu", targetPageKey: "hotel-charge-codes"));
        finance.Items.Add(new SettingItemViewModel("hotel-payment-methods", "\uE8D4", "Phương thức thanh toán", "Cash, bank, card, QR, ví, OTA, credit, phí, tiền tệ, tham chiếu, phê duyệt và mapping BO.", "Mở workspace dữ liệu", targetPageKey: "hotel-payment-methods"));
        finance.Items.Add(new SettingItemViewModel("hotel-deposit-policies", "\uE8FB", "Chính sách đặt cọc", "Loại cọc, tỷ lệ/số tiền, hạn nộp, hoàn trả, chuyển cọc, tịch thu, nhắc hạn và tự áp dụng Folio.", "Mở workspace dữ liệu", targetPageKey: "hotel-deposit-policies"));
        finance.Items.Add(new SettingItemViewModel("hotel-discount-policies", "\uE8FB", "Chính sách chiết khấu", "Loại giảm, mức tối đa, vai trò được phép, cấp phê duyệt, lý do và accounting treatment.", "Mở workspace dữ liệu", targetPageKey: "hotel-discount-policies"));
        finance.Items.Add(new SettingItemViewModel(
            "hotel-tax-service-charge",
            "\uE8C8",
            "Thuế & phí dịch vụ",
            "Quy tắc thuế/phí theo property: tỷ lệ, inclusive/exclusive, dịch vụ áp dụng, ngày hiệu lực và quy tắc làm tròn.",
            "Mở workspace dữ liệu",
            targetPageKey: "hotel-tax-service-charge"));
        finance.Items.Add(new SettingItemViewModel("hotel-currency-exchange", "\uE8D1", "Tiền tệ & tỷ giá", "Tiền tệ cơ sở, ngoại tệ, tỷ giá mua/bán, hotel rate, ngày hiệu lực và đặt mặc định.", "Mở workspace dữ liệu", targetPageKey: "hotel-currency-exchange"));
        detail.Cards.Add(finance);

        var services = new SettingCardViewModel(
            "Dịch vụ & vận hành",
            "Danh mục/định mức ở đây; màn hình xử lý yêu cầu hằng ngày vẫn nằm trong Front Office");
        services.Items.Add(new SettingItemViewModel(
            "hotel-housekeeping-supplies", "\uE8B7", "Định mức đồ vải & tiện nghi",
            "Master mã, nhóm, PAR và ngưỡng vận hành theo property; không mở tồn kho tác nghiệp.", "Mở workspace dữ liệu", targetPageKey: "hotel-housekeeping-supplies"));
        services.Items.Add(new SettingItemViewModel(
            "hotel-minibar-items", "\uE14C", "Hàng hóa & minibar",
            "Cấu hình minibar theo cơ sở: mã tồn kho, giá/VAT, định mức, mã tính tiền và hiệu lực.", "Mở workspace dữ liệu", targetPageKey: "hotel-minibar-items"));
        services.Items.Add(new SettingItemViewModel("hotel-housekeeping-rules", "\uE8B7", "Quy tắc housekeeping", "Loại tác vụ, ưu tiên, bộ phận, SLA, inspection và tự tạo task.", "Mở workspace dữ liệu", targetPageKey: "hotel-housekeeping-rules"));
        services.Items.Add(new SettingItemViewModel("hotel-wakeup-rules", "\uE787", "Wake-up call", "Lịch gọi, lead time, escalation, xác nhận và kênh thông báo.", "Mở workspace dữ liệu", targetPageKey: "hotel-wakeup-rules"));
        services.Items.Add(new SettingItemViewModel("hotel-guest-request-catalog", "\uE8FB", "Danh mục yêu cầu khách", "Loại yêu cầu, ưu tiên, bộ phận, SLA, tự phân công và thông báo.", "Mở workspace dữ liệu", targetPageKey: "hotel-guest-request-catalog"));
        services.Items.Add(new SettingItemViewModel("hotel-complaint-catalog", "\uE72E", "Danh mục khiếu nại", "Nhóm, mức độ, bộ phận, SLA, escalation và chính sách bồi thường.", "Mở workspace dữ liệu", targetPageKey: "hotel-complaint-catalog"));
        detail.Cards.Add(services);

        var partners = new SettingCardViewModel(
            "Đối tác & đoàn",
            "Danh mục đối tác dùng chung cho đặt phòng, công nợ và hoa hồng");
        partners.Items.Add(new SettingItemViewModel("hotel-companies", "\uE716", "Công ty", "Hồ sơ công ty, MST, credit, công nợ và điều kiện hợp đồng theo cơ sở.", "Mở workspace dữ liệu", targetPageKey: "hotel-companies"));
        partners.Items.Add(new SettingItemViewModel("hotel-travel-agents", "\uE8D4", "Đại lý du lịch", "Mã IATA, giấy phép, điều khoản thanh toán, market và hoa hồng mặc định.", "Mở workspace dữ liệu", targetPageKey: "hotel-travel-agents"));
        partners.Items.Add(new SettingItemViewModel("hotel-groups", "\uE8FB", "Đoàn", "Mẫu đoàn, allotment, cutoff, deposit, routing và billing instruction.", "Mở workspace dữ liệu", targetPageKey: "hotel-groups"));
        partners.Items.Add(new SettingItemViewModel("hotel-commissions", "\uE8C8", "Hoa hồng", "Cơ sở tính, tỷ lệ, số tiền cố định, thuế/phí và phê duyệt theo đối tác.", "Mở workspace dữ liệu", targetPageKey: "hotel-commissions"));
        detail.Cards.Add(partners);

        var control = new SettingCardViewModel(
            "Ca, kiểm soát & tích hợp",
            "Kiểm soát cuối ngày, quyền duyệt, biểu mẫu, máy in và kết nối ngoài");
        control.Items.Add(new SettingItemViewModel("hotel-cashier-shift", "\uE8D4", "Cashier & ca", "Ca thu ngân, float, closing, variance, approval và bàn giao ca.", "Mở workspace dữ liệu", targetPageKey: "hotel-cashier-shift"));
        control.Items.Add(new SettingItemViewModel("hotel-night-audit", "\uE787", "Night audit", "Checklist, thứ tự bước, điều kiện khóa ngày và xử lý lỗi.", "Mở workspace dữ liệu", targetPageKey: "hotel-night-audit"));
        control.Items.Add(new SettingItemViewModel("hotel-approval-matrix", "\uE8FB", "Ma trận phê duyệt", "Quyền duyệt giảm giá, refund, void, room move, late checkout và adjustment.", "Mở workspace dữ liệu", targetPageKey: "hotel-approval-matrix"));
        control.Items.Add(new SettingItemViewModel("hotel-fo-permissions", "\uE72E", "Phân quyền FO", "Permission theo module, chức năng, property và dữ liệu nhạy cảm.", "Mở workspace dữ liệu", targetPageKey: "hotel-fo-permissions"));
        control.Items.Add(new SettingItemViewModel("hotel-forms-printers", "\uE749", "Biểu mẫu & máy in", "Template, printer mapping, khổ giấy và mẫu theo nghiệp vụ.", "Mở workspace dữ liệu", targetPageKey: "hotel-forms-printers"));
        control.Items.Add(new SettingItemViewModel("hotel-messaging-integrations", "\uE968", "Email/SMS/API", "Kênh gửi, provider, endpoint, template, retry và timeout.", "Mở workspace dữ liệu", targetPageKey: "hotel-messaging-integrations"));
        control.Items.Add(new SettingItemViewModel("hotel-audit-correction", "\uE72E", "Audit & điều chỉnh posted", "Giới hạn số tiền, lý do, phê duyệt và vai trò được phép sửa giao dịch đã post.", "Mở workspace dữ liệu", targetPageKey: "hotel-audit-correction"));
        control.Items.Add(new SettingItemViewModel("hotel-business-rules", "\uE8FB", "Business rules / Rule Engine", "Condition reference, thứ tự ưu tiên, blocking, exclusive và hiệu lực.", "Mở workspace dữ liệu", targetPageKey: "hotel-business-rules"));
        detail.Cards.Add(control);

        var status = new SettingCardViewModel("Trạng thái dữ liệu", "Nguồn dữ liệu của trung tâm cấu hình");
        AddReadOnlyVfcItem(
            status,
            "hotel-data-connection",
            "Kết nối dữ liệu",
            "Các màn hình cấu hình sử dụng dữ liệu theo property hiện tại.",
            ConnectionStatus);
        AddReadOnlyVfcItem(
            status,
            "hotel-settings-scope",
            "Phạm vi cấu hình",
            "Tất cả danh mục có khóa PropertyId; không đọc dữ liệu mẫu từ ViewModel.",
            "Theo property hiện tại");
        detail.Cards.Add(status);

        return detail;
    }

    private SettingsDetailViewModel BuildHotelRoomConfigurationDetail()
    {
        var detail = new SettingsDetailViewModel
        {
            Title = "Cấu hình phòng",
            Subtitle = "Khai báo dữ liệu nền trước khi đưa phòng vào khai thác.",
            Breadcrumb = "Cài đặt > Khách sạn & Front Office > Cấu hình phòng"
        };

        var roomCatalog = new SettingCardViewModel(
            "Danh mục phòng",
            "Các thao tác tạo mới, chỉnh sửa và ngừng sử dụng dữ liệu phòng");
        roomCatalog.Items.Add(new SettingItemViewModel(
            "hotel-room-types",
            "\uE8EC",
            "Hạng phòng",
            "Thêm, sửa, kích hoạt hoặc tạm ngưng hạng phòng; thiết lập sức chứa chuẩn/tối đa.",
            "Thêm / sửa",
            targetPageKey: "hotel-room-types"));
        roomCatalog.Items.Add(new SettingItemViewModel(
            "hotel-rooms",
            "\uE8FA",
            "Quản lý phòng",
            "Thêm phòng, sửa hồ sơ, đổi hạng phòng, đổi trạng thái và theo dõi tiện nghi.",
            "Thêm / sửa",
            targetPageKey: "hotel-rooms"));
        roomCatalog.Items.Add(new SettingItemViewModel(
            "hotel-buildings",
            "\uE80F",
            "Tòa nhà",
            "Thêm, sửa, sắp xếp và kích hoạt/ngừng sử dụng các tòa nhà của khách sạn.",
            "Thêm / sửa",
            targetPageKey: "hotel-buildings"));
        roomCatalog.Items.Add(new SettingItemViewModel(
            "hotel-room-floors",
            "\uE787",
            "Tầng",
            "Thêm, sửa, gắn tầng vào tòa nhà và quản lý trạng thái sử dụng.",
            "Thêm / sửa",
            targetPageKey: "hotel-room-floors"));
        roomCatalog.Items.Add(new SettingItemViewModel(
            "hotel-areas",
            "\uE8B7",
            "Khu vực",
            "Thêm, sửa và quản lý khu vực độc lập; phòng có thể gắn khu vực bằng khóa thật.",
            "Thêm / sửa",
            targetPageKey: "hotel-areas"));
        detail.Cards.Add(roomCatalog);

        var pricing = new SettingCardViewModel(
            "Giá phòng",
            "Các thành phần giá được quản lý trong Rate Engine");
        pricing.Items.Add(new SettingItemViewModel(
            "hotel-rate-engine",
            "\uE8C8",
            "Khung giá & giá phòng",
            "Rate plan, giá cơ bản, lịch giá, mùa/sự kiện và quy tắc giảm/tăng giá.",
            "Mở cấu hình",
            targetPageKey: "hotel-rate-engine"));
        detail.Cards.Add(pricing);

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

        // The installed-app page must represent the persisted app catalog.
        // Module setting definitions describe a schema, not an installed app;
        // using them here made modules such as training-data appear even when
        // they were not present in AppModules.
        foreach (var app in InstalledApps)
        {
            var iconGlyph = string.IsNullOrWhiteSpace(app.Icon) || IsIconFileReference(app.Icon) ? "\uE8A5" : app.Icon;
            AddOrUpdate(app.AppKey, app.Name, app.Description ?? string.Empty, iconGlyph);
        }

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

    private async Task LoadAppearanceIconPickerAsync(int loadVersion)
    {
        var apps = await _appModuleService.GetAppsAsync();
        if (loadVersion != Volatile.Read(ref _editableDetailLoadVersion))
            return;

        InstalledApps.Clear();
        foreach (var app in apps)
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
        var loadVersion = Interlocked.Increment(ref _editableDetailLoadVersion);
        ClearDetailEditorStates();
        _scopeMap.Clear();
        _scopeTargetMap.Clear();
        IsAppearanceIconPickerVisible = string.Equals(rowRoute, "appearance-main", StringComparison.OrdinalIgnoreCase);
        if (!EditableSettingsByRoute.TryGetValue(rowRoute, out var keys))
        {
            IsDirty = false;
            return;
        }

        foreach (var key in keys)
        {
            var scope = SettingScope.System;
            var target = (string?)null;
            SettingDefinitionDto? def = null;
            foreach (var candidateScope in new[] { SettingScope.System, SettingScope.User, SettingScope.App })
            {
                var candidateTarget = candidateScope switch
                {
                    SettingScope.User => _currentUserService.CurrentUser?.Id.ToString(),
                    SettingScope.App => ResolveAppSettingTarget(key),
                    _ => null
                };
                var definitions = await _settingsService.GetDefinitionsAsync(candidateScope, candidateTarget);
                if (loadVersion != Volatile.Read(ref _editableDetailLoadVersion))
                    return;

                def = definitions.FirstOrDefault(x => x.Key == key);
                if (def is not null)
                {
                    scope = candidateScope;
                    target = candidateTarget;
                    break;
                }
            }
            if (def is null) continue;

            var settingValue = await _settingsService.GetValueAsync(key, scope, target);
            if (loadVersion != Volatile.Read(ref _editableDetailLoadVersion))
                return;

            var val = settingValue?.Value ?? def.DefaultValue;
            var vm = new EditableSettingItemViewModel(def, val, settingValue?.IsPersisted == true, settingValue?.IsRuntimeProvided == true);
            vm.PropertyChanged += (_, __) => IsDirty = EditableItems.Any(x => x.IsDirty) || SelectedInstalledApp is not null;

            if (def.ValueType == SettingValueType.Enum)
            {
                if (key == "Database.AuthenticationMode") { vm.Options.Add("Sql"); vm.Options.Add("Windows"); }
                else if (key == "Appearance.Theme") { vm.Options.Add("Light"); vm.Options.Add("Dark"); vm.Options.Add("System"); }
                else if (key == "Logs.LogLevel") { vm.Options.Add("Debug"); vm.Options.Add("Information"); vm.Options.Add("Warning"); vm.Options.Add("Error"); }
                else if (key == "Maintenance.UpdateChannel") { vm.Options.Add("Stable"); vm.Options.Add("Beta"); vm.Options.Add("Internal"); }
            }

            EditableItems.Add(vm);
            _scopeMap[key] = scope;
            _scopeTargetMap[key] = target;
        }

        HasEditableForm = EditableItems.Count > 0;
        if (IsAppearanceIconPickerVisible)
        {
            await LoadAppearanceIconPickerAsync(loadVersion);
            if (loadVersion != Volatile.Read(ref _editableDetailLoadVersion))
                return;
        }
        IsDirty = false;
    }

    private static string? ResolveAppSettingTarget(string key)
    {
        if (key.StartsWith("POS.", StringComparison.OrdinalIgnoreCase)) return "pos-client";
        if (key.StartsWith("FO.", StringComparison.OrdinalIgnoreCase)) return "fo-client";
        if (key.StartsWith("GeneralLedger.", StringComparison.OrdinalIgnoreCase)) return "general-ledger";
        if (key.StartsWith("ImportVoucher.", StringComparison.OrdinalIgnoreCase)) return "receipts";
        if (key.StartsWith("ExportVoucher.", StringComparison.OrdinalIgnoreCase)) return "issues";
        if (key.StartsWith("Warehouse.", StringComparison.OrdinalIgnoreCase)) return "inventory";
        return null;
    }

    private static bool HasWritableAppSettingDefinitions(IEnumerable<SettingDefinitionDto> definitions)
        => definitions.Any(x => !x.IsReadOnly);

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
    private Task SaveGeneralLedgerCore()
    {
        _notificationService.Show("Các tham số lõi của Sổ cái do runtime quản lý và hiện chỉ đọc.");
        return Task.CompletedTask;
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
        if (!DateTime.TryParseExact(LedgerPeriodFromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate)
            || !DateTime.TryParseExact(LedgerPeriodToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            _notificationService.Show("Ngày kỳ kế toán không hợp lệ.");
            return;
        }

        var periodCode = GetLedgerPeriodCode();
        var period = await _accountingPeriodService.GetPeriodByCodeAsync(periodCode);
        var isNewPeriod = period is null;
        if (period is null)
        {
            period = await _accountingPeriodService.CreatePeriodAsync(
                periodCode,
                fromDate,
                toDate,
                _currentUserService.CurrentUser?.UserName ?? CurrentUser);
        }
        else if (period.FromDate.Date != fromDate.Date || period.ToDate.Date != toDate.Date)
        {
            LedgerCurrentPeriod = period.PeriodCode.Replace("-", "/", StringComparison.Ordinal);
            LedgerPeriodFromDate = period.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            LedgerPeriodToDate = period.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            LedgerPeriodStatus = FormatAccountingPeriodStatus(period.Status);
            LedgerLastLockedDate = period.LockedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chưa khóa sổ";
            LedgerLockedBy = period.LockedBy ?? "-";
            _notificationService.Show("Kỳ kế toán đã tồn tại. Ngày bắt đầu và kết thúc được quản lý bởi dữ liệu kỳ thật, không thể sửa tại màn này.");
            return;
        }

        LedgerCurrentPeriod = period.PeriodCode.Replace("-", "/", StringComparison.Ordinal);
        LedgerPeriodFromDate = period.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        LedgerPeriodToDate = period.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        LedgerPeriodStatus = FormatAccountingPeriodStatus(period.Status);
        LedgerLastLockedDate = period.LockedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chưa khóa sổ";
        LedgerLockedBy = period.LockedBy ?? "-";
        _notificationService.Show(isNewPeriod
            ? "Đã tạo kỳ kế toán trong dữ liệu thật."
            : "Kỳ kế toán đã tồn tại; dữ liệu hiển thị được xác nhận từ bản ghi thật.");
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

        var periodCode = GetLedgerPeriodCode();
        var user = _currentUserService.CurrentUser?.UserName ?? CurrentUser;
        var periodLock = await _accountingPeriodService.LockPeriodAsync(
            periodCode,
            branchId: null,
            lockedBy: user,
            reason: "Khóa từ Cài đặt > Sổ cái");

        LedgerPeriodStatus = periodLock.IsLocked ? "Đã khóa" : "Đang mở";
        LedgerLastLockedDate = periodLock.IsLocked
            ? periodLock.LockedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : "Chưa khóa sổ";
        LedgerLockedBy = periodLock.IsLocked ? periodLock.LockedBy : "-";
    }

    [RelayCommand]
    private async Task UnlockGeneralLedgerPeriod()
    {
        var periodCode = GetLedgerPeriodCode();
        var user = _currentUserService.CurrentUser?.UserName ?? CurrentUser;
        var periodLock = await _accountingPeriodService.UnlockPeriodAsync(
            periodCode,
            branchId: null,
            unlockedBy: user,
            reason: "Mở khóa từ Cài đặt > Sổ cái");

        LedgerPeriodStatus = periodLock.IsLocked ? "Đã khóa" : "Đang mở";
        LedgerLastLockedDate = periodLock.IsLocked
            ? periodLock.LockedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : "Chưa khóa sổ";
        LedgerLockedBy = periodLock.IsLocked ? periodLock.LockedBy : "-";
    }

    private string GetLedgerPeriodCode()
    {
        if (!DateTime.TryParseExact(
                LedgerCurrentPeriod?.Trim(),
                "MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var period))
        {
            throw new InvalidOperationException("Kỳ kế toán phải có định dạng MM/yyyy.");
        }

        return period.ToString("yyyy-MM", CultureInfo.InvariantCulture);
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
        await SaveGeneralLedgerSettingsAsync(
        [
            ("GeneralLedger.DefaultExportFormat", LedgerDefaultExportFormat),
            ("GeneralLedger.AllowExportExcel", BoolText(LedgerAllowExportExcel)),
            ("GeneralLedger.AllowExportPdf", BoolText(LedgerAllowExportPdf)),
            ("GeneralLedger.ShowAccountantSignature", BoolText(LedgerShowAccountantSignature)),
            ("GeneralLedger.ShowDirectorSignature", BoolText(LedgerShowDirectorSignature)),
            ("GeneralLedger.ReportFooterNote", LedgerReportFooterNote ?? string.Empty)
        ]);
        await LoadGeneralLedgerConfigurationAsync();
        _notificationService.Show("Đã lưu cấu hình xuất và trình bày PDF; Report Center áp dụng ngay cho Sổ cái và Cân đối phát sinh.");
    }

    [RelayCommand]
    private async Task SaveGeneralLedgerStatus()
    {
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
            NotifyAppModuleChanged(app.AppKey);
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
            case "general-ledger-toggle-auto-balance":
                _notificationService.Show("Quy tắc này chưa có provider runtime nên đang chỉ đọc.");
                return;
        }

        await SaveGeneralLedgerStatus();
    }

    private async Task ApplyAppModuleToggleAsync(string route)
    {
        var app = SelectedInstalledApp;
        if (app is null)
        {
            _notificationService.Show("Không tìm thấy module ứng dụng cần cập nhật.");
            return;
        }

        var isEnabled = app.IsEnabled;
        var showOnDesktop = app.ShowOnDesktop;
        var pinToTaskbar = app.PinToTaskbar;
        switch (route)
        {
            case "app-module-toggle-enabled":
                isEnabled = !isEnabled;
                break;
            case "app-module-toggle-desktop":
                showOnDesktop = !showOnDesktop;
                break;
            case "app-module-toggle-taskbar":
                pinToTaskbar = !pinToTaskbar;
                break;
            default:
                _notificationService.Show("Không tìm thấy tùy chọn module cần cập nhật.");
                return;
        }

        try
        {
            await _appModuleService.UpdateAppAsync(new UpdateAppModuleRequest
            {
                AppKey = app.AppKey,
                Icon = app.Icon,
                IsEnabled = isEnabled,
                ShowOnDesktop = showOnDesktop,
                PinToTaskbar = pinToTaskbar,
                AllowClientAccess = app.AllowClientAccess,
                RequiredPermission = app.RequiredPermission,
                Reason = "Update app module display and activation options"
            });

            NotifyAppModuleChanged(app.AppKey);
            await LoadAppAdvancedEditorAsync(app.AppKey);
            if (CurrentPage is not null)
            {
                CurrentDetail = BuildAppAdvancedDetail(CurrentPage, app.AppKey, app.Name, app.Description ?? "Cấu hình ứng dụng");
                IsDetailMode = true;
                SetBreadcrumbFromDetail(CurrentDetail);
                RaisePageFlags();
            }
            _notificationService.Show("Đã cập nhật cài đặt ứng dụng.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Updating app module setting failed. AppKey={AppKey}, Route={Route}", app.AppKey, route);
            _notificationService.Show("Không thể cập nhật cài đặt ứng dụng. Kiểm tra quyền và nhật ký hệ thống.");
        }
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
        LedgerPostingMethod = await GetAppSettingAsync("GeneralLedger.PostingMethod", "Nhật ký chung");
        LedgerRoundingMode = await GetAppSettingAsync("GeneralLedger.RoundingMode", "Làm tròn đến đồng");
        LedgerDefaultCurrency = await GetAppSettingAsync("GeneralLedger.DefaultCurrency", "VND");
        LedgerAllowVoiceRecording = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowVoiceRecording", "false"));
        LedgerAllowEditPostedEntries = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowEditPostedEntries", "false"));
        LedgerAllowDeleteEntries = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowDeleteEntries", "false"));
        LedgerRequireBalancedBeforeSave = ParseBool(await GetAppSettingAsync("GeneralLedger.RequireBalancedBeforeSave", "true"));
        LedgerAllowQuickPosting = ParseBool(await GetAppSettingAsync("GeneralLedger.AllowQuickPosting", "true"));
        LedgerAutoBalanceCheck = ParseBool(await GetAppSettingAsync("GeneralLedger.AutoBalanceCheck", "true"));

        // Period fields are owned by AccountingPeriodService. Ignore legacy
        // Settings Engine snapshots so stale sample values cannot override the
        // persisted period or fabricate a current month when none exists.
        var actualPeriod = await _accountingPeriodService.GetCurrentPeriodAsync();
        var periodDisplay = GeneralLedgerPeriodDisplay.From(actualPeriod);
        LedgerCurrentPeriod = periodDisplay.PeriodCode;
        LedgerPeriodFromDate = periodDisplay.FromDate;
        LedgerPeriodToDate = periodDisplay.ToDate;
        LedgerPeriodStatus = periodDisplay.Status;
        LedgerLastLockedDate = periodDisplay.LastLockedAt;
        LedgerLockedBy = periodDisplay.LockedBy;

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

    private static string FormatAccountingPeriodStatus(ASMILE.BO.Core.Enums.AccountingPeriodStatus status)
        => status switch
        {
            ASMILE.BO.Core.Enums.AccountingPeriodStatus.Locked => "Đã khóa",
            ASMILE.BO.Core.Enums.AccountingPeriodStatus.Closed => "Đã đóng",
            _ => "Đang mở"
        };

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

        _generalLedgerPostingSummary = $"Mặc định: tiền mặt {cash}, ngân hàng {bank}, phải thu {receivable}, tồn kho {inventory}, phải trả {payable}, doanh thu {revenue}, giá vốn {cogs}, điều chỉnh {adjustment}.";
        _generalLedgerPostingValue = "8 tài khoản";

        var currentPeriod = await _accountingPeriodService.GetCurrentPeriodAsync();
        var periodDisplay = GeneralLedgerPeriodDisplay.From(currentPeriod);
        if (currentPeriod is null)
        {
            _generalLedgerSettingsSummary = "Chưa có kỳ kế toán được lưu cho ngày hiện tại.";
            _generalLedgerSettingsValue = "Chưa cấu hình";
            _generalLedgerPeriodSummary = "Tạo kỳ kế toán trong nghiệp vụ Sổ cái để xem thời gian và trạng thái kỳ.";
            _generalLedgerPeriodValue = "Chưa cấu hình";
            _generalLedgerReportsSummary = "Chưa thể tổng hợp báo cáo theo kỳ vì chưa có kỳ kế toán hiện hành.";
            _generalLedgerReportsValue = "Chưa có kỳ";
            return;
        }

        var periodFilter = new LedgerFilterDto
        {
            FromDate = currentPeriod.FromDate.Date,
            ToDate = currentPeriod.ToDate.Date
        };
        var entries = await _generalLedgerService.GetLedgerEntriesAsync(periodFilter);
        _generalLedgerSettingsSummary = $"Nguồn SQL Server, kỳ {periodDisplay.PeriodCode}, {entries.Count:N0} bút toán trong kỳ, danh mục {accounts.Count:N0} tài khoản.";
        _generalLedgerSettingsValue = periodDisplay.Status;
        _generalLedgerPeriodSummary = $"Kỳ kế toán {periodDisplay.PeriodCode}, từ ngày {periodDisplay.FromDate} đến ngày {periodDisplay.ToDate}.";
        _generalLedgerPeriodValue = periodDisplay.Status;
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
            var settingValue = values.FirstOrDefault(x => x.Key == def.Key);
            var val = settingValue?.Value ?? def.DefaultValue;
            var vm = new EditableSettingItemViewModel(def, val, settingValue?.IsPersisted == true);
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
        CloseEmbeddedModule();
        CurrentDetail = null;
        ClearDetailEditorStates();

        if (_pages.TryGetValue(value.Key, out var page))
        {
            CurrentPage = page;
            Breadcrumb = $"Cài đặt > {page.Title}";
            PageTitle = page.Title;
            PageSubtitle = page.Subtitle;
            SetCategoryBreadcrumb(page);

            if (string.Equals(value.Key, "hotel", StringComparison.OrdinalIgnoreCase))
            {
                CurrentDetail = BuildHotelConfigurationDetail();
                IsDetailMode = true;
                SetBreadcrumbFromDetail(CurrentDetail);
            }
        }

        ApplyRowsFilter();
        RaisePageFlags();
    }

    [RelayCommand]
    private void CloseEmbeddedModule()
    {
        EmbeddedModuleContent = null;
        EmbeddedModuleTitle = string.Empty;
        IsEmbeddedModuleMode = false;

        if (string.Equals(CurrentPage?.Key, "hotel", StringComparison.OrdinalIgnoreCase))
        {
            CurrentDetail = BuildHotelConfigurationDetail();
            IsDetailMode = true;
            SetBreadcrumbFromDetail(CurrentDetail);
        }
        else if (CurrentPage is { } currentPage
                 && string.Equals(currentPage.Key, "vfc", StringComparison.OrdinalIgnoreCase))
        {
            CurrentDetail = null;
            IsDetailMode = false;
            SetCategoryBreadcrumb(currentPage);
        }
        else if (CurrentPage is { } printingPage
                 && string.Equals(printingPage.Key, "printing", StringComparison.OrdinalIgnoreCase))
        {
            CurrentDetail = null;
            IsDetailMode = false;
            SetCategoryBreadcrumb(printingPage);
        }

        RaisePageFlags();
    }

    private async Task OpenVfcRetentionPolicyAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<VfcRetentionPolicyViewModel>();
        await viewModel.InitializeAsync();
        ClearDetailEditorStates();
        CurrentDetail = null;
        IsDetailMode = false;
        EmbeddedModuleTitle = "Retention policy";
        EmbeddedModuleContent = viewModel;
        IsEmbeddedModuleMode = true;
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Cài đặt", "root"));
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Virtual File Center", "category") { ShowSeparator = true });
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(EmbeddedModuleTitle, "embedded") { ShowSeparator = true });
        Breadcrumb = $"Cài đặt > Virtual File Center > {EmbeddedModuleTitle}";
        RaisePageFlags();
    }

    private async Task OpenExportPipelineSettingsAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<ExportPipelineSettingsViewModel>();
        await viewModel.InitializeAsync();
        ClearDetailEditorStates();
        CurrentDetail = null;
        IsDetailMode = false;
        EmbeddedModuleTitle = "Export PDF/Excel";
        EmbeddedModuleContent = viewModel;
        IsEmbeddedModuleMode = true;
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Cài đặt", "root"));
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("In ấn & mẫu biểu", "category") { ShowSeparator = true });
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(EmbeddedModuleTitle, "embedded") { ShowSeparator = true });
        Breadcrumb = $"Cài đặt > In ấn & mẫu biểu > {EmbeddedModuleTitle}";
        RaisePageFlags();
    }

    private Task OpenHotelConfigurationInWorkspaceAsync(string route)
    {
        object? content;
        if (route.StartsWith("hotel-reference-catalog:", StringComparison.OrdinalIgnoreCase))
        {
            var catalogKey = route["hotel-reference-catalog:".Length..];
            var catalog = _serviceProvider.GetRequiredService<HotelReferenceCatalogViewModel>();
            catalog.Configure(catalogKey, HotelCatalogTitle(catalogKey), "Danh mục độc lập, lưu theo cơ sở hiện tại và có kiểm soát bật/tắt.");
            content = catalog;
        }
        else
        {
            content = route switch
            {
                "hotel-room-master" => (object)_serviceProvider.GetRequiredService<RoomMasterViewModel>(),
                "hotel-room-configuration-master" => _serviceProvider.GetRequiredService<HotelRoomConfigurationViewModel>(),
                "hotel-room-types" => _serviceProvider.GetRequiredService<RoomTypeMasterViewModel>(),
                "hotel-building-master" => _serviceProvider.GetRequiredService<HotelBuildingMasterViewModel>(),
                "hotel-floor-master" => _serviceProvider.GetRequiredService<HotelFloorMasterViewModel>(),
                "hotel-area-master" => _serviceProvider.GetRequiredService<HotelAreaMasterViewModel>(),
                "hotel-room-status-rules" => _serviceProvider.GetRequiredService<HotelRoomStatusRuleViewModel>(),
                "hotel-property-profile" => _serviceProvider.GetRequiredService<HotelPropertyProfileViewModel>(),
                "hotel-stay-policies" => _serviceProvider.GetRequiredService<HotelStayPolicyViewModel>(),
                "hotel-check-in-out-rules" => _serviceProvider.GetRequiredService<HotelCheckInOutRuleViewModel>(),
                "hotel-room-move-upgrade" => _serviceProvider.GetRequiredService<HotelRoomMoveUpgradeRuleViewModel>(),
                "hotel-property-documents" => _serviceProvider.GetRequiredService<HotelPropertyDocumentViewModel>(),
                "hotel-promotions-packages" => _serviceProvider.GetRequiredService<HotelPromotionPackageViewModel>(),
                "hotel-groups" => _serviceProvider.GetRequiredService<HotelGroupPolicyViewModel>(),
                "hotel-cashier-shift" => _serviceProvider.GetRequiredService<HotelCashierShiftPolicyViewModel>(),
                "hotel-night-audit" => _serviceProvider.GetRequiredService<HotelNightAuditPolicyViewModel>(),
                "hotel-approval-matrix" => _serviceProvider.GetRequiredService<HotelApprovalMatrixViewModel>(),
                "hotel-fo-permissions" => _serviceProvider.GetRequiredService<HotelFOPermissionPolicyViewModel>(),
                "hotel-forms-printers" => _serviceProvider.GetRequiredService<HotelFormPrinterProfileViewModel>(),
                "hotel-messaging-integrations" => _serviceProvider.GetRequiredService<HotelMessagingIntegrationProfileViewModel>(),
                "hotel-audit-correction" => _serviceProvider.GetRequiredService<HotelPostedCorrectionPolicyViewModel>(),
                "hotel-business-rules" => _serviceProvider.GetRequiredService<HotelBusinessRuleProfileViewModel>(),
                "hotel-tax-service-charge" => _serviceProvider.GetRequiredService<HotelTaxServiceChargeRuleViewModel>(),
                "hotel-payment-methods" => _serviceProvider.GetRequiredService<HotelPaymentMethodProfileViewModel>(),
                "hotel-deposit-policies" => _serviceProvider.GetRequiredService<HotelDepositPolicyViewModel>(),
                "hotel-discount-policies" => _serviceProvider.GetRequiredService<HotelDiscountPolicyViewModel>(),
                "hotel-charge-codes" => _serviceProvider.GetRequiredService<HotelChargeCodeProfileViewModel>(),
                "hotel-folio-types" => _serviceProvider.GetRequiredService<HotelFolioProfileViewModel>(),
                "hotel-guest-types" => _serviceProvider.GetRequiredService<HotelGuestTypeProfileViewModel>(),
                "hotel-profile-types" => _serviceProvider.GetRequiredService<HotelProfileTypeProfileViewModel>(),
                "hotel-vip-levels" => _serviceProvider.GetRequiredService<HotelVipLevelProfileViewModel>(),
                "hotel-guest-preferences" => _serviceProvider.GetRequiredService<HotelGuestPreferenceProfileViewModel>(),
                "hotel-guest-request-catalog" => _serviceProvider.GetRequiredService<HotelGuestRequestProfileViewModel>(),
                "hotel-reservation-types" => _serviceProvider.GetRequiredService<HotelReservationTypeProfileViewModel>(),
                "hotel-booking-sources" => _serviceProvider.GetRequiredService<HotelBookingSourceProfileViewModel>(),
                "hotel-booking-channels" => _serviceProvider.GetRequiredService<HotelBookingChannelProfileViewModel>(),
                "hotel-market-segments" => _serviceProvider.GetRequiredService<HotelMarketSegmentProfileViewModel>(),
                "hotel-housekeeping-rules" => _serviceProvider.GetRequiredService<HotelHousekeepingRuleProfileViewModel>(),
                "hotel-wakeup-rules" => _serviceProvider.GetRequiredService<HotelWakeupRuleProfileViewModel>(),
                "hotel-complaint-catalog" => _serviceProvider.GetRequiredService<HotelComplaintProfileViewModel>(),
                "hotel-companies" => _serviceProvider.GetRequiredService<HotelCompanyProfileViewModel>(),
                "hotel-travel-agents" => _serviceProvider.GetRequiredService<HotelTravelAgentProfileViewModel>(),
                "hotel-commissions" => _serviceProvider.GetRequiredService<HotelCommissionRuleProfileViewModel>(),
                "hotel-currency-exchange" => _serviceProvider.GetRequiredService<HotelExchangeRateViewModel>(),
                "hotel-rate-calendar" => _serviceProvider.GetRequiredService<RateEngineViewModel>(),
                "hotel-housekeeping-supplies" => _serviceProvider.GetRequiredService<HotelHousekeepingSupplyCatalogViewModel>(),
                "hotel-housekeeping-supply-catalog" => _serviceProvider.GetRequiredService<HotelHousekeepingSupplyCatalogViewModel>(),
                "hotel-rate-engine" => _serviceProvider.GetRequiredService<RateEngineViewModel>(),
            "items" => _serviceProvider.GetRequiredService<ItemsViewModel>(),
            "hotel-minibar-items" => _serviceProvider.GetRequiredService<HotelMinibarConfigurationViewModel>(),
                "cash-bank" => _serviceProvider.GetRequiredService<CashBankViewModel>(),
                _ => null
            };
        }

        if (content is null)
        {
            _notificationService.Show("Không tìm thấy màn hình cấu hình nghiệp vụ khách sạn.");
            return Task.CompletedTask;
        }

        EmbeddedModuleTitle = route.StartsWith("hotel-reference-catalog:", StringComparison.OrdinalIgnoreCase)
            ? HotelCatalogTitle(route["hotel-reference-catalog:".Length..])
            : route switch
        {
            "hotel-room-master" => "Tra cứu phòng",
            "hotel-room-configuration-master" => "Quản lý phòng",
            "hotel-room-types" => "Hạng phòng",
            "hotel-building-master" => "Tòa nhà",
            "hotel-floor-master" => "Tầng",
            "hotel-area-master" => "Khu vực",
            "hotel-room-status-rules" => "Trạng thái & quy tắc phòng",
            "hotel-property-profile" => "Thông tin cơ sở & lịch nghiệp vụ",
            "hotel-stay-policies" => "Chính sách lưu trú",
            "hotel-check-in-out-rules" => "Quy tắc nhận / trả phòng",
            "hotel-room-move-upgrade" => "Đổi phòng & nâng / hạ hạng",
            "hotel-property-documents" => "Giấy phép & hồ sơ cơ sở",
            "hotel-promotions-packages" => "Khuyến mại & package",
            "hotel-groups" => "Chính sách đoàn",
            "hotel-cashier-shift" => "Cashier & ca",
            "hotel-night-audit" => "Night audit",
            "hotel-approval-matrix" => "Ma trận phê duyệt",
            "hotel-fo-permissions" => "Phân quyền FO",
            "hotel-forms-printers" => "Biểu mẫu & máy in",
            "hotel-messaging-integrations" => "Email / SMS / API",
            "hotel-audit-correction" => "Audit & điều chỉnh posted",
            "hotel-business-rules" => "Business rules / Rule Engine",
            "hotel-currency-exchange" => "Tiền tệ & tỷ giá",
            "hotel-rate-calendar" => "Lịch giá & mùa vụ",
            "hotel-housekeeping-supplies" => "Định mức đồ vải & tiện nghi",
            "hotel-housekeeping-supply-catalog" => "Định mức đồ vải & tiện nghi",
            "hotel-rate-engine" => "Khung giá & quy tắc giá",
            "items" => "Danh mục hàng minibar",
            "cash-bank" => "Tiền tệ & tỷ giá",
            _ => "Cấu hình khách sạn"
        };

        ClearDetailEditorStates();
        CurrentDetail = null;
        IsDetailMode = false;
        EmbeddedModuleContent = content;
        IsEmbeddedModuleMode = true;
        BreadcrumbSegments.Clear();
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Cài đặt", "root"));
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment("Khách sạn & Front Office", "hotel") { ShowSeparator = true });
        BreadcrumbSegments.Add(new ExplorerBreadcrumbSegment(EmbeddedModuleTitle, "embedded") { ShowSeparator = true });
        Breadcrumb = $"Cài đặt > Khách sạn & Front Office > {EmbeddedModuleTitle}";
        RaisePageFlags();
        return Task.CompletedTask;
    }

    private static string HotelCatalogTitle(string catalogKey) => catalogKey.ToLowerInvariant() switch
    {
        "guest-type" => "Loại khách",
        "profile-type" => "Loại hồ sơ",
        "vip-level" => "Hạng VIP",
        "guest-preference" => "Sở thích & yêu cầu chuẩn",
        "reservation-type" => "Loại đặt phòng",
        "booking-source" => "Nguồn đặt phòng",
        "booking-channel" => "Kênh đặt phòng",
        "market-segment" => "Phân khúc thị trường",
        "folio-type" => "Loại folio",
        "charge-code" => "Charge code",
        "payment-method" => "Phương thức thanh toán",
        "deposit-discount-type" => "Loại đặt cọc & chiết khấu",
        "tax-service-charge" => "Mã thuế & phí dịch vụ",
        "housekeeping-rule" => "Quy tắc housekeeping",
        "wakeup-rule" => "Wake-up call",
        "guest-request" => "Danh mục yêu cầu khách",
        "complaint" => "Danh mục khiếu nại",
        "company" => "Công ty",
        "travel-agent" => "Đại lý du lịch",
        "commission" => "Hoa hồng",
        _ => "Danh mục Front Office"
    };

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
        ShowDetailCardsWithEditableForm = false;
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


