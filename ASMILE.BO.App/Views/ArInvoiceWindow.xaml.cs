using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ArInvoiceWindow
{
    private readonly IServiceScope _serviceScope;

    public ArInvoiceWindow()
    {
        InitializeComponent();
        var app = (App)System.Windows.Application.Current;
        var services = app.Services ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho AR Invoice.");
        _serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        DataContext = _serviceScope.ServiceProvider.GetRequiredService<ArInvoiceViewModel>();
        Closed += (_, _) => _serviceScope.Dispose();
    }
}
