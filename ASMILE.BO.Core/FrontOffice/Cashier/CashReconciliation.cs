namespace ASMILE.BO.Core.FrontOffice.Cashier;

public enum CashReconciliationStatus { Pending, VarianceDetected, Approved, Completed, Escalated }

public sealed class CashReconciliation
{
    private CashReconciliation() { }

    public Guid CashReconciliationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid CashierShiftId { get; private set; }
    public string ShiftNumber { get; private set; } = string.Empty;
    public decimal ExpectedCash { get; private set; }
    public decimal ActualCash { get; private set; }
    public decimal Variance { get; private set; }
    public CashReconciliationStatus Status { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? ReasonDetail { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static CashReconciliation Create(
        Guid propertyId,
        CashierShift shift,
        decimal actualCash,
        Guid reviewerId,
        string? reasonCode,
        string? reasonDetail,
        DateTime nowUtc)
    {
        if (actualCash < 0) throw new ArgumentOutOfRangeException(nameof(actualCash), "CASH_RECONCILIATION_AMOUNT_INVALID");
        if (reviewerId == Guid.Empty) throw new ArgumentException("CASH_REVIEWER_REQUIRED", nameof(reviewerId));

        var expected = shift.ExpectedCash;
        var variance = actualCash - expected;
        return new CashReconciliation
        {
            CashReconciliationId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            CashierShiftId = shift.CashierShiftId,
            ShiftNumber = shift.ShiftNumber,
            ExpectedCash = expected,
            ActualCash = actualCash,
            Variance = variance,
            Status = variance == 0 ? CashReconciliationStatus.Completed : CashReconciliationStatus.VarianceDetected,
            ReasonCode = variance == 0 ? null : reasonCode,
            ReasonDetail = variance == 0 ? null : reasonDetail,
            ReviewedBy = reviewerId,
            ReviewedAtUtc = nowUtc
        };
    }

    public void Approve(Guid approverId, string reason, DateTime nowUtc)
    {
        if (approverId == Guid.Empty) throw new ArgumentException("CASH_APPROVER_REQUIRED", nameof(approverId));
        if (Status != CashReconciliationStatus.VarianceDetected)
            throw new InvalidOperationException("CASH_RECONCILIATION_NOT_APPROVABLE");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("CASH_APPROVAL_REASON_REQUIRED", nameof(reason));

        Status = CashReconciliationStatus.Approved;
        ReasonDetail = reason.Trim();
        ApprovedBy = approverId;
        ApprovedAtUtc = nowUtc;
    }
}
