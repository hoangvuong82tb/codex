using ASMILE.BO.App.Services;
using FluentAssertions;
using System.Reflection;

namespace ASMILE.BO.Tests.Unit;

public sealed class NavigationCatalogTests
{
    [Fact]
    public void Report_center_is_the_only_report_module_in_the_desktop_catalog()
    {
        var service = new NavigationService(null!, null!, null!, null!);

        var reportModules = service.GetModuleCatalog()
            .Where(module => module.Key.Contains("report", StringComparison.OrdinalIgnoreCase))
            .ToList();

        reportModules.Select(module => module.Key)
            .Should().Equal("report-center");
        reportModules[0].ShowOnDesktop.Should().BeTrue();
    }

    [Theory]
    [InlineData("GENERAL_LEDGER")]
    [InlineData("VAT_OUTPUT")]
    [InlineData("BALANCE_SHEET")]
    [InlineData("STOCK_CARD")]
    [InlineData("TT200_COMPLIANCE_ISSUES")]
    public void Report_links_always_open_the_canonical_report_center(string reportCode)
    {
        var method = typeof(NavigationService).GetMethod("ResolveReportRoute", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        method!.Invoke(null, [reportCode]).Should().Be("report-center");
    }
}
