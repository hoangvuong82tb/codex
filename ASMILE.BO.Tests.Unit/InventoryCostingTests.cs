using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.App.Services;
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

    [Fact]
    public async Task FIFO_setting_is_applied_to_issue_cost_and_consumes_oldest_layers_first()
    {
        await using var db = TestDbContextFactory.CreateContext();
        SeedReceiptsAndIssue(db);
        var settings = new CapturingSettingsService("FIFO");
        var service = new InventoryCostingService(db, TestDbContextFactory.AuditLogService(db), settings);

        await service.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(1));
        await service.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(2));
        var issue = await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(3));

        issue.Success.Should().BeTrue(issue.ErrorsString());
        issue.Lines.Single().CostMethod.Should().Be(nameof(InventoryCostMethod.FIFO));
        issue.Lines.Single().TotalCost.Should().Be(40m); // 2 x 5 + 3 x 10
        db.InventoryCostTransactions.Single().CostMethod.Should().Be(nameof(InventoryCostMethod.FIFO));
        settings.Request.Should().Be(("Module.inventory.costMethod", SettingScope.App, "inventory"));
    }

    [Fact]
    public async Task Unsupported_persisted_cost_method_is_rejected_without_mutating_layers()
    {
        await using var db = TestDbContextFactory.CreateContext();
        SeedReceiptAndIssue(db, issueQuantity: 4m);
        await new InventoryCostingService(db, TestDbContextFactory.AuditLogService(db))
            .ApplyReceiptCostAsync(PostingSourceIds.FromInt32(1));
        var service = new InventoryCostingService(
            db,
            TestDbContextFactory.AuditLogService(db),
            new CapturingSettingsService("Đích danh"));

        var result = await service.ApplyIssueCostAsync(PostingSourceIds.FromInt32(2));

        result.Success.Should().BeFalse();
        db.InventoryCostLayers.Single().RemainingQuantity.Should().Be(10m);
        db.InventoryCostTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Inventory_cost_method_options_only_advertise_supported_methods()
    {
        var service = new ModuleSettingsService(null!, null!);
        var definition = service.GetDefinition("inventory")!;
        var setting = definition.Sections.SelectMany(x => x.Settings).Single(x => x.Key == "costMethod");

        setting.Options.Should().BeEquivalentTo("Bình quân tức thời", "FIFO");
        new ModuleSettingsValidationService()
            .Validate(definition, new Dictionary<string, string> { ["costMethod"] = "Đích danh" })
            .Should().ContainKey("costMethod");
    }

    private static void SeedReceiptsAndIssue(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
    {
        db.StockReceipts.AddRange(
            new StockReceipt
            {
                Id = 1, ReceiptNo = "PN-001", ReceiptDate = new DateTime(2026, 5, 2), SupplierName = "NCC",
                WarehouseId = 1, IsApproved = true,
                Details = [new StockReceiptDetail { Id = 11, ReceiptId = 1, LineNo = 1, ItemId = 100, WarehouseId = 1, Unit = "Cái", Quantity = 2m, UnitPrice = 5m, Amount = 10m }]
            },
            new StockReceipt
            {
                Id = 2, ReceiptNo = "PN-002", ReceiptDate = new DateTime(2026, 5, 2), SupplierName = "NCC",
                WarehouseId = 1, IsApproved = true,
                Details = [new StockReceiptDetail { Id = 12, ReceiptId = 2, LineNo = 1, ItemId = 100, WarehouseId = 1, Unit = "Cái", Quantity = 5m, UnitPrice = 10m, Amount = 50m }]
            });
        db.StockIssues.Add(new StockIssue
        {
            Id = 3, IssueNo = "PX-003", IssueDate = new DateTime(2026, 5, 3), WarehouseId = 1, IsApproved = true,
            Details = [new StockIssueDetail { Id = 13, IssueId = 3, LineNo = 1, ItemId = 100, WarehouseId = 1, Unit = "Cái", Quantity = 5m, UnitPrice = 0m, Amount = 0m }]
        });
        db.SaveChanges();
    }

    private sealed class CapturingSettingsService(string? costMethod) : ISettingsService
    {
        public (string Key, SettingScope Scope, string? Target)? Request { get; private set; }

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
        {
            Request = (key, scope, scopeTarget);
            return Task.FromResult<SettingValueDto?>(new SettingValueDto
            {
                Key = key,
                Value = costMethod,
                Scope = scope,
                ScopeTarget = scopeTarget
            });
        }

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget) => throw new NotImplementedException();
        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget) => throw new NotImplementedException();
        public Task SaveValueAsync(SaveSettingValueRequest request) => throw new NotImplementedException();
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => throw new NotImplementedException();
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => throw new NotImplementedException();
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
