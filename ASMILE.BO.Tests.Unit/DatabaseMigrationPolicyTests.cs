using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Core.Entities;
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

    [Fact]
    public async Task Legacy_foundation_rows_are_adopted_by_the_selected_company_scope()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        db.FinancialStatementMappings.Add(new FinancialStatementMapping
        {
            Id = Guid.NewGuid(),
            StandardCode = "TT99",
            ReportCode = "BS",
            LineCode = "100",
            LineName = "Tiền và tương đương tiền",
            IsActive = true
        });
        db.TaxCodes.Add(new TaxCode
        {
            Id = Guid.NewGuid(),
            Code = "VAT10",
            Name = "VAT 10%",
            TaxRate = 10,
            TaxType = "VATInput",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        db.FinancialStatementMappings.Single().CompanyId = null;
        db.TaxCodes.Single().CompanyId = null;
        await db.SaveChangesAsync();
        db.FinancialStatementMappings.IgnoreQueryFilters().Count(x => x.CompanyId == null).Should().BeGreaterThan(0);
        db.TaxCodes.IgnoreQueryFilters().Count(x => x.CompanyId == null).Should().BeGreaterThan(0);

        var companyId = Guid.NewGuid();
        var service = new DatabaseMigrationService(
            db,
            NullLogger<DatabaseMigrationService>.Instance,
            Options.Create(new DatabaseOptions()),
            new TestHostEnvironment("Production"));

        var adopted = await service.EnsureLegacyFoundationScopeAsync(companyId);

        adopted.Should().Be(2);
        db.FinancialStatementMappings.IgnoreQueryFilters().Single().CompanyId.Should().Be(companyId);
        db.TaxCodes.IgnoreQueryFilters().Single().CompanyId.Should().Be(companyId);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ASMILE.BO.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
