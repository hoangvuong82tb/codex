using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using Xunit.Abstractions;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Services;

namespace ASMILE.BO.Tests.Integration;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerBackupIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public SqlServerBackupIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task LocalDb_backup_provider_creates_and_validates_a_real_backup()
    {
        var databaseName = $"ASMILE_BO_BACKUP_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var backupDirectory = Path.Combine(Path.GetTempPath(), $"asmile-backup-{Guid.NewGuid():N}");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            Directory.CreateDirectory(backupDirectory);

            var service = new SqlServerBackupService(
                db,
                Options.Create(new DatabaseOptions { BackupDirectory = backupDirectory }),
                new PathSecurityService(),
                new CurrentUserService());

            var backup = await service.BackupNowAsync();

            File.Exists(backup.FilePath).Should().BeTrue();
            new FileInfo(backup.FilePath).Length.Should().BeGreaterThan(0);
            (await service.ValidateBackupAsync(backup.FilePath)).Should().BeTrue();
            (await service.GetRecentAsync()).Should().ContainSingle(x => x.FilePath == backup.FilePath);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SqlExpress_service_identity_can_write_and_validate_a_real_backup_when_opted_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ASMILE_SQLSERVER_SERVICE_BACKUP_TEST"), "1", StringComparison.Ordinal))
        {
            _output.WriteLine("Skipped: set ASMILE_SQLSERVER_SERVICE_BACKUP_TEST=1 to run the opt-in SQL Express integration test.");
            return;
        }
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The SQL Express service-identity backup test requires Windows ACLs.");
        }

        var databaseName = $"ASMILE_BO_BACKUP_TEST_{Guid.NewGuid():N}";
        var connectionString = $"Server=.\\SQLEXPRESS;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ASMILE-BO", "Backups");
        var backupDirectory = Path.Combine(backupRoot, $".integration-test-{Guid.NewGuid():N}");
        var serviceIdentity = GetSqlExpressServiceIdentity();
        var expectedIdentity = Environment.GetEnvironmentVariable("ASMILE_SQLSERVER_SERVICE_IDENTITY");
        if (!string.IsNullOrWhiteSpace(expectedIdentity)
            && !string.Equals(expectedIdentity.Trim(), serviceIdentity, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"ASMILE_SQLSERVER_SERVICE_IDENTITY does not match the configured SQL Express service identity '{serviceIdentity}'.");
        }
        _output.WriteLine($"Granting isolated backup access to configured SQL Express service identity '{serviceIdentity}'.");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;

        await using var db = new AppDbContext(options);
        try
        {
            if (!Directory.Exists(backupRoot))
            {
                throw new DirectoryNotFoundException($"The configured SQL backup root does not exist: {backupRoot}");
            }
            Directory.CreateDirectory(backupDirectory);
            GrantIsolatedBackupDirectoryAccess(backupDirectory, serviceIdentity);
            await db.Database.MigrateAsync();

            var service = new SqlServerBackupService(
                db,
                Options.Create(new DatabaseOptions { BackupDirectory = backupDirectory }),
                new PathSecurityService(),
                new CurrentUserService());

            var backup = await service.BackupNowAsync();

            File.Exists(backup.FilePath).Should().BeTrue();
            new FileInfo(backup.FilePath).Length.Should().BeGreaterThan(0);
            (await service.ValidateBackupAsync(backup.FilePath)).Should().BeTrue();
            (await service.GetRecentAsync()).Should().Contain(x => x.FilePath == backup.FilePath);
        }
        finally
        {
            try
            {
                await db.Database.EnsureDeletedAsync();
            }
            finally
            {
                SqlConnection.ClearAllPools();
                if (Directory.Exists(backupDirectory))
                {
                    Directory.Delete(backupDirectory, recursive: true);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static string GetSqlExpressServiceIdentity()
    {
        const string serviceRegistryPath = @"SYSTEM\CurrentControlSet\Services\MSSQL$SQLEXPRESS";
        using var serviceKey = Registry.LocalMachine.OpenSubKey(serviceRegistryPath, writable: false);
        var serviceIdentity = serviceKey?.GetValue("ObjectName") as string;
        if (string.IsNullOrWhiteSpace(serviceIdentity))
        {
            throw new InvalidOperationException("Unable to read the configured service identity for MSSQL$SQLEXPRESS.");
        }

        return serviceIdentity.Trim();
    }

    [SupportedOSPlatform("windows")]
    private static void GrantIsolatedBackupDirectoryAccess(string directoryPath, string serviceIdentity)
    {
        var serviceSid = (SecurityIdentifier)new NTAccount(serviceIdentity).Translate(typeof(SecurityIdentifier));
        var currentUserSid = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Unable to resolve the current test user SID.");
        var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var administratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        var acl = new DirectorySecurity();
        acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        acl.AddAccessRule(new FileSystemAccessRule(currentUserSid, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(serviceSid, FileSystemRights.Modify, inheritance, PropagationFlags.None, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(systemSid, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(administratorsSid, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(directoryPath).SetAccessControl(acl);
    }
}
