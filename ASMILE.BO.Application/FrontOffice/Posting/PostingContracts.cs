using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;

namespace ASMILE.BO.Application.FrontOffice.Posting;

// === DTOs ===
public sealed record PostingRequest(
    Guid PropertyId, Guid FolioId, Guid? StayId, byte[] FolioRowVersion,
    HotelFolioEntryType EntryType, decimal Amount, string Description,
    DateOnly BusinessDate, string? ReferenceType = null, Guid? ReferenceId = null,
    decimal TaxRate = 0, decimal ServiceChargeRate = 0, decimal DiscountRate = 0,
    PaymentCurrency Currency = PaymentCurrency.VND, decimal ExchangeRate = 1,
    Guid? RatePlanId = null
);

public sealed record PostingResult(
    bool Success, Guid? FolioEntryId, int SequenceNumber,
    decimal BaseAmount, decimal TaxAmount, decimal ServiceChargeAmount,
    decimal DiscountAmount, decimal NetAmount, string? Error, List<string> AuditTrail
);

public sealed record BatchPostingRequest(
    Guid PropertyId, DateOnly BusinessDate,
    IReadOnlyList<PostingRequest> Postings
);

public sealed record BatchPostingResult(
    int TotalRequested, int TotalPosted, int TotalFailed,
    decimal TotalAmount, decimal TotalTax, decimal TotalServiceCharge,
    IReadOnlyList<PostingResult> Results, IReadOnlyList<string> Warnings
);

public sealed record PropertyLockState(
    Guid PropertyId, DateOnly BusinessDate, bool IsLocked, Guid? NightAuditId,
    DateTime? LockedAtUtc, Guid? LockedBy, string? LockReason
);

public sealed record PostingValidationResult(
    bool IsValid, string? Error, string? BusinessDateStatus,
    bool IsFolioOpen, decimal? CurrentBalance, bool HasOutstandingBalance
);

// === Posting Engine Interface ===
public interface IFrontOfficePostingEngine
{
    Task<PostingResult> PostAsync(PostingRequest request, CancellationToken ct);
    Task<BatchPostingResult> BatchPostAsync(BatchPostingRequest request, CancellationToken ct);
    Task<PostingValidationResult> ValidateAsync(Guid folioId, Guid propertyId, CancellationToken ct);
    Task<bool> CanPostAsync(Guid propertyId, CancellationToken ct);
}

// === Property Lock Interface ===
public interface IPropertyLockManager
{
    Task<PropertyLockState> GetLockStateAsync(Guid propertyId, CancellationToken ct);
    Task<bool> LockAsync(Guid propertyId, Guid nightAuditId, string reason, CancellationToken ct);
    Task<bool> UnlockAsync(Guid propertyId, CancellationToken ct);
    Task<bool> IsLockedAsync(Guid propertyId, CancellationToken ct);
}

// === Financial Orchestration Interface ===
public interface IFinancialProcessService
{
    Task<BatchPostingResult> PostRoomChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
    Task<BatchPostingResult> PostTaxesAndChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
    Task<PostingResult> CalculateAndPostRoomRateAsync(Guid stayId, Guid folioId, decimal baseRate, DateOnly date, int adults, int nights, int totalRooms, int roomsSold, CancellationToken ct);
}
