using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Group;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class GroupRoomDto
{
    public Guid AssignmentId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public bool IsVip { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public bool IsCheckedIn { get; set; }
    public bool IsCheckedOut { get; set; }
    public string StatusDisplay => IsCheckedOut ? "Đã trả" : IsCheckedIn ? "Đã nhận" : "Chưa nhận";
}

public sealed class AutoAssignCandidate
{
    public string RoomNumber { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string Floor { get; set; } = "";
    public int Score { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class GroupListDto
{
    public Guid GroupReservationId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? LeaderName { get; set; }
    public string? CompanyName { get; set; }
    public string? AgencyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int Nights => (DepartureDate.DayNumber - ArrivalDate.DayNumber);
    public int ExpectedRooms { get; set; }
    public int ConfirmedRooms { get; set; }
    public int TotalGuests { get; set; }
    public int CheckedInGuests { get; set; }
    public string Status { get; set; } = "Draft";
    public string Market { get; set; } = "Nội địa";
    public string Source { get; set; } = "Direct";
    public string? SalesPerson { get; set; }
    public string? Country { get; set; }
    public string? VipLevel { get; set; }
    public string? GroupType { get; set; }
    public string Routing { get; set; } = "CompanyPaysAll";
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public decimal TotalDeposit { get; set; }
    public decimal DepositRequired { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal CompanyPayAmount { get; set; }
    public decimal GuestPayAmount { get; set; }
    public decimal MasterFolioBalance { get; set; }
    public string? MasterFolioCode { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? FoNotes { get; set; }
    public string? HkNotes { get; set; }
    public string? SalesNotes { get; set; }
    public int ProgressPercent => ExpectedRooms > 0 ? (ConfirmedRooms * 100 / ExpectedRooms) : 0;
    public int GuestProgressPercent => TotalGuests > 0 ? (CheckedInGuests * 100 / TotalGuests) : 0;
    public bool HasDepositIssue => DepositRequired > 0 && TotalDeposit < DepositRequired;
    public bool HasRoomingPending => ConfirmedRooms < ExpectedRooms;
    public bool HasOverbooking => ConfirmedRooms > ExpectedRooms;
    public bool HasBalanceIssue => OutstandingBalance > 0;
    public bool IsNoShow => Status == "No Show";
    public string StatusBadge => Status switch
    {
        "Draft" => "Nháp",
        "Confirmed" => "Đã xác nhận",
        "Tentative" => "Tạm thời",
        "Waiting Deposit" => "Chờ đặt cọc",
        "Arrived" => "Đã đến",
        "Checked In" => "Đã nhận phòng",
        "Partial Check In" => "Nhận phòng 1 phần",
        "Checked Out" => "Đã trả phòng",
        "Cancelled" => "Đã hủy",
        "No Show" => "Không đến",
        "Closed" => "Đã đóng",
        _ => Status
    };
    public string StatusColor => Status switch
    {
        "Draft" => "#94A3B8",
        "Confirmed" => "#2563EB",
        "Tentative" => "#D97706",
        "Waiting Deposit" => "#EA580C",
        "Arrived" => "#7C3AED",
        "Checked In" => "#16A34A",
        "Partial Check In" => "#0891B2",
        "Checked Out" => "#4B5563",
        "Cancelled" => "#DC2626",
        "No Show" => "#F59E0B",
        "Closed" => "#1E40AF",
        _ => "#64748B"
    };
    // Row background color
    public string RowBg => Status switch
    {
        "Draft" => "#F8FAFC",
        "Cancelled" => "#FEF2F2",
        "No Show" => "#FFFBEB",
        "Closed" => "#EFF6FF",
        _ => "Transparent"
    };

    // Nested lists
    public ObservableCollection<GroupRoomDto> RoomingList { get; set; } = new();
    public ObservableCollection<AutoAssignCandidate> AssignCandidates { get; set; } = new();
    public ObservableCollection<GroupTimelineEntry> Timeline { get; set; } = new();
    public ObservableCollection<GroupAuditEntry> AuditLog { get; set; } = new();
}

public sealed class GroupTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string User { get; set; } = "";
    public string Detail { get; set; } = "";
}

public sealed class GroupAuditEntry
{
    public DateTime ChangedAt { get; set; }
    public string Field { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string ChangedBy { get; set; } = "";
}

public sealed class GroupDashboardKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string IconGlyph { get; set; } = "";
}

public sealed class GroupPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public GroupPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label;
        PageNumber = pageNumber;
        IsCurrent = isCurrent;
        IsEllipsis = isEllipsis;
    }
}

// ═══ VIEW MODEL ═══

public sealed partial class GroupReservationViewModel : ViewModelBase
{
    private readonly IGroupReservationQueryService? groupQuery;
    private readonly IGroupReservationCommandService? groupCommands;
    private readonly IGroupBillingService? groupBilling;
    private readonly IFrontOfficeExecutionContextAccessor? executionContext;
    private readonly IEmailDispatchService? emailDispatchService;

    public GroupReservationViewModel(
        IGroupReservationQueryService? groupQuery = null,
        IGroupReservationCommandService? groupCommands = null,
        IGroupBillingService? groupBilling = null,
        IFrontOfficeExecutionContextAccessor? executionContext = null,
        IEmailDispatchService? emailDispatchService = null)
    {
        this.groupQuery = groupQuery;
        this.groupCommands = groupCommands;
        this.groupBilling = groupBilling;
        this.executionContext = executionContext;
        this.emailDispatchService = emailDispatchService;
    }

    // ── Collections ──
    public ObservableCollection<GroupListDto> Groups { get; } = new();
    public ObservableCollection<GroupListDto> FilteredGroups { get; } = new();
    public ObservableCollection<GroupDashboardKpi> DashboardKpis { get; } = new();
    public ObservableCollection<GroupPaginationButton> PageButtons { get; } = new();

    // ── Selection ──
    [ObservableProperty] private GroupListDto? _selectedGroup;
    [ObservableProperty] private GroupRoomDto? _selectedRoom;
    [ObservableProperty] private int _selectedTabIndex;

    // ── Loading & State ──
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isAssignmentPanelOpen;
    [ObservableProperty] private bool _isMasterFolioOpen;
    [ObservableProperty] private bool _isFilterExpanded;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    // ── Search & Filter ──
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterMarket;
    [ObservableProperty] private string? _filterSource;
    [ObservableProperty] private string? _filterGroupType;
    [ObservableProperty] private string? _filterCountry;
    [ObservableProperty] private string? _filterVip;
    [ObservableProperty] private string? _filterPayment;

    // ── Pagination ──
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} đoàn";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // ── Financial ──
    [ObservableProperty] private decimal _companyPayPct = 80;
    [ObservableProperty] private decimal _guestPayPct = 20;
    [ObservableProperty] private decimal _fixedCompanyAmount;

    // ── Computed ──
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasSelection => SelectedGroup is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public int TotalGroups => Groups.Count;
    public int TotalRooms => Groups.Sum(x => x.ConfirmedRooms);
    public int TotalGuests => Groups.Sum(x => x.TotalGuests);

    // ── Static options ──
    public static string[] StatusFilters { get; } = ["Tất cả", "Draft", "Tentative", "Confirmed", "Waiting Deposit", "Arrived", "Checked In", "Partial Check In", "Checked Out", "Cancelled", "No Show", "Closed"];
    public static string[] MarketFilters { get; } = ["Tất cả", "Nội địa", "Quốc tế", "Đông Nam Á", "Đông Bắc Á", "Châu Âu", "Châu Mỹ"];
    public static string[] SourceFilters { get; } = ["Tất cả", "Direct", "Website", "OTA", "Travel Agent", "Corporate", "Referral"];
    public static string[] GroupTypeFilters { get; } = ["Tất cả", "Corporate", "Tour", "Conference", "Wedding", "Government", "MICE", "Sports", "Family"];
    public static string[] VipFilters { get; } = ["Tất cả", "VIP 1", "VIP 2", "VIP 3", "VVIP"];
    public static string[] PaymentFilters { get; } = ["Tất cả", "Chuyển khoản", "Tiền mặt", "Công nợ", "Thẻ tín dụng"];
    public static string[] CountryFilters { get; } = ["Tất cả", "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Singapore", "Thái Lan"];
    public static string[] RoutingOptions { get; } = ["CompanyPaysAll", "GuestPaysExtras", "SplitPercentage", "SplitFixed", "Custom"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Groups.Clear();

            if (groupQuery is not null)
            {
                if (executionContext?.Current is not { } context)
                {
                    ErrorMessage = "Chưa có ngữ cảnh property để tải dữ liệu đoàn.";
                    return;
                }

                var persistedGroups = await groupQuery.GetGroupsAsync(context.Property.PropertyId.Value, ct);
                foreach (var group in persistedGroups)
                    Groups.Add(MapPersistedGroup(group));

                BuildDashboard();
                ApplyFilters();
                return;
            }

            ErrorMessage = "Chưa đăng ký GroupReservationQueryService. Không hiển thị dữ liệu mẫu.";
            BuildDashboard();
            ApplyFilters();
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static GroupListDto MapPersistedGroup(GroupReservationSummaryDto group)
    {
        var item = new GroupListDto
        {
            GroupReservationId = group.GroupReservationId,
            GroupCode = group.GroupCode,
            GroupName = group.GroupName,
            CompanyName = group.CompanyName,
            AgencyName = group.AgencyName,
            ContactPerson = group.ContactPerson,
            ContactPhone = group.ContactPhone,
            ContactEmail = group.ContactEmail,
            ArrivalDate = group.ArrivalDate,
            DepartureDate = group.DepartureDate,
            ExpectedRooms = group.ExpectedRooms,
            ConfirmedRooms = group.ConfirmedRooms,
            TotalGuests = group.TotalAdults + group.TotalChildren,
            CheckedInGuests = group.CheckedInGuests,
            Status = group.Status switch
            {
                GroupReservationStatus.CheckedIn => "Checked In",
                GroupReservationStatus.CheckedOut => "Checked Out",
                _ => group.Status.ToString()
            },
            Source = group.Source ?? "Direct",
            SalesPerson = group.SalesManager,
            Market = group.MarketSegment ?? "Nội địa",
            Routing = group.Routing.ToString(),
            TotalDeposit = group.TotalDeposit,
            TotalRevenue = group.TotalRevenue,
            OutstandingBalance = group.OutstandingBalance,
            MasterFolioBalance = group.OutstandingBalance,
            Notes = "Dữ liệu EF theo property hiện tại."
        };

        foreach (var room in group.RoomingList)
        {
            item.RoomingList.Add(new GroupRoomDto
            {
                AssignmentId = room.AssignmentId,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                GuestName = room.GuestName,
                Adults = room.Adults,
                Children = room.Children,
                IsVip = room.IsVip,
                IsCheckedIn = room.IsCheckedIn,
                IsCheckedOut = room.IsCheckedOut
            });
        }

        return item;
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Tổng đoàn", Value = Groups.Count.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Đang ở", Value = Groups.Count(g => g.Status is "Checked In" or "Partial Check In").ToString(), Color = "#16A34A", BgColor = "#F0FDF4", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Sắp đến", Value = Groups.Count(g => g.Status is "Confirmed" or "Tentative" && g.ArrivalDate <= today.AddDays(3)).ToString(), Color = "#2563EB", BgColor = "#EFF6FF", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Hôm nay đến", Value = Groups.Count(g => g.ArrivalDate == today).ToString(), Color = "#0891B2", BgColor = "#ECFEFF", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Hôm nay đi", Value = Groups.Count(g => g.DepartureDate == today).ToString(), Color = "#D97706", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Chờ đặt cọc", Value = Groups.Count(g => g.HasDepositIssue).ToString(), Color = "#EA580C", BgColor = "#FFF7ED", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Chưa phân phòng", Value = Groups.Count(g => g.HasRoomingPending).ToString(), Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Còn công nợ", Value = Groups.Count(g => g.HasBalanceIssue).ToString(), Color = "#DC2626", BgColor = "#FEF2F2", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "No Show", Value = Groups.Count(g => g.IsNoShow).ToString(), Color = "#94A3B8", BgColor = "#F8FAFC", IconGlyph = "" });
        DashboardKpis.Add(new GroupDashboardKpi { Label = "Doanh thu", Value = $"{Groups.Sum(g => g.TotalRevenue) / 1_000_000_000m:F1}B", Color = "#7C3AED", BgColor = "#F5F3FF", IconGlyph = "" });
    }

    // ═══ FILTERS ═══

    [RelayCommand]
    private void ApplyFilters()
    {
        var q = Groups.AsEnumerable();
        var search = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(g => g.GroupName.ToLower().Contains(search) || g.GroupCode.ToLower().Contains(search) || (g.CompanyName?.ToLower().Contains(search) ?? false) || (g.AgencyName?.ToLower().Contains(search) ?? false) || (g.LeaderName?.ToLower().Contains(search) ?? false));
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả")
            q = q.Where(g => g.Status == FilterStatus);
        if (!string.IsNullOrEmpty(FilterMarket) && FilterMarket != "Tất cả")
            q = q.Where(g => g.Market == FilterMarket);
        if (!string.IsNullOrEmpty(FilterSource) && FilterSource != "Tất cả")
            q = q.Where(g => g.Source == FilterSource);
        if (!string.IsNullOrEmpty(FilterGroupType) && FilterGroupType != "Tất cả")
            q = q.Where(g => g.GroupType == FilterGroupType);
        if (!string.IsNullOrEmpty(FilterCountry) && FilterCountry != "Tất cả")
            q = q.Where(g => g.Country == FilterCountry);
        if (!string.IsNullOrEmpty(FilterVip) && FilterVip != "Tất cả")
            q = q.Where(g => g.VipLevel == FilterVip);
        if (!string.IsNullOrEmpty(FilterPayment) && FilterPayment != "Tất cả")
            q = q.Where(g => g.PaymentMethod == FilterPayment);

        var list = q.ToList();
        TotalFilteredCount = list.Count;
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredGroups.Clear();
        foreach (var g in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            FilteredGroups.Add(g);

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
        FilterMarket = null;
        FilterSource = null;
        FilterGroupType = null;
        FilterCountry = null;
        FilterVip = null;
        FilterPayment = null;
        IsFilterExpanded = false;
        ApplyFilters();
    }

    [RelayCommand]
    private void ToggleFilter() => IsFilterExpanded = !IsFilterExpanded;

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterMarketChanged(string? value) => ApplyFilters();
    partial void OnFilterSourceChanged(string? value) => ApplyFilters();
    partial void OnFilterGroupTypeChanged(string? value) => ApplyFilters();
    partial void OnFilterCountryChanged(string? value) => ApplyFilters();
    partial void OnFilterVipChanged(string? value) => ApplyFilters();
    partial void OnFilterPaymentChanged(string? value) => ApplyFilters();

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

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyFilters();
        OnPropertyChanged(nameof(PageSize));
    }

    // ═══ SELECTION ═══

    [RelayCommand]
    private void SelectGroup(GroupListDto? g)
    {
        SelectedGroup = g;
        IsDetailOpen = g is not null;
        SelectedTabIndex = 0;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        IsAssignmentPanelOpen = false;
        IsMasterFolioOpen = false;
        SelectedGroup = null;
    }

    partial void OnSelectedGroupChanged(GroupListDto? value)
    {
        IsDetailOpen = value is not null;
        SelectedTabIndex = 0;
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages;
        var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages;
        if (total <= 5)
        {
            pages = Enumerable.Range(1, total).Cast<int?>();
        }
        else if (current <= 3)
        {
            pages = new int?[] { 1, 2, 3, 4, 5, null, total };
        }
        else if (current >= total - 2)
        {
            pages = new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total };
        }
        else
        {
            pages = new int?[] { 1, null, current - 1, current, current + 1, null, total };
        }

        foreach (var page in pages)
        {
            PageButtons.Add(page is null
                ? new GroupPaginationButton("…", null, false, true)
                : new GroupPaginationButton(page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Value, page.Value == current));
        }
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    // ═══ WORKFLOW COMMANDS — open existing windows ═══

    [RelayCommand]
    private static void OpenCreateGroup()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.CreateGroupWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new Views.CreateGroupWindow { Owner = System.Windows.Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner }.Show();
    }

    [RelayCommand]
    private static void OpenGroupReservation()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.GroupReservationWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new Views.GroupReservationWindow().Show();
    }

    [RelayCommand]
    private static void OpenGroupCheckIn()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.GroupCheckInWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new Views.GroupCheckInWindow().Show();
    }

    [RelayCommand]
    private static void OpenGroupCheckOut()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.GroupCheckoutWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new Views.GroupCheckoutWindow().Show();
    }

    [RelayCommand]
    private static void OpenRoomingList()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.GroupRoomingListWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal; existing.Activate(); existing.Focus(); return; }
        new Views.GroupRoomingListWindow().Show();
    }

    [RelayCommand]
    private static void OpenFolioWindow()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.FolioWorkspaceWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.FolioWorkspaceWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private void OpenDepositWindow()
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi ghi nhận đặt cọc.";
            return;
        }

        var window = new Views.GroupDepositWindow(
            SelectedGroup.GroupReservationId,
            SelectedGroup.GroupCode,
            SelectedGroup.GroupName)
        { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private static void OpenInvoiceWindow()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.InvoiceManagementWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.InvoiceManagementWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private async Task OpenReportWindowAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi xem báo cáo.";
            return;
        }

        if (groupBilling is null)
        {
            InfoMessage = "Chưa đăng ký GroupBillingService thật; báo cáo bị chặn.";
            return;
        }

        try
        {
            var summary = await groupBilling.GetBillingSummaryAsync(SelectedGroup.GroupReservationId, ct);
            System.Windows.MessageBox.Show(
                $"Đoàn: {summary.GroupName}\n" +
                $"Tổng phát sinh: {summary.TotalCharges:N0}\n" +
                $"Đã thanh toán: {summary.TotalPayments:N0}\n" +
                $"Đặt cọc: {summary.TotalDeposits:N0}\n" +
                $"Còn phải thu: {summary.Outstanding:N0}\n" +
                $"Trạng thái cân đối: {(summary.IsBalanced ? "Đã cân đối" : "Còn công nợ")}",
                "Báo cáo đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể tải báo cáo đoàn thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private static void OpenGuestList() => OpenRoomingList();

    [RelayCommand]
    private static void OpenServiceList()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<Views.GroupServiceWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new Views.GroupServiceWindow { Owner = System.Windows.Application.Current.MainWindow }.Show();
    }

    [RelayCommand]
    private async Task OpenEmailWindow()
    {
        var group = SelectedGroup;
        if (group is null) return;
        if (string.IsNullOrWhiteSpace(group.ContactEmail))
        {
            System.Windows.MessageBox.Show("Đoàn chưa có email liên hệ.", "Email", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (emailDispatchService is null)
        {
            System.Windows.MessageBox.Show("Chưa đăng ký email outbox.", "Email", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var queued = await emailDispatchService.QueueAsync(new EmailDispatchRequestDto
            {
                Recipient = group.ContactEmail.Trim(),
                Subject = $"Xác nhận đoàn {group.GroupCode}",
                Body = $"Kính gửi {group.ContactPerson ?? group.GroupName},\n\nThông tin đoàn {group.GroupCode} - {group.GroupName}: {group.ExpectedRooms} phòng, {group.TotalGuests} khách.\n\nTrân trọng,\nASMILE BO Front Office",
                SourceType = "GroupReservation",
                SourceId = group.GroupReservationId
            }, CancellationToken.None);
            InfoMessage = $"Đã lưu email đoàn vào hàng đợi ({queued.Id:N}); gửi khi SMTP được bật.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể lưu email đoàn: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenPrintWindow()
    {
        var rows = FilteredGroups.ToList();
        if (rows.Count == 0)
        {
            System.Windows.MessageBox.Show("Không có đoàn thật trong bộ lọc hiện tại để in.", "In đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine($"Tổng đoàn: {rows.Count}")
            .AppendLine($"Tổng khách: {rows.Sum(x => x.TotalGuests)}")
            .AppendLine($"Tổng doanh thu: {CsvExportHelper.F(rows.Sum(x => x.TotalRevenue))}")
            .AppendLine($"Còn phải thu: {CsvExportHelper.F(rows.Sum(x => x.OutstandingBalance))}")
            .AppendLine()
            .AppendLine("Mã đoàn | Tên đoàn | Đến | Đi | Phòng | Khách | Trạng thái | Doanh thu | Còn phải thu")
            .AppendLine(new string('-', 125));

        foreach (var row in rows)
        {
            content.AppendLine($"{row.GroupCode} | {row.GroupName} | {row.ArrivalDate:yyyy-MM-dd} | {row.DepartureDate:yyyy-MM-dd} | {row.ConfirmedRooms}/{row.ExpectedRooms} | {row.CheckedInGuests}/{row.TotalGuests} | {row.StatusBadge} | {CsvExportHelper.F(row.TotalRevenue)} | {CsvExportHelper.F(row.OutstandingBalance)}");
        }

        new PrintPreviewDialog("Danh sách đoàn", content.ToString())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        }.ShowDialog();
    }

    [RelayCommand]
    private async Task DuplicateGroupAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi nhân bản.";
            return;
        }

        if (groupCommands is null || executionContext?.Current?.Property.PropertyId.Value is not Guid propertyId)
        {
            InfoMessage = "Chưa có ngữ cảnh cơ sở hoặc command đoàn thật; thao tác bị chặn.";
            return;
        }

        if (!Enum.TryParse<ChargeRouting>(SelectedGroup.Routing, true, out var routing))
            routing = ChargeRouting.CompanyPaysAll;

        try
        {
            var duplicate = await groupCommands.CreateAsync(new CreateGroupRequest(
                propertyId,
                null,
                $"{SelectedGroup.GroupName} (bản sao)",
                SelectedGroup.ArrivalDate,
                SelectedGroup.DepartureDate,
                SelectedGroup.ExpectedRooms,
                SelectedGroup.TotalGuests,
                0,
                SelectedGroup.CompanyName,
                SelectedGroup.AgencyName,
                SelectedGroup.ContactPerson,
                SelectedGroup.ContactPhone,
                SelectedGroup.ContactEmail,
                SelectedGroup.Source,
                SelectedGroup.SalesPerson,
                SelectedGroup.Market,
                !string.IsNullOrWhiteSpace(SelectedGroup.VipLevel),
                routing,
                SelectedGroup.Notes,
                SelectedGroup.InternalNotes), ct);

            InfoMessage = $"Đã nhân bản đoàn thành {duplicate.GroupCode}. Đoàn mới ở trạng thái nháp để tiếp tục cấu hình phòng/dịch vụ.";
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể nhân bản đoàn: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConfirmGroupAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi xác nhận.";
            return;
        }

        if (groupCommands is null)
        {
            InfoMessage = "Chưa đăng ký GroupReservationCommandService thật; thao tác bị chặn.";
            return;
        }

        try
        {
            await groupCommands.ConfirmAsync(SelectedGroup.GroupReservationId, ct);
            InfoMessage = $"Đã xác nhận đoàn {SelectedGroup.GroupCode} trong dữ liệu thật.";
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể xác nhận đoàn. Dữ liệu chưa được ghi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CloseGroupAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi đóng.";
            return;
        }

        if (groupCommands is null)
        {
            InfoMessage = "Chưa đăng ký GroupReservationCommandService thật; thao tác bị chặn.";
            return;
        }

        try
        {
            await groupCommands.CloseAsync(SelectedGroup.GroupReservationId, ct);
            InfoMessage = $"Đã đóng đoàn {SelectedGroup.GroupCode} trong dữ liệu thật.";
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể đóng đoàn. Chỉ được đóng đoàn đã check-out hoặc đã hủy: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteDraftGroupAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi xóa.";
            return;
        }

        if (groupCommands is null)
        {
            InfoMessage = "Chưa đăng ký GroupReservationCommandService thật; thao tác bị chặn.";
            return;
        }

        if (!string.Equals(SelectedGroup.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(SelectedGroup.Status, "Nháp", StringComparison.OrdinalIgnoreCase))
        {
            InfoMessage = "Chỉ được xóa đoàn đang ở trạng thái nháp.";
            return;
        }

        try
        {
            await groupCommands.DeleteDraftAsync(SelectedGroup.GroupReservationId, ct);
            InfoMessage = $"Đã xóa đoàn nháp {SelectedGroup.GroupCode} khỏi dữ liệu thật.";
            SelectedGroup = null;
            IsDetailOpen = false;
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            InfoMessage = $"Không thể xóa đoàn nháp: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshDashboard() => BuildDashboard();

    // ═══ EXISTING COMMANDS ═══

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void ToggleAssignmentPanel() => IsAssignmentPanelOpen = !IsAssignmentPanelOpen;
    [RelayCommand] private void ToggleMasterFolio() => IsMasterFolioOpen = !IsMasterFolioOpen;

    [RelayCommand]
    private void RunAutoAssignment()
    {
        OpenRealRoomAssignmentWindow();
    }

    [RelayCommand]
    private void AcceptAssignment(AutoAssignCandidate? candidate)
    {
        OpenRealRoomAssignmentWindow();
    }

    [RelayCommand]
    private void BulkCheckIn()
    {
        OpenGroupCheckInWorkflow();
    }

    [RelayCommand]
    private void BulkCheckOut()
    {
        OpenGroupCheckOutWorkflow();
    }

    private void OpenGroupCheckInWorkflow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<Views.GroupCheckInWindow>()
            .FirstOrDefault(w => w.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        new Views.GroupCheckInWindow
        {
            Owner = System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
        InfoMessage = "Đã mở workflow check-in đoàn thật.";
    }

    private void OpenGroupCheckOutWorkflow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<Views.GroupCheckoutWindow>()
            .FirstOrDefault(w => w.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        new Views.GroupCheckoutWindow
        {
            Owner = System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
        InfoMessage = "Đã mở workflow check-out đoàn thật.";
    }

    [RelayCommand]
    private void BulkAssignRooms()
    {
        OpenRealRoomAssignmentWindow();
    }

    private void OpenRealRoomAssignmentWindow()
    {
        if (SelectedGroup is null)
        {
            InfoMessage = "Hãy chọn một đoàn trước khi phân phòng.";
            return;
        }

        var existing = System.Windows.Application.Current.Windows
            .OfType<Views.GroupRoomAssignmentWindow>()
            .FirstOrDefault(w => w.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        new Views.GroupRoomAssignmentWindow(SelectedGroup.GroupReservationId)
        {
            Owner = System.Windows.Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
        InfoMessage = $"Đã mở phân phòng thật cho đoàn {SelectedGroup.GroupCode}.";
    }

    [RelayCommand]
    private void TransferToMasterFolio()
    {
        OpenFolioWindow();
        InfoMessage = "Đã mở Folio thật để chọn khoản và thực hiện chuyển vào Master Folio.";
    }

    [RelayCommand]
    private void SettleMasterFolio()
    {
        OpenFolioWindow();
        InfoMessage = "Đã mở Folio thật để kiểm tra số dư và thực hiện quyết toán.";
    }

    [RelayCommand]
    private async Task ApplyCompanyBillingAsync(CancellationToken ct)
    {
        if (SelectedGroup is null)
        {
            ErrorMessage = "Hãy chọn một đoàn trước khi lưu cấu hình billing.";
            return;
        }
        if (groupBilling is null)
        {
            ErrorMessage = "Chưa đăng ký GroupBillingService thật; không thể lưu cấu hình billing.";
            return;
        }

        var routing = CompanyPayPct > 0m && GuestPayPct > 0m
            ? ChargeRouting.SplitPercentage
            : CompanyPayPct > 0m
                ? ChargeRouting.CompanyPaysAll
                : ChargeRouting.GuestPaysExtras;
        decimal? split = routing == ChargeRouting.SplitPercentage ? CompanyPayPct : null;
        try
        {
            await groupBilling.ConfigureBillingAsync(
                new GroupBillingSetup(
                    SelectedGroup.GroupReservationId,
                    UseMasterFolio: true,
                    DefaultRouting: routing,
                    AllowIndividualFolios: GuestPayPct > 0m,
                    split,
                    MasterFolioId: null,
                    DepositRequired: SelectedGroup.DepositRequired > 0m),
                ct);
            InfoMessage = $"Đã lưu cấu hình billing thật cho đoàn {SelectedGroup.GroupCode}: {routing}.";
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể lưu cấu hình billing thật: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RecordCompanyPayment()
    {
        OpenDepositWindow();
        InfoMessage = "Đã mở sổ đặt cọc đoàn thật để ghi nhận thanh toán công ty.";
    }

    [RelayCommand]
    private void RecordGuestPayment()
    {
        OpenDepositWindow();
        InfoMessage = "Đã mở sổ đặt cọc đoàn thật để ghi nhận thanh toán khách.";
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var rows = FilteredGroups.ToList();
        if (rows.Count == 0)
        {
            System.Windows.MessageBox.Show("Không có đoàn thật trong bộ lọc hiện tại để xuất.", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var path = CsvExportHelper.Export(
            "group_reservations",
            ["Mã đoàn", "Tên đoàn", "Trưởng đoàn", "Công ty", "Đại lý", "Ngày đến", "Ngày đi", "Số đêm", "Phòng dự kiến", "Phòng xác nhận", "Tổng khách", "Đã check-in", "Trạng thái", "Đặt cọc", "Doanh thu", "Còn phải thu", "Phương thức thanh toán"],
            rows.Select(row => (IReadOnlyList<string>)[
                row.GroupCode,
                row.GroupName,
                row.LeaderName ?? string.Empty,
                row.CompanyName ?? string.Empty,
                row.AgencyName ?? string.Empty,
                row.ArrivalDate.ToString("yyyy-MM-dd"),
                row.DepartureDate.ToString("yyyy-MM-dd"),
                row.Nights.ToString(),
                row.ExpectedRooms.ToString(),
                row.ConfirmedRooms.ToString(),
                row.TotalGuests.ToString(),
                row.CheckedInGuests.ToString(),
                row.StatusBadge,
                CsvExportHelper.F(row.TotalDeposit),
                CsvExportHelper.F(row.TotalRevenue),
                CsvExportHelper.F(row.OutstandingBalance),
                row.PaymentMethod
            ]).ToList());

        System.Windows.MessageBox.Show($"Đã xuất {rows.Count} đoàn thật ra:\n{path}", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
