using System.Collections.ObjectModel;
using System.Text;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Rate;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class RatePlanDto
{
    public Guid RatePlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal BaseRate { get; set; }
    public string Status { get; set; } = "Active";
    public string Hierarchy { get; set; } = "BAR";
    public bool AllowYield { get; set; }
    public decimal YieldMinRate { get; set; }
    public decimal YieldMaxRate { get; set; } = 99999999;
    public int YieldThreshold { get; set; } = 80;
    public decimal YieldAdjPct { get; set; } = 10;
    public Guid? RoomTypeId { get; set; }
    public string RoomTypeDisplay { get; set; } = "Toàn bộ hạng phòng";
    public int TotalRooms { get; set; }
    public int RoomsSold { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public ObservableCollection<RateCalendarRow> Calendar { get; set; } = new();
    public ObservableCollection<RateRuleRow> Rules { get; set; } = new();
    public ObservableCollection<RateSeasonRow> Seasons { get; set; } = new();
}

public sealed class RateCalendarRow { public DateOnly Date { get; set; } public decimal Rate { get; set; } public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday; }
public sealed class RateRuleRow
{
    public Guid RateRuleId { get; set; }
    public string RuleType { get; set; } = "";
    public decimal Value { get; set; }
    public int Priority { get; set; } = 10;
    public bool Exclusive { get; set; }
    public bool Combinable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string ValueDisplay => RuleType switch
    {
        nameof(RateRuleType.FixedPrice) => $"{Value:N0} VND",
        nameof(RateRuleType.SeasonMultiplier) => $"x{Value:N2}",
        _ => $"{Value:N0}%"
    };
}
public sealed class RateSeasonRow { public Guid RateSeasonId { get; set; } public string SeasonType { get; set; } = ""; public DateTime FromDate { get; set; } public DateTime ToDate { get; set; } public decimal Multiplier { get; set; } = 1; public bool IsActive { get; set; } = true; public string? Description { get; set; } }
public sealed record RateRoomTypeOption(Guid? RoomTypeId, string DisplayName);

public sealed partial class RateEngineViewModel : ViewModelBase
{
    private readonly IRateRepository _repository;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;
    private readonly IHotelRoomMasterDataRepository _roomRepository;
    private readonly IHotelStayRepository _stayRepository;
    private readonly IFrontOfficePersistenceSession _persistence;

    public ObservableCollection<RatePlanDto> Plans { get; } = new();
    public ObservableCollection<RatePlanDto> VisiblePlans { get; } = new();
    public ObservableCollection<RateRoomTypeOption> RoomTypes { get; } = new();
    [ObservableProperty] private RatePlanDto? _selectedPlan;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private DateTime _calcArrival = DateTime.Today.AddDays(1);
    [ObservableProperty] private DateTime _calcDeparture = DateTime.Today.AddDays(3);
    [ObservableProperty] private int _calcAdults = 2;
    [ObservableProperty] private int _calcNights;
    [ObservableProperty] private decimal _calcBasePrice;
    [ObservableProperty] private decimal _calcFinalPrice;
    [ObservableProperty] private string _calcBreakdown = "";
    [ObservableProperty] private string _auditLog = "";
    [ObservableProperty] private bool _isYieldEnabled;
    [ObservableProperty] private string _yieldOccupancyDisplay = "";
    [ObservableProperty] private string? _filterHierarchy;
    [ObservableProperty] private string? _filterSeasonType;
    [ObservableProperty] private string _filterStatus = "Tất cả";
    [ObservableProperty] private RateRoomTypeOption? _filterRoomType;
    [ObservableProperty] private bool _isPlanEditorOpen;
    [ObservableProperty] private bool _isEditingPlan;
    [ObservableProperty] private bool _isComponentEditorOpen;
    [ObservableProperty] private string _editorErrorMessage = string.Empty;
    [ObservableProperty] private string _ratePlanCode = string.Empty;
    [ObservableProperty] private string _ratePlanName = string.Empty;
    [ObservableProperty] private RateRoomTypeOption? _selectedRoomType;
    [ObservableProperty] private decimal _ratePlanBaseRate;
    [ObservableProperty] private DateTime _ratePlanEffectiveFrom = DateTime.Today;
    [ObservableProperty] private DateTime _ratePlanEffectiveTo = DateTime.Today.AddYears(1);
    [ObservableProperty] private string _selectedRateHierarchy = nameof(RateHierarchyLevel.BAR);
    [ObservableProperty] private bool _enableYieldManagement;
    [ObservableProperty] private decimal _yieldMinRate;
    [ObservableProperty] private decimal _yieldMaxRate = 99999999;
    [ObservableProperty] private int _yieldThreshold = 80;
    [ObservableProperty] private decimal _yieldAdjustmentPct = 10;
    [ObservableProperty] private string _componentEditorMode = string.Empty;
    [ObservableProperty] private DateTime _componentFrom = DateTime.Today;
    [ObservableProperty] private DateTime _componentTo = DateTime.Today;
    [ObservableProperty] private decimal _componentValue;
    [ObservableProperty] private string _componentDescription = string.Empty;
    [ObservableProperty] private string _selectedRuleType = nameof(RateRuleType.WeekendSurcharge);
    [ObservableProperty] private string _selectedSeasonType = nameof(SeasonType.HighSeason);
    [ObservableProperty] private int _componentPriority = 10;
    [ObservableProperty] private bool _componentExclusive;
    [ObservableProperty] private bool _componentCombinable = true;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasSelection => SelectedPlan is not null;
    public bool IsCalendarEditor => ComponentEditorMode == "Calendar";
    public bool IsSeasonEditor => ComponentEditorMode == "Season";
    public bool IsRuleEditor => ComponentEditorMode == "Rule";
    public string[] HierarchyFilters { get; } = ["Tat ca", "BAR", "Corporate", "Negotiated", "Package", "Group", "ManualOverride", "FinalOverride"];
    public string[] SeasonTypeFilters { get; } = ["Tat ca", "LowSeason", "HighSeason", "PeakSeason", "Blackout", "SpecialEvent"];
    public string[] PlanStatusFilters { get; } = ["Tất cả", "Draft", "Active", "Inactive", "Archived"];
    public string[] RateHierarchies { get; } = Enum.GetNames<RateHierarchyLevel>();
    public string[] RuleTypes { get; } = Enum.GetValues<RateRuleType>()
        .Where(x => x != RateRuleType.HolidaySurcharge)
        .Select(x => x.ToString())
        .ToArray();
    public string[] SeasonTypes { get; } = Enum.GetNames<SeasonType>();

    public RateEngineViewModel(
        IRateRepository repository,
        IFrontOfficeExecutionContextAccessor contextAccessor,
        IHotelRoomMasterDataRepository roomRepository,
        IHotelStayRepository stayRepository,
        IFrontOfficePersistenceSession persistence)
    {
        _repository = repository;
        _contextAccessor = contextAccessor;
        _roomRepository = roomRepository;
        _stayRepository = stayRepository;
        _persistence = persistence;
    }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Plans.Clear();
            VisiblePlans.Clear();
            var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty)
            {
                ErrorMessage = "Chưa chọn cơ sở để tải kế hoạch giá thật.";
                return;
            }

            var filter = new RatePlanSearchFilter(currentPropertyId, null, null, null);
            var dtos = await _repository.GetAllDetailDtosAsync(currentPropertyId, filter, ct);
            var roomTypes = await _roomRepository.SearchRoomTypesAsync(
                currentPropertyId,
                keyword: null,
                isActive: true,
                page: new PageRequest(1, 5000),
                sort: null,
                ct: ct);
            var roomTypeNames = roomTypes.Items.ToDictionary(
                x => x.HotelRoomTypeId,
                x => $"{x.Code} — {x.Name}");
            RoomTypes.Clear();
            RoomTypes.Add(new RateRoomTypeOption(null, "Toàn bộ hạng phòng"));
            foreach (var roomType in roomTypes.Items.OrderBy(x => x.Code))
            {
                RoomTypes.Add(new RateRoomTypeOption(roomType.HotelRoomTypeId, $"{roomType.Code} — {roomType.Name}"));
            }
            FilterRoomType = RoomTypes.FirstOrDefault();
            var rooms = await _roomRepository.SearchRoomsAsync(
                currentPropertyId,
                keyword: null,
                roomTypeId: null,
                floor: null,
                building: null,
                status: null,
                isActive: true,
                page: new PageRequest(1, 5000),
                sort: null,
                assignableOnly: false,
                ct: ct);
            var inHouse = await _stayRepository.GetCurrentInHouseAsync(currentPropertyId, ct);

            if (dtos.Count == 0)
                InfoMessage = "Chưa có kế hoạch giá thật trong phạm vi hiện tại. Hãy tạo hoặc import rate plan trước khi bán phòng.";

            foreach (var dto in dtos)
            {
                var p = new RatePlanDto
                {
                    RatePlanId = dto.Plan.RatePlanId, PlanCode = dto.Plan.PlanCode, PlanName = dto.Plan.PlanName,
                    BaseRate = dto.Plan.BaseRate, Status = dto.Plan.Status.ToString(), Hierarchy = dto.Plan.Hierarchy.ToString(),
                    AllowYield = dto.Plan.AllowYield, YieldMinRate = dto.Plan.YieldMinRate, YieldMaxRate = dto.Plan.YieldMaxRate,
                    YieldThreshold = dto.Plan.YieldThreshold, YieldAdjPct = dto.Plan.YieldAdjPct,
                    RoomTypeId = dto.Plan.RoomTypeId,
                    RoomTypeDisplay = dto.Plan.RoomTypeId is Guid scopedType && roomTypeNames.TryGetValue(scopedType, out var roomTypeName)
                        ? roomTypeName
                        : "Toàn bộ hạng phòng",
                    EffectiveFrom = dto.Plan.EffectiveFrom, EffectiveTo = dto.Plan.EffectiveTo,
                    TotalRooms = dto.Plan.RoomTypeId is Guid roomTypeId
                        ? rooms.Items.Count(x => x.HotelRoomTypeId == roomTypeId)
                        : rooms.Total,
                    RoomsSold = dto.Plan.RoomTypeId is Guid scopedRoomTypeId
                        ? inHouse.Count(x => x.RoomTypeId == scopedRoomTypeId)
                        : inHouse.Count
                };
                foreach (var c in dto.Calendar) p.Calendar.Add(new RateCalendarRow { Date = c.Date, Rate = c.Rate });
                foreach (var r in dto.Rules) p.Rules.Add(new RateRuleRow { RateRuleId = r.RateRuleId, RuleType = r.Type.ToString(), Value = r.Value, Priority = r.Priority, Exclusive = r.Exclusive, Combinable = r.Combinable, IsActive = r.IsActive, Description = r.Description, ExpiryDate = r.ExpiryDate });
                foreach (var s in dto.Seasons) p.Seasons.Add(new RateSeasonRow { RateSeasonId = s.RateSeasonId, SeasonType = s.Type.ToString(), FromDate = s.FromDate, ToDate = s.ToDate, Multiplier = s.Multiplier, IsActive = s.IsActive, Description = s.Description });
                Plans.Add(p);
            }
            ApplyPlanFilters();
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectPlan(RatePlanDto? p) { SelectedPlan = p; if (p != null) { IsYieldEnabled = p.AllowYield; RefreshYieldDisplay(); } }

    [RelayCommand]
    private void OpenCreatePlan()
    {
        EditorErrorMessage = string.Empty;
        IsEditingPlan = false;
        RatePlanCode = string.Empty;
        RatePlanName = string.Empty;
        RatePlanBaseRate = 0;
        RatePlanEffectiveFrom = DateTime.Today;
        RatePlanEffectiveTo = DateTime.Today.AddYears(1);
        SelectedRoomType = RoomTypes.FirstOrDefault();
        SelectedRateHierarchy = nameof(RateHierarchyLevel.BAR);
        EnableYieldManagement = false;
        YieldMinRate = 0;
        YieldMaxRate = 99999999;
        YieldThreshold = 80;
        YieldAdjustmentPct = 10;
        IsPlanEditorOpen = true;
    }

    [RelayCommand]
    private void OpenEditPlan()
    {
        if (SelectedPlan is null) return;
        EditorErrorMessage = string.Empty;
        IsEditingPlan = true;
        RatePlanCode = SelectedPlan.PlanCode;
        RatePlanName = SelectedPlan.PlanName;
        RatePlanBaseRate = SelectedPlan.BaseRate;
        RatePlanEffectiveFrom = SelectedPlan.EffectiveFrom;
        RatePlanEffectiveTo = SelectedPlan.EffectiveTo;
        SelectedRateHierarchy = SelectedPlan.Hierarchy;
        SelectedRoomType = RoomTypes.FirstOrDefault(x => x.RoomTypeId == SelectedPlan.RoomTypeId) ?? RoomTypes.FirstOrDefault();
        EnableYieldManagement = SelectedPlan.AllowYield;
        YieldMinRate = SelectedPlan.YieldMinRate;
        YieldMaxRate = SelectedPlan.YieldMaxRate;
        YieldThreshold = SelectedPlan.YieldThreshold;
        YieldAdjustmentPct = SelectedPlan.YieldAdjPct;
        IsPlanEditorOpen = true;
    }

    [RelayCommand]
    private void ClosePlanEditor() => IsPlanEditorOpen = false;

    [RelayCommand]
    private async Task SavePlanAsync(CancellationToken ct)
    {
        EditorErrorMessage = string.Empty;
        var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty)
        {
            EditorErrorMessage = "Chưa chọn cơ sở để tạo kế hoạch giá.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RatePlanCode) || string.IsNullOrWhiteSpace(RatePlanName))
        {
            EditorErrorMessage = "Cần nhập mã và tên kế hoạch giá.";
            return;
        }
        if (RatePlanBaseRate <= 0 || RatePlanEffectiveTo.Date < RatePlanEffectiveFrom.Date)
        {
            EditorErrorMessage = "Giá cơ bản phải lớn hơn 0 và thời gian hiệu lực phải hợp lệ.";
            return;
        }
        if (!Enum.TryParse<RateHierarchyLevel>(SelectedRateHierarchy, out var hierarchy))
        {
            EditorErrorMessage = "Cấp giá không hợp lệ.";
            return;
        }
        if (Plans.Any(x => x.RatePlanId != SelectedPlan?.RatePlanId && string.Equals(x.PlanCode, RatePlanCode.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            EditorErrorMessage = "Mã kế hoạch giá đã tồn tại trong cơ sở này.";
            return;
        }

        if (EnableYieldManagement && (YieldMinRate < 0 || YieldMaxRate < YieldMinRate || YieldThreshold is < 0 or > 100 || YieldAdjustmentPct is < 0 or > 100))
        {
            EditorErrorMessage = "Giá sàn/trần, ngưỡng occupancy hoặc biên độ yield không hợp lệ.";
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            RatePlan plan;
            if (IsEditingPlan)
            {
                plan = await _repository.GetAsync(SelectedPlan!.RatePlanId, currentPropertyId, ct)
                    ?? throw new InvalidOperationException("Không tìm thấy kế hoạch giá cần cập nhật.");
                plan.UpdateDetails(RatePlanCode, RatePlanName, SelectedRoomType?.RoomTypeId, RatePlanBaseRate, hierarchy, RatePlanEffectiveFrom, RatePlanEffectiveTo);
                plan.ConfigureYieldManagement(EnableYieldManagement, YieldMinRate, YieldMaxRate, YieldThreshold, YieldAdjustmentPct);
            }
            else
            {
                plan = RatePlan.Create(currentPropertyId, RatePlanCode.Trim().ToUpperInvariant(), RatePlanName.Trim(), SelectedRoomType?.RoomTypeId, RatePlanBaseRate, hierarchy, RatePlanEffectiveFrom.Date, RatePlanEffectiveTo.Date, now, _contextAccessor.Current?.Actor.ActorId);
                if (EnableYieldManagement) plan.EnableYieldManagement(YieldMinRate, YieldMaxRate, YieldThreshold, YieldAdjustmentPct);
            }
            if (!IsEditingPlan) await _repository.AddAsync(plan, ct);
            await _persistence.CommitAsync(ct);
            await LoadAsync(ct);
            SelectedPlan = Plans.FirstOrDefault(x => x.RatePlanId == plan.RatePlanId);
            InfoMessage = IsEditingPlan ? "Đã cập nhật kế hoạch giá trên dữ liệu thật." : "Đã tạo kế hoạch giá từ dữ liệu thật.";
            IsPlanEditorOpen = false;
            IsEditingPlan = false;
        }
        catch (Exception ex)
        {
            EditorErrorMessage = $"Không thể lưu kế hoạch giá: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TogglePlanStatusAsync(CancellationToken ct)
    {
        if (SelectedPlan is null) return;
        var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty) return;
        try
        {
            var plan = await _repository.GetAsync(SelectedPlan.RatePlanId, currentPropertyId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy kế hoạch giá.");
            var next = plan.Status == RatePlanStatus.Active ? RatePlanStatus.Inactive : RatePlanStatus.Active;
            plan.SetStatus(next);
            await _persistence.CommitAsync(ct);
            var id = plan.RatePlanId;
            await LoadAsync(ct);
            SelectedPlan = Plans.FirstOrDefault(x => x.RatePlanId == id);
            InfoMessage = next == RatePlanStatus.Active ? "Đã mở bán kế hoạch giá." : "Đã ngừng bán kế hoạch giá; lịch sử đặt phòng không bị thay đổi.";
        }
        catch (Exception ex) { ErrorMessage = $"Không thể đổi trạng thái kế hoạch giá: {ex.Message}"; }
    }

    [RelayCommand] private void OpenAddCalendar() => OpenComponentEditor("Calendar");
    [RelayCommand] private void OpenAddSeason() => OpenComponentEditor("Season");
    [RelayCommand] private void OpenAddRule() => OpenComponentEditor("Rule");
    [RelayCommand] private void CloseComponentEditor() => IsComponentEditorOpen = false;

    [RelayCommand]
    private async Task ToggleRuleStatusAsync(RateRuleRow? row, CancellationToken ct)
    {
        if (row is null || SelectedPlan is null) return;
        await SetComponentStatusAsync(
            row.IsActive,
            (plan, next) => plan.SetRuleStatus(row.RateRuleId, next),
            ct,
            "quy tắc");
    }

    [RelayCommand]
    private async Task ToggleSeasonStatusAsync(RateSeasonRow? row, CancellationToken ct)
    {
        if (row is null || SelectedPlan is null) return;
        await SetComponentStatusAsync(
            row.IsActive,
            (plan, next) => plan.SetSeasonStatus(row.RateSeasonId, next),
            ct,
            "mùa/sự kiện");
    }

    private async Task SetComponentStatusAsync(
        bool currentStatus,
        Action<RatePlan, bool> update,
        CancellationToken ct,
        string componentName)
    {
        var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
        if (SelectedPlan is null || propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty) return;
        try
        {
            var plan = await _repository.GetAsync(SelectedPlan.RatePlanId, currentPropertyId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy kế hoạch giá.");
            var next = !currentStatus;
            update(plan, next);
            await _persistence.CommitAsync(ct);
            var selectedPlanId = plan.RatePlanId;
            await LoadAsync(ct);
            SelectedPlan = Plans.FirstOrDefault(x => x.RatePlanId == selectedPlanId);
            InfoMessage = $"Đã {(next ? "bật" : "tắt")} {componentName}; lịch sử cấu hình vẫn được giữ lại.";
        }
        catch (Exception ex)
        {
            EditorErrorMessage = $"Không thể đổi trạng thái {componentName}: {ex.Message}";
        }
    }

    private void OpenComponentEditor(string mode)
    {
        if (SelectedPlan is null) return;
        EditorErrorMessage = string.Empty;
        ComponentEditorMode = mode;
        ComponentFrom = DateTime.Today;
        ComponentTo = DateTime.Today;
        ComponentValue = mode switch
        {
            "Season" => 1m,
            "Rule" when string.Equals(SelectedRuleType, nameof(RateRuleType.FixedPrice), StringComparison.Ordinal) => SelectedPlan.BaseRate,
            "Rule" => 10m,
            _ => SelectedPlan.BaseRate
        };
        ComponentDescription = string.Empty;
        ComponentPriority = 10;
        ComponentExclusive = false;
        ComponentCombinable = true;
        IsComponentEditorOpen = true;
    }

    private void ApplyPlanFilters()
    {
        VisiblePlans.Clear();
        var status = string.IsNullOrWhiteSpace(FilterStatus) ? "Tất cả" : FilterStatus;
        foreach (var plan in Plans
            .Where(x => status == "Tất cả" || string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase))
            .Where(x => FilterRoomType?.RoomTypeId is not Guid roomTypeId || x.RoomTypeId == roomTypeId)
            .OrderBy(x => x.PlanCode))
        {
            VisiblePlans.Add(plan);
        }

        if (SelectedPlan is not null && !VisiblePlans.Contains(SelectedPlan))
            SelectedPlan = null;
    }

    partial void OnSelectedRuleTypeChanged(string value)
    {
        if (!IsComponentEditorOpen || !IsRuleEditor || SelectedPlan is null) return;
        ComponentValue = value switch
        {
            nameof(RateRuleType.FixedPrice) => SelectedPlan.BaseRate,
            nameof(RateRuleType.SeasonMultiplier) => 1m,
            _ => 10m
        };
    }

    partial void OnSelectedSeasonTypeChanged(string value)
    {
        if (IsComponentEditorOpen && IsSeasonEditor && string.Equals(value, nameof(SeasonType.Blackout), StringComparison.Ordinal))
            ComponentValue = 0m;
    }

    [RelayCommand]
    private async Task SaveComponentAsync(CancellationToken ct)
    {
        EditorErrorMessage = string.Empty;
        if (SelectedPlan is null) return;
        var isBlackoutSeason = IsSeasonEditor && string.Equals(SelectedSeasonType, nameof(SeasonType.Blackout), StringComparison.Ordinal);
        if (ComponentTo.Date < ComponentFrom.Date || (!isBlackoutSeason && ComponentValue <= 0) || (isBlackoutSeason && ComponentValue != 0))
        {
            EditorErrorMessage = isBlackoutSeason
                ? "Mùa đóng phòng phải có hệ số bằng 0."
                : "Ngày áp dụng và giá trị thiết lập chưa hợp lệ.";
            return;
        }
        if (IsRuleEditor && Enum.TryParse<RateRuleType>(SelectedRuleType, out var selectedRuleType)
            && selectedRuleType is RateRuleType.LengthOfStayDiscount or RateRuleType.AdvanceBookingDiscount or RateRuleType.VIPPricing or RateRuleType.CorporatePricing or RateRuleType.AgencyPricing or RateRuleType.GroupPricing
            && ComponentValue > 100)
        {
            EditorErrorMessage = "Giá trị chiết khấu phải nằm trong khoảng 0–100%.";
            return;
        }
        var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty) return;
        try
        {
            var plan = await _repository.GetAsync(SelectedPlan.RatePlanId, currentPropertyId, ct);
            if (plan is null)
            {
                EditorErrorMessage = "Không tìm thấy kế hoạch giá cần cập nhật.";
                return;
            }
            var now = DateTime.UtcNow;
            switch (ComponentEditorMode)
            {
                case "Calendar":
                    plan.AddCalendarEntry(DateOnly.FromDateTime(ComponentFrom), ComponentValue, now);
                    break;
                case "Season" when Enum.TryParse<SeasonType>(SelectedSeasonType, out var seasonType):
                    plan.AddSeason(seasonType, ComponentFrom.Date, ComponentTo.Date, ComponentValue, ComponentDescription.Trim(), now);
                    break;
                case "Rule" when Enum.TryParse<RateRuleType>(SelectedRuleType, out var ruleType):
                    plan.AddRule(ruleType, ComponentValue, ComponentPriority, ComponentExclusive, ComponentCombinable, ComponentDescription.Trim(), now);
                    break;
                default:
                    EditorErrorMessage = "Loại thiết lập giá không hợp lệ.";
                    return;
            }
            await _persistence.CommitAsync(ct);
            var selectedPlanId = SelectedPlan.RatePlanId;
            await LoadAsync(ct);
            SelectedPlan = Plans.FirstOrDefault(x => x.RatePlanId == selectedPlanId);
            InfoMessage = "Đã lưu thiết lập giá.";
            IsComponentEditorOpen = false;
        }
        catch (Exception ex)
        {
            EditorErrorMessage = $"Không thể lưu thiết lập giá: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CalculatePrice()
    {
        ErrorMessage = string.Empty;
        CalcFinalPrice = 0;
        if (SelectedPlan is null)
        {
            ErrorMessage = "Hãy chọn một kế hoạch giá trước khi tính.";
            return;
        }
        if (CalcDeparture.Date <= CalcArrival.Date)
        {
            ErrorMessage = "Ngày đi phải sau ngày đến.";
            return;
        }
        if (CalcAdults < 1)
        {
            ErrorMessage = "Số người lớn phải từ 1 trở lên.";
            return;
        }

        if (CalcArrival.Date < SelectedPlan.EffectiveFrom.Date
            || CalcDeparture.Date.AddDays(-1) > SelectedPlan.EffectiveTo.Date)
        {
            ErrorMessage = $"Kế hoạch giá chỉ có hiệu lực từ {SelectedPlan.EffectiveFrom:dd/MM/yyyy} đến {SelectedPlan.EffectiveTo:dd/MM/yyyy}; hãy chọn lại khoảng ngày.";
            return;
        }

        var propertyId = _contextAccessor.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty)
        {
            ErrorMessage = "Chưa chọn cơ sở để tính giá thật.";
            return;
        }

        CalcNights = (CalcDeparture.Date - CalcArrival.Date).Days;
        var selected = SelectedPlan;
        if (!Enum.TryParse<RateHierarchyLevel>(selected.Hierarchy, out var hierarchy))
        {
            ErrorMessage = "Kế hoạch giá đang có cấp giá không hợp lệ.";
            return;
        }
        try
        {
            var now = DateTime.UtcNow;
            var preview = RatePlan.Create(
                currentPropertyId,
                selected.PlanCode,
                selected.PlanName,
                selected.RoomTypeId,
                selected.BaseRate,
                hierarchy,
                selected.EffectiveFrom,
                selected.EffectiveTo,
                now,
                null);
            foreach (var calendar in selected.Calendar)
                preview.AddCalendarEntry(calendar.Date, calendar.Rate, now);
            foreach (var season in selected.Seasons.Where(x => x.IsActive))
                if (Enum.TryParse<SeasonType>(season.SeasonType, out var seasonType))
                    preview.AddSeason(seasonType, season.FromDate, season.ToDate, season.Multiplier, season.Description, now);
            foreach (var rule in selected.Rules.Where(x => x.IsActive))
                if (Enum.TryParse<RateRuleType>(rule.RuleType, out var ruleType))
                    preview.AddRule(ruleType, rule.Value, rule.Priority, rule.Exclusive, rule.Combinable, rule.Description, now, rule.ExpiryDate);
            if (selected.AllowYield)
                preview.EnableYieldManagement(selected.YieldMinRate, selected.YieldMaxRate, selected.YieldThreshold, selected.YieldAdjPct);

            var (stayTotal, log) = preview.CalculateStayPrice(
                DateOnly.FromDateTime(CalcArrival),
                CalcAdults,
                CalcNights,
                selected.TotalRooms,
                selected.RoomsSold);
            CalcFinalPrice = stayTotal;
            CalcBasePrice = Enumerable.Range(0, CalcNights)
                .Select(offset => DateOnly.FromDateTime(CalcArrival).AddDays(offset))
                .Sum(date => selected.Calendar.FirstOrDefault(x => x.Date == date)?.Rate ?? selected.BaseRate);
            AuditLog = string.Join("\n", log);
            CalcBreakdown = $"Giá nền theo từng đêm = {CalcBasePrice:N0}\n" + string.Join("\n", log);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tính giá: {ex.Message}";
        }
    }

    [RelayCommand] private void RefreshYieldDisplay() { if (SelectedPlan is { AllowYield: true }) { var occupancy = SelectedPlan.TotalRooms > 0 ? (decimal)SelectedPlan.RoomsSold / SelectedPlan.TotalRooms * 100 : 0m; YieldOccupancyDisplay = $"Occupancy: {SelectedPlan.RoomsSold}/{SelectedPlan.TotalRooms} = {occupancy:F0}% (nguong: {SelectedPlan.YieldThreshold}%)"; } }

    partial void OnSelectedPlanChanged(RatePlanDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnFilterStatusChanged(string value) => ApplyPlanFilters();
    partial void OnFilterRoomTypeChanged(RateRoomTypeOption? value) => ApplyPlanFilters();
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));
    partial void OnComponentEditorModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCalendarEditor));
        OnPropertyChanged(nameof(IsSeasonEditor));
        OnPropertyChanged(nameof(IsRuleEditor));
    }
}
