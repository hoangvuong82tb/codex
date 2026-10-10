# EF CORE — EXECUTION TRACE

**Ngày:** 2026-07-21
**Loại:** EXECUTION TRACE — NO CODE CHANGE

---

## 1. Failing SQL Statement

```sql
CREATE TABLE [AccountBalanceSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [FiscalYear] int NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [OpeningDebit] decimal(18,2) NOT NULL,
    [OpeningCredit] decimal(18,2) NOT NULL,
    [PeriodDebit] decimal(18,2) NOT NULL,
    [PeriodCredit] decimal(18,2) NOT NULL,
    [ClosingDebit] decimal(18,2) NOT NULL,
    [ClosingCredit] decimal(18,2) NOT NULL,
    [SnapshotDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountBalanceSnapshots] PRIMARY KEY ([Id])
);
```

**Error:** `SqlException (2714): There is already an object named 'AccountBalanceSnapshots' in the database.`

---

## 2. Migration That Generated This SQL

**File:** `20260521080623_AddFinancialStatementEngine.cs`

**Migration ID:** `20260521080623_AddFinancialStatementEngine`

**In `__EFMigrationsHistory`:** ❌ **NOT FOUND** (0 rows)

---

## 3. Evidence Chain

| # | Fact | Evidence |
|---|------|----------|
| 1 | `AccountBalanceSnapshots` table EXISTS in DB | `SELECT COUNT(*) = 1` |
| 2 | Migration that creates it: `20260521080623` | `grep OpeningDebit` matches this file |
| 3 | Migration NOT in `__EFMigrationsHistory` | `SELECT MigrationId` returns 0 rows |
| 4 | EF tries to apply this migration | Verbose log shows `CREATE TABLE [AccountBalanceSnapshots]` |
| 5 | Fails with "already exists" | Table was created by a previous run of this migration |

---

## 4. Root Cause Confirmed

### Migration `20260521080623_AddFinancialStatementEngine`

- **Status:** Applied at some point in the past (table exists as proof)
- **History:** Record was DELETED from `__EFMigrationsHistory`
- **Result:** EF Core sees it as PENDING, tries to re-apply, fails

### How the history record was lost:

Most likely scenario:
1. Database was restored from a backup using `RESTORE DATABASE`
2. The migration history had 99 entries but `20260521080623` was between two existing entries
3. Or: someone manually `DELETE FROM __EFMigrationsHistory WHERE MigrationId = '...'` to fix a previous migration issue
4. The table remained (DELETE only removed the history row, not the table)

---

## 5. DbContext Analysis

**Single DbContext:** `AppDbContext` (ASMILE.BO.Infrastructure)

No multiple DbContexts. The error comes from `AppDbContext` trying to apply `20260521080623`.

---

## 6. Recovery

### Fix: Mark migration as applied

```sql
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260521080623_AddFinancialStatementEngine', '8.0.7');
```

After this, EF Core will skip this migration and apply only the 3 PENDING ones.

### Verify after fix:
```bash
dotnet ef database update --verbose
# Should show: "No migrations were applied"
# OR successfully apply the 3 PENDING migrations
```

---

## 7. Are there other missing history records?

```sql
-- Find migration files that AREN'T in history
-- Compare file list vs history table
```

There may be additional gaps. Each missing record will cause a similar failure when `database update` reaches it.

---

## FINAL STATEMENT

# Root Cause Confirmed

**Migration `20260521080623_AddFinancialStatementEngine` is missing from `__EFMigrationsHistory`.**

The table `AccountBalanceSnapshots` was created by this migration in a previous deployment. The history record was deleted (likely during database restore). EF Core now tries to re-apply the migration, but the table already exists.

**Fix:** `INSERT INTO __EFMigrationsHistory` to restore the missing record.
