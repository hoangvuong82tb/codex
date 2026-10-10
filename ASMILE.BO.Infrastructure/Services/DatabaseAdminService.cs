using System.Data.Common;
using System.Data;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Database administration facade backed by the configured EF database.
/// This intentionally never reports a successful operation without executing
/// the corresponding database check/policy-gated migration.
/// </summary>
public sealed class DatabaseAdminService : IDatabaseAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly IDatabaseMigrationService _migrationService;

    public DatabaseAdminService(AppDbContext dbContext, IDatabaseMigrationService migrationService)
    {
        _dbContext = dbContext;
        _migrationService = migrationService;
    }

    public async Task<SystemConnectionDto> GetPrimaryConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var details = ReadConnectionDetails(connection);
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return new SystemConnectionDto
        {
            Id = 1,
            Name = "Primary SQL",
            Type = connection.GetType().Name.Contains("Sql", StringComparison.OrdinalIgnoreCase) ? "SQL Server" : connection.GetType().Name,
            Host = details.Host,
            Port = details.Port,
            DatabaseName = details.DatabaseName,
            Status = canConnect ? "Connected" : "Disconnected",
            LastCheckedAt = DateTime.Now
        };
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        => _dbContext.Database.CanConnectAsync(cancellationToken);

    public async Task<IReadOnlyList<DatabaseObjectMetadataDto>> GetDatabaseObjectsAsync(CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var result = new List<DatabaseObjectMetadataDto>();
            ReadSchemaRows(connection, "Tables", "TABLE", result);
            ReadSchemaRows(connection, "Views", "VIEW", result);
            ReadSchemaRows(connection, "Procedures", "PROCEDURE", result);
            return result
                .OrderBy(x => x.ObjectType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
    }

    public Task RunMigrationAsync(CancellationToken cancellationToken = default)
        => _migrationService.InitializeAsync(cancellationToken);

    public async Task RunMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        // Maintenance is deliberately a real, read-only health pass until a
        // concrete maintenance policy is configured. Never silently succeed.
        if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Database maintenance cannot start because the configured database is unreachable.");
        }

        await _migrationService.ValidateSeedConsistencyAsync(cancellationToken);
    }

    private static (string Host, int Port, string DatabaseName) ReadConnectionDetails(DbConnection connection)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
        var host = Read(builder, "Server") ?? Read(builder, "Data Source") ?? string.Empty;
        var database = Read(builder, "Database") ?? Read(builder, "Initial Catalog") ?? string.Empty;
        var port = 1433;
        var separator = host.LastIndexOf(',');
        if (separator >= 0 && int.TryParse(host[(separator + 1)..], out var parsedPort))
        {
            host = host[..separator];
            port = parsedPort;
        }

        return (host, port, database);
    }

    private static string? Read(DbConnectionStringBuilder builder, string key)
        => builder.TryGetValue(key, out var value) ? Convert.ToString(value) : null;

    private static void ReadSchemaRows(DbConnection connection, string collectionName, string objectType, ICollection<DatabaseObjectMetadataDto> result)
    {
        var table = connection.GetSchema(collectionName);
        foreach (DataRow row in table.Rows)
        {
            var name = ReadColumn(row, "TABLE_NAME") ?? ReadColumn(row, "ROUTINE_NAME") ?? ReadColumn(row, "OBJECT_NAME");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            result.Add(new DatabaseObjectMetadataDto
            {
                Schema = ReadColumn(row, "TABLE_SCHEMA") ?? ReadColumn(row, "ROUTINE_SCHEMA") ?? string.Empty,
                Name = name,
                ObjectType = objectType
            });
        }
    }

    private static string? ReadColumn(DataRow row, string columnName)
        => row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value
            ? Convert.ToString(row[columnName])
            : null;
}
