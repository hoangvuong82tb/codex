using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;

public interface IFrontOfficeDomainEvent
{
    EventId EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventName { get; }
    object Payload { get; }
}

public interface IHasFrontOfficeDomainEvents
{
    IReadOnlyCollection<IFrontOfficeDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
