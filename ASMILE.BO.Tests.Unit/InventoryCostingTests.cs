using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class InventoryCostingTests
{
    [Fact]
    public async Task MovingAverage_AppliesReceiptAndIssueCost()
    {
        await using var db = TestDbContextFactory.CreateContext();
        SeedReceiptAndIssue(db, issueQuantity: 4m);
        var service = new InventoryCostingService(db, TestDbContextFactory.AuditLogService(db));

        var receipt = await service.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(1));
        var issue = await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(2));

        receipt.Success.Should().BeTrue(receipt.ErrorsString());
        issue.Success.Should().BeTrue(issue.ErrorsString());
        db.InventoryCostLayers.Single().RemainingQuantity.Should().Be(6m);
        db.InventoryCostTransactions.Single().TotalCost.Should().Be(20m);
    }

    [Fact]
    public async Task ApplyIssueCostAsync_DoesNotCreateDuplicateCostTransaction()
    {
        await using var db = TestDbContextFactory.CreateContext();
        SeedReceiptAndIssue(db, issueQuantity: 4m);
        var service = new InventoryCostingService(db, TestDbContextFactory.AuditLogService(db));
        await service.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(1));
        await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(2));

        var second = await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(2));

        second.Success.Should().BeTrue(second.ErrorsString());
        db.InventoryCostTransactions.Count().Should().Be(1);
    }

    [Fact]
    public async Task ApplyIssueCostAsync_RejectsInsufficientCostQuantity()
    {
        await using var db = TestDbContextFactory.CreateContext();
        SeedReceiptAndIssue(db, issueQuantity: 12m);
        var service = new InventoryCostingService(db, TestDbContextFactory.AuditLogService(db));
        await service.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(1));

        var issue = await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(2));

        issue.Success.Should().BeFalse();
        issue.Errors.Should().NotBeEmpty();
    }

    private static void SeedReceiptAndIssue(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, decimal issueQuantity)
    {
        db.StockReceipts.Add(new StockReceipt
        {
            Id = 1,
            ReceiptNo = "PN-001",
            ReceiptDate = new DateTime(2026, 5, 2),
            SupplierName = "NCC",
            WarehouseId = 1,
            TotalAmount = 50m,
            IsApproved = true,
            Details =
            [
                new StockReceiptDetail { Id = 11, ReceiptId = 1, LineNo = 1, ItemId = 100, WarehouseId = 1, Unit = "Cái", Quantity = 10m, UnitPrice = 5m, Amount = 50m }
            ]
        });
        db.StockIssues.Add(new StockIssue
        {
            Id = 2,
            IssueNo = "PX-001",
            IssueDate = new DateTime(2026, 5, 3),
            WarehouseId = 1,
            TotalAmount = issueQuantity * 5m,
            IsApproved = true,
            Details =
            [
                new StockIssueDetail { Id = 21, IssueId = 2, LineNo = 1, ItemId = 100, WarehouseId = 1, Unit = "Cái", Quantity = issueQuantity, UnitPrice = 5m, Amount = issueQuantity * 5m }
            ]
        });
        db.SaveChanges();
    }
}

file static class InventoryCostResultExtensions
{
    public static string ErrorsString(this ASMILE.BO.Application.DTOs.InventoryCostResult result) => string.Join("; ", result.Errors);
}
