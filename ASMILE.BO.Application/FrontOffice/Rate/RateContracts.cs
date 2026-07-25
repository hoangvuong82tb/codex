using ASMILE.BO.Core.FrontOffice.Rate;

namespace ASMILE.BO.Application.FrontOffice.Rate;

public sealed record RatePlanDto(Guid RatePlanId, string PlanCode, string PlanName, Guid? RoomTypeId, decimal BaseRate, RatePlanStatus Status, RateHierarchyLevel Hierarchy, DateTime EffectiveFrom, DateTime EffectiveTo, bool AllowYield, decimal YieldMinRate, decimal YieldMaxRate, int YieldThreshold, decimal YieldAdjPct, DateTime CreatedAtUtc, byte[]? RowVersion);
public sealed record RateCalendarDto(DateOnly Date, decimal Rate);
public sealed record RateRuleDto(Guid RateRuleId, RateRuleType Type, decimal Value, int Priority, bool Exclusive, bool Combinable, bool IsActive, string? Description, DateTime? ExpiryDate);
public sealed record RateSeasonDto(Guid RateSeasonId, SeasonType Type, DateTime FromDate, DateTime ToDate, decimal Multiplier, bool IsActive, string? Description);
public sealed record RatePlanDetailDto(RatePlanDto Plan, IReadOnlyList<RateCalendarDto> Calendar, IReadOnlyList<RateRuleDto> Rules, IReadOnlyList<RateSeasonDto> Seasons);
public sealed record RateCalculationRequest(DateOnly Date, int Adults, int Nights, int TotalRooms, int RoomsSold);
public sealed record RateCalculationResult(decimal FinalPrice, decimal BaseRate, int Nights, List<string> AuditLog);
public sealed record RatePlanSearchFilter(Guid PropertyId, RatePlanStatus? Status, RateHierarchyLevel? Hierarchy, string? Keyword);

public interface IRateRepository
{
    Task<RatePlan?> GetAsync(Guid id, Guid propertyId, CancellationToken ct);
    Task<List<RatePlan>> GetAllAsync(Guid propertyId, RatePlanSearchFilter filter, CancellationToken ct);
    Task<List<RatePlanDetailDto>> GetAllDetailDtosAsync(Guid propertyId, RatePlanSearchFilter filter, CancellationToken ct);
    Task AddAsync(RatePlan plan, CancellationToken ct);
    Task<int> CountAsync(Guid propertyId, CancellationToken ct);
}
