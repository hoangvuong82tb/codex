using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Folio;

public sealed class CreateFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<CreateFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(CreateFolioCommand c, CancellationToken ct) => s.CreateAsync(c, ct); }
public sealed class AddFolioRoomChargeCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioRoomChargeCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioRoomChargeCommand c, CancellationToken ct) => s.AddRoomChargeAsync(c, ct); }
public sealed class AddFolioRestaurantChargeCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioRestaurantChargeCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioRestaurantChargeCommand c, CancellationToken ct) => s.AddRestaurantChargeAsync(c, ct); }
public sealed class AddFolioTaxCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioTaxCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioTaxCommand c, CancellationToken ct) => s.AddTaxAsync(c, ct); }
public sealed class AddFolioServiceChargeCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioServiceChargeCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioServiceChargeCommand c, CancellationToken ct) => s.AddServiceChargeAsync(c, ct); }
public sealed class AddFolioDiscountCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioDiscountCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioDiscountCommand c, CancellationToken ct) => s.AddDiscountAsync(c, ct); }
public sealed class AddFolioPaymentCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioPaymentCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioPaymentCommand c, CancellationToken ct) => s.AddPaymentAsync(c, ct); }
public sealed class AddFolioDepositCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioDepositCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioDepositCommand c, CancellationToken ct) => s.AddDepositAsync(c, ct); }
public sealed class AddFolioAdjustmentCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioAdjustmentCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioAdjustmentCommand c, CancellationToken ct) => s.AddAdjustmentAsync(c, ct); }
public sealed class AddFolioCorrectionCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioCorrectionCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioCorrectionCommand c, CancellationToken ct) => s.AddCorrectionAsync(c, ct); }
public sealed class AddFolioManualChargeCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioManualChargeCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioManualChargeCommand c, CancellationToken ct) => s.AddManualChargeAsync(c, ct); }
public sealed class AddFolioManualCreditCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioManualCreditCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioManualCreditCommand c, CancellationToken ct) => s.AddManualCreditAsync(c, ct); }
public sealed class AddFolioRefundCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioRefundCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioRefundCommand c, CancellationToken ct) => s.AddRefundAsync(c, ct); }
public sealed class AddFolioTransferCommandHandler(HotelFolioApplicationService s) : ICommandHandler<AddFolioTransferCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(AddFolioTransferCommand c, CancellationToken ct) => s.TransferAsync(c, ct); }
public sealed class SplitFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<SplitFolioCommand, FolioTransferResult> { public Task<Result<FolioTransferResult>> HandleAsync(SplitFolioCommand c, CancellationToken ct) => s.SplitAsync(c, ct); }
public sealed class MergeFoliosCommandHandler(HotelFolioApplicationService s) : ICommandHandler<MergeFoliosCommand, FolioMergeResult> { public Task<Result<FolioMergeResult>> HandleAsync(MergeFoliosCommand c, CancellationToken ct) => s.MergeAsync(c, ct); }
public sealed class ReverseFolioEntryCommandHandler(HotelFolioApplicationService s) : ICommandHandler<ReverseFolioEntryCommand, HotelFolioEntryDto> { public Task<Result<HotelFolioEntryDto>> HandleAsync(ReverseFolioEntryCommand c, CancellationToken ct) => s.ReverseEntryAsync(c, ct); }
public sealed class SuspendFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<SuspendFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(SuspendFolioCommand c, CancellationToken ct) => s.SuspendAsync(c, ct); }
public sealed class ResumeFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<ResumeFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(ResumeFolioCommand c, CancellationToken ct) => s.ResumeAsync(c, ct); }
public sealed class CloseFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<CloseFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(CloseFolioCommand c, CancellationToken ct) => s.CloseAsync(c, ct); }
public sealed class SettleFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<SettleFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(SettleFolioCommand c, CancellationToken ct) => s.SettleAsync(c, ct); }
public sealed class ReopenFolioCommandHandler(HotelFolioApplicationService s) : ICommandHandler<ReopenFolioCommand, HotelFolioDto> { public Task<Result<HotelFolioDto>> HandleAsync(ReopenFolioCommand c, CancellationToken ct) => s.ReopenAsync(c, ct); }

public sealed class GetFolioQueryHandler(IHotelFolioRepository r, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService a) : IQueryHandler<GetFolioQuery, HotelFolioDto> { public async Task<Result<HotelFolioDto>> HandleAsync(GetFolioQuery q, CancellationToken ct) => !FolioAuth.CanView(ctx, a, out var p, out var e) ? Result<HotelFolioDto>.Failure(e!) : await r.GetDtoAsync(p, q.HotelFolioId, ct) is { } d ? Result<HotelFolioDto>.Success(d) : Result<HotelFolioDto>.Failure(Error.Business("FOLIO_NOT_FOUND", "Not found.")); }
public sealed class GetFolioByStayQueryHandler(IHotelFolioRepository r, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService a) : IQueryHandler<GetFolioByStayQuery, HotelFolioDto> { public async Task<Result<HotelFolioDto>> HandleAsync(GetFolioByStayQuery q, CancellationToken ct) => !FolioAuth.CanView(ctx, a, out var p, out var e) ? Result<HotelFolioDto>.Failure(e!) : await r.GetByStayAsync(p, q.HotelStayId, ct) is { } d ? Result<HotelFolioDto>.Success(d) : Result<HotelFolioDto>.Failure(Error.Business("FOLIO_NOT_FOUND", "Not found.")); }
public sealed class GetFolioLedgerQueryHandler(IHotelFolioRepository r, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService a) : IQueryHandler<GetFolioLedgerQuery, FolioLedgerDto> { public async Task<Result<FolioLedgerDto>> HandleAsync(GetFolioLedgerQuery q, CancellationToken ct) => !FolioAuth.CanView(ctx, a, out var p, out var e) ? Result<FolioLedgerDto>.Failure(e!) : await r.GetLedgerAsync(p, q.HotelFolioId, ct) is { } d ? Result<FolioLedgerDto>.Success(d) : Result<FolioLedgerDto>.Failure(Error.Business("FOLIO_NOT_FOUND", "Not found.")); }
public sealed class SearchFoliosQueryHandler(IHotelFolioRepository r, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService a) : IQueryHandler<SearchFoliosQuery, PageResult<FolioLedgerDto>> { public async Task<Result<PageResult<FolioLedgerDto>>> HandleAsync(SearchFoliosQuery q, CancellationToken ct) => !FolioAuth.CanView(ctx, a, out var _, out var e) ? Result<PageResult<FolioLedgerDto>>.Failure(e!) : Result<PageResult<FolioLedgerDto>>.Success(await r.SearchAsync(q.Filter, q.Page, ct)); }

file static class FolioAuth
{
    public static bool CanView(IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService a, out Guid p, out Error? e)
    {
        p = Guid.Empty;
        if (!a.HasPermission("FrontOffice.Folio.View")) { e = Error.Authorization("FOLIO_PERMISSION_DENIED", "Folio view permission required."); return false; }
        if (ctx.Current is not { } c) { e = Error.Authorization("FOLIO_PERMISSION_DENIED", "Front Office context required."); return false; }
        p = c.Property.PropertyId.Value; e = null; return true;
    }
}
