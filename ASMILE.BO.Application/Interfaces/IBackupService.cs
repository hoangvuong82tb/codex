using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBackupService
{
    Task<IReadOnlyList<BackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<BackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
