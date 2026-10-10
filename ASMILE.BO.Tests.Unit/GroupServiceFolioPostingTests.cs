using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Settlement;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupServiceFolioPostingTests
{
    [Fact]
    public async Task Group_check_in_rejects_assignment_without_real_reservation_chain()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-CHECKIN-001", "Đoàn check-in", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        group.AddRoom(Guid.NewGuid(), "101", "STD", "Khách thật", 2, 0, now);
        group.Confirm(now);
        db.FrontOfficeGroupReservations.Add(group);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupCheckInCommandService(
            db,
            new TestPersistence(db),
            accessor,
            new FixedClock(now.AddMinutes(1)),
            new FixedNumberGenerator(),
            new FixedNumberGenerator(),
            new FixedNumberGenerator());

        await FluentActions.Invoking(() => service.CheckInAsync(
                group.GroupReservationId,
                group.RoomingList.Select(x => x.GroupRoomAssignmentId).ToArray(),
                CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GROUP_ROOM_RESERVATION_REQUIRED");

        (await db.FrontOfficeGroupReservations.SingleAsync()).Status.Should().Be(GroupReservationStatus.Confirmed);
        (await db.FrontOfficeGroupReservations.Include(x => x.RoomingList).SingleAsync()).RoomingList.Single().IsCheckedIn.Should().BeFalse();
    }

    [Fact]
    public async Task Completed_group_service_posts_to_real_folio_once()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-POST-001", "Đoàn post Folio", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var serviceOrder = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "SPA-01", "Massage đoàn", "Spa", new(2026, 9, 8), new(14, 0), 60, 1, 350m, 0m, 0m, 0m, "MasterFolio", now, null);
        serviceOrder.Confirm(now, null);
        serviceOrder.Complete(now, null);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "FOL-GRP-001", now, null);

        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.Add(serviceOrder);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupServiceOrderService(
            db,
            new TestPersistence(db),
            accessor,
            new FixedClock(now.AddMinutes(1)),
            new EfHotelFolioRepository(db));

        var posted = await service.PostToFolioAsync(
            new PostGroupServiceOrderToFolioRequest(serviceOrder.GroupServiceOrderId, folio.HotelFolioId),
            CancellationToken.None);

        posted.Status.Should().Be(GroupServiceOrderStatus.PostedToFolio);
        db.HotelFolioEntries.Should().ContainSingle(x =>
            x.HotelFolioId == folio.HotelFolioId
            && x.ReferenceType == "GroupServiceOrder"
            && x.ReferenceId == serviceOrder.GroupServiceOrderId
            && x.EntryType == HotelFolioEntryType.Spa
            && x.Amount == 350m);
        db.FrontOfficeGroupServiceOrders.Single(x => x.GroupServiceOrderId == serviceOrder.GroupServiceOrderId).Status
            .Should().Be(GroupServiceOrderStatus.PostedToFolio);

        await FluentActions.Invoking(() => service.PostToFolioAsync(
                new PostGroupServiceOrderToFolioRequest(serviceOrder.GroupServiceOrderId, folio.HotelFolioId),
                CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GROUP_SERVICE_MUST_BE_COMPLETED_BEFORE_FOLIO");
    }

    [Fact]
    public async Task Batch_posts_completed_group_services_to_selected_real_folio_atomically()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-BATCH-001", "Đoàn batch", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var first = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "SPA-01", "Massage", "Spa", new(2026, 9, 8), new(14, 0), 60, 1, 350m, 0m, 0m, 0m, "MasterFolio", now, null);
        var second = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "REST-01", "Set menu", "Restaurant", new(2026, 9, 8), new(18, 0), 90, 2, 500m, 0m, 0m, 0m, "MasterFolio", now, null);
        first.Confirm(now, null); first.Complete(now, null);
        second.Confirm(now, null); second.Complete(now, null);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "FOL-BATCH-001", now, null);
        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.AddRange(first, second);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupServiceOrderService(
            db, new TestPersistence(db), accessor, new FixedClock(now.AddMinutes(1)), new EfHotelFolioRepository(db));

        var result = await service.PostBatchToFolioAsync(
            new PostGroupServiceOrdersToFolioRequest([first.GroupServiceOrderId, second.GroupServiceOrderId], folio.HotelFolioId),
            CancellationToken.None);

        result.PostedCount.Should().Be(2);
        result.TotalAmount.Should().Be(1_350m);
        db.HotelFolioEntries.Count(x => x.HotelFolioId == folio.HotelFolioId && x.ReferenceType == "GroupServiceOrder").Should().Be(2);
        db.FrontOfficeGroupServiceOrders.Where(x => x.GroupReservationId == group.GroupReservationId)
            .Select(x => x.Status).Should().OnlyContain(x => x == GroupServiceOrderStatus.PostedToFolio);
    }

    [Fact]
    public async Task Batch_rejects_non_completed_order_without_writing_any_entry()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-BATCH-002", "Đoàn batch lỗi", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var completed = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "SPA-02", "Massage", "Spa", new(2026, 9, 8), new(14, 0), 60, 1, 350m, 0m, 0m, 0m, "MasterFolio", now, null);
        var pending = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "REST-02", "Set menu", "Restaurant", new(2026, 9, 8), new(18, 0), 90, 1, 500m, 0m, 0m, 0m, "MasterFolio", now, null);
        completed.Confirm(now, null); completed.Complete(now, null);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "FOL-BATCH-002", now, null);
        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.AddRange(completed, pending);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupServiceOrderService(
            db, new TestPersistence(db), accessor, new FixedClock(now.AddMinutes(1)), new EfHotelFolioRepository(db));

        await FluentActions.Invoking(() => service.PostBatchToFolioAsync(
                new PostGroupServiceOrdersToFolioRequest([completed.GroupServiceOrderId, pending.GroupServiceOrderId], folio.HotelFolioId),
                CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GROUP_SERVICE_BATCH_MUST_BE_COMPLETED");

        db.HotelFolioEntries.Should().BeEmpty();
        db.FrontOfficeGroupServiceOrders.Single(x => x.GroupServiceOrderId == completed.GroupServiceOrderId).Status
            .Should().Be(GroupServiceOrderStatus.Completed);
    }

    [Fact]
    public async Task Batch_status_commands_update_real_orders_atomically()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-BATCH-003", "Đoàn trạng thái", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var first = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "SPA-03", "Massage", "Spa", new(2026, 9, 8), new(14, 0), 60, 1, 350m, 0m, 0m, 0m, "MasterFolio", now, null);
        var second = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "REST-03", "Set menu", "Restaurant", new(2026, 9, 8), new(18, 0), 90, 1, 500m, 0m, 0m, 0m, "MasterFolio", now, null);
        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.AddRange(first, second);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupServiceOrderService(
            db, new TestPersistence(db), accessor, new FixedClock(now.AddMinutes(1)), new EfHotelFolioRepository(db));

        (await service.ChangeBatchStatusAsync(
            new BatchChangeGroupServiceOrderStatusRequest([first.GroupServiceOrderId, second.GroupServiceOrderId], GroupServiceOrderStatus.Confirmed),
            CancellationToken.None)).Status.Should().Be(GroupServiceOrderStatus.Confirmed);
        (await service.ChangeBatchStatusAsync(
            new BatchChangeGroupServiceOrderStatusRequest([first.GroupServiceOrderId, second.GroupServiceOrderId], GroupServiceOrderStatus.Completed),
            CancellationToken.None)).Status.Should().Be(GroupServiceOrderStatus.Completed);

        db.FrontOfficeGroupServiceOrders.Select(x => x.Status)
            .Should().OnlyContain(x => x == GroupServiceOrderStatus.Completed);
    }

    [Fact]
    public async Task Pending_group_service_can_be_updated_from_real_storage_and_a_completed_one_is_locked()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(propertyId, "GRP-EDIT-001", "Đoàn sửa dịch vụ", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var order = GroupServiceOrder.Create(propertyId, group.GroupReservationId, "GSV-EDIT-01", "Set menu cũ", "Restaurant", new(2026, 9, 8), new(12, 0), 60, 1, 200m, 0m, 0.08m, 0.05m, "MasterFolio", now, null);
        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.Add(order);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupServiceOrderService(
            db, new TestPersistence(db), accessor, new FixedClock(now.AddMinutes(1)), new EfHotelFolioRepository(db));

        var updated = await service.UpdateAsync(
            new UpdateGroupServiceOrderRequest(
                order.GroupServiceOrderId,
                "Set menu mới",
                "Restaurant",
                null,
                "Food & Beverage",
                "Sen Restaurant",
                "Phòng tiệc A",
                new(2026, 9, 8),
                new(13, 0),
                90,
                2,
                300m,
                0.10m,
                0.08m,
                0.05m,
                "MasterFolio",
                null,
                "Đã đổi suất phục vụ",
                null),
            CancellationToken.None);

        updated.ServiceName.Should().Be("Set menu mới");
        updated.Quantity.Should().Be(2);
        (await db.FrontOfficeGroupServiceOrders.SingleAsync(x => x.GroupServiceOrderId == order.GroupServiceOrderId)).UnitPrice
            .Should().Be(300m);

        await service.ChangeStatusAsync(
            new ChangeGroupServiceOrderStatusRequest(order.GroupServiceOrderId, GroupServiceOrderStatus.Confirmed, null),
            CancellationToken.None);
        await FluentActions.Invoking(() => service.UpdateAsync(
                new UpdateGroupServiceOrderRequest(
                    order.GroupServiceOrderId, "Không được sửa", "Restaurant", null, null, null, null,
                    new(2026, 9, 8), new(13, 0), 90, 2, 300m, 0m, 0m, 0m, "MasterFolio", null, null, null),
                CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GROUP_SERVICE_EDIT_REQUIRES_PENDING");
    }

    [Fact]
    public async Task Group_check_in_creates_real_reservation_stay_and_folio_chain()
    {
        var propertyId = Guid.NewGuid();
        var property = new PropertyId(propertyId);
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(property, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, null);
        var room = HotelRoom.Create(property, type.HotelRoomTypeId, "101", null, "1", null, null, null, now, null);
        var guest = GuestProfile.Create(propertyId, "GST-001", "Khách đoàn thật", "0900000000", null, GuestSource.Direct, now, null);
        var group = GroupReservation.Create(propertyId, "GRP-CHECKIN-002", "Đoàn tạo chain", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var assignment = group.AddRoom(room.HotelRoomId, room.RoomNumber, type.Code, guest.FullName, 2, 0, now);
        assignment.AssignGuest(guest.GuestProfileId, guest.FullName);
        group.Confirm(now);
        db.HotelRoomTypes.Add(type);
        db.HotelRooms.Add(room);
        db.GuestProfiles.Add(guest);
        db.FrontOfficeGroupReservations.Add(group);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var service = new EfGroupCheckInCommandService(
            db,
            new TestPersistence(db),
            accessor,
            new FixedClock(now.AddMinutes(1)),
            new FixedNumberGenerator(),
            new FixedNumberGenerator(),
            new FixedNumberGenerator());

        var result = await service.CheckInAsync(group.GroupReservationId, [assignment.GroupRoomAssignmentId], CancellationToken.None);

        result.Status.Should().Be(GroupReservationStatus.CheckedIn);
        var reservation = await db.FrontOfficeReservations.SingleAsync();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.AssignedRoomId.Should().Be(room.HotelRoomId);
        var stay = await db.HotelStays.SingleAsync();
        stay.ReservationId.Should().Be(reservation.ReservationId);
        stay.Status.Should().Be(HotelStayStatus.InHouse);
        var folio = await db.HotelFolios.SingleAsync();
        folio.HotelStayId.Should().Be(stay.HotelStayId);
        (await db.FrontOfficeGroupRoomAssignments.SingleAsync()).ReservationId.Should().Be(reservation.ReservationId);
    }

    [Fact]
    public async Task Group_checkout_closes_real_chain_issues_invoice_and_creates_housekeeping_task()
    {
        var propertyId = Guid.NewGuid();
        var property = new PropertyId(propertyId);
        var now = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var type = HotelRoomType.Create(property, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, null);
        var room = HotelRoom.Create(property, type.HotelRoomTypeId, "201", null, "2", null, null, null, now, null);
        var guest = GuestProfile.Create(propertyId, "GST-CHKOUT-001", "Khách checkout thật", "0900000001", null, GuestSource.Direct, now, null);
        var group = GroupReservation.Create(propertyId, "GRP-CHECKOUT-001", "Đoàn checkout thật", new(2026, 9, 8), new(2026, 9, 10), 1, 2, 0, now, null);
        var assignment = group.AddRoom(room.HotelRoomId, room.RoomNumber, type.Code, guest.FullName, 2, 0, now);
        assignment.AssignGuest(guest.GuestProfileId, guest.FullName);
        group.Confirm(now);
        db.HotelRoomTypes.Add(type);
        db.HotelRooms.Add(room);
        db.GuestProfiles.Add(guest);
        db.FrontOfficeGroupReservations.Add(group);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(Context(propertyId));
        var numberGenerator = new FixedNumberGenerator();
        var checkIn = new EfGroupCheckInCommandService(
            db,
            new TestPersistence(db),
            accessor,
            new FixedClock(now.AddMinutes(1)),
            numberGenerator,
            numberGenerator,
            numberGenerator);
        await checkIn.CheckInAsync(group.GroupReservationId, [assignment.GroupRoomAssignmentId], CancellationToken.None);

        var stay = await db.HotelStays.SingleAsync();
        var folio = await db.HotelFolios.SingleAsync();
        db.HotelFolioEntries.Add(HotelFolioEntry.Create(
            folio.HotelFolioId,
            propertyId,
            1,
            HotelFolioEntryType.RoomCharge,
            350m,
            "Tiền phòng đoàn",
            "GroupCheckoutTest",
            null,
            new DateOnly(2026, 9, 8),
            now.AddMinutes(2),
            null));
        await db.SaveChangesAsync();

        var checkout = new EfGroupCheckOutCommandService(
            db,
            new TestPersistence(db),
            accessor,
            new FixedClock(now.AddHours(2)),
            numberGenerator);
        var result = await checkout.CheckOutAndIssueInvoicesAsync(
            group.GroupReservationId,
            [assignment.GroupRoomAssignmentId],
            now.AddHours(1),
            CancellationToken.None);

        result.CheckedOutRooms.Should().Be(1);
        result.InvoicesGenerated.Should().Be(1);
        result.HousekeepingTasksCreated.Should().Be(1);
        (await db.HotelStays.SingleAsync(x => x.HotelStayId == stay.HotelStayId)).Status
            .Should().Be(HotelStayStatus.CheckedOut);
        (await db.FrontOfficeGroupRoomAssignments.SingleAsync()).IsCheckedOut.Should().BeTrue();
        (await db.FrontOfficeGroupReservations.SingleAsync()).Status.Should().Be(GroupReservationStatus.CheckedOut);
        db.Set<HotelInvoice>().Should().ContainSingle(x => x.HotelFolioId == folio.HotelFolioId);
        db.HousekeepingTasks.Should().ContainSingle(x => x.HotelRoomId == room.HotelRoomId);
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

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; } = now;
    }

    private sealed class FixedNumberGenerator : IReservationNumberGenerator, IHotelStayNumberGenerator, IHotelFolioNumberGenerator, IHotelInvoiceNumberGenerator
    {
        private int _next;

        public Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
            => Task.FromResult($"TEST-{++_next:D3}");
    }

    private sealed class TestPersistence(AppDbContext db) : IFrontOfficePersistenceSession
    {
        public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) { }
        public void StageAudit(FrontOfficeAuditIntent intent) { }
        public async Task<ASMILE.BO.Shared.Foundation.Result<int>> CommitAsync(CancellationToken cancellationToken = default)
            => ASMILE.BO.Shared.Foundation.Result<int>.Success(await db.SaveChangesAsync(cancellationToken));
    }
}
