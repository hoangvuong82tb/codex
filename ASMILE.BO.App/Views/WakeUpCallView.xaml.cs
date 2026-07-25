using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class WakeUpCallView : UserControl
{
    public WakeUpCallView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.WakeUpCallViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnCallClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.WakeUpCallDto dto && DataContext is ViewModels.WakeUpCallViewModel vm) vm.SelectCallCommand.Execute(dto); }
}
