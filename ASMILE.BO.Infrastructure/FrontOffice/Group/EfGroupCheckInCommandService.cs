using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupCheckInCommandService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock,
    IReservationNumberGenerator reservationNumberGenerator,
    IHotelStayNumberGenerator stayNumberGenerator,
    IHotelFolioNumberGenerator folioNumberGenerator) : IGroupCheckInCommandService
{
    public async Task<GroupCheckInResult> CheckInAsync(
        Guid groupId,
        IReadOnlyCollection<Guid> assignmentIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignmentIds);
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomingList)
            .SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        if (assignmentIds.Count == 0)
            throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENTS_REQUIRED");
        var selected = group.RoomingList
            .Where(x => assignmentIds.Contains(x.GroupRoomAssignmentId))
            .ToList();
        if (selected.Count != assignmentIds.Distinct().Count())
            throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENT_NOT_FOUND");
        if (group.Status is GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut)
            throw new InvalidOperationException("GROUP_STATUS_NOT_CHECKINABLE");
        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (today < group.ArrivalDate)
            throw new InvalidOperationException("GROUP_ARRIVAL_NOT_REACHED");
        if (today >= group.DepartureDate)
            throw new InvalidOperationException("GROUP_DEPARTURE_REACHED");
        if (group.Status is not (GroupReservationStatus.Confirmed or GroupReservationStatus.CheckedIn))
            throw new InvalidOperationException("GROUP_NOT_CONFIRMED");
        if (selected.Any(x => x.IsCheckedOut))
            throw new InvalidOperationException("GROUP_ROOM_ALREADY_CHECKED_OUT");

        var now = clock.UtcNow;
        var createdReservations = 0;
        var createdStays = 0;
        var createdFolios = 0;
        foreach (var assignment in selected)
        {
            var chain = await EnsureRealGuestChainAsync(group, assignment, propertyId, context, now, ct);
            if (chain.CreatedReservation) createdReservations++;
            if (chain.CreatedStay) createdStays++;
            if (chain.CreatedFolio) createdFolios++;
            assignment.MarkCheckedIn();
        }
        group.MarkCheckedIn(now);

        persistence.StageAudit(new FrontOfficeAuditIntent(
            "GroupCheckedIn",
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["AssignmentCount"] = selected.Count.ToString(),
                ["GuestCount"] = selected.Sum(x => x.Adults + x.Children).ToString(),
                ["CreatedReservations"] = createdReservations.ToString(),
                ["CreatedStays"] = createdStays.ToString(),
                ["CreatedFolios"] = createdFolios.ToString()
            }));

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_CHECKIN_FAILED");

        return new GroupCheckInResult(
            group.GroupReservationId,
            group.GroupCode,
            group.Status,
            selected.Count,
            selected.Sum(x => x.Adults + x.Children),
            now);
    }

    private async Task<GuestChainResult> EnsureRealGuestChainAsync(
        GroupReservation group,
        GroupRoomAssignment assignment,
        Guid propertyId,
        FrontOfficeExecutionContext context,
        DateTime now,
        CancellationToken ct)
    {
        if (assignment.ReservationId is null && assignment.GuestId is null)
            throw new InvalidOperationException("GROUP_ROOM_RESERVATION_REQUIRED");
        if (assignment.GuestId is null)
            throw new InvalidOperationException("GROUP_ROOM_GUEST_REQUIRED");

        var guest = await db.GuestProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.GuestProfileId == assignment.GuestId.Value, ct)
            ?? throw new KeyNotFoundException("GROUP_GUEST_NOT_FOUND");
        if (guest.IsBlacklisted)
            throw new InvalidOperationException("GROUP_GUEST_BLACKLISTED");

        var room = await db.HotelRooms
            .Join(db.HotelRoomTypes,
                room => room.HotelRoomTypeId,
                roomType => roomType.HotelRoomTypeId,
                (room, roomType) => new { room, roomType })
            .SingleOrDefaultAsync(x => x.room.PropertyId == propertyId && x.room.HotelRoomId == assignment.HotelRoomId, ct)
            ?? throw new KeyNotFoundException("HOTEL_ROOM_NOT_FOUND");
        if (!room.room.IsActive || room.room.OperationalStatus != HotelRoomOperationalStatus.Available || !room.roomType.IsActive)
            throw new InvalidOperationException("HOTEL_ROOM_NOT_ASSIGNABLE");
        if (!string.Equals(room.roomType.Code, assignment.RoomType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GROUP_ROOM_TYPE_MISMATCH");

        ReservationAggregate? reservation = null;
        var createdReservation = false;
        if (assignment.ReservationId is { } reservationId)
        {
            reservation = await db.FrontOfficeReservations
                .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ReservationId == reservationId, ct)
                ?? throw new KeyNotFoundException("GROUP_RESERVATION_NOT_FOUND");
            if (reservation.GuestId != assignment.GuestId
                || reservation.AssignedRoomId != assignment.HotelRoomId
                || reservation.RoomTypeId != room.room.HotelRoomTypeId
                || reservation.ArrivalDate != group.ArrivalDate
                || reservation.DepartureDate != group.DepartureDate)
                throw new InvalidOperationException("GROUP_RESERVATION_MISMATCH");
            if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Guaranteed or ReservationStatus.CheckedIn))
                throw new InvalidOperationException("GROUP_RESERVATION_NOT_CHECKINABLE");
        }
        else
        {
            var hasConflict = await db.FrontOfficeReservations.AnyAsync(x =>
                x.PropertyId == propertyId
                && x.AssignedRoomId == assignment.HotelRoomId
                && x.ArrivalDate < group.DepartureDate
                && group.ArrivalDate < x.DepartureDate
                && x.Status != ReservationStatus.Cancelled
                && x.Status != ReservationStatus.NoShow
                && x.Status != ReservationStatus.Completed, ct);
            if (hasConflict)
                throw new InvalidOperationException("GROUP_ROOM_RESERVATION_CONFLICT");

            var reservationNumber = await reservationNumberGenerator.GenerateAsync(propertyId, context.BusinessDate.BusinessDate, ct);
            reservation = ReservationAggregate.Create(
                reservationNumber,
                context.Property.PropertyId,
                guest.GuestProfileId,
                guest.FullName,
                StayPeriod.Create(group.ArrivalDate, group.DepartureDate),
                GuestCount.Create(assignment.Adults, assignment.Children),
                room.room.HotelRoomTypeId,
                "Group",
                now,
                context.Actor.ActorId);
            reservation.AssignRoom(assignment.HotelRoomId, now, context.Actor.ActorId);
            reservation.MarkTentative(now, context.Actor.ActorId);
            reservation.Confirm(now, context.Actor.ActorId);
            db.FrontOfficeReservations.Add(reservation);
            db.FrontOfficeReservationHistory.Add(ReservationHistory.Record(
                reservation.ReservationId, "Created", null, ReservationStatus.Draft, now,
                context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value));
            db.FrontOfficeReservationHistory.Add(ReservationHistory.Record(
                reservation.ReservationId, "Confirmed", ReservationStatus.Tentative, reservation.Status, now,
                context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value));
            foreach (var domainEvent in reservation.DomainEvents)
                persistence.StageDomainEvent(domainEvent);
            reservation.ClearDomainEvents();
            assignment.LinkReservation(reservation.ReservationId);
            createdReservation = true;
        }

        var stay = await db.HotelStays
            // Keep the predicate SQL-translatable. Calling the domain extension
            // method inside IQueryable would fail at runtime on SQL Server.
            .SingleOrDefaultAsync(x =>
                x.PropertyId == propertyId
                && x.ReservationId == reservation.ReservationId
                && (x.Status == HotelStayStatus.PendingArrival
                    || x.Status == HotelStayStatus.PreArrival
                    || x.Status == HotelStayStatus.CheckedIn
                    || x.Status == HotelStayStatus.InHouse
                    || x.Status == HotelStayStatus.RoomMoved
                    || x.Status == HotelStayStatus.Extended
                    || x.Status == HotelStayStatus.ExpectedDeparture),
                ct);
        var createdStay = false;
        if (stay is null)
        {
            var stayNumber = await stayNumberGenerator.GenerateAsync(propertyId, context.BusinessDate.BusinessDate, ct);
            stay = HotelStay.Prepare(
                stayNumber,
                context.Property.PropertyId,
                reservation.ReservationId,
                reservation.ReservationNumber,
                guest.GuestProfileId,
                guest.FullName,
                guest.Phone,
                room.room.HotelRoomId,
                room.room.RoomNumber,
                room.room.HotelRoomTypeId,
                room.roomType.Code,
                HotelStayPeriod.Create(group.ArrivalDate, group.DepartureDate),
                HotelStayGuestCount.Create(assignment.Adults, assignment.Children),
                "Group",
                now,
                context.Actor.ActorId);
            stay.CheckIn(now, context.Actor.ActorId);
            db.HotelStays.Add(stay);
            db.HotelStayHistories.Add(HotelStayHistory.RecordPrepared(stay, now, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value));
            db.HotelStayHistories.Add(HotelStayHistory.RecordCheckedIn(stay, now, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value));
            foreach (var domainEvent in stay.DomainEvents)
                persistence.StageDomainEvent(domainEvent);
            stay.ClearDomainEvents();
            createdStay = true;
        }
        else if (stay.RoomId != assignment.HotelRoomId || stay.GuestId != assignment.GuestId)
        {
            throw new InvalidOperationException("GROUP_STAY_MISMATCH");
        }
        else if (stay.Status.IsPreCheckIn())
        {
            stay.CheckIn(now, context.Actor.ActorId);
            db.HotelStayHistories.Add(HotelStayHistory.RecordCheckedIn(stay, now, context.Actor.ActorId?.Value, context.Correlation.CorrelationId.Value));
            foreach (var domainEvent in stay.DomainEvents)
                persistence.StageDomainEvent(domainEvent);
            stay.ClearDomainEvents();
        }
        else if (!stay.Status.IsOccupying())
        {
            throw new InvalidOperationException("GROUP_STAY_NOT_CHECKINABLE");
        }

        var folio = await db.HotelFolios
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelStayId == stay.HotelStayId, ct);
        var createdFolio = false;
        if (folio is null)
        {
            var folioNumber = await folioNumberGenerator.GenerateAsync(propertyId, context.BusinessDate.BusinessDate, ct);
            folio = HotelFolio.Create(propertyId, stay.HotelStayId, folioNumber, now, context.Actor.ActorId);
            db.HotelFolios.Add(folio);
            createdFolio = true;
        }

        return new GuestChainResult(createdReservation, createdStay, createdFolio);
    }

    private sealed record GuestChainResult(bool CreatedReservation, bool CreatedStay, bool CreatedFolio);
}
