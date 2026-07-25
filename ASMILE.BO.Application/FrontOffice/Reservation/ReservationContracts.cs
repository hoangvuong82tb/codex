using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Application.FrontOffice.Reservation;
public sealed record ReservationDto(Guid ReservationId, string ReservationNumber, Guid PropertyId, string GuestName, string? GuestPhone, string? GuestEmail, DateOnly ArrivalDate, DateOnly DepartureDate, int AdultCount, int ChildCount, Guid? RoomTypeId, Guid? AssignedRoomId, Guid? RatePlanId, ReservationStatus Status, string Source, string? SpecialRequests, string? InternalNotes, string? CancellationReason, bool IsVip, string? MarketSegment, string? CompanyName, string? TravelAgentName, DateTime? GuaranteedAtUtc, GuaranteeType GuaranteeType, DateTime? GuaranteeExpiryDate, decimal DepositAmount, DateOnly? DepositDueDate, DepositStatus DepositStatus, DateTime? CheckedInAtUtc, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, byte[]? RowVersion)
{
    public int Nights => Math.Max(0, DepartureDate.DayNumber - ArrivalDate.DayNumber);
}
public sealed record RoomConflictResult(bool HasConflict, Guid? ConflictingReservationId);
public interface IReservationNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default); }
public interface IRoomReservationConflictChecker { Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken cancellationToken = default); }
public interface IReservationRepository : IRoomReservationConflictChecker { Task AddAsync(ReservationAggregate reservation, ReservationHistory history, CancellationToken cancellationToken = default); Task<ReservationAggregate?> GetAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default); Task AppendHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default); void ApplyExpectedRowVersion(ReservationAggregate reservation, byte[] expectedRowVersion); Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default); Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken cancellationToken = default); Task<PageResult<ReservationHistoryDto>> GetHistoryAsync(Guid reservationId, PageRequest page, CancellationToken cancellationToken = default); Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, ReservationSearchFilter filter, PageRequest page, string? sort, CancellationToken cancellationToken = default); }
public sealed record ReservationSearchFilter(string? Keyword, string? Phone, ReservationStatus? Status, DateOnly? DateFrom, DateOnly? DateTo, Guid? RoomTypeId, Guid? RoomId, string? MarketSegment, string? Source, bool? IsVip, string? CompanyName, string? TravelAgentName);
public sealed record ReservationHistoryDto(Guid HistoryId, Guid ReservationId, string Action, ReservationStatus? FromStatus, ReservationStatus ToStatus, DateTime OccurredAtUtc, Guid? ActorId, string? ReasonCode, string? SafeSummary);
public sealed record CreateReservationCommand(Guid PropertyId, Guid? GuestId, string GuestName, DateOnly ArrivalDate, DateOnly DepartureDate, int AdultCount, int ChildCount, Guid? RoomTypeId, Guid? RatePlanId, string Source, string? MarketSegment, string? CompanyName, string? TravelAgentName, bool IsVip) : ICommand<ReservationDto>;
public sealed record SearchReservationsQuery(Guid PropertyId, ReservationSearchFilter Filter, PageRequest Page, string? Sort) : IQuery<PageResult<ReservationDto>>;
public sealed record MarkReservationTentativeCommand(Guid PropertyId, Guid ReservationId, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record ConfirmReservationCommand(Guid PropertyId, Guid ReservationId, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record CancelReservationCommand(Guid PropertyId, Guid ReservationId, string Reason, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record MarkReservationNoShowCommand(Guid PropertyId, Guid ReservationId, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record AssignReservationRoomCommand(Guid PropertyId, Guid ReservationId, Guid RoomId, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record UnassignReservationRoomCommand(Guid PropertyId, Guid ReservationId, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record UpdateReservationDetailsCommand(Guid PropertyId, Guid ReservationId, string GuestName, string? Phone, string? Email, Guid? RoomTypeId, Guid? RatePlanId, string? SpecialRequests, string? InternalNotes, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record ChangeReservationStayCommand(Guid PropertyId, Guid ReservationId, DateOnly ArrivalDate, DateOnly DepartureDate, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record ChangeReservationGuestCountCommand(Guid PropertyId, Guid ReservationId, int Adults, int Children, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record GetReservationByIdQuery(Guid PropertyId, Guid ReservationId) : IQuery<ReservationDto>;
public sealed record GetReservationByNumberQuery(Guid PropertyId, string ReservationNumber) : IQuery<ReservationDto>;
public sealed record CheckRoomReservationConflictQuery(Guid PropertyId, Guid RoomId, DateOnly ArrivalDate, DateOnly DepartureDate, Guid? ExcludedReservationId) : IQuery<RoomConflictResult>;
public sealed record GuaranteeReservationCommand(Guid PropertyId, Guid ReservationId, GuaranteeType GuaranteeType, DateTime? GuaranteeExpiryDate, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public sealed record RecordReservationDepositCommand(Guid PropertyId, Guid ReservationId, decimal DepositAmount, DateOnly DepositDueDate, byte[] ExpectedRowVersion) : ICommand<ReservationDto>;
public interface IOverbookingChecker { Task<bool> IsOverbookingLimitReachedAsync(Guid propertyId, Guid roomTypeId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken ct); }
