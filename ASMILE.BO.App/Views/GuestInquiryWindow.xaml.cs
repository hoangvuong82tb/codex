using System.Windows;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GuestInquiryWindow
{
    private readonly IServiceScope? _serviceScope;

    public GuestInquiryWindow()
    {
        InitializeComponent();

        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<GuestInquiryViewModel>()
            ?? throw new InvalidOperationException(
                "Guest Inquiry requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
