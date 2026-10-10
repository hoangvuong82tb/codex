using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelPropertyProfileView : UserControl
{
    public HotelPropertyProfileView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelPropertyProfileViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
