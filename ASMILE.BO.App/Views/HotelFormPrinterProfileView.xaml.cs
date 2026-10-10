using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelFormPrinterProfileView : UserControl
{
    public HotelFormPrinterProfileView() { InitializeComponent(); }
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelFormPrinterProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
