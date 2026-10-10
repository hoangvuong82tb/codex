using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelDiscountType
{
    Percentage = 1,
    FixedAmount = 2,
    Complimentary = 3
}

public sealed class HotelDiscountPolicy
{
    private HotelDiscountPolicy() { }

    public Guid HotelDiscountPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelDiscountType DiscountType { get; private set; }
    public decimal Percentage { get; private set; }
    public decimal FixedAmount { get; private set; }
    public decimal MaximumDiscount { get; private set; }
    public string AllowedRoles { get; private set; } = string.Empty;
    public int ApprovalLevel { get; private set; }
    public bool ReasonRequired { get; private set; }
    public string? AccountingTreatment { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelDiscountPolicy Create(PropertyId propertyId, string code, string name, HotelDiscountType discountType, decimal percentage, decimal fixedAmount,
        decimal maximumDiscount, string allowedRoles, int approvalLevel, bool reasonRequired, string? accountingTreatment, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, discountType, percentage, fixedAmount, maximumDiscount, allowedRoles, approvalLevel, effectiveFrom, effectiveTo);
        return new()
        {
            HotelDiscountPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), DiscountType = discountType,
            Percentage = percentage, FixedAmount = fixedAmount, MaximumDiscount = maximumDiscount, AllowedRoles = allowedRoles.Trim(), ApprovalLevel = approvalLevel, ReasonRequired = reasonRequired,
            AccountingTreatment = Normalize(accountingTreatment), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, Description = Normalize(description), IsActive = true,
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, HotelDiscountType discountType, decimal percentage, decimal fixedAmount, decimal maximumDiscount, string allowedRoles, int approvalLevel, bool reasonRequired,
        string? accountingTreatment, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, discountType, percentage, fixedAmount, maximumDiscount, allowedRoles, approvalLevel, effectiveFrom, effectiveTo);
        Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); DiscountType = discountType; Percentage = percentage; FixedAmount = fixedAmount; MaximumDiscount = maximumDiscount; AllowedRoles = allowedRoles.Trim(); ApprovalLevel = approvalLevel; ReasonRequired = reasonRequired; AccountingTreatment = Normalize(accountingTreatment); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; Description = Normalize(description); Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, HotelDiscountType type, decimal percentage, decimal fixedAmount, decimal maximumDiscount, string roles, int approvalLevel, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_DISCOUNT_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_DISCOUNT_NAME_REQUIRED");
        if (!Enum.IsDefined(type)) throw new ArgumentException("HOTEL_DISCOUNT_TYPE_INVALID");
        if (percentage is < 0 or > 100 || fixedAmount < 0 || maximumDiscount < 0) throw new ArgumentException("HOTEL_DISCOUNT_AMOUNT_INVALID");
        if (string.IsNullOrWhiteSpace(roles)) throw new ArgumentException("HOTEL_DISCOUNT_ALLOWED_ROLES_REQUIRED");
        if (approvalLevel < 0 || approvalLevel > 10) throw new ArgumentException("HOTEL_DISCOUNT_APPROVAL_LEVEL_INVALID");
        if (to.Date < from.Date) throw new ArgumentException("HOTEL_DISCOUNT_EFFECTIVE_RANGE_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
