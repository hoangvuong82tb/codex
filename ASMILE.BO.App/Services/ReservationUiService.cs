using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Shared.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Services;

public sealed record ReservationEditRequest(
    string GuestName,
    string? Phone,
    string? Email,
    Guid? RoomTypeId,
    Guid? RatePlanId,
    string? SpecialRequests,
    string? InternalNotes,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    int Adults,
    int Children,
    string Source,
    string? MarketSegment = null,
    string? CompanyName = null,
    string? TravelAgentName = null,
    bool? IsVip = null,
    Guid? BookingSourceProfileId = null,
    Guid? BookingChannelProfileId = null,
    Guid? MarketSegmentProfileId = null,
    Guid? CompanyProfileId = null,
    Guid? TravelAgentProfileId = null,
    Guid? CommissionRuleProfileId = null);

public interface IReservationUiService : IReservationHubPermissionReader, IDisposable
{
    void ResetForContextChange();
    new bool HasContext { get; }
    new bool CanView { get; }
    new bool CanCreate { get; }
    new bool CanUpdate { get; }
    new bool CanAssignRoom { get; }
    new bool CanChangeStatus { get; }
    new bool CanCancel { get; }
    Task<Result<PageResult<ReservationDto>>> SearchAsync(string? keyword, ReservationStatus? status, DateOnly? arrivalFrom, DateOnly? arrivalTo, int page, int pageSize, CancellationToken ct, string? source = null);
    Task<Result<ReservationDto>> GetAsync(Guid reservationId, CancellationToken ct);
    Task<Result<PageResult<HotelRoomTypeDto>>> GetActiveRoomTypesAsync(CancellationToken ct);
    Task<Result<PageResult<HotelRoomDto>>> GetAssignableRoomsAsync(Guid roomTypeId, CancellationToken ct);
    Task<Result<RoomConflictResult>> CheckRoomConflictAsync(ReservationDto current, Guid roomId, CancellationToken ct);
    Task<Result<ReservationDto>> CreateAsync(string guestName, string? guestPhone, string? guestEmail, Guid? guestId, DateOnly arrival, DateOnly departure, int adults, int children, Guid? roomTypeId, string source, CancellationToken ct, bool confirmImmediately = false, Guid? ratePlanId = null, Guid? bookingSourceProfileId = null, Guid? bookingChannelProfileId = null, Guid? marketSegmentProfileId = null, Guid? companyProfileId = null, Guid? travelAgentProfileId = null, Guid? commissionRuleProfileId = null);
    Task<Result<ReservationDto>> UpdateAsync(ReservationDto current, ReservationEditRequest request, CancellationToken ct);
    Task<Result<ReservationDto>> ChangeStayAsync(ReservationDto current, DateOnly arrival, DateOnly departure, CancellationToken ct);
    Task<Result<ReservationDto>> ChangeGuestsAsync(ReservationDto current, int adults, int children, CancellationToken ct);
    Task<Result<ReservationDto>> AssignAsync(ReservationDto current, Guid roomId, CancellationToken ct);
    Task<Result<ReservationDto>> UnassignAsync(ReservationDto current, CancellationToken ct);
    Task<Result<ReservationDto>> TentativeAsync(ReservationDto current, CancellationToken ct);
    Task<Result<ReservationDto>> ConfirmAsync(ReservationDto current, CancellationToken ct);
    Task<Result<ReservationDto>> RecordDepositAsync(ReservationDto current, decimal amount, DateOnly dueDate, CancellationToken ct);
    Task<Result<ReservationDto>> CancelAsync(ReservationDto current, string reason, CancellationToken ct);
    Task<Result<ReservationDto>> NoShowAsync(ReservationDto current, CancellationToken ct);
}

public sealed class ReservationUiService : IReservationUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope _scope = null!;
    private IFrontOfficeExecutionContextAccessor context = null!;
    private IAuthorizationService authorization = null!;
    private CreateReservationCommandHandler create = null!;
    private UpdateReservationDetailsCommandHandler update = null!;
    private ChangeReservationStayCommandHandler changeStay = null!;
    private ChangeReservationGuestCountCommandHandler changeGuests = null!;
    private AssignReservationRoomCommandHandler assign = null!;
    private UnassignReservationRoomCommandHandler unassign = null!;
    private MarkReservationTentativeCommandHandler tentative = null!;
    private ConfirmReservationCommandHandler confirm = null!;
    private RecordReservationDepositCommandHandler recordDeposit = null!;
    private CancelReservationCommandHandler cancel = null!;
    private MarkReservationNoShowCommandHandler noShow = null!;
    private GetReservationByIdQueryHandler getById = null!;
    private SearchReservationsQueryHandler search = null!;
    private CheckRoomReservationConflictQueryHandler checkConflict = null!;
    private HotelRoomMasterDataQueryHandlers hotelQueries = null!;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private int _scopeResetRequested;

    public ReservationUiService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        CreateScope();
    }

    public void ResetForContextChange()
    {
        // A context change can be raised while a list/detail query is still awaiting EF.
        // Dispose the scoped DbContext only after that operation has released the gate.
        // Disposing it synchronously here can race the in-flight query and surface either
        // "second operation" or disposed-context errors to the user.
        Volatile.Write(ref _scopeResetRequested, 1);
    }

    private void CreateScope()
    {
        _scope = _scopeFactory.CreateScope();
        var services = _scope.ServiceProvider;
        context = services.GetRequiredService<IFrontOfficeExecutionContextAccessor>();
        authorization = services.GetRequiredService<IAuthorizationService>();
        create = services.GetRequiredService<CreateReservationCommandHandler>();
        update = services.GetRequiredService<UpdateReservationDetailsCommandHandler>();
        changeStay = services.GetRequiredService<ChangeReservationStayCommandHandler>();
        changeGuests = services.GetRequiredService<ChangeReservationGuestCountCommandHandler>();
        assign = services.GetRequiredService<AssignReservationRoomCommandHandler>();
        unassign = services.GetRequiredService<UnassignReservationRoomCommandHandler>();
        tentative = services.GetRequiredService<MarkReservationTentativeCommandHandler>();
        confirm = services.GetRequiredService<ConfirmReservationCommandHandler>();
        recordDeposit = services.GetRequiredService<RecordReservationDepositCommandHandler>();
        cancel = services.GetRequiredService<CancelReservationCommandHandler>();
        noShow = services.GetRequiredService<MarkReservationNoShowCommandHandler>();
        getById = services.GetRequiredService<GetReservationByIdQueryHandler>();
        search = services.GetRequiredService<SearchReservationsQueryHandler>();
        checkConflict = services.GetRequiredService<CheckRoomReservationConflictQueryHandler>();
        hotelQueries = services.GetRequiredService<HotelRoomMasterDataQueryHandlers>();
    }

    public bool HasContext => context.Current is not null;
    public bool CanView => authorization.HasPermission(ReservationPermissions.View);
    public bool CanCreate => authorization.HasPermission(ReservationPermissions.Create);
    public bool CanUpdate => authorization.HasPermission(ReservationPermissions.Update);
    public bool CanAssignRoom => authorization.HasPermission(ReservationPermissions.AssignRoom);
    public bool CanChangeStatus => authorization.HasPermission(ReservationPermissions.ChangeStatus);
    public bool CanCancel => authorization.HasPermission(ReservationPermissions.Cancel);
    public Task<Result<PageResult<ReservationDto>>> SearchAsync(string? keyword, ReservationStatus? status, DateOnly? arrivalFrom, DateOnly? arrivalTo, int page, int pageSize, CancellationToken ct, string? source = null) => ExecuteSerializedAsync(() => RequireContext<PageResult<ReservationDto>>(property => search.HandleAsync(new SearchReservationsQuery(property, new ReservationSearchFilter(keyword, null, status, arrivalFrom, arrivalTo, null, null, null, source, null, null, null), new PageRequest(page, pageSize), "arrival"), ct)), ct);
    public Task<Result<ReservationDto>> GetAsync(Guid reservationId, CancellationToken ct) => ExecuteSerializedAsync(() => RequireContext<ReservationDto>(property => getById.HandleAsync(new GetReservationByIdQuery(property, reservationId), ct)), ct);
    public Task<Result<PageResult<HotelRoomTypeDto>>> GetActiveRoomTypesAsync(CancellationToken ct) => ExecuteSerializedAsync(() => RequireContext<PageResult<HotelRoomTypeDto>>(_ => hotelQueries.SearchTypes(null, true, new PageRequest(1, 200), "name", ct)), ct);
    public Task<Result<PageResult<HotelRoomDto>>> GetAssignableRoomsAsync(Guid roomTypeId, CancellationToken ct) => ExecuteSerializedAsync(() => RequireContext<PageResult<HotelRoomDto>>(_ => hotelQueries.SearchRooms(null, roomTypeId, null, null, null, true, new PageRequest(1, 200), "number", true, ct)), ct);
    public Task<Result<RoomConflictResult>> CheckRoomConflictAsync(ReservationDto current, Guid roomId, CancellationToken ct) => ExecuteSerializedAsync(() => RequireContext<RoomConflictResult>(property => checkConflict.HandleAsync(new CheckRoomReservationConflictQuery(property, roomId, current.ArrivalDate, current.DepartureDate, current.ReservationId), ct)), ct);
    public Task<Result<ReservationDto>> CreateAsync(string guestName, string? guestPhone, string? guestEmail, Guid? guestId, DateOnly arrival, DateOnly departure, int adults, int children, Guid? roomTypeId, string source, CancellationToken ct, bool confirmImmediately = false, Guid? ratePlanId = null, Guid? bookingSourceProfileId = null, Guid? bookingChannelProfileId = null, Guid? marketSegmentProfileId = null, Guid? companyProfileId = null, Guid? travelAgentProfileId = null, Guid? commissionRuleProfileId = null) => ExecuteSerializedAsync(() => RequireContext<ReservationDto>(property => create.HandleAsync(new CreateReservationCommand(property, guestId, guestName, arrival, departure, adults, children, roomTypeId, ratePlanId, source, null, null, null, false, ConfirmImmediately: confirmImmediately, GuestPhone: guestPhone, GuestEmail: guestEmail, BookingSourceProfileId: bookingSourceProfileId, BookingChannelProfileId: bookingChannelProfileId, MarketSegmentProfileId: marketSegmentProfileId, CompanyProfileId: companyProfileId, TravelAgentProfileId: travelAgentProfileId, CommissionRuleProfileId: commissionRuleProfileId), ct)), ct);
    public Task<Result<ReservationDto>> UpdateAsync(ReservationDto current, ReservationEditRequest request, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => update.HandleAsync(new UpdateReservationDetailsCommand(property, current.ReservationId, request.GuestName, request.Phone, request.Email, request.RoomTypeId, request.RatePlanId, request.SpecialRequests, request.InternalNotes, Token(current), request.ArrivalDate, request.DepartureDate, request.Adults, request.Children, request.Source, request.MarketSegment ?? current.MarketSegment, request.CompanyName ?? current.CompanyName, request.TravelAgentName ?? current.TravelAgentName, request.IsVip ?? current.IsVip, request.BookingSourceProfileId ?? current.BookingSourceProfileId, request.BookingChannelProfileId ?? current.BookingChannelProfileId, request.MarketSegmentProfileId ?? current.MarketSegmentProfileId, request.CompanyProfileId ?? current.CompanyProfileId, request.TravelAgentProfileId ?? current.TravelAgentProfileId, request.CommissionRuleProfileId ?? current.CommissionRuleProfileId), ct)), ct);
    public Task<Result<ReservationDto>> ChangeStayAsync(ReservationDto current, DateOnly arrival, DateOnly departure, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => changeStay.HandleAsync(new ChangeReservationStayCommand(property, current.ReservationId, arrival, departure, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> ChangeGuestsAsync(ReservationDto current, int adults, int children, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => changeGuests.HandleAsync(new ChangeReservationGuestCountCommand(property, current.ReservationId, adults, children, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> AssignAsync(ReservationDto current, Guid roomId, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => assign.HandleAsync(new AssignReservationRoomCommand(property, current.ReservationId, roomId, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> UnassignAsync(ReservationDto current, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => unassign.HandleAsync(new UnassignReservationRoomCommand(property, current.ReservationId, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> TentativeAsync(ReservationDto current, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => tentative.HandleAsync(new MarkReservationTentativeCommand(property, current.ReservationId, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> ConfirmAsync(ReservationDto current, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => confirm.HandleAsync(new ConfirmReservationCommand(property, current.ReservationId, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> RecordDepositAsync(ReservationDto current, decimal amount, DateOnly dueDate, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => recordDeposit.HandleAsync(new RecordReservationDepositCommand(property, current.ReservationId, amount, dueDate, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> CancelAsync(ReservationDto current, string reason, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => cancel.HandleAsync(new CancelReservationCommand(property, current.ReservationId, reason, Token(current)), ct)), ct);
    public Task<Result<ReservationDto>> NoShowAsync(ReservationDto current, CancellationToken ct) => ExecuteSerializedAsync(() => Execute(current, property => noShow.HandleAsync(new MarkReservationNoShowCommand(property, current.ReservationId, Token(current)), ct)), ct);
    private async Task<Result<T>> ExecuteSerializedAsync<T>(Func<Task<Result<T>>> operation, CancellationToken ct)
    {
        await _operationGate.WaitAsync(ct);
        try
        {
            if (Interlocked.Exchange(ref _scopeResetRequested, 0) == 1)
            {
                _scope.Dispose();
                CreateScope();
            }

            return await operation();
        }
        finally
        {
            _operationGate.Release();
        }
    }
    private Task<Result<ReservationDto>> Execute(ReservationDto current, Func<Guid, Task<Result<ReservationDto>>> action) => current.RowVersion is not { Length: 8 } ? Task.FromResult(Result<ReservationDto>.Failure(Error.Validation("RESERVATION_ROWVERSION_REQUIRED", "Reservation must be reloaded before it can be changed."))) : RequireContext<ReservationDto>(action);
    private Task<Result<T>> RequireContext<T>(Func<Guid, Task<Result<T>>> action) => context.Current is { } current ? action(current.Property.PropertyId.Value) : Task.FromResult(Result<T>.Failure(Error.Authorization("FRONT_OFFICE_CONTEXT_REQUIRED", "A Front Office property context is required.")));
    private static byte[] Token(ReservationDto dto) => dto.RowVersion!.ToArray();
    public void Dispose()
    {
        _scope.Dispose();
        _operationGate.Dispose();
    }
}

public static class ReservationErrorMessageMapper
{
    public static string ToMessage(Error? error) => error?.Code switch
    {
        "RESERVATION_CONCURRENCY_CONFLICT" => "Dat phong nay vua duoc cap nhat boi nguoi dung khac. Du lieu cua ban chua duoc luu.",
        "RESERVATION_PERMISSION_DENIED" => "Ban khong co quyen thuc hien thao tac nay.",
        "RESERVATION_ROOM_CONFLICT" => "Phong khong con phu hop trong khoang ngay da chon.",
        "RESERVATION_ROOM_NOT_ASSIGNABLE" => "Phong nay hien khong the phan bo.",
        "RESERVATION_ROOM_TYPE_MISMATCH" or "RESERVATION_ASSIGNED_ROOM_TYPE_MISMATCH" => "Phong khong phu hop voi loai phong cua dat phong.",
        "RESERVATION_ROOM_TYPE_NOT_FOUND" or "RESERVATION_ROOM_NOT_FOUND" => "Khong tim thay du lieu phong phu hop.",
        "RESERVATION_ROOM_TYPE_INACTIVE" => "Loai phong da ngung su dung.",
        "FRONT_OFFICE_CONTEXT_REQUIRED" or "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED" => "Chua chon Property cho Front Office.",
        "FRONT_OFFICE_PROPERTY_NOT_AUTHORIZED" => "Ban khong co quyen su dung Property da chon.",
        "FRONT_OFFICE_PROPERTY_NOT_FOUND" => "Khong tim thay Property dang hoat dong cho Front Office.",
        "FRONT_OFFICE_PROPERTY_INACTIVE" => "Property da ngung hoat dong.",
        "FRONT_OFFICE_PROPERTY_LOAD_FAILED" => "Khong the tai Property context cho Front Office.",
        _ => error?.Message ?? "Khong the hoan tat thao tac. Vui long thu lai."
    };
}
