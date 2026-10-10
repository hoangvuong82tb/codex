using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class DirtyRoomWindow
{
    private readonly IServiceScope? _serviceScope;

    public DirtyRoomWindow()
    {
        InitializeComponent();
        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<DirtyRoomViewModel>()
            ?? throw new InvalidOperationException("Dirty Room requires the application service provider.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
