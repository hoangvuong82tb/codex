using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class AddEditUserDialog : Window
{
    public CreateUserRequestDto? Request { get; private set; }

    public AddEditUserDialog(IReadOnlyList<RoleDto> roles)
    {
        InitializeComponent();
        RoleComboBox.ItemsSource = roles.Where(x => x.IsActive).ToList();
        RoleComboBox.SelectedIndex = roles.Count > 0 ? 0 : -1;
        UserNameTextBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var userName = UserNameTextBox.Text.Trim();
        var displayName = DisplayNameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        var password = PasswordBox.Password;
        var errors = new List<string>();
        if (userName.Length < 3) errors.Add("Tên đăng nhập phải có ít nhất 3 ký tự.");
        if (userName.Any(char.IsWhiteSpace)) errors.Add("Tên đăng nhập không được chứa khoảng trắng.");
        if (displayName.Length == 0) errors.Add("Vui lòng nhập tên hiển thị.");
        if (password.Length < 8) errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)) errors.Add("Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        if (!string.Equals(password, ConfirmPasswordBox.Password, StringComparison.Ordinal)) errors.Add("Mật khẩu nhập lại không khớp.");
        if (!string.IsNullOrWhiteSpace(email))
        {
            try { _ = new MailAddress(email); } catch (FormatException) { errors.Add("Email không hợp lệ."); }
        }
        var role = RoleComboBox.SelectedItem as RoleDto;
        if (role is null) errors.Add("Vui lòng chọn vai trò.");
        if (errors.Count > 0) { ValidationTextBlock.Text = string.Join("\n", errors); return; }

        Request = new CreateUserRequestDto
        {
            UserName = userName, DisplayName = displayName, InitialPassword = password,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
            IsActive = true, MustChangePassword = MustChangePasswordCheckBox.IsChecked == true,
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            RoleIds = new[] { role!.Id }
        };
        DialogResult = true;
    }
}
