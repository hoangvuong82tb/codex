using System.Collections.ObjectModel;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class MinibarRoomDto
{
    // Identifiers are kept separate from display values so commands can use the
    // persisted aggregate without parsing room/folio numbers from the UI.
    public Guid? PersistentSessionId { get; set; }
    public Guid? PersistentStayId { get; set; }
    public Guid PersistentRoomId { get; set; }
    public Guid? PersistentFolioEntryId { get; set; }
    public decimal RestockQuantity { get; set; }
    public DateTime? LastInventoryAtUtc { get; set; }
    public int InventoryLineCount { get; set; }
    public string InventoryDisplay => LastInventoryAtUtc.HasValue
        ? $"{LastInventoryAtUtc.Value.ToLocalTime():dd/MM HH:mm} ({InventoryLineCount} dòng)"
        : "Chưa kiểm kê";
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "STD";
    public string RoomTypeLabel => RoomType switch
    {
        "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe",
        "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential", _ => RoomType
    };
    public string Floor { get; set; } = "1";
    public string Building { get; set; } = "A";

    public string? GuestName { get; set; }
    public string? ReservationNumber { get; set; }
    public string? FolioNumber { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public DateTime? DepartureDate { get; set; }
    public bool IsVip { get; set; }
    public string VipLevel => IsVip ? "VIP" : "—";
    public string VipColor => IsVip ? "#D97706" : "#94A3B8";

    public string? Housekeeper { get; set; }
    public string? Supervisor { get; set; }
    public DateTime? InspectionTime { get; set; }
    public string Status { get; set; } = "Unchecked";
    public string StatusLabel => Status switch
    {
        "Unchecked" => "Chưa kiểm tra", "Checked" => "Đã kiểm tra",
        "Consumption" => "Có phát sinh", "NoConsumption" => "Không phát sinh",
        "Posted" => "Đã Post", "Unposted" => "Chưa Post", _ => Status
    };
    public string StatusColor => Status switch
    {
        "Unchecked" => "#94A3B8", "Checked" => "#2563EB", "Consumption" => "#D97706",
        "NoConsumption" => "#16A34A", "Posted" => "#16A34A", "Unposted" => "#DC2626", _ => "#64748B"
    };
    public string StatusBg => Status switch
    {
        "Unchecked" => "#F1F5F9", "Checked" => "#DBEAFE", "Consumption" => "#FFF7ED",
        "NoConsumption" => "#DCFCE7", "Posted" => "#DCFCE7", "Unposted" => "#FEE2E2", _ => "#F8FAFC"
    };

    public int ItemCount { get; set; }
    public decimal ConsumptionAmount { get; set; }
    public string ConsumptionDisplay => ConsumptionAmount > 0 ? $"{ConsumptionAmount:N0} ₫" : "—";
    public decimal VatAmount { get; set; }
    public string VatDisplay => VatAmount > 0 ? $"{VatAmount:N0} ₫" : "—";
    public decimal GrossAmount => ConsumptionAmount + VatAmount;
    public string GrossDisplay => GrossAmount > 0 ? $"{GrossAmount:N0} ₫" : "—";
    public decimal RestockAmount { get; set; }
    public string RestockDisplay => RestockAmount > 0 ? $"{RestockAmount:N0} ₫" : "—";
    public bool IsPosted { get; set; }
    public string PostedLabel => IsPosted ? "Đã Post" : "Chưa Post";
    public string? PostTarget { get; set; }

    public bool HasConsumption => ConsumptionAmount > 0;
    public bool IsRush => !IsPosted && HasConsumption && InspectionTime.HasValue && (DateTime.Now - InspectionTime.Value).TotalHours > 4;

    public List<MinibarLineItemDto> Items { get; set; } = new();
    public List<MinibarInventoryLineInputDto> InventoryItems { get; set; } = new();
    public List<MinibarTimelineEntry> Timeline { get; set; } = new();
    public List<MinibarAuditEntry> AuditLog { get; set; } = new();
    public string? Remark { get; set; }
}

public sealed class MinibarLineItemDto
{
    public string ItemCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "Lon";
    public int StandardQty { get; set; }
    public int RemainingQty { get; set; }
    public int Consumed => StandardQty - RemainingQty;
    public int RestockQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal => Consumed * UnitPrice;
    public string LineTotalDisplay => LineTotal > 0 ? $"{LineTotal:N0} ₫" : "—";
    public string VatDisplay => VatAmount > 0 ? $"VAT {VatRate:N0}%: {VatAmount:N0} ₫" : "Không VAT";
    public decimal LineGross => LineTotal + VatAmount;
    public string LineGrossDisplay => LineGross > 0 ? $"{LineGross:N0} ₫" : "—";
    public string Category { get; set; } = "Đồ uống";
}

public sealed class MinibarCatalogOption
{
    public string ItemCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string DisplayName => $"{ItemCode} — {ItemName} ({UnitPrice:N0} ₫)";
}

public sealed class MinibarWarehouseOption
{
    public int Id { get; init; }
    public string WarehouseCode { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public bool IsConsumptionWarehouse { get; init; }
    public string DisplayName => string.IsNullOrWhiteSpace(WarehouseName)
        ? WarehouseCode
        : $"{WarehouseCode} — {WarehouseName}";

    public static MinibarWarehouseOption From(WarehouseDto dto) => new()
    {
        Id = dto.Id,
        WarehouseCode = dto.WarehouseCode,
        WarehouseName = dto.WarehouseName,
        IsConsumptionWarehouse = dto.IsConsumptionWarehouse
    };
}

public sealed partial class MinibarInventoryLineInputDto : ObservableObject
{
    [ObservableProperty] private string _itemCode = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitPrice;
}

public sealed class MinibarKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class MinibarTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class MinibarTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class MinibarAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class MinibarPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public MinibarPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis;
    }
}

// ═══ FORM STATE ═══

public sealed class MinibarFormField
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

// ═══ VIEW MODEL ═══

public sealed partial class MinibarViewModel : ViewModelBase
{
    private readonly IMinibarQueryService _minibarQueryService;
    private readonly IFrontOfficeExecutionContextAccessor _executionContext;
    private readonly IMinibarCommandService _minibarCommandService;
    private readonly IWarehouseAppService? _warehouseAppService;

    public MinibarViewModel(
        IMinibarQueryService minibarQueryService,
        IFrontOfficeExecutionContextAccessor executionContext,
        IMinibarCommandService minibarCommandService,
        IWarehouseAppService? warehouseAppService = null)
    {
        _minibarQueryService = minibarQueryService;
        _executionContext = executionContext;
        _minibarCommandService = minibarCommandService;
        _warehouseAppService = warehouseAppService;
    }

    public ObservableCollection<MinibarRoomDto> Rooms { get; } = new();
    public ObservableCollection<MinibarRoomDto> FilteredRooms { get; } = new();
    public ObservableCollection<MinibarKpi> DashboardKpis { get; } = new();
    public ObservableCollection<MinibarTimelineEntry> RoomTimeline { get; } = new();
    public ObservableCollection<MinibarAuditEntry> RoomAuditLog { get; } = new();
    public ObservableCollection<MinibarInventoryLineInputDto> InventoryLines { get; } = new();
    public ObservableCollection<MinibarCatalogOption> RestockItemOptions { get; } = new();
    public ObservableCollection<MinibarWarehouseOption> WarehouseOptions { get; } = new();
    public ObservableCollection<MinibarTreeItem> TreeItems { get; } = new();
    public ObservableCollection<MinibarPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private MinibarRoomDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private MinibarTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterHousekeeper;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterPostedOnly;
    [ObservableProperty] private bool _filterUnpostedOnly;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 phòng" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} phòng";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedRoom is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";
    private Guid CurrentPropertyId => _executionContext?.Current?.Property.PropertyId.Value ?? Guid.Empty;
    private Guid? CurrentActorId => _executionContext?.Current?.Actor.ActorId?.Value;

    // ═══ FORM STATE ═══
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    public bool IsInspectForm => FormMode == "Inspect";
    public bool IsRestockForm => FormMode == "Restock";
    public bool IsPostForm => FormMode == "Post";
    public bool IsInventoryForm => FormMode == "Inventory";
    public bool IsFormClosed => !IsFormOpen;

    [ObservableProperty] private string _inspectHousekeeper = string.Empty;
    [ObservableProperty] private string _inspectSupervisor = string.Empty;
    [ObservableProperty] private DateTime _inspectDate = DateTime.Today;
    [ObservableProperty] private string _inspectTime = "08:00";
    [ObservableProperty] private string _inspectRemark = string.Empty;

    [ObservableProperty] private string _restockItemName = string.Empty;
    [ObservableProperty] private string _restockItemCode = string.Empty;
    [ObservableProperty] private MinibarCatalogOption? _selectedRestockItem;
    [ObservableProperty] private int _restockQty;
    [ObservableProperty] private Guid _restockTransactionId;
    [ObservableProperty] private string _restockWarehouse = "Kho chính";
    [ObservableProperty] private string _restockBy = string.Empty;
    [ObservableProperty] private string _restockRemark = string.Empty;

    [ObservableProperty] private string _postTarget = "Guest Folio";
    [ObservableProperty] private string _postFolioNumber = string.Empty;
    [ObservableProperty] private string _postRemark = string.Empty;

    [ObservableProperty] private string _inventoryRemark = string.Empty;
    [ObservableProperty] private string _inventoryItemCode = string.Empty;
    [ObservableProperty] private string _inventoryItemName = string.Empty;
    [ObservableProperty] private decimal _inventoryQuantity;
    [ObservableProperty] private decimal _inventoryUnitPrice;

    public int TotalRooms => Rooms.Count;
    public int UncheckedCount => Rooms.Count(r => r.Status == "Unchecked");
    public int CheckedCount => Rooms.Count(r => r.Status is "Checked" or "Consumption" or "NoConsumption" or "Posted" or "Unposted");
    public int ConsumptionCount => Rooms.Count(r => r.HasConsumption);
    public int NoConsumptionCount => Rooms.Count(r => r is { Status: "Checked" or "NoConsumption", HasConsumption: false });
    public int PostedCount => Rooms.Count(r => r.IsPosted);
    public int UnpostedCount => Rooms.Count(r => r is { HasConsumption: true, IsPosted: false });
    public int VipCount => Rooms.Count(r => r.IsVip);
    public int RushCount => Rooms.Count(r => r.IsRush);
    public decimal TodayRevenue => Rooms.Where(r => r.IsPosted && r.InspectionTime.HasValue && r.InspectionTime.Value.Date == DateTime.Today).Sum(r => r.ConsumptionAmount);
    public decimal MonthRevenue => Rooms.Where(r => r.IsPosted && r.InspectionTime.HasValue && r.InspectionTime.Value.Year == DateTime.Today.Year && r.InspectionTime.Value.Month == DateTime.Today.Month).Sum(r => r.ConsumptionAmount);
    public decimal TotalStockValue => Rooms.Sum(r => r.Items.Sum(i => i.RemainingQty * i.UnitPrice));
    public string TodayRevenueDisplay => $"{TodayRevenue:N0} ₫";
    public string MonthRevenueDisplay => $"{MonthRevenue:N0} ₫";
    public string TotalStockDisplay => $"{TotalStockValue:N0} ₫";

    public static string[] StatusFilters { get; } = ["Tất cả", "Unchecked", "Checked", "Consumption", "NoConsumption", "Posted", "Unposted"];
    public static string[] BuildingFilters { get; } = ["Tất cả", "A", "B", "C"];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE"];
    public string[] FloorFilters { get; private set; } = [];
    public string[] HousekeeperFilters { get; private set; } = [];
    public static string[] PostTargets { get; } = ["Guest Folio", "Master Folio", "Room Folio"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    private static MinibarRoomDto MapSnapshot(MinibarRoomSnapshot snapshot)
    {
        return new MinibarRoomDto
        {
            PersistentSessionId = snapshot.SessionId,
            PersistentStayId = snapshot.StayId,
            PersistentRoomId = snapshot.RoomId,
            PersistentFolioEntryId = snapshot.FolioEntryId,
            RoomNumber = snapshot.RoomNumber,
            RoomType = string.IsNullOrWhiteSpace(snapshot.RoomType) ? "STD" : snapshot.RoomType,
            Floor = snapshot.Floor ?? string.Empty,
            Building = snapshot.Building ?? string.Empty,
            GuestName = snapshot.GuestName,
            ReservationNumber = snapshot.ReservationNumber,
            FolioNumber = snapshot.FolioNumber,
            ArrivalDate = snapshot.ArrivalDate,
            DepartureDate = snapshot.DepartureDate,
            IsVip = snapshot.IsVip,
            Housekeeper = snapshot.Housekeeper,
            Supervisor = snapshot.Supervisor,
            InspectionTime = snapshot.InspectionTime,
            Status = string.IsNullOrWhiteSpace(snapshot.Status) ? "Unchecked" : snapshot.Status,
            ItemCount = snapshot.ItemCount,
            ConsumptionAmount = snapshot.ConsumptionAmount,
            VatAmount = snapshot.VatAmount,
            RestockAmount = snapshot.RestockAmount,
            IsPosted = snapshot.IsPosted,
            PostTarget = snapshot.PostTarget,
            Remark = snapshot.Remark,
            RestockQuantity = snapshot.RestockQuantity,
            LastInventoryAtUtc = snapshot.LastInventoryAtUtc,
            InventoryLineCount = snapshot.InventoryLineCount,
            InventoryItems = snapshot.InventoryItems?.Select(item => new MinibarInventoryLineInputDto
            {
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList() ?? [],
            Items = snapshot.Items.Select(item => new MinibarLineItemDto
            {
                ItemCode = item.ItemCode ?? string.Empty,
                Name = item.Name,
                Unit = item.Unit,
                StandardQty = item.StandardQty,
                RemainingQty = item.RemainingQty,
                RestockQty = item.RestockQty,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                VatAmount = item.VatAmount,
                Category = item.Category ?? string.Empty
            }).ToList()
        };
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            var propertyId = _executionContext?.Current?.Property.PropertyId.Value ?? Guid.Empty;
            if (_minibarQueryService is null || propertyId == Guid.Empty)
            {
                Rooms.Clear();
                ErrorMessage = "Minibar chưa có property context hoặc query service dữ liệu thật.";
                BuildDashboard();
                BuildTree();
                SelectTreeItem(TreeItems.FirstOrDefault());
                return;
            }

            var snapshots = await _minibarQueryService.QueryAsync(propertyId, ct);
            Rooms.Clear();
            foreach (var snapshot in snapshots)
            {
                Rooms.Add(MapSnapshot(snapshot));
            }
            RestockItemOptions.Clear();
            foreach (var item in Rooms.SelectMany(x => x.Items)
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemCode))
                .GroupBy(x => x.ItemCode!, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name))
            {
                RestockItemOptions.Add(new MinibarCatalogOption
                {
                    ItemCode = item.ItemCode!, ItemName = item.Name,
                    Category = item.Category, UnitPrice = item.UnitPrice
                });
            }

            await LoadWarehouseOptionsAsync(ct);

            FloorFilters = Rooms.Select(room => room.Floor)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();
            HousekeeperFilters = Rooms.Select(room => room.Housekeeper)
                .OfType<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();
            OnPropertyChanged(nameof(FloorFilters));
            OnPropertyChanged(nameof(HousekeeperFilters));
            BuildDashboard();
            BuildTree();
            SelectTreeItem(TreeItems.FirstOrDefault());
            return;

        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task LoadWarehouseOptionsAsync(CancellationToken ct)
    {
        WarehouseOptions.Clear();
        if (_warehouseAppService is null)
            return;

        var warehouses = await _warehouseAppService.SearchAsync(string.Empty, ct);
        foreach (var warehouse in warehouses
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsConsumptionWarehouse)
            .ThenBy(x => x.WarehouseCode, StringComparer.OrdinalIgnoreCase))
        {
            WarehouseOptions.Add(MinibarWarehouseOption.From(warehouse));
        }
    }

    private static string ToUserMessage(Exception ex) => ex.Message switch
    {
        "MINIBAR_WAREHOUSE_NOT_FOUND" => "Kho xuất không tồn tại hoặc đã ngừng hoạt động.",
        "MINIBAR_ITEM_NOT_FOUND" => "Mặt hàng không còn trong danh mục minibar hoặc đã ngừng sử dụng.",
        "MINIBAR_STOCK_INSUFFICIENT" => "Tồn kho thực tế không đủ để cấp bù mặt hàng này.",
        "MINIBAR_ROOM_NOT_FOUND" => "Phòng không còn hoạt động hoặc không thuộc cơ sở hiện tại.",
        "MINIBAR_SESSION_NOT_FOUND" => "Không tìm thấy phiên minibar thật của phòng.",
        "MINIBAR_SESSION_ALREADY_POSTED" => "Phiên minibar của stay này đã Post; không thể tạo phiên kiểm tra trùng.",
        "MINIBAR_FOLIO_ENTRY_NOT_FOUND" => "Không tìm thấy khoản Folio để ghi nhận minibar.",
        "MINIBAR_FOLIO_ENTRY_INVALID" => "Khoản Folio không thuộc đúng stay hoặc không phải khoản minibar.",
        "MINIBAR_FOLIO_NOT_FOUND" => "Stay chưa có Folio đang mở để ghi nhận minibar.",
        "MINIBAR_NO_CHARGE" => "Phiên minibar không có khoản tiêu dùng để ghi Folio.",
        _ => ex.Message
    };

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new MinibarKpi { Label = "Tổng phòng có minibar", Value = TotalRooms.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new MinibarKpi { Label = "Chưa kiểm tra", Value = UncheckedCount.ToString(), Color = "#94A3B8", BgColor = "#F1F5F9", FilterKey = "Status", FilterValue = "Unchecked" });
        DashboardKpis.Add(new MinibarKpi { Label = "Đã kiểm tra", Value = CheckedCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", FilterKey = "Status", FilterValue = "Checked" });
        DashboardKpis.Add(new MinibarKpi { Label = "Có phát sinh", Value = ConsumptionCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", FilterKey = "Status", FilterValue = "Consumption" });
        DashboardKpis.Add(new MinibarKpi { Label = "Không phát sinh", Value = NoConsumptionCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new MinibarKpi { Label = "Đã Post Folio", Value = PostedCount.ToString(), Color = "#16A34A", BgColor = "#DCFCE7", FilterKey = "Posted", FilterValue = "true" });
        DashboardKpis.Add(new MinibarKpi { Label = "Chưa Post", Value = UnpostedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Unposted", FilterValue = "true" });
        DashboardKpis.Add(new MinibarKpi { Label = "Doanh thu hôm nay", Value = TodayRevenueDisplay, Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new MinibarKpi { Label = "Doanh thu tháng", Value = MonthRevenueDisplay, Color = "#4F46E5", BgColor = "#EEF2FF" });
        DashboardKpis.Add(new MinibarKpi { Label = "Giá trị tồn", Value = TotalStockDisplay, Color = "#7C3AED", BgColor = "#F5F3FF" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new MinibarTreeItem { Label = "Tất cả", Count = TotalRooms, IsSelected = true });
        TreeItems.Add(new MinibarTreeItem { Label = "Chưa kiểm tra", Count = UncheckedCount, FilterKey = "Status", FilterValue = "Unchecked" });
        TreeItems.Add(new MinibarTreeItem { Label = "Có phát sinh", Count = ConsumptionCount, FilterKey = "Status", FilterValue = "Consumption" });
        TreeItems.Add(new MinibarTreeItem { Label = "Không phát sinh", Count = NoConsumptionCount, FilterKey = "Status", FilterValue = "NoConsumption" });
        TreeItems.Add(new MinibarTreeItem { Label = "Đã Post", Count = PostedCount, FilterKey = "Posted", FilterValue = "true" });
        TreeItems.Add(new MinibarTreeItem { Label = "Chưa Post", Count = UnpostedCount, FilterKey = "Unposted", FilterValue = "true" });
        TreeItems.Add(new MinibarTreeItem { Label = "VIP", Count = VipCount, FilterKey = "Vip", FilterValue = "true" });
        TreeItems.Add(new MinibarTreeItem { Label = "Rush", Count = RushCount, FilterKey = "Rush", FilterValue = "true" });
    }

    private void BuildTimeline(MinibarRoomDto room) { RoomTimeline.Clear(); foreach (var e in room.Timeline.OrderBy(t => t.Timestamp)) RoomTimeline.Add(e); }
    private void BuildAuditLog(MinibarRoomDto room) { RoomAuditLog.Clear(); foreach (var e in room.AuditLog.OrderBy(t => t.Timestamp)) RoomAuditLog.Add(e); }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Rooms.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.RoomNumber.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.ReservationNumber?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterBuilding) && FilterBuilding != "Tất cả") q = q.Where(r => r.Building == FilterBuilding);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterHousekeeper) && FilterHousekeeper != "Tất cả") q = q.Where(r => r.Housekeeper == FilterHousekeeper);
        if (FilterVipOnly) q = q.Where(r => r.IsVip);
        if (FilterPostedOnly) q = q.Where(r => r.IsPosted);
        if (FilterUnpostedOnly) q = q.Where(r => r is { HasConsumption: true, IsPosted: false });
        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Status" => q.Where(r => r.Status == v),
                "Posted" => q.Where(r => r.IsPosted),
                "Unposted" => q.Where(r => r is { HasConsumption: true, IsPosted: false }),
                "Vip" => q.Where(r => r.IsVip),
                "Rush" => q.Where(r => r.IsRush),
                _ => q
            };
        }
        var list = q.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredRooms.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredRooms.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterStatus = null; FilterFloor = null; FilterBuilding = null;
        FilterRoomType = null; FilterHousekeeper = null; FilterVipOnly = false; FilterPostedOnly = false; FilterUnpostedOnly = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void SelectTreeItem(MinibarTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) ti.IsSelected = false;
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }

    [RelayCommand] private void ApplyKpiFilter(MinibarKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Status") FilterStatus = kpi.FilterValue;
        else if (kpi.FilterKey == "Posted") FilterPostedOnly = true;
        else if (kpi.FilterKey == "Unposted") FilterUnpostedOnly = true;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterBuildingChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterHousekeeperChanged(string? value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterPostedOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterUnpostedOnlyChanged(bool value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages; var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5 ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3 ? new int?[] { 1, 2, 3, 4, 5, null, total }
            : current >= total - 2 ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
            : new int?[] { 1, null, current - 1, current, current + 1, null, total };
        foreach (var p in pages)
            PageButtons.Add(p is null ? new MinibarPaginationButton("…", null, false, true)
                : new MinibarPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectRoom(MinibarRoomDto? room) => SelectedRoom = room;

    public bool SelectRoomById(Guid roomId)
    {
        if (roomId == Guid.Empty) return false;
        var room = Rooms.FirstOrDefault(item => item.PersistentRoomId == roomId);
        if (room is null) return false;
        SelectedRoom = room;
        IsDetailOpen = true;
        return true;
    }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedRoom = null; RoomTimeline.Clear(); RoomAuditLog.Clear(); }
    partial void OnSelectedRoomChanged(MinibarRoomDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; BuildTimeline(value); BuildAuditLog(value); }
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsItemsTab));
        OnPropertyChanged(nameof(IsChargeTab)); OnPropertyChanged(nameof(IsHistoryTab)); OnPropertyChanged(nameof(IsRemarkTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsItemsTab => SelectedDetailTab == 1;
    public bool IsChargeTab => SelectedDetailTab == 2;
    public bool IsHistoryTab => SelectedDetailTab == 3;
    public bool IsRemarkTab => SelectedDetailTab == 4;
    partial void OnFormModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsInspectForm)); OnPropertyChanged(nameof(IsRestockForm));
        OnPropertyChanged(nameof(IsPostForm)); OnPropertyChanged(nameof(IsInventoryForm));
    }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));

    // ═══ WORKFLOW ═══

    [RelayCommand] private void OpenInspectForm()
    {
        if (SelectedRoom is null) return;
        FormMode = "Inspect"; FormTitle = $"Kiểm tra Minibar — Phòng {SelectedRoom.RoomNumber}"; FormErrorMessage = string.Empty;
        InspectHousekeeper = SelectedRoom.Housekeeper ?? string.Empty;
        InspectSupervisor = SelectedRoom.Supervisor ?? string.Empty;
        InspectDate = DateTime.Today; InspectTime = "08:00"; InspectRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitInspectFormAsync()
    {
        if (SelectedRoom is null) return;
        var inspTime = InspectDate.Date + (TimeSpan.TryParse(InspectTime, out var t) ? t : TimeSpan.Zero);
        if (_minibarCommandService is not null && CurrentPropertyId != Guid.Empty &&
            SelectedRoom.PersistentStayId.HasValue && SelectedRoom.PersistentRoomId != Guid.Empty)
        {
            var actorId = CurrentActorId;
            if (!actorId.HasValue)
            {
                FormErrorMessage = "Không xác định được người thao tác.";
                return;
            }

            try
            {
                var roomNumber = SelectedRoom.RoomNumber;
                var lines = SelectedRoom.Items
                    .Select(item => new MinibarCheckLine(
                        string.IsNullOrWhiteSpace(item.ItemCode) ? item.Name : item.ItemCode,
                        item.Name,
                        item.Consumed,
                        item.UnitPrice))
                    .Where(line => line.Quantity > 0)
                    .ToArray();
                await _minibarCommandService.CheckAsync(new CheckMinibarCommand(
                    SelectedRoom.PersistentSessionId ?? Guid.Empty,
                    CurrentPropertyId,
                    SelectedRoom.PersistentStayId.Value,
                    SelectedRoom.PersistentRoomId,
                    actorId.Value,
                    inspTime,
                    lines));
                await LoadAsync(CancellationToken.None);
                SelectedRoom = Rooms.FirstOrDefault(room => room.RoomNumber == roomNumber);
                IsFormOpen = false;
                BuildDashboard(); BuildTree(); ApplyFilters();
                InfoMessage = $"Đã lưu kiểm tra minibar phòng {roomNumber}.";
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ToUserMessage(ex);
                return;
            }
        }

        FormErrorMessage = "Phòng minibar chưa có stay/session thật để lưu kiểm tra; thao tác không được ghi giả.";
    }

    [RelayCommand] private void OpenRestockForm()
    {
        if (SelectedRoom is null) return;
        FormMode = "Restock"; FormTitle = $"Bổ sung Minibar — Phòng {SelectedRoom.RoomNumber}"; FormErrorMessage = string.Empty;
        SelectedRestockItem = RestockItemOptions.FirstOrDefault();
        RestockItemCode = SelectedRestockItem?.ItemCode ?? string.Empty;
        RestockItemName = SelectedRestockItem?.ItemName ?? string.Empty;
        RestockQty = 0;
        RestockTransactionId = Guid.NewGuid();
        RestockWarehouse = WarehouseOptions.FirstOrDefault()?.WarehouseCode ?? string.Empty;
        RestockBy = SelectedRoom.Housekeeper ?? string.Empty; RestockRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitRestockFormAsync()
    {
        if (SelectedRoom is null) return;
        if (string.IsNullOrWhiteSpace(RestockItemCode) || string.IsNullOrWhiteSpace(RestockItemName)) { FormErrorMessage = "Vui lòng chọn mặt hàng trong danh mục minibar."; return; }
        if (RestockQty <= 0) { FormErrorMessage = "Vui lòng nhập số lượng bổ sung."; return; }
        if (WarehouseOptions.Count == 0)
        {
            FormErrorMessage = "Chưa có kho hoạt động trong danh mục kho; không thể ghi nhận cấp bù.";
            return;
        }
        if (!WarehouseOptions.Any(x => string.Equals(x.WarehouseCode, RestockWarehouse, StringComparison.OrdinalIgnoreCase)))
        {
            FormErrorMessage = "Kho xuất không còn hoạt động hoặc không tồn tại; vui lòng chọn lại kho.";
            return;
        }

        if (CurrentPropertyId == Guid.Empty || SelectedRoom.PersistentRoomId == Guid.Empty)
        {
            FormErrorMessage = "Minibar chưa có property/phòng thật để ghi nhận cấp bù.";
            return;
        }
        if (!CurrentActorId.HasValue)
        {
            FormErrorMessage = "Không xác định được người thao tác.";
            return;
        }

        try
        {
            var roomNumber = SelectedRoom.RoomNumber;
            await _minibarCommandService.RestockAsync(new RestockMinibarCommand(
                RestockTransactionId == Guid.Empty ? Guid.NewGuid() : RestockTransactionId,
                CurrentPropertyId, SelectedRoom.PersistentRoomId,
                RestockItemCode, RestockItemName, RestockQty, RestockWarehouse,
                CurrentActorId.Value, DateTime.UtcNow, RestockRemark));
            await LoadAsync(CancellationToken.None);
            SelectedRoom = Rooms.FirstOrDefault(room => room.RoomNumber == roomNumber);
            IsFormOpen = false;
            InfoMessage = $"Đã ghi nhận cấp bù {RestockQty} {RestockItemName} cho phòng {roomNumber}.";
        }
        catch (Exception ex)
        {
            FormErrorMessage = ToUserMessage(ex);
        }
    }

    partial void OnSelectedRestockItemChanged(MinibarCatalogOption? value)
    {
        RestockItemCode = value?.ItemCode ?? string.Empty;
        RestockItemName = value?.ItemName ?? string.Empty;
    }

    [RelayCommand] private void OpenPostForm()
    {
        if (SelectedRoom is null || !SelectedRoom.HasConsumption) return;
        if (SelectedRoom.IsVip && SelectedRoom.Supervisor is null)
        { InfoMessage = "Phòng VIP — bắt buộc Supervisor xác nhận trước khi Post."; return; }

        FormMode = "Post"; FormTitle = $"Post Folio — Phòng {SelectedRoom.RoomNumber}"; FormErrorMessage = string.Empty;
        PostTarget = "Guest Folio"; PostFolioNumber = SelectedRoom.FolioNumber ?? ""; PostRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitPostFormAsync()
    {
        if (SelectedRoom is null) return;

        if (_minibarCommandService is not null && CurrentPropertyId != Guid.Empty && SelectedRoom.PersistentSessionId.HasValue)
        {
            var actorId = CurrentActorId;
            if (!actorId.HasValue)
            {
                FormErrorMessage = "Không xác định được người thao tác.";
                return;
            }

            var folioEntryId = SelectedRoom.PersistentFolioEntryId
                ?? (Guid.TryParse(PostFolioNumber, out var parsedFolioEntryId) ? parsedFolioEntryId : Guid.Empty);

            try
            {
                var roomNumber = SelectedRoom.RoomNumber;
                await _minibarCommandService.PostAsync(new PostMinibarCommand(
                    SelectedRoom.PersistentSessionId.Value,
                    CurrentPropertyId,
                    folioEntryId,
                    actorId.Value,
                    DateTime.UtcNow));
                await LoadAsync(CancellationToken.None);
                SelectedRoom = Rooms.FirstOrDefault(room => room.RoomNumber == roomNumber);
                IsFormOpen = false;
                BuildDashboard(); BuildTree(); ApplyFilters();
                InfoMessage = $"Đã Post minibar phòng {roomNumber} → {PostTarget}.";
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ToUserMessage(ex);
                return;
            }
        }

        FormErrorMessage = "Minibar chưa có session/Folio entry thật để Post; thao tác không được ghi giả.";
    }

    [RelayCommand] private async Task CancelPostAsync()
    {
        if (SelectedRoom is null || !SelectedRoom.IsPosted) return;
        var r = MessageBox.Show($"Hủy Post Folio phòng {SelectedRoom.RoomNumber}?\n\nKhoản {SelectedRoom.ConsumptionAmount:N0} ₫ sẽ được gỡ khỏi {SelectedRoom.PostTarget}.", "Hủy Post", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (r != MessageBoxResult.OK) return;

        if (_minibarCommandService is not null && CurrentPropertyId != Guid.Empty && SelectedRoom.PersistentSessionId.HasValue)
        {
            var actorId = CurrentActorId;
            if (!actorId.HasValue)
            {
                FormErrorMessage = "Không xác định được người thao tác.";
                return;
            }

            try
            {
                var roomNumber = SelectedRoom.RoomNumber;
                await _minibarCommandService.UnpostAsync(new UnpostMinibarCommand(
                    SelectedRoom.PersistentSessionId.Value,
                    CurrentPropertyId,
                    actorId.Value,
                    DateTime.UtcNow));
                await LoadAsync(CancellationToken.None);
                SelectedRoom = Rooms.FirstOrDefault(room => room.RoomNumber == roomNumber);
                BuildDashboard(); BuildTree(); ApplyFilters();
                InfoMessage = $"Đã hủy Post minibar phòng {roomNumber}.";
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ToUserMessage(ex);
                return;
            }
        }

        FormErrorMessage = "Minibar chưa có session thật để hủy Post; thao tác không được ghi giả.";
    }

    [RelayCommand] private void OpenInventoryForm()
    {
        if (SelectedRoom is null) return;
        FormMode = "Inventory"; FormTitle = $"Kiểm kê Minibar — Phòng {SelectedRoom.RoomNumber}"; FormErrorMessage = string.Empty;
        InventoryRemark = string.Empty;
        InventoryLines.Clear();
        var persistedItems = SelectedRoom.InventoryItems.Count > 0
            ? SelectedRoom.InventoryItems
            : SelectedRoom.Items.Select(item => new MinibarInventoryLineInputDto
            {
                ItemCode = string.IsNullOrWhiteSpace(item.ItemCode) ? item.Name : item.ItemCode,
                ItemName = item.Name,
                Quantity = item.RemainingQty,
                UnitPrice = item.UnitPrice
            }).ToList();
        foreach (var item in persistedItems)
        {
            InventoryLines.Add(new MinibarInventoryLineInputDto
            {
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }
        InventoryItemCode = string.Empty;
        InventoryItemName = string.Empty;
        InventoryQuantity = 0;
        InventoryUnitPrice = 0;
        IsFormOpen = true;
    }

    [RelayCommand]
    private void AddInventoryLine()
    {
        if (string.IsNullOrWhiteSpace(InventoryItemCode) || string.IsNullOrWhiteSpace(InventoryItemName))
        {
            FormErrorMessage = "Vui lòng nhập mã và tên mặt hàng.";
            return;
        }
        if (InventoryQuantity < 0 || InventoryUnitPrice < 0)
        {
            FormErrorMessage = "Số lượng và đơn giá kiểm kê không được âm.";
            return;
        }

        InventoryLines.Add(new MinibarInventoryLineInputDto
        {
            ItemCode = InventoryItemCode,
            ItemName = InventoryItemName,
            Quantity = InventoryQuantity,
            UnitPrice = InventoryUnitPrice
        });
        InventoryItemCode = string.Empty;
        InventoryItemName = string.Empty;
        InventoryQuantity = 0;
        InventoryUnitPrice = 0;
        FormErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void RemoveInventoryLine(MinibarInventoryLineInputDto? line)
    {
        if (line is not null) InventoryLines.Remove(line);
    }

    [RelayCommand]
    private async Task SubmitInventoryFormAsync()
    {
        if (SelectedRoom is null) return;
        if (CurrentPropertyId == Guid.Empty || SelectedRoom.PersistentRoomId == Guid.Empty)
        {
            FormErrorMessage = "Minibar chưa có property/phòng thật để ghi nhận kiểm kê.";
            return;
        }
        if (!CurrentActorId.HasValue)
        {
            FormErrorMessage = "Không xác định được người thao tác.";
            return;
        }
        if (InventoryLines.Count == 0)
        {
            FormErrorMessage = "Vui lòng thêm ít nhất một mặt hàng kiểm kê.";
            return;
        }
        if (InventoryLines.Any(item => string.IsNullOrWhiteSpace(item.ItemCode)
            || string.IsNullOrWhiteSpace(item.ItemName)
            || item.Quantity < 0
            || item.UnitPrice < 0))
        {
            FormErrorMessage = "Dữ liệu dòng kiểm kê chưa hợp lệ.";
            return;
        }

        try
        {
            var roomNumber = SelectedRoom.RoomNumber;
            await _minibarCommandService.InventoryAsync(new InventoryMinibarCommand(
                Guid.NewGuid(), CurrentPropertyId, SelectedRoom.PersistentRoomId,
                SelectedRoom.PersistentStayId, CurrentActorId.Value, DateTime.UtcNow,
                InventoryLines.Select(item => new MinibarInventoryLineInput(
                    item.ItemCode, item.ItemName, item.Quantity, item.UnitPrice)).ToArray(),
                InventoryRemark));
            await LoadAsync(CancellationToken.None);
            SelectedRoom = Rooms.FirstOrDefault(room => room.RoomNumber == roomNumber);
            IsFormOpen = false;
            InfoMessage = $"Đã lưu kiểm kê {InventoryLines.Count} dòng cho phòng {roomNumber}.";
        }
        catch (Exception ex)
        {
            FormErrorMessage = ToUserMessage(ex);
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand] private void RecordConsumption()
    {
        if (SelectedRoom is null) return;
        OpenInspectForm();
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (SelectedRoom is null) return;
        if (CurrentPropertyId == Guid.Empty || SelectedRoom.PersistentRoomId == Guid.Empty)
        {
            InfoMessage = "Minibar chưa có dữ liệu phòng thật để tạo báo cáo.";
            return;
        }

        try
        {
            var path = CsvExportHelper.Export(
                "minibar_report",
                ["Phòng", "Khách", "Trạng thái", "Số mặt hàng", "Tiêu thụ", "Cấp bù", "Đã Post", "Kiểm kê gần nhất"],
                [
                    (IReadOnlyList<string>)[
                        SelectedRoom.RoomNumber,
                        SelectedRoom.GuestName ?? string.Empty,
                        SelectedRoom.StatusLabel,
                        SelectedRoom.ItemCount.ToString(),
                        CsvExportHelper.F(SelectedRoom.ConsumptionAmount),
                        SelectedRoom.RestockQuantity.ToString("N0"),
                        SelectedRoom.IsPosted ? "Có" : "Không",
                        SelectedRoom.InventoryDisplay
                    ]
                ]);
            InfoMessage = $"Đã tạo báo cáo Minibar thật: {path}";
            MessageBox.Show(InfoMessage, "Báo cáo Minibar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tạo báo cáo Minibar thật: {ex.Message}";
        }
    }

    [RelayCommand] private void OpenGuestProfile()
    {
        if (SelectedRoom?.GuestName is null) return;
        MessageBox.Show($"HỒ SƠ KHÁCH\n\nTên: {SelectedRoom.GuestName}\nReservation: {SelectedRoom.ReservationNumber}\nFolio: {SelectedRoom.FolioNumber}\nArrival: {SelectedRoom.ArrivalDate:dd/MM/yyyy}\nDeparture: {SelectedRoom.DepartureDate:dd/MM/yyyy}", "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void SetGridView() => ViewMode = "Grid";
    [RelayCommand] private void SetCardView() => ViewMode = "Card";
    [RelayCommand]
    private void ExportExcel()
    {
        var rows = FilteredRooms.Count > 0 ? FilteredRooms.ToList() : Rooms.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Chưa có dữ liệu Minibar thật để xuất.";
            return;
        }

        try
        {
            var path = CsvExportHelper.Export(
                "minibar",
                ["Phòng", "Tầng", "Khách", "Trạng thái", "Số mặt hàng", "Tiêu thụ", "Cấp bù", "Đã Post", "Folio"],
                rows.Select(row => (IReadOnlyList<string>)[
                    row.RoomNumber,
                    row.Floor,
                    row.GuestName ?? string.Empty,
                    row.StatusLabel,
                    row.ItemCount.ToString(),
                    CsvExportHelper.F(row.ConsumptionAmount),
                    row.RestockQuantity.ToString("N0"),
                    row.IsPosted ? "Có" : "Không",
                    row.FolioNumber ?? string.Empty
                ]).ToList());
            InfoMessage = $"Đã xuất {rows.Count} phòng Minibar thật ra file CSV tương thích Excel: {path}";
            MessageBox.Show(InfoMessage, "Xuất Minibar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể xuất Minibar thật: {ex.Message}";
        }
    }
    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
