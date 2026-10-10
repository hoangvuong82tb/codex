using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped declarative business rule reference; executable code is never stored.</summary>
public sealed class HotelBusinessRuleProfile
{
    private HotelBusinessRuleProfile() { }
    public Guid HotelBusinessRuleProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public string ConditionReference { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsExclusive { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelBusinessRuleProfile Create(PropertyId propertyId, string code, string name, string domain, string conditionReference, int priority, bool blocking, bool exclusive, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, conditionReference, priority, from, to); return new HotelBusinessRuleProfile { HotelBusinessRuleProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Domain = domain.Trim(), ConditionReference = conditionReference.Trim(), Priority = priority, IsBlocking = blocking, IsExclusive = exclusive, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string domain, string conditionReference, int priority, bool blocking, bool exclusive, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, conditionReference, priority, from, to); Code = code.Trim(); Name = name.Trim(); Domain = domain.Trim(); ConditionReference = conditionReference.Trim(); Priority = priority; IsBlocking = blocking; IsExclusive = exclusive; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string domain, string condition, int priority, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_BUSINESS_RULE_PROFILE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(condition)) throw new ArgumentException("HOTEL_BUSINESS_RULE_PROFILE_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_BUSINESS_RULE_PROFILE_EFFECTIVE_RANGE_INVALID"); if (priority < 0 || priority > 10000) throw new ArgumentException("HOTEL_BUSINESS_RULE_PROFILE_PRIORITY_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
