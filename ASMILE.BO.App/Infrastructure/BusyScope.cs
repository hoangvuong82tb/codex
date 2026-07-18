using System.Windows.Input;

namespace ASMILE.BO.App.Infrastructure;

public sealed class BusyScope : IDisposable
{
    private readonly Action<bool> _setBusy;
    private bool _disposed;

    private BusyScope(Action<bool> setBusy)
    {
        _setBusy = setBusy;
        _setBusy(true);
        Mouse.OverrideCursor = Cursors.Wait;
    }

    public static BusyScope Enter(Action<bool> setBusy)
    {
        return new BusyScope(setBusy);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _setBusy(false);
        Mouse.OverrideCursor = null;
    }
}
