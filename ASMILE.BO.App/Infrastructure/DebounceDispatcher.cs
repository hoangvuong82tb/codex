using System.Windows.Threading;

namespace ASMILE.BO.App.Infrastructure;

public sealed class DebounceDispatcher : IDisposable
{
    private readonly DispatcherTimer _timer;
    private Func<Task>? _callback;
    private bool _disposed;

    public DebounceDispatcher(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += OnTick;
    }

    public void Debounce(Func<Task> callback)
    {
        ThrowIfDisposed();
        _callback = callback;
        _timer.Stop();
        _timer.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        var callback = _callback;
        if (callback is not null)
        {
            await callback().ConfigureAwait(true);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DebounceDispatcher));
        }
    }
}
