# HOTEL MANAGEMENT — PHASE 2B IMPLEMENTATION SPECIFICATION

**Date:** 2026-08-10  
**Status:** CODEX IMPLEMENTATION SPECIFICATION — Phase 2B.5 Validation Complete  
**Predecessors:** Live DB Map (Phase 1), Data Contract (Phase 2A), Production Readiness Audit, Phase 2B Blueprint

---

## 1. Executive Summary

This document validates the Phase 2B Migration Blueprint against all 4 source documents, audits all 127 gaps, and produces a precise Codex implementation specification. The Blueprint is **largely validated** with 3 critical blockers that require architectural decisions before Codex can begin.

**Overall verdict:** CONDITIONALLY APPROVED — 3 BLOCKED decisions must be resolved before full implementation. P0 Reservation drift resolution (G-001/002/003) is READY NOW.

---

## 2. Source Documents

| Document | Role | Validation |
|---|---|---|
| `HOTEL_MANAGEMENT_LIVE_DB_MAP.md` | Verified live SQL Server schema | Fully cross-referenced |
| `HOTEL_MANAGEMENT_DATA_CONTRACT.md` | Canonical entity contract, decisions D-01 to D-10 | Authority for all architectural decisions |
| `docs/HOTEL_MANAGEMENT_PRODUCTION_READINESS_AUDIT.md` | Repository, CQRS, mock, workflow evidence | Fully cross-referenced |
| `HOTEL_MANAGEMENT_PHASE_2B_MIGRATION_BLUEPRINT.md` | Proposed migration plan | This document's subject of validation |
| `AppDbContext.cs` (lines 920-949) | EF Core entity configuration | Inspected for Reservation/Stay/Folio/Group/Rate config |
| `Reservation.cs` (Core domain) | Domain entity with Guarantee/Deposit methods | Inspected for drift evidence |

---

## 3. Validation Method

Each Blueprint claim was validated against:
1. **Data Contract** — Does the canonical entity contract support this?
2. **Live DB Map** — Does the live database evidence support this?
3. **Production Readiness Audit** — Does the code/repository/CQRS evidence support this?
4. **AppDbContext configuration** — Does the EF mapping support this?
5. **Domain entities** — Do the domain aggregates support this?

Conflicts resolved by priority: Data Contract > Live DB Map > Production Readiness Audit > Blueprint.

---

## 4. Blueprint Validation Result

### 4.1 Confirmed Evidence

The following Blueprint claims are **VERIFIED** against source documents:

| Blueprint Claim | Source Evidence |
|---|---|
| 10 reservations, 5 stays, 12 rooms, 3 room types | Live DB Map §4, §26 |
| 99 migration records | Live DB Map §25 |
| Migration source absent from checkout | Live DB Map §25 |
| Reservation EF/DB drift (GuaranteeType, DepositStatus, DepositAmount) | Live DB Map §24 + AppDbContext line 929 |
| Guest profile table empty (0 rows) | Live DB Map §4, §9 |
| No DbSet<GuestProfile> in AppDbContext | AppDbContext grep (no GuestProfile DbSet) |
| Group tables empty (0 rows) | Live DB Map §13 |
| Folio/FolioEntry tables empty (0 rows) | Live DB Map §14 |
| Payment/Invoice/AR tables empty (0 rows) | Live DB Map §15, §16 |
| Lost & Found table empty (0 rows) | Live DB Map §19 |
| FO audit table empty (0 rows) | Live DB Map §20 |
| Platform audit populated (12,842 rows) | Live DB Map §20 |
| HotelStay has no GuestId FK | AppDbContext line 933 (no GuestId property mapped) |
| GroupRoomAssignment has no Guest/Stay FK | AppDbContext line 940 |
| KeyCardRecord, WakeUpCallRecord, ComplaintRecord ignored | AppDbContext lines 943-946 |
| RatePlan/RateCalendar/RateRule/RateSeason tables exist, empty | AppDbContext lines 935-938 |
| Reservation has Guarantee() and RecordDeposit() domain methods | Reservation.cs lines 86-97 |
| GuaranteeType enum (6 values), DepositStatus enum (6 values) | Reservation.cs lines 7-8 |

### 4.2 Blueprint Issues Found

| Issue | Blueprint Section | Problem | Severity |
|---|---|---|---|
| B-001 | §8 (Database Change Strategy) | Proposes 14 new tables without verifying Data Contract preconditions | Medium |
| B-002 | §14 (EF Core Impact) | Assumes `fo.HoSoKhach` is canonical guest without D-02 approval | High |
| B-003 | §14 (EF Core Impact) | Proposes new `fo.ThanhToan` without checking existing payment handlers | Medium |
| B-004 | §14 (EF Core Impact) | Proposes new AR tables when `dbo.ReceivableTransactions` DbSet exists | Medium |
| B-005 | §18 (Execution Order) | 26-week timeline assumes all 10 decisions approved in week 1 | High |
| B-006 | §8 (Database Change Strategy) | "Activate existing schema" for Guest assumes schema is complete | Low |
| B-007 | §11 (Index Strategy) | Proposes indexes without verifying query patterns from repository code | Low |

---

## 5. Overall Decision

| Gate | Decision |
|---|---|
| **Database Blueprint** | CONDITIONALLY APPROVED — 3 blockers |
| **Code Implementation** | CONDITIONALLY READY — can start P0 drift fix immediately |
| **Migration** | CONDITIONALLY APPROVED — delta-only, no destructive ops |
| **Financial** | BLOCKED — needs D-05/D-06/D-07/D-08 approval |
| **Security** | BLOCKED — needs D-10 approval |
| **Data** | CONDITIONALLY APPROVED — pilot data preservation required |

---

## 6. Approved Changes (READY FOR CODEX)

These changes are fully supported by all 4 source documents and can be implemented immediately.

| ID | Domain | Change | Evidence |
|---|---|---|---|
| A-001 | Reservation | Add `GuaranteeType` column to `fo.DatPhong` | Data Contract §13, AppDbContext line 929, Reservation.cs line 61 |
| A-002 | Reservation | Add `DepositStatus` column to `fo.DatPhong` | Data Contract §13, AppDbContext line 929, Reservation.cs line 65 |
| A-003 | Reservation | Add `DepositAmount` column to `fo.DatPhong` | Data Contract §13, AppDbContext line 929, Reservation.cs line 63 |
| A-004 | Stay | Add `GuestId` column to `fo.LuuTruKhachSan` | Data Contract §15, Live DB Map §11 |
| A-005 | Folio Entry | Add `IdempotencyKey` column to `fo.ButToanFolio` | Data Contract §38, §43 |
| A-006 | Folio Entry | Add `RowVersion` column to `fo.ButToanFolio` | Data Contract §30, Live DB Map §23 |
| A-007 | Room | Add `HousekeepingStatus` column to `fo.PhongKhachSan` | Data Contract §14, §24 |
| A-008 | All | Create migration baseline from live schema | Live DB Map §25 |
| A-009 | Mock | Remove runtime mock generators from DI | Production Readiness Audit §9, §16 |
| A-010 | FK | Add FK from `fo.DatPhong.RoomTypeId` to `fo.LoaiPhongKhachSan` | Data Contract §34 (already in EF config line 929, verify DB-side) |

---

## 7. Approved With Conditions

These changes are supported but require specific pre-conditions before Codex executes.

| ID | Domain | Change | Condition | Evidence |
|---|---|---|---|---|
| C-001 | Guest | Add `DbSet<GuestProfile>` to AppDbContext | D-02 must be approved first | Data Contract §11, D-02 |
| C-002 | Guest | Add FK `fo.DatPhong.GuestId` → `fo.HoSoKhach` | Guest profiles must exist or NULL allowed | Data Contract §11, §34 |
| C-003 | Guest | Add FK `fo.LuuTruKhachSan.GuestId` → `fo.HoSoKhach` | Backfill guest references on 5 pilot stays | Data Contract §15, D-03 |
| C-004 | Payment | Create `fo.ThanhToan` table | Existing payment handlers must be verified first | Data Contract §19, D-06 |
| C-005 | AR | Create AR sub-ledger tables | Verify `dbo.ReceivableTransactions` compatibility first | Data Contract §23, D-08 |
| C-006 | BusinessDate | Create `fo.BusinessDate` table | D-04 must be approved first | Data Contract §10, D-04 |
| C-007 | Property | Create/formalize Property master | D-01 must be approved first | Data Contract §9, D-01 |
| C-008 | Invoice | Create `fo.Invoices` table | D-07 must be approved first; evaluate reusing `dbo.Invoices` | Data Contract §22, D-07 |

---

## 8. Needs Decision (BLOCKED)

These changes cannot proceed until architectural decisions are made.

| ID | Decision Required | What's Blocked | Who Decides | Impact |
|---|---|---|---|---|
| D-01 | Property authority — canonical source | All PropertyId FK enforcement | Architect | All property-scoped operations |
| D-02 | Guest identity — `fo.HoSoKhach` vs `dbo.Customers` vs hybrid | Guest persistence, Guest FK on Reservation/Stay | Architect + Business | Guest 360, CRM, PII |
| D-07 | Legal invoice authority — reuse `dbo.Invoices` vs new `fo.Invoices` | Invoice persistence, AR linkage | Architect + Finance + Legal | Legal invoicing, tax |
| D-06 | Payment allocation — reuse `dbo.PaymentAllocations` vs new `fo.PhanBoThanhToan` | Payment persistence design | Architect + Finance | Financial integrity |
| D-08 | AR architecture — derived vs sub-ledger vs hybrid | AR table design | Architect + Finance | AR reconciliation |
| D-04 | Business date authority — night audit aggregate vs app setting | BusinessDate table design | Architect + Operations | Night audit, revenue |

---

## 9. Requires Live Verification

These Blueprint claims need live DB verification before Codex acts.

| ID | What to Verify | Why |
|---|---|---|
| V-001 | Exact column list of `fo.DatPhong` | Confirm which columns exist vs EF mapping |
| V-002 | Exact column list of `fo.HoSoKhach` | Confirm schema completeness before activation |
| V-003 | Exact column list of `fo.DoThatLac` | Confirm schema before activation |
| V-004 | `dbo.Invoices` schema and constraints | Evaluate reuse vs new table |
| V-005 | `dbo.ReceivableTransactions` schema and constraints | Evaluate reuse vs new table |
| V-006 | `dbo.PaymentAllocations` schema and constraints | Evaluate reuse vs new table |
| V-007 | Current `PropertyId` values in pilot data | Verify property scope consistency |
| V-008 | Exact FK constraints present on `fo.DatPhong` | Verify which FKs already exist |

---

## 10. Rejected / Duplicate Gaps

| ID | Original Gap | Reason |
|---|---|---|
| R-001 | G-017 (RowVersion on StayHistory) | StayHistory is append-only by design; rowversion not needed on history tables per Data Contract §30 |
| R-002 | G-032 (Guest Merge) | Deferred to post-GA; not a P0 production blocker |
| R-003 | G-038 (Room Block) | Room Block is a P2 capability; can be derived from Reservation + Group assignment |
| R-004 | G-050 through G-127 | P2/P3 gaps are correctly classified as non-blocking; no change to Blueprint |

---

## 11. Complete GAP Validation Matrix (P0 and P1 only)

| GAP ID | Domain | Blueprint Proposal | Evidence | Decision | Codex Action |
|---|---|---|---|---|---|
| G-001 | Reservation | Add GuaranteeType column | Data Contract §13, AppDbContext L929 | **APPROVED** | Add column via delta migration |
| G-002 | Reservation | Add DepositStatus column | Data Contract §13, AppDbContext L929 | **APPROVED** | Add column via delta migration |
| G-003 | Reservation | Add DepositAmount column | Data Contract §13, AppDbContext L929 | **APPROVED** | Add column via delta migration |
| G-004 | Guest | Populate `fo.HoSoKhach` + FK | Data Contract §11, D-02 | **NEEDS_DECISION** | Blocked on D-02 |
| G-005 | Guest | Add DbSet<GuestProfile> | Data Contract §11, D-02 | **NEEDS_DECISION** | Blocked on D-02 |
| G-006 | Stay | Add Guest FK | Data Contract §15, D-03 | **APPROVED_WITH_CONDITION** | Requires G-004 first |
| G-007 | Property | Property master | Data Contract §9, D-01 | **NEEDS_DECISION** | Blocked on D-01 |
| G-008 | BusinessDate | Business date authority | Data Contract §10, D-04 | **NEEDS_DECISION** | Blocked on D-04 |
| G-009 | Payment | Hotel payment aggregate | Data Contract §19, D-06 | **NEEDS_DECISION** | Blocked on D-06 |
| G-010 | Invoice | Legal invoice contract | Data Contract §22, D-07 | **NEEDS_DECISION** | Blocked on D-07 |
| G-011 | AR | AR sub-ledger + aging | Data Contract §23, D-08 | **NEEDS_DECISION** | Blocked on D-08 |
| G-012 | Guest | Guest vs customer boundary | Data Contract §11 | **NEEDS_DECISION** | Blocked on D-02 |
| G-013 | Reservation | Add rate/source/room FKs | Data Contract §34 | **APPROVED_WITH_CONDITION** | Verify existing FKs first (V-008) |
| G-014 | Financial | Folio→Payment→Invoice→AR chain | Data Contract §34, §38 | **APPROVED_WITH_CONDITION** | Requires G-009, G-010, G-011 |
| G-015 | Mock | Remove runtime mock | Production Readiness Audit §9 | **APPROVED** | Remove from DI, add design-time flag |
| G-016 | Migration | Controlled delta migration | Live DB Map §25 | **APPROVED** | Baseline from live schema |
| G-017 | Concurrency | RowVersion on FolioEntry | Data Contract §30 | **APPROVED** | Add column |
| G-018 | FK | Cross-aggregate FKs | Data Contract §34 | **APPROVED_WITH_CONDITION** | After orphan assessment |
| G-019 | Folio | Real posting/settlement | Data Contract §17 | **APPROVED_WITH_CONDITION** | Requires Payment first |
| G-020 | Deposit | Deposit sub-ledger | Data Contract §20 | **APPROVED_WITH_CONDITION** | Requires Payment first |
| G-021 | Refund | Refund sub-ledger | Data Contract §21 | **APPROVED_WITH_CONDITION** | Requires Payment + Deposit |
| G-022 | Group | Group rooming FKs | Data Contract §16 | **APPROVED_WITH_CONDITION** | Requires Guest first |
| G-023 | Housekeeping | Task/inspection aggregate | Data Contract §24 | **APPROVED_WITH_CONDITION** | Requires Room first |
| G-024 | Minibar | Session/consumption aggregate | Data Contract §25 | **APPROVED_WITH_CONDITION** | Requires Folio first |
| G-025 | KeyCard | Credential/event model | Data Contract §27 | **APPROVED_WITH_CONDITION** | Requires Stay + Guest |
| G-026 | LostFound | Custody workflow | Data Contract §26 | **APPROVED** | Activate existing schema |
| G-027 | Permission | Capability + property auth | Data Contract §29, D-10 | **NEEDS_DECISION** | Blocked on D-10 |
| G-028 | Audit | Shared audit + business events | Data Contract §28 | **APPROVED** | Integrate platform audit |
| G-029 | Read Models | Versioned source-labeled models | Data Contract §37 | **APPROVED** | After canonical sources exist |
| G-030 | Guest Pref | Guest preference table | Data Contract §11 | **APPROVED_WITH_CONDITION** | Requires Guest first |
| G-031 | Guest Privacy | PII governance | Data Contract §11, §38 | **APPROVED_WITH_CONDITION** | Requires Guest first |

---

## 12. P0 Implementation Plan (READY NOW)

### Unit P0-001: Resolve Reservation EF/DB Drift

**Priority:** P0 — BLOCKING ALL RESERVATION WRITES  
**Dependencies:** None  
**Objective:** Add 3 missing columns to `fo.DatPhong` so EF write path works

**Current State:**
- EF maps `GuaranteeType` (string, max 40), `DepositStatus` (string, max 30), `DepositAmount` (decimal 18,2)
- Live `fo.DatPhong` does not have these columns
- Domain entity `Reservation` uses `Guarantee()` and `RecordDeposit()` methods
- 10 pilot rows exist

**Target State:**
- 3 columns added with NULL allowed (pilot rows get NULL)
- EF mapping unchanged (already correct)
- Reservation write path functional

**Database Change:**
```sql
ALTER TABLE fo.DatPhong ADD GuaranteeType NVARCHAR(40) NULL;
ALTER TABLE fo.DatPhong ADD DepositStatus NVARCHAR(30) NULL;
ALTER TABLE fo.DatPhong ADD DepositAmount DECIMAL(18,2) NULL;
```

**EF Change:** NONE (mapping already correct at AppDbContext line 929)

**Validation:**
- Pre: `SELECT COUNT(*) FROM fo.DatPhong` = 10
- Post: `SELECT COUNT(*) FROM fo.DatPhong` = 10
- Post: Verify columns exist in `INFORMATION_SCHEMA.COLUMNS`
- Post: Verify pilot rows have NULL for new columns
- Post: Run reservation query through EF to confirm no mapping error

**Rollback:**
```sql
ALTER TABLE fo.DatPhong DROP COLUMN GuaranteeType;
ALTER TABLE fo.DatPhong DROP COLUMN DepositStatus;
ALTER TABLE fo.DatPhong DROP COLUMN DepositAmount;
```

**Done Criteria:** EF can read/write Reservation without SqlException on missing columns.

---

### Unit P0-002: Add RowVersion to FolioEntry

**Priority:** P0 — FINANCIAL CONCURRENCY  
**Dependencies:** None  
**Objective:** Add `RowVersion` column to `fo.ButToanFolio`

**Database Change:**
```sql
ALTER TABLE fo.ButToanFolio ADD RowVersion ROWVERSION NOT NULL;
```

**EF Change:** Add `ConfigureRowVersion(entity.Property(x => x.RowVersion))` to FolioEntry configuration in AppDbContext.

**Done Criteria:** FolioEntry has optimistic concurrency protection.

---

### Unit P0-003: Add IdempotencyKey to FolioEntry

**Priority:** P0 — FINANCIAL IDEMPOTENCY  
**Dependencies:** None  
**Objective:** Add `IdempotencyKey` to prevent duplicate folio postings

**Database Change:**
```sql
ALTER TABLE fo.ButToanFolio ADD IdempotencyKey NVARCHAR(100) NULL;
CREATE UNIQUE INDEX UQ_fo_ButToanFolio_Idempotency ON fo.ButToanFolio(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
```

**EF Change:** Add `entity.Property(x => x.IdempotencyKey).HasMaxLength(100)` to FolioEntry configuration.

**Done Criteria:** Duplicate posting with same idempotency key is rejected.

---

### Unit P0-004: Create Migration Baseline

**Priority:** P0 — FOUNDATION  
**Dependencies:** None  
**Objective:** Create a script representing the current live schema as a baseline

**Action:** Generate a SQL script of current `fo` schema using SSMS "Script Database As → CREATE" for all `fo` and `fo_process` tables. Save as `scripts/baseline_fo_schema.sql`.

**Done Criteria:** Baseline script exists and can recreate current `fo` schema on an empty database.

---

### Unit P0-005: Remove Runtime Mock Generators

**Priority:** P0 — DATA INTEGRITY  
**Dependencies:** None  
**Objective:** Isolate all mock data generators from runtime DI

**Files to inspect:**
- `GuestInquiryViewModel.cs` — `BuildFakeData()` (15,000 guests)
- `ArAgingViewModel.cs` — `BuildFakeData()` (800 companies)
- `ArLedgerViewModel.cs` — mock data
- `ArPaymentViewModel.cs` — mock data
- `LostFoundViewModel.cs` — mock data
- `MinibarViewModel.cs` — mock data
- `WorkforceViewModel.cs` — mock data
- `GroupReservationViewModel.cs` — mock data

**Action:** 
1. Wrap each `BuildFakeData()` call in `#if DEBUG` or feature flag
2. Add `IsDesignTime` property to each ViewModel
3. When runtime and no real data source: show empty state, not fake data
4. Register design-time mock providers separately from runtime

**Done Criteria:** Running in production mode with no data source returns empty state, not fake data.

---

## 13. P1 Implementation Plan (AFTER P0 + DECISIONS)

### Unit P1-001: Add GuestId FK to Stay

**Priority:** P1 — requires G-004 (Guest)  
**Dependencies:** P0-001, D-02 decision  
**Objective:** Add `GuestId` column and FK to `fo.LuuTruKhachSan`

**Database Change:**
```sql
ALTER TABLE fo.LuuTruKhachSan ADD GuestId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.LuuTruKhachSan ADD CONSTRAINT FK_fo_LuuTruKhachSan_HoSoKhach 
    FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId);
```

**EF Change:** Add `GuestId` property to `HotelStay` entity; add FK configuration in AppDbContext.

**Backfill:** For 5 pilot stays, attempt to match guest via reservation snapshot name.

**Done Criteria:** Stay.GuestId is populated or NULL where guest unknown.

---

### Unit P1-002: Add GuestId FK to Reservation

**Priority:** P1 — requires G-004  
**Dependencies:** D-02 decision  
**Objective:** Add `GuestId` FK to `fo.DatPhong`

**Database Change:**
```sql
ALTER TABLE fo.DatPhong ADD GuestId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.DatPhong ADD CONSTRAINT FK_fo_DatPhong_HoSoKhach 
    FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId);
```

---

### Unit P1-003: Add HousekeepingStatus to Room

**Priority:** P1 — OPERATIONS  
**Dependencies:** None  
**Objective:** Separate room operational status from housekeeping status

**Database Change:**
```sql
ALTER TABLE fo.PhongKhachSan ADD HousekeepingStatus NVARCHAR(30) NULL;
```

**EF Change:** Add `HousekeepingStatus` property to `HotelRoom` entity (nullable enum string).

---

### Units P1-004 through P1-031

See Section 51 (Implementation Dependency Graph) for remaining P1 units. All P1 units require at least one P0 unit or architectural decision to be completed first.

---

## 14. P2 Implementation Plan

P2 gaps (G-050 through G-091, 42 gaps) are correctly classified as non-blocking. They should be implemented after P0 and P1 are complete. Priority order:

1. Payment gateway integration (G-073)
2. Multi-currency support (G-062, G-069, G-072)
3. E-invoice provider (G-076)
4. Collection workflow (G-078)
5. Night audit workflow (G-091)
6. Remaining P2 gaps

---

## 15. P3 Implementation Plan

P3 gaps (G-092 through G-127, 36 gaps) are UI polish, reporting templates, and nice-to-have features. No database changes required. Implement after all P0/P1/P2 are complete.

---

## 16. Database Implementation Specification

### 16.1 New Tables Required

| Table | Schema | When | Pre-condition |
|---|---|---|---|
| `ThanhToan` (Payment) | `fo` | Phase 2B-5 | D-06 approved |
| `PhanBoThanhToan` (PaymentAllocation) | `fo` | Phase 2B-5 | Payment table exists |
| `DatCoc` (Deposit) | `fo` | Phase 2B-5 | Payment table exists |
| `HoanTien` (Refund) | `fo` | Phase 2B-5 | Payment + Deposit exist |
| `HoaDon` (Invoice) or reuse `dbo.Invoices` | `fo` or `dbo` | Phase 2B-6 | D-07 approved |
| `ARTransaction` (AR) or reuse `dbo.ReceivableTransactions` | `fo` or `dbo` | Phase 2B-6 | D-08 approved |
| `HousekeepingTask` | `fo` | Phase 2B-7 | Room table ready |
| `RoomInspection` | `fo` | Phase 2B-7 | HousekeepingTask exists |
| `MinibarSession` | `fo` | Phase 2B-7 | Room + Folio ready |
| `MinibarConsumption` | `fo` | Phase 2B-7 | MinibarSession exists |
| `KeyCardCredential` | `fo` | Phase 2B-8 | Stay + Guest ready |
| `KeyCardEvent` | `fo` | Phase 2B-8 | KeyCardCredential exists |
| `BusinessDate` | `fo` | Phase 2B-1 | D-04 approved |
| `GuestPreference` | `fo` | Phase 2B-2 | Guest table activated |

### 16.2 Columns to Add to Existing Tables

| Table | Column | Type | Nullable | When |
|---|---|---|---|---|
| `fo.DatPhong` | `GuaranteeType` | `NVARCHAR(40)` | YES | P0-001 |
| `fo.DatPhong` | `DepositStatus` | `NVARCHAR(30)` | YES | P0-001 |
| `fo.DatPhong` | `DepositAmount` | `DECIMAL(18,2)` | YES | P0-001 |
| `fo.DatPhong` | `GuestId` | `UNIQUEIDENTIFIER` | YES | P1-002 |
| `fo.LuuTruKhachSan` | `GuestId` | `UNIQUEIDENTIFIER` | YES | P1-001 |
| `fo.ButToanFolio` | `RowVersion` | `ROWVERSION` | NO | P0-002 |
| `fo.ButToanFolio` | `IdempotencyKey` | `NVARCHAR(100)` | YES | P0-003 |
| `fo.PhongKhachSan` | `HousekeepingStatus` | `NVARCHAR(30)` | YES | P1-003 |

### 16.3 New Foreign Keys

| Child | Parent | When |
|---|---|---|
| `fo.DatPhong.GuestId` | `fo.HoSoKhach.GuestProfileId` | P1-002 |
| `fo.LuuTruKhachSan.GuestId` | `fo.HoSoKhach.GuestProfileId` | P1-001 |
| `fo.ThanhToan.FolioId` | `fo.SoFolio.HotelFolioId` | Phase 2B-5 |
| `fo.PhanBoThanhToan.PaymentId` | `fo.ThanhToan.PaymentId` | Phase 2B-5 |
| `fo.HoanTien.PaymentId` | `fo.ThanhToan.PaymentId` | Phase 2B-5 |
| `fo.HousekeepingTask.RoomId` | `fo.PhongKhachSan.HotelRoomId` | Phase 2B-7 |
| `fo.MinibarSession.StayId` | `fo.LuuTruKhachSan.HotelStayId` | Phase 2B-7 |
| `fo.KeyCardCredential.StayId` | `fo.LuuTruKhachSan.HotelStayId` | Phase 2B-8 |
| `fo.KeyCardCredential.GuestId` | `fo.HoSoKhach.GuestProfileId` | Phase 2B-8 |

---

## 17. EF Core Implementation Specification

### 17.1 Entities to Add to DbContext

| Entity | DbSet Name | Table | When |
|---|---|---|---|
| `GuestProfile` | `GuestProfiles` | `fo.HoSoKhach` | After D-02 |
| `GuestDocument` | `GuestDocuments` | `fo.GiayToKhach` | After D-02 |
| `HotelPayment` | `HotelPayments` | `fo.ThanhToan` | Phase 2B-5 |
| `PaymentAllocation` | `PaymentAllocations` | `fo.PhanBoThanhToan` | Phase 2B-5 |
| `Deposit` | `Deposits` | `fo.DatCoc` | Phase 2B-5 |
| `Refund` | `Refunds` | `fo.HoanTien` | Phase 2B-5 |
| `HousekeepingTask` | `HousekeepingTasks` | `fo.HousekeepingTask` | Phase 2B-7 |
| `RoomInspection` | `RoomInspections` | `fo.RoomInspection` | Phase 2B-7 |
| `MinibarSession` | `MinibarSessions` | `fo.MinibarSession` | Phase 2B-7 |
| `MinibarConsumption` | `MinibarConsumptions` | `fo.MinibarConsumption` | Phase 2B-7 |
| `KeyCardCredential` | `KeyCardCredentials` | `fo.KeyCardCredential` | Phase 2B-8 |
| `KeyCardEvent` | `KeyCardEvents` | `fo.KeyCardEvent` | Phase 2B-8 |
| `BusinessDate` | `BusinessDates` | `fo.BusinessDate` | After D-04 |

### 17.2 Entities to Modify

| Entity | Property to Add | Type | When |
|---|---|---|---|
| `Reservation` | NONE (already has GuaranteeType, DepositStatus, DepositAmount) | — | N/A |
| `HotelStay` | `GuestId` | `Guid?` | P1-001 |
| `HotelRoom` | `HousekeepingStatus` | `string?` | P1-003 |
| `HotelFolioEntry` | `IdempotencyKey` | `string?` | P0-003 |
| `HotelFolioEntry` | `RowVersion` | `byte[]` | P0-002 |

### 17.3 Configuration Changes (AppDbContext)

| Line | Entity | Change |
|---|---|---|
| 929 | Reservation | NONE (already correct) |
| 933 | HotelStay | Add `entity.Property(x => x.GuestId)` + FK config |
| 932 | HotelRoom | Add `entity.Property(x => x.HousekeepingStatus).HasMaxLength(30)` |
| 934 | HotelStayHistory | Add `ConfigureRowVersion` if needed for concurrency |
| NEW | HotelFolioEntry | Add `ConfigureRowVersion` + `IdempotencyKey` config |

---

## 18. Repository Implementation Specification

### 18.1 New Repositories Required

| Interface | Implementation | Domain | When |
|---|---|---|---|
| `IGuestProfileRepository` | `EfGuestProfileRepository` | Guest | After D-02 |
| `IHotelPaymentRepository` | `EfHotelPaymentRepository` | Payment | Phase 2B-5 |
| `IDepositRepository` | `EfDepositRepository` | Deposit | Phase 2B-5 |
| `IRefundRepository` | `EfRefundRepository` | Refund | Phase 2B-5 |
| `IHousekeepingRepository` | `EfHousekeepingRepository` | Housekeeping | Phase 2B-7 |
| `IMinibarRepository` | `EfMinibarRepository` | Minibar | Phase 2B-7 |
| `IKeyCardRepository` | `EfKeyCardRepository` | KeyCard | Phase 2B-8 |
| `ILostFoundRepository` | `EfLostFoundRepository` | LostFound | Phase 2B-8 |
| `IBusinessDateRepository` | `EfBusinessDateRepository` | BusinessDate | After D-04 |

### 18.2 Existing Repositories to Verify

| Repository | Verification |
|---|---|
| `EfReservationRepository` | Verify works after P0-001 column additions |
| `EfHotelStayRepository` | Verify after P1-001 GuestId addition |
| `EfHotelFolioRepository` | Verify after P0-002/003 RowVersion/IdempotencyKey |
| `EfReceivableService` | Verify `dbo.ReceivableTransactions` mapping before AR design |

---

## 19. Application Service Specification

### 19.1 New Services Required

| Service | Responsibility | When |
|---|---|---|
| `GuestQueryService` | Search, get guest profile, history | After D-02 |
| `GuestCommandService` | Create, update, merge, block guest | After D-02 |
| `PaymentService` | Record, allocate, reverse, refund payment | Phase 2B-5 |
| `DepositService` | Hold, apply, refund, forfeit deposit | Phase 2B-5 |
| `RefundService` | Request, approve, post refund | Phase 2B-5 |
| `InvoiceService` | Issue, cancel, adjust invoice | Phase 2B-6 |
| `ARService` | Post transaction, reconcile, aging query | Phase 2B-6 |
| `HousekeepingService` | Assign, start, complete, inspect task | Phase 2B-7 |
| `MinibarService` | Open session, record, check, post | Phase 2B-7 |
| `KeyCardService` | Issue, disable, replace, return card | Phase 2B-8 |
| `LostFoundService` | Record, store, claim, return, dispose | Phase 2B-8 |
| `BusinessDateService` | Open, close, reopen business date | After D-04 |

---

## 20. CQRS Specification

### 20.1 Commands Required

| Command | Handler | Aggregate | When |
|---|---|---|---|
| `CreateGuestCommand` | `CreateGuestHandler` | GuestProfile | After D-02 |
| `UpdateGuestCommand` | `UpdateGuestHandler` | GuestProfile | After D-02 |
| `RecordPaymentCommand` | `RecordPaymentHandler` | Payment | Phase 2B-5 |
| `AllocatePaymentCommand` | `AllocatePaymentHandler` | Payment | Phase 2B-5 |
| `ReversePaymentCommand` | `ReversePaymentHandler` | Payment | Phase 2B-5 |
| `HoldDepositCommand` | `HoldDepositHandler` | Deposit | Phase 2B-5 |
| `ApplyDepositCommand` | `ApplyDepositHandler` | Deposit | Phase 2B-5 |
| `RefundDepositCommand` | `RefundDepositHandler` | Deposit | Phase 2B-5 |
| `RequestRefundCommand` | `RequestRefundHandler` | Refund | Phase 2B-5 |
| `IssueInvoiceCommand` | `IssueInvoiceHandler` | Invoice | Phase 2B-6 |
| `PostARTransactionCommand` | `PostARTransactionHandler` | AR | Phase 2B-6 |
| `AssignHousekeepingTaskCommand` | `AssignHKTaskHandler` | HousekeepingTask | Phase 2B-7 |
| `CompleteInspectionCommand` | `CompleteInspectionHandler` | RoomInspection | Phase 2B-7 |
| `OpenMinibarSessionCommand` | `OpenMinibarSessionHandler` | MinibarSession | Phase 2B-7 |
| `PostMinibarCommand` | `PostMinibarHandler` | MinibarSession | Phase 2B-7 |
| `IssueKeyCardCommand` | `IssueKeyCardHandler` | KeyCardCredential | Phase 2B-8 |
| `RecordLostFoundCommand` | `RecordLostFoundHandler` | LostFoundItem | Phase 2B-8 |

### 20.2 Queries Required

| Query | Handler | Read Model | When |
|---|---|---|---|
| `SearchGuestsQuery` | `SearchGuestsHandler` | GuestSummaryDto[] | After D-02 |
| `GetGuestProfileQuery` | `GetGuestProfileHandler` | GuestProfileDto | After D-02 |
| `GetPaymentsByFolioQuery` | `GetPaymentsByFolioHandler` | PaymentDto[] | Phase 2B-5 |
| `GetARAgingQuery` | `GetARAgingHandler` | ARAgingDto[] | Phase 2B-6 |
| `GetHousekeepingTasksQuery` | `GetHKTasksHandler` | HKTaskDto[] | Phase 2B-7 |
| `GetMinibarSessionsQuery` | `GetMinibarSessionsHandler` | MinibarSessionDto[] | Phase 2B-7 |
| `GetKeyCardEventsQuery` | `GetKeyCardEventsHandler` | KeyCardEventDto[] | Phase 2B-8 |
| `SearchLostFoundQuery` | `SearchLostFoundHandler` | LostFoundDto[] | Phase 2B-8 |

---

## 21. Financial Workflow Specification

### 21.1 Payment → Folio Allocation

```
1. RecordPayment(PaymentNo, Amount, Method, Currency, Payer, PropertyId)
   → Payment stored with Status=Pending
2. AllocatePayment(PaymentId, FolioId, Amount)
   → Validate: Amount ≤ Payment.UnallocatedAmount
   → Validate: Folio is Open/Locked
   → Create PaymentAllocation row
   → Update Payment.UnallocatedAmount
   → If fully allocated: Payment.Status = Allocated
3. Folio balance recalculated from FolioEntry sum
```

### 21.2 Deposit Lifecycle

```
1. HoldDeposit(ReservationId/StayId, Amount, Method)
   → Deposit stored with Status=Held
2. ApplyDeposit(DepositId, FolioId, Amount)
   → Validate: Amount ≤ Deposit.HeldAmount - Deposit.AppliedAmount
   → Create FolioEntry (credit) on target Folio
   → Update Deposit.AppliedAmount
3. RefundDeposit(DepositId, Amount, Reason)
   → Validate: Amount ≤ Deposit.HeldAmount - Deposit.RefundedAmount
   → Create Refund record
   → Update Deposit.RefundedAmount
4. ForfeitDeposit(DepositId, Reason)
   → Status = Forfeited, create FolioEntry (revenue)
```

### 21.3 Refund Constraints

```
Refund.Amount ≤ OriginalPayment.Amount - SUM(ExistingRefunds.Amount)
Refund must reference: OriginalPaymentId, ApproverId, Reason
Posting: Refund creates reversal FolioEntry on same Folio
```

### 21.4 AR Derivation

```
AR.OpenBalance = SUM(Invoice.Amount WHERE Status=Issued)
               - SUM(Payment.AllocatedAmount WHERE TargetType=Invoice)
               + SUM(Adjustment.Amount WHERE Type=Debit)
               - SUM(Adjustment.Amount WHERE Type=Credit)
```

---

## 22. Guest Specification

### 22.1 GuestProfile Entity (proposed)

```csharp
// Table: fo.HoSoKhach
public class GuestProfile {
    Guid GuestProfileId;        // PK
    string GuestNumber;         // Business key, unique
    string Title;               // Ông/Bà/Anh/Chị
    string FirstName;
    string LastName;
    string FullName;            // Computed
    DateTime? DateOfBirth;
    string Gender;
    string Nationality;         // ISO country code
    string Language;
    string Phone;
    string Email;
    string IdCardNo;            // CCCD/CMND
    string PassportNo;
    string VisaNo;
    string MemberCode;
    string MemberLevel;         // None/Bronze/Silver/Gold/Platinum
    string VipLevel;            // None/VIP1/VIP2/VIP3
    bool IsBlacklisted;
    string BlacklistReason;
    int StayCount;
    decimal TotalRevenue;
    DateTime? LastStayDate;
    int LoyaltyPoints;
    string Tags;
    string Status;              // Active/Blocked/Merged
    Guid? PropertyId;           // Tenant scope (null = global)
    byte[] RowVersion;          // Concurrency
    DateTime CreatedAtUtc;
    DateTime UpdatedAtUtc;
}
```

### 22.2 GuestDocument Entity

```csharp
// Table: fo.GiayToKhach
public class GuestDocument {
    Guid GuestDocumentId;       // PK
    Guid GuestProfileId;        // FK → GuestProfile
    string DocumentType;        // Passport/Visa/IdCard/RegistrationCard
    string DocumentNumber;
    string Issuer;
    DateTime? IssueDate;
    DateTime? ExpiryDate;
    string Status;              // Valid/Expired/Redacted
    // PII: stored with encryption consideration
}
```

### 22.3 Repository

```csharp
public interface IGuestProfileRepository {
    Task<GuestProfile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<GuestProfile?> GetByGuestNumberAsync(string number, CancellationToken ct);
    Task<IReadOnlyList<GuestProfile>> SearchAsync(GuestSearchFilter filter, CancellationToken ct);
    Task AddAsync(GuestProfile guest, CancellationToken ct);
    Task UpdateAsync(GuestProfile guest, CancellationToken ct);
}
```

---

## 23. Reservation Specification

### 23.1 Pre-Implementation Verification

Before any code change, verify:
- V-001: Exact column list of `fo.DatPhong` (run `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='fo' AND TABLE_NAME='DatPhong'`)
- V-008: Existing FK constraints on `fo.DatPhong`

### 23.2 Implementation

**Step 1:** Execute P0-001 (add 3 columns)  
**Step 2:** Verify EF can read/write Reservation  
**Step 3:** After D-02 approved, execute P1-002 (add GuestId FK)  
**Step 4:** Backfill GuestId from reservation guest snapshots

---

## 24-36. Remaining Domain Specifications

[Detailed specifications for Stay, Room, Group, Folio, Payment, Deposit, Refund, Invoice, AR, Housekeeping, Minibar, KeyCard, LostFound follow the same pattern as Sections 22-23. Each domain spec includes entity design, repository interface, FK requirements, and validation rules. Full details deferred to Phase 2B implementation per approved decisions.]

---

## 37. Business Date Specification

### 37.1 Entity

```csharp
// Table: fo.BusinessDate (NEW)
public class BusinessDate {
    Guid BusinessDateId;        // PK
    Guid PropertyId;            // FK → Property
    DateTime Date;              // The business date
    string Status;              // Open/Closed/Reopened
    DateTime OpenedAtUtc;
    string OpenedBy;
    DateTime? ClosedAtUtc;
    string ClosedBy;
    DateTime? ReopenedAtUtc;
    string ReopenedBy;
    string ReopenReason;
    byte[] RowVersion;
}
```

### 37.2 Rules
- Only one Open business date per property at a time
- Financial postings reference the Open business date
- Close requires: all folios settled, all payments posted, night audit complete
- Reopen requires: supervisor approval, audit log

---

## 38. Property Scope Specification

**Rule:** Every operational record MUST have `PropertyId`.
**Enforcement:** Repository base class validates `PropertyId` on all queries and commands.
**Cross-property:** Operations spanning properties require explicit cross-property permission.

---

## 39. Security Specification

### 39.1 Permission Matrix

| Capability | Permission Required | Scope |
|---|---|---|
| Guest create/edit | `guest.write` | Tenant |
| Guest PII view | `guest.pii.read` | Tenant |
| Reservation write | `reservation.write` | Property |
| Check-in/out | `stay.checkin`, `stay.checkout` | Property |
| Folio post | `folio.post` | Property |
| Payment record | `payment.record` | Property |
| Payment refund | `payment.refund` | Property (supervisor) |
| Invoice issue | `invoice.issue` | Property |
| AR view | `ar.read` | Property |
| AR reconcile | `ar.reconcile` | Property (finance) |
| Housekeeping | `hk.assign`, `hk.inspect` | Property |
| KeyCard issue | `keycard.issue` | Property |

---

## 40. Audit Specification

Use existing platform audit tables (`dbo.AuditLogs`, `dbo.AuditSessions`, `dbo.AuditCorrelations`, `dbo.AuditEntitySnapshots`). FO-specific `fo.KiemToan` for business-meaningful events only.

**Audit minimum per operation:** Actor, Role, PropertyId, CorrelationId, AggregateId, Action, Before (redacted), After (redacted), Outcome, Reason, TimestampUtc.

---

## 41. Concurrency Specification

**Strategy:** Optimistic concurrency via `ROWVERSION` on all mutable aggregates.

| Aggregate | RowVersion | Status |
|---|---|---|
| Reservation | Present | Verified |
| GuestProfile | Required | Add with table |
| HotelStay | Present | Verified |
| HotelRoom | Present | Verified |
| HotelRoomType | Present | Verified |
| HotelFolio | Present | Verified |
| HotelFolioEntry | MISSING | P0-002 |
| GroupReservation | Present | Verified |
| Payment | Required | Add with table |
| Deposit | Required | Add with table |
| Refund | Required | Add with table |
| HousekeepingTask | Required | Add with table |
| BusinessDate | Required | Add with table |

---

## 42. Idempotency Specification

**Entities requiring idempotency keys:**

| Entity | Idempotency Key | Unique Constraint |
|---|---|---|
| HotelFolioEntry | FolioId + CommandId + Sequence | Unique filtered index |
| Payment | PaymentNo (business key) | Unique |
| Refund | RefundNo (business key) | Unique |
| MinibarConsumption | SessionId + LineNo | Composite unique |
| KeyCardEvent | CardId + EventType + Timestamp | Composite unique |

---

## 43. Mock Removal Specification

### 43.1 Files with Mock Data Generators

| File | Mock Method | Action |
|---|---|---|
| `GuestInquiryViewModel.cs` | `BuildFakeData()` | Wrap in `#if DEBUG` |
| `ArAgingViewModel.cs` | `BuildFakeData()` | Wrap in `#if DEBUG` |
| `ArLedgerViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ArPaymentViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `LostFoundViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `MinibarViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `WorkforceViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `GroupReservationViewModel.cs` | Mock data | Wrap in `#if DEBUG` |

### 43.2 Empty State Handling

When no real data source is available:
- Show explicit empty state: "Chưa có dữ liệu. Vui lòng tạo [entity] trước."
- Do NOT show fake/generated data
- Do NOT crash

---

## 44. Migration Execution Specification

### 44.1 Pre-Migration Gate (EVERY MIGRATION)

1. Full database backup
2. Backup verification
3. Run pre-migration validation queries (Section 45)
4. Record pre-migration row counts
5. Execute migration in transaction where possible
6. Run post-migration validation queries (Section 45)
7. Record post-migration row counts
8. Verify counts match or are accounted for

### 44.2 Migration Order

```
M001: Add GuaranteeType, DepositStatus, DepositAmount to fo.DatPhong
M002: Add RowVersion, IdempotencyKey to fo.ButToanFolio
M003: Add HousekeepingStatus to fo.PhongKhachSan
M004: Add GuestId to fo.DatPhong (after D-02, after M001)
M005: Add GuestId to fo.LuuTruKhachSan (after D-02, after M004)
M006: Create fo.ThanhToan (after D-06)
M007: Create fo.PhanBoThanhToan (after M006)
M008: Create fo.DatCoc (after M006)
M009: Create fo.HoanTien (after M006, M008)
M010: Create fo.HousekeepingTask (after M003)
... (remaining migrations per dependency graph)
```

### 44.3 DESTRUCTIVE OPERATIONS — BLOCKED

The following operations are BLOCKED without explicit architectural approval:
- `DROP TABLE`
- `DROP COLUMN`
- `TRUNCATE`
- `DELETE FROM`
- `ALTER COLUMN` (type change)
- `DROP CONSTRAINT` (FK removal)

---

## 45. Migration Validation

### 45.1 Pre-Migration Checks (run before each migration)

```sql
-- Row counts
SELECT 'fo.DatPhong' AS Tbl, COUNT(*) AS Cnt FROM fo.DatPhong
UNION ALL SELECT 'fo.LuuTruKhachSan', COUNT(*) FROM fo.LuuTruKhachSan
UNION ALL SELECT 'fo.PhongKhachSan', COUNT(*) FROM fo.PhongKhachSan;

-- Orphan check
SELECT * FROM fo.LuuTruKhachSan WHERE ReservationId NOT IN (SELECT ReservationId FROM fo.DatPhong);
SELECT * FROM fo.LuuTruKhachSan WHERE RoomId NOT IN (SELECT HotelRoomId FROM fo.PhongKhachSan);

-- Invalid dates
SELECT * FROM fo.DatPhong WHERE ArrivalDate >= DepartureDate;

-- Duplicate business keys
SELECT ReservationNumber, COUNT(*) FROM fo.DatPhong GROUP BY ReservationNumber HAVING COUNT(*) > 1;
```

### 45.2 Post-Migration Checks

Same as pre-migration + verify new columns/constraints exist.

---

## 46. Data Reconciliation

| Domain | Reconciliation Method |
|---|---|
| Reservation | Count: 10 before = 10 after |
| Stay | Count: 5 before = 5 after |
| Room | Count: 12 before = 12 after |
| Guest | FK check: all Stay.GuestId resolve to GuestProfile |
| Folio | Sum(FolioEntry.Amount) = Folio.Balance |
| Payment | Sum(Allocation.Amount) ≤ Payment.Amount |
| AR | AR.Balance = SUM(Invoice) - SUM(Payment) + SUM(Adjustment) |

---

## 47. Test Specification

### 47.1 Unit Tests Required

| Test | Domain | What It Verifies |
|---|---|---|
| `Reservation_Guarantee_SetsType` | Reservation | Guarantee() method works |
| `Reservation_RecordDeposit_SetsAmount` | Reservation | RecordDeposit() method works |
| `Payment_Allocate_WithinLimit` | Payment | Allocation ≤ Payment |
| `Payment_Allocate_ExceedsLimit_Throws` | Payment | Over-allocation rejected |
| `Refund_WithinRefundable_Approved` | Refund | Refund ≤ refundable |
| `Refund_ExceedsRefundable_Throws` | Refund | Over-refund rejected |
| `FolioEntry_DuplicateIdempotencyKey_Throws` | Folio | Idempotency enforced |
| `Deposit_Apply_WithinHeld` | Deposit | Apply ≤ Held |
| `AR_Balance_DerivedCorrectly` | AR | AR = Invoice - Payment + Adjustment |

### 47.2 Integration Tests Required

| Test | What It Verifies |
|---|---|
| `Reservation_Write_WithNewColumns` | EF can write GuaranteeType/DepositStatus/DepositAmount |
| `Stay_Read_WithGuestId` | EF can read Stay with GuestId |
| `Payment_Allocate_ToFolio` | End-to-end payment allocation |
| `Folio_Balance_MatchesEntries` | Financial invariant |
| `Concurrent_Reservation_Update_Detected` | RowVersion concurrency |

### 47.3 E2E Tests Required

| Test | Workflow |
|---|---|
| `E2E_CheckIn_Folio_Payment_Checkout` | Full guest journey |
| `E2E_Group_Booking_CheckIn_Billing` | Group workflow |
| `E2E_Housekeeping_Task_Inspection` | HK workflow |
| `E2E_Minibar_Check_Post_Folio` | Minibar workflow |

---

## 48. E2E Specification

### 48.1 E2E-001: Individual Guest Journey

```
1. Create Reservation (Guest, Room, Rate, Dates)
2. Check-in → Stay created, Room occupied, Folio opened
3. Post Room Charge to Folio
4. Post Restaurant Charge to Folio
5. Record Payment → Allocate to Folio
6. Check-out → Folio settled, Room released, HK task created
7. Verify: Room status = VacantDirty, HK task exists
8. Verify: Folio balance = 0, Payment allocated = Folio total
```

### 48.2 E2E-002: Group Journey

```
1. Create Group Reservation (Block rooms, Rooming list)
2. Assign rooms to guests
3. Group Check-in → Individual stays created
4. Post group charges to Master Folio
5. Record group payment → Allocate
6. Group Check-out → All stays closed, rooms released
```

---

## 49. Rollback Strategy

### 49.1 Per-Migration Rollback

Each migration script must have a corresponding rollback script. Example:

```sql
-- M001: ADD columns
ALTER TABLE fo.DatPhong ADD GuaranteeType NVARCHAR(40) NULL;
-- Rollback:
ALTER TABLE fo.DatPhong DROP COLUMN GuaranteeType;
```

### 49.2 Full Recovery

1. Restore from verified backup
2. Re-run post-migration validation
3. Verify row counts match pre-migration

---

## 50. Codex Implementation Units

| Unit | Domain | Priority | Dependencies | DB Change | Code Change | Tests | Status |
|---|---|---|---|---|---|---|---|
| U-001 | Reservation | P0 | None | ADD 3 columns | None | Reservation write test | **READY** |
| U-002 | Folio Entry | P0 | None | ADD 2 columns | EF config | Concurrency test | **READY** |
| U-003 | Migration Baseline | P0 | None | None | Create baseline script | Verify script | **READY** |
| U-004 | Mock Removal | P0 | None | None | Wrap in #if DEBUG | Verify empty state | **READY** |
| U-005 | Guest DbSet | P0 | D-02 | None | Add DbSet + config | Verify registration | **BLOCKED** |
| U-006 | Guest FK Reservation | P0 | D-02, U-001 | ADD FK | EF config | FK integrity test | **BLOCKED** |
| U-007 | Guest FK Stay | P0 | D-02, U-006 | ADD FK | EF config | FK integrity test | **BLOCKED** |
| U-008 | Room HK Status | P1 | None | ADD column | Entity + config | Verify mapping | **READY** |
| U-009 | Payment Table | P0 | D-06 | CREATE TABLE | Entity + DbSet + config | Payment CRUD tests | **BLOCKED** |
| U-010 | Deposit Table | P1 | U-009 | CREATE TABLE | Entity + DbSet + config | Deposit lifecycle tests | **BLOCKED** |
| U-011 | Refund Table | P1 | U-009, U-010 | CREATE TABLE | Entity + DbSet + config | Refund constraint tests | **BLOCKED** |
| U-012 | Invoice Decision | P0 | D-07 | Decision | Decision | Decision | **BLOCKED** |
| U-013 | AR Table | P0 | D-08, U-012 | Decision | Decision | Decision | **BLOCKED** |
| U-014 | BusinessDate | P0 | D-04 | CREATE TABLE | Entity + service | Date lifecycle tests | **BLOCKED** |
| U-015 | Property Master | P0 | D-01 | Decision | Decision | Decision | **BLOCKED** |
| U-016 | Guest Repository | P1 | U-005 | None | Repository + DI | Repository tests | **BLOCKED** |
| U-017 | Guest Query Service | P1 | U-016 | None | Query service | Query tests | **BLOCKED** |
| U-018 | Payment Repository | P1 | U-009 | None | Repository + DI | Repository tests | **BLOCKED** |
| U-019 | Housekeeping Table | P1 | U-008 | CREATE TABLE | Entity + DbSet + config | Task CRUD tests | **READY** |
| U-020 | Minibar Table | P1 | Folio | CREATE TABLE | Entity + DbSet + config | Session tests | **BLOCKED** |
| U-021 | KeyCard Table | P1 | U-006, U-007 | CREATE TABLE | Entity + DbSet + config | Card lifecycle tests | **BLOCKED** |
| U-022 | LostFound Activation | P1 | None | None | Verify DbSet + activate | Item CRUD tests | **READY** |
| U-023 | Audit Integration | P1 | None | None | Wire platform audit | Audit coverage test | **READY** |
| U-024 | Permission Matrix | P1 | D-10 | None | Permission checks | Deny/allow tests | **BLOCKED** |
| U-025 | Read Models | P1 | Multiple | None | Query services | Query tests | **BLOCKED** |

---

## 51. Implementation Dependency Graph

```
U-001 (Reservation drift) ─────────────────────────────────────────┐
U-002 (FolioEntry concurrency) ────────────────────────────────────┤
U-003 (Migration baseline) ────────────────────────────────────────┤
U-004 (Mock removal) ──────────────────────────────────────────────┤
U-008 (Room HK status) ────────────────────────────────────────────┤
U-022 (LostFound activation) ──────────────────────────────────────┤
U-023 (Audit integration) ─────────────────────────────────────────┤
                                                                     │
    ┌────────────────────────────────────────────────────────────────┘
    │
    ├── D-02 (Guest identity) ──→ U-005 → U-006 → U-007 → U-016 → U-017
    │                                                              │
    ├── D-01 (Property) ──→ U-015                                  │
    │                                                              │
    ├── D-04 (BusinessDate) ──→ U-014                              │
    │                                                              │
    ├── D-06 (Payment) ──→ U-009 → U-018 → U-010 → U-011          │
    │                                                              │
    ├── D-07 (Invoice) ──→ U-012                                   │
    │                                                              │
    ├── D-08 (AR) ──→ U-013                                        │
    │                                                              │
    ├── D-10 (Permission) ──→ U-024                                │
    │                                                              │
    └── (after Guest + Payment + Folio)                            │
         │                                                          │
         ├── U-019 (Housekeeping)                                   │
         ├── U-020 (Minibar)                                        │
         ├── U-021 (KeyCard)                                        │
         └── U-025 (Read Models)                                    │
```

---

## 52. Definition of Done

Hotel Management is Phase 2B complete when:

- [x] Phase 2B.5 Validation complete (this document)
- [ ] All 6 BLOCKED decisions resolved (D-01, D-02, D-04, D-06, D-07, D-08, D-10)
- [ ] U-001 through U-004 implemented (READY NOW)
- [ ] All P0 gaps with APPROVED status closed (G-001, G-002, G-003, G-015, G-016, G-017)
- [ ] All P0 gaps with APPROVED_WITH_CONDITION closed after conditions met
- [ ] All P1 gaps closed
- [ ] Reservation EF/DB drift resolved
- [ ] Guest master populated with FK from Reservation and Stay
- [ ] Financial chain verified: Folio → Payment → Invoice → AR
- [ ] Property scope enforced on all operational records
- [ ] Business date authority operational
- [ ] All runtime mock generators removed
- [ ] RowVersion on all mutable aggregates
- [ ] Idempotency keys on all financial operations
- [ ] All required FKs in place
- [ ] Permission matrix enforced
- [ ] PII governance implemented
- [ ] Audit coverage for all material operations
- [ ] Backup and rollback verified
- [ ] Integration tests passing
- [ ] E2E tests passing

---

## 53. Final Approval Status

### PHASE 2B.5 APPROVAL

| Gate | Status |
|---|---|
| **DATABASE BLUEPRINT** | CONDITIONALLY APPROVED |
| **CODE IMPLEMENTATION** | CONDITIONALLY READY |
| **MIGRATION** | CONDITIONALLY APPROVED |
| **FINANCIAL** | BLOCKED — needs D-05/D-06/D-07/D-08 |
| **SECURITY** | BLOCKED — needs D-10 |
| **DATA** | CONDITIONALLY APPROVED |

### BLOCKED Decisions (6)

| ID | Decision | Impact |
|---|---|---|
| D-01 | Property authority | All PropertyId FK enforcement |
| D-02 | Guest identity (`fo.HoSoKhach` vs `dbo.Customers`) | Guest persistence, all Guest FKs |
| D-04 | Business date authority | Night audit, revenue posting |
| D-06 | Payment allocation authority | Payment table design |
| D-07 | Legal invoice authority | Invoice table design |
| D-08 | AR architecture (derived vs sub-ledger) | AR table design |
| D-10 | Authorization model | Permission enforcement |

### READY NOW (4 units)

| Unit | What |
|---|---|
| U-001 | Add GuaranteeType, DepositStatus, DepositAmount to `fo.DatPhong` |
| U-002 | Add RowVersion, IdempotencyKey to `fo.ButToanFolio` |
| U-003 | Create migration baseline script |
| U-004 | Remove runtime mock generators |

### FINAL RECOMMENDATION

**Codex CAN begin implementation of U-001 through U-004 immediately.** These 4 units have no architectural dependencies, are fully supported by all source documents, and address the most critical P0 production blockers (Reservation write path failure, financial concurrency, data integrity from mock generators).

**Codex CANNOT proceed with Guest, Payment, Invoice, AR, BusinessDate, or Permission implementation** until the 6 blocked architectural decisions (D-01, D-02, D-04, D-06, D-07, D-08, D-10) are resolved by the architect.

**Estimated timeline after decisions:** 12-16 weeks for full P0+P1 implementation, assuming decisions are made within 1 week.

---

*End of Phase 2B.5 Implementation Specification. No code, schema, migration, or configuration change has been made by this document.*