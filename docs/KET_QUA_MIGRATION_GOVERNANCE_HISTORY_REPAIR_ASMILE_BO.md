# Ket qua Migration Governance - History Repair va Hotel Stay Deployment

## 1. Executive Summary

**CHUA HOAN THANH.** Backup da duoc tao va verify thanh cong. Qua trinh dung tai approval gate truoc history repair vi migration `20260629114500_AddParentCategoryToMenuCategory` la **PARTIAL_MATCH**: column da ton tai nhung index va self-FK bat buoc theo `Up()` deu khong co.

Khong co history entry nao duoc them. Khong migration Hotel Stay nao duoc apply. Khong co schema/business/UI/code change nao duoc thuc hien.

## 2. Scope

Thuc hien: preflight, backup, RESTORE VERIFYONLY, read-only schema fingerprint, history/schema evidence va rollback plan.

Khong thuc hien: `dotnet ef database update`, direct history insert, Hotel Stay deployment, code/UI/business change, connection string change.

## 3. Runtime Database Identity

| Field | Value |
| --- | --- |
| Server | `HOANGVUONGPC\SQLEXPRESS` |
| Database | `ASMILE_BO` |
| Login | `sa` |
| State | `ONLINE` |
| Recovery model | `SIMPLE` |
| Compatibility level | `160` |
| Runtime migration mode | `Disabled` |
| AllowApplyPendingMigrations | `False` |

Password va secret khong duoc ghi trong report/artifact.

## 4. Backup

| Item | Result |
| --- | --- |
| Path | `D:\ASMILE-BO\Backups\ASMILE_BO_before_migration_governance_20260718-181134.bak` |
| Mode | COPY_ONLY full backup with CHECKSUM |
| Start / End | 2026-07-18 18:12:17 / 18:12:30 +07:00 |
| Size | 185,231,872 bytes |
| RESTORE VERIFYONLY WITH CHECKSUM | PASS |
| SHA-256 | `B860632B6F7D9D489F746411F3694A0253D3AAD84FCADA6B2C0F387771D6A82E` |

## 5. Migration History Before

- Applied count: **74**
- Latest MigrationId: `20260718014449_AddFrontOfficeHotelRoomMasterData`
- Source count: **85**
- Pending before governance: **11**

Full export: `artifacts/migration-governance/20260718-181134/migration-history-before.csv`.

## 6. Fingerprint Method

Fingerprint read-only da doi chieu metadata `sys.tables`, `sys.columns`, `sys.indexes`, `sys.index_columns`, `sys.foreign_keys`, `sys.default_constraints` va `sys.sequences` voi `Up()` source.

Kiem tra bao gom type, nullable, identity/computed, default constraint, PK/index key order/filter/disabled state, FK delete/update/trusted state, va sequence metadata. Raw metadata duoc luu tai `schema-fingerprint-before.json`.

## 7. Group C Results

Bay migration Enterprise cu co schema da duoc materialize boi migration da applied `20260523042803_AddBackgroundSchedulerEngine`:

- `20260522221500_Phase3Step32SqlPerformanceScaling`
- `20260522223000_Phase3Step33EnterprisePermissions`
- `20260522224500_Phase3Step34EInvoiceFoundation`
- `20260522230000_Phase3Step35SchedulerEngine`
- `20260523010000_AddFiscalPeriodAndBranchEngine`
- `20260523012000_AddEnterpriseNotificationCenter`
- `20260523014000_AddEnterpriseSearchIndex`

Tables/indexes muc tieu deu ton tai trong `ASMILE_BO`; direct execution co rui ro duplicate-object. Day la Group C: obsolete/superseded cho **execution**, khong phai business schema bi bo.

## 8. Group B Results

| Migration | Fingerprint | Evidence |
| --- | --- | --- |
| `20260629114500_AddParentCategoryToMenuCategory` | **PARTIAL_MATCH** | `ParentCategoryId` = `uniqueidentifier NULL`, nhung khong co `IX_MenuCategories_ParentCategoryId` va khong co `FK_MenuCategories_MenuCategories_ParentCategoryId`. Orphan row count = 0, nhung khong thay the cho FK/index. |
| `20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices` | EXACT_MATCH | Bon column `IsVatIncluded` la `bit NOT NULL`, co named default constraint `DF_*_IsVatIncluded` va definition `((1))`. |

## 9. Exact / Partial / Conflict Matrix

| Group | Exact | Functional | Partial | Conflict |
| --- | ---: | ---: | ---: | ---: |
| C - 7 migration superseded | 7 | 0 | 0 | 0 |
| B - ParentCategory | 0 | 0 | 1 | 0 |
| B - VAT flags | 1 | 0 | 0 | 0 |
| Total baseline candidates | 8 | 0 | 1 | 0 |

Vi co 1 PARTIAL_MATCH, gate baseline 9-row **FAIL**.

## 10. Generated SQL Risk Review

- Group C direct execution se tao trung table/index da ton tai va co the fail.
- ParentCategory `Up()` se khong tao lai index/FK bi thieu vi column da ton tai, nen direct execution khong phuc hoi duoc full invariant.
- VAT `Up()` co `IF COL_LENGTH` va schema target da ton tai.
- Hotel Stay SQL co scope dung 2 migration, nhung khong duoc execute khi history gate chua sach.

Preview: `artifacts/migration-governance/20260718-181134/generated-sql-preview.sql`.

## 11. History Repair Plan

Ke hoach ban dau la baseline 9 MigrationId trong transaction idempotent sau khi fingerprint EXACT_MATCH. Ke hoach nay **khong duoc phe duyet de execute** vi ParentCategory PARTIAL_MATCH.

## 12. Approval Gate

| Gate | Result |
| --- | --- |
| Backup verified | PASS |
| 9 migration fingerprint exact/approved functional | **FAIL** |
| History repair authority condition | Not reached |
| History repair execution | BLOCKED |
| Hotel Stay deployment | BLOCKED |

## 13. History Repair Execution

Rows inserted: **0**.

`history-repair.sql` la script blocked va chi `THROW`; no khong co `INSERT` nao.

## 14. Migration History After Repair

Khong co repair. History van **74** rows, latest van `20260718014449_AddFrontOfficeHotelRoomMasterData`.

## 15. Remaining Pending Migration

Van **11** migration pending, vi governance dung truoc write:

- 7 Group C superseded/history-gap
- 2 Group B
- 2 Group A Hotel Stay

## 16. Hotel Stay SQL Review

Da xac minh dependency schema cua Hotel Stay chi toi:

- `fo.DatPhong`
- `fo.PhongKhachSan`
- `fo.LoaiPhongKhachSan`

Ba prerequisite deu ton tai. Hai migration Hotel Stay khong phu thuoc Enterprise Group C. Tuy nhien EF history chua nhat quan nen deploy script bi block dung governance.

## 17. Hotel Stay Deployment

**Not executed.**

- `20260718092912_AddFrontOfficeHotelStayFoundation`: not applied.
- `20260718095823_AddFrontOfficeHotelStayCheckIn`: not applied.

## 18. Database Objects

Khong co object nao duoc tao/xoa/thay doi trong run nay. `fo.LuuTruKhachSan`, `fo.LichSuLuuTruKhachSan` va sequence `fo.TrinhTuSoLuuTru` van chua ton tai.

## 19. Constraints and Indexes

Blocking difference:

- Missing `IX_MenuCategories_ParentCategoryId`.
- Missing `FK_MenuCategories_MenuCategories_ParentCategoryId`.

Do do ParentCategory khong du dieu kien baseline history.

## 20. EF Verification

EF chain verification after repair khong chay vi repair khong duoc phep. Khong claim “no pending migrations” hoac “no pending model changes”.

## 21. Schema Fingerprint Before / After

- Before: `schema-fingerprint-before.json`.
- After: `schema-fingerprint-after.json` ghi nhan no database write performed.
- Schema drift due to this governance run: **none**.

## 22. Debug Runtime

Khong run lai Debug Check-in vi Hotel Stay schema chua duoc deploy. Khong claim modal PASS.

## 23. Release Runtime

Khong run lai Release Check-in vi Hotel Stay schema chua duoc deploy. Khong claim modal PASS.

## 24. Full Build

Not run trong governance run nay; khong co code change.

## 25. Full Unit

Not run trong governance run nay.

## 26. Full Integration

Not run trong governance run nay.

## 27. Security

- Password/secret/token khong duoc ghi trong report hay artifact.
- Khong thay doi SQL login, `sa`, role hay permission.

## 28. Files Created

- `KET_QUA_MIGRATION_GOVERNANCE_HISTORY_REPAIR_ASMILE_BO.md`
- `artifacts/migration-governance/20260718-181134/preflight.txt`
- `migration-history-before.csv`
- `schema-fingerprint-before.json`
- `schema-fingerprint-after.json`
- `pending-migrations.txt`
- `generated-sql-preview.sql`
- `history-repair.sql`
- `hotel-stay-deploy.sql`
- `backup-verify.txt`
- `execution-log.txt`
- `rollback-plan.md`

## 29. Database Writes

- Backup file da duoc tao va verify.
- History insert: **0**.
- Schema DDL/DML: **0**.
- Hotel Stay deployment: **0**.

## 30. Rollback Plan

Rollback plan da luu tai `artifacts/migration-governance/20260718-181134/rollback-plan.md`.

## 31. Deviations

Prompt yeu cau chi execute history repair khi ca 9 fingerprint dat dieu kien. Gate nay khong dat, nen Phase 5-12 duoc dung co chu y. Day la dung quy tac an toan, khong phai deployment failure sau write.

## 32. Remaining Issues

Can mot task duoc phe duyet de remedy **schema gap** cua ParentCategory: tao/verify dung index va self-FK, kiem tra data, backup, fingerprint lai. Khong duoc baseline `20260629114500` chi dua tren column ton tai.

## 33. Release Recommendation

**Do not deploy Hotel Stay to ASMILE_BO yet.**

Chi tiep tuc sau khi ParentCategory fingerprint dat EXACT_MATCH hoac co approved functional exception, sau do baseline history theo script reviewable va verify pending chain chi con 2 Hotel Stay migration.

## 34. Final Status

**CHUA HOAN THANH**

Ly do: Group B ParentCategory PARTIAL_MATCH; history repair va Hotel Stay deployment da dung truoc write theo governance gate.
