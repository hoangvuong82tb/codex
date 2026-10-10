using System.Globalization;
using System.IO;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.App.Services;

/// <summary>
/// Applies the user-configured retention period only to ASMILE's own daily
/// application log files. Audit logs and files outside that directory are
/// deliberately out of scope.
/// </summary>
public sealed class ApplicationLogRetentionService : BackgroundService
{
    public const string SettingKey = "Logs.RetentionDays";
    private const int DefaultRetentionDays = 14;
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsChangeNotifier _notifier;
    private readonly ILogger<ApplicationLogRetentionService> _logger;
    private readonly SemaphoreSlim _refreshRequested = new(0, 1);

    public ApplicationLogRetentionService(
        IServiceScopeFactory scopeFactory,
        ISettingsChangeNotifier notifier,
        ILogger<ApplicationLogRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _notifier.SettingChanged += OnSettingChanged;
        try
        {
            await ApplyConfiguredRetentionAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await _refreshRequested.WaitAsync(SweepInterval, stoppingToken);

                while (_refreshRequested.Wait(0))
                {
                }

                await ApplyConfiguredRetentionAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _notifier.SettingChanged -= OnSettingChanged;
        }
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs args)
    {
        if (!string.Equals(args.Value.Key, SettingKey, StringComparison.OrdinalIgnoreCase)
            || args.Value.Scope != SettingScope.System)
        {
            return;
        }

        try
        {
            _refreshRequested.Release();
        }
        catch (SemaphoreFullException)
        {
            // Multiple edits during a sweep collapse into one fresh read.
        }
    }

    private async Task ApplyConfiguredRetentionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var configured = await settings.GetValueAsync(SettingKey, SettingScope.System, null);
            var retentionDays = int.TryParse(configured?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                && parsed is >= 1 and <= 3650
                    ? parsed
                    : DefaultRetentionDays;

            var deletedCount = ApplicationLogRetention.PruneExpiredFiles(
                ApplicationDataPaths.UserLogsDirectory,
                retentionDays,
                DateTime.Now);
            if (deletedCount > 0)
            {
                _logger.LogInformation("Application log retention removed {DeletedCount} expired files using a {RetentionDays}-day policy.", deletedCount, retentionDays);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A cleanup issue must never prevent the application from starting
            // or stop future daily sweeps.
            _logger.LogWarning(ex, "Could not apply the configured application log retention policy.");
        }
    }
}

public static class ApplicationLogRetention
{
    private const string FilePrefix = "asmile-bo-";
    private const string FileSuffix = ".log";

    /// <summary>
    /// Deletes only non-reparse-point files with Serilog's exact daily filename
    /// format directly under the specified application log directory.
    /// </summary>
    public static int PruneExpiredFiles(string directory, int retentionDays, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (retentionDays is < 1 or > 3650)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention must be between 1 and 3650 days.");
        }

        var fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
        {
            return 0;
        }

        var firstRetainedDate = now.Date.AddDays(1 - retentionDays);
        var deletedCount = 0;
        foreach (var filePath in Directory.EnumerateFiles(fullDirectory, $"{FilePrefix}*{FileSuffix}", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.Length != FilePrefix.Length + 8 + FileSuffix.Length
                || !fileName.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)
                || !fileName.EndsWith(FileSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dateText = fileName.Substring(FilePrefix.Length, 8);
            if (!DateTime.TryParseExact(dateText, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate)
                || fileDate.Date >= firstRetainedDate)
            {
                continue;
            }

            try
            {
                if ((File.GetAttributes(filePath) & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                File.Delete(filePath);
                deletedCount++;
            }
            catch (FileNotFoundException)
            {
                // Another process may have rotated or cleaned this file.
            }
            catch (IOException)
            {
                // The current or a concurrently-open log file is left alone.
            }
            catch (UnauthorizedAccessException)
            {
                // Continue; cleanup is best-effort and must stay within the
                // application's own log folder.
            }
        }

        return deletedCount;
    }
}
