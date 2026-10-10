using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped Front Office permission policy for module/action/data sensitivity.</summary>
public sealed class HotelFOPermissionPolicy
{
    private HotelFOPermissionPolicy() { }
    public Guid HotelFOPermissionPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string RequiredRole { get; private set; } = string.Empty;
    public string DataScope { get; private set; } = string.Empty;
    public bool IsSensitive { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelFOPermissionPolicy Create(PropertyId propertyId, string code, string name, string module, string action, string requiredRole, string dataScope, bool sensitive, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, module, action, requiredRole, dataScope, from, to); return new HotelFOPermissionPolicy { HotelFOPermissionPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Module = module.Trim(), Action = action.Trim(), RequiredRole = requiredRole.Trim(), DataScope = dataScope.Trim(), IsSensitive = sensitive, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string module, string action, string requiredRole, string dataScope, bool sensitive, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, module, action, requiredRole, dataScope, from, to); Code = code.Trim(); Name = name.Trim(); Module = module.Trim(); Action = action.Trim(); RequiredRole = requiredRole.Trim(); DataScope = dataScope.Trim(); IsSensitive = sensitive; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string module, string action, string role, string scope, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_FO_PERMISSION_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(module) || string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("HOTEL_FO_PERMISSION_POLICY_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_FO_PERMISSION_POLICY_EFFECTIVE_RANGE_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
