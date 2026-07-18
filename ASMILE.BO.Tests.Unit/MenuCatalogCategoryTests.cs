using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class MenuCatalogCategoryTests
{
    [Fact]
    public async Task CreateOrUpdateCategoryAsync_ShouldCreateMenuCategory()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);

        var category = await catalog.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
        {
            Code = "mon-chay",
            Name = "Món chay",
            SortOrder = 10
        });

        category.Code.Should().Be("mon-chay");
        category.Name.Should().Be("Món chay");
        category.SortOrder.Should().Be(10);
        (await db.MenuCategories.CountAsync(x => x.Code == "mon-chay")).Should().Be(1);
    }

    [Fact]
    public async Task CreateOrUpdateCategoryAsync_ShouldRejectDuplicateCode()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);

        await catalog.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
        {
            Code = "do-uong",
            Name = "Đồ uống",
            SortOrder = 1
        });

        var act = () => catalog.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
        {
            Code = "do-uong",
            Name = "Đồ uống khác",
            SortOrder = 2
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã tồn tại*");
    }
}
