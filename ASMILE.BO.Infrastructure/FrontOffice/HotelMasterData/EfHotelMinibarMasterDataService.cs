using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;

public sealed class EfHotelMinibarMasterDataService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IFrontOfficePersistenceSession persistence,
    IClock clock) : IHotelMinibarMasterDataService
{
    public async Task<IReadOnlyList<HotelMinibarItemProfileDto>> SearchAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var query = db.HotelMinibarItemProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.ItemCode.Contains(keyword) || x.ItemName.Contains(keyword) || x.Category.Contains(keyword));
        }
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive);
        return await query.OrderBy(x => x.Category).ThenBy(x => x.ItemCode).Select(ToDto).ToListAsync(ct);
    }

    public async Task<HotelMinibarItemProfileDto> SaveAsync(Guid propertyId, HotelMinibarItemProfileInput input, Guid? actorId, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var itemCode = input.ItemCode.Trim().ToUpperInvariant();
        var inventoryItem = input.ItemId > 0
            ? await db.Items.SingleOrDefaultAsync(x => x.Id == input.ItemId && x.IsActive, ct)
            : await db.Items.SingleOrDefaultAsync(x => x.ItemCode == itemCode && x.IsActive, ct);
        if (inventoryItem is null) throw new InvalidOperationException("HOTEL_MINIBAR_INVENTORY_ITEM_NOT_FOUND");
        if (!string.Equals(inventoryItem.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("HOTEL_MINIBAR_ITEM_CODE_LINK_MISMATCH");

        var propertyIdValue = propertyId;
        HotelMinibarItemProfile entity;
        var isNew = !input.HotelMinibarItemProfileId.HasValue || input.HotelMinibarItemProfileId.Value == Guid.Empty;
        var actor = actorId.HasValue ? new ActorId(actorId.Value) : (ActorId?)null;
        if (input.HotelMinibarItemProfileId is { } id && id != Guid.Empty)
        {
            entity = await db.HotelMinibarItemProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyIdValue && x.HotelMinibarItemProfileId == id, ct)
                ?? throw new InvalidOperationException("HOTEL_MINIBAR_PROFILE_NOT_FOUND");
            if (input.ExpectedRowVersion is { Length: > 0 }) db.Entry(entity).Property("RowVersion").OriginalValue = input.ExpectedRowVersion;
            if (await db.HotelMinibarItemProfiles.AnyAsync(x => x.PropertyId == propertyIdValue && x.ItemCode == itemCode && x.HotelMinibarItemProfileId != id, ct))
                throw new InvalidOperationException("HOTEL_MINIBAR_ITEM_CODE_CONFLICT");
            entity.Update(inventoryItem.Id, inventoryItem.ItemCode, string.IsNullOrWhiteSpace(input.ItemName) ? inventoryItem.ItemName : input.ItemName,
                string.IsNullOrWhiteSpace(input.Unit) ? inventoryItem.Unit : input.Unit, input.Category, input.RetailPrice, input.VatRate,
                input.StandardQuantity, input.MinStock, input.MaxStock, input.ChargeCode, input.ServiceChargeCode, input.AutoPostToFolio, input.RequiresApproval, input.ReplenishmentLeadMinutes,
                input.EffectiveFrom, input.EffectiveTo, clock.UtcNow, actor);
        }
        else
        {
            if (await db.HotelMinibarItemProfiles.AnyAsync(x => x.PropertyId == propertyIdValue && x.ItemCode == itemCode, ct))
                throw new InvalidOperationException("HOTEL_MINIBAR_ITEM_CODE_CONFLICT");
            entity = HotelMinibarItemProfile.Create(new PropertyId(propertyIdValue), inventoryItem.Id, inventoryItem.ItemCode,
                string.IsNullOrWhiteSpace(input.ItemName) ? inventoryItem.ItemName : input.ItemName,
                string.IsNullOrWhiteSpace(input.Unit) ? inventoryItem.Unit : input.Unit, input.Category, input.RetailPrice, input.VatRate,
                input.StandardQuantity, input.MinStock, input.MaxStock, input.ChargeCode, input.ServiceChargeCode, input.AutoPostToFolio, input.RequiresApproval, input.ReplenishmentLeadMinutes,
                input.EffectiveFrom, input.EffectiveTo, clock.UtcNow, actor);
            db.HotelMinibarItemProfiles.Add(entity);
        }

        persistence.StageAudit(new FrontOfficeAuditIntent(
            isNew ? "Created" : "Updated",
            nameof(HotelMinibarItemProfile), entity.HotelMinibarItemProfileId.ToString(), "Succeeded"));
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) throw new InvalidOperationException(commit.Error?.Message ?? "HOTEL_MINIBAR_PROFILE_SAVE_FAILED");
        return await db.HotelMinibarItemProfiles.AsNoTracking().Where(x => x.PropertyId == propertyIdValue && x.HotelMinibarItemProfileId == entity.HotelMinibarItemProfileId).Select(ToDto).SingleAsync(ct);
    }

    public async Task<HotelMinibarItemProfileDto> SetActiveAsync(Guid propertyId, Guid id, bool isActive, byte[]? expectedRowVersion, Guid? actorId, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var entity = await db.HotelMinibarItemProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelMinibarItemProfileId == id, ct)
            ?? throw new InvalidOperationException("HOTEL_MINIBAR_PROFILE_NOT_FOUND");
        if (expectedRowVersion is { Length: > 0 }) db.Entry(entity).Property("RowVersion").OriginalValue = expectedRowVersion;
        var actor = actorId.HasValue ? new ActorId(actorId.Value) : (ActorId?)null;
        if (isActive) entity.Activate(clock.UtcNow, actor); else entity.Deactivate(clock.UtcNow, actor);
        persistence.StageAudit(new FrontOfficeAuditIntent(isActive ? "Activated" : "Deactivated", nameof(HotelMinibarItemProfile), id.ToString(), "Succeeded"));
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) throw new InvalidOperationException(commit.Error?.Message ?? "HOTEL_MINIBAR_PROFILE_STATUS_FAILED");
        return await db.HotelMinibarItemProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelMinibarItemProfileId == id).Select(ToDto).SingleAsync(ct);
    }

    private void EnsureProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || contextAccessor.Current?.Property.PropertyId.Value != propertyId)
            throw new InvalidOperationException("FO_PROPERTY_CONTEXT_REQUIRED");
    }

    private static readonly System.Linq.Expressions.Expression<Func<HotelMinibarItemProfile, HotelMinibarItemProfileDto>> ToDto = x =>
        new(x.HotelMinibarItemProfileId, x.ItemId, x.ItemCode, x.ItemName, x.Unit, x.Category, x.RetailPrice, x.VatRate,
            x.StandardQuantity, x.MinStock, x.MaxStock, x.ChargeCode, x.ServiceChargeCode, x.AutoPostToFolio, x.RequiresApproval, x.ReplenishmentLeadMinutes, x.EffectiveFrom, x.EffectiveTo,
            x.IsActive, x.UpdatedAtUtc, x.RowVersion);
}
