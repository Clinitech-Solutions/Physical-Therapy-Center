# Backend PR #6: merge resolution

Resolved locally on **2026-11-06, Africa/Cairo**. No push, deployment, API activation or database migration was performed during this task. No shared database was connected for verification.

## Source and preservation

- PR: MinaRomany78/Physical-Therapy-Center **#6**.
- Source: `Clinitech-Solutions:Abanoub3vv`, original tip **`8b38535b158d1369bcb6644abb9e3e43a186c920`**.
- Target: `MinaRomany78:master`, freshly fetched tip **`5af115a40d42c0ccd26df692e47f09bc90fc78ba`**.
- Merge base: `1eca33f377d00558e8c3054a661ea569e4be8b7a`; the original source had five exclusive commits and upstream had four.
- `origin` points to Clinitech-Solutions; `upstream` points to MinaRomany78. A fresh `ls-remote` also confirmed PR #6's head equals the inspected source tip and master equals the fetched target.
- Backend status was clean before integration. No uncommitted backend work needed stashing. The frontend was not modified by this task.
- Original source is retained on local backup branch **`codex/pr6-before-upstream-20261106`**.

Integration used `git merge --no-commit --no-ff upstream/master` on `Abanoub3vv`. All 14 conflicted files were inspected on both sides before editing, including stage inputs, callers, contracts and upstream-only automatically merged changes. Decisions were made per hunk; no global ours/theirs/both resolution was used. Original conflict-stage contents and individual resolution reasons are retained as ignored evidence in `.tools/pr6-merge/conflict-inputs` and `resolutions.json`.

## Conflict decisions

| Conflicted file | Final behavior |
| --- | --- |
| `PTCenter.Api/Controllers/CourseFinanceController.cs` | Keep Reception patient-collection permissions and Ceo/Admin/Owner insurer permissions. The old settlement route can replay an already-committed historical operation with exact intent; a new legacy receipt returns `409 Insurance.ChequeRequired` and cannot bypass cheque validation. |
| `PTCenter.Api/Controllers/DoctorWorkspaceController.cs` | Retain assigned Senior coverage access alongside Doctor and supervisory roles. Senior membership does not grant access to another clinician's session. |
| `PTCenter.Application/Contracts/Bookings/BookingContracts.cs` | Combine upstream nullable saved weekly frequency and enriched booking receipt fields with existing assessment-workspace and optional reviewed allocation hash contracts. Keep one declaration per property and sorted saved booked starts. Invalid legacy frequency is represented as unavailable and blocks booking. |
| `PTCenter.Application/Contracts/Doctor/DoctorWorkspaceContracts.cs` | Retain `CoveringTreatment` and `NeedsAttention`, required by coverage/absence callers. |
| `PTCenter.Application/Contracts/Patients/CreatePatientRequest.cs` | Accept upstream's optional legacy username field but ignore it in registration. Retain the operation key and real email; use upstream's 256-character email contract. |
| `PTCenter.Infrastructure/Services/BookingService.Doctor.cs` | Retain eligible Doctor/Senior identity, gender/recorded phone-consent checks, partial/withdrawn absence handling, current insurance approval and actual-start room/clinician capacity checks. |
| `PTCenter.Infrastructure/Services/BookingService.PatientProfile.cs` | Combine upstream's refusal to reschedule clinically started records and saved room capacity with source coverage, consent, actual appointment duration and audit history. Future availability uses planned reservations; actual occupancy is enforced at arrival/start. |
| `PTCenter.Infrastructure/Services/BookingService.cs` | Retain upstream bookable-patient directory, name/ID ordering, covered-session limits, weekly frequency checks, enriched receipts and capacity-aware arrival. Preserve recovery assignment/duration, allocation preconditions, absence guards and saved assessment drafts. Physical departure never signs an assessment or delivers its invoice; authorized clinical completion owns delivery. |
| `PTCenter.Infrastructure/Services/DoctorWorkspaceService.cs` | Retain exact Cairo day boundaries, assigned Doctor/Senior scope, absence attention and start-time checks. Treatment notes/finalization and reassessment behavior remain intact. |
| `PTCenter.Infrastructure/Services/PatientService.cs` | Keep backend-generated internal names, injected collision generator, bounded retries, serializable clinic lock, atomic Identity/Patient/role persistence, unique operation identity and changed-intent rejection. Retain upstream patient sorting and unconfirmed real email; no fake email is generated. |
| `PTCenter.Infrastructure/Services/ReceptionistService.cs` | Retain authoritative clinical completion flags and exclude unsigned assessments from clinical-completed counts. Preserve explicit date filtering and Senior coverage using Cairo boundaries. |
| `PTCenter.Infrastructure/Services/TreatmentPlanSessionService.cs` | Keep authenticated actor ownership and signed/clinically or financially used-record guards, audit actor fields and serializable mutation locking before legacy session changes. Upstream finalized-note protection is preserved. |
| `PTCenter.Tests/BookingMigrationTests.cs` | Retain both absence and Doctor migration checks. Add an offline assertion for the complete clinical/absence/registration/cheque chain and current snapshot. |
| `PTCenter.Tests/ReceptionAccessTests.cs` | Keep grouped Billing in Reception access coverage, alongside upstream operational permission checks. |

## Compatible upstream behavior retained

Room responses expose actual occupants and current load. Availability respects saved multi-patient room capacity, while live arrival/start checks prevent over-occupancy. Room capacity reduction/maintenance guards and clinic mutation locking remain. Future planned availability and currently overdue treatment occupancy retain their distinct meanings.

Booking directories sort before paging and omit exhausted plans while keeping the general patient directory. Weekly limits use the Cairo Saturday-to-Friday clinic week, consider existing appointments and release cancelled allocations. Saved course/insurance coverage limits constrain remaining bookable sessions. Booking receipts include clinician, room, session, plan, agreement and invoice links. Existing upstream acceptance tests and their explicit fixture cadence changes are retained.

All existing registration, legacy clinical authorization, signed-history protection, reasoned reassessment, absence/phone-consent audit, unsigned-departure handoff, grouped Billing, patient receipt allocation and company/month insurer-cheque workflows remain covered by the merged tests.

## Integration fixes beyond marker removal

New regression cases reproduced two interactions that neither parent handled together:

1. The older absence recovery path could move a treatment into a week already at upstream's prescribed frequency limit. Recovery options, recovery commit, retained Senior rescheduling and ordinary reschedule availability now use the same existing weekly-count rule. The appointment being moved is excluded from its old count; valid keep-time coverage remains possible. Rejection preserves invoice values and schedule audit records.
2. Upstream's enriched receipt mapper assumed a Doctor navigation and returned a null name for a covering Senior. It now resolves the saved assigned clinician ID against staff names while retaining original session/invoice/agreement links and financial amounts.

Additional real password-authenticated HTTP regressions verify ignored legacy usernames plus exact registration replay/unconfirmed real email, historical legacy settlement recovery, changed-intent rejection, mandatory cheque entry for new insurer cash and Reception denial. These tests supplement the existing direct clinical, reassessment, absence, Cairo, grouped billing and cheque suites.

## Migration and EF review

All migration classes, designers, the EF model snapshot, DbContext and persistence configurations are **byte-for-byte unchanged from the original PR source**. The merged chain retains DoctorClinicalWorkspace ? ClinicianAbsenceRecovery ? ReleaseReadinessPatientRegistration ? InsuranceChequeReceipts. The snapshot retains clinical records, absence/consent fields, registration operation identity, cheque headers, monthly submission identity and Payment allocation evidence/indexes.

Offline SQL Server model checks confirm no model/snapshot drift. Forward script generation checks the additive registration/cheque changes and excludes destructive operations or insertion into business invoice/payment tables. These checks construct a design-only context and do not open a SQL Server connection. Relational HTTP/service tests use unique disposable SQLite databases.

Upstream's already-reviewed DoctorClinicalWorkspace SQL and historical review/application documents were imported unchanged. Their presence does not mean this task applied anything. SQL SHA256 remains **`7570349E7A71FB6DD7AC8ADEC9DE0D126641E7524DF1B84BBAB42CF8BE920FD8`**, matching the upstream review. Its existing final blank line causes a `git diff --check` EOF-whitespace notice; it is intentionally preserved to keep the pinned artifact unchanged. All other resolved changes pass whitespace checks. No new migration was generated or applied.

## Verification

- Backend API build: **PASS**, zero errors. Existing `System.Security.Cryptography.Pkcs 9.0.4` restore advisories **NU1902/NU1903** remain unchanged.
- Initial relevant test run: **141 passed**.
- Targeted new interaction tests after fixes: **5 passed**.
- Final relevant run, including clinical workspace/reassessment and offline migration/model validation: **164 passed, 0 failed, 0 skipped**.
- Full backend suite: **247 passed, 0 failed, 0 skipped**. This includes all 230 source cases, 11 imported upstream cases and six additional merge integration/model cases.
- No unmerged index entries or conflict markers remain. All resolved source/test changes pass whitespace checks; only the unchanged pinned upstream SQL EOF notice described above remains.

**Remaining test failures: none.**

Reproducible commands from the backend repository:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build PTCenter.Api/PTCenter.Api.csproj --artifacts-path .tools/pr6-build
& 'C:\Program Files\dotnet\dotnet.exe' test PTCenter.Tests/PTCenter.Tests.csproj --artifacts-path .tools/pr6-build --logger 'trx;LogFileName=pr6-full.trx' --results-directory .tools/pr6-merge/results
```

Local logs/TRX: `.tools/pr6-merge/build.log`, `focused.log`, `integration-before.log`, `integration-after.log`, `focused-final.log`, `full.log`, and `results/{pr6-focused,pr6-focused-final,pr6-full}.trx`. The initial added HTTP test's enum-deserialization setup was corrected independently of the two reproduced product integration defects. No test was disabled or skipped to obtain a pass.

## Review state

The local integration is recorded as a merge commit on **Abanoub3vv**, retaining both the original source tip and fetched upstream master as parents. Review it with `git show HEAD`, compare the integrated changes with `git diff codex/pr6-before-upstream-20261106..HEAD`, or inspect the complete PR scope with `git diff upstream/master...HEAD`. The source backup branch remains available. The final working tree is clean.

No remote branch or PR was updated. No push, deployment, shared-database write, migration application or API restart was performed. Provider-specific SQL Server mutation/concurrency acceptance and production load were not exercised during this merge task. Existing business decisions about signed corrections and financial reversals remain unchanged.
