# BANQUET — BUSINESS REGRESSION AUDIT

**Ngày:** 2026-07-21
**Loại:** REGRESSION AUDIT — NO CODE CHANGE

---

## 1. Audit Scope

So sánh implementation hiện tại (sau BQ-IMPL-1B hotfix) với BQ-SPRINT-004 baseline.

### Flows Audited:
1. Menu → Event → Financial → Contract → Summary UI
2. Service → Financial → Summary UI
3. Deposit → Remaining → History → Financial UI
4. Checklist → Timeline
5. Contract → Version → Freeze → Financial UI
6. Quantification → Purchase Request

---

## 2. Flow 1: Menu → Financial

| Step | Status | Notes |
|------|--------|-------|
| Menu package attach | ✅ | `AttachMenuPackageAsync` unchanged |
| Menu amount calculation | ✅ | `ToDto:5181-5183` — Formula unchanged: `Custom × TableCount`, `PerTable × Price × TableCount` |
| Menu DTO mapping | ✅ | `BanquetEventDto.MenuAmount` populated correctly |
| ViewModel binding | ✅ | `MenuAmount` displayed in event detail |
| SyncContracts after menu change | ✅ | Frozen contracts skipped via `if (contract.IsFrozen) continue` |

**Regression risk: NONE** ✅

---

## 3. Flow 2: Service → Financial

| Step | Status | Notes |
|------|--------|-------|
| Service line add/edit | ✅ | `SaveServiceLineAsync` unchanged |
| Service amount calculation | ✅ | `ToDto:5187` — `Σ Service.Amount` |
| Service DTO mapping | ✅ | `BanquetEventDto.ServiceAmount` |
| SyncContracts after service change | ✅ | Frozen contracts skipped |

**Regression risk: NONE** ✅

---

## 4. Flow 3: Deposit → Remaining

| Step | Status | Notes |
|------|--------|-------|
| Deposit add | ✅ | `AddDepositAsync` unchanged |
| Deposit net calculation | ✅ | `ToDto:5193-5195` — `Σ(Max(0, Amount - RefundAmount))` |
| Remaining calculation | ✅ | `ToDto:5197` — `Max(0, Total - Deposits)` |
| Deposit block: exceeds remaining | ✅ | Validation still enforced |
| SyncContracts after deposit | ✅ | Frozen contracts skipped |

**Regression risk: NONE** ✅

---

## 5. Flow 4: Checklist / Timeline

| Step | Status | Notes |
|------|--------|-------|
| Checklist CRUD | ✅ | Unchanged — no contract dependency |
| Timeline CRUD | ✅ | Unchanged — no contract dependency |
| Financial lock check | ✅ | `EnsureFinanciallyEditable` unchanged |

**Regression risk: NONE** ✅

---

## 6. Flow 5: Contract → Freeze

| Step | Status | Notes |
|------|--------|-------|
| Contract create | ✅ | Creates version, freeze fields default to false |
| Contract update | ✅ | Guarded: `IsFrozen → throw` |
| Contract freeze | ✅ | Sets freeze fields, creates "Frozen" version |
| Contract unfreeze | ✅ | Clears freeze fields, creates "Unfrozen" version |
| SyncContracts on frozen | ✅ | `if (contract.IsFrozen) continue` |
| **ContractSummaryVm: IsFrozen** | ⚠️ | **MISSING** — summary VM lacks freeze field |

### Issue #1: ContractSummaryVm missing IsFrozen (P1)

**File:** `BanquetViewModels.cs:116` — `BanquetContractSummaryVm`
**Missing:** `IsFrozen` property
**Impact:** Dashboard contract list cannot show freeze status
**Fix:** Add `IsFrozen` to `BanquetContractSummaryVm` and populate from `BanquetContractDto.IsFrozen`

---

## 7. Flow 6: Quantification / Purchase Request

| Step | Status | Notes |
|------|--------|-------|
| Quantification batch | ✅ | Unchanged — no contract dependency |
| Purchase request | ✅ | Unchanged — no contract dependency |

**Regression risk: NONE** ✅

---

## 8. Null Mapping Audit

| Entity → DTO Field | Status |
|-------------------|--------|
| `BanquetContract.IsFrozen` → `BanquetContractDto.IsFrozen` | ✅ Mapped |
| `BanquetContractDto.IsFrozen` → `BanquetContractSummaryVm` | ❌ **NOT MAPPED** (P1) |
| `BanquetContract.FreezeReason` → UI | ❌ **NO UI BINDING** (P2) |

---

## 9. Summary of Findings

| # | Issue | Severity | Impact | Origin |
|---|-------|----------|--------|--------|
| 1 | `BanquetContractSummaryVm` missing `IsFrozen` | **P1** | Dashboard can't show freeze status | BQ-IMPL-1B (UI deferred) |
| 2 | No XAML binding for freeze fields | **P2** | No UI indication of frozen contracts | BQ-IMPL-1B (UI deferred) |
| 3 | `BanquetContractVersion` DTO not exposed in ViewModel | **P2** | Version history not viewable | BQ-IMPL-1A (UI deferred) |

---

## 10. Verified NOT Broken (Regression Pass)

| Flow | Status |
|------|--------|
| Menu calculation (PerTable, Custom) | ✅ |
| Service amount (Σ Qty × UnitPrice) | ✅ |
| Deposit net (Amount - RefundAmount) | ✅ |
| Remaining (Total - Deposits) | ✅ |
| Financial lock (`IsFinanciallyLocked`) | ✅ |
| SyncContracts (auto-update on changes) | ✅ |
| Checklist/Timeline | ✅ |
| Quantification | ✅ |
| Purchase Request | ✅ |
| Event status machine | ✅ |
| Room conflict detection | ✅ |

---

## 11. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS |
| Release Build | PASS |
| BQ Regression (32) | 32/32 PASS |
| BQ-IMPL-1A (4) | 4/4 PASS |
| BQ-IMPL-1B (9) | 9/9 PASS |
| BQ Hotfix (3) | 3/3 PASS |
| Existing Banquet (8) | 8/8 PASS |
| **Total** | **56/56 PASS** |

---

## 12. Conclusion

**0 P0 issues found.** All core business flows (menu, service, deposit, financial, checklist, timeline, quantification) are intact with no regression from BQ-SPRINT-004 baseline.

**2 P1 issues** are UI gaps intentionally deferred to BQ-IMPL-1C:
- ContractSummaryVm missing IsFrozen
- No XAML binding for freeze status

**1 P2 issue**: ContractVersion history not exposed in ViewModel.

**No broken bindings. No wrong calculations. No null mappings in the service/DTO layer. No outdated DTOs. No missing syncs.**
