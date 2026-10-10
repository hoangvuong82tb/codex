using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class WindowsPrinterSettingsServiceTests
{
    [Fact]
    public async Task GetAvailablePrintersAsync_reads_the_local_spooler_without_mock_rows()
    {
        var service = new WindowsPrinterSettingsService();

        var printers = await service.GetAvailablePrintersAsync();

        printers.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Name)
            && !string.IsNullOrWhiteSpace(x.SharePath)
            && new[] { "Online", "Online (mặc định)", "Offline", "Error", "Busy" }.Contains(x.Status));
    }

    [Fact]
    public async Task GetAvailablePrintersAsync_honors_cancellation()
    {
        var service = new WindowsPrinterSettingsService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = async () => await service.GetAvailablePrintersAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetPrinterPermissionsAsync_reads_real_acl_rows_or_reports_real_read_status()
    {
        var service = new WindowsPrinterSettingsService();

        var permissions = await service.GetPrinterPermissionsAsync();

        permissions.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.PrinterName)
            && !string.IsNullOrWhiteSpace(x.ReadStatus)
            && (x.ReadStatus == "Read" || x.ReadStatus.StartsWith("Win32:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GetPrinterPermissionsAsync_honors_cancellation()
    {
        var service = new WindowsPrinterSettingsService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = async () => await service.GetPrinterPermissionsAsync(cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
