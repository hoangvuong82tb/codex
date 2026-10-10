using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

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
        await EnsureVirtualFolderCatalogAsync(cancellationToken);
        // Company-scoped seed validation is intentionally deferred until the
        // application has selected a company.  Running it here would evaluate
        // the global CompanyId filter with a null scope and report valid legacy
        // foundation rows as missing.
        _logger.LogInformation("Database schema is ready; company-scoped seed validation is deferred until scope initialization.");
    }

    private async Task EnsureVirtualFolderCatalogAsync(CancellationToken cancellationToken)
    {
        // VirtualDrive rows are model-seeded, but the folder taxonomy belongs
        // to a separate table and older databases may have no rows there.
        // Materialize the system catalog once so VFC reads persisted paths in
        // production instead of silently falling back to an in-memory tree.
        var defaultFolders = VirtualFileCenterSeed.CreateDefaultFolders(DateTime.UtcNow);
        if (defaultFolders.Count == 0)
        {
            return;
        }

        var existingPaths = await _dbContext.VirtualFolders
            .Where(x => x.IsSystem)
            .Select(x => x.VirtualPath)
            .ToListAsync(cancellationToken);
        var existingPathSet = existingPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = defaultFolders
            .Where(x => !existingPathSet.Contains(x.VirtualPath))
            .ToList();
        if (missing.Count == 0)
        {
            return;
        }

        // A custom deployment may already contain some folders.  Insert only
        // missing system paths and preserve all user-defined catalog rows.
        _dbContext.VirtualFolders.AddRange(missing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Materialized {Count} missing system VFC folder catalog rows.", missing.Count);
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
            var pendingNames = string.Join(", ", status.PendingMigrations.Take(10));
            var suffix = status.PendingMigrations.Count > 10 ? $" (+{status.PendingMigrations.Count - 10} more)" : string.Empty;
            throw new InvalidOperationException(
                $"Database schema is not ready: {status.PendingMigrations.Count} migration(s) are pending ({pendingNames}{suffix}). " +
                "Apply them through the approved database deployment process before starting the application.");
        }

        // Migration history can be complete while a database was restored from an
        // incomplete backup or altered manually.  Compare the EF model with the
        // actual SQL Server catalog so the application fails at startup with the
        // real missing columns instead of showing a cascade of per-screen errors.
        if (_dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            var missingColumns = await FindMissingSqlServerColumnsAsync(cancellationToken);
            if (missingColumns.Count > 0)
            {
                var preview = string.Join(", ", missingColumns.Take(20));
                var suffix = missingColumns.Count > 20 ? $" (+{missingColumns.Count - 20} more)" : string.Empty;
                _logger.LogError("Database schema validation found {MissingCount} missing EF column(s): {MissingColumns}{Suffix}", missingColumns.Count, preview, suffix);
                throw new InvalidOperationException($"Database schema is incomplete: thiếu {missingColumns.Count} cột ({preview}{suffix}).");
            }

            _logger.LogInformation("Database schema validation: EF model columns are present in the SQL Server catalog.");
        }
    }

    private async Task<IReadOnlyList<string>> FindMissingSqlServerColumnsAsync(CancellationToken cancellationToken)
    {
        var requiredTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entityType in _dbContext.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            var tableKey = BuildSchemaKey(entityType.GetSchema(), tableName, string.Empty).TrimEnd('.');
            requiredTables.Add(tableKey);
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    requiredColumns.Add(BuildSchemaKey(entityType.GetSchema(), tableName, columnName));
                }
            }
        }

        var connection = _dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS";
            var actualTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var actualColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                {
                    continue;
                }

                var tableKey = BuildSchemaKey(reader.GetString(0), reader.GetString(1), string.Empty).TrimEnd('.');
                actualTables.Add(tableKey);
                actualColumns.Add(BuildSchemaKey(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }

            var missingTables = requiredTables
                .Where(table => !actualTables.Contains(table))
                .Select(table => $"{table} [table missing]");
            var missingColumns = requiredColumns
                .Where(column => actualTables.Contains(column[..column.LastIndexOf('.')]) && !actualColumns.Contains(column));

            return missingTables
                .Concat(missingColumns)
                .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string BuildSchemaKey(string? schema, string table, string column)
        => $"{schema ?? "dbo"}.{table}.{column}";

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

    public async Task<DatabasePreflightDto> GetPreflightAsync(CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        var pending = canConnect ? (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList() : [];
        var applied = canConnect ? (await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList() : [];
        var seedWarnings = canConnect
            ? await ReadSeedWarningsSafelyAsync(cancellationToken)
            : ["Không thể kiểm tra seed vì chưa kết nối được database."];

        if (!_dbContext.Database.IsRelational())
        {
            return new DatabasePreflightDto(
                canConnect,
                _dbContext.Database.ProviderName ?? "(unknown)",
                "(non-relational)",
                "(non-relational)",
                "(none)",
                CreateFingerprint([]),
                pending,
                applied,
                seedWarnings);
        }

        var connection = _dbContext.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var tables = await ReadVisibleTablesAsync(connection, cancellationToken);
            var builder = connection is SqlConnection sqlConnection
                ? new SqlConnectionStringBuilder(sqlConnection.ConnectionString)
                : null;
            return new DatabasePreflightDto(
                canConnect,
                _dbContext.Database.ProviderName ?? "(unknown)",
                builder?.DataSource ?? connection.DataSource ?? "(unknown)",
                builder?.InitialCatalog ?? connection.Database ?? "(unknown)",
                "dbo",
                CreateFingerprint(tables),
                pending,
                applied,
                seedWarnings);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<IReadOnlyList<string>> ReadVisibleTablesAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_SCHEMA + N'.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_SCHEMA, TABLE_NAME";
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                tables.Add(reader.GetString(0));
            }
        }

        return tables;
    }

    private static string CreateFingerprint(IEnumerable<string> values)
    {
        var payload = string.Join('\n', values.OrderBy(value => value, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private async Task<IReadOnlyList<string>> ReadSeedWarningsSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ValidateSeedConsistencyAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Seed consistency check skipped because the configured schema is incomplete.");
            return ["Không thể kiểm tra seed vì schema hiện tại chưa đầy đủ."];
        }
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Assigns legacy foundation rows that were created before the company scope
    /// existed to the selected company.  Older installations legitimately contain
    /// NULL CompanyId on these seed tables; leaving them unassigned makes the
    /// global company filter hide the standard accounting, tax and report data.
    /// The helper only adopts rows when the target company has no rows in that
    /// table, which keeps existing company-specific configuration untouched.
    /// </summary>
    public async Task<int> EnsureLegacyFoundationScopeAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return 0;
        }

        var adopted = 0;
        adopted += await AdoptUnscopedRowsAsync(_dbContext.BankAccounts, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.CashFunds, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.CompanyAccountingProfiles, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.FinancialStatementMappings, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.FixedAssetCategories, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.ReportTemplates, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.TaxCodes, companyId, cancellationToken);
        adopted += await AdoptUnscopedRowsAsync(_dbContext.ToolAssetCategories, companyId, cancellationToken);

        if (adopted > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Assigned {Count} legacy foundation rows to company scope {CompanyId}.",
                adopted,
                companyId);
        }

        return adopted;
    }

    private static async Task<int> AdoptUnscopedRowsAsync<TEntity>(
        DbSet<TEntity> set,
        Guid companyId,
        CancellationToken cancellationToken)
        where TEntity : class, ICompanyScoped
    {
        var hasScopedRows = await set
            .IgnoreQueryFilters()
            .AnyAsync(x => x.CompanyId == companyId, cancellationToken);
        if (hasScopedRows)
        {
            return 0;
        }

        var unscopedRows = await set
            .IgnoreQueryFilters()
            .Where(x => x.CompanyId == null)
            .ToListAsync(cancellationToken);
        if (unscopedRows.Count == 0)
        {
            return 0;
        }

        foreach (var row in unscopedRows)
        {
            row.CompanyId = companyId;
        }

        return unscopedRows.Count;
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
