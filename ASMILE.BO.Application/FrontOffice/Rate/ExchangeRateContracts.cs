using ASMILE.BO.Core.FrontOffice.Rate;

namespace ASMILE.BO.Application.FrontOffice.Rate;

public enum ExchangeRateDisplayState { Effective, Expiring, Expired, Scheduled, Inactive, Draft }

public sealed record ExchangeRateDto(Guid HotelExchangeRateId, Guid PropertyId, string CurrencyCode, string CurrencyName, string Symbol,
    decimal BuyRate, decimal SellRate, decimal HotelRate, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsDefault,
    HotelExchangeRateStatus Status, ExchangeRateDisplayState DisplayState, string? Notes, DateTime UpdatedAtUtc, byte[]? RowVersion);

public sealed record ExchangeRateSearchRequest(Guid PropertyId, string? SearchText = null, ExchangeRateDisplayState? State = null, DateTime? AsOfUtc = null);
public sealed record CreateExchangeRateRequest(Guid PropertyId, string CurrencyCode, string CurrencyName, string Symbol, decimal BuyRate, decimal SellRate,
    decimal HotelRate, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsDefault, string? Notes);
public sealed record UpdateExchangeRateRequest(Guid HotelExchangeRateId, string CurrencyCode, string CurrencyName, string Symbol, decimal BuyRate,
    decimal SellRate, decimal HotelRate, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, string? Notes, byte[]? ExpectedRowVersion = null);
public sealed record SetDefaultExchangeRateRequest(Guid HotelExchangeRateId, byte[]? ExpectedRowVersion = null);

public interface IExchangeRateQueryService
{
    Task<IReadOnlyList<ExchangeRateDto>> SearchAsync(ExchangeRateSearchRequest request, CancellationToken ct);
}

public interface IExchangeRateCommandService
{
    Task<ExchangeRateDto> CreateAsync(CreateExchangeRateRequest request, CancellationToken ct);
    Task<ExchangeRateDto> UpdateAsync(UpdateExchangeRateRequest request, CancellationToken ct);
    Task<ExchangeRateDto> SetDefaultAsync(SetDefaultExchangeRateRequest request, CancellationToken ct);
}
