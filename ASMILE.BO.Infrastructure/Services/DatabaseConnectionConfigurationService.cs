using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Security;
using ASMILE.BO.Shared.Storage;
using Microsoft.Data.SqlClient;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class DatabaseConnectionConfigurationService : IDatabaseConnectionConfigurationService
{
    private const string ConfigurationFileName = "database-connection.json";
    private readonly string _activeConnectionString;
    private readonly ISecretProtector _secretProtector;

    public DatabaseConnectionConfigurationService(string activeConnectionString, ISecretProtector secretProtector)
    {
        _activeConnectionString = activeConnectionString;
        _secretProtector = secretProtector;
    }

    public DatabaseConnectionConfigurationDto GetCurrent()
    {
        var builder = new SqlConnectionStringBuilder(_activeConnectionString);
        return new DatabaseConnectionConfigurationDto
        {
            Server = builder.DataSource,
            Database = builder.InitialCatalog,
            AuthenticationMode = builder.IntegratedSecurity ? "Windows" : "Sql",
            UserName = builder.IntegratedSecurity ? string.Empty : builder.UserID,
            HasPassword = !builder.IntegratedSecurity && !string.IsNullOrEmpty(builder.Password)
        };
    }

    public async Task<DatabaseConnectionTestResult> TestAsync(
        DatabaseConnectionConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = BuildConnectionString(request);
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return new DatabaseConnectionTestResult(true, $"Kết nối thành công tới {builder.DataSource} / {builder.InitialCatalog}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or SqlException or InvalidOperationException)
        {
            // Never surface provider exception text: some providers include connection details.
            return new DatabaseConnectionTestResult(false, "Không kết nối được. Hãy kiểm tra máy chủ, database, quyền truy cập và thông tin xác thực.");
        }
    }

    public async Task SaveAsync(DatabaseConnectionConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var builder = BuildConnectionString(request);
        await using (var connection = new SqlConnection(builder.ConnectionString))
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    "Không lưu cấu hình vì không thể kết nối tới SQL Server với thông tin đã nhập.", ex);
            }
        }

        var protectedConnectionString = _secretProtector.Protect(builder.ConnectionString);
        var json = JsonSerializer.Serialize(new
        {
            Database = new { UserConnectionOverride = protectedConnectionString }
        }, new JsonSerializerOptions { WriteIndented = true });

        var path = ApplicationDataPaths.GetUserSettingsFile(ConfigurationFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private SqlConnectionStringBuilder BuildConnectionString(DatabaseConnectionConfigurationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var server = request.Server.Trim();
        var database = request.Database.Trim();
        if (server.Length == 0 || database.Length == 0)
            throw new ArgumentException("Server and database are required.");

        var builder = new SqlConnectionStringBuilder(_activeConnectionString)
        {
            DataSource = server,
            InitialCatalog = database,
            ConnectTimeout = Math.Clamp(new SqlConnectionStringBuilder(_activeConnectionString).ConnectTimeout, 5, 60)
        };

        if (string.Equals(request.AuthenticationMode, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            builder.IntegratedSecurity = true;
            builder.UserID = string.Empty;
            builder.Password = string.Empty;
            builder.Remove("Authentication");
        }
        else if (string.Equals(request.AuthenticationMode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                throw new ArgumentException("SQL user name is required.");
            var current = new SqlConnectionStringBuilder(_activeConnectionString);
            var password = request.Password;
            if (string.IsNullOrEmpty(password)
                && !current.IntegratedSecurity
                && string.Equals(current.DataSource, server, StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.InitialCatalog, database, StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.UserID, request.UserName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                password = current.Password;
            }
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required for a new SQL authentication configuration.");

            builder.IntegratedSecurity = false;
            builder.UserID = request.UserName.Trim();
            builder.Password = password;
            builder.Remove("Authentication");
        }
        else
        {
            throw new ArgumentException("Unsupported SQL authentication mode.");
        }

        return builder;
    }
}
