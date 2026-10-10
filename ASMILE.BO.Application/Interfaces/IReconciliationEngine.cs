using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IReconciliationEngine
{
    Task<ReconciliationSessionDto> StartSessionAsync(string module, CancellationToken cancellationToken = default);
    Task<ReconciliationSessionDto> StartSessionAsync(string module, string? metadataJson, CancellationToken cancellationToken = default);
    Task<int> RunAutoMatchAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> MatchBankStatementLineAsync(Guid sessionId, Guid statementLineId, Guid receivableTransactionId, string? matchedBy = null, CancellationToken cancellationToken = default);
    Task<int> DetectAnomaliesAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReconciliationSessionDto>> GetSessionsAsync(int take = 100, CancellationToken cancellationToken = default);
}
