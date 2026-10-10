using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ArAgingWindow
{
    private readonly IServiceScope _serviceScope;

    public ArAgingWindow()
    {
        InitializeComponent();
        var app = (App)System.Windows.Application.Current;
        var services = app.Services ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho AR Aging.");
        _serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        DataContext = _serviceScope.ServiceProvider.GetRequiredService<ArAgingViewModel>();
        Closed += (_, _) => _serviceScope.Dispose();
    }
}
