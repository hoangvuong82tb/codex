using AppConcierge = ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Concierge;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ServiceRequest;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Concierge;

public sealed class EfWakeUpCallService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : AppConcierge.IWakeUpCallService
{
    public async Task<AppConcierge.WakeUpCallDto> ScheduleAsync(Guid? stayId, Guid? roomId, DateTime wakeTime, string? notes, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var id = Uuid7.NewGuid();
        var call = ConciergeWakeUpCall.Create(propertyId, Number("WU", id), stayId, roomId, wakeTime, notes, clock.UtcNow);
        db.ConciergeWakeUpCalls.Add(call);
        await CommitAsync("WakeupScheduled", call.CallId, propertyId, ct);
        return Map(call);
    }

    public async Task<AppConcierge.WakeUpCallDto> CompleteAsync(Guid callId, AppConcierge.WakeUpCallStatus result, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var call = await db.ConciergeWakeUpCalls.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CallId == callId, ct)
            ?? throw new KeyNotFoundException("WAKEUP_NOT_FOUND");
        call.Complete(result.ToString(), clock.UtcNow);
        await CommitAsync(result == AppConcierge.WakeUpCallStatus.NoAnswer ? "WakeupNoAnswer" : "WakeupCompleted", call.CallId, propertyId, ct);
        return Map(call);
    }

    public async Task<AppConcierge.WakeUpCallDto> RetryAsync(Guid callId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var call = await db.ConciergeWakeUpCalls.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.CallId == callId, ct)
            ?? throw new KeyNotFoundException("WAKEUP_NOT_FOUND");
        call.Retry();
        await CommitAsync("WakeupScheduled", call.CallId, propertyId, ct);
        return Map(call);
    }

    public async Task<AppConcierge.WakeUpDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var today = clock.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var rows = await db.ConciergeWakeUpCalls.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.WakeTime >= today && x.WakeTime < tomorrow)
            .ToListAsync(ct);
        var finished = rows.Count(x => x.Status is "Completed" or "NoAnswer" or "Failed");
        var completed = rows.Count(x => x.Status == "Completed");
        return new AppConcierge.WakeUpDashboardDto(
            rows.Count(x => x.Status == "Scheduled"),
            completed,
            rows.Count(x => x.Status == "NoAnswer"),
            rows.Count(x => x.Status == "Failed"),
            rows.Count(x => x.Status is "Scheduled" or "InProgress"),
            finished == 0 ? 0m : Math.Round(completed * 100m / finished, 2));
    }

    public async Task<IReadOnlyList<AppConcierge.WakeUpCallDto>> GetScheduledAsync(Guid propertyId, DateTime forDate, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var start = forDate.Date;
        var end = start.AddDays(1);
        var rows = await db.ConciergeWakeUpCalls.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.WakeTime >= start && x.WakeTime < end && (x.Status == "Scheduled" || x.Status == "InProgress"))
            .OrderBy(x => x.WakeTime)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private async Task CommitAsync(string action, Guid id, Guid propertyId, CancellationToken ct)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(ConciergeWakeUpCall), id.ToString("D"), "Succeeded",
            Metadata: new Dictionary<string, string> { ["PropertyId"] = propertyId.ToString("D") }));
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "WAKEUP_SAVE_FAILED");
    }

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (RequireContext().Property.PropertyId.Value != propertyId) throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH"); }
    private static string Number(string prefix, Guid id) => $"{prefix}-{id:N}"[..15].ToUpperInvariant();
    private static AppConcierge.WakeUpCallDto Map(ConciergeWakeUpCall row)
        => new(row.CallId, row.CallNumber, row.StayId, row.RoomId, row.RoomNumber, row.GuestName, row.WakeTime, row.ActualCallTime,
            Enum.TryParse<AppConcierge.WakeUpCallStatus>(row.Status, true, out var status) ? status : AppConcierge.WakeUpCallStatus.Scheduled,
            row.RetryCount, row.IsRecurring, row.Notes, row.AssignedStaffId, row.CreatedAtUtc);
}

public sealed class EfLuggageService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : AppConcierge.ILuggageService
{
    public async Task<AppConcierge.LuggageItemDto> StoreAsync(AppConcierge.LuggageItemDto item, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var tag = string.IsNullOrWhiteSpace(item.TagNumber) ? $"LUG-{Uuid7.NewGuid():N}"[..16].ToUpperInvariant() : item.TagNumber.Trim();
        var row = ConciergeLuggageItem.Create(propertyId, tag, item.GuestId, item.GuestName, item.StayId, item.RoomNumber,
            item.Pieces, item.Description, item.StorageLocation, clock.UtcNow);
        db.ConciergeLuggageItems.Add(row);
        await CommitAsync("LuggageStored", row.ItemId, propertyId, ct);
        return Map(row);
    }

    public async Task<AppConcierge.LuggageItemDto> DeliverAsync(Guid itemId, Guid staffId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var row = await FindAsync(propertyId, itemId, ct);
        row.Deliver(staffId, clock.UtcNow, null);
        await CommitAsync("LuggageDelivered", row.ItemId, propertyId, ct);
        return Map(row);
    }

    public async Task<AppConcierge.LuggageItemDto> CollectAsync(Guid itemId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var row = await FindAsync(propertyId, itemId, ct);
        row.Collect(clock.UtcNow);
        await CommitAsync("LuggageCollected", row.ItemId, propertyId, ct);
        return Map(row);
    }

    public async Task<IReadOnlyList<AppConcierge.LuggageItemDto>> GetByGuestAsync(Guid guestId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var rows = await db.ConciergeLuggageItems.AsNoTracking().Where(x => x.PropertyId == propertyId && x.GuestId == guestId)
            .OrderByDescending(x => x.StoredAtUtc).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AppConcierge.LuggageItemDto>> GetStoredAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var rows = await db.ConciergeLuggageItems.AsNoTracking().Where(x => x.PropertyId == propertyId && (x.Status == "Stored" || x.Status == "InTransit"))
            .OrderBy(x => x.StoredAtUtc).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private async Task<ConciergeLuggageItem> FindAsync(Guid propertyId, Guid itemId, CancellationToken ct)
        => await db.ConciergeLuggageItems.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ItemId == itemId, ct)
            ?? throw new KeyNotFoundException("LUGGAGE_NOT_FOUND");

    private async Task CommitAsync(string action, Guid id, Guid propertyId, CancellationToken ct)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(ConciergeLuggageItem), id.ToString("D"), "Succeeded",
            Metadata: new Dictionary<string, string> { ["PropertyId"] = propertyId.ToString("D") }));
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "LUGGAGE_SAVE_FAILED");
    }

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (RequireContext().Property.PropertyId.Value != propertyId) throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH"); }
    private static AppConcierge.LuggageItemDto Map(ConciergeLuggageItem row)
        => new(row.ItemId, row.TagNumber, row.GuestId, row.GuestName, row.StayId, row.RoomNumber, row.Pieces, row.Description, row.StorageLocation,
            Enum.TryParse<AppConcierge.LuggageStatus>(row.Status, true, out var status) ? status : AppConcierge.LuggageStatus.Stored,
            row.StoredAtUtc, row.DeliveredAtUtc, row.CollectedAtUtc, row.HandledBy, row.HandlerName);
}

public sealed class EfTransportationService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : AppConcierge.ITransportationService
{
    public async Task<AppConcierge.TransportationDto> BookAsync(AppConcierge.TransportationDto booking, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var id = Uuid7.NewGuid();
        var row = ConciergeTransportationBooking.Create(propertyId, string.IsNullOrWhiteSpace(booking.BookingNumber) ? Number("TR", id) : booking.BookingNumber,
            booking.Type.ToString(), booking.GuestId, booking.GuestName, booking.StayId, booking.RoomNumber, booking.DriverName, booking.VehicleNumber,
            booking.VehicleType, booking.ScheduledAtUtc, booking.PickupLocation, booking.DropoffLocation, booking.FlightNumber, booking.PassengerCount,
            booking.Cost, booking.Notes);
        db.ConciergeTransportationBookings.Add(row);
        await CommitAsync("TransportationBooked", row.BookingId, propertyId, ct);
        return Map(row);
    }

    public async Task<AppConcierge.TransportationDto> CompleteAsync(Guid bookingId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var row = await db.ConciergeTransportationBookings.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.BookingId == bookingId, ct)
            ?? throw new KeyNotFoundException("TRANSPORT_NOT_FOUND");
        row.Complete();
        await CommitAsync("TransportationCompleted", row.BookingId, propertyId, ct);
        return Map(row);
    }

    public async Task<IReadOnlyList<AppConcierge.TransportationDto>> GetUpcomingAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var rows = await db.ConciergeTransportationBookings.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.ScheduledAtUtc >= clock.UtcNow && x.Status != "Completed" && x.Status != "Cancelled")
            .OrderBy(x => x.ScheduledAtUtc).Take(500).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private async Task CommitAsync(string action, Guid id, Guid propertyId, CancellationToken ct)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(action, nameof(ConciergeTransportationBooking), id.ToString("D"), "Succeeded",
            Metadata: new Dictionary<string, string> { ["PropertyId"] = propertyId.ToString("D") }));
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? "TRANSPORT_SAVE_FAILED");
    }

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (RequireContext().Property.PropertyId.Value != propertyId) throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH"); }
    private static string Number(string prefix, Guid id) => $"{prefix}-{id:N}"[..15].ToUpperInvariant();
    private static AppConcierge.TransportationDto Map(ConciergeTransportationBooking row)
        => new(row.BookingId, row.BookingNumber, Enum.TryParse<AppConcierge.TransportType>(row.Type, true, out var type) ? type : AppConcierge.TransportType.Taxi,
            row.GuestId, row.GuestName, row.StayId, row.RoomNumber, row.DriverName, row.VehicleNumber, row.VehicleType, row.ScheduledAtUtc,
            row.PickupLocation, row.DropoffLocation, row.FlightNumber, row.PassengerCount,
            Enum.TryParse<AppConcierge.ServiceStatus>(row.Status, true, out var status) ? status : AppConcierge.ServiceStatus.Requested, row.Cost, row.Notes);
}

public sealed class EfConciergeDashboardService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : AppConcierge.IConciergeDashboardService
{
    public async Task<AppConcierge.ConciergeDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var today = clock.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var now = clock.UtcNow;
        var requests = await db.FrontOfficeServiceRequests.AsNoTracking().Where(x => x.PropertyId == propertyId).ToListAsync(ct);
        var wakeups = await db.ConciergeWakeUpCalls.AsNoTracking().Where(x => x.PropertyId == propertyId && x.WakeTime >= today && x.WakeTime < tomorrow).ToListAsync(ct);
        var luggageStored = await db.ConciergeLuggageItems.AsNoTracking().CountAsync(x => x.PropertyId == propertyId && (x.Status == "Stored" || x.Status == "InTransit"), ct);
        var transport = await db.ConciergeTransportationBookings.AsNoTracking().CountAsync(x => x.PropertyId == propertyId && x.ScheduledAtUtc >= now && x.Status != "Completed" && x.Status != "Cancelled", ct);
        var breached = requests.Where(x => IsBreached(x, now)).OrderBy(x => ResolveSlaDeadline(x)).Take(20).ToList();
        var urgent = requests.Where(IsOpen).Where(x => x.Priority is ServicePriority.Urgent or ServicePriority.Emergency).OrderByDescending(x => x.Priority).Take(20).Select(x => MapDashboard(x, now)).ToList();
        var completed = requests.Where(x => x.Status == ServiceRequestStatus.Completed && x.CompletedAtUtc.HasValue).ToList();
        var responseMinutes = requests.Where(x => x.FirstResponseAtUtc.HasValue)
            .Select(x => (x.FirstResponseAtUtc!.Value - x.RequestedAtUtc).TotalMinutes)
            .Where(x => x >= 0)
            .ToList();
        var onTime = completed.Count(x => x.CompletedAtUtc <= ResolveSlaDeadline(x));
        return new AppConcierge.ConciergeDashboardDto(
            requests.Count(IsOpen),
            requests.Count(x => x.AssignedStaffId.HasValue && x.RequestedAtUtc >= today && x.RequestedAtUtc < tomorrow),
            requests.Count(x => x.CompletedAtUtc >= today && x.CompletedAtUtc < tomorrow),
            breached.Count,
            wakeups.Count(x => x.Status == "Scheduled"),
            luggageStored,
            transport,
            responseMinutes.Count == 0 ? 0m : Math.Round((decimal)responseMinutes.Average(), 2),
            completed.Count == 0 ? 0m : Math.Round(onTime * 100m / completed.Count, 2),
            urgent,
            breached.Select(x => MapDashboard(x, now)).ToList());
    }

    public async Task<IReadOnlyList<AppConcierge.ServicePerformanceDto>> GetPerformanceAsync(Guid propertyId, CancellationToken ct)
    {
        EnsureProperty(propertyId);
        var requests = await db.FrontOfficeServiceRequests.AsNoTracking().Where(x => x.PropertyId == propertyId).ToListAsync(ct);
        var result = requests.GroupBy(x => Enum.TryParse<AppConcierge.ServiceType>(x.ServiceType, true, out var type) ? type : AppConcierge.ServiceType.Other)
            .Select(g =>
            {
                var completed = g.Where(x => x.Status == ServiceRequestStatus.Completed && x.CompletedAtUtc.HasValue).ToList();
                var onTime = completed.Count(x => !IsBreached(x, x.CompletedAtUtc!.Value));
                var avgMinutes = completed.Count == 0
                    ? 0m
                    : Math.Round((decimal)completed.Average(x => (x.CompletedAtUtc!.Value - x.RequestedAtUtc).TotalMinutes), 2);
                return new AppConcierge.ServicePerformanceDto(
                    g.Key,
                    g.Count(),
                    0m,
                    g.Count() == 0 ? 0m : Math.Round(completed.Count * 100m / g.Count(), 2),
                    avgMinutes,
                    completed.Count == 0 ? 0m : Math.Round(onTime * 100m / completed.Count, 2));
            })
            .ToList();
        var wakeups = await db.ConciergeWakeUpCalls.AsNoTracking().CountAsync(x => x.PropertyId == propertyId, ct);
        if (wakeups > 0) result.Add(new AppConcierge.ServicePerformanceDto(AppConcierge.ServiceType.WakeUpCall, wakeups, 0m, 0m, 0m, 0m));
        return result;
    }

    private FrontOfficeExecutionContext RequireContext() => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private void EnsureProperty(Guid propertyId) { if (RequireContext().Property.PropertyId.Value != propertyId) throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH"); }
    private static bool IsOpen(ServiceRequest x) => x.Status is not (ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled or ServiceRequestStatus.Rejected);

    private static bool IsBreached(ServiceRequest request, DateTime now)
        => request.Status == ServiceRequestStatus.Completed
            ? request.CompletedAtUtc.HasValue && request.CompletedAtUtc.Value > ResolveSlaDeadline(request)
            : IsOpen(request) && now > ResolveSlaDeadline(request);

    private static DateTime ResolveSlaDeadline(ServiceRequest request)
        => request.SlaDeadlineAtUtc ?? request.RequestedAtUtc.Add(GetDefaultSlaWindow(request.Priority));

    private static TimeSpan GetDefaultSlaWindow(ServicePriority priority)
        => priority switch
        {
            ServicePriority.Emergency => TimeSpan.FromMinutes(15),
            ServicePriority.Urgent => TimeSpan.FromMinutes(30),
            ServicePriority.High => TimeSpan.FromHours(1),
            ServicePriority.Normal => TimeSpan.FromHours(2),
            _ => TimeSpan.FromHours(4)
        };

    private static AppConcierge.ServiceRequestDto MapDashboard(ServiceRequest request, DateTime now)
    {
        var mapped = Map(request);
        var deadline = ResolveSlaDeadline(request);
        var sla = request.Status == ServiceRequestStatus.Completed
            ? request.CompletedAtUtc <= deadline ? AppConcierge.SlaStatus.OnTrack : AppConcierge.SlaStatus.Breached
            : now > deadline ? AppConcierge.SlaStatus.Breached : AppConcierge.SlaStatus.OnTrack;
        return mapped with { SlaDeadlineAtUtc = deadline, SlaStatus = sla };
    }

    private static AppConcierge.ServiceRequestDto Map(ServiceRequest request)
    {
        var type = Enum.TryParse<AppConcierge.ServiceType>(request.ServiceType, true, out var parsedType) ? parsedType : AppConcierge.ServiceType.Other;
        var priority = request.Priority switch
        {
            ServicePriority.High => AppConcierge.ServicePriority.High,
            ServicePriority.Urgent => AppConcierge.ServicePriority.Urgent,
            ServicePriority.Emergency => AppConcierge.ServicePriority.Vip,
            _ => AppConcierge.ServicePriority.Normal
        };
        var status = request.Status switch
        {
            ServiceRequestStatus.Assigned => AppConcierge.ServiceStatus.Assigned,
            ServiceRequestStatus.Accepted => AppConcierge.ServiceStatus.Accepted,
            ServiceRequestStatus.InProgress => AppConcierge.ServiceStatus.InProgress,
            ServiceRequestStatus.Completed => AppConcierge.ServiceStatus.Completed,
            ServiceRequestStatus.Cancelled => AppConcierge.ServiceStatus.Cancelled,
            _ => AppConcierge.ServiceStatus.Requested
        };
        return new AppConcierge.ServiceRequestDto(request.ServiceRequestId, request.RequestNumber, null, request.GuestName, request.HotelStayId,
            request.RoomNumber, type, status, priority, request.Description ?? string.Empty, request.AssignedStaffId, null,
            request.RequestedAtUtc, request.CompletedAtUtc, null, AppConcierge.SlaStatus.OnTrack, priority == AppConcierge.ServicePriority.Vip,
            request.Category.ToString(), request.EstimatedCost, request.ActualCost, request.ChargeType.ToString(), request.AssignedDepartment);
    }
}
