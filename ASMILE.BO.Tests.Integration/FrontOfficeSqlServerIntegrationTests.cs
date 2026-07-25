using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Integration;

public sealed class FrontOfficeSqlServerIntegrationTests
{
    [Fact]
    public async Task LocalDb_migration_and_technical_stores_are_durable_and_transactional()
    {
        var databaseName = $"ASMILE_BO_FO_TEST_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;";
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
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;";
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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
            db.HotelRoomTypes.AddRange(typeA, typeB); db.HotelRooms.AddRange(roomA, outOfOrder); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var repository = new EfHotelRoomMasterDataRepository(db);
            (await repository.GetRoomTypeDtoAsync(propertyB.Value, typeA.HotelRoomTypeId)).Should().BeNull();
            (await repository.SearchRoomTypesAsync(propertyA.Value, null, null, new(1, 20), null)).Items.Should().ContainSingle();
            (await repository.SearchRoomsAsync(propertyA.Value, null, null, null, null, null, null, new(1, 20), null, true)).Items.Should().ContainSingle(x => x.RoomNumber == "101");
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10;").Options;
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

    private static FrontOfficeExecutionContext CreateContext() => CreateContext(PropertyId.New(), ActorId.New());
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
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE [fo].[fo_hop_thu_den];");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE [fo].[fo_hop_thu_di];");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE [fo].[fo_khoa_idempotency];");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE [fo].[PhongKhachSan]; DROP TABLE [fo].[LoaiPhongKhachSan]; DROP TABLE [fo].[LichSuDatPhong]; DROP TABLE [fo].[DatPhong]; DROP SEQUENCE [fo].[TrinhTuSoDatPhong];");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE [fo_process].[LichSuQuyTrinh]; DROP TABLE [fo_process].[HanhDongThuCongQuyTrinh]; DROP TABLE [fo_process].[BuTruQuyTrinh]; DROP TABLE [fo_process].[ThuDaChetQuyTrinh]; DROP TABLE [fo_process].[HenGioQuyTrinh]; DROP TABLE [fo_process].[TinHieuQuyTrinh]; DROP TABLE [fo_process].[LanThucHienBuocQuyTrinh]; DROP TABLE [fo_process].[BuocQuyTrinh]; DROP TABLE [fo_process].[QuyTrinhVanHanh];");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);");

        var migrations = db.Database.GetMigrations().ToList();
        var foundationMigration = migrations.Single(x => x.EndsWith("_CreateFrontOfficeFoundationTechnicalStores", StringComparison.Ordinal));
        foreach (var migration in migrations.TakeWhile(x => x != foundationMigration))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({migration}, {"8.0.7"});");
        }
    }
}
