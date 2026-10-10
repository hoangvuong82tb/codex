using ASMILE.BO.Application.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.NightAudit;

/// <summary>
/// Builds Night Audit financial metrics from the canonical folio-entry ledger.
/// The query is property-scoped and keeps payments/refunds separate from revenue
/// so the snapshot cannot be populated with UI placeholders.
/// </summary>
public sealed class EfNightAuditFinancialReader(AppDbContext db) : INightAuditFinancialReader
{
    public async Task<NightAuditFinancialMetrics> GetAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var start = businessDate.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);
        var entries = db.HotelFolioEntries
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId);

        var periodEntries = entries.Where(x =>
            x.BusinessDate == businessDate ||
            (x.BusinessDate == null && x.CreatedAtUtc >= start && x.CreatedAtUtc < end));

        var totals = await periodEntries
            .GroupBy(_ => 1)
            .Select(g => new
            {
                RoomRevenue = g.Where(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.Amount > 0)
                    .Sum(x => (decimal?)x.Amount) ?? 0,
                OtherRevenue = g.Where(x =>
                        x.Amount > 0 &&
                        (x.EntryType == HotelFolioEntryType.Restaurant ||
                         x.EntryType == HotelFolioEntryType.Laundry ||
                         x.EntryType == HotelFolioEntryType.MiniBar ||
                         x.EntryType == HotelFolioEntryType.Spa ||
                         x.EntryType == HotelFolioEntryType.Telephone ||
                         x.EntryType == HotelFolioEntryType.OtherCharge ||
                         x.EntryType == HotelFolioEntryType.ManualCharge))
                    .Sum(x => (decimal?)x.Amount) ?? 0,
                ServiceCharge = g.Where(x => x.EntryType == HotelFolioEntryType.ServiceCharge && x.Amount > 0)
                    .Sum(x => (decimal?)x.Amount) ?? 0,
                TotalTax = g.Where(x => x.EntryType == HotelFolioEntryType.Tax && x.Amount > 0)
                    .Sum(x => (decimal?)x.Amount) ?? 0,
                TotalPayments = g.Where(x =>
                        x.Amount < 0 &&
                        (x.EntryType == HotelFolioEntryType.Payment || x.EntryType == HotelFolioEntryType.Deposit))
                    .Sum(x => (decimal?)-x.Amount) ?? 0,
                TotalRefunds = g.Where(x => x.EntryType == HotelFolioEntryType.Refund && x.Amount > 0)
                    .Sum(x => (decimal?)x.Amount) ?? 0,
                RoomNightsSold = g.Count(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.Amount > 0)
            })
            .FirstOrDefaultAsync(ct);

        var balances = await entries
            .GroupBy(x => x.HotelFolioId)
            .Select(g => g.Sum(x => x.Amount))
            .ToListAsync(ct);

        var outstanding = balances.Where(x => x > 0).Sum();
        return totals is null
            ? new NightAuditFinancialMetrics(0, 0, 0, 0, 0, 0, outstanding, 0)
            : new NightAuditFinancialMetrics(
                totals.RoomRevenue,
                totals.OtherRevenue,
                totals.ServiceCharge,
                totals.TotalTax,
                totals.TotalPayments,
                totals.TotalRefunds,
                outstanding,
                totals.RoomNightsSold);
    }
}
