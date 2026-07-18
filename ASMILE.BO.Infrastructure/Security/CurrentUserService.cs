using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public UserAccountDto? CurrentUser { get; private set; }

    public Guid? CurrentSessionId { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public bool HasPermission(string permissionCode)
    {
        return IsAuthenticated && _permissions.Contains(permissionCode);
    }

    public void SetCurrentUser(LoginResultDto login)
    {
        CurrentUser = new UserAccountDto
        {
            Id = login.UserId,
            UserName = login.UserName,
            DisplayName = login.DisplayName,
            Roles = login.Roles,
            MustChangePassword = login.MustChangePassword,
            IsActive = true
        };

        CurrentSessionId = login.SessionId;
        _permissions.Clear();
        foreach (var permissionCode in login.PermissionCodes)
        {
            _permissions.Add(permissionCode);
        }
    }

    public void Clear()
    {
        CurrentUser = null;
        CurrentSessionId = null;
        _permissions.Clear();
    }
}
