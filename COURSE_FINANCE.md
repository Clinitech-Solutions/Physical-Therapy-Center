# Treatment course finance

Treatment pricing now belongs to a `TreatmentCourseAgreement`, unique per saved treatment plan. Reception cannot change the prescribed session count. A subsequent individual or bulk booking reuses the agreement and session invoices. Booking batches retain their retry identity and appointment links; they do not create another course liability.

Self-pay supports PerSession and Package. Package gross is prescribed count × unit price; one Fixed or Percent discount applies to the full course. PerSession applies the discount to each session and invoices only booked sessions. Insurance uses Package, freezes the saved company, approved count and copay, and never applies a discount. Other agreements reserve covered approval capacity so the same approval cannot fund multiple full courses. A changed funding/company/copay/limit requires review of the existing agreement.

Package invoices are created for all covered course sessions, with nullable appointment links. Currency uses decimal cents. Allocation divides integer cents and assigns remainder cents to earlier session numbers. Patient and insurer allocations sum exactly to the agreed total. Cancelling an unstarted appointment releases the appointment/session reservation, retaining the course invoice and actual receipts for reuse. It does not erase an agreed charge or consume start coverage.

## Starting and collecting

`GET /api/Finance/appointments/{id}/payment-gate` reports actual patient receipts, cumulative required coverage, exact shortfall and patient balance. Package coverage is the sum of distinct consumed session liabilities plus the upcoming session, irrespective of appointment order. PerSession requires that specific invoice's patient share. Zero copay passes without patient payment.

Check-in evaluates the gate inside the same Serializable transaction as actual occupancy and status updates. Actual check-out stamps service delivery. New receipt operations also use Serializable transactions and unique retry keys. SQL Server deadlock/lock-timeout victims return a concurrency conflict; retry/reconcile the original operation key rather than creating a new payment intent.

`POST /api/Finance/appointments/{id}/payments` records a patient receipt and returns the refreshed gate. Recording payment does not start the session: start remains an explicit action. Course account collections remain available through the existing Booking accounts endpoint. Patient receipts never settle insurer liability.

## Insurers and audit

`GET /api/Finance/insurance/accounts` groups planned liabilities, delivered-service earnings, submitted claims, actual insurer receipts and earned outstanding balances by insurer and patient. Individual rows link invoice, course, treatment session, approval snapshot/hash and submitted claim.

`POST /api/Finance/insurance/claims` records an actually submitted external claim reference for a verified delivered session. It does not transmit an external claim. `POST /api/Finance/insurance/claims/{id}/settlements` records actual insurer money with its collection method and external settlement reference. Payments link to their claim and invoice and have a distinct insurer payer marker. Direct legacy `Insurance` invoice collections are rejected in favour of this audited route. Historical payments remain readable and unchanged.

Compatible historical session charges can be attached to the course without changing amounts or payment records. Ambiguous/mismatched charges return `LegacyAgreementReviewRequired` and create nothing. Legacy delivered services without verified delivery/approval links remain visible in insurer balances but require review before claim submission.

## Migration — prepared, not applied

New migration: `20261030184822_CourseFinancialAgreements` (after `20261029124607_BookingWorkflow`).

Reviewed forward SQL: [Database/ReviewedMigrations/20261030184822_CourseFinancialAgreements.sql](Database/ReviewedMigrations/20261030184822_CourseFinancialAgreements.sql).

The forward script adds agreement/claim tables, nullable invoice/batch/payment metadata, indexes and restrictive foreign keys, and allows `Invoices.AppointmentId` to be NULL for future course sessions. Its only DROP removes an AppointmentId default constraint as part of relaxing nullability. There are no table/column drops, DELETEs, TRUNCATEs, or updates of existing charge/payment values. No historical financial backfill runs in the migration.

The generated Down migration removes the new financial structures and cannot safely preserve new financial records. Do not use Down as a data-preserving rollback after the feature has been used.

No remote database migration was executed for this task. The previously agreed remote test target was `db56823.public.databaseasp.net` / `db56823`; its identity and pending history must be confirmed again before any authorized application. Prior authorization for BookingWorkflow does not authorize this migration. The main running API still needs this schema and a restart with the new build before the new flow can be used against that target.

Read-only recheck on **2026-10-31 at 12:00 Cairo time**: configured server/database match that target; SQL Server reports identity `S13` and database `db56823`. All 12 earlier migrations are applied, ending at `20261029124607_BookingWorkflow`. The only pending migration is `20261030184822_CourseFinancialAgreements`. The audit issues only identity/history SELECTs and compares them with repository migration metadata; it never executes a migration or writes database records. Credentials are not included in logs or reports. Running-process configuration overrides on port 7223 were not independently inspected.

The reviewed forward SQL has SHA256 `3922E9CA68EAFC0092C55B017E3D70B5D20473C8497F0C11403396CE25309941`. The final review again found no table/column drops, data updates/deletes or truncation. A future authorized application must recheck the target/history and review the exact script before execution. No rollback/Down migration was executed.

## Verification

Backend tests use only isolated SQLite databases and real relational constraints/transactions. HTTP tests use real JWT authorization and API controllers in TestServer. They cover discounts, exact allocation, cross-batch reuse, approved limits, zero copay, partial receipts, cumulative gates, explicit check-out, cancelled/rescheduled reuse, compatible/ambiguous historical agreements, insurer earnings/claims/settlements, retry identity, concurrent course creation, concurrent receipts and simultaneous starts.

The SQL Server model/snapshot and forward script are verified without a database connection. SQL Server runtime migration execution and SQL Server-specific locking have not been exercised against the remote target; they remain deployment validation after explicit authorization.

Final verification resumed on 2026-10-31: **52 backend tests passed; 87 focused frontend tests passed across 15 files**. Backend runtime tests use isolated SQLite and real JWT/ASP.NET TestServer controllers; the SQL Server provider check is offline model/snapshot/script verification. Frontend component/service tests intercept HTTP. Production build is warning-free at 1.45 MB initial size with unchanged budgets; the login screen and on-demand collection dialog load lazily without changing authentication behaviour. The unrelated full frontend suite was not used as evidence for these results.

Resumed review fixed historical package shortfall collection across existing batch invoices, capped PerSession collections to that invoice's balance, kept other sessions' receipts out of PerSession summaries, rejected legacy course adoption when consumed sessions lack reconcilable invoices, and normalized submitted claim references consistently across retries. These changes preserve historical charge/payment amounts. Negative legacy discounts are rejected. Additional regression tests cover these cases.

Browser validation used the real Angular application and the isolated SQLite API test host through a local-only proxy. Synthetic self-pay package: gross 1,500, discount 150, agreed total 1,350; actual receipt 400; check-in blocked with exact shortfall 50; recording 50 enabled an explicit successful start. Exactly one booking committed. English/dark/light and Arabic/RTL/mobile screenshots were inspected; no panel overflow or runtime exceptions occurred. No shared database records were used or changed.

An additional insured course was booked through the rendered wizard: 300 agreed, 60 patient share, 240 insurer share; 20 actually collected from the patient. After one completed service, the insurer ledger showed 240 planned and 80 earned. Recording a submitted claim and an 80 Online settlement changed insurer collections to 80 and earned outstanding to zero. Patient collections remained separate. Each of the two patient flows committed exactly one booking.

The full browser flow was repeated successfully after the resumed fixes against the rebuilt matching backend, relayed through port 7288 to TestServer and a new temporary SQLite database. No business responses were mocked in this browser flow; the frontend's test identity was exchanged for a test-only JWT by the local proxy. The user's API process on 7223 was not changed. Temporary test databases do not establish SQL Server migration execution or SQL Server concurrency certification.

Reproducible evidence: `.tools/finance-tests.log`, `.tools/finance-target-audit.json`, `.tools/finance-preview-final.log`; frontend `tmp/finance-final-tests.log`, `tmp/finance-final-build.log`, `tmp/finance-browser-resumed.log`, `tmp/finance-preview/result.json` and screenshots. No remote mutations, push or deployment occurred. The remaining activation blocker is explicit authorization to apply the reviewed new migration to the re-confirmed SQL Server test target, followed by starting the matching main API build and validating the populated SQL Server flow.
