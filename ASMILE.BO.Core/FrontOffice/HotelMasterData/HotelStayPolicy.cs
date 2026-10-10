using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped stay policy used by reservation/check-in pricing decisions.</summary>
public sealed class HotelStayPolicy
{
    private HotelStayPolicy() { }
    public Guid HotelStayPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public int CancellationDeadlineHours { get; private set; }
    public int NoShowChargeNights { get; private set; }
    public decimal DepositPercent { get; private set; }
    public int MinimumNights { get; private set; }
    public int MaximumNights { get; private set; }
    public bool ChildrenAllowed { get; private set; }
    public bool PetsAllowed { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelStayPolicy Create(PropertyId propertyId, string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int cancellationDeadlineHours, int noShowChargeNights, decimal depositPercent, int minimumNights, int maximumNights, bool childrenAllowed, bool petsAllowed, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, effectiveFrom, effectiveTo, cancellationDeadlineHours, noShowChargeNights, depositPercent, minimumNights, maximumNights);
        return new HotelStayPolicy { HotelStayPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, CancellationDeadlineHours = cancellationDeadlineHours, NoShowChargeNights = noShowChargeNights, DepositPercent = depositPercent, MinimumNights = minimumNights, MaximumNights = maximumNights, ChildrenAllowed = childrenAllowed, PetsAllowed = petsAllowed, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }
    public void Update(string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int cancellationDeadlineHours, int noShowChargeNights, decimal depositPercent, int minimumNights, int maximumNights, bool childrenAllowed, bool petsAllowed, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, cancellationDeadlineHours, noShowChargeNights, depositPercent, minimumNights, maximumNights); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; CancellationDeadlineHours = cancellationDeadlineHours; NoShowChargeNights = noShowChargeNights; DepositPercent = depositPercent; MinimumNights = minimumNights; MaximumNights = maximumNights; ChildrenAllowed = childrenAllowed; PetsAllowed = petsAllowed; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, int cancellation, int noShow, decimal deposit, int minNights, int maxNights)
    { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_STAY_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_STAY_POLICY_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_STAY_POLICY_EFFECTIVE_RANGE_INVALID"); if (cancellation < 0 || noShow < 0 || deposit is < 0 or > 100 || minNights < 1 || maxNights < minNights) throw new ArgumentException("HOTEL_STAY_POLICY_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
