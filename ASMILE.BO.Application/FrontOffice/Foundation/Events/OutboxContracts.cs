using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;

namespace ASMILE.BO.Application.FrontOffice.Foundation.Events;

public sealed record IntegrationEventEnvelope(string EventName, int SchemaVersion, object Payload, string AggregateType, string AggregateId, long AggregateVersion, string PartitionKey, string Sensitivity = "Internal");
public interface IIntegrationEventMapper { IntegrationEventEnvelope Map(IFrontOfficeDomainEvent domainEvent); }
public interface IOutboxMessageFactory { FrontOfficeOutboxMessage Create(IFrontOfficeDomainEvent domainEvent, IntegrationEventEnvelope integrationEvent, FrontOfficeExecutionContext context); }
public interface IRetryClassifier { bool IsRetryable(Exception exception); }
