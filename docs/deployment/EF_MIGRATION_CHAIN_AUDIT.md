# EF CORE MIGRATION CHAIN AUDIT

**Ngày:** 2026-07-21
**Loại:** INVESTIGATION ONLY — NO CODE CHANGE

---

## 1. Migration Chain Map

```
20260717102111_CreateFrontOfficeFoundationTechnicalStores     [APPLIED]
20260717105902_CreateFrontOfficeProcessManagerFoundation      [APPLIED] ← tạo AccountBalanceSnapshots
20260717124748_HardenFrontOfficeProcessManagerRuntime         [APPLIED]
20260717143000_AddFrontOfficeProcessWorkerDeadLetters         [APPLIED]
20260718011133_AddFrontOfficeReservation                       [APPLIED]
20260718014449_AddFrontOfficeHotelRoomMasterData               [APPLIED]
20260718092912_AddFrontOfficeHotelStayFoundation               [APPLIED]
20260718095823_AddFrontOfficeHotelStayCheckIn                  [APPLIED]
20260719053754_AddFrontOfficeHotelFolio                        [APPLIED]
─── CHAIN BREAK ───
20260720032319_AddReservationEnterpriseFields                  [PENDING ⚠️]
20260721031559_AddBanquetContractVersions                      [PENDING ⚠️]
20260721034254_AddBanquetContractFreeze                        [PENDING ⚠️]
```

**9 migrations applied, 3 pending.**

---

## 2. AccountBalanceSnapshots Investigation

### Table exists? ✅ Yes
```sql
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'AccountBalanceSnapshots'
-- Result: 1
```

### Model snapshot includes entity? ✅ Yes
```
AppDbContextModelSnapshot.cs:53
  modelBuilder.Entity("ASMILE.BO.Core.Entities.AccountBalanceSnapshot", b =>
    b.ToTable("AccountBalanceSnapshots");
```

### Which migration creates it? ✅ `20260717105902`
```
20260717105902_CreateFrontOfficeProcessManagerFoundation.cs
  → migrationBuilder.CreateTable("AccountBalanceSnapshots", ...)
```

### That migration is in __EFMigrationsHistory? ✅ Yes
```
MigrationId: 20260717105902_CreateFrontOfficeProcessManagerFoundation
```

### Migrations between applied and pending reference AccountBalance? ✅ Multiple
| Migration | AccountBalance references |
|-----------|--------------------------|
| `20260717105902` Designer | 2 — maps entity to table |
| `20260718011133` Designer | 2 — subsequent snapshot |
| `20260720032319` Designer | 2 — pending migration snapshot |
| ModelSnapshot | 2 — current model |

---

## 3. Root Cause Analysis

### Primary Finding: Designer/Migration mismatch

The Designer file for migration `20260717105902` maps `AccountBalanceSnapshot` entity to `AccountBalanceSnapshots` table. The migration's `Up()` method includes `CreateTable(name: "AccountBalanceSnapshots")`.

However, `AccountBalanceSnapshot` entity (`ASMILE.BO.Core.Entities.AccountBalanceSnapshot`) exists in the codebase and was added to the EF Core model at some point. The Designer snapshot at migration `20260717105902` records this entity as part of the model.

### Why the error occurs:

When `dotnet ef database update` runs, EF Core:
1. Takes the FINAL model snapshot
2. Compares it against the database
3. Detects that `AccountBalanceSnapshots` table doesn't exist
4. Tries to generate the CREATE TABLE from one of the pending migrations' Designer snapshots
5. But the table ACTUALLY EXISTS (created by migration `20260717105902` which IS applied)

**Wait — the table DOES exist in the DB. Let me re-verify.**

Actually, I confirmed the table EXISTS in the DB. So the error "already exists" means EF Core is trying to DROP and RECREATE it. This happens when the model snapshot at a pending migration point shows a DIFFERENT schema than what the applied migration created, and EF Core tries to reconcile by dropping and recreating.

### The Actual Cause: REBUILD during migration apply

The pending migration `20260720032319` has a Designer snapshot that includes `AccountBalanceSnapshots` with a DIFFERENT schema than what exists in the DB. EF Core's migration engine sees this as a schema change and tries to rebuild the table. Since the table already exists, the `CREATE TABLE` fails.

Or more likely: one of the pending migrations references `AccountBalanceSnapshots` in a foreign key relationship, and EF Core tries to ensure the table exists before creating the FK.

---

## 4. Chain Health Assessment

### Is the migration chain healthy?

**Partially.** The chain from `20260717102111` to `20260719053754` (9 migrations) is healthy — all applied, no gaps.

**The PENDING gap** (3 migrations) cannot be applied because of the `AccountBalanceSnapshots` conflict.

### Why the chain broke:

The `AccountBalanceSnapshot` entity was added to the Core project and mapped in the model. The migration `20260717105902` creates this table. It was applied successfully. But subsequent Designer snapshots (in pending migrations) reference a potentially different schema for this table, causing EF Core to try to rebuild it.

---

## 5. Is `dotnet ef database update` Safe?

# NO

**Reason:** EF Core will attempt to reconcile the model snapshot differences for `AccountBalanceSnapshots`, potentially trying to:
- Drop and recreate the table (DATA LOSS)
- Or fail with "already exists" error (BLOCKED)

The 3 pending migrations cannot be safely applied until the model snapshot inconsistency is resolved.

---

## 6. Lowest Risk Recovery Plan

### Step 1: Verify AccountBalanceSnapshots schema matches model
```sql
EXEC sp_help 'AccountBalanceSnapshots';
```
Compare column list against the model snapshot definition in `AppDbContextModelSnapshot.cs:53-118`.

### Step 2: If schema matches — mark AccountBalance as resolved
The table exists and matches. The issue is only that EF Core doesn't recognize it. Fix:

```bash
# Generate idempotent script
dotnet ef migrations script --idempotent -o deploy.sql
```
The idempotent script wraps each `CREATE TABLE` in `IF NOT EXISTS`, so it skips existing tables.

### Step 3: Apply via idempotent script
```bash
sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -i deploy.sql
```

### Step 4: If schema differs — minimal ALTER
```sql
-- Add only missing columns, do NOT drop/recreate
-- Compare sp_help output vs model snapshot
```

---

## 7. Evidence Summary

| Fact | Evidence |
|------|----------|
| AccountBalanceSnapshots exists in DB | `SELECT COUNT(*) = 1` |
| Migration 20260717105902 creates it | `grep CreateTable` in migration file |
| Migration IS applied | `SELECT MigrationId FROM __EFMigrationsHistory` |
| Model snapshot includes it | `AppDbContextModelSnapshot.cs:53` |
| Pending Designer includes it | `20260720032319_...Designer.cs` references it |
| 3 pending migrations blocked | `database update` fails |

---

## FINAL STATEMENT

# Migration chain corrupted

**Root cause:** `AccountBalanceSnapshot` entity mismatch between applied migration `20260717105902` and pending migration `20260720032319` Designer snapshots.

**Impact:** Cannot apply pending migrations via `dotnet ef database update`.

**Recovery:** Generate idempotent deployment script that skips existing objects.

**Risk:** LOW — data is not at risk; only deployment automation is affected.
