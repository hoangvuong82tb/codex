# HOTEL MANAGEMENT DATA CONTRACT — PHASE 2A

## 1. Purpose

Define the production data contract for the Hotel Management domain before Phase 2B connects real data. This document is an evidence-led analysis and target contract; it does not change code, schema, migrations, configuration, mock data, or UI.

**Contract status:** draft for architecture approval. “Target” means a contract to be implemented only after Phase 2B approval.

## 2. Scope

Included: property scope, guest, company, reservation, room, stay, group, folio, posting, payment, deposit, refund, invoice, AR, housekeeping, minibar, lost and found, key card, audit, permission, read models, workflow and integrity.

Excluded: implementation, SQL execution, migration generation/application, data repair, UI redesign, source-code edits, and configuration changes.

## 3. Principles

1. One business concept has one canonical write source.
2. Operational records preserve historical snapshots; master data remains mutable.
3. Property scope is explicit and mandatory where a record can affect hotel operations.
4. Financial facts are append-only or reversable, never silently overwritten.
5. Workflow transitions require permission, audit, concurrency control, and idempotency where retried.
6. A read model may combine sources but cannot become an ungoverned write source.
7. Missing evidence is recorded as an open decision, not inferred.

## 4. Source Documents

- `HOTEL_MANAGEMENT_LIVE_DB_MAP.md`: live SQL Server inventory and populated-table evidence.
- `docs/HOTEL_MANAGEMENT_PRODUCTION_READINESS_AUDIT.md`: repository, CQRS, mock, workflow, and readiness evidence.
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`: active EF mapping and relationship configuration.
- Front Office domain/application/infrastructure sources: Reservation, Stay, Folio, Payment, Settlement, Group, Housekeeping, LostFound.
- Current WPF view models and navigation registrations, inspected to identify entry points and mock/read-model exposure.

## 5. Current Architecture Summary

The current architecture has useful layered foundations: domain aggregates, EF repositories for reservation/room/stay/folio/payment/invoice, a DbContext, CQRS handlers, unit-of-work/persistence-session patterns, outbox/inbox support, and platform audit tables.

The live database is not yet a complete Hotel Management production model. It contains 305 user tables (245 `dbo`, 51 `fo`, 9 `fo_process`). Reservation, stay, room, and room type have pilot data; many intended FO entities are physically present but empty. Several UI modules still create random/mock data. Therefore the present system is a mixed transition state, not evidence that all displayed workflows are persisted end-to-end.

## 6. Canonical Entity Map

| Entity | Canonical Source | PK | Business Key | Property Scope | Status | Current State | Target State |
|---|---|---|---|---|---|---|---|
| Property | Property master — evidence incomplete | GUID/TBD | PropertyCode | Self | Active/Inactive | Property reference not verified in reviewed live map | Canonical property master |
| Company | `dbo.Companies` | existing key | CompanyCode/TIN | Global or tenant | Active/Inactive | populated candidate | Master source after ownership decision |
| Branch | `dbo.Branches` | existing key | BranchCode | Tenant | Active/Inactive | populated candidate | Property-to-branch scope defined |
| BusinessDate | `fo.KiemToan` candidate | GUID/TBD | Property + business date | Property | Open/Closed/Reopened | only audit/process evidence; not core-wide | Property business-date authority |
| GuestProfile | `fo.HoSoKhach` candidate | GUID | GuestNo/TBD | Tenant, optional property history | Active/Blocked/Merged | table empty; no verified reservation FK | Canonical guest profile |
| GuestDocument | `fo.GiayToKhach` | GUID | DocumentType + Number + issuer | Tenant | Valid/Expired/Redacted | table empty; FK to guest exists | Controlled PII document store |
| GuestPreference | missing verified persistence | GUID/TBD | Guest + preference type | Tenant | Active/Revoked | unsupported current model | Guest preference capability |
| Reservation | `fo.DatPhong` | GUID | ReservationNo | Property | Draft/Tentative/Confirmed/Cancelled/NoShow | 10 pilot rows | Canonical booking aggregate |
| ReservationHistory | `fo.LichSuDatPhong` | GUID | Reservation + sequence | Property | Event record | 20 rows; FK exists | Immutable reservation event log |
| Room | `fo.PhongKhachSan` | GUID | Property + RoomNo | Property | Active/OutOfService/Inactive | 12 pilot rows | Canonical room inventory |
| RoomType | `fo.LoaiPhongKhachSan` | GUID | Property + TypeCode | Property | Active/Inactive | 3 pilot rows | Canonical room classification |
| RatePlan | `fo.BangGiaPhong` candidate | GUID | Property + RateCode | Property | Draft/Active/Expired | empty | Controlled pricing master |
| RateRule | `fo.ChinhSachGiaPhong` candidate | GUID | RatePlan + effective range | Property | Draft/Active/Expired | empty | Rule-driven pricing |
| Stay | `fo.LuuTruKhachSan` | GUID | StayNo/TBD | Property | Reserved/CheckedIn/CheckedOut/Cancelled | 5 pilot rows; guest FK absent | Canonical in-house stay |
| StayHistory | `fo.LichSuLuuTruKhachSan` | GUID | Stay + sequence | Property | Event record | 9 rows; FK exists | Immutable stay event log |
| GroupReservation | `fo.DatPhongDoan` | GUID | GroupCode | Property | Draft/Confirmed/Arrived/Closed | empty | Canonical group aggregate |
| GroupRoomAssignment | `fo.PhanPhongDoan` | GUID | Group + guest/stay/room | Property | Planned/Assigned/Released | empty; group FK exists | Controlled rooming assignment |
| HotelFolio | `fo.SoFolio` | GUID | FolioNo | Property | Open/Locked/Settled/Closed | empty; stay FK exists | Canonical guest account |
| HotelFolioEntry | `fo.ButToanFolio` | GUID | Folio + entry sequence | Property | Posted/Reversed/Voided | empty; folio FK exists | Append-only financial entry |
| Posting | HotelFolioEntry projection | GUID/TBD | PostingNo | Property | Posted/Reversed/Voided | no independent proven canonical table | Contracted terminology/projection |
| Payment | `fo.ThanhToan` candidate; mapping must be verified | GUID/TBD | PaymentNo | Property | Pending/Posted/Reversed/Refunded | current repository exists; live canonical linkage unverified | Canonical payment fact |
| PaymentAllocation | `dbo.PaymentAllocations` candidate | existing key | Payment + target sequence | Property/TBD | Allocated/Reversed | generic finance table exists; FO FK unverified | Allocation bridge |
| Deposit | `fo.DatCoc` candidate | GUID/TBD | DepositNo | Property | Held/Applied/Refunded/Forfeited | empty candidate | Deposit sub-ledger |
| Refund | `fo.HoanTien` candidate | GUID/TBD | RefundNo | Property | Requested/Approved/Posted/Reversed | empty candidate | Refund sub-ledger |
| Invoice | `dbo.Invoices` candidate | existing key | InvoiceNo | Property/Legal entity | Draft/Issued/Cancelled/Adjusted | generic tables empty; FO linkage unverified | Legal invoice aggregate |
| InvoiceLine | `dbo.InvoiceLines` candidate | existing key | Invoice + line no | Property/Legal entity | Active/Cancelled | generic tables empty | Invoice line source |
| Receivable | AR account projection | GUID/TBD | Debtor + currency | Property/Legal entity | Open/Closed/Disputed | no verified FO canonical table | Derived AR balance |
| ReceivableTransaction | `dbo.ReceivableTransactions` candidate | existing key | AR transaction no | Property/Legal entity | Posted/Reversed | generic source candidate, linkage unverified | AR sub-ledger |
| HousekeepingTask | missing verified canonical FO persistence | GUID/TBD | Property + room + task/date | Property | Queued/Assigned/InProgress/Inspected/Done | UI mock evidence; no dedicated table proved | Task aggregate |
| RoomInspection | missing verified canonical FO persistence | GUID/TBD | Task + inspection sequence | Property | Pending/Passed/Failed | UI mock evidence | Inspection evidence |
| MinibarSession | missing verified canonical FO persistence | GUID/TBD | Stay/room + business date | Property | Open/Checked/Posted/Voided | UI mock evidence | Consumption session |
| MinibarConsumption | missing verified canonical FO persistence | GUID/TBD | Session + line no | Property | Recorded/Posted/Voided | UI mock evidence | Consumption evidence |
| LostFoundItem | `fo.DoThatLac` | GUID | LostFoundNo/TBD | Property | Found/Stored/Claimed/Returned/Disposed | table empty; UI mock evidence | Property custody item |
| KeyCardCredential | missing verified FO table | GUID/TBD | Card serial | Property | Issued/Active/Disabled/Returned | no FO table proved | Credential aggregate |
| KeyCardEvent | missing verified FO table | GUID/TBD | Card + event sequence | Property | Event record | no FO table proved | Card audit event |
| AuditRecord | platform audit tables | existing key | Correlation + sequence | Tenant/platform | Immutable | AuditLogs/Sessions/Correlations/Snapshots populated | Shared authoritative audit |
| PermissionAssignment | platform authorization source — evidence incomplete | existing key | Subject + role/capability | Tenant | Active/Revoked | UI guards exist; live source not fully verified | Capability authorization |
| WorkflowProcess | `fo_process` candidate + platform workflow | GUID/TBD | Process type + aggregate + correlation | Property | Pending/Running/Completed/Failed/Compensated | process schema empty | Long-running workflow state |

## 7. Source of Truth Map

| Business Concept | Canonical Source | Legacy/Secondary | Consumers |
|---|---|---|---|
| Property scope | Property master — unresolved | Branch/company references | All hotel writes and read models |
| Company/debtor | `dbo.Companies` candidate | AR/marketing company screens | Reservation, invoice, AR |
| Guest identity | `fo.HoSoKhach` proposed candidate | legacy guest sources; `dbo.Customers` empty | Reservation, stay, CRM, folio |
| Guest document | `fo.GiayToKhach` | scanned document provider | Check-in, compliance |
| Reservation | `fo.DatPhong` | reservation UI read model | Stay, group, rate, invoice |
| Room inventory | `fo.PhongKhachSan` | room-management read model | Reservation, stay, housekeeping |
| Stay | `fo.LuuTruKhachSan` | stay history | Folio, key card, housekeeping |
| Group | `fo.DatPhongDoan` | group read model | rooming, group service, check-in |
| Folio/Posting | `fo.SoFolio` + `fo.ButToanFolio` | reporting projection | settlement, invoice, AR |
| Payment | proposed FO payment aggregate | generic cash/payment tables | folio allocation, AR, receipt |
| Deposit/refund | proposed FO sub-ledgers | reservation guarantee fields | stay/folio/AR |
| Invoice | legal invoice aggregate decision required | `dbo.Invoices` candidate | AR, tax, reporting |
| Housekeeping | proposed task aggregate | room status projection | operations, supervisor |
| Lost & Found | `fo.DoThatLac` | platform audit/photo storage | housekeeping, guest service |
| Key card | proposed credential aggregate | external card vendor | check-in/out, security |
| Audit | platform AuditLogs/Sessions/Correlations/Snapshots | entity histories | all material operations |

## 8. ID Strategy

Current FO aggregate tables use GUID primary keys. Retain GUIDs for distributed workflow correlation and offline/integration safety.

**PROPOSED CHANGE:** define an immutable public business-key convention per aggregate (for example ReservationNo, FolioNo, PaymentNo, GroupCode) and keep GUID as the technical PK.  
**Reason:** UI and integrations need readable stable identifiers without exposing technical IDs.

## 9. Property Scope

`PropertyId` is present in the reviewed reservation, room, room type, stay, folio and several FO entities. Its authoritative property-master relationship and cross-property enforcement were not verified.

**PROPOSED CHANGE:** require a valid PropertyId on every property-operational aggregate and carry it into append-only financial/audit facts.  
**Reason:** a multi-property hotel system cannot safely infer ownership through room, reservation, or company alone.

## 10. Business Date

The live model exposes BusinessDate evidence only in `fo.KiemToan`/process-related tables, not consistently in reservation, stay, room, guest, folio, payment, or invoice core records.

**PROPOSED CHANGE:** establish one property-scoped business-date authority with open/close/reopen controls; financial postings reference its business date while retaining UTC event time.  
**Reason:** night audit, cut-off, revenue, and operational date must be deterministic.

## 11. Guest

Guest profile is intended in `fo.HoSoKhach`, but that table is empty. `dbo.Customers` is also empty and cannot be assumed equivalent. `fo.DatPhong` has no verified FK to the guest profile.

**PROPOSED CHANGE:** choose `fo.HoSoKhach` as the canonical hotel guest only after data-model/ownership approval; introduce a required Reservation-to-Guest relationship or an explicitly governed guest snapshot when booking without a profile is allowed.  
**Reason:** current stay and reservation chains cannot prove the guest identity.

## 12. Company

`dbo.Companies` and `dbo.Branches` are populated candidates. Company, TA, OTA, agent, and corporate debtor roles remain semantically distinct and must not be inferred from screen labels.

**PROPOSED CHANGE:** model company role(s), credit policy, invoice party, payer, and property applicability separately from the company identity.  
**Reason:** one company can play multiple commercial roles.

## 13. Reservation

`fo.DatPhong` is the strongest current booking candidate with 10 pilot rows and rowversion. ReservationHistory has an FK to it. Current mapping/live-schema drift exists: EF mapping references GuaranteeType, DepositStatus, and DepositAmount that are absent in the verified live table.

**PROPOSED CHANGE:** resolve the verified EF-to-live schema drift before any production reservation write path is approved.  
**Reason:** a mapped property that does not exist at runtime is a P0 persistence risk.

## 14. Room

Room and RoomType have live pilot records, restrictive relationships, and property-scoped uniqueness configured in EF. Room operational status is not sufficient to replace housekeeping task state.

**PROPOSED CHANGE:** separate inventory status, FO occupancy status, and housekeeping/maintenance status dictionaries.  
**Reason:** one overloaded room status creates invalid transitions and unreliable availability.

## 15. Stay

Stay has Reservation, Room, and RoomType foreign keys. No verified guest FK exists. StayHistory correctly links to Stay.

**PROPOSED CHANGE:** add/approve a direct guest identity reference or governed immutable guest snapshot at stay creation.  
**Reason:** a hotel stay must be attributable to a specific guest, not only a reservation.

## 16. Group

GroupReservation and GroupRoomAssignment tables exist but are empty. Group assignment only has a verified FK to group; guest, stay, and room linkage must be confirmed.

**PROPOSED CHANGE:** define group-rooming cardinality and assignment ownership before enabling group operational writes.  
**Reason:** group booking, room assignment, and individual stays cannot share ambiguous authority.

## 17. Folio

Folio links to Stay and FolioEntry links to Folio; both tables are empty. This is the intended account boundary, but current real linkage to payment, invoice, deposit, refund, and AR is not verified.

**PROPOSED CHANGE:** designate FolioEntry as the only canonical operational posting ledger and prohibit direct balance edits.  
**Reason:** financial balances must be reproducible from entries.

## 18. Posting

“Posting” is currently a UI/business term. A separate canonical posting entity was not proven; FolioEntry is the closest persistence evidence.

**PROPOSED CHANGE:** define Posting as a governed projection/command vocabulary over FolioEntry unless a separate ledger is approved.  
**Reason:** duplicate financial ledgers would create reconciliation ambiguity.

## 19. Payment

An EF payment repository and payment handlers exist, but the live canonical FO payment linkage was not verified. Generic `dbo.CashTransactions` contains data but no verified FK ties it to hotel stay/folio.

**PROPOSED CHANGE:** approve one hotel payment aggregate with method, currency, tender, external reference, payer, property, business date, and immutable allocation history.  
**Reason:** payment UI capability cannot substitute for a reconciled persistence contract.

## 20. Deposit

Deposit concepts appear in reservation semantics and candidate FO tables, but population and linkage are not proven.

**PROPOSED CHANGE:** model deposit as a distinct sub-ledger that can be held, applied, refunded, forfeited, or reversed, with links to reservation/stay/folio.  
**Reason:** a deposit is not automatically a revenue posting or payment allocation.

## 21. Refund

Refund handling is not proven as a populated, linked live flow.

**PROPOSED CHANGE:** require a Refund record referencing its originating payment/deposit/allocation and authorization/audit context.  
**Reason:** refund without origin linkage defeats reconciliation and fraud controls.

## 22. Invoice

`dbo.Invoices` and `dbo.InvoiceLines` are candidates but empty in the reviewed map. Whether they are the legal hotel invoice source is unresolved.

**PROPOSED CHANGE:** decide legal-invoice ownership, numbering authority, issuer legal entity, tax snapshot and cancellation/adjustment mechanics before reusing generic invoice tables.  
**Reason:** legal invoicing cannot be inferred from a UI card or a generic table name.

## 23. AR

Generic AR tables exist, while current AR UI readiness audit reports mock/random signals in multiple AR modules. No complete verified link from reservation/stay/folio/payment/invoice to AR was found.

**PROPOSED CHANGE:** derive AR from approved invoice/payment/allocation facts, or explicitly approve an AR sub-ledger with reconciliation rules.  
**Reason:** AR balance must not be a separately edited presentation value.

## 24. Housekeeping

No dedicated canonical housekeeping persistence was verified, while room clean/dirty/inspection/workforce screens contain mock evidence.

**PROPOSED CHANGE:** create a property-room-business-date task/inspection model only in Phase 2B after workflow approval.  
**Reason:** room status alone cannot preserve assignment, checklist, inspection, retry, and SLA evidence.

## 25. Minibar

No canonical minibar persistence was verified; current minibar UI has mock evidence.

**PROPOSED CHANGE:** model minibar session and consumption lines against stay/room/business date, then post approved consumption exactly once to FolioEntry.  
**Reason:** stock/charge reconciliation requires an explicit source event.

## 26. Lost & Found

`fo.DoThatLac` exists but is empty. UI mock evidence remains.

**PROPOSED CHANGE:** keep LostFoundItem as a property-custody aggregate with status history, claimant verification, return/disposal evidence, and photo/document references.  
**Reason:** custody and guest privacy need auditable chain of possession.

## 27. KeyCard

No dedicated FO key-card table was proved. Legacy analysis is a source of business ideas only, not an implementation authority.

**PROPOSED CHANGE:** define credential and event records, external-device idempotency key, stay/guest authorization window, replacement/disable reasons, and vendor integration ownership.  
**Reason:** physical-access events need traceability and safe retry behavior.

## 28. Audit

Platform AuditLogs, AuditSessions, AuditCorrelations, AuditEntitySnapshots, and AuditReplayEntries are populated. FO-specific audit table is empty.

**PROPOSED CHANGE:** use platform audit/correlation as the authoritative cross-module audit layer and add domain event/history records only where they add business meaning.  
**Reason:** duplicating full audit streams creates conflicting evidence.

## 29. Permission

Permission checks are present in selected application handlers, but a complete live role-to-capability matrix was not verified.

**PROPOSED CHANGE:** authorize commands by capability and property scope, not by UI visibility alone.  
**Reason:** hidden/disabled controls do not protect API/CQRS command execution.

## 30. Concurrency

Rowversion is present on key aggregates including Reservation, Guest, Room, RoomType, Stay, Folio, LostFound, and Rate. It is not uniformly established for entries/histories/supporting models.

**PROPOSED CHANGE:** require ExpectedRowVersion for mutable aggregate commands; append-only events use uniqueness/idempotency instead.  
**Reason:** this prevents lost updates without treating append-only facts as mutable rows.

## 31. Transaction Boundary

The codebase includes unit-of-work/persistence-session and outbox/inbox foundations. Exact transaction boundaries are not uniformly proven for all Hotel Management workflows.

**PROPOSED CHANGE:** define one transaction boundary per aggregate command and use outbox events for cross-aggregate/external effects.  
**Reason:** room, payment, and device operations cannot safely share an implicit UI transaction.

## 32. Status Dictionary

Current screens expose overlapping status wording. A system-wide status dictionary was not proven.

**PROPOSED CHANGE:** publish separate dictionaries for reservation, stay, room FO state, housekeeping task, folio, payment, invoice, AR, group, lost-found, and key card.  
**Reason:** similarly named statuses have different allowed transitions and business effects.

## 33. Snapshot vs Master

Master data changes over time. A reservation/stay/invoice must retain values valid at the moment of contract or posting.

**PROPOSED CHANGE:** snapshot guest display name/contact consent, company payer/legal details, room type/rate, tax, currency/exchange rate, and authorization context onto operational/financial documents as appropriate.  
**Reason:** historical documents must remain explainable after master changes.

## 34. Relationship

| Parent | Child | Relationship | FK | Required | Delete Rule | Notes |
|---|---|---|---|---|---|---|
| RoomType | Room | one-to-many | verified | yes | Restrict | Current live/EF evidence |
| Reservation | ReservationHistory | one-to-many | verified | yes for history | Restrict | History is event-like |
| Reservation | Stay | one-to-many/zero-to-many | verified | reservation reference required currently | Restrict | Guest linkage missing |
| Room | Stay | one-to-many over time | verified | yes for current model | Restrict | Assignment changes need history |
| RoomType | Stay | one-to-many | verified | yes currently | Restrict | Snapshot still needed |
| Stay | StayHistory | one-to-many | verified | yes for history | Restrict | Append-only target |
| Stay | Folio | one-to-many | verified | yes for current folio | Restrict | Multi-folio policy decision required |
| Folio | FolioEntry | one-to-many | verified | yes | Restrict | Entry deletion prohibited |
| GuestProfile | GuestDocument | one-to-many | verified | yes | Restrict | PII retention rules required |
| GroupReservation | GroupRoomAssignment | one-to-many | verified | yes | Restrict | Room/guest/stay FKs unverified |
| GuestProfile | Reservation | expected | **missing verified FK** | decision | Restrict | P0 data-model decision |
| Payment | PaymentAllocation | expected | **unverified** | yes for applied amount | Restrict | Generic allocation must be reconciled |
| Payment/Deposit | Refund | expected | **unverified** | yes | Restrict | Origin required |
| Invoice | InvoiceLine | expected | candidate only | yes | Restrict | Legal model decision |
| Invoice/Folio | ReceivableTransaction | expected | **unverified** | decision | Restrict | AR architecture decision |
| Property + Room | HousekeepingTask | expected | **missing proven model** | yes | Restrict | Phase 2B design |
| Stay/Room | MinibarSession | expected | **missing proven model** | yes | Restrict | Phase 2B design |
| Stay/Guest | KeyCardCredential | expected | **missing proven model** | yes | Restrict | Device integration |

## 35. Workflow

1. Reservation: draft/tentative → confirmed → arrival/no-show/cancelled; confirmation requires guest/payer/rate/property contract.
2. Stay: reservation → check-in → in-house updates → check-out; only valid room/stay transitions may alter availability.
3. Folio: open from approved stay → append postings/allocations → settle/lock/close; reversal is a new event.
4. Payment/deposit/refund: authorize/record → post → allocate/apply → reconcile; reversal/refund retains origin.
5. Invoice/AR: prepare from approved financial facts → issue/cancel/adjust under legal controls → update AR projection.
6. Housekeeping/minibar/lost-found/key-card: command-owned operational aggregates emit events, never edit room/stay/folio state directly without governed commands.

## 36. Entry Point vs Capability

Navigation cards are entry points only. They are not permissions, persistence guarantees, or canonical source declarations.

| Entry Point | Required Capability | Aggregate Command/Query | Current Evidence |
|---|---|---|---|
| Reservation workspace | reservation.read/write | reservation commands/queries | core repo/handlers present |
| Check-in/out | stay.checkin/checkout | stay transition commands | handlers/repository present |
| Folio | folio.read/post/settle | folio command/query | repo/handlers present; real data empty |
| Cashiering | payment.record/allocate/refund | payment commands | repository/handlers present; live linkage unverified |
| Group | group.manage/rooming | group commands | tables/handlers exist; production proof incomplete |
| Housekeeping | housekeeping.assign/inspect | task commands | mock/UI evidence; persistence unsupported |
| Minibar | minibar.check/post | session commands | mock/UI evidence; persistence unsupported |
| Key card | keycard.issue/disable | credential commands | no dedicated persistence proven |
| Lost & Found | lostfound.custody | custody commands | empty table; mock/UI evidence |
| AR | ar.read/reconcile/collect | AR query/commands | mock/readiness gap |

## 37. Read Model

Read models may join Reservation → Stay → Folio → Payment/Allocation → Invoice → AR and Room → Housekeeping → Minibar. They must expose source identity, property scope, business date, last event time, and freshness.

**PROPOSED CHANGE:** version read-model contracts and distinguish “authoritative operational view” from “mock/demo view” until live repositories supply the data.  
**Reason:** a polished UI can otherwise present non-persisted facts as real.

## 38. Integrity Rules

- Every hotel operational record must resolve to exactly one property scope.
- Reservation and stay dates must satisfy arrival < departure unless a documented day-use rule applies.
- A stay cannot be checked in without valid room/guest authority; room transfer records history.
- Guest document access follows consent, PII minimization, and retention/redaction rules.
- A group assignment cannot exceed group capacity or conflict with occupied/blocked room inventory.
- Folio entry amount, currency, source command, business date, and idempotency key are mandatory.
- Payment allocations cannot exceed un-reversed posted payment amount.
- Refund amount cannot exceed net refundable origin amount.
- Invoice issue/cancel/adjustment must preserve immutable legal history.
- Delete rules are restrictive for financial and operational history; cancellation/reversal is preferred.

## 39. Financial Invariants

1. Folio balance equals the sum of non-voided entries in its currency.
2. Payment allocation total is less than or equal to available posted payment amount.
3. Deposit applied/refunded/forfeited totals do not exceed original held amount.
4. Refund references an approved origin and preserves the reason/approver.
5. Invoice total equals invoice line total plus/minus approved tax/rounding treatment.
6. AR open balance is derived from approved invoice, payment, credit, and adjustment facts.
7. Currency conversion uses the captured rate/date, never a later mutable rate.
8. Reversal produces a compensating fact and never overwrites a posted amount.

## 40. Current vs Target

| Entity | Current Problem | Target | Gap Type | Severity | Phase |
|---|---|---|---|---|---|
| Reservation | EF mapping references three absent live columns | Mapping/schema contract aligned | Migration/db drift | P0 | 2B prerequisite |
| Guest | empty candidate source; no reservation FK | canonical guest + link/snapshot | Missing persistence/relationship | P0 | 2B |
| Stay | no verified guest relationship | attributable stay identity | Integrity | P0 | 2B |
| Property | authority/FK scope unverified | canonical property master | Source-of-truth | P0 | 2B |
| Business date | not core-wide | property close/calendar authority | Missing capability | P0 | 2B |
| Payment | live hotel linkage unverified | canonical immutable payment/allocation | Financial boundary | P0 | 2B |
| Invoice | generic candidates not approved legal source | legal invoice contract | Source-of-truth | P0 | 2B |
| AR | mock/random UI signals and unclear derivation | reconciled AR projection/sub-ledger | Mock/read-model | P0 | 2B |
| Folio | tables empty, settlement linkage unproven | real posting/settlement workflow | Missing data/validation | P1 | 2B |
| Deposit | no proven applied/refunded chain | deposit sub-ledger | Missing workflow | P1 | 2B |
| Refund | origin/authorization unproven | controlled refund flow | Missing workflow | P1 | 2B |
| Group | empty tables and incomplete linkage proof | governed group rooming | Missing proof | P1 | 2B |
| Housekeeping | no dedicated model; mocks | task/inspection aggregate | Missing persistence | P1 | 2B |
| Minibar | no dedicated model; mocks | session/consumption posting flow | Missing persistence | P1 | 2B |
| KeyCard | no FO table proved | credential/event model | Missing persistence | P1 | 2B |
| LostFound | empty table/mock UI | custody workflow | Missing data/validation | P1 | 2B |
| Permission | matrix/source incomplete | capability + property authorization | Governance | P1 | 2B |
| Audit | domain/platform roles not defined | shared audit + business events | Governance | P1 | 2B |
| Concurrency | partial rowversion coverage | aggregate policy | Integrity | P1 | 2B |
| Read models | mock and live views mixed | versioned source-labelled models | Data quality | P1 | 2B |

## 41. Legacy

Legacy Front Office analysis may inform business guards, idempotency, external card behavior, logging, and workflow vocabulary. It is not a schema, implementation, or source-of-truth authority for this application.

**PROPOSED CHANGE:** translate approved legacy behaviors into explicit acceptance criteria and data-contract decisions rather than copying legacy storage/code.  
**Reason:** the legacy estate is heterogeneous and does not prove compatibility with the current model.

## 42. DB Change Proposal

No database changes are made in Phase 2A.

**PROPOSED CHANGE P0:** prepare a reviewed, idempotent Phase 2B migration/remediation proposal for Reservation mapping/live-schema drift.  
**Reason:** current verified mismatch blocks reliable reservation writes.

**PROPOSED CHANGE P0:** propose canonical property and guest/stay/reservation relationship constraints after data backfill strategy is approved.  
**Reason:** required relationships cannot be safely introduced without population and orphan assessment.

**PROPOSED CHANGE P1:** propose dedicated tables/constraints for housekeeping, minibar, key card, and any missing financial sub-ledger only after decision register approval.  
**Reason:** physical model must follow agreed ownership and workflow, not UI demand.

## 43. Capability Contract

| Capability | Reads | Writes | Required Invariants | Audit |
|---|---|---|---|---|
| Manage reservation | Reservation, guest, room/rate | Reservation/history | property, guest/payer, date/rate validity | create/change/cancel |
| Check in/out | Reservation, stay, room, key card | Stay/history, room state | valid transition, room/guest authority | transition/correlation |
| Post folio | Stay, folio, source service | FolioEntry | immutable amount/currency/source/idempotency | posting/reversal |
| Take/allocate payment | Folio, payment, AR | Payment/allocation | allocation limits, tender/ref | payment/allocation |
| Deposit/refund | Reservation/stay/payment | Deposit/refund | origin and approval | hold/apply/refund |
| Issue invoice | Folio/payment/company | Invoice/line/AR fact | legal number/tax snapshot | issue/cancel/adjust |
| Housekeeping | Room/stay/tasks | Task/inspection | property/room/date/status transition | assignment/inspection |
| Minibar | Room/stay/session | Consumption/posting request | exactly-once folio source | check/post |
| Lost & Found | Item/guest | custody events | chain of custody | every disposition |
| Key card | Stay/guest/credential | credential/event | authorization window/idempotency | issue/disable |

## 44. Permission Matrix

| Capability | Front Desk | Cashier | Night Audit | Housekeeping | Supervisor | Finance | Admin |
|---|---|---|---|---|---|---|---|
| Reservation write | scoped | read | read | no | override scoped | read | manage |
| Check-in/out | scoped | read | controlled | no | override scoped | read | manage |
| Folio posting | limited | scoped | controlled | no | no | reconcile | manage |
| Payment/refund | no/limited | scoped | controlled | no | no | approve/reconcile | manage |
| Invoice/AR | read | prepare | controlled | no | no | issue/reconcile | manage |
| Housekeeping task | read | no | read | scoped | scoped | no | manage |
| Minibar check | read | no | read | scoped | scoped | no | manage |
| Lost & Found custody | read | no | read | scoped | scoped | no | manage |
| Key card | scoped | no | read | no | override scoped | no | manage |

“Scoped” means the actor also satisfies property/shift/assignment constraints. Exact role grants remain an open authorization decision.

## 45. Audit Matrix

| Event | Required Fields | Correlation | Retention/Access Note |
|---|---|---|---|
| Reservation create/change/cancel | actor, property, before/after, reason | command correlation | operational + PII policy |
| Check-in/out/room move | actor, stay, room, transition, time | stay correlation | operational |
| Folio post/reverse | actor/system, source, amount, currency, business date | financial correlation | financial retention |
| Payment/allocate/refund | payer, method, external ref, amount, approver | payment correlation | financial/PCI minimization |
| Invoice issue/cancel/adjust | issuer, legal data snapshot, reason | invoice correlation | statutory retention |
| Housekeeping/inspection | assignee, checklist, evidence, supervisor | task correlation | operational |
| Minibar post | checker, line source, folio result | session correlation | operational/financial |
| Lost-found disposition | custodian, claimant, evidence | item correlation | custody retention |
| Key-card action | actor/system, card surrogate, stay, action | credential correlation | security retention |
| Permission change | grant/revoke, actor, target, property | authorization correlation | security retention |

## 46. Quality Rules

- No mock/random generator may supply a production-labelled read model.
- Every result row exposes a stable ID and property scope.
- Dates display with timezone/context; financial event time and business date are distinct.
- Empty, stale, unsupported, and permission-denied states are explicit.
- PII fields are minimized, masked in broad lists, and audited when accessed.
- Read-model totals reconcile to authoritative facts or show a freshness/error state.
- Dropdown/options are sourced from explicit master/status dictionaries, not scroll-only pseudo-select behavior.

## 47. Production Readiness Implications

The domain is not ready for broad real-data cutover until P0 relationships, schema drift, source-of-truth, payment/invoice/AR boundaries, and business-date authority are resolved. Existing repositories/handlers are useful starting points but are not proof of end-to-end production behavior. Mock-backed modules must be clearly isolated until their write/read contracts and database evidence are approved.

## 48. Decisions

### D-01 — Property authority
- **Question:** What table/service is the canonical Property source and how does it relate to Branch?
- **Current Evidence:** PropertyId is used in FO; property master relationship was not verified.
- **Options:** use existing branch as property; introduce dedicated property master; integrate an external property authority.
- **Recommended:** approve a dedicated canonical property contract, with explicit branch/legal-entity mapping.
- **Reason:** all hotel ownership and business-date controls depend on it.
- **Impact:** P0 schema, authorization, and data-backfill work.

### D-02 — Guest identity
- **Question:** Is `fo.HoSoKhach` the canonical hotel guest, and are anonymous bookings allowed?
- **Current Evidence:** `fo.HoSoKhach` and `dbo.Customers` are empty; reservation guest FK is not verified.
- **Options:** canonical FO profile; reuse generic customer; guest snapshot only; hybrid profile plus snapshot.
- **Recommended:** FO GuestProfile plus immutable operational snapshots.
- **Reason:** preserves a hotel-specific identity while keeping historical documents stable.
- **Impact:** P0 relationship/backfill and PII governance.

### D-03 — Reservation-to-stay guest linkage
- **Question:** Should a stay reference GuestProfile directly or only reservation guest snapshot?
- **Current Evidence:** Stay links reservation/room/room type, not verified guest.
- **Options:** direct FK; snapshot only; both.
- **Recommended:** both direct FK and immutable snapshot.
- **Reason:** supports current identity, legal history, and guest merges.
- **Impact:** P0 integrity change.

### D-04 — Business date
- **Question:** Which component opens/closes/reopens property business date?
- **Current Evidence:** BusinessDate is not core-wide; audit/process candidate exists.
- **Options:** Night Audit aggregate; external PMS calendar; application setting.
- **Recommended:** property-scoped Night Audit/business-date aggregate with controlled reopen.
- **Reason:** avoids inconsistent revenue cut-off.
- **Impact:** P0 workflow and posting contract.

### D-05 — Folio and posting authority
- **Question:** Is `fo.ButToanFolio` the sole posting ledger?
- **Current Evidence:** Folio/FolioEntry FKs exist but tables are empty.
- **Options:** FolioEntry sole ledger; separate posting ledger; generic finance ledger.
- **Recommended:** FolioEntry sole operational ledger, mapped to finance via outbox/projection.
- **Reason:** minimizes duplicated balance authorities.
- **Impact:** P1 payment, invoice, AR integration.

### D-06 — Payment and allocation authority
- **Question:** Can generic `dbo.PaymentAllocations` be reused for hotel payments?
- **Current Evidence:** generic table exists; FO linkage is not verified.
- **Options:** reuse directly; adapter/projection; hotel allocation table.
- **Recommended:** decide only after key/ledger/transaction semantics are compared; default to adapter rather than assumption.
- **Reason:** table existence does not prove financial compatibility.
- **Impact:** P0 payment/AR design.

### D-07 — Legal invoice authority
- **Question:** Are `dbo.Invoices`/InvoiceLines the legal hotel invoice aggregate?
- **Current Evidence:** candidate tables are empty; legal constraints not verified.
- **Options:** reuse; new hotel invoice aggregate; external e-invoice provider as authority.
- **Recommended:** defer until legal numbering/tax/provider ownership is verified.
- **Reason:** legal issuance cannot be retrofitted from UI behaviour.
- **Impact:** P0 invoice/AR cutover gate.

### D-08 — AR architecture
- **Question:** Is AR a derived projection or an independent sub-ledger?
- **Current Evidence:** AR UI mock evidence and generic receivable tables; end-to-end linkage unverified.
- **Options:** derived only; sub-ledger; hybrid.
- **Recommended:** an append-only receivable transaction sub-ledger with a derived ageing projection.
- **Reason:** supports audit/reconciliation while keeping UI totals derivable.
- **Impact:** P0 finance contract.

### D-09 — Housekeeping/minibar/key-card storage
- **Question:** Which operations require first-class aggregates rather than projections?
- **Current Evidence:** no dedicated persistence proven; UI mock evidence.
- **Options:** add separate aggregates; use room status only; external service ownership.
- **Recommended:** dedicated aggregates for task/inspection, consumption, and credential/event where ASMILE owns writes.
- **Reason:** status-only designs cannot meet evidence and retry requirements.
- **Impact:** P1 Phase 2B domain/schema work.

### D-10 — Authorization model
- **Question:** What is the canonical role/capability/property-scope source?
- **Current Evidence:** selected command checks exist; complete matrix/live source unverified.
- **Options:** role-only; capability-only; role grants capabilities plus property scope.
- **Recommended:** capability authorization with role grants and property scope.
- **Reason:** supports separation of duties and controlled overrides.
- **Impact:** P1 cross-module enforcement.

## 49. Risks

| Risk | Consequence | Mitigation |
|---|---|---|
| Applying EF against current reservation drift | runtime write failure or silent inconsistency | resolve P0 schema/mapping evidence first |
| Treating mock UI values as persisted truth | incorrect operational/financial decisions | source-label views and cut over module-by-module |
| Reusing generic finance tables by name | duplicate/unreconciled payments or AR | contract/key/transaction comparison before reuse |
| Missing guest/stay/property FKs | orphaned stays and cross-property leakage | approved backfill + restrictive constraints |
| Business-date ambiguity | incorrect night-audit and revenue | property date authority before posting cutover |
| Overloaded room status | invalid operational transitions | separate status dictionaries |
| Direct balance edits | irreconcilable financial records | append-only entries and compensations |
| External card retries | duplicate issue/disable events | idempotency key and correlation audit |
| PII overexposure | privacy/security breach | masking, retention, capability controls |
| Broad migration application | unintended database change | narrow reviewed scripts and approval gates |

## 50. Phase 2B Prerequisites

1. Approve Decisions D-01 through D-10, beginning with property, guest, business date, payment, invoice, and AR.
2. Verify the intended runtime database connection and create an evidence-backed schema baseline.
3. Resolve Reservation EF/live schema drift using a reviewed proposal; do not apply a broad migration set.
4. Produce a property/guest/reservation/stay data profile: nulls, duplicates, orphan candidates, and backfill rules.
5. Approve financial ledger, payment allocation, invoice, and AR ownership before enabling live financial writes.
6. Define command transaction, idempotency, rowversion, permission, and audit requirements per workflow.
7. Replace or isolate mock/random sources module-by-module only after an approved live query contract exists.
8. Define reconciliation and rollback/compensation tests for booking, stay, folio, payment, invoice, AR, housekeeping, minibar, lost-found, and key-card workflows.

### Validation Checklist

- [x] Production-readiness report reviewed.
- [x] Phase 1 live DB map reviewed.
- [x] Domain aggregates inspected for Reservation, Stay, Folio, Payment, Settlement, Group, Housekeeping, and LostFound.
- [x] Application contracts/CQRS/handlers inspected where present.
- [x] Infrastructure repositories and dependency-registration evidence reviewed.
- [x] DbContext, entity mapping, key relationships, and concurrency evidence reviewed.
- [x] Applied migration history and current migration-source availability assessed.
- [x] UI/view-model/navigation entry points reviewed for mock/random and capability distinction.
- [x] Source-of-truth, duplicate-authority, ID, property-scope, business-date, transaction, concurrency, audit, permission, snapshot, legacy, gap, and decision analysis completed.
- [x] No code, migration, database, UI, config, or mock-data change was made.

### Hard Stop

Phase 2A ends with this document. No implementation, migration, SQL change, database update, UI change, configuration change, or mock-data modification is authorized by this contract.
