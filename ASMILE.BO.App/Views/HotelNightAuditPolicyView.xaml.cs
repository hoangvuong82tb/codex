using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelNightAuditPolicyView : UserControl
{
    public HotelNightAuditPolicyView() { InitializeComponent(); }
    private async void OnLoaded(object sender, RoutedEventArgs e) { if (DataContext is HotelNightAuditPolicyViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
