using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped cashier/shift control defaults.</summary>
public sealed class HotelCashierShiftPolicy
{
    private HotelCashierShiftPolicy() { }
    public Guid HotelCashierShiftPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public bool RequireOpeningFloat { get; private set; }
    public decimal DefaultOpeningFloat { get; private set; }
    public bool RequireSupervisorClose { get; private set; }
    public decimal VarianceApprovalThreshold { get; private set; }
    public bool AllowCashTransfer { get; private set; }
    public bool RequireHandoverNote { get; private set; }
    public string? SupervisorRole { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelCashierShiftPolicy Create(PropertyId propertyId, string code, string name, DateTime effectiveFrom, DateTime effectiveTo, bool requireOpeningFloat, decimal defaultOpeningFloat, bool requireSupervisorClose, decimal varianceApprovalThreshold, bool allowCashTransfer, bool requireHandoverNote, string? supervisorRole, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, defaultOpeningFloat, varianceApprovalThreshold); return new HotelCashierShiftPolicy { HotelCashierShiftPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, RequireOpeningFloat = requireOpeningFloat, DefaultOpeningFloat = defaultOpeningFloat, RequireSupervisorClose = requireSupervisorClose, VarianceApprovalThreshold = varianceApprovalThreshold, AllowCashTransfer = allowCashTransfer, RequireHandoverNote = requireHandoverNote, SupervisorRole = Normalize(supervisorRole), Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, DateTime effectiveFrom, DateTime effectiveTo, bool requireOpeningFloat, decimal defaultOpeningFloat, bool requireSupervisorClose, decimal varianceApprovalThreshold, bool allowCashTransfer, bool requireHandoverNote, string? supervisorRole, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, defaultOpeningFloat, varianceApprovalThreshold); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; RequireOpeningFloat = requireOpeningFloat; DefaultOpeningFloat = defaultOpeningFloat; RequireSupervisorClose = requireSupervisorClose; VarianceApprovalThreshold = varianceApprovalThreshold; AllowCashTransfer = allowCashTransfer; RequireHandoverNote = requireHandoverNote; SupervisorRole = Normalize(supervisorRole); Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, decimal openingFloat, decimal threshold) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_CASHIER_SHIFT_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_CASHIER_SHIFT_POLICY_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_CASHIER_SHIFT_POLICY_EFFECTIVE_RANGE_INVALID"); if (openingFloat < 0 || threshold < 0) throw new ArgumentException("HOTEL_CASHIER_SHIFT_POLICY_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
