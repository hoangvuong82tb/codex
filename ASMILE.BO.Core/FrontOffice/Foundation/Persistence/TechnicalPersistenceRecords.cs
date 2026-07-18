using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Foundation.Persistence;

public enum OutboxStatus { Pending, Leased, Published, Failed }
public enum InboxStatus { Reserved, Completed, Failed }
public enum IdempotencyStatus { InProgress, Completed, FailedRetryable, FailedPermanent, Expired }

public sealed class FrontOfficeOutboxMessage
{
    private FrontOfficeOutboxMessage() { }
    public MessageId MessageId { get; private set; }
    public string EventName { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public PropertyId? PropertyId { get; private set; }
    public DateOnly? BusinessDate { get; private set; }
    public CorrelationId? CorrelationId { get; private set; }
    public EventId? CausationId { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public string AggregateId { get; private set; } = string.Empty;
    public long AggregateVersion { get; private set; }
    public string PartitionKey { get; private set; } = string.Empty;
    public string Sensitivity { get; private set; } = "Internal";
    public string Payload { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTime? LeaseExpiresAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static FrontOfficeOutboxMessage Create(MessageId messageId, string eventName, int schemaVersion, DateTime occurredAtUtc, string payload, string payloadHash, string aggregateType, string aggregateId, long aggregateVersion, string partitionKey, DateTime createdAtUtc, PropertyId? propertyId = null, DateOnly? businessDate = null, CorrelationId? correlationId = null, EventId? causationId = null, string sensitivity = "Internal") => new() { MessageId = messageId, EventName = eventName, SchemaVersion = schemaVersion, OccurredAtUtc = occurredAtUtc, Payload = payload, PayloadHash = payloadHash, AggregateType = aggregateType, AggregateId = aggregateId, AggregateVersion = aggregateVersion, PartitionKey = partitionKey, CreatedAtUtc = createdAtUtc, PropertyId = propertyId, BusinessDate = businessDate, CorrelationId = correlationId, CausationId = causationId, Sensitivity = sensitivity };
    public void Lease(string owner, DateTime expiresAtUtc) { if (Status is not (OutboxStatus.Pending or OutboxStatus.Failed)) throw new InvalidOperationException("Only pending or failed messages can be leased."); Status = OutboxStatus.Leased; LeaseOwner = owner; LeaseExpiresAtUtc = expiresAtUtc; }
    public void MarkPublished(DateTime publishedAtUtc) { if (Status != OutboxStatus.Leased) throw new InvalidOperationException("Only leased messages can be published."); Status = OutboxStatus.Published; PublishedAtUtc = publishedAtUtc; LeaseOwner = null; LeaseExpiresAtUtc = null; }
    public void MarkFailed(string errorCode, DateTime? nextRetryAtUtc) { Status = OutboxStatus.Failed; RetryCount++; LastErrorCode = errorCode; NextRetryAtUtc = nextRetryAtUtc; LeaseOwner = null; LeaseExpiresAtUtc = null; }
}

public sealed class FrontOfficeInboxMessage
{
    private FrontOfficeInboxMessage() { }
    public string ConsumerName { get; private set; } = string.Empty;
    public MessageId MessageId { get; private set; }
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public InboxStatus Status { get; private set; } = InboxStatus.Reserved;
    public int RetryCount { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTime? LeaseExpiresAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static FrontOfficeInboxMessage Reserve(string consumerName, MessageId messageId, string payloadHash, DateTime receivedAtUtc) => new() { ConsumerName = consumerName, MessageId = messageId, PayloadHash = payloadHash, ReceivedAtUtc = receivedAtUtc };
    public void Complete(DateTime completedAtUtc) { Status = InboxStatus.Completed; CompletedAtUtc = completedAtUtc; LeaseOwner = null; LeaseExpiresAtUtc = null; }
    public void Fail(string errorCode) { Status = InboxStatus.Failed; RetryCount++; LastErrorCode = errorCode; }
}

public sealed class FrontOfficeIdempotencyRecord
{
    private FrontOfficeIdempotencyRecord() { }
    public Guid Id { get; private set; }
    public string Namespace { get; private set; } = string.Empty;
    public IdempotencyKey Key { get; private set; }
    public CommandId CommandId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public IdempotencyStatus Status { get; private set; } = IdempotencyStatus.InProgress;
    public string? ResultType { get; private set; }
    public string? ResultReference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string? FailureClassification { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static FrontOfficeIdempotencyRecord Reserve(string @namespace, IdempotencyKey key, CommandId commandId, string requestHash, DateTime createdAtUtc, DateTime expiresAtUtc) => new() { Id = Uuid7.NewGuid(), Namespace = @namespace, Key = key, CommandId = commandId, RequestHash = requestHash, CreatedAtUtc = createdAtUtc, ExpiresAtUtc = expiresAtUtc };
    public void Complete(string resultType, string resultReference, DateTime completedAtUtc) { Status = IdempotencyStatus.Completed; ResultType = resultType; ResultReference = resultReference; CompletedAtUtc = completedAtUtc; }
    public void Fail(string classification, bool retryable) { Status = retryable ? IdempotencyStatus.FailedRetryable : IdempotencyStatus.FailedPermanent; FailureClassification = classification; }
    public void Expire() => Status = IdempotencyStatus.Expired;
}
