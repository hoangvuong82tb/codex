using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Marketing;

public enum MarketingSection
{
    Source,
    Segment,
    Agency,
    Member,
    Promotion
}

public enum MarketingRecordStatus
{
    Active,
    Paused,
    Ended
}

public sealed class MarketingRecord
{
    private MarketingRecord() { }

    public Guid MarketingRecordId { get; private set; }
    public Guid PropertyId { get; private set; }
    public MarketingSection Section { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string Owner { get; private set; } = string.Empty;
    public decimal Revenue { get; private set; }
    public MarketingRecordStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static MarketingRecord Create(
        Guid propertyId,
        MarketingSection section,
        string code,
        string name,
        string groupName,
        string owner,
        string? notes,
        DateTime nowUtc,
        ActorId? actorId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        var record = new MarketingRecord
        {
            MarketingRecordId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            Section = section,
            Status = MarketingRecordStatus.Active,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        record.UpdateDetails(code, name, groupName, owner, notes, nowUtc, actorId);
        return record;
    }

    public void UpdateDetails(
        string code,
        string name,
        string groupName,
        string owner,
        string? notes,
        DateTime nowUtc,
        ActorId? actorId)
    {
        Code = Required(code, 60, nameof(code));
        Name = Required(name, 200, nameof(name));
        GroupName = Required(groupName, 120, nameof(groupName));
        Owner = Required(owner, 160, nameof(owner));
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    public void ChangeStatus(MarketingRecordStatus status, DateTime nowUtc, ActorId? actorId)
    {
        Status = status;
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static string Required(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", name);
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", name);
        return trimmed;
    }
}
