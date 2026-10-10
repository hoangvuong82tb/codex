using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class MinibarWindow
{
    private readonly IServiceScope? _serviceScope;
    public Guid? InitialRoomId { get; }

    public MinibarWindow() : this(null) { }

    public MinibarWindow(Guid? initialRoomId = null)
    {
        InitializeComponent();
        InitialRoomId = initialRoomId;

        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<MinibarViewModel>()
            ?? throw new InvalidOperationException(
                "Minibar requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
