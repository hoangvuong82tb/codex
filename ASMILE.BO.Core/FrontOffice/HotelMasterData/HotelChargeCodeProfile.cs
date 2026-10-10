using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public sealed class HotelChargeCodeProfile
{
    private HotelChargeCodeProfile() { }
    public Guid HotelChargeCodeProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string? TaxCode { get; private set; }
    public string? ServiceChargeCode { get; private set; }
    public string? RevenueDepartment { get; private set; }
    public string? RevenueAccount { get; private set; }
    public string? TaxAccount { get; private set; }
    public string? CostCenter { get; private set; }
    public bool AllowManualPosting { get; private set; }
    public bool AllowDiscount { get; private set; }
    public bool AllowVoid { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelChargeCodeProfile Create(PropertyId propertyId, string code, string name, string category, string unit, decimal price, string? taxCode, string? serviceChargeCode, string? revenueDepartment, string? revenueAccount, string? taxAccount, string? costCenter, bool allowManualPosting, bool allowDiscount, bool allowVoid, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, category, unit, price, effectiveFrom, effectiveTo);
        return new() { HotelChargeCodeProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), Category = category.Trim(), Unit = unit.Trim(), Price = price, TaxCode = Normalize(taxCode), ServiceChargeCode = Normalize(serviceChargeCode), RevenueDepartment = Normalize(revenueDepartment), RevenueAccount = Normalize(revenueAccount), TaxAccount = Normalize(taxAccount), CostCenter = Normalize(costCenter), AllowManualPosting = allowManualPosting, AllowDiscount = allowDiscount, AllowVoid = allowVoid, EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value };
    }
    public void Update(string code, string name, string category, string unit, decimal price, string? taxCode, string? serviceChargeCode, string? revenueDepartment, string? revenueAccount, string? taxAccount, string? costCenter, bool allowManualPosting, bool allowDiscount, bool allowVoid, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, category, unit, price, effectiveFrom, effectiveTo); Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); Category = category.Trim(); Unit = unit.Trim(); Price = price; TaxCode = Normalize(taxCode); ServiceChargeCode = Normalize(serviceChargeCode); RevenueDepartment = Normalize(revenueDepartment); RevenueAccount = Normalize(revenueAccount); TaxAccount = Normalize(taxAccount); CostCenter = Normalize(costCenter); AllowManualPosting = allowManualPosting; AllowDiscount = allowDiscount; AllowVoid = allowVoid; EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; Description = Normalize(description); Touch(nowUtc, actorId);
    }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); } public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string category, string unit, decimal price, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_CHARGE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("HOTEL_CHARGE_FIELDS_REQUIRED"); if (price < 0) throw new ArgumentException("HOTEL_CHARGE_PRICE_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_CHARGE_EFFECTIVE_RANGE_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
