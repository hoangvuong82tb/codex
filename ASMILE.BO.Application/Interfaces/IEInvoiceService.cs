using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IEInvoiceService
{
    Task<Guid> CreateDraftAsync(InvoiceDraftDto draft, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> QueueIssueAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> ProcessNextQueuedIssueAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfPreviewAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(string? status = null, int take = 200, CancellationToken cancellationToken = default);
}

public interface IEInvoiceProvider
{
    string ProviderCode { get; }
    Task<InvoiceIssueResultDto> IssueAsync(Guid invoiceId, string xmlPayload, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> QueryStatusAsync(Guid invoiceId, string? providerInvoiceId, CancellationToken cancellationToken = default);
}

public interface IEInvoiceXmlGenerator
{
    Task<string> GenerateAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
