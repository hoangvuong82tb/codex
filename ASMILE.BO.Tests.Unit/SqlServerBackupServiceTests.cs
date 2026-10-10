using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Services;

namespace ASMILE.BO.Tests.Unit;

public sealed class SqlServerBackupServiceTests
{
    [Fact]
    public async Task GetRecentAsync_reads_real_backup_files_and_ignores_other_files()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory, "asmile_backup_older.bak"), new byte[] { 1 });
            await File.WriteAllBytesAsync(Path.Combine(directory, "not-a-backup.txt"), new byte[] { 1 });

            await using var db = CreateSqlServerContext("HotelAlpha");
            var service = new SqlServerBackupService(
                db,
                Options.Create(new DatabaseOptions { BackupDirectory = directory }),
                new PathSecurityService(),
                new CurrentUserService());

            var result = await service.GetRecentAsync();

            result.Should().ContainSingle();
            result[0].FileName.Should().Be("asmile_backup_older.bak");
            result[0].Size.Should().Be(1);
            result[0].Status.Should().Be("Chưa xác thực", "enumerating a backup file does not run RESTORE VERIFYONLY");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetRecentAsync_shows_current_database_and_legacy_backups_but_not_other_databases()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var alphaPrefix = GetDatabaseBackupPrefix("HotelAlpha");
            var betaPrefix = GetDatabaseBackupPrefix("HotelBeta");
            await File.WriteAllBytesAsync(Path.Combine(directory, $"{alphaPrefix}today.bak"), [1]);
            await File.WriteAllBytesAsync(Path.Combine(directory, $"{betaPrefix}today.bak"), [2]);
            await File.WriteAllBytesAsync(Path.Combine(directory, "asmile_backup_legacy.bak"), [3]);
            await File.WriteAllBytesAsync(Path.Combine(directory, "not-a-backup.txt"), [4]);

            await using var db = CreateSqlServerContext("HotelAlpha");
            var service = new SqlServerBackupService(
                db,
                Options.Create(new DatabaseOptions { BackupDirectory = directory }),
                new PathSecurityService(),
                new CurrentUserService());

            var result = await service.GetRecentAsync();

            result.Select(record => record.FileName).Should().BeEquivalentTo(
                $"{alphaPrefix}today.bak",
                "asmile_backup_legacy.bak");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreAsync_rejects_a_database_scoped_backup_from_another_database()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var foreignBackup = Path.Combine(directory, $"{GetDatabaseBackupPrefix("HotelBeta")}foreign.bak");
            await File.WriteAllBytesAsync(foreignBackup, [1]);
            await using var db = CreateSqlServerContext("HotelAlpha");
            var service = new SqlServerBackupService(
                db,
                Options.Create(new DatabaseOptions { BackupDirectory = directory, AllowDatabaseRestore = true }),
                new PathSecurityService(),
                new CurrentUserService());

            var action = () => service.RestoreAsync(foreignBackup);

            await action.Should().ThrowAsync<FileNotFoundException>();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RetentionAsync_only_deletes_old_backups_for_the_current_database()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var alphaPrefix = GetDatabaseBackupPrefix("HotelAlpha");
            var betaPrefix = GetDatabaseBackupPrefix("HotelBeta");
            var alphaOld = Path.Combine(directory, $"{alphaPrefix}old.bak");
            var alphaCurrent = Path.Combine(directory, $"{alphaPrefix}current.bak");
            var betaBackup = Path.Combine(directory, $"{betaPrefix}keep.bak");
            var legacyBackup = Path.Combine(directory, "asmile_backup_legacy.bak");
            foreach (var path in new[] { alphaOld, alphaCurrent, betaBackup, legacyBackup })
            {
                await File.WriteAllBytesAsync(path, [1]);
            }
            File.SetCreationTimeUtc(alphaOld, DateTime.UtcNow.AddDays(-2));
            File.SetCreationTimeUtc(alphaCurrent, DateTime.UtcNow);

            var retention = typeof(SqlServerBackupService).GetMethod("ApplyRetentionAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            var operation = (Task)retention.Invoke(null, [directory, alphaCurrent, alphaPrefix, 1, CancellationToken.None])!;
            await operation;

            File.Exists(alphaCurrent).Should().BeTrue();
            File.Exists(alphaOld).Should().BeFalse();
            File.Exists(betaBackup).Should().BeTrue();
            File.Exists(legacyBackup).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreAsync_is_disabled_by_default()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var service = new SqlServerBackupService(
            db,
            Options.Create(new DatabaseOptions()),
            new PathSecurityService(),
            new CurrentUserService());

        var action = () => service.RestoreAsync("ignored.bak");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đang bị khóa*");
    }

    private static AppDbContext CreateSqlServerContext(string databaseName)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer($"Server=(local);Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    private static string GetDatabaseBackupPrefix(string databaseName)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(databaseName.ToUpperInvariant())));
        return $"asmile_backup_db_{hash}_";
    }
}
