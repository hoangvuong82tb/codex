using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CleaningInProgressWindow
{
    private readonly IServiceScope? _serviceScope;

    public CleaningInProgressWindow()
    {
        InitializeComponent();

        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<CleaningInProgressViewModel>()
            ?? throw new InvalidOperationException(
                "Cleaning In Progress requires the application service provider and a real execution context.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
