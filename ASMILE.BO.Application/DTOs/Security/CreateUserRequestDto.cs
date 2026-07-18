namespace ASMILE.BO.Application.DTOs.Security;

public sealed class CreateUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public string? Notes { get; set; }
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
}
