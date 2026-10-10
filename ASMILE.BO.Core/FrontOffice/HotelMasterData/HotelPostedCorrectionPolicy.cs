using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped controls for correcting already-posted folio entries.</summary>
public sealed class HotelPostedCorrectionPolicy
{
    private HotelPostedCorrectionPolicy() { }
    public Guid HotelPostedCorrectionPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public decimal MaximumAmount { get; private set; }
    public bool RequireReason { get; private set; }
    public bool RequireApproval { get; private set; }
    public string AllowedRoles { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelPostedCorrectionPolicy Create(PropertyId propertyId, string code, string name, string domain, decimal maximumAmount, bool requireReason, bool requireApproval, string allowedRoles, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, allowedRoles, maximumAmount, from, to); return new HotelPostedCorrectionPolicy { HotelPostedCorrectionPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Domain = domain.Trim(), MaximumAmount = maximumAmount, RequireReason = requireReason, RequireApproval = requireApproval, AllowedRoles = allowedRoles.Trim(), EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string domain, decimal maximumAmount, bool requireReason, bool requireApproval, string allowedRoles, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, allowedRoles, maximumAmount, from, to); Code = code.Trim(); Name = name.Trim(); Domain = domain.Trim(); MaximumAmount = maximumAmount; RequireReason = requireReason; RequireApproval = requireApproval; AllowedRoles = allowedRoles.Trim(); EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string domain, string roles, decimal amount, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_POSTED_CORRECTION_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(roles)) throw new ArgumentException("HOTEL_POSTED_CORRECTION_POLICY_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_POSTED_CORRECTION_POLICY_EFFECTIVE_RANGE_INVALID"); if (amount < 0) throw new ArgumentException("HOTEL_POSTED_CORRECTION_POLICY_AMOUNT_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
