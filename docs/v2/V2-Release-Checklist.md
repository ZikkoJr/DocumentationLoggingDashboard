# V2 Release Checklist

> **Production-candidate cleanup update (2026-07-20):** Final synthetic semantic, deferred-text, geometry, blocked-save, overwrite, paired PDF/index, and V1 regressions pass; the permanent harness is in the solution; production diagnostic counters are removed; and an external candidate is being prepared for independent review. The candidate is still not merged, tagged, or released. Real 100% and 150% DPI remain untested and cannot be checked here.

## Purpose and decision boundary

Use this checklist to move Version 2 from Phase 10 verification through a controlled pilot and, only after separate approval, toward merge and broad release. It is an operator checklist, not evidence that Version 2 has been released.

Phase 10 work is restricted to branch `v2-qa-reports`. The approved Phase 9 baseline is `5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1`. The completed Phase 10 implementation was committed and pushed only to this branch as `b0879645cde1b6a619accdea70abf7e08425dc22`, then independently inspected. The later controlled-pilot baseline was `d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc`. A focused P1 Blank/Broken correction has been implemented locally from that checkpoint and independently approved from a code-review standpoint, but this checklist does not claim a correction commit, package replacement, or pilot-resumption authorization. Do not merge to `main`, create or move a tag, publish a release, replace the frozen pilot package, modify the live Retry-01 root, or resume Scenario 3 without separate authorization.

Checkbox convention:

- `[x]` means the stated evidence has been completed with the displayed status.
- `[ ]` means an operator action or approval is still required. A pending item is not a Pass.
- Execution Type is always one of `Direct GUI test`, `Service or harness test`, `Source inspection`, or `Manual test pending`.
- Status is always one of `Pass`, `Fail`, `Blocked`, `Not Run`, or `Not Applicable`.

No historical Phase 10 result in this checklist is classified as `Direct GUI test`. Offscreen WinForms control tests are `Service or harness test`; they do not replace visible desktop testing.

> **Current decision:** **Production candidate may proceed to independent review only.** This is not authorization to resume the frozen pilot, merge `main`, create `v2.0.0`, or create a GitHub release.

## Deferred-text correction gate - local evidence complete, review pending

- [x] **Focused deferred-commit tests pass** - Execution Type: `Service or harness test`; Status: `Pass`. The final 8/8 run produced zero model commits/change events while typing, committed once per completed child edit, preserved finding identities, and produced no moved or duplicate notes. Production diagnostic counters are not used.
- [x] **Correction-specific save/PDF/index matrix passes** - Execution Type: `Service or harness test`; Status: `Pass`. Five reports covered General Notes, Explained and Accepted, handled Failure/custom script, Active Failure notes, and checklist Warning explanation. All pairs were byte-identical; statuses/index entries and final text markers passed; 14 rendered pages had no clipping/overlap.
- [x] **Actual-125% long-note readiness smoke passes** - Execution Type: `Direct GUI test`; Status: `Pass`. More than 1,000 characters preserved focus/caret/selection/scroll with no pre-boundary synchronization; direct readiness committed the final character with one synchronization/refresh.
- [x] **Temporary publish/startup passes** - Execution Type: `Direct GUI test`; Status: `Pass`. The new external-temporary executable opened visibly, responded, closed normally, and created no QA-root files. It is not an approved pilot package.
- [ ] **Independent deferred-text review recorded** - Execution Type: `Manual test pending`; Status: `Not Run`.
- [ ] **Real 100% and 150% deferred-text visual retest passes** - Execution Type: `Direct GUI test`; Status: `Not Run`.
- [x] **Dedicated V1 operational regression passes** - Execution Type: `Service or harness test`; Status: `Pass`. One synthetic entry per V1 log type passed prefixes, sequence, filenames, exact text, central LogIndex, folder resolution, QA-index isolation, and expected absence of Hotel/PMS Debugging Log routing.
- [ ] **Commit/package/pilot authorization granted** - Execution Type: `Manual test pending`; Status: `Not Run`.

## P1 correction gate — partially complete

- [x] **Focused correction implementation independently reviewed** — Execution Type: `Source inspection`; Status: `Pass`. Independent review of the approved Blank/Broken semantics, propagation, UI, validation/readiness, findings, PDF consumption, and permanent tests found no P0/P1 blocker and approved the local diff from a code-review standpoint.
- [x] **Permanent corrected-semantic tests pass** — Execution Type: `Service or harness test`; Status: `Pass`. Debug and Release each passed 7/7, covering 0/1/50/51 thresholds, separation, data-only 119-row header exclusion, First/Full/Last Name, email-in-name, propagation, lifecycle, invalid states, readiness/status, Notes-conditioned mapped Failure, and resolution noncausality.
- [x] **Permanent STA geometry/interaction tests pass** — Execution Type: `Service or harness test`; Status: `Pass`. Debug and Release each passed 7/7 at actual `DeviceDpi=120`, covering containment, group nonintersection, supported widths, no horizontal scrollbar, one outer vertical scrollbar, preserved inner scrollbars, dynamic applicability, propagation/no recursion, scrolling, native hit-testing, and forward/reverse Tab navigation.
- [ ] **Real Windows 100% visual matrix passes** — Execution Type: `Direct GUI test`; Status: `Not Run`. Record initial, maximized, and minimum 880x600 geometry/interactions plus screenshots at maximum applicability.
- [x] **Real Windows 125% visual no-save matrix passes** — Execution Type: `Direct GUI test`; Status: `Pass`. Nine accepted images cover default, 880x600, maximum applicability, 119/header-row behavior, Blank/Broken manual overrides, Auto restore, font pressure, and maximized states; STA interaction assertions passed 7/7 in Debug and Release at the same actual DPI.
- [ ] **Real Windows 150% visual matrix passes** — Execution Type: `Direct GUI test`; Status: `Not Run`. Repeat the complete supported-size/dynamic-state matrix and record screenshots.
- [x] **Corrected Debug and Release builds pass** — Execution Type: `Service or harness test`; Status: `Pass`. Both solution builds and both permanent-test project builds exited 0 with 0 warnings and 0 errors.
- [x] **Isolated external publish inventory passes** — Execution Type: `Service or harness test`; Status: `Pass`. Final external inventory contains EXE, PDB, `appsettings.json`, and isolated `user-settings.json`; sizes/hashes are recorded in the correction record. It is not an approved package.
- [x] **Published startup and visible no-save smoke pass** — Execution Type: `Direct GUI test`; Status: `Pass`. The isolated executable opened visibly with the expected title, responded at 1020x806, closed normally with exit 0, and left the isolated synthetic root empty.
- [x] **Seven corrected synthetic save/PDF/index scenarios pass** — Execution Type: `Service or harness test`; Status: `Pass`. All expected statuses/counts passed, Hotel/PMS copies were byte-identical, the QA index held seven expected logical entries, and no transaction leftovers remained; sizes and SHA-256 values are in the correction record.
- [ ] **Corrected PDF visual review complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Representative both-Warnings, mapped-Failure, and handled-mapped-Failure PDFs passed rendered review. A direct permanent PDF-order assertion and rendered nonblank-Notes two-Failure variant remain open; do not credit this combined gate until those gaps are dispositioned.
- [x] **V1 regression passes** — Execution Type: `Service or harness test`; Status: `Pass`. Separate synthetic DEBUG/EDIT/CREATE entries passed IDs, sequence, filenames, exact text, central V1 LogIndex, folders, QA-index isolation, and expected absence of Debugging Log Hotel/PMS routing.
- [x] **Protected-state audit passes** — Execution Type: `Source inspection`; Status: `Pass`. No dashboard/test process was running; branch/HEAD/upstream, `main`, and tag inventory were unchanged; the frozen package retained its hashes/timestamps; the live Retry-01 root retained five files with no write after 2026-07-17; and no V1, dependency, deployment, history, release, or protected-root mutation was found.
- [x] **Independent P1 review recorded** — Execution Type: `Source inspection`; Status: `Pass`. No P0/P1 blocker was found; approval is limited to the code-review standpoint and grants no commit/package/pilot authority.
- [ ] **Correction commit authorization and commit recorded** — Execution Type: `Manual test pending`; Status: `Not Run`. Do not commit or push until explicitly authorized after independent review.
- [ ] **Pilot-package replacement and Scenario 3 resumption separately authorized** — Execution Type: `Manual test pending`; Status: `Not Run`. A reviewed commit does not itself authorize either action.

## Completed historical Phase 10 evidence

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
- [x] **Independent implementation result** — Execution Type: `Source inspection`; Status: `Pass`. The recorded outcome is `Phase 10 approved with minor documentation corrections`. The follow-up documentation commit later became `d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc`, the previously approved pilot baseline. Neither historical result pre-approves the current P1 correction.

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

- [x] **Historical post-fix disposable harness** — Execution Type: `Service or harness test`; Status: `Pass`. The Phase 10 production-service/offscreen-control harness completed `368` assertions with `0` failures. Its affected Blank/Broken assertions are superseded as correction evidence; the historical count does not satisfy the fresh permanent-test gate.
- [x] **P10-DEF-001: publish failure propagation** — Execution Type: `Service or harness test`; Status: `Pass`. The wrapper defect was reproduced with a simulated failing `dotnet publish`, corrected narrowly, and retested so an internal native-command failure no longer reports false success.
- [x] **P10-DEF-002: QA index ordinary-whitespace normalization** — Execution Type: `Service or harness test`; Status: `Pass`. Ordinary whitespace runs are normalized according to the approved single-line index contract; the regression harness passed after the narrow correction.
- [x] **P10-DEF-003: rollback restoration order** — Execution Type: `Service or harness test`; Status: `Pass`. Injected transaction failure reproduced non-reverse restoration order; the corrected rollback now restores moved Hotel/PMS backups in true reverse move order and the transaction regressions passed.
- [x] **P10-DEF-004: oversized checklist-note PDF pagination** — Execution Type: `Service or harness test`; Status: `Pass`. The long checklist-note defect was reproduced, corrected within PDF layout code, and retested with rendered and extracted long content.
- [x] **V1 service/output regression** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic tests exercised all three V1 log types, IDs, filenames, text output, index output, and V1/QA separation without changing protected V1 contracts.
- [x] **Historical V2 end-to-end service workflow** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic Phase 10 execution covered clean initialization, metadata, checklist applicability, findings, custom-script resolution, statistics, validation/status, PDF generation, paired save, overwrite, rollback/fault branches, and QA index behavior. Blank/Broken-dependent outcomes are superseded and require the new gate above.
- [x] **Clean initialization** — Execution Type: `Service or harness test`; Status: `Pass`. A fresh temporary documentation root produced only the approved QA structure and valid empty metadata/index state; repeated initialization was idempotent and did not create a QA PDF.
- [x] **Metadata tests** — Execution Type: `Service or harness test`; Status: `Pass`. Valid add/load/search/persistence, normalized duplicates, canonical PMS relationships, unsafe names, collisions, corrupt input, explicit recovery, and backup isolation were exercised with synthetic data.
- [x] **Moderate-volume metadata/search** — Execution Type: `Service or harness test`; Status: `Pass`. Synthetic coverage used approximately 15 PMS systems and 100 Hotels, including partial search, duplicate Hotel names with distinct IDs, canonical PMS values, and reopen persistence.
- [x] **QA Report model/form control paths** — Execution Type: `Service or harness test`; Status: `Pass`. Offscreen forms opened, constructed their production controls, exercised report state/applicability, and closed without report artifacts.
- [x] **Historical findings and custom-script handling** — Execution Type: `Service or harness test`; Status: `Pass`. General resolution and severity behavior was exercised. Prior Blank/Broken IDs, threshold transitions, mapped-check behavior, and affected finding counts are superseded; the focused 7/7 Debug/Release suites now cover blank/nonblank Notes, resolution noncausality, and all four managed statistic families.
- [x] **Historical statistics tests** — Execution Type: `Service or harness test`; Status: `Pass`. Phase 10 exercised the then-current Blank, Broken, name, File Month, monetary, and database paths. The old exactly-50/no-Blank-Warning, above-50/Blank-Warning, any-positive-Broken/Failure, and read-only-derived Broken-denominator expectations are superseded; current corrected Auto/manual and threshold evidence is recorded in the passing focused gate above.
- [x] **Historical validation and status tests** — Execution Type: `Service or harness test`; Status: `Pass`. General incomplete-report blocking, stale-readiness invalidation, Created By fallback, and status-service outcomes remain useful. Current focused suites passed Auto/manual consistency, mapped Broken contradiction, mode fingerprinting, and corrected statistic report statuses.
- [x] **Historical PDF structural tests** — Execution Type: `Service or harness test`; Status: `Pass`. PDF boundaries, sections, footers, privacy reminders, and separate Blank/Broken tables were historically verified. Corrected denominators, finding severity/order, blank-Notes threshold-only output, nonblank-Notes contextual output, resolution-state independence, checklist state, and overall status are superseded as content evidence.
- [x] **Historical PDF visual and extraction tests** — Execution Type: `Service or harness test`; Status: `Pass`. The 2-, 4-, 3-, 3-, and 24-page Phase 10 outputs remain evidence for then-tested pagination. Representative corrected both-Warnings, mapped-Failure, and handled-mapped-Failure renders passed; the combined focused PDF visual gate remains `Not Run` only for the direct extracted-order assertion and rendered nonblank-Notes two-Failure variant.
- [x] **Historical paired-save tests** — Execution Type: `Service or harness test`; Status: `Pass`. Byte reuse/equality, transaction mechanics, and one-entry structure remain useful. Fresh isolated evidence for all seven corrected statuses, PDF contents, and QA index statuses is recorded in the passing focused gate above.
- [x] **Overwrite tests** — Execution Type: `Service or harness test`; Status: `Pass`. Cancellation wrote nothing; confirmation reconciled recognized same-key files to one current pair and one current index entry. Different QA Dates remained the same Hotel ID/File Month key.
- [x] **Rollback and fault tests** — Execution Type: `Service or harness test`; Status: `Pass`. Staging/commit failures, real file-in-use behavior, injected permission categorization, rollback failure/manual-review signaling, and cleanup-warning success semantics were exercised. Injected permission failure is not evidence of a real Windows ACL denial.
- [x] **Historical QA index tests** — Execution Type: `Service or harness test`; Status: `Pass`. Entry/key format, relative paths, strict malformed/duplicate handling, Created By/filename/timestamp, containment, and V1 separation remain useful. Blank/Broken-derived historical status values are superseded; the seven-scenario correction rerun passed without an index-format change.
- [x] **Path and filename safety** — Execution Type: `Service or harness test`; Status: `Pass`. Windows-invalid characters, reserved names, dot names, whitespace/control input, accents, long values, collision cases, similar Hotel IDs, structured parsing, and root containment were exercised.
- [x] **Privacy review** — Execution Type: `Source inspection`; Status: `Pass`. Forms, messages, metadata, index fields, PDF content rules, and documentation examples were reviewed; Phase 10 execution used synthetic data and no known privacy defect was found. Execution of the reviewed source paths is not implied by this source-inspection result.
- [x] **Known-issue review** — Execution Type: `Source inspection`; Status: `Pass`. Expected limitations are recorded below and must be acknowledged by pilot and release owners.

## Historical Phase 10 build and publish evidence

The Phase 10 build/publish owner recorded the completed results below after the historical source fixes. These results predate the P1 correction; the correction's separate current build, publish-inventory, and visible startup rows above are supported by their own evidence.

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

### Safe publish-output procedure for a future authorized correction verification

The existing ignored `PublishedApp` tree contains pre-existing/user-looking V1 documentation and must not be deleted, overwritten as test data, inspected for content, or reported as synthetic output.

1. Create and verify a new empty external output directory outside the repository, `PublishedApp`, frozen package, live Retry-01 root, pilot backups, and V1 data.
2. Confirm no dashboard process is running from any target or protected package.
3. Run the approved explicit Windows x64 self-contained single-file publish command into that external directory.
4. Confirm the output is Windows x64, self-contained, and single-file. Fail if source, metadata, `QAReportIndex.txt`, test PDFs, harness files, user settings, or transaction artifacts are included.
5. Record relative names, lengths, individual SHA-256 hashes, and aggregate size.
6. Start only the fresh executable visibly against isolated synthetic/no-save configuration, record the process ID, and stop only that process after the smoke check.
7. Remove only the verified external test output when safe. Do not modify the existing `PublishedApp` tree or designate this output as the pilot package.
8. Confirm no publish output is staged or tracked and no protected root changed.

## Pre-pilot gate

Complete every item in this section before authorizing the controlled pilot.

- [x] **Historical Phase 10 build/publish evidence is complete** — Execution Type: `Service or harness test`; Status: `Pass`. Debug, Release, publish, and the published-process startup probe passed at the Phase 10 checkpoint. Separate corrected Debug/Release builds, isolated publish inventory, and visible startup/no-save evidence also passed in the focused gate above.
- [x] **Final repository hygiene** — Execution Type: `Source inspection`; Status: `Pass`. The nine reviewed Phase 10 files were committed as `b0879645cde1b6a619accdea70abf7e08425dc22`, pushed only to `v2-qa-reports`, and independently inspected; no unexplained or generated artifact was included.
- [x] **Temporary verification cleanup** — Execution Type: `Source inspection`; Status: `Pass`. `.phase10-verification`, temporary PDF/render trees, synthetic scenario roots, publish-test output, transaction artifacts, and recovery-test data are absent; the protected pre-existing ignored publish tree was restored.
- [x] **Generated artifact review** — Execution Type: `Source inspection`; Status: `Pass`. No PDF, PNG, test metadata, QA index, user settings, QA runtime tree, publish output, harness source, test result, or coverage artifact is tracked.
- [x] **Final source-diff scope** — Execution Type: `Source inspection`; Status: `Pass`. Reviewed commit `b0879645cde1b6a619accdea70abf7e08425dc22` contains exactly four justified source fixes and five required Phase 10 documents. Project/package, `.gitignore`, MainForm, and V1 files are unchanged.
- [x] **Historical required documentation follow-up** — Execution Type: `Source inspection`; Status: `Pass`. The minor Git-state documentation correction was later committed as `d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc`. This historical check does not satisfy the current P1 documentation, evidence, or independent-review gate.
- [ ] **V1 visible desktop smoke plan accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner acknowledges that visible startup/close/reopen, required-field prompts, Preview/Submit, Change/Reset Folder, and Open File/Folder/Index were not directly clicked during Phase 10.
- [ ] **V2 visible desktop smoke plan accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner acknowledges that visible report entry, prompt text/defaults, cancellation, double-click protection, and end-to-end GUI saving remain pilot checks.
- [ ] **UI/scaling risk accepted for controlled pilot** — Execution Type: `Manual test pending`; Status: `Not Run`. Actual-125% visible no-save and permanent STA geometry/interaction passed, but real 100%/150%, screen-reader behavior, and full pilot interaction remain unverified.
- [ ] **Real ACL limitation accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. A real file lock was exercised; permission branches used injected exceptions and are not a real Windows ACL test.
- [ ] **Pilot backup created** — Execution Type: `Manual test pending`; Status: `Not Run`. Back up the configured documentation root and any executable-side `user-settings.json` before pilot activity without changing or deleting existing V1 logs.
- [ ] **Privacy briefing complete** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot users understand the prohibited data list and the approved use of synthetic or explicitly approved internal report-level summaries.
- [ ] **Stop/rollback contacts assigned** — Execution Type: `Manual test pending`; Status: `Not Run`. Record the pilot owner, defect contact, backup owner, and person authorized to reconcile a manual-review transaction state.
- [ ] **Known limitations accepted** — Execution Type: `Manual test pending`; Status: `Not Run`. Pilot owner reviews the limitation list below and accepts only controlled, monitored use.

Current technical recommendation: keep the frozen pilot and Scenario 3 on hold; send only the new immutable production candidate to independent review. Real 100%/150% DPI, merge, tag, and release remain open.

## Controlled pilot execution

Controlled internal pilot activity exposed the P1 defect. Further affected execution is on hold. Do not resume Scenario 3 or use approximately 10–15 reports until the entire P1 gate and a separate pilot-resumption authorization are complete; when authorized, record each attempt in the pilot log described by `V2-Pilot-Guide.md`.

- [ ] **Pilot authorization recorded** — Execution Type: `Manual test pending`; Status: `Not Run`. Authorization is for a controlled pilot only, not a production release.
- [ ] **Pilot environment and backup verified** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm the configured root, backup, write access, available disk space, and V1 log access before the first QA report.
- [ ] **Multiple Hotels and PMS systems** — Execution Type: `Manual test pending`; Status: `Not Run`. Use multiple approved Hotels and multiple PMS systems and confirm canonical Hotel/PMS selection.
- [ ] **Monetary scenarios** — Execution Type: `Manual test pending`; Status: `Not Run`. Include one, two, and more-than-two monetary-column cases and verify checklist/statistics applicability.
- [ ] **Status scenarios** — Execution Type: `Manual test pending`; Status: `Not Run`. After authorization, save corrected zero, Blank Warning, Broken Warning, combined Warnings, Blank Failure, Broken Failure, and handled Broken Failure reports and verify `Pass`, `Pass with Warnings`, and `Fail` outcomes.
- [ ] **Custom-script scenario** — Execution Type: `Manual test pending`; Status: `Not Run`. Handle one statistics Failure independently; confirm Failure severity and Failed Checks placement remain while the overall status becomes Pass with Warnings when no Active Failure remains.
- [ ] **Overwrite cancellation** — Execution Type: `Manual test pending`; Status: `Not Run`. Decline the default-No overwrite prompt and confirm both PDFs and the QA index are byte-for-byte unchanged.
- [ ] **Overwrite confirmation** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm replacement and verify one current Hotel copy, one current PMS copy, and one current index entry for the Hotel ID/File Month key.
- [ ] **Paired PDF equality** — Execution Type: `Manual test pending`; Status: `Not Run`. Compare length and SHA-256 of the Hotel and PMS copies for every saved pilot report.
- [ ] **QA index review** — Execution Type: `Manual test pending`; Status: `Not Run`. Confirm one entry per key, relative paths, correct status/effective Created By/filename/timestamp, and no absolute local or guest-level content.
- [ ] **PDF visual review** — Execution Type: `Manual test pending`; Status: `Not Run`. Open representative short and long reports and inspect wrapping, sections, grayscale readability, footer/page totals, special characters, and absence of blank trailing pages.
- [ ] **Visible UI review at 100%** — Execution Type: `Direct GUI test`; Status: `Not Run`. Inspect initial, maximized, and 880x600 states with maximum applicability/dynamic hide-show; record containment, scrolling, mouse hit-testing, and screenshots.
- [x] **Visible UI review at 125%** — Execution Type: `Direct GUI test`; Status: `Pass`. Accepted nine-image no-save matrix plus permanent STA interaction evidence recorded.
- [ ] **Visible UI review at 150%** — Execution Type: `Direct GUI test`; Status: `Not Run`. Repeat the complete size/state, clipping, intersection, scrollbar, focus, mouse, and reachability matrix.
- [ ] **Keyboard and prompt review** — Execution Type: `Direct GUI test`; Status: `Not Run`. Verify forward/reverse Tab order, Enter/Escape, bottom/top scrolling, owned dialogs, safe overwrite defaults, prompt consequences, and rapid double-click behavior.
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
- Phase 10 had no permanent automated test project. The correction adds permanent focused semantic, STA geometry/interaction, and save/PDF/index coverage that passed in Debug and Release; the project is not included in the solution and must be built/run explicitly.
- Actual-125% visible no-save and STA keyboard/navigation coverage passed. Real 100%/150%, screen-reader behavior, prompt interaction, and subjective pilot usability remain pending.
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
- [ ] **Final P1 correction commit and clean branch state** — Execution Type: `Source inspection`; Status: `Not Run`. After explicit commit authorization, require the exact independently reviewed correction commit on `v2-qa-reports`, with no unexplained/generated artifacts, then record its full hash. Do not infer authorization to push.
- [ ] **Post-commit identity/review confirmation** — Execution Type: `Source inspection`; Status: `Not Run`. The local production/test diff and evidence passed independent code review with no P0/P1 blocker, but no authorized correction commit exists yet to compare against that reviewed state. After commit authorization, confirm exact identity and record the reviewed commit hash; this grants no package, pilot, merge, tag, or release authority.
- [ ] **Explicit merge approval** — Execution Type: `Manual test pending`; Status: `Not Run`. Obtain a separate instruction authorizing merge to `main`.
- [ ] **Main-branch merge** — Execution Type: `Manual test pending`; Status: `Not Run`. Do not merge during the Phase 10 working run.
- [ ] **Post-merge Debug and Release builds** — Execution Type: `Service or harness test`; Status: `Not Run`. Run both exact build commands against the approved merged commit and record results.
- [ ] **Post-merge single-file publish and startup** — Execution Type: `Service or harness test`; Status: `Not Run`. Repeat safe publish preservation, clean-output inspection, and startup verification.
- [ ] **Release approval** — Execution Type: `Manual test pending`; Status: `Not Run`. Obtain explicit release authorization after post-merge evidence is reviewed.
- [ ] **Release tag creation** — Execution Type: `Manual test pending`; Status: `Not Run`. Create `v2.0.0` only if separately authorized; no V2 tag exists from Phase 10.
- [ ] **Release notes finalized** — Execution Type: `Source inspection`; Status: `Not Run`. Convert the draft only after approved commit/tag/version details and final limitations are known.
- [ ] **Release artifact published** — Execution Type: `Manual test pending`; Status: `Not Run`. Do not distribute or announce Version 2 before release approval.
- [ ] **Branch-retention decision** — Execution Type: `Manual test pending`; Status: `Not Run`. Decide whether and how to retain `v2-qa-reports`; do not delete it as part of Phase 10.

Current broad-release recommendation: **Not ready for merge and release**. Real 100%/150% scaling, correction-specific V1 regression, commit/package/pilot authorization, controlled-pilot completion, explicit merge approval, post-merge verification, release approval, tag creation, and publication are still pending.

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
- P1 correction commit: Pending explicit commit authorization; independent code review passed and no commit or push is claimed
- Explicit merge approval reference:
- Release approval reference:
- Final approved commit: Pending the authorized P1 correction commit and explicit approval
- Release tag, if separately authorized:
- Branch-retention decision:

Until these fields and the applicable gates are completed, Version 2 remains unreleased on `v2-qa-reports`.
