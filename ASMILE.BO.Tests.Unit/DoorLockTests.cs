using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class DoorLockTests
{
    [Fact]
    public async Task DL_004_to_010_key_card_lifecycle_is_persisted_and_scoped()
    {
        var property = PropertyId.New();
        var actor = ActorId.New();
        var now = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);
        using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var roomType = HotelRoomType.Create(property, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, actor);
        var room = HotelRoom.Create(property, roomType.HotelRoomTypeId, "101", null, "1", null, null, null, now, actor);
        var guest = GuestProfile.Create(property.Value, "GST-001", "Test Guest", null, null, GuestSource.Direct, now, actor);
        var stay = HotelStay.Prepare("STY-001", property, null, null, guest.GuestProfileId, guest.FullName, null,
            room.HotelRoomId, room.RoomNumber, roomType.HotelRoomTypeId, roomType.Code,
            new HotelStayPeriod(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10)),
            new HotelStayGuestCount(1, 0), "Direct", now, actor);
        stay.CheckIn(now, actor);
        db.AddRange(roomType, room, guest, stay);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(property, actor));
        var service = new EfKeyCardService(db, accessor, new FixedClock(now));

        var issued = await service.IssueKeyAsync(guest.GuestProfileId, stay.HotelStayId, room.HotelRoomId,
            AccessLevel.Master, now.AddDays(3), 12, default);
        issued.Status.Should().Be(KeyStatus.Active);
        issued.Level.Should().Be(AccessLevel.Master);
        issued.EncoderId.Should().Be(12);
        issued.IsMaster.Should().BeTrue();
        db.KeyCardCredentials.Should().ContainSingle(x => x.CredentialId == issued.KeyId && x.AccessLevel == "Master" && x.EncoderId == 12 && x.IsMaster);

        var duplicate = await service.IssueDuplicateAsync(issued.KeyId, default);
        duplicate.KeyId.Should().NotBe(issued.KeyId);
        duplicate.Level.Should().Be(AccessLevel.Master);
        duplicate.EncoderId.Should().Be(12);
        (await service.GetKeysForStayAsync(stay.HotelStayId, default)).Should().HaveCount(2);

        (await service.SuspendKeyAsync(issued.KeyId, default)).Status.Should().Be(KeyStatus.Suspended);
        (await service.ReactivateKeyAsync(issued.KeyId, default)).Status.Should().Be(KeyStatus.Active);
        (await service.ReportLostAsync(issued.KeyId, default)).Status.Should().Be(KeyStatus.Lost);
        (await service.RevokeKeyAsync(duplicate.KeyId, "Checkout", actor.Value, default)).Status.Should().Be(KeyStatus.Revoked);

        var inventory = await service.GetInventoryAsync(property.Value, default);
        inventory.TotalKeys.Should().Be(2);
        inventory.Lost.Should().Be(1);
        inventory.Revoked.Should().Be(1);
        db.KeyCardEvents.Count().Should().BeGreaterThanOrEqualTo(6);
    }

    [Fact] public void DL_014_events_unique() => typeof(DoorLockEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();

    private static FrontOfficeExecutionContext CreateContext(PropertyId property, ActorId actor)
        => new(new ActorContext(actor, new HashSet<string> { "Reception" }, "session", "workstation", false),
            new PropertyContext(property, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(property, new DateOnly(2026, 9, 7), "Test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }
}
