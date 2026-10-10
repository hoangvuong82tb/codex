using ASMILE.BO.Application.FrontOffice.Loyalty;
using ASMILE.BO.Core.FrontOffice.Loyalty;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Loyalty;

public sealed class EfVipLoyaltyService(AppDbContext db) : IVipLoyaltyService
{
    public async Task<VipProfileQueryResult> SearchAsync(Guid propertyId, string? keyword = null, CancellationToken cancellationToken = default)
    {
        RequireProperty(propertyId);
        var query = db.VipProfiles
            .AsNoTracking()
            .Include(x => x.Transactions)
            .Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.VipNumber.Contains(value) || (x.GuestName != null && x.GuestName.Contains(value)));
        }

        var entities = await query.OrderByDescending(x => x.EnrollmentDate).Take(500).ToListAsync(cancellationToken);
        return new VipProfileQueryResult(
            entities.Select(ToDto).ToList(),
            entities.Count(x => x.Level == MembershipLevel.Standard),
            entities.Count(x => x.Level == MembershipLevel.Gold),
            entities.Count(x => x.Level == MembershipLevel.Platinum),
            entities.Count(x => x.Level == MembershipLevel.Diamond),
            entities.Count);
    }

    public async Task<VipProfileDto> EnrollAsync(EnrollVipCommand command, CancellationToken cancellationToken = default)
    {
        RequireProperty(command.PropertyId);
        if (string.IsNullOrWhiteSpace(command.GuestName) && !command.GuestId.HasValue) throw new ArgumentException("Guest is required.", nameof(command));
        var number = string.IsNullOrWhiteSpace(command.VipNumber)
            ? await GenerateNumberAsync(command.PropertyId, cancellationToken)
            : command.VipNumber.Trim();
        if (await db.VipProfiles.AnyAsync(x => x.PropertyId == command.PropertyId && x.VipNumber == number, cancellationToken))
            throw new InvalidOperationException($"VIP number {number} already exists.");

        var vip = VipProfile.Enroll(
            command.PropertyId,
            number,
            command.GuestId,
            NullIfWhiteSpace(command.GuestName),
            command.Level,
            DateTime.UtcNow,
            command.ActorId is { } actor && actor != Guid.Empty ? ASMILE.BO.Core.FrontOffice.Foundation.Identifiers.ActorId.From(actor) : null);
        db.VipProfiles.Add(vip);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(vip);
    }

    public Task EarnAsync(Guid propertyId, Guid vipProfileId, int points, string description, CancellationToken cancellationToken = default) =>
        ApplyPointsAsync(propertyId, vipProfileId, points, description, redeem: false, cancellationToken);

    public Task RedeemAsync(Guid propertyId, Guid vipProfileId, int points, string description, CancellationToken cancellationToken = default) =>
        ApplyPointsAsync(propertyId, vipProfileId, points, description, redeem: true, cancellationToken);

    private async Task ApplyPointsAsync(Guid propertyId, Guid vipProfileId, int points, string description, bool redeem, CancellationToken cancellationToken)
    {
        RequireProperty(propertyId);
        if (points <= 0) throw new ArgumentOutOfRangeException(nameof(points), "Points must be positive.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        var vip = await db.VipProfiles.Include(x => x.Transactions).SingleOrDefaultAsync(x => x.VipProfileId == vipProfileId && x.PropertyId == propertyId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ VIP.");
        if (redeem) vip.RedeemPoints(points, description.Trim(), DateTime.UtcNow);
        else vip.EarnPoints(points, description.Trim(), DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateNumberAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"VIP-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 1000)}";
            if (!await db.VipProfiles.AnyAsync(x => x.PropertyId == propertyId && x.VipNumber == number, cancellationToken)) return number;
        }
        throw new InvalidOperationException("Không thể tạo mã VIP duy nhất.");
    }

    private static VipProfileDto ToDto(VipProfile entity) => new(
        entity.VipProfileId,
        entity.PropertyId,
        entity.VipNumber,
        entity.GuestId,
        entity.GuestName,
        entity.Level,
        entity.Status,
        entity.PointsBalance,
        entity.LifetimePoints,
        entity.EnrollmentDate,
        entity.TotalStayCount,
        entity.TotalRevenue,
        entity.AverageSpend,
        entity.Preferences,
        entity.Birthday,
        entity.Transactions.OrderByDescending(x => x.OccurredAtUtc).Select(x => new LoyaltyTransactionDto(x.Type, x.Points, x.Description, x.OccurredAtUtc)).ToList());

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void RequireProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property identifier is required.", nameof(propertyId));
    }
}
