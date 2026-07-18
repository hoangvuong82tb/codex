using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IDataIntegrityService
{
    Task<IReadOnlyList<DataIntegrityIssueDto>> ScanAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateCheckpointAsync(string checkpointCode, CancellationToken cancellationToken = default);
    Task<Guid> QueueRepairAsync(string operationCode, CancellationToken cancellationToken = default);
}
