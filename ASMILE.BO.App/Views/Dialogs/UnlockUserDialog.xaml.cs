using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.Application.DTOs.Security;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class UnlockUserDialog : Window
{
    public UserAccountDto? SelectedUser => UserComboBox.SelectedItem as UserAccountDto;

    public UnlockUserDialog(IReadOnlyList<UserAccountDto> lockedUsers)
    {
        InitializeComponent();
        var candidates = lockedUsers.Where(user => user.IsLocked).ToList();
        UserComboBox.ItemsSource = candidates;
        UserComboBox.SelectedIndex = candidates.Count == 1 ? 0 : -1;
    }

    private void UserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ValidationTextBlock.Text = string.Empty;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedUser is null || !SelectedUser.IsLocked)
        {
            ValidationTextBlock.Text = "Vui lòng chọn tài khoản đang bị khóa.";
            return;
        }

        DialogResult = true;
    }
}
