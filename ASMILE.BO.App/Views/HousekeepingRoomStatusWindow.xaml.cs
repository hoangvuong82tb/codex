using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingRoomStatusWindow
{
    private readonly IServiceScope? _serviceScope;

    public HousekeepingRoomStatusWindow()
    {
        InitializeComponent();
        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<HousekeepingRoomStatusViewModel>()
            ?? throw new InvalidOperationException(
                "Housekeeping Room Status requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
