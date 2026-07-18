using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class InventoryMoreOptionsDialog : Window
{
    public string? SelectedAction { get; private set; }

    public InventoryMoreOptionsDialog()
    {
        InitializeComponent();
    }

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string action)
        {
            SelectedAction = action;
            DialogResult = true;
        }
    }
}
