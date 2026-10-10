using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped promotion/package master. It stores commercial eligibility, not reservation transactions.</summary>
public sealed class HotelPromotionPackage
{
    private HotelPromotionPackage() { }
    public Guid HotelPromotionPackageId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string OfferType { get; private set; } = string.Empty;
    public string DiscountMode { get; private set; } = string.Empty;
    public decimal DiscountValue { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public DateTime? BookingFrom { get; private set; }
    public DateTime? BookingTo { get; private set; }
    public int MinimumNights { get; private set; }
    public bool RequiresPrepayment { get; private set; }
    public string? IncludedServices { get; private set; }
    public string? BlackoutDates { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelPromotionPackage Create(PropertyId propertyId, string code, string name, string offerType, string discountMode, decimal discountValue, DateTime effectiveFrom, DateTime effectiveTo, DateTime? bookingFrom, DateTime? bookingTo, int minimumNights, bool requiresPrepayment, string? includedServices, string? blackoutDates, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, offerType, discountMode, discountValue, effectiveFrom, effectiveTo, bookingFrom, bookingTo, minimumNights); return new HotelPromotionPackage { HotelPromotionPackageId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), OfferType = offerType.Trim(), DiscountMode = discountMode.Trim(), DiscountValue = discountValue, EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, BookingFrom = bookingFrom?.Date, BookingTo = bookingTo?.Date, MinimumNights = minimumNights, RequiresPrepayment = requiresPrepayment, IncludedServices = Normalize(includedServices), BlackoutDates = Normalize(blackoutDates), Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string offerType, string discountMode, decimal discountValue, DateTime effectiveFrom, DateTime effectiveTo, DateTime? bookingFrom, DateTime? bookingTo, int minimumNights, bool requiresPrepayment, string? includedServices, string? blackoutDates, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, offerType, discountMode, discountValue, effectiveFrom, effectiveTo, bookingFrom, bookingTo, minimumNights); Code = code.Trim(); Name = name.Trim(); OfferType = offerType.Trim(); DiscountMode = discountMode.Trim(); DiscountValue = discountValue; EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; BookingFrom = bookingFrom?.Date; BookingTo = bookingTo?.Date; MinimumNights = minimumNights; RequiresPrepayment = requiresPrepayment; IncludedServices = Normalize(includedServices); BlackoutDates = Normalize(blackoutDates); Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string type, string mode, decimal value, DateTime from, DateTime to, DateTime? bookingFrom, DateTime? bookingTo, int minimumNights)
    { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_NAME_REQUIRED"); if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_TYPE_REQUIRED"); if (string.IsNullOrWhiteSpace(mode)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_DISCOUNT_MODE_REQUIRED"); if (to.Date < from.Date || (bookingFrom is not null && bookingTo is not null && bookingTo.Value.Date < bookingFrom.Value.Date)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_DATE_RANGE_INVALID"); if (minimumNights < 1 || value < 0 || (mode.Equals("Percent", StringComparison.OrdinalIgnoreCase) && value > 100)) throw new ArgumentException("HOTEL_PROMOTION_PACKAGE_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
