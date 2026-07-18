using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockServiceStatusService : IServiceStatusService
{
    private readonly List<ServiceStatusDto> _services =
    [
        new() { Id = 1, ServiceName = "asmile-api", DisplayName = "API Service", Status = "Running", LastHeartbeatAt = DateTime.Now.AddSeconds(-10) },
        new() { Id = 2, ServiceName = "asmile-pos-sync", DisplayName = "POS Sync Service", Status = "Running", LastHeartbeatAt = DateTime.Now.AddSeconds(-23) },
        new() { Id = 3, ServiceName = "asmile-print", DisplayName = "Print Service", Status = "Stopped", LastHeartbeatAt = DateTime.Now.AddMinutes(-9) }
    ];

    public Task<IReadOnlyList<ServiceStatusDto>> GetServicesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<ServiceStatusDto>)_services);

    public Task RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var service = _services.FirstOrDefault(x => string.Equals(x.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
        if (service is not null)
        {
            service.Status = "Running";
            service.LastHeartbeatAt = DateTime.Now;
        }

        return Task.CompletedTask;
    }
}
