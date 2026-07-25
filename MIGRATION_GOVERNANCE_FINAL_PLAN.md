# MIGRATION GOVERNANCE FINAL PLAN — ASMILE-BO

**Ngày lập:** 2026-07-19
**Scope:** Migration Governance Completion — baseline history, verify chain, deploy Hotel Stay
**Status:** PLAN — chưa thực thi

---

## 1. Current State

### 1.1. Database Identity

| Field | Value |
|---|---|
| Server | `HOANGVUONGPC\SQLEXPRESS` (SQL Server Express 64-bit) |
| Database | `ASMILE_BO` |
| Login | `sa` (SQL authentication) |
| State | `ONLINE` |
| Recovery | `SIMPLE` |
| Compatibility | `160` (SQL Server 2022) |
| MigrationMode | `Disabled` |
| AllowApplyPendingMigrations | `False` |
| FailOnPendingMigrations | `True` |

### 1.2. Current History vs Source

| Metric | Count |
|---|---|
| Migration files in source | **85** |
| Applied in `__EFMigrationsHistory` | **75** |
| Pending (source - applied) | **10** |
| Latest applied MigrationId | `20260718014449_AddFrontOfficeHotelRoomMasterData` |

### 1.3. Recent Changes

| Date | Action | Result |
|---|---|---|
| 2026-07-18 18:37 | ParentCategory schema repair (index + FK) | COMMITTED |
| 2026-07-18 18:37 | ParentCategory history baseline (1 row) | INSERTED |
| History before repair | 74 rows | — |
| History after repair | **75 rows** | +1 (`20260629114500`) |

---

## 2. Migration Timeline

```
2026-04-21 ──────────────────────────────────────────────────────────────── 2026-07-19
    │                                                                           │
    │  74 migrations applied (InitialCreate → HotelRoomMasterData)              │
    │  ┌──────────────────────────────────────────────────────────────────┐    │
    │  │ Accounting → Inventory → AR/AP → Cash/Bank → Assets → Tax       │    │
    │  │ Enterprise Foundation → Training → BreakfastCafe → MenuRecipe    │    │
    │  │ Restaurant → Alacarte → Banquet → FO Foundation → Reservation   │    │
    │  └──────────────────────────────────────────────────────────────────┘    │
    │                                                                           │
    ├── 7 Group C: Enterprise (obsolete — schema superseded)                    │
    ├── 1 Group B: VAT flags (EXACT_MATCH — history gap only)                   │
    ├── 1 Group B: ParentCategory (NOW EXACT_MATCH — repaired + baselined)      │
    ├── 2 Group A: Hotel Stay Foundation + CheckIn (SCHEMA MISSING)             │
    │                                                                           │
    ▼ Current: 75/85 applied, 10 pending                                       │
```

---

## 3. Applied Migrations (75 rows)

74 original + 1 ParentCategory baseline. Full list in `artifacts/migration-governance/20260718-181134/migration-history-before.csv`.

Latest 10 applied:

| # | MigrationId | Module |
|---|---|---|
| 66 | `20260710033555_CompleteBanquetOverviewEventFields` | Banquet |
| 67 | `20260710040633_AddBanquetEventMenuCustomColumns` | Banquet |
| 68 | `20260711043948_AddBanquetMenuGroups` | Banquet |
| 69 | `20260713032500_AddBanquetRoomLayout` | Banquet |
| 70 | `20260715083035_AddBanquetServiceUnits` | Banquet |
| 71 | `20260715083738_AddBanquetQuantificationAndSettings` | Banquet |
| 72 | `20260715085020_AddBanquetFloorPlanAndDepositReceipt` | Banquet |
| 73 | `20260715100639_AddBanquetCommercialControls` | Banquet |
| 74 | `20260715101214_AddBanquetPurchaseRequests` | Banquet |
| 75 | `20260716085141_AddBanquetServiceCosting` | Banquet |
| 76 | `20260629114500_AddParentCategoryToMenuCategory` | Menu (BASELINED 18/07) |
| 77 | `20260717102111_CreateFrontOfficeFoundationTechnicalStores` | FO Foundation |
| 78 | `20260717105902_CreateFrontOfficeProcessManagerFoundation` | FO Process |
| 79 | `20260717124748_HardenFrontOfficeProcessManagerRuntime` | FO Process |
| 80 | `20260717143000_AddFrontOfficeProcessWorkerDeadLetters` | FO Process |
| 81 | `20260718011133_AddFrontOfficeReservation` | FO Reservation |
| 82 | `20260718014449_AddFrontOfficeHotelRoomMasterData` | FO Hotel Room |

---

## 4. Pending Migrations (10 remaining)

### 4.1. Complete Pending List

| # | MigrationId | Group | Schema Status | History Status | Action |
|---|---|---|---|---|---|
| 1 | `20260522221500_Phase3Step32SqlPerformanceScaling` | C | EXISTS | MISSING | Baseline |
| 2 | `20260522223000_Phase3Step33EnterprisePermissions` | C | EXISTS | MISSING | Baseline |
| 3 | `20260522224500_Phase3Step34EInvoiceFoundation` | C | EXISTS | MISSING | Baseline |
| 4 | `20260522230000_Phase3Step35SchedulerEngine` | C | EXISTS | MISSING | Baseline |
| 5 | `20260523010000_AddFiscalPeriodAndBranchEngine` | C | EXISTS | MISSING | Baseline |
| 6 | `20260523012000_AddEnterpriseNotificationCenter` | C | EXISTS | MISSING | Baseline |
| 7 | `20260523014000_AddEnterpriseSearchIndex` | C | EXISTS | MISSING | Baseline |
| 8 | `20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices` | B | EXISTS | MISSING | Baseline |
| 9 | `20260718092912_AddFrontOfficeHotelStayFoundation` | A | **MISSING** | MISSING | **Apply** |
| 10 | `20260718095823_AddFrontOfficeHotelStayCheckIn` | A | **MISSING** | MISSING | **Apply** |

---

## 5. Group A — Hotel Stay (CRITICAL — Schema Missing)

### 5.1. `20260718092912_AddFrontOfficeHotelStayFoundation`

**Creates:**
- Sequence: `[fo].[TrinhTuSoLuuTru]` — START 1, INCREMENT 1
- Table: `[fo].[LuuTruKhachSan]` — HotelStay aggregate (Id, StayNumber, ReservationId, GuestId, RoomId, RoomTypeId, Status, ArrivalDate, DepartureDate, ActualCheckInAtUtc, ActualCheckOutAtUtc, RowVersion, audit fields...)
- Table: `[fo].[LichSuLuuTruKhachSan]` — StayHistory (Id, StayId, FromStatus, ToStatus, Timestamp, Actor, Metadata...)
- 10 indexes including `IX_LuuTruKhachSan_RoomId`, `IX_LuuTruKhachSan_ReservationId`, `IX_LuuTruKhachSan_Status`
- 4 FKs to `[fo].[DatPhong]`, `[fo].[PhongKhachSan]`, `[fo].[LoaiPhongKhachSan]`

**Dependencies (all EXIST):**
- `[fo].[DatPhong]` — EXISTS (applied migration 81)
- `[fo].[PhongKhachSan]` — EXISTS (applied migration 82)
- `[fo].[LoaiPhongKhachSan]` — EXISTS (applied migration 82)

**Risk if NOT applied:** Check-in runtime crash (`Invalid object name 'fo.LuuTruKhachSan'`)

### 5.2. `20260718095823_AddFrontOfficeHotelStayCheckIn`

**Creates:**
- Drops `IX_LuuTruKhachSan_RoomId`
- Creates filtered unique index `UQ_fo_LuuTruKhachSan_Phong_InHouse`:
  ```sql
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_fo_LuuTruKhachSan_Phong_InHouse]
  ON [fo].[LuuTruKhachSan] ([RoomId])
  WHERE [Status] = N'InHouse';
  ```

**Dependencies:** Directly depends on Foundation (migration 9 above). Cannot apply before Foundation.

**Risk if NOT applied:** Check-in can execute but lacks database-level invariant against double room occupancy. Not acceptable for release.

---

## 6. Group B — Menu/VAT (History Gap Only)

### 6.1. `20260629114500_AddParentCategoryToMenuCategory` — RESOLVED

**Status:** ✅ COMPLETE

| Aspect | Before Repair (18/07) | After Repair (18/07) |
|---|---|---|
| Column `ParentCategoryId` | EXISTS | EXISTS |
| Index `IX_MenuCategories_ParentCategoryId` | MISSING | EXISTS |
| FK `FK_MenuCategories_MenuCategories_ParentCategoryId` | MISSING | EXISTS (trusted, enabled) |
| Orphan rows | 0 | 0 |
| Self-parent rows | 0 | 0 |
| Cycle (recursive CTE) | 0 | 0 |
| Fingerprint | PARTIAL_MATCH | **EXACT_MATCH** |
| History entry | MISSING | **INSERTED** (ProductVersion 8.0.7) |

**Why was it PARTIAL_MATCH:**
Column `ParentCategoryId` đã tồn tại trong DB (có thể được tạo bởi migration hợp nhất `AddBackgroundSchedulerEngine` hoặc repair thủ công trước đó), nhưng index `IX_MenuCategories_ParentCategoryId` và self-FK `FK_MenuCategories_MenuCategories_ParentCategoryId` bị thiếu. Migration `Up()` tạo cả 3 object này — nếu chạy lại, column sẽ fail (đã tồn tại), nhưng index và FK sẽ không được tạo vì EF migration dùng `migrationBuilder.AddColumn` đã có guard "nếu column tồn tại thì bỏ qua". Kết quả: column có, index/FK không có.

**How was it fixed:**
Tạo thủ công index và FK qua SQL script (có transaction, precondition check, post-condition verify). Sau đó baseline 1 history row.

**Impact if NOT fixed:**
- MenuRecipe UI: ComboBox chọn nhóm cha vẫn hoạt động (column có)
- Hiệu năng: thiếu index → slow khi query child categories
- Data integrity: thiếu self-FK → có thể tạo orphan `ParentCategoryId` trỏ đến `Id` không tồn tại
- Banquet category tree: vẫn hoạt động nhưng không có DB-level protection

**Modules affected by repair:** MenuRecipe, Banquet menu selection. Build PASS, tests PASS (MenuCatalogCategoryTests 2/2, Integration 23/23).

### 6.2. `20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices` — PENDING BASELINE

**Status:** Schema EXISTS, history MISSING

| Object | Database | Source Contract | Match |
|---|---|---|---|
| `MenuItems.IsVatIncluded` | `bit NOT NULL`, default `((1))` | `bit NOT NULL`, default `((1))` | EXACT |
| `RestaurantExtraServices.IsVatIncluded` | `bit NOT NULL`, default `((1))` | `bit NOT NULL`, default `((1))` | EXACT |
| `AlacarteOrderLines.IsVatIncluded` | `bit NOT NULL`, default `((1))` | `bit NOT NULL`, default `((1))` | EXACT |
| `RestaurantOrderExtraServices.IsVatIncluded` | `bit NOT NULL`, default `((1))` | `bit NOT NULL`, default `((1))` | EXACT |

**Why EXACT_MATCH:**
Cả 4 column + default constraints đều tồn tại với type, nullability, và default definition giống hệt source contract. Migration `Up()` dùng `IF COL_LENGTH` guard nên schema đã được materialize đầy đủ. Đây là history gap thuần túy — không thiếu schema.

**Action:** Baseline 1 history row (fingerprint verify → INSERT). Không cần DDL.

**Why migration file must NOT be deleted:**
`IsVatIncluded` đang được dùng bởi Restaurant, Menu, BreakfastCafe. Nếu reconstruct database từ đầu, migration này là nguồn duy nhất tạo 4 column này. File phải giữ lại.

---

## 7. Group C — Enterprise (Obsolete/Superseded)

### 7.1. Evidence of Supersession

Migration `20260523042803_AddBackgroundSchedulerEngine` (**đã applied**) chứa toàn bộ schema của 7 migration Enterprise:

| Enterprise Migration | Tables Created | Superseded By |
|---|---|---|
| `Phase3Step32SqlPerformanceScaling` | 42 performance indexes | `AddBackgroundSchedulerEngine` |
| `Phase3Step33EnterprisePermissions` | `AppDataPermissions`, `AppRoleInheritances` | `AddBackgroundSchedulerEngine` |
| `Phase3Step34EInvoiceFoundation` | 5 e-invoice tables | `AddBackgroundSchedulerEngine` |
| `Phase3Step35SchedulerEngine` | `JobDefinitions`, `JobExecutions`, `JobLogs`, `JobRetries` | `AddBackgroundSchedulerEngine` |
| `AddFiscalPeriodAndBranchEngine` | `FiscalYears`, `PeriodLocks`, `CompanyBranchPermissions` | `AddBackgroundSchedulerEngine` |
| `AddEnterpriseNotificationCenter` | `UserNotifications`, `TaskInboxItems`, `TaskComments`, `TaskAttachmentMetadata` | `AddBackgroundSchedulerEngine` |
| `AddEnterpriseSearchIndex` | `SearchIndexEntries`, `RecentSearches` | `AddBackgroundSchedulerEngine` |

### 7.2. Verification

Đã query read-only `ASMILE_BO`:
- **Toàn bộ 20+ table** của 7 migration Enterprise C tồn tại với đúng schema
- **Các index** hiệu năng của Step32 tồn tại
- Các feature sử dụng các bảng này vẫn hoạt động bình thường

### 7.3. Risk of Direct Execution

Nếu chạy `database update` qua 7 migration này:
- `Up()` sẽ cố gắng `CreateTable` / `CreateIndex` cho object đã tồn tại
- SQL Server sẽ throw error "There is already an object named '...' in the database"
- Migration chain sẽ fail, rollback partial

**KHÔNG ĐƯỢC chạy 7 migration này.**

### 7.4. Action Required

Baseline 7 history rows. Schema đã tồn tại, features đang hoạt động. Chỉ cần đưa vào `__EFMigrationsHistory` để EF Core biết các migration này "đã được áp dụng".

### 7.5. Why migration files must NOT be deleted

Mặc dù obsolete cho execution trên `ASMILE_BO`, các file này vẫn cần cho:
- Clean/reconstructed database từ đầu
- EF Core model snapshot integrity
- Audit trail của lịch sử phát triển
- Migration assembly resolution

---

## 8. Schema Fingerprint Summary

### 8.1. Current Database Schema State

| Schema | Object Count | Status |
|---|---|---|
| `dbo` | 150+ tables, 400+ indexes, 200+ FKs | Healthy |
| `fo` | 6 tables (DatPhong, LichSuDatPhong, PhongKhachSan, LoaiPhongKhachSan, 3 technical) + 3 technical stores | Healthy |
| `fo_process` | 8 process manager tables + 1 dead letter table | Healthy |

### 8.2. Fingerprint Matrix (10 pending)

| # | Migration | Column | Index | FK | Constraint | Match |
|---|---|---|---|---|---|---|
| 1 | Phase3Step32 | N/A | 42 ∃ | N/A | N/A | EXACT |
| 2 | Phase3Step33 | 2 tables ∃ | 6 ∃ | ∃ | N/A | EXACT |
| 3 | Phase3Step34 | 5 tables ∃ | 16 ∃ | ∃ | N/A | EXACT |
| 4 | Phase3Step35 | 4 tables ∃ | 11 ∃ | ∃ | N/A | EXACT |
| 5 | FiscalPeriod | 3 tables ∃ | 7 ∃ | N/A | N/A | EXACT |
| 6 | Notification | 4 tables ∃ | 8 ∃ | ∃ | N/A | EXACT |
| 7 | SearchIndex | 2 tables ∃ | 5 ∃ | N/A | N/A | EXACT |
| 8 | VAT flags | 4 columns ∃ | N/A | N/A | 4 defaults ∃ | **EXACT** |
| 9 | StayFoundation | **MISSING** | **MISSING** | **MISSING** | **MISSING** | **GAP** |
| 10 | StayCheckIn | **MISSING** | **MISSING** | N/A | N/A | **GAP** |

### 8.3. Verification Method

Fingerprint đọc từ `sys.tables`, `sys.columns`, `sys.indexes`, `sys.index_columns`, `sys.foreign_keys`, `sys.default_constraints`, `sys.sequences` — đối chiếu với `Up()` source của từng migration. Raw metadata lưu trong `artifacts/migration-governance/`.

---

## 9. History Summary

### 9.1. Current `__EFMigrationsHistory` State

```
74 original entries (InitialCreate → HotelRoomMasterData)
+ 1 ParentCategory baseline (20260629114500)
─────────────────────────────────────────────────────────
= 75 total
```

### 9.2. Gap Analysis

```
Source timeline:
  ... → 20260522221500 (Step32) ─┐
  ... → 20260522223000 (Step33)  │
  ... → 20260522224500 (Step34)  │
  ... → 20260522230000 (Step35)  │─ 7 Group C: schema ∃, history ✗
  ... → 20260523010000 (Fiscal)  │
  ... → 20260523012000 (Notify)  │
  ... → 20260523014000 (Search) ─┘
  ... → 20260523042803 (Scheduler Engine) ← APPLIED (supersedes above 7)
  ... → ... (30+ applied migrations) ...
  ... → 20260629114500 (ParentCategory) ← BASELINED ✓
  ... → 20260630113000 (VAT flags)       ← schema ∃, history ✗
  ... → ... (Banquet, FO Foundation, Reservation, HotelRoom) ← APPLIED
  ... → 20260718092912 (StayFoundation)  ← schema ✗, history ✗
  ... → 20260718095823 (StayCheckIn)     ← schema ✗, history ✗
```

### 9.3. Target State After Governance

```
75 current
+ 7 Group C baseline
+ 1 Group B VAT baseline
+ 2 Group A apply (schema + history)
─────────────────────
= 85 total = source count
Pending = 0
```

---

## 10. Risk Matrix

| # | Risk | Probability | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Group C baseline sai → EF Core không nhận diện | Low | Medium | Fingerprint EXACT_MATCH từng migration trước khi INSERT |
| R2 | VAT baseline thiếu precondition check | Low | Low | Verify 4 columns + defaults trước INSERT |
| R3 | Hotel Stay migration fail do FK reference | Low | High | Verify `DatPhong`, `PhongKhachSan`, `LoaiPhongKhachSan` tồn tại |
| R4 | Hotel Stay Foundation fail do object conflict | Low | High | Dry-run script, verify không có object `LuuTruKhachSan` |
| R5 | Check-in runtime crash sau deploy do mapping mismatch | Medium | High | Run full unit + integration test sau deploy |
| R6 | Rollback cần thiết sau Hotel Stay deploy | Low | High | Backup COPY_ONLY + CHECKSUM trước mọi write |
| R7 | SQL Server Express limitation (no compression) | Low | Low | Backup bỏ COMPRESSION, giữ COPY_ONLY + CHECKSUM |
| R8 | App fail sau migration do migration mode Disabled | Low | Low | App không tự apply migration; đây là safety feature |

---

## 11. Execution Order

### Phase 1: Pre-flight (READ-ONLY)

```
Step 1.1  Verify database identity (server, database name, state)
Step 1.2  Verify connection string unchanged
Step 1.3  Export current __EFMigrationsHistory to CSV
Step 1.4  Create fresh COPY_ONLY + CHECKSUM backup
Step 1.5  RESTORE VERIFYONLY backup
Step 1.6  Compute SHA-256 of backup file
```

### Phase 2: Group C Baseline (7 history rows)

```
Step 2.1  Fingerprint verify all 7 Group C migrations (read-only)
           - Verify target tables exist
           - Verify target indexes exist
           - Verify target FKs exist
Step 2.2  IF all 7 EXACT_MATCH → approval gate PASS
Step 2.3  Generate idempotent baseline SQL for 7 rows
           - Each INSERT precondition: MigrationId NOT IN history
           - Wrapped in XACT_ABORT transaction
Step 2.4  Execute baseline SQL
Step 2.5  Verify history count: 75 → 82
Step 2.6  Verify no duplicate MigrationId
```

### Phase 3: Group B Baseline (1 history row — VAT)

```
Step 3.1  Fingerprint verify VAT migration (read-only)
           - Verify 4 IsVatIncluded columns exist
           - Verify 4 default constraints exist with ((1))
Step 3.2  IF EXACT_MATCH → approval gate PASS
Step 3.3  Generate idempotent baseline SQL for 1 row
Step 3.4  Execute baseline SQL
Step 3.5  Verify history count: 82 → 83
```

### Phase 4: Chain Verification

```
Step 4.1  Verify EF pending list: only 2 Hotel Stay remain
Step 4.2  Verify no "pending model changes" (EF snapshot check)
Step 4.3  Build Debug: PASS (0 warn, 0 err)
Step 4.4  Build Release: PASS (0 warn, 0 err)
Step 4.5  Full unit test: PASS
Step 4.6  Full integration test: PASS
```

### Phase 5: Hotel Stay Deployment

```
Step 5.1  Create fresh backup before deployment
Step 5.2  Generate SQL from 20260718092912_AddFrontOfficeHotelStayFoundation
Step 5.3  Review SQL: sequence, 2 tables, 10 indexes, 4 FKs
Step 5.4  Execute Foundation SQL in transaction
Step 5.5  Verify: fo.LuuTruKhachSan EXISTS
Step 5.6  Verify: fo.LichSuLuuTruKhachSan EXISTS
Step 5.7  Verify: fo.TrinhTuSoLuuTru EXISTS
Step 5.8  Generate SQL from 20260718095823_AddFrontOfficeHotelStayCheckIn
Step 5.9  Execute CheckIn SQL in transaction
Step 5.10 Verify: UQ_fo_LuuTruKhachSan_Phong_InHouse EXISTS (filtered unique)
Step 5.11 Insert 2 history rows
Step 5.12 Verify history count: 83 → 85
Step 5.13 Verify pending = 0
```

### Phase 6: Acceptance

```
Step 6.1  Build Debug: PASS
Step 6.2  Build Release: PASS
Step 6.3  Full unit test: PASS
Step 6.4  Full integration test: PASS
Step 6.5  EF pending model changes: PASS (none)
Step 6.6  Launch Debug app
Step 6.7  Login → Front Office → Reception → Check-in modal
Step 6.8  Verify: PendingArrival list loads (no SqlException)
Step 6.9  Verify: Check-in flow completes end-to-end
Step 6.10 Launch Release app → repeat smoke test
```

---

## 12. Rollback Strategy

### 12.1. Before Any Write

Backup `ASMILE_BO` với `COPY_ONLY + CHECKSUM`. RESTORE VERIFYONLY. SHA-256 documented.

### 12.2. Group C/B Baseline Rollback

Nếu baseline INSERT fail:
- Transaction `XACT_ABORT` tự rollback
- Không partial insert
- History table unchanged

Nếu cần rollback sau baseline thành công:
- DELETE FROM `__EFMigrationsHistory` WHERE MigrationId IN (...)
- Chỉ xóa các row vừa insert
- Schema không bị ảnh hưởng (không có DDL trong baseline)

### 12.3. Hotel Stay Deployment Rollback

Nếu Foundation deploy fail:
- Transaction rollback → không partial schema
- Không cần cleanup

Nếu cần rollback sau deploy thành công:
```sql
-- Drop in reverse dependency order
DROP INDEX IF EXISTS [UQ_fo_LuuTruKhachSan_Phong_InHouse] ON [fo].[LuuTruKhachSan];
DROP TABLE IF EXISTS [fo].[LichSuLuuTruKhachSan];
DROP TABLE IF EXISTS [fo].[LuuTruKhachSan];
DROP SEQUENCE IF EXISTS [fo].[TrinhTuSoLuuTru];
DELETE FROM [dbo].[__EFMigrationsHistory] WHERE MigrationId IN (
  '20260718095823_AddFrontOfficeHotelStayCheckIn',
  '20260718092912_AddFrontOfficeHotelStayFoundation'
);
```
Sau đó restore từ backup nếu cần.

### 12.4. Disaster Recovery

Nếu mọi thứ fail → Restore từ backup COPY_ONLY gần nhất.

---

## 13. Approval Gates

| Gate | Prerequisite | Criteria | Dependencies |
|---|---|---|---|
| **G1 — Backup** | None | COPY_ONLY backup verified (SHA-256) | None |
| **G2 — Group C Fingerprint** | G1 | All 7 EXACT_MATCH | None |
| **G3 — Group C Baseline** | G2 | History 75→82, no duplicates | None |
| **G4 — Group B Fingerprint** | G3 | VAT EXACT_MATCH | None |
| **G5 — Group B Baseline** | G4 | History 82→83, no duplicates | None |
| **G6 — Chain Verify** | G5 | Build PASS, tests PASS, pending=2 | None |
| **G7 — Pre-Deploy Backup** | G6 | Fresh backup verified | None |
| **G8 — Foundation Deploy** | G7 | Tables + sequence exist | `DatPhong`, `PhongKhachSan`, `LoaiPhongKhachSan` |
| **G9 — CheckIn Deploy** | G8 | Filtered unique index exists | Foundation |
| **G10 — History Complete** | G9 | History 83→85, pending=0 | Foundation + CheckIn |
| **G11 — Acceptance** | G10 | Build PASS, tests PASS, Check-in modal smoke PASS | All above |

**CRITICAL RULE:** Mỗi gate phải PASS trước khi qua gate tiếp theo. Nếu gate FAIL → dừng, báo cáo, không tự ý sửa.

---

## 14. Expected Runtime

| Phase | Steps | Estimated Duration | Risk Level |
|---|---|---|---|
| Phase 1: Pre-flight | 6 | 5 min | Low |
| Phase 2: Group C Baseline | 6 | 10 min | Low |
| Phase 3: Group B Baseline | 5 | 5 min | Low |
| Phase 4: Chain Verification | 6 | 15 min | Low |
| Phase 5: Hotel Stay Deploy | 13 | 15 min | **Medium** |
| Phase 6: Acceptance | 10 | 20 min | Medium |
| **Total** | **46** | **~70 min** | — |

---

## 15. Verification Checklist

### 15.1. Database Verification

- [ ] Backup COPY_ONLY + CHECKSUM created and VERIFYONLY PASS
- [ ] SHA-256 documented
- [ ] History count = 75 (before), 85 (after)
- [ ] No duplicate MigrationId in `__EFMigrationsHistory`
- [ ] `[fo].[LuuTruKhachSan]` EXISTS (after Phase 5)
- [ ] `[fo].[LichSuLuuTruKhachSan]` EXISTS (after Phase 5)
- [ ] `[fo].[TrinhTuSoLuuTru]` EXISTS (after Phase 5)
- [ ] `UQ_fo_LuuTruKhachSan_Phong_InHouse` EXISTS (after Phase 5)
- [ ] EF pending migrations = 0
- [ ] EF pending model changes = none

### 15.2. Build & Test Verification

- [ ] Debug build: 0 warnings, 0 errors
- [ ] Release build: 0 warnings, 0 errors
- [ ] Unit tests: ALL PASS
- [ ] Integration tests: ALL PASS
- [ ] XAML encoding check: PASS

### 15.3. Runtime Verification

- [ ] Debug app launches without error
- [ ] Login successful
- [ ] Front Office Main Hub opens
- [ ] Reception Hub opens
- [ ] Check-in modal opens (no XAML error)
- [ ] PendingArrival list loads (no SQL error)
- [ ] Check-in flow: select stay → confirm → success
- [ ] Release app: same smoke test PASS

---

## 16. Recommended SQL Order

> **CẢNH BÁO:** SQL dưới đây là **RECOMMENDED ORDER** — chưa được phê duyệt để thực thi. Không chạy khi chưa qua tất cả approval gates.

### 16.1. Group C Baseline (7 rows)

```sql
-- Precondition: backup verified, fingerprint EXACT_MATCH for all 7
-- Mỗi row INSERT có guard: NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = @id)
-- Wrapped in XACT_ABORT ON transaction
-- ProductVersion = 8.0.7 (from EF Core tooling)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260522221500_Phase3Step32SqlPerformanceScaling', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260522221500_Phase3Step32SqlPerformanceScaling');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260522223000_Phase3Step33EnterprisePermissions', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260522223000_Phase3Step33EnterprisePermissions');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260522224500_Phase3Step34EInvoiceFoundation', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260522224500_Phase3Step34EInvoiceFoundation');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260522230000_Phase3Step35SchedulerEngine', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260522230000_Phase3Step35SchedulerEngine');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260523010000_AddFiscalPeriodAndBranchEngine', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260523010000_AddFiscalPeriodAndBranchEngine');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260523012000_AddEnterpriseNotificationCenter', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260523012000_AddEnterpriseNotificationCenter');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260523014000_AddEnterpriseSearchIndex', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260523014000_AddEnterpriseSearchIndex');

COMMIT TRANSACTION;

-- Verify: SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory]; -- expect 82
```

### 16.2. Group B VAT Baseline (1 row)

```sql
-- Precondition: backup verified, fingerprint EXACT_MATCH for VAT
-- Verify before executing:
--   SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
--   FROM INFORMATION_SCHEMA.COLUMNS
--   WHERE TABLE_NAME IN ('MenuItems','RestaurantExtraServices','AlacarteOrderLines','RestaurantOrderExtraServices')
--   AND COLUMN_NAME = 'IsVatIncluded';
-- Expected: 4 rows, all bit, NO, default ((1))

SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices');

COMMIT TRANSACTION;

-- Verify: SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory]; -- expect 83
```

### 16.3. Hotel Stay Foundation

> **NOTE:** SQL generated từ EF Core migration `20260718092912_AddFrontOfficeHotelStayFoundation`. Cần verify từng DDL statement trước khi chạy. Không chạy nếu chưa qua G1-G7.

Key objects created:
```sql
-- Sequence
CREATE SEQUENCE [fo].[TrinhTuSoLuuTru] START WITH 1 INCREMENT BY 1;

-- Tables
CREATE TABLE [fo].[LuuTruKhachSan] (...);
CREATE TABLE [fo].[LichSuLuuTruKhachSan] (...);

-- Indexes (10)
-- Foreign Keys (4) to [fo].[DatPhong], [fo].[PhongKhachSan], [fo].[LoaiPhongKhachSan]
```

Full SQL: `artifacts/migration-governance/20260718-181134/hotel-stay-deploy.sql`

### 16.4. Hotel Stay Check-in

```sql
-- Drop old index
DROP INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan];

-- Create filtered unique index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_fo_LuuTruKhachSan_Phong_InHouse]
ON [fo].[LuuTruKhachSan] ([RoomId])
WHERE [Status] = N'InHouse';
```

### 16.5. Hotel Stay History (2 rows)

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260718092912_AddFrontOfficeHotelStayFoundation', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260718092912_AddFrontOfficeHotelStayFoundation');

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '20260718095823_AddFrontOfficeHotelStayCheckIn', '8.0.7'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260718095823_AddFrontOfficeHotelStayCheckIn');

COMMIT TRANSACTION;

-- Verify: SELECT COUNT(*) FROM [dbo].[__EFMigrationsHistory]; -- expect 85
```

---

## 17. Things Never To Do

### Migration Governance Rules

1. **KHÔNG** chạy `dotnet ef database update` khi chưa fingerprint toàn bộ pending migration
2. **KHÔNG** baseline history khi schema còn PARTIAL_MATCH
3. **KHÔNG** insert history row mà không có precondition guard (NOT EXISTS)
4. **KHÔNG** chạy migration Group C trực tiếp — sẽ tạo duplicate object
5. **KHÔNG** apply Hotel Stay khi chưa baseline Group C và Group B
6. **KHÔNG** deploy Hotel Stay mà không có backup COPY_ONLY + CHECKSUM
7. **KHÔNG** xóa migration file — kể cả Group C đã superseded
8. **KHÔNG** sửa `__EFMigrationsHistory` bằng tay ngoài script đã review
9. **KHÔNG** đổi connection string để "né" migration pending
10. **KHÔNG** chạy SQL migration khi chưa verify database identity
11. **KHÔNG** bỏ qua approval gate — mỗi gate phải PASS
12. **KHÔNG** tạo migration mới để "sửa" history gap
13. **KHÔNG** dùng `WITH COMPRESSION` trên SQL Server Express
14. **KHÔNG** ghi password/secret trong script hoặc artifact
15. **KHÔNG** claim PASS nếu chưa verify runtime smoke

### Known Failure Patterns (từ sprint trước)

16. **KHÔNG** chạy `dotnet ef database update` không phân tích → đã bị chặn bởi governance gate
17. **KHÔNG** baseline history khi fingerprint PARTIAL_MATCH → đã bị chặn (ParentCategory)
18. **KHÔNG** insert riêng Hotel Stay SQL mà không cập nhật history → sẽ để EF chain sai lệch

---

## 18. Final Recommendation

### 18.1. Recommended Path

```
BACKUP → FINGERPRINT → BASELINE Group C (7) → BASELINE Group B (1)
→ VERIFY CHAIN → BACKUP → DEPLOY Hotel Stay (2) → VERIFY → ACCEPTANCE SMOKE
```

### 18.2. Why This Order

1. **Group C first**: 7 migration có timestamp cũ nhất. Phải baseline trước để EF chain không còn thấy chúng.
2. **Group B second**: VAT flag là migration cuối cùng trước Hotel Stay. Phải baseline để chain sạch đến Hotel Stay.
3. **Hotel Stay last**: Chỉ deploy khi chain đã sạch (pending chỉ còn 2 Hotel Stay). Dependency schema (`DatPhong`, `PhongKhachSan`, `LoaiPhongKhachSan`) đã sẵn sàng.

### 18.3. Success Criteria

| # | Criteria | Measurement |
|---|---|---|
| 1 | History = source count | 85 = 85 |
| 2 | Pending migrations = 0 | `dotnet ef migrations list` shows none |
| 3 | No pending model changes | EF snapshot check PASS |
| 4 | Build PASS | 0 warn, 0 err (Debug + Release) |
| 5 | Tests PASS | All unit + integration |
| 6 | Check-in modal loads | No XamlParseException, no SqlException |
| 7 | Check-in flow works | End-to-end: select stay → check-in → success |

### 18.4. Unknowns

| # | Unknown | Impact | Resolution |
|---|---|---|---|
| 1 | Chưa có live Menu smoke sau ParentCategory repair | Low | Read-only smoke khi app mở — xác nhận category tree hiển thị |
| 2 | Chưa có live Banquet smoke sau ParentCategory repair | Low | Read-only smoke khi app mở — xác nhận category tree hiển thị |
| 3 | Performance của Hotel Stay migration trên production data volume | Low | Hiện tại DB không có Hotel Stay data; migration chỉ tạo schema mới |
| 4 | Flaky test `Cancellation_PropagatesToRunningExecution` | Low | Đã xác định là flaky (pass khi chạy lại), không liên quan migration |

### 18.5. Overall Assessment

Migration governance đã tiến triển đáng kể:
- ✅ ParentCategory: PARTIAL_MATCH → EXACT_MATCH (index + FK đã tạo, history đã baseline)
- ⬜ 7 Group C: Schema EXISTS, cần baseline 7 history rows
- ⬜ 1 Group B VAT: Schema EXISTS, cần baseline 1 history row
- ⬜ 2 Group A Hotel Stay: Schema MISSING, cần deploy + history

**Đánh giá rủi ro: THẤP đến TRUNG BÌNH.** Các bước còn lại là baseline history (không DDL — rủi ro thấp) và deploy 2 migration mới (có DDL — cần backup). Tất cả các prerequisite schema đã tồn tại. Không có unknown về dependency.

**Khuyến nghị:** Tiến hành tuần tự theo execution order (Section 11). Mỗi phase phải PASS approval gate trước khi sang phase tiếp theo. Dừng và báo cáo nếu bất kỳ gate nào FAIL.

---

## Appendix A: Key Artifacts Reference

| Artifact | Path | Description |
|---|---|---|
| Current backup | `Backups\ASMILE_BO_before_parentcategory_schema_repair_20260718-183735.bak` | Backup trước ParentCategory repair |
| Governance backup | `Backups\ASMILE_BO_before_migration_governance_20260718-181134.bak` | Backup trước governance run |
| Fingerprint before | `artifacts/migration-governance/20260718-181134/schema-fingerprint-before.json` | Schema fingerprint trước repair |
| Fingerprint after | `artifacts/migration-governance/20260718-183735/fingerprint-after.json` | Schema fingerprint sau repair |
| History before | `artifacts/migration-governance/20260718-181134/migration-history-before.csv` | 74 rows |
| Repair SQL | `artifacts/migration-governance/20260718-183735/parentcategory-schema-repair.sql` | Index + FK creation |
| Baseline SQL | `artifacts/migration-governance/20260718-183735/parentcategory-history-baseline.sql` | 1 history row |
| Rollback plan | `artifacts/migration-governance/20260718-183735/rollback-plan.md` | ParentCategory rollback |
| Hotel Stay SQL preview | `artifacts/migration-governance/20260718-181134/generated-sql-preview.sql` | Foundation + CheckIn DDL |

## Appendix B: Migration File Inventory (85 total)

| Status | Count | Files |
|---|---|---|
| APPLIED | 75 | InitialCreate → HotelRoomMasterData + ParentCategory baseline |
| PENDING (Group C) | 7 | Enterprise migrations (obsolete) |
| PENDING (Group B) | 1 | VAT flags |
| PENDING (Group A) | 2 | Hotel Stay Foundation + CheckIn |
| **TOTAL** | **85** | — |

---

*Plan completed. Ready for approval. No code changes, no SQL execution, no database writes performed in creating this plan.*
