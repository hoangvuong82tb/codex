using ASMILE.BO.Core.FrontOffice.Availability;

namespace ASMILE.BO.Application.FrontOffice.Availability;

public interface IAvailabilityEngine
{
    Task<AvailabilityResult> CheckAvailabilityAsync(AvailabilityRequest request, CancellationToken ct);
    Task<IReadOnlyList<RoomAvailability>> GetRoomAvailabilityAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
    Task<IReadOnlyList<RoomTypeInventory>> CalculateInventoryAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
    Task<IReadOnlyList<DailyInventory>> CalculateDailyInventoryAsync(Guid propertyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct);
    Task<AvailabilityForecast> CalculateForecastAsync(Guid propertyId, DateOnly fromDate, int days, CancellationToken ct);
}
