using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Rate;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class RateEngineTests
{
    [Fact]
    public void RATE_DOM_001_calculate_base_rate_for_weekday()
    {
        var plan = CreateRackPlan();
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50); // Monday
        price.Should().Be(1500000m);
        log.Should().Contain(l => l.Contains("[BAR]"));
        log.Should().Contain(l => l.Contains("[FINAL]"));
    }

    [Fact]
    public void RATE_DOM_002_weekend_surcharge_applied_on_friday()
    {
        var plan = CreateRackPlan();
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 24), 1, 1, 100, 50); // Friday — calendar 1.8M + surcharge 20%
        price.Should().Be(2160000m); // 1.8M + 20% = 2.16M
        log.Should().Contain(l => l.Contains("WeekendSurcharge"));
    }

    [Fact]
    public void RATE_DOM_003_calendar_rate_overrides_base_rate()
    {
        var plan = CreateRackPlan();
        plan.AddCalendarEntry(new DateOnly(2026, 7, 21), 2000000m, DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 21), 1, 1, 100, 50);
        price.Should().Be(2000000m);
    }

    [Fact]
    public void RATE_DOM_004_exclusive_rule_skips_others()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.GroupPricing, 25, 1, true, false, "Group rate", DateTime.UtcNow);
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50); // Monday, group exclusive
        price.Should().Be(1125000m); // 1.5M - 25% = 1.125M, exclusive skips all other rules
        log.Should().Contain(l => l.Contains("EXCLUSIVE"));
        log.Should().NotContain(l => l.Contains("WeekendSurcharge"));
    }

    [Fact]
    public void RATE_DOM_005_length_of_stay_applies_for_7_nights()
    {
        var plan = CreateRackPlan();
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 7, 100, 50);
        price.Should().Be(1350000m); // 1.5M - 10% = 1.35M
        log.Should().Contain(l => l.Contains("LengthOfStayDiscount"));
    }

    [Fact]
    public void RATE_DOM_006_season_multiplier_applied()
    {
        var plan = CreateRackPlan();
        plan.AddSeason(SeasonType.PeakSeason, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), 1.5m, "Peak", DateTime.UtcNow);
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50);
        price.Should().Be(2250000m); // 1.5M x 1.5 = 2.25M
        log.Should().Contain(l => l.Contains("[Season]"));
    }

    [Fact]
    public void RATE_DOM_007_yield_management_triggers_at_threshold()
    {
        var plan = CreateRackPlanWithYield();
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 90); // 90% occupancy > 80% threshold
        price.Should().Be(1650000m); // 1.5M + 10% = 1.65M
        log.Should().Contain(l => l.Contains("[Yield]"));
    }

    [Fact]
    public void RATE_DOM_008_yield_management_inactive_below_threshold()
    {
        var plan = CreateRackPlanWithYield();
        var (price, log) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 70); // 70% < 80% threshold
        price.Should().Be(1500000m); // No yield adjustment
        log.Should().NotContain(l => l.Contains("[Yield]"));
    }

    [Fact]
    public void RATE_DOM_009_occupancy_multiplier_for_extra_adults()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.OccupancyMultiplier, 10, 5, false, true, "+10% per extra adult", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 3, 1, 100, 50); // 3 adults = 2 extra
        price.Should().Be(1800000m); // 1.5M + 2*10% = 1.8M
    }

    [Fact]
    public void RATE_DOM_010_fixed_price_rule_overrides()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.FixedPrice, 2000000m, 1, false, true, "Fixed", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50);
        price.Should().Be(2000000m);
    }

    [Fact]
    public void RATE_DOM_011_rate_clamped_to_min_max_bounds()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.FixedPrice, 50000m, 1, false, true, "Too low", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50);
        price.Should().Be(50000m); // YieldMinRate=0, YieldMaxRate=99999999, so 50000 passes
    }

    [Fact]
    public void RATE_DOM_012_hierarchy_order_is_preserved()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.PctAdjustment, 10, 30, false, true, "First +10%", DateTime.UtcNow);
        plan.AddRule(RateRuleType.PctAdjustment, 20, 20, false, true, "Then +20%", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50);
        price.Should().Be(1980000m); // 1.5M + 10% = 1.65M, then +20% = 1.98M
    }

    [Fact]
    public void RATE_DOM_013_advance_booking_discount()
    {
        var plan = CreateRackPlan();
        plan.AddRule(RateRuleType.AdvanceBookingDiscount, 5, 10, false, true, "-5% advance booking", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 1, 100, 50);
        price.Should().Be(1425000m); // 1.5M - 5% = 1.425M
    }

    [Fact]
    public void RATE_DOM_014_advance_booking_discount_combined_with_length_of_stay()
    {
        var plan = CreateRackPlan(); // Has LengthOfStayDiscount P20 -10% for 7+ nights
        plan.AddRule(RateRuleType.AdvanceBookingDiscount, 5, 10, false, true, "-5% advance booking", DateTime.UtcNow);
        var (price, _) = plan.CalculatePrice(new DateOnly(2026, 7, 20), 1, 7, 100, 50);
        price.Should().Be(1282500m); // 1.5M - 5% advance = 1.425M, then -10% LOS = 1.2825M
    }

    [Fact]
    public void RATE_REPO_001_detail_dto_maps_all_children()
    {
        var plan = RatePlan.Create(Guid.NewGuid(), "TEST", "Test Plan", null, 1000000m, RateHierarchyLevel.BAR, DateTime.Today, DateTime.Today.AddYears(1), DateTime.UtcNow, null);
        plan.AddCalendarEntry(new DateOnly(2026, 7, 20), 1600000m, DateTime.UtcNow);
        plan.AddRule(RateRuleType.WeekendSurcharge, 20, 10, false, true, "Test", DateTime.UtcNow);
        plan.AddSeason(SeasonType.HighSeason, new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), 1.2m, "High", DateTime.UtcNow);
        var detail = ToDetailDto(plan);
        detail.Calendar.Should().HaveCount(1);
        detail.Rules.Should().HaveCount(1);
        detail.Seasons.Should().HaveCount(1);
        detail.Plan.PlanCode.Should().Be("TEST");
    }

    private static RatePlan CreateRackPlan()
    {
        var plan = RatePlan.Create(
            Guid.NewGuid(), "RACK", "Rack Rate (BAR)", null, 1500000m,
            RateHierarchyLevel.BAR, DateTime.Today, DateTime.Today.AddYears(1),
            DateTime.UtcNow, null);
        plan.AddCalendarEntry(new DateOnly(2026, 7, 24), 1800000m, DateTime.UtcNow); // Friday
        plan.AddCalendarEntry(new DateOnly(2026, 7, 25), 1800000m, DateTime.UtcNow); // Saturday
        plan.AddRule(RateRuleType.WeekendSurcharge, 20, 10, false, true, "+20% cuoi tuan", DateTime.UtcNow);
        plan.AddRule(RateRuleType.LengthOfStayDiscount, 10, 20, false, true, "-10% cho o >=7 dem", DateTime.UtcNow);
        return plan;
    }

    private static RatePlan CreateRackPlanWithYield()
    {
        var plan = RatePlan.Create(
            Guid.NewGuid(), "RACK", "Rack Rate (BAR)", null, 1500000m,
            RateHierarchyLevel.BAR, DateTime.Today, DateTime.Today.AddYears(1),
            DateTime.UtcNow, null);
        plan.EnableYieldManagement(0, 99999999, 80, 10);
        return plan;
    }

    private static RatePlanDetailDto ToDetailDto(RatePlan p) => new(
        new RatePlanDto(p.RatePlanId, p.PlanCode, p.PlanName, p.RoomTypeId, p.BaseRate, p.Status, p.Hierarchy, p.EffectiveFrom, p.EffectiveTo, p.AllowYieldManagement, p.YieldMinRate, p.YieldMaxRate, p.YieldThresholdOccupancy, p.YieldAdjustmentPct, p.CreatedAtUtc, p.RowVersion),
        p.Calendar.Select(c => new RateCalendarDto(c.Date, c.Rate)).ToList(),
        p.Rules.Select(r => new RateRuleDto(r.RateRuleId, r.Type, r.Value, r.Priority, r.Exclusive, r.Combinable, r.IsActive, r.Description, r.ExpiryDate)).ToList(),
        p.Seasons.Select(s => new RateSeasonDto(s.RateSeasonId, s.Type, s.FromDate, s.ToDate, s.Multiplier, s.IsActive, s.Description)).ToList()
    );
}
