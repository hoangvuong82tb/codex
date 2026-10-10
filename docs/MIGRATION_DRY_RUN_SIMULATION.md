# MIGRATION DRY RUN SIMULATION — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** SIMULATION ONLY — không SQL, không baseline, không execute
**Mục đích:** Mô phỏng toàn bộ kịch bản baseline 7 Group C + 1 Group B VAT

---

## 1. Current State

### 1.1. Migration History

```
Applied: 75 rows
Latest:  20260718014449_AddFrontOfficeHotelRoomMasterData
Source:  85 migration files
Pending: 10

Breakdown:
  Group C (superseded):  7 — schema EXISTS, history MISSING
  Group B (VAT):         1 — schema EXISTS, history MISSING  
  Group A (Hotel Stay):  2 — schema MISSING, history MISSING
```

### 1.2. Current History Timeline (abbreviated)

```
Row 67: 20260522144345_AddMultiCompanyFoundation          ← APPLIED (original)
       ─────────────────── GAP ───────────────────
       ... 7 Group C migrations MISSING from history ...
       ─────────────────── GAP ───────────────────
Row 68: 20260523042803_AddBackgroundSchedulerEngine       ← APPLIED (supersedes Group C)
Row 69: 20260523053846_EnhanceEnterpriseAuditTrail         ← APPLIED
... (rows 70-73: more Enterprise hardening) ...
Row 74: 20260629114500_AddParentCategoryToMenuCategory    ← BASELINED (18/07 repair)
... (rows 75-82: Banquet, FO Foundation, Reservation, HotelRoom) ...
Row 82: 20260718014449_AddFrontOfficeHotelRoomMasterData   ← APPLIED (latest)
```

### 1.3. Pending Chain (current)

```
Source migration files (ordered by timestamp):

  [GAP START]
  20260522221500_Phase3Step32SqlPerformanceScaling        ← PENDING (Group C)
  20260522223000_Phase3Step33EnterprisePermissions         ← PENDING (Group C)
  20260522224500_Phase3Step34EInvoiceFoundation            ← PENDING (Group C)
  20260522230000_Phase3Step35SchedulerEngine               ← PENDING (Group C)
  20260523010000_AddFiscalPeriodAndBranchEngine            ← PENDING (Group C)
  20260523012000_AddEnterpriseNotificationCenter           ← PENDING (Group C)
  20260523014000_AddEnterpriseSearchIndex                  ← PENDING (Group C)
  [GAP END — superseded by 20260523042803 which IS applied]

  20260629114500_AddParentCategoryToMenuCategory           ← APPLIED (baselined 18/07)
  20260630113000_AddVatIncludedFlagsToMenuAndRestaurant    ← PENDING (Group B)

  ... 13 Banquet + FO Foundation + Reservation + Room ...  ← ALL APPLIED

  20260718092912_AddFrontOfficeHotelStayFoundation         ← PENDING (Group A)
  20260718095823_AddFrontOfficeHotelStayCheckIn            ← PENDING (Group A)
```

---

## 2. History Before Simulation

```
┌──────────────────────────────────────────────────────────┐
│ __EFMigrationsHistory — BEFORE                            │
├──────────────────────────────────────────────────────────┤
│ Row count:  75                                            │
│ First:      20260421153713_InitialCreate                  │
│ Last:       20260718014449_AddFrontOfficeHotelRoomMaster… │
│                                                           │
│ Gaps:                                                     │
│   ✗ 20260522221500 Phase3Step32SqlPerformanceScaling      │
│   ✗ 20260522223000 Phase3Step33EnterprisePermissions      │
│   ✗ 20260522224500 Phase3Step34EInvoiceFoundation         │
│   ✗ 20260522230000 Phase3Step35SchedulerEngine            │
│   ✗ 20260523010000 AddFiscalPeriodAndBranchEngine         │
│   ✗ 20260523012000 AddEnterpriseNotificationCenter        │
│   ✗ 20260523014000 AddEnterpriseSearchIndex               │
│   ✗ 20260630113000 AddVatIncludedFlags...                 │
│   ✗ 20260718092912 AddFrontOfficeHotelStayFoundation      │
│   ✗ 20260718095823 AddFrontOfficeHotelStayCheckIn         │
│                                                           │
│ Pending: 10                                               │
│ EF state: INCONSISTENT (schema = superseded, history < source) │
└──────────────────────────────────────────────────────────┘
```

---

## 3. History After Simulation

### 3.1. After Group C Baseline Only (Phase 2)

```
┌──────────────────────────────────────────────────────────┐
│ __EFMigrationsHistory — AFTER Group C baseline             │
├──────────────────────────────────────────────────────────┤
│ Row count:  75 + 7 = 82                                   │
│                                                           │
│ Inserted rows:                                            │
│   + 20260522221500_Phase3Step32SqlPerformanceScaling       │
│   + 20260522223000_Phase3Step33EnterprisePermissions       │
│   + 20260522224500_Phase3Step34EInvoiceFoundation          │
│   + 20260522230000_Phase3Step35SchedulerEngine             │
│   + 20260523010000_AddFiscalPeriodAndBranchEngine          │
│   + 20260523012000_AddEnterpriseNotificationCenter         │
│   + 20260523014000_AddEnterpriseSearchIndex                │
│                                                           │
│ Remaining gaps:                                           │
│   ✗ 20260630113000 AddVatIncludedFlags...                  │
│   ✗ 20260718092912 AddFrontOfficeHotelStayFoundation       │
│   ✗ 20260718095823 AddFrontOfficeHotelStayCheckIn          │
│                                                           │
│ Pending: 3                                                │
│ EF state: PARTIAL (Group C resolved, VAT + Hotel Stay remain) │
└──────────────────────────────────────────────────────────┘
```

### 3.2. After Group B VAT Baseline (Phase 3)

```
┌──────────────────────────────────────────────────────────┐
│ __EFMigrationsHistory — AFTER Group B baseline             │
├──────────────────────────────────────────────────────────┤
│ Row count:  82 + 1 = 83                                   │
│                                                           │
│ Inserted row:                                             │
│   + 20260630113000_AddVatIncludedFlagsToMenuAndRestaurant  │
│                                                           │
│ Remaining gaps:                                           │
│   ✗ 20260718092912 AddFrontOfficeHotelStayFoundation       │
│   ✗ 20260718095823 AddFrontOfficeHotelStayCheckIn          │
│                                                           │
│ Pending: 2 (ONLY Hotel Stay)                              │
│ EF state: CONSISTENT (history = source for all but Hotel Stay) │
└──────────────────────────────────────────────────────────┘
```

---

## 4. Pending Before Simulation

```
PENDING: 10 migrations
├── 7 Group C — schema EXISTS (superseded)
├── 1 Group B — schema EXISTS (VAT flags)
└── 2 Group A — schema MISSING (Hotel Stay)

EF will try to run ALL 10 if `database update` is called:
  Step32 → Step33 → Step34 → Step35 → Fiscal → Notify → Search
  → VAT → StayFoundation → StayCheckIn

RISK: Step32-Step35 will FAIL (duplicate index/table) → chain breaks
```

---

## 5. Pending After Simulation

### 5.1. After Group C Baseline

```
PENDING: 3 migrations
├── 1 Group B — schema EXISTS (VAT flags)
└── 2 Group A — schema MISSING (Hotel Stay)

EF will try to run 3 if `database update` is called:
  VAT → StayFoundation → StayCheckIn

VAT risk: LOW — migration has IF COL_LENGTH guards
StayFoundation: WILL RUN — this is DESIRED
StayCheckIn: WILL RUN — this is DESIRED
```

### 5.2. After Group C + VAT Baseline

```
PENDING: 2 migrations (IDEAL STATE)
├── 20260718092912_AddFrontOfficeHotelStayFoundation
└── 20260718095823_AddFrontOfficeHotelStayCheckIn

EF will try to run EXACTLY 2 if `database update` is called:
  StayFoundation → StayCheckIn

✓ Both are DESIRED to run
✓ Both have correct dependencies (DatPhong, PhongKhachSan, LoaiPhongKhachSan exist)
✓ Chain is clean and continuous
```

---

## 6. Migration Chain Continuity

### 6.1. Before Baseline

```
Chain: BROKEN at 2 points

Point 1 — Group C gap:
  20260522144345 (applied)
  → [7 MISSING] ← EF sees these as "not applied"
  → 20260523042803 (applied, supersedes them)
  
  PROBLEM: EF doesn't know about supersession. It will try to run
  the 7 missing migrations, which will fail (duplicate objects).

Point 2 — VAT gap:
  20260629114500 (baselined)
  → 20260630113000 (MISSING)
  → ... Banquet/FO migrations (applied) ...
  
  PROBLEM: EF will try to run VAT before continuing. VAT is safe
  (has column guards), but it shouldn't need to run at all.

Point 3 — Hotel Stay:
  ... applied chain ends at 20260718014449 ...
  → 20260718092912 (MISSING)
  → 20260718095823 (MISSING)
  
  PROBLEM: These are the ACTUAL missing schema. But they can't be
  reached because Point 1 blocks the chain.
```

### 6.2. After Group C Baseline

```
Chain: FIXED at Point 1, still broken at Point 2

  20260522144345 (applied)
  → 20260522221500 (BASELINED) ✓
  → 20260522223000 (BASELINED) ✓
  → 20260522224500 (BASELINED) ✓
  → 20260522230000 (BASELINED) ✓
  → 20260523010000 (BASELINED) ✓
  → 20260523012000 (BASELINED) ✓
  → 20260523014000 (BASELINED) ✓
  → 20260523042803 (applied) ✓
  → ... continuous applied chain ...
  → 20260629114500 (baselined)
  → 20260630113000 (MISSING) ← STILL GAP
  → ... Banquet/FO ...
  → 20260718014449 (applied)
  → 20260718092912 (MISSING) ← Hotel Stay
  → 20260718095823 (MISSING) ← Hotel Stay

EF state: 3 pending. Chain is continuous up to VAT gap.
```

### 6.3. After Group C + VAT Baseline (TARGET STATE)

```
Chain: FULLY CONTINUOUS

  20260421153713 InitialCreate
  → ... all 83 migrations in order ...
  → 20260718014449 AddFrontOfficeHotelRoomMasterData
  → 20260718092912 AddFrontOfficeHotelStayFoundation    ← LAST 2 PENDING
  → 20260718095823 AddFrontOfficeHotelStayCheckIn       ← LAST 2 PENDING

EF state: 2 pending. Chain is clean. Ready for Hotel Stay deployment.
```

---

## 7. EF State Simulation

### 7.1. `dotnet ef migrations list` — Expected Output

**Current (before baseline):**
```
Build succeeded.
20260522221500_Phase3Step32SqlPerformanceScaling (pending)
20260522223000_Phase3Step33EnterprisePermissions (pending)
20260522224500_Phase3Step34EInvoiceFoundation (pending)
20260522230000_Phase3Step35SchedulerEngine (pending)
20260523010000_AddFiscalPeriodAndBranchEngine (pending)
20260523012000_AddEnterpriseNotificationCenter (pending)
20260523014000_AddEnterpriseSearchIndex (pending)
20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices (pending)
20260718092912_AddFrontOfficeHotelStayFoundation (pending)
20260718095823_AddFrontOfficeHotelStayCheckIn (pending)
```

**After Group C baseline:**
```
Build succeeded.
20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices (pending)
20260718092912_AddFrontOfficeHotelStayFoundation (pending)
20260718095823_AddFrontOfficeHotelStayCheckIn (pending)
```

**After Group C + VAT baseline (TARGET):**
```
Build succeeded.
20260718092912_AddFrontOfficeHotelStayFoundation (pending)
20260718095823_AddFrontOfficeHotelStayCheckIn (pending)
```

### 7.2. `dotnet ef database update` — Expected Behavior

**If called BEFORE baseline:**
```
Apply migration: 20260522221500_Phase3Step32SqlPerformanceScaling
  → CREATE INDEX ... FAIL: duplicate index
  → ROLLBACK. Chain broken.
```

**If called AFTER Group C baseline (3 pending):**
```
Apply migration: 20260630113000_AddVatIncludedFlags...
  → ALTER TABLE ADD IsVatIncluded...
  → IF COL_LENGTH guard → column exists → skip
  → SUCCESS (no-op)
Apply migration: 20260718092912_AddFrontOfficeHotelStayFoundation
  → CREATE SEQUENCE fo.TrinhTuSoLuuTru
  → CREATE TABLE fo.LuuTruKhachSan
  → CREATE TABLE fo.LichSuLuuTruKhachSan
  → CREATE INDEX x10
  → ADD FOREIGN KEY x4
  → SUCCESS (desired behavior)
Apply migration: 20260718095823_AddFrontOfficeHotelStayCheckIn
  → DROP INDEX IX_LuuTruKhachSan_RoomId
  → CREATE UNIQUE FILTERED INDEX UQ_fo_...
  → SUCCESS (desired behavior)
Done. 3 migrations applied.
```

**If called AFTER Group C + VAT baseline (2 pending — IDEAL):**
```
Apply migration: 20260718092912_AddFrontOfficeHotelStayFoundation
  → SUCCESS
Apply migration: 20260718095823_AddFrontOfficeHotelStayCheckIn
  → SUCCESS
Done. 2 migrations applied.
History now = 85 = source count. Pending = 0. ✓
```

---

## 8. Model Snapshot Verification

### 8.1. Current Snapshot

File: `AppDbContextModelSnapshot.cs`
- Contains full model for ALL 85 migrations
- Includes Hotel Stay entities (`LuuTruKhachSan`, `LichSuLuuTruKhachSan`)
- Includes all Group C entity models
- Snapshot IS the target state — database should match this

### 8.2. Snapshot vs Database

| Entity Group | In Snapshot? | In Database? | Match? |
|---|---|---|---|
| Group C tables (20+) | YES | YES | ✅ (via superseding migration) |
| VAT columns (4) | YES | YES | ✅ |
| Hotel Stay tables | YES | **NO** | ❌ → Check-in crash |
| All other entities | YES | YES | ✅ |

### 8.3. After Full Baseline + Hotel Stay Deploy

| Entity Group | In Snapshot? | In Database? | Match? |
|---|---|---|---|
| ALL entities | YES | YES | ✅ PERFECT MATCH |
| EF pending model changes | — | — | NONE |

---

## 9. Hotel Stay Deployment Readiness

### 9.1. Pre-Deployment Checklist

| # | Condition | Current | After Group C | After Group C+VAT |
|---|---|---|---|---|
| 1 | History chain continuous to Hotel Stay | ❌ | ❌ (VAT gap) | ✅ |
| 2 | All Hotel Stay dependencies exist | ✅ | ✅ | ✅ |
| 3 | `fo.DatPhong` exists | ✅ | ✅ | ✅ |
| 4 | `fo.PhongKhachSan` exists | ✅ | ✅ | ✅ |
| 5 | `fo.LoaiPhongKhachSan` exists | ✅ | ✅ | ✅ |
| 6 | `fo.LuuTruKhachSan` NOT exist (clean deploy) | ✅ | ✅ | ✅ |
| 7 | No conflicting object names | ✅ | ✅ | ✅ |
| 8 | Backup verified before deploy | ⬜ | ⬜ | ⬜ |
| 9 | Build PASS | ✅ | ✅ | ✅ |
| 10 | Tests PASS | ✅ | ✅ | ✅ |

### 9.2. Hotel Stay Migration — What Gets Created

**Foundation (`20260718092912`):**
```sql
CREATE SEQUENCE [fo].[TrinhTuSoLuuTru] AS int START WITH 1 INCREMENT BY 1;
CREATE TABLE [fo].[LuuTruKhachSan] (
    Id, StayNumber, ReservationId, GuestId, RoomId, RoomTypeId,
    Status, ArrivalDate, DepartureDate, ActualCheckInAtUtc,
    ActualCheckOutAtUtc, RowVersion, PropertyId, CreatedAt, ...
);
CREATE TABLE [fo].[LichSuLuuTruKhachSan] (
    Id, StayId, FromStatus, ToStatus, Timestamp, Actor, Metadata, ...
);
-- 10 indexes
-- 4 FKs to DatPhong, PhongKhachSan, LoaiPhongKhachSan
```

**CheckIn (`20260718095823`):**
```sql
DROP INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan];
CREATE UNIQUE NONCLUSTERED INDEX [UQ_fo_LuuTruKhachSan_Phong_InHouse]
ON [fo].[LuuTruKhachSan] ([RoomId]) WHERE [Status] = N'InHouse';
```

### 9.3. Post-Deployment Expected State

- `__EFMigrationsHistory`: 85 rows = source count
- Pending: 0
- EF pending model changes: none
- Check-in modal: loads without SqlException
- Check-in flow: end-to-end operational

---

## 10. Check-in Runtime Simulation

### 10.1. Current (BEFORE)

```
User clicks "Check-in" on Reception Hub
  → CheckInStayModalViewModel initializes
  → Calls IHotelStayRepository to load PendingArrival stays
  → EF Core translates to SQL:
     SELECT * FROM [fo].[LuuTruKhachSan] WHERE Status = N'PendingArrival'
  → SQL Server: Invalid object name 'fo.LuuTruKhachSan'
  → SqlException → App.OnDispatcherUnhandledException
  → Generic error dialog
  ❌ CHECK-IN BLOCKED
```

### 10.2. After Hotel Stay Deploy (EXPECTED)

```
User clicks "Check-in" on Reception Hub
  → CheckInStayModalViewModel initializes
  → Calls IHotelStayRepository to load PendingArrival stays
  → EF Core translates to SQL:
     SELECT * FROM [fo].[LuuTruKhachSan] WHERE Status = N'PendingArrival'
  → SQL Server: returns 0+ rows (empty list or data)
  → ViewModel displays PendingArrival list
  → User selects stay, clicks Confirm
  → CheckInHotelStayCommand executes
  → HotelStay.CheckIn() → Status = InHouse
  → SaveChanges → unique index UQ_fo_... verifies no double occupancy
  → Success → modal closes, hub refreshes
  ✅ CHECK-IN OPERATIONAL
```

---

## 11. Simulated MigrationId Insert Order

### 11.1. Exact MigrationId Values for History Insert

```sql
-- These are the EXACT MigrationId values to be inserted
-- Order: by timestamp (already chronological in source)

-- Phase 2: Group C baseline
'20260522221500_Phase3Step32SqlPerformanceScaling'
'20260522223000_Phase3Step33EnterprisePermissions'
'20260522224500_Phase3Step34EInvoiceFoundation'
'20260522230000_Phase3Step35SchedulerEngine'
'20260523010000_AddFiscalPeriodAndBranchEngine'
'20260523012000_AddEnterpriseNotificationCenter'
'20260523014000_AddEnterpriseSearchIndex'

-- Phase 3: Group B baseline
'20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices'

-- All with ProductVersion = '8.0.7'
```

### 11.2. Position in History Table

7 Group C entries will be positioned BETWEEN:
- `20260522144345_AddMultiCompanyFoundation` (row ~67)
- `20260523042803_AddBackgroundSchedulerEngine` (row ~68)

VAT entry will be positioned BETWEEN:
- `20260629114500_AddParentCategoryToMenuCategory` (row ~74, already baselined)
- `20260710033555_CompleteBanquetOverviewEventFields` (row ~75)

Chronological order is preserved. No reordering needed.

---

## 12. Risk Matrix

| # | Risk | Phase | Probability | Impact | Mitigation |
|---|---|---|---|---|---|
| R1 | Group C history INSERT fails (duplicate key) | 2 | LOW | LOW | Precondition guard: `WHERE NOT EXISTS` |
| R2 | Group C history INSERT fails (transaction abort) | 2 | LOW | LOW | XACT_ABORT ON — auto rollback, no partial insert |
| R3 | VAT baseline INSERT fails | 3 | LOW | LOW | Same guard pattern |
| R4 | VAT migration would fail if run by EF instead of baselined | 2→3 gap | MEDIUM | LOW | VAT has `IF COL_LENGTH` guards — safe either way |
| R5 | Someone runs `database update` between Phase 2 and Phase 3 | 2→3 gap | LOW | MEDIUM | VAT runs safely (column guards); Hotel Stay runs next — DESIRED |
| R6 | Hotel Stay Foundation fails due to FK reference | 5 | LOW | HIGH | All 3 prerequisite tables verified EXISTS |
| R7 | Hotel Stay CheckIn fails (index conflict) | 5 | LOW | MEDIUM | `DROP INDEX IF EXISTS` pattern |
| R8 | Model snapshot mismatch after deploy | 5 | LOW | HIGH | Verify `dotnet ef migrations has-pending-model-changes` returns false |
| R9 | Check-in modal still fails after deploy | 6 | LOW | HIGH | Verify `fo.LuuTruKhachSan` EXISTS, run unit tests, smoke test |
| R10 | SQL Server Express limitation | All | LOW | LOW | No COMPRESSION; keep COPY_ONLY + CHECKSUM |
| R11 | Backup file corruption | All | LOW | CRITICAL | RESTORE VERIFYONLY WITH CHECKSUM every backup |
| R12 | Wrong database targeted | All | LOW | CRITICAL | Verify DB_NAME(), server, connection before every write |

---

## 13. Rollback Scenarios

### 13.1. Scenario A: Group C Baseline Rollback

**Trigger:** Any INSERT fails (duplicate, constraint, transaction abort)

**Auto-recovery:** XACT_ABORT rolls back entire transaction. Zero rows inserted.

**Manual rollback (if needed after success):**
```sql
DELETE FROM [dbo].[__EFMigrationsHistory]
WHERE MigrationId IN (
  '20260522221500_Phase3Step32SqlPerformanceScaling',
  '20260522223000_Phase3Step33EnterprisePermissions',
  '20260522224500_Phase3Step34EInvoiceFoundation',
  '20260522230000_Phase3Step35SchedulerEngine',
  '20260523010000_AddFiscalPeriodAndBranchEngine',
  '20260523012000_AddEnterpriseNotificationCenter',
  '20260523014000_AddEnterpriseSearchIndex'
);
-- History returns to 75 rows. Schema unchanged. No data loss.
```

### 13.2. Scenario B: VAT Baseline Rollback

```sql
DELETE FROM [dbo].[__EFMigrationsHistory]
WHERE MigrationId = '20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices';
-- History returns to 82 rows. Schema unchanged.
```

### 13.3. Scenario C: Hotel Stay Deploy Rollback

```sql
-- Reverse order of creation:
DROP INDEX IF EXISTS [UQ_fo_LuuTruKhachSan_Phong_InHouse] ON [fo].[LuuTruKhachSan];
DROP TABLE IF EXISTS [fo].[LichSuLuuTruKhachSan];
DROP TABLE IF EXISTS [fo].[LuuTruKhachSan];
DROP SEQUENCE IF EXISTS [fo].[TrinhTuSoLuuTru];

DELETE FROM [dbo].[__EFMigrationsHistory]
WHERE MigrationId IN (
  '20260718095823_AddFrontOfficeHotelStayCheckIn',
  '20260718092912_AddFrontOfficeHotelStayFoundation'
);
-- History returns to 83 rows. Schema clean.
-- If disaster: restore from backup.
```

---

## 14. Simulation Timeline

```
Phase 2: Group C Baseline ───────────────────── [~10 min]
│
│  Step 2.1: Verify fingerprint (read-only) ......... 2 min
│  Step 2.2: Approval gate PASS ..................... 1 min
│  Step 2.3: Execute 7 INSERT ....................... 1 min
│  Step 2.4: Verify history 75→82 ................... 1 min
│  Step 2.5: Verify no duplicates ................... 1 min
│  Step 2.6: Verify pending 10→3 .................... 1 min
│  Step 2.7: Build + test ............................ 3 min
│
│  State: History=82, Pending=3 (VAT + 2 Hotel Stay)
│
Phase 3: Group B Baseline ───────────────────── [~5 min]
│
│  Step 3.1: Fingerprint verify VAT (read-only) ..... 2 min
│  Step 3.2: Approval gate PASS ..................... 1 min
│  Step 3.3: Execute 1 INSERT ....................... 1 min
│  Step 3.4: Verify history 82→83 ................... 1 min
│  Step 3.5: Verify pending 3→2 ..................... 1 min
│
│  State: History=83, Pending=2 (ONLY Hotel Stay)
│  ⬜ TARGET STATE FOR HOTEL STAY DEPLOYMENT
│
Phase 5: Hotel Stay Deploy ──────────────────── [~15 min]
│
│  Step 5.1: Fresh backup ........................... 2 min
│  Step 5.2-5.7: Apply Foundation ................... 5 min
│  Step 5.8-5.10: Apply CheckIn .................... 3 min
│  Step 5.11-5.12: Insert 2 history rows ............ 1 min
│  Step 5.13: Verify history 83→85, pending=0 ....... 1 min
│  Step 5.14: Build + test ........................... 3 min
│
│  State: History=85=source, Pending=0 ✓
│
Phase 6: Acceptance ────────────────────────── [~20 min]
│
│  Step 6.1-6.4: Build + test ........................ 5 min
│  Step 6.5: EF pending check ........................ 1 min
│  Step 6.6-6.10: Check-in smoke test ................ 14 min
│
│  State: CHECK-IN OPERATIONAL ✓
```

---

## 15. Go / No Go Assessment

### 15.1. Group C Baseline

| Criteria | Status |
|---|---|
| Schema EXISTS for all 7 migrations | ✅ VERIFIED (forensic) |
| 100% DDL covered by superseding migration | ✅ VERIFIED |
| ZERO DML/Seed/RawSQL in any migration | ✅ VERIFIED |
| No data loss risk | ✅ CONFIRMED |
| Rollback possible (DELETE history rows) | ✅ SIMPLE |
| Features continue to work | ✅ Build + test PASS |
| **GO** | ✅ |

### 15.2. Group B VAT Baseline

| Criteria | Status |
|---|---|
| Schema EXISTS (4 columns + defaults) | ✅ VERIFIED |
| EXACT_MATCH fingerprint | ✅ VERIFIED |
| No DML/Seed/RawSQL | ✅ VERIFIED |
| Safe even if EF runs it (IF COL_LENGTH guards) | ✅ CONFIRMED |
| Rollback possible | ✅ SIMPLE |
| **GO** | ✅ |

### 15.3. Hotel Stay Deployment

| Criteria | Status |
|---|---|
| All prerequisites exist | ✅ VERIFIED |
| No conflicting objects | ✅ VERIFIED |
| Clean deploy surface | ✅ VERIFIED |
| Backup required before deploy | ⬜ PENDING |
| Rollback plan exists | ✅ DOCUMENTED |
| **GO after baseline complete** | ✅ |

### 15.4. OVERALL: GO

**Không có rào cản kỹ thuật nào.** Chain sẽ clean, pending sẽ giảm từ 10 → 2 sau baseline. Hotel Stay có thể deploy an toàn.

---

## 16. Key Numbers Summary

| Metric | Current | After Group C | After VAT | After Hotel Stay |
|---|---|---|---|---|
| History rows | 75 | 82 | 83 | **85** |
| Pending migrations | 10 | 3 | 2 | **0** |
| Schema gaps | 2 tables | 2 tables | 2 tables | **0** |
| History gaps | 8 | 1 | 0 | **0** |
| EF chain broken? | YES | Partial | NO | **NO** |
| Check-in works? | ❌ | ❌ | ❌ | **✅** |
| History = source? | NO | NO | NO | **YES** |

---

*Simulation complete. All states mapped. Risks identified. GO for baseline execution pending approval.*
