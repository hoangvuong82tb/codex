using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Rate;

public sealed class EfExchangeRateService(AppDbContext db, IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor, IClock clock) : IExchangeRateQueryService, IExchangeRateCommandService
{
    public async Task<IReadOnlyList<ExchangeRateDto>> SearchAsync(ExchangeRateSearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); EnsureProperty(request.PropertyId);
        var rows = await db.HotelExchangeRates.AsNoTracking().Where(x => x.PropertyId == request.PropertyId)
            .OrderBy(x => x.CurrencyCode).ThenByDescending(x => x.EffectiveFromUtc).Take(1000).ToListAsync(ct);
        var asOf = request.AsOfUtc ?? clock.UtcNow;
        IEnumerable<ExchangeRateDto> result = rows.Select(x => Map(x, asOf));
        if (!string.IsNullOrWhiteSpace(request.SearchText)) { var text = request.SearchText.Trim(); result = result.Where(x => x.CurrencyCode.Contains(text, StringComparison.OrdinalIgnoreCase) || x.CurrencyName.Contains(text, StringComparison.OrdinalIgnoreCase)); }
        if (request.State is ExchangeRateDisplayState state) result = result.Where(x => x.DisplayState == state);
        return result.ToList();
    }

    public async Task<ExchangeRateDto> CreateAsync(CreateExchangeRateRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); EnsureProperty(request.PropertyId);
        if (await db.HotelExchangeRates.AnyAsync(x => x.PropertyId == request.PropertyId && x.CurrencyCode == request.CurrencyCode.Trim().ToUpper() && x.EffectiveFromUtc == request.EffectiveFromUtc, ct)) throw new InvalidOperationException("EXCHANGE_RATE_DUPLICATE");
        var context = RequireContext(); var now = clock.UtcNow;
        if (request.IsDefault) await ClearDefaultsAsync(request.PropertyId, now, context, ct);
        var row = HotelExchangeRate.Create(request.PropertyId, request.CurrencyCode, request.CurrencyName, request.Symbol, request.BuyRate, request.SellRate, request.HotelRate, request.EffectiveFromUtc, request.EffectiveToUtc, request.IsDefault, request.Notes, now, context.Actor.ActorId);
        db.HotelExchangeRates.Add(row); StageAudit("ExchangeRateCreated", row); await CommitAsync(ct); return Map(row, now);
    }

    public async Task<ExchangeRateDto> UpdateAsync(UpdateExchangeRateRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); var context = RequireContext();
        var row = await db.HotelExchangeRates.SingleOrDefaultAsync(x => x.PropertyId == context.Property.PropertyId.Value && x.HotelExchangeRateId == request.HotelExchangeRateId, ct) ?? throw new KeyNotFoundException("EXCHANGE_RATE_NOT_FOUND");
        if (request.ExpectedRowVersion is { Length: > 0 }) db.Entry(row).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;
        row.Update(request.CurrencyCode, request.CurrencyName, request.Symbol, request.BuyRate, request.SellRate, request.HotelRate, request.EffectiveFromUtc, request.EffectiveToUtc, request.Notes, clock.UtcNow, context.Actor.ActorId);
        StageAudit("ExchangeRateUpdated", row); await CommitAsync(ct); return Map(row, clock.UtcNow);
    }

    public async Task<ExchangeRateDto> SetDefaultAsync(SetDefaultExchangeRateRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); var context = RequireContext(); var propertyId = context.Property.PropertyId.Value;
        var rows = await db.HotelExchangeRates.Where(x => x.PropertyId == propertyId).ToListAsync(ct);
        var target = rows.SingleOrDefault(x => x.HotelExchangeRateId == request.HotelExchangeRateId) ?? throw new KeyNotFoundException("EXCHANGE_RATE_NOT_FOUND");
        if (request.ExpectedRowVersion is { Length: > 0 }) db.Entry(target).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;
        var now = clock.UtcNow; foreach (var row in rows) row.SetDefault(row.HotelExchangeRateId == target.HotelExchangeRateId, now, context.Actor.ActorId);
        StageAudit("ExchangeRateDefaultChanged", target); await CommitAsync(ct); return Map(target, now);
    }

    private async Task ClearDefaultsAsync(Guid propertyId, DateTime now, Application.FrontOffice.Foundation.Contexts.FrontOfficeExecutionContext context, CancellationToken ct)
    { foreach (var row in await db.HotelExchangeRates.Where(x => x.PropertyId == propertyId && x.IsDefault).ToListAsync(ct)) row.SetDefault(false, now, context.Actor.ActorId); }
    private void StageAudit(string action, HotelExchangeRate row) => persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(HotelExchangeRate), row.HotelExchangeRateId.ToString("D"), "Succeeded", Metadata: new Dictionary<string, string> { ["PropertyId"] = row.PropertyId.ToString("D"), ["CurrencyCode"] = row.CurrencyCode, ["IsDefault"] = row.IsDefault.ToString() }));
    private async Task CommitAsync(CancellationToken ct) { var result = await persistence.CommitAsync(ct); if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "EXCHANGE_RATE_COMMIT_FAILED"); }
    private Application.FrontOffice.Foundation.Contexts.FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (propertyId == Guid.Empty || propertyId != RequireContext().Property.PropertyId.Value) throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH"); }
    private static ExchangeRateDto Map(HotelExchangeRate x, DateTime asOf) => new(x.HotelExchangeRateId, x.PropertyId, x.CurrencyCode, x.CurrencyName, x.Symbol, x.BuyRate, x.SellRate, x.HotelRate, x.EffectiveFromUtc, x.EffectiveToUtc, x.IsDefault, x.Status, State(x, asOf), x.Notes, x.UpdatedAtUtc, x.RowVersion);
    private static ExchangeRateDisplayState State(HotelExchangeRate x, DateTime asOf) => x.Status switch { HotelExchangeRateStatus.Draft => ExchangeRateDisplayState.Draft, HotelExchangeRateStatus.Inactive or HotelExchangeRateStatus.Archived => ExchangeRateDisplayState.Inactive, _ when x.EffectiveFromUtc > asOf => ExchangeRateDisplayState.Scheduled, _ when x.EffectiveToUtc is DateTime to && to <= asOf => ExchangeRateDisplayState.Expired, _ when x.EffectiveToUtc is DateTime soon && soon <= asOf.AddDays(7) => ExchangeRateDisplayState.Expiring, _ => ExchangeRateDisplayState.Effective };
}
