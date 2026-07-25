# KET QUA PARENTCATEGORY SCHEMA REPAIR AND BASELINE

## 1. Executive Summary

**CHUA HOAN THANH theo acceptance day du.** Schema repair va history baseline da PASS an toan, nhung Menu Runtime Smoke va Banquet Runtime Smoke chua duoc thuc hien vi app dung tai LoginPanel va automation khong duoc phep tu dong dang nhap.

Ket qua database: migration `20260629114500_AddParentCategoryToMenuCategory` da tu `PARTIAL_MATCH` thanh `EXACT_MATCH`; chi 1 index, 1 self-FK va 1 history row target duoc tao. Khong co migration Hotel Stay hay migration khac duoc apply.

## 2. Scope

Da repair dung partial schema cua `ParentCategoryId`, baseline dung mot history entry target va xac minh EF. Khong sua business code, UI, connection string, migration source; khong apply `dotnet ef database update`; khong deploy Hotel Stay.

## 3. Runtime Database Identity

| Field | Result |
|---|---|
| Server | `HOANGVUONGPC\\SQLEXPRESS` |
| Database | `ASMILE_BO` |
| Login | `sa` |
| State | `ONLINE` |

## 4. Backup

Backup cu da duoc re-verify `RESTORE VERIFYONLY WITH CHECKSUM` PASS. Vi backup cu luc 18:12 khong du co so chung minh database khong thay doi truoc repair, da tao backup moi:

`D:\\ASMILE-BO\\Backups\\ASMILE_BO_before_parentcategory_schema_repair_20260718-183735.bak`

Day la full `COPY_ONLY + CHECKSUM`, VERIFYONLY PASS, size `185231872` bytes, SHA-256 `7B37E069DB2801BDBDDF557D320034CA81C29CB91CE7EE8C62AF9EC316CF449E`.

## 5. Active Feature Evidence

`ParentCategoryId` van active trong entity, DTO, EF self relationship, `MenuCatalogService`, cycle validation, child delete guard, hierarchical ordering, Menu Recipe UI va Banquet category tree. Chi tiet da duoc xac minh trong bao cao 18B.

## 6. Source Contract

| Object | Source contract |
|---|---|
| Column | `dbo.MenuCategories.ParentCategoryId uniqueidentifier NULL` |
| Index | `IX_MenuCategories_ParentCategoryId`, one key `ParentCategoryId`, non-unique, no filter |
| FK | `FK_MenuCategories_MenuCategories_ParentCategoryId`, `ParentCategoryId -> Id`, `NO ACTION/NO ACTION` |
| EF mapping | `HasIndex(ParentCategoryId)` va self relationship `DeleteBehavior.Restrict` |

Migration `Up()`, `AppDbContext` va model snapshot dong nhat.

## 7. Data Integrity

| Check | Result |
|---|---:|
| Orphan parent | 0 |
| Self parent | 0 |
| Cycle (recursive CTE) | 0 |
| ParentCategoryId nullable uniqueidentifier | PASS |

Khong co business data nao bi sua.

## 8. Fingerprint Before

`PARTIAL_MATCH`: column da ton tai dung type/nullability, nhung index va self-FK deu thieu; target history row = 0; total history = 74.

Artifact: [fingerprint-before.json](D:\\ASMILE-BO\\artifacts\\migration-governance\\20260718-183735\\fingerprint-before.json)

## 9. Repair SQL Review

Script [parentcategory-schema-repair.sql](D:\\ASMILE-BO\\artifacts\\migration-governance\\20260718-183735\\parentcategory-schema-repair.sql) dung `XACT_ABORT`, mot transaction, precondition DB/column/integrity, kiem tra conflict object name/definition, va post-condition exact. History insert khong nam trong script nay.

## 10. Approval Gate

PASS: dung server/database; fresh verified backup; integrity 0/0/0; source contract dong nhat; expected schema writes dung 1 index + 1 FK. Prompt 18C cap quyen cho repair nay.

## 11. Schema Repair Execution

Script da commit thanh cong voi result `SCHEMA_REPAIR_COMMITTED`. Khong co SQL error, lock/deadlock hay rollback.

## 12. Index Verification

`IX_MenuCategories_ParentCategoryId` ton tai, key ordinal 1 = `ParentCategoryId`, non-unique, unfiltered, enabled.

## 13. Foreign Key Verification

`FK_MenuCategories_MenuCategories_ParentCategoryId` ton tai, `dbo.MenuCategories.ParentCategoryId -> dbo.MenuCategories.Id`, `NO_ACTION/NO_ACTION`, enabled va trusted.

## 14. Fingerprint After

`EXACT_MATCH`: column, index, FK, trusted/enabled state va 0 orphan/self/cycle deu dung source contract.

Artifact: [fingerprint-after.json](D:\\ASMILE-BO\\artifacts\\migration-governance\\20260718-183735\\fingerprint-after.json)

## 15. History Baseline Plan

Baseline script chi cho phep insert sau khi recheck exact schema va integrity. ProductVersion lay tu migration history/source convention: `8.0.7`.

## 16. History Baseline Execution

Script [parentcategory-history-baseline.sql](D:\\ASMILE-BO\\artifacts\\migration-governance\\20260718-183735\\parentcategory-history-baseline.sql) da tra `HISTORY_BASELINE_INSERTED`.

## 17. Migration History Before/After

| State | Total rows | Target rows |
|---|---:|---:|
| Before | 74 | 0 |
| After | 75 | 1 |

Target row: `20260629114500_AddParentCategoryToMenuCategory`, `ProductVersion = 8.0.7`.

## 18. EF Pending State

`dotnet ef migrations list` ban dau dung default LocalDB design-time factory nen khong phan anh `ASMILE_BO`. Khong sua config: chi dat `ASMILE_BO_DESIGNTIME_CONNECTION` cho process verification. Tren dung `ASMILE_BO`, target migration khong con Pending. Chi con:

- `20260718092912_AddFrontOfficeHotelStayFoundation` (Pending)
- `20260718095823_AddFrontOfficeHotelStayCheckIn` (Pending)

Khong migration nao trong hai migration tren duoc apply.

## 19. Debug Build

PASS: `dotnet build ASMILE.BO.sln --no-restore`, 0 warning, 0 error. XAML encoding check PASS.

## 20. Release Build

PASS: `dotnet build ASMILE.BO.sln -c Release --no-restore`, 0 warning, 0 error. XAML encoding check PASS.

## 21. Unit/Integration Tests

| Test | Result |
|---|---|
| `MenuCatalogCategoryTests` | PASS 2/2 |
| Full integration suite | PASS 23/23 |

Khong co test rieng trong solution cho cycle rejection, delete/deactivate parent guard hoac Banquet tree; khong ghi nhan PASS gia cho cac test chua ton tai.

## 22. Menu Runtime Smoke

CHUA RUN. Debug app da mo thanh cong nhung hien `LoginPanel`; Computer Use khong duoc phep tu dong dang nhap. Khong tao/sua category live.

## 23. Banquet Runtime Smoke

CHUA RUN, cung blocker LoginPanel. Khong co generic dialog hay SQL error de ghi nhan vi chua vao workspace.

## 24. Database Writes

Chi co cac write duoc phep:

1. Create `IX_MenuCategories_ParentCategoryId`.
2. Create trusted self-FK `FK_MenuCategories_MenuCategories_ParentCategoryId`.
3. Insert mot history row target.
4. Create fresh COPY_ONLY backup file.

Khong co data DML nghiep vu, khong co unrelated schema/history row, khong `database update`, khong Hotel Stay.

## 25. Rollback Plan

Ke hoach rollback chi tiet: [rollback-plan.md](D:\\ASMILE-BO\\artifacts\\migration-governance\\20260718-183735\\rollback-plan.md). Khong drop `ParentCategoryId`.

## 26. Files Created

- `KET_QUA_PARENTCATEGORY_SCHEMA_REPAIR_AND_BASELINE.md`
- `artifacts/migration-governance/20260718-183735/preflight.txt`
- `artifacts/migration-governance/20260718-183735/backup-verify.txt`
- `artifacts/migration-governance/20260718-183735/data-integrity-check.txt`
- `artifacts/migration-governance/20260718-183735/parentcategory-schema-repair.sql`
- `artifacts/migration-governance/20260718-183735/parentcategory-history-baseline.sql`
- `artifacts/migration-governance/20260718-183735/fingerprint-before.json`
- `artifacts/migration-governance/20260718-183735/fingerprint-after.json`
- `artifacts/migration-governance/20260718-183735/execution-log.txt`
- `artifacts/migration-governance/20260718-183735/rollback-plan.md`

## 27. Deviations

SQL Server Express khong ho tro `WITH COMPRESSION`. Backup retry bo compression, giu `COPY_ONLY`, `CHECKSUM` va `RESTORE VERIFYONLY`; ket qua PASS.

## 28. Remaining Issues

Acceptance blocker duy nhat: Menu va Banquet read-only smoke chua duoc thuc hien sau login. Hai migration Hotel Stay van pending theo dung scope va khong phai loi cua repair nay.

## 29. Next Recommended Step

Dang nhap vao Debug app, sau do mo Menu Categories (load tree va parent selector) va Banquet category tree theo read-only smoke. Neu PASS, cap nhat report status thanh `HOAN THANH`. Chi sau do moi quay lai ke hoach governance rieng cho cac migration duoc phe duyet tiep theo.

## 30. Final Status

**CHUA HOAN THANH** — database repair, baseline, EF verification, Debug/Release build va tests PASS; live Menu/Banquet smoke dang bi chan boi LoginPanel, khong phai boi schema repair.
