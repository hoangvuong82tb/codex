using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Guest;

public enum VipLevel { Normal, Bronze, Silver, Gold, Platinum }
public enum GuestDocumentType { Passport, NationalId, DriverLicense, Visa }
public enum GuestSource { Direct, Corporate, TravelAgent, Referral, OTA }

public sealed class GuestProfile
{
    private readonly List<GuestDocument> _documents = [];
    private readonly List<GuestPreference> _preferences = [];
    private GuestProfile() { }

    public Guid GuestProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string GuestNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Nationality { get; private set; }
    public string? Country { get; private set; }
    public string? Gender { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? EmergencyContact { get; private set; }
    public string? EmergencyPhone { get; private set; }
    public VipLevel VipLevel { get; private set; }
    public int TotalStays { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public DateOnly? LastStayDate { get; private set; }
    public decimal AverageSpend { get; private set; }
    public bool IsBlacklisted { get; private set; }
    public string? BlacklistReason { get; private set; }
    public Guid? BlacklistedBy { get; private set; }
    public DateTime? BlacklistedAtUtc { get; private set; }
    public GuestSource Source { get; private set; }
    public string? CompanyName { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<GuestDocument> Documents => _documents;
    public IReadOnlyList<GuestPreference> Preferences => _preferences;

    public static GuestProfile Create(Guid propertyId, string guestNumber, string fullName, string? phone, string? email, GuestSource source, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("GUEST_NAME_REQUIRED");
        return new GuestProfile { GuestProfileId = Uuid7.NewGuid(), PropertyId = propertyId, GuestNumber = guestNumber, FullName = fullName.Trim(), Phone = phone?.Trim(), Email = email?.Trim(), Source = source, VipLevel = VipLevel.Normal, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }

    public void UpdateProfile(string? phone, string? email, string? address, string? nationality, string? gender, DateOnly? dob, DateTime nowUtc, ActorId? actorId) { Phone = phone?.Trim(); Email = email?.Trim(); Address = address?.Trim(); Nationality = nationality?.Trim(); Gender = gender; DateOfBirth = dob; Touch(nowUtc, actorId); }
    public void SetNotes(string? notes, DateTime nowUtc, ActorId? actorId) { Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(); Touch(nowUtc, actorId); }
    public void SetVip(VipLevel level, DateTime nowUtc, ActorId? actorId) { VipLevel = level; Touch(nowUtc, actorId); }
    public void AddDocument(GuestDocumentType type, string number, string? issuingCountry, DateOnly? expiry, DateTime nowUtc) { _documents.Add(GuestDocument.Create(GuestProfileId, PropertyId, type, number, issuingCountry, expiry, nowUtc)); Touch(nowUtc, null); }
    public void SetDocument(GuestDocumentType type, string? number, string? issuingCountry, DateOnly? expiry, DateTime nowUtc, ActorId? actorId)
    {
        var normalizedNumber = number?.Trim();
        var existing = _documents.FirstOrDefault(x => x.DocumentType == type);
        if (string.IsNullOrWhiteSpace(normalizedNumber))
        {
            if (existing is not null) _documents.Remove(existing);
        }
        else if (existing is null)
        {
            _documents.Add(GuestDocument.Create(GuestProfileId, PropertyId, type, normalizedNumber, issuingCountry, expiry, nowUtc));
        }
        else
        {
            existing.Update(normalizedNumber, issuingCountry, expiry);
        }
        Touch(nowUtc, actorId);
    }
    public void AddPreference(string key, string value) { var existing = _preferences.FirstOrDefault(p => p.Key == key); if (existing is not null) _preferences.Remove(existing); _preferences.Add(GuestPreference.Create(GuestProfileId, PropertyId, key, value)); }
    public void RecordStay(decimal revenue, DateOnly stayDate) { TotalStays++; TotalRevenue += revenue; LastStayDate = stayDate; AverageSpend = TotalStays > 0 ? TotalRevenue / TotalStays : 0; }
    public void Blacklist(string reason, DateTime nowUtc, ActorId? actorId) { IsBlacklisted = true; BlacklistReason = reason?.Trim(); BlacklistedBy = actorId?.Value; BlacklistedAtUtc = nowUtc; Touch(nowUtc, actorId); }
    public void RemoveBlacklist(DateTime nowUtc, ActorId? actorId) { IsBlacklisted = false; BlacklistReason = null; BlacklistedBy = null; BlacklistedAtUtc = null; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}

public sealed class GuestDocument
{
    private GuestDocument() { }
    public Guid GuestDocumentId { get; private set; }
    public Guid GuestProfileId { get; private set; }
    public GuestProfile? GuestProfile { get; private set; }
    public Guid PropertyId { get; private set; }
    public GuestDocumentType DocumentType { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string? IssuingCountry { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public static GuestDocument Create(Guid guestId, Guid propertyId, GuestDocumentType type, string number, string? country, DateOnly? expiry, DateTime nowUtc) => new() { GuestDocumentId = Uuid7.NewGuid(), GuestProfileId = guestId, PropertyId = propertyId, DocumentType = type, DocumentNumber = number, IssuingCountry = country, ExpiryDate = expiry, CreatedAtUtc = nowUtc };
    internal void Update(string number, string? country, DateOnly? expiry)
    {
        DocumentNumber = number;
        IssuingCountry = country?.Trim();
        ExpiryDate = expiry;
    }
}

public sealed class GuestPreference
{
    private GuestPreference() { }
    public Guid GuestPreferenceId { get; private set; }
    public Guid GuestProfileId { get; private set; }
    public GuestProfile? GuestProfile { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public static GuestPreference Create(Guid guestId, Guid propertyId, string key, string value) => new() { GuestPreferenceId = Uuid7.NewGuid(), GuestProfileId = guestId, PropertyId = propertyId, Key = key, Value = value };
}
