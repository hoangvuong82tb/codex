using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelTaxServiceChargeKind
{
    Tax = 1,
    ServiceCharge = 2
}

public sealed class HotelTaxServiceChargeRule
{
    private HotelTaxServiceChargeRule() { }

    public Guid HotelTaxServiceChargeRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public HotelTaxServiceChargeKind Kind { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsInclusive { get; private set; }
    public string ApplicableService { get; private set; } = string.Empty;
    public string? ApplicableRatePlan { get; private set; }
    public string RoundingMode { get; private set; } = "NearestDong";
    public string? AccountingMapping { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelTaxServiceChargeRule Create(PropertyId propertyId, HotelTaxServiceChargeKind kind, string code, string name,
        decimal rate, bool isInclusive, string applicableService, string? applicableRatePlan, string roundingMode,
        string? accountingMapping, DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(kind, code, name, rate, applicableService, roundingMode, effectiveFrom, effectiveTo);
        return new()
        {
            HotelTaxServiceChargeRuleId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Kind = kind,
            Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), Rate = rate, IsInclusive = isInclusive,
            ApplicableService = applicableService.Trim(), ApplicableRatePlan = Normalize(applicableRatePlan),
            RoundingMode = roundingMode.Trim(), AccountingMapping = Normalize(accountingMapping),
            EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, Description = Normalize(description),
            IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(HotelTaxServiceChargeKind kind, string code, string name, decimal rate, bool isInclusive,
        string applicableService, string? applicableRatePlan, string roundingMode, string? accountingMapping,
        DateTime effectiveFrom, DateTime effectiveTo, string? description, DateTime nowUtc, ActorId? actorId)
    {
        Validate(kind, code, name, rate, applicableService, roundingMode, effectiveFrom, effectiveTo);
        Kind = kind; Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); Rate = rate; IsInclusive = isInclusive;
        ApplicableService = applicableService.Trim(); ApplicableRatePlan = Normalize(applicableRatePlan); RoundingMode = roundingMode.Trim();
        AccountingMapping = Normalize(accountingMapping); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date;
        Description = Normalize(description); Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }

    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(HotelTaxServiceChargeKind kind, string code, string name, decimal rate, string service, string rounding, DateTime from, DateTime to)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_KIND_INVALID");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(service)) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_FIELDS_REQUIRED");
        if (rate < 0 || rate > 100) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_RATE_INVALID");
        if (string.IsNullOrWhiteSpace(rounding)) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_ROUNDING_REQUIRED");
        if (to.Date < from.Date) throw new ArgumentException("HOTEL_TAX_SERVICE_CHARGE_EFFECTIVE_RANGE_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
