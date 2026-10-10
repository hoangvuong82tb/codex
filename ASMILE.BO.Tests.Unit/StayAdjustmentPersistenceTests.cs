using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.FrontOffice.Guest;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class StayAdjustmentPersistenceTests
{
    [Fact]
    public async Task AdjustStay_commits_stay_history_and_folio_room_charge_together()
    {
        var propertyId = Guid.NewGuid();
        var roomType = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var stayId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var type = HotelRoomType.Create(new PropertyId(propertyId), "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), type.HotelRoomTypeId, "201", null, "2", null, null, null, now, null);
        var stay = HotelStay.Prepare(
            "STY-202609-000001",
            new PropertyId(propertyId),
            null,
            null,
            Guid.NewGuid(),
            "Khách test",
            null,
            room.HotelRoomId,
            room.RoomNumber,
            type.HotelRoomTypeId,
            type.Code,
            HotelStayPeriod.Create(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 9)),
            HotelStayGuestCount.Create(1, 0),
            "WalkIn",
            now,
            null);
        stay.CheckIn(now, null);
        stay.ClearDomainEvents();
        var folio = HotelFolio.Create(propertyId, stay.HotelStayId, "FOL-202609-000001", now, null);

        db.HotelRoomTypes.Add(type);
        db.HotelRooms.Add(room);
        db.HotelStays.Add(stay);
        db.HotelFolios.Add(folio);
        db.Entry(stay).Property(x => x.RowVersion).CurrentValue = new byte[8];
        db.Entry(folio).Property(x => x.RowVersion).CurrentValue = new byte[8];
        await db.SaveChangesAsync();
        var stayRowVersion = stay.RowVersion;
        var folioRowVersion = folio.RowVersion;
        stayRowVersion.Should().NotBeNull();
        stayRowVersion!.Length.Should().Be(8);
        folioRowVersion.Should().NotBeNull();
        folioRowVersion!.Length.Should().Be(8);

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new StayAdjustmentApplicationService(
            new EfHotelStayRepository(db),
            new EfHotelRoomMasterDataRepository(db),
            new EfReservationRepository(db),
            new EfHotelFolioRepository(db),
            new FixedFolioNumberGenerator(),
            new TestPersistence(db),
            accessor,
            new AllowAllAuthorization(),
            new FixedClock(now.AddMinutes(1)));

        var result = await service.AdjustAsync(
            new AdjustStayCommand(
                stay.HotelStayId,
                stayRowVersion,
                room.HotelRoomId,
                new DateOnly(2026, 9, 11),
                500m,
                "Khách gia hạn"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.Stay.ScheduledDepartureDate.Should().Be(new DateOnly(2026, 9, 11));
        result.Value.FolioEntry.Should().NotBeNull();
        result.Value.FolioEntry!.EntryType.Should().Be(HotelFolioEntryType.RoomCharge);
        result.Value.FolioEntry.Amount.Should().Be(500m);

        db.HotelStayHistories.Should().Contain(x => x.HotelStayId == stay.HotelStayId && x.Action == "StayExtended");
        db.HotelFolioEntries.Should().ContainSingle(x =>
            x.HotelFolioId == folio.HotelFolioId
            && x.EntryType == HotelFolioEntryType.RoomCharge
            && x.Amount == 500m);
    }

    [Fact]
    public async Task AdjustStay_without_charge_updates_current_room_without_folio_permission_or_empty_folio()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(new PropertyId(propertyId), "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), type.HotelRoomTypeId, "201", null, "2", null, null, null, now, null);
        var stay = HotelStay.Prepare(
            "STY-202609-000002", new PropertyId(propertyId), null, null, Guid.NewGuid(), "Khách test", "0900000000",
            room.HotelRoomId, room.RoomNumber, type.HotelRoomTypeId, type.Code,
            HotelStayPeriod.Create(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 9)),
            HotelStayGuestCount.Create(1, 0), "WalkIn", now, null);
        stay.CheckIn(now, null);
        stay.ClearDomainEvents();
        db.HotelRoomTypes.Add(type);
        db.HotelRooms.Add(room);
        db.HotelStays.Add(stay);
        db.Entry(stay).Property(x => x.RowVersion).CurrentValue = new byte[8];
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new StayAdjustmentApplicationService(
            new EfHotelStayRepository(db),
            new EfHotelRoomMasterDataRepository(db),
            new EfReservationRepository(db),
            new EfHotelFolioRepository(db),
            new FixedFolioNumberGenerator(),
            new TestPersistence(db),
            accessor,
            new StayUpdateOnlyAuthorization(),
            new FixedClock(now.AddMinutes(1)));

        var result = await service.AdjustAsync(
            new AdjustStayCommand(stay.HotelStayId, stay.RowVersion!, room.HotelRoomId, new DateOnly(2026, 9, 11), 0m, "Gia hạn không chênh lệch"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.Stay.ScheduledDepartureDate.Should().Be(new DateOnly(2026, 9, 11));
        result.Value.Folio.Should().BeNull();
        result.Value.FolioEntry.Should().BeNull();
        db.HotelFolios.Should().BeEmpty();
        db.HotelFolioEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStayInformation_persists_guest_snapshot_requests_notes_and_history()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(new PropertyId(propertyId), "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), type.HotelRoomTypeId, "201", null, "2", null, null, null, now, null);
        var stay = HotelStay.Prepare(
            "STY-202609-000003", new PropertyId(propertyId), null, null, Guid.NewGuid(), "Khách cũ", "0900000000",
            room.HotelRoomId, room.RoomNumber, type.HotelRoomTypeId, type.Code,
            HotelStayPeriod.Create(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10)),
            HotelStayGuestCount.Create(1, 1), "WalkIn", now, null);
        stay.CheckIn(now, null);
        stay.ClearDomainEvents();
        db.HotelRoomTypes.Add(type);
        db.HotelRooms.Add(room);
        db.HotelStays.Add(stay);
        db.Entry(stay).Property(x => x.RowVersion).CurrentValue = new byte[8];
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new HotelStayApplicationService(
            new EfHotelStayRepository(db),
            new FixedStayNumberGenerator(),
            new EfReservationRepository(db),
            new EfHotelRoomMasterDataRepository(db),
            new EfHotelStayOccupantRepository(db),
            new EfGuestProfileRepository(db),
            new EfHotelFolioRepository(db),
            new TestPersistence(db),
            accessor,
            new AllowAllAuthorization(),
            new FixedClock(now.AddMinutes(1)));

        var result = await service.UpdateInformationAsync(
            new UpdateStayInformationCommand(
                stay.HotelStayId, stay.RowVersion!, "Khách đã cập nhật", "0912345678",
                "Phòng không hút thuốc", "Đã xác nhận giờ đến", "Khách yêu cầu cập nhật"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.GuestName.Should().Be("Khách đã cập nhật");
        result.Value.GuestPhone.Should().Be("0912345678");
        result.Value.SpecialRequests.Should().Be("Phòng không hút thuốc");
        result.Value.InternalNotes.Should().Be("Đã xác nhận giờ đến");
        db.HotelStayHistories.Should().ContainSingle(x =>
            x.HotelStayId == stay.HotelStayId
            && x.Action == "StayInformationUpdated"
            && x.SafeMetadata!.Contains("Fields=GuestName,GuestPhone,SpecialRequests,InternalNotes"));
    }

    private static FrontOfficeExecutionContext Context(Guid propertyId)
    {
        var id = new PropertyId(propertyId);
        return new FrontOfficeExecutionContext(
            new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "test-session", "test-workstation", false),
            new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(id, new DateOnly(2026, 9, 8), "Test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "test-trace"));
    }

    private sealed class AllowAllAuthorization : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => true;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true);
        public bool HasPermission(ASMILE.BO.Core.Security.Permission permission) => true;
        public void Demand(ASMILE.BO.Core.Security.Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class StayUpdateOnlyAuthorization : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => permissionCode == HotelStayPermissions.Update;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(HasPermission(permissionCode));
        public bool HasPermission(ASMILE.BO.Core.Security.Permission permission) => false;
        public void Demand(ASMILE.BO.Core.Security.Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow => now;
    }

    private sealed class FixedFolioNumberGenerator : IHotelFolioNumberGenerator
    {
        public Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
            => Task.FromResult("FOL-TEST-GENERATED");
    }

    private sealed class FixedStayNumberGenerator : IHotelStayNumberGenerator
    {
        public Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
            => Task.FromResult("STY-TEST-GENERATED");
    }

    private sealed class TestPersistence(AppDbContext db) : IFrontOfficePersistenceSession
    {
        public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) { }
        public void StageAudit(FrontOfficeAuditIntent intent) { }
        public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
            => Result<int>.Success(await db.SaveChangesAsync(cancellationToken));
    }
}
