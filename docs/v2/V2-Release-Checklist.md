# V2 Release Checklist

## Purpose and decision boundary

Use this checklist to move Version 2 from Phase 10 verification through a controlled pilot and, only after separate approval, toward merge and broad release. It is an operator checklist, not evidence that Version 2 has been released.

Phase 10 work is restricted to branch `v2-qa-reports`. The approved Phase 9 baseline is `5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1`. The completed implementation was explicitly committed and pushed only to this branch as `b0879645cde1b6a619accdea70abf7e08425dc22`, then independently inspected. This documentation-only correction is separately authorized. Do not merge to `main`, create or move a tag, publish a release, or push another branch; those actions require their own explicit approvals.

Checkbox convention:

- `[x]` means the stated evidence has been completed with the displayed status.
- `[ ]` means an operator action or approval is still required. A pending item is not a Pass.
- Execution Type is always one of `Direct GUI test`, `Service or harness test`, `Source inspection`, or `Manual test pending`.
- Status is always one of `Pass`, `Fail`, `Blocked`, `Not Run`, or `Not Applicable`.

No Phase 10 result in this checklist is classified as `Direct GUI test`. Offscreen WinForms control tests are `Service or harness test`; they do not replace visible desktop testing.

## Completed Phase 10 evidence

### Repository and approved baseline

- [x] **Correct working branch** — Execution Type: `Source inspection`; Status: `Pass`. `git branch --show-current` returned `v2-qa-reports` at the starting gate.
- [x] **Approved Phase 9 baseline** — Execution Type: `Source inspection`; Status: `Pass`. Starting `HEAD` was `5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1`.
- [x] **Approved Phase 9 ancestry** — Execution Type: `Source inspection`; Status: `Pass`. `git merge-base --is-ancestor 5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1 HEAD` exited `0`.
- [x] **Earlier approved ancestry** — Execution Type: `Source inspection`; Status: `Pass`. The Phase 1 commit `49beaa1f45700725d328ade215419254560cb406` and Phase 2 commit `7d94a27a4569f8ca521aeaa6c08da0e510fc4dc7` were each verified as ancestors with exit code `0`.
- [x] **Phase 1–9 approval/baseline record reviewed** — Execution Type: `Source inspection`; Status: `Pass`. The locked Phase 10 brief identifies the Phase 9 baseline above as approved; historical point-in-time wording in the Phase 9 document is not treated as the current approval state.
- [x] **Clean starting Git status** — Execution Type: `Source inspection`; Status: `Pass`. `git status --short` returned no paths before Phase 10 edits.
- [x] **Protected Git state** — Execution Type: `Source inspection`; Status: `Pass`. No merge, rebase, cherry-pick, conflict, tag change, commit, or push was present at the starting gate.
- [x] **Stable V1 tag retained** — Execution Type: `Source inspection`; Status: `Pass`. The only listed release tag was `v1.0.0`; no V2 tag was created.
- [x] **Reviewed Phase 10 implementation commit** — Execution Type: `Source inspection`; Status: `Pass`. Commit `b0879645cde1b6a619accdea70abf7e08425dc22` is a nine-file, non-merge child of `5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1`, was pushed only to `v2-qa-reports`, and contains no package change or generated artifact.
- [x] **Independent implementation result** — Execution Type: `Source inspection`; Status: `Pass`. The recorded outcome is `Phase 10 approved with minor documentation corrections`. This result applies to the reviewed implementation commit and does not pre-approve the documentation-correction commit.

Re-run the repository gate before any later commit, merge, or release action:

```powershell
git branch --show-current
git rev-parse HEAD
git status --short
git log --oneline --decorate --graph -20
git tag --list
git merge-base --is-ancestor 5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1 HEAD
git merge-base --is-ancestor 49beaa1f45700725d328ade215419254560cb406 HEAD
git merge-base --is-ancestor 7d94a27a4569f8ca521aeaa6c08da0e510fc4dc7 HEAD
git merge-base --is-ancestor b0879645cde1b6a619accdea70abf7e08425dc22 HEAD
```

All four ancestry commands must exit `0`. Stop if the branch is wrong, an approved commit is absent, a Git operation is in progress, or unexplained files appear.

### Regression and hardening evidence

- [x] **Post-fix disposable harness** — Execution Type: `Service or harness test`; Status: `Pass`. The production-service/offscreen-control harness completed `368` assertions with `0` failures. The harness is temporary verification infrastructure and must not remain in the final repository diff.
- [x] **P10-DEF-001: publish failure propagation** — Execution Type: `Service or harness test`; Status: `Pass`. The wrapper defect was reproduced with a simulated failing `dotnet publish`, corrected narrowly, and retested so an internal native-command failure no longer reports false success.
- [x] **P10-DEF-002: QA index ordinary-whitespace normalization** — Execution Type: `Service or harness test`; Status: `Pass`. Ordinary whitespace runs are normalized according to the approved single-line index contract; the regression harness passed after the narrow correction.
- [x] **P10-DEF-003: rollback restoration order** — Execution Type: `Service or harness test`; Status: `Pass`. Injected transaction failure reproduced non-reverse restoration order; the corrected rollback now restores moved Hotel/PMS backups in true reverse move order and the transaction regressions passed.
- [x] **P10-DEF-004: oversized checklist-note PDF pagination** — Execution Type: `Service or harness test`; Status: `Pass`. The long checklist-note defect was reproduced, corrected within PDF layout code, and retested with rendered and extracted long content.
- [x] **V1 service/output regression** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic tests exercised all three V1 log types, IDs, filenames, text output, index output, and V1/QA separation without changing protected V1 contracts.
- [x] **V2 end-to-end service workflow** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic execution covered clean initialization, metadata, checklist applicability, findings, custom-script resolution, statistics, validation/status, PDF generation, paired save, overwrite, rollback/fault branches, and QA index behavior.
- [x] **Clean initialization** — Execution Type: `Service or harness test`; Status: `Pass`. A fresh temporary documentation root produced only the approved QA structure and valid empty metadata/index state; repeated initialization was idempotent and did not create a QA PDF.
- [x] **Metadata tests** — Execution Type: `Service or harness test`; Status: `Pass`. Valid add/load/search/persistence, normalized duplicates, canonical PMS relationships, unsafe names, collisions, corrupt input, explicit recovery, and backup isolation were exercised with synthetic data.
- [x] **Moderate-volume metadata/search** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic coverage used approximately 15 PMS systems and 100 Hotels, including partial search, duplicate Hotel names with distinct IDs, canonical PMS values, and reopen persistence.
- [x] **QA Report model/form control paths** — Execution Type: `Service or harness test`; Status: `Pass`. Offscreen forms opened, constructed their production controls, exercised report state/applicability, and closed without report artifacts.
- [x] **Findings and custom-script handling** — Execution Type: `Service or harness test`; Status: `Pass`. Warning/Failure separation, deterministic IDs, independent resolution, severity retention, stale removal, recurrence reset, and handled Failure presentation were exercised.
- [x] **Statistics tests** — Execution Type: `Service or harness test`; Status: `Pass`. Blank, broken-data, name, File Month, monetary, and database calculation boundaries and safe denominators were exercised without changing approved thresholds.
- [x] **Validation and status tests** — Execution Type: `Service or harness test`; Status: `Pass`. Incomplete-report blocking, stale-readiness invalidation, Created By fallback, `Pass`, `Pass with Warnings`, and `Fail` were exercised.
- [x] **PDF structural tests** — Execution Type: `Service or harness test`; Status: `Pass`. Generated byte arrays had valid PDF boundaries, required sections, status/content, page footers, privacy reminders, distinct Blank/Broken tables, and safe filename/path presentation.
- [x] **PDF visual and extraction tests** — Execution Type: `Service or harness test`; Status: `Pass`. Rendered/extracted outputs of 2, 4, 3, 3, and 24 pages were inspected. Long content completed without missing indexed content, clipping, overlap, incorrect footer totals, or blank trailing pages in the tested set.
- [x] **Paired-save tests** — Execution Type: `Service or harness test`; Status: `Pass`. Fresh Hotel/PMS copies were byte-identical, one current index entry was written, V1 index content was not modified, and normal success left no transaction artifacts.
- [x] **Overwrite tests** — Execution Type: `Service or harness test`; Status: `Pass`. Cancellation wrote nothing; confirmation reconciled recognized same-key files to one current pair and one current index entry. Different QA Dates remained the same Hotel ID/File Month key.
- [x] **Rollback and fault tests** — Execution Type: `Service or harness test`; Status: `Pass`. Staging/commit failures, real file-in-use behavior, injected permission categorization, rollback failure/manual-review signaling, and cleanup-warning success semantics were exercised. Injected permission failure is not evidence of a real Windows ACL denial.
- [x] **QA index tests** — Execution Type: `Service or harness test`; Status: `Pass`. One-entry-per-key update, relative paths, strict malformed/duplicate handling, current status/Created By/filename/timestamp, path containment, and separation from the V1 index were exercised.
- [x] **Path and filename safety** — Execution Type: `Service or harness test`; Status: `Pass`. Windows-invalid characters, reserved names, dot names, whitespace/control input, accents, long values, collision cases, similar Hotel IDs, structured parsing, and root containment were exercised.
- [x] **Privacy review** — Execution Type: `Source inspection`; Status: `Pass`. Forms, messages, metadata, index fields, PDF content rules, and documentation examples were reviewed; Phase 10 execution used synthetic data and no known privacy defect was found. Execution of the reviewed source paths is not implied by this source-inspection result.
- [x] **Known-issue review** — Execution Type: `Source inspection`; Status: `Pass`. Expected limitations are recorded below and must be acknowledged by pilot and release owners.

## Final build and publish evidence

The final Phase 10 build/publish owner recorded the completed results below after the source fixes. Success was accepted from the actual command output and process behavior, not merely from the publish wrapper's completion text.

| Gate | Exact command | Result to record | Execution Type | Status |
| --- | --- | --- | --- | --- |
| Final Debug build | `dotnet build DocumentationLoggingDashboard.sln` | Exit code `0`; 0 warnings; 0 errors | `Service or harness test` | `Pass` |
| Final Release build | `dotnet build DocumentationLoggingDashboard.sln -c Release` | Exit code `0`; 0 warnings; 0 errors | `Service or harness test` | `Pass` |
| Windows x64 self-contained single-file publish | `powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1` | Exit code `0`; internal publish succeeded; fresh output contained only `appsettings.json`, `DocumentationLoggingDashboard.exe`, and `DocumentationLoggingDashboard.pdb`; three-file total 118,749,230 bytes | `Service or harness test` | `Pass` |
| Fresh published executable startup | Start only the newly published `DocumentationLoggingDashboard.exe` and stop only that process after the startup probe | Reached input-idle state, remained alive for 3 seconds, and only the spawned process ID was closed; the prior 20-file publish tree was restored with an identical manifest | `Service or harness test` | `Pass` |

- [x] **Production-only Rebuild gate** — Execution Type: `Service or harness test`; Status: `Pass`. An ordinary production Rebuild with no temporary friend-assembly access completed with 0 warnings and 0 errors.
- [x] **Debug build gate** — Execution Type: `Service or harness test`; Status: `Pass`. Final exit code `0`, zero warnings, and zero errors.
- [x] **Release build gate** — Execution Type: `Service or harness test`; Status: `Pass`. Final exit code `0`, zero warnings, and zero errors.
- [x] **Single-file publish gate** — Execution Type: `Service or harness test`; Status: `Pass`. Genuine internal restore/build/publish output completed and the wrapper exited `0`.
- [x] **Published startup gate** — Execution Type: `Service or harness test`; Status: `Pass`. The fresh executable reached input idle and remained alive for 3 seconds; this was a process smoke test, not Direct GUI navigation.
- [x] **Package gate** — Execution Type: `Source inspection`; Status: `Pass`. `PDFsharp-MigraDoc-GDI` remains exactly `6.2.4`, no new package was added, and the fresh output contained no loose source or verification project.

### Safe publish-output procedure

The existing ignored `PublishedApp` tree contains pre-existing/user-looking V1 documentation and must not be deleted, overwritten as test data, inspected for content, or reported as synthetic output.

1. Resolve the repository and `PublishedApp\win-x64` paths to absolute paths and verify they are inside this workspace.
2. Confirm whether a process is running from the existing publish tree. Do not stop a user-owned process merely to run the test.
3. Record a relative-path, length, and SHA-256 manifest of the existing tree without quoting document contents.
4. Move the complete existing publish tree to a unique, verified preservation path inside the workspace. Do not perform a recursive delete.
5. Run the repository publish wrapper into a fresh `PublishedApp\win-x64`.
6. Confirm the fresh output is Windows x64, self-contained, and single-file. Expected application files are the executable, PDB, and `appsettings.json`; fail the gate if source, metadata, `QAReportIndex.txt`, test PDFs, harness files, or user settings are included.
7. Start only the fresh executable, record the process ID, and stop only that process after the startup probe.
8. Remove only the known fresh output after resolving and rechecking its containment. Restore the preserved tree to its original path.
9. Regenerate the manifest and require every pre-existing relative path, length, and SHA-256 hash to match. Any mismatch is a blocking data-preservation failure.
10. Confirm no publish output is staged or tracked.

## Pre-pilot gate

Complete every item in this section before authorizing the controlled pilot.

- [x] **Final build/publish evidence is complete** — Execution Type: `Service or harness test`; Status: `Pass`. Debug, Release, publish, and the published-process startup probe all passed with the exact evidence recorded above.
- [x] **Final repository hygiene** — Execution Type: `Source inspection`; Status: `Pass`. The nine reviewed Phase 10 files were committed as `b0879645cde1b6a619accdea70abf7e08425dc22`, pushed only to `v2-qa-reports`, and independently inspected; no unexplained or generated artifact was included.
- [x] **Temporary verification cleanup** — Execution Type: `Source inspection`; Status: `Pass`. `.phase10-verification`, temporary PDF/render trees, synthetic scenario roots, publish-test output, transaction artifacts, and recovery-test data are absent; the protected pre-existing ignored publish tree was restored.
- [x] **Generated artifact review** — Execution Type: `Source inspection`; Status: `Pass`. No PDF, PNG, test metadata, QA index, user settings, QA runtime tree, publish output, harness source, test result, or coverage artifact is tracked.
- [x] **Final source-diff scope** — Execution Type: `Source inspection`; Status: `Pass`. Reviewed commit `b0879645cde1b6a619accdea70abf7e08425dc22` contains exactly four justified source fixes and five required Phase 10 documents. Project/package, `.gitignore`, MainForm, and V1 files are unchanged.
- [x] **Required documentation review** — Execution Type: `Source inspection`; Status: `Pass`. Independent inspection found the technical results consistent and required only minor Git-state documentation corrections. The documentation-correction commit remains subject to subsequent independent inspection.
- [ ] **V1 visible desktop smoke plan accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner acknowledges that visible startup/close/reopen, required-field prompts, Preview/Submit, Change/Reset Folder, and Open File/Folder/Index were not directly clicked during Phase 10.
- [ ] **V2 visible desktop smoke plan accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner acknowledges that visible report entry, prompt text/defaults, cancellation, double-click protection, and end-to-end GUI saving remain pilot checks.
- [ ] **UI/scaling risk accepted for controlled pilot** — Execution Type: `Manual test pending`; Status: `Not Run`. Offscreen default/minimum-size control checks passed, but visible 100%, 125%, and 150% scaling, clipping/overlap, scroll behavior, focus cues, keyboard navigation, and screen-reader behavior remain unverified.
- [ ] **Real ACL limitation accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. A real file lock was exercised; permission branches used injected exceptions and are not a real Windows ACL test.
- [ ] **Pilot backup created** — Execution Type: `Manual test pending`; Status: `Not Run`. Back up the configured documentation root and any executable-side `user-settings.json` before pilot activity without changing or deleting existing V1 logs.
- [ ] **Privacy briefing complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot users understand the prohibited data list and the approved use of synthetic or explicitly approved internal report-level summaries.
- [ ] **Stop/rollback contacts assigned** — Execution Type: `Manual test pending`; Status: `Not Run`. Record the pilot owner, defect contact, backup owner, and person authorized to reconcile a manual-review transaction state.
- [ ] **Known limitations accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner reviews the limitation list below and accepts only controlled, monitored use.

Current technical recommendation: **Ready for controlled pilot**. The completed engineering and cleanup gates support a supervised pilot; the unchecked operational items above must be completed by the pilot owner before pilot execution. Independent review result: **Phase 10 approved with minor documentation corrections**. Final unconditional Phase 10 approval remains pending independent inspection of the pushed documentation-correction commit; this does not authorize merge or release.

## Controlled pilot execution

The pilot has not occurred. Use approximately 10–15 reports and record each attempt in the pilot log described by `V2-Pilot-Guide.md`.

- [ ] **Pilot authorization recorded** — Execution Type: `Manual test pending`; Status: `Not Run`. Authorization is for a controlled pilot only, not a production release.
- [ ] **Pilot environment and backup verified** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm the configured root, backup, write access, available disk space, and V1 log access before the first QA report.
- [ ] **Multiple Hotels and PMS systems** — Execution Type: `Manual test pending`; Status: `Not Run`. Use multiple approved Hotels and multiple PMS systems and confirm canonical Hotel/PMS selection.
- [ ] **Monetary scenarios** — Execution Type: `Manual test pending`; Status: `Not Run`. Include one, two, and more-than-two monetary-column cases and verify checklist/statistics applicability.
- [ ] **Status scenarios** — Execution Type: `Manual test pending`; Status: `Not Run`. Save at least one `Pass`, one `Pass with Warnings`, and one `Fail` report.
- [ ] **Custom-script scenario** — Execution Type: `Manual test pending`; Status: `Not Run`. Handle one finding independently and confirm its original severity and section remain unchanged.
- [ ] **Overwrite cancellation** — Execution Type: `Manual test pending`; Status: `Not Run`. Decline the default-No overwrite prompt and confirm both PDFs and the QA index are byte-for-byte unchanged.
- [ ] **Overwrite confirmation** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm replacement and verify one current Hotel copy, one current PMS copy, and one current index entry for the Hotel ID/File Month key.
- [ ] **Paired PDF equality** — Execution Type: `Manual test pending`; Status: `Not Run`. Compare length and SHA-256 of the Hotel and PMS copies for every saved pilot report.
- [ ] **QA index review** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm one entry per key, relative paths, correct status/effective Created By/filename/timestamp, and no absolute local or guest-level content.
- [ ] **PDF visual review** — Execution Type: `Manual test pending`; Status: `Not Run`. Open representative short and long reports and inspect wrapping, sections, grayscale readability, footer/page totals, special characters, and absence of blank trailing pages.
- [ ] **Visible UI review at 100%** — Execution Type: `Manual test pending`; Status: `Not Run`. Inspect MainForm, metadata, Add PMS, Add Hotel, QA Report, Findings, Statistics, prompts, and long scroll areas.
- [ ] **Visible UI review at 125%** — Execution Type: `Manual test pending`; Status: `Not Run`. Repeat clipping, overlap, scrolling, focus, and reachability checks.
- [ ] **Visible UI review at 150%** — Execution Type: `Manual test pending`; Status: `Not Run`. Repeat clipping, overlap, scrolling, focus, and reachability checks.
- [ ] **Keyboard and prompt review** — Execution Type: `Manual test pending`; Status: `Not Run`. Verify Tab order, Enter/Escape, owned dialogs, safe overwrite defaults, prompt consequences, and rapid double-click behavior.
- [ ] **V1 continuity during pilot** — Execution Type: `Manual test pending`; Status: `Not Run`. Run representative V1 Debugging, Script Editing, and Script Creation actions before and after QA errors; confirm V1 remains accessible and its index remains separate.
- [ ] **Pilot artifact/privacy audit** — Execution Type: `Manual test pending`; Status: `Not Run`. Review generated metadata, index, PDFs, backups, and defect attachments for prohibited personal, payment, credential, full-file, or production-reservation data.
- [ ] **Pilot defect log reviewed** — Execution Type: `Manual test pending`; Status: `Not Run`. Every failure records exact steps, expected/observed behavior, key/path category without sensitive values, impact, and whether rollback/manual review was required.
- [ ] **Pilot exit criteria met** — Execution Type: `Manual test pending`; Status: `Not Run`. Require 10–15 completed reports, no privacy/data-loss/path-traversal blocker, no unresolved V1/V2 regression, correct paired/index output, successful rollback/backup checks, and owner sign-off.
- [ ] **Pilot completion approved** — Execution Type: `Manual test pending`; Status: `Not Run`. Keep unchecked until the controlled pilot has actually completed and its evidence has been reviewed.

### Stop the pilot immediately if

- a Hotel and PMS copy differ, only one copy is committed without an explicit failure/manual-review result, or the QA index conflicts with the committed pair;
- a rollback is incomplete, `ManualReviewRequired` is reported, or transaction artifacts remain without a bounded cleanup warning;
- existing V1 logs, settings, publish-tree files, unrelated QA reports, or metadata are modified unexpectedly;
- any path escapes `QAReports\ByHotel`, `QAReports\ByPMS`, or the dedicated QA index location;
- a malformed index or corrupt metadata file is silently reset;
- guest, payment, credential, reservation-level, or full hotel-file data is requested, stored, displayed, or disclosed;
- visible clipping, overlap, inaccessible controls, unsafe prompt defaults, or double-submit behavior blocks correct operation;
- V1 becomes inaccessible after a QA failure; or
- any failure is falsely reported as success.

## Rollback and manual-review checklist

- [ ] **Pre-pilot backup is readable** — Execution Type: `Manual test pending`; Status: `Not Run`. Test the backup inventory before relying on it.
- [ ] **Stop new writes** — Execution Type: `Manual test pending`; Status: `Not Run`. On a suspected data-loss or transaction issue, close the QA workflow and prevent additional saves to the affected root.
- [ ] **Preserve evidence** — Execution Type: `Manual test pending`; Status: `Not Run`. Copy the affected Hotel folder, PMS folder, QA index, metadata, and `.qa-*` artifacts to a restricted review location; do not rename, delete, or auto-repair originals first.
- [ ] **Record application result** — Execution Type: `Manual test pending`; Status: `Not Run`. Capture the error category/stage, `PreviousStateRestored`, `ManualReviewRequired`, cleanup warning, filenames, and timestamps without copying sensitive report narrative into the ticket.
- [ ] **Reconcile one logical key** — Execution Type: `Manual test pending`; Status: `Not Run`. Compare both canonical copies and the one index block for the Hotel ID/File Month key. Preserve unrelated files and keys.
- [ ] **Do not silently repair malformed state** — Execution Type: `Manual test pending`; Status: `Not Run`. Corrupt metadata requires explicit backed-up recovery; a malformed QA index requires manual review. The application has no automatic index repair.
- [ ] **Application rollback prepared** — Execution Type: `Manual test pending`; Status: `Not Run`. If pilot use must stop, restore the previously approved application folder/binary from its verified backup while retaining QA data for review and leaving V1 documents untouched.
- [ ] **V1 access verified after rollback** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm the configured root and the three V1 workflows still function against their existing data.
- [ ] **Rollback approval documented** — Execution Type: `Manual test pending`; Status: `Not Run`. Only the assigned data owner may authorize deletion of confirmed transaction leftovers or replacement of a reviewed index/metadata file.

## Privacy and artifact hygiene

- [x] **Synthetic Phase 10 data only** — Execution Type: `Service or harness test`; Status: `Pass`. Verification data used synthetic Hotel/PMS/report values and temporary roots.
- [x] **Relative QA index paths** — Execution Type: `Service or harness test`; Status: `Pass`. Index entries were verified to use QA-root-relative Hotel/PMS paths rather than absolute user paths.
- [x] **PDF/index privacy boundaries** — Execution Type: `Service or harness test`; Status: `Pass`. Generated output was checked for report-level summaries and privacy reminders; full original paths and readiness fingerprints were excluded.
- [x] **Pre-existing publish/user data identified as protected** — Execution Type: `Source inspection`; Status: `Pass`. Existing ignored publish-tree V1 documentation is treated as user-owned and excluded from destructive testing.
- [x] **No test artifact remains** — Execution Type: `Source inspection`; Status: `Pass`. Final harness/PDF/publish/temp-root cleanup and the repository scan found no Phase 10 test artifact outside the recorded source and documents.
- [x] **No generated artifact is tracked** — Execution Type: `Source inspection`; Status: `Pass`. Status, tracked-file patterns, ignored-file review, and scoped/full diff inspection found no generated PDF, metadata, index, publish output, or verification artifact under version control.
- [x] **Existing publish tree restored byte-for-byte** — Execution Type: `Service or harness test`; Status: `Pass`. The pre-existing 20-file tree was restored and the pre/post relative-path, length, and SHA-256 manifests matched exactly.

Pilot privacy rules:

- Do not enter guest names, guest email addresses, guest phone numbers, payment information, credentials, reservation-level personal data, full hotel-file contents, or production records into report fields, notes, findings, screenshots, tickets, or attachments.
- Use only Hotel ID, Hotel Name, PMS Name, File Month, counts, percentages, approved script names, and concise non-sensitive QA summaries.
- Use synthetic data unless the pilot owner has explicitly approved controlled internal report-level data and the storage/backup location meets internal access requirements.
- Keep backups and defect evidence access-restricted. Do not email or upload report artifacts through an unapproved channel.
- Treat any privacy defect as blocking. Stop the pilot, preserve evidence safely, and notify the pilot owner.

## Known limitations to acknowledge

- No automatic hotel-file parsing, spreadsheet reading, database integration, automatic checklist population, or QA automation.
- No metadata edit/delete UI, report-history browser, or draft persistence.
- No cross-process save lock; the in-process lock cannot prevent another application process from racing.
- No automatic QA index repair and no guarantee that every abandoned backup after abnormal termination is cleaned automatically.
- No permanent automated test project; the Phase 10 harness is disposable.
- Offscreen WinForms tests are not visible GUI tests. Real 100%, 125%, and 150% scaling, keyboard navigation, screen-reader behavior, prompt interaction, and subjective usability remain pending.
- The real file-lock case was exercised, but the permission-denied branch used injected exceptions rather than a real Windows ACL denial.
- Published startup is not a full published-GUI workflow test. Full visible navigation and published end-to-end saving remain pilot work.
- PDF verification covers the recorded synthetic scenarios and installed font environment; it is not a guarantee for arbitrary/infinite text or every target machine's font configuration.
- V1 retains known technical debt, including non-transactional daily-file/index saving and non-concurrency-safe ID allocation. Phase 10 did not refactor it.

## Broad-release and merge gates

All items below remain unchecked until they are actually and separately completed. A successful controlled pilot does not itself authorize a merge, tag, or release.

- [ ] **Visible V1 regression complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Complete every applicable item in `V1-Regression-Checklist.md` on the release candidate.
- [ ] **Visible V2 end-to-end regression complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Complete the applicable manual rows in `V2-End-to-End-Test-Matrix.md`, including prompt and save flows.
- [ ] **UI/scaling review complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Require acceptable visible behavior at 100%, 125%, and 150%, or document and approve a tested organizational scaling boundary.
- [ ] **Pilot completed successfully** — Execution Type: `Manual test pending`; Status: `Not Run`. Require reviewed pilot evidence or a separately documented explicit waiver.
- [ ] **Known issues dispositioned** — Execution Type: `Manual test pending`; Status: `Not Run`. No open privacy, data-loss, rollback, path-traversal, false-success, or blocking V1/V2 issue may remain.
- [ ] **Final documentation-correction commit and clean branch state** — Execution Type: `Source inspection`; Status: `Not Run`. Require the documentation-only correction commit to be pushed to `v2-qa-reports`, with no unexplained/untracked/generated files, then record its full hash.
- [ ] **Final unconditional Phase 10 approval** — Execution Type: `Manual test pending`; Status: `Not Run`. A separate reviewer must inspect the pushed documentation-only correction commit. The correction commit must not approve itself.
- [ ] **Explicit merge approval** — Execution Type: `Manual test pending`; Status: `Not Run`. Obtain a separate instruction authorizing merge to `main`.
- [ ] **Main-branch merge** — Execution Type: `Manual test pending`; Status: `Not Run`. Do not merge during the Phase 10 working run.
- [ ] **Post-merge Debug and Release builds** — Execution Type: `Service or harness test`; Status: `Not Run`. Run both exact build commands against the approved merged commit and record results.
- [ ] **Post-merge single-file publish and startup** — Execution Type: `Service or harness test`; Status: `Not Run`. Repeat safe publish preservation, clean-output inspection, and startup verification.
- [ ] **Release approval** — Execution Type: `Manual test pending`; Status: `Not Run`. Obtain explicit release authorization after post-merge evidence is reviewed.
- [ ] **Release tag creation** — Execution Type: `Manual test pending`; Status: `Not Run`. Create `v2.0.0` only if separately authorized; no V2 tag exists from Phase 10.
- [ ] **Release notes finalized** — Execution Type: `Source inspection`; Status: `Not Run`. Convert the draft only after approved commit/tag/version details and final limitations are known.
- [ ] **Release artifact published** — Execution Type: `Manual test pending`; Status: `Not Run`. Do not distribute or announce Version 2 before release approval.
- [ ] **Branch-retention decision** — Execution Type: `Manual test pending`; Status: `Not Run`. Decide whether and how to retain `v2-qa-reports`; do not delete it as part of Phase 10.

Current broad-release recommendation: **Not ready for merge and release**. Required visible GUI/scaling work, controlled-pilot completion, independent inspection of the documentation-correction commit and final unconditional Phase 10 approval, explicit merge approval, post-merge verification, release approval, tag creation, and publication are all still pending.

## Final operator sign-off

- Controlled-pilot decision: `Ready for controlled pilot` / `Not ready for controlled pilot`
- Decision date:
- Pilot owner:
- Final Phase 10 build/publish evidence reviewed by:
- Unresolved pilot risks accepted:
- Pilot completion decision and date:
- Independent Phase 10 reviewer:
- Reviewed Phase 10 implementation commit: `b0879645cde1b6a619accdea70abf7e08425dc22`
- Independent implementation result: `Phase 10 approved with minor documentation corrections`
- Documentation-correction commit: Pending creation, push to `v2-qa-reports`, and independent inspection
- Explicit merge approval reference:
- Release approval reference:
- Final approved commit: Pending the documentation-correction commit and its independent inspection
- Release tag, if separately authorized:
- Branch-retention decision:

Until these fields and the applicable gates are completed, Version 2 remains unreleased on `v2-qa-reports`.
