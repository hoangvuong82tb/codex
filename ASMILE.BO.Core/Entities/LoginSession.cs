namespace ASMILE.BO.Core.Entities;

public sealed class LoginSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    /// <summary>
    /// One-way hash of the in-memory session token. The raw token must never be persisted.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
