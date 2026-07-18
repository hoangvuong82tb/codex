using System.Windows;

namespace ASMILE.BO.App.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message, string title = "Xác nhận")
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public void Info(string message, string title = "Thông báo")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void Error(string message, string title = "Lỗi")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

