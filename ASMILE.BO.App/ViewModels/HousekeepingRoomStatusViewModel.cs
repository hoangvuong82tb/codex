using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class RoomStatusDto
{
    public Guid RoomId { get; set; }
    public byte[]? RowVersion { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch
    {
        "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe",
        "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential",
        "FAM" => "Family", "CON" => "Connecting", _ => RoomType
    };
    public string Floor { get; set; } = "";
    public string Area { get; set; } = "";
    public int AreaIndex { get; set; }
    public string? View { get; set; }
    public int SquareMeters { get; set; }
    public string BedType { get; set; } = "";
    public bool IsSmoking { get; set; }
    public bool IsConnecting { get; set; }
    public string RoomClass { get; set; } = "";

    // Status
    public string FoStatus { get; set; } = "VC"; // VC, VD, OC, OD, OOO, OOS, PU, EA, ED
    public string HkStatus { get; set; } = "Clean"; // Clean, Dirty, Cleaning, Inspected, Pickup
    public string RoomCondition { get; set; } = "Normal"; // Normal, Maintenance, DeepClean, Renovation

    public string FoStatusLabel => FoStatus switch
    {
        "VC" => "Vacant Clean", "VD" => "Vacant Dirty", "OC" => "Occupied Clean",
        "OD" => "Occupied Dirty", "OOO" => "Out Of Order", "OOS" => "Out Of Service",
        "PU" => "Pickup", "EA" => "Expected Arrival", "ED" => "Expected Departure",
        _ => FoStatus
    };
    public string FoStatusShort => FoStatus;
    public string HkStatusLabel => HkStatus switch
    {
        "Clean" => "Sạch", "Dirty" => "Bẩn", "Cleaning" => "Đang dọn",
        "Inspected" => "Đã kiểm tra", "Pickup" => "Chờ dọn", _ => HkStatus
    };

    // Guest
    public string? GuestName { get; set; }
    public string? ReservationCode { get; set; }
    public string? CompanyName { get; set; }
    public string? AgentName { get; set; }
    public string? Nationality { get; set; }
    public string? VipLevel { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public int StayDays => ArrivalDate.HasValue && DepartureDate.HasValue
        ? (DepartureDate.Value.DayNumber - ArrivalDate.Value.DayNumber) : 0;
    public bool IsOccupied => FoStatus is "OC" or "OD";

    // Housekeeping
    public Guid? HousekeepingTaskId { get; set; }
    public Guid? AssignedHousekeeperId { get; set; }
    public DateTime? DirtySince { get; set; }
    public DateTime? LastCleaned { get; set; }
    public string? Attendant { get; set; }
    public string? Inspector { get; set; }
    public int CleaningTimeMinutes { get; set; }

    // Amenities
    public bool HasExtraBed { get; set; }
    public bool HasBabyCot { get; set; }
    public bool MinibarNeedsCheck { get; set; }
    public bool HasLaundry { get; set; }
    public bool HasWakeUpCall { get; set; }
    public bool IsDnd { get; set; }
    public bool IsMur { get; set; }
    public bool HasLostFound { get; set; }
    public bool InspectionPending { get; set; }

    // Notes
    public string? Remark { get; set; }
    public string? HkNote { get; set; }

    // Colors
    public string BgColor => FoStatus switch
    {
        "VC" => "#DCFCE7", "VD" => "#FEF9C3", "OC" => "#DBEAFE",
        "OD" => "#FEE2E2", "OOO" => "#E5E7EB", "OOS" => "#FFEDD5",
        "PU" => "#F3E8FF", "EA" => "#E0F2FE", "ED" => "#FFF7ED",
        _ => "#FFFFFF"
    };
    public string BgBorder => FoStatus switch
    {
        "VC" => "#86EFAC", "VD" => "#FDE047", "OC" => "#93C5FD",
        "OD" => "#FCA5A5", "OOO" => "#9CA3AF", "OOS" => "#FDBA74",
        "PU" => "#C4B5FD", "EA" => "#7DD3FC", "ED" => "#FCD34D",
        _ => "#E2E8F0"
    };
    public string StatusDotColor => FoStatus switch
    {
        "VC" => "#16A34A", "VD" => "#CA8A04", "OC" => "#2563EB",
        "OD" => "#DC2626", "OOO" => "#6B7280", "OOS" => "#EA580C",
        _ => "#64748B"
    };

    // Computed warnings
    public bool HasVipWarning => !string.IsNullOrEmpty(VipLevel) && HkStatus is "Dirty" or "Cleaning";
    public bool HasArrivalWarning => FoStatus == "EA" && ArrivalDate.HasValue && ArrivalDate.Value <= DateOnly.FromDateTime(DateTime.Today).AddDays(1);
    public bool HasDepartureWarning => FoStatus == "ED" && DepartureDate.HasValue && DepartureDate.Value == DateOnly.FromDateTime(DateTime.Today);
    public bool HasMinibarWarning => MinibarNeedsCheck && IsOccupied;
    public bool HasLostFoundWarning => HasLostFound;
    public bool HasOverdueWarning => HkStatus == "Dirty" && DirtySince.HasValue && (DateTime.Now - DirtySince.Value).TotalHours > 4;
}

public sealed class RoomStatusKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string IconGlyph { get; set; } = "";
    public double? ProgressValue { get; set; }
    public string FilterKey { get; set; } = "";
    public bool ShowProgress => ProgressValue.HasValue;
}

public sealed class RoomLegendItem
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
    public string BgColor { get; set; } = "";
    public string IconGlyph { get; set; } = "";
}

public sealed class RoomTimelineEntry
{
    public string Time { get; set; } = "";
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class RoomWarningItem
{
    public string Label { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string Color { get; set; } = "#DC2626";
    public string IconGlyph { get; set; } = "";
}

// ═══ VIEW MODEL ═══

public sealed partial class HousekeepingRoomStatusViewModel : ViewModelBase
{
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly IReservationRepository? _reservationRepository;
    private readonly IAuditService? _auditService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;

    public HousekeepingRoomStatusViewModel() { }

    public HousekeepingRoomStatusViewModel(
        IHousekeepingBoardService boardService,
        IFrontOfficeExecutionContextAccessor executionContext,
        IHousekeepingWorkflowService? workflowService = null,
        IReservationRepository? reservationRepository = null,
        IAuditService? auditService = null,
        IServiceProvider? serviceProvider = null,
        IHotelRoomMasterDataRepository? roomMasterDataRepository = null)
    {
        _boardService = boardService;
        _executionContext = executionContext;
        _workflowService = workflowService;
        _reservationRepository = reservationRepository;
        _auditService = auditService;
        _serviceProvider = serviceProvider;
        _roomMasterDataRepository = roomMasterDataRepository;
    }

    // ── Collections ──
    public ObservableCollection<RoomStatusDto> Rooms { get; } = new();
    public ObservableCollection<RoomStatusDto> FilteredRooms { get; } = new();
    public ObservableCollection<RoomStatusKpi> DashboardKpis { get; } = new();
    public ObservableCollection<RoomLegendItem> LegendItems { get; } = new();
    public ObservableCollection<RoomTimelineEntry> RoomTimeline { get; } = new();
    public ObservableCollection<RoomWarningItem> Warnings { get; } = new();
    public ObservableCollection<int> FloorList { get; } = new();

    // ── Selection ──
    [ObservableProperty] private RoomStatusDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;

    // ── View Mode ──
    [ObservableProperty] private int _viewMode; // 0=Grid, 1=Table, 2=Floor
    [ObservableProperty] private int _selectedFloorView = 1;

    // ── State ──
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // ── Filter ──
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterFoStatus;
    [ObservableProperty] private string? _filterHkStatus;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterArea;
    [ObservableProperty] private string? _filterRoomClass;
    [ObservableProperty] private string? _filterRoomCondition;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterOccupiedOnly;
    [ObservableProperty] private bool _filterVacantOnly;
    [ObservableProperty] private bool _filterOooOnly;
    [ObservableProperty] private bool _filterOosOnly;
    [ObservableProperty] private string _searchGuest = string.Empty;
    [ObservableProperty] private string _searchReservation = string.Empty;

    // ── Computed ──
    public bool HasSelection => SelectedRoom is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsGridView => ViewMode == 0;
    public bool IsTableView => ViewMode == 1;
    public bool IsFloorView => ViewMode == 2;
    public int TotalRooms => Rooms.Count;
    public int VacantClean => Rooms.Count(r => r.FoStatus == "VC");
    public int VacantDirty => Rooms.Count(r => r.FoStatus == "VD");
    public int OccupiedClean => Rooms.Count(r => r.FoStatus == "OC");
    public int OccupiedDirty => Rooms.Count(r => r.FoStatus == "OD");
    public int OooCount => Rooms.Count(r => r.FoStatus == "OOO");
    public int OosCount => Rooms.Count(r => r.FoStatus == "OOS");
    public double OccupancyPct => TotalRooms > 0 ? Math.Round((double)(OccupiedClean + OccupiedDirty) / TotalRooms * 100, 1) : 0;
    public double CleanRate => TotalRooms > 0 ? Math.Round((double)(VacantClean + OccupiedClean) / TotalRooms * 100, 1) : 0;
    public double DirtyRate => TotalRooms > 0 ? Math.Round((double)(VacantDirty + OccupiedDirty) / TotalRooms * 100, 1) : 0;
    public double OooRate => TotalRooms > 0 ? Math.Round((double)OooCount / TotalRooms * 100, 1) : 0;
    public int TodaysArrivals => Rooms.Count(r => r.FoStatus == "EA" || (r.ArrivalDate.HasValue && r.ArrivalDate.Value == DateOnly.FromDateTime(DateTime.Today)));
    public int TodaysDepartures => Rooms.Count(r => r.FoStatus == "ED" || (r.DepartureDate.HasValue && r.DepartureDate.Value == DateOnly.FromDateTime(DateTime.Today)));

    // ── Static options ──
    public static string[] FoStatusFilters { get; } = ["Tất cả", "VC", "VD", "OC", "OD", "OOO", "OOS", "PU", "EA", "ED"];
    public static string[] HkStatusFilters { get; } = ["Tất cả", "Clean", "Dirty", "Cleaning", "Inspected", "Pickup"];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE", "FAM", "CON"];
    public static string[] FloorFilters { get; private set; } = [];
    public static string[] AreaFilters { get; } = ["Tất cả", "Khu A", "Khu B", "Khu C"];
    public static string[] RoomClassFilters { get; } = ["Tất cả", "Standard", "Superior", "Deluxe", "Suite"];
    public static string[] RoomConditionFilters { get; } = ["Tất cả", "Normal", "Maintenance", "DeepClean", "Renovation"];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Rooms.Clear();

            var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
            if (_boardService is not null)
            {
                if (propertyId is not Guid id)
                {
                    ErrorMessage = "Chưa chọn cơ sở để tải tình trạng phòng.";
                    return;
                }

                var snapshots = await _boardService.GetRoomSnapshotAsync(id, ct);
                if (snapshots.Count == 0)
                {
                    ErrorMessage = "Không có dữ liệu phòng cho cơ sở hiện tại.";
                    return;
                }

                foreach (var snapshot in snapshots)
                    Rooms.Add(MapSnapshot(snapshot));

                await EnrichRoomMasterDataAsync(id, Rooms, ct);
                await EnrichReservationDataAsync(id, Rooms, ct);
                await EnrichHousekeepingTaskDataAsync(id, Rooms, ct);

                FloorList.Clear();
                foreach (var floor in Rooms
                    .Select(r => r.Floor)
                    .Where(f => int.TryParse(f, out _))
                    .Select(int.Parse)
                    .Distinct()
                    .OrderBy(f => f))
                    FloorList.Add(floor);

                FloorFilters = Rooms
                    .Select(r => r.Floor)
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(f => f)
                    .Prepend("Tất cả")
                    .ToArray();

                BuildDashboard();
                BuildLegend();
                BuildWarnings();
                ApplyFilters();
                return;
            }

            ErrorMessage = "Chưa có Housekeeping query service; không hiển thị dữ liệu mẫu.";
            BuildDashboard();
            BuildLegend();
            BuildWarnings();
            ApplyFilters();
            return;

            /* Legacy designer fixture retained only for historical reference.
#pragma warning disable CS0162
            await Task.Delay(150, ct);

            var rng = new Random(77);
            var roomTypes = new[] { "STD", "STD", "STD", "SUP", "SUP", "DLX", "DLX", "JNR", "STE", "FAM" };
            var foStatuses = new[] { "VC", "VC", "VD", "OC", "OC", "OC", "OD", "OD", "OOO", "OOS", "PU", "EA", "ED" };
            var hkStatuses = new[] { "Clean", "Clean", "Clean", "Dirty", "Dirty", "Cleaning", "Inspected", "Pickup" };
            var views = new[] { "Biển", "Biển", "Thành phố", "Vườn", "Hồ bơi", null, null, null };
            var beds = new[] { "King", "King", "Twin", "Twin", "Queen", "Double" };
            var areas = new[] { ("Khu A", 0), ("Khu A", 1), ("Khu A", 2), ("Khu B", 3), ("Khu B", 4), ("Khu C", 5) };
            var attendants = new[] { "Nguyễn Lan", "Trần Mai", "Lê Hoa", "Phạm Cúc", "Hoàng Hương", null, null };
            var inspectors = new[] { "Nguyễn Giám Sát", "Trần Kiểm Tra", null, null };
            var guests = new[]
            {
                ("Nguyễn Văn An", "Việt Nam", "VIP 1"), ("Trần Thị Bình", "Việt Nam", null), ("Lê Hoàng Cường", "Việt Nam", "VIP 2"),
                ("John Smith", "Mỹ", null), ("Kim Min-Ji", "Hàn Quốc", "VIP 3"), ("Tanaka Hiroshi", "Nhật Bản", null),
                ("Chen Wei", "Trung Quốc", null), ("Sophie Martin", "Pháp", "VIP 1"), ("Hans Mueller", "Đức", null),
                ("Anna Rossi", "Ý", null), (null, null, null), (null, null, null), (null, null, null),
            };
            var companies = new[] { "VinGroup", "FPT Software", "Vietcombank", "Samsung VN", "ABC Corp", null, null, null };
            var agents = new[] { "Asia Travel", "Saigon Tourist", "Vietravel", null, null, null, null };

            var floorNames = new[] { "1", "1", "1", "1", "1", "1", "2", "2", "2", "3", "3", "3", "4", "4", "5", "5", "6", "7", "8" };

            for (int floor = 1; floor <= 8; floor++)
            {
                FloorList.Add(floor);
                var roomsPerFloor = floor <= 3 ? 32 : floor <= 6 ? 24 : 20;
                for (int j = 1; j <= roomsPerFloor; j++)
                {
                    var roomNum = $"{floor}{j:D2}";
                    var rType = roomTypes[rng.Next(roomTypes.Length)];
                    var foStatus = foStatuses[rng.Next(foStatuses.Length)];
                    if (foStatus is "OOO" or "OOS") foStatus = rng.Next(20) == 1 ? foStatus : "VC";

                    var hk = foStatus switch
                    {
                        "VC" => rng.Next(10) > 1 ? "Clean" : hkStatuses[rng.Next(hkStatuses.Length)],
                        "VD" => rng.Next(10) > 3 ? "Dirty" : "Pickup",
                        "OC" => rng.Next(2) == 1 ? "Clean" : "Cleaning",
                        "OD" => "Dirty",
                        "OOO" => "Dirty", "OOS" => "Dirty",
                        _ => hkStatuses[rng.Next(hkStatuses.Length)]
                    };

                    var (areaName, areaIdx) = areas[rng.Next(areas.Length)];
                    var g = guests[rng.Next(guests.Length)];
                    var isOcc = foStatus is "OC" or "OD";
                    var arrDate = DateOnly.FromDateTime(DateTime.Today.AddDays(rng.Next(-5, 3)));
                    var depDate = arrDate.AddDays(rng.Next(1, 5));
                    var vip = isOcc && rng.Next(5) == 1 ? g.Item3 : null;

                    Rooms.Add(new RoomStatusDto
                    {
                        RoomNumber = roomNum, RoomType = rType, Floor = floor.ToString(), Area = areaName, AreaIndex = areaIdx,
                        View = views[rng.Next(views.Length)], SquareMeters = 24 + rng.Next(0, 10) * 4,
                        BedType = beds[rng.Next(beds.Length)], IsSmoking = rng.Next(8) == 1, IsConnecting = rng.Next(10) == 1,
                        RoomClass = rType is "JNR" or "STE" or "PRE" ? "Suite" : rType is "DLX" or "SUP" ? "Deluxe" : "Standard",
                        FoStatus = foStatus, HkStatus = hk, RoomCondition = rng.Next(20) == 1 ? "Maintenance" : "Normal",
                        GuestName = isOcc ? g.Item1 : null, ReservationCode = isOcc ? $"RES-{25000 + rng.Next(9999)}" : null,
                        CompanyName = isOcc && rng.Next(3) == 1 ? companies[rng.Next(companies.Length)] : null,
                        AgentName = isOcc && rng.Next(4) == 1 ? agents[rng.Next(agents.Length)] : null,
                        Nationality = isOcc ? g.Item2 : null, VipLevel = vip,
                        Adults = isOcc ? rng.Next(1, 3) : 0, Children = isOcc ? rng.Next(0, 3) : 0,
                        ArrivalDate = isOcc ? arrDate : null, DepartureDate = isOcc ? depDate : null,
                        DirtySince = hk is "Dirty" or "Cleaning" ? DateTime.Now.AddHours(-rng.Next(1, 12)) : null,
                        LastCleaned = hk is "Clean" or "Inspected" ? DateTime.Now.AddHours(-rng.Next(1, 8)) : DateTime.Now.AddHours(-rng.Next(8, 48)),
                        Attendant = attendants[rng.Next(attendants.Length)],
                        Inspector = hk is "Inspected" ? inspectors[rng.Next(inspectors.Length)] : null,
                        CleaningTimeMinutes = rng.Next(20, 60),
                        HasExtraBed = isOcc && rng.Next(8) == 1, HasBabyCot = isOcc && rng.Next(10) == 1,
                        MinibarNeedsCheck = isOcc && rng.Next(4) == 1, HasLaundry = isOcc && rng.Next(6) == 1,
                        HasWakeUpCall = isOcc && rng.Next(8) == 1, IsDnd = isOcc && rng.Next(6) == 1,
                        IsMur = isOcc && rng.Next(10) == 1, HasLostFound = rng.Next(20) == 1,
                        InspectionPending = hk is "Clean" or "Cleaning" && rng.Next(5) == 1,
                        Remark = rng.Next(15) == 1 ? "Khách VIP — cần ưu tiên dọn sớm" : null,
                        HkNote = rng.Next(20) == 1 ? "Yêu cầu thêm 2 chai nước" : null,
                    });
                }
            }

            FloorFilters = Enumerable.Range(1, 8).Select(f => f.ToString()).Prepend("Tất cả").ToArray();

            BuildDashboard();
            BuildLegend();
            BuildWarnings();
            ApplyFilters();
        }
            */
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task EnrichRoomMasterDataAsync(Guid propertyId, IReadOnlyList<RoomStatusDto> rooms, CancellationToken ct)
    {
        if (_roomMasterDataRepository is null)
            return;

        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var master = await _roomMasterDataRepository.GetRoomDtoAsync(propertyId, room.RoomId, ct);
            if (master is null)
                continue;

            room.RoomNumber = master.RoomNumber;
            room.RoomType = master.HotelRoomTypeCode;
            room.Floor = master.Floor ?? string.Empty;
            room.Area = master.Building ?? master.Zone ?? string.Empty;
            room.RoomClass = master.HotelRoomTypeName;
        }
    }

    private async Task EnrichReservationDataAsync(Guid propertyId, IReadOnlyList<RoomStatusDto> rooms, CancellationToken ct)
    {
        if (_reservationRepository is null)
            return;

        var reservations = (await _reservationRepository.SearchAsync(
            propertyId,
            new ReservationSearchFilter(null, null, null, null, null, null, null, null, null, null, null, null),
            new PageRequest(1, 500),
            "ArrivalDate",
            ct)).Items;
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var reservation = reservations
                .Where(item => item.Status is not (ReservationStatus.Cancelled or ReservationStatus.NoShow)
                    && item.AssignedRoomId == room.RoomId
                    && item.DepartureDate >= today)
                .OrderBy(item => item.ArrivalDate)
                .FirstOrDefault();
            if (reservation is null)
                continue;

            room.GuestName = reservation.GuestName;
            room.ReservationCode = reservation.ReservationNumber;
            room.CompanyName = reservation.CompanyName;
            room.AgentName = reservation.TravelAgentName;
            room.VipLevel = reservation.IsVip ? "VIP" : null;
            room.Adults = reservation.AdultCount;
            room.Children = reservation.ChildCount;
            room.ArrivalDate = reservation.ArrivalDate;
            room.DepartureDate = reservation.DepartureDate;
        }
    }

    private async Task EnrichHousekeepingTaskDataAsync(Guid propertyId, IReadOnlyList<RoomStatusDto> rooms, CancellationToken ct)
    {
        if (_boardService is null)
            return;

        var tasks = await _boardService.GetTaskBoardAsync(propertyId, null, null, null, ct);
        var taskByRoom = tasks
            .Where(task => task.RoomId.HasValue)
            .GroupBy(task => task.RoomId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.FirstOrDefault(task => task.TaskStatus is not (HousekeepingTaskStatus.Completed or HousekeepingTaskStatus.Cancelled))
                    ?? group.First());

        foreach (var room in rooms)
        {
            if (!taskByRoom.TryGetValue(room.RoomId, out var task))
                continue;

            room.HousekeepingTaskId = task.TaskId;
            room.AssignedHousekeeperId = task.AssignedHousekeeperId;
            room.Attendant = task.AssignedName;
        }
    }

    private static RoomStatusDto MapSnapshot(HkRoomSnapshot snapshot)
    {
        var rawHousekeepingStatus = snapshot.HousekeepingStatus?.Trim();
        var housekeepingStatus = rawHousekeepingStatus switch
        {
            "VacantClean" or "OccupiedClean" or "ReadyForSale" or "Clean" => "Clean",
            "VacantDirty" or "OccupiedDirty" or "Dirty" => "Dirty",
            "Cleaning" or "CleaningInProgress" => "Cleaning",
            "Inspected" => "Inspected",
            "Pickup" => "Pickup",
            _ => string.IsNullOrWhiteSpace(rawHousekeepingStatus) ? "Unknown" : rawHousekeepingStatus
        };

        var operationalStatus = snapshot.OperationalStatus?.Trim() ?? string.Empty;
        var foStatus = operationalStatus switch
        {
            "OutOfOrder" => "OOO",
            "OutOfService" => "OOS",
            _ when rawHousekeepingStatus == "OccupiedDirty" => "OD",
            _ when rawHousekeepingStatus == "OccupiedClean" => "OC",
            _ when housekeepingStatus == "Dirty" => "VD",
            _ when housekeepingStatus is "Cleaning" or "Pickup" => "PU",
            _ => string.IsNullOrWhiteSpace(rawHousekeepingStatus) ? "—" : "VC"
        };

        return new RoomStatusDto
        {
            RoomId = snapshot.RoomId,
            RowVersion = snapshot.RowVersion,
            RoomNumber = snapshot.RoomNumber,
            Floor = snapshot.Floor ?? string.Empty,
            Area = snapshot.Zone ?? string.Empty,
            FoStatus = foStatus,
            HkStatus = housekeepingStatus,
            RoomCondition = operationalStatus is "OutOfOrder" or "OutOfService" ? "Maintenance" : string.IsNullOrWhiteSpace(operationalStatus) ? "Unknown" : "Normal"
        };
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new RoomStatusKpi { Label = "Tổng phòng", Value = TotalRooms.ToString(), FilterKey = "ALL", Color = "#4B5563", BgColor = "#F8FAFC", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Vacant Clean", Value = VacantClean.ToString(), FilterKey = "VC", Color = "#16A34A", BgColor = "#F0FDF4", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Vacant Dirty", Value = VacantDirty.ToString(), FilterKey = "VD", Color = "#CA8A04", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Occupied Clean", Value = OccupiedClean.ToString(), FilterKey = "OC", Color = "#2563EB", BgColor = "#EFF6FF", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Occupied Dirty", Value = OccupiedDirty.ToString(), FilterKey = "OD", Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "OOO", Value = OooCount.ToString(), FilterKey = "OOO", Color = "#6B7280", BgColor = "#F9FAFB", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "OOS", Value = OosCount.ToString(), FilterKey = "OOS", Color = "#EA580C", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Khách đến HN", Value = TodaysArrivals.ToString(), FilterKey = "EA", Color = "#0891B2", BgColor = "#ECFEFF", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Khách đi HN", Value = TodaysDepartures.ToString(), FilterKey = "ED", Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Công suất", Value = $"{OccupancyPct}%", FilterKey = "OCCUPIED", Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "", ProgressValue = OccupancyPct / 100 });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Tỉ lệ sạch", Value = $"{CleanRate}%", FilterKey = "CLEAN", Color = "#16A34A", BgColor = "#F0FDF4", IconGlyph = "", ProgressValue = CleanRate / 100 });
        DashboardKpis.Add(new RoomStatusKpi { Label = "Tỉ lệ bẩn", Value = $"{DirtyRate}%", FilterKey = "DIRTY", Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "", ProgressValue = DirtyRate / 100 });
    }

    private void BuildLegend()
    {
        LegendItems.Clear();
        LegendItems.Add(new RoomLegendItem { Code = "VC", Label = "Vacant Clean", Color = "#16A34A", BgColor = "#DCFCE7", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "VD", Label = "Vacant Dirty", Color = "#CA8A04", BgColor = "#FEF9C3", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "OC", Label = "Occupied Clean", Color = "#2563EB", BgColor = "#DBEAFE", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "OD", Label = "Occupied Dirty", Color = "#DC2626", BgColor = "#FEE2E2", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "OOO", Label = "Out Of Order", Color = "#6B7280", BgColor = "#E5E7EB", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "OOS", Label = "Out Of Service", Color = "#EA580C", BgColor = "#FFEDD5", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "EA", Label = "Expected Arrival", Color = "#0891B2", BgColor = "#E0F2FE", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "ED", Label = "Expected Departure", Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "PU", Label = "Pickup", Color = "#7C3AED", BgColor = "#F3E8FF", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "VIP", Label = "VIP", Color = "#E32F70", BgColor = "#FFF0F5", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "DND", Label = "DND", Color = "#4B5563", BgColor = "#F3F4F6", IconGlyph = "" });
        LegendItems.Add(new RoomLegendItem { Code = "INS", Label = "Chờ kiểm tra", Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
    }

    private void BuildWarnings()
    {
        Warnings.Clear();
        foreach (var r in Rooms.Where(r => r.HasVipWarning).Take(5))
            Warnings.Add(new RoomWarningItem { Label = "VIP chưa dọn", RoomNumber = r.RoomNumber, Color = "#DC2626", IconGlyph = "" });
        foreach (var r in Rooms.Where(r => r.HasArrivalWarning).Take(5))
            Warnings.Add(new RoomWarningItem { Label = "Khách sắp đến", RoomNumber = r.RoomNumber, Color = "#D97706", IconGlyph = "" });
        foreach (var r in Rooms.Where(r => r.HasMinibarWarning).Take(3))
            Warnings.Add(new RoomWarningItem { Label = "Minibar chưa nhập", RoomNumber = r.RoomNumber, Color = "#0891B2", IconGlyph = "" });
        foreach (var r in Rooms.Where(r => r.HasLostFoundWarning).Take(3))
            Warnings.Add(new RoomWarningItem { Label = "Lost & Found", RoomNumber = r.RoomNumber, Color = "#7C3AED", IconGlyph = "" });
    }

    private void BuildTimeline(RoomStatusDto room)
    {
        RoomTimeline.Clear();
        if (room.ArrivalDate is { } arrival)
            RoomTimeline.Add(new RoomTimelineEntry { Time = arrival.ToString("yyyy-MM-dd"), Event = "Ngày đến", Detail = room.GuestName ?? "Chưa có tên khách trong snapshot phòng", IconGlyph = "", IsActive = room.FoStatus is "OC" or "OD" });
        if (room.DepartureDate is { } departure)
            RoomTimeline.Add(new RoomTimelineEntry { Time = departure.ToString("yyyy-MM-dd"), Event = "Ngày đi", Detail = room.ReservationCode ?? "Chưa có mã đặt phòng trong snapshot phòng", IconGlyph = "", IsActive = false });
        if (room.DirtySince is { } dirtySince)
            RoomTimeline.Add(new RoomTimelineEntry { Time = dirtySince.ToString("yyyy-MM-dd HH:mm"), Event = "Bắt đầu bẩn", Detail = room.Attendant ?? "Chưa phân công attendant", IconGlyph = "", IsActive = room.HkStatus is "Dirty" or "Cleaning" or "Pickup" });
        if (room.LastCleaned is { } lastCleaned)
            RoomTimeline.Add(new RoomTimelineEntry { Time = lastCleaned.ToString("yyyy-MM-dd HH:mm"), Event = "Lần sạch gần nhất", Detail = room.CleaningTimeMinutes > 0 ? $"{room.CleaningTimeMinutes} phút" : "Không có thời lượng", IconGlyph = "", IsActive = room.HkStatus is "Clean" or "Inspected" });
        if (room.HkStatus == "Inspected")
            RoomTimeline.Add(new RoomTimelineEntry { Time = "—", Event = "Đã kiểm tra", Detail = room.Inspector ?? "Chưa ghi nhận inspector", IconGlyph = "", IsActive = true });

        // Current status is authoritative, but the time is intentionally not fabricated:
        // HkRoomSnapshot currently exposes state only, not a transition timestamp.
        if (RoomTimeline.Count == 0 || room.HkStatus is not ("Clean" or "Inspected"))
            RoomTimeline.Add(new RoomTimelineEntry { Time = "—", Event = "Trạng thái hiện tại", Detail = room.HkStatusLabel, IconGlyph = "", IsActive = true });
    }

    // ═══ FILTERS ═══

    [RelayCommand]
    private void ApplyFilters()
    {
        var q = Rooms.AsEnumerable();

        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.RoomNumber.ToLower().Contains(search) || (r.GuestName?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrWhiteSpace(SearchGuest))
            q = q.Where(r => r.GuestName?.ToLower().Contains(SearchGuest.Trim().ToLower()) ?? false);
        if (!string.IsNullOrWhiteSpace(SearchReservation))
            q = q.Where(r => r.ReservationCode?.ToLower().Contains(SearchReservation.Trim().ToLower()) ?? false);

        if (!string.IsNullOrEmpty(FilterFoStatus) && FilterFoStatus != "Tất cả")
            q = q.Where(r => r.FoStatus == FilterFoStatus);
        if (!string.IsNullOrEmpty(FilterHkStatus) && FilterHkStatus != "Tất cả")
            q = q.Where(r => r.HkStatus == FilterHkStatus);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả")
            q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả")
            q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterArea) && FilterArea != "Tất cả")
            q = q.Where(r => r.Area == FilterArea);
        if (!string.IsNullOrEmpty(FilterRoomClass) && FilterRoomClass != "Tất cả")
            q = q.Where(r => r.RoomClass == FilterRoomClass);
        if (!string.IsNullOrEmpty(FilterRoomCondition) && FilterRoomCondition != "Tất cả")
            q = q.Where(r => r.RoomCondition == FilterRoomCondition);

        if (FilterVipOnly) q = q.Where(r => !string.IsNullOrEmpty(r.VipLevel));
        if (FilterOccupiedOnly) q = q.Where(r => r.IsOccupied);
        if (FilterVacantOnly) q = q.Where(r => !r.IsOccupied && r.FoStatus != "OOO" && r.FoStatus != "OOS");
        if (FilterOooOnly) q = q.Where(r => r.FoStatus == "OOO");
        if (FilterOosOnly) q = q.Where(r => r.FoStatus == "OOS");

        if (ViewMode == 2)
            q = q.Where(r => r.Floor == SelectedFloorView.ToString());

        var list = q.OrderBy(r => int.Parse(r.RoomNumber)).ToList();
        FilteredRooms.Clear();
        foreach (var r in list) FilteredRooms.Add(r);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty; SearchGuest = string.Empty; SearchReservation = string.Empty;
        FilterFoStatus = null; FilterHkStatus = null; FilterRoomType = null; FilterFloor = null;
        FilterArea = null; FilterRoomClass = null; FilterRoomCondition = null;
        FilterVipOnly = false; FilterOccupiedOnly = false; FilterVacantOnly = false;
        FilterOooOnly = false; FilterOosOnly = false;
        IsFilterExpanded = false;
        ApplyFilters();
    }

    [RelayCommand] private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void ApplyKpiFilter(RoomStatusKpi? kpi)
    {
        if (kpi is null) return;

        FilterFoStatus = null;
        FilterHkStatus = null;
        FilterVipOnly = false;
        FilterOccupiedOnly = false;
        FilterVacantOnly = false;
        FilterOooOnly = false;
        FilterOosOnly = false;

        switch (kpi.FilterKey)
        {
            case "VC": case "VD": case "OC": case "OD": case "OOO": case "OOS": case "EA": case "ED":
                FilterFoStatus = kpi.FilterKey;
                break;
            case "OCCUPIED":
                FilterOccupiedOnly = true;
                break;
            case "CLEAN":
                FilterHkStatus = "Clean";
                break;
            case "DIRTY":
                FilterHkStatus = "Dirty";
                break;
        }

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSearchGuestChanged(string value) => ApplyFilters();
    partial void OnSearchReservationChanged(string value) => ApplyFilters();
    partial void OnFilterFoStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterHkStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterAreaChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomClassChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomConditionChanged(string? value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterOccupiedOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterVacantOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterOooOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterOosOnlyChanged(bool value) => ApplyFilters();

    // ═══ VIEW MODE ═══

    [RelayCommand] private void SetViewGrid() { ViewMode = 0; ApplyFilters(); }
    [RelayCommand] private void SetViewTable() { ViewMode = 1; ApplyFilters(); }
    [RelayCommand] private void SetViewFloor() { ViewMode = 2; ApplyFilters(); }

    [RelayCommand]
    private void SelectFloor(int floor)
    {
        SelectedFloorView = floor;
        ApplyFilters();
    }

    partial void OnViewModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsGridView));
        OnPropertyChanged(nameof(IsTableView));
        OnPropertyChanged(nameof(IsFloorView));
        ApplyFilters();
    }

    // ═══ SELECTION ═══

    [RelayCommand]
    private void SelectRoom(RoomStatusDto? r)
    {
        SelectedRoom = r;
        IsDetailOpen = r is not null;
        SelectedDetailTab = 0;
        if (r is not null) BuildTimeline(r);
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        SelectedRoom = null;
        RoomTimeline.Clear();
    }

    partial void OnSelectedRoomChanged(RoomStatusDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsDetailClosed));
    }

    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ═══ CONTEXT MENU / ACTIONS ═══

    [RelayCommand]
    private async Task MarkClean()
    {
        if (SelectedRoom is null) return;
        if (!await TryPersistActionAsync(HkRoomAction.MarkClean)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được đánh dấu sạch.";
    }

    [RelayCommand]
    private async Task MarkDirty()
    {
        if (SelectedRoom is null) return;
        if (!await TryPersistActionAsync(HkRoomAction.MarkDirty)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được đánh dấu bẩn.";
    }

    [RelayCommand]
    private async Task SendToHousekeeping()
    {
        if (SelectedRoom is null) return;
        if (!await TryPersistActionAsync(HkRoomAction.SendToHousekeeping)) return;
        InfoMessage = $"Đã gửi yêu cầu dọn phòng {SelectedRoom.RoomNumber} đến Housekeeping.";
    }

    [RelayCommand]
    private async Task InspectRoom()
    {
        if (SelectedRoom is null || SelectedRoom.HkStatus is not ("Clean" or "Cleaning")) return;
        if (!await TryPersistActionAsync(HkRoomAction.Inspect)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được kiểm tra.";
    }

    [RelayCommand]
    private async Task SetOoo()
    {
        if (SelectedRoom is null) return;
        if (!await TryPersistActionAsync(HkRoomAction.SetOutOfOrder)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được đưa vào OOO.";
    }

    [RelayCommand]
    private async Task SetOos()
    {
        if (SelectedRoom is null) return;
        if (!await TryPersistActionAsync(HkRoomAction.SetOutOfService)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được đưa vào OOS.";
    }

    [RelayCommand]
    private async Task ClearOooOos()
    {
        if (SelectedRoom is null || SelectedRoom.FoStatus is not ("OOO" or "OOS")) return;
        if (!await TryPersistActionAsync(HkRoomAction.ClearOperationalHold)) return;
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã được đưa về trạng thái sẵn sàng vệ sinh.";
    }

    private async Task<bool> TryPersistActionAsync(HkRoomAction action)
    {
        if (SelectedRoom is null)
            return false;

        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký; không cập nhật trạng thái giả.";
            return false;
        }

        if (SelectedRoom.RoomId == Guid.Empty)
        {
            ErrorMessage = $"Phòng {SelectedRoom.RoomNumber} chưa có RoomId thật; không cập nhật trạng thái giả.";
            return false;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid id)
        {
            ErrorMessage = "Chưa chọn cơ sở để thực hiện thao tác Housekeeping.";
            return false;
        }

        try
        {
            var result = await _workflowService.ExecuteAsync(new HkRoomActionRequest(id, SelectedRoom.RoomId, action), CancellationToken.None);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error?.Message ?? "Không thể cập nhật trạng thái phòng.";
                return false;
            }

            if (result.Value is not { } workflow)
            {
                ErrorMessage = "Housekeeping đã lưu nhưng không trả về trạng thái mới; màn hình không tự suy đoán dữ liệu.";
                return false;
            }

            ApplyPersistedStatus(SelectedRoom, workflow);
            SelectedRoom.RowVersion = result.Value?.RowVersion;
            ErrorMessage = string.Empty;
            BuildDashboard();
            BuildWarnings();
            BuildLegend();
            ApplyFilters();
            BuildTimeline(SelectedRoom);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật trạng thái phòng: {ex.Message}";
            return false;
        }
    }

    private static void ApplyPersistedStatus(RoomStatusDto room, HkWorkflowResult workflow)
    {
        room.HkStatus = workflow.HousekeepingStatus.ToString() switch
        {
            "VacantClean" or "OccupiedClean" or "ReadyForSale" => "Clean",
            "VacantDirty" or "OccupiedDirty" => "Dirty",
            "CleaningInProgress" => "Cleaning",
            _ => workflow.HousekeepingStatus.ToString()
        };

        room.FoStatus = workflow.OperationalStatus switch
        {
            HotelRoomOperationalStatus.OutOfOrder => "OOO",
            HotelRoomOperationalStatus.OutOfService => "OOS",
            _ => workflow.HousekeepingStatus switch
            {
                HousekeepingRoomStatus.OccupiedClean => "OC",
                HousekeepingRoomStatus.OccupiedDirty => "OD",
                HousekeepingRoomStatus.VacantDirty or HousekeepingRoomStatus.Cleaning => "VD",
                HousekeepingRoomStatus.OutOfOrder => "OOO",
                HousekeepingRoomStatus.OutOfService => "OOS",
                _ => "VC"
            }
        };

        room.RoomCondition = workflow.OperationalStatus is HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService
            ? "Maintenance"
            : "Normal";
        room.InspectionPending = workflow.HousekeepingStatus == HousekeepingRoomStatus.Cleaning;
    }

    [RelayCommand]
    private async Task ChangeAttendantAsync()
    {
        if (SelectedRoom is not { } room)
        {
            ErrorMessage = "Hãy chọn một phòng trước khi đổi attendant.";
            return;
        }

        if (_workflowService is null || _executionContext?.Current is not { } context || room.HousekeepingTaskId is not Guid taskId)
        {
            ErrorMessage = "Phòng chưa có task dọn phòng thật để đổi attendant.";
            return;
        }

        try
        {
            var assignees = await _workflowService.GetActiveAssigneesAsync(CancellationToken.None);
            var selected = HousekeepingAssigneePicker.Show(
                System.Windows.Application.Current?.MainWindow,
                "Đổi attendant",
                room.RoomNumber,
                assignees,
                room.AssignedHousekeeperId);
            if (selected is null)
                return;

            var result = await _workflowService.ExecuteTaskAsync(
                new HkTaskActionRequest(
                    context.Property.PropertyId.Value,
                    taskId,
                    HkTaskAction.Reassign,
                    AssignedHousekeeperId: selected.UserId),
                CancellationToken.None);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error?.Message ?? "Không thể đổi attendant.";
                return;
            }

            var roomId = room.RoomId;
            await LoadAsync(CancellationToken.None);
            SelectedRoom = Rooms.FirstOrDefault(item => item.RoomId == roomId);
            IsDetailOpen = SelectedRoom is not null;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể đổi attendant: " + ex.Message;
        }
    }

    [RelayCommand]
    private static void OpenMinibar()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.MinibarWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.MinibarWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private static void OpenLostFound()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.LostFoundWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.LostFoundWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private static void OpenFolio()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.FolioWorkspaceWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.FolioWorkspaceWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private async Task OpenReservation(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom?.RoomId is not { } roomId || roomId == Guid.Empty || _reservationRepository is null || _executionContext?.Current is not { } context)
        {
            ErrorMessage = "Bản ghi phòng chưa có RoomId hoặc Reservation query thật; không mở Reservation giả.";
            return;
        }

        try
        {
            var page = await _reservationRepository.SearchAsync(
                context.Property.PropertyId.Value,
                new ReservationSearchFilter(null, null, null, null, null, null, roomId, null, null, null, null, null),
                new PageRequest(1, 50), "ArrivalDate", ct);
            var businessDate = context.BusinessDate.BusinessDate;
            var reservation = page.Items
                .Where(item => item.Status is not (ReservationStatus.Cancelled or ReservationStatus.NoShow) && item.DepartureDate >= businessDate)
                .OrderBy(item => item.ArrivalDate)
                .FirstOrDefault();
            if (reservation is null)
            {
                ErrorMessage = $"Không tìm thấy Reservation thật đang/đến phòng {SelectedRoom.RoomNumber}.";
                return;
            }

            var workspace = _serviceProvider?.GetService<ReservationWorkspaceViewModel>();
            if (workspace is null)
            {
                ErrorMessage = "Không thể khởi tạo Reservation workspace.";
                return;
            }

            var existing = System.Windows.Application.Current.Windows
                .OfType<Views.ReservationStatusWindow>()
                .FirstOrDefault(window => window.IsLoaded);
            if (existing is not null)
            {
                existing.Activate();
                existing.Focus();
                return;
            }

            workspace.Mode = WorkspaceMode.Status;
            workspace.SelectedReservation = reservation;
            new Views.ReservationStatusWindow(workspace)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.Show();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải Reservation thật theo phòng: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenRoomHistory(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom?.RoomId is not { } roomId || roomId == Guid.Empty || _auditService is null)
        {
            ErrorMessage = "Bản ghi phòng chưa có RoomId hoặc audit query thật; không mở lịch sử giả.";
            return;
        }

        try
        {
            var logs = await _auditService.QueryAsync(
                entityName: "HotelRoom",
                entityId: roomId.ToString(),
                cancellationToken: ct);
            if (logs.Count == 0)
            {
                InfoMessage = $"Chưa có audit thật cho phòng {SelectedRoom.RoomNumber}. Không tạo timeline mẫu.";
                return;
            }

            var content = new StringBuilder()
                .AppendLine($"Phòng: {SelectedRoom.RoomNumber}")
                .AppendLine($"Số sự kiện audit: {logs.Count}")
                .AppendLine()
                .AppendLine("Thời gian | Người dùng | Hành động | Chi tiết")
                .AppendLine(new string('-', 120));
            foreach (var log in logs.OrderBy(log => log.CreatedAt))
            {
                content.AppendLine($"{log.CreatedAt:yyyy-MM-dd HH:mm:ss} | {log.Username} | {log.Action} | {log.Detail}");
            }

            new PrintPreviewDialog($"Lịch sử phòng {SelectedRoom.RoomNumber}", content.ToString())
            {
                Owner = System.Windows.Application.Current?.MainWindow
            }.ShowDialog();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải lịch sử phòng từ audit thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        var rows = FilteredRooms.ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có phòng thật trong bộ lọc hiện tại để xuất.", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var path = CsvExportHelper.Export(
            "housekeeping_room_status",
            ["Phòng", "Loại phòng", "Tầng", "FO", "Housekeeping", "Tình trạng", "Khách", "Mã đặt phòng", "Ngày đến", "Ngày đi", "Attendant", "Inspector"],
            rows.Select(row => (IReadOnlyList<string>)[
                row.RoomNumber,
                row.RoomTypeName,
                row.Floor,
                row.FoStatusLabel,
                row.HkStatusLabel,
                row.RoomCondition,
                row.GuestName ?? string.Empty,
                row.ReservationCode ?? string.Empty,
                row.ArrivalDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                row.DepartureDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                row.Attendant ?? string.Empty,
                row.Inspector ?? string.Empty
            ]).ToList());

        MessageBox.Show($"Đã xuất {rows.Count} phòng từ dữ liệu thật ra:\n{path}", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void PrintReport()
    {
        var rows = FilteredRooms.ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có phòng thật trong bộ lọc hiện tại để in.", "In báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine($"Tổng phòng: {rows.Count}")
            .AppendLine($"Vacant Clean: {rows.Count(x => x.FoStatus == "VC")} | Vacant Dirty: {rows.Count(x => x.FoStatus == "VD")}")
            .AppendLine($"Occupied Clean: {rows.Count(x => x.FoStatus == "OC")} | Occupied Dirty: {rows.Count(x => x.FoStatus == "OD")}")
            .AppendLine()
            .AppendLine("Phòng | FO | Housekeeping | Tình trạng | Khách | Đến | Đi")
            .AppendLine(new string('-', 110));

        foreach (var row in rows)
        {
            content.AppendLine($"{row.RoomNumber} | {row.FoStatusLabel} | {row.HkStatusLabel} | {row.RoomCondition} | {row.GuestName ?? "-"} | {row.ArrivalDate:yyyy-MM-dd} | {row.DepartureDate:yyyy-MM-dd}");
        }

        new PrintPreviewDialog("Tình trạng phòng Housekeeping", content.ToString())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        }.ShowDialog();
    }

    [RelayCommand] private static void ToggleFullScreen() { }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void RefreshDashboard() => BuildDashboard();
}
