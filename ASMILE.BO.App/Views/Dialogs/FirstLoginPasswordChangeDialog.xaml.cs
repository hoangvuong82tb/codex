using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class FirstLoginPasswordChangeDialog : Window
{
    private readonly string _currentPassword;

    public string NewPassword => NewPasswordBox.Password;

    public FirstLoginPasswordChangeDialog(string userName, string currentPassword)
    {
        InitializeComponent();
        UserNameTextBlock.Text = userName;
        _currentPassword = currentPassword;
        NewPasswordBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var password = NewPasswordBox.Password;
        var errors = new List<string>();
        if (password.Length < 8) errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            errors.Add("Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        if (string.Equals(password, _currentPassword, StringComparison.Ordinal))
            errors.Add("Mật khẩu mới phải khác mật khẩu hiện tại.");
        if (!string.Equals(password, ConfirmPasswordBox.Password, StringComparison.Ordinal))
            errors.Add("Mật khẩu nhập lại không khớp.");

        if (errors.Count > 0)
        {
            ValidationTextBlock.Text = string.Join(Environment.NewLine, errors);
            return;
        }

        DialogResult = true;
    }
}
