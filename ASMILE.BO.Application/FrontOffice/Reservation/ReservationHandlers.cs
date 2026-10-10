using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Shared.Foundation;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Application.FrontOffice.Reservation;

public sealed class ReservationApplicationService(IReservationRepository repository, IReservationNumberGenerator numberGenerator, IHotelReservationReferenceReader hotelReferences, IFrontOfficePersistenceSession persistence, IFrontOfficeExecutionContextAccessor contextAccessor, IAuthorizationService authorization, IClock clock, IOverbookingChecker overbookingChecker, IRateRepository? rateRepository = null)
{
    public async Task<Result<ReservationDto>> CreateAsync(CreateReservationCommand command, CancellationToken ct)
    {
        if (!Authorized(ReservationPermissions.Create, out var denial)) return Result<ReservationDto>.Failure(denial!);
        if (command.ConfirmImmediately && !Authorized(ReservationPermissions.ChangeStatus, out var statusDenial))
            return Result<ReservationDto>.Failure(statusDenial!);
        var context = Context(command.PropertyId); if (context is null) return PropertyFailure();
        if (command.ConfirmImmediately && command.RoomTypeId is null)
            return Result<ReservationDto>.Failure(Error.Business("RESERVATION_ROOM_TYPE_REQUIRED", "A room type is required to confirm a reservation."));
        var masterValidation = await ValidateMasterReferences(command.BookingSourceProfileId, command.BookingChannelProfileId, command.MarketSegmentProfileId, command.CompanyProfileId, command.TravelAgentProfileId, command.CommissionRuleProfileId, context.Property.PropertyId.Value, ct);
        if (masterValidation is not null) return Result<ReservationDto>.Failure(masterValidation);
        var type = await ValidateRoomType(command.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return Result<ReservationDto>.Failure(type.Error!);
        if (command.RoomTypeId is not null && await overbookingChecker.IsOverbookingLimitReachedAsync(context.Property.PropertyId.Value, command.RoomTypeId.Value, command.ArrivalDate, command.DepartureDate, null, ct)) return Result<ReservationDto>.Failure(Error.Business("RESERVATION_OVERBOOKING_LIMIT", "Overbooking limit reached for this room type and date range."));
        try
        {
            var number = await numberGenerator.GenerateAsync(context.Property.PropertyId.Value, context.BusinessDate.BusinessDate, ct);
            var ratePlanId = await ResolveRatePlanIdAsync(command.RatePlanId, command.RoomTypeId, command.ArrivalDate, context.Property.PropertyId.Value, ct);
            if (command.ConfirmImmediately && rateRepository is not null && ratePlanId is null)
                return Result<ReservationDto>.Failure(Error.Business("RESERVATION_RATE_PLAN_REQUIRED", "An active rate plan is required before confirming the reservation."));
            var reservation = ReservationAggregate.Create(number, context.Property.PropertyId, command.GuestId, command.GuestName, StayPeriod.Create(command.ArrivalDate, command.DepartureDate), GuestCount.Create(command.AdultCount, command.ChildCount), command.RoomTypeId, command.Source, clock.UtcNow, context.Actor.ActorId, command.GuestPhone, command.GuestEmail, ratePlanId, command.BookingSourceProfileId, command.BookingChannelProfileId, command.MarketSegmentProfileId, command.CompanyProfileId, command.TravelAgentProfileId, command.CommissionRuleProfileId, command.MarketSegment, command.CompanyName, command.TravelAgentName, command.IsVip);
            var createdHistory = ReservationHistory.Record(reservation.ReservationId, "Created", null, reservation.Status, clock.UtcNow, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value);
            if (command.ConfirmImmediately)
            {
                var fromStatus = reservation.Status;
                reservation.MarkTentative(clock.UtcNow, context.Actor.ActorId);
                await repository.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, "MarkedTentative", fromStatus, reservation.Status, clock.UtcNow, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value), ct);

                fromStatus = reservation.Status;
                reservation.Confirm(clock.UtcNow, context.Actor.ActorId);
                await repository.AppendHistoryAsync(ReservationHistory.Record(reservation.ReservationId, "Confirmed", fromStatus, reservation.Status, clock.UtcNow, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value), ct);
            }

            await repository.AddAsync(reservation, createdHistory, ct);
            return await Commit(reservation, command.ConfirmImmediately ? "CreatedAndConfirmed" : "Created", null, ct);
        }
        catch (ArgumentException ex) { return Result<ReservationDto>.Failure(Error.Validation(ex.Message, "Reservation input is invalid.")); }
        catch (InvalidOperationException ex) { return Result<ReservationDto>.Failure(Error.Business(ex.Message, "Reservation state does not allow this action.")); }
    }

    public Task<Result<ReservationDto>> UpdateDetailsAsync(UpdateReservationDetailsCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "DetailsUpdated", async (r, context) =>
    {
        var type = await ValidateRoomType(c.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return type.Error;
        var commercialChanged = c.BookingSourceProfileId is not null || c.BookingChannelProfileId is not null || c.MarketSegmentProfileId is not null || c.CompanyProfileId is not null || c.TravelAgentProfileId is not null || c.CommissionRuleProfileId is not null || c.MarketSegment is not null || c.CompanyName is not null || c.TravelAgentName is not null || c.IsVip is not null;
        if (commercialChanged)
        {
            var masterValidation = await ValidateMasterReferences(c.BookingSourceProfileId, c.BookingChannelProfileId, c.MarketSegmentProfileId, c.CompanyProfileId, c.TravelAgentProfileId, c.CommissionRuleProfileId, context.Property.PropertyId.Value, ct);
            if (masterValidation is not null) return masterValidation;
        }
        if (r.AssignedRoomId is not null && c.RoomTypeId is not null && !await hotelReferences.HotelRoomMatchesRoomTypeAsync(context.Property.PropertyId.Value, r.AssignedRoomId.Value, c.RoomTypeId.Value, ct)) return Error.Business("RESERVATION_ASSIGNED_ROOM_TYPE_MISMATCH", "Assigned room does not match room type.");
        if ((c.ArrivalDate is null) != (c.DepartureDate is null)) return Error.Validation("RESERVATION_STAY_DATES_REQUIRED", "Arrival and departure dates must be supplied together.");
        var arrival = c.ArrivalDate ?? r.ArrivalDate;
        var departure = c.DepartureDate ?? r.DepartureDate;
        var stayChanged = arrival != r.ArrivalDate || departure != r.DepartureDate;
        var roomTypeChanged = c.RoomTypeId != r.RoomTypeId;
        if (stayChanged) _ = StayPeriod.Create(arrival, departure);
        if ((c.AdultCount is null) != (c.ChildCount is null)) return Error.Validation("RESERVATION_GUEST_COUNTS_REQUIRED", "Adult and child counts must be supplied together.");
        var guestCountsChanged = c.AdultCount is { } requestedAdults && c.ChildCount is { } requestedChildren
            && (requestedAdults != r.AdultCount || requestedChildren != r.ChildCount);
        if (guestCountsChanged) _ = GuestCount.Create(c.AdultCount!.Value, c.ChildCount!.Value);
        if (stayChanged && r.AssignedRoomId is { } assignedRoomId)
        {
            var conflict = await repository.CheckAsync(context.Property.PropertyId.Value, assignedRoomId, arrival, departure, r.ReservationId, ct);
            if (conflict.HasConflict) return Error.Business("RESERVATION_ROOM_CONFLICT", "The assigned room has an overlapping reservation for the new dates.");
        }
        else if ((stayChanged || roomTypeChanged) && c.RoomTypeId is { } typeId && await overbookingChecker.IsOverbookingLimitReachedAsync(context.Property.PropertyId.Value, typeId, arrival, departure, r.ReservationId, ct))
        {
            return Error.Business("RESERVATION_OVERBOOKING_LIMIT", "Overbooking limit reached for this room type and date range.");
        }

        var ratePlanId = await ResolveRatePlanIdAsync(c.RatePlanId ?? r.RatePlanId, c.RoomTypeId, arrival, context.Property.PropertyId.Value, ct);
        if (rateRepository is not null && c.RoomTypeId is not null && ratePlanId is null)
            return Error.Business("RESERVATION_RATE_PLAN_REQUIRED", "An active rate plan is required for the selected room type and date range.");
        var now = clock.UtcNow;
        r.Update(c.GuestName, c.Phone, c.Email, c.RoomTypeId, ratePlanId, c.SpecialRequests, c.InternalNotes, now, context.Actor.ActorId, c.Source);
        if (commercialChanged) r.SetCommercialReferences(c.BookingSourceProfileId ?? r.BookingSourceProfileId, c.BookingChannelProfileId ?? r.BookingChannelProfileId, c.MarketSegmentProfileId ?? r.MarketSegmentProfileId, c.CompanyProfileId ?? r.CompanyProfileId, c.TravelAgentProfileId ?? r.TravelAgentProfileId, c.CommissionRuleProfileId ?? r.CommissionRuleProfileId, c.MarketSegment ?? r.MarketSegment, c.CompanyName ?? r.CompanyName, c.TravelAgentName ?? r.TravelAgentName, c.IsVip ?? r.IsVip, now, context.Actor.ActorId);
        if (stayChanged) r.ChangeStay(StayPeriod.Create(arrival, departure), now, context.Actor.ActorId);
        if (guestCountsChanged) r.ChangeGuests(GuestCount.Create(c.AdultCount!.Value, c.ChildCount!.Value), now, context.Actor.ActorId);
        return null;
    }, ct);
    public Task<Result<ReservationDto>> ChangeStayAsync(ChangeReservationStayCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "StayChanged", (r, context) => { r.ChangeStay(StayPeriod.Create(c.ArrivalDate, c.DepartureDate), clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> ChangeGuestsAsync(ChangeReservationGuestCountCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "GuestCountChanged", (r, context) => { r.ChangeGuests(GuestCount.Create(c.Adults, c.Children), clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> UnassignAsync(UnassignReservationRoomCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.AssignRoom, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "RoomUnassigned", (r, context) => { r.UnassignRoom(clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> TentativeAsync(MarkReservationTentativeCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "MarkedTentative", (r, context) => { r.MarkTentative(clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> ConfirmAsync(ConfirmReservationCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "Confirmed", async (r, context) =>
    {
        var type = await ValidateRoomType(r.RoomTypeId, context.Property.PropertyId.Value, ct); if (type is { IsFailure: true }) return type.Error;
        if (r.RatePlanId is null && rateRepository is not null && r.RoomTypeId is { } roomTypeId)
        {
            var resolved = await ResolveRatePlanIdAsync(null, roomTypeId, r.ArrivalDate, context.Property.PropertyId.Value, ct);
            if (resolved is null) return Error.Business("RESERVATION_RATE_PLAN_REQUIRED", "An active rate plan is required before confirming the reservation.");
            r.SetRatePlan(resolved.Value, clock.UtcNow, context.Actor.ActorId);
        }
        r.Confirm(clock.UtcNow, context.Actor.ActorId); return null;
    }, ct);
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
    public Task<Result<ReservationDto>> GuaranteeAsync(GuaranteeReservationCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.ChangeStatus, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "Guaranteed", (r, context) => { r.Guarantee(c.GuaranteeType, clock.UtcNow, context.Actor.ActorId, c.GuaranteeExpiryDate); return Task.FromResult<Error?>(null); }, ct);
    public Task<Result<ReservationDto>> RecordDepositAsync(RecordReservationDepositCommand c, CancellationToken ct) => MutateAsync(ReservationPermissions.Update, c.PropertyId, c.ReservationId, c.ExpectedRowVersion, "DepositRecorded", (r, context) => { r.RecordDeposit(c.DepositAmount, c.DepositDueDate, clock.UtcNow, context.Actor.ActorId); return Task.FromResult<Error?>(null); }, ct);

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
    private async Task<Error?> ValidateMasterReferences(Guid? sourceId, Guid? channelId, Guid? segmentId, Guid? companyId, Guid? travelAgentId, Guid? commissionId, Guid propertyId, CancellationToken ct)
    {
        if (sourceId is { } source && !await hotelReferences.IsActiveBookingSourceAsync(propertyId, source, ct)) return Error.Business("RESERVATION_BOOKING_SOURCE_INVALID", "The selected booking source is missing or inactive.");
        if (channelId is { } channel && !await hotelReferences.IsActiveBookingChannelAsync(propertyId, channel, ct)) return Error.Business("RESERVATION_BOOKING_CHANNEL_INVALID", "The selected booking channel is missing or inactive.");
        if (segmentId is { } segment && !await hotelReferences.IsActiveMarketSegmentAsync(propertyId, segment, ct)) return Error.Business("RESERVATION_MARKET_SEGMENT_INVALID", "The selected market segment is missing or inactive.");
        if (companyId is { } company && !await hotelReferences.IsActiveCompanyAsync(propertyId, company, ct)) return Error.Business("RESERVATION_COMPANY_INVALID", "The selected company is missing or inactive.");
        if (travelAgentId is { } travelAgent && !await hotelReferences.IsActiveTravelAgentAsync(propertyId, travelAgent, ct)) return Error.Business("RESERVATION_TRAVEL_AGENT_INVALID", "The selected travel agent is missing or inactive.");
        if (commissionId is { } commission && !await hotelReferences.IsActiveCommissionRuleAsync(propertyId, commission, ct)) return Error.Business("RESERVATION_COMMISSION_RULE_INVALID", "The selected commission rule is missing or inactive.");
        return null;
    }
    private async Task<Guid?> ResolveRatePlanIdAsync(Guid? requestedRatePlanId, Guid? roomTypeId, DateOnly date, Guid propertyId, CancellationToken ct)
    {
        if (rateRepository is null) return requestedRatePlanId;
        if (requestedRatePlanId is { } requested)
        {
            var requestedPlan = await rateRepository.GetAsync(requested, propertyId, ct);
            if (requestedPlan is null || requestedPlan.Status != RatePlanStatus.Active || requestedPlan.RoomTypeId != roomTypeId
                || DateOnly.FromDateTime(requestedPlan.EffectiveFrom) > date || DateOnly.FromDateTime(requestedPlan.EffectiveTo) < date)
                throw new InvalidOperationException("RESERVATION_RATE_PLAN_INVALID");
            return requestedPlan.RatePlanId;
        }
        if (roomTypeId is null) return null;
        var plans = await rateRepository.GetAllAsync(propertyId, new RatePlanSearchFilter(propertyId, RatePlanStatus.Active, null, null), ct);
        return plans
            .Where(x => x.RoomTypeId == roomTypeId
                && DateOnly.FromDateTime(x.EffectiveFrom) <= date
                && DateOnly.FromDateTime(x.EffectiveTo) >= date)
            .OrderBy(x => x.Hierarchy)
            .ThenBy(x => x.PlanCode)
            .Select(x => (Guid?)x.RatePlanId)
            .FirstOrDefault();
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
