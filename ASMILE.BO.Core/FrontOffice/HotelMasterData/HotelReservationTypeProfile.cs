using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelReservationTypeKind { Individual = 1, Group = 2, Corporate = 3, Agent = 4, Ota = 5, Complimentary = 6, HouseUse = 7 }

public sealed class HotelReservationTypeProfile
{
    private HotelReservationTypeProfile() { }
    public Guid HotelReservationTypeProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelReservationTypeKind Kind { get; private set; }
    public bool AutoConfirm { get; private set; }
    public bool AutoCancel { get; private set; }
    public bool DepositRequired { get; private set; }
    public bool GuaranteeRequired { get; private set; }
    public int HoldMinutes { get; private set; }
    public int CutoffHours { get; private set; }
    public int MinimumStayNights { get; private set; }
    public int MaximumStayNights { get; private set; }
    public bool AllowOverbooking { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelReservationTypeProfile Create(PropertyId propertyId, string code, string name, HotelReservationTypeKind kind, bool autoConfirm, bool autoCancel, bool depositRequired, bool guaranteeRequired, int holdMinutes, int cutoffHours, int minimumStayNights, int maximumStayNights, bool allowOverbooking, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, holdMinutes, cutoffHours, minimumStayNights, maximumStayNights, from, to); return new() { HotelReservationTypeProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind, AutoConfirm = autoConfirm, AutoCancel = autoCancel, DepositRequired = depositRequired, GuaranteeRequired = guaranteeRequired, HoldMinutes = holdMinutes, CutoffHours = cutoffHours, MinimumStayNights = minimumStayNights, MaximumStayNights = maximumStayNights, AllowOverbooking = allowOverbooking, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelReservationTypeKind kind, bool autoConfirm, bool autoCancel, bool depositRequired, bool guaranteeRequired, int holdMinutes, int cutoffHours, int minimumStayNights, int maximumStayNights, bool allowOverbooking, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, holdMinutes, cutoffHours, minimumStayNights, maximumStayNights, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; AutoConfirm = autoConfirm; AutoCancel = autoCancel; DepositRequired = depositRequired; GuaranteeRequired = guaranteeRequired; HoldMinutes = holdMinutes; CutoffHours = cutoffHours; MinimumStayNights = minimumStayNights; MaximumStayNights = maximumStayNights; AllowOverbooking = allowOverbooking; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelReservationTypeKind kind, int holdMinutes, int cutoffHours, int minimumStay, int maximumStay, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_RESERVATION_TYPE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_RESERVATION_TYPE_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_RESERVATION_TYPE_KIND_INVALID"); if (holdMinutes < 0) throw new ArgumentException("HOTEL_RESERVATION_TYPE_HOLD_INVALID"); if (cutoffHours < 0) throw new ArgumentException("HOTEL_RESERVATION_TYPE_CUTOFF_INVALID"); if (minimumStay < 1 || maximumStay < minimumStay) throw new ArgumentException("HOTEL_RESERVATION_TYPE_STAY_RANGE_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_RESERVATION_TYPE_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
