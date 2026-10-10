using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Folio;

public sealed class EfFolioRoutingService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IFolioRoutingQueryService, IFolioRoutingCommandService
{
    public async Task<IReadOnlyList<FolioRoutingRuleDto>> SearchAsync(FolioRoutingSearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureProperty(request.PropertyId);

        var query = db.HotelFolioRoutingRules.AsNoTracking()
            .Where(x => x.PropertyId == request.PropertyId);
        if (request.SourceFolioId is Guid sourceId)
            query = query.Where(x => x.SourceFolioId == sourceId);
        if (request.ActiveOnly)
            query = query.Where(x => x.IsActive);

        var rules = await query.OrderBy(x => x.Priority).ThenBy(x => x.Name).ToListAsync(ct);
        if (rules.Count == 0) return Array.Empty<FolioRoutingRuleDto>();

        var folioIds = rules.SelectMany(x => new[] { x.SourceFolioId, x.TargetFolioId }).Distinct().ToArray();
        var numbers = await db.HotelFolios.AsNoTracking()
            .Where(x => x.PropertyId == request.PropertyId && folioIds.Contains(x.HotelFolioId))
            .ToDictionaryAsync(x => x.HotelFolioId, x => x.FolioNumber, ct);

        return rules
            .Where(x => numbers.ContainsKey(x.SourceFolioId) && numbers.ContainsKey(x.TargetFolioId))
            .Select(x => Map(x, numbers[x.SourceFolioId], numbers[x.TargetFolioId]))
            .ToList();
    }

    public async Task<FolioRoutingRuleDto> CreateAsync(CreateFolioRoutingRuleRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureProperty(request.PropertyId);
        var context = RequireContext();
        await EnsureFoliosAsync(request.PropertyId, request.SourceFolioId, request.TargetFolioId, ct);
        var normalizedType = request.EntryType.Trim();
        if (await db.HotelFolioRoutingRules.AnyAsync(x =>
                x.PropertyId == request.PropertyId && x.SourceFolioId == request.SourceFolioId &&
                x.TargetFolioId == request.TargetFolioId && x.EntryType == normalizedType &&
                x.IsActive, ct))
            throw new InvalidOperationException("FOLIO_ROUTING_DUPLICATE_ACTIVE_RULE");

        var now = clock.UtcNow;
        var rule = HotelFolioRoutingRule.Create(
            request.PropertyId,
            request.SourceFolioId,
            request.TargetFolioId,
            request.Name,
            normalizedType,
            request.Percent,
            request.Priority,
            request.Notes,
            now,
            context.Actor.ActorId);
        db.HotelFolioRoutingRules.Add(rule);
        StageAudit("FolioRoutingRuleCreated", rule);
        await CommitAsync(ct);
        return await ReadSingleAsync(rule, ct);
    }

    public async Task<FolioRoutingRuleDto> UpdateAsync(UpdateFolioRoutingRuleRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var rule = await db.HotelFolioRoutingRules.SingleOrDefaultAsync(x =>
            x.PropertyId == context.Property.PropertyId.Value && x.HotelFolioRoutingRuleId == request.RuleId, ct)
            ?? throw new KeyNotFoundException("FOLIO_ROUTING_RULE_NOT_FOUND");
        ApplyRowVersion(rule, request.ExpectedRowVersion);
        rule.Update(request.Name, request.EntryType, request.Percent, request.Priority, request.Notes, clock.UtcNow, context.Actor.ActorId);
        StageAudit("FolioRoutingRuleUpdated", rule);
        await CommitAsync(ct);
        return await ReadSingleAsync(rule, ct);
    }

    public async Task<FolioRoutingRuleDto> SetActiveAsync(SetFolioRoutingRuleActiveRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var rule = await db.HotelFolioRoutingRules.SingleOrDefaultAsync(x =>
            x.PropertyId == context.Property.PropertyId.Value && x.HotelFolioRoutingRuleId == request.RuleId, ct)
            ?? throw new KeyNotFoundException("FOLIO_ROUTING_RULE_NOT_FOUND");
        ApplyRowVersion(rule, request.ExpectedRowVersion);
        rule.SetActive(request.IsActive, clock.UtcNow, context.Actor.ActorId);
        StageAudit(request.IsActive ? "FolioRoutingRuleActivated" : "FolioRoutingRuleDeactivated", rule);
        await CommitAsync(ct);
        return await ReadSingleAsync(rule, ct);
    }

    private async Task EnsureFoliosAsync(Guid propertyId, Guid sourceFolioId, Guid targetFolioId, CancellationToken ct)
    {
        if (sourceFolioId == targetFolioId) throw new InvalidOperationException("FOLIO_ROUTING_SAME_SOURCE_TARGET");
        var count = await db.HotelFolios.CountAsync(x =>
            x.PropertyId == propertyId && (x.HotelFolioId == sourceFolioId || x.HotelFolioId == targetFolioId), ct);
        if (count != 2) throw new KeyNotFoundException("FOLIO_ROUTING_FOLIO_NOT_FOUND");
    }

    private async Task<FolioRoutingRuleDto> ReadSingleAsync(HotelFolioRoutingRule rule, CancellationToken ct)
    {
        var numbers = await db.HotelFolios.AsNoTracking()
            .Where(x => x.PropertyId == rule.PropertyId && (x.HotelFolioId == rule.SourceFolioId || x.HotelFolioId == rule.TargetFolioId))
            .ToDictionaryAsync(x => x.HotelFolioId, x => x.FolioNumber, ct);
        return Map(rule, numbers[rule.SourceFolioId], numbers[rule.TargetFolioId]);
    }

    private void StageAudit(string action, HotelFolioRoutingRule rule) => persistence.StageAudit(new FrontOfficeAuditIntent(
        action,
        nameof(HotelFolioRoutingRule),
        rule.HotelFolioRoutingRuleId.ToString("D"),
        "Succeeded",
        Metadata: new Dictionary<string, string>
        {
            ["PropertyId"] = rule.PropertyId.ToString("D"),
            ["SourceFolioId"] = rule.SourceFolioId.ToString("D"),
            ["TargetFolioId"] = rule.TargetFolioId.ToString("D"),
            ["EntryType"] = rule.EntryType,
            ["Percent"] = rule.Percent.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["IsActive"] = rule.IsActive.ToString()
        }));

    private async Task CommitAsync(CancellationToken ct)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "FOLIO_ROUTING_COMMIT_FAILED");
    }

    private void ApplyRowVersion(HotelFolioRoutingRule rule, byte[]? expected)
    {
        if (expected is not { Length: 8 }) throw new InvalidOperationException("FOLIO_ROUTING_ROWVERSION_REQUIRED");
        db.Entry(rule).Property(x => x.RowVersion).OriginalValue = expected;
    }

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current
        ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private void EnsureProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || propertyId != RequireContext().Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static FolioRoutingRuleDto Map(HotelFolioRoutingRule rule, string sourceNumber, string targetNumber) => new(
        rule.HotelFolioRoutingRuleId,
        rule.PropertyId,
        rule.SourceFolioId,
        sourceNumber,
        rule.TargetFolioId,
        targetNumber,
        rule.Name,
        rule.EntryType,
        rule.Percent,
        rule.Priority,
        rule.IsActive,
        rule.Notes,
        rule.UpdatedAtUtc,
        rule.RowVersion);
}
