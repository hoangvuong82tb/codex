using ASMILE.BO.Application;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.App.Services;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Shared.Security;
using ASMILE.BO.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ASMILE.BO.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _applicationScope;
    private static int _isShowingFatalDialog;
    private static DateTime _lastFatalDialogAtUtc = DateTime.MinValue;

    public IServiceProvider? Services => _applicationScope?.ServiceProvider ?? _host?.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Several legacy module templates render a ComboBox header and chevron
        // without a clickable toggle over the value area. Register this once at
        // application level so clicking any non-editable selection field always
        // opens its drop-down; the mouse wheel remains optional navigation only.
        EventManager.RegisterClassHandler(
            typeof(ComboBox),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnComboBoxHeaderClick),
            handledEventsToo: true);

        Directory.CreateDirectory(ApplicationDataPaths.UserLogsDirectory);
        var logLevelSwitch = new LoggingLevelSwitch();
        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            })
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                // Local appsettings is developer-only and is intentionally
                // excluded from publish. Production deployments provide the
                // connection string and other secrets through environment or
                // deployment configuration instead.
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                // Deployment and E2E overrides must win over the local
                // appsettings defaults (for example Database:MigrationMode).
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:0:Args:path"] = ApplicationDataPaths.UserLogFilePattern
                });
                config.AddEnvironmentVariables();
                // Authorized Settings changes are saved as a protected,
                // per-user override and take effect after the next launch.
                config.AddJsonFile(
                    ApplicationDataPaths.GetUserSettingsFile("database-connection.json"),
                    optional: true,
                    reloadOnChange: false);
            })
            .UseSerilog((context, services, loggerConfig) =>
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .MinimumLevel.ControlledBy(logLevelSwitch))
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(logLevelSwitch);
                services.AddHostedService<SettingsLogLevelMonitor>();
                services.AddHostedService<ApplicationLogRetentionService>();
                services.AddHostedService<SettingsDatabaseCommandTimeoutMonitor>();
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
                DependencyInjection.AddPresentation(services);
            })
            .Build();

        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            using (var scope = _host.Services.CreateScope())
            {
                var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
                await migrationService.InitializeAsync();

                try
                {
                    var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
                    var exportRoot = await settingsService.GetValueAsync("Storage.ExportRootPath", SettingScope.System, null);
                    ApplicationDataPaths.ConfigureUserExportsDirectory(exportRoot?.Value);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not load the configured export directory. Falling back to the default user export folder.");
                    ApplicationDataPaths.ConfigureUserExportsDirectory(null);
                }

                // Establish the company/branch scope before any hosted service
                // or window can query property-scoped data. This prevents a
                // startup race from briefly using the default-company fallback
                // while the shell is still loading its context.
                var currentCompanyContext = scope.ServiceProvider.GetRequiredService<ICurrentCompanyContext>();
                await currentCompanyContext.EnsureInitializedAsync();

                if (currentCompanyContext.CompanyId is Guid companyId)
                {
                    var adoptedFoundationRows = await migrationService.EnsureLegacyFoundationScopeAsync(companyId);
                    if (adoptedFoundationRows > 0)
                    {
                        Log.Information(
                            "Adopted {Count} legacy foundation rows after company scope initialization.",
                            adoptedFoundationRows);
                    }

                    var seedWarnings = await migrationService.ValidateSeedConsistencyAsync();
                    foreach (var warning in seedWarnings)
                    {
                        Log.Warning("Seed consistency warning after company scope initialization: {Warning}", warning);
                    }
                }

                var seedService = scope.ServiceProvider.GetRequiredService<ISecuritySeedService>();
                await seedService.EnsureSeedDataAsync();
            }

            await _host.StartAsync();

            _applicationScope = _host.Services.CreateScope();
            var mainWindow = _applicationScope.ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(
                ex,
                "Application startup failed. Message={Message}",
                InputSanitizer.TrimAndLimit(ex.Message, 500));

            MessageBox.Show(
                "Ứng dụng không thể khởi động do cấu hình hoặc cơ sở dữ liệu chưa sẵn sàng. Vui lòng kiểm tra nhật ký hệ thống.",
                "Không thể khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            try
            {
                if (_host is not null)
                    await _host.StopAsync();
            }
            catch
            {
                // Preserve the original startup failure while best-effort
                // cleanup prevents a blank process from remaining alive.
            }

            _applicationScope?.Dispose();
            _applicationScope = null;
            _host?.Dispose();
            _host = null;
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _applicationScope?.Dispose();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception("Unknown domain exception");
        Log.Error(ex, "Unhandled domain exception: {Message}", InputSanitizer.TrimAndLimit(ex.Message, 500));
        MessageBox.Show("Thao tác không thành công. Vui lòng kiểm tra nhật ký hệ thống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void OnComboBoxHeaderClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox ||
            !comboBox.IsEnabled ||
            comboBox.IsEditable ||
            comboBox.IsDropDownOpen)
        {
            return;
        }

        comboBox.Focus();
        SizeDropDownToOwner(comboBox);
        comboBox.IsDropDownOpen = true;
        e.Handled = true;
    }

    private static void SizeDropDownToOwner(ComboBox comboBox)
    {
        // Both the shared Fluent template and older module templates expose
        // their drop-down as "Popup". Size it from the owner at open time so
        // the menu always lines up exactly with the input box.
        if (comboBox.ActualWidth > 0 &&
            comboBox.Template.FindName("Popup", comboBox) is Popup popup)
        {
            popup.Width = comboBox.ActualWidth;
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var currentView = ResolveCurrentViewContext();
        var baseException = e.Exception.GetBaseException();
        Log.Error(
            e.Exception,
            "Unhandled UI exception. Message={Message}; Source={Source}; TargetSite={TargetSite}; BaseException={BaseException}; InnerChain={InnerChain}; CurrentView={CurrentView}",
            InputSanitizer.TrimAndLimit(e.Exception.Message, 500),
            InputSanitizer.TrimAndLimit(e.Exception.Source ?? "(null)", 500),
            InputSanitizer.TrimAndLimit(e.Exception.TargetSite?.ToString() ?? "(null)", 500),
            InputSanitizer.TrimAndLimit(baseException.ToString(), 4000),
            InputSanitizer.TrimAndLimit(BuildInnerExceptionChain(e.Exception), 4000),
            InputSanitizer.TrimAndLimit(currentView, 500));

        try
        {
            var diagnosticPath = Path.Combine(ApplicationDataPaths.UserLogsDirectory, "ui-exceptions.log");
            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticPath)!);
            File.AppendAllText(
                diagnosticPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CurrentView={currentView}{Environment.NewLine}{e.Exception}{Environment.NewLine}{new string('-', 96)}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never replace the original UI exception handling path.
        }

        if (e.Exception is AppValidationException validation)
        {
            MessageBox.Show(validation.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
            return;
        }

        var now = DateTime.UtcNow;
        if (Interlocked.CompareExchange(ref _isShowingFatalDialog, 1, 0) == 0)
        {
            try
            {
                if ((now - _lastFatalDialogAtUtc) > TimeSpan.FromSeconds(2))
                {
                    _lastFatalDialogAtUtc = now;
                    MessageBox.Show("Thao tác không thành công. Vui lòng kiểm tra nhật ký hệ thống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isShowingFatalDialog, 0);
            }
        }
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var baseException = e.Exception.GetBaseException();
        Log.Error(
            e.Exception,
            "Unhandled background task exception. Message={Message}; BaseException={BaseException}; InnerChain={InnerChain}",
            InputSanitizer.TrimAndLimit(e.Exception.Message, 500),
            InputSanitizer.TrimAndLimit(baseException.ToString(), 4000),
            InputSanitizer.TrimAndLimit(BuildInnerExceptionChain(e.Exception), 4000));
        e.SetObserved();
    }

    private static string ResolveCurrentViewContext()
    {
        try
        {
            var window = Current?.MainWindow;
            var content = window?.Content;
            if (content is Frame frame)
            {
                return frame.Content?.GetType().FullName ?? "(frame-without-content)";
            }

            return content?.GetType().FullName ?? "(main-window-without-content)";
        }
        catch (Exception ex)
        {
            return $"(resolve-context-failed: {ex.GetType().Name})";
        }
    }

    private static string BuildInnerExceptionChain(Exception exception)
    {
        var chain = new StringBuilder();
        var current = exception.InnerException;
        var depth = 0;
        while (current is not null && depth < 8)
        {
            if (depth > 0)
            {
                chain.AppendLine("----");
            }

            chain.Append(current.ToString());
            current = current.InnerException;
            depth++;
        }

        return chain.Length == 0 ? "(none)" : chain.ToString();
    }
}
