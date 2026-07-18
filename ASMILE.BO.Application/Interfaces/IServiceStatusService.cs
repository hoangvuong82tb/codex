using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IServiceStatusService
{
    Task<IReadOnlyList<ServiceStatusDto>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}
