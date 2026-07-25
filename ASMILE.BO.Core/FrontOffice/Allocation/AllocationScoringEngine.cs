using ASMILE.BO.Core.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Loyalty;

namespace ASMILE.BO.Core.FrontOffice.Allocation;

/// <summary>Pure domain service — stateless room allocation scoring and decision engine.</summary>
public static class AllocationScoringEngine
{
    public static AllocationResult Allocate(AllocationRequest request, IReadOnlyList<RoomCandidate> candidates)
    {
        if (candidates.Count == 0)
            return new AllocationResult(AllocationDecisionType.NoAvailability, null, [], UpgradeReason.None, DowngradeReason.NoAvailability, "No rooms available for the requested criteria.", false);

        var weights = request.Weights ?? AllocationWeights.Default;

        // Score all candidates
        var scored = candidates.Select(c => ScoreRoom(c, request, weights)).OrderByDescending(s => s.TotalScore).ToList();

        // Filter out blocked rooms
        var available = scored.Where(s => s.BlockingReason is null).ToList();

        // If no rooms available, try upgrade
        if (available.Count == 0 && request.AllowUpgrade)
        {
            var upgrades = scored.Where(s => s.IsUpgrade && s.BlockingReason is null).OrderByDescending(s => s.TotalScore).ToList();
            if (upgrades.Count > 0)
            {
                var upgradePick = upgrades[0];
                return new AllocationResult(AllocationDecisionType.Upgraded, upgradePick, scored, UpgradeReason.RoomTypeOversold, DowngradeReason.None, $"Upgraded to {upgradePick.RoomTypeName} ({upgradePick.RoomNumber}).", false);
            }
            // Rooms exist but all are blocked — needs manual review
            if (scored.Count > 0)
            {
                var blockedReasons = scored.Where(s => s.BlockingReason is not null).Select(s => $"{s.RoomNumber}: {s.BlockingReason}").ToList();
                return new AllocationResult(AllocationDecisionType.NeedsReview, null, scored, UpgradeReason.None, DowngradeReason.NoAvailability, $"All rooms blocked. Reasons: {string.Join("; ", blockedReasons)}", true);
            }
            return new AllocationResult(AllocationDecisionType.NoAvailability, null, scored, UpgradeReason.None, DowngradeReason.NoAvailability, "No rooms or upgrades available.", false);
        }

        if (available.Count == 0)
        {
            var blockedReasons = scored.Where(s => s.BlockingReason is not null).Select(s => $"{s.RoomNumber}: {s.BlockingReason}").ToList();
            return new AllocationResult(AllocationDecisionType.NeedsReview, null, scored, UpgradeReason.None, DowngradeReason.NoAvailability, $"All rooms blocked. Reasons: {string.Join("; ", blockedReasons)}", true);
        }

        var best = available[0];
        var decision = best.IsUpgrade ? AllocationDecisionType.Upgraded
            : best.IsDowngrade ? AllocationDecisionType.Downgraded
            : request.Strategy == AllocationStrategy.ManualOverride ? AllocationDecisionType.ManualOverride
            : AllocationDecisionType.Assigned;

        var upgradeReason = best.IsUpgrade ? DetectUpgradeReason(request, best) : UpgradeReason.None;
        var downgradeReason = best.IsDowngrade ? DowngradeReason.NoAvailability : DowngradeReason.None;

        var message = decision switch
        {
            AllocationDecisionType.Upgraded => $"Auto-upgraded to {best.RoomTypeName} room {best.RoomNumber} (score: {best.TotalScore}).",
            AllocationDecisionType.Downgraded => $"Downgraded to {best.RoomTypeName} room {best.RoomNumber} — no matching rooms available.",
            _ => $"Assigned room {best.RoomNumber} ({best.RoomTypeName}, score: {best.TotalScore})."
        };

        return new AllocationResult(decision, best, scored, upgradeReason, downgradeReason, message, downgradeReason != DowngradeReason.None);
    }

    public static BatchAllocationResult BatchAllocate(AllocationRequest template, IReadOnlyList<RoomCandidate> candidates, int count)
    {
        var results = new List<AllocationResult>();
        var assignedRoomIds = new HashSet<Guid>();
        var remainingCandidates = candidates.ToList();

        for (int i = 0; i < count; i++)
        {
            var availableCandidates = remainingCandidates.Where(c => !assignedRoomIds.Contains(c.RoomId)).ToList();
            var result = Allocate(template, availableCandidates);
            results.Add(result);
            if (result.IsSuccess && result.AssignedRoom is not null)
                assignedRoomIds.Add(result.AssignedRoom.HotelRoomId);
        }

        var totalAssigned = results.Count(r => r.IsSuccess);
        var warnings = new List<string>();
        if (totalAssigned < count)
            warnings.Add($"Only {totalAssigned}/{count} rooms assigned. {count - totalAssigned} requests could not be fulfilled.");

        return new BatchAllocationResult(results, count, totalAssigned, count - totalAssigned, warnings);
    }

    public static AllocationScore ScoreRoom(RoomCandidate candidate, AllocationRequest request, AllocationWeights weights)
    {
        // 1. Availability check — blocking filter
        if (!candidate.IsAvailable)
            return CreateBlocked(candidate, "Room is not available for the requested dates.");

        if (!candidate.IsOperational)
            return CreateBlocked(candidate, $"Room is {candidate.OperationalStatus}.");

        // 2. Housekeeping score (0-100)
        var hkScore = ScoreHousekeeping(candidate.HousekeepingStatus);

        // 3. Preference match (0-100)
        var prefScore = ScorePreferences(candidate, request.Preferences);

        // 4. VIP priority (0-100)
        var vipScore = ScoreVip(request.VipLevel, request.IsVip);

        // 5. Floor preference (0-100)
        var floorScore = ScoreFloor(candidate, request.Preferences);

        // 6. Guest history (0-100)
        var historyScore = ScoreHistory(candidate, request.PreferredRoomId);

        // 7. Room type match (0-100)
        var (rtScore, isUpgrade, isDowngrade) = ScoreRoomType(candidate, request.RequestedRoomTypeId, request.AllowUpgrade, request.AllowDowngrade);
        if (rtScore == 0 && isUpgrade) return CreateBlocked(candidate, "Upgrade not allowed — room type mismatch.");
        if (rtScore == 0 && isDowngrade) return CreateBlocked(candidate, "Downgrade not allowed — room type mismatch.");

        // 8. Accessibility (0-100)
        var accessScore = ScoreAccessibility(candidate, request.Preferences);

        // Weighted total
        var total = (hkScore * weights.HousekeepingReadiness
                   + prefScore * weights.RoomPreferenceMatch
                   + vipScore * weights.VipPriority
                   + floorScore * weights.FloorPreference
                   + historyScore * weights.GuestHistory
                   + rtScore * weights.RoomTypeMatch
                   + accessScore * weights.Accessibility) / weights.TotalWeight;

        return new AllocationScore(
            candidate.RoomId, candidate.RoomNumber, candidate.RoomTypeName, candidate.RoomTypeId,
            total, hkScore, prefScore, vipScore, floorScore, historyScore, rtScore, accessScore,
            isUpgrade, isDowngrade, null);
    }

    // === Scoring functions ===

    private static int ScoreHousekeeping(HousekeepingRoomStatus status) => status switch
    {
        HousekeepingRoomStatus.VacantClean or HousekeepingRoomStatus.Inspected => 100,
        HousekeepingRoomStatus.OccupiedClean => 60,
        HousekeepingRoomStatus.Cleaning => 40,
        HousekeepingRoomStatus.VacantDirty => 20,
        HousekeepingRoomStatus.OccupiedDirty => 0,
        _ => 0
    };

    private static int ScorePreferences(RoomCandidate c, IReadOnlyList<RoomPreferenceType> prefs)
    {
        if (prefs.Count == 0) return 50; // neutral
        var matches = 0;
        foreach (var p in prefs)
        {
            var matched = p switch
            {
                RoomPreferenceType.HighFloor => c.FloorNumber >= 5,
                RoomPreferenceType.LowFloor => c.FloorNumber <= 2,
                RoomPreferenceType.Smoking => c.IsSmoking,
                RoomPreferenceType.NonSmoking => !c.IsSmoking,
                RoomPreferenceType.Connecting => c.IsConnecting,
                RoomPreferenceType.Accessible => c.IsAccessible,
                RoomPreferenceType.King => c.HasKingBed,
                RoomPreferenceType.Twin => c.HasTwinBed,
                RoomPreferenceType.Double => c.HasDoubleBed,
                RoomPreferenceType.Quiet => c.IsQuiet,
                RoomPreferenceType.SeaView => c.HasSeaView,
                RoomPreferenceType.CityView => c.HasCityView,
                RoomPreferenceType.GardenView => c.HasGardenView,
                RoomPreferenceType.PoolView => c.HasPoolView,
                RoomPreferenceType.MountainView => c.HasMountainView,
                RoomPreferenceType.Balcony => c.HasBalcony,
                RoomPreferenceType.Corner => c.IsCorner,
                RoomPreferenceType.NearElevator => c.IsNearElevator,
                RoomPreferenceType.FarElevator => !c.IsNearElevator,
                RoomPreferenceType.ExtraBed => c.CanAccommodateExtraBed,
                RoomPreferenceType.BabyCot => c.CanAccommodateBabyCot,
                _ => false
            };
            if (matched) matches++;
        }
        return matches * 100 / prefs.Count;
    }

    private static int ScoreVip(string? vipLevel, bool isVip)
    {
        if (!isVip || vipLevel is null) return 0;
        return vipLevel switch
        {
            "Diamond" => 100,
            "Platinum" => 80,
            "Gold" => 60,
            "Silver" => 40,
            "Bronze" => 20,
            _ => 0
        };
    }

    private static int ScoreFloor(RoomCandidate c, IReadOnlyList<RoomPreferenceType> prefs)
    {
        var hasHigh = prefs.Contains(RoomPreferenceType.HighFloor);
        var hasLow = prefs.Contains(RoomPreferenceType.LowFloor);
        if (hasHigh && hasLow) return 50;
        if (hasHigh) return c.FloorNumber >= 5 ? 100 : (c.FloorNumber >= 3 ? 50 : 0);
        if (hasLow) return c.FloorNumber <= 2 ? 100 : (c.FloorNumber <= 4 ? 50 : 0);
        return 50; // no preference = neutral
    }

    private static int ScoreHistory(RoomCandidate c, Guid? preferredRoomId)
    {
        if (preferredRoomId is null) return 0;
        return c.RoomId == preferredRoomId.Value ? 100 : 0;
    }

    private static (int score, bool isUpgrade, bool isDowngrade) ScoreRoomType(RoomCandidate c, Guid? requestedRoomTypeId, bool allowUpgrade, bool allowDowngrade)
    {
        if (requestedRoomTypeId is null) return (50, false, false); // no preference
        if (c.RoomTypeId == requestedRoomTypeId.Value) return (100, false, false); // exact match

        // Determine if candidate is upgrade or downgrade based on display order (lower = higher tier)
        if (c.RoomTypeDisplayOrder < c.RequestedRoomTypeDisplayOrder)
        {
            if (!allowUpgrade) return (0, true, false); // Blocked — upgrade not allowed
            return (80, true, false); // Upgrade — good but slightly lower score than exact
        }
        else
        {
            if (!allowDowngrade) return (0, false, true); // Blocked — downgrade not allowed
            return (30, false, true); // Downgrade — significantly lower score
        }
    }

    private static int ScoreAccessibility(RoomCandidate c, IReadOnlyList<RoomPreferenceType> prefs)
    {
        if (prefs.Contains(RoomPreferenceType.Accessible))
            return c.IsAccessible ? 100 : 0;
        return 50; // neutral
    }

    private static UpgradeReason DetectUpgradeReason(AllocationRequest request, AllocationScore score)
    {
        if (score.VipScore >= 60) return UpgradeReason.VipAutoUpgrade;
        if (score.PreferenceScore >= 80) return UpgradeReason.GuestPreference;
        return UpgradeReason.OperationalUpgrade;
    }

    private static AllocationScore CreateBlocked(RoomCandidate c, string reason)
        => new(c.RoomId, c.RoomNumber, c.RoomTypeName, c.RoomTypeId, 0, 0, 0, 0, 0, 0, 0, 0, false, false, reason);
}

/// <summary>Input data for scoring a single room — provided by the infrastructure layer.</summary>
public sealed record RoomCandidate(
    Guid RoomId,
    string RoomNumber,
    string RoomTypeName,
    Guid RoomTypeId,
    int RoomTypeDisplayOrder,
    int RequestedRoomTypeDisplayOrder,
    HotelRoomOperationalStatus OperationalStatus,
    HousekeepingRoomStatus HousekeepingStatus,
    int FloorNumber,
    bool IsAvailable,
    bool IsOperational,
    bool IsSmoking,
    bool IsConnecting,
    bool IsAccessible,
    bool HasKingBed,
    bool HasTwinBed,
    bool HasDoubleBed,
    bool HasSingleBed,
    bool IsQuiet,
    bool HasSeaView,
    bool HasCityView,
    bool HasGardenView,
    bool HasPoolView,
    bool HasMountainView,
    bool HasBalcony,
    bool IsCorner,
    bool IsNearElevator,
    bool CanAccommodateExtraBed,
    bool CanAccommodateBabyCot,
    decimal? BaseRate
);
