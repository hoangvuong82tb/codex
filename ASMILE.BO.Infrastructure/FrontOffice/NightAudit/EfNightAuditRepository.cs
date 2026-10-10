using ASMILE.BO.Application.FrontOffice.NightAudit;
using NA = ASMILE.BO.Core.FrontOffice.NightAudit.NightAudit;
using NAS = ASMILE.BO.Core.FrontOffice.NightAudit.NightAuditStatus;
using NASS = ASMILE.BO.Core.FrontOffice.NightAudit.NightAuditStepStatus;
using NABD = ASMILE.BO.Core.FrontOffice.NightAudit.BusinessDateStatus;
using CoreNightAudit = ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.NightAudit;

public sealed class EfNightAuditRepository(AppDbContext db) : INightAuditRepository
{
    public async Task<NA?> GetAsync(Guid propertyId, Guid auditId, CancellationToken ct) => await db.Set<NA>().Include(x => x.Steps).FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.NightAuditId == auditId, ct);
    public async Task<NightAuditDto?> GetCurrentAsync(Guid propertyId, CancellationToken ct)
    {
        var current = await db.Set<NA>()
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status == NAS.Running)
            .Select(x => new
            {
                x.NightAuditId,
                x.AuditNumber,
                x.BusinessDate,
                x.Status,
                x.CurrentStep,
                x.ErrorMessage,
                x.StartedAtUtc,
                x.CompletedAtUtc,
                CompletedSteps = x.Steps.Count(step => step.Status == NASS.Completed),
                TotalSteps = x.Steps.Count(),
                x.RowVersion
            })
            .FirstOrDefaultAsync(ct);

        return current is null
            ? null
            : new NightAuditDto(
                current.NightAuditId,
                current.AuditNumber,
                current.BusinessDate,
                current.Status,
                current.CurrentStep,
                current.ErrorMessage,
                current.StartedAtUtc,
                current.CompletedAtUtc,
                current.CompletedSteps,
                current.TotalSteps,
                current.RowVersion);
    }
    public async Task<CoreNightAudit.BusinessDateRecord?> GetBusinessDateAsync(Guid propertyId, DateOnly date, CancellationToken ct) => await db.Set<CoreNightAudit.BusinessDateRecord>().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.BusinessDate == date, ct);
    public async Task<BusinessDateDto?> GetCurrentBusinessDateAsync(Guid propertyId, CancellationToken ct) => await db.Set<CoreNightAudit.BusinessDateRecord>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.Status == NABD.Open).Select(x => new BusinessDateDto(x.BusinessDateId, x.BusinessDate, x.Status, x.OpenedAtUtc, x.ClosedAtUtc, x.RowVersion)).FirstOrDefaultAsync(ct);
    public async Task<RevenueSnapshotDto?> GetRevenueSnapshotAsync(Guid propertyId, DateOnly date, CancellationToken ct) => await db.Set<CoreNightAudit.RevenueSnapshot>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.BusinessDate == date).Select(x => new RevenueSnapshotDto(x.RevenueSnapshotId, x.BusinessDate, x.RoomRevenue, x.OtherRevenue, x.TotalRevenue, x.TotalTax, x.TotalPayments, x.TotalRefunds, x.OutstandingBalance, x.RoomsSold, x.TotalRooms, x.OccupancyRate, x.ADR, x.RevPAR, x.Arrivals, x.Departures, x.NoShows, x.Cancellations)).FirstOrDefaultAsync(ct);
    public async Task AddAsync(NA audit, CancellationToken ct) { db.Set<NA>().Add(audit); await db.SaveChangesAsync(ct); }
    public async Task AddRevenueSnapshotAsync(CoreNightAudit.RevenueSnapshot snapshot, CancellationToken ct) { db.Set<CoreNightAudit.RevenueSnapshot>().Add(snapshot); await db.SaveChangesAsync(ct); }
    public async Task AddBusinessDateAsync(CoreNightAudit.BusinessDateRecord record, CancellationToken ct) { db.Set<CoreNightAudit.BusinessDateRecord>().Add(record); await db.SaveChangesAsync(ct); }
    public void ApplyExpectedRowVersion(NA audit, byte[] rowVersion) => db.Entry(audit).Property(nameof(NA.RowVersion)).OriginalValue = rowVersion;
}
