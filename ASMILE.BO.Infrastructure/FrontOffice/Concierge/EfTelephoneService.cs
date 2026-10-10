using AppConcierge = ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Concierge;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Concierge;

public sealed class EfTelephoneService(AppDbContext db, IClock clock, IFrontOfficeExecutionContextAccessor contextAccessor) : AppConcierge.ITelephoneService
{
    public async Task<IReadOnlyList<AppConcierge.TelephoneCallDto>> GetCallsAsync(Guid propertyId, AppConcierge.TelephoneCallDirection? direction, string? keyword, CancellationToken ct)
    {
        var query = db.ConciergeTelephoneCalls.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (direction is not null) query = query.Where(x => x.Direction == direction.Value.ToString());
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.PhoneNumber.Contains(value) || (x.GuestName != null && x.GuestName.Contains(value)) || (x.RoomNumber != null && x.RoomNumber.Contains(value)));
        }
        return await query.OrderByDescending(x => x.StartedAtUtc).Select(x => new AppConcierge.TelephoneCallDto(x.CallId, x.Direction == "Outgoing" ? AppConcierge.TelephoneCallDirection.Outgoing : AppConcierge.TelephoneCallDirection.Incoming, x.PhoneNumber, x.GuestName, x.RoomNumber, x.Subject, x.Notes, x.StartedAtUtc, x.DurationSeconds, x.Status, x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<AppConcierge.TelephoneCallDto> LogCallAsync(AppConcierge.TelephoneCallDirection direction, string phoneNumber, string? guestName, string? roomNumber, string? subject, string? notes, DateTime startedAtUtc, int durationSeconds, CancellationToken ct)
    {
        var propertyId = RequireProperty();
        var call = ConciergeTelephoneCall.Create(propertyId, direction.ToString(), phoneNumber, guestName, roomNumber, subject, notes, startedAtUtc, durationSeconds, "Completed", clock.UtcNow);
        db.ConciergeTelephoneCalls.Add(call);
        await db.SaveChangesAsync(ct);
        return Map(call);
    }

    public async Task<IReadOnlyList<AppConcierge.TelephoneContactDto>> GetContactsAsync(Guid propertyId, string? keyword, CancellationToken ct)
    {
        var query = db.ConciergeTelephoneContacts.AsNoTracking().Where(x => x.PropertyId == propertyId && x.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.DisplayName.Contains(value) || x.PhoneNumber.Contains(value) || (x.RoomNumber != null && x.RoomNumber.Contains(value)));
        }
        return await query.OrderBy(x => x.DisplayName).Select(x => new AppConcierge.TelephoneContactDto(x.ContactId, x.DisplayName, x.PhoneNumber, x.RoomNumber, x.Category, x.Notes, x.IsActive, x.UpdatedAtUtc)).ToListAsync(ct);
    }

    public async Task<AppConcierge.TelephoneContactDto> SaveContactAsync(Guid? contactId, string displayName, string phoneNumber, string? roomNumber, string? category, string? notes, CancellationToken ct)
    {
        var propertyId = RequireProperty();
        var contact = contactId is { } id
            ? await db.ConciergeTelephoneContacts.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ContactId == id, ct) ?? throw new KeyNotFoundException("TELEPHONE_CONTACT_NOT_FOUND")
            : null;
        if (contact is null)
        {
            contact = ConciergeTelephoneContact.Create(propertyId, displayName, phoneNumber, roomNumber, category, notes, clock.UtcNow);
            db.ConciergeTelephoneContacts.Add(contact);
        }
        else contact.Update(displayName, phoneNumber, roomNumber, category, notes, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task DeactivateContactAsync(Guid contactId, CancellationToken ct)
    {
        var propertyId = RequireProperty();
        var contact = await db.ConciergeTelephoneContacts.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ContactId == contactId, ct) ?? throw new KeyNotFoundException("TELEPHONE_CONTACT_NOT_FOUND");
        contact.Deactivate(clock.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    private Guid RequireProperty() => contextAccessor.Current?.Property.PropertyId.Value ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
    private static AppConcierge.TelephoneCallDto Map(ConciergeTelephoneCall x) => new(x.CallId, x.Direction == "Outgoing" ? AppConcierge.TelephoneCallDirection.Outgoing : AppConcierge.TelephoneCallDirection.Incoming, x.PhoneNumber, x.GuestName, x.RoomNumber, x.Subject, x.Notes, x.StartedAtUtc, x.DurationSeconds, x.Status, x.CreatedAtUtc);
    private static AppConcierge.TelephoneContactDto Map(ConciergeTelephoneContact x) => new(x.ContactId, x.DisplayName, x.PhoneNumber, x.RoomNumber, x.Category, x.Notes, x.IsActive, x.UpdatedAtUtc);
}
