using ASMILE.BO.Application.FrontOffice.Concierge;

namespace ASMILE.BO.Infrastructure.FrontOffice.Concierge;

public sealed class ServiceRequestService : IServiceRequestService
{
    public Task<ServiceRequestDto> CreateAsync(Guid? guestId, Guid? stayId, ServiceType type, ServicePriority priority, string description, DateTime? sla, CancellationToken ct) => Task.FromResult(new ServiceRequestDto(Guid.NewGuid(), $"SR-{DateTime.UtcNow:yyyyMMdd}-001", guestId, "Guest Name", stayId, "101", type, ServiceStatus.Requested, priority, description, null, null, DateTime.UtcNow, null, sla, sla is not null ? SlaStatus.OnTrack : (SlaStatus)(-1), priority == ServicePriority.Vip));
    public Task<ServiceRequestDto> AssignAsync(Guid requestId, Guid staffId, CancellationToken ct) => Task.FromResult(new ServiceRequestDto(requestId, "SR-001", Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", ServiceType.AirportTransfer, ServiceStatus.Assigned, ServicePriority.High, "Pickup at airport", staffId, "Concierge A", DateTime.UtcNow, null, DateTime.UtcNow.AddMinutes(60), SlaStatus.OnTrack, false));
    public Task<ServiceRequestDto> TransitionAsync(Guid requestId, ServiceStatus to, string? note, CancellationToken ct) => Task.FromResult(new ServiceRequestDto(requestId, "SR-001", Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", ServiceType.AirportTransfer, to, ServicePriority.High, "Pickup", Guid.NewGuid(), "Concierge A", DateTime.UtcNow, to == ServiceStatus.Completed ? DateTime.UtcNow : null, DateTime.UtcNow.AddMinutes(30), SlaStatus.OnTrack, false));
    public Task<IReadOnlyList<ServiceRequestDto>> GetOpenAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<ServiceRequestDto>>([new ServiceRequestDto(Guid.NewGuid(), "SR-001", Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", ServiceType.WakeUpCall, ServiceStatus.InProgress, ServicePriority.High, "Wake up at 6am", Guid.NewGuid(), "Concierge A", DateTime.UtcNow, null, DateTime.UtcNow.AddMinutes(30), SlaStatus.OnTrack, false)]);
    public Task<IReadOnlyList<ServiceSlaDto>> GetSlaReportAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<ServiceSlaDto>>([new ServiceSlaDto(ServiceType.WakeUpCall, 50, 48, 45, 3, 90m, 3, 8)]);
}

public sealed class WakeUpCallService : IWakeUpCallService
{
    public Task<WakeUpCallDto> ScheduleAsync(Guid? stayId, Guid? roomId, DateTime wakeTime, string? notes, CancellationToken ct) => Task.FromResult(new WakeUpCallDto(Guid.NewGuid(), $"WU-{DateTime.UtcNow:yyyyMMdd}-001", stayId, roomId, "101", "Guest", wakeTime, null, WakeUpCallStatus.Scheduled, 0, false, notes, null, DateTime.UtcNow));
    public Task<WakeUpCallDto> CompleteAsync(Guid callId, WakeUpCallStatus result, CancellationToken ct) => Task.FromResult(new WakeUpCallDto(callId, "WU-001", Guid.NewGuid(), Guid.NewGuid(), "101", "Guest", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, result, 0, false, null, Guid.NewGuid(), DateTime.UtcNow));
    public Task<WakeUpCallDto> RetryAsync(Guid callId, CancellationToken ct) => Task.FromResult(new WakeUpCallDto(callId, "WU-001", Guid.NewGuid(), Guid.NewGuid(), "101", "Guest", DateTime.UtcNow, null, WakeUpCallStatus.Scheduled, 2, false, null, Guid.NewGuid(), DateTime.UtcNow));
    public Task<WakeUpDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new WakeUpDashboardDto(25, 20, 2, 1, 2, 80m));
    public Task<IReadOnlyList<WakeUpCallDto>> GetScheduledAsync(Guid propertyId, DateTime forDate, CancellationToken ct) => Task.FromResult<IReadOnlyList<WakeUpCallDto>>([new WakeUpCallDto(Guid.NewGuid(), "WU-001", Guid.NewGuid(), Guid.NewGuid(), "101", "Guest", forDate, null, WakeUpCallStatus.Scheduled, 0, false, null, null, DateTime.UtcNow)]);
}

public sealed class LuggageService : ILuggageService
{
    public Task<LuggageItemDto> StoreAsync(LuggageItemDto item, CancellationToken ct) => Task.FromResult(item with { Status = LuggageStatus.Stored, StoredAtUtc = DateTime.UtcNow, TagNumber = $"LUG-{Guid.NewGuid():N}"[..8] });
    public Task<LuggageItemDto> DeliverAsync(Guid itemId, Guid staffId, CancellationToken ct) => Task.FromResult(new LuggageItemDto(itemId, "LUG-001", Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", 3, "Suitcase x2, Bag x1", "Storage A", LuggageStatus.Delivered, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow, null, staffId, "Bell A"));
    public Task<LuggageItemDto> CollectAsync(Guid itemId, CancellationToken ct) => Task.FromResult(new LuggageItemDto(itemId, "LUG-001", Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", 3, null, null, LuggageStatus.Collected, DateTime.UtcNow.AddHours(-3), null, DateTime.UtcNow, Guid.NewGuid(), null));
    public Task<IReadOnlyList<LuggageItemDto>> GetByGuestAsync(Guid guestId, CancellationToken ct) => Task.FromResult<IReadOnlyList<LuggageItemDto>>([new LuggageItemDto(Guid.NewGuid(), "LUG-001", guestId, "Guest", Guid.NewGuid(), "101", 2, "Bag", "Storage A", LuggageStatus.Stored, DateTime.UtcNow, null, null, null, null)]);
    public Task<IReadOnlyList<LuggageItemDto>> GetStoredAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<LuggageItemDto>>([new LuggageItemDto(Guid.NewGuid(), "LUG-001", Guid.NewGuid(), "Guest", null, null, 2, null, "Storage A", LuggageStatus.Stored, DateTime.UtcNow, null, null, null, null)]);
}

public sealed class TransportationService : ITransportationService
{
    public Task<TransportationDto> BookAsync(TransportationDto b, CancellationToken ct) => Task.FromResult(b with { BookingNumber = $"TR-{Guid.NewGuid():N}"[..8], Status = ServiceStatus.Requested });
    public Task<TransportationDto> CompleteAsync(Guid bookingId, CancellationToken ct) => Task.FromResult(new TransportationDto(bookingId, "TR-001", TransportType.AirportPickup, Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", "Driver A", "51A-12345", "Sedan", DateTime.UtcNow.AddHours(-2), "Airport T2", "Hotel Main Entrance", "VN123", 2, ServiceStatus.Completed, 350000, null));
    public Task<IReadOnlyList<TransportationDto>> GetUpcomingAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<TransportationDto>>([new TransportationDto(Guid.NewGuid(), "TR-002", TransportType.Taxi, Guid.NewGuid(), "Guest", Guid.NewGuid(), "202", null, null, null, DateTime.UtcNow.AddHours(3), "Hotel", "City Center", null, 1, ServiceStatus.Requested, null, null)]);
}

public sealed class ConciergeDashboardService : IConciergeDashboardService
{
    public Task<ConciergeDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new ConciergeDashboardDto(12, 8, 15, 1, 25, 15, 5, 12m, 92m, [], []));
    public Task<IReadOnlyList<ServicePerformanceDto>> GetPerformanceAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<ServicePerformanceDto>>([new ServicePerformanceDto(ServiceType.WakeUpCall, 250, 4.8m, 95m, 8m, 90m)]);
}
