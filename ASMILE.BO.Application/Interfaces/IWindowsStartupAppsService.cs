using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IWindowsStartupAppsService
{
    Task<WindowsStartupAppsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
