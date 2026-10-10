using System.Windows;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class RoomMasterWindow
{
    private readonly IServiceScope? _serviceScope;

    public RoomMasterWindow()
    {
        InitializeComponent();

        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<RoomMasterViewModel>()
            ?? throw new InvalidOperationException(
                "Room Master requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
