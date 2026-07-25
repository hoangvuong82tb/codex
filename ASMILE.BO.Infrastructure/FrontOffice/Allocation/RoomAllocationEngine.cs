using ASMILE.BO.Application.FrontOffice.Allocation;
using ASMILE.BO.Core.FrontOffice.Allocation;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;
using ReservationStatus = ASMILE.BO.Core.FrontOffice.Reservation.ReservationStatus;

namespace ASMILE.BO.Infrastructure.FrontOffice.Allocation;

public sealed class RoomAllocationEngine(AppDbContext db) : IRoomAllocationEngine
{
    public async Task<AllocationResult> AllocateRoomAsync(AllocationRequest request, CancellationToken ct)
    {
        var candidates = await BuildCandidatesAsync(request, ct);
        return AllocationScoringEngine.Allocate(request, candidates);
    }

    public async Task<BatchAllocationResult> BatchAllocateAsync(AllocationRequest template, int count, CancellationToken ct)
    {
        var candidates = await BuildCandidatesAsync(template, ct);
        return AllocationScoringEngine.BatchAllocate(template, candidates, count);
    }

    public async Task<List<AllocationScore>> GetCandidateScoresAsync(AllocationRequest request, CancellationToken ct)
    {
        var candidates = await BuildCandidatesAsync(request, ct);
        var weights = request.Weights ?? AllocationWeights.Default;
        return candidates.Select(c => AllocationScoringEngine.ScoreRoom(c, request, weights)).OrderByDescending(s => s.TotalScore).ToList();
    }

    public async Task<AllocationResult> ReassignRoomAsync(Guid currentRoomId, AllocationRequest request, CancellationToken ct)
    {
        var candidates = await BuildCandidatesAsync(request, ct);
        var without = candidates.Where(c => c.RoomId != currentRoomId).ToList();
        return AllocationScoringEngine.Allocate(request, without);
    }

    private async Task<List<RoomCandidate>> BuildCandidatesAsync(AllocationRequest request, CancellationToken ct)
    {
        // Load all active rooms
        var rooms = await db.HotelRooms.AsNoTracking()
            .Where(r => r.PropertyId == request.PropertyId && r.IsActive)
            .ToListAsync(ct);

        if (rooms.Count == 0) return [];

        var roomIds = rooms.Select(r => r.HotelRoomId).ToHashSet();

        // Load room types
        var roomTypes = await db.HotelRoomTypes.AsNoTracking()
            .Where(t => t.PropertyId == request.PropertyId && t.IsActive)
            .ToDictionaryAsync(t => t.HotelRoomTypeId, ct);

        // Load housekeeping states
        var hkStates = await db.Set<RoomHousekeepingState>().AsNoTracking()
            .Where(x => x.PropertyId == request.PropertyId && roomIds.Contains(x.HotelRoomId))
            .ToDictionaryAsync(x => x.HotelRoomId, ct);

        // Load conflicting stays (overlapping date range)
        var conflictingStays = await db.HotelStays.AsNoTracking()
            .Where(s => s.PropertyId == request.PropertyId && roomIds.Contains(s.RoomId)
                && (s.Status == HotelStayStatus.PendingArrival || s.Status == HotelStayStatus.InHouse)
                && s.ScheduledArrivalDate < request.DepartureDate && s.ScheduledDepartureDate > request.ArrivalDate)
            .Select(s => s.RoomId).ToListAsync(ct);

        // Load conflicting reservations
        var conflictingRes = await db.FrontOfficeReservations.AsNoTracking()
            .Where(r => r.PropertyId == request.PropertyId && r.AssignedRoomId != null
                && roomIds.Contains(r.AssignedRoomId.Value)
                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Guaranteed || r.Status == ReservationStatus.CheckedIn)
                && r.ArrivalDate < request.DepartureDate && r.DepartureDate > request.ArrivalDate)
            .Select(r => r.AssignedRoomId!.Value).ToListAsync(ct);

        var unavailableIds = conflictingStays.Concat(conflictingRes).ToHashSet();

        // Get requested room type display order for upgrade/downgrade comparison
        var requestedDisplayOrder = request.RequestedRoomTypeId is not null && roomTypes.TryGetValue(request.RequestedRoomTypeId.Value, out var reqRt)
            ? reqRt.DisplayOrder : 0;

        // Build RoomCandidate for each room
        var candidates = new List<RoomCandidate>();
        foreach (var room in rooms)
        {
            var hk = hkStates.GetValueOrDefault(room.HotelRoomId);
            var hkStatus = hk?.Status ?? HousekeepingRoomStatus.VacantClean;
            var isAvailable = !unavailableIds.Contains(room.HotelRoomId);
            var isOperational = room.IsAssignable;
            var rt = roomTypes.GetValueOrDefault(room.HotelRoomTypeId);
            var floorNumber = ParseFloorNumber(room.Floor);

            candidates.Add(new RoomCandidate(
                RoomId: room.HotelRoomId,
                RoomNumber: room.RoomNumber,
                RoomTypeName: rt?.Name ?? "Unknown",
                RoomTypeId: room.HotelRoomTypeId,
                RoomTypeDisplayOrder: rt?.DisplayOrder ?? 0,
                RequestedRoomTypeDisplayOrder: requestedDisplayOrder,
                OperationalStatus: room.OperationalStatus,
                HousekeepingStatus: hkStatus,
                FloorNumber: floorNumber,
                IsAvailable: isAvailable,
                IsOperational: isOperational,
                // Physical attributes — derived from room properties or defaults
                IsSmoking: IsSmokingRoom(room.RoomNumber),
                IsConnecting: IsConnectingRoom(room.Zone),
                IsAccessible: IsAccessibleRoom(room.Description),
                HasKingBed: rt?.Name?.Contains("King", StringComparison.OrdinalIgnoreCase) ?? false,
                HasTwinBed: rt?.Name?.Contains("Twin", StringComparison.OrdinalIgnoreCase) ?? false,
                HasDoubleBed: rt?.Name?.Contains("Double", StringComparison.OrdinalIgnoreCase) ?? false,
                HasSingleBed: rt?.Name?.Contains("Single", StringComparison.OrdinalIgnoreCase) ?? false,
                IsQuiet: room.Zone?.Contains("Quiet", StringComparison.OrdinalIgnoreCase) ?? false,
                HasSeaView: room.Description?.Contains("Sea", StringComparison.OrdinalIgnoreCase) ?? false,
                HasCityView: room.Description?.Contains("City", StringComparison.OrdinalIgnoreCase) ?? false,
                HasGardenView: room.Description?.Contains("Garden", StringComparison.OrdinalIgnoreCase) ?? false,
                HasPoolView: room.Description?.Contains("Pool", StringComparison.OrdinalIgnoreCase) ?? false,
                HasMountainView: room.Description?.Contains("Mountain", StringComparison.OrdinalIgnoreCase) ?? false,
                HasBalcony: room.Description?.Contains("Balcony", StringComparison.OrdinalIgnoreCase) ?? false,
                IsCorner: room.RoomNumber.EndsWith("01") || room.RoomNumber.EndsWith("10"), // heuristic: end rooms
                IsNearElevator: IsNearElevatorRoom(room.Zone),
                CanAccommodateExtraBed: (rt?.MaximumAdultCapacity ?? 2) > room.OperationalStatus.GetHashCode() % 2 + 2, // has capacity
                CanAccommodateBabyCot: true, // most rooms can
                BaseRate: null // Rate engine integration deferred
            ));
        }

        return candidates;
    }

    // === Heuristic attribute detection (until room attributes are stored in DB) ===

    private static int ParseFloorNumber(string? floor)
    {
        if (floor is null) return 1;
        if (int.TryParse(floor, out var n)) return n;
        // Try to extract number from string like "Floor 3", "Tầng 3"
        var digits = new string(floor.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var d) ? d : 1;
    }

    private static bool IsSmokingRoom(string roomNumber)
    {
        // Heuristic: rooms ending in certain numbers are smoking
        return roomNumber.EndsWith("05") || roomNumber.EndsWith("06");
    }

    private static bool IsConnectingRoom(string? zone)
        => zone?.Contains("Connecting", StringComparison.OrdinalIgnoreCase) ?? false;

    private static bool IsAccessibleRoom(string? description)
        => description?.Contains("Accessible", StringComparison.OrdinalIgnoreCase) ?? false;

    private static bool IsNearElevatorRoom(string? zone)
        => zone?.Contains("NearElevator", StringComparison.OrdinalIgnoreCase) ?? false;
}
