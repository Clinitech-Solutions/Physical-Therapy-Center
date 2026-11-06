# Applied: Insurance cheque receipts and monthly submission identity

Applied **2026-11-06, Africa/Cairo** through the pinned reviewed SQL process.

- Target: `db56823.public.databaseasp.net / db56823`; connected server identity **S13** verified immediately before applying.
- Previous migration: `20261106140342_ReleaseReadinessPatientRegistration`.
- Applied migration: **`20261106160811_InsuranceChequeReceipts`**; zero pending migrations; current model matches the migration snapshot.
- Reviewed [SQL](20261106160811_InsuranceChequeReceipts.sql) SHA256: **`DF447C8181F633E11448C65A1DBAC450F343976CD191DBC2B974F035895F3D7B`**. Generated idempotent SQL was compared byte-for-byte with the reviewed file before application.

The migration adds two empty tables for cheque headers and monthly claim submission identity, four nullable Payment allocation evidence fields and one nullable InsuranceClaim batch link. Unique indexes protect operation keys, normalized cheque identity and receipt/claim allocation identity. Foreign keys restrict deletion. Existing invoices and Payments remain the liability and actual cash ledgers; no charge or receipt backfill was performed.

The runner required the exact pending migration and verified target/history/model/script identity. Only the pinned additive SQL ran, inside its generated transaction with XACT_ABORT and lock timeout. It contained no business-row update, deletion, backfill or financial insertion.

## Preservation

All **19** checked business/Identity table counts and SHA256 hashes of their original columns are identical before and after. The hashes use deterministic primary-key order and include nulls; new nullable columns are excluded from both fingerprints. Both new tables were verified empty. Current-model Payment reads succeeded and all five new nullable columns and four unique identity/allocation indexes were verified.

| Metric | Before | After |
| --- | ---: | ---: |
| Invoice face amount | EGP 15,600.00 | EGP 15,600.00 |
| Patient liability | EGP 7,600.00 | EGP 7,600.00 |
| Insurer liability | EGP 8,000.00 | EGP 8,000.00 |
| Invoice paid amount | EGP 4,600.00 | EGP 4,600.00 |
| Actual Payment amount | EGP 4,600.00 | EGP 4,600.00 |

Local ignored evidence: `.tools/InsuranceMonthMigration/{inspection,before,after,verification}.json`, `.tools/insurance-month-migration-{inspect,apply,verify}.log`. Runner source is retained at `.tools/InsuranceMonthMigration/Program.cs`. It reads configured credentials without printing them. Snapshot preparation was corrected for the clinical record's AppointmentId key and empty-table JSON before any migration SQL ran.

This is schema/preservation verification on SQL Server. Synthetic financial writes use disposable SQLite; it does not certify production-sized load or provider-specific financial concurrency acceptance. No push or deployment was performed.
