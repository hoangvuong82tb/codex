# Breakfast Cafe Fix 2026-07-01

- Fixed startup schema sync for `IsVatIncluded`.
- Added automatic column backfill for `BreakfastCafeOrderLines.IsVatIncluded` in `ASMILE.BO.Infrastructure/Persistence/DatabaseMigrationService.cs`.
- Verified `MenuItems.IsVatIncluded` exists in current database.
- Applied schema backfill to current `ASMILE_BO` database so Breakfast/Cafe screens can query open orders and closed-order details again.
- Fixed receipt/provisional print snapshot logic in `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs` to avoid enumerating a live `ObservableCollection` while UI data is changing.
- Added fallback inventory cost-layer creation in `ASMILE.BO.Infrastructure/Services/BreakfastCafeCostingService.cs` so recipe consumption can post when stock balance exists but `InventoryCostLayers` are missing.
- Rebuilt solution successfully and reopened app for testing.
