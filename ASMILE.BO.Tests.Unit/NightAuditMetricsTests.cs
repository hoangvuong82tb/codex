using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.FrontOffice.NightAudit;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class NightAuditMetricsTests
{
    [Fact]
    public void CalculateOccupancy_uses_room_inventory_not_in_house_stay_count()
    {
        var rooms = new[]
        {
            new RoomOccupancyDto(Guid.NewGuid(), "101", "STD", "Ready", "Occupied", "ST-001"),
            new RoomOccupancyDto(Guid.NewGuid(), "102", "STD", "Ready", "Vacant", null),
            new RoomOccupancyDto(Guid.NewGuid(), "103", "DLX", "Ready", "occupied", "ST-002"),
            new RoomOccupancyDto(Guid.NewGuid(), "104", "DLX", "OutOfOrder", "Vacant", null)
        };

        var result = NightAuditMetrics.CalculateOccupancy(rooms);

        result.RoomsSold.Should().Be(2);
        result.TotalRooms.Should().Be(4);
        result.OccupancyRate.Should().Be(50m);
    }

    [Fact]
    public void CalculateOccupancy_returns_zero_for_an_empty_inventory()
    {
        var result = NightAuditMetrics.CalculateOccupancy(Array.Empty<RoomOccupancyDto>());

        result.Should().Be(new NightAuditOccupancyMetrics(0, 0, 0m));
    }
}
