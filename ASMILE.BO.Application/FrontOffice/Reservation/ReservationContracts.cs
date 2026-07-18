using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Application.FrontOffice.Reservation;
public sealed record ReservationDto(Guid ReservationId, string ReservationNumber, Guid PropertyId, string GuestName, DateOnly ArrivalDate, DateOnly DepartureDate, int AdultCount, int ChildCount, Guid? RoomTypeId, Guid? AssignedRoomId, ReservationStatus Status, string Source, DateTime UpdatedAtUtc, byte[]? RowVersion);
public sealed record RoomConflictResult(bool HasConflict, Guid? ConflictingReservationId);
public interface IReservationNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default); }
public interface IRoomReservationConflictChecker { Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken cancellationToken = default); }
public interface IReservationRepository : IRoomReservationConflictChecker { Task AddAsync(ReservationAggregate reservation, ReservationHistory history, CancellationToken cancellationToken = default); Task<ReservationAggregate?> GetAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default); Task AppendHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default); void ApplyExpectedRowVersion(ReservationAggregate reservation, byte[] expectedRowVersion); Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default); Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken cancellationToken = default); Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, string? keyword, ReservationStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default); }
public sealed record CreateReservationCommand(Guid PropertyId, Guid? GuestId, string GuestName, DateOnly ArrivalDate, DateOnly DepartureDate, int AdultCount, int ChildCount, Guid? RoomTypeId, string Source) : ICommand<ReservationDto>;
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
public sealed record SearchReservationsQuery(Guid PropertyId, string? Keyword, ReservationStatus? Status, DateOnly? DateFrom, DateOnly? DateTo, PageRequest Page, string? Sort) : IQuery<PageResult<ReservationDto>>;
public sealed record CheckRoomReservationConflictQuery(Guid PropertyId, Guid RoomId, DateOnly ArrivalDate, DateOnly DepartureDate, Guid? ExcludedReservationId) : IQuery<RoomConflictResult>;
