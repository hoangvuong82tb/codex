using ASMILE.BO.Shared.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

public sealed class SettingsRegistry : ISettingsRegistry
{
    public IReadOnlyList<SettingDefinitionDto> GetDefaultDefinitions()
    {
        var rows = new List<SettingDefinitionDto>();
        void Add(string key, string name, string category, string group, SettingScope scope, SettingValueType type, string? defaultValue, bool required = false, bool sensitive = false, bool restart = false, string? permission = "Settings.Edit")
            => rows.Add(new SettingDefinitionDto { Key = key, DisplayName = name, Category = category, Group = group, Scope = scope, ValueType = type, DefaultValue = defaultValue, IsRequired = required, IsSensitive = sensitive, RequiresRestart = restart, RequiredPermission = permission });

        Add("Database.ServerName", "Server name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, @".\SQLEXPRESS", true);
        Add("Database.DatabaseName", "Database name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, "ASMILE_BO", true);
        Add("Database.AuthenticationMode", "Authentication mode", "Database", "DATABASE", SettingScope.System, SettingValueType.Enum, "Sql");
        Add("Database.UserName", "User name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, "sa");
        Add("Database.Password", "Password", "Database", "DATABASE", SettingScope.System, SettingValueType.Password, string.Empty, false, true);
        Add("Database.ConnectionString", "Connection string", "Database", "DATABASE", SettingScope.System, SettingValueType.ConnectionString, "", false, true);
        Add("Database.CommandTimeout", "Command timeout (s)", "Database", "DATABASE", SettingScope.System, SettingValueType.Number, "30");
        Add("Database.EnableAutoMigration", "Enable auto migration", "Database", "DATABASE", SettingScope.System, SettingValueType.Boolean, "false");
        Add("Network.ApiBaseUrl", "API Base URL", "Network", "NETWORK", SettingScope.System, SettingValueType.String, "https://localhost");
        Add("Network.ApiPort", "API Port", "Network", "NETWORK", SettingScope.System, SettingValueType.Number, "5001");
        Add("Network.EnableHttps", "Enable HTTPS", "Network", "NETWORK", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Network.AllowClientDiscovery", "Allow client discovery", "Network", "NETWORK", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Network.ScanIntervalSeconds", "Scan interval seconds", "Network", "NETWORK", SettingScope.System, SettingValueType.Number, "60");
        Add("Storage.DataRootPath", "Data root path", "Storage", "STORAGE", SettingScope.System, SettingValueType.Path, @"D:\ACCOUNTING", true);
        Add("VFC.EnableVirtualDrives", "Enable virtual drives", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.SystemDrivePath", "System drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, @"C:\SYSTEM");
        Add("VFC.AccountingDrivePath", "Accounting drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, @"D:\ACCOUNTING");
        Add("VFC.SalesDrivePath", "Sales drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, @"E:\SALES");
        Add("VFC.HrDrivePath", "HR drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, @"F:\HR");
        Add("VFC.LogDrivePath", "Log drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, @"G:\LOGS");
        Add("VFC.SystemDriveEnabled", "System drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.AccountingDriveEnabled", "Accounting drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.SalesDriveEnabled", "Sales drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.HrDriveEnabled", "HR drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.LogDriveEnabled", "Log drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Backup.EnableAutoBackup", "Enable auto backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Backup.BackupPath", "Backup path", "Backup", "BACKUP", SettingScope.System, SettingValueType.Path, @"D:\ASMILE-BO\Backups", true);
        Add("Backup.Schedule", "Schedule", "Backup", "BACKUP", SettingScope.System, SettingValueType.String, "0 0 * * *");
        Add("Backup.RetentionCount", "Retention count", "Backup", "BACKUP", SettingScope.System, SettingValueType.Number, "10");
        Add("Backup.CompressBackup", "Compress backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Backup.VerifyAfterBackup", "Verify after backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Security.MaxFailedLoginAttempts", "Max failed login attempts", "Security", "SECURITY", SettingScope.System, SettingValueType.Number, "5");
        Add("Security.SessionTimeoutMinutes", "Session timeout", "Security", "SECURITY", SettingScope.System, SettingValueType.Number, "30");
        Add("Security.RequireAdminConfirmation", "Require admin confirmation", "Security", "SECURITY", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Logs.LogLevel", "Log level", "Logs", "LOGS", SettingScope.System, SettingValueType.Enum, "Information");
        Add("Logs.RetentionDays", "Retention days", "Logs", "LOGS", SettingScope.System, SettingValueType.Number, "30");
        Add("Logs.EnableAuditLog", "Enable audit log", "Logs", "LOGS", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Logs.EnableErrorLog", "Enable error log", "Logs", "LOGS", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Appearance.Theme", "Theme", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Enum, "Light");
        Add("Appearance.WallpaperPath", "Wallpaper path", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Path, "");
        Add("Appearance.ShowDesktopIcons", "Show desktop icons", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Boolean, "true");
        Add("Appearance.ShowTaskbar", "Show taskbar", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Boolean, "true");

        Add("ImportVoucher.CodePrefix", "Import code prefix", "Apps", "IMPORT VOUCHER", SettingScope.App, SettingValueType.String, "PN");
        Add("ImportVoucher.AutoGenerateCode", "Auto generate code", "Apps", "IMPORT VOUCHER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("ExportVoucher.CodePrefix", "Export code prefix", "Apps", "EXPORT VOUCHER", SettingScope.App, SettingValueType.String, "PX");
        Add("Warehouse.LowStockThreshold", "Low stock threshold", "Apps", "WAREHOUSE", SettingScope.App, SettingValueType.Number, "20");
        Add("GeneralLedger.CashAccount", "Số tài khoản tiền mặt", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "111");
        Add("GeneralLedger.BankAccount", "Số tài khoản ngân hàng", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "112");
        Add("GeneralLedger.ReceivableAccount", "Số tài khoản phải thu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "131");
        Add("GeneralLedger.InventoryAccount", "Số tài khoản hàng tồn kho", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "156");
        Add("GeneralLedger.PayableAccount", "Số tài khoản phải trả", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "331");
        Add("GeneralLedger.RevenueAccount", "Số tài khoản doanh thu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "511");
        Add("GeneralLedger.CostOfGoodsSoldAccount", "Số tài khoản giá vốn", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "632");
        Add("GeneralLedger.AdjustmentAccount", "Số tài khoản điều chỉnh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "338");
        Add("GeneralLedger.DataSource", "Nguồn dữ liệu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "SQL Server");
        Add("GeneralLedger.DatabaseName", "Tên database", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "ASMILE_BO", true);
        Add("GeneralLedger.CurrentPeriod", "Kỳ kế toán hiện tại", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, DateTime.Today.ToString("MM/yyyy"), true);
        Add("GeneralLedger.PostingMethod", "Phương pháp ghi sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Nhật ký chung");
        Add("GeneralLedger.RoundingMode", "Chế độ làm tròn tiền", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Làm tròn đến đồng");
        Add("GeneralLedger.DefaultCurrency", "Tiền tệ mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "VND", true);
        Add("GeneralLedger.AllowVoiceRecording", "Cho phép ghi âm", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        Add("GeneralLedger.AllowEditPostedEntries", "Cho phép sửa bút toán đã ghi sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        Add("GeneralLedger.AllowDeleteEntries", "Cho phép xóa bút toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        Add("GeneralLedger.RequireBalancedBeforeSave", "Bắt buộc cân đối Nợ/Có trước khi lưu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.AllowQuickPosting", "Cho phép hạch toán nhanh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.AutoBalanceCheck", "Tự động kiểm tra cân đối", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.PeriodFromDate", "Từ ngày kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"));
        Add("GeneralLedger.PeriodToDate", "Đến ngày kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)).ToString("dd/MM/yyyy"));
        Add("GeneralLedger.PeriodStatus", "Trạng thái kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Đang mở");
        Add("GeneralLedger.LastLockedDate", "Ngày khóa sổ gần nhất", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Chưa khóa sổ");
        Add("GeneralLedger.LockedBy", "Người khóa sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "-");
        Add("GeneralLedger.DefaultGeneralLedgerReport", "Mẫu sổ cái mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Sổ cái chi tiết tài khoản");
        Add("GeneralLedger.DefaultTrialBalanceReport", "Mẫu cân đối phát sinh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Bảng cân đối phát sinh");
        Add("GeneralLedger.DefaultExportFormat", "Định dạng xuất mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Excel");
        Add("GeneralLedger.AllowPrintReports", "Cho phép in báo cáo", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.AllowExportExcel", "Cho phép xuất Excel", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.AllowExportPdf", "Cho phép xuất PDF", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.ShowAccountantSignature", "Hiển thị chữ ký kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.ShowDirectorSignature", "Hiển thị chữ ký giám đốc", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.ReportFooterNote", "Ghi chú cuối báo cáo", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Báo cáo được lập từ dữ liệu sổ cái.");
        Add("POS.ApiEndpoint", "POS API endpoint", "ClientServices", "POS CLIENT", SettingScope.App, SettingValueType.String, "https://localhost:5001");
        Add("FO.ApiEndpoint", "FO API endpoint", "ClientServices", "FO CLIENT", SettingScope.App, SettingValueType.String, "https://localhost:5001");
        return rows;
    }

    public IReadOnlyList<AppModuleDto> GetDefaultApps()
    {
        string[] keys = ["dashboard","virtual-file-center","inventory","inventory-movement","warehouse","import-voucher","export-voucher","receivable","payable","general-ledger","report-center","customers","suppliers","hr","settings","pos-client","fo-client","accounting-client","warehouse-client"];
        return keys.Select((k, i) => new AppModuleDto { AppKey = k, Name = GetAppDisplayName(k), Description = GetAppDisplayDescription(k), Version = "1.0.0", IsEnabled = true, ShowOnDesktop = true, AllowClientAccess = true, SortOrder = i + 1, RequiredPermission = "Settings.View" }).ToList();
    }

    internal static string GetAppDisplayName(string appKey) => appKey.ToLowerInvariant() switch
    {
        "dashboard" => "Tổng quan",
        "virtual-file-center" => "Trung tâm file ảo",
        "inventory" or "inventory-movement" => "Quản lý xuất nhập tồn",
        "warehouse" or "warehouses" => "Quản lý kho",
        "import-voucher" or "receipts" => "Phiếu nhập",
        "export-voucher" or "issues" => "Phiếu xuất",
        "receivable" or "ar" => "Công nợ phải thu",
        "payable" or "ap" => "Công nợ phải trả",
        "general-ledger" => "Sổ cái",
        "customers" => "Khách hàng",
        "suppliers" => "Nhà cung cấp",
        "items" => "Hàng hóa",
        "hr" or "departments" => "Nhân sự",
        "settings" => "Cài đặt hệ thống",
        "pos-client" => "Máy trạm POS",
        "fo-client" => "Máy trạm FO",
        "accounting-client" => "Máy trạm kế toán",
        "warehouse-client" => "Máy trạm kho",
        _ => appKey
    };

    internal static string GetAppDisplayDescription(string appKey) => appKey.ToLowerInvariant() switch
    {
        "dashboard" => "Bảng điều khiển tổng quan",
        "virtual-file-center" => "Quản lý file và thư mục nghiệp vụ",
        "inventory" or "inventory-movement" => "Theo dõi nhập, xuất, tồn kho",
        "warehouse" or "warehouses" => "Danh mục kho và vị trí lưu trữ",
        "import-voucher" or "receipts" => "Lập và quản lý phiếu nhập",
        "export-voucher" or "issues" => "Lập và quản lý phiếu xuất",
        "receivable" or "ar" => "Theo dõi công nợ khách hàng",
        "payable" or "ap" => "Theo dõi công nợ nhà cung cấp",
        "general-ledger" => "Kế toán tổng hợp và cài đặt tài khoản",
        "customers" => "Danh mục khách hàng",
        "suppliers" => "Danh mục nhà cung cấp",
        "items" => "Danh mục hàng hóa",
        "hr" or "departments" => "Bộ phận, nhân sự và phân quyền",
        "settings" => "Cấu hình hệ thống",
        "pos-client" => "Kết nối điểm bán",
        "fo-client" => "Kết nối lễ tân",
        "accounting-client" => "Kết nối kế toán",
        "warehouse-client" => "Kết nối kho",
        _ => $"Module {appKey}"
    };
}

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly ISettingsRegistry _registry;
    private readonly ISettingsValidator _validator;
    private readonly ISettingsAuditService _audit;
    private readonly ISettingsChangeNotifier _notifier;
    private readonly ISecretProtector _protector;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;

    public SettingsService(ISettingsRepository repository, ISettingsRegistry registry, ISettingsValidator validator, ISettingsAuditService audit, ISettingsChangeNotifier notifier, ISecretProtector protector, ICurrentUserService currentUser, IAuthorizationService authorization)
    {
        _repository = repository;
        _registry = registry;
        _validator = validator;
        _audit = audit;
        _notifier = notifier;
        _protector = protector;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
    {
        await EnsureSeededAsync();
        var defs = await _repository.GetDefinitionsAsync();
        return defs.Where(x => x.Scope == scope).OrderBy(x => x.SortOrder).Select(x => x.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
    {
        await EnsureSeededAsync();
        var defs = (await _repository.GetDefinitionsAsync()).Where(x => x.Scope == scope).ToDictionary(x => x.Key);
        var values = await _repository.GetValuesAsync();
        return values.Where(x => x.Scope == scope && MatchScope(x.ScopeTarget, scopeTarget))
            .Select(x => x.ToDto(defs.TryGetValue(x.DefinitionKey, out var d) && d.IsSensitive && x.IsEncrypted ? SecretMasker.MaskSecret("***") : (x.IsEncrypted ? _protector.Unprotect(x.Value ?? string.Empty) : x.Value)))
            .ToList();
    }

    public async Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
    {
        var defs = await GetDefinitionsAsync(scope, scopeTarget);
        var def = defs.FirstOrDefault(x => x.Key == key);
        if (def is null) return null;
        var row = await _repository.GetValueAsync(key, scope, scopeTarget);
        if (row is null)
        {
            return new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = def.DefaultValue };
        }

        var displayValue = row.IsEncrypted ? (def.IsSensitive ? SecretMasker.MaskSecret("***") : _protector.Unprotect(row.Value ?? string.Empty)) : row.Value;
        return row.ToDto(displayValue);
    }

    public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => Task.WhenAll(requests.Select(SaveValueAsync));

    public async Task SaveValueAsync(SaveSettingValueRequest request)
    {
        await EnsureSeededAsync();
        var definitions = await _repository.GetDefinitionsAsync();
        var def = definitions.Select(x => x.ToDto()).First(x => x.Key == request.Key && x.Scope == request.Scope);
        if (!string.IsNullOrWhiteSpace(def.RequiredPermission)) _authorization.Demand(def.RequiredPermission);
        var validation = _validator.Validate(def, request.Value);
        if (!validation.IsValid) throw new InvalidOperationException(validation.ErrorMessage);

        var existing = await _repository.GetValueAsync(request.Key, request.Scope, request.ScopeTarget);
        var oldPlain = existing is null ? def.DefaultValue : (existing.IsEncrypted ? _protector.Unprotect(existing.Value ?? string.Empty) : existing.Value);
        var encrypted = def.IsSensitive;
        var storageValue = encrypted ? _protector.Protect(request.Value ?? string.Empty) : request.Value;

        var entity = existing ?? new SettingValue { DefinitionKey = request.Key, Scope = request.Scope, ScopeTarget = request.ScopeTarget };
        entity.Value = storageValue;
        entity.IsEncrypted = encrypted;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.CurrentUser?.UserName ?? "system";
        await _repository.UpsertValueAsync(entity);

        await _audit.LogChangeAsync(new SettingChangeLogDto
        {
            SettingKey = request.Key,
            Scope = request.Scope,
            ScopeTarget = request.ScopeTarget,
            OldValue = def.IsSensitive ? "***" : oldPlain,
            NewValue = def.IsSensitive ? "***" : request.Value,
            ChangedBy = entity.UpdatedBy ?? "system",
            ChangedAt = entity.UpdatedAt,
            Reason = request.Reason
        });

        _notifier.NotifyChanged(entity.ToDto(request.Value), def.RequiresRestart);
    }

    public async Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget)
    {
        var def = (await GetDefinitionsAsync(scope, scopeTarget)).First(x => x.Key == key);
        await SaveValueAsync(new SaveSettingValueRequest { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = def.DefaultValue, Reason = "ResetToDefault" });
    }

    private static bool MatchScope(string? source, string? target) => string.Equals(source ?? string.Empty, target ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private async Task EnsureSeededAsync()
    {
        var defs = _registry.GetDefaultDefinitions();
        foreach (var def in defs)
        {
            await _repository.UpsertDefinitionAsync(new SettingDefinition
            {
                Key = def.Key, DisplayName = def.DisplayName, Description = def.Description, Category = def.Category, Group = def.Group, Scope = def.Scope,
                ValueType = def.ValueType, DefaultValue = def.DefaultValue, IsRequired = def.IsRequired, IsSensitive = def.IsSensitive, IsReadOnly = def.IsReadOnly,
                RequiresRestart = def.RequiresRestart, RequiredPermission = def.RequiredPermission, SortOrder = def.SortOrder
            });
        }
    }
}

public sealed class AppModuleService : IAppModuleService
{
    private readonly IAppModuleRepository _repo;
    private readonly ISettingsRegistry _registry;
    private readonly ISettingsService _settings;

    public AppModuleService(IAppModuleRepository repo, ISettingsRegistry registry, ISettingsService settings)
    {
        _repo = repo;
        _registry = registry;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AppModuleDto>> GetAppsAsync()
    {
        await EnsureSeededAsync();
        return (await _repo.GetAppsAsync()).OrderBy(x => x.SortOrder).Select(x => new AppModuleDto
        {
            AppKey = x.AppKey, Name = SettingsRegistry.GetAppDisplayName(x.AppKey), Description = SettingsRegistry.GetAppDisplayDescription(x.AppKey), Icon = x.Icon, Version = x.Version, IsEnabled = x.IsEnabled,
            ShowOnDesktop = x.ShowOnDesktop, PinToTaskbar = x.PinToTaskbar, AllowClientAccess = x.AllowClientAccess, RequiredPermission = x.RequiredPermission, SortOrder = x.SortOrder
        }).ToList();
    }

    public async Task<AppModuleDto?> GetAppAsync(string appKey) => (await GetAppsAsync()).FirstOrDefault(x => x.AppKey == appKey);

    public async Task UpdateAppAsync(UpdateAppModuleRequest request)
    {
        var app = await _repo.GetAppAsync(request.AppKey) ?? throw new InvalidOperationException("App not found.");
        app.Icon = request.Icon;
        app.IsEnabled = request.IsEnabled;
        app.ShowOnDesktop = request.ShowOnDesktop;
        app.PinToTaskbar = request.PinToTaskbar;
        app.AllowClientAccess = request.AllowClientAccess;
        app.RequiredPermission = request.RequiredPermission;
        await _repo.UpsertAppAsync(app);
    }

    public async Task<IReadOnlyList<SettingDefinitionDto>> GetAppSettingDefinitionsAsync(string appKey)
    {
        var all = await _settings.GetDefinitionsAsync(SettingScope.App, appKey);
        var prefixes = GetSettingPrefixes(appKey);
        return all.Where(x => prefixes.Any(p => x.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    public async Task<IReadOnlyList<SettingValueDto>> GetAppSettingsAsync(string appKey)
    {
        var defs = await GetAppSettingDefinitionsAsync(appKey);
        var keys = defs.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = await _settings.GetValuesAsync(SettingScope.App, appKey);
        return values.Where(x => keys.Contains(x.Key)).ToList();
    }
    public Task SaveAppSettingsAsync(string appKey, IReadOnlyList<SaveSettingValueRequest> requests) => _settings.SaveValuesAsync(requests.Select(x => { x.Scope = SettingScope.App; x.ScopeTarget = appKey; return x; }).ToList());

    private async Task EnsureSeededAsync()
    {
        var existingApps = await _repo.GetAppsAsync();
        var existingKeys = existingApps
            .Select(x => x.AppKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var app in _registry.GetDefaultApps().Where(x => !existingKeys.Contains(x.AppKey)))
        {
            await _repo.UpsertAppAsync(new AppModule { AppKey = app.AppKey, Name = app.Name, Description = app.Description, Icon = app.Icon, Version = app.Version, IsEnabled = app.IsEnabled, ShowOnDesktop = app.ShowOnDesktop, PinToTaskbar = app.PinToTaskbar, AllowClientAccess = app.AllowClientAccess, RequiredPermission = app.RequiredPermission, SortOrder = app.SortOrder });
        }
    }

    private static string[] GetSettingPrefixes(string appKey)
    {
        return appKey.ToLowerInvariant() switch
        {
            "import-voucher" => ["ImportVoucher."],
            "export-voucher" => ["ExportVoucher."],
            "warehouse" => ["Warehouse."],
            "general-ledger" => ["GeneralLedger."],
            "pos-client" => ["POS."],
            "fo-client" => ["FO."],
            _ => []
        };
    }
}

