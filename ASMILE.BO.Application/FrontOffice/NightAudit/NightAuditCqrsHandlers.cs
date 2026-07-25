using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.NightAudit;

public sealed class BeginNightAuditCommandHandler(NightAuditApplicationService s) : ICommandHandler<BeginNightAuditCommand, NightAuditDto>
{ public Task<Result<NightAuditDto>> HandleAsync(BeginNightAuditCommand c, CancellationToken ct) => s.BeginAsync(c, ct); }
public sealed class ExecuteNightAuditStepCommandHandler(NightAuditApplicationService s) : ICommandHandler<ExecuteNightAuditStepCommand, NightAuditDto>
{ public Task<Result<NightAuditDto>> HandleAsync(ExecuteNightAuditStepCommand c, CancellationToken ct) => s.ExecuteStepAsync(c, ct); }
public sealed class ResumeNightAuditCommandHandler(NightAuditApplicationService s) : ICommandHandler<ResumeNightAuditCommand, NightAuditDto>
{ public Task<Result<NightAuditDto>> HandleAsync(ResumeNightAuditCommand c, CancellationToken ct) => s.ResumeAsync(c, ct); }
public sealed class FailNightAuditCommandHandler(NightAuditApplicationService s) : ICommandHandler<FailNightAuditCommand, NightAuditDto>
{ public Task<Result<NightAuditDto>> HandleAsync(FailNightAuditCommand c, CancellationToken ct) => s.FailAsync(c, ct); }
public sealed class CancelNightAuditCommandHandler(NightAuditApplicationService s) : ICommandHandler<CancelNightAuditCommand, NightAuditDto>
{ public Task<Result<NightAuditDto>> HandleAsync(CancelNightAuditCommand c, CancellationToken ct) => s.CancelAsync(c, ct); }

public sealed class GetCurrentNightAuditQueryHandler(INightAuditRepository r, IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a) : IQueryHandler<GetCurrentNightAuditQuery, NightAuditDto?>
{ public Task<Result<NightAuditDto?>> HandleAsync(GetCurrentNightAuditQuery q, CancellationToken ct) => Task.FromResult(Result<NightAuditDto?>.Success(null)); } // Placeholder — resolves at infrastructure
public sealed class GetCurrentBusinessDateQueryHandler(INightAuditRepository r, IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a) : IQueryHandler<GetCurrentBusinessDateQuery, BusinessDateDto?>
{ public async Task<Result<BusinessDateDto?>> HandleAsync(GetCurrentBusinessDateQuery q, CancellationToken ct) => a.HasPermission("FrontOffice.BusinessDate.View") ? Result<BusinessDateDto?>.Success(await r.GetCurrentBusinessDateAsync(q.PropertyId, ct)) : Result<BusinessDateDto?>.Failure(Error.Authorization("", "")); }
public sealed class GetRevenueSnapshotQueryHandler(INightAuditRepository r, IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a) : IQueryHandler<GetRevenueSnapshotQuery, RevenueSnapshotDto?>
{ public Task<Result<RevenueSnapshotDto?>> HandleAsync(GetRevenueSnapshotQuery q, CancellationToken ct) => Task.FromResult(Result<RevenueSnapshotDto?>.Success(null)); }
