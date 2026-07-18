using System.Reflection;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class CurrentCompanyContext : ICurrentCompanyContext
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICompanyContextAccessor _contextAccessor;
    private IReadOnlyList<CompanyContextOptionDto>? _companies;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, IReadOnlyList<BranchContextOptionDto>> _branchCache = new();

    public CurrentCompanyContext(IServiceScopeFactory scopeFactory, ICompanyContextAccessor contextAccessor)
    {
        _scopeFactory = scopeFactory;
        _contextAccessor = contextAccessor;
    }

    public Guid? CompanyId => _contextAccessor.CurrentCompanyId;
    public Guid? BranchId => _contextAccessor.CurrentBranchId;
    public string CompanyName => _contextAccessor.CurrentCompanyName ?? "Default Company";
    public string? BranchName => _contextAccessor.CurrentBranchName;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var companies = await GetCompaniesAsync(cancellationToken);
        await TryRestoreActiveSessionAsync(companies, cancellationToken);
        if (CompanyId is null)
        {
            var selected = companies.FirstOrDefault(x => x.IsDefault) ?? companies.FirstOrDefault();
            if (selected is not null)
            {
                _contextAccessor.SetCurrentScope(selected.Id, selected.Name);
            }
        }

        await GetBranchesAsync(cancellationToken);
    }

    public async Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = (await GetCompaniesAsync(cancellationToken)).FirstOrDefault(x => x.Id == companyId);
        if (company is null)
        {
            throw new InvalidOperationException("Company is not available for the current user.");
        }

        _contextAccessor.SetCurrentScope(company.Id, company.Name);
        _branchCache.Clear();
        await GetBranchesAsync(cancellationToken);
        await UpsertActiveCompanySessionAsync(cancellationToken);
    }

    public async Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
    {
        if (branchId is null)
        {
            _contextAccessor.SetCurrentScope(CompanyId, CompanyName);
            await UpsertActiveCompanySessionAsync(cancellationToken);
            return;
        }

        var branch = (await GetBranchesAsync(cancellationToken)).FirstOrDefault(x => x.Id == branchId.Value);
        if (branch is null)
        {
            throw new InvalidOperationException("Branch is not available for the current company.");
        }

        _contextAccessor.SetCurrentScope(CompanyId, CompanyName, branch.Id, branch.Name);
        await UpsertActiveCompanySessionAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        if (_companies is not null)
        {
            return _companies;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EnsureDefaultCompanyAsync(db, cancellationToken);
        _companies = await db.Companies
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsArchived)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new CompanyContextOptionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsDefault = x.IsDefault
            })
            .ToListAsync(cancellationToken);
        return _companies;
    }

    public async Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCompanySelectedAsync(cancellationToken);
        var companyId = CompanyId ?? DefaultCompanyId;
        if (_branchCache.TryGetValue(companyId, out var cached))
        {
            return cached;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EnsureDefaultCompanyAsync(db, cancellationToken);
        var branches = await db.Branches
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsArchived)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => new BranchContextOptionDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                Code = x.Code,
                Name = x.Name,
                IsDefault = x.IsDefault
            })
            .ToListAsync(cancellationToken);
        _branchCache[companyId] = branches;
        return branches;
    }

    private async Task EnsureCompanySelectedAsync(CancellationToken cancellationToken)
    {
        if (CompanyId.HasValue)
        {
            return;
        }

        var companies = await GetCompaniesAsync(cancellationToken);
        var selected = companies.FirstOrDefault(x => x.IsDefault) ?? companies.FirstOrDefault();
        if (selected is not null)
        {
            _contextAccessor.SetCurrentScope(selected.Id, selected.Name);
        }
    }

    private async Task<bool> TryRestoreActiveSessionAsync(IReadOnlyList<CompanyContextOptionDto> companies, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();
        if (currentUser?.CurrentUser is null || currentUser.CurrentSessionId is null)
        {
            return false;
        }

        await CleanupExpiredCompanySessionsAsync(db, cancellationToken);

        var sessionId = currentUser.CurrentSessionId.Value;
        var userId = currentUser.CurrentUser.Id;
        var session = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.SessionId == sessionId && x.IsActive)
            .OrderByDescending(x => x.LastActivityAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return false;
        }

        var company = companies.FirstOrDefault(x => x.Id == session.CompanyId) ?? companies.FirstOrDefault(x => x.IsDefault) ?? companies.FirstOrDefault();
        if (company is null)
        {
            return false;
        }

        var branchName = session.BranchId is null
            ? null
            : await db.Branches.AsNoTracking()
                .Where(x => x.Id == session.BranchId && x.CompanyId == company.Id && x.IsActive && !x.IsArchived)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);

        _contextAccessor.SetCurrentScope(company.Id, company.Name, session.BranchId, branchName);
        return true;
    }

    private async Task UpsertActiveCompanySessionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();
        if (currentUser?.CurrentUser is null || currentUser.CurrentSessionId is null)
        {
            return;
        }

        await CleanupExpiredCompanySessionsAsync(db, cancellationToken);

        var now = DateTime.UtcNow;
        var sessionId = currentUser.CurrentSessionId.Value;
        var userId = currentUser.CurrentUser.Id;
        var row = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId, cancellationToken);

        if (row is null)
        {
            row = new ActiveCompanySession
            {
                UserId = userId,
                SessionId = sessionId,
                CompanyId = CompanyId ?? DefaultCompanyId,
                BranchId = BranchId,
                Status = "Active",
                Source = "desktop-shell",
                MachineName = Environment.MachineName,
                AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
                SessionMetadataJson = JsonSerializer.Serialize(new
                {
                    company = CompanyName,
                    branch = BranchName,
                    user = currentUser.CurrentUser.UserName
                })
            };

            db.ActiveCompanySessions.Add(row);
        }
        else
        {
            row.CompanyId = CompanyId ?? row.CompanyId ?? DefaultCompanyId;
            row.BranchId = BranchId;
            row.Status = "Active";
            row.IsActive = true;
            row.IsArchived = false;
            row.LastActivityAtUtc = now;
            row.CompletedAtUtc = null;
            row.ExpiresAtUtc = now.AddHours(8);
            row.ModifiedAtUtc = now;
        }

        row.StartedAtUtc = row.StartedAtUtc == default ? now : row.StartedAtUtc;
        row.LastActivityAtUtc = now;
        row.ExpiresAtUtc = now.AddHours(8);
        row.ModifiedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task CleanupExpiredCompanySessionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiredSessions = await db.ActiveCompanySessions.IgnoreQueryFilters()
            .Where(x => x.IsActive && x.ExpiresAtUtc != null && x.ExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0)
        {
            return;
        }

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.Status = "Abandoned";
            session.CompletedAtUtc = now;
            session.ModifiedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultCompanyAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Companies.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.Companies.Add(new Company
        {
            Id = DefaultCompanyId,
            Code = "DEFAULT",
            Name = "Default Company",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        });
        db.Branches.Add(new Branch
        {
            Id = DefaultBranchId,
            CompanyId = DefaultCompanyId,
            Code = "MAIN",
            Name = "Main Branch",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
