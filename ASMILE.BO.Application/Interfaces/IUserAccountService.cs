using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface IUserAccountService
{
    Task<IReadOnlyList<UserAccountDto>> GetUsersAsync();
    Task<UserAccountDto?> GetUserByIdAsync(Guid id);
    Task<UserAccountDto> CreateUserAsync(CreateUserRequestDto request);
    Task UpdateUserAsync(UpdateUserRequestDto request);
    Task SetUserActiveAsync(Guid userId, bool isActive);
    Task ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword);
    Task UnlockUserAsync(Guid userId);
    Task DeleteUserAsync(Guid userId);
    Task<IReadOnlyList<LoginAuditDto>> GetLoginAuditsAsync();
}
