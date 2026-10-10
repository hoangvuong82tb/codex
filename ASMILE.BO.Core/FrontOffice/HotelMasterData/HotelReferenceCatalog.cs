using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>
/// Property-scoped reference item for small Front Office catalogs.
/// Specialized aggregates (room types, rates, tax codes, folios) remain separate.
/// </summary>
public sealed class HotelReferenceCatalogItem
{
    private HotelReferenceCatalogItem() { }

    public Guid HotelReferenceCatalogItemId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CatalogKey { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelReferenceCatalogItem Create(PropertyId propertyId, string catalogKey, string code, string name, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(catalogKey, code, name);
        return new HotelReferenceCatalogItem
        {
            HotelReferenceCatalogItemId = Uuid7.NewGuid(), PropertyId = propertyId.Value,
            CatalogKey = catalogKey.Trim(), Code = code.Trim(), Name = name.Trim(), Description = Normalize(description),
            DisplayOrder = displayOrder, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string catalogKey, string code, string name, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(catalogKey, code, name);
        CatalogKey = catalogKey.Trim(); Code = code.Trim(); Name = name.Trim(); Description = Normalize(description);
        DisplayOrder = displayOrder; Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }

    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string catalogKey, string code, string name)
    {
        if (string.IsNullOrWhiteSpace(catalogKey)) throw new ArgumentException("HOTEL_CATALOG_KEY_REQUIRED");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_CATALOG_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_CATALOG_NAME_REQUIRED");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
