using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class EfSettingsRepositoryTests
{
    [Fact]
    public async Task Upserts_are_available_from_a_new_context()
    {
        var databaseName = $"settings-repository-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var writeDb = new AppDbContext(options))
        {
            var repository = new EfSettingsRepository(writeDb);
            await repository.UpsertDefinitionAsync(new SettingDefinition
            {
                Key = "GeneralLedger.DefaultCurrency",
                DisplayName = "Đơn vị tiền tệ mặc định",
                Scope = SettingScope.App,
                ValueType = SettingValueType.String,
                DefaultValue = "VND",
                SortOrder = 10
            });
            await repository.UpsertValueAsync(new SettingValue
            {
                DefinitionKey = "GeneralLedger.DefaultCurrency",
                Scope = SettingScope.App,
                ScopeTarget = "general-ledger",
                Value = "USD",
                UpdatedBy = "tester"
            });
        }

        await using (var readDb = new AppDbContext(options))
        {
            var repository = new EfSettingsRepository(readDb);
            var definitions = await repository.GetDefinitionsAsync();
            var value = await repository.GetValueAsync("GeneralLedger.DefaultCurrency", SettingScope.App, "general-ledger");

            definitions.Should().ContainSingle(x => x.Key == "GeneralLedger.DefaultCurrency");
            value.Should().NotBeNull();
            value!.Value.Should().Be("USD");
            value.UpdatedBy.Should().Be("tester");
        }
    }

    [Fact]
    public async Task Upsert_replaces_value_for_the_same_scope_key()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"settings-repository-update-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var repository = new EfSettingsRepository(db);
        await repository.UpsertValueAsync(new SettingValue
        {
            DefinitionKey = "FO.DefaultCurrency",
            Scope = SettingScope.App,
            ScopeTarget = "fo-client",
            Value = "VND"
        });
        await repository.UpsertValueAsync(new SettingValue
        {
            DefinitionKey = "FO.DefaultCurrency",
            Scope = SettingScope.App,
            ScopeTarget = "fo-client",
            Value = "EUR"
        });

        var rows = await repository.GetValuesAsync();

        rows.Should().ContainSingle(x => x.DefinitionKey == "FO.DefaultCurrency" && x.Value == "EUR");
    }

    [Fact]
    public async Task Factory_backed_repository_reads_and_writes_through_isolated_contexts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"settings-repository-factory-{Guid.NewGuid():N}")
            .Options;
        var repository = new EfSettingsRepository(new TestDbContextFactory(options));

        await repository.UpsertDefinitionAsync(new SettingDefinition
        {
            Key = "Maintenance.UpdateChannel",
            DisplayName = "Update channel",
            Scope = SettingScope.System,
            ValueType = SettingValueType.Enum,
            DefaultValue = "Stable",
            SortOrder = 1
        });
        await repository.UpsertValueAsync(new SettingValue
        {
            DefinitionKey = "Maintenance.UpdateChannel",
            Scope = SettingScope.System,
            Value = "Stable",
            UpdatedBy = "factory-test"
        });

        var value = await repository.GetValueAsync("Maintenance.UpdateChannel", SettingScope.System, null);

        value.Should().NotBeNull();
        value!.Value.Should().Be("Stable");
        value.UpdatedBy.Should().Be("factory-test");
    }

    [Fact]
    public async Task UpsertValuesAsync_commits_the_value_batch_atomically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var repository = new EfSettingsRepository(db);
        await repository.UpsertDefinitionAsync(new SettingDefinition
        {
            Key = "Atomic.First",
            DisplayName = "First",
            Scope = SettingScope.System,
            ValueType = SettingValueType.String
        });
        await repository.UpsertDefinitionAsync(new SettingDefinition
        {
            Key = "Atomic.Second",
            DisplayName = "Second",
            Scope = SettingScope.System,
            ValueType = SettingValueType.String
        });
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectAtomicSecond BEFORE INSERT ON SettingValues " +
            "WHEN NEW.DefinitionKey = 'Atomic.Second' BEGIN SELECT RAISE(ABORT, 'forced batch failure'); END;");

        var act = () => repository.UpsertValuesAsync(
        [
            new SettingValue { DefinitionKey = "Atomic.First", Scope = SettingScope.System, Value = "one" },
            new SettingValue { DefinitionKey = "Atomic.Second", Scope = SettingScope.System, Value = "two" }
        ]);

        await act.Should().ThrowAsync<DbUpdateException>();
        (await repository.GetValuesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CommitValuesAndAuditAsync_rolls_back_values_when_audit_insert_fails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var repository = new EfSettingsRepository(db);
        await repository.UpsertDefinitionAsync(new SettingDefinition
        {
            Key = "Atomic.Audited",
            DisplayName = "Audited",
            Scope = SettingScope.System,
            ValueType = SettingValueType.String
        });
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectSettingAudit BEFORE INSERT ON SettingChangeLogs " +
            "BEGIN SELECT RAISE(ABORT, 'forced audit failure'); END;");

        var act = () => repository.CommitValuesAndAuditAsync(
            [new SettingValue { DefinitionKey = "Atomic.Audited", Scope = SettingScope.System, Value = "new" }],
            [new SettingChangeLog { SettingKey = "Atomic.Audited", Scope = SettingScope.System, NewValue = "new" }]);

        await act.Should().ThrowAsync<DbUpdateException>();
        (await repository.GetValuesAsync()).Should().BeEmpty();
        (await db.SettingChangeLogs.AsNoTracking().ToListAsync()).Should().BeEmpty();
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AppDbContext(options));
        }
    }
}
