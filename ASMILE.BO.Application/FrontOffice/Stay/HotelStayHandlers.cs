using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Rate;
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
    IHotelStayOccupantRepository occupants,
    IGuestProfileRepository guests,
    IHotelFolioRepository folios,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IAuthorizationService authorization,
    IClock clock,
    IHotelStructureMasterDataRepository? hotelConfiguration = null,
    IRateRepository? rateRepository = null)
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
        if (string.IsNullOrWhiteSpace(stay.GuestNameSnapshot)) return Fail<HotelStayDto>(Error.Business("STAY_GUEST_REQUIRED", "Guest name is required."));
        if (stay.RoomId == Guid.Empty) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_REQUIRED", "Room is required for check-in."));
        var room = await rooms.GetRoomDtoAsync(pid, stay.RoomId, ct);
        if (room is null || !room.IsActive || room.OperationalStatus != HotelRoomOperationalStatus.Available) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Room is not available for check-in."));
        if (room.HotelRoomTypeId != stay.RoomTypeId) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_TYPE_MISMATCH", "Room type does not match the stay."));
        if (await repository.ExistsInHouseForRoomAsync(pid, stay.RoomId, stay.HotelStayId, ct)) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_ALREADY_OCCUPIED", "Room already occupied."));
        var at = c.ActualCheckInAtUtc ?? clock.UtcNow;
        if (at > clock.UtcNow.AddMinutes(5) || DateOnly.FromDateTime(at) >= stay.ScheduledDepartureDate) return Fail<HotelStayDto>(Error.Validation("STAY_INVALID_CHECKIN_TIME", "Check-in time is invalid."));
        var checkInRule = hotelConfiguration is null ? null : await hotelConfiguration.GetActiveCheckInOutRuleAsync(pid, at, ct);
        var propertyProfile = hotelConfiguration is null ? null : await hotelConfiguration.GetPropertyProfileAsync(pid, ct);
        if (checkInRule is not null)
        {
            if (checkInRule.RequireGuestDocument && !stay.IsIdentityVerified)
                return Fail<HotelStayDto>(Error.Business("STAY_GUEST_DOCUMENT_REQUIRED", "Guest document verification is required by the active check-in rule."));
            if (checkInRule.RequirePaymentAuthorization)
            {
                var authorizationFolio = await folios.GetByStayAsync(pid, stay.HotelStayId, ct);
                if (authorizationFolio is null || authorizationFolio.TotalPayments <= 0)
                    return Fail<HotelStayDto>(Error.Business("STAY_PAYMENT_AUTHORIZATION_REQUIRED", "Payment or deposit authorization is required by the active check-in rule."));
            }
            if (propertyProfile is not null && IsBeforeConfiguredCheckIn(stay.ScheduledArrivalDate, at, propertyProfile, checkInRule.EarlyCheckInGraceMinutes)
                && !HasRoleOrSystemActor(ctx, checkInRule.EarlyCheckInApprovalRole))
                return Fail<HotelStayDto>(Error.Business("STAY_EARLY_CHECKIN_APPROVAL_REQUIRED", "Early check-in requires the configured approval role."));
        }
        repository.ApplyExpectedRowVersion(stay, c.ExpectedRowVersion);
        try
        {
            stay.CheckIn(at, ctx.Actor.ActorId);
            await repository.AppendHistoryAsync(HotelStayHistory.RecordCheckedIn(stay, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            ReservationAggregate? reservation = null;
            if (stay.ReservationId is { } reservationId)
            {
                reservation = await reservations.GetAsync(reservationId, pid, ct);
                if (reservation is null) return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_NOT_FOUND", "Reservation linked to this stay was not found."));
                var reservationError = await MarkReservationCheckedInAsync(reservation, stay.RoomId, ctx, at, ct);
                if (reservationError is not null) return Fail<HotelStayDto>(reservationError);
            }
            return await Commit(stay, "StayCheckedIn", ctx, ct, reservation);
        }
        catch (InvalidOperationException) { return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay state does not allow check-in.")); }
    }

    public async Task<Result<HotelStayDto>> CheckInReservationAsync(CheckInReservationCommand command, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckIn, out var denial)) return Fail<HotelStayDto>(denial);
        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Fail<HotelStayDto>(contextError);
        if (!ValidToken(command.ExpectedRowVersion, out var tokenError)) return Fail<HotelStayDto>(tokenError);

        var propertyId = ctx.Property.PropertyId.Value;
        var reservation = await reservations.GetAsync(command.ReservationId, propertyId, ct);
        if (reservation is null) return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_NOT_FOUND", "Reservation not found."));

        var activeStay = await repository.GetActiveForReservationAsync(propertyId, reservation.ReservationId, ct);
        if (reservation.Status == ReservationStatus.CheckedIn)
        {
            if (activeStay is null) return Fail<HotelStayDto>(Error.Business("STAY_CHECKIN_ALREADY_COMPLETED", "Reservation is already checked in, but its active stay could not be found."));
            return await repository.GetDtoAsync(propertyId, activeStay.HotelStayId, ct) is { } checkedInDto
                ? Result<HotelStayDto>.Success(checkedInDto)
                : Fail<HotelStayDto>(Error.System("STAY_READ_FAILED", "Could not read the checked-in stay."));
        }

        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Guaranteed))
            return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_NOT_ELIGIBLE", "Only confirmed or guaranteed reservations can check in."));
        if (string.IsNullOrWhiteSpace(reservation.GuestNameSnapshot))
            return Fail<HotelStayDto>(Error.Business("STAY_GUEST_REQUIRED", "Reservation must have a guest name before check-in."));
        if (reservation.RoomTypeId is not { } roomTypeId)
            return Fail<HotelStayDto>(Error.Business("STAY_ROOM_TYPE_REQUIRED", "Reservation must have a room type before check-in."));

        reservations.ApplyExpectedRowVersion(reservation, command.ExpectedRowVersion);
        var at = command.ActualCheckInAtUtc ?? clock.UtcNow;
        var now = clock.UtcNow;
        HotelRoomDto? room;
        HotelStay stay = activeStay!;
        var isNewStay = activeStay is null;

        if (activeStay is { } existing && existing.Status.IsOccupying())
        {
            if (existing.RoomId == Guid.Empty) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_REQUIRED", "The active stay has no assigned room."));
            stay = existing;
            var syncError = await MarkReservationCheckedInAsync(reservation, stay.RoomId, ctx, stay.ActualCheckInAtUtc ?? at, ct);
            if (syncError is not null) return Fail<HotelStayDto>(syncError);
            return await Commit(stay, "ReservationCheckInReconciled", ctx, ct, reservation);
        }

        if (at > clock.UtcNow.AddMinutes(5) || DateOnly.FromDateTime(at) >= reservation.DepartureDate)
            return Fail<HotelStayDto>(Error.Validation("STAY_INVALID_CHECKIN_TIME", "Check-in time is invalid or the reservation has already departed."));

        var reservationCheckInRule = hotelConfiguration is null ? null : await hotelConfiguration.GetActiveCheckInOutRuleAsync(propertyId, at, ct);
        var reservationPropertyProfile = hotelConfiguration is null ? null : await hotelConfiguration.GetPropertyProfileAsync(propertyId, ct);
        if (reservationCheckInRule is not null)
        {
            if (reservationCheckInRule.RequireGuestDocument && (activeStay is null || !activeStay.IsIdentityVerified))
                return Fail<HotelStayDto>(Error.Business("STAY_GUEST_DOCUMENT_REQUIRED", "Guest document verification is required by the active check-in rule."));
            if (reservationCheckInRule.RequirePaymentAuthorization)
            {
                var authorizationFolio = activeStay is null ? null : await folios.GetByStayAsync(propertyId, activeStay.HotelStayId, ct);
                if (authorizationFolio is null || authorizationFolio.TotalPayments <= 0)
                    return Fail<HotelStayDto>(Error.Business("STAY_PAYMENT_AUTHORIZATION_REQUIRED", "Payment or deposit authorization is required by the active check-in rule."));
            }
            if (reservationPropertyProfile is not null && IsBeforeConfiguredCheckIn(reservation.ArrivalDate, at, reservationPropertyProfile, reservationCheckInRule.EarlyCheckInGraceMinutes)
                && !HasRoleOrSystemActor(ctx, reservationCheckInRule.EarlyCheckInApprovalRole))
                return Fail<HotelStayDto>(Error.Business("STAY_EARLY_CHECKIN_APPROVAL_REQUIRED", "Early check-in requires the configured approval role."));
        }

        if (activeStay is { } pending && !pending.Status.IsPreCheckIn())
            return Fail<HotelStayDto>(Error.Business("STAY_RESERVATION_HAS_INACTIVE_STAY", "Reservation has a stay that cannot be checked in from its current state."));

        Guid roomId;
        if (activeStay is { } existingPending)
        {
            stay = existingPending;
            roomId = stay.RoomId;
            if (roomId == Guid.Empty) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_REQUIRED", "The pending stay has no assigned room."));
            if (reservation.AssignedRoomId is { } assignedRoomId && assignedRoomId != roomId)
                return Fail<HotelStayDto>(Error.Business("STAY_ROOM_MISMATCH", "Assigned reservation room does not match its pending stay."));
            room = await rooms.GetRoomDtoAsync(propertyId, roomId, ct);
        }
        else if (reservation.AssignedRoomId is { } assignedRoomId)
        {
            roomId = assignedRoomId;
            var validation = await ValidateRoom(propertyId, roomId, roomTypeId, ct);
            if (validation.error is not null) return Fail<HotelStayDto>(validation.error);
            room = validation.room;
        }
        else
        {
            var candidates = await rooms.SearchRoomsAsync(propertyId, null, roomTypeId, null, null, HotelRoomOperationalStatus.Available, true,
                new PageRequest(1, 200), "number", true, ct);
            room = null;
            foreach (var candidate in candidates.Items)
            {
                var conflict = await reservations.CheckAsync(propertyId, candidate.HotelRoomId, reservation.ArrivalDate, reservation.DepartureDate, reservation.ReservationId, ct);
                if (conflict.HasConflict || await repository.ExistsInHouseForRoomAsync(propertyId, candidate.HotelRoomId, cancellationToken: ct)) continue;
                room = candidate;
                break;
            }
            if (room is null) return Fail<HotelStayDto>(Error.Business("STAY_NO_ROOM_AVAILABLE", "No available room of the reserved type can be assigned for these dates."));
            roomId = room.HotelRoomId;
        }

        if (room is null || !room.IsActive || room.OperationalStatus != HotelRoomOperationalStatus.Available)
            return Fail<HotelStayDto>(Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Room is not available for check-in."));
        if (room.HotelRoomTypeId != roomTypeId)
            return Fail<HotelStayDto>(Error.Business("STAY_ROOM_TYPE_MISMATCH", "Room type does not match the reservation."));
        var roomConflict = await reservations.CheckAsync(propertyId, roomId, reservation.ArrivalDate, reservation.DepartureDate, reservation.ReservationId, ct);
        if (roomConflict.HasConflict) return Fail<HotelStayDto>(Error.Business("STAY_ROOM_RESERVATION_CONFLICT", "The assigned room conflicts with another reservation."));
        if (await repository.ExistsInHouseForRoomAsync(propertyId, roomId, activeStay?.HotelStayId, ct))
            return Fail<HotelStayDto>(Error.Business("STAY_ROOM_ALREADY_OCCUPIED", "Room is already occupied."));

        if (reservation.AssignedRoomId != roomId)
        {
            var previousStatus = reservation.Status;
            reservation.AssignRoom(roomId, now, ctx.Actor.ActorId);
            await reservations.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, "RoomAssignedAtCheckIn", previousStatus, previousStatus, now, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
        }

        if (isNewStay)
        {
            var number = await numberGenerator.GenerateAsync(propertyId, ctx.BusinessDate.BusinessDate, ct);
            stay = HotelStay.Prepare(number, ctx.Property.PropertyId, reservation.ReservationId, reservation.ReservationNumber,
                reservation.GuestId, reservation.GuestNameSnapshot, reservation.GuestPhoneSnapshot, room.HotelRoomId, room.RoomNumber,
                room.HotelRoomTypeId, room.HotelRoomTypeCode,
                HotelStayPeriod.Create(reservation.ArrivalDate, reservation.DepartureDate),
                HotelStayGuestCount.Create(reservation.AdultCount, reservation.ChildCount), reservation.Source, now, ctx.Actor.ActorId);
            await repository.AddAsync(stay, HotelStayHistory.RecordPrepared(stay, now, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
        }
        else if (stay.RowVersion is { Length: 8 } stayRowVersion)
        {
            repository.ApplyExpectedRowVersion(stay, stayRowVersion);
        }

        try
        {
            stay.CheckIn(at, ctx.Actor.ActorId);
            await repository.AppendHistoryAsync(HotelStayHistory.RecordCheckedIn(stay, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            var reservationError = await MarkReservationCheckedInAsync(reservation, roomId, ctx, at, ct);
            if (reservationError is not null) return Fail<HotelStayDto>(reservationError);
            return await Commit(stay, "StayCheckedIn", ctx, ct, reservation);
        }
        catch (InvalidOperationException)
        {
            return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay state does not allow check-in."));
        }
        catch (ArgumentException ex)
        {
            return Fail<HotelStayDto>(Error.Validation("STAY_CHECKIN_INVALID", ex.Message));
        }
    }

    public async Task<Result<HotelStayDto>> UpdateInformationAsync(UpdateStayInformationCommand command, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.Update, out var denial)) return Fail<HotelStayDto>(denial);
        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Fail<HotelStayDto>(contextError);
        if (!ValidToken(command.ExpectedRowVersion, out var tokenError)) return Fail<HotelStayDto>(tokenError);

        var guestName = command.GuestName?.Trim();
        if (string.IsNullOrWhiteSpace(guestName))
            return Fail<HotelStayDto>(Error.Validation("STAY_GUEST_NAME_REQUIRED", "Guest name is required."));
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return Fail<HotelStayDto>(Error.Validation("STAY_INFORMATION_REASON_REQUIRED", "Adjustment reason is required."));
        if (guestName.Length > 200 || reason.Length > 150)
            return Fail<HotelStayDto>(Error.Validation("STAY_INFORMATION_FIELD_TOO_LONG", "Guest name or adjustment reason is too long."));

        var propertyId = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(propertyId, command.HotelStayId, ct);
        if (stay is null) return Fail<HotelStayDto>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying())
            return Fail<HotelStayDto>(Error.Business("STAY_NOT_OCCUPYING", "Only a current in-house stay can be adjusted."));

        var phone = string.IsNullOrWhiteSpace(command.GuestPhone) ? null : command.GuestPhone.Trim();
        var requests = string.IsNullOrWhiteSpace(command.SpecialRequests) ? null : command.SpecialRequests.Trim();
        var notes = string.IsNullOrWhiteSpace(command.InternalNotes) ? null : command.InternalNotes.Trim();
        if (phone?.Length > 50 || requests?.Length > 2000 || notes?.Length > 1000)
            return Fail<HotelStayDto>(Error.Validation("STAY_INFORMATION_FIELD_TOO_LONG", "Phone, special requests or internal notes exceed the supported length."));
        var nameChanged = !string.Equals(stay.GuestNameSnapshot?.Trim(), guestName, StringComparison.Ordinal);
        var phoneChanged = !string.Equals(stay.GuestPhoneSnapshot?.Trim(), phone, StringComparison.Ordinal);
        var requestsChanged = !string.Equals(stay.SpecialRequests?.Trim(), requests, StringComparison.Ordinal);
        var notesChanged = !string.Equals(stay.InternalNotes?.Trim(), notes, StringComparison.Ordinal);
        if (!nameChanged && !phoneChanged && !requestsChanged && !notesChanged)
            return Fail<HotelStayDto>(Error.Validation("STAY_INFORMATION_UNCHANGED", "No stay information has changed."));

        repository.ApplyExpectedRowVersion(stay, command.ExpectedRowVersion);
        var now = clock.UtcNow;
        try
        {
            if (nameChanged || phoneChanged) stay.UpdateGuestInfo(guestName, phone, now, ctx.Actor.ActorId);
            if (requestsChanged) stay.UpdateSpecialRequests(requests, now, ctx.Actor.ActorId);
            if (notesChanged) stay.UpdateNotes(notes, now, ctx.Actor.ActorId);

            var fields = string.Join(",", new[]
            {
                nameChanged ? "GuestName" : null,
                phoneChanged ? "GuestPhone" : null,
                requestsChanged ? "SpecialRequests" : null,
                notesChanged ? "InternalNotes" : null
            }.Where(x => x is not null));
            var metadata = $"Reason={reason};Fields={fields}";
            await repository.AppendHistoryAsync(HotelStayHistory.Record(
                stay, "StayInformationUpdated", now, ctx.Actor.ActorId?.Value,
                ctx.Correlation.CorrelationId.Value, metadata), ct);
            return await Commit(stay, "StayInformationUpdated", ctx, ct, metadata: $"Fields={fields}");
        }
        catch (InvalidOperationException)
        {
            return Fail<HotelStayDto>(Error.Business("STAY_INVALID_STATE", "Stay information cannot be changed in its current state."));
        }
        catch (ArgumentException ex)
        {
            return Fail<HotelStayDto>(Error.Validation("STAY_INFORMATION_INVALID", ex.Message));
        }
    }

    // =============================================================================
    // EXTEND STAY
    // =============================================================================
    public async Task<Result<HotelStayDto>> ExtendStayAsync(ExtendStayCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.Update, "StayExtended", (s, ctx) =>
        {
            s.ExtendStay(c.NewDepartureDate, clock.UtcNow, ctx.Actor.ActorId);
            return Task.CompletedTask;
        }, ct, $"NewDeparture={c.NewDepartureDate}");

    // =============================================================================
    // MOVE ROOM
    // =============================================================================
    public async Task<Result<HotelStayDto>> MoveRoomAsync(MoveRoomCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.Update, out var d1)) return Fail<HotelStayDto>(d1);
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
        if (fromRoomType is null || toRoomType is null)
            return Fail<HotelStayDto>(Error.Business("STAY_ROOM_TYPE_NOT_FOUND", "Room type configuration is missing for the room move."));
        if (hotelConfiguration is not null)
        {
            var rule = await hotelConfiguration.GetActiveRoomMoveUpgradeRuleAsync(pid, clock.UtcNow, ct);
            var policyError = RoomMovePolicyEvaluator.Validate(rule, fromRoomType.DisplayOrder, toRoomType.DisplayOrder, toRoom.HousekeepingStatus, ctx);
            if (policyError is not null) return Fail<HotelStayDto>(policyError);

            if (rule is not null && !string.IsNullOrWhiteSpace(rule.PriceDifferenceMode))
            {
                var pricing = await PrepareRoomMovePriceDifferenceAsync(
                    rule.PriceDifferenceMode,
                    stay,
                    fromRoomType.HotelRoomTypeId,
                    toRoomType.HotelRoomTypeId,
                    toRoom.RoomNumber,
                    pid,
                    ct);
                if (pricing.Error is not null) return Fail<HotelStayDto>(pricing.Error);
                if (pricing.Amount is { } difference && difference != 0)
                {
                    var folio = await folios.GetByStayAsync(pid, stay.HotelStayId, ct);
                    if (folio is null || folio.RowVersion is not { Length: 8 })
                        return Fail<HotelStayDto>(Error.Business("STAY_ROOM_MOVE_FOLIO_REQUIRED", "A Folio with a valid row version is required to post the room rate difference."));

                    var folioAggregate = await folios.GetAsync(pid, folio.HotelFolioId, ct);
                    if (folioAggregate is null)
                        return Fail<HotelStayDto>(Error.Business("STAY_ROOM_MOVE_FOLIO_REQUIRED", "The stay Folio could not be loaded for the room rate difference."));

                    folios.ApplyExpectedRowVersion(folioAggregate, folio.RowVersion);
                    var adjustment = folioAggregate.AddAdjustment(
                        difference,
                        $"Room move rate difference to {toRoom.RoomNumber} ({pricing.FromPlanCode} → {pricing.ToPlanCode})",
                        clock.UtcNow,
                        ctx.Actor.ActorId,
                        ctx.BusinessDate.BusinessDate);
                    await folios.AddEntryAsync(adjustment, ct);
                }
            }
        }
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

    private async Task<RoomMovePriceDifference> PrepareRoomMovePriceDifferenceAsync(
        string mode,
        HotelStay stay,
        Guid fromRoomTypeId,
        Guid toRoomTypeId,
        string targetRoomNumber,
        Guid propertyId,
        CancellationToken ct)
    {
        if (mode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("NoCharge", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("None", StringComparison.OrdinalIgnoreCase))
            return RoomMovePriceDifference.None;

        if (!mode.Equals("ChargeDifference", StringComparison.OrdinalIgnoreCase))
            return RoomMovePriceDifference.Failure(Error.Business("STAY_ROOM_MOVE_PRICE_MODE_UNSUPPORTED", $"Price difference mode '{mode}' is not supported by the active room move policy."));

        if (rateRepository is null)
            return RoomMovePriceDifference.Failure(Error.System("STAY_ROOM_MOVE_RATE_SERVICE_UNAVAILABLE", "The rate service is not configured for room move price differences."));

        var startDate = DateOnly.FromDateTime(clock.UtcNow);
        if (startDate < stay.ScheduledArrivalDate) startDate = stay.ScheduledArrivalDate;
        var nights = Math.Max(1, stay.ScheduledDepartureDate.DayNumber - startDate.DayNumber);
        var adults = Math.Max(stay.Adults, 1);
        var plans = await rateRepository.GetAllAsync(propertyId, new RatePlanSearchFilter(propertyId, RatePlanStatus.Active, null, null), ct);
        var fromPlan = SelectRatePlan(plans, fromRoomTypeId, startDate);
        var toPlan = SelectRatePlan(plans, toRoomTypeId, startDate);
        if (fromPlan is null || toPlan is null)
            return RoomMovePriceDifference.Failure(Error.Business("STAY_ROOM_MOVE_RATE_NOT_FOUND", $"No active rate plan covers the room move to {targetRoomNumber} on {startDate:yyyy-MM-dd}."));

        var (fromTotal, _) = fromPlan.CalculateStayPrice(startDate, adults, nights, 1, 0);
        var (toTotal, _) = toPlan.CalculateStayPrice(startDate, adults, nights, 1, 0);
        var difference = toTotal - fromTotal;
        return new RoomMovePriceDifference(difference, fromPlan.PlanCode, toPlan.PlanCode, null);
    }

    private static RatePlan? SelectRatePlan(IReadOnlyList<RatePlan> plans, Guid roomTypeId, DateOnly date) =>
        plans
            .Where(x => x.RoomTypeId == roomTypeId || x.RoomTypeId is null)
            .Where(x => DateOnly.FromDateTime(x.EffectiveFrom) <= date && DateOnly.FromDateTime(x.EffectiveTo) >= date)
            .OrderBy(x => x.RoomTypeId == roomTypeId ? 0 : 1)
            .ThenBy(x => x.Hierarchy)
            .ThenBy(x => x.PlanCode)
            .FirstOrDefault();

    private sealed record RoomMovePriceDifference(decimal? Amount, string? FromPlanCode, string? ToPlanCode, Error? Error)
    {
        public static RoomMovePriceDifference None { get; } = new(0, null, null, null);
        public static RoomMovePriceDifference Failure(Error error) => new(null, null, null, error);
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
        var folio = await folios.GetByStayAsync(pid, stay.HotelStayId, ct);
        if (folio is not null && folio.OutstandingBalance > 0)
            return Fail<HotelStayDto>(Error.Business("STAY_OUTSTANDING_BALANCE", $"Cannot check out while the folio has an outstanding balance of {folio.OutstandingBalance:N0}."));
        var at = c.ActualCheckOutAtUtc ?? clock.UtcNow;
        if (at < stay.ActualCheckInAtUtc) return Fail<HotelStayDto>(Error.Validation("STAY_INVALID_CHECKOUT", "Check-out cannot be before check-in."));
        var checkOutRule = hotelConfiguration is null ? null : await hotelConfiguration.GetActiveCheckInOutRuleAsync(pid, at, ct);
        var propertyProfile = hotelConfiguration is null ? null : await hotelConfiguration.GetPropertyProfileAsync(pid, ct);
        if (checkOutRule is not null && propertyProfile is not null
            && IsAfterConfiguredCheckOut(stay.ScheduledDepartureDate, at, propertyProfile, checkOutRule.LateCheckOutGraceMinutes)
            && !HasRoleOrSystemActor(ctx, checkOutRule.LateCheckOutApprovalRole))
            return Fail<HotelStayDto>(Error.Business("STAY_LATE_CHECKOUT_APPROVAL_REQUIRED", "Late check-out requires the configured approval role."));
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

    private static bool IsBeforeConfiguredCheckIn(DateOnly arrivalDate, DateTime actualUtc, HotelPropertyProfile profile, int graceMinutes)
    {
        var local = ToPropertyTime(actualUtc, profile.TimeZoneId);
        var scheduled = arrivalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(profile.CheckInMinutes);
        return (scheduled - local).TotalMinutes > Math.Max(0, graceMinutes);
    }

    private static bool IsAfterConfiguredCheckOut(DateOnly departureDate, DateTime actualUtc, HotelPropertyProfile profile, int graceMinutes)
    {
        var local = ToPropertyTime(actualUtc, profile.TimeZoneId);
        var scheduled = departureDate.ToDateTime(TimeOnly.MinValue).AddMinutes(profile.CheckOutMinutes);
        return (local - scheduled).TotalMinutes > Math.Max(0, graceMinutes);
    }

    private static DateTime ToPropertyTime(DateTime utc, string timeZoneId)
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); }
        catch (TimeZoneNotFoundException) { return utc.ToUniversalTime(); }
        catch (InvalidTimeZoneException) { return utc.ToUniversalTime(); }
    }

    private static bool HasRoleOrSystemActor(FrontOfficeExecutionContext context, string? role) =>
        context.Actor.IsSystemActor || (!string.IsNullOrWhiteSpace(role) && context.Actor.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));

    public async Task<Result<HotelStayDto>> ApproveDepartureAsync(ApproveDepartureCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckOut, "StayDepartureApproved", (s, ctx) => { s.ApproveDeparture(clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

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
    {
        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Fail<HotelStayDto>(contextError);
        var now = clock.UtcNow;
        if (hotelConfiguration is not null)
        {
            var rule = await hotelConfiguration.GetActiveWakeupRuleAsync(
                ctx.Property.PropertyId.Value,
                HotelWakeupRuleKind.Scheduled,
                now,
                ct);
            if (rule is not null && c.RequestedAtUtc < now.AddMinutes(rule.DefaultLeadMinutes))
                return Fail<HotelStayDto>(Error.Business("STAY_WAKEUP_LEAD_TIME_REQUIRED", $"Wake-up calls require at least {rule.DefaultLeadMinutes} minutes lead time."));
        }

        return await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayWakeUpCallRequested", (s, ctx) => { s.RequestWakeUpCall(c.RequestedAtUtc, c.Notes, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"WakeUpAt={c.RequestedAtUtc:O}");
    }

    public async Task<Result<HotelStayDto>> LogComplaintAsync(LogComplaintCommand c, CancellationToken ct)
    {
        var category = c.Category?.Trim();
        if (string.IsNullOrWhiteSpace(category)) return Fail<HotelStayDto>(Error.Validation("STAY_COMPLAINT_CATEGORY_REQUIRED", "Complaint category is required."));
        if (string.IsNullOrWhiteSpace(c.Description)) return Fail<HotelStayDto>(Error.Validation("STAY_COMPLAINT_DESCRIPTION_REQUIRED", "Complaint description is required."));

        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Fail<HotelStayDto>(contextError);
        var now = clock.UtcNow;
        var rule = hotelConfiguration is null ? null : await hotelConfiguration.GetActiveComplaintProfileAsync(
            ctx.Property.PropertyId.Value,
            category,
            now,
            ct);
        var metadata = rule is null
            ? $"Category={category}"
            : $"Category={category};ComplaintCode={rule.Code};Severity={rule.Severity};Department={rule.Department};SlaMinutes={rule.SlaMinutes};EscalationMinutes={rule.EscalationMinutes};CompensationAllowed={rule.CompensationAllowed}";
        return await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayComplaintLogged", (s, ctx) => { s.LogComplaint(category, c.Description.Trim(), clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, metadata);
    }

    public async Task<Result<HotelStayDto>> AddTagAsync(AddStayTagCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayTagAdded", (s, ctx) => { s.AddTag(c.Tag, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"Tag={c.Tag}");

    public async Task<Result<HotelStayDto>> RemoveTagAsync(RemoveStayTagCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, "StayTagRemoved", (s, ctx) => { s.RemoveTag(c.Tag, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct, $"Tag={c.Tag}");

    public async Task<Result<HotelStayDto>> MarkVipServiceAsync(MarkVipServiceCommand c, CancellationToken ct)
        => await MutateAsync(c.HotelStayId, c.ExpectedRowVersion, HotelStayPermissions.CheckIn, c.Enabled ? "StayVipServiceStarted" : "StayVipServiceStopped", (s, ctx) => { s.MarkVipService(c.Enabled, clock.UtcNow, ctx.Actor.ActorId); return Task.CompletedTask; }, ct);

    // =============================================================================
    // ADDITIONAL STAY GUEST
    // =============================================================================
    public async Task<Result<StayOccupantDto>> AddOccupantAsync(AddStayOccupantCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckIn, out var denial)) return Result<StayOccupantDto>.Failure(denial!);
        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Result<StayOccupantDto>.Failure(contextError);
        if (!ValidToken(c.ExpectedRowVersion, out var tokenError)) return Result<StayOccupantDto>.Failure(tokenError!);
        if (c.GuestProfileId == Guid.Empty) return Result<StayOccupantDto>.Failure(Error.Validation("STAY_OCCUPANT_GUEST_REQUIRED", "Guest profile is required."));

        var propertyId = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(propertyId, c.HotelStayId, ct);
        if (stay is null) return Result<StayOccupantDto>.Failure(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying()) return Result<StayOccupantDto>.Failure(Error.Business("STAY_NOT_OCCUPYING", "Only an in-house stay can add a companion."));
        if (stay.GuestId == c.GuestProfileId) return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_IS_PRIMARY", "The primary guest is already registered on this stay."));
        if (await occupants.ExistsActiveAsync(propertyId, stay.HotelStayId, c.GuestProfileId, ct))
            return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_DUPLICATE", "This guest is already registered on the stay."));

        var profile = await guests.GetAsync(propertyId, c.GuestProfileId, ct);
        if (profile is null) return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_GUEST_NOT_FOUND", "Guest profile not found."));
        if (profile.IsBlacklisted) return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_GUEST_BLACKLISTED", "Blacklisted guest cannot be added to a stay."));

        var roomType = await rooms.GetRoomTypeDtoAsync(propertyId, stay.RoomTypeId, ct);
        if (roomType is null) return Result<StayOccupantDto>.Failure(Error.Business("STAY_ROOM_TYPE_NOT_FOUND", "Room type not found."));

        var currentOccupants = await occupants.GetActiveAsync(propertyId, stay.HotelStayId, ct);
        var now = clock.UtcNow;
        var adults = Math.Max(stay.Adults, 1 + currentOccupants.Count(x => x.OccupantType == HotelStayOccupantType.Adult) + (c.OccupantType == HotelStayOccupantType.Adult ? 1 : 0));
        var children = Math.Max(stay.Children, currentOccupants.Count(x => x.OccupantType == HotelStayOccupantType.Child) + (c.OccupantType == HotelStayOccupantType.Child ? 1 : 0));
        if (adults > roomType.MaximumAdultCapacity || children > roomType.MaximumChildCapacity)
            return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_CAPACITY_EXCEEDED", "Room capacity does not allow this companion."));

        repository.ApplyExpectedRowVersion(stay, c.ExpectedRowVersion);
        var occupant = HotelStayOccupant.Create(
            ctx.Property.PropertyId,
            stay.HotelStayId,
            profile.GuestProfileId,
            profile.FullName,
            profile.Phone,
            c.OccupantType,
            now,
            ctx.Actor.ActorId);

        try
        {
            stay.ReconcileGuestCounts(adults, children, now, ctx.Actor.ActorId);
            await occupants.AddAsync(occupant, ct);
            await repository.AppendHistoryAsync(HotelStayHistory.Record(
                stay,
                "StayOccupantAdded",
                now,
                ctx.Actor.ActorId?.Value,
                ctx.Correlation.CorrelationId.Value,
                $"OccupantId={occupant.OccupantId:D};GuestProfileId={profile.GuestProfileId:D};Type={c.OccupantType}"), ct);
            foreach (var domainEvent in stay.DomainEvents) persistence.StageDomainEvent(domainEvent);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "StayOccupantAdded",
                nameof(HotelStayOccupant),
                occupant.OccupantId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["HotelStayId"] = stay.HotelStayId.ToString("D"),
                    ["GuestProfileId"] = profile.GuestProfileId.ToString("D"),
                    ["OccupantType"] = c.OccupantType.ToString(),
                    ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D")
                }));

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure)
                return Result<StayOccupantDto>.Failure(commit.Error!.Category == ErrorCategory.Concurrency
                    ? Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "Concurrent modification.")
                    : commit.Error!);

            stay.ClearDomainEvents();
            var saved = await occupants.GetDtoAsync(propertyId, occupant.OccupantId, ct);
            return saved is null
                ? Result<StayOccupantDto>.Failure(Error.System("STAY_OCCUPANT_READ_FAILED", "Could not read the companion after save."))
                : Result<StayOccupantDto>.Success(saved);
        }
        catch (ArgumentException ex)
        {
            return Result<StayOccupantDto>.Failure(Error.Validation(ex.Message, "Companion input is invalid."));
        }
    }

    public async Task<Result<StayOccupantDto>> RemoveOccupantAsync(RemoveStayOccupantCommand c, CancellationToken ct)
    {
        if (!Authorized(HotelStayPermissions.CheckIn, out var denial)) return Result<StayOccupantDto>.Failure(denial!);
        var (ctx, contextError) = RequireContext();
        if (contextError is not null) return Result<StayOccupantDto>.Failure(contextError);
        if (!ValidToken(c.ExpectedStayRowVersion, out var stayTokenError)) return Result<StayOccupantDto>.Failure(stayTokenError!);
        if (!ValidToken(c.ExpectedOccupantRowVersion, out var occupantTokenError)) return Result<StayOccupantDto>.Failure(occupantTokenError!);
        if (c.OccupantId == Guid.Empty) return Result<StayOccupantDto>.Failure(Error.Validation("STAY_OCCUPANT_REQUIRED", "Companion is required."));

        var propertyId = ctx.Property.PropertyId.Value;
        var stay = await repository.GetAsync(propertyId, c.HotelStayId, ct);
        if (stay is null) return Result<StayOccupantDto>.Failure(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying()) return Result<StayOccupantDto>.Failure(Error.Business("STAY_NOT_OCCUPYING", "Only an in-house stay can remove a companion."));

        var occupant = await occupants.GetAsync(propertyId, stay.HotelStayId, c.OccupantId, ct);
        if (occupant is null || !occupant.IsActive)
            return Result<StayOccupantDto>.Failure(Error.Business("STAY_OCCUPANT_NOT_FOUND", "Active companion not found."));

        var currentOccupants = await occupants.GetActiveAsync(propertyId, stay.HotelStayId, ct);
        var currentAdultCount = currentOccupants.Count(x => x.OccupantType == HotelStayOccupantType.Adult);
        var currentChildCount = currentOccupants.Count(x => x.OccupantType == HotelStayOccupantType.Child);
        var adults = stay.Adults;
        var children = stay.Children;
        if (occupant.OccupantType == HotelStayOccupantType.Adult && stay.Adults == 1 + currentAdultCount)
            adults = Math.Max(1, stay.Adults - 1);
        if (occupant.OccupantType == HotelStayOccupantType.Child && stay.Children == currentChildCount)
            children = Math.Max(0, stay.Children - 1);

        repository.ApplyExpectedRowVersion(stay, c.ExpectedStayRowVersion);
        occupants.ApplyExpectedRowVersion(occupant, c.ExpectedOccupantRowVersion);
        var now = clock.UtcNow;
        try
        {
            occupant.Remove(now, ctx.Actor.ActorId);
            stay.ReconcileGuestCountsAfterOccupantRemoval(adults, children, now, ctx.Actor.ActorId);
            await repository.AppendHistoryAsync(HotelStayHistory.Record(
                stay,
                "StayOccupantRemoved",
                now,
                ctx.Actor.ActorId?.Value,
                ctx.Correlation.CorrelationId.Value,
                $"OccupantId={occupant.OccupantId:D};GuestProfileId={occupant.GuestProfileId:D};Type={occupant.OccupantType}"), ct);
            foreach (var domainEvent in stay.DomainEvents) persistence.StageDomainEvent(domainEvent);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "StayOccupantRemoved",
                nameof(HotelStayOccupant),
                occupant.OccupantId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["HotelStayId"] = stay.HotelStayId.ToString("D"),
                    ["GuestProfileId"] = occupant.GuestProfileId.ToString("D"),
                    ["OccupantType"] = occupant.OccupantType.ToString(),
                    ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D")
                }));

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure)
                return Result<StayOccupantDto>.Failure(commit.Error!.Category == ErrorCategory.Concurrency
                    ? Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "Concurrent modification.")
                    : commit.Error!);

            stay.ClearDomainEvents();
            var saved = await occupants.GetDtoAsync(propertyId, occupant.OccupantId, ct);
            return saved is null
                ? Result<StayOccupantDto>.Failure(Error.System("STAY_OCCUPANT_READ_FAILED", "Could not read the companion after removal."))
                : Result<StayOccupantDto>.Success(saved);
        }
        catch (ArgumentException ex)
        {
            return Result<StayOccupantDto>.Failure(Error.Validation(ex.Message, "Companion input is invalid."));
        }
        catch (InvalidOperationException ex)
        {
            return Result<StayOccupantDto>.Failure(Error.Business(ex.Message, "Companion cannot be removed."));
        }
    }

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
        if (string.IsNullOrWhiteSpace(r.GuestNameSnapshot)) throw new ArgumentException("STAY_GUEST_REQUIRED");
        if (r.AssignedRoomId is null) throw new ArgumentException("STAY_ROOM_REQUIRED");
        if (r.RoomTypeId is null) throw new ArgumentException("STAY_ROOM_TYPE_MISMATCH");
    }

    private async Task<Error?> MarkReservationCheckedInAsync(ReservationAggregate reservation, Guid roomId, FrontOfficeExecutionContext ctx, DateTime at, CancellationToken ct)
    {
        if (reservation.Status == ReservationStatus.CheckedIn) return null;
        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Guaranteed))
            return Error.Business("STAY_RESERVATION_NOT_ELIGIBLE", "Only confirmed or guaranteed reservations can check in.");

        if (reservation.AssignedRoomId != roomId)
        {
            var currentStatus = reservation.Status;
            reservation.AssignRoom(roomId, at, ctx.Actor.ActorId);
            await reservations.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, "RoomAssignedAtCheckIn", currentStatus, currentStatus, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
        }

        try
        {
            var from = reservation.Status;
            reservation.MarkCheckedIn(at, ctx.Actor.ActorId);
            await reservations.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, "CheckedIn", from, reservation.Status, at, ctx.Actor.ActorId?.Value, ctx.Correlation.CorrelationId.Value), ct);
            return null;
        }
        catch (InvalidOperationException)
        {
            return Error.Business("STAY_RESERVATION_NOT_ELIGIBLE", "Reservation state does not allow check-in.");
        }
    }

    private async Task<(HotelRoomDto? room, Error? error)> ValidateRoom(Guid pid, Guid roomId, Guid roomTypeId, CancellationToken ct)
    {
        var room = await rooms.GetRoomDtoAsync(pid, roomId, ct);
        if (room is null) return (null, Error.Business("STAY_ROOM_NOT_FOUND", "Room not found."));
        if (!room.IsActive || room.OperationalStatus is not HotelRoomOperationalStatus.Available) return (null, Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Room not assignable."));
        if (room.HotelRoomTypeId != roomTypeId) return (null, Error.Business("STAY_ROOM_TYPE_MISMATCH", "Room type mismatch."));
        return (room, null);
    }

    private async Task<Result<HotelStayDto>> Commit(HotelStay stay, string action, FrontOfficeExecutionContext ctx, CancellationToken ct, ReservationAggregate? reservation = null, string? metadata = null)
    {
        foreach (var de in stay.DomainEvents) persistence.StageDomainEvent(de);
        var auditMetadata = new Dictionary<string, string> { ["PropertyId"] = stay.PropertyId.ToString("D"), ["StayNumber"] = stay.StayNumber, ["Status"] = stay.Status.ToString(), ["ActorId"] = ctx.Actor.ActorId?.Value.ToString("D") ?? "", ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D") };
        if (!string.IsNullOrWhiteSpace(metadata)) auditMetadata["Details"] = metadata;
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(HotelStay), stay.HotelStayId.ToString("D"), "Succeeded", Metadata: auditMetadata));
        if (reservation is not null)
        {
            foreach (var de in reservation.DomainEvents) persistence.StageDomainEvent(de);
            persistence.StageAudit(new FrontOfficeAuditIntent("ReservationCheckedIn", nameof(ReservationAggregate), reservation.ReservationId.ToString("D"), "Succeeded", Metadata: new Dictionary<string, string> { ["PropertyId"] = reservation.PropertyId.ToString("D"), ["ReservationNumber"] = reservation.ReservationNumber, ["RoomId"] = reservation.AssignedRoomId?.ToString("D") ?? "", ["Status"] = reservation.Status.ToString(), ["ActorId"] = ctx.Actor.ActorId?.Value.ToString("D") ?? "", ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D") }));
        }
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) return Fail<HotelStayDto>(commit.Error!.Category == ErrorCategory.Concurrency ? Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "Concurrent modification.") : commit.Error!);
        stay.ClearDomainEvents();
        reservation?.ClearDomainEvents();
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

public static class RoomMovePolicyEvaluator
{
    public static Error? Validate(HotelRoomMoveUpgradeRule? rule, int fromClass, int toClass, string? housekeepingStatus, FrontOfficeExecutionContext context)
    {
        if (rule is null) return null;

        var change = Math.Sign(toClass - fromClass);
        if (change == 0 && !rule.AllowRoomMove)
            return Error.Business("STAY_ROOM_MOVE_DISABLED", "Room moves are disabled by the active room move policy.");
        if (change > 0 && !rule.AllowUpgrade)
            return Error.Business("STAY_ROOM_UPGRADE_DISABLED", "Room upgrades are disabled by the active room move policy.");
        if (change < 0 && !rule.AllowDowngrade)
            return Error.Business("STAY_ROOM_DOWNGRADE_DISABLED", "Room downgrades are disabled by the active room move policy.");
        if (change != 0 && rule.MaxClassChangeLevels > 0 && Math.Abs(toClass - fromClass) > rule.MaxClassChangeLevels)
            return Error.Business("STAY_ROOM_MOVE_CLASS_LIMIT", "The requested room class change exceeds the active room move policy limit.");
        if (rule.RequireHousekeepingReady && !IsHousekeepingReady(housekeepingStatus))
            return Error.Business("STAY_ROOM_HOUSEKEEPING_NOT_READY", "The target room is not ready according to the active room move policy.");
        if (rule.RequireApproval && !HasRoleOrSystemActor(context, rule.ApprovalRole))
            return Error.Business("STAY_ROOM_MOVE_APPROVAL_REQUIRED", "The room move requires the configured approval role.");
        return null;
    }

    private static bool IsHousekeepingReady(string? status) =>
        status is not null && (status.Equals("VacantClean", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Inspected", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Clean", StringComparison.OrdinalIgnoreCase));

    private static bool HasRoleOrSystemActor(FrontOfficeExecutionContext context, string? role) =>
        context.Actor.IsSystemActor || (!string.IsNullOrWhiteSpace(role) && context.Actor.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
}
