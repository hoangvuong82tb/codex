using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ArPaymentWindow
{
    private readonly IServiceScope? _serviceScope;

    public ArPaymentWindow()
    {
        InitializeComponent();

        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        DataContext = scopedServices?.GetService<ArPaymentViewModel>() ?? new ArPaymentViewModel();
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
