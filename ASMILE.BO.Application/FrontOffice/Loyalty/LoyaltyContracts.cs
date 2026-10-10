using ASMILE.BO.Core.FrontOffice.Loyalty;

namespace ASMILE.BO.Application.FrontOffice.Loyalty;

public sealed record LoyaltyTransactionDto(
    PointTransactionType Type,
    int Points,
    string Description,
    DateTime OccurredAtUtc);

public sealed record VipProfileDto(
    Guid VipProfileId,
    Guid PropertyId,
    string VipNumber,
    Guid? GuestId,
    string? GuestName,
    MembershipLevel Level,
    VipStatus Status,
    int PointsBalance,
    int LifetimePoints,
    DateTime EnrollmentDate,
    int TotalStayCount,
    decimal TotalRevenue,
    decimal AverageSpend,
    string? Preferences,
    DateOnly? Birthday,
    IReadOnlyList<LoyaltyTransactionDto> Transactions);

public sealed record VipProfileQueryResult(
    IReadOnlyList<VipProfileDto> Members,
    int TotalStandard,
    int TotalGold,
    int TotalPlatinum,
    int TotalDiamond,
    int TotalCount);

public sealed record EnrollVipCommand(
    Guid PropertyId,
    string VipNumber,
    Guid? GuestId,
    string? GuestName,
    MembershipLevel Level,
    Guid? ActorId);

public interface IVipLoyaltyService
{
    Task<VipProfileQueryResult> SearchAsync(Guid propertyId, string? keyword = null, CancellationToken cancellationToken = default);
    Task<VipProfileDto> EnrollAsync(EnrollVipCommand command, CancellationToken cancellationToken = default);
    Task EarnAsync(Guid propertyId, Guid vipProfileId, int points, string description, CancellationToken cancellationToken = default);
    Task RedeemAsync(Guid propertyId, Guid vipProfileId, int points, string description, CancellationToken cancellationToken = default);
}
