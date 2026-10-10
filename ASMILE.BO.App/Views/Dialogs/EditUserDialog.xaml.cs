using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class EditUserDialog : Window
{
    private readonly UserAccountDto _user;
    public UpdateUserRequestDto? Request { get; private set; }

    public EditUserDialog(UserAccountDto user)
    {
        InitializeComponent();
        _user = user;
        UserNameTextBlock.Text = $"Tài khoản: {user.UserName}";
        DisplayNameTextBox.Text = user.DisplayName;
        EmailTextBox.Text = user.Email ?? string.Empty;
        NotesTextBox.Text = user.Notes ?? string.Empty;
        IsActiveCheckBox.IsChecked = user.IsActive;
        MustChangePasswordCheckBox.IsChecked = user.MustChangePassword;
        DisplayNameTextBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var displayName = DisplayNameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        var errors = new List<string>();
        if (displayName.Length == 0) errors.Add("Vui lòng nhập tên hiển thị.");
        if (!string.IsNullOrWhiteSpace(email))
        {
            try { _ = new MailAddress(email); } catch (FormatException) { errors.Add("Email không hợp lệ."); }
        }

        if (errors.Count > 0)
        {
            ValidationTextBlock.Text = string.Join("\n", errors);
            return;
        }

        Request = new UpdateUserRequestDto
        {
            UserId = _user.Id,
            DisplayName = displayName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Phone = _user.Phone,
            IsActive = IsActiveCheckBox.IsChecked == true,
            MustChangePassword = MustChangePasswordCheckBox.IsChecked == true,
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            RoleIds = _user.RoleIds.ToArray()
        };
        DialogResult = true;
    }
}
