using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Application.FrontOffice.Stay;

public sealed class HotelStayApplicationService(
    IHotelStayRepository repository,
    IHotelStayNumberGenerator numberGenerator,
    IReservationRepository reservations,
    IHotelRoomMasterDataRepository rooms,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IAuthorizationService authorization,
    IClock clock)
{
    // =============================================================================
    // PREPARE
    // =============================================================================
    public async Task<Result<HotelStayDto>> PrepareFromReservationAsync(PrepareStayFromReservationCommand command, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.Prepare, out var d1)) return Fail<HotelStayDto>(d1);
        var (ctx, err) = RequireContext(); if (err is not null) return Fail<HotelStayDto>(err);
        var pid = ctx.Property.PropertyId.Value;

        var reservation = await reservations.GetAsync(command.ReservationId, pid, ct);
        if (reservation is null) return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_NOT_FOUND", "Reservation not found."));
        if (reservation.Status != ReservationStatus.Confirmed) return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_NOT_ELIGIBLE", "Only confirmed reservations can prepare a stay."));
        if (await repository.ExistsActiveForReservationAsync(pid, reservation.ReservationId, ct)) return Fail<HotelStayDto>(Error.Business("STAY_ALREADY_EXISTS_FOR_RESERVATION", "Active stay already exists for this reservation."));

        try
        {
            ValidateGuestAndRoom(reservation);
            var room = await ValidateRoom(pid, reservation.AssignedRoomId!.Value, reservation.RoomTypeId!.Value, ct); if (room.error is not null) return Fail<HotelStayDto>(room.error);
            var now = clock.UtcNow;
            var number = await numberGenerator.GenerateAsync(pid, ctx.BusinessDate.BusinessDate, ct);
            var stay = HotelStay.Prepare(number, ctx.Property.PropertyId, reservation.ReservationId, reservation.ReservationNumber, reservation.GuestId, reservation.GuestNameSnapshot, reservation.GuestPhoneSnapshot, room.room!.HotelRoomId, room.room.RoomNumber, room.room.HotelRoomTypeId, room.room.HotelRoomTypeCode, HotelStayPeriod.Create(reservation.ArrivalDate, reservation.DepartureDate), HotelStayGuestCount.Create(reservation.AdultCount, reservation.ChildCount), reservation.Source, now, ctx.Actor.ActorId);
            await repository.AddAsync(stay, HotelStayHistory.RecordPrepared(stay, now, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            return await Commit(stay, "StayPrepared", ctx, ct);
        }
        catch (ArgumentException ex) { return Fail<HotelStayDto>(Error.Validation(ex.Message, "Stay input is invalid.")); }
    }

    // =============================================================================
    // PRE-ARRIVAL
    // =============================================================================
    public async Task<Result<HotelStayDto>> ConfirmPreArrivalAsync(ConfirmPreArrivalCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayPreArrivalConfirmed", (s, ctx) => { s.ConfirmPreArrival(clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // PRE CHECK-IN
    // =============================================================================
    public async Task<Result<HotelStayDto>> PreCheckInAsync(PreCheckInHotelStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayPreCheckInCompleted", (s, ctx) =>
        {
            s.PreCheckIn(clock.UtcNow, ctx.Actor.ActorId, c.Eta, c.IdentityVerified, c.SpecialRequestsConfirmed, c.RoomPreferencesConfirmed);
            return Task.CompletedTask;
        }, ct, $"IdentityVerified={c.IdentityVerified};Eta={c.Eta:O}");

    // =============================================================================
    // CHECK-IN
    // =============================================================================
    public async Task<Result<HotelStayDto>> CheckInAsync(CheckInHotelStayCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckIn, out var d1)) return Fail<HotelStayDto>(d1);
        var (ctx, err) = RequireContext(); if (err is not null) return Fail<HotelStayDto>(err);
        if (!ValidToken(c.ExpectedRowVersion, out var ve)) return Fail<HotelStayDto>(ve);
        var pid = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(pid, c.HotelStayId, ct);
        if (stay is null) return Fail<HotelStayDto>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsPreCheckIn()) return Fail<HotelStayDto>(Error.Business("STAY_NOT_PENDING_ARRIVAL", "Stay is not pending arrival."));
        if (stay.GuestId is null || string.IsNullOrWhiteSpace(stay.GuestNameSnapshot)) return Fail<HotelStayDto>(Error.Business("STAY_GUEST_REQUIRED", "Guest is required."));
        if (stay.RoomId == Guid.Empty) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_REQUIRED", "Room is required for check-in."));
        var room = await rooms.GetRoomDtoAsync(pid, stay.RoomId, ct);
        if (room is null || !room.IsActive || room.OperationalStatus != HotelRoomOperationalStatus.Available) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Room is not available for check-in."));
        if (room.HotelRoomTypeId != stay.RoomTypeId) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_TYPE_MISMATCH", "Room type does not match the stay."));
        if (await repository.ExistsInHouseForRoomAsync(pid, stay.RoomId, stay.HotelStayId, ct)) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_ALREADY_OCCUPIED", "Room already occupied."));
        var at = c.ActualCheckInAtUtc ?? clock.UtcNow;
        if (at > clock.UtcNow.AddMinutes(5) || DateOnly.FromDateTime(at) >= stay.ScheduledDepartureDate) return Fail<HotelStayDto>(Error.Validation("STAY_INVALID_CHECKIN_TIME", "Check-in time is invalid."));
        repository.ApplyExpectedRowVersion(stay, c.ExpectedRowVersion);
        try
        {
            stay.CheckIn(at, ctx.Actor.ActorId);
            await repository.AppendHistoryAsync(HotelStayHistory.RecordCheckedIn(stay, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            return await Commit(stay, "StayCheckedIn", ctx, ct);
        }
        catch (InvalidOperationException) { return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay state does not allow check-in.")); }
    }

    // =============================================================================
    // EXTEND STAY
    // =============================================================================
    public async Task<Result<HotelStayDto>> ExtendStayAsync(ExtendStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayExtended", (s, ctx) =>
        {
            s.ExtendStay(c.NewDepartureDate, clock.UtcNow, ctx.Actor.ActorId);
            return Task.CompletedTask;
        }, ct, $"NewDeparture={c.NewDepartureDate}");

    // =============================================================================
    // MOVE ROOM
    // =============================================================================
    public async Task<Result<HotelStayDto>> MoveRoomAsync(MoveRoomCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckIn, out var d1)) return Fail<HotelStayDto>(d1);
        var (ctx, err) = RequireContext(); if (err is not null) return Fail<HotelStayDto>(err);
        if (!ValidToken(c.ExpectedRowVersion, out var ve)) return Fail<HotelStayDto>(ve);
        var pid = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(pid, c.HotelStayId, ct);
        if (stay is null) return Fail<HotelStayDto>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying()) return Fail<HotelStayDto>(Error.Business("STAY_NOT_OCCUPYING", "Stay is not occupying a room."));
        var toRoom = await rooms.GetRoomDtoAsync(pid, c.ToRoomId, ct);
        if (toRoom is null || !toRoom.IsActive || toRoom.OperationalStatus != HotelRoomOperationalStatus.Available) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Target room is not available."));
        if (await repository.ExistsInHouseForRoomAsync(pid, c.ToRoomId, null, ct)) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_ALREADY_OCCUPIED", "Target room is already occupied."));
        var fromRoomType = await rooms.GetRoomTypeDtoAsync(pid, stay.RoomTypeId, ct);
        var toRoomType = await rooms.GetRoomTypeDtoAsync(pid, toRoom.HotelRoomTypeId, ct);
        repository.ApplyExpectedRowVersion(stay, c.ExpectedRowVersion);
        try
        {
            stay.MoveRoom(c.ToRoomId, toRoom.RoomNumber, toRoom.HotelRoomTypeId, c.Reason, clock.UtcNow, ctx.Actor.ActorId, toRoomType?.DisplayOrder ?? 0, fromRoomType?.DisplayOrder ?? 0);
            await repository.AppendHistoryAsync(HotelStayHistory.Record(stay, "StayRoomMoved", clock.UtcNow, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value, $"ToRoom={toRoom.RoomNumber};Reason={c.Reason}"), ct);
            return await Commit(stay, "StayRoomMoved", ctx, ct);
        }
        catch (InvalidOperationException) { return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay state does not allow room move.")); }
        catch (ArgumentException ex) { return Fail<HotelStayDto>(Error.Validation(ex.Message, "Room move input is invalid.")); }
    }

    // =============================================================================
    // CHECK-OUT
    // =============================================================================
    public async Task<Result<HotelStayDto>> CheckOutAsync(CheckOutHotelStayCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckOut, out var d1)) return Fail<HotelStayDto>(d1);
        var (ctx, err) = RequireContext(); if (err is not null) return Fail<HotelStayDto>(err);
        if (!ValidToken(c.ExpectedRowVersion, out var ve)) return Fail<HotelStayDto>(ve);
        var pid = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(pid, c.HotelStayId, ct);
        if (stay is null) return Fail<HotelStayDto>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying()) return Fail<HotelStayDto>(Error.Business("STAY_NOT_OCCUPYING", "Stay is not occupying a room."));
        var at = c.ActualCheckOutAtUtc ?? clock.UtcNow;
        if (at < stay.ActualCheckInAtUtc) return Fail<HotelStayDto>(Error.Validation("STAY_INVALID_CHECKOUT", "Check-out cannot be before check-in."));
        repository.ApplyExpectedRowVersion(stay, c.ExpectedRowVersion);
        try
        {
            var departureType = c.ActualCheckOutAtUtc is not null && DateOnly.FromDateTime(c.ActualCheckOutAtUtc.Value) < stay.ScheduledDepartureDate ? DepartureType.Early : DepartureType.Normal;
            stay.MarkCheckedOut(at, ctx.Actor.ActorId, departureType, c.DepartureReason, c.DepartureNotes, c.HasPendingCharges, c.HasOutstandingBalance);
            await repository.AppendHistoryAsync(HotelStayHistory.RecordCheckedOut(stay, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            return await Commit(stay, "StayCheckedOut", ctx, ct);
        }
        catch (InvalidOperationException) { return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay state does not allow check-out.")); }
    }

    // =============================================================================
    // COMPLETE
    // =============================================================================
    public async Task<Result<HotelStayDto>> CompleteAsync(CompleteStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckOut, "StayCompleted", (s, ctx) => { s.Complete(clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // REOPEN
    // =============================================================================
    public async Task<Result<HotelStayDto>> ReopenAsync(ReopenStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayReopened", (s, ctx) => { s.Reopen(clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // CANCEL
    // =============================================================================
    public async Task<Result<HotelStayDto>> CancelAsync(CancelStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayCancelled", (s, ctx) => { s.CancelPreparation(clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // IN-HOUSE MANAGEMENT
    // =============================================================================
    public async Task<Result<HotelStayDto>> ToggleDoNotDisturbAsync(ToggleDoNotDisturbCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, c.Enabled ? "StayDndEnabled" : "StayDndDisabled", (s, ctx) => { s.ToggleDoNotDisturb(c.Enabled, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    public async Task<Result<HotelStayDto>> RequestWakeUpCallAsync(RequestWakeUpCallCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayWakeUpCallRequested", (s, ctx) => { s.RequestWakeUpCall(c.RequestedAtUtc, c.Notes, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"WakeUpAt={c.RequestedAtUtc:O}");

    public async Task<Result<HotelStayDto>> LogComplaintAsync(LogComplaintCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayComplaintLogged", (s, ctx) => { s.LogComplaint(c.Category, c.Description, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"Category={c.Category}");

    public async Task<Result<HotelStayDto>> AddTagAsync(AddStayTagCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayTagAdded", (s, ctx) => { s.AddTag(c.Tag, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"Tag={c.Tag}");

    public async Task<Result<HotelStayDto>> RemoveTagAsync(RemoveStayTagCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayTagRemoved", (s, ctx) => { s.RemoveTag(c.Tag, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"Tag={c.Tag}");

    public async Task<Result<HotelStayDto>> MarkVipServiceAsync(MarkVipServiceCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, c.Enabled ? "StayVipServiceStarted" : "StayVipServiceStopped", (s, ctx) => { s.MarkVipService(c.Enabled, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // TIMELINE
    // =============================================================================
    public async Task<Result<IReadOnlyList<StayTimelineEntryDto>>> GetTimelineAsync(Guid stayId, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.View, out var d1)) return Result<IReadOnlyList<StayTimelineEntryDto>>.Failure(d1!);
        var (ctx, err) = RequireContext(); if (err is not null) return Result<IReadOnlyList<StayTimelineEntryDto>>.Failure(err);
        return Result<IReadOnlyList<StayTimelineEntryDto>>.Success(await repository.GetTimelineAsync(ctx.Property.PropertyId.Value, stayId, ct));
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private bool Authorized(string permission, out Error? denial) { if (!authorization.HasPermission(permission)) { denial = Error.Authorization("STAY_PERMISSION_DENIED", "Permission denied."); return false; } denial = null; return true; }

    private static Result<T> Fail<T>(Error? error) => Result<T>.Failure(error!);

    private (FrontOfficeExecutionContext ctx, Error? err) RequireContext()
    {
        var ctx = contextAccessor.Current;
        return ctx is null ? (null!, Error.Authorization("STAY_PERMISSION_DENIED", "Front Office context is required.")) : (ctx, null);
    }

    private static bool ValidToken(byte[]? token, out Error? error)
    {
        if (token is null || token.Length == 0) { error = Error.Validation("STAY_ROWVERSION_REQUIRED", "Row version required."); return false; }
        if (token.Length != 8) { error = Error.Validation("STAY_ROWVERSION_INVALID", "Invalid row version."); return false; }
        error = null; return true;
    }

    private static void ValidateGuestAndRoom(ReservationAggregate r)
    {
        if (r.GuestId is null || string.IsNullOrWhiteSpace(r.GuestNameSnapshot)) throw new ArgumentException("STAY_GUEST_REQUIRED");
        if (r.AssignedRoomId is null) throw new ArgumentException("STAY_ROOM_REQUIRED");
        if (r.RoomTypeId is null) throw new ArgumentException("STAY_ROOM_TYPE_MISMATCH");
    }

    private async Task<(HotelRoomDto? room, Error? error)> ValidateRoom(Guid pid, Guid roomId, Guid roomTypeId, CancellationToken ct)
    {
        var room = await rooms.GetRoomDtoAsync(pid, roomId, ct);
        if (room is null) return (null, Error.Business("STAY_ROOM_NOT_FOUND", "Room not found."));
        if (!room.IsActive || room.OperationalStatus is not HotelRoomOperationalStatus.Available) return (null, Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Room not assignable."));
        if (room.HotelRoomTypeId != roomTypeId) return (null, Error.Business("STAY_ROOM_TYPE_MISMATCH", "Room type mismatch."));
        return (room, null);
    }

    private async Task<Result<HotelStayDto>> Commit(HotelStay stay, string action, FrontOfficeExecutionContext ctx, CancellationToken ct)
    {
        foreach (var de in stay.DomainEvents) persistence.StageDomainEvent(de);
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(HotelStay), stay.HotelStayId.ToString("D"), "Succeeded", Metadata: new Dictionary<string, string> { ["PropertyId"] = stay.PropertyId.ToString("D"), ["StayNumber"] = stay.StayNumber, ["Status"] = stay.Status.ToString(), ["ActorId"] = ctx.Actor.ActorId?.Value.ToString("D") ?? "", ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D") }));
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) return Fail<HotelStayDto>(commit.Error!.Category == ErrorCategory.Concurrency ? Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "Concurrent modification.") : commit.Error!);
        stay.ClearDomainEvents();
        return await repository.GetDtoAsync(ctx.Property.PropertyId.Value, stay.HotelStayId, ct) is { } dto ? Result<HotelStayDto>.Success(dto) : Fail<HotelStayDto>(Error.System("STAY_READ_FAILED", "Could not read stay after save."));
    }

    private async Task<Result<HotelStayDto>> MutateAsync(Guid stayId, byte[] token, string permission, string action, Func<HotelStay, FrontOfficeExecutionContext, Task> mutation, CancellationToken ct, string? metadata = null)
    {
        if (!Authorized(permission, out var d1)) return Fail<HotelStayDto>(d1);
        var (ctx, err) = RequireContext(); if (err is not null) return Fail<HotelStayDto>(err);
        if (!ValidToken(token, out var ve)) return Fail<HotelStayDto>(ve);
        var pid = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(pid, stayId, ct);
        if (stay is null) return Fail<HotelStayDto>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        repository.ApplyExpectedRowVersion(stay, token);
        try
        {
            await mutation(stay, ctx);
            await repository.AppendHistoryAsync(HotelStayHistory.Record(stay, action, clock.UtcNow, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value, metadata), ct);
            return await Commit(stay, action, ctx, ct);
        }
        catch (InvalidOperationException) { return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Invalid state transition.")); }
        catch (ArgumentException ex) { return Fail<HotelStayDto>(Error.Validation(ex.Message, "Invalid input.")); }
    }
}
