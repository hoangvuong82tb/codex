using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface ICurrentUserService
{
    UserAccountDto? CurrentUser { get; }
    Guid? CurrentSessionId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionCode);
    void SetCurrentUser(LoginResultDto login);
    void Clear();
}
