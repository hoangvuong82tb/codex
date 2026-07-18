using ASMILE.BO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BackgroundSchedulerHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundSchedulerHostedService> _logger;

    public BackgroundSchedulerHostedService(IServiceScopeFactory scopeFactory, ILogger<BackgroundSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundSchedulerService>();
            await scheduler.ValidateStartupAsync(stoppingToken);
            var recovered = await scheduler.RecoverStaleExecutionsAsync(DateTime.UtcNow, TimeSpan.FromMinutes(15), stoppingToken);
            if (recovered > 0)
            {
                _logger.LogWarning("Recovered {RecoveredCount} stale background job executions during startup.", recovered);
            }
        }

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundSchedulerService>();

                var queued = await scheduler.QueueDueAsync(DateTime.UtcNow, stoppingToken);
                var executed = 0;
                while (await scheduler.ExecuteNextQueuedAsync(stoppingToken))
                {
                    executed++;
                }

                if (queued > 0 || executed > 0)
                {
                    _logger.LogInformation("Scheduler tick queued {QueuedCount} jobs and executed {ExecutedCount} jobs.", queued, executed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background scheduler tick failed.");
            }
        }
    }
}

