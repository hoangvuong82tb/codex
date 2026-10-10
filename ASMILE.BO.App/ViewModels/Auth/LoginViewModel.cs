using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Shared.Security;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels.Auth;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;

    private string _password = string.Empty;

    [ObservableProperty] private string userName = "administrator";
    [ObservableProperty] private string displayName = "Administrator";
    [ObservableProperty] private bool rememberUserName = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool showPassword;
    [ObservableProperty] private string visiblePassword = string.Empty;
    [ObservableProperty] private bool useAnotherAccount;

    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthenticationService authenticationService, ICurrentUserService currentUserService)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
    }

    public void SetPassword(string password)
    {
        _password = password ?? string.Empty;
        if (ShowPassword)
        {
            VisiblePassword = _password;
        }
    }

    public void ClearPassword()
    {
        _password = string.Empty;
        VisiblePassword = string.Empty;
    }

    partial void OnVisiblePasswordChanged(string value)
    {
        if (ShowPassword)
        {
            _password = value;
        }
    }

    partial void OnShowPasswordChanged(bool value)
    {
        VisiblePassword = value ? _password : string.Empty;
    }

    [RelayCommand]
    private void ToggleAnotherAccount()
    {
        UseAnotherAccount = !UseAnotherAccount;
        if (!UseAnotherAccount)
        {
            UserName = "administrator";
            DisplayName = "Administrator";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;
        Guid? pendingSessionId = null;
        try
        {
            var result = await _authenticationService.LoginAsync(new LoginRequestDto
            {
                UserName = InputSanitizer.TrimAndLimit(UserName, 64),
                Password = _password,
                MachineName = Environment.MachineName,
                IpAddress = "127.0.0.1"
            });

            if (!result.Success)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Tên đăng nhập hoặc mật khẩu không đúng."
                    : result.ErrorMessage;
                return;
            }

            pendingSessionId = result.SessionId;
            if (result.MustChangePassword)
            {
                var currentPassword = _password;
                ClearPassword();
                var dialog = new FirstLoginPasswordChangeDialog(result.UserName, currentPassword)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                if (dialog.ShowDialog() != true)
                {
                    await _authenticationService.LogoutAsync(result.SessionId);
                    pendingSessionId = null;
                    ErrorMessage = "Bạn cần đổi mật khẩu trước khi tiếp tục đăng nhập.";
                    return;
                }

                await _authenticationService.ChangePasswordAsync(new ChangePasswordRequestDto
                {
                    UserId = result.UserId,
                    CurrentPassword = currentPassword,
                    NewPassword = dialog.NewPassword
                });
                result.MustChangePassword = false;
            }

            _currentUserService.SetCurrentUser(result);
            pendingSessionId = null;
            ClearPassword();
            LoginSucceeded?.Invoke();
        }
        catch
        {
            if (pendingSessionId is Guid sessionId)
            {
                try
                {
                    await _authenticationService.LogoutAsync(sessionId);
                }
                catch
                {
                    // Keep the login failure generic while best-effort revoking
                    // a session created before the first-login flow failed.
                }
            }

            ErrorMessage = "Đăng nhập thất bại. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
