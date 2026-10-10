using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Concierge;

/// <summary>
/// Persisted front-office telephone records.  Calls and contacts are kept in
/// the property scope so the telephone hub never has to fall back to sample
/// rows when a property has no activity yet.
/// </summary>
public sealed class ConciergeTelephoneCall
{
    private ConciergeTelephoneCall() { }

    public Guid CallId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Direction { get; private set; } = "Incoming";
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? GuestName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? Subject { get; private set; }
    public string? Notes { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public string Status { get; private set; } = "Completed";
    public DateTime CreatedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ConciergeTelephoneCall Create(
        Guid propertyId,
        string direction,
        string phoneNumber,
        string? guestName,
        string? roomNumber,
        string? subject,
        string? notes,
        DateTime startedAtUtc,
        int durationSeconds,
        string status,
        DateTime nowUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (direction is not ("Incoming" or "Outgoing")) throw new ArgumentException("Call direction is invalid.", nameof(direction));
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        if (durationSeconds < 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Call status is required.", nameof(status));

        return new ConciergeTelephoneCall
        {
            CallId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            Direction = direction,
            PhoneNumber = phoneNumber.Trim(),
            GuestName = NullIfEmpty(guestName),
            RoomNumber = NullIfEmpty(roomNumber),
            Subject = NullIfEmpty(subject),
            Notes = NullIfEmpty(notes),
            StartedAtUtc = startedAtUtc,
            DurationSeconds = durationSeconds,
            Status = status.Trim(),
            CreatedAtUtc = nowUtc
        };
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ConciergeTelephoneContact
{
    private ConciergeTelephoneContact() { }

    public Guid ContactId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? RoomNumber { get; private set; }
    public string Category { get; private set; } = "Guest";
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ConciergeTelephoneContact Create(
        Guid propertyId,
        string displayName,
        string phoneNumber,
        string? roomNumber,
        string? category,
        string? notes,
        DateTime nowUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Contact name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        return new ConciergeTelephoneContact
        {
            ContactId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            DisplayName = displayName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            RoomNumber = NullIfEmpty(roomNumber),
            Category = string.IsNullOrWhiteSpace(category) ? "Guest" : category.Trim(),
            Notes = NullIfEmpty(notes),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            IsActive = true
        };
    }

    public void Update(string displayName, string phoneNumber, string? roomNumber, string? category, string? notes, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Contact name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        DisplayName = displayName.Trim();
        PhoneNumber = phoneNumber.Trim();
        RoomNumber = NullIfEmpty(roomNumber);
        Category = string.IsNullOrWhiteSpace(category) ? "Guest" : category.Trim();
        Notes = NullIfEmpty(notes);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
