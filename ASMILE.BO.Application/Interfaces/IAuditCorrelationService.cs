namespace ASMILE.BO.Application.Interfaces;

public interface IAuditCorrelationService
{
    string? CurrentCorrelationId { get; }
    string? ParentCorrelationId { get; }
    string EnsureCorrelationId(string? correlationId = null);
    IDisposable BeginScope(string? correlationId = null);
    Task<T> WithCorrelationAsync<T>(string? correlationId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task WithCorrelationAsync(string? correlationId, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
