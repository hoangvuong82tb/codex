using System.Collections.ObjectModel;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class WorkforceStaffDto
{
    public int PersonId { get; set; }
    public string StaffCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "Housekeeper";
    public string RoleLabel => Role == "Supervisor" ? "Supervisor" : "Housekeeper";
    public string RoleColor => Role == "Supervisor" ? "#7C3AED" : "#2563EB";
    public string Shift { get; set; } = "Chưa phân ca";
    public string ShiftLabel => Shift switch { "Sáng" => "Ca sáng (6h-14h)", "Chiều" => "Ca chiều (14h-22h)", "Đêm" => "Ca đêm (22h-6h)", _ => Shift };
    public string ShiftShort => Shift;
    public string Building { get; set; } = "A";
    public string Floor { get; set; } = "1";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Supervisor { get; set; }
    public bool HasLinkedOperationalAccount { get; set; }
    public string OperationalAccountStatus => HasLinkedOperationalAccount
        ? "Đã liên kết tài khoản tác nghiệp"
        : "Chưa liên kết AppUser";

    public string Status { get; set; } = "Chưa xác định";
    public string StatusLabel => Status switch { "Online" => "Đang làm", "Offline" => "Nghỉ", "Leave" => "Nghỉ phép", "Overload" => "Quá tải", _ => Status };
    public string StatusColor => Status switch { "Online" => "#16A34A", "Offline" => "#94A3B8", "Leave" => "#D97706", "Overload" => "#DC2626", _ => "#64748B" };
    public string StatusBg => Status switch { "Online" => "#DCFCE7", "Offline" => "#F1F5F9", "Leave" => "#FFF7ED", "Overload" => "#FEE2E2", _ => "#F8FAFC" };

    public int TotalAssigned { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int PendingInspection { get; set; }
    public int VipRooms { get; set; }
    public int RushRooms { get; set; }
    public int ProgressPercent => TotalAssigned > 0 ? (int)((double)Completed / TotalAssigned * 100) : 0;
    public string ProgressDisplay => $"{Completed}/{TotalAssigned}";
    public bool IsOverload => TotalAssigned > 15;

    public double KpiScore { get; set; }
    public string KpiDisplay => $"{KpiScore:F1}";
    public string KpiGrade => KpiScore switch { >= 90 => "A", >= 80 => "B", >= 70 => "C", >= 60 => "D", _ => "F" };
    public string KpiGradeColor => KpiScore switch { >= 90 => "#16A34A", >= 80 => "#2563EB", >= 70 => "#D97706", >= 60 => "#EA580C", _ => "#DC2626" };

    public int InspectionPass { get; set; }
    public int InspectionFail { get; set; }
    public double AvgCleaningMinutes { get; set; }
    public int SlaMet { get; set; }
    public int SlaMissed { get; set; }

    public List<AssignmentDto> Assignments { get; set; } = new();
    public List<WorkforceTimelineEntry> Timeline { get; set; } = new();
    public List<WorkforceAuditEntry> AuditLog { get; set; } = new();
}

public sealed class AssignmentDto
{
    public string RoomNumber { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public string PriorityColor => Priority switch { "VIP" => "#D97706", "Rush" => "#DC2626", "Normal" => "#64748B", _ => "#64748B" };
    public bool IsVip => Priority == "VIP";
    public bool IsRush => Priority == "Rush";
    public string Status { get; set; } = "Assigned";
    public string StatusLabel => Status switch { "Assigned" => "Đã giao", "InProgress" => "Đang làm", "Completed" => "Hoàn thành", "PendingInspection" => "Chờ kiểm tra", "Inspected" => "Đã kiểm tra", _ => Status };
    public string StatusColor => Status switch { "Assigned" => "#94A3B8", "InProgress" => "#2563EB", "Completed" => "#16A34A", "PendingInspection" => "#D97706", "Inspected" => "#7C3AED", _ => "#64748B" };
    public int EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public string TimeDisplay => ActualMinutes.HasValue ? $"{ActualMinutes}ph" : "Chưa có SLA";
}

public sealed class WorkforceKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class WorkforceTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class WorkforceTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class WorkforceAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class WorkforcePaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }
    public WorkforcePaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    { Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis; }
}

// ═══ VIEW MODEL ═══

public sealed partial class WorkforceViewModel : ViewModelBase
{
    private readonly IWorkforceQueryService? workforceQueryService;
    private readonly IWorkforceCommandService? workforceCommandService;
    private readonly IHousekeepingBoardService? housekeepingBoardService;
    private readonly IHousekeepingWorkflowService? housekeepingWorkflowService;
    private readonly IFrontOfficeExecutionContextAccessor? executionContext;

    public WorkforceViewModel()
    {
    }

    public WorkforceViewModel(
        IWorkforceQueryService workforceQueryService,
        IWorkforceCommandService workforceCommandService,
        IHousekeepingBoardService? housekeepingBoardService = null,
        IHousekeepingWorkflowService? housekeepingWorkflowService = null,
        IFrontOfficeExecutionContextAccessor? executionContext = null)
    {
        this.workforceQueryService = workforceQueryService;
        this.workforceCommandService = workforceCommandService;
        this.housekeepingBoardService = housekeepingBoardService;
        this.housekeepingWorkflowService = housekeepingWorkflowService;
        this.executionContext = executionContext;
    }

    public ObservableCollection<WorkforceStaffDto> Staff { get; } = new();
    public ObservableCollection<WorkforceStaffDto> FilteredStaff { get; } = new();
    public ObservableCollection<WorkforceKpi> DashboardKpis { get; } = new();
    public ObservableCollection<WorkforceTimelineEntry> StaffTimeline { get; } = new();
    public ObservableCollection<WorkforceAuditEntry> StaffAuditLog { get; } = new();
    public ObservableCollection<WorkforceTreeItem> TreeItems { get; } = new();
    public ObservableCollection<WorkforcePaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private WorkforceStaffDto? _selectedStaff;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private WorkforceTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterShift;
    [ObservableProperty] private string? _filterBuilding;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterRole;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private bool _filterOverloadOnly;
    [ObservableProperty] private bool _filterHighPerfOnly;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 NV" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} NV";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedStaff is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    // Form state
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;
    public bool IsAssignForm => FormMode == "Assign";
    public bool IsShiftForm => FormMode == "Shift";
    public bool IsAreaForm => FormMode == "Area";
    public bool IsFormClosed => !IsFormOpen;

    [ObservableProperty] private string _assignStaff = string.Empty;
    [ObservableProperty] private string _assignBuilding = "A";
    [ObservableProperty] private string _assignFloor = "1";
    [ObservableProperty] private string _assignRooms = string.Empty;
    [ObservableProperty] private string _assignRemark = string.Empty;

    [ObservableProperty] private string _shiftStaff = string.Empty;
    [ObservableProperty] private string _shiftCurrent = string.Empty;
    [ObservableProperty] private string _shiftNew = "Sáng";
    [ObservableProperty] private string _shiftReason = string.Empty;

    [ObservableProperty] private string _areaStaff = string.Empty;
    [ObservableProperty] private string _areaCurrent = string.Empty;
    [ObservableProperty] private string _areaNew = string.Empty;
    [ObservableProperty] private string _areaReason = string.Empty;

    public int TotalStaff => Staff.Count;
    public int OnlineCount => Staff.Count(s => s.Status == "Online");
    public int OfflineCount => Staff.Count(s => s.Status == "Offline");
    public int LeaveCount => Staff.Count(s => s.Status == "Leave");
    public int MorningShiftCount => Staff.Count(s => s.Shift == "Sáng");
    public int AfternoonShiftCount => Staff.Count(s => s.Shift == "Chiều");
    public int NightShiftCount => Staff.Count(s => s.Shift == "Đêm");
    public int SupervisorCount => Staff.Count(s => s.Role == "Supervisor");
    public int HousekeeperCount => Staff.Count(s => s.Role == "Housekeeper");
    public int TotalCompleted => Staff.Sum(s => s.Completed);
    public int TotalInProgress => Staff.Sum(s => s.InProgress);
    public double AvgKpi => Staff.Any() ? Staff.Average(s => s.KpiScore) : 0;
    public string AvgKpiDisplay => $"{AvgKpi:F1}";
    public int TodayCompleted => Staff.Sum(s => s.Completed);
    public int OverloadCount => Staff.Count(s => s.IsOverload);
    public int HighPerfCount => Staff.Count(s => s.KpiScore >= 85);

    public static string[] ShiftFilters { get; } = ["Tất cả", "Sáng", "Chiều", "Đêm"];
    public static string[] BuildingFilters { get; } = ["Tất cả", "A", "B", "C"];
    public static string[] RoleFilters { get; } = ["Tất cả", "Housekeeper", "Supervisor"];
    public static string[] StatusFilters { get; } = ["Tất cả", "Online", "Offline", "Leave", "Overload"];
    public string[] FloorFilters { get; private set; } = [];
    public static string[] AssignBuildings { get; } = ["A", "B", "C"];
    public static string[] ShiftOptions { get; } = ["Chưa phân ca", "Sáng", "Chiều", "Đêm"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Staff.Clear();
            if (workforceQueryService is null)
            {
                ErrorMessage = "Chưa có Workforce query service; không thể tải dữ liệu thật.";
                BuildDashboard(); BuildTree(); ApplyFilters();
                return;
            }

            var snapshots = await workforceQueryService.QueryAsync(ct);
            foreach (var snapshot in snapshots)
                Staff.Add(MapSnapshot(snapshot));

            FloorFilters = Staff.Select(staff => staff.Floor)
                .Where(floor => !string.IsNullOrWhiteSpace(floor))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(floor => floor, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            BuildDashboard();
            BuildTree();
            ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static WorkforceStaffDto MapSnapshot(WorkforceStaffSnapshot snapshot) => new()
    {
        PersonId = snapshot.PersonId,
        StaffCode = snapshot.StaffCode,
        FullName = snapshot.FullName,
        Role = snapshot.Role,
        Shift = snapshot.Shift,
        Building = string.IsNullOrWhiteSpace(snapshot.Building) ? "\u2014" : snapshot.Building,
        Floor = string.IsNullOrWhiteSpace(snapshot.Floor) ? "\u2014" : snapshot.Floor,
        Phone = snapshot.Phone,
        Email = snapshot.Email,
        Supervisor = snapshot.Supervisor,
        HasLinkedOperationalAccount = snapshot.HasLinkedOperationalAccount,
        Status = snapshot.Status,
        TotalAssigned = snapshot.TotalAssigned,
        Completed = snapshot.Completed,
        InProgress = snapshot.InProgress,
        PendingInspection = snapshot.PendingInspection,
        VipRooms = snapshot.VipRooms,
        RushRooms = snapshot.RushRooms,
        AvgCleaningMinutes = snapshot.AvgCleaningMinutes,
        Assignments = snapshot.Assignments?.Select(assignment => new AssignmentDto
        {
            RoomNumber = assignment.RoomNumber,
            Priority = assignment.Priority,
            Status = assignment.Status,
            ActualMinutes = assignment.ActualMinutes
        }).ToList() ?? new(),
        InspectionPass = snapshot.InspectionPass,
        InspectionFail = snapshot.InspectionFail
    };

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new WorkforceKpi { Label = "Tổng nhân viên", Value = TotalStaff.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Đang làm việc", Value = OnlineCount.ToString(), Color = "#16A34A", BgColor = "#DCFCE7", FilterKey = "Status", FilterValue = "Online" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Đang nghỉ", Value = OfflineCount.ToString(), Color = "#94A3B8", BgColor = "#F1F5F9", FilterKey = "Status", FilterValue = "Offline" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Nghỉ phép", Value = LeaveCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", FilterKey = "Status", FilterValue = "Leave" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Ca sáng", Value = MorningShiftCount.ToString(), Color = "#F59E0B", BgColor = "#FFFBEB", FilterKey = "Shift", FilterValue = "Sáng" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Ca chiều", Value = AfternoonShiftCount.ToString(), Color = "#EA580C", BgColor = "#FFF7ED", FilterKey = "Shift", FilterValue = "Chiều" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Ca đêm", Value = NightShiftCount.ToString(), Color = "#4F46E5", BgColor = "#EEF2FF", FilterKey = "Shift", FilterValue = "Đêm" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Supervisor", Value = SupervisorCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Housekeeper", Value = HousekeeperCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Phòng đã hoàn thành", Value = TotalCompleted.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Phòng đang làm", Value = TotalInProgress.ToString(), Color = "#D97706", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Hiệu suất TB", Value = AvgKpiDisplay, Color = "#4F46E5", BgColor = "#EEF2FF" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Quá tải", Value = OverloadCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Overload", FilterValue = "true" });
        DashboardKpis.Add(new WorkforceKpi { Label = "Hiệu suất cao", Value = HighPerfCount.ToString(), Color = "#16A34A", BgColor = "#DCFCE7", FilterKey = "HighPerf", FilterValue = "true" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new WorkforceTreeItem { Label = "Tất cả", Count = TotalStaff, IsSelected = true });
        TreeItems.Add(new WorkforceTreeItem { Label = "Supervisor", Count = SupervisorCount, FilterKey = "Role", FilterValue = "Supervisor" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Housekeeper", Count = HousekeeperCount, FilterKey = "Role", FilterValue = "Housekeeper" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Ca sáng", Count = MorningShiftCount, FilterKey = "Shift", FilterValue = "Sáng" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Ca chiều", Count = AfternoonShiftCount, FilterKey = "Shift", FilterValue = "Chiều" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Ca đêm", Count = NightShiftCount, FilterKey = "Shift", FilterValue = "Đêm" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Đang làm", Count = OnlineCount, FilterKey = "Status", FilterValue = "Online" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Nghỉ", Count = OfflineCount, FilterKey = "Status", FilterValue = "Offline" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Quá tải", Count = OverloadCount, FilterKey = "Overload", FilterValue = "true" });
        TreeItems.Add(new WorkforceTreeItem { Label = "Hiệu suất cao", Count = HighPerfCount, FilterKey = "HighPerf", FilterValue = "true" });
    }

    private void BuildTimeline(WorkforceStaffDto s) { StaffTimeline.Clear(); foreach (var e in s.Timeline.OrderBy(t => t.Timestamp)) StaffTimeline.Add(e); }
    private void BuildAuditLog(WorkforceStaffDto s) { StaffAuditLog.Clear(); foreach (var e in s.AuditLog.OrderBy(t => t.Timestamp)) StaffAuditLog.Add(e); }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        var q = Staff.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.FullName.ToLower().Contains(s) || r.StaffCode.ToLower().Contains(s) || (r.Supervisor?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterShift) && FilterShift != "Tất cả") q = q.Where(r => r.Shift == FilterShift);
        if (!string.IsNullOrEmpty(FilterBuilding) && FilterBuilding != "Tất cả") q = q.Where(r => r.Building == FilterBuilding);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterRole) && FilterRole != "Tất cả") q = q.Where(r => r.Role == FilterRole);
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.Status == FilterStatus);
        if (FilterOverloadOnly) q = q.Where(r => r.IsOverload);
        if (FilterHighPerfOnly) q = q.Where(r => r.KpiScore >= 85);
        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch
            {
                "Role" => q.Where(r => r.Role == v), "Shift" => q.Where(r => r.Shift == v),
                "Status" => q.Where(r => r.Status == v), "Overload" => q.Where(r => r.IsOverload),
                "HighPerf" => q.Where(r => r.KpiScore >= 85), _ => q
            };
        }
        var list = q.OrderBy(r => r.Role == "Supervisor" ? 0 : 1).ThenBy(r => r.StaffCode).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredStaff.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredStaff.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterShift = null; FilterBuilding = null; FilterFloor = null;
        FilterRole = null; FilterStatus = null; FilterOverloadOnly = false; FilterHighPerfOnly = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void SelectTreeItem(WorkforceTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) ti.IsSelected = false;
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }

    [RelayCommand] private void ApplyKpiFilter(WorkforceKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Status") FilterStatus = kpi.FilterValue;
        else if (kpi.FilterKey == "Shift") FilterShift = kpi.FilterValue;
        else if (kpi.FilterKey == "Overload") FilterOverloadOnly = true;
        else if (kpi.FilterKey == "HighPerf") FilterHighPerfOnly = true;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterShiftChanged(string? value) => ApplyFilters();
    partial void OnFilterBuildingChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterRoleChanged(string? value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterOverloadOnlyChanged(bool value) => ApplyFilters();
    partial void OnFilterHighPerfOnlyChanged(bool value) => ApplyFilters();

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
            PageButtons.Add(p is null ? new WorkforcePaginationButton("…", null, false, true)
                : new WorkforcePaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectStaff(WorkforceStaffDto? s) => SelectedStaff = s;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedStaff = null; StaffTimeline.Clear(); StaffAuditLog.Clear(); }
    partial void OnSelectedStaffChanged(WorkforceStaffDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null) { IsDetailOpen = true; SelectedDetailTab = 0; BuildTimeline(value); BuildAuditLog(value); }
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsAssignmentTab));
        OnPropertyChanged(nameof(IsPerformanceTab)); OnPropertyChanged(nameof(IsScheduleTab)); OnPropertyChanged(nameof(IsHistoryTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsAssignmentTab => SelectedDetailTab == 1;
    public bool IsPerformanceTab => SelectedDetailTab == 2;
    public bool IsScheduleTab => SelectedDetailTab == 3;
    public bool IsHistoryTab => SelectedDetailTab == 4;
    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsAssignForm)); OnPropertyChanged(nameof(IsShiftForm)); OnPropertyChanged(nameof(IsAreaForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));

    // ═══ WORKFLOW ═══

    [RelayCommand] private void OpenAssignForm()
    {
        FormMode = "Assign"; FormTitle = "Phân công phòng"; FormErrorMessage = string.Empty;
        AssignStaff = SelectedStaff?.FullName ?? ""; AssignBuilding = "A"; AssignFloor = "1";
        AssignRooms = string.Empty; AssignRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task SubmitAssignFormAsync(CancellationToken cancellationToken)
    {
        FormErrorMessage = string.Empty;
        if (SelectedStaff is null)
        {
            FormErrorMessage = "Hãy chọn nhân sự trước khi phân công phòng.";
            return;
        }

        if (housekeepingBoardService is null || housekeepingWorkflowService is null || executionContext?.Current is not { } context)
        {
            FormErrorMessage = "Chưa cấu hình Housekeeping board/workflow hoặc property context thật.";
            return;
        }

        var roomNumbers = AssignRooms
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(room => room.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (roomNumbers.Length == 0)
        {
            FormErrorMessage = "Hãy nhập ít nhất một số phòng.";
            return;
        }

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            var assignees = await housekeepingWorkflowService.GetActiveAssigneesAsync(cancellationToken);
            var assignee = assignees.FirstOrDefault(user =>
                user.UserName.Equals(SelectedStaff.StaffCode, StringComparison.OrdinalIgnoreCase)
                || user.DisplayName.Equals(SelectedStaff.FullName, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(SelectedStaff.Email)
                    && user.UserName.Equals(SelectedStaff.Email, StringComparison.OrdinalIgnoreCase)));
            if (assignee is null)
            {
                FormErrorMessage = $"Nhân sự {SelectedStaff.FullName} chưa được liên kết với tài khoản AppUser Housekeeping đang hoạt động (mã {SelectedStaff.StaffCode}).";
                return;
            }

            var rooms = await housekeepingBoardService.GetRoomSnapshotAsync(propertyId, cancellationToken);
            var requestedRooms = rooms
                .Where(room => roomNumbers.Contains(room.RoomNumber, StringComparer.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(AssignFloor)
                        || AssignFloor == "—"
                        || string.Equals(room.Floor, AssignFloor.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var missingRooms = roomNumbers
                .Except(requestedRooms.Select(room => room.RoomNumber), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (missingRooms.Length > 0)
            {
                FormErrorMessage = $"Không tìm thấy phòng thật trong property/tầng đã chọn: {string.Join(", ", missingRooms)}.";
                return;
            }

            var board = await housekeepingBoardService.GetTaskBoardAsync(
                propertyId,
                string.IsNullOrWhiteSpace(AssignFloor) || AssignFloor == "—" ? null : AssignFloor.Trim(),
                null,
                null,
                cancellationToken);
            var assigned = 0;
            foreach (var room in requestedRooms)
            {
                var task = board
                    .Where(item => item.RoomId == room.RoomId
                        && item.TaskStatus is not HousekeepingTaskStatus.Completed
                        and not HousekeepingTaskStatus.Cancelled)
                    .OrderByDescending(item => item.TaskId)
                    .FirstOrDefault();

                if (task is null)
                {
                    var created = await housekeepingWorkflowService.CreateTaskAsync(
                        new HkTaskCreateRequest(
                            propertyId,
                            room.RoomId,
                            HousekeepingTaskType.StayoverService,
                            RoomPriority.Normal,
                            string.IsNullOrWhiteSpace(AssignRemark) ? null : AssignRemark.Trim()),
                        cancellationToken);
                    if (created.IsFailure)
                    {
                        FormErrorMessage = $"Không thể tạo task cho phòng {room.RoomNumber}: {created.Error?.Message ?? "lỗi không xác định"}";
                        return;
                    }

                    var assignedCreated = await housekeepingWorkflowService.ExecuteTaskAsync(
                        new HkTaskActionRequest(propertyId, created.Value, HkTaskAction.Assign, assignee.UserId, AssignRemark),
                        cancellationToken);
                    if (assignedCreated.IsFailure)
                    {
                        FormErrorMessage = $"Đã tạo task nhưng không thể phân công phòng {room.RoomNumber}: {assignedCreated.Error?.Message ?? "lỗi không xác định"}";
                        return;
                    }
                }
                else
                {
                    if (task.TaskStatus == HousekeepingTaskStatus.WaitingInspection)
                    {
                        FormErrorMessage = $"Phòng {room.RoomNumber} đang chờ kiểm tra, không thể đổi người dọn ở bước này.";
                        return;
                    }

                    var action = task.TaskStatus == HousekeepingTaskStatus.Pending
                        ? HkTaskAction.Assign
                        : HkTaskAction.Reassign;
                    var result = await housekeepingWorkflowService.ExecuteTaskAsync(
                        new HkTaskActionRequest(propertyId, task.TaskId, action, assignee.UserId, AssignRemark),
                        cancellationToken);
                    if (result.IsFailure)
                    {
                        FormErrorMessage = $"Không thể phân công phòng {room.RoomNumber}: {result.Error?.Message ?? "lỗi không xác định"}";
                        return;
                    }
                }

                assigned++;
            }

            IsFormOpen = false;
            InfoMessage = $"Đã phân công {assigned} phòng thật cho {assignee.DisplayName}.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể phân công phòng thật: {ex.Message}";
        }
    }

    [RelayCommand] private void OpenShiftForm()
    {
        if (SelectedStaff is null) return;
        FormMode = "Shift"; FormTitle = $"Đổi ca — {SelectedStaff.FullName}"; FormErrorMessage = string.Empty;
        ShiftStaff = SelectedStaff.FullName; ShiftCurrent = SelectedStaff.Shift; ShiftNew = SelectedStaff.Shift; ShiftReason = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitShiftFormAsync(CancellationToken cancellationToken)
    {
        if (SelectedStaff is null || workforceCommandService is null)
        {
            FormErrorMessage = "Chưa có Workforce command service để lưu dữ liệu thật.";
            return;
        }

        var staffName = SelectedStaff.FullName;
        try
        {
            await workforceCommandService.ChangeShiftAsync(
                new WorkforceShiftChange(SelectedStaff.StaffCode, ShiftNew, ShiftReason), cancellationToken);
            IsFormOpen = false;
            await LoadAsync(cancellationToken);
            InfoMessage = $"Đã lưu ca {ShiftNew} cho {staffName}.";
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể lưu ca làm việc: {ex.Message}";
        }
    }

    [RelayCommand] private void OpenAreaForm()
    {
        if (SelectedStaff is null) return;
        FormMode = "Area"; FormTitle = $"Đổi khu vực — {SelectedStaff.FullName}"; FormErrorMessage = string.Empty;
        AreaStaff = SelectedStaff.FullName; AreaCurrent = $"{SelectedStaff.Building} — Tầng {SelectedStaff.Floor}";
        AreaNew = string.Empty; AreaReason = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitAreaFormAsync(CancellationToken cancellationToken)
    {
        if (SelectedStaff is null || workforceCommandService is null)
        {
            FormErrorMessage = "Chưa có Workforce command service để lưu dữ liệu thật.";
            return;
        }

        var area = AreaNew.Split('—', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var building = area.ElementAtOrDefault(0) ?? string.Empty;
        var floor = area.ElementAtOrDefault(1)?.Replace("Tầng", string.Empty, StringComparison.OrdinalIgnoreCase).Trim() ?? string.Empty;
        var staffName = SelectedStaff.FullName;
        try
        {
            await workforceCommandService.ChangeAreaAsync(
                new WorkforceAreaChange(SelectedStaff.StaffCode, building, floor, AreaReason), cancellationToken);
            IsFormOpen = false;
            await LoadAsync(cancellationToken);
            InfoMessage = $"Đã lưu khu vực cho {staffName}.";
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Không thể lưu khu vực: {ex.Message}";
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; FormErrorMessage = string.Empty; }

    [RelayCommand]
    private void ViewSchedule()
    {
        var rows = Staff.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Chưa có nhân sự thật để xuất lịch làm việc.";
            return;
        }

        try
        {
            var path = CsvExportHelper.Export(
                "housekeeping_workforce_schedule",
                ["Mã nhân viên", "Họ tên", "Ca", "Tòa", "Tầng", "Trạng thái"],
                rows.Select(row => (IReadOnlyList<string>)[
                    row.StaffCode, row.FullName, row.ShiftLabel, row.Building, row.Floor, row.StatusLabel
                ]).ToList());
            InfoMessage = $"Đã xuất lịch làm việc từ {rows.Count} nhân sự thật: {path}";
            MessageBox.Show(InfoMessage, "Lịch làm việc", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể xuất lịch Workforce thật: {ex.Message}";
        }
    }
    [RelayCommand] private void ViewKpi() { if (SelectedStaff is not null) MessageBox.Show($"KPI — {SelectedStaff.FullName}\n\nĐiểm: {SelectedStaff.KpiDisplay}\nXếp loại: {SelectedStaff.KpiGrade}\nPass: {SelectedStaff.InspectionPass}\nFail: {SelectedStaff.InspectionFail}\nSLA đúng: {SelectedStaff.SlaMet}\nSLA quá: {SelectedStaff.SlaMissed}", "KPI", MessageBoxButton.OK, MessageBoxImage.Information); }
    [RelayCommand]
    private void PrintReport()
    {
        var rows = FilteredStaff.Count > 0 ? FilteredStaff.ToList() : Staff.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Chưa có Workforce dữ liệu thật để tạo báo cáo.";
            return;
        }

        try
        {
            var path = CsvExportHelper.Export(
                "housekeeping_workforce_report",
                ["Mã nhân viên", "Họ tên", "Vai trò", "Ca", "Tòa", "Tầng", "Trạng thái", "Đã giao", "Hoàn thành", "KPI", "Xếp loại"],
                rows.Select(row => (IReadOnlyList<string>)[
                    row.StaffCode,
                    row.FullName,
                    row.RoleLabel,
                    row.ShiftLabel,
                    row.Building,
                    row.Floor,
                    row.StatusLabel,
                    row.TotalAssigned.ToString(),
                    row.Completed.ToString(),
                    row.KpiDisplay,
                    row.KpiGrade
                ]).ToList());
            InfoMessage = $"Đã tạo báo cáo Workforce từ {rows.Count} nhân sự thật: {path}";
            MessageBox.Show(InfoMessage, "Báo cáo Workforce", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tạo báo cáo Workforce thật: {ex.Message}";
        }
    }
    [RelayCommand]
    private void ExportExcel()
    {
        var rows = FilteredStaff.Count > 0 ? FilteredStaff.ToList() : Staff.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Chưa có Workforce dữ liệu thật để xuất.";
            return;
        }
        try
        {
            var path = CsvExportHelper.Export(
                "housekeeping_workforce",
                ["Mã nhân viên", "Họ tên", "Vai trò", "Ca", "Tòa", "Tầng", "Trạng thái", "Đã giao", "Hoàn thành", "KPI"],
                rows.Select(row => (IReadOnlyList<string>)[
                    row.StaffCode,
                    row.FullName,
                    row.RoleLabel,
                    row.ShiftLabel,
                    row.Building,
                    row.Floor,
                    row.StatusLabel,
                    row.TotalAssigned.ToString(),
                    row.Completed.ToString(),
                    row.KpiDisplay
                ]).ToList());
            InfoMessage = $"Đã xuất {rows.Count} nhân sự thật ra file CSV tương thích Excel: {path}";
            MessageBox.Show(InfoMessage, "Xuất Workforce", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể xuất Workforce thật: {ex.Message}";
        }
    }
    [RelayCommand] private void SetGridView() => ViewMode = "Grid";
    [RelayCommand] private void SetCardView() => ViewMode = "Card";
    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
