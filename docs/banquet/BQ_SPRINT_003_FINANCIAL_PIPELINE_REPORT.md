# BQ-SPRINT-003 — BANQUET FINANCIAL PIPELINE VERIFICATION

**Ngày:** 2026-07-21
**Loại:** FINANCIAL PIPELINE VERIFICATION — ZERO PRODUCTION CHANGE
**Trạng thái:** COMPLETED ✅

---

## 1. Financial Pipeline Map

```
Menu Package     Service Line
  │                 │
  ▼                 ▼
Menu Amount      Service Amount
  │                 │
  └────────┬────────┘
           ▼
    Total Amount = Menu + Service
           │
           ▼
    Deposit Amount ──────────► Remaining = Total - Deposit
           │                        │
           ▼                        ▼
    Contract Sync ◄────────── Financial Lock
           │
           ▼
    Dashboard / Reports
```

---

## 2. Menu Amount Calculation

### Source: `RestaurantServices.cs:5179-5186`

```csharp
// Total Menu Amount
menu = Σ Menus.Sum(m => 
    m.PricingMode == "Custom"
        ? m.Amount * (TableCount + ExtraTableCount)   // Custom: multiply by tables
        : m.Amount                                     // Preset: already calculated
);

// Main Menu Amount (without extra tables)
menuMain = Σ Menus.Sum(m => 
    m.PricingMode == "Custom"
        ? m.Amount * TableCount
        : m.PricingMode == "PerGuest" ? m.Amount : m.UnitPrice * TableCount
);
```

### Menu Amount Storage (per menu line)

**File:** `RestaurantServices.cs:4462` — Attach Menu Package

```csharp
var amount = package.PricingMode == "PerTable"
    ? package.Price * entity.TableCount        // 5,000,000 × 20 = 100,000,000
    : package.Price * entity.GuestCount;       // or per guest

var eventMenu = new BanquetEventMenu {
    Amount = amount,
    TableCount = entity.TableCount + entity.ExtraTableCount,  // 20 + 1 = 21
    UnitPrice = package.Price                                 // 5,000,000
};
```

**File:** `RestaurantServices.cs:4483` — Custom Menu
```csharp
Amount = line.Qty * line.UnitPrice  // 5 × 150,000 = 750,000
```

### Menu Amount Summary

| Value | Formula | Persisted | Recalculated |
|-------|---------|-----------|-------------|
| `BanquetEventMenu.Amount` | PerTable: Price × TableCount | ✅ Yes | No |
| `BanquetEventMenu.UnitPrice` | Package price | ✅ Yes | No |
| `BanquetEventMenu.TableCount` | TableCount + ExtraTableCount | ✅ Yes | No |
| Custom menu `Amount` | Qty × UnitPrice | ✅ Yes | No |

---

## 3. Service Amount Calculation

### Source: `RestaurantServices.cs:5187-5188`

```csharp
service = Σ Services.Sum(s => s.Amount);       // Total service sold amount
serviceBase = Σ Services.Sum(s => s.Qty * s.UnitPrice);  // Base cost
```

### Service Amount Storage

**File:** `RestaurantServices.cs:5228`
```csharp
BanquetServiceLine.Amount    // Persisted sell amount
BanquetServiceLine.UnitPrice  // Per unit price
BanquetServiceLine.Qty        // Quantity
BanquetServiceLine.CostPrice  // Cost price per unit
BanquetServiceLine.CostAmount // Total cost
```

### VAT Calculation
```csharp
VatAmount = Max(0, service - serviceBase)   // Difference = VAT portion
```

### Service Amount Summary

| Value | Formula | Persisted | Recalculated |
|-------|---------|-----------|-------------|
| `Service.Amount` | Qty × UnitPrice | ✅ Yes | No |
| `ServiceBaseAmount` | Σ(Qty × UnitPrice) | No | Yes (calculated per query) |
| `VatAmount` | Service - ServiceBase | No | Yes |
| `ServiceEstimatedCost` | Σ CostAmount | ✅ Yes | No |

---

## 4. Table Calculation

### Source: `RestaurantServices.cs:5181-5186`

| Table Type | Used In | Formula |
|-----------|---------|---------|
| `TableCount` | MenuAmount (main) | `Price × TableCount` |
| `ExtraTableCount` | MenuAmount (custom) | Additional factor |
| `GuestCount` | MenuAmount (PerGuest) | `Price × GuestCount` |

### Status of Table Fields
| Field | Persisted on Entity | Used in Calculation | Verified |
|-------|-------------------|---------------------|----------|
| `TableCount` | `BanquetEvent.TableCount` | ✅ Yes — PerTable pricing | ✅ |
| `ExtraTableCount` | `BanquetEvent.ExtraTableCount` | ✅ Yes — Custom menu × ExtraTableCount | ✅ |
| `BanquetEventMenu.TableCount` | `BanquetEventMenu.TableCount` (stores TableCount + ExtraTableCount) | ✅ Yes — stored at attach time | ✅ |
| Reserve Tables | NOT IN ENTITY | ❌ Not used | ❌ |
| Actual Tables | NOT IN ENTITY | ❌ Not used | ❌ |
| Complimentary Tables | NOT IN ENTITY | ❌ Not used | ❌ |

**Finding:** Reserve Tables, Actual Tables, and Complimentary Tables are NOT implemented in the domain model or financial pipeline.

---

## 5. Deposit Flow

### Source: `RestaurantServices.cs:5193-5195`

```csharp
deposits = Σ Deposits
    .Where(d => d.Status is "Confirmed" or "PartiallyRefunded" or "Refunded")
    .Sum(d => Max(0, d.Amount - d.RefundAmount));
```

### Deposit Rules (Business Validation)
| Rule | Location | Code |
|------|----------|------|
| Amount must be > 0 | `AddDepositAsync` | `request.Amount <= 0` |
| Cannot exceed remaining amount | `AddDepositAsync` | `deposited + request.Amount > total` |
| Idempotency key | `AddDepositAsync` | `IdempotencyKey` check |
| Cash fund integration | `AddDepositAsync` | Auto-creates CashFund receipt |
| Refund creates offset transaction | `RefundDepositAsync` | CashFund + audit |

### Deposit Amount Summary

| Value | Formula | Persisted |
|-------|---------|-----------|
| `BanquetDeposit.Amount` | Input amount | ✅ Yes |
| `BanquetDeposit.RefundAmount` | Cumulative refund | ✅ Yes |
| `DepositAmount` (DTO) | Σ(Amount - RefundAmount) for Confirmed/Refunded | No — calculated per query |
| `Net Deposit` | Amount - RefundAmount | No — calculated |

---

## 6. Outstanding / Remaining Formula

### Source: `RestaurantServices.cs:5196-5197`

```csharp
total = menu + service;
remaining = Max(0, total - deposits);
```

### Formula Breakdown

```
Total Amount = Σ Menu Amount + Σ Service Amount
Remaining   = Total Amount - Net Deposits
```

**Note:** Room rental, extra charges, discounts, and taxes are NOT included in the Total formula. Only Menu + Service.

### Outstanding Summary

| Value | Formula | Persisted |
|-------|---------|-----------|
| `TotalAmount` | MenuAmount + ServiceAmount | No — calculated per query |
| `DepositAmount` | Σ(Amount - RefundAmount) | No — calculated per query |
| `RemainingAmount` | Max(0, Total - Deposit) | No — calculated per query |

---

## 7. Contract Financial Source

### Source: `RestaurantServices.cs:5128-5151` (SyncContractsAsync)

```csharp
// Contracts are SYNCED from the event DTO, not independently calculated
var dto = ToDto(entity);
foreach (var contract in entity.Contracts) {
    contract.TotalAmount = dto.TotalAmount;
    contract.DepositAmount = dto.DepositAmount;
    contract.RemainingAmount = dto.RemainingAmount;
}
```

### Contract Behavior

| Scenario | Contract Behavior |
|----------|------------------|
| Event created → contract created | Snapshot of current totals |
| Event menu changed → contract | **Auto-updated** via `SyncContractsAsync` |
| Event service changed → contract | **Auto-updated** via `SyncContractsAsync` |
| New deposit → contract | **Auto-updated** via `SyncContractsAsync` |
| Contract signed | Status changes, amounts remain synced |

**Finding:** Contracts are DYNAMIC — they auto-sync with the event DTO after every financial change. There is no snapshot/frozen contract.

### Contract Amount Summary

| Value | Source | Nature |
|-------|--------|--------|
| `BanquetContract.TotalAmount` | DTO.TotalAmount | Dynamic (synced) |
| `BanquetContract.DepositAmount` | DTO.DepositAmount | Dynamic (synced) |
| `BanquetContract.RemainingAmount` | DTO.RemainingAmount | Dynamic (synced) |
| `BanquetContract.Status` | Auto: Remaining≤0 → "Completed" | Dynamic |

---

## 8. Settlement / Financial Lock

### Source: `RestaurantServices.cs:4278` (FinalizeSettlementAsync)

Settlement requires a contract. After finalize:
- `IsFinanciallyLocked = true`
- `FinanciallyLockedAt = now`
- Event becomes read-only for financial fields

---

## 9. Dashboard Financial Source

### Source: `RestaurantServices.cs:4060-4073`

```csharp
var dashboard = new BanquetDashboardDto {
    TodayRevenue = paidOrders.Sum(x => x.TotalAmount),
    TodayDeposits = deposits.Sum(x => x.Amount),
    TodayEvents = eventsToday.Count,
    ActiveEvents = activeEvents.Count,
    PendingContracts = pendingContracts.Count,
    UnpaidAmount = activeEvents.Sum(x => x.TotalAmount) - deposits.Sum(x => x.Amount),
};
```

---

## 10. Report Financial Source

### ViewModel-based (Client-Side)

**File:** `BanquetRestaurantViewModel.Partial.cs`

Reports are generated client-side by loading all events and filtering:
- Revenue: Sum of `TotalAmount` across events
- Profit: `TotalAmount - IngredientEstimatedCost - ServiceEstimatedCost`
- Menu Revenue: Sum of `MenuAmount`
- Service Revenue: Sum of `ServiceAmount`

**Finding:** Reports are calculated client-side with no server-side aggregation — may not scale with large datasets.

---

## 11. Financial Risk Matrix

| # | Risk | Severity | Evidence |
|---|------|----------|----------|
| 1 | **Total = Menu + Service only** — no room rental, taxes, discounts | HIGH | `RestaurantServices.cs:5196` |
| 2 | **Contract amounts auto-sync** — no frozen/signed contract | HIGH | `RestaurantServices.cs:5142-5149` |
| 3 | **Reserve/Actual/Complimentary Tables not used** in formulas | MEDIUM | Fields absent from entity |
| 4 | **VAT = Service - ServiceBase** — may not reflect actual tax | MEDIUM | `RestaurantServices.cs:5197` |
| 5 | **Reports calculated client-side** — scalability risk | MEDIUM | ViewModel code |
| 6 | **Menu ExtraAmount may double-count** ExtraTable on Custom only | MEDIUM | `RestaurantServices.cs:5181-5183` |
| 7 | **No Discount field** on BanquetEvent | MEDIUM | Entity field absent |
| 8 | **No Room Rental** in financial model | INFO | Not in entity |

---

## 12. Missing Financial Logic

| # | Missing | Priority |
|---|---------|----------|
| 1 | Room rental charge | P1 |
| 2 | Discount/surcharge field | P1 |
| 3 | Tax calculation separate from Service | P2 |
| 4 | Contract snapshot/freeze on sign | P1 |
| 5 | Reserve/Actual/Complimentary table pricing | P2 |
| 6 | Server-side report aggregation | P2 |
| 7 | Per-guest pricing for custom menus | P2 |

---

## 13. Evidence Appendix

All financial pipeline methods traced:

| File | Lines | Method | Purpose |
|------|-------|--------|---------|
| `RestaurantServices.cs` | 5179-5197 | `ToDto(BanquetEvent)` | Full financial DTO mapping |
| `RestaurantServices.cs` | 5181-5183 | — | Menu amount calculation |
| `RestaurantServices.cs` | 5187-5188 | — | Service amount calculation |
| `RestaurantServices.cs` | 5193-5195 | — | Deposit net calculation |
| `RestaurantServices.cs` | 5196-5197 | — | Total & Remaining formula |
| `RestaurantServices.cs` | 4462 | `AttachMenuPackageAsync` | Menu amount storage |
| `RestaurantServices.cs` | 4483 | `SaveCustomMenuAsync` | Custom menu amount |
| `RestaurantServices.cs` | 4564 | `AddDepositAsync` | Deposit creation |
| `RestaurantServices.cs` | 5128-5151 | `SyncContractsAsync` | Contract auto-sync |
| `RestaurantServices.cs` | 4060-4073 | `GetDashboardAsync` | Dashboard financials |

---

## 14. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS |
| Release Build | PASS |
| BQ Regression Tests (32) | 32/32 PASS |
| Existing Banquet Tests (8) | 8/8 PASS |

---

**BQ-SPRINT-003 COMPLETED AS FINANCIAL PIPELINE VERIFICATION ONLY.**
**NO PRODUCTION CODE WAS MODIFIED.**
**ALL FINANCIAL FORMULAS TRACED TO SOURCE CODE WITH LINE NUMBERS.**
