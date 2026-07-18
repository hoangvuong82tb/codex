using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class FixedAssetTests
{
    [Fact]
    public async Task StraightLineDepreciation_CalculatesMonthlyAmount()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var category = new FixedAssetCategory
        {
            Id = Guid.NewGuid(), CategoryCode = "MAY", CategoryName = "Máy móc", UsefulLifeMonthsDefault = 10,
            DepreciationMethodDefault = "StraightLine", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.FixedAssetCategories.Add(category);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        await service.CreateAssetAsync(new CreateFixedAssetRequest
        {
            AssetCode = "TS001", AssetName = "Tài sản test", CategoryId = category.Id,
            PurchaseDate = new DateTime(2026, 5, 1), StartDepreciationDate = new DateTime(2026, 5, 1),
            OriginalCost = 1200m, SalvageValue = 0m, UsefulLifeMonths = 12, ActivateImmediately = true, CounterAccountCode = "3311"
        });

        var depreciation = await service.RunDepreciationAsync(new DepreciationRunRequest { PeriodCode = "2026-05" });

        depreciation.Should().ContainSingle();
        depreciation.Single().DepreciationAmount.Should().Be(100m);
    }

    [Fact]
    public async Task RunDepreciationAsync_DoesNotDuplicatePeriod()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var category = new FixedAssetCategory
        {
            Id = Guid.NewGuid(), CategoryCode = "MAY", CategoryName = "Máy móc", UsefulLifeMonthsDefault = 12,
            DepreciationMethodDefault = "StraightLine", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.FixedAssetCategories.Add(category);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        await service.CreateAssetAsync(new CreateFixedAssetRequest
        {
            AssetCode = "TS002", AssetName = "Tài sản test", CategoryId = category.Id,
            PurchaseDate = new DateTime(2026, 5, 1), StartDepreciationDate = new DateTime(2026, 5, 1),
            OriginalCost = 1200m, SalvageValue = 0m, UsefulLifeMonths = 12, ActivateImmediately = true, CounterAccountCode = "3311"
        });
        await service.RunDepreciationAsync(new DepreciationRunRequest { PeriodCode = "2026-05" });

        var second = await service.RunDepreciationAsync(new DepreciationRunRequest { PeriodCode = "2026-05" });

        second.Should().BeEmpty();
    }

    private static FixedAssetService CreateService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
        => new(db, TestDbContextFactory.JournalService(db), TestDbContextFactory.PeriodService(db), TestDbContextFactory.RuleService(db), TestDbContextFactory.AuditLogService(db));
}
