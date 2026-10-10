using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using HotelFolioAggregate = ASMILE.BO.Core.FrontOffice.Folio.HotelFolio;

namespace ASMILE.BO.Application.FrontOffice.Folio;

// === DTOs ===
public sealed record HotelFolioDto(Guid HotelFolioId, string FolioNumber, Guid HotelStayId, Guid PropertyId, HotelFolioStatus Status, decimal RunningBalance, decimal TotalCharges, decimal TotalPayments, decimal OutstandingBalance, DateTime CreatedAtUtc, DateTime? ClosedAtUtc, DateTime? SettledAtUtc, byte[]? RowVersion);

public sealed record HotelFolioEntryDto(Guid HotelFolioEntryId, int SequenceNumber, HotelFolioEntryType EntryType, decimal Amount, string Description, string? ReferenceType, Guid? ReferenceId, DateOnly? BusinessDate, Guid? ReversesEntryId, Guid? ReversedByEntryId, DateTime CreatedAtUtc, Guid? CreatedBy, Guid HotelFolioId = default);

public sealed record FolioLedgerDto(
    Guid HotelFolioId,
    string FolioNumber,
    Guid HotelStayId,
    string StayNumber,
    string GuestName,
    string RoomNumber,
    HotelFolioStatus Status,
    decimal RunningBalance,
    IReadOnlyList<HotelFolioEntryDto> Entries,
    byte[]? RowVersion = null,
    Guid? GuestProfileId = null,
    string? ReservationNumber = null,
    DateOnly? ArrivalDate = null,
    DateOnly? DepartureDate = null,
    string? RoomTypeCode = null,
    string? Source = null,
    string? CompanyName = null,
    string? TravelAgentName = null,
    DateTime? OpenedAtUtc = null);

public sealed record FolioSearchFilter(Guid PropertyId, Guid? StayId, Guid? GuestId, HotelFolioStatus? Status, HotelFolioEntryType? PostingType, DateOnly? DateFrom, DateOnly? DateTo, string? Keyword);

// === Commands ===
public sealed record CreateFolioCommand(Guid HotelStayId) : ICommand<HotelFolioDto>;
public sealed record AddFolioRoomChargeCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description, DateOnly BusinessDate, Guid? RatePlanId = null) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioRestaurantChargeCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description, Guid? ReferenceId = null) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioTaxCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioServiceChargeCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioDiscountCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioPaymentCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description, string? PaymentMethod = null, string? ExternalReference = null) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioDepositCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioAdjustmentCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioCorrectionCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description, Guid? OriginalEntryId = null) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioManualChargeCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioManualCreditCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioRefundCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record AddFolioTransferCommand(Guid FromFolioId, byte[] ExpectedRowVersion, Guid ToFolioId, decimal Amount, string Description) : ICommand<HotelFolioEntryDto>;
public sealed record FolioSplitAllocation(Guid SourceEntryId, decimal Amount);
public sealed record SplitFolioCommand(Guid SourceFolioId, byte[] ExpectedSourceRowVersion, Guid TargetFolioId, byte[] ExpectedTargetRowVersion, decimal Amount, string Description, IReadOnlyList<FolioSplitAllocation>? Allocations = null, string? Rule = null) : ICommand<FolioTransferResult>;
public sealed record MergeFolioSource(Guid FolioId, byte[] ExpectedRowVersion);
public sealed record FolioMergeRules(string Transaction, string Payer, string Deposit, string Payment, string Refund, string Currency, string TaxPackage, string Document, string SourcePost);
public sealed record MergeFoliosCommand(Guid TargetFolioId, byte[] ExpectedTargetRowVersion, IReadOnlyList<MergeFolioSource> Sources, string Description, FolioMergeRules? Rules = null) : ICommand<FolioMergeResult>;
public sealed record ReverseFolioEntryCommand(Guid HotelFolioId, byte[] ExpectedRowVersion, Guid EntryId, string Reason) : ICommand<HotelFolioEntryDto>;
public sealed record SuspendFolioCommand(Guid HotelFolioId, byte[] ExpectedRowVersion) : ICommand<HotelFolioDto>;
public sealed record ResumeFolioCommand(Guid HotelFolioId, byte[] ExpectedRowVersion) : ICommand<HotelFolioDto>;
public sealed record CloseFolioCommand(Guid HotelFolioId, byte[] ExpectedRowVersion) : ICommand<HotelFolioDto>;
public sealed record SettleFolioCommand(Guid HotelFolioId, byte[] ExpectedRowVersion) : ICommand<HotelFolioDto>;
public sealed record ReopenFolioCommand(Guid HotelFolioId, byte[] ExpectedRowVersion) : ICommand<HotelFolioDto>;

// === Queries ===
public sealed record GetFolioQuery(Guid HotelFolioId) : IQuery<HotelFolioDto>;
public sealed record GetFolioByStayQuery(Guid HotelStayId) : IQuery<HotelFolioDto>;
public sealed record GetFolioLedgerQuery(Guid HotelFolioId) : IQuery<FolioLedgerDto>;
public sealed record SearchFoliosQuery(FolioSearchFilter Filter, PageRequest Page) : IQuery<PageResult<FolioLedgerDto>>;

public sealed record FolioTransferAllocationResult(Guid SourceEntryId, decimal Amount, HotelFolioEntryDto SourceEntry, HotelFolioEntryDto TargetEntry);
public sealed record FolioTransferResult(Guid SourceFolioId, Guid TargetFolioId, decimal Amount, HotelFolioEntryDto SourceEntry, HotelFolioEntryDto TargetEntry, IReadOnlyList<FolioTransferAllocationResult>? Allocations = null);
public sealed record FolioMergeResult(Guid TargetFolioId, decimal TotalTransferred, IReadOnlyList<FolioTransferResult> Transfers);

// === Repository ===
public interface IHotelFolioNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default); }

public interface IHotelFolioRepository
{
    Task<HotelFolioDto?> GetDtoAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<HotelFolioDto?> GetByStayAsync(Guid propertyId, Guid stayId, CancellationToken ct);
    Task<FolioLedgerDto?> GetLedgerAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<PageResult<FolioLedgerDto>> SearchAsync(FolioSearchFilter filter, PageRequest page, CancellationToken ct);
    Task AddAsync(HotelFolioAggregate folio, CancellationToken ct);
    Task<HotelFolioAggregate?> GetAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task AddEntryAsync(HotelFolioEntry entry, CancellationToken ct);
    Task UpdateEntryReversalAsync(HotelFolioEntry original, HotelFolioEntry reversal, CancellationToken ct);
    void ApplyExpectedRowVersion(HotelFolioAggregate folio, byte[] expectedRowVersion);
}
