using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Tests.Unit;

public sealed class SettingsReadOnlyEnforcementTests
{
    [Fact]
    public async Task GetValueAsync_reports_whether_a_value_comes_from_the_database_or_definition_default()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var defaultValue = await service.GetValueAsync("Security.SessionTimeoutMinutes", SettingScope.System, null);

        defaultValue.Should().NotBeNull();
        defaultValue!.Value.Should().Be("30");
        defaultValue.IsPersisted.Should().BeFalse();

        repository.Values.Add(new SettingValue
        {
            DefinitionKey = "Security.SessionTimeoutMinutes",
            Scope = SettingScope.System,
            Value = "45"
        });

        var persistedValue = await service.GetValueAsync("Security.SessionTimeoutMinutes", SettingScope.System, null);

        persistedValue.Should().NotBeNull();
        persistedValue!.Value.Should().Be("45");
        persistedValue.IsPersisted.Should().BeTrue();
    }

    [Fact]
    public async Task Database_settings_reflect_the_active_connection_without_exposing_credentials()
    {
        var repository = new InMemorySettingsRepository();
        repository.Values.AddRange(
        [
            new SettingValue
            {
                DefinitionKey = "Database.ServerName",
                Scope = SettingScope.System,
                Value = "stale-server"
            },
            new SettingValue
            {
                DefinitionKey = "Database.Password",
                Scope = SettingScope.System,
                Value = "old-secret"
            },
            new SettingValue
            {
                DefinitionKey = "Database.ConnectionString",
                Scope = SettingScope.System,
                Value = "Server=old-server;Password=old-secret"
            }
        ]);
        var runtime = RuntimeDatabaseSettings.FromConnectionString(
            "Server=live-sql;Initial Catalog=LIVE_DB;User ID=app_login;Password=runtime-secret;Encrypt=True");
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService(),
            runtime);

        var server = await service.GetValueAsync("Database.ServerName", SettingScope.System, null);
        var database = await service.GetValueAsync("Database.DatabaseName", SettingScope.System, null);
        var username = await service.GetValueAsync("Database.UserName", SettingScope.System, null);
        var password = await service.GetValueAsync("Database.Password", SettingScope.System, null);
        var connectionString = await service.GetValueAsync("Database.ConnectionString", SettingScope.System, null);
        var allValues = await service.GetValuesAsync(SettingScope.System, null);

        server!.Value.Should().Be("live-sql");
        database!.Value.Should().Be("LIVE_DB");
        username!.Value.Should().Be("app_login");
        password!.Value.Should().Be("••••••••");
        connectionString!.Value.Should().Contain("thông tin xác thực được ẩn");
        new[] { server.Value, database.Value, username.Value, password.Value, connectionString.Value }
            .Should().NotContain(x => x!.Contains("runtime-secret", StringComparison.Ordinal));
        server.IsPersisted.Should().BeFalse();
        server.IsRuntimeProvided.Should().BeTrue();
        allValues.Should().ContainSingle(x => x.Key == "Database.ServerName")
            .Which.Value.Should().Be("live-sql");
        foreach (var value in allValues.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)))
        {
            value.Should().NotContain("runtime-secret");
        }
        allValues.Should().ContainSingle(x => x.Key == "Database.Password")
            .Which.IsRuntimeProvided.Should().BeTrue();
    }

    [Fact]
    public async Task Windows_database_authentication_does_not_claim_a_sql_login_or_password()
    {
        var runtime = RuntimeDatabaseSettings.FromConnectionString(
            "Server=live-sql;Initial Catalog=LIVE_DB;Integrated Security=True;Encrypt=True");

        runtime.TryGetValue("Database.AuthenticationMode", out var authentication).Should().BeTrue();
        runtime.TryGetValue("Database.UserName", out var username).Should().BeTrue();
        runtime.TryGetValue("Database.Password", out var password).Should().BeTrue();

        authentication.Should().Be("Windows");
        username.Should().Be("Windows account");
        password.Should().BeEmpty();
    }

    [Fact]
    public async Task Application_log_retention_is_persisted_while_sink_switches_reflect_runtime_configuration()
    {
        var repository = new InMemorySettingsRepository();
        repository.Values.AddRange(
        [
            new SettingValue { DefinitionKey = "Logs.RetentionDays", Scope = SettingScope.System, Value = "30" },
            new SettingValue { DefinitionKey = "Logs.EnableAuditLog", Scope = SettingScope.System, Value = "false" },
            new SettingValue { DefinitionKey = "Logs.EnableErrorLog", Scope = SettingScope.System, Value = "false" }
        ]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Name"] = "File",
                ["Serilog:WriteTo:0:Args:path"] = "logs/app-.log",
                ["Serilog:WriteTo:0:Args:rollingInterval"] = "Day",
                ["Serilog:WriteTo:0:Args:retainedFileCountLimit"] = "14"
            })
            .Build();
        var runtime = new RuntimeLoggingSettings(configuration);
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService(),
            runtimeLoggingSettings: runtime);

        var retention = await service.GetValueAsync("Logs.RetentionDays", SettingScope.System, null);
        var audit = await service.GetValueAsync("Logs.EnableAuditLog", SettingScope.System, null);
        var errors = await service.GetValueAsync("Logs.EnableErrorLog", SettingScope.System, null);
        var allValues = await service.GetValuesAsync(SettingScope.System, null);

        retention!.Value.Should().Be("30");
        audit!.Value.Should().Be("true");
        errors!.Value.Should().Be("true");
        retention.IsPersisted.Should().BeTrue();
        retention.IsRuntimeProvided.Should().BeFalse();
        allValues.Should().ContainSingle(x => x.Key == "Logs.RetentionDays")
            .Which.Value.Should().Be("30");
    }

    [Fact]
    public void Runtime_logging_settings_only_report_switches_that_are_fixed_by_the_active_sink()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Name"] = "Console"
            })
            .Build();
        var runtime = new RuntimeLoggingSettings(configuration);

        runtime.TryGetValue("Logs.RetentionDays", out _).Should().BeFalse();
        runtime.TryGetValue("Logs.EnableErrorLog", out var errorLog).Should().BeTrue();
        errorLog.Should().Be("false");
        runtime.TryGetValue("Logs.EnableAuditLog", out var audit).Should().BeTrue();
        audit.Should().Be("true");
    }

    [Fact]
    public async Task SaveValueAsync_rejects_a_setting_without_a_runtime_provider()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var act = () => service.SaveValueAsync(new SaveSettingValueRequest
        {
            Key = "Database.ConnectionString",
            Scope = SettingScope.System,
            Value = "Server=not-used;Database=not-used"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chỉ đọc*");
        repository.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveValueAsync_rejects_an_unsupported_theme_at_the_service_boundary()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var act = () => service.SaveValueAsync(new SaveSettingValueRequest
        {
            Key = "Appearance.Theme",
            Scope = SettingScope.User,
            ScopeTarget = "admin",
            Value = "Neon"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Giao diện phải là Light, Dark hoặc System.");
        repository.Values.Should().BeEmpty();
        repository.ChangeLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveValueAsync_rejects_a_malformed_boolean_at_the_service_boundary()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var act = () => service.SaveValueAsync(new SaveSettingValueRequest
        {
            Key = "Backup.EnableAutoBackup",
            Scope = SettingScope.System,
            Value = "sometimes"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Giá trị Boolean phải là true hoặc false.");
        repository.Values.Should().BeEmpty();
        repository.ChangeLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveValuesAsync_validates_the_entire_batch_before_persisting_any_value()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var act = () => service.SaveValuesAsync(
        [
            new SaveSettingValueRequest
            {
                Key = "Logs.LogLevel",
                Scope = SettingScope.System,
                Value = "Debug"
            },
            new SaveSettingValueRequest
            {
                Key = "Database.ConnectionString",
                Scope = SettingScope.System,
                Value = "Server=not-used;Database=not-used"
            }
        ]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chỉ đọc*");
        repository.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveValuesAsync_rejects_a_later_invalid_value_without_partial_persistence()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        var act = () => service.SaveValuesAsync(
        [
            new SaveSettingValueRequest
            {
                Key = "Logs.LogLevel",
                Scope = SettingScope.System,
                Value = "Debug"
            },
            new SaveSettingValueRequest
            {
                Key = "Security.SessionTimeoutMinutes",
                Scope = SettingScope.System,
                Value = "not-a-number"
            }
        ]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveValueAsync_persists_the_runtime_backed_log_level_setting()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(
            repository,
            new SettingsRegistry(),
            new SettingsValidator(),
            new NoOpChangeNotifier(),
            new PassthroughSecretProtector(),
            new AnonymousCurrentUserService(),
            new AllowAllAuthorizationService());

        await service.SaveValueAsync(new SaveSettingValueRequest
        {
            Key = "Logs.LogLevel",
            Scope = SettingScope.System,
            Value = "Debug"
        });

        repository.Values.Should().ContainSingle(x =>
            x.DefinitionKey == "Logs.LogLevel"
            && x.Scope == SettingScope.System
            && x.Value == "Debug");
    }

    private sealed class InMemorySettingsRepository : ISettingsRepository
    {
        public List<SettingDefinition> Definitions { get; } = [];
        public List<SettingValue> Values { get; } = [];

        public Task<IReadOnlyList<SettingDefinition>> GetDefinitionsAsync()
            => Task.FromResult<IReadOnlyList<SettingDefinition>>(Definitions.ToList());

        public Task<IReadOnlyList<SettingValue>> GetValuesAsync()
            => Task.FromResult<IReadOnlyList<SettingValue>>(Values.ToList());

        public Task<SettingValue?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(Values.FirstOrDefault(x => x.DefinitionKey == key && x.Scope == scope && string.Equals(x.ScopeTarget, scopeTarget, StringComparison.OrdinalIgnoreCase)));

        public Task UpsertValueAsync(SettingValue value)
            => UpsertValuesAsync([value]);

        public Task UpsertValuesAsync(IReadOnlyList<SettingValue> values)
        {
            foreach (var value in values)
            {
                Values.RemoveAll(x => x.DefinitionKey == value.DefinitionKey && x.Scope == value.Scope && string.Equals(x.ScopeTarget, value.ScopeTarget, StringComparison.OrdinalIgnoreCase));
                Values.Add(value);
            }
            return Task.CompletedTask;
        }

        public async Task CommitValuesAndAuditAsync(
            IReadOnlyList<SettingValue> values,
            IReadOnlyList<SettingChangeLog> changeLogs)
        {
            await UpsertValuesAsync(values);
            ChangeLogs.AddRange(changeLogs);
        }

        public List<SettingChangeLog> ChangeLogs { get; } = [];

        public Task UpsertDefinitionAsync(SettingDefinition definition)
        {
            Definitions.RemoveAll(x => x.Key == definition.Key);
            Definitions.Add(definition);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpChangeNotifier : ISettingsChangeNotifier
    {
        public event EventHandler<SettingChangedEventArgs>? SettingChanged;
        public void NotifyChanged(SettingValueDto value, bool requiresRestart) => SettingChanged?.Invoke(this, new SettingChangedEventArgs(value, requiresRestart));
    }

    private sealed class PassthroughSecretProtector : ISecretProtector
    {
        public string Protect(string? plainText) => plainText ?? string.Empty;
        public string Unprotect(string? protectedText) => protectedText ?? string.Empty;
        public bool IsProtected(string? value) => false;
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser => null;
        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => false;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }

    private sealed class AllowAllAuthorizationService : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => true;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true);
        public bool HasPermission(Permission permission) => true;
        public void Demand(Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }
}
