using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Incident;

public sealed class EfIncidentService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IIncidentQueryService, IIncidentCommandService
{
    public async Task<IReadOnlyList<IncidentDto>> SearchAsync(IncidentSearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        EnsureProperty(request.PropertyId, context);

        var query = db.FrontOfficeIncidents.AsNoTracking()
            .Where(x => x.PropertyId == request.PropertyId);
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var text = request.SearchText.Trim();
            query = query.Where(x => x.IncidentNumber.Contains(text) || x.Title.Contains(text) ||
                (x.RoomReference != null && x.RoomReference.Contains(text)) ||
                (x.GuestReference != null && x.GuestReference.Contains(text)));
        }
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        if (request.Priority is IncidentPriority priority) query = query.Where(x => x.Priority == priority);
        if (request.Status is IncidentStatus status) query = query.Where(x => x.Status == status);
        if (request.FromUtc is DateTime from) query = query.Where(x => x.OccurredAtUtc >= from);
        if (request.ToUtc is DateTime to) query = query.Where(x => x.OccurredAtUtc < to);

        var rows = await query.OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Priority)
            .Take(5000)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IncidentDto> CreateAsync(CreateIncidentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var now = clock.UtcNow;
        var incident = FrontOfficeIncident.Create(
            propertyId,
            await NextNumberAsync(now, ct),
            request.OccurredAtUtc,
            request.Title,
            request.Category,
            request.Priority,
            await ReporterNameAsync(context.Actor.ActorId?.Value, ct),
            request.Impact,
            request.Description,
            now,
            context.Actor.ActorId,
            request.RoomReference,
            request.GuestReference,
            request.ReservationReference,
            request.Department,
            request.IsVip);

        db.FrontOfficeIncidents.Add(incident);
        StageAudit("IncidentCreated", incident, propertyId);
        await CommitAsync(ct, "INCIDENT_CREATE_FAILED");
        return Map(incident);
    }

    public async Task<IncidentDto> ChangeStatusAsync(ChangeIncidentStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var incident = await db.FrontOfficeIncidents.SingleOrDefaultAsync(
            x => x.PropertyId == propertyId && x.IncidentId == request.IncidentId, ct)
            ?? throw new KeyNotFoundException("INCIDENT_NOT_FOUND");

        if (request.ExpectedRowVersion is { Length: > 0 })
            db.Entry(incident).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;

        var now = clock.UtcNow;
        switch (request.Action)
        {
            case IncidentStatusAction.Assign:
                incident.Assign(request.Assignee ?? string.Empty, now, context.Actor.ActorId);
                break;
            case IncidentStatusAction.StartInvestigation:
                incident.StartInvestigation(now, context.Actor.ActorId);
                break;
            case IncidentStatusAction.SetPending:
                incident.SetPending(request.Reason ?? string.Empty, now, context.Actor.ActorId);
                break;
            case IncidentStatusAction.Resolve:
                incident.Resolve(request.RootCause ?? string.Empty, request.CorrectiveAction ?? string.Empty, now, context.Actor.ActorId);
                break;
            case IncidentStatusAction.Close:
                incident.Close(request.PreventiveAction ?? string.Empty, now, context.Actor.ActorId);
                break;
            case IncidentStatusAction.Cancel:
                incident.Cancel(request.Reason ?? string.Empty, now, context.Actor.ActorId);
                break;
            default:
                throw new InvalidOperationException("INCIDENT_ACTION_NOT_SUPPORTED");
        }

        StageAudit("IncidentStatusChanged", incident, propertyId, request.Action.ToString());
        await CommitAsync(ct, "INCIDENT_STATUS_CHANGE_FAILED");
        return Map(incident);
    }

    private async Task<string> NextNumberAsync(DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"INC-{nowUtc:yyyyMMdd}-{Uuid7.NewGuid():N}"[..25].ToUpperInvariant();
            if (!await db.FrontOfficeIncidents.AnyAsync(x => x.PropertyId == RequireContext().Property.PropertyId.Value && x.IncidentNumber == candidate, ct))
                return candidate;
        }
        throw new InvalidOperationException("INCIDENT_NUMBER_GENERATION_FAILED");
    }

    private async Task<string> ReporterNameAsync(Guid? actorId, CancellationToken ct)
    {
        if (actorId is Guid id)
        {
            var displayName = await db.AppUsers.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.DisplayName)
                .SingleOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
            return id.ToString("D");
        }
        return "System";
    }

    private void StageAudit(string action, FrontOfficeIncident incident, Guid propertyId, string? reason = null)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(FrontOfficeIncident),
            incident.IncidentId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["IncidentNumber"] = incident.IncidentNumber,
                ["Status"] = incident.Status.ToString(),
                ["Priority"] = incident.Priority.ToString()
            }));
    }

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static IncidentDto Map(FrontOfficeIncident incident) => new(
        incident.IncidentId,
        incident.PropertyId,
        incident.IncidentNumber,
        incident.OccurredAtUtc,
        incident.Title,
        incident.Category,
        incident.Priority,
        incident.RoomReference,
        incident.GuestReference,
        incident.ReservationReference,
        incident.Department,
        incident.ReporterName,
        incident.AssignedTo,
        incident.Status,
        incident.Impact,
        incident.Description,
        incident.RootCause,
        incident.TemporaryAction,
        incident.CorrectiveAction,
        incident.PreventiveAction,
        incident.IsVip,
        incident.CreatedAtUtc,
        incident.UpdatedAtUtc,
        incident.ResolvedAtUtc,
        incident.ClosedAtUtc,
        incident.RowVersion);
}
