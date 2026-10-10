using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelRoomConfigurationView : UserControl
{
    public HotelRoomConfigurationView() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is HotelRoomConfigurationViewModel vm) await vm.InitializeAsync(default); }
}
