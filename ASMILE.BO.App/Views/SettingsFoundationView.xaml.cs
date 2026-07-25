using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class SettingsFoundationView : UserControl
{
    public SettingsFoundationView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.FoSettingsViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
}
