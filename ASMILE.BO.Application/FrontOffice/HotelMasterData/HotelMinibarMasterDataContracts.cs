using ASMILE.BO.Core.FrontOffice.HotelMasterData;

namespace ASMILE.BO.Application.FrontOffice.HotelMasterData;

public sealed record HotelMinibarItemProfileDto(Guid HotelMinibarItemProfileId, int ItemId, string ItemCode, string ItemName, string Unit,
    string Category, decimal RetailPrice, decimal VatRate, decimal StandardQuantity, decimal MinStock, decimal MaxStock,
    string? ChargeCode, string? ServiceChargeCode, bool AutoPostToFolio, bool RequiresApproval, int ReplenishmentLeadMinutes,
    DateTime EffectiveFrom, DateTime EffectiveTo, bool IsActive,
    DateTime UpdatedAtUtc, byte[]? RowVersion);

public sealed record HotelMinibarItemProfileInput(Guid? HotelMinibarItemProfileId, int ItemId, string ItemCode, string ItemName,
    string Unit, string Category, decimal RetailPrice, decimal VatRate, decimal StandardQuantity, decimal MinStock, decimal MaxStock,
    string? ChargeCode, string? ServiceChargeCode, bool AutoPostToFolio, bool RequiresApproval, int ReplenishmentLeadMinutes,
    DateTime EffectiveFrom, DateTime EffectiveTo, byte[]? ExpectedRowVersion);

public interface IHotelMinibarMasterDataService
{
    Task<IReadOnlyList<HotelMinibarItemProfileDto>> SearchAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default);
    Task<HotelMinibarItemProfileDto> SaveAsync(Guid propertyId, HotelMinibarItemProfileInput input, Guid? actorId, CancellationToken ct = default);
    Task<HotelMinibarItemProfileDto> SetActiveAsync(Guid propertyId, Guid id, bool isActive, byte[]? expectedRowVersion, Guid? actorId, CancellationToken ct = default);
}
