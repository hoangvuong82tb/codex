using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Tests.Unit;

public sealed class DatabaseMigrationPolicyTests
{
    [Fact]
    public async Task Production_default_policy_disables_startup_migration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var policy = new DatabaseOptions();
        var service = new DatabaseMigrationService(db, NullLogger<DatabaseMigrationService>.Instance, Options.Create(policy), new TestHostEnvironment("Production"));

        await service.InitializeAsync();

        policy.MigrationMode.Should().Be(DatabaseMigrationMode.Disabled);
        policy.AllowApplyPendingMigrations.Should().BeFalse();
        policy.FailOnPendingMigrations.Should().BeTrue();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ASMILE.BO.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
