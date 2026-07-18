using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IReceivableService
{
    Task<IReadOnlyList<ReceivableSummaryDto>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableSummaryDto>> GetDebtSummariesAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsByCustomerAsync(string customerId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<string> GetNextReceiptNoAsync(CancellationToken cancellationToken = default);
    Task<string> CreatePaymentReceiptAsync(CreatePaymentReceiptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default);
    Task PrintDebtStatementAsync(string customerId, CancellationToken cancellationToken = default);
    Task ExportDebtStatementAsync(string customerId, CancellationToken cancellationToken = default);
}
