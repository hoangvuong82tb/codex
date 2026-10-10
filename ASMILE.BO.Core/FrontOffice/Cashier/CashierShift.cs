using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Cashier;

public enum CashierShiftStatus { Closed, Open, Closing }
public enum CashMovementType { OpeningCash, CashIn, CashOut, Refund, TransferOut, TransferIn, Adjustment, ClosingCash }

public sealed class CashierShift
{
    private readonly List<CashMovement> _movements = [];
    private CashierShift() { }

    public Guid CashierShiftId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ShiftNumber { get; private set; } = string.Empty;
    public Guid CashierId { get; private set; }
    public CashierShiftStatus Status { get; private set; }
    public decimal OpeningCash { get; private set; }
    public decimal ClosingCash { get; private set; }
    public decimal ExpectedCash => OpeningCash
        + _movements.Where(m => m.Type is CashMovementType.CashIn or CashMovementType.TransferIn).Sum(m => m.Amount)
        - _movements.Where(m => m.Type is CashMovementType.CashOut or CashMovementType.Refund or CashMovementType.TransferOut).Sum(m => m.Amount)
        + _movements.Where(m => m.Type == CashMovementType.Adjustment).Sum(m => m.Amount);
    public decimal? ActualCash { get; private set; }
    public decimal? Difference => ActualCash.HasValue ? ActualCash.Value - ExpectedCash : null;
    public DateOnly BusinessDate { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<CashMovement> Movements => _movements;

    public static CashierShift Open(Guid propertyId, Guid cashierId, string shiftNumber, DateOnly businessDate, decimal openingCash, DateTime nowUtc, ActorId? actorId)
    {
        if (openingCash < 0) throw new ArgumentException("CASH_OPENING_NEGATIVE");
        var shift = new CashierShift { CashierShiftId = Uuid7.NewGuid(), PropertyId = propertyId, CashierId = cashierId, ShiftNumber = shiftNumber, BusinessDate = businessDate, OpeningCash = openingCash, Status = CashierShiftStatus.Open, OpenedAtUtc = nowUtc };
        shift._movements.Add(CashMovement.Create(shift.CashierShiftId, propertyId, CashMovementType.OpeningCash, openingCash, "Opening cash", nowUtc, actorId));
        return shift;
    }

    public CashMovement RecordCashIn(decimal amount, string description, Guid? paymentId, DateTime nowUtc, ActorId? actorId)
    { RequireOpen(); if (amount <= 0) throw new ArgumentException("CASH_AMOUNT_POSITIVE"); var m = CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.CashIn, amount, description, nowUtc, actorId, paymentId); _movements.Add(m); return m; }

    public CashMovement RecordCashOut(decimal amount, string description, DateTime nowUtc, ActorId? actorId)
    { RequireOpen(); if (amount <= 0) throw new ArgumentException("CASH_AMOUNT_POSITIVE"); var m = CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.CashOut, amount, description, nowUtc, actorId); _movements.Add(m); return m; }

    public CashMovement RecordRefund(decimal amount, string description, Guid? paymentId, DateTime nowUtc, ActorId? actorId)
    { RequireOpen(); if (amount <= 0) throw new ArgumentException("CASH_AMOUNT_POSITIVE"); var m = CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.Refund, amount, description, nowUtc, actorId, paymentId); _movements.Add(m); return m; }

    public CashMovement RecordTransferOut(decimal amount, string reason, DateTime nowUtc, ActorId? actorId)
    { RequireOpen(); if (amount <= 0) throw new ArgumentException("CASH_AMOUNT_POSITIVE"); var m = CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.TransferOut, amount, reason, nowUtc, actorId); _movements.Add(m); return m; }

    public CashMovement RecordAdjustment(decimal amount, string reason, DateTime nowUtc, ActorId? actorId)
    { RequireOpen(); if (amount == 0) throw new ArgumentException("CASH_AMOUNT_NONZERO"); var m = CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.Adjustment, amount, reason, nowUtc, actorId); _movements.Add(m); return m; }

    public void Close(decimal actualCash, decimal closingCash, DateTime nowUtc, ActorId? actorId, string? closingNote = null)
    {
        RequireOpen();
        Status = CashierShiftStatus.Closing;
        ActualCash = actualCash;
        ClosingCash = closingCash;

        var description = $"Close shift: actual={actualCash}, expected={ExpectedCash}, diff={actualCash - ExpectedCash}";
        if (!string.IsNullOrWhiteSpace(closingNote))
        {
            var note = closingNote.Trim();
            if (note.Length > 350)
                note = note[..350] + "...";
            description += $" | {note}";
        }

        _movements.Add(CashMovement.Create(CashierShiftId, PropertyId, CashMovementType.ClosingCash, closingCash, description, nowUtc, actorId));
        Status = CashierShiftStatus.Closed;
        ClosedAtUtc = nowUtc;
        ClosedBy = actorId?.Value;
    }

    private void RequireOpen() { if (Status != CashierShiftStatus.Open) throw new InvalidOperationException("SHIFT_NOT_OPEN"); }
}

public sealed class CashMovement
{
    private CashMovement() { }
    public Guid CashMovementId { get; private set; }
    public Guid CashierShiftId { get; private set; }
    public Guid PropertyId { get; private set; }
    public CashMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? PaymentId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public static CashMovement Create(Guid shiftId, Guid propertyId, CashMovementType type, decimal amount, string description, DateTime nowUtc, ActorId? actorId, Guid? paymentId = null) => new()
    { CashMovementId = Uuid7.NewGuid(), CashierShiftId = shiftId, PropertyId = propertyId, Type = type, Amount = amount, Description = description, PaymentId = paymentId, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };
}
