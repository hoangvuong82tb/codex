using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class PayablePaymentPersistenceTests
{
    [Fact]
    public async Task Payment_voucher_persists_real_payable_transaction_and_approval_state()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Suppliers.Add(new Supplier
        {
            Id = 201,
            SupplierCode = "NCC-201",
            SupplierName = "Nhà cung cấp kiểm thử",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfPayableService(db, TestDbContextFactory.AuditLogService(db));
        var voucherNo = await service.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest
        {
            SupplierId = "201",
            PaymentDate = DateTime.Today,
            Amount = 2_500_000,
            PaymentMethod = "Chuyển khoản",
            Note = "Test payable persistence"
        });

        voucherNo.Should().NotBeNullOrWhiteSpace();
        var document = db.BusinessDocuments.Single(x => x.Code == voucherNo);
        document.DocumentType.Should().Be(BusinessDocumentType.PaymentVoucher);
        document.PaymentMethod.Should().Be("Chuyển khoản");
        document.Status.Should().Be(DocumentStatus.Draft);

        var transaction = db.PayableTransactions.Single(x => x.SourceId == document.Id);
        transaction.SupplierId.Should().Be(PostingSourceIds.FromInt32(201));
        transaction.SourceType.Should().Be("PaymentVoucher");
        transaction.DebitAmount.Should().Be(2_500_000);
        transaction.CreditAmount.Should().Be(0);
        transaction.Status.Should().Be("Pending");

        await service.SetPaymentVoucherApprovedAsync(voucherNo, approved: true);

        db.BusinessDocuments.Single(x => x.Id == document.Id).Status.Should().Be(DocumentStatus.Active);
        db.PayableTransactions.Single(x => x.Id == transaction.Id).Status.Should().Be("Paid");
    }

    [Fact]
    public async Task Pending_payment_voucher_can_be_cancelled_in_database_with_audit_state()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Suppliers.Add(new Supplier
        {
            Id = 202,
            SupplierCode = "NCC-202",
            SupplierName = "Nhà cung cấp hủy kiểm thử",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfPayableService(db, TestDbContextFactory.AuditLogService(db));
        var voucherNo = await service.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest
        {
            SupplierId = "202",
            PaymentDate = DateTime.Today,
            Amount = 750_000,
            PaymentMethod = "Tiền mặt",
            Note = "Test payable cancellation"
        });

        var cancelled = await service.CancelPaymentVoucherAsync(voucherNo);

        cancelled.Should().BeTrue();
        db.BusinessDocuments.Single(x => x.Code == voucherNo).Status.Should().Be(DocumentStatus.Cancelled);
        db.PayableTransactions.Single(x => x.DocumentNo == voucherNo).Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Pending_payment_voucher_can_be_rejected_with_reason_without_affecting_payable_balance()
    {
        await using var db = TestDbContextFactory.CreateContext(null, null, seedAccountingCore: false);
        db.Suppliers.Add(new Supplier
        {
            Id = 203,
            SupplierCode = "NCC-203",
            SupplierName = "Nhà cung cấp từ chối kiểm thử",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new EfPayableService(db, TestDbContextFactory.AuditLogService(db));
        var voucherNo = await service.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest
        {
            SupplierId = "203",
            PaymentDate = DateTime.Today,
            Amount = 1_250_000,
            PaymentMethod = "Chuyển khoản",
            Note = "Test payable rejection"
        });

        var rejected = await service.RejectPaymentVoucherAsync(voucherNo, "Thiếu chứng từ gốc");

        rejected.Should().BeTrue();
        db.BusinessDocuments.Single(x => x.Code == voucherNo).Status.Should().Be(DocumentStatus.Rejected);
        db.BusinessDocuments.Single(x => x.Code == voucherNo).Title.Should().Contain("Thiếu chứng từ gốc");
        db.PayableTransactions.Single(x => x.DocumentNo == voucherNo).Status.Should().Be("Rejected");
        db.PayableTransactions.Single(x => x.DocumentNo == voucherNo).Description.Should().Contain("Thiếu chứng từ gốc");
    }
}
