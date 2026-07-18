namespace ASMILE.BO.Core.Interfaces.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default);
}

