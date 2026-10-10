# HOTEL MANAGEMENT — PHASE 2B MIGRATION BLUEPRINT

**Date:** 2026-08-10  
**Status:** Architecture & Gap Analysis — NO IMPLEMENTATION  
**Predecessors:** Live DB Map (Phase 1), Data Contract (Phase 2A), Production Readiness Audit

---

## 1. Executive Summary

ASMILE-BO is transitioning from a pilot/mixed-state Hotel Management system to a production-ready commercial platform. The live database (`ASMILE_BO`) contains 305 user tables with partial pilot data (10 reservations, 5 stays, 12 rooms, 3 room types). Most business-critical domains — Guest, Group, Folio, Payment, Invoice, AR, Housekeeping, Minibar, Key Card, Lost & Found — have schema present but zero live records. The current codebase has strong application/infrastructure foundations (CQRS, repositories, outbox/inbox, rowversion concurrency) but many UI modules are backed by mock data generators.

**This blueprint identifies 127 gaps** across 76 canonical domains, classifies them by severity (18 P0, 31 P1, 42 P2, 36 P3), and provides a dependency-ordered migration plan spanning 11 implementation phases. No code, schema, migration, or configuration change is made by this document.

---

## 2. Source Documents

| Document | Role | Status |
|---|---|---|
| `HOTEL_MANAGEMENT_LIVE_DB_MAP.md` | Verified live SQL Server schema, row counts, FK, PK, drift | Complete |
| `HOTEL_MANAGEMENT_DATA_CONTRACT.md` | Canonical entity contract, source-of-truth map, decisions D-01 to D-10 | Complete |
| `docs/HOTEL_MANAGEMENT_PRODUCTION_READINESS_AUDIT.md` | Repository, CQRS, mock, workflow, permission readiness | Complete |
| `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs` | EF Core mapping and relationship configuration | Reviewed |
| FO domain/application/infrastructure sources | Reservation, Stay, Folio, Payment, Settlement, Group, Housekeeping, LostFound | Reviewed |

---

## 3. Current Live Database State

### 3.1 Schema Summary

| Schema | User tables | Population |
|---|---|---|
| `dbo` | 245 | Mixed — platform, finance, restaurant, banquet, audit |
| `fo` | 51 | 6 populated (pilot), 45 empty |
| `fo_process` | 9 | Schema present, empty |

### 3.2 Populated FO Tables

| Table | Rows | Domain |
|---|---|---|
| `fo.DatPhong` | 10 | Reservation |
| `fo.LichSuDatPhong` | 20 | Reservation history |
| `fo.LuuTruKhachSan` | 5 | Stay |
| `fo.LichSuLuuTruKhachSan` | 9 | Stay history |
| `fo.PhongKhachSan` | 12 | Room |
| `fo.LoaiPhongKhachSan` | 3 | Room type |

### 3.3 Empty but Critical FO Tables

`fo.HoSoKhach`, `fo.GiayToKhach`, `fo.DatPhongDoan`, `fo.PhanPhongDoan`, `fo.SoFolio`, `fo.ButToanFolio`, `fo.DoThatLac`, `fo.KiemToan`, rate-plan, VIP/loyalty, service-request, guest-message tables — all **0 rows**.

### 3.4 Confirmed Live Drift

| Entity | Column | EF Mapping | Live DB | Severity |
|---|---|---|---|---|
| Reservation (`fo.DatPhong`) | `GuaranteeType` | Mapped | **Absent** | P0 |
| Reservation (`fo.DatPhong`) | `DepositStatus` | Mapped | **Absent** | P0 |
| Reservation (`fo.DatPhong`) | `DepositAmount` | Mapped | **Absent** | P0 |

### 3.5 Missing Database FKs (Verified)

- `fo.DatPhong.GuestId` → `fo.HoSoKhach` — **not present**
- `fo.LuuTruKhachSan.GuestId` → `fo.HoSoKhach` — **not present**
- Reservation rate/source/assigned-room → master tables — **not present**
- Payment → Folio — **not present**
- Invoice → Folio/AR — **not present**
- Group → Guest/Stay/Room — **not present**

### 3.6 Migration History

- 99 applied migration records in `__EFMigrationsHistory`
- Migration source classes absent from checkout
- Runtime migration application **disabled**
- Full source-to-live reconciliation impossible from current checkout

---

## 4. Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     HOTEL MANAGEMENT PLATFORM                    │
├─────────────────────────────────────────────────────────────────┤
│  Foundation    │ Property │ BusinessDate │ User │ Audit │ Outbox │
├─────────────────────────────────────────────────────────────────┤
│  Master Data   │ Guest │ Company │ Room │ RoomType │ Rate │ RateCode │
├─────────────────────────────────────────────────────────────────┤
│  Operational   │ Reservation │ Stay │ Group │ Folio │ Posting    │
├─────────────────────────────────────────────────────────────────┤
│  Financial     │ Payment │ Deposit │ Refund │ Invoice │ AR       │
├─────────────────────────────────────────────────────────────────┤
│  Operations    │ Housekeeping │ Minibar │ LostFound │ KeyCard    │
├─────────────────────────────────────────────────────────────────┤
│  Read/Inquiry  │ Guest Inquiry │ Reservation Search │ Reports    │
└─────────────────────────────────────────────────────────────────┘
```

**Architectural rules:**
1. One canonical write source per business concept
2. Financial facts are append-only or reversible
3. Property scope is mandatory on every operational record
4. Business date is distinct from UTC timestamp
5. Cross-aggregate effects use outbox events, not shared transactions
6. Read models combine sources but never become write sources

---

## 5. Canonical Source of Truth

| Domain | Canonical Source | Current State | Target State |
|---|---|---|---|
| Property | TBD — needs dedicated master | `dbo.Companies`/`dbo.Branches` (1 row each) | Dedicated property master with branch mapping |
| Business Date | TBD — needs aggregate | Only in audit/process tables | Property-scoped business date authority |
| Guest | `fo.HoSoKhach` | Table exists, **0 rows** | Canonical guest profile with FK from Reservation/Stay |
| Guest Document | `fo.GiayToKhach` | Table exists, **0 rows** | PII-controlled document store |
| Guest Preference | Missing | No persistence | New table after Phase 2B approval |
| Company | `dbo.Companies` | 1 row | Multi-role company master (debtor, payer, TA, OTA) |
| Reservation | `fo.DatPhong` | 10 pilot rows | Canonical booking aggregate |
| Room | `fo.PhongKhachSan` | 12 pilot rows | Canonical room inventory |
| Room Type | `fo.LoaiPhongKhachSan` | 3 pilot rows | Canonical room classification |
| Rate Plan | `fo.BangGiaPhong` | Empty | Controlled pricing master |
| Stay | `fo.LuuTruKhachSan` | 5 pilot rows | Canonical in-house stay |
| Group | `fo.DatPhongDoan` | Empty | Canonical group aggregate |
| Folio | `fo.SoFolio` | Empty | Canonical guest account |
| Folio Entry | `fo.ButToanFolio` | Empty | Append-only financial entry |
| Payment | Proposed FO aggregate | No live canonical linkage | Immutable payment fact |
| Deposit | Proposed FO sub-ledger | Empty candidate | Deposit sub-ledger |
| Refund | Proposed FO sub-ledger | Empty candidate | Refund sub-ledger |
| Invoice | `dbo.Invoices` candidate | Empty | Legal invoice aggregate |
| AR | Derived/sub-ledger hybrid | Generic tables empty | Append-only AR sub-ledger + aging projection |
| Housekeeping | Missing | No persistence | Task + inspection aggregate |
| Minibar | Missing | No persistence | Session + consumption aggregate |
| Lost & Found | `fo.DoThatLac` | Empty | Property custody aggregate |
| Key Card | Missing | No FO table | Credential + event aggregate |
| Audit | Platform `dbo.AuditLogs` | 12,842 rows populated | Platform audit + domain events |
| FO Audit | `fo.KiemToan` | Empty | FO-specific business audit |

---

## 6. Complete Gap Register

### P0 Gaps (18) — Blocking production

| ID | Domain | Current | Target | Change | Severity | Dependency | Risk |
|---|---|---|---|---|---|---|---|
| G-001 | Reservation | EF maps `GuaranteeType` absent in live DB | Schema aligned | Resolve drift (add column or remove property) | P0 | None | Runtime write failure |
| G-002 | Reservation | EF maps `DepositStatus` absent in live DB | Schema aligned | Resolve drift | P0 | None | Runtime write failure |
| G-003 | Reservation | EF maps `DepositAmount` absent in live DB | Schema aligned | Resolve drift | P0 | None | Runtime write failure |
| G-004 | Guest | `fo.HoSoKhach` empty; no reservation FK | Populated canonical guest + FK | Add FK + data creation flow | P0 | D-02 | Orphan stays, no guest identity |
| G-005 | Guest | No `GuestProfile` DbSet confirmed in current EF | Entity mapped in DbContext | Add DbSet + configuration | P0 | D-02 | Code cannot persist guests |
| G-006 | Stay | No verified guest FK from Stay | Direct guest FK + snapshot | Add FK + data backfill | P0 | G-004, D-03 | Unattributable stays |
| G-007 | Property | No verified canonical property master | Property master with scope rules | New table or formalize existing | P0 | D-01 | Cross-property data leakage |
| G-008 | Business Date | Not core-wide; only in audit/process | Property-scoped business date authority | New aggregate + table | P0 | D-04 | Incorrect night audit/revenue |
| G-009 | Payment | No live hotel payment-to-folio linkage | Immutable payment + allocation FK | New/modified tables + FKs | P0 | D-06 | Unreconciled payments |
| G-010 | Invoice | Generic `dbo.Invoices` not approved legal source | Legal invoice contract | Decision + schema alignment | P0 | D-07 | Invalid legal invoices |
| G-011 | AR | Mock/random UI signals; unclear derivation | Append-only AR sub-ledger + aging projection | New tables + remove mock | P0 | D-08 | False AR balances |
| G-012 | Guest | `dbo.Customers` empty; cannot be assumed equivalent | Explicit guest vs customer boundary | Contract decision | P0 | D-02 | Wrong identity source |
| G-013 | Reservation | Missing FKs: rate/source/assigned-room → masters | Verified FKs | Add FKs after data assessment | P0 | Room, Rate | Orphan references |
| G-014 | Financial | No folio→payment→invoice→AR linkage proven | Verified financial chain | FKs + workflow | P0 | G-009, G-010, G-011 | Financial integrity |
| G-015 | Mock/UI | Mock generators in runtime DI path | Mock isolated to design-time | Remove from runtime DI | P0 | None | False production data |
| G-016 | Migration | No migration source in checkout; 99 applied | Controlled delta migration capability | Baseline + delta scripts | P0 | None | Cannot reconcile schema |
| G-017 | Concurrency | Rowversion absent on folio entries, histories | Uniform rowversion on mutable aggregates | Add rowversion columns | P0 | None | Lost updates on financial data |
| G-018 | FK | Multiple cross-aggregate IDs lack database FKs | Verified FK constraints | Add FKs after orphan assessment | P0 | G-004, G-006 | Orphaned child records |

### P1 Gaps (31) — Blocking domain productionization

| ID | Domain | Current | Target | Change | Dependency |
|---|---|---|---|---|---|
| G-019 | Folio | Tables empty; settlement linkage unproven | Real posting/settlement | Data population + workflow | G-009 |
| G-020 | Deposit | No proven applied/refunded chain | Deposit sub-ledger | New table + FKs | G-009 |
| G-021 | Refund | Origin/authorization unproven | Controlled refund flow | New table + FKs | G-020 |
| G-022 | Group | Tables empty; incomplete linkage | Governed group rooming | Data population + FKs | G-004 |
| G-023 | Housekeeping | No dedicated model; UI mocks | Task/inspection aggregate | New tables + repository | Room |
| G-024 | Minibar | No dedicated model; UI mocks | Session/consumption aggregate | New tables + repository | Folio |
| G-025 | KeyCard | No FO table proved | Credential/event model | New tables | Stay |
| G-026 | LostFound | Table empty; UI mocks | Custody workflow | Data population | None |
| G-027 | Permission | Matrix/source incomplete | Capability + property auth | Authorization matrix | D-10 |
| G-028 | Audit | Domain/platform roles not defined | Shared audit + business events | Audit integration | None |
| G-029 | Read Models | Mock and live views mixed | Versioned source-labeled models | Read model contracts | Multiple |
| G-030 | Guest Preference | Missing persistence | Guest preference capability | New table | G-004 |
| G-031 | Guest Privacy | No PII governance | Consent, retention, redaction | Policy + schema | G-004 |
| G-032 | Guest Merge | No merge strategy | Merge with history preservation | Workflow + schema | G-004 |
| G-033 | Company Role | Single company table; no role separation | Multi-role company model | Schema extension | D-02 |
| G-034 | Rate Plan | Empty tables | Controlled pricing master | Data population | Room Type |
| G-035 | Rate Rule | Empty tables | Rule-driven pricing | Data population | Rate Plan |
| G-036 | Room Status | Overloaded status field | Separate status dictionaries | Schema + dictionary | Room |
| G-037 | Room Availability | No dedicated availability store | Availability projection | Read model or derived | Reservation, Stay |
| G-038 | Room Block | No verified block model | Room block aggregate | New table | Reservation, Group |
| G-039 | Group Rooming | Assignment linkage incomplete | Room/guest/stay FKs | Add FKs + data | G-022 |
| G-040 | Group Folio | No group billing model | Group folio capability | Design + schema | G-022, Folio |
| G-041 | Folio Split/Merge | No persistence for split/merge events | Split/merge event records | Schema + workflow | Folio |
| G-042 | Folio Transfer | No persistence for transfer events | Transfer event records | Schema + workflow | Folio |
| G-043 | Posting Idempotency | No idempotency on folio entries | Idempotency key | Add column + constraint | Folio Entry |
| G-044 | Settlement | Partial handler evidence | Complete settlement workflow | Workflow completion | Folio, Payment |
| G-045 | Invoice Line | Tables empty; legal constraints unverified | Legal invoice line model | Decision + schema | G-010 |
| G-046 | AR Transaction | Generic tables empty | Hotel AR sub-ledger | New/modified tables | G-011 |
| G-047 | AR Aging | No aging store | Aging projection | Read model | G-046 |
| G-048 | Guest Snapshot | Snapshot strategy inconsistent | Governed snapshot policy | Contract + schema | G-004 |
| G-049 | Workflow Process | `fo_process` tables empty | Long-running workflow state | Data population | Multiple |

### P2 Gaps (42) — Missing capability, can operate without

| ID | Domain | Gap | Target |
|---|---|---|---|
| G-050 | Guest | Document verification workflow | Document status tracking |
| G-051 | Guest | Loyalty program integration | Loyalty point store |
| G-052 | Guest | Duplicate detection | Fuzzy matching + merge |
| G-053 | Company | Credit limit enforcement | Credit policy + checks |
| G-054 | Company | Tax code validation | Tax code verification |
| G-055 | Reservation | Waitlist capability | Waitlist queue |
| G-056 | Reservation | Overbooking controls | Overbooking policy |
| G-057 | Reservation | Share-with / companion | Companion linkage |
| G-058 | Room | Maintenance schedule | Maintenance tracking |
| G-059 | Room | Out-of-order management | OOO workflow |
| G-060 | Rate | Seasons/calendar | Seasonal rate engine |
| G-061 | Rate | Derived rate rules | Rate derivation |
| G-062 | Rate | Currency conversion | Multi-currency rate store |
| G-063 | Stay | Early check-in / late check-out | Policy + fee engine |
| G-064 | Stay | No-show handling | No-show workflow |
| G-065 | Stay | Day-use | Day-use stay type |
| G-066 | Group | Cut-off date management | Cut-off workflow |
| G-067 | Group | Pickup reporting | Pickup read model |
| G-068 | Group | Block release | Release workflow |
| G-069 | Folio | Multi-currency folio | Currency conversion |
| G-070 | Folio | Routing rules | Routing configuration |
| G-071 | Payment | Multi-tender payment | Split tender support |
| G-072 | Payment | Foreign currency payment | FX rate capture |
| G-073 | Payment | Payment gateway integration | Gateway abstraction |
| G-074 | Deposit | Scheduled deposit | Deposit schedule |
| G-075 | Refund | Original payment method enforcement | Refund-to-origin rule |
| G-076 | Invoice | E-invoice provider integration | Provider adapter |
| G-077 | Invoice | Tax breakdown | Tax line storage |
| G-078 | AR | Collection workflow | Collection task management |
| G-079 | AR | Statement generation | Statement template |
| G-080 | AR | Dunning letters | Dunning workflow |
| G-081 | Housekeeping | Assignment optimization | Assignment algorithm |
| G-082 | Housekeeping | Inspection checklist | Checklist template |
| G-083 | Housekeeping | SLA tracking | SLA metrics |
| G-084 | Minibar | Inventory integration | Stock sync |
| G-085 | Minibar | Auto-posting rules | Auto-post configuration |
| G-086 | LostFound | Photo attachment | Binary storage |
| G-087 | LostFound | Claimant verification | Verification workflow |
| G-088 | KeyCard | Door lock vendor integration | Vendor adapter |
| G-089 | KeyCard | Encoder integration | Encoder abstraction |
| G-090 | Audit | FO-specific audit population | FO audit activation |
| G-091 | BusinessDate | Night audit workflow | Night audit aggregate |

### P3 Gaps (36) — Improvements, not blocking

| ID | Domain | Gap |
|---|---|---|
| G-092 | General | Reporting/export templates |
| G-093 | General | Server-side paging for all queries |
| G-094 | General | Structured error handling (replace MessageBox) |
| G-095 | General | Correlation ID propagation |
| G-096 | General | Observability dashboards |
| G-097 | Guest | Profile photo storage |
| G-098 | Guest | Communication preferences |
| G-099 | Room | Room feature/amenity catalog |
| G-100 | Rate | Package rate builder |
| G-101 | Stay | Wake-up call integration |
| G-102 | Stay | DND status tracking |
| G-103 | Group | Group billing allocation |
| G-104 | Folio | Folio archiving |
| G-105 | Payment | Receipt template |
| G-106 | Invoice | PDF generation |
| G-107 | AR | Collector assignment |
| G-108 | Housekeeping | Mobile-friendly task list |
| G-109 | Minibar | Mobile check interface |
| G-110 | LostFound | Public claim portal |
| G-111 | KeyCard | Mobile key support |
| G-112-127 | Various | UI polish, animation, responsive layout, accessibility |

---

## 7. P0 Gap Detail

### G-001/002/003: Reservation EF/Live Schema Drift

**Problem:** `AppDbContext` maps `GuaranteeType`, `DepositStatus`, `DepositAmount` on `Reservation` entity. Live `fo.DatPhong` table does not have these columns. Any EF write that materializes these properties will fail at runtime.

**Resolution options:**
- **Option A (Recommended):** Add the three columns to `fo.DatPhong` via a controlled delta migration, after verifying the Data Contract requires them.
- **Option B:** Remove the three properties from the EF entity and mapping if Data Contract deems them unnecessary.
- **Option C:** Mark properties as `[NotMapped]` temporarily, add columns later.

**Pre-migration checks:**
- Verify no existing code reads these properties from live rows
- Verify the 10 pilot reservations can accept NULL for these columns
- Confirm Data Contract decision on guarantee/deposit ownership

**Risk:** High — runtime write path broken for reservation mutations.

### G-004/005: Guest Profile Persistence

**Problem:** `fo.HoSoKhach` exists but has 0 rows. No `DbSet<GuestProfile>` confirmed in current `AppDbContext` scan. `fo.DatPhong` has no FK to guest profile. Current Guest Inquiry UI generates 15,000 fake guests via `BuildFakeData()`.

**Resolution:**
1. Confirm/add `DbSet<GuestProfile>` in `AppDbContext` mapping to `fo.HoSoKhach`
2. Define guest creation flow (at reservation time, at check-in, or standalone)
3. Add FK from `fo.DatPhong.GuestId` → `fo.HoSoKhach.GuestProfileId` (after orphan assessment)
4. Add FK from `fo.LuuTruKhachSan.GuestId` → `fo.HoSoKhach.GuestProfileId`
5. Implement `IGuestProfileRepository` with EF implementation
6. Remove mock guest generator from runtime DI

**Risk:** High — no guest identity means no CRM, no loyalty, no blacklist enforcement, no history.

### G-009: Payment/Folio Linkage

**Problem:** No verified live hotel payment-to-folio settlement chain. `dbo.PaymentAllocations` and `dbo.ReceivableTransactions` exist but have 0 rows. `dbo.CashTransactions` has data but no FK to hotel folio.

**Resolution:**
1. Decide: reuse generic tables with adapter, or create dedicated hotel payment tables
2. Create Payment aggregate with: PaymentNo, Method, Currency, Amount, Tender, ExternalRef, Payer, PropertyId, BusinessDate, Status, RowVersion
3. Create PaymentAllocation with FK to Payment and Folio
4. Implement immutable allocation history (no destructive edits)
5. Wire payment commands to outbox for AR/Invoice projection

**Risk:** Critical — financial core of hotel operations.

---

## 8. Database Change Strategy by Domain

| Domain | Strategy | Tables Affected |
|---|---|---|
| Reservation | Resolve drift (add 3 columns) | `fo.DatPhong` |
| Guest | Activate existing schema + add FKs | `fo.HoSoKhach`, `fo.GiayToKhach`, `fo.DatPhong`, `fo.LuuTruKhachSan` |
| Company | Extend for multi-role | `dbo.Companies` (or new FO table) |
| Property | Create master | New `fo.Property` (or formalize existing) |
| Business Date | Create aggregate | New `fo.BusinessDate` + related |
| Room | Separate status dictionaries | `fo.PhongKhachSan` (add status columns) |
| Stay | Add guest FK | `fo.LuuTruKhachSan` |
| Group | Activate + add FKs | `fo.DatPhongDoan`, `fo.PhanPhongDoan` |
| Folio | Activate existing + add idempotency | `fo.SoFolio`, `fo.ButToanFolio` |
| Payment | New hotel payment aggregate | New `fo.ThanhToan`, `fo.PhanBoThanhToan` |
| Deposit | New sub-ledger | New `fo.DatCoc` |
| Refund | New sub-ledger | New `fo.HoanTien` |
| Invoice | Decide ownership, then create/adapt | `dbo.Invoices` or new `fo.Invoices` |
| AR | New sub-ledger + aging projection | New `fo.ARTransaction`, `fo.ARBalance` |
| Housekeeping | New task/inspection aggregate | New `fo.HousekeepingTask`, `fo.RoomInspection` |
| Minibar | New session/consumption | New `fo.MinibarSession`, `fo.MinibarConsumption` |
| LostFound | Activate existing | `fo.DoThatLac`, `fo.LichSuThatLac` |
| KeyCard | New credential/event | New `fo.KeyCardCredential`, `fo.KeyCardEvent` |
| Audit | Activate FO audit + integrate platform | `fo.KiemToan` + platform audit tables |

---

## 9. Migration Matrix

| Domain | Table | Current State | Target | Action | Data Backfill | FK | Index | EF Impact |
|---|---|---|---|---|---|---|---|---|
| Reservation | `fo.DatPhong` | 10 rows, 3 missing cols | Complete schema | ADD COLUMN ×3 | NULL for pilot rows | Guest FK | GuestId, PropertyId, Status, Arrival, Departure | Entity + config |
| Guest | `fo.HoSoKhach` | 0 rows | Active | None (schema exists) | Create from reservation snapshots | — | GuestNo, Phone, Email | Add DbSet |
| Guest Doc | `fo.GiayToKhach` | 0 rows | Active | None | From check-in | Guest FK | GuestId, DocType | Add DbSet |
| Stay | `fo.LuuTruKhachSan` | 5 rows | + Guest FK | ADD FK | Link to existing guest | Guest FK | GuestId | Config change |
| Room | `fo.PhongKhachSan` | 12 rows | + Status separation | ADD COLUMN for HK status | NULL for pilot | — | RoomNo, PropertyId, Status | Entity + config |
| Group | `fo.DatPhongDoan` | 0 rows | Active | None | None | Guest/Stay FKs | GroupCode | Confirm DbSet |
| Folio | `fo.SoFolio` | 0 rows | + IdempotencyKey | ADD COLUMN | None | — | FolioNo, StayId, Status | Entity + config |
| Folio Entry | `fo.ButToanFolio` | 0 rows | + RowVersion, IdempotencyKey | ADD COLUMN ×2 | None | — | FolioId, BusinessDate | Entity + config |
| Payment | New `fo.ThanhToan` | No table | New table | CREATE TABLE | None | Folio/Stay FK | PaymentNo, PropertyId, Status, Date | New entity |
| Deposit | New `fo.DatCoc` | No table | New table | CREATE TABLE | None | Reservation/Stay FK | DepositNo, Status | New entity |
| Refund | New `fo.HoanTien` | No table | New table | CREATE TABLE | None | Payment/Deposit FK | RefundNo, Status | New entity |
| Invoice | `dbo.Invoices` or new | 0 rows | Decision | Decision | None | Folio/AR FK | InvoiceNo, IssueDate | Decision |
| AR | New `fo.ARTransaction` | No table | New table | CREATE TABLE | None | Invoice/Payment FK | TransactionNo, Date | New entity |
| Housekeeping | New `fo.HousekeepingTask` | No table | New table | CREATE TABLE | None | Room FK | TaskNo, RoomId, Date, Status | New entity |
| Minibar | New `fo.MinibarSession` | No table | New table | CREATE TABLE | None | Stay/Room FK | SessionNo, Date | New entity |
| LostFound | `fo.DoThatLac` | 0 rows | Active | None | None | — | ItemNo, Status | Confirm DbSet |
| KeyCard | New `fo.KeyCardCredential` | No table | New table | CREATE TABLE | None | Stay/Guest FK | CardSerial, Status | New entity |
| FO Audit | `fo.KiemToan` | 0 rows | Active | None | None | — | CorrelationId, EntityId, Date | Confirm DbSet |
| BusinessDate | New `fo.BusinessDate` | No table | New table | CREATE TABLE | Seed one row | Property FK | PropertyId, Date | New entity |

---

## 10. FK Plan

### Required New Foreign Keys

| Child | Parent | Constraint | Priority |
|---|---|---|---|
| `fo.DatPhong.GuestId` | `fo.HoSoKhach.GuestProfileId` | `NO ACTION` | P0 |
| `fo.LuuTruKhachSan.GuestId` | `fo.HoSoKhach.GuestProfileId` | `NO ACTION` | P0 |
| `fo.DatPhong.RoomTypeId` | `fo.LoaiPhongKhachSan.HotelRoomTypeId` | `NO ACTION` | P0 |
| `fo.DatPhong.AssignedRoomId` | `fo.PhongKhachSan.HotelRoomId` | `NO ACTION` | P0 |
| `fo.ThanhToan.FolioId` | `fo.SoFolio.HotelFolioId` | `NO ACTION` | P0 |
| `fo.PhanBoThanhToan.PaymentId` | `fo.ThanhToan.PaymentId` | `NO ACTION` | P0 |
| `fo.PhanBoThanhToan.FolioId` | `fo.SoFolio.HotelFolioId` | `NO ACTION` | P0 |
| `fo.HoanTien.PaymentId` | `fo.ThanhToan.PaymentId` | `NO ACTION` | P1 |
| `fo.HoanTien.DepositId` | `fo.DatCoc.DepositId` | `NO ACTION` | P1 |
| `fo.Invoices.FolioId` | `fo.SoFolio.HotelFolioId` | `NO ACTION` | P1 |
| `fo.ARTransaction.InvoiceId` | `fo.Invoices.InvoiceId` | `NO ACTION` | P1 |
| `fo.ARTransaction.PaymentId` | `fo.ThanhToan.PaymentId` | `NO ACTION` | P1 |
| `fo.HousekeepingTask.RoomId` | `fo.PhongKhachSan.HotelRoomId` | `NO ACTION` | P1 |
| `fo.MinibarSession.StayId` | `fo.LuuTruKhachSan.HotelStayId` | `NO ACTION` | P1 |
| `fo.MinibarSession.RoomId` | `fo.PhongKhachSan.HotelRoomId` | `NO ACTION` | P1 |
| `fo.KeyCardCredential.StayId` | `fo.LuuTruKhachSan.HotelStayId` | `NO ACTION` | P1 |
| `fo.KeyCardCredential.GuestId` | `fo.HoSoKhach.GuestProfileId` | `NO ACTION` | P1 |
| `fo.PhanPhongDoan.GuestId` | `fo.HoSoKhach.GuestProfileId` | `NO ACTION` | P1 |
| `fo.PhanPhongDoan.RoomId` | `fo.PhongKhachSan.HotelRoomId` | `NO ACTION` | P1 |
| `fo.PhanPhongDoan.StayId` | `fo.LuuTruKhachSan.HotelStayId` | `NO ACTION` | P1 |

---

## 11. Index Strategy

Proposed indexes (not exhaustive):

| Table | Index | Type | Reason |
|---|---|---|---|
| `fo.HoSoKhach` | `GuestNo` | Unique | Business key lookup |
| `fo.HoSoKhach` | `Phone`, `Email` | Non-unique | Guest search |
| `fo.DatPhong` | `PropertyId, Status, Arrival` | Composite | Arrival list, occupancy |
| `fo.DatPhong` | `ReservationNo` | Unique | Business key |
| `fo.DatPhong` | `GuestId` | Non-unique | Guest inquiry |
| `fo.LuuTruKhachSan` | `PropertyId, Status` | Composite | In-house list |
| `fo.LuuTruKhachSan` | `GuestId, Status` | Composite | Guest stay history |
| `fo.SoFolio` | `StayId` | Non-unique | Folio lookup |
| `fo.SoFolio` | `FolioNo` | Unique | Business key |
| `fo.ButToanFolio` | `FolioId, BusinessDate` | Composite | Posting query |
| `fo.ThanhToan` | `PaymentNo` | Unique | Business key |
| `fo.ThanhToan` | `PropertyId, Status, Date` | Composite | Payment list |
| `fo.ARTransaction` | `PropertyId, Date` | Composite | AR aging |
| `fo.ARTransaction` | `InvoiceId` | Non-unique | AR reconciliation |
| `fo.HousekeepingTask` | `RoomId, Date, Status` | Composite | Task assignment |
| `fo.DoThatLac` | `PropertyId, Status` | Composite | Lost & Found list |

---

## 12. Data Backfill Strategy

### 12.1 Pilot Reservation Data (10 rows)

**Assessment:** The 10 pilot reservations have guest names as snapshots but no `GuestId` FK. They reference room types and assigned rooms that may exist in the 12-room pilot set.

**Backfill plan:**
1. Create guest profiles from reservation guest snapshots (if names are unique enough)
2. Set `GuestId` on existing reservations where a guest match is found
3. For unmatched reservations, leave `GuestId` NULL (allow nullable until full guest creation)
4. Add `GuaranteeType`, `DepositStatus`, `DepositAmount` as NULL for pilot rows
5. Verify FK integrity after backfill

### 12.2 Pilot Stay Data (5 rows)

**Assessment:** 5 stays linked to reservations via existing FK. Guest linkage is via snapshot only.

**Backfill plan:**
1. After guest backfill, set `GuestId` on stays matching the reservation guest
2. Verify room FK is valid for all 5 stays

### 12.3 Pilot Room Data (12 rows, 3 types)

**Assessment:** Small pilot set. Room-to-type FK is verified.

**Backfill plan:** None needed — schema is complete for pilot scope.

### 12.4 Empty Domains

All other domains (Guest, Group, Folio, Payment, Invoice, AR, Housekeeping, Minibar, KeyCard, LostFound) have **0 rows**. No backfill is needed. Data will be created through normal business operations after schema is activated.

---

## 13. Pilot Data Reconciliation

| Check | Method | Expected |
|---|---|---|
| Reservation count | `SELECT COUNT(*) FROM fo.DatPhong` | 10 after migration |
| Stay count | `SELECT COUNT(*) FROM fo.LuuTruKhachSan` | 5 after migration |
| Room count | `SELECT COUNT(*) FROM fo.PhongKhachSan` | 12 after migration |
| Guest count | `SELECT COUNT(*) FROM fo.HoSoKhach` | ≥0 after backfill |
| Orphan reservations | `SELECT * FROM fo.DatPhong WHERE GuestId IS NULL` | Acceptable if <10 |
| Orphan stays | `SELECT * FROM fo.LuuTruKhachSan WHERE RoomId NOT IN (SELECT HotelRoomId FROM fo.PhongKhachSan)` | 0 |
| Reservation-Stay link | `SELECT * FROM fo.LuuTruKhachSan WHERE ReservationId NOT IN (SELECT ReservationId FROM fo.DatPhong)` | 0 |
| Invalid dates | Check `Arrival >= Departure` | 0 |
| Invalid amounts | Check negative amounts on financial records | 0 |

---

## 14. EF Core Impact

### 14.1 Entity Changes Required

| Domain | Entity | Change |
|---|---|---|
| Reservation | `Reservation` | Resolve GuaranteeType/DepositStatus/DepositAmount drift |
| Guest | `GuestProfile` | Add/confirm DbSet, configure mapping |
| Guest | `GuestDocument` | Add/confirm DbSet |
| Stay | `HotelStay` | Add `GuestId` FK property |
| Room | `HotelRoom` | Add housekeeping status column |
| Folio | `HotelFolio` | Add `IdempotencyKey` |
| Folio Entry | `HotelFolioEntry` | Add `RowVersion`, `IdempotencyKey` |
| Payment | New `HotelPayment` | New entity |
| Payment Allocation | New `PaymentAllocation` | New entity |
| Deposit | New `Deposit` | New entity |
| Refund | New `Refund` | New entity |
| Invoice | Decision pending | TBD |
| AR | New `ReceivableTransaction` | New entity |
| Housekeeping | New `HousekeepingTask`, `RoomInspection` | New entities |
| Minibar | New `MinibarSession`, `MinibarConsumption` | New entities |
| KeyCard | New `KeyCardCredential`, `KeyCardEvent` | New entities |
| BusinessDate | New `BusinessDate` | New entity |

### 14.2 DbContext Changes

- Add `DbSet<GuestProfile>` → `fo.HoSoKhach`
- Add `DbSet<GuestDocument>` → `fo.GiayToKhach`
- Add `DbSet<HotelPayment>` → `fo.ThanhToan`
- Add `DbSet<PaymentAllocation>` → `fo.PhanBoThanhToan`
- Add `DbSet<Deposit>` → `fo.DatCoc`
- Add `DbSet<Refund>` → `fo.HoanTien`
- Add `DbSet<ReceivableTransaction>` → `fo.ARTransaction`
- Add `DbSet<HousekeepingTask>` → `fo.HousekeepingTask`
- Add `DbSet<RoomInspection>` → `fo.RoomInspection`
- Add `DbSet<MinibarSession>` → `fo.MinibarSession`
- Add `DbSet<MinibarConsumption>` → `fo.MinibarConsumption`
- Add `DbSet<KeyCardCredential>` → `fo.KeyCardCredential`
- Add `DbSet<KeyCardEvent>` → `fo.KeyCardEvent`
- Add `DbSet<BusinessDate>` → `fo.BusinessDate`
- Configure all new FKs, indexes, unique constraints
- Configure rowversion on all new mutable aggregates

### 14.3 Repository Changes

| Repository | Change |
|---|---|
| `IGuestProfileRepository` | Implement EF repository |
| `IHotelStayRepository` | Add `GuestId` to queries |
| `IHotelPaymentRepository` | New — implement |
| `IHotelInvoiceRepository` | Decision pending |
| `IHousekeepingRepository` | New — implement |
| `IMinibarRepository` | New — implement |
| `IKeyCardRepository` | New — implement |
| `ILostFoundRepository` | New — implement |
| `IReceivableRepository` | New — implement |
| `IBusinessDateRepository` | New — implement |

---

## 15. CQRS Impact

| Domain | Commands Needed | Queries Needed |
|---|---|---|
| Guest | CreateGuest, UpdateGuest, MergeGuests, BlockGuest | GetGuest, SearchGuests, GetGuestHistory |
| Company | CreateCompany, UpdateCompany, SetCreditLimit | GetCompany, SearchCompanies |
| Payment | RecordPayment, AllocatePayment, ReversePayment, RefundPayment | GetPayment, GetPaymentsByFolio, GetPaymentHistory |
| Deposit | HoldDeposit, ApplyDeposit, RefundDeposit, ForfeitDeposit | GetDeposit, GetDepositsByReservation |
| Refund | RequestRefund, ApproveRefund, PostRefund | GetRefund, GetRefundsByPayment |
| Invoice | IssueInvoice, CancelInvoice, AdjustInvoice | GetInvoice, GetInvoicesByFolio, GetInvoiceHistory |
| AR | PostARTransaction, ReconcileAR, CloseAR | GetARBalance, GetARAging, GetARTransactions |
| Housekeeping | AssignTask, StartTask, CompleteTask, InspectRoom, PassInspection, FailInspection | GetTasks, GetTasksByRoom, GetTasksByAssignee |
| Minibar | OpenSession, RecordConsumption, CheckSession, PostSession, VoidSession | GetSession, GetSessionsByRoom |
| KeyCard | IssueCard, DisableCard, ReplaceCard, ReturnCard | GetCard, GetCardsByStay, GetCardEvents |
| LostFound | RecordItem, StoreItem, ClaimItem, ReturnItem, DisposeItem | GetItem, SearchItems |
| BusinessDate | OpenDate, CloseDate, ReopenDate | GetCurrentBusinessDate |

---

## 16. UI Integration Impact

| Current UI Module | Required Backend | Current State | Target State |
|---|---|---|---|
| Guest Inquiry (Tra cứu khách) | Guest Query Service | `BuildFakeData()` 15K mock | Real guest repository query |
| AR Ledger (Công nợ) | AR Query Service | Mock data | Real AR sub-ledger query |
| AR Aging (Tuổi nợ) | AR Aging Query | Mock data | Derived aging projection |
| AR Payment (Thanh toán) | Payment Query Service | Mock data | Real payment repository |
| AR Customer (Khách hàng) | Guest/Company Query | Mock data | Real guest/company query |
| AR Company (Doanh nghiệp) | Company Query | Mock data | Real company repository |
| AR Invoice (Hóa đơn) | Invoice Query | Mock data | Real invoice repository |
| AR Reconcile (Đối chiếu) | AR Reconciliation | Mock data | Real reconciliation workflow |
| Housekeeping | Task Query/Command | Mock data | Real task repository |
| Minibar | Session Query/Command | Mock data | Real session repository |
| Lost & Found | Item Query/Command | Mock data | Real item repository |
| Key Management | Credential Query/Command | Partial mock | Real credential repository |
| Workforce | Assignment Query | Mock data | Real task assignment query |
| Group Reservation | Group Query/Command | Partial mock | Real group repository |

---

## 17. Migration Dependency Graph

```
PHASE A: Foundation
├── Property Master (D-01)
├── Business Date Authority (D-04)
├── Audit Integration (G-028)
└── Migration Baseline (G-016)
     │
PHASE B: Master Data
├── Guest Profile (G-004, G-005, G-012)
├── Guest Document (G-031)
├── Guest Preference (G-030)
├── Company Multi-Role (G-033)
├── Room Status Separation (G-036)
└── Rate Plan/Rule (G-034, G-035)
     │
PHASE C: Reservation Remediation
├── Resolve EF/DB Drift (G-001, G-002, G-003)
├── Add Guest FK (G-013)
├── Add Rate/Source/Room FKs (G-013)
└── Backfill Guest References
     │
PHASE D: Stay Remediation
├── Add Guest FK (G-006)
├── Backfill Guest on Pilot Stays
└── Concurrency on Stay History (G-017)
     │
PHASE E: Group
├── Activate Group Tables (G-022)
├── Add Guest/Stay/Room FKs (G-039)
└── Group Folio Design (G-040)
     │
PHASE F: Financial Core
├── Folio Activation + Idempotency (G-019, G-043)
├── Payment Aggregate (G-009, G-014)
├── Payment Allocation (G-014)
├── Deposit Sub-ledger (G-020)
└── Refund Sub-ledger (G-021)
     │
PHASE G: Invoice & AR
├── Invoice Decision + Schema (G-010, G-045)
├── AR Sub-ledger (G-011, G-046)
├── AR Aging Projection (G-047)
└── Financial FK Chain (G-014)
     │
PHASE H: Operations
├── Housekeeping Task/Inspection (G-023)
├── Minibar Session/Consumption (G-024)
├── Lost & Found Activation (G-026)
└── KeyCard Credential/Event (G-025)
     │
PHASE I: Security & Governance
├── Permission Matrix (G-027)
├── Property Scope Enforcement (G-007)
├── Concurrency Completion (G-017)
└── PII Governance (G-031)
     │
PHASE J: Read Models
├── Guest Inquiry Real Query (G-029)
├── AR Inquiry Real Query (G-029)
├── Housekeeping Read Models (G-029)
└── Reporting Read Models (G-029)
     │
PHASE K: Mock Removal
├── Remove runtime mock generators (G-015)
├── Design-time mock provider
└── Feature flags for demo mode
```

---

## 18. Migration Execution Order

### Phase 2B-1: Foundation (Week 1-2)
1. Create migration baseline from live schema
2. Implement Property master (or formalize existing)
3. Implement Business Date aggregate
4. Resolve Reservation EF/DB drift (G-001/002/003)
5. Add `RowVersion` to FolioEntry and StayHistory

### Phase 2B-2: Guest & Company (Week 3-4)
1. Add `DbSet<GuestProfile>` + configuration
2. Add `DbSet<GuestDocument>` + configuration
3. Add Guest FK to Reservation
4. Add Guest FK to Stay
5. Backfill guest profiles from pilot data
6. Extend Company for multi-role

### Phase 2B-3: Reservation & Stay (Week 5-6)
1. Add remaining Reservation FKs (rate, source, room)
2. Add Stay concurrency
3. Separate Room status dictionaries
4. Implement Guest Repository + Query Service

### Phase 2B-4: Group (Week 7-8)
1. Add Guest/Stay/Room FKs to Group
2. Implement Group Repository
3. Group Folio design

### Phase 2B-5: Financial Core (Week 9-12)
1. Activate Folio/FolioEntry with idempotency
2. Create Payment aggregate + allocation
3. Create Deposit sub-ledger
4. Create Refund sub-ledger
5. Implement Payment Repository + CQRS

### Phase 2B-6: Invoice & AR (Week 13-15)
1. Decide invoice ownership
2. Create/adapt Invoice tables
3. Create AR sub-ledger + aging projection
4. Implement Invoice/AR Repositories

### Phase 2B-7: Housekeeping & Minibar (Week 16-18)
1. Create Housekeeping Task/Inspection tables
2. Create Minibar Session/Consumption tables
3. Implement Repositories + CQRS

### Phase 2B-8: KeyCard & LostFound (Week 19-20)
1. Create KeyCard Credential/Event tables
2. Activate LostFound tables
3. Implement Repositories + CQRS

### Phase 2B-9: Security & Audit (Week 21-22)
1. Implement permission matrix
2. Property scope enforcement
3. Activate FO audit
4. PII governance

### Phase 2B-10: Read Models (Week 23-24)
1. Replace mock Guest Inquiry with real query
2. Replace mock AR modules with real queries
3. Replace mock Housekeeping/Minibar/Key UI with real queries

### Phase 2B-11: Mock Removal & Verification (Week 25-26)
1. Remove all runtime mock generators
2. Design-time mock provider
3. End-to-end integration tests
4. UAT on pilot property

---

## 19. Pre-Migration Validation

For each migration, run these read-only checks:

```sql
-- Orphan FK candidates
SELECT * FROM fo.DatPhong WHERE RoomTypeId NOT IN (SELECT HotelRoomTypeId FROM fo.LoaiPhongKhachSan);
SELECT * FROM fo.LuuTruKhachSan WHERE RoomId NOT IN (SELECT HotelRoomId FROM fo.PhongKhachSan);

-- Duplicate business keys
SELECT ReservationNo, COUNT(*) FROM fo.DatPhong GROUP BY ReservationNo HAVING COUNT(*) > 1;

-- NULL required fields
SELECT * FROM fo.DatPhong WHERE Arrival IS NULL OR Departure IS NULL;
SELECT * FROM fo.LuuTruKhachSan WHERE CheckInDate IS NULL;

-- Invalid dates
SELECT * FROM fo.DatPhong WHERE Arrival >= Departure;
SELECT * FROM fo.LuuTruKhachSan WHERE CheckInDate >= COALESCE(CheckOutDate, '2099-01-01');

-- Invalid amounts
SELECT * FROM fo.DatPhong WHERE TotalAmount < 0;

-- Cross-property data
SELECT PropertyId, COUNT(*) FROM fo.DatPhong GROUP BY PropertyId;
```

---

## 20. Post-Migration Validation

After each migration phase:

| Check | Method |
|---|---|
| Row counts preserved | Compare pre/post counts |
| FK integrity | `DBCC CHECKCONSTRAINTS` |
| Indexes created | `sys.indexes` verification |
| Unique constraints | Attempt duplicate insert (test only) |
| Nullability correct | `INFORMATION_SCHEMA.COLUMNS` verification |
| Data types correct | `INFORMATION_SCHEMA.COLUMNS` verification |
| Reservation count | `SELECT COUNT(*) FROM fo.DatPhong` = 10 |
| Stay count | `SELECT COUNT(*) FROM fo.LuuTruKhachSan` = 5 |
| Room count | `SELECT COUNT(*) FROM fo.PhongKhachSan` = 12 |

---

## 21. Reconciliation Plan

| Domain | Reconciliation Method |
|---|---|
| Reservation | Count + FK check: reservation → guest, room type, assigned room |
| Stay | Count + FK check: stay → reservation, room, guest |
| Room | Count + FK check: room → room type |
| Guest | Verify all reservation guests have profiles |
| Folio | Sum(FolioEntry.Amount) = Folio.Balance per currency |
| Payment | Sum(Allocation.Amount) ≤ Payment.Amount |
| Deposit | Sum(Applied + Refunded + Forfeited) ≤ Original |
| Refund | Every refund references valid origin payment/deposit |
| Invoice | Sum(InvoiceLine.Amount) + Tax = Invoice.Total |
| AR | AR balance = Sum(Invoice) - Sum(Payment) + Sum(Adjustment) |
| Group | GroupRoomAssignment.Count ≤ Group.BlockedRooms |
| Housekeeping | Tasks per room per day ≤ 1 active |
| Minibar | Session posted exactly once per folio |

---

## 22. Backup & Rollback Strategy

### Pre-Migration Gate
1. Full database backup
2. Backup verification (restore to test instance)
3. Restore rehearsal
4. Recovery plan documented
5. Rollback plan per migration

### Per-Migration
1. Transaction-scoped DDL where possible
2. Pre/post row count snapshot
3. FK/constraint verification after each step
4. Failed migration → rollback to backup

### Rollback
- Full restore from verified backup
- Point-in-time recovery if available
- Post-rollback validation (same checks as pre-migration)

---

## 23. Security / Permission Impact

| Capability | Required Permission | Property Scope |
|---|---|---|
| Guest create/edit | `guest.write` | Tenant |
| Guest PII view | `guest.pii.read` | Tenant |
| Reservation write | `reservation.write` | Property |
| Check-in/out | `stay.checkin`, `stay.checkout` | Property |
| Folio post | `folio.post` | Property |
| Payment record | `payment.record` | Property |
| Payment refund | `payment.refund` | Property (supervisor) |
| Invoice issue | `invoice.issue` | Property/Legal entity |
| AR view | `ar.read` | Property |
| AR reconcile | `ar.reconcile` | Property (finance) |
| Housekeeping | `hk.assign`, `hk.inspect` | Property |
| Minibar check | `minibar.check` | Property |
| KeyCard issue | `keycard.issue` | Property |
| LostFound custody | `lostfound.custody` | Property |

---

## 24. Audit Impact

After Phase 2B, every domain operation must produce audit records:

| Domain Event | Audit Table | Required Fields |
|---|---|---|
| Reservation create/change/cancel | Platform audit + FO audit | Actor, property, before/after, correlation ID |
| Check-in/out/room move | Platform audit + FO audit | Actor, stay, room, transition, correlation ID |
| Folio post/reverse | Platform audit | Actor, folio, entry, amount, business date |
| Payment/allocate/refund | Platform audit | Actor, payment, amount, method, correlation ID |
| Invoice issue/cancel/adjust | Platform audit | Actor, invoice, legal snapshot, correlation ID |
| Housekeeping task/inspection | Platform audit | Actor, room, task, status, correlation ID |
| Minibar post | Platform audit | Actor, session, consumption, folio reference |
| LostFound disposition | Platform audit | Actor, item, disposition, correlation ID |
| KeyCard action | Platform audit | Actor, card, stay, action, correlation ID |

---

## 25. Production Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Migration breaks pilot data | Medium | High | Backup + rollback + pre/post validation |
| EF drift causes runtime errors | High | Critical | Resolve P0 drift first |
| Mock data accepted as real | High | High | Runtime mock removal + feature flags |
| Cross-property data leakage | Medium | High | Property scope enforcement |
| Financial reconciliation failure | Medium | Critical | Append-only design + reconciliation tests |
| Performance degradation | Low | Medium | Index strategy + query optimization |
| Missing migration source blocks reconciliation | High | Medium | Baseline from live schema |
| Concurrent writes cause data loss | Medium | High | RowVersion on all mutable aggregates |
| PII exposure | Medium | Critical | PII governance + masking + audit |
| External integration failure | Low | Medium | Idempotency + correlation + retry |

---

## 26. Open Decisions

| ID | Decision | Options | Recommended | Impact |
|---|---|---|---|---|
| D-01 | Property authority | New master / formalize branch / external | Dedicated property master | P0 schema |
| D-02 | Guest identity | `fo.HoSoKhach` / `dbo.Customers` / hybrid | `fo.HoSoKhach` + snapshot | P0 schema + FK |
| D-03 | Stay guest linkage | Direct FK / snapshot only / both | Both FK + snapshot | P0 integrity |
| D-04 | Business date authority | Night audit aggregate / app setting / external | Property-scoped night audit | P0 workflow |
| D-05 | Folio posting authority | FolioEntry sole ledger / separate ledger | FolioEntry sole ledger | P1 financial |
| D-06 | Payment allocation authority | Reuse generic / adapter / new hotel table | New hotel payment table | P0 financial |
| D-07 | Legal invoice authority | Reuse `dbo.Invoices` / new hotel invoice / external | New hotel invoice aggregate | P0 financial |
| D-08 | AR architecture | Derived only / sub-ledger / hybrid | Sub-ledger + aging projection | P0 financial |
| D-09 | Housekeeping/Minibar/Key storage | Separate aggregates / room status only / external | Separate aggregates | P1 schema |
| D-10 | Authorization model | Role-only / capability-only / role+capability+scope | Capability + property scope | P1 security |

---

## 27. Implementation Phases Summary

| Phase | Domain | Duration | New Tables | New FKs | P0 Closed | P1 Closed |
|---|---|---|---|---|---|---|
| 2B-1 | Foundation | 2 weeks | 2 | 2 | 4 | 2 |
| 2B-2 | Guest & Company | 2 weeks | 0 | 3 | 3 | 3 |
| 2B-3 | Reservation & Stay | 2 weeks | 0 | 4 | 2 | 2 |
| 2B-4 | Group | 2 weeks | 0 | 3 | 0 | 3 |
| 2B-5 | Financial Core | 4 weeks | 4 | 6 | 2 | 4 |
| 2B-6 | Invoice & AR | 3 weeks | 2 | 3 | 3 | 3 |
| 2B-7 | Housekeeping & Minibar | 3 weeks | 4 | 3 | 0 | 4 |
| 2B-8 | KeyCard & LostFound | 2 weeks | 2 | 2 | 0 | 3 |
| 2B-9 | Security & Audit | 2 weeks | 0 | 0 | 2 | 3 |
| 2B-10 | Read Models | 2 weeks | 0 | 0 | 1 | 4 |
| 2B-11 | Mock Removal | 2 weeks | 0 | 0 | 1 | 0 |
| **Total** | | **26 weeks** | **14** | **26** | **18** | **31** |

---

## 28. Definition of Done

Hotel Management is production-ready when:

- [ ] All 18 P0 gaps closed
- [ ] All 31 P1 gaps closed
- [ ] Canonical source of truth established for all 76 domains
- [ ] Guest master populated with FK from Reservation and Stay
- [ ] Reservation EF/DB drift resolved
- [ ] Financial chain verified: Folio → Payment → Invoice → AR
- [ ] Property scope enforced on all operational records
- [ ] Business date authority operational
- [ ] All runtime mock generators removed
- [ ] RowVersion concurrency on all mutable aggregates
- [ ] Idempotency keys on all financial and device operations
- [ ] All required FKs in place and verified
- [ ] Permission matrix enforced at application layer
- [ ] PII governance implemented
- [ ] Audit coverage for all material operations
- [ ] Backup and rollback verified
- [ ] Integration tests passing for all critical workflows
- [ ] E2E tests: Check-in → Folio → Payment → Invoice → Check-out → HK
- [ ] UAT completed on pilot property

---

## 29. Final Recommendation

1. **Execute Phase 2B-1 immediately** — resolve the P0 Reservation EF/DB drift and establish the migration baseline. This unblocks all subsequent work.

2. **Approve decisions D-01 through D-10** before starting Phase 2B-2. The guest, payment, invoice, and AR architecture decisions are foundational and cannot be reversed cheaply.

3. **Do NOT connect mock-backed UI modules to real data** until their backend repositories and query services are verified. The mock data generators in GuestInquiry, AR modules, Housekeeping, Minibar, and Workforce must remain isolated until Phase 2B-10.

4. **Run the 26-week plan in dependency order.** Financial core (Phase 2B-5) cannot start before Guest (2B-2) and Reservation/Stay (2B-3) are stable. Housekeeping (2B-7) needs Room (2B-3) and Folio (2B-5).

5. **Establish a change control board** for any deviation from the Data Contract. The contract is the architectural authority; UI demands must not drive schema decisions.

6. **Begin with a full database backup and restore rehearsal.** No migration should execute without a verified rollback path.

---

*End of Phase 2B Migration Blueprint. No code, schema, migration, configuration, or data change has been made by this document.*