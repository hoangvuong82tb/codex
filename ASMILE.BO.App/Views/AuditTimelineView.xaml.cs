using System.Windows.Controls; using System.Windows.Input;
namespace ASMILE.BO.App.Views;
public partial class AuditTimelineView : UserControl
{
    public AuditTimelineView() => InitializeComponent();
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is ViewModels.AuditTimelineViewModel vm) await vm.InitializeAsync(System.Threading.CancellationToken.None); }
    private void OnClick(object sender, MouseButtonEventArgs e) { if (sender is Border b && b.Tag is ViewModels.AuditEntryDto dto && DataContext is ViewModels.AuditTimelineViewModel vm) vm.SelectEntryCommand.Execute(dto); }
}
