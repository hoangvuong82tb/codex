using System.Windows;
using ASMILE.BO.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ASMILE.BO.App.Services;

public interface ILockedRoomWindowService
{
    bool IsOpen { get; }
    void Open();
}

/// <summary>
/// Owns the single Locked Room window for the application so transient Reception
/// view-model lifetimes cannot create duplicate windows.
/// </summary>
public sealed class LockedRoomWindowService(IServiceProvider serviceProvider) : ILockedRoomWindowService
{
    private LockedRoomWindow? window;
    private bool isOpening;

    public bool IsOpen => window?.IsLoaded == true;

    public void Open()
    {
        if (window?.IsLoaded == true)
        {
            RestoreAndActivate(window);
            return;
        }

        if (isOpening)
        {
            return;
        }

        isOpening = true;
        try
        {
            var newWindow = serviceProvider.GetRequiredService<LockedRoomWindow>();
            var owner = System.Windows.Application.Current?.MainWindow;
            if (owner?.IsLoaded == true && !ReferenceEquals(owner, newWindow))
            {
                newWindow.Owner = owner;
            }

            newWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            newWindow.ResizeMode = ResizeMode.CanResize;
            newWindow.ShowInTaskbar = false;
            newWindow.Closed += OnWindowClosed;
            window = newWindow;
            newWindow.Show();
        }
        catch (Exception ex)
        {
            window = null;
            Log.Error(ex, "Failed to open LockedRoomWindow from Reception Hub.");
            throw;
        }
        finally
        {
            isOpening = false;
        }
    }

    private static void RestoreAndActivate(Window existingWindow)
    {
        if (existingWindow.WindowState == WindowState.Minimized)
        {
            existingWindow.WindowState = WindowState.Normal;
        }

        existingWindow.Activate();
        existingWindow.Focus();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is LockedRoomWindow closedWindow)
        {
            closedWindow.Closed -= OnWindowClosed;
        }

        if (ReferenceEquals(window, sender))
        {
            window = null;
        }
    }
}
