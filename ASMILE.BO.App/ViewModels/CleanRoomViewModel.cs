using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class CleanRoomDto
{
    public bool IsSelected { get; set; }
    public Guid TaskId { get; set; }
    public Guid? AssignedHousekeeperId { get; set; }
    public Guid? RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch { "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe", "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential", _ => RoomType };
    public string Building { get; set; } = "";
    public string Floor { get; set; } = "";
    public string Area { get; set; } = "";

    // Status in cleaning pipeline
    public string PipelineStatus { get; set; } = "Dirty"; // Dirty, Assigned, Cleaning, Inspection, Ready, Rejected
    public string PipelineLabel => PipelineStatus switch
    {
        "Dirty" => "Cần dọn", "Assigned" => "Đã phân công", "Cleaning" => "Đang dọn",
        "Inspection" => "Chờ kiểm tra", "Ready" => "Sẵn sàng", "Rejected" => "Làm lại",
        _ => PipelineStatus
    };
    public string PipelineColor => PipelineStatus switch
    {
        "Dirty" => "#DC2626", "Assigned" => "#D97706", "Cleaning" => "#2563EB",
        "Inspection" => "#7C3AED", "Ready" => "#16A34A", "Rejected" => "#EA580C",
        _ => "#64748B"
    };
    public string PipelineBg => PipelineStatus switch
    {
        "Dirty" => "#FEE2E2", "Assigned" => "#FFF7ED", "Cleaning" => "#DBEAFE",
        "Inspection" => "#F3E8FF", "Ready" => "#DCFCE7", "Rejected" => "#FFEDD5",
        _ => "#F8FAFC"
    };

    public string FoStatus { get; set; } = "VD";
    public bool IsVip { get; set; }
    public bool IsRush { get; set; }
    public int Priority => (IsVip ? 100 : 0) + (IsRush ? 50 : 0);

    // Guest / previous guest
    public string? PreviousGuest { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string? NextGuest { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public string? NextVip { get; set; }
    public string? SpecialRequest { get; set; }

    // Cleaning
    public string? Attendant { get; set; }
    public string? Supervisor { get; set; }
    public DateTime? CleanStart { get; set; }
    public DateTime? CleanEnd { get; set; }
    public int DurationMinutes => CleanStart.HasValue && CleanEnd.HasValue ? (int)(CleanEnd.Value - CleanStart.Value).TotalMinutes : CleanStart.HasValue ? (int)(DateTime.Now - CleanStart.Value).TotalMinutes : 0;
    public int ChecklistProgress { get; set; }
    public int ChecklistTotal { get; set; }
    public double ProgressPercent => ChecklistTotal > 0 ? Math.Round((double)ChecklistProgress / ChecklistTotal * 100, 1) : 0;
    public List<CleanChecklistItem> PersistedChecklist { get; } = new();

    // Inspection
    public string? Inspector { get; set; }
    public DateTime? InspectionTime { get; set; }
    public bool? InspectionPassed { get; set; } // null = not inspected
    public string? InspectionReason { get; set; }
    public string? InspectionRemark { get; set; }

    public string? Remark { get; set; }
    public string? HkNote { get; set; }
}

public sealed class CleanChecklistItem
{
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public bool IsDone { get; set; }
    public string Category { get; set; } = "";
}

public sealed class CleanRoomKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class CleanHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class CleanTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public ObservableCollection<CleanTreeItem> Children { get; } = new();
}

public sealed class CleanPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public CleanPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

// ═══ VIEW MODEL ═══

public sealed partial class CleanRoomViewModel : ViewModelBase
{
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;

    public CleanRoomViewModel() { }

    public CleanRoomViewModel(IHousekeepingBoardService boardService, IFrontOfficeExecutionContextAccessor executionContext, IHousekeepingWorkflowService? workflowService = null, ICurrentUserService? currentUserService = null, IHotelRoomMasterDataRepository? roomMasterDataRepository = null, IHotelStayRepository? stayRepository = null, IReservationRepository? reservationRepository = null)
    {
        _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _roomMasterDataRepository = roomMasterDataRepository;
        _stayRepository = stayRepository;
        _reservationRepository = reservationRepository;
    }

    public ObservableCollection<CleanRoomDto> Rooms { get; } = new();
    public ObservableCollection<CleanRoomDto> FilteredRooms { get; } = new();
    public ObservableCollection<CleanRoomKpi> DashboardKpis { get; } = new();
    public ObservableCollection<CleanChecklistItem> Checklist { get; } = new();
    public ObservableCollection<CleanHistoryEntry> RoomHistory { get; } = new();
    public ObservableCollection<CleanTreeItem> TreeItems { get; } = new();
    public ObservableCollection<CleanPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private CleanRoomDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private CleanTreeItem? _selectedTreeItem;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // Filter
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterPipeline;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterArea;
    [ObservableProperty] private string? _filterAttendant;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterRushOnly;
    [ObservableProperty] private string? _filterFoStatus;

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

    // Computed
    public int TotalRooms => Rooms.Count;
    public int DirtyCount => Rooms.Count(r => r.PipelineStatus == "Dirty");
    public int AssignedCount => Rooms.Count(r => r.PipelineStatus == "Assigned");
    public int CleaningCount => Rooms.Count(r => r.PipelineStatus == "Cleaning");
    public int InspectionCount => Rooms.Count(r => r.PipelineStatus == "Inspection");
    public int ReadyCount => Rooms.Count(r => r.PipelineStatus == "Ready");
    public int RejectedCount => Rooms.Count(r => r.PipelineStatus == "Rejected");
    public int VipCount => Rooms.Count(r => r.IsVip);
    public int RushCount => Rooms.Count(r => r.IsRush);
    public int VacantDirtyCt => Rooms.Count(r => r.FoStatus == "VD");
    public int OccupiedDirtyCt => Rooms.Count(r => r.FoStatus == "OD");
    public int CompletedToday => Rooms.Count(r => r.PipelineStatus == "Ready" && r.CleanEnd.HasValue && r.CleanEnd.Value.Date == DateTime.Today);

    public static string[] PipelineFilters { get; } = ["Tất cả", "Dirty", "Assigned", "Cleaning", "Inspection", "Ready", "Rejected"];
    public string[] FloorFilters { get; private set; } = [];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE", "FAM"];
    public static string[] AreaFilters { get; } = ["Tất cả", "Khu A", "Khu B", "Khu C"];
    public static string[] FoStatusFilters { get; } = ["Tất cả", "VD", "OD"];
    public string[] AttendantFilters { get; private set; } = [];
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
            if (_boardService is not null)
            {
                var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
                if (propertyId is null)
                {
                    ErrorMessage = "Chưa có ngữ cảnh cơ sở để tải dữ liệu phòng sạch.";
                    BuildDashboard(); BuildTree(); ApplyFilters();
                    return;
                }

                var tasks = await _boardService.GetTaskBoardAsync(propertyId.Value, null, null, null, ct);
                foreach (var task in tasks)
                    Rooms.Add(MapTask(task));

                await EnrichRoomMasterDataAsync(propertyId.Value, Rooms, ct);
                await EnrichGuestDataAsync(propertyId.Value, Rooms, ct);

                FloorFilters = Rooms.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Prepend("Tất cả").ToArray();
                AttendantFilters = Rooms.Select(x => x.Attendant!).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Prepend("Tất cả").ToArray();
                OnPropertyChanged(nameof(FloorFilters));
                OnPropertyChanged(nameof(AttendantFilters));
                BuildDashboard(); BuildTree(); ApplyFilters();
                SelectTreeItem(TreeItems.FirstOrDefault());
                return;
            }

            ErrorMessage = "Chưa có Housekeeping query service; không hiển thị dữ liệu mẫu.";
            BuildDashboard(); BuildTree(); ApplyFilters();
            return;

            /* Legacy designer fixture retained only for historical reference.
#pragma warning disable CS0162
            await Task.Delay(120, ct);
            var rng = new Random(88);
            var roomTypes = new[] { "STD", "STD", "STD", "SUP", "SUP", "DLX", "DLX", "JNR", "STE", "FAM" };
            var pipelineStatuses = new[] { "Dirty", "Dirty", "Assigned", "Assigned", "Cleaning", "Cleaning", "Inspection", "Inspection", "Ready", "Rejected" };
            var areas = new[] { "Khu A", "Khu A", "Khu B", "Khu B", "Khu C" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", null, null };
            var nextGuests = new[] { "Lê Hoàng Cường", "Hans Mueller", "Anna Rossi", "Park Ji-Sung", "David Chen", null, null, null };
            var specials = new[] { "Extra Bed", "Baby Cot", "Hoa tươi", "Trái cây", "Late Arrival", null, null, null };

            for (int floor = 1; floor <= 8; floor++)
            {
                var roomsPerFloor = floor <= 3 ? 16 : floor <= 6 ? 12 : 8;
                for (int j = 1; j <= roomsPerFloor; j++)
                {
                    var roomNum = $"{floor}{j:D2}";
                    var pipe = pipelineStatuses[rng.Next(pipelineStatuses.Length)];
                    if (pipe == "Rejected" && rng.Next(4) > 0) pipe = "Inspection";

                    var isVip = pipe is "Ready" or "Inspection" && rng.Next(5) == 1;
                    var isRush = rng.Next(6) == 1;
                    var rType = roomTypes[rng.Next(roomTypes.Length)];
                    var att = pipe is "Assigned" or "Cleaning" or "Inspection" ? CleanRoomViewModel.Attendants[rng.Next(Attendants.Length)] : null;
                    var start = pipe is "Cleaning" or "Inspection" or "Ready" or "Rejected" ? DateTime.Now.AddMinutes(-rng.Next(5, 120)) : (DateTime?)null;
                    var end = pipe is "Inspection" or "Ready" or "Rejected" ? (start.HasValue ? start.Value.AddMinutes(rng.Next(15, 45)) : DateTime.Now.AddMinutes(-30)) : (DateTime?)null;
                    var progress = pipe switch
                    {
                        "Dirty" => 0, "Assigned" => 0, "Cleaning" => rng.Next(3, 11), "Inspection" => 13, "Ready" => 13, "Rejected" => 13, _ => 0
                    };

                    Rooms.Add(new CleanRoomDto
                    {
                        RoomNumber = roomNum, RoomType = rType, Building = rng.Next(3) switch { 0 => "A", 1 => "B", _ => "C" },
                        Floor = floor.ToString(), Area = areas[rng.Next(areas.Length)],
                        PipelineStatus = pipe, FoStatus = pipe is "Ready" ? "VC" : pipe is "Inspection" ? "VC" : pipe is "Cleaning" ? "VD" : "VD",
                        IsVip = isVip, IsRush = isRush,
                        PreviousGuest = guests[rng.Next(guests.Length)],
                        CheckedOutAt = DateTime.Now.AddHours(-rng.Next(1, 48)),
                        NextGuest = pipe is "Ready" or "Inspection" ? nextGuests[rng.Next(nextGuests.Length)] : null,
                        ExpectedArrival = pipe is "Ready" or "Inspection" ? DateTime.Now.AddHours(rng.Next(2, 24)) : null,
                        NextVip = pipe is "Ready" && rng.Next(4) == 1 ? "VIP 2" : null,
                        SpecialRequest = pipe is "Ready" && rng.Next(5) == 1 ? specials[rng.Next(specials.Length)] : null,
                        Attendant = att, Supervisor = pipe is "Inspection" or "Ready" ? Supervisors[rng.Next(Supervisors.Length)] : null,
                        CleanStart = start, CleanEnd = end,
                        ChecklistProgress = progress, ChecklistTotal = 13,
                        Inspector = pipe is "Inspection" or "Ready" or "Rejected" ? Supervisors[rng.Next(Supervisors.Length)] : null,
                        InspectionTime = pipe is "Inspection" or "Ready" or "Rejected" ? (end.HasValue ? end.Value.AddMinutes(5) : DateTime.Now.AddMinutes(-10)) : null,
                        InspectionPassed = pipe switch { "Inspection" => null, "Ready" => true, "Rejected" => false, _ => null },
                        InspectionReason = pipe == "Rejected" ? (rng.Next(2) == 1 ? "WC chưa sạch" : "Thiếu amenities") : null,
                        InspectionRemark = pipe == "Rejected" ? "Cần làm lại WC và bổ sung đủ amenities" : null,
                        Remark = isVip ? "Phòng VIP — ưu tiên kiểm tra kỹ" : isRush ? "Phòng Rush — khách check-in sớm" : null,
                    });
                }
            }

            FloorFilters = Rooms.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Prepend("Tất cả").ToArray();
            AttendantFilters = Rooms.Select(x => x.Attendant ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Prepend("Tất cả").ToArray();
            OnPropertyChanged(nameof(FloorFilters));
            OnPropertyChanged(nameof(AttendantFilters));

            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
        }
            */
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static CleanRoomDto MapTask(HkTaskBoardDto task)
    {
        var rawStatus = task.TaskStatus.ToString();
        var pipeline = rawStatus switch
        {
            "WaitingInspection" => "Inspection",
            "Completed" => "Ready",
            "InProgress" or "Started" => "Cleaning",
            "Accepted" => "Cleaning",
            "Assigned" => "Assigned",
            "Paused" => "Assigned",
            "Cancelled" or "Failed" => "Rejected",
            "Pending" => "Dirty",
            _ => task.RoomStatus.ToString().Contains("Dirty", StringComparison.OrdinalIgnoreCase) ? "Dirty" : "Assigned"
        };
        var floor = task.RoomNumber.Length > 2 ? task.RoomNumber[..^2].TrimStart('0') : string.Empty;
        var priority = task.Priority.ToString();
        var isVip = task.IsVip || priority.Equals("VIP", StringComparison.OrdinalIgnoreCase);
        var isRush = priority.Equals("Rush", StringComparison.OrdinalIgnoreCase);
        var started = task.StartedAt?.ToLocalTime();
        var room = new CleanRoomDto
        {
            TaskId = task.TaskId,
            AssignedHousekeeperId = task.AssignedHousekeeperId,
            RoomId = task.RoomId,
            RoomNumber = task.RoomNumber,
            Floor = string.IsNullOrWhiteSpace(floor) ? "—" : floor,
            PipelineStatus = pipeline,
            FoStatus = task.RoomStatus.ToString(),
            IsVip = isVip,
            IsRush = isRush,
            Attendant = task.AssignedName ?? task.AssignedTo,
            CleanStart = started,
            CleanEnd = null,
            ChecklistProgress = 0,
            ChecklistTotal = 0,
            InspectionPassed = pipeline == "Ready" ? true : pipeline == "Rejected" ? false : null,
            Remark = task.IsOverdue ? "Quá hạn SLA" : null,
            HkNote = task.TaskType.ToString()
        };

        return ApplyPersistedInspectionData(room, task.InspectionChecklistData);
    }

    private static CleanRoomDto ApplyPersistedInspectionData(CleanRoomDto room, string? raw)
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

                    room.PersistedChecklist.Add(new CleanChecklistItem
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
        }
        catch (JsonException)
        {
            room.PersistedChecklist.Clear();
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

    private async Task EnrichRoomMasterDataAsync(Guid propertyId, IReadOnlyList<CleanRoomDto> rooms, CancellationToken ct)
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

    private async Task EnrichGuestDataAsync(Guid propertyId, IReadOnlyList<CleanRoomDto> rooms, CancellationToken ct)
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

            room.PreviousGuest = stay?.GuestName;
            room.CheckedOutAt = stay?.ActualCheckOutAtUtc?.ToLocalTime();
            room.NextGuest = reservation is not null && reservation.ArrivalDate > today ? reservation.GuestName : null;
            room.ExpectedArrival = reservation is not null && reservation.ArrivalDate > today
                ? reservation.ArrivalDate.ToDateTime(TimeOnly.MinValue)
                : null;
            room.NextVip = reservation?.IsVip == true ? "VIP" : null;
            room.SpecialRequest = reservation?.SpecialRequests;
        }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new CleanRoomKpi { Label = "Cần dọn", Value = TotalRooms.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Đã sạch", Value = ReadyCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Pipeline", FilterValue = "Ready" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Đang dọn", Value = CleaningCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", FilterKey = "Pipeline", FilterValue = "Cleaning" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Chờ kiểm tra", Value = InspectionCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", FilterKey = "Pipeline", FilterValue = "Inspection" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "VIP", Value = VipCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Rush", Value = RushCount.ToString(), Color = "#EA580C", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "VD (Trống bẩn)", Value = VacantDirtyCt.ToString(), Color = "#CA8A04", BgColor = "#FEF9C3", FilterKey = "FoStatus", FilterValue = "VD" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "OD (Ở bẩn)", Value = OccupiedDirtyCt.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "FoStatus", FilterValue = "OD" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Đã phân công", Value = AssignedCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", FilterKey = "Pipeline", FilterValue = "Assigned" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Làm lại", Value = RejectedCount.ToString(), Color = "#EA580C", BgColor = "#FFEDD5", FilterKey = "Pipeline", FilterValue = "Rejected" });
        DashboardKpis.Add(new CleanRoomKpi { Label = "Hoàn thành HN", Value = CompletedToday.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new CleanTreeItem { Label = "Tất cả", Count = TotalRooms, IsSelected = true });

        var dirtyNode = new CleanTreeItem { Label = "Cần dọn", Count = DirtyCount, IsExpanded = true, FilterKey = "Pipeline", FilterValue = "Dirty" };
        dirtyNode.Children.Add(new CleanTreeItem { Label = "Vacant Dirty", Count = VacantDirtyCt, FilterKey = "FoStatus", FilterValue = "VD" });
        dirtyNode.Children.Add(new CleanTreeItem { Label = "Occupied Dirty", Count = OccupiedDirtyCt, FilterKey = "FoStatus", FilterValue = "OD" });
        TreeItems.Add(dirtyNode);

        TreeItems.Add(new CleanTreeItem { Label = "VIP", Count = VipCount, FilterKey = "Vip", FilterValue = "true" });
        TreeItems.Add(new CleanTreeItem { Label = "Rush", Count = RushCount, FilterKey = "Rush", FilterValue = "true" });

        var inspectNode = new CleanTreeItem { Label = "Chờ kiểm tra", Count = InspectionCount, FilterKey = "Pipeline", FilterValue = "Inspection" };
        inspectNode.Children.Add(new CleanTreeItem { Label = "Đạt", Count = ReadyCount, FilterKey = "Pipeline", FilterValue = "Ready" });
        inspectNode.Children.Add(new CleanTreeItem { Label = "Làm lại", Count = RejectedCount, FilterKey = "Pipeline", FilterValue = "Rejected" });
        TreeItems.Add(inspectNode);

        TreeItems.Add(new CleanTreeItem { Label = "Đã hoàn thành", Count = ReadyCount, FilterKey = "Pipeline", FilterValue = "Ready" });
    }

    private void BuildChecklist(CleanRoomDto room)
    {
        Checklist.Clear();
        foreach (var item in room.PersistedChecklist)
        {
            Checklist.Add(new CleanChecklistItem
            {
                Label = item.Label,
                IconGlyph = item.IconGlyph,
                IsDone = item.IsDone,
                Category = item.Category
            });
        }
    }

    private void BuildHistory(CleanRoomDto room)
    {
        RoomHistory.Clear();
        if (room.CheckedOutAt.HasValue)
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = room.CheckedOutAt.Value, Event = "Check Out", Detail = room.PreviousGuest ?? "Khách", Color = "#DC2626" });
        if (room.PipelineStatus is not "Dirty")
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = (room.CheckedOutAt ?? DateTime.Now).AddMinutes(15), Event = "Phân công", Detail = room.Attendant ?? "Attendant", Color = "#D97706" });
        if (room.CleanStart.HasValue)
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = room.CleanStart.Value, Event = "Bắt đầu dọn", Detail = room.Attendant ?? "Attendant", Color = "#2563EB" });
        if (room.CleanEnd.HasValue)
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = room.CleanEnd.Value, Event = "Hoàn thành dọn", Detail = $"{room.DurationMinutes} phút", Color = "#0891B2" });
        if (room.InspectionPassed == true)
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = room.InspectionTime ?? DateTime.Now, Event = "Kiểm tra đạt", Detail = room.Inspector ?? "Inspector", Color = "#16A34A" });
        if (room.InspectionPassed == false)
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = room.InspectionTime ?? DateTime.Now, Event = "Kiểm tra KHÔNG đạt", Detail = room.InspectionReason ?? "Cần làm lại", Color = "#EA580C" });
        if (room.PipelineStatus == "Ready")
            RoomHistory.Add(new CleanHistoryEntry { Timestamp = DateTime.Now, Event = "Ready — Sẵn sàng bán", Detail = "FO có thể assign phòng", Color = "#16A34A" });
    }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Rooms.AsEnumerable();
        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.RoomNumber.ToLower().Contains(search) || (r.Attendant?.ToLower().Contains(search) ?? false) || (r.PreviousGuest?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrEmpty(FilterPipeline) && FilterPipeline != "Tất cả") q = q.Where(r => r.PipelineStatus == FilterPipeline);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterArea) && FilterArea != "Tất cả") q = q.Where(r => r.Area == FilterArea);
        if (!string.IsNullOrEmpty(FilterAttendant) && FilterAttendant != "Tất cả") q = q.Where(r => r.Attendant == FilterAttendant);
        if (!string.IsNullOrEmpty(FilterFoStatus) && FilterFoStatus != "Tất cả") q = q.Where(r => r.FoStatus == FilterFoStatus);
        if (FilterVipOnly) q = q.Where(r => r.IsVip);
        if (FilterRushOnly) q = q.Where(r => r.IsRush);

        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Pipeline" => q.Where(r => r.PipelineStatus == v),
                "FoStatus" => q.Where(r => r.FoStatus == v),
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
        SearchText = string.Empty; FilterPipeline = null; FilterFloor = null; FilterRoomType = null;
        FilterArea = null; FilterAttendant = null; FilterFoStatus = null;
        FilterVipOnly = false; FilterRushOnly = false; IsFilterExpanded = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void SelectTreeItem(CleanTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }

    private static void DeselectTree(CleanTreeItem item) { item.IsSelected = false; foreach (var c in item.Children) DeselectTree(c); }

    [RelayCommand] private void ApplyKpiFilter(CleanRoomKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Pipeline") FilterPipeline = kpi.FilterValue;
        else if (kpi.FilterKey == "FoStatus") FilterFoStatus = kpi.FilterValue;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterPipelineChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterAreaChanged(string? value) => ApplyFilters();
    partial void OnFilterAttendantChanged(string? value) => ApplyFilters();
    partial void OnFilterFoStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterRushOnlyChanged(bool value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }

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
                ? new CleanPaginationButton("…", null, false, true)
                : new CleanPaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectRoom(CleanRoomDto? r) { SelectedRoom = r; IsDetailOpen = r is not null; SelectedDetailTab = 0; if (r is not null) { BuildChecklist(r); BuildHistory(r); } }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedRoom = null; Checklist.Clear(); RoomHistory.Clear(); }
    partial void OnSelectedRoomChanged(CleanRoomDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    // ═══ WORKFLOW COMMANDS ═══

    [RelayCommand] private async Task AssignAttendant()
    {
        if (SelectedRoom is null || SelectedRoom.PipelineStatus is not "Dirty") return;
        var assigneeId = SelectedRoom.AssignedHousekeeperId ?? _currentUserService?.CurrentUser?.Id;
        if (assigneeId is null)
        {
            ErrorMessage = "Chưa đăng nhập tài khoản nhân sự để phân công; không tạo phân công giả.";
            return;
        }

        if (!await TryPersistTaskActionAsync(HkTaskAction.Assign, assigneeId)) return;
        SelectedRoom.PipelineStatus = "Assigned";
        SelectedRoom.AssignedHousekeeperId = assigneeId;
        SelectedRoom.Attendant = _currentUserService?.CurrentUser?.DisplayName ?? SelectedRoom.Attendant;
        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã lưu phân công phòng {SelectedRoom.RoomNumber}.";
    }

    [RelayCommand] private async Task StartCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.PipelineStatus is not "Assigned") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.Accept)) return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.StartCleaning)) return;
        SelectedRoom.PipelineStatus = "Cleaning"; SelectedRoom.CleanStart = DateTime.Now;
        SelectedRoom.ChecklistProgress = 0;
        BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); ApplyFilters();
        InfoMessage = $"Bắt đầu dọn phòng {SelectedRoom.RoomNumber}.";
    }

    [RelayCommand] private async Task CompleteCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.PipelineStatus is not "Cleaning") return;
        if (SelectedRoom.ChecklistProgress < SelectedRoom.ChecklistTotal)
        {
            var r = MessageBox.Show($"Checklist chưa hoàn tất ({SelectedRoom.ChecklistProgress}/{SelectedRoom.ChecklistTotal}). Vẫn đánh dấu hoàn thành?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (r != MessageBoxResult.OK) return;
        }
        if (!await TryPersistTaskActionAsync(HkTaskAction.CompleteCleaning)) return;
        SelectedRoom.PipelineStatus = "Inspection"; SelectedRoom.CleanEnd = DateTime.Now;
        SelectedRoom.ChecklistProgress = SelectedRoom.ChecklistTotal;
        BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); BuildHistory(SelectedRoom); ApplyFilters();
        InfoMessage = $"Hoàn thành dọn phòng {SelectedRoom.RoomNumber}. Chờ kiểm tra.";
    }

    [RelayCommand] private async Task PassInspection()
    {
        if (SelectedRoom is null || SelectedRoom.PipelineStatus is not "Inspection") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.ApproveInspection, SelectedRoom.InspectionRemark)) return;
        SelectedRoom.PipelineStatus = "Ready"; SelectedRoom.InspectionPassed = true;
        SelectedRoom.InspectionTime = DateTime.Now; SelectedRoom.FoStatus = "VC";
        BuildDashboard(); BuildTree(); BuildHistory(SelectedRoom); ApplyFilters();
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã đạt kiểm tra — Ready! FO có thể bán.";
    }

    [RelayCommand] private async Task RejectInspection()
    {
        if (SelectedRoom is null || SelectedRoom.PipelineStatus is not "Inspection") return;
        var result = MessageBox.Show($"Kiểm tra KHÔNG ĐẠT cho phòng {SelectedRoom.RoomNumber}.\n\nLý do:\n• WC chưa sạch\n• Thiếu amenities\n\nYêu cầu làm lại?", "Inspection Fail", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.OK)
        {
            if (!await TryPersistTaskActionAsync(HkTaskAction.RejectInspection, "WC chưa sạch, thiếu amenities")) return;
            SelectedRoom.PipelineStatus = "Rejected"; SelectedRoom.InspectionPassed = false;
            SelectedRoom.InspectionTime = DateTime.Now; SelectedRoom.InspectionReason = "WC chưa sạch, thiếu amenities";
            SelectedRoom.ChecklistProgress = SelectedRoom.PersistedChecklist.Count(item => item.IsDone);
            BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); BuildHistory(SelectedRoom); ApplyFilters();
            InfoMessage = $"Phòng {SelectedRoom.RoomNumber} cần làm lại.";
        }
    }

    [RelayCommand] private async Task MarkReady()
    {
        if (SelectedRoom is null) return;
        if (SelectedRoom.IsVip && SelectedRoom.PipelineStatus != "Ready" && SelectedRoom.InspectionPassed != true)
        {
            MessageBox.Show("Phòng VIP yêu cầu Supervisor Pass trước khi Ready!", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedRoom.PipelineStatus is "Inspection")
        {
            if (!await TryPersistTaskActionAsync(HkTaskAction.ApproveInspection, "Đánh dấu Ready")) return;
        }
        else
        {
            ErrorMessage = "Chỉ được chuyển Ready sau khi command kiểm tra thật đã hoàn tất.";
            return;
        }
        SelectedRoom.PipelineStatus = "Ready"; SelectedRoom.FoStatus = "VC";
        SelectedRoom.InspectionPassed = true; SelectedRoom.CleanEnd = DateTime.Now;
        SelectedRoom.ChecklistProgress = SelectedRoom.ChecklistTotal;
        BuildDashboard(); BuildTree(); BuildChecklist(SelectedRoom); BuildHistory(SelectedRoom); ApplyFilters();
        InfoMessage = $"Phòng {SelectedRoom.RoomNumber} Ready — FO có thể bán/phân phòng.";
    }

    private async Task<bool> TryPersistTaskActionAsync(HkTaskAction action, string? notes = null)
        => SelectedRoom is not null && await TryPersistTaskActionAsync(SelectedRoom, action, null, notes);

    private async Task<bool> TryPersistTaskActionAsync(HkTaskAction action, Guid? assignedHousekeeperId, string? notes = null)
        => SelectedRoom is not null && await TryPersistTaskActionAsync(SelectedRoom, action, assignedHousekeeperId, notes);

    private async Task<bool> TryPersistTaskActionAsync(CleanRoomDto room, HkTaskAction action, Guid? assignedHousekeeperId = null, string? notes = null)
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

    // ═══ BATCH ═══

    private List<CleanRoomDto> SelectedPersistableRooms() =>
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
        foreach (var room in selected.Where(room => room.PipelineStatus is "Dirty" or "Rejected"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.Assign, assigneeId)) continue;
            room.PipelineStatus = "Assigned";
            room.AssignedHousekeeperId = assigneeId;
            room.Attendant = _currentUserService?.CurrentUser?.DisplayName ?? room.Attendant;
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đủ điều kiện phân công hoặc command bị từ chối."
            : $"Đã phân công thật {changed}/{selected.Count} phòng.";
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
        foreach (var room in selected.Where(room => room.PipelineStatus == "Cleaning"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.CompleteCleaning)) continue;
            room.PipelineStatus = "Inspection";
            room.CleanEnd = DateTime.Now;
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
            .Where(room => room.PipelineStatus == "Inspection")
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
            room.PipelineStatus = "Ready";
            room.InspectionPassed = true;
            room.InspectionTime = DateTime.Now;
            room.FoStatus = "VC";
            room.ChecklistProgress = room.ChecklistTotal;
            changed++;
        }

        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng nào được kiểm tra hoặc command bị từ chối."
            : $"Đã kiểm tra đạt thật {changed}/{selected.Count} phòng và chuyển Ready.";
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (FilteredRooms.Count == 0)
        {
            InfoMessage = "Không có dữ liệu Housekeeping thật để xuất.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"housekeeping-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            AddExtension = true
        };
        if (dialog.ShowDialog() != true) return;

        static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        var csv = new StringBuilder();
        csv.AppendLine("Phòng,Loại,Tầng,Trạng thái,Attendant,Tiến độ,VIP,Rush,Ghi chú");
        foreach (var room in FilteredRooms)
            csv.AppendLine(string.Join(',', Csv(room.RoomNumber), Csv(room.RoomType), Csv(room.Floor), Csv(room.PipelineLabel),
                Csv(room.Attendant), Csv($"{room.ChecklistProgress}/{room.ChecklistTotal}"), Csv(room.IsVip ? "Có" : "Không"),
                Csv(room.IsRush ? "Có" : "Không"), Csv(room.Remark)));
        File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
        InfoMessage = $"Đã xuất {FilteredRooms.Count} dòng dữ liệu Housekeeping thật.";
    }

    [RelayCommand]
    private void PrintList()
    {
        if (FilteredRooms.Count == 0)
        {
            InfoMessage = "Không có dữ liệu Housekeeping thật để in.";
            return;
        }

        var printDialog = new System.Windows.Controls.PrintDialog();
        if (printDialog.ShowDialog() != true) return;
        var text = new System.Windows.Controls.TextBlock
        {
            Text = string.Join(Environment.NewLine, FilteredRooms.Select(room =>
                $"{room.RoomNumber} | {room.PipelineLabel} | {room.Attendant ?? "—"} | {room.ChecklistProgress}/{room.ChecklistTotal}")),
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 11,
            Margin = new Thickness(40)
        };
        printDialog.PrintVisual(text, "Housekeeping");
        InfoMessage = $"Đã gửi {FilteredRooms.Count} dòng dữ liệu Housekeeping thật tới máy in.";
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
