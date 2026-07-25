using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class LostFoundView : UserControl
{
    public LostFoundView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.LostFoundViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnItemClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.LostFoundDto dto && DataContext is ViewModels.LostFoundViewModel vm) vm.SelectItemCommand.Execute(dto); }
}
