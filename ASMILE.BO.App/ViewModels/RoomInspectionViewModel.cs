using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class RoomInspectionDto
{
    public Guid TaskId { get; set; }
    public bool IsSelected { get; set; }
    public Guid? RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch { "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe", "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential", "FAM" => "Family", _ => string.IsNullOrWhiteSpace(RoomType) ? "Chưa xác định" : RoomType };
    public string Building { get; set; } = "";
    public string Floor { get; set; } = "";
    public string Area { get; set; } = "";

    // Inspection status
    public string InspectionStatus { get; set; } = "Pending"; // Pending, Passed, Failed, Overdue
    public string InspectionLabel => InspectionStatus switch
    {
        "Pending" => "Chờ kiểm tra", "Passed" => "Đạt", "Failed" => "Không đạt", "Overdue" => "Quá hạn",
        _ => InspectionStatus
    };
    public string InspectionColor => InspectionStatus switch
    {
        "Pending" => "#7C3AED", "Passed" => "#16A34A", "Failed" => "#DC2626", "Overdue" => "#EA580C",
        _ => "#64748B"
    };
    public string InspectionBg => InspectionStatus switch
    {
        "Pending" => "#F3E8FF", "Passed" => "#DCFCE7", "Failed" => "#FEE2E2", "Overdue" => "#FFEDD5",
        _ => "#F8FAFC"
    };

    public string FoStatus { get; set; } = "VD";
    public bool IsVip { get; set; }
    public bool IsRush { get; set; }
    public int Priority => (IsVip ? 100 : 0) + (IsRush ? 50 : 0) + (InspectionStatus == "Overdue" ? 80 : 0);

    // Guest
    public string? GuestName { get; set; }
    public DateTime? CheckoutTime { get; set; }
    public string? NextGuest { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public string? GuestVip { get; set; }
    public string? SpecialRequest { get; set; }
    public string? Remark { get; set; }

    // Housekeeper
    public string? AssignedTo { get; set; }
    public string? Supervisor { get; set; }
    public DateTime? CleaningFinishedAt { get; set; }
    public int CleaningDurationMinutes { get; set; }

    // Inspection
    public string? Inspector { get; set; }
    public DateTime? InspectionTime { get; set; }
    public int ChecklistScore { get; set; }
    public int AmenitiesScore { get; set; }
    public int RoomConditionScore { get; set; }
    public int MaxChecklistScore { get; set; } = 80;
    public int MaxAmenitiesScore { get; set; } = 10;
    public int MaxRoomConditionScore { get; set; } = 10;
    public int TotalScore => ChecklistScore + AmenitiesScore + RoomConditionScore;
    public int MaxTotalScore => MaxChecklistScore + MaxAmenitiesScore + MaxRoomConditionScore;
    public double ScorePercent => MaxTotalScore > 0 ? Math.Round((double)TotalScore / MaxTotalScore * 100, 1) : 0;
    public string ScoreGrade => ScorePercent >= 95 ? "A+" : ScorePercent >= 90 ? "A" : ScorePercent >= 80 ? "B" : ScorePercent >= 70 ? "C" : "D";
    public string ScoreColor => ScorePercent >= 90 ? "#16A34A" : ScorePercent >= 80 ? "#D97706" : ScorePercent >= 70 ? "#EA580C" : "#DC2626";
    public string? RejectReason { get; set; }
    public string? InspectionNote { get; set; }

    // Checklist items
    public int ChecklistDone { get; set; }
    public int ChecklistTotal { get; set; }
    public double ChecklistPercent => ChecklistTotal > 0 ? Math.Round((double)ChecklistDone / ChecklistTotal * 100, 1) : 0;
    public List<InspectionChecklistItem> PersistedChecklist { get; } = new();

    // Photos
    public List<InspectionPhotoDto> Photos { get; set; } = new();

    // Timeline
    public List<InspectionTimelineEntry> Timeline { get; set; } = new();

    // Maintenance
    public bool HasMaintenanceIssue { get; set; }
    public string? MaintenanceNote { get; set; }

    // Lost & Found
    public bool HasLostFound { get; set; }
    public string? LostFoundNote { get; set; }

    // Minibar
    public bool MinibarProcessed { get; set; }
    public string? MinibarNote { get; set; }
    public string MinibarStatus => MinibarProcessed ? "Đã xử lý" : "Chưa xử lý";
    public string MinibarStatusColor => MinibarProcessed ? "#16A34A" : "#DC2626";

    // Computed
    public bool CanPass => ChecklistDone >= ChecklistTotal && MinibarProcessed && !HasMaintenanceIssue && !HasLostFound && ScorePercent >= 80;
    public bool CanNotPass => !CanPass;
    public bool IsOverdue => InspectionStatus == "Overdue";
    public string BlockReason
    {
        get
        {
            if (ChecklistTotal == 0) return "Chưa có dữ liệu checklist thật";
            if (ChecklistDone < ChecklistTotal) return $"Checklist chưa đủ ({ChecklistDone}/{ChecklistTotal})";
            if (!MinibarProcessed) return "Minibar chưa xử lý";
            if (HasMaintenanceIssue) return "Maintenance đang mở";
            if (HasLostFound) return "Lost & Found chưa xác nhận";
            if (ScorePercent < 80) return $"Score dưới 80% ({ScorePercent}%)";
            return "";
        }
    }
}

public sealed class InspectionChecklistItem
{
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public bool IsDone { get; set; }
    public string Category { get; set; } = ""; // Bedding, Bathroom, Amenities, Equipment, General
}

public sealed class InspectionKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class InspectionTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class InspectionPhotoDto
{
    public string Url { get; set; } = "";
    public string Label { get; set; } = ""; // Before, After, Inspection
    public string Category { get; set; } = "";
}

public sealed class InspectionTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public ObservableCollection<InspectionTreeItem> Children { get; } = new();
}

public sealed class InspectionPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public InspectionPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

public sealed class InspectorStatDto
{
    public string Name { get; set; } = "";
    public int InspectedToday { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public double AvgScore { get; set; }
    public double PassRate { get; set; }
}

public sealed class FloorInspectionDto
{
    public string Floor { get; set; } = "";
    public List<RoomInspectionDto> Rooms { get; set; } = new();
}

// ═══ VIEW MODEL ═══

public sealed partial class RoomInspectionViewModel : ViewModelBase
{
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly IWorkOrderService? _workOrderService;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;
    private readonly ILostFoundQueryService? _lostFoundQueryService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;

    // Kept for the designer and legacy XAML preview. Runtime windows resolve the
    // service-backed constructor from the application container.
    public RoomInspectionViewModel()
    {
    }

    public RoomInspectionViewModel(
        IHousekeepingBoardService boardService,
        IFrontOfficeExecutionContextAccessor executionContext,
        IHousekeepingWorkflowService? workflowService = null,
        IWorkOrderService? workOrderService = null,
        IHotelRoomMasterDataRepository? roomMasterDataRepository = null,
        ILostFoundQueryService? lostFoundQueryService = null,
        IHotelStayRepository? stayRepository = null,
        IReservationRepository? reservationRepository = null)
    {
        _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _workflowService = workflowService;
        _workOrderService = workOrderService;
        _roomMasterDataRepository = roomMasterDataRepository;
        _lostFoundQueryService = lostFoundQueryService;
        _stayRepository = stayRepository;
        _reservationRepository = reservationRepository;
    }

    public ObservableCollection<RoomInspectionDto> Rooms { get; } = new();
    public ObservableCollection<RoomInspectionDto> FilteredRooms { get; } = new();
    public ObservableCollection<InspectionKpi> DashboardKpis { get; } = new();
    public ObservableCollection<InspectionChecklistItem> Checklist { get; } = new();
    public ObservableCollection<InspectionTimelineEntry> RoomTimeline { get; } = new();
    public ObservableCollection<InspectionTreeItem> TreeItems { get; } = new();
    public ObservableCollection<InspectionPaginationButton> PageButtons { get; } = new();
    public ObservableCollection<InspectorStatDto> InspectorStats { get; } = new();
    public ObservableCollection<FloorInspectionDto> FloorViews { get; } = new();

    [ObservableProperty] private RoomInspectionDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private InspectionTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // Filter
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterArea;
    [ObservableProperty] private string? _filterSupervisor;
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterPriority;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterRushOnly;

    // Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0
        ? "0–0 / 0 phòng"
        : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} phòng";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedRoom is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";
    public bool IsFloorView => ViewMode == "Floor";

    // Computed
    public int TotalRooms => Rooms.Count;
    public int PendingCount => Rooms.Count(r => r.InspectionStatus == "Pending");
    public int PassedCount => Rooms.Count(r => r.InspectionStatus == "Passed");
    public int FailedCount => Rooms.Count(r => r.InspectionStatus == "Failed");
    public int OverdueCount => Rooms.Count(r => r.InspectionStatus == "Overdue");
    public int VipCount => Rooms.Count(r => r.IsVip);
    public int RushCount => Rooms.Count(r => r.IsRush);
    public int InspectedToday => Rooms.Count(r => r.InspectionTime.HasValue && r.InspectionTime.Value.Date == DateTime.Today);
    public double AvgInspectionMinutes => Rooms.Where(r => r.InspectionTime.HasValue).DefaultIfEmpty().Average(r => r?.CleaningDurationMinutes ?? 0);
    public double AvgScore => Rooms.Where(r => r.InspectionStatus is "Passed" or "Failed").DefaultIfEmpty().Average(r => r?.ScorePercent ?? 0);

    public static string[] StatusFilters { get; } = ["Tất cả", "Pending", "Passed", "Failed", "Overdue"];
    public string[] FloorFilters { get; private set; } = [];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE", "FAM"];
    public static string[] AreaFilters { get; } = ["Tất cả", "Khu A", "Khu B", "Khu C"];
    public static string[] BuildingFilters { get; } = ["Tất cả", "A", "B", "C"];
    public static string[] PriorityFilters { get; } = ["Tất cả", "VIP", "Rush", "Overdue", "Normal"];
    public string[] SupervisorFilters { get; private set; } = [];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    private static readonly string[] RejectReasons = [
        "WC chưa sạch", "Thiếu amenities", "Ga giường chưa thẳng", "Bụi trên bàn",
        "Điều hòa không mát", "TV không hoạt động", "Mùi phòng khó chịu",
        "Ban công chưa lau", "Khăn tắm thiếu", "Minibar chưa bổ sung"
    ];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Rooms.Clear();

            // Production path: inspection rows come from the authoritative
            // HousekeepingTask/HotelRoom data source. Do not manufacture rows
            // when the service is registered but the property has no tasks.
            if (_boardService is not null)
            {
                var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
                if (propertyId is null)
                {
                    ErrorMessage = "Chưa có ngữ cảnh cơ sở để tải dữ liệu kiểm tra phòng.";
                    BuildInspectorStats();
                    BuildDashboard();
                    BuildTree();
                    ApplyFilters();
                    return;
                }

                var tasks = await _boardService.GetTaskBoardAsync(propertyId.Value, null, null, null, ct);
                foreach (var task in tasks)
                    Rooms.Add(MapTask(task));

                await EnrichRoomMasterDataAsync(propertyId.Value, Rooms, ct);
                await EnrichGuestDataAsync(propertyId.Value, Rooms, ct);

                if (_workOrderService is not null)
                {
                    var workOrders = await _workOrderService.GetActiveAsync(propertyId.Value, null, ct);
                    foreach (var room in Rooms)
                    {
                        var matches = workOrders.Where(x =>
                            (room.RoomId is Guid roomId && x.RoomId == roomId)
                            || string.Equals(x.RoomNumber, room.RoomNumber, StringComparison.OrdinalIgnoreCase)).ToList();
                        room.HasMaintenanceIssue = matches.Count > 0;
                        room.MaintenanceNote = matches.FirstOrDefault()?.Description;
                    }
                }

                BuildInspectorStats();
                FloorFilters = Rooms.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
                SupervisorFilters = Rooms.Select(x => x.Supervisor ?? "—")
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
                OnPropertyChanged(nameof(FloorFilters));
                OnPropertyChanged(nameof(SupervisorFilters));
                BuildDashboard(); BuildTree(); ApplyFilters();
                SelectTreeItem(TreeItems.FirstOrDefault());
                return;
            }

            ErrorMessage = "Chưa có Housekeeping query service; không hiển thị dữ liệu mẫu.";
            BuildInspectorStats();
            BuildDashboard();
            BuildTree();
            ApplyFilters();
            return;

            /* Legacy designer fixture retained only for historical reference.
#pragma warning disable CS0162
            await Task.Delay(120, ct);
            var rng = new Random(66);
            var roomTypes = new[] { "STD", "STD", "STD", "SUP", "SUP", "DLX", "DLX", "JNR", "STE", "FAM" };
            var statuses = new[] { "Pending", "Pending", "Pending", "Pending", "Pending", "Passed", "Passed", "Failed", "Overdue" };
            var areas = new[] { "Khu A", "Khu A", "Khu B", "Khu B", "Khu C" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", null, null };
            var nextGuests = new[] { "Lê Hoàng Cường", "Park Ji-Sung", "David Chen", "Maria Garcia", "Ahmed Al-Rashid", null, null, null };
            var specials = new[] { "Extra Bed", "Baby Cot", "Hoa tươi", "Trái cây", "Late Arrival", "Phòng không hút thuốc", null, null, null };

            for (int floor = 1; floor <= 10; floor++)
            {
                var roomsPerFloor = floor <= 5 ? 14 : floor <= 8 ? 10 : 8;
                for (int j = 1; j <= roomsPerFloor; j++)
                {
                    var roomNum = $"{floor}{j:D2}";
                    var status = statuses[rng.Next(statuses.Length)];
                    var isVip = rng.Next(7) == 1;
                    var isRush = rng.Next(5) == 1;
                    var rType = roomTypes[rng.Next(roomTypes.Length)];
                    var hk = Housekeepers[rng.Next(Housekeepers.Length)];
                    var sup = Supervisors[rng.Next(Supervisors.Length)];
                    var inspector = Supervisors[rng.Next(Supervisors.Length)];
                    var cleanFinished = DateTime.Now.AddMinutes(-rng.Next(5, 180));
                    var cleanDuration = rng.Next(25, 75);
                    var checklistDone = status switch { "Passed" => 24, "Failed" => rng.Next(18, 24), "Pending" => rng.Next(20, 24), "Overdue" => rng.Next(19, 24), _ => 24 };
                    var checklistScore = status switch { "Passed" => rng.Next(72, 81), "Failed" => rng.Next(40, 72), "Pending" => 0, "Overdue" => 0, _ => 0 };
                    var amenitiesScore = status switch { "Passed" => rng.Next(8, 11), "Failed" => rng.Next(3, 8), _ => 0 };
                    var roomConditionScore = status switch { "Passed" => rng.Next(8, 11), "Failed" => rng.Next(3, 8), _ => 0 };

                    var photos = new List<InspectionPhotoDto>();
                    if (status is "Passed" or "Failed")
                    {
                        photos.Add(new InspectionPhotoDto { Label = "Before", Category = "Trước dọn", Url = $"room_{roomNum}_before.jpg" });
                        photos.Add(new InspectionPhotoDto { Label = "After", Category = "Sau dọn", Url = $"room_{roomNum}_after.jpg" });
                        photos.Add(new InspectionPhotoDto { Label = "Inspection", Category = "Kiểm tra", Url = $"room_{roomNum}_inspection.jpg" });
                    }

                    var room = new RoomInspectionDto
                    {
                        RoomNumber = roomNum, RoomType = rType, Building = rng.Next(3) switch { 0 => "A", 1 => "B", _ => "C" },
                        Floor = floor.ToString(), Area = areas[rng.Next(areas.Length)],
                        InspectionStatus = status,
                        FoStatus = status is "Passed" ? "VC" : "VD",
                        IsVip = isVip, IsRush = isRush,
                        GuestName = guests[rng.Next(guests.Length)],
                        CheckoutTime = DateTime.Now.AddHours(-rng.Next(1, 48)),
                        NextGuest = status is "Passed" ? nextGuests[rng.Next(nextGuests.Length)] : null,
                        ExpectedArrival = status is "Passed" ? DateTime.Now.AddHours(rng.Next(2, 24)) : null,
                        GuestVip = status is "Passed" && rng.Next(5) == 1 ? "VIP 2" : null,
                        SpecialRequest = rng.Next(5) == 1 ? specials[rng.Next(specials.Length)] : null,
                        AssignedTo = hk, Supervisor = sup,
                        CleaningFinishedAt = cleanFinished,
                        CleaningDurationMinutes = cleanDuration,
                        Inspector = status is "Passed" or "Failed" ? inspector : null,
                        InspectionTime = status is "Passed" or "Failed" ? DateTime.Now.AddMinutes(-rng.Next(5, 60)) : null,
                        ChecklistScore = checklistScore, AmenitiesScore = amenitiesScore, RoomConditionScore = roomConditionScore,
                        ChecklistDone = checklistDone, ChecklistTotal = 24,
                        RejectReason = status == "Failed" ? RejectReasons[rng.Next(RejectReasons.Length)] : null,
                        InspectionNote = status == "Failed" ? "Cần làm lại các hạng mục chưa đạt" : null,
                        Photos = photos,
                        Remark = isVip ? "Phòng VIP — yêu cầu kiểm tra kỹ lưỡng" : isRush ? "Phòng Rush — ưu tiên kiểm tra trước" : null,
                        HasMaintenanceIssue = rng.Next(10) == 1,
                        MaintenanceNote = rng.Next(10) == 1 ? "Điều hòa kêu to" : null,
                        HasLostFound = rng.Next(12) == 1,
                        LostFoundNote = rng.Next(12) == 1 ? "Ví tiền trong safe" : null,
                        MinibarProcessed = status is "Passed" or "Pending" || rng.Next(4) > 0,
                        MinibarNote = status is "Failed" && rng.Next(3) == 0 ? "Chưa bổ sung" : null,
                    };

                    // Build timeline
                    var t0 = room.CheckoutTime ?? DateTime.Now.AddHours(-3);
                    room.Timeline.Add(new InspectionTimelineEntry { Timestamp = t0, Event = "Check-out", Detail = room.GuestName ?? "Khách", Color = "#DC2626" });
                    room.Timeline.Add(new InspectionTimelineEntry { Timestamp = t0.AddMinutes(10), Event = "Phân công dọn", Detail = hk, Color = "#D97706" });
                    room.Timeline.Add(new InspectionTimelineEntry { Timestamp = t0.AddMinutes(20), Event = "Bắt đầu dọn", Detail = hk, Color = "#2563EB" });
                    room.Timeline.Add(new InspectionTimelineEntry { Timestamp = t0.AddMinutes(20 + cleanDuration), Event = "Hoàn thành dọn", Detail = $"{cleanDuration} phút", Color = "#0891B2" });
                    if (status is "Passed" or "Failed" && room.InspectionTime.HasValue)
                    {
                        room.Timeline.Add(new InspectionTimelineEntry { Timestamp = room.InspectionTime.Value, Event = status == "Passed" ? "Kiểm tra ĐẠT" : "Kiểm tra KHÔNG ĐẠT", Detail = $"{room.ScorePercent}% — {inspector}", Color = status == "Passed" ? "#16A34A" : "#DC2626" });
                    }
                    if (status == "Failed")
                        room.Timeline.Add(new InspectionTimelineEntry { Timestamp = DateTime.Now, Event = "Yêu cầu làm lại", Detail = room.RejectReason ?? "Cần dọn lại", Color = "#EA580C" });

                    Rooms.Add(room);
                }
            }

            // Inspector stats
            BuildInspectorStats();

            FloorFilters = Enumerable.Range(1, 10).Select(f => f.ToString()).Prepend("Tất cả").ToArray();
            SupervisorFilters = Supervisors.Prepend("Tất cả").ToArray();
            OnPropertyChanged(nameof(FloorFilters));
            OnPropertyChanged(nameof(SupervisorFilters));

            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
        }
            */
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static RoomInspectionDto MapTask(HkTaskBoardDto task)
    {
        var statusName = task.TaskStatus.ToString();
        var inspectionStatus = task.InspectionResult == HousekeepingInspectionResult.Rejected ? "Failed"
            : task.InspectionResult == HousekeepingInspectionResult.Approved || string.Equals(statusName, "Completed", StringComparison.OrdinalIgnoreCase) ? "Passed"
            : task.HasInspectionPending ? "Pending"
            : string.Equals(statusName, "Rejected", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(statusName, "Cancelled", StringComparison.OrdinalIgnoreCase) ? "Failed"
            : task.IsOverdue ? "Overdue" : "Pending";
        var roomNumber = task.RoomNumber ?? string.Empty;
        var digits = new string(roomNumber.Where(char.IsDigit).ToArray());
        var floor = digits.Length > 0 ? digits[..1] : "—";
        var assigned = task.AssignedName ?? task.AssignedTo;
        var room = new RoomInspectionDto
        {
            TaskId = task.TaskId,
            RoomId = task.RoomId,
            RoomNumber = roomNumber,
            RoomType = string.Empty,
            Building = string.Empty,
            Floor = floor == "—" ? string.Empty : floor,
            Area = string.Empty,
            InspectionStatus = inspectionStatus,
            FoStatus = task.RoomStatus.ToString(),
            IsVip = task.IsVip,
            IsRush = task.Priority.ToString().Contains("Rush", StringComparison.OrdinalIgnoreCase),
            AssignedTo = assigned,
            Supervisor = null,
            InspectionTime = task.InspectedAtUtc,
            Inspector = task.InspectorName,
            InspectionNote = task.InspectionNotes,
            ChecklistDone = 0,
            ChecklistTotal = 0,
            ChecklistScore = 0,
            AmenitiesScore = 0,
            RoomConditionScore = 0,
            MinibarProcessed = false
        };
        ApplyPersistedInspectionData(room, task.InspectionChecklistData);
        if (task.StartedAt.HasValue)
            room.CleaningFinishedAt = task.StartedAt;
        return room;
    }

    private async Task EnrichRoomMasterDataAsync(Guid propertyId, IReadOnlyList<RoomInspectionDto> rooms, CancellationToken ct)
    {
        if (_roomMasterDataRepository is null || rooms.Count == 0)
            return;

        foreach (var room in rooms)
        {
            HotelRoomDto? master = null;
            if (room.RoomId is Guid roomId)
                master = await _roomMasterDataRepository.GetRoomDtoAsync(propertyId, roomId, ct);
            if (master is null && !string.IsNullOrWhiteSpace(room.RoomNumber))
                master = await _roomMasterDataRepository.GetRoomByNumberAsync(propertyId, room.RoomNumber, ct);

            if (master is null)
                continue;

            room.RoomId = master.HotelRoomId;
            room.RoomNumber = master.RoomNumber;
            room.RoomType = master.HotelRoomTypeCode;
            room.Building = master.Building ?? string.Empty;
            room.Floor = master.Floor ?? string.Empty;
            room.Area = master.Zone ?? string.Empty;
        }
    }

    private async Task EnrichGuestDataAsync(Guid propertyId, IReadOnlyList<RoomInspectionDto> rooms, CancellationToken ct)
    {
        if (_stayRepository is null && _reservationRepository is null)
            return;

        var stays = _stayRepository is null
            ? Array.Empty<HotelStayDto>()
            : await _stayRepository.GetCurrentInHouseAsync(propertyId, ct);
        var departures = _stayRepository is null || _executionContext?.Current is not { } context
            ? Array.Empty<HotelStayDto>()
            : await _stayRepository.GetExpectedDeparturesAsync(propertyId, context.BusinessDate.BusinessDate, ct);
        var reservations = _reservationRepository is null
            ? Array.Empty<ReservationDto>()
            : (await _reservationRepository.SearchAsync(
                propertyId,
                new ReservationSearchFilter(null, null, null, null, null, null, null, null, null, null, null, null),
                new PageRequest(1, 500),
                "ArrivalDate",
                ct)).Items;
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var stay = stays.Concat(departures).FirstOrDefault(item =>
                (room.RoomId is Guid roomId && item.RoomId == roomId)
                || string.Equals(item.RoomNumber, room.RoomNumber, StringComparison.OrdinalIgnoreCase));
            var reservation = reservations
                .Where(item => item.Status is not (ReservationStatus.Cancelled or ReservationStatus.NoShow)
                    && item.AssignedRoomId == room.RoomId
                    && item.DepartureDate >= today)
                .OrderBy(item => item.ArrivalDate)
                .FirstOrDefault();

            room.GuestName = stay?.GuestName ?? reservation?.GuestName;
            room.CheckoutTime = stay?.ActualCheckOutAtUtc?.ToLocalTime();
            room.NextGuest = reservation is not null && reservation.ArrivalDate > today ? reservation.GuestName : null;
            room.ExpectedArrival = reservation is not null && reservation.ArrivalDate > today
                ? reservation.ArrivalDate.ToDateTime(TimeOnly.MinValue)
                : null;
            room.GuestVip = reservation?.IsVip == true ? "VIP" : null;
            room.SpecialRequest = reservation?.SpecialRequests;
        }
    }

    private static void ApplyPersistedInspectionData(RoomInspectionDto room, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            room.ChecklistDone = ReadInt(root, "checklistDone", "done") ?? room.ChecklistDone;
            room.ChecklistTotal = ReadInt(root, "checklistTotal", "total") ?? room.ChecklistTotal;
            room.ChecklistScore = ReadInt(root, "checklistScore", "score") ?? room.ChecklistScore;
            room.AmenitiesScore = ReadInt(root, "amenitiesScore") ?? room.AmenitiesScore;
            room.RoomConditionScore = ReadInt(root, "roomConditionScore") ?? room.RoomConditionScore;
            if (root.TryGetProperty("minibarProcessed", out var minibar) && minibar.ValueKind is JsonValueKind.True or JsonValueKind.False)
                room.MinibarProcessed = minibar.GetBoolean();

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var label = ReadString(item, "label", "title", "description", "code");
                    if (string.IsNullOrWhiteSpace(label))
                        continue;

                    room.PersistedChecklist.Add(new InspectionChecklistItem
                    {
                        Label = label,
                        IconGlyph = ReadString(item, "icon", "iconGlyph") ?? string.Empty,
                        Category = ReadString(item, "category", "group") ?? string.Empty,
                        IsDone = ReadBool(item, "isDone", "done", "completed") ?? false
                    });
                }

                if (room.PersistedChecklist.Count > 0)
                {
                    room.ChecklistTotal = room.PersistedChecklist.Count;
                    room.ChecklistDone = room.PersistedChecklist.Count(x => x.IsDone);
                }
            }

            if (root.TryGetProperty("photos", out var photos) && photos.ValueKind == JsonValueKind.Array)
            {
                foreach (var photo in photos.EnumerateArray())
                {
                    var url = ReadString(photo, "url", "path");
                    if (string.IsNullOrWhiteSpace(url))
                        continue;

                    room.Photos.Add(new InspectionPhotoDto
                    {
                        Url = url,
                        Label = ReadString(photo, "label") ?? "Inspection",
                        Category = ReadString(photo, "category") ?? "Kiểm tra"
                    });
                }
            }
        }
        catch (JsonException)
        {
            // Legacy inspection data is optional; malformed data must not be treated as valid.
        }
    }

    private static int? ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number))
                return number;
        }

        return null;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static bool? ReadBool(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
        }

        return null;
    }

    private void BuildInspectorStats()
    {
        InspectorStats.Clear();
        var names = Rooms.Select(r => r.Inspector ?? r.Supervisor ?? r.AssignedTo ?? "—")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        foreach (var name in names)
        {
            var rooms = Rooms.Where(r => r.Inspector == name).ToList();
            var passed = rooms.Count(r => r.InspectionStatus == "Passed");
            var failed = rooms.Count(r => r.InspectionStatus == "Failed");
            var today = rooms.Count(r => r.InspectionTime.HasValue && r.InspectionTime.Value.Date == DateTime.Today);
            var avg = rooms.Where(r => r.InspectionStatus is "Passed" or "Failed").DefaultIfEmpty().Average(r => r?.ScorePercent ?? 0);
            var total = passed + failed;
            InspectorStats.Add(new InspectorStatDto
            {
                Name = name, InspectedToday = today, Passed = passed, Failed = failed,
                AvgScore = Math.Round(avg, 1),
                PassRate = total > 0 ? Math.Round((double)passed / total * 100, 1) : 0
            });
        }

        // Floor views
        FloorViews.Clear();
        for (int f = 1; f <= 10; f++)
        {
            var floorStr = f.ToString();
            FloorViews.Add(new FloorInspectionDto { Floor = floorStr, Rooms = Rooms.Where(r => r.Floor == floorStr).ToList() });
        }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new InspectionKpi { Label = "Chờ kiểm tra", Value = PendingCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", FilterKey = "Status", FilterValue = "Pending" });
        DashboardKpis.Add(new InspectionKpi { Label = "Đạt", Value = PassedCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Status", FilterValue = "Passed" });
        DashboardKpis.Add(new InspectionKpi { Label = "Không đạt", Value = FailedCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Failed" });
        DashboardKpis.Add(new InspectionKpi { Label = "VIP", Value = VipCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new InspectionKpi { Label = "Rush", Value = RushCount.ToString(), Color = "#EA580C", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new InspectionKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEE2E2", FilterKey = "Status", FilterValue = "Overdue" });
        DashboardKpis.Add(new InspectionKpi { Label = "Đã KT HN", Value = InspectedToday.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new InspectionKpi { Label = "TB Score", Value = $"{AvgScore:F1}%", Color = "#4F46E5", BgColor = "#EEF2FF" });
        DashboardKpis.Add(new InspectionKpi { Label = "Pass Rate", Value = (PendingCount + PassedCount + FailedCount) > 0 ? $"{Math.Round((double)PassedCount / (PassedCount + FailedCount) * 100, 1):F1}%" : "—", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new InspectionKpi { Label = "TB thời gian", Value = $"{AvgInspectionMinutes:F0}ph", Color = "#D97706", BgColor = "#FFF7ED" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new InspectionTreeItem { Label = "Tất cả", Count = TotalRooms, IsSelected = true });
        TreeItems.Add(new InspectionTreeItem { Label = "Chờ kiểm tra", Count = PendingCount, FilterKey = "Status", FilterValue = "Pending" });
        TreeItems.Add(new InspectionTreeItem { Label = "VIP", Count = VipCount, FilterKey = "Vip", FilterValue = "true" });
        TreeItems.Add(new InspectionTreeItem { Label = "Rush", Count = RushCount, FilterKey = "Rush", FilterValue = "true" });
        TreeItems.Add(new InspectionTreeItem { Label = "Không đạt", Count = FailedCount, FilterKey = "Status", FilterValue = "Failed" });
        TreeItems.Add(new InspectionTreeItem { Label = "Đã hoàn thành", Count = PassedCount, FilterKey = "Status", FilterValue = "Passed" });
        TreeItems.Add(new InspectionTreeItem { Label = "Quá hạn", Count = OverdueCount, FilterKey = "Status", FilterValue = "Overdue" });
    }

    private void BuildChecklist(RoomInspectionDto room)
    {
        Checklist.Clear();
        foreach (var item in room.PersistedChecklist)
        {
            Checklist.Add(new InspectionChecklistItem
            {
                Label = item.Label,
                IconGlyph = item.IconGlyph,
                IsDone = item.IsDone,
                Category = item.Category
            });
        }
    }

    private void BuildTimeline(RoomInspectionDto room)
    {
        RoomTimeline.Clear();
        foreach (var entry in room.Timeline.OrderBy(e => e.Timestamp))
            RoomTimeline.Add(entry);
    }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Rooms.AsEnumerable();
        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.RoomNumber.ToLower().Contains(search) || (r.Inspector?.ToLower().Contains(search) ?? false) || (r.GuestName?.ToLower().Contains(search) ?? false) || (r.AssignedTo?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.InspectionStatus == FilterStatus);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterArea) && FilterArea != "Tất cả") q = q.Where(r => r.Area == FilterArea);
        if (!string.IsNullOrEmpty(FilterSupervisor) && FilterSupervisor != "Tất cả") q = q.Where(r => r.Supervisor == FilterSupervisor);
        if (!string.IsNullOrEmpty(FilterBuilding) && FilterBuilding != "Tất cả") q = q.Where(r => r.Building == FilterBuilding);
        if (!string.IsNullOrEmpty(FilterPriority) && FilterPriority != "Tất cả")
            q = FilterPriority switch { "VIP" => q.Where(r => r.IsVip), "Rush" => q.Where(r => r.IsRush), "Overdue" => q.Where(r => r.InspectionStatus == "Overdue"), "Normal" => q.Where(r => !r.IsVip && !r.IsRush && r.InspectionStatus != "Overdue"), _ => q };
        if (FilterVipOnly) q = q.Where(r => r.IsVip);
        if (FilterRushOnly) q = q.Where(r => r.IsRush);

        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Status" => q.Where(r => r.InspectionStatus == v),
                "Vip" => q.Where(r => r.IsVip),
                "Rush" => q.Where(r => r.IsRush),
                _ => q
            };
        }

        var list = q.OrderByDescending(r => r.Priority).ThenBy(r => int.Parse(r.RoomNumber)).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredRooms.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredRooms.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterStatus = null; FilterFloor = null; FilterRoomType = null;
        FilterArea = null; FilterSupervisor = null; FilterBuilding = null; FilterPriority = null;
        FilterVipOnly = false; FilterRushOnly = false; IsFilterExpanded = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void SelectTreeItem(InspectionTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }

    private static void DeselectTree(InspectionTreeItem item) { item.IsSelected = false; foreach (var c in item.Children) DeselectTree(c); }

    [RelayCommand] private void ApplyKpiFilter(InspectionKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Status") FilterStatus = kpi.FilterValue;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterAreaChanged(string? value) => ApplyFilters();
    partial void OnFilterSupervisorChanged(string? value) => ApplyFilters();
    partial void OnFilterBuildingChanged(string? value) => ApplyFilters();
    partial void OnFilterPriorityChanged(string? value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterRushOnlyChanged(bool value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); OnPropertyChanged(nameof(IsFloorView)); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages;
        var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5
            ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3
                ? new int?[] { 1, 2, 3, 4, 5, null, total }
                : current >= total - 2
                    ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
                    : new int?[] { 1, null, current - 1, current, current + 1, null, total };

        foreach (var page in pages)
            PageButtons.Add(page is null
                ? new InspectionPaginationButton("…", null, false, true)
                : new InspectionPaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectRoom(RoomInspectionDto? r) { SelectedRoom = r; IsDetailOpen = r is not null; SelectedDetailTab = 0; if (r is not null) { BuildChecklist(r); BuildTimeline(r); } }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedRoom = null; Checklist.Clear(); RoomTimeline.Clear(); }
    partial void OnSelectedRoomChanged(RoomInspectionDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    // ═══ WORKFLOW COMMANDS ═══

    [RelayCommand] private async Task ApproveInspection()
    {
        if (SelectedRoom is null || SelectedRoom.InspectionStatus is not "Pending") return;

        // VIP validation
        if (SelectedRoom.IsVip)
        {
            var vipResult = MessageBox.Show($"Phòng VIP {SelectedRoom.RoomNumber} — yêu cầu Supervisor kiểm tra trực tiếp.\n\nXác nhận Supervisor đã kiểm tra?", "VIP Required", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (vipResult != MessageBoxResult.OK) return;
        }

        if (!await RefreshLostFoundAsync(SelectedRoom, CancellationToken.None))
            return;

        // Validation checks
        if (SelectedRoom.ChecklistTotal == 0)
        {
            ErrorMessage = "Không thể Approve: chưa có dữ liệu checklist thật cho bản kiểm tra.";
            return;
        }
        if (SelectedRoom.ChecklistDone < SelectedRoom.ChecklistTotal)
        {
            MessageBox.Show($"Không thể Approve: Checklist chưa đủ ({SelectedRoom.ChecklistDone}/{SelectedRoom.ChecklistTotal}).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedRoom.ScorePercent < 80)
        {
            ErrorMessage = "Không thể Approve: chưa có điểm kiểm tra thật đạt tối thiểu 80%.";
            return;
        }
        if (!SelectedRoom.MinibarProcessed)
        {
            MessageBox.Show("Không thể Approve: Minibar chưa xử lý.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedRoom.HasMaintenanceIssue)
        {
            MessageBox.Show("Không thể Approve: Maintenance đang mở.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedRoom.HasLostFound)
        {
            MessageBox.Show("Không thể Approve: Lost & Found chưa xác nhận.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!await TryPersistInspectionAsync(SelectedRoom, HkTaskAction.ApproveInspection, "Kiểm tra đạt"))
            return;

        var roomNumber = SelectedRoom.RoomNumber;
        await RefreshInspectionAsync(SelectedRoom.TaskId);
        InfoMessage = $"Phòng {roomNumber} đã ĐẠT kiểm tra — Vacant Clean! FO có thể bán/phân phòng ngay.";
    }

    [RelayCommand] private async Task RejectInspection()
    {
        if (SelectedRoom is null || SelectedRoom.InspectionStatus is not "Pending") return;
        var result = MessageBox.Show($"Từ chối phòng {SelectedRoom.RoomNumber}?\n\nLý do:\n• WC chưa sạch\n• Thiếu amenities\n• Ga giường chưa thẳng\n\nYêu cầu Housekeeper làm lại?\n\nPhòng sẽ quay về module Đang dọn.", "Reject Inspection", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.OK)
        {
            if (!await TryPersistInspectionAsync(SelectedRoom, HkTaskAction.RejectInspection, SelectedRoom.InspectionNote ?? "Từ chối kiểm tra phòng"))
                return;

            var roomNumber = SelectedRoom.RoomNumber;
            await RefreshInspectionAsync(SelectedRoom.TaskId);
            InfoMessage = $"Phòng {roomNumber} KHÔNG ĐẠT — yêu cầu làm lại. Quay về module Đang dọn.";
        }
    }

    private async Task RefreshInspectionAsync(Guid taskId)
    {
        await LoadAsync(CancellationToken.None);
        var refreshed = Rooms.FirstOrDefault(room => room.TaskId == taskId);
        if (refreshed is null)
            return;

        SelectedRoom = refreshed;
        IsDetailOpen = true;
        BuildChecklist(refreshed);
        BuildTimeline(refreshed);
    }

    private async Task<bool> RefreshLostFoundAsync(RoomInspectionDto room, CancellationToken ct)
    {
        if (_lostFoundQueryService is null || _executionContext?.Current is not { } context)
        {
            ErrorMessage = "Chưa có Lost & Found query service hoặc ngữ cảnh cơ sở thật.";
            return false;
        }

        var result = await _lostFoundQueryService.SearchAsync(
            new LostFoundSearchFilter(
                context.Property.PropertyId.Value,
                null,
                null,
                null,
                null,
                room.RoomNumber,
                null),
            1,
            50,
            ct);

        var activeItems = result.Items
            .Where(item => item.Status is LostFoundStatus.Found or LostFoundStatus.Stored or LostFoundStatus.Matched)
            .ToList();
        room.HasLostFound = activeItems.Count > 0;
        room.LostFoundNote = activeItems.Count == 0
            ? null
            : string.Join("; ", activeItems.Take(3).Select(item => $"{item.ItemNumber} · {item.ItemName}"));
        return true;
    }

    private Task<bool> TryPersistInspectionAsync(HkTaskAction action, string notes, Guid? inspectorId = null) =>
        SelectedRoom is null
            ? Task.FromResult(false)
            : TryPersistInspectionAsync(SelectedRoom, action, notes, inspectorId);

    private async Task<bool> TryPersistInspectionAsync(RoomInspectionDto room, HkTaskAction action, string notes, Guid? inspectorId = null)
    {
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return false;
        }

        if (room.TaskId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi kiểm tra chưa có HousekeepingTaskId thật; không thể cập nhật.";
            return false;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để cập nhật kiểm tra phòng.";
            return false;
        }

        var resolvedInspectorId = inspectorId ?? _executionContext?.Current?.Actor.ActorId?.Value;
        var result = await _workflowService.ExecuteTaskAsync(
            new HkTaskActionRequest(
                propertyId.Value,
                room.TaskId,
                action,
                Notes: notes,
                InspectorId: resolvedInspectorId,
                InspectionChecklistData: SerializeInspectionData(room)),
            CancellationToken.None);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể cập nhật trạng thái kiểm tra phòng.";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }

    private static string? SerializeInspectionData(RoomInspectionDto room)
    {
        if (room.PersistedChecklist.Count == 0 && room.Photos.Count == 0)
            return null;

        return JsonSerializer.Serialize(new
        {
            checklistDone = room.ChecklistDone,
            checklistTotal = room.ChecklistTotal,
            checklistScore = room.ChecklistScore,
            amenitiesScore = room.AmenitiesScore,
            roomConditionScore = room.RoomConditionScore,
            minibarProcessed = room.MinibarProcessed,
            items = room.PersistedChecklist.Select(item => new
            {
                label = item.Label,
                icon = item.IconGlyph,
                category = item.Category,
                isDone = item.IsDone
            }),
            photos = room.Photos.Select(photo => new
            {
                url = photo.Url,
                label = photo.Label,
                category = photo.Category
            })
        });
    }

    [RelayCommand]
    private async Task ChangeInspector()
    {
        if (SelectedRoom is null) return;
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return;
        }

        IReadOnlyList<HkAssigneeDto> assignees;
        try
        {
            assignees = await _workflowService.GetActiveAssigneesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không tải được danh sách inspector thật: " + ex.Message;
            return;
        }

        if (assignees.Count == 0)
        {
            ErrorMessage = "Chưa có nhân sự đang hoạt động để phân công inspector.";
            return;
        }

        var selected = HousekeepingAssigneePicker.Show(
            System.Windows.Application.Current?.MainWindow,
            "Đổi Supervisor kiểm tra",
            SelectedRoom.RoomNumber,
            assignees,
            null);
        if (selected is null)
            return;

        if (!await TryPersistInspectionAsync(
                HkTaskAction.SaveInspectionEvidence,
                $"Phân công inspector: {selected.DisplayName}",
                selected.UserId))
            return;

        SelectedRoom.Inspector = selected.DisplayName;
        BuildInspectorStats(); BuildTimeline(SelectedRoom); ApplyFilters();
        InfoMessage = $"Đã phân công {selected.DisplayName} kiểm tra phòng {SelectedRoom.RoomNumber}.";
    }

    [RelayCommand]
    private async Task UploadInspectionPhoto()
    {
        if (SelectedRoom is null) return;
        if (SelectedRoom.TaskId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi kiểm tra chưa có HousekeepingTaskId thật; không thể lưu ảnh.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"Chọn ảnh kiểm tra phòng {SelectedRoom.RoomNumber}",
            Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            Multiselect = false
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                ErrorMessage = "Định dạng ảnh không được hỗ trợ.";
                return;
            }

            var storageRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ASMILE-BO",
                "HousekeepingInspectionPhotos");
            Directory.CreateDirectory(storageRoot);
            var destination = Path.Combine(
                storageRoot,
                $"{SelectedRoom.TaskId:N}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}");
            File.Copy(dialog.FileName, destination, overwrite: false);

            var photo = new InspectionPhotoDto
            {
                Url = destination,
                Label = "Inspection",
                Category = "Kiểm tra"
            };
            SelectedRoom.Photos.Add(photo);
            if (!await TryPersistInspectionAsync(SelectedRoom, HkTaskAction.SaveInspectionEvidence, "Lưu ảnh kiểm tra phòng"))
            {
                SelectedRoom.Photos.Remove(photo);
                File.Delete(destination);
                return;
            }

            ErrorMessage = string.Empty;
            InfoMessage = "Đã lưu ảnh kiểm tra thật.";
        }
        catch (IOException ex)
        {
            ErrorMessage = $"Không thể lưu ảnh kiểm tra: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = $"Không có quyền lưu ảnh kiểm tra: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenMaintenance(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || _workOrderService is null || _executionContext?.Current is not { } context)
        {
            ErrorMessage = "Chưa có Work Order query service hoặc ngữ cảnh cơ sở thật.";
            return;
        }

        try
        {
            var orders = (await _workOrderService.GetActiveAsync(context.Property.PropertyId.Value, null, ct))
                .Where(x => (SelectedRoom.RoomId is Guid roomId && x.RoomId == roomId)
                    || string.Equals(x.RoomNumber, SelectedRoom.RoomNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();
            InfoMessage = orders.Count == 0
                ? $"Phòng {SelectedRoom.RoomNumber} hiện không có Work Order đang mở."
                : string.Join("\n", orders.Select(x => $"{x.OrderNumber} · {x.Status} · {x.Description}"));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được Work Order phòng {SelectedRoom.RoomNumber}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenLostFound(CancellationToken ct)
    {
        if (SelectedRoom is null) return;
        if (!await RefreshLostFoundAsync(SelectedRoom, ct))
            return;

        InfoMessage = SelectedRoom.HasLostFound
            ? $"Lost & Found phòng {SelectedRoom.RoomNumber}: {SelectedRoom.LostFoundNote}."
            : $"Không tìm thấy Lost & Found đang lưu theo phòng {SelectedRoom.RoomNumber}.";
    }

    // ═══ VIEW MODE ═══

    [RelayCommand] private void SetGridView() => ViewMode = "Grid";
    [RelayCommand] private void SetCardView() => ViewMode = "Card";
    [RelayCommand] private void SetFloorView() => ViewMode = "Floor";

    // ═══ BATCH ═══

    private List<RoomInspectionDto> SelectedPersistableRooms() =>
        FilteredRooms.Where(room => room.IsSelected && room.TaskId != Guid.Empty).ToList();

    [RelayCommand]
    private async Task BatchApprove()
    {
        var selected = SelectedPersistableRooms().Where(room => room.InspectionStatus == "Pending").ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng Pending có HousekeepingTaskId thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected)
        {
            if (room.ChecklistTotal == 0 || room.ChecklistDone < room.ChecklistTotal || room.ScorePercent < 80
                || !room.MinibarProcessed || room.HasMaintenanceIssue || room.HasLostFound)
                continue;
            if (!await TryPersistInspectionAsync(room, HkTaskAction.ApproveInspection, "Kiểm tra đạt hàng loạt"))
                continue;
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đủ điều kiện phê duyệt hoặc command bị từ chối."
            : $"Đã phê duyệt thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task BatchReject()
    {
        var selected = SelectedPersistableRooms().Where(room => room.InspectionStatus == "Pending").ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng Pending có HousekeepingTaskId thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected)
        {
            if (!await TryPersistInspectionAsync(room, HkTaskAction.RejectInspection, room.InspectionNote ?? "Từ chối kiểm tra phòng hàng loạt"))
                continue;
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng nào được từ chối hoặc command bị từ chối."
            : $"Đã từ chối thật {changed}/{selected.Count} phòng và chuyển làm lại.";
    }

    [RelayCommand]
    private async Task BatchAssignInspector()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có HousekeepingTaskId thật.";
            return;
        }
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return;
        }

        IReadOnlyList<HkAssigneeDto> assignees;
        try
        {
            assignees = await _workflowService.GetActiveAssigneesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không tải được danh sách inspector thật: " + ex.Message;
            return;
        }

        if (assignees.Count == 0)
        {
            ErrorMessage = "Chưa có nhân sự đang hoạt động để phân công inspector.";
            return;
        }

        var selectedAssignee = HousekeepingAssigneePicker.Show(
            System.Windows.Application.Current?.MainWindow,
            "Đổi Supervisor kiểm tra hàng loạt",
            $"{selected.Count} phòng được chọn",
            assignees);
        if (selectedAssignee is null)
            return;

        var changed = 0;
        foreach (var room in selected)
        {
            if (string.Equals(room.Inspector, selectedAssignee.DisplayName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!await TryPersistInspectionAsync(
                    room,
                    HkTaskAction.SaveInspectionEvidence,
                    $"Phân công inspector hàng loạt: {selectedAssignee.DisplayName}",
                    selectedAssignee.UserId))
                continue;

            room.Inspector = selectedAssignee.DisplayName;
            changed++;
        }

        BuildInspectorStats(); BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng được đổi inspector hoặc command bị từ chối."
            : $"Đã phân công inspector thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có bản ghi kiểm tra thật trong danh sách hiện tại để xuất.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"RoomInspection-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;

        var csv = new StringBuilder()
            .AppendLine("TaskId,RoomNumber,RoomType,Floor,Area,InspectionStatus,FoStatus,Inspector,GuestName,IsVip,ChecklistDone,ChecklistTotal,MinibarProcessed,InspectionTime,RejectReason");
        foreach (var room in rows)
        {
            csv.AppendLine(string.Join(",",
                Csv(room.TaskId.ToString("D")), Csv(room.RoomNumber), Csv(room.RoomType), Csv(room.Floor), Csv(room.Area),
                Csv(room.InspectionStatus), Csv(room.FoStatus), Csv(room.Inspector ?? ""), Csv(room.GuestName ?? ""), room.IsVip,
                room.ChecklistDone, room.ChecklistTotal, room.MinibarProcessed,
                Csv(room.InspectionTime?.ToString("O") ?? ""), Csv(room.RejectReason ?? "")));
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ErrorMessage = string.Empty;
            InfoMessage = $"Đã xuất {rows.Count:N0} bản ghi kiểm tra thật.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể xuất danh sách kiểm tra: " + ex.Message;
        }
    }

    [RelayCommand]
    private void PrintList()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có bản ghi kiểm tra thật trong danh sách hiện tại để in.";
            return;
        }

        var content = new StringBuilder()
            .AppendLine("DANH SÁCH KIỂM TRA PHÒNG")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine(new string('-', 110));
        foreach (var room in rows)
        {
            content.AppendLine($"Phòng {room.RoomNumber} | {room.RoomTypeName} | {room.InspectionStatus} | " +
                $"Supervisor: {room.Inspector ?? room.Supervisor ?? "—"} | Checklist: {room.ChecklistDone}/{room.ChecklistTotal} | " +
                $"Minibar: {(room.MinibarProcessed ? "Đã xử lý" : "Chưa xử lý")} | VIP: {(room.IsVip ? "Có" : "Không")} | " +
                $"Lý do: {room.RejectReason ?? "—"}");
        }

        new PrintPreviewDialog("Danh sách kiểm tra phòng", content.ToString()).ShowDialog();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
