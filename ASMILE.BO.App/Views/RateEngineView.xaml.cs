using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class RateEngineView : UserControl
{
    public RateEngineView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is ViewModels.RateEngineViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
}
