using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class SettingsConfirmDialog : Window
{
    public SettingsConfirmDialog(string title, string message)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        Owner = System.Windows.Application.Current?.MainWindow;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
