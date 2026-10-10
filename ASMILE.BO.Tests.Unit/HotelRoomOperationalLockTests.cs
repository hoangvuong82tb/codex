using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelRoomOperationalLockTests
{
    [Fact]
    public void Operational_lock_keeps_reason_and_expiry_and_can_be_extended_only_forward()
    {
        var property = PropertyId.New();
        var actor = ActorId.New();
        var now = new DateTime(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);
        var room = HotelRoom.Create(property, Guid.NewGuid(), "101", null, "1", null, null, null, now, actor);

        room.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfOrder, now, actor, "Sửa chữa máy lạnh", now.AddHours(4));
        room.OperationalStatus.Should().Be(HotelRoomOperationalStatus.OutOfOrder);
        room.OperationalStatusChangedAtUtc.Should().Be(now);
        room.OperationalStatusExpiresAtUtc.Should().Be(now.AddHours(4));
        room.OperationalStatusReason.Should().Be("Sửa chữa máy lạnh");

        room.ExtendOperationalLock(now.AddHours(8), "Chưa hoàn tất sửa chữa", now.AddHours(1), actor);
        room.OperationalStatusExpiresAtUtc.Should().Be(now.AddHours(8));
        room.OperationalStatusReason.Should().Be("Chưa hoàn tất sửa chữa");

        Action earlier = () => room.ExtendOperationalLock(now.AddHours(7), "Gia hạn sai", now.AddHours(1), actor);
        earlier.Should().Throw<ArgumentException>().WithMessage("HOTEL_ROOM_LOCK_EXPIRY_NOT_LATER");

        room.ChangeOperationalStatus(HotelRoomOperationalStatus.Available, now.AddHours(9), actor);
        room.OperationalStatusExpiresAtUtc.Should().BeNull();
        room.OperationalStatusReason.Should().BeNull();
    }

    [Fact]
    public async Task Operational_lock_metadata_is_read_back_through_the_real_room_repository()
    {
        var property = PropertyId.New();
        var actor = ActorId.New();
        var now = new DateTime(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);
        using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(property, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, actor);
        var room = HotelRoom.Create(property, type.HotelRoomTypeId, "102", null, "1", null, null, null, now, actor);
        room.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfService, now, actor, "Bảo trì thang máy", now.AddDays(2));
        db.AddRange(type, room);
        await db.SaveChangesAsync();

        var dto = await new EfHotelRoomMasterDataRepository(db).GetRoomDtoAsync(property.Value, room.HotelRoomId);
        dto.Should().NotBeNull();
        dto!.OperationalStatus.Should().Be(HotelRoomOperationalStatus.OutOfService);
        dto.OperationalStatusChangedAtUtc.Should().Be(now);
        dto.OperationalStatusExpiresAtUtc.Should().Be(now.AddDays(2));
        dto.OperationalStatusReason.Should().Be("Bảo trì thang máy");
    }

    [Fact]
    public async Task Physical_room_attributes_are_persisted_and_read_back_as_real_data()
    {
        var property = PropertyId.New();
        var actor = ActorId.New();
        var now = new DateTime(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);
        using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(property, "DLX", "Deluxe", null, 1, 0, 2, 1, 1, 1, now, actor);
        var room = HotelRoom.Create(property, type.HotelRoomTypeId, "201", "Deluxe 201", "2", "A", "West", null, now, actor, "Normal", "Biển", "King", true, true);
        db.AddRange(type, room);
        await db.SaveChangesAsync();

        var dto = await new EfHotelRoomMasterDataRepository(db).GetRoomDtoAsync(property.Value, room.HotelRoomId);
        dto.Should().NotBeNull();
        dto!.RoomCondition.Should().Be("Normal");
        dto.View.Should().Be("Biển");
        dto.BedType.Should().Be("King");
        dto.IsSmoking.Should().BeTrue();
        dto.IsConnecting.Should().BeTrue();
    }
}
