using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ServiceRequest;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppServicePriority = ASMILE.BO.Application.FrontOffice.Concierge.ServicePriority;

namespace ASMILE.BO.Infrastructure.FrontOffice.Concierge;

public sealed class EfServiceRequestService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IServiceRequestService
{
    public Task<ServiceRequestDto> CreateAsync(
        Guid? guestId,
        Guid? stayId,
        ServiceType type,
        AppServicePriority priority,
        string description,
        DateTime? slaDeadline,
        CancellationToken ct)
        => CreateAsync(new CreateServiceRequestRequest(
            guestId,
            stayId,
            null,
            null,
            null,
            type,
            priority,
            description,
            slaDeadline,
            null,
            null), ct);

    public async Task<ServiceRequestDto> CreateAsync(CreateServiceRequestRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var now = clock.UtcNow;
        var slaDeadline = request.SlaDeadlineAtUtc ?? now.Add(GetDefaultSlaWindow(request.Priority));

        var serviceRequest = ServiceRequest.Create(
            propertyId,
            await NextNumberAsync(propertyId, now, ct),
            request.Type.ToString(),
            ToDomainCategory(request.Type),
            ToDomainPriority(request.Priority),
            request.Description,
            request.StayId,
            request.RoomId,
            request.RoomNumber,
            request.GuestName,
            ToDomainChargeType(request.ChargeType),
            request.EstimatedCost,
            slaDeadline,
            now,
            context.Actor.ActorId);

        db.FrontOfficeServiceRequests.Add(serviceRequest);
        StageAudit("ServiceRequestCreated", serviceRequest, propertyId);
        await CommitAsync(ct, "SERVICE_REQUEST_CREATE_FAILED");
        return Map(serviceRequest);
    }

    public async Task<ServiceRequestDto> AssignAsync(Guid requestId, Guid staffId, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var serviceRequest = await FindAsync(propertyId, requestId, ct);
        if (staffId == Guid.Empty) throw new InvalidOperationException("SERVICE_REQUEST_STAFF_REQUIRED");

        var staffExists = await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == staffId, ct);
        if (!staffExists) throw new KeyNotFoundException("SERVICE_REQUEST_STAFF_NOT_FOUND");

        serviceRequest.Assign("Concierge", staffId, clock.UtcNow);
        StageAudit("ServiceRequestAssigned", serviceRequest, propertyId, staffId.ToString("D"));
        await CommitAsync(ct, "SERVICE_REQUEST_ASSIGN_FAILED");
        return Map(serviceRequest);
    }

    public async Task<ServiceRequestDto> TransitionAsync(Guid requestId, ServiceStatus to, string? note, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var serviceRequest = await FindAsync(propertyId, requestId, ct);
        var now = clock.UtcNow;

        switch (to)
        {
            case ServiceStatus.Requested:
                break;
            case ServiceStatus.Assigned:
                serviceRequest.Assign(serviceRequest.AssignedDepartment ?? "Concierge", serviceRequest.AssignedStaffId, now);
                break;
            case ServiceStatus.Accepted:
                serviceRequest.Accept(now);
                break;
            case ServiceStatus.InProgress:
                serviceRequest.StartWork(now);
                break;
            case ServiceStatus.Completed:
                serviceRequest.Complete(serviceRequest.ActualCost, now);
                break;
            case ServiceStatus.Cancelled:
                serviceRequest.Cancel(note ?? "Cancelled", now);
                break;
            case ServiceStatus.Escalated:
                throw new InvalidOperationException("SERVICE_REQUEST_ESCALATION_NOT_SUPPORTED");
            default:
                throw new InvalidOperationException("SERVICE_REQUEST_STATUS_NOT_SUPPORTED");
        }

        AddPendingHistory(serviceRequest);

        StageAudit("ServiceRequestStatusChanged", serviceRequest, propertyId, note);
        await CommitAsync(ct, "SERVICE_REQUEST_STATUS_CHANGE_FAILED");
        return Map(serviceRequest);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetOpenAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);

        var rows = await db.FrontOfficeServiceRequests
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        x.Status != ServiceRequestStatus.Completed &&
                        x.Status != ServiceRequestStatus.Cancelled)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.RequestedAtUtc)
            .Take(5000)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetAllAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);

        var rows = await db.FrontOfficeServiceRequests
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(5000)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ServiceSlaDto>> GetSlaReportAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);

        var requests = await db.FrontOfficeServiceRequests
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status != ServiceRequestStatus.Cancelled)
            .OrderBy(x => x.ServiceType)
            .ThenBy(x => x.RequestedAtUtc)
            .Take(5000)
            .ToListAsync(ct);
        if (requests.Count == 0) return [];

        var requestIds = requests.Select(x => x.ServiceRequestId).ToList();
        var history = await db.FrontOfficeServiceHistories
            .AsNoTracking()
            .Where(x => requestIds.Contains(x.ServiceRequestId))
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(ct);
        var historyByRequest = history
            .GroupBy(x => x.ServiceRequestId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var now = clock.UtcNow;

        return requests
            .GroupBy(x => Enum.TryParse<ServiceType>(x.ServiceType, true, out var type) ? type : ServiceType.Other)
            .Select(group =>
            {
                var completed = group.Where(x => x.Status == ServiceRequestStatus.Completed && x.CompletedAtUtc.HasValue).ToList();
                var onTime = completed.Count(x => x.CompletedAtUtc <= ResolveSlaDeadline(x));
                var breached = group.Count(x =>
                {
                    var deadline = ResolveSlaDeadline(x);
                    return x.Status == ServiceRequestStatus.Completed
                        ? x.CompletedAtUtc > deadline
                        : now > deadline;
                });
                var responseMinutes = group
                    .Select(x =>
                    {
                        var responseAt = ResolveFirstResponse(x, historyByRequest);
                        return responseAt.HasValue ? (double?) (responseAt.Value - x.RequestedAtUtc).TotalMinutes : null;
                    })
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();
                var completionMinutes = completed
                    .Select(x => (x.CompletedAtUtc!.Value - x.RequestedAtUtc).TotalMinutes)
                    .ToList();
                return new ServiceSlaDto(
                    group.Key,
                    group.Count(),
                    completed.Count,
                    onTime,
                    breached,
                    completed.Count == 0 ? 0 : Math.Round(onTime * 100m / completed.Count, 2),
                    responseMinutes.Count == 0 ? 0 : (int)Math.Round(responseMinutes.Average(), MidpointRounding.AwayFromZero),
                    completionMinutes.Count == 0 ? 0 : (int)Math.Round(completionMinutes.Average(), MidpointRounding.AwayFromZero));
            })
            .OrderBy(x => x.Type)
            .ToList();
    }

    private async Task<ServiceRequest> FindAsync(Guid propertyId, Guid requestId, CancellationToken ct)
        => await db.FrontOfficeServiceRequests
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ServiceRequestId == requestId, ct)
            ?? throw new KeyNotFoundException("SERVICE_REQUEST_NOT_FOUND");

    private async Task<string> NextNumberAsync(Guid propertyId, DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"SRV-{nowUtc:yyyyMMdd}-{Uuid7.NewGuid():N}"[..25].ToUpperInvariant();
            if (!await db.FrontOfficeServiceRequests.AnyAsync(
                    x => x.PropertyId == propertyId && x.RequestNumber == candidate, ct))
                return candidate;
        }

        throw new InvalidOperationException("SERVICE_REQUEST_NUMBER_GENERATION_FAILED");
    }

    private void StageAudit(string action, ServiceRequest request, Guid propertyId, string? reason = null)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(ServiceRequest),
            request.ServiceRequestId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["RequestNumber"] = request.RequestNumber,
                ["Status"] = request.Status.ToString(),
                ["Priority"] = request.Priority.ToString()
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private void AddPendingHistory(ServiceRequest request)
    {
        var history = request.History.LastOrDefault();
        if (history is not null && db.Entry(history).State == EntityState.Detached)
            db.FrontOfficeServiceHistories.Add(history);
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static ServiceCategory ToDomainCategory(ServiceType type)
        => type switch
        {
            ServiceType.WakeUpCall or ServiceType.Luggage or ServiceType.BellService => ServiceCategory.FrontOffice,
            ServiceType.AirportTransfer or ServiceType.Taxi or ServiceType.Limousine => ServiceCategory.Transportation,
            ServiceType.RestaurantBooking or ServiceType.SpaBooking => ServiceCategory.FoodBeverage,
            _ => ServiceCategory.Custom
        };

    private static ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority ToDomainPriority(AppServicePriority priority)
        => priority switch
        {
            AppServicePriority.High => ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.High,
            AppServicePriority.Urgent => ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Urgent,
            AppServicePriority.Vip => ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Emergency,
            _ => ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Normal
        };

    private static ChargeType ToDomainChargeType(string? chargeType)
        => chargeType switch
        {
            "ImmediateCharge" => ChargeType.ImmediateCharge,
            "Complimentary" => ChargeType.Complimentary,
            _ => ChargeType.ChargeAtCheckout
        };

    private ServiceRequestDto Map(ServiceRequest request)
    {
        var type = Enum.TryParse<ServiceType>(request.ServiceType, ignoreCase: true, out var parsedType)
            ? parsedType
            : ServiceType.Other;
        var priority = request.Priority switch
        {
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.High => AppServicePriority.High,
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Urgent => AppServicePriority.Urgent,
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Emergency => AppServicePriority.Vip,
            _ => AppServicePriority.Normal
        };
        var status = request.Status switch
        {
            ServiceRequestStatus.Assigned => ServiceStatus.Assigned,
            ServiceRequestStatus.Accepted => ServiceStatus.Accepted,
            ServiceRequestStatus.InProgress => ServiceStatus.InProgress,
            ServiceRequestStatus.Completed => ServiceStatus.Completed,
            ServiceRequestStatus.Cancelled => ServiceStatus.Cancelled,
            _ => ServiceStatus.Requested
        };

        return new ServiceRequestDto(
            request.ServiceRequestId,
            request.RequestNumber,
            null,
            request.GuestName,
            request.HotelStayId,
            request.RoomNumber,
            type,
            status,
            priority,
            request.Description ?? string.Empty,
            request.AssignedStaffId,
            null,
            request.RequestedAtUtc,
            request.CompletedAtUtc,
            ResolveSlaDeadline(request),
            ResolveSlaStatus(request),
            priority == AppServicePriority.Vip,
            request.Category.ToString(),
            request.EstimatedCost,
            request.ActualCost,
            request.ChargeType.ToString(),
            request.AssignedDepartment);
    }

    private DateTime ResolveSlaDeadline(ServiceRequest request)
        => request.SlaDeadlineAtUtc ?? request.RequestedAtUtc.Add(GetDefaultSlaWindow(request.Priority));

    private SlaStatus ResolveSlaStatus(ServiceRequest request)
    {
        var deadline = ResolveSlaDeadline(request);
        if (request.Status == ServiceRequestStatus.Completed)
            return request.CompletedAtUtc <= deadline ? SlaStatus.OnTrack : SlaStatus.Breached;
        if (clock.UtcNow > deadline) return SlaStatus.Breached;

        var totalWindow = deadline - request.RequestedAtUtc;
        return deadline - clock.UtcNow <= TimeSpan.FromTicks(Math.Max(totalWindow.Ticks / 5, TimeSpan.TicksPerMinute))
            ? SlaStatus.AtRisk
            : SlaStatus.OnTrack;
    }

    private static DateTime? ResolveFirstResponse(
        ServiceRequest request,
        IReadOnlyDictionary<Guid, List<ServiceHistory>> historyByRequest)
    {
        if (request.FirstResponseAtUtc.HasValue) return request.FirstResponseAtUtc;
        return historyByRequest.TryGetValue(request.ServiceRequestId, out var entries)
            ? entries.FirstOrDefault(x => x.Action is "Assigned" or "Accepted" or "InProgress")?.OccurredAtUtc
            : null;
    }

    private static TimeSpan GetDefaultSlaWindow(AppServicePriority priority)
        => priority switch
        {
            AppServicePriority.Vip => TimeSpan.FromMinutes(15),
            AppServicePriority.Urgent => TimeSpan.FromMinutes(30),
            AppServicePriority.High => TimeSpan.FromHours(1),
            AppServicePriority.Normal => TimeSpan.FromHours(2),
            _ => TimeSpan.FromHours(4)
        };

    private static TimeSpan GetDefaultSlaWindow(ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority priority)
        => priority switch
        {
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Emergency => TimeSpan.FromMinutes(15),
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Urgent => TimeSpan.FromMinutes(30),
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.High => TimeSpan.FromHours(1),
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Normal => TimeSpan.FromHours(2),
            _ => TimeSpan.FromHours(4)
        };
}
