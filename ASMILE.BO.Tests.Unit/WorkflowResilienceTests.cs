using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class WorkflowResilienceTests
{
    [Fact]
    public async Task BeginWorkflow_IdempotencyKey_TranhTaoBanGhiTrung()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();

        var first = await service.BeginWorkflowAsync("posting", "JV-001", "idem-001", new { amount = 1000m });
        var second = await service.BeginWorkflowAsync("posting", "JV-001", "idem-001", new { amount = 1000m });

        second.Id.Should().Be(first.Id);
        (await fixture.Db.WorkflowExecutions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RetryWorkflow_GhiNhanLichSuRetry()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("import", "IMP-001");

        await service.RetryWorkflowAsync(execution.Id, "manual-retry");

        var entity = await fixture.Db.WorkflowExecutions.SingleAsync(x => x.Id == execution.Id);
        entity.RetryCount.Should().Be(1);
        entity.Status.Should().Be("Retrying");
        (await fixture.Db.WorkflowRetryHistories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ResumeWorkflow_GiuRecoveryState()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("report-export", "REP-001");

        await service.ResumeWorkflowAsync(execution.Id, new { cursor = "page-2" });

        var entity = await fixture.Db.WorkflowExecutions.SingleAsync(x => x.Id == execution.Id);
        entity.Status.Should().Be("Resumed");
        entity.RecoveryStateJson.Should().Contain("page-2");
    }

    [Fact]
    public async Task ExecuteCompensation_DanhDauHoanTat()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("archive-recovery", "AR-001");

        await service.RegisterCompensationAsync(execution.Id, null, "cleanup-temp", new { path = "tmp/file" });
        var count = await service.ExecuteCompensationAsync(execution.Id);

        count.Should().Be(1);
        (await fixture.Db.WorkflowCompensationActions.SingleAsync()).Status.Should().Be("Completed");
    }

    [Fact]
    public async Task CleanupStaleWorkflow_DanhDauFailed()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("scheduler", "JOB-001");
        var entity = await fixture.Db.WorkflowExecutions.SingleAsync(x => x.Id == execution.Id);
        entity.Status = "Running";
        entity.LastHeartbeatUtc = DateTime.UtcNow.AddHours(-2);
        await fixture.Db.SaveChangesAsync();

        var count = await service.CleanupStaleWorkflowAsync(DateTime.UtcNow, TimeSpan.FromMinutes(10));

        count.Should().Be(1);
        (await fixture.Db.WorkflowExecutions.SingleAsync(x => x.Id == execution.Id)).Status.Should().Be("Failed");
    }

    [Fact]
    public async Task ValidateConsistency_PhatHienDuplicateExecution()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.WorkflowExecutions.Add(new WorkflowExecution
        {
            WorkflowType = "posting",
            WorkflowKey = "JV-002",
            CorrelationId = Guid.NewGuid().ToString("N"),
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LastHeartbeatUtc = DateTime.UtcNow
        });
        fixture.Db.WorkflowExecutions.Add(new WorkflowExecution
        {
            WorkflowType = "posting",
            WorkflowKey = "JV-002",
            CorrelationId = Guid.NewGuid().ToString("N"),
            Status = "Started",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-4),
            LastHeartbeatUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateService();

        var violations = await service.ValidateConsistencyAsync(DateTime.UtcNow);

        violations.Should().Contain(x => x.Category == "DuplicateExecution");
    }

    [Fact]
    public async Task AcquireLeaseVaDistributedLock_GhiNhanNguonLuc()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();

        var lease = await service.AcquireLeaseAsync("scheduler", "job-lease", TimeSpan.FromMinutes(5));
        var lockEntry = await service.AcquireDistributedLockAsync("workflow", "job-lock", TimeSpan.FromMinutes(5));

        lease.Status.Should().Be("Leased");
        lockEntry.Status.Should().Be("Locked");
        (await fixture.Db.WorkflowLeases.CountAsync()).Should().Be(1);
        (await fixture.Db.WorkflowDistributedLocks.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ReleaseStaleLocksAsync_GiaiPhongHetHan()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.WorkflowLeases.Add(new WorkflowLease
        {
            WorkflowType = "scheduler",
            WorkflowKey = "stale",
            LeaseKey = "SCHEDULER::STALE",
            Status = "Leased",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        fixture.Db.WorkflowDistributedLocks.Add(new WorkflowDistributedLock
        {
            LockScope = "workflow",
            LockKey = "stale",
            WorkflowType = "workflow",
            WorkflowKey = "stale",
            Status = "Locked",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await fixture.Db.SaveChangesAsync();

        var service = fixture.CreateService();
        var count = await service.ReleaseStaleLocksAsync(DateTime.UtcNow);

        count.Should().Be(2);
        (await fixture.Db.WorkflowLeases.SingleAsync()).Status.Should().Be("Expired");
        (await fixture.Db.WorkflowDistributedLocks.SingleAsync()).Status.Should().Be("Released");
    }

    [Fact]
    public async Task CheckpointResume_RebuildsWorkflowExecution()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("report", "REP-009");
        var checkpoint = await service.CreateRecoveryCheckpointAsync(execution.Id, execution.WorkflowType, execution.WorkflowKey, "before-export", new { step = 1 });

        var resumed = await service.ResumeFromCheckpointAsync(checkpoint.Id);

        resumed.Status.Should().Be("Resumed");
        resumed.RecoveryStateJson.Should().Contain("\"step\":1");
        (await fixture.Db.WorkflowRecoveryCheckpoints.SingleAsync(x => x.Id == checkpoint.Id)).Status.Should().Be("Restored");
    }

    [Fact]
    public async Task ReplayWorkflow_GiuThuTuDeterministic()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateService();
        var execution = await service.BeginWorkflowAsync("import", "IMP-101");
        var firstStep = await service.BeginStepAsync(execution.Id, "parse");
        await service.RecordWorkflowExecutionMetricAsync(execution.Id, execution.WorkflowType, execution.WorkflowKey, "rows", 10, "import", durationMs: 25);
        await service.CompleteStepAsync(firstStep.Id);

        var replay = await service.ReplayWorkflowAsync(execution.Id);

        replay.WorkflowExecutionId.Should().Be(execution.Id);
        replay.Steps.Should().ContainSingle(x => x.StepName == "parse");
        replay.Metrics.Should().ContainSingle(x => x.MetricName == "rows");
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }

        private SqliteFixture(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source={Path.Combine(Path.GetTempPath(), $"workflow-tests-{Guid.NewGuid():N}.db")}");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new SqliteFixture(connection, db);
        }

        public WorkflowResilienceService CreateService()
            => new(Db, new AuditCorrelationService(), null, null, new TestCurrentUserService(), new EnterpriseSearchService(Db, new TestCurrentUserService()));

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; } = new()
        {
            Id = Guid.NewGuid(),
            UserName = "workflow-test",
            DisplayName = "Workflow Test"
        };

        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }
}
