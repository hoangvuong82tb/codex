using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockDatabaseAdminService : IDatabaseAdminService
{
    private readonly IConfiguration _configuration;
    private readonly ISecretProtector _secretProtector;

    public MockDatabaseAdminService(IConfiguration configuration, ISecretProtector secretProtector)
    {
        _configuration = configuration;
        _secretProtector = secretProtector;
    }

    public Task<SystemConnectionDto> GetPrimaryConnectionAsync(CancellationToken cancellationToken = default)
    {
        var configuredConnection = _configuration.GetSection("Database").GetValue<string>("ConnectionString") ?? string.Empty;
        var connectionString = _secretProtector.IsProtected(configuredConnection)
            ? _secretProtector.Unprotect(configuredConnection)
            : configuredConnection;

        var server = "localhost";
        var database = "ASMILE_BO";

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            server = ReadKey(builder, "Server") ?? ReadKey(builder, "Data Source") ?? server;
            database = ReadKey(builder, "Database") ?? ReadKey(builder, "Initial Catalog") ?? database;
        }
        catch
        {
            // keep defaults for mock display
        }

        return Task.FromResult(new SystemConnectionDto
        {
            Id = 1,
            Name = "Primary SQL",
            Type = "SQL Server",
            Host = server,
            Port = 1433,
            DatabaseName = database,
            Status = "Connected",
            LastCheckedAt = DateTime.Now
        });
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var ok = DateTime.Now.Second % 2 == 0;
        return Task.FromResult(ok);
    }

    public Task RunMigrationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RunMaintenanceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private static string? ReadKey(DbConnectionStringBuilder builder, string key)
    {
        return builder.TryGetValue(key, out var value) ? Convert.ToString(value) : null;
    }
}
