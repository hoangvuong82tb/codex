using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class TT200ComplianceService : ITT200ComplianceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IEnterpriseSearchService? _searchService;

    public TT200ComplianceService(
        AppDbContext dbContext,
        IAuditService? auditService = null,
        IAuditCorrelationService? correlationService = null,
        IRuntimeTelemetryService? telemetryService = null,
        ICurrentUserService? currentUserService = null,
        IEnterpriseSearchService? searchService = null)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _correlationService = correlationService;
        _telemetryService = telemetryService;
        _currentUserService = currentUserService;
        _searchService = searchService;
    }

    public async Task<IReadOnlyList<TT200AccountMappingDto>> GetAccountMappingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultFoundationAsync(cancellationToken);
        return await _dbContext.TT200AccountMappings.AsNoTracking()
            .OrderBy(x => x.AccountCode)
            .Select(x => new TT200AccountMappingDto
            {
                Id = x.Id,
                AccountCode = x.AccountCode,
                TT200Category = x.TT200Category,
                TT200ReportSection = x.TT200ReportSection,
                TT200ClosingGroup = x.TT200ClosingGroup,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TT200ComplianceIssueDto>> ValidateComplianceAsync(string? periodCode = null, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var now = DateTime.UtcNow;
        var issues = new List<TT200ComplianceIssue>();

        var accountCodes = await _dbContext.LedgerAccounts.AsNoTracking().Select(x => x.AccountCode).ToListAsync(cancellationToken);
        var mappings = await _dbContext.TT200AccountMappings.AsNoTracking().ToListAsync(cancellationToken);
        var mappedCodes = mappings.Select(x => x.AccountCode).ToArray();

        foreach (var code in mappings.GroupBy(x => x.AccountCode).Where(x => x.Count() > 1).Select(x => x.Key))
        {
            issues.Add(NewIssue("TT200_DUPLICATE_MAPPING", "Error", "Accounting", $"Tài khoản {code} bị mapping TT200 trùng.", code));
        }

        foreach (var code in accountCodes.Except(mappedCodes, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(NewIssue("TT200_MISSING_MAPPING", "Warning", "Accounting", $"Thiếu mapping TT200 cho tài khoản {code}.", code));
        }

        foreach (var mapping in mappings.Where(x => !accountCodes.Contains(x.AccountCode, StringComparer.OrdinalIgnoreCase)))
        {
            issues.Add(NewIssue("TT200_ORPHAN_MAPPING", "Error", "Accounting", $"Mapping TT200 tham chiếu tài khoản không tồn tại: {mapping.AccountCode}.", mapping.AccountCode));
        }

        foreach (var code in await _dbContext.LedgerAccounts.AsNoTracking()
                     .Where(x => mappedCodes.Contains(x.AccountCode) && !x.IsActive)
                     .Select(x => x.AccountCode)
                     .ToListAsync(cancellationToken))
        {
            issues.Add(NewIssue("TT200_INACTIVE_ACCOUNT", "Warning", "Accounting", $"Tài khoản mapped TT200 đang inactive: {code}.", code));
        }

        var closingEntries = await _dbContext.ClosingEntries.AsNoTracking().Include(x => x.Lines).ToListAsync(cancellationToken);
        foreach (var closing in closingEntries.Where(x => x.DebitTotal() != x.CreditTotal()))
        {
            issues.Add(NewIssue("TT200_CLOSING_MISMATCH", "Critical", "Closing", $"Bút toán khóa sổ TT200 lệch Nợ/Có kỳ {closing.Period}.", closing.Period));
        }

        if (!string.IsNullOrWhiteSpace(periodCode))
        {
            var normalized = periodCode.Trim();
            var overlapCount = await _dbContext.TT200DeclarationPeriods.AsNoTracking().CountAsync(x => x.PeriodCode == normalized, cancellationToken);
            if (overlapCount > 1)
            {
                issues.Add(NewIssue("TT200_DECLARATION_OVERLAP", "Error", "Reporting", $"Kỳ TT200 {normalized} bị trùng.", normalized));
            }
        }

        TT200ComplianceIssue NewIssue(string ruleCode, string severity, string module, string message, string? affectedEntity)
            => new()
            {
                RuleCode = ruleCode,
                Severity = severity,
                Module = module,
                Message = message,
                AffectedEntity = affectedEntity,
                CorrelationId = _correlationService?.CurrentCorrelationId,
                DetectedAtUtc = now
            };

        _dbContext.TT200ComplianceIssues.RemoveRange(await _dbContext.TT200ComplianceIssues.ToListAsync(cancellationToken));
        _dbContext.TT200ComplianceIssues.AddRange(issues);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var issue in issues)
        {
            if (_searchService is not null)
            {
                await _searchService.UpsertAsync(new SearchIndexUpsertRequest
                {
                    EntityType = nameof(TT200ComplianceIssue),
                    EntityId = issue.Id.ToString(),
                    Title = $"{issue.RuleCode} - {issue.Severity}",
                    Subtitle = issue.Module,
                    Category = "TT200Compliance",
                    SearchText = $"{issue.RuleCode} {issue.Severity} {issue.Module} {issue.Message}"
                }, cancellationToken);
            }
        }

        if (_auditService is not null)
        {
            await _auditService.LogAsync(nameof(TT200ComplianceIssue), "validation", "Validate", oldValues: null, newValues: new { issueCount = issues.Count, periodCode, correlationId = _correlationService?.CurrentCorrelationId }, correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }

        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("tt200", "validation", sw.ElapsedMilliseconds, "Information", "TT200 validation completed.", _correlationService?.CurrentCorrelationId, null, new { issueCount = issues.Count }, cancellationToken);
        }

        return issues.Select(x => new TT200ComplianceIssueDto
        {
            Id = x.Id,
            RuleCode = x.RuleCode,
            Severity = x.Severity,
            Module = x.Module,
            Message = x.Message,
            AffectedEntity = x.AffectedEntity,
            DetectedAtUtc = x.DetectedAtUtc
        }).ToArray();
    }

    public async Task<TT200ComplianceSnapshotDto> CreateSnapshotAsync(string periodCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(periodCode))
        {
            throw new ArgumentException("Kỳ snapshot là bắt buộc.", nameof(periodCode));
        }

        var normalized = periodCode.Trim();
        var issues = await ValidateComplianceAsync(normalized, cancellationToken);
        var score = Math.Max(0m, 100m - issues.Count(x => x.Severity is "Critical" or "Error") * 10m - issues.Count(x => x.Severity == "Warning") * 3m);

        var accountBalances = await _dbContext.JournalLines.AsNoTracking().ToListAsync(cancellationToken);
        var vatTransactions = await _dbContext.VatTransactions.AsNoTracking().Where(x => x.DeclarationPeriod == normalized).ToListAsync(cancellationToken);
        var closingEntries = await _dbContext.ClosingEntries.AsNoTracking().Include(x => x.Lines).Where(x => x.Period == normalized).ToListAsync(cancellationToken);

        var snapshotData = new
        {
            periodCode = normalized,
            accountBalances = accountBalances.GroupBy(x => x.AccountCode).Select(g => new { g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) }).ToList(),
            vatSummary = vatTransactions.GroupBy(x => x.Direction).Select(g => new { Direction = g.Key, Amount = g.Sum(x => x.TaxAmount) }).ToList(),
            closingSummary = closingEntries.Select(x => new { x.Id, x.Period, x.Status, Debit = x.DebitTotal(), Credit = x.CreditTotal() }).ToList(),
            complianceIssues = issues,
            declarationStatus = await _dbContext.TT200DeclarationPeriods.AsNoTracking().Where(x => x.PeriodCode == normalized).Select(x => x.Status).FirstOrDefaultAsync(cancellationToken) ?? "Draft",
            closingState = await _dbContext.ClosingEntries.AsNoTracking().Where(x => x.Period == normalized).Select(x => x.Status).FirstOrDefaultAsync(cancellationToken) ?? "Draft"
        };

        var snapshotJson = JsonSerializer.Serialize(snapshotData, JsonOptions);
        var snapshot = new TT200ComplianceSnapshot
        {
            PeriodCode = normalized,
            ComplianceScore = score,
            DeclarationStatus = snapshotData.declarationStatus,
            SnapshotJson = snapshotJson,
            SnapshotHash = Hash(snapshotJson),
            CorrelationId = _correlationService?.CurrentCorrelationId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.TT200ComplianceSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync(nameof(TT200ComplianceSnapshot), snapshot.Id.ToString(), "CreateSnapshot", oldValues: null, newValues: new { snapshot.PeriodCode, snapshot.ComplianceScore }, correlationId: snapshot.CorrelationId, cancellationToken: cancellationToken);
        }

        return new TT200ComplianceSnapshotDto
        {
            Id = snapshot.Id,
            PeriodCode = snapshot.PeriodCode,
            ComplianceScore = snapshot.ComplianceScore,
            DeclarationStatus = snapshot.DeclarationStatus,
            CreatedAtUtc = snapshot.CreatedAtUtc,
            SnapshotHash = snapshot.SnapshotHash
        };
    }

    public async Task<IReadOnlyList<TT200ComplianceSnapshotDto>> GetSnapshotsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TT200ComplianceSnapshots.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new TT200ComplianceSnapshotDto
            {
                Id = x.Id,
                PeriodCode = x.PeriodCode,
                ComplianceScore = x.ComplianceScore,
                DeclarationStatus = x.DeclarationStatus,
                CreatedAtUtc = x.CreatedAtUtc,
                SnapshotHash = x.SnapshotHash
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TT200ExportManifestDto> CreateExportManifestAsync(string exportType, string periodCode, int rowCount, string payloadHash, CancellationToken cancellationToken = default)
    {
        var manifest = new TT200ExportManifest
        {
            ExportType = string.IsNullOrWhiteSpace(exportType) ? "UNKNOWN" : exportType.Trim().ToUpperInvariant(),
            PeriodCode = periodCode.Trim(),
            ExportedAtUtc = DateTime.UtcNow,
            ExportedBy = _currentUserService?.CurrentUser?.UserName,
            Hash = payloadHash,
            RowCount = rowCount,
            ComplianceVersion = "TT200.v1",
            CorrelationId = _correlationService?.CurrentCorrelationId,
            MetadataJson = JsonSerializer.Serialize(new { company = _dbContext.CurrentCompanyId, branch = _dbContext.CurrentBranchId }, JsonOptions)
        };

        _dbContext.TT200ExportManifests.Add(manifest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync(nameof(TT200ExportManifest), manifest.Id.ToString(), "CreateManifest", oldValues: null, newValues: new { manifest.ExportType, manifest.PeriodCode, manifest.RowCount }, correlationId: manifest.CorrelationId, cancellationToken: cancellationToken);
        }

        return new TT200ExportManifestDto
        {
            Id = manifest.Id,
            ExportType = manifest.ExportType,
            PeriodCode = manifest.PeriodCode,
            ExportedAtUtc = manifest.ExportedAtUtc,
            Hash = manifest.Hash,
            RowCount = manifest.RowCount,
            ComplianceVersion = manifest.ComplianceVersion
        };
    }

    public async Task<IReadOnlyList<TT200ExportManifestDto>> GetExportManifestsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TT200ExportManifests.AsNoTracking()
            .OrderByDescending(x => x.ExportedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new TT200ExportManifestDto
            {
                Id = x.Id,
                ExportType = x.ExportType,
                PeriodCode = x.PeriodCode,
                ExportedAtUtc = x.ExportedAtUtc,
                Hash = x.Hash,
                RowCount = x.RowCount,
                ComplianceVersion = x.ComplianceVersion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TT200ValidationSummaryDto> GetValidationSummaryAsync(string? periodCode = null, CancellationToken cancellationToken = default)
    {
        var issues = await ValidateComplianceAsync(periodCode, cancellationToken);
        return new TT200ValidationSummaryDto
        {
            ComplianceScore = Math.Max(0m, 100m - issues.Count(x => x.Severity is "Critical" or "Error") * 10m - issues.Count(x => x.Severity == "Warning") * 3m),
            TotalIssues = issues.Count,
            CriticalIssues = issues.Count(x => x.Severity == "Critical"),
            ErrorIssues = issues.Count(x => x.Severity == "Error"),
            WarningIssues = issues.Count(x => x.Severity == "Warning"),
            InfoIssues = issues.Count(x => x.Severity == "Info")
        };
    }

    private async Task EnsureDefaultFoundationAsync(CancellationToken cancellationToken)
    {
        _dbContext.ChangeTracker.Clear();

        var hasChanges = false;
        var now = DateTime.UtcNow;

        var existingRuleCodeSet = (await _dbContext.TT200ValidationRules
                .AsNoTracking()
                .Select(x => x.RuleCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var defaultRules = new[]
        {
            NewRule("TT200_DUPLICATE_MAPPING", "Warning", "Accounting"),
            NewRule("TT200_MISSING_MAPPING", "Warning", "Accounting"),
            NewRule("TT200_ORPHAN_MAPPING", "Error", "Accounting"),
            NewRule("TT200_CLOSING_MISMATCH", "Critical", "Closing")
        };

        foreach (var rule in defaultRules.Where(x => !existingRuleCodeSet.Contains(x.RuleCode)))
        {
            _dbContext.TT200ValidationRules.Add(rule);
            hasChanges = true;
        }

        var existingReportIdSet = (await _dbContext.TT200ReportTemplates
                .AsNoTracking()
                .Select(x => x.ReportId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var defaultTemplates = new[]
        {
            NewTemplate("TT200_BALANCE_SHEET", "Bang can doi ke toan TT200"),
            NewTemplate("TT200_INCOME_STATEMENT", "Bao cao ket qua kinh doanh TT200"),
            NewTemplate("TT200_CASH_FLOW", "Bao cao luu chuyen tien te TT200"),
            NewTemplate("TT200_CLOSING_SUMMARY", "Tong hop khoa so TT200"),
            NewTemplate("TT200_ACCOUNT_MAPPING", "Doi chieu mapping tai khoan TT200"),
            NewTemplate("TT200_COMPLIANCE_ISSUES", "Danh sach loi tuan thu TT200"),
            NewTemplate("TT200_DECLARATION_SUMMARY", "Tong hop ky ke khai TT200")
        };

        foreach (var template in defaultTemplates.Where(x => !existingReportIdSet.Contains(x.ReportId)))
        {
            _dbContext.TT200ReportTemplates.Add(template);
            hasChanges = true;
        }

        var accounts = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Select(x => new { x.AccountCode, x.AccountCategory })
            .ToListAsync(cancellationToken);
        var existingMappedCodeSet = (await _dbContext.TT200AccountMappings
                .AsNoTracking()
                .Select(x => x.AccountCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var acc in accounts.Where(x => !existingMappedCodeSet.Contains(x.AccountCode)))
        {
            _dbContext.TT200AccountMappings.Add(new TT200AccountMapping
            {
                AccountCode = acc.AccountCode,
                TT200Category = string.IsNullOrWhiteSpace(acc.AccountCategory) ? ResolveCategory(acc.AccountCode) : acc.AccountCategory,
                TT200ReportSection = ResolveReportSection(acc.AccountCode),
                TT200ClosingGroup = ResolveClosingGroup(acc.AccountCode),
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            hasChanges = true;
        }

        if (accounts.Count == 0)
        {
            var fallbackMappings = new[]
            {
                new TT200AccountMapping { AccountCode = "111", TT200Category = "Asset", TT200ReportSection = "ASSET", TT200ClosingGroup = "CASH", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT200AccountMapping { AccountCode = "1311", TT200Category = "Asset", TT200ReportSection = "ASSET", TT200ClosingGroup = "RECEIVABLE", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT200AccountMapping { AccountCode = "3311", TT200Category = "Liability", TT200ReportSection = "LIABILITY", TT200ClosingGroup = "PAYABLE", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT200AccountMapping { AccountCode = "421", TT200Category = "Equity", TT200ReportSection = "EQUITY", TT200ClosingGroup = "RETAINED_EARNINGS", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT200AccountMapping { AccountCode = "511", TT200Category = "Revenue", TT200ReportSection = "INCOME", TT200ClosingGroup = "REVENUE", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT200AccountMapping { AccountCode = "632", TT200Category = "Expense", TT200ReportSection = "EXPENSE", TT200ClosingGroup = "COGS", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now }
            };

            foreach (var mapping in fallbackMappings.Where(x => !existingMappedCodeSet.Contains(x.AccountCode)))
            {
                _dbContext.TT200AccountMappings.Add(mapping);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
    private TT200ValidationRule NewRule(string code, string severity, string module) => new()
    {
        RuleCode = code,
        RuleName = code,
        Severity = severity,
        Module = module,
        IsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private TT200ReportTemplate NewTemplate(string reportId, string name) => new()
    {
        ReportId = reportId,
        Name = name,
        Description = name,
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static string ResolveCategory(string accountCode)
    {
        if (accountCode.StartsWith("1", StringComparison.Ordinal)) return "Asset";
        if (accountCode.StartsWith("2", StringComparison.Ordinal)) return "Liability";
        if (accountCode.StartsWith("3", StringComparison.Ordinal)) return "Equity";
        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "Revenue";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "Expense";
        return "Other";
    }

    private static string ResolveReportSection(string accountCode)
    {
        if (accountCode.StartsWith("1", StringComparison.Ordinal)) return "ASSET";
        if (accountCode.StartsWith("2", StringComparison.Ordinal)) return "LIABILITY";
        if (accountCode.StartsWith("3", StringComparison.Ordinal)) return "EQUITY";
        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "INCOME";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "EXPENSE";
        return "OTHER";
    }

    private static string ResolveClosingGroup(string accountCode)
    {
        if (accountCode.StartsWith("111", StringComparison.Ordinal) || accountCode.StartsWith("112", StringComparison.Ordinal)) return "CASH";
        if (accountCode.StartsWith("131", StringComparison.Ordinal)) return "RECEIVABLE";
        if (accountCode.StartsWith("331", StringComparison.Ordinal)) return "PAYABLE";
        if (accountCode.StartsWith("421", StringComparison.Ordinal)) return "RETAINED_EARNINGS";
        if (accountCode.StartsWith("511", StringComparison.Ordinal)) return "REVENUE";
        if (accountCode.StartsWith("632", StringComparison.Ordinal)) return "COGS";
        return "GENERAL";
    }

    private static string Hash(string payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}

internal static class TT200ClosingEntryExtensions
{
    public static decimal DebitTotal(this ClosingEntry closingEntry)
        => closingEntry.Lines.Sum(x => x.DebitAmount);

    public static decimal CreditTotal(this ClosingEntry closingEntry)
        => closingEntry.Lines.Sum(x => x.CreditAmount);
}

