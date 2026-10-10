using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FileBackupServiceTests
{
    [Fact]
    public async Task Backup_validate_and_restore_round_trip_uses_real_configured_files()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"asmile-file-backup-{Guid.NewGuid():N}");
        var dataRoot = Path.Combine(workspace, "data");
        var backupRoot = Path.Combine(workspace, "backups");
        var source = Path.Combine(dataRoot, "exports", "daily.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        await File.WriteAllTextAsync(source, "original-value");

        try
        {
            var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Backup.BackupPath"] = backupRoot,
                ["Storage.DataRootPath"] = dataRoot
            });
            var service = new FileBackupService(new PathSecurityService(), new CurrentUserService(), settings);

            var created = await service.BackupNowAsync();

            created.FileCount.Should().Be(1);
            File.Exists(created.FilePath).Should().BeTrue();
            (await service.ValidateAsync(created.FilePath)).Should().BeTrue();

            await File.WriteAllTextAsync(source, "changed-value");
            var restored = await service.RestoreAsync(created.FilePath, overwriteExisting: true);

            restored.RestoredFiles.Should().Be(1);
            (await File.ReadAllTextAsync(source)).Should().Be("original-value");
            (await service.GetRecentAsync()).Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Validate_rejects_tampered_archive()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"asmile-file-backup-{Guid.NewGuid():N}");
        var dataRoot = Path.Combine(workspace, "data");
        var backupRoot = Path.Combine(workspace, "backups");
        Directory.CreateDirectory(dataRoot);
        try
        {
            var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Backup.BackupPath"] = backupRoot,
                ["Storage.DataRootPath"] = dataRoot
            });
            var service = new FileBackupService(new PathSecurityService(), new CurrentUserService(), settings);
            var created = await service.BackupNowAsync();
            await File.WriteAllBytesAsync(created.FilePath, [1, 2, 3, 4, 5]);

            (await service.ValidateAsync(created.FilePath)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Backup_fails_instead_of_silently_skipping_a_configured_relative_vfc_path()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"asmile-file-backup-{Guid.NewGuid():N}");
        var dataRoot = Path.Combine(workspace, "data");
        var backupRoot = Path.Combine(workspace, "backups");
        Directory.CreateDirectory(dataRoot);

        try
        {
            var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Backup.BackupPath"] = backupRoot,
                ["Storage.DataRootPath"] = dataRoot,
                ["VFC.AccountingDriveEnabled"] = "true",
                ["VFC.AccountingDrivePath"] = "relative/accounting"
            });
            var service = new FileBackupService(new PathSecurityService(), new CurrentUserService(), settings);

            var act = () => service.BackupNowAsync();

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*VFC.AccountingDrivePath*đường dẫn tuyệt đối*");
            Directory.Exists(backupRoot).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Backup_fails_when_required_storage_root_is_missing_even_if_a_vfc_root_exists()
    {
        var workspace = Path.Combine(Path.GetTempPath(), $"asmile-file-backup-{Guid.NewGuid():N}");
        var vfcRoot = Path.Combine(workspace, "accounting");
        var backupRoot = Path.Combine(workspace, "backups");
        Directory.CreateDirectory(vfcRoot);

        try
        {
            var settings = new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Backup.BackupPath"] = backupRoot,
                ["VFC.AccountingDriveEnabled"] = "true",
                ["VFC.AccountingDrivePath"] = vfcRoot
            });
            var service = new FileBackupService(new PathSecurityService(), new CurrentUserService(), settings);

            var act = () => service.BackupNowAsync();

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Storage.DataRootPath*đầy đủ*");
            Directory.Exists(backupRoot).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        }
    }

    private sealed class StubSettingsService(IReadOnlyDictionary<string, string> values) : ISettingsService
    {
        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(values.TryGetValue(key, out var value)
                ? new SettingValueDto { Key = key, Scope = scope, Value = value }
                : null);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task SaveValueAsync(SaveSettingValueRequest request) => throw new NotSupportedException();
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => throw new NotSupportedException();
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => throw new NotSupportedException();
    }
}
