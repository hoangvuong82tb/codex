using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.App.Services;

/// <summary>Hydrates and live-applies the persisted EF Core command timeout.</summary>
public sealed class SettingsDatabaseCommandTimeoutMonitor : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsChangeNotifier _notifier;
    private readonly RuntimeDatabaseCommandTimeout _timeout;
    private readonly ILogger<SettingsDatabaseCommandTimeoutMonitor> _logger;
    private readonly object _sync = new();
    private long _changeVersion;
    private bool _subscribed;

    public SettingsDatabaseCommandTimeoutMonitor(
        IServiceScopeFactory scopeFactory,
        ISettingsChangeNotifier notifier,
        RuntimeDatabaseCommandTimeout timeout,
        ILogger<SettingsDatabaseCommandTimeoutMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _timeout = timeout;
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
            var persisted = await settings.GetValueAsync(RuntimeDatabaseCommandTimeout.SettingKey, SettingScope.System, null);

            lock (_sync)
            {
                if (_changeVersion == versionBeforeLoad && persisted is not null && !_timeout.TrySet(persisted.Value))
                {
                    _logger.LogWarning("Ignoring invalid persisted {SettingKey} value; retaining {TimeoutSeconds} seconds.",
                        RuntimeDatabaseCommandTimeout.SettingKey, _timeout.Seconds);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load persisted database command timeout; retaining the default {TimeoutSeconds} seconds.", _timeout.Seconds);
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
        if (!args.Value.Key.Equals(RuntimeDatabaseCommandTimeout.SettingKey, StringComparison.OrdinalIgnoreCase)
            || args.Value.Scope != SettingScope.System)
        {
            return;
        }

        lock (_sync)
        {
            _changeVersion++;
            if (!_timeout.TrySet(args.Value.Value))
            {
                _logger.LogWarning("Ignoring invalid {SettingKey} value received from the settings change event.",
                    RuntimeDatabaseCommandTimeout.SettingKey);
            }
        }
    }
}
