using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class ShellNavigationFlyoutView : UserControl
{
    public ShellNavigationFlyoutView()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        NavigationSearchBox.Focus();
        NavigationSearchBox.SelectAll();
    }
}
