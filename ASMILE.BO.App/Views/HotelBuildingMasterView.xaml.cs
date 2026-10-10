using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelBuildingMasterView : UserControl
{
    public HotelBuildingMasterView() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is HotelBuildingMasterViewModel vm) await vm.InitializeAsync(default); }
}
