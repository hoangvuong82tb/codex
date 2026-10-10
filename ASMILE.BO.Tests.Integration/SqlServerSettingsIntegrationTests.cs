using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Tests.Integration;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerSettingsIntegrationTests
{
    [Fact]
    public async Task Migration_clears_only_legacy_seed_versions_for_builtin_modules()
    {
        var databaseName = $"ASMILE_BO_APP_VERSION_MIGRATION_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            await db.Database.EnsureDeletedAsync();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261002160000_AddSupplierTaxCode");

            db.AppModules.AddRange(
                new() { AppKey = "dashboard", Name = "Dashboard", Version = "1.0.0" },
                new() { AppKey = "custom-reporting", Name = "Custom reporting", Version = "1.0.0" },
                new() { AppKey = "inventory", Name = "Inventory", Version = "2.0.0" });
            await db.SaveChangesAsync();

            await migrator.MigrateAsync();

            var versions = await db.AppModules.AsNoTracking()
                .ToDictionaryAsync(x => x.AppKey, x => x.Version);
            versions["dashboard"].Should().BeNull();
            versions["custom-reporting"].Should().Be("1.0.0");
            versions["inventory"].Should().Be("2.0.0");
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task Settings_repository_persists_definition_and_value_in_localdb()
    {
        var databaseName = $"ASMILE_BO_SETTINGS_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var repository = new EfSettingsRepository(db);
            await repository.UpsertDefinitionAsync(new SettingDefinition
            {
                Key = "FO.DefaultCurrency",
                DisplayName = "Tiền tệ mặc định",
                Scope = SettingScope.App,
                ValueType = SettingValueType.String,
                DefaultValue = "VND"
            });
            await repository.UpsertValueAsync(new SettingValue
            {
                DefinitionKey = "FO.DefaultCurrency",
                Scope = SettingScope.App,
                ScopeTarget = "fo-client",
                Value = "EUR",
                UpdatedBy = "integration-test"
            });

            await using var readDb = new AppDbContext(options);
            var persisted = await new EfSettingsRepository(readDb)
                .GetValueAsync("FO.DefaultCurrency", SettingScope.App, "fo-client");

            persisted.Should().NotBeNull();
            persisted!.Value.Should().Be("EUR");
            persisted.UpdatedBy.Should().Be("integration-test");
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }
}
