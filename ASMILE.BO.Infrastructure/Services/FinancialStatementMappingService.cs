using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class FinancialStatementMappingService : IFinancialStatementMappingService
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly IRuntimeTelemetryService? _telemetryService;

    public FinancialStatementMappingService(
        AppDbContext db,
        ICurrentCompanyContext companyContext,
        IAuditService? auditService = null,
        IAuditCorrelationService? correlationService = null,
        IRuntimeTelemetryService? telemetryService = null)
    {
        _db = db;
        _companyContext = companyContext;
        _auditService = auditService;
        _correlationService = correlationService;
        _telemetryService = telemetryService;
    }

    public async Task<IReadOnlyList<FinancialTemplateDto>> GetTemplatesAsync(string? standardCode = null, CancellationToken cancellationToken = default)
    {
        var query = _db.FinancialTemplates.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(standardCode))
        {
            query = query.Where(x => x.StandardCode == standardCode);
        }

        return await query.OrderBy(x => x.TemplateCode).Select(x => new FinancialTemplateDto
        {
            Id = x.Id,
            StandardCode = x.StandardCode,
            TemplateCode = x.TemplateCode,
            Name = x.Name,
            StatementType = x.StatementType,
            IsActive = x.IsActive
        }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialTemplateVersionDto>> GetTemplateVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _db.FinancialTemplateVersions.AsNoTracking()
            .Where(x => x.FinancialTemplateId == templateId)
            .OrderByDescending(x => x.EffectiveFromUtc)
            .Select(x => new FinancialTemplateVersionDto
            {
                Id = x.Id,
                FinancialTemplateId = x.FinancialTemplateId,
                VersionCode = x.VersionCode,
                IsActiveVersion = x.IsActiveVersion,
                EffectiveFromUtc = x.EffectiveFromUtc,
                EffectiveToUtc = x.EffectiveToUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FinancialTemplateVersionDto> ActivateVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _db.FinancialTemplateVersions.FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken)
            ?? throw new KeyNotFoundException("Financial template version not found.");

        var versions = await _db.FinancialTemplateVersions
            .Where(x => x.FinancialTemplateId == version.FinancialTemplateId)
            .ToListAsync(cancellationToken);
        foreach (var item in versions)
        {
            item.IsActiveVersion = item.Id == versionId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new FinancialTemplateVersionDto
        {
            Id = version.Id,
            FinancialTemplateId = version.FinancialTemplateId,
            VersionCode = version.VersionCode,
            IsActiveVersion = true,
            EffectiveFromUtc = version.EffectiveFromUtc,
            EffectiveToUtc = version.EffectiveToUtc
        };
    }

    public async Task<FinancialStatementSnapshotDto> CaptureSnapshotAsync(Guid templateVersionId, string periodCode, CancellationToken cancellationToken = default)
    {
        var version = await _db.FinancialTemplateVersions
            .Include(x => x.Formulas)
            .FirstOrDefaultAsync(x => x.Id == templateVersionId, cancellationToken)
            ?? throw new KeyNotFoundException("Financial template version not found.");

        var payload = $"template:{version.FinancialTemplateId};version:{version.VersionCode};period:{periodCode};formulaCount:{version.Formulas.Count}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var snapshot = new Core.Entities.FinancialStatementSnapshot
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            FinancialTemplateVersionId = templateVersionId,
            PeriodCode = periodCode,
            CapturedAtUtc = DateTime.UtcNow,
            SnapshotJson = payload,
            SnapshotHash = hash,
            CorrelationId = _correlationService?.CurrentCorrelationId
        };
        _db.FinancialStatementSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync("FinancialStatementSnapshot", snapshot.Id.ToString("N"), "Capture", newValues: new { snapshot.PeriodCode, snapshot.SnapshotHash }, correlationId: snapshot.CorrelationId, cancellationToken: cancellationToken);
        }

        if (_telemetryService is not null)
        {
            await _telemetryService.TrackWorkflowMetricsAsync("financial-mapping", templateVersionId.ToString("N"), 0, "Captured", correlationId: snapshot.CorrelationId, cancellationToken: cancellationToken);
        }

        return new FinancialStatementSnapshotDto
        {
            Id = snapshot.Id,
            FinancialTemplateVersionId = snapshot.FinancialTemplateVersionId,
            PeriodCode = snapshot.PeriodCode,
            CapturedAtUtc = snapshot.CapturedAtUtc,
            SnapshotHash = snapshot.SnapshotHash
        };
    }

    public async Task<IReadOnlyList<FinancialStatementSnapshotDto>> GetSnapshotsAsync(Guid templateVersionId, CancellationToken cancellationToken = default)
    {
        return await _db.FinancialStatementSnapshots.AsNoTracking()
            .Where(x => x.FinancialTemplateVersionId == templateVersionId)
            .OrderByDescending(x => x.CapturedAtUtc)
            .Select(x => new FinancialStatementSnapshotDto
            {
                Id = x.Id,
                FinancialTemplateVersionId = x.FinancialTemplateVersionId,
                PeriodCode = x.PeriodCode,
                CapturedAtUtc = x.CapturedAtUtc,
                SnapshotHash = x.SnapshotHash
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ValidateMappingAsync(Guid templateVersionId, CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();
        var version = await _db.FinancialTemplateVersions
            .Include(x => x.Formulas)
            .FirstOrDefaultAsync(x => x.Id == templateVersionId, cancellationToken);
        if (version is null)
        {
            issues.Add("Template version not found.");
            return issues;
        }

        if (!version.Formulas.Any())
        {
            issues.Add("Template version has no formulas.");
        }

        var duplicates = version.Formulas
            .GroupBy(x => x.LineCode, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        foreach (var duplicate in duplicates)
        {
            issues.Add($"Duplicate formula line: {duplicate}");
        }

        return issues;
    }
}
