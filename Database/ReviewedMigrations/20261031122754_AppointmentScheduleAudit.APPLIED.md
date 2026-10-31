# Authorized scheduling audit migration applied

Applied and verified on **2026-10-31, approximately 15:12 Cairo time** after the user explicitly requested applying the outstanding profile migration.

- Target rechecked immediately before execution: `db56823.public.databaseasp.net` / `db56823`, live server identity `S13`. No credentials were displayed.
- The history chain matched the repository and model snapshot. Last applied was `20261030184822_CourseFinancialAgreements`; the sole pending migration was `20261031122754_AppointmentScheduleAudit`. No additional migrations were applied.
- Fresh EF-generated forward SQL exactly matched the reviewed [SQL artifact](20261031122754_AppointmentScheduleAudit.sql), SHA256 `D4EFC568F1FCA88BB32C2CF5CC1E038FDF5C1D0C80E10361A1E2AB2FC1D72769`.
- Forward SQL only creates the audit table, indexes and restrictive foreign keys, then records the migration history entry. No existing column/table changes, destructive operations, historical updates or financial backfill. Only this pinned transactional script was executed; no migration-to-latest or Down script.

| Verification | Result |
| --- | --- |
| Migration history | Exact audit entry present and latest; zero pending migrations |
| Audit schema | All 19 columns, four indexes including unique operation key, two enabled/trusted NO ACTION foreign keys |
| Existing records | Counts unchanged for Appointments, Assessments, Patients, TreatmentPlans, TreatmentPlanSessions, Rooms, DoctorSchedules, Invoices, Payments, BookingBatches, TreatmentCourseAgreements and InsuranceClaims |
| Financial preservation | Invoice total/patient/insurer/paid sums and payment total unchanged |
| SQL Server service reads | Profile for patient 13, dashboard and invoice services passed |
| Running local API | GET `/api/Bookings/patients/13/profile`: **200**, verified patient identity; GET `/api/Receptionist/dashboard-stats`: **200**; GET `/api/Billing/invoices`: **200** |

Local authenticated HTTP checks used a five-minute, locally signed token for an existing Receptionist, held in memory only. These were read-only probes, not password-login/browser tests. No patient response bodies, credentials or tokens are included in the evidence. No test financial or scheduling records were created, and no reschedule mutation was exercised on this shared SQL Server database. Earlier isolated SQLite/browser mutation tests remain separate.

The running local API was preserved; no restart was needed for the successful reads. No push or deployment. The profile's missing-audit-schema activation blocker is resolved. Shared SQL Server rescheduling/concurrency writes remain deliberately untested.

Ignored local evidence: `.tools/ScheduleAuditApply/inspection.json`, `inspect.log`, `apply.log`, `before.json`, `after.json`, `verification.json`. Prior application reports remain historical; this report supersedes the audit migration's earlier pending status.
