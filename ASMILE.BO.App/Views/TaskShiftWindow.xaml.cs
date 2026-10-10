using System;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class TaskShiftWindow
{
    private IServiceScope? _dataScope;

    public TaskShiftWindow() : this(forceMock: false)
    {
    }

    /// <summary>
    /// Keeps direct opens on the real Task &amp; Shift service. The optional flag
    /// is retained only for isolated design/test hosts; production navigation
    /// uses the parameterless constructor.
    /// </summary>
    public TaskShiftWindow(bool forceMock)
    {
        InitializeComponent();
        if (forceMock)
            return;

        var app = global::System.Windows.Application.Current as App;
        var services = app?.Services
            ?? throw new InvalidOperationException("Ứng dụng chưa khởi tạo dịch vụ Task & Shift.");

        _dataScope = services.CreateScope();
        try
        {
            var service = _dataScope.ServiceProvider.GetRequiredService<ITaskShiftService>();
            DataContext = new TaskShiftViewModel(service);
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
