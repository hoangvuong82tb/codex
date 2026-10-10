using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

public sealed class EfHousekeepingSupplyMasterDataService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IFrontOfficePersistenceSession persistence) : IHousekeepingSupplyMasterDataService
{
    public async Task<IReadOnlyList<HousekeepingLinenTypeDto>> SearchLinenTypesAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var query = db.HousekeepingLinenTypes.AsNoTracking().Where(x => x.PropertyId == propertyId);
        query = ApplyFilter(query, keyword, isActive);
        return await query.OrderBy(x => x.Category).ThenBy(x => x.Code).Select(ToLinenDto).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<HousekeepingAmenityTypeDto>> SearchAmenityTypesAsync(Guid propertyId, string? keyword, bool? isActive, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var query = db.HousekeepingAmenityTypes.AsNoTracking().Where(x => x.PropertyId == propertyId);
        query = ApplyFilter(query, keyword, isActive);
        return await query.OrderBy(x => x.Category).ThenBy(x => x.Code).Select(ToAmenityDto).ToListAsync(ct);
    }

    public async Task<HousekeepingLinenTypeDto> SaveLinenTypeAsync(Guid propertyId, HousekeepingLinenTypeInput input, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var code = NormalizeCode(input.Code);
        HousekeepingLinenType entity;
        var isNew = !input.Id.HasValue || input.Id.Value <= 0;
        if (isNew)
        {
            await EnsureLinenCodeAvailable(propertyId, code, null, ct);
            entity = HousekeepingLinenType.Create(propertyId, code, input.Name, input.Category, input.DefaultParLevel, input.MaxWashCycles);
            db.HousekeepingLinenTypes.Add(entity);
        }
        else
        {
            entity = await db.HousekeepingLinenTypes.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == input.Id!.Value, ct)
                ?? throw new InvalidOperationException("HOUSEKEEPING_LINEN_TYPE_NOT_FOUND");
            SetOriginalRowVersion(entity, input.ExpectedRowVersion);
            await EnsureLinenCodeAvailable(propertyId, code, entity.Id, ct);
            entity.Update(code, input.Name, input.Category, input.DefaultParLevel, input.MaxWashCycles);
        }

        persistence.StageAudit(new FrontOfficeAuditIntent(isNew ? "Created" : "Updated", nameof(HousekeepingLinenType), entity.Id.ToString(), "Succeeded"));
        await CommitAsync(ct);
        return await db.HousekeepingLinenTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Id == entity.Id).Select(ToLinenDto).SingleAsync(ct);
    }

    public async Task<HousekeepingAmenityTypeDto> SaveAmenityTypeAsync(Guid propertyId, HousekeepingAmenityTypeInput input, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var code = NormalizeCode(input.Code);
        HousekeepingAmenityType entity;
        var isNew = !input.Id.HasValue || input.Id.Value <= 0;
        if (isNew)
        {
            await EnsureAmenityCodeAvailable(propertyId, code, null, ct);
            entity = HousekeepingAmenityType.Create(propertyId, code, input.Name, input.Category, input.DefaultParLevel, input.MinAlertThreshold);
            db.HousekeepingAmenityTypes.Add(entity);
        }
        else
        {
            entity = await db.HousekeepingAmenityTypes.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == input.Id!.Value, ct)
                ?? throw new InvalidOperationException("HOUSEKEEPING_AMENITY_TYPE_NOT_FOUND");
            SetOriginalRowVersion(entity, input.ExpectedRowVersion);
            await EnsureAmenityCodeAvailable(propertyId, code, entity.Id, ct);
            entity.Update(code, input.Name, input.Category, input.DefaultParLevel, input.MinAlertThreshold);
        }

        persistence.StageAudit(new FrontOfficeAuditIntent(isNew ? "Created" : "Updated", nameof(HousekeepingAmenityType), entity.Id.ToString(), "Succeeded"));
        await CommitAsync(ct);
        return await db.HousekeepingAmenityTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Id == entity.Id).Select(ToAmenityDto).SingleAsync(ct);
    }

    public async Task<HousekeepingLinenTypeDto> SetLinenTypeActiveAsync(Guid propertyId, int id, bool isActive, byte[]? expectedRowVersion, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var entity = await db.HousekeepingLinenTypes.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == id, ct)
            ?? throw new InvalidOperationException("HOUSEKEEPING_LINEN_TYPE_NOT_FOUND");
        SetOriginalRowVersion(entity, expectedRowVersion);
        entity.SetActive(isActive);
        persistence.StageAudit(new FrontOfficeAuditIntent(isActive ? "Activated" : "Deactivated", nameof(HousekeepingLinenType), id.ToString(), "Succeeded"));
        await CommitAsync(ct);
        return await db.HousekeepingLinenTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Id == id).Select(ToLinenDto).SingleAsync(ct);
    }

    public async Task<HousekeepingAmenityTypeDto> SetAmenityTypeActiveAsync(Guid propertyId, int id, bool isActive, byte[]? expectedRowVersion, CancellationToken ct = default)
    {
        EnsureProperty(propertyId);
        var entity = await db.HousekeepingAmenityTypes.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == id, ct)
            ?? throw new InvalidOperationException("HOUSEKEEPING_AMENITY_TYPE_NOT_FOUND");
        SetOriginalRowVersion(entity, expectedRowVersion);
        entity.SetActive(isActive);
        persistence.StageAudit(new FrontOfficeAuditIntent(isActive ? "Activated" : "Deactivated", nameof(HousekeepingAmenityType), id.ToString(), "Succeeded"));
        await CommitAsync(ct);
        return await db.HousekeepingAmenityTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Id == id).Select(ToAmenityDto).SingleAsync(ct);
    }

    private async Task CommitAsync(CancellationToken ct)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Message ?? "HOUSEKEEPING_SUPPLY_MASTER_SAVE_FAILED");
    }

    private async Task EnsureLinenCodeAvailable(Guid propertyId, string code, int? excludedId, CancellationToken ct)
    {
        if (await db.HousekeepingLinenTypes.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value), ct))
            throw new InvalidOperationException("HOUSEKEEPING_LINEN_CODE_CONFLICT");
    }

    private async Task EnsureAmenityCodeAvailable(Guid propertyId, string code, int? excludedId, CancellationToken ct)
    {
        if (await db.HousekeepingAmenityTypes.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value), ct))
            throw new InvalidOperationException("HOUSEKEEPING_AMENITY_CODE_CONFLICT");
    }

    private void EnsureProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || contextAccessor.Current?.Property.PropertyId.Value != propertyId)
            throw new InvalidOperationException("FO_PROPERTY_CONTEXT_REQUIRED");
    }

    private static IQueryable<HousekeepingLinenType> ApplyFilter(IQueryable<HousekeepingLinenType> query, string? keyword, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(keyword)) { keyword = keyword.Trim(); query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Category.Contains(keyword)); }
        return isActive is null ? query : query.Where(x => x.IsActive == isActive);
    }

    private static IQueryable<HousekeepingAmenityType> ApplyFilter(IQueryable<HousekeepingAmenityType> query, string? keyword, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(keyword)) { keyword = keyword.Trim(); query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Category.Contains(keyword)); }
        return isActive is null ? query : query.Where(x => x.IsActive == isActive);
    }

    private static string NormalizeCode(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Code is required.", nameof(value)) : value.Trim().ToUpperInvariant();
    private void SetOriginalRowVersion(HousekeepingLinenType entity, byte[]? expected) { if (expected is { Length: > 0 }) db.Entry(entity).Property("RowVersion").OriginalValue = expected; }
    private void SetOriginalRowVersion(HousekeepingAmenityType entity, byte[]? expected) { if (expected is { Length: > 0 }) db.Entry(entity).Property("RowVersion").OriginalValue = expected; }

    private static readonly System.Linq.Expressions.Expression<Func<HousekeepingLinenType, HousekeepingLinenTypeDto>> ToLinenDto = x => new(x.Id, x.Code, x.Name, x.Category, x.DefaultParLevel, x.MaxWashCycles, x.IsActive, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HousekeepingAmenityType, HousekeepingAmenityTypeDto>> ToAmenityDto = x => new(x.Id, x.Code, x.Name, x.Category, x.DefaultParLevel, x.MinAlertThreshold, x.IsActive, x.RowVersion);
}
