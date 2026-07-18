using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class FrontOfficeAuditWriter : IFrontOfficeAuditWriter
{
    private readonly List<(FrontOfficeAuditIntent Intent, FrontOfficeExecutionContext Context)> _staged = [];
    public void Stage(FrontOfficeAuditIntent intent, FrontOfficeExecutionContext context) => _staged.Add((intent, context));
    internal IReadOnlyList<AuditLog> Drain(DateTime utcNow)
    {
        var records = _staged.Select(item => new AuditLog
        {
            Action = item.Intent.Action,
            Detail = JsonSerializer.Serialize(new { item.Intent.Result, item.Intent.ReasonCode, item.Intent.ApprovalReference, item.Intent.FailureCode, item.Intent.Sensitivity, item.Intent.Metadata }),
            Username = item.Context.Actor.ActorId?.ToString() ?? "system",
            EntityName = item.Intent.AggregateType,
            EntityId = item.Intent.AggregateId,
            CorrelationId = item.Context.Correlation.CorrelationId.ToString(),
            CreatedAt = utcNow
        }).ToList();
        _staged.Clear();
        return records;
    }
}
