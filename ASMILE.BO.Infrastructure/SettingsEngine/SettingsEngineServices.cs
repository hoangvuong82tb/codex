using ASMILE.BO.Shared.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Shared.Storage;
using System.IO;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

public sealed class SettingsRegistry : ISettingsRegistry
{
    public IReadOnlyList<SettingDefinitionDto> GetDefaultDefinitions()
    {
        var rows = new List<SettingDefinitionDto>();
        const string runtimeProviderPending = "Chỉ đọc: chưa có provider runtime trong phiên bản này; thay đổi chưa được áp dụng vào nghiệp vụ thật.";
        void Add(string key, string name, string category, string group, SettingScope scope, SettingValueType type, string? defaultValue, bool required = false, bool sensitive = false, bool restart = false, string? permission = "Settings.Edit", bool readOnly = false, string? description = null)
            => rows.Add(new SettingDefinitionDto { Key = key, DisplayName = name, Description = description, Category = category, Group = group, Scope = scope, ValueType = type, DefaultValue = defaultValue, IsRequired = required, IsSensitive = sensitive, IsReadOnly = readOnly, RequiresRestart = restart, RequiredPermission = permission });
        void AddReadOnly(string key, string name, string category, string group, SettingScope scope, SettingValueType type, string? defaultValue, bool required = false, bool sensitive = false, bool restart = false, string? permission = "Settings.Edit", string? description = null)
            => Add(key, name, category, group, scope, type, defaultValue, required, sensitive, restart, permission, readOnly: true, description: description ?? runtimeProviderPending);

        const string activeDatabaseConfiguration = "Đọc từ cấu hình kết nối đang dùng bởi tiến trình; chỉ đọc, thay đổi ở đây không cấu hình lại kết nối.";
        const string accountingPeriodProvider = "Đọc từ kỳ kế toán đã lưu trong AccountingPeriods; hiển thị chưa cấu hình nếu chưa có kỳ hiện hành.";
        AddReadOnly("Database.ServerName", "Server name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, @".\SQLEXPRESS", true, description: activeDatabaseConfiguration);
        AddReadOnly("Database.DatabaseName", "Database name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, "ASMILE_BO", true, description: activeDatabaseConfiguration);
        AddReadOnly("Database.AuthenticationMode", "Authentication mode", "Database", "DATABASE", SettingScope.System, SettingValueType.Enum, "Sql", description: activeDatabaseConfiguration);
        AddReadOnly("Database.UserName", "User name", "Database", "DATABASE", SettingScope.System, SettingValueType.String, "sa", description: activeDatabaseConfiguration);
        AddReadOnly("Database.Password", "Password", "Database", "DATABASE", SettingScope.System, SettingValueType.Password, string.Empty, false, true, description: "Chỉ hiển thị trạng thái đã cấu hình; mật khẩu thật không bao giờ được trả về màn hình.");
        AddReadOnly("Database.ConnectionString", "Connection string", "Database", "DATABASE", SettingScope.System, SettingValueType.ConnectionString, "", false, true, description: "Trạng thái lấy từ provider runtime; chuỗi kết nối và thông tin xác thực được ẩn.");
        const string databaseExecutionPolicyUnavailable = "Chưa có provider runtime áp dụng chính sách này; không hiển thị giá trị mặc định như cấu hình đang có hiệu lực.";
        Add("Database.CommandTimeout", "Command timeout (s)", "Database", "DATABASE", SettingScope.System, SettingValueType.Number, "30",
            description: "Thời gian chờ tối đa cho mỗi lệnh EF Core gửi tới SQL Server. Áp dụng runtime ngay sau khi lưu; các thao tác backup/maintenance có timeout riêng.");
        AddReadOnly("Database.EnableAutoMigration", "Enable auto migration", "Database", "DATABASE", SettingScope.System, SettingValueType.Boolean, null,
            description: databaseExecutionPolicyUnavailable);
        const string endpointProbeConfiguration = "Được lưu trong Settings Engine và dùng để probe endpoint từ Virtual File Center; không khởi chạy hoặc thay đổi cấu hình dịch vụ API trên máy chủ.";
        Add("Network.ApiBaseUrl", "API Base URL", "Network", "NETWORK", SettingScope.System, SettingValueType.String, string.Empty, description: endpointProbeConfiguration);
        Add("Network.ApiPort", "API Port", "Network", "NETWORK", SettingScope.System, SettingValueType.Number, "5001", description: endpointProbeConfiguration);
        Add("Network.EnableHttps", "Enable HTTPS", "Network", "NETWORK", SettingScope.System, SettingValueType.Boolean, "true", description: endpointProbeConfiguration);
        const string clientDiscoveryUnavailable = "Chưa có dịch vụ discovery/sync tiêu thụ cấu hình này; để trống cho đến khi provider runtime được triển khai.";
        AddReadOnly("Network.AllowClientDiscovery", "Allow client discovery", "Network", "NETWORK", SettingScope.System, SettingValueType.Boolean, null,
            description: clientDiscoveryUnavailable);
        AddReadOnly("Network.ScanIntervalSeconds", "Scan interval seconds", "Network", "NETWORK", SettingScope.System, SettingValueType.Number, null,
            description: clientDiscoveryUnavailable);
        Add("Storage.DataRootPath", "Data root path", "Storage", "STORAGE", SettingScope.System, SettingValueType.Path, SettingsStoragePaths.DefaultDataRoot, true,
            description: "Thư mục dữ liệu vật lý mặc định của ASMILE trên máy chủ; không dùng ký tự ổ logic của VFC làm đường dẫn.");
        Add("Storage.ExportRootPath", "Export root path", "Storage", "STORAGE", SettingScope.System, SettingValueType.Path, ApplicationDataPaths.DefaultUserExportsDirectory, true,
            description: "Thư mục vật lý nhận các file xuất báo cáo/chứng từ của ASMILE. Đường dẫn được áp dụng ngay và dùng chung bởi các luồng export; đảm bảo tài khoản ứng dụng có quyền ghi.");
        Add("VFC.EnableVirtualDrives", "Enable virtual drives", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        const string optionalVfcPhysicalPath = "Ổ VFC là danh mục logic của ứng dụng, không phải ổ đĩa Windows. Để trống nếu không gắn thư mục file vật lý; chỉ nhập đường dẫn thật đã được cấp quyền khi cần backup file tại vị trí đó.";
        Add("VFC.SystemDrivePath", "System drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, string.Empty,
            description: optionalVfcPhysicalPath);
        Add("VFC.AccountingDrivePath", "Accounting drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, string.Empty,
            description: optionalVfcPhysicalPath);
        Add("VFC.SalesDrivePath", "Sales drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, string.Empty,
            description: optionalVfcPhysicalPath);
        Add("VFC.HrDrivePath", "HR drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, string.Empty,
            description: optionalVfcPhysicalPath);
        Add("VFC.LogDrivePath", "Log drive path", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Path, string.Empty,
            description: optionalVfcPhysicalPath);
        Add("VFC.SystemDriveEnabled", "System drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.AccountingDriveEnabled", "Accounting drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.SalesDriveEnabled", "Sales drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.HrDriveEnabled", "HR drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("VFC.LogDriveEnabled", "Log drive enabled", "VirtualFileCenter", "VIRTUAL FILE CENTER", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Backup.EnableAutoBackup", "Enable auto backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "false",
            description: "Mặc định tắt cho đến khi đã provision quyền ghi, chạy và xác minh thành công một backup thật; chỉ bật lịch sau bước kiểm tra đó.");
        Add("Backup.BackupPath", "Backup path", "Backup", "BACKUP", SettingScope.System, SettingValueType.Path, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ASMILE-BO", "Backups"), true,
            description: "Thư mục chung của máy chủ, mặc định đồng bộ với bước provision. Đường dẫn được tiến trình SQL Server ghi file .bak. Trạng thái Accessible trên màn hình chỉ xác nhận quyền của ứng dụng; hãy chạy backup thật để kiểm tra quyền của tài khoản dịch vụ SQL Server. Nếu SQL Server ở máy khác, dùng share UNC mà cả ứng dụng và dịch vụ SQL Server đều truy cập được.");
        Add("Backup.Schedule", "Schedule", "Backup", "BACKUP", SettingScope.System, SettingValueType.String, "0 0 * * *", true,
            description: "Cron 5 trường theo UTC: phút giờ ngày-tháng tháng ngày-tuần. Theo Vixie crontab, ngày-tháng/ngày-tuần là OR nếu cả hai không chứa *; hỗ trợ bước, khoảng, danh sách; Chủ nhật là 0 hoặc 7.");
        Add("Backup.RetentionCount", "Retention count", "Backup", "BACKUP", SettingScope.System, SettingValueType.Number, "10");
        Add("Backup.CompressBackup", "Compress backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "true");
        Add("Backup.VerifyAfterBackup", "Verify after backup", "Backup", "BACKUP", SettingScope.System, SettingValueType.Boolean, "true");
        // Keep this visible for deployment diagnostics, but do not expose a
        // writable value until an update provider is registered. Persisting a
        // channel without an updater would make the setting look effective
        // while the shipped application continues to use the same channel.
        Add("Maintenance.UpdateChannel", "Update channel", "Maintenance", "MAINTENANCE", SettingScope.System, SettingValueType.Enum, "Stable", required: true,
            description: "Kênh manifest có chữ ký dùng khi kiểm tra cập nhật. Chọn kênh không tự tải hoặc cài đặt bản mới.");
        Add("Security.MaxFailedLoginAttempts", "Max failed login attempts", "Security", "SECURITY", SettingScope.System, SettingValueType.Number, "5");
        Add("Security.SessionTimeoutMinutes", "Session timeout", "Security", "SECURITY", SettingScope.System, SettingValueType.Number, "30",
            description: "Thời gian không có thao tác của người dùng; phiên được gia hạn khi có hoạt động và kết thúc khi hết hạn.");
        Add("Security.RequireAdminConfirmation", "Require admin confirmation", "Security", "SECURITY", SettingScope.System, SettingValueType.Boolean, "true",
            description: "Khi bật, khôi phục database/file yêu cầu xác nhận bổ sung sau quyền Database.Restore. Khi tắt, vẫn kiểm tra quyền và ghi audit; chỉ bỏ hộp xác nhận.");
        Add("System.MaintenanceMode", "Maintenance mode", "System", "SYSTEM", SettingScope.System, SettingValueType.Boolean, "false",
            permission: "System.Maintenance", description: "Chặn đăng nhập mới và đưa phiên hiện tại không có quyền bảo trì về màn hình đăng nhập trong tối đa 30 giây. Hãy lưu công việc trước khi bật.");
        Add("Logs.LogLevel", "Log level", "Logs", "LOGS", SettingScope.System, SettingValueType.Enum, "Information", required: true,
            description: "Ngưỡng log tối thiểu của ứng dụng; thay đổi được áp dụng ngay. Log mức Microsoft/System vẫn giữ override Warning để hạn chế nhiễu.");
        Add("Logs.RetentionDays", "Retention days", "Logs", "LOGS", SettingScope.System, SettingValueType.Number, "14", required: true,
            description: "Số ngày giữ log ứng dụng dạng rolling theo ngày (1–3650). Áp dụng ngay cho file asmile-bo-*.log trong thư mục log riêng của ASMILE; không xóa audit log hoặc file ngoài thư mục này.");
        AddReadOnly("Logs.EnableAuditLog", "Enable audit log", "Logs", "LOGS", SettingScope.System, SettingValueType.Boolean, "true",
            required: true, description: "Nhật ký kiểm toán được ghi vào AuditLogs bởi SecurityAuditService và luôn bật; không hỗ trợ tắt tại màn hình này.");
        AddReadOnly("Logs.EnableErrorLog", "Enable error log", "Logs", "LOGS", SettingScope.System, SettingValueType.Boolean, "false",
            description: "Trạng thái phản ánh sink file Serilog của tiến trình; lỗi được ghi trong log ứng dụng nếu sink file đang cấu hình.");
        Add("Appearance.Theme", "Theme", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Enum, "Light");
        Add("Appearance.WallpaperPath", "Wallpaper path", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Path, "");
        Add("Appearance.ShowDesktopIcons", "Show desktop icons", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Boolean, "true");
        Add("Appearance.ShowTaskbar", "Show taskbar", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Boolean, "true");
        Add("Appearance.SnapDesktopIconsToGrid", "Snap desktop icons to grid", "Appearance", "APPEARANCE", SettingScope.User, SettingValueType.Boolean, "true");

        AddReadOnly("ImportVoucher.CodePrefix", "Import code prefix", "Apps", "IMPORT VOUCHER", SettingScope.App, SettingValueType.String, "PN");
        AddReadOnly("ImportVoucher.AutoGenerateCode", "Auto generate code", "Apps", "IMPORT VOUCHER", SettingScope.App, SettingValueType.Boolean, "true");
        AddReadOnly("ExportVoucher.CodePrefix", "Export code prefix", "Apps", "EXPORT VOUCHER", SettingScope.App, SettingValueType.String, "PX");
        Add("Warehouse.LowStockThreshold", "Low stock threshold", "Apps", "WAREHOUSE", SettingScope.App, SettingValueType.Number, "20");
        Add("Warehouse.LowStockAlertRecipients", "Low-stock alert recipients", "Apps", "WAREHOUSE", SettingScope.App, SettingValueType.String, string.Empty);
        Add("GeneralLedger.CashAccount", "Số tài khoản tiền mặt", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "111");
        Add("GeneralLedger.BankAccount", "Số tài khoản ngân hàng", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "112");
        Add("GeneralLedger.ReceivableAccount", "Số tài khoản phải thu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "131");
        Add("GeneralLedger.InventoryAccount", "Số tài khoản hàng tồn kho", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "156");
        Add("GeneralLedger.PayableAccount", "Số tài khoản phải trả", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "331");
        Add("GeneralLedger.RevenueAccount", "Số tài khoản doanh thu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "511");
        Add("GeneralLedger.CostOfGoodsSoldAccount", "Số tài khoản giá vốn", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "632");
        Add("GeneralLedger.AdjustmentAccount", "Số tài khoản điều chỉnh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "338");
        AddReadOnly("GeneralLedger.DataSource", "Nguồn dữ liệu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "SQL Server");
        AddReadOnly("GeneralLedger.DatabaseName", "Tên database", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "ASMILE_BO", true);
        AddReadOnly("GeneralLedger.CurrentPeriod", "Kỳ kế toán hiện tại", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, true, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.PostingMethod", "Phương pháp ghi sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Nhật ký chung");
        AddReadOnly("GeneralLedger.RoundingMode", "Chế độ làm tròn tiền", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Làm tròn đến đồng");
        AddReadOnly("GeneralLedger.DefaultCurrency", "Tiền tệ mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "VND", true);
        AddReadOnly("GeneralLedger.AllowVoiceRecording", "Cho phép ghi âm", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        AddReadOnly("GeneralLedger.AllowEditPostedEntries", "Cho phép sửa bút toán đã ghi sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        AddReadOnly("GeneralLedger.AllowDeleteEntries", "Cho phép xóa bút toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "false");
        AddReadOnly("GeneralLedger.RequireBalancedBeforeSave", "Bắt buộc cân đối Nợ/Có trước khi lưu", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        AddReadOnly("GeneralLedger.AllowQuickPosting", "Cho phép hạch toán nhanh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        AddReadOnly("GeneralLedger.AutoBalanceCheck", "Tự động kiểm tra cân đối", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        AddReadOnly("GeneralLedger.PeriodFromDate", "Từ ngày kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.PeriodToDate", "Đến ngày kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.PeriodStatus", "Trạng thái kỳ kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.LastLockedDate", "Ngày khóa sổ gần nhất", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.LockedBy", "Người khóa sổ", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, null, description: accountingPeriodProvider);
        AddReadOnly("GeneralLedger.DefaultGeneralLedgerReport", "Mẫu sổ cái mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Sổ cái chi tiết tài khoản");
        AddReadOnly("GeneralLedger.DefaultTrialBalanceReport", "Mẫu cân đối phát sinh", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Bảng cân đối phát sinh");
        Add("GeneralLedger.DefaultExportFormat", "Định dạng xuất mặc định", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Enum, "Excel",
            description: "Được dùng bởi nút Xuất mặc định trong Report Center khi đang xem báo cáo Sổ cái hoặc Cân đối phát sinh.");
        AddReadOnly("GeneralLedger.AllowPrintReports", "Cho phép in báo cáo", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true");
        Add("GeneralLedger.AllowExportExcel", "Cho phép xuất Excel", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true",
            description: "Áp dụng cho báo cáo Sổ cái và Cân đối phát sinh trong Report Center.");
        Add("GeneralLedger.AllowExportPdf", "Cho phép xuất PDF", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true",
            description: "Áp dụng cho báo cáo Sổ cái và Cân đối phát sinh trong Report Center.");
        Add("GeneralLedger.ShowAccountantSignature", "Hiển thị chữ ký kế toán", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true",
            description: "Đưa dòng ký tên Kế toán vào PDF Sổ cái và Cân đối phát sinh.");
        Add("GeneralLedger.ShowDirectorSignature", "Hiển thị chữ ký giám đốc", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.Boolean, "true",
            description: "Đưa dòng ký tên Giám đốc vào PDF Sổ cái và Cân đối phát sinh.");
        Add("GeneralLedger.ReportFooterNote", "Ghi chú cuối báo cáo", "Apps", "GENERAL LEDGER", SettingScope.App, SettingValueType.String, "Báo cáo được lập từ dữ liệu sổ cái.",
            description: "Ghi chú hiển thị ở cuối PDF Sổ cái và Cân đối phát sinh; tối đa 500 ký tự.");
        Add("POS.ApiEndpoint", "POS API endpoint", "ClientServices", "POS CLIENT", SettingScope.App, SettingValueType.String, string.Empty, description: endpointProbeConfiguration);
        Add("FO.ApiEndpoint", "FO API endpoint", "ClientServices", "FO CLIENT", SettingScope.App, SettingValueType.String, string.Empty, description: endpointProbeConfiguration);
        return rows;
    }

    public IReadOnlyList<AppModuleDto> GetDefaultApps()
    {
        string[] keys = ["dashboard","virtual-file-center","inventory","inventory-movement","warehouse","import-voucher","export-voucher","receivable","payable","general-ledger","report-center","customers","suppliers","hr","settings","pos-client","fo-client","accounting-client","warehouse-client"];
        return keys.Select((k, i) => new AppModuleDto { AppKey = k, Name = GetAppDisplayName(k), Description = GetAppDisplayDescription(k), Version = null, IsEnabled = true, ShowOnDesktop = true, AllowClientAccess = true, SortOrder = i + 1, RequiredPermission = "Settings.View" }).ToList();
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
    private readonly ISettingsChangeNotifier _notifier;
    private readonly ISecretProtector _protector;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;
    private readonly RuntimeDatabaseSettings? _runtimeDatabaseSettings;
    private readonly RuntimeLoggingSettings? _runtimeLoggingSettings;

    public SettingsService(ISettingsRepository repository, ISettingsRegistry registry, ISettingsValidator validator, ISettingsChangeNotifier notifier, ISecretProtector protector, ICurrentUserService currentUser, IAuthorizationService authorization, RuntimeDatabaseSettings? runtimeDatabaseSettings = null, RuntimeLoggingSettings? runtimeLoggingSettings = null)
    {
        _repository = repository;
        _registry = registry;
        _validator = validator;
        _notifier = notifier;
        _protector = protector;
        _currentUser = currentUser;
        _authorization = authorization;
        _runtimeDatabaseSettings = runtimeDatabaseSettings;
        _runtimeLoggingSettings = runtimeLoggingSettings;
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
        var result = values.Where(x => x.Scope == scope && MatchScope(x.ScopeTarget, scopeTarget))
            .Select(x => x.ToDto(defs.TryGetValue(x.DefinitionKey, out var d) && d.IsSensitive && x.IsEncrypted ? SecretMasker.MaskSecret("***") : (x.IsEncrypted ? _protector.Unprotect(x.Value ?? string.Empty) : x.Value)))
            .ToList();

        if (scope == SettingScope.System && scopeTarget is null && _runtimeDatabaseSettings is not null)
        {
            foreach (var (key, value) in _runtimeDatabaseSettings.Values)
            {
                var runtimeValue = CreateRuntimeValue(key, scope, scopeTarget, value);
                var existingIndex = result.FindIndex(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    result[existingIndex] = runtimeValue;
                }
                else if (defs.ContainsKey(key))
                {
                    result.Add(runtimeValue);
                }
            }
        }

        if (scope == SettingScope.System && scopeTarget is null && _runtimeLoggingSettings is not null)
        {
            foreach (var (key, value) in _runtimeLoggingSettings.Values)
            {
                var runtimeValue = CreateRuntimeValue(key, scope, scopeTarget, value);
                var existingIndex = result.FindIndex(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    result[existingIndex] = runtimeValue;
                }
                else if (defs.ContainsKey(key))
                {
                    result.Add(runtimeValue);
                }
            }
        }

        return result;
    }

    public async Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
    {
        var defs = await GetDefinitionsAsync(scope, scopeTarget);
        var def = defs.FirstOrDefault(x => x.Key == key);
        if (def is null) return null;
        if (scope == SettingScope.System
            && scopeTarget is null
            && _runtimeDatabaseSettings?.TryGetValue(key, out var runtimeValue) == true)
        {
            return CreateRuntimeValue(key, scope, scopeTarget, runtimeValue);
        }

        if (scope == SettingScope.System
            && scopeTarget is null
            && _runtimeLoggingSettings?.TryGetValue(key, out var runtimeLogValue) == true)
        {
            return CreateRuntimeValue(key, scope, scopeTarget, runtimeLogValue);
        }

        var row = await _repository.GetValueAsync(key, scope, scopeTarget);
        if (row is null)
        {
            return new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = def.DefaultValue, IsPersisted = false };
        }

        var displayValue = row.IsEncrypted ? (def.IsSensitive ? SecretMasker.MaskSecret("***") : _protector.Unprotect(row.Value ?? string.Empty)) : row.Value;
        return row.ToDto(displayValue);
    }

    private static SettingValueDto CreateRuntimeValue(string key, SettingScope scope, string? scopeTarget, string? value)
        => new()
        {
            Key = key,
            Scope = scope,
            ScopeTarget = scopeTarget,
            Value = value,
            IsPersisted = false,
            IsRuntimeProvided = true
        };

    public async Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0) return;

        for (var index = 0; index < requests.Count; index++)
        {
            var duplicate = requests.Take(index).Any(previous =>
                previous.Scope == requests[index].Scope
                && string.Equals(previous.Key, requests[index].Key, StringComparison.OrdinalIgnoreCase)
                && string.Equals(previous.ScopeTarget ?? string.Empty, requests[index].ScopeTarget ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new InvalidOperationException($"Lô lưu có cấu hình trùng: '{requests[index].Key}'.");
        }

        // Validate the full batch before the first write so a later invalid,
        // read-only, or unauthorized item cannot leave earlier values changed.
        await EnsureSeededAsync();
        var definitions = (await _repository.GetDefinitionsAsync())
            .Select(x => x.ToDto())
            .ToList();
        foreach (var request in requests)
        {
            var definition = definitions.FirstOrDefault(x => x.Key == request.Key && x.Scope == request.Scope)
                ?? throw new InvalidOperationException($"Không tìm thấy cấu hình '{request.Key}' trong phạm vi {request.Scope}.");
            ValidateSaveRequest(definition, request);
        }

        var changes = new List<PreparedSettingChange>(requests.Count);
        foreach (var request in requests)
        {
            var definition = definitions.First(x => x.Key == request.Key && x.Scope == request.Scope);
            var existing = await _repository.GetValueAsync(request.Key, request.Scope, request.ScopeTarget);
            var oldPlain = existing is null
                ? definition.DefaultValue
                : existing.IsEncrypted
                    ? _protector.Unprotect(existing.Value ?? string.Empty)
                    : existing.Value;
            var encrypted = definition.IsSensitive;
            var plainValue = request.Value;
            var entity = existing ?? new SettingValue
            {
                DefinitionKey = request.Key,
                Scope = request.Scope,
                ScopeTarget = request.ScopeTarget
            };
            entity.Value = encrypted ? _protector.Protect(plainValue ?? string.Empty) : plainValue;
            entity.IsEncrypted = encrypted;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser.CurrentUser?.UserName ?? "system";
            changes.Add(new PreparedSettingChange(request, definition, entity, oldPlain, plainValue));
        }

        var auditLogs = changes.Select(change => new SettingChangeLog
        {
            SettingKey = change.Request.Key,
            Scope = change.Request.Scope,
            ScopeTarget = change.Request.ScopeTarget,
            OldValue = change.Definition.IsSensitive ? "***" : change.OldPlainValue,
            NewValue = change.Definition.IsSensitive ? "***" : change.PlainValue,
            ChangedBy = change.Entity.UpdatedBy ?? "system",
            ChangedAt = change.Entity.UpdatedAt,
            Reason = change.Request.Reason
        }).ToArray();

        // Persist values and audit rows through the same DbContext/SaveChanges
        // so a failed audit insert cannot leave an unaudited configuration.
        await _repository.CommitValuesAndAuditAsync(
            changes.Select(x => x.Entity).ToArray(),
            auditLogs);

        foreach (var change in changes)
        {
            if (string.Equals(change.Request.Key, "Storage.ExportRootPath", StringComparison.OrdinalIgnoreCase))
            {
                ApplicationDataPaths.ConfigureUserExportsDirectory(change.PlainValue);
            }

            _notifier.NotifyChanged(change.Entity.ToDto(change.PlainValue), change.Definition.RequiresRestart);
        }
    }

    public async Task SaveValueAsync(SaveSettingValueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await SaveValuesAsync([request]);
    }

    private void ValidateSaveRequest(SettingDefinitionDto definition, SaveSettingValueRequest request)
    {
        if (definition.IsReadOnly)
            throw new InvalidOperationException($"Cấu hình '{definition.DisplayName}' chỉ đọc và không thể thay đổi từ trang này.");
        if (!string.IsNullOrWhiteSpace(definition.RequiredPermission))
            _authorization.Demand(definition.RequiredPermission);

        var validation = _validator.Validate(definition, request.Value);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);
    }

    private sealed record PreparedSettingChange(
        SaveSettingValueRequest Request,
        SettingDefinitionDto Definition,
        SettingValue Entity,
        string? OldPlainValue,
        string? PlainValue);

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
        if (app.AllowClientAccess != request.AllowClientAccess)
        {
            throw new InvalidOperationException(
                "Chính sách truy cập từ máy trạm chưa được hỗ trợ: hệ thống chưa có định danh vai trò workstation đáng tin cậy. Giá trị hiện có được giữ nguyên.");
        }

        app.Icon = request.Icon;
        app.IsEnabled = request.IsEnabled;
        app.ShowOnDesktop = request.ShowOnDesktop;
        app.PinToTaskbar = request.PinToTaskbar;
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
            // Warehouse settings are consumed by the inventory app. Keep the
            // older warehouse app key as an alias for existing installations.
            "inventory" => ["Warehouse."],
            "warehouse" => ["Warehouse."],
            "general-ledger" => ["GeneralLedger."],
            "pos-client" => ["POS."],
            "fo-client" => ["FO."],
            _ => []
        };
    }
}

