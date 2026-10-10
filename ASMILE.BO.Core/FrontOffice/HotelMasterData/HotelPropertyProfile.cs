using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped identity and operating calendar for Front Office.</summary>
public sealed class HotelPropertyProfile
{
    private HotelPropertyProfile() { }
    public Guid HotelPropertyProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? TaxCode { get; private set; }
    public string TimeZoneId { get; private set; } = "Asia/Ho_Chi_Minh";
    public string CurrencyCode { get; private set; } = "VND";
    public int CheckInMinutes { get; private set; }
    public int CheckOutMinutes { get; private set; }
    public DateTime BusinessDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelPropertyProfile Create(PropertyId propertyId, string code, string name, string? address, string? phone, string? email, string? taxCode, string timeZoneId, string currencyCode, int checkInMinutes, int checkOutMinutes, DateTime businessDate, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, timeZoneId, currencyCode, checkInMinutes, checkOutMinutes);
        return new HotelPropertyProfile { HotelPropertyProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Address = Normalize(address), Phone = Normalize(phone), Email = Normalize(email), TaxCode = Normalize(taxCode), TimeZoneId = timeZoneId.Trim(), CurrencyCode = currencyCode.Trim().ToUpperInvariant(), CheckInMinutes = checkInMinutes, CheckOutMinutes = checkOutMinutes, BusinessDate = businessDate.Date, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }

    public void Update(string code, string name, string? address, string? phone, string? email, string? taxCode, string timeZoneId, string currencyCode, int checkInMinutes, int checkOutMinutes, DateTime businessDate, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, timeZoneId, currencyCode, checkInMinutes, checkOutMinutes);
        Code = code.Trim(); Name = name.Trim(); Address = Normalize(address); Phone = Normalize(phone); Email = Normalize(email); TaxCode = Normalize(taxCode); TimeZoneId = timeZoneId.Trim(); CurrencyCode = currencyCode.Trim().ToUpperInvariant(); CheckInMinutes = checkInMinutes; CheckOutMinutes = checkOutMinutes; BusinessDate = businessDate.Date; Touch(nowUtc, actorId);
    }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string timeZoneId, string currencyCode, int checkIn, int checkOut)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_PROPERTY_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_PROPERTY_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(timeZoneId)) throw new ArgumentException("HOTEL_PROPERTY_TIMEZONE_REQUIRED");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3) throw new ArgumentException("HOTEL_PROPERTY_CURRENCY_INVALID");
        if (checkIn is < 0 or > 1439 || checkOut is < 0 or > 1439) throw new ArgumentException("HOTEL_PROPERTY_OPERATING_TIME_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
