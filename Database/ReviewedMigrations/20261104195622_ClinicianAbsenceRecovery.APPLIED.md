# Clinician absence recovery migration applied

Applied and verified on **2026-11-04**, following the previously authorized, pinned migration process.

- Verified target: `db56823.public.databaseasp.net` / `db56823`; connected server identity `S13`.
- Previous migration: `20261031164400_DoctorClinicalWorkspace`. Exactly one migration was pending: `20261104195622_ClinicianAbsenceRecovery`.
- Fresh EF-generated SQL matched the reviewed [SQL artifact](20261104195622_ClinicianAbsenceRecovery.sql). SHA256: `2AE89D83F18F170767ED04119DE545FE8CD9B805C7591135071F7AF57C93A7AC`.
- Applied only that transactional script. Six additive absence columns, ten additive scheduling audit columns, indexes and a restrictive absence/audit foreign key. No tables/columns dropped, existing columns narrowed, business data updated, financial backfill, or migration-to-latest operation.
- Existing absence dates retain their clinic-local full-day meaning when interval bounds are null. Existing history is preserved.

Verification immediately after the migration found the exact history entry, zero pending migrations, the expected columns, and successful current-source SQL Server absence/list/impact reads. Counts for all 14 inspected business tables and invoice/payment sums matched their before-migration snapshots exactly. The migration itself created no bookings, payments, absences or charges.

After the user restarted the local API, authenticated browser requests to `https://localhost:7223/api/Absences` and `/api/Absences/affected` returned **200**, and the Angular schedule and Record absence dialog loaded successfully. Subsequent synthetic acceptance data is documented separately in the workflow report; it is not part of the migration preservation comparison.

Ignored local evidence: `.tools/AbsenceMigrationApply/{inspection,before,after,verification}.json` and `.tools/absence-target-{inspect,apply,verify}.log`. No credentials are included here. No push or deployment.
