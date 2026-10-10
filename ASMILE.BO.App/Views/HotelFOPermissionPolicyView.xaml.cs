using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelFOPermissionPolicyView : UserControl
{
    public HotelFOPermissionPolicyView() { InitializeComponent(); }
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelFOPermissionPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
