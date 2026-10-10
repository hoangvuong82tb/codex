using ASMILE.BO.Application.DTOs.Settings;

namespace ASMILE.BO.Application.Interfaces.Settings;

public interface IClientRegistryService
{
    Task<IReadOnlyList<ClientRegistryEntryDto>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
    Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
