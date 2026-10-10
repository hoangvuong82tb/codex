using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class TaxService : ITaxService
{
    private const string VatInputRole = "VATInput";
    private const string VatOutputRole = "VATOutput";
    private const string DirectionInput = "Input";
    private const string DirectionOutput = "Output";
    private const string StatusDraft = "Draft";
    private const string StatusPosted = "Posted";
    private const string StatusDeclared = "Declared";
    private const string StatusCancelled = "Cancelled";

    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService? _auditLogService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;

    public TaxService(AppDbContext dbContext, IAuditLogService? auditLogService = null, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
    }

    public async Task<IReadOnlyList<TaxCodeDto>> GetTaxCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxCodes
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => MapTaxCode(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxCodeDto> CreateTaxCodeAsync(CreateTaxCodeRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var code = NormalizeRequired(request.Code, "Mã thuế là bắt buộc.").ToUpperInvariant();
        var name = NormalizeRequired(request.Name, "Tên mã thuế là bắt buộc.");
        var taxType = NormalizeRequired(request.TaxType, "Loại thuế là bắt buộc.");
        if (request.TaxRate < 0)
        {
            throw new InvalidOperationException("Thuế suất không được âm.");
        }

        var duplicated = await _dbContext.TaxCodes.AnyAsync(x => x.Code == code, cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã thuế đã tồn tại: {code}.");
        }

        var now = DateTime.UtcNow;
        var entity = new TaxCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            TaxRate = request.TaxRate,
            TaxType = taxType,
            InputAccountCode = NullIfWhiteSpace(request.InputAccountCode),
            OutputAccountCode = NullIfWhiteSpace(request.OutputAccountCode),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.TaxCodes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("TaxCode.Create", $"Tạo mã thuế {code}.", cancellationToken);
        await AuditAsync("TaxCode", entity.Id.ToString(), "Create", null, entity, null, cancellationToken);
        return MapTaxCode(entity);
    }

    public async Task<VatTransactionDto> CreateVatTransactionAsync(CreateVatTransactionRequest request, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var taxCode = await _dbContext.TaxCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.TaxCodeId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mã thuế đang hoạt động.");

        var direction = NormalizeDirection(request.Direction);
        if (request.TaxAmount <= 0)
        {
            throw new InvalidOperationException("Số tiền thuế phải lớn hơn 0.");
        }

        var now = DateTime.UtcNow;
        var invoiceDate = request.InvoiceDate;
        var declarationPeriod = NormalizePeriod(request.DeclarationPeriod, invoiceDate ?? now);
        var entity = new VatTransaction
        {
            Id = Guid.NewGuid(),
            TaxCodeId = taxCode.Id,
            SourceType = NormalizeRequired(request.SourceType, "Nguồn chứng từ là bắt buộc."),
            SourceId = request.SourceId,
            JournalHeaderId = request.JournalHeaderId,
            JournalLineId = request.JournalLineId,
            InvoiceNo = NullIfWhiteSpace(request.InvoiceNo),
            InvoiceDate = invoiceDate,
            PartnerType = NullIfWhiteSpace(request.PartnerType),
            PartnerId = request.PartnerId,
            TaxBaseAmount = request.TaxBaseAmount,
            TaxRate = taxCode.TaxRate,
            TaxAmount = request.TaxAmount,
            Direction = direction,
            DeclarationPeriod = declarationPeriod,
            Status = HasInvoiceInfo(request.InvoiceNo, invoiceDate) ? StatusPosted : StatusDraft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.VatTransactions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("VAT.Create", $"Tạo giao dịch VAT {direction} từ {entity.SourceType}.", cancellationToken);
        await AuditAsync("VatTransaction", entity.Id.ToString(), "Create", null, entity, null, cancellationToken);
        return MapVatTransaction(entity, taxCode.Code);
    }

    public async Task SyncVatFromJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var journal = await _dbContext.JournalHeaders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == journalHeaderId, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy bút toán: {journalHeaderId}.");

        await SyncVatFromJournalAsync(journal, cancellationToken);
    }

    public async Task SyncVatFromJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        if (journal.Status != JournalStatus.Posted)
        {
            return;
        }

        if (journal.Lines.Count == 0)
        {
            await _dbContext.Entry(journal).Collection(x => x.Lines).LoadAsync(cancellationToken);
        }

        var roleByAccount = await BuildVatRoleMapAsync(cancellationToken);
        if (roleByAccount.Count == 0)
        {
            return;
        }

        var created = false;
        foreach (var line in journal.Lines.OrderBy(x => x.LineNo))
        {
            if (!roleByAccount.TryGetValue(line.AccountCode, out var role))
            {
                continue;
            }

            var direction = string.Equals(role, VatInputRole, StringComparison.OrdinalIgnoreCase) ? DirectionInput : DirectionOutput;
            var taxAmount = direction == DirectionInput ? line.DebitAmount : line.CreditAmount;
            if (taxAmount <= 0)
            {
                continue;
            }

            var exists = await _dbContext.VatTransactions.AnyAsync(x => x.JournalLineId == line.Id, cancellationToken);
            if (exists)
            {
                continue;
            }

            var taxCode = await ResolveTaxCodeForLineAsync(line, direction, cancellationToken);
            if (taxCode is null)
            {
                await LogAsync("VAT.SyncSkipped", $"Bỏ qua dòng VAT {journal.JournalNo}/{line.LineNo} vì thiếu TaxCode.", cancellationToken);
                continue;
            }

            var now = DateTime.UtcNow;
            var invoiceNo = NullIfWhiteSpace(journal.SourceNo) ?? journal.JournalNo;
            var invoiceDate = journal.JournalDate == default ? journal.PostingDate : journal.JournalDate;
            _dbContext.VatTransactions.Add(new VatTransaction
            {
                Id = Guid.NewGuid(),
                TaxCodeId = taxCode.Id,
                SourceType = journal.SourceType,
                SourceId = journal.SourceId,
                JournalHeaderId = journal.Id,
                JournalLineId = line.Id,
                InvoiceNo = invoiceNo,
                InvoiceDate = invoiceDate,
                PartnerType = ResolvePartnerType(line),
                PartnerId = line.CustomerId ?? line.SupplierId,
                TaxBaseAmount = taxCode.TaxRate > 0 ? Math.Round(taxAmount * 100m / taxCode.TaxRate, 2, MidpointRounding.AwayFromZero) : 0m,
                TaxRate = taxCode.TaxRate,
                TaxAmount = taxAmount,
                Direction = direction,
                DeclarationPeriod = journal.PostingDate.ToString("yyyy-MM"),
                Status = HasInvoiceInfo(invoiceNo, invoiceDate) ? StatusPosted : StatusDraft,
                CreatedAt = now,
                UpdatedAt = now
            });
            created = true;
        }

        if (created)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await LogAsync("VAT.SyncFromJournal", $"Đồng bộ VAT từ bút toán {journal.JournalNo}.", cancellationToken);
            await AuditAsync("VatTransaction", journal.Id.ToString(), "SyncFromJournal", null, new { journal.JournalNo, journal.Status, journal.SourceType, journal.SourceId }, null, cancellationToken);
        }
    }

    public async Task<TaxDeclarationDraftDto> BuildVatDeclarationDraftAsync(string declarationPeriod, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var period = NormalizePeriod(declarationPeriod, DateTime.Today);
        var transactions = await _dbContext.VatTransactions
            .Where(x => x.DeclarationPeriod == period && x.Status != StatusCancelled)
            .ToListAsync(cancellationToken);

        var input = transactions.Where(x => x.Direction == DirectionInput).Sum(x => x.TaxAmount);
        var output = transactions.Where(x => x.Direction == DirectionOutput).Sum(x => x.TaxAmount);
        var now = DateTime.UtcNow;
        var draft = await _dbContext.TaxDeclarationDrafts.FirstOrDefaultAsync(x => x.DeclarationPeriod == period && x.TaxType == "VAT", cancellationToken);
        if (draft is null)
        {
            draft = new TaxDeclarationDraft
            {
                Id = Guid.NewGuid(),
                DeclarationPeriod = period,
                TaxType = "VAT",
                CreatedAt = now
            };
            _dbContext.TaxDeclarationDrafts.Add(draft);
        }

        draft.InputVatAmount = input;
        draft.OutputVatAmount = output;
        draft.NetVatPayable = output - input;
        draft.Status = StatusDraft;
        draft.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("VAT.DeclarationDraft", $"Lập nháp tờ khai VAT kỳ {period}.", cancellationToken);
        await AuditAsync("TaxDeclarationDraft", draft.Id.ToString(), "BuildDraft", null, draft, null, cancellationToken);
        return MapDeclaration(draft);
    }

    public async Task<TaxDeclarationDraftDto> MarkDeclaredAsync(Guid declarationDraftId, string? declaredBy = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var draft = await _dbContext.TaxDeclarationDrafts.FirstOrDefaultAsync(x => x.Id == declarationDraftId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tờ khai VAT.");

        draft.Status = "Submitted";
        draft.UpdatedAt = DateTime.UtcNow;
        var transactions = await _dbContext.VatTransactions
            .Where(x => x.DeclarationPeriod == draft.DeclarationPeriod && x.Status != StatusCancelled)
            .ToListAsync(cancellationToken);
        foreach (var transaction in transactions)
        {
            transaction.Status = StatusDeclared;
            transaction.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("VAT.MarkDeclared", $"Đánh dấu đã kê khai VAT kỳ {draft.DeclarationPeriod}.", cancellationToken, declaredBy);
        await AuditAsync("TaxDeclarationDraft", draft.Id.ToString(), "MarkDeclared", null, new { draft.DeclarationPeriod, draft.Status }, declaredBy, cancellationToken);
        return MapDeclaration(draft);
    }

    public async Task<TaxDeclarationDraftDto> ReopenDeclarationAsync(Guid declarationDraftId, string reason, string? reopenedBy = null, CancellationToken cancellationToken = default)
    {
        using var correlationScope = _auditCorrelationService?.BeginScope();
        var draft = await _dbContext.TaxDeclarationDrafts.FirstOrDefaultAsync(x => x.Id == declarationDraftId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tờ khai VAT.");
        if (draft.Status == StatusDraft)
        {
            return MapDeclaration(draft);
        }

        draft.Status = StatusDraft;
        draft.UpdatedAt = DateTime.UtcNow;
        var transactions = await _dbContext.VatTransactions
            .Where(x => x.DeclarationPeriod == draft.DeclarationPeriod && x.Status == StatusDeclared)
            .ToListAsync(cancellationToken);
        foreach (var transaction in transactions)
        {
            transaction.Status = StatusPosted;
            transaction.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("VAT.ReopenDeclaration", $"Mở lại tờ khai VAT kỳ {draft.DeclarationPeriod}. Lý do: {reason}", cancellationToken, reopenedBy);
        await AuditAsync("TaxDeclarationDraft", draft.Id.ToString(), "Reopen", null, new { draft.DeclarationPeriod, draft.Status, Reason = reason }, reopenedBy, cancellationToken);
        return MapDeclaration(draft);
    }

    public async Task<IReadOnlyList<TaxDeclarationDraftDto>> GetDeclarationsAsync(string? declarationPeriod = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaxDeclarationDrafts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(declarationPeriod))
        {
            var period = NormalizePeriod(declarationPeriod, DateTime.Today);
            query = query.Where(x => x.DeclarationPeriod == period);
        }

        return await query
            .OrderByDescending(x => x.DeclarationPeriod)
            .ThenBy(x => x.TaxType)
            .Select(x => MapDeclaration(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VatTransactionDto>> GetVatTransactionsAsync(string? declarationPeriod = null, string? direction = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VatTransactions.AsNoTracking().Include(x => x.TaxCode).AsQueryable();
        if (!string.IsNullOrWhiteSpace(declarationPeriod))
        {
            var period = NormalizePeriod(declarationPeriod, DateTime.Today);
            query = query.Where(x => x.DeclarationPeriod == period);
        }

        if (!string.IsNullOrWhiteSpace(direction))
        {
            var normalizedDirection = NormalizeDirection(direction);
            query = query.Where(x => x.Direction == normalizedDirection);
        }

        return await query
            .OrderByDescending(x => x.InvoiceDate ?? x.CreatedAt)
            .ThenByDescending(x => x.InvoiceNo)
            .Select(x => MapVatTransaction(x, x.TaxCode == null ? string.Empty : x.TaxCode.Code))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, string>> BuildVatRoleMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsDefault && (x.RoleCode == VatInputRole || x.RoleCode == VatOutputRole))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.AccountCode)
            .ToDictionary(x => x.Key, x => x.First().RoleCode, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<TaxCode?> ResolveTaxCodeForLineAsync(JournalLine line, string direction, CancellationToken cancellationToken)
    {
        if (string.Equals(line.ObjectType, "TaxCode", StringComparison.OrdinalIgnoreCase) && line.ObjectId.HasValue)
        {
            var byId = await _dbContext.TaxCodes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == line.ObjectId.Value && x.IsActive, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        // VAT sync intentionally does not infer tax code from amount/account alone. The source document must carry TaxCode metadata.
        return null;
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, string? userName, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, userName, correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }

    private static string? ResolvePartnerType(JournalLine line)
    {
        if (line.CustomerId.HasValue)
        {
            return "Customer";
        }

        return line.SupplierId.HasValue ? "Supplier" : null;
    }

    private static TaxCodeDto MapTaxCode(TaxCode entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        TaxRate = entity.TaxRate,
        TaxType = entity.TaxType,
        InputAccountCode = entity.InputAccountCode,
        OutputAccountCode = entity.OutputAccountCode,
        IsActive = entity.IsActive
    };

    private static VatTransactionDto MapVatTransaction(VatTransaction entity, string taxCode) => new()
    {
        Id = entity.Id,
        TaxCodeId = entity.TaxCodeId,
        TaxCode = taxCode,
        SourceType = entity.SourceType,
        SourceId = entity.SourceId,
        JournalHeaderId = entity.JournalHeaderId,
        JournalLineId = entity.JournalLineId,
        InvoiceNo = entity.InvoiceNo,
        InvoiceDate = entity.InvoiceDate,
        PartnerType = entity.PartnerType,
        PartnerId = entity.PartnerId,
        TaxBaseAmount = entity.TaxBaseAmount,
        TaxRate = entity.TaxRate,
        TaxAmount = entity.TaxAmount,
        Direction = entity.Direction,
        DeclarationPeriod = entity.DeclarationPeriod,
        Status = entity.Status
    };

    private static TaxDeclarationDraftDto MapDeclaration(TaxDeclarationDraft entity) => new()
    {
        Id = entity.Id,
        DeclarationPeriod = entity.DeclarationPeriod,
        TaxType = entity.TaxType,
        InputVatAmount = entity.InputVatAmount,
        OutputVatAmount = entity.OutputVatAmount,
        NetVatPayable = entity.NetVatPayable,
        Status = entity.Status
    };

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }

    private static string NormalizeDirection(string direction)
    {
        var normalized = NormalizeRequired(direction, "Chiều VAT là bắt buộc.");
        if (string.Equals(normalized, DirectionInput, StringComparison.OrdinalIgnoreCase))
        {
            return DirectionInput;
        }

        if (string.Equals(normalized, DirectionOutput, StringComparison.OrdinalIgnoreCase))
        {
            return DirectionOutput;
        }

        throw new InvalidOperationException("Chiều VAT chỉ nhận Input hoặc Output.");
    }

    private static string NormalizePeriod(string? period, DateTime fallbackDate)
    {
        if (!string.IsNullOrWhiteSpace(period))
        {
            var trimmed = period.Trim();
            if (DateTime.TryParse($"01/{trimmed}", out var monthYear))
            {
                return monthYear.ToString("yyyy-MM");
            }

            if (DateTime.TryParse($"{trimmed}-01", out var yearMonth))
            {
                return yearMonth.ToString("yyyy-MM");
            }
        }

        return fallbackDate.ToString("yyyy-MM");
    }

    private static bool HasInvoiceInfo(string? invoiceNo, DateTime? invoiceDate)
        => !string.IsNullOrWhiteSpace(invoiceNo) && invoiceDate.HasValue;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task LogAsync(string action, string detail, CancellationToken cancellationToken, string? username = null)
        => _auditLogService is null ? Task.CompletedTask : _auditLogService.LogAsync(action, detail, username ?? "system", cancellationToken);
}
