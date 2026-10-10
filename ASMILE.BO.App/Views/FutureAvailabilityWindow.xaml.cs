using System.Windows;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
namespace ASMILE.BO.App.Views;
public partial class FutureAvailabilityWindow : Window
{
    private readonly IServiceScope? _serviceScope;

    public FutureAvailabilityWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var app = System.Windows.Application.Current as App
            ?? throw new InvalidOperationException("Ứng dụng chưa khởi tạo dịch vụ.");
        var services = app.Services
            ?? throw new InvalidOperationException("Container dịch vụ chưa được khởi tạo.");
        _serviceScope = services.CreateScope();
        DataContext = _serviceScope.ServiceProvider.GetService<FutureAvailabilityViewModel>()
            ?? throw new InvalidOperationException("FutureAvailabilityViewModel chưa được đăng ký.");
        Loaded += async (_, _) => await ((FutureAvailabilityViewModel)DataContext).InitializeAsync();
        Closed += (_, _) => _serviceScope.Dispose();
    }
}
