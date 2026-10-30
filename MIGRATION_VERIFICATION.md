# Booking migration verification (2026-10-30)

Applied only `20261029124607_BookingWorkflow` to the approved database after displaying the target and pending migrations and reviewing the generated SQL. The user confirmed Mina's approval and that a backup had been taken.

| Check | Result |
| --- | --- |
| Configured server | `db56823.public.databaseasp.net` |
| Connected SQL Server identity | `S13` |
| Connected database | `db56823` |
| Connection source | API JSON configuration; no credentials displayed or copied into this report |
| Previous latest migration | `20261024151158_AddInsuranceDetails` |
| Pending migrations before execution | `20261029124607_BookingWorkflow` only |
| Latest migration after execution | `20261029124607_BookingWorkflow` |
| Pending migrations after execution | None |
| Reviewed and executed SQL SHA256 | `12D54BE8FB9E9964B221FF7477E22FA6C4B103C7B59E0D9A6151D301ACAB4E2A` |

## SQL review

- No tables or columns are dropped; no existing rows are deleted or truncated.
- Three appointment indexes are replaced by indexes with the same leading key and an additional `StartTime` key. The migration also adds conflict/uniqueness indexes and foreign keys with `ON DELETE NO ACTION`.
- `Appointments.SeniorId` changes from `nvarchar(max)` to `nvarchar(450)`. The live preflight found zero values whose `DATALENGTH` exceeds 900 bytes, including trailing spaces. There was no existing default constraint to remove. The migration also contains its own length guard.
- Eight columns and the `BookingBatches` table are added. Actual attendance and assessment completion remain null for existing records. Insurance funding is backfilled only from existing positive insurer invoice liability; zero appointments qualified on this target.
- Live migration history matched the repository's migration chain, and the EF model matched its migration snapshot.

## Execution and correction

The first attempt encountered SQL Server error 207: the idempotent script's backfill referenced `InsuranceFunded` in the same batch that adds it. Reinspection confirmed no new migration columns or table existed, migration history was unchanged, and all checked row counts were unchanged.

The migration's backfill now uses `EXEC(N'UPDATE ...')`, so SQL Server compiles it after the new column exists. The update condition and financial behavior are unchanged. The corrected SQL was regenerated and reviewed before execution. The executor checked the live target/history and pinned both generated SQL and the saved SQL file to the SHA256 above. It executed the reviewed transaction with `XACT_ABORT ON` and a lock timeout.

The transaction committed successfully. Post-execution verification confirmed all eight new columns and the `BookingBatches` table. Direct calls to the real SQL Server-backed `BillingService.GetAllInvoicesAsync` and `ReceptionistService.GetTodaySessionsAsync` both succeeded, returning empty results on this database. This confirms the previously failing service queries run against the migrated schema; it does not represent a populated booking walkthrough through the browser.

| Existing table | Before | After |
| --- | ---: | ---: |
| Appointments | 0 | 0 |
| Assessments | 0 | 0 |
| Invoices | 0 | 0 |
| Payments | 0 | 0 |
| Patients | 8 | 8 |

## Local booking checks

- Backend: **25 passed, 0 failed, 0 skipped**, including relational booking/payment/attendance rules, authenticated HTTP checks, retry/concurrency checks and SQL Server migration/model/script consistency. The runtime test databases are isolated SQLite databases.
- Frontend: **58 passed across 9 files**, covering booking service workflow, rendered booking controls, booking presentation, schedule, billing, invoice/appointment integration and Senior assessment/dashboard behavior. HTTP requests are intercepted locally.
- The isolated Release build used for execution passed without warnings or errors. Builds/tests used ignored artifacts directories and did not replace DLLs used by the running API.

Backend command:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test PTCenter.Tests/PTCenter.Tests.csproj --artifacts-path .tools/verification --configuration Release --verbosity minimal -p:NuGetAudit=false --logger 'trx;LogFileName=booking-checks.trx' --results-directory .tools/migration-review/test-results
```

Frontend command, from the sibling admin-panel repository:

```powershell
npm test -- --watch=false --include=src/app/core/services/booking-workflow.spec.ts --include=src/app/features/receptionist/bookings/*.spec.ts --include=src/app/features/receptionist/billing/billing.spec.ts --include=src/app/features/receptionist/schedule/schedule.spec.ts --include=src/app/core/services/billing-integration.spec.ts --include=src/app/core/services/appointment-integration.spec.ts --include=src/app/features/senior/assessment-form/assessment-form.spec.ts --include=src/app/features/senior/live-dashboard/live-dashboard.spec.ts
```

Credential-free inspection, reviewed SQL, before/after verification, backend logs and TRX results are retained under ignored `.tools/migration-review/`. No push, deployment, backend restart or synthetic booking/payment writes to the approved database were performed. Provider-specific booking concurrency on SQL Server and the browser walkthrough remain separate from these local checks.
