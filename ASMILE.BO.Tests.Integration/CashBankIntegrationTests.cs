using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Integration.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Integration;

public sealed class CashBankIntegrationTests
{
    [Fact]
    public async Task CashReceiptAndPayment_UpdateCashBookBalance()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new CashFundService(db, TestDbContextFactory.JournalService(db), TestDbContextFactory.PeriodService(db), TestDbContextFactory.RuleService(db), TestDbContextFactory.AuditLogService(db));
        var fund = await service.CreateFundAsync(new CashFundRequest { FundCode = "QTM", FundName = "Quỹ tiền mặt", AccountCode = "1111", OpeningBalance = 0m, IsActive = true, IsDefault = true });
        var receipt = await service.CreateReceiptAsync(new CashTransactionRequest { FundId = fund.Id, TransactionDate = new DateTime(2026, 5, 4), Amount = 100m, CounterAccountCode = "421", Description = "Thu test" });
        await service.PostTransactionAsync(receipt.Id, "tester");
        var payment = await service.CreatePaymentAsync(new CashTransactionRequest { FundId = fund.Id, TransactionDate = new DateTime(2026, 5, 5), Amount = 40m, CounterAccountCode = "642", Description = "Chi test" });
        await service.PostTransactionAsync(payment.Id, "tester");

        var book = await service.GetCashBookAsync(fund.Id, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        book.Should().HaveCount(2);
        book.Last().Balance.Should().Be(60m);
    }

    [Fact]
    public async Task BankReceiptPaymentAndReconciliation_WorkTogether()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BankingService(db, TestDbContextFactory.JournalService(db), TestDbContextFactory.PeriodService(db), TestDbContextFactory.RuleService(db), TestDbContextFactory.AuditLogService(db));
        var account = await service.CreateAccountAsync(new BankAccountRequest { BankCode = "VCB", BankName = "Vietcombank", AccountNumber = "123", AccountName = "ASMILE", AccountCode = "1121", IsActive = true, IsDefault = true });
        var receipt = await service.CreateReceiptAsync(new BankTransactionRequest { BankAccountId = account.Id, TransactionDate = new DateTime(2026, 5, 4), Amount = 200m, CounterAccountCode = "421", ReferenceNo = "BC001" });
        await service.PostTransactionAsync(receipt.Id, "tester");
        var payment = await service.CreatePaymentAsync(new BankTransactionRequest { BankAccountId = account.Id, TransactionDate = new DateTime(2026, 5, 5), Amount = 50m, CounterAccountCode = "642", ReferenceNo = "BN001" });
        await service.PostTransactionAsync(payment.Id, "tester");

        var book = await service.GetBankBookAsync(account.Id, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        book.Should().HaveCount(2);
        book.Last().Balance.Should().Be(150m);
    }
}
