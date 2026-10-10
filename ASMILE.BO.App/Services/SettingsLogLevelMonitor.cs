using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace ASMILE.BO.App.Services;

public static class SettingsLogLevelPolicy
{
    public const string SettingKey = "Logs.LogLevel";

    public static bool TryApply(LoggingLevelSwitch levelSwitch, string? value)
    {
        ArgumentNullException.ThrowIfNull(levelSwitch);

        if (!Enum.TryParse<LogEventLevel>(value?.Trim(), ignoreCase: true, out var level)
            || level is not (LogEventLevel.Debug or LogEventLevel.Information or LogEventLevel.Warning or LogEventLevel.Error))
        {
            return false;
        }

        levelSwitch.MinimumLevel = level;
        return true;
    }
}

public sealed class SettingsLogLevelMonitor : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsChangeNotifier _notifier;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<SettingsLogLevelMonitor> _logger;
    private readonly object _sync = new();
    private long _changeVersion;
    private bool _subscribed;

    public SettingsLogLevelMonitor(
        IServiceScopeFactory scopeFactory,
        ISettingsChangeNotifier notifier,
        LoggingLevelSwitch levelSwitch,
        ILogger<SettingsLogLevelMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _levelSwitch = levelSwitch;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_subscribed)
            {
                _notifier.SettingChanged += OnSettingChanged;
                _subscribed = true;
            }
        }

        var versionBeforeLoad = Volatile.Read(ref _changeVersion);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var value = await settings.GetValueAsync(SettingsLogLevelPolicy.SettingKey, SettingScope.System, null);

            lock (_sync)
            {
                // A concurrent save after subscription is newer than this
                // startup read; do not overwrite its in-memory switch value.
                if (_changeVersion == versionBeforeLoad && value is not null
                    && !SettingsLogLevelPolicy.TryApply(_levelSwitch, value.Value))
                {
                    _logger.LogWarning("Ignoring unsupported persisted {SettingKey} value.", SettingsLogLevelPolicy.SettingKey);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Logging configuration must not prevent the application from
            // starting. Keep Serilog's configured Information fallback.
            _logger.LogWarning(ex, "Could not load persisted log level; retaining the configured fallback.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_subscribed)
            {
                _notifier.SettingChanged -= OnSettingChanged;
                _subscribed = false;
            }
        }

        return Task.CompletedTask;
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs args)
    {
        if (!args.Value.Key.Equals(SettingsLogLevelPolicy.SettingKey, StringComparison.OrdinalIgnoreCase)
            || args.Value.Scope != SettingScope.System)
        {
            return;
        }

        lock (_sync)
        {
            _changeVersion++;
            if (!SettingsLogLevelPolicy.TryApply(_levelSwitch, args.Value.Value))
            {
                _logger.LogWarning("Ignoring unsupported {SettingKey} value received from the settings change event.", SettingsLogLevelPolicy.SettingKey);
            }
        }
    }
}
