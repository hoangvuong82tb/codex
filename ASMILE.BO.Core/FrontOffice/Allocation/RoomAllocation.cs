using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Allocation;

/// <summary>Room preference types that guests can specify or that are inferred from guest profile.</summary>
public enum RoomPreferenceType
{
    HighFloor, LowFloor, NearElevator, FarElevator, Quiet,
    SeaView, CityView, GardenView, PoolView, MountainView,
    Twin, King, Double, Single,
    Smoking, NonSmoking,
    Connecting, Accessible,
    BabyCot, ExtraBed,
    Corner, Balcony, HighCeiling
}

/// <summary>Result of a room allocation decision.</summary>
public enum AllocationDecisionType { Assigned, Suggested, Blocked, NeedsReview, Upgraded, Downgraded, ManualOverride, NoAvailability }

/// <summary>Reason for upgrade/downgrade.</summary>
public enum UpgradeReason { None, VipAutoUpgrade, PaidUpgrade, OperationalUpgrade, RoomTypeOversold, GuestPreference, ManagerOverride }
public enum DowngradeReason { None, NoAvailability, OperationalDowngrade, RoomOutOfOrder, Overbooking, ManagerOverride }

/// <summary>Allocation strategy for auto-assignment.</summary>
public enum AllocationStrategy { BestRoom, NextBestRoom, ManualOverride, BatchAutoAssign, Reassign }

/// <summary>Scoring criteria weights — configurable per property.</summary>
public sealed record AllocationWeights(
    int HousekeepingReadiness = 15,
    int RoomPreferenceMatch = 25,
    int VipPriority = 20,
    int FloorPreference = 10,
    int GuestHistory = 10,
    int RoomTypeMatch = 15,
    int Accessibility = 5
)
{
    public int TotalWeight => HousekeepingReadiness + RoomPreferenceMatch + VipPriority + FloorPreference + GuestHistory + RoomTypeMatch + Accessibility;
    public static AllocationWeights Default => new();
}

/// <summary>A single room's allocation score with breakdown.</summary>
public sealed record AllocationScore(
    Guid HotelRoomId,
    string RoomNumber,
    string RoomTypeName,
    Guid RoomTypeId,
    int TotalScore,
    int HousekeepingScore,
    int PreferenceScore,
    int VipScore,
    int FloorScore,
    int HistoryScore,
    int RoomTypeScore,
    int AccessibilityScore,
    bool IsUpgrade,
    bool IsDowngrade,
    string? BlockingReason
);

/// <summary>Request to allocate a room.</summary>
public sealed record AllocationRequest(
    Guid PropertyId,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    int Adults,
    int Children,
    Guid? RequestedRoomTypeId,
    Guid? GuestId,
    string? GuestName,
    bool IsVip,
    string? VipLevel,
    IReadOnlyList<RoomPreferenceType> Preferences,
    Guid? PreferredRoomId,
    AllocationStrategy Strategy,
    bool AllowUpgrade,
    bool AllowDowngrade,
    AllocationWeights? Weights
);

/// <summary>Result of room allocation.</summary>
public sealed record AllocationResult(
    AllocationDecisionType Decision,
    AllocationScore? AssignedRoom,
    IReadOnlyList<AllocationScore> CandidateRooms,
    UpgradeReason UpgradeReason,
    DowngradeReason DowngradeReason,
    string? Message,
    bool RequiresManagerApproval
)
{
    public bool IsSuccess => Decision is AllocationDecisionType.Assigned or AllocationDecisionType.Suggested or AllocationDecisionType.Upgraded;
}

/// <summary>Batch allocation result.</summary>
public sealed record BatchAllocationResult(
    IReadOnlyList<AllocationResult> Results,
    int TotalRequested,
    int TotalAssigned,
    int TotalFailed,
    IReadOnlyList<string> Warnings
);
