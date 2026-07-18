using ASMILE.BO.Core.Security;

namespace ASMILE.BO.Application.Interfaces;

public interface ISecurityAuditService
{
    Task LogAsync(
        SecurityEventType eventType,
        string module,
        string entityType,
        string? entityId,
        string message,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
