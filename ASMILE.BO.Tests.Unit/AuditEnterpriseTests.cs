using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class AuditEnterpriseTests
{
    [Fact]
    public void CorrelationScopes_AreNestedSafe()
    {
        var correlation = new AuditCorrelationService();

        using var outer = correlation.BeginScope("corr-outer");
        using var inner = correlation.BeginScope();

        correlation.CurrentCorrelationId.Should().Be("corr-outer");
        inner.Dispose();
        correlation.CurrentCorrelationId.Should().Be("corr-outer");
    }

    [Fact]
    public async Task AuditService_WritesSnapshotHashDiffAndReplayTrail()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();

        using var scope = correlation.BeginScope("corr-snapshot");
        await audit.LogAsync(
            "JournalHeader",
            "J-1",
            "Update",
            new { Amount = 100m, Status = "Draft" },
            new { Amount = 120m, Status = "Posted" },
            "tester",
            correlationId: correlation.CurrentCorrelationId);

        var snapshot = await fixture.Db.AuditEntitySnapshots.AsNoTracking().SingleAsync();
        snapshot.SnapshotHash.Should().HaveLength(64);
        snapshot.BeforeJson.Should().Contain("100");
        snapshot.DiffJson.Should().Contain("Amount");

        var summary = await replay.GetReplaySummaryAsync("corr-snapshot");
        summary.EntryCount.Should().Be(1);
        summary.Entries.Should().ContainSingle(x => x.SequenceNo == 1 && x.Route == "JournalHeader");
    }

    [Fact]
    public async Task ReplayExport_StreamsJsonAndPersistsHistory()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();

        using var scope = correlation.BeginScope("corr-export");
        await audit.LogAsync("ReportExecution", "R-1", "Run", null, new { ReportCode = "TRIAL_BALANCE" }, "tester", correlationId: correlation.CurrentCorrelationId);
        await audit.LogAsync("ReportExecution", "R-1", "Complete", null, new { ReportCode = "TRIAL_BALANCE", Status = "Completed" }, "tester", correlationId: correlation.CurrentCorrelationId);

        var exportPath = Path.Combine(Path.GetTempPath(), $"audit-export-{Guid.NewGuid():N}.json");
        var result = await replay.ExportAsync(new AuditExportRequestDto
        {
            ExportType = "json",
            CorrelationId = "corr-export",
            FilePath = exportPath
        });

        File.Exists(result.FilePath).Should().BeTrue();
        result.RecordCount.Should().Be(2);
        (await replay.GetExportHistoryAsync()).Should().ContainSingle(x => x.FilePath == result.FilePath);
    }

    [Fact]
    public async Task ReplayExport_RequiresLogsExportPermissionBeforeCreatingFile()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var replay = fixture.CreateReplayService(new TestCurrentUserService([]));
        var exportPath = Path.Combine(Path.GetTempPath(), $"audit-export-denied-{Guid.NewGuid():N}.json");

        var act = () => replay.ExportAsync(new AuditExportRequestDto
        {
            ExportType = "json",
            FilePath = exportPath
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Permission denied: Logs.Export");
        File.Exists(exportPath).Should().BeFalse();
        (await replay.GetExportHistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplayExport_AppliesAuditCenterFiltersToExportedRows()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();

        using (correlation.BeginScope("corr-export-filter-match"))
        {
            await audit.LogAsync("Invoice", "INV-1", "Exported", null, new { Status = "Done" }, "alice", correlationId: correlation.CurrentCorrelationId);
            await audit.LogAsync("Invoice", "INV-2", "Viewed", null, new { Status = "Read" }, "alice", correlationId: correlation.CurrentCorrelationId);
        }

        using (correlation.BeginScope("corr-export-filter-user"))
        {
            await audit.LogAsync("Invoice", "INV-3", "Exported", null, new { Status = "Done" }, "bob", correlationId: correlation.CurrentCorrelationId);
        }

        var exportPath = Path.Combine(Path.GetTempPath(), $"audit-export-filter-{Guid.NewGuid():N}.json");
        var result = await replay.ExportAsync(new AuditExportRequestDto
        {
            ExportType = "json",
            ModuleFilter = "Invoice",
            ActionFilter = "Exported",
            UserFilter = "alice",
            FromDate = DateTime.UtcNow.AddMinutes(-5),
            ToDate = DateTime.UtcNow.AddMinutes(5),
            FilePath = exportPath
        });

        result.RecordCount.Should().Be(1);
        var json = await File.ReadAllTextAsync(result.FilePath);
        json.Should().Contain("corr-export-filter-match");
        json.Should().Contain("Exported");
        json.Should().NotContain("Viewed");
        json.Should().NotContain("corr-export-filter-user");
        (await replay.GetExportHistoryAsync()).Single(x => x.Id == result.ExportHistoryId)
            .FilterJson.Should().Contain("userFilter");
    }

    [Fact]
    public async Task ReplayExport_AlwaysRestrictsRowsToPersistedAuditLogScope()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();

        using (correlation.BeginScope("corr-export-scoped"))
        {
            await audit.LogAsync("Invoice", "INV-1", "Viewed", null, new { Status = "Read" }, "alice", correlationId: correlation.CurrentCorrelationId);
        }

        fixture.Db.AuditReplayEntries.Add(new AuditReplayEntry
        {
            CorrelationId = "corr-orphan-replay",
            SequenceNo = 1,
            Route = "Unscoped",
            Module = "Unscoped",
            Action = "Viewed",
            EntityType = "Invoice",
            EntityId = "INV-ORPHAN",
            TimestampUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var exportPath = Path.Combine(Path.GetTempPath(), $"audit-export-scope-{Guid.NewGuid():N}.json");
        var result = await replay.ExportAsync(new AuditExportRequestDto
        {
            ExportType = "json",
            FilePath = exportPath
        });

        result.RecordCount.Should().Be(1);
        (await File.ReadAllTextAsync(result.FilePath)).Should().Contain("corr-export-scoped").And.NotContain("corr-orphan-replay");
    }

    [Fact]
    public async Task JournalPosting_CreatesCorrelationReplayAndSnapshot()
    {
        await using var fixture = await SqliteFixture.CreateAsync(seedAccountingCore: true);
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();
        var journalService = fixture.CreateJournalService(audit, correlation);

        using var scope = correlation.BeginScope("corr-journal");
        var draft = await journalService.CreateDraftAsync(new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "JV-001",
            JournalDate = new DateTime(2026, 5, 10),
            PostingDate = new DateTime(2026, 5, 10),
            SourceType = "ManualJournal",
            SourceNo = "JV-001",
            Description = "Journal test",
            PeriodCode = "2026-05",
            Status = JournalStatus.Draft,
            CreatedBy = "tester",
            CreatedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = "1111", AccountName = "Cash", DebitAmount = 100m, CurrencyCode = "VND", ExchangeRate = 1m },
                new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = "5111", AccountName = "Revenue", CreditAmount = 100m, CurrencyCode = "VND", ExchangeRate = 1m }
            ]
        });

        await journalService.PostDraftAsync(draft.Id, "tester");

        var summary = await replay.GetReplaySummaryAsync("corr-journal");
        summary.EntryCount.Should().BeGreaterThan(0);
        fixture.Db.AuditEntitySnapshots.Should().NotBeEmpty();
    }

    [Fact]
    public async Task NavigationOpen_RecordsReplayTrail()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();
        using var scope = correlation.BeginScope("corr-nav");
        await audit.LogAsync("Navigation", "custom-audit-route", "Open", null, new { Route = "custom-audit-route", Module = "navigation" }, "tester", correlationId: correlation.CurrentCorrelationId);

        var summary = await replay.GetReplaySummaryAsync("corr-nav");
        summary.RouteTrail.Should().Contain("custom-audit-route");
    }

    [Fact]
    public async Task ImportPreview_RecordsReplayMetadata()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();
        var importEngine = fixture.CreateImportEngine(audit, correlation);
        var filePath = fixture.CreateBankStatementWorkbook();

        using var scope = correlation.BeginScope("corr-import");
        var result = await importEngine.PreviewAsync(new ImportPreviewRequest
        {
            TemplateCode = "BANK_STATEMENT",
            FilePath = filePath,
            CreatedBy = "tester",
            HeaderRowNumber = 1
        });

        result.ImportSessionId.Should().NotBe(Guid.Empty);
        (await replay.GetReplayEntriesAsync("corr-import")).Should().Contain(x => x.EntityType == "ImportSession" && x.Action.Contains("Preview", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BackgroundSchedulerExecution_PropagatesCorrelation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var scheduler = fixture.CreateSchedulerService(audit, correlation);
        var definition = await fixture.AddJobAsync("AUDIT_JOB");

        using var scope = correlation.BeginScope("corr-scheduler");
        var executionId = await scheduler.RunNowAsync(definition.Code, "tester");

        (await fixture.Replay.GetReplayEntriesAsync("corr-scheduler")).Should().Contain(x => x.EntityType == "BackgroundJobExecution" && x.EntityId == executionId.ToString());
    }

    [Fact]
    public async Task AuditSessionService_StartHeartbeatAndComplete_FinalizesLifecycle()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var sessionService = fixture.CreateSessionService(fixture.CreateCorrelationService());

        var started = await sessionService.StartSessionAsync("AuditCenter", "corr-session", "desktop-1", "device-1", "Windows");
        await sessionService.HeartbeatAsync("corr-session");
        await sessionService.CompleteSessionAsync("corr-session", "Logout");

        var session = await fixture.Db.AuditSessions.AsNoTracking().SingleAsync(x => x.Id == started.Id);
        session.Status.Should().Be(AuditSessionStatus.Completed);
        session.CompletedAtUtc.Should().NotBeNull();
        session.LastActivityAtUtc.Should().BeOnOrAfter(started.StartedAtUtc);
        session.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        session.DeviceId.Should().Be("device-1");
        session.Platform.Should().Be("Windows");
    }

    [Fact]
    public async Task AuditSessionService_AbandonExpiredSessions_MarksAbandonedAndExpired()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var now = DateTime.UtcNow;
        fixture.Db.AuditSessions.AddRange(
            new AuditSession { CorrelationId = "corr-abandoned", Source = "test", Status = AuditSessionStatus.Active, StartedAtUtc = now.AddHours(-1), LastActivityAtUtc = now.AddMinutes(-15) },
            new AuditSession { CorrelationId = "corr-expired", Source = "test", Status = AuditSessionStatus.Active, StartedAtUtc = now.AddHours(-2), LastActivityAtUtc = now.AddMinutes(-40) });
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateSessionService(fixture.CreateCorrelationService());

        var updated = await service.AbandonExpiredSessionsAsync(now, TimeSpan.FromMinutes(10));

        updated.Should().Be(2);
        (await fixture.Db.AuditSessions.AsNoTracking().SingleAsync(x => x.CorrelationId == "corr-abandoned")).Status.Should().Be(AuditSessionStatus.Abandoned);
        (await fixture.Db.AuditSessions.AsNoTracking().SingleAsync(x => x.CorrelationId == "corr-expired")).Status.Should().Be(AuditSessionStatus.Expired);
    }

    [Fact]
    public async Task AuditSessionService_GetActiveSessions_ExcludesCompleted()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateSessionService(fixture.CreateCorrelationService());
        await service.StartSessionAsync("Active", "corr-active");
        await service.StartSessionAsync("Done", "corr-done");
        await service.CompleteSessionAsync("corr-done");

        var active = await service.GetActiveSessionsAsync();

        active.Should().ContainSingle(x => x.CorrelationId == "corr-active");
        active.Should().NotContain(x => x.CorrelationId == "corr-done");
    }

    [Fact]
    public async Task AuditSessionService_Start_ObservesCancellation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateSessionService(fixture.CreateCorrelationService());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => service.StartSessionAsync("Cancelled", "corr-cancelled", cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AuditSnapshotCompression_CompressesAndReplayDecompressesLargePayload()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        var replay = fixture.CreateReplayService();
        var largeText = new string('A', 16_000);

        await audit.LogAsync("Voucher", "V-1", "Update", new { Notes = "before" }, new { Notes = largeText }, "tester", correlationId: "corr-compress");

        var stored = await fixture.Db.AuditEntitySnapshots.AsNoTracking().SingleAsync();
        stored.CompressionType.Should().Be("GZip");
        stored.CompressedPayload.Should().NotBeNull();
        stored.AfterJson.Should().BeNull();
        var snapshot = (await replay.GetSnapshotsAsync(stored.AuditLogId!.Value)).Single();
        snapshot.AfterJson.Should().Contain(largeText);
        snapshot.HashVerified.Should().BeTrue();
        snapshot.HashMismatchDetected.Should().BeFalse();
    }

    [Fact]
    public async Task AuditSnapshotCompression_DetectsHashMismatch()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var audit = fixture.CreateAuditService(fixture.CreateCorrelationService());
        var replay = fixture.CreateReplayService();
        await audit.LogAsync("Voucher", "V-2", "Update", new { Amount = 1m }, new { Amount = 2m }, "tester", correlationId: "corr-hash");
        var stored = await fixture.Db.AuditEntitySnapshots.SingleAsync();
        stored.SnapshotHash = new string('0', 64);
        await fixture.Db.SaveChangesAsync();

        var snapshot = (await replay.GetSnapshotsAsync(stored.AuditLogId!.Value)).Single();

        snapshot.HashVerified.Should().BeFalse();
        snapshot.HashMismatchDetected.Should().BeTrue();
    }

    [Fact]
    public async Task AuditReplay_ReconstructState_ReturnsTimelineFramesAndSnapshots()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var audit = fixture.CreateAuditService(fixture.CreateCorrelationService());
        var replay = fixture.CreateReplayService();
        await audit.LogAsync("ReportExecution", "R-STATE", "Run", null, new { Status = "Started" }, "tester", correlationId: "corr-state");
        await audit.LogAsync("ReportExecution", "R-STATE", "Complete", new { Status = "Started" }, new { Status = "Completed" }, "tester", correlationId: "corr-state");

        var result = await replay.ReconstructStateAsync("corr-state");

        result.Timeline.Should().HaveCount(2);
        result.StateFrames.Should().NotBeEmpty();
        result.Snapshots.Should().OnlyContain(x => x.HashVerified);
    }

    [Fact]
    public async Task AuditReplay_GetReplayBySessionJournalAndSource_UsesProjectedQueries()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var audit = fixture.CreateAuditService(fixture.CreateCorrelationService());
        var replay = fixture.CreateReplayService();
        var journalId = Guid.NewGuid();
        await audit.LogAsync("JournalHeader", journalId.ToString(), "Post", null, new { SourceType = "ManualJournal", SourceId = "JV-100" }, "tester", correlationId: "corr-journal-query");
        await audit.LogAsync("Voucher", "SRC-100", "Commit", null, new { SourceType = "Voucher" }, "tester", correlationId: "corr-source-query");
        var sessionId = await fixture.Db.AuditSessions.AsNoTracking().Where(x => x.CorrelationId == "corr-journal-query").Select(x => x.Id).SingleAsync();

        (await replay.GetReplayBySessionAsync(sessionId)).Should().ContainSingle(x => x.CorrelationId == "corr-journal-query");
        (await replay.GetReplayByJournalAsync(journalId)).Should().ContainSingle(x => x.EntityId == journalId.ToString());
        (await replay.GetReplayBySourceAsync("Voucher", "SRC-100")).Should().ContainSingle(x => x.CorrelationId == "corr-source-query");
    }

    [Fact]
    public async Task AuditExport_NdjsonPersistsIntegrityMetadata()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var audit = fixture.CreateAuditService(fixture.CreateCorrelationService());
        var replay = fixture.CreateReplayService();
        await audit.LogAsync("ReportExecution", "R-NDJSON", "Run", null, new { ReportCode = "TB" }, "tester", correlationId: "corr-ndjson");
        var exportPath = Path.Combine(Path.GetTempPath(), $"audit-export-{Guid.NewGuid():N}.ndjson");

        var result = await replay.ExportAsync(new AuditExportRequestDto { ExportType = "ndjson", CorrelationId = "corr-ndjson", FilePath = exportPath });

        result.ExportType.Should().Be("NDJSON");
        result.RecordCount.Should().Be(1);
        result.ExportHash.Should().HaveLength(64);
        File.ReadAllLines(result.FilePath).Should().ContainSingle();
        var history = (await replay.GetExportHistoryAsync()).Single();
        history.RowCount.Should().Be(1);
        history.IntegrityManifestJson.Should().Contain("SHA256");
        history.CorrelationCoverageJson.Should().Contain("corr-ndjson");
    }

    [Fact]
    public async Task AuditCorrelation_StoresParentChildChain()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var correlation = fixture.CreateCorrelationService();
        var audit = fixture.CreateAuditService(correlation);
        using var outer = correlation.BeginScope("corr-parent");
        using var inner = correlation.BeginScope("corr-child");

        await audit.LogAsync("Navigation", "child-route", "Open", null, new { Route = "child-route" }, "tester", correlationId: correlation.CurrentCorrelationId);

        var replay = await fixture.Db.AuditReplayEntries.AsNoTracking().SingleAsync();
        replay.CorrelationId.Should().Be("corr-child");
        replay.ParentCorrelationId.Should().Be("corr-parent");
        (await fixture.Db.AuditCorrelations.AsNoTracking().SingleAsync()).ParentCorrelationId.Should().Be("corr-parent");
    }

    [Fact]
    public async Task AuditSnapshotQueryFilter_IsolatesCompanyScopedSnapshots()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var branch = Guid.NewGuid();
        await using var fixture = await SqliteFixture.CreateAsync(new TestCompanyContext(companyA, branch));
        fixture.Db.AuditEntitySnapshots.AddRange(
            new AuditEntitySnapshot { EntityType = "Voucher", EntityId = "A", CompanyId = companyA, BranchId = branch, SnapshotHash = new string('A', 64) },
            new AuditEntitySnapshot { EntityType = "Voucher", EntityId = "B", CompanyId = companyB, BranchId = branch, SnapshotHash = new string('B', 64) });
        await fixture.Db.SaveChangesAsync();

        var visible = await fixture.Db.AuditEntitySnapshots.AsNoTracking().ToListAsync();

        visible.Should().ContainSingle(x => x.EntityId == "A");
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly SqliteConnection _rootConnection;
        public AppDbContext Db { get; }
        public AuditReplayService Replay { get; }

        private SqliteFixture(string databasePath, SqliteConnection rootConnection, AppDbContext db)
        {
            _databasePath = databasePath;
            _rootConnection = rootConnection;
            Db = db;
            Replay = new AuditReplayService(Db);
        }

        public static Task<SqliteFixture> CreateAsync(bool seedAccountingCore = false)
            => CreateAsync(null, seedAccountingCore);

        public static async Task<SqliteFixture> CreateAsync(ICurrentCompanyContext? companyContext, bool seedAccountingCore = false)
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"audit-enterprise-tests-{Guid.NewGuid():N}.db");
            var rootConnection = new SqliteConnection($"Data Source={databasePath}");
            await rootConnection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(rootConnection).Options;
            var db = companyContext is null ? new AppDbContext(options) : new AppDbContext(options, companyContext);
            await db.Database.EnsureCreatedAsync();
            if (seedAccountingCore)
            {
                TestDbContextFactory.SeedAccountingCore(db);
            }

            return new SqliteFixture(databasePath, rootConnection, db);
        }

        public AuditCorrelationService CreateCorrelationService() => new();

        public AuditService CreateAuditService(IAuditCorrelationService correlationService)
            => new(Db, correlationService, new TestCurrentUserService());

        public AuditReplayService CreateReplayService(ICurrentUserService? currentUserService = null)
            => new(Db, currentUserService ?? new TestCurrentUserService(["Logs.Export"]));

        public AuditSessionService CreateSessionService(IAuditCorrelationService correlationService)
            => new(Db, correlationService, new TestCurrentUserService());

        public JournalService CreateJournalService(IAuditService auditService, IAuditCorrelationService correlationService)
            => new(Db, TestDbContextFactory.PeriodService(Db), TestDbContextFactory.AuditLogService(Db), null, null, auditService, correlationService);

        public ImportEngine CreateImportEngine(IAuditService auditService, IAuditCorrelationService correlationService)
            => new(Db, [new BankStatementImportTemplateHandler(Db)], TestDbContextFactory.AuditLogService(Db), auditService, correlationService);

        public BackgroundSchedulerService CreateSchedulerService(IAuditService auditService, IAuditCorrelationService correlationService)
            => new(new SqliteScopeFactory(_databasePath), Db, NullLogger<BackgroundSchedulerService>.Instance, auditService, correlationService);

        public async Task<BackgroundJobDefinition> AddJobAsync(string code)
        {
            var definition = new BackgroundJobDefinition
            {
                Code = code,
                Name = code,
                Description = code,
                IsEnabled = true,
                AllowConcurrentExecution = false,
                RetryCount = 1,
                TimeoutSeconds = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            definition.Schedules.Add(new BackgroundJobSchedule
            {
                IntervalMinutes = 5,
                NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1),
                IsPaused = false
            });
            Db.BackgroundJobDefinitions.Add(definition);
            await Db.SaveChangesAsync();
            return definition;
        }

        public string CreateBankStatementWorkbook()
        {
            var path = Path.Combine(Path.GetTempPath(), $"audit-import-{Guid.NewGuid():N}.xlsx");
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("Sheet1");
            sheet.Cell(1, 1).Value = "Date";
            sheet.Cell(1, 2).Value = "ReferenceNo";
            sheet.Cell(1, 3).Value = "Debit";
            sheet.Cell(1, 4).Value = "Credit";
            sheet.Cell(1, 5).Value = "Description";
            sheet.Cell(2, 1).Value = new DateTime(2026, 5, 1);
            sheet.Cell(2, 2).Value = "REF-1";
            sheet.Cell(2, 3).Value = 100m;
            sheet.Cell(2, 4).Value = 0m;
            sheet.Cell(2, 5).Value = "Opening balance";
            workbook.SaveAs(path);
            return path;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _rootConnection.DisposeAsync();
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
            }
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly HashSet<string>? _permissions;

        public TestCurrentUserService(IEnumerable<string>? permissions = null)
        {
            _permissions = permissions is null ? null : new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
            CurrentUser = new UserAccountDto
            {
                Id = Guid.NewGuid(),
                UserName = "tester",
                DisplayName = "Tester",
                Roles = [],
                IsActive = true
            };
            CurrentSessionId = Guid.NewGuid();
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => _permissions is null || _permissions.Contains(permissionCode);
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId, Guid? branchId = null)
        {
            CompanyId = companyId;
            BranchId = branchId;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName => "Test";
        public string? BranchName => "Test";
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            return Task.CompletedTask;
        }
    }

    private sealed class SqliteScopeFactory : IServiceScopeFactory
    {
        private readonly string _databasePath;

        public SqliteScopeFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        public IServiceScope CreateScope()
            => new SqliteScope(_databasePath);
    }

    private sealed class SqliteScope : IServiceScope
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;

        public SqliteScope(string databasePath)
        {
            _connection = new SqliteConnection($"Data Source={databasePath}");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _dbContext = new AppDbContext(options);
            ServiceProvider = new SqliteServiceProvider(_dbContext);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class SqliteServiceProvider : IServiceProvider
    {
        private readonly AppDbContext _dbContext;

        public SqliteServiceProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public object? GetService(Type serviceType)
            => serviceType == typeof(AppDbContext) ? _dbContext : null;
    }
}
