using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.ViewModels.Auth;
using ASMILE.BO.Application.Interfaces;
using System.Windows;

namespace ASMILE.BO.App;

public partial class MainWindow : Window
{
    private readonly LoginViewModel _loginViewModel;
    private readonly DesktopShellViewModel _shellViewModel;

    public MainWindow(DesktopShellViewModel viewModel, LoginViewModel loginViewModel, ICurrentUserService currentUserService)
    {
        InitializeComponent();
        _shellViewModel = viewModel;
        DataContext = _shellViewModel;

        _loginViewModel = loginViewModel;
        LoginPanel.DataContext = _loginViewModel;
        _loginViewModel.LoginSucceeded += OnLoginSucceeded;

        LoginOverlay.Visibility = currentUserService.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnLoginSucceeded()
    {
        _shellViewModel.RefreshAfterLogin();
        LoginOverlay.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _loginViewModel.LoginSucceeded -= OnLoginSucceeded;
        base.OnClosed(e);
    }
}
