using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Concierge;
using ASMILE.BO.Core.FrontOffice.DoorLock;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.Group;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.FrontOffice.Concierge;
using ASMILE.BO.Infrastructure.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.FrontOffice.Message;
using ASMILE.BO.Infrastructure.FrontOffice.Folio;
using ASMILE.BO.Infrastructure.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Settlement;
using ASMILE.BO.Infrastructure.FrontOffice.Payment;
using ASMILE.BO.Infrastructure.FrontOffice.Guest;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Reporting;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using EntityItem = ASMILE.BO.Core.Entities.Item;
using EntityStockBalance = ASMILE.BO.Core.Entities.StockBalance;
using EntityWarehouse = ASMILE.BO.Core.Entities.Warehouse;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Integration;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class FrontOfficeSqlServerIntegrationTests
{
    [Fact]
    public async Task LocalDb_group_reservation_deposit_policy_columns_are_queryable_after_migration()
    {
        var databaseName = $"ASMILE_BO_GROUP_SCHEMA_{Guid.NewGuid():N}";
        var connectionString = BuildIntegrationConnectionString(databaseName);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var group = GroupReservation.Create(
                Guid.NewGuid(),
                "GRP-SCHEMA-001",
                "Schema test group",
                new DateOnly(2026, 9, 20),
                new DateOnly(2026, 9, 22),
                1,
                2,
                0,
                DateTime.UtcNow,
                null);
            db.FrontOfficeGroupReservations.Add(group);
            await db.SaveChangesAsync();

            var projection = await db.FrontOfficeGroupReservations
                .Where(x => x.GroupReservationId == group.GroupReservationId)
                .Select(x => new
                {
                    x.DepositRequired,
                    x.GuaranteeType,
                    x.DepositCutoffDate,
                    x.DepositReleaseDate
                })
                .SingleAsync();

            projection.DepositRequired.Should().BeFalse();
            projection.GuaranteeType.Should().BeNull();
            projection.DepositCutoffDate.Should().BeNull();
            projection.DepositReleaseDate.Should().BeNull();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task LocalDb_migration_and_technical_stores_are_durable_and_transactional()
    {
        var databaseName = $"ASMILE_BO_FO_TEST_{Guid.NewGuid():N}";
        var connectionString = BuildIntegrationConnectionString(databaseName);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var clock = new FixedClock(new DateTime(2026, 7, 17, 9, 0, 0, DateTimeKind.Utc));
            var accessor = new FrontOfficeExecutionContextAccessor();
            var auditWriter = new FrontOfficeAuditWriter();
            var session = new EfFrontOfficePersistenceSession(db, accessor, new PassthroughIntegrationEventMapper(), new CanonicalOutboxMessageFactory(clock), auditWriter, clock);
            var context = CreateContext();
            var messageId = MessageId.New();
            using (accessor.BeginScope(context))
            {
                session.StageDomainEvent(new TestEvent(EventId.From(messageId.Value), clock.UtcNow));
                session.StageAudit(new FrontOfficeAuditIntent("FoundationSqlServer", "Foundation", "test", "Succeeded"));
                (await session.CommitAsync()).IsSuccess.Should().BeTrue();
            }

            var outbox = await db.FrontOfficeOutboxMessages.SingleAsync();
            outbox.MessageId.Should().Be(messageId);
            outbox.BusinessDate.Should().Be(new DateOnly(2026, 7, 17));
            outbox.PayloadHash.Should().HaveLength(64);
            outbox.RowVersion.Should().NotBeNullOrEmpty();
            (await db.AuditLogs.CountAsync(x => x.Action == "FoundationSqlServer")).Should().Be(1);

            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                using (accessor.BeginScope(context))
                {
                    session.StageDomainEvent(new TestEvent(EventId.New(), clock.UtcNow));
                    session.StageAudit(new FrontOfficeAuditIntent("FoundationRollback", "Foundation", "rollback", "Succeeded"));
                    (await session.CommitAsync()).IsSuccess.Should().BeTrue();
                }
                await transaction.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            (await db.FrontOfficeOutboxMessages.CountAsync()).Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "FoundationRollback")).Should().Be(0);

            var inbox = new EfInboxStore(db, clock);
            var inboxMessageId = MessageId.New();
            (await inbox.ReserveAsync("bo tieu thu Unicode", inboxMessageId, "a".PadLeft(64, '0'))).Should().Be(InboxReservationResult.Reserved);
            (await inbox.ReserveAsync("bo tieu thu Unicode", inboxMessageId, "a".PadLeft(64, '0'))).Should().Be(InboxReservationResult.InProgress);
            (await inbox.ReserveAsync("bo tieu thu Unicode", inboxMessageId, "b".PadLeft(64, '0'))).Should().Be(InboxReservationResult.PayloadHashMismatch);
            await inbox.CompleteAsync("bo tieu thu Unicode", inboxMessageId);
            (await inbox.ReserveAsync("bo tieu thu Unicode", inboxMessageId, "a".PadLeft(64, '0'))).Should().Be(InboxReservationResult.DuplicateCompleted);

            var idempotency = new EfIdempotencyStore(db, clock);
            var key = IdempotencyKey.From("sql-server-foundation-key");
            (await idempotency.ReserveAsync("fo-command", key, CommandId.New(), "a".PadLeft(64, '0'), clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.Reserved);
            (await idempotency.ReserveAsync("fo-command", key, CommandId.New(), "a".PadLeft(64, '0'), clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.InProgress);
            (await idempotency.ReserveAsync("fo-command", key, CommandId.New(), "b".PadLeft(64, '0'), clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.RequestHashMismatch);

            var process = ProcessInstance.Start("test:process:1", "Foundation.Test", 1, context.Property.PropertyId, context.BusinessDate.BusinessDate, "Test", "1", context.Correlation.CorrelationId, clock.UtcNow);
            db.FrontOfficeProcesses.Add(process);
            db.FrontOfficeProcessTimeouts.Add(ProcessTimeout.Schedule(process.ProcessId, null, "Test", clock.UtcNow.AddMinutes(-1), context.Correlation.CorrelationId.Value));
            await db.SaveChangesAsync();
            var processStore = new EfProcessManagerStore(db);
            var claimed = await processStore.ClaimDueTimeoutsAsync("worker-one", clock.UtcNow, TimeSpan.FromMinutes(1), 10);
            claimed.Should().ContainSingle();
            (await processStore.ClaimDueTimeoutsAsync("worker-two", clock.UtcNow, TimeSpan.FromMinutes(1), 10)).Should().BeEmpty();
            (await processStore.RenewTimeoutLeaseAsync(claimed[0].TimeoutId, "worker-one", clock.UtcNow, TimeSpan.FromMinutes(2))).Renewed.Should().BeTrue();
            (await processStore.RenewTimeoutLeaseAsync(claimed[0].TimeoutId, "worker-two", clock.UtcNow, TimeSpan.FromMinutes(2))).Renewed.Should().BeFalse();
            (await processStore.ClaimDueTimeoutsAsync("worker-two", clock.UtcNow.AddMinutes(3), TimeSpan.FromMinutes(1), 10)).Should().ContainSingle();
            var signal = ProcessSignal.Reserve(process.ProcessId, Guid.NewGuid(), "Test.Signal", "c".PadLeft(64, '0'), context.Correlation.CorrelationId.Value, clock.UtcNow);
            (await processStore.ReserveSignalAsync(signal)).Should().Be(ProcessSignalReservationResult.Reserved);
            await processStore.SaveChangesAsync();
            (await processStore.ReserveSignalAsync(signal)).Should().Be(ProcessSignalReservationResult.DuplicateSamePayload);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task LocalDb_two_independent_contexts_claim_one_timeout_once()
    {
        var databaseName = $"ASMILE_BO_PM_RACE_{Guid.NewGuid():N}";
        var connectionString = BuildIntegrationConnectionString(databaseName);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(setup);
            await setup.Database.MigrateAsync();
            var property = PropertyId.New();
            var correlation = CorrelationId.New();
            var process = ProcessInstance.Start("test:race:1", "Foundation.Race", 1, property, new DateOnly(2026, 7, 17), "Test", "race", correlation, DateTime.UtcNow);
            setup.FrontOfficeProcesses.Add(process);
            setup.FrontOfficeProcessTimeouts.Add(ProcessTimeout.Schedule(process.ProcessId, null, "Race", DateTime.UtcNow.AddMinutes(-1), correlation.Value));
            await setup.SaveChangesAsync();

            await using var workerA = new AppDbContext(options);
            await using var workerB = new AppDbContext(options);
            var claims = await Task.WhenAll(
                new EfProcessManagerStore(workerA).ClaimDueTimeoutsAsync("worker-a", DateTime.UtcNow, TimeSpan.FromMinutes(1), 1),
                new EfProcessManagerStore(workerB).ClaimDueTimeoutsAsync("worker-b", DateTime.UtcNow, TimeSpan.FromMinutes(1), 1));
            claims.Sum(x => x.Count).Should().Be(1);
        }
        finally
        {
            await setup.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task LocalDb_signal_handler_commits_process_history_signal_and_outbox_once_and_duplicates_are_noops()
    {
        var databaseName = $"ASMILE_BO_PM_SIGNAL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();
            var now = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
            var property = PropertyId.New(); var correlation = CorrelationId.New();
            var process = ProcessInstance.Start("test:atomic:1", "Atomic.Test", 1, property, new DateOnly(2026, 7, 17), "Test", "atomic", correlation, now, initialState: AtomicState.Started.ToString());
            db.FrontOfficeProcesses.Add(process); await db.SaveChangesAsync();
            var handler = new ProcessSignalHandler(db, new ProcessTransitionEngine(), new NoOpProcessFaultInjector(), new FixedClock(now));
            var signalId = Guid.NewGuid();
            var signal = ProcessSignal.Reserve(process.ProcessId, signalId, "Accepted", "a".PadLeft(64, '0'), correlation.Value, now);
            var result = await handler.HandleAsync(signal, AtomicDefinition(), AtomicSignal.Accepted);
            result.IsSuccess.Should().BeTrue(); result.Value!.Applied.Should().BeTrue();
            db.ChangeTracker.Clear();
            (await db.FrontOfficeProcessSignals.SingleAsync()).Status.Should().Be(ProcessSignalStatus.Processed);
            (await db.FrontOfficeProcessHistory.CountAsync()).Should().Be(1);
            (await db.FrontOfficeOutboxMessages.CountAsync(x => x.EventName == "FrontOffice.Process.Transitioned")).Should().Be(1);
            (await handler.HandleAsync(ProcessSignal.Reserve(process.ProcessId, signalId, "Accepted", "a".PadLeft(64, '0'), correlation.Value, now), AtomicDefinition(), AtomicSignal.Accepted)).Value!.DuplicateNoOp.Should().BeTrue();
            (await handler.HandleAsync(ProcessSignal.Reserve(process.ProcessId, signalId, "Accepted", "b".PadLeft(64, '0'), correlation.Value, now), AtomicDefinition(), AtomicSignal.Accepted)).IsFailure.Should().BeTrue();
            (await db.FrontOfficeOutboxMessages.CountAsync(x => x.EventName == "FrontOffice.Process.Transitioned")).Should().Be(1);
        }
        finally { await db.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task LocalDb_signal_handler_rolls_back_before_commit_and_recovers_after_commit_before_ack()
    {
        var databaseName = $"ASMILE_BO_PM_RECOVERY_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db); await db.Database.MigrateAsync();
            var now = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc); var property = PropertyId.New(); var correlation = CorrelationId.New();
            var process = ProcessInstance.Start("test:recovery:1", "Atomic.Test", 1, property, new DateOnly(2026, 7, 17), "Test", "recovery", correlation, now, initialState: AtomicState.Started.ToString());
            db.FrontOfficeProcesses.Add(process); await db.SaveChangesAsync();
            var signal = ProcessSignal.Reserve(process.ProcessId, Guid.NewGuid(), "Accepted", "c".PadLeft(64, '0'), correlation.Value, now);
            var failing = new ProcessSignalHandler(db, new ProcessTransitionEngine(), new ThrowAt(ProcessFaultPoint.BeforeSaveChanges), new FixedClock(now));
            await Assert.ThrowsAsync<InjectedProcessFault>(() => failing.HandleAsync(signal, AtomicDefinition(), AtomicSignal.Accepted));
            db.ChangeTracker.Clear();
            (await db.FrontOfficeProcessSignals.CountAsync()).Should().Be(0); (await db.FrontOfficeProcessHistory.CountAsync()).Should().Be(0); (await db.FrontOfficeOutboxMessages.CountAsync(x => x.EventName == "FrontOffice.Process.Transitioned")).Should().Be(0);
            var committedSignal = ProcessSignal.Reserve(process.ProcessId, Guid.NewGuid(), "Accepted", "d".PadLeft(64, '0'), correlation.Value, now);
            var afterCommit = new ProcessSignalHandler(db, new ProcessTransitionEngine(), new ThrowAt(ProcessFaultPoint.AfterCommitBeforeAck), new FixedClock(now));
            await Assert.ThrowsAsync<InjectedProcessFault>(() => afterCommit.HandleAsync(committedSignal, AtomicDefinition(), AtomicSignal.Accepted));
            db.ChangeTracker.Clear();
            (await db.FrontOfficeProcessSignals.SingleAsync(x => x.SignalId == committedSignal.SignalId)).Status.Should().Be(ProcessSignalStatus.Processed);
            var retry = new ProcessSignalHandler(db, new ProcessTransitionEngine(), new NoOpProcessFaultInjector(), new FixedClock(now));
            (await retry.HandleAsync(ProcessSignal.Reserve(process.ProcessId, committedSignal.SignalId, "Accepted", "d".PadLeft(64, '0'), correlation.Value, now), AtomicDefinition(), AtomicSignal.Accepted)).Value!.DuplicateNoOp.Should().BeTrue();
        }
        finally { await db.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task WRK_001_to_WRK_009_claim_renew_wrong_owner_retry_exhausted_dead_letter_and_manual_action_are_durable()
    {
        var databaseName = $"ASMILE_BO_PM_WORKER_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db); await db.Database.MigrateAsync();
            var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc); var property = PropertyId.New(); var correlation = CorrelationId.New();
            var process = ProcessInstance.Start("test:worker:1", "Worker.Test", 1, property, new DateOnly(2026, 7, 17), "Test", "worker", correlation, now);
            var scheduled = ProcessTimeout.Schedule(process.ProcessId, null, "Worker", now.AddMinutes(-1), correlation.Value);
            db.FrontOfficeProcesses.Add(process); db.FrontOfficeProcessTimeouts.Add(scheduled); await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var store = new EfProcessManagerStore(db);
            var firstClaim = (await store.ClaimDueTimeoutsAsync("worker-a", now, TimeSpan.FromMinutes(1), 1)).Single();
            var renewal = await store.RenewTimeoutLeaseAsync(firstClaim.TimeoutId, "worker-a", now, TimeSpan.FromMinutes(1), firstClaim.RowVersion);
            renewal.Renewed.Should().BeTrue();
            (await store.RenewTimeoutLeaseAsync(firstClaim.TimeoutId, "worker-b", now, TimeSpan.FromMinutes(1), renewal.RowVersion)).Renewed.Should().BeFalse();
            (await store.RecordTimeoutFailureAsync(firstClaim, "worker-a", ProcessWorkerFailureKind.Transient, "PM_EXTERNAL_TIMEOUT", 3, now, TimeSpan.FromMinutes(1))).Should().Be(ProcessTimeoutFailureResult.RetryScheduled);
            db.ChangeTracker.Clear();
            var retry = await db.FrontOfficeProcessTimeouts.SingleAsync(); retry.Status.Should().Be(ProcessTimeoutStatus.Pending); retry.NextRetryAtUtc.Should().Be(now.AddMinutes(1));
            var secondClaim = (await store.ClaimDueTimeoutsAsync("worker-b", now.AddMinutes(1), TimeSpan.FromMinutes(1), 1)).Single();
            db.ChangeTracker.Clear();
            (await store.RecordTimeoutFailureAsync(secondClaim, "worker-b", ProcessWorkerFailureKind.Permanent, "PM_BUSINESS_VALIDATION", 3, now.AddMinutes(1), TimeSpan.FromMinutes(1))).Should().Be(ProcessTimeoutFailureResult.DeadLettered);
            db.ChangeTracker.Clear();
            var deadLetter = await db.FrontOfficeProcessDeadLetters.SingleAsync();
            deadLetter.ProcessId.Should().Be(process.ProcessId); deadLetter.TimeoutId.Should().Be(scheduled.TimeoutId); deadLetter.SignalId.Should().Be(scheduled.TimeoutId); deadLetter.WorkerId.Should().Be("worker-b"); deadLetter.RetryCount.Should().Be(2); deadLetter.ErrorCode.Should().Be("PM_BUSINESS_VALIDATION");
            (await db.FrontOfficeProcessManualActions.CountAsync(x => x.ActionType == "ProcessTimeoutDeadLetter")).Should().Be(1);
            (await db.FrontOfficeProcessHistory.CountAsync(x => x.TransitionType == "DeadLetter")).Should().Be(1);
        }
        finally { await db.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task NUM_001_to_NUM_005_clean_localdb_migration_creates_reservation_sequence_and_concurrent_numbers_are_unique()
    {
        var databaseName = $"ASMILE_BO_RES_NUM_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(setup); await setup.Database.MigrateAsync();
            (await setup.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = N'DatPhong' AND schema_id = SCHEMA_ID(N'fo')").SingleAsync()).Should().Be(1);
            (await setup.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.sequences WHERE name = N'TrinhTuSoDatPhong' AND schema_id = SCHEMA_ID(N'fo')").SingleAsync()).Should().Be(1);
            await using var first = new AppDbContext(options); await using var second = new AppDbContext(options);
            var businessDate = new DateOnly(2026, 7, 18); var property = Guid.NewGuid();
            var numbers = await Task.WhenAll(new SqlReservationNumberGenerator(first).GenerateAsync(property, businessDate), new SqlReservationNumberGenerator(second).GenerateAsync(property, businessDate));
            numbers.Should().OnlyHaveUniqueItems(); numbers.Should().OnlyContain(x => x.StartsWith("RSV-202607-", StringComparison.Ordinal));
        }
        finally { await setup.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task HMS_I_001_to_030_clean_localdb_migration_enforces_property_isolation_and_room_assignability()
    {
        var databaseName = $"ASMILE_BO_HMS_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db); await db.Database.MigrateAsync();
            (await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = N'LoaiPhongKhachSan' AND schema_id = SCHEMA_ID(N'fo')").SingleAsync()).Should().Be(1);
            (await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = N'PhongKhachSan' AND schema_id = SCHEMA_ID(N'fo')").SingleAsync()).Should().Be(1);
            var propertyA = PropertyId.New(); var propertyB = PropertyId.New(); var actor = ActorId.New(); var now = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc);
            var typeA = HotelRoomType.Create(propertyA, "DLX", "Deluxe", null, 1, 0, 2, 1, 2, 1, now, actor);
            var typeB = HotelRoomType.Create(propertyB, "DLX", "Deluxe B", null, 1, 0, 2, 1, 2, 1, now, actor);
            var roomA = HotelRoom.Create(propertyA, typeA.HotelRoomTypeId, "101", null, "1", null, null, null, now, actor);
            var outOfOrder = HotelRoom.Create(propertyA, typeA.HotelRoomTypeId, "102", null, "1", null, null, null, now, actor); outOfOrder.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfOrder, now, actor);
            var housekeepingState = RoomHousekeepingState.Create(roomA.HotelRoomId, propertyA.Value, now);
            housekeepingState.MarkDirty(now);
            db.HotelRoomTypes.AddRange(typeA, typeB); db.HotelRooms.AddRange(roomA, outOfOrder); db.RoomHousekeepingStates.Add(housekeepingState); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var repository = new EfHotelRoomMasterDataRepository(db);
            (await repository.GetRoomTypeDtoAsync(propertyB.Value, typeA.HotelRoomTypeId)).Should().BeNull();
            (await repository.SearchRoomTypesAsync(propertyA.Value, null, null, new(1, 20), null)).Items.Should().ContainSingle();
            (await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), null, true)).Items.Should().ContainSingle(x => x.RoomNumber == "101");
            var dirtyRooms = await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), "number", false, default, HousekeepingRoomStatus.VacantDirty);
            dirtyRooms.Total.Should().Be(1);
            dirtyRooms.Items.Should().ContainSingle(x => x.RoomNumber == "101");
            var cleanRooms = await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), "number", false, default, HousekeepingRoomStatus.VacantClean);
            cleanRooms.Total.Should().Be(0);
            var vacantDirtyRooms = await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), "number", false, default, null, "VD");
            vacantDirtyRooms.Total.Should().Be(1);
            var occupiedRooms = await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), "number", false, default, null, "OC");
            occupiedRooms.Total.Should().Be(0);
            (await repository.GetRoomReferenceAsync(propertyB.Value, roomA.HotelRoomId)).Should().BeNull();
            (await repository.GetRoomReferenceAsync(propertyA.Value, roomA.HotelRoomId)).Should().Match<HotelRoomReference>(x => x.IsAssignable);
            (await repository.HotelRoomMatchesRoomTypeAsync(propertyA.Value, roomA.HotelRoomId, typeA.HotelRoomTypeId)).Should().BeTrue();
        }
        finally { await db.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task RES_CON_and_REF_localdb_stale_mutation_is_atomic_and_hotel_references_are_property_scoped()
    {
        var databaseName = $"ASMILE_BO_RES_CON_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(BuildIntegrationConnectionString(databaseName)).Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(setup); await setup.Database.MigrateAsync();
            var propertyA = PropertyId.New(); var propertyB = PropertyId.New(); var actor = ActorId.New(); var now = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);
            var typeA = HotelRoomType.Create(propertyA, "DLX", "Deluxe", null, 1, 0, 2, 1, 2, 1, now, actor); var typeB = HotelRoomType.Create(propertyB, "DLX", "Other", null, 1, 0, 2, 1, 2, 1, now, actor);
            var roomA = HotelRoom.Create(propertyA, typeA.HotelRoomTypeId, "101", null, "1", null, null, null, now, actor); var roomB = HotelRoom.Create(propertyB, typeB.HotelRoomTypeId, "101", null, "1", null, null, null, now, actor); var outOfOrder = HotelRoom.Create(propertyA, typeA.HotelRoomTypeId, "102", null, "1", null, null, null, now, actor); outOfOrder.ChangeOperationalStatus(HotelRoomOperationalStatus.OutOfOrder, now, actor);
            setup.HotelRoomTypes.AddRange(typeA, typeB); setup.HotelRooms.AddRange(roomA, roomB, outOfOrder); await setup.SaveChangesAsync();
            await using var createDb = new AppDbContext(options); var accessor = new FrontOfficeExecutionContextAccessor(); var context = CreateContext(propertyA, actor);
            using (accessor.BeginScope(context))
            {
                var created = await CreateReservationService(createDb, accessor, now).CreateAsync(new CreateReservationCommand(propertyA.Value, Guid.NewGuid(), "Guest", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 1, 0, typeA.HotelRoomTypeId, null, "Direct", null, null, null, false), default);
                created.IsSuccess.Should().BeTrue(); var reservation = created.Value!; reservation.RowVersion.Should().HaveCount(8);
                var historyBefore = await createDb.FrontOfficeReservationHistory.CountAsync(); var outboxBefore = await createDb.FrontOfficeOutboxMessages.CountAsync(); var auditBefore = await createDb.AuditLogs.CountAsync();
                await using var winnerDb = new AppDbContext(options); await using var staleDb = new AppDbContext(options); var winnerAccessor = new FrontOfficeExecutionContextAccessor(); var staleAccessor = new FrontOfficeExecutionContextAccessor();
                using (winnerAccessor.BeginScope(context)) using (staleAccessor.BeginScope(context))
                {
                    (await CreateReservationService(winnerDb, winnerAccessor, now.AddMinutes(1)).UpdateDetailsAsync(new UpdateReservationDetailsCommand(propertyA.Value, reservation.ReservationId, "Winner", null, null, typeA.HotelRoomTypeId, null, null, null, reservation.RowVersion!), default)).IsSuccess.Should().BeTrue();
                    var stale = await CreateReservationService(staleDb, staleAccessor, now.AddMinutes(2)).UpdateDetailsAsync(new UpdateReservationDetailsCommand(propertyA.Value, reservation.ReservationId, "Stale", null, null, typeA.HotelRoomTypeId, null, null, null, reservation.RowVersion!), default);
                    stale.Error!.Code.Should().Be("RESERVATION_CONCURRENCY_CONFLICT");
                }
                createDb.ChangeTracker.Clear(); (await createDb.FrontOfficeReservations.SingleAsync(x => x.ReservationId == reservation.ReservationId)).GuestNameSnapshot.Should().Be("Winner"); (await createDb.FrontOfficeReservationHistory.CountAsync()).Should().Be(historyBefore + 1); (await createDb.FrontOfficeOutboxMessages.CountAsync()).Should().Be(outboxBefore); (await createDb.AuditLogs.CountAsync()).Should().Be(auditBefore + 1);
                var latest = await new EfReservationRepository(createDb).GetDtoAsync(reservation.ReservationId, propertyA.Value);
                (await CreateReservationService(createDb, accessor, now.AddMinutes(3)).AssignAsync(new AssignReservationRoomCommand(propertyA.Value, reservation.ReservationId, roomB.HotelRoomId, latest!.RowVersion!), default)).Error!.Code.Should().Be("RESERVATION_ROOM_NOT_FOUND");
                (await CreateReservationService(createDb, accessor, now.AddMinutes(3)).AssignAsync(new AssignReservationRoomCommand(propertyA.Value, reservation.ReservationId, outOfOrder.HotelRoomId, latest.RowVersion!), default)).Error!.Code.Should().Be("RESERVATION_ROOM_NOT_ASSIGNABLE");
                (await CreateReservationService(createDb, accessor, now.AddMinutes(3)).AssignAsync(new AssignReservationRoomCommand(propertyA.Value, reservation.ReservationId, roomA.HotelRoomId, latest.RowVersion!), default)).IsSuccess.Should().BeTrue();
            }
        }
        finally { await setup.Database.EnsureDeletedAsync(); SqlConnection.ClearAllPools(); }
    }

    [Fact]
    public async Task HOTEL_E2E_reservation_group_checkin_folio_invoice_checkout_and_housekeeping_are_persisted()
    {
        var databaseName = $"ASMILE_BO_HOTEL_E2E_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var now = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
            var property = PropertyId.New();
            var actor = ActorId.New();
            var contextAccessor = new FrontOfficeExecutionContextAccessor();
            var context = CreateContext(property, actor);
            var clock = new FixedClock(now);
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                contextAccessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);

            var roomType = HotelRoomType.Create(
                property,
                "E2E-DLX",
                "E2E Deluxe",
                null,
                1,
                0,
                2,
                1,
                1,
                1,
                now,
                actor);
            var room = HotelRoom.Create(
                property,
                roomType.HotelRoomTypeId,
                "E2E-101",
                null,
                "1",
                "A",
                null,
                null,
                now,
                actor);
            var guest = GuestProfile.Create(
                property.Value,
                "E2E-GUEST-001",
                "Commercial E2E Guest",
                "0900000000",
                null,
                GuestSource.Direct,
                now,
                actor);
            var group = GroupReservation.Create(
                property.Value,
                "E2E-GRP-001",
                "Commercial E2E Group",
                new DateOnly(2026, 9, 14),
                new DateOnly(2026, 9, 15),
                1,
                1,
                0,
                now,
                actor);
            var assignment = group.AddRoom(
                room.HotelRoomId,
                room.RoomNumber,
                roomType.Code,
                guest.FullName,
                1,
                0,
                now);
            assignment.AssignGuest(guest.GuestProfileId, guest.FullName);
            group.Confirm(now);
            db.HotelRoomTypes.Add(roomType);
            db.HotelRooms.Add(room);
            db.GuestProfiles.Add(guest);
            db.FrontOfficeGroupReservations.Add(group);
            await db.SaveChangesAsync();

            using (contextAccessor.BeginScope(context))
            {
                var groupCheckIn = new EfGroupCheckInCommandService(
                    db,
                    persistence,
                    contextAccessor,
                    clock,
                    new SqlReservationNumberGenerator(db),
                    new SqlHotelStayNumberGenerator(db),
                    new SqlHotelFolioNumberGenerator(db));
                var checkIn = await groupCheckIn.CheckInAsync(
                    group.GroupReservationId,
                    new[] { assignment.GroupRoomAssignmentId },
                    CancellationToken.None);
                checkIn.CheckedInRooms.Should().Be(1);
            }

            db.ChangeTracker.Clear();
            var persistedGroup = await db.FrontOfficeGroupReservations
                .Include(x => x.RoomingList)
                .SingleAsync(x => x.GroupReservationId == group.GroupReservationId);
            var persistedAssignment = persistedGroup.RoomingList.Single();
            var stay = await db.HotelStays.SingleAsync(x => x.PropertyId == property.Value && x.ReservationId == persistedAssignment.ReservationId);
            var folio = await db.HotelFolios.SingleAsync(x => x.PropertyId == property.Value && x.HotelStayId == stay.HotelStayId);
            persistedGroup.Status.Should().Be(GroupReservationStatus.CheckedIn);
            persistedAssignment.IsCheckedIn.Should().BeTrue();
            persistedAssignment.GuestId.Should().Be(guest.GuestProfileId);
            stay.Status.IsOccupying().Should().BeTrue();

            var roomCharge = folio.AddRoomCharge(100_000m, "E2E room charge", new DateOnly(2026, 9, 14), null, now, actor);
            db.HotelFolioEntries.Add(roomCharge);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            using (contextAccessor.BeginScope(context))
            {
                var groupCheckOut = new EfGroupCheckOutCommandService(
                    db,
                    persistence,
                    contextAccessor,
                    clock,
                    new SqlHotelInvoiceNumberGenerator(db));
                var checkOut = await groupCheckOut.CheckOutAndIssueInvoicesAsync(
                    group.GroupReservationId,
                    new[] { assignment.GroupRoomAssignmentId },
                    now,
                    CancellationToken.None);
                checkOut.CheckedOutRooms.Should().Be(1);
                checkOut.InvoicesGenerated.Should().Be(1);
                checkOut.HousekeepingTasksCreated.Should().Be(1);
            }

            db.ChangeTracker.Clear();
            (await db.FrontOfficeGroupReservations.SingleAsync(x => x.GroupReservationId == group.GroupReservationId))
                .Status.Should().Be(GroupReservationStatus.CheckedOut);
            (await db.HotelStays.SingleAsync(x => x.HotelStayId == stay.HotelStayId))
                .Status.Should().Be(HotelStayStatus.CheckedOut);
            var invoice = await db.HotelInvoices.SingleAsync(x => x.HotelFolioId == folio.HotelFolioId);
            invoice.TotalAmount.Should().Be(100_000m);
            (await db.HousekeepingTasks.CountAsync(x => x.PropertyId == property.Value && x.HotelRoomId == room.HotelRoomId && x.TaskType == HousekeepingTaskType.DepartureClean))
                .Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "GroupCheckedIn")).Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "GroupCheckedOut")).Should().Be(1);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_guest_documents_duplicates_and_merge_round_trip_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_GUEST_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var actor = ActorId.New();
            var now = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
            var target = GuestProfile.Create(
                propertyId.Value, "GUEST-SQL-001", "Nguyễn Minh An", "0900000101", "an@example.test", GuestSource.Direct, now, actor);
            target.SetDocument(GuestDocumentType.Passport, "P-VN-001", "VN", new DateOnly(2030, 1, 1), now, actor);
            target.AddPreference("RoomType", "Quiet room");
            var source = GuestProfile.Create(
                propertyId.Value, "GUEST-SQL-002", "An Nguyen Duplicate", null, null, GuestSource.Referral, now, actor);
            source.AddDocument(GuestDocumentType.NationalId, "ID-VN-002", "VN", null, now);

            var repository = new EfGuestProfileRepository(db);
            await repository.AddAsync(target, CancellationToken.None);
            await repository.AddAsync(source, CancellationToken.None);
            db.ChangeTracker.Clear();

            var persistedTarget = await repository.GetAsync(propertyId.Value, target.GuestProfileId, CancellationToken.None);
            persistedTarget.Should().NotBeNull();
            persistedTarget!.Documents.Should().ContainSingle(x =>
                x.DocumentType == GuestDocumentType.Passport && x.DocumentNumber == "P-VN-001" && x.IssuingCountry == "VN");
            persistedTarget.Preferences.Should().ContainSingle(x => x.Key == "RoomType" && x.Value == "Quiet room");

            var duplicateCandidates = await repository.FindDuplicatesAsync(
                propertyId.Value,
                new DuplicateDetectionCriteria("Nguyen Minh An", null, null, "P-VN-001", null),
                CancellationToken.None);
            duplicateCandidates.Should().ContainSingle(x => x.ExistingGuestId == target.GuestProfileId && x.MatchReason.Contains("Document"));

            var merged = await repository.MergeAsync(
                new GuestMergeRequest(source.GuestProfileId, target.GuestProfileId, "Duplicate guest profile", true, actor.Value),
                CancellationToken.None);
            merged.Success.Should().BeTrue();
            merged.TargetGuestId.Should().Be(target.GuestProfileId);

            db.ChangeTracker.Clear();
            var mergedTarget = await repository.GetAsync(propertyId.Value, target.GuestProfileId, CancellationToken.None);
            mergedTarget.Should().NotBeNull();
            mergedTarget!.Documents.Should().HaveCount(2);
            mergedTarget.Documents.Should().Contain(x => x.DocumentType == GuestDocumentType.NationalId && x.DocumentNumber == "ID-VN-002");
            var retainedSource = await repository.GetAsync(propertyId.Value, source.GuestProfileId, CancellationToken.None);
            retainedSource.Should().NotBeNull();
            retainedSource!.Documents.Should().BeEmpty();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_cash_shift_reconciliation_variance_and_approval_persist_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_CASHIER_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var cashierId = Guid.NewGuid();
            var reviewerId = Guid.NewGuid();
            var accessor = new FrontOfficeExecutionContextAccessor();
            var clock = new FixedClock(new DateTime(2026, 9, 20, 11, 0, 0, DateTimeKind.Utc));
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                accessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);
            using var scope = accessor.BeginScope(CreateContext(propertyId, new ActorId(cashierId)));

            var shifts = new EfCashShiftService(db, accessor, clock);
            var opened = await shifts.OpenShiftAsync(Guid.NewGuid(), cashierId, 1_000_000m, CancellationToken.None);
            opened.Status.Should().Be(CashierShiftStatus.Open);
            opened.OpeningCash.Should().Be(1_000_000m);
            (await shifts.GetActiveShiftsAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.ShiftId == opened.ShiftId);

            var closed = await shifts.CloseShiftAsync(opened.ShiftId, 1_000_000m, null, CancellationToken.None);
            closed.Status.Should().Be(CashierShiftStatus.Closed);
            closed.ActualCash.Should().Be(1_000_000m);
            (await db.CashMovements.CountAsync(x => x.CashierShiftId == opened.ShiftId)).Should().Be(2);

            var reconciliationService = new EfCashReconciliationService(db, persistence, accessor, clock);
            var reconciliation = await reconciliationService.ReconcileAsync(opened.ShiftId, 1_100_000m, CancellationToken.None);
            reconciliation.Status.Should().Be(ReconciliationStatus.VarianceDetected);
            reconciliation.ExpectedCash.Should().Be(1_000_000m);
            reconciliation.ActualCash.Should().Be(1_100_000m);
            reconciliation.Variance.Should().Be(100_000m);
            (await reconciliationService.GetPendingAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.ReconciliationId == reconciliation.ReconciliationId);

            var approved = await reconciliationService.ApproveVarianceAsync(
                reconciliation.ReconciliationId, reviewerId, "Count rechecked and approved.", CancellationToken.None);
            approved.Status.Should().Be(ReconciliationStatus.Approved);
            approved.ApprovedBy.Should().Be(reviewerId);

            db.ChangeTracker.Clear();
            (await db.CashierShifts.SingleAsync(x => x.CashierShiftId == opened.ShiftId)).Status
                .Should().Be(CashierShiftStatus.Closed);
            (await db.CashReconciliations.SingleAsync(x => x.CashierShiftId == opened.ShiftId)).Status
                .Should().Be(CashReconciliationStatus.Approved);
            (await db.AuditLogs.CountAsync(x => x.EntityName == nameof(CashReconciliation))).Should().Be(2);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_night_audit_business_date_and_revenue_snapshot_round_trip_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_NIGHT_AUDIT_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var actorId = new ActorId(Guid.NewGuid());
            var auditDate = new DateOnly(2026, 9, 19);
            var now = new DateTime(2026, 9, 20, 1, 0, 0, DateTimeKind.Utc);
            var audit = NightAudit.Begin(propertyId.Value, auditDate, "NA-20260919-001", now, actorId);
            audit.CompleteStep(NightAuditStep.LockBusinessDate, "Business date locked.", now.AddMinutes(1));

            var businessDate = BusinessDateRecord.Open(propertyId.Value, auditDate, now, actorId);
            businessDate.BeginClosing(audit.NightAuditId, now.AddMinutes(1));
            businessDate.MarkClosed(now.AddMinutes(10), actorId);
            var nextBusinessDate = BusinessDateRecord.Open(propertyId.Value, auditDate.AddDays(1), now.AddMinutes(10), actorId);
            var snapshot = RevenueSnapshot.Create(
                propertyId.Value,
                audit.NightAuditId,
                auditDate,
                roomRev: 1_200_000m,
                otherRev: 300_000m,
                totalRev: 1_500_000m,
                tax: 150_000m,
                payments: 900_000m,
                refunds: 50_000m,
                outstanding: 700_000m,
                roomsSold: 3,
                totalRooms: 10,
                occupancy: 0.3m,
                adr: 500_000m,
                revpar: 150_000m,
                arrivals: 2,
                departures: 1,
                noShows: 0,
                cancellations: 1,
                json: "{\"source\":\"integration-test\"}",
                nowUtc: now.AddMinutes(9));

            db.Set<NightAudit>().Add(audit);
            var repository = new EfNightAuditRepository(db);
            await repository.AddBusinessDateAsync(businessDate, CancellationToken.None);
            await repository.AddBusinessDateAsync(nextBusinessDate, CancellationToken.None);
            await repository.AddRevenueSnapshotAsync(snapshot, CancellationToken.None);

            db.ChangeTracker.Clear();

            var currentAudit = await repository.GetCurrentAsync(propertyId.Value, CancellationToken.None);
            currentAudit.Should().NotBeNull();
            currentAudit!.AuditNumber.Should().Be("NA-20260919-001");
            currentAudit.CompletedSteps.Should().Be(1);
            currentAudit.TotalSteps.Should().Be(Enum.GetValues<NightAuditStep>().Length);

            var closedDate = await repository.GetBusinessDateAsync(propertyId.Value, auditDate, CancellationToken.None);
            closedDate.Should().NotBeNull();
            closedDate!.Status.Should().Be(BusinessDateStatus.Closed);
            closedDate.NightAuditId.Should().Be(audit.NightAuditId);

            var openDate = await repository.GetCurrentBusinessDateAsync(propertyId.Value, CancellationToken.None);
            openDate.Should().NotBeNull();
            openDate!.BusinessDate.Should().Be(auditDate.AddDays(1));
            openDate.Status.Should().Be(BusinessDateStatus.Open);

            var persistedSnapshot = await repository.GetRevenueSnapshotAsync(propertyId.Value, auditDate, CancellationToken.None);
            persistedSnapshot.Should().NotBeNull();
            persistedSnapshot!.TotalRevenue.Should().Be(1_500_000m);
            persistedSnapshot.TotalTax.Should().Be(150_000m);
            persistedSnapshot.OutstandingBalance.Should().Be(700_000m);
            persistedSnapshot.OccupancyRate.Should().Be(0.3m);
            persistedSnapshot.RevPAR.Should().Be(150_000m);

            var reportingAccessor = new FrontOfficeExecutionContextAccessor();
            using var reportingScope = reportingAccessor.BeginScope(CreateContext(propertyId, actorId));
            var reporting = new EfReportingQueryService(db, reportingAccessor, new AllowAllAuthorization());
            var revenueReport = await reporting.GetRevenueReportAsync(
                new RevenueReportQuery(propertyId.Value, auditDate, auditDate), CancellationToken.None);
            revenueReport.IsSuccess.Should().BeTrue($"{revenueReport.Error?.Code}: {revenueReport.Error?.Message}");
            revenueReport.Value.Should().ContainSingle().Which.TotalRevenue.Should().Be(0m);
            var occupancyReport = await reporting.GetOccupancyReportAsync(
                new OccupancyReportQuery(propertyId.Value, auditDate, auditDate), CancellationToken.None);
            occupancyReport.IsSuccess.Should().BeTrue($"{occupancyReport.Error?.Code}: {occupancyReport.Error?.Message}");
            occupancyReport.Value.Should().ContainSingle().Which.RoomsSold.Should().Be(0);
            var dashboard = await reporting.GetDashboardAsync(new DashboardQuery(auditDate), CancellationToken.None);
            dashboard.IsSuccess.Should().BeTrue($"{dashboard.Error?.Code}: {dashboard.Error?.Message}");
            dashboard.Value!.Cashier.ExpectedCash.Should().Be(0m);

            var otherPropertyId = Guid.NewGuid();
            (await repository.GetCurrentAsync(otherPropertyId, CancellationToken.None)).Should().BeNull();
            (await repository.GetCurrentBusinessDateAsync(otherPropertyId, CancellationToken.None)).Should().BeNull();
            (await repository.GetRevenueSnapshotAsync(otherPropertyId, auditDate, CancellationToken.None)).Should().BeNull();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_lost_found_contact_fields_round_trip_through_migrations_and_query_service()
    {
        var databaseName = $"ASMILE_BO_HOTEL_LOST_FOUND_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var commandService = new EfLostFoundCommandService(db);
            var created = await commandService.RegisterAsync(new RegisterLostFoundItemCommand(
                propertyId,
                $"LF-SQL-{Guid.NewGuid():N}"[..18],
                "SQL integration test item",
                LostFoundCategory.Electronics,
                "Synthetic LocalDB workflow record",
                "Room 405",
                now.AddMinutes(-10),
                "Housekeeping",
                null,
                "Black",
                "Test brand",
                "SQL-TEST-001",
                "Synthetic SQL migration test",
                actorId));

            await commandService.ContactAsync(new ContactLostFoundItemCommand(
                created.ItemId,
                propertyId,
                "Front Office",
                now,
                "Phone",
                "Contacted; awaiting guest response",
                "Synthetic contact note",
                actorId));
            await commandService.ReturnAsync(new ReturnLostFoundItemCommand(
                created.ItemId,
                propertyId,
                "Front Office",
                "SQL Integration Guest",
                now,
                actorId));

            db.ChangeTracker.Clear();
            var query = await new EfLostFoundQueryService(db).SearchAsync(
                new LostFoundSearchFilter(propertyId, created.ItemNumber, null, null, null, null, null),
                1,
                20);

            query.Items.Should().ContainSingle();
            var persisted = query.Items.Single();
            persisted.Status.Should().Be(LostFoundStatus.Returned);
            persisted.ContactedBy.Should().Be("Front Office");
            persisted.ContactMethod.Should().Be("Phone");
            persisted.ContactResult.Should().Be("Contacted; awaiting guest response");
            persisted.ContactNotes.Should().Be("Synthetic contact note");
            persisted.ReleasedTo.Should().Be("SQL Integration Guest");
            persisted.History.Should().HaveCount(2);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_concierge_guest_message_and_service_request_workflows_persist_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_CONCIERGE_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var guestId = Guid.NewGuid();
            var now = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
            var accessor = new FrontOfficeExecutionContextAccessor();
            var clock = new FixedClock(now);
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                accessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);
            using var scope = accessor.BeginScope(CreateContext(propertyId, ActorId.New()));

            var wakeups = new EfWakeUpCallService(db, persistence, accessor, clock);
            var luggage = new EfLuggageService(db, persistence, accessor, clock);
            var transport = new EfTransportationService(db, persistence, accessor, clock);
            var conciergeDashboard = new EfConciergeDashboardService(db, accessor, clock);

            var wake = await wakeups.ScheduleAsync(Guid.NewGuid(), null, now.AddHours(2), "Gọi phòng 101", CancellationToken.None);
            (await wakeups.GetScheduledAsync(propertyId.Value, now, CancellationToken.None))
                .Should().ContainSingle(x => x.CallId == wake.CallId);
            (await wakeups.CompleteAsync(wake.CallId, WakeUpCallStatus.NoAnswer, CancellationToken.None))
                .Status.Should().Be(WakeUpCallStatus.NoAnswer);
            (await wakeups.RetryAsync(wake.CallId, CancellationToken.None)).RetryCount.Should().Be(1);
            (await wakeups.CompleteAsync(wake.CallId, WakeUpCallStatus.Completed, CancellationToken.None))
                .Status.Should().Be(WakeUpCallStatus.Completed);

            var stored = await luggage.StoreAsync(new LuggageItemDto(
                Guid.Empty, string.Empty, guestId, "Khách SQL", null, "101", 2, "Vali", "A-01",
                LuggageStatus.Stored, default, null, null, null, null), CancellationToken.None);
            (await luggage.GetByGuestAsync(guestId, CancellationToken.None))
                .Should().ContainSingle(x => x.ItemId == stored.ItemId);
            (await luggage.DeliverAsync(stored.ItemId, Guid.NewGuid(), CancellationToken.None)).Status
                .Should().Be(LuggageStatus.Delivered);
            (await luggage.CollectAsync(stored.ItemId, CancellationToken.None)).Status
                .Should().Be(LuggageStatus.Collected);

            var ride = await transport.BookAsync(new TransportationDto(
                Guid.Empty, string.Empty, TransportType.AirportPickup, guestId, "Khách SQL", null, "101",
                null, null, null, now.AddHours(3), "Sân bay", "Khách sạn", "VN123", 2,
                ServiceStatus.Requested, 350000m, null), CancellationToken.None);
            (await transport.GetUpcomingAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.BookingId == ride.BookingId);
            (await transport.CompleteAsync(ride.BookingId, CancellationToken.None)).Status
                .Should().Be(ServiceStatus.Completed);

            var conciergeSummary = await conciergeDashboard.GetDashboardAsync(propertyId.Value, CancellationToken.None);
            conciergeSummary.WakeupsScheduled.Should().Be(0);
            conciergeSummary.LuggageStored.Should().Be(0);
            conciergeSummary.TransportBookings.Should().Be(0);

            var messages = new EfGuestMessageService(db, persistence, accessor, clock);
            var message = await messages.CreateAndSendAsync(new CreateGuestMessageRequest(
                null, null, null, null, "Nguyễn Văn A", "101", "Welcome", "FrontDesk", "High",
                "Chào mừng", "Chúc quý khách có kỳ nghỉ vui vẻ.", true, "FrontOffice"), CancellationToken.None);
            (await messages.GetAllAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle();
            (await messages.MarkReadAsync(message.GuestMessageId, CancellationToken.None)).Status.Should().Be("Read");
            var replied = await messages.ReplyAsync(message.GuestMessageId, "Đã xác nhận.", CancellationToken.None);
            replied.Status.Should().Be("Replied");
            replied.ResponseContent.Should().Be("Đã xác nhận.");
            (await db.FrontOfficeMessageHistories.CountAsync()).Should().Be(3);

            var requests = new EfServiceRequestService(db, persistence, accessor, clock);
            var request = await requests.CreateAsync(new CreateServiceRequestRequest(
                null, null, null, "405", "Bùi Thanh Hà", ServiceType.AirportTransfer,
                ServicePriority.High, "Đặt xe sân bay", null, 300000, "ImmediateCharge"), CancellationToken.None);
            (await requests.GetAllAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.RequestId == request.RequestId);
            var completed = await requests.TransitionAsync(
                request.RequestId, ServiceStatus.Completed, "Đã hoàn tất", CancellationToken.None);
            completed.Status.Should().Be(ServiceStatus.Completed);
            (await db.FrontOfficeServiceHistories.CountAsync()).Should().Be(1);
            (await db.AuditLogs.AnyAsync(x => x.Action == "ServiceRequestStatusChanged")).Should().BeTrue();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_door_lock_events_and_dashboard_round_trip_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_DOOR_LOCK_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            // The dashboard and the stay key-card lifecycle use separate tables
            // from the legacy door-device/event tables below.
            (await db.KeyCardCredentials.CountAsync()).Should().Be(0);
            (await db.KeyCardEvents.CountAsync()).Should().Be(0);

            var propertyId = PropertyId.New();
            var actorId = ActorId.New();
            var now = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
            var roomType = HotelRoomType.Create(propertyId, "STD", "Standard", null, 1, 0, 2, 1, 1, 1, now, actorId);
            var room = HotelRoom.Create(propertyId, roomType.HotelRoomTypeId, "101", null, "1", null, null, null, now, actorId);
            var guest = GuestProfile.Create(propertyId.Value, "GST-DOOR-001", "SQL Key Card Guest", null, null, GuestSource.Direct, now, actorId);
            var stay = HotelStay.Prepare("STY-DOOR-001", propertyId, null, null, guest.GuestProfileId, guest.FullName, null,
                room.HotelRoomId, room.RoomNumber, roomType.HotelRoomTypeId, roomType.Code,
                new HotelStayPeriod(new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 23)),
                new HotelStayGuestCount(1, 0), "Direct", now, actorId);
            stay.CheckIn(now, actorId);

            var online = DoorLockDevice.Create(propertyId.Value, "LOCK-101", "101", Guid.NewGuid(),
                nameof(LockVendor.VingCard), "VisionLine", "3.2.1", nameof(LockStatus.Online), 90,
                now.AddMinutes(-2), now.AddMonths(-2), 12, 1, true);
            var offline = DoorLockDevice.Create(propertyId.Value, "LOCK-102", "102", Guid.NewGuid(),
                nameof(LockVendor.Salto), "XS4", "1.0", nameof(LockStatus.Offline), 18,
                now.AddHours(-3), null, 4, 2, false);
            db.AddRange(roomType, room, guest, stay);
            db.DoorLockDevices.AddRange(online, offline);
            db.DoorLockEvents.AddRange(
                DoorLockEvent.Create(propertyId.Value, online.DeviceId, nameof(DoorEventType.AccessGranted),
                    "101", "KEY-101", nameof(AccessLevel.Guest), true, now.AddMinutes(-5), null),
                DoorLockEvent.Create(propertyId.Value, offline.DeviceId, nameof(DoorEventType.AccessDenied),
                    "102", "KEY-EXPIRED", nameof(AccessLevel.Guest), false, now.AddMinutes(-4), "Key expired"),
                DoorLockEvent.Create(propertyId.Value, offline.DeviceId, nameof(DoorEventType.TamperDetected),
                    "102", null, null, false, now.AddMinutes(-3), "Door forced"));
            await db.SaveChangesAsync();

            var accessor = new FrontOfficeExecutionContextAccessor();
            using var scope = accessor.BeginScope(CreateContext(propertyId, actorId));
            var devices = new EfDoorLockService(db, accessor);
            var events = new EfDoorEventService(db, accessor);
            var dashboard = new EfDoorLockDashboardService(db, accessor, new FixedClock(now));
            var keyCards = new EfKeyCardService(db, accessor, new FixedClock(now));

            var issued = await keyCards.IssueKeyAsync(guest.GuestProfileId, stay.HotelStayId, room.HotelRoomId,
                AccessLevel.Master, now.AddDays(3), 12, CancellationToken.None);
            issued.Status.Should().Be(KeyStatus.Active);
            issued.Level.Should().Be(AccessLevel.Master);
            issued.EncoderId.Should().Be(12);
            issued.IsMaster.Should().BeTrue();

            var duplicate = await keyCards.IssueDuplicateAsync(issued.KeyId, CancellationToken.None);
            duplicate.KeyId.Should().NotBe(issued.KeyId);
            duplicate.Level.Should().Be(AccessLevel.Master);
            duplicate.EncoderId.Should().Be(12);
            (await keyCards.GetKeysForStayAsync(stay.HotelStayId, CancellationToken.None)).Should().HaveCount(2);

            (await keyCards.SuspendKeyAsync(issued.KeyId, CancellationToken.None)).Status.Should().Be(KeyStatus.Suspended);
            (await keyCards.ReactivateKeyAsync(issued.KeyId, CancellationToken.None)).Status.Should().Be(KeyStatus.Active);
            (await keyCards.ReportLostAsync(issued.KeyId, CancellationToken.None)).Status.Should().Be(KeyStatus.Lost);
            (await keyCards.RevokeKeyAsync(duplicate.KeyId, "Checkout", actorId.Value, CancellationToken.None))
                .Status.Should().Be(KeyStatus.Revoked);

            var inventory = await keyCards.GetInventoryAsync(propertyId.Value, CancellationToken.None);
            inventory.TotalKeys.Should().Be(2);
            inventory.Lost.Should().Be(1);
            inventory.Revoked.Should().Be(1);
            (await db.KeyCardEvents.CountAsync()).Should().BeGreaterThanOrEqualTo(6);

            (await devices.GetDevicesAsync(propertyId.Value, null, CancellationToken.None)).Should().HaveCount(2);
            (await devices.GetOfflineDevicesAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.DeviceCode == "LOCK-102");
            (await devices.GetLowBatteryDevicesAsync(propertyId.Value, 20, CancellationToken.None))
                .Should().ContainSingle(x => x.DeviceCode == "LOCK-102");
            (await events.GetFailedAttemptsAsync(propertyId.Value, now.AddHours(-1), CancellationToken.None))
                .Should().HaveCount(2);
            (await events.GetSecurityAlertsAsync(propertyId.Value, CancellationToken.None))
                .Should().ContainSingle(x => x.Type == DoorEventType.TamperDetected);

            var summary = await dashboard.GetDashboardAsync(propertyId.Value, CancellationToken.None);
            summary.TotalLocks.Should().Be(2);
            summary.Online.Should().Be(1);
            summary.Offline.Should().Be(1);
            summary.AccessGrantedToday.Should().Be(1);
            summary.AccessDeniedToday.Should().Be(2);
            summary.FailedAttempts.Should().Be(2);
            summary.RecentSecurityAlerts.Should().ContainSingle();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_maintenance_assets_and_preventive_schedules_round_trip_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_MAINTENANCE_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var roomId = Guid.NewGuid();
            var now = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
            var accessor = new FrontOfficeExecutionContextAccessor();
            var clock = new FixedClock(now);
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                accessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);
            using var scope = accessor.BeginScope(CreateContext(propertyId, ActorId.New()));

            var asset = MaintenanceAsset.Create(
                propertyId.Value, "HVAC-101", "Điều hòa phòng 101", nameof(AssetCategory.HVAC),
                "Tầng 1", roomId, "SN-HVAC-101", "VRF-2025", "Vendor A", now.AddYears(1),
                now.AddYears(-1), 1200, 85m, now.AddDays(-30), now.AddDays(30), now);
            db.MaintenanceAssets.Add(asset);
            var plan = PreventiveMaintenancePlan.Create(
                propertyId.Value, "Bảo trì điều hòa tháng", nameof(AssetCategory.HVAC),
                "Vệ sinh và kiểm tra hiệu suất điều hòa.", asset.AssetId, 30, 60);
            plan.AddChecklistItem("FILTER", "Thay lọc gió", "HVAC", true, "Dùng lọc OEM");
            db.PreventiveMaintenancePlans.Add(plan);
            db.PreventiveMaintenanceSchedules.AddRange(
                PreventiveMaintenanceSchedule.Create(propertyId.Value, plan.PlanId, asset.AssetId, roomId, now.AddDays(-2)),
                PreventiveMaintenanceSchedule.Create(propertyId.Value, plan.PlanId, asset.AssetId, roomId, now.AddDays(3)));
            await db.SaveChangesAsync();

            var assets = new EfMaintenanceAssetService(db, persistence, accessor, clock);
            var preventive = new EfPreventiveMaintenanceService(db, accessor, clock);
            (await assets.GetAssetsByRoomAsync(roomId, CancellationToken.None)).Should().ContainSingle();
            (await assets.GetHealthAsync(propertyId.Value, AssetCategory.HVAC, CancellationToken.None))
                .Single().ConditionScore.Should().Be(85m);
            await assets.RecordFailureAsync(asset.AssetId, "Không làm lạnh đủ công suất.", CancellationToken.None);
            var persisted = await db.MaintenanceAssets.SingleAsync();
            persisted.FailureCount.Should().Be(1);
            persisted.TotalRepairs.Should().Be(1);
            persisted.ConditionScore.Should().Be(75m);
            (await db.MaintenanceAssetFailures.CountAsync()).Should().Be(1);

            var plans = await preventive.GetPlansAsync(AssetCategory.HVAC, CancellationToken.None);
            plans.Should().ContainSingle();
            plans.Single().Checklist.Should().ContainSingle(x => x.Code == "FILTER");
            (await preventive.GetOverdueAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle();
            (await preventive.GetScheduleAsync(propertyId.Value, now.AddDays(-3), now.AddDays(4), CancellationToken.None))
                .Should().HaveCount(2);
            (await db.AuditLogs.CountAsync(x => x.EntityName == nameof(MaintenanceAsset))).Should().Be(1);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_housekeeping_linen_inventory_round_trips_and_rejects_over_issue_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_LINEN_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = PropertyId.New();
            var roomType = HotelRoomType.Create(
                propertyId, Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
            var room = HotelRoom.Create(propertyId, roomType.HotelRoomTypeId, "101", null, "1", null, null, null, DateTime.UtcNow, null);
            db.HotelRoomTypes.Add(roomType);
            db.HotelRooms.Add(room);
            db.HousekeepingLinenTypes.Add(HousekeepingLinenType.Create(
                propertyId.Value, "BEDSHEET-KING", "King sheet", "Bedding", 4, 200));
            await db.SaveChangesAsync();

            var now = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
            var accessor = new FrontOfficeExecutionContextAccessor();
            using var scope = accessor.BeginScope(CreateContext(propertyId, ActorId.New()));
            var service = new EfHousekeepingSupplyService(db, accessor, new FixedClock(now));
            var restockId = Guid.NewGuid();
            var restock = new LinenTransaction(
                restockId, "BEDSHEET-KING", room.HotelRoomId, LinenAction.Restocked, 4, now.AddHours(-2), null);
            await service.RecordLinenTransactionAsync(restock, CancellationToken.None);
            await service.RecordLinenTransactionAsync(restock, CancellationToken.None);
            await service.RecordLinenTransactionAsync(
                new LinenTransaction(Guid.NewGuid(), "BEDSHEET-KING", room.HotelRoomId, LinenAction.Issued, 1, now, null),
                CancellationToken.None);

            var inventory = await ((ILinenService)service).GetInventoryAsync(propertyId.Value, "1", CancellationToken.None);
            inventory.Should().ContainSingle(x =>
                x.RoomId == room.HotelRoomId && x.RoomNumber == "101" && x.LinenCode == "BEDSHEET-KING" &&
                x.Quantity == 3 && x.LastRestockedUtc.HasValue);
            (await db.HousekeepingLinenTransactions.CountAsync()).Should().Be(2);

            var overIssue = () => service.RecordLinenTransactionAsync(
                new LinenTransaction(Guid.NewGuid(), "BEDSHEET-KING", room.HotelRoomId, LinenAction.Issued, 4, now, null),
                CancellationToken.None);
            await overIssue.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("HK_LINEN_STOCK_INSUFFICIENT");
            (await db.HousekeepingLinenTransactions.CountAsync()).Should().Be(2);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_minibar_restock_and_inventory_persist_to_sql_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_MINIBAR_SQL_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var propertyId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var roomType = HotelRoomType.Create(
                new PropertyId(propertyId), Guid.NewGuid().ToString("N"), "Standard", null, 2, 0, 2, 0, 1, 1, DateTime.UtcNow, null);
            var room = HotelRoom.Create(
                new PropertyId(propertyId), roomType.HotelRoomTypeId, "201", null, "2", null, null, null, DateTime.UtcNow, null);
            db.HotelRoomTypes.Add(roomType);
            db.HotelRooms.Add(room);
            await db.SaveChangesAsync();

            // Minibar restock is intentionally fail-closed: the production flow
            // requires a configured warehouse, minibar catalog item and stock
            // balance. Seed those real prerequisites explicitly instead of
            // relying on unrelated designer data.
            var warehouse = new EntityWarehouse
            {
                WarehouseCode = "MINI",
                WarehouseName = "Kho chính",
                IsConsumptionWarehouse = true,
                IsActive = true
            };
            var water = new EntityItem
            {
                ItemCode = "WATER",
                ItemName = "Nước Lavie",
                Unit = "Chai",
                GroupName = "Minibar",
                IsMinibarItem = true,
                RetailPrice = 25_000m,
                IsActive = true
            };
            var cola = new EntityItem
            {
                ItemCode = "COLA",
                ItemName = "Coca Cola",
                Unit = "Lon",
                GroupName = "Minibar",
                IsMinibarItem = true,
                RetailPrice = 35_000m,
                IsActive = true
            };
            db.Warehouses.Add(warehouse);
            db.Items.AddRange(water, cola);
            await db.SaveChangesAsync();
            db.StockBalances.Add(new EntityStockBalance
            {
                WarehouseId = warehouse.Id,
                ItemId = water.Id,
                OpeningQty = 10,
                ClosingQty = 10,
                AvgPrice = 10_000m
            });
            await db.SaveChangesAsync();

            var now = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc);
            var commands = new EfMinibarCommandService(db);
            var restock = new RestockMinibarCommand(
                Guid.NewGuid(), propertyId, room.HotelRoomId, "WATER", "Nước Lavie", 2,
                "Kho chính", actorId, now, "Bổ sung sau kiểm tra");
            await commands.RestockAsync(restock, CancellationToken.None);
            await commands.RestockAsync(restock, CancellationToken.None);

            var inventory = new InventoryMinibarCommand(
                Guid.NewGuid(), propertyId, room.HotelRoomId, null, actorId, now,
                [
                    new MinibarInventoryLineInput("WATER", "Nước Lavie", 3, 25_000m),
                    new MinibarInventoryLineInput("COLA", "Coca Cola", 2, 35_000m)
                ],
                "Kiểm kê đầu ca");
            await commands.InventoryAsync(inventory, CancellationToken.None);
            await commands.InventoryAsync(inventory, CancellationToken.None);

            (await db.MinibarRestockTransactions.CountAsync()).Should().Be(1);
            (await db.MinibarInventoryAudits.CountAsync()).Should().Be(1);
            (await db.MinibarInventoryLines.CountAsync()).Should().Be(2);

            var snapshot = await new EfMinibarQueryService(db).QueryAsync(propertyId, CancellationToken.None);
            snapshot.Should().ContainSingle(x => x.RoomId == room.HotelRoomId && x.RestockQuantity == 2);
            var roomSnapshot = snapshot.Single(x => x.RoomId == room.HotelRoomId);
            roomSnapshot.LastInventoryAtUtc.Should().Be(now);
            roomSnapshot.InventoryLineCount.Should().Be(2);
            roomSnapshot.InventoryItems.Should().Contain(x => x.ItemCode == "WATER" && x.Quantity == 3);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_invoice_center_adjustment_fields_are_queryable_after_migration()
    {
        var databaseName = $"ASMILE_BO_HOTEL_INVOICE_SCHEMA_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);

        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var service = new ASMILE.BO.Infrastructure.Services.EInvoiceService(
                db,
                new ASMILE.BO.Infrastructure.Services.EInvoiceXmlGenerator(db),
                Array.Empty<ASMILE.BO.Application.Interfaces.IEInvoiceProvider>());

            var invoices = await service.GetInvoicesAsync();

            invoices.Should().BeEmpty();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_confirmed_reservation_creation_persists_contact_and_all_status_history_atomically()
    {
        var databaseName = $"ASMILE_BO_HOTEL_CONFIRMED_CREATE_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var now = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
            var property = PropertyId.New();
            var actor = ActorId.New();
            var accessor = new FrontOfficeExecutionContextAccessor();
            var context = CreateContext(property, actor);
            var clock = new FixedClock(now);
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                accessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);
            var roomType = HotelRoomType.Create(
                property,
                "ATOMIC-DLX",
                "Atomic Deluxe",
                null,
                1,
                0,
                2,
                1,
                1,
                1,
                now,
                actor);
            db.HotelRoomTypes.Add(roomType);
            await db.SaveChangesAsync();

            using (accessor.BeginScope(context))
            {
                var reservationService = CreateReservationService(db, accessor, now);
                var created = await reservationService.CreateAsync(
                    new CreateReservationCommand(
                        property.Value,
                        null,
                        "Atomic Create Guest",
                        new DateOnly(2026, 9, 14),
                        new DateOnly(2026, 9, 16),
                        2,
                        1,
                        roomType.HotelRoomTypeId,
                        null,
                        "Direct",
                        null,
                        null,
                        null,
                        false,
                        ConfirmImmediately: true,
                        GuestPhone: "0907654321",
                        GuestEmail: "atomic.guest@example.test"),
                    CancellationToken.None);

                created.IsSuccess.Should().BeTrue($"{created.Error?.Code}: {created.Error?.Message}");
                created.Value!.Status.Should().Be(ReservationStatus.Confirmed);
                created.Value.GuestPhone.Should().Be("0907654321");
                created.Value.GuestEmail.Should().Be("atomic.guest@example.test");

                var deposit = await reservationService.RecordDepositAsync(
                    new RecordReservationDepositCommand(
                        property.Value,
                        created.Value.ReservationId,
                        500_000m,
                        new DateOnly(2026, 9, 14),
                        created.Value.RowVersion!),
                    CancellationToken.None);
                deposit.IsSuccess.Should().BeTrue($"{deposit.Error?.Code}: {deposit.Error?.Message}");
                deposit.Value!.DepositAmount.Should().Be(500_000m);
                deposit.Value.DepositDueDate.Should().Be(new DateOnly(2026, 9, 14));
                deposit.Value.DepositStatus.Should().Be(DepositStatus.Pending);

                var persisted = await db.FrontOfficeReservations.AsNoTracking()
                    .SingleAsync(x => x.ReservationId == created.Value.ReservationId && x.PropertyId == property.Value);
                persisted.Status.Should().Be(ReservationStatus.Confirmed);
                persisted.GuestNameSnapshot.Should().Be("Atomic Create Guest");
                persisted.GuestPhoneSnapshot.Should().Be("0907654321");
                persisted.GuestEmailSnapshot.Should().Be("atomic.guest@example.test");
                persisted.DepositAmount.Should().Be(500_000m);
                persisted.DepositDueDate.Should().Be(new DateOnly(2026, 9, 14));
                persisted.DepositStatus.Should().Be(DepositStatus.Pending);
                (await db.FrontOfficeReservations.CountAsync(x => x.PropertyId == property.Value && x.Status == ReservationStatus.Draft)).Should().Be(0);

                var history = await db.FrontOfficeReservationHistory.AsNoTracking()
                    .Where(x => x.ReservationId == created.Value.ReservationId)
                    .ToListAsync();
                history.Should().HaveCount(4);
                history.Select(x => x.Action).Should().BeEquivalentTo("Created", "MarkedTentative", "Confirmed", "DepositRecorded");
                history.Single(x => x.Action == "MarkedTentative").FromStatus.Should().Be(ReservationStatus.Draft);
                history.Single(x => x.Action == "MarkedTentative").ToStatus.Should().Be(ReservationStatus.Tentative);
                history.Single(x => x.Action == "Confirmed").FromStatus.Should().Be(ReservationStatus.Tentative);
                history.Single(x => x.Action == "Confirmed").ToStatus.Should().Be(ReservationStatus.Confirmed);
                history.Single(x => x.Action == "DepositRecorded").FromStatus.Should().Be(ReservationStatus.Confirmed);
                history.Single(x => x.Action == "DepositRecorded").ToStatus.Should().Be(ReservationStatus.Confirmed);
                (await db.AuditLogs.CountAsync(x => x.Action == "CreatedAndConfirmed" && x.EntityId == created.Value.ReservationId.ToString("D"))).Should().Be(1);
            }
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task HOTEL_SQL_confirm_draft_reservation_persists_status_and_history()
    {
        var databaseName = $"ASMILE_BO_HOTEL_CONFIRM_DRAFT_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var now = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
            var property = PropertyId.New();
            var actor = ActorId.New();
            var roomType = HotelRoomType.Create(property, "CONFIRM-DLX", "Confirm Deluxe", null, 1, 0, 2, 1, 1, 1, now, actor);
            db.HotelRoomTypes.Add(roomType);
            await db.SaveChangesAsync();
            var accessor = new FrontOfficeExecutionContextAccessor();
            using (accessor.BeginScope(CreateContext(property, actor)))
            {
                var service = CreateReservationService(db, accessor, now);
                var draft = await service.CreateAsync(
                    new CreateReservationCommand(
                        property.Value,
                        null,
                        "Confirm Draft Guest",
                        new DateOnly(2026, 9, 14),
                        new DateOnly(2026, 9, 16),
                        1,
                        0,
                        roomType.HotelRoomTypeId,
                        null,
                        "Direct",
                        null,
                        null,
                        null,
                        false,
                        ConfirmImmediately: false),
                    CancellationToken.None);

                draft.IsSuccess.Should().BeTrue($"{draft.Error?.Code}: {draft.Error?.Message}");
                draft.Value!.Status.Should().Be(ReservationStatus.Draft);
                draft.Value.RowVersion.Should().HaveCount(8);

                var confirmer = CreateReservationService(db, accessor, now.AddMinutes(1));
                var confirmed = await confirmer.ConfirmAsync(
                    new ConfirmReservationCommand(property.Value, draft.Value.ReservationId, draft.Value.RowVersion!),
                    CancellationToken.None);

                confirmed.IsSuccess.Should().BeTrue($"{confirmed.Error?.Code}: {confirmed.Error?.Message}");
                confirmed.Value!.Status.Should().Be(ReservationStatus.Confirmed);
                var persisted = await db.FrontOfficeReservations.AsNoTracking()
                    .SingleAsync(x => x.ReservationId == draft.Value.ReservationId && x.PropertyId == property.Value);
                persisted.Status.Should().Be(ReservationStatus.Confirmed);

                var history = await db.FrontOfficeReservationHistory.AsNoTracking()
                    .Where(x => x.ReservationId == draft.Value.ReservationId)
                    .OrderBy(x => x.OccurredAtUtc)
                    .ToListAsync();
                history.Select(x => x.Action).Should().ContainInOrder("Created", "Confirmed");
                history.Single(x => x.Action == "Confirmed").FromStatus.Should().Be(ReservationStatus.Draft);
                history.Single(x => x.Action == "Confirmed").ToStatus.Should().Be(ReservationStatus.Confirmed);
            }
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task HOTEL_E2E_individual_reservation_prepare_checkin_checkout_is_persisted()
    {
        var databaseName = $"ASMILE_BO_HOTEL_INDIVIDUAL_E2E_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildIntegrationConnectionString(databaseName))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await PrepareSchemaAtFoundationPredecessorAsync(db);
            await db.Database.MigrateAsync();

            var now = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
            var property = PropertyId.New();
            var actor = ActorId.New();
            var accessor = new FrontOfficeExecutionContextAccessor();
            var context = CreateContext(property, actor);
            var clock = new FixedClock(now);
            var persistence = new EfFrontOfficePersistenceSession(
                db,
                accessor,
                new PassthroughIntegrationEventMapper(),
                new CanonicalOutboxMessageFactory(clock),
                new FrontOfficeAuditWriter(),
                clock);

            var roomType = HotelRoomType.Create(
                property,
                "IND-DLX",
                "Individual Deluxe",
                null,
                1,
                0,
                2,
                1,
                1,
                1,
                now,
                actor);
            var room = HotelRoom.Create(
                property,
                roomType.HotelRoomTypeId,
                "IND-101",
                null,
                "1",
                "A",
                null,
                null,
                now,
                actor);
            var moveTargetRoom = HotelRoom.Create(
                property,
                roomType.HotelRoomTypeId,
                "IND-102",
                null,
                "1",
                "B",
                null,
                null,
                now,
                actor);
            var guest = GuestProfile.Create(
                property.Value,
                "IND-GUEST-001",
                "Individual E2E Guest",
                "0900000001",
                null,
                GuestSource.Direct,
                now,
                actor);
            db.HotelRoomTypes.Add(roomType);
            db.HotelRooms.AddRange(room, moveTargetRoom);
            db.GuestProfiles.Add(guest);
            await db.SaveChangesAsync();

            var individualReservationId = Guid.Empty;
            var futureReservationId = Guid.Empty;
            using (accessor.BeginScope(context))
            {
                var reservationService = CreateReservationService(db, accessor, now);
                var created = await reservationService.CreateAsync(
                    new CreateReservationCommand(
                        property.Value,
                        guest.GuestProfileId,
                        guest.FullName,
                        new DateOnly(2026, 9, 14),
                        new DateOnly(2026, 9, 15),
                        1,
                        0,
                        roomType.HotelRoomTypeId,
                        null,
                        "Direct",
                        null,
                        null,
                        null,
                        false),
                    CancellationToken.None);
                created.IsSuccess.Should().BeTrue();
                individualReservationId = created.Value!.ReservationId;

                var updated = await reservationService.UpdateDetailsAsync(
                    new UpdateReservationDetailsCommand(
                        property.Value,
                        created.Value!.ReservationId,
                        "Individual E2E Guest Updated",
                        "0900000002",
                        "individual.updated@example.test",
                        roomType.HotelRoomTypeId,
                        null,
                        "Late arrival",
                        "Call before arrival",
                        created.Value.RowVersion!,
                        new DateOnly(2026, 9, 14),
                        new DateOnly(2026, 9, 16),
                        2,
                        0,
                        "OTA"),
                    CancellationToken.None);
                updated.IsSuccess.Should().BeTrue($"{updated.Error?.Code}: {updated.Error?.Message}");
                updated.Value!.GuestName.Should().Be("Individual E2E Guest Updated");
                updated.Value.GuestPhone.Should().Be("0900000002");
                updated.Value.GuestEmail.Should().Be("individual.updated@example.test");
                updated.Value.ArrivalDate.Should().Be(new DateOnly(2026, 9, 14));
                updated.Value.DepartureDate.Should().Be(new DateOnly(2026, 9, 16));
                updated.Value.AdultCount.Should().Be(2);
                updated.Value.ChildCount.Should().Be(0);
                updated.Value.SpecialRequests.Should().Be("Late arrival");
                updated.Value.InternalNotes.Should().Be("Call before arrival");
                updated.Value.Source.Should().Be("OTA");

                db.ChangeTracker.Clear();
                var persistedEdit = await new EfReservationRepository(db).GetAsync(
                    updated.Value.ReservationId, property.Value, CancellationToken.None);
                persistedEdit.Should().NotBeNull();
                persistedEdit!.GuestNameSnapshot.Should().Be("Individual E2E Guest Updated");
                persistedEdit.GuestPhoneSnapshot.Should().Be("0900000002");
                persistedEdit.GuestEmailSnapshot.Should().Be("individual.updated@example.test");
                persistedEdit.ArrivalDate.Should().Be(new DateOnly(2026, 9, 14));
                persistedEdit.DepartureDate.Should().Be(new DateOnly(2026, 9, 16));
                persistedEdit.AdultCount.Should().Be(2);
                persistedEdit.Source.Should().Be("OTA");

                var tentative = await reservationService.TentativeAsync(
                    new MarkReservationTentativeCommand(property.Value, created.Value.ReservationId, updated.Value.RowVersion!),
                    CancellationToken.None);
                tentative.IsSuccess.Should().BeTrue();

                var confirmed = await reservationService.ConfirmAsync(
                    new ConfirmReservationCommand(property.Value, created.Value.ReservationId, tentative.Value!.RowVersion!),
                    CancellationToken.None);
                confirmed.IsSuccess.Should().BeTrue();

                var assigned = await reservationService.AssignAsync(
                    new AssignReservationRoomCommand(property.Value, created.Value.ReservationId, room.HotelRoomId, confirmed.Value!.RowVersion!),
                    CancellationToken.None);
                assigned.IsSuccess.Should().BeTrue();

                var stayService = new HotelStayApplicationService(
                    new EfHotelStayRepository(db),
                    new SqlHotelStayNumberGenerator(db),
                    new EfReservationRepository(db),
                    new EfHotelRoomMasterDataRepository(db),
                    new EfHotelStayOccupantRepository(db),
                    new EfGuestProfileRepository(db),
                    new EfHotelFolioRepository(db),
                    persistence,
                    accessor,
                    new AllowAllAuthorization(),
                    clock);

                var prepared = await stayService.PrepareFromReservationAsync(
                    new PrepareStayFromReservationCommand(created.Value.ReservationId),
                    CancellationToken.None);
                prepared.IsSuccess.Should().BeTrue();

                var preArrival = await stayService.ConfirmPreArrivalAsync(
                    new ConfirmPreArrivalCommand(prepared.Value!.HotelStayId, prepared.Value.RowVersion!),
                    CancellationToken.None);
                preArrival.IsSuccess.Should().BeTrue();

                var preCheckIn = await stayService.PreCheckInAsync(
                    new PreCheckInHotelStayCommand(prepared.Value.HotelStayId, preArrival.Value!.RowVersion!),
                    CancellationToken.None);
                preCheckIn.IsSuccess.Should().BeTrue();

                var checkedIn = await stayService.CheckInAsync(
                    new CheckInHotelStayCommand(prepared.Value.HotelStayId, preCheckIn.Value!.RowVersion!, now),
                    CancellationToken.None);
                checkedIn.IsSuccess.Should().BeTrue();
                checkedIn.Value!.Status.Should().Be(HotelStayStatus.InHouse);

                var futureReservation = await reservationService.CreateAsync(
                    new CreateReservationCommand(
                        property.Value,
                        guest.GuestProfileId,
                        "Individual E2E Future Guest",
                        new DateOnly(2026, 9, 16),
                        new DateOnly(2026, 9, 18),
                        1,
                        0,
                        roomType.HotelRoomTypeId,
                        null,
                        "Direct",
                        null,
                        null,
                        null,
                        false),
                    CancellationToken.None);
                futureReservation.IsSuccess.Should().BeTrue($"{futureReservation.Error?.Code}: {futureReservation.Error?.Message}");
                futureReservationId = futureReservation.Value!.ReservationId;
                var futureTentative = await reservationService.TentativeAsync(
                    new MarkReservationTentativeCommand(property.Value, futureReservation.Value!.ReservationId, futureReservation.Value.RowVersion!),
                    CancellationToken.None);
                futureTentative.IsSuccess.Should().BeTrue();
                var futureConfirmed = await reservationService.ConfirmAsync(
                    new ConfirmReservationCommand(property.Value, futureReservation.Value.ReservationId, futureTentative.Value!.RowVersion!),
                    CancellationToken.None);
                futureConfirmed.IsSuccess.Should().BeTrue();
                var futureAssigned = await reservationService.AssignAsync(
                    new AssignReservationRoomCommand(property.Value, futureReservation.Value.ReservationId, room.HotelRoomId, futureConfirmed.Value!.RowVersion!),
                    CancellationToken.None);
                futureAssigned.IsSuccess.Should().BeTrue($"{futureAssigned.Error?.Code}: {futureAssigned.Error?.Message}");

                var stayAdjustmentService = new StayAdjustmentApplicationService(
                    new EfHotelStayRepository(db),
                    new EfHotelRoomMasterDataRepository(db),
                    new EfReservationRepository(db),
                    new EfHotelFolioRepository(db),
                    new SqlHotelFolioNumberGenerator(db),
                    persistence,
                    accessor,
                    new AllowAllAuthorization(),
                    clock);
                var conflictingExtension = await stayAdjustmentService.AdjustAsync(
                    new AdjustStayCommand(
                        prepared.Value.HotelStayId,
                        checkedIn.Value.RowVersion!,
                        room.HotelRoomId,
                        new DateOnly(2026, 9, 17),
                        75_000m,
                        "Extend into a reserved room"),
                    CancellationToken.None);
                conflictingExtension.IsFailure.Should().BeTrue();
                conflictingExtension.Error!.Code.Should().Be("STAY_ROOM_RESERVATION_CONFLICT");
                db.ChangeTracker.Clear();
                (await db.HotelStays.SingleAsync(x => x.HotelStayId == prepared.Value.HotelStayId))
                    .ScheduledDepartureDate.Should().Be(new DateOnly(2026, 9, 16));
                (await db.HotelFolios.CountAsync(x => x.HotelStayId == prepared.Value.HotelStayId)).Should().Be(0);

                var adjusted = await stayAdjustmentService.AdjustAsync(
                    new AdjustStayCommand(
                        prepared.Value.HotelStayId,
                        checkedIn.Value.RowVersion!,
                        moveTargetRoom.HotelRoomId,
                        new DateOnly(2026, 9, 17),
                        75_000m,
                        "Move and extend to an available room"),
                    CancellationToken.None);
                adjusted.IsSuccess.Should().BeTrue($"{adjusted.Error?.Code}: {adjusted.Error?.Message}");
                adjusted.Value!.Stay.RoomId.Should().Be(moveTargetRoom.HotelRoomId);
                adjusted.Value.Stay.ScheduledDepartureDate.Should().Be(new DateOnly(2026, 9, 17));
                adjusted.Value.FolioEntry.Should().NotBeNull();
                adjusted.Value.FolioEntry!.EntryType.Should().Be(HotelFolioEntryType.RoomCharge);
                adjusted.Value.FolioEntry!.Amount.Should().Be(75_000m);
                var finalStayRowVersion = adjusted.Value.Stay.RowVersion!;
                adjusted.Value.Folio.Should().NotBeNull();
                var folio = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value,
                    adjusted.Value.Folio!.HotelFolioId,
                    CancellationToken.None);
                folio.Should().NotBeNull();
                var folioLedger = await new EfHotelFolioRepository(db).GetLedgerAsync(
                    property.Value,
                    adjusted.Value.Folio!.HotelFolioId,
                    CancellationToken.None);
                folioLedger.Should().NotBeNull();
                folioLedger!.ReservationNumber.Should().Be(created.Value.ReservationNumber);
                folioLedger.ArrivalDate.Should().Be(new DateOnly(2026, 9, 14));
                folioLedger.DepartureDate.Should().Be(new DateOnly(2026, 9, 17));
                folioLedger.RoomTypeCode.Should().Be("IND-DLX");
                folioLedger.Source.Should().Be("OTA");
                var searchedLedgers = await new EfHotelFolioRepository(db).SearchAsync(
                    new FolioSearchFilter(property.Value, null, null, null, null, null, null, null),
                    new PageRequest(1, 50),
                    CancellationToken.None);
                searchedLedgers.Items.Should().ContainSingle(x => x.HotelFolioId == folioLedger.HotelFolioId);
                var searchedLedger = searchedLedgers.Items.Single(x => x.HotelFolioId == folioLedger.HotelFolioId);
                searchedLedger.ReservationNumber.Should().Be(created.Value.ReservationNumber);
                searchedLedger.ArrivalDate.Should().Be(new DateOnly(2026, 9, 14));
                searchedLedger.DepartureDate.Should().Be(new DateOnly(2026, 9, 17));
                searchedLedger.Entries.Should().Contain(x => x.EntryType == HotelFolioEntryType.RoomCharge);

                var roomCharge = folio!.AddRoomCharge(
                    100_000m,
                    "Individual E2E room charge",
                    new DateOnly(2026, 9, 14),
                    null,
                    now,
                    actor);
                db.HotelFolioEntries.Add(roomCharge);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                var minibarCommands = new EfMinibarCommandService(db);
                await minibarCommands.CheckAsync(
                    new CheckMinibarCommand(
                        Guid.Empty,
                        property.Value,
                        prepared.Value.HotelStayId,
                        moveTargetRoom.HotelRoomId,
                        actor.Value,
                        now.AddHours(1),
                        [new MinibarCheckLine("WATER", "Nước Lavie", 1, 25_000m)]),
                    CancellationToken.None);
                var minibarSessionId = await db.MinibarSessions
                    .Where(x => x.PropertyId == property.Value && x.StayId == prepared.Value.HotelStayId)
                    .Select(x => x.SessionId)
                    .SingleAsync();
                var minibarFolio = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value, folio.HotelFolioId, CancellationToken.None);
                var minibarEntry = minibarFolio!.AddMiniBarCharge(
                    25_000m, "Minibar water", minibarSessionId, now.AddHours(1), actor);
                db.HotelFolioEntries.Add(minibarEntry);
                await db.SaveChangesAsync();
                await minibarCommands.PostAsync(
                    new PostMinibarCommand(
                        minibarSessionId, property.Value, minibarEntry.HotelFolioEntryId, actor.Value, now.AddHours(1)),
                    CancellationToken.None);
                var persistedMinibarSession = await db.MinibarSessions
                    .Include(x => x.Consumptions)
                    .SingleAsync(x => x.SessionId == minibarSessionId);
                persistedMinibarSession.Status.Should().Be("Posted");
                persistedMinibarSession.FolioEntryId.Should().Be(minibarEntry.HotelFolioEntryId);
                persistedMinibarSession.Consumptions.Should().ContainSingle(x =>
                    x.ItemCode == "WATER" && x.Quantity == 1m && x.LineAmount == 25_000m);
                db.ChangeTracker.Clear();

                var paymentService = new HotelPaymentApplicationService(
                    new EfHotelPaymentRepository(db),
                    new EfHotelFolioRepository(db),
                    new SqlHotelPaymentNumberGenerator(db),
                    persistence,
                    accessor,
                    new AllowAllAuthorization(),
                    clock);
                var paymentFolio = await new EfHotelFolioRepository(db).GetAsync(property.Value, folio.HotelFolioId, CancellationToken.None);
                var received = await paymentService.ReceiveAsync(
                    new ReceivePaymentCommand(
                        folio.HotelFolioId,
                        paymentFolio!.RowVersion!,
                        HotelPaymentMethod.Cash,
                        100_000m),
                    CancellationToken.None);
                received.IsSuccess.Should().BeTrue();
                received.Value!.Amount.Should().Be(100_000m);

                // Verify the financial lifecycle against SQL Server: partial refund for a
                // completed cash payment, then void an authorized card payment.
                db.ChangeTracker.Clear();
                var cashPayment = await new EfHotelPaymentRepository(db).GetAsync(
                    property.Value,
                    received.Value.HotelPaymentId,
                    CancellationToken.None);
                cashPayment.Should().NotBeNull();
                cashPayment!.RowVersion.Should().Equal(received.Value.RowVersion);
                var refunded = await paymentService.RefundAsync(
                    new RefundPaymentCommand(
                        cashPayment.HotelPaymentId,
                        cashPayment.RowVersion!,
                        40_000m,
                        "Customer request"),
                    CancellationToken.None);
                refunded.IsSuccess.Should().BeTrue($"{refunded.Error?.Code}: {refunded.Error?.Message}");
                refunded.Value!.Status.Should().Be(HotelPaymentStatus.PartiallyRefunded);
                refunded.Value.RefundedAmount.Should().Be(40_000m);

                db.ChangeTracker.Clear();
                var folioAfterRefund = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value,
                    folio.HotelFolioId,
                    CancellationToken.None);
                folioAfterRefund.Should().NotBeNull();
                var secondRoomCharge = folioAfterRefund!.AddRoomCharge(
                    50_000m,
                    "Individual E2E second room charge",
                    new DateOnly(2026, 9, 14),
                    null,
                    now,
                    actor);
                db.HotelFolioEntries.Add(secondRoomCharge);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                var cardFolio = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value,
                    folio.HotelFolioId,
                    CancellationToken.None);
                var cardReceived = await paymentService.ReceiveAsync(
                    new ReceivePaymentCommand(
                        folio.HotelFolioId,
                        cardFolio!.RowVersion!,
                        HotelPaymentMethod.CreditCard,
                        50_000m),
                    CancellationToken.None);
                cardReceived.IsSuccess.Should().BeTrue();
                cardReceived.Value!.Status.Should().Be(HotelPaymentStatus.Authorized);

                db.ChangeTracker.Clear();
                var cardPayment = await new EfHotelPaymentRepository(db).GetAsync(
                    property.Value,
                    cardReceived.Value.HotelPaymentId,
                    CancellationToken.None);
                cardPayment.Should().NotBeNull();
                var voided = await paymentService.VoidAsync(
                    new VoidPaymentCommand(
                        cardPayment!.HotelPaymentId,
                        cardPayment.RowVersion!,
                        "Terminal cancellation"),
                    CancellationToken.None);
                voided.IsSuccess.Should().BeTrue();
                voided.Value!.Status.Should().Be(HotelPaymentStatus.Voided);

                var blockedCheckout = await stayService.CheckOutAsync(
                    new CheckOutHotelStayCommand(
                        prepared.Value.HotelStayId,
                        finalStayRowVersion,
                        now.AddHours(8),
                        "Normal departure",
                        "Individual E2E completed"),
                    CancellationToken.None);
                blockedCheckout.IsFailure.Should().BeTrue();
                blockedCheckout.Error!.Code.Should().Be("STAY_OUTSTANDING_BALANCE");

                db.ChangeTracker.Clear();
                var settlementFolio = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value,
                    folio.HotelFolioId,
                    CancellationToken.None);
                settlementFolio.Should().NotBeNull();
                var finalPayment = await paymentService.ReceiveAsync(
                    new ReceivePaymentCommand(
                        folio.HotelFolioId,
                        settlementFolio!.RowVersion!,
                        HotelPaymentMethod.Cash,
                        190_000m),
                    CancellationToken.None);
                finalPayment.IsSuccess.Should().BeTrue();
                finalPayment.Value!.Amount.Should().Be(190_000m);

                var invoiceFolio = await new EfHotelFolioRepository(db).GetAsync(
                    property.Value,
                    folio.HotelFolioId,
                    CancellationToken.None);
                invoiceFolio.Should().NotBeNull();
                var settlementService = new ASMILE.BO.Application.FrontOffice.Settlement.SettlementApplicationService(
                    new EfHotelFolioRepository(db),
                    new EfHotelInvoiceRepository(db),
                    new SqlHotelInvoiceNumberGenerator(db),
                    new EfHotelStayRepository(db),
                    persistence,
                    accessor,
                    new AllowAllAuthorization(),
                    clock);
                var invoiceCommand = new ASMILE.BO.Application.FrontOffice.Settlement.GenerateInvoiceCommand(
                    folio.HotelFolioId,
                    invoiceFolio!.RowVersion!);
                var staleInvoice = await settlementService.GenerateInvoiceAsync(
                    invoiceCommand with { FolioExpectedRowVersion = new byte[8] },
                    CancellationToken.None);
                staleInvoice.IsFailure.Should().BeTrue();
                staleInvoice.Error!.Code.Should().Be("INVOICE_CONCURRENCY_CONFLICT");

                var invoice = await settlementService.GenerateInvoiceAsync(invoiceCommand, CancellationToken.None);
                invoice.IsSuccess.Should().BeTrue($"{invoice.Error?.Code}: {invoice.Error?.Message}");
                invoice.Value!.TotalAmount.Should().Be(250_000m);
                invoice.Value.OutstandingAmount.Should().Be(0m);
                invoice.Value.PaidAmount.Should().Be(invoice.Value.TotalAmount);
                invoice.Value.GuestName.Should().Be("Individual E2E Guest Updated");
                invoice.Value.RoomNumber.Should().Be("IND-102");

                var duplicateInvoice = await settlementService.GenerateInvoiceAsync(invoiceCommand, CancellationToken.None);
                duplicateInvoice.IsFailure.Should().BeTrue();
                duplicateInvoice.Error!.Code.Should().Be("INVOICE_ALREADY_EXISTS");

                var checkedOut = await stayService.CheckOutAsync(
                    new CheckOutHotelStayCommand(
                        prepared.Value.HotelStayId,
                        finalStayRowVersion,
                        now.AddHours(8),
                        "Normal departure",
                        "Individual E2E completed"),
                    CancellationToken.None);
                checkedOut.IsSuccess.Should().BeTrue();
                checkedOut.Value!.Status.Should().Be(HotelStayStatus.CheckedOut);
            }

            db.ChangeTracker.Clear();
            (await db.FrontOfficeReservations.SingleAsync(x => x.ReservationId == individualReservationId))
                .Status.Should().Be(ReservationStatus.CheckedIn);
            (await db.FrontOfficeReservations.SingleAsync(x => x.ReservationId == futureReservationId))
                .Status.Should().Be(ReservationStatus.Confirmed);
            var persistedStay = await db.HotelStays.SingleAsync(x => x.PropertyId == property.Value);
            persistedStay.Status.Should().Be(HotelStayStatus.CheckedOut);
            persistedStay.RoomId.Should().Be(moveTargetRoom.HotelRoomId);
            persistedStay.ScheduledDepartureDate.Should().Be(new DateOnly(2026, 9, 17));
            persistedStay.ActualCheckInAtUtc.Should().Be(now);
            persistedStay.ActualCheckOutAtUtc.Should().Be(now.AddHours(8));
            var persistedFolio = await db.HotelFolios
                .Include(x => x.Entries)
                .SingleAsync(x => x.HotelStayId == persistedStay.HotelStayId);
            var persistedInvoice = await db.HotelInvoices.SingleAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId);
            persistedInvoice.Status.Should().Be(ASMILE.BO.Core.FrontOffice.Settlement.InvoiceStatus.Generated);
            persistedInvoice.TotalAmount.Should().Be(250_000m);
            persistedInvoice.OutstandingAmount.Should().Be(persistedFolio.OutstandingBalance);
            persistedInvoice.PaidAmount.Should().Be(persistedInvoice.TotalAmount - persistedInvoice.OutstandingAmount);
            persistedFolio.TotalCharges.Should().Be(340_000m);
            persistedFolio.TotalPayments.Should().Be(340_000m);
            persistedFolio.OutstandingBalance.Should().Be(0m);
            (await db.HotelPayments.CountAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId)).Should().Be(3);
            (await db.HotelFolioEntries.CountAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId && x.EntryType == HotelFolioEntryType.Payment)).Should().Be(3);
            (await db.HotelFolioEntries.CountAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId && x.EntryType == HotelFolioEntryType.Refund)).Should().Be(1);
            (await db.HotelFolioEntries.CountAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId && x.EntryType == HotelFolioEntryType.Correction)).Should().Be(1);
            (await db.HotelFolioEntries.CountAsync(x => x.HotelFolioId == persistedFolio.HotelFolioId && x.EntryType == HotelFolioEntryType.MiniBar)).Should().Be(1);
            var persistedCashPayment = await db.HotelPayments.SingleAsync(x => x.PaymentMethod == HotelPaymentMethod.Cash && x.Amount == 100_000m);
            persistedCashPayment.Status.Should().Be(HotelPaymentStatus.PartiallyRefunded);
            persistedCashPayment.RefundedAmount.Should().Be(40_000m);
            (await db.HotelPayments.SingleAsync(x => x.PaymentMethod == HotelPaymentMethod.Cash && x.Amount == 190_000m)).Status.Should().Be(HotelPaymentStatus.Completed);
            var persistedCardPayment = await db.HotelPayments.SingleAsync(x => x.PaymentMethod == HotelPaymentMethod.CreditCard);
            persistedCardPayment.Status.Should().Be(HotelPaymentStatus.Voided);
            var reversedCardPaymentEntry = await db.HotelFolioEntries.SingleAsync(x => x.HotelFolioEntryId == persistedCardPayment.FolioEntryId);
            reversedCardPaymentEntry.EntryType.Should().Be(HotelFolioEntryType.Payment);
            var cardVoidEntry = await db.HotelFolioEntries.SingleAsync(x => x.EntryType == HotelFolioEntryType.Correction
                && x.ReversesEntryId == persistedCardPayment.FolioEntryId);
            reversedCardPaymentEntry.ReversedByEntryId.Should().Be(cardVoidEntry.HotelFolioEntryId);
            (await db.HotelStayHistories.CountAsync(x => x.HotelStayId == persistedStay.HotelStayId))
                .Should().Be(7);
            (await db.HotelStayHistories.CountAsync(x => x.HotelStayId == persistedStay.HotelStayId && x.Action == "StayRoomMoved"))
                .Should().Be(1);
            (await db.HotelStayHistories.CountAsync(x => x.HotelStayId == persistedStay.HotelStayId && x.Action == "StayExtended"))
                .Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "StayAdjustedWithFolio" && x.EntityId == persistedStay.HotelStayId.ToString("D")))
                .Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "StayPrepared")).Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "StayCheckedIn")).Should().Be(1);
            (await db.AuditLogs.CountAsync(x => x.Action == "StayCheckedOut")).Should().Be(1);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private static FrontOfficeExecutionContext CreateContext() => CreateContext(PropertyId.New(), ActorId.New());

    private static string BuildIntegrationConnectionString(string databaseName)
    {
        var template = Environment.GetEnvironmentVariable("ASMILE_BO_TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(template))
        {
            var builder = new SqlConnectionStringBuilder(template)
            {
                InitialCatalog = databaseName,
                ConnectTimeout = 20,
                TrustServerCertificate = true
            };
            return builder.ConnectionString;
        }

        return $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;";
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId, ActorId actorId)
    {
        return new FrontOfficeExecutionContext(
            new ActorContext(actorId, new HashSet<string> { "System" }, "test-session", "test-workstation", true),
            new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(propertyId, new DateOnly(2026, 7, 17), "SQL Server test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "sql-server-test"));
    }

    private static ReservationApplicationService CreateReservationService(AppDbContext db, FrontOfficeExecutionContextAccessor accessor, DateTime now)
    {
        var clock = new FixedClock(now); var repo = new EfReservationRepository(db); return new ReservationApplicationService(repo, new SqlReservationNumberGenerator(db), new EfHotelRoomMasterDataRepository(db), new EfFrontOfficePersistenceSession(db, accessor, new PassthroughIntegrationEventMapper(), new CanonicalOutboxMessageFactory(clock), new FrontOfficeAuditWriter(), clock), accessor, new AllowAllAuthorization(), clock, repo);
    }
    private sealed class AllowAllAuthorization : IAuthorizationService { public bool HasPermission(string permissionCode) => true; public void Demand(string permissionCode) { } public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true); public bool HasPermission(Permission permission) => true; public void Demand(Permission permission) { } public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true); }

    private sealed class FixedClock(DateTime utcNow) : IClock { public DateTime UtcNow => utcNow; }
    private sealed class ThrowAt(ProcessFaultPoint point) : IProcessFaultInjector { public Task CheckpointAsync(ProcessFaultPoint candidate, CancellationToken cancellationToken = default) => candidate == point ? Task.FromException(new InjectedProcessFault()) : Task.CompletedTask; }
    private sealed class InjectedProcessFault : Exception;
    private static ProcessDefinition<AtomicState, AtomicStep, AtomicSignal> AtomicDefinition() => new("Atomic.Test", 1, AtomicState.Started, AtomicStep.Wait, [AtomicState.Completed], [new ProcessTransition<AtomicState, AtomicStep, AtomicSignal>(AtomicState.Started, AtomicSignal.Accepted, AtomicState.Completed, null)]);
    private enum AtomicState { Started, Completed }
    private enum AtomicStep { Wait }
    private enum AtomicSignal { Accepted }
    private sealed record TestEvent(EventId EventId, DateTime OccurredAtUtc) : IFrontOfficeDomainEvent { public string EventName => "FrontOffice.Foundation.SqlServerTested"; public object Payload => new { Source = "sql-server-test" }; }

    private static async Task PrepareSchemaAtFoundationPredecessorAsync(AppDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        var migrations = db.Database.GetMigrations().ToList();
        var foundationMigration = migrations.Single(x => x.EndsWith("_CreateFrontOfficeFoundationTechnicalStores", StringComparison.Ordinal));
        var predecessor = migrations.TakeWhile(x => x != foundationMigration).LastOrDefault() ?? "0";
        await db.Database.GetInfrastructure().GetRequiredService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync(predecessor);
    }
}
