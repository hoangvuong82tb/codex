# BANQUET — RESTORE FROM BACKUP

**Ngày:** 2026-07-21
**Loại:** RESTORE — FEATURE FREEZE
**Trạng thái:** COMPLETED ✅

---

## 1. Files Restored from Backup

| File | Action | Reason |
|------|--------|--------|
| `Core/Entities/RestaurantEntities.cs` | Restored | Removed ContractVersion, freeze fields |
| `Application/DTOs/RestaurantDtos.cs` | Restored | Removed freeze DTOs, ContractVersionDto |
| `Infrastructure/Services/RestaurantServices.cs` | Restored | Removed freeze/unfreeze, version logic, SyncContracts guard |
| `Application/Interfaces/IRestaurantServices.cs` | Restored | Removed freeze method signatures |
| `App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | Restored | Reverted to stable UI |
| `App/ViewModels/Banquet/BanquetViewModels.cs` | Restored | Reverted to stable VM |
| `Infrastructure/Persistence/AppDbContext.cs` | Restored | Removed ContractVersion DbSet + mapping |
| `Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` | Restored | Reverted to stable model |

---

## 2. Files Removed

| File | Reason |
|------|--------|
| `Migrations/*AddBanquetContractVersions*` | BQ-IMPL-1A migration |
| `Migrations/*AddBanquetContractFreeze*` | BQ-IMPL-1B migration |
| `Tests/BanquetRegressionBaselineTests.cs` | BQ regression test suite |
| `docs/banquet/BQ_IMPL_*.md` | Implementation reports |

---

## 3. Banquet Entities Restored (13)

```
BanquetRoom, BanquetEvent, BanquetMenuPackage, BanquetMenuGroup,
BanquetMenuPackageLine, BanquetEventMenu, BanquetServiceLine,
BanquetContract, BanquetDeposit, BanquetChecklist,
BanquetFloorPlan, BanquetFloorPlanTable, BanquetTimeline
```

**Entities NOT in backup (removed):**
```
BanquetContractVersion, BanquetQuantificationBatch,
BanquetQuantificationLine, BanquetPurchaseRequest,
BanquetPurchaseRequestLine, BanquetModuleSetting
```

**Freeze fields removed from BanquetContract:**
```
IsFrozen, FrozenAtUtc, FrozenBy, FreezeReason
```

---

## 4. Front Office — NOT Modified

| Module | Status |
|--------|--------|
| Reservation | ✅ Untouched |
| Stay | ✅ Untouched |
| Folio | ✅ Untouched |
| Payment | ✅ Untouched |
| NightAudit | ✅ Untouched |
| Housekeeping | ✅ Untouched |
| Guest | ✅ Untouched |
| Room | ✅ Untouched |

---

## 5. Feature Freeze

Banquet module is now **FROZEN**.

- No further feature development
- Only bug fixes allowed
- Next development target: **Front Office only**

---

**BANQUET RESTORED TO STABLE BASELINE.**

**FEATURE FREEZE ENABLED.**

**NEXT DEVELOPMENT TARGET: FRONT OFFICE ONLY.**
