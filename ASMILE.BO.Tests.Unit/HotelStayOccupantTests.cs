using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Stay;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelStayOccupantTests
{
    [Fact]
    public void STAY_OCCUPANT_DOM_U_001_creates_a_real_profile_snapshot_and_can_be_removed()
    {
        var property = PropertyId.New();
        var guestId = Guid.NewGuid();
        var addedAt = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        var occupant = HotelStayOccupant.Create(property, Guid.NewGuid(), guestId, "  Nguyễn Văn B  ", " 0901 ", HotelStayOccupantType.Adult, addedAt, ActorId.New());

        occupant.PropertyId.Should().Be(property.Value);
        occupant.GuestProfileId.Should().Be(guestId);
        occupant.GuestNameSnapshot.Should().Be("Nguyễn Văn B");
        occupant.GuestPhoneSnapshot.Should().Be("0901");
        occupant.IsActive.Should().BeTrue();

        occupant.Remove(addedAt.AddMinutes(5), ActorId.New());
        occupant.IsActive.Should().BeFalse();
        occupant.RemovedAtUtc.Should().Be(addedAt.AddMinutes(5));
        Action removeAgain = () => occupant.Remove(addedAt.AddMinutes(6), null);
        removeAgain.Should().Throw<InvalidOperationException>().WithMessage("STAY_OCCUPANT_ALREADY_REMOVED");
    }

    [Fact]
    public void STAY_OCCUPANT_DOM_U_002_reconciles_stay_counts_without_reducing_existing_values()
    {
        var stay = HotelStay.Prepare(
            "STY-202609-000001", PropertyId.New(), Guid.NewGuid(), "RSV-1", Guid.NewGuid(), "Primary", null,
            Guid.NewGuid(), "101", Guid.NewGuid(), "STD",
            HotelStayPeriod.Create(new(2026, 9, 8), new(2026, 9, 9)),
            HotelStayGuestCount.Create(2, 1), "WalkIn", DateTime.UtcNow, null);

        stay.ReconcileGuestCounts(1, 0, DateTime.UtcNow, null);
        stay.Adults.Should().Be(2);
        stay.Children.Should().Be(1);
        stay.DomainEvents.Should().Contain(x => x.EventName == "StayGuestCountsReconciled");
    }

    [Fact]
    public void STAY_OCCUPANT_DOM_U_003_removal_reconciles_explicit_guest_counts()
    {
        var stay = HotelStay.Prepare(
            "STY-202609-000002", PropertyId.New(), Guid.NewGuid(), "RSV-2", Guid.NewGuid(), "Primary", null,
            Guid.NewGuid(), "101", Guid.NewGuid(), "STD",
            HotelStayPeriod.Create(new(2026, 9, 8), new(2026, 9, 9)),
            HotelStayGuestCount.Create(2, 1), "WalkIn", DateTime.UtcNow, null);

        stay.ReconcileGuestCountsAfterOccupantRemoval(1, 0, DateTime.UtcNow, null);
        stay.Adults.Should().Be(1);
        stay.Children.Should().Be(0);
    }
}
