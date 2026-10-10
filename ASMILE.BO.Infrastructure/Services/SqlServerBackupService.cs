using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.SettingsEngine;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// SQL Server backed database backup provider. Backup files are discovered from
/// the configured directory; no in-memory or fabricated records are returned.
/// </summary>
public sealed class SqlServerBackupService : IBackupService
{
    private const int ExpressEngineEdition = 4;
    private const int CompressionUnsupportedSqlError = 1844;
    private const string BackupFilePattern = "asmile_backup_*.bak";
    private const string DatabaseBackupPrefix = "asmile_backup_db_";
    private readonly AppDbContext _dbContext;
    private readonly DatabaseOptions _options;
    private readonly IPathSecurityService _pathSecurityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISettingsService? _settingsService;

    public SqlServerBackupService(
        AppDbContext dbContext,
        IOptions<DatabaseOptions> options,
        IPathSecurityService pathSecurityService,
        ICurrentUserService currentUserService)
        : this(dbContext, options, pathSecurityService, currentUserService, null)
    {
    }

    public SqlServerBackupService(
        AppDbContext dbContext,
        IOptions<DatabaseOptions> options,
        IPathSecurityService pathSecurityService,
        ICurrentUserService currentUserService,
        ISettingsService? settingsService)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _pathSecurityService = pathSecurityService;
        _currentUserService = currentUserService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<BackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = await GetBackupDirectoryAsync(cancellationToken);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<BackupRecordDto>();
        }

        var backupPrefix = GetDatabaseBackupPrefix(GetConnectionInfo().DatabaseName);
        var files = GetBackupFiles(directory, backupPrefix, includeLegacy: true)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Take(10)
            .Select((file, index) => Map(file, index + 1, "Chưa xác thực"))
            .ToList();

        return files;
    }

    public async Task<BackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = await GetBackupDirectoryAsync(cancellationToken);
        var policy = await GetBackupPolicyAsync(cancellationToken);
        Directory.CreateDirectory(directory);
        var connectionInfo = GetConnectionInfo();
        var backupPrefix = GetDatabaseBackupPrefix(connectionInfo.DatabaseName);
        var path = Path.Combine(directory, $"{backupPrefix}{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.bak");

        await using var connection = new SqlConnection(connectionInfo.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        var compress = policy.Compress;
        if (compress && await GetEngineEditionAsync(connection, cancellationToken) == ExpressEngineEdition)
        {
            compress = false;
            System.Diagnostics.Trace.TraceWarning(
                "Backup compression is unavailable on SQL Server Express; creating an uncompressed backup for {0}.",
                connectionInfo.DatabaseName);
        }

        command.CommandText = BuildBackupCommand(connectionInfo.DatabaseName, path, compress);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (compress && ex.Number == CompressionUnsupportedSqlError)
        {
            // Edition detection is a best-effort preflight. Keep a narrow runtime
            // fallback for servers/proxies that report capabilities differently.
            System.Diagnostics.Trace.TraceWarning(
                "SQL Server rejected compressed backup for {0}; retrying once without compression. {1}",
                connectionInfo.DatabaseName,
                ex);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            command.CommandText = BuildBackupCommand(connectionInfo.DatabaseName, path, compress: false);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var file = new FileInfo(path);
        if (!file.Exists || file.Length <= 0)
        {
            throw new InvalidOperationException("SQL Server không tạo được file backup hợp lệ.");
        }

        var verified = false;
        if (policy.VerifyAfterBackup)
        {
            // Never advertise a backup as completed until SQL Server has verified
            // its checksum/header when the persisted policy requires verification.
            verified = await ValidateBackupAsync(path, cancellationToken);
            if (!verified)
            {
                throw new InvalidOperationException("SQL Server không xác thực được file backup vừa tạo.");
            }
        }

        await ApplyRetentionAsync(directory, path, backupPrefix, policy.RetentionCount, cancellationToken);

        return Map(file, 1, verified ? "Đã xác thực" : "Chưa xác thực");
    }

    public async Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolved = await TryResolveBackupPathAsync(backupFilePath, cancellationToken);
        if (!resolved.IsValid || !File.Exists(resolved.Path))
        {
            return false;
        }
        var path = resolved.Path;

        var connectionInfo = GetConnectionInfo();
        await using var connection = new SqlConnection(connectionInfo.MasterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = $"RESTORE VERIFYONLY FROM DISK = N'{EscapeLiteral(path)}' WITH CHECKSUM;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!_options.AllowDatabaseRestore)
        {
            throw new InvalidOperationException("Restore database đang bị khóa trong cấu hình an toàn.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var resolved = await TryResolveBackupPathAsync(backupFilePath, cancellationToken);
        if (!resolved.IsValid || !File.Exists(resolved.Path))
        {
            throw new FileNotFoundException("Không tìm thấy file backup trong thư mục backup được phép.", backupFilePath);
        }
        var path = resolved.Path;

        // Validate before switching the live database to SINGLE_USER. A corrupt
        // or truncated file must fail before any destructive restore step.
        if (!await ValidateBackupAsync(path, cancellationToken))
        {
            throw new InvalidDataException("Backup không vượt qua RESTORE VERIFYONLY WITH CHECKSUM.");
        }

        var connectionInfo = GetConnectionInfo();
        await using var connection = new SqlConnection(connectionInfo.MasterConnectionString);
        await connection.OpenAsync(cancellationToken);
        var database = QuoteIdentifier(connectionInfo.DatabaseName);
        var escapedPath = EscapeLiteral(path);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        var switchedToSingleUser = false;
        try
        {
            command.CommandText = $"ALTER DATABASE {database} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            switchedToSingleUser = true;

            command.CommandText = $"RESTORE DATABASE {database} FROM DISK = N'{escapedPath}' WITH REPLACE, RECOVERY;";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (switchedToSingleUser && connection.State == System.Data.ConnectionState.Open)
            {
                command.CommandText = $"ALTER DATABASE {database} SET MULTI_USER;";
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
    }

    private async Task<string> GetBackupDirectoryAsync(CancellationToken cancellationToken)
    {
        var configured = _settingsService is null
            ? null
            : (await _settingsService.GetValueAsync("Backup.BackupPath", SettingScope.System, null))?.Value?.Trim();
        configured ??= _options.BackupDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "Backups";
        }

        if (!Path.IsPathRooted(configured) && !_pathSecurityService.ValidateVirtualPath(configured))
        {
            throw new InvalidOperationException("Database:BackupDirectory không hợp lệ.");
        }

        return Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(SettingsStoragePaths.DefaultDataRoot, configured));
    }

    private async Task<BackupPolicy> GetBackupPolicyAsync(CancellationToken cancellationToken)
    {
        if (_settingsService is null)
        {
            return new BackupPolicy(Compress: true, VerifyAfterBackup: true, RetentionCount: 10);
        }

        var compress = ParseBoolean(
            (await _settingsService.GetValueAsync("Backup.CompressBackup", SettingScope.System, null))?.Value,
            defaultValue: true);
        var verify = ParseBoolean(
            (await _settingsService.GetValueAsync("Backup.VerifyAfterBackup", SettingScope.System, null))?.Value,
            defaultValue: true);
        var retentionValue = (await _settingsService.GetValueAsync("Backup.RetentionCount", SettingScope.System, null))?.Value;
        var retention = int.TryParse(retentionValue, out var parsedRetention) && parsedRetention > 0
            ? Math.Min(parsedRetention, 10_000)
            : 10;

        return new BackupPolicy(compress, verify, retention);
    }

    private static async Task<int?> GetEngineEditionAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(int, SERVERPROPERTY('EngineEdition'));";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string BuildBackupCommand(string databaseName, string path, bool compress)
        => $"BACKUP DATABASE {QuoteIdentifier(databaseName)} "
            + $"TO DISK = N'{EscapeLiteral(path)}' WITH COPY_ONLY, CHECKSUM, INIT, {(compress ? "COMPRESSION" : "NO_COMPRESSION")};";

    private static async Task ApplyRetentionAsync(
        string directory,
        string currentPath,
        string backupPrefix,
        int retentionCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentFullPath = Path.GetFullPath(currentPath);
        var files = GetBackupFiles(directory, backupPrefix, includeLegacy: false)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.LastWriteTimeUtc)
            .ToList();
        var keep = files.Take(Math.Max(0, retentionCount - 1))
            .Select(file => file.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        keep.Add(currentFullPath);

        foreach (var file in files.Where(file => !keep.Contains(file.FullName)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Không thể áp dụng retention cho file backup '{file.Name}'.", ex);
            }
        }
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out var parsed)) return parsed;
        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "yes" or "on" => true,
            "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    private async Task<(bool IsValid, string Path)> TryResolveBackupPathAsync(
        string input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input) || !string.Equals(Path.GetExtension(input), ".bak", StringComparison.OrdinalIgnoreCase))
        {
            return (false, string.Empty);
        }

        try
        {
            var root = await GetBackupDirectoryAsync(cancellationToken);
            var path = Path.GetFullPath(input);
            _pathSecurityService.EnsureUnderRoot(root, path);

            var fileName = Path.GetFileName(path);
            if (!fileName.StartsWith("asmile_backup_", StringComparison.OrdinalIgnoreCase))
            {
                return (false, string.Empty);
            }

            if (fileName.StartsWith(DatabaseBackupPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var backupPrefix = GetDatabaseBackupPrefix(GetConnectionInfo().DatabaseName);
                if (!fileName.StartsWith(backupPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, string.Empty);
                }
            }

            return (true, path);
        }
        catch (ArgumentException)
        {
            return (false, string.Empty);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, string.Empty);
        }
    }

    private (string ConnectionString, string MasterConnectionString, string DatabaseName) GetConnectionInfo()
    {
        var connectionString = _dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException("Database name is missing from the SQL Server connection string.");
        }

        var master = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };
        return (builder.ConnectionString, master.ConnectionString, builder.InitialCatalog);
    }

    private static string GetDatabaseBackupPrefix(string databaseName)
    {
        var normalizedName = databaseName.ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName)));
        return $"{DatabaseBackupPrefix}{hash}_";
    }

    private static IEnumerable<string> GetBackupFiles(string directory, string backupPrefix, bool includeLegacy)
        => Directory.EnumerateFiles(directory, BackupFilePattern, SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith(backupPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Legacy files are retained and remain visible for compatibility, but
                // are excluded from database-scoped retention because their owner is unknown.
                return includeLegacy
                    && !fileName.StartsWith(DatabaseBackupPrefix, StringComparison.OrdinalIgnoreCase);
            });

    private BackupRecordDto Map(FileInfo file, int id, string status)
        => new()
        {
            Id = id,
            FileName = file.Name,
            FilePath = file.FullName,
            Size = file.Length,
            Status = status,
            CreatedAt = file.CreationTime,
            CreatedBy = _currentUserService.CurrentUser?.DisplayName ?? Environment.UserName
        };

    private sealed record BackupPolicy(bool Compress, bool VerifyAfterBackup, int RetentionCount);

    private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
