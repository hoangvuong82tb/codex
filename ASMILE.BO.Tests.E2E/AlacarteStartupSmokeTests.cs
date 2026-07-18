using System.Globalization;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FluentAssertions;

namespace ASMILE.BO.Tests.E2E;

public sealed class AlacarteStartupSmokeTests
{
    [Fact]
    public void AlacarteApp_ShouldLaunchWithoutStartupErrorDialog()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        Application? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = Application.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindowResult = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500));
            mainWindowResult.Result.Should().NotBeNull("main window should appear");

            var mainWindow = mainWindowResult.Result!;
            mainWindow.WaitUntilClickable(TimeSpan.FromSeconds(10));
            mainWindow.Title.Should().NotBeNullOrWhiteSpace();

            var errorWindow = Retry.WhileNull(
                () => FindErrorWindow(app, automation),
                timeout: TimeSpan.FromSeconds(6),
                interval: TimeSpan.FromMilliseconds(400));
            if (errorWindow.Result is not null)
            {
                var errorText = string.Join(" | ", errorWindow.Result.FindAllDescendants().Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)));
                throw new Xunit.Sdk.XunitException($"App showed startup error dialog: {errorText}");
            }
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public void AlacarteApp_ShouldExposeWorkspaceShellSurface()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        Application? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = Application.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));

            var shellSurface = Retry.WhileNull(
                () => FindShellSurface(window),
                timeout: TimeSpan.FromSeconds(6),
                interval: TimeSpan.FromMilliseconds(400));
            shellSurface.Result.Should().NotBeNull("workspace shell should expose a stable surface like SQL, Tong quan, or the A La Carte launcher");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public void AlacarteApp_ShouldNavigateRestaurantWorkspaceWithoutErrorDialog()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        Application? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = Application.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));

            ClickIfPresent(window, "Khởi tạo DL mẫu");
            ClickIfPresent(window, "Đơn mở");

            app.HasExited.Should().BeFalse("restaurant app process should remain alive after navigation attempts");
            var refreshedMainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(8),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            refreshedMainWindow.Should().NotBeNull("main restaurant window should still be obtainable after navigation attempts");
            refreshedMainWindow!.Title.Should().NotBeNullOrWhiteSpace();

            var errorWindow = FindErrorWindow(app, automation);
            errorWindow.Should().BeNull("restaurant navigation should not produce an error dialog");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public void AlacarteApp_ShouldSeedSampleDataWithoutErrorDialog()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        Application? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = Application.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));

            ClickIfPresent(window, "Khoi tao DL mau");
            ClickIfPresent(window, "Cai dat");

            var shellSurface = Retry.WhileNull(
                () => FindShellSurface(window),
                timeout: TimeSpan.FromSeconds(8),
                interval: TimeSpan.FromMilliseconds(400));
            shellSurface.Result.Should().NotBeNull("workspace shell should be visible before triggering sample data");

            ClickIfPresent(window, "Khoi tao DL mau");

            app.HasExited.Should().BeFalse("restaurant app should remain alive after triggering sample data");
            FindErrorWindow(app, automation).Should().BeNull("triggering sample data should not produce an error dialog");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    private static string ResolveExePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ASMILE.BO.App", "bin", "Debug", "net8.0-windows", "ASMILE.BO.App.exe"));

    private static Window? FindErrorWindow(Application app, UIA3Automation automation)
    {
        return app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(window =>
                string.Equals(NormalizeText(window.Title), "Loi", StringComparison.OrdinalIgnoreCase)
                || FindDescendantByNameFragment(window, "Thao tac khong thanh cong") is not null);
    }

    private static AutomationElement? FindShellSurface(Window window)
    {
        return FindDescendantByNameFragment(window, "SQL")
               ?? FindDescendantByNameFragment(window, "Tong quan")
               ?? FindDescendantByNameFragment(window, "Nha hang A La Carte")
               ?? FindDescendantByNameFragment(window, "A La Carte")
               ?? FindDescendantByNameFragment(window, "restaurant-alacarte")
               ?? FindDescendantByNameFragment(window, "Khoi tao DL mau")
               ?? FindDescendantByNameFragment(window, "Don mo")
               ?? FindDescendantByNameFragment(window, "Khu vuc");
    }

    private static AutomationElement? FindDescendantByAnyNameFragment(AutomationElement root, params string[] fragments)
        => fragments.Select(fragment => FindDescendantByNameFragment(root, fragment)).FirstOrDefault(x => x is not null);

    private static AutomationElement? FindDescendantByNameFragment(AutomationElement root, string fragment)
    {
        var normalizedFragment = NormalizeText(fragment);
        return root.FindAllDescendants()
            .FirstOrDefault(x => NormalizeText(x.Name).Contains(normalizedFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static void ClickIfPresent(Window window, string fragment)
    {
        var element = Retry.WhileNull(
            () => FindDescendantByNameFragment(window, fragment),
            timeout: TimeSpan.FromSeconds(4),
            interval: TimeSpan.FromMilliseconds(300)).Result;
        if (element is null)
        {
            return;
        }

        TryInvokeOrClick(element);
    }

    private static void TryInvokeOrClick(AutomationElement element)
    {
        try
        {
            element.Focus();
        }
        catch
        {
        }

        try
        {
            element.AsButton().Invoke();
            return;
        }
        catch
        {
        }

        try
        {
            element.Click();
        }
        catch
        {
        }
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString()
            .Replace('Đ', 'D')
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormC);
    }

    private static void Cleanup(Application? app, UIA3Automation? automation)
    {
        try
        {
            app?.Close();
        }
        catch
        {
        }

        try
        {
            if (app?.HasExited == false)
            {
                app.Kill();
            }
        }
        catch
        {
        }

        automation?.Dispose();
    }
}
