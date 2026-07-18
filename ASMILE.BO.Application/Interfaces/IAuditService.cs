using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> QueryAsync(
        string? entityName = null,
        string? entityId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}
