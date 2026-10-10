using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped night-audit execution policy.</summary>
public sealed class HotelNightAuditPolicy
{
    private HotelNightAuditPolicy() { }
    public Guid HotelNightAuditPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public bool LockBusinessDateDuringAudit { get; private set; }
    public bool RequireAllValidationsPassed { get; private set; }
    public bool AllowResumeAfterFailure { get; private set; }
    public bool RequireSupervisorApproval { get; private set; }
    public int MaximumRetries { get; private set; }
    public bool PostRoomCharges { get; private set; }
    public bool CloseEligibleFolios { get; private set; }
    public bool AdvanceBusinessDateOnSuccess { get; private set; }
    public string? SupervisorRole { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelNightAuditPolicy Create(PropertyId propertyId, string code, string name, DateTime from, DateTime to, bool lockBusinessDate, bool requireValidations, bool allowResume, bool requireApproval, int maximumRetries, bool postRoomCharges, bool closeEligibleFolios, bool advanceBusinessDate, string? supervisorRole, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, from, to, maximumRetries); return new HotelNightAuditPolicy { HotelNightAuditPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = from.Date, EffectiveTo = to.Date, LockBusinessDateDuringAudit = lockBusinessDate, RequireAllValidationsPassed = requireValidations, AllowResumeAfterFailure = allowResume, RequireSupervisorApproval = requireApproval, MaximumRetries = maximumRetries, PostRoomCharges = postRoomCharges, CloseEligibleFolios = closeEligibleFolios, AdvanceBusinessDateOnSuccess = advanceBusinessDate, SupervisorRole = Normalize(supervisorRole), Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, DateTime from, DateTime to, bool lockBusinessDate, bool requireValidations, bool allowResume, bool requireApproval, int maximumRetries, bool postRoomCharges, bool closeEligibleFolios, bool advanceBusinessDate, string? supervisorRole, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, from, to, maximumRetries); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = from.Date; EffectiveTo = to.Date; LockBusinessDateDuringAudit = lockBusinessDate; RequireAllValidationsPassed = requireValidations; AllowResumeAfterFailure = allowResume; RequireSupervisorApproval = requireApproval; MaximumRetries = maximumRetries; PostRoomCharges = postRoomCharges; CloseEligibleFolios = closeEligibleFolios; AdvanceBusinessDateOnSuccess = advanceBusinessDate; SupervisorRole = Normalize(supervisorRole); Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, int retries) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_NIGHT_AUDIT_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_NIGHT_AUDIT_POLICY_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_NIGHT_AUDIT_POLICY_EFFECTIVE_RANGE_INVALID"); if (retries < 0 || retries > 20) throw new ArgumentException("HOTEL_NIGHT_AUDIT_POLICY_RETRIES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
