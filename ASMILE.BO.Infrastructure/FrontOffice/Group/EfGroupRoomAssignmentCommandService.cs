using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupRoomAssignmentCommandService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IGroupRoomAssignmentCommandService
{
    public async Task<GroupRoomAssignmentDto> AssignAsync(AssignGroupRoomRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomingList)
            .SingleOrDefaultAsync(x => x.GroupReservationId == request.GroupReservationId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");
        if (group.Status is not (GroupReservationStatus.Draft or GroupReservationStatus.Tentative or GroupReservationStatus.Confirmed))
            throw new InvalidOperationException("GROUP_STATUS_NOT_ASSIGNABLE");
        if (request.Adults < 0 || request.Children < 0 || request.Adults + request.Children == 0)
            throw new ArgumentException("GROUP_OCCUPANCY_REQUIRED");
        if (request.GuestId is not null && string.IsNullOrWhiteSpace(request.GuestName))
            throw new ArgumentException("GROUP_GUEST_NAME_REQUIRED");
        if (group.ExpectedRooms > 0 && group.ConfirmedRooms >= group.ExpectedRooms)
            throw new InvalidOperationException("GROUP_ROOM_COUNT_EXCEEDED");

        var room = await db.HotelRooms
            .Join(db.HotelRoomTypes,
                room => room.HotelRoomTypeId,
                roomType => roomType.HotelRoomTypeId,
                (room, roomType) => new { room, roomType })
            .SingleOrDefaultAsync(x => x.room.PropertyId == propertyId && x.room.HotelRoomId == request.HotelRoomId, ct)
            ?? throw new KeyNotFoundException("HOTEL_ROOM_NOT_FOUND");
        if (!room.room.IsActive || room.room.OperationalStatus != HotelRoomOperationalStatus.Available || !room.roomType.IsActive)
            throw new InvalidOperationException("HOTEL_ROOM_NOT_ASSIGNABLE");
        if (request.GuestId is not null && !await db.GuestProfiles.AnyAsync(
                x => x.PropertyId == propertyId && x.GuestProfileId == request.GuestId.Value, ct))
            throw new KeyNotFoundException("GROUP_GUEST_NOT_FOUND");

        var overlap = await db.FrontOfficeGroupRoomAssignments
            .Where(x => x.PropertyId == propertyId && x.HotelRoomId == request.HotelRoomId && x.GroupReservationId != group.GroupReservationId)
            .Join(db.FrontOfficeGroupReservations,
                assignment => assignment.GroupReservationId,
                otherGroup => otherGroup.GroupReservationId,
                (assignment, otherGroup) => otherGroup)
            .AnyAsync(otherGroup =>
                otherGroup.Status != GroupReservationStatus.Cancelled
                && otherGroup.Status != GroupReservationStatus.Closed
                && otherGroup.Status != GroupReservationStatus.CheckedOut
                && otherGroup.ArrivalDate < group.DepartureDate
                && group.ArrivalDate < otherGroup.DepartureDate,
                ct);
        if (overlap)
            throw new InvalidOperationException("HOTEL_ROOM_GROUP_DATE_CONFLICT");

        var now = clock.UtcNow;
        var assignment = group.AddRoom(
            room.room.HotelRoomId,
            room.room.RoomNumber,
            room.roomType.Code,
            request.GuestName,
            request.Adults,
            request.Children,
            now);
        if (request.GuestId is not null)
            assignment.AssignGuest(request.GuestId.Value, request.GuestName ?? string.Empty);
        db.FrontOfficeGroupRoomAssignments.Add(assignment);
        await CommitAsync("GroupRoomAssigned", assignment, propertyId, ct);
        return Map(assignment);
    }

    public async Task<ReplaceGroupRoomAssignmentsResult> ReplaceAsync(ReplaceGroupRoomAssignmentsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var requested = request.Assignments ?? throw new ArgumentNullException(nameof(request.Assignments));
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomingList)
            .SingleOrDefaultAsync(x => x.GroupReservationId == request.GroupReservationId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        if (group.Status is not (GroupReservationStatus.Draft or GroupReservationStatus.Tentative or GroupReservationStatus.Confirmed))
            throw new InvalidOperationException("GROUP_STATUS_NOT_ASSIGNABLE");
        if (group.RoomingList.Any(x => x.IsCheckedIn || x.IsCheckedOut))
            throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENT_ALREADY_USED");
        if (group.ExpectedRooms > 0 && requested.Count > group.ExpectedRooms)
            throw new InvalidOperationException("GROUP_ROOM_COUNT_EXCEEDED");
        if (requested.Select(x => x.HotelRoomId).Distinct().Count() != requested.Count)
            throw new InvalidOperationException("GROUP_ROOM_DUPLICATE");

        var roomIds = requested.Select(x => x.HotelRoomId).ToArray();
        var rooms = await db.HotelRooms
            .Where(x => x.PropertyId == propertyId && roomIds.Contains(x.HotelRoomId))
            .Join(db.HotelRoomTypes,
                room => room.HotelRoomTypeId,
                roomType => roomType.HotelRoomTypeId,
                (room, roomType) => new { room, roomType })
            .ToListAsync(ct);
        if (rooms.Count != requested.Count)
            throw new KeyNotFoundException("HOTEL_ROOM_NOT_FOUND");

        foreach (var item in requested)
        {
            if (item.Adults < 0 || item.Children < 0 || item.Adults + item.Children == 0)
                throw new ArgumentException("GROUP_OCCUPANCY_REQUIRED");
            if (item.GuestId is not null && string.IsNullOrWhiteSpace(item.GuestName))
                throw new ArgumentException("GROUP_GUEST_NAME_REQUIRED");

            var room = rooms.Single(x => x.room.HotelRoomId == item.HotelRoomId);
            if (!room.room.IsActive || room.room.OperationalStatus != HotelRoomOperationalStatus.Available || !room.roomType.IsActive)
                throw new InvalidOperationException("HOTEL_ROOM_NOT_ASSIGNABLE");

            if (item.GuestId is not null && !await db.GuestProfiles.AnyAsync(
                    x => x.PropertyId == propertyId && x.GuestProfileId == item.GuestId.Value, ct))
                throw new KeyNotFoundException("GROUP_GUEST_NOT_FOUND");

            var overlap = await db.FrontOfficeGroupRoomAssignments
                .Where(x => x.PropertyId == propertyId && x.HotelRoomId == item.HotelRoomId && x.GroupReservationId != group.GroupReservationId)
                .Join(db.FrontOfficeGroupReservations,
                    assignment => assignment.GroupReservationId,
                    otherGroup => otherGroup.GroupReservationId,
                    (assignment, otherGroup) => otherGroup)
                .AnyAsync(otherGroup =>
                    otherGroup.Status != GroupReservationStatus.Cancelled
                    && otherGroup.Status != GroupReservationStatus.Closed
                    && otherGroup.Status != GroupReservationStatus.CheckedOut
                    && otherGroup.ArrivalDate < group.DepartureDate
                    && group.ArrivalDate < otherGroup.DepartureDate,
                    ct);
            if (overlap)
                throw new InvalidOperationException("HOTEL_ROOM_GROUP_DATE_CONFLICT");
        }

        var now = clock.UtcNow;
        foreach (var current in group.RoomingList.ToList())
        {
            group.RemoveRoom(current.HotelRoomId);
            db.FrontOfficeGroupRoomAssignments.Remove(current);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "GroupRoomAssignmentReplaced",
                nameof(GroupRoomAssignment),
                current.GroupRoomAssignmentId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                    ["HotelRoomId"] = current.HotelRoomId.ToString("D"),
                    ["Operation"] = "RemovedBeforeReplace"
                }));
        }

        foreach (var item in requested)
        {
            var room = rooms.Single(x => x.room.HotelRoomId == item.HotelRoomId);
            var assignment = group.AddRoom(
                room.room.HotelRoomId,
                room.room.RoomNumber,
                room.roomType.Code,
                item.GuestName,
                item.Adults,
                item.Children,
                now);
            if (item.GuestId is not null)
                assignment.AssignGuest(item.GuestId.Value, item.GuestName!);
            db.FrontOfficeGroupRoomAssignments.Add(assignment);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "GroupRoomAssignmentReplaced",
                nameof(GroupRoomAssignment),
                assignment.GroupRoomAssignmentId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                    ["HotelRoomId"] = assignment.HotelRoomId.ToString("D"),
                    ["RoomNumber"] = assignment.RoomNumber,
                    ["Operation"] = "AddedAfterReplace"
                }));
        }

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_ROOM_ASSIGN_REPLACE_FAILED");
        return new ReplaceGroupRoomAssignmentsResult(group.GroupReservationId, requested.Count, now);
    }

    public async Task RemoveAsync(Guid groupId, Guid hotelRoomId, CancellationToken ct)
    {
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomingList)
            .SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");
        if (group.Status is GroupReservationStatus.CheckedIn or GroupReservationStatus.CheckedOut)
            throw new InvalidOperationException("GROUP_STATUS_NOT_ASSIGNABLE");
        var assignment = group.RoomingList.SingleOrDefault(x => x.HotelRoomId == hotelRoomId)
            ?? throw new KeyNotFoundException("GROUP_ROOM_ASSIGNMENT_NOT_FOUND");
        if (assignment.IsCheckedIn || assignment.IsCheckedOut)
            throw new InvalidOperationException("GROUP_ROOM_ALREADY_USED");
        group.RemoveRoom(hotelRoomId);
        db.FrontOfficeGroupRoomAssignments.Remove(assignment);
        persistence.StageAudit(new FrontOfficeAuditIntent(
            "GroupRoomUnassigned",
            nameof(GroupRoomAssignment),
            assignment.GroupRoomAssignmentId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupReservationId"] = groupId.ToString("D"),
                ["HotelRoomId"] = hotelRoomId.ToString("D")
            }));
        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_ROOM_UNASSIGN_FAILED");
    }

    private async Task CommitAsync(string action, GroupRoomAssignment assignment, Guid propertyId, CancellationToken ct)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(GroupRoomAssignment),
            assignment.GroupRoomAssignmentId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupReservationId"] = assignment.GroupReservationId.ToString("D"),
                ["HotelRoomId"] = assignment.HotelRoomId.ToString("D"),
                ["RoomNumber"] = assignment.RoomNumber
            }));
        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_ROOM_ASSIGN_FAILED");
    }

    private static GroupRoomAssignmentDto Map(GroupRoomAssignment assignment) => new(
        assignment.GroupRoomAssignmentId,
        assignment.GroupReservationId,
        assignment.PropertyId,
        assignment.HotelRoomId,
        assignment.RoomNumber,
        assignment.RoomType,
        assignment.GuestId,
        assignment.GuestName,
        assignment.Adults,
        assignment.Children,
        assignment.IsCheckedIn,
        assignment.IsCheckedOut);
}
