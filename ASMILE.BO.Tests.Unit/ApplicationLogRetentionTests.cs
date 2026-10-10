using ASMILE.BO.App.Services;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ApplicationLogRetentionTests
{
    [Fact]
    public void Retention_setting_is_editable_validated_and_defaults_to_the_existing_fourteen_day_policy()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(setting => setting.Key == ApplicationLogRetentionService.SettingKey);
        var validator = new SettingsValidator();

        definition.IsReadOnly.Should().BeFalse();
        definition.DefaultValue.Should().Be("14");
        validator.Validate(definition, "1").IsValid.Should().BeTrue();
        validator.Validate(definition, "3650").IsValid.Should().BeTrue();
        validator.Validate(definition, "0").IsValid.Should().BeFalse();
        validator.Validate(definition, "3651").IsValid.Should().BeFalse();
        validator.Validate(definition, "abc").IsValid.Should().BeFalse();
    }

    [Fact]
    public void PruneExpiredFiles_deletes_only_expired_daily_asmile_logs_in_the_target_folder()
    {
        var directory = CreateTempDirectory();
        try
        {
            var nestedDirectory = Directory.CreateDirectory(Path.Combine(directory, "nested"));
            CreateFile(directory, "asmile-bo-20260924.log");
            CreateFile(directory, "asmile-bo-20260925.log");
            CreateFile(directory, "asmile-bo-20261008.log");
            CreateFile(directory, "asmile-bo-20260230.log");
            CreateFile(directory, "other-app-20260901.log");
            CreateFile(nestedDirectory.FullName, "asmile-bo-20260901.log");

            var removed = ApplicationLogRetention.PruneExpiredFiles(
                directory,
                retentionDays: 14,
                now: new DateTime(2026, 10, 8, 12, 0, 0));

            removed.Should().Be(1);
            File.Exists(Path.Combine(directory, "asmile-bo-20260924.log")).Should().BeFalse();
            File.Exists(Path.Combine(directory, "asmile-bo-20260925.log")).Should().BeTrue();
            File.Exists(Path.Combine(directory, "asmile-bo-20261008.log")).Should().BeTrue();
            File.Exists(Path.Combine(directory, "asmile-bo-20260230.log")).Should().BeTrue();
            File.Exists(Path.Combine(directory, "other-app-20260901.log")).Should().BeTrue();
            File.Exists(Path.Combine(nestedDirectory.FullName, "asmile-bo-20260901.log")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PruneExpiredFiles_rejects_out_of_range_retention_without_touching_files()
    {
        var directory = CreateTempDirectory();
        try
        {
            CreateFile(directory, "asmile-bo-20200101.log");

            var act = () => ApplicationLogRetention.PruneExpiredFiles(
                directory,
                retentionDays: 0,
                now: new DateTime(2026, 10, 8));

            act.Should().Throw<ArgumentOutOfRangeException>();
            File.Exists(Path.Combine(directory, "asmile-bo-20200101.log")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"asmile-log-retention-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateFile(string directory, string name)
        => File.WriteAllText(Path.Combine(directory, name), "test log");
}
