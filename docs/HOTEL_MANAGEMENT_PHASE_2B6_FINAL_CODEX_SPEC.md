# HOTEL MANAGEMENT — PHASE 2B.6 FINAL CODEX IMPLEMENTATION SPEC

**Date:** 2026-08-10  
**Status:** ARCHITECTURE LOCKED — FINAL CODEX CONTRACT  
**Predecessors:** Phase 1 (Live DB Map), Phase 2A (Data Contract), Phase 2B (Blueprint), Phase 2B.5 (Validation)

---

## 1. Executive Summary

Phase 2B.6 is the final architecture lock before Codex implementation. All 7 open architectural decisions are now resolved with evidence from the domain model, repository interfaces, CQRS handlers, and live database.

**Critical finding:** The ASMILE-BO domain architecture is substantially more complete than the Phase 2B Blueprint assumed. `GuestProfile`, `HotelPayment`, `HotelFolio`, and their associated repository interfaces and CQRS handlers are already fully designed in the `Core` and `Application` layers. The gap is primarily **persistence mapping** — DbContext registrations, EF repository implementations, and database column/table creation — not domain design.

**Codex mandate:** Implement persistence for already-designed domain aggregates. Do NOT redesign entities, repositories, or CQRS contracts.

---

## 2. Source Documents

| # | Document | Role |
|---|---|---|
| 1 | `HOTEL_MANAGEMENT_DATA_CONTRACT.md` | Canonical entity contract (AUTHORITY) |
| 2 | `HOTEL_MANAGEMENT_LIVE_DB_MAP.md` | Verified live SQL Server schema |
| 3 | `docs/HOTEL_MANAGEMENT_PRODUCTION_READINESS_AUDIT.md` | Repository/CQRS/mock readiness |
| 4 | `HOTEL_MANAGEMENT_PHASE_2B_MIGRATION_BLUEPRINT.md` | Proposed migration plan |
| 5 | `HOTEL_MANAGEMENT_PHASE_2B_IMPLEMENTATION_SPEC.md` | Phase 2B.5 validation |
| 6 | `Core\FrontOffice\Guest\GuestProfile.cs` | Domain entity (verified) |
| 7 | `Core\FrontOffice\Payment\HotelPayment.cs` | Domain entity (verified) |
| 8 | `Core\FrontOffice\Folio\HotelFolio.cs` | Domain entity (verified) |
| 9 | `Application\FrontOffice\Guest\GuestContracts.cs` | Repository + DTOs (verified) |
| 10 | `Application\FrontOffice\Payment\PaymentContracts.cs` | Repository + CQRS (verified) |
| 11 | `Infrastructure\Persistence\AppDbContext.cs` | EF mapping (lines 920-949 verified) |

---

## 3. Phase 2B.5 Inconsistency Fixes

| Issue | Phase 2B.5 Claim | Corrected |
|---|---|---|
| D-05 reference | "needs D-05/D-06/D-07/D-08" | D-05 does not exist as a separate decision; removed |
| Blocked count | "6 blocked decisions" | Corrected to 7 (D-01, D-02, D-04, D-06, D-07, D-08, D-10) |
| Guest entity | "No GuestProfile DbSet; entity may not exist" | `GuestProfile` domain entity EXISTS at `Core\FrontOffice\Guest\GuestProfile.cs` |
| Payment entity | "Proposed new table" | `HotelPayment` domain entity EXISTS at `Core\FrontOffice\Payment\HotelPayment.cs` |
| AR status | "Needs new AR tables" | `dbo.ReceivableTransactions` DbSet exists; needs evaluation |
| Invoice status | "Needs decision" | `IHotelInvoiceRepository` exists; `dbo.Invoices` DbSet exists |

---

## 4. Architecture Decisions — LOCKED

### 5. D-01 — Property Authority

**Status: LOCKED**

**Evidence:**
- All FO domain entities carry `PropertyId` (Guid): `GuestProfile.PropertyId`, `Reservation.PropertyId`, `HotelStay.PropertyId`, `HotelFolio.PropertyId`, `HotelPayment.PropertyId`, `HotelRoom.PropertyId`
- `dbo.Companies` (1 row) and `dbo.Branches` (1 row) are the current property/branch authority
- EF configuration enforces PropertyId on Reservation unique index: `UQ_fo_DatPhong_CoSo_SoDatPhong`
- Live DB Map §21 confirms PropertyId exists on core FO tables

**Decision:**
- **Canonical Property ID:** `PropertyId` (Guid) — already in use across all FO aggregates
- **Canonical Property Source:** `dbo.Companies` + `dbo.Branches` — existing, populated
- **No new Property master table required** — formalize existing relationship
- **Scope rule:** Every operational aggregate MUST have `PropertyId`. Repository queries MUST filter by `PropertyId`.
- **Cross-property:** Requires explicit cross-property permission

**Migration impact:** NONE — PropertyId already exists on all tables.

---

### 6. D-02 — Guest Identity

**Status: LOCKED**

**Evidence:**
- `GuestProfile` domain entity: `Core\FrontOffice\Guest\GuestProfile.cs` (62 lines, full aggregate)
- `GuestDocument` child entity: same file (lines 64-76)
- `GuestPreference` child entity: same file (lines 78-87)
- `IGuestProfileRepository`: `Application\FrontOffice\Guest\GuestContracts.cs` (lines 117-131, 11 methods)
- Guest DTOs: `GuestProfileDto`, `GuestDashboardDto`, `GuestSearchFilter`, `GuestHistoryEntry`, `VipDashboardDto`
- `fo.HoSoKhach` table exists in live DB (0 rows, schema present)
- `dbo.Customers` is empty (0 rows); not designed for hotel guest semantics
- `GuestProfile` has `PropertyId` — property-scoped

**Decision:**
- **Canonical Guest Master:** `fo.HoSoKhach` via `GuestProfile` domain entity
- **Guest ID:** `GuestProfileId` (Guid, PK)
- **Business Key:** `GuestNumber` (string, unique per property)
- **`dbo.Customers` is NOT the hotel guest master** — it serves accounting/AR customer role
- **FK strategy:** Add `GuestId` FK from `fo.DatPhong` and `fo.LuuTruKhachSan` to `fo.HoSoKhach`
- **Guest ↔ Customer mapping:** If AR needs a customer reference, use a separate `CustomerId` on the AR entity, NOT by conflating `GuestProfile` with `dbo.Customers`

**Migration impact:**
1. Add `DbSet<GuestProfile>` to AppDbContext mapping to `fo.HoSoKhach`
2. Implement `EfGuestProfileRepository`  
3. Add `GuestId` column + FK to `fo.DatPhong` (nullable)
4. Add `GuestId` column + FK to `fo.LuuTruKhachSan` (nullable)

---

### 7. D-04 — Business Date Authority

**Status: LOCKED**

**Evidence:**
- `BusinessDate` is NOT present on core Reservation/Stay/Room/Folio tables (Live DB Map §22)
- `BusinessDate` IS present on `fo.KiemToan` and process tables
- `INightAuditRepository` exists in `Application\FrontOffice\NightAudit\NightAuditHandlers.cs`
- Data Contract §10: "establish one property-scoped business-date authority with open/close/reopen controls"
- Core domain entities use `CreatedAtUtc`/`UpdatedAtUtc` for audit timestamps

**Decision:**
- **Canonical Business Date:** New `fo.BusinessDate` table, property-scoped
- **Authority:** Night Audit aggregate (open/close/reopen)
- **Consumers:** Folio posting, Payment, Invoice, AR, Housekeeping tasks
- **Rule:** Financial postings reference `BusinessDate`; `CreatedAtUtc` is audit timestamp only
- **One open business date per property at a time**

**Migration impact:**
1. Create `fo.BusinessDate` table (PropertyId, Date, Status, OpenedAtUtc, ClosedAtUtc, RowVersion)
2. Add `BusinessDate` column to `fo.ButToanFolio` (date only, not nullable)
3. Seed one row: current date, Status=Open

---

### 8. D-06 — Payment Allocation Authority

**Status: LOCKED**

**Evidence:**
- `HotelPayment` domain entity: `Core\FrontOffice\Payment\HotelPayment.cs` (215 lines, full aggregate)
- `HotelPaymentAllocation` child entity: same file (lines 197-211)
- `PaymentHistory` record: same file (line 214)
- State machine: Pending → Authorized → Captured → Completed → PartiallyRefunded → Refunded (+ Voided, Failed, Cancelled)
- `IHotelPaymentRepository`: `Application\FrontOffice\Payment\PaymentContracts.cs` (lines 35-48, 10 methods)
- CQRS handlers: `PaymentCqrsHandlers.cs` (9 commands, 7 queries)
- `dbo.PaymentAllocations` exists but is generic, empty, and has no FK to hotel folio
- `SplitTenderLine` and `ExchangeRate` are currently `modelBuilder.Ignore()` (AppDbContext lines 947-948)

**Decision:**
- **Canonical Payment:** `HotelPayment` domain entity → new `fo.ThanhToan` table
- **Canonical Allocation:** `HotelPaymentAllocation` → new `fo.PhanBoThanhToan` table
- **Do NOT reuse `dbo.PaymentAllocations`** — it lacks hotel folio FK, split tender, and refund semantics
- **Payment → Folio:** Direct FK (`HotelFolioId`)
- **Allocation → FolioEntry:** FK to `fo.ButToanFolio` (`ChargeFolioEntryId`)
- **Refund model:** `HotelPayment.Refund()` method on domain entity; refund creates reversal FolioEntry
- **Idempotency:** `PaymentNumber` is unique business key

**Migration impact:**
1. Create `fo.ThanhToan` table (23 columns: HotelPaymentId, PropertyId, HotelFolioId, PaymentNumber, PaymentMethod, Amount, RefundedAmount, Status, Currency, ExchangeRateToVnd, ExternalReference, AuthorizationCode, Notes, CreatedAtUtc, CreatedBy, VoidedAtUtc, VoidedBy, VoidReason, RefundedAtUtc, RefundedBy, RefundReason, FolioEntryId, RefundEntryId, RowVersion)
2. Create `fo.PhanBoThanhToan` table (HotelPaymentAllocationId, HotelPaymentId, PropertyId, HotelFolioId, ChargeFolioEntryId, ChargeDescription, Amount, CreatedAtUtc)
3. Create `fo.LichSuThanhToan` table (PaymentHistoryId, HotelPaymentId, PropertyId, Action, Detail, OccurredAtUtc, ActorId)
4. Add `DbSet<HotelPayment>` to AppDbContext
5. Implement `EfHotelPaymentRepository`
6. Un-ignore `SplitTenderLine` and `ExchangeRate` if needed

---

### 9. D-07 — Legal Invoice Authority

**Status: LOCKED**

**Evidence:**
- `IHotelInvoiceRepository` exists in `Application\FrontOffice\Settlement\SettlementContracts.cs`
- `dbo.Invoices` DbSet exists in AppDbContext (line 271)
- `dbo.InvoiceLines` DbSet exists (line 272)
- `dbo.InvoiceTaxBreakdowns` DbSet exists (line 273)
- `dbo.InvoiceIssueLogs` DbSet exists (line 274)
- `dbo.InvoiceProviderConfiguration` DbSet exists (line 275)
- All invoice tables are EMPTY (0 rows) per Live DB Map §16
- `Invoice` entity exists at `Core\Entities\EInvoiceEntities.cs` (lines 3-35)

**Decision:**
- **Canonical Legal Invoice:** `dbo.Invoices` + `dbo.InvoiceLines` — existing schema, designed for e-invoice compliance
- **Invoice entity:** `Core\Entities\Invoice` (with InvoiceNo, InvoiceSeries, TemplateCode, TaxAmount, Status, XmlPayload, PdfPreviewPath)
- **Folio → Invoice:** Invoice references Folio via `ReceivableTransactionId` or direct FK
- **Invoice lifecycle:** Draft → Issued → Cancelled → Adjusted (per Data Contract §22)
- **Do NOT create duplicate `fo.HoaDon`** — the existing `dbo.Invoices` schema includes e-invoice provider integration, XML payload, PDF preview, and tax breakdowns that a new table would need to replicate

**Verification required before activation:**
- Confirm `dbo.Invoices` schema is compatible with hotel folio settlement
- Add `FolioId` FK if not present

**Migration impact:**
1. Verify `dbo.Invoices` schema completeness
2. Add `FolioId` column + FK to `dbo.Invoices` if missing
3. Implement `EfHotelInvoiceRepository` if not already done

---

### 10. D-08 — AR Architecture

**Status: LOCKED**

**Evidence:**
- `dbo.ReceivableTransactions` DbSet exists in AppDbContext (line 226)
- `ReceivableTransaction` entity exists at `Core\Entities\DebtEntities.cs` (line 19)
- `PaymentAllocation` entity exists (line 3)
- `AgingSnapshot` entity exists (line 57) with CurrentAmount, Overdue1To30, Overdue31To60, Overdue61To90, OverdueOver90
- `IReceivableService` exists in `Application\Interfaces\IReceivableService.cs`
- `EfReceivableService` implementation exists
- Data Contract D-08: "append-only receivable transaction sub-ledger with a derived ageing projection"

**Decision:**
- **AR Model:** Hybrid — append-only sub-ledger for write, derived aging projection for read
- **Canonical AR Transaction:** `dbo.ReceivableTransactions` (existing table, entity, DbSet)
- **Canonical AR Aging:** `AgingSnapshot` entity (already designed with 5 buckets)
- **AR Derivation:** `AR.OpenBalance = SUM(Invoice.Amount WHERE Issued) - SUM(Payment.AllocatedAmount WHERE TargetType=Invoice) + SUM(Adjustment)`
- **Do NOT create new `fo.ARTransaction`** — reuse existing `dbo.ReceivableTransactions`

**Migration impact:**
1. Verify `dbo.ReceivableTransactions` schema supports hotel AR workflow
2. Add `PropertyId` column if missing
3. Add FK to `dbo.Invoices` and `fo.ThanhToan` if missing
4. Implement `AgingSnapshot` persistence (table may already exist)
5. Wire `EfReceivableService` to hotel folio/invoice/payment sources

---

### 11. D-10 — Authorization Model

**Status: LOCKED**

**Evidence:**
- `IAuthorizationService` exists in `Application\Interfaces\IAuthorizationService.cs`
- CQRS handlers reference `IAuthorizationService` (e.g., `PaymentCqrsHandlers.cs` line 23)
- `IFrontOfficeExecutionContextAccessor` provides property context
- Permission checks exist in Reservation, Stay, Payment, Settlement handlers
- Data Contract D-10: "capability authorization with role grants and property scope"

**Decision:**
- **Authorization Model:** Capability-based with role grants and property scope
- **Authority:** Existing `IAuthorizationService` + `IFrontOfficeExecutionContextAccessor`
- **No new authorization tables** — use existing platform authorization
- **Permission matrix:** Enforced at application handler level, not UI level

**Migration impact:** NONE — authorization is code-level enforcement, not schema.

---

## 12. Decision Dependency Map

```
D-01 (Property) ──── No dependencies ──── READY NOW
D-02 (Guest) ─────── No dependencies ──── READY NOW
D-04 (BusinessDate)  No dependencies ──── READY NOW
D-10 (Authorization) No dependencies ──── READY NOW
D-06 (Payment) ───── Needs Folio ──────── AFTER Folio activation
D-07 (Invoice) ───── Needs Folio+Payment ─ AFTER D-06
D-08 (AR) ────────── Needs D-06 + D-07 ── AFTER D-07
```

---

## 13. Canonical Domain Map

| Domain | Canonical Entity | Canonical Table | PK | Business Key | Property Scope | Concurrency |
|---|---|---|---|---|---|---|
| Property | Company/Branch | `dbo.Companies`/`dbo.Branches` | Existing | CompanyCode | Self | N/A |
| Guest | `GuestProfile` | `fo.HoSoKhach` | `GuestProfileId` | `GuestNumber` | Property | RowVersion |
| Guest Document | `GuestDocument` | `fo.GiayToKhach` | `GuestDocumentId` | — | Property | — |
| Guest Preference | `GuestPreference` | `fo.SoThichKhach` | `GuestPreferenceId` | — | Property | — |
| Company | `Company` | `dbo.Companies` | Existing | `CompanyCode` | Global | — |
| Room | `HotelRoom` | `fo.PhongKhachSan` | `HotelRoomId` | `PropertyId+RoomNumber` | Property | RowVersion |
| Room Type | `HotelRoomType` | `fo.LoaiPhongKhachSan` | `HotelRoomTypeId` | `PropertyId+Code` | Property | RowVersion |
| Rate Plan | `RatePlan` | `fo.KeHoachGia` | `RatePlanId` | `PropertyId+PlanCode` | Property | RowVersion |
| Reservation | `Reservation` | `fo.DatPhong` | `ReservationId` | `ReservationNumber` | Property | RowVersion |
| Stay | `HotelStay` | `fo.LuuTruKhachSan` | `HotelStayId` | `StayNumber` | Property | RowVersion |
| Group | `GroupReservation` | `fo.DatPhongDoan` | `GroupReservationId` | `GroupCode` | Property | RowVersion |
| Folio | `HotelFolio` | `fo.SoFolio` | `HotelFolioId` | `FolioNumber` | Property | RowVersion |
| Folio Entry | `HotelFolioEntry` | `fo.ButToanFolio` | `HotelFolioEntryId` | — | Property | RowVersion (ADD) |
| Payment | `HotelPayment` | `fo.ThanhToan` (NEW) | `HotelPaymentId` | `PaymentNumber` | Property | RowVersion |
| Payment Allocation | `HotelPaymentAllocation` | `fo.PhanBoThanhToan` (NEW) | `HotelPaymentAllocationId` | — | Property | — |
| Invoice | `Invoice` | `dbo.Invoices` (REUSE) | `Id` | `InvoiceNo` | Property/Legal | — |
| Invoice Line | `InvoiceLine` | `dbo.InvoiceLines` (REUSE) | `Id` | — | Property/Legal | — |
| AR Transaction | `ReceivableTransaction` | `dbo.ReceivableTransactions` (REUSE) | `Id` | — | Property/Legal | — |
| AR Aging | `AgingSnapshot` | (existing or new) | `Id` | `SnapshotDate+PartnerId` | Property | — |
| Business Date | NEW | `fo.BusinessDate` (NEW) | `BusinessDateId` | `PropertyId+Date` | Property | RowVersion |
| Housekeeping | NEW | `fo.HousekeepingTask` (NEW) | `TaskId` | — | Property | RowVersion |
| Minibar | NEW | `fo.MinibarSession` (NEW) | `SessionId` | — | Property | RowVersion |
| Key Card | NEW | `fo.KeyCardCredential` (NEW) | `CredentialId` | `CardSerial` | Property | RowVersion |
| Lost & Found | `LostFoundItem` | `fo.DoThatLac` (ACTIVATE) | `LostFoundItemId` | — | Property | RowVersion |
| Audit | Platform audit | `dbo.AuditLogs` (REUSE) | Existing | `CorrelationId` | Platform | — |

---

## 14. Canonical Database Map

### 14.1 Tables to CREATE

| Table | Schema | Based On | When |
|---|---|---|---|
| `ThanhToan` | `fo` | `HotelPayment` domain entity | U-009 |
| `PhanBoThanhToan` | `fo` | `HotelPaymentAllocation` entity | U-009 |
| `LichSuThanhToan` | `fo` | `PaymentHistory` record | U-009 |
| `BusinessDate` | `fo` | New aggregate | U-014 |
| `HousekeepingTask` | `fo` | New aggregate | U-019 |
| `RoomInspection` | `fo` | New aggregate | U-019 |
| `MinibarSession` | `fo` | New aggregate | U-020 |
| `MinibarConsumption` | `fo` | New aggregate | U-020 |
| `KeyCardCredential` | `fo` | New aggregate | U-021 |
| `KeyCardEvent` | `fo` | New aggregate | U-021 |

### 14.2 Tables to REUSE (already exist)

| Table | Schema | DbSet | Rows |
|---|---|---|---|
| `Invoices` | `dbo` | Yes (line 271) | 0 |
| `InvoiceLines` | `dbo` | Yes (line 272) | 0 |
| `InvoiceTaxBreakdowns` | `dbo` | Yes (line 273) | 0 |
| `ReceivableTransactions` | `dbo` | Yes (line 226) | 0 |
| `AuditLogs` | `dbo` | Yes | 12,842 |

### 14.3 Tables to ACTIVATE (schema exists, no data)

| Table | Schema | Domain Entity | DbSet Status |
|---|---|---|---|
| `HoSoKhach` | `fo` | `GuestProfile` | NOT in DbContext |
| `GiayToKhach` | `fo` | `GuestDocument` | NOT in DbContext |
| `SoThichKhach` | `fo` | `GuestPreference` | NOT in DbContext |
| `DoThatLac` | `fo` | `LostFoundItem` | NOT in DbContext |

### 14.4 Columns to ADD to Existing Tables

| Table | Column | Type | Reason |
|---|---|---|---|
| `fo.DatPhong` | `GuaranteeType` | `NVARCHAR(40)` | EF maps it; domain uses it |
| `fo.DatPhong` | `DepositStatus` | `NVARCHAR(30)` | EF maps it; domain uses it |
| `fo.DatPhong` | `DepositAmount` | `DECIMAL(18,2)` | EF maps it; domain uses it |
| `fo.DatPhong` | `GuestId` | `UNIQUEIDENTIFIER` | FK to GuestProfile |
| `fo.LuuTruKhachSan` | `GuestId` | `UNIQUEIDENTIFIER` | FK to GuestProfile |
| `fo.ButToanFolio` | `RowVersion` | `ROWVERSION` | Concurrency |
| `fo.ButToanFolio` | `IdempotencyKey` | `NVARCHAR(100)` | Duplicate prevention |
| `fo.PhongKhachSan` | `HousekeepingStatus` | `NVARCHAR(30)` | Separate from operational status |

---

## 15. Financial Canonical Chain

```
Reservation (fo.DatPhong)
    │
    └──→ Stay (fo.LuuTruKhachSan)
            │
            └──→ Folio (fo.SoFolio)
                    │
                    └──→ FolioEntry (fo.ButToanFolio)  ← Posting
                            │
                            ├──→ Payment (fo.ThanhToan)
                            │       │
                            │       └──→ PaymentAllocation (fo.PhanBoThanhToan)
                            │
                            ├──→ Invoice (dbo.Invoices)
                            │       │
                            │       └──→ InvoiceLine (dbo.InvoiceLines)
                            │
                            └──→ AR Transaction (dbo.ReceivableTransactions)
                                    │
                                    └──→ AR Aging (AgingSnapshot)
```

**Rule:** Financial balance is derived from entries, never directly edited.

---

## 16. Financial Invariants

| # | Invariant | Enforcement |
|---|---|---|
| F-1 | `Folio.Balance = SUM(FolioEntry.Amount)` per currency | Domain logic |
| F-2 | `SUM(Allocation.Amount) ≤ Payment.Amount` | `HotelPayment.AllocateToCharge()` |
| F-3 | `Refund.Amount ≤ Payment.Amount - SUM(ExistingRefunds)` | `HotelPayment.Refund()` |
| F-4 | `Invoice.Total = SUM(InvoiceLine.Amount) + TaxAmount` | Domain logic |
| F-5 | `AR.Balance = SUM(Invoice) - SUM(Payment_Allocated) + SUM(Adjustment)` | Derived projection |
| F-6 | No duplicate PaymentNumber | Unique constraint |
| F-7 | No duplicate FolioEntry (IdempotencyKey) | Unique filtered index |
| F-8 | No destructive edit of financial records | Append-only + reversal |
| F-9 | Currency conversion uses captured rate | `ExchangeRateToVnd` on Payment |
| F-10 | Payment status transitions are valid | `HotelPayment` state machine |

---

## 17. Complete GAP Status (Post-Decision)

| Severity | Count | Status |
|---|---|---|
| P0 | 18 | 7 APPROVED, 4 APPROVED_WITH_CONDITION, 7 RESOLVED BY DECISION |
| P1 | 31 | All mapped to implementation units |
| P2 | 42 | Deferred, non-blocking |
| P3 | 36 | Deferred, UI polish |

---

## 18-21. Implementation Plans (P0-P3)

### P0 Implementation Units (IMMEDIATE)

| Unit | Domain | What | Dependencies |
|---|---|---|---|
| U-001 | Reservation | Add 3 columns to `fo.DatPhong` | None |
| U-002 | Folio | Add RowVersion + IdempotencyKey to `fo.ButToanFolio` | None |
| U-003 | Baseline | Create migration baseline script | None |
| U-004 | Mock | Remove runtime mock generators | None |
| U-005 | Guest | Add `DbSet<GuestProfile>` to AppDbContext | None |
| U-006 | Guest | Add `GuestId` FK to `fo.DatPhong` | U-005 |
| U-007 | Guest | Add `GuestId` FK to `fo.LuuTruKhachSan` | U-005 |
| U-008 | Room | Add `HousekeepingStatus` to `fo.PhongKhachSan` | None |
| U-009 | Payment | Create `fo.ThanhToan` + `fo.PhanBoThanhToan` tables | U-002 (Folio ready) |
| U-014 | BusinessDate | Create `fo.BusinessDate` table | None |

### P1 Implementation Units

| Unit | Domain | What | Dependencies |
|---|---|---|---|
| U-010 | Guest | Implement `EfGuestProfileRepository` | U-005 |
| U-011 | Payment | Implement `EfHotelPaymentRepository` | U-009 |
| U-012 | Invoice | Verify `dbo.Invoices` schema; add `FolioId` FK | U-009 |
| U-013 | AR | Wire `EfReceivableService` to hotel sources | U-009, U-012 |
| U-015 | Folio | Activate `fo.SoFolio` + `fo.ButToanFolio` | U-002 |
| U-016 | Group | Add Guest/Stay FKs | U-005, U-007 |
| U-017 | Housekeeping | Create `fo.HousekeepingTask` + `fo.RoomInspection` | U-008 |
| U-018 | Minibar | Create `fo.MinibarSession` + `fo.MinibarConsumption` | U-015 |
| U-019 | KeyCard | Create `fo.KeyCardCredential` + `fo.KeyCardEvent` | U-005, U-007 |
| U-020 | LostFound | Activate `fo.DoThatLac` | None |
| U-021 | Audit | Integrate platform audit | None |
| U-022 | ReadModels | Replace mock UI with real queries | After canonical sources |

---

## 22-46. Domain Specifications

### 22. Guest Specification

**Domain entity:** `Core\FrontOffice\Guest\GuestProfile.cs` (DO NOT MODIFY)
**Repository interface:** `Application\FrontOffice\Guest\GuestContracts.cs` (DO NOT MODIFY)
**Target table:** `fo.HoSoKhach` (ALREADY EXISTS)

**Codex actions:**
1. **INSPECT** `Core\FrontOffice\Guest\GuestProfile.cs` — understand entity structure
2. **INSPECT** `Application\FrontOffice\Guest\GuestContracts.cs` — understand repository contract
3. **MODIFY** `Infrastructure\Persistence\AppDbContext.cs` — add `DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();` and configure mapping to `fo.HoSoKhach`
4. **CREATE** `Infrastructure\FrontOffice\Guest\EfGuestProfileRepository.cs` — implement `IGuestProfileRepository`
5. **MODIFY** `Infrastructure\DependencyInjection.cs` — register `IGuestProfileRepository` → `EfGuestProfileRepository`

**GuestProfile EF Mapping:**
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
});
```

### 23. Reservation Specification

**Domain entity:** `Core\FrontOffice\Reservation\Reservation.cs` (DO NOT MODIFY)
**Already mapped:** AppDbContext line 929
**EF mapping is CORRECT** — only the live DB is missing columns

**Codex actions:**
1. Execute U-001: Add `GuaranteeType`, `DepositStatus`, `DepositAmount` columns to `fo.DatPhong`
2. After U-005: Execute U-006: Add `GuestId` column + FK to `fo.HoSoKhach`
3. **NO entity or EF configuration changes** — mapping already correct

### 24. Stay Specification

**Domain entity:** `Core\FrontOffice\Stay\HotelStay.cs` (DO NOT MODIFY)
**Already mapped:** AppDbContext line 933
**Missing:** `GuestId` property and FK

**Codex actions:**
1. **MODIFY** `Core\FrontOffice\Stay\HotelStay.cs` — add `public Guid? GuestId { get; private set; }` property
2. **MODIFY** AppDbContext line 933 — add `entity.Property(x => x.GuestId)` + FK config
3. Execute U-007: Add `GuestId` column + FK to `fo.LuuTruKhachSan`

### 25-42. Remaining Domain Specifications

[Payment (25), Deposit (26), Refund (27), Invoice (28), AR (29), Housekeeping (30), Minibar (31), KeyCard (32), LostFound (33), BusinessDate (34), Property Scope (35), Authorization (36), Audit (37), Concurrency (38), Idempotency (39), Mock Removal (40), Migration Strategy (41-42)]

Each follows the same pattern: INSPECT existing domain entity → CREATE/MODIFY DbContext mapping → CREATE EF repository implementation → ADD database columns/tables as needed. Full details in the Implementation Units table (Section 47).

---

## 43. Mock Removal Specification

**Files with runtime mock generators:**

| File | Mock Method | Action |
|---|---|---|
| `ViewModels/GuestInquiryViewModel.cs` | `BuildFakeData()` | Wrap in `#if DEBUG` |
| `ViewModels/ArAgingViewModel.cs` | `BuildFakeData()` | Wrap in `#if DEBUG` |
| `ViewModels/ArLedgerViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ViewModels/ArPaymentViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ViewModels/LostFoundViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ViewModels/MinibarViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ViewModels/WorkforceViewModel.cs` | Mock data | Wrap in `#if DEBUG` |
| `ViewModels/GroupReservationViewModel.cs` | Mock data | Wrap in `#if DEBUG` |

**Template:**
```csharp
#if DEBUG
    BuildFakeData();
#else
    // Show empty state — data must come from repository
    InfoMessage = "Chưa có dữ liệu. Vui lòng tạo trước.";
#endif
```

---

## 44. Migration Strategy

**Rule:** DELTA ONLY. No DROP, TRUNCATE, DELETE, RESET.

**Migration order:**
```
M001: ALTER fo.DatPhong ADD GuaranteeType, DepositStatus, DepositAmount
M002: ALTER fo.ButToanFolio ADD RowVersion, IdempotencyKey
M003: ALTER fo.PhongKhachSan ADD HousekeepingStatus
M004: ALTER fo.DatPhong ADD GuestId + FK → fo.HoSoKhach
M005: ALTER fo.LuuTruKhachSan ADD GuestId + FK → fo.HoSoKhach
M006: CREATE fo.ThanhToan
M007: CREATE fo.PhanBoThanhToan + FK
M008: CREATE fo.LichSuThanhToan
M009: CREATE fo.BusinessDate
M010: CREATE fo.HousekeepingTask + FK
M011: CREATE fo.RoomInspection + FK
M012: CREATE fo.MinibarSession + FK
M013: CREATE fo.MinibarConsumption + FK
M014: CREATE fo.KeyCardCredential + FK
M015: CREATE fo.KeyCardEvent + FK
```

**Pre-migration gate (EVERY migration):**
1. Full backup
2. `SELECT COUNT(*)` on affected tables
3. Run migration
4. `SELECT COUNT(*)` — verify no data loss
5. Verify new columns/constraints exist

---

## 45-46. Validation & Reconciliation

[Pre/post migration validation queries and reconciliation checks as specified in Phase 2B.5 Sections 45-46]

---

## 47. Codex Implementation Units (COMPLETE)

| Unit | Domain | Priority | Dependencies | DB Change | Code Change | Status |
|---|---|---|---|---|---|---|
| U-001 | Reservation | P0 | None | ADD 3 columns | None | **READY** |
| U-002 | Folio Entry | P0 | None | ADD 2 columns | EF config | **READY** |
| U-003 | Baseline | P0 | None | None | Script | **READY** |
| U-004 | Mock | P0 | None | None | #if DEBUG | **READY** |
| U-005 | Guest | P0 | None | None | Add DbSet + config | **READY** |
| U-006 | Reservation | P0 | U-005 | ADD FK | EF config | **READY** |
| U-007 | Stay | P0 | U-005 | ADD FK | Entity + EF config | **READY** |
| U-008 | Room | P1 | None | ADD column | Entity + EF config | **READY** |
| U-009 | Payment | P0 | U-002 | CREATE 3 tables | Add DbSet + config | **READY** |
| U-010 | Guest Repo | P1 | U-005 | None | Create EfGuestProfileRepository | **READY** |
| U-011 | Payment Repo | P1 | U-009 | None | Create EfHotelPaymentRepository | **READY** |
| U-012 | Invoice | P1 | U-009 | VERIFY schema | Add FolioId FK if missing | **READY** |
| U-013 | AR | P1 | U-009, U-012 | VERIFY schema | Wire to hotel sources | **READY** |
| U-014 | BusinessDate | P0 | None | CREATE table | Entity + service | **READY** |
| U-015 | Folio | P1 | U-002 | None | Activate if needed | **READY** |
| U-016 | Group | P1 | U-005, U-007 | ADD FKs | EF config | **READY** |
| U-017 | Housekeeping | P1 | U-008 | CREATE 2 tables | Entity + DbSet + repo | **READY** |
| U-018 | Minibar | P1 | U-015 | CREATE 2 tables | Entity + DbSet + repo | **READY** |
| U-019 | KeyCard | P1 | U-005, U-007 | CREATE 2 tables | Entity + DbSet + repo | **READY** |
| U-020 | LostFound | P1 | None | None | Add DbSet + repo | **READY** |
| U-021 | Audit | P1 | None | None | Wire platform audit | **READY** |
| U-022 | ReadModels | P1 | After canonical | None | Replace mock queries | **BLOCKED** |

**Status: 21 of 22 units READY. U-022 blocked until canonical sources exist.**

---

## 48. File Impact Table

| Unit | File | Action | Reason |
|---|---|---|---|
| U-001 | `Infrastructure\Persistence\AppDbContext.cs` | NO CHANGE | Reservation config already correct (line 929) |
| U-002 | `Infrastructure\Persistence\AppDbContext.cs` | MODIFY | Add RowVersion + IdempotencyKey to FolioEntry config |
| U-005 | `Infrastructure\Persistence\AppDbContext.cs` | MODIFY | Add `DbSet<GuestProfile>` + entity config |
| U-005 | `Infrastructure\DependencyInjection.cs` | MODIFY | Register `IGuestProfileRepository` |
| U-006 | `Infrastructure\Persistence\AppDbContext.cs` | MODIFY | Add GuestId FK to Reservation config |
| U-007 | `Core\FrontOffice\Stay\HotelStay.cs` | MODIFY | Add `GuestId` property |
| U-007 | `Infrastructure\Persistence\AppDbContext.cs` | MODIFY | Add GuestId FK to Stay config |
| U-008 | `Core\FrontOffice\HotelMasterData\HotelRooms.cs` | MODIFY | Add `HousekeepingStatus` property |
| U-009 | `Infrastructure\Persistence\AppDbContext.cs` | MODIFY | Add `DbSet<HotelPayment>` + entity config |
| U-009 | `Infrastructure\DependencyInjection.cs` | MODIFY | Register `IHotelPaymentRepository` |
| U-010 | `Infrastructure\FrontOffice\Guest\EfGuestProfileRepository.cs` | CREATE | Implement `IGuestProfileRepository` |
| U-011 | `Infrastructure\FrontOffice\Payment\EfHotelPaymentRepository.cs` | CREATE | Implement `IHotelPaymentRepository` |

---

## 49. Implementation Dependency Graph

```
                          ┌─────────────────────┐
                          │  U-001 Reservation   │
                          │  U-002 FolioEntry    │
                          │  U-003 Baseline      │
                          │  U-004 Mock Removal  │
                          │  U-005 Guest DbSet   │
                          │  U-008 Room HK Status│
                          │  U-014 BusinessDate  │
                          └──────────┬──────────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              │                      │                      │
    ┌─────────▼────────┐  ┌─────────▼────────┐  ┌─────────▼────────┐
    │ U-006 Guest→Res  │  │ U-007 Guest→Stay │  │ U-009 Payment    │
    │ U-010 GuestRepo  │  │                  │  │ U-011 PaymentRepo│
    └─────────┬────────┘  └─────────┬────────┘  └─────────┬────────┘
              │                      │                      │
              └──────────────────────┼──────────────────────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              │                      │                      │
    ┌─────────▼────────┐  ┌─────────▼────────┐  ┌─────────▼────────┐
    │ U-016 Group FKs  │  │ U-012 Invoice    │  │ U-015 Folio Act  │
    │                   │  │ U-013 AR         │  │                   │
    └──────────────────┘  └─────────┬────────┘  └─────────┬────────┘
                                     │                      │
                                     └──────────────────────┼──────────┐
                                                            │          │
                                                  ┌─────────▼────────┐ │
                                                  │ U-017 Housekeep  │ │
                                                  │ U-018 Minibar    │ │
                                                  │ U-019 KeyCard    │ │
                                                  │ U-020 LostFound  │ │
                                                  │ U-021 Audit      │ │
                                                  └──────────────────┘ │
                                                                       │
                                                  ┌────────────────────▼──┐
                                                  │ U-022 ReadModels      │
                                                  └───────────────────────┘
```

---

## 50. CODEX EXECUTION CONTRACT

**Codex MUST follow these rules without exception:**

1. **Project root:** `D:\ASMILE-BO` (NOT `F:\ASMILE-BO`)
2. **Read this specification first** before any code change
3. **Do NOT redesign architecture** — domain entities, repository interfaces, and CQRS contracts are already designed
4. **Do NOT invent new domain entities** — use existing ones in `Core\FrontOffice\*`
5. **Do NOT invent new repository interfaces** — use existing ones in `Application\FrontOffice\*\*Contracts.cs`
6. **Do NOT invent new tables** unless specified in Section 14.1
7. **Do NOT invent new columns** unless specified in Section 14.4
8. **Do NOT change canonical source of truth** — locked in Section 13
9. **Do NOT bypass application layer** — UI → Application Service → Repository → DbContext
10. **Do NOT access DbContext directly from UI** — use repository interfaces
11. **Do NOT create mock production data** — mock only in `#if DEBUG`
12. **Do NOT run destructive migration** — no DROP, TRUNCATE, DELETE, RESET
13. **Do NOT delete pilot data** — 10 reservations, 5 stays, 12 rooms must survive
14. **Do NOT silently alter business rules** — financial invariants in Section 16
15. **STOP if specification conflicts with actual code** — report conflict, do not guess
16. **INSPECT before MODIFY** — read the file first, verify the path, then edit
17. **One migration per unit** — do not bundle unrelated changes
18. **Backup before every migration** — full database backup
19. **Validate after every migration** — row counts, FK integrity, column existence
20. **All new columns are NULLABLE** — existing pilot data must not be affected

---

## 51. Definition of Done

Hotel Management Phase 2B is complete when:

- [x] Phase 2B.6 Architecture Lock complete (this document)
- [ ] U-001 through U-021 implemented
- [ ] All P0 gaps closed
- [ ] All P1 gaps closed
- [ ] Reservation EF/DB drift resolved (3 columns added)
- [ ] GuestProfile DbSet registered and mapped
- [ ] GuestProfile FK on Reservation and Stay
- [ ] HotelPayment tables created and mapped
- [ ] Invoice schema verified and activated
- [ ] AR wired to hotel sources
- [ ] BusinessDate table created
- [ ] Housekeeping, Minibar, KeyCard, LostFound tables created
- [ ] All EF repositories implemented
- [ ] All runtime mock generators removed
- [ ] Migration baseline created
- [ ] All migrations validated
- [ ] Pilot data preserved (10 reservations, 5 stays, 12 rooms)
- [ ] Financial invariants enforced
- [ ] Concurrency (RowVersion) on all mutable aggregates
- [ ] Idempotency keys on financial operations
- [ ] FK integrity verified
- [ ] Property scope enforced
- [ ] Platform audit integrated
- [ ] Integration tests passing
- [ ] E2E tests passing

---

## 52. Final Approval

| Gate | Status |
|---|---|
| **ARCHITECTURE** | **LOCKED** |
| **DATABASE** | **READY** |
| **FINANCIAL** | **READY** |
| **SECURITY** | **READY** |
| **MIGRATION** | **READY** |
| **CODEX** | **READY** |

### All 7 architectural decisions are LOCKED.

| ID | Decision | Status |
|---|---|---|
| D-01 | Property authority — `dbo.Companies`/`dbo.Branches` | **LOCKED** |
| D-02 | Guest identity — `fo.HoSoKhach` via `GuestProfile` domain entity | **LOCKED** |
| D-04 | Business date — `fo.BusinessDate` property-scoped | **LOCKED** |
| D-06 | Payment — `HotelPayment` → `fo.ThanhToan` (new) | **LOCKED** |
| D-07 | Invoice — `dbo.Invoices` (reuse, verify) | **LOCKED** |
| D-08 | AR — Hybrid: `dbo.ReceivableTransactions` + `AgingSnapshot` | **LOCKED** |
| D-10 | Authorization — Existing `IAuthorizationService` + capability model | **LOCKED** |

### 21 of 22 implementation units are READY. U-022 (ReadModels) blocked until canonical sources exist.

---

## PHASE 2B.6 FINAL CODEX SPEC IS READY.

**Codex may begin implementation of U-001 through U-021 in dependency order.**

*End of Phase 2B.6 Final Codex Specification. No code, schema, migration, or configuration change has been made by this document.*