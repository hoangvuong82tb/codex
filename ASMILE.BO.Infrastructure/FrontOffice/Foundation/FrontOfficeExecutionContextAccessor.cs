using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class FrontOfficeExecutionContextAccessor : IFrontOfficeExecutionContextAccessor
{
    private FrontOfficeExecutionContext? _current;
    public FrontOfficeExecutionContext? Current => _current;
    public IDisposable BeginScope(FrontOfficeExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var previous = _current;
        _current = context;
        return new Scope(this, previous);
    }
    private sealed class Scope(FrontOfficeExecutionContextAccessor owner, FrontOfficeExecutionContext? previous) : IDisposable
    {
        public void Dispose() => owner._current = previous;
    }
}

public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
