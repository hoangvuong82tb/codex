using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.RoomRack;

// === DTOs ===
public sealed record RoomRackDto(
    Guid HotelRoomId, string RoomNumber, string RoomTypeName, string? Floor, string? Building, string? Zone,
    string OccupancyStatus, string HousekeepingStatus, string InventoryStatus, string ServiceStatus,
    string? GuestName, string? GuestNumber, bool IsVip, string? Nationality,
    DateOnly? ArrivalDate, DateOnly? DepartureDate, int Adults, int Children,
    decimal? FolioBalance, decimal? DepositBalance, decimal? OutstandingBalance,
    string? SpecialRequests, DateTime? LastCleanedAt, bool HasWakeUpCall,
    bool IsBirthday, bool HasMaintenanceRequest, bool HasRoomMovePending);

public sealed record RoomRackAlertDto(string AlertType, string Severity, string RoomNumber, string Title, string? Action, DateTime CreatedAt);
public sealed record RoomRackFilterDto(string? Building, string? Floor, string? RoomType, string? Occupancy, string? HousekeepingStatus, string? InventoryStatus, bool? IsVip, bool? ArrivingToday, bool? DepartingToday);

public sealed record GetRoomRackQuery(RoomRackFilterDto? Filter, string? Search, PageRequest Page) : IQuery<PageResult<RoomRackDto>>;
public sealed record GetRoomRackAlertsQuery(Guid PropertyId) : IQuery<IReadOnlyList<RoomRackAlertDto>>;

public interface IRoomRackQueryService
{
    Task<Result<PageResult<RoomRackDto>>> GetRoomRackAsync(GetRoomRackQuery q, CancellationToken ct);
    Task<Result<IReadOnlyList<RoomRackAlertDto>>> GetAlertsAsync(GetRoomRackAlertsQuery q, CancellationToken ct);
}
