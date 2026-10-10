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

    public static RatePlan Create(Guid propertyId, string code, string name, Guid? roomTypeId, decimal baseRate, RateHierarchyLevel hierarchy, DateTime from, DateTime to, DateTime nowUtc, ActorId? actorId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Rate plan code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rate plan name is required.", nameof(name));
        if (baseRate <= 0) throw new ArgumentOutOfRangeException(nameof(baseRate), "Base rate must be greater than zero.");
        if (to.Date < from.Date) throw new ArgumentException("Effective end date must not precede the start date.");

        return new()
        {
            RatePlanId = Uuid7.NewGuid(), PropertyId = propertyId, PlanCode = code.Trim().ToUpperInvariant(),
            PlanName = name.Trim(), RoomTypeId = roomTypeId, BaseRate = baseRate, Status = RatePlanStatus.Active,
            Hierarchy = hierarchy, EffectiveFrom = from.Date, EffectiveTo = to.Date, AllowYieldManagement = false,
            YieldMinRate = 0, YieldMaxRate = 99999999, YieldThresholdOccupancy = 80, YieldAdjustmentPct = 10,
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value
        };
    }

    public void UpdateDetails(string code, string name, Guid? roomTypeId, decimal baseRate, RateHierarchyLevel hierarchy, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Rate plan code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rate plan name is required.", nameof(name));
        if (baseRate <= 0) throw new ArgumentOutOfRangeException(nameof(baseRate), "Base rate must be greater than zero.");
        if (to.Date < from.Date) throw new ArgumentException("Effective end date must not precede the start date.");
        PlanCode = code.Trim().ToUpperInvariant();
        PlanName = name.Trim();
        RoomTypeId = roomTypeId;
        BaseRate = baseRate;
        Hierarchy = hierarchy;
        EffectiveFrom = from.Date;
        EffectiveTo = to.Date;
    }

    public void AddCalendarEntry(DateOnly date, decimal rate, DateTime nowUtc) { _calendar.RemoveAll(c => c.Date == date); _calendar.Add(RateCalendarEntry.Create(RatePlanId, PropertyId, date, rate, nowUtc)); }
    public void AddRule(RateRuleType type, decimal value, int priority, bool exclusive, bool combinable, string? description, DateTime nowUtc, DateTime? expiry = null) { _rules.Add(RateRule.Create(RatePlanId, PropertyId, type, value, priority, exclusive, combinable, description, nowUtc, expiry)); }
    public void AddSeason(SeasonType type, DateTime from, DateTime to, decimal multiplier, string? description, DateTime nowUtc) => _seasons.Add(RateSeason.Create(RatePlanId, PropertyId, type, from, to, multiplier, description, nowUtc));

    public void ConfigureYieldManagement(bool enabled, decimal minRate, decimal maxRate, int thresholdOccupancy, decimal adjustmentPct)
    {
        if (minRate < 0 || maxRate < minRate) throw new ArgumentOutOfRangeException(nameof(minRate), "Yield rate bounds are invalid.");
        if (thresholdOccupancy is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(thresholdOccupancy));
        if (adjustmentPct < 0) throw new ArgumentOutOfRangeException(nameof(adjustmentPct));
        AllowYieldManagement = enabled;
        YieldMinRate = minRate;
        YieldMaxRate = maxRate;
        YieldThresholdOccupancy = thresholdOccupancy;
        YieldAdjustmentPct = adjustmentPct;
    }

    public void EnableYieldManagement(decimal minRate, decimal maxRate, int thresholdOccupancy, decimal adjustmentPct)
        => ConfigureYieldManagement(true, minRate, maxRate, thresholdOccupancy, adjustmentPct);

    public void SetStatus(RatePlanStatus status) => Status = status;

    public void SetRuleStatus(Guid ruleId, bool isActive)
    {
        var rule = _rules.SingleOrDefault(x => x.RateRuleId == ruleId)
            ?? throw new KeyNotFoundException("Rate rule was not found.");
        rule.SetActive(isActive);
    }

    public void SetSeasonStatus(Guid seasonId, bool isActive)
    {
        var season = _seasons.SingleOrDefault(x => x.RateSeasonId == seasonId)
            ?? throw new KeyNotFoundException("Rate season was not found.");
        season.SetActive(isActive);
    }

    public (decimal finalPrice, List<string> auditLog) CalculatePrice(DateOnly date, int adults, int nights, int totalRooms, int roomsSold)
    {
        var log = new List<string>();
        var price = _calendar.FirstOrDefault(c => c.Date == date)?.Rate ?? BaseRate;
        log.Add($"[BAR] Base rate: {price:N0} VND");

        // Apply seasons
        foreach (var season in _seasons.Where(s => s.IsActive && date >= DateOnly.FromDateTime(s.FromDate) && date <= DateOnly.FromDateTime(s.ToDate)))
        { price *= season.Multiplier; log.Add($"[Season] {season.Type} (×{season.Multiplier}): {price:N0} VND"); }

        var blackout = _seasons.Any(s => s.IsActive && s.Type == SeasonType.Blackout
            && date >= DateOnly.FromDateTime(s.FromDate) && date <= DateOnly.FromDateTime(s.ToDate));
        if (blackout)
        {
            log.Add("[Blackout] Closed date: no room price is available.");
            log.Add("[FINAL] 0 VND");
            return (0, log);
        }

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

        // Yield management. When disabled, previously stored floor/ceiling values
        // must not affect the normal BAR calculation.
        if (AllowYieldManagement && totalRooms > 0)
        {
            var occupancyPct = (decimal)roomsSold / totalRooms * 100;
            if (occupancyPct > YieldThresholdOccupancy) { var adj = price * YieldAdjustmentPct / 100; price += adj; log.Add($"[Yield] Occupancy {occupancyPct:F0}% > {YieldThresholdOccupancy}%: +{YieldAdjustmentPct}% = {price:N0} VND"); }

            price = Math.Max(YieldMinRate, Math.Min(YieldMaxRate, price));
            log.Add($"[YieldBounds] {YieldMinRate:N0}–{YieldMaxRate:N0} VND");
        }

        log.Add($"[FINAL] {price:N0} VND");
        return (price, log);
    }

    /// <summary>
    /// Calculates the complete stay one night at a time so calendar rates,
    /// seasons, blackout dates and yield rules are evaluated for every night.
    /// </summary>
    public (decimal totalPrice, List<string> auditLog) CalculateStayPrice(
        DateOnly arrivalDate,
        int adults,
        int nights,
        int totalRooms,
        int roomsSold)
    {
        if (nights <= 0)
            throw new ArgumentOutOfRangeException(nameof(nights), "Stay must contain at least one night.");
        if (adults < 1)
            throw new ArgumentOutOfRangeException(nameof(adults), "At least one adult is required.");

        var total = 0m;
        var log = new List<string>();
        for (var offset = 0; offset < nights; offset++)
        {
            var date = arrivalDate.AddDays(offset);
            var (nightlyPrice, nightlyLog) = CalculatePrice(date, adults, nights, totalRooms, roomsSold);
            total += nightlyPrice;
            log.Add($"[Night {date:yyyy-MM-dd}] {nightlyPrice:N0} VND");
            log.AddRange(nightlyLog);
        }

        log.Add($"[STAY FINAL] {total:N0} VND ({nights} night(s))");
        return (total, log);
    }

    private static decimal? ApplyRule(RateRule rule, decimal price, DateOnly date, int adults, int nights)
    {
        return rule.Type switch
        {
            RateRuleType.FixedPrice => rule.Value,
            RateRuleType.PctAdjustment => price * (1 + rule.Value / 100),
            RateRuleType.WeekendSurcharge when date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday => price * (1 + rule.Value / 100),
            RateRuleType.WeekendSurcharge => null,
            RateRuleType.LengthOfStayDiscount when nights >= 7 => price * (1 - rule.Value / 100),
            RateRuleType.LengthOfStayDiscount => null,
            RateRuleType.OccupancyMultiplier => price * (1 + (adults - 1) * rule.Value / 100),
            RateRuleType.AdvanceBookingDiscount => price * (1 - rule.Value / 100),
            RateRuleType.LastMinutePricing => price * (1 + rule.Value / 100),
            RateRuleType.SeasonMultiplier => price * rule.Value,
            RateRuleType.VIPPricing => price * (1 - rule.Value / 100),
            RateRuleType.CorporatePricing => price * (1 - rule.Value / 100),
            RateRuleType.AgencyPricing => price * (1 - rule.Value / 100),
            RateRuleType.GroupPricing => price * (1 - rule.Value / 100),
            RateRuleType.HolidaySurcharge => throw new InvalidOperationException("HolidaySurcharge requires a configured holiday calendar before it can be used."),
            _ => throw new InvalidOperationException($"Rate rule '{rule.Type}' is not supported by the calculator.")
        };
    }
}

public sealed class RateCalendarEntry
{
    private RateCalendarEntry() { }
    public Guid RateCalendarEntryId { get; private set; } public Guid RatePlanId { get; private set; } public RatePlan? RatePlan { get; private set; } public Guid PropertyId { get; private set; }
    public DateOnly Date { get; private set; } public decimal Rate { get; private set; } public DateTime UpdatedAtUtc { get; private set; }
    public static RateCalendarEntry Create(Guid planId, Guid propertyId, DateOnly date, decimal rate, DateTime nowUtc)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate), "Calendar rate must be greater than zero.");
        return new() { RateCalendarEntryId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Date = date, Rate = rate, UpdatedAtUtc = nowUtc };
    }
}

public sealed class RateRule
{
    private RateRule() { }
    public Guid RateRuleId { get; private set; } public Guid RatePlanId { get; private set; } public RatePlan? RatePlan { get; private set; } public Guid PropertyId { get; private set; }
    public RateRuleType Type { get; private set; } public decimal Value { get; private set; } public int Priority { get; private set; }
    public bool Exclusive { get; private set; } public bool Combinable { get; private set; } public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; } public DateTime CreatedAtUtc { get; private set; } public DateTime? ExpiryDate { get; private set; }

    public static RateRule Create(Guid planId, Guid propertyId, RateRuleType type, decimal value, int priority, bool exclusive, bool combinable, string? description, DateTime nowUtc, DateTime? expiry = null)
    {
        if (type == RateRuleType.HolidaySurcharge)
            throw new NotSupportedException("HolidaySurcharge requires a configured holiday calendar before it can be used.");
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Rule value must be greater than zero.");
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority), "Rule priority cannot be negative.");
        var isPercentageDiscount = type is RateRuleType.LengthOfStayDiscount
            or RateRuleType.AdvanceBookingDiscount or RateRuleType.VIPPricing
            or RateRuleType.CorporatePricing or RateRuleType.AgencyPricing or RateRuleType.GroupPricing;
        if (isPercentageDiscount && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Discount rule value cannot exceed 100%.");
        if (expiry is not null && expiry.Value < nowUtc) throw new ArgumentException("Rule expiry cannot be in the past.", nameof(expiry));
        return new() { RateRuleId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Type = type, Value = value, Priority = priority, Exclusive = exclusive, Combinable = combinable, Description = description, CreatedAtUtc = nowUtc, ExpiryDate = expiry };
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}

public sealed class RateSeason
{
    private RateSeason() { }
    public Guid RateSeasonId { get; private set; } public Guid RatePlanId { get; private set; } public RatePlan? RatePlan { get; private set; } public Guid PropertyId { get; private set; }
    public SeasonType Type { get; private set; } public DateTime FromDate { get; private set; } public DateTime ToDate { get; private set; }
    public decimal Multiplier { get; private set; } public string? Description { get; private set; } public bool IsActive { get; private set; } = true;
    public static RateSeason Create(Guid planId, Guid propertyId, SeasonType type, DateTime from, DateTime to, decimal multiplier, string? description, DateTime nowUtc)
    {
        if (to.Date < from.Date) throw new ArgumentException("Season end date must not precede the start date.");
        if (type == SeasonType.Blackout && multiplier != 0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Blackout season must use a zero multiplier.");
        if (type != SeasonType.Blackout && multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Season multiplier must be greater than zero.");
        return new() { RateSeasonId = Uuid7.NewGuid(), RatePlanId = planId, PropertyId = propertyId, Type = type, FromDate = from.Date, ToDate = to.Date, Multiplier = multiplier, Description = description };
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
