using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        => await StartSessionAsync(module, metadataJson: null, cancellationToken);

    public async Task<ReconciliationSessionDto> StartSessionAsync(string module, string? metadataJson, CancellationToken cancellationToken = default)
    {
        var session = new ReconciliationSession
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            Module = module,
            SessionCode = $"{module}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            MetadataJson = metadataJson
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
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var statementId = ReadBankStatementId(session);
        var statement = await _db.BankStatements
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == statementId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sao kê ngân hàng của phiên đối soát.");

        var receipts = await _db.ReceivableTransactions
            .AsNoTracking()
            .Where(x => x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId
                && x.SourceType == "PaymentReceipt"
                && x.CreditAmount > 0m
                && x.Status != "Cancelled"
                && x.Status != "Voided")
            .ToListAsync(cancellationToken);
        var alreadyMatchedReceiptIds = (await _db.ReconciliationMatches
            .Where(x => x.TargetType == "ReceivableTransaction" && x.Status == "Matched")
            .Select(x => x.TargetId)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matched = 0;

        foreach (var line in statement.Lines.Where(x => string.Equals(x.MatchStatus, "Unmatched", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.TransactionDate))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetStatementAmount(line, out var amount))
            {
                continue;
            }

            var candidates = receipts
                .Where(x => !alreadyMatchedReceiptIds.Contains(x.Id.ToString("N"))
                    && x.CreditAmount == amount
                    && Math.Abs((x.DocumentDate.Date - line.TransactionDate.Date).TotalDays) <= 3)
                .ToList();
            var referenceCandidate = candidates
                .Where(x => HasMatchingReference(line, x))
                .ToList();
            var candidate = referenceCandidate.Count == 1
                ? referenceCandidate[0]
                : referenceCandidate.Count == 0 && candidates.Count == 1 ? candidates[0] : null;
            if (candidate is null)
            {
                continue;
            }

            var targetId = candidate.Id.ToString("N");
            _db.ReconciliationMatches.Add(new ReconciliationMatch
            {
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId,
                SessionId = sessionId,
                SourceType = "BankStatementLine",
                SourceId = line.Id.ToString("N"),
                TargetType = "ReceivableTransaction",
                TargetId = targetId,
                MatchScore = HasMatchingReference(line, candidate) ? 1m : 0.8m,
                IsFuzzy = !HasMatchingReference(line, candidate),
                Status = "Matched",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    line.TransactionDate,
                    Amount = amount,
                    line.ReferenceNo,
                    candidate.DocumentNo
                })
            });
            line.MatchStatus = "Matched";
            alreadyMatchedReceiptIds.Add(targetId);
            matched++;
        }

        session.Status = "Completed";
        session.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
        {
            await _auditService.LogAsync("ReconciliationSession", session.Id.ToString("N"), "AutoMatch", newValues: new { matched, statement.Id }, cancellationToken: cancellationToken);
        }

        return matched;
    }

    public async Task<bool> MatchBankStatementLineAsync(
        Guid sessionId,
        Guid statementLineId,
        Guid receivableTransactionId,
        string? matchedBy = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var statementId = ReadBankStatementId(session);
        var line = await _db.BankStatementLines
            .FirstOrDefaultAsync(x => x.Id == statementLineId
                && x.BankStatementId == statementId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng sao kê thật thuộc phiên đối soát.");
        if (!string.Equals(line.MatchStatus, "Unmatched", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Dòng sao kê đã được đối chiếu trước đó.");
        }

        var receipt = await _db.ReceivableTransactions
            .FirstOrDefaultAsync(x => x.Id == receivableTransactionId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu thu thật để ghép.");
        if (receipt.SourceType != "PaymentReceipt" || receipt.CreditAmount <= 0m || receipt.Status is "Cancelled" or "Voided")
        {
            throw new InvalidOperationException("Khoản đích phải là phiếu thu phải thu đang còn hiệu lực.");
        }
        if (!TryGetStatementAmount(line, out var statementAmount) || statementAmount != receipt.CreditAmount)
        {
            throw new InvalidOperationException("Số tiền dòng sao kê và phiếu thu không khớp.");
        }

        var targetId = receipt.Id.ToString("N");
        var alreadyMatched = await _db.ReconciliationMatches.AnyAsync(x =>
            x.TargetType == "ReceivableTransaction"
            && x.TargetId == targetId
            && x.Status == "Matched", cancellationToken);
        if (alreadyMatched)
        {
            throw new InvalidOperationException("Phiếu thu đã được ghép với một dòng sao kê khác.");
        }

        _db.ReconciliationMatches.Add(new ReconciliationMatch
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            SessionId = sessionId,
            SourceType = "BankStatementLine",
            SourceId = line.Id.ToString("N"),
            TargetType = "ReceivableTransaction",
            TargetId = targetId,
            MatchScore = 1m,
            IsFuzzy = false,
            Status = "Matched",
            MetadataJson = JsonSerializer.Serialize(new
            {
                Method = "Manual",
                MatchedBy = string.IsNullOrWhiteSpace(matchedBy) ? "system" : matchedBy.Trim(),
                Amount = statementAmount,
                line.TransactionDate,
                line.ReferenceNo,
                receipt.DocumentNo
            })
        });
        line.MatchStatus = "Matched";
        session.Status = "Completed";
        session.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
        {
            await _auditService.LogAsync("ReconciliationSession", session.Id.ToString("N"), "ManualMatch", newValues: new
            {
                StatementLineId = line.Id,
                ReceivableTransactionId = receipt.Id,
                MatchedBy = matchedBy
            }, cancellationToken: cancellationToken);
        }

        return true;
    }

    public async Task<int> DetectAnomaliesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var statementId = ReadBankStatementId(session);
        var statement = await _db.BankStatements
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == statementId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sao kê ngân hàng của phiên đối soát.");

        var existingAnomalyIds = (await _db.ReconciliationAnomalies
            .Where(x => x.SessionId == sessionId && x.RelatedEntityType == "BankStatementLine" && x.RelatedEntityId != null)
            .Select(x => x.RelatedEntityId!)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var anomalies = 0;
        foreach (var line in statement.Lines.Where(x => !string.Equals(x.MatchStatus, "Matched", StringComparison.OrdinalIgnoreCase)))
        {
            var relatedId = line.Id.ToString("N");
            if (!existingAnomalyIds.Add(relatedId))
            {
                continue;
            }

            _db.ReconciliationAnomalies.Add(new ReconciliationAnomaly
            {
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId,
                SessionId = sessionId,
                Severity = "Warning",
                Category = "UnmatchedBankStatementLine",
                Message = $"Dòng sao kê ngày {line.TransactionDate:dd/MM/yyyy} chưa khớp giao dịch phải thu.",
                RelatedEntityType = "BankStatementLine",
                RelatedEntityId = relatedId,
                MetadataJson = JsonSerializer.Serialize(new { line.ReferenceNo, line.Description, line.DebitAmount, line.CreditAmount })
            });
            anomalies++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return anomalies;
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

    private async Task<ReconciliationSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => await _db.ReconciliationSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId
                && x.CompanyId == _companyContext.CompanyId
                && x.BranchId == _companyContext.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên đối soát trong phạm vi công ty/chi nhánh hiện tại.");

    private static Guid ReadBankStatementId(ReconciliationSession session)
    {
        if (string.IsNullOrWhiteSpace(session.MetadataJson))
        {
            throw new InvalidOperationException("Phiên đối soát chưa gắn BankStatementId thật; không thể chạy đối soát.");
        }

        try
        {
            using var document = JsonDocument.Parse(session.MetadataJson);
            var property = document.RootElement.EnumerateObject()
                .FirstOrDefault(x => string.Equals(x.Name, "bankStatementId", StringComparison.OrdinalIgnoreCase));
            if (property.Value.ValueKind == JsonValueKind.String && Guid.TryParse(property.Value.GetString(), out var id))
            {
                return id;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Metadata phiên đối soát không hợp lệ.", ex);
        }

        throw new InvalidOperationException("Phiên đối soát chưa có BankStatementId hợp lệ.");
    }

    private static bool TryGetStatementAmount(BankStatementLine line, out decimal amount)
    {
        var debit = Math.Abs(line.DebitAmount);
        var credit = Math.Abs(line.CreditAmount);
        if (debit > 0m && credit == 0m)
        {
            amount = debit;
            return true;
        }

        if (credit > 0m && debit == 0m)
        {
            amount = credit;
            return true;
        }

        amount = 0m;
        return false;
    }

    private static bool HasMatchingReference(BankStatementLine line, ReceivableTransaction receipt)
    {
        if (string.IsNullOrWhiteSpace(receipt.DocumentNo))
        {
            return false;
        }

        return string.Equals(line.ReferenceNo?.Trim(), receipt.DocumentNo.Trim(), StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(line.Description)
                && line.Description.Contains(receipt.DocumentNo, StringComparison.OrdinalIgnoreCase));
    }
}
