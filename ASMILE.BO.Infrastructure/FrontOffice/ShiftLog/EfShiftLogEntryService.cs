using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.ShiftLog;
using ASMILE.BO.Core.FrontOffice.ShiftLog;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.ShiftLog;

public sealed class EfShiftLogEntryService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IShiftLogEntryQueryService, IShiftLogEntryCommandService
{
    public async Task<IReadOnlyList<ShiftLogEntryDto>> SearchAsync(ShiftLogEntrySearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        EnsureProperty(request.PropertyId, context);

        var query = db.FrontOfficeShiftLogEntries.AsNoTracking()
            .Where(x => x.PropertyId == request.PropertyId);
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var text = request.SearchText.Trim();
            query = query.Where(x => x.Title.Contains(text) || x.Description.Contains(text) ||
                (x.RoomReference != null && x.RoomReference.Contains(text)) ||
                (x.GuestReference != null && x.GuestReference.Contains(text)));
        }
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        if (request.Status is ShiftLogEntryStatus status) query = query.Where(x => x.Status == status);
        if (request.FromUtc is DateTime from) query = query.Where(x => x.OccurredAtUtc >= from);
        if (request.ToUtc is DateTime to) query = query.Where(x => x.OccurredAtUtc < to);

        var rows = await query.OrderByDescending(x => x.OccurredAtUtc).Take(5000).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<ShiftLogEntryDto> CreateAsync(CreateShiftLogEntryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var now = clock.UtcNow;
        var entry = FrontOfficeShiftLogEntry.Create(
            propertyId, request.OccurredAtUtc, request.ShiftName, request.Title, request.Category,
            request.Priority, request.Description, now, context.Actor.ActorId, request.RoomReference,
            request.GuestReference, request.ReservationReference, request.AssignedTo, request.IsVip);

        db.FrontOfficeShiftLogEntries.Add(entry);
        StageAudit("ShiftLogEntryCreated", entry, propertyId);
        await CommitAsync(ct, "SHIFT_LOG_CREATE_FAILED");
        return Map(entry);
    }

    public async Task<ShiftLogEntryDto> ChangeStatusAsync(ChangeShiftLogEntryStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var entry = await db.FrontOfficeShiftLogEntries.SingleOrDefaultAsync(
            x => x.PropertyId == propertyId && x.ShiftLogEntryId == request.ShiftLogEntryId, ct)
            ?? throw new KeyNotFoundException("SHIFT_LOG_ENTRY_NOT_FOUND");
        if (request.ExpectedRowVersion is { Length: > 0 })
            db.Entry(entry).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;

        entry.ChangeStatus(request.Status, clock.UtcNow, context.Actor.ActorId);
        StageAudit("ShiftLogEntryStatusChanged", entry, propertyId, request.Status.ToString());
        await CommitAsync(ct, "SHIFT_LOG_STATUS_CHANGE_FAILED");
        return Map(entry);
    }

    private void StageAudit(string action, FrontOfficeShiftLogEntry entry, Guid propertyId, string? reason = null)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action, nameof(FrontOfficeShiftLogEntry), entry.ShiftLogEntryId.ToString("D"), "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["Category"] = entry.Category,
                ["Status"] = entry.Status.ToString()
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure) throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static ShiftLogEntryDto Map(FrontOfficeShiftLogEntry entry) => new(
        entry.ShiftLogEntryId, entry.PropertyId, entry.OccurredAtUtc, entry.ShiftName, entry.Title,
        entry.Category, entry.Priority, entry.RoomReference, entry.GuestReference,
        entry.ReservationReference, entry.AssignedTo, entry.Description, entry.IsVip, entry.Status,
        entry.CreatedAtUtc, entry.UpdatedAtUtc, entry.RowVersion);
}
