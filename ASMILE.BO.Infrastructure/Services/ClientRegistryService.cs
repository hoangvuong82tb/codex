using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ClientRegistryService : IClientRegistryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public ClientRegistryService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _dbContextFactory = dbContextFactory;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ClientRegistryEntryDto>> GetActiveClientsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return await dbContext.ActiveCompanySessions
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsArchived && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.LastActivityAtUtc)
            .Select(x => new ClientRegistryEntryDto
            {
                Id = x.Id,
                UserId = x.UserId,
                SessionId = x.SessionId,
                Status = x.Status,
                MachineName = x.MachineName ?? string.Empty,
                Source = x.Source ?? string.Empty,
                AppVersion = x.AppVersion ?? string.Empty,
                StartedAtUtc = x.StartedAtUtc,
                LastActivityAtUtc = x.LastActivityAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _authorizationService.Demand(Permission.ManageUsers);
        if (sessionId == Guid.Empty)
        {
            return false;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // Resolve the target through the active company/branch query filters first.
        // Only after validating this visible row do we invalidate its globally-scoped login session.
        var visiblePresence = await dbContext.ActiveCompanySessions
            .FirstOrDefaultAsync(x => x.SessionId == sessionId
                && x.IsActive
                && !x.IsArchived
                && x.ExpiresAtUtc > now,
                cancellationToken);
        if (visiblePresence is null)
        {
            return false;
        }

        var loginSession = await dbContext.LoginSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId
                && x.UserId == visiblePresence.UserId
                && x.IsActive
                && x.ExpiresAt > now,
                cancellationToken);
        if (loginSession is null)
        {
            return false;
        }

        loginSession.IsActive = false;
        loginSession.EndedAt = now;

        // LoginSession is global to the authenticated client. Clear every company presence
        // linked to it so it cannot remain visible as active in another company context.
        var presences = await dbContext.ActiveCompanySessions
            .IgnoreQueryFilters()
            .Where(x => x.SessionId == sessionId && x.IsActive && !x.IsArchived)
            .ToListAsync(cancellationToken);
        foreach (var presence in presences)
        {
            presence.IsActive = false;
            presence.Status = "Revoked";
            presence.CompletedAtUtc = now;
            presence.ExpiresAtUtc = now;
            presence.ModifiedAtUtc = now;
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            CompanyId = visiblePresence.CompanyId,
            BranchId = visiblePresence.BranchId,
            CreatedAt = now,
            Username = _currentUserService.CurrentUser?.UserName is { Length: > 0 } userName ? userName : "system",
            Action = "WorkstationSessionRevoked",
            Detail = $"An administrator revoked an active workstation login session. Presence records closed: {presences.Count}.",
            EntityName = nameof(LoginSession),
            EntityId = sessionId.ToString("N")
        });

        // A single SaveChanges keeps authentication invalidation, presence cleanup,
        // and its audit record in the same database transaction.
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
