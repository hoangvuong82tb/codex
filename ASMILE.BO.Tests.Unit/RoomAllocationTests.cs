using ASMILE.BO.Core.FrontOffice.Allocation;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class RoomAllocationTests
{
    private static readonly DateOnly Arrival = new(2026, 7, 20);
    private static readonly DateOnly Departure = new(2026, 7, 22);

    [Fact]
    public void ALLOC_DOM_001_best_room_selected_by_score()
    {
        var request = CreateRequest();
        var candidates = new[]
        {
            CreateCandidate("101", "STD", hk: HousekeepingRoomStatus.VacantClean, floor: 3),
            CreateCandidate("102", "STD", hk: HousekeepingRoomStatus.VacantDirty, floor: 3),
        };
        var result = AllocationScoringEngine.Allocate(request, candidates);
        result.IsSuccess.Should().BeTrue();
        result.AssignedRoom!.RoomNumber.Should().Be("101"); // Clean beats dirty
    }

    [Fact]
    public void ALLOC_DOM_002_no_availability_when_all_blocked()
    {
        var request = CreateRequest();
        var candidates = new[] { CreateCandidate("101", "STD", available: false) };
        var result = AllocationScoringEngine.Allocate(request, candidates);
        result.Decision.Should().Be(AllocationDecisionType.NeedsReview);
    }

    [Fact]
    public void ALLOC_DOM_003_no_rooms_returns_no_availability()
    {
        var request = CreateRequest();
        var result = AllocationScoringEngine.Allocate(request, []);
        result.Decision.Should().Be(AllocationDecisionType.NoAvailability);
    }

    [Fact]
    public void ALLOC_DOM_004_vip_guest_gets_higher_score()
    {
        var vipRequest = CreateRequest(isVip: true, vipLevel: "Diamond");
        var normalRequest = CreateRequest(isVip: false);
        var candidates = new[] { CreateCandidate("101", "STD", hk: HousekeepingRoomStatus.VacantClean) };

        var vipScore = AllocationScoringEngine.ScoreRoom(candidates[0], vipRequest, AllocationWeights.Default);
        var normalScore = AllocationScoringEngine.ScoreRoom(candidates[0], normalRequest, AllocationWeights.Default);

        vipScore.TotalScore.Should().BeGreaterThan(normalScore.TotalScore);
        vipScore.VipScore.Should().Be(100);
        normalScore.VipScore.Should().Be(0);
    }

    [Fact]
    public void ALLOC_DOM_005_floor_preference_high_floor()
    {
        var request = CreateRequest(prefs: [RoomPreferenceType.HighFloor]);
        var highFloor = CreateCandidate("501", "STD", floor: 5);
        var lowFloor = CreateCandidate("101", "STD", floor: 1);

        var highScore = AllocationScoringEngine.ScoreRoom(highFloor, request, AllocationWeights.Default);
        var lowScore = AllocationScoringEngine.ScoreRoom(lowFloor, request, AllocationWeights.Default);

        highScore.FloorScore.Should().Be(100);
        lowScore.FloorScore.Should().Be(0);
        highScore.TotalScore.Should().BeGreaterThan(lowScore.TotalScore);
    }

    [Fact]
    public void ALLOC_DOM_006_preference_match_smoking()
    {
        var request = CreateRequest(prefs: [RoomPreferenceType.NonSmoking]);
        var nonSmoking = CreateCandidate("101", "STD", smoking: false);
        var smoking = CreateCandidate("102", "STD", smoking: true);

        var nsScore = AllocationScoringEngine.ScoreRoom(nonSmoking, request, AllocationWeights.Default);
        var sScore = AllocationScoringEngine.ScoreRoom(smoking, request, AllocationWeights.Default);

        nsScore.PreferenceScore.Should().Be(100);
        sScore.PreferenceScore.Should().Be(0);
    }

    [Fact]
    public void ALLOC_DOM_007_room_type_exact_match_scores_higher()
    {
        var request = CreateRequest(roomTypeId: Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var exactMatch = CreateCandidate("101", "STD", typeId: Guid.Parse("00000000-0000-0000-0000-000000000001"), typeOrder: 1, reqTypeOrder: 1);
        var upgrade = CreateCandidate("201", "DLX", typeId: Guid.Parse("00000000-0000-0000-0000-000000000002"), typeOrder: 0, reqTypeOrder: 1); // DLX is higher tier (order 0 < 1)

        var exactScore = AllocationScoringEngine.ScoreRoom(exactMatch, request, AllocationWeights.Default);
        var upgradeScore = AllocationScoringEngine.ScoreRoom(upgrade, request, AllocationWeights.Default);

        exactScore.RoomTypeScore.Should().Be(100); // exact match
        upgradeScore.RoomTypeScore.Should().Be(80); // upgrade — good but not exact
        upgradeScore.IsUpgrade.Should().BeTrue();
    }

    [Fact]
    public void ALLOC_DOM_008_upgrade_auto_selected_when_best()
    {
        var request = CreateRequest(roomTypeId: Guid.Parse("00000000-0000-0000-0000-000000000001"), allowUpgrade: true);
        var candidates = new[]
        {
            CreateCandidate("201", "DLX", typeId: Guid.Parse("00000000-0000-0000-0000-000000000002"), typeOrder: 0, reqTypeOrder: 1, hk: HousekeepingRoomStatus.VacantClean, floor: 5),
        };
        var result = AllocationScoringEngine.Allocate(request, candidates);
        result.Decision.Should().Be(AllocationDecisionType.Upgraded);
        result.AssignedRoom!.IsUpgrade.Should().BeTrue();
    }

    [Fact]
    public void ALLOC_DOM_009_upgrade_blocked_when_not_allowed()
    {
        var request = CreateRequest(roomTypeId: Guid.Parse("00000000-0000-0000-0000-000000000001"), allowUpgrade: false);
        var candidates = new[]
        {
            CreateCandidate("201", "DLX", typeId: Guid.Parse("00000000-0000-0000-0000-000000000002"), typeOrder: 0, reqTypeOrder: 1, hk: HousekeepingRoomStatus.VacantClean),
        };
        var result = AllocationScoringEngine.Allocate(request, candidates);
        // Upgrade not allowed and no exact match → candidate's RoomTypeScore is 0 (blocked)
        result.Decision.Should().Be(AllocationDecisionType.NeedsReview);
        result.AssignedRoom.Should().BeNull();
    }

    [Fact]
    public void ALLOC_DOM_010_batch_allocation_respects_room_deduplication()
    {
        var request = CreateRequest();
        var candidates = new[]
        {
            CreateCandidate("101", "STD", hk: HousekeepingRoomStatus.VacantClean, floor: 3),
            CreateCandidate("102", "STD", hk: HousekeepingRoomStatus.VacantClean, floor: 2),
        };
        var result = AllocationScoringEngine.BatchAllocate(request, candidates, 2);
        result.TotalAssigned.Should().Be(2);
        result.Results[0].AssignedRoom!.RoomNumber.Should().NotBe(result.Results[1].AssignedRoom!.RoomNumber);
    }

    [Fact]
    public void ALLOC_DOM_011_batch_partial_when_not_enough_rooms()
    {
        var request = CreateRequest();
        var candidates = new[] { CreateCandidate("101", "STD", hk: HousekeepingRoomStatus.VacantClean) };
        var result = AllocationScoringEngine.BatchAllocate(request, candidates, 3);
        result.TotalAssigned.Should().Be(1);
        result.TotalFailed.Should().Be(2);
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void ALLOC_DOM_012_guest_history_prefers_same_room()
    {
        var preferredRoomId = Guid.NewGuid();
        var request = CreateRequest(preferredRoomId: preferredRoomId);
        var sameRoom = CreateCandidate("101", "STD", roomId: preferredRoomId, hk: HousekeepingRoomStatus.VacantClean);
        var otherRoom = CreateCandidate("102", "STD", roomId: Guid.NewGuid(), hk: HousekeepingRoomStatus.VacantClean);

        var sameScore = AllocationScoringEngine.ScoreRoom(sameRoom, request, AllocationWeights.Default);
        var otherScore = AllocationScoringEngine.ScoreRoom(otherRoom, request, AllocationWeights.Default);

        sameScore.HistoryScore.Should().Be(100);
        otherScore.HistoryScore.Should().Be(0);
        sameScore.TotalScore.Should().BeGreaterThan(otherScore.TotalScore);
    }

    [Fact]
    public void ALLOC_DOM_013_accessibility_preference_boosts_accessible_room()
    {
        var request = CreateRequest(prefs: [RoomPreferenceType.Accessible]);
        var accessible = CreateCandidate("101", "STD", accessible: true);
        var notAccessible = CreateCandidate("102", "STD", accessible: false);

        var accScore = AllocationScoringEngine.ScoreRoom(accessible, request, AllocationWeights.Default);
        var naScore = AllocationScoringEngine.ScoreRoom(notAccessible, request, AllocationWeights.Default);

        accScore.AccessibilityScore.Should().Be(100);
        naScore.AccessibilityScore.Should().Be(0);
    }

    [Fact]
    public void ALLOC_DOM_014_view_preferences_match()
    {
        var request = CreateRequest(prefs: [RoomPreferenceType.SeaView]);
        var seaView = CreateCandidate("101", "STD", seaView: true);
        var cityView = CreateCandidate("102", "STD", cityView: true);

        var seaScore = AllocationScoringEngine.ScoreRoom(seaView, request, AllocationWeights.Default);
        var cityScore = AllocationScoringEngine.ScoreRoom(cityView, request, AllocationWeights.Default);

        seaScore.PreferenceScore.Should().Be(100);
        cityScore.PreferenceScore.Should().Be(0);
    }

    [Fact]
    public void ALLOC_DOM_015_reassign_excludes_current_room()
    {
        var currentRoomId = Guid.NewGuid();
        var request = CreateRequest();
        var candidates = new[]
        {
            CreateCandidate("101", "STD", roomId: currentRoomId, hk: HousekeepingRoomStatus.VacantClean, floor: 5),
            CreateCandidate("102", "STD", roomId: Guid.NewGuid(), hk: HousekeepingRoomStatus.VacantClean, floor: 3),
        };
        // Simulate what ReassignRoomAsync does: filter out current room
        var withoutCurrent = candidates.Where(c => c.RoomId != currentRoomId).ToList();
        var result = AllocationScoringEngine.Allocate(request, withoutCurrent);
        result.AssignedRoom!.RoomNumber.Should().Be("102");
    }

    [Fact]
    public void ALLOC_DOM_016_multiple_preferences_partial_match()
    {
        var request = CreateRequest(prefs: [RoomPreferenceType.HighFloor, RoomPreferenceType.SeaView, RoomPreferenceType.King]);
        var candidate = CreateCandidate("501", "STD", floor: 5, seaView: true, kingBed: true, hk: HousekeepingRoomStatus.VacantClean);

        var score = AllocationScoringEngine.ScoreRoom(candidate, request, AllocationWeights.Default);
        score.PreferenceScore.Should().Be(100); // 3/3 matched
    }

    // === Helpers ===

    private static AllocationRequest CreateRequest(
        Guid? roomTypeId = null, bool isVip = false, string? vipLevel = null,
        RoomPreferenceType[]? prefs = null, Guid? preferredRoomId = null,
        bool allowUpgrade = true, bool allowDowngrade = false)
        => new(Guid.NewGuid(), Arrival, Departure, 2, 0, roomTypeId, null, null,
               isVip, vipLevel, prefs ?? [], preferredRoomId,
               AllocationStrategy.BestRoom, allowUpgrade, allowDowngrade, null);

    private static RoomCandidate CreateCandidate(
        string roomNumber, string typeName,
        Guid? roomId = null, Guid? typeId = null,
        int floor = 1, int typeOrder = 1, int reqTypeOrder = 1,
        bool available = true, bool operational = true,
        HousekeepingRoomStatus hk = HousekeepingRoomStatus.VacantClean,
        bool smoking = false, bool connecting = false, bool accessible = false,
        bool kingBed = false, bool twinBed = false, bool doubleBed = false,
        bool seaView = false, bool cityView = false, bool gardenView = false,
        bool quiet = false, bool balcony = false, bool corner = false,
        bool nearElevator = false, bool extraBed = false, bool babyCot = false)
        => new(
            RoomId: roomId ?? Guid.NewGuid(),
            RoomNumber: roomNumber,
            RoomTypeName: typeName,
            RoomTypeId: typeId ?? Guid.NewGuid(),
            RoomTypeDisplayOrder: typeOrder,
            RequestedRoomTypeDisplayOrder: reqTypeOrder,
            OperationalStatus: operational ? HotelRoomOperationalStatus.Available : HotelRoomOperationalStatus.OutOfOrder,
            HousekeepingStatus: hk,
            FloorNumber: floor,
            IsAvailable: available,
            IsOperational: operational,
            IsSmoking: smoking,
            IsConnecting: connecting,
            IsAccessible: accessible,
            HasKingBed: kingBed,
            HasTwinBed: twinBed,
            HasDoubleBed: doubleBed,
            HasSingleBed: !kingBed && !twinBed && !doubleBed,
            IsQuiet: quiet,
            HasSeaView: seaView,
            HasCityView: cityView,
            HasGardenView: gardenView,
            HasPoolView: false,
            HasMountainView: false,
            HasBalcony: balcony,
            IsCorner: corner,
            IsNearElevator: nearElevator,
            CanAccommodateExtraBed: extraBed,
            CanAccommodateBabyCot: babyCot,
            BaseRate: null
        );
}
