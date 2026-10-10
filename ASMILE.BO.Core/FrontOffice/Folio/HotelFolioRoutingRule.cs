using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Folio;

/// <summary>
/// A persisted instruction for routing a posting from one Folio to another.
/// The rule is deliberately separate from the Folio ledger: changing a rule
/// must never rewrite historical entries.
/// </summary>
public sealed class HotelFolioRoutingRule
{
    private HotelFolioRoutingRule() { }

    public Guid HotelFolioRoutingRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid SourceFolioId { get; private set; }
    public Guid TargetFolioId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string EntryType { get; private set; } = "All";
    public decimal Percent { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelFolioRoutingRule Create(
        Guid propertyId,
        Guid sourceFolioId,
        Guid targetFolioId,
        string name,
        string entryType,
        decimal percent,
        int priority,
        string? notes,
        DateTime nowUtc,
        ActorId? actorId)
    {
        Validate(propertyId, sourceFolioId, targetFolioId, name, entryType, percent, priority);
        return new HotelFolioRoutingRule
        {
            HotelFolioRoutingRuleId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            SourceFolioId = sourceFolioId,
            TargetFolioId = targetFolioId,
            Name = name.Trim(),
            EntryType = NormalizeEntryType(entryType),
            Percent = percent,
            Priority = priority,
            IsActive = true,
            Notes = NormalizeNotes(notes),
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = actorId?.Value
        };
    }

    public void Update(string name, string entryType, decimal percent, int priority, string? notes, DateTime nowUtc, ActorId? actorId)
    {
        Validate(PropertyId, SourceFolioId, TargetFolioId, name, entryType, percent, priority);
        Name = name.Trim();
        EntryType = NormalizeEntryType(entryType);
        Percent = percent;
        Priority = priority;
        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    public void SetActive(bool active, DateTime nowUtc, ActorId? actorId)
    {
        IsActive = active;
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static void Validate(Guid propertyId, Guid sourceFolioId, Guid targetFolioId, string name, string entryType, decimal percent, int priority)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.");
        if (sourceFolioId == Guid.Empty || targetFolioId == Guid.Empty || sourceFolioId == targetFolioId)
            throw new ArgumentException("Source and target Folio must be different and valid.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new ArgumentException("Routing rule name is invalid.");
        if (string.IsNullOrWhiteSpace(entryType) || entryType.Trim().Length > 40) throw new ArgumentException("Routing entry type is invalid.");
        if (percent <= 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent), "Routing percent must be greater than zero and at most 100.");
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
    }

    private static string NormalizeEntryType(string value) => value.Trim();
    private static string? NormalizeNotes(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
