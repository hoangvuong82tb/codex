using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Payment;

/// <summary>Payment methods — all tender types accepted by the hotel.</summary>
public enum HotelPaymentMethod
{
    Cash, Card, CreditCard, DebitCard, BankTransfer,
    QR, EWallet, CorporateBilling, CityLedger, Voucher,
    Mixed, Split
}

/// <summary>Payment lifecycle states.</summary>
public enum HotelPaymentStatus
{
    Pending,       // Created but not yet authorized
    Authorized,    // Card authorized but not captured
    Captured,      // Card captured, funds held
    Completed,     // Payment finalized
    PartiallyRefunded, // Partial refund processed
    Refunded,      // Fully refunded
    Voided,        // Voided before capture
    Failed,        // Authorization/capture failed
    Cancelled      // Cancelled by user
}

/// <summary>Currency for multi-currency support (model).</summary>
public enum PaymentCurrency { VND, USD, EUR, GBP, JPY, CNY, KRW, SGD }

/// <summary>Split tender — one payment split across multiple methods.</summary>
public sealed record SplitTenderLine(
    HotelPaymentMethod Method, decimal Amount, string? ExternalReference, string? Notes
);

/// <summary>Exchange rate record for multi-currency payments.</summary>
public sealed record ExchangeRate(
    PaymentCurrency From, PaymentCurrency To, decimal Rate, DateTime EffectiveAtUtc
);

public static class HotelPaymentStatusExtensions
{
    public static bool IsTerminal(this HotelPaymentStatus s) => s is HotelPaymentStatus.Completed or HotelPaymentStatus.Refunded or HotelPaymentStatus.Voided or HotelPaymentStatus.Failed or HotelPaymentStatus.Cancelled;
    public static bool CanRefund(this HotelPaymentStatus s) => s is HotelPaymentStatus.Completed or HotelPaymentStatus.Captured or HotelPaymentStatus.PartiallyRefunded;
    public static bool CanVoid(this HotelPaymentStatus s) => s is HotelPaymentStatus.Authorized or HotelPaymentStatus.Pending;
    public static bool CanCapture(this HotelPaymentStatus s) => s == HotelPaymentStatus.Authorized;
}

public sealed class HotelPayment
{
    private readonly List<HotelPaymentAllocation> _allocations = [];
    private readonly List<PaymentHistory> _history = [];
    private HotelPayment() { }

    public Guid HotelPaymentId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelFolioId { get; private set; }
    public Guid? FolioEntryId { get; private set; }
    public Guid? RefundEntryId { get; private set; }
    public string PaymentNumber { get; private set; } = string.Empty;
    public HotelPaymentMethod PaymentMethod { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public decimal AllocatedAmount => _allocations.Sum(a => a.Amount);
    public decimal UnallocatedAmount => Amount - AllocatedAmount;
    public HotelPaymentStatus Status { get; private set; }
    public PaymentCurrency Currency { get; private set; }
    public decimal ExchangeRateToVnd { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }
    public Guid? RefundedBy { get; private set; }
    public string? RefundReason { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<HotelPaymentAllocation> Allocations => _allocations;
    public IReadOnlyList<PaymentHistory> History => _history;

    // =============================================================================
    // FACTORY
    // =============================================================================
    public static HotelPayment Create(Guid propertyId, Guid folioId, string paymentNumber, HotelPaymentMethod method, decimal amount, PaymentCurrency currency, decimal exchangeRate, string? externalReference, string? notes, DateTime nowUtc, ActorId? actorId)
    {
        if (amount <= 0) throw new ArgumentException("PAYMENT_AMOUNT_POSITIVE");
        if (string.IsNullOrWhiteSpace(paymentNumber)) throw new ArgumentException("PAYMENT_NUMBER_REQUIRED");
        var payment = new HotelPayment
        {
            HotelPaymentId = Uuid7.NewGuid(), PropertyId = propertyId, HotelFolioId = folioId,
            PaymentNumber = paymentNumber.Trim(), PaymentMethod = method, Amount = amount,
            Currency = currency, ExchangeRateToVnd = exchangeRate,
            Status = method is HotelPaymentMethod.CreditCard or HotelPaymentMethod.DebitCard ? HotelPaymentStatus.Authorized : HotelPaymentStatus.Completed,
            ExternalReference = Normalize(externalReference), Notes = Normalize(notes),
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value
        };
        payment.RecordHistory("Created", $"Payment created: {method} {amount:N0} {currency}", nowUtc, actorId);
        return payment;
    }

    // =============================================================================
    // STATE TRANSITIONS
    // =============================================================================
    public void Authorize(string authCode, DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelPaymentStatus.Pending);
        Status = HotelPaymentStatus.Authorized;
        AuthorizationCode = authCode;
        RecordHistory("Authorized", $"Authorization: {authCode}", nowUtc, actorId);
    }

    public void Capture(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelPaymentStatus.Authorized);
        Status = HotelPaymentStatus.Captured;
        RecordHistory("Captured", "Payment captured", nowUtc, actorId);
    }

    public void Complete(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelPaymentStatus.Authorized, HotelPaymentStatus.Captured, HotelPaymentStatus.Pending);
        Status = HotelPaymentStatus.Completed;
        RecordHistory("Completed", "Payment completed", nowUtc, actorId);
    }

    public void MarkFailed(string reason, DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelPaymentStatus.Pending, HotelPaymentStatus.Authorized);
        Status = HotelPaymentStatus.Failed;
        RecordHistory("Failed", reason, nowUtc, actorId);
    }

    public void Cancel(string reason, DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelPaymentStatus.Pending, HotelPaymentStatus.Authorized);
        Status = HotelPaymentStatus.Cancelled;
        RecordHistory("Cancelled", reason, nowUtc, actorId);
    }

    public void Refund(decimal amount, Guid refundEntryId, string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (!Status.CanRefund()) throw new InvalidOperationException($"PAYMENT_CANNOT_REFUND: {Status}");
        if (amount <= 0 || amount > Amount - RefundedAmount) throw new ArgumentException("PAYMENT_REFUND_AMOUNT_INVALID");
        RefundedAmount += amount;
        Status = RefundedAmount >= Amount ? HotelPaymentStatus.Refunded : HotelPaymentStatus.PartiallyRefunded;
        RefundEntryId = refundEntryId;
        RefundReason = Normalize(reason);
        RefundedAtUtc = nowUtc;
        RefundedBy = actorId?.Value;
        RecordHistory(Status == HotelPaymentStatus.Refunded ? "FullyRefunded" : "PartiallyRefunded", $"{reason} ({amount:N0})", nowUtc, actorId);
    }

    public void Void(Guid voidEntryId, string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (!Status.CanVoid()) throw new InvalidOperationException($"PAYMENT_CANNOT_VOID: {Status}");
        Status = HotelPaymentStatus.Voided;
        RefundEntryId = voidEntryId;
        VoidReason = Normalize(reason);
        VoidedAtUtc = nowUtc;
        VoidedBy = actorId?.Value;
        RecordHistory("Voided", reason, nowUtc, actorId);
    }

    // =============================================================================
    // ALLOCATION
    // =============================================================================
    public void LinkFolioEntry(Guid folioEntryId) => FolioEntryId = folioEntryId;

    public HotelPaymentAllocation AllocateToCharge(Guid folioEntryId, string chargeDescription, decimal amount, DateTime nowUtc)
    {
        if (Status != HotelPaymentStatus.Completed) throw new InvalidOperationException($"PAYMENT_NOT_COMPLETED: {Status}");
        if (amount <= 0) throw new ArgumentException("ALLOCATION_AMOUNT_POSITIVE");
        if (UnallocatedAmount < amount) throw new InvalidOperationException($"ALLOCATION_EXCEEDS_UNALLOCATED: {amount} > {UnallocatedAmount}");
        var allocation = HotelPaymentAllocation.Create(HotelPaymentId, PropertyId, HotelFolioId, folioEntryId, chargeDescription, amount, nowUtc);
        _allocations.Add(allocation);
        RecordHistory("Allocated", $"Allocated {amount:N0} to charge {chargeDescription}", nowUtc, null);
        return allocation;
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private void RequireStatus(params HotelPaymentStatus[] allowed)
    { if (!allowed.Contains(Status)) throw new InvalidOperationException($"PAYMENT_INVALID_STATE: {Status}"); }

    private void RecordHistory(string action, string detail, DateTime at, ActorId? actor)
        => _history.Add(new PaymentHistory(Uuid7.NewGuid(), HotelPaymentId, PropertyId, action, detail, at, actor?.Value));

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================
public sealed class HotelPaymentAllocation
{
    private HotelPaymentAllocation() { }
    public Guid HotelPaymentAllocationId { get; private set; }
    public Guid HotelPaymentId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelFolioId { get; private set; }
    public Guid ChargeFolioEntryId { get; private set; }
    public string ChargeDescription { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static HotelPaymentAllocation Create(Guid paymentId, Guid propertyId, Guid folioId, Guid chargeEntryId, string description, decimal amount, DateTime nowUtc) => new()
    { HotelPaymentAllocationId = Uuid7.NewGuid(), HotelPaymentId = paymentId, PropertyId = propertyId, HotelFolioId = folioId, ChargeFolioEntryId = chargeEntryId, ChargeDescription = description, Amount = amount, CreatedAtUtc = nowUtc };
}

/// <summary>Immutable payment history snapshot.</summary>
public sealed record PaymentHistory(Guid PaymentHistoryId, Guid HotelPaymentId, Guid PropertyId, string Action, string Detail, DateTime OccurredAtUtc, Guid? ActorId);
