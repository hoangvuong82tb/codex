# ASMILE BO - Commercial release manifest v72

**Snapshot:** 2026-09-17  
**Status:** release candidate for controlled staging/UAT; production cutover pending external gates.

## Artifact

- Executable: `E:\ASMILE-BO-commercial-publish-20260917-v72\ASMILE.BO.App.exe`
- Runtime: self-contained `win-x64`, Release configuration
- SHA-256: `EAC556E93893475465C77BE0E70EBF4AB7262E3DFC215DCAE9640ADCB9ACABBB`
- Package verifier: `COMMERCIAL_PACKAGE=PASS`, 11 checks, 0 PDB files
- Current manual-check process: v72 executable, responding normally

## Verification

- Solution build: 0 warnings, 0 errors
- Unit tests: 672/672 passed, 0 skipped (including persisted Administrator-role authorization regression)
- SQL Server integration tests: 26/26 passed, 0 skipped (serial run)
- Desktop startup/shell/restaurant smoke E2E against this published executable: 4/4 passed, 0 skipped
- NuGet transitive vulnerability scan: no vulnerable packages in 8 projects
- Startup: `CanConnect=true`, `PendingCount=0`, `AppliedCount=160`
- Runtime error scan: 0 unhandled, disposed-context, concurrent-DbContext,
  fatal, error or invalid-operation matches

## Commercial safety controls

- Release startup is `ValidateOnly` and fails closed on pending migrations.
- Connection strings and provider secrets are externalized; provider secrets are
  protected with Windows DPAPI and never returned to the UI.
- E-invoice issuance fails closed until a real approved provider adapter is
  registered and matching credentials are configured.
- Front Office production routes use persisted EF data; unsupported mutations
  do not fabricate success.
- Group rooming/assignment/guest windows own independent DI scopes and dispose
  them when closed.
- Administrator security seeding persists the `administrator` to
  `Administrator` role link, so a fresh install can authenticate and see
  authorized modules instead of rendering an empty shell.
- Desktop and Start module tiles expose their module title to UI Automation.

## Required sign-off before production

1. Install an approved MISA, VNPT or Viettel adapter and test with approved
   staging credentials.
2. Complete staging/UAT for hotel, group, guest PII, refund/void, e-invoice
   and night-audit workflows.
3. Reconcile folio, payment, VAT and e-invoice totals with Finance.
4. Capture property/role/concurrency/idempotency/audit/export evidence and
   business acceptance.
5. Take a verified `COPY_ONLY,CHECKSUM` backup for the target environment.
