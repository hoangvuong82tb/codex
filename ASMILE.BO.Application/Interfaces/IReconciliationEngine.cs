using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IReconciliationEngine
{
    Task<ReconciliationSessionDto> StartSessionAsync(string module, CancellationToken cancellationToken = default);
    Task<int> RunAutoMatchAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<int> DetectAnomaliesAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReconciliationSessionDto>> GetSessionsAsync(int take = 100, CancellationToken cancellationToken = default);
}
