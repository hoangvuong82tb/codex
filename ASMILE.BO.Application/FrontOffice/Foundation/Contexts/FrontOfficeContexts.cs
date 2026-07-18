using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Application.FrontOffice.Foundation.Contexts;

public sealed record ActorContext(ActorId? ActorId, IReadOnlySet<string> Roles, string? SessionId, string? WorkstationId, bool IsSystemActor);
public sealed record PropertyContext(PropertyId PropertyId, string TimeZoneId, string? CurrencyCode);
public sealed record BusinessDateContext(PropertyId PropertyId, DateOnly BusinessDate, string Source);
public sealed record CorrelationContext(CorrelationId CorrelationId, EventId? CausationId, CommandId? CommandId, string? TraceId);
public sealed record FrontOfficeExecutionContext(ActorContext Actor, PropertyContext Property, BusinessDateContext BusinessDate, CorrelationContext Correlation);

public interface IFrontOfficeExecutionContextAccessor
{
    FrontOfficeExecutionContext? Current { get; }
    IDisposable BeginScope(FrontOfficeExecutionContext context);
}

public interface IClock { DateTime UtcNow { get; } }
