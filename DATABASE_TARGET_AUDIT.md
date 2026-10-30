# Read-only database target audit

Inspected on 2026-10-29 at approximately 23:00 Africa/Cairo. No SQL connection was opened. No database was created, migration applied, application configuration changed, or service stopped/started.

## Configured target and precedence

| Source | Finding |
| --- | --- |
| `PTCenter.Api/appsettings.json`, `ConnectionStrings:DefaultConnection` | Server `db56823.public.databaseasp.net`; database `db56823`. SQL credentials are present; their values were neither logged nor included here. |
| `PTCenter.Api/appsettings.Development.json` | Logging settings only; no database override. |
| API Debug and Release output `appsettings.json` | Same server and database as the source configuration. |
| `PTCenter.Api/Properties/launchSettings.json` | Both profiles select Development and local API URLs; neither overrides the connection. |
| Process, user and machine environment variables | No connection override, including ASP.NET connection keys and SQLCONNSTR/SQLAZURECONNSTR/CUSTOMCONNSTR variants. This is the audit process environment, not a claim about a different terminal's private environment. |
| Current user's secrets | Neither `%APPDATA%/Microsoft/UserSecrets` nor `~/.microsoft/usersecrets` exists. No project declares `UserSecretsId`. |
| Application configuration code | Default `WebApplication.CreateBuilder(args)` configuration; no additional database configuration provider found. Infrastructure reads `GetConnectionString("DefaultConnection")`. |

With these inspected settings and no command-line or terminal-specific override, the API resolves to **`db56823.public.databaseasp.net / db56823`**. There is no active backend on ports 7223 or 5013 at audit time, so no live backend target can be independently confirmed. The previously running backend's environment was inaccessible, and it is no longer running. No claim is made about which database it actually used.

The design-time factory and migration-generation test mention `(localdb)\\MSSQLLocalDB / PTCenter_MigrationDesignOnly`. This is a code-only design target, not evidence that the instance or database exists. It does not override the runtime API connection. SQLite test databases likewise provide no SQL Server verification.

## Installed/local database checks

- No SQL-named Windows services or SQL Server process found.
- Neither normal nor WOW6432 SQL Server instance registry key exists. LocalDB installation and current-user instance registry keys are absent.
- SQL Server installation directories in Program Files and Program Files (x86) are absent; `sqlcmd` and `sqllocaldb` are not on PATH.
- Docker and Podman commands are absent. The WSL launcher exists, but distribution listing returns setup/usage output, and the current-user WSL distribution registry key is absent. No usable SQL Server container/distribution was identified.
- Angular remains on localhost:4200, configured for https://localhost:7223/api. No API listener is present on 7223/5013 and no listener was found on the default SQL Server port 1433. These checks cannot rule out a remote instance or an instance configured elsewhere.

## Test-isolation conclusion

**Unestablished.** No inspected configuration, secrets store, installation, or local documentation establishes that the configured remote database is disposable, contains only synthetic/sanitized test data, or is separate from live clinic use. A database name and Development launch profile are not isolation evidence. No patient data was queried to infer its purpose.

## Exact confirmation needed from Mina

Ask Mina:

> Which SQL Server instance and database are reserved for booking integration testing? The checked-in API currently selects `db56823.public.databaseasp.net`, database `db56823`. Is that exact database used by any live clinic, production deployment, shared operational application, or other environment?
>
> Please confirm the designated test database contains only synthetic or appropriately sanitized test data and identify what establishes that separation. Confirm we may apply migration `20261029124607_BookingWorkflow` and create/update synthetic patient, assessment, appointment, invoice and payment records there without affecting live users or downstream systems.
>
> Where is its connection securely configured for this Windows account (configuration file path, user-secrets ID, or environment variable location)? Please provide the server/instance and database name separately, without sending passwords or a credential-bearing connection string in chat. If a terminal-only override was used for the previous backend, identify that source too. If this is a shared test environment, identify any limits on migration timing and test-data cleanup.

Until those answers identify a safe target, do not create a database, apply migrations, start a backend for write testing, or exercise authenticated Angular booking writes. After confirmation, coordinate the backend start/stop with the user, retain the explicit confirmed connection, and use the guarded migration command in `BOOKING_WORKFLOW.md`. The user's willingness to stop the backend later does not establish database safety.

The reproducible local inspection script and sanitized JSON output are in ignored `.tools/audit-db-target.ps1` and `.tools/database-target-audit.json`.
