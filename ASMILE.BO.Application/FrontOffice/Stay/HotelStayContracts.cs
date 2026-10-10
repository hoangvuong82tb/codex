using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Stay;
using HotelStayAggregate = ASMILE.BO.Core.FrontOffice.Stay.HotelStay;

namespace ASMILE.BO.Application.FrontOffice.Stay;

// === DTOs ===
public sealed record HotelStayDto(Guid HotelStayId, string StayNumber, Guid PropertyId, Guid? ReservationId, string? ReservationNumber, Guid? GuestId, string GuestName, Guid RoomId, string RoomNumber, Guid RoomTypeId, string RoomTypeCode, DateOnly ScheduledArrivalDate, DateOnly ScheduledDepartureDate, DateTime? ActualCheckInAtUtc, DateTime? ActualCheckOutAtUtc, int Adults, int Children, HotelStayStatus Status, string Source, int ExtensionCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, byte[]? RowVersion, bool IsDepartureApproved = false, string? GuestPhone = null, string? SpecialRequests = null, string? InternalNotes = null);

public sealed record StayOccupantDto(Guid OccupantId, Guid PropertyId, Guid HotelStayId, Guid GuestProfileId, string GuestNumber, string FullName, string? Phone, HotelStayOccupantType OccupantType, DateTime AddedAtUtc, bool IsActive, byte[]? RowVersion);

public sealed record StayTimelineEntryDto(DateTime OccurredAtUtc, string EventName, HotelStayStatus? FromStatus, HotelStayStatus ToStatus, Guid? ActorId, string? Metadata);

public sealed record RoomOccupancyDto(Guid RoomId, string RoomNumber, string RoomTypeCode, string OperationalStatus, string OccupancyStatus, string? CurrentStayNumber);

// === Interfaces ===
public interface IHotelStayNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default); }

public interface IHotelStayRepository
{
    Task AddAsync(HotelStayAggregate stay, HotelStayHistory history, CancellationToken cancellationToken = default);
    Task<HotelStayAggregate?> GetAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default);
    async Task<HotelStayAggregate?> GetActiveForReservationAsync(Guid propertyId, Guid reservationId, CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(propertyId, null, null, null, null, new PageRequest(1, 200), null, cancellationToken);
        var active = page.Items.FirstOrDefault(x => x.ReservationId == reservationId && x.Status is HotelStayStatus.PendingArrival or HotelStayStatus.PreArrival or HotelStayStatus.CheckedIn or HotelStayStatus.InHouse or HotelStayStatus.RoomMoved or HotelStayStatus.Extended or HotelStayStatus.ExpectedDeparture);
        return active is null ? null : await GetAsync(propertyId, active.HotelStayId, cancellationToken);
    }
    Task<HotelStayDto?> GetDtoAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default);
    Task<HotelStayDto?> GetByNumberAsync(Guid propertyId, string stayNumber, CancellationToken cancellationToken = default);
    Task<PageResult<HotelStayDto>> SearchAsync(Guid propertyId, string? keyword, HotelStayStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotelStayDto>> GetCurrentInHouseAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotelStayDto>> GetExpectedArrivalsAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotelStayDto>> GetExpectedDeparturesAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StayTimelineEntryDto>> GetTimelineAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveForReservationAsync(Guid propertyId, Guid reservationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsInHouseForRoomAsync(Guid propertyId, Guid roomId, Guid? excludingStayId = null, CancellationToken cancellationToken = default);
    Task AppendHistoryAsync(HotelStayHistory history, CancellationToken cancellationToken = default);
    void ApplyExpectedRowVersion(HotelStayAggregate stay, byte[] expectedRowVersion);
}

public interface IHotelStayOccupancyReader { Task<IReadOnlyList<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid propertyId, CancellationToken cancellationToken = default); }

public interface IHotelStayOccupantRepository
{
    Task<IReadOnlyList<StayOccupantDto>> GetActiveAsync(Guid propertyId, Guid hotelStayId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAsync(Guid propertyId, Guid hotelStayId, Guid guestProfileId, CancellationToken cancellationToken = default);
    Task<HotelStayOccupant?> GetAsync(Guid propertyId, Guid hotelStayId, Guid occupantId, CancellationToken cancellationToken = default);
    Task AddAsync(HotelStayOccupant occupant, CancellationToken cancellationToken = default);
    void ApplyExpectedRowVersion(HotelStayOccupant occupant, byte[] expectedRowVersion);
    Task<StayOccupantDto?> GetDtoAsync(Guid propertyId, Guid occupantId, CancellationToken cancellationToken = default);
}

// === Commands ===
public sealed record PrepareStayFromReservationCommand(Guid ReservationId) : ICommand<HotelStayDto>;
public sealed record ConfirmPreArrivalCommand(Guid HotelStayId, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record PreCheckInHotelStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion, DateTime? Eta = null, bool IdentityVerified = true, bool SpecialRequestsConfirmed = true, bool RoomPreferencesConfirmed = true) : ICommand<HotelStayDto>;
public sealed record CheckInHotelStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion, DateTime? ActualCheckInAtUtc = null) : ICommand<HotelStayDto>;
public sealed record CheckInReservationCommand(Guid ReservationId, byte[] ExpectedRowVersion, DateTime? ActualCheckInAtUtc = null) : ICommand<HotelStayDto>;
public sealed record ExtendStayCommand(Guid HotelStayId, DateOnly NewDepartureDate, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record MoveRoomCommand(Guid HotelStayId, Guid ToRoomId, string Reason, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record UpdateStayInformationCommand(Guid HotelStayId, byte[] ExpectedRowVersion, string GuestName, string? GuestPhone, string? SpecialRequests, string? InternalNotes, string Reason) : ICommand<HotelStayDto>;
public sealed record CheckOutHotelStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion, DateTime? ActualCheckOutAtUtc = null, string? DepartureReason = null, string? DepartureNotes = null, bool HasPendingCharges = false, bool HasOutstandingBalance = false) : ICommand<HotelStayDto>;
public sealed record ApproveDepartureCommand(Guid HotelStayId, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record CompleteStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record ReopenStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record CancelStayCommand(Guid HotelStayId, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record ToggleDoNotDisturbCommand(Guid HotelStayId, bool Enabled, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record RequestWakeUpCallCommand(Guid HotelStayId, DateTime RequestedAtUtc, string? Notes, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record LogComplaintCommand(Guid HotelStayId, string Category, string Description, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record AddStayTagCommand(Guid HotelStayId, string Tag, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record RemoveStayTagCommand(Guid HotelStayId, string Tag, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record MarkVipServiceCommand(Guid HotelStayId, bool Enabled, byte[] ExpectedRowVersion) : ICommand<HotelStayDto>;
public sealed record AddStayOccupantCommand(Guid HotelStayId, Guid GuestProfileId, HotelStayOccupantType OccupantType, byte[] ExpectedRowVersion) : ICommand<StayOccupantDto>;
public sealed record RemoveStayOccupantCommand(Guid HotelStayId, Guid OccupantId, byte[] ExpectedStayRowVersion, byte[] ExpectedOccupantRowVersion) : ICommand<StayOccupantDto>;

// === Queries ===
public sealed record GetStayByIdQuery(Guid HotelStayId) : IQuery<HotelStayDto>;
public sealed record GetStayByNumberQuery(string StayNumber) : IQuery<HotelStayDto>;
public sealed record SearchStaysQuery(string? Keyword, HotelStayStatus? Status, DateOnly? DateFrom, DateOnly? DateTo, PageRequest Page, string? Sort) : IQuery<PageResult<HotelStayDto>>;
public sealed record GetCurrentInHouseStaysQuery : IQuery<IReadOnlyList<HotelStayDto>>;
public sealed record GetTodayExpectedArrivalsQuery : IQuery<IReadOnlyList<HotelStayDto>>;
public sealed record GetTodayExpectedDeparturesQuery : IQuery<IReadOnlyList<HotelStayDto>>;
public sealed record GetStayTimelineQuery(Guid HotelStayId) : IQuery<IReadOnlyList<StayTimelineEntryDto>>;
