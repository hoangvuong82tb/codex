using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelGroupPolicyView : UserControl
{
    public HotelGroupPolicyView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelGroupPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
