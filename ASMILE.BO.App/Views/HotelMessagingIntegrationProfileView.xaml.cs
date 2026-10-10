using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelMessagingIntegrationProfileView : UserControl
{
    public HotelMessagingIntegrationProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HotelMessagingIntegrationProfileViewModel vm)
            await vm.InitializeAsync(CancellationToken.None);
    }
}
