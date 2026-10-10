using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IPayableService
{
    Task<IReadOnlyList<PayableSummaryDto>> GetDebtSummariesAsync(PayableFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayableTransactionDto>> GetTransactionsBySupplierAsync(string supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<string> GetNextPaymentVoucherNoAsync(CancellationToken cancellationToken = default);
    Task<string> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default);
    Task<bool?> GetPaymentVoucherApprovedAsync(string documentNo, CancellationToken cancellationToken = default);
    Task SetPaymentVoucherApprovedAsync(string documentNo, bool approved, CancellationToken cancellationToken = default);
    Task<bool> CancelPaymentVoucherAsync(string documentNo, CancellationToken cancellationToken = default);
    Task<bool> RejectPaymentVoucherAsync(string documentNo, string reason, CancellationToken cancellationToken = default);
    Task CreateAdvancePaymentAsync(CreateAdvancePaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default);
    Task PrintDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default);
    Task ExportDebtStatementAsync(string supplierId, CancellationToken cancellationToken = default);
}
