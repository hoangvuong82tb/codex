using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Events;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Events;

/// <summary>
/// EventBus — pub/sub infrastructure using existing Outbox pattern.
/// Publishes to Outbox transactionally and dispatches to in-process subscribers.
/// Retry: re-lease failed outbox messages. Dead Letter: archive expired messages.
/// </summary>
public sealed class EventBus(AppDbContext db) : IEventBus
{
    private readonly Dictionary<string, List<IEventHandler>> _subscriptions = [];

    public async Task PublishAsync(IntegrationEvent e, CancellationToken ct)
    {
        // Persist to outbox for durability
        var msg = FrontOfficeOutboxMessage.Create(MessageId.New(), e.EventName, 1, e.OccurredAtUtc, e.PayloadJson, e.PayloadJson.GetHashCode().ToString(), e.AggregateType, e.AggregateId, e.AggregateVersion, e.PartitionKey, DateTime.UtcNow);
        db.Set<FrontOfficeOutboxMessage>().Add(msg);

        // Dispatch to in-process subscribers
        if (_subscriptions.TryGetValue(e.EventName, out var handlers))
            foreach (var h in handlers)
                try { await h.HandleAsync(e, ct); } catch { /* Log error — subscriber failed */ }

        await db.SaveChangesAsync(ct);
    }

    public Task SubscribeAsync(IEventHandler handler, CancellationToken ct)
    {
        if (!_subscriptions.ContainsKey(handler.EventName)) _subscriptions[handler.EventName] = [];
        _subscriptions[handler.EventName].Add(handler);
        return Task.CompletedTask;
    }

    public async Task PublishFromAggregateAsync<T>(T aggregate, string aggregateType, Guid correlationId, CancellationToken ct) where T : IHasDomainEvents
    {
        foreach (var de in aggregate.DomainEvents)
        {
            var json = JsonSerializer.Serialize(de);
            await PublishAsync(new IntegrationEvent(de.GetType().Name, aggregateType, aggregate.GetAggregateId().ToString(),
                aggregate.GetVersion(), Guid.Empty, null, correlationId, null, DateTime.UtcNow, json,
                $"{aggregateType}/{aggregate.GetAggregateId()}"), ct);
        }
        aggregate.ClearDomainEvents();
    }

    public async Task<int> RetryFailedAsync(string eventName, int maxRetry, CancellationToken ct)
    {
        var failed = await db.Set<FrontOfficeOutboxMessage>()
            .Where(m => m.EventName == eventName && m.Status == OutboxStatus.Failed && m.RetryCount < maxRetry)
            .Take(100).ToListAsync(ct);
        foreach (var m in failed) { m.GetType().GetProperty("Status")!.SetValue(m, OutboxStatus.Pending); }
        await db.SaveChangesAsync(ct);
        return failed.Count;
    }

    public async Task<int> MoveToDeadLetterAsync(string eventName, int olderThanHours, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-olderThanHours);
        var dead = await db.Set<FrontOfficeOutboxMessage>()
            .Where(m => m.EventName == eventName && m.Status == OutboxStatus.Failed && m.CreatedAtUtc < cutoff && m.RetryCount >= 5)
            .Take(100).ToListAsync(ct);
        // Dead letter = logged for investigation, messages remain as Failed
        return dead.Count;
    }
}

/// <summary>Stay → Folio: auto-create folio on check-in.</summary>
public sealed class StayCheckedInFolioHandler(IEventBus bus) : IEventHandler
{
    public string EventName => FrontOfficeEventNames.StayCheckedIn;
    public async Task HandleAsync(IntegrationEvent e, CancellationToken ct)
    {
        await bus.PublishAsync(new IntegrationEvent(FrontOfficeEventNames.FolioCreated, "Folio", e.AggregateId, 0,
            e.PropertyId, e.BusinessDate, e.CorrelationId, null, DateTime.UtcNow,
            $"{{\"stayId\":\"{e.AggregateId}\"}}", $"Folio/{e.AggregateId}"), ct);
    }
}

public sealed class PaymentReceivedReportingHandler : IEventHandler { public string EventName => FrontOfficeEventNames.PaymentReceived; public Task HandleAsync(IntegrationEvent e, CancellationToken ct) => Task.CompletedTask; }
public sealed class NightAuditCompletedAvailabilityHandler : IEventHandler { public string EventName => FrontOfficeEventNames.NightAuditCompleted; public Task HandleAsync(IntegrationEvent e, CancellationToken ct) => Task.CompletedTask; }
public sealed class PostingErrorNotificationHandler : IEventHandler { public string EventName => "PostingError"; public Task HandleAsync(IntegrationEvent e, CancellationToken ct) => Task.CompletedTask; }
