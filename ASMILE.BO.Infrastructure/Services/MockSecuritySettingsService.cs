using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockSecuritySettingsService : ISecuritySettingsService
{
    public Task<bool> GetRequireAdminConfirmationAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
