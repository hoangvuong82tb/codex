# Hotel Management module - production readiness audit

> **Current snapshot — 2026-09-25.** The current release candidate is buildable,
> publishable and verified on a separate E: build volume. It is ready for
> controlled staging/UAT validation, but it is not approved for production
> cutover until an approved e-invoice provider, credentials, financial
> reconciliation and business acceptance evidence are supplied.
>
> Latest package after the e-invoice provider readiness workflow:
> `E:\ASMILE-BO-commercial-publish-20260925-einvoice-readiness-v97`.

## Scope and release decision

This audit covers Hotel Management, Front Office, Group, Guest, Housekeeping,
Workforce, Lost & Found, Night Audit and accounting flows in `D:\ASMILE-BO`.

**Decision:** release candidate for controlled staging; production cutover
pending external/provider and business gates.

**Incremental update — 2026-09-23:** the Folio business-date gap report now
has a controlled backfill workflow. It requires the dedicated `reports.backfill`
permission, an explicit approval reason/reference, property-scope validation,
transactional update, idempotent retry handling and
`FOLIO_BUSINESS_DATE_BACKFILL` audit logging. It remains a controlled staging
capability until accounting signs off the historical rows; it does not remove
the external staging/UAT, e-invoice, load and backup/restore gates below.

The financial-process seam is no longer a no-op: `FinancialProcessService`
loads in-house stays, the stay's folio and the active room-type rate plan in
the current property scope; it posts idempotent room charges by business date
and derives tax/service-charge entries with refreshed folio row versions.
Missing folios, row versions or rate plans fail closed. This service is
registered in the production DI path; the dedicated Night Audit pipeline keeps
its audit-numbered postings and remains the controlled execution path for
Night Audit.

The rate engine now supports editing an existing room-type rate plan, opening /
stopping sales without deleting its history, and uses the same domain calculator
for the preview screen instead of a reduced UI-only rule set. Minibar master data
is persisted through the real item service (sale price, VAT, category, standard
quantity and min/max stock); saving a minibar item now fails closed when those
commercial fields are incomplete. The domain rate calculator also ignores stale
yield floor/ceiling values after yield management is disabled and rejects invalid
calendar, season and discount-rule inputs at the domain boundary.

The Front Office report center also separates `reports.export` from
`reports.export.sensitive`: cashier names are masked in print/PDF/Excel/CSV
unless the sensitive-export permission is explicitly granted, and the export
audit records whether the output was masked or full.

Reservation creation now preserves and validates `RatePlanId`. When the
production rate repository is available, the wizard loads active plans scoped
to the selected property, room type and arrival date; the selected plan is
passed to the command and a missing valid plan blocks confirmation before any
reservation is created. This prevents a confirmed reservation from entering
the financial flow with a cosmetic rate code but no real price source.

## Current data-path classification

| Area | Evidence from the production DI path | Classification |
|---|---|---|
| Reservation, stay, folio | EF repositories/handlers and `AppDbContext` | Real EF path; staging E2E required |
| Payment | `EfHotelPaymentRepository`, payment/allocation/history entities | Real read/write path; financial reconciliation required |
| Guest profile | `EfGuestProfileRepository` search, update, duplicate and merge paths | Real path; privacy-request/history policy remains to be signed off |
| Group dashboard | `EfGroupDashboardService` reads reservations, assignments, stays, folios and payments | Real read model |
| Room block / rooming list | `EfRoomBlockService`, `EfRoomingListService` and `EfGroupRoomAssignmentCommandService` handle create, modify, assign, remove, import, arrival and departure commands | Real EF read/write path with property scoping and audit staging |
| Group billing | `EfGroupBillingService` reads folio and payment totals | Real read path; billing policy needs staging evidence |
| Room / housekeeping | `EfHousekeepingBoardService` plus `IHousekeepingWorkflowService` | Real EF read/write path; authorization, concurrency and evidence policy need staging proof |
| Workforce | `EfWorkforceQueryService` and `EfWorkforceCommandService` | Real people/department, shift/area and today's housekeeping workload paths; account-link coverage remains an operational data-quality gate |
| Lost & Found | `EfLostFoundQueryService` and `EfLostFoundCommandService` | Real property-scoped persistence and history path |
| Minibar | `EfMinibarQueryService` and `EfMinibarCommandService` | Real session/consumption and folio command path |
| Night Audit | `BusinessDateRecord` and `INightAuditFinancialReader` | Real EF path; reconciliation and staging workflow remain |
| E-invoice | `EInvoiceService`, encrypted provider configuration, readiness preflight, queue claiming and fail-closed provider resolution | Real draft/queue/configuration path; issuance is blocked or saved as draft until a real approved adapter and complete active configuration are present |

The normal production DI route must not use generated rows. Mock providers and
designer constructors are limited to design-time, demo and automated tests;
unsupported mutations fail closed.

## Group workflow scope isolation

`GroupRoomingListWindow`, `GroupRoomAssignmentWindow` and
`GroupGuestManagementWindow` now create a dedicated `IServiceScope` and dispose
it on `Closed`. Their production constructors resolve the real room-block,
rooming-list, assignment and guest services from that scope. This prevents
concurrent refreshes or repeated opens from sharing an application-lifetime
`AppDbContext`. Explicit mock constructors remain isolated to test/design use.

## Schema and startup evidence

- Startup policy is `ValidateOnly`, with pending migrations failing closed.
- v72 runtime log reports `CanConnect=true`, `PendingCount=0`,
  `AppliedCount=160`, and EF model columns present in SQL Server.
- Phase 2B6 schema validation passed for reservation/guest/stay, folio
  idempotency, payment/allocation/history, business date and housekeeping
  contracts.
- Provider secrets are stored through the existing Windows DPAPI protector and
  are not returned to the UI.

## Earlier v72 verification baseline

The verified package is:

`E:\ASMILE-BO-commercial-publish-20260917-v72`

- Release solution build: 0 warnings, 0 errors.
- Unit tests: 672/672 passed, 0 skipped, including the persisted
  Administrator-role authorization regression.
- SQL Server integration tests: 26/26 passed, 0 skipped, when run serially.
- Desktop startup/shell/restaurant smoke E2E against the published executable:
  4/4 passed, 0 skipped; full Hotel workflow E2E remains a staging/UAT gate.
- `verify_commercial_package.ps1 -RequireRunningProcess`: `COMMERCIAL_PACKAGE=PASS`,
  11 checks, 0 PDB files.
- Runtime scan after startup: 0 `Unhandled`, `ObjectDisposedException`,
  `DbContext second operation`, `Fatal`, `ERROR` or
  `InvalidOperationException` matches.
- NuGet transitive vulnerability scan: no vulnerable packages reported for
  any of the 8 solution projects.
- This section records the earlier v72 baseline; the latest package and
  regression evidence are recorded below.

The first integration attempt was intentionally parallel with unit tests and
produced three SQL migration/backup timeouts. The authoritative serial rerun
passed 26/26; this is recorded as an environment/concurrency observation, not
as a hidden failure.

## Remaining gates before production cutover

The latest hardening pass also validates Night Audit execution scope: starting
an audit must match the active property context, and failure handling requires
the Night Audit execute permission. The full unit suite remains green at
743 passed, 0 failed, 0 skipped after the Menu/Recipe, group-assignment and
e-invoice readiness hardening.

Concierge SLA tracking is now persisted and operational: service-request
deadlines, first-response timestamps, grouped SLA metrics, and OnTrack/AtRisk/
Breached display are backed by the real `fo.YeuCauDichVu` data. Existing rows
without a deadline use the documented priority-based fallback until they are
reopened or updated.

Concierge state changes are also fail-safe: terminal requests cannot be changed
again, cancellation requires a reason, and assignment requires a department.
The model snapshot is aligned by guarded migration
`20260923130000_AlignCommercialModelSnapshot`, which is safe for databases that
already received the earlier guarded minibar/SLA migrations.

Reservation deposit persistence is now explicit: `EfReservationRepository` maps
the real `fo.DatPhong` columns for guarantee/deposit amount, due date and status,
and the reservation wizard records a pending deposit requirement after a
successful create. The wizard no longer presents cash/card/bank collection as
completed at reservation time; actual payment remains a cashier/Folio action.
The SQL round trip verifies create -> record deposit -> reload, including the
`DepositRecorded` history event.

The complete SQL Server integration suite was rerun after these migration and
minibar changes: **38 passed, 0 failed, 0 skipped**. The minibar test now seeds
the same explicit warehouse, catalog item and stock-balance prerequisites that
the production command validates, so a missing setup fails clearly instead of
being hidden by designer data.

After the rate-engine hardening, the same integration project was rerun again:
**38 passed, 0 failed, 0 skipped**; the Front Office SQL Server class accounted
for **23 passed, 0 failed, 0 skipped**. The Front Office SQL class was rerun
again after the blackout/rule hardening and still passed **23/23**, confirming
that the rate-domain changes did not break the current EF/migration/transaction
path.

The latest rate-engine pass also closes three silent configuration hazards:
`SeasonMultiplier` is now applied as an explicit factor, `Blackout` seasons
return zero and bypass pricing rules, and `HolidaySurcharge` is rejected until
a real holiday calendar is configured. Rate-plan creation and percentage
discount validation now run at the domain boundary, not only in the WPF form.
The UI no longer offers the unsupported holiday rule, so an operator cannot
save a rule that the calculator would ignore.

**Incremental update — 2026-09-24:** the Rate Engine workspace now surfaces
inline success/error feedback, the selected plan and room type, yield
occupancy, and the configured yield adjustment percentage. The editor validates
the yield adjustment range instead of silently accepting an unusable value.
Release build remains clean; full unit suite is **716/716**, the focused
rate/minibar group is **25/25**, and the v34 published executable passed the
commercial verifier (**10 checks, 0 PDB**) plus Hotel Management startup smoke
(**1/1**). This improves operator readiness but does not change the remaining
external-provider and staging/UAT gates below.

**Incremental update — 2026-09-24 (backup safety):** `BackupNowAsync` now
verifies the newly created `COPY_ONLY, CHECKSUM` file with SQL Server before
returning it as completed. `RestoreAsync` performs the same verification before
entering `SINGLE_USER`, so a corrupt/truncated backup fails before the
destructive restore step. The real LocalDB backup test passed **1/1**, backup
unit tests passed **2/2**, and package v35 passed the commercial verifier
(**10 checks, 0 PDB**) plus startup smoke (**1/1**). This closes a code-level
safety gap but does not replace the required environment backup/restore drill.

The latest financial-process change was rebuilt and regression-tested after
the above checks: the full unit suite passed **712/712**, the full SQL Server
integration suite passed **38/38**, and the Front Office SQL class passed
**23/23**. The fresh Release artifact at
`E:\ASMILE-BO-commercial-publish-20260923-financial-posting-v2` passed
`verify_commercial_package.ps1` with **10 checks, 0 PDB files** and the same
verified executable SHA-256
`479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

An E2E smoke test against that exact executable also passed **1/1** for
startup, test sign-in, opening the Hotel Management module, and absence of an
error dialog.

After the reservation rate-plan correction, the final verification is
**713/713 unit**, **38/38 integration** and **23/23 Front Office SQL**. The
fresh package at
`E:\ASMILE-BO-commercial-publish-20260923-rate-plan-ui-v4` passed
the same 10 checks with **0 PDB files** and the same executable SHA-256.

After wiring reservation deposit persistence and the cashier/Folio boundary,
the authoritative rerun is **713/713 unit**, **38/38 integration**, plus the
reservation deposit SQL round trip **1/1**. The package at
`E:\ASMILE-BO-commercial-publish-20260923-commercial-gates-v6` passed
`verify_commercial_package.ps1` with **10 checks, 0 PDB files** and executable
SHA-256
`479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

The subsequent reception-route hardening also protects the stay-profile,
current-guest and arrivals/departures windows from assigning an invisible main
window as WPF owner. The final v9 package
`E:\ASMILE-BO-commercial-publish-20260923-reception-owner-hardening-v9`
passed the commercial verifier (**10 checks, 0 PDB files**) and targeted E2E
smoke test (**1/1**); the full unit suite remained **713/713**.

The Front Office log workspace was then hardened for its five child windows:
shift log, incident log, Lost & Found, VIP notes and shift handover. Each route
now uses a visible owner when available and otherwise centers on screen. The
v10 package
`E:\ASMILE-BO-commercial-publish-20260923-fo-log-owner-hardening-v10`
passed the package verifier (**10 checks, 0 PDB files**) and targeted E2E smoke
test (**1/1**); the full unit suite remained **713/713**.

The wizard also fails closed for financial inputs that are not yet persisted by
the reservation/Folio contract: negotiated-rate switching, discount and VAT
editing are disabled and labeled for Folio/Invoice processing, so a value
cannot be entered and silently discarded.

A fresh Release artifact was published to
`E:\ASMILE-BO-commercial-publish-20260923-rate-minibar` and passed
`verify_commercial_package.ps1` with **10 checks, 0 PDB files**. Its executable
SHA-256 is
`479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

1. Register a real MISA, VNPT or Viettel adapter matching the approved
   provider contract and test it with approved staging credentials. The
   built-in unavailable provider is deliberately fail-closed; source-level
   tests confirm queue claiming, protected provider secrets, adapter
   preference and failed issuance persistence, but they cannot substitute for
   the provider's approved staging endpoint and credentials.
2. Execute staging/UAT for reservation -> check-in -> folio -> payment ->
   e-invoice -> checkout -> housekeeping, plus group rooming, guest PII,
   refund/void and night audit.
3. Verify property scope, role/permission checks, optimistic concurrency,
   idempotent retries, audit/outbox records and export masking.
4. Reconcile opening balances, folio totals, payment allocations, VAT and
   e-invoice totals with finance, then obtain business sign-off.
5. Keep a `COPY_ONLY,CHECKSUM` backup and capture Phase 2B6 validation output
   for each target environment before any approved migration.

## Operational notes

- The D: volume contains historical publish artifacts; the latest Release
  package was built and published on E: with the persisted administrator-role
  seed fix, UI Automation names for module tiles and the financial-process
  hardening described above.
- The source changes are present in the D: worktree and were hash-checked
  against the E: build copy for all three group workflow windows.
- Production issuance must remain blocked until the provider adapter and
  credentials are approved; enabling a fake adapter would invalidate this
  audit.

## Stay Adjustment runtime hardening

The Stay Adjustment workspace was smoke-tested on a real STA/WPF runtime. The
test found and fixed an owner-assignment failure (`Cannot set Owner property to
itself`, followed by the not-yet-visible owner case) affecting the adjustment
window group. Room Move, Extend Stay, Room Move + Extend and Information
Adjustment now use a visible owner only when one is valid, fall back to centered
screen startup, and surface/log an actionable error instead of bubbling to the
generic fatal UI dialog. The final rerun remained **713/713 unit** and
**38/38 integration**.

The release artifact at
`E:\ASMILE-BO-commercial-publish-20260923-stay-adjustment-v7` passed
`verify_commercial_package.ps1` with **10 checks, 0 PDB files** and executable
SHA-256
`479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

## Front Office window-owner hardening

Shared window routes for cashier, telephone center, rate engine and minibar
master data now assign the main window as owner only when it is visible. When
the app is still starting or a workspace has no visible main window, the child
window uses centered-screen startup instead of throwing a WPF owner exception.
The change passed **713/713 unit tests**, Release build, and a targeted E2E
smoke test against the published executable (**1/1**).

The handoff package is
`E:\ASMILE-BO-commercial-publish-20260923-owner-hardening-v8b`; it passed
`verify_commercial_package.ps1` with **10 checks, 0 PDB files** and executable
SHA-256
`479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

## Incremental update - 2026-09-24 (backup settings UI)

The Settings backup page is now connected to the real SQL Server backup service.
`Backup now` calls the real provider and is guarded against duplicate execution;
backup history is loaded from the allowed backup directory; selecting a history
row runs `RESTORE VERIFYONLY ... WITH CHECKSUM` and reports the result. The
service-level hardening remains in place: a backup is not returned as completed
until it passes checksum/header verification, and restore validates before any
`SINGLE_USER` switch.

The UI exposes database restore only as a controlled, permission-gated action.
It requires `Database.Restore`, `Security.RequireAdminConfirmation`, a valid
checksum-verified backup and an explicit destructive-action confirmation. The
service still keeps restore disabled by default through
`Database.AllowDatabaseRestore=false`, so the feature is not silently enabled
in production. File restore remains unavailable until a verified provider and
recovery drill exist.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **716/716**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-settings-backup-v36`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (group guest profile completeness)

The Group Guest Management workspace already loaded the persisted guest profile
when an operator selected a rooming-list guest, but it omitted persisted gender
and date of birth while rebuilding the guest form and summary. Those fields now
come from `GuestProfile.Gender` and `GuestProfile.DateOfBirth`; the placeholder
values remain only when the real profile has no value.

The save path is now consistent with the load path as well: after the real
`GuestProfile` update, the selected rooming-list guest is refreshed from the
persisted profile for gender and date of birth before the form and summary are
rebuilt. This prevents the right-hand card from retaining stale placeholders
after the operator saves an edit.

Evidence:

- Group/Front Office focused unit tests: **30/30**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-group-guest-profile-save-v72`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1** on the
  second run against the same isolated LocalDB after the first cold-start
  attempt exceeded the UIA startup timeout.

## Incremental update - 2026-09-25 (guest profile read contract)

The real `GuestProfileDto` contract now includes the persisted gender and date
of birth fields. `EfGuestProfileRepository` maps them in both search and
single-profile projections, so Guest Inquiry and Group Guest Management use
the same EF-backed read contract. Group profile save also prefers the refreshed
DTO values before falling back to the tracked entity, preventing a stale
summary card after an edit.

Evidence:

- Guest/Group/Front Office focused unit tests: **108/108**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-profile-contract-v73`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Guest Inquiry operational stay detail)

Guest Inquiry now enriches the selected real guest from the persisted Stay
read path. It reads current in-house stays and expected arrivals for the
active property business date, matches by the persisted `GuestProfileId` (with
a guarded name/phone fallback), and renders the operational status, room,
reservation number, arrival/departure dates and current Stay row. If neither
source contains the guest, the UI keeps the profile but explicitly states that
there is no current or same-day arrival; it does not manufacture history,
folio or payment rows.

Evidence:

- Guest/Group/Stay focused unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-operational-detail-v74`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Guest Inquiry list operational enrichment)

The same real Stay enrichment is now applied to every guest on the loaded
page, not only after a row is selected. This lets the visible status badge,
room, reservation number, arrival/departure dates and dashboard counts reflect
current in-house and same-day expected-arrival data. The guest profile search
remains authoritative for identity and financial aggregates; when the Stay
read is unavailable, the profile list remains usable and explicitly reports
that operational status could not be loaded.

Evidence:

- Guest/Group/Stay focused unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-operational-list-v75`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Guest Inquiry folio and settlement detail)

When a selected guest has a real Stay and Folio, Guest Inquiry now loads the
persisted Folio ledger, its invoice and its payment rows through the existing
Front Office repositories. The UI maps only persisted balances, entries,
invoice status and payment methods; absent Folio, invoice or payment records
remain empty rather than being replaced with sample amounts. This closes the
previous gap where the detail tabs looked complete but had no real financial
read path.

Evidence:

- Guest/Group/Stay focused unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-financial-detail-v76`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

The final v77 republish also preserves the operational-read failure message
when the Stay query is unavailable, instead of allowing a later success text
to overwrite it.

- Regression unit tests after the message-path fix: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-financial-detail-v77`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

The final v78 package also maps the selected Stay's persisted `ReservationId`
through `IReservationRepository.GetDtoAsync`, so the reservation tab is backed
by the exact reservation rather than a name search. The resulting detail chain
is profile -> Stay -> Reservation -> Folio -> Invoice/Payment, with empty
sections only when the corresponding real record does not exist.

- Regression unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-reservation-financial-detail-v78`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

The final v79 package also maps the real guest history contract into the Audit
tab (`EventType`, module, description, occurrence time and reference type).
No synthetic audit entries are created; the tab reflects only events returned
by `IGuestProfileRepository.GetHistoryAsync`.

- Regression unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-history-v79`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

The final v80 package maps the persisted `GuestProfileDto.Notes` value into
the Guest Inquiry notes tab. When no note exists, the tab stays empty; no
sample note is inserted.

- Regression unit tests: **46/46**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-guest-notes-v80`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Folio review financial data)

The Folio Review workflow previously read the Folio DTO totals but displayed
the deposit allocation area as “Chưa có API phân bổ đặt cọc”. That was
misleading because the production payment repository already exposes the real
`GetOutstandingAsync` and `GetDepositSummaryAsync` contracts. The window now
loads those summaries for each persisted Folio and uses them for total
charges, payments, outstanding balance, total deposit, applied deposit and
remaining deposit. A per-Folio read failure keeps the real Folio visible but
marks the footer as financially incomplete so an operator cannot mistake a
fallback DTO total for a complete settlement read.

Payer mapping remains explicitly unavailable because there is no persisted
payer-allocation contract in the current data model; the UI continues to mark
that field as needing determination instead of inventing a payer.

- Debug application build: **0 warnings, 0 errors**.
- Targeted regression unit tests: **46/46 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-folio-financial-v81b`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1** after the
  first-run database migration completed; the initial 45-second attempt timed
  out while applying the fresh database migrations, not because of a Folio
  runtime error.

## Incremental update - 2026-09-25 (Proforma Folio export isolation)

The Proforma Folio print and CSV export actions now scope postings, payments,
deposits and refunds to the selected Folio. Previously the active Folio header
could be paired with rows from every loaded Folio, which was a cross-guest data
leak and an accounting correctness issue. The detail tabs and preview already
used the selected `FolioId`; the print/export paths now use the same boundary.

- Debug application build: **0 warnings, 0 errors**.
- Targeted regression unit tests: **46/46 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-proforma-folio-isolation-v82`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Proforma deposit-summary integrity)

Proforma Folio no longer labels every individual deposit ledger row with
synthetic “applied” and “remaining” values. The payment repository's real
deposit summary is now loaded once per Folio and shown at summary level. Each
ledger row is limited to its persisted amount; if the summary cannot be read,
the UI explicitly says that allocation is unknown instead of presenting a
fallback balance. Print and CSV output follow the same rule and remain scoped
to the selected Folio.

- Debug application build: **0 warnings, 0 errors**.
- Targeted regression unit tests: **46/46 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-proforma-deposit-summary-v83`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Proforma deposit tab presentation)

The Proforma deposit tab no longer binds `Applied` and `Remaining` columns to
individual ledger rows after the data model was made fail-closed. Those
properties are Folio-level summary values, not persisted per-deposit
allocations. The tab now shows the real Folio summary first, then ledger-only
rows with code, timestamp, recorded amount and currency. If the summary read
is unavailable, the operator sees an explicit warning rather than blank or
synthetic allocation values.

- Debug application build: **0 warnings, 0 errors**.
- Targeted regression unit tests: **46/46 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-proforma-deposit-summary-v84`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Folio workspace deposit integrity)

The main Folio Workspace had the same presentation defect as the Proforma
screen: every persisted deposit ledger row was reconstructed with
`Applied=0`, `Remaining=Amount` and `Status=Unapplied`, then shown as if those
were persisted allocation facts. The workspace now loads the real
`GetDepositSummaryAsync` result per Folio, shows allocation only at Folio
summary level, and keeps each ledger row limited to its recorded amount,
currency and timestamp. A payment/deposit read failure leaves the real Folio
visible and shows an explicit warning instead of inventing a balance.

- Debug application build: **0 warnings, 0 errors**.
- Focused Folio/Front Office unit tests: **45/45 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-folio-deposit-integrity-v85`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Folio document linkage and stale-read guard)

The main Folio Workspace document tab was previously backed by
`Array.Empty(...)`, so it could never show persisted customer documents even
though the application already had `IBusinessDocumentService`. The tab now
reads documents by the real `GuestProfileId`, shows the persisted title/file
metadata, enables opening only when the physical file exists, and reports
missing service or guest linkage explicitly. Both Folio Workspace and
Proforma now reject a late document-read result when the operator has already
switched to another Folio, preventing cross-guest document rendering.

- Debug application build: **0 warnings, 0 errors**.
- Focused Front Office/Folio unit tests: **60/60 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-folio-documents-deposit-v86`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Stay and current-guest stale-read guard)

Stay Profile and Current Guests now invalidate an in-flight detail load when
the operator selects another row. Previously, timeline, Folio ledger and
customer-document reads could complete out of order and paint data from the
previous selection into the current detail panel. The load-version guard also
checks the selected guest identity in Current Guests before rendering.

- Debug application build: **0 warnings, 0 errors**.
- Focused Stay/Guest/Folio/Front Office unit tests: **123/123 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-stay-guest-stale-read-v87`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Proforma inspector stale-read guard)

Proforma Folio now invalidates in-flight routing, audit-history and document
loads when the operator changes the selected Folio. Each result is committed
only when both the load version and active Folio still match; this prevents a
late response from a previous guest from replacing the current inspector
content. The same guard is used when opening Routing or History directly.

- Debug application build: **0 warnings, 0 errors**.
- Focused Banquet/Report/Front Office reporting unit tests: **92/92 passed, 0 skipped**;
  the previous Folio/Stay/Guest/Front Office gate remains recorded below at
  **124/124 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-banquet-report-stale-read-v90`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Group checkout stale-read guard)

Group Check-out now protects the selected group's billing summary, service
orders, deposits and payment reconciliation from out-of-order responses. Each
async result is committed only while the same group id and detail-load version
remain active; stale responses exit without rendering or changing the current
summary.

- Debug application build: **0 warnings, 0 errors**.
- Focused Group/Checkout/Stay unit tests: **87/87 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-group-checkout-stale-read-v89`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Banquet and Report Center stale-read guard)

Banquet event detail loading now carries a monotonically increasing load version
in addition to the selected event id. When users change events rapidly, a late
`GetEventDetailAsync` response is discarded before it can replace services,
deposits, contracts, menus, timeline or checklist collections for the current
event.

The Report Center ViewModel now applies the same protection to report parameter
and history loading. A late parameter response from a previously selected
report cannot repopulate the current report's controls or reset its loading and
error state. The existing Front Office report window already had an equivalent
guard and was verified unchanged.

- Debug application build: **0 warnings, 0 errors**.
- Focused Banquet/Report/Front Office reporting tests: **92/92 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-banquet-report-stale-read-v90`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (group room-assignment stale-read guard)

Group room assignment now protects persisted rooming-list and room-availability
loads with a monotonically increasing load version plus the selected group id.
When reception changes groups or refreshes the assignment workspace while an
older query is still running, the late result is discarded before it can
replace guests, rooms, assignments, room blocks, status text or error dialogs
for the current group. The post-save reload participates in the same guard.

- Debug application build: **0 warnings, 0 errors**.
- Focused group-assignment, group billing/folio, Menu/Recipe, Banquet and
  Report Center tests: **112/112 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-group-assignment-stale-read-v92`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (Settings detail stale-read guard)

Settings detail loading now carries a monotonically increasing version across
definition reads, value reads and the appearance icon-picker load. If an
operator switches settings routes quickly, a late response from the previous
route is discarded before it can replace the current editable items, scope
maps, installed-app list or dirty-state reset.

- Debug application build: **0 warnings, 0 errors**.
- Focused settings/backup tests: **15/15 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-settings-stale-read-v93`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Full desktop E2E smoke suite against v93: **5/5 passed, 0 skipped**.

## Incremental update - 2026-09-25 (backup verification route hardening)

The Settings > Backup page now handles the `backup-verify` landing route
explicitly. Clicking “Verify backup” opens the real backup history/detail
workspace instead of falling through the generic settings route, so the
operator can select a persisted `.bak` record and run the existing
`RESTORE VERIFYONLY WITH CHECKSUM` action.

The database restore card remains intentionally fail-closed. It now reports
whether the current operator has `Database.Restore`; even with that permission,
the destructive restore path remains unavailable until the safety configuration,
admin confirmation and staging restore drill are approved. File restore remains
unavailable because no verified commercial provider exists yet.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Focused Settings/Backup tests: **9/9**.
- Full unit suite: **741/741**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-backup-verify-route-v94b`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Full desktop E2E smoke suite against v94b: **5/5 passed, 0 skipped**.

## Incremental update - 2026-09-25 (controlled database-restore workflow)

The Settings backup workspace now exposes a real restore action for persisted
backup records instead of leaving the service contract disconnected from the
operator workflow. The action is gated by `Permission.RestoreDatabase` and
`Security.RequireAdminConfirmation`, verifies the selected backup checksum
before showing the destructive confirmation, calls the real
`IBackupService.RestoreAsync`, and records `RestoreRequested`,
`RestoreCompleted` or `RestoreFailed` through the security audit service.
Concurrent backup/restore actions share the existing operation guard.

The service remains fail-closed when `Database.AllowDatabaseRestore` is false,
when the file is outside the allowed backup directory, or when SQL Server
verification fails. The UI never performs a restore during automated tests;
the final production gate remains an approved staging restore drill and
recovery sign-off.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Focused Settings/Backup/Security tests: **12/12**.
- Full unit suite: **741/741**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-restore-workflow-v95`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Full desktop E2E smoke suite against v95: **5/5 passed, 0 skipped**.

## Incremental update - 2026-09-25 (e-invoice provider readiness preflight)

The e-invoice wizard now checks the real provider path before presenting an
issuance action. Readiness requires all of the following: a non-sentinel
provider adapter registered in DI, an active provider configuration, a valid
endpoint, tenant, username and protected secret. The UI never exposes the
secret. When readiness is incomplete, the operator can save a real invoice
draft for later processing, but the application does not enqueue it or claim
that it was issued.

The readiness contract is shared by the application service and the wizard,
and reports a safe status/message for missing adapter versus incomplete
configuration. The review card also no longer clears the selected Folio when
a validation check fails, so the operator can correct the form and retry.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- E-invoice readiness/service tests: **16/16 passed**.
- Full unit suite: **743/743**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-einvoice-readiness-v97`:
  verifier **PASS, 10 checks, 0 PDB files**; desktop smoke suite **5/5
  passed, 0 skipped** against this exact package.

## Incremental update - 2026-09-25 (authoritative commercial regression)

The latest release candidate was rechecked after the e-invoice provider
readiness work. The complete unit suite, the complete SQL Server integration
suite and the existing desktop smoke suite all pass. The SQL suite includes
the persisted Front Office reservation, stay, folio, payment, housekeeping,
group and minibar workflows; this is evidence of the real persistence path,
not a mock-only screen check.

- Full unit suite: **743/743 passed, 0 skipped**.
- Full SQL Server integration suite: **38/38 passed, 0 skipped**.
- Full desktop E2E smoke suite: **5/5 passed, 0 skipped** against the exact
  published executable.
- Commercial package verifier: **PASS, 10 checks, 0 PDB files**.
- Exact package used for this release candidate:
  `E:\ASMILE-BO-commercial-publish-20260925-einvoice-readiness-v97`.

This closes the repository-side regression loop for the current candidate. It
does not replace the external production gates listed above: an approved real
e-invoice adapter and credentials, staging/UAT with business sign-off,
financial reconciliation, and the approved backup/restore drill are still
required before production cutover.

The release-candidate navigation audit also compared the visible desktop
catalog with the production resolver: all 34 visible module routes have an
explicit resolver branch. The placeholder view remains only as a safe fallback
for unknown or stale saved routes; it is not used by the visible Hotel,
Front Office, pricing or minibar entry points.

Focused navigation/property-scope verification: **40/40 passed**. Focused
pricing/minibar verification after this audit: **30/30 passed**
(rate-plan, item/minibar commercial fields, minibar persistence and restock
flows).

## Incremental update - 2026-09-25 (Menu/Recipe selection stale-read guard)

Menu/Recipe selection now uses a monotonically increasing load version. A
second selection is no longer dropped merely because the first detail request
is busy; the latest selected menu card wins. Late item-detail, recipe, cost
preview and history results are ignored when they belong to an older
selection, so the editor, recipe lines, cost preview and history cannot be
rendered for the wrong card.

Save flows keep their existing behavior: the optional selection version is
only supplied by the selection path, while create/edit/save operations may
continue to refresh their own recipe cost preview normally.

- Debug application build: **0 warnings, 0 errors**.
- Focused Menu/Recipe, Menu Catalog, Banquet and Report Center tests:
  **98/98 passed, 0 skipped**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-menu-recipe-stale-read-v91`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (receivable reconciliation data integrity)

The Receivable reconciliation-list dialog was still presenting a hard-coded
monthly history (fixed 2026 dates, amounts, and reviewer names). That was a
production-visible mock because `ReceivableViewModel.OpenReconciliationSendModalAsync`
opens the dialog from the AR workspace.

The dialog now maps only the `ReceivableSummaryDto` rows supplied by the real
`IReceivableService`. A selected customer filters the same persisted summary
set; it no longer creates synthetic monthly history. The system balance is
shown as the persisted closing balance, while the customer-confirmed balance,
difference, and reviewer remain blank/explicitly `Chưa đối chiếu` until a real
customer confirmation source is implemented. The grid headers were renamed to
distinguish system balance from customer confirmation.

Evidence:

- `ReconciliationListDialog.BuildRealRows` is the single mapping path for the
  dialog and has no generated dates, random values, or sample customer names.
- `ReconciliationListDataTests` verifies persisted customer/closing-balance
  mapping, selected-customer filtering, and the fail-closed unconfirmed state.
- Focused unit test: **2/2 passed**.
- The full unit command was stopped after an extended no-output period; the
  prior baseline had the same long-running test behavior, so this change is
  not reported as a full-suite pass.

## Incremental update - 2026-09-25 (receivable reconciliation action routing)

The AR button labelled `Tạo đối chiếu` was incorrectly wired to
`PrintAsync()`, so the operator received a print preview instead of an
operational reconciliation view. The action is now labelled `Xem đối chiếu`
and opens the real read-only transaction detail for the selected customer.
When no customer is selected, it stops with an explicit instruction instead
of opening an empty or misleading dialog.

This is intentionally fail-closed: the current backend contract has persisted
receivable transactions and summaries, but no persisted customer-confirmation
entity. The UI therefore does not claim that a customer reconciliation has
been created or confirmed. A future write workflow must first add that
backend contract, audit fields, and approval semantics.

The same fail-closed rule was applied to Inventory “Đề xuất nhập hàng”, which
now explains that the inventory service has no purchase-suggestion command
instead of claiming that a request was created. AR “Gửi nhắc nợ” was then
completed in the next tranche below after adding a persistent customer email
source.

## Incremental update - 2026-09-25 (customer contacts and AR reminder outbox)

Customer `Email` and `TaxCode` were previously DTO-only fields. They are now
persisted on `Customers` with an EF migration, mapped through the customer
application service, editable in the customer detail card, and supported as
optional trailing columns in customer import/export. Existing seven-column
customer files remain compatible.

Receivable summaries now carry the persisted customer email. “Gửi nhắc nợ”
validates the overdue set, skips customers without an email, and queues one
real outbox message per valid recipient through `IEmailDispatchService`; the
background dispatcher remains responsible for SMTP delivery. Invalid or
missing contacts are reported explicitly and never treated as sent.

Evidence for this tranche:

- EF migration `20260925010857_AddCustomerContactFields` was generated and
  applied successfully by the LocalDB migration integration test.
- Customer/contact focused tests: **5/5**.
- LocalDB migration durability test: **1/1**.

## Incremental update - 2026-09-25 (inventory low-stock data integrity)

Inventory low-stock alerts no longer display the synthetic supplier label
`NCC gần nhất`. Stock balances now carry the supplier code and name persisted
on the item master. Alerts show the configured supplier, the supplier code when
only the code exists, or the explicit state `Chưa cấu hình nhà cung cấp` when
the item has no supplier assignment.

The low-stock threshold also no longer uses a hard-coded value. The service
reads the persisted `Warehouse.LowStockThreshold` application setting and
uses the item-level `MinibarMinStock` and `MinibarMaxStock` for configured
minibar items. The suggested purchase quantity is the gap to the configured
target stock, not an arbitrary 20% uplift. If the setting is missing or
invalid, it falls back to the registered default of 20, not to a misleading
supplier or stock value.

The inventory “Gửi cảnh báo email” action now uses the persisted
`Warehouse.LowStockAlertRecipients` setting, validates each recipient and
queues real messages through `IEmailDispatchService`. Missing recipients or
queue failures are reported explicitly; the former simulated success message
has been removed. The “Khóa kỳ tồn kho” action is also fail-closed until a
persisted period-lock command and audit contract are available.

Evidence:

- Release/debug application build after the tranche: **0 warnings, 0 errors**.
- Inventory/minibar/settings/email focused unit tests: **24/24**.
- Release package
  `E:\ASMILE-BO-commercial-publish-20260925-inventory-email-v70`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (commercial UI fallback hardening)

The defensive settings-detail fallback no longer exposes developer-facing
`TODO` text or a fake clickable surface. Unknown/dynamic rows are now shown as
an explicit read-only status with the existing title/value, while all known
settings routes continue to use the persisted Settings Engine mappings.

Evidence for this tranche:

- Settings configuration tests: **7/7**.
- Full unit suite: **739/739**, 0 skipped.
- Production-code scan: no remaining `TODO` marker in App/Application/
  Infrastructure sources (the `ToDomain*` identifiers are unrelated symbols).
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-commercial-ui-hardening-v64`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- The first smoke attempt timed out waiting for the main window in the existing
  LocalDB/process environment; a retry with a fresh isolated database passed
  **1/1** against the exact published v64 executable.

## Incremental update - 2026-09-25 (e-invoice endpoint transport hardening)

Provider configuration now rejects non-loopback `http://` endpoints before a
provider secret can be persisted. External MISA/VNPT/Viettel endpoints must
use HTTPS; loopback HTTP remains available only for local/UAT harnesses. This
hardens the configuration boundary without pretending that an external
provider adapter is installed.

Evidence for this tranche:

- E-invoice service tests: **14/14**.
- Full unit suite: **739/739**, 0 skipped.
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-einvoice-transport-v63`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (settings app-scope targets)

The editable settings loader now resolves definitions across `System`, `User`
and `App` scope, then preserves the resolved scope and target when reading and
saving values. App-scoped POS/FO endpoints use stable `pos-client` and
`fo-client` targets; voucher, warehouse and general-ledger settings use their
corresponding app targets. This closes the gap where an editable route existed
but its persisted App-scoped value was not loaded or saved.

Regression coverage now verifies all six App-target mappings in addition to the
route-to-definition registry check.

Evidence for this tranche:

- Settings configuration tests: **7/7**.
- Full unit suite: **737/737**, 0 skipped.
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-settings-scope-v62`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (canonical report route)

`OpenReportAsync` now always resolves to the single `report-center` workspace.
Tax, financial, inventory and compliance report codes no longer open a
different legacy module, so saved report links cannot recreate duplicate report
screens. Regression coverage includes representative ledger, VAT, inventory
and TT200 codes.

Evidence for this tranche:

- Navigation tests: **6/6**.
- Full unit suite: **731/731**, 0 skipped.
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-report-route-v61`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (settings route persistence)

The system settings landing pages now use the persisted Settings Engine for
their editable paths instead of falling through to a disabled `TODO` card.
Database, network/API, backup schedule, virtual drives, security/audit and
appearance routes are mapped to existing validated definitions; POS and FO
client endpoint routes are also backed by their persisted app settings. Routes
without a real definition remain action/status pages and are not presented as
editable data.

A regression test verifies that every editable route points only to registered
settings definitions, with no duplicate keys or orphaned definitions.

Evidence for this tranche:

- Full unit suite: **726/726**, 0 skipped.
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-settings-routes-v60`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (complete report fallback catalog)

The Report Center fallback catalog now exposes every report code handled by
`ReportEngine`, including stock card/stock alerts/cost analysis, cash and bank
books, fixed assets and prepaid expenses, plus TT99 and TT200 compliance
reports. This prevents a clean or partially-seeded database from silently
showing only the smaller accounting subset. A regression test compares the
fallback catalog with the supported-code set and rejects duplicates or missing
entries.

The housekeeping dashboard persistence test was also stabilized by keeping its
fixture on the same UTC calendar day as the production `completed today`
calculation; no production date behavior was changed.

Evidence for this tranche:

- Full unit suite: **725/725**, 0 skipped.
- Commercial package
  `E:\ASMILE-BO-commercial-publish-20260925-report-catalog-v59`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-25 (item/minibar commercial import)

The item master import/export now carries the persisted commercial and minibar
setup alongside the core catalog fields: retail price, VAT, minibar flag,
minibar category, standard quantity, and minimum/maximum stock. The importer
accepts invariant and Vietnamese numeric formats, defaults blank optional
minibar fields safely, and reports row-level parsing failures. Existing
seven-column item files remain compatible, with their legacy active column
still read at the old position. This closes the bulk-setup gap where the item
form and database already supported minibar fields but CSV/Excel operations
silently dropped them.

Evidence for this tranche:

- Release publish: **0 warnings, 0 errors**.
- Focused item/minibar tests: **11/11**.
- Full unit suite: **721/721**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-item-minibar-v53`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke: **1/1**.

## Incremental update - 2026-09-25 (report navigation cleanup)

The desktop module catalog now exposes only `report-center` for reporting.
The former `stock-report` and `debt-report` entries were duplicate entry points
to the same ReportCenter view and have been removed from the visible catalog
and recommended shortcuts. Their route aliases remain accepted by the resolver
so existing saved shortcuts continue to open the canonical report center.
Report-code routing also returns `report-center` directly, preventing new
duplicate report modules from being created by navigation.

Evidence for this tranche:

- Navigation catalog test: **1/1**.
- Full unit suite: **722/722**, 0 skipped.
- Release publish: **0 warnings, 0 errors**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-report-cleanup-v54`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke: **1/1**.

## Incremental update - 2026-09-25 (rate/minibar setup entry-point regression)

The Hotel Management settings hub keeps the two operational setup cards wired to
their real workspaces: `Khung giá & kiểm tra giá` opens the persisted rate-engine
workspace and `Danh mục hàng minibar` resolves the registered item-master
view-model. A regression test now guards both card IDs and their commands, so a
future navigation cleanup cannot leave either setup area as a dead mock tile.

Evidence for this tranche:

- Focused setup-entry-point regression: **1/1**.
- Full unit suite: **723/723**, 0 skipped.
- Release publish: **0 warnings, 0 errors**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-setup-entrypoints-v55`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke on an isolated LocalDB:
  **1/1**. The first shared LocalDB attempt was rejected by stale test login
  state; it was not used as release evidence.

## Incremental update - 2026-09-25 (setup DI registration guard)

The setup-entry-point regression now verifies the composition root as well as
the visible cards: `RateEngineViewModel`, `ItemsViewModel`, and
`MinibarViewModel` must all be registered by `AddPresentation`. This protects
the runtime path behind the two settings cards from silently degrading to the
unavailable-state message when navigation is refactored.

Evidence for this tranche:

- Focused Front Office hub/DI tests: **27/27**.
- Full unit suite: **724/724**, 0 skipped.
- Release publish: **0 warnings, 0 errors**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-setup-di-v56`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke on an isolated LocalDB:
  **1/1**.

## Incremental update - 2026-09-25 (Front Office operational-log scope lifetime)

The three database-backed FO log windows now create a dedicated DI scope for
their lifetime: Incident Log, Shift Log, and Shift Handover. Their scoped
repositories/services and `AppDbContext` are no longer resolved from the root
provider. The scope is released when the window closes, while the existing
single-window reuse behavior remains unchanged. This closes a real runtime
failure seam behind stale data and concurrent-operation errors when multiple FO
workspaces are opened during a shift.

Evidence for this tranche:

- App build: **0 warnings, 0 errors**.
- Focused Incident/Shift Log/Cashier tests: **9/9**, 0 skipped.
- Full unit suite: **743/743**, 0 skipped.
- The previously verified SQL Server Front Office integration suite remains
  **38/38**; this change is presentation-scope lifetime hardening only.
- Release build: **0 warnings, 0 errors**.
- Commercial package: `E:\ASMILE-BO-commercial-publish-20260925-fo-log-scope-v98`.
  Package check: executable present, **0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.

## Incremental update - 2026-09-25 (Front Office workspace scope lifetime)

The rate-engine and minibar item-master entry points now create a dedicated DI
scope per window. Their scoped application services and EF repositories are no
longer resolved from the root service provider; the scope is disposed when the
window closes, while an already-open window is still reused. Initialization
failures are surfaced through the existing unavailable-workspace message.

Evidence for this tranche:

- Full Front Office hub tests: **27/27**.
- Full unit suite: **724/724**, 0 skipped.
- Release publish: **0 warnings, 0 errors**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-fo-scope-v57`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke on an isolated LocalDB:
  **1/1**.

## Incremental update - 2026-09-25 (Front Office search/telephone scope lifetime)

The reservation-search workspace and telephone-center workspace now follow the
same per-window scope rule as rate/minibar setup. `ReservationWorkspaceViewModel`
and `TelephoneCenterViewModel` are resolved from a child scope rather than the
root provider, and that scope is released when the window closes. This avoids
scoped rate repositories, telephone services, and EF-backed dependencies leaking
across windows or failing when scope validation is enabled.

Evidence for this tranche:

- Full Front Office hub tests: **27/27**.
- Full unit suite: **724/724**, 0 skipped.
- Release publish: **0 warnings, 0 errors**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260925-fo-scope-v58`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke on an isolated LocalDB:
  **1/1**.

## Incremental update - 2026-09-25 (scoped DI integration guard)

The DI regression coverage now resolves the rate engine, item master, minibar,
reservation-search, and telephone-center workspaces inside a validated child
scope. This confirms the concrete composition path required by the new
per-window scope handling, rather than checking registrations only by service
descriptor.

Evidence for this tranche:

- Front Office and scoped-DI tests: **28/28**.
- Full unit suite: **724/724**, 0 skipped.
- The runtime package is the already verified `v58` publish above; no app
  binary changed in this test-only guard.

## Incremental update - 2026-09-24 (POS timeline and service KPI)

The Breakfast/Cafe closed-order report no longer presents a fixed 12-minute
service KPI or synthetic timeline timestamps. Payment creation timestamps and
the persisted order update timestamp are now carried through the DTO mapping;
the average is calculated only from paid orders with a real payment record.
When historical data has no usable payment timestamp, the UI reports no
measured duration rather than inventing one.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **717/717**, 0 skipped.
- Front Office E2E reservation -> check-in -> folio -> invoice -> checkout -> housekeeping: **1/1**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-pos-timeline-v44`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (POS posted inventory cost)

The Breakfast/Cafe sales summary now reads the posted `StockIssue` totals
created by recipe consumption for each paid POS order. `EstimatedCostAmount`
is no longer a hard-coded zero: only approved inventory issues whose number is
linked to the order are included, while orders without a posted recipe issue
remain explicitly uncosted instead of receiving a fabricated value.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **717/717**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-pos-cost-v45`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (rate component activation)

The Rate Engine can now activate or deactivate a saved season/event or pricing
rule without deleting its history. The action reloads the real tracked rate
plan, persists the status change, and refreshes the selected plan. This closes
the operational gap where an operator could add a configuration but could not
stop a promotion or seasonal multiplier safely.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **717/717**, 0 skipped, including the saved component
  activation regression.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-rate-component-toggle-v47`:
  verifier **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (rate preview correctness)

The Rate Engine price calculator no longer reconstructs its preview aggregate
with an empty property identifier. The preview now requires the active property
context, so it follows the same scope contract as the persisted rate plan and
cannot silently calculate outside a property. It also rebuilds the preview from
only active seasons and rules; disabling a saved promotion or seasonal event
therefore changes the preview immediately while preserving the configuration
history. Calculation exceptions are surfaced inline instead of escaping from a
UI command into a generic application error dialog.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Focused Rate Engine, Minibar and item-commercial setup tests: **33/33**.
- Full unit suite: **719/719**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-rate-preview-v51`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke: **1/1**.

## Incremental update - 2026-09-24 (Workforce operational workload)

The Workforce query now joins the canonical `People` directory to the active
`AppUser` account used by housekeeping task assignment, using the same
person-code/name/email matching contract as the assignment workflow. For the
current UTC operating day it reads real `HousekeepingTasks` and exposes total
assigned, completed, in-progress, waiting-inspection, VIP/rush counts and
average cleaning minutes. Cancelled tasks are excluded. No KPI score or
capacity is inferred from these counts; personnel without a linked operational
account remain visible with zero workload and an explicit linkage flag in the
read contract. The same read contract now carries today's assignment rows, so
the detail panel shows the real room number, priority and task state rather
than an empty list; unknown duration is displayed as `Chưa có SLA` instead of
an invented estimate. Inspection Pass/Fail now comes from the persisted task
inspection result; SLA counters remain zero until a persisted due-at/SLA
contract exists.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **719/719**, 0 skipped, including a persisted task joined
  through the operational account.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-workforce-kpi-v50`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable Hotel Management startup smoke: **1/1**.
- Staging must still validate account-link coverage across all active personnel
  before production cutover.

## Incremental update - 2026-09-24 (cashier allocation race and amount validation)

`CashierPaymentWindow` now cancels stale allocation reads whenever the selected
Folio changes, the list refreshes, or the window closes. A background result is
applied only when its Folio and load version are still current. This prevents a
previous Folio's allocations from appearing in or affecting the current
payment flow.

The payment amount parser now handles common Vietnamese/invariant separators,
and the final confirmation button stays disabled for unsupported payment
methods, invalid Folio state, non-positive amounts, or amounts above the
current debt. The service-level guard remains in place as the final authority.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **716/716**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-cashier-allocation-v42b`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management on an isolated smoke
  database: **1/1**.

## Incremental update - 2026-09-24 (invoice receivable summary)

The Invoice Center now reads the linked `ReceivableTransaction` summary for
each invoice. The real debit, credit, outstanding balance, and receivable
status are exposed through `InvoiceSummaryDto` and rendered in the payment
inspector. When no receivable link exists, the UI says so explicitly instead
of presenting a generic static payment placeholder.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **717/717**, 0 skipped.
- EInvoice service unit suite: **12/12**.
- SQL Server invoice schema integration: **1/1**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-invoice-ar-v43`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management on an isolated smoke
  database: **1/1**.

## Incremental update - 2026-09-24 (rate-plan posting safety)

Room-charge posting no longer falls back to a base rate supplied by the UI when
no active persisted rate plan matches the stay. The operation now fails closed
and identifies the room type/date that needs rate configuration. A persisted
property-wide plan is supported as a governed fallback, but an exact room-type
plan is preferred. The calculator also rejects preview dates outside the
selected plan's effective window.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Rate Engine tests: **23/23**.

## Incremental update - 2026-09-24 (post-create group actions)

The successful group-creation screen no longer routes six real-looking buttons
to a placeholder warning. The actions now open the existing real group workspaces
for group profile/list, room assignment, rooming list and group services. The
room-assignment window receives the newly-created `GroupReservationId`; creating
another group starts a fresh wizard and closes the completed one.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Full unit suite: **716/716**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-group-actions-v40`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (group confirmation overbooking gate)

The availability result now includes active group room blocks (`Tentative` and
`Confirmed`) for the same room type and overlapping stay dates, less picked-up
rooms. A group can exclude its own reservation id while revalidating, so its
existing block does not falsely conflict with itself.

The command service revalidates every room block immediately before confirming
both booking configuration and a later `ConfirmAsync` transition. If inventory
is no longer sufficient, the operation fails with a business error and does not
commit the confirmation. The confirmed path now runs inside a SQL Server
`Serializable` transaction, including the availability read and persistence
commit. A final UAT drill should still exercise two simultaneous confirmations
against the production-like SQL topology.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Group workflow tests: **15/15**.
- Full unit suite: **716/716**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-group-availability-v38`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- SQL Server Front Office integration suite: **23/23**.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (serializable group confirmation)

The confirmed group-booking paths now open a SQL Server `Serializable`
transaction before the availability recheck and keep it through the persistence
commit. This prevents the availability read and the room-block confirmation
from being separated into independent transactions. The existing persistence
session detects the ambient transaction and does not create a second one.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Group workflow tests: **15/15**.
- Full unit suite: **716/716**, 0 skipped.
- SQL Server Front Office integration suite: **23/23**.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-group-concurrency-v39`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.

## Incremental update - 2026-09-24 (group room availability)

The group reservation wizard no longer presents raw physical-room inventory as
if it were sellable availability. `CreateGroupWindow` now calls the real
`IAvailabilityEngine` for each requested room type and date range. The result
subtracts overlapping pending-arrival/in-house stays and assigned confirmed,
guaranteed or checked-in reservations before reporting the remaining count.

The inspector is fail-closed: it shows `Chưa kiểm tra` until the operator runs
the availability check, shows `Đủ phòng theo tồn thực` or
`Thiếu phòng theo tồn thực` after the check, and invalidates the old result when
the group or room requirements change.

Evidence for this tranche:

- Release build: **0 warnings, 0 errors**.
- Group workflow tests: **15/15**.
- Full unit suite: **716/716**, 0 skipped.
- Published package
  `E:\ASMILE-BO-commercial-publish-20260924-group-availability-v37`: verifier
  **PASS, 10 checks, 0 PDB files**; executable SHA-256
  `479FA07279E363BFEFC27529FD80248998E2A08D810075C2F8FA0F0AE4A63A66`.
- Published executable smoke test opening Hotel Management: **1/1**.
