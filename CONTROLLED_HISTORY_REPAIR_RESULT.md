# CONTROLLED HISTORY REPAIR RESULT — ASMILE-BO

**Ngày thực hiện:** 2026-07-19
**Loại:** CONTROLLED — history baseline only, no schema DDL
**Trạng thái:** HOAN THANH

---

## 1. Backup

| Item | Result |
|---|---|
| Path | `D:\ASMILE-BO\Backups\ASMILE_BO_before_group_c_baseline_20260719.bak` |
| Mode | COPY_ONLY + CHECKSUM |
| Database | `ASMILE_BO` on `HOANGVUONGPC\SQLEXPRESS` |
| RESTORE VERIFYONLY | **PASS** |
| Pages | 22,665 |

---

## 2. History Before

| Metric | Value |
|---|---|
| Row count | **75** |
| Latest MigrationId | `20260718014449_AddFrontOfficeHotelRoomMasterData` |
| Pending | **10** (7 Group C + 1 VAT + 2 Hotel Stay) |
| History gaps | 8 |
| EF chain | BROKEN (Group C blocks path to Hotel Stay) |

---

## 3. History After Group C Baseline

| Metric | Value |
|---|---|
| Action | INSERT 7 history rows |
| Row count | **82** |
| Duplicates | 0 |
| Chain between MultiCompany and BackgroundSchedulerEngine | CONTINUOUS |
| Pending after | **3** (1 VAT + 2 Hotel Stay) |

**7 MigrationId inserted:**
```
20260522221500_Phase3Step32SqlPerformanceScaling
20260522223000_Phase3Step33EnterprisePermissions
20260522224500_Phase3Step34EInvoiceFoundation
20260522230000_Phase3Step35SchedulerEngine
20260523010000_AddFiscalPeriodAndBranchEngine
20260523012000_AddEnterpriseNotificationCenter
20260523014000_AddEnterpriseSearchIndex
```

---

## 4. History After VAT Baseline

| Metric | Value |
|---|---|
| Action | INSERT 1 history row |
| Row count | **83** |
| VAT schema fingerprint | EXACT_MATCH (4 columns, 4 defaults) |
| Pending after | **2** (ONLY Hotel Stay) |

**1 MigrationId inserted:**
```
20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices
```

---

## 5. Pending Before → After

| Stage | Group C | VAT | Hotel Stay | Total |
|---|---|---|---|---|
| **Before** | 7 PENDING | 1 PENDING | 2 PENDING | **10** |
| After Group C baseline | 0 | 1 PENDING | 2 PENDING | **3** |
| After VAT baseline | 0 | 0 | 2 PENDING | **2** |

---

## 6. Remaining Pending

```
20260718092912_AddFrontOfficeHotelStayFoundation       ← Schema MISSING
20260718095823_AddFrontOfficeHotelStayCheckIn          ← Schema MISSING
```

Đây là 2 migration Hotel Stay — **chưa được deploy trong sprint này.** Sẽ deploy ở sprint tiếp theo.

---

## 7. Migration List (EF View)

```
Applied:
  20260421153713_InitialCreate
  → ... 81 migrations ...
  20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices (BASELINED)
  20260710033555_CompleteBanquetOverviewEventFields
  → ... 14 Banquet/FO/Reservation/Room migrations ...
  20260718014449_AddFrontOfficeHotelRoomMasterData

Pending:
  20260718092912_AddFrontOfficeHotelStayFoundation
  20260718095823_AddFrontOfficeHotelStayCheckIn
```

---

## 8. EF Chain

**Status:** CONTINUOUS ✅

Chain is fully linked from `InitialCreate` to `HotelRoomMasterData`. The only 2 pending migrations are the Hotel Stay pair, which naturally extend the chain forward.

No gaps, no breaks, no missing prerequisites.

---

## 9. Schema Verification

| Object Group | Expected | Actual | Match |
|---|---|---|---|
| Group C tables (20+) | EXISTS (via superseding) | EXISTS | ✅ |
| Group C indexes (95+) | EXISTS (via superseding) | EXISTS | ✅ |
| VAT columns (4) | EXISTS | EXISTS | ✅ |
| Hotel Stay tables | STILL MISSING | MISSING | ✅ (expected — not deployed) |

---

## 10. Build

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
  Duration: 41s
```

---

## 12. Integration Test

```
dotnet test ASMILE.BO.Tests.Integration
  Passed: 23, Failed: 0, Skipped: 0
  Duration: 1m 37s
```

---

## 13. Remaining Pending

| MigrationId | Schema | History | Priority |
|---|---|---|---|
| `20260718092912_AddFrontOfficeHotelStayFoundation` | MISSING | MISSING | P0 — Check-in blocker |
| `20260718095823_AddFrontOfficeHotelStayCheckIn` | MISSING | MISSING | P0 — Check-in invariant |

---

## 14. Go / No Go

### History Repair: GO ✅

| Criteria | Status |
|---|---|
| History = 83 | ✅ |
| Pending = 2 (only Hotel Stay) | ✅ |
| No duplicates | ✅ |
| Chain continuous | ✅ |
| Build PASS (0 warn, 0 err) | ✅ |
| Unit tests PASS (328/328) | ✅ |
| Integration tests PASS (23/23) | ✅ |
| Backup verified | ✅ |
| History gap resolved | ✅ |
| Schema unchanged (no DDL executed) | ✅ |

### Hotel Stay Deploy: NOT YET ⬜

Theo đúng scope sprint — dừng ở đây. History đã đồng bộ với schema. Chỉ còn 2 Hotel Stay pending. Sẵn sàng cho sprint deploy tiếp theo.

---

## 15. Database Writes Summary

| Write | Count | Type |
|---|---|---|
| Backup file created | 1 | File (COPY_ONLY .bak) |
| `__EFMigrationsHistory` INSERT — Group C | 7 | History row |
| `__EFMigrationsHistory` INSERT — VAT | 1 | History row |
| Schema DDL | **0** | — |
| Business data DML | **0** | — |
| **Total database writes** | **8 rows** | History only |

---

*Controlled history repair complete. History = 83. Pending = 2. Ready for Hotel Stay deployment sprint.*
