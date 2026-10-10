using ASMILE.BO.Core.FrontOffice.Folio;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FolioRoutingTests
{
    [Fact]
    public void FOLIO_ROUTING_DOM_001_creates_active_property_scoped_rule()
    {
        var propertyId = Guid.NewGuid();
        var rule = HotelFolioRoutingRule.Create(
            propertyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Room charges to master",
            "RoomCharge",
            100m,
            1,
            "Nightly room rate",
            DateTime.UtcNow,
            null);

        rule.PropertyId.Should().Be(propertyId);
        rule.EntryType.Should().Be("RoomCharge");
        rule.Percent.Should().Be(100m);
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FOLIO_ROUTING_DOM_002_rejects_same_source_and_target()
    {
        var folioId = Guid.NewGuid();

        Action action = () => HotelFolioRoutingRule.Create(
            Guid.NewGuid(), folioId, folioId, "Invalid", "All", 100m, 0, null, DateTime.UtcNow, null);

        action.Should().Throw<ArgumentException>().WithMessage("*different*");
    }

    [Fact]
    public void FOLIO_ROUTING_DOM_003_rejects_invalid_percent()
    {
        Action action = () => HotelFolioRoutingRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Invalid", "All", 0m, 0, null, DateTime.UtcNow, null);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FOLIO_ROUTING_DOM_004_update_and_deactivate_keep_audit_timestamps_current()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-2);
        var updatedAt = DateTime.UtcNow;
        var rule = HotelFolioRoutingRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Initial", "All", 50m, 2, null, createdAt, null);

        rule.Update("Updated", "Restaurant", 75m, 3, "Changed", updatedAt, null);
        rule.SetActive(false, updatedAt.AddSeconds(1), null);

        rule.Name.Should().Be("Updated");
        rule.EntryType.Should().Be("Restaurant");
        rule.Percent.Should().Be(75m);
        rule.Priority.Should().Be(3);
        rule.IsActive.Should().BeFalse();
        rule.UpdatedAtUtc.Should().BeAfter(createdAt);
    }
}
