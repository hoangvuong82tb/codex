# HOTEL STAY DEPLOYMENT RESULT — ASMILE-BO

**Ngày:** 2026-07-19
**Loại:** CONTROLLED DEPLOYMENT — Hotel Stay Foundation + CheckIn
**Trạng thái:** HOAN THANH ✅

---

## 1. Backup

| Item | Result |
|---|---|
| Path | `D:\ASMILE-BO\Backups\ASMILE_BO_before_hotel_stay_deploy_20260719.bak` |
| Mode | COPY_ONLY + CHECKSUM |
| Database | `ASMILE_BO` on `HOANGVUONGPC\SQLEXPRESS` |
| RESTORE VERIFYONLY | **PASS** |
| Pages | 22,665 |

---

## 2. SQL Preview Summary

### Foundation (`20260718092912`)
- `CREATE SEQUENCE [fo].[TrinhTuSoLuuTru]` — START 1, INCREMENT 1, NO CYCLE
- `CREATE TABLE [fo].[LuuTruKhachSan]` — 26 columns, 3 FKs (NO ACTION)
- `CREATE TABLE [fo].[LichSuLuuTruKhachSan]` — 10 columns, 1 FK (NO ACTION)
- 10 indexes including 2 UNIQUE filtered indexes
- 1 history INSERT

### CheckIn (`20260718095823`)
- `DROP INDEX [IX_LuuTruKhachSan_RoomId]`
- `CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_Phong_InHouse]` — filtered WHERE Status = 'InHouse'
- 1 history INSERT

---

## 3. Foundation Deployment

| Step | Result |
|---|---|
| Create SEQUENCE `fo.TrinhTuSoLuuTru` | ✅ |
| Create TABLE `fo.LuuTruKhachSan` (26 cols) | ✅ |
| Create TABLE `fo.LichSuLuuTruKhachSan` (10 cols) | ✅ |
| Create PK `PK_fo_LuuTruKhachSan` | ✅ |
| Create FK → DatPhong | ✅ (NO ACTION) |
| Create FK → PhongKhachSan | ✅ (NO ACTION) |
| Create FK → LoaiPhongKhachSan | ✅ (NO ACTION) |
| Create FK → LichSuLuuTruKhachSan (History) | ✅ (NO ACTION) |
| Create 10 indexes | ✅ |
| Create UNIQUE index `UQ_fo_LuuTruKhachSan_DatPhong_HoatDong` | ✅ |
| Create UNIQUE index `UQ_fo_LuuTruKhachSan_SoLuuTru` | ✅ |
| Insert history row | ✅ |
| History count | 75→82→83→**84** |
| **ISSUE:** QUOTED_IDENTIFIER for filtered index | Retried with SET QUOTED_IDENTIFIER ON ✅ |

---

## 4. CheckIn Deployment

| Step | Result |
|---|---|
| DROP INDEX `IX_LuuTruKhachSan_RoomId` | ✅ |
| CREATE UNIQUE FILTERED INDEX `UQ_fo_LuuTruKhachSan_Phong_InHouse` | ✅ |
| Insert history row | ✅ |
| History count | 84→**85** |

---

## 5. History Before → After

| Stage | Count | Latest MigrationId |
|---|---|---|
| **Before sprint FO-004** | 75 | `20260718014449_AddFrontOfficeHotelRoomMasterData` |
| After Group C baseline | 82 | (same) |
| After VAT baseline | 83 | (same) |
| **After Hotel Stay deploy** | **85** | **`20260718095823_AddFrontOfficeHotelStayCheckIn`** |

---

## 6. Pending Before → After

| Stage | Pending | Breakdown |
|---|---|---|
| **Before sprint FO-004** | 10 | 7 Group C + 1 VAT + 2 Hotel Stay |
| After Group C baseline | 3 | 1 VAT + 2 Hotel Stay |
| After VAT baseline | 2 | 2 Hotel Stay |
| **After Hotel Stay deploy** | **0** | — |

---

## 7. DDL Verification

| Object | Expected | Actual | Status |
|---|---|---|---|
| `fo.TrinhTuSoLuuTru` (sequence) | EXISTS | EXISTS | ✅ |
| `fo.LuuTruKhachSan` (table) | EXISTS | EXISTS | ✅ |
| `fo.LichSuLuuTruKhachSan` (table) | EXISTS | EXISTS | ✅ |
| Primary Key (Stay) | 1 | 1 | ✅ |
| Primary Key (History) | 1 | 1 | ✅ |
| Foreign Keys (Stay) | 3 | 3 | ✅ |
| Foreign Keys (History) | 1 | 1 | ✅ |
| Indexes (Stay) | 10 | 10 | ✅ |
| Indexes (History) | 2 | 2 | ✅ |

---

## 8. FK Verification

| FK Name | Parent Table | Delete Behavior | Status |
|---|---|---|---|
| `FK_LuuTruKhachSan_DatPhong_ReservationId` | `fo.DatPhong` | NO ACTION | ✅ |
| `FK_LuuTruKhachSan_LoaiPhongKhachSan_RoomTypeId` | `fo.LoaiPhongKhachSan` | NO ACTION | ✅ |
| `FK_LuuTruKhachSan_PhongKhachSan_RoomId` | `fo.PhongKhachSan` | NO ACTION | ✅ |
| `FK_LichSuLuuTruKhachSan_LuuTruKhachSan_HotelStayId` | `fo.LuuTruKhachSan` | NO ACTION | ✅ |

---

## 9. Index Verification

| Index | Type | Filter | Status |
|---|---|---|---|
| `IX_fo_LuuTruKhachSan_CoSo_NgayDen` | Non-unique | None | ✅ |
| `IX_fo_LuuTruKhachSan_CoSo_NgayDi` | Non-unique | None | ✅ |
| `IX_fo_LuuTruKhachSan_CoSo_Phong_TrangThai` | Non-unique | None | ✅ |
| `IX_fo_LuuTruKhachSan_CoSo_TrangThai` | Non-unique | None | ✅ |
| `IX_fo_LuuTruKhachSan_CapNhat` | Non-unique | None | ✅ |
| `IX_LuuTruKhachSan_RoomTypeId` | Non-unique | None | ✅ |
| `UQ_fo_LuuTruKhachSan_DatPhong_HoatDong` | **UNIQUE** | `[ReservationId] IS NOT NULL AND [Status] IN ('PendingArrival','InHouse')` | ✅ |
| `UQ_fo_LuuTruKhachSan_SoLuuTru` | **UNIQUE** | None | ✅ |
| **`UQ_fo_LuuTruKhachSan_Phong_InHouse`** | **UNIQUE** | **`[Status] = 'InHouse'`** | ✅ |
| `IX_fo_LichSuLuuTruKhachSan_LuuTru_Luc` | Non-unique | None | ✅ |

---

## 10. Build Result

```
dotnet build ASMILE.BO.sln
  Build succeeded.
  0 Warning(s)
  0 Error(s)
  Encoding check: PASS
```

---

## 11. Unit Test

```
dotnet test ASMILE.BO.Tests.Unit
  Passed: 328, Failed: 0, Skipped: 0
  Duration: 27s
```

---

## 12. Integration Test

```
dotnet test ASMILE.BO.Tests.Integration
  Passed: 23, Failed: 0, Skipped: 0
  Duration: 1m 21s
```

---

## 13. EF Verification

| Check | Result |
|---|---|
| `__EFMigrationsHistory` count | **85** = source count |
| Hotel Stay entries in history | 2 ✅ |
| Pending migrations (SQL query) | 0 ✅ |
| `dotnet ef migrations list` (vs ASMILE_BO) | All show as pending* |

*EF CLI uses LocalDB by default via `AppDbContextFactory`. Runtime uses `ASMILE_BO` correctly. Database has all 85 history entries confirmed via direct SQL query. This is a design-time tooling artifact, not a runtime issue.

---

## 14. Runtime Smoke Test

| Check | Result |
|---|---|
| App launch | ✅ No crash |
| Startup log — SqlException | **NONE** (previously had `Invalid object name 'fo.LuuTruKhachSan'`) |
| Startup log — ERR | **NONE** |
| Database migration mode | Disabled (correct) |
| Database identified | `ASMILE_BO` (correct) |
| Full Check-in flow | ⬜ Requires user login (manual step) |

**Runtime log evidence:**
```
[2026-07-19 10:48:01 INF] Database migration startup policy: Mode="Disabled"; Environment=Production; Database=ASMILE_BO
[2026-07-19 10:48:01 INF] Database migration is disabled by policy.
```

No SqlException. No crash. No `Invalid object name`. App is running and ready for manual Check-in testing.

---

## 15. Database Objects Created

| Object | Type | Schema | Name |
|---|---|---|---|
| Sequence | SEQUENCE | `fo` | `TrinhTuSoLuuTru` |
| Table | TABLE | `fo` | `LuuTruKhachSan` |
| Table | TABLE | `fo` | `LichSuLuuTruKhachSan` |
| PK | PRIMARY KEY | — | `PK_fo_LuuTruKhachSan` |
| PK | PRIMARY KEY | — | `PK_fo_LichSuLuuTruKhachSan` |
| FK × 4 | FOREIGN KEY | — | (see Section 8) |
| Index × 10 | INDEX | — | (see Section 9) |
| Filtered unique index | INDEX | — | `UQ_fo_LuuTruKhachSan_Phong_InHouse` |
| History row × 2 | DATA | `dbo` | `__EFMigrationsHistory` |

---

## 16. Performance Notes

- All FKs use NO ACTION — no cascade overhead
- Filtered unique index only applies to `Status = 'InHouse'` — minimal overhead
- Sequence uses NO CYCLE — safe for long-running operations
- RowVersion column for optimistic concurrency
- Index on `(PropertyId, RoomId, Status)` for occupancy queries
- Index on `(PropertyId, ScheduledArrivalDate)` for arrivals queries

---

## 17. Risk Assessment

| # | Risk | Status |
|---|---|---|
| R1 | FK reference failure (prerequisite table missing) | ✅ AVOIDED — all 3 prerequisites existed |
| R2 | Duplicate object (table already exists) | ✅ AVOIDED — clean deploy surface confirmed |
| R3 | QUOTED_IDENTIFIER for filtered index | ✅ RESOLVED — retried with SET option |
| R4 | Transaction rollback (partial deploy) | ✅ AVOIDED — both transactions committed |
| R5 | App crash after deploy | ✅ AVOIDED — log clean, no SqlException |
| R6 | Check-in double occupancy | ✅ MITIGATED — filtered unique index enforces invariant |

---

## 18. Rollback

If rollback is needed:

```sql
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

DROP INDEX IF EXISTS [UQ_fo_LuuTruKhachSan_Phong_InHouse] ON [fo].[LuuTruKhachSan];

-- Recreate the original index that was dropped
CREATE INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan] ([RoomId]);

DROP TABLE IF EXISTS [fo].[LichSuLuuTruKhachSan];
DROP TABLE IF EXISTS [fo].[LuuTruKhachSan];
DROP SEQUENCE IF EXISTS [fo].[TrinhTuSoLuuTru];

DELETE FROM [dbo].[__EFMigrationsHistory]
WHERE MigrationId IN (
  '20260718095823_AddFrontOfficeHotelStayCheckIn',
  '20260718092912_AddFrontOfficeHotelStayFoundation'
);

COMMIT TRANSACTION;
-- History returns to 83. Schema clean.
```

Alternatively: restore from `ASMILE_BO_before_hotel_stay_deploy_20260719.bak`

---

## 19. Go / No Go

### Deployment: GO ✅

| Criteria | Status |
|---|---|
| Backup verified (RESTORE VERIFYONLY) | ✅ |
| All prerequisites exist | ✅ |
| Clean deploy surface | ✅ |
| Foundation deployed | ✅ |
| CheckIn deployed | ✅ |
| All DDL verified | ✅ |
| All FKs verified (4/4) | ✅ |
| All indexes verified (10/10) | ✅ |
| Filtered unique index exists | ✅ |
| History = 85 | ✅ |
| Pending = 0 | ✅ |
| Build PASS (0 warn, 0 err) | ✅ |
| Unit tests PASS (328/328) | ✅ |
| Integration tests PASS (23/23) | ✅ |
| App launches without SqlException | ✅ |
| No ERR in runtime log | ✅ |

### Check-in Runtime: READY FOR MANUAL SMOKE ✅

- Tables exist in database
- App starts without crash
- Log is clean
- User needs to login → Front Office → Reception → Check-in to complete end-to-end verification

---

## 20. Migration Governance — Final State

```
85/85 migrations applied
0 pending
0 history gaps
0 schema gaps
History = Source = 85 ✅
EF chain continuous ✅
Check-in unblocked ✅
```

---

*Hotel Stay deployment complete. History = 85, Pending = 0. San sang cho Check-in acceptance testing.*
