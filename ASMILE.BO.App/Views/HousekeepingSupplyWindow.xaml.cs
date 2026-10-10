using System;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class HousekeepingSupplyWindow
{
    private IServiceScope? _dataScope;

    public HousekeepingSupplyWindow() : this(forceMock: false)
    {
    }

    /// <summary>
    /// Keeps the parameterless route on the real persistence-backed view.
    /// The optional flag is retained only for isolated design/test hosts;
    /// the production housekeeping hub never passes it.
    /// </summary>
    public HousekeepingSupplyWindow(bool forceMock)
    {
        InitializeComponent();
        if (forceMock)
            return;

        var app = global::System.Windows.Application.Current as App;
        var services = app?.Services
            ?? throw new InvalidOperationException("Housekeeping supply management requires the application service provider and a real execution context.");

        _dataScope = services.CreateScope();
        try
        {
            DataContext = _dataScope.ServiceProvider.GetRequiredService<HousekeepingSupplyViewModel>();
            Closed += OnWindowClosed;
        }
        catch
        {
            _dataScope.Dispose();
            _dataScope = null;
            throw;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        _dataScope?.Dispose();
        _dataScope = null;
    }
}
