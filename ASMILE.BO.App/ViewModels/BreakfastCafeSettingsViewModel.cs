using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class BreakfastCafeSettingsViewModel : ViewModelBase
{
    public ObservableCollection<SettingsNavGroup> NavItems { get; } = new();

    [ObservableProperty] private int selectedNavIndex;

    partial void OnSelectedNavIndexChanged(int value)
    {
        foreach (var item in NavItems)
            item.IsSelected = item.Index == value;
    }

    private static void AddRange<T>(ObservableCollection<T> col, params T[] items)
    {
        col.Clear();
        foreach (var item in items) col.Add(item);
    }

    public BreakfastCafeSettingsViewModel()
    {
        InitNavItems();
        InitMockData();
    }

    // ================================================================
    // NAVIGATION
    // ================================================================

    private void InitNavItems()
    {
        var items = new (string Icon, string Title, string Desc)[]
        {
            ("", "Tổng quan",         "Dashboard & trạng thái"),
            ("", "Khu vực & Bàn",     "Quản lý sơ đồ bàn"),
            ("", "Thực đơn",          "Món ăn & định lượng"),
            ("", "Bếp & Bar",         "Máy in bếp và bar"),
            ("", "Bán hàng",          "Tùy chọn bán hàng"),
            ("", "Thanh toán",        "Phương thức & thuế phí"),
            ("", "In ấn",             "Máy in & khổ giấy"),
            ("", "Khuyến mại",        "Giảm giá & voucher"),
            ("", "Nhân viên",         "Phân quyền nhân viên"),
            ("", "Thiết bị",          "POS, máy in, scanner"),
            ("", "Đồng bộ",           "Cloud & chi nhánh"),
            ("", "Nâng cao",          "Cấu hình hệ thống"),
        };

        for (int i = 0; i < items.Length; i++)
        {
            var (icon, title, desc) = items[i];
            NavItems.Add(new SettingsNavGroup(icon, title, desc, i));
        }

        NavItems[0].IsSelected = true;
    }

    // ================================================================
    // MOCK DATA
    // ================================================================

    private void InitMockData()
    {
        // Overview
        TotalTables = 23;
        TotalMenuItems = 42;
        TotalAreas = 5;
        TotalPrinters = 3;
        TotalStaff = 8;
        RevenueToday = "2.450.000 đ";

        // Status
        AddRange(SystemStatuses,
            new SystemStatusCard("", "Database", true),
            new SystemStatusCard("", "Kitchen Printer", true),
            new SystemStatusCard("", "Bar Printer", true),
            new SystemStatusCard("", "POS Terminal", true),
            new SystemStatusCard("", "Internet", true)
        );

        AddRange(AreaCards,
            new AreaInfo("Tầng 1", "8 bàn", "#22C55E"),
            new AreaInfo("Tầng 2", "6 bàn", "#3B82F6"),
            new AreaInfo("Sân vườn", "4 bàn", "#F2994A"),
            new AreaInfo("Mang đi", "1 bàn", "#8B5CF6"),
            new AreaInfo("Giao hàng", "-", "#64748B")
        );

        AddRange(PaymentMethodCards,
            new PaymentMethodInfo("", "Tiền mặt", true),
            new PaymentMethodInfo("", "Chuyển khoản", true),
            new PaymentMethodInfo("", "QR Code", false),
            new PaymentMethodInfo("", "POS/The", true),
            new PaymentMethodInfo("", "Công nợ", false)
        );

        AddRange(DeviceCards,
            new DeviceInfo("POS Terminal 01", true),
            new DeviceInfo("Kitchen Printer", true),
            new DeviceInfo("Bar Printer", true),
            new DeviceInfo("Receipt Printer", false),
            new DeviceInfo("Barcode Scanner", true),
            new DeviceInfo("Customer Display", false)
        );
    }

    // ================================================================
    // OVERVIEW
    // ================================================================
    [ObservableProperty] private int totalTables;
    [ObservableProperty] private int totalMenuItems;
    [ObservableProperty] private int totalAreas;
    [ObservableProperty] private int totalPrinters;
    [ObservableProperty] private int totalStaff;
    [ObservableProperty] private string revenueToday = string.Empty;
    public ObservableCollection<SystemStatusCard> SystemStatuses { get; } = new();

    // ================================================================
    // AREA & TABLE
    // ================================================================
    [ObservableProperty] private int defaultSeatCount = 4;
    [ObservableProperty] private string emptyTableColor = "#22C55E";
    [ObservableProperty] private string servingTableColor = "#3B82F6";
    [ObservableProperty] private string waitingTableColor = "#F97316";
    [ObservableProperty] private bool allowMergeTables = true;
    [ObservableProperty] private bool allowSplitTables = true;
    [ObservableProperty] private bool allowChangeTable = true;
    public ObservableCollection<AreaInfo> AreaCards { get; } = new();

    // ================================================================
    // MENU
    // ================================================================
    [ObservableProperty] private bool showMenuItemImage = true;
    [ObservableProperty] private bool showMenuItemCode = true;
    [ObservableProperty] private bool showMenuItemPrice = true;
    [ObservableProperty] private bool requireRecipe = false;
    [ObservableProperty] private bool warnMissingRecipe = true;
    [ObservableProperty] private bool autoCalculateCost = true;

    // ================================================================
    // KITCHEN & BAR
    // ================================================================
    [ObservableProperty] private bool printOnSendKitchen = true;
    [ObservableProperty] private bool reprintOnEdit = false;
    [ObservableProperty] private string kitchenPrinterName = "EPSON TM-T88V (Bếp)";
    [ObservableProperty] private string kitchenPrinterStatus = "Online";
    [ObservableProperty] private string barPrinterName = "EPSON TM-T88V (Bar)";
    [ObservableProperty] private string barPrinterStatus = "Online";

    // ================================================================
    // SALES
    // ================================================================
    [ObservableProperty] private bool autoCreateOrderOnTableSelect = true;
    [ObservableProperty] private bool confirmCancelItem = true;
    [ObservableProperty] private bool confirmCancelOrder = true;
    [ObservableProperty] private bool allowEditPrice = false;
    [ObservableProperty] private bool allowNegativeStock = false;
    [ObservableProperty] private bool autoLockAfterPayment = true;

    // ================================================================
    // PAYMENT
    // ================================================================
    [ObservableProperty] private decimal vatRate = 8m;
    [ObservableProperty] private decimal serviceChargeRate = 5m;
    [ObservableProperty] private bool enableServiceCharge = false;
    [ObservableProperty] private int roundingRule = 0; // 0=Không, 1=Lên 500, 2=Lên 1000
    [ObservableProperty] private string roundingRuleText = "Không làm tròn";
    public ObservableCollection<PaymentMethodInfo> PaymentMethodCards { get; } = new();
    public ObservableCollection<string> RoundingOptions { get; } = new() { "Không làm tròn", "Làm tròn lên 500đ", "Làm tròn lên 1.000đ" };

    // ================================================================
    // PRINT
    // ================================================================
    [ObservableProperty] private string receiptPrinterName = "EPSON TM-T88V (Hóa đơn)";
    [ObservableProperty] private int paperSize = 80;
    [ObservableProperty] private bool isPaperSize58;
    [ObservableProperty] private bool isPaperSize80 = true;
    [ObservableProperty] private int copyCount = 2;
    [ObservableProperty] private bool hasLogo = true;
    [ObservableProperty] private string logoPath = "logo.png";

    partial void OnIsPaperSize58Changed(bool value) { if (value) { PaperSize = 58; IsPaperSize80 = false; } }
    partial void OnIsPaperSize80Changed(bool value) { if (value) { PaperSize = 80; IsPaperSize58 = false; } }

    // ================================================================
    // PROMOTION
    // ================================================================
    [ObservableProperty] private bool allowOrderDiscount = true;
    [ObservableProperty] private bool allowItemDiscount = true;
    [ObservableProperty] private bool enableVoucher = false;
    [ObservableProperty] private bool enableLoyaltyPoints = false;

    // ================================================================
    // STAFF
    // ================================================================
    [ObservableProperty] private bool cashierCanDiscount = true;
    [ObservableProperty] private bool cashierCanCancelItem = true;
    [ObservableProperty] private bool cashierCanCancelOrder = false;
    [ObservableProperty] private bool managerCanEditPrice = true;

    // ================================================================
    // DEVICES
    // ================================================================
    public ObservableCollection<DeviceInfo> DeviceCards { get; } = new();

    // ================================================================
    // SYNC
    // ================================================================
    [ObservableProperty] private bool enableCloudSync = true;
    [ObservableProperty] private bool enableBranchSync = false;
    [ObservableProperty] private bool enableAutoBackup = true;
    [ObservableProperty] private string backupPath = @"D:\ASMILE-BO\Backups\";

    // ================================================================
    // ADVANCED
    // ================================================================
    [ObservableProperty] private bool debugMode = false;
    [ObservableProperty] private string orderPrefix = "ORD";
    [ObservableProperty] private int autoLogoutMinutes = 30;
    [ObservableProperty] private string language = "Tiếng Việt";
    public ObservableCollection<string> LanguageOptions { get; } = new() { "Tiếng Việt", "English" };

    // ================================================================
    // COMMANDS
    // ================================================================

    [RelayCommand]
    private void NavigateSettingsNav(object? parameter)
    {
        var idx = parameter switch
        {
            int i => i,
            string s when int.TryParse(s, out var p) => p,
            _ => -1
        };
        if (idx >= 0 && idx < NavItems.Count)
            SelectedNavIndex = idx;
    }

    [RelayCommand]
    private void SelectRounding(string? option)
    {
        if (string.IsNullOrWhiteSpace(option)) return;
        RoundingRuleText = option;
        RoundingRule = option switch
        {
            "Làm tròn lên 500đ" => 1,
            "Làm tròn lên 1.000đ" => 2,
            _ => 0
        };
    }

    [RelayCommand]
    private void TestPrint()
    {
        // Mock: show message
        System.Windows.MessageBox.Show($"In thử trên {ReceiptPrinterName}\nKhổ giấy: {PaperSize}mm\nSố liên: {CopyCount}",
            "Test Print", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private void BackupNow()
    {
        System.Windows.MessageBox.Show($"Đã tạo backup tại:\n{BackupPath}{DateTime.Now:yyyyMMdd_HHmmss}.bak",
            "Backup thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}

// ================================================================
// HELPER CLASSES
// ================================================================

public sealed partial class SettingsNavGroup : ObservableObject
{
    public SettingsNavGroup(string icon, string title, string description, int index)
    {
        Icon = icon;
        Title = title;
        Description = description;
        Index = index;
    }

    public string Icon { get; }
    public string Title { get; }
    public string Description { get; }
    public int Index { get; }

    [ObservableProperty] private bool isSelected;
}

public sealed record SystemStatusCard(string Icon, string Name, bool IsOnline);

public sealed partial class AreaInfo : ObservableObject
{
    public AreaInfo(string name, string tableCount, string color)
    {
        Name = name;
        TableCount = tableCount;
        Color = color;
    }

    public string Name { get; }
    public string TableCount { get; }
    public string Color { get; }
}

public sealed record PaymentMethodInfo(string Icon, string Name, bool IsEnabled);

public sealed record DeviceInfo(string Name, bool IsOnline);
