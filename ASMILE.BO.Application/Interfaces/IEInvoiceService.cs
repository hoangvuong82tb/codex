using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IEInvoiceService
{
    Task<string> GetDefaultProviderCodeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceProviderConfigurationDto>> GetProviderConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<InvoiceProviderReadinessDto> GetProviderReadinessAsync(CancellationToken cancellationToken = default);
    Task<Guid> SaveProviderConfigurationAsync(InvoiceProviderConfigurationRequestDto request, CancellationToken cancellationToken = default);
    Task<Guid> CreateDraftAsync(InvoiceDraftDto draft, CancellationToken cancellationToken = default);
    Task<Guid> CreateAdjustmentDraftAsync(InvoiceAdjustmentDraftDto draft, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> QueueIssueAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> ProcessNextQueuedIssueAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateXmlAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfPreviewAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(string? status = null, int take = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceIssueFailureDto>> GetFailedIssueLogsAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<InvoiceIssueResultDto> CancelAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default);
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
