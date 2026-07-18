namespace ASMILE.BO.Application.DTOs.Security;

public sealed class LoginAuditDto
{
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
}
