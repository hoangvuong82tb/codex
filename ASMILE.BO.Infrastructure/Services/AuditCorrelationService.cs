using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AuditCorrelationService : IAuditCorrelationService
{
    private static readonly AsyncLocal<Stack<string>?> CorrelationStack = new();

    public string? CurrentCorrelationId => CorrelationStack.Value is { Count: > 0 } stack ? stack.Peek() : null;
    public string? ParentCorrelationId
    {
        get
        {
            var stack = CorrelationStack.Value;
            if (stack is null || stack.Count < 2)
            {
                return null;
            }

            return stack.Skip(1).FirstOrDefault();
        }
    }

    public string EnsureCorrelationId(string? correlationId = null)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        return CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
    }

    public IDisposable BeginScope(string? correlationId = null)
    {
        var value = EnsureCorrelationId(correlationId);
        CorrelationStack.Value ??= new Stack<string>();
        CorrelationStack.Value.Push(value);
        return new Scope(value);
    }

    public async Task<T> WithCorrelationAsync<T>(string? correlationId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var scope = BeginScope(correlationId);
        cancellationToken.ThrowIfCancellationRequested();
        return await action(cancellationToken);
    }

    public async Task WithCorrelationAsync(string? correlationId, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var scope = BeginScope(correlationId);
        cancellationToken.ThrowIfCancellationRequested();
        await action(cancellationToken);
    }

    private sealed class Scope : IDisposable
    {
        private readonly string _value;
        private bool _disposed;

        public Scope(string value)
        {
            _value = value;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            var stack = CorrelationStack.Value;
            if (stack is null || stack.Count == 0)
            {
                return;
            }

            if (string.Equals(stack.Peek(), _value, StringComparison.Ordinal))
            {
                stack.Pop();
                if (stack.Count == 0)
                {
                    CorrelationStack.Value = null;
                }
            }
        }
    }
}
