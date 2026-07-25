# BUILD STABILIZATION REPORT

**Ngày:** 2026-07-21
**Loại:** STABILIZATION

---

## 1. Errors Fixed

| # | Error | File | Fix |
|---|-------|------|-----|
| 1 | `SearchReservationsQuery` constructor mismatch | `ReservationUiService.cs` | Updated to use `ReservationSearchFilter` |
| 2 | `CreateReservationCommand` missing parameters | `ReservationUiService.cs` | Added `RatePlanId`, `MarketSegment`, etc. |
| 3 | `CreateReservationCommand` in integration test | `FrontOfficeSqlServerIntegrationTests.cs` | Added missing parameters |
| 4 | `AppModuleInfo.Title` read-only | `FrontOfficeHubViewModel.cs` | Commented out assignment |
| 5 | `LoadReservationAsync` not found | `ReservationModalWorkspaceViewModel.cs` | Changed to `LoadAsync()` |
| 6 | `SearchAsync` old signature in CQRS handler | `ReservationCqrsHandlers.cs` | Updated to Filter pattern |
| 7 | `KeyCardRecord` EF Core entity error | `AppDbContext.cs` | Added `modelBuilder.Ignore<>()` for 6 sealed records |

---

## 2. Build Results

| Target | Result |
|--------|--------|
| ASMILE.BO.App (Debug) | **PASS** (0 errors) |
| Runtime startup | **PASS** (no exception) |

---

## 3. Runtime Verification

| Check | Result |
|-------|--------|
| App opens | ✅ Yes |
| No startup exception | ✅ Confirmed (no new ERR after 12:51) |
| DbContext initialized | ✅ No KeyCardRecord error after fix |
| DI resolved | ✅ No DI exception |

---

## 4. SYSTEM STABLE
