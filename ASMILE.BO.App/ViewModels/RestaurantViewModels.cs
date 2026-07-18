using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Models;
using ASMILE.BO.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using ASMILE.BO.App.Views.Dialogs;
using System.Windows.Threading;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class AlacarteRestaurantViewModel : ViewModelBase
{
    private readonly IAlacarteRestaurantService _service;
    private readonly IQuoteChangeService _quoteChangeService;
    private readonly IMenuCatalogService? _menuCatalog;
    private readonly IMenuRecipeService? _recipeService;
    private readonly ICurrentUserService? _currentUser;
    private readonly ICashFundService? _cashFundService;
    private readonly IRoomOrderMenuPlanningEngine _roomOrderMenuPlanningEngine;
    private readonly DispatcherTimer _dashboardRefreshTimer;
    private string? _editingQuoteNo;
    private readonly List<string> _manualDiningAreas = [];
    private readonly HashSet<Guid> _allowedMenuCategoryIds = [];
    private readonly Dictionary<Guid, string> _menuItemTypeOverrides = [];
    private bool _isAutoRefreshingDashboard;

    public ObservableCollection<DiningRoomDto> Rooms { get; } = new();
    public ObservableCollection<DiningTableDto> Tables { get; } = new();
    public ObservableCollection<DiningTableDto> FilteredTables { get; } = new();
    public ObservableCollection<DiningRoomDto> FilteredRooms { get; } = new();
    public ObservableCollection<TableReservationDto> Reservations { get; } = new();
    public ObservableCollection<string> Floors { get; } = new();
    public ObservableCollection<QuoteLineItem> QuoteLines { get; } = new();
    public ObservableCollection<string> QuotePricingRules { get; } = new(["PerPerson", "PerTable", "PerRoom", "Manual"]);
    public ObservableCollection<BanquetMenuPackageDto> MenuPackages { get; } = new();
    [ObservableProperty] private string selectedFloor = string.Empty;
    public ObservableCollection<AlacarteOrderDto> OpenOrders { get; } = new();
    public ObservableCollection<AlacarteOrderDto> FilteredOpenOrders { get; } = new();
    public ObservableCollection<DiningRoomDto> OpenOrderRooms { get; } = new();
    public ObservableCollection<RestaurantContractDto> Contracts { get; } = new();
    public ObservableCollection<AlacarteServiceItem> AlacarteServices { get; } = new();
    public ObservableCollection<AlacarteAlertItem> Alerts { get; } = new();
    public ObservableCollection<MetricPoint> DashboardRevenueSeries { get; } = new();

    [ObservableProperty] private AlacarteDashboardDto dashboard = new();
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private string statusText = "Sẵn sàng";
    [ObservableProperty] private DiningRoomDto? selectedRoom;
    [ObservableProperty] private DiningRoomDto? selectedOrderRoomFilter;
    [ObservableProperty] private string selectedOrderSearchText = string.Empty;
    [ObservableProperty] private DiningTableDto? selectedTable;
    [ObservableProperty] private AlacarteOrderDto? selectedOrder;
    [ObservableProperty] private RestaurantContractDto? selectedContract;
    [ObservableProperty] private bool isRoomSetupDialogOpen;
    [ObservableProperty] private bool isOrderItemPickerOpen;
    [ObservableProperty] private bool isServingPlanDialogOpen;
    [ObservableProperty] private bool isOrderItemPickerMinimized;
    [ObservableProperty] private bool isServingPlanDialogMinimized;
    [ObservableProperty] private ServingGroupEditorRow? selectedServingGroupEditor;
    [ObservableProperty] private string servingGroupEditorName = string.Empty;
    [ObservableProperty] private string servingGroupEditorServingRule = "PerTable";
    [ObservableProperty] private string servingGroupEditorNote = string.Empty;
    [ObservableProperty] private DiningRoomDto? roomSetupRoom;
    [ObservableProperty] private Guid? roomSetupOrderId;
    [ObservableProperty] private string roomSetupCustomerName = string.Empty;
    [ObservableProperty] private string roomSetupPhone = string.Empty;
    [ObservableProperty] private int roomSetupGuestCount = 20;
    [ObservableProperty] private int roomSetupTableCount = 2;
    [ObservableProperty] private string roomSetupStartTime = string.Empty;
    [ObservableProperty] private string roomSetupEndTime = string.Empty;
    [ObservableProperty] private string roomSetupNote = string.Empty;
    [ObservableProperty] private string roomCode = string.Empty;
    [ObservableProperty] private string roomName = string.Empty;
    [ObservableProperty] private int roomCapacity = 40;
    [ObservableProperty] private string tableCode = string.Empty;
    [ObservableProperty] private string tableName = string.Empty;
    [ObservableProperty] private int tableSeats = 4;
    [ObservableProperty] private string reservationCustomerName = string.Empty;
    [ObservableProperty] private string reservationPhone = string.Empty;
    [ObservableProperty] private DateTime reservationDateTime = DateTime.Now.AddHours(2);
    [ObservableProperty] private int reservationGuestCount = 4;
    [ObservableProperty] private int quoteGuestCount = 40;
    [ObservableProperty] private int quoteGuestsPerTable = 10;
    [ObservableProperty] private DateTime quoteDate = DateTime.Today;
    [ObservableProperty] private string quoteStartTime = "17:00";
    [ObservableProperty] private string quoteEndTime = "21:00";
    [ObservableProperty] private string quoteNo = "BG-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
    [ObservableProperty] private int quoteSubTabIndex;
    public bool IsQuoteFoodTab => QuoteSubTabIndex == 0;
    public bool IsQuoteServiceTab => QuoteSubTabIndex == 1;
    public bool IsQuotePackageTab => QuoteSubTabIndex == 2;
    public string PreparedByName => _currentUser?.CurrentUser?.DisplayName ?? _currentUser?.CurrentUser?.UserName ?? "Nhân viên";

    // Settings
    [ObservableProperty] private decimal defaultVatRate = 8m;
    [ObservableProperty] private decimal defaultServiceChargeRate = 5m;
    [ObservableProperty] private decimal defaultDepositRate = 30m;
    [ObservableProperty] private int defaultHoldMinutes = 15;
    [ObservableProperty] private int alertBeforeMinutes = 30;
    [ObservableProperty] private bool enableAutoTableColor = true;
    [ObservableProperty] private int dashboardRoomStatusColumnCount = 3;
    [ObservableProperty] private int dashboardServingColumnCount = 4;
    [ObservableProperty] private int dashboardTimelineColumnCount = 2;
    [ObservableProperty] private int dashboardAlertColumnCount = 2;
    [ObservableProperty] private int dashboardActivityColumnCount = 3;

    // Right panel sub-tab
    [ObservableProperty] private int orderDetailTabIndex;
    [ObservableProperty] private int orderItemPickerTabIndex;
    [ObservableProperty] private int orderItemPickerStepIndex;
    [ObservableProperty] private PosMenuItemCard? selectedOrderItemPickerMenuItem;
    [ObservableProperty] private string selectedOrderItemPickerScope = "AllServing";
    [ObservableProperty] private string selectedOrderItemPickerServingRule = "PerGuest";
    [ObservableProperty] private decimal orderItemPickerBaseQuantity = 1m;
    [ObservableProperty] private decimal orderItemPickerExtraUnitQuantity = 1m;
    [ObservableProperty] private decimal orderItemPickerManualQuantity = 1m;

    // Settings sub-tab (0-10)
    [ObservableProperty] private int settingsSubTabIndex;

    // Services sub-tab: 0=Món ăn, 1=Dịch vụ
    [ObservableProperty] private int servicesSubTabIndex;

    public ObservableCollection<AlacarteMenuItemCard> AlacarteMenuItems { get; } = new();
    public ObservableCollection<MenuCategoryDto> MenuCategories { get; } = new();
    public ObservableCollection<MenuCategoryDto> QuoteMenuCategories { get; } = new();
    public ObservableCollection<PosMenuItemCard> FilteredMenuItems { get; } = new();
    public ObservableCollection<PosMenuItemCard> OrderItemPickerMenuItems { get; } = new();
    public ObservableCollection<AlacarteServiceItem> OrderItemPickerServices { get; } = new();
    public ObservableCollection<string> OrderItemPickerCategories { get; } = new(["Tất cả"]);
    public ObservableCollection<OrderItemPickerPlanRow> OrderItemPickerPlanRows { get; } = new();
    public ObservableCollection<ServingGroupSummaryRow> ServingGroupSummaries { get; } = new();
    public ObservableCollection<ServingGroupEditorRow> ServingGroupEditorRows { get; } = new();
    public ObservableCollection<ServingGroupTableSelectionRow> ServingGroupTableSelectionRows { get; } = new();
    public ObservableCollection<PreparationQueueRow> PreparationQueueRows { get; } = new();
    public ObservableCollection<RoomServingPlanRow> RoomServingPlanRows { get; } = new();
    public ObservableCollection<AlacarteOrderLineDto> SelectedOrderFoodLines { get; } = new();
    public ObservableCollection<AlacarteOrderLineDto> SelectedOrderWineLines { get; } = new();
    public ObservableCollection<AlacarteOrderLineDto> SelectedOrderDrinkLines { get; } = new();
    public ObservableCollection<AlacarteOrderLineDto> SelectedOrderServiceLines { get; } = new();

    public IEnumerable<Guid> AddedFoodItemIds
        => (SelectedOrder?.Lines ?? Enumerable.Empty<AlacarteOrderLineDto>())
            .Where(l => !IsOrderServiceLine(l))
            .Select(l => l.MenuItemId)
            .Distinct();

    public ObservableCollection<RestaurantPreparationTicketDto> SelectedOrderPreparationTickets { get; } = new();
    public ObservableCollection<PosMenuItemCard> PagedQuoteMenuItems { get; } = new();
    public ObservableCollection<KeyValueOption> OrderItemPickerScopeOptions { get; } =
    [
        new("AllServing", "Tất cả bàn phục vụ"),
        new("StandardOnly", "Chỉ bàn chuẩn"),
        new("OddOnly", "Chỉ bàn lẻ"),
        new("SelectedTable", "Bàn đang chọn")
    ];
    public ObservableCollection<KeyValueOption> OrderItemPickerServingRuleOptions { get; } =
    [
        new("PerGuest", "Theo khách"),
        new("PerTable", "Theo bàn"),
        new("PerTableWithExtraUnit", "Bàn chuẩn + phần bù"),
        new("Manual", "Thủ công")
    ];
    public ObservableCollection<KeyValueOption> ServingGroupEditorServingRuleOptions { get; } =
    [
        new("PerGuest", "Theo khách"),
        new("PerTable", "Theo bàn"),
        new("PerTableWithExtraUnit", "Bàn chuẩn + phần bù"),
        new("Manual", "Thủ công")
    ];

    // Menu filter properties
    [ObservableProperty] private string menuSearchText = string.Empty;
    [ObservableProperty] private string orderItemPickerSearchText = string.Empty;
    [ObservableProperty] private string selectedMenuSortMode = "Tên món";
    [ObservableProperty] private string selectedMenuViewMode = "Lưới";
    [ObservableProperty] private string selectedOrderItemPickerCategory = "Tất cả";
    [ObservableProperty] private int orderItemPickerMenuPageIndex = 1;
    [ObservableProperty] private int orderItemPickerMenuPageSize = 8;
    [ObservableProperty] private int orderItemPickerServicePageIndex = 1;
    [ObservableProperty] private int orderItemPickerServicePageSize = 8;
    [ObservableProperty] private int servingPlanTabIndex;
    public ObservableCollection<string> MenuSortModes { get; } = new(["Tên món", "Giá tăng dần", "Giá giảm dần", "Nhóm món"]);
    public ObservableCollection<string> MenuViewModes { get; } = new(["Lưới", "Danh sách"]);
    public ObservableCollection<int> OrderItemPickerPageSizeOptions { get; } = new([6, 8, 12, 16]);
    public ObservableCollection<int> DashboardColumnCountOptions { get; } = new([1, 2, 3, 4]);
    [ObservableProperty] private int quoteMenuPageIndex = 1;
    [ObservableProperty] private int quoteMenuPageSize = 10;
    public ObservableCollection<int> QuoteMenuPageSizeOptions { get; } = new([10, 20, 50]);
    [ObservableProperty] private string serviceSearchText = string.Empty;
    [ObservableProperty] private string tableSearchText = string.Empty;
    [ObservableProperty] private string tableStatusFilter = "Tất cả";
    [ObservableProperty] private int openOrderTableColumnCount = 4;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    public ObservableCollection<string> TableStatusFilterOptions { get; } = new(["Tất cả", "Trống", "Đã đặt", "Đang phục vụ", "Khóa bàn"]);
    public ObservableCollection<int> OpenOrderTableColumnOptions { get; } = new([2, 3, 4]);
    [ObservableProperty] private MenuCategoryDto? selectedMenuCategory;
    private static MenuCategoryDto AllQuoteMenuCategory => new() { Id = Guid.Empty, Code = "ALL", Name = "T\u1EA5t c\u1EA3" };
    public bool IsMenuViewGrid => SelectedMenuViewMode == "Lưới";
    public bool IsMenuViewList => SelectedMenuViewMode == "Danh sách";
    public int QuoteMenuTotalCount => FilteredMenuItems.Count;
    public int QuoteMenuTotalPages => Math.Max(1, (int)Math.Ceiling(QuoteMenuTotalCount / (double)Math.Max(1, QuoteMenuPageSize)));
    public bool CanGoPreviousQuoteMenuPage => QuoteMenuPageIndex > 1;
    public bool CanGoNextQuoteMenuPage => QuoteMenuPageIndex < QuoteMenuTotalPages;
    public string QuoteMenuPageInfo => $"{QuoteMenuPageIndex}/{QuoteMenuTotalPages}";
    private List<MenuItemDto> _allMenuItems = [];
    private readonly List<PosMenuItemCard> _orderItemPickerFilteredMenuItems = [];
    private readonly List<AlacarteServiceItem> _orderItemPickerFilteredServices = [];
    private bool _syncingQuoteCapacity;

    public bool IsDashboardTab => SelectedTabIndex == 0;
    public bool IsRoomsTab => SelectedTabIndex == 1;
    public bool IsReservationsTab => SelectedTabIndex == 2;
    public bool IsOpenOrdersTab => SelectedTabIndex == 3;
    public bool IsContractsTab => SelectedTabIndex == 4;
    public bool IsSettingsTab => SelectedTabIndex == 5;
    [ObservableProperty] private int roomDetailTabIndex;
    public bool IsRoomDetailTab => RoomDetailTabIndex == 0;
    public ObservableCollection<DiningTableDto> SelectedRoomTables { get; } = new();
    public IEnumerable<TableReservationDto> SelectedRoomReservations
        => SelectedRoom is null ? Enumerable.Empty<TableReservationDto>()
            : Reservations.Where(r => r.DiningRoomId == SelectedRoom.Id).OrderBy(r => r.ReservationDateTime);
    public int SelectedRoomReservationCount => SelectedRoomReservations.Count();
    [ObservableProperty] private TableReservationDto? selectedReservation;
    public bool IsRoomTableDetailTab => RoomDetailTabIndex == 1;
    [ObservableProperty] private DateTime roomCalendarMonth = DateTime.Today;
    [ObservableProperty] private DateTime? selectedRoomCalendarDate;

    public IEnumerable<DateTime> RoomReservedDates
        => SelectedRoom is null ? Enumerable.Empty<DateTime>()
            : Reservations.Where(r => r.DiningRoomId == SelectedRoom.Id && r.ReservationDateTime.Month == RoomCalendarMonth.Month && r.ReservationDateTime.Year == RoomCalendarMonth.Year)
                .Select(r => r.ReservationDateTime.Date).Distinct();
    public int RoomReservedDateCount => RoomReservedDates.Count();

    public IEnumerable<TableReservationDto> SelectedDateReservations
        => SelectedRoom is null
            ? Enumerable.Empty<TableReservationDto>()
            : Reservations
                .Where(r => r.DiningRoomId == SelectedRoom.Id
                    && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(r.Status, "NoShow", StringComparison.OrdinalIgnoreCase)
                    && (SelectedRoomCalendarDate.HasValue
                        ? r.ReservationDateTime.Date == SelectedRoomCalendarDate.Value.Date
                        : r.ReservationDateTime.Date >= DateTime.Today))
                .OrderBy(r => r.ReservationDateTime);
    public int SelectedDateReservationCount => SelectedDateReservations.Count();
    public string SelectedRoomCalendarCaption
        => SelectedRoomCalendarDate.HasValue
            ? $"Lịch đặt ngày {SelectedRoomCalendarDate:dd/MM/yyyy}"
            : "Danh sách đặt phòng từ hôm nay trở đi";
    public string SelectedRoomCalendarHintText
        => SelectedRoomCalendarDate.HasValue
            ? "Danh sách bên dưới đang lọc theo ngày bạn chọn trên lịch."
            : "Danh sách bên dưới đang hiển thị toàn bộ booking từ ngày hiện tại trở về sau.";
    public string SelectedRoomCalendarEmptyText
        => SelectedRoomCalendarDate.HasValue
            ? "Chưa có đặt phòng cho ngày này."
            : "Chưa có booking nào từ hôm nay trở đi.";

    public IEnumerable<CalendarDay> RoomCalendarDays
    {
        get
        {
            var firstDay = new DateTime(RoomCalendarMonth.Year, RoomCalendarMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(RoomCalendarMonth.Year, RoomCalendarMonth.Month);
            var startPad = (int)firstDay.DayOfWeek;
            var days = new List<CalendarDay>();
            for (int i = 0; i < startPad; i++) days.Add(new CalendarDay { Day = 0, IsPadding = true });
            var reservedSet = new HashSet<DateTime>(RoomReservedDates);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(RoomCalendarMonth.Year, RoomCalendarMonth.Month, d);
                days.Add(new CalendarDay
                {
                    Day = d,
                    Date = date,
                    HasReservation = reservedSet.Contains(date),
                    IsToday = date.Date == DateTime.Today,
                    IsSelected = SelectedRoomCalendarDate.HasValue && date.Date == SelectedRoomCalendarDate.Value.Date
                });
            }
            return days;
        }
    }

    [RelayCommand]
    private void SelectRoomCalendarDate(CalendarDay? day)
    {
        if (day is null || day.Day == 0) return;
        SelectedRoomCalendarDate = day.Date;
    }

    [RelayCommand]
    private void PrevRoomCalendarMonth()
    {
        RoomCalendarMonth = RoomCalendarMonth.AddMonths(-1);
    }

    [RelayCommand]
    private void NextRoomCalendarMonth()
    {
        RoomCalendarMonth = RoomCalendarMonth.AddMonths(1);
    }

    partial void OnRoomCalendarMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(RoomCalendarDays));
        OnPropertyChanged(nameof(RoomReservedDates));
        OnPropertyChanged(nameof(RoomReservedDateCount));
        OnPropertyChanged(nameof(SelectedDateReservations));
        OnPropertyChanged(nameof(SelectedDateReservationCount));
        OnPropertyChanged(nameof(SelectedRoomCalendarCaption));
        OnPropertyChanged(nameof(SelectedRoomCalendarHintText));
        OnPropertyChanged(nameof(SelectedRoomCalendarEmptyText));
    }

    partial void OnSelectedRoomCalendarDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(RoomCalendarDays));
        OnPropertyChanged(nameof(SelectedDateReservations));
        OnPropertyChanged(nameof(SelectedDateReservationCount));
        OnPropertyChanged(nameof(SelectedRoomCalendarCaption));
        OnPropertyChanged(nameof(SelectedRoomCalendarHintText));
        OnPropertyChanged(nameof(SelectedRoomCalendarEmptyText));
    }

    private void RefreshRoomReservationCalendarState()
    {
        OnPropertyChanged(nameof(SelectedRoomReservations));
        OnPropertyChanged(nameof(SelectedRoomReservationCount));
        OnPropertyChanged(nameof(RoomReservedDates));
        OnPropertyChanged(nameof(RoomReservedDateCount));
        OnPropertyChanged(nameof(RoomCalendarDays));
        OnPropertyChanged(nameof(SelectedDateReservations));
        OnPropertyChanged(nameof(SelectedDateReservationCount));
        OnPropertyChanged(nameof(SelectedRoomCalendarCaption));
        OnPropertyChanged(nameof(SelectedRoomCalendarHintText));
        OnPropertyChanged(nameof(SelectedRoomCalendarEmptyText));
    }

    [ObservableProperty] private bool isOrderItemPickerCardView;
    [ObservableProperty] private bool isOrderItemPickerListView = true;

    [RelayCommand]
    private void SelectRoomReservation(TableReservationDto? res)
    {
        SelectedReservation = res;
        if (res is null)
        {
            return;
        }

        var quote = res.QuoteId.HasValue
            ? SavedQuotes.FirstOrDefault(x => x.Id == res.QuoteId.Value)
            : res.ApprovedOrderId.HasValue
                ? SavedQuotes.FirstOrDefault(x => x.ApprovedOrderId == res.ApprovedOrderId.Value)
                : null;
        if (quote is null)
        {
            StatusText = "Không tìm thấy báo giá liên kết cho lịch đặt này.";
            return;
        }

        QuoteSearchText = string.Empty;
        QuoteListTabIndex = ResolveQuoteListTabIndex(quote);
        SelectedSavedQuote = quote;
        ApplySavedQuoteDetails(quote);
        SelectedTabIndex = 4;
    }

    [RelayCommand]
    private void OpenDashboardRoomStatus(AlacarteDashboardRoomStatusDto? roomStatus)
    {
        if (roomStatus is null)
        {
            return;
        }

        SelectedRoom = Rooms.FirstOrDefault(x => x.Id == roomStatus.RoomId);
        SelectedTabIndex = 1;
        RoomDetailTabIndex = 0;
    }

    [RelayCommand]
    private void OpenDashboardServingOrder(AlacarteDashboardServingCardDto? card)
    {
        if (card is null)
        {
            return;
        }

        NavigateToOrder(card.OrderId);
    }

    [RelayCommand]
    private void OpenDashboardTimelineItem(AlacarteDashboardTimelineItemDto? item)
    {
        if (item is null)
        {
            return;
        }

        NavigateToReservation(item.ReservationId, item.QuoteId, item.ApprovedOrderId);
    }

    [RelayCommand]
    private void OpenDashboardAlert(AlacarteDashboardAlertDto? item)
    {
        if (item is null)
        {
            return;
        }

        NavigateDashboardTarget(item.TargetType, item.ReservationId, item.OrderId, item.QuoteId);
    }

    [RelayCommand]
    private void OpenDashboardActivity(AlacarteDashboardActivityDto? item)
    {
        if (item is null)
        {
            return;
        }

        NavigateDashboardTarget(item.TargetType, item.ReservationId, item.OrderId, item.QuoteId);
    }

    private void NavigateDashboardTarget(string? targetType, Guid? reservationId, Guid? orderId, Guid? quoteId)
    {
        if (string.Equals(targetType, "order", StringComparison.OrdinalIgnoreCase) && orderId.HasValue)
        {
            NavigateToOrder(orderId.Value);
            return;
        }

        if (string.Equals(targetType, "quote", StringComparison.OrdinalIgnoreCase) && (quoteId.HasValue || orderId.HasValue))
        {
            NavigateToQuote(quoteId, orderId);
            return;
        }

        if (string.Equals(targetType, "reservation", StringComparison.OrdinalIgnoreCase) && reservationId.HasValue)
        {
            NavigateToReservation(reservationId.Value, quoteId, orderId);
            return;
        }
    }

    private void NavigateToReservation(Guid reservationId, Guid? quoteId, Guid? orderId)
    {
        var reservation = Reservations.FirstOrDefault(x => x.Id == reservationId);
        if (reservation is not null)
        {
            SelectedReservation = reservation;
        }

        if (quoteId.HasValue || orderId.HasValue)
        {
            NavigateToQuote(quoteId, orderId);
            return;
        }

        if (reservation is not null && reservation.DiningRoomId.HasValue)
        {
            SelectedRoom = Rooms.FirstOrDefault(x => x.Id == reservation.DiningRoomId.Value);
        }

        SelectedTabIndex = 2;
    }

    private void NavigateToOrder(Guid orderId)
    {
        var order = OpenOrders.FirstOrDefault(x => x.Id == orderId);
        if (order is null)
        {
            StatusText = "Không tìm thấy đơn liên kết trên dashboard.";
            return;
        }

        SelectedOrder = order;
        SelectedOrderRoomFilter = null;
        ApplyOpenOrderFilter();
        SelectedTabIndex = 3;
        EnsureSelectedOpenOrder();
    }

    private void NavigateToQuote(Guid? quoteId, Guid? orderId)
    {
        var quote = quoteId.HasValue
            ? SavedQuotes.FirstOrDefault(x => x.Id == quoteId.Value)
            : orderId.HasValue
                ? SavedQuotes.FirstOrDefault(x => x.ApprovedOrderId == orderId.Value)
                : null;

        if (quote is null)
        {
            StatusText = "Không tìm thấy báo giá liên kết trên dashboard.";
            return;
        }

        QuoteSearchText = string.Empty;
        QuoteListTabIndex = ResolveQuoteListTabIndex(quote);
        SelectedSavedQuote = quote;
        ApplySavedQuoteDetails(quote);
        SelectedTabIndex = 4;
    }

    [RelayCommand]
    private void ToggleOrderItemPickerView(string? mode)
    {
        if (mode == "card") { IsOrderItemPickerCardView = true; IsOrderItemPickerListView = false; }
        else { IsOrderItemPickerCardView = false; IsOrderItemPickerListView = true; }
    }

    [RelayCommand]
    private void NavigateRoomDetailTab(string? index)
    {
        if (int.TryParse(index, out var i))
            RoomDetailTabIndex = i;
    }

    partial void OnRoomDetailTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsRoomDetailTab));
        OnPropertyChanged(nameof(IsRoomTableDetailTab));
    }

    public bool IsOrderFoodTab => OrderDetailTabIndex == 0;
    public bool IsOrderWineTab => OrderDetailTabIndex == 1;
    public bool IsOrderDrinkTab => OrderDetailTabIndex == 2;
    public bool IsOrderServiceTab => OrderDetailTabIndex == 3;
    public bool IsOrderInfoTab => OrderDetailTabIndex == 4;
    public bool IsOrderPrintHistoryTab => OrderDetailTabIndex == 5;
    public bool ShowOrderActionButtons => OrderDetailTabIndex <= 3;
    public bool IsOrderItemPickerFoodTab => OrderItemPickerTabIndex == 0;
    public bool IsOrderItemPickerServiceTab => OrderItemPickerTabIndex == 1;
    public bool IsOrderItemPickerSelectStep => OrderItemPickerStepIndex == 0;
    public bool IsOrderItemPickerPlanStep => OrderItemPickerStepIndex == 1;
    public bool IsOrderItemPickerSendStep => OrderItemPickerStepIndex == 2;
    public bool IsServingPlanOverviewTab => ServingPlanTabIndex == 0;
    public bool IsServingPlanTablesTab => ServingPlanTabIndex == 1;
    public bool IsServingPlanGroupsTab => ServingPlanTabIndex == 2;
    public bool HasSelectedOrderItemPickerMenuItem => SelectedOrderItemPickerMenuItem is not null;
    public bool IsOrderItemPickerPerGuestRule => string.Equals(SelectedOrderItemPickerServingRule, "PerGuest", StringComparison.OrdinalIgnoreCase);
    public bool IsOrderItemPickerPerTableRule => string.Equals(SelectedOrderItemPickerServingRule, "PerTable", StringComparison.OrdinalIgnoreCase);
    public bool IsOrderItemPickerPerTableWithExtraRule => string.Equals(SelectedOrderItemPickerServingRule, "PerTableWithExtraUnit", StringComparison.OrdinalIgnoreCase);
    public bool IsOrderItemPickerManualRule => string.Equals(SelectedOrderItemPickerServingRule, "Manual", StringComparison.OrdinalIgnoreCase);
    public string OrderItemPickerSelectedItemTitle
        => SelectedOrderItemPickerMenuItem is null
            ? "Chọn món từ danh sách bên trái"
            : $"{SelectedOrderItemPickerMenuItem.Name} · {SelectedOrderItemPickerMenuItem.UnitPrice:N0} đ";
    public string OrderItemPickerSelectedItemSubtitle
        => SelectedOrderItemPickerMenuItem is null
            ? "Phase 2: áp dụng món theo nhóm bàn, bàn chuẩn và bàn lẻ."
            : $"{SelectedOrderItemPickerMenuItem.CategoryName} · {SelectedOrderItemPickerMenuItem.UnitName}";
    public string OrderItemPickerPlanSummary
        => OrderItemPickerPlanRows.Count == 0
            ? "Chưa có bàn mục tiêu."
            : $"{OrderItemPickerPlanRows.Count} bàn · {OrderItemPickerPlanRows.Sum(x => x.SaveQuantity):N0} SL lưu · {OrderItemPickerPlanRows.Sum(x => x.DisplayQuantity):N0} SL hiển thị";
    public int PendingPreparationLineCount => SelectedOrder?.Lines?.Count(IsPendingPreparationLine) ?? 0;
    public int SentPreparationLineCount => SelectedOrder?.Lines?.Count(IsSentPreparationLine) ?? 0;
    public bool CanSendOrderToPreparation => SelectedOrder is not null && PendingPreparationLineCount > 0;
    [ObservableProperty] private bool isOrderFinalized;
    public bool CanPrintKitchen => IsOrderFinalized && CanSendOrderToPreparation;
    public string PreparationQueueSummary
        => SelectedOrder is null
            ? "Chọn đơn mở để soạn món và gửi bếp/bar."
            : PendingPreparationLineCount > 0
                ? $"{PendingPreparationLineCount} dòng món chờ gửi bếp/bar · {SentPreparationLineCount} dòng đã gửi."
                : SentPreparationLineCount > 0
                    ? "Đơn hiện không còn món chờ gửi. Các dòng hiện tại đã lên bếp/bar."
                    : "Đơn chưa có món để gửi bếp/bar.";
    public string SendPreparationButtonText
        => PendingPreparationLineCount > 0 ? $"Gửi bếp/bar ({PendingPreparationLineCount})" : "Gửi bếp/bar";
    public bool HasServingGroupSummaries => ServingGroupSummaries.Count > 0;
    public bool HasServingGroupEditorRows => ServingGroupEditorRows.Count > 0;
    public bool HasServingGroupSelectionRows => ServingGroupTableSelectionRows.Count > 0;
    public bool CanEditSelectedServingGroup => SelectedServingGroupEditor?.IsSystemDefined != true;
    public bool CanDeleteSelectedServingGroup => SelectedServingGroupEditor is not null && !SelectedServingGroupEditor.IsSystemDefined;
    public string SelectedServingGroupTableSummary
        => ServingGroupTableSelectionRows.Count(x => x.IsSelected) <= 0
            ? "Chưa chọn bàn nào cho nhóm phục vụ."
            : $"{ServingGroupTableSelectionRows.Count(x => x.IsSelected)} bàn · {ServingGroupTableSelectionRows.Where(x => x.IsSelected).Sum(x => x.GuestCount)} khách";
    public bool HasPreparationQueueRows => PreparationQueueRows.Count > 0;
    public bool HasSelectedOrderPreparationTickets => SelectedOrderPreparationTickets.Count > 0;
    public bool HasRoomServingPlan => RoomServingPlanRows.Count > 0;
    public int RoomServingPlanTotalGuests => RoomServingPlanRows.Where(x => x.IsServing).Sum(x => Math.Max(0, x.GuestCount));
    public int RoomServingPlanServingTableCount => RoomServingPlanRows.Count(x => x.IsServing);
    public int RoomServingPlanWaitingTableCount => RoomServingPlanRows.Count(x => !x.IsServing);
    public bool RoomServingPlanHasSoftWarnings => RoomServingPlanRows.Any(x => x.IsServing && x.IsOverSoftLimit);
    public string RoomServingPlanSummary
        => !HasRoomServingPlan
            ? "Chưa có cấu hình bàn để điều chỉnh."
            : $"{RoomServingPlanServingTableCount} bàn phục vụ · {RoomServingPlanWaitingTableCount} bàn chờ · {RoomServingPlanTotalGuests} khách";
    public string RoomServingPlanWarningText
        => !RoomServingPlanHasSoftWarnings
            ? "Các bàn đang trong ngưỡng phục vụ mềm."
            : "Có bàn đang vượt ngưỡng mềm, cần kiểm tra lại trước khi chốt món.";
    public string OrderItemPickerStepSummary
        => OrderItemPickerStepIndex switch
        {
            0 => "Bước 1: Chọn món hoặc dịch vụ cần áp dụng.",
            1 => "Bước 2: Xem trước nhóm bàn, quy tắc số lượng và lưu xuống đơn.",
            2 => "Bước 3: Kiểm tra các dòng chờ và gửi bếp/bar theo đợt.",
            _ => "Bắt đầu quy trình thêm món theo nhóm bàn."
        };
    public int OrderItemPickerMenuTotalCount => _orderItemPickerFilteredMenuItems.Count;
    public int OrderItemPickerMenuTotalPages => Math.Max(1, (int)Math.Ceiling(OrderItemPickerMenuTotalCount / (double)Math.Max(1, OrderItemPickerMenuPageSize)));
    public bool CanGoPreviousOrderItemPickerMenuPage => OrderItemPickerMenuPageIndex > 1;
    public bool CanGoNextOrderItemPickerMenuPage => OrderItemPickerMenuPageIndex < OrderItemPickerMenuTotalPages;
    public string OrderItemPickerMenuPageInfo => $"{OrderItemPickerMenuPageIndex}/{OrderItemPickerMenuTotalPages}";
    public string OrderItemPickerMenuRangeSummary
    {
        get
        {
            if (OrderItemPickerMenuTotalCount <= 0)
            {
                return "Hiển thị 0 của 0 món";
            }

            var pageSize = Math.Max(1, OrderItemPickerMenuPageSize);
            var start = ((Math.Max(1, OrderItemPickerMenuPageIndex) - 1) * pageSize) + 1;
            var end = Math.Min(OrderItemPickerMenuTotalCount, start + pageSize - 1);
            return $"Hiển thị {start}-{end} của {OrderItemPickerMenuTotalCount} món";
        }
    }
    public int OrderItemPickerServiceTotalCount => _orderItemPickerFilteredServices.Count;
    public int OrderItemPickerServiceTotalPages => Math.Max(1, (int)Math.Ceiling(OrderItemPickerServiceTotalCount / (double)Math.Max(1, OrderItemPickerServicePageSize)));
    public bool CanGoPreviousOrderItemPickerServicePage => OrderItemPickerServicePageIndex > 1;
    public bool CanGoNextOrderItemPickerServicePage => OrderItemPickerServicePageIndex < OrderItemPickerServiceTotalPages;
    public string OrderItemPickerServicePageInfo => $"{OrderItemPickerServicePageIndex}/{OrderItemPickerServiceTotalPages}";
    public string OrderItemPickerServiceRangeSummary
    {
        get
        {
            if (OrderItemPickerServiceTotalCount <= 0)
            {
                return "Hiển thị 0 của 0 dịch vụ";
            }

            var pageSize = Math.Max(1, OrderItemPickerServicePageSize);
            var start = ((Math.Max(1, OrderItemPickerServicePageIndex) - 1) * pageSize) + 1;
            var end = Math.Min(OrderItemPickerServiceTotalCount, start + pageSize - 1);
            return $"Hiển thị {start}-{end} của {OrderItemPickerServiceTotalCount} dịch vụ";
        }
    }
    public IEnumerable<DiningTableDto> SelectedOrderRoomTables => BuildSelectedOrderRoomTables();
    public DiningTableDto? SelectedOrderSummaryTable => SelectedOrderRoomTables.FirstOrDefault(t => t.Id == Guid.Empty);
    public IEnumerable<DiningTableDto> SelectedOrderDetailTables => SelectedOrderRoomTables.Where(t => t.Id != Guid.Empty);
    public int SelectedOrderRoomTableCount => GetSelectedOrderServingTables().Count;
    public string SelectedOrderTableDisplayName
        => SelectedTable?.Name
           ?? SelectedOrder?.TableName
           ?? GetSelectedOrderServingTables().FirstOrDefault()?.Name
           ?? "Chọn bàn";
    public string SelectedOrderRoomDisplayName
        => SelectedOrder?.RoomName
           ?? SelectedTable?.RoomName
           ?? GetSelectedOrderServingTables().FirstOrDefault()?.RoomName
           ?? "—";
    public string SelectedOrderAreaDisplayName
        => Rooms.FirstOrDefault(x => x.Id == SelectedOrder?.DiningRoomId)?.RoomType
           ?? Rooms.FirstOrDefault(x => string.Equals(x.Name, SelectedOrderRoomDisplayName, StringComparison.OrdinalIgnoreCase))?.RoomType
           ?? "—";
    public string SelectedOrderSingleTableOrderNo
        => SelectedOrder?.OrderNo ?? string.Empty;
    public IReadOnlyList<RestaurantQuoteTableDto> CurrentQuoteTables => SelectedSavedQuote is null ? Array.Empty<RestaurantQuoteTableDto>() : SelectedSavedQuote.Tables;
    public int RoomSetupMaxTables
        => Math.Max(1, Tables.Count(x => x.DiningRoomId == (RoomSetupRoom?.Id ?? Guid.Empty)));

    // Services sub-tab
    public bool IsServicesFoodTab => ServicesSubTabIndex == 0;
    public bool IsServicesSvcTab => ServicesSubTabIndex == 1;

    // Settings sub-tabs
    public bool IsSettingsArea => SettingsSubTabIndex == 0;
    public bool IsSettingsRooms => SettingsSubTabIndex == 0;
    public bool IsSettingsTables => SettingsSubTabIndex == 1;
    public bool IsSettingsPrinters => SettingsSubTabIndex == 2;
    public bool IsSettingsMenuGroups => SettingsSubTabIndex == 3;
    public bool IsSettingsMenuItems => SettingsSubTabIndex == 4;
    public bool IsSettingsExtServices => SettingsSubTabIndex == 5;
    public bool IsSettingsPayment => SettingsSubTabIndex == 6;
    public bool IsSettingsReservation => SettingsSubTabIndex == 7;
    public bool IsSettingsDocuments => SettingsSubTabIndex == 8;
    public bool IsSettingsPermissions => SettingsSubTabIndex == 9;
    public bool IsSettingsIntegration => SettingsSubTabIndex == 10;
    public bool IsSettingsRestaurant => SettingsSubTabIndex == 11;
    public bool IsSettingsServingGroups => SettingsSubTabIndex == 12;

    // Settings sample collections
    public ObservableCollection<AlacarteSettingDiningArea> SettingDiningAreas { get; } = new();
    public ObservableCollection<AlacarteSettingArea> SettingAreas { get; } = new();
    public ObservableCollection<AlacarteSettingTable> SettingTables { get; } = new();
    public ObservableCollection<AlacarteSettingPrinter> SettingPrinters { get; } = new();
    public ObservableCollection<string> AvailablePrinterNames { get; } = new();
    public ObservableCollection<string> PrinterPaperSizeOptions { get; } = new(["58mm", "80mm", "A4"]);
    public ObservableCollection<AlacarteSettingMenuGroup> SettingMenuGroups { get; } = new();
    public ObservableCollection<AlacarteSettingMenuItem> SettingMenuItems { get; } = new();
    public ObservableCollection<AlacarteServiceItem> SettingExtServices { get; } = new();
    public ObservableCollection<AlacarteCategorySaleSelection> SettingCategorySelections { get; } = new();
    public ObservableCollection<AlacarteSettingPaymentMethod> SettingPaymentMethods { get; } = new();
    public ObservableCollection<AlacarteSettingPermission> SettingPermissions { get; } = new();
    public ObservableCollection<AlacarteSettingIntegration> SettingIntegrations { get; } = new();
    public ObservableCollection<string> SettingMenuItemTypeOptions { get; } = new(["Đồ ăn", "Rượu", "Đồ uống", "Dịch vụ", "Khác"]);
    public ObservableCollection<string> SettingServiceTypeOptions { get; } = new(["Thiết bị / sân khấu", "Nhân sự biểu diễn", "Trang trí", "Phụ thu / phụ phí", "Khác"]);
    public bool HasRooms => Rooms.Count > 0;
    public bool HasTables => Tables.Count > 0;
    public bool HasReservations => Reservations.Count > 0;
    public bool HasOpenOrders => OpenOrders.Count > 0;
    public bool HasContracts => Contracts.Count > 0;
    public bool HasServices => AlacarteServices.Count > 0;

    public IEnumerable<AlacarteSettingArea> FilteredSettingRooms => SettingAreas;

    public IEnumerable<AlacarteSettingTable> FilteredSettingTables
        => EditingSettingAreaId.HasValue
            ? SettingTables.Where(x => x.DiningRoomId == EditingSettingAreaId.Value)
            : !string.IsNullOrWhiteSpace(SelectedSettingDiningAreaName)
                ? SettingTables.Where(x => string.Equals(x.AreaName, SelectedSettingDiningAreaName, StringComparison.OrdinalIgnoreCase))
                : SettingTables;

    // Settings form fields
    [ObservableProperty] private string settingDiningAreaName = string.Empty;
    [ObservableProperty] private string? editingSettingDiningAreaName;
    [ObservableProperty] private string selectedSettingDiningAreaName = string.Empty;
    [ObservableProperty] private string settingAreaCode = string.Empty;
    [ObservableProperty] private string settingAreaName = string.Empty;
    [ObservableProperty] private string settingAreaType = "Standard";
    [ObservableProperty] private int settingAreaCapacity = 40;
    [ObservableProperty] private bool settingAreaIsActive = true;
    [ObservableProperty] private Guid? editingSettingAreaId;
    [ObservableProperty] private string settingTableCode = string.Empty;
    [ObservableProperty] private string settingTableName = string.Empty;
    [ObservableProperty] private int settingTableSeats = 4;
    [ObservableProperty] private Guid? settingTableRoomId;
    [ObservableProperty] private string kitchenPrinterName = "Kitchen Printer";
    [ObservableProperty] private string kitchenPaperSize = "80mm";
    [ObservableProperty] private string barPrinterName = "Bar Printer";
    [ObservableProperty] private string barPaperSize = "80mm";
    [ObservableProperty] private string receiptPrinterName = "Receipt Printer";
    [ObservableProperty] private string receiptPaperSize = "80mm";
    public string KitchenPrintTestSummary => "Phiếu bếp mẫu gồm thông tin phòng, bàn, món chính, món phần bù và ghi chú bếp để test máy in bếp thật.";
    public string ReceiptPrintTestSummary => "Phiếu thanh toán mẫu gồm khách, món, số lượng, đơn giá, VAT và tổng cộng để test máy in hóa đơn thật.";
    [ObservableProperty] private string settingTableRoomName = string.Empty;
    [ObservableProperty] private Guid? editingSettingTableId;
    [ObservableProperty] private Guid? editingSettingMenuGroupId;
    [ObservableProperty] private string settingMenuGroupCode = string.Empty;
    [ObservableProperty] private string settingMenuGroupName = string.Empty;
    [ObservableProperty] private int settingMenuGroupSortOrder = 100;
    [ObservableProperty] private Guid? editingSettingMenuItemId;
    [ObservableProperty] private Guid? settingMenuItemCategoryId;
    [ObservableProperty] private string settingMenuItemCode = string.Empty;
    [ObservableProperty] private string settingMenuItemName = string.Empty;
    [ObservableProperty] private decimal settingMenuItemPrice;
    [ObservableProperty] private decimal settingMenuItemVatRate = 8m;
    [ObservableProperty] private string settingMenuItemStation = "Bếp";
    [ObservableProperty] private string settingMenuItemType = "Đồ ăn";
    [ObservableProperty] private bool settingMenuItemHasRecipe = true;
    [ObservableProperty] private bool settingMenuItemIsActive = true;
    [ObservableProperty] private Guid? editingSettingExtServiceId;
    [ObservableProperty] private string settingExtServiceCode = string.Empty;
    [ObservableProperty] private string settingExtServiceName = string.Empty;
    [ObservableProperty] private decimal settingExtServicePrice;
    [ObservableProperty] private decimal settingExtServiceVatRate;
    [ObservableProperty] private bool settingExtServiceVatIncluded = true;
    [ObservableProperty] private bool settingExtServiceIsActive = true;
    [ObservableProperty] private string settingExtServiceUnit = "lần";
    [ObservableProperty] private string settingExtServiceType = "Khác";
    [ObservableProperty] private string settingTableGenPrefix = "A";
    [ObservableProperty] private int settingTableGenCount = 10;
    [ObservableProperty] private bool settingEnableCash = true;
    [ObservableProperty] private bool settingEnableTransfer = true;
    [ObservableProperty] private bool settingEnableCard = true;
    [ObservableProperty] private bool settingEnableRoomCharge = true;
    [ObservableProperty] private bool settingEnableDebt = true;
    [ObservableProperty] private bool settingEnableEWallet;
    [ObservableProperty] private bool settingEnableAutoVat = true;
    [ObservableProperty] private int settingAutoCancelMinutes = 30;
    [ObservableProperty] private bool settingAllowMultiDeposit;
    [ObservableProperty] private bool settingAutoCancelNoShow = true;
    [ObservableProperty] private bool settingPrintLogo = true;
    [ObservableProperty] private bool settingPrintVat = true;
    [ObservableProperty] private int settingPrintCopies = 2;
    [ObservableProperty] private bool settingSaved;

    // Right panel visibility helpers
    public bool HasSelectedOrder => SelectedOrder is not null;
    public bool HasSelectedRoom => SelectedRoom is not null;
    public bool IsSummaryTableContext => IsSummaryTableSelected();
    public bool HasRoomNoOrder => SelectedRoom is not null && SelectedOrder is null;
    public bool ShowOrderPanel => IsRoomsTab || IsOpenOrdersTab;
    public bool IsOrderPanelWithOrder => IsOpenOrdersTab && HasSelectedOrder;

    public AlacarteRestaurantViewModel(IAlacarteRestaurantService service, IQuoteChangeService? quoteChangeService = null, IMenuCatalogService? menuCatalog = null, IMenuRecipeService? recipeService = null, ICurrentUserService? currentUser = null, ICashFundService? cashFundService = null, IRoomOrderMenuPlanningEngine? roomOrderMenuPlanningEngine = null)
    {
        _service = service;
        _quoteChangeService = quoteChangeService!;
        _menuCatalog = menuCatalog;
        _recipeService = recipeService;
        _currentUser = currentUser;
        _cashFundService = cashFundService;
        _roomOrderMenuPlanningEngine = roomOrderMenuPlanningEngine ?? new RoomOrderMenuPlanningEngine();
        _dashboardRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _dashboardRefreshTimer.Tick += async (_, _) => await RefreshDashboardTimerAsync();
        RefreshAvailablePrinters();
        LoadRestaurantSettings();
        QuoteLines.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(QuoteSubtotal)); OnPropertyChanged(nameof(QuoteVat)); OnPropertyChanged(nameof(QuoteTotalPerTable)); };
        InitSettingsReferenceData();
        _dashboardRefreshTimer.Start();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await RefreshAsync();
        await LoadSavedQuotesAsync();
        // Nếu DB trống, tự động tạo dữ liệu mẫu
        if (Rooms.Count == 0 && Tables.Count == 0)
        {
            var services = await _service.SeedDefaultExtraServicesAsync();
            Reset(AlacarteServices, services.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.DefaultPrice, "lần", x.Code, x.VatRate)));
            Reset(SettingExtServices, AlacarteServices.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.Price, x.Unit, x.Code, x.VatRate)));
            NotifyCollectionState();
        }
        await LoadMenuItemsAsync();
        await LoadMenuPackagesAsync();
    }

    private async Task RefreshDashboardTimerAsync()
    {
        if (_isAutoRefreshingDashboard || IsBusy)
        {
            return;
        }

        try
        {
            _isAutoRefreshingDashboard = true;
            await RefreshAsync();
        }
        finally
        {
            _isAutoRefreshingDashboard = false;
        }
    }

    private void SyncDashboardVisualState()
    {
        Reset(DashboardRevenueSeries, Dashboard.RevenueChart.Select(x => new MetricPoint
        {
            Label = x.Label,
            Value = x.Value
        }));
        OnPropertyChanged(nameof(Dashboard));
    }

    private async Task LoadMenuPackagesAsync()
    {
        try
        {
            Reset(MenuPackages, await _service.GetMenuPackagesAsync());
        }
        catch
        {
            MenuPackages.Clear();
        }
    }

    private async Task LoadMenuItemsAsync()
    {
        if (_menuCatalog is null) return;
        try
        {
            var categories = await _menuCatalog.GetCategoriesAsync();
            Reset(MenuCategories, categories);
            Reset(QuoteMenuCategories, new[] { AllQuoteMenuCategory }.Concat(categories));
            SelectedMenuCategory ??= QuoteMenuCategories.FirstOrDefault();
            var items = await _menuCatalog.GetMenuItemsAsync(includeInactive: true);
            SyncSettingMenuGroups(categories, items);
            SyncSettingMenuItems(items);
            ResetFilteredMenu(items);
        }
        catch { /* Fallback to sample if DB not available */ }
    }

    private void ResetFilteredMenu(IEnumerable<MenuItemDto> items)
    {
        _allMenuItems = items.ToList();
        ApplyMenuFilters();
    }

    private void SyncSettingMenuGroups(IReadOnlyList<MenuCategoryDto> categories, IReadOnlyList<MenuItemDto> items)
    {
        var allowedIds = GetAllowedMenuCategoryIds();
        Reset(SettingMenuGroups, categories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(category => new AlacarteSettingMenuGroup(
                category.Id,
                category.Code,
                category.Name,
                category.SortOrder,
                items.Count(x => x.MenuCategoryId == category.Id),
                allowedIds.Count == 0 || allowedIds.Contains(category.Id))));

        Reset(SettingCategorySelections, categories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(category => new AlacarteCategorySaleSelection(
                category.Id,
                category.Code,
                category.Name,
                items.Count(x => x.MenuCategoryId == category.Id),
                allowedIds.Count == 0 || allowedIds.Contains(category.Id))));
    }

    private void SyncSettingMenuItems(IReadOnlyList<MenuItemDto> items)
    {
        Reset(SettingMenuItems, items
            .OrderBy(x => x.CategoryName)
            .ThenBy(x => x.Name)
            .Select(item => new AlacarteSettingMenuItem(
                item.Id,
                item.MenuCategoryId,
                item.Code,
                item.Name,
                item.CategoryName,
                item.UnitPrice,
                (int)Math.Round(item.VatRate * 100m, MidpointRounding.AwayFromZero),
                item.PreparationArea ?? string.Empty,
                item.HasRecipeManagement || item.HasRecipe,
                item.IsActive,
                ResolveConfiguredMenuItemType(item))));
    }

    private HashSet<Guid> GetAllowedMenuCategoryIds()
    {
        if (SettingCategorySelections.Count > 0)
        {
            return SettingCategorySelections
                .Where(x => x.IsEnabled)
                .Select(x => x.CategoryId)
                .ToHashSet();
        }

        return new HashSet<Guid>(_allowedMenuCategoryIds);
    }

    private string ResolveConfiguredMenuItemType(MenuItemDto item)
    {
        if (_menuItemTypeOverrides.TryGetValue(item.Id, out var configuredType) && !string.IsNullOrWhiteSpace(configuredType))
        {
            return configuredType;
        }

        return InferMenuItemType(item);
    }

    private string InferMenuItemType(MenuItemDto item)
    {
        if (item.IsHotelService)
        {
            return "Dịch vụ";
        }

        if (MatchesWineCategory(item.CategoryName, item.Name, item.Code))
        {
            return "Rượu";
        }

        if (MatchesDrinkCategory(item.CategoryName, item.Name, item.Code))
        {
            return "Đồ uống";
        }

        return "Đồ ăn";
    }

    private static string NormalizeServiceTypeKey(string? value)
        => value?.Trim() switch
        {
            "Thiết bị / sân khấu" => "Equipment",
            "Nhân sự biểu diễn" => "Staff",
            "Trang trí" => "Decor",
            "Phụ thu / phụ phí" => "Fee",
            "Khác" => "Other",
            "Equipment" => "Equipment",
            "Staff" => "Staff",
            "Decor" => "Decor",
            "Fee" => "Fee",
            _ => "Other"
        };

    private static string NormalizeServiceTypeLabel(string? value)
        => value?.Trim() switch
        {
            "Equipment" => "Thiết bị / sân khấu",
            "Staff" => "Nhân sự biểu diễn",
            "Decor" => "Trang trí",
            "Fee" => "Phụ thu / phụ phí",
            "Other" => "Khác",
            "Thiết bị / sân khấu" => "Thiết bị / sân khấu",
            "Nhân sự biểu diễn" => "Nhân sự biểu diễn",
            "Trang trí" => "Trang trí",
            "Phụ thu / phụ phí" => "Phụ thu / phụ phí",
            _ => "Khác"
        };

    private void ApplyMenuFilters()
    {
        var allowedIds = GetAllowedMenuCategoryIds();
        var query = _allMenuItems
            .Where(x => allowedIds.Count == 0 || allowedIds.Contains(x.MenuCategoryId))
            .AsEnumerable();
        if (!string.IsNullOrWhiteSpace(MenuSearchText))
            query = query.Where(x => x.Name.Contains(MenuSearchText, StringComparison.OrdinalIgnoreCase)
                                  || x.Code.Contains(MenuSearchText, StringComparison.OrdinalIgnoreCase));
        if (SelectedMenuCategory is not null && SelectedMenuCategory.Id != Guid.Empty)
            query = query.Where(x => x.MenuCategoryId == SelectedMenuCategory.Id);
        query = SelectedMenuSortMode switch
        {
            "Giá tăng dần" => query.OrderBy(x => x.UnitPrice),
            "Giá giảm dần" => query.OrderByDescending(x => x.UnitPrice),
            "Nhóm món" => query.OrderBy(x => x.CategoryName).ThenBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };
        FilteredMenuItems.Clear();
        foreach (var item in query) FilteredMenuItems.Add(new PosMenuItemCard(item));
        RefreshOrderItemPickerCategories();
        ApplyOrderItemPickerFilters();
        if (QuoteMenuPageIndex > QuoteMenuTotalPages) QuoteMenuPageIndex = QuoteMenuTotalPages;
        RefreshQuoteMenuPage();
    }

    partial void OnMenuSearchTextChanged(string value) { QuoteMenuPageIndex = 1; ApplyMenuFilters(); }
    partial void OnOrderItemPickerSearchTextChanged(string value)
    {
        OrderItemPickerMenuPageIndex = 1;
        OrderItemPickerServicePageIndex = 1;
        ApplyOrderItemPickerFilters();
    }
    partial void OnSelectedOrderItemPickerCategoryChanged(string value)
    {
        OrderItemPickerMenuPageIndex = 1;
        ApplyOrderItemPickerFilters();
    }
    partial void OnSelectedOrderItemPickerMenuItemChanged(PosMenuItemCard? value)
    {
        if (value is not null)
        {
            OrderItemPickerBaseQuantity = 1m;
            OrderItemPickerExtraUnitQuantity = 1m;
            OrderItemPickerManualQuantity = 1m;
        }

        RefreshOrderItemPickerPlan();
        OnPropertyChanged(nameof(HasSelectedOrderItemPickerMenuItem));
        OnPropertyChanged(nameof(OrderItemPickerSelectedItemTitle));
        OnPropertyChanged(nameof(OrderItemPickerSelectedItemSubtitle));
    }
    partial void OnSelectedOrderItemPickerScopeChanged(string value) => RefreshOrderItemPickerPlan();
    partial void OnSelectedOrderItemPickerServingRuleChanged(string value)
    {
        RefreshOrderItemPickerPlan();
        OnPropertyChanged(nameof(IsOrderItemPickerPerGuestRule));
        OnPropertyChanged(nameof(IsOrderItemPickerPerTableRule));
        OnPropertyChanged(nameof(IsOrderItemPickerPerTableWithExtraRule));
        OnPropertyChanged(nameof(IsOrderItemPickerManualRule));
    }
    partial void OnOrderItemPickerBaseQuantityChanged(decimal value) => RefreshOrderItemPickerPlan();
    partial void OnOrderItemPickerExtraUnitQuantityChanged(decimal value) => RefreshOrderItemPickerPlan();
    partial void OnOrderItemPickerManualQuantityChanged(decimal value) => RefreshOrderItemPickerPlan();
    partial void OnOrderItemPickerMenuPageSizeChanged(int value)
    {
        OrderItemPickerMenuPageIndex = 1;
        RefreshOrderItemPickerMenuPage();
    }
    partial void OnOrderItemPickerMenuPageIndexChanged(int value) => RefreshOrderItemPickerMenuPage();
    partial void OnOrderItemPickerServicePageSizeChanged(int value)
    {
        OrderItemPickerServicePageIndex = 1;
        RefreshOrderItemPickerServicePage();
    }
    partial void OnOrderItemPickerServicePageIndexChanged(int value) => RefreshOrderItemPickerServicePage();
    partial void OnServingPlanTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsServingPlanOverviewTab));
        OnPropertyChanged(nameof(IsServingPlanTablesTab));
        OnPropertyChanged(nameof(IsServingPlanGroupsTab));
    }
    partial void OnSelectedServingGroupEditorChanged(ServingGroupEditorRow? value)
    {
        if (value is null)
        {
            ServingGroupEditorName = string.Empty;
            ServingGroupEditorServingRule = "PerTable";
            ServingGroupEditorNote = string.Empty;
            foreach (var row in ServingGroupTableSelectionRows)
            {
                row.IsSelected = false;
            }
        }
        else
        {
            ServingGroupEditorName = value.Name;
            ServingGroupEditorServingRule = string.IsNullOrWhiteSpace(value.ServingRule) ? "PerTable" : value.ServingRule;
            ServingGroupEditorNote = value.Note ?? string.Empty;
            ApplyServingGroupSelection(value);
        }

        OnPropertyChanged(nameof(CanEditSelectedServingGroup));
        OnPropertyChanged(nameof(CanDeleteSelectedServingGroup));
        OnPropertyChanged(nameof(SelectedServingGroupTableSummary));
    }
    partial void OnSelectedMenuSortModeChanged(string value) { QuoteMenuPageIndex = 1; ApplyMenuFilters(); }
    partial void OnSelectedMenuCategoryChanged(MenuCategoryDto? value) { QuoteMenuPageIndex = 1; ApplyMenuFilters(); }
    partial void OnQuoteMenuPageSizeChanged(int value)
    {
        QuoteMenuPageIndex = 1;
        RefreshQuoteMenuPage();
    }
    partial void OnQuoteMenuPageIndexChanged(int value) => RefreshQuoteMenuPage();
    partial void OnSelectedMenuViewModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsMenuViewGrid));
        OnPropertyChanged(nameof(IsMenuViewList));
    }

    private void RefreshQuoteMenuPage()
    {
        var pageSize = Math.Max(1, QuoteMenuPageSize);
        var totalPages = QuoteMenuTotalPages;
        if (QuoteMenuPageIndex < 1)
        {
            QuoteMenuPageIndex = 1;
            return;
        }
        if (QuoteMenuPageIndex > totalPages)
        {
            QuoteMenuPageIndex = totalPages;
            return;
        }

        PagedQuoteMenuItems.Clear();
        foreach (var item in FilteredMenuItems.Skip((QuoteMenuPageIndex - 1) * pageSize).Take(pageSize))
            PagedQuoteMenuItems.Add(item);

        OnPropertyChanged(nameof(QuoteMenuTotalCount));
        OnPropertyChanged(nameof(QuoteMenuTotalPages));
        OnPropertyChanged(nameof(CanGoPreviousQuoteMenuPage));
        OnPropertyChanged(nameof(CanGoNextQuoteMenuPage));
        OnPropertyChanged(nameof(QuoteMenuPageInfo));
    }

    private void RefreshOrderItemPickerCategories()
    {
        var current = string.IsNullOrWhiteSpace(SelectedOrderItemPickerCategory) ? "Tất cả" : SelectedOrderItemPickerCategory;
        var categories = _allMenuItems
            .Select(x => x.CategoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        OrderItemPickerCategories.Clear();
        OrderItemPickerCategories.Add("Tất cả");
        foreach (var category in categories)
        {
            OrderItemPickerCategories.Add(category);
        }

        if (!OrderItemPickerCategories.Contains(current))
        {
            SelectedOrderItemPickerCategory = "Tất cả";
        }
    }

    private void ApplyOrderItemPickerFilters()
    {
        var search = OrderItemPickerSearchText?.Trim() ?? string.Empty;
        var category = string.IsNullOrWhiteSpace(SelectedOrderItemPickerCategory) ? "Tất cả" : SelectedOrderItemPickerCategory;
        var currentSelectedItemId = SelectedOrderItemPickerMenuItem?.Id;

        IEnumerable<MenuItemDto> menuQuery = _allMenuItems;
        if (!string.IsNullOrWhiteSpace(search))
        {
            menuQuery = menuQuery.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(category, "Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            menuQuery = menuQuery.Where(x => string.Equals(x.CategoryName, category, StringComparison.OrdinalIgnoreCase));
        }

        _orderItemPickerFilteredMenuItems.Clear();
        _orderItemPickerFilteredMenuItems.AddRange(menuQuery
            .OrderBy(x => x.CategoryName)
            .ThenBy(x => x.Name)
            .Select(x => new PosMenuItemCard(x)));

        IEnumerable<AlacarteServiceItem> serviceQuery = AlacarteServices;
        if (!string.IsNullOrWhiteSpace(search))
        {
            serviceQuery = serviceQuery.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Unit.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        _orderItemPickerFilteredServices.Clear();
        _orderItemPickerFilteredServices.AddRange(serviceQuery.OrderBy(x => x.Name));

        RefreshOrderItemPickerMenuPage();
        RefreshOrderItemPickerServicePage();

        if (currentSelectedItemId.HasValue)
        {
            SelectedOrderItemPickerMenuItem = _orderItemPickerFilteredMenuItems.FirstOrDefault(x => x.Id == currentSelectedItemId.Value)
                ?? OrderItemPickerMenuItems.FirstOrDefault()
                ?? _orderItemPickerFilteredMenuItems.FirstOrDefault();
        }
        else if (SelectedOrderItemPickerMenuItem is null)
        {
            SelectedOrderItemPickerMenuItem = OrderItemPickerMenuItems.FirstOrDefault()
                ?? _orderItemPickerFilteredMenuItems.FirstOrDefault();
        }

        RefreshOrderItemPickerPlan();
    }

    private void RefreshOrderItemPickerMenuPage()
    {
        var pageSize = Math.Max(1, OrderItemPickerMenuPageSize);
        var totalPages = OrderItemPickerMenuTotalPages;
        if (OrderItemPickerMenuPageIndex < 1)
        {
            OrderItemPickerMenuPageIndex = 1;
            return;
        }

        if (OrderItemPickerMenuPageIndex > totalPages)
        {
            OrderItemPickerMenuPageIndex = totalPages;
            return;
        }

        OrderItemPickerMenuItems.Clear();
        foreach (var item in _orderItemPickerFilteredMenuItems.Skip((OrderItemPickerMenuPageIndex - 1) * pageSize).Take(pageSize))
        {
            OrderItemPickerMenuItems.Add(item);
        }

        OnPropertyChanged(nameof(OrderItemPickerMenuTotalCount));
        OnPropertyChanged(nameof(OrderItemPickerMenuTotalPages));
        OnPropertyChanged(nameof(CanGoPreviousOrderItemPickerMenuPage));
        OnPropertyChanged(nameof(CanGoNextOrderItemPickerMenuPage));
        OnPropertyChanged(nameof(OrderItemPickerMenuPageInfo));
        OnPropertyChanged(nameof(OrderItemPickerMenuRangeSummary));
    }

    private void RefreshOrderItemPickerServicePage()
    {
        var pageSize = Math.Max(1, OrderItemPickerServicePageSize);
        var totalPages = OrderItemPickerServiceTotalPages;
        if (OrderItemPickerServicePageIndex < 1)
        {
            OrderItemPickerServicePageIndex = 1;
            return;
        }

        if (OrderItemPickerServicePageIndex > totalPages)
        {
            OrderItemPickerServicePageIndex = totalPages;
            return;
        }

        OrderItemPickerServices.Clear();
        foreach (var service in _orderItemPickerFilteredServices.Skip((OrderItemPickerServicePageIndex - 1) * pageSize).Take(pageSize))
        {
            OrderItemPickerServices.Add(service);
        }

        OnPropertyChanged(nameof(OrderItemPickerServiceTotalCount));
        OnPropertyChanged(nameof(OrderItemPickerServiceTotalPages));
        OnPropertyChanged(nameof(CanGoPreviousOrderItemPickerServicePage));
        OnPropertyChanged(nameof(CanGoNextOrderItemPickerServicePage));
        OnPropertyChanged(nameof(OrderItemPickerServicePageInfo));
        OnPropertyChanged(nameof(OrderItemPickerServiceRangeSummary));
    }

    private void RefreshOrderItemPickerPlan()
    {
        OrderItemPickerPlanRows.Clear();
        if (SelectedOrderItemPickerMenuItem is null)
        {
            OnPropertyChanged(nameof(OrderItemPickerPlanSummary));
            return;
        }

        var roomTables = GetSelectedOrderServingTables()
            .Where(t => string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(t.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
            .Select(t => new RoomOrderMenuTableInput
            {
                TableId = t.OrderTableId ?? t.Id,
                TableName = t.Name,
                GuestCount = Math.Max(0, t.SeatCount),
                Status = t.Status
            })
            .ToList();
        var selectedTableId = SelectedTable is not null && SelectedTable.Id != Guid.Empty
            ? SelectedTable.OrderTableId ?? SelectedTable.Id
            : roomTables.FirstOrDefault()?.TableId;

        var plan = _roomOrderMenuPlanningEngine.BuildPlan(new RoomOrderMenuPlanningRequest
        {
            Tables = roomTables,
            ApplyScope = SelectedOrderItemPickerScope,
            ServingRule = SelectedOrderItemPickerServingRule,
            TableStandard = GetSelectedOrderTableStandard(),
            BaseQuantity = OrderItemPickerBaseQuantity,
            ExtraUnitQuantity = OrderItemPickerExtraUnitQuantity,
            ManualQuantity = OrderItemPickerManualQuantity,
            SelectedTableId = selectedTableId
        });

        foreach (var row in plan)
        {
            var allLines = SelectedOrder?.Lines ?? new List<AlacarteOrderLineDto>();
            var existingLines = allLines
                .Where(l => l.OrderTableId == row.TableId && !IsOrderServiceLine(l))
                .Select(l => $"{l.MenuItemName} x{l.Qty:N0}")
                .ToList();
            var display = existingLines.Count > 0
                ? string.Join("  ·  ", existingLines)
                : "Chưa có món";

            OrderItemPickerPlanRows.Add(new OrderItemPickerPlanRow
            {
                TableId = row.TableId,
                OrderTableId = row.TableId,
                TableName = row.TableName,
                GuestCount = row.GuestCount,
                SaveQuantity = row.SaveQuantity,
                DisplayQuantity = row.DisplayQuantity,
                ExtraQuantity = row.ExtraQuantity,
                DisplayRuleText = row.DisplayRuleText,
                KitchenNote = row.KitchenNote,
                ExistingItemsDisplay = display
            });
        }

        OnPropertyChanged(nameof(OrderItemPickerPlanSummary));
    }

    [RelayCommand]
    private void PreviousQuoteMenuPage()
    {
        if (CanGoPreviousQuoteMenuPage) QuoteMenuPageIndex--;
    }

    [RelayCommand]
    private void NextQuoteMenuPage()
    {
        if (CanGoNextQuoteMenuPage) QuoteMenuPageIndex++;
    }

    [RelayCommand]
    private void ClearQuoteMenuCategory()
    {
        SelectedMenuCategory = null;
    }

    partial void OnServiceSearchTextChanged(string value)
    {
        ApplyServiceFilter();
    }

    private void ApplyServiceFilter()
    {
        AlacarteServices.Clear();
        var filtered = string.IsNullOrWhiteSpace(ServiceSearchText)
            ? _defaultServices
            : _defaultServices.Where(s => s.Name.Contains(ServiceSearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var s in filtered) AlacarteServices.Add(s);
    }

    partial void OnQuoteDiscountChanged(decimal value) => NotifyQuoteTotals();
    partial void OnQuoteDepositChanged(decimal value) => NotifyQuoteTotals();
    partial void OnReservationGuestCountChanged(int value)
    {
        SyncQuoteCapacityFromTableCount();
        NotifyQuoteTotals();
    }

    partial void OnQuoteGuestCountChanged(int value)
    {
        SyncQuoteCapacityFromGuestsPerTable();
        NotifyQuoteTotals();
    }

    partial void OnQuoteGuestsPerTableChanged(int value)
    {
        SyncQuoteCapacityFromGuestsPerTable();
        NotifyQuoteTotals();
    }

    partial void OnSelectedSavedQuoteChanged(SavedQuoteDto? value)
    {
        NotifyQuoteTotals();
        OnPropertyChanged(nameof(SelectedSavedFoodLines));
        OnPropertyChanged(nameof(SelectedSavedServiceLines));
        OnPropertyChanged(nameof(CurrentQuoteTables));
    }

    partial void OnDashboardChanged(AlacarteDashboardDto value)
    {
        SyncDashboardVisualState();
    }

    private readonly List<AlacarteServiceItem> _defaultServices = [];

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTab));
        OnPropertyChanged(nameof(IsRoomsTab));
        OnPropertyChanged(nameof(IsReservationsTab));
        OnPropertyChanged(nameof(IsOpenOrdersTab));
        OnPropertyChanged(nameof(IsContractsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsOrderPanelWithOrder));
        OnPropertyChanged(nameof(ShowOrderPanel));
        if (value == 3)
        {
            EnsureSelectedOpenOrder();
        }
    }

    partial void OnOrderDetailTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsOrderFoodTab));
        OnPropertyChanged(nameof(IsOrderWineTab));
        OnPropertyChanged(nameof(IsOrderDrinkTab));
        OnPropertyChanged(nameof(IsOrderServiceTab));
        OnPropertyChanged(nameof(IsOrderInfoTab));
        OnPropertyChanged(nameof(IsOrderPrintHistoryTab));
        OnPropertyChanged(nameof(ShowOrderActionButtons));
    }

    partial void OnOrderItemPickerTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsOrderItemPickerFoodTab));
        OnPropertyChanged(nameof(IsOrderItemPickerServiceTab));
    }

    partial void OnOrderItemPickerStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsOrderItemPickerSelectStep));
        OnPropertyChanged(nameof(IsOrderItemPickerPlanStep));
        OnPropertyChanged(nameof(IsOrderItemPickerSendStep));
        OnPropertyChanged(nameof(OrderItemPickerStepSummary));
    }


    partial void OnQuoteSubTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsQuoteFoodTab));
        OnPropertyChanged(nameof(IsQuoteServiceTab));
        OnPropertyChanged(nameof(IsQuotePackageTab));
    }
    partial void OnSettingsSubTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSettingsArea));
        OnPropertyChanged(nameof(IsSettingsRooms));
        OnPropertyChanged(nameof(IsSettingsTables));
        OnPropertyChanged(nameof(IsSettingsPrinters));
        OnPropertyChanged(nameof(IsSettingsMenuGroups));
        OnPropertyChanged(nameof(IsSettingsMenuItems));
        OnPropertyChanged(nameof(IsSettingsExtServices));
        OnPropertyChanged(nameof(IsSettingsPayment));
        OnPropertyChanged(nameof(IsSettingsReservation));
        OnPropertyChanged(nameof(IsSettingsDocuments));
        OnPropertyChanged(nameof(IsSettingsPermissions));
        OnPropertyChanged(nameof(IsSettingsIntegration));
        OnPropertyChanged(nameof(IsSettingsRestaurant));
        OnPropertyChanged(nameof(IsSettingsServingGroups));
    }

    partial void OnSelectedSettingDiningAreaNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(SettingAreaType))
        {
            SettingAreaType = value;
        }

        OnPropertyChanged(nameof(FilteredSettingRooms));
        OnPropertyChanged(nameof(FilteredSettingTables));
    }

    partial void OnServicesSubTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsServicesFoodTab));
        OnPropertyChanged(nameof(IsServicesSvcTab));
    }

    private void RefreshRoomTables()
    {
        SelectedRoomTables.Clear();
        if (SelectedRoom is null) return;
        var tables = Tables
            .Where(t => t.DiningRoomId == SelectedRoom.Id)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name);
        foreach (var t in tables) SelectedRoomTables.Add(t);
    }

    partial void OnSelectedRoomChanged(DiningRoomDto? value)
    {
        if (value is not null)
        {
            RoomCode = value.Code;
            RoomName = value.Name;
            RoomCapacity = value.Capacity;
        }
        OnPropertyChanged(nameof(HasSelectedRoom));
        OnPropertyChanged(nameof(HasRoomNoOrder));
        OnPropertyChanged(nameof(SelectedRoomReservations));
        OnPropertyChanged(nameof(SelectedRoomReservationCount));
        OnPropertyChanged(nameof(RoomCalendarDays));
        OnPropertyChanged(nameof(RoomReservedDates));
        OnPropertyChanged(nameof(RoomReservedDateCount));
        SelectedRoomCalendarDate = null;
        RefreshRoomReservationCalendarState();
        RefreshRoomTables();
        ApplyTableFilter();
    }

    partial void OnSelectedFloorChanged(string value)
    {
        ApplyTableFilter();
        RefreshOpenOrderRooms();
        ApplyOpenOrderFilter();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        ApplyTableFilter();
    }

    partial void OnTableSearchTextChanged(string value) => ApplyTableFilter();
    partial void OnTableStatusFilterChanged(string value) => ApplyTableFilter();

    partial void OnSelectedOrderRoomFilterChanged(DiningRoomDto? value) => ApplyOpenOrderFilter();
    partial void OnSelectedOrderSearchTextChanged(string value) => ApplyOpenOrderFilter();

    private void ApplyTableFilter()
    {
        var query = Tables.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SelectedFloor))
        {
            var floorRoomIds = Rooms.Where(r => r.RoomType == SelectedFloor).Select(r => r.Id).ToHashSet();
            query = query.Where(t => floorRoomIds.Contains(t.DiningRoomId));
        }
        if (SelectedRoom is not null)
            query = query.Where(t => t.DiningRoomId == SelectedRoom.Id);
        if (!string.IsNullOrWhiteSpace(TableSearchText))
            query = query.Where(t => t.Name.Contains(TableSearchText, StringComparison.OrdinalIgnoreCase)
                                  || t.RoomName.Contains(TableSearchText, StringComparison.OrdinalIgnoreCase));
        query = TableStatusFilter switch
        {
            "Trống" => query.Where(t => t.Status == "Empty"),
            "Đã đặt" => query.Where(t => t.Status == "Reserved"),
            "Đang phục vụ" => query.Where(t => t.Status == "Serving"),
            "Khóa bàn" => query.Where(t => t.Status == "Locked"),
            _ => query
        };
        FilteredTables.Clear();
        foreach (var t in query) FilteredTables.Add(t);

        // Filter rooms by floor
        var roomQuery = Rooms.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SelectedFloor))
            roomQuery = roomQuery.Where(r => r.RoomType == SelectedFloor);
        var filteredRooms = new List<DiningRoomDto>();
        foreach (var r in roomQuery)
        {
            var roomOrders = OpenOrders
                .Where(o => o.DiningRoomId == r.Id && IsOrderScheduledForSelectedDate(o))
                .OrderBy(ResolveOrderScheduleDate)
                .ToList();
            var roomReservations = Reservations
                .Where(res => res.DiningRoomId == r.Id
                    && res.ReservationDateTime.Date == SelectedDate.Date
                    && !string.Equals(res.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(res.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(res.Status, "NoShow", StringComparison.OrdinalIgnoreCase))
                .OrderBy(res => res.ReservationDateTime)
                .ToList();

            var hasServingOpenOrder = roomOrders.Any(o =>
                string.Equals(o.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                || string.Equals(o.Status, "SentKitchen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(o.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) && ResolveOrderScheduleDate(o) <= DateTime.Now
                || 
                o.Tables.Any(t => !t.IsVirtual && string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase)));
            var hasPendingOpenOrder = roomOrders.Count != 0;
            r.Status = hasServingOpenOrder ? "Occupied" : hasPendingOpenOrder ? "Reserved" : "Available";
            if (!hasServingOpenOrder && roomReservations.Count != 0)
                r.Status = "Reserved";

            var primaryOrder = roomOrders.FirstOrDefault();
            var primaryReservation = roomReservations.FirstOrDefault();
            ApplyRoomBookingDisplay(r, primaryOrder, primaryReservation);
            filteredRooms.Add(r);
        }

        if (!string.IsNullOrWhiteSpace(TableSearchText))
        {
            filteredRooms = filteredRooms
                .Where(r => r.Name.Contains(TableSearchText, StringComparison.OrdinalIgnoreCase)
                    || r.Code.Contains(TableSearchText, StringComparison.OrdinalIgnoreCase)
                    || r.RoomType.Contains(TableSearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        filteredRooms = TableStatusFilter switch
        {
            "Trống" => filteredRooms.Where(r => string.Equals(r.Status, "Available", StringComparison.OrdinalIgnoreCase)).ToList(),
            "Đã đặt" => filteredRooms.Where(r => string.Equals(r.Status, "Reserved", StringComparison.OrdinalIgnoreCase)).ToList(),
            "Đang phục vụ" => filteredRooms.Where(r =>
                string.Equals(r.Status, "Occupied", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.Status, "Serving", StringComparison.OrdinalIgnoreCase)).ToList(),
            "Khóa bàn" => filteredRooms.Where(r => string.Equals(r.Status, "Locked", StringComparison.OrdinalIgnoreCase)).ToList(),
            _ => filteredRooms
        };

        FilteredRooms.Clear();
        foreach (var room in filteredRooms)
            FilteredRooms.Add(room);
    }

    private bool IsOrderScheduledForSelectedDate(AlacarteOrderDto order)
        => ResolveOrderScheduleDate(order).Date == SelectedDate.Date;

    private static DateTime ResolveOrderScheduleDate(AlacarteOrderDto order)
    {
        if (order.ArrivalDateTime.HasValue)
            return order.ArrivalDateTime.Value;

        return order.OrderDate;
    }

    private static void ApplyRoomBookingDisplay(DiningRoomDto room, AlacarteOrderDto? order, TableReservationDto? reservation)
    {
        room.BookingSummaryText = string.Empty;
        room.ServingSummaryText = string.Empty;

        if (order is not null)
        {
            room.BookingSummaryText = BuildRoomBookingSummary(
                order.CustomerName,
                order.StartTime,
                order.EndTime,
                order.GuestCount,
                order.ArrivalDateTime);
            room.ServingSummaryText = BuildRoomServingSummary(order);
            if (!string.IsNullOrWhiteSpace(room.BookingSummaryText))
                return;
        }

        if (reservation is null)
            return;

        room.BookingSummaryText = BuildRoomBookingSummary(
            reservation.CustomerName,
            reservation.ReservationDateTime.ToString("HH:mm"),
            string.Empty,
            reservation.GuestCount,
            reservation.ReservationDateTime);
        room.ServingSummaryText = BuildRoomReservationServingSummary(reservation);
    }

    private static string BuildRoomBookingSummary(string? customerName, string? startTime, string? endTime, int guestCount, DateTime? fallbackDateTime = null)
    {
        var segments = new List<string>();
        var normalizedCustomerName = customerName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCustomerName))
            segments.Add(normalizedCustomerName);

        var timeText = BuildBookingTimeText(startTime, endTime, fallbackDateTime);
        if (!string.IsNullOrWhiteSpace(timeText))
            segments.Add(timeText);

        if (guestCount > 0)
            segments.Add($"{guestCount} khách");

        return string.Join(" / ", segments);
    }

    private static string BuildRoomServingSummary(AlacarteOrderDto order)
    {
        var servingTableCount = ResolveOpenedTableCount(order);
        return BuildRoomServingSummaryCore(order.CustomerName, servingTableCount, order.GuestCount);
    }

    private static string BuildRoomReservationServingSummary(TableReservationDto reservation)
        => BuildRoomServingSummaryCore(reservation.CustomerName, reservation.DiningTableId.HasValue ? 1 : 0, reservation.GuestCount);

    private static int ResolveOpenedTableCount(AlacarteOrderDto order)
    {
        var physicalTables = order.Tables?
            .Where(t => !t.IsVirtual && string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.DiningTableId ?? (Guid?)t.Id)
            .Distinct()
            .Count() ?? 0;
        if (physicalTables > 0)
            return physicalTables;

        if (order.DiningTableId.HasValue || !string.IsNullOrWhiteSpace(order.TableName))
            return 1;

        return 0;
    }

    private static string BuildRoomServingSummaryCore(string? customerName, int tableCount, int guestCount)
    {
        var segments = new List<string>();
        var normalizedCustomerName = customerName?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCustomerName))
            segments.Add(normalizedCustomerName);

        if (tableCount > 0)
            segments.Add($"{tableCount} bàn");

        if (guestCount > 0)
            segments.Add($"{guestCount} khách");

        return string.Join(" / ", segments);
    }

    private static string BuildBookingTimeText(string? startTime, string? endTime, DateTime? fallbackDateTime = null)
    {
        var normalizedStart = NormalizeBookingTime(startTime, fallbackDateTime);
        var normalizedEnd = NormalizeBookingTime(endTime, null);

        if (!string.IsNullOrWhiteSpace(normalizedStart) && !string.IsNullOrWhiteSpace(normalizedEnd))
            return $"{normalizedStart} - {normalizedEnd}";

        return normalizedStart;
    }

    private static string NormalizeBookingTime(string? timeText, DateTime? fallbackDateTime)
    {
        if (string.IsNullOrWhiteSpace(timeText))
            return fallbackDateTime?.ToString("HH'h'mm") ?? string.Empty;

        var trimmed = timeText.Trim();
        if (TimeSpan.TryParse(trimmed, out var time))
            return $"{time.Hours:00}h{time.Minutes:00}";

        if (DateTime.TryParse(trimmed, out var dateTime))
            return dateTime.ToString("HH'h'mm");

        return trimmed;
    }

    private void RefreshOpenOrderRooms()
    {
        var roomIds = OpenOrders.Where(o => o.DiningRoomId.HasValue).Select(o => o.DiningRoomId!.Value).Distinct().ToHashSet();
        var matched = Rooms.Where(r => roomIds.Contains(r.Id)).ToList();
        // Filter by selected floor if set
        if (!string.IsNullOrWhiteSpace(SelectedFloor))
            matched = matched.Where(r => r.RoomType == SelectedFloor).ToList();
        for (int i = OpenOrderRooms.Count - 1; i >= 0; i--)
        {
            if (!roomIds.Contains(OpenOrderRooms[i].Id))
                OpenOrderRooms.RemoveAt(i);
        }
        foreach (var r in matched)
        {
            if (!OpenOrderRooms.Any(x => x.Id == r.Id))
                OpenOrderRooms.Add(new DiningRoomDto
                {
                    Id = r.Id, Code = r.Code, Name = r.Name,
                    RoomType = r.RoomType, Capacity = r.Capacity,
                    Description = r.Description, Status = "Serving",
                    IsActive = r.IsActive, TableCount = OpenOrders.Count(o => o.DiningRoomId == r.Id)
                });
        }
        // Remove rooms that don't match floor filter
        if (!string.IsNullOrWhiteSpace(SelectedFloor))
        {
            for (int i = OpenOrderRooms.Count - 1; i >= 0; i--)
            {
                if (OpenOrderRooms[i].RoomType != SelectedFloor)
                    OpenOrderRooms.RemoveAt(i);
            }
        }
        foreach (var r in OpenOrderRooms)
        {
            r.TableCount = OpenOrders.Count(o => o.DiningRoomId == r.Id);
        }
    }

    private void ApplyOpenOrderFilter()
    {
        var query = OpenOrders.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SelectedFloor))
        {
            var floorRoomIds = Rooms.Where(r => r.RoomType == SelectedFloor).Select(r => r.Id).ToHashSet();
            query = query.Where(o => o.DiningRoomId.HasValue && floorRoomIds.Contains(o.DiningRoomId.Value));
        }
        if (!string.IsNullOrWhiteSpace(SelectedOrderSearchText))
            query = query.Where(o =>
                (o.OrderNo?.Contains(SelectedOrderSearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (o.RoomName?.Contains(SelectedOrderSearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (o.CustomerName?.Contains(SelectedOrderSearchText, StringComparison.OrdinalIgnoreCase) == true));
        FilteredOpenOrders.Clear();
        foreach (var o in query) FilteredOpenOrders.Add(o);
    }

    [RelayCommand]
    private void SetOpenOrderTableColumn(int count)
    {
        if (count >= 2 && count <= 6)
            OpenOrderTableColumnCount = count;
    }

    [RelayCommand]
    private void SelectOrderRoom(DiningRoomDto? room)
    {
        SelectedOrderRoomFilter = (SelectedOrderRoomFilter?.Id == room?.Id) ? null : room;
    }

    [RelayCommand]
    private void SelectFloor(string? floor)
    {
        SelectedFloor = (SelectedFloor == floor) ? string.Empty : floor ?? string.Empty;
    }

    [RelayCommand]
    private void AddMenuItemToQuote(PosMenuItemCard? item)
    {
        if (item is null) return;
        var existing = QuoteLines.FirstOrDefault(q => q.MenuItemId == item.Id || (q.Type == "Món ăn" && string.Equals(q.Name, item.Name, StringComparison.OrdinalIgnoreCase)));
        if (existing is not null) { existing.Quantity++; existing.NotifyTotal(); }
        else QuoteLines.Add(new QuoteLineItem { MenuItemId = item.Id, Name = item.Name, UnitPrice = item.UnitPrice, Quantity = 1, Type = "Món ăn", PricingRule = "PerTable" });
        RefreshLineNumbers();
        NotifyQuoteTotals();
    }

    [RelayCommand]
    private void AddServiceToQuote(AlacarteServiceItem? svc)
    {
        if (svc is null) return;
        var existing = QuoteLines.FirstOrDefault(q => q.Name == svc.Name && q.Type == "Dịch vụ");
        if (existing is not null) { existing.Quantity++; existing.NotifyTotal(); }
        else QuoteLines.Add(new QuoteLineItem { Name = svc.Name, UnitPrice = svc.Price, Quantity = 1, Type = "Dịch vụ", PricingRule = "PerRoom" });
        RefreshLineNumbers();
        NotifyQuoteTotals();
    }

    [RelayCommand]
    private void AddMenuPackageToQuote(BanquetMenuPackageDto? package)
    {
        if (package is null)
        {
            return;
        }

        if (package.Lines is null || package.Lines.Count == 0)
        {
            StatusText = $"Gói {package.Name} chưa có món chi tiết để áp vào báo giá.";
            return;
        }

        var addedCount = 0;
        foreach (var line in package.Lines.OrderBy(x => x.LineNo))
        {
            if (line.MenuItemId == Guid.Empty && string.IsNullOrWhiteSpace(line.MenuItemName))
            {
                continue;
            }

            var pricingRule = string.IsNullOrWhiteSpace(line.PricingRule) ? package.PricingMode : line.PricingRule;
            var quantity = Math.Max(1, (int)Math.Ceiling(line.Qty <= 0 ? 1m : line.Qty));
            var existing = QuoteLines.FirstOrDefault(q =>
                q.Type == "Món ăn"
                && ((q.MenuItemId.HasValue && q.MenuItemId.Value == line.MenuItemId)
                    || string.Equals(q.Name, line.MenuItemName, StringComparison.OrdinalIgnoreCase))
                && string.Equals(q.PricingRule, pricingRule, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Quantity += quantity;
                existing.NotifyTotal();
            }
            else
            {
                QuoteLines.Add(new QuoteLineItem
                {
                    MenuItemId = line.MenuItemId,
                    Name = string.IsNullOrWhiteSpace(line.MenuItemName) ? package.Name : line.MenuItemName,
                    UnitPrice = line.UnitPrice,
                    Quantity = quantity,
                    Type = "Món ăn",
                    PricingRule = pricingRule
                });
            }

            addedCount++;
        }

        RefreshLineNumbers();
        NotifyQuoteTotals();
        StatusText = addedCount > 0
            ? $"Đã áp gói {package.Name} vào báo giá ({addedCount} món)."
            : $"Không có món hợp lệ trong gói {package.Name}.";
    }

    [RelayCommand]
    private void RemoveFromQuote(QuoteLineItem? item)
    {
        if (item is not null) { QuoteLines.Remove(item); RefreshLineNumbers(); NotifyQuoteTotals(); }
    }

    [RelayCommand]
    private void IncreaseQuoteQty(QuoteLineItem? item)
    {
        if (item is null) return;
        item.Quantity++;
        item.NotifyTotal();
        NotifyQuoteTotals();
    }

    [RelayCommand]
    private void DecreaseQuoteQty(QuoteLineItem? item)
    {
        if (item is null) return;
        if (item.Quantity <= 1) { QuoteLines.Remove(item); RefreshLineNumbers(); }
        else { item.Quantity--; item.NotifyTotal(); }
        NotifyQuoteTotals();
    }

    private void RefreshLineNumbers()
    {
        int i = 1;
        foreach (var q in QuoteLines) { q.LineNo = i; q.NotifyLineNo(); i++; }
    }

    private void NotifyQuoteTotals()
    {
        OnPropertyChanged(nameof(QuoteSubtotal));
        OnPropertyChanged(nameof(QuoteServiceTotal));
        OnPropertyChanged(nameof(QuoteVat));
        OnPropertyChanged(nameof(QuoteTotalPerTable));
        OnPropertyChanged(nameof(QuoteGrandTotal));
        OnPropertyChanged(nameof(QuoteFinalTotal));
        OnPropertyChanged(nameof(QuoteRemaining));
        OnPropertyChanged(nameof(QuoteDiscountDisplay));
        OnPropertyChanged(nameof(QuoteDepositDisplay));
        OnPropertyChanged(nameof(QuoteTableCount));
        OnPropertyChanged(nameof(QuoteOddGuestCount));
        OnPropertyChanged(nameof(QuoteOddServingText));
        OnPropertyChanged(nameof(QuoteBaseAmount));
        OnPropertyChanged(nameof(QuoteFinalAmount));
        OnPropertyChanged(nameof(QuoteRemainingAmount));
    }

    public decimal QuoteSubtotal => QuoteLines.Where(q => q.Type != "Dịch vụ").Sum(q => q.Total);
    public decimal QuoteServiceTotal => QuoteLines.Where(q => q.Type == "Dịch vụ").Sum(q => q.Total);
    public decimal QuoteVat => (QuoteSubtotal + QuoteServiceTotal) * 8m / 100m;
    public decimal QuoteTotalPerTable => Math.Max(0, QuoteSubtotal + QuoteVat) / Math.Max(1, ReservationGuestCount);
    public decimal QuoteGrandTotal => QuoteSubtotal + QuoteServiceTotal + QuoteVat;
    public decimal QuoteFinalTotal => Math.Max(0, QuoteGrandTotal - QuoteDiscount);
    public decimal QuoteRemaining => Math.Max(0, QuoteFinalTotal - QuoteDeposit);
    public string QuoteDiscountDisplay => QuoteDiscount > 0 ? $"-{QuoteDiscount:N0} đ" : "0 đ";
    public string QuoteDepositDisplay => QuoteDeposit > 0 ? $"{QuoteDeposit:N0} đ" : "0 đ";
    public decimal QuoteBaseAmount => SelectedSavedQuote?.TotalAmount ?? QuoteGrandTotal;
    public decimal QuoteFinalAmount => Math.Max(0, QuoteBaseAmount - QuoteDiscount);
    public decimal QuoteRemainingAmount => Math.Max(0, QuoteFinalAmount - QuoteDeposit);
    public int QuoteTableCount => Math.Max(1, ReservationGuestCount);
    public int QuoteOddGuestCount => Math.Max(1, QuoteGuestCount) % Math.Max(1, QuoteGuestsPerTable);
    public string QuoteOddServingText => QuoteOddGuestCount > 0 ? $"1 bàn lẻ {QuoteOddGuestCount}" : "Không";

    private void SyncQuoteCapacityFromGuestsPerTable()
    {
        if (_syncingQuoteCapacity) return;
        try
        {
            _syncingQuoteCapacity = true;
            var guestCount = Math.Max(1, QuoteGuestCount);
            var guestsPerTable = Math.Max(1, QuoteGuestsPerTable);
            var tableCount = Math.Max(1, (int)Math.Ceiling(guestCount / (double)guestsPerTable));

            if (QuoteGuestCount != guestCount) QuoteGuestCount = guestCount;
            if (QuoteGuestsPerTable != guestsPerTable) QuoteGuestsPerTable = guestsPerTable;
            if (ReservationGuestCount != tableCount) ReservationGuestCount = tableCount;
        }
        finally
        {
            _syncingQuoteCapacity = false;
        }
    }

    private void SyncQuoteCapacityFromTableCount()
    {
        if (_syncingQuoteCapacity) return;
        try
        {
            _syncingQuoteCapacity = true;
            var tableCount = Math.Max(1, ReservationGuestCount);
            var guestCount = Math.Max(1, QuoteGuestCount);
            var guestsPerTable = Math.Max(1, (int)Math.Ceiling(guestCount / (double)tableCount));

            if (ReservationGuestCount != tableCount) ReservationGuestCount = tableCount;
            if (QuoteGuestCount != guestCount) QuoteGuestCount = guestCount;
            if (QuoteGuestsPerTable != guestsPerTable) QuoteGuestsPerTable = guestsPerTable;
        }
        finally
        {
            _syncingQuoteCapacity = false;
        }
    }
    public IEnumerable<SavedQuoteLineDto> SelectedSavedFoodLines => SelectedSavedQuote?.Lines.Where(l => l.Type != "Dịch vụ") ?? Enumerable.Empty<SavedQuoteLineDto>();
    public IEnumerable<SavedQuoteLineDto> SelectedSavedServiceLines => SelectedSavedQuote?.Lines.Where(l => l.Type == "Dịch vụ") ?? Enumerable.Empty<SavedQuoteLineDto>();

    [RelayCommand]
    private async Task OpenRoomOrderAsync(DiningRoomDto? room)
    {
        if (room is null) return;
        await OpenRoomInOrders(room);
    }

    [RelayCommand]
    private async Task OpenRoomInOrders(DiningRoomDto? room)
    {
        if (room is null) return;
        await RunBusyAsync(async () =>
        {
            var order = await _service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
            {
                DiningRoomId = room.Id,
                CreatedBy = "pos-ui"
            });
            SelectedOrder = order;
            Dashboard = await _service.GetDashboardAsync();
            Reset(Tables, await _service.GetTablesAsync());
            ApplyTableFilter();
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshOpenOrderRooms();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == order.Id) ?? order;
            SelectedOrderRoomFilter = null;
            ApplyOpenOrderFilter();
            NotifyCollectionState();
            SelectedTabIndex = 3;
            StatusText = $"Đã mở phòng {room.Name} — đơn {SelectedOrder.OrderNo}.";
            if ((SelectedOrder.Tables?.Count(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)) ?? 0) == 0)
            {
                OpenRoomSetupDialog(room, SelectedOrder);
            }
        });
    }

    private async Task<DiningTableDto> EnsureRoomHasTableAsync(DiningRoomDto room)
    {
        var existingTable = Tables
            .Where(t => t.DiningRoomId == room.Id)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Code)
            .FirstOrDefault();
        if (existingTable is not null)
        {
            return existingTable;
        }

        var tableCode = BuildDefaultRoomTableCode(room);
        var defaultTable = await _service.SaveTableAsync(new SaveDiningTableRequest
        {
            DiningRoomId = room.Id,
            Code = tableCode,
            Name = "Bàn 01",
            SeatCount = Math.Max(1, room.Capacity > 0 ? room.Capacity : 10),
            Status = "Empty",
            IsActive = true,
            SortOrder = 1
        });

        UpsertTable(defaultTable);
        RefreshSettingCollections();
        ApplyTableFilter();
        return defaultTable;
    }

    private string BuildDefaultRoomTableCode(DiningRoomDto room)
    {
        var rawPrefix = string.IsNullOrWhiteSpace(room.Code) ? room.Name : room.Code;
        var normalizedPrefix = new string((rawPrefix ?? "PHONG")
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedPrefix))
        {
            normalizedPrefix = "PHONG";
        }

        for (var index = 1; index <= 999; index++)
        {
            var candidate = $"{normalizedPrefix}-B{index:00}";
            if (!Tables.Any(t => string.Equals(t.Code, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        var fallback = $"{normalizedPrefix}-{Guid.NewGuid():N}";
        return fallback[..Math.Min(24, fallback.Length)];
    }

    partial void OnSelectedTableChanged(DiningTableDto? value)
    {
        OnPropertyChanged(nameof(SelectedOrderAreaDisplayName));
        OnPropertyChanged(nameof(SelectedOrderRoomDisplayName));
        OnPropertyChanged(nameof(SelectedOrderTableDisplayName));
        OnPropertyChanged(nameof(IsSummaryTableContext));
        RefreshSelectedOrderDetailLines();
        RefreshRoomServingPlanRows();
        RefreshServingGroupSummaries();
        RefreshOrderItemPickerPlan();
        if (value is null) return;
        TableCode = value.Code;
        TableName = value.Name;
        TableSeats = value.SeatCount;
    }

    partial void OnSelectedOrderChanged(AlacarteOrderDto? value)
    {
        SelectedTable = value is null ? null : SelectedOrderRoomTables.FirstOrDefault();
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(IsOrderPanelWithOrder));
        OnPropertyChanged(nameof(HasRoomNoOrder));
        OnPropertyChanged(nameof(IsSummaryTableContext));
        OnPropertyChanged(nameof(SelectedOrderAreaDisplayName));
        OnPropertyChanged(nameof(SelectedOrderRoomDisplayName));
        OnPropertyChanged(nameof(SelectedOrderSingleTableOrderNo));
        OnPropertyChanged(nameof(SelectedOrderTableDisplayName));
        RefreshSelectedOrderDetailLines();
        OnPropertyChanged(nameof(SelectedOrderRoomTables));
        OnPropertyChanged(nameof(SelectedOrderSummaryTable));
        OnPropertyChanged(nameof(SelectedOrderDetailTables));
        OnPropertyChanged(nameof(SelectedOrderRoomTableCount));
        OnPropertyChanged(nameof(AddedFoodItemIds));
        RefreshRoomServingPlanRows();
        RefreshServingGroupSummaries();
        RefreshServingGroupEditor();
        NotifyPreparationQueueState();
        RefreshOrderItemPickerPlan();
        IsOrderFinalized = false;
        if (value is not null) OrderDetailTabIndex = 0;
    }

    partial void OnIsOrderFinalizedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPrintKitchen));
    }

    private static bool IsPendingPreparationLine(AlacarteOrderLineDto line)
        => !IsOrderServiceLine(line)
           && !string.Equals(line.LineStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase);

    private static bool IsSentPreparationLine(AlacarteOrderLineDto line)
        => !IsOrderServiceLine(line)
           && !string.Equals(line.LineStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
           && string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase);

    private void NotifyPreparationQueueState()
    {
        RefreshSelectedOrderDetailLines();
        OnPropertyChanged(nameof(PendingPreparationLineCount));
        OnPropertyChanged(nameof(SentPreparationLineCount));
        OnPropertyChanged(nameof(CanSendOrderToPreparation));
        OnPropertyChanged(nameof(PreparationQueueSummary));
        OnPropertyChanged(nameof(SendPreparationButtonText));
        RefreshPreparationQueueRows();
    }

    private void NotifyRoomServingPlanState()
    {
        OnPropertyChanged(nameof(HasRoomServingPlan));
        OnPropertyChanged(nameof(RoomServingPlanTotalGuests));
        OnPropertyChanged(nameof(RoomServingPlanServingTableCount));
        OnPropertyChanged(nameof(RoomServingPlanWaitingTableCount));
        OnPropertyChanged(nameof(RoomServingPlanHasSoftWarnings));
        OnPropertyChanged(nameof(RoomServingPlanSummary));
        OnPropertyChanged(nameof(RoomServingPlanWarningText));
    }

    private void RefreshRoomServingPlanRows()
    {
        foreach (var existing in RoomServingPlanRows)
        {
            existing.PropertyChanged -= RoomServingPlanRow_PropertyChanged;
        }

        RoomServingPlanRows.Clear();
        if (SelectedOrder is null)
        {
            NotifyRoomServingPlanState();
            return;
        }

        var standardSeats = GetSelectedOrderTableStandard();
        foreach (var table in GetSelectedOrderServingTables().Where(x => x.Id != Guid.Empty))
        {
            var baseSeatCount = Tables.FirstOrDefault(x => x.Id == table.Id)?.SeatCount ?? standardSeats;
            var isServing = string.Equals(table.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(table.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase);
            var row = new RoomServingPlanRow
            {
                TableId = table.Id,
                OrderTableId = table.OrderTableId ?? table.Id,
                TableName = table.Name,
                StandardSeats = Math.Max(1, baseSeatCount),
                SoftLimitSeats = Math.Max(1, baseSeatCount + 2),
                GuestCount = isServing ? Math.Max(0, table.SeatCount) : 0,
                IsServing = isServing
            };
            row.PropertyChanged += RoomServingPlanRow_PropertyChanged;
            RoomServingPlanRows.Add(row);
        }

        NotifyRoomServingPlanState();
    }

    private void RoomServingPlanRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RoomServingPlanRow.GuestCount)
            or nameof(RoomServingPlanRow.IsServing))
        {
            NotifyRoomServingPlanState();
        }
    }

    private void RefreshServingGroupSummaries()
    {
        ServingGroupSummaries.Clear();
        var servingTables = GetSelectedOrderServingTables();
        if (servingTables.Count == 0)
        {
            OnPropertyChanged(nameof(HasServingGroupSummaries));
            return;
        }

        var standard = GetSelectedOrderTableStandard();
        var activeTables = servingTables
            .Where(t => string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(t.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var standardTables = activeTables.Where(t => t.SeatCount == standard).ToList();
        var oddTables = activeTables.Where(t => t.SeatCount != standard).ToList();
        var waitingTables = servingTables
            .Where(t => string.Equals(t.Status, "Waiting", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(t.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(t.Status, "Locked", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(t.Status, "Bàn chờ", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (standardTables.Count > 0)
        {
            ServingGroupSummaries.Add(new ServingGroupSummaryRow
            {
                Title = "Nhóm bàn chuẩn",
                Subtitle = $"{standardTables.Count} bàn · {standardTables.Sum(x => x.SeatCount)} khách",
                ScopeValue = "StandardOnly",
                Summary = $"Mỗi bàn {standard} khách, phù hợp áp món chuẩn theo bàn hoặc theo khách.",
                AccentKey = "Blue"
            });
        }

        if (oddTables.Count > 0)
        {
            ServingGroupSummaries.Add(new ServingGroupSummaryRow
            {
                Title = "Nhóm bàn lẻ",
                Subtitle = $"{oddTables.Count} bàn · {oddTables.Sum(x => x.SeatCount)} khách",
                ScopeValue = "OddOnly",
                Summary = "Các bàn vượt hoặc thiếu chuẩn, phù hợp món cần phần bù hoặc định lượng riêng.",
                AccentKey = "Orange"
            });
        }

        if (waitingTables.Count > 0)
        {
            ServingGroupSummaries.Add(new ServingGroupSummaryRow
            {
                Title = "Bàn chờ / chưa mở",
                Subtitle = $"{waitingTables.Count} bàn sẵn sàng mở thêm",
                ScopeValue = "AllServing",
                Summary = "Các bàn này chưa tham gia phục vụ nên chưa nhận món trong plan hiện tại.",
                AccentKey = "Green"
            });
        }

        if (SelectedTable is not null && SelectedTable.Id != Guid.Empty)
        {
            ServingGroupSummaries.Add(new ServingGroupSummaryRow
            {
                Title = "Bàn đang chọn",
                Subtitle = $"{SelectedTable.Name} · {Math.Max(0, SelectedTable.SeatCount)} khách",
                ScopeValue = "SelectedTable",
                Summary = "Dùng khi cần áp món riêng cho đúng bàn đang chọn.",
                AccentKey = "Slate"
            });
        }

        OnPropertyChanged(nameof(HasServingGroupSummaries));
    }

    private void RefreshServingGroupEditor()
    {
        ServingGroupEditorRows.Clear();
        ServingGroupTableSelectionRows.Clear();

        var order = SelectedOrder;
        if (order is null)
        {
            OnPropertyChanged(nameof(HasServingGroupEditorRows));
            OnPropertyChanged(nameof(HasServingGroupSelectionRows));
            OnPropertyChanged(nameof(SelectedServingGroupTableSummary));
            return;
        }

        foreach (var table in SelectedOrderRoomTables
                     .Where(x => x.OrderTableId.HasValue || x.Id != Guid.Empty)
                     .OrderBy(x => x.SortOrder)
                     .ThenBy(x => x.Name))
        {
            var isServing = string.Equals(table.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(table.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase);
            var row = new ServingGroupTableSelectionRow
            {
                OrderTableId = table.OrderTableId ?? table.Id,
                TableName = table.Name,
                GuestCount = Math.Max(0, table.SeatCount),
                Status = table.Status,
                IsServing = isServing,
                CanSelect = isServing
            };
            row.PropertyChanged += ServingGroupTableSelectionRow_PropertyChanged;
            ServingGroupTableSelectionRows.Add(row);
        }

        foreach (var group in order.ServingGroups.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
        {
            ServingGroupEditorRows.Add(new ServingGroupEditorRow
            {
                Id = group.Id,
                Name = group.Name,
                Code = group.Code,
                ScopeType = group.ScopeType,
                ServingRule = group.ServingRule,
                Note = group.Note,
                TableCount = group.TableCount,
                GuestCount = group.GuestCount,
                IsSystemDefined = group.IsSystemDefined,
                TableSummary = group.Tables.Count == 0
                    ? "Chưa có bàn"
                    : string.Join(" · ", group.Tables.OrderBy(x => x.TableName).Select(x => $"{x.TableName} ({x.GuestCount})"))
            });
        }

        var currentSelectedId = SelectedServingGroupEditor?.Id;
        SelectedServingGroupEditor = currentSelectedId.HasValue
            ? ServingGroupEditorRows.FirstOrDefault(x => x.Id == currentSelectedId.Value)
            : ServingGroupEditorRows.FirstOrDefault(x => !x.IsSystemDefined) ?? ServingGroupEditorRows.FirstOrDefault();

        if (SelectedServingGroupEditor is null)
        {
            ServingGroupEditorName = string.Empty;
            ServingGroupEditorServingRule = "PerTable";
            ServingGroupEditorNote = string.Empty;
        }

        OnPropertyChanged(nameof(HasServingGroupEditorRows));
        OnPropertyChanged(nameof(HasServingGroupSelectionRows));
        OnPropertyChanged(nameof(SelectedServingGroupTableSummary));
    }

    private void ServingGroupTableSelectionRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServingGroupTableSelectionRow.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedServingGroupTableSummary));
        }
    }

    private void ApplyServingGroupSelection(ServingGroupEditorRow row)
    {
        var selectedTableIds = SelectedOrder?.ServingGroups
            .FirstOrDefault(x => x.Id == row.Id)?
            .Tables
            .Select(x => x.RestaurantOrderTableId)
            .ToHashSet() ?? [];

        foreach (var table in ServingGroupTableSelectionRows)
        {
            table.IsSelected = selectedTableIds.Contains(table.OrderTableId) && table.CanSelect;
        }
    }

    private void RefreshPreparationQueueRows()
    {
        PreparationQueueRows.Clear();
        if (SelectedOrder?.Lines is null || SelectedOrder.Lines.Count == 0)
        {
            OnPropertyChanged(nameof(HasPreparationQueueRows));
            return;
        }

        var tableNameLookup = (SelectedOrder.Tables ?? Array.Empty<RestaurantOrderTableDto>())
            .Where(x => !x.IsVirtual)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().DisplayName);

        foreach (var line in SelectedOrder.Lines
                     .Where(x => !IsOrderServiceLine(x) && !string.Equals(x.LineStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => IsPendingPreparationLine(x) ? 0 : 1)
                     .ThenBy(x => x.LineNo))
        {
            tableNameLookup.TryGetValue(line.OrderTableId ?? Guid.Empty, out var tableName);
            PreparationQueueRows.Add(new PreparationQueueRow
            {
                MenuItemName = line.MenuItemName,
                TableName = string.IsNullOrWhiteSpace(tableName) ? "Bàn tổng / chưa gán bàn" : tableName,
                Qty = line.Qty,
                KitchenStatus = string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase) ? "Đã gửi" : "Chờ gửi",
                KitchenNote = line.KitchenNote,
                AccentKey = string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase) ? "Green" : "Orange"
            });
        }

        OnPropertyChanged(nameof(HasPreparationQueueRows));
    }

    private void RefreshSelectedOrderDetailLines()
    {
        Reset(SelectedOrderFoodLines, BuildSelectedOrderFoodLines());
        Reset(SelectedOrderWineLines, BuildSelectedOrderWineLines());
        Reset(SelectedOrderDrinkLines, BuildSelectedOrderDrinkLines());
        Reset(SelectedOrderServiceLines, BuildSelectedOrderServiceLines());
        Reset(SelectedOrderPreparationTickets, BuildSelectedOrderPreparationTickets());
        OnPropertyChanged(nameof(HasSelectedOrderPreparationTickets));
    }

    private IEnumerable<RestaurantPreparationTicketDto> BuildSelectedOrderPreparationTickets()
        => SelectedOrder?.PreparationTickets?.OrderByDescending(x => x.SentAt).ThenByDescending(x => x.TicketNo)
           ?? Enumerable.Empty<RestaurantPreparationTicketDto>();

    [RelayCommand]
    private void PreviewPreparationTicket(RestaurantPreparationTicketDto? ticket)
    {
        if (ticket is null)
        {
            return;
        }

        var preview = new PrintPreviewDialog(
            $"Phiếu {ticket.Station} - {ticket.TicketNo}",
            BuildPreparationTicketPreview(ticket));
        preview.Owner = System.Windows.Application.Current?.MainWindow;
        preview.ShowDialog();
    }

    private static string BuildPreparationTicketPreview(RestaurantPreparationTicketDto ticket)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Phiếu: {ticket.TicketNo}");
        builder.AppendLine($"Khu xử lý: {ticket.Station}");
        builder.AppendLine($"Loại phiếu: {ticket.TicketType}");
        builder.AppendLine($"Trạng thái: {ticket.Status}");
        builder.AppendLine($"Thời gian gửi: {ticket.SentAt:dd/MM/yyyy HH:mm:ss}");
        builder.AppendLine(new string('-', 48));

        if (ticket.Lines is null || ticket.Lines.Count == 0)
        {
            builder.AppendLine("Chưa có dòng món trong phiếu.");
            return builder.ToString();
        }

        for (var index = 0; index < ticket.Lines.Count; index++)
        {
            var line = ticket.Lines[index];
            builder.AppendLine($"{index + 1}. {line.MenuItemName} x{line.Qty:0.##}");

            if (line.SupplementQuantity > 0)
            {
                builder.AppendLine($"   Phần bù thực tế: +{line.SupplementQuantity:0.##}");
            }

            if (!string.IsNullOrWhiteSpace(line.KitchenNote))
            {
                builder.AppendLine($"   Ghi chú bếp/bar: {line.KitchenNote.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(line.SupplementNote))
            {
                builder.AppendLine($"   Ghi chú điều chỉnh: {line.SupplementNote.Trim()}");
            }
        }

        return builder.ToString();
    }

    private IEnumerable<AlacarteOrderLineDto> BuildSelectedOrderServiceLines()
    {
        var orderServiceLines = SelectedOrder?.Lines?.Where(IsOrderServiceLine).ToList();
        if (orderServiceLines is { Count: > 0 })
        {
            return IsSummaryTableSelected() ? AggregateOrderLines(orderServiceLines) : orderServiceLines;
        }

        var quoteServiceLines = GetSelectedOrderQuoteLines()?.Where(x => IsQuoteServiceLine(x.Type)).Select(ToOrderLineDisplay);
        if (quoteServiceLines is not null)
        {
            return IsSummaryTableSelected() ? AggregateOrderLines(quoteServiceLines) : quoteServiceLines;
        }

        return Enumerable.Empty<AlacarteOrderLineDto>();
    }

    private static bool IsOrderServiceLine(AlacarteOrderLineDto line)
        => string.Equals(line.LineType, "Dịch vụ", StringComparison.OrdinalIgnoreCase)
           || string.Equals(line.LineType, "Service", StringComparison.OrdinalIgnoreCase)
           || string.Equals(line.MenuItemCode, "SERVICE", StringComparison.OrdinalIgnoreCase);

    private IEnumerable<AlacarteOrderLineDto> BuildSelectedOrderFoodLines()
    {
        return BuildSelectedOrderNonServiceLines(line => !IsWineLine(line) && !IsDrinkLine(line));
    }

    private IEnumerable<AlacarteOrderLineDto> BuildSelectedOrderWineLines()
        => BuildSelectedOrderNonServiceLines(IsWineLine);

    private IEnumerable<AlacarteOrderLineDto> BuildSelectedOrderDrinkLines()
        => BuildSelectedOrderNonServiceLines(line => !IsWineLine(line) && IsDrinkLine(line));

    private IEnumerable<AlacarteOrderLineDto> BuildSelectedOrderNonServiceLines(Func<AlacarteOrderLineDto, bool> predicate)
    {
        var actualLines = SelectedOrder?.Lines?.Where(x => !IsOrderServiceLine(x) && predicate(x)).ToList();
        if (actualLines is { Count: > 0 })
        {
            if (SelectedTable is null)
            {
                return actualLines;
            }

            if (IsSummaryTableSelected())
            {
                return AggregateOrderLines(actualLines);
            }

            var selectedTableLines = actualLines.Where(x => x.OrderTableId == (SelectedTable.OrderTableId ?? SelectedTable.Id)).ToList();
            if (selectedTableLines.Count > 0)
            {
                return selectedTableLines;
            }

            var actualServingTableCount = SelectedOrder?.Tables?.Count(t => !t.IsVirtual) ?? 0;
            if (actualServingTableCount <= 1)
            {
                return actualLines;
            }

            return Enumerable.Empty<AlacarteOrderLineDto>();
        }

        if (GetSelectedOrderQuoteLines() is { } quoteLines)
        {
            var filteredQuoteLines = quoteLines
                .Where(x => !IsQuoteServiceLine(x.Type))
                .Select(ToOrderLineDisplay)
                .Where(predicate)
                .ToList();
            return IsSummaryTableSelected() ? AggregateOrderLines(filteredQuoteLines) : filteredQuoteLines;
        }

        return Enumerable.Empty<AlacarteOrderLineDto>();
    }

    private bool IsWineLine(AlacarteOrderLineDto line)
        => string.Equals(ResolveConfiguredOrderLineType(line), "Rượu", StringComparison.OrdinalIgnoreCase)
           || MatchesWineCategory(ResolveOrderLineCategoryName(line), line.MenuItemName, line.MenuItemCode);

    private bool IsDrinkLine(AlacarteOrderLineDto line)
        => string.Equals(ResolveConfiguredOrderLineType(line), "Đồ uống", StringComparison.OrdinalIgnoreCase)
           || MatchesDrinkCategory(ResolveOrderLineCategoryName(line), line.MenuItemName, line.MenuItemCode);

    private string ResolveConfiguredOrderLineType(AlacarteOrderLineDto line)
    {
        if (line.MenuItemId != Guid.Empty && _menuItemTypeOverrides.TryGetValue(line.MenuItemId, out var itemType))
        {
            return itemType;
        }

        return string.Empty;
    }

    private string ResolveOrderLineCategoryName(AlacarteOrderLineDto line)
    {
        if (line.MenuItemId != Guid.Empty)
        {
            var menuItem = _allMenuItems.FirstOrDefault(x => x.Id == line.MenuItemId);
            if (menuItem is not null)
            {
                return menuItem.CategoryName ?? string.Empty;
            }
        }

        var fallback = _allMenuItems.FirstOrDefault(x =>
            string.Equals(x.Code, line.MenuItemCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Name, line.MenuItemName, StringComparison.OrdinalIgnoreCase));
        return fallback?.CategoryName ?? string.Empty;
    }

    private static bool MatchesWineCategory(string? categoryName, string? itemName, string? itemCode)
    {
        var haystack = $"{categoryName} {itemName} {itemCode}".ToLowerInvariant();
        return haystack.Contains("rượu")
               || haystack.Contains("ruou")
               || haystack.Contains("wine")
               || haystack.Contains("whisky")
               || haystack.Contains("whiskey")
               || haystack.Contains("vodka")
               || haystack.Contains("brandy")
               || haystack.Contains("gin")
               || haystack.Contains("rum")
               || haystack.Contains("tequila")
               || haystack.Contains("champagne");
    }

    private static bool MatchesDrinkCategory(string? categoryName, string? itemName, string? itemCode)
    {
        var haystack = $"{categoryName} {itemName} {itemCode}".ToLowerInvariant();
        return haystack.Contains("đồ uống")
               || haystack.Contains("do uong")
               || haystack.Contains("nước")
               || haystack.Contains("nuoc")
               || haystack.Contains("trà")
               || haystack.Contains("tra")
               || haystack.Contains("cà phê")
               || haystack.Contains("ca phe")
               || haystack.Contains("coffee")
               || haystack.Contains("juice")
               || haystack.Contains("soda")
               || haystack.Contains("soft drink")
               || haystack.Contains("smoothie")
               || haystack.Contains("bia")
               || haystack.Contains("beer");
    }

    private bool IsSummaryTableSelected()
        => SelectedTable is not null
           && (SelectedTable.Id == Guid.Empty
               || string.Equals(SelectedTable.Name, "Bàn Tổng", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<AlacarteOrderLineDto> AggregateOrderLines(IEnumerable<AlacarteOrderLineDto> lines)
        => lines
            .GroupBy(line => new
            {
                line.MenuItemId,
                line.MenuItemCode,
                line.MenuItemName,
                line.UnitName,
                line.UnitPrice,
                line.LineType,
                line.PricingRule
            })
            .Select(group =>
            {
                var first = group
                    .OrderBy(x => x.LineNo)
                    .ThenBy(x => x.MenuItemName, StringComparer.OrdinalIgnoreCase)
                    .First();

                return new AlacarteOrderLineDto
                {
                    Id = first.Id,
                    OrderTableId = null,
                    LineNo = first.LineNo,
                    MenuItemId = first.MenuItemId,
                    MenuItemCode = first.MenuItemCode,
                    MenuItemName = first.MenuItemName,
                    UnitName = first.UnitName,
                    Qty = group.Sum(x => x.Qty),
                    UnitPrice = first.UnitPrice,
                    VatRate = first.VatRate,
                    VatAmount = group.Sum(x => x.VatAmount),
                    DiscountAmount = group.Sum(x => x.DiscountAmount),
                    Amount = group.Sum(x => x.Amount),
                    SupplementQuantity = group.Sum(x => x.SupplementQuantity),
                    KitchenNote = group.Select(x => x.KitchenNote).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    SupplementNote = group.Select(x => x.SupplementNote).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    LineStatus = group.All(x => string.Equals(x.LineStatus, first.LineStatus, StringComparison.OrdinalIgnoreCase))
                        ? first.LineStatus
                        : "Ordered",
                    KitchenStatus = group.All(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
                        ? "Sent"
                        : first.KitchenStatus,
                    LineType = first.LineType,
                    PricingRule = first.PricingRule,
                    IsSupplement = group.Any(x => x.IsSupplement)
                };
            })
            .OrderBy(x => x.LineNo)
            .ThenBy(x => x.MenuItemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private IEnumerable<SavedQuoteLineDto>? GetSelectedOrderQuoteLines()
        => GetSelectedOrderQuote()?.Lines;

    private SavedQuoteDto? GetSelectedOrderQuote()
    {
        if (SelectedOrder is null)
        {
            return null;
        }

        return SavedQuotes.FirstOrDefault(x => x.ApprovedOrderId == SelectedOrder.Id)
               ?? SavedQuotes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(SelectedOrder.SourceQuoteNo)
                                                  && string.Equals(x.QuoteNo, SelectedOrder.SourceQuoteNo, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<DiningTableDto> BuildQuoteServiceTables(SavedQuoteDto quote)
    {
        if (quote.Tables.Count > 0)
        {
            foreach (var table in quote.Tables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder))
            {
                yield return new DiningTableDto
                {
                    Id = table.Id,
                    Code = table.TableNo <= 0 ? "ORDER-TOTAL" : $"ORDER-T{table.TableNo:00}",
                    Name = table.DisplayName,
                    RoomName = quote.RoomName,
                    SeatCount = table.GuestCount,
                    Status = "Đang phục vụ",
                    SortOrder = table.SortOrder,
                    IsActive = true
                };
            }
            yield break;
        }

        var tableCount = Math.Max(1, quote.TableCount);
        var guestsPerTable = Math.Max(1, (int)Math.Ceiling(Math.Max(1, quote.GuestCount) / (double)tableCount));
        for (var i = 1; i <= tableCount; i++)
        {
            yield return new DiningTableDto
            {
                Id = Guid.NewGuid(),
                Code = $"ORDER-T{i:00}",
                Name = $"Bàn {i:00}",
                RoomName = quote.RoomName,
                SeatCount = guestsPerTable,
                Status = "Đang phục vụ",
                SortOrder = i,
                IsActive = true
            };
        }
    }

    private IEnumerable<DiningTableDto> BuildSelectedOrderRoomTables()
    {
        if (SelectedOrder?.Tables?.Count > 0)
        {
            foreach (var table in SelectedOrder.Tables.OrderBy(x => x.SortOrder))
            {
                yield return new DiningTableDto
                {
                    Id = table.IsVirtual ? Guid.Empty : Guid.NewGuid(),
                    OrderTableId = table.Id,
                    Code = table.IsVirtual ? "ORDER-TOTAL" : $"ORDER-T{table.TableNo:00}",
                    Name = table.DisplayName,
                    RoomName = SelectedOrder.RoomName ?? string.Empty,
                    SeatCount = table.GuestCount,
                    TableAmount = (SelectedOrder.Lines ?? Array.Empty<AlacarteOrderLineDto>())
                        .Where(l => l.OrderTableId == table.Id)
                        .Sum(l => l.Amount),
                    Status = table.Status,
                    SortOrder = table.SortOrder,
                    IsActive = true
                };
            }
            yield break;
        }

        var servingTables = GetSelectedOrderServingTables();
        var activeTableCount = servingTables.Count(t => string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                                                     || string.Equals(t.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase));
        if (SelectedOrder is not null && servingTables.Count > 0)
        {
            yield return new DiningTableDto
            {
                Id = Guid.Empty,
                Code = "ORDER-TOTAL",
                Name = "Bàn Tổng",
                RoomName = SelectedOrder.RoomName ?? string.Empty,
                SeatCount = Math.Max(0, SelectedOrder.GuestCount),
                TableAmount = SelectedOrder.TotalAmount,
                Status = $"{activeTableCount} bàn đang phục vụ",
                SortOrder = 0,
                IsActive = true
            };
        }

        foreach (var table in servingTables)
        {
            yield return table;
        }
    }

    private List<DiningTableDto> GetSelectedOrderServingTables()
    {
        if (SelectedOrder is not null)
        {
            var roomId = SelectedOrder.DiningRoomId
                         ?? Rooms.FirstOrDefault(r => string.Equals(r.Name, SelectedOrder.RoomName, StringComparison.OrdinalIgnoreCase))?.Id;
            var roomName = SelectedOrder.RoomName
                           ?? Tables.FirstOrDefault(t => t.Id == SelectedOrder.DiningTableId)?.RoomName
                           ?? SelectedTable?.RoomName;

            var roomTables = Tables
                .Where(t => !string.IsNullOrWhiteSpace(roomName)
                    ? string.Equals(t.RoomName, roomName, StringComparison.OrdinalIgnoreCase)
                    : roomId.HasValue && t.DiningRoomId == roomId.Value)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Code)
                .Select(t => new DiningTableDto
                {
                    Id = t.Id,
                    OrderTableId = t.OrderTableId,
                    DiningRoomId = t.DiningRoomId,
                    RoomName = t.RoomName,
                    Code = t.Code,
                    Name = t.Name,
                    SeatCount = t.SeatCount,
                    Status = t.Status,
                    Note = t.Note,
                    IsActive = t.IsActive,
                    SortOrder = t.SortOrder
                })
                .ToList();

            if (roomTables.Count > 0)
            {
                var lineAmountLookup = (SelectedOrder.Lines ?? Array.Empty<AlacarteOrderLineDto>())
                    .Where(l => l.OrderTableId.HasValue)
                    .GroupBy(l => l.OrderTableId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                var servingLookup = (SelectedOrder.Tables ?? Array.Empty<RestaurantOrderTableDto>())
                    .Where(t => !t.IsVirtual && t.DiningTableId.HasValue)
                    .GroupBy(t => t.DiningTableId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var table in roomTables)
                {
                    if (servingLookup.TryGetValue(table.Id, out var servingTable))
                    {
                        table.OrderTableId = servingTable.Id;
                        table.Status = NormalizeOrderTableStatus(servingTable.Status);
                        table.SeatCount = servingTable.GuestCount > 0 ? servingTable.GuestCount : table.SeatCount;
                    }
                    else if (string.Equals(table.Status, "Serving", StringComparison.OrdinalIgnoreCase))
                    {
                        table.Status = "Trống";
                    }
                    else if (string.Equals(table.Status, "Empty", StringComparison.OrdinalIgnoreCase))
                    {
                        table.Status = "Trống";
                    }

                    if (table.OrderTableId.HasValue && lineAmountLookup.TryGetValue(table.OrderTableId.Value, out var tableAmount))
                    {
                        table.TableAmount = tableAmount;
                    }
                }

                var servingTableIds = roomTables
                    .Where(t => string.Equals(t.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(t.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Id)
                    .ToList();

                if (SelectedOrder.TotalAmount > 0
                    && servingTableIds.Count == 1
                    && roomTables.All(t => t.TableAmount <= 0 || t.Id == servingTableIds[0]))
                {
                    var onlyServingTable = roomTables.FirstOrDefault(t => t.Id == servingTableIds[0]);
                    if (onlyServingTable is not null)
                    {
                        onlyServingTable.TableAmount = SelectedOrder.TotalAmount;
                    }
                }

                return roomTables;
            }
        }

        if (SelectedOrder?.Tables?.Count > 0)
        {
            return SelectedOrder.Tables
                .Where(t => !t.IsVirtual)
                .OrderBy(t => t.SortOrder)
                .Select(t => new DiningTableDto
                {
                    Id = t.DiningTableId ?? t.Id,
                    OrderTableId = t.Id,
                    Code = t.TableNo <= 0 ? "ORDER-TOTAL" : $"ORDER-T{t.TableNo:00}",
                    Name = t.DisplayName,
                    RoomName = SelectedOrder.RoomName ?? string.Empty,
                    SeatCount = t.GuestCount,
                    TableAmount = (SelectedOrder.Lines ?? Array.Empty<AlacarteOrderLineDto>())
                        .Where(l => l.OrderTableId == t.Id)
                        .Sum(l => l.Amount),
                    Status = NormalizeOrderTableStatus(t.Status),
                    SortOrder = t.SortOrder,
                    IsActive = true
                })
                .ToList();
        }

        if (GetSelectedOrderQuote() is { } quote)
        {
            return BuildQuoteServiceTables(quote).ToList();
        }

        if (SelectedOrder is null)
        {
            return [];
        }

        return Tables
            .Where(t => string.Equals(t.RoomName, SelectedOrder.RoomName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Code)
            .ToList();
    }

    private int GetSelectedOrderTableStandard()
    {
        var roomId = SelectedOrder?.DiningRoomId
                     ?? SelectedTable?.DiningRoomId;
        if (roomId.HasValue)
        {
            var seatStandard = Tables
                .Where(x => x.DiningRoomId == roomId.Value && x.SeatCount > 0)
                .Select(x => x.SeatCount)
                .DefaultIfEmpty(10)
                .Max();
            return Math.Max(1, seatStandard);
        }

        var servingStandard = GetSelectedOrderServingTables()
            .Where(x => x.SeatCount > 0)
            .Select(x => x.SeatCount)
            .DefaultIfEmpty(10)
            .Max();
        return Math.Max(1, servingStandard);
    }

    private static bool IsQuoteServiceLine(string? lineType)
        => string.Equals(lineType, "Dịch vụ", StringComparison.OrdinalIgnoreCase)
           || string.Equals(lineType, "Service", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeOrderTableStatus(string? status)
    {
        if (string.Equals(status, "Serving", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
        {
            return "Đang phục vụ";
        }

        if (string.Equals(status, "Waiting", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Locked", StringComparison.OrdinalIgnoreCase))
        {
            return "Bàn chờ";
        }

        if (string.Equals(status, "Empty", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Trống", StringComparison.OrdinalIgnoreCase))
        {
            return "Trống";
        }

        return string.IsNullOrWhiteSpace(status) ? "Trống" : status;
    }

    private static AlacarteOrderLineDto ToOrderLineDisplay(SavedQuoteLineDto line)
        => new()
        {
            Id = Guid.NewGuid(),
            LineNo = 0,
            MenuItemId = line.MenuItemId ?? Guid.Empty,
            MenuItemCode = IsQuoteServiceLine(line.Type) ? "SERVICE" : "QUOTE",
            MenuItemName = line.Name,
            UnitName = IsQuoteServiceLine(line.Type) ? "dịch vụ" : "món",
            Qty = line.CalculatedQuantity > 0 ? line.CalculatedQuantity : line.Quantity,
            UnitPrice = line.UnitPrice,
            Amount = line.Total,
            LineType = line.Type,
            PricingRule = line.PricingRule,
            LineStatus = "Ordered",
            KitchenStatus = IsQuoteServiceLine(line.Type) ? "NotRequired" : "New"
        };

    [RelayCommand]
    private void NavigateTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index)) SelectedTabIndex = index;
    }

    [RelayCommand]
    private void NavigateOrderDetailTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index)) OrderDetailTabIndex = index;
    }

    [RelayCommand]
    private void SelectOpenOrder(AlacarteOrderDto? order)
    {
        if (order is not null)
        {
            SelectedOrder = order;
            StatusText = $"Đang xem đơn {order.OrderNo} - {order.RoomName ?? "chưa rõ phòng"}.";
        }
    }

    [RelayCommand]
    private void SelectOpenOrderTable(DiningTableDto? table)
    {
        if (table is null) return;
        SelectedTable = table;
        var tableOrder = OpenOrders.FirstOrDefault(x => x.DiningTableId == table.Id);
        if (tableOrder is not null)
        {
            SelectedOrder = tableOrder;
            SelectedTable = table;
            OrderDetailTabIndex = 0;
            OnPropertyChanged(nameof(SelectedOrderTableDisplayName));
        }
        else if (SelectedOrder is not null)
        {
            StatusText = $"Đang xem bàn {table.Name} thuộc đơn phòng {SelectedOrder.RoomName}.";
            OrderDetailTabIndex = 0;
            OnPropertyChanged(nameof(SelectedOrderTableDisplayName));
            RefreshSelectedOrderDetailLines();
            OnPropertyChanged(nameof(SelectedOrderRoomTables));
            OnPropertyChanged(nameof(SelectedOrderSummaryTable));
            OnPropertyChanged(nameof(SelectedOrderDetailTables));
            OnPropertyChanged(nameof(SelectedOrderRoomTableCount));
        }
    }

    [RelayCommand]
    private void OpenSummaryTableSetup()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var room = ResolveSelectedOrderRoom();
        if (room is null)
        {
            StatusText = "Không tìm thấy phòng để cấu hình bàn tổng.";
            return;
        }

        OpenRoomSetupDialog(room, SelectedOrder);
    }

    [RelayCommand]
    private void OpenRoomTableSetup(DiningTableDto? table)
    {
        if (SelectedOrder is null || table is null)
        {
            return;
        }

        var room = ResolveSelectedOrderRoom();
        if (room is null)
        {
            StatusText = "Không tìm thấy phòng để mở thêm bàn.";
            return;
        }

        if (string.Equals(table.Status, "Serving", StringComparison.OrdinalIgnoreCase)
            || string.Equals(table.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
        {
            SelectedTable = table;
            OrderDetailTabIndex = 0;
            return;
        }

        var currentServingCount = Math.Max(1, SelectedOrder.Tables?.Count(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)) ?? 1);
        OpenRoomSetupDialog(room, SelectedOrder, Math.Min(RoomSetupMaxTables, currentServingCount + 1));
    }

    [RelayCommand]
    private void CloseRoomSetupDialog()
    {
        IsRoomSetupDialogOpen = false;
    }

    [RelayCommand]
    private async Task SaveRoomSetupAsync()
    {
        if (RoomSetupRoom is null || !RoomSetupOrderId.HasValue)
        {
            StatusText = "Chưa có phòng để mở bàn.";
            return;
        }

        var maxTables = Math.Max(1, RoomSetupMaxTables);
        var safeGuestCount = Math.Max(1, RoomSetupGuestCount);
        var safeTableCount = Math.Max(1, Math.Min(RoomSetupTableCount, maxTables));
        var safeStartTime = NormalizeRoomSetupTime(RoomSetupStartTime, DateTime.Now.ToString("HH:mm"));
        var safeEndTime = NormalizeRoomSetupTime(RoomSetupEndTime, AddHoursToTimeText(safeStartTime, 4));

        if (!TryValidateRoomSetupWindow(safeStartTime, safeEndTime, out var timeError))
        {
            StatusText = timeError;
            return;
        }

        // Route quote-based orders through Change Request for full rebuild
        if (SelectedOrder is not null
            && SelectedOrder.RestaurantQuoteId.HasValue
            && SelectedOrder.OrderType == "Advance"
            && !SelectedOrder.Lines.Any(l => l.KitchenStatus == "Sent"))
        {
            await ApplyQuoteChangeForRoomSetup(safeGuestCount);
            return;
        }

        // Walk-in order flow (original)
        await RunBusyAsync(async () =>
        {
            var order = await _service.ConfigureRoomWalkInOrderAsync(new ConfigureRoomWalkInOrderRequest
            {
                OrderId = RoomSetupOrderId.Value,
                GuestCount = safeGuestCount,
                TableCountToOpen = safeTableCount,
                StartTime = safeStartTime,
                EndTime = safeEndTime,
                CustomerName = string.IsNullOrWhiteSpace(RoomSetupCustomerName) ? null : RoomSetupCustomerName.Trim(),
                Phone = RoomSetupPhone?.Trim() ?? string.Empty,
                Note = string.IsNullOrWhiteSpace(RoomSetupNote) ? null : RoomSetupNote.Trim(),
                UpdatedBy = PreparedByName
            });

            IsRoomSetupDialogOpen = false;
            Dashboard = await _service.GetDashboardAsync();
            Reset(Tables, await _service.GetTablesAsync());
            ApplyTableFilter();
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshOpenOrderRooms();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == order.Id) ?? order;
            SelectedOrderRoomFilter = null;
            ApplyOpenOrderFilter();
            NotifyCollectionState();
            StatusText = $"Đã mở {safeTableCount} bàn cho phòng {RoomSetupRoom.Name}.";
        });
    }

    private async Task ApplyQuoteChangeForRoomSetup(int newGuestCount)
    {
        var quoteId = SelectedOrder!.RestaurantQuoteId!.Value;
        var currentGuestCount = SelectedOrder.GuestCount;
        if (newGuestCount == currentGuestCount)
        {
            IsRoomSetupDialogOpen = false;
            StatusText = "Số khách không thay đổi.";
            return;
        }

        try
        {
            var preview = await _quoteChangeService.PreviewAdjustmentAsync(
                new PreviewAdjustmentRequest { QuoteId = quoteId, NewGuestCount = newGuestCount });
            if (!preview.Preview.CanApply)
            {
                StatusText = $"Không thể áp dụng: đơn đang '{preview.Preview.OrderStatus}'.";
                return;
            }
            if (preview.Warnings.Count > 0)
            {
                StatusText = $"Cảnh báo: {string.Join("; ", preview.Warnings)}";
            }

            var cr = await _quoteChangeService.RequestChangeAsync(new RequestChangeRequest
            {
                QuoteId = quoteId,
                NewGuestCount = newGuestCount,
                Reason = $"Đổi số khách từ {currentGuestCount} → {newGuestCount} qua Bàn Tổng",
                RequestedBy = PreparedByName
            });
            await _quoteChangeService.ApproveChangeRequestAsync(new ReviewChangeRequest
            {
                ChangeRequestId = cr.Id,
                ReviewedBy = PreparedByName
            });
            await _quoteChangeService.ApplyChangeAsync(new ApplyChangeRequest
            {
                ChangeRequestId = cr.Id,
                AppliedBy = PreparedByName
            });

            IsRoomSetupDialogOpen = false;
            await RefreshAsync();
            var newOrder = OpenOrders.FirstOrDefault(x => x.Id == cr.NewOrderId);
            if (newOrder is not null) SelectedOrder = newOrder;
            SelectedOrderRoomFilter = null;
            ApplyOpenOrderFilter();
            StatusText = $"Đã đổi số khách {currentGuestCount} → {newGuestCount}. {(preview.RequiresFullRebuild ? "Đã rebuild toàn bộ đơn." : "")}";
        }
        catch (NotSupportedException ex)
        {
            StatusText = ex.Message;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "ApplyQuoteChangeForRoomSetup failed");
            StatusText = $"Không đổi được số khách: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateServicesSubTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index)) ServicesSubTabIndex = index;
    }

    private DiningRoomDto? ResolveSelectedOrderRoom()
        => Rooms.FirstOrDefault(x => x.Id == SelectedOrder?.DiningRoomId)
           ?? Rooms.FirstOrDefault(x => string.Equals(x.Name, SelectedOrder?.RoomName, StringComparison.OrdinalIgnoreCase));

    public AlacarteRoomSetupDraft? CreateSummaryRoomSetupDraft(out string? error)
    {
        error = null;
        if (SelectedOrder is null)
        {
            error = "Chưa chọn đơn mở.";
            return null;
        }

        var room = ResolveSelectedOrderRoom();
        if (room is null)
        {
            error = "Không tìm thấy phòng để cấu hình bàn tổng.";
            return null;
        }

        return BuildRoomSetupDraft(room, SelectedOrder);
    }

    public AlacarteRoomSetupDraft? CreateRoomTableSetupDraft(DiningTableDto? table, out string? error)
    {
        error = null;
        if (SelectedOrder is null || table is null)
        {
            error = "Chưa có bàn để mở thêm.";
            return null;
        }

        var room = ResolveSelectedOrderRoom();
        if (room is null)
        {
            error = "Không tìm thấy phòng để mở thêm bàn.";
            return null;
        }

        var currentServingCount = Math.Max(1, SelectedOrder.Tables?.Count(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)) ?? 1);
        var preferredTableCount = string.Equals(table.Status, "Serving", StringComparison.OrdinalIgnoreCase)
            || string.Equals(table.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase)
            ? currentServingCount
            : Math.Min(Math.Max(1, Tables.Count(x => x.DiningRoomId == room.Id)), currentServingCount + 1);

        return BuildRoomSetupDraft(room, SelectedOrder, preferredTableCount);
    }

    public async Task ApplyRoomSetupDraftAsync(AlacarteRoomSetupDraft draft)
    {
        if (draft is null)
        {
            StatusText = "Thiếu thông tin mở bàn.";
            return;
        }

        var maxTables = Math.Max(1, draft.MaxTables);
        var safeGuestCount = Math.Max(1, draft.GuestCount);
        var safeTableCount = Math.Max(1, Math.Min(draft.TableCount, maxTables));
        var safeStartTime = NormalizeRoomSetupTime(draft.StartTime, DateTime.Now.ToString("HH:mm"));
        var safeEndTime = NormalizeRoomSetupTime(draft.EndTime, AddHoursToTimeText(safeStartTime, 4));
        var room = Rooms.FirstOrDefault(x => x.Id == draft.RoomId);
        if (room is null)
        {
            StatusText = "Không tìm thấy phòng để mở bàn.";
            return;
        }

        if (!TryValidateRoomSetupWindow(safeStartTime, safeEndTime, out var timeError))
        {
            StatusText = timeError;
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.ConfigureRoomWalkInOrderAsync(new ConfigureRoomWalkInOrderRequest
            {
                OrderId = draft.OrderId,
                GuestCount = safeGuestCount,
                TableCountToOpen = safeTableCount,
                StartTime = safeStartTime,
                EndTime = safeEndTime,
                CustomerName = string.IsNullOrWhiteSpace(draft.CustomerName) ? null : draft.CustomerName.Trim(),
                Phone = draft.Phone?.Trim() ?? string.Empty,
                Note = string.IsNullOrWhiteSpace(draft.Note) ? null : draft.Note.Trim(),
                UpdatedBy = PreparedByName
            });

            Dashboard = await _service.GetDashboardAsync();
            Reset(Tables, await _service.GetTablesAsync());
            ApplyTableFilter();
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshOpenOrderRooms();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == order.Id) ?? order;
            SelectedOrderRoomFilter = null;
            ApplyOpenOrderFilter();
            NotifyCollectionState();
            StatusText = $"Đã mở {safeTableCount} bàn cho phòng {room.Name}.";
        });
    }

    private void OpenRoomSetupDialog(DiningRoomDto room, AlacarteOrderDto order, int? preferredTableCount = null)
    {
        RoomSetupRoom = room;
        RoomSetupOrderId = order.Id;
        RoomSetupCustomerName = order.CustomerName ?? string.Empty;
        RoomSetupPhone = order.CustomerPhone ?? string.Empty;
        RoomSetupGuestCount = Math.Max(1, order.GuestCount > 0 ? order.GuestCount : 10);

        var currentServingCount = order.Tables?.Count(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)) ?? 0;
        var maxTables = Math.Max(1, Tables.Count(x => x.DiningRoomId == room.Id));
        var suggestedTableCount = Math.Max(1, (int)Math.Ceiling(RoomSetupGuestCount / 10d));
        RoomSetupTableCount = Math.Max(1, Math.Min(preferredTableCount ?? (currentServingCount > 0 ? currentServingCount : suggestedTableCount), maxTables));
        RoomSetupStartTime = NormalizeRoomSetupTime(order.StartTime, DateTime.Now.ToString("HH:mm"));
        RoomSetupEndTime = NormalizeRoomSetupTime(order.EndTime, AddHoursToTimeText(RoomSetupStartTime, 4));
        RoomSetupNote = order.Note ?? string.Empty;
        IsRoomSetupDialogOpen = true;
    }

    private AlacarteRoomSetupDraft BuildRoomSetupDraft(DiningRoomDto room, AlacarteOrderDto order, int? preferredTableCount = null)
    {
        var currentServingCount = order.Tables?.Count(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)) ?? 0;
        var maxTables = Math.Max(1, Tables.Count(x => x.DiningRoomId == room.Id));
        var guestCount = Math.Max(1, order.GuestCount > 0 ? order.GuestCount : 10);
        var suggestedTableCount = Math.Max(1, (int)Math.Ceiling(guestCount / 10d));
        var tableCount = Math.Max(1, Math.Min(preferredTableCount ?? (currentServingCount > 0 ? currentServingCount : suggestedTableCount), maxTables));

        return new AlacarteRoomSetupDraft
        {
            OrderId = order.Id,
            RoomId = room.Id,
            RoomType = room.RoomType,
            RoomName = room.Name,
            CustomerName = order.CustomerName ?? string.Empty,
            Phone = order.CustomerPhone ?? string.Empty,
            GuestCount = guestCount,
            TableCount = tableCount,
            StartTime = NormalizeRoomSetupTime(order.StartTime, DateTime.Now.ToString("HH:mm")),
            EndTime = NormalizeRoomSetupTime(order.EndTime, AddHoursToTimeText(NormalizeRoomSetupTime(order.StartTime, DateTime.Now.ToString("HH:mm")), 4)),
            MaxTables = maxTables,
            Note = order.Note ?? string.Empty
        };
    }

    private static string NormalizeRoomSetupTime(string? value, string fallback)
    {
        if (TimeSpan.TryParse(value, out var time))
        {
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}";
        }

        return fallback;
    }

    private static string AddHoursToTimeText(string startTime, int hours)
    {
        if (TimeSpan.TryParse(startTime, out var time))
        {
            var end = time.Add(TimeSpan.FromHours(hours));
            if (end.TotalHours >= 24)
            {
                end = end.Subtract(TimeSpan.FromDays(Math.Floor(end.TotalHours / 24)));
            }

            return $"{(int)end.TotalHours:00}:{end.Minutes:00}";
        }

        return "21:00";
    }

    private static bool TryValidateRoomSetupWindow(string startTime, string endTime, out string error)
    {
        error = string.Empty;
        if (!TimeSpan.TryParse(startTime, out var start))
        {
            error = "Giờ bắt đầu chưa đúng định dạng HH:mm.";
            return false;
        }

        if (!TimeSpan.TryParse(endTime, out var end))
        {
            error = "Giờ kết thúc chưa đúng định dạng HH:mm.";
            return false;
        }

        if (start >= end)
        {
            error = "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
            return false;
        }

        return true;
    }
    [RelayCommand]
    private void NavigateQuoteSubTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index)) QuoteSubTabIndex = index;
    }

    [RelayCommand]
    private void SelectMenuCategory(MenuCategoryDto? cat)
    {
        SelectedMenuCategory = (cat == SelectedMenuCategory) ? null : cat;
    }

    [RelayCommand]
    private void NavigateSettingsSubTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index)) SettingsSubTabIndex = index;
    }

    [RelayCommand]
    private async Task SaveSettingDiningAreaAsync()
    {
        var areaName = SettingDiningAreaName.Trim();
        if (string.IsNullOrWhiteSpace(areaName))
        {
            StatusText = "Cần nhập tên khu vực.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var oldName = EditingSettingDiningAreaName?.Trim();
            if (!string.IsNullOrWhiteSpace(oldName)
                && !string.Equals(oldName, areaName, StringComparison.OrdinalIgnoreCase))
            {
                var roomsInArea = Rooms
                    .Where(x => string.Equals(x.RoomType, oldName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var room in roomsInArea)
                {
                    await _service.SaveRoomAsync(new SaveDiningRoomRequest
                    {
                        Id = room.Id,
                        Code = room.Code,
                        Name = room.Name,
                        RoomType = areaName,
                        Capacity = room.Capacity,
                        Description = room.Description,
                        Status = room.Status,
                        IsActive = room.IsActive
                    });
                }
            }

            if (_manualDiningAreas.All(x => !string.Equals(x, areaName, StringComparison.OrdinalIgnoreCase)))
            {
                _manualDiningAreas.Add(areaName);
            }

            if (!string.IsNullOrWhiteSpace(oldName)
                && !string.Equals(oldName, areaName, StringComparison.OrdinalIgnoreCase))
            {
                _manualDiningAreas.RemoveAll(x => string.Equals(x, oldName, StringComparison.OrdinalIgnoreCase));
                foreach (var room in Rooms.Where(x => string.Equals(x.RoomType, oldName, StringComparison.OrdinalIgnoreCase)))
                {
                    room.RoomType = areaName;
                }
            }

            SaveRestaurantSettings();
            RefreshSettingCollections();
            _ = RefreshAsync();
            SelectedSettingDiningAreaName = areaName;
            NewSettingDiningArea();
            OnPropertyChanged(nameof(FilteredSettingRooms));
            OnPropertyChanged(nameof(FilteredSettingTables));
            StatusText = $"Đã lưu khu vực {areaName}.";
        });
    }

    [RelayCommand]
    private void NewSettingDiningArea()
    {
        EditingSettingDiningAreaName = null;
        SettingDiningAreaName = string.Empty;
    }

    [RelayCommand]
    private void EditSettingDiningArea(AlacarteSettingDiningArea? area)
    {
        if (area is null) return;
        EditingSettingDiningAreaName = area.Name;
        SettingDiningAreaName = area.Name;
        SelectedSettingDiningAreaName = area.Name;
        OnPropertyChanged(nameof(FilteredSettingRooms));
        OnPropertyChanged(nameof(FilteredSettingTables));
    }

    [RelayCommand]
    private async Task DeleteSettingDiningAreaAsync(AlacarteSettingDiningArea? area)
    {
        if (area is null) return;

        var roomsInArea = Rooms
            .Where(x => string.Equals(x.RoomType, area.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (roomsInArea.Count > 0)
        {
            StatusText = "Khu vực vẫn còn phòng, vui lòng xóa hoặc chuyển phòng trước.";
            return;
        }

        _manualDiningAreas.RemoveAll(x => string.Equals(x, area.Name, StringComparison.OrdinalIgnoreCase));
        SaveRestaurantSettings();
        await RefreshAsync();

        if (string.Equals(EditingSettingDiningAreaName, area.Name, StringComparison.OrdinalIgnoreCase))
        {
            NewSettingDiningArea();
        }

        if (string.Equals(SelectedSettingDiningAreaName, area.Name, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSettingDiningAreaName = string.Empty;
        }

        StatusText = $"Đã xóa khu vực {area.Name}.";
    }

    [RelayCommand]
    private async Task SaveSettingAreaAsync()
    {
        await RunBusyAsync(async () =>
        {
            var room = await _service.SaveRoomAsync(new SaveDiningRoomRequest
            {
                Id = EditingSettingAreaId,
                Code = SettingAreaCode.Trim(),
                Name = SettingAreaName.Trim(),
                RoomType = string.IsNullOrWhiteSpace(SettingAreaType) ? "Standard" : SettingAreaType,
                Capacity = SettingAreaCapacity,
                Status = "Available",
                IsActive = SettingAreaIsActive
            });

            UpsertRoom(room);
            RefreshSettingCollections();
            _ = RefreshAsync();
            SelectedSettingDiningAreaName = room.RoomType;
            SelectedRoom = null;
            NewSettingArea();
            OnPropertyChanged(nameof(FilteredSettingRooms));
            OnPropertyChanged(nameof(FilteredSettingTables));
            StatusText = $"Đã lưu phòng ăn {room.Name}.";
        });
    }

    [RelayCommand]
    private void NewSettingArea()
    {
        EditingSettingAreaId = null;
        SettingAreaCode = string.Empty;
        SettingAreaName = string.Empty;
        SettingAreaType = string.Empty;
        SettingAreaCapacity = 40;
        SettingAreaIsActive = true;
    }

    [RelayCommand]
    private void EditSettingArea(AlacarteSettingArea? area)
    {
        if (area is null) return;
        var room = area.Id != Guid.Empty
            ? Rooms.FirstOrDefault(x => x.Id == area.Id)
            : Rooms.FirstOrDefault(x => string.Equals(x.Code, area.Code, StringComparison.OrdinalIgnoreCase))
              ?? Rooms.FirstOrDefault(x => string.Equals(x.Name, area.Name, StringComparison.OrdinalIgnoreCase));
        EditingSettingAreaId = room?.Id;
        SettingAreaCode = area.Code;
        SettingAreaName = area.Name;
        SettingAreaType = string.IsNullOrWhiteSpace(area.Type) ? "Standard" : area.Type;
        SettingAreaCapacity = area.Capacity;
        SettingAreaIsActive = area.IsActive;
        SelectedSettingDiningAreaName = SettingAreaType;
        if (room is not null)
        {
            SelectedRoom = room;
        }
    }

    [RelayCommand]
    private async Task DeleteSettingAreaAsync(AlacarteSettingArea? area)
    {
        if (area is null) return;
        var room = area.Id != Guid.Empty
            ? Rooms.FirstOrDefault(x => x.Id == area.Id)
            : Rooms.FirstOrDefault(x => string.Equals(x.Code, area.Code, StringComparison.OrdinalIgnoreCase))
              ?? Rooms.FirstOrDefault(x => string.Equals(x.Name, area.Name, StringComparison.OrdinalIgnoreCase));
        if (room is null)
        {
            StatusText = "Không tìm thấy phòng ăn cần xóa.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.DeleteRoomAsync(room.Id);
            await RefreshAsync();
            if (EditingSettingAreaId == room.Id)
            {
                NewSettingArea();
            }
            OnPropertyChanged(nameof(FilteredSettingRooms));
            OnPropertyChanged(nameof(FilteredSettingTables));
            StatusText = $"Đã xóa phòng ăn {room.Name}.";
        });
    }

    [RelayCommand]
    private async Task SaveSettingTableAsync()
    {
        var room = SettingTableRoomId.HasValue
            ? Rooms.FirstOrDefault(x => x.Id == SettingTableRoomId.Value)
            : null;
        room ??= Rooms.FirstOrDefault(x => string.Equals(x.Name, SettingTableRoomName, StringComparison.OrdinalIgnoreCase))
                   ?? Rooms.FirstOrDefault(x => string.Equals(x.Code, SettingTableRoomName, StringComparison.OrdinalIgnoreCase))
                   ?? SelectedRoom;
        if (room is null)
        {
            StatusText = "Cần chọn hoặc nhập đúng phòng ăn cho bàn.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var table = await _service.SaveTableAsync(new SaveDiningTableRequest
            {
                Id = EditingSettingTableId,
                DiningRoomId = room.Id,
                Code = SettingTableCode.Trim(),
                Name = SettingTableName.Trim(),
                SeatCount = SettingTableSeats,
                Status = "Empty",
                IsActive = true
            });

            UpsertTable(table);
            RefreshSettingCollections();
            _ = RefreshAsync();
            SelectedRoom = Rooms.FirstOrDefault(x => x.Id == room.Id) ?? room;
            SelectedTable = null;
            NewSettingTable();
            OnPropertyChanged(nameof(FilteredSettingTables));
            StatusText = $"Đã lưu bàn ăn {table.Name}.";
        });
    }

    [RelayCommand]
    private void NewSettingTable()
    {
        EditingSettingTableId = null;
        SettingTableCode = string.Empty;
        SettingTableName = string.Empty;
        SettingTableSeats = 4;
        SettingTableRoomId = SelectedRoom?.Id;
        SettingTableRoomName = SelectedRoom?.Name ?? string.Empty;
        OnPropertyChanged(nameof(FilteredSettingTables));
    }

    [RelayCommand]
    private void EditSettingTable(AlacarteSettingTable? table)
    {
        if (table is null) return;
        var dto = table.Id != Guid.Empty
            ? Tables.FirstOrDefault(x => x.Id == table.Id)
            : Tables.FirstOrDefault(x => string.Equals(x.Code, table.Code, StringComparison.OrdinalIgnoreCase))
              ?? Tables.FirstOrDefault(x => string.Equals(x.Name, table.Name, StringComparison.OrdinalIgnoreCase)
                                           && string.Equals(x.RoomName, table.RoomName, StringComparison.OrdinalIgnoreCase));
        EditingSettingTableId = dto?.Id;
        SettingTableCode = table.Code;
        SettingTableName = table.Name;
        SettingTableSeats = table.Seats;
        SettingTableRoomId = dto?.DiningRoomId;
        SettingTableRoomName = table.RoomName;
        if (dto is not null)
        {
            SelectedTable = dto;
            SelectedRoom = Rooms.FirstOrDefault(x => x.Id == dto.DiningRoomId) ?? SelectedRoom;
            SelectedSettingDiningAreaName = SelectedRoom?.RoomType ?? SelectedSettingDiningAreaName;
        }
        OnPropertyChanged(nameof(FilteredSettingTables));
    }

    [RelayCommand]
    private async Task DeleteSettingTableAsync(AlacarteSettingTable? table)
    {
        if (table is null) return;
        var dto = table.Id != Guid.Empty
            ? Tables.FirstOrDefault(x => x.Id == table.Id)
            : Tables.FirstOrDefault(x => string.Equals(x.Code, table.Code, StringComparison.OrdinalIgnoreCase))
              ?? Tables.FirstOrDefault(x => string.Equals(x.Name, table.Name, StringComparison.OrdinalIgnoreCase)
                                           && string.Equals(x.RoomName, table.RoomName, StringComparison.OrdinalIgnoreCase));
        if (dto is null)
        {
            StatusText = "Không tìm thấy bàn ăn cần xóa.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.DeleteTableAsync(dto.Id);
            await RefreshAsync();
            if (EditingSettingTableId == dto.Id)
            {
                NewSettingTable();
            }
            OnPropertyChanged(nameof(FilteredSettingTables));
            StatusText = $"Đã xóa bàn ăn {dto.Name}.";
        });
    }

    [RelayCommand]
    private async Task GenerateSettingTablesAsync()
    {
        var room = Rooms.FirstOrDefault(x => string.Equals(x.Name, SettingTableRoomName, StringComparison.OrdinalIgnoreCase))
                   ?? Rooms.FirstOrDefault(x => string.Equals(x.Code, SettingTableRoomName, StringComparison.OrdinalIgnoreCase))
                   ?? SelectedRoom;
        if (room is null)
        {
            StatusText = "Cần chọn phòng ăn trước khi tạo nhanh bàn.";
            return;
        }

        var prefix = string.IsNullOrWhiteSpace(SettingTableGenPrefix) ? "B" : SettingTableGenPrefix.Trim().ToUpperInvariant();
        var count = Math.Max(1, SettingTableGenCount);

        await RunBusyAsync(async () =>
        {
            for (var i = 1; i <= count; i++)
            {
                var code = $"{prefix}{i:00}";
                await _service.SaveTableAsync(new SaveDiningTableRequest
                {
                    DiningRoomId = room.Id,
                    Code = code,
                    Name = $"Bàn {i:00}",
                    SeatCount = SettingTableSeats,
                    Status = "Empty",
                    IsActive = true,
                    SortOrder = i
                });
            }

            await RefreshAsync();
            SelectedRoom = Rooms.FirstOrDefault(x => x.Id == room.Id) ?? room;
            StatusText = $"Đã tạo nhanh {count} bàn cho phòng {room.Name}.";
        });
    }

    [RelayCommand]
    private void SelectRoom(DiningRoomDto? room)
    {
        if (room is not null) SelectedRoom = room;
    }

    [RelayCommand]
    private void ClearRoomFilter()
    {
        SelectedRoom = null;
    }

    [RelayCommand]
    private void SelectTableAsync(DiningTableDto? table)
    {
        if (table is null) return;
        SelectedTable = table;
        _ = OpenOrderAsync(table);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            try
            {
                Dashboard = await _service.GetDashboardAsync() ?? new AlacarteDashboardDto();
                Reset(Rooms, await _service.GetRoomsAsync());
                Reset(Tables, await _service.GetTablesAsync());
                RefreshFloorsFromRooms();
                RefreshSettingCollections();
                ApplyTableFilter();
                Reset(Reservations, await _service.GetReservationsAsync());
                Reset(OpenOrders, await _service.GetOpenOrdersAsync());
                RefreshOpenOrderRooms();
                ApplyOpenOrderFilter();
                EnsureSelectedOpenOrder();
                RefreshRoomReservationCalendarState();
                Reset(Contracts, await _service.GetContractsAsync());
                await LoadExtraServicesAsync();
                NotifyCollectionState();
                ApplyTableFilter();
                SyncDashboardVisualState();
                StatusText = "Đã tải dữ liệu À La Carte.";
            }
            catch (Exception ex)
            {
                StatusText = $"Lỗi tải dữ liệu: {ex.Message}";
            }
        });
    }

    private void EnsureSelectedOpenOrder()
    {
        if (SelectedOrder is not null)
        {
            var matchedOrder = OpenOrders.FirstOrDefault(x => x.Id == SelectedOrder.Id);
            if (matchedOrder is null)
            {
                SelectedOrder = null;
                SelectedTable = null;
            }
            else if (!ReferenceEquals(matchedOrder, SelectedOrder))
            {
                SelectedOrder = matchedOrder;
            }
        }

        if (SelectedOrder is null && OpenOrders.Count > 0)
        {
            SelectedOrder = OpenOrders[0];
        }
        else
        {
            OnPropertyChanged(nameof(SelectedOrderRoomTables));
            OnPropertyChanged(nameof(SelectedOrderSummaryTable));
            OnPropertyChanged(nameof(SelectedOrderDetailTables));
            OnPropertyChanged(nameof(SelectedOrderRoomTableCount));
        }
    }

    [RelayCommand]
    private async Task BootstrapSampleDataAsync()
    {
        await RunBusyAsync(async () =>
        {
            var services = await _service.SeedDefaultExtraServicesAsync();
            Reset(AlacarteServices, services.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.DefaultPrice, "lần", x.Code, x.VatRate)));
            Reset(SettingExtServices, AlacarteServices.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.Price, x.Unit, x.Code, x.VatRate)));
            NotifyCollectionState();
            StatusText = "Đã khởi tạo dịch vụ bổ sung mẫu À La Carte (" + AlacarteServices.Count + " dịch vụ).";
        });
    }

    private void RefreshFloorsFromRooms()
    {
        var types = Rooms.Select(r => r.RoomType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (types.Count == 0) return;
        for (int i = Floors.Count - 1; i >= 0; i--)
        {
            if (!types.Contains(Floors[i])) Floors.RemoveAt(i);
        }
        foreach (var t in types)
        {
            if (!Floors.Contains(t)) Floors.Add(t);
        }
    }

    private void PopulateSampleDataInMemory()
    {
        if (Floors.Count == 0) { Floors.Add("Tầng 1"); Floors.Add("Tầng 2"); Floors.Add("Tầng 3"); Floors.Add("Tầng 4"); }

        if (Rooms.Count == 0)
        {
            var sampleRooms = new (string Floor, string Code, string Name, int MaxTables, int SeatPerTable)[]
            {
                ("Tầng 1", "T1-KA", "Khoang A",  6, 10),
                ("Tầng 1", "T1-KB", "Khoang B",  4, 10),
                ("Tầng 1", "T1-KC", "Khoang C",  8, 10),
                ("Tầng 2", "T2-V1", "VIP 1",     3, 10),
                ("Tầng 2", "T2-V2", "VIP 2",     3, 10),
                ("Tầng 2", "T2-V3", "VIP 3",     3, 10),
                ("Tầng 2", "T2-V4", "VIP 4",     3, 10),
                ("Tầng 2", "T2-V5", "VIP 5",     3, 10),
                ("Tầng 2", "T2-V6", "VIP 6",     3, 10),
                ("Tầng 2", "T2-V7", "VIP 7",     3, 10),
                ("Tầng 2", "T2-V8", "VIP 8",     3, 10),
                ("Tầng 3", "T3-V9", "VIP 9",     3, 10),
                ("Tầng 3", "T3-V10","VIP 10",    3, 10),
                ("Tầng 3", "T3-V11","VIP 11",    3, 10),
                ("Tầng 3", "T3-V12","VIP 12",    3, 10),
                ("Tầng 4", "T4-V1", "VIP 4-1",  10, 10),
                ("Tầng 4", "T4-V2", "VIP 4-2",  40, 10),
            };
            foreach (var (floor, code, name, maxTables, seats) in sampleRooms)
                Rooms.Add(new DiningRoomDto { Id = Guid.NewGuid(), Code = code, Name = name,
                    Capacity = maxTables * seats, RoomType = floor, Status = "Available", TableCount = maxTables });
        }

        if (Tables.Count == 0)
        {
            foreach (var room in Rooms)
            {
                for (int i = 1; i <= room.TableCount; i++)
                {
                    var status = room.Code == "T1-KA" ? "Serving" : "Empty";
                    Tables.Add(new DiningTableDto
                    {
                        Id = Guid.NewGuid(), DiningRoomId = room.Id, RoomName = room.Name,
                        Code = $"B{i:00}", Name = $"Bàn {i:00}", SeatCount = 10, Status = status
                    });
                }
            }
        }

        // Đặt bàn mẫu
        if (Reservations.Count == 0)
        {
            var a03 = Tables.FirstOrDefault(t => t.Name == "A03");
            var b04 = Tables.FirstOrDefault(t => t.Name == "B04");
            var a05 = Tables.FirstOrDefault(t => t.Name == "A05");

            Reservations.Add(new TableReservationDto
            {
                Id = Guid.NewGuid(), ReservationNo = "DB-2026-001",
                CustomerName = "Nguyễn Văn An", Phone = "0912 345 678",
                ReservationDateTime = DateTime.Today.AddHours(18), GuestCount = 6,
                DiningRoomId = a03?.DiningRoomId, RoomName = a03?.RoomName,
                DiningTableId = a03?.Id, TableName = a03?.Name, Status = "Confirmed"
            });
            Reservations.Add(new TableReservationDto
            {
                Id = Guid.NewGuid(), ReservationNo = "DB-2026-002",
                CustomerName = "Trần Thị Bích", Phone = "0987 654 321",
                ReservationDateTime = DateTime.Today.AddHours(19), GuestCount = 4,
                DiningRoomId = b04?.DiningRoomId, RoomName = b04?.RoomName,
                DiningTableId = b04?.Id, TableName = b04?.Name, Status = "Confirmed"
            });
            Reservations.Add(new TableReservationDto
            {
                Id = Guid.NewGuid(), ReservationNo = "DB-2026-003",
                CustomerName = "Hoàng Văn Nam", Phone = "0903 111 222",
                ReservationDateTime = DateTime.Today.AddHours(20), GuestCount = 6,
                DiningRoomId = a05?.DiningRoomId, RoomName = a05?.RoomName,
                DiningTableId = a05?.Id, TableName = a05?.Name, Status = "Confirmed"
            });
        }

        // Đơn mở mẫu
        if (OpenOrders.Count == 0)
        {
            var a04 = Tables.FirstOrDefault(t => t.Name == "A04");
            var order = new AlacarteOrderDto
            {
                Id = Guid.NewGuid(), OrderNo = "ALC-2026-001",
                DiningTableId = a04?.Id, TableName = "A04", RoomName = "VIP 1",
                OrderDate = DateTime.Now.AddHours(-2), Status = "Serving", PaymentStatus = "Unpaid",
                SubtotalAmount = 2230000, DiscountAmount = 100000, VatAmount = 170400,
                ServiceChargeAmount = 106500, TotalAmount = 2406900,
                PaidAmount = 0, RemainingAmount = 2406900,
                Note = "Không cay",
                Lines = new List<AlacarteOrderLineDto>
                {
                    new() { Id = Guid.NewGuid(), LineNo = 1, MenuItemCode = "M01", MenuItemName = "Tôm hùm nướng bơ tỏi", UnitName = "suất", Qty = 2, UnitPrice = 550000, Amount = 1100000 },
                    new() { Id = Guid.NewGuid(), LineNo = 2, MenuItemCode = "M02", MenuItemName = "Bò Úc nướng đá", UnitName = "suất", Qty = 2, UnitPrice = 450000, Amount = 900000 },
                    new() { Id = Guid.NewGuid(), LineNo = 3, MenuItemCode = "M03", MenuItemName = "Súp hải sản", UnitName = "suất", Qty = 1, UnitPrice = 120000, Amount = 120000 },
                    new() { Id = Guid.NewGuid(), LineNo = 4, MenuItemCode = "M04", MenuItemName = "Salad dầu giấm", UnitName = "suất", Qty = 1, UnitPrice = 80000, Amount = 80000 },
                    new() { Id = Guid.NewGuid(), LineNo = 5, MenuItemCode = "M05", MenuItemName = "Nước suối", UnitName = "chai", Qty = 2, UnitPrice = 15000, Amount = 30000 },
                }
            };
            OpenOrders.Add(order);
        }

        PopulateSampleContracts();
        ApplyTableFilter();
    }

    private void PopulateSampleContracts()
    {
        if (Contracts.Count > 0) return;
        Contracts.Add(new RestaurantContractDto
        {
            Id = Guid.NewGuid(), ContractNo = "HD-ALC-0001",
            EventDescription = "Tiệc sinh nhật - VIP 1 - Nguyễn Văn An",
            TotalAmount = 8500000, DepositAmount = 3000000, RemainingAmount = 5500000,
            Status = "Active"
        });
        Contracts.Add(new RestaurantContractDto
        {
            Id = Guid.NewGuid(), ContractNo = "HD-ALC-0002",
            EventDescription = "Họp mặt đối tác - Tầng 1 - Trần Thị Bích",
            TotalAmount = 12000000, DepositAmount = 5000000, RemainingAmount = 7000000,
            Status = "Draft"
        });
        Contracts.Add(new RestaurantContractDto
        {
            Id = Guid.NewGuid(), ContractNo = "HD-ALC-0003",
            EventDescription = "Tiệc cưới - Sân vườn - Hoàng Văn Nam",
            TotalAmount = 45000000, DepositAmount = 15000000, RemainingAmount = 30000000,
            Status = "Active"
        });
    }

    [RelayCommand]
    private async Task PayOrder()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn cần thanh toán.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var orderId = SelectedOrder.Id;
            var orderNo = SelectedOrder.OrderNo;
            var roomName = SelectedOrder.RoomName ?? "—";
            var guestCount = SelectedOrder.GuestCount;
            var orderDate = SelectedOrder.OrderDate;
            var paid = await _service.PayOrderAsync(new PayAlacarteOrderRequest
            {
                OrderId = orderId,
                PaidAmount = SelectedOrder.RemainingAmount,
                PaidBy = PreparedByName,
                PaymentMethod = "Tiền mặt"
            });
            Dashboard = await _service.GetDashboardAsync();
            Reset(Rooms, await _service.GetRoomsAsync());
            Reset(Tables, await _service.GetTablesAsync());
            Reset(Reservations, await _service.GetReservationsAsync());
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshFloorsFromRooms();
            RefreshOpenOrderRooms();
            ApplyTableFilter();
            ApplyOpenOrderFilter();
            RefreshRoomReservationCalendarState();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == paid.Id);
            if (SelectedOrder is null) SelectedTable = null;
            EnsureSelectedOpenOrder();
            NotifyCollectionState();
            SyncDashboardVisualState();
            StatusText = $"Đã thanh toán đơn {paid.OrderNo}.";

            // Build and show receipt
            var seller = PreparedByName;
            var foodLines = paid.Lines?.Where(l => l.LineStatus != "Cancelled" && l.LineType != "Dịch vụ").ToList() ?? [];
            var svcLines = paid.Lines?.Where(l => l.LineStatus != "Cancelled" && l.LineType == "Dịch vụ").ToList() ?? [];
            var doc = new System.Windows.Documents.FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11,
                PageWidth = 300
            };
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run("ASMILE — NHÀ HÀNG À LA CARTE"))
            { FontSize = 14, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"HÓA ĐƠN THANH TOÁN"))
            { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 4) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"{orderNo}  |  {orderDate:dd/MM/yyyy HH:mm}"))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"Phòng: {roomName}  |  {guestCount} khách"))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 2) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"Thu ngân: {seller}"))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(new string('-', 34)))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });

            var aggregatedFood = foodLines
                .GroupBy(l => l.MenuItemName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Qty), UnitPrice = g.First().UnitPrice, Amount = g.Sum(x => x.Amount) })
                .ToList();

            foreach (var line in aggregatedFood)
            {
                var p = new System.Windows.Documents.Paragraph
                { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) };
                p.Inlines.Add(new System.Windows.Documents.Run(line.Name)
                { FontWeight = System.Windows.FontWeights.SemiBold });
                p.Inlines.Add(new System.Windows.Documents.Run(
                    $"\n  {line.Qty:N0} × {line.UnitPrice:N0}đ = {line.Amount:N0}đ"));
                doc.Blocks.Add(p);
            }
            foreach (var svc in svcLines)
            {
                var p = new System.Windows.Documents.Paragraph
                { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) };
                p.Inlines.Add(new System.Windows.Documents.Run($"[DV] {svc.MenuItemName}")
                { FontWeight = System.Windows.FontWeights.SemiBold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)) });
                p.Inlines.Add(new System.Windows.Documents.Run(
                    $"\n  {svc.Qty:N0} × {svc.UnitPrice:N0}đ = {svc.Amount:N0}đ"));
                doc.Blocks.Add(p);
            }

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(new string('-', 34)))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"Tạm tính: {paid.SubtotalAmount:N0}đ  |  Giảm giá: {paid.DiscountAmount:N0}đ"))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            if (paid.VatAmount > 0)
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run($"VAT: {paid.VatAmount:N0}đ  |  Phí DV: {paid.ServiceChargeAmount:N0}đ"))
                { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(new string('-', 34)))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"TỔNG THANH TOÁN: {paid.TotalAmount:N0}đ"))
            { FontSize = 16, FontWeight = System.Windows.FontWeights.Bold, TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12)),
                Margin = new System.Windows.Thickness(0, 4, 0, 0) });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(new string('-', 34)))
            { FontSize = 10, TextAlignment = System.Windows.TextAlignment.Center });
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run("Cảm ơn quý khách — Hẹn gặp lại!"))
            { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 6, 0, 0) });

            // Auto-print attempt
            if (!string.IsNullOrWhiteSpace(ReceiptPrinterName))
            {
                try
                {
                    if (TryParseNetworkPrinterTarget(ReceiptPrinterName, out var host, out var port))
                    {
                        var thermalText = BuildAlacarteReceiptThermalText(paid, seller);
                        await SendRawThermalPrintAsync(host, port, thermalText);
                    }
                    else
                    {
                        PrintFlowDocumentToWindowsPrinter(doc, ReceiptPrinterName, $"Hoa don {orderNo}");
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Đã thanh toán nhưng chưa in được: {ex.Message}";
                }
            }

            // Show receipt popup
            var viewer = new System.Windows.Controls.FlowDocumentPageViewer { Document = doc };
            var printBtn = new System.Windows.Controls.Button
            {
                Content = "🖨️  In hóa đơn",
                Height = 34, Padding = new System.Windows.Thickness(16, 0, 16, 0),
                Margin = new System.Windows.Thickness(0, 6, 0, 2),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                FontSize = 13, FontWeight = System.Windows.FontWeights.SemiBold,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xF2, 0x99, 0x4A)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            printBtn.Click += (_, _) =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(ReceiptPrinterName))
                    {
                        if (TryParseNetworkPrinterTarget(ReceiptPrinterName, out var h, out var p))
                            _ = SendRawThermalPrintAsync(h, p, BuildAlacarteReceiptThermalText(paid, seller));
                        else
                            PrintFlowDocumentToWindowsPrinter(doc, ReceiptPrinterName, $"Hoa don {orderNo}");
                    }
                    else
                    {
                        var dlg = new System.Windows.Controls.PrintDialog();
                        if (dlg.ShowDialog() == true)
                            dlg.PrintDocument(
                                ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator,
                                $"Hoa don {orderNo}");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Không in được hóa đơn: {ex.Message}", "Lỗi in",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            };
            var panel = new System.Windows.Controls.DockPanel();
            System.Windows.Controls.DockPanel.SetDock(printBtn, System.Windows.Controls.Dock.Top);
            panel.Children.Add(printBtn);
            panel.Children.Add(viewer);
            var win = new System.Windows.Window
            {
                Title = $"Hóa đơn — {orderNo}",
                Width = 420, Height = 600,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow,
                Content = panel,
                WindowStyle = System.Windows.WindowStyle.ToolWindow
            };
            win.ShowDialog();
        });
    }

    private string BuildAlacarteReceiptThermalText(AlacarteOrderDto order, string seller)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\x1B\x40");
        sb.AppendLine("\x1B\x61\x01");
        sb.AppendLine("ASMILE - NHA HANG A LA CARTE");
        sb.AppendLine("HOA DON THANH TOAN");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"{order.OrderNo}  {order.OrderDate:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Phong: {order.RoomName ?? "-"}  |  {order.GuestCount} khach");
        sb.AppendLine($"Thu ngan: {seller}");
        sb.AppendLine(new string('-', 32));
        if (order.Lines is not null)
        {
            var aggregated = order.Lines
                .Where(x => x.LineStatus != "Cancelled")
                .GroupBy(x => x.MenuItemName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Qty), UnitPrice = g.First().UnitPrice, Amount = g.Sum(x => x.Amount) });
            foreach (var l in aggregated)
                sb.AppendLine($"{l.Name,-20}{l.Qty,4:N0} x{l.UnitPrice,8:N0}");
        }
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"{"TONG:",-20}{order.TotalAmount,12:N0}đ");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("Cam on quy khach!");
        sb.AppendLine("\x1B\x64\x04");
        sb.AppendLine("\x1B\x69");
        return sb.ToString();
    }

    [RelayCommand]
    private async Task CancelOrder()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn cần hủy.";
            return;
        }

        await CancelOrderCoreAsync(
            SelectedOrder,
            string.Equals(SelectedOrder.Status, "Draft", StringComparison.OrdinalIgnoreCase) ? "Xóa nháp mở nhầm" : "Khách hủy đơn",
            string.Equals(SelectedOrder.Status, "Draft", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task CancelOpenOrderAsync(AlacarteOrderDto? order)
    {
        if (order is null)
        {
            StatusText = "Vui lòng chọn đơn cần hủy.";
            return;
        }

        await CancelOrderCoreAsync(
            order,
            string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase) ? "Xóa nháp mở nhầm" : "Khách hủy đơn",
            string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task CancelRoomOrderAsync(DiningRoomDto? room)
    {
        if (room is null)
        {
            StatusText = "Vui lòng chọn phòng cần hủy đơn.";
            return;
        }

        var order = OpenOrders
            .Where(x => x.DiningRoomId == room.Id)
            .OrderByDescending(x => x.OrderDate)
            .FirstOrDefault();

        if (order is null)
        {
            StatusText = $"Phòng {room.Name} hiện chưa có đơn mở để hủy.";
            return;
        }

        await CancelOrderCoreAsync(
            order,
            string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase) ? "Xóa nháp mở nhầm" : $"Hủy đơn của phòng {room.Name}",
            string.Equals(order.Status, "Draft", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task FinalizeOrderAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở.";
            return;
        }
        if (!CanSendOrderToPreparation)
        {
            StatusText = "Đơn không có món chờ gửi bếp/bar.";
            return;
        }
        IsOrderFinalized = true;
        await SendToKitchenAsync();
        StatusText = $"Đã chốt và gửi bếp/bar cho đơn {SelectedOrder.OrderNo}.";
    }

        [RelayCommand]
    private async Task SendToKitchenAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở trước khi gửi bếp/bar.";
            return;
        }

        if (!CanSendOrderToPreparation)
        {
            StatusText = "Đơn hiện không còn món chờ gửi bếp/bar.";
            return;
        }

        var currentOrderId = SelectedOrder.Id;
        var currentOrderNo = SelectedOrder.OrderNo;

        await RunBusyAsync(async () =>
        {
            var tickets = await _service.SendToPreparationAsync(new SendAlacartePreparationRequest
            {
                OrderId = currentOrderId,
                SentBy = PreparedByName
            });

            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            ApplyTableFilter();
            ApplyOpenOrderFilter();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == currentOrderId);
            Dashboard = await _service.GetDashboardAsync();
            NotifyCollectionState();
            OrderItemPickerStepIndex = 2;

            var stationSummary = string.Join(", ", tickets.Select(x => x.Station).Distinct(StringComparer.OrdinalIgnoreCase));
            var lineCount = tickets.Sum(x => x.Lines.Count);
            StatusText = lineCount > 0
                ? $"Đã gửi {lineCount} dòng món tới {stationSummary} cho đơn {currentOrderNo}."
                : $"Đã gửi bếp/bar cho đơn {currentOrderNo}.";
        });
    }

    [RelayCommand]
    private async Task QuickAddOrderItem(PosMenuItemCard? item)
    {
        if (item is null || SelectedOrder is null) return;
        var savedQty = OrderItemPickerBaseQuantity;
        OrderItemPickerBaseQuantity = 1m;
        await AddSingleItemToOrderAsync(item);
        OrderItemPickerBaseQuantity = savedQty;
    }

    [RelayCommand]
    private async Task AddSelectedOrderItem()
    {
        if (SelectedOrderItemPickerMenuItem is null || SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn món và đơn mở.";
            return;
        }
        await AddSingleItemToOrderAsync(SelectedOrderItemPickerMenuItem);
    }

    private async Task AddSingleItemToOrderAsync(PosMenuItemCard item)
    {
        var qty = Math.Max(0.01m, OrderItemPickerBaseQuantity);
        var orderId = SelectedOrder!.Id;
        var tableIds = OrderItemPickerTableSelections.Count > 0
            ? OrderItemPickerTableSelections.Where(t => t.IsSelected).Select(t => t.OrderTableId).ToList()
            : SelectedOrderRoomTables.Where(t => t.OrderTableId.HasValue).Select(t => t.OrderTableId!.Value).ToList();
        if (tableIds.Count == 0)
        {
            StatusText = "Đơn mở chưa có bàn kích hoạt.";
            return;
        }

        AlacarteOrderDto? lastOrder = SelectedOrder;
        foreach (var tableId in tableIds)
        {
            lastOrder = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
            {
                OrderId = orderId,
                OrderTableId = tableId,
                MenuItemId = item.Id,
                Qty = tableIds.Count == 1 ? qty : Math.Ceiling(qty / (decimal)tableIds.Count),
                SupplementQuantity = 0,
                KitchenNote = null,
                SupplementNote = null
            });
        }

        if (lastOrder is not null)
        {
            UpsertOpenOrder(lastOrder);
            SelectedOrder = lastOrder;
            OnPropertyChanged(nameof(SelectedOrderFoodLines));
            OnPropertyChanged(nameof(SelectedOrderWineLines));
            OnPropertyChanged(nameof(SelectedOrderDrinkLines));
            OnPropertyChanged(nameof(SelectedOrderServiceLines));
            StatusText = $"Đã thêm {item.Name} x{qty:N0} vào đơn {SelectedOrder.OrderNo}.";
        }
    }

    [RelayCommand]
    private void SetOrderItemPickerQuantity(string qty)
    {
        if (decimal.TryParse(qty, out var value) && value > 0)
            OrderItemPickerBaseQuantity = value;
    }

    [RelayCommand]
    private void IncrementOrderItemPickerQuantity()
    {
        OrderItemPickerBaseQuantity = Math.Min(999, OrderItemPickerBaseQuantity + 1);
    }

    [RelayCommand]
    private void DecrementOrderItemPickerQuantity()
    {
        OrderItemPickerBaseQuantity = Math.Max(1, OrderItemPickerBaseQuantity - 1);
    }

    [RelayCommand]
    private void OpenOrderItemPicker()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở trước khi thêm món hoặc dịch vụ.";
            return;
        }

        OrderItemPickerTabIndex = IsOrderServiceTab ? 1 : 0;
        ApplyOrderItemPickerFilters();
        SelectedOrderItemPickerMenuItem ??= OrderItemPickerMenuItems.FirstOrDefault();
        RefreshRoomServingPlanRows();
        RefreshServingGroupSummaries();
        NotifyPreparationQueueState();
        RefreshOrderItemPickerPlan();
        IsOrderItemPickerMinimized = false;
        OrderItemPickerStepIndex = OrderItemPickerTabIndex == 1 ? 2 : 0;
        RefreshOrderItemPickerTables();
        IsOrderItemPickerOpen = true;
    }

    [RelayCommand]
    private void CloseOrderItemPicker()
    {
        IsOrderItemPickerMinimized = false;
        IsOrderItemPickerOpen = false;
    }

    public ObservableCollection<OrderItemPickerTableSelection> OrderItemPickerTableSelections { get; } = new();

    private void RefreshOrderItemPickerTables()
    {
        Reset(OrderItemPickerTableSelections,
            SelectedOrderDetailTables.Select(t => new OrderItemPickerTableSelection
            {
                OrderTableId = t.OrderTableId ?? t.Id,
                Name = string.IsNullOrWhiteSpace(t.Code) ? t.Name : $"{t.Code} - {t.Name}",
                IsSelected = true
            }));
    }

    [RelayCommand]
    private void SetOrderItemPickerScope(string scopeValue)
    {
        if (string.IsNullOrWhiteSpace(scopeValue)) return;
        SelectedOrderItemPickerScope = scopeValue;
    }

    [RelayCommand]
    private void SetOrderItemPickerServingRule(string ruleValue)
    {
        if (string.IsNullOrWhiteSpace(ruleValue)) return;
        SelectedOrderItemPickerServingRule = ruleValue;
    }

    [RelayCommand]
    private void ToggleOrderItemPickerTable(OrderItemPickerTableSelection? table)
    {
        if (table is not null)
            table.IsSelected = !table.IsSelected;
    }

    private async Task CancelOrderCoreAsync(AlacarteOrderDto order, string reason, bool isDraftDelete)
    {
        await RunBusyAsync(async () =>
        {
            var cancelled = await _service.CancelOrderAsync(new CancelAlacarteOrderRequest
            {
                OrderId = order.Id,
                Reason = reason,
                CancelledBy = PreparedByName
            });

            Dashboard = await _service.GetDashboardAsync();
            Reset(Rooms, await _service.GetRoomsAsync());
            Reset(Tables, await _service.GetTablesAsync());
            Reset(Reservations, await _service.GetReservationsAsync());
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshFloorsFromRooms();
            RefreshOpenOrderRooms();
            ApplyTableFilter();
            ApplyOpenOrderFilter();
            RefreshRoomReservationCalendarState();

            if (SelectedOrder?.Id == cancelled.Id)
            {
                SelectedOrder = null;
            }

            EnsureSelectedOpenOrder();
            NotifyCollectionState();
            SyncDashboardVisualState();
            StatusText = isDraftDelete
                ? $"Đã xóa đơn nháp {cancelled.OrderNo}."
                : $"Đã hủy đơn {cancelled.OrderNo}.";
        });
    }

    [RelayCommand]
    private void ToggleOrderItemPickerMinimized()
    {
        IsOrderItemPickerMinimized = !IsOrderItemPickerMinimized;
    }

    [RelayCommand]
    private void HideOrderItemPicker()
    {
        IsOrderItemPickerOpen = false;
    }

    [RelayCommand]
    private void NavigateOrderItemPickerTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            OrderItemPickerTabIndex = Math.Clamp(index, 0, 1);
            OrderItemPickerStepIndex = OrderItemPickerTabIndex == 1 ? 2 : Math.Min(OrderItemPickerStepIndex, 1);
        }
    }

    [RelayCommand]
    private void NavigateOrderItemPickerStep(string? stepIndex)
    {
        if (!int.TryParse(stepIndex, out var index))
        {
            return;
        }

        OrderItemPickerStepIndex = Math.Clamp(index, 0, 2);
    }

    [RelayCommand]
    private void SelectOrderItemPickerCategory(string? category)
    {
        SelectedOrderItemPickerCategory = string.IsNullOrWhiteSpace(category) ? "Tất cả" : category;
    }

    [RelayCommand]
    private void ApplyServingGroupScope(string? scopeValue)
    {
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return;
        }

        SelectedOrderItemPickerScope = scopeValue;
        if (string.Equals(scopeValue, "SelectedTable", StringComparison.OrdinalIgnoreCase) && SelectedTable is null)
        {
            StatusText = "Vui lòng chọn một bàn cụ thể trước khi áp dụng phạm vi bàn đang chọn.";
        }
        else
        {
            StatusText = $"Đã chọn phạm vi áp dụng: {OrderItemPickerScopeOptions.FirstOrDefault(x => string.Equals(x.Value, scopeValue, StringComparison.OrdinalIgnoreCase))?.Label ?? scopeValue}.";
        }

        OrderItemPickerStepIndex = 1;
    }

    [RelayCommand]
    private void OpenServingPlanDialog()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn phòng trước khi chỉnh cấu hình bàn.";
            return;
        }

        RefreshRoomServingPlanRows();
        RefreshServingGroupSummaries();
        RefreshServingGroupEditor();
        IsServingPlanDialogMinimized = false;
        IsServingPlanDialogOpen = true;
    }

    [RelayCommand]
    private void CloseServingPlanDialog()
    {
        IsServingPlanDialogMinimized = false;
        IsServingPlanDialogOpen = false;
    }

    [RelayCommand]
    private void ToggleServingPlanDialogMinimized()
    {
        IsServingPlanDialogMinimized = !IsServingPlanDialogMinimized;
    }

    [RelayCommand]
    private void HideServingPlanDialog()
    {
        IsServingPlanDialogOpen = false;
    }

    [RelayCommand]
    private void NavigateServingPlanTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            ServingPlanTabIndex = Math.Clamp(index, 0, 2);
        }
    }

    [RelayCommand]
    private void NewServingGroup()
    {
        SelectedServingGroupEditor = null;

        var suggestedName = $"Nhóm phục vụ {ServingGroupEditorRows.Count(x => !x.IsSystemDefined) + 1}";
        ServingGroupEditorName = suggestedName;
        ServingGroupEditorServingRule = "PerTable";
        ServingGroupEditorNote = string.Empty;

        var selectedOrderTableId = SelectedTable?.OrderTableId ?? SelectedTable?.Id;
        foreach (var table in ServingGroupTableSelectionRows)
        {
            table.IsSelected = table.CanSelect && selectedOrderTableId.HasValue && table.OrderTableId == selectedOrderTableId.Value;
        }

        OnPropertyChanged(nameof(CanEditSelectedServingGroup));
        OnPropertyChanged(nameof(SelectedServingGroupTableSummary));
    }

    [RelayCommand]
    private void SelectServingGroupEditor(ServingGroupEditorRow? row)
    {
        SelectedServingGroupEditor = row;
    }

    [RelayCommand]
    private async Task SaveServingGroupAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn phòng trước khi lưu nhóm phục vụ.";
            return;
        }

        if (SelectedServingGroupEditor?.IsSystemDefined == true)
        {
            StatusText = "Nhóm phục vụ hệ thống chỉ đọc, vui lòng tạo nhóm tùy chỉnh mới.";
            return;
        }

        var selectedTableIds = ServingGroupTableSelectionRows
            .Where(x => x.CanSelect && x.IsSelected)
            .Select(x => x.OrderTableId)
            .Distinct()
            .ToList();
        if (selectedTableIds.Count == 0)
        {
            StatusText = "Vui lòng chọn ít nhất 1 bàn phục vụ cho nhóm.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.SaveServingGroupAsync(new SaveRestaurantServingGroupRequest
            {
                Id = SelectedServingGroupEditor?.Id,
                OrderId = SelectedOrder.Id,
                Name = ServingGroupEditorName,
                ScopeType = "Custom",
                ServingRule = ServingGroupEditorServingRule,
                Note = ServingGroupEditorNote,
                OrderTableIds = selectedTableIds
            });

            var groups = await _service.GetServingGroupsAsync(SelectedOrder.Id);
            UpdateSelectedOrderServingGroups(groups);
            RefreshServingGroupEditor();
            SelectedServingGroupEditor = ServingGroupEditorRows
                .FirstOrDefault(x => string.Equals(x.Name, ServingGroupEditorName, StringComparison.OrdinalIgnoreCase) && !x.IsSystemDefined)
                ?? ServingGroupEditorRows.FirstOrDefault(x => !x.IsSystemDefined);
            StatusText = $"Đã lưu nhóm phục vụ {ServingGroupEditorName}.";
        });
    }

    [RelayCommand]
    private async Task DeleteServingGroupAsync(ServingGroupEditorRow? row)
    {
        row ??= SelectedServingGroupEditor;
        if (row is null)
        {
            StatusText = "Chưa chọn nhóm phục vụ để xóa.";
            return;
        }

        if (row.IsSystemDefined)
        {
            StatusText = "Nhóm phục vụ hệ thống không thể xóa.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.DeleteServingGroupAsync(row.Id);
            if (SelectedOrder is null)
            {
                return;
            }

            var groups = await _service.GetServingGroupsAsync(SelectedOrder.Id);
            UpdateSelectedOrderServingGroups(groups);
            RefreshServingGroupEditor();
            SelectedServingGroupEditor = ServingGroupEditorRows.FirstOrDefault(x => !x.IsSystemDefined);
            StatusText = $"Đã xóa nhóm phục vụ {row.Name}.";
        });
    }

    [RelayCommand]
    private void PreviousOrderItemPickerMenuPage()
    {
        if (CanGoPreviousOrderItemPickerMenuPage)
        {
            OrderItemPickerMenuPageIndex--;
        }
    }

    [RelayCommand]
    private void NextOrderItemPickerMenuPage()
    {
        if (CanGoNextOrderItemPickerMenuPage)
        {
            OrderItemPickerMenuPageIndex++;
        }
    }

    [RelayCommand]
    private void PreviousOrderItemPickerServicePage()
    {
        if (CanGoPreviousOrderItemPickerServicePage)
        {
            OrderItemPickerServicePageIndex--;
        }
    }

    [RelayCommand]
    private void NextOrderItemPickerServicePage()
    {
        if (CanGoNextOrderItemPickerServicePage)
        {
            OrderItemPickerServicePageIndex++;
        }
    }

    [RelayCommand]
    private async Task AutoBalanceRoomServingPlanAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn phòng trước khi cân lại bàn.";
            return;
        }

        if (!HasRoomServingPlan)
        {
            StatusText = "Chưa có bàn phục vụ để cân lại.";
            return;
        }

        var guestCount = Math.Max(1, RoomServingPlanTotalGuests);
        var servingTableCount = Math.Max(1, RoomServingPlanServingTableCount);
        await RunBusyAsync(async () =>
        {
            var order = await _service.ConfigureRoomWalkInOrderAsync(new ConfigureRoomWalkInOrderRequest
            {
                OrderId = SelectedOrder.Id,
                GuestCount = guestCount,
                TableCountToOpen = servingTableCount,
                StartTime = SelectedOrder.StartTime,
                EndTime = SelectedOrder.EndTime,
                CustomerName = SelectedOrder.CustomerName,
                Phone = SelectedOrder.CustomerPhone ?? string.Empty,
                Note = SelectedOrder.Note,
                UpdatedBy = PreparedByName
            });

            UpsertOpenOrder(order);
            SelectedOrder = order;
            OnPropertyChanged(nameof(SelectedOrderFoodLines));
            OnPropertyChanged(nameof(SelectedOrderWineLines));
            OnPropertyChanged(nameof(SelectedOrderDrinkLines));
            OnPropertyChanged(nameof(SelectedOrderServiceLines));
            RefreshRoomServingPlanRows();
            RefreshServingGroupSummaries();
            RefreshOrderItemPickerPlan();
            IsServingPlanDialogOpen = true;
            StatusText = $"Đã cân lại {servingTableCount} bàn cho {guestCount} khách.";
        });
    }

    [RelayCommand]
    private async Task ApplyRoomServingPlanAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn phòng trước khi lưu cấu hình bàn.";
            return;
        }

        if (!HasRoomServingPlan)
        {
            StatusText = "Chưa có cấu hình bàn để lưu.";
            return;
        }

        var servingRows = RoomServingPlanRows.Where(x => x.IsServing).ToList();
        if (servingRows.Count == 0)
        {
            StatusText = "Cần ít nhất 1 bàn ở trạng thái phục vụ.";
            return;
        }

        if (servingRows.Any(x => x.GuestCount <= 0))
        {
            StatusText = "Bàn đang phục vụ phải có số khách lớn hơn 0.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.UpdateRoomWalkInServingPlanAsync(new UpdateRoomWalkInServingPlanRequest
            {
                OrderId = SelectedOrder.Id,
                UpdatedBy = PreparedByName,
                Tables = RoomServingPlanRows.Select(x => new UpdateRoomWalkInServingTableRequest
                {
                    OrderTableId = x.OrderTableId,
                    IsServing = x.IsServing,
                    GuestCount = x.IsServing ? Math.Max(0, x.GuestCount) : 0
                }).ToList()
            });

            UpsertOpenOrder(order);
            SelectedOrder = order;
            OnPropertyChanged(nameof(SelectedOrderFoodLines));
            OnPropertyChanged(nameof(SelectedOrderWineLines));
            OnPropertyChanged(nameof(SelectedOrderDrinkLines));
            OnPropertyChanged(nameof(SelectedOrderServiceLines));
            RefreshRoomServingPlanRows();
            RefreshServingGroupSummaries();
            RefreshOrderItemPickerPlan();
            IsServingPlanDialogOpen = true;
            StatusText = $"Đã lưu cấu hình {RoomServingPlanServingTableCount} bàn phục vụ / {RoomServingPlanTotalGuests} khách.";
        });
    }

    [RelayCommand]
    private void PrepareMenuItemForOrder(PosMenuItemCard? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedOrderItemPickerMenuItem = item;
        OrderItemPickerBaseQuantity = 1m;
        RefreshOrderItemPickerPlan();
        OrderItemPickerStepIndex = 1;
    }

    [RelayCommand]
    private async Task AddMenuItem()
    {
        var item = PagedQuoteMenuItems.FirstOrDefault() ?? FilteredMenuItems.FirstOrDefault();
        await AddMenuItemToSelectedOrderAsync(item);
    }

    [RelayCommand]
    private async Task AddMenuItemToSelectedOrderAsync(PosMenuItemCard? item)
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở trước khi thêm món.";
            return;
        }
        if (item is null || item.Id == Guid.Empty)
        {
            StatusText = "Không có món bán hợp lệ để thêm.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            Guid? selectedOrderTableId = SelectedTable is not null
                && SelectedTable.Id != Guid.Empty
                && (string.Equals(SelectedTable.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(SelectedTable.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase))
                ? SelectedTable.OrderTableId ?? SelectedTable.Id
                : null;
            var order = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
            {
                OrderId = SelectedOrder.Id,
                OrderTableId = selectedOrderTableId,
                MenuItemId = item.Id,
                Qty = 1,
                SupplementQuantity = 0,
                KitchenNote = SelectedOrder.KitchenSentAt.HasValue ? "Món bổ sung" : null
            });
            UpsertOpenOrder(order);
            OnPropertyChanged(nameof(SelectedOrderFoodLines));
            OnPropertyChanged(nameof(SelectedOrderWineLines));
            OnPropertyChanged(nameof(SelectedOrderDrinkLines));
            OnPropertyChanged(nameof(SelectedOrderServiceLines));
            NotifyPreparationQueueState();
            IsOrderItemPickerOpen = true;
            OrderItemPickerStepIndex = 2;
            StatusText = $"Đã thêm món {item.Name} vào đơn {order.OrderNo}.";
        });
    }

    [RelayCommand]
    private async Task ApplyPreparedMenuItemPlanAsync()
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở trước khi thêm món.";
            return;
        }

        if (SelectedOrderItemPickerMenuItem is null)
        {
            StatusText = "Vui lòng chọn món cần áp dụng theo nhóm bàn.";
            return;
        }

        if (OrderItemPickerPlanRows.Count == 0)
        {
            StatusText = "Không có bàn mục tiêu phù hợp với phạm vi áp dụng.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            AlacarteOrderDto? lastOrder = SelectedOrder;
            var affectedOrderTableIds = OrderItemPickerPlanRows
                .Select(x => x.OrderTableId ?? x.TableId)
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            foreach (var row in OrderItemPickerPlanRows)
            {
                lastOrder = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
                {
                    OrderId = SelectedOrder.Id,
                    OrderTableId = row.OrderTableId ?? row.TableId,
                    MenuItemId = SelectedOrderItemPickerMenuItem.Id,
                    Qty = row.SaveQuantity <= 0 ? 1m : row.SaveQuantity,
                    SupplementQuantity = Math.Max(0, row.ExtraQuantity),
                    KitchenNote = BuildPlannedKitchenNote(row),
                    SupplementNote = row.ExtraQuantity > 0 ? $"{row.TableName}: +{row.ExtraQuantity:N0} phần bù thực tế." : null
                });
            }

            if (lastOrder is not null)
            {
                UpsertOpenOrder(lastOrder);
                SelectedOrder = lastOrder;
                if (affectedOrderTableIds.Count > 0)
                {
                    var targetTable = SelectedOrderRoomTables.FirstOrDefault(x =>
                        affectedOrderTableIds.Contains(x.OrderTableId ?? x.Id));
                    if (targetTable is not null)
                    {
                        SelectedTable = targetTable;
                    }
                }

                OnPropertyChanged(nameof(SelectedOrderFoodLines));
                OnPropertyChanged(nameof(SelectedOrderWineLines));
                OnPropertyChanged(nameof(SelectedOrderDrinkLines));
                OnPropertyChanged(nameof(SelectedOrderServiceLines));
                NotifyPreparationQueueState();
                RefreshOrderItemPickerPlan();
                OrderItemPickerStepIndex = 2;
                StatusText = $"Đã áp dụng món {SelectedOrderItemPickerMenuItem.Name} cho {OrderItemPickerPlanRows.Count} bàn.";
            }
        });
    }

    [RelayCommand]
    private async Task AddServiceToSelectedOrderAsync(AlacarteServiceItem? service)
    {
        if (SelectedOrder is null)
        {
            StatusText = "Vui lòng chọn đơn mở trước khi thêm dịch vụ.";
            return;
        }
        if (service is null)
        {
            StatusText = "Chưa có dịch vụ hợp lệ để thêm.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            if (service.Id == Guid.Empty)
            {
                var rawCode = new string(service.Name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(rawCode))
                {
                    rawCode = "ALC-SVC";
                }

                var persisted = await _service.SaveExtraServiceAsync(new SaveRestaurantExtraServiceRequest
                {
                    Code = rawCode[..Math.Min(24, rawCode.Length)],
                    Name = service.Name,
                    ServiceType = "Other",
                    DefaultPrice = service.Price,
                    VatRate = service.VatRate,
                    IsVatIncluded = true,
                    IsActive = true,
                    Note = "Tự tạo từ popup thêm dịch vụ À La Carte."
                });
                service.Id = persisted.Id;
                service.Code = persisted.Code;
            }

            var order = await _service.AddExtraServiceToOrderAsync(new AddAlacarteOrderExtraServiceRequest
            {
                OrderId = SelectedOrder.Id,
                RestaurantExtraServiceId = service.Id,
                Qty = Math.Max(1, service.Quantity)
            });
            UpsertOpenOrder(order);
            OnPropertyChanged(nameof(SelectedOrderFoodLines));
            OnPropertyChanged(nameof(SelectedOrderWineLines));
            OnPropertyChanged(nameof(SelectedOrderDrinkLines));
            OnPropertyChanged(nameof(SelectedOrderServiceLines));
            NotifyPreparationQueueState();
            IsOrderItemPickerOpen = true;
            OrderItemPickerStepIndex = 2;
            StatusText = $"Đã thêm dịch vụ {service.Name} vào đơn {order.OrderNo}.";
        });
    }

    [RelayCommand]
    private async Task IncreaseSelectedOrderLineQuantityAsync(AlacarteOrderLineDto? line)
    {
        if (line is null || SelectedOrder is null)
        {
            return;
        }

        if (IsSummaryTableSelected())
        {
            StatusText = "Ở Bàn Tổng chỉ hỗ trợ xóa theo nhóm. Vui lòng chọn bàn cụ thể để sửa số lượng.";
            return;
        }

        var persistedLine = ResolveTargetOrderLines(line).FirstOrDefault();
        if (persistedLine is null)
        {
            StatusText = "Không tìm thấy dòng món cần cập nhật.";
            return;
        }

        if (string.Equals(persistedLine.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            await EditSelectedOrderLineAsync(line);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
            {
                OrderId = SelectedOrder.Id,
                OrderLineId = persistedLine.Id,
                OrderTableId = persistedLine.OrderTableId,
                MenuItemId = persistedLine.MenuItemId,
                Qty = persistedLine.Qty + 1,
                SupplementQuantity = persistedLine.SupplementQuantity,
                KitchenNote = persistedLine.KitchenNote,
                SupplementNote = persistedLine.SupplementNoteRaw ?? persistedLine.SupplementNote
            });
            ApplyUpdatedOrder(order, persistedLine.OrderTableId);
            StatusText = $"Đã tăng số lượng món {persistedLine.MenuItemName}.";
        });
    }

    [RelayCommand]
    private async Task EditSelectedOrderLineAsync(AlacarteOrderLineDto? line)
    {
        if (line is null || SelectedOrder is null)
        {
            return;
        }

        if (IsSummaryTableSelected())
        {
            var targetLines = ResolveTargetOrderLines(line);
            if (targetLines.Count == 0)
            {
                StatusText = $"Không tìm thấy món {line.MenuItemName} để chỉnh tổng.";
                return;
            }

            var targetQuantity = PromptForSummaryOrderLineQuantity(
                line.MenuItemName,
                targetLines.Sum(x => x.Qty),
                targetLines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase)));
            if (targetQuantity is null)
            {
                StatusText = "Đã hủy thao tác sửa tổng số lượng món.";
                return;
            }

            if (targetQuantity.Quantity <= 0)
            {
                await RemoveSelectedOrderLineAsync(line);
                return;
            }

            await UpdateSummaryOrderLineQuantityAsync(line, targetLines, targetQuantity.Quantity, targetQuantity.AdjustmentReason);
            return;
        }

        var persistedLine = ResolveTargetOrderLines(line).FirstOrDefault();
        if (persistedLine is null)
        {
            StatusText = "Không tìm thấy dòng món cần sửa.";
            return;
        }

        var editResult = await PromptForOrderLineEditAsync(persistedLine);
        if (editResult is null)
        {
            StatusText = "Đã hủy thao tác sửa số lượng món.";
            return;
        }

        if (editResult.Quantity <= 0)
        {
            await RemoveSelectedOrderLineAsync(line);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
            {
                OrderId = SelectedOrder.Id,
                OrderLineId = persistedLine.Id,
                OrderTableId = persistedLine.OrderTableId,
                MenuItemId = persistedLine.MenuItemId,
                Qty = editResult.Quantity,
                SupplementQuantity = Math.Max(0, editResult.SupplementQuantity),
                KitchenNote = string.IsNullOrWhiteSpace(editResult.KitchenNote) ? null : editResult.KitchenNote.Trim(),
                SupplementNote = string.IsNullOrWhiteSpace(editResult.SupplementNoteRaw) ? null : editResult.SupplementNoteRaw.Trim(),
                AdjustmentReason = string.IsNullOrWhiteSpace(editResult.AdjustmentReason) ? null : editResult.AdjustmentReason.Trim()
            });
            ApplyUpdatedOrder(order, persistedLine.OrderTableId);
            StatusText = $"Đã cập nhật món {persistedLine.MenuItemName} thành x{editResult.Quantity:0.##}.";
        });
    }

    [RelayCommand]
    private async Task DecreaseSelectedOrderLineQuantityAsync(AlacarteOrderLineDto? line)
    {
        if (line is null || SelectedOrder is null)
        {
            return;
        }

        if (IsSummaryTableSelected())
        {
            StatusText = "Ở Bàn Tổng chỉ hỗ trợ xóa theo nhóm. Vui lòng chọn bàn cụ thể để sửa số lượng.";
            return;
        }

        var persistedLine = ResolveTargetOrderLines(line).FirstOrDefault();
        if (persistedLine is null)
        {
            StatusText = "Không tìm thấy dòng món cần cập nhật.";
            return;
        }

        if (persistedLine.Qty <= 1)
        {
            await RemoveSelectedOrderLineAsync(line);
            return;
        }

        if (string.Equals(persistedLine.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            await EditSelectedOrderLineAsync(line);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
            {
                OrderId = SelectedOrder.Id,
                OrderLineId = persistedLine.Id,
                OrderTableId = persistedLine.OrderTableId,
                MenuItemId = persistedLine.MenuItemId,
                Qty = Math.Max(1, persistedLine.Qty - 1),
                SupplementQuantity = persistedLine.SupplementQuantity,
                KitchenNote = persistedLine.KitchenNote,
                SupplementNote = persistedLine.SupplementNoteRaw ?? persistedLine.SupplementNote
            });
            ApplyUpdatedOrder(order, persistedLine.OrderTableId);
            StatusText = $"Đã giảm số lượng món {persistedLine.MenuItemName}.";
        });
    }

    [RelayCommand]
    private async Task RemoveSelectedOrderLineAsync(AlacarteOrderLineDto? line)
    {
        if (line is null || SelectedOrder is null)
        {
            return;
        }

        var targetLines = ResolveTargetOrderLines(line);
        if (targetLines.Count == 0)
        {
            StatusText = "Không tìm thấy dòng món cần xóa.";
            return;
        }

        var scopeLabel = IsSummaryTableSelected() ? "toàn bộ các bàn trong Bàn Tổng" : "bàn đang chọn";
        var sentToKitchen = targetLines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
                            || SelectedOrder.KitchenSentAt.HasValue;
        var confirmMessage = IsSummaryTableSelected()
            ? $"Bạn có chắc muốn xóa món {line.MenuItemName} khỏi {scopeLabel}?{Environment.NewLine}Thao tác này sẽ gỡ món khỏi toàn bộ các bàn đã nhận món này."
            : $"Bạn có chắc muốn xóa món {line.MenuItemName} khỏi {scopeLabel}?";

        if (sentToKitchen)
        {
            confirmMessage += $"{Environment.NewLine}{Environment.NewLine}Món đã gửi bếp/bar, hệ thống sẽ tạo phiếu hủy tương ứng.";
        }

        if (System.Windows.MessageBox.Show(
                confirmMessage,
                "Xác nhận xóa món",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
        {
            StatusText = "Đã hủy thao tác xóa món.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            AlacarteOrderDto? latestOrder = SelectedOrder;
            foreach (var targetLine in targetLines)
            {
                latestOrder = await _service.RemoveLineAsync(
                    SelectedOrder.Id,
                    targetLine.Id,
                    sentToKitchen,
                    PreparedByName);
            }

            if (latestOrder is not null)
            {
                ApplyUpdatedOrder(latestOrder, IsSummaryTableSelected() ? Guid.Empty : (SelectedTable?.OrderTableId ?? SelectedTable?.Id));
            }

            StatusText = IsSummaryTableSelected()
                ? $"Đã xóa món {line.MenuItemName} khỏi toàn bộ nhóm bàn."
                : $"Đã xóa món {line.MenuItemName}.";
        });
    }

    private List<AlacarteOrderLineDto> ResolveTargetOrderLines(AlacarteOrderLineDto line)
    {
        var orderLines = SelectedOrder?.Lines?
            .Where(x => !IsOrderServiceLine(x))
            .Where(x => x.Id != Guid.Empty)
            .Where(x => x.MenuItemId == line.MenuItemId
                        && string.Equals(x.MenuItemCode, line.MenuItemCode, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.LineType, line.LineType, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.PricingRule, line.PricingRule, StringComparison.OrdinalIgnoreCase)
                        && x.UnitPrice == line.UnitPrice)
            .ToList() ?? [];

        if (!IsSummaryTableSelected())
        {
            var selectedOrderTableId = SelectedTable?.OrderTableId ?? SelectedTable?.Id;
            orderLines = orderLines
                .Where(x => x.OrderTableId == selectedOrderTableId)
                .OrderBy(x => x.LineNo)
                .ToList();

            var exactLine = orderLines.FirstOrDefault(x => x.Id == line.Id);
            if (exactLine is not null)
            {
                return [exactLine];
            }
        }

        return orderLines
            .OrderBy(x => x.LineNo)
            .ThenBy(x => x.MenuItemName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task UpdateSummaryOrderLineQuantityAsync(AlacarteOrderLineDto line, IReadOnlyList<AlacarteOrderLineDto> targetLines, decimal targetQuantity, string? adjustmentReason)
    {
        if (SelectedOrder is null || targetLines.Count == 0)
        {
            return;
        }

        var sentToKitchen = targetLines.Any(x => string.Equals(x.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
                            || SelectedOrder.KitchenSentAt.HasValue;
        var allocations = DistributeSummaryQuantities(targetLines, targetQuantity);

        await RunBusyAsync(async () =>
        {
            AlacarteOrderDto? latestOrder = SelectedOrder;
            foreach (var targetLine in targetLines)
            {
                if (!allocations.TryGetValue(targetLine.Id, out var newQty))
                {
                    continue;
                }

                if (newQty <= 0)
                {
                    latestOrder = await _service.RemoveLineAsync(
                        SelectedOrder.Id,
                        targetLine.Id,
                        sentToKitchen,
                        PreparedByName);
                    continue;
                }

                latestOrder = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
                {
                    OrderId = SelectedOrder.Id,
                    OrderLineId = targetLine.Id,
                    OrderTableId = targetLine.OrderTableId,
                    MenuItemId = targetLine.MenuItemId,
                    Qty = newQty,
                    SupplementQuantity = targetLine.SupplementQuantity,
                    KitchenNote = targetLine.KitchenNote,
                    SupplementNote = targetLine.SupplementNoteRaw ?? targetLine.SupplementNote,
                    AdjustmentReason = adjustmentReason
                });
            }

            if (latestOrder is not null)
            {
                ApplyUpdatedOrder(latestOrder, Guid.Empty);
            }

            StatusText = $"Đã cập nhật tổng món {line.MenuItemName} thành x{targetQuantity:0.##}.";
        });
    }

    private static Dictionary<Guid, decimal> DistributeSummaryQuantities(IReadOnlyList<AlacarteOrderLineDto> targetLines, decimal targetQuantity)
    {
        var allocations = new Dictionary<Guid, decimal>();
        if (targetLines.Count == 0)
        {
            return allocations;
        }

        var totalCurrent = targetLines.Sum(x => x.Qty);
        if (totalCurrent <= 0)
        {
            var evenQty = Math.Round(targetQuantity / targetLines.Count, 2, MidpointRounding.AwayFromZero);
            decimal assigned = 0;
            for (var index = 0; index < targetLines.Count; index++)
            {
                var line = targetLines[index];
                var qty = index == targetLines.Count - 1
                    ? Math.Max(0, targetQuantity - assigned)
                    : evenQty;
                allocations[line.Id] = qty;
                assigned += qty;
            }

            return allocations;
        }

        var allWhole = targetLines.All(x => x.Qty == Math.Truncate(x.Qty)) && targetQuantity == Math.Truncate(targetQuantity);
        if (allWhole)
        {
            var provisional = targetLines
                .Select(x =>
                {
                    var exact = targetQuantity * x.Qty / totalCurrent;
                    var floor = Math.Floor(exact);
                    return new
                    {
                        Line = x,
                        Floor = floor,
                        Fraction = exact - floor
                    };
                })
                .ToList();

            foreach (var item in provisional)
            {
                allocations[item.Line.Id] = item.Floor;
            }

            var remainder = (int)(targetQuantity - provisional.Sum(x => x.Floor));
            foreach (var item in provisional
                         .OrderByDescending(x => x.Fraction)
                         .ThenBy(x => x.Line.LineNo)
                         .Take(Math.Max(0, remainder)))
            {
                allocations[item.Line.Id] += 1;
            }

            return allocations;
        }

        decimal distributed = 0;
        for (var index = 0; index < targetLines.Count; index++)
        {
            var line = targetLines[index];
            var qty = index == targetLines.Count - 1
                ? Math.Max(0, targetQuantity - distributed)
                : Math.Round(targetQuantity * line.Qty / totalCurrent, 2, MidpointRounding.AwayFromZero);
            allocations[line.Id] = qty;
            distributed += qty;
        }

        return allocations;
    }

    private void ApplyUpdatedOrder(AlacarteOrderDto order, Guid? preferredOrderTableId)
    {
        UpsertOpenOrder(order);

        if (preferredOrderTableId == Guid.Empty)
        {
            SelectedTable = SelectedOrderSummaryTable;
            return;
        }

        if (preferredOrderTableId.HasValue)
        {
            var targetTable = SelectedOrderRoomTables.FirstOrDefault(x =>
                (x.OrderTableId ?? x.Id) == preferredOrderTableId.Value || x.Id == preferredOrderTableId.Value);
            if (targetTable is not null)
            {
                SelectedTable = targetTable;
            }
        }
    }

    private void UpdateSelectedOrderServingGroups(IReadOnlyList<RestaurantServingGroupDto> groups)
    {
        if (SelectedOrder is null)
        {
            return;
        }

        SelectedOrder.ServingGroups = groups;
        var openOrder = OpenOrders.FirstOrDefault(x => x.Id == SelectedOrder.Id);
        if (openOrder is not null)
        {
            openOrder.ServingGroups = groups;
        }

        RefreshServingGroupSummaries();
        RefreshServingGroupEditor();
    }

    private void SyncSettingPrintersFromConfig()
    {
        SettingPrinters.Clear();
        SettingPrinters.Add(new AlacarteSettingPrinter("Máy in bếp", "Cấu hình trực tiếp", KitchenPrinterName, true));
        SettingPrinters.Add(new AlacarteSettingPrinter("Máy in bar", "Cấu hình trực tiếp", BarPrinterName, true));
        SettingPrinters.Add(new AlacarteSettingPrinter("Máy in hóa đơn", "Cấu hình trực tiếp", ReceiptPrinterName, true));
    }

    private void RefreshAvailablePrinters()
    {
        try
        {
            AvailablePrinterNames.Clear();
            var server = new LocalPrintServer();
            foreach (var printer in server.GetPrintQueues().Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            {
                AvailablePrinterNames.Add(printer);
            }

            ApplyDefaultPrinterMappings();
        }
        catch
        {
            // ignore printer enumeration errors
        }
    }

    private void ApplyDefaultPrinterMappings()
    {
        if (AvailablePrinterNames.Count == 0)
        {
            return;
        }

        var thermalPrinter = AvailablePrinterNames.FirstOrDefault(x => x.Contains("XP-80", StringComparison.OrdinalIgnoreCase))
                             ?? AvailablePrinterNames.FirstOrDefault();
        var receiptPrinter = AvailablePrinterNames.FirstOrDefault(x => x.Contains("Canon", StringComparison.OrdinalIgnoreCase))
                             ?? thermalPrinter
                             ?? AvailablePrinterNames.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(KitchenPrinterName) || !AvailablePrinterNames.Any(x => string.Equals(x, KitchenPrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            KitchenPrinterName = thermalPrinter ?? KitchenPrinterName;
        }

        if (string.IsNullOrWhiteSpace(BarPrinterName) || !AvailablePrinterNames.Any(x => string.Equals(x, BarPrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            BarPrinterName = thermalPrinter ?? BarPrinterName;
        }

        if (string.IsNullOrWhiteSpace(ReceiptPrinterName) || !AvailablePrinterNames.Any(x => string.Equals(x, ReceiptPrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            ReceiptPrinterName = receiptPrinter ?? ReceiptPrinterName;
        }
    }

    private async Task<OrderLineEditDialogResult?> PromptForOrderLineEditAsync(AlacarteOrderLineDto line)
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        var quantityBox = new System.Windows.Controls.TextBox
        {
            Text = line.Qty.ToString("0.##"),
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 34,
            Padding = new System.Windows.Thickness(10, 6, 10, 6)
        };
        var supplementBox = new System.Windows.Controls.TextBox
        {
            Text = line.SupplementQuantity.ToString("0.##"),
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 34,
            Padding = new System.Windows.Thickness(10, 6, 10, 6)
        };
        var kitchenNoteBox = new System.Windows.Controls.TextBox
        {
            Text = line.KitchenNote ?? string.Empty,
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 72,
            AcceptsReturn = true,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Padding = new System.Windows.Thickness(10, 8, 10, 8)
        };
        var supplementMetadata = ParseSupplementMetadata(line.SupplementNoteRaw ?? line.SupplementNote);
        var supplementNoteBox = new System.Windows.Controls.TextBox
        {
            Text = supplementMetadata?.DisplayNote ?? line.SupplementNote ?? string.Empty,
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 72,
            AcceptsReturn = true,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Padding = new System.Windows.Thickness(10, 8, 10, 8)
        };
        var adjustmentReasonBox = new System.Windows.Controls.TextBox
        {
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 34,
            Padding = new System.Windows.Thickness(10, 6, 10, 6),
            Visibility = string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed
        };

        var ingredientEditors = new List<OrderLineIngredientEditorRuntime>();
        if (_recipeService is not null)
        {
            try
            {
                var recipe = await _recipeService.GetRecipeAsync(line.MenuItemId);
                if (recipe is not null)
                {
                    foreach (var recipeLine in recipe.Lines.Where(x => x.Id != Guid.Empty && x.ItemId.HasValue))
                    {
                        var previous = supplementMetadata?.IngredientAdjustments.FirstOrDefault(x => x.RecipeLineId == recipeLine.Id);
                        var options = new List<OrderLineIngredientOption>
                        {
                            new()
                            {
                                InventoryItemId = recipeLine.ItemId,
                                DisplayName = $"{recipeLine.ItemName} ({recipeLine.UnitName})",
                                ConversionRate = 1m
                            }
                        };

                        try
                        {
                            var substitutes = await _recipeService.GetSubstitutesAsync(recipeLine.Id);
                            options.AddRange(substitutes.Select(x => new OrderLineIngredientOption
                            {
                                InventoryItemId = x.SubstituteInventoryItemId,
                                DisplayName = $"{x.SubstituteItemName} ({x.SubstituteUnitName})",
                                ConversionRate = x.ConversionRate
                            }));
                        }
                        catch
                        {
                        }

                        var selector = new System.Windows.Controls.ComboBox
                        {
                            ItemsSource = options,
                            DisplayMemberPath = nameof(OrderLineIngredientOption.DisplayName),
                            SelectedItem = options.FirstOrDefault(x => x.InventoryItemId == previous?.SelectedInventoryItemId) ?? options.First(),
                            Height = 34,
                            Margin = new System.Windows.Thickness(0, 6, 0, 0)
                        };
                        var extraBox = new System.Windows.Controls.TextBox
                        {
                            Text = (previous?.ExtraQuantity ?? 0m).ToString("0.##"),
                            Height = 32,
                            Margin = new System.Windows.Thickness(0, 6, 0, 0),
                            Padding = new System.Windows.Thickness(8, 4, 8, 4)
                        };
                        var noteBox = new System.Windows.Controls.TextBox
                        {
                            Text = previous?.Note ?? string.Empty,
                            Height = 56,
                            Margin = new System.Windows.Thickness(0, 6, 0, 0),
                            AcceptsReturn = true,
                            TextWrapping = System.Windows.TextWrapping.Wrap,
                            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                            Padding = new System.Windows.Thickness(8, 6, 8, 6)
                        };

                        ingredientEditors.Add(new OrderLineIngredientEditorRuntime
                        {
                            RecipeLineId = recipeLine.Id,
                            IngredientName = recipeLine.ItemName,
                            UnitName = recipeLine.UnitName,
                            Selector = selector,
                            ExtraQuantityBox = extraBox,
                            NoteBox = noteBox
                        });
                    }
                }
            }
            catch
            {
            }
        }

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new System.Windows.Thickness(18)
        };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"Sửa món: {line.MenuItemName}",
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A"))
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Sửa số lượng chính, phần bù thực tế và ghi chú bếp ngay trên bàn đang chọn. Nếu cần bù nguyên liệu thực tế thì chỉnh ngay trong danh sách nguyên liệu phía dưới.",
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Margin = new System.Windows.Thickness(0, 6, 0, 0)
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Số lượng chính", Margin = new System.Windows.Thickness(0, 12, 0, 0), Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")) });
        panel.Children.Add(quantityBox);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Phần bù tổng thể", Margin = new System.Windows.Thickness(0, 10, 0, 0), Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")) });
        panel.Children.Add(supplementBox);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Ghi chú bếp/bar", Margin = new System.Windows.Thickness(0, 10, 0, 0), Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")) });
        panel.Children.Add(kitchenNoteBox);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Ghi chú phần bù", Margin = new System.Windows.Thickness(0, 10, 0, 0), Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")) });
        panel.Children.Add(supplementNoteBox);
        if (adjustmentReasonBox.Visibility == System.Windows.Visibility.Visible)
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Lý do chỉnh món đã gửi",
                Margin = new System.Windows.Thickness(0, 10, 0, 0),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155"))
            });
            panel.Children.Add(adjustmentReasonBox);
        }

        if (ingredientEditors.Count > 0)
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Nguyên liệu thực tế / thay thế",
                Margin = new System.Windows.Thickness(0, 12, 0, 0),
                FontWeight = System.Windows.FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A"))
            });

            var ingredientHost = new System.Windows.Controls.StackPanel();
            foreach (var editor in ingredientEditors)
            {
                var card = new System.Windows.Controls.Border
                {
                    BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D8E3F5")),
                    BorderThickness = new System.Windows.Thickness(1),
                    CornerRadius = new System.Windows.CornerRadius(10),
                    Padding = new System.Windows.Thickness(10),
                    Margin = new System.Windows.Thickness(0, 8, 0, 0),
                    Child = new System.Windows.Controls.StackPanel
                    {
                        Children =
                        {
                            new System.Windows.Controls.TextBlock
                            {
                                Text = $"{editor.IngredientName} ({editor.UnitName})",
                                FontWeight = System.Windows.FontWeights.SemiBold,
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A"))
                            },
                            new System.Windows.Controls.TextBlock
                            {
                                Text = "Chọn nguyên liệu xuất kho thực tế và nhập phần bù nếu bếp cần thêm riêng cho dòng này.",
                                Margin = new System.Windows.Thickness(0, 2, 0, 0),
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
                                TextWrapping = System.Windows.TextWrapping.Wrap
                            },
                            editor.Selector,
                            new System.Windows.Controls.TextBlock
                            {
                                Text = "Phần bù theo nguyên liệu",
                                Margin = new System.Windows.Thickness(0, 8, 0, 0),
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155"))
                            },
                            editor.ExtraQuantityBox,
                            new System.Windows.Controls.TextBlock
                            {
                                Text = "Ghi chú riêng",
                                Margin = new System.Windows.Thickness(0, 8, 0, 0),
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155"))
                            },
                            editor.NoteBox
                        }
                    }
                };
                ingredientHost.Children.Add(card);
            }

            panel.Children.Add(new System.Windows.Controls.ScrollViewer
            {
                Height = 220,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Content = ingredientHost
            });
        }

        var cancelButton = new System.Windows.Controls.Button { Content = "Đóng", Width = 96, Height = 34, IsCancel = true };
        var saveButton = new System.Windows.Controls.Button { Content = "Lưu", Width = 96, Height = 34, Margin = new System.Windows.Thickness(8, 0, 0, 0), IsDefault = true };
        var actions = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 14, 0, 0)
        };
        actions.Children.Add(cancelButton);
        actions.Children.Add(saveButton);
        panel.Children.Add(actions);

        var dialog = new System.Windows.Window
        {
            Title = "Sửa món trên bàn",
            Content = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Content = panel
            },
            Width = 620,
            Height = ingredientEditors.Count > 0 ? 760 : 560,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = System.Windows.Media.Brushes.White
        };

        OrderLineEditDialogResult? result = null;
        saveButton.Click += (_, _) =>
        {
            if (!decimal.TryParse(quantityBox.Text?.Trim(), out var parsed))
            {
                System.Windows.MessageBox.Show(dialog, "Số lượng không hợp lệ.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                quantityBox.Focus();
                quantityBox.SelectAll();
                return;
            }

            if (!decimal.TryParse(supplementBox.Text?.Trim(), out var supplement))
            {
                System.Windows.MessageBox.Show(dialog, "Phần bù thực tế không hợp lệ.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                supplementBox.Focus();
                supplementBox.SelectAll();
                return;
            }

            var hasSentLineChanges =
                !string.Equals(quantityBox.Text?.Trim(), line.Qty.ToString("0.##"), StringComparison.Ordinal) ||
                !string.Equals(supplementBox.Text?.Trim(), line.SupplementQuantity.ToString("0.##"), StringComparison.Ordinal) ||
                !string.Equals((kitchenNoteBox.Text ?? string.Empty).Trim(), (line.KitchenNote ?? string.Empty).Trim(), StringComparison.Ordinal) ||
                !string.Equals((supplementNoteBox.Text ?? string.Empty).Trim(), (supplementMetadata?.DisplayNote ?? line.SupplementNote ?? string.Empty).Trim(), StringComparison.Ordinal);

            if (adjustmentReasonBox.Visibility == System.Windows.Visibility.Visible
                && hasSentLineChanges
                && string.IsNullOrWhiteSpace(adjustmentReasonBox.Text))
            {
                System.Windows.MessageBox.Show(dialog, "Vui lòng nhập lý do khi chỉnh món đã gửi bếp/bar.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                adjustmentReasonBox.Focus();
                adjustmentReasonBox.SelectAll();
                return;
            }

            var ingredientAdjustments = new List<RestaurantLineIngredientAdjustmentDto>();
            foreach (var editor in ingredientEditors)
            {
                if (!decimal.TryParse(editor.ExtraQuantityBox.Text?.Trim(), out var extraQuantity))
                {
                    System.Windows.MessageBox.Show(dialog, $"Phần bù nguyên liệu {editor.IngredientName} không hợp lệ.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    editor.ExtraQuantityBox.Focus();
                    editor.ExtraQuantityBox.SelectAll();
                    return;
                }

                var selectedOption = editor.Selector.SelectedItem as OrderLineIngredientOption;
                if (selectedOption is null)
                {
                    continue;
                }

                if (extraQuantity > 0 || selectedOption.ConversionRate != 1m || !string.IsNullOrWhiteSpace(editor.NoteBox.Text))
                {
                    ingredientAdjustments.Add(new RestaurantLineIngredientAdjustmentDto
                    {
                        RecipeLineId = editor.RecipeLineId,
                        SelectedInventoryItemId = selectedOption.InventoryItemId,
                        SelectedItemName = selectedOption.DisplayName,
                        ConversionRate = selectedOption.ConversionRate,
                        ExtraQuantity = Math.Max(0, extraQuantity),
                        Note = editor.NoteBox.Text?.Trim()
                    });
                }
            }

            var displaySupplementNote = supplementNoteBox.Text?.Trim();
            var supplementNoteRaw = SerializeSupplementMetadata(displaySupplementNote, ingredientAdjustments);
            result = new OrderLineEditDialogResult
            {
                Quantity = parsed,
                SupplementQuantity = Math.Max(0, supplement),
                KitchenNote = kitchenNoteBox.Text?.Trim(),
                SupplementNote = displaySupplementNote,
                SupplementNoteRaw = supplementNoteRaw,
                AdjustmentReason = adjustmentReasonBox.Visibility == System.Windows.Visibility.Visible ? adjustmentReasonBox.Text?.Trim() : null
            };
            dialog.DialogResult = true;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? result : null;
    }

    private SummaryOrderLineEditDialogResult? PromptForSummaryOrderLineQuantity(string itemName, decimal currentQty, bool requireReason)
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        var quantityBox = new System.Windows.Controls.TextBox
        {
            Text = currentQty.ToString("0.##"),
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 34,
            Padding = new System.Windows.Thickness(10, 6, 10, 6)
        };
        var adjustmentReasonBox = new System.Windows.Controls.TextBox
        {
            Margin = new System.Windows.Thickness(0, 8, 0, 0),
            Height = 34,
            Padding = new System.Windows.Thickness(10, 6, 10, 6),
            Visibility = requireReason ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed
        };

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new System.Windows.Thickness(18)
        };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"Sửa tổng số lượng: {itemName}",
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A"))
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Nhập tổng số lượng đúng cho Bàn Tổng. Hệ thống sẽ tự chia lại xuống các bàn con theo tỷ lệ hiện tại.",
            Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B")),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Margin = new System.Windows.Thickness(0, 6, 0, 0)
        });
        panel.Children.Add(quantityBox);
        if (requireReason)
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Lý do chỉnh tổng món đã gửi",
                Margin = new System.Windows.Thickness(0, 10, 0, 0),
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155"))
            });
            panel.Children.Add(adjustmentReasonBox);
        }

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Đóng",
            Width = 96,
            Height = 34,
            IsCancel = true
        };
        var saveButton = new System.Windows.Controls.Button
        {
            Content = "Lưu tổng",
            Width = 96,
            Height = 34,
            Margin = new System.Windows.Thickness(8, 0, 0, 0),
            IsDefault = true
        };

        var actions = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 14, 0, 0)
        };
        actions.Children.Add(cancelButton);
        actions.Children.Add(saveButton);
        panel.Children.Add(actions);

        var dialog = new System.Windows.Window
        {
            Title = "Sửa tổng số lượng món",
            Content = panel,
            Width = 460,
            Height = 230,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = System.Windows.Media.Brushes.White
        };

        SummaryOrderLineEditDialogResult? result = null;
        saveButton.Click += (_, _) =>
        {
            if (!decimal.TryParse(quantityBox.Text?.Trim(), out var parsed))
            {
                System.Windows.MessageBox.Show(dialog, "Số lượng không hợp lệ.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                quantityBox.Focus();
                quantityBox.SelectAll();
                return;
            }

            if (requireReason && !string.Equals(parsed.ToString("0.##"), currentQty.ToString("0.##"), StringComparison.Ordinal) && string.IsNullOrWhiteSpace(adjustmentReasonBox.Text))
            {
                System.Windows.MessageBox.Show(dialog, "Vui lòng nhập lý do khi chỉnh món đã gửi bếp/bar.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                adjustmentReasonBox.Focus();
                adjustmentReasonBox.SelectAll();
                return;
            }

            result = new SummaryOrderLineEditDialogResult
            {
                Quantity = parsed,
                AdjustmentReason = adjustmentReasonBox.Visibility == System.Windows.Visibility.Visible ? adjustmentReasonBox.Text?.Trim() : null
            };
            dialog.DialogResult = true;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? result : null;
    }

    private const string SupplementMetadataMarker = "[ALC-SUPP-META]";

    private static RestaurantLineSupplementMetadata? ParseSupplementMetadata(string? supplementNoteRaw)
    {
        if (string.IsNullOrWhiteSpace(supplementNoteRaw))
        {
            return null;
        }

        var markerIndex = supplementNoteRaw.IndexOf(SupplementMetadataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var payload = supplementNoteRaw[(markerIndex + SupplementMetadataMarker.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var metadata = JsonSerializer.Deserialize<RestaurantLineSupplementMetadata>(json);
            if (metadata is not null)
            {
                metadata.DisplayNote = ExtractSupplementDisplayNote(supplementNoteRaw);
            }

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSupplementDisplayNote(string? supplementNoteRaw)
    {
        if (string.IsNullOrWhiteSpace(supplementNoteRaw))
        {
            return null;
        }

        var markerIndex = supplementNoteRaw.IndexOf(SupplementMetadataMarker, StringComparison.Ordinal);
        var display = markerIndex >= 0 ? supplementNoteRaw[..markerIndex] : supplementNoteRaw;
        display = display.Trim();
        return string.IsNullOrWhiteSpace(display) ? null : display;
    }

    private static string? SerializeSupplementMetadata(string? displayNote, IReadOnlyList<RestaurantLineIngredientAdjustmentDto> ingredientAdjustments)
    {
        var normalizedDisplay = string.IsNullOrWhiteSpace(displayNote) ? null : displayNote.Trim();
        var normalizedAdjustments = ingredientAdjustments
            .Where(x => x.RecipeLineId != Guid.Empty
                        && (x.SelectedInventoryItemId.HasValue || x.ExtraQuantity > 0 || !string.IsNullOrWhiteSpace(x.Note)))
            .ToList();
        if (normalizedAdjustments.Count == 0)
        {
            return normalizedDisplay;
        }

        var metadata = new RestaurantLineSupplementMetadata
        {
            DisplayNote = normalizedDisplay,
            IngredientAdjustments = normalizedAdjustments
        };
        var json = JsonSerializer.Serialize(metadata);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return string.IsNullOrWhiteSpace(normalizedDisplay)
            ? $"{SupplementMetadataMarker}{encoded}"
            : $"{normalizedDisplay}{Environment.NewLine}{SupplementMetadataMarker}{encoded}";
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
            System.Windows.MessageBox.Show(StatusText, "Lỗi in thử", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void PreviewKitchenSampleTicket()
    {
        var preview = new PrintPreviewDialog("Phiếu bếp mẫu", BuildKitchenSamplePreviewText())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        preview.ShowDialog();
    }

    [RelayCommand]
    private void PreviewReceiptSampleTicket()
    {
        var preview = new PrintPreviewDialog("Phiếu thanh toán mẫu", BuildReceiptSamplePreviewText())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        preview.ShowDialog();
    }

    [RelayCommand]
    private async Task PrintKitchenSampleTicketAsync()
    {
        await PrintStructuredSampleTicketAsync(
            "PHIẾU BẾP MẪU",
            KitchenPrinterName,
            KitchenPaperSize,
            BuildKitchenSampleThermalText(KitchenPaperSize),
            () => BuildKitchenSampleDocument(KitchenPaperSize));
    }

    [RelayCommand]
    private async Task PrintReceiptSampleTicketAsync()
    {
        await PrintStructuredSampleTicketAsync(
            "PHIẾU THANH TOÁN MẪU",
            ReceiptPrinterName,
            ReceiptPaperSize,
            BuildReceiptSampleThermalText(ReceiptPaperSize),
            () => BuildReceiptSampleDocument(ReceiptPaperSize));
    }

    private async Task PrintStructuredSampleTicketAsync(
        string title,
        string printerTarget,
        string paperSize,
        string thermalText,
        Func<System.Windows.Documents.FlowDocument> buildDocument)
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
                await SendRawThermalPrintAsync(host, port, thermalText);
            }
            else
            {
                PrintFlowDocumentToWindowsPrinter(buildDocument(), printerTarget, title);
            }

            StatusText = $"Đã gửi phiếu test thật '{title}' tới {printerTarget}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Không in được phiếu test '{title}': {ex.Message}";
            System.Windows.MessageBox.Show(StatusText, "Lỗi in phiếu test", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private string BuildKitchenSamplePreviewText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("PHIẾU BẾP MẪU");
        builder.AppendLine("Mã phiếu: KB-TEST-001");
        builder.AppendLine("Phòng: TẦNG 2 - VIP 2");
        builder.AppendLine("Bàn: Bàn Tổng / Ban 01 / Ban 02");
        builder.AppendLine("Khách: Nguyễn Văn A - 20 khách");
        builder.AppendLine($"Nhân viên gửi: {PreparedByName}");
        builder.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine("1. Tôm hấp x22");
        builder.AppendLine("   Phần bù thực tế: +2 con");
        builder.AppendLine("   Ghi chú bếp: Chia 2 bàn, lên nóng");
        builder.AppendLine("2. Lẩu thái x2");
        builder.AppendLine("   Ghi chú bếp: Ra sau món hấp");
        builder.AppendLine("3. Cơm chiên hải sản x2");
        builder.AppendLine("   Ghi chú bếp: Ít cay");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine("Mục đích: test máy in bếp thật");
        return builder.ToString();
    }

    private string BuildReceiptSamplePreviewText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("PHIẾU THANH TOÁN MẪU");
        builder.AppendLine("Nhà hàng: A La Carte");
        builder.AppendLine("Mã đơn: ALA-TEST-001");
        builder.AppendLine("Khách hàng: Nguyễn Văn A");
        builder.AppendLine("Phòng: TẦNG 2 - VIP 2");
        builder.AppendLine("Tổng khách: 20");
        builder.AppendLine($"Thu ngân: {PreparedByName}");
        builder.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine("1. Tôm hấp          x22   35,000   770,000");
        builder.AppendLine("2. Lẩu thái         x2   320,000   640,000");
        builder.AppendLine("3. Nước ngọt        x20   18,000   360,000");
        builder.AppendLine(new string('-', 42));
        builder.AppendLine("Tạm tính:                    1,770,000");
        builder.AppendLine("VAT 8%:                        141,600");
        builder.AppendLine("Phí phục vụ 5%:                 88,500");
        builder.AppendLine("TỔNG CỘNG:                   2,000,100");
        builder.AppendLine("Mục đích: test máy in hóa đơn thật");
        return builder.ToString();
    }

    private string BuildKitchenSampleThermalText(string paperSize)
    {
        var columns = GetThermalPaperColumns(paperSize);
        return string.Join(Environment.NewLine,
            CenterThermalText("PHIEU BEP MAU", columns),
            CenterThermalText("ASMILE BO", columns),
            new string('-', columns),
            "Ma phieu: KB-TEST-001",
            "Phong: TANG 2 - VIP 2",
            "Ban: BAN TONG / BAN 01 / BAN 02",
            "Khach: NGUYEN VAN A - 20 KHACH",
            $"Gui boi: {PreparedByName}",
            $"Luc: {DateTime.Now:dd/MM/yyyy HH:mm}",
            new string('-', columns),
            "1. TOM HAP x22",
            "   Phan bu: +2 con",
            "   Ghi chu: Chia 2 ban, len nong",
            "2. LAU THAI x2",
            "   Ghi chu: Ra sau mon hap",
            "3. COM CHIEN HAI SAN x2",
            "   Ghi chu: It cay",
            new string('-', columns),
            CenterThermalText("TEST MAY IN BEP THAT", columns),
            string.Empty,
            string.Empty);
    }

    private string BuildReceiptSampleThermalText(string paperSize)
    {
        var columns = GetThermalPaperColumns(paperSize);
        return string.Join(Environment.NewLine,
            CenterThermalText("PHIEU THANH TOAN MAU", columns),
            CenterThermalText("NHA HANG A LA CARTE", columns),
            new string('-', columns),
            "Ma don: ALA-TEST-001",
            "Khach: NGUYEN VAN A",
            "Phong: TANG 2 - VIP 2",
            "Tong khach: 20",
            $"Thu ngan: {PreparedByName}",
            $"Luc: {DateTime.Now:dd/MM/yyyy HH:mm}",
            new string('-', columns),
            "Tom hap          x22   770,000",
            "Lau thai          x2   640,000",
            "Nuoc ngot        x20   360,000",
            new string('-', columns),
            "Tam tinh:       1,770,000",
            "VAT 8%:           141,600",
            "Phi PV 5%:         88,500",
            "Tong cong:      2,000,100",
            new string('-', columns),
            CenterThermalText("TEST MAY IN HOA DON THAT", columns),
            string.Empty,
            string.Empty);
    }

    private static System.Windows.Documents.FlowDocument BuildKitchenSampleDocument(string paperSize)
    {
        var doc = CreateThermalFlowDocument(paperSize, 11);
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("PHIẾU BẾP MẪU"))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
            $"Mã phiếu: KB-TEST-001\nPhòng: TẦNG 2 - VIP 2\nBàn: Bàn Tổng / Ban 01 / Ban 02\nKhách: Nguyễn Văn A - 20 khách\nNhân viên gửi: {System.Environment.UserName}\nThời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"))
        {
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("1. Tôm hấp x22\n   Phần bù thực tế: +2 con\n   Ghi chú bếp: Chia 2 bàn, lên nóng\n2. Lẩu thái x2\n   Ghi chú bếp: Ra sau món hấp\n3. Cơm chiên hải sản x2\n   Ghi chú bếp: Ít cay"))
        {
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Mục đích: test máy in bếp thật"))
        {
            FontWeight = System.Windows.FontWeights.SemiBold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        return doc;
    }

    private static System.Windows.Documents.FlowDocument BuildReceiptSampleDocument(string paperSize)
    {
        var doc = CreateThermalFlowDocument(paperSize, 11);
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("PHIẾU THANH TOÁN MẪU"))
        {
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
            $"Nhà hàng: A La Carte\nMã đơn: ALA-TEST-001\nKhách hàng: Nguyễn Văn A\nPhòng: TẦNG 2 - VIP 2\nTổng khách: 20\nThời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"))
        {
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
            "1. Tôm hấp        x22   35,000   770,000\n2. Lẩu thái        x2   320,000   640,000\n3. Nước ngọt       x20   18,000   360,000"))
        {
            Margin = new System.Windows.Thickness(0, 0, 0, 10)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
            "Tạm tính: 1,770,000\nVAT 8%: 141,600\nPhí phục vụ 5%: 88,500\nTỔNG CỘNG: 2,000,100"))
        {
            FontWeight = System.Windows.FontWeights.SemiBold
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Mục đích: test máy in hóa đơn thật"))
        {
            FontWeight = System.Windows.FontWeights.SemiBold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        return doc;
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

    private static System.Windows.Documents.FlowDocument CreateThermalFlowDocument(string paperSize, double fontSize = 11)
    {
        var normalizedPaper = NormalizePaperSize(paperSize);
        var width = normalizedPaper.StartsWith("58", StringComparison.OrdinalIgnoreCase) ? 210d : 285d;
        return new System.Windows.Documents.FlowDocument
        {
            PagePadding = new System.Windows.Thickness(16, 12, 16, 12),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = fontSize,
            ColumnWidth = double.PositiveInfinity,
            PageWidth = width
        };
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
        var targetName = printerName.Trim();
        var queues = server.GetPrintQueues().ToList();
        var queue = queues.FirstOrDefault(x => string.Equals(x.Name, targetName, StringComparison.OrdinalIgnoreCase))
                    ?? queues.FirstOrDefault(x => x.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase));
        if (queue is null)
        {
            var available = string.Join(", ", queues.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Không tìm thấy máy in '{printerName}'. Máy in hiện có: {available}.");
        }

        var dialog = new System.Windows.Controls.PrintDialog
        {
            PrintQueue = queue
        };
        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, jobName);
    }

    private string? BuildPlannedKitchenNote(OrderItemPickerPlanRow row)
    {
        var notes = new List<string>();
        if (SelectedOrder?.KitchenSentAt.HasValue == true)
        {
            notes.Add("Món bổ sung");
        }

        if (!string.IsNullOrWhiteSpace(row.KitchenNote))
        {
            notes.Add(row.KitchenNote!);
        }

        if (notes.Count == 0)
        {
            return null;
        }

        return string.Join(" | ", notes);
    }

    private void SaveRoomAsync()
    {
        _ = SaveRoomImplAsync();
    }
    private async Task SaveRoomImplAsync()
    {
        await RunBusyAsync(async () =>
        {
            var room = await _service.SaveRoomAsync(new SaveDiningRoomRequest
            {
                Id = SelectedRoom?.Id, Code = RoomCode, Name = RoomName,
                RoomType = "Standard", Capacity = RoomCapacity,
                Status = "Available", IsActive = true
            });
            SelectedRoom = room;
            await RefreshAsync();
            StatusText = $"Đã lưu phòng ăn {room.Name}.";
        });
    }

    [RelayCommand]
    private void NewRoom()
    {
        SelectedRoom = null;
        RoomCode = string.Empty;
        RoomName = string.Empty;
        RoomCapacity = 40;
    }

    [RelayCommand]
    private async Task SaveTableAsync()
    {
        var roomId = SelectedRoom?.Id ?? Rooms.FirstOrDefault()?.Id;
        if (roomId is null) { StatusText = "Cần tạo/chọn phòng ăn trước."; return; }
        await RunBusyAsync(async () =>
        {
            var table = await _service.SaveTableAsync(new SaveDiningTableRequest
            {
                Id = SelectedTable?.Id, DiningRoomId = roomId.Value,
                Code = TableCode, Name = TableName, SeatCount = TableSeats,
                Status = "Empty", IsActive = true
            });
            SelectedTable = table;
            await RefreshAsync();
            StatusText = $"Đã lưu bàn {table.Name}.";
        });
    }

    [RelayCommand]
    private void NewTable()
    {
        SelectedTable = null;
        TableCode = string.Empty;
        TableName = string.Empty;
        TableSeats = 4;
    }

    [RelayCommand]
    private async Task SaveReservationAsync()
    {
        await RunBusyAsync(async () =>
        {
            var reservation = await _service.SaveReservationAsync(new SaveTableReservationRequest
            {
                CustomerName = ReservationCustomerName, Phone = ReservationPhone,
                ReservationDateTime = ReservationDateTime, GuestCount = ReservationGuestCount,
                DiningRoomId = SelectedRoom?.Id, DiningTableId = SelectedTable?.Id,
                Status = "Confirmed"
            });
            await RefreshAsync();
            StatusText = $"Đã tạo đặt bàn {reservation.ReservationNo}.";
        });
    }

    [RelayCommand]
    private async Task OpenOrderAsync(DiningTableDto? table)
    {
        table ??= SelectedTable;
        if (table is null)
        {
            StatusText = "Chọn bàn trước khi mở đơn.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var order = await _service.OpenOrderAsync(new CreateAlacarteOrderRequest
            {
                DiningTableId = table.Id,
                CreatedBy = "pos-ui"
            });
            SelectedOrder = order;
            Dashboard = await _service.GetDashboardAsync();
            Reset(Tables, await _service.GetTablesAsync());
            ApplyTableFilter();
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            NotifyCollectionState();
            SelectedOrder = OpenOrders.FirstOrDefault(x => x.Id == order.Id) ?? order;
            StatusText = $"Đã mở đơn {SelectedOrder.OrderNo}.";
        });
    }

    private async Task LoadExtraServicesAsync()
    {
        var services = await _service.GetExtraServicesAsync();
        if (services.Count == 0)
        {
            services = await _service.SeedDefaultExtraServicesAsync();
        }

        Reset(AlacarteServices, services.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.DefaultPrice, "lần", x.Code, x.VatRate)
        {
            ServiceType = NormalizeServiceTypeLabel(x.ServiceType),
            IsVatIncluded = x.IsVatIncluded,
            IsActive = x.IsActive
        }));
        Reset(SettingExtServices, services.Select(x => new AlacarteServiceItem(x.Id, x.Name, x.DefaultPrice, "lần", x.Code, x.VatRate)
        {
            ServiceType = NormalizeServiceTypeLabel(x.ServiceType),
            IsVatIncluded = x.IsVatIncluded,
            IsActive = x.IsActive
        }));
        ApplyOrderItemPickerFilters();
        NotifyCollectionState();
    }

    private void InitDefaultServices()
    {
        if (_defaultServices.Count > 0) return;
        var svcs = new List<AlacarteServiceItem>
        {
            new("Màn LED", 500000, "buổi"), new("MC", 300000, "buổi"), new("Ca sĩ", 800000, "buổi"),
            new("Âm thanh", 400000, "buổi"), new("Trang trí", 500000, "buổi"), new("Máy chiếu", 300000, "buổi"),
            new("Phí phòng VIP", 200000, "buổi"), new("Phí phục vụ", 0, "%"), new("Hoa tươi", 350000, "buổi"),
            new("Ánh sáng sân khấu", 600000, "buổi"), new("Photobooth", 400000, "buổi"), new("Cổng hoa", 500000, "buổi")
        };
        _defaultServices.AddRange(svcs);
        foreach (var s in _defaultServices) AlacarteServices.Add(s);
    }

    private void InitMenuItems()
    {
        if (AlacarteMenuItems.Count > 0) return;
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Tôm hùm nướng bơ tỏi", "Hải sản", 550000, "🦞"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Bò Úc nướng đá", "Món nướng", 450000, "🥩"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Súp hải sản", "Khai vị", 120000, "🍲"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Salad dầu giấm", "Khai vị", 80000, "🥗"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Gỏi cuốn tôm thịt", "Khai vị", 95000, "🥢"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Cá hồi sốt chanh dây", "Hải sản", 380000, "🐟"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Lẩu hải sản", "Món lẩu", 650000, "🍲"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Cơm chiên hải sản", "Cơm", 150000, "🍚"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Mì xào hải sản", "Mì", 130000, "🍝"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Nước cam ép", "Đồ uống", 45000, "🍊"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Cà phê đen", "Cà phê", 35000, "☕"));
        AlacarteMenuItems.Add(new AlacarteMenuItemCard("Trà đá", "Trà", 15000, "🍵"));
    }

    private static void Reset<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }

    private void RefreshSettingCollections()
    {
        var areaRows = Rooms
            .GroupBy(x => string.IsNullOrWhiteSpace(x.RoomType) ? "Mặc định" : x.RoomType.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new AlacarteSettingDiningArea(
                g.Key,
                g.Count(),
                Tables.Count(t => g.Select(r => r.Id).Contains(t.DiningRoomId))))
            .ToList();

        foreach (var manualArea in _manualDiningAreas.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (areaRows.All(x => !string.Equals(x.Name, manualArea, StringComparison.OrdinalIgnoreCase)))
            {
                areaRows.Add(new AlacarteSettingDiningArea(manualArea.Trim(), 0, 0));
            }
        }

        Reset(SettingDiningAreas, areaRows.OrderBy(x => x.Name));
        Reset(SettingAreas, Rooms.Select(x => new AlacarteSettingArea(x.Id, x.Code, x.Name, x.RoomType, x.Capacity, x.IsActive)));
        Reset(SettingTables, Tables.Select(x => new AlacarteSettingTable(x.Id, x.DiningRoomId, x.Code, x.Name, x.SeatCount, x.RoomName, x.Status, Rooms.FirstOrDefault(r => r.Id == x.DiningRoomId)?.RoomType ?? string.Empty)));
        OnPropertyChanged(nameof(FilteredSettingRooms));
        OnPropertyChanged(nameof(FilteredSettingTables));
    }

    private void UpsertRoom(DiningRoomDto room)
    {
        var existing = Rooms.FirstOrDefault(x => x.Id == room.Id);
        if (existing is null)
        {
            Rooms.Add(room);
            return;
        }

        var index = Rooms.IndexOf(existing);
        if (index >= 0)
        {
            Rooms[index] = room;
        }
    }

    private void UpsertTable(DiningTableDto table)
    {
        var existing = Tables.FirstOrDefault(x => x.Id == table.Id);
        if (existing is null)
        {
            Tables.Add(table);
            return;
        }

        var index = Tables.IndexOf(existing);
        if (index >= 0)
        {
            Tables[index] = table;
        }
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            StatusText = "Hệ thống đang xử lý, vui lòng thử lại sau.";
            return;
        }
        try { IsBusy = true; await action(); }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Alacarte restaurant operation failed");
            StatusText = $"Thao tác không thành công: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void NotifyCollectionState()
    {
        RefreshSavedQuoteWorkflowStates();
        OnPropertyChanged(nameof(HasRooms));
        OnPropertyChanged(nameof(HasTables));
        OnPropertyChanged(nameof(HasReservations));
        OnPropertyChanged(nameof(HasOpenOrders));
        OnPropertyChanged(nameof(HasContracts));
        OnPropertyChanged(nameof(HasServices));
    }

    partial void OnQuoteListTabIndexChanged(int value)
    {
        QuoteSearchText = string.Empty;
        QuotePageIndex = 0;
        RefreshFilteredQuotes();
    }

    partial void OnQuoteSearchTextChanged(string value)
    {
        QuotePageIndex = 0;
        RefreshFilteredQuotes();
    }

    partial void OnQuotePageIndexChanged(int value)
    {
        RefreshPagedQuotes();
    }

    [RelayCommand]
    private void QuotePrevPage()
    {
        if (QuotePageIndex > 0) QuotePageIndex--;
        RefreshPagedQuotes();
    }

    [RelayCommand]
    private void QuoteNextPage()
    {
        if (QuotePageIndex < QuoteTotalPages - 1) QuotePageIndex++;
        RefreshPagedQuotes();
    }

    private void RefreshFilteredQuotes()
    {
        var selected = SelectedSavedQuote;
        var filtered = QuoteListTabIndex switch
        {
            0 => SavedQuotes.Where(q => q.ApprovedOrderId.HasValue && q.WorkflowState != "Đã phục vụ" && q.WorkflowState != "Đã thanh toán"),
            1 => SavedQuotes.Where(q => !q.ApprovedOrderId.HasValue),
            2 => SavedQuotes.Where(q => q.WorkflowState == "Đã phục vụ"),
            3 => SavedQuotes.Where(q => q.WorkflowState == "Đã thanh toán"),
            _ => SavedQuotes.AsEnumerable()
        };

        if (!string.IsNullOrWhiteSpace(QuoteSearchText))
        {
            var s = QuoteSearchText.Trim();
            filtered = filtered.Where(q =>
                (q.QuoteNo?.Contains(s, StringComparison.OrdinalIgnoreCase) == true) ||
                (q.CustomerName?.Contains(s, StringComparison.OrdinalIgnoreCase) == true) ||
                (q.Phone?.Contains(s, StringComparison.OrdinalIgnoreCase) == true) ||
                (q.RoomName?.Contains(s, StringComparison.OrdinalIgnoreCase) == true));
        }

        Reset(FilteredSavedQuotes, filtered);
        QuotePager.SetSource(FilteredSavedQuotes);
        Reset(PagedQuotes, QuotePager.Items);
        QuoteTotalPages = QuotePager.TotalPages;
        QuotePageIndex = Math.Max(0, QuotePager.CurrentPage - 1);
        OnPropertyChanged(nameof(HasFilteredSavedQuotes));
        if (selected is not null && FilteredSavedQuotes.Contains(selected))
            SelectedSavedQuote = selected;
    }

    private void RefreshPagedQuotes()
    {
        Reset(PagedQuotes, QuotePager.Items);
        OnPropertyChanged(nameof(CanGoPrevPage));
        OnPropertyChanged(nameof(CanGoNextPage));
    }

    private static int ResolveQuoteListTabIndex(SavedQuoteDto quote)
    {
        if (quote.WorkflowState == "Đã thanh toán")
            return 3;
        if (quote.WorkflowState == "Đã phục vụ")
            return 2;
        if (quote.ApprovedOrderId.HasValue)
            return 0;
        return 1;
    }

    public bool CanGoPrevPage => QuotePageIndex > 0;
    public bool CanGoNextPage => QuotePageIndex < QuoteTotalPages - 1;
    public bool HasFilteredSavedQuotes => FilteredSavedQuotes.Count > 0;

    private void RefreshSavedQuoteWorkflowStates()
    {
        foreach (var quote in SavedQuotes)
        {
            quote.WorkflowState = ResolveQuoteWorkflowState(quote);
        }
        RefreshFilteredQuotes();
    }

    private string ResolveQuoteWorkflowState(SavedQuoteDto quote)
    {
        if (quote.ApprovedOrderId.HasValue)
        {
            var openOrder = OpenOrders.FirstOrDefault(x => x.Id == quote.ApprovedOrderId.Value);
            if (openOrder is not null)
            {
                if (string.Equals(openOrder.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                    && HasQuoteServiceStarted(quote))
                {
                    return "Đã phục vụ";
                }

                return "Đã duyệt";
            }

            return "Đã thanh toán";
        }

        if (DateTime.Today > quote.CreatedAt.Date.AddDays(Math.Max(1, quote.ValidityDays)))
        {
            return "Quá hạn";
        }

        if (quote.Status.Contains("hủy", StringComparison.OrdinalIgnoreCase))
        {
            return "Đã hủy";
        }

        if (quote.Status.Contains("gửi", StringComparison.OrdinalIgnoreCase))
        {
            return "Đã gửi";
        }

        return "Chờ duyệt";
    }

    private static bool HasQuoteServiceStarted(SavedQuoteDto quote)
    {
        if (!TimeSpan.TryParse(quote.StartTime, out var startTime))
        {
            return quote.EventDate.Date <= DateTime.Today;
        }

        return quote.EventDate.Date.Add(startTime) <= DateTime.Now;
    }

    private void InitSettingsReferenceData()
    {
        // 1. Khu vực / Phòng ăn
        if (SettingDiningAreas.Count == 0)
        {
            var roomAreas = Rooms
                .Select(r => string.IsNullOrWhiteSpace(r.RoomType) ? "Mặc định" : r.RoomType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var area in roomAreas)
            {
                SettingDiningAreas.Add(new AlacarteSettingDiningArea(area, Rooms.Count(r => string.Equals(r.RoomType, area, StringComparison.OrdinalIgnoreCase)), Tables.Count(t => string.Equals(Rooms.FirstOrDefault(r => r.Id == t.DiningRoomId)?.RoomType, area, StringComparison.OrdinalIgnoreCase))));
            }
            if (SettingDiningAreas.Count == 0)
            {
                SettingDiningAreas.Add(new AlacarteSettingDiningArea("Tầng 1", 0, 0));
                SettingDiningAreas.Add(new AlacarteSettingDiningArea("Tầng 2", 0, 0));
            }
        }

        if (SettingAreas.Count == 0)
        {
            foreach (var r in Rooms)
                SettingAreas.Add(new AlacarteSettingArea(r.Id, r.Code, r.Name, r.RoomType, r.Capacity, r.IsActive));
            if (SettingAreas.Count == 0)
            {
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "VIP1", "VIP 1", "Vip", 40, true));
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "VIP2", "VIP 2", "Vip", 30, true));
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "TANG1", "Tầng 1", "Standard", 60, true));
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "TANG2", "Tầng 2", "Standard", 50, true));
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "SANVUON", "Sân vườn", "Outdoor", 30, true));
                SettingAreas.Add(new AlacarteSettingArea(Guid.Empty, "NGOAITROI", "Ngoài trời", "Outdoor", 20, true));
            }
        }

        // 2. Bàn ăn
        if (SettingTables.Count == 0)
        {
            foreach (var t in Tables)
                SettingTables.Add(new AlacarteSettingTable(t.Id, t.DiningRoomId, t.Code, t.Name, t.SeatCount, t.RoomName, t.Status, Rooms.FirstOrDefault(r => r.Id == t.DiningRoomId)?.RoomType ?? string.Empty));
            if (SettingTables.Count == 0)
            {
                var sampleTables = new[] { ("A01","VIP 1",4,"Empty"),("A02","VIP 1",4,"Empty"),("A03","VIP 1",6,"Reserved"),
                    ("A04","VIP 1",8,"Serving"),("A05","VIP 1",6,"Empty"),("B01","Tầng 1",6,"Serving"),
                    ("B02","Tầng 1",4,"Empty"),("B03","Tầng 1",4,"Empty"),("B04","Tầng 1",4,"Reserved"),
                    ("B05","Tầng 1",4,"Locked"),("B06","Tầng 1",8,"Empty") };
                foreach (var (code, room, seats, status) in sampleTables)
                    SettingTables.Add(new AlacarteSettingTable(Guid.Empty, Guid.Empty, code, code, seats, room, status, room));
            }
        }

        // 3. Máy in
        if (SettingPrinters.Count == 0)
        {
            SyncSettingPrintersFromConfig();
        }

        // 7. Thanh toán
        if (SettingPaymentMethods.Count == 0)
        {
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("Tiền mặt", "Cash", true));
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("Chuyển khoản", "Transfer", true));
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("POS/Thẻ", "Card", true));
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("Ghi phòng", "RoomCharge", true));
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("Công nợ", "Debt", true));
            SettingPaymentMethods.Add(new AlacarteSettingPaymentMethod("Ví điện tử", "EWallet", false));
        }

        // 9. Phân quyền
        if (SettingPermissions.Count == 0)
        {
            SettingPermissions.Add(new AlacarteSettingPermission("Xem module", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Quản lý phòng/bàn", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Mở đơn", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Hủy đơn", false));
            SettingPermissions.Add(new AlacarteSettingPermission("Giảm giá", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Thanh toán", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Nhận cọc", true));
            SettingPermissions.Add(new AlacarteSettingPermission("Quản lý hợp đồng", false));
            SettingPermissions.Add(new AlacarteSettingPermission("Cấu hình hệ thống", false));
        }

        // 11. Tích hợp
        if (SettingIntegrations.Count == 0)
        {
            SettingIntegrations.Add(new AlacarteSettingIntegration("Món ăn & Định lượng", true));
            SettingIntegrations.Add(new AlacarteSettingIntegration("Khách hàng", true));
            SettingIntegrations.Add(new AlacarteSettingIntegration("Công nợ phải thu", true));
            SettingIntegrations.Add(new AlacarteSettingIntegration("Quỹ / Ngân hàng", false));
            SettingIntegrations.Add(new AlacarteSettingIntegration("Kho / Định lượng", false));
        }
    }

    // Restaurant info for printed documents
    [ObservableProperty] private string restaurantName = "ASMILE BO — NHÀ HÀNG À LA CARTE";
    [ObservableProperty] private string restaurantAddress = "123 Nguyễn Huệ, Q.1, TP.HCM";
    [ObservableProperty] private string restaurantPhone = "1900 9999";
    [ObservableProperty] private string restaurantEmail = "info@asmilebo.vn";
    [ObservableProperty] private string restaurantTaxCode = "0312345678";
    [ObservableProperty] private string restaurantLogoPath = string.Empty;
    public string RestaurantQuoteTitle => string.IsNullOrWhiteSpace(RestaurantName) ? "Nhà hàng" : RestaurantName;
    public string RestaurantContactLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(RestaurantAddress)) parts.Add(RestaurantAddress);
            if (!string.IsNullOrWhiteSpace(RestaurantPhone)) parts.Add("Hotline: " + RestaurantPhone);
            if (!string.IsNullOrWhiteSpace(RestaurantEmail)) parts.Add("Email: " + RestaurantEmail);
            if (!string.IsNullOrWhiteSpace(RestaurantTaxCode)) parts.Add("MST: " + RestaurantTaxCode);
            return string.Join(" · ", parts);
        }
    }
    partial void OnRestaurantNameChanged(string value) => NotifyRestaurantInfoChanged();
    partial void OnRestaurantAddressChanged(string value) => NotifyRestaurantInfoChanged();
    partial void OnRestaurantPhoneChanged(string value) => NotifyRestaurantInfoChanged();
    partial void OnRestaurantEmailChanged(string value) => NotifyRestaurantInfoChanged();
    partial void OnRestaurantTaxCodeChanged(string value) => NotifyRestaurantInfoChanged();
    private void NotifyRestaurantInfoChanged()
    {
        OnPropertyChanged(nameof(RestaurantQuoteTitle));
        OnPropertyChanged(nameof(RestaurantContactLine));
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _allowedMenuCategoryIds.Clear();
        _allowedMenuCategoryIds.UnionWith(GetAllowedMenuCategoryIds());
        _menuItemTypeOverrides.Clear();
        foreach (var menuItem in SettingMenuItems.Where(x => x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.ItemType)))
        {
            _menuItemTypeOverrides[menuItem.Id] = menuItem.ItemType.Trim();
        }

        SaveRestaurantSettings();
        SyncSettingPrintersFromConfig();
        ApplyMenuFilters();
        SettingSaved = true;
        StatusText = "Đã lưu cấu hình.";
    }

    [RelayCommand]
    private void NewSettingMenuGroup()
    {
        EditingSettingMenuGroupId = null;
        SettingMenuGroupCode = string.Empty;
        SettingMenuGroupName = string.Empty;
        SettingMenuGroupSortOrder = Math.Max(100, SettingMenuGroups.Count * 10 + 10);
        StatusText = "Sẵn sàng tạo nhóm món mới.";
    }

    [RelayCommand]
    private void EditSettingMenuGroup(AlacarteSettingMenuGroup? group)
    {
        if (group is null) return;
        EditingSettingMenuGroupId = group.Id;
        SettingMenuGroupCode = group.Code;
        SettingMenuGroupName = group.Name;
        SettingMenuGroupSortOrder = group.SortOrder;
        StatusText = $"Đang sửa nhóm {group.Name}.";
    }

    [RelayCommand]
    private async Task SaveSettingMenuGroupAsync()
    {
        if (_menuCatalog is null)
        {
            StatusText = "Chưa cấu hình dịch vụ danh mục món.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SettingMenuGroupCode) || string.IsNullOrWhiteSpace(SettingMenuGroupName))
        {
            StatusText = "Nhập đủ mã nhóm và tên nhóm.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _menuCatalog.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
            {
                Id = EditingSettingMenuGroupId,
                Code = SettingMenuGroupCode.Trim(),
                Name = SettingMenuGroupName.Trim(),
                SortOrder = SettingMenuGroupSortOrder,
                IsActive = true
            });

            await LoadMenuItemsAsync();
            NewSettingMenuGroup();
            StatusText = "Đã lưu nhóm món.";
        });
    }

    [RelayCommand]
    private async Task DeleteSettingMenuGroupAsync(AlacarteSettingMenuGroup? group)
    {
        if (_menuCatalog is null || group is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _menuCatalog.DeleteOrDeactivateCategoryAsync(group.Id);
            await LoadMenuItemsAsync();
            if (EditingSettingMenuGroupId == group.Id)
            {
                NewSettingMenuGroup();
            }

            StatusText = $"Đã ngừng nhóm {group.Name}.";
        });
    }

    [RelayCommand]
    private void NewSettingMenuItem()
    {
        EditingSettingMenuItemId = null;
        SettingMenuItemCategoryId = SettingMenuGroups.FirstOrDefault()?.Id;
        SettingMenuItemCode = string.Empty;
        SettingMenuItemName = string.Empty;
        SettingMenuItemPrice = 0;
        SettingMenuItemVatRate = 8m;
        SettingMenuItemStation = "Bếp";
        SettingMenuItemType = "Đồ ăn";
        SettingMenuItemHasRecipe = true;
        SettingMenuItemIsActive = true;
        StatusText = "Sẵn sàng tạo món bán mới.";
    }

    [RelayCommand]
    private void EditSettingMenuItem(AlacarteSettingMenuItem? item)
    {
        if (item is null) return;
        EditingSettingMenuItemId = item.Id;
        SettingMenuItemCategoryId = item.CategoryId;
        SettingMenuItemCode = item.Code;
        SettingMenuItemName = item.Name;
        SettingMenuItemPrice = item.Price;
        SettingMenuItemVatRate = item.VatRate;
        SettingMenuItemStation = item.Station;
        SettingMenuItemType = item.ItemType;
        SettingMenuItemHasRecipe = item.HasRecipe;
        SettingMenuItemIsActive = item.IsActive;
        StatusText = $"Đang sửa món {item.Name}.";
    }

    [RelayCommand]
    private async Task SaveSettingMenuItemAsync()
    {
        if (_menuCatalog is null)
        {
            StatusText = "Chưa cấu hình dịch vụ danh mục món.";
            return;
        }

        if (!SettingMenuItemCategoryId.HasValue)
        {
            StatusText = "Chọn nhóm món trước khi lưu.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SettingMenuItemCode) || string.IsNullOrWhiteSpace(SettingMenuItemName))
        {
            StatusText = "Nhập đủ mã món và tên món.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var item = await _menuCatalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
            {
                Id = EditingSettingMenuItemId,
                MenuCategoryId = SettingMenuItemCategoryId.Value,
                Code = SettingMenuItemCode.Trim(),
                Name = SettingMenuItemName.Trim(),
                UnitName = "phần",
                UnitPrice = Math.Max(0, SettingMenuItemPrice),
                VatRate = Math.Max(0, SettingMenuItemVatRate) / 100m,
                IsVatIncluded = true,
                HasRecipeManagement = SettingMenuItemHasRecipe,
                IsHotelService = string.Equals(SettingMenuItemType, "Dịch vụ", StringComparison.OrdinalIgnoreCase),
                IsActive = SettingMenuItemIsActive,
                PreparationArea = string.IsNullOrWhiteSpace(SettingMenuItemStation) ? null : SettingMenuItemStation.Trim()
            });

            _menuItemTypeOverrides[item.Id] = SettingMenuItemType;
            await LoadMenuItemsAsync();
            NewSettingMenuItem();
            StatusText = "Đã lưu món bán.";
        });
    }

    [RelayCommand]
    private async Task DeleteSettingMenuItemAsync(AlacarteSettingMenuItem? item)
    {
        if (_menuCatalog is null || item is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _menuCatalog.DeleteOrDeactivateMenuItemAsync(item.Id);
            _menuItemTypeOverrides.Remove(item.Id);
            await LoadMenuItemsAsync();
            if (EditingSettingMenuItemId == item.Id)
            {
                NewSettingMenuItem();
            }

            StatusText = $"Đã ngừng bán món {item.Name}.";
        });
    }

    [RelayCommand]
    private void NewSettingExtService()
    {
        EditingSettingExtServiceId = null;
        SettingExtServiceCode = string.Empty;
        SettingExtServiceName = string.Empty;
        SettingExtServicePrice = 0;
        SettingExtServiceVatRate = 8m;
        SettingExtServiceVatIncluded = true;
        SettingExtServiceIsActive = true;
        SettingExtServiceUnit = "lần";
        SettingExtServiceType = "Khác";
        StatusText = "Sẵn sàng tạo dịch vụ bổ sung mới.";
    }

    [RelayCommand]
    private void EditSettingExtService(AlacarteServiceItem? item)
    {
        if (item is null) return;
        EditingSettingExtServiceId = item.Id;
        SettingExtServiceCode = item.Code;
        SettingExtServiceName = item.Name;
        SettingExtServicePrice = item.Price;
        SettingExtServiceVatRate = item.VatRate;
        SettingExtServiceVatIncluded = item.IsVatIncluded;
        SettingExtServiceIsActive = item.IsActive;
        SettingExtServiceUnit = string.IsNullOrWhiteSpace(item.Unit) ? "lần" : item.Unit;
        SettingExtServiceType = string.IsNullOrWhiteSpace(item.ServiceType) ? "Khác" : item.ServiceType;
        StatusText = $"Đang sửa dịch vụ {item.Name}.";
    }

    [RelayCommand]
    private async Task SaveSettingExtServiceAsync()
    {
        if (string.IsNullOrWhiteSpace(SettingExtServiceCode) || string.IsNullOrWhiteSpace(SettingExtServiceName))
        {
            StatusText = "Nhập đủ mã dịch vụ và tên dịch vụ.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.SaveExtraServiceAsync(new SaveRestaurantExtraServiceRequest
            {
                Id = EditingSettingExtServiceId,
                Code = SettingExtServiceCode.Trim(),
                Name = SettingExtServiceName.Trim(),
                DefaultPrice = Math.Max(0, SettingExtServicePrice),
                VatRate = Math.Max(0, SettingExtServiceVatRate),
                IsVatIncluded = SettingExtServiceVatIncluded,
                IsActive = SettingExtServiceIsActive,
                ServiceType = NormalizeServiceTypeKey(SettingExtServiceType)
            });

            await LoadExtraServicesAsync();
            NewSettingExtService();
            StatusText = "Đã lưu dịch vụ bổ sung.";
        });
    }

    [RelayCommand]
    private async Task DeleteSettingExtServiceAsync(AlacarteServiceItem? item)
    {
        if (item is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.SaveExtraServiceAsync(new SaveRestaurantExtraServiceRequest
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                DefaultPrice = item.Price,
                VatRate = item.VatRate,
                IsVatIncluded = item.IsVatIncluded,
                IsActive = false,
                ServiceType = NormalizeServiceTypeKey(item.ServiceType)
            });

            await LoadExtraServicesAsync();
            if (EditingSettingExtServiceId == item.Id)
            {
                NewSettingExtService();
            }

            StatusText = $"Đã ngừng dịch vụ {item.Name}.";
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

    public ObservableCollection<SavedQuoteDto> SavedQuotes { get; } = new();
    public ObservableCollection<SavedQuoteDto> FilteredSavedQuotes { get; } = new();
    public ObservableCollection<SavedQuoteDto> PagedQuotes { get; } = new();
    public TablePagerViewModel<SavedQuoteDto> QuotePager { get; } = new() { ItemName = "báo giá" };
    [ObservableProperty] private int quoteListTabIndex;
    [ObservableProperty] private string quoteSearchText = string.Empty;
    [ObservableProperty] private int quotePageIndex;
    [ObservableProperty] private int quotePageSize = 10;
    [ObservableProperty] private int quoteTotalPages;
    [ObservableProperty] private SavedQuoteDto? selectedSavedQuote;
    [ObservableProperty] private string quoteNote = string.Empty;

    [RelayCommand]
    private void SwitchQuoteListTab(string index)
    {
        if (int.TryParse(index, out var i))
            QuoteListTabIndex = i;
    }

    [RelayCommand]
    private void SelectSavedQuote(SavedQuoteDto? quote)
    {
        SelectedSavedQuote = quote;
        ApplySavedQuoteDetails(quote);
    }

    [RelayCommand]
    private void EditSavedQuote(SavedQuoteDto? quote)
    {
        if (quote is null) return;
        CopyQuoteToEditor(quote, isNew: false);
        StatusText = $"Đang sửa báo giá {quote.QuoteNo}.";
    }

    [RelayCommand]
    private void CopySavedQuote(SavedQuoteDto? quote)
    {
        if (quote is null) return;
        CopyQuoteToEditor(quote, isNew: true);
        StatusText = $"Đã sao chép báo giá {quote.QuoteNo} sang báo giá mới.";
    }

    private void CopyQuoteToEditor(SavedQuoteDto quote, bool isNew)
    {
        SelectedSavedQuote = quote;
        if (isNew)
        {
            _editingQuoteNo = null;
            QuoteNo = string.Empty; // Sẽ được service generate khi lưu
        }
        else
        {
            _editingQuoteNo = quote.QuoteNo;
            QuoteNo = quote.QuoteNo;
        }
        ReservationCustomerName = quote.CustomerName;
        ReservationPhone = quote.Phone;
        ReservationGuestCount = Math.Max(1, quote.TableCount);
        QuoteGuestCount = Math.Max(1, quote.GuestCount);
        QuoteGuestsPerTable = Math.Max(1, quote.TableStandard);
        QuoteDate = quote.EventDate;
        QuoteStartTime = quote.StartTime;
        QuoteEndTime = quote.EndTime;
        SelectedRoom = Rooms.FirstOrDefault(r => string.Equals(r.Name, quote.RoomName, StringComparison.OrdinalIgnoreCase));

        QuoteLines.Clear();
        foreach (var line in quote.Lines)
        {
            QuoteLines.Add(new QuoteLineItem
            {
                MenuItemId = line.MenuItemId,
                Name = line.Name,
                UnitPrice = line.UnitPrice,
                Quantity = Math.Max(1, line.Quantity),
                Type = line.Type,
                PricingRule = string.IsNullOrWhiteSpace(line.PricingRule) ? (IsQuoteServiceLine(line.Type) ? "PerRoom" : "PerTable") : line.PricingRule,
                CalculatedQuantity = line.CalculatedQuantity
            });
        }

        RefreshLineNumbers();
        NotifyQuoteTotals();
        SelectedTabIndex = 2;
        QuoteSubTabIndex = 0;
    }

    [RelayCommand]
    private async Task SaveSelectedQuoteDetailsAsync()
    {
        if (SelectedSavedQuote is null)
        {
            StatusText = "Vui lòng chọn báo giá cần lưu thông tin chi tiết.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            WriteCurrentDetailsToQuote(SelectedSavedQuote);
            var saved = await _service.SaveQuoteAsync(ToSaveQuoteRequest(SelectedSavedQuote, keepCurrentDetails: true));
            ReplaceSavedQuote(saved);
            NotifyQuoteTotals();
            StatusText = $"Đã lưu thông tin chi tiết báo giá {SelectedSavedQuote.QuoteNo}.";
        });
    }
    [ObservableProperty] private decimal quoteDeposit;
    [ObservableProperty] private string quotePaymentDeadline = string.Empty;
    [ObservableProperty] private string quotePaymentMethod = "Chuyển khoản";
    [ObservableProperty] private decimal quoteDiscount;
    [ObservableProperty] private string quotePromotion = string.Empty;
    [ObservableProperty] private int quoteValidityDays = 7;
    [ObservableProperty] private int quoteCancelDeadlineDays = 3;
    [ObservableProperty] private decimal quoteCancelFeePercent = 30;
    [ObservableProperty] private string quoteBankAccount = string.Empty;
    [ObservableProperty] private string quoteStatus = "Nháp";
    public ObservableCollection<string> QuotePaymentMethodOptions { get; } = new(["Tiền mặt", "Chuyển khoản", "Tiền mặt + CK"]);
    public ObservableCollection<string> QuoteStatusOptions { get; } = new(["Nháp", "Đã gửi", "Đã duyệt", "Đã hủy"]);

    [RelayCommand]
    private async Task SaveQuoteAsync()
    {
        await RunBusyAsync(async () =>
        {
            var isEditing = !string.IsNullOrWhiteSpace(_editingQuoteNo);
            var quoteNo = isEditing ? _editingQuoteNo! : "BG-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var existingQuote = isEditing ? SavedQuotes.FirstOrDefault(q => q.QuoteNo == _editingQuoteNo) : null;
            var quote = new SavedQuoteDto
            {
                Id = existingQuote?.Id ?? Guid.Empty,
                QuoteNo = quoteNo,
                CustomerName = ReservationCustomerName,
                Phone = ReservationPhone,
                TableCount = Math.Max(1, ReservationGuestCount),
                GuestCount = Math.Max(1, QuoteGuestCount),
                TableStandard = Math.Max(1, QuoteGuestsPerTable),
                RoomName = SelectedRoom?.Name ?? "Chưa chọn",
                EventDate = QuoteDate,
                StartTime = QuoteStartTime,
                EndTime = QuoteEndTime,
                TotalAmount = QuoteGrandTotal,
                PreparedBy = PreparedByName,
                CreatedAt = existingQuote?.CreatedAt ?? DateTime.Now,
                ApprovedOrderId = existingQuote?.ApprovedOrderId,
                DepositCashTransactionId = existingQuote?.DepositCashTransactionId,
                DepositCashTransactionNo = existingQuote?.DepositCashTransactionNo,
                Lines = QuoteLines.Select(q => new SavedQuoteLineDto
                {
                    MenuItemId = q.MenuItemId,
                    Name = q.Name,
                    UnitPrice = q.UnitPrice,
                    Quantity = q.Quantity,
                    PricingRule = q.PricingRule,
                    CalculatedQuantity = q.CalculatedQuantity,
                    Total = q.Total,
                    Type = q.Type
                }).ToList()
            };
            if (existingQuote is not null)
            {
                CopySavedDetails(existingQuote, quote);
            }

            var saved = await _service.SaveQuoteAsync(ToSaveQuoteRequest(quote, keepCurrentDetails: existingQuote is not null));
            ReplaceSavedQuote(saved);
            QuoteNo = saved.QuoteNo;
            _editingQuoteNo = saved.QuoteNo;
            StatusText = isEditing ? $"Đã cập nhật báo giá {quote.QuoteNo}." : $"Đã lưu báo giá {quote.QuoteNo}.";
        });
    }
    private void ApplySavedQuoteDetails(SavedQuoteDto? quote)
    {
        if (quote is null) return;

        QuoteDeposit = quote.DepositAmount;
        QuotePaymentDeadline = quote.PaymentDeadline;
        QuotePaymentMethod = string.IsNullOrWhiteSpace(quote.PaymentMethod) ? "Chuyển khoản" : quote.PaymentMethod;
        QuoteDiscount = quote.DiscountAmount;
        QuotePromotion = quote.Promotion;
        QuoteValidityDays = quote.ValidityDays > 0 ? quote.ValidityDays : 7;
        QuoteCancelDeadlineDays = quote.CancelDeadlineDays > 0 ? quote.CancelDeadlineDays : 3;
        QuoteCancelFeePercent = quote.CancelFeePercent > 0 ? quote.CancelFeePercent : 30;
        QuoteBankAccount = quote.BankAccount;
        QuoteStatus = string.IsNullOrWhiteSpace(quote.Status) ? "Nháp" : quote.Status;
        QuoteNote = quote.Note;
        NotifyQuoteTotals();
    }

    private void WriteCurrentDetailsToQuote(SavedQuoteDto quote)
    {
        quote.GuestCount = Math.Max(1, QuoteGuestCount);
        quote.TableStandard = Math.Max(1, QuoteGuestsPerTable);
        quote.DepositAmount = QuoteDeposit;
        quote.PaymentDeadline = QuotePaymentDeadline;
        quote.PaymentMethod = QuotePaymentMethod;
        quote.DiscountAmount = QuoteDiscount;
        quote.Promotion = QuotePromotion;
        quote.ValidityDays = QuoteValidityDays;
        quote.CancelDeadlineDays = QuoteCancelDeadlineDays;
        quote.CancelFeePercent = QuoteCancelFeePercent;
        quote.BankAccount = QuoteBankAccount;
        quote.Status = QuoteStatus;
        quote.Note = QuoteNote;
    }

    private static void CopySavedDetails(SavedQuoteDto source, SavedQuoteDto target)
    {
        target.DepositAmount = source.DepositAmount;
        target.GuestCount = source.GuestCount;
        target.TableStandard = source.TableStandard;
        target.PaymentDeadline = source.PaymentDeadline;
        target.PaymentMethod = source.PaymentMethod;
        target.DiscountAmount = source.DiscountAmount;
        target.Promotion = source.Promotion;
        target.ValidityDays = source.ValidityDays;
        target.CancelDeadlineDays = source.CancelDeadlineDays;
        target.CancelFeePercent = source.CancelFeePercent;
        target.BankAccount = source.BankAccount;
        target.Status = source.Status;
        target.Note = source.Note;
    }

    private SaveRestaurantQuoteRequest ToSaveQuoteRequest(SavedQuoteDto quote, bool keepCurrentDetails)
    {
        return new SaveRestaurantQuoteRequest
        {
            Id = quote.Id == Guid.Empty ? null : quote.Id,
            QuoteNo = quote.QuoteNo,
            CustomerName = quote.CustomerName,
            Phone = quote.Phone,
            TableCount = Math.Max(1, quote.TableCount),
            GuestCount = Math.Max(1, quote.GuestCount),
            TableStandard = Math.Max(1, quote.TableStandard),
            DiningRoomId = SelectedRoom?.Name == quote.RoomName ? SelectedRoom.Id : Rooms.FirstOrDefault(r => string.Equals(r.Name, quote.RoomName, StringComparison.OrdinalIgnoreCase))?.Id,
            DiningTableId = SelectedTable?.DiningRoomId == (SelectedRoom?.Id ?? Guid.Empty) ? SelectedTable?.Id : null,
            RoomName = quote.RoomName,
            EventDate = quote.EventDate,
            StartTime = quote.StartTime,
            EndTime = quote.EndTime,
            TotalAmount = quote.TotalAmount,
            DepositAmount = keepCurrentDetails ? quote.DepositAmount : 0m,
            PaymentDeadline = keepCurrentDetails ? quote.PaymentDeadline : string.Empty,
            PaymentMethod = keepCurrentDetails ? quote.PaymentMethod : "Chuyển khoản",
            DiscountAmount = keepCurrentDetails ? quote.DiscountAmount : 0m,
            Promotion = keepCurrentDetails ? quote.Promotion : string.Empty,
            ValidityDays = keepCurrentDetails ? quote.ValidityDays : 7,
            CancelDeadlineDays = keepCurrentDetails ? quote.CancelDeadlineDays : 3,
            CancelFeePercent = keepCurrentDetails ? quote.CancelFeePercent : 30,
            BankAccount = keepCurrentDetails ? quote.BankAccount : string.Empty,
            Status = keepCurrentDetails ? quote.Status : "Nháp",
            Note = keepCurrentDetails ? quote.Note : string.Empty,
            PreparedBy = quote.PreparedBy,
            Lines = quote.Lines.Select(l => new SaveRestaurantQuoteLineRequest
            {
                MenuItemId = l.MenuItemId,
                Name = l.Name,
                Type = l.Type,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                PricingRule = l.PricingRule,
                Total = l.Total
            }).ToList()
        };
    }

    private void ReplaceSavedQuote(RestaurantQuoteDto quote)
    {
        var row = FromDto(quote);
        row.WorkflowState = ResolveQuoteWorkflowState(row);
        var existing = SavedQuotes.FirstOrDefault(x => x.Id == row.Id || x.QuoteNo == row.QuoteNo);
        if (existing is null) SavedQuotes.Insert(0, row);
        else SavedQuotes[SavedQuotes.IndexOf(existing)] = row;
        SelectedSavedQuote = row;
        ApplySavedQuoteDetails(row);
        RefreshFilteredQuotes();
    }

    private static SavedQuoteDto FromDto(RestaurantQuoteDto quote)
    {
        return new SavedQuoteDto
        {
            Id = quote.Id,
            QuoteNo = quote.QuoteNo,
            CustomerName = quote.CustomerName,
            Phone = quote.Phone,
            TableCount = quote.TableCount,
            GuestCount = quote.GuestCount,
            TableStandard = quote.TableStandard,
            RoomName = quote.RoomName,
            EventDate = quote.EventDate,
            StartTime = quote.StartTime,
            EndTime = quote.EndTime,
            TotalAmount = quote.TotalAmount,
            PreparedBy = quote.PreparedBy,
            CreatedAt = quote.CreatedAt,
            DepositAmount = quote.DepositAmount,
            PaymentDeadline = quote.PaymentDeadline,
            PaymentMethod = quote.PaymentMethod,
            DiscountAmount = quote.DiscountAmount,
            Promotion = quote.Promotion,
            ValidityDays = quote.ValidityDays,
            CancelDeadlineDays = quote.CancelDeadlineDays,
            CancelFeePercent = quote.CancelFeePercent,
            BankAccount = quote.BankAccount,
            Status = quote.Status,
            Note = quote.Note,
            ApprovedOrderId = quote.ApprovedOrderId,
            DepositCashTransactionId = quote.DepositCashTransactionId,
            DepositCashTransactionNo = quote.DepositCashTransactionNo,
            Tables = quote.Tables.ToList(),
            Lines = quote.Lines.Select(l => new SavedQuoteLineDto
            {
                MenuItemId = l.MenuItemId,
                Name = l.Name,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                PricingRule = l.PricingRule,
                CalculatedQuantity = l.CalculatedQuantity,
                Total = l.Total,
                Type = l.Type
            }).ToList()
        };
    }

    [RelayCommand]
    private void PrintOrderReceipt()
    {
        if (SelectedOrder is null) { StatusText = "Vui lòng chọn đơn."; return; }
        var doc = BuildReceiptDocument();
        ShowPrintPreview(doc);
    }

    private static System.Windows.Documents.Block Sep()
    {
        return new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(new string('-', 34)))
        {
            TextAlignment = System.Windows.TextAlignment.Center
        };
    }

    private System.Windows.Documents.FlowDocument BuildReceiptDocument()
    {
        const double paperWidth = 300;
        var order = SelectedOrder!;
        var doc = new System.Windows.Documents.FlowDocument
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 11,
            PagePadding = new System.Windows.Thickness(4),
            PageWidth = paperWidth,
            ColumnWidth = paperWidth - 8
        };
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        var orange = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12));

        // Header
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("PHIẾU TẠM TÍNH"))
        {
            FontSize = 18, FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(order.OrderNo))
        {
            FontSize = 12, TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 2, 0, 0)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Bàn: {SelectedOrderTableDisplayName}  |  {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 6)
        });
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
        {
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"KH: {order.CustomerName}"))
            {
                FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 0, 0, 4)
            });
        }
        doc.Blocks.Add(Sep());

        // Items
        var allLines = (order.Lines ?? Enumerable.Empty<AlacarteOrderLineDto>())
            .GroupBy(l => l.MenuItemId)
            .Select(g => (Name: g.First().MenuItemName, UnitPrice: g.First().UnitPrice, Qty: g.Sum(x => x.Qty), Amount: g.Sum(x => x.Amount), IsService: IsOrderServiceLine(g.First())))
            .ToList();
        var foodLines = allLines.Where(l => !l.IsService).ToList();
        var svcLines = allLines.Where(l => l.IsService).ToList();

        foreach (var line in foodLines)
        {
            var p = new System.Windows.Documents.Paragraph { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) };
            p.Inlines.Add(new System.Windows.Documents.Run(line.Name) { FontWeight = System.Windows.FontWeights.SemiBold });
            p.Inlines.Add(new System.Windows.Documents.Run($"\n  {line.Qty:N1} x {line.UnitPrice:N0}đ = {line.Amount:N0}đ"));
            doc.Blocks.Add(p);
        }
        if (svcLines.Count > 0)
        {
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("DỊCH VỤ"))
            {
                FontSize = 11, FontWeight = System.Windows.FontWeights.Bold,
                TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 6, 0, 2)
            });
            foreach (var line in svcLines)
            {
                var p = new System.Windows.Documents.Paragraph { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0) };
                p.Inlines.Add(new System.Windows.Documents.Run(line.Name) { FontWeight = System.Windows.FontWeights.SemiBold });
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  {line.Qty:N1} x {line.UnitPrice:N0}đ = {line.Amount:N0}đ"));
                doc.Blocks.Add(p);
            }
        }

        doc.Blocks.Add(Sep());

        // Totals
        var totals = new System.Windows.Documents.Paragraph { FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center };
        totals.Inlines.Add(new System.Windows.Documents.Run($"Tạm tính: {order.SubtotalAmount:N0}đ\n"));
        if (order.DiscountAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"Giảm giá: -{order.DiscountAmount:N0}đ\n"));
        if (order.VatAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"VAT: {order.VatAmount:N0}đ\n"));
        if (order.ServiceChargeAmount > 0) totals.Inlines.Add(new System.Windows.Documents.Run($"Phí DV: {order.ServiceChargeAmount:N0}đ\n"));
        doc.Blocks.Add(totals);
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"TỔNG: {order.TotalAmount:N0}đ"))
        {
            FontSize = 18, FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center, Foreground = orange
        });
        if (order.PaidAmount > 0)
        {
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Đã TT: {order.PaidAmount:N0}đ  |  Còn lại: {order.RemainingAmount:N0}đ"))
            {
                FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new System.Windows.Thickness(0, 4, 0, 0), Foreground = gray
            });
        }

        var footerText = "Cảm ơn quý khách! Hẹn gặp lại.";
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(footerText))
        {
            FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 8, 0, 0)
        });

        return doc;
    }

    [RelayCommand]
    private void PrintQuote()
    {
        var doc = BuildQuoteFlowDocument();
        ShowPrintPreview(doc);
    }

    private System.Windows.Documents.FlowDocument BuildQuoteFlowDocument()
    {
        var doc = new System.Windows.Documents.FlowDocument
        {
            PageWidth = 793.7,
            PageHeight = 1122.5,
            ColumnWidth = 709.7,
            PagePadding = new System.Windows.Thickness(36, 28, 36, 28),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 10
        };
        var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235));
        var teal = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 118, 110));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        var red = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
        var green = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74));
        var orange = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 88, 12));
        var lightBg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));

        var useSaved = SelectedSavedQuote is not null;
        var customerName = useSaved ? SelectedSavedQuote!.CustomerName : ReservationCustomerName;
        var phone = useSaved ? SelectedSavedQuote!.Phone : ReservationPhone;
        var tableCount = useSaved ? SelectedSavedQuote!.TableCount : Math.Max(1, ReservationGuestCount);
        var guestCount = useSaved ? Math.Max(1, SelectedSavedQuote!.GuestCount) : Math.Max(1, QuoteGuestCount);
        var roomName = useSaved ? SelectedSavedQuote!.RoomName : SelectedRoom?.Name ?? "Chưa chọn";
        var date = useSaved ? SelectedSavedQuote!.EventDate : QuoteDate;
        var startTime = useSaved ? SelectedSavedQuote!.StartTime : QuoteStartTime;
        var endTime = useSaved ? SelectedSavedQuote!.EndTime : QuoteEndTime;
        var lines = useSaved
            ? SelectedSavedQuote!.Lines.Select(l => new QuoteLineItem { Name = l.Name, UnitPrice = l.UnitPrice, Quantity = l.Quantity, Type = l.Type }).ToList()
            : QuoteLines.ToList();
        var discount = QuoteDiscount;
        var deposit = QuoteDeposit;
        var foodLines = lines.Where(l => l.Type != "Dịch vụ").ToList();
        var serviceLines = lines.Where(l => l.Type == "Dịch vụ").ToList();
        var foodSubtotal = foodLines.Sum(l => l.UnitPrice * l.Quantity);
        var serviceSubtotal = serviceLines.Sum(l => l.UnitPrice * l.Quantity);
        var grandSub = foodSubtotal * tableCount + serviceSubtotal;
        var vatAmount = grandSub * 8m / 100m;
        var finalTotal = Math.Max(0, grandSub + vatAmount - discount);
        var remaining = Math.Max(0, finalTotal - deposit);
        var quoteNo = useSaved ? SelectedSavedQuote!.QuoteNo : QuoteNo;
        var preparedBy = useSaved ? SelectedSavedQuote!.PreparedBy : PreparedByName;
        var createdAt = useSaved ? SelectedSavedQuote!.CreatedAt : DateTime.Now;
        var validityDays = useSaved ? SelectedSavedQuote!.ValidityDays : QuoteValidityDays;
        var cancelDeadline = useSaved ? SelectedSavedQuote!.CancelDeadlineDays : QuoteCancelDeadlineDays;
        var cancelFee = useSaved ? SelectedSavedQuote!.CancelFeePercent : QuoteCancelFeePercent;
        var promotion = useSaved ? (SelectedSavedQuote!.Promotion ?? "") : QuotePromotion ?? "";
        var status = useSaved ? SelectedSavedQuote!.Status : QuoteStatus;
        var note = useSaved ? (SelectedSavedQuote!.Note ?? "") : QuoteNote ?? "";

        // ===== HEADER =====
        var headerTable = new System.Windows.Documents.Table { CellSpacing = 0, Margin = new System.Windows.Thickness(0, 0, 0, 12) };
        headerTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(410) });
        headerTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(300) });
        var hdrGrp = new System.Windows.Documents.TableRowGroup();
        var hdrRow = new System.Windows.Documents.TableRow();
        // Left: Restaurant info
        var leftStack = new System.Windows.Documents.TableCell { Padding = new System.Windows.Thickness(0, 0, 10, 0) };
        var leftP = new System.Windows.Documents.Paragraph();
        leftP.Inlines.Add(new System.Windows.Documents.Run(RestaurantQuoteTitle) { FontSize = 16, FontWeight = System.Windows.FontWeights.Bold, Foreground = blue });
        if (!string.IsNullOrWhiteSpace(RestaurantAddress))
            leftP.Inlines.Add(new System.Windows.Documents.Run($"\n{RestaurantAddress}") { FontSize = 9, Foreground = gray });
        if (!string.IsNullOrWhiteSpace(RestaurantPhone))
            leftP.Inlines.Add(new System.Windows.Documents.Run($"\nHotline: {RestaurantPhone}") { FontSize = 10, FontWeight = System.Windows.FontWeights.SemiBold, Foreground = dark });
        if (!string.IsNullOrWhiteSpace(RestaurantEmail))
            leftP.Inlines.Add(new System.Windows.Documents.Run($"  |  Email: {RestaurantEmail}") { FontSize = 9, Foreground = gray });
        leftStack.Blocks.Add(leftP);
        hdrRow.Cells.Add(leftStack);
        // Right: Document info box
        var rightBox = new System.Windows.Documents.TableCell { Padding = new System.Windows.Thickness(10, 0, 0, 0) };
        var rtGrp = new System.Windows.Documents.TableRowGroup();
        hdrGrp.Rows.Add(hdrRow);
        headerTable.RowGroups.Add(hdrGrp);
        doc.Blocks.Add(headerTable);

        // Separator line
        var line1 = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 0, 0, 8), BorderBrush = blue, BorderThickness = new System.Windows.Thickness(0, 0, 0, 2) };
        line1.Inlines.Add(new System.Windows.Documents.Run(" ") { FontSize = 1 });
        doc.Blocks.Add(line1);

        // Title
        var titleP = new System.Windows.Documents.Paragraph { TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 8, 0, 4) };
        titleP.Inlines.Add(new System.Windows.Documents.Run("BÁO GIÁ DỊCH VỤ TIỆC / SỰ KIỆN") { FontSize = 16, FontWeight = System.Windows.FontWeights.Bold, Foreground = dark });
        doc.Blocks.Add(titleP);

        // ===== CUSTOMER + DOC INFO (2 columns side by side) =====
        var infoTable = new System.Windows.Documents.Table { CellSpacing = 0, Margin = new System.Windows.Thickness(0, 8, 0, 8), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), BorderThickness = new System.Windows.Thickness(1) };
        infoTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(355) });
        infoTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(355) });
        var infoGrp = new System.Windows.Documents.TableRowGroup();
        // Left cell: customer info
        var leftCell = new System.Windows.Documents.TableCell { Padding = new System.Windows.Thickness(0), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), BorderThickness = new System.Windows.Thickness(0, 0, 1, 0) };
        var leftSubTbl = new System.Windows.Documents.Table { CellSpacing = 0 };
        leftSubTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(110) });
        leftSubTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(244) });
        var leftGrp = new System.Windows.Documents.TableRowGroup();
        void LeftRow(string label, string val)
        {
            var r = new System.Windows.Documents.TableRow();
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(label) { FontSize = 10, FontWeight = System.Windows.FontWeights.SemiBold, Foreground = dark }) { Background = lightBg }) { Padding = new System.Windows.Thickness(10, 4, 6, 4) });
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(val) { FontSize = 10, Foreground = dark })) { Padding = new System.Windows.Thickness(10, 4, 6, 4) });
            leftGrp.Rows.Add(r);
        }
        LeftRow("Khách hàng", customerName);
        LeftRow("Số điện thoại", phone);
        LeftRow("Phòng / Khu vực", roomName);
        LeftRow("Số bàn", $"{tableCount} bàn  ·  Chuẩn 10 khách/bàn  ·  Tổng {guestCount} khách");
        LeftRow("Thời gian", $"{date:dd/MM/yyyy}  ·  {startTime} - {endTime}");
        leftSubTbl.RowGroups.Add(leftGrp);
        leftCell.Blocks.Add(leftSubTbl);
        // Right cell: document info
        var rightCell = new System.Windows.Documents.TableCell { Padding = new System.Windows.Thickness(0) };
        var rightSubTbl = new System.Windows.Documents.Table { CellSpacing = 0 };
        rightSubTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(100) });
        rightSubTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(254) });
        var rightGrp = new System.Windows.Documents.TableRowGroup();
        void RightRow(string label, string val)
        {
            var r = new System.Windows.Documents.TableRow();
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(label) { FontSize = 10, FontWeight = System.Windows.FontWeights.SemiBold, Foreground = dark }) { Background = lightBg }) { Padding = new System.Windows.Thickness(10, 4, 6, 4) });
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(val) { FontSize = 10, Foreground = dark })) { Padding = new System.Windows.Thickness(10, 4, 6, 4) });
            rightGrp.Rows.Add(r);
        }
        RightRow("Số báo giá", quoteNo);
        RightRow("Ngày lập", createdAt.ToString("dd/MM/yyyy"));
        RightRow("Người lập", preparedBy);
        RightRow("Trạng thái", status);
        rightSubTbl.RowGroups.Add(rightGrp);
        rightCell.Blocks.Add(rightSubTbl);
        // Assemble row
        var infoRow = new System.Windows.Documents.TableRow();
        infoRow.Cells.Add(leftCell);
        infoRow.Cells.Add(rightCell);
        infoGrp.Rows.Add(infoRow);
        infoTable.RowGroups.Add(infoGrp);
        doc.Blocks.Add(infoTable);

        // ===== SECTION: MÓN ĂN =====
        if (foodLines.Count > 0)
        {
            var secTitle = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 12, 0, 4) };
            secTitle.Inlines.Add(new System.Windows.Documents.Run("A. MÓN ĂN") { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, Foreground = blue });
            doc.Blocks.Add(secTitle);

            var ft = new System.Windows.Documents.Table { CellSpacing = 0 };
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(35) });
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(240) });
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(45) });
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(45) });
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(100) });
            ft.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(170) });
            var fhdr = new System.Windows.Documents.TableRowGroup();
            var fhr = new System.Windows.Documents.TableRow { Background = blue };
            fhr.Cells.Add(Tc("STT", System.Windows.Media.Brushes.White, true, 9));
            fhr.Cells.Add(Tc("Tên món", System.Windows.Media.Brushes.White, true, 9));
            fhr.Cells.Add(Tc("ĐVT", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Center));
            fhr.Cells.Add(Tc("SL", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Center));
            fhr.Cells.Add(Tc("Đơn giá", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Right));
            fhr.Cells.Add(Tc("Thành tiền", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Right));
            fhdr.Rows.Add(fhr);
            ft.RowGroups.Add(fhdr);
            var fbody = new System.Windows.Documents.TableRowGroup();
            int idx = 1;
            foreach (var l in foodLines)
            {
                var lineTotal = l.UnitPrice * l.Quantity * tableCount;
                var r = new System.Windows.Documents.TableRow();
                r.Cells.Add(Tc($"{idx++}", dark, fontSz: 9));
                r.Cells.Add(Tc(l.Name, dark, fontSz: 9));
                r.Cells.Add(Tc("Phần", dark, fontSz: 9, align: System.Windows.TextAlignment.Center));
                r.Cells.Add(Tc($"{l.Quantity}", dark, fontSz: 9, align: System.Windows.TextAlignment.Center));
                r.Cells.Add(Tc($"{l.UnitPrice:N0} đ", dark, fontSz: 9, align: System.Windows.TextAlignment.Right));
                r.Cells.Add(Tc($"{lineTotal:N0} đ", dark, true, 9, System.Windows.TextAlignment.Right));
                fbody.Rows.Add(r);
            }
            // Food subtotal row
            var fsr = new System.Windows.Documents.TableRow { Background = lightBg };
            fsr.Cells.Add(Tc("", dark, fontSz: 9));
            fsr.Cells.Add(Tc($"Cộng món ăn ({tableCount} bàn x {foodSubtotal:N0} đ)", dark, true, 9));
            fsr.Cells.Add(Tc("", dark, fontSz: 9));
            fsr.Cells.Add(Tc("", dark, fontSz: 9));
            fsr.Cells.Add(Tc("", dark, fontSz: 9));
            fsr.Cells.Add(Tc($"{foodSubtotal * tableCount:N0} đ", dark, true, 9, System.Windows.TextAlignment.Right));
            fbody.Rows.Add(fsr);
            ft.RowGroups.Add(fbody);
            doc.Blocks.Add(ft);
        }

        // ===== SECTION: DỊCH VỤ =====
        if (serviceLines.Count > 0)
        {
            var secTitle = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 12, 0, 4) };
            secTitle.Inlines.Add(new System.Windows.Documents.Run("B. DỊCH VỤ") { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, Foreground = teal });
            doc.Blocks.Add(secTitle);

            var st = new System.Windows.Documents.Table { CellSpacing = 0 };
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(35) });
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(240) });
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(45) });
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(45) });
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(100) });
            st.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(170) });
            var shdr = new System.Windows.Documents.TableRowGroup();
            var shr = new System.Windows.Documents.TableRow { Background = teal };
            shr.Cells.Add(Tc("STT", System.Windows.Media.Brushes.White, true, 9));
            shr.Cells.Add(Tc("Tên dịch vụ", System.Windows.Media.Brushes.White, true, 9));
            shr.Cells.Add(Tc("ĐVT", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Center));
            shr.Cells.Add(Tc("SL", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Center));
            shr.Cells.Add(Tc("Đơn giá", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Right));
            shr.Cells.Add(Tc("Thành tiền", System.Windows.Media.Brushes.White, true, 9, System.Windows.TextAlignment.Right));
            shdr.Rows.Add(shr);
            st.RowGroups.Add(shdr);
            var sbody = new System.Windows.Documents.TableRowGroup();
            int idx2 = 1;
            foreach (var l in serviceLines)
            {
                var lineTotal = l.UnitPrice * l.Quantity;
                var r = new System.Windows.Documents.TableRow();
                r.Cells.Add(Tc($"{idx2++}", dark, fontSz: 9));
                r.Cells.Add(Tc(l.Name, dark, fontSz: 9));
                r.Cells.Add(Tc("Gói", dark, fontSz: 9, align: System.Windows.TextAlignment.Center));
                r.Cells.Add(Tc($"{l.Quantity}", dark, fontSz: 9, align: System.Windows.TextAlignment.Center));
                r.Cells.Add(Tc($"{l.UnitPrice:N0} đ", dark, fontSz: 9, align: System.Windows.TextAlignment.Right));
                r.Cells.Add(Tc($"{lineTotal:N0} đ", dark, fontSz: 9, align: System.Windows.TextAlignment.Right));
                sbody.Rows.Add(r);
            }
            var ssr = new System.Windows.Documents.TableRow { Background = lightBg };
            ssr.Cells.Add(Tc("", dark, fontSz: 9));
            ssr.Cells.Add(Tc("Cộng dịch vụ", dark, true, 9));
            ssr.Cells.Add(Tc("", dark, fontSz: 9));
            ssr.Cells.Add(Tc("", dark, fontSz: 9));
            ssr.Cells.Add(Tc("", dark, fontSz: 9));
            ssr.Cells.Add(Tc($"{serviceSubtotal:N0} đ", dark, true, 9, System.Windows.TextAlignment.Right));
            sbody.Rows.Add(ssr);
            st.RowGroups.Add(sbody);
            doc.Blocks.Add(st);
        }

        // ===== SECTION: TỔNG HỢP THANH TOÁN =====
        var secTitle3 = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 14, 0, 4) };
        secTitle3.Inlines.Add(new System.Windows.Documents.Run("C. TỔNG HỢP THANH TOÁN") { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, Foreground = orange });
        doc.Blocks.Add(secTitle3);

        var totTbl = new System.Windows.Documents.Table { CellSpacing = 0, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), BorderThickness = new System.Windows.Thickness(1) };
        totTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(300) });
        totTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(145) });
        var totGrp = new System.Windows.Documents.TableRowGroup();
        void TotRow(string label, decimal val, System.Windows.Media.Brush? fg = null, bool bold = false, double fs = 10)
        {
            var r = new System.Windows.Documents.TableRow();
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(label) { FontSize = fs, FontWeight = bold ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal, Foreground = fg ?? dark })) { Padding = new System.Windows.Thickness(12, 5, 6, 5), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), BorderThickness = new System.Windows.Thickness(0, 0, 0, 1) });
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(val == 0 && label.Contains("Phí") ? "—" : $"{val:N0} đ") { FontSize = fs, FontWeight = bold ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal, Foreground = fg ?? dark })) { Padding = new System.Windows.Thickness(6, 5, 12, 5), BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), BorderThickness = new System.Windows.Thickness(0, 0, 0, 1), TextAlignment = System.Windows.TextAlignment.Right });
            totGrp.Rows.Add(r);
        }
        TotRow($"Tạm tính món ăn ({tableCount} bàn x {foodSubtotal:N0} đ)", foodSubtotal * tableCount);
        if (serviceSubtotal > 0) TotRow("Tạm tính dịch vụ", serviceSubtotal);
        TotRow("Tạm tính tổng", grandSub, dark, true, 11);
        if (discount > 0) TotRow("Giảm giá", -discount, red);
        TotRow("Thuế VAT (8%)", vatAmount);
        TotRow("Phí phục vụ", 0);
        // Separator row
        var sepR = new System.Windows.Documents.TableRow();
        sepR.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("TỔNG CỘNG") { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, Foreground = red })) { Padding = new System.Windows.Thickness(12, 7, 6, 7), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242)) });
        sepR.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{finalTotal:N0} đ") { FontSize = 16, FontWeight = System.Windows.FontWeights.Bold, Foreground = red })) { Padding = new System.Windows.Thickness(6, 7, 12, 7), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242)), TextAlignment = System.Windows.TextAlignment.Right });
        totGrp.Rows.Add(sepR);
        TotRow("Đặt cọc trước", deposit, deposit > 0 ? green : gray);
        TotRow("Số tiền còn lại", remaining, deposit > 0 ? orange : gray, deposit > 0, deposit > 0 ? 12 : 10);
        totTbl.RowGroups.Add(totGrp);
        // Right-align totals table
        // Right-align totals: wrap in a 2-col table (empty left, totals right)
        var alignTable = new System.Windows.Documents.Table { CellSpacing = 0 };
        alignTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(200) });
        alignTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(435) });
        var alignGrp = new System.Windows.Documents.TableRowGroup();
        var alignRow = new System.Windows.Documents.TableRow();
        alignRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(""))));
        var totRightCell = new System.Windows.Documents.TableCell();
        totRightCell.Blocks.Add(totTbl);
        alignRow.Cells.Add(totRightCell);
        alignGrp.Rows.Add(alignRow);
        alignTable.RowGroups.Add(alignGrp);
        doc.Blocks.Add(alignTable);

        // ===== SECTION: ĐIỀU KHOẢN & GHI CHÚ =====
        var secTitle4 = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 14, 0, 4) };
        secTitle4.Inlines.Add(new System.Windows.Documents.Run("D. ĐIỀU KHOẢN & GHI CHÚ") { FontSize = 13, FontWeight = System.Windows.FontWeights.Bold, Foreground = dark });
        doc.Blocks.Add(secTitle4);

        var termsTbl = new System.Windows.Documents.Table { CellSpacing = 0 };
        termsTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(160) });
        termsTbl.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(540) });
        var termsGrp = new System.Windows.Documents.TableRowGroup();
        void TermRow(string label, string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return;
            var r = new System.Windows.Documents.TableRow();
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(label) { FontSize = 10, Foreground = gray })) { Padding = new System.Windows.Thickness(0, 3, 8, 3) });
            r.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(val) { FontSize = 10, Foreground = dark })) { Padding = new System.Windows.Thickness(0, 3, 0, 3) });
            termsGrp.Rows.Add(r);
        }
        TermRow("Hiệu lực báo giá:", $"{validityDays} ngày kể từ ngày lập ({createdAt.AddDays(validityDays):dd/MM/yyyy})");
        TermRow("Thời hạn hủy:", $"Trước {cancelDeadline} ngày so với ngày phục vụ. Phí hủy: {cancelFee:F0}% tổng giá trị.");
        TermRow("Phương thức TT:", useSaved ? (SelectedSavedQuote!.PaymentMethod ?? "Chuyển khoản") : "Chuyển khoản");
        TermRow("Tài khoản nhận:", useSaved ? (SelectedSavedQuote!.BankAccount ?? "") : "");
        if (!string.IsNullOrWhiteSpace(promotion))
            TermRow("Khuyến mại:", promotion);
        if (!string.IsNullOrWhiteSpace(note))
            TermRow("Ghi chú:", note);
        termsTbl.RowGroups.Add(termsGrp);
        doc.Blocks.Add(termsTbl);

        // ===== SIGNATURES =====
        var sigTable = new System.Windows.Documents.Table { CellSpacing = 0, Margin = new System.Windows.Thickness(0, 40, 0, 0) };
        sigTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(365) });
        sigTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(365) });
        var sigGrp = new System.Windows.Documents.TableRowGroup();
        var sigRow = new System.Windows.Documents.TableRow();
        sigRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("ĐẠI DIỆN KHÁCH HÀNG") { FontSize = 11, FontWeight = System.Windows.FontWeights.Bold, Foreground = dark }) { TextAlignment = System.Windows.TextAlignment.Center, Padding = new System.Windows.Thickness(0, 10, 10, 4) }));
        sigRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("ĐẠI DIỆN NHÀ HÀNG") { FontSize = 11, FontWeight = System.Windows.FontWeights.Bold, Foreground = dark }) { TextAlignment = System.Windows.TextAlignment.Center, Padding = new System.Windows.Thickness(10, 10, 0, 4) }));
        sigGrp.Rows.Add(sigRow);
        var sigRow2 = new System.Windows.Documents.TableRow();
        sigRow2.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("\n\n\n\n(Ký, ghi rõ họ tên)") { FontSize = 9, Foreground = gray }) { TextAlignment = System.Windows.TextAlignment.Center }));
        sigRow2.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("\n\n\n\n(Ký, ghi rõ họ tên)") { FontSize = 9, Foreground = gray }) { TextAlignment = System.Windows.TextAlignment.Center }));
        sigGrp.Rows.Add(sigRow2);
        sigTable.RowGroups.Add(sigGrp);
        doc.Blocks.Add(sigTable);

        return doc;
    }

    private static System.Windows.Documents.TableCell Tc(string text, System.Windows.Media.Brush? fg = null, bool bold = false, double fontSz = 9, System.Windows.TextAlignment align = System.Windows.TextAlignment.Left)
    {
        var p = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text)) { FontSize = fontSz, TextAlignment = align, Margin = new System.Windows.Thickness(5, 3, 5, 3) };
        if (bold) p.FontWeight = System.Windows.FontWeights.Bold;
        if (fg is not null) p.Foreground = fg;
        return new System.Windows.Documents.TableCell(p) { BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), BorderThickness = new System.Windows.Thickness(0, 0, 0, 0.5) };
    }

    private static void AddSectionTitle(System.Windows.Documents.FlowDocument doc, string title, System.Windows.Media.Brush brush)
    {
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
        {
            FontSize = 11.5,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = brush,
            Margin = new System.Windows.Thickness(0, 10, 0, 5)
        });
    }

    private static System.Windows.Documents.Block EmptyPrintNote(string text)
    {
        return new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
        {
            FontSize = 9.5,
            FontStyle = System.Windows.FontStyles.Italic,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
            Margin = new System.Windows.Thickness(0, 0, 0, 8)
        };
    }

    private static System.Windows.Documents.Table BuildInfoTable(params (string Label, string Value)[] items)
    {
        var table = new System.Windows.Documents.Table
        {
            CellSpacing = 0,
            Margin = new System.Windows.Thickness(0, 0, 0, 8),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 230, 243)),
            BorderThickness = new System.Windows.Thickness(1)
        };
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(345) });
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(345) });
        table.RowGroups.Add(new System.Windows.Documents.TableRowGroup());
        for (var i = 0; i < items.Length; i += 2)
        {
            var row = new System.Windows.Documents.TableRow();
            row.Cells.Add(InfoCell(items[i].Label, items[i].Value));
            row.Cells.Add(i + 1 < items.Length ? InfoCell(items[i + 1].Label, items[i + 1].Value) : InfoCell(string.Empty, string.Empty));
            table.RowGroups[0].Rows.Add(row);
        }
        return table;
    }

    private static System.Windows.Documents.TableCell InfoCell(string label, string value)
    {
        var p = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(8, 3, 8, 3), FontSize = 9.5, LineHeight = 13 };
        if (!string.IsNullOrWhiteSpace(label))
        {
            p.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)) });
            p.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = System.Windows.FontWeights.SemiBold });
        }
        return new System.Windows.Documents.TableCell(p)
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new System.Windows.Thickness(0, 0, 0.5, 0.5)
        };
    }

    private static System.Windows.Documents.Table BuildSummaryTable(IEnumerable<(string Label, string Value, bool Bold, bool Negative)> rows)
    {
        var table = new System.Windows.Documents.Table
        {
            CellSpacing = 0,
            Margin = new System.Windows.Thickness(250, 0, 0, 8),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new System.Windows.Thickness(1)
        };
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(260) });
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(120) });
        table.RowGroups.Add(new System.Windows.Documents.TableRowGroup());
        foreach (var rowData in rows)
        {
            var row = new System.Windows.Documents.TableRow
            {
                Background = rowData.Bold ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 247, 237)) : System.Windows.Media.Brushes.White
            };
            row.Cells.Add(SummaryCell(rowData.Label, false, rowData.Bold, false));
            row.Cells.Add(SummaryCell(rowData.Value, true, rowData.Bold, rowData.Negative));
            table.RowGroups[0].Rows.Add(row);
        }
        return table;
    }

    private static System.Windows.Documents.TableCell SummaryCell(string text, bool right, bool bold, bool negative)
    {
        var p = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
        {
            TextAlignment = right ? System.Windows.TextAlignment.Right : System.Windows.TextAlignment.Left,
            Margin = new System.Windows.Thickness(8, 3, 8, 3),
            FontSize = bold ? 10 : 9.5,
            LineHeight = 13,
            FontWeight = bold ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal,
            Foreground = negative ? System.Windows.Media.Brushes.Red : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42))
        };
        return new System.Windows.Documents.TableCell(p)
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new System.Windows.Thickness(0, 0, 0, 0.5)
        };
    }

    private static System.Windows.Documents.Table BuildSignatureTable()
    {
        var table = new System.Windows.Documents.Table { CellSpacing = 0, Margin = new System.Windows.Thickness(0, 18, 0, 0) };
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        table.RowGroups.Add(new System.Windows.Documents.TableRowGroup());
        var row = new System.Windows.Documents.TableRow();
        row.Cells.Add(SignatureCell("ĐẠI DIỆN KHÁCH HÀNG"));
        row.Cells.Add(SignatureCell("ĐẠI DIỆN NHÀ HÀNG"));
        table.RowGroups[0].Rows.Add(row);
        return table;
    }

    private static System.Windows.Documents.TableCell SignatureCell(string title)
    {
        var p = new System.Windows.Documents.Paragraph
        {
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0),
            FontSize = 10,
            FontWeight = System.Windows.FontWeights.Bold
        };
        p.Inlines.Add(new System.Windows.Documents.Run(title));
        p.Inlines.Add(new System.Windows.Documents.LineBreak());
        p.Inlines.Add(new System.Windows.Documents.LineBreak());
        p.Inlines.Add(new System.Windows.Documents.LineBreak());
        p.Inlines.Add(new System.Windows.Documents.Run("(Ký, ghi rõ họ tên)") { FontSize = 9, FontWeight = System.Windows.FontWeights.Normal });
        return new System.Windows.Documents.TableCell(p);
    }
    private static bool IsBarPreparationAreaStatic(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return code.StartsWith("DRINK", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("BAR", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("BEV", StringComparison.OrdinalIgnoreCase);
    }

    private System.Windows.Documents.FlowDocument BuildKitchenPrintPreviewDocument(
        List<(Guid Id, string Name, decimal Qty, string? Note)> kitchenItems,
        List<(Guid Id, string Name, decimal Qty, string? Note)> barItems)
    {
        const double paperWidth = 300;
        var order = SelectedOrder!;
        var doc = new System.Windows.Documents.FlowDocument
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new System.Windows.Thickness(4),
            PageWidth = paperWidth,
            ColumnWidth = paperWidth - 8
        };
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175));

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("PHIẾU BẾP / BAR"))
        {
            FontSize = 18, FontWeight = System.Windows.FontWeights.Bold,
            TextAlignment = System.Windows.TextAlignment.Center
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{order.OrderNo}  |  {SelectedOrderTableDisplayName}"))
        {
            TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 2, 0, 0)
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Phòng: {SelectedOrderRoomDisplayName}"))
        {
            TextAlignment = System.Windows.TextAlignment.Center, FontSize = 11, Foreground = gray
        });
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"Giờ gửi: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 6)
        });
        doc.Blocks.Add(Sep());

        void AddStation(string title, List<(Guid Id, string Name, decimal Qty, string? Note)> items)
        {
            if (items.Count == 0) return;
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(title))
            {
                FontSize = 14, FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 8, 0, 2),
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = blue
            });
            foreach (var item in items)
            {
                var p = new System.Windows.Documents.Paragraph
                {
                    FontSize = 13, Margin = new System.Windows.Thickness(0, 4, 0, 0),
                    TextAlignment = System.Windows.TextAlignment.Center
                };
                p.Inlines.Add(new System.Windows.Documents.Run($"{item.Qty:N1} x {item.Name}") { FontWeight = System.Windows.FontWeights.Bold });
                if (!string.IsNullOrWhiteSpace(item.Note))
                    p.Inlines.Add(new System.Windows.Documents.Run($"\n  Ghi chú: {item.Note}"));
                doc.Blocks.Add(p);
            }
        }

        AddStation("BẾP - ĐỒ ĂN", kitchenItems);
        AddStation("BAR - ĐỒ UỐNG", barItems);

        doc.Blocks.Add(Sep());
        doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Kiểm tra đúng khu chế biến trước khi in/gửi."))
        {
            FontSize = 11, TextAlignment = System.Windows.TextAlignment.Center
        });

        return doc;
    }

    [RelayCommand]
    private void PrintOrderKitchen()
    {
        if (SelectedOrder is null) { StatusText = "Vui lòng chọn đơn."; return; }
        if (!CanPrintKitchen) { StatusText = "Vui lòng chốt thực đơn trước khi in bếp."; return; }

        var foodItems = (SelectedOrder.Lines ?? Enumerable.Empty<AlacarteOrderLineDto>())
            .Where(l => !IsOrderServiceLine(l))
            .GroupBy(l => l.MenuItemId)
            .Select(g => (Id: g.Key, Name: g.First().MenuItemName, Qty: g.Sum(x => x.Qty), Note: g.First().KitchenNote))
            .ToList();

        var kitchenItems = new List<(Guid Id, string Name, decimal Qty, string? Note)>();
        var barItems = new List<(Guid Id, string Name, decimal Qty, string? Note)>();
        foreach (var item in foodItems)
        {
            var menuItem = OrderItemPickerMenuItems.FirstOrDefault(m => m.Id == item.Id);
            if (IsBarPreparationAreaStatic(menuItem?.Code))
                barItems.Add(item);
            else
                kitchenItems.Add(item);
        }

        var doc = BuildKitchenPrintPreviewDocument(kitchenItems, barItems);
        ShowKitchenPrintPreview(doc, kitchenItems, barItems);
    }

    private void ShowKitchenPrintPreview(System.Windows.Documents.FlowDocument doc,
        List<(Guid Id, string Name, decimal Qty, string? Note)> kitchenItems,
        List<(Guid Id, string Name, decimal Qty, string? Note)> barItems)
    {
        var tempFile = System.IO.Path.GetTempFileName();
        try
        {
            using (var xps = new System.Windows.Xps.Packaging.XpsDocument(tempFile, System.IO.FileAccess.ReadWrite))
            {
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xps);
                writer.Write(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator);
            }
            using (var xps = new System.Windows.Xps.Packaging.XpsDocument(tempFile, System.IO.FileAccess.Read))
            {
                var fixedDoc = xps.GetFixedDocumentSequence();
                var w = new System.Windows.Window { Title = "Xem trước in - Phiếu bếp/bar", Width = 420, Height = 700, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner };
                var g = new System.Windows.Controls.Grid();
                g.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
                g.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                var dv = new System.Windows.Controls.DocumentViewer { Document = fixedDoc };
                System.Windows.Controls.Grid.SetRow(dv, 0); g.Children.Add(dv);
                var bp = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(12) };
                var pb = new System.Windows.Controls.Button { Content = "In", Width = 80, Height = 32 };
                pb.Click += async (_, _) =>
                {
                    try
                    {
                        bool printed = false;
                        if (kitchenItems.Count > 0 && !string.IsNullOrWhiteSpace(KitchenPrinterName))
                        {
                            var kitchenText = BuildStationThermalText("BẾP", kitchenItems);
                            if (TryParseNetworkPrinterTarget(KitchenPrinterName, out var kHost, out var kPort))
                                await SendRawThermalPrintAsync(kHost, kPort, kitchenText);
                            else
                                PrintFlowDocumentToWindowsPrinter(BuildStationDocument("BẾP", kitchenItems), KitchenPrinterName, "Phiếu bếp");
                            printed = true;
                        }
                        if (barItems.Count > 0 && !string.IsNullOrWhiteSpace(BarPrinterName))
                        {
                            var barText = BuildStationThermalText("BAR", barItems);
                            if (TryParseNetworkPrinterTarget(BarPrinterName, out var bHost, out var bPort))
                                await SendRawThermalPrintAsync(bHost, bPort, barText);
                            else
                                PrintFlowDocumentToWindowsPrinter(BuildStationDocument("BAR", barItems), BarPrinterName, "Phiếu bar");
                            printed = true;
                        }
                        if (!printed)
                        {
                            var pd = new System.Windows.Controls.PrintDialog();
                            if (pd.ShowDialog() == true) pd.PrintDocument(fixedDoc.DocumentPaginator, "Phiếu bếp/bar");
                        }
                        else
                        {
                            var parts = new List<string>();
                            if (kitchenItems.Count > 0) parts.Add($"bếp ({kitchenItems.Count} món)");
                            if (barItems.Count > 0) parts.Add($"bar ({barItems.Count} món)");
                            StatusText = $"Đã gửi phiếu tới {string.Join(" và ", parts)}.";
                        }
                    }
                    catch (Exception ex) { StatusText = $"Lỗi in: {ex.Message}"; }
                };
                var cb = new System.Windows.Controls.Button { Content = "Đóng", Width = 80, Height = 32, Margin = new System.Windows.Thickness(8, 0, 0, 0) };
                cb.Click += (_, _) => w.Close();
                bp.Children.Add(pb); bp.Children.Add(cb);
                System.Windows.Controls.Grid.SetRow(bp, 1); g.Children.Add(bp);
                w.Content = g; w.ShowDialog();
            }
        }
        finally { try { System.IO.File.Delete(tempFile); } catch { } }
    }

    private string BuildStationThermalText(string station, List<(Guid Id, string Name, decimal Qty, string? Note)> items)
    {
        const int cols = 32;
        var order = SelectedOrder!;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\x1B\x40");
        sb.AppendLine("\x1B\x21\x10");
        sb.AppendLine(CenterThermalText($"PHIẾU {station}", cols));
        sb.AppendLine("\x1B\x21\x00");
        sb.AppendLine(CenterThermalText("ASMILE BO", cols));
        sb.AppendLine(CenterThermalText($"Phòng: {SelectedOrderRoomDisplayName}", cols));
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
            sb.AppendLine(CenterThermalText($"KH: {order.CustomerName}", cols));
        sb.AppendLine(CenterThermalText($"Bàn: {SelectedOrderTableDisplayName}", cols));
        sb.AppendLine(CenterThermalText($"Mã: {order.OrderNo}", cols));
        sb.AppendLine(CenterThermalText($"Ngày giờ gửi: {DateTime.Now:dd/MM/yyyy HH:mm}", cols));
        sb.AppendLine(new string('─', cols));
        foreach (var item in items)
        {
            sb.AppendLine($"{item.Name}");
            sb.AppendLine($"  x{item.Qty:N0}");
            if (!string.IsNullOrWhiteSpace(item.Note))
                sb.AppendLine($"  GC: {item.Note}");
        }
        sb.AppendLine(new string('─', cols));
        sb.AppendLine(CenterThermalText($"Tổng: {items.Count} món  {items.Sum(x => x.Qty):N0} SL", cols));
        sb.AppendLine(CenterThermalText($"In lúc: {DateTime.Now:HH:mm dd/MM/yyyy}", cols));
        sb.AppendLine("\n\n\n");
        return sb.ToString();
    }

    private System.Windows.Documents.FlowDocument BuildStationDocument(string station, List<(Guid Id, string Name, decimal Qty, string? Note)> items)
    {
        var order = SelectedOrder!;
        var doc = new System.Windows.Documents.FlowDocument
        {
            PageWidth = 297.0, PageHeight = 420.0, ColumnWidth = 257.0,
            PagePadding = new System.Windows.Thickness(20, 10, 20, 10),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 9
        };
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235));

        var title = new System.Windows.Documents.Paragraph { TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0, 0, 0, 4) };
        title.Inlines.Add(new System.Windows.Documents.Run($"PHIẾU {station}") { FontSize = 14, FontWeight = System.Windows.FontWeights.Bold, Foreground = dark });
        doc.Blocks.Add(title);
        var info = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 0, 0, 4) };
        info.Inlines.Add(new System.Windows.Documents.Run($"Bàn: {SelectedOrderTableDisplayName}") { FontSize = 9, Foreground = dark });
        info.Inlines.Add(new System.Windows.Documents.Run($"  Mã: {order.OrderNo}") { FontSize = 8, Foreground = gray });
        doc.Blocks.Add(info);
        doc.Blocks.Add(Sep());

        foreach (var item in items)
        {
            var p = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0, 0, 0, 6), FontSize = 11 };
            p.Inlines.Add(new System.Windows.Documents.Run($"{item.Name}") { FontWeight = System.Windows.FontWeights.Bold, Foreground = dark });
            p.Inlines.Add(new System.Windows.Documents.Run($"  x{item.Qty:N0}") { Foreground = blue });
            if (!string.IsNullOrWhiteSpace(item.Note))
                p.Inlines.Add(new System.Windows.Documents.Run($"\n  Ghi chú: {item.Note}") { FontSize = 8, Foreground = gray });
            doc.Blocks.Add(p);
        }
        doc.Blocks.Add(Sep());
        var foot = new System.Windows.Documents.Paragraph { TextAlignment = System.Windows.TextAlignment.Center };
        foot.Inlines.Add(new System.Windows.Documents.Run($"In lúc: {DateTime.Now:HH:mm dd/MM/yyyy}") { FontSize = 7, Foreground = gray });
        doc.Blocks.Add(foot);
        return doc;
    }

    private static void ShowPrintPreview(System.Windows.Documents.FlowDocument doc)
    {
        // Convert to FixedDocument for WYSIWYG preview and printing
        var tempFile = System.IO.Path.GetTempFileName();
        try
        {
            using (var xps = new System.Windows.Xps.Packaging.XpsDocument(tempFile, System.IO.FileAccess.ReadWrite))
            {
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xps);
                writer.Write(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator);
            }
            using (var xps = new System.Windows.Xps.Packaging.XpsDocument(tempFile, System.IO.FileAccess.Read))
            {
                var fixedDoc = xps.GetFixedDocumentSequence();
                var w = new System.Windows.Window { Title = "Xem trước in - Bảng báo giá", Width = 980, Height = 760, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner };
                var g = new System.Windows.Controls.Grid();
                g.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
                g.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                var dv = new System.Windows.Controls.DocumentViewer { Document = fixedDoc };
                System.Windows.Controls.Grid.SetRow(dv, 0); g.Children.Add(dv);
                var bp = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new System.Windows.Thickness(12) };
                var pb = new System.Windows.Controls.Button { Content = "In", Width = 80, Height = 32 };
                pb.Click += (_, _) => { var pd = new System.Windows.Controls.PrintDialog(); if (pd.ShowDialog() == true) pd.PrintDocument(fixedDoc.DocumentPaginator, "Báo giá"); };
                var cb = new System.Windows.Controls.Button { Content = "Đóng", Width = 80, Height = 32, Margin = new System.Windows.Thickness(8, 0, 0, 0) };
                cb.Click += (_, _) => w.Close();
                bp.Children.Add(pb); bp.Children.Add(cb);
                System.Windows.Controls.Grid.SetRow(bp, 1); g.Children.Add(bp);
                w.Content = g; w.ShowDialog();
            }
        }
        finally { if (System.IO.File.Exists(tempFile)) { try { System.IO.File.Delete(tempFile); } catch { } } }
    }

    [RelayCommand]
    private async Task ApproveQuoteAsync(SavedQuoteDto? quote)
    {
        if (quote is null) return;
        if (quote.ApprovedOrderId.HasValue)
        {
            StatusText = $"Báo giá {quote.QuoteNo} đã được duyệt trước đó.";
            return;
        }

        try
        {
            var approved = await _service.ApproveQuoteAsync(quote.Id, PreparedByName);
            ReplaceSavedQuote(approved);
            Reset(Reservations, await _service.GetReservationsAsync());
            Reset(OpenOrders, await _service.GetOpenOrdersAsync());
            RefreshOpenOrderRooms();
            RefreshSavedQuoteWorkflowStates();
            RefreshRoomReservationCalendarState();
            QuoteListTabIndex = 0;
            ApplyOpenOrderFilter();
            var order = OpenOrders.FirstOrDefault(x => x.Id == approved.ApprovedOrderId);
            if (order is not null) SelectedOrder = order;
            SelectedTabIndex = 3;
            StatusText = $"Đã duyệt báo giá {approved.QuoteNo} → đơn mở {order?.OrderNo ?? approved.ApprovedOrderId?.ToString()}.";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Approve alacarte quote failed");
            StatusText = $"Không duyệt được báo giá: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteQuoteAsync(SavedQuoteDto? quote)
    {
        if (quote is null)
        {
            StatusText = "Vui lòng chọn báo giá cần xóa.";
            return;
        }

        try
        {
            await _service.DeleteQuoteAsync(quote.Id);
            SavedQuotes.Remove(quote);
            if (ReferenceEquals(SelectedSavedQuote, quote))
            {
                SelectedSavedQuote = SavedQuotes.FirstOrDefault();
                if (SelectedSavedQuote is not null)
                {
                    ApplySavedQuoteDetails(SelectedSavedQuote);
                }
            }

            NotifyCollectionState();
            StatusText = $"Đã xóa báo giá {quote.QuoteNo}.";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Delete alacarte quote failed");
            StatusText = $"Không xóa được báo giá: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RequestChangeAsync(int newGuestCount)
    {
        if (SelectedSavedQuote is null)
        {
            StatusText = "Vui lòng chọn báo giá cần thay đổi.";
            return;
        }
        if (!SelectedSavedQuote.ApprovedOrderId.HasValue)
        {
            StatusText = "Báo giá chưa được duyệt, không thể yêu cầu thay đổi.";
            return;
        }
        try
        {
            var preview = await _quoteChangeService.PreviewAdjustmentAsync(
                new PreviewAdjustmentRequest { QuoteId = SelectedSavedQuote.Id, NewGuestCount = newGuestCount });
            if (!preview.Preview.CanApply)
            {
                StatusText = $"Không thể áp dụng thay đổi: đơn đang ở trạng thái '{preview.Preview.OrderStatus}'.";
                return;
            }
            var cr = await _quoteChangeService.RequestChangeAsync(new RequestChangeRequest
            {
                QuoteId = SelectedSavedQuote.Id,
                NewGuestCount = newGuestCount,
                Reason = $"Yêu cầu đổi số khách từ {preview.Preview.PreviousGuestCount} → {newGuestCount}",
                RequestedBy = PreparedByName
            });
            StatusText = $"Đã tạo yêu cầu thay đổi {cr.RequestNumber}: {cr.PreviousGuestCount} → {cr.NewGuestCount} khách (chênh lệch {cr.AmountDelta:N0}đ).";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Request change failed");
            StatusText = $"Không tạo được yêu cầu thay đổi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ApplyChangeAsync()
    {
        if (SelectedSavedQuote is null)
        {
            StatusText = "Vui lòng chọn báo giá.";
            return;
        }
        try
        {
            var history = await _quoteChangeService.GetChangeHistoryAsync(SelectedSavedQuote.Id);
            var pendingOrApproved = history.FirstOrDefault(x => x.Status is "Pending" or "Approved");
            if (pendingOrApproved is null)
            {
                StatusText = "Không có yêu cầu thay đổi nào đang chờ áp dụng.";
                return;
            }
            if (pendingOrApproved.Status == "Pending")
            {
                await _quoteChangeService.ApproveChangeRequestAsync(new ReviewChangeRequest
                {
                    ChangeRequestId = pendingOrApproved.Id,
                    ReviewedBy = PreparedByName
                });
                StatusText = $"Đã duyệt yêu cầu {pendingOrApproved.RequestNumber}. Đang áp dụng...";
            }
            await _quoteChangeService.ApplyChangeAsync(new ApplyChangeRequest
            {
                ChangeRequestId = pendingOrApproved.Id,
                AppliedBy = PreparedByName
            });
            await RefreshAsync();
            StatusText = $"Đã áp dụng thay đổi: {pendingOrApproved.PreviousGuestCount} → {pendingOrApproved.NewGuestCount} khách.";
        }
        catch (NotSupportedException ex)
        {
            StatusText = ex.Message;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Apply change failed");
            StatusText = $"Không áp dụng được thay đổi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyMenuFromTable(DiningTableDto? targetTable)
    {
        if (SelectedOrder is null || targetTable is null || SelectedTable is null)
        {
            StatusText = "Vui lòng chọn bàn nguồn và bàn đích.";
            return;
        }
        var sourceTableId = SelectedTable.OrderTableId ?? SelectedTable.Id;
        var targetTableId = targetTable.OrderTableId ?? targetTable.Id;
        if (sourceTableId == targetTableId || sourceTableId == Guid.Empty || targetTableId == Guid.Empty)
        {
            StatusText = "Không thể copy món vào Bàn Tổng hoặc cùng một bàn.";
            return;
        }
        try
        {
            var updated = await _service.CopyMenuFromTableAsync(
                SelectedOrder.Id, sourceTableId, targetTableId, scaleByGuestCount: true);
            ApplyUpdatedOrder(updated, targetTableId);
            StatusText = $"Đã copy món từ {SelectedTable.Name} sang {targetTable.Name}.";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "CopyMenuFromTable failed");
            StatusText = $"Không copy được món: {ex.Message}";
        }
    }

    private async Task<AlacarteOrderDto> OpenOrderFromQuoteAsync(SavedQuoteDto quote)
    {
        var room = Rooms.FirstOrDefault(r => string.Equals(r.Name, quote.RoomName, StringComparison.OrdinalIgnoreCase));
        var table = Tables.FirstOrDefault(t => t.DiningRoomId == (room?.Id ?? Guid.Empty));
        try
        {
            return await _service.OpenOrderAsync(new CreateAlacarteOrderRequest
            {
                DiningTableId = table?.Id,
                Note = $"Từ báo giá {quote.QuoteNo}",
                CreatedBy = "quote-approval"
            });
        }
        catch
        {
            return new AlacarteOrderDto
            {
                Id = Guid.NewGuid(),
                OrderNo = "ALC-QUOTE-" + DateTime.Now.ToString("HHmmss"),
                DiningTableId = table?.Id,
                TableName = table?.Name,
                RoomName = room?.Name ?? quote.RoomName,
                OrderDate = DateTime.Now,
                Status = "Serving",
                PaymentStatus = "Unpaid",
                Note = $"Từ báo giá {quote.QuoteNo}",
                Lines = quote.Lines.Select((l, i) => new AlacarteOrderLineDto
                {
                    Id = Guid.NewGuid(), LineNo = i + 1,
                    MenuItemCode = l.Name.Length > 5 ? l.Name[..5] : l.Name,
                    MenuItemName = l.Name, UnitName = "suất",
                    Qty = l.Quantity, UnitPrice = l.UnitPrice,
                    Amount = l.UnitPrice * l.Quantity,
                    LineType = l.Type == "Dịch vụ" ? "Service" : "Food"
                }).ToList()
            };
        }
    }

    private async Task<AlacarteOrderDto> AddQuoteLinesToOrderAsync(AlacarteOrderDto order, SavedQuoteDto quote)
    {
        var currentOrder = order;
        var persistedLines = new List<AlacarteOrderLineDto>();
        foreach (var line in quote.Lines.Where(l => l.Type != "Dịch vụ"))
        {
            var menuItemId = line.MenuItemId ?? _allMenuItems.FirstOrDefault(x => string.Equals(x.Name, line.Name, StringComparison.OrdinalIgnoreCase))?.Id;
            if (!menuItemId.HasValue || menuItemId.Value == Guid.Empty) continue;
            try
            {
                currentOrder = await _service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
                {
                    OrderId = currentOrder.Id,
                    MenuItemId = menuItemId.Value,
                    Qty = Math.Max(1, line.Quantity),
                    KitchenNote = $"Từ báo giá {quote.QuoteNo}"
                });
                persistedLines = currentOrder.Lines.ToList();
            }
            catch
            {
                // Vẫn cho duyệt báo giá lên Đơn mở; phần service có thể thiếu mapping món cũ.
            }
        }

        return BuildApprovedQuoteOrderSnapshot(currentOrder, quote, persistedLines);
    }

    private AlacarteOrderDto BuildApprovedQuoteOrderSnapshot(AlacarteOrderDto order, SavedQuoteDto quote, IReadOnlyList<AlacarteOrderLineDto>? persistedLines = null)
    {
        var lines = persistedLines?.ToList() ?? order.Lines.ToList();
        if (lines.Count == 0)
        {
            lines.AddRange(quote.Lines.Where(l => l.Type != "Dịch vụ").Select((line, index) => new AlacarteOrderLineDto
            {
                Id = Guid.NewGuid(),
                LineNo = index + 1,
                MenuItemId = line.MenuItemId ?? Guid.Empty,
                MenuItemCode = "QUOTE",
                MenuItemName = line.Name,
                UnitName = "món",
                Qty = Math.Max(1, line.Quantity),
                UnitPrice = line.UnitPrice,
                Amount = line.Total,
                LineStatus = "Ordered",
                KitchenStatus = "New",
                LineType = "Món ăn"
            }));
        }
        var nextLineNo = lines.Count == 0 ? 1 : lines.Max(x => x.LineNo) + 1;
        foreach (var serviceLine in quote.Lines.Where(l => l.Type == "Dịch vụ"))
        {
            lines.Add(new AlacarteOrderLineDto
            {
                Id = Guid.NewGuid(),
                LineNo = nextLineNo++,
                MenuItemId = Guid.Empty,
                MenuItemCode = "SERVICE",
                MenuItemName = serviceLine.Name,
                UnitName = "dịch vụ",
                Qty = Math.Max(1, serviceLine.Quantity),
                UnitPrice = serviceLine.UnitPrice,
                Amount = serviceLine.Total,
                LineStatus = "Ordered",
                KitchenStatus = "NotRequired",
                LineType = "Dịch vụ"
            });
        }

        var subtotal = quote.Lines.Sum(x => x.Total);
        var finalTotal = Math.Max(0, quote.TotalAmount - quote.DiscountAmount);
        return new AlacarteOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            DiningTableId = order.DiningTableId,
            TableName = order.TableName,
            RoomName = order.RoomName,
            CustomerId = order.CustomerId,
            ReservationId = order.ReservationId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            PaymentStatus = quote.DepositAmount > 0 ? "Deposit" : order.PaymentStatus,
            SubtotalAmount = subtotal,
            DiscountAmount = quote.DiscountAmount,
            VatAmount = order.VatAmount,
            ServiceChargeAmount = order.ServiceChargeAmount,
            TotalAmount = finalTotal,
            PaidAmount = quote.DepositAmount,
            RemainingAmount = Math.Max(0, finalTotal - quote.DepositAmount),
            KitchenSentAt = order.KitchenSentAt,
            Note = $"Từ báo giá {quote.QuoteNo}",
            Lines = lines
        };
    }

    private async Task<string> CreateDepositCashDraftAsync(SavedQuoteDto quote)
    {
        try
        {
            if (quote.DepositAmount <= 0) return string.Empty;
            if (quote.DepositCashTransactionId.HasValue)
                return $" Phiếu thu cọc nháp: {quote.DepositCashTransactionNo}.";
            if (_cashFundService is null)
                return " Chưa tạo được phiếu thu cọc nháp do chưa có service Quỹ.";

            var funds = await _cashFundService.GetFundsAsync();
            var fund = funds.FirstOrDefault(x => x.IsDefault && x.IsActive) ?? funds.FirstOrDefault(x => x.IsActive);
            if (fund is null)
                return " Chưa tạo được phiếu thu cọc nháp do chưa có quỹ tiền mặt đang hoạt động.";

            var cash = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest
            {
                FundId = fund.Id,
                TransactionDate = DateTime.Today,
                Amount = quote.DepositAmount,
                CounterAccountCode = "131",
                PartnerType = "Customer",
                SourceType = "RestaurantQuoteDeposit",
                SourceId = quote.Id,
                Description = $"Thu cọc báo giá {quote.QuoteNo} - {quote.CustomerName}",
                CreatedBy = PreparedByName
            });
            quote.DepositCashTransactionId = cash.Id;
            quote.DepositCashTransactionNo = cash.TransactionNo;
            return $" Đã tạo phiếu thu cọc nháp {cash.TransactionNo}.";
        }
        catch (Exception ex)
        {
            return $" Chưa tạo được phiếu thu cọc nháp: {ex.Message}";
        }
    }

    private void UpsertOpenOrder(AlacarteOrderDto order)
    {
        var existing = OpenOrders.FirstOrDefault(x => x.Id == order.Id);
        if (existing is null) OpenOrders.Insert(0, order);
        else OpenOrders[OpenOrders.IndexOf(existing)] = order;
        SelectedOrder = order;
    }

    private static readonly string RestaurantSettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "alacarte_restaurant_settings.json");
    private static readonly string LegacySavedQuotesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "alacarte_quotes.json");

    private async Task LoadSavedQuotesAsync()
    {
        try
        {
            var quotes = await _service.GetQuotesAsync();
            if (quotes.Count == 0)
            {
                quotes = await ImportLegacySavedQuotesAsync();
            }

            Reset(SavedQuotes, quotes.Select(FromDto));
            RefreshSavedQuoteWorkflowStates();
        }
        catch (Exception ex)
        {
            StatusText = $"Không tải được danh sách báo giá: {ex.Message}";
        }
    }

    private async Task<IReadOnlyList<RestaurantQuoteDto>> ImportLegacySavedQuotesAsync()
    {
        if (!File.Exists(LegacySavedQuotesFilePath))
        {
            return Array.Empty<RestaurantQuoteDto>();
        }

        var json = await File.ReadAllTextAsync(LegacySavedQuotesFilePath, Encoding.UTF8);
        var legacyQuotes = JsonSerializer.Deserialize<List<SavedQuoteDto>>(json) ?? [];
        foreach (var legacyQuote in legacyQuotes.Where(x => x.Lines.Count > 0))
        {
            legacyQuote.Id = Guid.Empty;
            legacyQuote.ApprovedOrderId = null;
            legacyQuote.DepositCashTransactionId = null;
            legacyQuote.DepositCashTransactionNo = null;
            legacyQuote.Status = string.IsNullOrWhiteSpace(legacyQuote.Status) ? "Nháp" : legacyQuote.Status;
            await _service.SaveQuoteAsync(ToSaveQuoteRequest(legacyQuote, keepCurrentDetails: true));
        }

        return await _service.GetQuotesAsync();
    }

    private void SaveRestaurantSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(RestaurantSettingsFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var settings = new RestaurantInfoSettings
            {
                Name = RestaurantName,
                Address = RestaurantAddress,
                Phone = RestaurantPhone,
                Email = RestaurantEmail,
                TaxCode = RestaurantTaxCode,
                LogoPath = RestaurantLogoPath,
                KitchenPrinterName = KitchenPrinterName,
                KitchenPaperSize = KitchenPaperSize,
                BarPrinterName = BarPrinterName,
                BarPaperSize = BarPaperSize,
                ReceiptPrinterName = ReceiptPrinterName,
                ReceiptPaperSize = ReceiptPaperSize,
                DiningAreas = _manualDiningAreas
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList(),
                AllowedMenuCategoryIds = GetAllowedMenuCategoryIds().OrderBy(x => x).ToList(),
                MenuItemTypes = SettingMenuItems
                    .Where(x => x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.ItemType))
                    .Select(x => new RestaurantMenuItemTypeSetting
                    {
                        MenuItemId = x.Id,
                        ItemType = x.ItemType.Trim()
                    })
                    .ToList()
            };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RestaurantSettingsFilePath, json, Encoding.UTF8);
        }
        catch
        {
            StatusText = "Không lưu được thông tin nhà hàng.";
        }
    }

    private void LoadRestaurantSettings()
    {
        try
        {
            if (!File.Exists(RestaurantSettingsFilePath)) return;
            var json = File.ReadAllText(RestaurantSettingsFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<RestaurantInfoSettings>(json);
            if (settings is null) return;

            RestaurantName = settings.Name ?? RestaurantName;
            RestaurantAddress = settings.Address ?? RestaurantAddress;
            RestaurantPhone = settings.Phone ?? RestaurantPhone;
            RestaurantEmail = settings.Email ?? RestaurantEmail;
            RestaurantTaxCode = settings.TaxCode ?? RestaurantTaxCode;
            RestaurantLogoPath = settings.LogoPath ?? RestaurantLogoPath;
            KitchenPrinterName = string.IsNullOrWhiteSpace(settings.KitchenPrinterName) ? KitchenPrinterName : settings.KitchenPrinterName;
            KitchenPaperSize = string.IsNullOrWhiteSpace(settings.KitchenPaperSize) ? KitchenPaperSize : settings.KitchenPaperSize;
            BarPrinterName = string.IsNullOrWhiteSpace(settings.BarPrinterName) ? BarPrinterName : settings.BarPrinterName;
            BarPaperSize = string.IsNullOrWhiteSpace(settings.BarPaperSize) ? BarPaperSize : settings.BarPaperSize;
            ReceiptPrinterName = string.IsNullOrWhiteSpace(settings.ReceiptPrinterName) ? ReceiptPrinterName : settings.ReceiptPrinterName;
            ReceiptPaperSize = string.IsNullOrWhiteSpace(settings.ReceiptPaperSize) ? ReceiptPaperSize : settings.ReceiptPaperSize;
            _manualDiningAreas.Clear();
            _manualDiningAreas.AddRange(settings.DiningAreas.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            _allowedMenuCategoryIds.Clear();
            _allowedMenuCategoryIds.UnionWith(settings.AllowedMenuCategoryIds);
            _menuItemTypeOverrides.Clear();
            foreach (var itemType in settings.MenuItemTypes.Where(x => x.MenuItemId != Guid.Empty && !string.IsNullOrWhiteSpace(x.ItemType)))
            {
                _menuItemTypeOverrides[itemType.MenuItemId] = itemType.ItemType.Trim();
            }

            ApplyDefaultPrinterMappings();
            SyncSettingPrintersFromConfig();
        }
        catch { /* ignore load errors */ }
    }
}

public sealed class RestaurantInfoSettings
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
    public string? LogoPath { get; set; }
    public string? KitchenPrinterName { get; set; }
    public string? KitchenPaperSize { get; set; }
    public string? BarPrinterName { get; set; }
    public string? BarPaperSize { get; set; }
    public string? ReceiptPrinterName { get; set; }
    public string? ReceiptPaperSize { get; set; }
    public List<string> DiningAreas { get; set; } = [];
    public List<Guid> AllowedMenuCategoryIds { get; set; } = [];
    public List<RestaurantMenuItemTypeSetting> MenuItemTypes { get; set; } = [];
}

public sealed class RestaurantMenuItemTypeSetting
{
    public Guid MenuItemId { get; set; }
    public string ItemType { get; set; } = string.Empty;
}

public partial class SavedQuoteDto : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string QuoteNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public int TableStandard { get; set; } = 10;
    public string RoomName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal DepositAmount { get; set; }
    public string PaymentDeadline { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public decimal DiscountAmount { get; set; }
    public string Promotion { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 7;
    public int CancelDeadlineDays { get; set; } = 3;
    public decimal CancelFeePercent { get; set; } = 30;
    public string BankAccount { get; set; } = string.Empty;
    [ObservableProperty] private string status = "Nháp";
    public string Note { get; set; } = string.Empty;
    [ObservableProperty] private Guid? approvedOrderId;
    [ObservableProperty] private string workflowState = "Chờ duyệt";
    public Guid? DepositCashTransactionId { get; set; }
    public string? DepositCashTransactionNo { get; set; }
    public List<RestaurantQuoteTableDto> Tables { get; set; } = new();
    public List<SavedQuoteLineDto> Lines { get; set; } = new();
    public bool IsApproved => ApprovedOrderId.HasValue;
    public bool HasWorkflowTag => true;
    public bool IsReadOnly => WorkflowState == "Đã phục vụ" || WorkflowState == "Đã thanh toán";
    public bool IsPaid => WorkflowState == "Đã thanh toán";
    public bool IsServing => WorkflowState == "Đã phục vụ";
    public bool IsExpired => WorkflowState == "Quá hạn";
    public bool CanDelete => !IsApproved && DepositCashTransactionId is null;
    public string ApprovalTagText => WorkflowState;
    public string CardBackground => IsPaid ? "#ECFDF5" : IsServing ? "#EFF6FF" : IsApproved ? "#ECFDF5" : IsExpired ? "#FEF2F2" : "#FFF7ED";
    public string CardBorderBrush => IsPaid ? "#86EFAC" : IsServing ? "#93C5FD" : IsApproved ? "#86EFAC" : IsExpired ? "#FECACA" : "#FDBA74";
    public string QuoteNoBrush => IsPaid ? "#166534" : IsServing ? "#1D4ED8" : IsApproved ? "#166534" : IsExpired ? "#B91C1C" : "#9A3412";
    public string AmountBrush => IsPaid ? "#15803D" : IsServing ? "#2563EB" : IsApproved ? "#15803D" : IsExpired ? "#DC2626" : "#EA580C";
    public string TagBackground => IsPaid ? "#DCFCE7" : IsServing ? "#DBEAFE" : IsApproved ? "#DCFCE7" : IsExpired ? "#FEE2E2" : "#FFEDD5";
    public string TagBorderBrush => IsPaid ? "#BBF7D0" : IsServing ? "#BFDBFE" : IsApproved ? "#BBF7D0" : IsExpired ? "#FECACA" : "#FED7AA";
    public string TagForeground => IsPaid ? "#166534" : IsServing ? "#1D4ED8" : IsApproved ? "#166534" : IsExpired ? "#B91C1C" : "#9A3412";

    partial void OnStatusChanged(string value) => NotifyApprovalVisuals();
    partial void OnApprovedOrderIdChanged(Guid? value) => NotifyApprovalVisuals();
    partial void OnWorkflowStateChanged(string value) => NotifyApprovalVisuals();
    private void NotifyApprovalVisuals()
    {
        OnPropertyChanged(nameof(IsApproved));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsPaid));
        OnPropertyChanged(nameof(IsServing));
        OnPropertyChanged(nameof(IsExpired));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(ApprovalTagText));
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(CardBorderBrush));
        OnPropertyChanged(nameof(QuoteNoBrush));
        OnPropertyChanged(nameof(AmountBrush));
        OnPropertyChanged(nameof(TagBackground));
        OnPropertyChanged(nameof(TagBorderBrush));
        OnPropertyChanged(nameof(TagForeground));
    }
    }

public class SavedQuoteLineDto
{
    public Guid? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PricingRule { get; set; } = "Manual";
    public decimal CalculatedQuantity { get; set; }
    public decimal EffectiveQuantity => CalculatedQuantity > 0 ? CalculatedQuantity : Quantity;
    public string EffectiveQuantityDisplay => EffectiveQuantity.ToString("N0");
    public decimal Total { get; set; }
    public string Type { get; set; } = string.Empty;
}

// Local helper classes for À La Carte
public sealed partial class AlacarteServiceItem : ObservableObject
{
    public AlacarteServiceItem(Guid id, string name, decimal price, string unit, string code = "", decimal vatRate = 0)
    {
        Id = id; Code = code; Name = name; Price = price; Unit = unit; VatRate = vatRate;
    }

    public AlacarteServiceItem(string name, decimal price, string unit)
    {
        Name = name; Price = price; Unit = unit;
    }
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; }
    public decimal VatRate { get; set; }
    public string ServiceType { get; set; } = "Khác";
    public bool IsVatIncluded { get; set; } = true;
    public bool IsActive { get; set; } = true;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private int quantity = 1;
}

public sealed partial class AlacarteCategorySaleSelection : ObservableObject
{
    public AlacarteCategorySaleSelection(Guid categoryId, string code, string name, int itemCount, bool isEnabled)
    {
        CategoryId = categoryId;
        Code = code;
        Name = name;
        ItemCount = itemCount;
        IsEnabled = isEnabled;
    }

    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    [ObservableProperty] private bool isEnabled;
}

public sealed partial class AlacarteAlertItem : ObservableObject
{
    public AlacarteAlertItem(string message, string time, string type)
    {
        Message = message; Time = time; Type = type;
    }
    public string Message { get; set; }
    public string Time { get; set; }
    public string Type { get; set; }
}

public sealed partial class AlacarteSettingDiningArea : ObservableObject
{
    public AlacarteSettingDiningArea(string name, int roomCount, int tableCount)
    {
        Name = name;
        RoomCount = roomCount;
        TableCount = tableCount;
    }

    public string Name { get; set; }
    public int RoomCount { get; set; }
    public int TableCount { get; set; }
}

// Settings helper classes
public sealed partial class AlacarteSettingArea : ObservableObject
{
    public AlacarteSettingArea(Guid id, string code, string name, string type, int capacity, bool isActive)
    { Id = id; Code = code; Name = name; Type = type; Capacity = capacity; IsActive = isActive; }
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int Capacity { get; set; }
    [ObservableProperty] private bool isActive;
    [ObservableProperty] private bool isLocked;
}

public sealed partial class AlacarteSettingTable : ObservableObject
{
    public AlacarteSettingTable(Guid id, Guid diningRoomId, string code, string name, int seats, string room, string status, string areaName)
    { Id = id; DiningRoomId = diningRoomId; Code = code; Name = name; Seats = seats; RoomName = room; Status = status; AreaName = areaName; }
    public Guid Id { get; set; }
    public Guid DiningRoomId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int Seats { get; set; }
    public string AreaName { get; set; }
    public string RoomName { get; set; }
    public string Status { get; set; }
    [ObservableProperty] private bool isLocked;
}

public sealed partial class AlacarteSettingPrinter : ObservableObject
{
    public AlacarteSettingPrinter(string name, string model, string port, bool online)
    { Name = name; Model = model; Port = port; IsOnline = online; }
    public string Name { get; set; }
    public string Model { get; set; }
    public string Port { get; set; }
    [ObservableProperty] private bool isOnline;
}

public sealed partial class AlacarteSettingMenuGroup : ObservableObject
{
    public AlacarteSettingMenuGroup(Guid id, string code, string name, int sortOrder, int itemCount, bool isAllowed)
    {
        Id = id;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
        ItemCount = itemCount;
        IsAllowed = isAllowed;
    }

    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public int ItemCount { get; set; }
    [ObservableProperty] private bool isAllowed;
}

public sealed partial class AlacarteSettingMenuItem : ObservableObject
{
    public AlacarteSettingMenuItem(Guid id, Guid categoryId, string code, string name, string group, decimal price, int vat, string station, bool hasRecipe, bool isActive, string itemType)
    {
        Id = id;
        CategoryId = categoryId;
        Code = code;
        Name = name;
        GroupName = group;
        Price = price;
        VatRate = vat;
        Station = station;
        HasRecipe = hasRecipe;
        IsActive = isActive;
        ItemType = itemType;
    }

    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; }
    public string GroupName { get; set; }
    public decimal Price { get; set; }
    public int VatRate { get; set; }
    public string Station { get; set; }
    public bool HasRecipe { get; set; }
    public string ItemType { get; set; } = "Đồ ăn";
    [ObservableProperty] private bool isActive;
}

public sealed partial class AlacarteSettingPaymentMethod : ObservableObject
{
    public AlacarteSettingPaymentMethod(string name, string code, bool enabled)
    { Name = name; Code = code; IsEnabled = enabled; }
    public string Name { get; set; }
    public string Code { get; set; }
    [ObservableProperty] private bool isEnabled;
}

public sealed partial class AlacarteSettingPermission : ObservableObject
{
    public AlacarteSettingPermission(string name, bool granted)
    { Name = name; IsGranted = granted; }
    public string Name { get; set; }
    [ObservableProperty] private bool isGranted;
}

public sealed partial class AlacarteSettingIntegration : ObservableObject
{
    public AlacarteSettingIntegration(string name, bool ready)
    { Name = name; IsReady = ready; }
    public string Name { get; set; }
    [ObservableProperty] private bool isReady;
}

// Food menu item for Services tab
public sealed partial class AlacarteMenuItemCard : ObservableObject
{
    public AlacarteMenuItemCard(string name, string category, decimal price, string emoji)
    { Name = name; CategoryName = category; Price = price; Emoji = emoji; }
    public string Name { get; set; }
    public string CategoryName { get; set; }
    public decimal Price { get; set; }
    public string Emoji { get; set; }
    [ObservableProperty] private bool isSelected;
}

// Quote line item for Báo giá
public sealed partial class QuoteLineItem : ObservableObject
{
    public Guid? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    [ObservableProperty] private int quantity = 1;
    [ObservableProperty] private string pricingRule = "Manual";
    [ObservableProperty] private decimal calculatedQuantity;
    public string Type { get; set; } = string.Empty;
    public int LineNo { get; set; } = 1;
    public decimal EffectiveQuantity => CalculatedQuantity > 0 ? CalculatedQuantity : Quantity;
    public decimal Total => UnitPrice * EffectiveQuantity;
    [ObservableProperty] private string totalDisplay = "0";
    [ObservableProperty] private string lineNoDisplay = "1";
    public string CalculatedQuantityDisplay => EffectiveQuantity.ToString("N0");

    public void NotifyTotal() { TotalDisplay = Total.ToString("N0"); OnPropertyChanged(nameof(EffectiveQuantity)); OnPropertyChanged(nameof(CalculatedQuantityDisplay)); OnPropertyChanged(nameof(Total)); }
    public void NotifyLineNo() { LineNoDisplay = LineNo.ToString(); OnPropertyChanged(nameof(LineNo)); }
    partial void OnQuantityChanged(int value) { NotifyTotal(); }
    partial void OnCalculatedQuantityChanged(decimal value) { NotifyTotal(); }
}

public sealed class KeyValueOption
{
    public KeyValueOption(string value, string label)
    {
        Value = value;
        Label = label;
    }

    public string Value { get; }
    public string Label { get; }
}

public sealed class OrderItemPickerPlanRow
{
    public Guid TableId { get; set; }
    public Guid? OrderTableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public decimal SaveQuantity { get; set; }
    public decimal DisplayQuantity { get; set; }
    public decimal ExtraQuantity { get; set; }
    public string DisplayRuleText { get; set; } = string.Empty;
    public string? KitchenNote { get; set; }
    public string ExistingItemsDisplay { get; set; } = "Chưa có món";
}

public sealed class ServingGroupSummaryRow
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string AccentKey { get; set; } = "Blue";
}

public sealed class OrderLineEditDialogResult
{
    public decimal Quantity { get; set; }
    public decimal SupplementQuantity { get; set; }
    public string? KitchenNote { get; set; }
    public string? SupplementNote { get; set; }
    public string? SupplementNoteRaw { get; set; }
    public string? AdjustmentReason { get; set; }
}

public sealed class SummaryOrderLineEditDialogResult
{
    public decimal Quantity { get; set; }
    public string? AdjustmentReason { get; set; }
}

public sealed class OrderLineIngredientOption
{
    public int? InventoryItemId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; } = 1m;
}

public sealed class OrderLineIngredientEditorRuntime
{
    public Guid RecipeLineId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public System.Windows.Controls.ComboBox Selector { get; set; } = null!;
    public System.Windows.Controls.TextBox ExtraQuantityBox { get; set; } = null!;
    public System.Windows.Controls.TextBox NoteBox { get; set; } = null!;
}

public sealed class RestaurantLineSupplementMetadata
{
    public string? DisplayNote { get; set; }
    public List<RestaurantLineIngredientAdjustmentDto> IngredientAdjustments { get; set; } = [];
}

public sealed partial class ServingGroupEditorRow : ObservableObject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Custom";
    public string ServingRule { get; set; } = "PerTable";
    public string? Note { get; set; }
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public bool IsSystemDefined { get; set; }
    public string TableSummary { get; set; } = string.Empty;
    public string DisplaySubtitle => $"{TableCount} bàn · {GuestCount} khách";
    public string AccentKey => IsSystemDefined ? "Blue" : "Green";
}

public sealed partial class ServingGroupTableSelectionRow : ObservableObject
{
    public Guid OrderTableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsServing { get; set; }
    public bool CanSelect { get; set; }
    [ObservableProperty] private bool isSelected;
    public string StatusHint => $"{GuestCount} khách · {Status}";
}

public sealed class PreparationQueueRow
{
    public string MenuItemName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string KitchenStatus { get; set; } = string.Empty;
    public string? KitchenNote { get; set; }
    public string AccentKey { get; set; } = "Orange";
}

public sealed partial class RoomServingPlanRow : ObservableObject
{
    public Guid TableId { get; set; }
    public Guid OrderTableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int StandardSeats { get; set; }
    public int SoftLimitSeats { get; set; }
    [ObservableProperty] private bool isServing;
    [ObservableProperty] private int guestCount;
    public bool IsOverStandard => IsServing && GuestCount > StandardSeats;
    public bool IsOverSoftLimit => IsServing && GuestCount > SoftLimitSeats;
    public int ExtraGuests => IsServing ? Math.Max(0, GuestCount - StandardSeats) : 0;
    public string StatusLabel => IsServing ? "Đang phục vụ" : "Bàn chờ";
    public string CapacityHint => $"Chuẩn {StandardSeats} · Mềm {SoftLimitSeats}";

    partial void OnIsServingChanged(bool value)
    {
        if (!value && GuestCount != 0)
        {
            GuestCount = 0;
        }

        if (value && GuestCount <= 0)
        {
            GuestCount = Math.Max(1, StandardSeats);
        }

        OnPropertyChanged(nameof(IsOverStandard));
        OnPropertyChanged(nameof(IsOverSoftLimit));
        OnPropertyChanged(nameof(ExtraGuests));
        OnPropertyChanged(nameof(StatusLabel));
    }

    partial void OnGuestCountChanged(int value)
    {
        if (!IsServing && value != 0)
        {
            GuestCount = 0;
            return;
        }

        OnPropertyChanged(nameof(IsOverStandard));
        OnPropertyChanged(nameof(IsOverSoftLimit));
        OnPropertyChanged(nameof(ExtraGuests));
    }
}


public sealed partial class BanquetRestaurantViewModel : ViewModelBase
{
    private readonly IBanquetRestaurantService _service;
    private readonly IMenuCatalogService _menuCatalog;
    private readonly IAlacarteRestaurantService _alacarteService;
    private readonly IWindowManagerService _windowManager;
    private readonly IMenuRecipeService _menuRecipeService;
    private readonly INotificationTaskService _notificationTaskService;
    private readonly IMenuCalculationEngine _menuCalculationEngine;
    private readonly SemaphoreSlim _busyOperationGate = new(1, 1);

    public ObservableCollection<BanquetRoomDto> Rooms { get; } = new();
    public ObservableCollection<BanquetEventDto> Events { get; } = new();
    public ObservableCollection<BanquetMenuPackageDto> MenuPackages { get; } = new();
    public ObservableCollection<BanquetChecklistDto> ChecklistItems { get; } = new();

    [ObservableProperty] private BanquetDashboardDto dashboard = new();
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private int selectedDetailTabIndex;
    [ObservableProperty] private string statusText = "Sẵn sàng";
    [ObservableProperty] private BanquetRoomDto? selectedRoom;
    [ObservableProperty] private BanquetEventDto? selectedEvent;
    [ObservableProperty] private string roomCode = string.Empty;
    [ObservableProperty] private string roomName = string.Empty;
    [ObservableProperty] private int roomCapacity = 300;
    [ObservableProperty] private int roomMaxTables = 30;
    [ObservableProperty] private string eventType = "Wedding";
    [ObservableProperty] private string eventCustomerName = string.Empty;
    [ObservableProperty] private string eventPhone = string.Empty;
    [ObservableProperty] private DateTime eventDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string startTimeText = "17:00";
    [ObservableProperty] private string endTimeText = "21:00";
    [ObservableProperty] private int guestCount = 100;
    [ObservableProperty] private int tableCount = 10;
    [ObservableProperty] private decimal depositAmount;
    [ObservableProperty] private double sidebarWidth = 88;
    [ObservableProperty] private double sidebarButtonWidth = 72;
    [ObservableProperty] private string sidebarToggleText = "<";
    [ObservableProperty] private bool isSidebarCompact;

    public bool IsDashboardTab => SelectedTabIndex == 0;
    public bool IsCalendarTab => SelectedTabIndex == 1;
    public bool IsCustomersTab => SelectedTabIndex == 2;
    public bool IsEventsTab => SelectedTabIndex == 3;
    public bool IsContractsTab => SelectedTabIndex == 4;
    public bool IsFinanceTab => SelectedTabIndex == 5;
    public bool IsReportsTab => SelectedTabIndex == 6;
    public bool IsSettingsTab => SelectedTabIndex == 7;
    public bool IsQuantificationTab => SelectedTabIndex == 8;
    public bool ShowOverviewWorkspace => IsDashboardTab;
    public bool ShowCalendarWorkspace => IsCalendarTab;
    public bool ShowCustomersWorkspace => IsCustomersTab;
    public bool ShowEventWorkspace => IsEventsTab;
    public bool ShowContractsWorkspace => IsContractsTab;
    public bool ShowFinanceWorkspace => IsFinanceTab;
    public bool ShowReportsWorkspace => IsReportsTab;
    public bool ShowSettingsWorkspace => IsSettingsTab;
    public bool ShowQuantificationWorkspace => IsQuantificationTab;

    public BanquetRestaurantViewModel(
        IBanquetRestaurantService service,
        IMenuCatalogService menuCatalog,
        IAlacarteRestaurantService alacarteService,
        IWindowManagerService windowManager,
        IMenuRecipeService menuRecipeService,
        INotificationTaskService notificationTaskService,
        IMenuCalculationEngine menuCalculationEngine)
    {
        _service = service;
        _menuCatalog = menuCatalog;
        _alacarteService = alacarteService;
        _windowManager = windowManager;
        _menuRecipeService = menuRecipeService;
        _notificationTaskService = notificationTaskService;
        _menuCalculationEngine = menuCalculationEngine;
        LoadBanquetSettings();
        _ = RefreshAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTab));
        OnPropertyChanged(nameof(IsCalendarTab));
        OnPropertyChanged(nameof(IsCustomersTab));
        OnPropertyChanged(nameof(IsEventsTab));
        OnPropertyChanged(nameof(IsContractsTab));
        OnPropertyChanged(nameof(IsFinanceTab));
        OnPropertyChanged(nameof(IsReportsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsQuantificationTab));
        OnPropertyChanged(nameof(ShowOverviewWorkspace));
        OnPropertyChanged(nameof(ShowCalendarWorkspace));
        OnPropertyChanged(nameof(ShowCustomersWorkspace));
        OnPropertyChanged(nameof(ShowEventWorkspace));
        OnPropertyChanged(nameof(ShowContractsWorkspace));
        OnPropertyChanged(nameof(ShowFinanceWorkspace));
        OnPropertyChanged(nameof(ShowReportsWorkspace));
        OnPropertyChanged(nameof(ShowSettingsWorkspace));
        OnPropertyChanged(nameof(ShowQuantificationWorkspace));
        OnPropertyChanged(nameof(CurrentWorkspaceTitle));
        OnPropertyChanged(nameof(CurrentWorkspaceSubtitle));

        switch (value)
        {
            case 1:
                SelectedDetailTabIndex = 1;
                break;
            case 6:
                RefreshReportCollections();
                SelectedDetailTabIndex = 0;
                break;
            case 8:
                _ = RunBusyAsync(RefreshQuantificationAsync);
                SelectedDetailTabIndex = 0;
                break;
            case 3:
                SelectedDetailTabIndex = SelectedDetailTabIndex < 0 ? 0 : SelectedDetailTabIndex;
                break;
            default:
                SelectedDetailTabIndex = 0;
                break;
        }

        if (value != 8)
        {
            _ = RunBusyAsync(EnsureSelectedEventDetailLoadedAsync);
        }
    }

    partial void OnSelectedRoomChanged(BanquetRoomDto? value)
    {
        if (value is null) return;
        RoomCode = value.Code;
        RoomName = value.Name;
        RoomCapacity = value.Capacity;
        RoomMaxTables = value.MaxTables;
        RoomDescription = value.Description ?? string.Empty;
        RoomStatus = value.Status;
        RoomIsActive = value.IsActive;
        LoadRoomLayout(value.LayoutJson);
    }

    partial void OnSelectedEventChanged(BanquetEventDto? value)
    {
        ResetChecklistEditor();
        ResetTimelineEditor();
        if (value is null)
        {
            ResetEventEditor();
            ResetDetailCollections();
            return;
        }

        ApplySelectedEventToEditor(value);
        NotifyFinanceSummaryChanged();

        if (_suppressSelectedEventChanged)
        {
            return;
        }

        _ = RunBusyAsync(() => LoadSelectedEventDetailAsync(value.Id));
    }

    [RelayCommand]
    private void NavigateTab(string? tabIndex)
    {
        if (!int.TryParse(tabIndex, out var index) || index == SelectedTabIndex)
        {
            return;
        }

        if (IsDirty)
        {
            var result = MessageBox.Show(
                "Sự kiện đang có thay đổi chưa lưu. Bạn có muốn rời màn hình và bỏ các thay đổi này?",
                "Thay đổi chưa lưu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            UndoEventEdit();
        }

        SelectedTabIndex = index;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCompact = !IsSidebarCompact;
        SidebarWidth = IsSidebarCompact ? 52 : 88;
        SidebarButtonWidth = IsSidebarCompact ? 40 : 72;
        SidebarToggleText = IsSidebarCompact ? ">" : "<";
    }
    [RelayCommand] private void SelectRoom(BanquetRoomDto? room) { if (room is not null) SelectedRoom = room; }
    [RelayCommand] private void SelectEvent(BanquetEventDto? evt) { if (evt is not null) { SelectedEvent = evt; _isCreatingEvent = false; OnPropertyChanged(nameof(IsCreatingEvent)); OnPropertyChanged(nameof(ShowEventEditor)); } }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            await LoadBanquetSettingsFromDatabaseAsync();
            await RefreshPreservingSelectionAsync();
            StatusText = "Đã tải dữ liệu tiệc.";
        });
    }

    [RelayCommand]
    private async Task SaveRoomAsync()
    {
        await RunBusyAsync(async () =>
        {
            var room = await _service.SaveRoomAsync(new SaveBanquetRoomRequest
            {
                Id = SelectedRoom?.Id, Code = RoomCode, Name = RoomName,
                Capacity = RoomCapacity, MaxTables = RoomMaxTables,
                Description = RoomDescription, LayoutJson = SerializeRoomLayout(),
                Status = RoomStatus, IsActive = RoomIsActive
            });
            await RefreshPreservingSelectionAsync(SelectedEvent?.Id, room.Id);
            RoomLayoutIsDirty = false;
            StatusText = $"Đã lưu phòng tiệc {room.Name}.";
        });
    }

    [RelayCommand] private void NewRoom() { SelectedRoom = null; RoomCode = string.Empty; RoomName = string.Empty; RoomCapacity = 300; RoomMaxTables = 30; RoomDescription = string.Empty; RoomStatus = "Available"; RoomIsActive = true; RoomLayoutItems.Clear(); RoomWorkspaceMode = 0; }

    [RelayCommand]
    private async Task DeleteRoomAsync()
    {
        if (SelectedRoom is null) return;
        var name = SelectedRoom.Name;
        await RunBusyAsync(async () =>
        {
            await _service.DeleteRoomAsync(SelectedRoom.Id);
            SelectedRoom = null;
            await RefreshPreservingSelectionAsync(SelectedEvent?.Id, null);
            NewRoom();
            StatusText = $"Đã xóa hoặc ngừng hoạt động phòng {name}.";
        });
    }

    [RelayCommand]
    private async Task SaveEventAsync()
    {
        if (_isSavingEvent) return;
        if (!ValidateEventEditor())
        {
            StatusText = ValidationMessage;
            return;
        }

        var roomId = SelectedEventRoomId ?? SelectedRoom?.Id ?? Rooms.FirstOrDefault()?.Id;
        if (roomId is null) { StatusText = "Cần tạo/chọn phòng tiệc trước."; return; }
        if (!TimeSpan.TryParse(StartTimeText, out var start) || !TimeSpan.TryParse(EndTimeText, out var end))
        { StatusText = "Giờ tiệc không hợp lệ."; return; }
        _isSavingEvent = true;
        OnPropertyChanged(nameof(CanSaveEvent));
        try
        {
            await RunBusyAsync(async () =>
            {
                var evt = await _service.SaveEventAsync(new SaveBanquetEventRequest
                {
                    Id = SelectedEvent?.Id, RowVersion = SelectedEvent?.RowVersion, EventType = EventType, CustomerName = EventCustomerName,
                    Phone = EventPhone, EventDate = EventDate, StartTime = start, EndTime = end,
                    GuestCount = GuestCount, TableCount = TableCount,
                    ExtraGuestCount = ExtraGuestCount, ExtraTableCount = ExtraTableCount,
                    BrideName = BrideName, GroomName = GroomName, ConferenceName = ConferenceName,
                    BirthdayPersonName = BirthdayPersonName, BirthdayDate = BirthdayDate,
                    MeetingTopic = MeetingTopic, PartyTheme = PartyTheme, OtherEventName = OtherEventName,
                    BanquetRoomId = roomId, Status = EventStatus, Note = EventNote
                });
                await RefreshPreservingSelectionAsync(evt.Id, roomId);
                IsDirty = false;
                StatusText = $"Đã lưu sự kiện {evt.EventNo}.";
            });
        }
        finally
        {
            _isSavingEvent = false;
            OnPropertyChanged(nameof(CanSaveEvent));
        }
    }

    [RelayCommand]
    private async Task AddDepositAsync()
    {
        ValidateDepositForm();
        if (!CanAddDeposit || SelectedEvent is null) return;
        _isAddingDeposit = true;
        DepositSuccessMessage = string.Empty;
        OnPropertyChanged(nameof(CanAddDeposit));
        try
        {
            await RunBusyAsync(async () =>
            {
                await _service.AddDepositAsync(new AddBanquetDepositRequest
                {
                    BanquetEventId = SelectedEvent.Id,
                    Amount = DepositAmount,
                    PaymentMethod = DepositPaymentMethod,
                    Receiver = DepositReceiver,
                    Note = DepositNote,
                    IdempotencyKey = $"ui:{SelectedEvent.Id:N}:{Guid.NewGuid():N}"
                });
                DepositAmount = 0m;
                DepositNote = string.Empty;
                await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
                DepositSuccessMessage = "Đã ghi nhận khoản đặt cọc thành công.";
                StatusText = "Đã ghi nhận đặt cọc tiệc.";
            });
        }
        finally
        {
            _isAddingDeposit = false;
            NotifyFinanceSummaryChanged();
        }
    }

    [RelayCommand]
    private async Task RefundDepositAsync(BanquetDepositDto? deposit)
    {
        if (deposit is null || !deposit.CanRefund) return;
        var result = MessageBox.Show(
            $"Hoàn lại toàn bộ số tiền còn lại {deposit.NetAmount:N0} đ của phiếu {deposit.DepositNo}?\n\nHệ thống sẽ tạo phiếu chi nếu khoản cọc được thu bằng tiền mặt.",
            "Xác nhận hoàn cọc",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        await RunBusyAsync(async () =>
        {
            await _service.RefundDepositAsync(new RefundBanquetDepositRequest
            {
                DepositId = deposit.Id,
                Amount = deposit.NetAmount,
                Reason = "Hoàn cọc theo yêu cầu khách hàng",
                RefundedBy = string.IsNullOrWhiteSpace(DepositReceiver) ? null : DepositReceiver.Trim(),
                RowVersion = deposit.RowVersion
            });
            await RefreshPreservingSelectionAsync(deposit.BanquetEventId, SelectedRoom?.Id);
            StatusText = $"Đã hoàn cọc phiếu {deposit.DepositNo}.";
        });
    }

    [RelayCommand]
    private async Task CreateContractAsync()
    {
        if (SelectedEvent is null) { StatusText = "Chọn sự kiện trước khi tạo hợp đồng."; return; }
        await RunBusyAsync(async () =>
        {
            var contract = await _service.CreateContractAsync(SelectedEvent.Id, ContractSignDate);
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            SelectedContract = EventContracts.FirstOrDefault(x => x.Id == contract.Id) ?? EventContracts.FirstOrDefault();
            StatusText = $"Đã tạo hợp đồng {contract.ContractNo}.";
        });
    }

    [RelayCommand]
    private void ViewContract(BanquetContractDto? contract)
    {
        SelectedContract = contract ?? SelectedContract ?? EventContracts.FirstOrDefault();
        if (SelectedContract is null)
        {
            StatusText = "Chưa có hợp đồng để xem.";
            return;
        }

        ContractPreviewText = BuildContractDocument(SelectedContract);
        StatusText = $"Đang xem hợp đồng {SelectedContract.ContractNo}.";
        ShowContractPreview(SelectedContract);
    }

    [RelayCommand]
    private async Task SaveContractDetailsAsync()
    {
        if (SelectedContract is null)
        {
            StatusText = "Chọn hợp đồng cần chỉnh sửa.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var saved = await _service.UpdateContractAsync(new UpdateBanquetContractRequest
            {
                ContractId = SelectedContract.Id,
                SignDate = ContractSignDate,
                Status = ContractStatus,
                Note = ContractAdditionalTerms
            });
            var index = EventContracts.IndexOf(SelectedContract);
            if (index >= 0) EventContracts[index] = saved;
            SelectedContract = saved;
            ContractPreviewText = BuildContractDocument(saved);
            StatusText = $"Đã lưu nội dung bổ sung hợp đồng {saved.ContractNo}.";
        });
    }

    [RelayCommand]
    private void PrintContract()
    {
        var contract = SelectedContract ?? EventContracts.FirstOrDefault();
        if (contract is null)
        {
            StatusText = "Chưa có hợp đồng để in.";
            return;
        }

        SelectedContract = contract;
        ContractPreviewText = BuildContractDocument(contract);
        ShowContractPreview(contract);
    }

    private void ShowContractPreview(BanquetContractDto contract)
    {
        if (SelectedEvent is null) return;
        var preview = new BanquetContractPreviewDialog(contract, SelectedEvent, EventMenus, EventServices)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        preview.ShowDialog();
    }

    [RelayCommand]
    private async Task ToggleChecklistAsync(BanquetChecklistDto? item)
    {
        if (item is null) return;
        await RunBusyAsync(async () =>
        {
            await _service.ToggleChecklistAsync(item.Id, !item.IsDone, "operator");
            if (SelectedEvent is not null)
            {
                await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            }
        });
    }

    private static void Reset<T>(ObservableCollection<T> target, IEnumerable<T> source) { target.Clear(); foreach (var item in source) target.Add(item); }

    private async Task RunBusyAsync(Func<Task> action)
    {
        await _busyOperationGate.WaitAsync();
        try
        {
            IsBusy = true;
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Thao tác đã được hủy.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Banquet operation failed: {ex}");
            StatusText = string.IsNullOrWhiteSpace(ex.Message)
                ? "Thao tác không thành công. Vui lòng thử lại."
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
            _busyOperationGate.Release();
        }
    }
}

public sealed class AlacarteRoomSetupDraft
{
    public Guid OrderId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int TableCount { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int MaxTables { get; set; }
    public string Note { get; set; } = string.Empty;
}

public sealed class CalendarDay
{
    public int Day { get; set; }
    public DateTime Date { get; set; }
    public bool HasReservation { get; set; }
    public bool IsPadding { get; set; }
    public bool IsToday { get; set; }
    public bool IsSelected { get; set; }
}

public sealed partial class OrderItemPickerTableSelection : ObservableObject
{
    public Guid OrderTableId { get; set; }
    public string Name { get; set; } = string.Empty;
    [ObservableProperty] private bool isSelected = true;
}
