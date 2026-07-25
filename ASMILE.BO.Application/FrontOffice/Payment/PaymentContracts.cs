using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Payment;

namespace ASMILE.BO.Application.FrontOffice.Payment;

// === DTOs ===
public sealed record HotelPaymentDto(Guid HotelPaymentId, string PaymentNumber, Guid HotelFolioId, HotelPaymentMethod PaymentMethod, decimal Amount, decimal RefundedAmount, decimal AllocatedAmount, HotelPaymentStatus Status, PaymentCurrency Currency, string? AuthorizationCode, string? ExternalReference, string? Notes, DateTime CreatedAtUtc, DateTime? VoidedAtUtc, string? VoidReason, DateTime? RefundedAtUtc, string? RefundReason, byte[]? RowVersion);
public sealed record PaymentAllocationDto(Guid HotelPaymentAllocationId, Guid HotelPaymentId, Guid ChargeFolioEntryId, string ChargeDescription, decimal Amount, DateTime CreatedAtUtc);
public sealed record PaymentHistoryDto(DateTime OccurredAtUtc, string Action, string? Detail);
public sealed record OutstandingBalanceDto(decimal TotalCharges, decimal TotalPayments, decimal TotalDepositApplied, decimal Outstanding);
public sealed record DepositSummaryDto(decimal TotalDeposits, decimal AppliedDeposits, decimal RemainingDeposits);
public sealed record SettlementSummaryDto(Guid FolioId, string FolioNumber, string GuestName, string RoomNumber, decimal TotalCharges, decimal TotalPayments, decimal TotalDeposits, decimal Outstanding, bool IsBalanced);

// === Commands ===
public sealed record ReceivePaymentCommand(Guid HotelFolioId, byte[] FolioExpectedRowVersion, HotelPaymentMethod Method, decimal Amount, PaymentCurrency Currency = PaymentCurrency.VND, decimal ExchangeRate = 1, string? ExternalReference = null, string? Notes = null) : ICommand<HotelPaymentDto>;
public sealed record AuthorizePaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion, string AuthorizationCode) : ICommand<HotelPaymentDto>;
public sealed record CapturePaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion) : ICommand<HotelPaymentDto>;
public sealed record CompletePaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion) : ICommand<HotelPaymentDto>;
public sealed record SplitTenderCommand(Guid HotelFolioId, byte[] FolioExpectedRowVersion, IReadOnlyList<SplitTenderLine> Lines) : ICommand<IReadOnlyList<HotelPaymentDto>>;
public sealed record AllocatePaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion, Guid ChargeFolioEntryId, string ChargeDescription, decimal Amount) : ICommand<PaymentAllocationDto>;
public sealed record RefundPaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion, decimal? Amount, string Reason) : ICommand<HotelPaymentDto>;
public sealed record VoidPaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion, string Reason) : ICommand<HotelPaymentDto>;
public sealed record CancelPaymentCommand(Guid HotelPaymentId, byte[] ExpectedRowVersion, string Reason) : ICommand<HotelPaymentDto>;

// === Queries ===
public sealed record GetPaymentQuery(Guid HotelPaymentId) : IQuery<HotelPaymentDto>;
public sealed record GetPaymentsForFolioQuery(Guid HotelFolioId) : IQuery<IReadOnlyList<HotelPaymentDto>>;
public sealed record GetAllocationsForPaymentQuery(Guid HotelPaymentId) : IQuery<IReadOnlyList<PaymentAllocationDto>>;
public sealed record GetPaymentHistoryQuery(Guid HotelPaymentId) : IQuery<IReadOnlyList<PaymentHistoryDto>>;
public sealed record GetOutstandingBalanceQuery(Guid HotelFolioId) : IQuery<OutstandingBalanceDto>;
public sealed record GetDepositSummaryQuery(Guid HotelFolioId) : IQuery<DepositSummaryDto>;
public sealed record GetSettlementSummaryQuery(Guid PropertyId, DateOnly? DateFrom, DateOnly? DateTo) : IQuery<IReadOnlyList<SettlementSummaryDto>>;

// === Repository ===
public interface IHotelPaymentRepository
{
    Task<HotelPaymentDto?> GetDtoAsync(Guid propertyId, Guid paymentId, CancellationToken ct);
    Task<IReadOnlyList<HotelPaymentDto>> GetForFolioAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<IReadOnlyList<PaymentAllocationDto>> GetAllocationsAsync(Guid propertyId, Guid paymentId, CancellationToken ct);
    Task<IReadOnlyList<PaymentHistoryDto>> GetHistoryAsync(Guid propertyId, Guid paymentId, CancellationToken ct);
    Task<OutstandingBalanceDto> GetOutstandingAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<DepositSummaryDto> GetDepositSummaryAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<IReadOnlyList<SettlementSummaryDto>> GetSettlementSummaryAsync(Guid propertyId, DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<HotelPayment?> GetAsync(Guid propertyId, Guid paymentId, CancellationToken ct);
    Task AddAsync(HotelPayment payment, CancellationToken ct);
    Task AddAllocationAsync(HotelPaymentAllocation allocation, CancellationToken ct);
    void ApplyExpectedRowVersion(HotelPayment payment, byte[] expectedRowVersion);
}

public interface IHotelPaymentNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct = default); }
