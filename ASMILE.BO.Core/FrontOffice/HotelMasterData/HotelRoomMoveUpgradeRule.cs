using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped policy for room moves and room class changes.</summary>
public sealed class HotelRoomMoveUpgradeRule
{
    private HotelRoomMoveUpgradeRule() { }
    public Guid HotelRoomMoveUpgradeRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public bool AllowRoomMove { get; private set; }
    public bool AllowUpgrade { get; private set; }
    public bool AllowDowngrade { get; private set; }
    public bool RequireApproval { get; private set; }
    public string? ApprovalRole { get; private set; }
    public string? PriceDifferenceMode { get; private set; }
    public int MaxClassChangeLevels { get; private set; }
    public bool RequireHousekeepingReady { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelRoomMoveUpgradeRule Create(PropertyId propertyId, string code, string name, DateTime effectiveFrom, DateTime effectiveTo, bool allowMove, bool allowUpgrade, bool allowDowngrade, bool requireApproval, string? approvalRole, string? priceDifferenceMode, int maxClassChangeLevels, bool requireHousekeepingReady, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, maxClassChangeLevels); return new HotelRoomMoveUpgradeRule { HotelRoomMoveUpgradeRuleId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, AllowRoomMove = allowMove, AllowUpgrade = allowUpgrade, AllowDowngrade = allowDowngrade, RequireApproval = requireApproval, ApprovalRole = Normalize(approvalRole), PriceDifferenceMode = Normalize(priceDifferenceMode), MaxClassChangeLevels = maxClassChangeLevels, RequireHousekeepingReady = requireHousekeepingReady, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, DateTime effectiveFrom, DateTime effectiveTo, bool allowMove, bool allowUpgrade, bool allowDowngrade, bool requireApproval, string? approvalRole, string? priceDifferenceMode, int maxClassChangeLevels, bool requireHousekeepingReady, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, maxClassChangeLevels); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; AllowRoomMove = allowMove; AllowUpgrade = allowUpgrade; AllowDowngrade = allowDowngrade; RequireApproval = requireApproval; ApprovalRole = Normalize(approvalRole); PriceDifferenceMode = Normalize(priceDifferenceMode); MaxClassChangeLevels = maxClassChangeLevels; RequireHousekeepingReady = requireHousekeepingReady; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, int maxLevels) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_ROOM_MOVE_RULE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_ROOM_MOVE_RULE_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_ROOM_MOVE_RULE_EFFECTIVE_RANGE_INVALID"); if (maxLevels < 0) throw new ArgumentException("HOTEL_ROOM_MOVE_RULE_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
