using ASMILE.BO.App.ViewModels.Auth;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views.Auth;

public partial class LoginPanelView : UserControl
{
    public LoginPanelView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdatePasswordUi();
            PasswordBox.Focus();
        };
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.SetPassword(passwordBox.Password);
            UpdatePasswordUi();
        }
    }

    private void ShowPassword_OnChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
        {
            return;
        }

        vm.ShowPassword = !vm.ShowPassword;

        if (vm.ShowPassword)
        {
            PasswordText.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordText.Text = PasswordBox.Password;
            EyeButton.Content = new TextBlock { Text = "\uE70F", FontFamily = new FontFamily("Segoe MDL2 Assets"), Foreground = Brushes.White, FontSize = 14 };
            PasswordText.Focus();
            PasswordText.CaretIndex = PasswordText.Text.Length;
        }
        else
        {
            PasswordBox.Visibility = Visibility.Visible;
            PasswordText.Visibility = Visibility.Collapsed;
            PasswordBox.Password = vm.VisiblePassword;
            EyeButton.Content = new TextBlock { Text = "\uE722", FontFamily = new FontFamily("Segoe MDL2 Assets"), Foreground = Brushes.White, FontSize = 14 };
            PasswordBox.Focus();
        }

        UpdatePasswordUi();
    }

    private void PasswordInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
        {
            vm.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void PasswordText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is TextBox tb)
        {
            vm.SetPassword(tb.Text);
        }

        UpdatePasswordUi();
    }

    private void UpdatePasswordUi()
    {
        PasswordPlaceholder.Visibility = (PasswordBox.Visibility == Visibility.Visible && string.IsNullOrEmpty(PasswordBox.Password))
            || (PasswordText.Visibility == Visibility.Visible && string.IsNullOrEmpty(PasswordText.Text))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
