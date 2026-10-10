using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads security switches from the persisted Settings Engine.
/// Missing values intentionally fall back to the registry default (true).
/// </summary>
public sealed class EfSecuritySettingsService : ISecuritySettingsService
{
    private const string RequireAdminConfirmationKey = "Security.RequireAdminConfirmation";
    private readonly ISettingsService _settingsService;

    public EfSecuritySettingsService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> GetRequireAdminConfirmationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _settingsService.GetValueAsync(
            RequireAdminConfirmationKey,
            SettingScope.System,
            scopeTarget: null);

        if (value is null || string.IsNullOrWhiteSpace(value.Value))
        {
            return true;
        }

        return bool.TryParse(value.Value, out var result) ? result : true;
    }
}
