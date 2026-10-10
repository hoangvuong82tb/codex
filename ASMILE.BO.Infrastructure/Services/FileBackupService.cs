using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Filesystem backup provider for the configured storage/VFC roots.
/// Archives contain a manifest and SHA-256 for every file. Restore is additive by
/// default and never deletes anything; overwrite is only requested by an explicit
/// administrator-confirmed UI action.
/// </summary>
public sealed class FileBackupService : IFileBackupService
{
    private const string ManifestName = "manifest.json";
    private const string ProductName = "ASMILE.BO";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly IPathSecurityService _pathSecurityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISettingsService _settingsService;

    public FileBackupService(
        IPathSecurityService pathSecurityService,
        ICurrentUserService currentUserService,
        ISettingsService settingsService)
    {
        _pathSecurityService = pathSecurityService;
        _currentUserService = currentUserService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<FileBackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var directory = await GetBackupDirectoryAsync(cancellationToken);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<FileBackupRecordDto>();
        }

        var records = new List<FileBackupRecordDto>();
        foreach (var file in Directory.EnumerateFiles(directory, "asmile_files_backup_*.zip", SearchOption.TopDirectoryOnly)
                     .Select(path => new FileInfo(path))
                     .OrderByDescending(file => file.CreationTimeUtc)
                     .Take(10))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = await TryMapAsync(file, records.Count + 1, cancellationToken);
            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public async Task<FileBackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default)
    {
        var backupDirectory = await GetBackupDirectoryAsync(cancellationToken);
        var roots = await GetConfiguredRootsAsync(cancellationToken);
        if (roots.Count == 0)
        {
            throw new InvalidOperationException("Chưa cấu hình thư mục dữ liệu để backup file.");
        }

        Directory.CreateDirectory(backupDirectory);
        var fileName = $"asmile_files_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.zip";
        var finalPath = Path.Combine(backupDirectory, fileName);
        var tempPath = finalPath + ".partial";
        var manifest = new FileBackupManifest
        {
            Product = ProductName,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = _currentUserService.CurrentUser?.DisplayName ?? Environment.UserName
        };

        try
        {
            await using (var fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 128 * 1024, useAsync: true))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                foreach (var root in roots)
                {
                    if (string.Equals(root.Key, "storage-data", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(root.Path, SettingsStoragePaths.DefaultDataRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(root.Path);
                    }

                    await AddRootFilesAsync(archive, root, backupDirectory, manifest, cancellationToken);
                }

                var manifestEntry = archive.CreateEntry(ManifestName, CompressionLevel.Fastest);
                await using var manifestStream = manifestEntry.Open();
                await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken);
            }

            File.Move(tempPath, finalPath);
            var result = await TryMapAsync(new FileInfo(finalPath), 1, cancellationToken);
            return result ?? throw new InvalidOperationException("Không đọc được manifest của file backup vừa tạo.");
        }
        catch
        {
            TryDelete(tempPath);
            TryDelete(finalPath);
            throw;
        }
    }

    public async Task<bool> ValidateAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await ReadValidatedManifestAsync(backupFilePath, cancellationToken);
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public async Task<FileRestoreResultDto> RestoreAsync(
        string backupFilePath,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var manifest = await ReadValidatedManifestAsync(backupFilePath, cancellationToken);
        var roots = (await GetConfiguredRootsAsync(cancellationToken)).ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var missingRoots = manifest.Files.Select(x => x.RootKey).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => !roots.ContainsKey(key))
            .ToList();
        if (missingRoots.Count > 0)
        {
            throw new InvalidOperationException($"Thiếu thư mục đích đã cấu hình cho backup: {string.Join(", ", missingRoots)}.");
        }

        var restored = 0;
        var skipped = 0;
        await using var fileStream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);
        foreach (var item in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = ResolveTargetPath(roots[item.RootKey].Path, item.RelativePath);
            var entry = archive.GetEntry(item.ArchivePath)
                ?? throw new InvalidDataException($"Thiếu file trong archive: {item.ArchivePath}");
            if (File.Exists(target) && !overwriteExisting)
            {
                skipped++;
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            var tempTarget = target + $".restore-{Guid.NewGuid():N}.partial";
            try
            {
                await using (var source = entry.Open())
                await using (var destination = new FileStream(tempTarget, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
                {
                    await source.CopyToAsync(destination, cancellationToken);
                }

                File.Move(tempTarget, target, overwriteExisting);
                restored++;
            }
            finally
            {
                TryDelete(tempTarget);
            }
        }

        return new FileRestoreResultDto { RestoredFiles = restored, SkippedFiles = skipped };
    }

    private async Task AddRootFilesAsync(
        ZipArchive archive,
        RootSpec root,
        string backupDirectory,
        FileBackupManifest manifest,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root.Path))
        {
            throw new DirectoryNotFoundException($"Không tìm thấy thư mục dữ liệu: {root.Path}");
        }

        var excludedBackup = Path.GetFullPath(backupDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = false,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var filePath in Directory.EnumerateFiles(root.Path, "*", enumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.GetFullPath(filePath);
            if (fullPath.StartsWith(excludedBackup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(root.Path, fullPath);
            var archivePath = $"roots/{root.Key}/{NormalizeArchivePath(relativePath)}";
            var entry = archive.CreateEntry(archivePath, CompressionLevel.Fastest);
            var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long length = 0;
            await using (var source = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true))
            await using (var destination = entry.Open())
            {
                var buffer = new byte[128 * 1024];
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    hash.AppendData(buffer, 0, read);
                    length += read;
                }
            }

            manifest.Files.Add(new FileBackupEntry
            {
                RootKey = root.Key,
                RelativePath = NormalizeRelativePath(relativePath),
                ArchivePath = archivePath,
                Length = length,
                Sha256 = Convert.ToHexString(hash.GetHashAndReset())
            });
        }
    }

    private async Task<FileBackupManifest> ReadValidatedManifestAsync(string backupFilePath, CancellationToken cancellationToken)
    {
        var path = await ResolveBackupFileAsync(backupFilePath, cancellationToken);
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);
        var manifestEntry = archive.GetEntry(ManifestName)
            ?? throw new InvalidDataException("Backup file không có manifest.");
        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<FileBackupManifest>(manifestStream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("Manifest backup không hợp lệ.");
        if (!string.Equals(manifest.Product, ProductName, StringComparison.Ordinal)
            || manifest.Files is null)
        {
            throw new InvalidDataException("Manifest không thuộc ASMILE.BO hoặc thiếu danh sách file.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in manifest.Files)
        {
            if (!seen.Add(item.ArchivePath)
                || !IsSafeRelativePath(item.RelativePath)
                || !item.ArchivePath.StartsWith($"roots/{item.RootKey}/", StringComparison.Ordinal)
                || archive.GetEntry(item.ArchivePath) is not { } entry
                || entry.Length != item.Length)
            {
                throw new InvalidDataException($"Manifest có entry không an toàn hoặc thiếu dữ liệu: {item.ArchivePath}");
            }

            await using var stream = entry.Open();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[128 * 1024];
            int read;
            long length = 0;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                length += read;
            }

            if (length != item.Length
                || !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(item.Sha256),
                    hash.GetHashAndReset()))
            {
                throw new InvalidDataException($"Hash không khớp: {item.ArchivePath}");
            }
        }

        var manifestPaths = seen.Append(ManifestName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (archive.Entries.Any(x => !manifestPaths.Contains(x.FullName)))
        {
            throw new InvalidDataException("Backup có entry ngoài manifest.");
        }

        return manifest;
    }

    private async Task<FileBackupRecordDto?> TryMapAsync(FileInfo file, int id, CancellationToken cancellationToken)
    {
        if (!file.Exists || !await ValidateAsync(file.FullName, cancellationToken))
        {
            return null;
        }

        var manifest = await ReadValidatedManifestAsync(file.FullName, cancellationToken);
        return new FileBackupRecordDto
        {
            Id = id,
            FileName = file.Name,
            FilePath = file.FullName,
            Size = file.Length,
            FileCount = manifest.Files.Count,
            Status = "Completed",
            CreatedAt = file.CreationTime,
            CreatedBy = manifest.CreatedBy
        };
    }

    private async Task<string> ResolveBackupFileAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input) || !string.Equals(Path.GetExtension(input), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("File backup không hợp lệ.");
        }

        var root = await GetBackupDirectoryAsync(cancellationToken);
        var path = Path.GetFullPath(input);
        _pathSecurityService.EnsureUnderRoot(root, path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Không tìm thấy file backup.", input);
        }

        return path;
    }

    private async Task<string> GetBackupDirectoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configured = (await _settingsService.GetValueAsync("Backup.BackupPath", SettingScope.System, null))?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "Backups";
        }

        if (!Path.IsPathRooted(configured) && !_pathSecurityService.ValidateVirtualPath(configured))
        {
            throw new InvalidOperationException("Backup.BackupPath không hợp lệ.");
        }

        return Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(SettingsStoragePaths.DefaultDataRoot, configured));
    }

    private async Task<IReadOnlyList<RootSpec>> GetConfiguredRootsAsync(CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            (Key: "storage-data", Setting: "Storage.DataRootPath", EnabledSetting: (string?)null),
            (Key: "vfc-system", Setting: "VFC.SystemDrivePath", EnabledSetting: "VFC.SystemDriveEnabled"),
            (Key: "vfc-accounting", Setting: "VFC.AccountingDrivePath", EnabledSetting: "VFC.AccountingDriveEnabled"),
            (Key: "vfc-sales", Setting: "VFC.SalesDrivePath", EnabledSetting: "VFC.SalesDriveEnabled"),
            (Key: "vfc-hr", Setting: "VFC.HrDrivePath", EnabledSetting: "VFC.HrDriveEnabled"),
            (Key: "vfc-log", Setting: "VFC.LogDrivePath", EnabledSetting: "VFC.LogDriveEnabled")
        };
        var roots = new List<RootSpec>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.EnabledSetting is not null)
            {
                var enabled = (await _settingsService.GetValueAsync(candidate.EnabledSetting, SettingScope.System, null))?.Value?.Trim();
                if (string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(enabled, "0", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(enabled, "off", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(enabled, "no", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var value = (await _settingsService.GetValueAsync(candidate.Setting, SettingScope.System, null))?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                if (string.Equals(candidate.Key, "storage-data", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Storage.DataRootPath chưa được cấu hình; không thể tạo bản backup file đầy đủ.");
                }

                continue;
            }

            if (!Path.IsPathRooted(value))
            {
                throw new InvalidOperationException($"Đường dẫn {candidate.Setting} phải là đường dẫn tuyệt đối: {value}");
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(value);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException($"Đường dẫn {candidate.Setting} không hợp lệ: {value}", ex);
            }

            if (roots.Any(x => string.Equals(x.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            roots.Add(new RootSpec(candidate.Key, fullPath));
        }

        return roots;
    }

    private static string ResolveTargetPath(string root, string relativePath)
    {
        if (!IsSafeRelativePath(relativePath))
        {
            throw new InvalidDataException("Đường dẫn restore không an toàn.");
        }

        var target = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Đường dẫn restore vượt khỏi thư mục đích.");
        }

        return target;
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return false;
        var normalized = path.Replace('\\', '/');
        return !normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(x => x is "." or "..")
               && !normalized.StartsWith("/", StringComparison.Ordinal)
               && !normalized.Contains(":", StringComparison.Ordinal);
    }

    private static string NormalizeRelativePath(string path) => path.Replace('\\', '/');
    private static string NormalizeArchivePath(string path) => NormalizeRelativePath(path).TrimStart('/');

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Preserve the original operation failure; cleanup is best effort.
        }
    }

    private sealed record RootSpec(string Key, string Path);

    private sealed class FileBackupManifest
    {
        public string Product { get; set; } = ProductName;
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<FileBackupEntry> Files { get; set; } = [];
    }

    private sealed class FileBackupEntry
    {
        public string RootKey { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ArchivePath { get; set; } = string.Empty;
        public long Length { get; set; }
        public string Sha256 { get; set; } = string.Empty;
    }
}
