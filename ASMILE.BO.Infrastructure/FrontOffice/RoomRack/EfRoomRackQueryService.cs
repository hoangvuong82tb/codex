using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.RoomRack;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.RoomRack;

public sealed class EfRoomRackQueryService(AppDbContext db, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService auth, IAvailabilityEngine availability) : IRoomRackQueryService
{
    public async Task<Result<PageResult<RoomRackDto>>> GetRoomRackAsync(GetRoomRackQuery q, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.RoomRack.View")) return Result<PageResult<RoomRackDto>>.Failure(Error.Authorization("ROOM_RACK_PERMISSION_DENIED", ""));
        var cx = ctx.Current; if (cx is null) return Result<PageResult<RoomRackDto>>.Failure(Error.Authorization("ROOM_RACK_CONTEXT_REQUIRED", ""));
        var pid = cx.Property.PropertyId.Value;
        var businessDate = cx.BusinessDate.BusinessDate;

        var rooms = await availability.GetRoomAvailabilityAsync(pid, businessDate, ct);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLowerInvariant();
            rooms = rooms.Where(r => r.RoomNumber.ToLowerInvariant().Contains(s) || (r.GuestName?.ToLowerInvariant().Contains(s) ?? false)).ToList();
        }
        if (q.Filter is not null)
        {
            var f = q.Filter;
            if (!string.IsNullOrWhiteSpace(f.Building)) rooms = rooms.Where(r => r.Building == f.Building).ToList();
            if (!string.IsNullOrWhiteSpace(f.Floor)) rooms = rooms.Where(r => r.Floor == f.Floor).ToList();
            if (!string.IsNullOrWhiteSpace(f.Occupancy)) rooms = rooms.Where(r => StateToOccupancy(r.State) == f.Occupancy).ToList();
            if (!string.IsNullOrWhiteSpace(f.HousekeepingStatus)) rooms = rooms.Where(r => StateToHousekeeping(r.State) == f.HousekeepingStatus).ToList();
            if (f.IsVip == true) rooms = rooms.Where(r => r.IsVip).ToList();
        }

        var result = rooms.Select(r => ToDto(r)).ToList();
        return Result<PageResult<RoomRackDto>>.Success(new PageResult<RoomRackDto>(result, result.Count, q.Page.Page, q.Page.PageSize));
    }

    public async Task<Result<IReadOnlyList<RoomRackAlertDto>>> GetAlertsAsync(GetRoomRackAlertsQuery q, CancellationToken ct)
    {
        var cx = ctx.Current; if (cx is null) return Result<IReadOnlyList<RoomRackAlertDto>>.Failure(Error.Authorization("ROOM_RACK_CONTEXT_REQUIRED", ""));
        var pid = cx.Property.PropertyId.Value;
        var businessDate = cx.BusinessDate.BusinessDate;
        var rooms = await availability.GetRoomAvailabilityAsync(pid, businessDate, ct);

        var alerts = new List<RoomRackAlertDto>();
        var dirty = rooms.Count(r => r.State is RoomAvailabilityState.VacantDirty or RoomAvailabilityState.OccupiedDirty);
        if (dirty > 0) alerts.Add(new RoomRackAlertDto("DirtyRoom", "Warning", $"{dirty} rooms", $"{dirty} rooms need cleaning", "OpenHousekeeping", DateTime.UtcNow));

        var inHouse = rooms.Count(r => r.State is RoomAvailabilityState.OccupiedClean or RoomAvailabilityState.OccupiedDirty);
        alerts.Add(new RoomRackAlertDto("Occupancy", "Info", $"{inHouse} in-house", $"Current occupancy: {inHouse} rooms", null, DateTime.UtcNow));

        var arriving = rooms.Count(r => r.State == RoomAvailabilityState.ExpectedArrival);
        if (arriving > 0) alerts.Add(new RoomRackAlertDto("Arrival", "Info", $"{arriving} arriving", $"{arriving} guests arriving today", null, DateTime.UtcNow));

        var departing = rooms.Count(r => r.State is RoomAvailabilityState.ExpectedDeparture or RoomAvailabilityState.DueOut);
        if (departing > 0) alerts.Add(new RoomRackAlertDto("Departure", "Info", $"{departing} departing", $"{departing} guests departing today", null, DateTime.UtcNow));

        return Result<IReadOnlyList<RoomRackAlertDto>>.Success(alerts);
    }

    private static RoomRackDto ToDto(RoomAvailability r) => new(
        r.HotelRoomId, r.RoomNumber, r.RoomTypeName, r.Floor, r.Building, null,
        StateToOccupancy(r.State), StateToHousekeeping(r.State), StateToInventory(r.State), "Normal",
        r.GuestName, null, r.IsVip, null,
        r.ArrivalDate, r.DepartureDate, r.CurrentAdults, r.CurrentChildren,
        null, null, null, null, null, false, false, false, false);

    private static string StateToOccupancy(RoomAvailabilityState s) => s switch
    {
        RoomAvailabilityState.OccupiedClean or RoomAvailabilityState.OccupiedDirty or RoomAvailabilityState.DueOut => "Occupied",
        RoomAvailabilityState.Reserved or RoomAvailabilityState.ExpectedArrival => "Reserved",
        _ => "Vacant"
    };

    private static string StateToHousekeeping(RoomAvailabilityState s) => s switch
    {
        RoomAvailabilityState.VacantClean => "Clean",
        RoomAvailabilityState.VacantDirty => "Dirty",
        RoomAvailabilityState.OccupiedClean => "Clean",
        RoomAvailabilityState.OccupiedDirty => "Dirty",
        RoomAvailabilityState.OutOfOrder => "OutOfOrder",
        RoomAvailabilityState.OutOfService => "OutOfService",
        RoomAvailabilityState.Blocked => "Blocked",
        _ => "Clean"
    };

    private static string StateToInventory(RoomAvailabilityState s) => s switch
    {
        RoomAvailabilityState.OutOfOrder => "OutOfOrder",
        RoomAvailabilityState.OutOfService => "OutOfService",
        _ => "Available"
    };
}
