using System.Text;
using System.Xml.Linq;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EInvoiceService : IEInvoiceService
{
    private readonly AppDbContext _dbContext;
    private readonly IEInvoiceXmlGenerator _xmlGenerator;
    private readonly IEnumerable<IEInvoiceProvider> _providers;

    public EInvoiceService(AppDbContext dbContext, IEInvoiceXmlGenerator xmlGenerator, IEnumerable<IEInvoiceProvider> providers)
    {
        _dbContext = dbContext;
        _xmlGenerator = xmlGenerator;
        _providers = providers;
    }

    public async Task<Guid> CreateDraftAsync(InvoiceDraftDto draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (string.IsNullOrWhiteSpace(draft.InvoiceNo)) throw new InvalidOperationException("Số hóa đơn là bắt buộc.");
        if (string.IsNullOrWhiteSpace(draft.BuyerName)) throw new InvalidOperationException("Tên người mua là bắt buộc.");
        if (draft.Lines.Count == 0) throw new InvalidOperationException("Hóa đơn phải có ít nhất một dòng.");

        var invoice = new Invoice
        {
            InvoiceNo = draft.InvoiceNo.Trim(),
            InvoiceSeries = draft.InvoiceSeries.Trim(),
            TemplateCode = draft.TemplateCode.Trim(),
            InvoiceDate = draft.InvoiceDate,
            BuyerName = draft.BuyerName.Trim(),
            BuyerTaxCode = draft.BuyerTaxCode?.Trim(),
            BuyerAddress = draft.BuyerAddress?.Trim(),
            ProviderCode = NormalizeProvider(draft.ProviderCode),
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

    public async Task<InvoiceIssueResultDto> QueueIssueAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices.FirstAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice.Status is not "Draft" and not "Adjusted")
        {
            throw new InvalidOperationException("Chỉ hóa đơn nháp hoặc điều chỉnh mới được đưa vào hàng đợi phát hành.");
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
        var log = await _dbContext.InvoiceIssueLogs
            .Include(x => x.Invoice)
            .ThenInclude(x => x!.TaxBreakdowns)
            .Where(x => x.Status == "Queued" && x.Invoice != null)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (log?.Invoice is null)
        {
            return new InvoiceIssueResultDto { Status = "Idle", Message = "Không có hóa đơn chờ phát hành." };
        }

        var invoice = log.Invoice;
        var provider = ResolveProvider(invoice.ProviderCode);
        log.Status = "Running";
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
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

    public async Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(string? status = null, int take = 200, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Invoices.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 1_000))
            .Select(x => new InvoiceSummaryDto
            {
                Id = x.Id,
                InvoiceNo = x.InvoiceNo,
                BuyerName = x.BuyerName,
                InvoiceDate = x.InvoiceDate,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                ProviderCode = x.ProviderCode
            })
            .ToListAsync(cancellationToken);
    }

    private IEInvoiceProvider ResolveProvider(string providerCode)
    {
        return _providers.FirstOrDefault(x => string.Equals(x.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase))
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

public sealed class StubEInvoiceProvider : IEInvoiceProvider
{
    public StubEInvoiceProvider(string providerCode)
    {
        ProviderCode = providerCode;
    }

    public string ProviderCode { get; }

    public Task<InvoiceIssueResultDto> IssueAsync(Guid invoiceId, string xmlPayload, CancellationToken cancellationToken = default)
        => Task.FromResult(new InvoiceIssueResultDto
        {
            InvoiceId = invoiceId,
            ProviderCode = ProviderCode,
            Status = "Issued",
            ProviderInvoiceId = $"{ProviderCode}-{invoiceId:N}",
            Message = "Provider foundation issued invoice in local stub mode."
        });

    public Task<InvoiceIssueResultDto> QueryStatusAsync(Guid invoiceId, string? providerInvoiceId, CancellationToken cancellationToken = default)
        => Task.FromResult(new InvoiceIssueResultDto
        {
            InvoiceId = invoiceId,
            ProviderCode = ProviderCode,
            Status = string.IsNullOrWhiteSpace(providerInvoiceId) ? "Signed" : "Issued",
            ProviderInvoiceId = providerInvoiceId,
            Message = "Provider status foundation."
        });
}
