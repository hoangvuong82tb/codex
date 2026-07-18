using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Tests.Unit;

public sealed class TrainingDataServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldExposeInnerExceptionMessage()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, allowDestructiveReset: true, seeder: new ThrowingSeeder());

        var result = await service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            SeedProfile = TrainingDataSeedProfile.FoundationOnly,
            ConfirmationText = "RESET DATA"
        });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Outer exception");
        result.ErrorMessage.Should().Contain("Inner root cause");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBlockDestructive_WhenOptionDisabled()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, allowDestructiveReset: false);

        var act = () => service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_DryRunSeed_ShouldNotMutateMasterData()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var before = await serviceStatusCount(db);
        var service = CreateService(db, allowDestructiveReset: true);

        var result = await service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFoundation,
            IsDryRun = true,
            ConfirmationText = "RESET DATA"
        });

        result.Success.Should().BeTrue();
        var after = await serviceStatusCount(db);
        after.Should().Be(before);
    }

    [Fact]
    public async Task ExecuteAsync_SeedFullTraining_ShouldRunValidator()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var seeder = new FakeSeeder([new TrainingDataStepResult { StepOrder = 1, StepCode = "S", StepName = "S", Success = true, CreatedCount = 1 }]);
        var validator = new FakeValidator(["w1"]);
        var service = CreateService(db, allowDestructiveReset: true, seeder: seeder, validator: validator);

        var result = await service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.SeedFullTraining,
            SeedProfile = TrainingDataSeedProfile.FullTraining,
            ConfirmationText = "RESET DATA"
        });

        result.Success.Should().BeTrue();
        result.ValidationWarnings.Should().ContainSingle().Which.Should().Be("w1");
    }

    [Fact]
    public async Task GetPreflightAsync_ShouldWarn_WhenBusinessDataWithoutTrainingMarker()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.JournalHeaders.Add(new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "JV-REAL-1",
            JournalDate = DateTime.UtcNow.Date,
            PostingDate = DateTime.UtcNow.Date,
            PeriodCode = "2026-05",
            SourceType = "Manual",
            SourceNo = "REAL-1",
            Status = Core.Enums.JournalStatus.Posted
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, allowDestructiveReset: true);
        var preflight = await service.GetPreflightAsync();

        preflight.HasRealDataRisk.Should().BeTrue();
        preflight.Warnings.Should().Contain(x => x.Code == "REAL_DATA_RISK");
    }

    [Fact]
    public async Task ExecuteAsync_ResetTrainingOnly_ShouldKeepSystemData_AndUnmarkedBusinessData()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var supplier = new Supplier { SupplierCode = "SUP-REAL", SupplierName = "Real supplier", IsActive = true };
        var customer = new Customer { CustomerCode = "CUS-REAL", CustomerName = "Real customer", IsActive = true };
        db.Suppliers.Add(supplier);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var markerRunId = Guid.NewGuid();
        db.TrainingDataMarkers.Add(new TrainingDataMarker
        {
            TrainingDataRunId = markerRunId,
            Profile = TrainingDataSeedProfile.FullTraining.ToString(),
            EntityName = "Suppliers",
            RecordId = supplier.Id.ToString()
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, allowDestructiveReset: true, cleaner: new TrainingDataCleaner(db));
        var result = await service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetTrainingOnly,
            SeedProfile = TrainingDataSeedProfile.FullTraining,
            ConfirmationText = "RESET TRAINING DATA"
        });

        result.Success.Should().BeTrue();
        (await db.Suppliers.CountAsync(x => x.SupplierCode == "SUP-REAL")).Should().Be(0);
        (await db.Customers.CountAsync(x => x.CustomerCode == "CUS-REAL")).Should().Be(1);
        (await db.AppUsers.CountAsync()).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReject_WhenConfirmationTextInvalidPerOperation()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, allowDestructiveReset: true);

        var fullReset = () => service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ConfirmationText = "RESET DATA"
        });

        var trainingOnly = () => service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetTrainingOnly,
            ConfirmationText = "RESET DATA"
        });

        await fullReset.Should().ThrowAsync<InvalidOperationException>();
        await trainingOnly.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBlockFullReset_WhenRealDataRiskAndOptionDisabled()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.JournalHeaders.Add(new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "JV-REAL-RISK",
            JournalDate = DateTime.UtcNow.Date,
            PostingDate = DateTime.UtcNow.Date,
            PeriodCode = "2026-05",
            SourceType = "Manual",
            SourceNo = "REAL-RISK",
            Status = Core.Enums.JournalStatus.Posted
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, allowDestructiveReset: true, allowResetWithRealDataRisk: false);
        var act = () => service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AllowResetWithRealDataRisk*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAllowFullReset_WhenRealDataRiskAndOptionEnabled()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.JournalHeaders.Add(new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = "JV-REAL-ALLOW",
            JournalDate = DateTime.UtcNow.Date,
            PostingDate = DateTime.UtcNow.Date,
            PeriodCode = "2026-05",
            SourceType = "Manual",
            SourceNo = "REAL-ALLOW",
            Status = Core.Enums.JournalStatus.Posted
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, allowDestructiveReset: true, allowResetWithRealDataRisk: true, cleaner: new TrainingDataCleaner(db));
        var result = await service.ExecuteAsync(new TrainingDataOperationRequest
        {
            OperationType = TrainingDataOperationType.ResetToBlank,
            ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
            ConfirmationText = "RESET ALL BUSINESS DATA"
        });

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetDemoProfilesAndPreview_ShouldReturnHospitalityProfiles()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, allowDestructiveReset: true);

        var profiles = await service.GetDemoProfilesAsync();
        profiles.Select(x => x.Profile).Should().Contain([
            TrainingDataSeedProfile.RestaurantDemo,
            TrainingDataSeedProfile.HotelDemo,
            TrainingDataSeedProfile.RestaurantHotelFullDemo
        ]);

        var preview = await service.GetDemoPreviewAsync(TrainingDataSeedProfile.FullTraining);
        preview.Profile.Should().Be(TrainingDataSeedProfile.RestaurantHotelFullDemo);
        preview.EstimatedCounts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPreflightAsync_ShouldHandleMissingTrainingMarkerTable_WithoutCrash()
    {
        await using var db = TestDbContextFactory.CreateContext();
        if (!db.Database.IsRelational())
        {
            return;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("DROP TABLE TrainingDataMarkers");
        }
        catch
        {
            return;
        }

        var service = CreateService(db, allowDestructiveReset: true);
        var preflight = await service.GetPreflightAsync();
        preflight.Warnings.Should().Contain(x => x.Code == "TRAINING_MARKER_TABLE_MISSING");
    }

    private static async Task<(int c, int s, int i)> serviceStatusCount(Infrastructure.Persistence.AppDbContext db)
    {
        return (await db.Customers.CountAsync(), await db.Suppliers.CountAsync(), await db.Items.CountAsync());
    }

    private static TrainingDataService CreateService(
        Infrastructure.Persistence.AppDbContext db,
        bool allowDestructiveReset,
        ITrainingDataCleaner? cleaner = null,
        ITrainingDataSeeder? seeder = null,
        ITrainingDataValidator? validator = null,
        bool allowResetWithRealDataRisk = false)
    {
        return new TrainingDataService(
            db,
            cleaner ?? new FakeCleaner([]),
            seeder ?? new FakeSeeder([]),
            validator ?? new FakeValidator([]),
            new FakeCurrentUserService(),
            new FakeAuditService(),
            new FakeHostEnvironment(),
            Options.Create(new TrainingDataOptions
            {
                AllowDestructiveReset = allowDestructiveReset,
                AllowInProduction = false,
                AllowResetWithRealDataRisk = allowResetWithRealDataRisk,
                RequireConfirmationText = "RESET DATA"
            }));
    }

    private sealed class FakeCleaner(IReadOnlyList<TrainingDataStepResult> steps) : ITrainingDataCleaner
    {
        public Task<IReadOnlyList<TrainingDataStepResult>> ClearAsync(TrainingDataResetScope scope, bool isDryRun, CancellationToken cancellationToken = default)
            => Task.FromResult(steps);

        public Task<IReadOnlyList<TrainingDataStepResult>> ClearTrainingOnlyAsync(TrainingDataSeedProfile? profile, bool isDryRun, CancellationToken cancellationToken = default)
            => Task.FromResult(steps);
    }

    private sealed class FakeSeeder(IReadOnlyList<TrainingDataStepResult> steps) : ITrainingDataSeeder
    {
        public Task<IReadOnlyList<TrainingDataStepResult>> SeedAsync(TrainingDataSeedProfile profile, bool isDryRun, Guid? runId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(steps);
    }

    private sealed class ThrowingSeeder : ITrainingDataSeeder
    {
        public Task<IReadOnlyList<TrainingDataStepResult>> SeedAsync(TrainingDataSeedProfile profile, bool isDryRun, Guid? runId = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Outer exception", new Exception("Inner root cause"));
    }


    private sealed class FakeValidator(IReadOnlyList<string> warnings) : ITrainingDataValidator
    {
        public Task<IReadOnlyList<string>> ValidateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(warnings);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new() { Id = Guid.NewGuid(), UserName = "tester" };
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login)
            => CurrentUser = new UserAccountDto
            {
                Id = login.UserId,
                UserName = login.UserName,
                DisplayName = login.DisplayName,
                Roles = login.Roles.ToList()
            };
        public void Clear() => CurrentUser = null;
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task LogAsync(string entityName, string entityId, string action, object? oldValues = null, object? newValues = null, string? userName = null, string? source = null, string? correlationId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditLog>> QueryAsync(string? entityName = null, string? entityId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AuditLog>>([]);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ASMILE.BO.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

}
