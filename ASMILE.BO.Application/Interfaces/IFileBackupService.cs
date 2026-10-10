using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

/// <summary>
/// Creates and restores a signed-by-content file snapshot for configured data roots.
/// The provider is deliberately filesystem-only; it never treats a UI placeholder as
/// a backup record and never deletes files during restore.
/// </summary>
public interface IFileBackupService
{
    Task<IReadOnlyList<FileBackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<FileBackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task<FileRestoreResultDto> RestoreAsync(string backupFilePath, bool overwriteExisting = false, CancellationToken cancellationToken = default);
}
