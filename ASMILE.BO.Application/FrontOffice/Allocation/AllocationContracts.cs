using ASMILE.BO.Core.FrontOffice.Allocation;

namespace ASMILE.BO.Application.FrontOffice.Allocation;

// === Application DTOs for UI integration ===

public sealed record AllocationRequestDto(
    Guid PropertyId,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    int Adults,
    int Children,
    Guid? RoomTypeId,
    Guid? GuestId,
    bool IsVip,
    string? VipLevel,
    List<string> Preferences,
    Guid? PreferredRoomId,
    string Strategy,
    bool AllowUpgrade,
    bool AllowDowngrade
);

public sealed record AllocationScoreDto(
    Guid HotelRoomId, string RoomNumber, string RoomTypeName, Guid RoomTypeId,
    int TotalScore, int HousekeepingScore, int PreferenceScore, int VipScore,
    int FloorScore, int HistoryScore, int RoomTypeScore, int AccessibilityScore,
    bool IsUpgrade, bool IsDowngrade, string? BlockingReason
);

public sealed record AllocationResultDto(
    string Decision, AllocationScoreDto? AssignedRoom,
    IReadOnlyList<AllocationScoreDto> CandidateRooms,
    string UpgradeReason, string DowngradeReason,
    string? Message, bool RequiresManagerApproval
);

public sealed record BatchAllocationResultDto(
    IReadOnlyList<AllocationResultDto> Results,
    int TotalRequested, int TotalAssigned, int TotalFailed,
    IReadOnlyList<string> Warnings
);

// === Infrastructure interface ===

public interface IRoomAllocationEngine
{
    Task<AllocationResult> AllocateRoomAsync(AllocationRequest request, CancellationToken ct);
    Task<BatchAllocationResult> BatchAllocateAsync(AllocationRequest template, int count, CancellationToken ct);
    Task<List<AllocationScore>> GetCandidateScoresAsync(AllocationRequest request, CancellationToken ct);
    Task<AllocationResult> ReassignRoomAsync(Guid currentRoomId, AllocationRequest request, CancellationToken ct);
}

// === Mapping extensions ===

public static class AllocationMappingExtensions
{
    public static AllocationRequest ToDomain(this AllocationRequestDto dto) => new(
        dto.PropertyId, dto.ArrivalDate, dto.DepartureDate, dto.Adults, dto.Children,
        dto.RoomTypeId, dto.GuestId, null, dto.IsVip, dto.VipLevel,
        dto.Preferences.Select(ParsePreference).Where(p => p is not null).Cast<RoomPreferenceType>().ToList(),
        dto.PreferredRoomId,
        Enum.TryParse<AllocationStrategy>(dto.Strategy, out var s) ? s : AllocationStrategy.BestRoom,
        dto.AllowUpgrade, dto.AllowDowngrade, null);

    private static RoomPreferenceType? ParsePreference(string p) =>
        Enum.TryParse<RoomPreferenceType>(p, out var result) ? result : null;

    public static AllocationResultDto ToDto(this AllocationResult r) => new(
        r.Decision.ToString(), r.AssignedRoom?.ToDto(),
        r.CandidateRooms.Select(s => s.ToDto()).ToList(),
        r.UpgradeReason.ToString(), r.DowngradeReason.ToString(),
        r.Message, r.RequiresManagerApproval);

    public static AllocationScoreDto ToDto(this AllocationScore s) => new(
        s.HotelRoomId, s.RoomNumber, s.RoomTypeName, s.RoomTypeId,
        s.TotalScore, s.HousekeepingScore, s.PreferenceScore, s.VipScore,
        s.FloorScore, s.HistoryScore, s.RoomTypeScore, s.AccessibilityScore,
        s.IsUpgrade, s.IsDowngrade, s.BlockingReason);

    public static BatchAllocationResultDto ToDto(this BatchAllocationResult r) => new(
        r.Results.Select(res => res.ToDto()).ToList(),
        r.TotalRequested, r.TotalAssigned, r.TotalFailed, r.Warnings);
}
