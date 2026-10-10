using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelAreaMasterView : UserControl
{
    public HotelAreaMasterView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HotelAreaMasterViewModel viewModel)
        {
            await viewModel.InitializeAsync(CancellationToken.None);
        }
    }
}
