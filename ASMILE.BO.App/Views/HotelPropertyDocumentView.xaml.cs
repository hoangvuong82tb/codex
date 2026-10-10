using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class HotelPropertyDocumentView : UserControl
{
    public HotelPropertyDocumentView() => InitializeComponent();
    private async void OnLoaded(object sender, RoutedEventArgs e)
    { if (DataContext is HotelPropertyDocumentViewModel vm) await vm.InitializeAsync(CancellationToken.None); }
}
