using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupServiceWindow
{
    private readonly IServiceScope _serviceScope;

    public GroupServiceWindow()
    {
        InitializeComponent();

        var services = (System.Windows.Application.Current as App)?.Services
            ?? throw new InvalidOperationException("Chưa khởi tạo DI container cho Dịch vụ đoàn.");
        _serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        DataContext = _serviceScope.ServiceProvider.GetRequiredService<ViewModels.GroupServiceViewModel>();
        Closed += (_, _) => _serviceScope.Dispose();
    }
}
