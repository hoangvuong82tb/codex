using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class VipLoyaltyView : UserControl
{
    public VipLoyaltyView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.VipLoyaltyViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.VipProfileDto dto && DataContext is ViewModels.VipLoyaltyViewModel vm) vm.SelectMemberCommand.Execute(dto); }
}
