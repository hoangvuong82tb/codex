using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ExportPipelineService : IExportPipelineService
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;

    public ExportPipelineService(AppDbContext db, ICurrentCompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
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
        var execution = await _db.ExportExecutions.FirstOrDefaultAsync(x => x.Id == exportExecutionId, cancellationToken)
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
        var policies = await _db.ExportRetentionPolicies.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var total = 0;
        foreach (var policy in policies)
        {
            var cutoff = utcNow.AddDays(-policy.RetentionDays);
            var toDelete = await _db.ExportExecutions
                .Where(x => x.ExportCode == policy.ExportCode && x.CompletedAtUtc != null && x.CompletedAtUtc < cutoff)
                .ToListAsync(cancellationToken);
            if (toDelete.Count == 0)
            {
                continue;
            }

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
