using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;

namespace ASMILE.BO.Application.FrontOffice.Foundation.Audit;

public sealed record FrontOfficeAuditIntent(string Action, string AggregateType, string AggregateId, string Result, string? ReasonCode = null, string? ApprovalReference = null, string? FailureCode = null, string Sensitivity = "Internal", IReadOnlyDictionary<string, string>? Metadata = null);
public interface IFrontOfficeAuditWriter { void Stage(FrontOfficeAuditIntent intent, FrontOfficeExecutionContext context); }
