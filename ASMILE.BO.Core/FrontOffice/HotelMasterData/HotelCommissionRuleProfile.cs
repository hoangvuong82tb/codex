using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelCommissionPartnerKind { TravelAgent = 1, Company = 2, Channel = 3 }
public enum HotelCommissionBasis { RoomOnly = 1, RoomAndTax = 2, Gross = 3, Net = 4, FixedPerNight = 5, FixedPerBooking = 6 }

public sealed class HotelCommissionRuleProfile
{
    private HotelCommissionRuleProfile() { }
    public Guid HotelCommissionRuleProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelCommissionPartnerKind PartnerKind { get; private set; }
    public HotelCommissionBasis Basis { get; private set; }
    public decimal RatePercent { get; private set; }
    public decimal FixedAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public bool IncludeTax { get; private set; }
    public bool IncludeServiceCharge { get; private set; }
    public decimal? MinimumAmount { get; private set; }
    public decimal? MaximumAmount { get; private set; }
    public bool ApprovalRequired { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelCommissionRuleProfile Create(PropertyId propertyId, string code, string name, HotelCommissionPartnerKind partnerKind, HotelCommissionBasis basis, decimal ratePercent, decimal fixedAmount, string currencyCode, bool includeTax, bool includeServiceCharge, decimal? minimumAmount, decimal? maximumAmount, bool approvalRequired, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, partnerKind, basis, ratePercent, fixedAmount, currencyCode, minimumAmount, maximumAmount, from, to); return new() { HotelCommissionRuleProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), PartnerKind = partnerKind, Basis = basis, RatePercent = ratePercent, FixedAmount = fixedAmount, CurrencyCode = CodeValue(currencyCode), IncludeTax = includeTax, IncludeServiceCharge = includeServiceCharge, MinimumAmount = minimumAmount, MaximumAmount = maximumAmount, ApprovalRequired = approvalRequired, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelCommissionPartnerKind partnerKind, HotelCommissionBasis basis, decimal ratePercent, decimal fixedAmount, string currencyCode, bool includeTax, bool includeServiceCharge, decimal? minimumAmount, decimal? maximumAmount, bool approvalRequired, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, partnerKind, basis, ratePercent, fixedAmount, currencyCode, minimumAmount, maximumAmount, from, to); Code = CodeValue(code); Name = name.Trim(); PartnerKind = partnerKind; Basis = basis; RatePercent = ratePercent; FixedAmount = fixedAmount; CurrencyCode = CodeValue(currencyCode); IncludeTax = includeTax; IncludeServiceCharge = includeServiceCharge; MinimumAmount = minimumAmount; MaximumAmount = maximumAmount; ApprovalRequired = approvalRequired; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelCommissionPartnerKind partnerKind, HotelCommissionBasis basis, decimal ratePercent, decimal fixedAmount, string currencyCode, decimal? minimumAmount, decimal? maximumAmount, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_COMMISSION_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_COMMISSION_NAME_REQUIRED"); if (!Enum.IsDefined(partnerKind) || !Enum.IsDefined(basis)) throw new ArgumentException("HOTEL_COMMISSION_KIND_INVALID"); if (ratePercent < 0 || ratePercent > 100 || fixedAmount < 0) throw new ArgumentException("HOTEL_COMMISSION_VALUE_INVALID"); if (string.IsNullOrWhiteSpace(currencyCode)) throw new ArgumentException("HOTEL_COMMISSION_CURRENCY_REQUIRED"); if (minimumAmount is < 0 || maximumAmount is < 0 || (minimumAmount.HasValue && maximumAmount.HasValue && maximumAmount < minimumAmount)) throw new ArgumentException("HOTEL_COMMISSION_LIMIT_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_COMMISSION_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
