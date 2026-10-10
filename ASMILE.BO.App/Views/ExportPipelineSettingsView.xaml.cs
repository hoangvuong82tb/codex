using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class ExportPipelineSettingsView : UserControl
{
    public ExportPipelineSettingsView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExportPipelineSettingsViewModel viewModel)
            await viewModel.InitializeAsync();
    }
}
