using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped rule set for early check-in and late check-out decisions.</summary>
public sealed class HotelCheckInOutRule
{
    private HotelCheckInOutRule() { }
    public Guid HotelCheckInOutRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public int EarlyCheckInGraceMinutes { get; private set; }
    public int LateCheckOutGraceMinutes { get; private set; }
    public string? EarlyCheckInApprovalRole { get; private set; }
    public string? LateCheckOutApprovalRole { get; private set; }
    public string? EarlyCheckInChargeCode { get; private set; }
    public string? LateCheckOutChargeCode { get; private set; }
    public bool RequireGuestDocument { get; private set; }
    public bool RequirePaymentAuthorization { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelCheckInOutRule Create(PropertyId propertyId, string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int earlyGrace, int lateGrace, string? earlyRole, string? lateRole, string? earlyCharge, string? lateCharge, bool requireDocument, bool requirePayment, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, earlyGrace, lateGrace); return new HotelCheckInOutRule { HotelCheckInOutRuleId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, EarlyCheckInGraceMinutes = earlyGrace, LateCheckOutGraceMinutes = lateGrace, EarlyCheckInApprovalRole = Normalize(earlyRole), LateCheckOutApprovalRole = Normalize(lateRole), EarlyCheckInChargeCode = Normalize(earlyCharge), LateCheckOutChargeCode = Normalize(lateCharge), RequireGuestDocument = requireDocument, RequirePaymentAuthorization = requirePayment, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int earlyGrace, int lateGrace, string? earlyRole, string? lateRole, string? earlyCharge, string? lateCharge, bool requireDocument, bool requirePayment, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, earlyGrace, lateGrace); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; EarlyCheckInGraceMinutes = earlyGrace; LateCheckOutGraceMinutes = lateGrace; EarlyCheckInApprovalRole = Normalize(earlyRole); LateCheckOutApprovalRole = Normalize(lateRole); EarlyCheckInChargeCode = Normalize(earlyCharge); LateCheckOutChargeCode = Normalize(lateCharge); RequireGuestDocument = requireDocument; RequirePaymentAuthorization = requirePayment; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, int earlyGrace, int lateGrace) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_CHECKIN_OUT_RULE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_CHECKIN_OUT_RULE_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_CHECKIN_OUT_RULE_EFFECTIVE_RANGE_INVALID"); if (earlyGrace < 0 || lateGrace < 0) throw new ArgumentException("HOTEL_CHECKIN_OUT_RULE_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
