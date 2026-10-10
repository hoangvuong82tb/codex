using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ArCustomerWindow
{
    private readonly IServiceScope _serviceScope;

    public ArCustomerWindow()
    {
        InitializeComponent();
        var app = (App)System.Windows.Application.Current;
        var services = app.Services ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho AR Customer.");
        _serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        DataContext = _serviceScope.ServiceProvider.GetRequiredService<ArCustomerViewModel>();
        Closed += (_, _) => _serviceScope.Dispose();
    }
}
