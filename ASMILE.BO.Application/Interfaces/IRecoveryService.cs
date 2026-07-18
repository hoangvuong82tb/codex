using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IRecoveryService
{
    Task<RecoveryCheckpointDto> CreateCheckpointAsync(string module = "system", CancellationToken cancellationToken = default);
    Task<RecoveryCheckpointDto> RestoreCheckpointAsync(Guid checkpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryTimelineEntryDto>> GetRecoveryTimelineAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<ArchiveExportManifestDto> ValidateRecoveryAsync(Guid checkpointId, CancellationToken cancellationToken = default);
}
