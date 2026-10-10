using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class HousekeepingSupplyPersistenceTests
{
    [Fact]
    public async Task Linen_service_reads_and_aggregates_persisted_transactions()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "101", null, "1", null, null, null, DateTime.UtcNow, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.HousekeepingLinenTypes.Add(HousekeepingLinenType.Create(propertyId, "BEDSHEET-KING", "King sheet", "Bedding", 4, 200));
        await db.SaveChangesAsync();

        var service = CreateService(db, propertyId);
        await service.RecordLinenTransactionAsync(new LinenTransaction(Guid.NewGuid(), "BEDSHEET-KING", room.HotelRoomId, LinenAction.Restocked, 4, DateTime.UtcNow.AddHours(-2), null), default);
        await service.RecordLinenTransactionAsync(new LinenTransaction(Guid.NewGuid(), "BEDSHEET-KING", room.HotelRoomId, LinenAction.Issued, 1, DateTime.UtcNow, null), default);

        var inventory = await ((ILinenService)service).GetInventoryAsync(propertyId, "1", default);

        inventory.Should().ContainSingle(x => x.RoomId == room.HotelRoomId && x.RoomNumber == "101" && x.LinenCode == "BEDSHEET-KING" && x.Quantity == 3 && x.LastRestockedUtc.HasValue);
    }

    [Fact]
    public async Task Amenity_service_persists_refill_request_only_for_real_low_stock()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "102", null, "1", null, null, null, DateTime.UtcNow, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.HousekeepingAmenityTypes.Add(HousekeepingAmenityType.Create(propertyId, "SHAMPOO", "Shampoo", "Bathroom", 5, 2));
        db.HousekeepingAmenityBalances.Add(HousekeepingAmenityBalance.Create(propertyId, room.HotelRoomId, "SHAMPOO", 2, 5, 10, DateTime.UtcNow.AddHours(-1)));
        await db.SaveChangesAsync();

        var service = CreateService(db, propertyId);
        var request = await service.CreateRefillRequestAsync(room.HotelRoomId, default);

        request.Items.Should().ContainSingle(x => x.AmenityCode == "SHAMPOO" && x.Quantity == 3);
        var inventory = await ((IAmenityService)service).GetInventoryAsync(propertyId, "1", default);
        inventory.Should().ContainSingle(x => x.RoomId == room.HotelRoomId && x.RoomNumber == "102" && x.AmenityCode == "SHAMPOO");
        (await db.HousekeepingAmenityRefillRequests.CountAsync()).Should().Be(1);
        (await db.HousekeepingAmenityRefillLines.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Linen_service_rejects_outgoing_quantity_above_persisted_stock()
    {
        var propertyId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "103", null, "1", null, null, null, DateTime.UtcNow, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.HousekeepingLinenTypes.Add(HousekeepingLinenType.Create(propertyId, "TOWEL", "Towel", "Bath", 2, 100));
        await db.SaveChangesAsync();

        var service = CreateService(db, propertyId);
        await service.RecordLinenTransactionAsync(new LinenTransaction(Guid.NewGuid(), "TOWEL", room.HotelRoomId, LinenAction.Restocked, 2, DateTime.UtcNow, null), default);

        var act = () => service.RecordLinenTransactionAsync(
            new LinenTransaction(Guid.NewGuid(), "TOWEL", room.HotelRoomId, LinenAction.Issued, 3, DateTime.UtcNow, null), default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("HK_LINEN_STOCK_INSUFFICIENT");
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static EfHousekeepingSupplyService CreateService(AppDbContext db, Guid propertyId)
    {
        var context = new TestContextAccessor(propertyId);
        return new EfHousekeepingSupplyService(db, context, new TestClock());
    }

    private sealed class TestContextAccessor(Guid propertyId) : IFrontOfficeExecutionContextAccessor
    {
        public FrontOfficeExecutionContext? Current { get; } = new(
            new ActorContext(null, new HashSet<string>(), null, null, false),
            new PropertyContext(new PropertyId(propertyId), "UTC", "VND"),
            new BusinessDateContext(new PropertyId(propertyId), DateOnly.FromDateTime(DateTime.UtcNow), "test"),
            new CorrelationContext(new CorrelationId(Guid.NewGuid()), null, null, null));

        public IDisposable BeginScope(FrontOfficeExecutionContext context) => throw new NotSupportedException();
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow => new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    }
}
