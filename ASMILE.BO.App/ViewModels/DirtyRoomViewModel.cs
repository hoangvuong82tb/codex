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
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class DirtyRoomDto : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public Guid TaskId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? AssignedHousekeeperId { get; set; }

    private void Notify(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public void NotifyChecklistChanged()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ChecklistDone)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ProgressPct)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsChecklistComplete)));
    }
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch { "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe", "JNR" => "Junior Suite", "STE" => "Suite", _ => RoomType };
    public string Floor { get; set; } = "";
    public string Area { get; set; } = "";
    public string? View { get; set; }

    // Dirty status
    public string DirtyStatus { get; set; } = "Dirty"; // Dirty, Cleaning, Paused, Inspection, Clean, FailInspection, OOO
    public string DirtyLabel => DirtyStatus switch
    {
        "Dirty" => "Cần dọn", "Cleaning" => "Đang dọn", "Paused" => "Tạm dừng",
        "Inspection" => "Chờ kiểm tra", "Clean" => "Đã sạch", "FailInspection" => "Fail KT",
        "OOO" => "Out Of Order", _ => DirtyStatus
    };
    public string DirtyColor => DirtyStatus switch
    {
        "Dirty" => "#DC2626", "Cleaning" => "#2563EB", "Paused" => "#D97706",
        "Inspection" => "#EA580C", "Clean" => "#16A34A", "FailInspection" => "#E32F70",
        "OOO" => "#6B7280", _ => "#64748B"
    };
    public string DirtyBg => DirtyStatus switch
    {
        "Dirty" => "#FEF2F2", "Cleaning" => "#EFF6FF", "Paused" => "#FFF7ED",
        "Inspection" => "#FFF7ED", "Clean" => "#F0FDF4", "FailInspection" => "#FFF0F5",
        "OOO" => "#F3F4F6", _ => "#F8FAFC"
    };

    public DateTime? DirtySince { get; set; }
    public int DirtyHours => DirtySince.HasValue ? (int)(DateTime.Now - DirtySince.Value).TotalHours : 0;
    public bool IsOverdue => DirtyStatus == "Dirty" && DirtyHours > 2;

    // Priority
    public bool IsVip { get; set; }
    public bool IsRush { get; set; }
    public RoomPriority TaskPriority { get; set; } = RoomPriority.Normal;
    public int PriorityScore => (IsVip ? 200 : 0) + (IsRush ? 100 : 0) + (IsOverdue ? 50 : 0);

    // Guest
    public string? GuestName { get; set; }
    public string? ReservationCode { get; set; }
    public string? CompanyName { get; set; }
    public string? MarketName { get; set; }
    public string? SourceName { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string? VipLevel { get; set; }
    public decimal? Balance { get; set; }
    public string? FolioCode { get; set; }

    // Housekeeping
    public string? Attendant { get; set; }
    public string? Inspector { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CleanStart { get; set; }
    public DateTime? CleanEnd { get; set; }
    public int DurationMin => CleanStart.HasValue && CleanEnd.HasValue ? (int)(CleanEnd.Value - CleanStart.Value).TotalMinutes : CleanStart.HasValue ? (int)(DateTime.Now - CleanStart.Value).TotalMinutes : 0;

    // Checklist comes from persisted InspectionChecklistData; do not fabricate a template here.
    public bool CkBed { get; set; }
    public bool CkToilet { get; set; }
    public bool CkTowel { get; set; }
    public bool CkAmenities { get; set; }
    public bool CkMinibar { get; set; }
    public bool CkBalcony { get; set; }
    public bool CkAirCon { get; set; }
    public bool CkTv { get; set; }
    public bool CkCabinet { get; set; }
    public bool CkSlippers { get; set; }
    public bool CkElectric { get; set; }
    public bool CkLock { get; set; }
    public bool CkOther { get; set; }
    public List<DirtyChecklistItem> PersistedChecklist { get; } = new();
    public int ChecklistDone => PersistedChecklist.Count(item => item.IsDone);
    public int ChecklistTotal => PersistedChecklist.Count;
    public double ProgressPct => ChecklistTotal > 0 ? Math.Round((double)ChecklistDone / ChecklistTotal * 100, 1) : 0;
    public bool IsChecklistComplete => ChecklistDone >= ChecklistTotal;

    // Photos
    public bool HasBeforePhoto { get; set; }
    public bool HasAfterPhoto { get; set; }
    public List<string> PhotoUrls { get; } = new();

    // Related
    public bool HasLostFound { get; set; }
    public bool HasMinibarIssue { get; set; }
    public bool HasMaintenance { get; set; }
    public string? MaintenanceDesc { get; set; }

    public string? Remark { get; set; }
    public string? HkNote { get; set; }

    // Multi-select
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; Notify(nameof(IsSelected)); } }
}

public sealed class DirtyChecklistItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public string Category { get; set; } = "";
    private bool _isDone;
    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (_isDone == value) return;
            _isDone = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsDone)));
        }
    }
}

public sealed class DirtyDashboardKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class DirtyPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public DirtyPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

public sealed class DirtyHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

// ═══ VIEW MODEL ═══

public sealed partial class DirtyRoomViewModel : ViewModelBase
{
    private readonly IHousekeepingBoardService? _boardService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IHousekeepingWorkflowService? _workflowService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;
    private readonly IWorkOrderService? _workOrderService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;

    public DirtyRoomViewModel() { }

    public DirtyRoomViewModel(IHousekeepingBoardService boardService, IFrontOfficeExecutionContextAccessor executionContext, IHousekeepingWorkflowService? workflowService = null, ICurrentUserService? currentUserService = null, IHotelStayRepository? stayRepository = null, IReservationRepository? reservationRepository = null, IWorkOrderService? workOrderService = null, IServiceProvider? serviceProvider = null, IHotelRoomMasterDataRepository? roomMasterDataRepository = null)
    {
        _boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _stayRepository = stayRepository;
        _reservationRepository = reservationRepository;
        _workOrderService = workOrderService;
        _serviceProvider = serviceProvider;
        _roomMasterDataRepository = roomMasterDataRepository;
    }

    public ObservableCollection<DirtyRoomDto> Rooms { get; } = new();
    public ObservableCollection<DirtyRoomDto> FilteredRooms { get; } = new();
    public ObservableCollection<DirtyDashboardKpi> DashboardKpis { get; } = new();
    public ObservableCollection<DirtyHistoryEntry> RoomHistory { get; } = new();
    public ObservableCollection<DirtyPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private DirtyRoomDto? _selectedRoom;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // Filter
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterDirtyStatus;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterArea;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterAttendant;
    [ObservableProperty] private bool _filterVip;
    [ObservableProperty] private bool _filterRush;
    [ObservableProperty] private bool _filterOverdue;
    [ObservableProperty] private bool _filterInspection;
    [ObservableProperty] private bool _filterOoo;
    [ObservableProperty] private bool _filterMine;
    [ObservableProperty] private string? _filterShift;

    // Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount}";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedRoom is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool AllSelected => FilteredRooms.Count > 0 && FilteredRooms.All(r => r.IsSelected);
    public int SelectedCount => FilteredRooms.Count(r => r.IsSelected);

    public int TotalRooms => Rooms.Count;
    public int DirtyCt => Rooms.Count(r => r.DirtyStatus == "Dirty");
    public int CleaningCt => Rooms.Count(r => r.DirtyStatus == "Cleaning");
    public int InspectionCt => Rooms.Count(r => r.DirtyStatus == "Inspection");
    public int CleanCt => Rooms.Count(r => r.DirtyStatus == "Clean");
    public int FailCt => Rooms.Count(r => r.DirtyStatus == "FailInspection");
    public int VipCt => Rooms.Count(r => r.IsVip);
    public int RushCt => Rooms.Count(r => r.IsRush);
    public int OooCt => Rooms.Count(r => r.DirtyStatus == "OOO");
    public int OverdueCt => Rooms.Count(r => r.IsOverdue);
    public int CheckoutTodayCt => Rooms.Count(r => r.CheckedOutAt?.Date == DateTime.Today);

    public static string[] StatusFilters { get; } = ["Tất cả", "Dirty", "Cleaning", "Paused", "Inspection", "Clean", "FailInspection", "OOO"];
    public string[] FloorFilters { get; private set; } = [];
    public static string[] AreaFilters { get; } = ["Tất cả", "Khu A", "Khu B", "Khu C"];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE"];
    public string[] AttendantFilters { get; private set; } = [];
    public static string[] ShiftFilters { get; } = ["Tất cả", "Ca sáng", "Ca chiều", "Ca tối"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

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
                    ErrorMessage = "Chưa có ngữ cảnh cơ sở để tải dữ liệu phòng bẩn.";
                    BuildDashboard(); ApplyFilters();
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
                        room.HasMaintenance = matches.Count > 0;
                        room.MaintenanceDesc = matches.FirstOrDefault()?.Description;
                    }
                }

                FloorFilters = Rooms.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Prepend("Tất cả").ToArray();
                AttendantFilters = Rooms.Select(x => x.Attendant!).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).Prepend("Tất cả").ToArray();
                OnPropertyChanged(nameof(FloorFilters));
                OnPropertyChanged(nameof(AttendantFilters));
                BuildDashboard(); ApplyFilters();
                return;
            }

            ErrorMessage = "Chưa có Housekeeping query service; không hiển thị dữ liệu mẫu.";
            BuildDashboard(); ApplyFilters();
            return;

            /* Legacy designer fixture retained only for historical reference.
#pragma warning disable CS0162
            await Task.Delay(100, ct);
            var rng = new Random(55);
            var statuses = new[] { "Dirty", "Dirty", "Dirty", "Dirty", "Cleaning", "Cleaning", "Cleaning", "Inspection", "Inspection", "Clean", "Clean", "Clean", "FailInspection", "OOO" };
            var roomTypes = new[] { "STD", "STD", "STD", "SUP", "SUP", "DLX", "DLX", "JNR", "STE" };
            var floors = new[] { "1", "1", "2", "2", "3", "3", "4", "4", "5", "5", "6", "7", "8" };
            var guests = new[] { "Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", null, null };
            var atts = Attendants;

            for (int i = 1; i <= 120; i++)
            {
                var floor = floors[rng.Next(floors.Length)];
                var roomNum = $"{floor}{rng.Next(1, 9):D2}";
                var status = statuses[rng.Next(statuses.Length)];
                if (status == "OOO" && rng.Next(3) > 0) status = "Dirty";
                var rType = roomTypes[rng.Next(roomTypes.Length)];
                var isVip = rng.Next(8) == 1;
                var isRush = rng.Next(6) == 1;
                var att = status is "Cleaning" or "Paused" or "Inspection" or "Clean" or "FailInspection" ? atts[rng.Next(atts.Length)] : null;
                var co = DateTime.Now.AddHours(-rng.Next(1, 48));
                var hasMinibar = rng.Next(5) == 1;
                var hasMaint = rng.Next(10) == 1;
                var hasLF = rng.Next(12) == 1;
                var ckDone = status switch { "Dirty" => 0, "Cleaning" => rng.Next(3, 10), "Paused" => rng.Next(5, 11), "Inspection" => 13, "Clean" => 13, "FailInspection" => rng.Next(8, 12), _ => 0 };

                Rooms.Add(new DirtyRoomDto
                {
                    RoomNumber = roomNum, RoomType = rType, Floor = floor, Area = rng.Next(3) switch { 0 => "Khu A", 1 => "Khu B", _ => "Khu C" },
                    View = rng.Next(3) switch { 0 => "Biển", 1 => "Thành phố", _ => "Vườn" },
                    DirtyStatus = status, DirtySince = co, IsVip = isVip, IsRush = isRush,
                    GuestName = guests[rng.Next(guests.Length)], ReservationCode = $"RES-{26000 + i}",
                    CompanyName = rng.Next(4) == 1 ? "VinGroup" : null, MarketName = rng.Next(3) switch { 0 => "Nội địa", 1 => "Quốc tế", _ => "Đông Nam Á" },
                    SourceName = rng.Next(3) switch { 0 => "Direct", 1 => "OTA", _ => "Corporate" },
                    ArrivalDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-rng.Next(1, 5))),
                    DepartureDate = DateOnly.FromDateTime(DateTime.Today),
                    CheckedOutAt = co, VipLevel = isVip ? $"VIP {rng.Next(1, 4)}" : null,
                    Balance = rng.Next(3) == 1 ? (decimal)(rng.NextDouble() * 5000000) : 0,
                    FolioCode = $"MF-{27000 + i}",
                    Attendant = att, Inspector = status is "Inspection" or "Clean" or "FailInspection" ? "Nguyễn Giám Sát" : null,
                    AssignedAt = att is not null ? co.AddMinutes(15) : null,
                    CleanStart = status is "Cleaning" or "Paused" or "Inspection" or "Clean" or "FailInspection" ? co.AddMinutes(rng.Next(20, 120)) : null,
                    CleanEnd = status is "Inspection" or "Clean" or "FailInspection" ? co.AddMinutes(rng.Next(60, 240)) : null,
                    CkBed = ckDone >= 1, CkToilet = ckDone >= 2, CkTowel = ckDone >= 3,
                    CkAmenities = ckDone >= 4, CkMinibar = ckDone >= 5, CkBalcony = ckDone >= 6,
                    CkAirCon = ckDone >= 7, CkTv = ckDone >= 8, CkCabinet = ckDone >= 9,
                    CkSlippers = ckDone >= 10, CkElectric = ckDone >= 11,
                    CkLock = ckDone >= 12, CkOther = ckDone >= 13,
                    HasBeforePhoto = status is not "Dirty", HasAfterPhoto = status is "Clean" or "Inspection",
                    HasLostFound = hasLF, HasMinibarIssue = hasMinibar, HasMaintenance = hasMaint,
                    MaintenanceDesc = hasMaint ? (rng.Next(2) == 1 ? "Điều hòa hỏng" : "Đèn phòng tắm cháy") : null,
                    Remark = isVip ? "VIP — ưu tiên số 1" : isRush ? "Rush — khách check-in sớm" : null,
                    HkNote = hasLF ? "Tìm thấy ví khách trong ngăn kéo" : null,
                });
            }

            FloorFilters = Enumerable.Range(1, 8).Select(f => f.ToString()).Prepend("Tất cả").ToArray();
            AttendantFilters = Attendants.Prepend("Tất cả").ToArray();
            OnPropertyChanged(nameof(FloorFilters));
            OnPropertyChanged(nameof(AttendantFilters));

            BuildDashboard(); ApplyFilters();
        }
            */
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static DirtyRoomDto MapTask(HkTaskBoardDto task)
    {
        var rawStatus = task.TaskStatus.ToString();
        var status = rawStatus switch
        {
            "WaitingInspection" => "Inspection",
            "Completed" => "Clean",
            "InProgress" or "Started" => "Cleaning",
            "Accepted" => "Cleaning",
            "Assigned" => "Assigned",
            "Paused" => "Paused",
            "Cancelled" or "Failed" => "FailInspection",
            "Pending" => "Dirty",
            _ => "Dirty"
        };
        var priority = task.Priority.ToString();
        var isVip = task.IsVip || priority.Equals("VIP", StringComparison.OrdinalIgnoreCase);
        var isRush = priority.Equals("Rush", StringComparison.OrdinalIgnoreCase);
        var started = task.StartedAt?.ToLocalTime();
        var room = new DirtyRoomDto
        {
            TaskId = task.TaskId,
            RoomId = task.RoomId,
            AssignedHousekeeperId = task.AssignedHousekeeperId,
            RoomNumber = task.RoomNumber,
            DirtyStatus = status,
            DirtySince = started,
            IsVip = isVip,
            IsRush = isRush,
            TaskPriority = task.Priority,
            Attendant = task.AssignedName ?? task.AssignedTo,
            AssignedAt = null,
            CleanStart = started,
            CleanEnd = null,
            Inspector = task.InspectorName,
            HasBeforePhoto = false,
            HasAfterPhoto = false,
            Remark = task.IsOverdue ? "Quá hạn SLA" : null,
            HkNote = task.TaskType.ToString()
        };

        return ApplyPersistedInspectionData(room, task.InspectionChecklistData);
    }

    private static DirtyRoomDto ApplyPersistedInspectionData(DirtyRoomDto room, string? raw)
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

                    room.PersistedChecklist.Add(new DirtyChecklistItem
                    {
                        Label = label,
                        IconGlyph = ReadString(item, "icon", "iconGlyph") ?? string.Empty,
                        Category = ReadString(item, "category", "group") ?? string.Empty,
                        IsDone = ReadBool(item, "isDone", "done", "completed") ?? false
                    });
                }
            }

            if (root.TryGetProperty("photos", out var photos) && photos.ValueKind == JsonValueKind.Array)
            {
                foreach (var photo in photos.EnumerateArray())
                {
                    var url = ReadString(photo, "url", "path");
                    if (string.IsNullOrWhiteSpace(url))
                        continue;

                    room.PhotoUrls.Add(url);
                    var label = ReadString(photo, "label", "category") ?? string.Empty;
                    room.HasBeforePhoto |= label.Contains("before", StringComparison.OrdinalIgnoreCase)
                        || label.Contains("trước", StringComparison.OrdinalIgnoreCase);
                    room.HasAfterPhoto |= label.Contains("after", StringComparison.OrdinalIgnoreCase)
                        || label.Contains("sau", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch (JsonException)
        {
            room.PersistedChecklist.Clear();
            room.PhotoUrls.Clear();
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

    private async Task EnrichRoomMasterDataAsync(Guid propertyId, IReadOnlyList<DirtyRoomDto> rooms, CancellationToken ct)
    {
        if (_roomMasterDataRepository is null)
            return;

        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var master = room.RoomId is Guid roomId && roomId != Guid.Empty
                ? await _roomMasterDataRepository.GetRoomDtoAsync(propertyId, roomId, ct)
                : await _roomMasterDataRepository.GetRoomByNumberAsync(propertyId, room.RoomNumber, ct);
            if (master is null)
                continue;

            room.RoomId = master.HotelRoomId;
            room.RoomNumber = master.RoomNumber;
            room.RoomType = master.HotelRoomTypeCode;
            room.Floor = master.Floor ?? string.Empty;
            room.Area = master.Building ?? master.Zone ?? string.Empty;
        }
    }

    private async Task EnrichGuestDataAsync(Guid propertyId, IReadOnlyList<DirtyRoomDto> rooms, CancellationToken ct)
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

        foreach (var room in rooms)
        {
            ct.ThrowIfCancellationRequested();
            var stay = stays.Concat(departures).FirstOrDefault(item =>
                (room.RoomId is Guid roomId && item.RoomId == roomId)
                || string.Equals(item.RoomNumber, room.RoomNumber, StringComparison.OrdinalIgnoreCase));
            var reservation = reservations
                .Where(item => item.Status is not (ReservationStatus.Cancelled or ReservationStatus.NoShow)
                    && item.DepartureDate >= DateOnly.FromDateTime(DateTime.Today)
                    && ((room.RoomId is Guid roomId && item.AssignedRoomId == roomId)
                        || item.AssignedRoomId is null && string.Equals(item.ReservationNumber, room.ReservationCode, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(item => item.ArrivalDate)
                .FirstOrDefault();

            room.GuestName = stay?.GuestName ?? reservation?.GuestName;
            room.ReservationCode = stay?.ReservationNumber ?? reservation?.ReservationNumber;
            room.CompanyName = reservation?.CompanyName;
            room.MarketName = reservation?.MarketSegment;
            room.SourceName = stay?.Source ?? reservation?.Source;
            room.ArrivalDate = stay?.ScheduledArrivalDate ?? reservation?.ArrivalDate;
            room.DepartureDate = stay?.ScheduledDepartureDate ?? reservation?.DepartureDate;
            room.CheckedOutAt = stay?.ActualCheckOutAtUtc?.ToLocalTime();
            room.IsVip = room.IsVip || reservation?.IsVip == true;
            room.VipLevel = reservation?.IsVip == true ? "VIP" : room.VipLevel;
        }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Cần dọn", Value = DirtyCt.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Dirty" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "VIP", Value = VipCt.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Rush", Value = RushCt.ToString(), Color = "#EA580C", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Checkout HN", Value = CheckoutTodayCt.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Fail KT", Value = FailCt.ToString(), Color = "#E32F70", BgColor = "#FFF0F5", FilterKey = "Status", FilterValue = "FailInspection" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "OOO", Value = OooCt.ToString(), Color = "#6B7280", BgColor = "#F3F4F6", FilterKey = "Status", FilterValue = "OOO" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Đang dọn", Value = CleaningCt.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", FilterKey = "Status", FilterValue = "Cleaning" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Đã sạch", Value = CleanCt.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Status", FilterValue = "Clean" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Chờ KT", Value = InspectionCt.ToString(), Color = "#EA580C", BgColor = "#FFF7ED", FilterKey = "Status", FilterValue = "Inspection" });
        DashboardKpis.Add(new DirtyDashboardKpi { Label = "Quá hạn", Value = OverdueCt.ToString(), Color = "#DC2626", BgColor = "#FEF2F2" });
    }

    private void BuildHistory(DirtyRoomDto room)
    {
        RoomHistory.Clear();
        if (room.CheckedOutAt.HasValue)
            RoomHistory.Add(new DirtyHistoryEntry { Timestamp = room.CheckedOutAt.Value, Event = "Check Out", Detail = room.GuestName ?? "Khách", Color = "#DC2626" });
        if (room.AssignedAt.HasValue)
            RoomHistory.Add(new DirtyHistoryEntry { Timestamp = room.AssignedAt.Value, Event = "Phân công", Detail = room.Attendant ?? "HK", Color = "#D97706" });
        if (room.CleanStart.HasValue)
            RoomHistory.Add(new DirtyHistoryEntry { Timestamp = room.CleanStart.Value, Event = "Bắt đầu dọn", Detail = $"{room.DurationMin} phút", Color = "#2563EB" });
        if (room.CleanEnd.HasValue)
            RoomHistory.Add(new DirtyHistoryEntry { Timestamp = room.CleanEnd.Value, Event = "Hoàn thành", Detail = "", Color = "#16A34A" });
    }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Rooms.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s)) q = q.Where(r => r.RoomNumber.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.Attendant?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterDirtyStatus) && FilterDirtyStatus != "Tất cả") q = q.Where(r => r.DirtyStatus == FilterDirtyStatus);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterArea) && FilterArea != "Tất cả") q = q.Where(r => r.Area == FilterArea);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterAttendant) && FilterAttendant != "Tất cả") q = q.Where(r => r.Attendant == FilterAttendant);
        if (FilterVip) q = q.Where(r => r.IsVip);
        if (FilterRush) q = q.Where(r => r.IsRush);
        if (FilterOverdue) q = q.Where(r => r.IsOverdue);
        if (FilterInspection) q = q.Where(r => r.DirtyStatus == "Inspection");
        if (FilterOoo) q = q.Where(r => r.DirtyStatus == "OOO");
        if (FilterMine) q = q.Where(r => r.Attendant == "Nguyễn Lan");

        var list = q.OrderByDescending(r => r.PriorityScore).ThenBy(r => int.Parse(r.RoomNumber)).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredRooms.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredRooms.Add(r);
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
        RebuildPageButtons();
        OnPropertyChanged(nameof(AllSelected)); OnPropertyChanged(nameof(SelectedCount));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterDirtyStatus = null; FilterFloor = null; FilterArea = null;
        FilterRoomType = null; FilterAttendant = null;
        FilterVip = false; FilterRush = false; FilterOverdue = false; FilterInspection = false; FilterOoo = false; FilterMine = false;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterDirtyStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterAreaChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterAttendantChanged(string? value) => ApplyFilters();
    partial void OnFilterVipChanged(bool value) => ApplyFilters();
    partial void OnFilterRushChanged(bool value) => ApplyFilters();
    partial void OnFilterOverdueChanged(bool value) => ApplyFilters();
    partial void OnFilterInspectionChanged(bool value) => ApplyFilters();
    partial void OnFilterOooChanged(bool value) => ApplyFilters();
    partial void OnFilterMineChanged(bool value) => ApplyFilters();

    [RelayCommand] private void ApplyKpiFilter(DirtyDashboardKpi? kpi)
    {
        if (kpi is null) return;
        ClearFilters();
        if (kpi.FilterKey == "Status") FilterDirtyStatus = kpi.FilterValue;
        else
        {
            switch (kpi.Label)
            {
                case "VIP": FilterVip = true; break;
                case "Rush": FilterRush = true; break;
                case "OOO": FilterOoo = true; break;
                case "Chờ KT": FilterInspection = true; break;
                case "Quá hạn": FilterOverdue = true; break;
            }
        }
        ApplyFilters();
    }

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
                ? new DirtyPaginationButton("…", null, false, true)
                : new DirtyPaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
    }

    // ═══ SELECTION ═══
    [RelayCommand] private void SelectRoom(DirtyRoomDto? r) { SelectedRoom = r; IsDetailOpen = r is not null; SelectedDetailTab = 0; if (r is not null) BuildHistory(r); }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedRoom = null; RoomHistory.Clear(); }
    partial void OnSelectedRoomChanged(DirtyRoomDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    // ═══ MULTI-SELECT ═══
    [RelayCommand] private void SelectAll()
    {
        foreach (var r in FilteredRooms) r.IsSelected = true;
        OnPropertyChanged(nameof(AllSelected)); OnPropertyChanged(nameof(SelectedCount));
    }
    [RelayCommand] private void DeselectAll()
    {
        foreach (var r in FilteredRooms) r.IsSelected = false;
        OnPropertyChanged(nameof(AllSelected)); OnPropertyChanged(nameof(SelectedCount));
    }
    [RelayCommand] private void OnSelectionChanged() { OnPropertyChanged(nameof(AllSelected)); OnPropertyChanged(nameof(SelectedCount)); }

    // ═══ WORKFLOW ═══
    [RelayCommand] private async Task AssignRoom()
    {
        if (SelectedRoom is null || SelectedRoom.DirtyStatus is not "Dirty") return;
        var assigneeId = SelectedRoom.AssignedHousekeeperId ?? _currentUserService?.CurrentUser?.Id;
        if (assigneeId is null)
        {
            ErrorMessage = "Chưa đăng nhập tài khoản nhân sự để phân công; không tạo phân công giả.";
            return;
        }
        if (!await TryPersistTaskActionAsync(HkTaskAction.Assign, assigneeId)) return;
        SelectedRoom.AssignedHousekeeperId = assigneeId;
        SelectedRoom.Attendant = _currentUserService?.CurrentUser?.DisplayName ?? SelectedRoom.Attendant;
        SelectedRoom.DirtyStatus = "Assigned"; SelectedRoom.AssignedAt = DateTime.Now;
        BuildDashboard(); ApplyFilters(); InfoMessage = $"Đã lưu phân công phòng {SelectedRoom.RoomNumber}.";
    }
    [RelayCommand] private async Task StartCleaning()
    {
        if (SelectedRoom is null || SelectedRoom.DirtyStatus is not "Assigned") return;
        if (SelectedRoom.DirtyStatus is "Dirty" or "FailInspection")
        {
            ErrorMessage = "Công việc chưa ở trạng thái Assigned; hãy phân công bằng dữ liệu nhân sự thật trước.";
            return;
        }
        if (!await TryPersistTaskActionAsync(HkTaskAction.Accept)) return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.StartCleaning)) return;
        SelectedRoom.DirtyStatus = "Cleaning"; SelectedRoom.CleanStart = DateTime.Now;
        if (string.IsNullOrEmpty(SelectedRoom.Attendant)) SelectedRoom.Attendant = "Nguyễn Lan";
        BuildDashboard(); ApplyFilters(); InfoMessage = $"Bắt đầu dọn phòng {SelectedRoom.RoomNumber}.";
    }
    [RelayCommand] private async Task PauseCleaning()
    {
        if (SelectedRoom?.DirtyStatus is not "Cleaning") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.PauseCleaning, notes: "Tạm dừng từ màn hình phòng bẩn")) return;
        SelectedRoom.DirtyStatus = "Paused";
        BuildDashboard(); ApplyFilters();
        InfoMessage = $"Đã tạm dừng dọn phòng {SelectedRoom.RoomNumber} và lưu trạng thái thật.";
    }
    [RelayCommand] private async Task ResumeCleaning()
    {
        if (SelectedRoom?.DirtyStatus is not "Paused") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.ResumeCleaning)) return;
        SelectedRoom.DirtyStatus = "Cleaning";
        BuildDashboard(); ApplyFilters();
        InfoMessage = $"Đã tiếp tục dọn phòng {SelectedRoom.RoomNumber} và lưu trạng thái thật.";
    }
    [RelayCommand] private async Task FinishCleaning()
    {
        if (SelectedRoom?.DirtyStatus is not ("Cleaning" or "Paused")) return;
        if (!SelectedRoom.IsChecklistComplete) { MessageBox.Show("Checklist chưa hoàn tất!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (SelectedRoom.HasMaintenance) { MessageBox.Show("Có Maintenance chưa xử lý!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (SelectedRoom.HasMinibarIssue) { MessageBox.Show("Minibar chưa xác nhận!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (!await TryPersistTaskActionAsync(HkTaskAction.CompleteCleaning)) return;
        SelectedRoom.DirtyStatus = "Inspection"; SelectedRoom.CleanEnd = DateTime.Now;
        BuildDashboard(); BuildHistory(SelectedRoom); ApplyFilters(); InfoMessage = $"Hoàn thành dọn phòng {SelectedRoom.RoomNumber}. Chờ kiểm tra.";
    }
    [RelayCommand] private async Task ApproveInspection()
    {
        if (SelectedRoom?.DirtyStatus is not "Inspection") return;
        if (!await TryPersistTaskActionAsync(HkTaskAction.ApproveInspection, notes: "Kiểm tra đạt")) return;
        SelectedRoom.DirtyStatus = "Clean"; SelectedRoom.HasAfterPhoto = true;
        BuildDashboard(); BuildHistory(SelectedRoom); ApplyFilters(); InfoMessage = $"Phòng {SelectedRoom.RoomNumber} đã sạch — Ready!";
    }
    [RelayCommand] private async Task RejectInspection()
    {
        if (SelectedRoom?.DirtyStatus is not "Inspection") return;
        var r = MessageBox.Show($"Fail kiểm tra phòng {SelectedRoom.RoomNumber}?\n\nLý do: Cần làm lại WC và kiểm tra amenities", "Fail Inspection", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (r == MessageBoxResult.OK)
        {
            if (!await TryPersistTaskActionAsync(HkTaskAction.RejectInspection, notes: "Cần làm lại WC và kiểm tra amenities")) return;
            SelectedRoom.DirtyStatus = "FailInspection"; SelectedRoom.CkToilet = false; SelectedRoom.CkAmenities = false; BuildDashboard(); BuildHistory(SelectedRoom); ApplyFilters(); InfoMessage = $"Phòng {SelectedRoom.RoomNumber} cần làm lại.";
        }
    }
    [RelayCommand]
    private async Task ChangeAttendant()
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
        SelectedRoom.Attendant = selected.DisplayName;
        BuildDashboard(); BuildHistory(SelectedRoom); ApplyFilters();
        InfoMessage = $"Đã đổi nhân viên dọn phòng {SelectedRoom.RoomNumber} sang {selected.DisplayName}.";
    }

    private async Task<bool> TryPersistTaskActionAsync(HkTaskAction action, Guid? assignedHousekeeperId = null, string? notes = null)
    {
        if (SelectedRoom is null)
        {
            ErrorMessage = "Hãy chọn một phòng trước khi cập nhật.";
            return false;
        }

        return await TryPersistTaskActionAsync(SelectedRoom, action, assignedHousekeeperId, notes);
    }

    private async Task<bool> TryPersistTaskActionAsync(DirtyRoomDto room, HkTaskAction action, Guid? assignedHousekeeperId = null, string? notes = null)
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

    // ═══ LINKS ═══
    [RelayCommand]
    private void OpenLostFound()
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || string.IsNullOrWhiteSpace(SelectedRoom.RoomNumber))
        {
            ErrorMessage = "Bản ghi Housekeeping chưa có số phòng thật để mở Lost & Found.";
            return;
        }

        var existing = global::System.Windows.Application.Current.Windows.OfType<Views.LostFoundWindow>().FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        new Views.LostFoundWindow(SelectedRoom.RoomNumber)
        {
            Owner = global::System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
    }

    [RelayCommand]
    private void OpenMinibar()
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || SelectedRoom.RoomId is not Guid roomId || roomId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi Housekeeping chưa có RoomId thật để mở Minibar.";
            return;
        }

        var existing = global::System.Windows.Application.Current.Windows.OfType<Views.MinibarWindow>().FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        new Views.MinibarWindow(roomId)
        {
            Owner = global::System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
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
            if (orders.Count == 0)
            {
                InfoMessage = $"Phòng {SelectedRoom.RoomNumber} hiện không có Work Order đang mở.";
                return;
            }

            InfoMessage = string.Join("\n", orders.Select(x => $"{x.OrderNumber} · {x.Status} · {x.Description}"));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được Work Order phòng {SelectedRoom.RoomNumber}: {ex.Message}";
        }
    }
    [RelayCommand]
    private async Task OpenFolio(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom?.RoomId is not { } roomId || roomId == Guid.Empty || _stayRepository is null || _executionContext?.Current is not { } context)
        {
            ErrorMessage = "Bản ghi Housekeeping chưa có RoomId/Stay query thật; không mở Folio giả.";
            return;
        }

        try
        {
            var stay = (await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, ct))
                .FirstOrDefault(item => item.RoomId == roomId || string.Equals(item.RoomNumber, SelectedRoom.RoomNumber, StringComparison.OrdinalIgnoreCase));
            if (stay is null)
            {
                ErrorMessage = $"Không tìm thấy Stay thật đang ở phòng {SelectedRoom.RoomNumber}; không có Folio để mở.";
                return;
            }

            var existing = System.Windows.Application.Current.Windows
                .OfType<Views.FolioWorkspaceWindow>()
                .FirstOrDefault(window => window.IsLoaded);
            if (existing is not null) { existing.Activate(); existing.Focus(); return; }
            new Views.FolioWorkspaceWindow(stay.HotelStayId)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.Show();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể tải Folio thật theo phòng: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task OpenReservation(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom?.RoomId is not { } roomId || roomId == Guid.Empty || _reservationRepository is null || _executionContext?.Current is not { } context)
        {
            ErrorMessage = "Bản ghi Housekeeping chưa có RoomId/Reservation query thật; không mở Reservation giả.";
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
            if (workspace is null) { ErrorMessage = "Không thể khởi tạo Reservation workspace."; return; }
            var existing = System.Windows.Application.Current.Windows
                .OfType<Views.ReservationStatusWindow>()
                .FirstOrDefault(window => window.IsLoaded);
            if (existing is not null) { existing.Activate(); existing.Focus(); return; }
            workspace.Mode = WorkspaceMode.Status;
            workspace.SelectedReservation = reservation;
            new Views.ReservationStatusWindow(workspace)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.Show();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể tải Reservation thật theo phòng: {ex.Message}"; }
    }
    [RelayCommand]
    private Task ToggleVip() => TogglePriorityAsync(
        SelectedRoom?.TaskPriority == RoomPriority.VIP ? RoomPriority.Normal : RoomPriority.VIP,
        "VIP");

    [RelayCommand]
    private Task ToggleRush() => TogglePriorityAsync(
        SelectedRoom?.TaskPriority == RoomPriority.Rush ? RoomPriority.Normal : RoomPriority.Rush,
        "Rush");

    private async Task TogglePriorityAsync(RoomPriority priority, string label)
    {
        if (SelectedRoom is null)
            return;
        if (SelectedRoom.TaskId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi đang chọn chưa có HousekeepingTaskId thật; không thể cập nhật ưu tiên.";
            return;
        }
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để cập nhật ưu tiên phòng.";
            return;
        }

        var result = await _workflowService.ExecuteTaskAsync(
            new HkTaskActionRequest(
                propertyId.Value,
                SelectedRoom.TaskId,
                HkTaskAction.SetPriority,
                Notes: $"Cập nhật ưu tiên: {label}",
                Priority: priority),
            CancellationToken.None);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể cập nhật ưu tiên Housekeeping.";
            return;
        }

        SelectedRoom.TaskPriority = priority;
        SelectedRoom.IsVip = priority == RoomPriority.VIP;
        SelectedRoom.IsRush = priority == RoomPriority.Rush;
        BuildDashboard(); ApplyFilters();
        InfoMessage = priority == RoomPriority.Normal
            ? $"Đã bỏ ưu tiên đặc biệt cho phòng {SelectedRoom.RoomNumber}."
            : $"Đã đặt ưu tiên {label} cho phòng {SelectedRoom.RoomNumber}.";
    }
    [RelayCommand]
    private async Task ToggleChecklistItem(string? item)
    {
        if (SelectedRoom is null || string.IsNullOrWhiteSpace(item))
            return;

        var checklistItem = SelectedRoom.PersistedChecklist.FirstOrDefault(
            candidate => string.Equals(candidate.Label, item, StringComparison.OrdinalIgnoreCase));
        if (checklistItem is null)
        {
            ErrorMessage = $"Không tìm thấy mục checklist thật: {item}.";
            return;
        }

        var previous = checklistItem.IsDone;
        checklistItem.IsDone = !previous;
        SelectedRoom.NotifyChecklistChanged();
        if (!await TryPersistInspectionEvidenceAsync(SelectedRoom, $"Cập nhật checklist: {checklistItem.Label}"))
        {
            checklistItem.IsDone = previous;
            SelectedRoom.NotifyChecklistChanged();
            return;
        }

        BuildDashboard(); BuildHistory(SelectedRoom); ApplyFilters();
        InfoMessage = $"Đã lưu checklist phòng {SelectedRoom.RoomNumber}: {checklistItem.Label}.";
    }

    private async Task<bool> TryPersistInspectionEvidenceAsync(DirtyRoomDto room, string notes)
    {
        if (_workflowService is null)
        {
            ErrorMessage = "Housekeeping workflow service chưa được đăng ký.";
            return false;
        }
        if (room.TaskId == Guid.Empty)
        {
            ErrorMessage = "Bản ghi đang chọn chưa có HousekeepingTaskId thật; không thể cập nhật checklist.";
            return false;
        }

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để cập nhật checklist phòng.";
            return false;
        }

        var result = await _workflowService.ExecuteTaskAsync(
            new HkTaskActionRequest(
                propertyId.Value,
                room.TaskId,
                HkTaskAction.SaveInspectionEvidence,
                Notes: notes,
                InspectorId: _currentUserService?.CurrentUser?.Id,
                InspectionChecklistData: SerializeInspectionData(room, "Checklist", "Checklist phòng bẩn")),
            CancellationToken.None);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể lưu checklist phòng.";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }

    [RelayCommand]
    private Task UploadBeforePhoto() => UploadPhotoAsync("Before", "Trước khi dọn phòng");

    [RelayCommand]
    private Task UploadAfterPhoto() => UploadPhotoAsync("After", "Sau khi dọn phòng");

    private async Task UploadPhotoAsync(string label, string category)
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
            Title = $"Chọn ảnh {category.ToLowerInvariant()}",
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
            if (label == "Before") SelectedRoom.HasBeforePhoto = true;
            if (label == "After") SelectedRoom.HasAfterPhoto = true;

            var result = await _workflowService.ExecuteTaskAsync(
                new HkTaskActionRequest(
                    propertyId.Value,
                    SelectedRoom.TaskId,
                    HkTaskAction.SaveInspectionEvidence,
                    Notes: $"Lưu ảnh {category}",
                    InspectorId: _currentUserService?.CurrentUser?.Id,
                    InspectionChecklistData: SerializeInspectionData(SelectedRoom, label, category)),
                CancellationToken.None);
            if (result.IsFailure)
            {
                SelectedRoom.PhotoUrls.Remove(destination);
                File.Delete(destination);
                ErrorMessage = result.Error?.Message ?? "Không thể lưu bằng chứng ảnh phòng.";
                return;
            }

            ErrorMessage = string.Empty;
            InfoMessage = $"Đã lưu ảnh {category.ToLowerInvariant()}.";
        }
        catch (Exception ex)
        {
            SelectedRoom.PhotoUrls.Remove(destination);
            if (File.Exists(destination))
                File.Delete(destination);
            ErrorMessage = $"Không thể lưu ảnh phòng: {ex.Message}";
        }
    }

    private static string SerializeInspectionData(DirtyRoomDto room, string currentLabel, string currentCategory)
        => JsonSerializer.Serialize(new
        {
            checklistDone = room.ChecklistDone,
            checklistTotal = room.ChecklistTotal,
            items = room.PersistedChecklist.Select(item => new
            {
                label = item.Label,
                icon = item.IconGlyph,
                category = item.Category,
                isDone = item.IsDone
            }),
            photos = room.PhotoUrls.Select((url, index) => new
            {
                url,
                label = index == room.PhotoUrls.Count - 1 ? currentLabel : "Housekeeping",
                category = index == room.PhotoUrls.Count - 1 ? currentCategory : "Housekeeping"
            })
        });

    // ═══ BATCH ═══
    private List<DirtyRoomDto> SelectedPersistableRooms() =>
        FilteredRooms.Where(r => r.IsSelected && r.TaskId != Guid.Empty).ToList();

    [RelayCommand]
    private async Task BatchAssign()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc thật.";
            return;
        }

        var currentUser = _currentUserService?.CurrentUser;
        var assigneeId = currentUser?.Id;
        if (currentUser is null || assigneeId is null)
        {
            ErrorMessage = "Chưa đăng nhập tài khoản nhân sự để phân công; không tạo phân công giả.";
            return;
        }

        var changed = 0;
        foreach (var room in selected.Where(r => r.DirtyStatus is "Dirty" or "FailInspection"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.Assign, assigneeId)) continue;
            room.AssignedHousekeeperId = assigneeId;
            room.Attendant = currentUser.DisplayName;
            room.AssignedAt = DateTime.Now;
            room.DirtyStatus = "Assigned";
            changed++;
        }

        BuildDashboard();
        ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng ở trạng thái cần phân công hoặc command bị từ chối."
            : $"Đã phân công thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task BatchStart()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected.Where(r => r.DirtyStatus == "Assigned"))
        {
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.Accept) ||
                !await TryPersistTaskActionAsync(room, HkTaskAction.StartCleaning)) continue;
            room.DirtyStatus = "Cleaning";
            room.CleanStart = DateTime.Now;
            changed++;
        }

        BuildDashboard();
        ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đã phân công để bắt đầu dọn hoặc command bị từ chối."
            : $"Đã bắt đầu dọn thật {changed}/{selected.Count} phòng.";
    }

    [RelayCommand]
    private async Task BatchComplete()
    {
        var selected = SelectedPersistableRooms();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng có công việc thật.";
            return;
        }

        var changed = 0;
        foreach (var room in selected.Where(r => r.DirtyStatus == "Cleaning"))
        {
            if (!room.IsChecklistComplete || room.HasMaintenance || room.HasMinibarIssue) continue;
            if (!await TryPersistTaskActionAsync(room, HkTaskAction.CompleteCleaning)) continue;
            room.DirtyStatus = "Inspection";
            room.CleanEnd = DateTime.Now;
            changed++;
        }

        BuildDashboard();
        ApplyFilters();
        InfoMessage = changed == 0
            ? "Không có phòng đủ điều kiện hoàn thành hoặc command bị từ chối."
            : $"Đã hoàn thành dọn thật {changed}/{selected.Count} phòng và chuyển chờ kiểm tra.";
    }

    [RelayCommand]
    private void BatchInspect()
    {
        ErrorMessage = "Không tự động Pass kiểm tra hàng loạt; cần kết quả và ghi chú riêng cho từng phòng.";
    }
    [RelayCommand]
    private async Task ExportExcel()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có phòng bẩn có công việc thật trong danh sách hiện tại để xuất.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"DirtyRooms-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;

        var csv = new StringBuilder()
            .AppendLine("TaskId,RoomNumber,RoomType,Floor,Area,DirtyStatus,Attendant,Inspector,GuestName,IsVip,IsRush,ChecklistDone,ChecklistTotal,HasMaintenance,HasMinibarIssue,HasLostFound,DirtySince");
        foreach (var room in rows)
        {
            csv.AppendLine(string.Join(",",
                Csv(room.TaskId.ToString("D")), Csv(room.RoomNumber), Csv(room.RoomType), Csv(room.Floor), Csv(room.Area),
                Csv(room.DirtyStatus), Csv(room.Attendant ?? ""), Csv(room.Inspector ?? ""), Csv(room.GuestName ?? ""),
                room.IsVip, room.IsRush, room.ChecklistDone, room.ChecklistTotal, room.HasMaintenance,
                room.HasMinibarIssue, room.HasLostFound, Csv(room.DirtySince?.ToString("O") ?? "")));
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ErrorMessage = string.Empty;
            InfoMessage = $"Đã xuất {rows.Count:N0} phòng bẩn từ dữ liệu Housekeeping thật.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể xuất danh sách phòng bẩn: " + ex.Message;
        }
    }

    [RelayCommand]
    private void PrintList()
    {
        var rows = FilteredRooms.Where(room => room.TaskId != Guid.Empty).ToList();
        if (rows.Count == 0)
        {
            ErrorMessage = "Không có phòng bẩn có công việc thật trong danh sách hiện tại để in.";
            return;
        }

        var content = new StringBuilder()
            .AppendLine("DANH SÁCH PHÒNG BẨN")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine(new string('-', 115));
        foreach (var room in rows)
        {
            content.AppendLine($"Phòng {room.RoomNumber} | {room.RoomTypeName} | {room.DirtyLabel} | " +
                $"NV: {room.Attendant ?? "—"} | Khách: {room.GuestName ?? "—"} | " +
                $"Checklist: {room.ChecklistDone}/{room.ChecklistTotal} | " +
                $"VIP: {(room.IsVip ? "Có" : "Không")} | Rush: {(room.IsRush ? "Có" : "Không")} | " +
                $"Maintenance: {(room.HasMaintenance ? "Có" : "Không")} | Minibar: {(room.HasMinibarIssue ? "Có" : "Không")}");
        }

        new PrintPreviewDialog("Danh sách phòng bẩn", content.ToString()).ShowDialog();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
