using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelProfileTypeProfileView : UserControl
{
    public HotelProfileTypeProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelProfileTypeProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
