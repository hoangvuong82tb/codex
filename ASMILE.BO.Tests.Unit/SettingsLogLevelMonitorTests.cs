using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace ASMILE.BO.Tests.Unit;

public sealed class SettingsLogLevelMonitorTests
{
    [Fact]
    public async Task Persisted_level_loads_at_start_and_system_changes_apply_immediately()
    {
        using var services = new ServiceCollection()
            .AddScoped<ISettingsService>(_ => new FakeSettingsService("Warning"))
            .BuildServiceProvider();
        var notifier = new TestSettingsChangeNotifier();
        var levelSwitch = new LoggingLevelSwitch();
        var monitor = new SettingsLogLevelMonitor(
            services.GetRequiredService<IServiceScopeFactory>(),
            notifier,
            levelSwitch,
            NullLogger<SettingsLogLevelMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);

        notifier.Raise(new SettingValueDto
        {
            Key = SettingsLogLevelPolicy.SettingKey,
            Scope = SettingScope.System,
            Value = "Debug"
        });
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);

        notifier.Raise(new SettingValueDto
        {
            Key = SettingsLogLevelPolicy.SettingKey,
            Scope = SettingScope.User,
            Value = "Error"
        });
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);

        await monitor.StopAsync(CancellationToken.None);
    }

    private sealed class FakeSettingsService(string? level) : ISettingsService
    {
        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingValueDto>>([]);

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult<SettingValueDto?>(key == SettingsLogLevelPolicy.SettingKey && scope == SettingScope.System
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = level }
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
