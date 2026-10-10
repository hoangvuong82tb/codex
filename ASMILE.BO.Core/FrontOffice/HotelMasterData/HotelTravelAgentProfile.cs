using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelTravelAgentKind { TravelAgency = 1, TourOperator = 2, OnlineAgency = 3, Wholesaler = 4, CorporateAgent = 5, Other = 99 }

public sealed class HotelTravelAgentProfile
{
    private HotelTravelAgentProfile() { }
    public Guid HotelTravelAgentProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelTravelAgentKind Kind { get; private set; }
    public string? IataCode { get; private set; }
    public string? LicenseNumber { get; private set; }
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? CommissionRuleCode { get; private set; }
    public string? DefaultMarketSegmentCode { get; private set; }
    public string PaymentTerms { get; private set; } = "Prepaid";
    public decimal CreditLimit { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelTravelAgentProfile Create(PropertyId propertyId, string code, string name, HotelTravelAgentKind kind, string? iataCode, string? licenseNumber, string? contactName, string? phone, string? email, string? address, string? commissionRuleCode, string? defaultMarketSegmentCode, string paymentTerms, decimal creditLimit, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, paymentTerms, creditLimit, from, to); return new() { HotelTravelAgentProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind, IataCode = Normalize(iataCode), LicenseNumber = Normalize(licenseNumber), ContactName = Normalize(contactName), Phone = Normalize(phone), Email = Normalize(email), Address = Normalize(address), CommissionRuleCode = NormalizeCode(commissionRuleCode), DefaultMarketSegmentCode = NormalizeCode(defaultMarketSegmentCode), PaymentTerms = paymentTerms.Trim(), CreditLimit = creditLimit, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelTravelAgentKind kind, string? iataCode, string? licenseNumber, string? contactName, string? phone, string? email, string? address, string? commissionRuleCode, string? defaultMarketSegmentCode, string paymentTerms, decimal creditLimit, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, paymentTerms, creditLimit, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; IataCode = Normalize(iataCode); LicenseNumber = Normalize(licenseNumber); ContactName = Normalize(contactName); Phone = Normalize(phone); Email = Normalize(email); Address = Normalize(address); CommissionRuleCode = NormalizeCode(commissionRuleCode); DefaultMarketSegmentCode = NormalizeCode(defaultMarketSegmentCode); PaymentTerms = paymentTerms.Trim(); CreditLimit = creditLimit; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelTravelAgentKind kind, string paymentTerms, decimal creditLimit, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_TRAVEL_AGENT_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_TRAVEL_AGENT_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_TRAVEL_AGENT_KIND_INVALID"); if (string.IsNullOrWhiteSpace(paymentTerms)) throw new ArgumentException("HOTEL_TRAVEL_AGENT_PAYMENT_TERMS_REQUIRED"); if (creditLimit < 0) throw new ArgumentException("HOTEL_TRAVEL_AGENT_CREDIT_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_TRAVEL_AGENT_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
