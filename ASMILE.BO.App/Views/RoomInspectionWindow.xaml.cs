using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class RoomInspectionWindow
{
    private readonly IServiceScope? _serviceScope;

    public RoomInspectionWindow()
    {
        InitializeComponent();
        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<RoomInspectionViewModel>()
            ?? throw new InvalidOperationException(
                "Room Inspection requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
