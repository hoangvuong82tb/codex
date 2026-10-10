using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Integration;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class BackgroundSchedulerSqlServerIntegrationTests
{
    [Fact]
    public async Task Concurrent_scheduler_instances_queue_a_due_schedule_once()
    {
        var databaseName = $"ASMILE_BO_SCHEDULER_QUEUE_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        var definitionId = Guid.NewGuid();

        await using var setupDb = new AppDbContext(options);
        try
        {
            await setupDb.Database.EnsureDeletedAsync();
            await setupDb.Database.MigrateAsync();
            setupDb.BackgroundJobDefinitions.Add(new BackgroundJobDefinition
            {
                Id = definitionId,
                Code = "SCHEDULER_QUEUE_RACE_TEST",
                Name = "Scheduler queue race test",
                Description = "Integration-test schedule for cross-instance queue locking.",
                IsEnabled = true,
                AllowConcurrentExecution = false,
                RetryCount = 0,
                TimeoutSeconds = 60,
                Schedules =
                [
                    new BackgroundJobSchedule
                    {
                        JobDefinitionId = definitionId,
                        IntervalMinutes = 60,
                        NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1),
                        IsPaused = false
                    }
                ]
            });
            await setupDb.SaveChangesAsync();

            await using var firstDb = new AppDbContext(options);
            await using var secondDb = new AppDbContext(options);
            await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using var firstScheduler = new BackgroundSchedulerService(
                scopeFactory,
                firstDb,
                NullLogger<BackgroundSchedulerService>.Instance);
            using var secondScheduler = new BackgroundSchedulerService(
                scopeFactory,
                secondDb,
                NullLogger<BackgroundSchedulerService>.Instance);

            var now = DateTime.UtcNow;
            var queueResults = await Task.WhenAll(
                firstScheduler.QueueDueAsync(now),
                secondScheduler.QueueDueAsync(now));

            queueResults.Sum().Should().Be(1);
            var executions = await setupDb.BackgroundJobExecutions.AsNoTracking()
                .Where(x => x.JobDefinitionId == definitionId && x.TriggerSource == "scheduler")
                .ToListAsync();
            executions.Should().ContainSingle(x => x.Status == "Queued");

            var schedule = await setupDb.BackgroundJobSchedules.AsNoTracking()
                .SingleAsync(x => x.JobDefinitionId == definitionId);
            schedule.NextRunAtUtc.Should().BeAfter(now);
        }
        finally
        {
            await setupDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task Concurrent_scheduler_instances_claim_a_queued_execution_once()
    {
        var databaseName = $"ASMILE_BO_SCHEDULER_CLAIM_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        var definitionId = Guid.NewGuid();

        await using var setupDb = new AppDbContext(options);
        try
        {
            await setupDb.Database.EnsureDeletedAsync();
            await setupDb.Database.MigrateAsync();
            var definition = new BackgroundJobDefinition
            {
                Id = definitionId,
                Code = "ARCHIVE_CLEANUP",
                Name = "Scheduler claim race test",
                Description = "Integration-test execution claim for concurrent scheduler instances.",
                IsEnabled = true,
                AllowConcurrentExecution = false,
                RetryCount = 0,
                TimeoutSeconds = 60
            };
            setupDb.BackgroundJobDefinitions.Add(definition);
            setupDb.BackgroundJobExecutions.Add(new BackgroundJobExecution
            {
                Id = Guid.NewGuid(),
                JobDefinitionId = definitionId,
                JobType = definition.Code,
                TriggerSource = "scheduler",
                RequestedBy = "integration-test",
                Status = "Queued",
                QueuedAtUtc = DateTime.UtcNow
            });
            await setupDb.SaveChangesAsync();

            await using var firstDb = new AppDbContext(options);
            await using var secondDb = new AppDbContext(options);
            await using var serviceProvider = new ServiceCollection()
                .AddScoped(_ => new AppDbContext(options))
                .BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using var firstScheduler = new BackgroundSchedulerService(
                scopeFactory,
                firstDb,
                NullLogger<BackgroundSchedulerService>.Instance);
            using var secondScheduler = new BackgroundSchedulerService(
                scopeFactory,
                secondDb,
                NullLogger<BackgroundSchedulerService>.Instance);

            var claimResults = await Task.WhenAll(
                firstScheduler.ExecuteNextQueuedAsync(),
                secondScheduler.ExecuteNextQueuedAsync());

            claimResults.Count(x => x).Should().Be(1);
            var execution = await setupDb.BackgroundJobExecutions.AsNoTracking().SingleAsync();
            execution.Status.Should().Be("Completed");
        }
        finally
        {
            await setupDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }
}
