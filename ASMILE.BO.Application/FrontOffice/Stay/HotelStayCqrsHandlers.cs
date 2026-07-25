using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Stay;

public sealed class PrepareStayFromReservationCommandHandler(HotelStayApplicationService service) : ICommandHandler<PrepareStayFromReservationCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(PrepareStayFromReservationCommand command, CancellationToken ct) => service.PrepareFromReservationAsync(command, ct); }
public sealed class CheckInHotelStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<CheckInHotelStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(CheckInHotelStayCommand command, CancellationToken ct) => service.CheckInAsync(command, ct); }
public sealed class CheckOutHotelStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<CheckOutHotelStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(CheckOutHotelStayCommand command, CancellationToken ct) => service.CheckOutAsync(command, ct); }
public sealed class ConfirmPreArrivalCommandHandler(HotelStayApplicationService service) : ICommandHandler<ConfirmPreArrivalCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(ConfirmPreArrivalCommand c, CancellationToken ct) => service.ConfirmPreArrivalAsync(c, ct); }
public sealed class PreCheckInHotelStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<PreCheckInHotelStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(PreCheckInHotelStayCommand c, CancellationToken ct) => service.PreCheckInAsync(c, ct); }
public sealed class ExtendStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<ExtendStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(ExtendStayCommand c, CancellationToken ct) => service.ExtendStayAsync(c, ct); }
public sealed class MoveRoomCommandHandler(HotelStayApplicationService service) : ICommandHandler<MoveRoomCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(MoveRoomCommand c, CancellationToken ct) => service.MoveRoomAsync(c, ct); }
public sealed class CompleteStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<CompleteStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(CompleteStayCommand c, CancellationToken ct) => service.CompleteAsync(c, ct); }
public sealed class ReopenStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<ReopenStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(ReopenStayCommand c, CancellationToken ct) => service.ReopenAsync(c, ct); }
public sealed class CancelStayCommandHandler(HotelStayApplicationService service) : ICommandHandler<CancelStayCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(CancelStayCommand c, CancellationToken ct) => service.CancelAsync(c, ct); }
public sealed class ToggleDoNotDisturbCommandHandler(HotelStayApplicationService service) : ICommandHandler<ToggleDoNotDisturbCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(ToggleDoNotDisturbCommand c, CancellationToken ct) => service.ToggleDoNotDisturbAsync(c, ct); }
public sealed class RequestWakeUpCallCommandHandler(HotelStayApplicationService service) : ICommandHandler<RequestWakeUpCallCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(RequestWakeUpCallCommand c, CancellationToken ct) => service.RequestWakeUpCallAsync(c, ct); }
public sealed class LogComplaintCommandHandler(HotelStayApplicationService service) : ICommandHandler<LogComplaintCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(LogComplaintCommand c, CancellationToken ct) => service.LogComplaintAsync(c, ct); }
public sealed class AddStayTagCommandHandler(HotelStayApplicationService service) : ICommandHandler<AddStayTagCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(AddStayTagCommand c, CancellationToken ct) => service.AddTagAsync(c, ct); }
public sealed class RemoveStayTagCommandHandler(HotelStayApplicationService service) : ICommandHandler<RemoveStayTagCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(RemoveStayTagCommand c, CancellationToken ct) => service.RemoveTagAsync(c, ct); }
public sealed class MarkVipServiceCommandHandler(HotelStayApplicationService service) : ICommandHandler<MarkVipServiceCommand, HotelStayDto>
{ public Task<Result<HotelStayDto>> HandleAsync(MarkVipServiceCommand c, CancellationToken ct) => service.MarkVipServiceAsync(c, ct); }
public sealed class GetStayTimelineQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetStayTimelineQuery, IReadOnlyList<StayTimelineEntryDto>>
{ public async Task<Result<IReadOnlyList<StayTimelineEntryDto>>> HandleAsync(GetStayTimelineQuery q, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var pid, out var error) ? Result<IReadOnlyList<StayTimelineEntryDto>>.Failure(error!) : Result<IReadOnlyList<StayTimelineEntryDto>>.Success(await repository.GetTimelineAsync(pid, q.HotelStayId, ct)); }

public sealed class GetStayByIdQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetStayByIdQuery, HotelStayDto>
{ public async Task<Result<HotelStayDto>> HandleAsync(GetStayByIdQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<HotelStayDto>.Failure(error!) : await repository.GetDtoAsync(propertyId, query.HotelStayId, ct) is { } dto ? Result<HotelStayDto>.Success(dto) : Result<HotelStayDto>.Failure(Error.Business("STAY_NOT_FOUND", "Stay was not found.")); }

public sealed class GetStayByNumberQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetStayByNumberQuery, HotelStayDto>
{ public async Task<Result<HotelStayDto>> HandleAsync(GetStayByNumberQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<HotelStayDto>.Failure(error!) : await repository.GetByNumberAsync(propertyId, query.StayNumber, ct) is { } dto ? Result<HotelStayDto>.Success(dto) : Result<HotelStayDto>.Failure(Error.Business("STAY_NOT_FOUND", "Stay was not found.")); }

public sealed class SearchStaysQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<SearchStaysQuery, PageResult<HotelStayDto>>
{ public async Task<Result<PageResult<HotelStayDto>>> HandleAsync(SearchStaysQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<PageResult<HotelStayDto>>.Failure(error!) : Result<PageResult<HotelStayDto>>.Success(await repository.SearchAsync(propertyId, query.Keyword, query.Status, query.DateFrom, query.DateTo, query.Page, query.Sort, ct)); }

public sealed class GetCurrentInHouseStaysQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetCurrentInHouseStaysQuery, IReadOnlyList<HotelStayDto>>
{ public async Task<Result<IReadOnlyList<HotelStayDto>>> HandleAsync(GetCurrentInHouseStaysQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<IReadOnlyList<HotelStayDto>>.Failure(error!) : Result<IReadOnlyList<HotelStayDto>>.Success(await repository.GetCurrentInHouseAsync(propertyId, ct)); }

public sealed class GetTodayExpectedArrivalsQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetTodayExpectedArrivalsQuery, IReadOnlyList<HotelStayDto>>
{ public async Task<Result<IReadOnlyList<HotelStayDto>>> HandleAsync(GetTodayExpectedArrivalsQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<IReadOnlyList<HotelStayDto>>.Failure(error!) : context.Current is { } current ? Result<IReadOnlyList<HotelStayDto>>.Success(await repository.GetExpectedArrivalsAsync(propertyId, current.BusinessDate.BusinessDate, ct)) : Result<IReadOnlyList<HotelStayDto>>.Failure(error!); }

public sealed class GetTodayExpectedDeparturesQueryHandler(IHotelStayRepository repository, IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization) : IQueryHandler<GetTodayExpectedDeparturesQuery, IReadOnlyList<HotelStayDto>>
{ public async Task<Result<IReadOnlyList<HotelStayDto>>> HandleAsync(GetTodayExpectedDeparturesQuery query, CancellationToken ct) => !HotelStayQueryAuthorization.CanView(context, authorization, out var propertyId, out var error) ? Result<IReadOnlyList<HotelStayDto>>.Failure(error!) : context.Current is { } current ? Result<IReadOnlyList<HotelStayDto>>.Success(await repository.GetExpectedDeparturesAsync(propertyId, current.BusinessDate.BusinessDate, ct)) : Result<IReadOnlyList<HotelStayDto>>.Failure(error!); }

file static class HotelStayQueryAuthorization
{
public static bool CanView(IFrontOfficeExecutionContextAccessor context, IAuthorizationService authorization, out Guid propertyId, out Error? error)
{
    propertyId = Guid.Empty;
    if (!authorization.HasPermission(HotelStayPermissions.View)) { error = Error.Authorization("STAY_PERMISSION_DENIED", "Stay view permission is required."); return false; }
    if (context.Current is not { } current) { error = Error.Authorization("STAY_PERMISSION_DENIED", "A trusted Front Office property context is required."); return false; }
    propertyId = current.Property.PropertyId.Value;
    error = null;
    return true;
}
}
