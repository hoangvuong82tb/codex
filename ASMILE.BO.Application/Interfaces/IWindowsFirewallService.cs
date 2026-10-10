using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IWindowsFirewallService
{
    Task<WindowsFirewallSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
