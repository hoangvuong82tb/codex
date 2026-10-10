namespace ASMILE.BO.Application.DTOs.Security;

public sealed class UserAccountDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
    public string? Notes { get; set; }
}
