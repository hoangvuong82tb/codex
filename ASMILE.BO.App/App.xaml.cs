using ASMILE.BO.Application;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _applicationScope;
    private static int _isShowingFatalDialog;
    private static DateTime _lastFatalDialogAtUtc = DateTime.MinValue;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            })
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseSerilog((context, services, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration))
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
                DependencyInjection.AddPresentation(services);
            })
            .Build();

        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        using (var scope = _host.Services.CreateScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
            await migrationService.InitializeAsync();

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
            var diagnosticPath = Path.Combine(AppContext.BaseDirectory, "logs", "ui-exceptions.log");
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
