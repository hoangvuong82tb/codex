using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class CheckInWindow : Window
{
    public CheckInWindow(ReservationWorkspaceViewModel workspace)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        DataContext = workspace;
        Loaded += async (_, _) => await workspace.RefreshAsync();
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as ReservationWorkspaceViewModel;
        if (vm?.SelectedReservation is null) return;
        // Open Check-in execution via existing stay modal service
        var sp = ((App)System.Windows.Application.Current).GetType()
            .GetProperty("ServiceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(System.Windows.Application.Current) as IServiceProvider;
        var svc = sp?.GetRequiredService<ASMILE.BO.App.Services.IReceptionUiService>();
        if (svc is null) return;
        var checkInVm = new CheckInStayModalViewModel(svc);
        var checkInWin = new Window { Title = "Check-in — " + vm.SelectedReservation.ReservationNumber, Width = 1000, Height = 650, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.CanResize, ShowInTaskbar = false };
        checkInWin.Content = new CheckInStayModalView { DataContext = checkInVm };
        _ = checkInVm.InitializeAsync(() => checkInWin.Close(), System.Threading.CancellationToken.None);
        checkInWin.ShowDialog();
    }
}
