# V2 Controlled Pilot Guide

## Purpose and current status

This guide defines a small, supervised pilot of the Version 2 QA Report workflow. The pilot is intended to verify the complete operator experience with approximately 10-15 QA reports before any merge or release decision. It is not a production rollout, and this document does not claim that the pilot has occurred.

The candidate remains on `v2-qa-reports`. Phase 10 started from the approved Phase 9 baseline `5ff47c4ddfd54cf38cb70fd9e4cca65a2fd793b1`. No commit or push occurred during the original Codex validation run; the completed implementation was later committed as `b0879645cde1b6a619accdea70abf7e08425dc22`, pushed only to `v2-qa-reports`, and independently inspected with the result `Phase 10 approved with minor documentation corrections`. It has not been merged, tagged, or released, and the controlled pilot has not occurred.

The final production rebuild without the temporary friend harness, exact Debug build, and exact Release build passed with 0 warnings and 0 errors. The repository wrapper produced a fresh Windows x64 self-contained single-file output containing only `DocumentationLoggingDashboard.exe`, `DocumentationLoggingDashboard.pdb`, and `appsettings.json`; the executable remained running through the published smoke interval. This makes the package technically build/publish ready for a controlled pilot with the guardrails below. The pilot coordinator must record the exact executable hash and map the pilot executable to the final reviewed documentation-correction commit after that commit is created, pushed to `v2-qa-reports`, and independently inspected.

Phase 10 service/harness validation completed 368 assertions with 0 failures after four narrow hardening corrections. Offscreen WinForms construction also passed. Those results do not replace a direct visible-GUI test: normal visible operation, 125%/150% scaling, confirmation dialogs, message boxes, and keyboard navigation remain manual pilot checks.

## Pilot ownership and guardrails

Assign one pilot coordinator, one or more trained QA operators, and one technical owner who can preserve evidence and assess storage errors. The coordinator owns the scenario log and stop decisions. The technical owner owns backups and any explicitly approved recovery.

Run only one application process against a pilot documentation root at a time. The save workflow has an in-process guard but no cross-process save lock. Do not use two published copies, two remote sessions, or two users against the same root concurrently.

Use a dedicated pilot root and keep it separate from Phase 10 temporary data. Prefer synthetic data for training and the first smoke scenarios. Approved real internal use may follow only with the pilot sponsor's authorization and the privacy rules below. The application records manual summaries; it must not be used to ingest or attach a full hotel file.

## Privacy requirements

Pilot metadata may contain only the approved Hotel ID, Hotel Name, PMS Name, and generated safe folder names. Report content may contain non-sensitive QA summaries and aggregate counts only.

Never enter, paste, capture, or attach:

- guest names, email addresses, phone numbers, or other personal identifiers;
- payment information, credentials, tokens, or connection details;
- production reservation records, raw rows, or full hotel-file contents; or
- screenshots, error text, or defect attachments that expose any of those values.

Use synthetic names such as `Example Hotel`, `TEST-001`, `Example PMS`, and `synthetic-example.csv` for training. If approved real internal identifiers are used later, keep findings descriptive and aggregate. Redact screenshots before attaching them to a defect. Record storage paths relative to the documentation root in reusable pilot records; do not copy a user's absolute local path into a PDF, QA index, release note, or shared defect template.

A suspected privacy leak is a stop condition. Preserve the affected artifact securely, do not distribute it, and notify the pilot coordinator and privacy owner immediately.

## Backup and environment preparation

Before the first pilot run:

1. After the documentation-only correction commit is created, pushed to `v2-qa-reports`, and independently inspected, record the candidate executable's SHA-256 hash, build date, branch, and that final reviewed commit. Map the pilot executable to that commit; do not identify an unreviewed worktree or correction commit as a release build.
2. Confirm that the package supplied to the pilot is the same package produced by the recorded successful Debug build, Release build, and repository publish wrapper. Confirm the published directory contains the application executable, its PDB, and `appsettings.json`, and contains no source, test metadata, QA index, test PDF, or temporary transaction artifact.
3. Start the published executable visibly on the pilot workstation. Confirm the main dashboard opens at 100% scaling and that both V1 logging and V2 QA entry points are reachable.
4. Where practical, repeat the layout check at 125% and 150% scaling. Record clipping, overlap, scrolling, tab order, Enter/Escape behavior, and message-box clarity. Do not mark a scaling level complete if it was not directly observed.
5. Close the application before taking the initial backup. Copy the entire selected documentation root to a timestamped, access-controlled backup. At minimum, preserve the V1 `Index\LogIndex.txt`, all V1 log folders, and the complete `QAReports` tree if it already exists.
6. Verify that the backup can be listed and that its file count and hashes for the V1 index, QA metadata, QA index, and existing QA PDFs match the source. Do not test restore by overwriting the live root.
7. Confirm adequate free space and write access using a non-sensitive pilot folder. Do not weaken filesystem permissions to make the application run.
8. Record the documentation root in the restricted pilot log. Confirm V2 storage resolves below `QAReports\ByHotel`, `QAReports\ByPMS`, `QAReports\Metadata`, and `QAReports\Index`.
9. Capture the pre-pilot hash and modified time of the V1 `Index\LogIndex.txt`. V2 report activity must not change it.

Take an additional closed-application backup before the overwrite exercise and at the end of each pilot session. Retain backups according to the organization's internal retention and access rules. Do not place a backup inside the Git repository or the published application folder.

## Metadata setup

Use at least three PMS systems and four Hotels so selectors, canonical relationships, and duplicate display behavior receive real operator coverage. Add each PMS before adding a Hotel that references it. Include two Hotels with the same display name but different Hotel IDs, and verify the selector keeps them distinguishable.

For each add operation, record the intended values and the displayed result. Verify trimmed, case-insensitive duplicates are refused; a Hotel cannot reference a nonexistent PMS; search works with partial Hotel ID, Hotel Name, and PMS text; and closing/reopening the forms preserves the collection and each canonical Hotel-to-PMS relationship.

Do not edit `hotels.json` or `pms-systems.json` by hand. Metadata editing and deletion are not pilot features.

### If metadata is corrupt or unreadable

Normal loading deliberately does not reset corrupt metadata. If an invalid, unsupported, empty, missing, or inaccessible metadata message appears:

1. Stop V2 pilot activity; do not repeatedly add records or generate a report.
2. Close the application and preserve the complete `QAReports\Metadata` directory, including file hashes and the exact non-sensitive error text.
3. Do not rename, delete, hand-edit, or replace the affected JSON file, and do not copy another environment's metadata over it.
4. Report the incident to the technical owner. The owner must first determine whether the problem is access, storage, schema version, relationship validation, or malformed content.
5. Use the production service's explicit recovery operation only after the owner approves losing the affected metadata collection. That operation backs up the selected known metadata file under `QAReports\Metadata\Backups` before replacing it with an empty valid document.
6. Verify the recovery backup exists before rebuilding approved metadata through normal add operations. If backup or recovery fails, stop and preserve the root for manual review.

Recovery is destructive to the active metadata collection even though a backup is created. It is not an automatic repair and must not be used casually during the pilot.

## Recommended 12-scenario plan

Complete 12 logical report scenarios, then perform one cancel-and-confirm overwrite exercise against P-01. The final QA index should still contain 12 logical keys. The same plan can be executed entirely with synthetic data; after the training cases, approved real internal identifiers and aggregate summaries may be substituted only with sponsor authorization.

| Pilot ID | Hotel / PMS allocation | Monetary shape | Intended result | Required focus |
| --- | --- | --- | --- | --- |
| P-01 | Hotel A / PMS 1 | One monetary column | Pass | All applicable Raw and DB checks pass; no warning notes. This is the later overwrite key. |
| P-02 | Hotel B / PMS 1 | Two monetary columns | Pass | Both monetary-column branches and their statistics are completed. |
| P-03 | Hotel C / PMS 2 | More than two monetary columns | Pass with Warnings | Confirm the approved characteristic Warning remains separate from Failures and is resolved explicitly. |
| P-04 | Hotel D / PMS 3 | One monetary column | Pass with Warnings | Add a note to a passed check, review its Warning, and mark it Explained and Accepted. |
| P-05 | Hotel A / PMS 1 | Two monetary columns | Fail | Fail one Raw File QA check and leave the Failure accurately represented in the PDF. |
| P-06 | Hotel B / PMS 1 | One monetary column | Fail | Fail one DB QA check and resolve that finding as Handled by Custom Script; verify its original Failure severity and Fail status remain unchanged. |
| P-07 | Hotel C / PMS 2 | Two monetary columns | Pass | Exercise separate-name-column applicability and confirm full-name-only checks become N/A. |
| P-08 | Hotel D / PMS 3 | One monetary column | Pass | Exercise full-name-column applicability and confirm separate-name checks become N/A. |
| P-09 | Hotel A / PMS 1 | One monetary column | Pass with Warnings | Exercise the rejected-records branch and its approved Warning/statistics rules. |
| P-10 | Hotel B / PMS 1 | Two monetary columns | Fail | Combine a failed checklist result with Blank Value and Broken Data Statistics; verify the two statistic tables remain distinct. |
| P-11 | Hotel C / PMS 2 | More than two monetary columns | Pass with Warnings | Leave Created By blank and verify the effective value is `InnoVarxi QA Team` in the PDF and index. |
| P-12 | Hotel D / PMS 3 | One monetary column | Fail | Use several Warnings and Failures to exercise long lists, scrolling, per-finding resolution, PDF pagination, and section separation. |

Use distinct File Months where needed so P-01 through P-12 are distinct logical keys. The key is Hotel ID plus File Month; QA Date and Hotel display name do not create a separate key.

## Per-report procedure

For every scenario:

1. Record the Pilot ID, operator, date/time, executable hash, Hotel ID, Hotel Name, canonical PMS, File Month, QA Date, and whether the data is synthetic or approved internal.
2. Open a new QA Report and confirm the intended Hotel search result identifies the correct Hotel ID and canonical PMS.
3. Set file characteristics and record which checklist items become applicable or N/A. Do not change stable checklist meanings to force an intended result.
4. Complete all applicable Raw File QA and DB QA items manually. Enter only non-sensitive summary notes.
5. Enter applicable statistics. Check calculations and safe zero-denominator behavior against an independent simple calculation.
6. Review Warnings and Failed Checks in their separate areas. Resolve each finding individually. When a custom script is used, record only the approved script identifier and non-sensitive resolution summary.
7. Validate readiness and record the expected and actual status: Pass, Pass with Warnings, or Fail. A handled Failure remains a Failure and keeps the report at Fail.
8. Generate and save once. Do not double-click the save control. Record the final filename and whether any overwrite prompt appeared.
9. Verify the two PDF copies are identical and verify the QA index as described below.
10. Open the PDF and inspect headings, checklist applicability, statistics, finding severity and resolution, status, page numbers, privacy footer, wrapping, and the absence of clipping or overlap.
11. Close the QA Report form and reopen a new one. Do not expect an incomplete form to persist as a draft.

## Verify identical Hotel and PMS PDF copies

The final filename must be identical in both canonical destinations. Compare file length and SHA-256; matching names alone are insufficient. With paths selected from the QA index, a pilot verifier can use:

```powershell
$hotelCopy = Join-Path $documentationRoot 'QAReports\ByHotel\HOTEL_FOLDER\REPORT.pdf'
$pmsCopy = Join-Path $documentationRoot 'QAReports\ByPMS\PMS_FOLDER\REPORT.pdf'

Get-Item -LiteralPath $hotelCopy, $pmsCopy |
    Select-Object FullName, Length, LastWriteTimeUtc

Get-FileHash -Algorithm SHA256 -LiteralPath $hotelCopy, $pmsCopy |
    Select-Object Path, Hash
```

Pass only when both files exist, have the expected filename and nonzero identical length, and have the same SHA-256 hash. Record the hash once in the restricted pilot log. If a copy is missing or hashes differ, do not retry over the evidence; stop the pilot and preserve the pair and index for technical review.

## Verify the QA Report index

Inspect `QAReports\Index\QAReportIndex.txt` after each successful save. For the current logical key, verify:

- exactly one entry exists for the Hotel ID and File Month;
- QA Date, canonical Hotel Name, Hotel ID, PMS, status, effective Created By, filename, and UTC save timestamp match the saved report;
- `Hotel Copy` begins with `ByHotel\` and `PMS Copy` begins with `ByPMS\`;
- both copy paths are relative to `QAReports`, contain the same filename, and resolve to the two existing PDFs;
- no drive-qualified, UNC, parent-traversal, or user-profile path appears; and
- the separate V1 `Index\LogIndex.txt` hash and modified time remain unchanged by the V2 save.

Do not hand-edit, truncate, or regenerate the QA index. A malformed index, duplicate logical key, missing-file reference, unexpected extra entry, or absolute/escaping path is a stop condition. Preserve the index and referenced files for review; the application intentionally has no automatic index repair.

## Overwrite exercise

Perform this only after P-01 has been verified and a closed-application backup has been taken.

1. Reopen the same Hotel ID and File Month as P-01, but use a different synthetic QA Date and deliberately changed non-sensitive report content.
2. Record hashes, timestamps, and the P-01 index block before attempting the save.
3. Generate the replacement and review the overwrite warning. It should identify that an existing logical report is being replaced. If the dialog is ambiguous, does not default safely, or reports an unexpected number of matching copies, cancel and stop.
4. Cancel the first prompt. Verify both original PDF hashes, timestamps, file counts, and the complete index bytes are unchanged.
5. Repeat only after a second person confirms the selected Hotel ID and File Month. Confirm the overwrite once.
6. Verify exactly one current Hotel copy, one current PMS copy, and one index entry remain for the logical key. Verify the new filename reflects the new QA Date, the new copies have identical SHA-256 hashes, and the index reports the new status and effective Created By.
7. Verify no `.tmp`, staging, rollback, or transaction backup artifact remains after normal success.

If the result reports manual review, rollback failure, cleanup warning, or an inconsistent pair, do not attempt another overwrite. Close the application, preserve the root, and escalate.

## Preserve V1 access

Before V2 scenarios, create one synthetic V1 entry in each of the Debugging Log, Script Editing Log, and Script Creation Log workflows. Verify Preview, Submit, daily file content, automatic ID, and V1 index update. Repeat a small V1 smoke test after the overwrite exercise.

If V2 initialization, metadata loading, report validation, PDF generation, or saving reports an error, dismiss the error without changing the documentation root and return to the main dashboard. Verify that the V1 log selector and V1 Preview/Submit workflow remain available. Do not delete the `QAReports` directory to restore V1 access.

A V2 failure may be isolated while operators continue approved V1 work only if the V1 smoke test passes and the coordinator authorizes that limited continuation. Any V1 crash, output-format change, index corruption, or loss of access stops the pilot.

## What pilot users must record

Maintain a restricted pilot log containing:

- candidate executable hash, workstation Windows version, display scaling, and operator;
- each Pilot ID, synthetic/approved-internal classification, Hotel ID, PMS, File Month, and QA Date;
- expected and actual checklist applicability, finding counts, status, and effective Created By;
- final filename, relative Hotel/PMS copy paths, shared PDF hash, and matching index evidence;
- overwrite prompt choice and pre/post hashes for the cancellation and confirmation attempts;
- visible layout, scrolling, tab order, keyboard, message-box, and double-click observations;
- time to complete the report and any confusing label or instruction;
- defect reference, severity, disposition, retest result, and whether the run was stopped; and
- end-of-session backup identifier and V1 smoke-test result.

Do not put raw report rows, guest data, credentials, absolute user paths, or unredacted sensitive screenshots in this log.

## Defect reporting

Report a defect with a concise title and include the Pilot ID, executable hash, Windows/scaling environment, exact preconditions, minimal steps, expected result, observed result, and whether retrying could overwrite evidence. Attach only redacted screenshots and synthetic reproductions. Refer to files by path relative to the documentation root and provide hashes instead of copying sensitive content.

Classify privacy exposure, path escape, V1 corruption, false save success, partial Hotel/PMS state, index corruption, unintended overwrite, unrecoverable rollback, or data loss as blocking. Preserve the full pilot root and backup; do not manually repair it before the technical owner captures evidence. Nonblocking usability issues still require triage before release.

## When to stop the pilot

Stop V2 activity immediately if any of the following occurs:

- sensitive or prohibited data appears in metadata, an error, a PDF, the index, or a defect artifact;
- a generated path leaves the configured documentation root, or an index path is absolute or contains traversal;
- a save reports success without two identical copies and one correct index entry;
- only one copy exists, the two hashes differ, an unrelated file changes, or transaction artifacts remain after normal success;
- overwrite cancellation changes a file or the index, or confirmation targets the wrong logical key;
- rollback/manual-review text appears, a prior report cannot be restored, or cleanup state is unclear;
- metadata or the QA index is malformed, silently reset, or unexpectedly duplicated;
- V1 becomes inaccessible or its file/index contract changes;
- clipping, overlap, scrolling, focus, keyboard behavior, or a dialog prevents accurate entry or a safe decision;
- the app crashes, hangs, opens duplicate save actions, or more than one process is using the pilot root; or
- the verified backup is unavailable.

Do not convert a stop condition into a Pass by retrying. Preserve evidence and wait for a reviewed correction and focused retest.

## Pilot exit criteria

The pilot is complete only when all of the following are recorded:

- 12 logical scenarios were completed across multiple Hotels and multiple PMS systems;
- one-, two-, and more-than-two-monetary-column behavior was directly observed;
- Pass, Pass with Warnings, Fail, and a custom-script-handled finding were directly observed with severity preserved;
- all 12 current pairs are byte-for-byte identical and exactly one valid relative-path index entry exists for each logical key;
- overwrite cancellation changed nothing and overwrite confirmation safely replaced only P-01;
- metadata add, duplicate refusal, search, canonical PMS, persistence, and the documented corrupt-metadata stop/recovery path were reviewed;
- visible GUI behavior at 100% and, where practical, 125% and 150% was recorded, including prompts, keyboard navigation, scrolling, and long findings/statistics lists;
- V1 smoke tests passed before and after V2 activity;
- no unresolved privacy, path-safety, data-loss, rollback, index, paired-save, overwrite, or V1 defect remains;
- all other defects were triaged, retested where corrected, and accepted by the pilot coordinator;
- final backups and the restricted pilot log were verified; and
- the QA owner, technical owner, and release owner reviewed the outcome.

Completing these criteria does not itself approve a merge, tag, or release. Those actions require a separate review of the final committed and pushed branch plus explicit approval.
