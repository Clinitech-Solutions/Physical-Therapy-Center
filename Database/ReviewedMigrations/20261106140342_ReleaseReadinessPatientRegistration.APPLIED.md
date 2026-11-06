# Patient registration operation identity — applied

Applied **2026-11-06**, verified at **14:22 UTC / 16:22 Africa/Cairo**. This records a schema migration only; no application deployment or shared patient registration was performed.

- Verified configured target: `db56823.public.databaseasp.net / db56823`; connected server identity **S13**, database **db56823**.
- Previous migration: `20261104195622_ClinicianAbsenceRecovery`.
- Applied: `20261106140342_ReleaseReadinessPatientRegistration`.
- Reviewed SQL: [20261106140342_ReleaseReadinessPatientRegistration.sql](20261106140342_ReleaseReadinessPatientRegistration.sql).
- SHA256: `1A2B292323FCECCE7C58A07148148671BE34151A9B9F8EA5D884D450672B2537`.

The pinned-script process checked the target, complete migration chain, source model/snapshot, generated SQL equality and SQL hash before execution. The only changes are nullable `Patients.RegistrationOperationKey` (`nvarchar(100)`), nullable `Patients.RegistrationRequestHash` (`nvarchar(64)`), their filtered unique operation-key index, and the migration-history entry. Existing patients require no backfill. No tables or columns were dropped and no existing business rows were updated.

Before/after preservation snapshots were identical:

| Table | Rows |
| --- | ---: |
| AspNetUsers / AspNetUserRoles | 41 / 35 |
| Patients / Appointments | 19 / 30 |
| Assessments / TreatmentPlans / TreatmentPlanSessions | 6 / 6 / 35 |
| TreatmentSessionRecords / ReassessmentRequests / ReAssessments | 1 / 0 / 0 |
| Rooms / DoctorSchedules / DoctorAbsences | 8 / 108 / 5 |
| AppointmentScheduleChanges / BookingBatches | 10 / 21 |
| Invoices / Payments / TreatmentCourseAgreements / InsuranceClaims | 33 / 13 / 3 / 0 |

Invoice totals remained **15600.00 total, 7600.00 patient liability, 8000.00 insurer liability, 4600.00 paid**. Payment total remained **4600.00**. Post-apply verification confirmed both nullable columns, the unique filtered index, successful current-model reads, matching model/snapshot and **zero pending migrations**.

Local ignored evidence: `.tools/ReleaseReadinessMigration/{inspection,before,after,verification}.json` and `.tools/release-fixes-migration-{inspect,apply,verify}.log`. Credentials are excluded.
