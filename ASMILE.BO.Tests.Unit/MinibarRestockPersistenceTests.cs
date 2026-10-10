using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class MinibarRestockPersistenceTests
{
    [Fact]
    public void Consumption_snapshots_vat_and_gross_amount()
    {
        var line = MinibarConsumption.Create(Guid.NewGuid(), Guid.NewGuid(), "WATER", "Nước Lavie", 2, 25_000m, 10m);

        line.LineAmount.Should().Be(50_000m);
        line.VatAmount.Should().Be(5_000m);
        line.GrossAmount.Should().Be(55_000m);
    }

    [Fact]
    public async Task Restock_persists_real_transaction_and_is_idempotent()
    {
        var propertyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "201", null, "2", null, null, null, DateTime.UtcNow, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        var stay = HotelStay.Prepare(
            "STY-202609-000001", new PropertyId(propertyId), Guid.NewGuid(), "RSV-202609-000001",
            Guid.NewGuid(), "Khách VIP", "0900000000", room.HotelRoomId, room.RoomNumber,
            roomType.HotelRoomTypeId, roomType.Code,
            HotelStayPeriod.Create(new(2026, 9, 24), new(2026, 9, 25)),
            HotelStayGuestCount.Create(1, 0), "Direct", DateTime.UtcNow, null);
        stay.MarkVipService(true, DateTime.UtcNow, null);
        db.HotelStays.Add(stay);
        var warehouse = new Warehouse
        {
            WarehouseCode = "KHO-MINIBAR",
            WarehouseName = "Kho minibar",
            IsActive = true,
            IsConsumptionWarehouse = true
        };
        var item = new Item
        {
            ItemCode = "WATER",
            ItemName = "Nước Lavie",
            Unit = "Chai",
            IsActive = true,
            IsMinibarItem = true,
            RetailPrice = 25000,
            MinibarStandardQuantity = 2
        };
        db.Warehouses.Add(warehouse);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        db.StockBalances.Add(new StockBalance
        {
            WarehouseId = warehouse.Id,
            ItemId = item.Id,
            OpeningQty = 10,
            ClosingQty = 10,
            AvgPrice = 15000
        });
        await db.SaveChangesAsync();

        var service = new EfMinibarCommandService(db);
        var command = new RestockMinibarCommand(
            transactionId, propertyId, room.HotelRoomId, "WATER", "Nước Lavie", 2,
            "KHO-MINIBAR", actorId, DateTime.UtcNow, "Bổ sung sau kiểm tra");

        await service.RestockAsync(command);
        await service.RestockAsync(command);

        var row = await db.MinibarRestockTransactions.SingleAsync();
        row.TransactionId.Should().Be(transactionId);
        row.PropertyId.Should().Be(propertyId);
        row.HotelRoomId.Should().Be(room.HotelRoomId);
        row.ItemCode.Should().Be("WATER");
        row.Quantity.Should().Be(2);
        row.ActorId.Should().Be(actorId);

        var snapshot = await new EfMinibarQueryService(db).QueryAsync(propertyId);
        snapshot.Should().ContainSingle(x => x.RoomId == room.HotelRoomId
            && x.GuestName == "Khách VIP"
            && x.IsVip
            && x.RestockQuantity == 2
            && x.RestockAmount == 50_000m);
    }

    [Fact]
    public async Task Inventory_persists_detail_lines_and_is_idempotent()
    {
        var propertyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "202", null, "2", null, null, null, DateTime.UtcNow, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        await db.SaveChangesAsync();

        var service = new EfMinibarCommandService(db);
        var command = new InventoryMinibarCommand(
            inventoryId, propertyId, room.HotelRoomId, null, actorId, DateTime.UtcNow,
            [
                new MinibarInventoryLineInput("WATER", "Nước Lavie", 3, 25000),
                new MinibarInventoryLineInput("COLA", "Coca Cola", 2, 35000)
            ],
            "Kiểm kê đầu ca");

        await service.InventoryAsync(command);
        await service.InventoryAsync(command);

        var audit = await db.MinibarInventoryAudits.Include(x => x.Lines).SingleAsync();
        audit.InventoryId.Should().Be(inventoryId);
        audit.PropertyId.Should().Be(propertyId);
        audit.HotelRoomId.Should().Be(room.HotelRoomId);
        audit.CountedBy.Should().Be(actorId);
        audit.Lines.Should().HaveCount(2);
        audit.Lines.Should().Contain(x => x.ItemCode == "WATER" && x.Quantity == 3);

        var snapshot = await new EfMinibarQueryService(db).QueryAsync(propertyId);
        var snapshotRow = snapshot.Single(x => x.RoomId == room.HotelRoomId);
        snapshotRow.LastInventoryAtUtc.Should().NotBeNull();
        snapshotRow.InventoryLineCount.Should().Be(2);
        snapshotRow.InventoryItems.Should().Contain(item => item.ItemCode == "WATER" && item.Quantity == 3);
    }

    [Fact]
    public async Task Restock_rejects_missing_or_insufficient_real_stock()
    {
        var propertyId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var db = CreateDb();
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "203", null, "2", null, null, null, DateTime.UtcNow, null);
        var warehouse = new Warehouse { WarehouseCode = "KHO-MINIBAR", WarehouseName = "Kho minibar", IsActive = true };
        var item = new Item { ItemCode = "WATER", ItemName = "Nước Lavie", Unit = "Chai", IsActive = true, IsMinibarItem = true };
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.Warehouses.Add(warehouse);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        db.StockBalances.Add(new StockBalance { WarehouseId = warehouse.Id, ItemId = item.Id, ClosingQty = 1 });
        await db.SaveChangesAsync();

        var service = new EfMinibarCommandService(db);
        var command = new RestockMinibarCommand(Guid.NewGuid(), propertyId, room.HotelRoomId, "WATER", "Nước Lavie", 2, "KHO-MINIBAR", actorId, DateTime.UtcNow);

        var action = () => service.RestockAsync(command);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("MINIBAR_STOCK_INSUFFICIENT");
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
