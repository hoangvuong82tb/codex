using System.Collections.ObjectModel;
using System.Text;
using ASMILE.BO.Application.FrontOffice.Rate;
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
    public int TotalRooms { get; set; } = 100;
    public int RoomsSold { get; set; } = 65;
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public ObservableCollection<RateCalendarRow> Calendar { get; set; } = new();
    public ObservableCollection<RateRuleRow> Rules { get; set; } = new();
    public ObservableCollection<RateSeasonRow> Seasons { get; set; } = new();
}

public sealed class RateCalendarRow { public DateOnly Date { get; set; } public decimal Rate { get; set; } public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday; }
public sealed class RateRuleRow { public string RuleType { get; set; } = ""; public decimal Value { get; set; } public int Priority { get; set; } = 10; public bool Exclusive { get; set; } public bool Combinable { get; set; } = true; public bool IsActive { get; set; } = true; public string? Description { get; set; } public DateTime? ExpiryDate { get; set; } }
public sealed class RateSeasonRow { public string SeasonType { get; set; } = ""; public DateTime FromDate { get; set; } public DateTime ToDate { get; set; } public decimal Multiplier { get; set; } = 1; public bool IsActive { get; set; } = true; public string? Description { get; set; } }

public sealed partial class RateEngineViewModel : ViewModelBase
{
    private readonly IRateRepository _repository;
    private readonly Guid _propertyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ObservableCollection<RatePlanDto> Plans { get; } = new();
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedPlan is not null;
    public string[] HierarchyFilters { get; } = ["Tat ca", "BAR", "Corporate", "Negotiated", "Package", "Group", "ManualOverride", "FinalOverride"];
    public string[] SeasonTypeFilters { get; } = ["Tat ca", "LowSeason", "HighSeason", "PeakSeason", "Blackout", "SpecialEvent"];

    public RateEngineViewModel(IRateRepository repository) { _repository = repository; }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Plans.Clear();
            var filter = new RatePlanSearchFilter(_propertyId, null, null, null);
            var dtos = await _repository.GetAllDetailDtosAsync(_propertyId, filter, ct);

            if (dtos.Count == 0)
            {
                InfoMessage = "Chua co ke hoach gia nao. Seed data da duoc tao.";
                await SeedDefaultPlansAsync(ct);
                dtos = await _repository.GetAllDetailDtosAsync(_propertyId, filter, ct);
            }

            foreach (var dto in dtos)
            {
                var p = new RatePlanDto
                {
                    RatePlanId = dto.Plan.RatePlanId, PlanCode = dto.Plan.PlanCode, PlanName = dto.Plan.PlanName,
                    BaseRate = dto.Plan.BaseRate, Status = dto.Plan.Status.ToString(), Hierarchy = dto.Plan.Hierarchy.ToString(),
                    AllowYield = dto.Plan.AllowYield, YieldMinRate = dto.Plan.YieldMinRate, YieldMaxRate = dto.Plan.YieldMaxRate,
                    YieldThreshold = dto.Plan.YieldThreshold, YieldAdjPct = dto.Plan.YieldAdjPct,
                    EffectiveFrom = dto.Plan.EffectiveFrom, EffectiveTo = dto.Plan.EffectiveTo, TotalRooms = 12, RoomsSold = 10
                };
                foreach (var c in dto.Calendar) p.Calendar.Add(new RateCalendarRow { Date = c.Date, Rate = c.Rate });
                foreach (var r in dto.Rules) p.Rules.Add(new RateRuleRow { RuleType = r.Type.ToString(), Value = r.Value, Priority = r.Priority, Exclusive = r.Exclusive, Combinable = r.Combinable, IsActive = r.IsActive, Description = r.Description, ExpiryDate = r.ExpiryDate });
                foreach (var s in dto.Seasons) p.Seasons.Add(new RateSeasonRow { SeasonType = s.Type.ToString(), FromDate = s.FromDate, ToDate = s.ToDate, Multiplier = s.Multiplier, IsActive = s.IsActive, Description = s.Description });
                Plans.Add(p);
            }
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task SeedDefaultPlansAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pid = _propertyId;

        var rack = RatePlan.Create(pid, "RACK", "Rack Rate (BAR)", null, 1500000m, RateHierarchyLevel.BAR, DateTime.Today.AddMonths(-6), DateTime.Today.AddYears(1), now, null);
        for (int d = 0; d < 30; d++) { var dt = DateOnly.FromDateTime(DateTime.Today.AddDays(d)); rack.AddCalendarEntry(dt, dt.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday ? 1800000m : 1500000m, now); }
        rack.AddRule(RateRuleType.WeekendSurcharge, 20, 10, false, true, "+20% cuoi tuan", now);
        rack.AddRule(RateRuleType.LengthOfStayDiscount, 10, 20, false, true, "-10% cho o >=7 dem", now);
        rack.AddRule(RateRuleType.GroupPricing, 25, 1, true, false, "Group rate -25% (EXCLUSIVE)", now);
        rack.AddSeason(SeasonType.PeakSeason, DateTime.Today.AddMonths(1), DateTime.Today.AddMonths(2), 1.5m, "Tet holiday peak", now);
        rack.AddSeason(SeasonType.LowSeason, DateTime.Today.AddMonths(3), DateTime.Today.AddMonths(5), 0.7m, "Low season discount", now);
        await _repository.AddAsync(rack, ct);

        var corp = RatePlan.Create(pid, "CORP", "Corporate Rate", null, 1200000m, RateHierarchyLevel.Corporate, DateTime.Today, DateTime.Today.AddYears(1), now, null);
        await _repository.AddAsync(corp, ct);

        var group = RatePlan.Create(pid, "GROUP", "Group Rate", null, 900000m, RateHierarchyLevel.Group, DateTime.Today, DateTime.Today.AddMonths(6), now, null);
        await _repository.AddAsync(group, ct);
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectPlan(RatePlanDto? p) { SelectedPlan = p; if (p != null) { IsYieldEnabled = p.AllowYield; RefreshYieldDisplay(); } }

    [RelayCommand]
    private void CalculatePrice()
    {
        CalcNights = Math.Max(1, (CalcDeparture - CalcArrival).Days);
        if (SelectedPlan is null) return;
        var date = DateOnly.FromDateTime(CalcArrival);
        var plan = SelectedPlan;
        var log = new List<string>();
        var price = plan.Calendar.FirstOrDefault(c => c.Date == date)?.Rate ?? plan.BaseRate;
        log.Add($"[BAR] Base rate: {price:N0} VND");
        foreach (var s in plan.Seasons.Where(s => s.IsActive && CalcArrival >= s.FromDate && CalcArrival <= s.ToDate)) { price *= s.Multiplier; log.Add($"[Season] {s.SeasonType} (x{s.Multiplier}): {price:N0} VND"); }
        foreach (var rule in plan.Rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
        {
            if (rule.Exclusive)
            {
                var appliedEx = false;
                if (rule.RuleType == "GroupPricing") { price = plan.BaseRate * (1 - rule.Value / 100); appliedEx = true; }
                if (rule.RuleType == "FixedPrice") { price = rule.Value; appliedEx = true; }
                if (appliedEx) { log.Add($"[Rule P{rule.Priority}] {rule.RuleType}: {price:N0} VND (EXCLUSIVE — bo qua cac rule khac)"); break; }
            }
            var applied = false;
            if (rule.RuleType == "WeekendSurcharge" && (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)) { price *= (1 + rule.Value / 100); applied = true; }
            if (rule.RuleType == "LengthOfStayDiscount" && CalcNights >= 7) { price *= (1 - rule.Value / 100); applied = true; }
            if (rule.RuleType == "GroupPricing") { price = plan.BaseRate * (1 - rule.Value / 100); applied = true; }
            if (applied) log.Add($"[Rule P{rule.Priority}] {rule.RuleType}: {price:N0} VND");
        }
        if (plan.AllowYield) { var occPct = (decimal)plan.RoomsSold / plan.TotalRooms * 100; if (occPct > plan.YieldThreshold) { var adj = price * plan.YieldAdjPct / 100; price += adj; log.Add($"[Yield] Occupancy {occPct:F0}% > {plan.YieldThreshold}%: +{plan.YieldAdjPct}% = {price:N0} VND"); } }
        price = Math.Max(plan.YieldMinRate, Math.Min(plan.YieldMaxRate, Math.Max(0, price)));
        log.Add($"[FINAL] {price:N0} VND (x {CalcNights} dem = {price * CalcNights:N0} VND)");
        CalcFinalPrice = price * CalcNights;
        CalcBasePrice = plan.BaseRate * CalcNights;
        AuditLog = string.Join("\n", log);
        CalcBreakdown = $"Gia goc: {plan.BaseRate:N0}/đem x {CalcNights} đem = {CalcBasePrice:N0}\n" + string.Join("\n", log);
    }

    [RelayCommand] private void RefreshYieldDisplay() { if (SelectedPlan is { AllowYield: true }) YieldOccupancyDisplay = $"Occupancy: {SelectedPlan.RoomsSold}/{SelectedPlan.TotalRooms} = {(decimal)SelectedPlan.RoomsSold / SelectedPlan.TotalRooms * 100:F0}% (nguong: {SelectedPlan.YieldThreshold}%)"; }

    partial void OnSelectedPlanChanged(RatePlanDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
