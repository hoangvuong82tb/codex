using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class WindowsStartupAppsServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_reads_real_windows_startup_sources_without_fabricating_entries()
    {
        var service = new WindowsStartupAppsService();

        var snapshot = await service.GetSnapshotAsync();

        if (OperatingSystem.IsWindows())
        {
            snapshot.IsAvailable.Should().BeTrue();
            snapshot.ProviderStatus.Should().Contain("startup entry");
            snapshot.Entries.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Name)
                && !string.IsNullOrWhiteSpace(x.Command)
                && !string.IsNullOrWhiteSpace(x.Source));
        }
        else
        {
            snapshot.IsAvailable.Should().BeFalse();
            snapshot.Entries.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_honors_cancellation()
    {
        var service = new WindowsStartupAppsService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.GetSnapshotAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
