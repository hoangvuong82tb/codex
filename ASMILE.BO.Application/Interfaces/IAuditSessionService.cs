using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IAuditSessionService
{
    Task<AuditSessionDto> StartSessionAsync(string source, string? correlationId = null, string? clientMachine = null, string? deviceId = null, string? platform = null, string? metadataJson = null, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(string correlationId, CancellationToken cancellationToken = default);
    Task CompleteSessionAsync(string correlationId, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> AbandonExpiredSessionsAsync(DateTime utcNow, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
}
