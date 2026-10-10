using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Publishes durable notifications from persisted operational conditions even
/// when the desktop notification center is not open.
/// </summary>
public sealed class NotificationConditionHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationConditionHostedService> _logger;

    public NotificationConditionHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationConditionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification condition scan failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ScanOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationTaskService>();
        var eInvoiceService = scope.ServiceProvider.GetRequiredService<IEInvoiceService>();

        var taskCount = 0;
        var now = DateTime.UtcNow;
        var tasks = await notificationService.GetTasksAsync(null, cancellationToken);
        foreach (var task in tasks
            .Where(x => x.DueAt is not null
                && x.DueAt <= now
                && x.Status is ("Open" or "Assigned" or "Accepted" or "InProgress" or "OnHold"))
            .Take(100))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dueAt = task.DueAt!.Value;
            await notificationService.PublishAsync(new NotificationPublishRequest
            {
                UserId = task.AssignedToUserId,
                NotificationType = "TaskOverdue",
                Severity = task.Priority.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                    ? "Critical"
                    : "Warning",
                Title = "Công việc quá hạn",
                Message = $"Công việc \"{task.Title}\" đã quá hạn từ {dueAt.ToLocalTime():dd/MM/yyyy HH:mm}.",
                SourceType = "TaskInbox",
                SourceId = task.Id,
                TargetRoute = task.TargetRoute ?? "notification-center",
                IdempotencyKey = $"task-overdue:{task.Id:N}:{dueAt.Ticks}"
            }, cancellationToken);
            taskCount++;
        }

        var invoiceFailureCount = 0;
        foreach (var failure in await eInvoiceService.GetFailedIssueLogsAsync(100, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detail = string.IsNullOrWhiteSpace(failure.ErrorMessage)
                ? "Không có thông tin lỗi từ provider."
                : failure.ErrorMessage.Trim();
            await notificationService.PublishAsync(new NotificationPublishRequest
            {
                NotificationType = "EInvoiceIssueFailed",
                Severity = "Critical",
                Title = "Phát hành hóa đơn điện tử thất bại",
                Message = $"Hóa đơn {failure.InvoiceNo} / {failure.ProviderCode} thất bại ở lần thử {failure.AttemptNo}: {detail}",
                SourceType = "InvoiceIssueLog",
                SourceId = failure.LogId,
                TargetRoute = "invoice-management",
                IdempotencyKey = $"e-invoice-issue-failed:{failure.LogId:N}"
            }, cancellationToken);
            invoiceFailureCount++;
        }

        if (taskCount > 0 || invoiceFailureCount > 0)
        {
            _logger.LogInformation(
                "Notification condition scan processed {OverdueTaskCount} overdue tasks and {InvoiceFailureCount} failed e-invoice logs.",
                taskCount,
                invoiceFailureCount);
        }
    }
}
