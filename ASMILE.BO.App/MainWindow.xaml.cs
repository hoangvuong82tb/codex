using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.ViewModels.Auth;
using ASMILE.BO.Application.Interfaces;
using Serilog;
using System.Windows;
using System.Windows.Threading;

namespace ASMILE.BO.App;

public partial class MainWindow : Window
{
    private readonly LoginViewModel _loginViewModel;
    private readonly DesktopShellViewModel _shellViewModel;
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DispatcherTimer _sessionValidationTimer;
    private bool _isValidatingSession;
    private DateTime _lastUserActivityAtUtc = DateTime.UtcNow;
    private DateTime _lastSessionRenewalActivityAtUtc = DateTime.UtcNow;

    public MainWindow(
        DesktopShellViewModel viewModel,
        LoginViewModel loginViewModel,
        ICurrentUserService currentUserService,
        IAuthenticationService authenticationService)
    {
        InitializeComponent();
        _shellViewModel = viewModel;
        DataContext = _shellViewModel;

        _loginViewModel = loginViewModel;
        _currentUserService = currentUserService;
        _authenticationService = authenticationService;
        LoginPanel.DataContext = _loginViewModel;
        _loginViewModel.LoginSucceeded += OnLoginSucceeded;
        PreviewMouseDown += (_, _) => MarkUserActivity();
        PreviewMouseMove += (_, _) => MarkUserActivity();
        PreviewMouseWheel += (_, _) => MarkUserActivity();
        PreviewKeyDown += (_, _) => MarkUserActivity();
        PreviewTouchDown += (_, _) => MarkUserActivity();
        PreviewStylusDown += (_, _) => MarkUserActivity();

        LoginOverlay.Visibility = currentUserService.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
        DesktopShell.IsEnabled = currentUserService.IsAuthenticated;
        _lastUserActivityAtUtc = DateTime.UtcNow;
        _lastSessionRenewalActivityAtUtc = _lastUserActivityAtUtc;
        _sessionValidationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _sessionValidationTimer.Tick += OnSessionValidationTimerTick;
        if (currentUserService.IsAuthenticated)
        {
            _sessionValidationTimer.Start();
        }
    }

    private void OnLoginSucceeded()
    {
        _shellViewModel.RefreshAfterLogin();
        _lastUserActivityAtUtc = DateTime.UtcNow;
        _lastSessionRenewalActivityAtUtc = _lastUserActivityAtUtc;
        DesktopShell.IsEnabled = true;
        SessionValidationOverlay.Visibility = Visibility.Collapsed;
        LoginOverlay.Visibility = Visibility.Collapsed;
        _sessionValidationTimer.Start();
    }

    private async void OnSessionValidationTimerTick(object? sender, EventArgs e)
    {
        if (_isValidatingSession)
        {
            return;
        }

        var sessionId = _currentUserService.CurrentSessionId;
        if (!_currentUserService.IsAuthenticated || sessionId is null)
        {
            _sessionValidationTimer.Stop();
            DesktopShell.IsEnabled = false;
            SessionValidationOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        _isValidatingSession = true;
        try
        {
            var observedActivityAtUtc = _lastUserActivityAtUtc;
            var hasNewActivity = observedActivityAtUtc > _lastSessionRenewalActivityAtUtc;
            var sessionIsValid = hasNewActivity
                ? await _authenticationService.RenewSessionAsync(sessionId.Value)
                : await _authenticationService.ValidateSessionAsync(sessionId.Value);
            IReadOnlyList<string>? permissionCodes = sessionIsValid
                ? await _authenticationService.GetSessionPermissionCodesAsync(sessionId.Value)
                : null;
            var permissionsChanged = sessionIsValid
                && permissionCodes is not null
                && !PermissionSetsMatch(_currentUserService.CurrentUser?.PermissionCodes ?? [], permissionCodes);

            if (sessionIsValid && permissionCodes is not null && !permissionsChanged)
            {
                if (hasNewActivity)
                {
                    _lastSessionRenewalActivityAtUtc = observedActivityAtUtc;
                }
                DesktopShell.IsEnabled = true;
                SessionValidationOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            _sessionValidationTimer.Stop();
            DesktopShell.IsEnabled = false;
            SessionValidationOverlay.Visibility = Visibility.Collapsed;
            try
            {
                await _authenticationService.LogoutAsync(sessionId.Value);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not persist logout after session validation failed.");
            }

            _currentUserService.Clear();
            _loginViewModel.ClearPassword();
            _loginViewModel.ErrorMessage = permissionsChanged
                ? "Quyền truy cập của tài khoản đã thay đổi. Vui lòng đăng nhập lại để áp dụng quyền mới."
                : "Phiên đã hết hạn, bị thu hồi hoặc không còn quyền truy cập trong maintenance mode. Vui lòng đăng nhập lại.";
            LoginOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            // Pause shell interaction while authorization cannot be confirmed,
            // but preserve the user's screen and unsaved work for recovery.
            Log.Warning(ex, "Session validation failed; shell interaction is paused until authorization can be rechecked.");
            DesktopShell.IsEnabled = false;
            SessionValidationOverlay.Visibility = Visibility.Visible;
        }
        finally
        {
            _isValidatingSession = false;
        }
    }

    private void MarkUserActivity() => _lastUserActivityAtUtc = DateTime.UtcNow;

    private static bool PermissionSetsMatch(IReadOnlyCollection<string> current, IReadOnlyCollection<string> latest)
    {
        return current.Count == latest.Count
            && new HashSet<string>(current, StringComparer.OrdinalIgnoreCase).SetEquals(latest);
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _sessionValidationTimer.Stop();
        _sessionValidationTimer.Tick -= OnSessionValidationTimerTick;
        if (_loginViewModel is not null)
        {
            _loginViewModel.LoginSucceeded -= OnLoginSucceeded;
        }

        base.OnClosed(e);
    }
}
