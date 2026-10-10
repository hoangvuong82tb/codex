using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Cashier;

/// <summary>
/// Persistent cashier-shift workflow backed by fo.CaLamViec and fo.BienDongTienMat.
/// DrawerId remains a UI contract value because the legacy schema has no drawer FK.
/// </summary>
public sealed class EfCashShiftService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor executionContext,
    IClock clock) : ICashShiftService
{
    public async Task<CashShiftDto> OpenShiftAsync(Guid drawerId, Guid cashierId, decimal openingFloat, CancellationToken ct)
    {
        var context = RequireContext();
        if (drawerId == Guid.Empty || cashierId == Guid.Empty)
            throw new ArgumentException("CASH_SHIFT_IDENTIFIERS_REQUIRED");
        if (openingFloat < 0)
            throw new ArgumentOutOfRangeException(nameof(openingFloat), "CASH_OPENING_NEGATIVE");

        var propertyId = context.Property.PropertyId.Value;
        if (await db.CashierShifts.AnyAsync(x => x.PropertyId == propertyId && x.CashierId == cashierId && x.Status == CashierShiftStatus.Open, ct))
            throw new InvalidOperationException("CASHIER_ALREADY_HAS_OPEN_SHIFT");

        var now = clock.UtcNow;
        var shift = CashierShift.Open(propertyId, cashierId, await NextShiftNumberAsync(context.BusinessDate.BusinessDate, ct), context.BusinessDate.BusinessDate, openingFloat, now, context.Actor.ActorId);
        db.CashierShifts.Add(shift);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(shift, drawerId, context.Property.CurrencyCode, null, ct);
    }

    public async Task<CashShiftDto> CloseShiftAsync(Guid shiftId, decimal actualCash, string? varianceReason, CancellationToken ct)
    {
        var context = RequireContext();
        if (shiftId == Guid.Empty || actualCash < 0)
            throw new ArgumentException("CASH_CLOSE_INPUT_INVALID");

        var shift = await LoadShiftAsync(context.Property.PropertyId.Value, shiftId, ct)
            ?? throw new KeyNotFoundException("CASH_SHIFT_NOT_FOUND");
        var movementCount = shift.Movements.Count;
        shift.Close(actualCash, actualCash, clock.UtcNow, context.Actor.ActorId);
        db.CashMovements.Add(shift.Movements.Skip(movementCount).Single());
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(shift, Guid.Empty, context.Property.CurrencyCode, varianceReason, ct);
    }

    public async Task<ShiftHandoverDto> HandOverAsync(Guid fromShiftId, Guid toCashierId, decimal countedCash, string? notes, CancellationToken ct)
    {
        var context = RequireContext();
        if (fromShiftId == Guid.Empty || toCashierId == Guid.Empty || countedCash < 0)
            throw new ArgumentException("CASH_HANDOVER_INPUT_INVALID");

        var propertyId = context.Property.PropertyId.Value;
        var from = await LoadShiftAsync(propertyId, fromShiftId, ct)
            ?? throw new KeyNotFoundException("CASH_SHIFT_NOT_FOUND");
        if (from.Status != CashierShiftStatus.Open)
            throw new InvalidOperationException("CASH_SHIFT_NOT_OPEN");
        if (from.CashierId == toCashierId)
            throw new InvalidOperationException("CASH_HANDOVER_TARGET_MUST_DIFFER");
        if (await db.CashierShifts.AnyAsync(x => x.PropertyId == propertyId && x.CashierId == toCashierId && x.Status == CashierShiftStatus.Open, ct))
            throw new InvalidOperationException("CASHIER_ALREADY_HAS_OPEN_SHIFT");

        var expected = from.ExpectedCash;
        var now = clock.UtcNow;
        var movementCount = from.Movements.Count;
        var handoverNote = $"Handover to cashier {toCashierId:D}";
        if (!string.IsNullOrWhiteSpace(notes))
            handoverNote += $" | {notes.Trim()}";
        from.Close(countedCash, countedCash, now, context.Actor.ActorId, handoverNote);
        db.CashMovements.Add(from.Movements.Skip(movementCount).Single());
        var to = CashierShift.Open(propertyId, toCashierId, await NextShiftNumberAsync(context.BusinessDate.BusinessDate, ct), context.BusinessDate.BusinessDate, countedCash, now, context.Actor.ActorId);
        db.CashierShifts.Add(to);
        await db.SaveChangesAsync(ct);
        return new ShiftHandoverDto(from.CashierShiftId, to.CashierShiftId, from.CashierId, toCashierId, countedCash, expected, countedCash - expected, notes, now);
    }

    public async Task<CashShiftDto> GetShiftAsync(Guid shiftId, CancellationToken ct)
    {
        var context = RequireContext();
        var shift = await LoadShiftAsync(context.Property.PropertyId.Value, shiftId, ct)
            ?? throw new KeyNotFoundException("CASH_SHIFT_NOT_FOUND");
        return await ToDtoAsync(shift, Guid.Empty, context.Property.CurrencyCode, null, ct);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetActiveShiftsAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");

        var shifts = await db.CashierShifts.AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == propertyId && x.Status == CashierShiftStatus.Open)
            .OrderBy(x => x.OpenedAtUtc)
            .ToListAsync(ct);
        return await ToDtosAsync(shifts, context.Property.CurrencyCode, ct);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetShiftHistoryAsync(Guid propertyId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var context = RequireContext();
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            throw new ArgumentException("CASH_SHIFT_HISTORY_RANGE_INVALID");

        var query = db.CashierShifts.AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == propertyId);
        if (fromUtc.HasValue)
            query = query.Where(x => x.OpenedAtUtc >= fromUtc.Value || (x.ClosedAtUtc.HasValue && x.ClosedAtUtc.Value >= fromUtc.Value));
        if (toUtc.HasValue)
            query = query.Where(x => x.OpenedAtUtc <= toUtc.Value || (x.ClosedAtUtc.HasValue && x.ClosedAtUtc.Value <= toUtc.Value));

        var shifts = await query.OrderByDescending(x => x.OpenedAtUtc).Take(500).ToListAsync(ct);
        return await ToDtosAsync(shifts, context.Property.CurrencyCode, ct);
    }

    public async Task<IReadOnlyList<CashierOptionDto>> GetCashierOptionsAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");

        return await db.AppUsers.AsNoTracking()
            .Where(x => x.IsActive && x.DisplayName != "")
            .OrderBy(x => x.DisplayName)
            .Select(x => new CashierOptionDto(x.Id, x.DisplayName, x.UserName))
            .ToListAsync(ct);
    }

    private Task<CashierShift?> LoadShiftAsync(Guid propertyId, Guid shiftId, CancellationToken ct) =>
        db.CashierShifts.Include(x => x.Movements).SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CashierShiftId == shiftId, ct);

    private async Task<string> NextShiftNumberAsync(DateOnly businessDate, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"CS-{businessDate:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
            if (!await db.CashierShifts.AnyAsync(x => x.ShiftNumber == candidate, ct))
                return candidate;
        }
        throw new InvalidOperationException("CASH_SHIFT_NUMBER_UNAVAILABLE");
    }

    private async Task<IReadOnlyList<CashShiftDto>> ToDtosAsync(IReadOnlyList<CashierShift> shifts, string? currencyCode, CancellationToken ct)
    {
        var cashierIds = shifts.Select(x => x.CashierId).Distinct().ToArray();
        var names = await db.AppUsers.AsNoTracking().Where(x => cashierIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        var currency = ResolveCurrency(currencyCode);
        return shifts.Select(x => ToDto(x, Guid.Empty, currency, names.GetValueOrDefault(x.CashierId) ?? string.Empty)).ToList();
    }

    private async Task<CashShiftDto> ToDtoAsync(CashierShift shift, Guid drawerId, string? currencyCode, string? varianceReason, CancellationToken ct)
    {
        var cashierName = await db.AppUsers.AsNoTracking().Where(x => x.Id == shift.CashierId).Select(x => x.DisplayName).SingleOrDefaultAsync(ct) ?? string.Empty;
        return ToDto(shift, drawerId, ResolveCurrency(currencyCode), cashierName, varianceReason);
    }

    private static CashShiftDto ToDto(CashierShift shift, Guid drawerId, PaymentCurrency currency, string cashierName, string? varianceReason = null) =>
        new(shift.CashierShiftId, shift.ShiftNumber, drawerId, shift.CashierId, cashierName, shift.Status, shift.OpeningCash, shift.ExpectedCash, shift.ActualCash, shift.Difference, varianceReason, shift.OpenedAtUtc, shift.ClosedAtUtc, shift.ClosedBy,
            shift.Movements.Select(m => new CashMovementDto(m.CashMovementId, m.Type, m.Amount, m.Description, currency, 1m, m.PaymentId, m.CreatedAtUtc)).ToList());

    private FrontOfficeExecutionContext RequireContext() => executionContext.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static PaymentCurrency ResolveCurrency(string? code) =>
        Enum.TryParse<PaymentCurrency>(code, true, out var currency) ? currency : PaymentCurrency.VND;
}
