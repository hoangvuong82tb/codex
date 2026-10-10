# Security bootstrap for production

The application does not contain a built-in administrator password. Before the
first production start, provide the bootstrap password through the deployment
environment:

```powershell
[Environment]::SetEnvironmentVariable(
  'ASMILE_BO_INITIAL_ADMIN_PASSWORD',
  '<strong-unique-password>',
  'Machine')
```

The password is used only when the in-process security store creates the
`administrator` account. The first login is marked `MustChangePassword`; the
administrator must change it immediately and the deployment secret should then
be rotated or removed according to the hosting policy.

The database connection must also be supplied outside the shipped application
configuration when deploying to a shared or customer environment:

```powershell
[Environment]::SetEnvironmentVariable(
  'ASMILE_BO_CONNECTION_STRING',
  '<deployment-connection-string>',
  'Machine')
```

On a single Windows machine, the value may also be stored as a DPAPI-protected
value with the `dpapi:` prefix. The application decrypts that value at runtime
using the machine scope; never place the unprotected connection string in the
published folder or in a command-line argument. For managed deployments,
prefer the deployment secret manager and inject `ASMILE_BO_CONNECTION_STRING`
when the application process starts.

Do not commit either value to source control, screenshots, logs, or support
tickets. Keep `Database:MigrationMode` at `ValidateOnly` for normal startup;
run migrations through the controlled release procedure after taking a backup.

The publish profile intentionally excludes `ASMILE.BO.App/appsettings.json` so a
developer's local connection string cannot be shipped accidentally. The
published application includes `appsettings.example.json` only as a reference;
configure the real connection string and bootstrap password in the deployment
environment before the first start.

## Tenant data readiness

Normal queries are isolated to the active `CompanyId`. Rows with a null
`CompanyId` are treated as legacy or unassigned data and are not exposed to
every company. Before enabling multiple companies for an existing database,
take a backup and run a reviewed data-readiness report, then assign each
legacy row to its owning company through a controlled migration. Do not solve
this by enabling unrestricted query filters in the application.
