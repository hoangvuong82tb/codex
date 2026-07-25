using ASMILE.BO.Core.FrontOffice.Cashier;

namespace ASMILE.BO.Application.FrontOffice.Cashier;

// =============================================================================
// DRAWER & SHIFT MANAGEMENT
// =============================================================================

public enum DrawerStatus { Closed, Opening, Open, Suspended, Closing, Reconciled, Archived }
public enum CashOperationType { OpenDrawer, CloseDrawer, Deposit, Withdraw, Transfer, Correction, PettyCash, PaidOut, PaidIn, Refund, Void, HandOver, TakeOver }

public sealed record CashDrawerDto(
    Guid DrawerId, string DrawerCode, Guid? CurrentShiftId, Guid? CurrentCashierId,
    string? CurrentCashierName, DrawerStatus Status, decimal ExpectedCash,
    decimal ActualCash, decimal Variance, DateTime? OpenedAtUtc, DateTime? ClosedAtUtc
);

public sealed record CashShiftDto(
    Guid ShiftId, string ShiftNumber, Guid DrawerId, Guid CashierId, string CashierName,
    CashierShiftStatus Status, decimal OpeningCash, decimal ExpectedCash,
    decimal? ActualCash, decimal? Variance, string? VarianceReason,
    DateTime OpenedAtUtc, DateTime? ClosedAtUtc, Guid? ClosedBy,
    IReadOnlyList<CashMovementDto> Movements
);

public sealed record CashMovementDto(
    Guid MovementId, CashMovementType Type, decimal Amount, string Description,
    PaymentCurrency Currency, decimal ExchangeRate, Guid? PaymentId, DateTime CreatedAtUtc
);

public sealed record ShiftHandoverDto(
    Guid FromShiftId, Guid ToShiftId, Guid FromCashierId, Guid ToCashierId,
    decimal HandoverAmount, decimal ExpectedAmount, decimal Variance,
    string? Notes, DateTime HandoverAtUtc
);

// =============================================================================
// RECONCILIATION
// =============================================================================

public enum ReconciliationStatus { Pending, InProgress, VarianceDetected, Approved, Completed, Escalated }
public enum VarianceReasonCode { CountingError, MissingReceipt, UnrecordedPayment, UnrecordedRefund, FraudSuspect, SystemError, RoundingDifference, Other }

public sealed record CashReconciliationDto(
    Guid ReconciliationId, Guid ShiftId, string ShiftNumber,
    decimal ExpectedCash, decimal ActualCash, decimal Variance,
    ReconciliationStatus Status, VarianceReasonCode? ReasonCode,
    string? ReasonDetail, Guid? ReviewedBy, DateTime? ReviewedAtUtc,
    Guid? ApprovedBy, DateTime? ApprovedAtUtc
);

// =============================================================================
// MULTI-CURRENCY
// =============================================================================

public enum PaymentCurrency { VND, USD, EUR, GBP, JPY, CNY, KRW, SGD }
public sealed record CurrencyHoldingDto(PaymentCurrency Currency, decimal Amount, decimal ExchangeRateToBase, decimal BaseAmount);
public sealed record CurrencyExchangeDto(Guid ShiftId, PaymentCurrency From, PaymentCurrency To, decimal Amount, decimal Rate, decimal GainLoss, DateTime ExchangedAtUtc);

// =============================================================================
// DASHBOARD
// =============================================================================

public sealed record CashierDashboardDto(
    Guid CashierId, string CashierName, Guid? CurrentShiftId, string? CurrentDrawer,
    DrawerStatus DrawerStatus, decimal CurrentBalance, decimal OpeningFloat,
    int TransactionsToday, decimal TotalCashIn, decimal TotalCashOut,
    bool HasVariance, DateTime ShiftStartedAt
);

public sealed record CashierSupervisorDashboardDto(
    int OpenDrawerCount, int ActiveShifts, int PendingReconciliations,
    int VariancesDetected, decimal TotalVarianceToday,
    IReadOnlyList<CashReconciliationDto> Unresolved,
    IReadOnlyList<CashDrawerDto> OpenDrawersList
);

public sealed record TreasuryDashboardDto(
    decimal TotalCashOnHand, decimal TotalDepositsToday, decimal TotalWithdrawalsToday,
    int ActiveShiftCount, int ReconciledShiftCount, decimal TotalVariance
);

// =============================================================================
// CONTRACTS
// =============================================================================

public interface ICashDrawerService
{
    Task<CashDrawerDto> OpenDrawerAsync(Guid drawerId, Guid cashierId, decimal openingFloat, CancellationToken ct);
    Task<CashDrawerDto> CloseDrawerAsync(Guid drawerId, decimal actualCash, CancellationToken ct);
    Task<CashDrawerDto> GetDrawerAsync(Guid drawerId, CancellationToken ct);
    Task<IReadOnlyList<CashDrawerDto>> GetOpenDrawersAsync(Guid propertyId, CancellationToken ct);
}

public interface ICashShiftService
{
    Task<CashShiftDto> OpenShiftAsync(Guid drawerId, Guid cashierId, decimal openingFloat, CancellationToken ct);
    Task<CashShiftDto> CloseShiftAsync(Guid shiftId, decimal actualCash, string? varianceReason, CancellationToken ct);
    Task<ShiftHandoverDto> HandOverAsync(Guid fromShiftId, Guid toCashierId, decimal countedCash, string? notes, CancellationToken ct);
    Task<CashShiftDto> GetShiftAsync(Guid shiftId, CancellationToken ct);
    Task<IReadOnlyList<CashShiftDto>> GetActiveShiftsAsync(Guid propertyId, CancellationToken ct);
}

public interface ICashReconciliationService
{
    Task<CashReconciliationDto> ReconcileAsync(Guid shiftId, decimal actualCash, CancellationToken ct);
    Task<CashReconciliationDto> ApproveVarianceAsync(Guid reconciliationId, Guid approverId, string reason, CancellationToken ct);
    Task<IReadOnlyList<CashReconciliationDto>> GetPendingAsync(Guid propertyId, CancellationToken ct);
}

public interface ICashierDashboardService
{
    Task<CashierDashboardDto> GetCashierDashboardAsync(Guid cashierId, CancellationToken ct);
    Task<CashierSupervisorDashboardDto> GetSupervisorDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<TreasuryDashboardDto> GetTreasuryDashboardAsync(Guid propertyId, CancellationToken ct);
}

// =============================================================================
// EVENTS
// =============================================================================

public static class CashierEvents
{
    public const string DrawerOpened = "Cashier.DrawerOpened";
    public const string DrawerClosed = "Cashier.DrawerClosed";
    public const string ShiftOpened = "Cashier.ShiftOpened";
    public const string ShiftClosed = "Cashier.ShiftClosed";
    public const string ShiftHandedOver = "Cashier.ShiftHandedOver";
    public const string CashTransferred = "Cashier.CashTransferred";
    public const string CashAdjusted = "Cashier.CashAdjusted";
    public const string CashVarianceDetected = "Cashier.CashVarianceDetected";
    public const string ReconciliationCompleted = "Cashier.ReconciliationCompleted";
    public const string ReconciliationApproved = "Cashier.ReconciliationApproved";
}
