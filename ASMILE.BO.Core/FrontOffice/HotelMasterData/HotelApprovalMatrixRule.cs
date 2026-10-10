using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped approval threshold and segregation-of-duties rule.</summary>
public sealed class HotelApprovalMatrixRule
{
    private HotelApprovalMatrixRule() { }
    public Guid HotelApprovalMatrixRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public decimal ThresholdAmount { get; private set; }
    public int MinimumApprovers { get; private set; }
    public string RequiredRole { get; private set; } = string.Empty;
    public bool RequireDifferentApprover { get; private set; }
    public bool RequireReason { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelApprovalMatrixRule Create(PropertyId propertyId, string code, string name, string domain, string action, DateTime from, DateTime to, decimal threshold, int minimumApprovers, string requiredRole, bool differentApprover, bool requireReason, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, action, from, to, threshold, minimumApprovers, requiredRole); return new HotelApprovalMatrixRule { HotelApprovalMatrixRuleId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Domain = domain.Trim(), Action = action.Trim(), EffectiveFrom = from.Date, EffectiveTo = to.Date, ThresholdAmount = threshold, MinimumApprovers = minimumApprovers, RequiredRole = requiredRole.Trim(), RequireDifferentApprover = differentApprover, RequireReason = requireReason, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string domain, string action, DateTime from, DateTime to, decimal threshold, int minimumApprovers, string requiredRole, bool differentApprover, bool requireReason, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, domain, action, from, to, threshold, minimumApprovers, requiredRole); Code = code.Trim(); Name = name.Trim(); Domain = domain.Trim(); Action = action.Trim(); EffectiveFrom = from.Date; EffectiveTo = to.Date; ThresholdAmount = threshold; MinimumApprovers = minimumApprovers; RequiredRole = requiredRole.Trim(); RequireDifferentApprover = differentApprover; RequireReason = requireReason; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string domain, string action, DateTime from, DateTime to, decimal threshold, int approvers, string role) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_APPROVAL_MATRIX_RULE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(role)) throw new ArgumentException("HOTEL_APPROVAL_MATRIX_RULE_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_APPROVAL_MATRIX_RULE_EFFECTIVE_RANGE_INVALID"); if (threshold < 0 || approvers < 1 || approvers > 20) throw new ArgumentException("HOTEL_APPROVAL_MATRIX_RULE_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
