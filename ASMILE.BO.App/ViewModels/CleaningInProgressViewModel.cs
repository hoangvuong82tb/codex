using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class CleaningInProgressDto
{
    public Guid TaskId { get; set; }
    public bool IsSelected { get; set; }
    public Guid? AssignedHousekeeperId { get; set; }
    public Guid? RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch { "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe", "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential", "FAM" => "Family", _ => RoomType };
    public string Building { get; set; } = "";
    public string Floor { get; set; } = "";
    public string Area { get; set; } = "";

    // Cleaning status
    public string CleaningStatus { get; set; } = "Assigned"; // Assigned, Cleaning, Paused, WaitingInspection, Completed, Overdue
    public string CleaningLabel => CleaningStatus switch
    {
        "Assigned" => "Đã phân công", "Cleaning" => "Đang dọn", "Paused" => "Tạm dừng",
        "WaitingInspection" => "Chờ kiểm tra", "Completed" => "Hoàn thành", "Overdue" => "Quá hạn",
        _ => CleaningStatus
    };
    public string CleaningColor => CleaningStatus switch
    {
        "Assigned" => "#D97706", "Cleaning" => "#2563EB", "Paused" => "#F59E0B",
        "WaitingInspection" => "#7C3AED", "Completed" => "#16A34A", "Overdue" => "#DC2626",
        _ => "#64748B"
    };
    public string CleaningBg => CleaningStatus switch
    {
        "Assigned" => "#FFF7ED", "Cleaning" => "#DBEAFE", "Paused" => "#FEF9C3",
        "WaitingInspection" => "#F3E8FF", "Completed" => "#DCFCE7", "Overdue" => "#FEE2E2",
        _ => "#F8FAFC"
    };

    public string FoStatus { get; set; } = "VD";
    public bool IsVip { get; set; }
    public bool IsRush { get; set; }
    public int Priority => (IsVip ? 100 : 0) + (IsRush ? 50 : 0) + (CleaningStatus == "Overdue" ? 80 : 0);

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
    public DateTime? StartTime { get; set; }
    public DateTime? PausedAt { get; set; }
    public int TotalPauseMinutes { get; set; }
    // Do not invent an SLA when the persisted task has no estimate.
    public int ExpectedDurationMinutes { get; set; }
    public DateTime? ExpectedFinish => ExpectedDurationMinutes > 0
        ? (StartTime ?? DateTime.Now).AddMinutes(ExpectedDurationMinutes + TotalPauseMinutes)
        : null;
    public int ElapsedMinutes => StartTime.HasValue
        ? (int)(DateTime.Now - StartTime.Value).TotalMinutes - TotalPauseMinutes
        : 0;
    public int RemainingMinutes => Math.Max(0, ExpectedDurationMinutes - ElapsedMinutes);
    public bool IsOverdue => ExpectedDurationMinutes > 0
        && (CleaningStatus is "Cleaning" or "Paused")
        && ElapsedMinutes > ExpectedDurationMinutes;

    // Checklist
    public int ChecklistProgress { get; set; }
    public int ChecklistTotal { get; set; }
    public double ProgressPercent => ChecklistTotal > 0 ? Math.Round((double)ChecklistProgress / ChecklistTotal * 100, 1) : 0;
    public List<CleaningChecklistItem> PersistedChecklist { get; } = new();

    // Inspection
    public string? Inspector { get; set; }
    public DateTime? InspectionTime { get; set; }
    public bool? InspectionPassed { get; set; }
    public string? InspectionReason { get; set; }
    public string? InspectionRemark { get; set; }

    // Current task detail
    public string? CurrentTask { get; set; }

    // Timeline
    public List<CleaningTimelineEntry> Timeline { get; set; } = new();

    // Photos
    public List<string> PhotoUrls { get; set; } = new();

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
}

public sealed class CleaningChecklistItem
{
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public bool IsDone { get; set; }
    public string Category { get; set; } = "";
}

public sealed class CleaningKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class CleaningTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class CleaningTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public ObservableCollection<CleaningTreeItem> Children { get; } = new();
}

public sealed class CleaningPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public CleaningPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

public sealed class HousekeeperStatDto
{
    public string Name { get; set; } = "";
    public int ActiveCleaning { get; set; }
    public int CompletedToday { get; set; }
    public double AvgMinutes { get; set; }
    public int VipCount { get; set; }
    public int RushCount { get; set; }
    public int InspectionFails { get; set; }
    public bool IsOnline { get; set; } = true;
}

public sealed class FloorViewDto
{
    public string Floor { get; set; } = "";
    public List<CleaningInProgressDto> Rooms { get; set; } = new();
}

// ═══ VIEW MODEL ═══

public sealed partial class CleaningInProgressViewModel : ViewModelBase
{
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;

    // Kept for the designer and legacy XAML preview. Runtime windows resolve
    // the service-backed constructor from the application container.
    public CleaningInProgressViewModel()
    {
    }

    public CleaningInProgressViewModel(
        IHousekeepingBoardService boardService,
        IFrontOfficeExecutionContextAccessor executionContext,
        IHousekeepingWorkflowService? workflowService = null,
        ICurrentUserService? currentUserService = null,
        IHotelRoomMasterDataRepository? roomMasterDataRepository = null,
        IHotelStayRepository? stayRepository = null,
        IReservationRepository? reservationRepository = null)
    {
        _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _roomMasterDataRepository = roomMasterDataRepository;
        _stayRepository = stayRepository;
        _reservationRepository = reservationRepository;
    }

    public ObservableCollection<CleaningInProgressDto> Rooms { get; } = new();
    public ObservableCollection<CleaningInProgressDto> FilteredRooms { get; } = new();
    public ObservableCollection<CleaningKpi> DashboardKpis { get; } = new();
    public ObservableCollection<CleaningChecklistItem> Checklist { get; } = new();
    public ObservableCollection<CleaningTimelineEntry> RoomTimeline { get; } = new();
    public ObservableCollection<CleaningTreeItem> TreeItems { get; } = new();
    public ObservableCollection<CleaningPaginationButton> PageButtons { get; } = new();
    public ObservableCollection<HousekeeperStatDto> HousekeeperStats { get; } = new();
    public ObservableCollection<FloorViewDto> FloorViews { get; } = new();

    [ObservableProperty] private CleaningInProgressDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private CleaningTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid"; // Grid, Card, Floor
    [ObservableProperty] private HousekeeperStatDto? _selectedHousekeeper;

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
    [ObservableProperty] private string? _filterHousekeeper;
    [ObservableProperty] private string? _filterSupervisor;
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterPriority;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterRushOnly;
    [ObservableProperty] private string? _filterDuration;

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
    public int ActiveCleaningCount => Rooms.Count(r => r.CleaningStatus is "Assigned" or "Cleaning" or "Paused");
    public int CompletedToday => Rooms.Count(r => r.CleaningStatus == "Completed" && r.InspectionTime.HasValue && r.InspectionTime.Value.Date == DateTime.Today);
    public int OverdueCount => Rooms.Count(r => r.IsOverdue || r.CleaningStatus == "Overdue");
    public int VipCount => Rooms.Count(r => r.IsVip);
    public int RushCount => Rooms.Count(r => r.IsRush);
    public int WaitingInspectionCount => Rooms.Count(r => r.CleaningStatus == "WaitingInspection");
    public double AvgCleaningMinutes => ActiveCleaningCount > 0 ? Math.Round(Rooms.Where(r => r.CleaningStatus is "Cleaning" or "Paused").DefaultIfEmpty().Average(r => r?.ElapsedMinutes ?? 0), 1) : 0;
    public int LongestCleaning => Rooms.Where(r => r.CleaningStatus is "Cleaning" or "Paused").DefaultIfEmpty().Max(r => r?.ElapsedMinutes ?? 0);
    public int ShortestCleaning => Rooms.Where(r => r.CleaningStatus is "Cleaning" or "Paused" && r.ElapsedMinutes > 0).DefaultIfEmpty().Min(r => r?.ElapsedMinutes ?? 0);
    public int OnlineHousekeepers => HousekeeperStats.Count(h => h.IsOnline);
    public int AssignedCount => Rooms.Count(r => r.CleaningStatus == "Assigned");
    public int CleaningCount => Rooms.Count(r => r.CleaningStatus == "Cleaning");
    public int PausedCount => Rooms.Count(r => r.CleaningStatus == "Paused");
    public int CompletedCount => Rooms.Count(r => r.CleaningStatus == "Completed");

    public static string[] StatusFilters { get; } = ["Tất cả", "Assigned", "Cleaning", "Paused", "WaitingInspection", "Completed", "Overdue"];
    public string[] FloorFilters { get; private set; } = [];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE", "FAM"];
    public static string[] AreaFilters { get; } = ["Tất cả", "Khu A", "Khu B", "Khu C"];
    public static string[] BuildingFilters { get; } = ["Tất cả", "A", "B", "C"];
    public static string[] PriorityFilters { get; } = ["Tất cả", "VIP", "Rush", "Overdue", "Normal"];
    public static string[] DurationFilters { get; } = ["Tất cả", "< 15 phút", "15-30 phút", "30-45 phút", "45-60 phút", "> 60 phút"];
    public string[] HousekeeperFilters { get; private set; } = [];
    public string[] SupervisorFilters { get; private set; } = [];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Rooms.Clear();

            // Production path: the in-progress board is backed by the
            // authoritative HousekeepingTask/HotelRoom data source. Never
            // manufacture rows when the service is registered but context is
            // unavailable or the database has no active tasks.
            if (_boardService is not null)
            {
                var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
                if (propertyId is null)
                {
                    ErrorMessage = "Chưa có ngữ cảnh cơ sở để tải dữ liệu đang dọn.";
                    BuildHousekeeperStats();
                    BuildDashboard(); BuildTree(); ApplyFilters();
                    return;
                }

                var tasks = await _boardService.GetTaskBoardAsync(propertyId.Value, null, null, null, ct);
                foreach (var task in tasks)
                    Rooms.Add(MapTask(task));

                await EnrichRoomMasterDataAsync(propertyId.Value, Rooms, ct);
                await EnrichGuestDataAsync(propertyId.Value, Rooms, ct);

                BuildHousekeeperStats();
                FloorFilters = Rooms.Select(x => x.Floor).OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
                HousekeeperFilters = Rooms.Select(x => x.AssignedTo).OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
                SupervisorFilters = Rooms.Select(x => x.Supervisor).OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
                OnPropertyChanged(nameof(FloorFilters));
                OnPropertyChanged(nameof(HousekeeperFilters));
                OnPropertyChanged(nameof(SupervisorFilters));
                BuildDashboard(); BuildTree(); ApplyFilters();
                SelectTreeItem(TreeItems.FirstOrDefault());
                return;
            }

            ErrorMessage = "Chưa có Housekeeping query service; không hiển thị dữ liệu mẫu.";
            BuildHousekeeperStats(); BuildDashboard(); BuildTree(); ApplyFilters();
            return;

            /* Legacy designer fixture retained only for historical reference.
#pragma warning disable CS0162
            await Task.Delay(120, ct);
            var rng = new Random(77);
            var roomTypes = new[] { "STD", "STD", "STD", "SUP", "SUP", "DLX", "DLX", "JNR", "STE", "FAM" };
            var statuses = new[] { "Assigned", "Assigned", "Cleaning", "Cleaning", "Cleaning", "Cleaning", "Paused", "WaitingInspection", "WaitingInspection", "Completed", "Overdue" };
            var areas = new[] { "Khu A", "Khu A", "Khu B", "Khu B", "Khu C" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", null, null };
            var nextGuests = new[] { "Lê Hoàng Cường", "Park Ji-Sung", "David Chen", "Maria Garcia", "Ahmed Al-Rashid", null, null, null };
            var specials = new[] { "Extra Bed", "Baby Cot", "Hoa tươi", "Trái cây", "Late Arrival", "Phòng không hút thuốc", null, null, null };

            for (int floor = 1; floor <= 10; floor++)
            {
                var roomsPerFloor = floor <= 5 ? 16 : floor <= 8 ? 12 : 8;
                for (int j = 1; j <= roomsPerFloor; j++)
                {
                    var roomNum = $"{floor}{j:D2}";
                    var status = statuses[rng.Next(statuses.Length)];
                    var isVip = rng.Next(7) == 1;
                    var isRush = rng.Next(5) == 1;
                    var rType = roomTypes[rng.Next(roomTypes.Length)];
                    var hk = Housekeepers[rng.Next(Housekeepers.Length)];
                    var sup = Supervisors[rng.Next(Supervisors.Length)];
                    var startTime = DateTime.Now.AddMinutes(-rng.Next(2, 150));
                    var expectedDur = rng.Next(30, 90);
                    var pauseMin = status == "Paused" ? rng.Next(5, 25) : 0;
                    var progress = status switch
                    {
                        "Assigned" => 0,
                        "Cleaning" => rng.Next(3, 12),
                        "Paused" => rng.Next(3, 10),
                        "WaitingInspection" => 13,
                        "Completed" => 13,
                        "Overdue" => rng.Next(8, 13),
                        _ => 0
                    };
                    var currentTask = status is "Cleaning" or "Paused" ? CleaningTasks[rng.Next(CleaningTasks.Length)] : null;

                    var room = new CleaningInProgressDto
                    {
                        RoomNumber = roomNum, RoomType = rType, Building = rng.Next(3) switch { 0 => "A", 1 => "B", _ => "C" },
                        Floor = floor.ToString(), Area = areas[rng.Next(areas.Length)],
                        CleaningStatus = status,
                        FoStatus = status is "Completed" ? "VC" : "VD",
                        IsVip = isVip, IsRush = isRush,
                        GuestName = guests[rng.Next(guests.Length)],
                        CheckoutTime = DateTime.Now.AddHours(-rng.Next(1, 36)),
                        NextGuest = status is "WaitingInspection" or "Completed" ? nextGuests[rng.Next(nextGuests.Length)] : null,
                        ExpectedArrival = status is "WaitingInspection" or "Completed" ? DateTime.Now.AddHours(rng.Next(2, 24)) : null,
                        GuestVip = status is "WaitingInspection" or "Completed" && rng.Next(5) == 1 ? "VIP 2" : null,
                        SpecialRequest = rng.Next(5) == 1 ? specials[rng.Next(specials.Length)] : null,
                        AssignedTo = hk, Supervisor = sup,
                        StartTime = startTime,
                        ExpectedDurationMinutes = expectedDur,
                        TotalPauseMinutes = pauseMin,
                        ChecklistProgress = progress, ChecklistTotal = 13,
                        CurrentTask = currentTask,
                        Inspector = status is "WaitingInspection" or "Completed" ? sup : null,
                        InspectionTime = status is "WaitingInspection" or "Completed" ? DateTime.Now.AddMinutes(-rng.Next(5, 30)) : null,
                        InspectionPassed = status switch { "WaitingInspection" => null, "Completed" => true, "Overdue" => false, _ => null },
                        InspectionReason = status == "Overdue" ? "Quá thời gian SLA" : null,
                        Remark = isVip ? "Phòng VIP — ưu tiên kiểm tra kỹ" : isRush ? "Phòng Rush — khách check-in sớm" : null,
                        HasMaintenanceIssue = rng.Next(8) == 1,
                        MaintenanceNote = rng.Next(8) == 1 ? "Điều hòa không lạnh" : null,
                        HasLostFound = rng.Next(10) == 1,
                        LostFoundNote = rng.Next(10) == 1 ? "Điện thoại trong ngăn kéo" : null,
                        MinibarProcessed = status is "WaitingInspection" or "Completed" || rng.Next(3) > 0,
                        MinibarNote = status is not ("WaitingInspection" or "Completed") && rng.Next(3) == 0 ? "Chưa kiểm" : null,
                    };

                    // Build timeline
                    var t0 = room.CheckoutTime ?? DateTime.Now.AddHours(-2);
                    room.Timeline.Add(new CleaningTimelineEntry { Timestamp = t0, Event = "Check-out", Detail = room.GuestName ?? "Khách", Color = "#DC2626" });
                    room.Timeline.Add(new CleaningTimelineEntry { Timestamp = t0.AddMinutes(15), Event = "Phân công", Detail = hk, Color = "#D97706" });
                    if (status is not "Assigned")
                    {
                        room.Timeline.Add(new CleaningTimelineEntry { Timestamp = t0.AddMinutes(20), Event = "Bắt đầu dọn", Detail = hk, Color = "#2563EB" });
                        if (pauseMin > 0)
                            room.Timeline.Add(new CleaningTimelineEntry { Timestamp = t0.AddMinutes(20 + rng.Next(10, 20)), Event = "Tạm dừng", Detail = $"{pauseMin} phút", Color = "#F59E0B" });
                    }
                    if (status is "WaitingInspection" or "Completed" or "Overdue")
                        room.Timeline.Add(new CleaningTimelineEntry { Timestamp = t0.AddMinutes(20 + expectedDur), Event = "Hoàn thành dọn", Detail = $"{expectedDur} phút", Color = "#0891B2" });
                    if (status == "Completed")
                        room.Timeline.Add(new CleaningTimelineEntry { Timestamp = room.InspectionTime ?? DateTime.Now, Event = "Kiểm tra đạt", Detail = sup, Color = "#16A34A" });
                    if (status == "Overdue")
                        room.Timeline.Add(new CleaningTimelineEntry { Timestamp = DateTime.Now, Event = "Quá hạn SLA", Detail = $"Vượt {room.ElapsedMinutes - room.ExpectedDurationMinutes} phút", Color = "#DC2626" });

                    Rooms.Add(room);
                }
            }

            // Build housekeeper stats
            BuildHousekeeperStats();

            FloorFilters = Rooms.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Prepend("Tất cả").ToArray();
            HousekeeperFilters = Rooms.Select(x => x.AssignedTo ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Prepend("Tất cả").ToArray();
            SupervisorFilters = Rooms.Select(x => x.Supervisor ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Prepend("Tất cả").ToArray();
            OnPropertyChanged(nameof(FloorFilters));
            OnPropertyChanged(nameof(HousekeeperFilters));
            OnPropertyChanged(nameof(SupervisorFilters));

            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
        }
            */
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static CleaningInProgressDto MapTask(HkTaskBoardDto task)
    {
        var rawStatus = task.TaskStatus.ToString();
        var status = rawStatus switch
        {
            "InProgress" or "Started" => "Cleaning",
            "Accepted" => "Cleaning",
            "WaitingInspection" => "WaitingInspection",
            "Completed" => "Completed",
            "Paused" => "Paused",
            "Cancelled" or "Failed" => "Overdue",
            "Pending" or "Assigned" => "Assigned",
            _ => "Assigned"
        };

        var floor = task.RoomNumber.Length > 2 ? task.RoomNumber[..^2].TrimStart('0') : string.Empty;
        var startedAt = task.StartedAt?.ToLocalTime();
        var isVip = task.IsVip || task.Priority.ToString().Equals("VIP", StringComparison.OrdinalIgnoreCase);
        var isRush = task.Priority.ToString().Equals("Rush", StringComparison.OrdinalIgnoreCase);

        var room = new CleaningInProgressDto
        {
            TaskId = task.TaskId,
            AssignedHousekeeperId = task.AssignedHousekeeperId,
            RoomId = task.RoomId,
            RoomNumber = task.RoomNumber,
            Floor = string.IsNullOrWhiteSpace(floor) ? "—" : floor,
            Building = "—",
            Area = "—",
            CleaningStatus = status,
            FoStatus = task.RoomStatus.ToString(),
            IsVip = isVip,
            IsRush = isRush,
            AssignedTo = task.AssignedName ?? task.AssignedTo,
            StartTime = startedAt,
            ExpectedDurationMinutes = Math.Max(0, task.EstimatedMinutesLeft ?? 0),
            ChecklistProgress = 0,
            ChecklistTotal = 0,
            CurrentTask = task.TaskType.ToString(),
            InspectionPassed = status == "Completed" ? true : null,
            Remark = task.IsOverdue ? "Quá hạn SLA" : null,
            Timeline = startedAt.HasValue
                ? [new CleaningTimelineEntry { Timestamp = startedAt.Value, Event = "Bắt đầu dọn", Detail = task.AssignedName ?? task.AssignedTo ?? "—", Color = "#2563EB" }]
                : []
        };

        return ApplyPersistedInspectionData(room, task.InspectionChecklistData);
    }

    private static CleaningInProgressDto ApplyPersistedInspectionData(CleaningInProgressDto room, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return room;

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var label = ReadString(item, "label", "title", "description", "code");
                    if (string.IsNullOrWhiteSpace(label))
                        continue;

                    room.PersistedChecklist.Add(new CleaningChecklistItem
                    {
                        Label = label,
                        IconGlyph = ReadString(item, "icon", "iconGlyph") ?? string.Empty,
                        Category = ReadString(item, "category", "group") ?? string.Empty,
                        IsDone = ReadBool(item, "isDone", "done", "completed") ?? false
                    });
                }
            }

            room.ChecklistTotal = room.PersistedChecklist.Count;
            room.ChecklistProgress = room.PersistedChecklist.Count(item => item.IsDone);
            if (root.TryGetProperty("photos", out var photos) && photos.ValueKind == JsonValueKind.Array)
            {
                foreach (var photo in photos.EnumerateArray())
                {
                    var url = ReadString(photo, "url", "path");
                    if (!string.IsNullOrWhiteSpace(url))
                        room.PhotoUrls.Add(url);
                }
            }
        }
        catch (JsonException)
        {
            // Optional legacy evidence must not be treated as valid when malformed.
            room.PersistedChecklist.Clear();
            room.PhotoUrls.Clear();
            room.ChecklistTotal = 0;
            room.ChecklistProgress = 0;
        }

        return room;
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

    private async Task EnrichRoomMasterDataAsync(Guid propertyId, IReadOnlyList<CleaningInProgressDto> rooms, CancellationToken ct)
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

    private async Task EnrichGuestDataAsync(Guid propertyId, IReadOnlyList<CleaningInProgressDto> rooms, CancellationToken ct)
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

    private void BuildHousekeeperStats()
    {
        HousekeeperStats.Clear();
        var names = Rooms
            .Select(r => r.AssignedTo)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var rooms = Rooms.Where(r => r.AssignedTo == name).ToList();
            var active = rooms.Count(r => r.CleaningStatus is "Assigned" or "Cleaning" or "Paused");
            var completed = rooms.Count(r => r.CleaningStatus is "Completed" or "WaitingInspection");
            var vips = rooms.Count(r => r.IsVip);
            var rush = rooms.Count(r => r.IsRush);
            var fails = rooms.Count(r => r.InspectionPassed == false);
            var avg = rooms.Where(r => r.CleaningStatus is "Completed" && r.StartTime.HasValue)
                .DefaultIfEmpty().Average(r => r?.ElapsedMinutes ?? 0);

            HousekeeperStats.Add(new HousekeeperStatDto
            {
                Name = name, ActiveCleaning = active, CompletedToday = completed,
                AvgMinutes = Math.Round(avg, 1),
                VipCount = vips, RushCount = rush, InspectionFails = fails,
                IsOnline = rooms.Count > 0
            });
        }

        // Build floor views
        FloorViews.Clear();
        for (int f = 1; f <= 10; f++)
        {
            var floorStr = f.ToString();
            FloorViews.Add(new FloorViewDto { Floor = floorStr, Rooms = Rooms.Where(r => r.Floor == floorStr).ToList() });
        }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new CleaningKpi { Label = "Đang dọn", Value = ActiveCleaningCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", FilterKey = "Status", FilterValue = "Cleaning" });
        DashboardKpis.Add(new CleaningKpi { Label = "Hoàn thành HN", Value = CompletedToday.ToString(), Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new CleaningKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Overdue" });
        DashboardKpis.Add(new CleaningKpi { Label = "VIP", Value = VipCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new CleaningKpi { Label = "Rush", Value = RushCount.ToString(), Color = "#EA580C", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new CleaningKpi { Label = "Chờ kiểm tra", Value = WaitingInspectionCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", FilterKey = "Status", FilterValue = "WaitingInspection" });
        DashboardKpis.Add(new CleaningKpi { Label = "TB thời gian", Value = $"{AvgCleaningMinutes}ph", Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new CleaningKpi { Label = "Lâu nhất", Value = $"{LongestCleaning}ph", Color = "#D97706", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new CleaningKpi { Label = "Nhanh nhất", Value = ShortestCleaning > 0 ? $"{ShortestCleaning}ph" : "—", Color = "#16A34A", BgColor = "#F0FDF4" });
        DashboardKpis.Add(new CleaningKpi { Label = "NV Online", Value = OnlineHousekeepers.ToString(), Color = "#4F46E5", BgColor = "#EEF2FF" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new CleaningTreeItem { Label = "Tất cả", Count = TotalRooms, IsSelected = true });

        TreeItems.Add(new CleaningTreeItem { Label = "Đang dọn", Count = CleaningCount, FilterKey = "Status", FilterValue = "Cleaning" });
        TreeItems.Add(new CleaningTreeItem { Label = "Đã phân công", Count = AssignedCount, FilterKey = "Status", FilterValue = "Assigned" });
        TreeItems.Add(new CleaningTreeItem { Label = "VIP", Count = VipCount, FilterKey = "Vip", FilterValue = "true" });
        TreeItems.Add(new CleaningTreeItem { Label = "Rush", Count = RushCount, FilterKey = "Rush", FilterValue = "true" });
        TreeItems.Add(new CleaningTreeItem { Label = "Quá hạn", Count = OverdueCount, FilterKey = "Status", FilterValue = "Overdue" });
        TreeItems.Add(new CleaningTreeItem { Label = "Sắp xong", Count = Rooms.Count(r => r.ProgressPercent >= 80 && r.CleaningStatus is "Cleaning"), FilterKey = "NearlyDone", FilterValue = "true" });
        TreeItems.Add(new CleaningTreeItem { Label = "Chờ kiểm tra", Count = WaitingInspectionCount, FilterKey = "Status", FilterValue = "WaitingInspection" });
        TreeItems.Add(new CleaningTreeItem { Label = "Tạm dừng", Count = PausedCount, FilterKey = "Status", FilterValue = "Paused" });
    }

    private void BuildChecklist(CleaningInProgressDto room)
    {
        Checklist.Clear();
        foreach (var item in room.PersistedChecklist)
        {
            Checklist.Add(new CleaningChecklistItem
            {
                Label = item.Label,
                IconGlyph = item.IconGlyph,
                IsDone = item.IsDone,
                Category = item.Category
            });
        }
    }

    private void BuildTimeline(CleaningInProgressDto room)
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
            q = q.Where(r => r.RoomNumber.ToLower().Contains(search) || (r.AssignedTo?.ToLower().Contains(search) ?? false) || (r.GuestName?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.CleaningStatus == FilterStatus);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterArea) && FilterArea != "Tất cả") q = q.Where(r => r.Area == FilterArea);
        if (!string.IsNullOrEmpty(FilterHousekeeper) && FilterHousekeeper != "Tất cả") q = q.Where(r => r.AssignedTo == FilterHousekeeper);
        if (!string.IsNullOrEmpty(FilterSupervisor) && FilterSupervisor != "Tất cả") q = q.Where(r => r.Supervisor == FilterSupervisor);
        if (!string.IsNullOrEmpty(FilterBuilding) && FilterBuilding != "Tất cả") q = q.Where(r => r.Building == FilterBuilding);
        if (!string.IsNullOrEmpty(FilterPriority) && FilterPriority != "Tất cả")
            q = FilterPriority switch { "VIP" => q.Where(r => r.IsVip), "Rush" => q.Where(r => r.IsRush), "Overdue" => q.Where(r => r.IsOverdue || r.CleaningStatus == "Overdue"), "Normal" => q.Where(r => !r.IsVip && !r.IsRush && !r.IsOverdue && r.CleaningStatus != "Overdue"), _ => q };
        if (FilterVipOnly) q = q.Where(r => r.IsVip);
        if (FilterRushOnly) q = q.Where(r => r.IsRush);
        if (!string.IsNullOrEmpty(FilterDuration) && FilterDuration != "Tất cả")
            q = FilterDuration switch
            {
                "< 15 phút" => q.Where(r => r.ElapsedMinutes < 15),
                "15-30 phút" => q.Where(r => r.ElapsedMinutes >= 15 && r.ElapsedMinutes < 30),
                "30-45 phút" => q.Where(r => r.ElapsedMinutes >= 30 && r.ElapsedMinutes < 45),
                "45-60 phút" => q.Where(r => r.ElapsedMinutes >= 45 && r.ElapsedMinutes < 60),
                "> 60 phút" => q.Where(r => r.ElapsedMinutes >= 60),
                _ => q
            };

        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Status" => q.Where(r => r.CleaningStatus == v),
                "Vip" => q.Where(r => r.IsVip),
                "Rush" => q.Where(r => r.IsRush),
                "NearlyDone" => q.Where(r => r.ProgressPercent >= 80 && r.CleaningStatus is "Cleaning"),
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
        FilterArea = null; FilterHousekeeper = null; FilterSupervisor = null; FilterBuilding = null;
        FilterPriority = null; FilterDuration = null;
        FilterVipOnly = false; FilterRushOnly = false; IsFilterExpanded = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void SelectTreeItem(CleaningTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }

    private static void DeselectTree(CleaningTreeItem item) { item.IsSelected = false; foreach (var c in item.Children) DeselectTree(c); }

    [RelayCommand] private void ApplyKpiFilter(CleaningKpi? kpi)
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
    partial void OnFilterHousekeeperChanged(string? value) => ApplyFilters();
    partial void OnFilterSupervisorChanged(string? value) => ApplyFilters();
    partial void OnFilterBuildingChanged(string? value) => ApplyFilters();
    partial void OnFilterPriorityChanged(string? value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterRushOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterDurationChanged(string? value) => ApplyFilters();

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
                ? new CleaningPaginationButton("…", null, false, true)
                : new CleaningPaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectRoom(CleaningInProgressDto? r) { SelectedRoom = r; IsDetailOpen = r is not null; SelectedDetailTab = 0; if (r is not null) { BuildChecklist(r); BuildTimeline(r); } }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedRoom = null; Checklist.Clear(); RoomTimeline.Clear(); }
    partial void OnSelectedRoomChanged(CleaningInProgressDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    // ═══ WORKFLOW COMMANDS ═══

    [RelayCommand] private async Task AssignHousekeeper()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "Assigned") return;
        var assigneeId = SelectedRoom.AssignedHousekeeperId ?? _currentUserService?.CurrentUser?.Id;
        if (assigneeId is null)
        {
            ErrorMessage = "Chưa đăng nhập tài khoản nhân sự để phân công; không tạo phân công giả.";
            return;
        }

        if (SelectedRoom.AssignedHousekeeperId is null && !await TryPersistTaskActionAsync(HkTaskAction.Assign, assigneeId)) return;
        SelectedRoom.AssignedHousekeeperId = assigneeId;
        SelectedRoom.AssignedTo = _currentUserService?.CurrentUser?.DisplayName ?? SelectedRoom.AssignedTo;
        BuildHousekeeperStats(); BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã lưu phân công phòng {SelectedRoom.RoomNumber}.";
    }

    [RelayCommand] private async Task StartCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "Assigned") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.Accept)) return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.StartCleaning)) return;
        SelectedRoom.CleaningStatus = "Cleaning"; SelectedRoom.StartTime = DateTime.Now;
        SelectedRoom.ChecklistProgress = 0;
        // Keep CurrentTask from the persisted Housekeeping task; do not fabricate a label here.
        BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); ApplyFilters();
        InfoMessage = $"Bắt đầu dọn phòng {SelectedRoom.RoomNumber}.";
    }

    [RelayCommand] private async Task PauseCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "Cleaning") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.PauseCleaning, notes: "Tạm dừng từ màn hình đang dọn phòng")) return;
        SelectedRoom.CleaningStatus = "Paused";
        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã tạm dừng dọn phòng {SelectedRoom.RoomNumber} và lưu trạng thái thật.";
    }

    [RelayCommand] private async Task ResumeCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "Paused") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.ResumeCleaning)) return;
        SelectedRoom.CleaningStatus = "Cleaning";
        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã tiếp tục dọn phòng {SelectedRoom.RoomNumber} và lưu trạng thái thật.";
    }

    [RelayCommand] private async Task CompleteCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not ("Cleaning" or "Paused")) return;
        if (SelectedRoom.ChecklistProgress < SelectedRoom.ChecklistTotal)
        {
            var r = MessageBox.Show($"Checklist chưa hoàn tất ({SelectedRoom.ChecklistProgress}/{SelectedRoom.ChecklistTotal}). Vẫn đánh dấu hoàn thành?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
        }
        if (!SelectedRoom.MinibarProcessed)
        {
            var r = MessageBox.Show("Minibar chưa được xử lý. Vẫn tiếp tục?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
        }
        if (SelectedRoom.HasMaintenanceIssue && string.IsNullOrEmpty(SelectedRoom.MaintenanceNote))
        {
            var r = MessageBox.Show("Có vấn đề bảo trì chưa xử lý. Vẫn tiếp tục?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
        }
        if (!await TryPersistTaskActionAsync(HkTaskAction.CompleteCleaning, notes: "Checklist đã hoàn tất")) return;
        SelectedRoom.CleaningStatus = "WaitingInspection";
        SelectedRoom.ChecklistProgress = SelectedRoom.ChecklistTotal;
        BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); BuildTimeline(SelectedRoom); ApplyFilters();
        InfoMessage = $"Hoàn thành dọn phòng {SelectedRoom.RoomNumber}. Chờ Supervisor kiểm tra.";
    }

    [RelayCommand] private async Task PassInspection()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "WaitingInspection") return;
            if (!await TryPersistTaskActionAsync(HkTaskAction.ApproveInspection, notes: "Kiểm tra đạt")) return;
        SelectedRoom.CleaningStatus = "Completed"; SelectedRoom.InspectionPassed = true;
        SelectedRoom.InspectionTime = DateTime.Now; SelectedRoom.FoStatus = "VC";
        BuildDashboard(); BuildTree(); BuildTimeline(SelectedRoom); ApplyFilters();
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã đạt kiểm tra — Ready! FO có thể bán.";
    }

    [RelayCommand] private async Task RejectInspection()
    {
        if (SelectedRoom is null || SelectedRoom.CleaningStatus is not "WaitingInspection") return;
        var result = MessageBox.Show($"Kiểm tra KHÔNG ĐẠT cho phòng {SelectedRoom.RoomNumber}.\n\nLý do:\n• WC chưa sạch\n• Thiếu amenities\n\nYêu cầu làm lại?", "Inspection Fail", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.OK)
        {
            if (!await TryPersistTaskActionAsync(HkTaskAction.RejectInspection, notes: "WC chưa sạch, thiếu amenities")) return;
            SelectedRoom.CleaningStatus = "Cleaning"; SelectedRoom.InspectionPassed = false;
            SelectedRoom.InspectionTime = DateTime.Now; SelectedRoom.InspectionReason = "WC chưa sạch, thiếu amenities";
            SelectedRoom.ChecklistProgress = SelectedRoom.PersistedChecklist.Count(item => item.IsDone);
            BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); BuildTimeline(SelectedRoom); ApplyFilters();
            InfoMessage = $"Phòng {SelectedRoom.RoomNumber} cần làm lại.";
        }
    }

    private Task<bool> TryPersistTaskActionAsync(HkTaskAction action, Guid? assignedHousekeeperId = null, string? notes = null) =>
        SelectedRoom is null
            ? Task.FromResult(false)
            : TryPersistTaskActionAsync(SelectedRoom, action, assignedHousekeeperId, notes);

    private async Task<bool> TryPersistTaskActionAsync(CleaningInProgressDto room, HkTaskAction action, Guid? assignedHousekeeperId = null, string? notes = null)
    {
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return false;
        }

        if (room.TaskId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi đang chọn chưa có HousekeepingTaskId thật; không thể cập nhật.";
            return false;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để cập nhật công việc dọn phòng.";
            return false;
        }

        var result = await _workflowService.ExecuteTaskAsync(
            new HkTaskActionRequest(propertyId.Value, room.TaskId, action, assignedHousekeeperId, notes),
            CancellationToken.None);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể cập nhật công việc dọn phòng.";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }

    [RelayCommand]
    private async Task ChangeHousekeeper()
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
            ErrorMessage = "Không tải được danh sách nhân sự Housekeeping thật: " + ex.Message;
            return;
        }

        if (assignees.Count == 0)
        {
            ErrorMessage = "Chưa có nhân sự Housekeeping đang hoạt động để phân công.";
            return;
        }

        var selected = HousekeepingAssigneePicker.Show(
            System.Windows.Application.Current?.MainWindow,
            "Đổi nhân viên Housekeeping",
            SelectedRoom.RoomNumber,
            assignees,
            SelectedRoom.AssignedHousekeeperId);
        if (selected is null || selected.UserId == SelectedRoom.AssignedHousekeeperId)
            return;

        if (!await TryPersistTaskActionAsync(HkTaskAction.Reassign, selected.UserId))
            return;

        SelectedRoom.AssignedHousekeeperId = selected.UserId;
        SelectedRoom.AssignedTo = selected.DisplayName;
        HousekeeperFilters = Rooms.Select(x => x.AssignedTo).OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
        OnPropertyChanged(nameof(HousekeeperFilters));
        BuildHousekeeperStats(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã đổi nhân viên dọn phòng {SelectedRoom.RoomNumber} sang {selected.DisplayName}.";
    }

    [RelayCommand]
    private async Task UploadPhoto()
    {
        if (SelectedRoom is null)
            return;

        if (SelectedRoom.TaskId == Guid.Empty || _workflowService is null)
        {
            ErrorMessage = "Bản ghi đang chọn chưa có công việc Housekeeping thật để lưu ảnh.";
            return;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để lưu ảnh phòng.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"Chọn ảnh phòng {SelectedRoom.RoomNumber}",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true)
            return;

        var evidenceDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ASMILE-BO", "HousekeepingInspectionPhotos");
        var extension = Path.GetExtension(dialog.FileName);
        var destination = Path.Combine(evidenceDirectory, $"{SelectedRoom.TaskId:N}_{Guid.NewGuid():N}{extension}");

        try
        {
            Directory.CreateDirectory(evidenceDirectory);
            File.Copy(dialog.FileName, destination, overwrite: false);
            SelectedRoom.PhotoUrls.Add(destination);

            var result = await _workflowService.ExecuteTaskAsync(
                new HkTaskActionRequest(
                    propertyId.Value,
                    SelectedRoom.TaskId,
                    HkTaskAction.SaveInspectionEvidence,
                    Notes: "Lưu ảnh trong quá trình dọn phòng",
                    InspectorId: _currentUserService?.CurrentUser?.Id,
                    InspectionChecklistData: SerializeInspectionData(SelectedRoom)),
                CancellationToken.None);
            if (result.IsFailure)
            {
                SelectedRoom.PhotoUrls.Remove(destination);
                File.Delete(destination);
                ErrorMessage = result.Error?.Message ?? "Không thể lưu bằng chứng ảnh phòng.";
                return;
            }

            ErrorMessage = string.Empty;
            InfoMessage = $"Đã lưu ảnh phòng {SelectedRoom.RoomNumber}.";
        }
        catch (Exception ex)
        {
            SelectedRoom.PhotoUrls.Remove(destination);
            if (File.Exists(destination))
                File.Delete(destination);
            ErrorMessage = $"Không thể lưu ảnh phòng: {ex.Message}";
        }
    }

    private static string? SerializeInspectionData(CleaningInProgressDto room)
    {
        if (room.PersistedChecklist.Count == 0 && room.PhotoUrls.Count == 0)
            return null;

        return JsonSerializer.Serialize(new
        {
            checklistDone = room.ChecklistProgress,
            checklistTotal = room.ChecklistTotal,
            items = room.PersistedChecklist.Select(item => new
            {
                label = item.Label,
                icon = item.IconGlyph,
                category = item.Category,
                isDone = item.IsDone
            }),
            photos = room.PhotoUrls.Select(url => new
            {
                url,
                label = "Housekeeping",
                category = "Trong quá trình dọn phòng"
            })
        });
    }

    // ═══ VIEW MODE ═══

    [RelayCommand] private void SetGridView() => ViewMode = "Grid";
    [RelayCommand] private void SetCardView() => ViewMode = "Card";
    [RelayCommand] private void SetFloorView() => ViewMode = "Floor";

    // ═══ BATCH ═══

    private List<CleaningInProgressDto> SelectedPersistableRooms() =>
        FilteredRooms.Where(room => room.IsSelected && room.TaskId != Guid.Empty).ToList();

    [RelayCommand]
    private async Task BatchAssign()
    {
        var selected = SelectedPersistableRooms();
        var assigneeId = _currentUserService?.CurrentUser?.Id;
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc Housekeeping thật.";
            return;
        }
        if (assigneeId is null)
        {
            ErrorMessage = "Chưa đăng nhập tài khoản nhân sự để phân công; không tạo phân công giả.";
            return;
        }

        var changed = 0;
        foreach (var room in selected.Where(room => room.CleaningStatus == "Assigned"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.Assign, assigneeId)) continue;
            room.AssignedHousekeeperId = assigneeId;
            room.AssignedTo = _currentUserService?.CurrentUser?.DisplayName ?? room.AssignedTo;
            changed++;
        }

        BuildHousekeeperStats(); BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đủ điều kiện phân công hoặc command bị từ chối."
            : $"Đã phân công thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task BatchChangeStaff()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc Housekeeping thật.";
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
            ErrorMessage = "Không tải được danh sách nhân sự Housekeeping thật: " + ex.Message;
            return;
        }

        if (assignees.Count == 0)
        {
            ErrorMessage = "Chưa có nhân sự Housekeeping đang hoạt động để phân công.";
            return;
        }

        var selectedAssignee = HousekeepingAssigneePicker.Show(
            System.Windows.Application.Current?.MainWindow,
            "Đổi nhân viên Housekeeping hàng loạt",
            $"{selected.Count} phòng được chọn",
            assignees);
        if (selectedAssignee is null)
            return;

        var changed = 0;
        foreach (var room in selected.Where(room => room.CleaningStatus is "Assigned" or "Cleaning" or "Paused"))
        {
            if (room.AssignedHousekeeperId == selectedAssignee.UserId)
                continue;
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.Reassign, selectedAssignee.UserId))
                continue;

            room.AssignedHousekeeperId = selectedAssignee.UserId;
            room.AssignedTo = selectedAssignee.DisplayName;
            changed++;
        }

        HousekeeperFilters = Rooms.Select(x => x.AssignedTo).OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Prepend("Tất cả").ToArray();
        OnPropertyChanged(nameof(HousekeeperFilters));
        BuildHousekeeperStats(); BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đủ điều kiện đổi nhân viên hoặc command bị từ chối."
            : $"Đã đổi nhân viên thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task BatchComplete()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc Housekeeping thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected.Where(room => room.CleaningStatus is "Cleaning" or "Paused"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.CompleteCleaning, notes: "Hoàn thành hàng loạt")) continue;
            room.CleaningStatus = "WaitingInspection";
            room.ChecklistProgress = room.ChecklistTotal;
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đang dọn đủ điều kiện hoàn thành hoặc command bị từ chối."
            : $"Đã hoàn thành dọn thật {changed}/{selected.Count} phòng và chuyển chờ kiểm tra.";
    }

    [RelayCommand]
    private async Task BatchInspect()
    {
        var selected = SelectedPersistableRooms()
            .Where(room => room.CleaningStatus == "WaitingInspection")
            .ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng đang chờ kiểm tra và có HousekeepingTaskId thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected)
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.ApproveInspection, notes: "Kiểm tra hàng loạt")) continue;
            room.CleaningStatus = "Completed";
            room.InspectionPassed = true;
            room.InspectionTime = DateTime.Now;
            room.FoStatus = "VC";
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng nào được kiểm tra hoặc command bị từ chối."
            : $"Đã kiểm tra đạt thật {changed}/{selected.Count} phòng và chuyển Ready.";
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có công việc Housekeeping thật trong danh sách hiện tại để xuất.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"Housekeeping-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;

        var csv = new StringBuilder()
            .AppendLine("TaskId,RoomNumber,RoomType,Floor,Area,CleaningStatus,FoStatus,AssignedTo,Supervisor,GuestName,IsVip,IsRush,ChecklistProgress,ChecklistTotal,StartTime,InspectionTime");
        foreach (var room in rows)
        {
            csv.AppendLine(string.Join(",",
                Csv(room.TaskId.ToString("D")), Csv(room.RoomNumber), Csv(room.RoomType), Csv(room.Floor), Csv(room.Area),
                Csv(room.CleaningStatus), Csv(room.FoStatus), Csv(room.AssignedTo), Csv(room.Supervisor), Csv(room.GuestName ?? ""),
                room.IsVip, room.IsRush, room.ChecklistProgress, room.ChecklistTotal,
                Csv(room.StartTime?.ToString("O") ?? ""), Csv(room.InspectionTime?.ToString("O") ?? "")));
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ErrorMessage = string.Empty;
            InfoMessage = $"Đã xuất {rows.Count:N0} công việc Housekeeping thật.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể xuất danh sách Housekeeping: " + ex.Message;
        }
    }

    [RelayCommand]
    private void PrintList()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có công việc Housekeeping thật trong danh sách hiện tại để in.";
            return;
        }

        var content = new StringBuilder()
            .AppendLine("DANH SÁCH HOUSEKEEPING ĐANG DỌN")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine(new string('-', 120));
        foreach (var room in rows)
        {
            content.AppendLine(
                $"Phòng {room.RoomNumber} | {room.RoomTypeName} | {room.CleaningLabel} | " +
                $"Tầng {room.Floor} / {room.Area} | NV: {room.AssignedTo ?? "—"} | " +
                $"Khách: {room.GuestName ?? "—"} | Checklist: {room.ChecklistProgress}/{room.ChecklistTotal} | " +
                $"VIP: {(room.IsVip ? "Có" : "Không")} | Rush: {(room.IsRush ? "Có" : "Không")}");
        }

        new PrintPreviewDialog("Danh sách Housekeeping", content.ToString()).ShowDialog();
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
