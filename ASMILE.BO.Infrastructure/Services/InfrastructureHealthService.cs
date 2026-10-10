using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class InfrastructureHealthService : IInfrastructureHealthService
{
    private readonly AppDbContext _dbContext;
    private readonly IDatabaseMigrationService _migrationService;
    private readonly ILogger<InfrastructureHealthService> _logger;
    private readonly IReadOnlyList<IEInvoiceProvider> _invoiceProviders;

    public InfrastructureHealthService(
        AppDbContext dbContext,
        IDatabaseMigrationService migrationService,
        ILogger<InfrastructureHealthService> logger,
        IEnumerable<IEInvoiceProvider>? invoiceProviders = null)
    {
        _dbContext = dbContext;
        _migrationService = migrationService;
        _logger = logger;
        _invoiceProviders = invoiceProviders?.ToList() ?? [];
    }

    public async Task<InfrastructureHealthReportDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<InfrastructureHealthCheckDto>();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            checks.Add(new InfrastructureHealthCheckDto("Database", canConnect ? "Healthy" : "Unhealthy", canConnect ? "Kết nối database thành công." : "Không kết nối được database."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed.");
            checks.Add(new InfrastructureHealthCheckDto("Database", "Unhealthy", ex.Message));
        }

        try
        {
            var migrationStatus = await _migrationService.ValidatePendingMigrationsAsync(cancellationToken);
            var pendingMessage = migrationStatus.PendingMigrations.Count == 0
                ? "Không có migration pending."
                : $"Có {migrationStatus.PendingMigrations.Count} migration pending.";
            checks.Add(new InfrastructureHealthCheckDto("Migrations", migrationStatus.PendingMigrations.Count == 0 ? "Healthy" : "Warning", pendingMessage));

            foreach (var warning in migrationStatus.SeedWarnings)
            {
                checks.Add(new InfrastructureHealthCheckDto("Seed data", "Warning", warning));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration health check failed.");
            checks.Add(new InfrastructureHealthCheckDto("Migrations", "Unhealthy", ex.Message));
        }

        try
        {
            var configuredProviders = await _dbContext.InvoiceProviderConfigurations
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.ProviderCode,
                    x.EndpointUrl,
                    x.TenantCode,
                    x.Username,
                    x.ProtectedSecret
                })
                .ToListAsync(cancellationToken);

            var configuredProviderCount = configuredProviders.Count;
            var realProviderCodes = _invoiceProviders
                .Where(x => x is not UnavailableEInvoiceProvider)
                .Select(x => NormalizeProviderCode(x.ProviderCode))
                .Where(x => x.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var configuredProviderCodes = configuredProviders
                .Select(x => NormalizeProviderCode(x.ProviderCode))
                .Where(x => x.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingAdapterCodes = configuredProviderCodes
                .Where(x => !realProviderCodes.Contains(x))
                .OrderBy(x => x)
                .ToArray();
            var incompleteConfigurationCount = configuredProviders.Count(x =>
                string.IsNullOrWhiteSpace(x.ProviderCode)
                || string.IsNullOrWhiteSpace(x.EndpointUrl)
                || string.IsNullOrWhiteSpace(x.TenantCode)
                || string.IsNullOrWhiteSpace(x.Username)
                || string.IsNullOrWhiteSpace(x.ProtectedSecret));
            var hasRealInvoiceProvider = realProviderCodes.Count > 0;

            string invoiceStatus;
            string invoiceMessage;
            if (!hasRealInvoiceProvider)
            {
                invoiceStatus = "Warning";
                invoiceMessage = configuredProviderCount > 0
                    ? $"Có {configuredProviderCount} cấu hình provider trong database nhưng chưa có adapter thật; phát hành hóa đơn đang bị khóa an toàn."
                    : "Chưa cấu hình adapter hóa đơn điện tử thật hoặc provider; phát hành hóa đơn đang bị khóa an toàn.";
            }
            else if (incompleteConfigurationCount > 0)
            {
                invoiceStatus = "Warning";
                invoiceMessage = $"Có {incompleteConfigurationCount} cấu hình provider thiếu endpoint, tenant, username hoặc secret; chưa đủ điều kiện phát hành hóa đơn.";
            }
            else if (missingAdapterCodes.Length > 0)
            {
                invoiceStatus = "Warning";
                invoiceMessage = $"Provider {string.Join(", ", missingAdapterCodes)} chưa có adapter thật tương ứng; phát hành hóa đơn đang bị khóa an toàn.";
            }
            else
            {
                invoiceStatus = "Healthy";
                invoiceMessage = $"Đã có adapter hóa đơn điện tử thật và {configuredProviderCount} cấu hình provider hợp lệ đang hoạt động.";
            }
            checks.Add(new InfrastructureHealthCheckDto("E-invoice", invoiceStatus, invoiceMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-invoice provider configuration health check failed.");
            checks.Add(new InfrastructureHealthCheckDto(
                "E-invoice",
                "Unhealthy",
                "Không đọc được cấu hình provider hóa đơn điện tử từ database."));
        }

        var status = checks.Any(x => x.Status == "Unhealthy") ? "Unhealthy" : checks.Any(x => x.Status == "Warning") ? "Warning" : "Healthy";
        return new InfrastructureHealthReportDto(status, DateTime.UtcNow, checks);
    }

    private static string NormalizeProviderCode(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
