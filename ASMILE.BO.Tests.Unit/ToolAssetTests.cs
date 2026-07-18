using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ToolAssetTests
{
    [Fact]
    public async Task BuildAllocationSchedule_CreatesStraightLineSchedule()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, out var categoryId);
        var tool = await service.CreateToolAsync(new CreateToolAssetRequest
        {
            ToolCode = "CCDC001", ToolName = "Công cụ test", CategoryId = categoryId,
            PurchaseDate = new DateTime(2026, 5, 1), OriginalCost = 600m, Quantity = 1m, AllocationMonths = 6
        });
        await service.IssueToolAsync(new IssueToolRequest { ToolAssetId = tool.Id, IssueDate = new DateTime(2026, 5, 1), Quantity = 1m, PostedBy = "tester" });

        var schedule = await service.BuildAllocationScheduleAsync(tool.Id);

        schedule.Should().HaveCount(6);
        schedule.Sum(x => x.AllocationAmount).Should().Be(600m);
    }

    [Fact]
    public async Task RunAllocationAsync_ReducesRemainingAmountToZeroAfterFullAllocation()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, out var categoryId);
        var tool = await service.CreateToolAsync(new CreateToolAssetRequest
        {
            ToolCode = "CCDC002", ToolName = "Công cụ test", CategoryId = categoryId,
            PurchaseDate = new DateTime(2026, 5, 1), OriginalCost = 200m, Quantity = 1m, AllocationMonths = 2
        });
        await service.IssueToolAsync(new IssueToolRequest { ToolAssetId = tool.Id, IssueDate = new DateTime(2026, 5, 1), Quantity = 1m, PostedBy = "tester" });
        await service.BuildAllocationScheduleAsync(tool.Id);
        await service.RunAllocationAsync(new AllocationRunRequest { AllocationPeriod = "2026-05", PostedBy = "tester" });
        await service.RunAllocationAsync(new AllocationRunRequest { AllocationPeriod = "2026-06", PostedBy = "tester" });

        var stored = db.ToolAssets.Single(x => x.Id == tool.Id);
        stored.RemainingAmount.Should().Be(0m);
        stored.Status.Should().Be("FullyAllocated");
    }

    private static ToolAssetService CreateService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, out Guid categoryId)
    {
        var category = new ToolAssetCategory
        {
            Id = Guid.NewGuid(), CategoryCode = "CCDC", CategoryName = "Công cụ", DefaultAllocationMonths = 6,
            ToolAccountCode = "153", PrepaidExpenseAccountCode = "242", ExpenseAccountCode = "642",
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.ToolAssetCategories.Add(category);
        db.SaveChanges();
        categoryId = category.Id;
        return new ToolAssetService(db, TestDbContextFactory.JournalService(db), TestDbContextFactory.PeriodService(db), TestDbContextFactory.RuleService(db), TestDbContextFactory.AuditLogService(db));
    }
}
