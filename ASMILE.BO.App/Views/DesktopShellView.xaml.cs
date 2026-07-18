using ASMILE.BO.App.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class DesktopShellView : UserControl
{
    private ShellNavigationFlyoutView? _navigationFlyout;

    public DesktopShellView()
    {
        InitializeComponent();
    }

    private void DesktopShellView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not DesktopShellViewModel viewModel)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
        {
            viewModel.IsStartMenuOpen = true;
            viewModel.FocusGlobalSearchCommand.Execute(null);
            EnsureNavigationFlyout();
            _navigationFlyout?.FocusSearch();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape)
        {
            viewModel.IsStartMenuOpen = false;
            viewModel.IsGlobalSearchOpen = false;
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Tab)
        {
            viewModel.PreviousTabCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Tab)
        {
            viewModel.NextTabCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ShellNavigationFlyoutHost_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            EnsureNavigationFlyout();
            _navigationFlyout?.FocusSearch();
        }
    }

    private void EnsureNavigationFlyout()
    {
        if (_navigationFlyout is not null)
        {
            return;
        }

        _navigationFlyout = new ShellNavigationFlyoutView();
        ShellNavigationFlyoutHost.Content = _navigationFlyout;
    }
}
