using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Shared.Security;
using System.Text.Json;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class SecurityAuditService : ISecurityAuditService
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public SecurityAuditService(IAuditLogService auditLogService, ICurrentUserService currentUserService)
    {
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    public Task LogAsync(SecurityEventType eventType, string module, string entityType, string? entityId, string message, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        var sanitized = SecretMasker.SanitizeMetadata(metadata);
        var payload = new
        {
            module,
            entityType,
            entityId,
            metadata = sanitized
        };

        var detail = $"[{eventType}] {message} | {JsonSerializer.Serialize(payload)}";
        return _auditLogService.LogAsync(eventType.ToString(), detail, _currentUserService.CurrentUser?.UserName ?? "system", cancellationToken);
    }
}

