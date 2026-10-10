using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelCompanyKind { Corporate = 1, Government = 2, Education = 3, Medical = 4, Wholesale = 5, Supplier = 6, Other = 99 }

public sealed class HotelCompanyProfile
{
    private HotelCompanyProfile() { }
    public Guid HotelCompanyProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelCompanyKind Kind { get; private set; }
    public string? TaxCode { get; private set; }
    public string? BusinessLicense { get; private set; }
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public decimal CreditLimit { get; private set; }
    public int CreditDays { get; private set; }
    public bool RequireContract { get; private set; }
    public bool RequirePurchaseOrder { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelCompanyProfile Create(PropertyId propertyId, string code, string name, HotelCompanyKind kind, string? taxCode, string? businessLicense, string? contactName, string? phone, string? email, string? address, string currencyCode, decimal creditLimit, int creditDays, bool requireContract, bool requirePurchaseOrder, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, currencyCode, creditLimit, creditDays, from, to); return new() { HotelCompanyProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind, TaxCode = Normalize(taxCode), BusinessLicense = Normalize(businessLicense), ContactName = Normalize(contactName), Phone = Normalize(phone), Email = Normalize(email), Address = Normalize(address), CurrencyCode = CodeValue(currencyCode), CreditLimit = creditLimit, CreditDays = creditDays, RequireContract = requireContract, RequirePurchaseOrder = requirePurchaseOrder, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelCompanyKind kind, string? taxCode, string? businessLicense, string? contactName, string? phone, string? email, string? address, string currencyCode, decimal creditLimit, int creditDays, bool requireContract, bool requirePurchaseOrder, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, currencyCode, creditLimit, creditDays, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; TaxCode = Normalize(taxCode); BusinessLicense = Normalize(businessLicense); ContactName = Normalize(contactName); Phone = Normalize(phone); Email = Normalize(email); Address = Normalize(address); CurrencyCode = CodeValue(currencyCode); CreditLimit = creditLimit; CreditDays = creditDays; RequireContract = requireContract; RequirePurchaseOrder = requirePurchaseOrder; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelCompanyKind kind, string currencyCode, decimal creditLimit, int creditDays, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_COMPANY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_COMPANY_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_COMPANY_KIND_INVALID"); if (string.IsNullOrWhiteSpace(currencyCode)) throw new ArgumentException("HOTEL_COMPANY_CURRENCY_REQUIRED"); if (creditLimit < 0 || creditDays < 0) throw new ArgumentException("HOTEL_COMPANY_CREDIT_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_COMPANY_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
