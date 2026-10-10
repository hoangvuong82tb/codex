using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class ConciergeView : UserControl
{
    public ConciergeView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.ConciergeViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.ConciergeDto dto && DataContext is ViewModels.ConciergeViewModel vm) vm.SelectRequestCommand.Execute(dto); }
}
