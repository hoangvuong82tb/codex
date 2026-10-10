using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace ASMILE.BO.Tests.Unit;

public sealed class EfInventoryMovementSettingsTests
{
    [Fact]
    public async Task Stock_balance_load_reads_the_app_scoped_inventory_threshold()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var settings = new CapturingSettingsService();
        var service = new EfInventoryMovementService(db, new NoOpAuditLogService(), settings);

        var balances = await service.GetStockBalancesAsync(new InventoryMovementFilterDto());

        balances.Should().BeEmpty();
        settings.Request.Should().Be(("Warehouse.LowStockThreshold", SettingScope.App, "inventory"));
    }

    private sealed class CapturingSettingsService : ISettingsService
    {
        public (string Key, SettingScope Scope, string? ScopeTarget)? Request { get; private set; }

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
        {
            Request = (key, scope, scopeTarget);
            return Task.FromResult<SettingValueDto?>(new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = "17" });
        }

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task SaveValueAsync(SaveSettingValueRequest request)
            => throw new NotSupportedException();

        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests)
            => throw new NotSupportedException();

        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();
    }

    private sealed class NoOpAuditLogService : IAuditLogService
    {
        public Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
