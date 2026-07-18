namespace ASMILE.BO.Application.DTOs.Security;

public sealed class UpdateUserRequestDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
}
