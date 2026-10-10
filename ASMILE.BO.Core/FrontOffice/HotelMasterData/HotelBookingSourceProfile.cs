using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelBookingSourceKind
{
    WalkIn = 1, Phone = 2, Email = 3, Website = 4, Facebook = 5, Zalo = 6,
    Ota = 7, TravelAgent = 8, Corporate = 9, Referral = 10, RepeatGuest = 11
}

public sealed class HotelBookingSourceProfile
{
    private HotelBookingSourceProfile() { }
    public Guid HotelBookingSourceProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelBookingSourceKind Kind { get; private set; }
    public bool IsCommissionable { get; private set; }
    public bool RequiresReference { get; private set; }
    public string? DefaultMarketSegmentCode { get; private set; }
    public string? TrackingCode { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelBookingSourceProfile Create(PropertyId propertyId, string code, string name, HotelBookingSourceKind kind, bool isCommissionable, bool requiresReference, string? defaultMarketSegmentCode, string? trackingCode, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, kind, from, to);
        return new()
        {
            HotelBookingSourceProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind,
            IsCommissionable = isCommissionable, RequiresReference = requiresReference, DefaultMarketSegmentCode = NormalizeCode(defaultMarketSegmentCode),
            TrackingCode = Normalize(trackingCode), EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true,
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, HotelBookingSourceKind kind, bool isCommissionable, bool requiresReference, string? defaultMarketSegmentCode, string? trackingCode, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, kind, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; IsCommissionable = isCommissionable; RequiresReference = requiresReference;
        DefaultMarketSegmentCode = NormalizeCode(defaultMarketSegmentCode); TrackingCode = Normalize(trackingCode); EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId);
    }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelBookingSourceKind kind, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_BOOKING_SOURCE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_BOOKING_SOURCE_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_BOOKING_SOURCE_KIND_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_BOOKING_SOURCE_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
