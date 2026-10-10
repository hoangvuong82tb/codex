using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class BanquetRoomLayoutItemVm : ObservableObject
{
    [ObservableProperty] private string itemType = "RoundTable";
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private int seatCount = 10;
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private double width = 82;
    [ObservableProperty] private double height = 82;
    [ObservableProperty] private double rotation;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty, JsonIgnore] private bool isSelected;
}

public sealed partial class BanquetRestaurantViewModel
{
    private bool _updatingReportPeriod;
    private readonly SemaphoreSlim _quantificationOperationGate = new(1, 1);
    private static readonly string BanquetSettingsFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Data",
        "banquet_module_settings.json");

    public ObservableCollection<BanquetRoomDto> FilteredRooms { get; } = new();
    public ObservableCollection<BanquetRoomLayoutItemVm> RoomLayoutItems { get; } = new();
    public ObservableCollection<BanquetQuantificationItemVm> QuantificationIngredients { get; } = new();
    public ObservableCollection<BanquetQuantificationItemVm> QuantificationServices { get; } = new();
    public ObservableCollection<BanquetQuantificationMissingVm> QuantificationMissingRecipes { get; } = new();
    public ObservableCollection<BanquetQuantificationEventVm> QuantificationEvents { get; } = new();
    public IReadOnlyList<LookupOption<string>> QuantificationStatusOptions { get; } = new LookupOption<string>[]
    {
        new("Sẵn sàng chuẩn bị", "Ready"),
        new("Tất cả chưa hủy", "Active"),
        new("Đã xác nhận", "Confirmed"),
        new("Đang chuẩn bị", "Preparing")
    };
    [ObservableProperty] private DateTime quantificationFromDate = DateTime.Today;
    [ObservableProperty] private DateTime quantificationToDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private string quantificationStatusFilter = "Ready";
    [ObservableProperty] private string quantificationStatusText = "Chưa tổng hợp dữ liệu.";
    [ObservableProperty] private int quantificationEventCount;
    [ObservableProperty] private DateTime? quantificationSentAt;
    [ObservableProperty] private bool isQuantificationLoading;
    [ObservableProperty] private BanquetQuantificationEventVm? selectedQuantificationEvent;
    public IReadOnlyList<LookupOption<string>> RoomStatusOptions { get; } = new LookupOption<string>[]
    {
        new("Sẵn sàng", "Available"), new("Đang sử dụng", "Occupied"),
        new("Bảo trì", "Maintenance"), new("Ngừng hoạt động", "Inactive")
    };
    [ObservableProperty] private string roomSearchText = string.Empty;
    [ObservableProperty] private string roomDescription = string.Empty;
    [ObservableProperty] private string roomStatus = "Available";
    [ObservableProperty] private bool roomIsActive = true;
    [ObservableProperty] private int roomWorkspaceMode;
    [ObservableProperty] private BanquetRoomLayoutItemVm? selectedRoomLayoutItem;
    [ObservableProperty] private int roomLayoutRows = 5;
    [ObservableProperty] private int roomLayoutTablesPerRow = 8;
    [ObservableProperty] private int roomLayoutSeatsPerTable = 10;
    [ObservableProperty] private double roomLayoutHorizontalGap = 18;
    [ObservableProperty] private double roomLayoutVerticalGap = 16;
    [ObservableProperty] private double roomLayoutAisleWidth = 90;
    [ObservableProperty] private bool roomLayoutShowNumbers = true;
    [ObservableProperty] private double roomMapZoom = 1;
    [ObservableProperty] private bool roomLayoutIsDirty;
    [ObservableProperty] private bool roomLayoutSnapToGrid = true;
    [ObservableProperty] private int roomLayoutGridSize = 10;

    private readonly List<string> _roomLayoutUndoStack = [];
    private readonly List<string> _roomLayoutRedoStack = [];
    private bool _isRestoringRoomLayout;
    private BanquetRoomLayoutItemVm? _trackedRoomLayoutItem;

    public int RoomLayoutTableCount => RoomLayoutItems.Count(x => x.ItemType.EndsWith("Table", StringComparison.Ordinal));
    public int RoomLayoutTotalSeats => RoomLayoutItems.Where(x => x.ItemType.EndsWith("Table", StringComparison.Ordinal)).Sum(x => x.SeatCount);
    public bool CanUndoRoomLayout => _roomLayoutUndoStack.Count > 0;
    public bool CanRedoRoomLayout => _roomLayoutRedoStack.Count > 0;
    public string RoomMapZoomDisplay => $"{RoomMapZoom * 100:0}%";
    public string RoomLayoutSaveStatus => RoomLayoutIsDirty ? "Có thay đổi chưa lưu" : "Đã đồng bộ";
    public int QuantificationIngredientCount => QuantificationIngredients.Count;
    public int QuantificationServiceCount => QuantificationServices.Count;
    public int QuantificationMissingRecipeCount => QuantificationMissingRecipes.Count;
    public decimal QuantificationEstimatedCost => QuantificationIngredients.Sum(x => x.EstimatedCost);
    public bool HasQuantificationData => QuantificationIngredients.Count > 0 || QuantificationServices.Count > 0;
    public bool IsQuantificationEmpty => !HasQuantificationData;
    public bool IsQuantificationIngredientsEmpty => QuantificationIngredients.Count == 0;
    public bool IsQuantificationServicesEmpty => QuantificationServices.Count == 0;
    public bool HasQuantificationWarnings => QuantificationMissingRecipes.Count > 0;
    public string QuantificationDateRangeText => $"{QuantificationFromDate:dd/MM/yyyy} - {QuantificationToDate:dd/MM/yyyy}";
    public string QuantificationSentText => QuantificationSentAt.HasValue
        ? $"Đã gửi lúc {QuantificationSentAt:HH:mm dd/MM/yyyy}"
        : "Chưa gửi bộ phận nhập hàng";
    public bool HasQuantificationEvents => QuantificationEvents.Count > 0;
    public bool IsQuantificationEventsEmpty => QuantificationEvents.Count == 0;
    public string SelectedQuantificationEventTitle => SelectedQuantificationEvent is null
        ? "Chưa chọn tiệc"
        : $"{SelectedQuantificationEvent.EventTypeDisplay} · {SelectedQuantificationEvent.CustomerName}";
    public string SelectedQuantificationEventDetail => SelectedQuantificationEvent is null
        ? "Chọn một tiệc trong danh sách để xem định lượng riêng."
        : $"{SelectedQuantificationEvent.EventDateText} · {SelectedQuantificationEvent.TimeText} · {SelectedQuantificationEvent.LocationText}";

    partial void OnRoomSearchTextChanged(string value) => RefreshRoomFilter();
    partial void OnRoomMapZoomChanged(double value) => OnPropertyChanged(nameof(RoomMapZoomDisplay));
    partial void OnRoomLayoutIsDirtyChanged(bool value) => OnPropertyChanged(nameof(RoomLayoutSaveStatus));
    partial void OnSelectedRoomLayoutItemChanged(BanquetRoomLayoutItemVm? value)
    {
        if (_trackedRoomLayoutItem is not null)
            _trackedRoomLayoutItem.PropertyChanged -= OnTrackedRoomLayoutItemPropertyChanged;

        foreach (var item in RoomLayoutItems)
            item.IsSelected = ReferenceEquals(item, value);

        _trackedRoomLayoutItem = value;
        if (_trackedRoomLayoutItem is not null)
            _trackedRoomLayoutItem.PropertyChanged += OnTrackedRoomLayoutItemPropertyChanged;
    }

    private void OnTrackedRoomLayoutItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRestoringRoomLayout || e.PropertyName == nameof(BanquetRoomLayoutItemVm.IsSelected)) return;
        RoomLayoutIsDirty = true;
        NotifyRoomLayoutSummaryChanged();
    }

    private void RefreshRoomFilter()
    {
        var query = RoomSearchText.Trim();
        FilteredRooms.Clear();
        foreach (var room in Rooms.Where(x => string.IsNullOrWhiteSpace(query)
            || x.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
            || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            FilteredRooms.Add(room);
    }

    private void LoadRoomLayout(string? json)
    {
        SelectedRoomLayoutItem = null;
        RoomLayoutItems.Clear();
        _roomLayoutUndoStack.Clear();
        _roomLayoutRedoStack.Clear();
        if (string.IsNullOrWhiteSpace(json))
        {
            RoomLayoutIsDirty = false;
            NotifyRoomLayoutHistoryChanged();
            NotifyRoomLayoutSummaryChanged();
            return;
        }
        try
        {
            foreach (var item in JsonSerializer.Deserialize<List<BanquetRoomLayoutItemVm>>(json) ?? [])
                RoomLayoutItems.Add(item);
            NotifyRoomLayoutSummaryChanged();
            RoomLayoutIsDirty = false;
            NotifyRoomLayoutHistoryChanged();
        }
        catch
        {
            StatusText = "Sơ đồ phòng cũ không hợp lệ; bạn có thể thiết kế lại và lưu.";
        }
    }

    private string SerializeRoomLayout() => JsonSerializer.Serialize(RoomLayoutItems);

    [RelayCommand]
    private async Task SaveRoomLayoutForSelectedEventAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn một sự kiện trước khi lưu sơ đồ riêng.";
            return;
        }
        await RunBusyAsync(async () =>
        {
            await _service.SaveFloorPlanAsync(new SaveBanquetFloorPlanRequest
            {
                BanquetEventId = SelectedEvent.Id,
                Name = $"Sơ đồ {SelectedEvent.EventNo} - {SelectedEvent.CustomerName}",
                CanvasWidth = 760,
                CanvasHeight = 520,
                LayoutJson = SerializeRoomLayout()
            });
            StatusText = $"Đã chốt sơ đồ riêng cho tiệc {SelectedEvent.EventNo}.";
        });
    }

    public void CaptureRoomLayoutSnapshot()
    {
        if (_isRestoringRoomLayout) return;
        var snapshot = SerializeRoomLayout();
        if (_roomLayoutUndoStack.Count == 0 || !string.Equals(_roomLayoutUndoStack[^1], snapshot, StringComparison.Ordinal))
        {
            _roomLayoutUndoStack.Add(snapshot);
            if (_roomLayoutUndoStack.Count > 30) _roomLayoutUndoStack.RemoveAt(0);
        }
        _roomLayoutRedoStack.Clear();
        NotifyRoomLayoutHistoryChanged();
    }

    public void CommitRoomLayoutChange()
    {
        RoomLayoutIsDirty = true;
        NotifyRoomLayoutSummaryChanged();
    }

    private void NotifyRoomLayoutHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndoRoomLayout));
        OnPropertyChanged(nameof(CanRedoRoomLayout));
    }

    private void RestoreRoomLayoutSnapshot(string json)
    {
        _isRestoringRoomLayout = true;
        try
        {
            RoomLayoutItems.Clear();
            foreach (var item in JsonSerializer.Deserialize<List<BanquetRoomLayoutItemVm>>(json) ?? [])
                RoomLayoutItems.Add(item);
            SelectedRoomLayoutItem = null;
            RoomLayoutIsDirty = true;
            NotifyRoomLayoutSummaryChanged();
        }
        finally { _isRestoringRoomLayout = false; }
    }

    private void NotifyRoomLayoutSummaryChanged()
    {
        OnPropertyChanged(nameof(RoomLayoutTableCount));
        OnPropertyChanged(nameof(RoomLayoutTotalSeats));
    }

    [RelayCommand]
    private void SetRoomWorkspaceMode(string? mode) => RoomWorkspaceMode = mode == "Map" ? 1 : 0;

    [RelayCommand]
    private void AddRoomLayoutItem(string? type)
    {
        CaptureRoomLayoutSnapshot();
        type = string.IsNullOrWhiteSpace(type) ? "RoundTable" : type;
        var tableCount = RoomLayoutItems.Count(x => x.ItemType.EndsWith("Table", StringComparison.Ordinal));
        var item = new BanquetRoomLayoutItemVm
        {
            ItemType = type,
            Name = type switch { "Stage" => "Sân khấu", "Entrance" => "Cửa vào", "Aisle" => "Lối đi", "RectangleTable" => $"Bàn {tableCount + 1}", _ => $"Bàn {tableCount + 1}" },
            SeatCount = type.EndsWith("Table", StringComparison.Ordinal) ? 10 : 0,
            X = 28 + (tableCount % 6) * 112,
            Y = 72 + (tableCount / 6) * 104,
            Width = type switch { "Stage" => 210, "Aisle" => 260, "Entrance" => 84, _ => 82 },
            Height = type switch { "Stage" => 58, "Aisle" => 34, "Entrance" => 44, _ => 82 }
        };
        RoomLayoutItems.Add(item);
        SelectedRoomLayoutItem = item;
        NotifyRoomLayoutSummaryChanged();
        RoomLayoutIsDirty = true;
    }

    [RelayCommand]
    private void RemoveRoomLayoutItem(BanquetRoomLayoutItemVm? item)
    {
        item ??= SelectedRoomLayoutItem;
        if (item is null) return;
        CaptureRoomLayoutSnapshot();
        RoomLayoutItems.Remove(item);
        SelectedRoomLayoutItem = null;
        NotifyRoomLayoutSummaryChanged();
        RoomLayoutIsDirty = true;
    }

    [RelayCommand]
    private void AutoArrangeRoomLayout()
    {
        CaptureRoomLayoutSnapshot();
        var tables = RoomLayoutItems.Where(x => x.ItemType.EndsWith("Table", StringComparison.Ordinal)).ToList();
        for (var index = 0; index < tables.Count; index++)
        {
            tables[index].X = 36 + (index % 6) * 112;
            tables[index].Y = 92 + (index / 6) * 104;
        }
        RoomLayoutIsDirty = true;
    }

    [RelayCommand]
    private void GenerateRoomLayout()
    {
        CaptureRoomLayoutSnapshot();
        var rows = Math.Clamp(RoomLayoutRows, 1, 10);
        var tablesPerRow = Math.Clamp(RoomLayoutTablesPerRow, 2, 12);
        var seats = Math.Clamp(RoomLayoutSeatsPerTable, 2, 20);
        var tableSize = 54d;
        var startY = 112d;
        var usableWidth = 700d;
        var halfCount = (int)Math.Ceiling(tablesPerRow / 2d);
        var sideWidth = (usableWidth - RoomLayoutAisleWidth) / 2d;
        var stepX = Math.Max(tableSize + 5, (sideWidth - tableSize) / Math.Max(1, halfCount - 1));
        var stepY = tableSize + Math.Clamp(RoomLayoutVerticalGap, 4, 42);
        var leftStart = 30d;
        var rightStart = 30d + sideWidth + RoomLayoutAisleWidth;

        RoomLayoutItems.Clear();
        RoomLayoutItems.Add(new BanquetRoomLayoutItemVm
        {
            ItemType = "Stage", Name = "SÂN KHẤU", SeatCount = 0,
            X = 230, Y = 22, Width = 300, Height = 62
        });
        RoomLayoutItems.Add(new BanquetRoomLayoutItemVm
        {
            ItemType = "Aisle", Name = "LỐI ĐI", SeatCount = 0,
            X = 30 + sideWidth, Y = 102, Width = RoomLayoutAisleWidth,
            Height = Math.Min(400, rows * stepY + 12)
        });

        var number = 1;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < tablesPerRow; column++)
            {
                var sideColumn = column < halfCount ? column : column - halfCount;
                var x = column < halfCount
                    ? leftStart + sideColumn * stepX
                    : rightStart + sideColumn * stepX;
                RoomLayoutItems.Add(new BanquetRoomLayoutItemVm
                {
                    ItemType = "RoundTable",
                    Name = RoomLayoutShowNumbers ? $"Bàn {number}" : "Bàn",
                    SeatCount = seats,
                    X = x,
                    Y = startY + row * stepY,
                    Width = tableSize,
                    Height = tableSize
                });
                number++;
            }
        }
        RoomMaxTables = rows * tablesPerRow;
        RoomCapacity = RoomMaxTables * seats;
        SelectedRoomLayoutItem = RoomLayoutItems.FirstOrDefault();
        NotifyRoomLayoutSummaryChanged();
        StatusText = $"Đã tạo sơ đồ {RoomMaxTables} bàn, sức chứa {RoomCapacity} khách.";
        RoomLayoutIsDirty = true;
    }

    [RelayCommand]
    private void ChangeRoomMapZoom(string? direction)
    {
        RoomMapZoom = direction == "In"
            ? Math.Min(1.5, RoomMapZoom + 0.1)
            : direction == "Reset" ? 1 : Math.Max(0.6, RoomMapZoom - 0.1);
    }

    [RelayCommand]
    private void UndoRoomLayout()
    {
        if (_roomLayoutUndoStack.Count == 0) return;
        _roomLayoutRedoStack.Add(SerializeRoomLayout());
        var snapshot = _roomLayoutUndoStack[^1];
        _roomLayoutUndoStack.RemoveAt(_roomLayoutUndoStack.Count - 1);
        RestoreRoomLayoutSnapshot(snapshot);
        NotifyRoomLayoutHistoryChanged();
    }

    [RelayCommand]
    private void RedoRoomLayout()
    {
        if (_roomLayoutRedoStack.Count == 0) return;
        _roomLayoutUndoStack.Add(SerializeRoomLayout());
        var snapshot = _roomLayoutRedoStack[^1];
        _roomLayoutRedoStack.RemoveAt(_roomLayoutRedoStack.Count - 1);
        RestoreRoomLayoutSnapshot(snapshot);
        NotifyRoomLayoutHistoryChanged();
    }

    [RelayCommand]
    private void DuplicateRoomLayoutItem()
    {
        var source = SelectedRoomLayoutItem;
        if (source is null) return;
        CaptureRoomLayoutSnapshot();
        var copy = new BanquetRoomLayoutItemVm
        {
            ItemType = source.ItemType, Name = source.ItemType.EndsWith("Table", StringComparison.Ordinal)
                ? $"Bàn {RoomLayoutTableCount + 1}" : $"{source.Name} - bản sao",
            SeatCount = source.SeatCount, X = Math.Min(760 - source.Width, source.X + 16),
            Y = Math.Min(520 - source.Height, source.Y + 16), Width = source.Width,
            Height = source.Height, Rotation = source.Rotation, Note = source.Note
        };
        RoomLayoutItems.Add(copy);
        SelectedRoomLayoutItem = copy;
        CommitRoomLayoutChange();
    }

    [RelayCommand]
    private void RotateRoomLayoutItem(string? direction)
    {
        if (SelectedRoomLayoutItem is null) return;
        CaptureRoomLayoutSnapshot();
        var delta = direction == "Left" ? -15 : 15;
        SelectedRoomLayoutItem.Rotation = (SelectedRoomLayoutItem.Rotation + delta + 360) % 360;
        CommitRoomLayoutChange();
    }

    [RelayCommand]
    private void AlignRoomLayoutItem(string? alignment)
    {
        if (SelectedRoomLayoutItem is null) return;
        CaptureRoomLayoutSnapshot();
        SelectedRoomLayoutItem.X = alignment switch
        {
            "Left" => 20,
            "Center" => (760 - SelectedRoomLayoutItem.Width) / 2,
            "Right" => 740 - SelectedRoomLayoutItem.Width,
            _ => SelectedRoomLayoutItem.X
        };
        CommitRoomLayoutChange();
    }

    [RelayCommand]
    private void ClearRoomLayout()
    {
        if (RoomLayoutItems.Count == 0) return;
        CaptureRoomLayoutSnapshot();
        RoomLayoutItems.Clear();
        SelectedRoomLayoutItem = null;
        CommitRoomLayoutChange();
    }

    public void NudgeSelectedRoomLayoutItem(double deltaX, double deltaY)
    {
        if (SelectedRoomLayoutItem is null) return;
        CaptureRoomLayoutSnapshot();
        SelectedRoomLayoutItem.X = Math.Clamp(SelectedRoomLayoutItem.X + deltaX, 0, 760 - SelectedRoomLayoutItem.Width);
        SelectedRoomLayoutItem.Y = Math.Clamp(SelectedRoomLayoutItem.Y + deltaY, 0, 520 - SelectedRoomLayoutItem.Height);
        CommitRoomLayoutChange();
    }

    public double SnapRoomLayoutCoordinate(double value)
        => RoomLayoutSnapToGrid ? Math.Round(value / Math.Max(2, RoomLayoutGridSize)) * Math.Max(2, RoomLayoutGridSize) : value;
    private bool _suppressSelectedEventChanged;
    private int _selectedEventDetailLoadVersion;
    private bool _restoringEventSnapshot;
    private bool _isSavingEvent;
    private bool _isAddingDeposit;
    private readonly SemaphoreSlim _menuPackageOperationGate = new(1, 1);
    private readonly SemaphoreSlim _banquetCatalogLoadGate = new(1, 1);
    private BanquetEventEditorSnapshot? _undoEventSnapshot;
    private BanquetEventEditorSnapshot? _redoEventSnapshot;

    public ObservableCollection<BanquetServiceLineDto> EventServices { get; } = new();
    public ObservableCollection<BanquetDepositDto> EventDeposits { get; } = new();
    public ObservableCollection<BanquetContractDto> EventContracts { get; } = new();
    public IReadOnlyList<LookupOption<string>> ContractStatusOptions { get; } = new LookupOption<string>[]
    {
        new("Bản nháp", "Draft"), new("Đã xác nhận", "Confirmed"), new("Đã hoàn thành", "Completed"), new("Đã hủy", "Cancelled")
    };

    [ObservableProperty] private BanquetContractDto? selectedContract;
    [ObservableProperty] private string contractAdditionalTerms = string.Empty;
    [ObservableProperty] private string contractStatus = "Confirmed";
    [ObservableProperty] private string contractPreviewText = "Chọn hoặc tạo hợp đồng để xem nội dung.";
    public ObservableCollection<BanquetEventMenuDto> EventMenus { get; } = new();
    public ObservableCollection<BanquetMenuGroupDto> BanquetMenuGroups { get; } = new();
    public ObservableCollection<BanquetSelectedFoodItemVm> SelectedBanquetCustomFoods { get; } = new();
    public ObservableCollection<BanquetTimelineItemDto> EventTimelineItems { get; } = new();
    public ObservableCollection<BanquetWorkflowStepVm> EventWorkflowSteps { get; } = new();
    public ObservableCollection<BanquetEventDto> HoldRoomEvents { get; } = new();

    public ObservableCollection<BanquetEventDto> FilteredEvents { get; } = new();
    public ObservableCollection<BanquetEventDto> DashboardTodayEvents { get; } = new();
    public ObservableCollection<BanquetEventDto> DashboardActionEvents { get; } = new();
    public ObservableCollection<BanquetAlertVm> DashboardAlerts { get; } = new();
    public ObservableCollection<BanquetRevenueBarVm> RevenueChartBars { get; } = new();
    public ObservableCollection<BanquetCustomerSummaryVm> CustomerSummaries { get; } = new();
    public ObservableCollection<BanquetContractSummaryVm> ContractSummaries { get; } = new();
    public ObservableCollection<BanquetFinanceSummaryVm> FinanceSummaries { get; } = new();
    public ObservableCollection<BanquetChecklistGroupVm> ChecklistGroups { get; } = new();
    public ObservableCollection<BanquetChecklistDisplayVm> OverviewChecklistPreviewItems { get; } = new();
    public ObservableCollection<BanquetMilestoneVm> TimelineMilestones { get; } = new();
    public ObservableCollection<BanquetScheduleColumnVm> OverviewCalendarColumns { get; } = new();
    public ObservableCollection<BanquetScheduleRowVm> OverviewCalendarRows { get; } = new();
    public ObservableCollection<BanquetRoomCalendarDayVm> RoomCalendarDays { get; } = new();
    public ObservableCollection<BanquetRoomCalendarRowVm> RoomCalendarRows { get; } = new();
    public ObservableCollection<BanquetYearMonthVm> RoomCalendarYearMonths { get; } = new();
    public ObservableCollection<LookupOption<Guid?>> RoomCalendarRoomFilterOptions { get; } = new();
    public ObservableCollection<BanquetEventDto> ReportEvents { get; } = new();
    public ObservableCollection<BanquetReportTrendVm> ReportTrends { get; } = new();
    public ObservableCollection<BanquetReportRoomVm> ReportRooms { get; } = new();
    public ObservableCollection<BanquetReportTypeVm> ReportTypes { get; } = new();
    public ObservableCollection<LookupOption<Guid?>> EventRoomFilterOptions { get; } = new();
    public TablePagerViewModel<BanquetRoomDto> RoomsPager { get; } = new() { ItemName = "phòng" };
    public TablePagerViewModel<BanquetEventDto> EventsPager { get; } = new() { ItemName = "sự kiện" };

    public IReadOnlyList<LookupOption<string>> EventTypeOptions { get; } = new LookupOption<string>[]
    {
        new("Tiệc Cưới", "Wedding"), new("Hội nghị", "Conference"), new("Sinh nhật", "Birthday"),
        new("Họp mặt", "Meeting"), new("Tiệc / Party", "Party"), new("Sự kiện khác", "Other")
    };
    public IReadOnlyList<LookupOption<string>> EventStatusOptions { get; } = new LookupOption<string>[]
    {
        new("Nháp", "Draft"), new("Giữ chỗ", "Hold"), new("Đã xác nhận", "Confirmed"),
        new("Đang chuẩn bị", "Preparing"), new("Hoàn thành", "Completed"), new("Đã hủy", "Cancelled")
    };
    public IReadOnlyList<LookupOption<string>> ServiceTypeOptions { get; } = new LookupOption<string>[]
    {
        new("MC", "MC"), new("Ca sĩ", "Singer"), new("Màn hình LED", "LedScreen"),
        new("Trang trí", "Decoration"), new("Âm thanh & Ánh sáng", "SoundLight"),
        new("Cổng chào", "Gateway"), new("Chụp ảnh", "PhotoBooth"),
        new("Vận chuyển", "Transport"), new("Khác", "Other")
    };
    public IReadOnlyList<LookupOption<string>> PaymentMethodOptions { get; } = new LookupOption<string>[]
    {
        new("Tiền mặt", "Cash"), new("Chuyển khoản", "Transfer"), new("Thẻ", "Card"), new("Công nợ", "Debt")
    };
    public IReadOnlyList<string> ChecklistDepartmentOptions { get; } =
        ["Kinh doanh", "Bếp", "Kỹ thuật", "Trang trí", "Phục vụ", "Thu ngân"];
    public IReadOnlyList<LookupOption<string?>> EventStatusFilterOptions { get; } = new LookupOption<string?>[]
    {
        new("Tất cả", null),
        new("Nháp", "Draft"), new("Giữ chỗ", "Hold"), new("Đã xác nhận", "Confirmed"),
        new("Đang chuẩn bị", "Preparing"), new("Hoàn thành", "Completed"), new("Đã hủy", "Cancelled")
    };
    public IReadOnlyList<LookupOption<string?>> EventTypeFilterOptions { get; } = new LookupOption<string?>[]
    {
        new("Tất cả", null),
        new("Tiệc Cưới", "Wedding"), new("Hội nghị", "Conference"), new("Sinh nhật", "Birthday"),
        new("Họp mặt", "Meeting"), new("Tiệc / Party", "Party"), new("Sự kiện khác", "Other")
    };
    public IReadOnlyList<LookupOption<string?>> RoomCalendarStatusFilterOptions { get; } = new LookupOption<string?>[]
    {
        new("Tất cả trạng thái", null), new("Giữ chỗ", "Hold"), new("Đã xác nhận", "Confirmed"),
        new("Đang chuẩn bị", "Preparing"), new("Hoàn thành", "Completed")
    };
    public IReadOnlyList<LookupOption<string?>> RoomCalendarShiftFilterOptions { get; } = new LookupOption<string?>[]
    {
        new("Cả hai ca", null), new("Ca Trưa", "Lunch"), new("Ca Tối", "Dinner")
    };
    public IReadOnlyList<LookupOption<string>> ReportPeriodOptions { get; } = new LookupOption<string>[]
    {
        new("Tháng này", "ThisMonth"), new("Quý này", "ThisQuarter"),
        new("Năm nay", "ThisYear"), new("12 tháng gần nhất", "Last12Months"),
        new("Tùy chọn", "Custom")
    };

    [ObservableProperty] private Guid? selectedEventRoomId;
    [ObservableProperty] private string eventNote = string.Empty;
    [ObservableProperty] private string eventStatus = "Confirmed";
    [ObservableProperty] private string depositPaymentMethod = "Cash";
    [ObservableProperty] private string depositReceiver = string.Empty;
    [ObservableProperty] private string depositNote = string.Empty;
    [ObservableProperty] private string depositValidationMessage = string.Empty;
    [ObservableProperty] private string depositSuccessMessage = string.Empty;
    [ObservableProperty] private DateTime contractSignDate = DateTime.Today;
    [ObservableProperty] private string serviceType = "Other";
    [ObservableProperty] private string serviceDescription = string.Empty;
    [ObservableProperty] private string serviceUnitName = "lần";
    [ObservableProperty] private decimal serviceQty = 1m;
    [ObservableProperty] private decimal serviceUnitPrice;
    [ObservableProperty] private decimal serviceCostPrice;
    [ObservableProperty] private decimal serviceVatRate = 8m;
    [ObservableProperty] private string serviceNote = string.Empty;
    [ObservableProperty] private BanquetMenuPackageDto? selectedMenuPackage;
    [ObservableProperty] private DateTime holdCalendarDate = DateTime.Today;
    [ObservableProperty] private DateTime roomCalendarWeekStart = GetRoomCalendarWeekStart(DateTime.Today);
    [ObservableProperty] private int roomCalendarViewMode;
    [ObservableProperty] private int roomCalendarYear = DateTime.Today.Year;
    [ObservableProperty] private Guid? selectedRoomCalendarRoomId;
    [ObservableProperty] private string? selectedRoomCalendarStatus;
    [ObservableProperty] private string? selectedRoomCalendarEventType;
    [ObservableProperty] private string? selectedRoomCalendarShift;
    [ObservableProperty] private int reportViewMode;
    [ObservableProperty] private DateTime reportFromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime reportToDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);
    [ObservableProperty] private string reportPeriodPreset = "ThisMonth";
    [ObservableProperty] private Guid? selectedReportRoomId;
    [ObservableProperty] private string? selectedReportEventType;
    [ObservableProperty] private string? selectedReportStatus;
    [ObservableProperty] private BanquetEventDto? selectedReportEventRow;
    [ObservableProperty] private string reportSearchText = string.Empty;
    [ObservableProperty] private string reportStatusText = "Dữ liệu được tổng hợp từ các sự kiện đã lưu.";
    [ObservableProperty] private string banquetVenueName = "Nhà hàng Tiệc (Banquet)";
    [ObservableProperty] private string banquetVenueCode = "BNQ001";
    [ObservableProperty] private string banquetVenueAddress = string.Empty;
    [ObservableProperty] private string banquetVenuePhone = string.Empty;
    [ObservableProperty] private string banquetVenueEmail = string.Empty;
    [ObservableProperty] private string banquetVenueWebsite = string.Empty;
    [ObservableProperty] private string banquetVenueTaxCode = string.Empty;
    [ObservableProperty] private int banquetGuestsPerTable = 10;
    [ObservableProperty] private decimal banquetDefaultDepositRate = 30m;
    [ObservableProperty] private decimal banquetDefaultVatRate = 8m;
    [ObservableProperty] private int banquetHoldDays = 7;
    [ObservableProperty] private int banquetReminderDays = 3;
    [ObservableProperty] private string banquetLunchStartTime = "11:00";
    [ObservableProperty] private string banquetDinnerStartTime = "18:00";
    [ObservableProperty] private bool banquetAutoGenerateEventNo = true;
    [ObservableProperty] private bool banquetPreventRoomConflict = true;
    [ObservableProperty] private bool banquetEnableDepositReminder = true;
    [ObservableProperty] private bool banquetShowCompletedEvents = true;
    [ObservableProperty] private int banquetSettingsSection;
    [ObservableProperty] private string banquetSettingsValidationMessage = string.Empty;
    [ObservableProperty] private string banquetSettingsSavedText = "Chưa có thay đổi mới.";
    [ObservableProperty] private string newChecklistTitle = string.Empty;
    [ObservableProperty] private string newChecklistNote = string.Empty;
    [ObservableProperty] private string newChecklistDepartment = "Kinh doanh";
    [ObservableProperty] private Guid? editingChecklistItemId;
    [ObservableProperty] private string timelineStartTimeText = "17:00";
    [ObservableProperty] private string timelineTitle = string.Empty;
    [ObservableProperty] private string timelineDescription = string.Empty;
    [ObservableProperty] private string timelineResponsiblePerson = string.Empty;
    [ObservableProperty] private Guid? editingTimelineItemId;
    [ObservableProperty] private string customerCareNote = string.Empty;
    [ObservableProperty] private string liquidationNote = string.Empty;
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private string globalSearchText = string.Empty;

    public int ChecklistItemCount => ChecklistItems.Count;
    public int ChecklistCompletedCount => ChecklistItems.Count(x => x.IsDone);
    public int ChecklistPendingCount => ChecklistItemCount - ChecklistCompletedCount;
    public int ChecklistOverdueCount => ChecklistGroups.SelectMany(x => x.Items).Count(x => x.IsOverdue);
    public double ChecklistProgressPercent => ChecklistItemCount == 0 ? 0d : ChecklistCompletedCount * 100d / ChecklistItemCount;
    public string ChecklistProgressText => ChecklistItemCount == 0
        ? "Chưa có công việc"
        : $"{ChecklistCompletedCount}/{ChecklistItemCount} công việc hoàn thành";
    public bool HasChecklistItems => ChecklistItemCount > 0;
    public bool IsChecklistEmpty => !HasChecklistItems;
    public bool IsEditingChecklistItem => EditingChecklistItemId.HasValue;
    public string ChecklistFormTitle => IsEditingChecklistItem ? "Chỉnh sửa công việc" : "Thêm công việc";
    public string ChecklistSaveButtonText => IsEditingChecklistItem ? "Lưu thay đổi" : "Thêm công việc";

    public int TimelineItemCount => EventTimelineItems.Count;
    public int TimelineCompletedCount => EventTimelineItems.Count(x => string.Equals(x.Status, "Done", StringComparison.OrdinalIgnoreCase));
    public int TimelinePendingCount => TimelineItemCount - TimelineCompletedCount;
    public double TimelineProgressPercent => TimelineItemCount == 0 ? 0d : TimelineCompletedCount * 100d / TimelineItemCount;
    public string TimelineProgressText => TimelineItemCount == 0
        ? "Chưa có mốc vận hành"
        : $"{TimelineCompletedCount}/{TimelineItemCount} mốc đã hoàn thành";
    public bool HasTimelineItems => TimelineItemCount > 0;
    public bool IsTimelineEmpty => !HasTimelineItems;
    public bool IsEditingTimelineItem => EditingTimelineItemId.HasValue;
    public string TimelineFormTitle => IsEditingTimelineItem ? "Chỉnh sửa mốc vận hành" : "Thêm mốc vận hành";
    public string TimelineSaveButtonText => IsEditingTimelineItem ? "Lưu thay đổi" : "Thêm mốc";

    // Event-specific fields
    [ObservableProperty] private int extraGuestCount;
    [ObservableProperty] private int extraTableCount;
    [ObservableProperty] private string brideName = string.Empty;
    [ObservableProperty] private string groomName = string.Empty;
    [ObservableProperty] private string conferenceName = string.Empty;
    [ObservableProperty] private string birthdayPersonName = string.Empty;
    [ObservableProperty] private DateTime? birthdayDate;
    [ObservableProperty] private string meetingTopic = string.Empty;
    [ObservableProperty] private string partyTheme = string.Empty;
    [ObservableProperty] private string otherEventName = string.Empty;

    // Menu mode
    [ObservableProperty] private int menuSelectionMode;
    public bool ShowMenuCards => MenuSelectionMode == 0;
    public bool ShowCustomMenu => MenuSelectionMode == 1;
    public bool ShowPresetMenus => MenuSelectionMode == 2;
    public bool ShowMenuModal => MenuSelectionMode != 0;

    public string EventTypeDisplayName => EventType switch { "Wedding" => "Tiệc Cưới", "Conference" => "Hội nghị", "Birthday" => "Sinh nhật", "Meeting" => "Họp mặt", "Party" => "Tiệc / Party", _ => "Sự kiện khác" };
    public bool IsWedding => EventType == "Wedding";
    public bool IsConference => EventType == "Conference";
    public bool IsBirthday => EventType == "Birthday";
    public bool IsMeeting => EventType == "Meeting";
    public bool IsParty => EventType == "Party";
    public bool IsOtherEvent => EventType == "Other";
    public string EventTypeSpecificLabel => EventType switch { "Wedding" => "Cô dâu — Chú rể", "Conference" => "Tên hội nghị", "Birthday" => "Người tổ chức sinh nhật", "Meeting" => "Chủ đề họp mặt", "Party" => "Chủ đề tiệc", "Other" => "Tên sự kiện", _ => "Thông tin bổ sung" };

    partial void OnEventTypeChanged(string value)
    {
        OnPropertyChanged(nameof(EventTypeDisplayName));
        OnPropertyChanged(nameof(EventTypeSpecificLabel));
        OnPropertyChanged(nameof(IsWedding)); OnPropertyChanged(nameof(IsConference));
        OnPropertyChanged(nameof(IsBirthday)); OnPropertyChanged(nameof(IsMeeting));
        OnPropertyChanged(nameof(IsParty)); OnPropertyChanged(nameof(IsOtherEvent));
    }
    partial void OnMenuSelectionModeChanged(int value)
    {
        OnPropertyChanged(nameof(ShowMenuCards)); OnPropertyChanged(nameof(ShowCustomMenu));
        OnPropertyChanged(nameof(ShowPresetMenus)); OnPropertyChanged(nameof(ShowMenuModal));
    }

    [RelayCommand] private void OpenCustomMenu()
    {
        try
        {
            MenuSelectionMode = 1; OnPropertyChanged(nameof(ShowMenuModal));
            var vm = new BanquetMenuSelectionViewModel(this, true);
            var window = _windowManager.OpenOrActivate(new AppModuleInfo("banquet-menu-custom", "Chọn món theo yêu cầu", "", "Danh sách món ăn", "#EA580C", false, false), () => vm);
            WatchWindowClose(window);
        }
        catch (Exception ex) { StatusText = $"Lỗi modal: {ex.Message}"; MenuSelectionMode = 0; OnPropertyChanged(nameof(ShowMenuModal)); }
    }
    [RelayCommand] private async Task OpenPresetMenus()
    {
        await LoadMenuPackagesIfNeededAsync();
        if (BanquetMenuGroups.Count == 0) Reset(BanquetMenuGroups, await _service.GetMenuGroupsAsync());
        await EnsureMenuItemsLoadedAsync();
        MenuSelectionMode = 2; OnPropertyChanged(nameof(ShowMenuModal));
        var vm = new BanquetMenuSelectionViewModel(this, false);
        var window = _windowManager.OpenOrActivate(new AppModuleInfo("banquet-menu-preset", "Chọn thực đơn có sẵn", "", "Gói thực đơn", "#16A34A", false, false), () => vm);
        WatchWindowClose(window);
    }

    private void WatchWindowClose(AppWindowViewModel window)
    {
        System.Collections.Specialized.NotifyCollectionChangedEventHandler? handler = null;
        handler = (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems?.Contains(window) == true)
            {
                _windowManager.OpenWindows.CollectionChanged -= handler;
                MenuSelectionMode = 0; OnPropertyChanged(nameof(ShowMenuModal));
            }
        };
        _windowManager.OpenWindows.CollectionChanged += handler;
    }

    public void CloseMenuSelection(bool fromSave)
    {
        var window = _windowManager.OpenWindows.FirstOrDefault(x =>
            x.Module.Key == "banquet-menu-custom" || x.Module.Key == "banquet-menu-preset");
        if (window is not null)
        {
            _windowManager.Close(window);
        }
        else
        {
            MenuSelectionMode = 0;
            OnPropertyChanged(nameof(ShowMenuModal));
        }

        if (fromSave)
        {
            StatusText = SelectedBanquetCustomFoods.Count > 0
                ? $"Đã lưu {SelectedBanquetCustomFoods.Count} món đã chọn."
                : "Đã lưu thực đơn.";
        }
    }

    public async Task ApplyCustomMenuSelectionAsync(IEnumerable<SelectedFoodItem> selectedItems)
    {
        var rows = selectedItems
            .OrderBy(x => x.LineNo)
            .Select(x => new BanquetSelectedFoodItemVm
            {
                LineNo = x.LineNo,
                FoodItem = x.FoodItem,
                Quantity = x.Quantity
            })
            .ToArray();

        Reset(SelectedBanquetCustomFoods, rows);
        OnPropertyChanged(nameof(HasSelectedBanquetCustomFoods));
        NotifyCustomFoodAmountsChanged();
        if (SelectedEvent is null)
        {
            StatusText = "Lưu sự kiện trước khi lưu thực đơn tùy chỉnh.";
            return;
        }
        await RunBusyAsync(async () =>
        {
            await _service.SaveCustomMenuAsync(new SaveBanquetCustomMenuRequest
            {
                BanquetEventId = SelectedEvent.Id,
                Lines = rows.Select(x => new SaveBanquetCustomMenuLineRequest { MenuItemId = x.FoodItem.Id, ItemName = x.FoodItem.Name, Qty = x.Quantity, UnitPrice = x.FoodItem.UnitPrice }).ToArray()
            });
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            StatusText = $"Đã lưu {rows.Length} món tùy chỉnh.";
        });
    }

    [RelayCommand] private void BackToMenuCards() { MenuSelectionMode = 0; }
    [RelayCommand] private void SelectEventType(string eventType) { EventType = eventType; SelectedDetailTabIndex = 1; }

    [RelayCommand]
    internal async Task SeedSampleMenuPackagesAsync()
    {
        if (MenuPackages.Count > 0) { StatusText = "Đã có gói thực đơn."; return; }
        await RunBusyAsync(async () =>
        {
            await _service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest { Code = "TD-TC01", Name = "Tiệc cưới tiêu chuẩn", PricingMode = "PerTable", Price = 2500000m, VatRate = 8m, Lines = [new SaveBanquetMenuPackageLineRequest { MenuItemId = BanquetFoodItems.FirstOrDefault()?.Id ?? Guid.Empty, CourseType = "Main", Qty = 1 }] });
            await _service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest { Code = "TD-TC02", Name = "Tiệc cưới cao cấp", PricingMode = "PerTable", Price = 4200000m, VatRate = 8m, Lines = [new SaveBanquetMenuPackageLineRequest { MenuItemId = BanquetFoodItems.Skip(1).FirstOrDefault()?.Id ?? Guid.Empty, CourseType = "Main", Qty = 1 }] });
            await _service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest { Code = "TD-HN01", Name = "Hội nghị trọn gói", PricingMode = "PerTable", Price = 1800000m, VatRate = 8m });
            Reset(MenuPackages, await _service.GetMenuPackagesAsync());
            StatusText = "Đã tạo 3 gói thực đơn mẫu.";
        });
    }

    internal async Task<BanquetMenuPackageDto> SaveMenuPackageFromModalAsync(Guid? id, string code, string name, string? groupName, decimal price, decimal vatRate, SaveBanquetMenuPackageLineRequest[] lines)
    {
        await _menuPackageOperationGate.WaitAsync();
        try
        {
            while (IsBusy) await Task.Delay(50);
            IsBusy = true;
            var saved = await _service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
            {
                Id = id,
                Code = code,
                Name = name,
                GroupName = groupName,
                PricingMode = "PerTable",
                Price = price,
                VatRate = vatRate,
                Lines = lines
            });
            Reset(MenuPackages, await _service.GetMenuPackagesAsync());
            StatusText = $"Đã lưu gói thực đơn {name}.";
            return saved;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Save banquet menu package failed");
            StatusText = $"Không lưu được thực đơn: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
            _menuPackageOperationGate.Release();
        }
    }

    internal async Task<BanquetMenuGroupDto> SaveMenuGroupFromModalAsync(string name)
    {
        await _menuPackageOperationGate.WaitAsync();
        try
        {
            while (IsBusy) await Task.Delay(50);
            IsBusy = true;
            var saved = await _service.SaveMenuGroupAsync(new SaveBanquetMenuGroupRequest
            {
                Name = name,
                SortOrder = BanquetMenuGroups.Count + 1
            });
            Reset(BanquetMenuGroups, await _service.GetMenuGroupsAsync());
            StatusText = $"Đã tạo nhóm thực đơn {saved.Name}.";
            return saved;
        }
        finally
        {
            IsBusy = false;
            _menuPackageOperationGate.Release();
        }
    }

    private async Task LoadMenuPackagesIfNeededAsync()
    {
        if (MenuPackages.Count == 0) Reset(MenuPackages, await _service.GetMenuPackagesAsync());
    }

    [RelayCommand]
    private async Task AddBanquetFoodToEventAsync(PosMenuItemCard? item)
    {
        if (item is null || SelectedEvent is null) return;
        ServiceDescription = item.Name; ServiceUnitPrice = item.UnitPrice;
        ServiceType = "Other"; ServiceQty = 1; ServiceNote = string.Empty;
        await SaveServiceLineAsync();
    }
    [ObservableProperty] private string banquetMenuSearchText = string.Empty;
    [ObservableProperty] private Guid? selectedBanquetMenuCategoryId;
    [ObservableProperty] private string banquetServiceSearchText = string.Empty;
    [ObservableProperty] private string? selectedBanquetServiceType;
    [ObservableProperty] private int banquetCatalogColumnCount = 4;

    public ObservableCollection<MenuCategoryDto> BanquetMenuCategories { get; } = new();
    public ObservableCollection<PosMenuItemCard> BanquetFoodItems { get; } = new();
    public ObservableCollection<PosMenuItemCard> BanquetFilteredFoodItems { get; } = new();
    public ObservableCollection<BanquetServiceCard> BanquetServiceItems { get; } = new();
    public ObservableCollection<BanquetServiceCard> BanquetFilteredServiceItems { get; } = new();
    public ObservableCollection<string> BanquetServiceTypes { get; } = new();
    public ObservableCollection<int> BanquetCatalogColumnOptions { get; } = new([3, 4, 5, 6]);

    private static readonly Dictionary<string, string> ServiceTypeNames = new()
    {
        ["Equipment"] = "Thiết bị / sân khấu",
        ["Staff"] = "Nhân sự biểu diễn",
        ["Decor"] = "Trang trí",
        ["Fee"] = "Phụ thu / phụ phí",
        ["MC"] = "MC",
        ["Singer"] = "Ca sĩ",
        ["LedScreen"] = "Màn hình LED",
        ["Decoration"] = "Trang trí",
        ["SoundLight"] = "Âm thanh & Ánh sáng",
        ["Gateway"] = "Cổng chào",
        ["PhotoBooth"] = "Chụp ảnh",
        ["Transport"] = "Vận chuyển",
        ["Other"] = "Khác"
    };

    public static string GetServiceTypeDisplayName(string? serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType)) return "Khác";
        return ServiceTypeNames.TryGetValue(serviceType, out var name) ? name : serviceType;
    }

    public string CurrentBreadcrumb => SelectedTabIndex switch
    {
        0 => "Tổng quan",
        1 => "Lịch phòng",
        2 => "Cài đặt",
        3 => SelectedEvent?.CustomerName ?? "Sự kiện",
        4 => "Vận hành",
        6 => "Báo cáo",
        7 => "Phòng tiệc",
        _ => "Nhà hàng Tiệc"
    };

    [ObservableProperty] private string eventSearchText = string.Empty;
    [ObservableProperty] private string? selectedEventStatusFilter;
    [ObservableProperty] private string? selectedEventTypeFilter;
    [ObservableProperty] private Guid? selectedEventRoomFilterId;
    [ObservableProperty] private DateTime eventFilterFromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime eventFilterToDate = DateTime.Today.AddMonths(6);

    public bool HasSelectedEvent => SelectedEvent is not null;
    public bool CanSaveEvent => !_isSavingEvent;
    public bool HasSelectedBanquetCustomFoods => SelectedBanquetCustomFoods.Count > 0;
    public decimal SelectedBanquetCustomFoodTotalAmount => SelectedBanquetCustomFoods.Sum(x => x.Amount);
    public decimal SelectedBanquetCustomFoodMainTableAmount => SelectedBanquetCustomFoodTotalAmount * Math.Max(0, TableCount);
    public decimal SelectedBanquetCustomFoodProjectedTableAmount => SelectedBanquetCustomFoodTotalAmount * Math.Max(0, ExtraTableCount);
    public decimal SelectedBanquetServiceTotalAmount => EventServices.Sum(x => x.Amount);
    public decimal FinanceGrandTotal => SelectedEvent?.TotalAmount ?? 0m;
    public decimal FinanceMenuTotalAmount => SelectedEvent?.MenuAmount ?? 0m;
    public decimal FinanceMenuMainAmount => EventMenus.Sum(x => x.PricingMode == "Custom"
        ? x.Amount * Math.Max(0, TableCount)
        : x.PricingMode == "PerGuest" ? x.Amount : x.UnitPrice * Math.Max(0, TableCount));
    public decimal FinanceMenuExtraAmount => Math.Max(0m, FinanceMenuTotalAmount - FinanceMenuMainAmount);
    public decimal FinanceServiceBaseAmount => EventServices.Sum(x => x.Qty * x.UnitPrice);
    public decimal FinanceVatAmount => Math.Max(0m, EventServices.Sum(x => x.Amount) - FinanceServiceBaseAmount);
    public decimal FinanceSurchargeAmount => 0m;
    public decimal FinanceDiscountAmount => 0m;
    public decimal FinanceDepositedAmount => EventDeposits.Sum(x => x.NetAmount);
    public decimal FinanceRemainingAmount => Math.Max(0m, FinanceGrandTotal - FinanceDepositedAmount);
    public decimal FinanceRefundAmount => Math.Max(0m, FinanceDepositedAmount - FinanceGrandTotal);
    public double FinancePaymentProgress => FinanceGrandTotal <= 0m ? 0d : Math.Clamp((double)(FinanceDepositedAmount / FinanceGrandTotal * 100m), 0d, 100d);
    public int FinanceDepositCount => EventDeposits.Count(x => x.NetAmount > 0m);
    public bool HasFinanceDeposits => EventDeposits.Count > 0;
    public bool HasNoFinanceDeposits => !HasFinanceDeposits;
    public bool IsFinanceOverpaid => FinanceRefundAmount > 0m;
    public bool CanAddDeposit => !_isAddingDeposit && SelectedEvent is not null && string.IsNullOrEmpty(DepositValidationMessage);
    public string FinancePaymentStatus => SelectedEvent?.Status == "Cancelled" ? "Đã hủy"
        : IsFinanceOverpaid ? "Đã thu vượt"
        : FinanceDepositedAmount <= 0m ? "Chưa đặt cọc"
        : FinanceRemainingAmount <= 0m ? "Đã thanh toán"
        : "Đã đặt cọc một phần";
    public string FinanceMenuDescription => $"{Math.Max(0, TableCount)} bàn chính × đơn giá thực đơn";
    public string FinanceExtraMenuDescription => $"{Math.Max(0, ExtraTableCount)} bàn phát sinh";
    public string FinanceServiceDescription => EventServices.Count == 0 ? "Chưa chọn dịch vụ" : string.Join(", ", EventServices.Take(3).Select(x => x.Description)) + (EventServices.Count > 3 ? "..." : string.Empty);

    partial void OnDepositAmountChanged(decimal value) => ValidateDepositForm();
    partial void OnDepositPaymentMethodChanged(string value) => ValidateDepositForm();

    private void ValidateDepositForm()
    {
        DepositValidationMessage = SelectedEvent is null ? "Vui lòng lưu thông tin sự kiện trước khi ghi nhận đặt cọc."
            : SelectedEvent.Status is "Cancelled" or "Completed" ? "Không thể ghi nhận cọc cho sự kiện đã hủy hoặc đã hoàn tất."
            : DepositAmount <= 0m ? "Số tiền đặt cọc phải lớn hơn 0."
            : DepositAmount > FinanceRemainingAmount ? "Số tiền đặt cọc không được vượt số tiền còn phải thu."
            : string.IsNullOrWhiteSpace(DepositPaymentMethod) ? "Vui lòng chọn phương thức thanh toán."
            : string.Empty;
        OnPropertyChanged(nameof(CanAddDeposit));
    }

    private void NotifyFinanceSummaryChanged()
    {
        OnPropertyChanged(nameof(FinanceGrandTotal));
        OnPropertyChanged(nameof(FinanceMenuTotalAmount));
        OnPropertyChanged(nameof(FinanceMenuMainAmount));
        OnPropertyChanged(nameof(FinanceMenuExtraAmount));
        OnPropertyChanged(nameof(FinanceServiceBaseAmount));
        OnPropertyChanged(nameof(FinanceVatAmount));
        OnPropertyChanged(nameof(FinanceSurchargeAmount));
        OnPropertyChanged(nameof(FinanceDiscountAmount));
        OnPropertyChanged(nameof(FinanceDepositedAmount));
        OnPropertyChanged(nameof(FinanceRemainingAmount));
        OnPropertyChanged(nameof(FinanceRefundAmount));
        OnPropertyChanged(nameof(FinancePaymentProgress));
        OnPropertyChanged(nameof(FinanceDepositCount));
        OnPropertyChanged(nameof(HasFinanceDeposits));
        OnPropertyChanged(nameof(HasNoFinanceDeposits));
        OnPropertyChanged(nameof(IsFinanceOverpaid));
        OnPropertyChanged(nameof(FinancePaymentStatus));
        OnPropertyChanged(nameof(FinanceMenuDescription));
        OnPropertyChanged(nameof(FinanceExtraMenuDescription));
        OnPropertyChanged(nameof(FinanceServiceDescription));
        ValidateDepositForm();
    }

    [RelayCommand]
    private void SetDepositSuggestion(string? suggestion)
    {
        DepositAmount = suggestion switch
        {
            "30" => Math.Min(FinanceRemainingAmount, Math.Round(FinanceGrandTotal * 0.30m, 0, MidpointRounding.AwayFromZero)),
            "50" => Math.Min(FinanceRemainingAmount, Math.Round(FinanceGrandTotal * 0.50m, 0, MidpointRounding.AwayFromZero)),
            "Remaining" => FinanceRemainingAmount,
            _ => DepositAmount
        };
    }

    [RelayCommand]
    private void ResetDepositForm()
    {
        DepositAmount = 0m;
        DepositPaymentMethod = "Cash";
        DepositReceiver = string.Empty;
        DepositNote = string.Empty;
        DepositSuccessMessage = string.Empty;
        ValidateDepositForm();
    }

    private void NotifyCustomFoodAmountsChanged()
    {
        OnPropertyChanged(nameof(SelectedBanquetCustomFoodTotalAmount));
        OnPropertyChanged(nameof(SelectedBanquetCustomFoodMainTableAmount));
        OnPropertyChanged(nameof(SelectedBanquetCustomFoodProjectedTableAmount));
    }
    public decimal SelectedEventPaidAmount => EventDeposits.Sum(x => x.NetAmount);
    public decimal SelectedEventOutstandingAmount => SelectedEvent?.RemainingAmount ?? 0m;
    public decimal SelectedEventContractValue => SelectedEvent?.TotalAmount ?? 0m;
    public int HoldRoomEventCount => HoldRoomEvents.Count;
    public string HoldAvailabilityText => HoldRoomEvents.Count == 0
        ? "Ngày đang chọn còn trống."
        : $"Đã có {HoldRoomEvents.Count} lịch tiệc trong ngày.";
    public int UpcomingEventsCount => Events.Count(x => x.EventDate.Date > DateTime.Today && x.EventDate.Date <= DateTime.Today.AddDays(7) && !IsCancelled(x.Status));
    public decimal ForecastRevenue => Events.Where(x => x.EventDate.Date >= DateTime.Today && !IsCancelled(x.Status)).Sum(x => x.TotalAmount);
    public int ImportantAlertCount => DashboardAlerts.Count;
    public double MaxRevenueBarValue => Math.Max(1d, RevenueChartBars.Select(x => (double)x.Amount).DefaultIfEmpty(1d).Max());
    public string OverviewCalendarRangeText => $"{DateTime.Today:dd/MM/yyyy} - {DateTime.Today.AddDays(4):dd/MM/yyyy}";
    public string RoomCalendarRangeText => $"{RoomCalendarWeekStart:dd/MM/yyyy} - {RoomCalendarWeekStart.AddDays(6):dd/MM/yyyy}";
    public string RoomCalendarYearText => $"Năm {RoomCalendarYear}";
    public bool IsRoomCalendarWeekView => RoomCalendarViewMode == 0;
    public bool IsRoomCalendarYearView => RoomCalendarViewMode == 1;
    public bool IsReportOverviewView => ReportViewMode == 0;
    public bool IsReportEventsView => ReportViewMode == 1;
    public bool IsReportRoomsView => ReportViewMode == 2;
    public int ReportTotalEvents => ReportEvents.Count;
    public int ReportConfirmedEvents => ReportEvents.Count(x => NormalizeEventStatus(x.Status) == "Confirmed");
    public int ReportGuestCount => ReportEvents.Sum(x => x.GuestCount);
    public decimal ReportRevenue => ReportEvents.Sum(x => x.TotalAmount);
    public decimal ReportDepositAmount => ReportEvents.Sum(x => x.DepositAmount);
    public decimal ReportOutstandingAmount => ReportEvents.Sum(x => x.RemainingAmount);
    public decimal ReportEstimatedCost => ReportEvents.Sum(x => x.EstimatedCost);
    public decimal ReportEstimatedGrossProfit => ReportEvents.Sum(x => x.EstimatedGrossProfit);
    public decimal ReportEstimatedGrossMarginRate => ReportRevenue <= 0m ? 0m : ReportEstimatedGrossProfit / ReportRevenue;
    public decimal ReportAverageEventValue => ReportTotalEvents == 0 ? 0m : ReportRevenue / ReportTotalEvents;
    public string ReportDateRangeText => $"{ReportFromDate:dd/MM/yyyy} - {ReportToDate:dd/MM/yyyy}";
    public string ReportRevenueChangeText { get; private set; } = "Không có kỳ so sánh";
    public string ReportRevenueChangeBrush { get; private set; } = "#64748B";
    public bool HasReportEvents => ReportEvents.Count > 0;
    public bool IsReportEventsEmpty => !HasReportEvents;
    public int RoomCalendarTotalSlotCount => RoomCalendarRows.Count * 14;
    public int RoomCalendarBookedSlotCount => RoomCalendarRows.Sum(row => row.Days.Count(day => !day.Lunch.IsEmpty) + row.Days.Count(day => !day.Dinner.IsEmpty));
    public int RoomCalendarAvailableSlotCount => Math.Max(0, RoomCalendarTotalSlotCount - RoomCalendarBookedSlotCount);
    public string RoomCalendarOccupancyText => RoomCalendarTotalSlotCount == 0 ? "0%" : $"{RoomCalendarBookedSlotCount * 100d / RoomCalendarTotalSlotCount:0}%";
    public bool HasRoomCalendarRows => RoomCalendarRows.Count > 0;
    public bool IsRoomCalendarEmpty => !HasRoomCalendarRows;
    public string TodayHeaderText => DateTime.Now.ToString("dddd, dd/MM/yyyy  HH:mm", new CultureInfo("vi-VN"));
    public bool HasEvents => Events.Count > 0;
    public bool HasRooms => Rooms.Count > 0;
    public bool HasFilteredEvents => FilteredEvents.Count > 0;
    public bool HasDashboardEvents => DashboardActionEvents.Count > 0;
    public bool HasDashboardAlerts => DashboardAlerts.Count > 0;
    public bool HasOverviewChecklistPreviewItems => OverviewChecklistPreviewItems.Count > 0;
    public bool HasHoldRoomEvents => HoldRoomEvents.Count > 0;
    public bool IsFilteredEventsEmpty => FilteredEvents.Count == 0;
    public bool IsDashboardEventsEmpty => DashboardActionEvents.Count == 0;
    public bool IsDashboardAlertsEmpty => DashboardAlerts.Count == 0;
    public bool IsOverviewChecklistPreviewEmpty => OverviewChecklistPreviewItems.Count == 0;
    public bool IsHoldRoomEventsEmpty => HoldRoomEvents.Count == 0;
    public bool HasFinanceSummaries => FinanceSummaries.Count > 0;
    public bool IsFinanceSummariesEmpty => FinanceSummaries.Count == 0;
    public bool HasBanquetServices => BanquetServiceItems.Count > 0;

    public string CurrentWorkspaceTitle => SelectedTabIndex switch
    {
        0 => "Tổng quan vận hành tiệc",
        1 => "Lịch phòng & sảnh tiệc",
        2 => "Cài đặt module tiệc",
        3 => "Danh sách sự kiện",
        4 => "Vận hành & checklist",
        6 => "Báo cáo điều hành",
        7 => "Phòng tiệc",
        8 => "Định lượng tiệc",
        _ => "Nhà hàng Tiệc"
    };

    public string CurrentWorkspaceSubtitle => SelectedTabIndex switch
    {
        0 => "Chỉ hiển thị KPI, lịch tiệc hôm nay, việc cần xử lý và cảnh báo.",
        1 => "Kiểm tra phòng trống, lịch đặt và cảnh báo trùng lịch theo ngày.",
        2 => "Thiết lập tham số vận hành cấp module, không chen vào luồng xử lý sự kiện.",
        3 => "Lọc danh sách sự kiện và xử lý chi tiết theo từng tab nghiệp vụ.",
        4 => "Checklist cross-event: tất cả công việc vận hành trong tuần.",
        6 => "Tổng hợp số liệu điều hành theo thời gian, loại tiệc và trạng thái.",
        7 => "Quản lý danh sách phòng tiệc, sức chứa và số bàn tối đa.",
        8 => "Tổng hợp nguyên liệu và dịch vụ cần chuẩn bị từ dữ liệu tiệc đã xác nhận.",
        _ => "Không gian điều hành nghiệp vụ nhà hàng tiệc."
    };

    partial void OnQuantificationFromDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(QuantificationDateRangeText));
        QuantificationSentAt = null;
    }

    partial void OnQuantificationToDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(QuantificationDateRangeText));
        QuantificationSentAt = null;
    }

    partial void OnQuantificationStatusFilterChanged(string value) => QuantificationSentAt = null;
    partial void OnQuantificationSentAtChanged(DateTime? value) => OnPropertyChanged(nameof(QuantificationSentText));
    partial void OnSelectedQuantificationEventChanged(BanquetQuantificationEventVm? value)
    {
        Reset(QuantificationIngredients, value?.Ingredients ?? []);
        Reset(QuantificationServices, value?.Services ?? []);
        Reset(QuantificationMissingRecipes, value?.MissingRecipes ?? []);
        QuantificationSentAt = value?.LastSentAt;
        OnPropertyChanged(nameof(SelectedQuantificationEventTitle));
        OnPropertyChanged(nameof(SelectedQuantificationEventDetail));
        NotifyQuantificationSummaryChanged();
    }

    [RelayCommand]
    private async Task RefreshQuantificationAsync()
    {
        if (!await _quantificationOperationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (QuantificationToDate.Date < QuantificationFromDate.Date)
            {
                QuantificationStatusText = "Đến ngày phải lớn hơn hoặc bằng từ ngày.";
                return;
            }

            IsQuantificationLoading = true;
            QuantificationStatusText = "Đang đọc thực đơn, công thức và dịch vụ...";

            var events = (await _service.GetEventsAsync(QuantificationFromDate.Date, QuantificationToDate.Date))
                .Where(IsEventIncludedInQuantification)
                .OrderBy(x => x.EventDate)
                .ThenBy(x => x.StartTime)
                .ToArray();
            var packages = (await _service.GetMenuPackagesAsync()).ToDictionary(x => x.Id);
            var details = new List<BanquetEventDetailDto>(events.Length);
            foreach (var banquetEvent in events)
            {
                details.Add(await _service.GetEventDetailAsync(banquetEvent.Id));
            }

            var quantificationHistory = (await _service.GetQuantificationBatchesAsync())
                .GroupBy(x => x.BanquetEventId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(n => n.VersionNo).ToArray());
            var recipeCache = new Dictionary<Guid, MenuRecipeDto?>();
            var eventRows = new List<BanquetQuantificationEventVm>(details.Count);
            foreach (var detail in details)
            {
                quantificationHistory.TryGetValue(detail.Event.Id, out var eventHistory);
                eventRows.Add(await BuildEventQuantificationAsync(detail, packages, recipeCache, eventHistory ?? []));
            }

            var selectedEventId = SelectedQuantificationEvent?.EventId;
            Reset(QuantificationEvents, eventRows);
            QuantificationEventCount = events.Length;
            SelectedQuantificationEvent = QuantificationEvents.FirstOrDefault(x => x.EventId == selectedEventId)
                ?? QuantificationEvents.FirstOrDefault();
            QuantificationStatusText = events.Length == 0
                ? "Không có tiệc phù hợp trong khoảng ngày đã chọn."
                : $"Đã tải định lượng riêng cho {events.Length} tiệc. Chọn một tiệc để xem và gửi.";
            OnPropertyChanged(nameof(HasQuantificationEvents));
            OnPropertyChanged(nameof(IsQuantificationEventsEmpty));
            NotifyQuantificationSummaryChanged();
        }
        catch (Exception ex)
        {
            QuantificationStatusText = $"Không thể tổng hợp định lượng: {ex.Message}";
        }
        finally
        {
            IsQuantificationLoading = false;
            _quantificationOperationGate.Release();
        }
    }

    [RelayCommand]
    private async Task SendQuantificationAsync()
    {
        var selected = SelectedQuantificationEvent;
        if (selected is null || IsQuantificationLoading)
        {
            QuantificationStatusText = "Chọn một tiệc trước khi gửi định lượng.";
            return;
        }

        if (!await _quantificationOperationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            IsQuantificationLoading = true;
            var batchLines = selected.Ingredients.Select(x => new BanquetQuantificationLineDto
                {
                    LineType = "Ingredient", Code = x.Code, Name = x.Name, UnitName = x.UnitName,
                    RequiredQuantity = x.RequiredQuantity, EstimatedCost = x.EstimatedCost, SourceText = x.SourceText
                })
                .Concat(selected.Services.Select(x => new BanquetQuantificationLineDto
                {
                    LineType = "Service", Code = x.Code, Name = x.Name, UnitName = x.UnitName,
                    RequiredQuantity = x.RequiredQuantity, EstimatedCost = x.EstimatedCost, SourceText = x.SourceText
                }))
                .Concat(selected.MissingRecipes.Select(x => new BanquetQuantificationLineDto
                {
                    LineType = "MissingRecipe", Code = x.MenuItemId.ToString(), Name = x.MenuItemName,
                    UnitName = "suất", RequiredQuantity = x.MenuQuantity, Note = x.Reason
                }))
                .ToArray();
            var savedBatch = await _service.SaveQuantificationBatchAsync(new SaveBanquetQuantificationBatchRequest
            {
                BanquetEventId = selected.EventId,
                Fingerprint = selected.CurrentFingerprint,
                SentBy = Environment.UserName,
                Note = selected.MissingRecipeCount > 0 ? $"Còn {selected.MissingRecipeCount} món chưa có công thức." : null,
                Lines = batchLines
            });
            var purchaseRequest = await _service.CreatePurchaseRequestAsync(savedBatch.Id, Environment.UserName);
            var metadata = JsonSerializer.Serialize(new
            {
                Fingerprint = selected.CurrentFingerprint,
                SendNumber = savedBatch.VersionNo,
                QuantificationBatchId = savedBatch.Id,
                PurchaseRequestId = purchaseRequest.Id,
                purchaseRequest.RequestNo,
                purchaseRequest.RequiredDate,
                Event = new
                {
                    selected.EventId,
                    selected.EventNo,
                    selected.CustomerName,
                    selected.Phone,
                    selected.EventTypeDisplay,
                    selected.EventDate,
                    selected.StartTime,
                    selected.EndTime,
                    selected.RoomName,
                    selected.GuestCount,
                    selected.TableCount
                },
                Ingredients = selected.Ingredients.Select(x => new
                {
                    x.Code,
                    x.Name,
                    x.UnitName,
                    x.RequiredQuantity,
                    x.EstimatedCost,
                    x.SourceText
                }).ToArray(),
                Services = selected.Services.Select(x => new
                {
                    x.Code,
                    x.Name,
                    x.UnitName,
                    x.RequiredQuantity,
                    x.EstimatedCost
                }).ToArray(),
                MissingRecipes = selected.MissingRecipes.Select(x => new
                {
                    x.MenuItemId,
                    x.MenuItemName,
                    x.MenuQuantity,
                    x.Reason
                }).ToArray()
            });

            await _notificationTaskService.PublishAsync(new NotificationPublishRequest
            {
                NotificationType = "Định lượng tiệc",
                Severity = selected.MissingRecipeCount > 0 ? "Warning" : "Info",
                Title = $"Định lượng tiệc {selected.EventNo} - {selected.CustomerName} (lần {savedBatch.VersionNo})",
                Message = BuildQuantificationNotificationMessage(selected),
                SourceType = "BanquetQuantificationBatch",
                SourceId = savedBatch.Id,
                TargetRoute = "inventory",
                MetadataJson = metadata
            });

            var sentAt = savedBatch.SentAt;
            selected.MarkSent(sentAt);
            QuantificationSentAt = sentAt;
            QuantificationStatusText = $"Đã gửi định lượng lần {selected.SendCount} và tạo yêu cầu mua {purchaseRequest.RequestNo}.";
        }
        catch (Exception ex)
        {
            QuantificationStatusText = $"Không thể gửi định lượng: {ex.Message}";
        }
        finally
        {
            IsQuantificationLoading = false;
            _quantificationOperationGate.Release();
        }
    }

    private static string BuildQuantificationNotificationMessage(BanquetQuantificationEventVm selected)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Mã sự kiện: {selected.EventNo}");
        builder.AppendLine($"Khách đặt tiệc: {selected.CustomerName}");
        builder.AppendLine($"Điện thoại: {(string.IsNullOrWhiteSpace(selected.Phone) ? "Chưa cập nhật" : selected.Phone)}");
        builder.AppendLine($"Loại tiệc: {selected.EventTypeDisplay}");
        builder.AppendLine($"Thời gian: {selected.EventDateText}, {selected.TimeText}");
        builder.AppendLine($"Địa điểm: {selected.LocationText}");
        builder.AppendLine($"Quy mô: {selected.ScaleText}");
        builder.AppendLine();
        builder.AppendLine($"NGUYÊN LIỆU CẦN CHUẨN BỊ ({selected.IngredientCount} mặt hàng)");
        foreach (var item in selected.Ingredients)
        {
            builder.AppendLine($"- {item.Name} [{item.Code}]: {item.RequiredQuantity:N0} {item.UnitName}");
        }
        if (selected.Ingredients.Count == 0) builder.AppendLine("- Chưa có nguyên liệu từ công thức.");

        builder.AppendLine();
        builder.AppendLine($"DỊCH VỤ CẦN ĐIỀU PHỐI ({selected.ServiceCount} hạng mục)");
        foreach (var item in selected.Services)
        {
            builder.AppendLine($"- {item.Name}: {item.RequiredQuantity:N0} {item.UnitName}");
        }
        if (selected.Services.Count == 0) builder.AppendLine("- Không có dịch vụ cần điều phối.");

        if (selected.MissingRecipes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"CẢNH BÁO: {selected.MissingRecipeCount} món chưa có công thức");
            foreach (var item in selected.MissingRecipes)
            {
                builder.AppendLine($"- {item.MenuItemName}: {item.Reason}");
            }
        }
        return builder.ToString().TrimEnd();
    }

    private async Task<BanquetQuantificationEventVm> BuildEventQuantificationAsync(
        BanquetEventDetailDto detail,
        IReadOnlyDictionary<Guid, BanquetMenuPackageDto> packages,
        IDictionary<Guid, MenuRecipeDto?> recipeCache,
        IReadOnlyList<BanquetQuantificationBatchDto> history)
    {
        var menuDemands = BuildMenuDemands([detail], packages);
        var ingredientAccumulators = new Dictionary<string, QuantificationItemAccumulator>(StringComparer.OrdinalIgnoreCase);
        var missingAccumulators = new Dictionary<Guid, QuantificationMissingAccumulator>();
        foreach (var demand in menuDemands.Values)
        {
            await ExpandRecipeAsync(
                demand.MenuItemId,
                demand.MenuItemName,
                demand.Quantity,
                demand.EventIds,
                new HashSet<Guid>(),
                recipeCache,
                ingredientAccumulators,
                missingAccumulators);
        }

        var ingredients = ingredientAccumulators.Values
            .OrderBy(x => x.Name)
            .Select((x, index) => ToQuantificationItem(x, index + 1))
            .ToArray();
        var services = BuildServiceDemands([detail]).Values
            .OrderBy(x => x.Name)
            .Select((x, index) => ToQuantificationItem(x, index + 1))
            .ToArray();
        var missing = missingAccumulators.Values
            .OrderBy(x => x.MenuItemName)
            .Select(x => new BanquetQuantificationMissingVm
            {
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItemName,
                MenuQuantity = x.MenuQuantity,
                EventCount = x.EventIds.Count,
                Reason = x.Reason
            })
            .ToArray();
        var fingerprint = CreateQuantificationFingerprint(detail.Event, ingredients, services, missing);
        var latest = history.FirstOrDefault();

        return new BanquetQuantificationEventVm
        {
            EventId = detail.Event.Id,
            EventNo = detail.Event.EventNo,
            CustomerName = string.IsNullOrWhiteSpace(detail.Event.CustomerName) ? "Khách chưa đặt tên" : detail.Event.CustomerName,
            Phone = detail.Event.Phone,
            EventTypeDisplay = detail.Event.EventTypeDisplay,
            EventDate = detail.Event.EventDate,
            StartTime = detail.Event.StartTime,
            EndTime = detail.Event.EndTime,
            RoomName = detail.Event.RoomName ?? string.Empty,
            GuestCount = detail.Event.GuestCount + detail.Event.ExtraGuestCount,
            TableCount = detail.Event.TableCount + detail.Event.ExtraTableCount,
            Ingredients = ingredients,
            Services = services,
            MissingRecipes = missing,
            CurrentFingerprint = fingerprint,
            SendCount = history.Count,
            LastSentFingerprint = latest?.Fingerprint,
            LastSentAt = latest?.SentAt
        };
    }

    private static string CreateQuantificationFingerprint(
        BanquetEventDto banquetEvent,
        IReadOnlyList<BanquetQuantificationItemVm> ingredients,
        IReadOnlyList<BanquetQuantificationItemVm> services,
        IReadOnlyList<BanquetQuantificationMissingVm> missing)
    {
        var payload = JsonSerializer.Serialize(new
        {
            banquetEvent.EventDate,
            banquetEvent.CustomerName,
            banquetEvent.Phone,
            banquetEvent.StartTime,
            banquetEvent.EndTime,
            banquetEvent.BanquetRoomId,
            banquetEvent.GuestCount,
            banquetEvent.TableCount,
            banquetEvent.ExtraGuestCount,
            banquetEvent.ExtraTableCount,
            Ingredients = ingredients.Select(x => new { x.Code, x.Name, x.UnitName, x.RequiredQuantity }),
            Services = services.Select(x => new { x.Code, x.Name, x.UnitName, x.RequiredQuantity }),
            Missing = missing.Select(x => new { x.MenuItemId, x.MenuQuantity, x.Reason })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string? TryReadFingerprint(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.TryGetProperty("Fingerprint", out var value) ? value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private bool IsEventIncludedInQuantification(BanquetEventDto banquetEvent)
    {
        var status = NormalizeEventStatus(banquetEvent.Status);
        return QuantificationStatusFilter switch
        {
            "Ready" => status is "Confirmed" or "Preparing",
            "Active" => status is not "Cancelled" and not "Completed",
            _ => string.Equals(status, QuantificationStatusFilter, StringComparison.OrdinalIgnoreCase)
        };
    }

    private Dictionary<Guid, QuantificationMenuDemand> BuildMenuDemands(
        IEnumerable<BanquetEventDetailDto> details,
        IReadOnlyDictionary<Guid, BanquetMenuPackageDto> packages)
    {
        var result = new Dictionary<Guid, QuantificationMenuDemand>();
        foreach (var detail in details)
        {
            var guestCount = Math.Max(1, detail.Event.GuestCount + detail.Event.ExtraGuestCount);
            var tableCount = Math.Max(1, detail.Event.TableCount + detail.Event.ExtraTableCount);
            foreach (var menu in detail.Menus)
            {
                if (menu.BanquetMenuPackageId.HasValue
                    && packages.TryGetValue(menu.BanquetMenuPackageId.Value, out var package))
                {
                    foreach (var line in package.Lines)
                    {
                        var packageLineQuantity = _menuCalculationEngine.Calculate(
                            NormalizePricingRule(line.PricingRule), guestCount, tableCount, line.Qty, line.Qty);
                        AddMenuDemand(result, line.MenuItemId, line.MenuItemName, packageLineQuantity, detail.Event.Id);
                    }
                    continue;
                }

                if (!menu.MenuItemId.HasValue)
                {
                    continue;
                }

                var rule = NormalizePricingRule(menu.PricingMode);
                var quantity = rule == "PerPerson"
                    ? _menuCalculationEngine.Calculate(rule, guestCount, tableCount, menu.Qty, menu.Qty)
                    : Math.Max(1, tableCount) * Math.Max(0, menu.Qty);
                AddMenuDemand(result, menu.MenuItemId.Value, menu.ItemName ?? "Món chưa đặt tên", quantity, detail.Event.Id);
            }
        }
        return result;
    }

    private static void AddMenuDemand(
        IDictionary<Guid, QuantificationMenuDemand> demands,
        Guid menuItemId,
        string menuItemName,
        decimal quantity,
        Guid eventId)
    {
        if (!demands.TryGetValue(menuItemId, out var demand))
        {
            demand = new QuantificationMenuDemand { MenuItemId = menuItemId, MenuItemName = menuItemName };
            demands[menuItemId] = demand;
        }
        demand.Quantity += quantity;
        demand.EventIds.Add(eventId);
    }

    private async Task ExpandRecipeAsync(
        Guid menuItemId,
        string menuItemName,
        decimal multiplier,
        IReadOnlyCollection<Guid> eventIds,
        HashSet<Guid> path,
        IDictionary<Guid, MenuRecipeDto?> recipeCache,
        IDictionary<string, QuantificationItemAccumulator> ingredients,
        IDictionary<Guid, QuantificationMissingAccumulator> missing)
    {
        if (!path.Add(menuItemId))
        {
            AddMissingRecipe(missing, menuItemId, menuItemName, multiplier, eventIds, "Công thức thành phần bị lặp.");
            return;
        }

        if (!recipeCache.TryGetValue(menuItemId, out var recipe))
        {
            recipe = await _menuRecipeService.GetRecipeAsync(menuItemId);
            recipeCache[menuItemId] = recipe;
        }

        if (recipe is null || recipe.Lines.Count == 0)
        {
            AddMissingRecipe(missing, menuItemId, menuItemName, multiplier, eventIds, "Món chưa khai báo công thức định lượng.");
            path.Remove(menuItemId);
            return;
        }

        foreach (var line in recipe.Lines.Where(x => x.IsRequired && x.Quantity > 0))
        {
            var requiredQuantity = multiplier * line.Quantity * (1m + Math.Max(0, line.LossRate) / 100m);
            if (line.ChildMenuItemId.HasValue)
            {
                await ExpandRecipeAsync(
                    line.ChildMenuItemId.Value,
                    string.IsNullOrWhiteSpace(line.ItemName) ? "Bán thành phẩm" : line.ItemName,
                    requiredQuantity,
                    eventIds,
                    path,
                    recipeCache,
                    ingredients,
                    missing);
                continue;
            }

            var key = $"{line.ItemId}|{line.ItemCode}|{line.ItemName}|{line.UnitName}";
            if (!ingredients.TryGetValue(key, out var item))
            {
                item = new QuantificationItemAccumulator
                {
                    Code = line.ItemCode,
                    Name = string.IsNullOrWhiteSpace(line.ItemName) ? "Nguyên liệu chưa đặt tên" : line.ItemName,
                    UnitName = string.IsNullOrWhiteSpace(line.UnitName) ? "đơn vị" : line.UnitName
                };
                ingredients[key] = item;
            }
            item.RequiredQuantity += requiredQuantity;
            item.EstimatedCost += requiredQuantity * line.EstimatedUnitCost;
            item.SourceIds.Add(menuItemId);
            foreach (var eventId in eventIds) item.EventIds.Add(eventId);
        }

        path.Remove(menuItemId);
    }

    private static Dictionary<string, QuantificationItemAccumulator> BuildServiceDemands(
        IEnumerable<BanquetEventDetailDto> details)
    {
        var result = new Dictionary<string, QuantificationItemAccumulator>(StringComparer.OrdinalIgnoreCase);
        foreach (var detail in details)
        {
            foreach (var service in detail.Services.Where(x => x.Qty > 0))
            {
                var key = $"{service.ServiceType}|{service.Description}";
                if (!result.TryGetValue(key, out var item))
                {
                    item = new QuantificationItemAccumulator
                    {
                        Code = service.ServiceType,
                        Name = string.IsNullOrWhiteSpace(service.Description) ? service.ServiceTypeDisplay : service.Description,
                        UnitName = string.IsNullOrWhiteSpace(service.UnitName) ? "lần" : service.UnitName
                    };
                    result[key] = item;
                }
                item.RequiredQuantity += service.Qty;
                item.EstimatedCost += service.Amount;
                item.SourceIds.Add(service.Id);
                item.EventIds.Add(detail.Event.Id);
            }
        }
        return result;
    }

    private static BanquetQuantificationItemVm ToQuantificationItem(QuantificationItemAccumulator item, int sequence) => new()
    {
        Sequence = sequence,
        Code = item.Code,
        Name = item.Name,
        UnitName = item.UnitName,
        RequiredQuantity = decimal.Round(item.RequiredQuantity, 3),
        EventCount = item.EventIds.Count,
        SourceCount = item.SourceIds.Count,
        EstimatedCost = decimal.Round(item.EstimatedCost, 0),
        SourceText = $"{item.SourceIds.Count} nguồn · {item.EventIds.Count} tiệc"
    };

    private static void AddMissingRecipe(
        IDictionary<Guid, QuantificationMissingAccumulator> missing,
        Guid menuItemId,
        string menuItemName,
        decimal quantity,
        IEnumerable<Guid> eventIds,
        string reason)
    {
        if (!missing.TryGetValue(menuItemId, out var item))
        {
            item = new QuantificationMissingAccumulator
            {
                MenuItemId = menuItemId,
                MenuItemName = menuItemName,
                Reason = reason
            };
            missing[menuItemId] = item;
        }
        item.MenuQuantity += quantity;
        foreach (var eventId in eventIds) item.EventIds.Add(eventId);
    }

    private void NotifyQuantificationSummaryChanged()
    {
        OnPropertyChanged(nameof(QuantificationIngredientCount));
        OnPropertyChanged(nameof(QuantificationServiceCount));
        OnPropertyChanged(nameof(QuantificationMissingRecipeCount));
        OnPropertyChanged(nameof(QuantificationEstimatedCost));
        OnPropertyChanged(nameof(HasQuantificationData));
        OnPropertyChanged(nameof(IsQuantificationEmpty));
        OnPropertyChanged(nameof(IsQuantificationIngredientsEmpty));
        OnPropertyChanged(nameof(IsQuantificationServicesEmpty));
        OnPropertyChanged(nameof(HasQuantificationWarnings));
    }

    private static string NormalizePricingRule(string? pricingRule) =>
        string.Equals(pricingRule, "PerGuest", StringComparison.OrdinalIgnoreCase)
            ? "PerPerson"
            : pricingRule?.Trim() ?? "PerTable";

    private sealed class QuantificationMenuDemand
    {
        public Guid MenuItemId { get; init; }
        public string MenuItemName { get; init; } = string.Empty;
        public decimal Quantity { get; set; }
        public HashSet<Guid> EventIds { get; } = [];
    }

    private sealed class QuantificationItemAccumulator
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string UnitName { get; init; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal EstimatedCost { get; set; }
        public HashSet<Guid> EventIds { get; } = [];
        public HashSet<Guid> SourceIds { get; } = [];
    }

    private sealed class QuantificationMissingAccumulator
    {
        public Guid MenuItemId { get; init; }
        public string MenuItemName { get; init; } = string.Empty;
        public decimal MenuQuantity { get; set; }
        public string Reason { get; init; } = string.Empty;
        public HashSet<Guid> EventIds { get; } = [];
    }

    partial void OnBanquetCatalogColumnCountChanged(int value)
    {
        if (value < 1)
        {
            BanquetCatalogColumnCount = 1;
        }
        else if (value > 8)
        {
            BanquetCatalogColumnCount = 8;
        }
    }

    private void ApplySelectedEventToEditor(BanquetEventDto? value)
    {
        if (value is null)
        {
            ResetEventEditor();
            return;
        }

        EventType = string.IsNullOrWhiteSpace(value.EventType) ? "Wedding" : value.EventType;
        EventCustomerName = value.CustomerName ?? string.Empty;
        EventPhone = value.Phone;
        EventDate = value.EventDate;
        StartTimeText = value.StartTime.ToString(@"hh\:mm");
        EndTimeText = value.EndTime.ToString(@"hh\:mm");
        GuestCount = value.GuestCount;
        TableCount = value.TableCount;
        ExtraGuestCount = value.ExtraGuestCount;
        ExtraTableCount = value.ExtraTableCount;
        BrideName = value.BrideName ?? string.Empty;
        GroomName = value.GroomName ?? string.Empty;
        ConferenceName = value.ConferenceName ?? string.Empty;
        BirthdayPersonName = value.BirthdayPersonName ?? string.Empty;
        BirthdayDate = value.BirthdayDate;
        MeetingTopic = value.MeetingTopic ?? string.Empty;
        PartyTheme = value.PartyTheme ?? string.Empty;
        OtherEventName = value.OtherEventName ?? string.Empty;
        SelectedEventRoomId = value.BanquetRoomId;
        EventStatus = NormalizeEventStatus(value.Status);
        EventNote = value.Note ?? string.Empty;
        DepositAmount = value.RemainingAmount > 0m ? value.RemainingAmount : 0m;
        ContractSignDate = DateTime.Today;
        BuildTimelineMilestones();
        IsDirty = false;
        ValidationMessage = string.Empty;
        ResetEventHistory();
    }

    private void ResetEventEditor()
    {
        EventType = "Wedding";
        EventCustomerName = string.Empty;
        EventPhone = string.Empty;
        EventDate = DateTime.Today.AddDays(1);
        StartTimeText = TimeSpan.TryParse(BanquetDinnerStartTime, out var dinnerStart)
            ? dinnerStart.ToString(@"hh\:mm")
            : "18:00";
        EndTimeText = TimeSpan.TryParse(StartTimeText, out var parsedStart)
            ? parsedStart.Add(TimeSpan.FromHours(3)).ToString(@"hh\:mm")
            : "21:00";
        GuestCount = 100;
        TableCount = Math.Max(1, (int)Math.Ceiling(
            GuestCount / (double)Math.Max(1, BanquetGuestsPerTable)));
        ExtraGuestCount = 0;
        ExtraTableCount = 0;
        BrideName = string.Empty;
        GroomName = string.Empty;
        ConferenceName = string.Empty;
        BirthdayPersonName = string.Empty;
        BirthdayDate = null;
        MeetingTopic = string.Empty;
        PartyTheme = string.Empty;
        OtherEventName = string.Empty;
        SelectedEventRoomId = SelectedRoom?.Id ?? Rooms.FirstOrDefault()?.Id;
        EventStatus = "Confirmed";
        EventNote = string.Empty;
        DepositAmount = 0m;
        ContractSignDate = DateTime.Today;
        BuildTimelineMilestones();
        IsDirty = false;
        ValidationMessage = string.Empty;
        ResetEventHistory();
    }

    private void ResetEventHistory()
    {
        _undoEventSnapshot = CaptureEventSnapshot();
        _redoEventSnapshot = null;
    }

    private BanquetEventEditorSnapshot CaptureEventSnapshot() => new(
        EventType, EventCustomerName, EventPhone, EventDate, StartTimeText, EndTimeText,
        GuestCount, TableCount, ExtraGuestCount, ExtraTableCount, BrideName, GroomName, ConferenceName,
        BirthdayPersonName, BirthdayDate, MeetingTopic, PartyTheme, OtherEventName, SelectedEventRoomId, EventStatus, EventNote);

    private void RestoreEventSnapshot(BanquetEventEditorSnapshot snapshot)
    {
        _restoringEventSnapshot = true;
        try
        {
            EventType = snapshot.EventType;
            EventCustomerName = snapshot.CustomerName;
            EventPhone = snapshot.Phone;
            EventDate = snapshot.EventDate;
            StartTimeText = snapshot.StartTime;
            EndTimeText = snapshot.EndTime;
        GuestCount = snapshot.GuestCount;
        TableCount = snapshot.TableCount;
        ExtraGuestCount = snapshot.ExtraGuestCount;
        ExtraTableCount = snapshot.ExtraTableCount;
        BrideName = snapshot.BrideName;
        GroomName = snapshot.GroomName;
        ConferenceName = snapshot.ConferenceName;
        BirthdayPersonName = snapshot.BirthdayPersonName;
        BirthdayDate = snapshot.BirthdayDate;
        MeetingTopic = snapshot.MeetingTopic;
        PartyTheme = snapshot.PartyTheme;
        OtherEventName = snapshot.OtherEventName;
            SelectedEventRoomId = snapshot.RoomId;
            EventStatus = snapshot.Status;
            EventNote = snapshot.Note;
        }
        finally
        {
            _restoringEventSnapshot = false;
        }
    }

    private void MarkEventDirty()
    {
        if (_restoringEventSnapshot)
        {
            return;
        }

        IsDirty = true;
        _redoEventSnapshot = null;
    }

    [RelayCommand]
    private void UndoEventEdit()
    {
        if (_undoEventSnapshot is null || !IsDirty)
        {
            return;
        }

        _redoEventSnapshot = CaptureEventSnapshot();
        RestoreEventSnapshot(_undoEventSnapshot);
        IsDirty = false;
        ValidationMessage = string.Empty;
        StatusText = "Đã hoàn tác thay đổi trên sự kiện.";
    }

    [RelayCommand]
    private void RedoEventEdit()
    {
        if (_redoEventSnapshot is null)
        {
            return;
        }

        RestoreEventSnapshot(_redoEventSnapshot);
        IsDirty = true;
        StatusText = "Đã khôi phục thay đổi trên sự kiện.";
    }

    private bool ValidateEventEditor()
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(EventCustomerName)) messages.Add("Nhập tên khách hàng.");
        if (GuestCount <= 0) messages.Add("Số khách phải lớn hơn 0.");
        if (TableCount <= 0) messages.Add("Số bàn phải lớn hơn 0.");

        if (!TimeSpan.TryParse(StartTimeText, out var start) ||
            !TimeSpan.TryParse(EndTimeText, out var end) ||
            start >= end)
        {
            messages.Add("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");
        }

        var room = Rooms.FirstOrDefault(x => x.Id == SelectedEventRoomId);
        if (room is not null && (GuestCount > room.Capacity || TableCount > room.MaxTables))
        {
            messages.Add($"Quy mô vượt sức chứa của {room.Name}.");
        }

        ValidationMessage = string.Join(" ", messages);
        return messages.Count == 0;
    }

    partial void OnEventCustomerNameChanged(string value) => MarkEventDirty();
    partial void OnEventPhoneChanged(string value) => MarkEventDirty();
    partial void OnEventDateChanged(DateTime value) => MarkEventDirty();
    partial void OnStartTimeTextChanged(string value) => MarkEventDirty();
    partial void OnEndTimeTextChanged(string value) => MarkEventDirty();
    partial void OnGuestCountChanged(int value) => MarkEventDirty();
    partial void OnTableCountChanged(int value) { MarkEventDirty(); OnPropertyChanged(nameof(SelectedBanquetCustomFoodMainTableAmount)); OnPropertyChanged(nameof(SelectedBanquetCustomFoodProjectedTableAmount)); }
    partial void OnExtraTableCountChanged(int value) { MarkEventDirty(); OnPropertyChanged(nameof(SelectedBanquetCustomFoodProjectedTableAmount)); }
    partial void OnExtraGuestCountChanged(int value) => MarkEventDirty();
    partial void OnBrideNameChanged(string value) => MarkEventDirty();
    partial void OnGroomNameChanged(string value) => MarkEventDirty();
    partial void OnConferenceNameChanged(string value) => MarkEventDirty();
    partial void OnBirthdayPersonNameChanged(string value) => MarkEventDirty();
    partial void OnBirthdayDateChanged(DateTime? value) => MarkEventDirty();
    partial void OnMeetingTopicChanged(string value) => MarkEventDirty();
    partial void OnPartyThemeChanged(string value) => MarkEventDirty();
    partial void OnOtherEventNameChanged(string value) => MarkEventDirty();
    partial void OnEventNoteChanged(string value) => MarkEventDirty();
    partial void OnEventStatusChanged(string value) => MarkEventDirty();
    partial void OnEditingChecklistItemIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(IsEditingChecklistItem));
        OnPropertyChanged(nameof(ChecklistFormTitle));
        OnPropertyChanged(nameof(ChecklistSaveButtonText));
    }

    partial void OnEditingTimelineItemIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(IsEditingTimelineItem));
        OnPropertyChanged(nameof(TimelineFormTitle));
        OnPropertyChanged(nameof(TimelineSaveButtonText));
    }

    private void ResetDetailCollections()
    {
        ResetEventServices(Array.Empty<BanquetServiceLineDto>());
        Reset(EventDeposits, Array.Empty<BanquetDepositDto>());
        Reset(EventContracts, Array.Empty<BanquetContractDto>());
        Reset(EventMenus, Array.Empty<BanquetEventMenuDto>());
        Reset(SelectedBanquetCustomFoods, Array.Empty<BanquetSelectedFoodItemVm>());
        Reset(EventTimelineItems, Array.Empty<BanquetTimelineItemDto>());
        Reset(ChecklistItems, Array.Empty<BanquetChecklistDto>());
        UpdateChecklistGroups();
        UpdateWorkflowSteps();
        BuildTimelineMilestones();
        UpdateOverviewChecklistPreview();
        OnPropertyChanged(nameof(HasSelectedBanquetCustomFoods));
        NotifyCustomFoodAmountsChanged();
        OnPropertyChanged(nameof(SelectedEventPaidAmount));
        OnPropertyChanged(nameof(SelectedEventOutstandingAmount));
        OnPropertyChanged(nameof(SelectedEventContractValue));
        OnPropertyChanged(nameof(HasSelectedEvent));
        NotifyFinanceSummaryChanged();
    }

    private async Task LoadSelectedEventDetailAsync(Guid eventId)
    {
        var loadVersion = Interlocked.Increment(ref _selectedEventDetailLoadVersion);
        var detail = await _service.GetEventDetailAsync(eventId);
        if (loadVersion != Volatile.Read(ref _selectedEventDetailLoadVersion)
            || SelectedEvent?.Id != eventId)
        {
            return;
        }

        if (detail.Event is not null)
        {
            _suppressSelectedEventChanged = true;
            try
            {
                SelectedEvent = detail.Event;
                ApplySelectedEventToEditor(detail.Event);
            }
            finally
            {
                _suppressSelectedEventChanged = false;
            }
        }

        ResetEventServices(detail.Services);
        Reset(EventDeposits, detail.Deposits);
        Reset(EventContracts, detail.Contracts.OrderByDescending(x => x.SignDate).ThenByDescending(x => x.ContractNo));
        SelectedContract = EventContracts.FirstOrDefault();
        Reset(EventMenus, detail.Menus);
        PopulateCustomFoodsFromMenus(detail.Menus);
        Reset(EventTimelineItems, detail.Timeline.OrderBy(x => x.StartTime).ThenBy(x => x.SortOrder));
        Reset(ChecklistItems, detail.Checklist);
        UpdateChecklistGroups();
        UpdateWorkflowSteps();
        UpdateHoldRoomEvents();
        BuildTimelineMilestones();
        UpdateOverviewChecklistPreview();
        OnPropertyChanged(nameof(HasSelectedBanquetCustomFoods));
        NotifyCustomFoodAmountsChanged();
        OnPropertyChanged(nameof(SelectedEventPaidAmount));
        OnPropertyChanged(nameof(SelectedEventOutstandingAmount));
        OnPropertyChanged(nameof(SelectedEventContractValue));
        OnPropertyChanged(nameof(HasSelectedEvent));
        NotifyFinanceSummaryChanged();
    }

    private void PopulateCustomFoodsFromMenus(IReadOnlyList<BanquetEventMenuDto> menus)
    {
        var foodLookup = BanquetFoodItems.Where(f => f.Id != Guid.Empty).ToDictionary(f => f.Id);
        var customFoods = new List<BanquetSelectedFoodItemVm>();
        int lineNo = 1;
        foreach (var menu in menus.OrderBy(m => m.Id))
        {
            if (menu.BanquetMenuPackageId.HasValue)
            {
                var package = MenuPackages.FirstOrDefault(x => x.Id == menu.BanquetMenuPackageId.Value);
                var packageLines = menu.PackageLines.Count > 0
                    ? menu.PackageLines
                    : package?.Lines ?? Array.Empty<BanquetMenuPackageLineDto>();
                foreach (var line in packageLines.OrderBy(x => x.LineNo))
                {
                    foodLookup.TryGetValue(line.MenuItemId, out var packageFoodItem);
                    customFoods.Add(new BanquetSelectedFoodItemVm
                    {
                        LineNo = lineNo++,
                        FoodItem = packageFoodItem ?? new PosMenuItemCard(new MenuItemDto
                        {
                            Id = line.MenuItemId,
                            Code = line.MenuItemCode,
                            Name = line.MenuItemName,
                            UnitName = line.UnitName,
                            UnitPrice = line.UnitPrice,
                            IsActive = true
                        }),
                        Quantity = Math.Max(1, (int)line.Qty)
                    });
                }
                continue;
            }

            if (!menu.MenuItemId.HasValue) continue;
            foodLookup.TryGetValue(menu.MenuItemId.Value, out var foodItem);
            customFoods.Add(new BanquetSelectedFoodItemVm
            {
                LineNo = lineNo++,
                FoodItem = foodItem ?? new PosMenuItemCard(new MenuItemDto { Id = menu.MenuItemId.Value, Name = menu.ItemName ?? "Món không xác định", UnitPrice = menu.UnitPrice, UnitName = string.Empty }),
                Quantity = Math.Max(1, (int)menu.Qty)
            });
        }
        Reset(SelectedBanquetCustomFoods, customFoods);
    }

    private void ResetEventServices(IEnumerable<BanquetServiceLineDto> services)
    {
        var rows = services.ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            rows[index].LineNo = index + 1;
            rows[index].ServiceTypeDisplay = GetServiceTypeDisplayName(rows[index].ServiceType);
        }
        Reset(EventServices, rows);
        OnPropertyChanged(nameof(SelectedBanquetServiceTotalAmount));
    }

    private async Task RefreshPreservingSelectionAsync(Guid? selectedEventId = null, Guid? selectedRoomId = null)
    {
        selectedEventId ??= SelectedEvent?.Id;
        selectedRoomId ??= SelectedRoom?.Id;

        Dashboard = await _service.GetDashboardAsync();
        Reset(Rooms, await _service.GetRoomsAsync());
        RefreshRoomFilter();
        RebuildRoomCalendarFilterOptions();
        OnPropertyChanged(nameof(HasRooms));
        Reset(Events, await _service.GetEventsAsync(
            new DateTime(DateTime.Today.Year - 1, 1, 1),
            new DateTime(DateTime.Today.Year + 2, 12, 31)));
        OnPropertyChanged(nameof(HasEvents));
        if (ShouldLoadMenuPackages())
        {
            Reset(MenuPackages, await _service.GetMenuPackagesAsync());
        }

        RebuildRoomFilterOptions();
        RoomsPager.SetSource(Rooms);
        RefreshEventCollections();
        RefreshDashboardCollections();
        RefreshSidebarCollections();
        RefreshReportCollections();
        BuildOverviewCalendar();

        SelectedRoom = Rooms.FirstOrDefault(x => x.Id == selectedRoomId) ?? Rooms.FirstOrDefault();

        _suppressSelectedEventChanged = true;
        try
        {
            SelectedEvent = Events.FirstOrDefault(x => x.Id == selectedEventId)
                ?? FilteredEvents.FirstOrDefault()
                ?? Events.FirstOrDefault();
        }
        finally
        {
            _suppressSelectedEventChanged = false;
        }

        if (SelectedEvent is null)
        {
            ResetDetailCollections();
        }
        else if (ShouldLoadSelectedEventDetail())
        {
            ApplySelectedEventToEditor(SelectedEvent);
            await LoadSelectedEventDetailAsync(SelectedEvent.Id);
        }
        else
        {
            ApplySelectedEventToEditor(SelectedEvent);
            ResetDetailCollections();
        }

        UpdateHoldRoomEvents();
        BuildOverviewCalendar();
        BuildRoomCalendar();
        BuildRoomCalendarYear();
    }

    private bool ShouldLoadMenuPackages()
        => IsEventsTab || IsContractsTab || IsFinanceTab;

    private bool ShouldLoadSelectedEventDetail()
        => IsEventsTab || IsContractsTab || IsFinanceTab || ShowEventEditor;

    internal async Task EnsureSelectedEventDetailLoadedAsync()
    {
        if (!ShouldLoadSelectedEventDetail() || SelectedEvent is null)
            return;

        await EnsureMenuItemsLoadedAsync();

        if (MenuPackages.Count == 0 && ShouldLoadMenuPackages())
            Reset(MenuPackages, await _service.GetMenuPackagesAsync());

        if (EventMenus.Count == 0 && EventServices.Count == 0 && EventDeposits.Count == 0 && EventContracts.Count == 0 && ChecklistItems.Count == 0 && EventTimelineItems.Count == 0)
            await LoadSelectedEventDetailAsync(SelectedEvent.Id);
    }

    internal async Task EnsureMenuItemsLoadedAsync()
    {
        await _banquetCatalogLoadGate.WaitAsync();
        try
        {
            if (BanquetFoodItems.Count == 0)
                await LoadBanquetMenuItemsAsync();

            if (BanquetServiceItems.Count == 0)
                await LoadBanquetServicesWithRetryAsync();
        }
        finally
        {
            _banquetCatalogLoadGate.Release();
        }
    }

    partial void OnSelectedContractChanged(BanquetContractDto? value)
    {
        if (value is null)
        {
            ContractAdditionalTerms = string.Empty;
            ContractStatus = "Confirmed";
            ContractPreviewText = "Chọn hoặc tạo hợp đồng để xem nội dung.";
            return;
        }

        ContractSignDate = value.SignDate ?? DateTime.Today;
        ContractStatus = string.IsNullOrWhiteSpace(value.Status) ? "Confirmed" : value.Status;
        ContractAdditionalTerms = value.Note ?? string.Empty;
        ContractPreviewText = BuildContractDocument(value);
    }

    private string BuildContractDocument(BanquetContractDto contract)
    {
        if (SelectedEvent is null)
            return "Không có dữ liệu sự kiện để lập hợp đồng.";

        var evt = SelectedEvent;
        var menuLines = EventMenus.Count == 0
            ? "- Chưa có thực đơn được chọn."
            : string.Join(Environment.NewLine, EventMenus.Select((x, i) => $"  {i + 1}. {x.ItemName ?? x.PackageName ?? "Món ăn"} - SL: {x.Qty:0.##} - Thành tiền: {x.Amount:N0} đ"));
        var serviceLines = EventServices.Count == 0
            ? "- Chưa có dịch vụ bổ sung."
            : string.Join(Environment.NewLine, EventServices.Select((x, i) => $"  {i + 1}. {x.Description} - SL: {x.Qty:0.##} - Thành tiền: {x.Amount:N0} đ"));
        var eventSubject = evt.EventType switch
        {
            "Wedding" => $"Tiệc cưới {evt.BrideName} - {evt.GroomName}",
            "Conference" => evt.ConferenceName ?? evt.EventTypeDisplay,
            "Birthday" => $"Sinh nhật {evt.BirthdayPersonName}",
            "Meeting" => evt.MeetingTopic ?? evt.EventTypeDisplay,
            "Party" => evt.PartyTheme ?? evt.EventTypeDisplay,
            _ => evt.OtherEventName ?? evt.EventTypeDisplay
        };

        var sb = new StringBuilder();
        sb.AppendLine("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM");
        sb.AppendLine("Độc lập - Tự do - Hạnh phúc");
        sb.AppendLine();
        sb.AppendLine("HỢP ĐỒNG CUNG CẤP DỊCH VỤ TIỆC");
        sb.AppendLine($"Số: {contract.ContractNo}");
        sb.AppendLine($"Ngày lập: {(contract.SignDate ?? DateTime.Today):dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("Căn cứ Bộ luật Dân sự 2015 và nhu cầu, khả năng của các bên;");
        sb.AppendLine("Hai bên thống nhất giao kết hợp đồng với các điều khoản sau:");
        sb.AppendLine();
        sb.AppendLine("BÊN A - ĐƠN VỊ CUNG CẤP DỊCH VỤ");
        sb.AppendLine("Thông tin pháp lý, địa chỉ, mã số thuế và người đại diện: theo hồ sơ doanh nghiệp đang cấu hình trên hệ thống.");
        sb.AppendLine();
        sb.AppendLine("BÊN B - KHÁCH HÀNG");
        sb.AppendLine($"Họ tên/Tổ chức: {evt.CustomerName ?? "Chưa cập nhật"}");
        sb.AppendLine($"Điện thoại: {evt.Phone}");
        sb.AppendLine($"Mã sự kiện: {evt.EventNo}");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 1. ĐỐI TƯỢNG VÀ PHẠM VI DỊCH VỤ");
        sb.AppendLine($"Bên A cung cấp dịch vụ tổ chức {eventSubject} tại {evt.RoomName ?? "địa điểm đã thỏa thuận"}.");
        sb.AppendLine($"Thời gian: {evt.StartTime:hh\\:mm} - {evt.EndTime:hh\\:mm}, ngày {evt.EventDate:dd/MM/yyyy}.");
        sb.AppendLine($"Quy mô: {evt.GuestCount} khách, {evt.TableCount} bàn chính; dự kiến phát sinh {evt.ExtraGuestCount} khách, {evt.ExtraTableCount} bàn.");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 2. THỰC ĐƠN");
        sb.AppendLine(menuLines);
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 3. DỊCH VỤ BỔ SUNG");
        sb.AppendLine(serviceLines);
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 4. GIÁ TRỊ VÀ THANH TOÁN");
        sb.AppendLine($"Tổng giá trị tạm tính: {contract.TotalAmount:N0} đ.");
        sb.AppendLine($"Đã đặt cọc: {contract.DepositAmount:N0} đ. Còn phải thanh toán: {contract.RemainingAmount:N0} đ.");
        sb.AppendLine("Giá trị cuối cùng được đối chiếu theo số lượng và dịch vụ thực tế phát sinh, có xác nhận của hai bên.");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 5. QUYỀN VÀ NGHĨA VỤ CÁC BÊN");
        sb.AppendLine("Bên A cung cấp dịch vụ đúng nội dung, chất lượng, thời gian đã thỏa thuận và bảo đảm an toàn trong phạm vi trách nhiệm.");
        sb.AppendLine("Bên B cung cấp thông tin chính xác, phối hợp tổ chức và thanh toán đầy đủ, đúng hạn.");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 6. THAY ĐỔI, HỦY DỊCH VỤ VÀ BẤT KHẢ KHÁNG");
        sb.AppendLine("Mọi thay đổi phải được hai bên xác nhận bằng văn bản hoặc thông điệp dữ liệu có thể truy xuất. Trách nhiệm hoàn/hủy cọc và chi phí phát sinh thực hiện theo thỏa thuận bổ sung và quy định pháp luật.");
        sb.AppendLine("Bên bị ảnh hưởng bởi sự kiện bất khả kháng phải thông báo kịp thời và phối hợp hạn chế thiệt hại.");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU 7. GIẢI QUYẾT TRANH CHẤP VÀ HIỆU LỰC");
        sb.AppendLine("Tranh chấp được ưu tiên giải quyết bằng thương lượng; nếu không đạt thỏa thuận, các bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật Việt Nam.");
        sb.AppendLine("Hợp đồng có hiệu lực từ ngày ký/xác nhận và được lập thành các bản có giá trị pháp lý như nhau.");
        if (!string.IsNullOrWhiteSpace(contract.Note))
        {
            sb.AppendLine();
            sb.AppendLine("ĐIỀU KHOẢN BỔ SUNG");
            sb.AppendLine(contract.Note.Trim());
        }
        sb.AppendLine();
        sb.AppendLine("ĐẠI DIỆN BÊN A                                      ĐẠI DIỆN BÊN B");
        sb.AppendLine("(Ký, ghi rõ họ tên, đóng dấu nếu có)                 (Ký, ghi rõ họ tên)");
        return sb.ToString();
    }

    internal void EnsureMenuItemsLoaded() => _ = EnsureMenuItemsLoadedAsync();

    partial void OnSelectedDetailTabIndexChanged(int value)
    {
        if (value == 3 && BanquetServiceItems.Count == 0)
            _ = EnsureMenuItemsLoadedAsync();
    }

    private void ResetServiceEditor()
    {
        ServiceType = "Other";
        ServiceDescription = string.Empty;
        ServiceUnitName = "lần";
        ServiceQty = 1m;
        ServiceUnitPrice = 0m;
        ServiceCostPrice = 0m;
        ServiceVatRate = 8m;
        ServiceNote = string.Empty;
    }

    partial void OnHoldCalendarDateChanged(DateTime value)
    {
        UpdateHoldRoomEvents();
        OnPropertyChanged(nameof(HoldAvailabilityText));
        OnPropertyChanged(nameof(HoldRoomEventCount));
    }

    partial void OnRoomCalendarWeekStartChanged(DateTime value)
    {
        OnPropertyChanged(nameof(RoomCalendarRangeText));
    }

    partial void OnRoomCalendarViewModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsRoomCalendarWeekView));
        OnPropertyChanged(nameof(IsRoomCalendarYearView));
    }

    partial void OnRoomCalendarYearChanged(int value)
    {
        OnPropertyChanged(nameof(RoomCalendarYearText));
        BuildRoomCalendarYear();
    }

    partial void OnSelectedRoomCalendarRoomIdChanged(Guid? value) => RebuildRoomCalendars();
    partial void OnSelectedRoomCalendarStatusChanged(string? value) => RebuildRoomCalendars();
    partial void OnSelectedRoomCalendarEventTypeChanged(string? value) => RebuildRoomCalendars();
    partial void OnSelectedRoomCalendarShiftChanged(string? value) => RebuildRoomCalendars();
    partial void OnReportViewModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsReportOverviewView));
        OnPropertyChanged(nameof(IsReportEventsView));
        OnPropertyChanged(nameof(IsReportRoomsView));
    }
    partial void OnReportPeriodPresetChanged(string value) => ApplyReportPeriodPreset(value);
    partial void OnReportFromDateChanged(DateTime value)
    {
        if (!_updatingReportPeriod) ReportPeriodPreset = "Custom";
        RefreshReportCollections();
    }
    partial void OnReportToDateChanged(DateTime value)
    {
        if (!_updatingReportPeriod) ReportPeriodPreset = "Custom";
        RefreshReportCollections();
    }
    partial void OnSelectedReportRoomIdChanged(Guid? value) => RefreshReportCollections();
    partial void OnSelectedReportEventTypeChanged(string? value) => RefreshReportCollections();
    partial void OnSelectedReportStatusChanged(string? value) => RefreshReportCollections();
    partial void OnReportSearchTextChanged(string value) => RefreshReportCollections();
    partial void OnSelectedReportEventRowChanged(BanquetEventDto? value)
    {
        if (value is null) return;
        OpenReportEvent(value);
        SelectedReportEventRow = null;
    }

    partial void OnSelectedEventRoomIdChanged(Guid? value)
    {
        UpdateHoldRoomEvents();
        OnPropertyChanged(nameof(HoldAvailabilityText));
        OnPropertyChanged(nameof(HoldRoomEventCount));
    }

    partial void OnEventSearchTextChanged(string value) => RefreshEventCollections();
    partial void OnGlobalSearchTextChanged(string value) => RefreshEventCollections();
    partial void OnBanquetMenuSearchTextChanged(string value) => FilterBanquetFoodItems();
    partial void OnBanquetServiceSearchTextChanged(string value) => FilterBanquetServiceItems();
    partial void OnSelectedBanquetServiceTypeChanged(string? value) => FilterBanquetServiceItems();
    partial void OnSelectedBanquetMenuCategoryIdChanged(Guid? value) => FilterBanquetFoodItems();
    partial void OnSelectedEventStatusFilterChanged(string? value) => RefreshEventCollections();
    partial void OnSelectedEventTypeFilterChanged(string? value) => RefreshEventCollections();
    partial void OnSelectedEventRoomFilterIdChanged(Guid? value) => RefreshEventCollections();
    partial void OnEventFilterFromDateChanged(DateTime value) => RefreshEventCollections();
    partial void OnEventFilterToDateChanged(DateTime value) => RefreshEventCollections();

    [RelayCommand]
    private void SetReportView(string? view)
    {
        ReportViewMode = view switch
        {
            "Events" => 1,
            "Rooms" => 2,
            _ => 0
        };
    }

    [RelayCommand]
    private async Task RefreshBanquetReportsAsync()
    {
        await RefreshPreservingSelectionAsync(SelectedEvent?.Id, SelectedRoom?.Id);
        ReportStatusText = $"Đã cập nhật dữ liệu lúc {DateTime.Now:HH:mm}.";
    }

    [RelayCommand]
    private void ResetReportFilters()
    {
        SelectedReportRoomId = null;
        SelectedReportEventType = null;
        SelectedReportStatus = null;
        ReportSearchText = string.Empty;
        ReportPeriodPreset = "ThisMonth";
        ApplyReportPeriodPreset("ThisMonth");
    }

    [RelayCommand]
    private void OpenReportEvent(BanquetEventDto? evt)
    {
        if (evt is null) return;
        SelectedTabIndex = 0;
        SelectedEvent = Events.FirstOrDefault(x => x.Id == evt.Id) ?? evt;
        SelectedDetailTabIndex = 1;
    }

    [RelayCommand]
    private void ExportBanquetReportExcel()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                FileName = $"BaoCao_Tiec_{ReportFromDate:yyyyMMdd}_{ReportToDate:yyyyMMdd}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            using var workbook = new XLWorkbook();
            var overview = workbook.Worksheets.Add("Tong quan");
            overview.Cell("A1").Value = BanquetVenueName;
            overview.Cell("A1").Style.Font.Bold = true;
            overview.Cell("A1").Style.Font.FontSize = 16;
            overview.Cell("A2").Value = $"BÁO CÁO HOẠT ĐỘNG TIỆC · {ReportDateRangeText}";
            overview.Cell("A2").Style.Font.Bold = true;
            var metrics = new (string Label, object Value)[]
            {
                ("Tổng sự kiện", ReportTotalEvents),
                ("Sự kiện đã xác nhận", ReportConfirmedEvents),
                ("Tổng lượt khách", ReportGuestCount),
                ("Tổng giá trị tiệc", ReportRevenue),
                ("Đã đặt cọc", ReportDepositAmount),
                ("Còn phải thu", ReportOutstandingAmount),
                ("Chi phí ước tính", ReportEstimatedCost),
                ("Lãi gộp ước tính", ReportEstimatedGrossProfit),
                ("Biên lãi gộp ước tính", ReportEstimatedGrossMarginRate),
                ("Giá trị bình quân", ReportAverageEventValue)
            };
            for (var index = 0; index < metrics.Length; index++)
            {
                var row = index + 4;
                overview.Cell(row, 1).Value = metrics[index].Label;
                overview.Cell(row, 2).Value = XLCellValue.FromObject(metrics[index].Value);
            }
            overview.Range(4, 1, 3 + metrics.Length, 1).Style.Font.Bold = true;
            overview.Range(4, 2, 3 + metrics.Length, 2).Style.NumberFormat.Format = "#,##0";
            overview.Cell(12, 2).Style.NumberFormat.Format = "0.0%";
            overview.Columns().AdjustToContents();

            var eventSheet = workbook.Worksheets.Add("Su kien");
            var eventHeaders = new[] { "STT", "Mã sự kiện", "Ngày", "Giờ", "Loại tiệc", "Khách hàng", "Phòng", "Số khách", "Số bàn", "Trạng thái", "Tổng tiền", "Đã cọc", "Còn phải thu", "Chi phí ước tính", "Lãi gộp ước tính", "Biên lãi gộp" };
            for (var column = 0; column < eventHeaders.Length; column++) eventSheet.Cell(1, column + 1).Value = eventHeaders[column];
            for (var index = 0; index < ReportEvents.Count; index++)
            {
                var evt = ReportEvents[index];
                var row = index + 2;
                eventSheet.Cell(row, 1).Value = index + 1;
                eventSheet.Cell(row, 2).Value = evt.EventNo;
                eventSheet.Cell(row, 3).Value = evt.EventDate;
                eventSheet.Cell(row, 4).Value = $"{evt.StartTime:hh\\:mm} - {evt.EndTime:hh\\:mm}";
                eventSheet.Cell(row, 5).Value = evt.EventTypeDisplay;
                eventSheet.Cell(row, 6).Value = evt.CustomerName ?? string.Empty;
                eventSheet.Cell(row, 7).Value = evt.RoomName ?? "Chưa xếp phòng";
                eventSheet.Cell(row, 8).Value = evt.GuestCount;
                eventSheet.Cell(row, 9).Value = evt.TableCount;
                eventSheet.Cell(row, 10).Value = evt.StatusDisplay;
                eventSheet.Cell(row, 11).Value = evt.TotalAmount;
                eventSheet.Cell(row, 12).Value = evt.DepositAmount;
                eventSheet.Cell(row, 13).Value = evt.RemainingAmount;
                eventSheet.Cell(row, 14).Value = evt.EstimatedCost;
                eventSheet.Cell(row, 15).Value = evt.EstimatedGrossProfit;
                eventSheet.Cell(row, 16).Value = evt.EstimatedGrossMarginRate;
            }
            FormatReportWorksheet(eventSheet, eventHeaders.Length, ReportEvents.Count + 1);
            eventSheet.Column(3).Style.DateFormat.Format = "dd/MM/yyyy";
            eventSheet.Columns(11, 15).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]";
            eventSheet.Column(16).Style.NumberFormat.Format = "0.0%";

            var roomSheet = workbook.Worksheets.Add("Phong tiec");
            var roomHeaders = new[] { "STT", "Phòng / Sảnh", "Số sự kiện", "Tổng khách", "Tổng doanh thu", "Tỷ lệ sử dụng" };
            for (var column = 0; column < roomHeaders.Length; column++) roomSheet.Cell(1, column + 1).Value = roomHeaders[column];
            for (var index = 0; index < ReportRooms.Count; index++)
            {
                var room = ReportRooms[index];
                var row = index + 2;
                roomSheet.Cell(row, 1).Value = index + 1;
                roomSheet.Cell(row, 2).Value = room.RoomName;
                roomSheet.Cell(row, 3).Value = room.EventCount;
                roomSheet.Cell(row, 4).Value = room.GuestCount;
                roomSheet.Cell(row, 5).Value = room.Revenue;
                roomSheet.Cell(row, 6).Value = room.UtilizationRate / 100d;
            }
            FormatReportWorksheet(roomSheet, roomHeaders.Length, ReportRooms.Count + 1);
            roomSheet.Column(5).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]";
            roomSheet.Column(6).Style.NumberFormat.Format = "0.0%";

            var typeSheet = workbook.Worksheets.Add("Co cau loai tiec");
            var typeHeaders = new[] { "STT", "Loại tiệc", "Số sự kiện", "Tổng khách", "Doanh thu", "Tỷ trọng" };
            for (var column = 0; column < typeHeaders.Length; column++) typeSheet.Cell(1, column + 1).Value = typeHeaders[column];
            for (var index = 0; index < ReportTypes.Count; index++)
            {
                var type = ReportTypes[index];
                var row = index + 2;
                typeSheet.Cell(row, 1).Value = index + 1;
                typeSheet.Cell(row, 2).Value = type.EventTypeDisplay;
                typeSheet.Cell(row, 3).Value = type.EventCount;
                typeSheet.Cell(row, 4).Value = type.GuestCount;
                typeSheet.Cell(row, 5).Value = type.Revenue;
                typeSheet.Cell(row, 6).Value = type.Share / 100d;
            }
            FormatReportWorksheet(typeSheet, typeHeaders.Length, ReportTypes.Count + 1);
            typeSheet.Column(5).Style.NumberFormat.Format = "#,##0 [$đ-vi-VN]";
            typeSheet.Column(6).Style.NumberFormat.Format = "0.0%";

            workbook.SaveAs(dialog.FileName);
            ReportStatusText = $"Đã xuất báo cáo: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Export banquet report Excel failed");
            ReportStatusText = $"Không xuất được báo cáo: {ex.Message}";
        }
    }

    private static void FormatReportWorksheet(IXLWorksheet sheet, int columnCount, int rowCount)
    {
        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Font.Bold = true;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(1, rowCount), columnCount).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        sheet.Range(1, 1, Math.Max(1, rowCount), columnCount).Style.Border.BottomBorderColor = XLColor.FromHtml("#DCE5F0");
        sheet.Columns().AdjustToContents(8, 42);
        sheet.Row(1).Height = 24;
        sheet.Range(1, 1, Math.Max(1, rowCount), columnCount).SetAutoFilter();
    }

    private void ApplyReportPeriodPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || preset == "Custom")
        {
            RefreshReportCollections();
            return;
        }

        var today = DateTime.Today;
        _updatingReportPeriod = true;
        try
        {
            switch (preset)
            {
                case "ThisQuarter":
                    var quarterMonth = ((today.Month - 1) / 3) * 3 + 1;
                    ReportFromDate = new DateTime(today.Year, quarterMonth, 1);
                    ReportToDate = ReportFromDate.AddMonths(3).AddDays(-1);
                    break;
                case "ThisYear":
                    ReportFromDate = new DateTime(today.Year, 1, 1);
                    ReportToDate = new DateTime(today.Year, 12, 31);
                    break;
                case "Last12Months":
                    ReportFromDate = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
                    ReportToDate = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
                    break;
                default:
                    ReportFromDate = new DateTime(today.Year, today.Month, 1);
                    ReportToDate = ReportFromDate.AddMonths(1).AddDays(-1);
                    break;
            }
        }
        finally
        {
            _updatingReportPeriod = false;
        }
        RefreshReportCollections();
    }

    private IEnumerable<BanquetEventDto> GetReportFilteredEvents(DateTime fromDate, DateTime toDate)
    {
        var query = Events.Where(x => x.EventDate.Date >= fromDate.Date && x.EventDate.Date <= toDate.Date);
        if (!BanquetShowCompletedEvents) query = query.Where(x => NormalizeEventStatus(x.Status) != "Completed");
        if (SelectedReportRoomId.HasValue) query = query.Where(x => x.BanquetRoomId == SelectedReportRoomId.Value);
        if (!string.IsNullOrWhiteSpace(SelectedReportEventType)) query = query.Where(x => x.EventType == SelectedReportEventType);
        if (!string.IsNullOrWhiteSpace(SelectedReportStatus)) query = query.Where(x => NormalizeEventStatus(x.Status) == SelectedReportStatus);
        if (!string.IsNullOrWhiteSpace(ReportSearchText))
        {
            var keyword = ReportSearchText.Trim();
            query = query.Where(x => (x.EventNo?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                                     || (x.CustomerName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                                     || (x.Phone?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                                     || (x.RoomName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        return query;
    }

    private void RefreshReportCollections()
    {
        if (ReportFromDate.Date > ReportToDate.Date)
        {
            ReportStatusText = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
            Reset(ReportEvents, Array.Empty<BanquetEventDto>());
            NotifyReportSummaryChanged();
            return;
        }

        var events = GetReportFilteredEvents(ReportFromDate, ReportToDate)
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .ToArray();
        Reset(ReportEvents, events);

        var dayCount = Math.Max(1, (ReportToDate.Date - ReportFromDate.Date).Days + 1);
        var roomRows = events
            .GroupBy(x => new { x.BanquetRoomId, Name = x.RoomName ?? "Chưa xếp phòng" })
            .Select(group => new BanquetReportRoomVm
            {
                RoomId = group.Key.BanquetRoomId,
                RoomName = group.Key.Name,
                EventCount = group.Count(),
                GuestCount = group.Sum(x => x.GuestCount),
                Revenue = group.Sum(x => x.TotalAmount),
                UtilizationRate = group.Key.BanquetRoomId.HasValue
                    ? Math.Min(100d, group.Count() * 100d / (dayCount * 2d))
                    : 0d
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.RoomName)
            .ToArray();
        Reset(ReportRooms, roomRows);

        var accents = new[] { "#2563EB", "#059669", "#D97706", "#7C3AED", "#DB2777", "#64748B" };
        var typeRows = events
            .GroupBy(x => x.EventType)
            .Select((group, index) => new BanquetReportTypeVm
            {
                EventType = group.Key,
                EventTypeDisplay = group.First().EventTypeDisplay,
                EventCount = group.Count(),
                GuestCount = group.Sum(x => x.GuestCount),
                Revenue = group.Sum(x => x.TotalAmount),
                Share = events.Length == 0 ? 0d : group.Count() * 100d / events.Length,
                AccentBrush = accents[index % accents.Length]
            })
            .OrderByDescending(x => x.EventCount)
            .ToArray();
        Reset(ReportTypes, typeRows);

        var trendRows = BuildReportTrends(events, ReportFromDate, ReportToDate);
        Reset(ReportTrends, trendRows);

        var previousTo = ReportFromDate.Date.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(dayCount - 1));
        var previousRevenue = GetReportFilteredEvents(previousFrom, previousTo).Sum(x => x.TotalAmount);
        if (previousRevenue <= 0m)
        {
            ReportRevenueChangeText = ReportRevenue > 0m ? "Kỳ trước chưa phát sinh doanh thu" : "Chưa có dữ liệu so sánh";
            ReportRevenueChangeBrush = "#64748B";
        }
        else
        {
            var change = (ReportRevenue - previousRevenue) / previousRevenue * 100m;
            ReportRevenueChangeText = $"{(change >= 0 ? "+" : string.Empty)}{change:0.#}% so với kỳ trước";
            ReportRevenueChangeBrush = change >= 0 ? "#059669" : "#DC2626";
        }

        ReportStatusText = $"{events.Length:N0} sự kiện trong kỳ {ReportDateRangeText}.";
        NotifyReportSummaryChanged();
    }

    private static IReadOnlyList<BanquetReportTrendVm> BuildReportTrends(IReadOnlyCollection<BanquetEventDto> events, DateTime fromDate, DateTime toDate)
    {
        var dayCount = Math.Max(1, (toDate.Date - fromDate.Date).Days + 1);
        IEnumerable<IGrouping<DateTime, BanquetEventDto>> groups;
        Func<DateTime, string> label;
        Func<DateTime, string> period;
        if (dayCount <= 45)
        {
            groups = events.GroupBy(x => x.EventDate.Date);
            label = date => date.ToString("dd/MM", CultureInfo.InvariantCulture);
            period = date => date.ToString("dddd", new CultureInfo("vi-VN"));
        }
        else if (dayCount <= 180)
        {
            groups = events.GroupBy(x => GetRoomCalendarWeekStart(x.EventDate));
            label = date => $"Tuần {date:dd/MM}";
            period = date => $"{date:dd/MM} - {date.AddDays(6):dd/MM}";
        }
        else
        {
            groups = events.GroupBy(x => new DateTime(x.EventDate.Year, x.EventDate.Month, 1));
            label = date => $"T{date.Month:00}/{date:yy}";
            period = date => date.ToString("MMMM yyyy", new CultureInfo("vi-VN"));
        }

        var rows = groups.OrderBy(x => x.Key).Select(group => new BanquetReportTrendVm
        {
            Label = label(group.Key),
            PeriodText = period(group.Key),
            EventCount = group.Count(),
            Revenue = group.Sum(x => x.TotalAmount)
        }).ToArray();
        var maxRevenue = Math.Max(1m, rows.Select(x => x.Revenue).DefaultIfEmpty(1m).Max());
        foreach (var row in rows) row.BarWidth = Math.Max(3d, (double)(row.Revenue / maxRevenue) * 260d);
        return rows;
    }

    private void NotifyReportSummaryChanged()
    {
        OnPropertyChanged(nameof(ReportTotalEvents));
        OnPropertyChanged(nameof(ReportConfirmedEvents));
        OnPropertyChanged(nameof(ReportGuestCount));
        OnPropertyChanged(nameof(ReportRevenue));
        OnPropertyChanged(nameof(ReportDepositAmount));
        OnPropertyChanged(nameof(ReportOutstandingAmount));
        OnPropertyChanged(nameof(ReportEstimatedCost));
        OnPropertyChanged(nameof(ReportEstimatedGrossProfit));
        OnPropertyChanged(nameof(ReportEstimatedGrossMarginRate));
        OnPropertyChanged(nameof(ReportAverageEventValue));
        OnPropertyChanged(nameof(ReportDateRangeText));
        OnPropertyChanged(nameof(ReportRevenueChangeText));
        OnPropertyChanged(nameof(ReportRevenueChangeBrush));
        OnPropertyChanged(nameof(HasReportEvents));
        OnPropertyChanged(nameof(IsReportEventsEmpty));
    }

    [RelayCommand]
    private async Task SaveBanquetSettingsAsync()
    {
        BanquetSettingsValidationMessage = ValidateBanquetSettings();
        if (!string.IsNullOrWhiteSpace(BanquetSettingsValidationMessage)) return;

        try
        {
            await _service.SaveModuleSettingsAsync(CaptureBanquetSettingsDto());
            BanquetSettingsSavedText = $"Đã lưu cấu hình lúc {DateTime.Now:HH:mm dd/MM/yyyy}.";
            StatusText = "Đã lưu cấu hình Nhà hàng Tiệc.";
            RefreshReportCollections();
            RebuildRoomCalendars();
            RefreshDashboardCollections();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Save banquet settings failed");
            BanquetSettingsValidationMessage = $"Không lưu được cấu hình: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetBanquetSettings()
    {
        var result = MessageBox.Show(
            "Khôi phục các giá trị cấu hình mặc định? Thay đổi chỉ được ghi khi bạn nhấn Lưu cấu hình.",
            "Khôi phục mặc định",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        ApplyBanquetSettings(new BanquetModuleSettings());
        BanquetSettingsValidationMessage = string.Empty;
        BanquetSettingsSavedText = "Đã khôi phục giá trị mặc định, chưa lưu.";
    }

    [RelayCommand]
    private async Task ReloadBanquetSettingsAsync()
    {
        await LoadBanquetSettingsFromDatabaseAsync();
        StatusText = "Đã tải lại cấu hình Nhà hàng Tiệc.";
    }

    private BanquetModuleSettingsDto CaptureBanquetSettingsDto() => new()
    {
        VenueName = BanquetVenueName.Trim(), VenueCode = BanquetVenueCode.Trim(), Address = BanquetVenueAddress.Trim(),
        Phone = BanquetVenuePhone.Trim(), Email = BanquetVenueEmail.Trim(), Website = BanquetVenueWebsite.Trim(), TaxCode = BanquetVenueTaxCode.Trim(),
        GuestsPerTable = BanquetGuestsPerTable, DefaultDepositRate = BanquetDefaultDepositRate, DefaultVatRate = BanquetDefaultVatRate,
        HoldDays = BanquetHoldDays, ReminderDays = BanquetReminderDays, LunchStartTime = BanquetLunchStartTime.Trim(), DinnerStartTime = BanquetDinnerStartTime.Trim(),
        AutoGenerateEventNo = BanquetAutoGenerateEventNo, WarnRoomConflict = BanquetPreventRoomConflict,
        EnableDepositReminder = BanquetEnableDepositReminder
    };

    private async Task LoadBanquetSettingsFromDatabaseAsync()
    {
        var settings = await _service.GetModuleSettingsAsync();
        ApplyBanquetSettings(new BanquetModuleSettings
        {
            VenueName = settings.VenueName, VenueCode = settings.VenueCode, Address = settings.Address, Phone = settings.Phone,
            Email = settings.Email, Website = settings.Website, TaxCode = settings.TaxCode, GuestsPerTable = settings.GuestsPerTable,
            DefaultDepositRate = settings.DefaultDepositRate, DefaultVatRate = settings.DefaultVatRate, HoldDays = settings.HoldDays,
            ReminderDays = settings.ReminderDays, LunchStartTime = settings.LunchStartTime, DinnerStartTime = settings.DinnerStartTime,
            AutoGenerateEventNo = settings.AutoGenerateEventNo, PreventRoomConflict = settings.WarnRoomConflict,
            EnableDepositReminder = settings.EnableDepositReminder
        });
        BanquetSettingsSavedText = "Đã tải cấu hình dùng chung từ cơ sở dữ liệu.";
        BanquetSettingsValidationMessage = string.Empty;
    }

    [RelayCommand]
    private void SetBanquetSettingsSection(string? section)
    {
        BanquetSettingsSection = section switch
        {
            "Operation" => 1,
            "Automation" => 2,
            _ => 0
        };
    }

    public bool IsBanquetGeneralSettings => BanquetSettingsSection == 0;
    public bool IsBanquetOperationSettings => BanquetSettingsSection == 1;
    public bool IsBanquetAutomationSettings => BanquetSettingsSection == 2;

    partial void OnBanquetSettingsSectionChanged(int value)
    {
        OnPropertyChanged(nameof(IsBanquetGeneralSettings));
        OnPropertyChanged(nameof(IsBanquetOperationSettings));
        OnPropertyChanged(nameof(IsBanquetAutomationSettings));
    }

    private string ValidateBanquetSettings()
    {
        if (string.IsNullOrWhiteSpace(BanquetVenueName)) return "Tên đơn vị không được để trống.";
        if (string.IsNullOrWhiteSpace(BanquetVenueCode)) return "Mã đơn vị không được để trống.";
        if (!string.IsNullOrWhiteSpace(BanquetVenueEmail) && !BanquetVenueEmail.Contains('@')) return "Email chưa đúng định dạng.";
        if (BanquetGuestsPerTable is < 1 or > 30) return "Số khách mỗi bàn phải từ 1 đến 30.";
        if (BanquetDefaultDepositRate is < 0m or > 100m) return "Tỷ lệ đặt cọc phải từ 0% đến 100%.";
        if (BanquetDefaultVatRate is < 0m or > 100m) return "Thuế VAT phải từ 0% đến 100%.";
        if (BanquetHoldDays is < 1 or > 90) return "Số ngày giữ chỗ phải từ 1 đến 90.";
        if (BanquetReminderDays is < 0 or > 90) return "Số ngày nhắc trước phải từ 0 đến 90.";
        if (!TimeSpan.TryParse(BanquetLunchStartTime, out _)) return "Giờ bắt đầu ca Trưa chưa hợp lệ.";
        if (!TimeSpan.TryParse(BanquetDinnerStartTime, out _)) return "Giờ bắt đầu ca Tối chưa hợp lệ.";
        return string.Empty;
    }

    private BanquetModuleSettings CaptureBanquetSettings() => new()
    {
        VenueName = BanquetVenueName.Trim(),
        VenueCode = BanquetVenueCode.Trim(),
        Address = BanquetVenueAddress.Trim(),
        Phone = BanquetVenuePhone.Trim(),
        Email = BanquetVenueEmail.Trim(),
        Website = BanquetVenueWebsite.Trim(),
        TaxCode = BanquetVenueTaxCode.Trim(),
        GuestsPerTable = BanquetGuestsPerTable,
        DefaultDepositRate = BanquetDefaultDepositRate,
        DefaultVatRate = BanquetDefaultVatRate,
        HoldDays = BanquetHoldDays,
        ReminderDays = BanquetReminderDays,
        LunchStartTime = BanquetLunchStartTime.Trim(),
        DinnerStartTime = BanquetDinnerStartTime.Trim(),
        AutoGenerateEventNo = BanquetAutoGenerateEventNo,
        PreventRoomConflict = BanquetPreventRoomConflict,
        EnableDepositReminder = BanquetEnableDepositReminder,
        ShowCompletedEvents = BanquetShowCompletedEvents
    };

    private void ApplyBanquetSettings(BanquetModuleSettings settings)
    {
        BanquetVenueName = settings.VenueName;
        BanquetVenueCode = settings.VenueCode;
        BanquetVenueAddress = settings.Address;
        BanquetVenuePhone = settings.Phone;
        BanquetVenueEmail = settings.Email;
        BanquetVenueWebsite = settings.Website;
        BanquetVenueTaxCode = settings.TaxCode;
        BanquetGuestsPerTable = settings.GuestsPerTable;
        BanquetDefaultDepositRate = settings.DefaultDepositRate;
        BanquetDefaultVatRate = settings.DefaultVatRate;
        BanquetHoldDays = settings.HoldDays;
        BanquetReminderDays = settings.ReminderDays;
        BanquetLunchStartTime = settings.LunchStartTime;
        BanquetDinnerStartTime = settings.DinnerStartTime;
        BanquetAutoGenerateEventNo = settings.AutoGenerateEventNo;
        BanquetPreventRoomConflict = settings.PreventRoomConflict;
        BanquetEnableDepositReminder = settings.EnableDepositReminder;
        BanquetShowCompletedEvents = settings.ShowCompletedEvents;
    }

    private void LoadBanquetSettings()
    {
        try
        {
            if (!File.Exists(BanquetSettingsFilePath))
            {
                ApplyBanquetSettings(new BanquetModuleSettings());
                BanquetSettingsSavedText = "Đang sử dụng cấu hình mặc định.";
                return;
            }

            var json = File.ReadAllText(BanquetSettingsFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<BanquetModuleSettings>(json) ?? new BanquetModuleSettings();
            ApplyBanquetSettings(settings);
            BanquetSettingsSavedText = settings.UpdatedAt.HasValue
                ? $"Lần lưu gần nhất: {settings.UpdatedAt:HH:mm dd/MM/yyyy}."
                : "Đã tải cấu hình đã lưu.";
            BanquetSettingsValidationMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Load banquet settings failed");
            ApplyBanquetSettings(new BanquetModuleSettings());
            BanquetSettingsValidationMessage = "Không đọc được tệp cấu hình; đang dùng giá trị mặc định.";
        }
    }

    [RelayCommand]
    private void ResetEventFilters()
    {
        EventSearchText = string.Empty;
        SelectedEventStatusFilter = null;
        SelectedEventTypeFilter = null;
        SelectedEventRoomFilterId = null;
        EventFilterFromDate = DateTime.Today.AddMonths(-1);
        EventFilterToDate = DateTime.Today.AddMonths(6);
        RefreshEventCollections();
    }

    private bool _isCreatingEvent;
    public bool IsCreatingEvent => _isCreatingEvent || SelectedEvent is not null;
    public bool ShowEventEditor => IsCreatingEvent;

    [RelayCommand]
    private void NewEvent()
    {
        SelectedEvent = null;
        ResetEventEditor();
        ResetDetailCollections();
        _isCreatingEvent = true;
        OnPropertyChanged(nameof(IsCreatingEvent));
        OnPropertyChanged(nameof(ShowEventEditor));
        SelectedTabIndex = 0;
        StatusText = "Đang tạo sự kiện tiệc mới. Nhập thông tin và nhấn Lưu.";
    }

    [RelayCommand]
    private void OpenEventFromSummary(BanquetEventDto? evt)
    {
        if (evt is null)
        {
            return;
        }

        SelectedTabIndex = 0;
        SelectedEvent = evt;
    }

    [RelayCommand]
    private void OpenCalendarEvent(BanquetScheduleCellVm? cell)
    {
        if (cell?.EventId is null)
        {
            return;
        }

        var evt = Events.FirstOrDefault(x => x.Id == cell.EventId.Value);
        if (evt is not null)
        {
            SelectedTabIndex = 0;
            SelectedEvent = evt;
        }
    }

    [RelayCommand]
    private void ChangeRoomCalendarWeek(string? direction)
    {
        RoomCalendarWeekStart = RoomCalendarWeekStart.AddDays(direction == "Previous" ? -7 : 7);
        BuildRoomCalendar();
        StatusText = $"Đang xem lịch phòng từ {RoomCalendarRangeText}.";
    }

    [RelayCommand]
    private void GoToCurrentRoomCalendarWeek()
    {
        RoomCalendarWeekStart = GetRoomCalendarWeekStart(DateTime.Today);
        BuildRoomCalendar();
        StatusText = $"Đang xem lịch phòng tuần hiện tại ({RoomCalendarRangeText}).";
    }

    [RelayCommand]
    private void SetRoomCalendarView(string? mode)
    {
        RoomCalendarViewMode = mode == "Year" ? 1 : 0;
        if (RoomCalendarViewMode == 1) BuildRoomCalendarYear();
    }

    [RelayCommand]
    private void ChangeRoomCalendarYear(string? direction)
    {
        RoomCalendarYear += direction == "Previous" ? -1 : 1;
    }

    [RelayCommand]
    private void GoToCurrentRoomCalendarYear()
    {
        RoomCalendarYear = DateTime.Today.Year;
        BuildRoomCalendarYear();
    }

    [RelayCommand]
    private void OpenRoomCalendarDay(BanquetYearDayVm? day)
    {
        if (day is null || !day.IsCurrentMonth) return;
        RoomCalendarWeekStart = GetRoomCalendarWeekStart(day.Date);
        RoomCalendarViewMode = 0;
        BuildRoomCalendar();
        StatusText = $"Đang xem chi tiết tuần chứa ngày {day.Date:dd/MM/yyyy}.";
    }

    [RelayCommand]
    private void ResetRoomCalendarFilters()
    {
        SelectedRoomCalendarRoomId = null;
        SelectedRoomCalendarStatus = null;
        SelectedRoomCalendarEventType = null;
        SelectedRoomCalendarShift = null;
        RebuildRoomCalendars();
    }

    [RelayCommand]
    private void NewEventFromRoomCalendar()
    {
        NewEvent();
        SelectedDetailTabIndex = 1;
    }

    [RelayCommand]
    private void OpenRoomCalendarSlot(BanquetRoomCalendarSlotVm? slot)
    {
        if (slot is null) return;

        if (slot.SourceEvent is not null)
        {
            SelectedTabIndex = 0;
            SelectedEvent = Events.FirstOrDefault(x => x.Id == slot.SourceEvent.Id) ?? slot.SourceEvent;
            SelectedDetailTabIndex = 1;
            return;
        }

        NewEvent();
        SelectedRoom = Rooms.FirstOrDefault(x => x.Id == slot.RoomId);
        SelectedEventRoomId = slot.RoomId;
        EventDate = slot.Date;
        StartTimeText = slot.Shift == "Lunch" ? "11:00" : "18:00";
        EndTimeText = slot.Shift == "Lunch" ? "14:00" : "21:00";
        SelectedDetailTabIndex = 1;
        StatusText = $"Đang tạo sự kiện ca {slot.ShiftLabel.ToLowerInvariant()} tại {slot.RoomName} ngày {slot.Date:dd/MM/yyyy}.";
    }

    [RelayCommand]
    private void OpenEventFromAlert(BanquetAlertVm? alert)
    {
        if (alert?.EventId is null)
        {
            return;
        }

        var evt = Events.FirstOrDefault(x => x.Id == alert.EventId.Value);
        if (evt is not null)
        {
            SelectedTabIndex = 0;
            SelectedEvent = evt;
        }
    }

    [RelayCommand]
    private void OpenDetailTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            SelectedDetailTabIndex = index;
            SelectedTabIndex = 0;
        }
    }

    [RelayCommand]
    private void OpenFinanceFromEvent(BanquetFinanceSummaryVm? row)
    {
        if (row?.EventId is null)
        {
            return;
        }

        var evt = Events.FirstOrDefault(x => x.Id == row.EventId);
        if (evt is not null)
        {
            SelectedTabIndex = 0;
            SelectedEvent = evt;
            SelectedDetailTabIndex = 4;
        }
    }

    [RelayCommand]
    private void OpenContractFromEvent(BanquetContractSummaryVm? row)
    {
        if (row?.EventId is null)
        {
            return;
        }

        var evt = Events.FirstOrDefault(x => x.Id == row.EventId);
        if (evt is not null)
        {
            SelectedTabIndex = 0;
            SelectedEvent = evt;
            SelectedDetailTabIndex = 5;
        }
    }

    [RelayCommand]
    private async Task SaveServiceLineAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi thêm dịch vụ.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
            {
                BanquetEventId = SelectedEvent.Id,
                ServiceType = ServiceType,
                Description = ServiceDescription,
                UnitName = ServiceUnitName,
                Qty = ServiceQty,
                UnitPrice = ServiceUnitPrice,
                CostPrice = ServiceCostPrice,
                VatRate = ServiceVatRate,
                Note = ServiceNote
            });
            ResetServiceEditor();
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            StatusText = "Đã lưu dịch vụ tiệc.";
        });
    }

    [RelayCommand]
    private async Task DeleteServiceLineAsync(BanquetServiceLineDto? line)
    {
        if (line is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.DeleteServiceLineAsync(line.Id);
            await RefreshPreservingSelectionAsync(SelectedEvent?.Id, SelectedRoom?.Id);
            StatusText = "Đã xóa dịch vụ tiệc.";
        });
    }

    [RelayCommand]
    private async Task AttachMenuPackageAsync(BanquetMenuPackageDto? package)
    {
        package ??= SelectedMenuPackage;
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi gắn thực đơn.";
            return;
        }

        if (package is null)
        {
            StatusText = "Chọn gói thực đơn trước khi gắn.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.AttachMenuPackageAsync(new AttachBanquetMenuRequest
            {
                BanquetEventId = SelectedEvent.Id,
                BanquetMenuPackageId = package.Id
            });
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            StatusText = $"Đã gắn thực đơn {package.Name}.";
        });
    }

    internal async Task AttachPresetMenuSelectionAsync(BanquetMenuPackageDto package)
    {
        await AttachMenuPackageAsync(package);
    }

    [RelayCommand]
    private async Task DetachMenuPackageAsync(BanquetEventMenuDto? menu)
    {
        if (menu is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _service.DetachMenuPackageAsync(menu.Id);
            await RefreshPreservingSelectionAsync(SelectedEvent?.Id, SelectedRoom?.Id);
            StatusText = "Đã gỡ thực đơn khỏi sự kiện.";
        });
    }

    [RelayCommand]
    private async Task FinalizeSettlementAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi quyết toán.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var result = await _service.FinalizeSettlementAsync(SelectedEvent.Id, DepositPaymentMethod, DepositReceiver, DepositNote);
            await RefreshPreservingSelectionAsync(result.Id, result.BanquetRoomId);
            StatusText = $"Đã quyết toán sự kiện {result.EventNo}.";
        });
    }

    [RelayCommand]
    private async Task CancelSelectedEventAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi hủy.";
            return;
        }

        var activeDeposit = EventDeposits.Sum(x => x.NetAmount);
        var warning = activeDeposit > 0m
            ? $"\n\nTiệc còn {activeDeposit:N0} đ tiền cọc hiệu lực. Hủy tiệc không tự hoàn tiền; hãy thực hiện Hoàn cọc để tạo chứng từ chi và lưu dấu vết."
            : string.Empty;
        var confirmation = MessageBox.Show(
            $"Bạn chắc chắn muốn hủy tiệc {SelectedEvent.EventNo} - {SelectedEvent.CustomerName}?{warning}",
            "Xác nhận hủy tiệc",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        await RunBusyAsync(async () =>
        {
            var result = await _service.UpdateEventStatusAsync(SelectedEvent.Id, "Cancelled");
            await RefreshPreservingSelectionAsync(result.Id, result.BanquetRoomId);
            StatusText = $"Đã chuyển sự kiện {result.EventNo} sang trạng thái hủy.";
        });
    }

    [RelayCommand]
    private async Task MarkPreparingAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi chuyển sang chuẩn bị.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var result = await _service.UpdateEventStatusAsync(SelectedEvent.Id, "Preparing");
            await RefreshPreservingSelectionAsync(result.Id, result.BanquetRoomId);
            StatusText = $"Đã chuyển {result.EventNo} sang trạng thái chuẩn bị.";
        });
    }

    [RelayCommand]
    private async Task ToggleTimelineStatusAsync(BanquetTimelineItemDto? item)
    {
        if (item is null)
        {
            return;
        }

        var nextStatus = string.Equals(item.Status, "Done", StringComparison.OrdinalIgnoreCase) ? "Planned" : "Done";
        await RunBusyAsync(async () =>
        {
            await _service.UpdateTimelineItemStatusAsync(item.Id, nextStatus);
            if (SelectedEvent is not null)
            {
                await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            }
            StatusText = $"Đã cập nhật timeline “{item.Title}”.";
        });
    }

    private void UpdateWorkflowSteps()
    {
        var steps = new[]
        {
            CreateWorkflowStep("01", "Tiếp nhận", SelectedEvent is not null, SelectedEvent is null, SelectedEvent?.CustomerName ?? "Chưa chọn khách"),
            CreateWorkflowStep("02", "Báo giá", EventMenus.Any() || EventServices.Any(), SelectedEvent is not null && !(EventMenus.Any() || EventServices.Any()), SelectedEvent is null ? "Chưa có báo giá" : $"{SelectedEvent.TotalAmount:N0} đ"),
            CreateWorkflowStep("03", "Giữ lịch", string.Equals(SelectedEvent?.Status, "Hold", StringComparison.OrdinalIgnoreCase), SelectedEvent is not null && string.Equals(SelectedEvent?.Status, "Draft", StringComparison.OrdinalIgnoreCase), HoldRoomEventCount > 0 ? $"{HoldRoomEventCount} lịch trong ngày" : "Chưa giữ lịch"),
            CreateWorkflowStep("04", "Đặt cọc", EventDeposits.Any(), SelectedEvent is not null && !EventDeposits.Any(), EventDeposits.Any() ? $"{SelectedEventPaidAmount:N0} đ" : "Chưa thu cọc"),
            CreateWorkflowStep("05", "Hợp đồng", EventContracts.Any(), SelectedEvent is not null && !EventContracts.Any(), EventContracts.FirstOrDefault()?.ContractNo ?? "Chưa tạo hợp đồng"),
            CreateWorkflowStep("06", "Chuẩn bị", ChecklistItems.Any(), SelectedEvent is not null && !ChecklistItems.Any(), $"{ChecklistItems.Count(x => x.IsDone)}/{ChecklistItems.Count} mục"),
            CreateWorkflowStep("07", "Thực hiện", string.Equals(SelectedEvent?.Status, "Completed", StringComparison.OrdinalIgnoreCase), string.Equals(SelectedEvent?.Status, "Preparing", StringComparison.OrdinalIgnoreCase), SelectedEvent?.Status ?? "Chưa bắt đầu"),
            CreateWorkflowStep("08", "Nghiệm thu", SelectedEvent is not null && string.Equals(SelectedEvent.Status, "Completed", StringComparison.OrdinalIgnoreCase), false, "Chờ chốt thực tế"),
            CreateWorkflowStep("09", "Thanh toán", SelectedEvent is not null && SelectedEvent.RemainingAmount <= 0m, SelectedEvent is not null && SelectedEvent.RemainingAmount > 0m, SelectedEvent is null ? "Chưa có dữ liệu" : $"Còn {SelectedEvent.RemainingAmount:N0} đ"),
            CreateWorkflowStep("10", "Thanh lý", SelectedEvent is not null && SelectedEvent.RemainingAmount <= 0m && EventContracts.Any(), false, EventContracts.Any() ? "Đã có hồ sơ hợp đồng" : "Chưa có biên bản")
        };

        Reset(EventWorkflowSteps, steps);
    }

    private static BanquetWorkflowStepVm CreateWorkflowStep(string code, string title, bool isDone, bool isCurrent, string description)
    {
        var state = isDone ? "Done" : isCurrent ? "Current" : "Pending";
        return new BanquetWorkflowStepVm
        {
            Code = code,
            Title = title,
            Description = description,
            State = state
        };
    }

    [RelayCommand]
    private void NavigateWorkflowStep(string? code)
    {
        SelectedDetailTabIndex = code switch
        {
            "01" => 0,
            "02" => 2,
            "03" => 1,
            "04" => 6,
            "05" => 7,
            "06" => 4,
            "07" => 5,
            "08" => 6,
            "09" => 6,
            "10" => 7,
            _ => 0
        };
        SelectedTabIndex = 0;
    }

    [RelayCommand]
    private async Task SaveChecklistItemAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi thêm checklist.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewChecklistTitle))
        {
            StatusText = "Nhập tên công việc checklist.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var existing = EditingChecklistItemId.HasValue
                ? ChecklistItems.FirstOrDefault(x => x.Id == EditingChecklistItemId.Value)
                : null;
            await _service.SaveChecklistItemAsync(new SaveBanquetChecklistItemRequest
            {
                Id = EditingChecklistItemId,
                BanquetEventId = SelectedEvent.Id,
                Title = NewChecklistTitle.Trim(),
                Note = BuildChecklistNote(NewChecklistDepartment, NewChecklistNote),
                SortOrder = existing?.SortOrder ?? ChecklistItems.Count + 1
            });
            var wasEditing = EditingChecklistItemId.HasValue;
            ResetChecklistEditor();
            await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            StatusText = wasEditing ? "Đã cập nhật công việc checklist." : "Đã thêm công việc checklist.";
        });
    }

    [RelayCommand]
    private void EditChecklistItem(BanquetChecklistDto? item)
    {
        if (item is null) return;
        EditingChecklistItemId = item.Id;
        NewChecklistDepartment = ResolveChecklistGroup(item);
        NewChecklistTitle = item.Title;
        NewChecklistNote = StripChecklistDepartmentMarker(item.Note);
    }

    [RelayCommand]
    private void CancelChecklistEdit() => ResetChecklistEditor();

    private void ResetChecklistEditor()
    {
        EditingChecklistItemId = null;
        NewChecklistDepartment = "Kinh doanh";
        NewChecklistTitle = string.Empty;
        NewChecklistNote = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteChecklistItemAsync(BanquetChecklistDto? item)
    {
        if (item is null) return;
        await RunBusyAsync(async () =>
        {
            await _service.DeleteChecklistItemAsync(item.Id);
            if (EditingChecklistItemId == item.Id)
            {
                ResetChecklistEditor();
            }
            if (SelectedEvent is not null)
            {
                await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            }
            StatusText = "Đã xóa checklist.";
        });
    }

    [RelayCommand]
    private async Task SaveTimelineItemAsync()
    {
        if (SelectedEvent is null)
        {
            StatusText = "Chọn sự kiện trước khi thêm mốc thực hiện.";
            return;
        }

        if (!TimeSpan.TryParse(TimelineStartTimeText, out var startTime))
        {
            StatusText = "Giờ timeline không hợp lệ.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TimelineTitle))
        {
            StatusText = "Nhập tên mốc vận hành.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var existing = EditingTimelineItemId.HasValue
                ? EventTimelineItems.FirstOrDefault(x => x.Id == EditingTimelineItemId.Value)
                : null;
            await _service.SaveTimelineItemAsync(new SaveBanquetTimelineItemRequest
            {
                Id = EditingTimelineItemId,
                BanquetEventId = SelectedEvent.Id,
                StartTime = startTime,
                Title = TimelineTitle.Trim(),
                Description = TimelineDescription,
                ResponsiblePerson = TimelineResponsiblePerson,
                SortOrder = existing?.SortOrder ?? EventTimelineItems.Count + 1,
                Status = existing?.Status ?? "Planned"
            });
            var wasEditing = EditingTimelineItemId.HasValue;
            ResetTimelineEditor();
            await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            StatusText = wasEditing ? "Đã cập nhật mốc vận hành." : "Đã thêm mốc vận hành.";
        });
    }

    [RelayCommand]
    private void EditTimelineItem(BanquetTimelineItemDto? item)
    {
        if (item is null) return;
        EditingTimelineItemId = item.Id;
        TimelineStartTimeText = item.StartTime.ToString(@"hh\:mm");
        TimelineTitle = item.Title;
        TimelineDescription = item.Description ?? string.Empty;
        TimelineResponsiblePerson = item.ResponsiblePerson ?? string.Empty;
    }

    [RelayCommand]
    private void CancelTimelineEdit() => ResetTimelineEditor();

    private void ResetTimelineEditor()
    {
        EditingTimelineItemId = null;
        TimelineStartTimeText = SelectedEvent?.StartTime.ToString(@"hh\:mm") ?? "17:00";
        TimelineTitle = string.Empty;
        TimelineDescription = string.Empty;
        TimelineResponsiblePerson = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteTimelineItemAsync(BanquetTimelineItemDto? item)
    {
        if (item is null) return;
        await RunBusyAsync(async () =>
        {
            await _service.DeleteTimelineItemAsync(item.Id);
            if (EditingTimelineItemId == item.Id)
            {
                ResetTimelineEditor();
            }
            if (SelectedEvent is not null)
            {
                await LoadSelectedEventDetailAsync(SelectedEvent.Id);
            }
            StatusText = "Đã xóa mốc thực hiện.";
        });
    }

    private void UpdateHoldRoomEvents()
    {
        var selectedRoomId = SelectedEventRoomId ?? SelectedRoom?.Id;
        var filtered = Events
            .Where(x => x.EventDate.Date == HoldCalendarDate.Date
                        && (selectedRoomId is null || x.BanquetRoomId == selectedRoomId)
                        && !IsCancelled(x.Status))
            .OrderBy(x => x.StartTime)
            .ToArray();

        Reset(HoldRoomEvents, filtered);
        OnPropertyChanged(nameof(HoldRoomEventCount));
        OnPropertyChanged(nameof(HoldAvailabilityText));
        OnPropertyChanged(nameof(HasHoldRoomEvents));
        OnPropertyChanged(nameof(IsHoldRoomEventsEmpty));
    }

    private void RefreshEventCollections()
    {
        IEnumerable<BanquetEventDto> query = Events;

        query = query.Where(x => x.EventDate.Date >= EventFilterFromDate.Date && x.EventDate.Date <= EventFilterToDate.Date);

        if (!string.IsNullOrWhiteSpace(SelectedEventStatusFilter))
        {
            query = query.Where(x => string.Equals(NormalizeEventStatus(x.Status), SelectedEventStatusFilter, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            query = query.Where(x => !IsCancelled(x.Status));
        }

        if (!string.IsNullOrWhiteSpace(SelectedEventTypeFilter))
        {
            query = query.Where(x => string.Equals(x.EventType, SelectedEventTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedEventRoomFilterId.HasValue)
        {
            query = query.Where(x => x.BanquetRoomId == SelectedEventRoomFilterId.Value);
        }

        if (!string.IsNullOrWhiteSpace(EventSearchText))
        {
            var keyword = EventSearchText.Trim();
            query = query.Where(x =>
                x.EventNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.CustomerName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.RoomName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(GlobalSearchText))
        {
            var gKeyword = GlobalSearchText.Trim();
            query = query.Where(x =>
                x.EventNo.Contains(gKeyword, StringComparison.OrdinalIgnoreCase) ||
                (x.CustomerName?.Contains(gKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.RoomName?.Contains(gKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.EventType?.Contains(gKeyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filtered = query
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .ToArray();
        Reset(FilteredEvents, filtered);
        EventsPager.SetSource(filtered);
        OnPropertyChanged(nameof(HasFilteredEvents));
        OnPropertyChanged(nameof(IsFilteredEventsEmpty));
        OnPropertyChanged(nameof(HasEvents));
    }

    private void RefreshDashboardCollections()
    {
        var today = DateTime.Today;
        Reset(DashboardTodayEvents, Events
            .Where(x => x.EventDate.Date == today && !IsCancelled(x.Status))
            .OrderBy(x => x.StartTime)
            .ToArray());

        Reset(DashboardActionEvents, Events
            .Where(x => !IsCancelled(x.Status))
            .Where(x =>
                string.Equals(NormalizeEventStatus(x.Status), "Draft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeEventStatus(x.Status), "Hold", StringComparison.OrdinalIgnoreCase) ||
                x.RemainingAmount > 0m ||
                x.EventDate.Date <= today.AddDays(7))
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .Take(8)
            .ToArray());

        var alerts = new List<BanquetAlertVm>();
        var activeEvents = Events.Where(x => !IsCancelled(x.Status)).ToList();

        foreach (var conflictGroup in activeEvents
                     .Where(x => x.BanquetRoomId.HasValue)
                     .GroupBy(x => new { x.BanquetRoomId, Day = x.EventDate.Date }))
        {
            var ordered = conflictGroup.OrderBy(x => x.StartTime).ToArray();
            for (var i = 0; i < ordered.Length - 1; i++)
            {
                if (ordered[i].EndTime > ordered[i + 1].StartTime)
                {
                    alerts.Add(new BanquetAlertVm
                    {
                        Level = "Danger",
                        Title = $"Trùng lịch {ordered[i].RoomName}",
                        Detail = $"{ordered[i].EventDate:dd/MM/yyyy} · {ordered[i].EventNo} chồng giờ với {ordered[i + 1].EventNo}",
                        EventId = ordered[i].Id
                    });
                }
            }
        }

        alerts.AddRange(activeEvents
            .Where(x => BanquetEnableDepositReminder
                        && x.EventDate.Date <= today.AddDays(BanquetReminderDays)
                        && x.DepositAmount <= 0m)
            .OrderBy(x => x.EventDate)
            .Select(x => new BanquetAlertVm
            {
                Level = "Warning",
                Title = $"Chưa cọc: {x.CustomerName}",
                Detail = $"{x.EventDate:dd/MM/yyyy} · {x.RoomName} · {x.EventNo}",
                EventId = x.Id
            }));

        alerts.AddRange(activeEvents
            .Where(x => x.RemainingAmount > 0m && string.Equals(NormalizeEventStatus(x.Status), "Completed", StringComparison.OrdinalIgnoreCase))
            .Select(x => new BanquetAlertVm
            {
                Level = "Info",
                Title = $"Còn phải thu: {x.CustomerName}",
                Detail = $"{x.EventNo} · còn {x.RemainingAmount:N0} đ",
                EventId = x.Id
            }));

        Reset(DashboardAlerts, alerts.Take(8).ToArray());
        OnPropertyChanged(nameof(HasDashboardAlerts));
        OnPropertyChanged(nameof(IsDashboardAlertsEmpty));

        var revenueBars = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var day = today.AddDays(offset);
                var amount = Events.Where(x => x.EventDate.Date == day && !IsCancelled(x.Status)).Sum(x => x.TotalAmount);
                return new BanquetRevenueBarVm
                {
                    Label = day.ToString("dd/MM", CultureInfo.InvariantCulture),
                    Amount = amount
                };
            })
            .ToArray();

        var maxBarValue = Math.Max(1m, revenueBars.Max(x => x.Amount));
        foreach (var bar in revenueBars)
        {
            bar.BarHeight = Math.Max(4.0, (double)(bar.Amount / maxBarValue) * 72.0);
        }

        Reset(RevenueChartBars, revenueBars);
        BuildOverviewCalendar();
        OnPropertyChanged(nameof(MaxRevenueBarValue));
        OnPropertyChanged(nameof(UpcomingEventsCount));
        OnPropertyChanged(nameof(ForecastRevenue));
        OnPropertyChanged(nameof(ImportantAlertCount));
        OnPropertyChanged(nameof(HasDashboardEvents));
        OnPropertyChanged(nameof(IsDashboardEventsEmpty));
        OnPropertyChanged(nameof(OverviewCalendarRangeText));
        OnPropertyChanged(nameof(TodayHeaderText));
    }

    private void RefreshSidebarCollections()
    {
        var customers = Events
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
            .GroupBy(x => new { Name = x.CustomerName!.Trim(), x.Phone })
            .Select(g =>
            {
                var ordered = g.OrderByDescending(x => x.EventDate).ToArray();
                var next = g.Where(x => x.EventDate.Date >= DateTime.Today && !IsCancelled(x.Status)).OrderBy(x => x.EventDate).FirstOrDefault();
                return new BanquetCustomerSummaryVm
                {
                    CustomerName = g.Key.Name,
                    Phone = g.Key.Phone,
                    EventCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    LastEventDate = ordered.FirstOrDefault()?.EventDate,
                    NextEventDate = next?.EventDate,
                    LastEventNo = ordered.FirstOrDefault()?.EventNo ?? string.Empty
                };
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ThenBy(x => x.CustomerName)
            .ToArray();
        Reset(CustomerSummaries, customers);

        var contracts = Events
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .Select(x => new BanquetContractSummaryVm
            {
                EventId = x.Id,
                EventNo = x.EventNo,
                CustomerName = x.CustomerName ?? "Chưa có khách",
                EventDate = x.EventDate,
                RoomName = x.RoomName ?? "Chưa chọn sảnh",
                TotalAmount = x.TotalAmount,
                DepositAmount = x.DepositAmount,
                RemainingAmount = x.RemainingAmount,
                Status = NormalizeEventStatus(x.Status)
            })
            .ToArray();
        Reset(ContractSummaries, contracts);

        var finance = Events
            .OrderByDescending(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .Select(x => new BanquetFinanceSummaryVm
            {
                EventId = x.Id,
                EventNo = x.EventNo,
                CustomerName = x.CustomerName ?? "Chưa có khách",
                EventDate = x.EventDate,
                TotalAmount = x.TotalAmount,
                DepositAmount = x.DepositAmount,
                RemainingAmount = x.RemainingAmount,
                Status = NormalizeEventStatus(x.Status)
            })
            .ToArray();
        Reset(FinanceSummaries, finance);
        OnPropertyChanged(nameof(HasFinanceSummaries));
        OnPropertyChanged(nameof(IsFinanceSummariesEmpty));
    }

    private void RebuildRoomFilterOptions()
    {
        EventRoomFilterOptions.Clear();
        EventRoomFilterOptions.Add(new LookupOption<Guid?>("Tất cả sảnh", null));
        foreach (var room in Rooms.OrderBy(x => x.Name))
        {
            EventRoomFilterOptions.Add(new LookupOption<Guid?>(room.Name, room.Id));
        }
    }

    private void RebuildRoomCalendarFilterOptions()
    {
        var selectedRoomId = SelectedRoomCalendarRoomId;
        RoomCalendarRoomFilterOptions.Clear();
        RoomCalendarRoomFilterOptions.Add(new LookupOption<Guid?>("Tất cả phòng", null));
        foreach (var room in Rooms.Where(x => x.IsActive).OrderBy(x => x.Name))
        {
            RoomCalendarRoomFilterOptions.Add(new LookupOption<Guid?>(room.Name, room.Id));
        }

        if (selectedRoomId.HasValue && !Rooms.Any(x => x.Id == selectedRoomId.Value && x.IsActive))
        {
            SelectedRoomCalendarRoomId = null;
        }
    }

    private void RebuildRoomCalendars()
    {
        BuildRoomCalendar();
        BuildRoomCalendarYear();
    }

    private IEnumerable<BanquetEventDto> GetFilteredRoomCalendarEvents(IEnumerable<BanquetEventDto>? sourceEvents = null)
    {
        var query = (sourceEvents ?? Events).Where(x => !IsCancelled(x.Status));

        if (!BanquetShowCompletedEvents)
        {
            query = query.Where(x => NormalizeEventStatus(x.Status) != "Completed");
        }

        if (SelectedRoomCalendarRoomId.HasValue)
        {
            query = query.Where(x => x.BanquetRoomId == SelectedRoomCalendarRoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(SelectedRoomCalendarStatus))
        {
            query = query.Where(x => string.Equals(
                NormalizeEventStatus(x.Status),
                SelectedRoomCalendarStatus,
                StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedRoomCalendarEventType))
        {
            query = query.Where(x => string.Equals(
                x.EventType,
                SelectedRoomCalendarEventType,
                StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedRoomCalendarShift))
        {
            query = query.Where(x => SelectedRoomCalendarShift == "Lunch"
                ? x.StartTime < TimeSpan.FromHours(16)
                : x.StartTime >= TimeSpan.FromHours(16));
        }

        return query;
    }

    private void UpdateChecklistGroups()
    {
        var groupNames = new[] { "Kinh doanh", "Bếp", "Kỹ thuật", "Trang trí", "Phục vụ", "Thu ngân" };
        var groups = groupNames.ToDictionary(name => name, name => new BanquetChecklistGroupVm(name));

        foreach (var item in ChecklistItems.OrderBy(x => x.SortOrder))
        {
            var groupName = ResolveChecklistGroup(item);
            var deadline = SelectedEvent?.EventDate.Date.AddDays(groupName switch
            {
                "Kinh doanh" => -30,
                "Bếp" => -7,
                "Kỹ thuật" => -5,
                "Trang trí" => -3,
                "Phục vụ" => -1,
                "Thu ngân" => 0,
                _ => -7
            });

            groups[groupName].Items.Add(new BanquetChecklistDisplayVm
            {
                Source = item,
                Title = item.Title,
                Responsible = string.IsNullOrWhiteSpace(item.DoneBy) ? groupName : item.DoneBy!,
                DueDate = deadline,
                Status = item.IsDone
                    ? "Hoàn thành"
                    : deadline.HasValue && deadline.Value.Date < DateTime.Today ? "Quá hạn" : "Chờ xử lý",
                Note = StripChecklistDepartmentMarker(item.Note)
            });
        }

        Reset(ChecklistGroups, groups.Values.Where(x => x.Items.Count > 0).ToArray());
        UpdateOverviewChecklistPreview();
        NotifyChecklistTimelineSummary();
    }

    private void UpdateOverviewChecklistPreview()
    {
        Reset(OverviewChecklistPreviewItems, ChecklistGroups
            .SelectMany(x => x.Items)
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .Take(8)
            .ToArray());
        OnPropertyChanged(nameof(HasOverviewChecklistPreviewItems));
        OnPropertyChanged(nameof(IsOverviewChecklistPreviewEmpty));
    }

    private void BuildOverviewCalendar()
    {
        var roomColumns = Rooms
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Take(3)
            .Select(x => new BanquetScheduleColumnVm
            {
                RoomId = x.Id,
                RoomName = x.Name
            })
            .ToArray();

        Reset(OverviewCalendarColumns, roomColumns);

        var today = DateTime.Today;
        var activeEvents = Events
            .Where(x => !IsCancelled(x.Status))
            .ToArray();

        var rows = Enumerable.Range(0, 5)
            .Select(offset =>
            {
                var day = today.AddDays(offset);
                var row = new BanquetScheduleRowVm
                {
                    Date = day,
                    DayLabel = day.ToString("ddd", new CultureInfo("vi-VN")).ToUpperInvariant(),
                    DateLabel = day.ToString("dd/MM", CultureInfo.InvariantCulture)
                };

                foreach (var room in roomColumns)
                {
                    var evt = activeEvents
                        .Where(x => x.BanquetRoomId == room.RoomId && x.EventDate.Date == day.Date)
                        .OrderBy(x => x.StartTime)
                        .FirstOrDefault();

                    row.Cells.Add(CreateOverviewScheduleCell(room, evt));
                }

                return row;
            })
            .ToArray();

        Reset(OverviewCalendarRows, rows);
    }

    private void BuildRoomCalendar(IEnumerable<BanquetEventDto>? sourceEvents = null)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => RoomCalendarWeekStart.AddDays(offset))
            .Select(date => new BanquetRoomCalendarDayVm
            {
                Date = date,
                DayLabel = date.ToString("ddd", new CultureInfo("vi-VN")).ToUpperInvariant(),
                DateLabel = date.ToString("dd/MM", CultureInfo.InvariantCulture),
                IsToday = date.Date == DateTime.Today
            })
            .ToArray();
        Reset(RoomCalendarDays, days);

        var activeEvents = GetFilteredRoomCalendarEvents(sourceEvents)
            .Where(x => x.EventDate.Date >= RoomCalendarWeekStart.Date
                        && x.EventDate.Date <= RoomCalendarWeekStart.AddDays(6).Date)
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .ToArray();

        var rows = Rooms
            .Where(x => x.IsActive
                        && (!SelectedRoomCalendarRoomId.HasValue || x.Id == SelectedRoomCalendarRoomId.Value))
            .OrderBy(x => x.Name)
            .Select(room =>
            {
                var row = new BanquetRoomCalendarRowVm
                {
                    RoomId = room.Id,
                    RoomCode = room.Code,
                    RoomName = room.Name,
                    Capacity = room.Capacity,
                    MaxTables = room.MaxTables
                };

                foreach (var day in days)
                {
                    var roomEvents = activeEvents
                        .Where(x => x.BanquetRoomId == room.Id && x.EventDate.Date == day.Date.Date)
                        .ToArray();
                    var lunch = roomEvents.FirstOrDefault(x => x.StartTime < TimeSpan.FromHours(16));
                    var dinner = roomEvents.FirstOrDefault(x => x.StartTime >= TimeSpan.FromHours(16));
                    row.Days.Add(new BanquetRoomCalendarCellVm
                    {
                        Date = day.Date,
                        Lunch = CreateRoomCalendarSlot(room, day.Date, "Lunch", "Trưa", lunch),
                        Dinner = CreateRoomCalendarSlot(room, day.Date, "Dinner", "Tối", dinner)
                    });
                }

                return row;
            })
            .ToArray();

        Reset(RoomCalendarRows, rows);
        OnPropertyChanged(nameof(RoomCalendarTotalSlotCount));
        OnPropertyChanged(nameof(RoomCalendarBookedSlotCount));
        OnPropertyChanged(nameof(RoomCalendarAvailableSlotCount));
        OnPropertyChanged(nameof(RoomCalendarOccupancyText));
        OnPropertyChanged(nameof(HasRoomCalendarRows));
        OnPropertyChanged(nameof(IsRoomCalendarEmpty));
    }

    private void BuildRoomCalendarYear()
    {
        var yearEvents = GetFilteredRoomCalendarEvents()
            .Where(x => x.EventDate.Year == RoomCalendarYear)
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .ToArray();
        var eventsByDate = yearEvents
            .GroupBy(x => x.EventDate.Date)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var culture = new CultureInfo("vi-VN");
        var months = new List<BanquetYearMonthVm>(12);

        for (var monthNumber = 1; monthNumber <= 12; monthNumber++)
        {
            var firstDay = new DateTime(RoomCalendarYear, monthNumber, 1);
            var daysSinceMonday = ((int)firstDay.DayOfWeek + 6) % 7;
            var gridStart = firstDay.AddDays(-daysSinceMonday);
            var monthEvents = yearEvents.Where(x => x.EventDate.Month == monthNumber).ToArray();
            var month = new BanquetYearMonthVm
            {
                Month = monthNumber,
                MonthLabel = culture.DateTimeFormat.GetMonthName(monthNumber).ToUpper(culture),
                EventCount = monthEvents.Length,
                BookedDayCount = monthEvents.Select(x => x.EventDate.Date).Distinct().Count()
            };

            for (var offset = 0; offset < 42; offset++)
            {
                var date = gridStart.AddDays(offset);
                var isCurrentMonth = date.Month == monthNumber && date.Year == RoomCalendarYear;
                var dayEvents = isCurrentMonth && eventsByDate.TryGetValue(date.Date, out var matches)
                    ? matches
                    : Array.Empty<BanquetEventDto>();
                var primaryStatus = dayEvents
                    .Select(x => NormalizeEventStatus(x.Status))
                    .FirstOrDefault();
                var background = primaryStatus switch
                {
                    "Confirmed" => "#DCFCE7",
                    "Hold" => "#FEF3C7",
                    "Preparing" => "#DBEAFE",
                    "Completed" => "#EDE9FE",
                    _ when dayEvents.Length > 0 => "#E2E8F0",
                    _ => "Transparent"
                };
                var eventSummary = dayEvents.Length == 0
                    ? "Chưa có tiệc"
                    : string.Join("\n", dayEvents.Take(4).Select(x => $"{x.StartTime:hh\\:mm} · {x.EventTypeDisplay} · {x.RoomName ?? "Chưa xếp phòng"}"));
                if (dayEvents.Length > 4)
                {
                    eventSummary += $"\n+ {dayEvents.Length - 4} sự kiện khác";
                }

                month.Days.Add(new BanquetYearDayVm
                {
                    Date = date,
                    DayNumber = date.Day.ToString(CultureInfo.InvariantCulture),
                    IsCurrentMonth = isCurrentMonth,
                    IsToday = date.Date == DateTime.Today,
                    HasEvents = dayEvents.Length > 0,
                    EventCount = dayEvents.Length,
                    BackgroundBrush = background,
                    ForegroundBrush = isCurrentMonth ? "#172B4D" : "#CBD5E1",
                    ToolTipText = $"{date:dd/MM/yyyy}\n{eventSummary}"
                });
            }

            months.Add(month);
        }

        Reset(RoomCalendarYearMonths, months);
    }

    private static BanquetRoomCalendarSlotVm CreateRoomCalendarSlot(
        BanquetRoomDto room,
        DateTime date,
        string shift,
        string shiftLabel,
        BanquetEventDto? evt)
    {
        if (evt is null)
        {
            return new BanquetRoomCalendarSlotVm
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Date = date,
                Shift = shift,
                ShiftLabel = shiftLabel,
                Title = "Còn trống",
                TimeText = shift == "Lunch" ? "11:00 - 14:00" : "18:00 - 21:00",
                DetailText = shift == "Lunch" ? "11:00 - 14:00" : "18:00 - 21:00",
                StatusText = "Có thể đặt",
                BackgroundBrush = "#FFFFFF",
                BorderBrush = "#DCE5F0",
                AccentBrush = "#94A3B8",
                IsEmpty = true
            };
        }

        var (background, border, accent) = NormalizeEventStatus(evt.Status) switch
        {
            "Confirmed" => ("#ECFDF5", "#86EFAC", "#059669"),
            "Hold" => ("#FFFBEB", "#FCD34D", "#D97706"),
            "Preparing" => ("#EFF6FF", "#93C5FD", "#2563EB"),
            "Completed" => ("#F5F3FF", "#C4B5FD", "#7C3AED"),
            _ => ("#F8FAFC", "#CBD5E1", "#64748B")
        };

        return new BanquetRoomCalendarSlotVm
        {
            RoomId = room.Id,
            EventId = evt.Id,
            SourceEvent = evt,
            RoomName = room.Name,
            Date = date,
            Shift = shift,
            ShiftLabel = shiftLabel,
            Title = evt.EventTypeDisplay,
            TimeText = $"{evt.StartTime:hh\\:mm} - {evt.EndTime:hh\\:mm}",
            Subtitle = $"{evt.CustomerName ?? "Chưa có tên khách"} · {evt.GuestCount:N0} khách",
            DetailText = $"{evt.CustomerName ?? "Chưa có tên khách"} · {evt.GuestCount:N0} khách",
            StatusText = evt.StatusDisplay,
            BackgroundBrush = background,
            BorderBrush = border,
            AccentBrush = accent,
            IsEmpty = false
        };
    }

    private static DateTime GetRoomCalendarWeekStart(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    private static BanquetScheduleCellVm CreateOverviewScheduleCell(BanquetScheduleColumnVm room, BanquetEventDto? evt)
    {
        if (evt is null)
        {
            return new BanquetScheduleCellVm
            {
                RoomName = room.RoomName,
                AccentBrush = "#E2E8F0",
                BackgroundBrush = "#FFFFFF",
                BorderBrush = "#E2E8F0",
                Title = string.Empty,
                TimeText = string.Empty,
                Subtitle = string.Empty,
                IsEmpty = true
            };
        }

        var (background, border) = NormalizeEventStatus(evt.Status) switch
        {
            "Confirmed" => ("#ECFDF5", "#86EFAC"),
            "Hold" => ("#FFFBEB", "#FCD34D"),
            "Preparing" => ("#EFF6FF", "#93C5FD"),
            "Completed" => ("#F5F3FF", "#C4B5FD"),
            "Cancelled" => ("#FEF2F2", "#FCA5A5"),
            _ => ("#F8FAFC", "#CBD5E1")
        };

        return new BanquetScheduleCellVm
        {
            EventId = evt.Id,
            RoomName = room.RoomName,
            AccentBrush = border,
            BackgroundBrush = background,
            BorderBrush = border,
            Title = string.IsNullOrWhiteSpace(evt.EventType) ? "Sự kiện" : evt.EventType,
            TimeText = $"{evt.StartTime:hh\\:mm} - {evt.EndTime:hh\\:mm}",
            Subtitle = $"{evt.CustomerName} · {evt.GuestCount} khách",
            IsEmpty = false
        };
    }

    private string ResolveChecklistGroup(BanquetChecklistDto item)
    {
        var markedDepartment = ReadChecklistDepartmentMarker(item.Note);
        if (!string.IsNullOrWhiteSpace(markedDepartment) && ChecklistDepartmentOptions.Contains(markedDepartment))
        {
            return markedDepartment;
        }

        var text = $"{item.Title} {item.Note}".ToLowerInvariant();
        if (text.Contains("bếp") || text.Contains("món") || text.Contains("menu"))
        {
            return "Bếp";
        }

        if (text.Contains("âm thanh") || text.Contains("ánh sáng") || text.Contains("led") || text.Contains("kỹ thuật"))
        {
            return "Kỹ thuật";
        }

        if (text.Contains("trang trí") || text.Contains("hoa") || text.Contains("sân khấu"))
        {
            return "Trang trí";
        }

        if (text.Contains("phục vụ") || text.Contains("bàn") || text.Contains("set up"))
        {
            return "Phục vụ";
        }

        if (text.Contains("thu") || text.Contains("cọc") || text.Contains("công nợ"))
        {
            return "Thu ngân";
        }

        return "Kinh doanh";
    }

    private static string? ReadChecklistDepartmentMarker(string? note)
    {
        const string prefix = "[Bộ phận:";
        if (string.IsNullOrWhiteSpace(note) || !note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var markerEnd = note.IndexOf(']');
        return markerEnd <= prefix.Length ? null : note[prefix.Length..markerEnd].Trim();
    }

    private static string StripChecklistDepartmentMarker(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return string.Empty;
        const string prefix = "[Bộ phận:";
        if (!note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return note.Trim();
        var markerEnd = note.IndexOf(']');
        return markerEnd < 0 ? note.Trim() : note[(markerEnd + 1)..].TrimStart('\r', '\n', ' ');
    }

    private static string BuildChecklistNote(string department, string? note)
    {
        var marker = $"[Bộ phận:{department}]";
        return string.IsNullOrWhiteSpace(note) ? marker : $"{marker}{Environment.NewLine}{note.Trim()}";
    }

    private void NotifyChecklistTimelineSummary()
    {
        OnPropertyChanged(nameof(ChecklistItemCount));
        OnPropertyChanged(nameof(ChecklistCompletedCount));
        OnPropertyChanged(nameof(ChecklistPendingCount));
        OnPropertyChanged(nameof(ChecklistOverdueCount));
        OnPropertyChanged(nameof(ChecklistProgressPercent));
        OnPropertyChanged(nameof(ChecklistProgressText));
        OnPropertyChanged(nameof(HasChecklistItems));
        OnPropertyChanged(nameof(IsChecklistEmpty));
        OnPropertyChanged(nameof(TimelineItemCount));
        OnPropertyChanged(nameof(TimelineCompletedCount));
        OnPropertyChanged(nameof(TimelinePendingCount));
        OnPropertyChanged(nameof(TimelineProgressPercent));
        OnPropertyChanged(nameof(TimelineProgressText));
        OnPropertyChanged(nameof(HasTimelineItems));
        OnPropertyChanged(nameof(IsTimelineEmpty));
    }

    private void BuildTimelineMilestones()
    {
        TimelineMilestones.Clear();
        if (SelectedEvent is null)
        {
            return;
        }

        var milestones = new[]
        {
            (90, "90 ngày trước"),
            (60, "60 ngày trước"),
            (30, "30 ngày trước"),
            (15, "15 ngày trước"),
            (7, "7 ngày trước"),
            (3, "3 ngày trước"),
            (1, "1 ngày trước"),
            (0, "Ngày diễn ra")
        };

        foreach (var milestone in milestones)
        {
            var dueDate = SelectedEvent.EventDate.Date.AddDays(-milestone.Item1);
            var linkedTask = EventTimelineItems.FirstOrDefault(x =>
                string.Equals(x.Title, milestone.Item2, StringComparison.OrdinalIgnoreCase) ||
                (milestone.Item1 == 0 && x.StartTime == SelectedEvent.StartTime));
            var isDone = linkedTask is not null && string.Equals(linkedTask.Status, "Done", StringComparison.OrdinalIgnoreCase);

            TimelineMilestones.Add(new BanquetMilestoneVm
            {
                Title = milestone.Item2,
                DueDate = dueDate,
                Status = isDone ? "Done" : dueDate.Date < DateTime.Today ? "Overdue" : dueDate.Date == DateTime.Today ? "Today" : "Planned",
                Description = linkedTask?.Description ?? $"Mốc chuẩn bị cho {SelectedEvent.EventType} · {SelectedEvent.CustomerName}"
            });
        }

        NotifyChecklistTimelineSummary();
    }

    [RelayCommand]
    private void SelectBanquetMenuCategory(Guid? categoryId)
    {
        SelectedBanquetMenuCategoryId = categoryId;
    }

    private async Task LoadBanquetMenuItemsAsync()
    {
        try
        {
            var categories = await _menuCatalog.GetCategoriesAsync();
            Reset(BanquetMenuCategories, categories);

            var items = await _menuCatalog.GetMenuItemsAsync();
            BanquetFoodItems.Clear();
            foreach (var item in items.Where(x => x.IsActive && !x.IsHotelService))
                BanquetFoodItems.Add(new PosMenuItemCard(item));

            await LoadBanquetServicesWithRetryAsync();

            FilterBanquetFoodItems();
            FilterBanquetServiceItems();
            OnPropertyChanged(nameof(HasBanquetServices));
            StatusText = $"Đã tải {BanquetFoodItems.Count} món + {BanquetServiceItems.Count} dịch vụ.";
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi tải danh mục: {ex.GetType().Name} - {ex.Message}";
        }
    }

    private async Task LoadBanquetServicesWithRetryAsync()
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var services = await _alacarteService.GetExtraServicesAsync();

                BanquetServiceItems.Clear();
                BanquetServiceTypes.Clear();
                BanquetServiceTypes.Add("Tất cả");
                SelectedBanquetServiceType = "Tất cả";
                foreach (var service in services.Where(x => x.IsActive))
                {
                    BanquetServiceItems.Add(new BanquetServiceCard(service));
                    var displayName = GetServiceTypeDisplayName(service.ServiceType);
                    if (!string.IsNullOrWhiteSpace(displayName) && !BanquetServiceTypes.Contains(displayName))
                        BanquetServiceTypes.Add(displayName);
                }

                FilterBanquetServiceItems();
                OnPropertyChanged(nameof(HasBanquetServices));
                return;
            }
            catch (InvalidOperationException ex) when (attempt < maxAttempts && ex.Message.Contains("second operation", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(150 * attempt);
            }
        }
    }

    private void FilterBanquetServiceItems()
    {
        IEnumerable<BanquetServiceCard> query = BanquetServiceItems;

        if (!string.IsNullOrWhiteSpace(SelectedBanquetServiceType) && SelectedBanquetServiceType != "Tất cả")
            query = query.Where(x => x.ServiceTypeDisplay == SelectedBanquetServiceType);

        if (!string.IsNullOrWhiteSpace(BanquetServiceSearchText))
        {
            var kw = BanquetServiceSearchText.Trim();
            query = query.Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                  || x.Code.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }
        Reset(BanquetFilteredServiceItems, query.Take(50).ToArray());
        OnPropertyChanged(nameof(HasBanquetServices));
    }

    [RelayCommand]
    private void SelectBanquetServiceType(string? type)
    {
        SelectedBanquetServiceType = type == "Tất cả" ? null : type;
    }

    [RelayCommand]
    private async Task AddServiceToEventAsync(BanquetServiceCard? card)
    {
        if (card is null || SelectedEvent is null) return;

        await RunBusyAsync(async () =>
        {
            var existing = EventServices.FirstOrDefault(x =>
                x.ServiceType == card.ServiceType &&
                x.Description.Equals(card.Name, StringComparison.OrdinalIgnoreCase));
            await _service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
            {
                Id = existing?.Id,
                BanquetEventId = SelectedEvent.Id,
                ServiceType = card.ServiceType,
                Description = card.Name,
                UnitName = card.UnitName,
                Qty = (existing?.Qty ?? 0) + 1,
                UnitPrice = card.Price,
                CostPrice = existing?.CostPrice ?? 0m,
                VatRate = card.VatRate,
                Note = $"Dịch vụ · {card.Code}"
            });
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            StatusText = $"Đã thêm dịch vụ {card.Name}.";
        });
    }

    [RelayCommand]
    private async Task IncreaseEventServiceAsync(BanquetServiceLineDto? line)
    {
        if (line is null) return;
        await SaveEventServiceQuantityAsync(line, line.Qty + 1);
    }

    [RelayCommand]
    private async Task DecreaseEventServiceAsync(BanquetServiceLineDto? line)
    {
        if (line is null) return;
        if (line.Qty <= 1)
        {
            await DeleteServiceLineAsync(line);
            return;
        }
        await SaveEventServiceQuantityAsync(line, line.Qty - 1);
    }

    private async Task SaveEventServiceQuantityAsync(BanquetServiceLineDto line, decimal quantity)
    {
        await RunBusyAsync(async () =>
        {
            await _service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
            {
                Id = line.Id,
                BanquetEventId = line.BanquetEventId,
                ServiceType = line.ServiceType,
                Description = line.Description,
                UnitName = line.UnitName,
                Qty = quantity,
                UnitPrice = line.UnitPrice,
                CostPrice = line.CostPrice,
                VatRate = line.VatRate
            });
            await RefreshPreservingSelectionAsync(SelectedEvent?.Id, SelectedRoom?.Id);
        });
    }

    private void FilterBanquetFoodItems()
    {
        IEnumerable<PosMenuItemCard> query = BanquetFoodItems;

        if (SelectedBanquetMenuCategoryId.HasValue)
            query = query.Where(x => x.MenuCategoryId == SelectedBanquetMenuCategoryId.Value);

        if (!string.IsNullOrWhiteSpace(BanquetMenuSearchText))
        {
            var kw = BanquetMenuSearchText.Trim();
            query = query.Where(x => x.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                  || x.Code.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        Reset(BanquetFilteredFoodItems, query.Take(50).ToArray());
    }

    [RelayCommand]
    private async Task AddFoodItemToEventAsync(PosMenuItemCard? card)
    {
        if (card is null || SelectedEvent is null) return;

        await RunBusyAsync(async () =>
        {
            var customLines = EventMenus
                .Where(x => !x.BanquetMenuPackageId.HasValue && x.MenuItemId.HasValue)
                .Select(x => new SaveBanquetCustomMenuLineRequest
                {
                    MenuItemId = x.MenuItemId!.Value,
                    ItemName = x.ItemName ?? string.Empty,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    Note = x.Note
                })
                .ToList();
            var existing = customLines.FirstOrDefault(x => x.MenuItemId == card.Id);
            if (existing is null)
            {
                customLines.Add(new SaveBanquetCustomMenuLineRequest
                {
                    MenuItemId = card.Id,
                    ItemName = card.Name,
                    Qty = 1m,
                    UnitPrice = card.UnitPrice,
                    Note = $"Món từ danh mục · {card.Code}"
                });
            }
            else
            {
                existing.Qty += 1m;
            }
            await _service.SaveCustomMenuAsync(new SaveBanquetCustomMenuRequest
            {
                BanquetEventId = SelectedEvent.Id,
                Lines = customLines
            });
            await RefreshPreservingSelectionAsync(SelectedEvent.Id, SelectedRoom?.Id);
            StatusText = $"Đã thêm {card.Name}.";
        });
    }

    private static string NormalizeEventStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Draft";
        }

        if (string.Equals(status, "Deposited", StringComparison.OrdinalIgnoreCase))
        {
            return "Confirmed";
        }

        return status.Trim();
    }

private static bool IsCancelled(string? status)
    => string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);

}

public sealed class BanquetSelectedFoodItemVm
{
    public int LineNo { get; set; }
    public PosMenuItemCard FoodItem { get; set; } = null!;
    public int Quantity { get; set; }
    public string UnitName => string.IsNullOrWhiteSpace(FoodItem.UnitName) ? "phần" : FoodItem.UnitName;
    public decimal UnitPrice => FoodItem.UnitPrice;
    public decimal Amount => Quantity * UnitPrice;
}
