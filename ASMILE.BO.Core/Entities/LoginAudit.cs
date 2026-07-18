namespace ASMILE.BO.Core.Entities;

public sealed class LoginAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
