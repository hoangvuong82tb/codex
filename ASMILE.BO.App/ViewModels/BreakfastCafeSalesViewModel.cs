using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;
using ASMILE.BO.Shared.Storage;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class BreakfastCafeSalesViewModel : ViewModelBase
{
    private readonly IBreakfastCafeSalesService _salesService;
    private readonly IMenuCatalogService _menuCatalogService;
    private readonly IMenuRecipeService _recipeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDialogService _dialogService;
    private readonly List<MenuItemDto> _allMenuItems = [];
    private readonly List<PosTableDto> _allTables = [];
    private readonly HashSet<Guid> _allowedBreakfastCafeCategoryIds = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _orderSentKitchenLineIds = [];
    private readonly HashSet<Guid> _ordersWithKitchenHistory = [];
    private bool _isLoadingSettings;
    private bool _reloadSettingsAfterCurrent;
    private static readonly JsonSerializerOptions SalesPolicyJsonOptions = new() { WriteIndented = true };

    public ObservableCollection<string> Areas { get; } = new();
    public ObservableCollection<PosTableDto> FilteredTables { get; } = new();
    public ObservableCollection<PosCategoryFilter> Categories { get; } = new();
    public ObservableCollection<string> MenuFilters { get; } = new(["Đang bán", "Có định lượng", "Chưa định lượng"]);
    public ObservableCollection<string> SortModes { get; } = new(["Tên món", "Giá tăng dần", "Giá giảm dần", "Nhóm món"]);
    public ObservableCollection<string> MenuViewModes { get; } = new(["Danh sách", "Lưới"]);
    public ObservableCollection<string> PaymentMethods { get; } = new(["Tiền mặt", "Chuyển khoản", "POS/Thẻ", "Ghi phòng", "Công nợ"]);
    public ObservableCollection<string> TableStatusFilterOptions { get; } = new(["Tất cả", "Trống", "Đang phục vụ", "Chờ thanh toán", "Đã đặt"]);
    public ObservableCollection<int> TableColumnOptions { get; } = new([2, 3, 4, 5]);
    public ObservableCollection<string> OpenOrderSortOptions { get; } = new(["Mới nhất", "Cũ nhất", "Tổng tiền cao", "Tổng tiền thấp", "Theo bàn", "Nhiều món"]);
    public ObservableCollection<string> SettingMenuItemClassificationTabs { get; } = new(["Tất cả", "Đồ ăn", "Đồ uống", "Phụ Trợ", "Dịch vụ", "Loại Khác"]);
    public ObservableCollection<ShiftSettingRow> ShiftSettings { get; } = new();
    public ObservableCollection<PosMenuItemCard> FilteredMenuItems { get; } = new();
    public ObservableCollection<PosOrderLineRow> CurrentOrderLines { get; } = new();
    public ObservableCollection<PosOrderTabRow> CurrentOrderTabs { get; } = new();
    public ObservableCollection<BreakfastCafeOrderDto> OpenOrders { get; } = new();
    public ObservableCollection<BreakfastCafeOrderDto> TodayOrders { get; } = new();
    public ObservableCollection<BreakfastCafeOrderDto> ClosedOrders { get; } = new();
    public ObservableCollection<PosOrderLineRow> ClosedOrderDetailLines { get; } = new();
    public ObservableCollection<PosReportOrderRow> ReportOrders { get; } = new();
    public ObservableCollection<PosReportItemRow> ReportItems { get; } = new();
    public ObservableCollection<PosReportPaymentRow> ReportPayments { get; } = new();
    public ObservableCollection<PosReportGroupRow> ReportAreas { get; } = new();
    public ObservableCollection<PosReportGroupRow> ReportOrderTypes { get; } = new();
    [ObservableProperty] private BreakfastCafeOrderDto? selectedClosedOrder;
    private bool _loadingClosedDetail;

    partial void OnSelectedClosedOrderChanged(BreakfastCafeOrderDto? value)
    {
        if (_loadingClosedDetail || value is null) return;
        _ = SelectClosedOrderAsync(value);
    }

    // Closed orders — enhanced filters
    public record FilterOption(string Label, string Value);
    public ObservableCollection<FilterOption> ClosedOrderStatusFilters { get; } = new([
        new("Tất cả trạng thái", ""), new("Đã thanh toán", "Paid"), new("Đã hủy", "Cancelled")
    ]);
    public ObservableCollection<FilterOption> ClosedOrderDateFilters { get; } = new([
        new("Hôm nay", "Today"), new("7 ngày", "Week"), new("Tháng này", "Month"), new("Tất cả", "All")
    ]);
    public ObservableCollection<FilterOption> ClosedOrderShiftFilters { get; } = new([
        new("Tất cả ca", ""), new("Ca sáng", "Morning"), new("Ca chiều", "Afternoon"), new("Ca tối", "Evening")
    ]);
    public ObservableCollection<FilterOption> ClosedOrderSortOptions { get; } = new([
        new("Mới nhất", "Newest"), new("Cũ nhất", "Oldest"), new("Tiền cao nhất", "Highest"), new("Tiền thấp nhất", "Lowest")
    ]);
    [ObservableProperty] private string closedOrderStatusFilter = "";
    [ObservableProperty] private DateTime? closedOrderFromDate = DateTime.Today;
    [ObservableProperty] private DateTime? closedOrderToDate = DateTime.Today;
    [ObservableProperty] private string closedOrderShiftFilter = "";
    [ObservableProperty] private string closedOrderSearchText = "";
    [ObservableProperty] private string closedOrderSortOption = "Newest";
    [ObservableProperty] private DateTime? reportFromDate = DateTime.Today;
    [ObservableProperty] private DateTime? reportToDate = DateTime.Today;
    [ObservableProperty] private string reportStatusFilter = "";
    [ObservableProperty] private string reportSearchText = "";
    [ObservableProperty] private int reportDetailTabIndex;

    // Closed orders KPIs
    public int ClosedOrderCount => ClosedOrders.Count;
    public decimal ClosedOrderRevenue => ClosedOrders.Where(x => x.Status == "Paid").Sum(x => x.TotalAmount);
    public decimal ClosedOrderDiscountTotal => ClosedOrders.Sum(x => x.DiscountAmount);
    public int CancelledOrderCount => ClosedOrders.Count(x => x.Status == "Cancelled");
    public decimal AverageOrderValue => ClosedOrderCount > 0 ? ClosedOrderRevenue / ClosedOrderCount : 0;
    public int AverageServiceMinutes
    {
        get
        {
            var durations = ClosedOrders
                .Where(x => string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                .Select(CalculateServiceMinutes)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
            return durations.Count == 0 ? 0 : (int)Math.Round(durations.Average(), MidpointRounding.AwayFromZero);
        }
    }

    // Timeline entries for selected closed order
    public ObservableCollection<ClosedOrderTimelineEntry> ClosedOrderTimeline { get; } = new();

    // Closed orders visibility helpers
    public bool HasClosedOrders => ClosedOrders.Count > 0;
    public bool HasSelectedClosedOrder => SelectedClosedOrder is not null;
    public bool HasNoSelectedClosedOrder => SelectedClosedOrder is null;
    public ObservableCollection<FilterOption> ReportStatusFilters { get; } = new([
        new("Tất cả trạng thái", ""), new("Đã thanh toán", "Paid"), new("Đang mở", "Open"), new("Đã hủy", "Cancelled")
    ]);
    public int ReportOrderCount => ReportOrders.Count;
    public int ReportPaidOrderCount => ReportOrders.Count(x => x.Status == "Paid");
    public int ReportCancelledOrderCount => ReportOrders.Count(x => x.Status == "Cancelled");
    public decimal ReportRevenueAmount => ReportOrders.Where(x => x.Status == "Paid").Sum(x => x.TotalAmount);
    public decimal ReportDiscountAmount => ReportOrders.Sum(x => x.DiscountAmount);
    public decimal ReportVatAmount => ReportOrders.Sum(x => x.VatAmount);
    public decimal ReportAverageOrderAmount => ReportPaidOrderCount > 0 ? ReportRevenueAmount / ReportPaidOrderCount : 0m;
    public bool HasReportOrders => ReportOrders.Count > 0;

    [ObservableProperty] private int leftTabIndex;
    [ObservableProperty] private string selectedArea = string.Empty;
    [ObservableProperty] private PosTableDto? selectedTable;
    [ObservableProperty] private PosCategoryFilter? selectedCategory;
    [ObservableProperty] private PosMenuItemCard? selectedMenuItemForSale;
    [ObservableProperty] private string selectedMenuFilter = "Đang bán";
    [ObservableProperty] private string selectedSortMode = "Tên món";
    [ObservableProperty] private string selectedMenuViewMode = "Lưới";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string tableSearchText = string.Empty;
    [ObservableProperty] private string tableStatusFilter = "Tất cả";
    [ObservableProperty] private string selectedOpenOrderSort = "Mới nhất";
    [ObservableProperty] private int tableColumnCount = 4;
    [ObservableProperty] private int openOrderColumnCount = 5;
    [ObservableProperty] private string orderNote = string.Empty;
    [ObservableProperty] private string currentOrderTabName = string.Empty;
    [ObservableProperty] private BreakfastCafeOrderDto? currentOrder;
    [ObservableProperty] private bool isChangingTable;
    [ObservableProperty] private bool isMergingOrder;
    [ObservableProperty] private BreakfastCafeSalesSummaryDto summary = new();
    [ObservableProperty] private string statusText = "Sẵn sàng bán hàng";
    [ObservableProperty] private string selectedPaymentMethod = "Tiền mặt";
    [ObservableProperty] private decimal discountAmount;

    // Quick add by code
    [ObservableProperty] private string quickAddCode = string.Empty;

    // Kitchen timer
    public string KitchenElapsedText => CurrentOrder?.KitchenSentAt is null ? "" :
        (DateTime.UtcNow - CurrentOrder.KitchenSentAt.Value).TotalMinutes switch
        {
            < 1 => "Vừa gửi",
            < 60 => $"{(int)(DateTime.UtcNow - CurrentOrder.KitchenSentAt.Value).TotalMinutes} phút",
            _ => $"{(int)(DateTime.UtcNow - CurrentOrder.KitchenSentAt.Value).TotalHours}h"
        };

    // QR Payment
    [ObservableProperty] private string qrPaymentText = string.Empty;
    public bool HasQrCode => !string.IsNullOrWhiteSpace(QrPaymentText);

    // Kitchen display
    public ObservableCollection<PosOrderLineRow> KitchenPendingItems { get; } = new();
    public bool HasKitchenItems => KitchenPendingItems.Count > 0;

        public BreakfastCafeSalesViewModel(IBreakfastCafeSalesService salesService, IMenuCatalogService menuCatalogService, IMenuRecipeService recipeService, ICurrentUserService currentUserService, IDialogService dialogService)
    {
        _salesService = salesService;
        _menuCatalogService = menuCatalogService;
        _recipeService = recipeService;
        _currentUserService = currentUserService;
        _dialogService = dialogService;
        LoadSalesPolicySettings();
        EnsureDefaultShiftSettings();
        _ = InitializeAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                StatusText = $"Lỗi khởi tạo: {t.Exception?.InnerException?.Message ?? t.Exception?.Message ?? "không rõ"}";
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public decimal SubtotalAmount => CurrentOrder?.SubtotalAmount ?? 0;
    public decimal VatAmount => CurrentOrder?.VatAmount ?? 0;
    public decimal ServiceChargeAmount => CurrentOrder?.ServiceChargeAmount ?? 0;
    public decimal TotalAmount => CurrentOrder?.TotalAmount ?? 0;
    public decimal RemainingAmount => CurrentOrder?.RemainingAmount ?? 0;
    public string CurrentOrderTitle => CurrentOrder is null ? "Chưa mở đơn" : CurrentOrder.OrderNumber;
    public string CurrentSeatName => CurrentOrder?.TableName ?? SelectedTable?.TableName ?? "Chưa chọn bàn";
    public string CurrentOrderTypeText => ToOrderTypeText(CurrentOrder?.OrderType ?? SelectedTable?.OrderType);
    public string CurrentOrderStatusText => ToServiceStatusText(CurrentOrder?.ServiceStatus ?? CurrentOrder?.Status);
    public bool HasCurrentOrder => CurrentOrder is not null;
    public bool HasOrderLines => CurrentOrderLines.Count > 0;
    public bool HasNoOrderLines => !HasOrderLines;
    public bool IsMenuViewList => SelectedMenuViewMode == "Danh sách";
    public bool IsMenuViewGrid => SelectedMenuViewMode == "Lưới";
    public bool CanSendKitchen => CurrentOrder is not null && GetPendingKitchenLines().Count > 0;
    public int SelectedOrderLineCount => CurrentOrderLines.Count(x => x.IsSelected);
    public bool HasSelectedOrderLines => SelectedOrderLineCount > 0;
    public bool HasOpenOrders => OpenOrders.Count > 0;
    public bool HasNoOpenOrders => !HasOpenOrders;
    public bool HasOrderTabs => CurrentOrderTabs.Count > 0;
    public bool HasMultipleOrderTabs => CurrentOrderTabs.Count > 1;

    // Left tab visibility
    public bool IsTabAreas => LeftTabIndex == 0;
    public bool IsTabMenu => LeftTabIndex == 1;
    public bool IsTabOpenOrders => LeftTabIndex == 2;
    public bool IsTabSettings => LeftTabIndex == 3;
    public bool IsTabClosedOrders => LeftTabIndex == 4;
    public bool IsTabReports => LeftTabIndex == 5;
    public bool ShowOrderPanel => !IsTabSettings && !IsTabClosedOrders && !IsTabReports;

    // Settings: sub-tab navigation (Win11 style)
    [ObservableProperty] private int settingsSubTabIndex;
    public bool IsSettingsTabOverview => SettingsSubTabIndex == 0;
    public bool IsSettingsTabTables => SettingsSubTabIndex == 1;
    public bool IsSettingsTabCategories => SettingsSubTabIndex == 2;
    public bool IsSettingsTabMenuItems => SettingsSubTabIndex == 3;
    public bool IsSettingsTabServiceCharge => SettingsSubTabIndex == 4;
    public bool IsSettingsTabDiscount => SettingsSubTabIndex == 5;
    public bool IsSettingsTabPayment => SettingsSubTabIndex == 6;
    public bool IsSettingsTabKitchen => SettingsSubTabIndex == 7;
    public bool IsSettingsTabShift => SettingsSubTabIndex == 8;
    public bool IsSettingsTabPrint => SettingsSubTabIndex == 9;
    public bool IsSettingsTabPermissions => SettingsSubTabIndex == 10;
    public bool IsSettingsTabInventory => SettingsSubTabIndex == 11;
    public bool IsSettingsTabDataAudit => SettingsSubTabIndex == 12;
    public int SettingsTotalTables => SettingTables.Count;
    public int SettingsTotalAreas => SettingAreaRows.Count;
    public int SettingsTotalCategories => SettingCategories.Count;
    public int SettingsTotalMenuItems => SettingMenuItems.Count;

    // Settings: Table form
    [ObservableProperty] private string newAreaCode = string.Empty;
    [ObservableProperty] private string newAreaName = string.Empty;
    [ObservableProperty] private PosAreaSettingRow? selectedSettingArea;
    [ObservableProperty] private string newTableAreaName = string.Empty;
    [ObservableProperty] private string newTableAreaCode = string.Empty;
    [ObservableProperty] private string newTableCode = string.Empty;
    [ObservableProperty] private string newTableName = string.Empty;
    [ObservableProperty] private int newTableSeats = 4;
    [ObservableProperty] private string newTableOrderType = "DineIn (Tại bàn)";
    [ObservableProperty] private PosTableDto? selectedSettingTable;
    public ObservableCollection<PosAreaSettingRow> SettingAreaRows { get; } = new();
    public ObservableCollection<PosTableDto> SettingTables { get; } = new();
    public ObservableCollection<string> SettingAreas { get; } = new();
    public ObservableCollection<string> SettingOrderTypes { get; } = new(["DineIn (Tại bàn)", "TakeAway (Mang đi)", "RoomService (Phòng)"]);

    // Settings: Category form
    [ObservableProperty] private string newCategoryCode = string.Empty;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private MenuCategoryDto? selectedSettingCategory;
    public ObservableCollection<MenuCategoryDto> SettingCategories { get; } = new();
    public ObservableCollection<CategorySaleSelectionRow> BreakfastCafeCategorySelections { get; } = new();

    // Settings: Menu Item management
    [ObservableProperty] private MenuItemDto? selectedSettingMenuItem;
    [ObservableProperty] private string editMenuItemName = string.Empty;
    [ObservableProperty] private decimal editMenuItemPrice;
    [ObservableProperty] private bool editMenuItemActive = true;
    [ObservableProperty] private string editMenuItemClassification = "Đồ ăn";
    [ObservableProperty] private string selectedSettingMenuItemClassification = "Tất cả";
    public ObservableCollection<string> MenuItemClassificationOptions { get; } = new(["Đồ ăn", "Đồ uống", "Phụ Trợ", "Dịch vụ", "Loại Khác"]);
    public ObservableCollection<MenuItemDto> SettingMenuItems { get; } = new();
    public ObservableCollection<MenuItemDto> FilteredSettingMenuItems { get; } = new();

    // Settings: service charge policy
    [ObservableProperty] private bool enableDineInServiceCharge;
    [ObservableProperty] private bool enableTakeAwayServiceCharge;
    [ObservableProperty] private bool enableRoomServiceServiceCharge = true;
    [ObservableProperty] private decimal defaultServiceChargeRate = 5m;
    [ObservableProperty] private bool applyServiceChargeAfterDiscount = true;
    [ObservableProperty] private bool includeServiceChargeInVatBase;
    [ObservableProperty] private bool showServiceChargeLineOnReceipt = true;

    // Settings: discount policy
    [ObservableProperty] private bool allowOrderDiscount = true;
    [ObservableProperty] private bool allowLineDiscount;
    [ObservableProperty] private decimal maxOrderDiscountPercent = 20m;
    [ObservableProperty] private decimal maxOrderDiscountAmount = 500000m;
    [ObservableProperty] private bool requireManagerApprovalForDiscount = true;
    [ObservableProperty] private bool requireDiscountReason;
    [ObservableProperty] private bool enableVoucherDiscount;
    [ObservableProperty] private string defaultDiscountReason = "Khuyến mại";

    // Settings: payment policy
    [ObservableProperty] private bool enableCashPayment = true;
    [ObservableProperty] private bool enableBankTransferPayment = true;
    [ObservableProperty] private bool enableCardPosPayment = true;
    [ObservableProperty] private bool enableRoomChargePayment = true;
    [ObservableProperty] private bool enableCustomerDebtPayment = true;
    [ObservableProperty] private bool allowSplitPayment;
    [ObservableProperty] private bool requirePaymentReferenceForBank = true;
    [ObservableProperty] private string defaultQrBankAccount = string.Empty;
    [ObservableProperty] private string defaultCashAccountCode = "111";
    [ObservableProperty] private string defaultBankAccountCode = "112";

    // Settings: kitchen/bar policy
    [ObservableProperty] private bool autoPrintKitchenTicket = true;
    [ObservableProperty] private bool printKitchenTicketOnEdit;
    [ObservableProperty] private bool allowKitchenReprint = true;
    [ObservableProperty] private bool requireKitchenReprintReason = true;
    [ObservableProperty] private string kitchenPrinterName = "Kitchen Printer";
    [ObservableProperty] private string barPrinterName = "Bar Printer";
    [ObservableProperty] private string kitchenPaperSize = "80mm";
    [ObservableProperty] private string barPaperSize = "80mm";
    [ObservableProperty] private string defaultPreparationStation = "Kitchen";
    [ObservableProperty] private bool selectAllOrderLines;

    // Settings: shift policy
    [ObservableProperty] private bool requireOpenShiftBeforeSale;
    [ObservableProperty] private bool requireCashCountOnCloseShift = true;
    [ObservableProperty] private bool lockSalesAfterShiftClose = true;
    [ObservableProperty] private decimal openingCashAmount;
    [ObservableProperty] private decimal maxShiftCashVariance = 10000m;
    [ObservableProperty] private string currentShiftName = "Ca sáng";
    [ObservableProperty] private int shiftSubTabIndex;
    [ObservableProperty] private ShiftSettingRow? selectedShiftSetting;
    [ObservableProperty] private string shiftFormCode = string.Empty;
    [ObservableProperty] private string shiftFormName = string.Empty;
    [ObservableProperty] private string shiftFormStartTime = "06:00";
    [ObservableProperty] private string shiftFormEndTime = "14:00";
    [ObservableProperty] private bool shiftFormIsActive = true;

    // Settings: print policy
    [ObservableProperty] private bool autoPrintReceiptAfterPayment;
    [ObservableProperty] private bool allowProvisionalPrint = true;
    [ObservableProperty] private string receiptPrinterName = "Receipt Printer";
    [ObservableProperty] private string receiptPaperSize = "80mm";
    [ObservableProperty] private int receiptCopyCount = 1;
    [ObservableProperty] private string receiptFooterText = "Cảm ơn quý khách!";
    [ObservableProperty] private bool printStoreLogo = true;

    // Settings: permissions policy
    [ObservableProperty] private bool cashierCanCancelItem = true;
    [ObservableProperty] private bool cashierCanCancelOrder;
    [ObservableProperty] private bool cashierCanEditPrice;
    [ObservableProperty] private bool cashierCanViewReports;
    [ObservableProperty] private bool requireManagerPinForSensitiveActions = true;
    [ObservableProperty] private decimal cashierMaxDiscountPercent = 10m;

    // Settings: inventory policy
    [ObservableProperty] private bool checkInventoryBeforeSale = true;
    [ObservableProperty] private bool blockSaleWhenOutOfStock = true;
    [ObservableProperty] private bool warnMenuWithoutRecipe = true;
    [ObservableProperty] private bool consumeInventoryOnPayment = true;
    [ObservableProperty] private bool consumeInventoryOnKitchenSend;
    [ObservableProperty] private string defaultSaleWarehouseCode = "KHO-001";

    // Settings: data/audit policy
    [ObservableProperty] private bool enableSettingsAuditLog = true;
    [ObservableProperty] private bool requireConfirmBeforeReset = true;
    [ObservableProperty] private bool allowExportSettings = true;
    [ObservableProperty] private bool allowImportSettings = true;
    [ObservableProperty] private int auditRetentionDays = 365;
    [ObservableProperty] private string settingsBackupFolder = "Data\\Settings";

    partial void OnSelectedAreaChanged(string value) => ApplyTableFilter();
    partial void OnSearchTextChanged(string value) => ApplyMenuFilter();
    partial void OnSelectedCategoryChanged(PosCategoryFilter? value) => ApplyMenuFilter();
    partial void OnSelectedMenuItemForSaleChanged(PosMenuItemCard? value)
    {
        // POS adds items from explicit card/button clicks to avoid duplicate adds from ListBox selection.
    }
    partial void OnSelectedMenuFilterChanged(string value) => ApplyMenuFilter();
    partial void OnSelectedSortModeChanged(string value) => ApplyMenuFilter();
    partial void OnSelectedSettingMenuItemClassificationChanged(string value) => ApplySettingMenuItemFilter();
    partial void OnSelectedMenuViewModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsMenuViewList));
        OnPropertyChanged(nameof(IsMenuViewGrid));
    }
    partial void OnTableSearchTextChanged(string value) => ApplyTableFilter();
    partial void OnTableStatusFilterChanged(string value) => ApplyTableFilter();
    partial void OnSelectedOpenOrderSortChanged(string value) => ApplyOpenOrderSort();
    partial void OnOpenOrderColumnCountChanged(int value)
    {
        if (value < 1)
        {
            OpenOrderColumnCount = 1;
        }
        else if (value > 8)
        {
            OpenOrderColumnCount = 8;
        }
    }
    partial void OnReportFromDateChanged(DateTime? value) => _ = LoadReportsAsync();
    partial void OnReportToDateChanged(DateTime? value) => _ = LoadReportsAsync();
    partial void OnReportStatusFilterChanged(string value) => _ = LoadReportsAsync();
    partial void OnReportSearchTextChanged(string value) => _ = LoadReportsAsync();
    partial void OnSelectedShiftSettingChanged(ShiftSettingRow? value)
    {
        OnPropertyChanged(nameof(ShiftFormHeaderText));
    }

    partial void OnLeftTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsTabAreas));
        OnPropertyChanged(nameof(IsTabMenu));
        OnPropertyChanged(nameof(IsTabOpenOrders));
        OnPropertyChanged(nameof(IsTabSettings));
        OnPropertyChanged(nameof(IsTabClosedOrders));
        OnPropertyChanged(nameof(IsTabReports));
        OnPropertyChanged(nameof(ShowOrderPanel));
        if (value == 0) _ = RefreshOpenDataSafeAsync();
        if (value == 3) _ = LoadSettingsAsync();
        if (value == 4) _ = LoadClosedOrdersAsync();
        if (value == 5) _ = LoadReportsAsync();
    }

    partial void OnSettingsSubTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSettingsTabOverview));
        OnPropertyChanged(nameof(IsSettingsTabTables));
        OnPropertyChanged(nameof(IsSettingsTabCategories));
        OnPropertyChanged(nameof(IsSettingsTabMenuItems));
        OnPropertyChanged(nameof(IsSettingsTabServiceCharge));
        OnPropertyChanged(nameof(IsSettingsTabDiscount));
        OnPropertyChanged(nameof(IsSettingsTabPayment));
        OnPropertyChanged(nameof(IsSettingsTabKitchen));
        OnPropertyChanged(nameof(IsSettingsTabShift));
        OnPropertyChanged(nameof(IsSettingsTabPrint));
        OnPropertyChanged(nameof(IsSettingsTabPermissions));
        OnPropertyChanged(nameof(IsSettingsTabInventory));
        OnPropertyChanged(nameof(IsSettingsTabDataAudit));
    }

    partial void OnCurrentOrderChanged(BreakfastCafeOrderDto? value)
    {
        foreach (var row in CurrentOrderLines)
        {
            row.PropertyChanged -= OnCurrentOrderLinePropertyChanged;
        }

        if (value is not null && HasKitchenHistory(value))
        {
            _ordersWithKitchenHistory.Add(value.Id);
            if (!_orderSentKitchenLineIds.TryGetValue(value.Id, out var sentIds))
            {
                sentIds = [];
                _orderSentKitchenLineIds[value.Id] = sentIds;
            }

            foreach (var line in value.Lines)
            {
                var isSupplement = !string.IsNullOrWhiteSpace(line.Note) && line.Note.Contains("Món thêm", StringComparison.OrdinalIgnoreCase);
                if (string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase) || !isSupplement)
                {
                    sentIds.Add(line.Id);
                }
            }
        }

        ResetWith(CurrentOrderLines, value?.Lines.Select(x => new PosOrderLineRow(x)) ?? []);
        foreach (var row in CurrentOrderLines)
        {
            row.PropertyChanged += OnCurrentOrderLinePropertyChanged;
        }

        SelectAllOrderLines = false;
        DiscountAmount = value?.DiscountAmount ?? 0;
        OrderNote = value?.Note ?? string.Empty;
        CurrentOrderTabName = ResolveOrderTabName(value, CurrentOrderTabs.Count + 1);
        BuildCurrentOrderTabs();
        OnPropertyChanged(nameof(SubtotalAmount));
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(ServiceChargeAmount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(CurrentOrderTitle));
        OnPropertyChanged(nameof(CurrentSeatName));
        OnPropertyChanged(nameof(CurrentOrderTypeText));
        OnPropertyChanged(nameof(CurrentOrderStatusText));
        OnPropertyChanged(nameof(HasCurrentOrder));
        OnPropertyChanged(nameof(HasOrderLines));
        OnPropertyChanged(nameof(HasNoOrderLines));
        OnPropertyChanged(nameof(CanSendKitchen));
        OnPropertyChanged(nameof(SelectedOrderLineCount));
        OnPropertyChanged(nameof(HasSelectedOrderLines));
        OnPropertyChanged(nameof(KitchenElapsedText));
    }

    partial void OnSelectAllOrderLinesChanged(bool value)
    {
        foreach (var row in CurrentOrderLines)
        {
            row.IsSelected = value;
        }

        OnPropertyChanged(nameof(SelectedOrderLineCount));
        OnPropertyChanged(nameof(HasSelectedOrderLines));
    }

    private void OnCurrentOrderLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(PosOrderLineRow.IsSelected), StringComparison.Ordinal))
        {
            return;
        }

        var allSelected = CurrentOrderLines.Count > 0 && CurrentOrderLines.All(x => x.IsSelected);
        if (SelectAllOrderLines != allSelected)
        {
            SelectAllOrderLines = allSelected;
            return;
        }

        OnPropertyChanged(nameof(SelectedOrderLineCount));
        OnPropertyChanged(nameof(HasSelectedOrderLines));
    }

    [RelayCommand]
    private async Task OpenTakeAwayAsync()
    {
        var table = _allTables.FirstOrDefault(x => string.Equals(x.OrderType, "TakeAway", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(x.AreaCode, "takeaway", StringComparison.OrdinalIgnoreCase));
        if (table is null)
        {
            StatusText = "Chưa có điểm bán mang đi trong danh sách bàn.";
            return;
        }

        if (SelectedArea != table.AreaName)
        {
            SelectedArea = table.AreaName;
        }

        await SelectTableAsync(table);
    }

    [RelayCommand]
    private void NavigateLeftTab(string tabIndexStr)
    {
        if (int.TryParse(tabIndexStr, out int idx) && idx >= 0 && idx <= 5)
            LeftTabIndex = idx;
    }

    [RelayCommand]
    private void SelectArea(string? area)
    {
        if (!string.IsNullOrWhiteSpace(area))
            SelectedArea = area;
    }

    [RelayCommand]
    private void SelectCategory(PosCategoryFilter? cat)
    {
        if (cat is null) return;
        foreach (var c in Categories)
            c.IsSelected = c == cat;
        SelectedCategory = cat;
    }

    [RelayCommand]
    private async Task SaveOrderNoteAsync()
    {
        if (CurrentOrder is null) return;
        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.UpdateOrderNoteAsync(CurrentOrder.Id, OrderNote);
            StatusText = string.IsNullOrWhiteSpace(OrderNote) ? "Đã xóa ghi chú đơn." : "Đã lưu ghi chú đơn.";
        });
    }

    [RelayCommand]
    private void StartChangeTable()
    {
        if (CurrentOrder is null) return;
        IsMergingOrder = false;
        IsChangingTable = true;
        LeftTabIndex = 0;
        StatusText = "Chọn bàn mới để chuyển đơn...";
    }

    [RelayCommand]
    private void StartMergeOrder()
    {
        if (CurrentOrder is null) return;
        IsChangingTable = false;
        IsMergingOrder = true;
        LeftTabIndex = 2;
        StatusText = "Chọn đơn đang mở để gộp vào đơn hiện tại...";
    }

    [RelayCommand]
    private async Task ChangeTableAsync(PosTableDto? newTable)
    {
        if (newTable is null || CurrentOrder is null) return;
        if (newTable.Id == CurrentOrder.PosTableId)
        {
            IsChangingTable = false;
            LeftTabIndex = 1;
            StatusText = "Đơn đang ở bàn này.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.ChangeTableAsync(CurrentOrder.Id, newTable.Id, "pos-ui");
            await RefreshOpenDataAsync();
            SelectedTable = _allTables.FirstOrDefault(x => x.Id == newTable.Id) ?? newTable;
            if (SelectedArea != newTable.AreaName)
                SelectedArea = newTable.AreaName;
            IsChangingTable = false;
            LeftTabIndex = 1;
            StatusText = $"Đã chuyển đơn sang {newTable.TableName}.";
        });
    }

    [RelayCommand]
    private async Task MergeOrderAsync(PosTableDto? sourceTable)
    {
        if (sourceTable is null || CurrentOrder is null) return;
        if (!sourceTable.OpenOrderId.HasValue)
        {
            StatusText = "Bàn này chưa có đơn mở để gộp.";
            return;
        }

        if (CurrentOrder.PosTableId == sourceTable.Id || sourceTable.OpenOrderId.Value == CurrentOrder.Id)
        {
            IsMergingOrder = false;
            LeftTabIndex = 1;
            StatusText = "Không thể gộp cùng một đơn.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.MergeOrdersAsync(CurrentOrder.Id, sourceTable.OpenOrderId.Value, "pos-ui");
            await RefreshOpenDataAsync();
            if (CurrentOrder.PosTableId.HasValue)
            {
                SelectedTable = _allTables.FirstOrDefault(x => x.Id == CurrentOrder.PosTableId.Value) ?? SelectedTable;
            }
            IsMergingOrder = false;
            LeftTabIndex = 1;
            StatusText = $"Đã gộp đơn từ {sourceTable.TableName} vào {CurrentOrder.OrderNumber}.";
        });
    }

    [RelayCommand]
    private async Task MergeOpenOrderAsync(BreakfastCafeOrderDto? sourceOrder)
    {
        if (sourceOrder is null || CurrentOrder is null)
        {
            return;
        }

        if (sourceOrder.Id == CurrentOrder.Id)
        {
            IsMergingOrder = false;
            LeftTabIndex = 1;
            StatusText = "Không thể gộp cùng một đơn.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.MergeOrdersAsync(CurrentOrder.Id, sourceOrder.Id, "pos-ui");
            await RefreshOpenDataAsync();
            if (CurrentOrder.PosTableId.HasValue)
            {
                SelectedTable = _allTables.FirstOrDefault(x => x.Id == CurrentOrder.PosTableId.Value) ?? SelectedTable;
            }
            IsMergingOrder = false;
            LeftTabIndex = 1;
            StatusText = $"Đã gộp {sourceOrder.OrderNumber} vào {CurrentOrder.OrderNumber}.";
        });
    }

    [RelayCommand]
    private void SelectPaymentMethod(string? method)
    {
        if (!string.IsNullOrWhiteSpace(method) && PaymentMethods.Contains(method))
        {
            SelectedPaymentMethod = method;
        }
    }

    [RelayCommand]
    private async Task SelectTableAsync(PosTableDto? table)
    {
        if (table is null)
        {
            return;
        }

        if (IsChangingTable)
        {
            await ChangeTableAsync(table);
            return;
        }

        if (IsMergingOrder)
        {
            await MergeOrderAsync(table);
            return;
        }

        await RunBusyAsync(async () =>
        {
            SelectedTable = table;
            CurrentOrder = await _salesService.OpenOrderForTableAsync(table.Id, "pos-ui");
            StatusText = $"{table.TableName}: đã mở {CurrentOrder.OrderNumber}.";
            LeftTabIndex = 1; // Auto-switch to menu tab for adding items
        });

        await RefreshOpenDataAsync();
    }

    [RelayCommand]
    private async Task AddMenuItemAsync(PosMenuItemCard? item)
    {
        if (item is null) return;
        await AddMenuItemFromSelectionAsync(item);
    }

    public Task AddMenuItemForSaleAsync(PosMenuItemCard? item)
        => item is null ? Task.CompletedTask : AddMenuItemFromSelectionAsync(item);

    private async Task AddMenuItemFromSelectionAsync(PosMenuItemCard item)
    {
        if (!item.CanOrder) { StatusText = $"Không thể bán: {item.StockWarning}"; return; }
        await RunBusyAsync(async () =>
        {
            if (CurrentOrder is null || CurrentOrder.Status is "Paid" or "Cancelled")
            {
                var table = SelectedTable ?? _allTables.FirstOrDefault(x => x.OpenOrderId.HasValue) ?? _allTables.FirstOrDefault();
                if (table is null)
                {
                    StatusText = "Vui lòng nháy đúp/chọn bàn trước khi thêm món.";
                    return;
                }

                SelectedTable = table;
                CurrentOrder = await _salesService.OpenOrderForTableAsync(table.Id, "pos-ui");
                StatusText = $"{table.TableName}: đã mở {CurrentOrder.OrderNumber}.";
            }

            if (CurrentOrder is null) return;

            CurrentOrder = await _salesService.AddLineAsync(new UpdateBreakfastCafeOrderLineRequest
            {
                OrderId = CurrentOrder.Id,
                MenuItemId = item.Id,
                Quantity = 1
            });
            CurrentOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id) ?? CurrentOrder;
            LeftTabIndex = 1;
            StatusText = $"Đã thêm {item.Name}.";
            CheckComboDiscount();
            await RefreshOpenDataAsync();
        });

        if (ReferenceEquals(SelectedMenuItemForSale, item))
        {
            SelectedMenuItemForSale = null;
        }
    }

    private void CheckComboDiscount()
    {
        if (CurrentOrder is null) return;
        var orderCodes = CurrentOrderLines.Select(x => x.MenuItemCode).ToHashSet();
        if (orderCodes.Contains("BM_TRUNG") && orderCodes.Contains("CF_DEN"))
            StatusText = "Combo bánh mì + cà phê: giá ưu đãi 35k!";
    }
    [RelayCommand]
    private async Task IncreaseLineAsync(PosOrderLineRow? line)
    {
        if (line is null || CurrentOrder is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.UpdateLineQuantityAsync(new UpdateBreakfastCafeOrderLineRequest
            {
                OrderId = CurrentOrder.Id,
                OrderLineId = line.Id,
                MenuItemId = line.MenuItemId,
                Quantity = line.Quantity + 1,
                Note = line.NoteText
            });
            await RefreshOpenDataAsync();
        });
    }

    [RelayCommand]
    private async Task DecreaseLineAsync(PosOrderLineRow? line)
    {
        if (line is null || CurrentOrder is null)
        {
            return;
        }

        if (line.Quantity <= 1)
        {
            await RemoveLineAsync(line);
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.UpdateLineQuantityAsync(new UpdateBreakfastCafeOrderLineRequest
            {
                OrderId = CurrentOrder.Id,
                OrderLineId = line.Id,
                MenuItemId = line.MenuItemId,
                Quantity = line.Quantity - 1,
                Note = line.NoteText
            });
            await RefreshOpenDataAsync();
        });
    }

    [RelayCommand]
    private async Task RemoveLineAsync(PosOrderLineRow? line)
    {
        if (line is null)
        {
            return;
        }

        if (CurrentOrder is null)
        {
            return;
        }

        var freshOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
        if (freshOrder is not null)
        {
            CurrentOrder = freshOrder;
        }

        var stationLabel = string.Equals(ResolvePreparationStation(line), "Bar", StringComparison.OrdinalIgnoreCase) ? "bar" : "bếp";
        if (HasLocalKitchenHistory(CurrentOrder) || string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            var confirmCancel = MessageBox.Show(
                $"Món {line.MenuItemName} đã gửi {stationLabel}.{Environment.NewLine}{Environment.NewLine}Bạn có muốn gửi phiếu hủy món vào {stationLabel} không?",
                "Xác nhận hủy món",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirmCancel != MessageBoxResult.Yes)
            {
                StatusText = "Đã hủy thao tác xóa món.";
                return;
            }

            await RemoveOrderLinesAsync([line], forceCancelTicketPromptAccepted: true);
            return;
        }

        await RemoveOrderLinesAsync([line]);
    }

    [RelayCommand]
    private async Task CancelSelectedLinesAsync()
    {
        var selectedLines = CurrentOrderLines.Where(x => x.IsSelected).ToList();
        if (selectedLines.Count == 0)
        {
            _dialogService.Info("Chưa chọn món để hủy.", "Thông báo");
            return;
        }

        if (CurrentOrder is null)
        {
            return;
        }

        var freshOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
        if (freshOrder is not null)
        {
            CurrentOrder = freshOrder;
        }

        var selectedIds = selectedLines.Select(x => x.Id).ToHashSet();
        var selectedCurrentLines = CurrentOrderLines.Where(x => selectedIds.Contains(x.Id)).ToList();
        var stationLabels = selectedCurrentLines
            .Select(ResolvePreparationStation)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => string.Equals(x, "Bar", StringComparison.OrdinalIgnoreCase) ? "bar" : "bếp")
            .ToList();
        var stationLabel = stationLabels.Count == 0 ? "bếp/bar" : string.Join("/", stationLabels);
        var confirmCancel = MessageBox.Show(
            $"Bạn có muốn gửi phiếu hủy món vào {stationLabel} không?",
            "Xác nhận hủy món",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmCancel != MessageBoxResult.Yes)
        {
            StatusText = "Đã hủy thao tác hủy món đã chọn.";
            return;
        }

        await RemoveOrderLinesAsync(selectedLines, forceCancelTicketPromptAccepted: true);
    }

    private async Task RemoveOrderLinesAsync(IReadOnlyList<PosOrderLineRow> lines, bool forceCancelTicketPromptAccepted = false)
    {
        if (CurrentOrder is null || lines.Count == 0)
        {
            return;
        }

        if (!CashierCanCancelItem)
        {
            StatusText = "Cấu hình hiện không cho phép thu ngân hủy món.";
            return;
        }

        var freshOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
        if (freshOrder is not null)
        {
            CurrentOrder = freshOrder;
        }

        var orderSent = HasLocalKitchenHistory(CurrentOrder);
        var selectedIds = lines.Select(x => x.Id).ToHashSet();
        var selectedCurrentLines = CurrentOrderLines.Where(x => selectedIds.Contains(x.Id)).ToList();
        var lineShowsSent = lines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase));
        var needsCancelTicket = orderSent || lineShowsSent || selectedCurrentLines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase));
        var shouldSendCancelTicket = forceCancelTicketPromptAccepted || needsCancelTicket;
        var stationLabels = selectedCurrentLines
            .Select(ResolvePreparationStation)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => string.Equals(x, "Bar", StringComparison.OrdinalIgnoreCase) ? "bar" : "bếp")
            .ToList();
        var stationLabel = stationLabels.Count == 0 ? "bếp/bar" : string.Join("/", stationLabels);
        var itemLabel = lines.Count == 1 ? lines[0].MenuItemName : $"{lines.Count} món đã chọn";

        if (needsCancelTicket && !forceCancelTicketPromptAccepted)
        {
            StatusText = $"Đang xác nhận hủy món {itemLabel}...";
            var confirmMessage = lines.Count == 1
                ? $"Món {itemLabel} đã gửi {stationLabel}.{Environment.NewLine}{Environment.NewLine}Bạn có muốn gửi phiếu hủy món vào {stationLabel} không?"
                : $"Các món đã chọn đã gửi {stationLabel}.{Environment.NewLine}{Environment.NewLine}Bạn có muốn gửi phiếu hủy món vào {stationLabel} không?";
            var confirmCancel = MessageBox.Show(
                confirmMessage,
                "Xác nhận hủy món",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirmCancel != MessageBoxResult.Yes)
            {
                StatusText = "Đã hủy thao tác xóa món.";
                return;
            }
        }

        await RunBusyAsync(async () =>
        {
            var order = CurrentOrder;
            var cancelKitchenLines = new List<KitchenPreviewLine>();
            var cancelBarLines = new List<KitchenPreviewLine>();
            var removedNames = new List<string>();
            try
            {
                if (shouldSendCancelTicket)
                {
                    StatusText = $"Đang hủy {itemLabel}...";
                }

                foreach (var selectedLine in lines)
                {
                    var currentLine = CurrentOrder?.Lines.FirstOrDefault(x => x.Id == selectedLine.Id);
                    if (currentLine is null)
                    {
                        continue;
                    }

                    var row = new PosOrderLineRow(currentLine);
                    var currentOrderId = CurrentOrder!.Id;
                    CurrentOrder = await _salesService.RemoveLineAsync(currentOrderId, selectedLine.Id);
                    removedNames.Add(row.MenuItemName);

                    if (!shouldSendCancelTicket)
                    {
                        continue;
                    }

                    var cancelLine = new KitchenPreviewLine(row.MenuItemName, row.Quantity, row.UnitPrice, row.LineTotalAmount, $"Hủy món: {row.MenuItemName}");
                    if (string.Equals(ResolvePreparationStation(row), "Bar", StringComparison.OrdinalIgnoreCase))
                    {
                        cancelBarLines.Add(cancelLine);
                    }
                    else
                    {
                        cancelKitchenLines.Add(cancelLine);
                    }
                }

                if (shouldSendCancelTicket)
                {
                    if (cancelKitchenLines.Count > 0)
                    {
                        await PrintPreparationTicketAsync(order!, "PHIẾU HỦY MÓN - BẾP", cancelKitchenLines, KitchenPrinterName, KitchenPaperSize);
                    }

                    if (cancelBarLines.Count > 0)
                    {
                        await PrintPreparationTicketAsync(order!, "PHIẾU HỦY MÓN - BAR", cancelBarLines, BarPrinterName, BarPaperSize);
                    }

                    StatusText = lines.Count == 1
                        ? $"Đã gửi hủy món {itemLabel} vào {stationLabel}."
                        : $"Đã gửi phiếu hủy {removedNames.Count} món vào {stationLabel}.";
                    _dialogService.Info(StatusText, "Hủy món");
                }
                else
                {
                    StatusText = lines.Count == 1
                        ? $"Đã xóa {itemLabel}."
                        : $"Đã xóa {removedNames.Count} món.";
                    _dialogService.Info(StatusText, "Xóa món");
                }

                if (CurrentOrder is not null && CurrentOrder.Lines.Count == 0)
                {
                    CurrentOrder.SubtotalAmount = 0m;
                    CurrentOrder.DiscountAmount = 0m;
                    CurrentOrder.VatAmount = 0m;
                    CurrentOrder.ServiceChargeAmount = 0m;
                    CurrentOrder.TotalAmount = 0m;
                    CurrentOrder.PaidAmount = 0m;
                    CurrentOrder.RemainingAmount = 0m;
                    DiscountAmount = 0m;
                    OnPropertyChanged(nameof(SubtotalAmount));
                    OnPropertyChanged(nameof(VatAmount));
                    OnPropertyChanged(nameof(ServiceChargeAmount));
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
            catch (Exception ex)
            {
                StatusText = shouldSendCancelTicket
                    ? $"Không gửi được phiếu hủy món {itemLabel}: {ex.Message}"
                    : $"Không xóa được món {itemLabel}: {ex.Message}";
                _dialogService.Error(StatusText, "Lỗi");
            }
            await RefreshOpenDataAsync();
        });
    }

    [RelayCommand]
    private void ToggleLineNotePopup(PosOrderLineRow? line)
    {
        if (line is null)
        {
            return;
        }

        foreach (var row in CurrentOrderLines.Where(x => x.Id != line.Id))
        {
            row.IsNotePopupOpen = false;
        }

        line.IsNotePopupOpen = !line.IsNotePopupOpen;
    }

    [RelayCommand]
    private async Task SaveLineNoteAsync(PosOrderLineRow? line)
    {
        if (line is null || CurrentOrder is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.UpdateLineNoteAsync(CurrentOrder.Id, line.Id, line.NoteText);
            StatusText = $"Đã lưu ghi chú cho {line.MenuItemName}.";
            line.IsNotePopupOpen = false;
        });
    }

    [RelayCommand]
    private async Task ApplyDiscountAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        if (!AllowOrderDiscount)
        {
            StatusText = "Cấu hình hiện không cho phép giảm giá trên đơn.";
            return;
        }

        var maxByPercent = CurrentOrder.SubtotalAmount * MaxOrderDiscountPercent / 100m;
        var cashierMaxByPercent = CurrentOrder.SubtotalAmount * CashierMaxDiscountPercent / 100m;
        var maxAllowed = MaxOrderDiscountAmount > 0m
            ? Math.Min(maxByPercent, MaxOrderDiscountAmount)
            : maxByPercent;
        maxAllowed = Math.Min(maxAllowed, cashierMaxByPercent);
        if (DiscountAmount > maxAllowed)
        {
            StatusText = $"Giảm giá vượt ngưỡng cấu hình ({maxAllowed:N0} đ).";
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.ApplyDiscountAsync(CurrentOrder.Id, DiscountAmount);
            StatusText = "Đã áp dụng giảm giá.";
            await RefreshOpenDataAsync();
        });
    }

    [RelayCommand]
    private async Task SendKitchenAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var freshOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
            if (freshOrder is not null)
            {
                CurrentOrder = freshOrder;
            }
        });

        if (CurrentOrderLines.Count == 0)
        {
            StatusText = "Đơn chưa có món để gửi bếp/bar.";
            return;
        }

        var order = CurrentOrder;
        var sentBefore = HasLocalKitchenHistory(CurrentOrder) || CurrentOrderLines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase));
        var sendLines = GetPendingKitchenLines();
        var kitchenLines = sendLines
            .Where(x => ResolvePreparationStation(x) == "Kitchen")
            .Select(x => CloneKitchenPreviewLineWithAdditionalNote(x, sentBefore ? "Món thêm" : null))
            .ToList();
        var barLines = sendLines
            .Where(x => ResolvePreparationStation(x) == "Bar")
            .Select(x => CloneKitchenPreviewLineWithAdditionalNote(x, sentBefore ? "Món thêm" : null))
            .ToList();
        var unsentCount = sendLines.Count;
        if (unsentCount == 0)
        {
            StatusText = "Phiếu này đã được gửi bếp/bar rồi. Chỉ khi thêm món mới thì mới gửi tiếp.";
            _dialogService.Info(StatusText, "Thông báo");
            return;
        }
        if (sentBefore && unsentCount > 0)
        {
            StatusText = $"Đơn đã gửi trước đó, chỉ gửi {unsentCount} món mới thêm.";
            _dialogService.Info(StatusText, "Thông báo");
        }

        var station = "KitchenBar";
        var previewTitle = sentBefore ? "PHIẾU BỔ SUNG MÓN" : "PHIẾU BẾP / BAR";
        var doc = BuildKitchenBarPreviewDocument(order, kitchenLines, barLines, previewTitle);
        var ticketSubmitted = false;
        ShowPrintPreview(
            $"Xem trước gửi bếp/bar — {order.OrderNumber}",
            doc,
            "Gửi bếp/bar & in",
            $"Bep bar {order.OrderNumber}",
            async () =>
            {
                if (ticketSubmitted)
                {
                    StatusText = "Phiếu đã gửi rồi, lần bấm này chỉ in lại bản xem trước.";
                    _dialogService.Info(StatusText, "Thông báo");
                    return false;
                }

                var submitted = false;
                await RunBusyAsync(async () =>
                {
                    var tickets = await _salesService.SendToKitchenAsync(new SendBreakfastCafeKitchenRequest
                    {
                        OrderId = order.Id,
                        Station = station,
                        SentBy = "pos-ui"
                    });
                    CurrentOrder = await _salesService.GetOrderDetailAsync(order.Id);
                    foreach (var row in CurrentOrderLines.Where(x => sendLines.Any(s => s.Id == x.Id)))
                    {
                        row.KitchenStatus = "Sent";
                    }
                    if (CurrentOrder is not null)
                    {
                        _ordersWithKitchenHistory.Add(CurrentOrder.Id);
                        if (!_orderSentKitchenLineIds.TryGetValue(CurrentOrder.Id, out var sentIds))
                        {
                            sentIds = [];
                            _orderSentKitchenLineIds[CurrentOrder.Id] = sentIds;
                        }

                        foreach (var sentLine in sendLines)
                        {
                            sentIds.Add(sentLine.Id);
                        }
                        CurrentOrder.KitchenTicketCount = Math.Max(CurrentOrder.KitchenTicketCount, tickets.Count);
                        CurrentOrder.KitchenSentAt ??= DateTime.UtcNow;
                        CurrentOrder.ServiceStatus = "SentKitchen";
                    }
                    OnPropertyChanged(nameof(CanSendKitchen));
                    ticketSubmitted = true;
                    submitted = true;
                    StatusText = $"Đã gửi {tickets.Count} phiếu: {string.Join(", ", tickets.Select(t => t.Station + " " + t.TicketNumber))}.";
                    _dialogService.Info(StatusText, "Gửi bếp/bar");
                    await RefreshOpenDataAsync();
                });
                return submitted;
            },
            async () => await PrintKitchenBarTicketsAsync(order, kitchenLines, barLines, sentBefore, previewTitle));
    }

    [RelayCommand]
    private async Task PrintAllKitchenBarAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var freshOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
            if (freshOrder is not null)
            {
                CurrentOrder = freshOrder;
            }
        });

        if (CurrentOrderLines.Count == 0)
        {
            StatusText = "Đơn chưa có món để in bếp/bar.";
            return;
        }

        var order = CurrentOrder;
        var currentLineRows = SnapshotCurrentOrderLineRows();
        var kitchenLines = currentLineRows
            .Where(x => ResolvePreparationStation(x) == "Kitchen")
            .Select(CloneKitchenPreviewLine)
            .ToList();
        var barLines = currentLineRows
            .Where(x => ResolvePreparationStation(x) == "Bar")
            .Select(CloneKitchenPreviewLine)
            .ToList();

        var doc = BuildKitchenBarPreviewDocument(order, kitchenLines, barLines);
        ShowPrintPreview(
            $"Xem trước in toàn bộ bếp/bar — {order.OrderNumber}",
            doc,
            "In toàn bộ bếp/bar",
            $"In toan bo bep bar {order.OrderNumber}",
            directPrintAsync: async () => await PrintKitchenBarTicketsAsync(order, kitchenLines, barLines));
    }

    private async Task SendKitchenDirectAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var tickets = await _salesService.SendToKitchenAsync(new SendBreakfastCafeKitchenRequest
            {
                OrderId = CurrentOrder.Id,
                Station = "KitchenBar",
                SentBy = "pos-ui"
            });
            CurrentOrder = await _salesService.GetOrderDetailAsync(CurrentOrder.Id);
            StatusText = $"Đã gửi {tickets.Count} phiếu: {string.Join(", ", tickets.Select(t => t.Station + " " + t.TicketNumber))}.";
            await RefreshOpenDataAsync();
        });
    }

    [RelayCommand]
    private async Task TestKitchenPrinterAsync()
    {
        await PrintTestTicketAsync("PHIẾU TEST BẾP", KitchenPrinterName, KitchenPaperSize);
    }

    [RelayCommand]
    private async Task TestBarPrinterAsync()
    {
        await PrintTestTicketAsync("PHIẾU TEST BAR", BarPrinterName, BarPaperSize);
    }

    [RelayCommand]
    private async Task TestReceiptPrinterAsync()
    {
        await PrintTestTicketAsync("PHIẾU TEST HÓA ĐƠN", ReceiptPrinterName, ReceiptPaperSize);
    }

    public string ShiftFormHeaderText => SelectedShiftSetting is null
        ? "Thêm ca"
        : $"Sửa ca: {SelectedShiftSetting.Code} - {SelectedShiftSetting.Name}";

    [RelayCommand]
    private void EditShiftSetting(ShiftSettingRow? shift)
    {
        if (shift is null)
        {
            return;
        }

        SelectedShiftSetting = shift;
        ShiftFormCode = shift.Code;
        ShiftFormName = shift.Name;
        ShiftFormStartTime = shift.StartTime;
        ShiftFormEndTime = shift.EndTime;
        ShiftFormIsActive = shift.IsActive;
        ShiftSubTabIndex = 1;
    }

    [RelayCommand]
    private void ClearShiftForm()
    {
        SelectedShiftSetting = null;
        ShiftFormCode = string.Empty;
        ShiftFormName = string.Empty;
        ShiftFormStartTime = "06:00";
        ShiftFormEndTime = "14:00";
        ShiftFormIsActive = true;
    }

    [RelayCommand]
    private void SaveShiftSetting()
    {
        var code = string.IsNullOrWhiteSpace(ShiftFormCode) ? CreateShiftCode(ShiftFormName) : ShiftFormCode.Trim();
        var name = ShiftFormName.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            StatusText = "Nhập mã ca và tên ca trước khi lưu.";
            return;
        }

        if (!IsValidShiftTime(ShiftFormStartTime) || !IsValidShiftTime(ShiftFormEndTime))
        {
            StatusText = "Giờ ca phải theo định dạng HH:mm, ví dụ 06:00.";
            return;
        }

        var existing = ShiftSettings.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            ShiftSettings.Add(new ShiftSettingRow(code, name, ShiftFormStartTime.Trim(), ShiftFormEndTime.Trim(), ShiftFormIsActive));
            StatusText = $"Đã thêm ca {name}.";
        }
        else
        {
            existing.Name = name;
            existing.StartTime = ShiftFormStartTime.Trim();
            existing.EndTime = ShiftFormEndTime.Trim();
            existing.IsActive = ShiftFormIsActive;
            StatusText = $"Đã cập nhật ca {name}.";
        }

        CurrentShiftName = ShiftSettings.FirstOrDefault(x => x.IsActive)?.Name ?? name;
        ShiftSubTabIndex = 0;
        SelectedShiftSetting = null;
        ShiftFormCode = string.Empty;
        ShiftFormName = string.Empty;
        ShiftFormStartTime = "06:00";
        ShiftFormEndTime = "14:00";
        ShiftFormIsActive = true;
    }

    [RelayCommand]
    private void DeleteShiftSetting(ShiftSettingRow? shift)
    {
        shift ??= SelectedShiftSetting;
        if (shift is null)
        {
            return;
        }

        ShiftSettings.Remove(shift);
        if (CurrentShiftName.Equals(shift.Name, StringComparison.OrdinalIgnoreCase))
        {
            CurrentShiftName = ShiftSettings.FirstOrDefault(x => x.IsActive)?.Name ?? "Ca sáng";
        }

        ShiftSubTabIndex = 0;
        ClearShiftForm();
        StatusText = $"Đã xóa ca {shift.Name}.";
    }

    [RelayCommand]
    private async Task QuickAddByCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddCode)) return;
        var code = QuickAddCode.Trim();
        QuickAddCode = string.Empty;
        var item = _allMenuItems.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(code, StringComparison.OrdinalIgnoreCase));
        if (item is null) { StatusText = $"Không tìm thấy món: {code}"; return; }
        await AddMenuItemFromSelectionAsync(new PosMenuItemCard(item));
    }


    [RelayCommand]
    private async Task SplitOrderAsync()
    {
        if (CurrentOrder is null)
        {
            StatusText = "Chọn bàn và mở đơn trước khi chia đơn.";
            return;
        }

        var nextIndex = Math.Max(2, CurrentOrderTabs.Count + 1);
        var tabName = $"Khách {nextIndex}";

        CurrentOrder = await _salesService.CreateOrderAsync(new CreateBreakfastCafeOrderRequest
        {
            PosTableId = SelectedTable?.Id,
            OrderType = CurrentOrder.OrderType,
            AreaName = CurrentOrder.AreaName,
            TableCode = CurrentOrder.TableCode,
            TableName = CurrentOrder.TableName,
            Note = tabName,
            CreatedBy = "pos-ui-split"
        });

        CurrentOrderTabName = tabName;
        StatusText = $"Đã tạo tab {tabName} cho {CurrentOrder.TableName ?? "bàn hiện tại"}.";
        await RefreshOpenDataAsync();
        BuildCurrentOrderTabs();
    }

    [RelayCommand]
    private async Task SelectOrderTabAsync(PosOrderTabRow? tab)
    {
        if (tab is null)
        {
            return;
        }

        var detail = await _salesService.GetOrderDetailAsync(tab.OrderId);
        if (detail is null)
        {
            StatusText = "Không tìm thấy đơn của tab đã chọn.";
            await RefreshOpenDataAsync();
            return;
        }

        CurrentOrder = detail;
        CurrentOrderTabName = tab.DisplayName;
        BuildCurrentOrderTabs();
    }

    [RelayCommand]
    private async Task SaveOrderTabNameAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(CurrentOrderTabName)
            ? ResolveOrderTabName(CurrentOrder, 1)
            : CurrentOrderTabName.Trim();

        CurrentOrder = await _salesService.UpdateOrderNoteAsync(CurrentOrder.Id, name);
        CurrentOrderTabName = name;
        StatusText = $"Đã lưu tên tab: {name}.";
        await RefreshOpenDataAsync();
        BuildCurrentOrderTabs();
    }

    [RelayCommand]
    private void PrintInvoice()
    {
        if (CurrentOrder is null) { StatusText = "Chưa có đơn để in."; return; }
        if (!AllowProvisionalPrint)
        {
            StatusText = "Cấu hình hiện không cho phép in tạm tính.";
            return;
        }
        var order = CurrentOrder;
        var lines = SnapshotCurrentOrderPrintLines();
        var doc = BuildProvisionalInvoiceDocument(order, lines);
        ShowPrintPreview(
            $"Xem trước tạm tính — {order.OrderNumber}",
            doc,
            "In tạm tính",
            $"Tam tinh {order.OrderNumber}",
            directPrintAsync: async () => await PrintReceiptAsync(order, lines, "PHIẾU TẠM TÍNH", $"Tam tinh {order.OrderNumber}"));
    }

    [RelayCommand]
    private void PrintProvisional()
    {
        PrintInvoice();
    }

    private static KitchenPreviewLine CloneKitchenPreviewLine(PosOrderLineRow line)
        => new(line.MenuItemName, line.Quantity, line.UnitPrice, line.LineTotalAmount, line.NoteText);

    private List<PosOrderLineRow> SnapshotCurrentOrderLineRows()
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var snapshot = new List<PosOrderLineRow>(CurrentOrderLines.Count);
                for (int index = 0; index < CurrentOrderLines.Count; index++)
                {
                    snapshot.Add(CurrentOrderLines[index]);
                }

                return snapshot;
            }
            catch (ArgumentOutOfRangeException) when (attempt < maxAttempts - 1)
            {
            }
            catch (InvalidOperationException ex)
                when (attempt < maxAttempts - 1
                    && ex.Message.Contains("Collection was modified", StringComparison.OrdinalIgnoreCase))
            {
            }
        }

        return [];
    }

    private List<KitchenPreviewLine> SnapshotCurrentOrderPrintLines()
    {
        if (CurrentOrder?.Lines is { Count: > 0 } orderLines)
        {
            return orderLines
                .OrderBy(x => x.LineNo)
                .Select(x => new KitchenPreviewLine(x.MenuItemName, x.Quantity, x.UnitPrice, x.LineTotalAmount, x.Note))
                .ToList();
        }

        return SnapshotCurrentOrderLineRows()
            .Select(CloneKitchenPreviewLine)
            .ToList();
    }

    private static KitchenPreviewLine CloneKitchenPreviewLineWithAdditionalNote(PosOrderLineRow line, string? additionalNote)
    {
        var note = string.IsNullOrWhiteSpace(additionalNote)
            ? line.NoteText
            : string.IsNullOrWhiteSpace(line.NoteText)
                ? additionalNote
                : $"{additionalNote} - {line.NoteText}";
        return new KitchenPreviewLine(line.MenuItemName, line.Quantity, line.UnitPrice, line.LineTotalAmount, note);
    }

    private static bool HasKitchenHistory(BreakfastCafeOrderDto? order)
        => order is not null
           && (order.KitchenSentAt.HasValue
               || order.KitchenTicketCount > 0
               || order.KitchenHistoryCount > 0
               || string.Equals(order.ServiceStatus, "SentKitchen", StringComparison.OrdinalIgnoreCase));

    private bool HasLocalKitchenHistory(BreakfastCafeOrderDto? order)
        => order is not null && (_ordersWithKitchenHistory.Contains(order.Id) || HasKitchenHistory(order));

    private List<PosOrderLineRow> GetPendingKitchenLines()
    {
        if (CurrentOrder is null)
        {
            return [];
        }

        var hadKitchenHistory = HasLocalKitchenHistory(CurrentOrder);
        if (hadKitchenHistory)
        {
            var sentIds = _orderSentKitchenLineIds.TryGetValue(CurrentOrder.Id, out var knownSentIds)
                ? knownSentIds
                : [];
            return CurrentOrderLines
                .Where(x => !sentIds.Contains(x.Id))
                .ToList();
        }

        return CurrentOrderLines
            .Where(x => !string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsPendingKitchenLine(PosOrderLineRow line, DateTime? kitchenSentAt, bool hadKitchenHistory)
    {
        if (string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!hadKitchenHistory || !kitchenSentAt.HasValue)
        {
            return true;
        }

        if (line.CreatedAt > kitchenSentAt.Value || line.UpdatedAt > kitchenSentAt.Value)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(line.NoteText) && line.NoteText.Contains("Món thêm", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolvePreparationStation(PosOrderLineRow line)
    {
        var item = _allMenuItems.FirstOrDefault(x => x.Id == line.MenuItemId);
        if (IsBarPreparationArea(item?.PreparationArea))
        {
            return "Bar";
        }

        if (IsKitchenPreparationArea(item?.PreparationArea))
        {
            return "Kitchen";
        }

        var code = (item?.Code ?? line.MenuItemCode).Trim().ToUpperInvariant();
        var text = $"{item?.CategoryName} {item?.Name} {line.MenuItemName}".ToLowerInvariant();
        if (code.StartsWith("CF_", StringComparison.Ordinal)
            || code.StartsWith("TRA_", StringComparison.Ordinal)
            || code.StartsWith("NUOC_", StringComparison.Ordinal)
            || code.Contains("LATTE", StringComparison.Ordinal)
            || code.Contains("CAPPU", StringComparison.Ordinal)
            || code.Contains("BAC_XIU", StringComparison.Ordinal)
            || text.Contains("cà phê", StringComparison.Ordinal)
            || text.Contains("coffee", StringComparison.Ordinal)
            || text.Contains("trà", StringComparison.Ordinal)
            || text.Contains("nước", StringComparison.Ordinal)
            || text.Contains("latte", StringComparison.Ordinal)
            || text.Contains("cappu", StringComparison.Ordinal)
            || text.Contains("bạc xỉu", StringComparison.Ordinal))
        {
            return "Bar";
        }

        return "Kitchen";
    }

    private static bool IsBarPreparationArea(string? preparationArea)
    {
        var value = (preparationArea ?? string.Empty).Trim();
        return value.Equals("Bar", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("BAR", StringComparison.OrdinalIgnoreCase)
            || value.Contains("bar", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKitchenPreparationArea(string? preparationArea)
    {
        var value = (preparationArea ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value)
            || value.Equals("Kitchen", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("KITCHEN", StringComparison.OrdinalIgnoreCase)
            || value.Contains("bếp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidShiftTime(string value)
        => TimeSpan.TryParse(value.Trim(), out var time) && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);

    private static string CreateShiftCode(string name)
    {
        var raw = string.IsNullOrWhiteSpace(name) ? "SHIFT" : name.Trim();
        var ascii = new string(raw
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_')
            .ToArray());
        while (ascii.Contains("__", StringComparison.Ordinal))
        {
            ascii = ascii.Replace("__", "_", StringComparison.Ordinal);
        }

        ascii = ascii.Trim('_');
        return string.IsNullOrWhiteSpace(ascii) ? "SHIFT" : ascii;
    }

    private void EnsureDefaultShiftSettings()
    {
        if (ShiftSettings.Count > 0)
        {
            return;
        }

        ShiftSettings.Add(new ShiftSettingRow("MORNING", "Ca sáng", "06:00", "14:00", true));
        ShiftSettings.Add(new ShiftSettingRow("AFTERNOON", "Ca chiều", "14:00", "22:00", true));
        ShiftSettings.Add(new ShiftSettingRow("NIGHT", "Ca đêm", "22:00", "06:00", true));
    }

    private static System.Windows.Documents.FlowDocument CreateThermalFlowDocument(string paperSize, double fontSize = 11)
    {
        var paperWidth = GetThermalPaperWidth(paperSize);
        return new System.Windows.Documents.FlowDocument
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = fontSize,
            PagePadding = new System.Windows.Thickness(4),
            PageWidth = paperWidth,
            ColumnWidth = paperWidth - 8
        };
    }

    private static void ApplyReceiptPrintLayout(System.Windows.Documents.FlowDocument doc, string paperSize)
    {
        var paperWidth = GetThermalPaperWidth(paperSize);
        doc.PagePadding = new System.Windows.Thickness(4);
        doc.PageWidth = paperWidth;
        doc.PageHeight = 1000;
        doc.ColumnWidth = paperWidth - 8;
    }

    private static double GetThermalPaperWidth(string paperSize)
        => NormalizePaperSize(paperSize).StartsWith("58", StringComparison.OrdinalIgnoreCase)
            || NormalizePaperSize(paperSize).StartsWith("60", StringComparison.OrdinalIgnoreCase)
                ? 230
                : 300;

    private static int GetThermalSeparatorLength(string paperSize)
        => NormalizePaperSize(paperSize).StartsWith("58", StringComparison.OrdinalIgnoreCase)
            || NormalizePaperSize(paperSize).StartsWith("60", StringComparison.OrdinalIgnoreCase)
                ? 26
                : 34;

    private string ResolveKitchenBarPreviewPaperSize()
        => NormalizePaperSize(KitchenPaperSize).StartsWith("58", StringComparison.OrdinalIgnoreCase)
            && NormalizePaperSize(BarPaperSize).StartsWith("58", StringComparison.OrdinalIgnoreCase)
                ? "58mm"
                : "80mm";

    private System.Windows.Documents.FlowDocument BuildProvisionalInvoiceDocument(BreakfastCafeOrderDto order, IReadOnlyList<KitchenPreviewLine> lines, string? paperSize = null)
    {
        paperSize ??= ReceiptPaperSize;
        var doc = CreateThermalFlowDocument(paperSize, 11);
        var separatorLength = GetThermalSeparatorLength(paperSize);

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("PHIẾU TẠM TÍNH"))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(order.OrderNumber))
        {
            FontSize = 12,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 2, 0, 0)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Bàn: {order.TableName ?? "Mang đi"}  |  {order.OrderDate:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 11,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 6)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });

        foreach (var line in lines)
        {
            var p = new System.Windows.Documents.Paragraph
            {
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 2, 0, 0)
            };
            p.Inlines.Add(new System.Windows.Documents.Run(line.MenuItemName) { FontWeight = System.Windows.FontWeights.SemiBold });
            p.Inlines.Add(new System.Windows.Documents.Run($"\n  {line.Quantity:N1} x {line.UnitPrice:N0}đ = {line.LineTotalAmount:N0}đ"));
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  Ghi chú: {line.Note}"));
            }
            doc.Blocks.Add(p);
        }

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });
        var totals = new System.Windows.Documents.Paragraph
        {
            FontSize = 11,
            TextAlignment = System.Windows.TextAlignment.Center
        };
        totals.Inlines.Add(new System.Windows.Documents.Run($"Tạm tính: {order.SubtotalAmount:N0}đ\n"));
        if (order.DiscountAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"Giảm giá: -{order.DiscountAmount:N0}đ\n"));
        if (order.VatAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"VAT: {order.VatAmount:N0}đ\n"));
        if (order.ServiceChargeAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"Phí DV: {order.ServiceChargeAmount:N0}đ\n"));
        doc.Blocks.Add(totals);
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"TỔNG: {order.TotalAmount:N0}đ"))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12))
        });

        if (!string.IsNullOrWhiteSpace(ReceiptFooterText))
        {
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(ReceiptFooterText))
            {
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            });
        }

        return doc;
    }

    private System.Windows.Documents.FlowDocument BuildKitchenBarPreviewDocument(BreakfastCafeOrderDto order, IReadOnlyList<KitchenPreviewLine> kitchenLines, IReadOnlyList<KitchenPreviewLine> barLines, string title = "PHIẾU BẾP / BAR")
    {
        var paperSize = ResolveKitchenBarPreviewPaperSize();
        var doc = CreateThermalFlowDocument(paperSize, 12);
        var separatorLength = GetThermalSeparatorLength(paperSize);

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{order.OrderNumber}  |  {order.TableName ?? "Mang đi"}"))
        {
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 2, 0, 0)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Giờ gửi: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 11,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 6)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });

        AddPreparationSection(doc, "BẾP - ĐỒ ĂN", kitchenLines);
        AddPreparationSection(doc, "BAR - ĐỒ UỐNG", barLines);

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Kiểm tra đúng khu chế biến trước khi in/gửi.")
        )
        {
            FontSize = 11,
            TextAlignment = System.Windows.TextAlignment.Center
        });

        return doc;
    }

    private System.Windows.Documents.FlowDocument BuildPreparationTicketDocument(BreakfastCafeOrderDto order, string title, IReadOnlyList<KitchenPreviewLine> lines, string paperSize)
    {
        var doc = CreateThermalFlowDocument(paperSize, 12);
        var separatorLength = GetThermalSeparatorLength(paperSize);

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{order.OrderNumber}  |  {order.TableName ?? "Mang đi"}"))
        {
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 2, 0, 0)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Giờ gửi: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 11,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 6)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });

        foreach (var line in lines)
        {
            var p = new System.Windows.Documents.Paragraph
            {
                FontSize = 13,
                Margin = new System.Windows.Thickness(0, 4, 0, 0),
                TextAlignment = System.Windows.TextAlignment.Center
            };
            p.Inlines.Add(new System.Windows.Documents.Run($"{line.Quantity:N1} x {line.MenuItemName}") { FontWeight = System.Windows.FontWeights.Bold });
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  Ghi chú: {line.Note}"));
            }
            doc.Blocks.Add(p);
        }

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', separatorLength)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        });
        return doc;
    }

    private static void AddPreparationSection(System.Windows.Documents.FlowDocument doc, string title, IReadOnlyList<KitchenPreviewLine> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
        {
            FontSize = 14,
            FontWeight = System.Windows.FontWeights.Bold,
            Margin = new System.Windows.Thickness(0, 8, 0, 2),
            TextAlignment = System.Windows.TextAlignment.Center,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175))
        });

        foreach (var line in lines)
        {
            var p = new System.Windows.Documents.Paragraph
            {
                FontSize = 13,
                Margin = new System.Windows.Thickness(0, 4, 0, 0),
                TextAlignment = System.Windows.TextAlignment.Center
            };
            p.Inlines.Add(new System.Windows.Documents.Run($"{line.Quantity:N1} x {line.MenuItemName}") { FontWeight = System.Windows.FontWeights.Bold });
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  Ghi chú: {line.Note}"));
            }
            doc.Blocks.Add(p);
        }
    }

    private async Task PrintKitchenBarTicketsAsync(BreakfastCafeOrderDto order, IReadOnlyList<KitchenPreviewLine> kitchenLines, IReadOnlyList<KitchenPreviewLine> barLines, bool isSupplemental = false, string? ticketTitle = null)
    {
        var printed = new List<string>();
        var kitchenTitle = isSupplemental ? "PHIẾU BỔ SUNG MÓN - BẾP" : "PHIẾU BẾP";
        var barTitle = isSupplemental ? "PHIẾU BỔ SUNG MÓN - BAR" : "PHIẾU BAR";
        if (kitchenLines.Count > 0)
        {
            await PrintPreparationTicketAsync(order, kitchenTitle, kitchenLines, KitchenPrinterName, KitchenPaperSize);
            printed.Add($"bếp ({KitchenPrinterName})");
        }

        if (barLines.Count > 0)
        {
            await PrintPreparationTicketAsync(order, barTitle, barLines, BarPrinterName, BarPaperSize);
            printed.Add($"bar ({BarPrinterName})");
        }

        StatusText = printed.Count == 0
            ? "Không có dòng món cần in bếp/bar."
            : $"Đã in phiếu {string.Join(", ", printed)}.";
    }

    private async Task PrintReceiptAsync(BreakfastCafeOrderDto order, IReadOnlyList<KitchenPreviewLine> lines, string title, string jobName)
    {
        if (string.IsNullOrWhiteSpace(ReceiptPrinterName))
        {
            throw new InvalidOperationException("Chưa cấu hình máy in hóa đơn.");
        }

        if (TryParseNetworkPrinterTarget(ReceiptPrinterName, out var host, out var port))
        {
            var text = BuildThermalReceiptText(order, lines, title);
            await SendRawThermalPrintAsync(host, port, text);
            StatusText = $"Đã in hóa đơn tới {ReceiptPrinterName}.";
            return;
        }

        var doc = BuildProvisionalInvoiceDocument(order, lines);
        PrintFlowDocumentToWindowsPrinter(doc, ReceiptPrinterName, jobName);
        StatusText = $"Đã in hóa đơn tới {ReceiptPrinterName}.";
    }

    private async Task PrintReceiptSnapshotAsync(System.Windows.Documents.FlowDocument doc, string jobName, string thermalText)
    {
        if (string.IsNullOrWhiteSpace(ReceiptPrinterName))
        {
            throw new InvalidOperationException("Chưa cấu hình máy in hóa đơn.");
        }

        if (TryParseNetworkPrinterTarget(ReceiptPrinterName, out var host, out var port))
        {
            await SendRawThermalPrintAsync(host, port, thermalText);
            StatusText = $"Đã in hóa đơn tới {ReceiptPrinterName}.";
            return;
        }

        ApplyReceiptPrintLayout(doc, ReceiptPaperSize);
        PrintFlowDocumentToWindowsPrinter(doc, ReceiptPrinterName, jobName);
        StatusText = $"Đã in hóa đơn tới {ReceiptPrinterName}.";
    }

    private async Task PrintPreparationTicketAsync(BreakfastCafeOrderDto order, string title, IReadOnlyList<KitchenPreviewLine> lines, string printerTarget, string paperSize)
    {
        if (string.IsNullOrWhiteSpace(printerTarget))
        {
            throw new InvalidOperationException($"Chưa cấu hình máy in cho {title.ToLowerInvariant()}.");
        }

        if (TryParseNetworkPrinterTarget(printerTarget, out var host, out var port))
        {
            var text = BuildThermalTicketText(order, title, lines, paperSize);
            await SendRawThermalPrintAsync(host, port, text);
            return;
        }

        var doc = BuildPreparationTicketDocument(order, title, lines, paperSize);
        PrintFlowDocumentToWindowsPrinter(doc, printerTarget, $"{title} {order.OrderNumber}");
    }

    private async Task PrintTestTicketAsync(string title, string printerTarget, string paperSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(printerTarget))
            {
                StatusText = $"Chưa cấu hình máy in cho {title.ToLowerInvariant()}.";
                return;
            }

            if (TryParseNetworkPrinterTarget(printerTarget, out var host, out var port))
            {
                var columns = GetThermalPaperColumns(paperSize);
                var text = string.Join(Environment.NewLine,
                    CenterThermalText(title, columns),
                    CenterThermalText("ASMILE BO", columns),
                    CenterThermalText($"May in: {printerTarget}", columns),
                    CenterThermalText($"Kho giay: {NormalizePaperSize(paperSize)}", columns),
                    CenterThermalText($"Thoi gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", columns),
                    new string('-', columns),
                    CenterThermalText("In thu thanh cong.", columns),
                    string.Empty,
                    string.Empty,
                    string.Empty);
                await SendRawThermalPrintAsync(host, port, text);
            }
            else
            {
                var doc = BuildTestPrinterDocument(title, printerTarget, paperSize);
                PrintFlowDocumentToWindowsPrinter(doc, printerTarget, title);
            }

            StatusText = $"Đã gửi lệnh in thử tới {printerTarget}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Không in thử được {printerTarget}: {ex.Message}";
            System.Windows.MessageBox.Show(StatusText, "Lỗi in thử", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static System.Windows.Documents.FlowDocument BuildTestPrinterDocument(string title, string printerTarget, string paperSize)
    {
        var doc = CreateThermalFlowDocument(paperSize, 12);
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Máy in: {printerTarget}\nKhổ giấy: {NormalizePaperSize(paperSize)}\nThời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nIn thử thành công."))
        {
            FontSize = 12,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        return doc;
    }

    private static bool TryParseNetworkPrinterTarget(string value, out string host, out int port)
    {
        host = string.Empty;
        port = 9100;
        var target = value.Trim();
        if (target.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            target = target[6..];
        }

        var parts = target.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var candidateHost = parts[0];
        if (!IPAddress.TryParse(candidateHost, out _))
        {
            return false;
        }

        if (parts.Length == 2 && (!int.TryParse(parts[1], out port) || port <= 0))
        {
            port = 9100;
        }

        host = candidateHost;
        return true;
    }

    private static string BuildThermalTicketText(BreakfastCafeOrderDto order, string title, IReadOnlyList<KitchenPreviewLine> lines, string paperSize)
    {
        var columns = GetThermalPaperColumns(paperSize);
        var sb = new StringBuilder();
        sb.AppendLine(CenterThermalText(title, columns));
        sb.AppendLine(CenterThermalText(order.OrderNumber, columns));
        sb.AppendLine(CenterThermalText($"Ban: {order.TableName ?? "Mang di"}", columns));
        sb.AppendLine(CenterThermalText($"Gio gui: {DateTime.Now:dd/MM/yyyy HH:mm}", columns));
        sb.AppendLine(new string('-', columns));
        foreach (var line in lines)
        {
            sb.AppendLine(CenterThermalText($"{line.Quantity:N1} x {line.MenuItemName}", columns));
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine(CenterThermalText($"Ghi chu: {line.Note}", columns));
            }
        }
        sb.AppendLine(new string('-', columns));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine();
        return sb.ToString();
    }

    private static int GetThermalPaperColumns(string paperSize)
        => NormalizePaperSize(paperSize).StartsWith("58", StringComparison.OrdinalIgnoreCase) ? 32 : 42;

    private static string NormalizePaperSize(string paperSize)
        => string.IsNullOrWhiteSpace(paperSize) ? "80mm" : paperSize.Trim();

    private static string CenterThermalText(string value, int columns)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        if (text.Length >= columns)
        {
            return text;
        }

        return new string(' ', (columns - text.Length) / 2) + text;
    }

    private string BuildThermalReceiptText(BreakfastCafeOrderDto order, IReadOnlyList<KitchenPreviewLine> lines, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ASMILE BO");
        sb.AppendLine(title);
        sb.AppendLine(order.OrderNumber);
        sb.AppendLine($"Ban: {order.TableName ?? "Mang di"}");
        sb.AppendLine($"Ngay: {order.OrderDate:dd/MM/yyyy HH:mm}");
        sb.AppendLine(new string('-', 32));
        foreach (var line in lines)
        {
            sb.AppendLine(line.MenuItemName);
            sb.AppendLine($"  {line.Quantity:N1} x {line.UnitPrice:N0} = {line.LineTotalAmount:N0}");
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine($"  Ghi chu: {line.Note}");
            }
        }
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Tam tinh: {order.SubtotalAmount:N0}");
        if (order.DiscountAmount > 0)
        {
            sb.AppendLine($"Giam gia: -{order.DiscountAmount:N0}");
        }
        if (order.VatAmount > 0)
        {
            sb.AppendLine($"VAT: {order.VatAmount:N0}");
        }
        if (order.ServiceChargeAmount > 0)
        {
            sb.AppendLine($"Phi DV: {order.ServiceChargeAmount:N0}");
        }
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"TONG: {order.TotalAmount:N0}");
        if (!string.IsNullOrWhiteSpace(ReceiptFooterText))
        {
            sb.AppendLine(ReceiptFooterText);
        }
        else
        {
            sb.AppendLine("Cam on quy khach!");
        }
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine();
        return sb.ToString();
    }

    private static async Task SendRawThermalPrintAsync(string host, int port, string text)
    {
        using var client = new TcpClient();
        using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.ConnectAsync(host, port, connectCts.Token);
        await using var stream = client.GetStream();
        var init = new byte[] { 0x1B, 0x40 };
        var cut = new byte[] { 0x1D, 0x56, 0x42, 0x00 };
        await stream.WriteAsync(init);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(text));
        await stream.WriteAsync(cut);
        await stream.FlushAsync();
    }

    private static void PrintFlowDocumentToWindowsPrinter(System.Windows.Documents.FlowDocument doc, string printerName, string jobName)
    {
        var server = new LocalPrintServer();
        var queue = server.GetPrintQueue(printerName.Trim());
        var dialog = new System.Windows.Controls.PrintDialog
        {
            PrintQueue = queue
        };
        dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, jobName);
    }

    private void ShowPrintPreview(string title, System.Windows.Documents.FlowDocument doc, string printButtonText, string printJobName, Func<Task<bool>>? beforePrintAsync = null, Func<Task>? directPrintAsync = null)
    {
        var viewer = new FlowDocumentPageViewer { Document = doc };
        var printBtn = new Button
        {
            Content = printButtonText,
            Height = 36,
            Padding = new System.Windows.Thickness(16, 0, 16, 0),
            Margin = new System.Windows.Thickness(0, 8, 0, 4),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = System.Windows.FontWeights.SemiBold,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF2, 0x99, 0x4A)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0),
            Cursor = Cursors.Hand
        };
        ToolTipService.SetToolTip(printBtn, "Mở hộp thoại in hệ thống");
        var isProcessing = false;
        printBtn.Click += async (_, _) =>
        {
            if (isProcessing)
            {
                return;
            }

            isProcessing = true;
            printBtn.IsEnabled = false;
            if (beforePrintAsync is not null)
            {
                var canPrint = await beforePrintAsync();
                if (!canPrint)
                {
                    isProcessing = false;
                    printBtn.IsEnabled = true;
                    return;
                }
            }

            if (directPrintAsync is not null)
            {
                try
                {
                    await directPrintAsync();
                    if (beforePrintAsync is not null)
                    {
                        printBtn.Content = "Đã gửi và in";
                    }
                    else
                    {
                        isProcessing = false;
                        printBtn.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Không in được phiếu: {ex.Message}";
                    System.Windows.MessageBox.Show(StatusText, "Lỗi in", MessageBoxButton.OK, MessageBoxImage.Warning);
                    isProcessing = false;
                    printBtn.IsEnabled = true;
                }

                return;
            }

            var dlg = new System.Windows.Controls.PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                doc.PageWidth = dlg.PrintableAreaWidth;
                doc.PageHeight = dlg.PrintableAreaHeight;
                dlg.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, printJobName);
                StatusText = $"Đã in {printJobName}.";
            }
            isProcessing = false;
            printBtn.IsEnabled = true;
        };

        var panel = new DockPanel();
        DockPanel.SetDock(printBtn, Dock.Top);
        panel.Children.Add(printBtn);
        panel.Children.Add(viewer);

        var win = new System.Windows.Window
        {
            Title = title,
            Width = 420,
            Height = 650,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow,
            Content = panel,
            WindowStyle = System.Windows.WindowStyle.ToolWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task PayOrderAsync(string? method)
    {
        if (CurrentOrder is null)
        {
            return;
        }

        var paymentMethod = string.IsNullOrWhiteSpace(method) ? SelectedPaymentMethod : method;
        if (!PaymentMethods.Contains(paymentMethod))
        {
            StatusText = "Phương thức thanh toán này đang bị tắt trong cài đặt.";
            return;
        }

        paymentMethod = ToPaymentCode(paymentMethod);

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.PayOrderAsync(new PayBreakfastCafeOrderRequest
            {
                OrderId = CurrentOrder.Id,
                PaymentMethod = paymentMethod,
                Amount = CurrentOrder.RemainingAmount,
                CreatedBy = "pos-ui"
            });

            StatusText = $"Đã thanh toán {CurrentOrder.OrderNumber}.";

            // Capture data before clearing
            var orderNumber = CurrentOrder.OrderNumber;
            var tableName = CurrentOrder.TableName ?? "Mang đi";
            var orderDate = CurrentOrder.OrderDate;
            var subtotal = CurrentOrder.SubtotalAmount;
            var discount = CurrentOrder.DiscountAmount;
            var vat = CurrentOrder.VatAmount;
            var service = CurrentOrder.ServiceChargeAmount;
            var total = CurrentOrder.TotalAmount;
            var paidOrder = CurrentOrder;
            var lines = SnapshotCurrentOrderPrintLines();
            var seller = _currentUserService.CurrentUser?.DisplayName ?? _currentUserService.CurrentUser?.UserName ?? "Thu ngân";

            await RefreshAllAsync();
            CurrentOrder = null;

            // Build receipt with full details
            var doc = new System.Windows.Documents.FlowDocument { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 11 };
            ApplyReceiptPrintLayout(doc, ReceiptPaperSize);
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("ASMILE — BÁN ĐỒ ĂN SÁNG & CÀ PHÊ")) { FontSize = 14, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(orderNumber)) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Bàn: {tableName}  |  {orderDate:dd/MM/yyyy HH:mm}")) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 2) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Người bán: {seller}")) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', 34))) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });

            foreach (var line in lines)
            {
                var p = new System.Windows.Documents.Paragraph { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) };
                p.Inlines.Add(new System.Windows.Documents.Run($"{line.MenuItemName}") { FontWeight = System.Windows.FontWeights.SemiBold });
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  {line.Quantity:N1} × {line.UnitPrice:N0}đ = {line.LineTotalAmount:N0}đ"));
                doc.Blocks.Add(p);
            }

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', 34))) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Tạm tính: {subtotal:N0}đ  |  Giảm giá: {discount:N0}đ")) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });
            if (vat > 0) doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"VAT: {vat:N0}đ  |  Phí DV: {service:N0}đ")) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', 34))) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"TỔNG: {total:N0}đ")) { FontSize = 18, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12)), Margin = new System.Windows.Thickness(0, 4, 0, 0) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', 34))) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Cảm ơn quý khách!")) { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 4, 0, 0) });

            var thermalText = BuildThermalReceiptText(paidOrder, lines, "HÓA ĐƠN THANH TOÁN");
            if (AutoPrintReceiptAfterPayment)
            {
                try
                {
                    await PrintReceiptSnapshotAsync(doc, orderNumber, thermalText);
                }
                catch (Exception ex)
                {
                    StatusText = $"Đã thanh toán nhưng chưa in được hóa đơn: {ex.Message}";
                    System.Windows.MessageBox.Show(StatusText, "Lỗi in hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            var viewer = new FlowDocumentPageViewer { Document = doc };
            var printBtn = new Button { Content = "🖨️  In hóa đơn", Height = 34, Padding = new System.Windows.Thickness(16, 0, 16, 0), Margin = new System.Windows.Thickness(0, 6, 0, 2), HorizontalAlignment = System.Windows.HorizontalAlignment.Center, FontSize = 13, FontWeight = System.Windows.FontWeights.SemiBold, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF2, 0x99, 0x4A)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new System.Windows.Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            printBtn.Click += async (_, _) =>
            {
                try
                {
                    await PrintReceiptSnapshotAsync(doc, orderNumber, thermalText);
                }
                catch (Exception ex)
                {
                    StatusText = $"Không in được hóa đơn: {ex.Message}";
                    System.Windows.MessageBox.Show(StatusText, "Lỗi in hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            var panel = new DockPanel(); DockPanel.SetDock(printBtn, Dock.Top); panel.Children.Add(printBtn); panel.Children.Add(viewer);
            var win = new System.Windows.Window { Title = $"Hóa đơn — {orderNumber}", Width = 420, Height = 600, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, Owner = System.Windows.Application.Current.MainWindow, Content = panel, WindowStyle = System.Windows.WindowStyle.ToolWindow };
            win.ShowDialog();
        });
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (CurrentOrder is null)
        {
            return;
        }

        if (!CashierCanCancelOrder)
        {
            StatusText = "Cấu hình hiện không cho phép thu ngân hủy đơn.";
            return;
        }

        var result = MessageBox.Show(
            $"Hủy đơn {CurrentOrder.OrderNumber}?",
            "Xác nhận hủy đơn",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.CancelOrderAsync(CurrentOrder.Id, "Hủy từ màn hình POS");
            StatusText = $"Đã hủy {CurrentOrder.OrderNumber}.";
            await RefreshAllAsync();
        });
    }

    [RelayCommand]
    private async Task OpenOrderAsync(BreakfastCafeOrderDto? order)
    {
        if (order is null)
        {
            return;
        }

        if (IsMergingOrder)
        {
            await MergeOpenOrderAsync(order);
            return;
        }

        await RunBusyAsync(async () =>
        {
            CurrentOrder = await _salesService.GetOrderDetailAsync(order.Id);
            SelectedTable = _allTables.FirstOrDefault(x => x.Id == CurrentOrder?.PosTableId);
            if (SelectedTable is not null && SelectedArea != SelectedTable.AreaName)
            {
                SelectedArea = SelectedTable.AreaName;
            }

            LeftTabIndex = 1;
            StatusText = $"Đã mở lại {order.OrderNumber}.";
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    // ================================================================
    // SETTINGS COMMANDS
    // ================================================================

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        if (_isLoadingSettings)
        {
            _reloadSettingsAfterCurrent = true;
            return;
        }

        _isLoadingSettings = true;
        try
        {
            do
            {
                _reloadSettingsAfterCurrent = false;
                await WaitUntilNotBusyAsync();
                await LoadSettingsCoreAsync();
            }
            while (_reloadSettingsAfterCurrent);
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private async Task LoadSettingsCoreAsync()
    {
        var tables = await _salesService.GetTablesAsync();
        ResetWith(SettingTables, tables.OrderBy(x => x.AreaName).ThenBy(x => x.TableCode));
        ResetWith(SettingAreas, tables.Select(x => x.AreaName).Distinct().OrderBy(x => x));
        ResetWith(SettingAreaRows, tables
            .GroupBy(x => new { x.AreaCode, x.AreaName })
            .OrderBy(x => x.Key.AreaName)
            .Select(x => new PosAreaSettingRow(
                x.Key.AreaCode,
                x.Key.AreaName,
                x.Count(),
                x.Count(t => t.Status != "Empty"),
                x.Min(t => t.SortOrder))));

        var categories = await _menuCatalogService.GetCategoriesAsync();
        ResetWith(SettingCategories, categories.OrderBy(x => x.SortOrder));
        SyncBreakfastCafeCategorySelections();

        var menuItems = await _salesService.GetMenuAsync();
        ResetWith(SettingMenuItems, menuItems.OrderBy(x => x.CategoryName).ThenBy(x => x.Name));
        ApplySettingMenuItemFilter();
        OnPropertyChanged(nameof(SettingsTotalTables));
        OnPropertyChanged(nameof(SettingsTotalAreas));
        OnPropertyChanged(nameof(SettingsTotalCategories));
        OnPropertyChanged(nameof(SettingsTotalMenuItems));
    }

    [RelayCommand]
    private void NavigateSettingsSubTab(string idxStr)
    {
        if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx <= 12)
            SettingsSubTabIndex = idx;
    }

    [RelayCommand]
    private async Task SaveSalesPolicySettingsAsync()
    {
        if (DefaultServiceChargeRate < 0m)
        {
            DefaultServiceChargeRate = 0m;
        }

        if (MaxOrderDiscountPercent < 0m)
        {
            MaxOrderDiscountPercent = 0m;
        }
        else if (MaxOrderDiscountPercent > 100m)
        {
            MaxOrderDiscountPercent = 100m;
        }

        if (MaxOrderDiscountAmount < 0m)
        {
            MaxOrderDiscountAmount = 0m;
        }

        try
        {
            var state = new BreakfastCafeSalesPolicyState
            {
                EnableDineInServiceCharge = EnableDineInServiceCharge,
                EnableTakeAwayServiceCharge = EnableTakeAwayServiceCharge,
                EnableRoomServiceServiceCharge = EnableRoomServiceServiceCharge,
                DefaultServiceChargeRate = DefaultServiceChargeRate,
                ApplyServiceChargeAfterDiscount = ApplyServiceChargeAfterDiscount,
                IncludeServiceChargeInVatBase = IncludeServiceChargeInVatBase,
                ShowServiceChargeLineOnReceipt = ShowServiceChargeLineOnReceipt,
                AllowOrderDiscount = AllowOrderDiscount,
                AllowLineDiscount = AllowLineDiscount,
                MaxOrderDiscountPercent = MaxOrderDiscountPercent,
                MaxOrderDiscountAmount = MaxOrderDiscountAmount,
                RequireManagerApprovalForDiscount = RequireManagerApprovalForDiscount,
                RequireDiscountReason = RequireDiscountReason,
                EnableVoucherDiscount = EnableVoucherDiscount,
                DefaultDiscountReason = DefaultDiscountReason,
                EnableCashPayment = EnableCashPayment,
                EnableBankTransferPayment = EnableBankTransferPayment,
                EnableCardPosPayment = EnableCardPosPayment,
                EnableRoomChargePayment = EnableRoomChargePayment,
                EnableCustomerDebtPayment = EnableCustomerDebtPayment,
                AllowSplitPayment = AllowSplitPayment,
                RequirePaymentReferenceForBank = RequirePaymentReferenceForBank,
                DefaultQrBankAccount = DefaultQrBankAccount,
                DefaultCashAccountCode = DefaultCashAccountCode,
                DefaultBankAccountCode = DefaultBankAccountCode,
                AutoPrintKitchenTicket = AutoPrintKitchenTicket,
                PrintKitchenTicketOnEdit = PrintKitchenTicketOnEdit,
                AllowKitchenReprint = AllowKitchenReprint,
                RequireKitchenReprintReason = RequireKitchenReprintReason,
                KitchenPrinterName = KitchenPrinterName,
                BarPrinterName = BarPrinterName,
                KitchenPaperSize = KitchenPaperSize,
                BarPaperSize = BarPaperSize,
                DefaultPreparationStation = DefaultPreparationStation,
                RequireOpenShiftBeforeSale = RequireOpenShiftBeforeSale,
                RequireCashCountOnCloseShift = RequireCashCountOnCloseShift,
                LockSalesAfterShiftClose = LockSalesAfterShiftClose,
                OpeningCashAmount = OpeningCashAmount,
                MaxShiftCashVariance = MaxShiftCashVariance,
                CurrentShiftName = CurrentShiftName,
                ShiftSettings = ShiftSettings.Select(x => new ShiftSettingState
                {
                    Code = x.Code,
                    Name = x.Name,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    IsActive = x.IsActive
                }).ToList(),
                AutoPrintReceiptAfterPayment = AutoPrintReceiptAfterPayment,
                AllowProvisionalPrint = AllowProvisionalPrint,
                ReceiptPrinterName = ReceiptPrinterName,
                ReceiptPaperSize = ReceiptPaperSize,
                ReceiptCopyCount = ReceiptCopyCount,
                ReceiptFooterText = ReceiptFooterText,
                PrintStoreLogo = PrintStoreLogo,
                CashierCanCancelItem = CashierCanCancelItem,
                CashierCanCancelOrder = CashierCanCancelOrder,
                CashierCanEditPrice = CashierCanEditPrice,
                CashierCanViewReports = CashierCanViewReports,
                RequireManagerPinForSensitiveActions = RequireManagerPinForSensitiveActions,
                CashierMaxDiscountPercent = CashierMaxDiscountPercent,
                CheckInventoryBeforeSale = CheckInventoryBeforeSale,
                BlockSaleWhenOutOfStock = BlockSaleWhenOutOfStock,
                WarnMenuWithoutRecipe = WarnMenuWithoutRecipe,
                ConsumeInventoryOnPayment = ConsumeInventoryOnPayment,
                ConsumeInventoryOnKitchenSend = ConsumeInventoryOnKitchenSend,
                DefaultSaleWarehouseCode = DefaultSaleWarehouseCode,
                EnableSettingsAuditLog = EnableSettingsAuditLog,
                RequireConfirmBeforeReset = RequireConfirmBeforeReset,
                AllowExportSettings = AllowExportSettings,
                AllowImportSettings = AllowImportSettings,
                AuditRetentionDays = AuditRetentionDays,
                SettingsBackupFolder = SettingsBackupFolder,
                AllowedBreakfastCafeCategoryIds = BreakfastCafeCategorySelections
                    .Where(x => x.IsEnabled)
                    .Select(x => x.CategoryId)
                    .ToList()
            };

            var path = GetSalesPolicySettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(state, SalesPolicyJsonOptions));
            UpdatePaymentMethodsFromSettings();
            StatusText = "Đã lưu cấu hình phí phục vụ và giảm giá.";
        }
        catch (Exception ex)
        {
            StatusText = $"Không lưu được cấu hình: {ex.Message}";
        }
    }

    private void LoadSalesPolicySettings()
    {
        try
        {
            var path = GetSalesPolicySettingsPath();
            if (!File.Exists(path))
            {
                var legacyPath = GetLegacySalesPolicySettingsPath();
                if (File.Exists(legacyPath)) path = legacyPath;
            }
            if (!File.Exists(path))
            {
                return;
            }

            var state = JsonSerializer.Deserialize<BreakfastCafeSalesPolicyState>(File.ReadAllText(path), SalesPolicyJsonOptions);
            if (state is null)
            {
                return;
            }

            EnableDineInServiceCharge = state.EnableDineInServiceCharge;
            EnableTakeAwayServiceCharge = state.EnableTakeAwayServiceCharge;
            EnableRoomServiceServiceCharge = state.EnableRoomServiceServiceCharge;
            DefaultServiceChargeRate = state.DefaultServiceChargeRate;
            ApplyServiceChargeAfterDiscount = state.ApplyServiceChargeAfterDiscount;
            IncludeServiceChargeInVatBase = state.IncludeServiceChargeInVatBase;
            ShowServiceChargeLineOnReceipt = state.ShowServiceChargeLineOnReceipt;
            AllowOrderDiscount = state.AllowOrderDiscount;
            AllowLineDiscount = state.AllowLineDiscount;
            MaxOrderDiscountPercent = state.MaxOrderDiscountPercent;
            MaxOrderDiscountAmount = state.MaxOrderDiscountAmount;
            RequireManagerApprovalForDiscount = state.RequireManagerApprovalForDiscount;
            RequireDiscountReason = state.RequireDiscountReason;
            EnableVoucherDiscount = state.EnableVoucherDiscount;
            DefaultDiscountReason = string.IsNullOrWhiteSpace(state.DefaultDiscountReason) ? "Khuyến mại" : state.DefaultDiscountReason;
            EnableCashPayment = state.EnableCashPayment;
            EnableBankTransferPayment = state.EnableBankTransferPayment;
            EnableCardPosPayment = state.EnableCardPosPayment;
            EnableRoomChargePayment = state.EnableRoomChargePayment;
            EnableCustomerDebtPayment = state.EnableCustomerDebtPayment;
            AllowSplitPayment = state.AllowSplitPayment;
            RequirePaymentReferenceForBank = state.RequirePaymentReferenceForBank;
            DefaultQrBankAccount = state.DefaultQrBankAccount;
            DefaultCashAccountCode = string.IsNullOrWhiteSpace(state.DefaultCashAccountCode) ? "111" : state.DefaultCashAccountCode;
            DefaultBankAccountCode = string.IsNullOrWhiteSpace(state.DefaultBankAccountCode) ? "112" : state.DefaultBankAccountCode;
            AutoPrintKitchenTicket = state.AutoPrintKitchenTicket;
            PrintKitchenTicketOnEdit = state.PrintKitchenTicketOnEdit;
            AllowKitchenReprint = state.AllowKitchenReprint;
            RequireKitchenReprintReason = state.RequireKitchenReprintReason;
            KitchenPrinterName = string.IsNullOrWhiteSpace(state.KitchenPrinterName) ? "Kitchen Printer" : state.KitchenPrinterName;
            BarPrinterName = string.IsNullOrWhiteSpace(state.BarPrinterName) ? "Bar Printer" : state.BarPrinterName;
            KitchenPaperSize = string.IsNullOrWhiteSpace(state.KitchenPaperSize) ? "80mm" : state.KitchenPaperSize;
            BarPaperSize = string.IsNullOrWhiteSpace(state.BarPaperSize) ? "80mm" : state.BarPaperSize;
            DefaultPreparationStation = string.IsNullOrWhiteSpace(state.DefaultPreparationStation) ? "Kitchen" : state.DefaultPreparationStation;
            RequireOpenShiftBeforeSale = state.RequireOpenShiftBeforeSale;
            RequireCashCountOnCloseShift = state.RequireCashCountOnCloseShift;
            LockSalesAfterShiftClose = state.LockSalesAfterShiftClose;
            OpeningCashAmount = state.OpeningCashAmount;
            MaxShiftCashVariance = state.MaxShiftCashVariance;
            CurrentShiftName = string.IsNullOrWhiteSpace(state.CurrentShiftName) ? "Ca sáng" : state.CurrentShiftName;
            ShiftSettings.Clear();
            foreach (var shift in state.ShiftSettings ?? [])
            {
                if (string.IsNullOrWhiteSpace(shift.Code) || string.IsNullOrWhiteSpace(shift.Name))
                {
                    continue;
                }

                ShiftSettings.Add(new ShiftSettingRow(
                    shift.Code.Trim(),
                    shift.Name.Trim(),
                    string.IsNullOrWhiteSpace(shift.StartTime) ? "06:00" : shift.StartTime.Trim(),
                    string.IsNullOrWhiteSpace(shift.EndTime) ? "14:00" : shift.EndTime.Trim(),
                    shift.IsActive));
            }
            EnsureDefaultShiftSettings();
            AutoPrintReceiptAfterPayment = state.AutoPrintReceiptAfterPayment;
            AllowProvisionalPrint = state.AllowProvisionalPrint;
            ReceiptPrinterName = string.IsNullOrWhiteSpace(state.ReceiptPrinterName) ? "Receipt Printer" : state.ReceiptPrinterName;
            ReceiptPaperSize = string.IsNullOrWhiteSpace(state.ReceiptPaperSize) ? "80mm" : state.ReceiptPaperSize;
            ReceiptCopyCount = state.ReceiptCopyCount <= 0 ? 1 : state.ReceiptCopyCount;
            ReceiptFooterText = string.IsNullOrWhiteSpace(state.ReceiptFooterText) ? "Cảm ơn quý khách!" : state.ReceiptFooterText;
            PrintStoreLogo = state.PrintStoreLogo;
            CashierCanCancelItem = state.CashierCanCancelItem;
            CashierCanCancelOrder = state.CashierCanCancelOrder;
            CashierCanEditPrice = state.CashierCanEditPrice;
            CashierCanViewReports = state.CashierCanViewReports;
            RequireManagerPinForSensitiveActions = state.RequireManagerPinForSensitiveActions;
            CashierMaxDiscountPercent = state.CashierMaxDiscountPercent;
            CheckInventoryBeforeSale = state.CheckInventoryBeforeSale;
            BlockSaleWhenOutOfStock = state.BlockSaleWhenOutOfStock;
            WarnMenuWithoutRecipe = state.WarnMenuWithoutRecipe;
            ConsumeInventoryOnPayment = state.ConsumeInventoryOnPayment;
            ConsumeInventoryOnKitchenSend = state.ConsumeInventoryOnKitchenSend;
            DefaultSaleWarehouseCode = string.IsNullOrWhiteSpace(state.DefaultSaleWarehouseCode) ? "KHO-001" : state.DefaultSaleWarehouseCode;
            EnableSettingsAuditLog = state.EnableSettingsAuditLog;
            RequireConfirmBeforeReset = state.RequireConfirmBeforeReset;
            AllowExportSettings = state.AllowExportSettings;
            AllowImportSettings = state.AllowImportSettings;
            AuditRetentionDays = state.AuditRetentionDays <= 0 ? 365 : state.AuditRetentionDays;
            SettingsBackupFolder = string.IsNullOrWhiteSpace(state.SettingsBackupFolder) ? "Data\\Settings" : state.SettingsBackupFolder;
            _allowedBreakfastCafeCategoryIds.Clear();
            foreach (var categoryId in state.AllowedBreakfastCafeCategoryIds ?? [])
            {
                _allowedBreakfastCafeCategoryIds.Add(categoryId);
            }

            SyncBreakfastCafeCategorySelections();
            UpdatePaymentMethodsFromSettings();
            RefreshMenuCategories();
            ApplyMenuFilter();
        }
        catch
        {
            // Keep defaults when the local settings file is missing or invalid.
        }
    }

    private static string GetSalesPolicySettingsPath()
        => ApplicationDataPaths.GetUserSettingsFile("breakfast-cafe-sales-policy.json");

    private static string GetLegacySalesPolicySettingsPath()
        => Path.Combine(AppContext.BaseDirectory, "Data", "Settings", "breakfast-cafe-sales-policy.json");

    private void UpdatePaymentMethodsFromSettings()
    {
        var selected = SelectedPaymentMethod;
        var methods = new List<string>();
        if (EnableCashPayment) methods.Add("Tiền mặt");
        if (EnableBankTransferPayment) methods.Add("Chuyển khoản");
        if (EnableCardPosPayment) methods.Add("POS/Thẻ");
        if (EnableRoomChargePayment) methods.Add("Ghi phòng");
        if (EnableCustomerDebtPayment) methods.Add("Công nợ");

        ResetWith(PaymentMethods, methods);
        if (PaymentMethods.Count > 0 && !PaymentMethods.Contains(selected))
        {
            SelectedPaymentMethod = PaymentMethods[0];
        }
    }

    private void SyncBreakfastCafeCategorySelections()
    {
        var rows = SettingCategories
            .Select(x => new CategorySaleSelectionRow(
                x.Id,
                x.Code,
                x.Name,
                _allowedBreakfastCafeCategoryIds.Count == 0 || _allowedBreakfastCafeCategoryIds.Contains(x.Id)))
            .ToList();
        ResetWith(BreakfastCafeCategorySelections, rows);
    }

    private void RefreshMenuCategories()
    {
        var allowedIds = BreakfastCafeCategorySelections
            .Where(x => x.IsEnabled)
            .Select(x => x.CategoryId)
            .ToHashSet();

        IEnumerable<MenuCategoryDto> categories = SettingCategories;
        if (allowedIds.Count > 0)
        {
            categories = categories.Where(x => allowedIds.Contains(x.Id));
        }

        ResetWith(Categories, new[] { new PosCategoryFilter(null, "Tất cả", 0) }
            .Concat(categories.OrderBy(x => x.SortOrder).Select(x => new PosCategoryFilter(x.Id, x.Name, x.SortOrder))));

        if (SelectedCategory?.CategoryId is Guid selectedId && !Categories.Any(x => x.CategoryId == selectedId))
        {
            SelectedCategory = Categories.FirstOrDefault();
        }
        else if (SelectedCategory is null)
        {
            SelectedCategory = Categories.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task SaveAreaSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAreaName)) return;

        await RunBusyAsync(async () =>
        {
            var areaCode = string.IsNullOrWhiteSpace(NewAreaCode)
                ? NormalizeCode(NewAreaName)
                : NewAreaCode.Trim();
            var areaName = NewAreaName.Trim();

            if (SelectedSettingArea is null)
            {
                var tableCode = BuildDefaultTableCode(areaCode);
                await _salesService.SaveTableAsync(new PosTableDto
                {
                    Id = Guid.Empty,
                    AreaCode = areaCode,
                    AreaName = areaName,
                    TableCode = tableCode,
                    TableName = $"{areaName} 01",
                    OrderType = "DineIn",
                    SeatCount = 4,
                    SortOrder = 999
                });
                StatusText = $"Đã thêm khu vực {areaName}.";
            }
            else
            {
                var rows = SettingTables
                    .Where(x => string.Equals(x.AreaName, SelectedSettingArea.AreaName, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(x.AreaCode, SelectedSettingArea.AreaCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var table in rows)
                {
                    table.AreaCode = areaCode;
                    table.AreaName = areaName;
                    await _salesService.SaveTableAsync(table);
                }
                StatusText = $"Đã sửa khu vực {areaName}.";
            }

            ClearAreaForm();
            await RefreshOpenDataAsync();
            await LoadSettingsCoreAsync();
        });
    }

    [RelayCommand]
    private void EditAreaSettings(PosAreaSettingRow? area)
    {
        if (area is null) return;
        SelectedSettingArea = area;
        NewAreaCode = area.AreaCode;
        NewAreaName = area.AreaName;
    }

    [RelayCommand]
    private async Task DeleteAreaSettingsAsync(PosAreaSettingRow? area)
    {
        if (area is null) return;
        var result = MessageBox.Show($"Xóa khu vực {area.AreaName} và toàn bộ bàn thuộc khu vực này?", "Xác nhận",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await RunBusyAsync(async () =>
        {
            var rows = SettingTables
                .Where(x => string.Equals(x.AreaName, area.AreaName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(x.AreaCode, area.AreaCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var table in rows)
            {
                await _salesService.DeleteTableAsync(table.Id);
            }

            StatusText = $"Đã xóa khu vực {area.AreaName}.";
            ClearAreaForm();
            await RefreshOpenDataAsync();
            await LoadSettingsCoreAsync();
        });
    }

    [RelayCommand]
    private void ClearAreaForm()
    {
        SelectedSettingArea = null;
        NewAreaCode = string.Empty;
        NewAreaName = string.Empty;
    }

    [RelayCommand]
    private async Task SaveTableSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTableCode)) return;

        await RunBusyAsync(async () =>
        {
            var dto = new PosTableDto
            {
                Id = SelectedSettingTable?.Id ?? Guid.Empty,
                AreaCode = string.IsNullOrWhiteSpace(NewTableAreaCode) ? NewTableAreaName : NewTableAreaCode,
                AreaName = string.IsNullOrWhiteSpace(NewTableAreaName) ? NewTableAreaCode : NewTableAreaName,
                TableCode = NewTableCode.Trim(),
                TableName = string.IsNullOrWhiteSpace(NewTableName) ? NewTableCode.Trim() : NewTableName.Trim(),
                SeatCount = NewTableSeats,
                OrderType = NewTableOrderType.Replace(" (Tại bàn)", "").Replace(" (Mang đi)", "").Replace(" (Phòng)", "").Split(' ')[0].Trim(),
                SortOrder = SelectedSettingTable?.SortOrder ?? 999
            };
            await _salesService.SaveTableAsync(dto);
            StatusText = SelectedSettingTable is null ? $"Đã thêm bàn {dto.TableName}." : $"Đã sửa bàn {dto.TableName}.";
            ClearTableForm();
            await RefreshOpenDataAsync();
            await LoadSettingsCoreAsync();
        });
    }

    [RelayCommand]
    private async Task DeleteTableSettingsAsync(PosTableDto? table)
    {
        if (table is null) return;

        var result = MessageBox.Show($"Xóa bàn {table.TableName}?", "Xác nhận",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await RunBusyAsync(async () =>
        {
            await _salesService.DeleteTableAsync(table.Id);
            StatusText = $"Đã xóa bàn {table.TableName}.";
            ClearTableForm();
            await RefreshOpenDataAsync();
            await LoadSettingsCoreAsync();
        });
    }

    [RelayCommand]
    private void EditTableSettings(PosTableDto? table)
    {
        if (table is null) return;
        SelectedSettingTable = table;
        NewTableAreaName = table.AreaName;
        NewTableAreaCode = table.AreaCode;
        NewTableCode = table.TableCode;
        NewTableName = table.TableName;
        NewTableSeats = table.SeatCount;
        NewTableOrderType = table.OrderType switch
        {
            "TakeAway" => "TakeAway (Mang đi)",
            "RoomService" => "RoomService (Phòng)",
            _ => "DineIn (Tại bàn)"
        };
    }

    [RelayCommand]
    private void ClearTableForm()
    {
        SelectedSettingTable = null;
        NewTableAreaName = string.Empty;
        NewTableAreaCode = string.Empty;
        NewTableCode = string.Empty;
        NewTableName = string.Empty;
        NewTableSeats = 4;
        NewTableOrderType = "DineIn (Tại bàn)";
    }

    [RelayCommand]
    private async Task SaveCategorySettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

        await RunBusyAsync(async () =>
        {
            var existing = SelectedSettingCategory;
            await _menuCatalogService.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
            {
                Id = existing?.Id,
                Code = string.IsNullOrWhiteSpace(NewCategoryCode) ? NewCategoryName.Trim() : NewCategoryCode.Trim(),
                Name = NewCategoryName.Trim(),
                SortOrder = existing?.SortOrder ?? 99,
                IsActive = true
            });
            StatusText = existing is null ? $"Đã thêm nhóm {NewCategoryName}." : $"Đã sửa nhóm {NewCategoryName}.";
            ClearCategoryForm();
            await LoadSettingsCoreAsync();
            await InitializeAsync(); // Reload menu data for category chips
        });
    }

    [RelayCommand]
    private void EditCategorySettings(MenuCategoryDto? cat)
    {
        if (cat is null) return;
        SelectedSettingCategory = cat;
        NewCategoryCode = cat.Code;
        NewCategoryName = cat.Name;
    }

    [RelayCommand]
    private void ClearCategoryForm()
    {
        SelectedSettingCategory = null;
        NewCategoryCode = string.Empty;
        NewCategoryName = string.Empty;
    }

    // Settings: Menu Item management
    [RelayCommand]
    private void EditMenuItemSettings(MenuItemDto? item)
    {
        if (item is null) return;
        SelectedSettingMenuItem = item;
        EditMenuItemName = item.Name;
        EditMenuItemPrice = item.UnitPrice;
        EditMenuItemActive = item.IsActive;
        EditMenuItemClassification = ResolveSettingMenuItemClassification(item);
    }

    [RelayCommand]
    private async Task SaveMenuItemSettingsAsync()
    {
        if (SelectedSettingMenuItem is null || string.IsNullOrWhiteSpace(EditMenuItemName)) return;

        await RunBusyAsync(async () =>
        {
            var item = SelectedSettingMenuItem;
            await _menuCatalogService.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
            {
                Id = item.Id,
                MenuCategoryId = item.MenuCategoryId,
                Code = item.Code,
                Name = EditMenuItemName.Trim(),
                UnitName = item.UnitName,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                IsVatIncluded = item.IsVatIncluded,
                HasRecipeManagement = item.HasRecipeManagement,
                IsActive = EditMenuItemActive,
                Note = item.Note,
                PreparationArea = MapClassificationToPreparationArea(EditMenuItemClassification),
                ImagePath = item.ImagePath
            });
            StatusText = $"Đã sửa món {EditMenuItemName}.";
            ClearMenuItemForm();
            await LoadSettingsCoreAsync();
            await InitializeAsync(); // Reload menu
        });
    }

    [RelayCommand]
    private async Task ToggleMenuItemActiveAsync(MenuItemDto? item)
    {
        if (item is null) return;
        await RunBusyAsync(async () =>
        {
            await _menuCatalogService.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
            {
                Id = item.Id,
                MenuCategoryId = item.MenuCategoryId,
                Code = item.Code,
                Name = item.Name,
                UnitName = item.UnitName,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                IsVatIncluded = item.IsVatIncluded,
                HasRecipeManagement = item.HasRecipeManagement,
                IsActive = !item.IsActive,
                Note = item.Note,
                PreparationArea = item.PreparationArea,
                ImagePath = item.ImagePath
            });
            StatusText = item.IsActive ? $"Đã ngừng bán {item.Name}." : $"Đã mở bán {item.Name}.";
            await LoadSettingsCoreAsync();
            await InitializeAsync();
        });
    }

    [RelayCommand]
    private void ClearMenuItemForm()
    {
        SelectedSettingMenuItem = null;
        EditMenuItemName = string.Empty;
        EditMenuItemPrice = 0;
        EditMenuItemActive = true;
        EditMenuItemClassification = "Đồ ăn";
    }

    [RelayCommand]
    private void SelectMenuItemImage()
    {
        var dlg = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp", Title = "Chọn ảnh món" };
        if (dlg.ShowDialog() == true)
        {
            EditMenuItemImagePath = dlg.FileName;
            StatusText = $"Đã chọn ảnh: {System.IO.Path.GetFileName(dlg.FileName)}";
        }
    }

    [ObservableProperty] private string? editMenuItemImagePath;

    // ================================================================

    private static string NormalizeCode(string value)
    {
        var text = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());
        while (text.Contains("--", StringComparison.Ordinal))
        {
            text = text.Replace("--", "-", StringComparison.Ordinal);
        }
        return text.Trim('-');
    }

    private string BuildDefaultTableCode(string areaCode)
    {
        var prefix = string.IsNullOrWhiteSpace(areaCode) ? "AREA" : areaCode.Trim().ToUpperInvariant();
        var baseCode = $"{prefix}-01";
        if (SettingTables.All(x => !string.Equals(x.TableCode, baseCode, StringComparison.OrdinalIgnoreCase)))
        {
            return baseCode;
        }

        for (var i = 2; i < 100; i++)
        {
            var code = $"{prefix}-{i:00}";
            if (SettingTables.All(x => !string.Equals(x.TableCode, code, StringComparison.OrdinalIgnoreCase)))
            {
                return code;
            }
        }

        return $"{prefix}-{DateTime.Now:HHmmss}";
    }

    private async Task InitializeAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _menuCatalogService.EnsureDefaultsAsync();

            _allMenuItems.Clear();
            var menu = await _salesService.GetMenuAsync();
            _allMenuItems.AddRange(menu);

            var categories = await _menuCatalogService.GetCategoriesAsync();
            ResetWith(SettingCategories, categories.OrderBy(x => x.SortOrder));
            SyncBreakfastCafeCategorySelections();
            RefreshMenuCategories();
            SelectedCategory ??= Categories.FirstOrDefault();

            await RefreshAllAsync();
            SelectedArea = Areas.FirstOrDefault() ?? string.Empty;
            ApplyMenuFilter();

            if (CurrentOrder is null && _allTables.Count > 0)
            {
                SelectedTable = _allTables.FirstOrDefault(x => x.Status != "Empty") ?? _allTables.First();
            }

            StatusText = $"Đã tải POS Bữa sáng & Cà phê ({_allMenuItems.Count} món, {Categories.Count} nhóm).";
        });
    }

    private async Task RefreshAllAsync()
    {
        await RefreshMenuAsync();
        await RefreshOpenDataAsync();
        ResetWith(TodayOrders, await _salesService.GetTodayOrdersAsync());
        Summary = await _salesService.GetSalesSummaryAsync();
    }

    private async Task RefreshMenuAsync()
    {
        var menu = await _salesService.GetMenuAsync();
        _allMenuItems.Clear();
        _allMenuItems.AddRange(menu);
        ApplyMenuFilter();
    }

    private async Task RefreshOpenDataAsync()
    {
        _allTables.Clear();
        _allTables.AddRange(await _salesService.GetTablesAsync());
        ResetWith(Areas, _allTables.Select(x => x.AreaName).Distinct().OrderBy(x => x));
        if (Areas.Count == 0)
        {
            SelectedArea = string.Empty;
        }
        else if (string.IsNullOrWhiteSpace(SelectedArea) || !Areas.Contains(SelectedArea))
        {
            SelectedArea = Areas.FirstOrDefault() ?? string.Empty;
        }

        ApplyTableFilter();
        ResetWith(OpenOrders, SortOpenOrders(await _salesService.GetOpenOrdersAsync()));
        OnPropertyChanged(nameof(HasOpenOrders));
        OnPropertyChanged(nameof(HasNoOpenOrders));
        BuildCurrentOrderTabs();
    }

    private void BuildCurrentOrderTabs()
    {
        var current = CurrentOrder;
        CurrentOrderTabs.Clear();

        if (current is null)
        {
            NotifyOrderTabState();
            return;
        }

        var rows = OpenOrders
            .Where(x => IsSameOrderSeat(x, current))
            .OrderBy(x => x.OrderDate)
            .ToList();

        if (rows.All(x => x.Id != current.Id))
        {
            rows.Add(current);
            rows = rows.OrderBy(x => x.OrderDate).ToList();
        }

        var index = 1;
        foreach (var order in rows)
        {
            CurrentOrderTabs.Add(new PosOrderTabRow(
                order.Id,
                ResolveOrderTabName(order, index),
                order.OrderNumber,
                order.TotalAmount,
                order.Id == current.Id));
            index++;
        }

        NotifyOrderTabState();
    }

    private void NotifyOrderTabState()
    {
        OnPropertyChanged(nameof(HasOrderTabs));
        OnPropertyChanged(nameof(HasMultipleOrderTabs));
    }

    private static bool IsSameOrderSeat(BreakfastCafeOrderDto order, BreakfastCafeOrderDto current)
    {
        if (current.PosTableId.HasValue)
        {
            return order.PosTableId == current.PosTableId;
        }

        return string.Equals(order.TableCode, current.TableCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(order.TableName, current.TableName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveOrderTabName(BreakfastCafeOrderDto? order, int index)
    {
        if (!string.IsNullOrWhiteSpace(order?.Note))
        {
            return order.Note.Trim();
        }

        return index <= 1 ? "Đơn chính" : $"Khách {index}";
    }

    [RelayCommand]
    private async Task LoadClosedOrdersAsync()
    {
        try
        {
            var from = ClosedOrderFromDate ?? DateTime.Today;
            var to = ClosedOrderToDate ?? DateTime.Today;
            var all = await _salesService.GetOrdersByDateRangeAsync(from, to);
            var query = all.Where(x => x.Status is "Paid" or "Cancelled").AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ClosedOrderStatusFilter))
                query = query.Where(x => x.Status == ClosedOrderStatusFilter);

            if (ClosedOrderFromDate.HasValue)
                query = query.Where(x => x.OrderDate.Date >= ClosedOrderFromDate.Value.Date);
            if (ClosedOrderToDate.HasValue)
                query = query.Where(x => x.OrderDate.Date <= ClosedOrderToDate.Value.Date);

            // Shift filter — approximate by hour
            query = ClosedOrderShiftFilter switch
            {
                "Morning" => query.Where(x => x.OrderDate.Hour >= 6 && x.OrderDate.Hour < 12),
                "Afternoon" => query.Where(x => x.OrderDate.Hour >= 12 && x.OrderDate.Hour < 18),
                "Evening" => query.Where(x => x.OrderDate.Hour >= 18 || x.OrderDate.Hour < 6),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(ClosedOrderSearchText))
            {
                var term = ClosedOrderSearchText.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.OrderNumber.ToLowerInvariant().Contains(term) ||
                    (x.TableName ?? "").ToLowerInvariant().Contains(term));
            }

            var ordered = ClosedOrderSortOption switch
            {
                "Oldest" => query.OrderBy(x => x.OrderDate).ToList(),
                "Highest" => query.OrderByDescending(x => x.TotalAmount).ToList(),
                "Lowest" => query.OrderBy(x => x.TotalAmount).ToList(),
                _ => query.OrderByDescending(x => x.OrderDate).ToList()
            };

            ResetWith(ClosedOrders, ordered);
            NotifyClosedOrderKpis();
            OnPropertyChanged(nameof(HasClosedOrders));
            StatusText = $"Đã tải {ClosedOrders.Count} đơn đóng ({ClosedOrders.Count} đơn).";
            // Auto-select first order and load its detail
            if (ClosedOrders.Count > 0)
            {
                await SelectClosedOrderAsync(ClosedOrders[0]);
            }
            else
            {
                SelectedClosedOrder = null;
                ClosedOrderDetailLines.Clear();
                ClosedOrderTimeline.Clear();
                OnPropertyChanged(nameof(HasSelectedClosedOrder));
                OnPropertyChanged(nameof(HasNoSelectedClosedOrder));
            }
        }
        catch (Exception ex) { StatusText = ex.Message; }
    }

    partial void OnClosedOrderStatusFilterChanged(string value) => _ = LoadClosedOrdersAsync();
    partial void OnClosedOrderFromDateChanged(DateTime? value) => _ = LoadClosedOrdersAsync();
    partial void OnClosedOrderToDateChanged(DateTime? value) => _ = LoadClosedOrdersAsync();
    partial void OnClosedOrderShiftFilterChanged(string value) => _ = LoadClosedOrdersAsync();
    partial void OnClosedOrderSearchTextChanged(string value) => _ = LoadClosedOrdersAsync();
    partial void OnClosedOrderSortOptionChanged(string value) => _ = LoadClosedOrdersAsync();

    private void NotifyClosedOrderKpis()
    {
        OnPropertyChanged(nameof(ClosedOrderCount));
        OnPropertyChanged(nameof(ClosedOrderRevenue));
        OnPropertyChanged(nameof(ClosedOrderDiscountTotal));
        OnPropertyChanged(nameof(CancelledOrderCount));
        OnPropertyChanged(nameof(AverageOrderValue));
        OnPropertyChanged(nameof(AverageServiceMinutes));
    }

    [RelayCommand]
    private async Task SelectClosedOrderAsync(BreakfastCafeOrderDto? order)
    {
        if (order is null) return;
        if (_loadingClosedDetail) return;
        _loadingClosedDetail = true;
        try
        {
            var detail = await _salesService.GetOrderDetailAsync(order.Id);
            if (detail is null) { _loadingClosedDetail = false; return; }
            SelectedClosedOrder = detail;
            ResetWith(ClosedOrderDetailLines, detail.Lines.Select(l => new PosOrderLineRow(l)).ToList());
            BuildTimeline(detail);
            OnPropertyChanged(nameof(HasSelectedClosedOrder));
            OnPropertyChanged(nameof(HasNoSelectedClosedOrder));
            OnPropertyChanged(nameof(ClosedOrderTypeText));
            OnPropertyChanged(nameof(ClosedOrderPaymentMethod));
        }
        catch (Exception ex) { StatusText = ex.Message; }
        finally { _loadingClosedDetail = false; }
    }

    private void BuildTimeline(BreakfastCafeOrderDto detail)
    {
        ClosedOrderTimeline.Clear();
        ClosedOrderTimeline.Add(new("Tạo đơn", detail.OrderDate, "Thu ngân"));
        if (detail.KitchenSentAt.HasValue)
            ClosedOrderTimeline.Add(new("Gửi bếp", detail.KitchenSentAt.Value, "Thu ngân"));
        var paymentAt = detail.Payments
            .Select(x => ToLocalTimestamp(x.CreatedAt))
            .OrderByDescending(x => x)
            .FirstOrDefault();
        if (detail.Status == "Paid" && paymentAt != default && paymentAt >= detail.OrderDate)
            ClosedOrderTimeline.Add(new("Thanh toán", paymentAt, "Thu ngân"));
        if (detail.Status is "Paid" or "Cancelled")
            ClosedOrderTimeline.Add(new(
                detail.Status == "Paid" ? "Đơn đã đóng" : "Đơn đã hủy",
                detail.Status == "Paid" && paymentAt != default ? paymentAt : ToLocalTimestamp(detail.UpdatedAt),
                "Hệ thống"));
    }

    private static double? CalculateServiceMinutes(BreakfastCafeOrderDto order)
    {
        var payment = order.Payments
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (payment is null)
            return null;

        var startedAtUtc = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Local).ToUniversalTime();
        var completedAtUtc = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc);
        var minutes = (completedAtUtc - startedAtUtc).TotalMinutes;
        return minutes is >= 0 and <= 7 * 24 * 60 ? minutes : null;
    }

    private static DateTime ToLocalTimestamp(DateTime timestamp)
    {
        var utc = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        return utc.ToLocalTime();
    }

    [RelayCommand]
    private void PrintClosedOrder(BreakfastCafeOrderDto? order)
    {
        if (order is null) { StatusText = "Chọn 1 đơn để in."; return; }
        try
        {
            // Paper size selection
            var paperChoice = System.Windows.MessageBox.Show(
                "Chọn khổ giấy in:\n\nYES = K80 (80mm)\nNO = K60 (60mm)\nCancel = Hủy",
                $"In hóa đơn — {order.OrderNumber}",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            if (paperChoice == System.Windows.MessageBoxResult.Cancel) return;

            bool isK80 = paperChoice == System.Windows.MessageBoxResult.Yes;
            double paperWidth = isK80 ? 300 : 230; // 80mm vs 60mm
            double fontSize = isK80 ? 13 : 11;
            double titleSize = isK80 ? 16 : 14;
            double totalSize = isK80 ? 18 : 15;
            int sepLen = isK80 ? 34 : 26;

            var doc = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new System.Windows.Thickness(4),
                ColumnWidth = paperWidth,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = fontSize
            };

            // Header
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("ASMILE — BÁN ĐỒ ĂN SÁNG & CÀ PHÊ"))
                { FontSize = titleSize, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(order.OrderNumber))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Bàn: {order.TableName ?? "Mang đi"}  |  {order.OrderDate:dd/MM/yyyy HH:mm}"))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 2) });
            var seller = _currentUserService.CurrentUser?.DisplayName ?? _currentUserService.CurrentUser?.UserName ?? "Thu ngân";
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Người bán: {seller}"))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 4) });

            // Separator
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', sepLen)))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center });

            // Line items
            if (order.Lines is { Count: > 0 })
            {
                foreach (var line in order.Lines)
                {
                    var linePara = new System.Windows.Documents.Paragraph { FontSize = fontSize, Margin = new System.Windows.Thickness(0, 2, 0, 0), TextAlignment = System.Windows.TextAlignment.Center };
                    linePara.Inlines.Add(new System.Windows.Documents.Run($"{line.MenuItemName}") { FontWeight = System.Windows.FontWeights.SemiBold });
                    linePara.Inlines.Add(new System.Windows.Documents.Run($"\n  {line.Quantity:N1} × {line.UnitPrice:N0}đ = {line.LineTotalAmount:N0}đ"));
                    doc.Blocks.Add(linePara);
                }
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', sepLen)))
                    { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center });
            }

            // Totals
            var totalsPara = new System.Windows.Documents.Paragraph { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 4, 0, 0) };
            totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Tạm tính: {order.SubtotalAmount:N0}đ\n"));
            if (order.DiscountAmount > 0) totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Giảm giá: -{order.DiscountAmount:N0}đ\n"));
            if (order.VatAmount > 0) totalsPara.Inlines.Add(new System.Windows.Documents.Run($"VAT: {order.VatAmount:N0}đ\n"));
            if (order.ServiceChargeAmount > 0) totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Phí DV: {order.ServiceChargeAmount:N0}đ\n"));
            doc.Blocks.Add(totalsPara);

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"TỔNG: {order.TotalAmount:N0}đ"))
                { FontSize = totalSize, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center,
                  Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12)), Margin = new System.Windows.Thickness(0, 6, 0, 0) });

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', sepLen)))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Cảm ơn quý khách!"))
                { FontSize = fontSize, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 4, 0, 0) });

            var viewer = new System.Windows.Controls.FlowDocumentPageViewer { Document = doc };

            var printBtn = new Button
            {
                Content = "🖨️  In hóa đơn",
                Height = 36,
                Padding = new System.Windows.Thickness(16, 0, 16, 0),
                Margin = new System.Windows.Thickness(0, 8, 0, 4),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.SemiBold,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF2, 0x99, 0x4A)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = Cursors.Hand
            };
            ToolTipService.SetToolTip(printBtn, "Mở hộp thoại in hệ thống");
            printBtn.Click += (_, _) =>
            {
                var dlg = new System.Windows.Controls.PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    doc.PageWidth = dlg.PrintableAreaWidth;
                    doc.PageHeight = dlg.PrintableAreaHeight;
                    dlg.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, $"Đơn đóng {order.OrderNumber}");
                }
            };

            var panel = new DockPanel();
            DockPanel.SetDock(printBtn, Dock.Top);
            panel.Children.Add(printBtn);
            panel.Children.Add(viewer);

            var win = new System.Windows.Window
            {
                Title = $"Xem trước — {order.OrderNumber} (K{(isK80 ? "80" : "60")})",
                Width = isK80 ? 420 : 340,
                Height = 700,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow,
                Content = panel,
                WindowStyle = System.Windows.WindowStyle.ToolWindow
            };
            win.ShowDialog();
            StatusText = $"Đã xem trước {order.OrderNumber}.";
        }
        catch (Exception ex) { StatusText = ex.Message; }
    }

    [RelayCommand]
    private void ShowShiftSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("══════ TỔNG KẾT CA ══════");
        sb.AppendLine();
        sb.AppendLine($"📊 Doanh thu:  {Summary.RevenueAmount:N0} đ");
        sb.AppendLine($"📋 Tổng đơn:   {Summary.OrderCount}");
        sb.AppendLine($"📋 Đang mở:    {OpenOrders.Count}");
        sb.AppendLine($"📋 Đã đóng:    {ClosedOrderCount}");
        sb.AppendLine($"🏷️  Giảm giá:  {ClosedOrderDiscountTotal:N0} đ");
        sb.AppendLine($"💰 VAT:        {Summary.VatOutputAmount:N0} đ");
        sb.AppendLine();
        if (Summary.PaymentSummaries is { Count: > 0 })
        {
            sb.AppendLine("── Phương thức thanh toán ──");
            foreach (var ps in Summary.PaymentSummaries)
                sb.AppendLine($"  • {ps.PaymentMethod}: {ps.Amount:N0} đ");
        }
        sb.AppendLine();
        sb.AppendLine("── Hiệu suất nhân viên ──");
        var empGroups = ClosedOrders.GroupBy(x => "Thu ngân");
        foreach (var g in empGroups)
        {
            sb.AppendLine($"  👤 {g.Key}: {g.Count()} đơn — {g.Sum(x => x.TotalAmount):N0} đ");
        }
        sb.AppendLine();
        sb.AppendLine($"🕐 {DateTime.Now:HH:mm dd/MM/yyyy}");
        sb.AppendLine($"👤 {(_currentUserService.CurrentUser?.DisplayName ?? "Administrator")}");

        System.Windows.MessageBox.Show(sb.ToString(), "Tổng kết ca", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task RefundClosedOrderAsync(BreakfastCafeOrderDto? order)
    {
        if (order is null || order.Status != "Paid") { StatusText = "Chỉ hoàn tiền đơn đã thanh toán."; return; }
        var confirm = System.Windows.MessageBox.Show($"Hoàn tiền toàn bộ {order.TotalAmount:N0}đ cho đơn {order.OrderNumber}?\n\nHành động này sẽ tạo phiếu hoàn tiền trong Quỹ và không thể hoàn tác.", "Xác nhận hoàn tiền", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        StatusText = "Tính năng hoàn tiền cần tích hợp service Quỹ. Vui lòng vào Quỹ → Đảo giao dịch để hoàn tiền thủ công.";
    }

    [RelayCommand]
    private async Task LoadReportsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var from = ReportFromDate?.Date ?? DateTime.Today;
            var to = ReportToDate?.Date ?? from;
            if (to < from) (from, to) = (to, from);

            var headers = await _salesService.GetOrdersByDateRangeAsync(from, to);
            IEnumerable<BreakfastCafeOrderDto> query = headers;
            query = ReportStatusFilter switch
            {
                "Open" => query.Where(x => x.Status is not ("Paid" or "Cancelled")),
                "" => query,
                _ => query.Where(x => x.Status == ReportStatusFilter)
            };

            if (!string.IsNullOrWhiteSpace(ReportSearchText))
            {
                var term = ReportSearchText.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.OrderNumber.ToLowerInvariant().Contains(term) ||
                    (x.TableName ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (x.AreaName ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (x.CustomerReference ?? string.Empty).ToLowerInvariant().Contains(term));
            }

            var details = new List<BreakfastCafeOrderDto>();
            foreach (var order in query.OrderByDescending(x => x.OrderDate))
            {
                details.Add(await _salesService.GetOrderDetailAsync(order.Id) ?? order);
            }

            ResetWith(ReportOrders, details.Select(x => new PosReportOrderRow(x)));

            var itemRows = details
                .Where(x => x.Status != "Cancelled")
                .SelectMany(x => x.Lines)
                .GroupBy(x => new { x.MenuItemCode, x.MenuItemName })
                .Select(g => new PosReportItemRow(
                    g.Key.MenuItemCode,
                    g.Key.MenuItemName,
                    g.Sum(x => x.Quantity),
                    g.Sum(x => x.LineTotalAmount),
                    g.Sum(x => x.Quantity) > 0 ? g.Sum(x => x.LineTotalAmount) / g.Sum(x => x.Quantity) : 0m))
                .OrderByDescending(x => x.RevenueAmount)
                .ToList();
            ResetWith(ReportItems, itemRows);

            ResetWith(ReportPayments, details
                .SelectMany(x => x.Payments)
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new PosReportPaymentRow(g.Key, g.Count(), g.Sum(x => x.Amount), g.Count(x => x.Status == "Draft"), g.Count(x => x.Status == "Posted")))
                .OrderByDescending(x => x.Amount));

            ResetWith(ReportAreas, details
                .GroupBy(x => string.IsNullOrWhiteSpace(x.AreaName) ? "Mang đi/khác" : x.AreaName!)
                .Select(g => new PosReportGroupRow(g.Key, g.Count(), g.Count(x => x.Status == "Paid"), g.Sum(x => x.TotalAmount)))
                .OrderByDescending(x => x.TotalAmount));

            ResetWith(ReportOrderTypes, details
                .GroupBy(x => ToOrderTypeText(x.OrderType))
                .Select(g => new PosReportGroupRow(g.Key, g.Count(), g.Count(x => x.Status == "Paid"), g.Sum(x => x.TotalAmount)))
                .OrderByDescending(x => x.TotalAmount));

            NotifyReportState();
            StatusText = $"Đã tải báo cáo {ReportOrders.Count:N0} đơn từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ExportReportsExcel()
    {
        if (ReportOrders.Count == 0)
        {
            StatusText = "Không có dữ liệu báo cáo để xuất.";
            return;
        }

        try
        {
            var from = ReportFromDate?.Date ?? DateTime.Today;
            var to = ReportToDate?.Date ?? from;
            var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"BaoCao_POS_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx" };
            if (dlg.ShowDialog() != true) return;

            using var wb = new XLWorkbook();
            var summary = wb.Worksheets.Add("Tong hop");
            summary.Cell(1, 1).Value = "Báo cáo POS bữa sáng & cà phê";
            summary.Cell(2, 1).Value = "Từ ngày";
            summary.Cell(2, 2).Value = from.ToString("dd/MM/yyyy");
            summary.Cell(3, 1).Value = "Đến ngày";
            summary.Cell(3, 2).Value = to.ToString("dd/MM/yyyy");
            summary.Cell(5, 1).Value = "Tổng đơn";
            summary.Cell(5, 2).Value = ReportOrderCount;
            summary.Cell(6, 1).Value = "Đơn đã thanh toán";
            summary.Cell(6, 2).Value = ReportPaidOrderCount;
            summary.Cell(7, 1).Value = "Đơn đã hủy";
            summary.Cell(7, 2).Value = ReportCancelledOrderCount;
            summary.Cell(8, 1).Value = "Doanh thu";
            summary.Cell(8, 2).Value = (double)ReportRevenueAmount;
            summary.Cell(9, 1).Value = "Giảm giá";
            summary.Cell(9, 2).Value = (double)ReportDiscountAmount;
            summary.Cell(10, 1).Value = "VAT";
            summary.Cell(10, 2).Value = (double)ReportVatAmount;
            summary.Cell(11, 1).Value = "Trung bình/đơn";
            summary.Cell(11, 2).Value = (double)ReportAverageOrderAmount;

            WriteOrderSheet(wb.Worksheets.Add("Chi tiet don"));
            WriteItemSheet(wb.Worksheets.Add("Mon ban"));
            WritePaymentSheet(wb.Worksheets.Add("Thanh toan"));
            WriteGroupSheet(wb.Worksheets.Add("Khu vuc"), ReportAreas);
            WriteGroupSheet(wb.Worksheets.Add("Loai don"), ReportOrderTypes);

            foreach (var ws in wb.Worksheets)
            {
                ws.Row(1).Style.Font.Bold = true;
                ws.Columns().AdjustToContents();
            }

            wb.SaveAs(dlg.FileName);
            StatusText = $"Đã xuất báo cáo POS: {Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private void WriteOrderSheet(IXLWorksheet ws)
    {
        var headers = new[] { "Mã đơn", "Ngày", "Khu vực", "Bàn/phòng", "Loại đơn", "Trạng thái", "Số món", "Tạm tính", "Giảm giá", "VAT", "Phí DV", "Tổng", "Đã thu", "Còn lại" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int row = 2;
        foreach (var x in ReportOrders)
        {
            ws.Cell(row, 1).Value = x.OrderNumber;
            ws.Cell(row, 2).Value = x.OrderDateText;
            ws.Cell(row, 3).Value = x.AreaName;
            ws.Cell(row, 4).Value = x.TableName;
            ws.Cell(row, 5).Value = x.OrderTypeText;
            ws.Cell(row, 6).Value = x.StatusText;
            ws.Cell(row, 7).Value = x.LineCount;
            ws.Cell(row, 8).Value = (double)x.SubtotalAmount;
            ws.Cell(row, 9).Value = (double)x.DiscountAmount;
            ws.Cell(row, 10).Value = (double)x.VatAmount;
            ws.Cell(row, 11).Value = (double)x.ServiceChargeAmount;
            ws.Cell(row, 12).Value = (double)x.TotalAmount;
            ws.Cell(row, 13).Value = (double)x.PaidAmount;
            ws.Cell(row, 14).Value = (double)x.RemainingAmount;
            row++;
        }
    }

    private void WriteItemSheet(IXLWorksheet ws)
    {
        var headers = new[] { "Mã món", "Tên món", "Số lượng", "Doanh thu", "Giá bình quân" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int row = 2;
        foreach (var x in ReportItems)
        {
            ws.Cell(row, 1).Value = x.Code;
            ws.Cell(row, 2).Value = x.Name;
            ws.Cell(row, 3).Value = (double)x.Quantity;
            ws.Cell(row, 4).Value = (double)x.RevenueAmount;
            ws.Cell(row, 5).Value = (double)x.AveragePrice;
            row++;
        }
    }

    private void WritePaymentSheet(IXLWorksheet ws)
    {
        var headers = new[] { "Phương thức", "Số giao dịch", "Số tiền", "Nháp", "Đã ghi sổ" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int row = 2;
        foreach (var x in ReportPayments)
        {
            ws.Cell(row, 1).Value = x.PaymentMethodText;
            ws.Cell(row, 2).Value = x.TransactionCount;
            ws.Cell(row, 3).Value = (double)x.Amount;
            ws.Cell(row, 4).Value = x.DraftCount;
            ws.Cell(row, 5).Value = x.PostedCount;
            row++;
        }
    }

    private static void WriteGroupSheet(IXLWorksheet ws, IEnumerable<PosReportGroupRow> rows)
    {
        var headers = new[] { "Nhóm", "Tổng đơn", "Đơn đã thanh toán", "Doanh thu" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int row = 2;
        foreach (var x in rows)
        {
            ws.Cell(row, 1).Value = x.Name;
            ws.Cell(row, 2).Value = x.OrderCount;
            ws.Cell(row, 3).Value = x.PaidOrderCount;
            ws.Cell(row, 4).Value = (double)x.TotalAmount;
            row++;
        }
    }

    private void NotifyReportState()
    {
        OnPropertyChanged(nameof(ReportOrderCount));
        OnPropertyChanged(nameof(ReportPaidOrderCount));
        OnPropertyChanged(nameof(ReportCancelledOrderCount));
        OnPropertyChanged(nameof(ReportRevenueAmount));
        OnPropertyChanged(nameof(ReportDiscountAmount));
        OnPropertyChanged(nameof(ReportVatAmount));
        OnPropertyChanged(nameof(ReportAverageOrderAmount));
        OnPropertyChanged(nameof(HasReportOrders));
    }

    [RelayCommand]
    private void ExportClosedOrdersExcel()
    {
        if (ClosedOrders.Count == 0) { StatusText = "Không có đơn nào để xuất."; return; }
        try
        {
            var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"DonDong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx" };
            if (dlg.ShowDialog() != true) return;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Đơn đóng");
            var headers = new[] { "Mã đơn", "Bàn", "Ngày", "Trạng thái", "Thanh toán", "Tạm tính", "Giảm giá", "VAT", "Phí DV", "Tổng" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            int row = 2;
            foreach (var o in ClosedOrders)
            {
                ws.Cell(row, 1).Value = o.OrderNumber;
                ws.Cell(row, 2).Value = o.TableName ?? "Mang đi";
                ws.Cell(row, 3).Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = o.Status == "Paid" ? "Đã thanh toán" : "Đã hủy";
                ws.Cell(row, 5).Value = o.Payments.FirstOrDefault()?.PaymentMethod ?? "";
                ws.Cell(row, 6).SetValue((double)o.SubtotalAmount);
                ws.Cell(row, 7).SetValue((double)o.DiscountAmount);
                ws.Cell(row, 8).SetValue((double)o.VatAmount);
                ws.Cell(row, 9).SetValue((double)o.ServiceChargeAmount);
                ws.Cell(row, 10).SetValue((double)o.TotalAmount);
                row++;
            }
            ws.Columns().AdjustToContents();
            wb.SaveAs(dlg.FileName);
            StatusText = $"Đã xuất {ClosedOrders.Count} đơn ra {Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex) { StatusText = ex.Message; }
    }

    [RelayCommand]
    private void CreateSimilarOrder(BreakfastCafeOrderDto? order)
    {
        if (order is null) return;
        StatusText = $"Đã tạo đơn mới từ {order.OrderNumber}. Vào tab Thực đơn để thêm món.";
        LeftTabIndex = 1;
    }

    public sealed record ClosedOrderTimelineEntry(string Action, DateTime Time, string Actor);
    public string ClosedOrderPaymentMethod => SelectedClosedOrder?.Payments.FirstOrDefault()?.PaymentMethod ?? "";
    public string ClosedOrderTypeText => (SelectedClosedOrder?.OrderType) switch
    {
        "DineIn" => "Tại bàn", "TakeAway" => "Mang đi", "RoomService" => "Phục vụ phòng", _ => ""
    };

    private async Task RefreshOpenDataSafeAsync()
    {
        try
        {
            await WaitUntilNotBusyAsync();
            await RefreshOpenDataAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private async Task WaitUntilNotBusyAsync()
    {
        while (IsBusy)
        {
            await Task.Delay(100);
        }
    }

    private void ApplyOpenOrderSort()
    {
        ResetWith(OpenOrders, SortOpenOrders(OpenOrders.ToList()));
    }

    private IEnumerable<BreakfastCafeOrderDto> SortOpenOrders(IEnumerable<BreakfastCafeOrderDto> rows)
    {
        return SelectedOpenOrderSort switch
        {
            "Cũ nhất" => rows.OrderBy(x => x.OrderDate),
            "Tổng tiền cao" => rows.OrderByDescending(x => x.TotalAmount),
            "Tổng tiền thấp" => rows.OrderBy(x => x.TotalAmount),
            "Theo bàn" => rows.OrderBy(x => x.TableName).ThenByDescending(x => x.OrderDate),
            "Nhiều món" => rows.OrderByDescending(x => x.LineCount).ThenByDescending(x => x.OrderDate),
            _ => rows.OrderByDescending(x => x.OrderDate)
        };
    }

    private void ApplyTableFilter()
    {
        var rows = _allTables.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedArea))
            rows = rows.Where(x => x.AreaName == SelectedArea);

        rows = TableStatusFilter switch
        {
            "Trống" => rows.Where(x => x.Status == "Empty"),
            "Đang phục vụ" => rows.Where(x => x.Status == "Serving"),
            "Chờ thanh toán" => rows.Where(x => x.Status == "AwaitingPayment"),
            "Đã đặt" => rows.Where(x => x.Status == "Reserved"),
            _ => rows
        };

        if (!string.IsNullOrWhiteSpace(TableSearchText))
        {
            var term = TableSearchText.Trim();
            rows = rows.Where(x => x.TableName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                || x.TableCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        ResetWith(FilteredTables, rows.OrderBy(x => x.SortOrder).ThenBy(x => x.TableCode));
    }

    private void ApplyMenuFilter()
    {
        IEnumerable<MenuItemDto> query = _allMenuItems.Where(x => x.IsActive);
        var allowedIds = BreakfastCafeCategorySelections
            .Where(x => x.IsEnabled)
            .Select(x => x.CategoryId)
            .ToHashSet();
        if (allowedIds.Count > 0)
        {
            query = query.Where(x => allowedIds.Contains(x.MenuCategoryId));
        }

        if (SelectedCategory?.CategoryId is Guid categoryId)
        {
            query = query.Where(x => x.MenuCategoryId == categoryId);
        }

        query = SelectedMenuFilter switch
        {
            "Có định lượng" => query.Where(x => x.HasRecipe),
            "Chưa định lượng" => query.Where(x => !x.HasRecipe),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        query = SelectedSortMode switch
        {
            "Giá tăng dần" => query.OrderBy(x => x.UnitPrice).ThenBy(x => x.Name),
            "Giá giảm dần" => query.OrderByDescending(x => x.UnitPrice).ThenBy(x => x.Name),
            "Nhóm món" => query.OrderBy(x => x.CategoryName).ThenBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        ResetWith(FilteredMenuItems, query.Select(x =>
        {
            var card = new PosMenuItemCard(x);
            if (x.HasRecipeManagement && !x.HasRecipe)
            {
                card.IsOutOfStock = true;
                card.HasStockInfo = true;
            }
            if (x.HasAvailabilityWarning || !string.IsNullOrWhiteSpace(x.StockWarning))
            {
                card.HasStockInfo = true;
            }
            return card;
        }));
    }

    private void ApplySettingMenuItemFilter()
    {
        IEnumerable<MenuItemDto> query = SettingMenuItems;
        if (!string.Equals(SelectedSettingMenuItemClassification, "Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(ResolveSettingMenuItemClassification(x), SelectedSettingMenuItemClassification, StringComparison.OrdinalIgnoreCase));
        }

        ResetWith(FilteredSettingMenuItems, query.OrderBy(x => x.CategoryName).ThenBy(x => x.Name));
    }

    private static string ResolveSettingMenuItemClassification(MenuItemDto item)
    {
        if (string.Equals(item.PreparationArea, "Support", StringComparison.OrdinalIgnoreCase))
        {
            return "Phụ Trợ";
        }

        if (string.Equals(item.PreparationArea, "Service", StringComparison.OrdinalIgnoreCase))
        {
            return "Dịch vụ";
        }

        if (string.Equals(item.PreparationArea, "Other", StringComparison.OrdinalIgnoreCase))
        {
            return "Loại Khác";
        }

        var text = $"{item.CategoryName} {item.Name} {item.Code}".ToLowerInvariant();
        if (IsBarPreparationArea(item.PreparationArea)
            || text.Contains("cà phê")
            || text.Contains("coffee")
            || text.Contains("trà")
            || text.Contains("tea")
            || text.Contains("nước")
            || text.Contains("juice")
            || text.Contains("sinh tố")
            || text.Contains("smoothie")
            || text.Contains("latte")
            || text.Contains("espresso"))
        {
            return "Đồ uống";
        }

        if (text.Contains("dịch vụ")
            || text.Contains("service")
            || text.Contains("phụ thu")
            || text.Contains("setup")
            || text.Contains("phòng")
            || text.Contains("room"))
        {
            return "Dịch vụ";
        }

        if (text.Contains("khăn")
            || text.Contains("đá")
            || text.Contains("túi")
            || text.Contains("ly")
            || text.Contains("ống hút")
            || text.Contains("topping")
            || text.Contains("phụ")
            || text.Contains("addon")
            || text.Contains("add-on"))
        {
            return "Phụ Trợ";
        }

        if (IsKitchenPreparationArea(item.PreparationArea)
            || text.Contains("bánh")
            || text.Contains("mì")
            || text.Contains("xôi")
            || text.Contains("phở")
            || text.Contains("bún")
            || text.Contains("cơm")
            || text.Contains("ăn sáng")
            || text.Contains("đồ ăn")
            || text.Contains("food"))
        {
            return "Đồ ăn";
        }

        return "Loại Khác";
    }

    private static string MapClassificationToPreparationArea(string classification)
        => classification switch
        {
            "Đồ uống" => "Bar",
            "Phụ Trợ" => "Support",
            "Dịch vụ" => "Service",
            "Loại Khác" => "Other",
            _ => "Kitchen"
        };

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            StatusText = "Đang xử lý thao tác trước, vui lòng bấm lại sau một giây.";
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ToPaymentCode(string value)
    {
        return value switch
        {
            "Chuyển khoản" => "BankTransfer",
            "POS/Thẻ" => "CardPOS",
            "Ghi phòng" => "RoomCharge",
            "Công nợ" => "CustomerDebt",
            _ => "Cash"
        };
    }

    private static string ToOrderTypeText(string? value)
    {
        return value switch
        {
            "TakeAway" => "Mang đi",
            "RoomService" => "Phục vụ phòng",
            _ => "Tại bàn"
        };
    }

    private static string ToServiceStatusText(string? value)
    {
        return value switch
        {
            "New" => "Mới",
            "SentKitchen" => "Đã gửi bếp",
            "Serving" => "Đang phục vụ",
            "AwaitingPayment" => "Chờ thanh toán",
            "Paid" => "Đã thanh toán",
            "Cancelled" => "Đã hủy",
            _ => value ?? "Mới"
        };
    }

    private static void ResetWith<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}

public sealed class BreakfastCafeSalesPolicyState
{
    public bool EnableDineInServiceCharge { get; set; }
    public bool EnableTakeAwayServiceCharge { get; set; }
    public bool EnableRoomServiceServiceCharge { get; set; } = true;
    public decimal DefaultServiceChargeRate { get; set; } = 5m;
    public bool ApplyServiceChargeAfterDiscount { get; set; } = true;
    public bool IncludeServiceChargeInVatBase { get; set; }
    public bool ShowServiceChargeLineOnReceipt { get; set; } = true;
    public bool AllowOrderDiscount { get; set; } = true;
    public bool AllowLineDiscount { get; set; }
    public decimal MaxOrderDiscountPercent { get; set; } = 20m;
    public decimal MaxOrderDiscountAmount { get; set; } = 500000m;
    public bool RequireManagerApprovalForDiscount { get; set; } = true;
    public bool RequireDiscountReason { get; set; }
    public bool EnableVoucherDiscount { get; set; }
    public string DefaultDiscountReason { get; set; } = "Khuyến mại";
    public bool EnableCashPayment { get; set; } = true;
    public bool EnableBankTransferPayment { get; set; } = true;
    public bool EnableCardPosPayment { get; set; } = true;
    public bool EnableRoomChargePayment { get; set; } = true;
    public bool EnableCustomerDebtPayment { get; set; } = true;
    public bool AllowSplitPayment { get; set; }
    public bool RequirePaymentReferenceForBank { get; set; } = true;
    public string DefaultQrBankAccount { get; set; } = string.Empty;
    public string DefaultCashAccountCode { get; set; } = "111";
    public string DefaultBankAccountCode { get; set; } = "112";
    public bool AutoPrintKitchenTicket { get; set; } = true;
    public bool PrintKitchenTicketOnEdit { get; set; }
    public bool AllowKitchenReprint { get; set; } = true;
    public bool RequireKitchenReprintReason { get; set; } = true;
    public string KitchenPrinterName { get; set; } = "Kitchen Printer";
    public string BarPrinterName { get; set; } = "Bar Printer";
    public string KitchenPaperSize { get; set; } = "80mm";
    public string BarPaperSize { get; set; } = "80mm";
    public string DefaultPreparationStation { get; set; } = "Kitchen";
    public bool RequireOpenShiftBeforeSale { get; set; }
    public bool RequireCashCountOnCloseShift { get; set; } = true;
    public bool LockSalesAfterShiftClose { get; set; } = true;
    public decimal OpeningCashAmount { get; set; }
    public decimal MaxShiftCashVariance { get; set; } = 10000m;
    public string CurrentShiftName { get; set; } = "Ca sáng";
    public List<ShiftSettingState> ShiftSettings { get; set; } = [];
    public bool AutoPrintReceiptAfterPayment { get; set; }
    public bool AllowProvisionalPrint { get; set; } = true;
    public string ReceiptPrinterName { get; set; } = "Receipt Printer";
    public string ReceiptPaperSize { get; set; } = "80mm";
    public int ReceiptCopyCount { get; set; } = 1;
    public string ReceiptFooterText { get; set; } = "Cảm ơn quý khách!";
    public bool PrintStoreLogo { get; set; } = true;
    public bool CashierCanCancelItem { get; set; } = true;
    public bool CashierCanCancelOrder { get; set; }
    public bool CashierCanEditPrice { get; set; }
    public bool CashierCanViewReports { get; set; }
    public bool RequireManagerPinForSensitiveActions { get; set; } = true;
    public decimal CashierMaxDiscountPercent { get; set; } = 10m;
    public bool CheckInventoryBeforeSale { get; set; } = true;
    public bool BlockSaleWhenOutOfStock { get; set; } = true;
    public bool WarnMenuWithoutRecipe { get; set; } = true;
    public bool ConsumeInventoryOnPayment { get; set; } = true;
    public bool ConsumeInventoryOnKitchenSend { get; set; }
    public string DefaultSaleWarehouseCode { get; set; } = "KHO-001";
    public bool EnableSettingsAuditLog { get; set; } = true;
    public bool RequireConfirmBeforeReset { get; set; } = true;
    public bool AllowExportSettings { get; set; } = true;
    public bool AllowImportSettings { get; set; } = true;
    public int AuditRetentionDays { get; set; } = 365;
    public string SettingsBackupFolder { get; set; } = "Data\\Settings";
    public List<Guid> AllowedBreakfastCafeCategoryIds { get; set; } = [];
}

public sealed class ShiftSettingState
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = "06:00";
    public string EndTime { get; set; } = "14:00";
    public bool IsActive { get; set; } = true;
}

public sealed partial class ShiftSettingRow : ObservableObject
{
    public ShiftSettingRow(string code, string name, string startTime, string endTime, bool isActive)
    {
        Code = code;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
    }

    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string startTime = "06:00";
    [ObservableProperty] private string endTime = "14:00";
    [ObservableProperty] private bool isActive = true;

    public string TimeRangeText => $"{StartTime} - {EndTime}";
    public string StatusText => IsActive ? "Đang dùng" : "Tắt";

    partial void OnStartTimeChanged(string value) => OnPropertyChanged(nameof(TimeRangeText));
    partial void OnEndTimeChanged(string value) => OnPropertyChanged(nameof(TimeRangeText));
    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(StatusText));
}

public sealed partial class CategorySaleSelectionRow : ObservableObject
{
    public CategorySaleSelectionRow(Guid categoryId, string code, string name, bool isEnabled)
    {
        CategoryId = categoryId;
        Code = code;
        Name = name;
        IsEnabled = isEnabled;
    }

    public Guid CategoryId { get; }
    public string Code { get; }
    public string Name { get; }

    [ObservableProperty] private bool isEnabled;
}

public sealed record PosCategoryFilter(Guid? CategoryId, string Name, int SortOrder)
{
    public bool IsSelected { get; set; }
}

public sealed record PosAreaSettingRow(
    string AreaCode,
    string AreaName,
    int TableCount,
    int BusyTableCount,
    int SortOrder)
{
    public string TableCountText => $"{TableCount} bàn";
    public string BusyText => BusyTableCount == 0 ? "Chưa có đơn mở" : $"{BusyTableCount} bàn đang dùng";
}

public sealed class PosReportOrderRow
{
    public PosReportOrderRow(BreakfastCafeOrderDto order)
    {
        OrderNumber = order.OrderNumber;
        OrderDate = order.OrderDate;
        AreaName = string.IsNullOrWhiteSpace(order.AreaName) ? "Khác" : order.AreaName!;
        TableName = string.IsNullOrWhiteSpace(order.TableName) ? "Mang đi" : order.TableName!;
        OrderType = order.OrderType;
        Status = order.Status;
        LineCount = order.LineCount;
        SubtotalAmount = order.SubtotalAmount;
        DiscountAmount = order.DiscountAmount;
        VatAmount = order.VatAmount;
        ServiceChargeAmount = order.ServiceChargeAmount;
        TotalAmount = order.TotalAmount;
        PaidAmount = order.PaidAmount;
        RemainingAmount = order.RemainingAmount;
    }

    public string OrderNumber { get; }
    public DateTime OrderDate { get; }
    public string OrderDateText => OrderDate.ToString("dd/MM/yyyy HH:mm");
    public string AreaName { get; }
    public string TableName { get; }
    public string OrderType { get; }
    public string OrderTypeText => OrderType switch
    {
        "TakeAway" => "Mang đi",
        "RoomService" => "Phục vụ phòng",
        _ => "Tại bàn"
    };
    public string Status { get; }
    public string StatusText => Status switch
    {
        "Paid" => "Đã thanh toán",
        "Cancelled" => "Đã hủy",
        "Draft" => "Nháp",
        "SentKitchen" => "Đã gửi bếp",
        _ => "Đang mở"
    };
    public int LineCount { get; }
    public decimal SubtotalAmount { get; }
    public decimal DiscountAmount { get; }
    public decimal VatAmount { get; }
    public decimal ServiceChargeAmount { get; }
    public decimal TotalAmount { get; }
    public decimal PaidAmount { get; }
    public decimal RemainingAmount { get; }
}

public sealed record PosReportItemRow(string Code, string Name, decimal Quantity, decimal RevenueAmount, decimal AveragePrice);

public sealed record PosReportPaymentRow(string PaymentMethod, int TransactionCount, decimal Amount, int DraftCount, int PostedCount)
{
    public string PaymentMethodText => PaymentMethod switch
    {
        "Cash" => "Tiền mặt",
        "BankTransfer" => "Chuyển khoản",
        "CardPOS" => "POS/Thẻ",
        "RoomCharge" => "Ghi phòng",
        "CustomerDebt" => "Công nợ",
        _ => PaymentMethod
    };
}

public sealed record PosReportGroupRow(string Name, int OrderCount, int PaidOrderCount, decimal TotalAmount);

public sealed record PosOrderTabRow(
    Guid OrderId,
    string DisplayName,
    string OrderNumber,
    decimal TotalAmount,
    bool IsSelected)
{
    public string TotalText => TotalAmount <= 0 ? "Chưa có món" : $"{TotalAmount:N0} đ";
}

public sealed record KitchenPreviewLine(string MenuItemName, decimal Quantity, decimal UnitPrice, decimal LineTotalAmount, string? Note);

public sealed class PosMenuItemCard
{
    public PosMenuItemCard(MenuItemDto item)
    {
        Id = item.Id;
        MenuCategoryId = item.MenuCategoryId;
        CategoryName = item.CategoryName;
        Code = item.Code;
        Name = item.Name;
        UnitName = item.UnitName;
        UnitPrice = item.UnitPrice;
        HasRecipe = item.HasRecipe;
        HasRecipeManagement = item.HasRecipeManagement;
        IsActive = item.IsActive;
        HasAvailabilityWarning = item.HasAvailabilityWarning;
        HasStockInfo = item.HasAvailabilityWarning || !string.IsNullOrWhiteSpace(item.StockWarning);
        IsLowStock = item.IsLowStock;
        IsOutOfStock = item.IsOutOfStock;
        StockWarningText = item.StockWarning;
        ImagePath = item.ImagePath;
        ImageUrl = item.ImageUrl;
        Emoji = ResolveEmoji(item);
    }

    public Guid Id { get; }
    public Guid MenuCategoryId { get; }
    public string CategoryName { get; }
    public string Code { get; }
    public string Name { get; }
    public string UnitName { get; }
    public decimal UnitPrice { get; }
    public bool HasRecipe { get; }
    public bool HasRecipeManagement { get; }
    public bool IsActive { get; }
    public bool HasAvailabilityWarning { get; }
    public string? ImagePath { get; }
    public string? ImageUrl { get; }
    public string? DisplayImageSource => !string.IsNullOrWhiteSpace(ImagePath) ? ImagePath : ImageUrl;
    public bool HasImage => !string.IsNullOrWhiteSpace(DisplayImageSource);
    public bool HasNoImage => !HasImage;
    public string Emoji { get; }
    public string RecipeBadge => HasRecipe ? "Có định lượng" : "Chưa định lượng";
    public string StatusBadge => IsActive ? "Đang bán" : "Ngừng bán";

    // Stock & availability
    public bool HasStockInfo { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public string? StockWarningText { get; }
    public bool HasStockWarning => !string.IsNullOrWhiteSpace(StockWarning);
    public string StockWarning => !string.IsNullOrWhiteSpace(StockWarningText) ? StockWarningText! :
                                   !HasRecipe && HasRecipeManagement ? "Chưa định lượng" :
                                   IsOutOfStock ? "Hết hàng" :
                                   IsLowStock ? "Tồn kho thấp" : "";
    public bool CanOrder => IsActive && HasRecipe && !HasAvailabilityWarning && !IsOutOfStock && !HasStockWarning;

    private static string ResolveEmoji(MenuItemDto item)
    {
        var text = $"{item.CategoryName} {item.Name}".ToLowerInvariant();
        if (text.Contains("cà phê") || text.Contains("coffee"))
        {
            return "☕";
        }

        if (text.Contains("trà") || text.Contains("nước"))
        {
            return "🥤";
        }

        if (text.Contains("combo"))
        {
            return "🍽";
        }

        if (text.Contains("phòng") || text.Contains("hotel"))
        {
            return "🏨";
        }

        return "🥐";
    }
}

public sealed partial class PosOrderLineRow : ObservableObject
{
    public PosOrderLineRow(BreakfastCafeOrderLineDto line)
    {
        Id = line.Id;
        LineNo = line.LineNo;
        MenuItemId = line.MenuItemId;
        MenuItemCode = line.MenuItemCode;
        MenuItemName = line.MenuItemName;
        UnitName = line.UnitName;
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        LineTotalAmount = line.LineTotalAmount;
        KitchenStatus = line.KitchenStatus;
        CreatedAt = line.CreatedAt;
        UpdatedAt = line.UpdatedAt;
        ImagePath = line.ImagePath;
        ImageUrl = line.ImageUrl;
        NoteText = line.Note ?? string.Empty;
    }

    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemCode { get; set; } = "";
    public string MenuItemName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string KitchenStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string? DisplayImageSource => !string.IsNullOrWhiteSpace(ImagePath) ? ImagePath : ImageUrl;
    public bool HasImage => !string.IsNullOrWhiteSpace(DisplayImageSource);
    public bool HasNoImage => !HasImage;
    public string QuantityDisplay => Quantity % 1 == 0 ? $"{Quantity:N0}" : $"{Quantity:N2}";
    public string KitchenStatusText => KitchenStatus == "Sent" ? "Đã gửi" : "Mới";

    [ObservableProperty] private string noteText = string.Empty;
    [ObservableProperty] private bool isNotePopupOpen;
    [ObservableProperty] private bool isSelected;
}
