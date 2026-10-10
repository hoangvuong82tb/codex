# DATABASE DEPLOYMENT AUDIT — Banquet Enterprise

**Ngày:** 2026-07-21
**Loại:** DEPLOYMENT AUDIT — NO CODE CHANGE

---

## 1. Pending Migrations

### 3 Pending (chưa apply)

| # | Migration | Module | Tables Affected | Risk |
|---|-----------|--------|-----------------|------|
| 1 | `20260720032319_AddReservationEnterpriseFields` | FO-EXEC-002 | 12 tables (fo.*) + 10 sequences | **HIGH** |
| 2 | `20260721031559_AddBanquetContractVersions` | BQ-IMPL-1A | 1 new table | LOW |
| 3 | `20260721034254_AddBanquetContractFreeze` | BQ-IMPL-1B | 1 ALTER (4 columns) | LOW |

### 1. AddReservationEnterpriseFields — HIGH RISK

**Creates 12 tables:**
| Table | Schema | Purpose |
|-------|--------|---------|
| `CaLamViec` | fo | Cashier shifts |
| `DanhThuc` | fo | Wake-up calls |
| `DatPhongDoan` | fo | Group reservations |
| `DonDep` | fo | Housekeeping tasks |
| `HoaDon` | fo | Hotel invoices |
| `HoSoKhach` | fo | Guest profiles |
| `KeHoachGia` | fo | Rate plans |
| `KiemDem` | fo | Night audit |
| `NgayKinhDoanh` | fo | Business dates |
| `ThanhToan` | fo | Payments |
| `TrangThaiDonDep` | fo | Room housekeeping state |
| `YeuCauDichVu` | fo | Service requests |

**Creates 10 sequences:**
`TrinhTuSoCa`, `TrinhTuSoDanhThuc`, `TrinhTuSoDoan`, `TrinhTuSoDonDep`, `TrinhTuSoGia`, `TrinhTuSoHoaDon`, `TrinhTuSoKhach`, `TrinhTuSoKiemDem`, `TrinhTuSoThanhToan`, `TrinhTuSoThatLac`, `TrinhTuSoYeuCau`

**Alters 1 table:**
- `fo.DatPhong` — ADD columns: CheckedInAtUtc, CompanyName, CompletedAtUtc, GuaranteedAtUtc, IsVip, MarketSegment, TravelAgentName

**Creates FK child tables:**
- `BienDongTienMat` (Cash movements)
- `LichSuDanhThuc` (Wake-up history)
- `PhanPhongDoan` (Group room assignments)
- `GiayToKhach` (Guest documents)
- `SoThichKhach` (Guest preferences)
- `LichGia` (Rate calendar)
- `MuaGia` (Rate seasons)
- `QuyTacGia` (Rate rules)
- `BuocKiemDem` (Night audit steps)
- `DoanhThuNgay` (Revenue snapshots)
- `LostFoundHistories` (Lost & found history)
- `PhanBoThanhToan` (Payment allocations)
- `LichSuYeuCau` (Service history)

### 2. AddBanquetContractVersions — LOW RISK

**Creates 1 table:** `BanquetContractVersions`
- PK: Id
- FK: BanquetContractId → BanquetContracts (Restrict)
- Unique index: (BanquetContractId, VersionNumber)
- **Additive only — no existing data affected**

### 3. AddBanquetContractFreeze — LOW RISK

**Alters 1 table:** `BanquetContracts`
- ADD `IsFrozen` (bit, NOT NULL, DEFAULT 0)
- ADD `FrozenAtUtc` (datetime2, NULL)
- ADD `FrozenBy` (uniqueidentifier, NULL)
- ADD `FreezeReason` (nvarchar(max), NULL)
- **Additive only — all new columns default safe**

---

## 2. Deployment Plan

### Phase A — Front Office Foundation (Migration 1)
```
dotnet ef database update 20260720032319
```

Creates all FO tables needed by the application.

**Dependencies:** None — all FK targets exist (DatPhong, PhongKhachSan, LoaiPhongKhachSan)

**Risk mitigation:**
- Backup database before execution
- Run on staging first
- Verify no existing tables with same names

### Phase B — Banquet Contract Version (Migration 2)
```
dotnet ef database update 20260721031559
```

**Dependencies:** Migration 1 (FO tables) — the migration includes FO schema changes accumulated in model snapshot

### Phase C — Banquet Contract Freeze (Migration 3)
```
dotnet ef database update 20260721034254
```

**Dependencies:** Migration 2 — requires BanquetContractVersions table

### Single-Step (if no staging):
```bash
dotnet ef database update
```
Applies all 3 migrations in order.

---

## 3. High Risk Operations

| # | Migration | Operation | Risk | Mitigation |
|---|-----------|-----------|------|------------|
| 1 | Migration 1 | Creates 12 tables | **HIGH** — may conflict with existing objects | Check for existing tables before run |
| 2 | Migration 1 | Adds columns to `fo.DatPhong` | **MEDIUM** — existing data unaffected (nullable/default) | Backup |
| 3 | Migration 2 | Creates `BanquetContractVersions` | LOW | None needed |
| 4 | Migration 3 | Adds 4 columns to `BanquetContracts` | LOW | None needed |

### No operations found that:
- ❌ Drop columns
- ❌ Rename columns
- ❌ Change data types
- ❌ Change constraints (only ADD new ones)

---

## 4. Known Blocker

### `AccountBalanceSnapshots` already exists

Migration `20260717102111_CreateFrontOfficeFoundationTechnicalStores` was applied to `__EFMigrationsHistory` but its table `AccountBalanceSnapshots` already exists. This is a pre-existing condition — the table was likely created manually or from a different migration path.

This blocker affects ALL three pending migrations since EF Core validates the entire migration chain.

### Resolution
```sql
-- Option A: Skip the problematic table creation
-- (requires modifying the migration — NOT recommended)

-- Option B: Drop and recreate (DATA LOSS)
DROP TABLE IF EXISTS AccountBalanceSnapshots;
-- Then run: dotnet ef database update

-- Option C: Mark migration as already applied
-- The table already exists, so the migration is effectively done
UPDATE __EFMigrationsHistory
SET MigrationId = '20260717102111_CreateFrontOfficeFoundationTechnicalStores'
WHERE MigrationId = '20260717102111_CreateFrontOfficeFoundationTechnicalStores';
-- But this won't help since the migration IS already in history
```

**The actual issue is**: The migration IS in `__EFMigrationsHistory` but EF Core still tries to execute it because the model snapshot differs. This means the migration was applied but the model snapshot wasn't updated.

---

## 5. Idempotent Script

```bash
dotnet ef migrations script \
  --project ASMILE.BO.Infrastructure \
  --startup-project ASMILE.BO.App \
  --idempotent \
  -o deploy.sql
```

This generates an idempotent script that checks `IF NOT EXISTS` before each operation.

### Execute safely:
```bash
sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -i deploy.sql
```

---

## 6. Rollback Plan

### Migration 3 Rollback (BanquetContractFreeze)
```sql
ALTER TABLE BanquetContracts DROP COLUMN IF EXISTS IsFrozen;
ALTER TABLE BanquetContracts DROP COLUMN IF EXISTS FrozenAtUtc;
ALTER TABLE BanquetContracts DROP COLUMN IF EXISTS FrozenBy;
ALTER TABLE BanquetContracts DROP COLUMN IF EXISTS FreezeReason;
```
**Data loss:** No — all columns are new, no existing data modified.

### Migration 2 Rollback (BanquetContractVersions)
```sql
DROP TABLE IF EXISTS BanquetContractVersions;
```
**Data loss:** Yes — all version history lost. Acceptable in early deployment.

### Migration 1 Rollback (FO Reservation Enterprise)
```sql
-- DO NOT rollback in production — 12 tables + 10 sequences + 12 child tables
-- This would cause catastrophic data loss
-- Only rollback in staging/dev
```
**Data loss:** CATASTROPHIC in production.

---

## 7. Recommended Approach

Given the `AccountBalanceSnapshots` blocker and the complexity of Migration 1:

**Option A — Idempotent Script (Recommended)**
```bash
dotnet ef migrations script --idempotent -o deploy.sql
sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -i deploy.sql
```
The idempotent script checks `IF NOT EXISTS` so it skips already-existing objects.

**Option B — Manual column addition (Fastest)**
```sql
-- Add only the Banquet columns directly (skip migration tool)
ALTER TABLE BanquetContracts ADD IsFrozen bit NOT NULL DEFAULT 0;
ALTER TABLE BanquetContracts ADD FrozenAtUtc datetime2 NULL;
ALTER TABLE BanquetContracts ADD FrozenBy uniqueidentifier NULL;
ALTER TABLE BanquetContracts ADD FreezeReason nvarchar(max) NULL;

-- Create BanquetContractVersions table
CREATE TABLE BanquetContractVersions (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    BanquetContractId uniqueidentifier NOT NULL,
    VersionNumber int NOT NULL,
    TotalAmount decimal(18,2) NOT NULL,
    MenuAmount decimal(18,2) NOT NULL,
    ServiceAmount decimal(18,2) NOT NULL,
    DepositAmount decimal(18,2) NOT NULL,
    RemainingAmount decimal(18,2) NOT NULL,
    SnapshotJson nvarchar(max) NOT NULL,
    ChangeType nvarchar(40) NOT NULL,
    ChangeReason nvarchar(500) NULL,
    ChangedBy uniqueidentifier NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CompanyId uniqueidentifier NULL,
    BranchId uniqueidentifier NULL,
    CONSTRAINT FK_BanquetContractVersions_Contracts FOREIGN KEY (BanquetContractId) REFERENCES BanquetContracts(Id),
    CONSTRAINT UQ_BanquetContractVersions_Contract_Version UNIQUE (BanquetContractId, VersionNumber)
);
```
**Pros:** Immediate fix, no migration chain issues
**Cons:** Bypasses EF migration history

---

## FINAL STATEMENT

# DATABASE UPDATE BLOCKED

**Blocker:** Migration `20260717102111_CreateFrontOfficeFoundationTechnicalStores` conflicts with existing `AccountBalanceSnapshots` table.

**Recommended fix:** Generate idempotent script and execute via SQL Server.

**Banquet-specific columns can be added directly via SQL as documented in Section 7, Option B.**
