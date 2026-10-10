using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Infrastructure.Options;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class WindowsServiceStatusServiceTests
{
    [Fact]
    public async Task GetServicesAsync_reads_real_windows_services_without_mock_heartbeat()
    {
        var service = new WindowsServiceStatusService();

        var services = await service.GetServicesAsync();

        if (OperatingSystem.IsWindows())
        {
            services.Should().NotBeEmpty();
        }

        services.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.ServiceName)
            && !string.IsNullOrWhiteSpace(x.DisplayName)
            && new[]
            {
                "Stopped", "Start pending", "Stop pending", "Running",
                "Continue pending", "Pause pending", "Paused", "Unknown"
            }.Contains(x.Status)
            && x.LastHeartbeatAt == null);
    }

    [Fact]
    public async Task RestartServiceAsync_is_fail_closed_and_honors_cancellation()
    {
        var service = new WindowsServiceStatusService();

        var action = () => service.RestartServiceAsync("any-service");

        await action.Should().ThrowAsync<InvalidOperationException>();

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = () => service.RestartServiceAsync("any-service", cancellation.Token);
        await cancelled.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RestartServiceAsync_requires_an_explicit_allow_list()
    {
        var service = new WindowsServiceStatusService(Microsoft.Extensions.Options.Options.Create(
            new ServiceSupervisorOptions { AllowRestart = true, AllowedServiceNames = ["allowed-service"] }));

        var action = () => service.RestartServiceAsync("other-service");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*allow-list*");
    }
}
