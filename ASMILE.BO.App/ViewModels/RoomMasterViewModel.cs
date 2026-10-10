using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class RoomMasterDto
{
    public Guid HotelRoomId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public bool IsSelected { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string Building { get; set; } = "";
    public string Floor { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomTypeName => RoomType switch
    {
        "STD" => "Standard", "SUP" => "Superior", "DLX" => "Deluxe",
        "JNR" => "Junior Suite", "STE" => "Suite", "PRE" => "Presidential",
        "FAM" => "Family", "CON" => "Connecting", _ => RoomType
    };
    public string RoomClass { get; set; } = "";
    public int SquareMeters { get; set; }
    public string? View { get; set; }
    public string BedType { get; set; } = "";
    public int MaxPax { get; set; }
    public bool IsSmoking { get; set; }
    public bool IsConnecting { get; set; }
    public string? Telephone { get; set; }
    public string? Extension { get; set; }

    // Status
    public string FoStatus { get; set; } = "";
    public string FoStatusLabel => FoStatus switch
    {
        "VC" => "Vacant Clean", "VD" => "Vacant Dirty", "OC" => "Occupied Clean",
        "OD" => "Occupied Dirty", "OOO" => "Out Of Order", "OOS" => "Out Of Service",
        "PU" => "Pickup", _ => FoStatus
    };
    public string FoStatusColor => FoStatus switch
    {
        "VC" => "#16A34A", "VD" => "#CA8A04", "OC" => "#2563EB",
        "OD" => "#DC2626", "OOO" => "#6B7280", "OOS" => "#EA580C", _ => "#64748B"
    };
    public string FoStatusBg => FoStatus switch
    {
        "VC" => "#DCFCE7", "VD" => "#FEF9C3", "OC" => "#DBEAFE",
        "OD" => "#FEE2E2", "OOO" => "#E5E7EB", "OOS" => "#FFEDD5", _ => "#FFFFFF"
    };
    public string HkStatus { get; set; } = "";
    public string HkStatusLabel => HkStatus switch { "Clean" => "Sạch", "Dirty" => "Bẩn", "Cleaning" => "Đang dọn", "Inspected" => "Đã kiểm tra", _ => string.IsNullOrWhiteSpace(HkStatus) ? "Chưa có dữ liệu" : HkStatus };
    public string RoomCondition { get; set; } = "";
    public string? Attendant { get; set; }
    public string? Remark { get; set; }

    // Guest (if occupied)
    public string? GuestName { get; set; }
    public string? ReservationCode { get; set; }
    public string? CompanyName { get; set; }
    public string? AgentName { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public int StayDays => ArrivalDate.HasValue && DepartureDate.HasValue ? (DepartureDate.Value.DayNumber - ArrivalDate.Value.DayNumber) : 0;
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? VipLevel { get; set; }
    public string? Nationality { get; set; }
    public bool IsOccupied => FoStatus is "OC" or "OD";

    // HK details
    public DateTime? LastCleaned { get; set; }
    public DateTime? DirtySince { get; set; }
    public string? Inspector { get; set; }
    public int CleaningTimeMinutes { get; set; }

    // Facilities (bit flags approach, but using bools for binding simplicity)
    public bool HasTv { get; set; }
    public bool HasSmartTv { get; set; }
    public bool HasMinibar { get; set; }
    public bool HasSafe { get; set; }
    public bool HasBathtub { get; set; }
    public bool HasShower { get; set; }
    public bool HasHairDryer { get; set; }
    public bool HasWifi { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasCoffeeMachine { get; set; }
    public bool HasIron { get; set; }
    public bool HasAirCon { get; set; }
    public bool HasDesk { get; set; }
    public int FacilityCount => (HasTv ? 1 : 0) + (HasSmartTv ? 1 : 0) + (HasMinibar ? 1 : 0) + (HasSafe ? 1 : 0) + (HasBathtub ? 1 : 0) + (HasShower ? 1 : 0) + (HasHairDryer ? 1 : 0) + (HasWifi ? 1 : 0) + (HasBalcony ? 1 : 0) + (HasKitchen ? 1 : 0) + (HasCoffeeMachine ? 1 : 0) + (HasIron ? 1 : 0) + (HasAirCon ? 1 : 0) + (HasDesk ? 1 : 0);

    // Badge flags
    public bool IsDnd { get; set; }
    public bool IsMur { get; set; }
    public bool HasLostFound { get; set; }

    // Shared computed
    public string CardColor => FoStatus switch
    {
        "VC" => "#DCFCE7", "VD" => "#FEF9C3", "OC" => "#DBEAFE",
        "OD" => "#FEE2E2", "OOO" => "#E5E7EB", "OOS" => "#FFEDD5", _ => "#FFFFFF"
    };
    public string CardBorder => FoStatus switch
    {
        "VC" => "#86EFAC", "VD" => "#FDE047", "OC" => "#93C5FD",
        "OD" => "#FCA5A5", "OOO" => "#9CA3AF", "OOS" => "#FDBA74", _ => "#E2E8F0"
    };
}

public sealed class RoomFacilityItem
{
    public string Name { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public bool IsAvailable { get; set; }
    public string Category { get; set; } = "";
}

public sealed class RoomMasterKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string IconGlyph { get; set; } = "";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class RoomHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class RoomDistributionItem
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public string Color { get; set; } = "#64748B";
    public double Percentage { get; set; }
}

public sealed class RoomTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public int Count { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }
    public ObservableCollection<RoomTreeItem> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0;
}

public sealed class RoomPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public RoomPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

// ═══ VIEW MODEL ═══

public sealed partial class RoomMasterViewModel : ViewModelBase
{
    private readonly IHotelRoomMasterDataRepository? _roomRepository;
    private readonly IHousekeepingBoardService? _housekeepingBoardService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelRoomCommandHandler? _createRoomHandler;
    private readonly UpdateHotelRoomCommandHandler? _updateRoomHandler;
    private readonly HotelRoomMasterDataQueryHandlers? _hotelQueries;
    private readonly ChangeHotelRoomOperationalStatusCommandHandler? _changeStatusHandler;
    private readonly IAuditService? _auditService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IHousekeepingWorkflowService? _housekeepingWorkflowService;
    private readonly bool _usingRealData;
    private CancellationTokenSource? _reloadCts;
    private CancellationTokenSource? _historyCts;

    public RoomMasterViewModel(
        IHotelRoomMasterDataRepository? roomRepository = null,
        IFrontOfficeExecutionContextAccessor? contextAccessor = null,
        CreateHotelRoomCommandHandler? createRoomHandler = null,
        UpdateHotelRoomCommandHandler? updateRoomHandler = null,
        HotelRoomMasterDataQueryHandlers? hotelQueries = null,
        ChangeHotelRoomOperationalStatusCommandHandler? changeStatusHandler = null,
        IAuditService? auditService = null,
        IHotelStayRepository? stayRepository = null,
        IReservationRepository? reservationRepository = null,
        IServiceProvider? serviceProvider = null,
        IHousekeepingWorkflowService? housekeepingWorkflowService = null,
        IHousekeepingBoardService? housekeepingBoardService = null)
    {
        _roomRepository = roomRepository;
        _contextAccessor = contextAccessor;
        _createRoomHandler = createRoomHandler;
        _updateRoomHandler = updateRoomHandler;
        _hotelQueries = hotelQueries;
        _changeStatusHandler = changeStatusHandler;
        _auditService = auditService;
        _stayRepository = stayRepository;
        _reservationRepository = reservationRepository;
        _serviceProvider = serviceProvider;
        _housekeepingWorkflowService = housekeepingWorkflowService;
        _housekeepingBoardService = housekeepingBoardService;
        _usingRealData = roomRepository is not null;
    }

    // ── Collections ──
    public ObservableCollection<RoomMasterDto> Rooms { get; } = new();
    public ObservableCollection<RoomMasterDto> FilteredRooms { get; } = new();
    public ObservableCollection<RoomMasterKpi> DashboardKpis { get; } = new();
    public ObservableCollection<RoomTreeItem> TreeItems { get; } = new();
    public ObservableCollection<RoomHistoryEntry> RoomHistory { get; } = new();
    public ObservableCollection<RoomFacilityItem> RoomFacilities { get; } = new();
    public ObservableCollection<RoomPaginationButton> PageButtons { get; } = new();
    public ObservableCollection<RoomDistributionItem> TypeDistribution { get; } = new();
    public ObservableCollection<RoomDistributionItem> StatusDistribution { get; } = new();
    public ObservableCollection<RoomDistributionItem> ClassDistribution { get; } = new();

    // ── Selection ──
    [ObservableProperty] private RoomMasterDto? _selectedRoom;
    [ObservableProperty] private RoomTreeItem? _selectedTreeItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private int _viewMode; // 0=Table, 1=Card, 2=Floor

    // ── State ──
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private bool _isBatchMode;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // ── Filter ──
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterFoStatus;
    [ObservableProperty] private string? _filterHkStatus;
    [ObservableProperty] private string? _filterRoomType;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterRoomClass;
    [ObservableProperty] private string? _filterBedType;
    [ObservableProperty] private string? _filterView;
    [ObservableProperty] private string? _filterCondition;
    [ObservableProperty] private bool _filterSmokingOnly;
    [ObservableProperty] private bool _filterConnectingOnly;
    [ObservableProperty] private bool _filterOccupiedOnly;
    [ObservableProperty] private bool _filterVacantOnly;
    [ObservableProperty] private bool _filterOooOos;
    [ObservableProperty] private bool _filterVipOnly;

    // ── Pagination ──
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} phòng";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedRoom is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool IsTableView => ViewMode == 0;
    public bool IsCardView => ViewMode == 1;
    public bool IsFloorView => ViewMode == 2;

    // ── Computed totals ──
    public int TotalRooms => Rooms.Count;
    public int VacantRooms => Rooms.Count(r => r.FoStatus is "VC" or "VD");
    public int OccupiedRooms => Rooms.Count(r => r.IsOccupied);
    public int VacantCleanCount => Rooms.Count(r => r.FoStatus == "VC");
    public int VacantDirtyCount => Rooms.Count(r => r.FoStatus == "VD");
    public int OccupiedCleanCount => Rooms.Count(r => r.FoStatus == "OC");
    public int OccupiedDirtyCount => Rooms.Count(r => r.FoStatus == "OD");
    public int OooCount => Rooms.Count(r => r.FoStatus == "OOO");
    public int OosCount => Rooms.Count(r => r.FoStatus == "OOS");
    public int SuiteCount => Rooms.Count(r => r.RoomType is "STE" or "JNR" or "PRE");
    public int SmokingCount => Rooms.Count(r => r.IsSmoking);
    public int ConnectingCount => Rooms.Count(r => r.IsConnecting);
    public double OccupancyPct => TotalRooms > 0 ? Math.Round((double)OccupiedRooms / TotalRooms * 100, 1) : 0;

    // ── Static options ──
    public static string[] FoStatusFilters { get; } = ["Tất cả", "VC", "VD", "OC", "OD", "OOO", "OOS", "PU"];
    public static string[] HkStatusFilters { get; } = ["Tất cả", "Clean", "Dirty", "Cleaning", "Inspected", "OutOfOrder", "OutOfService"];
    public static string[] RoomTypeFilters { get; } = ["Tất cả", "STD", "SUP", "DLX", "JNR", "STE", "PRE", "FAM", "CON"];
    public static string[] FloorFilters { get; private set; } = [];
    public static string[] BuildingFilters { get; } = ["Tất cả", "A", "B", "C"];
    public static string[] RoomClassFilters { get; } = ["Tất cả", "Standard", "Superior", "Deluxe", "Suite"];
    public static string[] BedTypeFilters { get; } = ["Tất cả", "King", "Twin", "Queen", "Double"];
    public static string[] ViewFilters { get; } = ["Tất cả", "Biển", "Thành phố", "Vườn", "Hồ bơi"];
    public static string[] ConditionFilters { get; } = ["Tất cả", "Normal", "Maintenance", "DeepClean", "Renovation"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            if (_usingRealData)
            {
                await LoadFromRepositoryAsync(ct);
                return;
            }

            Rooms.Clear();
            ErrorMessage = "Chưa có Room Master repository hoặc property context; không hiển thị dữ liệu mẫu.";
            BuildDashboard(); BuildTreeItems(); BuildDistributions(); ApplyFilters();
            return;

        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task LoadFromRepositoryAsync(CancellationToken ct)
    {
        Rooms.Clear();
        var propertyId = _contextAccessor?.Current?.Property.PropertyId.Value;
        if (propertyId is null || propertyId == Guid.Empty)
        {
            TotalFilteredCount = 0;
            FilteredRooms.Clear();
            FloorFilters = ["Tất cả"];
            BuildDashboard();
            BuildTreeItems();
            BuildDistributions();
            RebuildPageButtons();
            OnPropertyChanged(nameof(PageDisplay));
            OnPropertyChanged(nameof(HasPrevPage));
            OnPropertyChanged(nameof(HasNextPage));
            OnPropertyChanged(nameof(TotalPages));
            ErrorMessage = "Property context is not available.";
            return;
        }

        var operationalStatus = FilterFoStatus switch
        {
            "OOO" => HotelRoomOperationalStatus.OutOfOrder,
            "OOS" => HotelRoomOperationalStatus.OutOfService,
            _ => (HotelRoomOperationalStatus?)null
        };

        Guid? roomTypeId = null;
        if (!string.IsNullOrWhiteSpace(FilterRoomType) && FilterRoomType != "Tất cả")
        {
            var roomTypes = await _roomRepository!.SearchRoomTypesAsync(
                propertyId.Value,
                FilterRoomType.Trim(),
                true,
                new PageRequest(1, 100),
                "code",
                ct);
            roomTypeId = roomTypes.Items
                .FirstOrDefault(t => string.Equals(t.Code, FilterRoomType.Trim(), StringComparison.OrdinalIgnoreCase))
                ?.HotelRoomTypeId;

            // An unknown type must produce no rows instead of silently ignoring
            // the user's filter and showing the complete room list.
            if (roomTypeId is null)
            {
                TotalFilteredCount = 0;
                FilteredRooms.Clear();
                BuildDashboard();
                BuildTreeItems();
                BuildDistributions();
                RebuildPageButtons();
                OnPropertyChanged(nameof(PageDisplay));
                OnPropertyChanged(nameof(HasPrevPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(TotalPages));
                return;
            }
        }

        var result = await _roomRepository!.SearchRoomsAsync(
            propertyId.Value,
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            roomTypeId,
            string.IsNullOrWhiteSpace(FilterFloor) || FilterFloor == "Tất cả" ? null : FilterFloor,
            string.IsNullOrWhiteSpace(FilterBuilding) || FilterBuilding == "Tất cả" ? null : FilterBuilding,
            operationalStatus,
            true,
            new PageRequest(CurrentPage, PageSize),
            "room-number",
            false,
            ct,
            ParseHousekeepingStatus(FilterHkStatus),
            ParseFrontOfficeStatus(FilterFoStatus),
            NullFilter(FilterRoomClass),
            NullFilter(FilterBedType),
            NullFilter(FilterView),
            NullFilter(FilterCondition),
            FilterSmokingOnly,
            FilterConnectingOnly);

        foreach (var room in result.Items)
            Rooms.Add(MapRoom(room));

        await EnrichHousekeepingStatusesAsync(propertyId.Value, ct);
        await EnrichStayDataAsync(propertyId.Value, ct);

        TotalFilteredCount = result.Total;
        FloorFilters = Rooms.Select(r => r.Floor)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Prepend("Tất cả")
            .ToArray();
        BuildDashboard();
        BuildTreeItems();
        BuildDistributions();
        FilteredRooms.Clear();
        foreach (var room in Rooms)
            FilteredRooms.Add(room);

        if (SelectedRoom is null || Rooms.All(r => r.RoomNumber != SelectedRoom.RoomNumber))
            await SelectRoom(Rooms.FirstOrDefault());

        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(HasPrevPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(TotalPages));
    }

    private static RoomMasterDto MapRoom(HotelRoomDto room)
    {
        var foStatus = room.OperationalStatus switch
        {
            HotelRoomOperationalStatus.Available => "VC",
            HotelRoomOperationalStatus.OutOfOrder => "OOO",
            HotelRoomOperationalStatus.OutOfService => "OOS",
            _ => "VC"
        };

        return new RoomMasterDto
        {
            HotelRoomId = room.HotelRoomId,
            RowVersion = room.RowVersion ?? [],
            RoomNumber = room.RoomNumber,
            RoomName = room.DisplayName ?? room.RoomNumber,
            Building = room.Building ?? string.Empty,
            Floor = room.Floor ?? string.Empty,
            RoomType = room.HotelRoomTypeCode,
            RoomClass = room.HotelRoomTypeName,
            RoomCondition = room.RoomCondition ?? string.Empty,
            View = room.View,
            BedType = room.BedType ?? string.Empty,
            IsSmoking = room.IsSmoking,
            IsConnecting = room.IsConnecting,
            FoStatus = foStatus,
            HkStatus = string.Empty,
            Remark = room.Zone
        };
    }

    private async Task EnrichHousekeepingStatusesAsync(Guid propertyId, CancellationToken ct)
    {
        if (_housekeepingBoardService is null)
        {
            InfoMessage = "Chưa có Housekeeping board service; không hiển thị trạng thái HK suy diễn.";
            return;
        }

        try
        {
            var snapshots = await _housekeepingBoardService.GetRoomSnapshotAsync(propertyId, ct);
            var byRoomId = snapshots.ToDictionary(snapshot => snapshot.RoomId);
            foreach (var room in Rooms)
            {
                if (!byRoomId.TryGetValue(room.HotelRoomId, out var snapshot))
                    continue;

                room.HkStatus = NormalizeHousekeepingStatus(snapshot.HousekeepingStatus);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không tải được trạng thái Nhà buồng thật: {ex.Message}";
        }
    }

    private static HousekeepingRoomStatus? ParseHousekeepingStatus(string? value)
        => value switch
        {
            "Clean" => HousekeepingRoomStatus.VacantClean,
            "Dirty" => HousekeepingRoomStatus.VacantDirty,
            "Cleaning" => HousekeepingRoomStatus.Cleaning,
            "Inspected" => HousekeepingRoomStatus.Inspected,
            "OutOfOrder" => HousekeepingRoomStatus.OutOfOrder,
            "OutOfService" => HousekeepingRoomStatus.OutOfService,
            _ => null
        };

    private static string? ParseFrontOfficeStatus(string? value)
        => value is "VC" or "VD" or "OC" or "OD" ? value : null;

    private static string? NullFilter(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "Tất cả" ? null : value;

    private async Task EnrichStayDataAsync(Guid propertyId, CancellationToken ct)
    {
        if (_stayRepository is null)
        {
            InfoMessage = "Chưa có Stay repository; không hiển thị trạng thái FO và khách suy diễn.";
            return;
        }

        try
        {
            var stays = await _stayRepository.GetCurrentInHouseAsync(propertyId, ct);
            var staysByRoom = stays.ToDictionary(stay => stay.RoomId);
            foreach (var room in Rooms)
            {
                if (staysByRoom.TryGetValue(room.HotelRoomId, out var stay))
                {
                    room.FoStatus = room.HkStatus switch
                    {
                        "Dirty" => "OD",
                        _ => "OC"
                    };
                    room.GuestName = stay.GuestName;
                    room.ReservationCode = stay.ReservationNumber;
                    room.ArrivalDate = stay.ScheduledArrivalDate;
                    room.DepartureDate = stay.ScheduledDepartureDate;
                    room.Adults = stay.Adults;
                    room.Children = stay.Children;
                }
                else if (room.FoStatus is not ("OOO" or "OOS"))
                {
                    room.FoStatus = room.HkStatus switch
                    {
                        "Dirty" or "Cleaning" => "VD",
                        _ => "VC"
                    };
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không tải được lưu trú thật cho Room Master: {ex.Message}";
        }
    }

    private static string NormalizeHousekeepingStatus(string? rawStatus)
        => rawStatus?.Trim() switch
        {
            "VacantClean" or "OccupiedClean" => "Clean",
            "VacantDirty" or "OccupiedDirty" => "Dirty",
            "Cleaning" or "CleaningInProgress" => "Cleaning",
            "Inspected" => "Inspected",
            "OutOfOrder" => "OutOfOrder",
            "OutOfService" => "OutOfService",
            _ => string.Empty
        };

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new RoomMasterKpi { Label = "Tổng phòng", Value = TotalRooms.ToString(), Color = "#4B5563", BgColor = "#F8FAFC", IconGlyph = "" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "Phòng trống", Value = VacantRooms.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "VC" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "Đang ở", Value = OccupiedRooms.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", IconGlyph = "", FilterKey = null, FilterValue = null });
        DashboardKpis.Add(new RoomMasterKpi { Label = "VC", Value = VacantCleanCount.ToString(), Color = "#16A34A", BgColor = "#DCFCE7", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "VC" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "VD", Value = VacantDirtyCount.ToString(), Color = "#CA8A04", BgColor = "#FEF9C3", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "VD" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "OC", Value = OccupiedCleanCount.ToString(), Color = "#2563EB", BgColor = "#DBEAFE", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "OC" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "OD", Value = OccupiedDirtyCount.ToString(), Color = "#DC2626", BgColor = "#FEE2E2", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "OD" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "OOO", Value = OooCount.ToString(), Color = "#6B7280", BgColor = "#F3F4F6", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "OOO" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "OOS", Value = OosCount.ToString(), Color = "#EA580C", BgColor = "#FFEDD5", IconGlyph = "", FilterKey = "FoStatus", FilterValue = "OOS" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "Connecting", Value = ConnectingCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF", IconGlyph = "" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "Suite", Value = SuiteCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
        DashboardKpis.Add(new RoomMasterKpi { Label = "Công suất", Value = $"{OccupancyPct}%", Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
    }

    private void BuildTreeItems()
    {
        TreeItems.Clear();

        // All rooms
        TreeItems.Add(new RoomTreeItem { Label = "Tất cả phòng", IconGlyph = "", FilterKey = null, FilterValue = null, Count = TotalRooms, IsSelected = true });

        // By Building
        var buildingNode = new RoomTreeItem { Label = "Building", IconGlyph = "", IsExpanded = true };
        foreach (var bld in new[] { "A", "B", "C" })
        {
            var cnt = Rooms.Count(r => r.Building == bld);
            if (cnt > 0) buildingNode.Children.Add(new RoomTreeItem { Label = $"Building {bld}", IconGlyph = "", FilterKey = "Building", FilterValue = bld, Count = cnt });
        }
        TreeItems.Add(buildingNode);

        // By Floor
        var floorNode = new RoomTreeItem { Label = "Tầng", IconGlyph = "", IsExpanded = true };
        foreach (var f in Enumerable.Range(1, 8))
        {
            var cnt = Rooms.Count(r => r.Floor == f.ToString());
            if (cnt > 0) floorNode.Children.Add(new RoomTreeItem { Label = $"Tầng {f}", IconGlyph = "", FilterKey = "Floor", FilterValue = f.ToString(), Count = cnt });
        }
        TreeItems.Add(floorNode);

        // By Room Type
        var typeNode = new RoomTreeItem { Label = "Loại phòng", IconGlyph = "", IsExpanded = true };
        foreach (var (rt, name) in new[] { ("STD", "Standard"), ("SUP", "Superior"), ("DLX", "Deluxe"), ("JNR", "Junior Suite"), ("STE", "Suite"), ("PRE", "Presidential"), ("FAM", "Family"), ("CON", "Connecting") })
        {
            var cnt = Rooms.Count(r => r.RoomType == rt);
            if (cnt > 0) typeNode.Children.Add(new RoomTreeItem { Label = name, IconGlyph = "", FilterKey = "RoomType", FilterValue = rt, Count = cnt });
        }
        TreeItems.Add(typeNode);

        // By Status
        var statusNode = new RoomTreeItem { Label = "Trạng thái FO", IconGlyph = "", IsExpanded = true };
        foreach (var (fs, label) in new[] { ("VC", "Vacant Clean"), ("VD", "Vacant Dirty"), ("OC", "Occupied Clean"), ("OD", "Occupied Dirty"), ("OOO", "Out Of Order"), ("OOS", "Out Of Service") })
        {
            var cnt = Rooms.Count(r => r.FoStatus == fs);
            if (cnt > 0) statusNode.Children.Add(new RoomTreeItem { Label = label, IconGlyph = "", FilterKey = "FoStatus", FilterValue = fs, Count = cnt });
        }
        TreeItems.Add(statusNode);

        // By HK
        var hkNode = new RoomTreeItem { Label = "Housekeeping", IconGlyph = "", IsExpanded = false };
        foreach (var (hs, label) in new[] { ("Clean", "Sạch"), ("Dirty", "Bẩn"), ("Cleaning", "Đang dọn"), ("Inspected", "Đã kiểm tra") })
        {
            var cnt = Rooms.Count(r => r.HkStatus == hs);
            if (cnt > 0) hkNode.Children.Add(new RoomTreeItem { Label = label, IconGlyph = "", FilterKey = "HkStatus", FilterValue = hs, Count = cnt });
        }
        TreeItems.Add(hkNode);
    }

    private void BuildDistributions()
    {
        var chartColors = new[] { "#4A8EFF", "#16A34A", "#EA580C", "#7C3AED", "#DC2626", "#0891B2", "#D97706", "#E32F70", "#6B7280" };

        // Type distribution
        TypeDistribution.Clear();
        var typeDist = Rooms.GroupBy(r => r.RoomTypeName).OrderByDescending(g => g.Count());
        int ci = 0;
        foreach (var g in typeDist)
            TypeDistribution.Add(new RoomDistributionItem { Label = g.Key, Count = g.Count(), Color = chartColors[ci++ % chartColors.Length], Percentage = Math.Round((double)g.Count() / TotalRooms * 100, 1) });

        // Status distribution
        StatusDistribution.Clear();
        var statusDist = Rooms.GroupBy(r => r.FoStatus).OrderByDescending(g => g.Count());
        ci = 0;
        foreach (var g in statusDist)
            StatusDistribution.Add(new RoomDistributionItem { Label = g.First().FoStatusLabel, Count = g.Count(), Color = chartColors[ci++ % chartColors.Length], Percentage = Math.Round((double)g.Count() / TotalRooms * 100, 1) });

        // Class distribution
        ClassDistribution.Clear();
        var classDist = Rooms.GroupBy(r => r.RoomClass).OrderByDescending(g => g.Count());
        ci = 0;
        foreach (var g in classDist)
            ClassDistribution.Add(new RoomDistributionItem { Label = g.Key, Count = g.Count(), Color = chartColors[ci++ % chartColors.Length], Percentage = Math.Round((double)g.Count() / TotalRooms * 100, 1) });
    }

    private void BuildFacilities(RoomMasterDto room)
    {
        RoomFacilities.Clear();
        var allFacs = new (string Name, string Icon, bool Val)[]
        {
            ("TV", "📺", room.HasTv), ("Smart TV", "🖥", room.HasSmartTv), ("Minibar", "🍸", room.HasMinibar),
            ("Safe", "🔒", room.HasSafe), ("Bồn tắm", "🛁", room.HasBathtub), ("Vòi sen", "🚿", room.HasShower),
            ("Máy sấy tóc", "💨", room.HasHairDryer), ("Wifi", "📶", room.HasWifi), ("Ban công", "🌅", room.HasBalcony),
            ("Bếp", "🍳", room.HasKitchen), ("Cà phê", "☕", room.HasCoffeeMachine), ("Bàn ủi", "👔", room.HasIron),
            ("Điều hòa", "❄", room.HasAirCon), ("Bàn làm việc", "🪑", room.HasDesk),
        };
        foreach (var (name, icon, val) in allFacs)
            RoomFacilities.Add(new RoomFacilityItem { Name = name, IconGlyph = icon, IsAvailable = val, Category = val ? "Có" : "Không" });
    }

    private async Task LoadHistoryAsync(RoomMasterDto room, CancellationToken ct = default)
    {
        RoomHistory.Clear();
        InfoMessage = string.Empty;

        if (!_usingRealData || _auditService is null || room.HotelRoomId == Guid.Empty)
        {
            InfoMessage = "Chưa có dịch vụ audit thật để tải lịch sử phòng; không hiển thị dữ liệu suy diễn.";
            return;
        }

        try
        {
            var auditRows = await _auditService.QueryAsync(
                entityName: "HotelRoom",
                entityId: room.HotelRoomId.ToString("D"),
                cancellationToken: ct);

            foreach (var audit in auditRows.OrderByDescending(x => x.CreatedAt))
            {
                var action = string.IsNullOrWhiteSpace(audit.Action) ? "Audit" : audit.Action;
                RoomHistory.Add(new RoomHistoryEntry
                {
                    Timestamp = audit.CreatedAt,
                    Event = action,
                    Detail = string.IsNullOrWhiteSpace(audit.Detail)
                        ? $"HotelRoom/{room.RoomNumber}"
                        : audit.Detail,
                    IconGlyph = HistoryIcon(action),
                    Color = HistoryColor(action)
                });
            }

            if (RoomHistory.Count == 0)
                InfoMessage = "Phòng này chưa có bản ghi audit thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được lịch sử phòng từ audit: {ex.Message}";
        }
    }

    private static string HistoryIcon(string action) => action.ToLowerInvariant() switch
    {
        var x when x.Contains("status") || x.Contains("activate") || x.Contains("deactivate") => "",
        var x when x.Contains("create") => "",
        var x when x.Contains("update") || x.Contains("change") => "",
        _ => ""
    };

    private static string HistoryColor(string action) => action.ToLowerInvariant() switch
    {
        var x when x.Contains("create") || x.Contains("activate") => "#16A34A",
        var x when x.Contains("deactivate") => "#DC2626",
        var x when x.Contains("update") || x.Contains("change") => "#2563EB",
        _ => "#64748B"
    };

    // ═══ FILTERS ═══

    [RelayCommand]
    private void ApplyFilters()
    {
        if (_usingRealData)
        {
            _ = ReloadRealDataAsync();
            return;
        }

        ApplyInMemoryFilters();
    }

    // Used only by the no-service/design-time state. Production filtering is
    // reloaded from the Room Master repository above; this method never creates
    // or enriches rows with synthetic data.
    private void ApplyInMemoryFilters()
    {
        var q = Rooms.AsEnumerable();
        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.RoomNumber.ToLower().Contains(search) || r.RoomName.ToLower().Contains(search) || (r.GuestName?.ToLower().Contains(search) ?? false) || (r.ReservationCode?.ToLower().Contains(search) ?? false) || (r.Telephone?.Contains(search) ?? false) || (r.Remark?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrEmpty(FilterFoStatus) && FilterFoStatus != "Tất cả") q = q.Where(r => r.FoStatus == FilterFoStatus);
        if (!string.IsNullOrEmpty(FilterHkStatus) && FilterHkStatus != "Tất cả") q = q.Where(r => r.HkStatus == FilterHkStatus);
        if (!string.IsNullOrEmpty(FilterRoomType) && FilterRoomType != "Tất cả") q = q.Where(r => r.RoomType == FilterRoomType);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterBuilding) && FilterBuilding != "Tất cả") q = q.Where(r => r.Building == FilterBuilding);
        if (!string.IsNullOrEmpty(FilterRoomClass) && FilterRoomClass != "Tất cả") q = q.Where(r => r.RoomClass == FilterRoomClass);
        if (!string.IsNullOrEmpty(FilterBedType) && FilterBedType != "Tất cả") q = q.Where(r => r.BedType == FilterBedType);
        if (!string.IsNullOrEmpty(FilterView) && FilterView != "Tất cả") q = q.Where(r => r.View == FilterView);
        if (!string.IsNullOrEmpty(FilterCondition) && FilterCondition != "Tất cả") q = q.Where(r => r.RoomCondition == FilterCondition);
        if (FilterSmokingOnly) q = q.Where(r => r.IsSmoking);
        if (FilterConnectingOnly) q = q.Where(r => r.IsConnecting);
        if (FilterOccupiedOnly) q = q.Where(r => r.IsOccupied);
        if (FilterVacantOnly) q = q.Where(r => !r.IsOccupied && r.FoStatus is not "OOO" and not "OOS");
        if (FilterOooOos) q = q.Where(r => r.FoStatus is "OOO" or "OOS");
        if (FilterVipOnly) q = q.Where(r => !string.IsNullOrEmpty(r.VipLevel));

        // TreeView filter
        if (SelectedTreeItem?.FilterKey is not null)
            q = SelectedTreeItem.FilterKey switch
            {
                "Building" => q.Where(r => r.Building == SelectedTreeItem.FilterValue),
                "Floor" => q.Where(r => r.Floor == SelectedTreeItem.FilterValue),
                "RoomType" => q.Where(r => r.RoomType == SelectedTreeItem.FilterValue),
                "FoStatus" => q.Where(r => r.FoStatus == SelectedTreeItem.FilterValue),
                "HkStatus" => q.Where(r => r.HkStatus == SelectedTreeItem.FilterValue),
                _ => q
            };

        var list = q.OrderBy(r => int.Parse(r.RoomNumber)).ToList();
        TotalFilteredCount = list.Count;
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredRooms.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            FilteredRooms.Add(r);

        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage)); OnPropertyChanged(nameof(TotalPages));
        RebuildPageButtons();
    }

    private async Task ReloadRealDataAsync(bool resetPage = false)
    {
        if (!_usingRealData) return;
        if (resetPage) CurrentPage = 1;
        _reloadCts?.Cancel();
        _reloadCts?.Dispose();
        _reloadCts = new CancellationTokenSource();
        try
        {
            await LoadAsync(_reloadCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load rooms: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty; FilterFoStatus = null; FilterHkStatus = null; FilterRoomType = null; FilterFloor = null;
        FilterBuilding = null; FilterRoomClass = null; FilterBedType = null; FilterView = null; FilterCondition = null;
        FilterSmokingOnly = false; FilterConnectingOnly = false; FilterOccupiedOnly = false; FilterVacantOnly = false;
        FilterOooOos = false; FilterVipOnly = false; IsFilterExpanded = false;
        SelectTreeItem(TreeItems.FirstOrDefault());
        ApplyFilters();
    }

    [RelayCommand] private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void SelectTreeItem(RoomTreeItem? item)
    {
        if (item is null) return;
        // Deselect all
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true;
        SelectedTreeItem = item;
        CurrentPage = 1;
        ApplyFilters();
    }

    private static void DeselectTree(RoomTreeItem item)
    {
        item.IsSelected = false;
        foreach (var child in item.Children) DeselectTree(child);
    }

    [RelayCommand]
    private void ApplyKpiFilter(RoomMasterKpi? kpi)
    {
        if (kpi?.FilterKey is null) return;
        ClearFilters();
        if (kpi.FilterKey == "FoStatus") FilterFoStatus = kpi.FilterValue;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterFoStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterHkStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterBuildingChanged(string? value) => ApplyFilters();
    partial void OnFilterRoomClassChanged(string? value) => ApplyFilters();
    partial void OnFilterBedTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterViewChanged(string? value) => ApplyFilters();
    partial void OnFilterConditionChanged(string? value) => ApplyFilters();
    partial void OnFilterSmokingOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterConnectingOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterOccupiedOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterVacantOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterOooOosChanged(bool value) => ApplyFilters();
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear(); var total = TotalPages; var cur = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages;
        if (total <= 5) pages = Enumerable.Range(1, total).Cast<int?>();
        else if (cur <= 3) pages = new int?[] { 1, 2, 3, 4, 5, null, total };
        else if (cur >= total - 2) pages = new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total };
        else pages = new int?[] { 1, null, cur - 1, cur, cur + 1, null, total };
        foreach (var p in pages)
            PageButtons.Add(p is null ? new RoomPaginationButton("…", null, false, true) : new RoomPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == cur));
    }

    // ═══ SELECTION ═══

    [RelayCommand]
    private async Task SelectRoom(RoomMasterDto? r)
    {
        _historyCts?.Cancel();
        _historyCts?.Dispose();
        _historyCts = r is null ? null : new CancellationTokenSource();
        SelectedRoom = r; IsDetailOpen = r is not null; SelectedDetailTab = 0;
        if (r is not null)
        {
            BuildFacilities(r);
            await LoadHistoryAsync(r, _historyCts?.Token ?? CancellationToken.None);
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        _historyCts?.Cancel();
        _historyCts?.Dispose();
        _historyCts = null;
        IsDetailOpen = false; SelectedRoom = null; RoomFacilities.Clear(); RoomHistory.Clear();
    }

    partial void OnSelectedRoomChanged(RoomMasterDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    // ═══ VIEW MODE ═══

    [RelayCommand] private void SetViewTable() { ViewMode = 0; ApplyFilters(); }
    [RelayCommand] private void SetViewCard() { ViewMode = 1; ApplyFilters(); }

    partial void OnViewModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsTableView)); OnPropertyChanged(nameof(IsCardView)); OnPropertyChanged(nameof(IsFloorView));
    }

    // ═══ TOOLBAR ACTIONS ═══

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void ToggleBatchMode() => IsBatchMode = !IsBatchMode;

    [RelayCommand]
    private async Task AddRoom(CancellationToken ct)
    {
        if (!_usingRealData || _createRoomHandler is null || _hotelQueries is null)
        {
            ErrorMessage = "Chưa có Room Master command/query thật; không tạo phòng giả.";
            return;
        }

        var roomTypes = await _hotelQueries.SearchTypes(null, true, new PageRequest(1, 200), "name", ct);
        if (roomTypes.IsFailure || roomTypes.Value is null || roomTypes.Value.Items.Count == 0)
        {
            ErrorMessage = roomTypes.Error?.Message ?? "Chưa có loại phòng hoạt động để tạo phòng.";
            return;
        }

        var dialog = new Views.CreateHotelRoomDialog(roomTypes.Value.Items)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.SelectedRoomType is null) return;

        var result = await _createRoomHandler.HandleAsync(
            new CreateHotelRoomCommand(
                dialog.SelectedRoomType.HotelRoomTypeId,
                dialog.RoomNumber,
                dialog.DisplayName,
                dialog.Floor,
                dialog.Building,
                dialog.Zone,
                null,
                dialog.RoomCondition,
                dialog.View,
                dialog.BedType,
                dialog.IsSmoking,
                dialog.IsConnecting),
            ct);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể tạo phòng.";
            return;
        }

        InfoMessage = $"Đã tạo phòng {result.Value!.RoomNumber}.";
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task EditRoom(CancellationToken ct)
    {
        if (SelectedRoom is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Hãy chọn một phòng thật trước khi sửa hồ sơ.";
            return;
        }
        if (!_usingRealData || _updateRoomHandler is null || _hotelQueries is null)
        {
            ErrorMessage = "Chưa có Room Master update command/query thật; không lưu dữ liệu giả.";
            return;
        }

        var current = await _hotelQueries.GetRoom(SelectedRoom.HotelRoomId, ct);
        if (current.IsFailure || current.Value is null)
        {
            ErrorMessage = current.Error?.Message ?? "Không đọc được hồ sơ phòng hiện tại.";
            return;
        }

        var dialog = new Views.UpdateHotelRoomDialog(current.Value)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        var result = await _updateRoomHandler.HandleAsync(
            new UpdateHotelRoomCommand(
                current.Value.HotelRoomId,
                current.Value.RoomNumber,
                dialog.DisplayName,
                dialog.Floor,
                dialog.Building,
                dialog.Zone,
                null,
                current.Value.RowVersion ?? [],
                dialog.RoomCondition,
                dialog.View,
                dialog.BedType,
                dialog.IsSmoking,
                dialog.IsConnecting),
            ct);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể cập nhật hồ sơ phòng.";
            return;
        }

        InfoMessage = $"Đã cập nhật hồ sơ phòng {current.Value.RoomNumber}.";
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task ChangeStatus()
    {
        if (SelectedRoom is null)
        {
            ErrorMessage = "Hãy chọn một phòng trước khi đổi trạng thái.";
            return;
        }
        if (_changeStatusHandler is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Room Master command chưa được đăng ký; không cập nhật giả.";
            return;
        }

        var dialog = new Views.RoomOperationalStatusDialog(SelectedRoom.FoStatus)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.SelectedStatus is null) return;

        var result = await _changeStatusHandler.HandleAsync(
            new ChangeHotelRoomOperationalStatusCommand(SelectedRoom.HotelRoomId, dialog.SelectedStatus.Value, SelectedRoom.RowVersion),
            CancellationToken.None);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error?.Message ?? "Không thể cập nhật trạng thái phòng.";
            return;
        }

        InfoMessage = $"Đã cập nhật trạng thái phòng {SelectedRoom.RoomNumber}.";
        await LoadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void GenerateQr()
    {
        ErrorMessage = string.Empty;
        if (!_usingRealData || SelectedRoom is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Hãy chọn một phòng thật trước khi tạo QR Code.";
            return;
        }

        try
        {
            var payload = $"asmile://hotel/room/{SelectedRoom.HotelRoomId:D}?number={Uri.EscapeDataString(SelectedRoom.RoomNumber)}";
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var pngBytes = new QRCoder.PngByteQRCode(data).GetGraphic(12);
            var dialog = new Views.QrCodeDialog(SelectedRoom.RoomNumber, payload, pngBytes)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tạo QR Code từ phòng thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BatchMarkClean(CancellationToken ct) =>
        await BatchHousekeepingAction(HkRoomAction.MarkClean, "Sạch", ct);

    [RelayCommand]
    private async Task BatchMarkDirty(CancellationToken ct) =>
        await BatchHousekeepingAction(HkRoomAction.MarkDirty, "Bẩn", ct);

    private async Task BatchHousekeepingAction(HkRoomAction action, string label, CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!_usingRealData || _housekeepingWorkflowService is null || _contextAccessor?.Current is not { } context
            || context.Property.PropertyId.Value == Guid.Empty)
        {
            ErrorMessage = "Chưa có Housekeeping workflow/property context thật; không cập nhật dữ liệu giả.";
            return;
        }

        var selected = Rooms.Where(room => room.IsSelected && room.HotelRoomId != Guid.Empty).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng thật trước khi cập nhật Housekeeping hàng loạt.";
            return;
        }

        var succeeded = 0;
        var rejected = 0;
        foreach (var room in selected)
        {
            try
            {
                var result = await _housekeepingWorkflowService.ExecuteAsync(
                    new HkRoomActionRequest(context.Property.PropertyId.Value, room.HotelRoomId, action), ct);
                if (result.IsSuccess)
                {
                    succeeded++;
                    room.IsSelected = false;
                }
                else
                {
                    rejected++;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                rejected++;
            }
        }

        await LoadAsync(ct);
        InfoMessage = rejected == 0
            ? $"Đã cập nhật {succeeded} phòng thật sang trạng thái {label}."
            : $"Đã cập nhật {succeeded}/{selected.Count} phòng sang {label}; {rejected} phòng bị từ chối do lỗi hoặc dữ liệu không hợp lệ.";
    }

    [RelayCommand]
    private async Task BatchOoo(CancellationToken ct) => await BatchOperationalStatus(HotelRoomOperationalStatus.OutOfOrder, "OOO", ct);

    [RelayCommand]
    private async Task BatchOos(CancellationToken ct) => await BatchOperationalStatus(HotelRoomOperationalStatus.OutOfService, "OOS", ct);

    private async Task BatchOperationalStatus(HotelRoomOperationalStatus status, string label, CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!_usingRealData || _changeStatusHandler is null)
        {
            ErrorMessage = "Chưa có Room Master status command thật; không cập nhật giả.";
            return;
        }

        var selected = Rooms.Where(room => room.IsSelected && room.HotelRoomId != Guid.Empty).ToList();
        if (selected.Count == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một phòng thật trước khi cập nhật hàng loạt.";
            return;
        }

        var succeeded = 0;
        var rejected = 0;
        foreach (var room in selected)
        {
            var result = await _changeStatusHandler.HandleAsync(
                new ChangeHotelRoomOperationalStatusCommand(room.HotelRoomId, status, room.RowVersion), ct);
            if (result.IsSuccess)
            {
                succeeded++;
                room.IsSelected = false;
            }
            else
            {
                rejected++;
            }
        }

        await LoadAsync(ct);
        InfoMessage = rejected == 0
            ? $"Đã cập nhật {succeeded} phòng thật sang {label}."
            : $"Đã cập nhật {succeeded}/{selected.Count} phòng sang {label}; {rejected} phòng bị từ chối do lỗi hoặc xung đột dữ liệu.";
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!_usingRealData)
        {
            InfoMessage = "Chưa có Room Master dữ liệu thật; không xuất dữ liệu mẫu.";
            return;
        }

        var rows = FilteredRooms.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có phòng thật trong bộ lọc hiện tại để xuất.";
            return;
        }

        try
        {
            var path = CsvExportHelper.Export(
                "room_master",
                ["Mã phòng", "Tên phòng", "Tòa", "Tầng", "Loại phòng", "FO", "HK", "Tình trạng", "Khách", "Ngày đến", "Ngày đi"],
                rows.Select(room => (IReadOnlyList<string>)[
                    room.RoomNumber,
                    room.RoomName,
                    room.Building,
                    room.Floor,
                    room.RoomTypeName,
                    room.FoStatusLabel,
                    room.HkStatusLabel,
                    room.RoomCondition,
                    room.GuestName ?? string.Empty,
                    room.ArrivalDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    room.DepartureDate?.ToString("dd/MM/yyyy") ?? string.Empty
                ]).ToList());

            InfoMessage = $"Đã xuất {rows.Count} phòng thật ra file CSV tương thích Excel: {path}";
            MessageBox.Show(InfoMessage, "Xuất danh sách phòng", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể xuất danh sách phòng thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (!_usingRealData)
        {
            InfoMessage = "Chưa có Room Master dữ liệu thật; không in dữ liệu mẫu.";
            return;
        }

        var rows = FilteredRooms.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có phòng thật trong bộ lọc hiện tại để in.";
            return;
        }

        var content = new StringBuilder()
            .AppendLine($"Danh sách phòng — {DateTime.Now:dd/MM/yyyy HH:mm}")
            .AppendLine($"Số lượng: {rows.Count}")
            .AppendLine(new string('-', 112))
            .AppendLine("Phòng | Tên | Tòa/Tầng | Loại | FO | HK | Tình trạng | Khách | Đến | Đi")
            .AppendLine(new string('-', 112));

        foreach (var room in rows)
        {
            content.AppendLine(string.Join(" | ",
                room.RoomNumber,
                room.RoomName,
                $"{room.Building}/{room.Floor}",
                room.RoomTypeName,
                room.FoStatusLabel,
                room.HkStatusLabel,
                room.RoomCondition,
                room.GuestName ?? "",
                room.ArrivalDate?.ToString("dd/MM/yyyy") ?? "",
                room.DepartureDate?.ToString("dd/MM/yyyy") ?? ""));
        }

        var preview = new PrintPreviewDialog("Danh sách phòng", content.ToString())
        {
            Owner = System.Windows.Application.Current?.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        preview.ShowDialog();
    }

    // ═══ CONTEXT MENU ═══

    [RelayCommand]
    private async Task OpenReservationFromRoom(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Hãy chọn một phòng thật trước khi mở Reservation.";
            return;
        }
        if (!_usingRealData || _reservationRepository is null || _contextAccessor?.Current is not { } context)
        {
            ErrorMessage = "Chưa có Reservation query/property context thật; không mở dữ liệu giả.";
            return;
        }

        try
        {
            var page = await _reservationRepository.SearchAsync(
                context.Property.PropertyId.Value,
                new ReservationSearchFilter(null, null, null, null, null, null, SelectedRoom.HotelRoomId, null, null, null, null, null),
                new PageRequest(1, 50),
                "ArrivalDate",
                ct);
            var businessDate = context.BusinessDate.BusinessDate;
            var reservation = page.Items
                .Where(item => item.Status is not (ReservationStatus.Cancelled or ReservationStatus.NoShow)
                    && item.DepartureDate >= businessDate)
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải Reservation theo phòng: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenFolioFromRoom(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Hãy chọn một phòng thật trước khi mở Folio.";
            return;
        }
        if (!_usingRealData || _stayRepository is null || _contextAccessor?.Current is not { } context)
        {
            ErrorMessage = "Chưa có Stay/Folio query/property context thật; không mở dữ liệu giả.";
            return;
        }

        try
        {
            var stay = (await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, ct))
                .FirstOrDefault(item => item.RoomId == SelectedRoom.HotelRoomId || string.Equals(item.RoomNumber, SelectedRoom.RoomNumber, StringComparison.OrdinalIgnoreCase));
            if (stay is null)
            {
                ErrorMessage = $"Không tìm thấy Stay thật đang ở phòng {SelectedRoom.RoomNumber}; không có Folio để mở.";
                return;
            }

            var existing = System.Windows.Application.Current.Windows
                .OfType<Views.FolioWorkspaceWindow>()
                .FirstOrDefault(window => window.IsLoaded);
            if (existing is not null)
            {
                existing.Activate();
                existing.Focus();
                return;
            }

            new Views.FolioWorkspaceWindow(stay.HotelStayId)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.Show();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải Folio theo phòng: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RoomMove(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom is null || SelectedRoom.HotelRoomId == Guid.Empty)
        {
            ErrorMessage = "Hãy chọn một phòng thật trước khi đổi phòng.";
            return;
        }
        if (!_usingRealData || _stayRepository is null || _contextAccessor?.Current is not { } context)
        {
            ErrorMessage = "Chưa có Stay query/property context thật; không mở workflow đổi phòng giả.";
            return;
        }

        try
        {
            var stay = (await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, ct))
                .FirstOrDefault(item => item.RoomId == SelectedRoom.HotelRoomId || string.Equals(item.RoomNumber, SelectedRoom.RoomNumber, StringComparison.OrdinalIgnoreCase));
            if (stay is null)
            {
                ErrorMessage = $"Phòng {SelectedRoom.RoomNumber} không có Stay thật đang lưu trú để đổi phòng.";
                return;
            }

            var existing = System.Windows.Application.Current.Windows
                .OfType<Views.RoomMoveWindow>()
                .FirstOrDefault(window => window.IsLoaded);
            if (existing is not null) { existing.Activate(); existing.Focus(); return; }
            new Views.RoomMoveWindow(stay.HotelStayId)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.Show();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể mở workflow đổi phòng thật: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task RoomHistoryCmd()
    {
        if (SelectedRoom is null)
        {
            MessageBox.Show("Hãy chọn một phòng thật trước khi xem lịch sử.", "Room History", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _historyCts?.Cancel();
        _historyCts?.Dispose();
        _historyCts = new CancellationTokenSource();
        await LoadHistoryAsync(SelectedRoom, _historyCts.Token);
    }
}
