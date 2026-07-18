namespace ASMILE.BO.Application.DTOs.Security;

public sealed class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
}
