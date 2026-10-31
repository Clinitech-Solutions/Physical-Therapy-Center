# Approved finance migration application

Applied and verified on **2026-10-31 at 13:29 Cairo time**.

- Authorized scope: only `20261030184822_CourseFinancialAgreements` on `db56823.public.databaseasp.net` / `db56823`. The user confirmed Mina's approval and an existing backup, explicitly overriding the earlier migration restriction for this migration only.
- Live target rechecked: identity `S13`, database `db56823`. History was the repository's first 12 migrations, ending at `20261029124607_BookingWorkflow`. The sole pending migration was the authorized migration. Model/snapshot matched; no partial target schema was present.
- Reviewed [forward SQL](20261030184822_CourseFinancialAgreements.sql), file SHA256 `3922E9CA68EAFC0092C55B017E3D70B5D20473C8497F0C11403396CE25309941`. Fresh EF-generated SQL matched its text exactly. The file includes a UTF-8 BOM; decoded/generated text SHA256 is `BA2B18C2D2658E110C1D2A107090C20D9EEDD7BCCD92F7C7EB99841B91045631`.
- No unexpected destructive changes: no table/column drops, truncation, historical deletes/updates or narrowing conversions. The only DROP removes an `Invoices.AppointmentId` default constraint before relaxing nullability. New foreign keys use NO ACTION. No historical financial backfill runs.
- Executed only the pinned reviewed transactional script after rechecking target/history/hash. No migration to latest, Down migration or unrelated migration was executed.

| Verification | Result |
| --- | --- |
| History | Exact authorized entry present and latest; no pending migrations |
| Missing invoice columns | `CourseAgreementId`, `DeliveredAt`, `TreatmentPlanSessionId` present and nullable |
| Other schema | `Invoices.AppointmentId` nullable; payment/batch metadata and both new finance tables present |
| Existing records | Counts unchanged for Appointments, Assessments, Patients, TreatmentPlans, TreatmentPlanSessions, Rooms, DoctorSchedules, Invoices, Payments and BookingBatches |
| Existing financial values | Invoice total/patient/insurer/paid sums and payment total unchanged |
| New finance tables | TreatmentCourseAgreements: 0 rows; InsuranceClaims: 0 rows |
| Local code against SQL Server | EF dashboard invoice materialization, full dashboard service and billing invoice service passed |
| Running local API | GET `/api/Receptionist/dashboard-stats`: **200**; GET `/api/Billing/invoices`: **200**, on `https://localhost:7223`; JSON shapes verified |

HTTP probes used a short-lived, locally signed JWT for an existing Receptionist with application JWT configuration, held only in memory. This was an authenticated read-only endpoint check, not a password-login or browser test. Response bodies, credentials and tokens were not written to evidence. No `SaveChanges` or financial mutation endpoint was used for verification.

No test financial records, push or deployment. The running main API was preserved. SQL Server financial writes and concurrent booking/payment behavior were deliberately not exercised on this shared database; earlier isolated tests remain separate.

Ignored local evidence: `.tools/CourseMigrationApply/inspect.log`, `inspection.json`, `apply.log`, `before.json`, `after.json`, `verification.json`. Verification timestamp: `2026-10-31T11:29:05.3532855Z` (13:29 Cairo).
