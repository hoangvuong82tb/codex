using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class LostFoundWindow
{
    private readonly IServiceScope? _serviceScope;
    public string? InitialSearchText { get; }

    public LostFoundWindow() : this(null) { }

    public LostFoundWindow(string? initialSearchText = null)
    {
        InitializeComponent();
        InitialSearchText = initialSearchText;
        var app = global::System.Windows.Application.Current as App;
        _serviceScope = app?.Services?.CreateScope();
        DataContext = (_serviceScope?.ServiceProvider ?? app?.Services)?.GetRequiredService<LostFoundViewModel>()
            ?? throw new InvalidOperationException("Lost & Found requires the application service provider.");
        Closed += (_, _) => _serviceScope?.Dispose();
    }
}
