using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockSystemSettingsService : ISystemSettingsService
{
    public Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SystemSettingDto> rows =
        [
            new() { Id = 1, Key = "System.ServerMode", Value = "Central Server", Group = "System", Description = "Chế độ máy chủ trung tâm" },
            new() { Id = 2, Key = "Network.ApiBaseUrl", Value = "https://localhost:8443", Group = "Network", Description = "Base URL API Server" },
            new() { Id = 3, Key = "Backup.Path", Value = @"D:\ASMILE-BO\Backups", Group = "Backup", Description = "Đường dẫn backup" },
            new() { Id = 4, Key = "Storage.Root", Value = @"D:\ACCOUNTING", Group = "Storage", Description = "Data root path" },
            new() { Id = 5, Key = "Storage.Temp", Value = @"D:\ACCOUNTING\_temp", Group = "Storage", Description = "Temp path" },
            new() { Id = 6, Key = "Storage.Export", Value = @"D:\ACCOUNTING\exports", Group = "Storage", Description = "Export path" }
        ];
        return Task.FromResult(rows);
    }
}
