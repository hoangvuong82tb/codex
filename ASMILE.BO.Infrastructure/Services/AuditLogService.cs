using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;

    public AuditLogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Detail = detail,
            Username = username,
            CreatedAt = DateTime.Now
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

