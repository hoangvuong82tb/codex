using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ExportPipelineService : IExportPipelineService
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly IAuditService? _auditService;
    private readonly IAuthorizationService? _authorizationService;

    public ExportPipelineService(AppDbContext db, ICurrentCompanyContext companyContext, IAuditService? auditService = null, IAuthorizationService? authorizationService = null)
    {
        _db = db;
        _companyContext = companyContext;
        _auditService = auditService;
        _authorizationService = authorizationService;
    }

    public async Task<IReadOnlyList<ExportRetentionPolicyDto>> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ExportRetentionPolicies.AsNoTracking()
            .Where(x => x.CompanyId == _companyContext.CompanyId && x.BranchId == _companyContext.BranchId)
            .OrderBy(x => x.ExportCode)
            .Select(x => new ExportRetentionPolicyDto
            {
                Id = x.Id,
                ExportCode = x.ExportCode,
                RetentionDays = x.RetentionDays,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ExportRetentionPolicyDto> SaveRetentionPolicyAsync(ExportRetentionPolicyDto policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _authorizationService?.Demand(Permission.EditSystemSettings);
        var exportCode = policy.ExportCode.Trim();
        if (exportCode.Length is < 1 or > 100 || exportCode.Any(char.IsControl))
            throw new ArgumentException("Export code must contain 1 to 100 non-control characters.", nameof(policy));
        if (policy.RetentionDays is < 1 or > 36500)
            throw new ArgumentOutOfRangeException(nameof(policy), "Retention must be between 1 and 36500 days.");

        var policies = _db.ExportRetentionPolicies
            .Where(x => x.CompanyId == _companyContext.CompanyId && x.BranchId == _companyContext.BranchId);
        var entity = policy.Id == Guid.Empty
            ? null
            : await policies.FirstOrDefaultAsync(x => x.Id == policy.Id, cancellationToken)
                ?? throw new InvalidOperationException("Export retention policy no longer exists. Refresh and try again.");
        if (await policies.AnyAsync(x => x.ExportCode == exportCode && x.Id != policy.Id, cancellationToken))
            throw new InvalidOperationException($"An export retention policy already exists for '{exportCode}'.");

        var before = entity is null ? null : new { entity.ExportCode, entity.RetentionDays, entity.IsActive };
        entity ??= new ExportRetentionPolicy
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId
        };
        entity.ExportCode = exportCode;
        entity.RetentionDays = policy.RetentionDays;
        entity.IsActive = policy.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        if (_db.Entry(entity).State == EntityState.Detached)
            _db.ExportRetentionPolicies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                nameof(ExportRetentionPolicy),
                entity.Id.ToString(),
                before is null ? "Create" : "Update",
                before,
                new { entity.ExportCode, entity.RetentionDays, entity.IsActive },
                cancellationToken: cancellationToken);
        }

        return new ExportRetentionPolicyDto
        {
            Id = entity.Id,
            ExportCode = entity.ExportCode,
            RetentionDays = entity.RetentionDays,
            IsActive = entity.IsActive
        };
    }

    public async Task<ExportExecutionDto> QueueExportAsync(string exportCode, string exportType, CancellationToken cancellationToken = default)
    {
        var execution = new ExportExecution
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            ExportCode = exportCode,
            ExportType = exportType,
            Status = "Queued",
            QueuedAtUtc = DateTime.UtcNow
        };
        _db.ExportExecutions.Add(execution);
        await _db.SaveChangesAsync(cancellationToken);
        return new ExportExecutionDto { Id = execution.Id, ExportCode = execution.ExportCode, Status = execution.Status, QueuedAtUtc = execution.QueuedAtUtc };
    }

    public async Task<ExportExecutionDto> MarkCompletedAsync(Guid exportExecutionId, CancellationToken cancellationToken = default)
    {
        var execution = await _db.ExportExecutions.FirstOrDefaultAsync(
            x => x.Id == exportExecutionId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Export execution not found.");
        execution.Status = "Completed";
        execution.CompletedAtUtc = DateTime.UtcNow;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(execution.Id.ToString("N"))));
        _db.ExportArtifacts.Add(new ExportArtifact
        {
            CompanyId = execution.CompanyId,
            BranchId = execution.BranchId,
            ExportExecutionId = execution.Id,
            FilePath = $"exports/{execution.ExportCode}-{execution.Id:N}.json",
            FileType = "json",
            Hash = hash,
            SizeBytes = hash.Length
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new ExportExecutionDto { Id = execution.Id, ExportCode = execution.ExportCode, Status = execution.Status, QueuedAtUtc = execution.QueuedAtUtc };
    }

    public async Task<int> CleanupRetentionAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var policies = await _db.ExportRetentionPolicies.AsNoTracking()
            .Where(x => x.IsActive && x.CompanyId == _companyContext.CompanyId && x.BranchId == _companyContext.BranchId)
            .ToListAsync(cancellationToken);
        var total = 0;
        foreach (var policy in policies)
        {
            var cutoff = utcNow.AddDays(-policy.RetentionDays);
            var toDelete = await _db.ExportExecutions
                .Where(x => x.ExportCode == policy.ExportCode
                    && x.CompanyId == _companyContext.CompanyId
                    && x.BranchId == _companyContext.BranchId
                    && x.CompletedAtUtc != null
                    && x.CompletedAtUtc < cutoff)
                .ToListAsync(cancellationToken);
            if (toDelete.Count == 0)
            {
                continue;
            }

            var executionIds = toDelete.Select(x => x.Id).ToArray();
            var artifacts = await _db.ExportArtifacts
                .Where(x => executionIds.Contains(x.ExportExecutionId)
                    && x.CompanyId == _companyContext.CompanyId
                    && x.BranchId == _companyContext.BranchId)
                .ToListAsync(cancellationToken);
            _db.ExportArtifacts.RemoveRange(artifacts);
            _db.ExportExecutions.RemoveRange(toDelete);
            total += toDelete.Count;
        }

        if (total > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return total;
    }

    public async Task<IReadOnlyList<ExportExecutionDto>> GetExecutionsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 300);
        return await _db.ExportExecutions.AsNoTracking()
            .Where(x => x.CompanyId == _companyContext.CompanyId && x.BranchId == _companyContext.BranchId)
            .OrderByDescending(x => x.QueuedAtUtc)
            .Take(normalizedTake)
            .Select(x => new ExportExecutionDto
            {
                Id = x.Id,
                ExportCode = x.ExportCode,
                Status = x.Status,
                QueuedAtUtc = x.QueuedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
