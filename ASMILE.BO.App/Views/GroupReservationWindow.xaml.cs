using System.Windows;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupReservationWindow : Window
{
    private readonly IServiceScope? _serviceScope;

    public GroupReservationWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;

        if (Content is GroupReservationView view)
        {
            var services = (System.Windows.Application.Current as App)?.Services;
            _serviceScope = services?.CreateScope();
            view.DataContext = _serviceScope?.ServiceProvider.GetService<GroupReservationViewModel>()
                ?? new GroupReservationViewModel();
        }
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
