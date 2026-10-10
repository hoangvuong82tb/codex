using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class InfrastructureHealthServiceTests
{
    [Fact]
    public async Task EInvoiceHealth_WarnsWhenOnlyFailClosedProvidersAreRegistered()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"health-einvoice-unavailable-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);

        var service = new InfrastructureHealthService(
            db,
            new HealthyMigrationService(),
            NullLogger<InfrastructureHealthService>.Instance,
            [new UnavailableEInvoiceProvider("VIETTEL")]);

        var report = await service.CheckAsync();

        report.Status.Should().Be("Warning");
        report.Checks.Should().ContainSingle(x =>
            x.Name == "E-invoice" &&
            x.Status == "Warning" &&
            x.Message.Contains("chưa cấu hình adapter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EInvoiceHealth_IsHealthyWhenARealProviderAdapterIsRegistered()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"health-einvoice-configured-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);

        var service = new InfrastructureHealthService(
            db,
            new HealthyMigrationService(),
            NullLogger<InfrastructureHealthService>.Instance,
            [new ConfiguredEInvoiceProvider()]);

        var report = await service.CheckAsync();

        report.Status.Should().Be("Healthy");
        report.Checks.Should().ContainSingle(x =>
            x.Name == "E-invoice" &&
            x.Status == "Healthy" &&
            x.Message.Contains("adapter hóa đơn điện tử thật", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EInvoiceHealth_WarnsWhenDatabaseProviderHasNoMatchingAdapter()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"health-einvoice-missing-adapter-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        db.InvoiceProviderConfigurations.Add(new InvoiceProviderConfiguration
        {
            ProviderCode = "MISA",
            DisplayName = "MISA staging",
            EndpointUrl = "https://staging.example.invalid/einvoice",
            TenantCode = "tenant",
            Username = "operator",
            ProtectedSecret = "protected-secret",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new InfrastructureHealthService(
            db,
            new HealthyMigrationService(),
            NullLogger<InfrastructureHealthService>.Instance,
            [new ConfiguredEInvoiceProvider()]);

        var report = await service.CheckAsync();

        report.Status.Should().Be("Warning");
        report.Checks.Should().ContainSingle(x =>
            x.Name == "E-invoice" &&
            x.Status == "Warning" &&
            x.Message.Contains("MISA", StringComparison.OrdinalIgnoreCase)
            && x.Message.Contains("adapter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EInvoiceHealth_WarnsWhenProviderConfigurationIsIncomplete()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"health-einvoice-incomplete-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        db.InvoiceProviderConfigurations.Add(new InvoiceProviderConfiguration
        {
            ProviderCode = "TEST",
            DisplayName = "Test provider",
            EndpointUrl = string.Empty,
            TenantCode = "tenant",
            Username = "operator",
            ProtectedSecret = "protected-secret",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new InfrastructureHealthService(
            db,
            new HealthyMigrationService(),
            NullLogger<InfrastructureHealthService>.Instance,
            [new ConfiguredEInvoiceProvider()]);

        var report = await service.CheckAsync();

        report.Status.Should().Be("Warning");
        report.Checks.Should().ContainSingle(x =>
            x.Name == "E-invoice" &&
            x.Status == "Warning" &&
            x.Message.Contains("thiếu endpoint", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class HealthyMigrationService : IDatabaseMigrationService
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<MigrationStatusDto> ValidatePendingMigrationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MigrationStatusDto(true, [], [], []));

        public Task<DatabasePreflightDto> GetPreflightAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DatabasePreflightDto>(null!);

        public Task ApplyMigrationsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> ValidateSeedConsistencyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class ConfiguredEInvoiceProvider : IEInvoiceProvider
    {
        public string ProviderCode => "TEST";

        public Task<InvoiceIssueResultDto> IssueAsync(
            Guid invoiceId,
            string xmlPayload,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<InvoiceIssueResultDto> QueryStatusAsync(
            Guid invoiceId,
            string? providerInvoiceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
