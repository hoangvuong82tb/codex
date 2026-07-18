using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockBackupService : IBackupService
{
    private readonly List<BackupRecordDto> _records =
    [
        new()
        {
            Id = 1,
            FileName = "asmile_backup_20260429_230000.zip",
            FilePath = @"D:\ASMILE-BO\Backups\asmile_backup_20260429_230000.zip",
            Size = 72_100_223,
            Status = "Completed",
            CreatedAt = DateTime.Now.AddHours(-10),
            CreatedBy = "Administrator"
        }
    ];

    public Task<IReadOnlyList<BackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<BackupRecordDto>)_records.OrderByDescending(x => x.CreatedAt).Take(10).ToList());

    public Task<BackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default)
    {
        var record = new BackupRecordDto
        {
            Id = _records.Count + 1,
            FileName = $"asmile_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
            FilePath = $@"D:\ASMILE-BO\Backups\asmile_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
            Size = 70_000_000 + DateTime.Now.Second * 1000,
            Status = "Completed",
            CreatedAt = DateTime.Now,
            CreatedBy = "Administrator"
        };
        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(backupFilePath));

    public Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
