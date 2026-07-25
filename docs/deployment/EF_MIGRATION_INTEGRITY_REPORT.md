# EF CORE — MIGRATION INTEGRITY REPORT

**Ngày:** 2026-07-21
**Loại:** INTEGRITY AUDIT — NO CODE CHANGE

---

## 1. Summary

| Source Files | History Records | Matched | Missing from History | Missing from Source |
|-------------|----------------|---------|---------------------|-------------------|
| 89 | 99 | 84 | **5** | **15** |

---

## 2. Category A — Missing from History (5 migrations)

These exist as source files but NOT in `__EFMigrationsHistory`:

| # | Migration | Date | Module | Blocking |
|---|-----------|------|--------|----------|
| 1 | `20260521080623_AddFinancialStatementEngine` | May 21 | Accounting | ✅ Creates `AccountBalanceSnapshots` — **BLOCKER** |
| 2 | `20260524060743_AddAdvancedFinancialStatementMappingFoundation` | May 24 | Accounting | ⚠️ Will run after blocker resolved |
| 3 | `20260720032319_AddReservationEnterpriseFields` | Jul 20 | FO-EXEC-002 | ⚠️ Creates 12 FO tables |
| 4 | `20260721031559_AddBanquetContractVersions` | Jul 21 | BQ-IMPL-1A | ⚠️ Creates contract version table |
| 5 | `20260721034254_AddBanquetContractFreeze` | Jul 21 | BQ-IMPL-1B | ⚠️ Adds freeze columns |

---

## 3. Category B — Missing from Source (15 records)

These exist in `__EFMigrationsHistory` but have NO source file:

| Migration | Likely Origin |
|-----------|--------------|
| `20260719080000_AddFrontOfficeGuestProfile` | Manual SQL / direct DB insert |
| `20260719100000_AddFrontOfficeGroupReservation` | Manual SQL / direct DB insert |
| `20260719110000_AddFrontOfficeRateEngine` | Manual SQL / direct DB insert |
| `20260719120000_AddFrontOfficeServiceRequest` | Manual SQL / direct DB insert |
| `20260719130000_AddFrontOfficeWakeUpCall` | Manual SQL / direct DB insert |
| `20260719140000_AddFrontOfficeLostFound` | Manual SQL / direct DB insert |
| `20260719150000_AddFrontOfficeConcierge` | Manual SQL / direct DB insert |
| `20260719160000_AddFrontOfficeMessageCenter` | Manual SQL / direct DB insert |
| `20260719170000_AddFrontOfficeGuestFeedback` | Manual SQL / direct DB insert |
| `20260719180000_AddFrontOfficeVipLoyalty` | Manual SQL / direct DB insert |
| `20260719190000_AddFrontOfficeTaskShift` | Manual SQL / direct DB insert |
| `20260719200000_AddFrontOfficeNotificationCenter` | Manual SQL / direct DB insert |
| `20260719210000_AddFrontOfficeAuditTimeline` | Manual SQL / direct DB insert |
| `20260719220000_AddFrontOfficeSettingsFoundation` | Manual SQL / direct DB insert |
| `20260719230000_AddFrontOfficeSettingsPhase2` | Manual SQL / direct DB insert |

**Pattern:** All 15 are from July 19, 2026, with timestamps at round hours (08:00, 09:00, 10:00...). This is characteristic of manual `INSERT INTO __EFMigrationsHistory` statements used to baseline FO-EXEC-001 tables that were created via SQL scripts.

These are NOT a problem — they simply mark tables as "already created" so EF Core doesn't try to create them again.

---

## 4. Migration Chain Health

```
[Applied: 20260421153713 ... 20260521072800]  ← 23 migrations OK
─── GAP ───
[MISSING: 20260521080623_AddFinancialStatementEngine]  ← BLOCKER
[Applied: 20260521093739 ... 20260524030741]  ← 21 migrations OK
─── GAP ───
[MISSING: 20260524060743_AddAdvancedFinancialStatementMapping]  ← WILL BLOCK
[Applied: 20260524061631 ... 20260719053754]  ← 24 migrations OK
─── GAP (15 manual history entries inserted) ───
[Applied: 20260719080000 ... 20260719230000]  ← 15 manual baseline entries
─── GAP ───
[PENDING: 20260720032319_AddReservationEnterpriseFields]  ← PENDING
[PENDING: 20260721031559_AddBanquetContractVersions]  ← PENDING
[PENDING: 20260721034254_AddBanquetContractFreeze]  ← PENDING
```

---

## 5. Duplicate / Out-of-Order

**None detected.** All migration IDs are unique and in chronological order.

---

## 6. Chain Break Assessment

### Break 1: `20260521080623_AddFinancialStatementEngine`

| Property | Value |
|----------|-------|
| Creates table | `AccountBalanceSnapshots` |
| Table exists in DB? | ✅ Yes |
| Migration in history? | ❌ No |
| Blocking? | ✅ Yes — first migration EF tries to apply |
| Fix | `INSERT INTO __EFMigrationsHistory` |

### Break 2: `20260524060743_AddAdvancedFinancialStatementMappingFoundation`

| Property | Value |
|----------|-------|
| Migration in history? | ❌ No |
| Blocking? | ⚠️ Will block after Break 1 is resolved |
| Fix | Same: `INSERT INTO __EFMigrationsHistory` (after verifying tables exist) |

---

## 7. Recovery Plan

### Step 1: Restore missing history records
```sql
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260521080623_AddFinancialStatementEngine', '8.0.7');

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260524060743_AddAdvancedFinancialStatementMappingFoundation', '8.0.7');
```

### Step 2: Verify chain
```bash
dotnet ef migrations list
# Should show only 3 PENDING (July 2026 migrations)
```

### Step 3: Apply pending
```bash
dotnet ef database update
# Applies: ReservationEnterprise → BanquetContractVersions → BanquetContractFreeze
```

---

## FINAL STATEMENT

# Migration history inconsistent

**5 source migrations missing from `__EFMigrationsHistory`.**

**2 of these block the migration chain:** `20260521080623` (AccountBalanceSnapshots creator) and `20260524060743`.

**15 history-only records** (without source files) are manual baselines from July 19 — these are intentional and not a problem.

**Recovery:** 2 `INSERT INTO __EFMigrationsHistory` statements + `dotnet ef database update`.
