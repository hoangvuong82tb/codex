using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;
using ReservationStatus = ASMILE.BO.Core.FrontOffice.Reservation.ReservationStatus;

namespace ASMILE.BO.Infrastructure.FrontOffice.Availability;

public sealed class AvailabilityEngine(AppDbContext db) : IAvailabilityEngine
{
    public async Task<AvailabilityResult> CheckAvailabilityAsync(AvailabilityRequest r, CancellationToken ct)
    {
        var rooms = await db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == r.PropertyId && x.IsActive && x.OperationalStatus == HotelRoomOperationalStatus.Available)
            .Where(x => r.RoomTypeId == null || x.HotelRoomTypeId == r.RoomTypeId.Value)
            .ToListAsync(ct);

        if (rooms.Count == 0) return new AvailabilityResult(false, 0, "RESERVATION_NO_ROOMS_AVAILABLE", []);

        var roomIds = rooms.Select(x => x.HotelRoomId).ToHashSet();
        var conflictingStays = await db.HotelStays.AsNoTracking()
            .Where(s => s.PropertyId == r.PropertyId && roomIds.Contains(s.RoomId)
                && (s.Status == HotelStayStatus.PendingArrival || s.Status == HotelStayStatus.InHouse)
                && s.ScheduledArrivalDate < r.DepartureDate && s.ScheduledDepartureDate > r.ArrivalDate)
            .Select(s => s.RoomId).ToListAsync(ct);

        var conflictingReservations = await db.FrontOfficeReservations.AsNoTracking()
            .Where(res => res.PropertyId == r.PropertyId && roomIds.Contains(res.AssignedRoomId!.Value)
                && (res.Status == ReservationStatus.Confirmed || res.Status == ReservationStatus.Guaranteed || res.Status == ReservationStatus.CheckedIn)
                && res.ArrivalDate < r.DepartureDate && res.DepartureDate > r.ArrivalDate)
            .Select(res => res.AssignedRoomId!.Value).ToListAsync(ct);

        var unavailableIds = conflictingStays.Concat(conflictingReservations).ToHashSet();
        var available = rooms.Where(x => !unavailableIds.Contains(x.HotelRoomId)).ToList();

        if (available.Count < r.RoomCount)
            return new AvailabilityResult(false, available.Count, available.Count == 0 ? "RESERVATION_NO_AVAILABILITY" : $"RESERVATION_INSUFFICIENT_ROOMS: need {r.RoomCount}, have {available.Count}", []);

        var hkStates = await db.Set<RoomHousekeepingState>().AsNoTracking()
            .Where(x => x.PropertyId == r.PropertyId && available.Select(a => a.HotelRoomId).Contains(x.HotelRoomId))
            .ToDictionaryAsync(x => x.HotelRoomId, ct);

        var roomTypes = await db.HotelRoomTypes.AsNoTracking()
            .Where(t => t.PropertyId == r.PropertyId && t.IsActive)
            .ToDictionaryAsync(t => t.HotelRoomTypeId, ct);

        var result = available.Select(room =>
        {
            var hk = hkStates.GetValueOrDefault(room.HotelRoomId);
            var rt = roomTypes.GetValueOrDefault(room.HotelRoomTypeId);
            return new RoomAvailability(room.HotelRoomId, room.RoomNumber, rt?.Name ?? "Unknown", room.Floor, room.Building,
                room.HotelRoomTypeId, hk is { Status: HousekeepingRoomStatus.VacantClean or HousekeepingRoomStatus.Inspected }
                    ? RoomAvailabilityState.VacantClean : RoomAvailabilityState.VacantDirty,
                rt?.MaximumAdultCapacity ?? 2, rt?.MaximumChildCapacity ?? 1, 0, 0, null, null, false, null, null, null, DateTime.UtcNow);
        }).ToList();

        return new AvailabilityResult(true, result.Count, null, result);
    }

    public async Task<IReadOnlyList<RoomAvailability>> GetRoomAvailabilityAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var rooms = await db.HotelRooms.AsNoTracking().Where(r => r.PropertyId == propertyId && r.IsActive).ToListAsync(ct);
        var roomIds = rooms.Select(r => r.HotelRoomId).ToHashSet();

        var hkStates = await db.Set<RoomHousekeepingState>().AsNoTracking().Where(x => x.PropertyId == propertyId).ToDictionaryAsync(x => x.HotelRoomId, ct);
        var roomTypes = await db.HotelRoomTypes.AsNoTracking().Where(t => t.PropertyId == propertyId && t.IsActive).ToDictionaryAsync(t => t.HotelRoomTypeId, ct);

        var stays = await db.HotelStays.AsNoTracking()
            .Where(s => s.PropertyId == propertyId && roomIds.Contains(s.RoomId) && (s.Status == HotelStayStatus.PendingArrival || s.Status == HotelStayStatus.InHouse))
            .ToListAsync(ct);

        var reservations = await db.FrontOfficeReservations.AsNoTracking()
            .Where(r => r.PropertyId == propertyId && r.AssignedRoomId != null
                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Guaranteed || r.Status == ReservationStatus.CheckedIn))
            .ToListAsync(ct);

        var result = new List<RoomAvailability>();
        foreach (var room in rooms)
        {
            var hk = hkStates.GetValueOrDefault(room.HotelRoomId);
            var rt = roomTypes.GetValueOrDefault(room.HotelRoomTypeId);
            var stay = stays.FirstOrDefault(s => s.RoomId == room.HotelRoomId);
            var reservation = reservations.FirstOrDefault(r => r.AssignedRoomId == room.HotelRoomId);

            var state = ComputeState(room, hk, stay, reservation, businessDate);
            result.Add(new RoomAvailability(room.HotelRoomId, room.RoomNumber, rt?.Name ?? "Unknown", room.Floor, room.Building,
                room.HotelRoomTypeId, state, rt?.MaximumAdultCapacity ?? 2, rt?.MaximumChildCapacity ?? 1,
                stay?.Adults ?? reservation?.AdultCount ?? 0, stay?.Children ?? reservation?.ChildCount ?? 0,
                stay?.HotelStayId, stay?.GuestNameSnapshot ?? reservation?.GuestNameSnapshot,
                reservation?.IsVip ?? false,
                stay?.ScheduledArrivalDate ?? reservation?.ArrivalDate,
                stay?.ScheduledDepartureDate ?? reservation?.DepartureDate,
                reservation?.ReservationNumber, room.UpdatedAtUtc));
        }
        return result;
    }

    private static RoomAvailabilityState ComputeState(HotelRoom room, RoomHousekeepingState? hk, HotelStay? stay, ReservationAggregate? reservation, DateOnly businessDate)
    {
        if (room.OperationalStatus == HotelRoomOperationalStatus.OutOfOrder) return RoomAvailabilityState.OutOfOrder;
        if (room.OperationalStatus == HotelRoomOperationalStatus.OutOfService) return RoomAvailabilityState.OutOfService;
        if (hk?.Status == HousekeepingRoomStatus.Blocked) return RoomAvailabilityState.Blocked;

        if (stay is not null)
        {
            if (stay.Status == HotelStayStatus.InHouse)
            {
                if (stay.ScheduledDepartureDate <= businessDate) return RoomAvailabilityState.DueOut;
                if (stay.ScheduledDepartureDate == businessDate.AddDays(1)) return RoomAvailabilityState.ExpectedDeparture;
                var isDirty = hk?.Status is HousekeepingRoomStatus.OccupiedDirty or HousekeepingRoomStatus.VacantDirty;
                return isDirty ? RoomAvailabilityState.OccupiedDirty : RoomAvailabilityState.OccupiedClean;
            }
            if (stay.Status == HotelStayStatus.PendingArrival)
            {
                if (stay.ScheduledArrivalDate == businessDate) return RoomAvailabilityState.ExpectedArrival;
                return RoomAvailabilityState.Reserved;
            }
        }

        if (reservation is not null)
        {
            if (reservation.ArrivalDate == businessDate) return RoomAvailabilityState.ExpectedArrival;
            return RoomAvailabilityState.Reserved;
        }

        return hk?.Status switch
        {
            HousekeepingRoomStatus.VacantClean or HousekeepingRoomStatus.Inspected or null => RoomAvailabilityState.VacantClean,
            _ => RoomAvailabilityState.VacantDirty
        };
    }

    public async Task<IReadOnlyList<RoomTypeInventory>> CalculateInventoryAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var availability = await GetRoomAvailabilityAsync(propertyId, businessDate, ct);
        var roomTypes = await db.HotelRoomTypes.AsNoTracking().Where(t => t.PropertyId == propertyId && t.IsActive).ToListAsync(ct);

        return roomTypes.Select(rt =>
        {
            var rooms = availability.Where(a => a.RoomTypeId == rt.HotelRoomTypeId).ToList();
            return new RoomTypeInventory(rt.HotelRoomTypeId, rt.Name, rt.Code,
                PhysicalInventory: rooms.Count,
                OutOfOrder: rooms.Count(a => a.State == RoomAvailabilityState.OutOfOrder),
                OutOfService: rooms.Count(a => a.State == RoomAvailabilityState.OutOfService),
                Occupied: rooms.Count(a => a.State is RoomAvailabilityState.OccupiedClean or RoomAvailabilityState.OccupiedDirty or RoomAvailabilityState.DueOut),
                Reserved: rooms.Count(a => a.State == RoomAvailabilityState.Reserved),
                ExpectedDeparture: rooms.Count(a => a.State == RoomAvailabilityState.ExpectedDeparture),
                ExpectedArrival: rooms.Count(a => a.State == RoomAvailabilityState.ExpectedArrival),
                VacantClean: rooms.Count(a => a.State == RoomAvailabilityState.VacantClean),
                VacantDirty: rooms.Count(a => a.State == RoomAvailabilityState.VacantDirty),
                Blocked: rooms.Count(a => a.State == RoomAvailabilityState.Blocked),
                AvailableRooms: rooms.Count(a => a.State is RoomAvailabilityState.VacantClean or RoomAvailabilityState.VacantDirty),
                RemainingInventory: rooms.Count(a => a.State is RoomAvailabilityState.VacantClean or RoomAvailabilityState.VacantDirty or RoomAvailabilityState.ExpectedDeparture),
                OverbookingLimit: 0,
                WalkInCapacity: rooms.Count(a => a.State is RoomAvailabilityState.VacantClean or RoomAvailabilityState.VacantDirty or RoomAvailabilityState.ExpectedDeparture)
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<DailyInventory>> CalculateDailyInventoryAsync(Guid propertyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        var totalRooms = await db.HotelRooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive, ct);
        var result = new List<DailyInventory>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var availability = await GetRoomAvailabilityAsync(propertyId, d, ct);
            var available = availability.Count(a => a.State is RoomAvailabilityState.VacantClean or RoomAvailabilityState.VacantDirty);
            var occupied = availability.Count(a => a.State is RoomAvailabilityState.OccupiedClean or RoomAvailabilityState.OccupiedDirty);
            result.Add(new DailyInventory(d, totalRooms,
                availability.Count(a => a.State == RoomAvailabilityState.OutOfOrder),
                availability.Count(a => a.State == RoomAvailabilityState.OutOfService),
                occupied,
                availability.Count(a => a.State == RoomAvailabilityState.Reserved),
                availability.Count(a => a.State == RoomAvailabilityState.ExpectedArrival),
                availability.Count(a => a.State == RoomAvailabilityState.ExpectedDeparture),
                available,
                totalRooms > 0 ? (int)((double)(totalRooms - available) / totalRooms * 100) : 0,
                available,
                0, 0));
        }
        return result;
    }

    public async Task<AvailabilityForecast> CalculateForecastAsync(Guid propertyId, DateOnly fromDate, int days, CancellationToken ct)
    {
        var toDate = fromDate.AddDays(days - 1);
        var daily = await CalculateDailyInventoryAsync(propertyId, fromDate, toDate, ct);
        var peakDay = daily.OrderByDescending(d => d.OccupancyPercent).FirstOrDefault();
        var lowestDay = daily.OrderBy(d => d.Remaining).FirstOrDefault();
        return new AvailabilityForecast(fromDate, toDate, daily.ToList(),
            peakDay.OccupancyPercent, peakDay.Date,
            lowestDay.Remaining, lowestDay.Date);
    }
}
