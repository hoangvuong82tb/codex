using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class DesktopWorkspaceView : UserControl
{
    public DesktopWorkspaceView()
    {
        InitializeComponent();
    }

    private void WorkspaceHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is DesktopShellViewModel viewModel)
        {
            viewModel.UpdateWorkspaceBounds(e.NewSize.Width, e.NewSize.Height);
        }
    }
}
