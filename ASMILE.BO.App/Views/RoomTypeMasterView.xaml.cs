using System.Windows;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views;

public partial class RoomTypeMasterView
{
    private bool _loaded;

    public RoomTypeMasterView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded || DataContext is not RoomTypeMasterViewModel viewModel) return;
        _loaded = true;
        await viewModel.InitializeAsync(CancellationToken.None);
    }
}
