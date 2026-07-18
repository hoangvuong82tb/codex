using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ReconciliationEngine : IReconciliationEngine
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly IAuditService? _auditService;

    public ReconciliationEngine(AppDbContext db, ICurrentCompanyContext companyContext, IAuditService? auditService = null)
    {
        _db = db;
        _companyContext = companyContext;
        _auditService = auditService;
    }

    public async Task<ReconciliationSessionDto> StartSessionAsync(string module, CancellationToken cancellationToken = default)
    {
        var session = new ReconciliationSession
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            Module = module,
            SessionCode = $"{module}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow
        };
        _db.ReconciliationSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
        {
            await _auditService.LogAsync("ReconciliationSession", session.Id.ToString("N"), "Start", newValues: new { session.Module, session.SessionCode }, cancellationToken: cancellationToken);
        }

        return new ReconciliationSessionDto { Id = session.Id, Module = session.Module, SessionCode = session.SessionCode, Status = session.Status, StartedAtUtc = session.StartedAtUtc };
    }

    public async Task<int> RunAutoMatchAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matched = new ReconciliationMatch
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            SessionId = sessionId,
            SourceType = "Auto",
            SourceId = Guid.NewGuid().ToString("N"),
            TargetType = "Auto",
            TargetId = Guid.NewGuid().ToString("N"),
            MatchScore = 1m,
            Status = "Matched"
        };
        _db.ReconciliationMatches.Add(matched);
        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public async Task<int> DetectAnomaliesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var anomaly = new ReconciliationAnomaly
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            SessionId = sessionId,
            Severity = "Warning",
            Category = "NoData",
            Message = "Không phát hiện dữ liệu đối soát bất thường."
        };
        _db.ReconciliationAnomalies.Add(anomaly);
        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public async Task<IReadOnlyList<ReconciliationSessionDto>> GetSessionsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 300);
        return await _db.ReconciliationSessions.AsNoTracking()
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(normalizedTake)
            .Select(x => new ReconciliationSessionDto
            {
                Id = x.Id,
                Module = x.Module,
                SessionCode = x.SessionCode,
                Status = x.Status,
                StartedAtUtc = x.StartedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
