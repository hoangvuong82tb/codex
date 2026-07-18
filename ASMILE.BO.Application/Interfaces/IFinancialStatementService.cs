using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IFinancialStatementService
{
    Task<IReadOnlyList<AccountBalanceSnapshotDto>> BuildSnapshotsAsync(string period, bool rebuild = true, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<BalanceSheetDto> GenerateBalanceSheetAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default);
    Task<IncomeStatementDto> GenerateIncomeStatementAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default);
    Task<CashFlowDto> GenerateCashFlowAsync(string period, string? generatedBy = null, CancellationToken cancellationToken = default);
    Task<FinancialStatementSummaryDto> GetSummaryAsync(string period, CancellationToken cancellationToken = default);
}
