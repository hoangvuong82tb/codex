using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;

public sealed class ProcessTimeoutDispatcher(
    IProcessRepository repository,
    IServiceScopeFactory scopeFactory,
    IClock clock,
    IProcessFaultInjector faultInjector,
    IProcessTimeoutWorkExecutor workExecutor,
    ProcessWorkerFailureClassifier failureClassifier,
    IProcessWorkerMetrics metrics,
    ProcessWorkerOptions options,
    ILogger<ProcessTimeoutDispatcher> logger)
{
    public async Task<int> DispatchOnceAsync(string workerId, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var claimed = await repository.ClaimDueTimeoutsAsync(workerId, now, options.LeaseDuration, options.BatchSize, cancellationToken);
        metrics.TimeoutClaimed(workerId, claimed.Count);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterTimeoutClaim, cancellationToken);
        var completed = 0;
        foreach (var timeout in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedAtUtc = clock.UtcNow;
            await using var heartbeat = await ProcessLeaseHeartbeat.StartAsync(scopeFactory, timeout, workerId, clock, options, metrics, logger, cancellationToken);
            if (heartbeat.LeaseLost) continue;
            try
            {
                await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeSignalReserve, heartbeat.Token);
                await workExecutor.ExecuteAsync(timeout.ProcessId, timeout.TimeoutId, timeout.TimeoutType, heartbeat.Token);
                heartbeat.Token.ThrowIfCancellationRequested();
                await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeTimeoutCompletion, heartbeat.Token);
                var finalization = await repository.FinalizeTimeoutAsync(timeout, workerId, clock.UtcNow, heartbeat.Token);
                if (finalization == ProcessTimeoutFinalizeResult.LeaseLost)
                {
                    heartbeat.MarkLeaseLost();
                    continue;
                }
                if (finalization == ProcessTimeoutFinalizeResult.Completed)
                {
                    completed++;
                    metrics.TimeoutCompleted(workerId);
                }
                await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterTimeoutCompletion, heartbeat.Token);
                logger.LogInformation("Process timeout completed. WorkerId={WorkerId} TimeoutId={TimeoutId} LeaseOwner={LeaseOwner} LeaseExpiry={LeaseExpiry} RetryCount={RetryCount} DurationMs={DurationMs} Result={Result} SignalId={SignalId} ProcessId={ProcessId}", workerId, timeout.TimeoutId, workerId, heartbeat.LeaseExpiryUtc, timeout.AttemptCount, (clock.UtcNow - startedAtUtc).TotalMilliseconds, finalization, timeout.TimeoutId, timeout.ProcessId);
            }
            catch (OperationCanceledException) when (heartbeat.LeaseLost)
            {
                logger.LogWarning("Process timeout lease lost; work stopped without commit. WorkerId={WorkerId} TimeoutId={TimeoutId} LeaseOwner={LeaseOwner} LeaseExpiry={LeaseExpiry} ProcessId={ProcessId}", workerId, timeout.TimeoutId, workerId, heartbeat.LeaseExpiryUtc, timeout.ProcessId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (heartbeat.LeaseLost) continue;
                var (kind, errorCode) = failureClassifier.Classify(exception);
                var delay = RetryDelay(timeout.AttemptCount);
                var result = await repository.RecordTimeoutFailureAsync(timeout, workerId, kind, errorCode, options.MaxAttempts, clock.UtcNow, delay, cancellationToken);
                if (result == ProcessTimeoutFailureResult.LeaseLost) { heartbeat.MarkLeaseLost(); continue; }
                if (result == ProcessTimeoutFailureResult.RetryScheduled) metrics.RetryScheduled(workerId);
                if (result == ProcessTimeoutFailureResult.DeadLettered)
                {
                    metrics.RetryExhausted(workerId); metrics.DeadLetter(workerId); metrics.ManualActionCreated(workerId);
                }
                logger.LogWarning(exception, "Process timeout failed. WorkerId={WorkerId} TimeoutId={TimeoutId} LeaseOwner={LeaseOwner} LeaseExpiry={LeaseExpiry} RetryCount={RetryCount} DurationMs={DurationMs} Result={Result} ErrorCode={ErrorCode} SignalId={SignalId} ProcessId={ProcessId}", workerId, timeout.TimeoutId, workerId, heartbeat.LeaseExpiryUtc, timeout.AttemptCount, (clock.UtcNow - startedAtUtc).TotalMilliseconds, result, errorCode, timeout.TimeoutId, timeout.ProcessId);
            }
        }
        return completed;
    }

    private TimeSpan RetryDelay(int attemptCount) => TimeSpan.FromTicks(options.RetryBaseDelay.Ticks * Math.Min(8, 1 << Math.Min(3, attemptCount)));

    private sealed class ProcessLeaseHeartbeat : IAsyncDisposable
    {
        private readonly CancellationTokenSource _leaseLost = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private byte[]? _rowVersion;
        private int _lost;
        private ProcessLeaseHeartbeat(IServiceScopeFactory scopeFactory, ProcessTimeout timeout, string workerId, IClock clock, ProcessWorkerOptions options, IProcessWorkerMetrics metrics, ILogger logger, CancellationToken outerToken)
        {
            Timeout = timeout; WorkerId = workerId; Clock = clock; Options = options; Metrics = metrics; Logger = logger;
            _rowVersion = timeout.RowVersion;
            var linked = CancellationTokenSource.CreateLinkedTokenSource(outerToken, _leaseLost.Token, _stop.Token);
            Token = linked.Token;
            _loop = LoopAsync(scopeFactory, linked);
        }
        private ProcessTimeout Timeout { get; }
        private string WorkerId { get; }
        private IClock Clock { get; }
        private ProcessWorkerOptions Options { get; }
        private IProcessWorkerMetrics Metrics { get; }
        private ILogger Logger { get; }
        public CancellationToken Token { get; }
        public bool LeaseLost => Volatile.Read(ref _lost) == 1;
        public DateTime? LeaseExpiryUtc { get; private set; }
        public static async Task<ProcessLeaseHeartbeat> StartAsync(IServiceScopeFactory scopeFactory, ProcessTimeout timeout, string workerId, IClock clock, ProcessWorkerOptions options, IProcessWorkerMetrics metrics, ILogger logger, CancellationToken cancellationToken)
        {
            var heartbeat = new ProcessLeaseHeartbeat(scopeFactory, timeout, workerId, clock, options, metrics, logger, cancellationToken);
            await heartbeat.RenewAsync(scopeFactory, cancellationToken);
            return heartbeat;
        }
        public void MarkLeaseLost()
        {
            if (Interlocked.Exchange(ref _lost, 1) == 0) { Metrics.LeaseLost(WorkerId); _leaseLost.Cancel(); }
        }
        private async Task LoopAsync(IServiceScopeFactory scopeFactory, CancellationTokenSource linked)
        {
            try
            {
                using var timer = new PeriodicTimer(Options.HeartbeatInterval);
                while (await timer.WaitForNextTickAsync(linked.Token)) await RenewAsync(scopeFactory, linked.Token);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
        }
        private async Task RenewAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var renewal = await scope.ServiceProvider.GetRequiredService<IProcessRepository>().RenewTimeoutLeaseAsync(Timeout.TimeoutId, WorkerId, Clock.UtcNow, Options.LeaseDuration, _rowVersion, cancellationToken);
            if (!renewal.Renewed)
            {
                Logger.LogWarning("Process timeout lease renewal rejected. WorkerId={WorkerId} TimeoutId={TimeoutId} ProcessId={ProcessId}", WorkerId, Timeout.TimeoutId, Timeout.ProcessId);
                MarkLeaseLost();
                return;
            }
            _rowVersion = renewal.RowVersion;
            LeaseExpiryUtc = Clock.UtcNow.Add(Options.LeaseDuration);
            Metrics.LeaseRenewed(WorkerId);
        }
        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            try { await _loop; } catch (OperationCanceledException) { }
            _stop.Dispose(); _leaseLost.Dispose();
        }
    }
}

public sealed class ProcessTimeoutHostedService(IServiceScopeFactory scopeFactory, IProcessWorkerMetrics metrics, ProcessWorkerOptions options, ILogger<ProcessTimeoutHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerId = $"process-timeout-{Environment.MachineName}-{Guid.NewGuid():N}";
        metrics.WorkerStarted(workerId);
        try
        {
            using var timer = new PeriodicTimer(options.PollInterval);
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ProcessTimeoutDispatcher>().DispatchOnceAsync(workerId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Process timeout worker iteration failed. WorkerId={WorkerId}", workerId); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        finally { metrics.WorkerStopped(workerId); }
    }
}
