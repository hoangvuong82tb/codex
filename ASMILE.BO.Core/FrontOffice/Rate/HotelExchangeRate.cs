using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Rate;

public enum HotelExchangeRateStatus { Draft, Active, Inactive, Archived }

public sealed class HotelExchangeRate
{
    private HotelExchangeRate() { }

    public Guid HotelExchangeRateId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public decimal BuyRate { get; private set; }
    public decimal SellRate { get; private set; }
    public decimal HotelRate { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public bool IsDefault { get; private set; }
    public HotelExchangeRateStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelExchangeRate Create(Guid propertyId, string currencyCode, string currencyName, string symbol,
        decimal buyRate, decimal sellRate, decimal hotelRate, DateTime effectiveFromUtc, DateTime? effectiveToUtc,
        bool isDefault, string? notes, DateTime nowUtc, ActorId? actorId, HotelExchangeRateStatus status = HotelExchangeRateStatus.Active)
    {
        Validate(propertyId, currencyCode, currencyName, buyRate, sellRate, hotelRate, effectiveFromUtc, effectiveToUtc);
        return new()
        {
            HotelExchangeRateId = Uuid7.NewGuid(), PropertyId = propertyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(), CurrencyName = currencyName.Trim(), Symbol = symbol?.Trim() ?? string.Empty,
            BuyRate = buyRate, SellRate = sellRate, HotelRate = hotelRate,
            EffectiveFromUtc = effectiveFromUtc, EffectiveToUtc = effectiveToUtc, IsDefault = isDefault,
            Status = status, Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string currencyCode, string currencyName, string symbol, decimal buyRate, decimal sellRate,
        decimal hotelRate, DateTime effectiveFromUtc, DateTime? effectiveToUtc, string? notes, DateTime nowUtc, ActorId? actorId)
    {
        Validate(PropertyId, currencyCode, currencyName, buyRate, sellRate, hotelRate, effectiveFromUtc, effectiveToUtc);
        CurrencyCode = currencyCode.Trim().ToUpperInvariant(); CurrencyName = currencyName.Trim(); Symbol = symbol?.Trim() ?? string.Empty;
        BuyRate = buyRate; SellRate = sellRate; HotelRate = hotelRate; EffectiveFromUtc = effectiveFromUtc; EffectiveToUtc = effectiveToUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(); UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value;
    }

    public void SetDefault(bool value, DateTime nowUtc, ActorId? actorId)
    { IsDefault = value; UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }

    private static void Validate(Guid propertyId, string code, string name, decimal buy, decimal sell, decimal hotel, DateTime from, DateTime? to)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.");
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 10) throw new ArgumentException("Currency code is invalid.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new ArgumentException("Currency name is invalid.");
        if (buy < 0 || sell < 0 || hotel < 0) throw new ArgumentOutOfRangeException(nameof(buy));
        if (to is DateTime end && end <= from) throw new ArgumentException("Effective end must be after start.");
    }
}
