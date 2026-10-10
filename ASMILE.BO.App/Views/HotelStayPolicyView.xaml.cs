using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelStayPolicyView : UserControl
{
    public HotelStayPolicyView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelStayPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
