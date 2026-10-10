using ASMILE.BO.Application.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.NightAudit;

/// <summary>
/// Reads reservation outcomes from the canonical reservation table for the
/// property's business date. This keeps Night Audit KPIs tied to persisted
/// reservation state rather than UI counters or sample data.
/// </summary>
public sealed class EfNightAuditReservationMetricsReader(AppDbContext db) : INightAuditReservationMetricsReader
{
    public async Task<NightAuditReservationMetrics> GetAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var counts = await db.FrontOfficeReservations
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.ArrivalDate == businessDate)
            .Where(x => x.Status == ReservationStatus.NoShow || x.Status == ReservationStatus.Cancelled)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new NightAuditReservationMetrics(
            counts.FirstOrDefault(x => x.Status == ReservationStatus.NoShow)?.Count ?? 0,
            counts.FirstOrDefault(x => x.Status == ReservationStatus.Cancelled)?.Count ?? 0);
    }
}
