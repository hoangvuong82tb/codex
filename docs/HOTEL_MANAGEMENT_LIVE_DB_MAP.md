# HOTEL MANAGEMENT — LIVE DATABASE MAP

## 1. Verification Status

**VERIFIED — LIVE DATABASE AVAILABLE (read-only).** This report was produced from direct metadata and count queries against the runtime SQL Server database used by the application configuration. No migrations, DDL, DML, seed operation, source-code change, or configuration change was executed.

## 2. Database Identity

| Field | Verified value |
|---|---|
| SQL Server | `HOANGVUONGPC\\SQLEXPRESS` |
| Database | `ASMILE_BO` |
| Engine | Microsoft SQL Server Express 16.0.1000.6 (64-bit) |
| Runtime DbContext | `AppDbContext` |
| Authentication | SQL authentication configured by the application; credential intentionally redacted |
| Runtime migration policy | Disabled; automatic application of pending migrations disabled; pending migrations configured to fail |

## 3. Schema Inventory

| Schema | User tables | Purpose observed |
|---|---:|---|
| `dbo` | 245 | Shared platform, company/branch, finance, restaurant/banquet, audit, workflow and operational support |
| `fo` | 51 | Front Office / Hotel Management domain |
| `fo_process` | 9 | Front Office workflow/process framework |

The live database contains **305 user tables**, **7 stored procedures**, and no user views. Hotel Management must therefore be isolated by bounded-context ownership rather than by assuming that every `dbo` table is Front Office data.

## 4. Hotel Management Table Inventory

### Live `fo` tables carrying rows

| Table | Rows | Role indicated by schema |
|---|---:|---|
| `fo.DatPhong` | 10 | Reservation |
| `fo.LichSuDatPhong` | 20 | Reservation history |
| `fo.LuuTruKhachSan` | 5 | Hotel stay / in-house stay |
| `fo.LichSuLuuTruKhachSan` | 9 | Stay history |
| `fo.PhongKhachSan` | 12 | Room master |
| `fo.LoaiPhongKhachSan` | 3 | Room-type master |

### Present but empty Front Office tables of material importance

`fo.HoSoKhach`, `fo.GiayToKhach`, `fo.DatPhongDoan`, `fo.PhanPhongDoan`, `fo.SoFolio`, `fo.ButToanFolio`, `fo.DoThatLac`, `fo.KiemToan`, rate-plan tables, service-request tables, guest-message tables, VIP/loyalty tables and `fo_process` tables all exist but have no verified runtime records at audit time.

## 5. Business Capability Mapping

| Capability | Live tables found | Live readiness |
|---|---|---|
| Reservation | `fo.DatPhong`, `fo.LichSuDatPhong` | Seed / pilot data only |
| Stay / check-in-out | `fo.LuuTruKhachSan`, `fo.LichSuLuuTruKhachSan` | Seed / pilot data only |
| Room master | `fo.PhongKhachSan`, `fo.LoaiPhongKhachSan` | Seed / pilot data only |
| Guest master/documents | `fo.HoSoKhach`, `fo.GiayToKhach` | Schema exists; no live records |
| Group | `fo.DatPhongDoan`, `fo.PhanPhongDoan` | Schema exists; no live records |
| Folio / posting | `fo.SoFolio`, `fo.ButToanFolio` | Schema exists; no live records |
| Payment / allocation | `dbo.PaymentAllocations`, `dbo.ReceivableTransactions` | Tables exist; no live records; not linked to hotel folio in verified FK map |
| Invoice / AR | `dbo.Invoices`, `dbo.InvoiceLines`, `dbo.ReceivableTransactions` | Tables exist; no live records |
| Lost & Found | `fo.DoThatLac`, `fo.LichSuThatLac` | Schema exists; no live records |
| FO audit | `fo.KiemToan` | Schema exists; no live records |
| Platform audit | `dbo.AuditLogs` and related audit tables | Populated; candidate platform audit source |

## 6. Primary Key Map

| Entity | Table | Primary key |
|---|---|---|
| Reservation | `fo.DatPhong` | `ReservationId` (`uniqueidentifier`) |
| Guest profile | `fo.HoSoKhach` | `GuestProfileId` (`uniqueidentifier`) |
| Stay | `fo.LuuTruKhachSan` | `HotelStayId` (`uniqueidentifier`) |
| Room | `fo.PhongKhachSan` | `HotelRoomId` (`uniqueidentifier`) |
| Room type | `fo.LoaiPhongKhachSan` | `HotelRoomTypeId` (`uniqueidentifier`) |
| Group reservation | `fo.DatPhongDoan` | `GroupReservationId` (`uniqueidentifier`) |
| Group assignment | `fo.PhanPhongDoan` | `GroupRoomAssignmentId` (`uniqueidentifier`) |
| Folio | `fo.SoFolio` | `HotelFolioId` (`uniqueidentifier`) |
| Folio entry | `fo.ButToanFolio` | `HotelFolioEntryId` (`uniqueidentifier`) |
| Guest document | `fo.GiayToKhach` | `GuestDocumentId` (`uniqueidentifier`) |
| Lost & Found item | `fo.DoThatLac` | `LostFoundItemId` (`uniqueidentifier`) |
| FO audit row | `fo.KiemToan` | `AuditLogId` (`uniqueidentifier`) |

## 7. Foreign Key Map

Verified Front Office foreign keys are restrictive (`NO ACTION`) and include:

| Child | Parent | Relationship |
|---|---|---|
| `fo.LichSuDatPhong.ReservationId` | `fo.DatPhong.ReservationId` | Reservation history → reservation |
| `fo.LuuTruKhachSan.ReservationId` | `fo.DatPhong.ReservationId` | Stay → reservation |
| `fo.LuuTruKhachSan.RoomId` | `fo.PhongKhachSan.HotelRoomId` | Stay → room |
| `fo.LuuTruKhachSan.RoomTypeId` | `fo.LoaiPhongKhachSan.HotelRoomTypeId` | Stay → room type |
| `fo.LichSuLuuTruKhachSan.HotelStayId` | `fo.LuuTruKhachSan.HotelStayId` | Stay history → stay |
| `fo.PhongKhachSan.HotelRoomTypeId` | `fo.LoaiPhongKhachSan.HotelRoomTypeId` | Room → room type |
| `fo.SoFolio.HotelStayId` | `fo.LuuTruKhachSan.HotelStayId` | Folio → stay |
| `fo.ButToanFolio.HotelFolioId` | `fo.SoFolio.HotelFolioId` | Folio entry → folio |
| `fo.GiayToKhach.GuestProfileId` | `fo.HoSoKhach.GuestProfileId` | Document → guest |
| `fo.PhanPhongDoan.GroupReservationId` | `fo.DatPhongDoan.GroupReservationId` | Assignment → group reservation |

There is **no verified database FK** from `fo.DatPhong.GuestId` to `fo.HoSoKhach`, nor from reservation rate/source/assigned-room columns to their likely master tables. Those columns exist but the required integrity is not database-enforced.

## 8. Core Entity Relationships

```text
RoomType ──< Room
Reservation ──< Stay ──< Folio ──< FolioEntry
Reservation ──< ReservationHistory
Stay ──< StayHistory
GuestProfile ──< GuestDocument
GroupReservation ──< GroupRoomAssignment
```

`GuestId` appears on reservation, stay and several supporting tables, but is not protected by a verified foreign key to the guest-profile table. The live data model also stores guest/room/reservation snapshots alongside IDs; these are useful for historical display but must not silently replace canonical relationships.

## 9. Guest Data Source

**Candidate canonical table:** `fo.HoSoKhach` (`GuestProfileId`, `GuestNumber`, profile, contact, VIP/blacklist and lifetime metrics). `fo.GiayToKhach` and `fo.SoThichKhach` are dependent structures.

**Live result:** `fo.HoSoKhach = 0`, `fo.GiayToKhach = 0`; no populated Front Office guest master was verified. `dbo.Customers` is also empty. Guest names currently visible in reservation/stay data are snapshots, not a verified live master source of truth.

## 10. Reservation Data Source

**Source of truth candidate:** `fo.DatPhong` with `fo.LichSuDatPhong` for state history. The table has `PropertyId`, arrival/departure, guest snapshot, room type/assigned-room IDs, status and rowversion.

**Live result:** `fo.DatPhong = 10`; `fo.LichSuDatPhong = 20`. This is suitable only as pilot/seed evidence until ownership, population, guest linkage and rate/deposit persistence are completed.

## 11. Stay Data Source

**Source of truth candidate:** `fo.LuuTruKhachSan` with `fo.LichSuLuuTruKhachSan`.

**Live result:** `fo.LuuTruKhachSan = 5`; `fo.LichSuLuuTruKhachSan = 9`. The FK chain to reservation, room and room type is present. Guest is represented by both `GuestId` and snapshots, but the guest-master FK is absent.

## 12. Room Data Source

**Source of truth candidate:** `fo.PhongKhachSan` and `fo.LoaiPhongKhachSan`.

**Live result:** 12 rooms and 3 room types. Property-scoped unique constraints for room number and type code are configured in EF; live cardinality is too small to indicate production readiness.

## 13. Group Data Source

**Candidate tables:** `fo.DatPhongDoan` and `fo.PhanPhongDoan`.

**Live result:** both are present and both have **0 rows**. Group booking and group room-assignment are therefore not live-data-backed capabilities at audit time.

## 14. Folio Data Source

**Candidate tables:** `fo.SoFolio` and `fo.ButToanFolio`, with a verified stay → folio → folio-entry FK chain.

**Live result:** both tables have **0 rows**. No populated hotel folio/posting source was verified.

## 15. Payment Data Source

**Observed generic finance tables:** `dbo.PaymentAllocations` and `dbo.ReceivableTransactions` exist but have **0 rows**. `dbo.CashTransactions` contains operational rows, but no verified FK ties it to `fo.SoFolio` or a hotel stay.

**Conclusion:** no live, hotel-scoped payment source of truth is currently verified. Restaurant/cafe payment tables are separate bounded-context data and must not be assumed to settle hotel folios.

## 16. Invoice / AR Data Source

**Observed tables:** `dbo.Invoices`, `dbo.InvoiceLines`, `dbo.InvoiceTaxBreakdowns`, `dbo.InvoiceIssueLogs`, `dbo.ReceivableTransactions`, `dbo.PaymentAllocations`.

**Live result:** key invoice/AR/payment-allocation tables queried are all **0 rows**. They are not a confirmed live source for hotel invoice or receivable operations.

## 17. Housekeeping Data Source

No populated, dedicated Front Office housekeeping/room-cleaning table was found in `fo`. The room master contains operational status only. Therefore the currently demonstrated housekeeping UI must not be connected to a claimed live housekeeping data source without a separately verified schema and dataset.

## 18. Key Card Data Source

No Front Office key-card issue, credential, lock integration or card-event table was found in the live `fo` schema inventory. A live key-card source of truth is **not verified**.

## 19. Lost & Found Data Source

**Candidate tables:** `fo.DoThatLac`, `fo.LichSuThatLac`.

**Live result:** both are present but empty. The schema is not evidence of an operating Lost & Found dataset.

## 20. Audit Data Source

`fo.KiemToan` includes `PropertyId`, entity/action values, timestamp and `BusinessDate`, but has **0 rows**. In contrast, populated platform audit structures exist in `dbo`: `AuditLogs` (12,842), `AuditSessions` (6,715), `AuditCorrelations` (9,004), `AuditEntitySnapshots` (9,003) and `AuditReplayEntries` (9,004).

**Live source-of-truth conclusion:** platform audit is the only populated audit trail verified. FO-specific audit is structurally present but inactive.

## 21. Property Scope

The FO core tables inspected carry mandatory `PropertyId`. The database also has branch-scoped `dbo` structures and contains one `dbo.Companies` row and one `dbo.Branches` row.

**Risk:** the audit confirms the scope key exists but does not prove that every current FO row is consistently bound to the active company/branch/property identity. Production readiness requires an explicit property-to-company/branch contract before real multi-property data is introduced.

## 22. Business Date

`BusinessDate` is physically present on `fo.KiemToan` and process tables (`fo_process.QuyTrinhVanHanh`, `fo_process.ThuDaChetQuyTrinh`). It is **not present** on core reservation, stay, room, guest, folio, invoice or payment tables inspected.

**Conclusion:** a database-level hotel business-date source of truth for operational transactions is not verified. Do not infer it from calendar dates, UTC timestamps or UI state.

## 23. Concurrency

The following FO aggregates have SQL Server `timestamp`/rowversion columns: reservation, group reservation, guest profile, room type, room, stay, folio, lost-and-found item, rate plan and selected service/notification records. This provides an optimistic-concurrency mechanism at aggregate level.

`fo.ButToanFolio`, history tables and some assignment/support structures do not show an equivalent rowversion in the verified column map. Concurrency protection is therefore uneven across the FO data surface.

## 24. EF Model vs Database

The current `AppDbContext` maps the FO tables (`DatPhong`, `LuuTruKhachSan`, `PhongKhachSan`, `LoaiPhongKhachSan`, `SoFolio`, `ButToanFolio`, group tables and histories) into schemas `fo`/`fo_process`, and it declares the primary relationship chain verified above.

**Confirmed live drift:** the EF mapping for `Reservation` / `fo.DatPhong` includes `GuaranteeType`, `DepositStatus` and `DepositAmount`, while the live table column inventory does not contain those three columns. This is a real runtime schema-model incompatibility until resolved through an approved schema/change-management phase.

The model also exposes many optional IDs where corresponding database FKs are not present, notably reservation/stay guest references.

## 25. Migration vs Database

Live `__EFMigrationsHistory` contains **99** applied records, from `20260421153713_InitialCreate` through `20260719230000_AddFrontOfficeSettingsPhase2` (EF product version `8.0.7`).

The current checkout contains the migration policy service/tests but no migration source classes were discovered outside build artifacts. As a result, a full source-migration-to-live-schema comparison cannot be reproduced from this checkout. The confirmed `fo.DatPhong` mismatch above is nevertheless sufficient evidence of drift. Runtime migration application is disabled by configuration.

## 26. Data Counts

| Dataset | Exact count at audit |
|---|---:|
| Reservations | 10 |
| Reservation history | 20 |
| Stays | 5 |
| Stay history | 9 |
| Rooms | 12 |
| Room types | 3 |
| Guest profiles | 0 |
| Guest documents | 0 |
| Group reservations | 0 |
| Group assignments | 0 |
| Folios / folio entries | 0 / 0 |
| Lost & Found items | 0 |
| FO audit logs | 0 |
| Platform audit logs | 12,842 |
| Invoices | 0 |
| Payment allocations | 0 |
| Receivable transactions | 0 |

## 27. Source of Truth Map

| Domain | Current source-of-truth assessment |
|---|---|
| Property / branch | `dbo.Companies` + `dbo.Branches`; one row each verified |
| Room / room type | `fo.PhongKhachSan` + `fo.LoaiPhongKhachSan` — populated pilot data |
| Reservation | `fo.DatPhong` + history — populated pilot data |
| Stay | `fo.LuuTruKhachSan` + history — populated pilot data |
| Guest | No populated canonical source verified |
| Group | No populated canonical source verified |
| Folio / posting | No populated canonical source verified |
| Payment / invoice / AR | No populated hotel-linked source verified |
| Housekeeping / key card | No live source verified |
| Lost & Found | Schema candidate only; no data |
| Audit | `dbo.AuditLogs` family is populated; FO audit table is inactive |

## 28. Duplicate / Legacy Structures

The database is a mixed application database: shared `dbo` financial/restaurant/banquet structures coexist with the `fo` hotel boundary. Multiple UI modules that display similar concepts must not select a table merely because names resemble one another (for example, restaurant payments versus hotel folio payment).

Snapshot columns in reservation/stay/group/lost-found records duplicate guest and room attributes by design for historical context. They are not substitutes for normalized guest and room master relationships.

## 29. Database Gaps

1. Guest master and guest-document datasets are empty.
2. Group, folio, posting, hotel payment, invoice and AR datasets are empty.
3. No verified dedicated housekeeping, room-inspection or key-card persistence source exists in the FO schema.
4. Guest, reservation room/rate/source and several cross-aggregate ID links lack verified database FKs.
5. A single authoritative business-date source for core hotel operations is not verified.
6. Current source migration artifacts are absent from the checkout, preventing reliable migration reconciliation.
7. The live `fo.DatPhong` table is behind the current EF mapping for guarantee/deposit fields.

## 30. Critical Risks

| Severity | Risk |
|---|---|
| Critical | Connecting mock UI to empty or unrelated finance/restaurant data would create false hotel business behavior. |
| Critical | EF reservation reads/writes that materialize guarantee/deposit properties can fail because the live table is missing mapped columns. |
| High | Missing database FKs permit orphaned or cross-property guest/reservation/stay references. |
| High | There is no verified live hotel payment-to-folio settlement chain. |
| High | Missing migration source history blocks deterministic schema reconciliation. |
| Medium | FO audit and business-date structures exist but are not populated/established as operational sources. |

## 31. Production Implications

The live database is reachable and contains a partial Front Office schema with small pilot data, not a completed hotel operational dataset. It is **not safe to connect all current Hotel Management UI modules to this database as if it were production-complete**.

Any production connection plan must first decide the canonical owner for guest, reservation, stay, payment/folio, invoice/AR, property and business-date data; establish required FK and concurrency boundaries; reconcile EF and migration artifacts with the exact runtime database; and prove data-loading, authorization and audit behavior with controlled test records. Those actions are outside this Phase 1 audit.

## 32. Recommended Next Phase

**Phase 2 — Data Contract & Gap Closure (design-only first):** approve a canonical entity contract and ownership matrix for the source-of-truth map; reconcile the confirmed `fo.DatPhong` EF/database drift; define missing relationships and business-date/property rules; decide whether unpopulated FO areas receive real tables/data or remain unavailable; then prepare an explicitly approved, idempotent migration and controlled seed/import plan. No UI module should be wired to an unverified or empty source before that contract is accepted.
