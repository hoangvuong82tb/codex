using ASMILE.BO.Core.Entities;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class EfSystemSettingsServiceTests
{
    [Fact]
    public async Task GetAllAsync_reads_persisted_app_settings_and_maps_metadata_from_key_value()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"system-settings-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        db.AppSettings.AddRange(
            new AppSetting { Key = "Backup.Path", Value = @"D:\ASMILE-BO\Backups" },
            new AppSetting { Key = "GeneralLedger.AllowQuickPosting", Value = "true" },
            new AppSetting { Key = "GeneralLedger.DefaultCurrency", Value = "VND" });
        await db.SaveChangesAsync();

        var service = new EfSystemSettingsService(db);

        var rows = await service.GetAllAsync();

        rows.Should().HaveCount(3);
        rows.Should().BeInAscendingOrder(x => x.Key);
        rows.Single(x => x.Key == "Backup.Path").Should().Match<SystemSettingDto>(x =>
            x.Value == @"D:\ASMILE-BO\Backups" &&
            x.Group == "Backup" &&
            x.DataType == "string" &&
            x.Description == null &&
            x.IsEncrypted == null &&
            x.UpdatedAt == null &&
            x.UpdatedBy == null);
        rows.Single(x => x.Key == "GeneralLedger.AllowQuickPosting").DataType.Should().Be("bool");
    }

    [Fact]
    public async Task GetAllAsync_redacts_secret_values_without_inventing_encryption_metadata()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"system-settings-secret-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        db.AppSettings.AddRange(
            new AppSetting { Key = "Network.ApiKey", Value = "do-not-return-this" },
            new AppSetting { Key = "GeneralLedger.DefaultCurrency", Value = "VND" });
        await db.SaveChangesAsync();

        var rows = await new EfSystemSettingsService(db).GetAllAsync();

        rows.Single(x => x.Key == "Network.ApiKey").Should().Match<SystemSettingDto>(x =>
            x.Value == "[REDACTED]" && x.IsEncrypted == null);
        rows.Single(x => x.Key == "GeneralLedger.DefaultCurrency").Value.Should().Be("VND");
        (await db.AppSettings.SingleAsync(x => x.Key == "Network.ApiKey")).Value.Should().Be("do-not-return-this");
    }

    [Fact]
    public async Task GetAllAsync_honors_cancellation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"system-settings-cancel-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var service = new EfSystemSettingsService(db);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = async () => await service.GetAllAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
