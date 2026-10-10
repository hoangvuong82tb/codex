using ASMILE.BO.App.Models;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Shared.Storage;
using System.IO;
using System.Text.Json;

namespace ASMILE.BO.App.Services;

public interface IModuleSettingsService
{
    IReadOnlyList<ModuleSettingsDefinition> GetDefinitions();
    ModuleSettingsDefinition? GetDefinition(string appKey);
    string NormalizeAppKey(string appKey);
    Task<ModuleSettingsState> LoadAsync(string appKey, CancellationToken cancellationToken = default);
    Task SaveAsync(string appKey, IDictionary<string, string> values, CancellationToken cancellationToken = default);
    Task<ModuleSettingsState> ResetDefaultAsync(string appKey, CancellationToken cancellationToken = default);
}

public interface ISettingsValidationService
{
    IReadOnlyDictionary<string, string> Validate(ModuleSettingsDefinition definition, IDictionary<string, string> values);
}

public interface IToastNotificationService
{
    void Success(string message);
    void Error(string message);
}

public sealed class ModuleSettingsValidationException(string key, string message) : InvalidOperationException(message)
{
    public string Key { get; } = key;
}

public sealed class ToastNotificationService(INotificationService notificationService) : IToastNotificationService
{
    public void Success(string message) => notificationService.Show(message);
    public void Error(string message) => notificationService.Show(message);
}

public sealed class ModuleSettingsValidationService : ISettingsValidationService
{
    public IReadOnlyDictionary<string, string> Validate(ModuleSettingsDefinition definition, IDictionary<string, string> values)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in definition.Sections.SelectMany(x => x.Settings))
        {
            if (!string.IsNullOrWhiteSpace(item.DisabledDescription))
            {
                continue;
            }

            var value = values.TryGetValue(item.Key, out var current) ? current : item.DefaultValue;
            if (item.Kind == ModuleSettingKind.Select
                && item.Options is { Count: > 0 }
                && !item.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                errors[item.Key] = "Giá trị không nằm trong danh sách lựa chọn được hỗ trợ.";
                continue;
            }

            if (item.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                errors[item.Key] = "Không được để trống.";
                continue;
            }

            if (item.Kind == ModuleSettingKind.Number && !decimal.TryParse(value, out _))
            {
                errors[item.Key] = "Giá trị phải là số.";
                continue;
            }

            if (item.Key.Contains("MaxFileSize", StringComparison.OrdinalIgnoreCase)
                && decimal.TryParse(value, out var maxFileSize)
                && maxFileSize <= 0)
            {
                errors[item.Key] = "Dung lượng file tối đa phải lớn hơn 0.";
            }

            if (item.Key.Contains("PaymentDays", StringComparison.OrdinalIgnoreCase)
                && decimal.TryParse(value, out var paymentDays)
                && paymentDays < 0)
            {
                errors[item.Key] = "Số ngày hạn thanh toán phải lớn hơn hoặc bằng 0.";
            }

            if (item.Key.Contains("CreditLimit", StringComparison.OrdinalIgnoreCase)
                && decimal.TryParse(value, out var creditLimit)
                && creditLimit < 0)
            {
                errors[item.Key] = "Hạn mức công nợ phải lớn hơn hoặc bằng 0.";
            }

            if (item.Key.Contains("PrintCopies", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value, out var copies)
                && (copies < 1 || copies > 5))
            {
                errors[item.Key] = "Số liên in phải từ 1 đến 5.";
            }

            if (item.Key.Contains("CodePattern", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(value))
            {
                errors[item.Key] = "Quy tắc mã không được rỗng.";
            }
        }

        var map = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        var masterCodeSettings = definition.AppKey.ToLowerInvariant() switch
        {
            "items" => (AutoKey: "autoProductCode", PatternKey: "productCodePattern"),
            "customers" => (AutoKey: "autoCustomerCode", PatternKey: "customerCodePattern"),
            "suppliers" => (AutoKey: "autoSupplierCode", PatternKey: "supplierCodePattern"),
            "departments" => (AutoKey: "autoEmployeeCode", PatternKey: "employeeCodePattern"),
            _ => (AutoKey: string.Empty, PatternKey: string.Empty)
        };
        if (!string.IsNullOrEmpty(masterCodeSettings.AutoKey) && Bool(map, masterCodeSettings.AutoKey))
        {
            var codePattern = map.TryGetValue(masterCodeSettings.PatternKey, out var configuredPattern)
                ? configuredPattern
                : definition.Sections.SelectMany(x => x.Settings)
                    .First(x => string.Equals(x.Key, masterCodeSettings.PatternKey, StringComparison.OrdinalIgnoreCase)).DefaultValue;
            if (!ASMILE.BO.Application.Services.SequenceCodePattern.IsValid(codePattern))
            {
                errors[masterCodeSettings.PatternKey] = "Cần đúng một token số dạng {00000}; có thể dùng thêm {yyyyMMdd}, {yyyy}, {MM}, {dd}.";
            }
        }

        if (Bool(map, "allowNegativeStock") || Bool(map, "allowNegativeWarehouse"))
        {
            errors[map.ContainsKey("allowNegativeStock") ? "allowNegativeStock" : "allowNegativeWarehouse"] =
                "Chỉ tài khoản quản trị/cấu hình mới được bật xuất âm kho.";
        }

        return errors;
    }

    private static bool Bool(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string Get(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : string.Empty;
}

public sealed class ModuleSettingsService : IModuleSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ISettingsRepository _repository;
    private readonly ISettingsService _settingsService;
    private readonly IWarehouseAppService? _warehouseAppService;
    private readonly string _settingsFolder;
    private readonly IReadOnlyList<ModuleSettingsDefinition> _definitions;

    public ModuleSettingsService(
        ISettingsRepository repository,
        ISettingsService settingsService,
        IWarehouseAppService? warehouseAppService = null)
    {
        _repository = repository;
        _settingsService = settingsService;
        _warehouseAppService = warehouseAppService;
        _settingsFolder = ApplicationDataPaths.UserSettingsDirectory;
        _definitions = BuildDefinitions();
    }

    public IReadOnlyList<ModuleSettingsDefinition> GetDefinitions() => _definitions;

    public ModuleSettingsDefinition? GetDefinition(string appKey)
    {
        var normalized = NormalizeAppKey(appKey);
        return _definitions.FirstOrDefault(x => string.Equals(x.AppKey, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public string NormalizeAppKey(string appKey)
    {
        var key = (appKey ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "ar" => "receivable",
            "ap" => "payable",
            "inventory-movement" => "inventory",
            "item" or "items" or "product" => "items",
            "warehouse" or "warehouses" => "warehouses",
            "hr" or "departments" or "department-hr" => "departments",
            "payment-receipt" or "receipt-voucher" => "payment-receipt",
            "payment-voucher" => "payment-voucher",
            "import-voucher" or "stock-in-voucher" or "receipts" => "receipts",
            "export-voucher" or "stock-out-voucher" or "issues" => "issues",
            _ => key
        };
    }

    public async Task<ModuleSettingsState> LoadAsync(string appKey, CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(appKey) ?? throw new InvalidOperationException($"Không có cấu hình cho module {appKey}.");
        await EnsureDefinitionsAsync(definition, cancellationToken);
        var defaults = CreateDefaultState(definition);

        var persistedValues = await _settingsService.GetValuesAsync(SettingScope.App, definition.AppKey);
        var prefix = GetKeyPrefix(definition.AppKey);
        var values = persistedValues
            .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key[prefix.Length..], x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        // Migrate the legacy local JSON store once. The authoritative store
        // after migration is Settings Engine/SQL, not the application folder.
        if (values.Count == 0)
        {
            var legacy = await ReadLegacyValuesAsync(definition, cancellationToken);
            if (legacy.Count > 0)
            {
                await SaveValuesToSettingsEngineAsync(definition, legacy, "Migrate legacy module settings", cancellationToken);
                values = legacy;
                persistedValues = await _settingsService.GetValuesAsync(SettingScope.App, definition.AppKey);
            }
        }

        foreach (var key in defaults.Values.Keys.ToList())
        {
            if (values.TryGetValue(key, out var value))
            {
                defaults.Values[key] = value;
            }
        }

        defaults.UpdatedAt = persistedValues.Count == 0 ? DateTime.UtcNow : persistedValues.Max(x => x.UpdatedAt);
        return defaults;
    }

    public async Task SaveAsync(string appKey, IDictionary<string, string> values, CancellationToken cancellationToken = default)
        => await SaveCoreAsync(appKey, values, "Update module settings", cancellationToken);

    private async Task SaveCoreAsync(
        string appKey,
        IDictionary<string, string> values,
        string reason,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(appKey) ?? throw new InvalidOperationException($"Không có cấu hình cho module {appKey}.");
        await EnsureDefinitionsAsync(definition, cancellationToken);
        // Start from the persisted state so settings that are intentionally
        // unavailable remain unchanged instead of being reset by the editor.
        var state = await LoadAsync(definition.AppKey, cancellationToken);
        foreach (var item in definition.Sections.SelectMany(x => x.Settings))
        {
            if (string.IsNullOrWhiteSpace(item.DisabledDescription)
                && values.TryGetValue(item.Key, out var value))
            {
                state.Values[item.Key] = value ?? string.Empty;
            }
        }

        if (definition.AppKey is "receipts" or "issues"
            && state.Values.TryGetValue("defaultWarehouse", out var defaultWarehouseCode)
            && !string.IsNullOrWhiteSpace(defaultWarehouseCode)
            && _warehouseAppService is not null)
        {
            var warehouses = await _warehouseAppService.GetLookupAsync(cancellationToken);
            if (!warehouses.Any(x => string.Equals(
                    x.Code?.Trim(), defaultWarehouseCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ModuleSettingsValidationException(
                    "defaultWarehouse",
                    $"Mã kho mặc định '{defaultWarehouseCode}' không tồn tại hoặc đã ngừng hoạt động. Hãy chọn mã kho đang hoạt động hoặc để trống để dùng kho đầu tiên.");
            }
        }
        state.UpdatedAt = DateTime.UtcNow;

        await SaveValuesToSettingsEngineAsync(definition, state.Values, reason, cancellationToken);
    }

    public async Task<ModuleSettingsState> ResetDefaultAsync(string appKey, CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(appKey) ?? throw new InvalidOperationException($"Không có cấu hình cho module {appKey}.");
        var state = CreateDefaultState(definition);
        await SaveCoreAsync(definition.AppKey, state.Values, "Reset module settings", cancellationToken);
        return await LoadAsync(definition.AppKey, cancellationToken);
    }

    private string GetPath(string appKey) => Path.Combine(_settingsFolder, $"{NormalizeAppKey(appKey)}.json");

    private static string GetKeyPrefix(string appKey) => $"Module.{appKey}.";

    private async Task EnsureDefinitionsAsync(ModuleSettingsDefinition definition, CancellationToken cancellationToken)
    {
        var prefix = GetKeyPrefix(definition.AppKey);
        var sortOrder = 0;
        foreach (var item in definition.Sections.SelectMany(x => x.Settings))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.UpsertDefinitionAsync(new SettingDefinition
            {
                Key = prefix + item.Key,
                DisplayName = item.Title,
                Description = item.Description,
                Category = "Module",
                Group = definition.AppKey.ToUpperInvariant(),
                Scope = SettingScope.App,
                ValueType = ToSettingValueType(item.Kind),
                DefaultValue = item.DefaultValue,
                IsRequired = item.IsRequired,
                RequiredPermission = "Settings.Edit",
                SortOrder = sortOrder++
            });
        }
    }

    private async Task SaveValuesToSettingsEngineAsync(
        ModuleSettingsDefinition definition,
        IReadOnlyDictionary<string, string> values,
        string reason,
        CancellationToken cancellationToken)
    {
        var prefix = GetKeyPrefix(definition.AppKey);
        var requests = definition.Sections
            .SelectMany(x => x.Settings)
            .Select(item => new SaveSettingValueRequest
            {
                Key = prefix + item.Key,
                Scope = SettingScope.App,
                ScopeTarget = definition.AppKey,
                Value = values.TryGetValue(item.Key, out var value) ? value : item.DefaultValue,
                Reason = reason
            })
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();
        await _settingsService.SaveValuesAsync(requests);
    }

    private async Task<Dictionary<string, string>> ReadLegacyValuesAsync(
        ModuleSettingsDefinition definition,
        CancellationToken cancellationToken)
    {
        var path = GetPath(definition.AppKey);
        if (!File.Exists(path))
        {
            var legacyPath = Path.Combine(AppContext.BaseDirectory, "Data", "Settings", $"{NormalizeAppKey(definition.AppKey)}.json");
            if (File.Exists(legacyPath))
            {
                path = legacyPath;
            }
        }
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        ModuleSettingsState? saved;
        try
        {
            await using var stream = File.OpenRead(path);
            saved = await JsonSerializer.DeserializeAsync<ModuleSettingsState>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            // A damaged legacy file must not prevent the module from opening.
            // Settings Engine defaults remain authoritative in this case.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            // A locked or unavailable legacy file is also non-authoritative.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        if (saved?.Values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var allowedKeys = definition.Sections
            .SelectMany(x => x.Settings)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return saved.Values
            .Where(x => allowedKeys.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static SettingValueType ToSettingValueType(ModuleSettingKind kind)
        => kind switch
        {
            ModuleSettingKind.Toggle or ModuleSettingKind.Permission => SettingValueType.Boolean,
            ModuleSettingKind.Number => SettingValueType.Number,
            ModuleSettingKind.Select => SettingValueType.Enum,
            _ => SettingValueType.String
        };

    private static ModuleSettingsState CreateDefaultState(ModuleSettingsDefinition definition)
        => new()
        {
            AppKey = definition.AppKey,
            Values = definition.Sections
                .SelectMany(x => x.Settings)
                .ToDictionary(x => x.Key, x => x.DefaultValue, StringComparer.OrdinalIgnoreCase),
            UpdatedAt = DateTime.UtcNow
        };

    private static IReadOnlyList<ModuleSettingsDefinition> BuildDefinitions()
    {
        static ModuleSettingDefinition Toggle(string key, string title, string description, bool value = true, string? dependsOn = null, string? disabledDescription = null)
            => new(key, title, description, ModuleSettingKind.Toggle, value ? "true" : "false", DependsOnModuleKey: dependsOn, DisabledDescription: disabledDescription);
        static ModuleSettingDefinition Text(string key, string title, string description, string value, bool required = false, string? disabledDescription = null)
            => new(key, title, description, ModuleSettingKind.Text, value, IsRequired: required, DisabledDescription: disabledDescription);
        static ModuleSettingDefinition Number(string key, string title, string description, string value, bool required = false, string? disabledDescription = null)
            => new(key, title, description, ModuleSettingKind.Number, value, IsRequired: required, DisabledDescription: disabledDescription);
        static ModuleSettingDefinition Select(string key, string title, string description, string value, params string[] options)
            => new(key, title, description, ModuleSettingKind.Select, value, options);
        static ModuleSettingDefinition Permission(string key, string title, string description, bool value = true)
            => new(key, title, description, ModuleSettingKind.Permission, value ? "true" : "false");
        static ModuleSettingSectionDefinition Section(string key, string title, string description, string icon, params ModuleSettingDefinition[] settings)
            => new(key, title, description, icon, settings);

        var definitions = new ModuleSettingsDefinition[]
        {
            new(ModuleKey.TrainingData, "training-data", "Dữ liệu mẫu", "Bật/tắt chức năng seed, reset và preflight dữ liệu mẫu", "\uE8B8",
            [
                Section("guards", "Bảo vệ thao tác", "Kiểm soát thao tác destructive và điều kiện an toàn", "\uE7BA",
                    Toggle("allowDestructiveReset", "Cho phép thao tác destructive", "Bật để cho phép chạy seed/reset dữ liệu mẫu", false),
                    Toggle("allowInProduction", "Cho phép trên Production", "Bật để cho phép thao tác destructive khi môi trường là Production", false),
                    Toggle("allowResetWithRealDataRisk", "Cho phép khi có rủi ro dữ liệu thật", "Bật để cho phép full reset ngay cả khi phát hiện dữ liệu nghiệp vụ không có marker training", false)),
                Section("confirm", "Chuỗi xác nhận", "Chuỗi xác nhận bắt buộc cho thao tác seed/reset", "\uE946",
                    Text("requireConfirmationText", "Chuỗi xác nhận mặc định", "Áp dụng cho các thao tác không dùng chuỗi xác nhận riêng", "RESET DATA", true))
            ]),
            new(ModuleKey.Dashboard, "dashboard", "Tổng quan", "Bảng điều khiển tổng quan", "\uE9D2",
            [
                Section("status", "Trạng thái module", "Bật/tắt hiển thị dashboard", "\uE73E",
                    Toggle("enabled", "Bật widget tổng quan", "Cho phép hiển thị các widget tổng quan"),
                    Toggle("pinWidgets", "Cho phép ghim widget", "Người dùng được ghim widget thường dùng"),
                    Toggle("hideMoneyWithoutPermission", "Ẩn số tiền theo quyền", "Ẩn số tiền với người dùng không có quyền")),
                Section("defaults", "Dữ liệu mặc định", "Widget và kỳ dữ liệu mặc định", "\uE8A5",
                    Select("defaultWidget", "Widget mặc định", "Widget mở đầu khi vào dashboard", "Doanh thu", "Doanh thu", "Công nợ phải thu", "Công nợ phải trả", "Tồn kho", "Phiếu thu/chi", "Lịch nhắc việc"),
                    Select("defaultRange", "Khoảng thời gian mặc định", "Kỳ dữ liệu mặc định", "Tháng này", "Hôm nay", "7 ngày", "Tháng này", "Quý này", "Năm nay"),
                    Select("refreshInterval", "Tự động làm mới dữ liệu", "Chu kỳ làm mới dashboard", "5 phút", "30 giây", "1 phút", "5 phút", "Tắt")),
                Section("alerts", "Kiểm tra & cảnh báo", "Cảnh báo nhanh trên dashboard", "\uE7BA",
                    Toggle("quickAlerts", "Cảnh báo nhanh", "Hiển thị công nợ, tồn kho, chứng từ cần xử lý"))
            ]),
            new(ModuleKey.Inventory, "inventory", "Quản lý xuất nhập tồn", "Theo dõi nhập, xuất, tồn kho", "\uE7C3",
            [
                Section("cost", "Quy tắc tồn kho", "Tính giá và xuất kho", "\uE8A5",
                    Select("costMethod", "Phương pháp tính giá xuất kho", "Cách tính giá vốn khi xuất", "Bình quân tức thời", "Bình quân tức thời", "FIFO"),
                    Toggle("allowNegativeStock", "Cho phép xuất âm kho", "Chỉ bật khi có quyền quản trị", false),
                    Toggle("syncToLedger", "Đồng bộ dữ liệu với Sổ cái", "Tự động chuyển phát sinh kho sang sổ cái", true, "general-ledger")),
                Section("code", "Quy tắc mã chứng từ/mã danh mục", "Mã hàng và chứng từ kho", "\uE8EC",
                    Toggle("autoProductCode", "Tự động sinh mã hàng", "Sinh mã khi thêm hàng hóa"),
                    Text("stockInCodePattern", "Quy tắc mã phiếu nhập", "Mẫu mã phiếu nhập", "PN-{yyyyMMdd}-{0000}", true),
                    Text("stockOutCodePattern", "Quy tắc mã phiếu xuất", "Mẫu mã phiếu xuất", "PX-{yyyyMMdd}-{0000}", true)),
                Section("defaults", "Dữ liệu mặc định", "Kho, đơn vị tính và làm tròn", "\uE713",
                    Text("defaultWarehouse", "Kho mặc định", "Kho dùng khi nhập/xuất", "KHO-001", true),
                    Text("defaultUnit", "Đơn vị tính mặc định", "Đơn vị mặc định", "Cái", true),
                    Select("roundingMode", "Làm tròn số lượng/số tiền", "Quy tắc làm tròn", "2 chữ số", "Không làm tròn", "2 chữ số", "Làm tròn đến đồng")),
                Section("control", "Kiểm tra & cảnh báo", "Ràng buộc nhập xuất", "\uE7BA",
                    Toggle("minStockAlert", "Cảnh báo tồn kho tối thiểu", "Báo khi tồn thấp"),
                    Toggle("maxStockAlert", "Cảnh báo tồn kho tối đa", "Báo khi vượt tồn tối đa"),
                    Toggle("checkDuplicateProductCode", "Kiểm tra trùng mã hàng", "Không cho trùng mã hàng"),
                    Toggle("requireWarehouse", "Bắt buộc chọn kho khi nhập/xuất", "Chứng từ phải có kho"),
                    Toggle("requireObject", "Bắt buộc chọn đối tượng khi nhập/xuất", "Chứng từ phải có khách hàng/NCC/đối tượng"))
            ]),
            new(ModuleKey.Receivable, "receivable", "Công nợ phải thu", "Theo dõi công nợ khách hàng", "\uE716",
            [
                Section("status", "Trạng thái module", "Theo dõi công nợ khách hàng", "\uE73E",
                    Toggle("enabled", "Bật theo dõi công nợ khách hàng", "Cho phép ghi nhận công nợ phải thu"),
                    Toggle("showOpeningBalance", "Hiển thị số dư đầu kỳ", "Hiện số dư đầu kỳ trên báo cáo")),
                Section("rules", "Quy tắc mã chứng từ/mã danh mục", "Mã phiếu và nhóm mặc định", "\uE8EC",
                    Text("debtCodePattern", "Quy tắc mã phiếu công nợ", "Mẫu mã đối chiếu/công nợ", "CNPT-{yyyyMMdd}-{0000}", true),
                    Text("defaultCustomerGroup", "Nhóm khách hàng mặc định", "Nhóm dùng khi tạo khách hàng", "Khách hàng thường", true)),
                Section("alerts", "Kiểm tra & cảnh báo", "Hạn thanh toán và hạn mức", "\uE7BA",
                    Number("paymentDays", "Hạn thanh toán mặc định", "Số ngày thanh toán", "30", true),
                    Number("defaultCreditLimit", "Hạn mức công nợ mặc định", "Hạn mức mặc định", "0", true),
                    Toggle("overdueAlert", "Cảnh báo nợ quá hạn", "Báo khách hàng quá hạn"),
                    Toggle("creditLimitAlert", "Cảnh báo vượt hạn mức công nợ", "Báo khi vượt hạn mức"),
                    Toggle("allowNegativeDebt", "Cho phép ghi nhận công nợ âm", "Cho phép số dư âm")),
                Section("sync", "Đồng bộ & ghi sổ", "Đối chiếu và chuyển Sổ cái", "\uE8A5",
                    Toggle("autoMatchReceipt", "Tự động đối chiếu phiếu thu với công nợ", "Tự khớp phiếu thu"),
                    Toggle("syncReceiptToLedger", "Đồng bộ phiếu thu sang Sổ cái", "Tạo bút toán từ phiếu thu", true, "general-ledger"))
            ]),
            new(ModuleKey.Payable, "payable", "Công nợ phải trả", "Theo dõi công nợ nhà cung cấp", "\uE8F9",
            [
                Section("status", "Trạng thái module", "Theo dõi công nợ nhà cung cấp", "\uE73E",
                    Toggle("enabled", "Bật theo dõi công nợ nhà cung cấp", "Cho phép ghi nhận công nợ phải trả"),
                    Toggle("showOpeningBalance", "Hiển thị số dư đầu kỳ", "Hiện số dư đầu kỳ trên báo cáo")),
                Section("rules", "Quy tắc mã chứng từ/mã danh mục", "Mã chứng từ phải trả", "\uE8EC",
                    Text("payableCodePattern", "Quy tắc mã chứng từ phải trả", "Mẫu mã công nợ phải trả", "CNPC-{yyyyMMdd}-{0000}", true),
                    Text("defaultSupplier", "Nhà cung cấp mặc định nếu cần", "NCC mặc định", "")),
                Section("alerts", "Kiểm tra & cảnh báo", "Hạn thanh toán và nợ đến hạn", "\uE7BA",
                    Number("paymentDays", "Hạn thanh toán mặc định", "Số ngày thanh toán", "30", true),
                    Toggle("dueAlert", "Cảnh báo nợ đến hạn", "Báo khoản nợ sắp đến hạn"),
                    Toggle("overdueAlert", "Cảnh báo nợ quá hạn", "Báo khoản nợ quá hạn"),
                    Toggle("allowNegativeDebt", "Cho phép công nợ âm", "Cho phép số dư âm")),
                Section("sync", "Đồng bộ & ghi sổ", "Đối chiếu và chuyển Sổ cái", "\uE8A5",
                    Toggle("autoMatchPayment", "Tự động đối chiếu phiếu chi với công nợ", "Tự khớp phiếu chi"),
                    Toggle("syncPaymentToLedger", "Đồng bộ phiếu chi sang Sổ cái", "Tạo bút toán từ phiếu chi", true, "general-ledger"))
            ]),
            new(ModuleKey.Customer, "customers", "Quản lý khách hàng", "Danh mục khách hàng", "\uE716",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", "Mã và nhóm khách hàng", "\uE8EC",
                    Toggle("autoCustomerCode", "Tự động sinh mã khách hàng", "Sinh mã khi thêm khách hàng nếu để trống mã"),
                    Text("customerCodePattern", "Quy tắc mã khách hàng", "Một token số dạng {00000}; hỗ trợ token ngày {yyyyMMdd}, {yyyy}, {MM}, {dd}.", "KH-{00000}", true),
                    Text("defaultCustomerGroup", "Nhóm khách hàng mặc định", "Nhóm mặc định", "Khách hàng thường", true)),
                Section("validate", "Kiểm tra & cảnh báo", "Thông tin bắt buộc và kiểm tra trùng", "\uE7BA",
                    Toggle("requirePhone", "Bắt buộc nhập số điện thoại", "Không cho lưu nếu thiếu điện thoại"),
                    Toggle("requireTaxCode", "Bắt buộc nhập mã số thuế", "Không cho lưu nếu thiếu MST", false),
                    Toggle("checkDuplicatePhone", "Kiểm tra trùng số điện thoại", "Không cho lưu nếu điện thoại đã thuộc khách hàng khác"),
                    Toggle("checkDuplicateTaxCode", "Kiểm tra trùng mã số thuế", "Không cho lưu nếu MST đã thuộc khách hàng khác"),
                    Toggle("lookupTaxInfo", "Tự động lấy thông tin doanh nghiệp từ mã số thuế", "Dùng service nếu có"),
                    Text("extraFields", "Trường mở rộng khách hàng", "Danh sách trường mở rộng", "Zalo, Người phụ trách")),
                Section("permission", "Phân quyền", "Quyền dữ liệu khách hàng", "\uE72E",
                    Permission("canView", "Quyền xem khách hàng", "Cho phép xem"),
                    Permission("canEdit", "Quyền sửa khách hàng", "Cho phép sửa"),
                    Permission("canDelete", "Quyền xóa khách hàng", "Cho phép xóa"),
                    Toggle("allowLockCustomer", "Cho phép khóa khách hàng ngừng giao dịch", "Ngăn phát sinh mới"))
            ]),
            new(ModuleKey.Supplier, "suppliers", "Quản lý nhà cung cấp", "Danh mục nhà cung cấp", "\uE8F9",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", "Mã và nhóm nhà cung cấp", "\uE8EC",
                    Toggle("autoSupplierCode", "Tự động sinh mã nhà cung cấp", "Sinh mã khi thêm NCC nếu để trống mã"),
                    Text("supplierCodePattern", "Quy tắc mã nhà cung cấp", "Một token số dạng {00000}; hỗ trợ token ngày {yyyyMMdd}, {yyyy}, {MM}, {dd}.", "NCC-{00000}", true),
                    Text("defaultSupplierGroup", "Nhóm nhà cung cấp mặc định", "Nhóm mặc định", "Nhà cung cấp thường", true)),
                Section("validate", "Kiểm tra & cảnh báo", "Thông tin bắt buộc và kiểm tra trùng", "\uE7BA",
                    Toggle("requirePhone", "Bắt buộc nhập số điện thoại", "Không cho lưu nếu thiếu điện thoại"),
                    Toggle("requireTaxCode", "Bắt buộc nhập mã số thuế", "Không cho lưu nếu thiếu MST", false),
                    Toggle("checkDuplicateTaxCode", "Kiểm tra trùng mã số thuế", "Không cho lưu nếu MST đã thuộc nhà cung cấp khác"),
                    Toggle("lookupTaxInfo", "Tự động lấy thông tin doanh nghiệp từ mã số thuế", "Dùng service nếu có")),
                Section("permission", "Phân quyền", "Quyền dữ liệu nhà cung cấp", "\uE72E",
                    Permission("canView", "Quyền xem nhà cung cấp", "Cho phép xem"),
                    Permission("canEdit", "Quyền sửa nhà cung cấp", "Cho phép sửa"),
                    Permission("canDelete", "Quyền xóa nhà cung cấp", "Cho phép xóa"),
                    Toggle("allowLockSupplier", "Cho phép khóa nhà cung cấp ngừng giao dịch", "Ngăn phát sinh mới"))
            ]),
            new(ModuleKey.Product, "items", "Quản lý hàng hóa", "Danh mục hàng hóa", "\uE7C3",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", "Mã hàng và nhóm hàng", "\uE8EC",
                    Toggle("autoProductCode", "Tự động sinh mã hàng", "Sinh mã hàng hóa"),
                    Text("productCodePattern", "Quy tắc mã hàng", "Mẫu mã hàng", "HH-{00000}", true),
                    Text("defaultProductGroup", "Nhóm hàng mặc định", "Nhóm mặc định", "Hàng hóa", true)),
                Section("defaults", "Dữ liệu mặc định", "Đơn vị, thuế và giá", "\uE713",
                    Text("defaultUnit", "Đơn vị tính mặc định", "Đơn vị mặc định", "Cái", true),
                    Select("defaultVat", "Thuế VAT mặc định", "Áp dụng cho hàng mới khi để trống VAT; chọn 0% để giữ mức miễn/không chịu thuế.", "10%", "0%", "5%", "8%", "10%"),
                    Select("defaultPriceMode", "Cấu hình giá bán mặc định", "Nguồn giá bán", "Giá bán lẻ", "Giá bán lẻ", "Giá bán sỉ", "Theo bảng giá")),
                Section("control", "Kiểm tra & cảnh báo", "Kiểm soát hàng hóa", "\uE7BA",
                    Toggle("trackInventory", "Quản lý tồn kho theo hàng", "Theo dõi tồn kho"),
                    Toggle("trackBatchExpiry", "Quản lý lô/hạn sử dụng", "Theo dõi lô và hạn dùng", false),
                    Toggle("trackSerialImei", "Quản lý serial/IMEI", "Theo dõi serial", false),
                    Toggle("checkDuplicateCodeNameBarcode", "Cảnh báo trùng mã hàng/tên hàng/barcode", "Không cho trùng dữ liệu chính"),
                    Toggle("multiUnit", "Cho phép nhiều đơn vị tính", "Quy đổi nhiều đơn vị"),
                    Toggle("allowProductImage", "Cho phép ảnh hàng hóa", "Lưu ảnh sản phẩm"))
            ]),
            new(ModuleKey.Warehouse, "warehouses", "Quản lý kho", "Danh mục kho và vị trí lưu trữ", "\uE8B7",
            [
                Section("defaults", "Dữ liệu mặc định", "Kho mặc định", "\uE713",
                    Text("defaultWarehouse", "Tạo kho mặc định", "Kho chính", "KHO-001", true),
                    Text("defaultSaleWarehouse", "Kho bán hàng mặc định", "Kho xuất bán", "KHO-001", true),
                    Text("defaultInWarehouse", "Kho nhập hàng mặc định", "Kho nhập", "KHO-001", true)),
                Section("rules", "Quy tắc mã chứng từ/mã danh mục", "Mã kiểm kê/chuyển kho", "\uE8EC",
                    Text("warehouseCodePattern", "Quy tắc mã kho", "Mẫu mã kho", "KHO-{000}", true),
                    Text("inventoryCheckCodePattern", "Quy tắc mã phiếu kiểm kê", "Mẫu phiếu kiểm kê", "KK-{yyyyMMdd}-{0000}", true),
                    Text("transferCodePattern", "Quy tắc mã phiếu chuyển kho", "Mẫu phiếu chuyển kho", "CK-{yyyyMMdd}-{0000}", true)),
                Section("control", "Kiểm tra & cảnh báo", "Kiểm soát kho", "\uE7BA",
                    Toggle("multiWarehouse", "Cho phép nhiều kho", "Quản lý nhiều kho"),
                    Toggle("allowTransfer", "Cho phép chuyển kho", "Cho phép điều chuyển"),
                    Toggle("requireStocktakeBeforeClosing", "Bắt buộc kiểm kê trước khi khóa kỳ", "Kiểm kê trước khóa kỳ"),
                    Toggle("stockDifferenceAlert", "Cảnh báo lệch tồn kho", "Báo lệch tồn")),
                Section("permission", "Phân quyền", "Quyền theo kho", "\uE72E",
                    Permission("warehousePermission", "Phân quyền theo kho", "Cho phép thiết lập quyền kho"),
                    Toggle("limitEmployeeWarehouse", "Nhân viên chỉ xem kho được phân quyền", "Giới hạn dữ liệu theo quyền"))
            ]),
            new(ModuleKey.DepartmentHR, "departments", "Bộ phận & Nhân sự", "Phòng ban, nhân viên và người duyệt", "\uE77B",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", "Mã nhân viên", "\uE8EC",
                    Toggle("autoEmployeeCode", "Tự động sinh mã nhân viên", "Sinh mã nhân viên"),
                    Text("employeeCodePattern", "Quy tắc mã nhân viên", "Mẫu mã nhân viên", "NV-{00000}", true)),
                Section("defaults", "Dữ liệu mặc định", "Phòng ban và trạng thái", "\uE713",
                    Text("defaultDepartment", "Phòng ban mặc định", "Phòng ban khi thêm nhân sự", "Hành chính", true),
                    Text("defaultPosition", "Chức vụ mặc định", "Chức vụ khi thêm nhân sự", "Nhân viên", true),
                    Select("defaultHrStatus", "Trạng thái nhân sự mặc định", "Trạng thái mới", "Đang làm việc", "Đang làm việc", "Thử việc", "Nghỉ việc")),
                Section("control", "Kiểm tra & cảnh báo", "Thông tin bắt buộc", "\uE7BA",
                    Toggle("requirePhoneEmail", "Bắt buộc số điện thoại/email", "Không lưu nếu thiếu liên hệ"),
                    Toggle("allowLockInactiveEmployee", "Cho phép khóa nhân sự nghỉ việc", "Ngăn đăng nhập/phát sinh")),
                Section("permission", "Phân quyền", "Phân quyền và duyệt chứng từ", "\uE72E",
                    Permission("departmentPermission", "Phân quyền theo bộ phận", "Giới hạn dữ liệu theo bộ phận"),
                    Text("departmentApprover", "Người duyệt chứng từ theo bộ phận", "Thiết lập người duyệt mặc định", "Trưởng bộ phận"))
            ]),
            Voucher(ModuleKey.ReceiptVoucher, "payment-receipt", "Phiếu thu", "PT-{yyyyMMdd}-{0000}", "Quỹ/tài khoản tiền mặt mặc định", "111", "Tài khoản ngân hàng mặc định", "112", "Bắt buộc chọn khách hàng/đối tượng", "Bắt buộc lý do thu", "Cho phép thu nhiều hóa đơn/công nợ", "Tự động hạch toán sang Sổ cái", "Mẫu in phiếu thu mặc định", "PT-A5", "Bật duyệt phiếu thu trước khi ghi sổ"),
            Voucher(ModuleKey.PaymentVoucher, "payment-voucher", "Phiếu chi", "PC-{yyyyMMdd}-{0000}", "Quỹ/tài khoản tiền mặt mặc định", "111", "Tài khoản ngân hàng mặc định", "112", "Bắt buộc chọn nhà cung cấp/đối tượng", "Bắt buộc lý do chi", "Cho phép chi nhiều hóa đơn/công nợ", "Tự động hạch toán sang Sổ cái", "Mẫu in phiếu chi mặc định", "PC-A5", "Bật duyệt phiếu chi trước khi ghi sổ"),
            StockVoucher(ModuleKey.StockInVoucher, "receipts", "Phiếu nhập", "PN-{yyyyMMdd}-{0000}", "Kho nhập mặc định", "", "Nhà cung cấp mặc định", "", "Bắt buộc chọn kho", "Bắt buộc chọn nhà cung cấp", "Cho phép nhập giá vốn bằng 0", "Tự động cập nhật giá nhập gần nhất", "Tự động cập nhật tồn kho", "Tự động hạch toán sang Sổ cái", "Mẫu in phiếu nhập mặc định", "PN-A4", "Bật duyệt phiếu nhập"),
            StockVoucher(ModuleKey.StockOutVoucher, "issues", "Phiếu xuất", "PX-{yyyyMMdd}-{0000}", "Kho xuất mặc định", "", "Khách hàng/đối tượng mặc định", "", "Bắt buộc chọn kho", "Bắt buộc chọn khách hàng/đối tượng", "Cho phép xuất âm kho theo quyền", "Tự động tính giá vốn", "Tự động cập nhật tồn kho", "Tự động hạch toán sang Sổ cái", "Mẫu in phiếu xuất mặc định", "PX-A4", "Bật duyệt phiếu xuất")
        };

        // These module forms currently have no runtime consumer for their
        // Module.* values. Keep the legacy values intact, but do not present
        // the editor as if changing them affects operational behavior.
        var runtimeUnwiredAppKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "warehouses",
            "dashboard", "receivable", "payable"
        };
        var runtimeUnwiredSettingKeysByAppKey = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["items"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "defaultPriceMode",
                "trackInventory", "trackBatchExpiry", "trackSerialImei", "checkDuplicateCodeNameBarcode",
                "multiUnit", "allowProductImage"
            },
            ["customers"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "defaultCustomerGroup", "lookupTaxInfo",
                "extraFields", "canView", "canEdit", "canDelete", "allowLockCustomer"
            },
            ["suppliers"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "defaultSupplierGroup", "lookupTaxInfo",
                "canView", "canEdit", "canDelete", "allowLockSupplier"
            },
            ["departments"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "defaultHrStatus", "requirePhoneEmail", "allowLockInactiveEmployee",
                "departmentPermission", "departmentApprover"
            },
            ["inventory"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "allowNegativeStock", "syncToLedger", "autoProductCode", "stockInCodePattern", "stockOutCodePattern",
                "defaultWarehouse", "defaultUnit", "roundingMode", "minStockAlert", "maxStockAlert",
                "checkDuplicateProductCode", "requireWarehouse", "requireObject"
            }
        };
        const string runtimeUnwiredReason = "Chưa có luồng nghiệp vụ đọc và áp dụng thiết lập module này; giá trị cũ được giữ nguyên nhưng hiện không thể chỉnh sửa.";

        return definitions.Select(definition => runtimeUnwiredAppKeys.Contains(definition.AppKey)
            || runtimeUnwiredSettingKeysByAppKey.ContainsKey(definition.AppKey)
            ? definition with
            {
                Sections = definition.Sections.Select(section => section with
                {
                    Settings = section.Settings.Select(setting =>
                    {
                        var hasNoRuntimeConsumer = runtimeUnwiredAppKeys.Contains(definition.AppKey)
                            || (runtimeUnwiredSettingKeysByAppKey.TryGetValue(definition.AppKey, out var keys)
                                && keys.Contains(setting.Key));
                        return hasNoRuntimeConsumer
                            ? setting with { DisabledDescription = setting.DisabledDescription ?? runtimeUnwiredReason }
                            : setting;
                    }).ToArray()
                }).ToArray()
            }
            : definition).ToArray();

        ModuleSettingsDefinition Voucher(ModuleKey key, string appKey, string title, string codePattern, string cashTitle, string cashDefault, string bankTitle, string bankDefault, string requireObjectTitle, string requireReasonTitle, string multiTitle, string syncTitle, string printTitle, string printDefault, string approvalTitle)
            => new(key, appKey, title, $"Thiết lập {title.ToLowerInvariant()}", "\uE8A5",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", $"Quy tắc mã {title.ToLowerInvariant()}", "\uE8EC",
                    Text("voucherCodePattern", $"Quy tắc mã {title.ToLowerInvariant()}", "Mẫu mã chứng từ", codePattern, true)),
                Section("defaults", "Dữ liệu mặc định", "Tài khoản, người lập và người duyệt", "\uE713",
                    Text("defaultCashAccount", cashTitle, "Tài khoản tiền mặt", cashDefault, true,
                        "Luồng phiếu chưa đọc thiết lập tài khoản mặc định này; tài khoản hiện được lấy từ cấu hình nghiệp vụ khác."),
                    Text("defaultBankAccount", bankTitle, "Tài khoản ngân hàng", bankDefault, true,
                        "Luồng phiếu chưa đọc thiết lập tài khoản mặc định này; tài khoản hiện được lấy từ cấu hình nghiệp vụ khác."),
                    Text("defaultCreator", "Người lập mặc định", "Người lập chứng từ", "Kế toán",
                        disabledDescription: "Người lập được xác định theo tài khoản đăng nhập; chưa hỗ trợ ghi đè bằng cấu hình module."),
                    Text("defaultApprover", "Người duyệt mặc định", "Người duyệt chứng từ", "Kế toán trưởng",
                        disabledDescription: "Người duyệt được xác định theo quy trình/phân quyền; chưa hỗ trợ chỉ định bằng cấu hình module.")),
                Section("control", "Kiểm tra & cảnh báo", "Ràng buộc chứng từ", "\uE7BA",
                    Toggle("requireObject", requireObjectTitle, "Bắt buộc chọn đối tượng",
                        disabledDescription: "Chưa được áp dụng trong luồng phiếu thu/chi hiện tại; tùy chọn được giữ nguyên nhưng không thể chỉnh sửa."),
                    Toggle("requireReason", requireReasonTitle, "Bắt buộc nhập lý do",
                        disabledDescription: "Chưa được áp dụng trong luồng phiếu thu/chi hiện tại; tùy chọn được giữ nguyên nhưng không thể chỉnh sửa."),
                    Toggle("allowMultipleDebts", multiTitle, "Cho phép xử lý nhiều hóa đơn/công nợ",
                        disabledDescription: "Chưa có luồng phân bổ nhiều hóa đơn được điều khiển bởi thiết lập này.")),
                Section("sync", "Đồng bộ & ghi sổ", "Hạch toán sang Sổ cái", "\uE8A5",
                    Toggle("syncToLedger", syncTitle, "Tạo bút toán tự động", true, "general-ledger",
                        "Luồng ghi sổ hiện được điều khiển bởi quy trình chứng từ; tùy chọn này chưa có hành vi bật/tắt an toàn."),
                    Toggle("requireApproval", approvalTitle, "Duyệt trước khi ghi sổ",
                        disabledDescription: "Quy trình duyệt hiện chưa được cấu hình theo tùy chọn này; chưa thể tắt hoặc thay đổi bằng thiết lập module.")),
                Section("print", "In ấn & mẫu biểu", "Mẫu in và số liên", "\uE749",
                    Text("defaultPrintTemplate", printTitle, "Mẫu in mặc định", printDefault, true,
                        "Màn hình in chưa đọc mẫu in từ thiết lập module; mẫu được chọn theo danh mục biểu mẫu hiện hành."),
                    Number("printCopies", "Số liên in mặc định", "Số liên khi in", "2", true,
                        "Số liên hiện do màn hình in quyết định; chưa đọc thiết lập này."))
            ]);

        ModuleSettingsDefinition StockVoucher(ModuleKey key, string appKey, string title, string codePattern, string warehouseTitle, string warehouseDefault, string objectTitle, string objectDefault, string requireWarehouseTitle, string requireObjectTitle, string allowSpecialTitle, string updatePriceOrCostTitle, string updateStockTitle, string syncTitle, string printTitle, string printDefault, string approvalTitle)
            => new(key, appKey, title, $"Thiết lập {title.ToLowerInvariant()}", "\uE7C3",
            [
                Section("code", "Quy tắc mã chứng từ/mã danh mục", $"Quy tắc mã {title.ToLowerInvariant()}", "\uE8EC",
                    Text("voucherCodePattern", $"Quy tắc mã {title.ToLowerInvariant()}", "Mẫu mã chứng từ", codePattern, true)),
                Section("defaults", "Dữ liệu mặc định", "Kho và đối tượng mặc định", "\uE713",
                    Text("defaultWarehouse", warehouseTitle, "Mã kho thật dùng làm mặc định; để trống sẽ dùng kho đầu tiên trong danh mục.", warehouseDefault),
                    Text("defaultObject", objectTitle, "Đối tượng mặc định", objectDefault,
                        disabledDescription: "Chưa được áp dụng trong luồng phiếu nhập/xuất hiện tại; cấu hình này được giữ nguyên nhưng không thể chỉnh sửa.")),
                Section("control", "Kiểm tra & cảnh báo", "Ràng buộc chứng từ kho", "\uE7BA",
                    Toggle("requireWarehouse", requireWarehouseTitle, "Bắt buộc chọn kho", disabledDescription: "Kho hiện vẫn được yêu cầu theo quy tắc nghiệp vụ; tùy chọn này chưa thay đổi được quy tắc."),
                    Toggle("requireObject", requireObjectTitle, "Bắt buộc chọn đối tượng", disabledDescription: "Chưa được áp dụng trong luồng phiếu hiện tại; cấu hình này được giữ nguyên nhưng không thể chỉnh sửa."),
                    Toggle("allowSpecialStock", allowSpecialTitle, "Cho phép nghiệp vụ đặc biệt", key == ModuleKey.StockInVoucher, disabledDescription: "Chưa có kiểm tra quyền và luồng nghiệp vụ tương ứng; tùy chọn đang bị khóa để tránh bật ngoài ý muốn."),
                    Toggle("updatePriceOrCost", updatePriceOrCostTitle, "Cập nhật giá tự động", disabledDescription: "Chưa được áp dụng trong luồng phiếu hiện tại; cấu hình này được giữ nguyên nhưng không thể chỉnh sửa."),
                    Toggle("updateStock", updateStockTitle, "Cập nhật tồn kho", disabledDescription: "Việc cập nhật tồn kho hiện do luồng ghi sổ thực hiện; tùy chọn này chưa điều khiển được luồng đó.")),
                Section("sync", "Đồng bộ & ghi sổ", "Hạch toán sang Sổ cái", "\uE8A5",
                    Toggle("syncToLedger", syncTitle, "Tạo bút toán tự động", true, "general-ledger", "Luồng ghi sổ hiện luôn tạo bút toán khi duyệt; chưa có quy trình ghi sổ/bù trừ an toàn để cho phép tắt."),
                    Toggle("requireApproval", approvalTitle, "Duyệt chứng từ", disabledDescription: "Quy trình hiện yêu cầu duyệt trước khi ghi sổ; chưa hỗ trợ tắt bước duyệt.")),
                Section("print", "In ấn & mẫu biểu", "Mẫu in", "\uE749",
                    Text("defaultPrintTemplate", printTitle, "Mẫu in mặc định", printDefault, true,
                        "Mẫu in mặc định chưa được đọc bởi màn hình phiếu; cấu hình này được giữ nguyên nhưng chưa thể chỉnh sửa."))
            ]);
    }
}

