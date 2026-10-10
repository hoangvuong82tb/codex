# Database preflight (read-only) contract

This contract is the safety gate before enabling real Hotel/Front Office data.
It is diagnostic only and must never be used as a replacement for an approved
database deployment plan.

## API

`IDatabaseMigrationService.GetPreflightAsync` returns `DatabasePreflightDto`
with:

- connection status, provider, server, database and schema marker;
- sorted visible-table SHA-256 fingerprint;
- pending and applied EF migration identifiers;
- seed-consistency warnings.

The fingerprint is intentionally a table-list fingerprint. It is not a claim
that columns, keys, indexes, or data quality are already production-ready.

## Safety guarantees

- The method calls `CanConnectAsync`, migration metadata queries, seed checks,
  and a read-only `INFORMATION_SCHEMA.TABLES` query.
- It does not call `Migrate`/`MigrateAsync` and does not execute DDL or DML.
- The runtime database target and fingerprint must be reviewed and approved
  before any migration or seed operation is enabled.
- A failed preflight must block production cut-over; it must not be converted
  into an empty/mock success response.

## Current Hotel/Front Office status

- Group Rooming List read/export now uses the EF-backed service.
- Group Room Block and Group Billing still require real entities and service
  implementations; their current mock paths remain explicitly non-production.
- Group Dashboard now uses an EF-backed read-only projection from group
  reservations and room assignments; no dashboard mutation is exposed.
- Rooming List mutation operations remain fail-closed until transaction,
  authorization, idempotency, and concurrency rules are implemented.

## Verification evidence (2026-08-13)

- `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj --no-restore`: passed with
  zero errors; existing solution warnings remain.
- Scoped unit tests for migration policy and Group workflows: 15 passed,
  zero failed.
- XAML encoding validation: passed with no mojibake patterns.
- No migration, schema write, seed write, or production data write was run.
