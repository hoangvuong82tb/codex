using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

/// <summary>
/// Room blocks are persisted commitments by group and room type. They are
/// intentionally separate from individual room assignments: a block reserves
/// quantity for planning, while assignment later binds physical rooms.
/// </summary>
public sealed class EfRoomBlockService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IRoomBlockService
{
    public async Task<RoomBlockDto> CreateBlockAsync(Guid groupId, Guid roomTypeId, int rooms, DateOnly arrival, DateOnly departure, DateOnly? cutoff, decimal? negotiatedRate, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations.SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");
        if (group.Status is GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut)
            throw new InvalidOperationException("GROUP_ROOM_BLOCK_GROUP_NOT_ACTIVE");

        var roomType = await db.HotelRoomTypes.SingleOrDefaultAsync(x => x.HotelRoomTypeId == roomTypeId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("HOTEL_ROOM_TYPE_NOT_FOUND");
        if (!roomType.IsActive) throw new InvalidOperationException("HOTEL_ROOM_TYPE_INACTIVE");
        ValidateDates(group, arrival, departure);

        var now = clock.UtcNow;
        var block = GroupRoomBlock.Create(propertyId, groupId, roomTypeId, rooms, arrival, departure, cutoff, negotiatedRate, now, context.Actor.ActorId);
        db.FrontOfficeGroupRoomBlocks.Add(block);
        StageAudit("GroupRoomBlockCreated", block, propertyId, roomType.Code);
        await CommitAsync(ct, "GROUP_ROOM_BLOCK_CREATE_FAILED");
        return await FindBlockDtoAsync(propertyId, block.GroupRoomBlockId, arrival, ct)
            ?? throw new InvalidOperationException("GROUP_ROOM_BLOCK_READ_AFTER_WRITE_FAILED");
    }

    public async Task<RoomBlockDto> ModifyBlockAsync(Guid blockId, int? additionalRooms, DateOnly? newCutoff, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var block = await db.FrontOfficeGroupRoomBlocks.SingleOrDefaultAsync(x => x.GroupRoomBlockId == blockId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_ROOM_BLOCK_NOT_FOUND");
        var now = clock.UtcNow;
        block.Modify(additionalRooms, newCutoff, now, context.Actor.ActorId);
        StageAudit("GroupRoomBlockModified", block, propertyId);
        await CommitAsync(ct, "GROUP_ROOM_BLOCK_MODIFY_FAILED");
        return await FindBlockDtoAsync(propertyId, blockId, DateOnly.FromDateTime(now), ct)
            ?? throw new InvalidOperationException("GROUP_ROOM_BLOCK_READ_AFTER_WRITE_FAILED");
    }

    public async Task<RoomBlockDto> ReleaseRoomsAsync(Guid blockId, int count, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var block = await db.FrontOfficeGroupRoomBlocks.SingleOrDefaultAsync(x => x.GroupRoomBlockId == blockId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_ROOM_BLOCK_NOT_FOUND");
        var now = clock.UtcNow;
        block.Release(count, now, context.Actor.ActorId);
        StageAudit("GroupRoomBlockReleased", block, propertyId);
        await CommitAsync(ct, "GROUP_ROOM_BLOCK_RELEASE_FAILED");
        return await FindBlockDtoAsync(propertyId, blockId, DateOnly.FromDateTime(now), ct)
            ?? throw new InvalidOperationException("GROUP_ROOM_BLOCK_READ_AFTER_WRITE_FAILED");
    }

    public async Task<PickupAnalysisDto> GetPickupAsync(Guid blockId, DateOnly? asOf, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var today = asOf ?? DateOnly.FromDateTime(clock.UtcNow);
        var row = await LoadRows(propertyId, blockId, includeInactive: true).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("GROUP_ROOM_BLOCK_NOT_FOUND");
        var pickedUp = await GetPickupCountAsync(row, ct);
        return ToPickupProjection(ToBlockProjection(row, pickedUp, today), today);
    }

    public async Task<RoomBlockDashboardDto> GetBlockDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var rows = await LoadRows(propertyId, null).ToListAsync(ct);
        var pickedUp = await GetPickupCountsAsync(rows, ct);
        return BuildDashboard(rows.Select(row => ToBlockProjection(row, pickedUp.GetValueOrDefault(row.BlockId), today)).ToList(), today);
    }

    public async Task<IReadOnlyList<RoomBlockDto>> GetActiveBlocksAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var rows = await LoadRows(propertyId, null).ToListAsync(ct);
        var pickedUp = await GetPickupCountsAsync(rows, ct);
        return rows.Select(row => ToBlockProjection(row, pickedUp.GetValueOrDefault(row.BlockId), today))
            .OrderBy(x => x.ArrivalDate).ThenBy(x => x.GroupCode).ToList();
    }

    public async Task<IReadOnlyList<PickupAnalysisDto>> GetPickupTrendsAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var rows = await LoadRows(propertyId, null).ToListAsync(ct);
        var pickedUp = await GetPickupCountsAsync(rows, ct);
        return rows.Select(row => ToPickupProjection(ToBlockProjection(row, pickedUp.GetValueOrDefault(row.BlockId), today), today))
            .OrderBy(x => x.Date).ThenByDescending(x => x.Remaining).ToList();
    }

    private IQueryable<BlockRow> LoadRows(Guid propertyId, Guid? blockId, bool includeInactive = false)
    {
        var query = from block in db.FrontOfficeGroupRoomBlocks.AsNoTracking()
                    join grp in db.FrontOfficeGroupReservations.AsNoTracking() on block.GroupReservationId equals grp.GroupReservationId
                    join roomType in db.HotelRoomTypes.AsNoTracking() on block.HotelRoomTypeId equals roomType.HotelRoomTypeId
                    where block.PropertyId == propertyId && grp.PropertyId == propertyId && roomType.PropertyId == propertyId
                    where includeInactive || (grp.Status != GroupReservationStatus.Cancelled && grp.Status != GroupReservationStatus.Closed && grp.Status != GroupReservationStatus.CheckedOut)
                    where includeInactive || (block.Status != GroupRoomBlockStatus.Released && block.Status != GroupRoomBlockStatus.Expired)
                    select new BlockRow(
                        block.GroupRoomBlockId, block.PropertyId, block.GroupReservationId, grp.GroupCode, grp.GroupName,
                        roomType.HotelRoomTypeId, roomType.Code, roomType.Name, block.RequestedRooms, block.BlockedRooms,
                        block.PickedUpRooms, block.ArrivalDate, block.DepartureDate, block.CutoffDate, block.Status,
                        block.NegotiatedRate, block.CreatedAtUtc);
        return blockId is null ? query : query.Where(x => x.BlockId == blockId.Value);
    }

    private async Task<int> GetPickupCountAsync(BlockRow row, CancellationToken ct)
    {
        var count = await db.FrontOfficeGroupRoomAssignments.AsNoTracking()
            .Where(x => x.GroupReservationId == row.GroupId && x.RoomType == row.RoomTypeCode)
            .Where(x => x.IsCheckedIn || x.ReservationId != null).CountAsync(ct);
        return Math.Max(row.PickedUpRooms, count);
    }

    private async Task<Dictionary<Guid, int>> GetPickupCountsAsync(IReadOnlyCollection<BlockRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return [];
        var groupIds = rows.Select(x => x.GroupId).Distinct().ToArray();
        var roomTypeCodes = rows.Select(x => x.RoomTypeCode).Distinct().ToArray();
        var assignments = await db.FrontOfficeGroupRoomAssignments.AsNoTracking()
            .Where(x => groupIds.Contains(x.GroupReservationId) && roomTypeCodes.Contains(x.RoomType))
            .Where(x => x.IsCheckedIn || x.ReservationId != null)
            .Select(x => new { x.GroupReservationId, x.RoomType }).ToListAsync(ct);
        return rows.ToDictionary(row => row.BlockId, row => Math.Max(row.PickedUpRooms, assignments.Count(x => x.GroupReservationId == row.GroupId && x.RoomType == row.RoomTypeCode)));
    }

    private async Task<RoomBlockDto?> FindBlockDtoAsync(Guid propertyId, Guid blockId, DateOnly asOf, CancellationToken ct)
    {
        var row = await LoadRows(propertyId, blockId, includeInactive: true).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        return ToBlockProjection(row, await GetPickupCountAsync(row, ct), asOf);
    }

    private static RoomBlockDashboardDto BuildDashboard(IReadOnlyList<RoomBlockDto> blocks, DateOnly today)
    {
        var totalBlocked = blocks.Sum(x => x.RoomsBlocked);
        var totalPickedUp = blocks.Sum(x => x.RoomsPickedUp);
        var pickup = totalBlocked == 0 ? 0m : Math.Round(totalPickedUp * 100m / totalBlocked, 2);
        var trends = blocks.Select(x => ToPickupProjection(x, today)).ToList();
        var cutoffs = blocks.Count(x => x.CutoffDate is not null && x.CutoffDate.Value.DayNumber >= today.DayNumber && x.CutoffDate.Value.DayNumber - today.DayNumber <= 7);
        return new RoomBlockDashboardDto(blocks.Count, blocks.Count(x => x.Status == BlockStatus.Confirmed), totalBlocked, totalPickedUp, pickup, cutoffs, trends.Count(x => x.Trend == "AtRisk"), blocks.OrderByDescending(x => x.RoomsBlocked).ToList(), trends);
    }

    private static RoomBlockDto ToBlockProjection(BlockRow row, int pickedUp, DateOnly today)
    {
        var blocked = Math.Max(row.BlockedRooms, 0);
        var picked = Math.Min(Math.Max(pickedUp, 0), blocked);
        return new RoomBlockDto(row.BlockId, row.GroupId, row.GroupCode, row.GroupName, row.RoomTypeId, row.RoomTypeName,
            Math.Max(row.RequestedRooms, 0), blocked, picked, Math.Max(blocked - picked, 0), row.ArrivalDate, row.DepartureDate,
            row.CutoffDate, row.CutoffDate is null ? CutoffType.None : CutoffType.Automatic, MapBlockStatus(row.Status, row.ArrivalDate, today), row.NegotiatedRate, row.CreatedAtUtc);
    }

    private static PickupAnalysisDto ToPickupProjection(RoomBlockDto block, DateOnly today)
    {
        var pickupPercent = block.RoomsBlocked == 0 ? 0m : Math.Round(block.RoomsPickedUp * 100m / block.RoomsBlocked, 2);
        var daysUntilArrival = block.ArrivalDate.DayNumber - today.DayNumber;
        var trend = block.RoomsRemaining > 0 && daysUntilArrival <= 7 ? "AtRisk" : pickupPercent >= 75m ? "Ahead" : "OnTrack";
        return new PickupAnalysisDto(block.BlockId, today, block.RoomsBlocked, block.RoomsPickedUp, pickupPercent, block.RoomsRemaining, daysUntilArrival, trend);
    }

    private static BlockStatus MapBlockStatus(GroupRoomBlockStatus status, DateOnly arrivalDate, DateOnly today) => status switch
    {
        GroupRoomBlockStatus.Tentative => BlockStatus.Tentative,
        GroupRoomBlockStatus.Released => BlockStatus.Released,
        GroupRoomBlockStatus.Expired => BlockStatus.Expired,
        _ when arrivalDate < today => BlockStatus.Expired,
        _ => BlockStatus.Confirmed
    };

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (RequireContext().Property.PropertyId.Value != propertyId) throw new InvalidOperationException("FRONT_OFFICE_PROPERTY_MISMATCH"); }
    private static void ValidateDates(GroupReservation group, DateOnly arrival, DateOnly departure) { if (arrival != group.ArrivalDate || departure != group.DepartureDate) throw new ArgumentException("GROUP_ROOM_BLOCK_DATES_MUST_MATCH_GROUP"); }

    private void StageAudit(string action, GroupRoomBlock block, Guid propertyId, string? roomTypeCode = null)
        => persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(GroupRoomBlock), block.GroupRoomBlockId.ToString("D"), "Succeeded", Metadata: new Dictionary<string, string>
        {
            ["PropertyId"] = propertyId.ToString("D"), ["GroupReservationId"] = block.GroupReservationId.ToString("D"),
            ["RoomsBlocked"] = block.BlockedRooms.ToString(), ["Status"] = block.Status.ToString(), ["RoomTypeCode"] = roomTypeCode ?? string.Empty
        }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure) throw new InvalidOperationException(saved.Error?.Code ?? errorCode);
    }

    private sealed record BlockRow(Guid BlockId, Guid PropertyId, Guid GroupId, string GroupCode, string GroupName,
        Guid RoomTypeId, string RoomTypeCode, string RoomTypeName, int RequestedRooms, int BlockedRooms, int PickedUpRooms,
        DateOnly ArrivalDate, DateOnly DepartureDate, DateOnly? CutoffDate, GroupRoomBlockStatus Status,
        decimal? NegotiatedRate, DateTime CreatedAtUtc);
}
