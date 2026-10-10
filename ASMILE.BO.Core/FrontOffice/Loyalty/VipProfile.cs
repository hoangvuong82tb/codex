using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Loyalty;

public enum MembershipLevel { Standard, Silver, Gold, Platinum, Diamond }
public enum VipStatus { Active, Inactive, Suspended, Expired }
public enum PointTransactionType { Earn, Redeem, Adjust, Expire, Transfer }

public sealed class VipProfile
{
    private readonly List<LoyaltyTransaction> _transactions = [];
    private VipProfile() { }

    public Guid VipProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string VipNumber { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public string? GuestName { get; private set; }
    public MembershipLevel Level { get; private set; }
    public VipStatus Status { get; private set; }
    public int PointsBalance { get; private set; }
    public int LifetimePoints { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public string? Preferences { get; private set; }
    public string? SpecialRequests { get; private set; }
    public DateOnly? Birthday { get; private set; }
    public DateOnly? Anniversary { get; private set; }
    public string? AssignedAccountManager { get; private set; }
    public DateOnly? LastStayDate { get; private set; }
    public int TotalStayCount { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal AverageSpend => TotalStayCount > 0 ? TotalRevenue / TotalStayCount : 0;
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<LoyaltyTransaction> Transactions => _transactions;

    public static VipProfile Enroll(Guid propertyId, string vipNumber, Guid? guestId, string? guestName, MembershipLevel level, DateTime nowUtc, ActorId? actorId) => new()
    { VipProfileId = Uuid7.NewGuid(), PropertyId = propertyId, VipNumber = vipNumber, GuestId = guestId, GuestName = guestName, Level = level, Status = VipStatus.Active, EnrollmentDate = nowUtc, CreatedAtUtc = nowUtc };

    public void EarnPoints(int points, string description, DateTime nowUtc) { PointsBalance += points; LifetimePoints += points; _transactions.Add(LoyaltyTransaction.Record(VipProfileId, PropertyId, PointTransactionType.Earn, points, description, nowUtc)); CheckUpgrade(); }
    public void RedeemPoints(int points, string description, DateTime nowUtc) { if (points > PointsBalance) throw new InvalidOperationException("LOYALTY_INSUFFICIENT_POINTS"); PointsBalance -= points; _transactions.Add(LoyaltyTransaction.Record(VipProfileId, PropertyId, PointTransactionType.Redeem, -points, description, nowUtc)); }
    public void RecordStay(decimal revenue, DateOnly stayDate, DateTime nowUtc) { TotalStayCount++; TotalRevenue += revenue; LastStayDate = stayDate; EarnPoints((int)(revenue / 1000), $"Stay points: {stayDate}", nowUtc); }
    public void UpgradeLevel(MembershipLevel newLevel, DateTime nowUtc) { if (newLevel <= Level) throw new InvalidOperationException("LOYALTY_INVALID_UPGRADE"); Level = newLevel; _transactions.Add(LoyaltyTransaction.Record(VipProfileId, PropertyId, PointTransactionType.Adjust, 0, $"Tier upgrade: {newLevel}", nowUtc)); }

    private void CheckUpgrade()
    {
        if (LifetimePoints >= 100000 && Level < MembershipLevel.Diamond) UpgradeLevel(MembershipLevel.Diamond, DateTime.UtcNow);
        else if (LifetimePoints >= 50000 && Level < MembershipLevel.Platinum) UpgradeLevel(MembershipLevel.Platinum, DateTime.UtcNow);
        else if (LifetimePoints >= 20000 && Level < MembershipLevel.Gold) UpgradeLevel(MembershipLevel.Gold, DateTime.UtcNow);
        else if (LifetimePoints >= 5000 && Level < MembershipLevel.Silver) UpgradeLevel(MembershipLevel.Silver, DateTime.UtcNow);
    }
}

public sealed class LoyaltyTransaction
{
    private LoyaltyTransaction() { }
    public Guid LoyaltyTransactionId { get; private set; } public Guid VipProfileId { get; private set; } public Guid PropertyId { get; private set; }
    public PointTransactionType Type { get; private set; } public int Points { get; private set; } public string Description { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static LoyaltyTransaction Record(Guid vipId, Guid propertyId, PointTransactionType type, int points, string description, DateTime nowUtc) => new()
    { LoyaltyTransactionId = Uuid7.NewGuid(), VipProfileId = vipId, PropertyId = propertyId, Type = type, Points = points, Description = description, OccurredAtUtc = nowUtc };
}
