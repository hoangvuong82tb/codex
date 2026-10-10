using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.LostFound;

public enum LostFoundStatus { Found, Stored, Matched, Claimed, Returned, Disposed, Archived, Contacted }
public enum LostFoundCategory { Electronics, Jewelry, Documents, Cash, Clothing, Luggage, Keys, Medical, Others }

public sealed class LostFoundItem
{
    private readonly List<LostFoundHistory> _history = [];
    private LostFoundItem() { }

    public Guid LostFoundItemId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ItemNumber { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public LostFoundCategory Category { get; private set; }
    public string? Description { get; private set; }
    public string? Color { get; private set; }
    public string? Brand { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? FoundLocation { get; private set; }
    public DateTime FoundDateTime { get; private set; }
    public string? FoundByStaff { get; private set; }
    public string? StoredLocation { get; private set; }
    public LostFoundStatus Status { get; private set; }
    public Guid? OwnerGuestId { get; private set; }
    public string? OwnerGuestName { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid? HotelStayId { get; private set; }
    public DateTime? ClaimedDate { get; private set; }
    public DateTime? ContactDate { get; private set; }
    public string? ContactedBy { get; private set; }
    public string? ContactMethod { get; private set; }
    public string? ContactResult { get; private set; }
    public string? ContactNotes { get; private set; }
    public string? ReleasedBy { get; private set; }
    public string? ReleasedTo { get; private set; }
    public DateTime? DisposalDate { get; private set; }
    public string? PhotoPath { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<LostFoundHistory> History => _history;

    public static LostFoundItem Register(Guid propertyId, string itemNumber, string itemName, LostFoundCategory category, string? description, string? foundLocation, DateTime foundDateTime, string? foundByStaff, string? storedLocation, string? color, string? brand, string? serialNumber, string? notes, DateTime nowUtc, ActorId? actorId) => new()
    { LostFoundItemId = Uuid7.NewGuid(), PropertyId = propertyId, ItemNumber = itemNumber, ItemName = itemName, Category = category, Description = description, Color = color, Brand = brand, SerialNumber = serialNumber, FoundLocation = foundLocation, FoundDateTime = foundDateTime, FoundByStaff = foundByStaff, StoredLocation = storedLocation, Status = LostFoundStatus.Found, Notes = notes, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void UpdateDetails(string itemName, LostFoundCategory category, string? description, string? color, string? brand, string? serialNumber, string? notes, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("Item name is required.", nameof(itemName));
        if (Status is LostFoundStatus.Returned or LostFoundStatus.Disposed or LostFoundStatus.Archived)
            throw new InvalidOperationException($"Item {ItemNumber} cannot be edited from status {Status}.");

        ItemName = itemName.Trim();
        Category = category;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, "Details updated", nowUtc));
    }

    public void Store(string location, DateTime nowUtc) { StoredLocation = location; Status = LostFoundStatus.Stored; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, $"Stored at {location}", nowUtc)); }
    public void Match(Guid? guestId, string? guestName, Guid? reservationId, Guid? stayId, DateTime nowUtc) { OwnerGuestId = guestId; OwnerGuestName = guestName; ReservationId = reservationId; HotelStayId = stayId; Status = LostFoundStatus.Matched; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, $"Matched to guest: {guestName}", nowUtc)); }
    public void Claim(DateTime nowUtc) { Status = LostFoundStatus.Claimed; ClaimedDate = nowUtc; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, "Claimed by guest", nowUtc)); }
    public void RecordContact(string contactedBy, DateTime contactDate, string contactMethod, string contactResult, string? contactNotes, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(contactedBy))
            throw new ArgumentException("Contact staff is required.", nameof(contactedBy));
        if (string.IsNullOrWhiteSpace(contactMethod))
            throw new ArgumentException("Contact method is required.", nameof(contactMethod));
        if (string.IsNullOrWhiteSpace(contactResult))
            throw new ArgumentException("Contact result is required.", nameof(contactResult));
        if (Status is LostFoundStatus.Returned or LostFoundStatus.Disposed or LostFoundStatus.Archived)
            throw new InvalidOperationException($"Item {ItemNumber} cannot be contacted from status {Status}.");

        ContactDate = contactDate == default ? nowUtc : contactDate;
        ContactedBy = contactedBy.Trim();
        ContactMethod = contactMethod.Trim();
        ContactResult = contactResult.Trim();
        ContactNotes = string.IsNullOrWhiteSpace(contactNotes) ? null : contactNotes.Trim();
        Status = LostFoundStatus.Contacted;
        _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, $"Contacted guest via {ContactMethod}: {ContactResult}", nowUtc));
    }
    public void ReturnItem(string releasedBy, string releasedTo, DateTime nowUtc) { Status = LostFoundStatus.Returned; ReleasedBy = releasedBy; ReleasedTo = releasedTo; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, $"Returned by {releasedBy} to {releasedTo}", nowUtc)); }
    public void Dispose(DateTime nowUtc, string? reason = null) { Status = LostFoundStatus.Disposed; DisposalDate = nowUtc; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, $"Disposed: {reason ?? "Policy"}", nowUtc)); }
    public void Archive(DateTime nowUtc) { Status = LostFoundStatus.Archived; _history.Add(LostFoundHistory.Record(LostFoundItemId, PropertyId, "Archived", nowUtc)); }
}

public sealed class LostFoundHistory
{
    private LostFoundHistory() { }
    public Guid LostFoundHistoryId { get; private set; } public Guid LostFoundItemId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static LostFoundHistory Record(Guid itemId, Guid propertyId, string action, DateTime nowUtc) => new() { LostFoundHistoryId = Uuid7.NewGuid(), LostFoundItemId = itemId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
