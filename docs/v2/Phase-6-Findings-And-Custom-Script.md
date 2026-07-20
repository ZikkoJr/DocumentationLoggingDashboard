# Phase 6 Findings Generation and Custom Script Resolution

## Purpose and boundaries

Phase 6 extends the existing Phase 5 `QaReportForm` with in-memory finding generation and per-finding resolution. Failed checklist results produce Failure findings, passed checklist results can carry a separate user-selected Warning, and three file-characteristic conditions produce automatic Warnings. The form displays Warning and Failure severities separately while retaining handled findings in their original severity groups.

This phase remains an incomplete draft workflow. It does not calculate report status or completion, validate final completion, collect or evaluate statistics, generate statistic findings, generate a Created By fallback, create a Report ID, parse a raw file or spreadsheet, access a database, run automation, generate a PDF, save a report, or write a QA Report index entry. Close and Escape still discard the form instance without persistence.

> **Post-Phase-10 pilot correction:** The paragraphs above accurately describe the historical Phase 6 boundary. The current `QaFindingSynchronizationService` has since been extended by Phase 7 and the controlled-pilot correction. It now manages all four Blank/Broken threshold ID families while preserving the Phase 6 identity and resolution lifecycle. Final focused semantic automation passed 7/7 in both Debug and Release, including finding-resolution noncausality and the Notes-conditioned mapped-Failure paths. The authoritative correction evidence and remaining P2 Notes-discriminator risk are in `Pilot-Correction-Blank-Broken-Statistics.md`; historical Phase 6 verification is not evidence for the corrected statistic thresholds.

> **Deferred-text pilot correction:** Finding Custom Script Name and Resolution Notes now remain control-local drafts while typing. `Validated`, resolution transitions, readiness/save, tab/workflow transitions, and form closing flush the normalized value. Same-ID refreshes do not overwrite a dirty draft, and a multi-finding action flush performs one final synchronization/refresh. Resolution/severity/ID and approved clearing rules are unchanged. Final verification is recorded in `Pilot-Correction-Deferred-Text-Commit.md` and `V2-Production-Readiness.md`.

The corrected statistic finding families are:

```text
WARN:STAT:BLANK:<FieldId>
FAIL:STAT:BLANK:<FieldId>
WARN:STAT:BROKEN:<FieldId>
FAIL:STAT:BROKEN:<FieldId>
```

A Warning/Failure threshold transition changes the deterministic ID. The old finding and its resolution are removed, and the replacement starts Active. Unrelated edits preserve resolution only while the same ID remains expected. A mapped Broken result above 50% uses the canonical statistics Failure. Its directly related generic checklist Failure is suppressed only when the current failing checklist result has blank Notes after trimming, which represents the threshold-only path. Nonblank checklist Notes document a separate contextual checklist defect, so both Failures remain. Finding resolution state alone is not causal and does not control suppression.

This Notes-based distinction is a known residual risk, not a new domain contract. The model has no explicit checklist-failure cause marker, so a separate defect recorded with blank Notes can be mistaken for the threshold-only path. Operators should enter nonblank contextual Notes when a mapped checklist Failure documents an independent defect. Adding a cause marker would require separate approval because it changes the model/domain contract.

## Repository baseline

- Required and active branch: `v2-qa-reports`.
- Approved Phase 5 baseline and starting commit: `8e5d1ff3b4a7ddfb0f70cddc7fbc4f9426ea6fb7`.
- Phase 5 implementation commit: `e02b323d8a0897b40e60607bb31ce6b2d98ce78f`.
- Phase 6 implementation commit: `2c599ea3ca0be6c5c434de625a95ef8c788aabd0`.
- Phase 4 commit: `3606194f60ae9b8c54206408a934b71ce54f2239`.
- Phase 3 full commit: `1fff629aff29356fc80b91fc220cdaaa1588054a`.
- Phase 2 commit: `7d94a27a4569f8ca521aeaa6c08da0e510fc4dc7`.
- Phase 1 commit: `49beaa1f45700725d328ade215419254560cb406`.

All required ancestry checks succeeded before editing. The local branch and `origin/v2-qa-reports` both pointed to the starting commit, and the worktree was clean. No commit or push occurred before implementation or before explicit instruction. The completed Phase 6 implementation was subsequently committed and pushed only to `v2-qa-reports` as commit `2c599ea3ca0be6c5c434de625a95ef8c788aabd0`. No merge, rebase, history rewrite, or tag modification occurred.

## Files added and modified

Added:

- `DocumentationLoggingDashboard/QAReports/Definitions/QaFindingIds.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaFindingSynchronizationService.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaFindingItemControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaFindingItemControl.Designer.cs`
- `docs/v2/Phase-6-Findings-And-Custom-Script.md`

Modified:

- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.Designer.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaChecklistItemControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaChecklistItemControl.Designer.cs`

No MainForm, project, package, Phase 2 contract, Phase 3 service/schema, Phase 4 metadata form, protected V1 model, or protected V1 service was changed.

## Finding synchronization architecture

`QaFindingSynchronizationService` is the single rule owner for Phase 6 generated findings. It accepts either the approved catalog or a validated definition snapshot and operates directly on the existing `QaReport`, `QaCheckResult`, `QaFinding`, `QaCheckDefinition`, and `QaFileCharacteristics` objects.

Its main operation is:

```csharp
void SynchronizeFindings(QaReport report)
```

Because `QaCheckResult` deliberately has no warning flag, the service also exposes the narrow UI-boundary operation:

```csharp
void SetChecklistWarningSelected(
    QaReport report,
    string checkId,
    bool selected)
```

The deterministic `WARN:<ChecklistId>` finding itself is the authoritative manual-warning state. The checklist checkbox never writes a duplicate model flag. Selecting or clearing the checkbox adds or removes that deterministic finding through the service, and every UI refresh reconstructs checkbox state from `CurrentReport.Findings`.

Synchronization performs these steps:

1. Validate the definition snapshot and exact checklist-result set.
2. Snapshot current findings by ID with ordinal comparison.
3. Reject null findings, blank IDs, and duplicate IDs clearly.
4. Build the expected generated Failure, passed-check Warning, and characteristic-Warning set.
5. Reuse the same existing `QaFinding` object while its deterministic condition remains continuously present.
6. Refresh generated identity, severity, title, description, related check, and source fields from current authoritative inputs.
7. Preserve only valid resolution, custom-script name, and resolution notes.
8. Remove stale managed findings entirely, without a cache, tombstone, or archive.
9. Retain unrelated non-Phase-6 findings without reading statistics or manufacturing their source conditions.
10. Normalize custom-script availability and invalid Failure resolution combinations.
11. sort deterministically by ordinal finding ID;
12. clear and repopulate the existing `report.Findings` list in place; and
13. revalidate final nonblank, unique IDs.

The service does not access `QaReport.Statistics`, assign `QaReport.ReportStatus`, change a checklist result, or perform any persistence.

## Deterministic finding IDs

`QaFindingIds` validates nonblank checklist IDs and produces human-inspectable IDs:

```text
FAIL:<ChecklistId>
WARN:<ChecklistId>
WARN:CHARACTERISTIC:FULL_NAME
WARN:CHARACTERISTIC:MORE_THAN_TWO_MONETARY_COLUMNS
WARN:CHARACTERISTIC:MULTIPLE_CONFIRMATION_CANDIDATES
```

No GUID is generated during form interaction. Existing Phase 2 checklist IDs remain unchanged.

## Failure generation

Every checklist result with `QaCheckStatus.Fail` produces exactly one `FAIL:<ChecklistId>` finding using:

- the matching catalog definition rather than duplicated wording;
- `QaFindingSeverity.Failure`;
- `QaFindingResolution.Active` for a new occurrence;
- `QaFindingSource.Checklist`;
- title `Failed check: <catalog display name>`; and
- trimmed checklist notes as the description, falling back to the catalog description when notes are blank.

Pass, NotEvaluated, or NotApplicable removes the Failure and all state stored on that object. A later Fail creates a new Active finding and does not restore the old script name or resolution notes. Handling a Failure does not change its checklist status or original severity and does not move it out of Failed Checks.

## Passed-check Warning workflow and checklist notes

`QaChecklistItemControl` now adds:

- one `Warning Found` checkbox;
- one multiline `Check notes / warning explanation` field; and
- a nonblocking prompt when Warning Found is selected with a blank explanation.

The single notes field binds directly to the existing `QaCheckResult.Notes`. Text is trimmed, and blank or whitespace-only text becomes `null`. It supplies Failure context when the check fails and the Warning description when a passed warning is selected.

The Warning checkbox is enabled only for an applicable Pass. Pass does not select it automatically. A selected applicable Pass produces one `WARN:<ChecklistId>` finding while the result remains Pass. Clearing the checkbox removes the Warning. Fail, NotEvaluated, or NotApplicable also removes it. A warning-to-Fail transition replaces the Warning with a Failure and retains the shared notes as Failure context. N/A clears notes, evaluation state, status, and warning UI; reapplying starts at NotEvaluated with no stale Warning.

A blank passed-warning explanation remains temporarily valid in this phase. The prompt is visible, but Close and Escape are not blocked.

## Automatic characteristic Warnings

Phase 6 generates exactly these automatic characteristic Warnings:

| Condition | Finding ID | Related checklist ID | Behavior |
| --- | --- | --- | --- |
| `NameColumnMode == FullName` | `WARN:CHARACTERISTIC:FULL_NAME` | `RAW.REQUIRED.NAME_FIELD_PRESENT` | Reports use of one Full Name field; no name check is automatically failed. |
| `MonetaryColumnScenario == MoreThanTwoMonetaryColumns` | `WARN:CHARACTERISTIC:MORE_THAN_TWO_MONETARY_COLUMNS` | `RAW.REQUIRED.MONETARY_VALUE_PRESENT` | Records the special-review condition; no checklist result is automatically failed. |
| `HasMultipleConfirmationNumberCandidateColumns` | `WARN:CHARACTERISTIC:MULTIPLE_CONFIRMATION_CANDIDATES` | `RAW.CONFIRMATION.CANDIDATES_REVIEWED` | Records multiple candidates while the review check may still pass. |

All three use Warning severity and Checklist source. Removing the characteristic removes the finding. Re-enabling it creates a new Active finding. The Phase 5 more-than-two informational label now says that the warning is recorded in Findings.

No blank-value, broken-data, multiword-name, File Month, high Stay Value, unusual monetary value, row-count difference, rejected-record count, missing-DB-value count, or other statistic finding is generated. Those rules remain deferred to Phase 7.

## Findings UI and severity separation

The existing Report Details, Raw File QA, and DB QA tabs remain. A final Findings tab contains two independently scrolling groups:

- Warnings
- Failed Checks

Empty groups display `No warnings` or `No failed checks`. Rows are selected for a group only by `QaFinding.Severity`, never by resolution. Therefore:

- handled Warnings stay in Warnings;
- explained Warnings stay in Warnings;
- handled Failures stay in Failed Checks; and
- resolution never converts a Failure into a Warning.

Dynamic rows are keyed by deterministic ID with ordinal comparison. Existing rows are rebound to the same finding objects, removed rows are detached and disposed, and event handlers are attached once when a control is created. Repeated refreshes do not accumulate controls or subscriptions. The permanent Close row remains outside every scrolling panel.

Each `QaFindingItemControl` binds directly to one existing `QaFinding` and displays original severity, title, description, related catalog display name and stable ID, current resolution, optional script name, and resolution notes.

## Per-finding resolution and custom-script availability

Warnings allow:

- Active
- Handled by Custom Script
- Explained and Accepted

Failures allow only:

- Active
- Handled by Custom Script

Explained and Accepted is hidden for Failures. If invalid external state places a Failure in that resolution, synchronization returns it to Active and clears incompatible script and resolution fields.

When `CurrentReport.FileCharacteristics.IsCustomScriptSupportAvailable` is false:

- each Handled by Custom Script option is disabled;
- no finding remains handled by custom script;
- handled findings return to Active;
- script names and script-specific resolution notes are cleared;
- findings remain visible;
- original severity and checklist status remain unchanged; and
- Explained and Accepted Warnings remain valid.

When support is true, each row enables its own independent handled decision. Merely enabling support handles nothing. A handled row stores trimmed optional values directly in the existing `CustomScriptName` and `ResolutionNotes` properties. Selecting another resolution clears incompatible script-specific values. Handling Finding A does not affect Finding B.

Explained and Accepted preserves Warning severity, clears `CustomScriptName`, retains trimmed resolution notes, and shows a strong nonblocking prompt while those notes are blank. It never requires custom-script support.

## Resolution preservation and stale removal

While the same deterministic condition remains present, synchronization reuses the same finding object and preserves valid resolution state. Created By, QA Date, File Month, hotel selection, Original Filename, General Notes, and unrelated checklist edits do not reset it.

When a condition ends, its finding is removed from `CurrentReport.Findings` and its dynamic control is disposed. No removed finding is stored in a hidden cache. If the condition returns, a new object is created as Active with no old script name or resolution notes.

Original severity is refreshed from the generation rule and never altered by resolution. `CurrentReport.Findings` remains the existing get-only `List<QaFinding>` and is synchronized in place.

## Event and refresh flow

Checklist controls update their bound result and expose one coherent `ResultChanged` event for status, notes, and warning selection. Characteristic controls update the existing `QaFileCharacteristics`. Finding controls update their bound `QaFinding` resolution fields.

The form uses one guarded refresh boundary to:

1. synchronize findings;
2. reconstruct checklist Warning checkboxes from deterministic finding presence; and
3. reconcile the Warnings and Failed Checks controls.

The same guard suppresses recursive checkbox, text, applicability, and finding-row refresh events. Applicability changes are batched across all 28 catalog controls before one final finding refresh.

## Privacy and in-memory-only behavior

The form-level reminder now says not to enter guest names, guest email addresses, reservation-level personal information, payment information, credentials, or full hotel-file contents. It directs users to field names, counts, percentages, script names, and summarized conditions.

Phase 6 stores findings only on the current form-owned `QaReport`. Closing writes no report, PDF, QA index entry, metadata record, or final status. Reopening constructs a fresh draft.

## Verification record

### Required builds

Before implementation, the exact command was:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

Result: exit code 0, build succeeded, 0 warnings, and 0 errors.

The integrated production implementation was also built with the same exact command during development. Result: exit code 0, build succeeded, 0 warnings, and 0 errors. The final post-documentation build uses the same command and result.

The solution remains one SDK-style `net10.0-windows` WinForms project with no package references and no project-file change.

### Human-visible GUI testing

No human-visible interactive WinForms session was controlled through the execution interface. No claim is made that a person clicked or visually inspected the application. Visual appearance, high-DPI behavior on multiple monitors, real keyboard-only navigation, screen-reader announcements, and native focus cues remain for a manual desktop pass.

### STA/offscreen production-control harness

A disposable `net10.0-windows` STA WinForms harness was created under:

```text
C:\Users\GABRIE~1\AppData\Local\Temp\Phase6RootVerification-019f613b99c17653
```

It referenced the production project and used a unique synthetic runtime root under the operating-system temporary directory. The form was shown outside the visible desktop bounds and exercised with actual production controls and services.

Exact harness commands:

```powershell
dotnet build Phase6RootVerification.csproj
dotnet run --no-build --project Phase6RootVerification.csproj
```

Build result: exit code 0, 0 warnings, and 0 errors.

Run result: exit code 0 and:

```text
PASS: 161 assertions completed.
```

The harness directly verified:

- deterministic helper output and blank-ID rejection;
- duplicate and blank existing finding rejection;
- the exact 28-result catalog contract;
- Failure generation, fallback/notes descriptions, identity reuse, handled preservation, stale removal, and Active recreation;
- passed-check Warning creation/removal, Pass preservation, blank explanations, notes descriptions, and Warning-to-Failure replacement;
- all three characteristic warnings, exact related IDs, no automatic Fail, stale removal, and Active recreation;
- independent custom-script handling, trimmed script fields, severity preservation, unavailable-support normalization, and retained explained Warning state;
- Failure rejection of Explained and Accepted;
- untouched statistics object and report-status sentinel in direct service tests;
- final ordinal uniqueness and repeated synchronization;
- actual checklist control Pass/Fail, notes trimming/null behavior, warning enablement/prompt, coherent events, N/A clearing, and reapplicable NotEvaluated state;
- actual finding control resolution availability, direct model binding, prompts, script fields, and severity preservation;
- actual `QaReportForm` dynamic counts, Warning/Failure group separation, handled-row visibility, unrelated-edit preservation, characteristic events, conditional-warning applicability, stable control counts, and minimum-size panel usability;
- Close outside scrolling content;
- byte-for-byte unchanged synthetic metadata files after closing; and
- a fresh draft on reopen.

The harness project, its build output, the synthetic runtime root, and all generated temporary data were removed after containment checks. No verification project or synthetic data remains in the repository.

### Source-review-only checks

Source and diff review confirms:

- `Program.Main` and the startup path are unchanged;
- all three V1 log types, dynamic fields, preview/save paths, and six V1 bottom actions are unchanged;
- Phase 4 metadata management still opens through the unchanged MainForm path;
- Hotel/PMS search, real metadata binding, canonical PMS selection, and metadata refresh remain unchanged;
- the QA Report action and owned form construction remain unchanged;
- File Month and QA Date remain independent;
- all 28 Phase 5 results and dynamic applicability transitions remain;
- Pass/Fail still mutates the actual results and Manual result source remains;
- Close and Escape remain nonblocking and have no persistence path;
- no event handler is subscribed in both designer and code;
- no direct JSON, report save, index write, raw-file parse, database, automation, statistics evaluation, or report-status path was added; and
- no protected V1 or earlier-phase contract file changed.

Interactive V1 Preview/Submit, folder picker/reset, shell-opening actions, partial index failure, and real application shutdown were not re-executed. Their production source is unchanged and was reviewed against `docs/v2/V1-Regression-Checklist.md`.

## Deferred work

Phase 7 can build statistic-generated findings and final-status calculation on this deterministic synchronization infrastructure. It must supply its own statistic rules and status policy. Phase 6 implements neither.

Later phases remain responsible for completion validation, Created By generation policy, Report ID generation, PDF selection and generation, report persistence, paired output, QA index writing, parsing, database integration, diagnostics, and automation.

## Final scope confirmations

- Existing Phase 2 finding models, enums, checklist IDs, definitions, and statistics contracts are used directly and are unchanged.
- `QaReport.Findings` remains the existing get-only list and is mutated in place.
- No report-status or completion calculation was added.
- No statistics UI, statistics evaluation, or statistic-generated finding was added.
- No PDF, report saving, QA index writing, raw-file/spreadsheet parsing, database access, or automation was added.
- No metadata schema or Phase 3 persistence API was changed.
- No protected V1 model or service was changed.
- No package or project-file change was added.
- No report, PDF, index entry, or synthetic repository data was created.
- The approved Phase 6 implementation was committed and pushed only to `v2-qa-reports` as commit `2c599ea3ca0be6c5c434de625a95ef8c788aabd0`. It was not merged into `main`, and no tag was modified.
