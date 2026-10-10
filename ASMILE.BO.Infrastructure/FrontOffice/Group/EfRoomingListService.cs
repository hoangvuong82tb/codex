using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

/// <summary>
/// Reads the group rooming list from the Front Office EF model.
///
/// The legacy GroupEngine implementation returned a fabricated row and silently
/// acknowledged every write. Reads and writes here are backed by the persisted
/// group-room assignments and share one validation/audit commit for imports.
/// </summary>
public sealed class EfRoomingListService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IRoomingListService
{
    public async Task<IReadOnlyList<RoomingListItemDto>> GetRoomingListAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return await QueryAssignments(groupId).ToListAsync(ct);
    }

    public Task<IReadOnlyList<RoomingListItemDto>> ExportAsync(
        Guid groupId,
        CancellationToken ct)
        => GetRoomingListAsync(groupId, ct);

    public async Task<RoomingListImportResult> ImportAsync(
        Guid groupId,
        IReadOnlyList<RoomingListItemDto> items,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(items);
        ct.ThrowIfCancellationRequested();
        if (groupId == Guid.Empty)
            throw new ArgumentException("GROUP_REQUIRED", nameof(groupId));
        if (items.Count == 0)
            return new RoomingListImportResult(0, 0, 0, 0, Array.Empty<string>());

        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var groupExists = await db.FrontOfficeGroupReservations
            .AnyAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct);
        if (!groupExists)
            throw new KeyNotFoundException("GROUP_NOT_FOUND");

        var assignmentIds = items.Select(x => x.AssignmentId).ToArray();
        var details = new List<string>();
        if (assignmentIds.Any(x => x == Guid.Empty))
            details.Add("Có dòng Rooming List thiếu AssignmentId thật.");
        if (assignmentIds.Length != assignmentIds.Distinct().Count())
            details.Add("Rooming List có AssignmentId bị trùng.");

        var assignments = await db.FrontOfficeGroupRoomAssignments
            .Where(x => x.PropertyId == propertyId && x.GroupReservationId == groupId && assignmentIds.Contains(x.GroupRoomAssignmentId))
            .ToDictionaryAsync(x => x.GroupRoomAssignmentId, ct);
        if (assignments.Count != items.Count)
            details.Add("Có dòng không còn tồn tại trong đoàn hoặc không thuộc property hiện tại.");

        var guestIds = items.Where(x => x.GuestId is not null).Select(x => x.GuestId!.Value).Distinct().ToArray();
        var validGuestIds = await db.GuestProfiles
            .Where(x => x.PropertyId == propertyId && guestIds.Contains(x.GuestProfileId))
            .Select(x => x.GuestProfileId)
            .ToListAsync(ct);
        var validGuestSet = validGuestIds.ToHashSet();

        foreach (var item in items)
        {
            if (!assignments.TryGetValue(item.AssignmentId, out var assignment))
                continue;
            if (assignment.IsCheckedIn || assignment.IsCheckedOut)
                details.Add($"Phòng {assignment.RoomNumber} đã phát sinh check-in/check-out, không thể sửa Rooming List.");
            if (assignment.HotelRoomId != item.HotelRoomId
                || !string.Equals(assignment.RoomNumber, item.RoomNumber, StringComparison.Ordinal)
                || !string.Equals(assignment.RoomType, item.RoomType, StringComparison.Ordinal))
                details.Add($"Dòng phòng {item.RoomNumber} không khớp dữ liệu phân phòng thật.");
            if (item.GuestId is not null && !validGuestSet.Contains(item.GuestId.Value))
                details.Add($"GuestId {item.GuestId:D} không tồn tại trong property hiện tại.");
            if (item.Adults < 0 || item.Children < 0 || item.Adults + item.Children == 0)
                details.Add($"Phòng {item.RoomNumber} có số lượng khách không hợp lệ.");
            if (item.GuestId is not null && string.IsNullOrWhiteSpace(item.GuestName))
                details.Add($"Phòng {item.RoomNumber} đã chọn GuestId nhưng thiếu tên khách.");
        }

        if (details.Count > 0)
            return new RoomingListImportResult(items.Count, 0, items.Count, details.Count, details.Distinct().ToArray());

        foreach (var item in items)
        {
            var assignment = assignments[item.AssignmentId];
            assignment.UpdateGuest(item.GuestId, item.GuestName);
            assignment.UpdateOccupancy(item.Adults, item.Children);
            assignment.SetVip(item.IsVip);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "GroupRoomingListImported",
                nameof(GroupRoomAssignment),
                assignment.GroupRoomAssignmentId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["GroupReservationId"] = groupId.ToString("D"),
                    ["HotelRoomId"] = assignment.HotelRoomId.ToString("D"),
                    ["RoomNumber"] = assignment.RoomNumber
                }));
        }

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_ROOMING_LIST_IMPORT_FAILED");

        return new RoomingListImportResult(items.Count, items.Count, 0, 0, Array.Empty<string>());
    }

    public async Task AssignGuestAsync(Guid assignmentId, Guid guestId, string guestName, CancellationToken ct)
    {
        var context = RequireContext();
        var guestExists = await db.GuestProfiles.AnyAsync(
            x => x.PropertyId == context.Property.PropertyId.Value && x.GuestProfileId == guestId,
            ct);
        if (!guestExists) throw new KeyNotFoundException("GROUP_GUEST_NOT_FOUND");
        var assignment = await LoadAssignmentAsync(assignmentId, context.Property.PropertyId.Value, ct);
        assignment.AssignGuest(guestId, guestName);
        await CommitAsync("GroupGuestAssigned", assignment, context.Property.PropertyId.Value, ct);
    }

    public async Task MarkArrivalAsync(Guid assignmentId, DateTime checkInAt, CancellationToken ct)
    {
        var context = RequireContext();
        var assignment = await LoadAssignmentAsync(assignmentId, context.Property.PropertyId.Value, ct);
        if (checkInAt > clock.UtcNow.AddMinutes(5)) throw new ArgumentException("GROUP_INVALID_CHECKIN_TIME");
        if (assignment.GroupReservation is null) throw new InvalidOperationException("GROUP_NOT_FOUND");
        assignment.MarkCheckedIn();
        assignment.GroupReservation.MarkCheckedIn(checkInAt);
        await CommitAsync("GroupGuestCheckedIn", assignment, context.Property.PropertyId.Value, ct);
    }

    public async Task MarkDepartureAsync(Guid assignmentId, DateTime checkOutAt, CancellationToken ct)
    {
        var context = RequireContext();
        var assignment = await LoadAssignmentAsync(assignmentId, context.Property.PropertyId.Value, ct);
        if (checkOutAt > clock.UtcNow.AddMinutes(5)) throw new ArgumentException("GROUP_INVALID_CHECKOUT_TIME");
        assignment.MarkCheckedOut();
        if (assignment.GroupReservation is not null)
        {
            var remaining = await db.FrontOfficeGroupRoomAssignments
                .CountAsync(x => x.GroupReservationId == assignment.GroupReservationId && !x.IsCheckedOut && x.GroupRoomAssignmentId != assignmentId, ct);
            if (remaining == 0) assignment.GroupReservation.MarkCheckedOut(checkOutAt);
        }
        await CommitAsync("GroupGuestCheckedOut", assignment, context.Property.PropertyId.Value, ct);
    }

    private IQueryable<RoomingListItemDto> QueryAssignments(Guid groupId)
        => db.FrontOfficeGroupRoomAssignments
            .AsNoTracking()
            .Where(x => x.GroupReservationId == groupId)
            .OrderBy(x => x.RoomNumber)
            .Select(x => new RoomingListItemDto(
                x.GroupRoomAssignmentId,
                x.HotelRoomId,
                x.RoomNumber,
                x.RoomType,
                x.GuestId,
                x.GuestName,
                x.Adults,
                x.Children,
                x.IsVip,
                x.IsCheckedIn,
                x.IsCheckedOut,
                x.ReservationId,
                null,
                null));

    private async Task<GroupRoomAssignment> LoadAssignmentAsync(Guid assignmentId, Guid propertyId, CancellationToken ct)
        => await db.FrontOfficeGroupRoomAssignments
            .Include(x => x.GroupReservation)
            .SingleOrDefaultAsync(x => x.GroupRoomAssignmentId == assignmentId && x.PropertyId == propertyId, ct)
        ?? throw new KeyNotFoundException("GROUP_ROOM_ASSIGNMENT_NOT_FOUND");

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

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
                ["RoomNumber"] = assignment.RoomNumber
            }));
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "GROUP_ROOMING_LIST_UPDATE_FAILED");
    }

}
