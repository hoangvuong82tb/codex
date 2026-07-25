using ASMILE.BO.App.Models;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Services;

/// <summary>Scoped adapter over existing hotel-room and reservation queries. It contains no reception workflow or command.</summary>
public sealed class ReceptionUiService : IReceptionUiService
{
    private readonly IServiceScopeFactory scopeFactory;
    private IServiceScope scope = null!;
    private IFrontOfficeExecutionContextAccessor context = null!;
    private IAuthorizationService authorization = null!;
    private HotelRoomMasterDataQueryHandlers hotelQueries = null!;
    private SearchReservationsQueryHandler reservationSearch = null!;
    private SearchStaysQueryHandler staySearch = null!;
    private GetCurrentInHouseStaysQueryHandler currentInHouse = null!;
    private CheckInHotelStayCommandHandler checkIn = null!;
    private CheckOutHotelStayCommandHandler checkOut = null!;

    public ReceptionUiService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
        CreateScope();
    }

    public bool HasContext => context.Current is not null;
    public bool CanView => authorization.HasPermission(ReservationPermissions.View) && authorization.HasPermission(HotelStayPermissions.View);

    public void ResetForContextChange()
    {
        scope.Dispose();
        CreateScope();
    }

    public async Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetLockedRoomsAsync(CancellationToken cancellationToken)
    {
        var unavailable = await hotelQueries.SearchRooms(null, null, null, null, HotelRoomOperationalStatus.OutOfOrder, true, new PageRequest(1, 200), "number", false, cancellationToken);
        if (unavailable.IsFailure) return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(unavailable.Error!);

        var outOfService = await hotelQueries.SearchRooms(null, null, null, null, HotelRoomOperationalStatus.OutOfService, true, new PageRequest(1, 200), "number", false, cancellationToken);
        if (outOfService.IsFailure) return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(outOfService.Error!);

        var rows = unavailable.Value!.Items.Concat(outOfService.Value!.Items)
            .OrderBy(room => room.RoomNumber, StringComparer.CurrentCultureIgnoreCase)
            .Select(room => new ReceptionReadOnlyRow(
                room.RoomNumber,
                string.Join(" • ", new[] { room.DisplayName, room.HotelRoomTypeName, room.Floor, room.Building }.Where(value => !string.IsNullOrWhiteSpace(value))),
                room.OperationalStatus == HotelRoomOperationalStatus.OutOfOrder ? "Ngừng khai thác" : "Tạm dừng"))
            .ToArray();
        return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success(rows);
    }

    public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetArrivalsTodayAsync(CancellationToken cancellationToken) => GetTodayReservationsAsync(true, cancellationToken);
    public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetDeparturesTodayAsync(CancellationToken cancellationToken) => GetTodayReservationsAsync(false, cancellationToken);

    public async Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetStayProfilesAsync(CancellationToken cancellationToken)
    {
        var result = await staySearch.HandleAsync(new SearchStaysQuery(null, null, null, null, new PageRequest(1, 200), "arrival"), cancellationToken);
        if (result.IsFailure) return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(result.Error!);
        return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success(result.Value!.Items.Select(stay => new ReceptionReadOnlyRow(stay.GuestName, $"{stay.StayNumber} • {stay.RoomNumber}", stay.Status.ToString())).ToArray());
    }

    public async Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetCurrentInHouseAsync(CancellationToken cancellationToken)
    {
        var result = await currentInHouse.HandleAsync(new GetCurrentInHouseStaysQuery(), cancellationToken);
        if (result.IsFailure) return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(result.Error!);
        return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success(result.Value!.Select(stay => new ReceptionReadOnlyRow(stay.GuestName, $"{stay.StayNumber} • {stay.RoomNumber}", stay.Status.ToString())).ToArray());
    }
    public async Task<Result<IReadOnlyList<HotelStayDto>>> GetPendingArrivalsAsync(string? keyword, CancellationToken cancellationToken)
    { var result = await staySearch.HandleAsync(new SearchStaysQuery(keyword, HotelStayStatus.PendingArrival, null, null, new PageRequest(1, 200), "arrival"), cancellationToken); return result.IsFailure ? Result<IReadOnlyList<HotelStayDto>>.Failure(result.Error!) : Result<IReadOnlyList<HotelStayDto>>.Success(result.Value!.Items); }
    public Task<Result<HotelStayDto>> CheckInAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken) => checkIn.HandleAsync(new CheckInHotelStayCommand(stayId, rowVersion), cancellationToken);
    public async Task<Result<IReadOnlyList<HotelStayDto>>> GetInHouseForCheckOutAsync(string? keyword, CancellationToken cancellationToken)
    { var result = await staySearch.HandleAsync(new SearchStaysQuery(keyword, HotelStayStatus.InHouse, null, null, new PageRequest(1, 200), "departure"), cancellationToken); return result.IsFailure ? Result<IReadOnlyList<HotelStayDto>>.Failure(result.Error!) : Result<IReadOnlyList<HotelStayDto>>.Success(result.Value!.Items); }
    public Task<Result<HotelStayDto>> CheckOutAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken) => checkOut.HandleAsync(new CheckOutHotelStayCommand(stayId, rowVersion), cancellationToken);

    private async Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetTodayReservationsAsync(bool arrivals, CancellationToken cancellationToken)
    {
        if (context.Current is not { } current)
        {
            return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(Error.Authorization("FRONT_OFFICE_CONTEXT_REQUIRED", "A Front Office property context is required."));
        }

        var date = DateOnly.FromDateTime(DateTime.Today);
        var page = await reservationSearch.HandleAsync(
            new SearchReservationsQuery(current.Property.PropertyId.Value, new ReservationSearchFilter(null, null, null, date, date.AddDays(1), null, null, null, null, null, null, null), new PageRequest(1, 200), arrivals ? "arrival" : "departure"),
            cancellationToken);
        if (page.IsFailure) return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Failure(page.Error!);

        var rows = page.Value!.Items
            .Where(reservation => arrivals ? reservation.ArrivalDate == date : reservation.DepartureDate == date)
            .Where(reservation => reservation.Status is not ReservationStatus.Cancelled and not ReservationStatus.NoShow)
            .OrderBy(reservation => reservation.GuestName, StringComparer.CurrentCultureIgnoreCase)
            .Select(reservation => new ReceptionReadOnlyRow(
                reservation.GuestName,
                $"{reservation.ReservationNumber} • {reservation.AdultCount} người lớn, {reservation.ChildCount} trẻ em",
                reservation.Status.ToString()))
            .ToArray();
        return Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success(rows);
    }

    private void CreateScope()
    {
        scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        context = services.GetRequiredService<IFrontOfficeExecutionContextAccessor>();
        authorization = services.GetRequiredService<IAuthorizationService>();
        hotelQueries = services.GetRequiredService<HotelRoomMasterDataQueryHandlers>();
        reservationSearch = services.GetRequiredService<SearchReservationsQueryHandler>();
        staySearch = services.GetRequiredService<SearchStaysQueryHandler>();
        currentInHouse = services.GetRequiredService<GetCurrentInHouseStaysQueryHandler>();
        checkIn = services.GetRequiredService<CheckInHotelStayCommandHandler>();
        checkOut = services.GetRequiredService<CheckOutHotelStayCommandHandler>();
        dnd = services.GetRequiredService<ToggleDoNotDisturbCommandHandler>();
        wakeUpCall = services.GetRequiredService<RequestWakeUpCallCommandHandler>();
    }

    private ToggleDoNotDisturbCommandHandler dnd = null!;
    private RequestWakeUpCallCommandHandler wakeUpCall = null!;

    public async Task<Result<IReadOnlyList<HotelStayDto>>> GetCurrentInHouseStaysAsync(int limit, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Stay.View")) return Result<IReadOnlyList<HotelStayDto>>.Failure(Error.Authorization("PERMISSION_DENIED", "Stay view permission required."));
        if (context.Current is not { } c) return Result<IReadOnlyList<HotelStayDto>>.Failure(Error.Authorization("CONTEXT_REQUIRED", "Property context required."));
        return await currentInHouse.HandleAsync(new GetCurrentInHouseStaysQuery(), ct);
    }

    public async Task<Result<HotelStayDto>> ToggleDndAsync(Guid stayId, bool enabled, byte[]? rowVersion, CancellationToken ct)
    {
        if (context.Current is not { } c) return Result<HotelStayDto>.Failure(Error.Authorization("CONTEXT_REQUIRED", "Property context required."));
        return await dnd.HandleAsync(new ToggleDoNotDisturbCommand(stayId, enabled, rowVersion ?? []), ct);
    }

    public async Task<Result<HotelStayDto>> RequestWakeUpCallAsync(Guid stayId, DateTime requestedAt, string? notes, byte[]? rowVersion, CancellationToken ct)
    {
        if (context.Current is not { } c) return Result<HotelStayDto>.Failure(Error.Authorization("CONTEXT_REQUIRED", "Property context required."));
        return await wakeUpCall.HandleAsync(new RequestWakeUpCallCommand(stayId, requestedAt, notes, rowVersion ?? []), ct);
    }

    public void Dispose() => scope.Dispose();
}
