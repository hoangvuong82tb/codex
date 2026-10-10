using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelFloorMasterView : UserControl
{
    public HotelFloorMasterView() { InitializeComponent(); }
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is HotelFloorMasterViewModel vm) await vm.InitializeAsync(default); }
}
