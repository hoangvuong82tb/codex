using System.Text.RegularExpressions;
using ASMILE.BO.App.Models;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FrontOfficeDesignSystemTests
{
    private static readonly string[] HubViews = ["FrontOfficeHubView.xaml", "ReservationHubView.xaml", "ReceptionHubView.xaml"];
    private static readonly string[] BrushProperties = ["Background", "Foreground", "BorderBrush", "Fill", "Stroke"];

    [Fact]
    public void FO_DS_U_001_to_003_hub_cards_hide_status_badges_and_keep_shared_hosts()
    {
        var views = HubViews.ToDictionary(name => name, ReadView);

        views["FrontOfficeHubView.xaml"].Should().Contain("FO.MainHubCardButton").And.NotContain("FO.HubBadge").And.Contain("FO.ComingSoonModalShell");
        views["ReservationHubView.xaml"].Should().Contain("FO.HubCardButton").And.NotContain("FO.HubBadge").And.Contain("FO.LargeModalShell");
        views["ReceptionHubView.xaml"].Should().Contain("FO.HubCardButton").And.NotContain("FO.HubBadge");
        ReadView("ReceptionReadOnlyWorkspaceView.xaml").Should().Contain("FO.ReadOnlyModalShell");
    }

    [Fact]
    public void FO_DS_U_003b_reception_read_only_cards_render_the_active_workspace_modal()
    {
        var receptionView = ReadView("ReceptionHubView.xaml");

        receptionView.Should().Contain("ReceptionReadOnlyWorkspaceViewModel");
        receptionView.Should().Contain("{Binding ModalService.ActiveModal}");
        receptionView.Should().Contain("{Binding ModalService.IsOpen");
    }

    [Fact]
    public void FO_DS_U_004_to_006_resources_are_type_safe_and_hub_views_keep_no_local_card_template()
    {
        var xaml = Directory.EnumerateFiles(Path.Combine(Root(), "ASMILE.BO.App"), "*.xaml", SearchOption.AllDirectories)
            .Where(path => path.Contains("FrontOffice", StringComparison.OrdinalIgnoreCase) || path.Contains("Reservation", StringComparison.OrdinalIgnoreCase) || path.Contains("Reception", StringComparison.OrdinalIgnoreCase) || path.EndsWith("FrontOfficeDesignSystem.xaml", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var mismatches = xaml.SelectMany(path => FindResourceTypeMismatches(File.ReadAllLines(path), path)).ToArray();
        mismatches.Should().BeEmpty();
        HubViews.Select(ReadView).Should().NotContain(view => view.Contains("<ControlTemplate TargetType=\"Button\"", StringComparison.Ordinal));
    }

    [Fact]
    public void FO_DS_U_007_to_009_shared_front_office_ui_contains_no_messagebox_property_id_or_data_access_dependency()
    {
        var sharedSources = Directory.EnumerateFiles(Path.Combine(Root(), "ASMILE.BO.App"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.Contains("FrontOfficeDesignSystem", StringComparison.OrdinalIgnoreCase) || path.Contains("FrontOfficeHub", StringComparison.OrdinalIgnoreCase) || path.Contains("ReservationHub", StringComparison.OrdinalIgnoreCase) || path.Contains("ReceptionHub", StringComparison.OrdinalIgnoreCase) || path.Contains("ModalWorkspace", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .ToArray();

        sharedSources.Should().NotContain(source => source.Contains("MessageBox", StringComparison.Ordinal));
        sharedSources.Should().NotContain(source => source.Contains("PropertyId", StringComparison.Ordinal));
        sharedSources.Should().NotContain(source => source.Contains("Repository", StringComparison.OrdinalIgnoreCase) || source.Contains("DbContext", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FO_DS_U_010_to_014_models_expose_accessible_names_and_state_contracts()
    {
        typeof(FrontOfficeHubCard).GetProperty(nameof(FrontOfficeHubCard.AutomationName)).Should().NotBeNull();
        typeof(ReservationHubCard).GetProperty(nameof(ReservationHubCard.AutomationName)).Should().NotBeNull();
        typeof(ReceptionHubCard).GetProperty(nameof(ReceptionHubCard.AutomationName)).Should().NotBeNull();
        typeof(FrontOfficeHubCard).GetProperty(nameof(FrontOfficeHubCard.IsActive)).Should().NotBeNull();
        typeof(ReservationHubCard).GetProperty(nameof(ReservationHubCard.IsComingSoon)).Should().NotBeNull();
        typeof(ReceptionHubCard).GetProperty(nameof(ReceptionHubCard.IsReadOnly)).Should().NotBeNull();
        typeof(ReceptionHubCard).GetProperty(nameof(ReceptionHubCard.IsComingSoon)).Should().NotBeNull();
    }

    private static IEnumerable<string> FindResourceTypeMismatches(IEnumerable<string> lines, string path)
    {
        foreach (var (line, index) in lines.Select((line, index) => (line, index)))
        {
            foreach (Match match in Regex.Matches(line, "(?<property>Background|Foreground|BorderBrush|Fill|Stroke|Color)=\\\"\\{StaticResource (?<resource>[^}]+)\\}\\\""))
            {
                var property = match.Groups["property"].Value;
                var resource = match.Groups["resource"].Value;
                if (BrushProperties.Contains(property) && resource.EndsWith("Color", StringComparison.Ordinal)) yield return $"{path}:{index + 1} {property} -> {resource}";
                if (property == "Color" && resource.EndsWith("Brush", StringComparison.Ordinal)) yield return $"{path}:{index + 1} Color -> {resource}";
            }
        }
    }

    private static string ReadView(string name) => File.ReadAllText(Path.Combine(Root(), "ASMILE.BO.App", "Views", name));

    private static string Root()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("ASMILE_BO_SOLUTION_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot)
            && File.Exists(Path.Combine(configuredRoot, "ASMILE.BO.sln"))
            && Directory.Exists(Path.Combine(configuredRoot, "ASMILE.BO.App", "Views")))
        {
            return Path.GetFullPath(configuredRoot);
        }

        var candidateRoots = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var candidateRoot in candidateRoots)
        {
            for (var directory = new DirectoryInfo(candidateRoot); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ASMILE.BO.sln"))) return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate ASMILE.BO.sln from the test output or current directory.");
    }
}
