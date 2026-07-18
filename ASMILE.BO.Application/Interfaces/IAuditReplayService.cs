using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IAuditReplayService
{
    Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayEntriesAsync(string? correlationId = null, string? entityType = null, string? entityId = null, int take = 500, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditCorrelationDto>> GetCorrelationsAsync(string? entityType = null, string? entityId = null, int take = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEntitySnapshotDto>> GetSnapshotsAsync(int auditLogId, CancellationToken cancellationToken = default);
    Task<AuditReplaySummaryDto> GetReplaySummaryAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditTimelineNodeDto>> GetTimelineAsync(string? correlationId = null, Guid? sessionId = null, int page = 1, int pageSize = 200, CancellationToken cancellationToken = default);
    Task<AuditReplayDiffResultDto> ReconstructStateAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayByJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayBySourceAsync(string sourceType, string sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditExportHistoryDto>> GetExportHistoryAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<AuditExportResultDto> ExportAsync(AuditExportRequestDto request, CancellationToken cancellationToken = default);
}
