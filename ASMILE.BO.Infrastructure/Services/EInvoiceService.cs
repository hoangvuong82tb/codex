using System.Text;
using System.Xml.Linq;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EInvoiceService : IEInvoiceService
{
    private readonly AppDbContext _dbContext;
    private readonly IEInvoiceXmlGenerator _xmlGenerator;
    private readonly IEnumerable<IEInvoiceProvider> _providers;
    private readonly ISecretProtector _secretProtector;

    public EInvoiceService(
        AppDbContext dbContext,
        IEInvoiceXmlGenerator xmlGenerator,
        IEnumerable<IEInvoiceProvider> providers,
        ISecretProtector? secretProtector = null)
    {
        _dbContext = dbContext;
        _xmlGenerator = xmlGenerator;
        _providers = providers;
        _secretProtector = secretProtector ?? new PlainTextSecretProtector();
    }

    public async Task<string> GetDefaultProviderCodeAsync(CancellationToken cancellationToken = default)
    {
        var configuredProvider = await _dbContext.InvoiceProviderConfigurations
            .AsNoTracking()
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.ProviderCode))
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(x => x.ProviderCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            return NormalizeProvider(configuredProvider);
        }

        var registeredProvider = _providers
            .FirstOrDefault(x => x is not UnavailableEInvoiceProvider && !string.IsNullOrWhiteSpace(x.ProviderCode));
        return registeredProvider is not null
            ? NormalizeProvider(registeredProvider.ProviderCode)
            : "MISA";
    }

    public async Task<IReadOnlyList<InvoiceProviderConfigurationDto>> GetProviderConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.InvoiceProviderConfigurations
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.ProviderCode)
            .Select(x => new InvoiceProviderConfigurationDto
            {
                Id = x.Id,
                ProviderCode = x.ProviderCode,
                DisplayName = x.DisplayName,
                EndpointUrl = x.EndpointUrl,
                TenantCode = x.TenantCode,
                Username = x.Username,
                HasSecret = !string.IsNullOrWhiteSpace(x.ProtectedSecret),
                IsActive = x.IsActive,
                IsDefault = x.IsDefault,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceProviderReadinessDto> GetProviderReadinessAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _dbContext.InvoiceProviderConfigurations
            .AsNoTracking()
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.ProviderCode))
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var providerCode = configuration is not null
            ? NormalizeProvider(configuration.ProviderCode)
            : await GetDefaultProviderCodeAsync(cancellationToken);
        var hasRealAdapter = _providers.Any(x =>
            x is not UnavailableEInvoiceProvider
            && string.Equals(NormalizeProvider(x.ProviderCode), providerCode, StringComparison.OrdinalIgnoreCase));

        var hasActiveConfiguration = configuration is not null;
        var endpoint = configuration?.EndpointUrl?.Trim();
        var hasValidEndpoint = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            && (endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || endpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && (!endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) || endpointUri.IsLoopback);
        var hasTenant = !string.IsNullOrWhiteSpace(configuration?.TenantCode);
        var hasUsername = !string.IsNullOrWhiteSpace(configuration?.Username);
        var hasSecret = !string.IsNullOrWhiteSpace(configuration?.ProtectedSecret);
        var isReady = hasRealAdapter
            && hasActiveConfiguration
            && hasValidEndpoint
            && hasTenant
            && hasUsername
            && hasSecret;

        string status;
        string message;
        if (!hasRealAdapter)
        {
            status = "Unavailable";
            message = $"Provider {providerCode} chưa có adapter thật được đăng ký.";
        }
        else if (!hasActiveConfiguration)
        {
            status = "Incomplete";
            message = $"Provider {providerCode} chưa có cấu hình active.";
        }
        else if (!hasValidEndpoint || !hasTenant || !hasUsername || !hasSecret)
        {
            status = "Incomplete";
            var missing = new List<string>();
            if (!hasValidEndpoint) missing.Add("endpoint hợp lệ");
            if (!hasTenant) missing.Add("tenant");
            if (!hasUsername) missing.Add("username");
            if (!hasSecret) missing.Add("secret");
            message = $"Cấu hình provider {providerCode} còn thiếu: {string.Join(", ", missing)}.";
        }
        else
        {
            status = "Ready";
            message = $"Provider {providerCode} sẵn sàng phát hành hóa đơn điện tử.";
        }

        return new InvoiceProviderReadinessDto
        {
            ProviderCode = providerCode,
            HasRealAdapter = hasRealAdapter,
            HasActiveConfiguration = hasActiveConfiguration,
            HasValidEndpoint = hasValidEndpoint,
            HasTenant = hasTenant,
            HasUsername = hasUsername,
            HasSecret = hasSecret,
            IsReady = isReady,
            Status = status,
            Message = message
        };
    }

    public async Task<Guid> SaveProviderConfigurationAsync(
        InvoiceProviderConfigurationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var providerCode = NormalizeProvider(request.ProviderCode);
        var displayName = request.DisplayName.Trim();
        var endpointUrl = request.EndpointUrl.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Tên cấu hình provider là bắt buộc.");
        }

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("https" or "http"))
        {
            throw new InvalidOperationException("Endpoint provider phải là URL http hoặc https hợp lệ.");
        }

        // Provider credentials/secrets must never be sent to a public endpoint
        // over clear text HTTP. Keep loopback HTTP available for local/UAT
        // harnesses, but require HTTPS for every non-local provider endpoint.
        if (endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !endpoint.IsLoopback)
        {
            throw new InvalidOperationException("Endpoint provider bên ngoài phải sử dụng HTTPS để bảo vệ thông tin xác thực.");
        }

        var configuration = request.Id is { } id
            ? await _dbContext.InvoiceProviderConfigurations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            : null;
        if (request.Id.HasValue && configuration is null)
        {
            throw new KeyNotFoundException("Không tìm thấy cấu hình provider cần cập nhật.");
        }

        configuration ??= await _dbContext.InvoiceProviderConfigurations
            .FirstOrDefaultAsync(x => x.ProviderCode == providerCode, cancellationToken);
        if (configuration is null)
        {
            configuration = new InvoiceProviderConfiguration { ProviderCode = providerCode };
            _dbContext.InvoiceProviderConfigurations.Add(configuration);
        }

        configuration.ProviderCode = providerCode;
        configuration.DisplayName = displayName;
        configuration.EndpointUrl = endpointUrl;
        configuration.TenantCode = string.IsNullOrWhiteSpace(request.TenantCode) ? null : request.TenantCode.Trim();
        configuration.Username = string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim();
        configuration.IsActive = request.IsActive;
        configuration.IsDefault = request.IsDefault;
        configuration.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Secret))
        {
            configuration.ProtectedSecret = _secretProtector.Protect(request.Secret.Trim());
        }

        if (configuration.IsDefault)
        {
            var siblings = await _dbContext.InvoiceProviderConfigurations
                .Where(x => x.Id != configuration.Id && x.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var sibling in siblings)
            {
                sibling.IsDefault = false;
                sibling.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return configuration.Id;
    }

    public async Task<Guid> CreateDraftAsync(InvoiceDraftDto draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (string.IsNullOrWhiteSpace(draft.InvoiceNo)) throw new InvalidOperationException("Số hóa đơn là bắt buộc.");
        if (string.IsNullOrWhiteSpace(draft.BuyerName)) throw new InvalidOperationException("Tên người mua là bắt buộc.");
        if (draft.Lines.Count == 0) throw new InvalidOperationException("Hóa đơn phải có ít nhất một dòng.");

        var providerCode = string.IsNullOrWhiteSpace(draft.ProviderCode)
            ? await GetDefaultProviderCodeAsync(cancellationToken)
            : NormalizeProvider(draft.ProviderCode);
        var invoice = new Invoice
        {
            InvoiceNo = draft.InvoiceNo.Trim(),
            InvoiceSeries = draft.InvoiceSeries.Trim(),
            TemplateCode = draft.TemplateCode.Trim(),
            InvoiceDate = draft.InvoiceDate,
            BuyerName = draft.BuyerName.Trim(),
            BuyerTaxCode = draft.BuyerTaxCode?.Trim(),
            BuyerAddress = draft.BuyerAddress?.Trim(),
            ProviderCode = providerCode,
            JournalHeaderId = draft.JournalHeaderId,
            ReceivableTransactionId = draft.ReceivableTransactionId,
            PayableTransactionId = draft.PayableTransactionId,
            Status = "Draft"
        };

        var lineNo = 1;
        foreach (var line in draft.Lines)
        {
            var amount = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var taxAmount = Math.Round(amount * line.TaxRate / 100m, 2, MidpointRounding.AwayFromZero);
            invoice.Lines.Add(new InvoiceLine
            {
                LineNo = lineNo++,
                ItemCode = line.ItemCode.Trim(),
                ItemName = line.ItemName.Trim(),
                UnitName = line.UnitName.Trim(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineAmount = amount,
                TaxRate = line.TaxRate,
                TaxAmount = taxAmount,
                JournalAccountCode = line.JournalAccountCode
            });
        }

        foreach (var group in invoice.Lines.GroupBy(x => x.TaxRate))
        {
            invoice.TaxBreakdowns.Add(new InvoiceTaxBreakdown
            {
                TaxRate = group.Key,
                TaxCode = $"VAT{group.Key:0.####}",
                TaxBaseAmount = group.Sum(x => x.LineAmount),
                TaxAmount = group.Sum(x => x.TaxAmount)
            });
        }

        invoice.SubtotalAmount = invoice.Lines.Sum(x => x.LineAmount);
        invoice.TaxAmount = invoice.Lines.Sum(x => x.TaxAmount);
        invoice.TotalAmount = invoice.SubtotalAmount + invoice.TaxAmount;

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task<Guid> CreateAdjustmentDraftAsync(InvoiceAdjustmentDraftDto draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.SourceInvoiceId == Guid.Empty)
        {
            throw new InvalidOperationException("Hóa đơn gốc của nghiệp vụ điều chỉnh là bắt buộc.");
        }

        var reason = draft.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Lý do điều chỉnh hóa đơn là bắt buộc.");
        }

        if (reason.Length > 2_000)
        {
            throw new InvalidOperationException("Lý do điều chỉnh không được vượt quá 2.000 ký tự.");
        }

        var description = draft.Description.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Diễn giải điều chỉnh hóa đơn là bắt buộc.");
        }

        var adjustmentType = NormalizeAdjustmentType(draft.AdjustmentType);
        if (draft.Amount < 0m || (adjustmentType is "Increase" or "Decrease" && draft.Amount == 0m))
        {
            throw new InvalidOperationException("Số tiền điều chỉnh phải lớn hơn 0 đối với điều chỉnh tăng/giảm.");
        }

        if (draft.TaxRate is < 0m or > 100m)
        {
            throw new InvalidOperationException("Thuế suất điều chỉnh phải nằm trong khoảng 0 đến 100%.");
        }

        var source = await _dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == draft.SourceInvoiceId, cancellationToken);
        if (source is null)
        {
            throw new KeyNotFoundException("Không tìm thấy hóa đơn gốc cần điều chỉnh.");
        }

        if (source.Status is not ("Issued" or "Sent") || string.IsNullOrWhiteSpace(source.ProviderInvoiceId))
        {
            throw new InvalidOperationException("Chỉ hóa đơn đã phát hành và có mã provider mới được tạo hóa đơn điều chỉnh.");
        }

        var signedAmount = adjustmentType == "Decrease" ? -draft.Amount : draft.Amount;
        var taxAmount = Math.Round(signedAmount * draft.TaxRate / 100m, 2, MidpointRounding.AwayFromZero);
        var adjustmentNo = $"ADJ-{source.InvoiceNo}-{Guid.NewGuid():N}";
        var adjustment = new Invoice
        {
            InvoiceNo = adjustmentNo[..Math.Min(100, adjustmentNo.Length)],
            InvoiceSeries = source.InvoiceSeries,
            TemplateCode = source.TemplateCode,
            InvoiceDate = DateTime.UtcNow,
            BuyerName = source.BuyerName,
            BuyerTaxCode = source.BuyerTaxCode,
            BuyerAddress = source.BuyerAddress,
            CurrencyCode = source.CurrencyCode,
            ExchangeRate = source.ExchangeRate,
            Status = "Adjusted",
            ProviderCode = source.ProviderCode,
            AdjustsInvoiceId = source.Id,
            AdjustmentType = adjustmentType,
            AdjustmentReason = reason,
            FolioId = source.FolioId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SubtotalAmount = signedAmount,
            TaxAmount = taxAmount,
            TotalAmount = signedAmount + taxAmount
        };

        adjustment.Lines.Add(new InvoiceLine
        {
            LineNo = 1,
            ItemCode = string.IsNullOrWhiteSpace(draft.ItemCode) ? "ADJUSTMENT" : draft.ItemCode.Trim(),
            ItemName = description,
            UnitName = string.IsNullOrWhiteSpace(draft.UnitName) ? "Lần" : draft.UnitName.Trim(),
            Quantity = 1m,
            UnitPrice = signedAmount,
            LineAmount = signedAmount,
            TaxRate = draft.TaxRate,
            TaxAmount = taxAmount
        });
        adjustment.TaxBreakdowns.Add(new InvoiceTaxBreakdown
        {
            TaxRate = draft.TaxRate,
            TaxCode = $"VAT{draft.TaxRate:0.####}",
            TaxBaseAmount = signedAmount,
            TaxAmount = taxAmount
        });

        _dbContext.Invoices.Add(adjustment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return adjustment.Id;
    }

    public async Task<InvoiceIssueResultDto> QueueIssueAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices.FirstAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice.Status is not "Draft" and not "Adjusted")
        {
            throw new InvalidOperationException("Chỉ hóa đơn nháp hoặc điều chỉnh mới được đưa vào hàng đợi phát hành.");
        }

        var provider = ResolveProvider(invoice.ProviderCode);
        if (provider is UnavailableEInvoiceProvider)
        {
            throw new InvalidOperationException($"Chưa cấu hình kết nối hóa đơn điện tử thật cho provider {invoice.ProviderCode}.");
        }

        invoice.XmlPayload = await _xmlGenerator.GenerateAsync(invoiceId, cancellationToken);
        invoice.Status = "Signed";
        invoice.UpdatedAt = DateTime.UtcNow;
        var attemptNo = await _dbContext.InvoiceIssueLogs.CountAsync(x => x.InvoiceId == invoiceId, cancellationToken) + 1;
        _dbContext.InvoiceIssueLogs.Add(new InvoiceIssueLog
        {
            InvoiceId = invoice.Id,
            ProviderCode = invoice.ProviderCode,
            Status = "Queued",
            AttemptNo = attemptNo,
            RequestXml = invoice.XmlPayload
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new InvoiceIssueResultDto { InvoiceId = invoiceId, ProviderCode = invoice.ProviderCode, Status = invoice.Status, Message = "Đã đưa hóa đơn vào hàng đợi phát hành." };
    }

    public async Task<InvoiceIssueResultDto> ProcessNextQueuedIssueAsync(CancellationToken cancellationToken = default)
    {
        var candidateId = await _dbContext.InvoiceIssueLogs
            .AsNoTracking()
            .Where(x => x.Status == "Queued" && x.Invoice != null)
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidateId is null)
        {
            return new InvoiceIssueResultDto { Status = "Idle", Message = "Không có hóa đơn chờ phát hành." };
        }

        // Claim the queue row with a conditional UPDATE. Two scheduler instances
        // may select the same candidate, but only one can move it from Queued to
        // Running. This prevents duplicate provider calls and duplicate VAT output.
        var claimed = _dbContext.Database.IsRelational()
            ? await _dbContext.InvoiceIssueLogs
                .Where(x => x.Id == candidateId.Value && x.Status == "Queued")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Running"), cancellationToken)
            : await ClaimQueueRowForNonRelationalProviderAsync(candidateId.Value, cancellationToken);
        if (claimed == 0)
        {
            return new InvoiceIssueResultDto { Status = "Busy", Message = "Hóa đơn đang được một tiến trình khác xử lý." };
        }

        var log = await _dbContext.InvoiceIssueLogs
            .Include(x => x.Invoice)
            .ThenInclude(x => x!.TaxBreakdowns)
            .SingleOrDefaultAsync(x => x.Id == candidateId.Value, cancellationToken);
        if (log?.Invoice is null)
        {
            return new InvoiceIssueResultDto { Status = "Idle", Message = "Không có hóa đơn chờ phát hành." };
        }

        var invoice = log.Invoice;
        log.Status = "Running";
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var provider = ResolveProvider(invoice.ProviderCode);
            var result = await provider.IssueAsync(invoice.Id, invoice.XmlPayload ?? await _xmlGenerator.GenerateAsync(invoice.Id, cancellationToken), cancellationToken);
            invoice.Status = result.Status;
            invoice.ProviderInvoiceId = result.ProviderInvoiceId;
            invoice.IssuedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;
            log.Status = "Completed";
            log.CompletedAt = DateTime.UtcNow;
            log.ResponsePayload = result.Message;
            await SyncVatFromInvoiceAsync(invoice, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.CompletedAt = DateTime.UtcNow;
            log.ErrorMessage = ex.Message;
            invoice.Status = "Draft";
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<int> ClaimQueueRowForNonRelationalProviderAsync(Guid queueId, CancellationToken cancellationToken)
    {
        // EF Core's InMemory provider cannot translate ExecuteUpdateAsync. Keep
        // unit tests and local diagnostics functional while production relational
        // databases use the atomic conditional UPDATE above.
        var queueRow = await _dbContext.InvoiceIssueLogs
            .SingleOrDefaultAsync(x => x.Id == queueId && x.Status == "Queued", cancellationToken);
        if (queueRow is null)
        {
            return 0;
        }

        queueRow.Status = "Running";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public Task<string> GenerateXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return _xmlGenerator.GenerateAsync(invoiceId, cancellationToken);
    }

    public async Task<byte[]> GeneratePdfPreviewAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices.AsNoTracking().Include(x => x.Lines).FirstAsync(x => x.Id == invoiceId, cancellationToken);
        var preview = new StringBuilder()
            .AppendLine("ASMILE BO - Xem trước hóa đơn điện tử")
            .AppendLine($"Số: {invoice.InvoiceNo}")
            .AppendLine($"Người mua: {invoice.BuyerName}")
            .AppendLine($"Tổng tiền: {invoice.TotalAmount:0.##}")
            .AppendLine($"Trạng thái: {invoice.Status}");
        foreach (var line in invoice.Lines.OrderBy(x => x.LineNo))
        {
            preview.AppendLine($"{line.LineNo}. {line.ItemName} - {line.LineAmount:0.##}");
        }

        return Encoding.UTF8.GetBytes(preview.ToString());
    }

    public async Task<InvoiceIssueResultDto> CancelAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Lý do hủy hóa đơn là bắt buộc.");
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length > 2_000)
        {
            throw new InvalidOperationException("Lý do hủy hóa đơn không được vượt quá 2.000 ký tự.");
        }

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new KeyNotFoundException("Không tìm thấy hóa đơn cần hủy.");
        }

        if (string.Equals(invoice.Status, "Issued", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(invoice.ProviderInvoiceId))
        {
            throw new InvalidOperationException("Hóa đơn đã phát hành hoặc đã có mã provider; cần nghiệp vụ điều chỉnh/hủy với nhà cung cấp hóa đơn điện tử.");
        }

        if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Hóa đơn đã được hủy trước đó.");
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(invoice.Status, "Adjusted", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(invoice.Status, "Signed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Không thể hủy hóa đơn ở trạng thái {invoice.Status}.");
        }

        var now = DateTime.UtcNow;
        var pendingLogs = await _dbContext.InvoiceIssueLogs
            .Where(x => x.InvoiceId == invoiceId && (x.Status == "Queued" || x.Status == "Running"))
            .ToListAsync(cancellationToken);
        foreach (var log in pendingLogs)
        {
            log.Status = "Cancelled";
            log.CompletedAt = now;
            log.ErrorMessage = normalizedReason;
        }

        var lastAttempt = await _dbContext.InvoiceIssueLogs
            .Where(x => x.InvoiceId == invoiceId)
            .Select(x => (int?)x.AttemptNo)
            .MaxAsync(cancellationToken) ?? 0;
        _dbContext.InvoiceIssueLogs.Add(new InvoiceIssueLog
        {
            InvoiceId = invoiceId,
            ProviderCode = invoice.ProviderCode,
            Status = "Cancelled",
            AttemptNo = lastAttempt + 1,
            CompletedAt = now,
            ErrorMessage = normalizedReason,
            ResponsePayload = "Cancelled before provider issuance."
        });

        invoice.Status = "Cancelled";
        invoice.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InvoiceIssueResultDto
        {
            InvoiceId = invoiceId,
            ProviderCode = invoice.ProviderCode,
            Status = invoice.Status,
            Message = "Đã hủy hóa đơn nháp và ghi nhận lý do vào nhật ký phát hành."
        };
    }

    public async Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(string? status = null, int take = 200, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Invoices.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var invoices = await query
            .Include(x => x.Lines)
            .OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 1_000))
            .ToListAsync(cancellationToken);

        var receivableIds = invoices
            .Where(x => x.ReceivableTransactionId.HasValue)
            .Select(x => x.ReceivableTransactionId!.Value)
            .ToList();
        var receivableSummaries = receivableIds.Count == 0
            ? new Dictionary<Guid, ReceivableInvoiceSummary>()
            : await _dbContext.ReceivableTransactions
                .AsNoTracking()
                .Where(x => receivableIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => new ReceivableInvoiceSummary(x.CustomerId, x.DebitAmount, x.CreditAmount, x.BalanceAmount, x.Status),
                    cancellationToken);
        var receivableCustomers = receivableSummaries.ToDictionary(x => x.Key, x => x.Value.CustomerId);
        var customerIdsByEntityGuid = new Dictionary<Guid, string>();
        if (receivableCustomers.Count > 0)
        {
            var customerIds = await _dbContext.Customers
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            foreach (var customerId in customerIds)
                customerIdsByEntityGuid[GuidUtility.Create(GuidUtility.UrlNamespace, $"customer:{customerId}")] = customerId.ToString();
        }

        return invoices.Select(x =>
        {
            var receivable = x.ReceivableTransactionId is Guid receivableId
                && receivableSummaries.TryGetValue(receivableId, out var summary)
                ? summary
                : null;

            return new InvoiceSummaryDto
            {
                Id = x.Id,
                ReceivableTransactionId = x.ReceivableTransactionId,
                ReceivableCustomerId = x.ReceivableTransactionId.HasValue
                    && receivableCustomers.TryGetValue(x.ReceivableTransactionId.Value, out var customerEntityId)
                    && customerIdsByEntityGuid.TryGetValue(customerEntityId, out var customerId)
                    ? customerId
                    : null,
                InvoiceNo = x.InvoiceNo,
                BuyerName = x.BuyerName,
                BuyerTaxCode = x.BuyerTaxCode,
                BuyerAddress = x.BuyerAddress,
                InvoiceDate = x.InvoiceDate,
                CurrencyCode = x.CurrencyCode,
                ExchangeRate = x.ExchangeRate,
                SubtotalAmount = x.SubtotalAmount,
                TaxAmount = x.TaxAmount,
                TotalAmount = x.TotalAmount,
                ReceivableDebitAmount = receivable?.DebitAmount ?? 0m,
                ReceivableCreditAmount = receivable?.CreditAmount ?? 0m,
                ReceivableBalanceAmount = receivable?.BalanceAmount ?? 0m,
                ReceivableStatus = receivable?.Status,
                Status = x.Status,
                ProviderCode = x.ProviderCode,
                ProviderInvoiceId = x.ProviderInvoiceId,
                IssuedAt = x.IssuedAt,
                AdjustsInvoiceId = x.AdjustsInvoiceId,
                AdjustmentType = x.AdjustmentType,
                AdjustmentReason = x.AdjustmentReason,
                Lines = x.Lines
                    .OrderBy(line => line.LineNo)
                    .Select(line => new InvoiceLineSummaryDto
                    {
                        LineNo = line.LineNo,
                        ItemCode = line.ItemCode,
                        ItemName = line.ItemName,
                        UnitName = line.UnitName,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineAmount = line.LineAmount,
                        TaxRate = line.TaxRate,
                        TaxAmount = line.TaxAmount,
                        JournalAccountCode = line.JournalAccountCode
                    })
                    .ToList()
            };
        })
            .ToList();
    }

    private sealed record ReceivableInvoiceSummary(
        Guid CustomerId,
        decimal DebitAmount,
        decimal CreditAmount,
        decimal BalanceAmount,
        string Status);

    public async Task<IReadOnlyList<InvoiceIssueFailureDto>> GetFailedIssueLogsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InvoiceIssueLogs
            .AsNoTracking()
            .Include(x => x.Invoice)
            .Where(x => x.Status == "Failed")
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new InvoiceIssueFailureDto
            {
                LogId = x.Id,
                InvoiceId = x.InvoiceId,
                InvoiceNo = x.Invoice != null ? x.Invoice.InvoiceNo : string.Empty,
                ProviderCode = x.ProviderCode,
                AttemptNo = x.AttemptNo,
                ErrorMessage = x.ErrorMessage,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private IEInvoiceProvider ResolveProvider(string providerCode)
    {
        // The built-in unavailable providers are deliberate fail-closed sentinels.
        // Prefer a real adapter when an installation registers one alongside the
        // sentinel; registration order must not make a configured adapter unusable.
        return _providers.FirstOrDefault(x =>
                x is not UnavailableEInvoiceProvider
                && string.Equals(x.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase))
            ?? _providers.FirstOrDefault(x => string.Equals(x.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Chưa cấu hình provider hóa đơn điện tử {providerCode}.");
    }

    private async Task SyncVatFromInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (await _dbContext.VatTransactions.AnyAsync(x => x.SourceType == "EInvoice" && x.SourceId == invoice.Id, cancellationToken))
        {
            return;
        }

        var taxBreakdowns = await _dbContext.InvoiceTaxBreakdowns
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);

        foreach (var tax in taxBreakdowns)
        {
            var taxCode = await _dbContext.TaxCodes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TaxRate == tax.TaxRate && x.IsActive, cancellationToken);
            if (taxCode is null)
            {
                taxCode = new TaxCode
                {
                    Id = Guid.NewGuid(),
                    Code = tax.TaxCode,
                    Name = $"Thuế GTGT {tax.TaxRate:0.####}%",
                    TaxRate = tax.TaxRate,
                    TaxType = "VATOutput",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.TaxCodes.Add(taxCode);
            }

            _dbContext.VatTransactions.Add(new VatTransaction
            {
                Id = Guid.NewGuid(),
                TaxCodeId = taxCode.Id,
                SourceType = "EInvoice",
                SourceId = invoice.Id,
                JournalHeaderId = invoice.JournalHeaderId,
                InvoiceNo = invoice.InvoiceNo,
                InvoiceDate = invoice.InvoiceDate,
                PartnerType = "Customer",
                TaxBaseAmount = tax.TaxBaseAmount,
                TaxRate = tax.TaxRate,
                TaxAmount = tax.TaxAmount,
                Direction = "Output",
                DeclarationPeriod = invoice.InvoiceDate.ToString("yyyy-MM"),
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static string NormalizeProvider(string providerCode)
        => providerCode.Trim().ToUpperInvariant() switch
        {
            "VIETTEL" => "VIETTEL",
            "VNPT" => "VNPT",
            "MISA" => "MISA",
            _ => throw new InvalidOperationException("Provider hóa đơn điện tử chỉ hỗ trợ Viettel, VNPT hoặc MISA.")
        };

    private static string NormalizeAdjustmentType(string adjustmentType)
        => adjustmentType.Trim().ToUpperInvariant() switch
        {
            "INCREASE" or "TĂNG" => "Increase",
            "DECREASE" or "GIẢM" => "Decrease",
            "INFORMATION" or "THÔNG TIN" => "Information",
            _ => throw new InvalidOperationException("Loại điều chỉnh chỉ hỗ trợ tăng, giảm hoặc thông tin.")
        };
}

public sealed class EInvoiceXmlGenerator : IEInvoiceXmlGenerator
{
    private readonly AppDbContext _dbContext;

    public EInvoiceXmlGenerator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.TaxBreakdowns)
            .FirstAsync(x => x.Id == invoiceId, cancellationToken);

        var document = new XDocument(
            new XElement("EInvoice",
                new XAttribute("provider", invoice.ProviderCode),
                new XElement("Header",
                    new XElement("InvoiceNo", invoice.InvoiceNo),
                    new XElement("Series", invoice.InvoiceSeries),
                    new XElement("TemplateCode", invoice.TemplateCode),
                    new XElement("InvoiceDate", invoice.InvoiceDate.ToString("yyyy-MM-dd")),
                    new XElement("BuyerName", invoice.BuyerName),
                    new XElement("BuyerTaxCode", invoice.BuyerTaxCode ?? string.Empty),
                    new XElement("BuyerAddress", invoice.BuyerAddress ?? string.Empty),
                    new XElement("Currency", invoice.CurrencyCode)),
                new XElement("Lines",
                    invoice.Lines.OrderBy(x => x.LineNo).Select(x =>
                        new XElement("Line",
                            new XElement("LineNo", x.LineNo),
                            new XElement("ItemCode", x.ItemCode),
                            new XElement("ItemName", x.ItemName),
                            new XElement("UnitName", x.UnitName),
                            new XElement("Quantity", x.Quantity),
                            new XElement("UnitPrice", x.UnitPrice),
                            new XElement("LineAmount", x.LineAmount),
                            new XElement("TaxRate", x.TaxRate),
                            new XElement("TaxAmount", x.TaxAmount)))),
                new XElement("TaxBreakdowns",
                    invoice.TaxBreakdowns.OrderBy(x => x.TaxRate).Select(x =>
                        new XElement("Tax",
                            new XElement("TaxCode", x.TaxCode),
                            new XElement("TaxRate", x.TaxRate),
                            new XElement("TaxBaseAmount", x.TaxBaseAmount),
                            new XElement("TaxAmount", x.TaxAmount)))),
                new XElement("Totals",
                    new XElement("SubtotalAmount", invoice.SubtotalAmount),
                    new XElement("TaxAmount", invoice.TaxAmount),
                    new XElement("TotalAmount", invoice.TotalAmount))));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}

/// <summary>
/// Production-safe placeholder used until a real provider adapter is configured.
/// It deliberately throws so the invoice workflow records a failed attempt and
/// never persists a fake provider invoice number or an Issued status.
/// </summary>
public sealed class UnavailableEInvoiceProvider(string providerCode) : IEInvoiceProvider
{
    public string ProviderCode { get; } = providerCode;

    public Task<InvoiceIssueResultDto> IssueAsync(
        Guid invoiceId,
        string xmlPayload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException($"Chưa cấu hình kết nối hóa đơn điện tử thật cho provider {ProviderCode}.");
    }

    public Task<InvoiceIssueResultDto> QueryStatusAsync(
        Guid invoiceId,
        string? providerInvoiceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException($"Chưa cấu hình kết nối hóa đơn điện tử thật cho provider {ProviderCode}.");
    }
}
