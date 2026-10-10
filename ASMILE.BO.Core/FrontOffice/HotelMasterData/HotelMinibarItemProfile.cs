using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>
/// Property-scoped minibar master. The inventory Item is only the stock
/// integration link; price, VAT and par levels belong to the hotel property.
/// </summary>
public sealed class HotelMinibarItemProfile
{
    private HotelMinibarItemProfile() { }

    public Guid HotelMinibarItemProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int ItemId { get; private set; }
    public string ItemCode { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal RetailPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal StandardQuantity { get; private set; }
    public decimal MinStock { get; private set; }
    public decimal MaxStock { get; private set; }
    public string? ChargeCode { get; private set; }
    public string? ServiceChargeCode { get; private set; }
    public bool AutoPostToFolio { get; private set; }
    public bool RequiresApproval { get; private set; }
    public int ReplenishmentLeadMinutes { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelMinibarItemProfile Create(PropertyId propertyId, int itemId, string itemCode,
        string itemName, string unit, string category, decimal retailPrice, decimal vatRate,
        decimal standardQuantity, decimal minStock, decimal maxStock, string? chargeCode,
        string? serviceChargeCode, bool autoPostToFolio, bool requiresApproval, int replenishmentLeadMinutes,
        DateTime effectiveFrom, DateTime effectiveTo, DateTime nowUtc, ActorId? actorId)
    {
        if (replenishmentLeadMinutes < 0) throw new ArgumentException("HOTEL_MINIBAR_REPLENISHMENT_TIME_INVALID");
        Validate(itemId, itemCode, itemName, unit, category, retailPrice, vatRate, standardQuantity, minStock, maxStock, effectiveFrom, effectiveTo);
        return new HotelMinibarItemProfile
        {
            HotelMinibarItemProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, ItemId = itemId,
            ItemCode = itemCode.Trim().ToUpperInvariant(), ItemName = itemName.Trim(), Unit = unit.Trim(), Category = category.Trim(),
            RetailPrice = retailPrice, VatRate = vatRate, StandardQuantity = standardQuantity, MinStock = minStock, MaxStock = maxStock,
            ChargeCode = Normalize(chargeCode), ServiceChargeCode = Normalize(serviceChargeCode), AutoPostToFolio = autoPostToFolio, RequiresApproval = requiresApproval,
            ReplenishmentLeadMinutes = replenishmentLeadMinutes, EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = effectiveTo.Date, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(int itemId, string itemCode, string itemName, string unit, string category, decimal retailPrice,
        decimal vatRate, decimal standardQuantity, decimal minStock, decimal maxStock, string? chargeCode,
        string? serviceChargeCode, bool autoPostToFolio, bool requiresApproval, int replenishmentLeadMinutes,
        DateTime effectiveFrom, DateTime effectiveTo, DateTime nowUtc, ActorId? actorId)
    {
        if (replenishmentLeadMinutes < 0) throw new ArgumentException("HOTEL_MINIBAR_REPLENISHMENT_TIME_INVALID");
        Validate(itemId, itemCode, itemName, unit, category, retailPrice, vatRate, standardQuantity, minStock, maxStock, effectiveFrom, effectiveTo);
        ItemId = itemId; ItemCode = itemCode.Trim().ToUpperInvariant(); ItemName = itemName.Trim(); Unit = unit.Trim(); Category = category.Trim();
        RetailPrice = retailPrice; VatRate = vatRate; StandardQuantity = standardQuantity; MinStock = minStock; MaxStock = maxStock;
        ChargeCode = Normalize(chargeCode); ServiceChargeCode = Normalize(serviceChargeCode); AutoPostToFolio = autoPostToFolio; RequiresApproval = requiresApproval;
        ReplenishmentLeadMinutes = replenishmentLeadMinutes; EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date;
        UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value;
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }

    private static void Validate(int itemId, string code, string name, string unit, string category, decimal price, decimal vat,
        decimal standard, decimal min, decimal max, DateTime from, DateTime to)
    {
        if (itemId <= 0) throw new ArgumentException("HOTEL_MINIBAR_ITEM_LINK_REQUIRED");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_MINIBAR_ITEM_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_MINIBAR_ITEM_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("HOTEL_MINIBAR_ITEM_UNIT_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("HOTEL_MINIBAR_ITEM_CATEGORY_REQUIRED");
        if (price < 0 || vat < 0 || vat > 100 || standard <= 0 || min < 0 || max < min || standard < min || standard > max)
            throw new ArgumentException("HOTEL_MINIBAR_ITEM_NUMERIC_RANGE_INVALID");
        if (to.Date < from.Date) throw new ArgumentException("HOTEL_MINIBAR_ITEM_EFFECTIVE_RANGE_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
