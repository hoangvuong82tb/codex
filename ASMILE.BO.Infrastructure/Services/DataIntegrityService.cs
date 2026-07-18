using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class DataIntegrityService : IDataIntegrityService
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;

    public DataIntegrityService(AppDbContext db, ICurrentCompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<IReadOnlyList<DataIntegrityIssueDto>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var issues = await _db.DataIntegrityScanResults.AsNoTracking()
            .OrderByDescending(x => x.ScannedAtUtc)
            .Take(200)
            .Select(x => new DataIntegrityIssueDto
            {
                Id = x.Id,
                Module = x.Module,
                Severity = x.Severity,
                Message = x.Message,
                IsCorrupted = x.IsCorrupted
            }).ToListAsync(cancellationToken);
        if (issues.Count == 0)
        {
            return [new DataIntegrityIssueDto { Id = Guid.Empty, Module = "System", Severity = "Info", Message = "Chưa có issue toàn vẹn dữ liệu.", IsCorrupted = false }];
        }

        return issues;
    }

    public async Task<Guid> CreateCheckpointAsync(string checkpointCode, CancellationToken cancellationToken = default)
    {
        var payload = $"{checkpointCode}:{DateTime.UtcNow:O}:{_companyContext.CompanyId}:{_companyContext.BranchId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var checkpoint = new DataIntegrityCheckpoint
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            CheckpointCode = checkpointCode,
            SnapshotJson = payload,
            SnapshotHash = hash
        };
        _db.DataIntegrityCheckpoints.Add(checkpoint);
        await _db.SaveChangesAsync(cancellationToken);
        return checkpoint.Id;
    }

    public async Task<Guid> QueueRepairAsync(string operationCode, CancellationToken cancellationToken = default)
    {
        var operation = new DataRepairOperation
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            OperationCode = operationCode,
            Status = "Queued",
            RequestedAtUtc = DateTime.UtcNow
        };
        _db.DataRepairOperations.Add(operation);
        await _db.SaveChangesAsync(cancellationToken);
        return operation.Id;
    }
}
