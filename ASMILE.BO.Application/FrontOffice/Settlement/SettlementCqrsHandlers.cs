using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Settlement;

public sealed class SettleFolioCommandHandler(SettlementApplicationService s) : ICommandHandler<SettleFolioCommand, SettlementResultDto>
{ public Task<Result<SettlementResultDto>> HandleAsync(SettleFolioCommand c, CancellationToken ct) => s.SettleAsync(c, ct); }
public sealed class CloseFolioCommandHandler(SettlementApplicationService s) : ICommandHandler<CloseFolioCommand, HotelFolioDto>
{ public Task<Result<HotelFolioDto>> HandleAsync(CloseFolioCommand c, CancellationToken ct) => s.CloseFolioAsync(c, ct); }
public sealed class GenerateInvoiceCommandHandler(SettlementApplicationService s) : ICommandHandler<GenerateInvoiceCommand, HotelInvoiceDto>
{ public Task<Result<HotelInvoiceDto>> HandleAsync(GenerateInvoiceCommand c, CancellationToken ct) => s.GenerateInvoiceAsync(c, ct); }
public sealed class PrintInvoiceCommandHandler(SettlementApplicationService s) : ICommandHandler<PrintInvoiceCommand, HotelInvoiceDto>
{ public Task<Result<HotelInvoiceDto>> HandleAsync(PrintInvoiceCommand c, CancellationToken ct) => s.PrintAsync(c, ct); }

public sealed class GetInvoiceQueryHandler(IHotelInvoiceRepository r, IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a) : IQueryHandler<GetInvoiceQuery, HotelInvoiceDto>
{ public async Task<Result<HotelInvoiceDto>> HandleAsync(GetInvoiceQuery q, CancellationToken ct) => !S.CanView(cx, a, out var p, out var e) ? Result<HotelInvoiceDto>.Failure(e!) : await r.GetDtoAsync(p, q.HotelInvoiceId, ct) is { } d ? Result<HotelInvoiceDto>.Success(d) : Result<HotelInvoiceDto>.Failure(Error.Business("INVOICE_NOT_FOUND", "")); }
public sealed class GetInvoiceByFolioQueryHandler(IHotelInvoiceRepository r, IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a) : IQueryHandler<GetInvoiceByFolioQuery, HotelInvoiceDto>
{ public async Task<Result<HotelInvoiceDto>> HandleAsync(GetInvoiceByFolioQuery q, CancellationToken ct) => !S.CanView(cx, a, out var p, out var e) ? Result<HotelInvoiceDto>.Failure(e!) : await r.GetByFolioAsync(p, q.HotelFolioId, ct) is { } d ? Result<HotelInvoiceDto>.Success(d) : Result<HotelInvoiceDto>.Failure(Error.Business("INVOICE_NOT_FOUND", "")); }

file static class S
{
    public static bool CanView(IFrontOfficeExecutionContextAccessor cx, IAuthorizationService a, out Guid pid, out Error? e)
    { pid = Guid.Empty; if (!a.HasPermission("FrontOffice.Settlement.View")) { e = Error.Authorization("SETTLEMENT_PERMISSION_DENIED", ""); return false; } if (cx.Current is not { } c) { e = Error.Authorization("SETTLEMENT_PERMISSION_DENIED", ""); return false; } pid = c.Property.PropertyId.Value; e = null; return true; }
}
