using ASMILE.BO.Application.DTOs.Settings;

namespace ASMILE.BO.Application.Interfaces.Settings;

public interface ILicenseService
{
    Task<LicenseStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IUpdateManifestService
{
    Task<UpdateCheckResultDto> CheckAsync(
        string channel,
        Version currentVersion,
        CancellationToken cancellationToken = default);
}

public interface IUpdatePackageStagingService
{
    Task<UpdatePackageStageResultDto> StageLatestAsync(
        string channel,
        Version currentVersion,
        CancellationToken cancellationToken = default);
}
