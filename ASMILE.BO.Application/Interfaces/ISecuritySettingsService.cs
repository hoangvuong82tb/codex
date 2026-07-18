namespace ASMILE.BO.Application.Interfaces;

public interface ISecuritySettingsService
{
    Task<bool> GetRequireAdminConfirmationAsync(CancellationToken cancellationToken = default);
}
