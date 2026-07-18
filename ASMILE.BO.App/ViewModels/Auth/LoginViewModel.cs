using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Security;
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

            _currentUserService.SetCurrentUser(result);
            ClearPassword();
            LoginSucceeded?.Invoke();
        }
        catch
        {
            ErrorMessage = "Đăng nhập thất bại. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
