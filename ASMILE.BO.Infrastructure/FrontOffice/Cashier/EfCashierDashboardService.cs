using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Cashier;

public sealed class EfCashierDashboardService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor executionContext,
    IClock clock) : ICashierDashboardService
{
    public async Task<CashierDashboardDto> GetCashierDashboardAsync(Guid cashierId, CancellationToken ct)
    {
        var context = RequireContext();
        if (cashierId == Guid.Empty) throw new ArgumentException("CASHIER_ID_REQUIRED");

        var propertyId = context.Property.PropertyId.Value;
        var name = await db.AppUsers.AsNoTracking()
            .Where(x => x.Id == cashierId)
            .Select(x => x.DisplayName)
            .SingleOrDefaultAsync(ct) ?? string.Empty;
        var today = clock.UtcNow.Date;
        var shift = await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == propertyId && x.CashierId == cashierId && x.Status == CashierShiftStatus.Open)
            .OrderByDescending(x => x.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (shift is null)
            return new CashierDashboardDto(cashierId, name, null, null, DrawerStatus.Closed, 0, 0, 0, 0, 0, false, clock.UtcNow);

        var movements = shift.Movements.Where(x => x.CreatedAtUtc.Date == today).ToList();
        var cashIn = movements.Where(x => x.Type is CashMovementType.CashIn or CashMovementType.TransferIn).Sum(x => x.Amount);
        var cashOut = movements.Where(x => x.Type is CashMovementType.CashOut or CashMovementType.Refund or CashMovementType.TransferOut).Sum(x => x.Amount);
        return new CashierDashboardDto(
            cashierId,
            name,
            shift.CashierShiftId,
            shift.ShiftNumber,
            DrawerStatus.Open,
            shift.ExpectedCash,
            shift.OpeningCash,
            movements.Count,
            cashIn,
            cashOut,
            false,
            shift.OpenedAtUtc);
    }

    public async Task<CashierSupervisorDashboardDto> GetSupervisorDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);
        var today = clock.UtcNow.Date;
        var active = await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == propertyId && x.Status == CashierShiftStatus.Open)
            .OrderBy(x => x.OpenedAtUtc)
            .ToListAsync(ct);

        var cashierIds = active.Select(x => x.CashierId).Distinct().ToArray();
        var cashierNames = await db.AppUsers.AsNoTracking()
            .Where(x => cashierIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        var openDrawers = active
            .Select(x => ToDrawerDto(x, cashierNames.GetValueOrDefault(x.CashierId)))
            .ToList();
        var closedToday = await db.CashierShifts.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status == CashierShiftStatus.Closed && x.ClosedAtUtc.HasValue && x.ClosedAtUtc.Value.Date == today)
            .Select(x => new { x.ActualCash, x.OpeningCash })
            .ToListAsync(ct);
        var variance = closedToday.Sum(x => x.ActualCash.GetValueOrDefault() - x.OpeningCash);
        var unresolvedRows = await db.CashReconciliations.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        (x.Status == CashReconciliationStatus.Pending ||
                         x.Status == CashReconciliationStatus.VarianceDetected ||
                         x.Status == CashReconciliationStatus.Escalated))
            .OrderByDescending(x => x.ReviewedAtUtc)
            .Take(500)
            .ToListAsync(ct);
        var unresolved = unresolvedRows.Select(MapReconciliation).ToList();

        return new CashierSupervisorDashboardDto(
            active.Count,
            active.Count,
            unresolved.Count,
            closedToday.Count(x => x.ActualCash.HasValue && x.ActualCash.Value != x.OpeningCash),
            variance,
            unresolved,
            openDrawers);
    }

    public async Task<TreasuryDashboardDto> GetTreasuryDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);
        var today = clock.UtcNow.Date;
        var shifts = await db.CashierShifts
            .AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(ct);

        var movements = shifts.SelectMany(x => x.Movements).Where(x => x.CreatedAtUtc.Date == today).ToList();
        var deposits = movements.Where(x => x.Type is CashMovementType.CashIn or CashMovementType.TransferIn).Sum(x => x.Amount);
        var withdrawals = movements.Where(x => x.Type is CashMovementType.CashOut or CashMovementType.TransferOut or CashMovementType.Refund).Sum(x => x.Amount);
        var closed = shifts.Where(x => x.Status == CashierShiftStatus.Closed).ToList();
        return new TreasuryDashboardDto(
            shifts.Where(x => x.Status == CashierShiftStatus.Open).Sum(x => x.ExpectedCash),
            deposits,
            withdrawals,
            shifts.Count(x => x.Status == CashierShiftStatus.Open),
            closed.Count,
            closed.Sum(x => x.Difference ?? 0));
    }

    private static CashDrawerDto ToDrawerDto(CashierShift shift, string? cashierName)
        => new(shift.CashierShiftId, shift.ShiftNumber, shift.CashierShiftId, shift.CashierId, cashierName,
            DrawerStatus.Open, shift.ExpectedCash, shift.ExpectedCash, 0, shift.OpenedAtUtc, null);

    private static CashReconciliationDto MapReconciliation(CashReconciliation row)
        => new(
            row.CashReconciliationId,
            row.CashierShiftId,
            row.ShiftNumber,
            row.ExpectedCash,
            row.ActualCash,
            row.Variance,
            Enum.TryParse<ReconciliationStatus>(row.Status.ToString(), true, out var status)
                ? status
                : ReconciliationStatus.Pending,
            Enum.TryParse<VarianceReasonCode>(row.ReasonCode, true, out var reasonCode)
                ? reasonCode
                : null,
            row.ReasonDetail,
            row.ReviewedBy,
            row.ReviewedAtUtc,
            row.ApprovedBy,
            row.ApprovedAtUtc);

    private FrontOfficeExecutionContext RequireContext()
        => executionContext.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }
}
