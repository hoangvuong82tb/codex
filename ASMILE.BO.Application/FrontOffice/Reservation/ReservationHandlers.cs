using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Shared.Foundation;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Application.FrontOffice.Reservation;

public sealed class ReservationApplicationService(IReservationRepository repository, IReservationNumberGenerator numberGenerator, IHotelReservationReferenceReader hotelReferences, IFrontOfficePersistenceSession persistence, IFrontOfficeExecutionContextAccessor contextAccessor, IAuthorizationService authorization, IClock clock)
{
    public async Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct)
    {
        if (!Authorized(ReservationPermissions.Create, out var denial)) return Result<ReservationDto>.Failure(denial!);
        var context = Context(command.PropertyId); if (context is null) return PropertyFailure();
        var type = await ValidateRoomType(command.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return Result<ReservationDto>.Failure(type.Error!);
        try
        {
            var number = await numberGenerator.GenerateAsync(context.Property.PropertyId.Value, context.BusinessDate.BusinessDate, ct);
            var reservation = ReservationAggregate.Create(number, context.Property.PropertyId, command.GuestId, command.GuestName, StayPeriod.Create(command.ArrivalDate, command.DepartureDate), GuestCount.Create(command.AdultCount, command.ChildCount), command.RoomTypeId, command.Source, clock.UtcNow, context.Actor.ActorId);
            await repository.AddAsync(reservation, ReservationHistory.Record(reservation.ReservationId, "Created", null, reservation.Status, clock.UtcNow, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value), ct);
            return await Commit(reservation, "Created", null, ct);
        }
        catch (ArgumentException ex) { return Result<ReservationDto>.Failure(Error.Validation(ex.Message, "Reservation input is invalid.")); }
    }

    public Task<Result<ReservationDto>> UpdateDetailsAsync(UpdateReservationDetailsCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "DetailsUpdated", async (r, context) =>
    {
        var type = await ValidateRoomType(c.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return type.Error;
        if (r.AssignedRoomId is not null && c.RoomTypeId is not null && !await hotelReferences.HotelRoomMatchesRoomTypeAsync(context.Property.PropertyId.Value, r.AssignedRoomId.Value, c.RoomTypeId.Value, ct)) return Error.Business("RESERVATION_ASSIGNED_ROOM_TYPE_MISMATCH", "Assigned room does not match room type.");
        r.Update(c.GuestName, c.Phone, c.Email, c.RoomTypeId, c.RatePlanId, c.SpecialRequests, c.InternalNotes, clock.UtcNow, context.Actor.ActorId); return null;
    }, ct);
    public Task<Result<ReservationDto>> ChangeStayAsync(ChangeReservationStayCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "StayChanged", (r, context) => { r.ChangeStay(StayPeriod.Create(c.ArrivalDate, c.DepartureDate), clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> ChangeGuestsAsync(ChangeReservationGuestCountCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "GuestCountChanged", (r, context) => { r.ChangeGuests(GuestCount.Create(c.Adults, c.Children), clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> UnassignAsync(UnassignReservationRoomCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.AssignRoom, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "RoomUnassigned", (r, context) => { r.UnassignRoom(clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> TentativeAsync(MarkReservationTentativeCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "MarkedTentative", (r, context) => { r.MarkTentative(clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> ConfirmAsync(ConfirmReservationCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "Confirmed", async (r, context) => { var type = await ValidateRoomType(r.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return type.Error; r.Confirm(clock.UtcNow, context.Actor.ActorId); return null; }, ct);
    public Task<Result<ReservationDto>> CancelAsync(CancelReservationCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Cancel, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "Cancelled", (r, context) => { r.Cancel(c.Reason, clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> NoShowAsync(MarkReservationNoShowCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "MarkedNoShow", (r, context) => { r.MarkNoShow(clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> AssignAsync(AssignReservationRoomCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.AssignRoom, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "RoomAssigned", async (r, context) =>
    {
        var room = await hotelReferences.GetRoomReferenceAsync(context.Property.PropertyId.Value, c.RoomId, ct);
        if (room is null) return Error.Business("RESERVATION_ROOM_NOT_FOUND", "Hotel room was not found.");
        if (!room.IsAssignable) return Error.Business("RESERVATION_ROOM_NOT_ASSIGNABLE", "Hotel room is not assignable.");
        if (r.RoomTypeId is null || room.HotelRoomTypeId != r.RoomTypeId.Value) return Error.Business("RESERVATION_ROOM_TYPE_MISMATCH", "Hotel room does not match reservation room type.");
        var conflict = await repository.CheckAsync(context.Property.PropertyId.Value, c.RoomId, r.ArrivalDate, r.DepartureDate, r.ReservationId, ct);
        if (conflict.HasConflict) return Error.Business("RESERVATION_ROOM_CONFLICT", "The room has an overlapping reservation.");
        r.AssignRoom(c.RoomId, clock.UtcNow, context.Actor.ActorId); return null;
    }, ct);

    private async Task<Result<ReservationDto>> MutateAsync(string permission, Guid clientPropertyId, Guid reservationId, byte[]? token, string action, Func<ReservationAggregate, FrontOfficeExecutionContext, Task<Error?>> mutation, CancellationToken ct)
    {
        if (!Authorized(permission, out var denial)) return Result<ReservationDto>.Failure(denial!);
        var context = Context(clientPropertyId); if (context is null) return PropertyFailure();
        if (token is null || token.Length == 0) return Result<ReservationDto>.Failure(Error.Validation("RESERVATION_ROWVERSION_REQUIRED", "A row version is required."));
        if (token.Length != 8) return Result<ReservationDto>.Failure(Error.Validation("RESERVATION_ROWVERSION_INVALID", "Row version is invalid."));
        var reservation = await repository.GetAsync(reservationId, context.Property.PropertyId.Value, ct); if (reservation is null) return Result<ReservationDto>.Failure(Error.Business("RESERVATION_NOT_FOUND", "Reservation was not found."));
        repository.ApplyExpectedRowVersion(reservation, token);
        var before = reservation.Status;
        try
        {
            var validation = await mutation(reservation, context); if (validation is not null) return Result<ReservationDto>.Failure(validation);
            await repository.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, action, before, reservation.Status, clock.UtcNow, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value), ct);
            return await Commit(reservation, action, before, ct);
        }
        catch (ArgumentException ex) { return Result<ReservationDto>.Failure(Error.Validation(ex.Message, "Reservation input is invalid.")); }
        catch (InvalidOperationException ex) { return Result<ReservationDto>.Failure(Error.Business(ex.Message, "Reservation state does not allow this action.")); }
    }
    private async Task<Result<bool>> ValidateRoomType(Guid? roomTypeId, Guid propertyId, CancellationToken ct)
    {
        if (roomTypeId is null) return Result<bool>.Success(true);
        var type = await hotelReferences.GetRoomTypeReferenceAsync(propertyId, roomTypeId.Value, ct);
        if (type is null) return Result<bool>.Failure(Error.Business("RESERVATION_ROOM_TYPE_NOT_FOUND", "Hotel room type was not found."));
        if (!type.IsActive) return Result<bool>.Failure(Error.Business("RESERVATION_ROOM_TYPE_INACTIVE", "Hotel room type is inactive."));
        return Result<bool>.Success(true);
    }
    private async Task<Result<ReservationDto>> Commit(ReservationAggregate reservation, string action, ReservationStatus? fromStatus, CancellationToken ct)
    {
        foreach (var e in reservation.DomainEvents) persistence.StageDomainEvent(e);
        var context = contextAccessor.Current!;
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(ReservationAggregate), reservation.ReservationId.ToString(), "Succeeded", Metadata: new Dictionary<string, string> { ["PropertyId"] = reservation.PropertyId.ToString("D"), ["ReservationNumber"] = reservation.ReservationNumber, ["FromStatus"] = fromStatus?.ToString() ?? string.Empty, ["ToStatus"] = reservation.Status.ToString(), ["ActorId"] = context.Actor.ActorId?.Value.ToString("D") ?? string.Empty, ["CorrelationId"] = context.Correlation.CorrelationId.Value.ToString("D") }));
        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure) return Result<ReservationDto>.Failure(saved.Error!.Category == ErrorCategory.Concurrency ? Error.Concurrency("RESERVATION_CONCURRENCY_CONFLICT", "Reservation changed before the operation could be saved.") : saved.Error!);
        reservation.ClearDomainEvents();
        return (await repository.GetDtoAsync(reservation.ReservationId, reservation.PropertyId, ct)) is { } dto ? Result<ReservationDto>.Success(dto) : Result<ReservationDto>.Failure(Error.System("RESERVATION_READ_AFTER_WRITE_FAILED", "Reservation could not be read."));
    }
    private FrontOfficeExecutionContext? Context(Guid clientPropertyId) { var c = contextAccessor.Current; return c is not null && c.Property.PropertyId.Value == clientPropertyId ? c : null; }
    private bool Authorized(string permission, out Error? denial) { if (authorization.HasPermission(permission)) { denial = null; return true; } denial = Error.Authorization("RESERVATION_PERMISSION_DENIED", "The current user does not have the required reservation permission."); return false; }
    private static Result<ReservationDto> PropertyFailure() => Result<ReservationDto>.Failure(Error.Authorization("RESERVATION_PROPERTY_MISMATCH", "Property scope is invalid."));
}
