using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

/// <summary>
/// Read-only group dashboard backed by the persisted group reservation and
/// room-assignment tables. Room blocks are not persisted in the current
/// schema, so the dashboard exposes a group-level block projection until the
/// dedicated room-block aggregate is introduced.
/// </summary>
public sealed class EfGroupDashboardService(AppDbContext db) : IGroupDashboardService, IGroupReservationQueryService
{
    public async Task<IReadOnlyList<GroupReservationSummaryDto>> GetGroupsAsync(Guid propertyId, CancellationToken ct)
    {
        var groups = await db.FrontOfficeGroupReservations
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(ct);

        var groupIds = groups.Select(x => x.GroupReservationId).ToArray();
        var assignments = await db.FrontOfficeGroupRoomAssignments
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.GroupReservationId))
            .OrderBy(x => x.RoomNumber)
            .ToListAsync(ct);

        return groups.Select(group =>
        {
            var groupAssignments = assignments.Where(x => x.GroupReservationId == group.GroupReservationId).ToList();
            return new GroupReservationSummaryDto(
                group.GroupReservationId,
                group.PropertyId,
                group.GroupCode,
                group.GroupName,
                group.CompanyName,
                group.AgencyName,
                group.ContactPerson,
                group.ContactPhone,
                group.ContactEmail,
                group.ArrivalDate,
                group.DepartureDate,
                group.ExpectedRooms,
                group.ConfirmedRooms,
                group.TotalAdults,
                group.TotalChildren,
                groupAssignments.Where(x => x.IsCheckedIn).Sum(x => x.Adults + x.Children),
                group.Status,
                group.Source,
                group.SalesManager,
                group.MarketSegment,
                group.IsVip,
                group.Routing,
                group.TotalDeposit,
                group.TotalRevenue,
                group.OutstandingBalance,
                group.CreatedAtUtc,
                groupAssignments.Select(x => new RoomingListItemDto(
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
                    null)).ToList());
        }).ToList();
    }

    public async Task<GroupDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var groups = await db.FrontOfficeGroupReservations
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .Select(x => new GroupSnapshot(
                x.GroupReservationId,
                x.GroupCode,
                x.GroupName,
                x.ArrivalDate,
                x.DepartureDate,
                x.ExpectedRooms,
                x.ConfirmedRooms,
                x.Status,
                x.TotalRevenue,
                x.OutstandingBalance,
                x.CreatedAtUtc))
            .ToListAsync(ct);

        if (groups.Count == 0)
            return new GroupDashboardDto(0, 0, 0, 0m, 0, 0, 0m, 0m, [], []);

        var groupIds = groups.Select(x => x.GroupReservationId).ToArray();
        var assignments = await db.FrontOfficeGroupRoomAssignments
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.GroupReservationId))
            .Select(x => new AssignmentSnapshot(x.GroupReservationId, x.IsCheckedIn))
            .ToListAsync(ct);

        var pickedUpByGroup = assignments
            .GroupBy(x => x.GroupReservationId)
            .ToDictionary(x => x.Key, x => x.Count(a => a.IsCheckedIn));

        var activeGroups = groups
            .Where(x => x.Status is not (GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut))
            .ToList();

        var projections = activeGroups
            .Select(x => ToBlockProjection(x, pickedUpByGroup.GetValueOrDefault(x.GroupReservationId), today))
            .ToList();

        var totalRoomsBlocked = projections.Sum(x => x.RoomsBlocked);
        var totalRoomsPickedUp = projections.Sum(x => x.RoomsPickedUp);
        var overallPickup = totalRoomsBlocked == 0
            ? 0m
            : Math.Round(totalRoomsPickedUp * 100m / totalRoomsBlocked, 2);

        var atRisk = projections
            .Where(x => x.RoomsRemaining > 0 && x.ArrivalDate <= today.AddDays(7))
            .OrderByDescending(x => x.RoomsRemaining)
            .ThenBy(x => x.ArrivalDate)
            .Take(10)
            .Select(x => ToPickupProjection(x, today))
            .ToList();

        return new GroupDashboardDto(
            activeGroups.Count,
            totalRoomsBlocked,
            totalRoomsPickedUp,
            overallPickup,
            activeGroups.Count(x => x.ArrivalDate == today),
            activeGroups.Count(x => x.DepartureDate == today),
            activeGroups.Sum(x => x.TotalRevenue),
            activeGroups.Sum(x => x.OutstandingBalance),
            projections.OrderByDescending(x => x.RoomsBlocked).Take(10).ToList(),
            atRisk);
    }

    private static RoomBlockDto ToBlockProjection(GroupSnapshot group, int pickedUp, DateOnly today)
    {
        var blocked = Math.Max(group.ConfirmedRooms, 0);
        var remaining = Math.Max(group.ExpectedRooms - blocked, 0);
        return new RoomBlockDto(
            group.GroupReservationId,
            group.GroupReservationId,
            group.GroupCode,
            group.GroupName,
            Guid.Empty,
            "Group allocation",
            Math.Max(group.ExpectedRooms, 0),
            blocked,
            pickedUp,
            remaining,
            group.ArrivalDate,
            group.DepartureDate,
            null,
            CutoffType.None,
            MapBlockStatus(group.Status, group.ArrivalDate, today),
            null,
            group.CreatedAtUtc);
    }

    private static PickupAnalysisDto ToPickupProjection(RoomBlockDto block, DateOnly today)
    {
        var pickupPercent = block.RoomsBlocked == 0
            ? 0m
            : Math.Round(block.RoomsPickedUp * 100m / block.RoomsBlocked, 2);
        var daysUntilArrival = block.ArrivalDate.DayNumber - today.DayNumber;
        var trend = block.RoomsRemaining > 0 && daysUntilArrival <= 7 ? "AtRisk" : "OnTrack";
        return new PickupAnalysisDto(
            block.BlockId,
            today,
            block.RoomsBlocked,
            block.RoomsPickedUp,
            pickupPercent,
            block.RoomsRemaining,
            daysUntilArrival,
            trend);
    }

    private static BlockStatus MapBlockStatus(GroupReservationStatus status, DateOnly arrivalDate, DateOnly today) =>
        status switch
        {
            GroupReservationStatus.Draft or GroupReservationStatus.Tentative => BlockStatus.Tentative,
            GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut => BlockStatus.Released,
            _ when arrivalDate < today => BlockStatus.Expired,
            _ => BlockStatus.Confirmed
        };

    private sealed record GroupSnapshot(
        Guid GroupReservationId,
        string GroupCode,
        string GroupName,
        DateOnly ArrivalDate,
        DateOnly DepartureDate,
        int ExpectedRooms,
        int ConfirmedRooms,
        GroupReservationStatus Status,
        decimal TotalRevenue,
        decimal OutstandingBalance,
        DateTime CreatedAtUtc);

    private sealed record AssignmentSnapshot(Guid GroupReservationId, bool IsCheckedIn);
}
