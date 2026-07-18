namespace ASMILE.BO.Application.DTOs.Security;

public sealed class LoginResultDto
{
    public bool Success { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
    public bool MustChangePassword { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
