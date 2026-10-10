using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Tests.Unit;

public sealed class ConfiguredErrorLogQueryServiceTests
{
    [Fact]
    public async Task Reads_only_error_and_fatal_events_from_configured_rolling_file_and_redacts_secrets()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-error-log-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "asmile-bo-20261002.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            await File.WriteAllTextAsync(path,
                $"[{timestamp} INF] routine startup{Environment.NewLine}" +
                $"[{timestamp} ERR] Request failed password=super-secret{Environment.NewLine}" +
                $"   at ASMILE.Tests.Run() token=abc123{Environment.NewLine}" +
                $"[{timestamp} FTL] Cannot connect https://service-user:service-pass@example.test/api{Environment.NewLine}");
            await File.WriteAllTextAsync(Path.Combine(directory, "asmile-bo-debug.log"),
                $"[{timestamp} FTL] unrelated file password=must-not-be-read{Environment.NewLine}");
            var service = CreateService(path);

            var snapshot = await service.GetRecentErrorsAsync(100);

            snapshot.IsConfigured.Should().BeTrue();
            snapshot.IsAvailable.Should().BeTrue();
            snapshot.Entries.Should().HaveCount(2);
            snapshot.Entries[0].Level.Should().Be("FTL");
            snapshot.Entries[1].Level.Should().Be("ERR");
            var rendered = string.Join(" ", snapshot.Entries.Select(x => x.Message));
            rendered.Should().Contain("password=***").And.Contain("token=***").And.Contain("https://***:***@example.test");
            rendered.Should().NotContain("super-secret").And.NotContain("abc123").And.NotContain("service-pass").And.NotContain("must-not-be-read");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Reports_unconfigured_sink_without_searching_other_files()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new ConfiguredErrorLogQueryService(configuration);

        var snapshot = await service.GetRecentErrorsAsync();

        snapshot.IsConfigured.Should().BeFalse();
        snapshot.IsAvailable.Should().BeFalse();
        snapshot.Entries.Should().BeEmpty();
    }

    private static ConfiguredErrorLogQueryService CreateService(string logPath)
    {
        var values = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "File",
            ["Serilog:WriteTo:0:Args:path"] = logPath,
            ["Serilog:WriteTo:0:Args:rollingInterval"] = "Day"
        };
        return new ConfiguredErrorLogQueryService(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}
