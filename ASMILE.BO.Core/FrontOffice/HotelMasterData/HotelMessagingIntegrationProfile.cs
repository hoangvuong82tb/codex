using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped messaging/API integration profile without storing plaintext secrets.</summary>
public sealed class HotelMessagingIntegrationProfile
{
    private HotelMessagingIntegrationProfile() { }
    public Guid HotelMessagingIntegrationProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public string? SecretReference { get; private set; }
    public string? TemplateReference { get; private set; }
    public int MaxRetries { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelMessagingIntegrationProfile Create(PropertyId propertyId, string code, string name, string channel, string provider, string endpoint, string? secretReference, string? templateReference, int maxRetries, int timeoutSeconds, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, channel, provider, endpoint, maxRetries, timeoutSeconds, from, to); return new HotelMessagingIntegrationProfile { HotelMessagingIntegrationProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Channel = channel.Trim(), Provider = provider.Trim(), Endpoint = endpoint.Trim(), SecretReference = Normalize(secretReference), TemplateReference = Normalize(templateReference), MaxRetries = maxRetries, TimeoutSeconds = timeoutSeconds, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string channel, string provider, string endpoint, string? secretReference, string? templateReference, int maxRetries, int timeoutSeconds, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, channel, provider, endpoint, maxRetries, timeoutSeconds, from, to); Code = code.Trim(); Name = name.Trim(); Channel = channel.Trim(); Provider = provider.Trim(); Endpoint = endpoint.Trim(); SecretReference = Normalize(secretReference); TemplateReference = Normalize(templateReference); MaxRetries = maxRetries; TimeoutSeconds = timeoutSeconds; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string channel, string provider, string endpoint, int retries, int timeout, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_MESSAGING_INTEGRATION_PROFILE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("HOTEL_MESSAGING_INTEGRATION_PROFILE_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_MESSAGING_INTEGRATION_PROFILE_EFFECTIVE_RANGE_INVALID"); if (retries < 0 || retries > 20 || timeout < 1 || timeout > 600) throw new ArgumentException("HOTEL_MESSAGING_INTEGRATION_PROFILE_VALUES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
