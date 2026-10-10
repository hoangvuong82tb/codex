using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReceivablePaymentAllocationTests
{
    [Fact]
    public async Task Create_payment_receipt_always_creates_a_real_receivable_credit_row()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 101,
            CustomerCode = "KH-101",
            CustomerName = "Khách kiểm thử",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfReceivableService(db, TestDbContextFactory.AuditLogService(db));
        var receiptNo = await service.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = "101",
            Amount = 1_250_000,
            ReceiptDate = DateTime.Today,
            PaymentMethod = "Tiền mặt",
            Note = "Test receipt"
        });

        receiptNo.Should().NotBeNullOrWhiteSpace();
        var transaction = db.ReceivableTransactions.Single();
        transaction.DocumentNo.Should().Be(receiptNo);
        transaction.SourceType.Should().Be("PaymentReceipt");
        transaction.CreditAmount.Should().Be(1_250_000);
        transaction.BalanceAmount.Should().Be(-1_250_000);
        db.BusinessDocuments.Single(x => x.Code == receiptNo).DocumentType.Should().Be(BusinessDocumentType.PaymentReceipt);
    }

    [Fact]
    public async Task Allocate_payment_rejects_over_allocation_and_wrong_customer()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var customer = Guid.NewGuid();
        var otherCustomer = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var otherInvoiceId = Guid.NewGuid();
        db.ReceivableTransactions.AddRange(
            NewTransaction(paymentId, customer, credit: 1_000_000, balance: -1_000_000, documentNo: "PT-001"),
            NewTransaction(invoiceId, customer, debit: 800_000, balance: 800_000, documentNo: "INV-001"),
            NewTransaction(otherInvoiceId, otherCustomer, debit: 100_000, balance: 100_000, documentNo: "INV-002"));
        await db.SaveChangesAsync();

        var service = new DebtTransactionService(db);
        var act = () => service.AllocatePaymentAsync(
            "Receivable", paymentId, null, "Receivable", invoiceId, 800_000, DateTime.UtcNow, "test");

        await act.Should().NotThrowAsync();
        db.PaymentAllocations.Should().ContainSingle(x => x.PaymentSourceId == paymentId && x.TargetTransactionId == invoiceId);
        db.ReceivableTransactions.Single(x => x.Id == invoiceId).BalanceAmount.Should().Be(0);

        var overAllocation = () => service.AllocatePaymentAsync(
            "Receivable", paymentId, null, "Receivable", invoiceId, 1, DateTime.UtcNow, "test");
        await overAllocation.Should().ThrowAsync<InvalidOperationException>().WithMessage("*số dư công nợ*");

        var wrongCustomer = () => service.AllocatePaymentAsync(
            "Receivable", paymentId, null, "Receivable", otherInvoiceId, 1, DateTime.UtcNow, "test");
        await wrongCustomer.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cùng khách hàng*");
        db.PaymentAllocations.Should().ContainSingle();
    }

    [Fact]
    public async Task Cancel_payment_receipt_accepts_transaction_id_and_marks_real_receipt_reversed()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 102,
            CustomerCode = "KH-102",
            CustomerName = "Khách đảo ngược",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfReceivableService(db, TestDbContextFactory.AuditLogService(db));
        var receiptNo = await service.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = "102",
            Amount = 750_000,
            ReceiptDate = DateTime.Today,
            Note = "Receipt reversal test"
        });
        var transaction = db.ReceivableTransactions.Single(x => x.DocumentNo == receiptNo);

        var cancelled = await service.CancelPaymentReceiptAsync(transaction.Id.ToString("N"));

        cancelled.Should().BeTrue();
        db.BusinessDocuments.Single(x => x.Code == receiptNo).Status.Should().Be(DocumentStatus.Cancelled);
        transaction.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Get_payment_receipts_keeps_cancelled_documents_for_audit_history()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 103,
            CustomerCode = "KH-103",
            CustomerName = "Khách lưu lịch sử",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfReceivableService(db, TestDbContextFactory.AuditLogService(db));
        var receiptNo = await service.CreatePaymentReceiptAsync(new CreatePaymentReceiptRequest
        {
            CustomerId = "103",
            Amount = 500_000,
            ReceiptDate = DateTime.Today,
            Note = "Cancelled receipt audit test"
        });
        var transaction = db.ReceivableTransactions.Single(x => x.DocumentNo == receiptNo);

        (await service.CancelPaymentReceiptAsync(transaction.Id.ToString("N"))).Should().BeTrue();

        var receipts = await service.GetPaymentReceiptsAsync(new ReceivableFilterDto
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today
        });

        receipts.Should().ContainSingle(x => x.DocumentNo == receiptNo && x.Status == "Đã hủy");
    }

    [Fact]
    public async Task Cancel_payment_receipt_rejects_receipt_with_real_allocations()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var customerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        db.BusinessDocuments.Add(new BusinessDocument
        {
            Id = documentId,
            Code = "PT-ALLOC-001",
            Title = "Allocated receipt",
            EntityType = BusinessEntityType.Customer,
            EntityId = customerId,
            DocumentType = BusinessDocumentType.PaymentVoucher,
            DocumentDate = DateTime.Today,
            Amount = 1_000_000,
            Status = DocumentStatus.Locked
        });
        db.ReceivableTransactions.Add(new ReceivableTransaction
        {
            Id = paymentId,
            CustomerId = customerId,
            SourceType = "PaymentReceipt",
            SourceId = documentId,
            DocumentNo = "PT-ALLOC-001",
            DocumentDate = DateTime.Today,
            CreditAmount = 1_000_000,
            BalanceAmount = -1_000_000,
            CurrencyCode = "VND",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.PaymentAllocations.Add(new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentSourceType = "Receivable",
            PaymentSourceId = paymentId,
            TargetType = "Receivable",
            TargetTransactionId = invoiceId,
            AllocatedAmount = 500_000,
            AllocationDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new EfReceivableService(db, TestDbContextFactory.AuditLogService(db));
        var cancelled = await service.CancelPaymentReceiptAsync(paymentId.ToString("N"));

        cancelled.Should().BeFalse();
        db.BusinessDocuments.Single(x => x.Id == documentId).Status.Should().Be(DocumentStatus.Locked);
        db.ReceivableTransactions.Single(x => x.Id == paymentId).Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Undo_payment_allocations_restores_target_balance_and_removes_real_rows()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var customer = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        db.ReceivableTransactions.AddRange(
            NewTransaction(paymentId, customer, credit: 1_000_000, balance: -1_000_000, documentNo: "PT-UNDO"),
            NewTransaction(invoiceId, customer, debit: 1_000_000, balance: 500_000, documentNo: "INV-UNDO"));
        db.PaymentAllocations.Add(new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentSourceType = "Receivable",
            PaymentSourceId = paymentId,
            TargetType = "Receivable",
            TargetTransactionId = invoiceId,
            AllocatedAmount = 500_000,
            AllocationDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DebtTransactionService(db);
        var removed = await service.UndoPaymentAllocationsAsync(paymentId, "test");

        removed.Should().Be(1);
        db.PaymentAllocations.Should().BeEmpty();
        db.ReceivableTransactions.Single(x => x.Id == invoiceId).BalanceAmount.Should().Be(1_000_000);
        db.ReceivableTransactions.Single(x => x.Id == invoiceId).Status.Should().Be("Open");
    }

    [Fact]
    public async Task Auto_allocate_payment_uses_oldest_same_customer_debts_first()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        var customer = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var oldestInvoiceId = Guid.NewGuid();
        var newestInvoiceId = Guid.NewGuid();
        var oldestInvoice = NewTransaction(oldestInvoiceId, customer, debit: 800_000, balance: 800_000, documentNo: "INV-OLD");
        oldestInvoice.DocumentDate = DateTime.Today.AddDays(-5);
        var newestInvoice = NewTransaction(newestInvoiceId, customer, debit: 800_000, balance: 800_000, documentNo: "INV-NEW");
        newestInvoice.DocumentDate = DateTime.Today;
        db.ReceivableTransactions.AddRange(
            NewTransaction(paymentId, customer, credit: 1_200_000, balance: -1_200_000, documentNo: "PT-AUTO"),
            oldestInvoice,
            newestInvoice);
        await db.SaveChangesAsync();

        var service = new DebtTransactionService(db);
        var count = await service.AutoAllocatePaymentAsync(paymentId, "test");

        count.Should().Be(2);
        db.PaymentAllocations.Should().HaveCount(2);
        db.ReceivableTransactions.Single(x => x.Id == oldestInvoiceId).BalanceAmount.Should().Be(0);
        db.ReceivableTransactions.Single(x => x.Id == newestInvoiceId).BalanceAmount.Should().Be(400_000);
    }

    private static ReceivableTransaction NewTransaction(Guid id, Guid customerId, decimal debit = 0, decimal credit = 0, decimal balance = 0, string documentNo = "")
        => new()
        {
            Id = id,
            CustomerId = customerId,
            SourceType = "Test",
            DocumentNo = documentNo,
            DocumentDate = DateTime.Today,
            DebitAmount = debit,
            CreditAmount = credit,
            BalanceAmount = balance,
            CurrencyCode = "VND",
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
