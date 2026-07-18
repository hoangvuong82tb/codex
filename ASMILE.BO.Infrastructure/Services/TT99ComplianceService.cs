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

public sealed class TT99ComplianceService : ITT99ComplianceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IEnterpriseSearchService? _searchService;

    public TT99ComplianceService(
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

    public async Task<IReadOnlyList<TT99AccountMappingDto>> GetAccountMappingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultFoundationAsync(cancellationToken);
        return await _dbContext.TT99AccountMappings.AsNoTracking()
            .OrderBy(x => x.AccountCode)
            .Select(x => new TT99AccountMappingDto
            {
                Id = x.Id,
                AccountCode = x.AccountCode,
                TT99Category = x.TT99Category,
                TT99ReportSection = x.TT99ReportSection,
                TT99DeclarationGroup = x.TT99DeclarationGroup,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TT99ComplianceIssueDto>> ValidateComplianceAsync(string? periodCode = null, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var now = DateTime.UtcNow;
        var issues = new List<TT99ComplianceIssue>();

        var accountCodes = await _dbContext.LedgerAccounts.AsNoTracking().Select(x => x.AccountCode).ToListAsync(cancellationToken);
        var mappings = await _dbContext.TT99AccountMappings.AsNoTracking().ToListAsync(cancellationToken);

        var duplicateMappings = mappings.GroupBy(x => x.AccountCode).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var code in duplicateMappings)
        {
            issues.Add(NewIssue("TT99_DUPLICATE_MAPPING", "Error", "Accounting", $"Tài khoản {code} bị mapping trùng.", code));
        }

        var missingAccountMappings = accountCodes.Except(mappings.Select(x => x.AccountCode), StringComparer.OrdinalIgnoreCase);
        foreach (var code in missingAccountMappings)
        {
            issues.Add(NewIssue("TT99_MISSING_MAPPING", "Warning", "Accounting", $"Thiếu mapping TT99 cho tài khoản {code}.", code));
        }

        var orphanMappings = mappings.Where(x => !accountCodes.Contains(x.AccountCode, StringComparer.OrdinalIgnoreCase));
        foreach (var orphan in orphanMappings)
        {
            issues.Add(NewIssue("TT99_ORPHAN_MAPPING", "Error", "Accounting", $"Mapping TT99 tham chiếu tài khoản không tồn tại: {orphan.AccountCode}.", orphan.AccountCode));
        }

        var mappedCodes = mappings.Select(m => m.AccountCode).ToArray();
        var inactiveMapped = await _dbContext.LedgerAccounts.AsNoTracking()
            .Where(x => mappedCodes.Contains(x.AccountCode) && !x.IsActive)
            .Select(x => x.AccountCode)
            .ToListAsync(cancellationToken);
        foreach (var code in inactiveMapped)
        {
            issues.Add(NewIssue("TT99_INACTIVE_ACCOUNT", "Warning", "Accounting", $"Tài khoản mapped nhưng đang inactive: {code}.", code));
        }

        var imbalance = await _dbContext.JournalHeaders.AsNoTracking()
            .Where(x => x.Status == JournalStatus.Posted)
            .AnyAsync(x => x.TotalDebit > x.TotalCredit + 0.005m || x.TotalCredit > x.TotalDebit + 0.005m, cancellationToken);
        if (imbalance)
        {
            issues.Add(NewIssue("TT99_JOURNAL_IMBALANCE", "Critical", "Journal", "Phát hiện bút toán lệch Nợ/Có.", null));
        }

        if (!string.IsNullOrWhiteSpace(periodCode))
        {
            var normalized = periodCode.Trim();
            var overlappingPeriods = await _dbContext.TT99DeclarationPeriods.AsNoTracking()
                .Where(x => x.PeriodCode == normalized)
                .CountAsync(cancellationToken);
            if (overlappingPeriods > 1)
            {
                issues.Add(NewIssue("TT99_DECLARATION_OVERLAP", "Error", "Tax", $"Kỳ kê khai {normalized} bị trùng.", normalized));
            }
        }

        var previous = await _dbContext.TT99ComplianceIssues.ToListAsync(cancellationToken);
        _dbContext.TT99ComplianceIssues.RemoveRange(previous);
        _dbContext.TT99ComplianceIssues.AddRange(issues);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var issue in issues)
        {
            if (_searchService is not null)
            {
                await _searchService.UpsertAsync(new SearchIndexUpsertRequest
                {
                    EntityType = nameof(TT99ComplianceIssue),
                    EntityId = issue.Id.ToString(),
                    Title = $"{issue.RuleCode} - {issue.Severity}",
                    Subtitle = issue.Module,
                    Category = "TT99Compliance",
                    SearchText = $"{issue.RuleCode} {issue.Severity} {issue.Module} {issue.Message}"
                }, cancellationToken);
            }
        }

        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                nameof(TT99ComplianceIssue),
                "validation",
                "Validate",
                oldValues: null,
                newValues: new
                {
                    issueCount = issues.Count,
                    periodCode,
                    correlationId = _correlationService?.CurrentCorrelationId
                },
                correlationId: _correlationService?.CurrentCorrelationId,
                cancellationToken: cancellationToken);
        }

        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("tt99", "validation", sw.ElapsedMilliseconds, "Information", "TT99 validation completed.", _correlationService?.CurrentCorrelationId, null, new { issueCount = issues.Count }, cancellationToken);
        }

        return issues
            .OrderByDescending(x => x.DetectedAtUtc)
            .Select(x => new TT99ComplianceIssueDto
            {
                Id = x.Id,
                RuleCode = x.RuleCode,
                Severity = x.Severity,
                Module = x.Module,
                Message = x.Message,
                AffectedEntity = x.AffectedEntity,
                DetectedAtUtc = x.DetectedAtUtc
            })
            .ToArray();

        TT99ComplianceIssue NewIssue(string ruleCode, string severity, string module, string message, string? affectedEntity)
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
    }

    public async Task<TT99ComplianceSnapshotDto> CreateSnapshotAsync(string periodCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(periodCode))
        {
            throw new ArgumentException("Kỳ snapshot là bắt buộc.", nameof(periodCode));
        }

        var normalized = periodCode.Trim();
        var issues = await ValidateComplianceAsync(normalized, cancellationToken);
        var score = Math.Max(0m, 100m - issues.Count(x => x.Severity is "Critical" or "Error") * 10m - issues.Count(x => x.Severity == "Warning") * 3m);

        var inventoryRows = await _dbContext.StockBalances.AsNoTracking().ToListAsync(cancellationToken);
        var journalLines = await _dbContext.JournalLines.AsNoTracking().ToListAsync(cancellationToken);
        var vatTransactions = await _dbContext.VatTransactions.AsNoTracking().Where(x => x.DeclarationPeriod == normalized).ToListAsync(cancellationToken);
        var closingEntries = await _dbContext.ClosingEntries.AsNoTracking().Where(x => x.Period == normalized).ToListAsync(cancellationToken);
        var snapshotData = new
        {
            periodCode = normalized,
            accountBalances = journalLines
                .GroupBy(x => x.AccountCode)
                .Select(g => new { g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
                .ToList(),
            vatSummary = vatTransactions
                .GroupBy(x => x.Direction)
                .Select(g => new { Direction = g.Key, Amount = g.Sum(x => x.TaxAmount) })
                .ToList(),
            inventorySummary = new
            {
                Qty = inventoryRows.Sum(x => x.ClosingQty),
                Value = inventoryRows.Sum(x => x.ClosingAmount)
            },
            closingSummary = closingEntries.Select(x => new { x.Id, x.Status, x.Period, x.ClosingDate }).ToList(),
            complianceIssues = issues,
            declarationStatus = await _dbContext.TT99DeclarationPeriods.AsNoTracking()
                .Where(x => x.PeriodCode == normalized)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(cancellationToken) ?? "Draft",
            retainedEarnings = await _dbContext.LedgerAccounts.AsNoTracking()
                .Where(x => x.AccountCode.StartsWith("421"))
                .Select(x => new { x.AccountCode, x.NormalBalance })
                .ToListAsync(cancellationToken)
        };

        var snapshotJson = JsonSerializer.Serialize(snapshotData, JsonOptions);
        var snapshot = new TT99ComplianceSnapshot
        {
            PeriodCode = normalized,
            ComplianceScore = score,
            DeclarationStatus = snapshotData.declarationStatus,
            SnapshotJson = snapshotJson,
            SnapshotHash = Hash(snapshotJson),
            CorrelationId = _correlationService?.CurrentCorrelationId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.TT99ComplianceSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                nameof(TT99ComplianceSnapshot),
                snapshot.Id.ToString(),
                "CreateSnapshot",
                oldValues: null,
                newValues: new { snapshot.PeriodCode, snapshot.ComplianceScore },
                correlationId: snapshot.CorrelationId,
                cancellationToken: cancellationToken);
        }

        return new TT99ComplianceSnapshotDto
        {
            Id = snapshot.Id,
            PeriodCode = snapshot.PeriodCode,
            ComplianceScore = snapshot.ComplianceScore,
            DeclarationStatus = snapshot.DeclarationStatus,
            CreatedAtUtc = snapshot.CreatedAtUtc,
            SnapshotHash = snapshot.SnapshotHash
        };
    }

    public async Task<IReadOnlyList<TT99ComplianceSnapshotDto>> GetSnapshotsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TT99ComplianceSnapshots.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new TT99ComplianceSnapshotDto
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

    public async Task<TT99ExportManifestDto> CreateExportManifestAsync(string exportType, string periodCode, int rowCount, string payloadHash, CancellationToken cancellationToken = default)
    {
        var manifest = new TT99ExportManifest
        {
            ExportType = string.IsNullOrWhiteSpace(exportType) ? "UNKNOWN" : exportType.Trim().ToUpperInvariant(),
            PeriodCode = periodCode.Trim(),
            ExportedAtUtc = DateTime.UtcNow,
            ExportedBy = _currentUserService?.CurrentUser?.UserName,
            Hash = payloadHash,
            RowCount = rowCount,
            ComplianceVersion = "TT99.v1",
            CorrelationId = _correlationService?.CurrentCorrelationId,
            MetadataJson = JsonSerializer.Serialize(new { company = _dbContext.CurrentCompanyId, branch = _dbContext.CurrentBranchId }, JsonOptions)
        };

        _dbContext.TT99ExportManifests.Add(manifest);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                nameof(TT99ExportManifest),
                manifest.Id.ToString(),
                "CreateManifest",
                oldValues: null,
                newValues: new { manifest.ExportType, manifest.PeriodCode, manifest.RowCount },
                correlationId: manifest.CorrelationId,
                cancellationToken: cancellationToken);
        }

        return new TT99ExportManifestDto
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

    public async Task<IReadOnlyList<TT99ExportManifestDto>> GetExportManifestsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TT99ExportManifests.AsNoTracking()
            .OrderByDescending(x => x.ExportedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new TT99ExportManifestDto
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

    public async Task<TT99ValidationSummaryDto> GetValidationSummaryAsync(string? periodCode = null, CancellationToken cancellationToken = default)
    {
        var issues = await ValidateComplianceAsync(periodCode, cancellationToken);
        return new TT99ValidationSummaryDto
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

        var existingRuleCodeSet = (await _dbContext.TT99ValidationRules
                .AsNoTracking()
                .Select(x => x.RuleCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var defaultRules = new[]
        {
            NewRule("TT99_DUPLICATE_MAPPING", "Warning", "Accounting"),
            NewRule("TT99_MISSING_MAPPING", "Warning", "Accounting"),
            NewRule("TT99_ORPHAN_MAPPING", "Error", "Accounting"),
            NewRule("TT99_JOURNAL_IMBALANCE", "Critical", "Journal")
        };

        foreach (var rule in defaultRules.Where(x => !existingRuleCodeSet.Contains(x.RuleCode)))
        {
            _dbContext.TT99ValidationRules.Add(rule);
            hasChanges = true;
        }

        var existingReportIdSet = (await _dbContext.TT99ReportTemplates
                .AsNoTracking()
                .Select(x => x.ReportId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var defaultTemplates = new[]
        {
            NewTemplate("TT99_BALANCE_SHEET", "Bang can doi ke toan TT99"),
            NewTemplate("TT99_INCOME_STATEMENT", "Bao cao ket qua kinh doanh TT99"),
            NewTemplate("TT99_CASH_FLOW", "Bao cao luu chuyen tien te TT99"),
            NewTemplate("TT99_ACCOUNT_MAPPING", "Doi chieu mapping tai khoan TT99"),
            NewTemplate("TT99_COMPLIANCE_ISSUES", "Danh sach loi tuan thu TT99"),
            NewTemplate("TT99_DECLARATION_SUMMARY", "Tong hop ky ke khai TT99")
        };

        foreach (var template in defaultTemplates.Where(x => !existingReportIdSet.Contains(x.ReportId)))
        {
            _dbContext.TT99ReportTemplates.Add(template);
            hasChanges = true;
        }

        var accounts = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .Select(x => new { x.AccountCode, x.AccountCategory })
            .ToListAsync(cancellationToken);
        var existingMappedCodeSet = (await _dbContext.TT99AccountMappings
                .AsNoTracking()
                .Select(x => x.AccountCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var acc in accounts.Where(x => !existingMappedCodeSet.Contains(x.AccountCode)))
        {
            _dbContext.TT99AccountMappings.Add(new TT99AccountMapping
            {
                AccountCode = acc.AccountCode,
                TT99Category = string.IsNullOrWhiteSpace(acc.AccountCategory) ? ResolveCategory(acc.AccountCode) : acc.AccountCategory,
                TT99ReportSection = ResolveReportSection(acc.AccountCode),
                TT99DeclarationGroup = ResolveDeclarationGroup(acc.AccountCode),
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
                new TT99AccountMapping { AccountCode = "1111", TT99Category = "Asset", TT99ReportSection = "ASSET", TT99DeclarationGroup = "GENERAL", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now },
                new TT99AccountMapping { AccountCode = "3331", TT99Category = "Liability", TT99ReportSection = "LIABILITY", TT99DeclarationGroup = "VAT_OUTPUT", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now }
            };

            foreach (var mapping in fallbackMappings.Where(x => !existingMappedCodeSet.Contains(x.AccountCode)))
            {
                _dbContext.TT99AccountMappings.Add(mapping);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        static TT99ValidationRule NewRule(string code, string severity, string module) => new()
        {
            RuleCode = code,
            RuleName = code,
            Severity = severity,
            Module = module,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        static TT99ReportTemplate NewTemplate(string reportId, string name) => new()
        {
            ReportId = reportId,
            Name = name,
            Description = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
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

    private static string ResolveCategory(string accountCode)
    {
        if (accountCode.StartsWith("1", StringComparison.Ordinal)) return "Asset";
        if (accountCode.StartsWith("2", StringComparison.Ordinal)) return "Liability";
        if (accountCode.StartsWith("3", StringComparison.Ordinal) || accountCode.StartsWith("4", StringComparison.Ordinal)) return "Equity";
        if (accountCode.StartsWith("5", StringComparison.Ordinal) || accountCode.StartsWith("7", StringComparison.Ordinal)) return "Revenue";
        if (accountCode.StartsWith("6", StringComparison.Ordinal) || accountCode.StartsWith("8", StringComparison.Ordinal)) return "Expense";
        return "Other";
    }

    private static string ResolveDeclarationGroup(string accountCode)
    {
        if (accountCode.StartsWith("133", StringComparison.Ordinal)) return "VAT_INPUT";
        if (accountCode.StartsWith("333", StringComparison.Ordinal)) return "VAT_OUTPUT";
        return "GENERAL";
    }

    private static string Hash(string payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}

