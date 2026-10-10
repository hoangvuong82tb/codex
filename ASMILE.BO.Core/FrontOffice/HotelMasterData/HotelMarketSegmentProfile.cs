using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelMarketSegmentKind
{
    Fit = 1, Group = 2, Corporate = 3, Government = 4, Leisure = 5, Wholesale = 6, Contract = 7, Ota = 8, LongStay = 9, Crew = 10
}

public sealed class HotelMarketSegmentProfile
{
    private HotelMarketSegmentProfile() { }
    public Guid HotelMarketSegmentProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelMarketSegmentKind Kind { get; private set; }
    public string RevenueBucket { get; private set; } = string.Empty;
    public string? DefaultRateCode { get; private set; }
    public string? ForecastCategory { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelMarketSegmentProfile Create(PropertyId propertyId, string code, string name, HotelMarketSegmentKind kind, string revenueBucket, string? defaultRateCode, string? forecastCategory, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, revenueBucket, from, to); return new() { HotelMarketSegmentProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), Kind = kind, RevenueBucket = revenueBucket.Trim(), DefaultRateCode = NormalizeCode(defaultRateCode), ForecastCategory = Normalize(forecastCategory), EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, HotelMarketSegmentKind kind, string revenueBucket, string? defaultRateCode, string? forecastCategory, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, kind, revenueBucket, from, to); Code = CodeValue(code); Name = name.Trim(); Kind = kind; RevenueBucket = revenueBucket.Trim(); DefaultRateCode = NormalizeCode(defaultRateCode); ForecastCategory = Normalize(forecastCategory); EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, HotelMarketSegmentKind kind, string revenueBucket, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_MARKET_SEGMENT_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_MARKET_SEGMENT_NAME_REQUIRED"); if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_MARKET_SEGMENT_KIND_INVALID"); if (string.IsNullOrWhiteSpace(revenueBucket)) throw new ArgumentException("HOTEL_MARKET_SEGMENT_REVENUE_BUCKET_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_MARKET_SEGMENT_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
