using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelBookingChannelKind
{
    Direct = 1, Website = 2, BookingEngine = 3, Ota = 4, Gds = 5, TravelAgent = 6, Corporate = 7, CallCenter = 8
}

public sealed class HotelBookingChannelProfile
{
    private HotelBookingChannelProfile() { }
    public Guid HotelBookingChannelProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelBookingChannelKind Kind { get; private set; }
    public bool IsOnline { get; private set; }
    public bool RequiresExternalConfirmation { get; private set; }
    public string? ConnectorKey { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelBookingChannelProfile Create(PropertyId propertyId, string code, string name, HotelBookingChannelKind kind, bool isOnline, bool requiresExternalConfirmation, string? connectorKey, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, from, to); return new() { HotelBookingChannelProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind, IsOnline = isOnline, RequiresExternalConfirmation = requiresExternalConfirmation, ConnectorKey = NormalizeCode(connectorKey), EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelBookingChannelKind kind, bool isOnline, bool requiresExternalConfirmation, string? connectorKey, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; IsOnline = isOnline; RequiresExternalConfirmation = requiresExternalConfirmation; ConnectorKey = NormalizeCode(connectorKey); EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelBookingChannelKind kind, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_BOOKING_CHANNEL_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_BOOKING_CHANNEL_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_BOOKING_CHANNEL_KIND_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_BOOKING_CHANNEL_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
