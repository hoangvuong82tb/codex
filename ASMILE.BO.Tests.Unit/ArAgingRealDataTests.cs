using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ArAgingRealDataTests
{
    [Fact]
    public void Constructor_does_not_create_fake_rows()
    {
        var viewModel = new ArAgingViewModel(null);

        viewModel.AllItems.Should().BeEmpty();
        viewModel.FilteredItems.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_maps_real_summary_and_transactions()
    {
        var service = new FakeReceivableService
        {
            Summaries =
            [
                new ReceivableSummaryDto
                {
                    CustomerId = "C-001",
                    CustomerCode = "ACME",
                    CustomerName = "ACME Travel",
                    ClosingBalance = 1_000_000,
                    OverdueAmount = 600_000,
                    Status = DebtStatus.Overdue
                }
            ],
            Transactions =
            [
                new ReceivableTransactionDto
                {
                    CustomerId = "C-001",
                    CustomerCode = "ACME",
                    CustomerName = "ACME Travel",
                    DocumentNo = "INV-001",
                    TransactionDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(-10),
                    DebitAmount = 600_000,
                    BalanceAmount = 600_000,
                    Currency = "VND",
                    Status = DebtStatus.Overdue
                },
                new ReceivableTransactionDto
                {
                    CustomerId = "C-001",
                    CustomerCode = "ACME",
                    CustomerName = "ACME Travel",
                    DocumentNo = "INV-002",
                    TransactionDate = DateTime.Today.AddDays(-1),
                    DueDate = DateTime.Today.AddDays(2),
                    DebitAmount = 400_000,
                    BalanceAmount = 400_000,
                    Currency = "VND",
                    Status = DebtStatus.Open
                },
                new ReceivableTransactionDto
                {
                    CustomerId = "C-001",
                    CustomerCode = "ACME",
                    CustomerName = "ACME Travel",
                    DocumentNo = "RCPT-001",
                    TransactionDate = DateTime.Today.AddDays(-3),
                    CreditAmount = 200_000,
                    Currency = "VND",
                    Status = DebtStatus.Paid
                }
            ]
        };

        var viewModel = new ArAgingViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.AllItems.Should().ContainSingle();
        var item = viewModel.AllItems.Single();
        item.PartnerCode.Should().Be("ACME");
        item.OutstandingAmount.Should().Be(1_000_000);
        item.CurrentAmount.Should().Be(400_000);
        item.Overdue1To30.Should().Be(600_000);
        item.Invoices.Should().HaveCount(2);
        item.Payments.Should().ContainSingle(x => x.Amount == 200_000);
        item.Timeline.Should().HaveCount(3);
        item.AgingStatus.Should().Be("Overdue");
        viewModel.ErrorMessage.Should().BeNull();
        service.LastFilter.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeAsync_fails_closed_when_service_is_missing()
    {
        var viewModel = new ArAgingViewModel(null);

        await viewModel.InitializeAsync();

        viewModel.AllItems.Should().BeEmpty();
        viewModel.FilteredItems.Should().BeEmpty();
        viewModel.ErrorMessage.Should().Contain("không dùng dữ liệu mẫu");
    }

    private sealed class FakeReceivableService : IReceivableService
    {
        public IReadOnlyList<ReceivableSummaryDto> Summaries { get; init; } = [];
        public IReadOnlyList<ReceivableTransactionDto> Transactions { get; init; } = [];
        public ReceivableFilterDto? LastFilter { get; private set; }

        public Task<IReadOnlyList<ReceivableSummaryDto>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Summaries);
        public Task<IReadOnlyList<string>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyList<ReceivableSummaryDto>> GetDebtSummariesAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult(Summaries);
        }
        public Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default) => Task.FromResult(Transactions);
        public Task<IReadOnlyList<ReceivableTransactionDto>> GetTransactionsByCustomerAsync(string customerId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) => Task.FromResult(Transactions);
        public Task<string> GetNextReceiptNoAsync(CancellationToken cancellationToken = default) => Task.FromResult("RCPT-TEST");
        public Task<string> CreatePaymentReceiptAsync(CreatePaymentReceiptRequest request, CancellationToken cancellationToken = default) => Task.FromResult("RCPT-TEST");
        public Task<IReadOnlyList<PaymentReceiptDto>> GetPaymentReceiptsAsync(ReceivableFilterDto filter, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PaymentReceiptDto>>([]);
        public Task<bool> ApprovePaymentReceiptAsync(string receiptId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CancelPaymentReceiptAsync(string receiptId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ReopenPaymentReceiptAsync(string receiptId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CloseReceivableAsync(string customerId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> AddReceivableNoteAsync(string customerId, string noteType, string content, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IReadOnlyList<ReceivableSummaryDto>> GetOverdueDebtsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Summaries);
        public Task PrintDebtStatementAsync(string customerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportDebtStatementAsync(string customerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
