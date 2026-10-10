# HOTEL MANAGEMENT — CODEX EXECUTION MASTER PROMPT

**Date:** 2026-08-10
**Architecture Contract:** `HOTEL_MANAGEMENT_PHASE_2B6_FINAL_CODEX_SPEC.md`
**Status:** IMPLEMENTED FOR PHASE 2B6 CORE CONTRACTS — ready for controlled staging validation; U-017/U-018/U-019 remain scoped follow-up units; live database gate pending

---

## 1. CODEX ROLE

You are the **implementation engineer** for ASMILE-BO Hotel Management. Your task is to implement Phase 2B.6 — persistence mapping for already-designed domain aggregates.

**You are NOT an architect.** Architecture decisions are LOCKED. You implement what is specified, nothing more, nothing less.

**You are NOT a UI designer.** You do not modify ViewModels, Views, or XAML unless explicitly directed for mock removal (U-004).

**You are a persistence engineer.** Your work is: DbContext → Entity Configuration → Repository Implementation → Database Migration → Validation.

---

## 2. PROJECT ROOT

```
D:\ASMILE-BO
```

Absolute. Never use `F:\ASMILE-BO`.

---

## 3. SOURCE OF TRUTH

The following document is your architecture contract. All decisions within it are FINAL:

```
D:\ASMILE-BO\HOTEL_MANAGEMENT_PHASE_2B6_FINAL_CODEX_SPEC.md
```

If you encounter a conflict between the spec and actual code, **STOP** and report. Do not guess.

---

## 4. PRE-FLIGHT INSPECTION

Before any modification, execute these inspections and produce a compact report:

### 4.1 Solution Structure
```bash
dotnet build D:\ASMILE-BO\ASMILE.BO.sln 2>&1 | tail -5
```
Expected: Build succeeded. If not, report errors before proceeding.

### 4.2 Key Files — Verify Existence
| File | Expected |
|---|---|
| `ASMILE.BO.Core\FrontOffice\Guest\GuestProfile.cs` | EXISTS — domain entity |
| `ASMILE.BO.Core\FrontOffice\Payment\HotelPayment.cs` | EXISTS — domain entity |
| `ASMILE.BO.Core\FrontOffice\Folio\HotelFolio.cs` | EXISTS — domain entity |
| `ASMILE.BO.Core\FrontOffice\Stay\HotelStay.cs` | EXISTS — domain entity |
| `ASMILE.BO.Core\FrontOffice\Reservation\Reservation.cs` | EXISTS — domain entity |
| `ASMILE.BO.Core\FrontOffice\HotelMasterData\HotelRooms.cs` | EXISTS — domain entity |
| `ASMILE.BO.Application\FrontOffice\Guest\GuestContracts.cs` | EXISTS — repository interface |
| `ASMILE.BO.Application\FrontOffice\Payment\PaymentContracts.cs` | EXISTS — repository + CQRS |
| `ASMILE.BO.Infrastructure\Persistence\AppDbContext.cs` | EXISTS — EF Core DbContext |

### 4.3 Current DbContext — Verify DbSets Present
```bash
grep "DbSet<" D:\ASMILE-BO\ASMILE.BO.Infrastructure\Persistence\AppDbContext.cs
```
Expected: `FrontOfficeReservations`, `HotelStays`, `HotelFolios`, `HotelFolioEntries`, `FrontOfficeGroupReservations`, `Invoices`, `InvoiceLines`, `ReceivableTransactions`.

The canonical model already exposes `GuestProfiles`, `HotelPayments` and `BusinessDates` (`BusinessDateRecord`). The remaining operational tables are tracked separately and must not be duplicated in this phase.

### 4.4 Current DI — Verify Repository Registrations
```bash
grep "Ef.*Repository\|IGuest.*Repository\|IHotelPayment" D:\ASMILE-BO\ASMILE.BO.Infrastructure\DependencyInjection.cs
```
Expected: `EfReservationRepository`, `EfHotelStayRepository`, `EfHotelFolioRepository`, `EfHotelRoomMasterDataRepository`, `EfReceivableService`.

`EfGuestProfileRepository` and `EfHotelPaymentRepository` are registered and must remain the single persistence path for guest and payment operations.

### 4.5 Verify Domain Entities Have Required Properties
| Entity | Property | Expected | Actual |
|---|---|---|---|
| `Reservation` | `GuestId` | `Guid?` | **Verify** |
| `Reservation` | `GuaranteeType` | `GuaranteeType` enum | **Verify** |
| `Reservation` | `DepositStatus` | `DepositStatus` enum | **Verify** |
| `Reservation` | `DepositAmount` | `decimal` | **Verify** |
| `HotelStay` | `GuestId` | `Guid?` | **Verify** |
| `HotelRoom` | `HousekeepingStatus` | `string?` | **Present — verify mapping** |
| `HotelFolioEntry` | `RowVersion` | `byte[]?` | **Present — verify mapping** |
| `HotelFolioEntry` | `IdempotencyKey` | `string?` | **Present — verify mapping** |

### 4.6 Verify AppDbContext Entity Configurations
```bash
grep "modelBuilder.Entity<" D:\ASMILE-BO\ASMILE.BO.Infrastructure\Persistence\AppDbContext.cs
```
Verify that Reservation config (line 929) maps `GuaranteeType`, `DepositStatus`, `DepositAmount`.
Verify that Stay config (line 933) does NOT map `GuestId`.
Verify that NO config exists for `GuestProfile`, `HotelPayment`, `LostFoundItem`.

### 4.7 Verify Existing Migrations
The folio tables (`fo.SoFolio`, `fo.ButToanFolio`) were created by migration `20260719053754_AddFrontOfficeHotelFolio`. Their configuration exists in the migration Designer file but may NOT be in the current AppDbContext `OnModelCreating`. This is a **known gap** — the folio entity configuration must be present in AppDbContext for subsequent migrations to work correctly.

**Action:** Verify that `modelBuilder.Entity<HotelFolio>()` and `modelBuilder.Entity<HotelFolioEntry>()` configurations exist in the current AppDbContext. If they are missing, add them based on the latest migration Designer snapshot.

---

## 5. CONFLICT GATE

If actual code differs from the Phase 2B.6 specification, **STOP that unit** and report:

```
UNIT: [unit-id]
FILE: [absolute path]
SYMBOL: [class/property/method name]
SPEC EXPECTATION: [what the spec says]
ACTUAL CODE: [what the code actually has]
CONFLICT: [description of the mismatch]
MINIMAL SAFE RESOLUTION: [what to do — do not execute without approval]
```

**Known potential conflicts to watch for:**

1. **HotelFolio/HotelFolioEntry config missing from AppDbContext** — The DbSets exist (lines 376-377) but the `modelBuilder.Entity<>()` configuration may be absent from `OnModelCreating`. The migration Designer snapshot has the correct configuration. If missing, add it.

2. **HotelStay.GuestId already exists on entity** — The domain entity has `GuestId` (Guid?) at line 22. Only the EF mapping and DB column are missing. Do NOT add the property to the entity.

3. **Reservation.GuestId already exists on entity** — The domain entity has `GuestId` (Guid?) at line 33. Only the EF mapping and DB column are missing. Do NOT add the property.

4. **HotelFolio.RowVersion already exists on entity** — The domain entity has `RowVersion` at line 77. Only HotelFolioEntry needs RowVersion added.

5. **KeyCardRecord is currently ignored** — `modelBuilder.Ignore<KeyCardRecord>()` at line 943. The KeyCardRecord is a value object on Stay, not a standalone aggregate. U-019 requires creating NEW standalone entities `KeyCardCredential` and `KeyCardEvent`, not reusing the ignored `KeyCardRecord`.

6. **SplitTenderLine and ExchangeRate are ignored** — Lines 947-948. These are payment value objects. When implementing U-009, decide whether to un-ignore them or keep them as value objects.

---

## 6. IMPLEMENTATION ORDER

Execute units in this exact order. Each unit is a self-contained change.

### PHASE 1: FOUNDATION (no dependencies)

#### U-003: Migration Baseline
**Objective:** Create a mechanism to validate the current live schema before any changes.

**Files to inspect:**
- `Infrastructure\Persistence\AppDbContext.cs` — current model
- `Infrastructure\Persistence\Migrations\` — migration history

**Action:**
1. Run `dotnet ef migrations script --project ASMILE.BO.Infrastructure --startup-project ASMILE.BO.App` to generate the current SQL schema
2. Save as `scripts\baseline_schema_20260810.sql`
3. Create a validation script that checks: row counts, FK existence, column existence for `fo`.* tables

**DB change:** NONE
**Code change:** Create baseline script only
**Validation:** Script can be executed read-only against live DB
**Rollback:** N/A (read-only)
**Acceptance:** Baseline script exists and matches live schema

---

#### U-001: Reservation — Add 3 Missing Columns
**Objective:** Add `GuaranteeType`, `DepositStatus`, `DepositAmount` to `fo.DatPhong` so the EF mapping (already correct at AppDbContext line 929) matches the live database.

**Files to inspect:**
- `Core\FrontOffice\Reservation\Reservation.cs` — verify properties exist (lines 61, 63, 65)
- `Infrastructure\Persistence\AppDbContext.cs` line 929 — verify EF config already maps them

**Files to modify:** NONE — EF config is already correct

**DB change:**
```sql
ALTER TABLE fo.DatPhong ADD GuaranteeType NVARCHAR(40) NULL;
ALTER TABLE fo.DatPhong ADD DepositStatus NVARCHAR(30) NULL;
ALTER TABLE fo.DatPhong ADD DepositAmount DECIMAL(18,2) NULL;
```

**Pre-migration validation:**
```sql
SELECT COUNT(*) FROM fo.DatPhong; -- Must be 10
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='fo' AND TABLE_NAME='DatPhong';
-- Verify GuaranteeType, DepositStatus, DepositAmount are present and mapped
```

**Post-migration validation:**
```sql
SELECT COUNT(*) FROM fo.DatPhong; -- Must still be 10
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='fo' AND TABLE_NAME='DatPhong';
-- Verify 3 new columns exist
SELECT * FROM fo.DatPhong WHERE GuaranteeType IS NOT NULL; -- Should be 0 (pilot data has NULL)
```

**Acceptance:** EF can query Reservation without SqlException. Pilot data intact (10 rows).

---

#### U-002: FolioEntry — Add RowVersion + IdempotencyKey
**Objective:** Add concurrency and idempotency protection to `fo.ButToanFolio`.

**Files to inspect:**
- `Core\FrontOffice\Folio\HotelFolio.cs` lines 171-197 — HotelFolioEntry entity (no RowVersion, no IdempotencyKey)

**Files to modify:**
- `Core\FrontOffice\Folio\HotelFolio.cs` — add `public byte[]? RowVersion { get; private set; }` and `public string? IdempotencyKey { get; private set; }` to `HotelFolioEntry`
- `Infrastructure\Persistence\AppDbContext.cs` — add `ConfigureRowVersion(entity.Property(x => x.RowVersion))` and `entity.Property(x => x.IdempotencyKey).HasMaxLength(100)` to `HotelFolioEntry` config (or create config if missing)

**DB change:**
```sql
ALTER TABLE fo.ButToanFolio ADD RowVersion ROWVERSION NOT NULL;
ALTER TABLE fo.ButToanFolio ADD IdempotencyKey NVARCHAR(100) NULL;
CREATE UNIQUE INDEX UQ_fo_ButToanFolio_Idempotency ON fo.ButToanFolio(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
```

**Validation:** Verify columns exist, unique index created, `dotnet build` succeeds.

---

#### U-005: Guest — Add DbSet<GuestProfile>
**Objective:** Register `GuestProfile` domain entity in AppDbContext and map to `fo.HoSoKhach`.

**Files to inspect:**
- `Core\FrontOffice\Guest\GuestProfile.cs` — full entity (62 lines)
- `Application\FrontOffice\Guest\GuestContracts.cs` — repository interface (11 methods)

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs`:
  1. Add `public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();`
  2. Add `modelBuilder.Entity<GuestProfile>(...)` configuration mapping to `fo.HoSoKhach`

**EF Configuration:**
```csharp
modelBuilder.Entity<GuestProfile>(entity => {
    entity.ToTable("HoSoKhach", "fo");
    entity.HasKey(x => x.GuestProfileId).HasName("PK_fo_HoSoKhach");
    entity.Property(x => x.GuestNumber).HasMaxLength(40);
    entity.Property(x => x.FullName).HasMaxLength(200);
    entity.Property(x => x.Phone).HasMaxLength(50);
    entity.Property(x => x.Email).HasMaxLength(200);
    entity.Property(x => x.Address).HasMaxLength(500);
    entity.Property(x => x.Nationality).HasMaxLength(40);
    entity.Property(x => x.Gender).HasMaxLength(20);
    entity.Property(x => x.EmergencyContact).HasMaxLength(200);
    entity.Property(x => x.EmergencyPhone).HasMaxLength(50);
    entity.Property(x => x.VipLevel).HasConversion<string>().HasMaxLength(30);
    entity.Property(x => x.TotalRevenue).HasColumnType("decimal(18,2)");
    entity.Property(x => x.AverageSpend).HasColumnType("decimal(18,2)");
    entity.Property(x => x.BlacklistReason).HasMaxLength(500);
    entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);
    entity.Property(x => x.CompanyName).HasMaxLength(200);
    entity.Property(x => x.Notes).HasMaxLength(1000);
    ConfigureRowVersion(entity.Property(x => x.RowVersion));
    entity.HasIndex(x => new { x.PropertyId, x.GuestNumber }).IsUnique().HasDatabaseName("UQ_fo_HoSoKhach_CoSo_SoKhach");
    entity.HasIndex(x => new { x.PropertyId, x.FullName }).HasDatabaseName("IX_fo_HoSoKhach_CoSo_Ten");
    entity.HasIndex(x => x.Phone).HasDatabaseName("IX_fo_HoSoKhach_Phone");
    entity.HasIndex(x => x.Email).HasDatabaseName("IX_fo_HoSoKhach_Email");
    // GuestDocument and GuestPreference as owned collections or separate tables
    entity.HasMany(x => x.Documents).WithOne().HasForeignKey("GuestProfileId").OnDelete(DeleteBehavior.Restrict);
    entity.HasMany(x => x.Preferences).WithOne().HasForeignKey("GuestProfileId").OnDelete(DeleteBehavior.Restrict);
    entity.Navigation(x => x.Documents).AutoInclude(false);
    entity.Navigation(x => x.Preferences).AutoInclude(false);
});
```

**DB change:** NONE — `fo.HoSoKhach` table already exists. If the existing table schema differs from the entity, add a migration to reconcile.

**Validation:** `dotnet build` succeeds. `GuestProfiles` DbSet accessible.

---

#### U-008: Room — Add HousekeepingStatus
**Objective:** Add `HousekeepingStatus` property to `HotelRoom` entity and database.

**Files to inspect:**
- `Core\FrontOffice\HotelMasterData\HotelRooms.cs` — HotelRoom entity (no HousekeepingStatus)

**Files to modify:**
- `Core\FrontOffice\HotelMasterData\HotelRooms.cs` — add `public string? HousekeepingStatus { get; private set; }`
- `Infrastructure\Persistence\AppDbContext.cs` line 932 — add `entity.Property(x => x.HousekeepingStatus).HasConversion<string>().HasMaxLength(30)`

**DB change:**
```sql
ALTER TABLE fo.PhongKhachSan ADD HousekeepingStatus NVARCHAR(30) NULL;
```

**Validation:** Column exists, `dotnet build` succeeds.

---

#### U-014: BusinessDateRecord — Canonical Table
**Objective:** Use the existing `BusinessDateRecord` aggregate and canonical `fo.NgayKinhDoanh` table for property-scoped business-date authority. Do not create a parallel `fo.BusinessDate` table.

**Files to verify:**
- `Core\FrontOffice\NightAudit\NightAudit.cs` — existing `BusinessDateRecord` entity

**Entity design:**
```csharp
public sealed class BusinessDateRecord {
    public Guid BusinessDateId { get; private set; }
    public Guid PropertyId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public BusinessDateStatus Status { get; private set; } // Open, Closing, Closed, Archived
    public DateTime OpenedAtUtc { get; private set; }
    public string? OpenedBy { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public string? ClosedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
}
```

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs` — add DbSet + config

**DB change:**
```sql
CREATE TABLE fo.NgayKinhDoanh (
    BusinessDateId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    BusinessDate DATE NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Open',
    OpenedAtUtc DATETIME2 NOT NULL,
    OpenedBy NVARCHAR(200) NULL,
    ClosedAtUtc DATETIME2 NULL,
    ClosedBy NVARCHAR(200) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_fo_NgayKinhDoanh PRIMARY KEY (BusinessDateId),
    CONSTRAINT UQ_fo_NgayKinhDoanh_CoSo_Ngay UNIQUE (PropertyId, BusinessDate)
);
```

**Validation:** `fo.NgayKinhDoanh` exists, status values are constrained, and the property/date unique index is enforced.

---

### PHASE 2: GUEST FKs (depends on U-005)

#### U-006: Reservation — Add GuestId FK
**Objective:** Add `GuestId` FK from `fo.DatPhong` to `fo.HoSoKhach`.

**Files to inspect:**
- `Core\FrontOffice\Reservation\Reservation.cs` — GuestId already exists (line 33)

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs` line 929 — add `entity.Property(x => x.GuestId)` and FK config

**DB change:**
```sql
ALTER TABLE fo.DatPhong ADD GuestId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.DatPhong ADD CONSTRAINT FK_fo_DatPhong_HoSoKhach FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId);
```

**Validation:** FK exists. Pilot data unchanged (10 rows, GuestId = NULL).

---

#### U-007: Stay — Add GuestId FK
**Objective:** Add `GuestId` FK from `fo.LuuTruKhachSan` to `fo.HoSoKhach`. Entity already has `GuestId` property (line 22).

**Files to inspect:**
- `Core\FrontOffice\Stay\HotelStay.cs` — GuestId already exists (line 22)

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs` line 933 — add `entity.Property(x => x.GuestId)` and FK config

**DB change:**
```sql
ALTER TABLE fo.LuuTruKhachSan ADD GuestId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.LuuTruKhachSan ADD CONSTRAINT FK_fo_LuuTruKhachSan_HoSoKhach FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId);
```

**Validation:** FK exists. Pilot data unchanged (5 rows, GuestId = NULL).

---

### PHASE 3: PAYMENT (depends on U-002)

#### U-009: Payment — Canonical Tables (implemented)
**Objective:** Create `fo.ThanhToan`, `fo.PhanBoThanhToan`, `fo.LichSuThanhToan` for `HotelPayment` domain entity.

**Files to inspect:**
- `Core\FrontOffice\Payment\HotelPayment.cs` — full entity (215 lines)
- `Application\FrontOffice\Payment\PaymentContracts.cs` — repository + CQRS

**Implemented files:**
- `Infrastructure\Persistence\AppDbContext.cs` — add DbSet<HotelPayment> + configs for all 3 tables
- `scripts\phase2b6_u009_payment_tables.sql` — canonical SQL contract

**Canonical DB contract (apply only through an approved migration window):**
```sql
-- Payment master
CREATE TABLE fo.ThanhToan (
    HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    HotelFolioId UNIQUEIDENTIFIER NOT NULL,
    FolioEntryId UNIQUEIDENTIFIER NULL,
    RefundEntryId UNIQUEIDENTIFIER NULL,
    PaymentNumber NVARCHAR(40) NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    RefundedAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'VND',
    ExchangeRateToVnd DECIMAL(18,6) NOT NULL DEFAULT 1,
    ExternalReference NVARCHAR(200) NULL,
    AuthorizationCode NVARCHAR(100) NULL,
    Notes NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    VoidedAtUtc DATETIME2 NULL,
    VoidedBy UNIQUEIDENTIFIER NULL,
    VoidReason NVARCHAR(500) NULL,
    RefundedAtUtc DATETIME2 NULL,
    RefundedBy UNIQUEIDENTIFIER NULL,
    RefundReason NVARCHAR(500) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_fo_ThanhToan PRIMARY KEY (HotelPaymentId),
    CONSTRAINT UQ_fo_ThanhToan_SoThanhToan UNIQUE (PaymentNumber),
    CONSTRAINT FK_fo_ThanhToan_SoFolio FOREIGN KEY (HotelFolioId) REFERENCES fo.SoFolio(HotelFolioId)
);

-- Payment allocation
CREATE TABLE fo.PhanBoThanhToan (
    HotelPaymentAllocationId UNIQUEIDENTIFIER NOT NULL,
    HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    HotelFolioId UNIQUEIDENTIFIER NOT NULL,
    ChargeFolioEntryId UNIQUEIDENTIFIER NOT NULL,
    ChargeDescription NVARCHAR(300) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT PK_fo_PhanBoThanhToan PRIMARY KEY (HotelPaymentAllocationId),
    CONSTRAINT FK_fo_PhanBoThanhToan_ThanhToan FOREIGN KEY (HotelPaymentId) REFERENCES fo.ThanhToan(HotelPaymentId),
    CONSTRAINT FK_fo_PhanBoThanhToan_ButToanFolio FOREIGN KEY (ChargeFolioEntryId) REFERENCES fo.ButToanFolio(HotelFolioEntryId)
);

-- Payment history
CREATE TABLE fo.LichSuThanhToan (
    PaymentHistoryId UNIQUEIDENTIFIER NOT NULL,
    HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    Detail NVARCHAR(500) NULL,
    OccurredAtUtc DATETIME2 NOT NULL,
    ActorId UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_fo_LichSuThanhToan PRIMARY KEY (PaymentHistoryId),
    CONSTRAINT FK_fo_LichSuThanhToan_ThanhToan FOREIGN KEY (HotelPaymentId) REFERENCES fo.ThanhToan(HotelPaymentId)
);
```

**Note on ignored types:** `SplitTenderLine` (line 947) and `ExchangeRate` (line 948) are currently ignored. For U-009, keep them ignored unless the payment handlers require them. If needed later, un-ignore and add as owned types.

**Validation:** Tables created, FKs verified, `dotnet build` succeeds.

---

### PHASE 4: REPOSITORIES (depends on U-005, U-009)

#### U-010: Guest Repository
**Objective:** Implement `EfGuestProfileRepository` implementing `IGuestProfileRepository`.

**Files to inspect:**
- `Application\FrontOffice\Guest\GuestContracts.cs` — `IGuestProfileRepository` (11 methods)

**Files to create:**
- `Infrastructure\FrontOffice\Guest\EfGuestProfileRepository.cs`

**Files to modify:**
- `Infrastructure\DependencyInjection.cs` — add:
```csharp
services.AddScoped<EfGuestProfileRepository>();
services.AddScoped<IGuestProfileRepository>(sp => sp.GetRequiredService<EfGuestProfileRepository>());
```

**Implementation:**
- Constructor: inject `AppDbContext`
- `GetAsync`: `_dbContext.GuestProfiles.Include(x => x.Documents).Include(x => x.Preferences).FirstOrDefaultAsync(...)`
- `SearchAsync`: Apply `GuestSearchFilter`, project to `GuestProfileDto`, paginate
- `AddAsync`: `_dbContext.GuestProfiles.Add(guest)`
- `UpdateAsync`: Apply expected RowVersion
- `FindDuplicatesAsync`: Query by name/phone/email
- `MergeAsync`: Transfer stays/reservations from source to target
- `MaskPiiAsync`: Set PII fields to null
- `SubmitPrivacyRequestAsync`: Create privacy request record
- `GetHistoryAsync`: Query across stays, reservations, payments
- `GetDashboardAsync`: Aggregate guest metrics

**Validation:** All 11 methods compile. `dotnet build` succeeds.

---

#### U-011: Payment Repository
**Objective:** Implement `EfHotelPaymentRepository` implementing `IHotelPaymentRepository`.

**Files to inspect:**
- `Application\FrontOffice\Payment\PaymentContracts.cs` — `IHotelPaymentRepository` (10 methods)

**Files to create:**
- `Infrastructure\FrontOffice\Payment\EfHotelPaymentRepository.cs`

**Files to modify:**
- `Infrastructure\DependencyInjection.cs` — add:
```csharp
services.AddScoped<EfHotelPaymentRepository>();
services.AddScoped<IHotelPaymentRepository>(sp => sp.GetRequiredService<EfHotelPaymentRepository>());
```

**Implementation:**
- Constructor: inject `AppDbContext`
- `GetAsync`: `_dbContext.Set<HotelPayment>().Include(x => x.Allocations).FirstOrDefaultAsync(...)`
- `GetForFolioAsync`: Query by `HotelFolioId`, project to `HotelPaymentDto`
- `GetAllocationsAsync`: Query `HotelPaymentAllocation` by payment ID
- `GetHistoryAsync`: Query `PaymentHistory` by payment ID
- `GetOutstandingAsync`: Compute from folio entries minus payment allocations
- `GetDepositSummaryAsync`: Aggregate deposit-type folio entries
- `GetSettlementSummaryAsync`: Aggregate across folios
- `AddAsync`: `_dbContext.Set<HotelPayment>().Add(payment)`
- `AddAllocationAsync`: `_dbContext.Set<HotelPaymentAllocation>().Add(allocation)`
- `ApplyExpectedRowVersion`: `_dbContext.Entry(payment).Property("RowVersion").OriginalValue = expectedRowVersion`

**Validation:** All 10 methods compile. `dotnet build` succeeds.

---

### PHASE 5: FOLIO + INVOICE + AR (depends on U-009, U-010, U-011)

#### U-015: Folio — Verify & Activate
**Objective:** Verify `fo.SoFolio` and `fo.ButToanFolio` are correctly mapped in AppDbContext. If the `modelBuilder.Entity<HotelFolio>()` and `modelBuilder.Entity<HotelFolioEntry>()` configurations are missing from `OnModelCreating`, add them based on the migration Designer snapshot.

**Files to inspect:**
- `Infrastructure\Persistence\AppDbContext.cs` — search for `HotelFolio` config
- `Infrastructure\Persistence\Migrations\20260720032319_AddReservationEnterpriseFields.Designer.cs` — snapshot has correct config

**If config missing:** Add based on Designer snapshot. Ensure `fo.SoFolio` and `fo.ButToanFolio` table mapping is correct.

**Validation:** `dotnet build` succeeds. EF can query empty folio tables.

---

#### U-012: Invoice — Verify Schema
**Objective:** Verify `dbo.Invoices` schema is compatible with hotel invoice requirements. Add `FolioId` FK only if missing.

**Files to inspect:**
- `Core\Entities\EInvoiceEntities.cs` — Invoice entity

**Action:**
1. Check live `dbo.Invoices` column list
2. If `FolioId` column missing, add it:
```sql
ALTER TABLE dbo.Invoices ADD FolioId UNIQUEIDENTIFIER NULL;
ALTER TABLE dbo.Invoices ADD CONSTRAINT FK_dbo_Invoices_SoFolio FOREIGN KEY (FolioId) REFERENCES fo.SoFolio(HotelFolioId);
```
3. Verify `IHotelInvoiceRepository` implementation exists

**Validation:** Invoice table has `FolioId` FK.

---

#### U-013: AR — Wire to Hotel Sources
**Objective:** Wire existing `EfReceivableService` to hotel folio, payment, and invoice sources.

**Files to inspect:**
- `Infrastructure\Services\EfReceivableService.cs` — existing implementation
- `Application\DTOs\ReceivableDtos.cs` — existing DTOs
- `Application\Interfaces\IReceivableService.cs` — existing interface

**Action:**
1. Verify `dbo.ReceivableTransactions` schema supports `PropertyId` and `FolioId` references
2. Add `PropertyId` column if missing:
```sql
ALTER TABLE dbo.ReceivableTransactions ADD PropertyId UNIQUEIDENTIFIER NULL;
```
3. Add `FolioId` column if missing:
```sql
ALTER TABLE dbo.ReceivableTransactions ADD FolioId UNIQUEIDENTIFIER NULL;
```
4. Update `EfReceivableService` to accept hotel folio/invoice/payment sources
5. Add `AgingSnapshot` DbSet + config if not present

**Validation:** AR service can query by PropertyId and FolioId.

---

### PHASE 6: GROUP + OPERATIONS (depends on guest/stay FKs)

#### U-016: Group — Add FKs
**Objective:** Add Guest/Stay relationships to `fo.DatPhongDoan` and `fo.PhongDoan`.

**Files to inspect:**
- `Core\FrontOffice\Group\GroupReservation.cs` — GroupReservation entity

**Action:**
1. Check if `GuestId` and `StayId` columns exist on `fo.PhanPhongDoan`
2. If missing, add:
```sql
ALTER TABLE fo.PhanPhongDoan ADD GuestId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.PhanPhongDoan ADD StayId UNIQUEIDENTIFIER NULL;
ALTER TABLE fo.PhanPhongDoan ADD CONSTRAINT FK_fo_PhongDoan_HoSoKhach FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId);
ALTER TABLE fo.PhanPhongDoan ADD CONSTRAINT FK_fo_PhongDoan_LuuTruKhachSan FOREIGN KEY (StayId) REFERENCES fo.LuuTruKhachSan(HotelStayId);
```

**Validation:** FKs exist.

---

#### U-017: Housekeeping — Remaining operational unit (separate approval)
**Objective:** Create `fo.HousekeepingTask` and `fo.RoomInspection`.

**Current status:** Not part of the Phase 2B6 core contract completion. Keep this unit pending until the housekeeping aggregate, workflow handlers, and production migration are approved together.

**Files to create:**
- `Core\FrontOffice\Housekeeping\HousekeepingTask.cs` — new entity
- `Core\FrontOffice\Housekeeping\RoomInspection.cs` — new entity

**DB change:**
```sql
CREATE TABLE fo.HousekeepingTask (
    TaskId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    RoomId UNIQUEIDENTIFIER NOT NULL,
    AssignedTo UNIQUEIDENTIFIER NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Queued',
    TaskType NVARCHAR(50) NOT NULL,
    ScheduledDate DATE NOT NULL,
    StartedAtUtc DATETIME2 NULL,
    CompletedAtUtc DATETIME2 NULL,
    Notes NVARCHAR(500) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_fo_HousekeepingTask PRIMARY KEY (TaskId),
    CONSTRAINT FK_fo_HousekeepingTask_PhongKhachSan FOREIGN KEY (RoomId) REFERENCES fo.PhongKhachSan(HotelRoomId)
);

CREATE TABLE fo.RoomInspection (
    InspectionId UNIQUEIDENTIFIER NOT NULL,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    InspectorId UNIQUEIDENTIFIER NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    Result NVARCHAR(30) NULL,
    ChecklistData NVARCHAR(MAX) NULL,
    InspectedAtUtc DATETIME2 NULL,
    Notes NVARCHAR(500) NULL,
    CONSTRAINT PK_fo_RoomInspection PRIMARY KEY (InspectionId),
    CONSTRAINT FK_fo_RoomInspection_HousekeepingTask FOREIGN KEY (TaskId) REFERENCES fo.HousekeepingTask(TaskId)
);
```

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs` — add DbSets + configs

**Validation:** Tables created, FKs verified.

---

#### U-018: Minibar — Remaining operational unit (separate approval)
**Objective:** Create `fo.MinibarSession` and `fo.MinibarConsumption`.

**Current status:** Pending separate domain and migration approval; do not apply independently of the minibar posting workflow.

**DB change:**
```sql
CREATE TABLE fo.MinibarSession (
    SessionId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    StayId UNIQUEIDENTIFIER NOT NULL,
    RoomId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Open',
    CheckedBy NVARCHAR(200) NULL,
    CheckedAtUtc DATETIME2 NULL,
    PostedAtUtc DATETIME2 NULL,
    FolioEntryId UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_fo_MinibarSession PRIMARY KEY (SessionId),
    CONSTRAINT FK_fo_MinibarSession_LuuTruKhachSan FOREIGN KEY (StayId) REFERENCES fo.LuuTruKhachSan(HotelStayId),
    CONSTRAINT FK_fo_MinibarSession_PhongKhachSan FOREIGN KEY (RoomId) REFERENCES fo.PhongKhachSan(HotelRoomId)
);

CREATE TABLE fo.MinibarConsumption (
    ConsumptionId UNIQUEIDENTIFIER NOT NULL,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    ItemCode NVARCHAR(40) NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_fo_MinibarConsumption PRIMARY KEY (ConsumptionId),
    CONSTRAINT FK_fo_MinibarConsumption_MinibarSession FOREIGN KEY (SessionId) REFERENCES fo.MinibarSession(SessionId)
);
```

**Validation:** Tables created, FKs verified.

---

#### U-019: KeyCard — Remaining operational unit (separate approval)
**Objective:** Create `fo.KeyCardCredential` and `fo.KeyCardEvent`. Note: `KeyCardRecord` is currently ignored (line 943) — it is a value object on Stay, not a standalone aggregate. Create new standalone entities.

**Current status:** Pending separate key-card integration and migration approval; do not apply independently of the card-issuance workflow.

**DB change:**
```sql
CREATE TABLE fo.KeyCardCredential (
    CredentialId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    CardSerial NVARCHAR(100) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Issued',
    StayId UNIQUEIDENTIFIER NOT NULL,
    GuestId UNIQUEIDENTIFIER NULL,
    RoomId UNIQUEIDENTIFIER NULL,
    IssuedAtUtc DATETIME2 NOT NULL,
    IssuedBy UNIQUEIDENTIFIER NULL,
    ExpiresAtUtc DATETIME2 NULL,
    DisabledAtUtc DATETIME2 NULL,
    DisabledBy UNIQUEIDENTIFIER NULL,
    DisableReason NVARCHAR(200) NULL,
    ReturnedAtUtc DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_fo_KeyCardCredential PRIMARY KEY (CredentialId),
    CONSTRAINT UQ_fo_KeyCardCredential_CardSerial UNIQUE (CardSerial),
    CONSTRAINT FK_fo_KeyCardCredential_LuuTruKhachSan FOREIGN KEY (StayId) REFERENCES fo.LuuTruKhachSan(HotelStayId),
    CONSTRAINT FK_fo_KeyCardCredential_HoSoKhach FOREIGN KEY (GuestId) REFERENCES fo.HoSoKhach(GuestProfileId)
);

CREATE TABLE fo.KeyCardEvent (
    EventId UNIQUEIDENTIFIER NOT NULL,
    CredentialId UNIQUEIDENTIFIER NOT NULL,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    OccurredAtUtc DATETIME2 NOT NULL,
    DoorId NVARCHAR(100) NULL,
    AccessGranted BIT NULL,
    Notes NVARCHAR(500) NULL,
    CONSTRAINT PK_fo_KeyCardEvent PRIMARY KEY (EventId),
    CONSTRAINT FK_fo_KeyCardEvent_KeyCardCredential FOREIGN KEY (CredentialId) REFERENCES fo.KeyCardCredential(CredentialId)
);
```

**Validation:** Tables created, FKs verified.

---

#### U-020: LostFound — Activate
**Objective:** Add `DbSet<LostFoundItem>` to AppDbContext mapping to `fo.DoThatLac`.

**Files to inspect:**
- Verify `LostFoundItem` entity exists in Core (check `Core\FrontOffice\` or `Core\Entities\`)

**Files to modify:**
- `Infrastructure\Persistence\AppDbContext.cs` — add DbSet + config

**DB change:** NONE — table already exists.

**Validation:** DbSet registered, `dotnet build` succeeds.

---

#### U-021: Audit — Integrate
**Objective:** Wire platform audit (`dbo.AuditLogs`) to FO operations. No new tables needed.

**Action:**
1. Verify `IAuditLogService` is registered in DI
2. Ensure all new repository write operations call `IAuditLogService.LogAsync()`
3. Use existing `AuditCorrelation` for cross-entity correlation

**Validation:** Audit entries created for new domain operations.

---

### PHASE 7: MOCK REMOVAL

#### U-004: Mock Removal
**Objective:** Remove runtime mock data generators from production path.

**Files to modify:**
| File | Action |
|---|---|
| `ViewModels\GuestInquiryViewModel.cs` | Wrap `BuildFakeData()` in `#if DEBUG` |
| `ViewModels\ArAgingViewModel.cs` | Wrap `BuildFakeData()` in `#if DEBUG` |
| `ViewModels\ArLedgerViewModel.cs` | Wrap mock data in `#if DEBUG` |
| `ViewModels\ArPaymentViewModel.cs` | Wrap mock data in `#if DEBUG` |
| `ViewModels\LostFoundViewModel.cs` | Wrap mock data in `#if DEBUG` |
| `ViewModels\MinibarViewModel.cs` | Wrap mock data in `#if DEBUG` |
| `ViewModels\WorkforceViewModel.cs` | Wrap mock data in `#if DEBUG` |
| `ViewModels\GroupReservationViewModel.cs` | Wrap mock data in `#if DEBUG` |

**Template:**
```csharp
public void LoadData()
{
#if DEBUG
    BuildFakeData();
#else
    InfoMessage = "Chưa có dữ liệu. Vui lòng tạo trước.";
#endif
}
```

**Validation:** In Release mode, no fake data is generated. Empty state is shown instead.

---

### U-022: ReadModels — BLOCKED
**Status: BLOCKED** — Cannot replace UI mock queries with real queries until all canonical persistence sources (U-001 through U-021) are available. This unit will be unblocked after all prior units are complete and verified.

---

## 7. DATABASE SAFETY

**Before every database change:**

1. **Backup:** Ensure a full database backup exists
2. **Pre-count:** `SELECT COUNT(*) FROM [affected_table]` — record the count
3. **Schema check:** `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='fo' AND TABLE_NAME='[table]'`
4. **Migration:** Execute the DDL
5. **Post-count:** Same row count as pre-count (unless adding rows)
6. **FK validation:** `DBCC CHECKCONSTRAINTS('[constraint_name]')`
7. **Index validation:** Verify index exists in `sys.indexes`
8. **Data preservation:** Pilot data (10 reservations, 5 stays, 12 rooms) MUST survive

**Absolutely forbidden:**
- `DROP TABLE`
- `DROP COLUMN`
- `TRUNCATE TABLE`
- `DELETE FROM`
- `DROP DATABASE`
- `RESET migration history`
- Any operation that modifies existing data

---

## 8. EF CORE IMPLEMENTATION RULES

- Use `entity.ToTable("TableName", "fo")` for FO schema
- Use `entity.ToTable("TableName", "dbo")` for dbo schema
- All PKs are `Guid` (`UNIQUEIDENTIFIER`)
- All FK delete behavior: `DeleteBehavior.Restrict`
- All enums: `.HasConversion<string>().HasMaxLength(30)`
- All amounts: `.HasColumnType("decimal(18,2)")`
- All RowVersion: `ConfigureRowVersion(entity.Property(x => x.RowVersion))`
- All business keys: unique index with `PropertyId`
- Navigation properties: `.AutoInclude(false)` (explicit include in queries)
- Property scope: add `PropertyId` to every composite index

---

## 9. REPOSITORIES

**Implement existing interfaces only.** Do not create new repository interfaces unless the contract is genuinely missing and you report the conflict.

**Repository pattern:**
```csharp
public sealed class EfXxxRepository : IXxxRepository
{
    private readonly AppDbContext _db;
    public EfXxxRepository(AppDbContext db) => _db = db;

    // Queries: AsNoTracking(), project to DTOs
    // Commands: track entity, apply RowVersion, SaveChangesAsync()
    // All methods accept PropertyId + CancellationToken
}
```

---

## 10. CQRS

Use existing CQRS contracts. For Payment, the commands and queries are already defined in `PaymentContracts.cs`. For Guest, the repository interface defines the query contract.

**Do not create new commands/queries** unless the existing contract is insufficient and you report the gap.

---

## 11. PROPERTY SCOPE

Every operational query MUST filter by `PropertyId`. Use `IFrontOfficeExecutionContextAccessor` to get the current property context.

---

## 12. AUTHORIZATION

Use `IAuthorizationService` for permission checks. Authorization belongs in the application/handler layer, not in UI or repositories.

---

## 13. FINANCIAL SAFETY

Enforce these invariants:

| # | Invariant | Where |
|---|---|---|
| F-1 | Folio balance = SUM(FolioEntry.Amount) | Repository query |
| F-2 | Allocation ≤ Payment.Amount | `HotelPayment.AllocateToCharge()` |
| F-3 | Refund ≤ RefundableAmount | `HotelPayment.Refund()` |
| F-4 | Invoice total = SUM(Lines) + Tax | Invoice domain |
| F-5 | AR balance is derived | `EfReceivableService` |
| F-6 | PaymentNumber unique | DB unique constraint |
| F-7 | IdempotencyKey unique | DB filtered unique index |
| F-8 | Append-only financial records | No UPDATE to amount fields |
| F-9 | Captured exchange rate | `ExchangeRateToVnd` on Payment |
| F-10 | Valid state transitions | `HotelPayment` state machine |

---

## 14. MOCK REMOVAL

- Production code MUST NOT call `BuildFakeData()` or any `Random`-based data generator
- DEBUG builds may include mock data for development
- Empty state: show "Chưa có dữ liệu" message, not fake data
- No `MessageBox.Show()` for error handling — use the existing `InfoMessage`/`ErrorMessage` properties

---

## 15. MIGRATIONS

**One logical migration per implementation unit.** Do not bundle unrelated changes.

**Migration naming:** `{YYYYMMDDHHMMSS}_{Description}.cs`

**Each migration must:**
1. Be generated by EF Core (`dotnet ef migrations add`)
2. Be reviewed for destructive operations
3. Be applied in a transaction where possible
4. Be validated with pre/post row counts

**No DROP/TRUNCATE/DELETE/RESET.**

---

## 16. TESTING

After each unit, run:

```bash
dotnet build D:\ASMILE-BO\ASMILE.BO.sln
```

If build fails, fix before continuing.

**Test categories required (after all units complete):**
- Unit tests for new domain entities
- Repository integration tests (against real DB)
- EF mapping tests (verify table/column/FK/index)
- Authorization tests (permission deny/allow)
- Property scope tests (cross-property access denied)
- Financial invariant tests (F-1 through F-10)
- Migration validation (pre/post counts, FK integrity)
- Regression tests (existing tests still pass)

---

## 17. FINAL VALIDATION CHECKLIST

After all units (U-001 through U-021) are complete, verify:

- [ ] `dotnet build` succeeds with 0 errors
- [ ] All existing tests pass
- [ ] `GuestProfile` DbSet accessible
- [ ] `HotelPayment` DbSet accessible
- [ ] `fo.DatPhong` has `GuaranteeType`, `DepositStatus`, `DepositAmount`, `GuestId`
- [ ] `fo.LuuTruKhachSan` has `GuestId`
- [ ] `fo.ButToanFolio` has `RowVersion`, `IdempotencyKey`
- [ ] `fo.PhongKhachSan` has `HousekeepingStatus`
- [ ] `fo.ThanhToan` table exists with FKs
- [ ] `fo.PhanBoThanhToan` table exists with FKs
- [ ] `fo.LichSuThanhToan` table exists with FK
- [ ] `fo.NgayKinhDoanh` (`BusinessDateRecord`) exists with unique constraint
- [ ] `fo.HousekeepingTask` table exists
- [ ] `fo.RoomInspection` table exists
- [ ] `fo.MinibarSession` table exists
- [ ] `fo.MinibarConsumption` table exists
- [ ] `fo.KeyCardCredential` table exists
- [ ] `fo.KeyCardEvent` table exists
- [ ] `dbo.Invoices` has `FolioId` FK
- [ ] `dbo.ReceivableTransactions` has `PropertyId`, `FolioId`
- [ ] `EfGuestProfileRepository` registered in DI
- [ ] `EfHotelPaymentRepository` registered in DI
- [ ] Pilot data intact: 10 reservations, 5 stays, 12 rooms, 3 room types
- [ ] No runtime mock generators active
- [ ] Property scope enforced on all queries
- [ ] Financial invariants enforced
- [ ] Audit entries created for material operations

---

## 18. FINAL REPORT

After completing all units, produce a report:

```
### CODEX EXECUTION REPORT

**Implemented units:** [list]
**Skipped units:** [list]
**Blocked units:** [list]
**Files changed:** [count]
**Files created:** [count]
**DB migrations created:** [count]
**Tables created:** [count]
**Columns added:** [count]
**Indexes added:** [count]
**FKs added:** [count]
**Repositories created:** [count]
**Services changed:** [count]
**Tests executed:** [count]
**Tests passed:** [count]
**Tests failed:** [count]
**Remaining risks:** [list]
**Spec/code conflicts:** [list or "None"]
```

---

**END OF CODEX EXECUTION MASTER PROMPT**

*This document is the complete implementation contract for ASMILE-BO Phase 2B.6. Codex must follow it exactly, in dependency order, with validation after each unit. No architecture redesign. No silent data changes. No destructive operations.*
