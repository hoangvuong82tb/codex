using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelDepositType
{
    Percentage = 1,
    FixedAmount = 2,
    FirstNight = 3,
    FullStay = 4,
    Manual = 5
}

public sealed class HotelDepositPolicy
{
    private HotelDepositPolicy() { }

    public Guid HotelDepositPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelDepositType DepositType { get; private set; }
    public bool Required { get; private set; }
    public decimal Percentage { get; private set; }
    public decimal FixedAmount { get; private set; }
    public int DueDaysBeforeArrival { get; private set; }
    public bool Refundable { get; private set; }
    public bool Transferable { get; private set; }
    public bool Forfeitable { get; private set; }
    public bool AutoReminder { get; private set; }
    public bool AutoApplyToFolio { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelDepositPolicy Create(PropertyId propertyId, string code, string name, HotelDepositType depositType, bool required,
        decimal percentage, decimal fixedAmount, int dueDaysBeforeArrival, bool refundable, bool transferable, bool forfeitable,
        bool autoReminder, bool autoApplyToFolio, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, depositType, percentage, fixedAmount, dueDaysBeforeArrival, effectiveFrom, effectiveTo);
        return new()
        {
            HotelDepositPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), DepositType = depositType, Required = required,
            Percentage = percentage, FixedAmount = fixedAmount, DueDaysBeforeArrival = dueDaysBeforeArrival, Refundable = refundable, Transferable = transferable, Forfeitable = forfeitable,
            AutoReminder = autoReminder, AutoApplyToFolio = autoApplyToFolio, EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, Description = Normalize(description),
            IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, HotelDepositType depositType, bool required, decimal percentage, decimal fixedAmount, int dueDaysBeforeArrival,
        bool refundable, bool transferable, bool forfeitable, bool autoReminder, bool autoApplyToFolio, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, depositType, percentage, fixedAmount, dueDaysBeforeArrival, effectiveFrom, effectiveTo);
        Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); DepositType = depositType; Required = required; Percentage = percentage; FixedAmount = fixedAmount; DueDaysBeforeArrival = dueDaysBeforeArrival;
        Refundable = refundable; Transferable = transferable; Forfeitable = forfeitable; AutoReminder = autoReminder; AutoApplyToFolio = autoApplyToFolio; EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; Description = Normalize(description); Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, HotelDepositType type, decimal percentage, decimal fixedAmount, int dueDays, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_DEPOSIT_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_DEPOSIT_NAME_REQUIRED");
        if (!Enum.IsDefined(type)) throw new ArgumentException("HOTEL_DEPOSIT_TYPE_INVALID");
        if (percentage is < 0 or > 100 || fixedAmount < 0) throw new ArgumentException("HOTEL_DEPOSIT_AMOUNT_INVALID");
        if (dueDays < 0 || dueDays > 3650) throw new ArgumentException("HOTEL_DEPOSIT_DUE_DAYS_INVALID");
        if (to.Date < from.Date) throw new ArgumentException("HOTEL_DEPOSIT_EFFECTIVE_RANGE_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
