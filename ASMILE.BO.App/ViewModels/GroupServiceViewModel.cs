using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views;
using ASMILE.BO.App.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class GroupServiceDto
{
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = "Restaurant"; // Restaurant, Meeting, Transport, Spa, Laundry, Banquet, Equipment, Other
    public string SubCategory { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, CoffeeBreak, etc.
    public string Department { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly ServiceDate { get; set; }
    public TimeOnly ServiceTime { get; set; }
    public int DurationMinutes { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal ServiceChargeRate { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
    public decimal DiscountAmount => SubTotal * Discount;
    public decimal TaxAmount => (SubTotal - DiscountAmount) * TaxRate;
    public decimal ServiceCharge => (SubTotal - DiscountAmount) * ServiceChargeRate;
    public decimal TotalAmount => SubTotal - DiscountAmount + TaxAmount + ServiceCharge;
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - PaidAmount;
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, InProgress, Completed, Cancelled, PostedToFolio
    public string ChargeTo { get; set; } = "MasterFolio"; // MasterFolio, GuestFolio, Room, Company, Agent
    public Guid GroupReservationId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int RoomCount { get; set; }
    public string? PicPerson { get; set; }
    public string? Remark { get; set; }
    public string? VipLevel { get; set; }
    public string? GuideName { get; set; }
    public string? DriverName { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }

    // Restaurant-specific
    public string? MenuType { get; set; }
    public string? RestaurantName { get; set; }

    // Meeting-specific
    public string? MeetingRoom { get; set; }
    public string? MeetingSetup { get; set; }
    public bool HasProjector { get; set; }
    public bool HasScreen { get; set; }
    public bool HasSound { get; set; }
    public bool HasStage { get; set; }
    public bool HasFlower { get; set; }
    public bool HasWelcomeBoard { get; set; }

    // Transport-specific
    public string? FlightNumber { get; set; }
    public string? VehicleType { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropoffLocation { get; set; }
    public string? ETA { get; set; }
    public string? ETD { get; set; }

    // Spa-specific
    public string? GuestName { get; set; }
    public string? TreatmentName { get; set; }
    public string? TherapistName { get; set; }

    // Laundry-specific
    public bool IsExpress { get; set; }
    public string? DeliveryTime { get; set; }

    // Status display
    public string StatusBadge => Status switch
    {
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "InProgress" => "Đang phục vụ",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        "PostedToFolio" => "Đã chuyển Folio",
        _ => Status
    };
    public string StatusColor => Status switch
    {
        "Pending" => "#D97706",
        "Confirmed" => "#2563EB",
        "InProgress" => "#0891B2",
        "Completed" => "#16A34A",
        "Cancelled" => "#DC2626",
        "PostedToFolio" => "#7C3AED",
        _ => "#64748B"
    };
    public string CategoryBadge => Category switch
    {
        "Restaurant" => "Nhà hàng",
        "Meeting" => "Hội họp",
        "Transport" => "Vận chuyển",
        "Spa" => "Spa",
        "Laundry" => "Giặt ủi",
        "Banquet" => "Tiệc",
        "Equipment" => "Thiết bị",
        _ => "Khác"
    };
    public string CategoryIcon => Category switch
    {
        "Restaurant" => "",
        "Meeting" => "",
        "Transport" => "",
        "Spa" => "",
        "Laundry" => "",
        "Banquet" => "",
        "Equipment" => "",
        _ => ""
    };
    public string ChargeToBadge => ChargeTo switch
    {
        "MasterFolio" => "Master Folio",
        "GuestFolio" => "Guest Folio",
        "Room" => "Phòng",
        "Company" => "Công ty",
        "Agent" => "Đại lý",
        _ => ChargeTo
    };
}

public sealed class GroupServiceGroupOption
{
    public Guid GroupReservationId { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public string LeaderName { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public int RoomCount { get; init; }
    public int ServiceCount { get; init; }
    public string VipLevel { get; init; } = "—";
}

public sealed class ServiceDashboardKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string IconGlyph { get; set; } = "";
}

public sealed class ServiceCategoryItem
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public int Count { get; set; }
    public string Color { get; set; } = "#64748B";
    public bool IsSelected { get; set; }
}

public sealed class ServiceTimelineEntry
{
    public TimeOnly Time { get; set; }
    public string ServiceName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public string Location { get; set; } = "";
}

public sealed class ServiceHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string User { get; set; } = "";
    public string Detail { get; set; } = "";
}

public sealed class ServicePaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public ServicePaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

public sealed class ServiceRevenueItem
{
    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
    public string Color { get; set; } = "";
}

public sealed class GroupServiceFolioOption
{
    public Guid FolioId { get; init; }
    public string FolioNumber { get; init; } = string.Empty;
    public string GuestName { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Display => $"{FolioNumber} • Phòng {RoomNumber} • {GuestName} • Dư {Balance:N0}";
}

// ═══ VIEW MODEL ═══

public sealed partial class GroupServiceViewModel : ViewModelBase
{
    private readonly IGroupServiceOrderQueryService _queryService;
    private readonly IGroupServiceOrderCommandService _commandService;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;
    private readonly IHotelFolioRepository _folioRepository;
    private readonly IGroupReservationQueryService _groupReservationQueryService;
    private readonly IEmailDispatchService _emailDispatchService;

    public GroupServiceViewModel(
        IGroupServiceOrderQueryService queryService,
        IGroupServiceOrderCommandService commandService,
        IFrontOfficeExecutionContextAccessor contextAccessor,
        IHotelFolioRepository folioRepository,
        IGroupReservationQueryService groupReservationQueryService,
        IEmailDispatchService emailDispatchService)
    {
        _queryService = queryService;
        _commandService = commandService;
        _contextAccessor = contextAccessor;
        _folioRepository = folioRepository;
        _groupReservationQueryService = groupReservationQueryService;
        _emailDispatchService = emailDispatchService;
    }

    // ── Collections ──
    public ObservableCollection<GroupServiceDto> Services { get; } = new();
    public ObservableCollection<GroupServiceDto> FilteredServices { get; } = new();
    public ObservableCollection<ServiceDashboardKpi> DashboardKpis { get; } = new();
    public ObservableCollection<ServiceCategoryItem> CategoryItems { get; } = new();
    public ObservableCollection<ServicePaginationButton> PageButtons { get; } = new();
    public ObservableCollection<ServiceTimelineEntry> ScheduleTimeline { get; } = new();
    public ObservableCollection<ServiceHistoryEntry> ServiceHistory { get; } = new();
    public ObservableCollection<ServiceRevenueItem> RevenueSummary { get; } = new();
    public ObservableCollection<GroupServiceGroupOption> GroupOptions { get; } = new();
    public ObservableCollection<GroupServiceFolioOption> FolioOptions { get; } = new();
    public ObservableCollection<GroupServiceDto> SelectedServices { get; } = new();

    // ── Selection ──
    [ObservableProperty] private GroupServiceDto? _selectedService;
    [ObservableProperty] private GroupServiceGroupOption? _selectedGroupOption;
    [ObservableProperty] private GroupServiceFolioOption? _selectedTargetFolio;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private int _selectedCategoryIndex;

    // ── Loading & State ──
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isGroupSelectionVisible = true;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private bool _isAddDialogOpen;
    [ObservableProperty] private bool _isBatchMode;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // ── Search & Filter ──
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    // Keep the group selector visible and initialized so the module can be
    // scoped to a đoàn immediately after opening.
    [ObservableProperty] private string? _filterGroup = "Tất cả";
    [ObservableProperty] private string? _filterCategory;
    [ObservableProperty] private string? _filterDepartment;
    [ObservableProperty] private string? _filterRestaurant;
    [ObservableProperty] private string? _filterChargeTo;
    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;

    // ── Pagination ──
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} dịch vụ";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedService is not null;
    public bool HasSelectedGroup => SelectedGroupOption is not null;
    public bool IsDetailClosed => !IsDetailOpen;

    // ── Computed totals ──
    public decimal TotalRevenue => Services.Sum(s => s.TotalAmount);
    public decimal TotalVat => Services.Sum(s => s.TaxAmount);
    public decimal TotalOutstanding => Services.Sum(s => s.Balance);
    public int TotalServices => Services.Count;
    public int ConfirmedCount => Services.Count(s => s.Status == "Confirmed");
    public int InProgressCount => Services.Count(s => s.Status == "InProgress");
    public int CompletedCount => Services.Count(s => s.Status == "Completed");
    public int CancelledCount => Services.Count(s => s.Status == "Cancelled");
    public int PendingCount => Services.Count(s => s.Status == "Pending");

    // ── Static options ──
    public static string[] StatusFilters { get; } = ["Tất cả", "Pending", "Confirmed", "InProgress", "Completed", "Cancelled", "PostedToFolio"];
    public static string[] CategoryFilters { get; } = ["Tất cả", "Restaurant", "Meeting", "Transport", "Spa", "Laundry", "Banquet", "Equipment", "Other"];
    public static string[] DepartmentFilters { get; } = ["Tất cả", "Food & Beverage", "Banquet", "Concierge", "Spa", "Housekeeping", "Business Center", "Front Office"];
    public static string[] ChargeToFilters { get; } = ["Tất cả", "MasterFolio", "GuestFolio", "Room", "Company", "Agent"];
    public static string[] RestaurantFilters { get; } = ["Tất cả", "Sen Restaurant", "Lotus Restaurant", "Pool Bar", "Lobby Lounge", "Executive Lounge"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];
    public ObservableCollection<string> GroupFilterOptions { get; } = new();


    // ═══ CATEGORY DEFINITIONS ═══

    private static readonly (string Key, string Label, string Icon, string Color)[] CategoryDefs =
    [
        ("All", "Tất cả", "", "#64748B"),
        ("Restaurant", "Nhà hàng", "", "#EA580C"),
        ("Meeting", "Hội họp", "", "#2563EB"),
        ("Transport", "Vận chuyển", "", "#0891B2"),
        ("Spa", "Spa", "", "#7C3AED"),
        ("Laundry", "Giặt ủi", "", "#16A34A"),
        ("Banquet", "Tiệc", "", "#D97706"),
        ("Equipment", "Thiết bị", "", "#4B5563"),
        ("Other", "Khác", "", "#94A3B8"),
    ];

    private void BuildCategories()
    {
        CategoryItems.Clear();
        foreach (var (key, label, icon, color) in CategoryDefs)
        {
            var count = key == "All" ? Services.Count : Services.Count(s => s.Category == key);
            CategoryItems.Add(new ServiceCategoryItem
            {
                Key = key, Label = label, IconGlyph = icon, Color = color, Count = count,
                IsSelected = key == "All"
            });
        }
    }

    private static GroupServiceDto Map(GroupServiceOrderDto row) => new()
    {
        ServiceId = row.GroupServiceOrderId,
        ServiceCode = row.ServiceCode,
        ServiceName = row.ServiceName,
        Category = row.Category,
        SubCategory = row.SubCategory ?? string.Empty,
        Department = row.Department ?? string.Empty,
        Provider = row.Provider ?? string.Empty,
        Location = row.Location ?? string.Empty,
        ServiceDate = row.ServiceDate,
        ServiceTime = row.ServiceTime,
        DurationMinutes = row.DurationMinutes,
        Quantity = row.Quantity,
        UnitPrice = row.UnitPrice,
        Discount = row.DiscountRate,
        TaxRate = row.TaxRate,
        ServiceChargeRate = row.ServiceChargeRate,
        PaidAmount = row.PaidAmount,
        Status = row.Status.ToString(),
        ChargeTo = row.ChargeTo,
        GroupReservationId = row.GroupReservationId,
        GroupCode = row.GroupCode,
        GroupName = row.GroupName,
        GuestName = row.GuestName,
        Remark = row.Remark,
        UpdatedAtUtc = row.UpdatedAtUtc,
        RowVersion = row.RowVersion
    };

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Services.Clear();
            GroupOptions.Clear();
            GroupFilterOptions.Clear();
            FolioOptions.Clear();
            SelectedServices.Clear();

            var context = _contextAccessor?.Current;
            if (_queryService is null || context is null)
            {
                ErrorMessage = "Chưa có ngữ cảnh cơ sở hoặc service query dịch vụ đoàn.";
                BuildCategories();
                BuildDashboard();
                ApplyFilters();
                return;
            }

            var propertyId = context.Property.PropertyId.Value;
            var rows = await _queryService.SearchAsync(propertyId, null, ct);
            foreach (var row in rows)
                Services.Add(Map(row));

            if (_folioRepository is not null)
            {
                var folios = await _folioRepository.SearchAsync(
                    new FolioSearchFilter(propertyId, null, null, HotelFolioStatus.Open, null, null, null, null),
                    new PageRequest(1, 500), ct);
                foreach (var folio in folios.Items.OrderBy(x => x.FolioNumber))
                {
                    var ledger = await _folioRepository.GetLedgerAsync(propertyId, folio.HotelFolioId, ct);
                    FolioOptions.Add(new GroupServiceFolioOption
                    {
                        FolioId = folio.HotelFolioId,
                        FolioNumber = folio.FolioNumber,
                        GuestName = ledger?.GuestName ?? string.Empty,
                        RoomNumber = ledger?.RoomNumber ?? string.Empty,
                        Balance = folio.RunningBalance
                    });
                }
            }

            foreach (var group in Services
                         .Where(x => x.GroupReservationId != Guid.Empty)
                         .GroupBy(x => x.GroupReservationId)
                         .Select(x => x.First())
                         .OrderBy(x => x.GroupName)
                         .ThenBy(x => x.GroupCode))
            {
                GroupOptions.Add(new GroupServiceGroupOption
                {
                    GroupReservationId = group.GroupReservationId,
                    GroupCode = group.GroupCode,
                    GroupName = group.GroupName,
                    ServiceCount = Services.Count(x => x.GroupReservationId == group.GroupReservationId),
                    VipLevel = "—"
                });
            }

            foreach (var groupName in Services
                         .Select(x => x.GroupName)
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x))
                GroupFilterOptions.Add(groupName);
            GroupFilterOptions.Insert(0, "Tất cả");

            BuildCategories();
            BuildDashboard();
            ApplyFilters();
            if (Services.Count == 0)
                ErrorMessage = "Chưa có dịch vụ đoàn trong dữ liệu thật.";
            return;

        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        RevenueSummary.Clear();

        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Tổng dịch vụ", Value = TotalServices.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Đã xác nhận", Value = ConfirmedCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Đang phục vụ", Value = InProgressCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Hoàn thành", Value = CompletedCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Đã hủy", Value = CancelledCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Chờ xác nhận", Value = PendingCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Doanh thu", Value = $"{TotalRevenue / 1_000_000m:F0}M", Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "VAT", Value = $"{TotalVat / 1_000_000m:F0}M", Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new ServiceDashboardKpi { Label = "Còn phải thu", Value = $"{TotalOutstanding / 1_000_000m:F0}M", Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "" });

        // Revenue breakdown by category
        var revCats = new (string Cat, string Label, string Color)[]
        {
            ("Restaurant", "Nhà hàng", "#EA580C"),
            ("Meeting", "Hội họp", "#2563EB"),
            ("Transport", "Vận chuyển", "#0891B2"),
            ("Spa", "Spa", "#7C3AED"),
            ("Laundry", "Giặt ủi", "#16A34A"),
            ("Banquet", "Tiệc", "#D97706"),
            ("Equipment", "Thiết bị", "#4B5563"),
            ("Other", "Khác", "#94A3B8"),
        };
        foreach (var (cat, label, color) in revCats)
        {
            var amt = Services.Where(s => s.Category == cat).Sum(s => s.TotalAmount);
            RevenueSummary.Add(new ServiceRevenueItem { Category = cat, Label = label, Amount = amt, Color = color });
        }
    }

    // ═══ FILTERS ═══

    [RelayCommand]
    private void ApplyFilters()
    {
        var q = Services.AsEnumerable();
        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.ServiceName.ToLower().Contains(search) || s.ServiceCode.ToLower().Contains(search) || s.GroupName.ToLower().Contains(search) || (s.GroupCode?.ToLower().Contains(search) ?? false) || (s.Provider?.ToLower().Contains(search) ?? false));

        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả")
            q = q.Where(s => s.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != "Tất cả")
            q = q.Where(s => s.Category == FilterCategory);
        if (!string.IsNullOrEmpty(FilterDepartment) && FilterDepartment != "Tất cả")
            q = q.Where(s => s.Department == FilterDepartment);
        if (!string.IsNullOrEmpty(FilterRestaurant) && FilterRestaurant != "Tất cả")
            q = q.Where(s => s.RestaurantName == FilterRestaurant || s.Provider == FilterRestaurant);
        if (!string.IsNullOrEmpty(FilterGroup) && FilterGroup != "Tất cả")
            q = q.Where(s => s.GroupName == FilterGroup);
        if (!string.IsNullOrEmpty(FilterChargeTo) && FilterChargeTo != "Tất cả")
            q = q.Where(s => s.ChargeTo == FilterChargeTo);
        if (FilterDateFrom.HasValue)
            q = q.Where(s => s.ServiceDate >= DateOnly.FromDateTime(FilterDateFrom.Value));
        if (FilterDateTo.HasValue)
            q = q.Where(s => s.ServiceDate <= DateOnly.FromDateTime(FilterDateTo.Value));

        // Category panel filter
        if (SelectedCategoryIndex >= 0 && SelectedCategoryIndex < CategoryItems.Count)
        {
            var cat = CategoryItems[SelectedCategoryIndex];
            if (cat.Key != "All")
                q = q.Where(s => s.Category == cat.Key);
        }

        var list = q.ToList();
        TotalFilteredCount = list.Count;
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredServices.Clear();
        foreach (var s in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            FilteredServices.Add(s);

        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(HasPrevPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(TotalPages));
        RebuildPageButtons();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterStatus = null;
        FilterCategory = null;
        FilterDepartment = null;
        FilterRestaurant = null;
        FilterGroup = "Tất cả";
        FilterChargeTo = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        IsFilterExpanded = false;
        SelectedCategoryIndex = 0;
        foreach (var c in CategoryItems) c.IsSelected = false;
        if (CategoryItems.Count > 0) CategoryItems[0].IsSelected = true;
        ApplyFilters();
    }

    [RelayCommand]
    private void SelectGroup(GroupServiceGroupOption? group)
    {
        if (group is null) return;

        SelectedGroupOption = group;
        FilterGroup = group.GroupName;
        CurrentPage = 1;
        IsGroupSelectionVisible = false;
        ApplyFilters();
    }

    [RelayCommand]
    private void ShowGroupSelection()
    {
        SelectedService = null;
        IsDetailOpen = false;
        IsGroupSelectionVisible = true;
    }

    [RelayCommand]
    private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    [RelayCommand]
    private void SelectCategory(ServiceCategoryItem? category)
    {
        // The alternation index is not available on the Button itself. Bind the
        // category item so every category click resolves to a real index.
        if (category is null) return;
        var index = CategoryItems.IndexOf(category);
        if (index < 0) return;
        SelectedCategoryIndex = index;
        for (int i = 0; i < CategoryItems.Count; i++)
            CategoryItems[i].IsSelected = i == index;
        CurrentPage = 1;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterCategoryChanged(string? value) => ApplyFilters();
    partial void OnFilterDepartmentChanged(string? value) => ApplyFilters();
    partial void OnFilterRestaurantChanged(string? value) => ApplyFilters();
    partial void OnFilterGroupChanged(string? value) => ApplyFilters();
    partial void OnSelectedGroupOptionChanged(GroupServiceGroupOption? value) => OnPropertyChanged(nameof(HasSelectedGroup));
    partial void OnFilterChargeToChanged(string? value) => ApplyFilters();
    partial void OnFilterDateFromChanged(DateTime? value) => ApplyFilters();
    partial void OnFilterDateToChanged(DateTime? value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page)
    {
        if (page is null) return;
        CurrentPage = Math.Clamp(page.Value, 1, TotalPages);
        ApplyFilters();
    }

    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); OnPropertyChanged(nameof(PageSize)); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages;
        var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages;
        if (total <= 5)
            pages = Enumerable.Range(1, total).Cast<int?>();
        else if (current <= 3)
            pages = new int?[] { 1, 2, 3, 4, 5, null, total };
        else if (current >= total - 2)
            pages = new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total };
        else
            pages = new int?[] { 1, null, current - 1, current, current + 1, null, total };

        foreach (var page in pages)
        {
            PageButtons.Add(page is null
                ? new ServicePaginationButton("…", null, false, true)
                : new ServicePaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
        }
    }

    // ═══ SELECTION ═══

    [RelayCommand]
    private void SelectService(GroupServiceDto? s)
    {
        SelectedService = s;
        IsDetailOpen = s is not null;
        SelectedTabIndex = 0;
        if (s is not null) BuildScheduleTimeline(s);
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        SelectedService = null;
        ScheduleTimeline.Clear();
        ServiceHistory.Clear();
    }

    partial void OnSelectedServiceChanged(GroupServiceDto? value)
    {
        IsDetailOpen = value is not null;
        SelectedTabIndex = 0;
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null) BuildScheduleTimeline(value);
    }

    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);

    private void BuildScheduleTimeline(GroupServiceDto svc)
    {
        ScheduleTimeline.Clear();
        ServiceHistory.Clear();

        // Timeline for selected service's date
        var dateServices = Services
            .Where(s => s.ServiceDate == svc.ServiceDate && s.GroupCode == svc.GroupCode)
            .OrderBy(s => s.ServiceTime)
            .Take(20)
            .ToList();

        foreach (var s in dateServices)
        {
            ScheduleTimeline.Add(new ServiceTimelineEntry
            {
                Time = s.ServiceTime,
                ServiceName = s.ServiceName,
                Category = s.Category,
                Status = s.Status,
                Location = s.Location
            });
        }

        // Do not reconstruct history from the current status. Those timestamps/users
        // would be fabricated. A dedicated audit query is still needed before this
        // tab can show real events.
        ServiceHistory.Add(new ServiceHistoryEntry
        {
            Timestamp = svc.UpdatedAtUtc.ToLocalTime(),
            Event = "Lịch sử audit",
            User = "—",
            Detail = "Chưa có API truy vấn audit chi tiết cho dịch vụ đoàn; không hiển thị mốc giả."
        });
    }

    // ═══ CRUD COMMANDS ═══

    [RelayCommand]
    private async Task AddService()
    {
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command tạo dịch vụ đoàn.";
            return;
        }

        var window = new AddGroupServiceWindow(_commandService, GroupOptions.ToList())
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
            await LoadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task EditService()
    {
        if (SelectedService is null) return;
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command sửa dịch vụ đoàn.";
            return;
        }

        var window = new AddGroupServiceWindow(_commandService, GroupOptions.ToList(), SelectedService)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
            await LoadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task DeleteService()
    {
        if (SelectedService is null) return;
        if (SelectedService.Status is "Completed" or "PostedToFolio" or "Cancelled")
        {
            ErrorMessage = "Chỉ được hủy dịch vụ đang chờ xác nhận, đã xác nhận hoặc đang phục vụ.";
            return;
        }

        await ChangeStatusAsync(SelectedService, GroupServiceOrderStatus.Cancelled, "Hủy từ thao tác xóa dịch vụ đoàn");
    }

    [RelayCommand]
    private async Task ConfirmService()
    {
        if (SelectedService is null || SelectedService.Status != "Pending") return;
        await ChangeStatusAsync(SelectedService, GroupServiceOrderStatus.Confirmed);
    }

    [RelayCommand]
    private async Task CompleteService()
    {
        if (SelectedService is null || SelectedService.Status is not ("Confirmed" or "InProgress")) return;
        await ChangeStatusAsync(SelectedService, GroupServiceOrderStatus.Completed);
    }

    [RelayCommand]
    private async Task CancelService()
    {
        if (SelectedService is null || SelectedService.Status is "Completed" or "PostedToFolio") return;
        await ChangeStatusAsync(SelectedService, GroupServiceOrderStatus.Cancelled, "Hủy từ màn hình Dịch vụ đoàn");
    }

    private async Task ChangeStatusAsync(GroupServiceDto service, GroupServiceOrderStatus status, string? reason = null)
    {
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command dịch vụ đoàn.";
            return;
        }

        try
        {
            IsLoading = true;
            await _commandService.ChangeStatusAsync(
                new ChangeGroupServiceOrderStatusRequest(service.ServiceId, status, reason),
                CancellationToken.None);
            InfoMessage = $"Đã cập nhật {service.ServiceCode}: {status}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật dịch vụ {service.ServiceCode}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PostToFolio()
    {
        if (SelectedService is null || SelectedService.Status is not ("Completed")) return;
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command post dịch vụ đoàn vào Folio.";
            return;
        }

        try
        {
            IsLoading = true;
            await _commandService.PostToFolioAsync(
                new PostGroupServiceOrderToFolioRequest(SelectedService.ServiceId),
                CancellationToken.None);
            InfoMessage = $"Đã chuyển {SelectedService.ServiceCode} vào Folio thật.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể post {SelectedService.ServiceCode} vào Folio: {FormatFolioPostError(ex)}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatFolioPostError(Exception ex) => ex.Message switch
    {
        "GROUP_SERVICE_FOLIO_TARGET_NOT_FOUND" => "chưa có Folio thật liên kết với phòng/đặt phòng của đoàn.",
        "GROUP_SERVICE_FOLIO_TARGET_AMBIGUOUS" => "có nhiều Folio thật; cần chọn Folio đích trước khi chuyển.",
        "GROUP_SERVICE_MUST_BE_COMPLETED_BEFORE_FOLIO" => "dịch vụ phải ở trạng thái Hoàn thành.",
        "GROUP_SERVICE_ALREADY_POSTED" => "dịch vụ đã được chuyển trước đó, hệ thống không ghi trùng.",
        "GROUP_SERVICE_FOLIO_NOT_FOUND" => "Folio đích không còn tồn tại hoặc không thuộc cơ sở hiện tại.",
        "GROUP_SERVICE_FOLIO_NOT_OPEN" => "Folio đích không còn ở trạng thái mở.",
        "GROUP_SERVICE_BATCH_EMPTY" => "Chưa chọn dịch vụ để post hàng loạt.",
        "GROUP_SERVICE_BATCH_MUST_BE_COMPLETED" => "Tất cả dịch vụ được chọn phải ở trạng thái Hoàn thành.",
        "GROUP_SERVICE_BATCH_ALREADY_POSTED" => "Có dịch vụ đã được chuyển Folio trước đó; hệ thống không ghi trùng.",
        "GROUP_SERVICE_FOLIO_TARGET_REQUIRED" => "Chưa chọn Folio đích.",
        "GROUP_SERVICE_BATCH_CONFIRM_REQUIRES_PENDING" => "Batch xác nhận chỉ nhận các dịch vụ đang Chờ xác nhận.",
        "GROUP_SERVICE_BATCH_COMPLETE_REQUIRES_ACTIVE" => "Batch hoàn thành chỉ nhận dịch vụ Đã xác nhận hoặc Đang phục vụ.",
        "GROUP_SERVICE_STATUS_NOT_CANCELLABLE" => "Có dịch vụ đã hoàn thành hoặc đã chuyển Folio nên không thể hủy.",
        _ => ex.Message
    };

    // ═══ BATCH OPERATIONS ═══

    [RelayCommand]
    private void ToggleBatchMode() => IsBatchMode = !IsBatchMode;

    [RelayCommand]
    private async Task BatchConfirm() => await BatchChangeStatusAsync(GroupServiceOrderStatus.Confirmed);

    [RelayCommand]
    private async Task BatchComplete() => await BatchChangeStatusAsync(GroupServiceOrderStatus.Completed);

    [RelayCommand]
    private async Task BatchCancel() => await BatchChangeStatusAsync(GroupServiceOrderStatus.Cancelled, "Hủy hàng loạt từ màn hình Dịch vụ đoàn");

    private async Task BatchChangeStatusAsync(GroupServiceOrderStatus status, string? reason = null)
    {
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command dịch vụ đoàn.";
            return;
        }
        var selected = SelectedServices.Count > 0
            ? SelectedServices.ToArray()
            : SelectedService is not null ? [SelectedService] : Array.Empty<GroupServiceDto>();
        if (selected.Length == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một dịch vụ trong bảng trước khi thao tác hàng loạt.";
            return;
        }

        try
        {
            IsLoading = true;
            var result = await _commandService.ChangeBatchStatusAsync(
                new BatchChangeGroupServiceOrderStatusRequest(selected.Select(x => x.ServiceId).ToArray(), status, reason),
                CancellationToken.None);
            InfoMessage = $"Đã cập nhật {result.GroupServiceOrderIds.Count} dịch vụ sang {result.Status}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật hàng loạt: {FormatFolioPostError(ex)}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BatchPostFolio()
    {
        if (_commandService is null)
        {
            ErrorMessage = "Chưa cấu hình command post dịch vụ đoàn vào Folio.";
            return;
        }

        var selected = SelectedServices.Count > 0
            ? SelectedServices.ToArray()
            : SelectedService is not null ? [SelectedService] : Array.Empty<GroupServiceDto>();
        if (selected.Length == 0)
        {
            ErrorMessage = "Hãy chọn ít nhất một dịch vụ trong bảng trước khi post hàng loạt.";
            return;
        }
        if (selected.Any(x => x.Status != "Completed"))
        {
            ErrorMessage = "Batch Folio chỉ nhận các dịch vụ đang ở trạng thái Hoàn thành.";
            return;
        }
        if (SelectedTargetFolio is null)
        {
            ErrorMessage = "Hãy chọn Folio mở thật ở bộ chọn Folio đích.";
            return;
        }

        try
        {
            IsLoading = true;
            var result = await _commandService.PostBatchToFolioAsync(
                new PostGroupServiceOrdersToFolioRequest(
                    selected.Select(x => x.ServiceId).ToArray(),
                    SelectedTargetFolio.FolioId),
                CancellationToken.None);
            InfoMessage = $"Đã chuyển {result.PostedCount} dịch vụ, tổng {result.TotalAmount:N0}, vào Folio {SelectedTargetFolio.FolioNumber}.";
            await LoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể post Folio hàng loạt: {FormatFolioPostError(ex)}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void UpdateSelectedServices(IEnumerable<GroupServiceDto> services)
    {
        SelectedServices.Clear();
        foreach (var service in services.DistinctBy(x => x.ServiceId))
            SelectedServices.Add(service);
        if (SelectedServices.Count == 1)
            SelectedService = SelectedServices[0];
        OnPropertyChanged(nameof(HasSelection));
    }

    // ═══ PRINT & EXPORT ═══

    [RelayCommand]
    private void PrintService()
    {
        var rows = FilteredServices.ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có dịch vụ thật trong bộ lọc hiện tại để in.", "In dịch vụ", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine($"Tổng dịch vụ: {rows.Count}")
            .AppendLine($"Tổng tiền: {CsvExportHelper.F(rows.Sum(x => x.TotalAmount))}")
            .AppendLine($"Còn phải thu: {CsvExportHelper.F(rows.Sum(x => x.Balance))}")
            .AppendLine()
            .AppendLine("Mã | Dịch vụ | Đoàn | Ngày | SL | Tổng tiền | Trạng thái")
            .AppendLine(new string('-', 110));

        foreach (var row in rows)
        {
            content.AppendLine($"{row.ServiceCode} | {row.ServiceName} | {row.GroupCode} | {row.ServiceDate:yyyy-MM-dd} | {row.Quantity} | {CsvExportHelper.F(row.TotalAmount)} | {row.Status}");
        }

        new PrintPreviewDialog("Dịch vụ đoàn", content.ToString())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        }.ShowDialog();
    }

    [RelayCommand]
    private async Task EmailService()
    {
        if (SelectedService is null) return;
        if (_groupReservationQueryService is null || _emailDispatchService is null)
        {
            MessageBox.Show("Chưa đăng ký đủ dịch vụ đoàn/email outbox.", "Gửi Email", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var propertyId = _contextAccessor?.Current?.Property.PropertyId.Value;
        if (!propertyId.HasValue)
        {
            MessageBox.Show("Chưa có property context để gửi email dịch vụ đoàn.", "Gửi Email", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var groups = await _groupReservationQueryService.GetGroupsAsync(propertyId.Value, CancellationToken.None);
            var group = groups.FirstOrDefault(x => x.GroupReservationId == SelectedService.GroupReservationId);
            if (group is null || string.IsNullOrWhiteSpace(group.ContactEmail))
            {
                MessageBox.Show("Đoàn chưa có email liên hệ thật.", "Gửi Email", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var queued = await _emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = group.ContactEmail.Trim(),
                Subject = $"Dịch vụ đoàn {SelectedService.GroupCode}",
                Body = $"Kính gửi {group.ContactPerson ?? group.GroupName},\n\nDịch vụ {SelectedService.ServiceName} của đoàn {group.GroupCode} đã được ghi nhận.\nNgày: {SelectedService.ServiceDate:dd/MM/yyyy}\nSố lượng: {SelectedService.Quantity}\nTổng tiền: {SelectedService.TotalAmount:N0}\n\nTrân trọng,\nASMILE BO Group Services",
                SourceType = "GroupService",
                SourceId = SelectedService.ServiceId
            }, CancellationToken.None);
            InfoMessage = $"Đã lưu email dịch vụ vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể lưu email dịch vụ: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        ExportCsv();
    }

    [RelayCommand]
    private void ExportPdf()
    {
        var rows = FilteredServices.ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có dịch vụ thật trong bộ lọc hiện tại để xuất PDF.", "Xuất PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var path = PdfExportHelper.Export(
            "Dịch vụ đoàn",
            "group_services",
            ["Mã dịch vụ", "Dịch vụ", "Đoàn", "Ngày", "SL", "Tổng tiền", "Còn phải thu", "Trạng thái"],
            rows.Select(row => (IReadOnlyList<string>)[
                row.ServiceCode,
                row.ServiceName,
                row.GroupCode,
                row.ServiceDate.ToString("yyyy-MM-dd"),
                row.Quantity.ToString(),
                CsvExportHelper.F(row.TotalAmount),
                CsvExportHelper.F(row.Balance),
                row.Status
            ]).ToList());

        if (!string.IsNullOrWhiteSpace(path))
            InfoMessage = $"Đã xuất PDF dịch vụ đoàn: {path}";
    }

    [RelayCommand]
    private void ExportCsv()
    {
        var rows = FilteredServices.ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show("Không có dịch vụ thật trong bộ lọc hiện tại để xuất.", "Xuất CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var path = CsvExportHelper.Export(
            "group_services",
            ["Mã dịch vụ", "Dịch vụ", "Danh mục", "Đoàn", "Mã đoàn", "Ngày", "Số lượng", "Đơn giá", "Tổng tiền", "Đã thu", "Còn phải thu", "Trạng thái", "Hạch toán vào"],
            rows.Select(row => (IReadOnlyList<string>)[
                row.ServiceCode,
                row.ServiceName,
                row.Category,
                row.GroupName,
                row.GroupCode,
                row.ServiceDate.ToString("yyyy-MM-dd"),
                row.Quantity.ToString(),
                CsvExportHelper.F(row.UnitPrice),
                CsvExportHelper.F(row.TotalAmount),
                CsvExportHelper.F(row.PaidAmount),
                CsvExportHelper.F(row.Balance),
                row.Status,
                row.ChargeTo
            ]).ToList());

        MessageBox.Show($"Đã xuất {rows.Count} dịch vụ thật ra:\n{path}", "Xuất CSV", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ═══ DUPLICATE ═══

    [RelayCommand]
    private async Task DuplicateServiceAsync(CancellationToken ct)
    {
        if (SelectedService is null)
        {
            InfoMessage = "Hãy chọn một dịch vụ trước khi nhân bản.";
            return;
        }

        if (_commandService is null)
        {
            InfoMessage = "Chưa cấu hình command dịch vụ đoàn thật.";
            return;
        }

        try
        {
            var copy = await _commandService.CreateAsync(new CreateGroupServiceOrderRequest(
                SelectedService.GroupReservationId,
                $"{SelectedService.ServiceName} (bản sao)",
                SelectedService.Category,
                string.IsNullOrWhiteSpace(SelectedService.SubCategory) ? null : SelectedService.SubCategory,
                string.IsNullOrWhiteSpace(SelectedService.Department) ? null : SelectedService.Department,
                string.IsNullOrWhiteSpace(SelectedService.Provider) ? null : SelectedService.Provider,
                string.IsNullOrWhiteSpace(SelectedService.Location) ? null : SelectedService.Location,
                SelectedService.ServiceDate,
                SelectedService.ServiceTime,
                SelectedService.DurationMinutes,
                SelectedService.Quantity,
                SelectedService.UnitPrice,
                SelectedService.Discount,
                SelectedService.TaxRate,
                SelectedService.ServiceChargeRate,
                SelectedService.ChargeTo,
                string.IsNullOrWhiteSpace(SelectedService.GuestName) ? null : SelectedService.GuestName,
                SelectedService.Remark), ct);

            InfoMessage = $"Đã nhân bản dịch vụ thành {copy.ServiceCode} trong dữ liệu thật.";
            await LoadAsync(ct);
            SelectedService = Services.FirstOrDefault(x => x.ServiceId == copy.GroupServiceOrderId);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể nhân bản dịch vụ đoàn: {ex.Message}";
        }
    }

    // ═══ REFRESH ═══

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void RefreshDashboard() => BuildDashboard();
}
