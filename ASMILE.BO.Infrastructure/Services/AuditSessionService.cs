using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AuditSessionService : IAuditSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentUserService? _currentUserService;

    public AuditSessionService(AppDbContext dbContext, IAuditCorrelationService? correlationService = null, ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public async Task<AuditSessionDto> StartSessionAsync(string source, string? correlationId = null, string? clientMachine = null, string? deviceId = null, string? platform = null, string? metadataJson = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var effectiveCorrelationId = _correlationService?.EnsureCorrelationId(correlationId) ?? correlationId ?? Guid.NewGuid().ToString("N");
        var session = await _dbContext.AuditSessions.FirstOrDefaultAsync(x => x.CorrelationId == effectiveCorrelationId, cancellationToken);
        if (session is null)
        {
            session = new AuditSession
            {
                CorrelationId = effectiveCorrelationId,
                UserId = _currentUserService?.CurrentUser?.Id,
                CompanyId = _dbContext.CurrentCompanyId,
                BranchId = _dbContext.CurrentBranchId,
                StartedAtUtc = now,
                LastActivityAtUtc = now,
                Status = AuditSessionStatus.Started,
                Source = source,
                ClientMachine = clientMachine ?? Environment.MachineName,
                AppVersion = typeof(AuditSessionService).Assembly.GetName().Version?.ToString(),
                DeviceId = deviceId,
                Platform = platform ?? Environment.OSVersion.Platform.ToString(),
                SessionMetadataJson = metadataJson ?? BuildMetadata(source, clientMachine, deviceId, platform)
            };
            _dbContext.AuditSessions.Add(session);
        }
        else if (session.Status is AuditSessionStatus.Started or AuditSessionStatus.Active)
        {
            session.Status = AuditSessionStatus.Active;
            session.LastActivityAtUtc = now;
            session.DurationMs = CalculateDurationMs(session.StartedAtUtc, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task HeartbeatAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await _dbContext.AuditSessions.FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken)
            ?? throw new InvalidOperationException($"Audit session '{correlationId}' was not found.");
        if (session.Status is AuditSessionStatus.Completed or AuditSessionStatus.Abandoned or AuditSessionStatus.Expired)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.Status = AuditSessionStatus.Active;
        session.LastActivityAtUtc = now;
        session.DurationMs = CalculateDurationMs(session.StartedAtUtc, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteSessionAsync(string correlationId, string? reason = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await _dbContext.AuditSessions.FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken)
            ?? throw new InvalidOperationException($"Audit session '{correlationId}' was not found.");
        if (session.Status is AuditSessionStatus.Completed)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.Status = AuditSessionStatus.Completed;
        session.CompletedAtUtc = now;
        session.LastActivityAtUtc = now;
        session.DurationMs = CalculateDurationMs(session.StartedAtUtc, now);
        session.CompletedReason = string.IsNullOrWhiteSpace(reason) ? "Completed" : reason;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> AbandonExpiredSessionsAsync(DateTime utcNow, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cutoff = utcNow.Subtract(timeout);
        var expiredCutoff = utcNow.Subtract(TimeSpan.FromTicks(timeout.Ticks * 2));
        var sessions = await _dbContext.AuditSessions
            .Where(x => (x.Status == AuditSessionStatus.Started || x.Status == AuditSessionStatus.Active) && x.LastActivityAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Status = session.LastActivityAtUtc < expiredCutoff ? AuditSessionStatus.Expired : AuditSessionStatus.Abandoned;
            session.CompletedAtUtc = utcNow;
            session.DurationMs = CalculateDurationMs(session.StartedAtUtc, utcNow);
            session.CompletedReason = session.Status.ToString();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }

    public async Task<IReadOnlyList<AuditSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditSessions.AsNoTracking()
            .Where(x => x.Status == AuditSessionStatus.Started || x.Status == AuditSessionStatus.Active)
            .OrderByDescending(x => x.LastActivityAtUtc)
            .Select(x => new AuditSessionDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                UserId = x.UserId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                LastActivityAtUtc = x.LastActivityAtUtc,
                DurationMs = x.DurationMs,
                Status = x.Status,
                Source = x.Source,
                ClientMachine = x.ClientMachine,
                AppVersion = x.AppVersion,
                DeviceId = x.DeviceId,
                Platform = x.Platform,
                CompletedReason = x.CompletedReason,
                SessionMetadataJson = x.SessionMetadataJson
            })
            .ToListAsync(cancellationToken);
    }

    private static AuditSessionDto ToDto(AuditSession session)
        => new()
        {
            Id = session.Id,
            CorrelationId = session.CorrelationId,
            UserId = session.UserId,
            CompanyId = session.CompanyId,
            BranchId = session.BranchId,
            StartedAtUtc = session.StartedAtUtc,
            CompletedAtUtc = session.CompletedAtUtc,
            LastActivityAtUtc = session.LastActivityAtUtc,
            DurationMs = session.DurationMs,
            Status = session.Status,
            Source = session.Source,
            ClientMachine = session.ClientMachine,
            AppVersion = session.AppVersion,
            DeviceId = session.DeviceId,
            Platform = session.Platform,
            CompletedReason = session.CompletedReason,
            SessionMetadataJson = session.SessionMetadataJson
        };

    private static string BuildMetadata(string source, string? clientMachine, string? deviceId, string? platform)
        => JsonSerializer.Serialize(new
        {
            source,
            clientMachine = clientMachine ?? Environment.MachineName,
            deviceId,
            platform = platform ?? Environment.OSVersion.Platform.ToString()
        }, JsonOptions);

    private static long CalculateDurationMs(DateTime startedAtUtc, DateTime endedAtUtc)
        => Math.Max(0, (long)(endedAtUtc - startedAtUtc).TotalMilliseconds);
}
