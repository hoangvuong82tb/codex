using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResultDto> LoginAsync(LoginRequestDto request);
    Task LogoutAsync(Guid sessionId);
    Task<bool> ValidateSessionAsync(Guid sessionId);
    Task<bool> RenewSessionAsync(Guid sessionId);
    Task<IReadOnlyList<string>?> GetSessionPermissionCodesAsync(Guid sessionId);
    Task ChangePasswordAsync(ChangePasswordRequestDto request);
}
