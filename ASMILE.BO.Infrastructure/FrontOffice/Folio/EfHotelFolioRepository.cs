using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Folio;

public sealed class EfHotelFolioRepository(AppDbContext db) : IHotelFolioRepository
{
    public async Task<HotelFolioDto?> GetDtoAsync(Guid propertyId, Guid folioId, CancellationToken ct)
    {
        var f = await db.HotelFolios.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFolioId == folioId, ct);
        return f is null ? null : MapDto(f, await db.HotelFolioEntries.Where(e => e.HotelFolioId == folioId).SumAsync(e => e.Amount, ct));
    }

    public async Task<HotelFolioDto?> GetByStayAsync(Guid propertyId, Guid stayId, CancellationToken ct)
    {
        var f = await db.HotelFolios.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelStayId == stayId, ct);
        return f is null ? null : MapDto(f, await db.HotelFolioEntries.Where(e => e.HotelFolioId == f.HotelFolioId).SumAsync(e => e.Amount, ct));
    }

    public async Task<FolioLedgerDto?> GetLedgerAsync(Guid propertyId, Guid folioId, CancellationToken ct)
    {
        var f = await db.HotelFolios.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFolioId == folioId, ct);
        if (f is null) return null;
        var entries = await db.HotelFolioEntries.AsNoTracking().Where(e => e.HotelFolioId == folioId).OrderBy(e => e.SequenceNumber).Select(MapEntryDto).ToListAsync(ct);
        var stay = await db.HotelStays.AsNoTracking().FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.HotelStayId == f.HotelStayId, ct);
        var reservation = stay?.ReservationId is { } reservationId
            ? await db.FrontOfficeReservations.AsNoTracking().FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.ReservationId == reservationId, ct)
            : null;
        return new FolioLedgerDto(
            f.HotelFolioId,
            f.FolioNumber,
            f.HotelStayId,
            stay?.StayNumber ?? "N/A",
            stay?.GuestNameSnapshot ?? "N/A",
            stay?.RoomNumberSnapshot ?? "N/A",
            f.Status,
            entries.Sum(e => e.Amount),
            entries,
            f.RowVersion,
            stay?.GuestId,
            stay?.ReservationNumberSnapshot,
            stay?.ScheduledArrivalDate,
            stay?.ScheduledDepartureDate,
            stay?.RoomTypeCodeSnapshot,
            stay?.Source,
            reservation?.CompanyName,
            reservation?.TravelAgentName,
            f.CreatedAtUtc);
    }

    public async Task<PageResult<FolioLedgerDto>> SearchAsync(FolioSearchFilter filter, PageRequest page, CancellationToken ct)
    {
        var query = db.HotelFolios.AsNoTracking().Where(x => x.PropertyId == filter.PropertyId);
        if (filter.StayId is not null) query = query.Where(x => x.HotelStayId == filter.StayId);
        if (filter.Status is not null) query = query.Where(x => x.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) query = query.Where(x => x.FolioNumber.Contains(filter.Keyword));
        if (filter.DateFrom is not null || filter.DateTo is not null)
        {
            query = query.Where(x =>
                (filter.DateFrom == null || x.CreatedAtUtc >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue)) &&
                (filter.DateTo == null || x.CreatedAtUtc <= filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue)));
        }
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(ct);
        var size = Math.Clamp(page.PageSize, 1, 200);
        var folios = await query.Skip(page.Skip).Take(size).ToListAsync(ct);

        var folioIds = folios.Select(x => x.HotelFolioId).ToArray();
        var stayIds = folios.Select(x => x.HotelStayId).Distinct().ToArray();
        var stays = await db.HotelStays.AsNoTracking()
            .Where(x => x.PropertyId == filter.PropertyId && stayIds.Contains(x.HotelStayId))
            .ToDictionaryAsync(x => x.HotelStayId, ct);
        var reservationIds = stays.Values
            .Where(x => x.ReservationId.HasValue)
            .Select(x => x.ReservationId!.Value)
            .Distinct()
            .ToArray();
        var reservations = await db.FrontOfficeReservations.AsNoTracking()
            .Where(x => x.PropertyId == filter.PropertyId && reservationIds.Contains(x.ReservationId))
            .ToDictionaryAsync(x => x.ReservationId, ct);
        var entryQuery = db.HotelFolioEntries.AsNoTracking().Where(e => folioIds.Contains(e.HotelFolioId));
        if (filter.PostingType is not null) entryQuery = entryQuery.Where(e => e.EntryType == filter.PostingType);
        var entryRows = await entryQuery
            .OrderBy(e => e.HotelFolioId)
            .ThenBy(e => e.SequenceNumber)
            .Select(MapEntryDto)
            .ToListAsync(ct);
        var entriesByFolio = entryRows
            .GroupBy(x => x.HotelFolioId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<HotelFolioEntryDto>)x.ToList());

        var results = new List<FolioLedgerDto>();
        foreach (var f in folios)
        {
            var entries = entriesByFolio.TryGetValue(f.HotelFolioId, out var folioEntries)
                ? folioEntries
                : Array.Empty<HotelFolioEntryDto>();
            stays.TryGetValue(f.HotelStayId, out var stay);
            var reservation = stay?.ReservationId is { } reservationId && reservations.TryGetValue(reservationId, out var persistedReservation)
                ? persistedReservation
                : null;
            results.Add(new FolioLedgerDto(
                f.HotelFolioId,
                f.FolioNumber,
                f.HotelStayId,
                stay?.StayNumber ?? "N/A",
                stay?.GuestNameSnapshot ?? "N/A",
                stay?.RoomNumberSnapshot ?? "N/A",
                f.Status,
                entries.Sum(e => e.Amount),
                entries,
                f.RowVersion,
                stay?.GuestId,
                stay?.ReservationNumberSnapshot,
                stay?.ScheduledArrivalDate,
                stay?.ScheduledDepartureDate,
                stay?.RoomTypeCodeSnapshot,
                stay?.Source,
                reservation?.CompanyName,
                reservation?.TravelAgentName,
                f.CreatedAtUtc));
        }
        return new PageResult<FolioLedgerDto>(results, total, page.Page, page.PageSize);
    }

    public Task AddAsync(HotelFolio folio, CancellationToken ct) { db.HotelFolios.Add(folio); return Task.CompletedTask; }
    public Task<HotelFolio?> GetAsync(Guid propertyId, Guid folioId, CancellationToken ct) => db.HotelFolios.Include(x => x.Entries).FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFolioId == folioId, ct);
    public Task AddEntryAsync(HotelFolioEntry entry, CancellationToken ct) { db.HotelFolioEntries.Add(entry); return Task.CompletedTask; }
    public async Task UpdateEntryReversalAsync(HotelFolioEntry original, HotelFolioEntry reversal, CancellationToken ct) { db.HotelFolioEntries.Entry(original).Property(e => e.ReversedByEntryId).CurrentValue = reversal.HotelFolioEntryId; db.HotelFolioEntries.Entry(reversal).Property(e => e.ReversesEntryId).CurrentValue = original.HotelFolioEntryId; await Task.CompletedTask; }
    public void ApplyExpectedRowVersion(HotelFolio folio, byte[] expectedRowVersion) => db.Entry(folio).Property(nameof(HotelFolio.RowVersion)).OriginalValue = expectedRowVersion;

    private static HotelFolioDto MapDto(HotelFolio f, decimal balance) => new(f.HotelFolioId, f.FolioNumber, f.HotelStayId, f.PropertyId, f.Status, balance, f.TotalCharges, f.TotalPayments, Math.Max(0m, balance), f.CreatedAtUtc, f.ClosedAtUtc, f.SettledAtUtc, f.RowVersion);

    private static readonly System.Linq.Expressions.Expression<Func<HotelFolioEntry, HotelFolioEntryDto>> MapEntryDto = e => new HotelFolioEntryDto(e.HotelFolioEntryId, e.SequenceNumber, e.EntryType, e.Amount, e.Description, e.ReferenceType, e.ReferenceId, e.BusinessDate, e.ReversesEntryId, e.ReversedByEntryId, e.CreatedAtUtc, e.CreatedBy, e.HotelFolioId);
}
