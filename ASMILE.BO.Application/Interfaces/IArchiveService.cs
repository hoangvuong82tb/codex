using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IArchiveService
{
    Task<IReadOnlyList<DataRetentionPolicyDto>> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default);
    Task<DataRetentionPolicyDto> SaveRetentionPolicyAsync(DataRetentionPolicyDto policy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetentionPreviewDto>> PreviewRetentionAsync(CancellationToken cancellationToken = default);
    Task<ArchiveBatchDto> RunArchiveAsync(string? module = null, bool compressArchivedPayload = true, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    Task<ArchiveBatchDto> RestoreArchiveAsync(Guid archiveBatchId, CancellationToken cancellationToken = default);
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArchiveBatchDto>> GetArchiveHistoryAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArchiveIntegrityResultDto>> ValidateArchiveIntegrityAsync(Guid? archiveBatchId = null, CancellationToken cancellationToken = default);
}
