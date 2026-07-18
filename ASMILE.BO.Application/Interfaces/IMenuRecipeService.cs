using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IMenuRecipeService
{
    Task<IReadOnlyList<MenuRecipeDto>> GetRecipesByMenuItemAsync(Guid menuItemId, CancellationToken cancellationToken = default);
    Task<MenuRecipeDto?> GetRecipeAsync(Guid menuItemId, CancellationToken cancellationToken = default);
    Task<MenuRecipeDto> SaveRecipeAsync(SaveMenuRecipeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateRecipeAsync(Guid menuItemId, CancellationToken cancellationToken = default);
    Task<MenuCostPreviewDto> PreviewCostAsync(Guid menuItemId, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubstituteIngredientDto>> GetSubstitutesAsync(Guid recipeLineId, CancellationToken cancellationToken = default);
    Task<SubstituteIngredientDto> CreateSubstituteAsync(CreateSubstituteIngredientRequest request, CancellationToken cancellationToken = default);
    Task<SubstituteIngredientDto> UpdateSubstituteAsync(UpdateSubstituteIngredientRequest request, CancellationToken cancellationToken = default);
    Task DeleteSubstituteAsync(Guid substituteId, string? userName = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateSubstituteAsync(CreateSubstituteIngredientRequest request, CancellationToken cancellationToken = default);
}
