using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace ASMILE.BO.Infrastructure.Persistence;

public sealed class DatabaseMigrationService : IDatabaseMigrationService
{
    private static readonly string[] RequiredRoleMappings =
    [
        "Cash", "Bank", "Receivable", "Payable", "Inventory", "Revenue", "COGS", "VATInput", "VATOutput",
        "IncomeSummary", "RetainedEarnings", "FixedAsset", "Depreciation", "ToolAndSupply", "PrepaidExpense", "Expense"
    ];

    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly DatabaseOptions _options;
    private readonly IHostEnvironment _hostEnvironment;

    public DatabaseMigrationService(AppDbContext dbContext, ILogger<DatabaseMigrationService> logger, IOptions<DatabaseOptions> options, IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var mode = _options.MigrationMode;
        _logger.LogInformation("Database migration startup policy: Mode={Mode}; Environment={Environment}; Database={Database}", mode, _hostEnvironment.EnvironmentName, GetSafeDatabaseName());

        if (mode == DatabaseMigrationMode.Disabled)
        {
            _logger.LogInformation("Database migration is disabled by policy.");
            return;
        }

        if (mode == DatabaseMigrationMode.ValidateOnly)
        {
            await ValidateSchemaAsync(cancellationToken);
            return;
        }

        if (mode == DatabaseMigrationMode.DevelopmentAutoMigrate && !_hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("DevelopmentAutoMigrate is only permitted in the Development environment.");
        }

        if (mode == DatabaseMigrationMode.ApplyPendingMigrations && !_hostEnvironment.IsDevelopment() && !_options.AllowApplyPendingMigrations)
        {
            throw new InvalidOperationException("ApplyPendingMigrations outside Development requires Database:AllowApplyPendingMigrations=true.");
        }

        _logger.LogInformation("Applying pending database migrations after explicit policy approval.");
        await ApplyMigrationsAsync(cancellationToken);
        await EnsureVatIncludedColumnsAsync(cancellationToken);
        try
        {
            var warnings = await ValidateSeedConsistencyAsync(cancellationToken);
            foreach (var warning in warnings)
            {
                _logger.LogWarning("Seed consistency warning: {Warning}", warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Seed consistency check skipped due to incomplete schema — run Training Data seed to populate.");
        }
        _logger.LogInformation("Database is ready.");
    }

    private async Task ValidateSchemaAsync(CancellationToken cancellationToken)
    {
        var status = await ValidatePendingMigrationsAsync(cancellationToken);
        _logger.LogInformation("Database migration validation: CanConnect={CanConnect}; PendingCount={PendingCount}; AppliedCount={AppliedCount}", status.CanConnect, status.PendingMigrations.Count, status.AppliedMigrations.Count);
        if (!status.CanConnect)
        {
            throw new InvalidOperationException("Database migration validation could not connect to the configured database.");
        }

        if (status.PendingMigrations.Count > 0 && _options.FailOnPendingMigrations)
        {
            throw new InvalidOperationException($"Database schema is not ready: {status.PendingMigrations.Count} migration(s) are pending.");
        }
    }

    private string GetSafeDatabaseName()
    {
        if (!_dbContext.Database.IsRelational())
        {
            return "(non-relational)";
        }

        var connectionString = _dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "(not-configured)";
        }

        return new SqlConnectionStringBuilder(connectionString).InitialCatalog ?? "(unnamed)";
    }

    public async Task<MigrationStatusDto> ValidatePendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        var pending = canConnect ? await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken) : [];
        var applied = canConnect ? await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken) : [];
        var seedWarnings = canConnect ? await ValidateSeedConsistencyAsync(cancellationToken) : ["Không thể kiểm tra seed vì chưa kết nối được database."];
        return new MigrationStatusDto(canConnect, pending.ToList(), applied.ToList(), seedWarnings);
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task EnsureVatIncludedColumnsAsync(CancellationToken cancellationToken)
    {
        var commands = new[]
        {
            """
            IF COL_LENGTH('MenuItems', 'IsVatIncluded') IS NULL
                ALTER TABLE [MenuItems] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_MenuItems_IsVatIncluded] DEFAULT 1;
            """,
            """
            IF COL_LENGTH('BreakfastCafeOrderLines', 'IsVatIncluded') IS NULL
                ALTER TABLE [BreakfastCafeOrderLines] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_BreakfastCafeOrderLines_IsVatIncluded] DEFAULT 1;
            """,
            """
            IF COL_LENGTH('RestaurantExtraServices', 'IsVatIncluded') IS NULL
                ALTER TABLE [RestaurantExtraServices] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_RestaurantExtraServices_IsVatIncluded] DEFAULT 1;
            """,
            """
            IF COL_LENGTH('AlacarteOrderLines', 'IsVatIncluded') IS NULL
                ALTER TABLE [AlacarteOrderLines] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_AlacarteOrderLines_IsVatIncluded] DEFAULT 1;
            """,
            """
            IF COL_LENGTH('RestaurantOrderExtraServices', 'IsVatIncluded') IS NULL
                ALTER TABLE [RestaurantOrderExtraServices] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_RestaurantOrderExtraServices_IsVatIncluded] DEFAULT 1;
            """,
            """
            IF COL_LENGTH('RestaurantExtraServices', 'ImagePath') IS NULL
                ALTER TABLE [RestaurantExtraServices] ADD [ImagePath] nvarchar(500) NULL;
            """,
            """
            IF COL_LENGTH('RestaurantExtraServices', 'ImageUrl') IS NULL
                ALTER TABLE [RestaurantExtraServices] ADD [ImageUrl] nvarchar(500) NULL;
            """
        };

        foreach (var command in commands)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken);
            }
            catch (SqlException ex) when (ex.Message.Contains("already an object named", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "Skipped duplicate default-constraint creation while ensuring IsVatIncluded columns.");
            }
        }
    }

    public async Task<IReadOnlyList<string>> ValidateSeedConsistencyAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        if (!await _dbContext.AccountingStandards.AnyAsync(x => x.Code == "TT99" && x.IsActive, cancellationToken))
        {
            warnings.Add("Thiếu chuẩn kế toán mặc định TT99 hoặc TT99 chưa active.");
        }

        var mappings = await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.AccountingStandardCode == "TT99" && x.IsActive)
            .Select(x => x.RoleCode)
            .ToListAsync(cancellationToken);
        foreach (var role in RequiredRoleMappings.Except(mappings, StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add($"Thiếu AccountRoleMapping TT99 cho role {role}.");
        }

        if (!await _dbContext.AccountTemplates.AnyAsync(x => x.StandardCode == "TT99" && x.IsActive, cancellationToken))
        {
            warnings.Add("Thiếu AccountTemplate TT99.");
        }

        if (!await _dbContext.FinancialStatementMappings.AnyAsync(x => x.StandardCode == "TT99" && x.IsActive, cancellationToken))
        {
            warnings.Add("Thiếu FinancialStatementMapping TT99.");
        }

        var requiredTaxCodes = new[] { "VAT0", "VAT5", "VAT8", "VAT10" };
        var taxCodes = await _dbContext.TaxCodes.AsNoTracking().Select(x => x.Code).ToListAsync(cancellationToken);
        foreach (var code in requiredTaxCodes.Except(taxCodes, StringComparer.OrdinalIgnoreCase))
        {
            warnings.Add($"Thiếu mã thuế mặc định {code}.");
        }

        return warnings;
    }
}
