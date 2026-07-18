using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class MockCurrentUserService : ICurrentUserService
{
    public UserAccountDto? CurrentUser { get; private set; } = new()
    {
        Id = Guid.Empty,
        UserName = "administrator",
        DisplayName = "Administrator",
        Roles = ["Administrator"],
        IsActive = true
    };

    public Guid? CurrentSessionId { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public bool HasPermission(string permissionCode) => true;

    public void SetCurrentUser(LoginResultDto login)
    {
        CurrentUser = new UserAccountDto
        {
            Id = login.UserId,
            UserName = login.UserName,
            DisplayName = login.DisplayName,
            Roles = login.Roles,
            IsActive = true,
            MustChangePassword = login.MustChangePassword
        };
        CurrentSessionId = login.SessionId;
    }

    public void Clear()
    {
        CurrentUser = null;
        CurrentSessionId = null;
    }
}
