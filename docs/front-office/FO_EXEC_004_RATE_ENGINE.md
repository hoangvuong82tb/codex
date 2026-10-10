# FO-EXEC-004 ENTERPRISE RATE ENGINE — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE MODULE
**Trạng thái:** HOAN THANH ✅

---

## 1. Architecture

```
RatePlan (Aggregate Root)
  ├── RateCalendarEntry[] — daily prices per date
  ├── RateRule[] — pricing rules (13 types, priority-ordered)
  └── RateSeason[] — season multipliers (5 types)

CalculatePrice(date, adults, nights, totalRooms, roomsSold):
  BaseRate from Calendar (or BaseRate fallback)
  → Apply Seasons (multipliers, applied before rules)
  → Apply Rules ordered by Priority:
    · Exclusive rules break the loop (no other rules apply)
    · Non-exclusive rules stack in priority order
  → Apply Yield Management (occupancy-based, if enabled)
  → Clamp to [YieldMinRate, YieldMaxRate]
  → Return (finalPrice, auditLog)
```

---

## 2. Database

### Tables (in migration `20260720032319_AddReservationEnterpriseFields`)

| C# Entity | Table | Purpose |
|---|---|---|
| `RatePlan` | `fo.KeHoachGia` | Rate plan definition (base rate, hierarchy, yield config) |
| `RateCalendarEntry` | `fo.LichGia` | Daily rate overrides per plan, UNIQUE(RatePlanId, Date) |
| `RateRule` | `fo.QuyTacGia` | Pricing rules (13 types, priority-ordered, exclusive/combinable flags) |
| `RateSeason` | `fo.MuaGia` | Season definitions (5 types with multipliers) |

### Sequence
`fo.TrinhTuSoGia` — RPL-YYYYMM-XXXXXX

---

## 3. Rate Rules (13 Types)

| Type | Logic | Example |
|---|---|---|
| FixedPrice | Override rate to fixed value | 2,000,000 |
| PctAdjustment | ± percentage on base | +15% |
| WeekendSurcharge | +X% on Fri/Sat | +20% |
| HolidaySurcharge | +X% on holidays | +30% |
| SeasonMultiplier | × season factor | ×1.5 |
| OccupancyMultiplier | +X% per extra adult | +10%/adult |
| LengthOfStayDiscount | −X% for ≥7 nights | −10% |
| AdvanceBookingDiscount | −X% for early booking | −5% |
| LastMinutePricing | +X% within N days | +15% |
| VIPPricing | X% discount for VIP | −10% |
| CorporatePricing | Custom corporate rate | −20% |
| AgencyPricing | Custom agency rate | −15% |
| GroupPricing | Group rate | −25% |

---

## 4. Rate Hierarchy (7 Levels)

| Level | Description | Override? |
|---|---|---|
| BAR | Best Available Rate (base) | No |
| Corporate | Company-negotiated rate | Can override BAR |
| Negotiated | One-off negotiated rate | Can override Corporate |
| Package | Package-inclusive rate | Can override Negotiated |
| Group | Group block rate | Can override Package |
| ManualOverride | Front desk override | Can override all above |
| FinalOverride | Manager/Revenue override | Absolute override |

---

## 5. Season Management (5 Types)

| Season | Multiplier | Effect |
|---|---|---|
| LowSeason | 0.7 | −30% off base |
| HighSeason | 1.2 | +20% premium |
| PeakSeason | 1.5 | +50% peak |
| Blackout | 0 | Closed dates |
| SpecialEvent | 2.0 | Double rate for events |

Seasons apply BEFORE rules, multiplying the base/calendar rate.

---

## 6. Yield Management

```
Input: TotalRooms=100, RoomsSold=90 → Occupancy=90%
Threshold: 80%
  ↓ 90% > 80% → TRIGGERED
Adjustment: +10%
  ↓
Final price = MAX(YieldMinRate, MIN(YieldMaxRate, adjustedPrice))
```

---

## 7. Audit Log (Calculation Trace)

```
[BAR] Base rate: 1,500,000 VND
[Season] PeakSeason (×1.5): 2,250,000 VND
[Rule P1] GroupPricing: 1,125,000 VND (EXCLUSIVE)
[FINAL] 1,125,000 VND
```

Every calculation produces a traceable audit log.

---

## 8. Reservation Integration

`RatePlanId` is now accepted in:
- `Reservation.Create()` factory method
- `CreateReservationCommand`
- `IReservationUiService.CreateAsync()`

UI rate plan picker deferred to P2.

---

## 9. Bug Fixes

| Bug | Root Cause | Fix |
|---|---|---|
| All prices clamped to 0 | `YieldMaxRate` defaulted to 0 (not initialized in `Create()`) | Set proper defaults: Min=0, Max=99999999, Threshold=80, AdjPct=10 |
| Exclusive rule crashes | `List.Clear()` during `foreach` enumeration | Exclusive rules now `break` out of loop instead of modifying collection |

---

## 10. Files Changed

| File | Action |
|---|---|
| `Application/FrontOffice/Rate/RateContracts.cs` | NEW — DTOs, IRateRepository, search filter |
| `Infrastructure/FrontOffice/Rate/EfRateRepository.cs` | NEW — EF Core implementation |
| `Infrastructure/DependencyInjection.cs` | MODIFIED — Register rate services |
| `Core/FrontOffice/Rate/RatePlan.cs` | FIXED — Yield defaults + exclusive logic + EnableYieldManagement() |
| `Core/FrontOffice/Reservation/Reservation.cs` | MODIFIED — +RatePlanId in Create() |
| `Application/FrontOffice/Reservation/ReservationContracts.cs` | MODIFIED — +RatePlanId in command |
| `Application/FrontOffice/Reservation/ReservationHandlers.cs` | MODIFIED — Pass RatePlanId |
| `App/Services/ReservationUiService.cs` | MODIFIED — +RatePlanId in CreateAsync |
| `App/ViewModels/ReservationWorkspaceViewModel.cs` | MODIFIED — Pass null RatePlanId |
| `App/ViewModels/RateEngineViewModel.cs` | REWRITTEN — Uses IRateRepository + seed fallback |
| `Tests.Unit/RateEngineTests.cs` | NEW — 15 domain + DTO tests |
| `Tests.Unit/ReservationAcceptanceTests.cs` | MODIFIED — Updated command call |
| `Tests.Unit/ReservationWorkspaceViewModelTests.cs` | MODIFIED — Updated fake service |
| `Tests.Unit/HotelStayFoundationTests.cs` | MODIFIED — Updated Create() calls |
| `Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs` | MODIFIED — Updated Create() calls |

---

## 11. Tests

| Suite | Result |
|---|---|
| Unit Tests | 349/350 PASS (1 pre-existing failure) |
| Rate Engine Tests | 15/15 PASS |
| Integration Tests | 14/23 PASS (9 pre-existing LocalDB failures) |
| Build Errors | 0 ✅ |
| New Tables | 4 (KeHoachGia, LichGia, QuyTacGia, MuaGia) |

---

## 12. Go / No Go

### Rate Engine Enterprise: GO ✅

- ✅ RatePlan domain with calendar, rules, seasons, yield
- ✅ 13 rule types with priority-ordered execution
- ✅ 7-level rate hierarchy
- ✅ 5 season types with multipliers
- ✅ Yield management (occupancy-based)
- ✅ Audit log (full calculation trace)
- ✅ EF Core repository with full CRUD
- ✅ Reservation RatePlanId integration
- ✅ 15 passing unit tests
- ✅ Build 0 errors

2 deferred items:
- Rate plan picker in Reservation UI (P2)
- Multi-currency support (P3)

---

*Enterprise Rate Engine complete. Core pricing engine operational with full domain model, persistence layer, and Reservation integration.*
