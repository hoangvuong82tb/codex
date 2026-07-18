using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IMenuCatalogService
{
    Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<MenuCategoryDto> CreateOrUpdateCategoryAsync(CreateMenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItemDto>> GetMenuItemsAsync(
        Guid? categoryId = null,
        string? keyword = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
    Task<MenuItemDto?> GetMenuItemDetailAsync(Guid menuItemId, CancellationToken cancellationToken = default);
    Task<MenuItemDto> CreateOrUpdateMenuItemAsync(CreateMenuItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteOrDeactivateCategoryAsync(Guid categoryId, bool softDeleteOnly = true, CancellationToken cancellationToken = default);
    Task DeleteOrDeactivateMenuItemAsync(Guid menuItemId, bool softDeleteOnly = true, CancellationToken cancellationToken = default);
    Task<IngredientsLookupDto> GetIngredientsLookupAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
}
