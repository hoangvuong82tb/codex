using ASMILE.BO.Shared.Storage;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ApplicationDataPathsTests
{
    [Fact]
    public void Writable_application_paths_are_outside_the_install_directory()
    {
        var installRoot = Path.GetFullPath(AppContext.BaseDirectory);
        var writablePaths = new[]
        {
            ApplicationDataPaths.UserSettingsDirectory,
            ApplicationDataPaths.UserLogsDirectory,
            ApplicationDataPaths.UserExportsDirectory
        };

        writablePaths.Should().OnlyContain(path =>
            !Path.GetFullPath(path).StartsWith(installRoot, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../outside.csv")]
    public void Export_path_rejects_segments_that_escape_the_export_root(string segment)
    {
        var act = () => ApplicationDataPaths.GetUserExportPath(segment);

        act.Should().Throw<ArgumentException>();
    }
}
