using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelFolioProfileView : UserControl
{
    public HotelFolioProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is HotelFolioProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
