using System.Windows.Controls;
namespace ASMILE.BO.App.Views;
public partial class GroupReservationView : UserControl
{
    public GroupReservationView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    { if (DataContext is ViewModels.GroupReservationViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
}
