using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>
/// Property-scoped guest preference master data. Runtime guest preferences keep
/// their values separately; this aggregate defines the reusable preference key,
/// validation metadata and operational defaults.
/// </summary>
public sealed class HotelGuestPreferenceProfile
{
    private HotelGuestPreferenceProfile() { }

    public Guid HotelGuestPreferenceProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string ValueKind { get; private set; } = string.Empty;
    public string? DefaultValue { get; private set; }
    public string? Options { get; private set; }
    public string Scope { get; private set; } = "Guest";
    public int Priority { get; private set; }
    public bool IsSafetyCritical { get; private set; }
    public bool IsChargeable { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelGuestPreferenceProfile Create(PropertyId propertyId, string code, string name, string category, string valueKind, string? defaultValue, string? options, string scope, int priority, bool safetyCritical, bool chargeable, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, category, valueKind, scope, priority, from, to);
        return new()
        {
            HotelGuestPreferenceProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value,
            Code = NormalizeCode(code), Name = name.Trim(), Category = category.Trim(), ValueKind = valueKind.Trim(),
            DefaultValue = Normalize(defaultValue), Options = Normalize(options), Scope = scope.Trim(), Priority = priority,
            IsSafetyCritical = safetyCritical, IsChargeable = chargeable, EffectiveFrom = from.Date, EffectiveTo = to.Date,
            Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value,
            UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, string category, string valueKind, string? defaultValue, string? options, string scope, int priority, bool safetyCritical, bool chargeable, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, category, valueKind, scope, priority, from, to);
        Code = NormalizeCode(code); Name = name.Trim(); Category = category.Trim(); ValueKind = valueKind.Trim();
        DefaultValue = Normalize(defaultValue); Options = Normalize(options); Scope = scope.Trim(); Priority = priority;
        IsSafetyCritical = safetyCritical; IsChargeable = chargeable; EffectiveFrom = from.Date; EffectiveTo = to.Date;
        Description = Normalize(description); Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }

    private static void Validate(string code, string name, string category, string valueKind, string scope, int priority, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_CATEGORY_REQUIRED");
        if (string.IsNullOrWhiteSpace(valueKind)) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_VALUE_KIND_REQUIRED");
        if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_SCOPE_REQUIRED");
        if (priority < 0) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_PRIORITY_INVALID");
        if (to.Date < from.Date) throw new ArgumentException("HOTEL_GUEST_PREFERENCE_EFFECTIVE_RANGE_INVALID");
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
