using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public partial class VfcRetentionPolicyView : UserControl
{
    public VfcRetentionPolicyView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VfcRetentionPolicyViewModel viewModel)
            await viewModel.InitializeAsync();
    }
}
