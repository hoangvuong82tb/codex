using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class PassthroughIntegrationEventMapper : IIntegrationEventMapper
{
    public IntegrationEventEnvelope Map(IFrontOfficeDomainEvent domainEvent) => new(domainEvent.EventName, 1, domainEvent.Payload, "FrontOffice", "foundation", 0, domainEvent.EventName);
}

public sealed class CanonicalOutboxMessageFactory(IClock clock) : IOutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    public FrontOfficeOutboxMessage Create(IFrontOfficeDomainEvent domainEvent, IntegrationEventEnvelope integrationEvent, FrontOfficeExecutionContext context)
    {
        var payload = JsonSerializer.Serialize(integrationEvent.Payload, SerializerOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return FrontOfficeOutboxMessage.Create(MessageId.From(domainEvent.EventId.Value), integrationEvent.EventName, integrationEvent.SchemaVersion, domainEvent.OccurredAtUtc, payload, hash, integrationEvent.AggregateType, integrationEvent.AggregateId, integrationEvent.AggregateVersion, integrationEvent.PartitionKey, clock.UtcNow, context.Property.PropertyId, context.BusinessDate.BusinessDate, context.Correlation.CorrelationId, context.Correlation.CausationId, integrationEvent.Sensitivity);
    }
}

public sealed class NoOpOutboxPublisher : IOutboxPublisher { public Task PublishAsync(FrontOfficeOutboxMessage message, CancellationToken cancellationToken) => Task.CompletedTask; }
