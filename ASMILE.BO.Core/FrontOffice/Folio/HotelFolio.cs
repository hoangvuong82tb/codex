using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Payment;

namespace ASMILE.BO.Core.FrontOffice.Folio;

/// <summary>Unified posting types — every financial transaction on a folio.</summary>
public enum HotelFolioEntryType
{
    // === Revenue ===
    RoomCharge,         // Nightly room rate (from RateEngine)
    Restaurant,         // Restaurant / F&B charge
    Laundry,            // Laundry service
    MiniBar,            // Minibar consumption
    Spa,                // Spa treatment
    Telephone,          // Telephone / communication
    OtherCharge,        // Miscellaneous charge

    // === Tax & Fees ===
    Tax,                // VAT / government tax
    ServiceCharge,      // Service charge %

    // === Adjustments ===
    Discount,           // Promotional / negotiated discount
    Package,            // Package-inclusive item (zero-charge)
    Adjustment,         // Manual adjustment (±)
    Correction,         // Error correction
    Transfer,           // Move entry to another folio

    // === Payments ===
    Payment,            // Cash, card, bank transfer
    Deposit,            // Advance payment / guarantee
    Refund,             // Return of payment

    // === Manual ===
    ManualCharge,       // Manual debit
    ManualCredit,       // Manual credit

    // === Balance ===
    OpeningBalance,     // Initial balance carried forward
    ClosingBalance      // Final balance at folio close
}

/// <summary>Folio lifecycle states.</summary>
public enum HotelFolioStatus
{
    Open,       // Accepting postings
    Suspended,  // Temporarily frozen (dispute, review)
    Closed,     // No more postings, ready for settlement
    Settled,    // All payments settled
    Archived    // Long-term storage
}

public static class HotelFolioStatusExtensions
{
    public static bool IsMutable(this HotelFolioStatus s) => s is HotelFolioStatus.Open;
    public static bool IsTerminal(this HotelFolioStatus s) => s is HotelFolioStatus.Settled or HotelFolioStatus.Archived;
    public static bool CanReopen(this HotelFolioStatus s) => s is HotelFolioStatus.Closed or HotelFolioStatus.Settled;
}

public sealed class HotelFolio
{
    private readonly List<HotelFolioEntry> _entries = [];
    private HotelFolio() { }

    public Guid HotelFolioId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelStayId { get; private set; }
    public string FolioNumber { get; private set; } = string.Empty;
    public HotelFolioStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }
    public Guid? SettledBy { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<HotelFolioEntry> Entries => _entries;
    public decimal RunningBalance => _entries.Sum(e => e.Amount);
    public decimal TotalCharges => _entries.Where(e => e.Amount > 0).Sum(e => e.Amount);
    public decimal TotalPayments => _entries.Where(e => e.Amount < 0).Sum(e => -e.Amount);
    public decimal OutstandingBalance => Math.Max(0, RunningBalance);

    // =============================================================================
    // FACTORY
    // =============================================================================
    public static HotelFolio Create(Guid propertyId, Guid hotelStayId, string folioNumber, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(folioNumber)) throw new ArgumentException("FOLIO_NUMBER_REQUIRED");
        return new HotelFolio { HotelFolioId = Uuid7.NewGuid(), PropertyId = propertyId, HotelStayId = hotelStayId, FolioNumber = folioNumber.Trim(), Status = HotelFolioStatus.Open, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };
    }

    // =============================================================================
    // STATE TRANSITIONS
    // =============================================================================
    public void Suspend(DateTime nowUtc, ActorId? actorId) { RequireStatus(HotelFolioStatus.Open); Status = HotelFolioStatus.Suspended; }
    public void Resume(DateTime nowUtc, ActorId? actorId) { RequireStatus(HotelFolioStatus.Suspended); Status = HotelFolioStatus.Open; }
    public void Close(DateTime nowUtc, ActorId? actorId) { RequireStatus(HotelFolioStatus.Open, HotelFolioStatus.Suspended); Status = HotelFolioStatus.Closed; ClosedAtUtc = nowUtc; ClosedBy = actorId?.Value; }

    public void Settle(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(HotelFolioStatus.Closed);
        AddEntry(HotelFolioEntryType.ClosingBalance, -RunningBalance, "Folio settled — balance zeroed", null, null, null, nowUtc, actorId);
        Status = HotelFolioStatus.Settled;
        SettledAtUtc = nowUtc;
        SettledBy = actorId?.Value;
    }

    public void Reopen(DateTime nowUtc, ActorId? actorId)
    {
        if (!Status.CanReopen()) throw new InvalidOperationException("FOLIO_CANNOT_REOPEN");
        Status = HotelFolioStatus.Open;
        ClosedAtUtc = null;
        ClosedBy = null;
        SettledAtUtc = null;
        SettledBy = null;
    }

    public void Archive(DateTime nowUtc, ActorId? actorId) { RequireStatus(HotelFolioStatus.Settled); Status = HotelFolioStatus.Archived; ArchivedAtUtc = nowUtc; }

    // =============================================================================
    // CHARGE POSTINGS
    // =============================================================================
    public HotelFolioEntry AddRoomCharge(decimal amount, string description, DateOnly businessDate, Guid? ratePlanId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.RoomCharge, amount, description, "RatePlan", ratePlanId, businessDate, nowUtc, actorId); }
    public HotelFolioEntry AddRestaurantCharge(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Restaurant, amount, description, "RestaurantOrder", referenceId, null, nowUtc, actorId); }
    public HotelFolioEntry AddLaundryCharge(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Laundry, amount, description, "LaundryOrder", referenceId, null, nowUtc, actorId); }
    public HotelFolioEntry AddMiniBarCharge(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.MiniBar, amount, description, "MiniBarConsumption", referenceId, null, nowUtc, actorId); }
    public HotelFolioEntry AddSpaCharge(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Spa, amount, description, "SpaBooking", referenceId, null, nowUtc, actorId); }
    public HotelFolioEntry AddTelephoneCharge(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Telephone, amount, description, "CallRecord", referenceId, null, nowUtc, actorId); }

    // === Tax & Fees ===
    public HotelFolioEntry AddTax(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); if (amount <= 0) throw new ArgumentException("FOLIO_AMOUNT_POSITIVE"); return AddEntry(HotelFolioEntryType.Tax, amount, description, null, null, null, nowUtc, actorId); }
    public HotelFolioEntry AddServiceCharge(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); if (amount <= 0) throw new ArgumentException("FOLIO_AMOUNT_POSITIVE"); return AddEntry(HotelFolioEntryType.ServiceCharge, amount, description, null, null, null, nowUtc, actorId); }

    // === Adjustments ===
    public HotelFolioEntry AddDiscount(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Discount, -amount, description, null, null, null, nowUtc, actorId); }
    public HotelFolioEntry AddPackage(decimal amount, string description, Guid? referenceId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Package, -amount, description, "Package", referenceId, null, nowUtc, actorId); }
    public HotelFolioEntry AddAdjustment(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); if (amount == 0) throw new ArgumentException("FOLIO_AMOUNT_NONZERO"); return AddEntry(HotelFolioEntryType.Adjustment, amount, description, null, null, null, nowUtc, actorId); }
    public HotelFolioEntry AddCorrection(decimal amount, string description, Guid? originalEntryId, DateTime nowUtc, ActorId? actorId) { RequireMutable(); if (amount == 0) throw new ArgumentException("FOLIO_AMOUNT_NONZERO"); var entry = AddEntry(HotelFolioEntryType.Correction, amount, description, null, null, null, nowUtc, actorId); if (originalEntryId.HasValue) { entry.MarkAsReversal(originalEntryId.Value); var orig = _entries.FirstOrDefault(e => e.HotelFolioEntryId == originalEntryId.Value); orig?.MarkReversedBy(entry.HotelFolioEntryId); } return entry; }

    // === Payments ===
    public HotelFolioEntry AddPayment(decimal amount, string description, HotelPaymentMethod? method, string? externalRef, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); var refType = method.HasValue ? $"Payment:{method}" : "Payment"; return AddEntry(HotelFolioEntryType.Payment, -amount, description, refType, null, null, nowUtc, actorId); }
    public HotelFolioEntry AddDeposit(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Deposit, -amount, description, "Deposit", null, null, nowUtc, actorId); }
    public HotelFolioEntry AddRefund(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.Refund, amount, description, "Refund", null, null, nowUtc, actorId); }

    // === Manual ===
    public HotelFolioEntry AddManualCharge(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.ManualCharge, amount, description, null, null, null, nowUtc, actorId); }
    public HotelFolioEntry AddManualCredit(decimal amount, string description, DateTime nowUtc, ActorId? actorId) { RequireMutable(); RequirePositive(amount); return AddEntry(HotelFolioEntryType.ManualCredit, -amount, description, null, null, null, nowUtc, actorId); }

    // === Balance ===
    public HotelFolioEntry AddOpeningBalance(decimal amount, DateTime nowUtc, ActorId? actorId) { RequireMutable(); return AddEntry(HotelFolioEntryType.OpeningBalance, amount, "Opening balance", null, null, null, nowUtc, actorId); }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private HotelFolioEntry AddEntry(HotelFolioEntryType type, decimal amount, string description, string? referenceType, Guid? referenceId, DateOnly? businessDate, DateTime nowUtc, ActorId? actorId)
    {
        var entry = HotelFolioEntry.Create(HotelFolioId, PropertyId, _entries.Count + 1, type, amount, description, referenceType, referenceId, businessDate, nowUtc, actorId);
        _entries.Add(entry);
        return entry;
    }

    private void RequireMutable() { if (!Status.IsMutable()) throw new InvalidOperationException($"FOLIO_NOT_MUTABLE: Status is {Status}."); }
    private void RequireStatus(params HotelFolioStatus[] allowed) { if (!allowed.Contains(Status)) throw new InvalidOperationException($"FOLIO_INVALID_STATE: {Status} not in [{string.Join(",", allowed)}]."); }
    private static void RequirePositive(decimal amount) { if (amount <= 0) throw new ArgumentException("FOLIO_AMOUNT_POSITIVE"); }
}

// =============================================================================
// FOLIO ENTRY (IMMUTABLE)
// =============================================================================
public sealed class HotelFolioEntry
{
    private HotelFolioEntry() { }
    public Guid HotelFolioEntryId { get; private set; }
    public Guid HotelFolioId { get; private set; }
    public Guid PropertyId { get; private set; }
    public int SequenceNumber { get; private set; }
    public HotelFolioEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }           // Positive = debit, Negative = credit
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateOnly? BusinessDate { get; private set; }   // When the charge was incurred
    public Guid? ReversesEntryId { get; private set; }
    public Guid? ReversedByEntryId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public static HotelFolioEntry Create(Guid folioId, Guid propertyId, int sequence, HotelFolioEntryType type, decimal amount, string description, string? referenceType, Guid? referenceId, DateOnly? businessDate, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("FOLIO_DESCRIPTION_REQUIRED");
        return new HotelFolioEntry { HotelFolioEntryId = Uuid7.NewGuid(), HotelFolioId = folioId, PropertyId = propertyId, SequenceNumber = sequence, EntryType = type, Amount = amount, Description = description.Trim(), ReferenceType = referenceType, ReferenceId = referenceId, BusinessDate = businessDate, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };
    }

    internal void MarkReversedBy(Guid reversalEntryId) => ReversedByEntryId = reversalEntryId;
    internal void MarkAsReversal(Guid originalEntryId) => ReversesEntryId = originalEntryId;
}

// =============================================================================
// REVENUE CATEGORY
// =============================================================================
public enum RevenueBucket { Rooms, FoodAndBeverage, SpaAndWellness, Laundry, Telephone, MiniBar, Parking, Entertainment, OtherRevenue, ServiceCharge, Tax, Discount, Adjustment }

public sealed class RevenueCategory
{
    private RevenueCategory() { }
    public Guid RevenueCategoryId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public RevenueBucket Bucket { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsTaxInclusive { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static RevenueCategory Create(Guid propertyId, string code, string name, RevenueBucket bucket, decimal taxRate = 0, bool taxInclusive = false) => new() { RevenueCategoryId = Uuid7.NewGuid(), PropertyId = propertyId, Code = code, Name = name, Bucket = bucket, TaxRate = taxRate, IsTaxInclusive = taxInclusive, IsActive = true };
}

// =============================================================================
// TAX RULE
// =============================================================================
public enum TaxRuleType { Vat, ServiceCharge, CityTax, TourismTax, EnvironmentalFee, ResortFee }

public sealed class TaxRule
{
    private TaxRule() { }
    public Guid TaxRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public TaxRuleType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public RevenueBucket AppliesTo { get; private set; }
    public decimal? MinimumCharge { get; private set; }
    public decimal? MaximumCharge { get; private set; }

    public static TaxRule Create(Guid propertyId, TaxRuleType type, string name, decimal rate, RevenueBucket appliesTo, decimal? min = null, decimal? max = null) => new() { TaxRuleId = Uuid7.NewGuid(), PropertyId = propertyId, Type = type, Name = name, Rate = rate, AppliesTo = appliesTo, MinimumCharge = min, MaximumCharge = max, IsActive = true };

    public decimal Calculate(decimal taxableAmount)
    {
        var tax = taxableAmount * Rate / 100;
        if (MinimumCharge.HasValue && tax < MinimumCharge.Value) tax = MinimumCharge.Value;
        if (MaximumCharge.HasValue && tax > MaximumCharge.Value) tax = MaximumCharge.Value;
        return tax;
    }
}
