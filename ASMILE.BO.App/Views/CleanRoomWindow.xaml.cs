using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CleanRoomWindow
{
    private readonly IServiceScope? _serviceScope;

    public CleanRoomWindow()
    {
        InitializeComponent();
        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<CleanRoomViewModel>()
            ?? throw new InvalidOperationException("Clean Room requires the application service provider.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
