using System.Windows;
using System.Collections.Generic;
using System.Linq;
using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class ResetPasswordDialog : Window
{
    public UserAccountDto? SelectedUser => UserComboBox.SelectedItem as UserAccountDto;
    public string NewPassword => PasswordBox.Password;
    public bool MustChangePassword => MustChangePasswordCheckBox.IsChecked == true;

    public ResetPasswordDialog(IReadOnlyList<UserAccountDto> users)
    {
        InitializeComponent();
        UserComboBox.ItemsSource = users;
        UserComboBox.SelectedIndex = users.Count > 0 ? 0 : -1;
        PasswordBox.Focus();
    }

    private void UserComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedUser is { } user) UserLabel.Text = $"{user.UserName} — {user.DisplayName}";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Password;
        var errors = new List<string>();
        if (SelectedUser is null) errors.Add("Vui lòng chọn tài khoản.");
        if (password.Length < 8) errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)) errors.Add("Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        if (!string.Equals(password, ConfirmPasswordBox.Password, StringComparison.Ordinal)) errors.Add("Mật khẩu nhập lại không khớp.");
        if (errors.Count > 0) { ValidationTextBlock.Text = string.Join("\n", errors); return; }
        DialogResult = true;
    }
}
