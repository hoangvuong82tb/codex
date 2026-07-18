using FluentAssertions;
using System.Text.RegularExpressions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationWorkspaceXamlResourceTypeTests
{
    private static readonly string[] BrushProperties = ["Background", "Foreground", "BorderBrush", "Fill", "Stroke"];

    [Fact]
    public void XAML_RES_U_001_brush_properties_do_not_reference_color_resources_and_color_properties_do_not_reference_brush_resources()
    {
        var lines = File.ReadAllLines(Path.Combine(FindSolutionRoot(), "ASMILE.BO.App", "Views", "ReservationWorkspaceView.xaml"));
        var mismatches = lines
            .SelectMany((line, index) => FindMismatches(line, index + 1))
            .ToArray();

        mismatches.Should().BeEmpty("ReservationWorkspaceView brush properties must use Brush resources and Color properties must use Color resources");
    }

    private static IEnumerable<string> FindMismatches(string line, int lineNumber)
    {
        foreach (Match match in Regex.Matches(line, "(?<property>Background|Foreground|BorderBrush|Fill|Stroke|Color)=\\\"\\{StaticResource (?<resource>[^}]+)\\}\\\""))
        {
            var property = match.Groups["property"].Value;
            var resource = match.Groups["resource"].Value;
            if (BrushProperties.Contains(property) && resource.EndsWith("Color", StringComparison.Ordinal))
                yield return $"Line {lineNumber}: {property} references Color resource {resource}";
            if (property == "Color" && resource.EndsWith("Brush", StringComparison.Ordinal))
                yield return $"Line {lineNumber}: Color references Brush resource {resource}";
        }

        foreach (Match match in Regex.Matches(line, "<Setter Property=\\\"(?<property>Background|Foreground|BorderBrush|Fill|Stroke|Color)\\\" Value=\\\"\\{StaticResource (?<resource>[^}]+)\\}\\\""))
        {
            var property = match.Groups["property"].Value;
            var resource = match.Groups["resource"].Value;
            if (BrushProperties.Contains(property) && resource.EndsWith("Color", StringComparison.Ordinal))
                yield return $"Line {lineNumber}: Setter {property} references Color resource {resource}";
            if (property == "Color" && resource.EndsWith("Brush", StringComparison.Ordinal))
                yield return $"Line {lineNumber}: Setter Color references Brush resource {resource}";
        }
    }

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ASMILE.BO.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate ASMILE.BO.sln from the test output directory.");
    }
}
