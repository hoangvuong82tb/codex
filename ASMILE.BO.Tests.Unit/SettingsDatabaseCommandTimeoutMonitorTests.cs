using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class SettingsDatabaseCommandTimeoutMonitorTests
{
    [Fact]
    public async Task Persisted_timeout_loads_at_start_and_system_changes_apply_immediately()
    {
        using var services = new ServiceCollection()
            .AddScoped<ISettingsService>(_ => new FakeSettingsService("90"))
            .BuildServiceProvider();
        var notifier = new TestSettingsChangeNotifier();
        var runtimeTimeout = new RuntimeDatabaseCommandTimeout();
        var monitor = new SettingsDatabaseCommandTimeoutMonitor(
            services.GetRequiredService<IServiceScopeFactory>(),
            notifier,
            runtimeTimeout,
            NullLogger<SettingsDatabaseCommandTimeoutMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        runtimeTimeout.Seconds.Should().Be(90);

        notifier.Raise(new SettingValueDto
        {
            Key = RuntimeDatabaseCommandTimeout.SettingKey,
            Scope = SettingScope.System,
            Value = "120"
        });
        runtimeTimeout.Seconds.Should().Be(120);

        notifier.Raise(new SettingValueDto
        {
            Key = RuntimeDatabaseCommandTimeout.SettingKey,
            Scope = SettingScope.User,
            Value = "300"
        });
        runtimeTimeout.Seconds.Should().Be(120, "database command timeout is a system-wide policy");

        await monitor.StopAsync(CancellationToken.None);
    }

    private sealed class FakeSettingsService(string? timeout) : ISettingsService
    {
        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingValueDto>>([]);

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult<SettingValueDto?>(key == RuntimeDatabaseCommandTimeout.SettingKey && scope == SettingScope.System
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = timeout }
                : null);

        public Task SaveValueAsync(SaveSettingValueRequest request) => Task.CompletedTask;
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => Task.CompletedTask;
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => Task.CompletedTask;
    }

    private sealed class TestSettingsChangeNotifier : ISettingsChangeNotifier
    {
        public event EventHandler<SettingChangedEventArgs>? SettingChanged;

        public void NotifyChanged(SettingValueDto value, bool requiresRestart)
            => SettingChanged?.Invoke(this, new SettingChangedEventArgs(value, requiresRestart));

        public void Raise(SettingValueDto value) => NotifyChanged(value, requiresRestart: false);
    }
}
