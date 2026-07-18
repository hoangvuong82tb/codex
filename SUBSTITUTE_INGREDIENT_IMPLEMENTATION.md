# Substitute Ingredient Implementation

## Files Added

- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260615090000_AddRecipeIngredientSubstitutes.cs`
- `SUBSTITUTE_INGREDIENT_IMPLEMENTATION.md`

## Files Modified

- `ASMILE.BO.Core/Entities/BreakfastCafeSalesEntities.cs`
- `ASMILE.BO.Application/DTOs/BreakfastCafeSalesDtos.cs`
- `ASMILE.BO.Application/Interfaces/IMenuRecipeService.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/MenuRecipeService.cs`
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/MenuRecipeTab.xaml`
- `ASMILE.BO.Tests.Unit/MenuRecipeModuleTests.cs`

## Database Changes

- Added table `RecipeIngredientSubstitutes`.
- Added columns:
  - `Id`
  - `CompanyId`
  - `BranchId`
  - `RecipeLineId`
  - `InventoryItemId`
  - `SubstituteInventoryItemId`
  - `ConversionRate`
  - `Priority`
  - `AutoReplace`
  - `Notes`
  - `CreatedAt`
  - `CreatedBy`
  - `ModifiedAt`
  - `ModifiedBy`
  - `IsDeleted`
- Added indexes:
  - `IX_RecipeIngredientSubstitutes_RecipeLineId`
  - `IX_RecipeIngredientSubstitutes_InventoryItemId`
  - `IX_RecipeIngredientSubstitutes_SubstituteInventoryItemId`
  - `IX_RecipeIngredientSubstitutes_RecipeLine_Substitute_IsDeleted`
- Added FK relationships:
  - `RecipeLineId` -> `MenuItemRecipeLines.Id`
  - `InventoryItemId` -> `Items.Id`
  - `SubstituteInventoryItemId` -> `Items.Id`

## Migration Name

- `20260615090000_AddRecipeIngredientSubstitutes`

## Commands Added

- `GetSubstitutesAsync`
- `CreateSubstituteAsync`
- `UpdateSubstituteAsync`
- `DeleteSubstituteAsync`
- `ValidateSubstituteAsync`
- `LoadSubstitutesCommand`
- `AddSubstituteCommand`
- `DeleteSubstituteCommand`
- `SaveSubstituteCommand`

## ViewModel Changes

- Added `SelectedRecipeLine`.
- Added `SelectedSubstituteIngredient`.
- Added `SubstituteIngredients`.
- Added `AvailableSubstituteItems`.
- Added `SelectedSubstituteItem`.
- Added `SubstituteConversionRate`.
- Added `SubstitutePriority`.
- Added `SubstituteAutoReplace`.
- Added `SubstituteNotes`.
- Added `SubstituteSearchText`.
- Auto-loads substitutes when `SelectedRecipeLine` changes.
- Filters substitute lookup by search text, excludes the main ingredient, and excludes existing substitutes when adding.

## Validation Rules

- Cannot select the main ingredient as its own substitute.
- Cannot add duplicate active substitutes for the same recipe line.
- One main ingredient can have multiple substitutes.
- One substitute ingredient can be reused by multiple recipe lines.
- `ConversionRate` must be greater than 0.
- `Priority` must be greater than or equal to 1.
- Delete substitute is soft delete by setting `IsDeleted = true`.

## Build Result

- Command: `dotnet build ASMILE.BO.sln -c Release --no-restore`
- Result: Passed.
- Errors: 0.
- Warnings: 2 existing nullable warnings in `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`, not introduced by this change.
- XAML encoding check: Passed.

## Test Result

- Command: `dotnet test ASMILE.BO.sln -c Release --no-build`
- Result: Passed.
- Integration tests: 11 passed.
- Unit tests: 138 passed.
- Added test: `CreateSubstituteAsync_ShouldRejectSelfAndDuplicate_ThenAllowAfterSoftDelete`.

## Migration Result

- Command: `dotnet ef database update --project ASMILE.BO.Infrastructure --startup-project ASMILE.BO.App --configuration Release --no-build`
- Result: Applied migration `20260615090000_AddRecipeIngredientSubstitutes`.
- Note: EF reported existing design-time DI validation warnings, then continued without the application service provider and applied the migration successfully.
