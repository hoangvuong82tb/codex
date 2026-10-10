using System.Globalization;
using System.Text;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using FlaUI.Core;
using FlaUIApplication = FlaUI.Core.Application;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.E2E;

public sealed class AlacarteStartupSmokeTests
{
    [Fact]
    public void AlacarteApp_ShouldLaunchWithoutStartupErrorDialog()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindowResult = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(120),
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
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                // A clean LocalDB E2E database may need to apply hundreds of
                // historical migrations before the WPF main window is shown.
                timeout: TimeSpan.FromSeconds(180),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

            var shellSurface = Retry.WhileNull(
                () => FindShellSurface(window),
                timeout: TimeSpan.FromSeconds(6),
                interval: TimeSpan.FromMilliseconds(400));
            shellSurface.Result.Should().NotBeNull($"workspace shell should expose a stable surface like SQL, Tong quan, or the A La Carte launcher. UIA names: {DescribeUi(window)}");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public void AlacarteApp_ShouldOpenSystemSettingsHomeWithLivePanels()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(180),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear after the isolated E2E database is migrated");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

            var startMenuButton = Retry.WhileNull(
                () => window.FindAllDescendants().FirstOrDefault(x =>
                    x.ControlType == ControlType.Button
                    && HasAutomationId(x, "StartMenuButton")
                    && x.IsEnabled
                    && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(30),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            startMenuButton.Should().NotBeNull(
                $"the taskbar Start menu button should become enabled and visible after sign-in. UIA names: {DescribeUi(window)}");
            startMenuButton!.AsButton().Invoke();

            var searchBox = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidateWindow => candidateWindow.FindAllDescendants())
                    .FirstOrDefault(x => HasAutomationId(x, "ShellNavigationSearchBox")),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            searchBox.Should().NotBeNull("the navigation search box should be available after opening Start");
            searchBox!.Click();
            searchBox.Focus();
            Keyboard.Type("Cài đặt hệ thống");

            var settingsResult = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidateWindow => candidateWindow.FindAllDescendants())
                    .FirstOrDefault(x => x.ControlType == ControlType.Button
                                         && HasAutomationId(x, "settings")
                                         && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(20),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            settingsResult.Should().NotBeNull("global search should expose the real System Settings module route");
            settingsResult!.AsButton().Invoke();

            var recommendedPanel = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidateWindow => candidateWindow.FindAllDescendants())
                    .FirstOrDefault(x => NormalizeText(x.Name).Equals("Recommended settings", StringComparison.OrdinalIgnoreCase)
                                         && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(30),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            recommendedPanel.Should().NotBeNull(
                $"opening System Settings should render its Home recommendations. UIA windows: {string.Join(" || ", app.GetAllTopLevelWindows(automation).Select(DescribeUi))}");

            var allElements = app.GetAllTopLevelWindows(automation).SelectMany(candidateWindow => candidateWindow.FindAllDescendants()).ToArray();
            allElements.Any(x => NormalizeText(x.Name).Equals("System health", StringComparison.OrdinalIgnoreCase) && !x.IsOffscreen)
                .Should().BeTrue("System Settings Home should render its health panel");
            allElements.Any(x => NormalizeText(x.Name).StartsWith("Migrations:", StringComparison.OrdinalIgnoreCase) && !x.IsOffscreen)
                .Should().BeTrue("System Settings Home should surface the live pending-migration health status");
            allElements.Any(x => NormalizeText(x.Name).StartsWith("E-invoice:", StringComparison.OrdinalIgnoreCase) && !x.IsOffscreen)
                .Should().BeTrue("System Settings Home should surface whether an e-invoice adapter is configured");
            var fileBackupStatusElements = allElements
                .Where(x => NormalizeText(x.Name).StartsWith(NormalizeText("File backup đã kiểm tra:"), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            fileBackupStatusElements.Any(x => !x.IsOffscreen)
                .Should().BeTrue(
                    $"System Settings Home should show the file-backup validation status. Matching elements: {string.Join(" || ", fileBackupStatusElements.Select(DescribeUi))}. Visible text names: {string.Join(" || ", allElements.Where(x => x.ControlType == ControlType.Text && !x.IsOffscreen).Select(x => NormalizeText(x.Name)).Where(x => x.Length > 0))}");
            allElements.Any(x => NormalizeText(x.Name).Equals(NormalizeText("Cơ sở dữ liệu"), StringComparison.OrdinalIgnoreCase) && !x.IsOffscreen)
                .Should().BeTrue("the recommended Database settings action should be visible");
            FindErrorWindow(app, automation).Should().BeNull("opening System Settings should not show an error dialog");
            app.HasExited.Should().BeFalse("app process should remain alive after opening System Settings");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public async Task AlacarteApp_ShouldReturnToLoginWhenMaintenanceRevokesTheCurrentSession()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        var databaseName = $"ASMILE_BO_E2E_MAINTENANCE_{Guid.NewGuid():N}";
        var environmentNames = new[]
        {
            "ASMILE_BO_E2E_DATABASE",
            "ASMILE_BO_E2E_INITIAL_PASSWORD",
            "ASMILE_BO_E2E_PASSWORD",
            "ASMILE_BO_CONNECTION_STRING",
            "ASMILE_BO_DESIGNTIME_CONNECTION",
            "ASMILE_BO_INITIAL_ADMIN_PASSWORD",
            "Database__MigrationMode",
            "Database__AllowApplyPendingMigrations",
            "Database__FailOnPendingMigrations"
        };
        var originalEnvironment = environmentNames.ToDictionary(
            name => name,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);
        Environment.SetEnvironmentVariable("ASMILE_BO_E2E_DATABASE", databaseName);
        Environment.SetEnvironmentVariable("ASMILE_BO_E2E_INITIAL_PASSWORD", null);
        Environment.SetEnvironmentVariable("ASMILE_BO_E2E_PASSWORD", null);
        ConfigureTestEnvironment();

        var connectionString = Environment.GetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING")!;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(180),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("the isolated maintenance E2E database should migrate before the shell appears");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);
            FindEnabledStartMenuButton(window).Should().NotBeNull(
                $"the shell should be authenticated before enabling maintenance. UIA: {DescribeUi(window)}");

            await using (var db = new AppDbContext(options))
            {
                var activeSession = await db.LoginSessions
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.StartedAt)
                    .FirstAsync();
                var roleIds = await db.AppUserRoles
                    .Where(x => x.UserId == activeSession.UserId)
                    .Select(x => x.RoleId)
                    .ToArrayAsync();
                var maintenancePermissions = await db.AppRolePermissions
                    .Where(x => roleIds.Contains(x.RoleId) && x.PermissionCode == "System.Maintenance")
                    .ToListAsync();
                db.AppRolePermissions.RemoveRange(maintenancePermissions);

                var maintenanceSetting = await db.SettingValues.FirstOrDefaultAsync(x =>
                    x.DefinitionKey == "System.MaintenanceMode"
                    && x.Scope == SettingScope.System
                    && x.ScopeTarget == null);
                if (maintenanceSetting is null)
                {
                    db.SettingValues.Add(new SettingValue
                    {
                        DefinitionKey = "System.MaintenanceMode",
                        Scope = SettingScope.System,
                        Value = "true",
                        UpdatedBy = "maintenance-e2e"
                    });
                }
                else
                {
                    maintenanceSetting.Value = "true";
                    maintenanceSetting.UpdatedBy = "maintenance-e2e";
                    maintenanceSetting.UpdatedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
            }

            var loginPasswordInput = Retry.WhileNull(
                () => window.FindAllDescendants().FirstOrDefault(x => HasAutomationId(x, "LoginPasswordInput") && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            loginPasswordInput.Should().NotBeNull(
                $"maintenance should revoke the unauthorized session and return the client to Login within the polling window. UIA: {DescribeUi(window)}");
            FindDescendantByNameFragment(window, "maintenance mode").Should().NotBeNull(
                "the login panel should explain that the session was denied by maintenance mode");
            app.HasExited.Should().BeFalse("the app should remain open at the login screen after maintenance revocation");

            await using (var db = new AppDbContext(options))
            {
                var session = await db.LoginSessions.OrderByDescending(x => x.StartedAt).FirstAsync();
                session.IsActive.Should().BeFalse();
                session.EndedAt.Should().NotBeNull();

                var presence = await db.ActiveCompanySessions.IgnoreQueryFilters()
                    .SingleAsync(x => x.SessionId == session.Id);
                presence.IsActive.Should().BeFalse();
                presence.Status.Should().Be("MaintenanceMode");
                presence.CompletedAtUtc.Should().NotBeNull();
                (await db.AuditLogs.AnyAsync(x =>
                    x.Action == "SessionRevoked"
                    && x.Detail.Contains(session.Id.ToString())))
                    .Should().BeTrue("the forced logout should leave an auditable event");
            }
        }
        finally
        {
            Cleanup(app, automation);
            try
            {
                await using var cleanupDb = new AppDbContext(options);
                await cleanupDb.Database.EnsureDeletedAsync();
            }
            finally
            {
                foreach (var (name, value) in originalEnvironment)
                {
                    Environment.SetEnvironmentVariable(name, value);
                }
            }
        }
    }

    [Fact]
    public void AlacarteApp_ShouldOpenHotelManagementTileWithoutErrorDialog()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

            var hotelTile = Retry.WhileNull(
                () => FindModuleTileByRoute(window, "front-office-reservations"),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            hotelTile.Should().NotBeNull($"Hotel Management tile should be exposed to UI Automation. UIA names: {DescribeUi(window)}");

            TryInvokeOrClick(hotelTile!);
            Thread.Sleep(1200);

            app.HasExited.Should().BeFalse("app process should remain alive after opening Hotel Management");
            FindErrorWindow(app, automation).Should().BeNull("opening Hotel Management should not produce an error dialog");
        }
        finally
        {
            Cleanup(app, automation);
        }
    }

    [Fact]
    public void AlacarteApp_ShouldOpenVirtualFileCenterWithoutDbContextConcurrencyError()
    {
        var exePath = ResolveExePath();
        File.Exists(exePath).Should().BeTrue($"App executable must exist at {exePath}");

        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                // A clean E2E LocalDB must apply the complete migration chain
                // before the shell and Virtual File Center can be inspected.
                timeout: TimeSpan.FromSeconds(180),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

            var startMenuButton = Retry.WhileNull(
                () => window.FindAllDescendants().FirstOrDefault(x =>
                    x.ControlType == ControlType.Button
                    && HasAutomationId(x, "StartMenuButton")
                    && x.IsEnabled
                    && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(30),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            startMenuButton.Should().NotBeNull(
                $"the taskbar Start menu button should become enabled and visible after sign-in. UIA names: {DescribeUi(window)}");
            startMenuButton!.AsButton().Invoke();

            var searchBox = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidateWindow => candidateWindow.FindAllDescendants())
                    .FirstOrDefault(x => HasAutomationId(x, "ShellNavigationSearchBox")),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            searchBox.Should().NotBeNull($"the navigation flyout search box should be available after opening Start. UIA names: {DescribeUi(window)}");
            searchBox!.Click();
            searchBox.Focus();
            Keyboard.Type("Virtual File Center");

            var vfcSearchResult = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidateWindow => candidateWindow.FindAllDescendants())
                    .FirstOrDefault(x => x.ControlType == ControlType.Button
                                         && HasAutomationId(x, "virtual-file-center")
                                         && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(20),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            vfcSearchResult.Should().NotBeNull($"global search should show a visible Virtual File Center module result. UIA names: {DescribeUi(window)}");
            vfcSearchResult!.AsButton().Invoke();
            startMenuButton.AsButton().Invoke();
            var errorWindow = Retry.WhileNull(
                () => FindErrorWindow(app, automation),
                // VFC loads settings and system-health data asynchronously after
                // the module window is created. Keep observing beyond the first
                // render so a delayed DbContext concurrency dialog is not missed.
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(300));
            errorWindow.Result.Should().BeNull("opening Virtual File Center must not show a DbContext concurrency error dialog");

            var explorerSurface = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .FirstOrDefault(candidate => FindDescendantByNameFragment(candidate, "Devices and drives") is not null),
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(300));
            explorerSurface.Result.Should().NotBeNull(
                $"opening Virtual File Center must render its This PC content in an application window. UIA windows: {string.Join(" || ", app.GetAllTopLevelWindows(automation).Select(DescribeUi))}");

            var explorerWindow = explorerSurface.Result!;
            var vfcRootView = Retry.WhileNull(
                () => app.GetAllTopLevelWindows(automation)
                    .SelectMany(candidate => candidate.FindAllDescendants())
                    .FirstOrDefault(x => HasAutomationId(x, "VfcRootExplorerView")),
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            vfcRootView.Should().NotBeNull(
                $"Virtual File Center should expose its root view to UI Automation. UIA windows: {string.Join(" || ", app.GetAllTopLevelWindows(automation).Select(DescribeUi))}");

            foreach (var (buttonId, iconId) in new[]
                     {
                         ("VfcBackButton", "VfcBackIcon"),
                         ("VfcForwardButton", "VfcForwardIcon"),
                         ("VfcUpButton", "VfcUpIcon"),
                         ("VfcRefreshButton", "VfcRefreshIcon")
                     })
            {
                var toolbarButton = vfcRootView!.FindAllDescendants()
                    .FirstOrDefault(x => HasAutomationId(x, buttonId));
                toolbarButton.Should().NotBeNull($"Virtual File Center should expose toolbar control {buttonId} to UI Automation. UIA names: {DescribeUi(vfcRootView)}");
                toolbarButton!.FindAllDescendants()
                    .Any(x => HasAutomationId(x, iconId) && !string.IsNullOrWhiteSpace(x.Name))
                    .Should().BeTrue($"toolbar control {buttonId} should expose its vector icon to accessibility/UI Automation, not render an empty button. UIA names: {DescribeUi(toolbarButton)}");
            }

            foreach (var driveName in new[] { "D: ACCOUNTING", "E: SALES", "F: HR", "G: LOGS" })
            {
                (FindDescendantByNameFragment(vfcRootView!, driveName) is not null)
                    .Should().BeTrue($"Virtual File Center should list configured business drive {driveName}. UIA names: {DescribeUi(vfcRootView!)}");
            }

            var accountingDriveItem = Retry.WhileNull(
                () => vfcRootView!.FindAllDescendants().FirstOrDefault(x =>
                    x.ControlType == ControlType.DataItem
                    && x.FindAllDescendants().Any(child =>
                        NormalizeText(child.Name).Contains("D: ACCOUNTING", StringComparison.OrdinalIgnoreCase))),
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            accountingDriveItem.Should().NotBeNull(
                $"the Accounting drive card should be openable from This PC. UIA names: {DescribeUi(vfcRootView!)}");
            accountingDriveItem!.DoubleClick();

            var currentItemsList = Retry.WhileNull(
                () => vfcRootView!.FindAllDescendants().FirstOrDefault(x =>
                    (HasAutomationId(x, "VfcCurrentItemsListView")
                     || HasAutomationId(x, "VfcCurrentItemsIconList"))
                    && !x.IsOffscreen),
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            currentItemsList.Should().NotBeNull(
                $"opening the Accounting drive should expose the visible file list. UIA names: {DescribeUi(vfcRootView!)}");

            var expectedAccountingFolders = new[] { "Nhà cung cấp", "Khách hàng", "Hóa đơn", "Phiếu nhập", "Phiếu xuất" };
            var missingAccountingFolders = Retry.WhileNull(
                () =>
                {
                    var missing = expectedAccountingFolders
                        .Where(folder => FindDescendantByNameFragment(currentItemsList!, folder) is null)
                        .ToArray();
                    return missing.Length == 0 ? missing : null;
                },
                timeout: TimeSpan.FromSeconds(20),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            missingAccountingFolders.Should().NotBeNull(
                $"opening the Accounting drive should render its customer and invoice/import/export folders in the content list. UIA names: {DescribeUi(currentItemsList!)}");
            missingAccountingFolders!.Should().BeEmpty();

            app.HasExited.Should().BeFalse("app process should remain alive after opening Virtual File Center");
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
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

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
        ConfigureTestEnvironment();

        FlaUIApplication? app = null;
        UIA3Automation? automation = null;
        try
        {
            app = FlaUIApplication.Launch(exePath);
            automation = new UIA3Automation();

            var mainWindow = Retry.WhileNull(
                () => app.GetMainWindow(automation, TimeSpan.FromSeconds(2)),
                timeout: TimeSpan.FromSeconds(45),
                interval: TimeSpan.FromMilliseconds(500)).Result;
            mainWindow.Should().NotBeNull("main window should appear");

            var window = mainWindow!;
            window.WaitUntilClickable(TimeSpan.FromSeconds(10));
            SignInIfRequired(app, automation, window);

            ClickIfPresent(window, "Khoi tao DL mau");
            ClickIfPresent(window, "Cai dat");

            var shellSurface = Retry.WhileNull(
                () => FindShellSurface(window),
                timeout: TimeSpan.FromSeconds(8),
                interval: TimeSpan.FromMilliseconds(400));
            shellSurface.Result.Should().NotBeNull($"workspace shell should be visible before triggering sample data. UIA names: {DescribeUi(window)}");

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
    {
        var overridePath = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_EXE");
        var exePath = !string.IsNullOrWhiteSpace(overridePath)
            ? Path.GetFullPath(overridePath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ASMILE.BO.App", "bin", "Debug", "net8.0-windows", "ASMILE.BO.App.exe"));

        // The app uses single-instance activation. Never let a UI test attach
        // to or type into a user's already-running application session.
        var runningInstances = System.Diagnostics.Process.GetProcessesByName(
            Path.GetFileNameWithoutExtension(exePath));
        try
        {
            runningInstances.Should().BeEmpty(
                "UI smoke tests must not take over an already-running user application");
        }
        finally
        {
            foreach (var runningInstance in runningInstances)
            {
                runningInstance.Dispose();
            }
        }

        return exePath;
    }

    private static void ConfigureTestEnvironment()
    {
        // Derive a repeatable test-only credential from the isolated E2E DB
        // name so tests in one daily run can reuse that database without
        // storing a password. A date-scoped default avoids stale credentials
        // left by older smoke-test implementations or manual E2E overrides.
        var databaseName = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_DATABASE")
                           ?? $"ASMILE_BO_E2E_SMOKE_{DateTime.UtcNow:yyyyMMdd}";
        var configuredInitialPassword = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_INITIAL_PASSWORD");
        var initialPassword = !string.IsNullOrWhiteSpace(configuredInitialPassword)
            ? configuredInitialPassword
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes($"ASMILE-BO-E2E:{databaseName}")));
        var configuredE2ePassword = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_PASSWORD");
        var e2ePassword = !string.IsNullOrWhiteSpace(configuredE2ePassword)
            ? configuredE2ePassword
            : $"E2eSmoke{DateTime.UtcNow:yyyyMMdd}";
        Environment.SetEnvironmentVariable(
            "ASMILE_BO_CONNECTION_STRING",
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;");
        Environment.SetEnvironmentVariable("ASMILE_BO_DESIGNTIME_CONNECTION", Environment.GetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING"));
        Environment.SetEnvironmentVariable("ASMILE_BO_INITIAL_ADMIN_PASSWORD", initialPassword);
        Environment.SetEnvironmentVariable("ASMILE_BO_E2E_INITIAL_PASSWORD", initialPassword);
        Environment.SetEnvironmentVariable("ASMILE_BO_E2E_PASSWORD", e2ePassword);
        Environment.SetEnvironmentVariable("Database__MigrationMode", "ApplyPendingMigrations");
        Environment.SetEnvironmentVariable("Database__AllowApplyPendingMigrations", "true");
        Environment.SetEnvironmentVariable("Database__FailOnPendingMigrations", "false");
    }

    private static void SignInIfRequired(FlaUIApplication app, UIA3Automation automation, Window window)
    {
        var password = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var passwordInput = window.FindAllDescendants()
            .FirstOrDefault(x => HasAutomationId(x, "LoginPasswordInput") && !x.IsOffscreen);
        if (passwordInput is null)
        {
            return;
        }

        var initialPassword = Environment.GetEnvironmentVariable("ASMILE_BO_E2E_INITIAL_PASSWORD");
        foreach (var credential in new[] { password, initialPassword }
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.Ordinal))
        {
            passwordInput.Click();
            passwordInput.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(credential!);
            Keyboard.Press(VirtualKeyShort.RETURN);

            var outcome = Retry.WhileNull(
                () =>
                {
                    var firstLoginDialog = FindFirstLoginPasswordDialog(app, automation);
                    if (firstLoginDialog is not null)
                    {
                        return new LoginAttempt(firstLoginDialog, Rejected: false);
                    }

                    if (FindEnabledStartMenuButton(window) is not null)
                    {
                        return new LoginAttempt(null, Rejected: false);
                    }

                    return FindDescendantByNameFragment(window, "Tên đăng nhập hoặc mật khẩu không đúng") is not null
                        ? new LoginAttempt(null, Rejected: true)
                        : null;
                },
                timeout: TimeSpan.FromSeconds(12),
                interval: TimeSpan.FromMilliseconds(300)).Result;

            if (outcome is null)
            {
                throw new Xunit.Sdk.XunitException(
                    $"The E2E login did not reach an authenticated, first-login, or rejected state. UIA: {DescribeUi(window)}");
            }

            if (outcome.Rejected)
            {
                continue;
            }

            if (outcome.FirstLoginDialog is not null)
            {
                CompleteFirstLoginPasswordChange(outcome.FirstLoginDialog, password);
                var authenticated = Retry.WhileNull(
                    () => FindEnabledStartMenuButton(window),
                    timeout: TimeSpan.FromSeconds(20),
                    interval: TimeSpan.FromMilliseconds(300)).Result;
                authenticated.Should().NotBeNull(
                    $"the shell should unlock after the required first-login password change. UIA: {DescribeUi(window)}");
            }

            return;
        }

        throw new Xunit.Sdk.XunitException(
            $"Neither the persisted E2E password nor initial seed credential authenticated. UIA: {DescribeUi(window)}");
    }

    private static AutomationElement? FindFirstLoginPasswordDialog(FlaUIApplication app, UIA3Automation automation)
    {
        var windows = app.GetAllTopLevelWindows(automation);
        var dialogNode = windows
            .SelectMany(candidate => candidate.FindAllDescendants())
            .FirstOrDefault(x => x.ControlType == ControlType.Window
                                 && NormalizeText(x.Name).Contains(NormalizeText("Đổi mật khẩu bắt buộc"), StringComparison.OrdinalIgnoreCase));
        if (dialogNode is not null)
        {
            return dialogNode;
        }

        return windows.FirstOrDefault(candidate => candidate.FindAllDescendants().Any(x =>
            HasAutomationId(x, "FirstLoginNewPassword")
            || NormalizeText(x.Name).Contains(NormalizeText("Đổi mật khẩu trước khi tiếp tục"), StringComparison.OrdinalIgnoreCase)));
    }

    private static void CompleteFirstLoginPasswordChange(AutomationElement dialog, string newPassword)
    {
        foreach (var automationId in new[] { "FirstLoginNewPassword", "FirstLoginConfirmPassword" })
        {
            var input = Retry.WhileNull(
                () => dialog.FindAllDescendants().FirstOrDefault(x => HasAutomationId(x, automationId) && !x.IsOffscreen)
                      ?? dialog.FindAllDescendants()
                          .Where(x => x.ControlType == ControlType.Edit && !x.IsOffscreen)
                          .ElementAtOrDefault(automationId == "FirstLoginNewPassword" ? 0 : 1),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(300)).Result;
            input.Should().NotBeNull($"first-login dialog should expose password field {automationId}");
            input!.Click();
            input.Focus();
            Keyboard.Type(newPassword);
        }

        var submit = dialog.FindAllDescendants()
            .FirstOrDefault(x => HasAutomationId(x, "FirstLoginPasswordChangeSubmit"));
        submit.Should().NotBeNull("first-login dialog should expose its password-change action");
        submit!.AsButton().Invoke();
    }

    private static AutomationElement? FindEnabledStartMenuButton(Window window)
        => window.FindAllDescendants().FirstOrDefault(x =>
            x.ControlType == ControlType.Button
            && HasAutomationId(x, "StartMenuButton")
            && x.IsEnabled
            && !x.IsOffscreen);

    private sealed record LoginAttempt(AutomationElement? FirstLoginDialog, bool Rejected);

    private static Window? FindErrorWindow(FlaUIApplication app, UIA3Automation automation)
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

    private static AutomationElement? FindModuleTileByName(AutomationElement root, string fragment)
    {
        var normalizedFragment = NormalizeText(fragment);
        return root.FindAllDescendants()
            .Where(x => x.ControlType is ControlType.DataItem or ControlType.Button)
            .FirstOrDefault(x => NormalizeText(x.Name).Contains(normalizedFragment, StringComparison.OrdinalIgnoreCase)
                || x.FindAllDescendants().Any(child => NormalizeText(child.Name).Contains(normalizedFragment, StringComparison.OrdinalIgnoreCase)));
    }

    private static AutomationElement? FindModuleTileByRoute(AutomationElement root, string route)
    {
        return root.FindAllDescendants()
            .FirstOrDefault(x => HasAutomationId(x, route))
            ?? root.FindAllDescendants()
                .Where(x => x.ControlType is ControlType.DataItem or ControlType.Button)
                .FirstOrDefault(x => x.FindAllDescendants().Any(child => HasAutomationId(child, route)));
    }

    private static bool HasAutomationId(AutomationElement element, string route)
    {
        try
        {
            return string.Equals(element.AutomationId, route, StringComparison.OrdinalIgnoreCase);
        }
        catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return false;
        }
    }

    private static bool HasName(AutomationElement element, string expectedName)
    {
        try
        {
            return string.Equals(element.Name, expectedName, StringComparison.Ordinal);
        }
        catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return false;
        }
    }

    private static AutomationElement? FindDescendantByNameFragment(AutomationElement root, string fragment)
    {
        var normalizedFragment = NormalizeText(fragment);
        return root.FindAllDescendants()
            .FirstOrDefault(x =>
            {
                try
                {
                    return NormalizeText(x.Name).Contains(normalizedFragment, StringComparison.OrdinalIgnoreCase);
                }
                catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
                {
                    // UIA providers may expose descendants without a Name
                    // property. They are not an application error window.
                    return false;
                }
            });
    }

    private static string DescribeUi(AutomationElement root)
        => string.Join(" | ", root.FindAllDescendants()
            .Select(x =>
            {
                try
                {
                    return $"{x.ControlType}:{x.Name}";
                }
                catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
                {
                    return string.Empty;
                }
            })
            .Where(x => x.Length > 2)
            .Take(120));

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

    private static void Cleanup(FlaUIApplication? app, UIA3Automation? automation)
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
