using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Rate;

public enum RateRuleType { FixedPrice, PctAdjustment, WeekendSurcharge, HolidaySurcharge, SeasonMultiplier, OccupancyMultiplier, LengthOfStayDiscount, AdvanceBookingDiscount, LastMinutePricing, VIPPricing, CorporatePricing, AgencyPricing, GroupPricing }
public enum RatePlanStatus { Draft, Active, Inactive, Archived }
public enum RateHierarchyLevel { BAR, Corporate, Negotiated, Package, Group, ManualOverride, FinalOverride }
public enum SeasonType { LowSeason, HighSeason, PeakSeason, Blackout, SpecialEvent }

public sealed class RatePlan
{
    private readonly List<RateCalendarEntry> _calendar = [];
    private readonly List<RateRule> _rules = [];
    private readonly List<RateSeason> _seasons = [];
    private RatePlan() { }

    public Guid RatePlanId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public string PlanName { get; private set; } = string.Empty;
    public Guid? RoomTypeId { get; private set; }
    public decimal BaseRate { get; private set; }
    public RatePlanStatus Status { get; private set; }
    public RateHierarchyLevel Hierarchy { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public bool AllowYieldManagement { get; private set; }
    public decimal YieldMinRate { get; private set; }
    public decimal YieldMaxRate { get; private set; }
    public int YieldThresholdOccupancy { get; private set; }
    public decimal YieldAdjustmentPct { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<RateCalendarEntry> Calendar => _calendar;
    public IReadOnlyList<RateRule> Rules => _rules;
    public IReadOnlyList<RateSeason> Seasons => _seasons;

    public static RatePlan Create(Guid propertyId, string code, string name, Guid? roomTypeId, decimal baseRate, RateHierarchyLevel hierarchy, DateTime from, DateTime to, DateTime nowUtc, ActorId? actorId) => new()
    { RatePlanId = Uuid7.NewGuid(), PropertyId = propertyId, PlanCode = code, PlanName = name, RoomTypeId = roomTypeId, BaseRate = baseRate, Status = RatePlanStatus.Active, Hierarchy = hierarchy, EffectiveFrom = from, EffectiveTo = to, AllowYieldManagement = false, YieldMinRate = 0, YieldMaxRate = 99999999, YieldThresholdOccupancy = 80, YieldAdjustmentPct = 10, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void AddCalendarEntry(DateOnly date, decimal rate, DateTime nowUtc) { _calendar.RemoveAll(c => c.Date == date); _calendar.Add(RateCalendarEntry.Create(RatePlanId, PropertyId, date, rate, nowUtc)); }
    public void AddRule(RateRuleType type, decimal value, int priority, bool exclusive, bool combinable, string? description, DateTime nowUtc, DateTime? expiry = null) { _rules.Add(RateRule.Create(RatePlanId, PropertyId, type, value, priority, exclusive, combinable, description, nowUtc, expiry)); }
    public void AddSeason(SeasonType type, DateTime from, DateTime to, decimal multiplier, string? description, DateTime nowUtc) => _seasons.Add(RateSeason.Create(RatePlanId, PropertyId, type, from, to, multiplier, description, nowUtc));

    public void EnableYieldManagement(decimal minRate, decimal maxRate, int thresholdOccupancy, decimal adjustmentPct) { AllowYieldManagement = true; YieldMinRate = minRate; YieldMaxRate = maxRate; YieldThresholdOccupancy = thresholdOccupancy; YieldAdjustmentPct = adjustmentPct; }

    public (decimal finalPrice, List<string> auditLog) CalculatePrice(DateOnly date, int adults, int nights, int totalRooms, int roomsSold)
    {
        var log = new List<string>();
        var price = _calendar.FirstOrDefault(c => c.Date == date)?.Rate ?? BaseRate;
        log.Add($"[BAR] Base rate: {price:N0} VND");

        // Apply seasons
        foreach (var season in _seasons.Where(s => s.IsActive && date >= DateOnly.FromDateTime(s.FromDate) && date <= DateOnly.FromDateTime(s.ToDate)))
        { price *= season.Multiplier; log.Add($"[Season] {season.Type} (×{season.Multiplier}): {price:N0} VND"); }

        // Apply active rules ordered by priority (lowest first)
        var activeRules = _rules.Where(r => r.IsActive && (r.ExpiryDate == null || r.ExpiryDate > DateTime.UtcNow)).OrderBy(r => r.Priority).ToList();

        foreach (var rule in activeRules)
        {
            if (rule.Exclusive)
            {
                var applied = ApplyRule(rule, price, date, adults, nights);
                if (applied.HasValue) price = applied.Value;
                log.Add($"[Rule P{rule.Priority}] {rule.Type}: {price:N0} VND (EXCLUSIVE)");
                break;
            }
            else
            {
                var applied = ApplyRule(rule, price, date, adults, nights);
                if (applied.HasValue) { price = applied.Value; log.Add($"[Rule P{rule.Priority}] {rule.Type}: {price:N0} VND"); }
            }
        }

        // Yield management
        if (AllowYieldManagement && totalRooms > 0)
        {
            var occupancyPct = (decimal)roomsSold / totalRooms * 100;
            if (occupancyPct > YieldThresholdOccupancy) { var adj = price * YieldAdjustmentPct / 100; price += adj; log.Add($"[Yield] Occupancy {occupancyPct:F0}% > {YieldThresholdOccupancy}%: +{YieldAdjustmentPct}% = {price:N0} VND"); }
        }

        price = Math.Max(YieldMinRate, Math.Min(YieldMaxRate, price));
        log.Add($"[FINAL] {price:N0} VND");
        return (price, log);
    }

    private static decimal? ApplyRule(RateRule rule, decimal price, DateOnly date, int adults, int nights)
    {
        return rule.Type switch
        {
            RateRuleType.FixedPrice => rule.Value,
            RateRuleType.PctAdjustment => price * (1 + rule.Value / 100),
            RateRuleType.WeekendSurcharge when date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday => price * (1 + rule.Value / 100),
            RateRuleType.LengthOfStayDiscount when nights >= 7 => price * (1 - rule.Value / 100),
            RateRuleType.OccupancyMultiplier => price * (1 + (adults - 1) * rule.Value / 100),
            RateRuleType.AdvanceBookingDiscount => price * (1 - rule.Value / 100),
            RateRuleType.LastMinutePricing => price * (1 + rule.Value / 100),
            RateRuleType.VIPPricing => price * (1 - rule.Value / 100),
            RateRuleType.CorporatePricing => price * (1 - rule.Value / 100),
            RateRuleType.AgencyPricing => price * (1 - rule.Value / 100),
            RateRuleType.GroupPricing => price * (1 - rule.Value / 100),
            _ => null
        };
    }
}

public sealed class RateCalendarEntry
{
    private RateCalendarEntry() { }
    public Guid RateCalendarEntryId { get; private set; } public Guid RatePlanId { get; private set; } public Guid PropertyId { get; private set; }
    public DateOnly Date { get; private set; } public decimal Rate { get; private set; } public DateTime UpdatedAtUtc { get; private set; }
    public static RateCalendarEntry Create(Guid planId, Guid propertyId, DateOnly date, decimal rate, DateTime nowUtc) => new() { RateCalendarEntryId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Date = date, Rate = rate, UpdatedAtUtc = nowUtc };
}

public sealed class RateRule
{
    private RateRule() { }
    public Guid RateRuleId { get; private set; } public Guid RatePlanId { get; private set; } public Guid PropertyId { get; private set; }
    public RateRuleType Type { get; private set; } public decimal Value { get; private set; } public int Priority { get; private set; }
    public bool Exclusive { get; private set; } public bool Combinable { get; private set; } public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; } public DateTime CreatedAtUtc { get; private set; } public DateTime? ExpiryDate { get; private set; }

    public static RateRule Create(Guid planId, Guid propertyId, RateRuleType type, decimal value, int priority, bool exclusive, bool combinable, string? description, DateTime nowUtc, DateTime? expiry = null) => new()
    { RateRuleId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Type = type, Value = value, Priority = priority, Exclusive = exclusive, Combinable = combinable, Description = description, CreatedAtUtc = nowUtc, ExpiryDate = expiry };
}

public sealed class RateSeason
{
    private RateSeason() { }
    public Guid RateSeasonId { get; private set; } public Guid RatePlanId { get; private set; } public Guid PropertyId { get; private set; }
    public SeasonType Type { get; private set; } public DateTime FromDate { get; private set; } public DateTime ToDate { get; private set; }
    public decimal Multiplier { get; private set; } public string? Description { get; private set; } public bool IsActive { get; private set; } = true;
    public static RateSeason Create(Guid planId, Guid propertyId, SeasonType type, DateTime from, DateTime to, decimal multiplier, string? description, DateTime nowUtc) => new()
    { RateSeasonId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Type = type, FromDate = from, ToDate = to, Multiplier = multiplier, Description = description };
}
