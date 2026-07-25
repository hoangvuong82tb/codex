namespace ASMILE.BO.Core.FrontOffice.Availability;

/// <summary>Single source of truth for room state — derived from Operational + Housekeeping + Occupancy.</summary>
public enum RoomAvailabilityState
{
    VacantClean,
    VacantDirty,
    OccupiedClean,
    OccupiedDirty,
    Reserved,
    ExpectedArrival,
    ExpectedDeparture,
    DueOut,
    OutOfOrder,
    OutOfService,
    Blocked,
    Maintenance,
    HouseUse,
    Complimentary
}

public readonly record struct RoomAvailability(
    Guid HotelRoomId,
    string RoomNumber,
    string RoomTypeName,
    string? Floor,
    string? Building,
    Guid RoomTypeId,
    RoomAvailabilityState State,
    int StandardCapacity,
    int MaxCapacity,
    int CurrentAdults,
    int CurrentChildren,
    Guid? StayId,
    string? GuestName,
    bool IsVip,
    DateOnly? ArrivalDate,
    DateOnly? DepartureDate,
    string? ReservationNumber,
    DateTime LastUpdatedUtc);

public readonly record struct RoomTypeInventory(
    Guid RoomTypeId,
    string RoomTypeName,
    string Code,
    int PhysicalInventory,
    int OutOfOrder,
    int OutOfService,
    int Occupied,
    int Reserved,
    int ExpectedDeparture,
    int ExpectedArrival,
    int VacantClean,
    int VacantDirty,
    int Blocked,
    int AvailableRooms,
    int RemainingInventory,
    int OverbookingLimit,
    int WalkInCapacity
);

public readonly record struct AvailabilityRequest(
    Guid PropertyId,
    DateOnly BusinessDate,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    Guid? RoomTypeId,
    int Adults,
    int Children,
    int RoomCount);

public readonly record struct AvailabilityResult(
    bool IsAvailable,
    int AvailableRoomCount,
    string? UnavailabilityReason,
    IReadOnlyList<RoomAvailability> AvailableRooms);

public readonly record struct DailyInventory(
    DateOnly Date,
    int TotalPhysicalRooms,
    int OutOfOrder,
    int OutOfService,
    int Occupied,
    int Reserved,
    int ExpectedArrivals,
    int ExpectedDepartures,
    int Available,
    int OccupancyPercent,
    int Remaining,
    decimal Adr,
    decimal RevPar
);

public readonly record struct AvailabilityForecast(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<DailyInventory> DailyInventories,
    int PeakOccupancyPercent,
    DateOnly PeakDate,
    int LowestAvailability,
    DateOnly LowestDate
);
