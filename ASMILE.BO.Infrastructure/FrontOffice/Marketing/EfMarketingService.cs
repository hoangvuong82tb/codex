using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Marketing;
using ASMILE.BO.Core.FrontOffice.Marketing;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Marketing;

public sealed class EfMarketingService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IMarketingQueryService, IMarketingCommandService
{
    public async Task<IReadOnlyList<MarketingRecordDto>> SearchAsync(
        Guid propertyId,
        MarketingSearchFilter filter,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);
        EnsureProperty(propertyId);

        var query = db.MarketingRecords
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Section == filter.Section);

        if (filter.Status is MarketingRecordStatus status)
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.GroupName))
            query = query.Where(x => x.GroupName == filter.GroupName);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.Code.Contains(keyword)
                || x.Name.Contains(keyword)
                || x.GroupName.Contains(keyword)
                || x.Owner.Contains(keyword));
        }

        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Take(2000)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<MarketingRecordDto> CreateAsync(
        Guid propertyId,
        CreateMarketingRecordRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureProperty(propertyId);
        var context = RequireContext();
        var code = request.Code.Trim();
        if (await db.MarketingRecords.AnyAsync(
                x => x.PropertyId == propertyId && x.Section == request.Section && x.Code == code,
                ct))
            throw new InvalidOperationException("MARKETING_DUPLICATE_CODE");

        var now = clock.UtcNow;
        var row = MarketingRecord.Create(
            propertyId,
            request.Section,
            request.Code,
            request.Name,
            request.GroupName,
            request.Owner,
            request.Notes,
            now,
            context.Actor.ActorId);
        db.MarketingRecords.Add(row);
        StageAudit("MarketingRecordCreated", row);
        await CommitAsync(ct, "MARKETING_CREATE_FAILED");
        return Map(row);
    }

    public async Task<MarketingRecordDto> UpdateAsync(
        Guid propertyId,
        UpdateMarketingRecordRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureProperty(propertyId);
        var context = RequireContext();
        var row = await db.MarketingRecords.SingleOrDefaultAsync(
            x => x.PropertyId == propertyId && x.MarketingRecordId == request.MarketingRecordId,
            ct) ?? throw new KeyNotFoundException("MARKETING_NOT_FOUND");
        if (request.ExpectedRowVersion is { Length: > 0 })
            db.Entry(row).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;

        var duplicate = await db.MarketingRecords.AnyAsync(
            x => x.PropertyId == propertyId
                && x.Section == row.Section
                && x.Code == request.Code.Trim()
                && x.MarketingRecordId != row.MarketingRecordId,
            ct);
        if (duplicate)
            throw new InvalidOperationException("MARKETING_DUPLICATE_CODE");

        row.UpdateDetails(
            request.Code,
            request.Name,
            request.GroupName,
            request.Owner,
            request.Notes,
            clock.UtcNow,
            context.Actor.ActorId);
        StageAudit("MarketingRecordUpdated", row);
        await CommitAsync(ct, "MARKETING_UPDATE_FAILED");
        return Map(row);
    }

    public async Task<MarketingRecordDto> ChangeStatusAsync(
        Guid propertyId,
        ChangeMarketingRecordStatusRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureProperty(propertyId);
        var context = RequireContext();
        var row = await db.MarketingRecords.SingleOrDefaultAsync(
            x => x.PropertyId == propertyId && x.MarketingRecordId == request.MarketingRecordId,
            ct) ?? throw new KeyNotFoundException("MARKETING_NOT_FOUND");
        if (request.ExpectedRowVersion is { Length: > 0 })
            db.Entry(row).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;

        row.ChangeStatus(request.Status, clock.UtcNow, context.Actor.ActorId);
        StageAudit("MarketingRecordStatusChanged", row);
        await CommitAsync(ct, "MARKETING_STATUS_CHANGE_FAILED");
        return Map(row);
    }

    private void EnsureProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || propertyId != RequireContext().Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private ASMILE.BO.Application.FrontOffice.Foundation.Contexts.FrontOfficeExecutionContext RequireContext() =>
        contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private void StageAudit(string action, MarketingRecord row) =>
        persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(MarketingRecord),
            row.MarketingRecordId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = row.PropertyId.ToString("D"),
                ["Section"] = row.Section.ToString(),
                ["Code"] = row.Code,
                ["Status"] = row.Status.ToString()
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private static MarketingRecordDto Map(MarketingRecord row) => new(
        row.MarketingRecordId,
        row.PropertyId,
        row.Section,
        row.Code,
        row.Name,
        row.GroupName,
        row.Owner,
        row.Revenue,
        row.Status,
        row.Notes,
        row.CreatedAtUtc,
        row.UpdatedAtUtc,
        row.RowVersion);
}
