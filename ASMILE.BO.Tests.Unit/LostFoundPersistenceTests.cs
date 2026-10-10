using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class LostFoundPersistenceTests
{
    [Fact]
    public async Task LF_REAL_001_register_update_and_operational_transitions_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();

        var service = new EfLostFoundCommandService(db);
        var created = await service.RegisterAsync(new RegisterLostFoundItemCommand(
            propertyId.Value,
            "LF-TEST-001",
            "Điện thoại",
            LostFoundCategory.Electronics,
            "Tìm thấy trong phòng 405",
            "Phòng 405",
            DateTime.UtcNow.AddMinutes(-10),
            "Housekeeping",
            null,
            "Đen",
            "Apple",
            "SN-001",
            "Gọi khách theo hồ sơ lưu trú",
            ActorId.New().Value));

        created.ItemNumber.Should().Be("LF-TEST-001");
        (await db.LostFoundItems.CountAsync()).Should().Be(1);

        await service.UpdateAsync(new UpdateLostFoundItemCommand(
            created.ItemId,
            propertyId.Value,
            "Điện thoại iPhone",
            LostFoundCategory.Electronics,
            "Đã cập nhật mô tả",
            "Đen",
            "Apple",
            "SN-002",
            "Ghi chú mới",
            ActorId.New().Value));
        var updated = await db.LostFoundItems.SingleAsync();
        updated.ItemName.Should().Be("Điện thoại iPhone");
        updated.SerialNumber.Should().Be("SN-002");
        (await db.LostFoundHistories.CountAsync()).Should().Be(1);

        await service.StoreAsync(new StoreLostFoundItemCommand(
            created.ItemId,
            propertyId.Value,
            "Kho A - Kệ 1",
            DateTime.UtcNow,
            ActorId.New().Value));
        await service.ContactAsync(new ContactLostFoundItemCommand(
            created.ItemId,
            propertyId.Value,
            "Front Office",
            DateTime.UtcNow,
            "Phone",
            "Đang chờ phản hồi",
            "Đã gọi theo số trong hồ sơ",
            ActorId.New().Value));

        var queryService = new EfLostFoundQueryService(db);
        var queried = await queryService.SearchAsync(
            new LostFoundSearchFilter(propertyId.Value, "LF-TEST-001", null, null, null, null, null),
            1,
            20);
        queried.Items.Should().ContainSingle();
        queried.Items.Single().ContactResult.Should().Be("Đang chờ phản hồi");

        await service.ReturnAsync(new ReturnLostFoundItemCommand(
            created.ItemId,
            propertyId.Value,
            "Front Office",
            "Bùi Thanh Hà",
            DateTime.UtcNow,
            ActorId.New().Value));

        updated = await db.LostFoundItems.SingleAsync();
        updated.Status.Should().Be(LostFoundStatus.Returned);
        updated.ContactedBy.Should().Be("Front Office");
        (await db.LostFoundHistories.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task LF_REAL_002_delete_only_removes_new_real_item()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();

        var service = new EfLostFoundCommandService(db);
        var created = await service.RegisterAsync(new RegisterLostFoundItemCommand(
            propertyId.Value,
            "LF-TEST-002",
            "Ví",
            LostFoundCategory.Others,
            null,
            "Phòng 301",
            DateTime.UtcNow,
            "Housekeeping",
            null,
            null,
            null,
            null,
            null,
            null));

        await service.DeleteAsync(new DeleteLostFoundItemCommand(created.ItemId, propertyId.Value, null));
        (await db.LostFoundItems.CountAsync()).Should().Be(0);
    }
}
