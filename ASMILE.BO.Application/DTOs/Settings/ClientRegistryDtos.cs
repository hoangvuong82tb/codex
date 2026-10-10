namespace ASMILE.BO.Application.DTOs.Settings;

public sealed class ClientRegistryEntryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? SessionId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string AppVersion { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; }
    public DateTime LastActivityAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public bool IsActive { get; init; }
}
