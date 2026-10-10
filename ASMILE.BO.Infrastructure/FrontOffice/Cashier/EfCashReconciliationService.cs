using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppReconciliationStatus = ASMILE.BO.Application.FrontOffice.Cashier.ReconciliationStatus;

namespace ASMILE.BO.Infrastructure.FrontOffice.Cashier;

public sealed class EfCashReconciliationService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor executionContext,
    IClock clock) : ICashReconciliationService
{
    public async Task<CashReconciliationDto> ReconcileAsync(Guid shiftId, decimal actualCash, CancellationToken ct)
    {
        var context = RequireContext();
        if (shiftId == Guid.Empty || actualCash < 0)
            throw new ArgumentException("CASH_RECONCILIATION_INPUT_INVALID");

        var propertyId = context.Property.PropertyId.Value;
        var shift = await db.CashierShifts
            .Include(x => x.Movements)
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CashierShiftId == shiftId, ct)
            ?? throw new KeyNotFoundException("CASH_SHIFT_NOT_FOUND");
        if (shift.Status != CashierShiftStatus.Closed)
            throw new InvalidOperationException("CASH_RECONCILIATION_REQUIRES_CLOSED_SHIFT");

        var existing = await db.CashReconciliations
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CashierShiftId == shiftId, ct);
        if (existing is not null)
        {
            if (existing.ActualCash != actualCash)
                throw new InvalidOperationException("CASH_RECONCILIATION_ALREADY_EXISTS");
            return Map(existing);
        }

        var reconciliation = CashReconciliation.Create(
            propertyId,
            shift,
            actualCash,
            context.Actor.ActorId?.Value ?? throw new InvalidOperationException("CASH_REVIEWER_REQUIRED"),
            actualCash == shift.ExpectedCash ? null : VarianceReasonCode.Other.ToString(),
            null,
            clock.UtcNow);
        db.CashReconciliations.Add(reconciliation);
        StageAudit("CashReconciliationCreated", reconciliation, propertyId);
        await CommitAsync(ct, "CASH_RECONCILIATION_CREATE_FAILED");
        return Map(reconciliation);
    }

    public async Task<CashReconciliationDto> ApproveVarianceAsync(Guid reconciliationId, Guid approverId, string reason, CancellationToken ct)
    {
        var context = RequireContext();
        if (reconciliationId == Guid.Empty || approverId == Guid.Empty)
            throw new ArgumentException("CASH_APPROVAL_INPUT_INVALID");

        var propertyId = context.Property.PropertyId.Value;
        var reconciliation = await db.CashReconciliations
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CashReconciliationId == reconciliationId, ct)
            ?? throw new KeyNotFoundException("CASH_RECONCILIATION_NOT_FOUND");
        reconciliation.Approve(approverId, reason, clock.UtcNow);
        StageAudit("CashReconciliationApproved", reconciliation, propertyId);
        await CommitAsync(ct, "CASH_RECONCILIATION_APPROVAL_FAILED");
        return Map(reconciliation);
    }

    public async Task<IReadOnlyList<CashReconciliationDto>> GetPendingAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);
        var rows = await db.CashReconciliations.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        (x.Status == CashReconciliationStatus.Pending ||
                         x.Status == CashReconciliationStatus.VarianceDetected ||
                         x.Status == CashReconciliationStatus.Escalated))
            .OrderByDescending(x => x.ReviewedAtUtc)
            .Take(500)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private void StageAudit(string action, CashReconciliation reconciliation, Guid propertyId)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(CashReconciliation),
            reconciliation.CashReconciliationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["ShiftId"] = reconciliation.CashierShiftId.ToString("D"),
                ["Status"] = reconciliation.Status.ToString(),
                ["Variance"] = reconciliation.Variance.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private FrontOfficeExecutionContext RequireContext()
        => executionContext.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static CashReconciliationDto Map(CashReconciliation row)
        => new(
            row.CashReconciliationId,
            row.CashierShiftId,
            row.ShiftNumber,
            row.ExpectedCash,
            row.ActualCash,
            row.Variance,
            Enum.TryParse<AppReconciliationStatus>(row.Status.ToString(), true, out var status) ? status : AppReconciliationStatus.Pending,
            Enum.TryParse<VarianceReasonCode>(row.ReasonCode, true, out var reasonCode) ? reasonCode : null,
            row.ReasonDetail,
            row.ReviewedBy,
            row.ReviewedAtUtc,
            row.ApprovedBy,
            row.ApprovedAtUtc);
}
