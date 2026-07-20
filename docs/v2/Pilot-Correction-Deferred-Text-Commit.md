# Pilot Correction - Deferred Text Commit and Refresh Performance

> **Final baseline update (2026-07-20):** This correction is included in the V2 production-candidate source baseline. The final 8/8 suite and actual-125% visible 1,099-character readiness case pass. Test-only synchronization/readiness counters were removed from production code; final verification uses observable model/event/control state, focus/caret/selection/scroll, PDF markers, and index status. See `V2-Production-Readiness.md`.

## 1. Defect description

Free-text controls in Create QA Report updated the bound model from every `TextChanged` event. Finding and checklist events then ran the full statistics/finding synchronization and finding-control refresh pipeline. Long notes therefore caused repeated calculation, rebinding, layout, and readiness work while the operator was still typing.

The correction keeps draft text inside each control and commits the normalized completed value at validation or a mandatory workflow boundary. It does not add a timer, debounce dependency, background thread, or Done button.

## 2. Severity

The reproduced defect is classified **P2**. Typing remained possible in the focused harness, but every meaningful character caused expensive model/event work and the form path mapped those events one-for-one to synchronization and finding refresh. This produced substantial delay, redraw risk, and an unreliable pilot experience for long required notes. No character loss was observed during the controlled reproduction, so the P1 threshold was not claimed.

## 3. Starting state and commit

- Branch: `v2-qa-reports`
- Exact starting commit: `d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc`
- `HEAD...origin/v2-qa-reports`: `0 0`
- `main` and `origin/main`: `2f95d8a12c9b772124bd688d9a331e07730e5ab0`
- Existing tags: `v1.0.0` only; no V2 tag existed
- Target: `net10.0-windows`
- Package: `PDFsharp-MigraDoc-GDI` `6.2.4`
- Baseline build: exit `0`, 0 warnings, 0 errors

The initial gate found the separately authorized Blank/Broken Statistics correction, File Information/Statistics layout work, and permanent test project still uncommitted. It also found the frozen-pilot executable running. Work stopped. The operator closed the application and explicitly authorized continuing on top of the preserved uncommitted correction. No stash, reset, discard, checkout, rebase, or history rewrite was used.

The Blank/Broken and layout changes remain distinguishable in their existing source/tests/documentation. The deferred-text correction did not change their thresholds, formulas, IDs, or layout contract.

## 4. Root cause

The following paths ran from every character:

- Finding script name and resolution notes: `TextChanged` -> model assignment -> `FindingChanged` -> `SynchronizeFindingsAndRefreshUi` -> statistics calculation -> finding reconciliation -> checklist warning synchronization -> `RefreshFindingControls` -> statistics refresh.
- Checklist notes: `TextChanged` -> model assignment -> `ResultChanged` -> warning-selection reconciliation -> the same full pipeline.
- Statistics explanations, including every Broken Data row: `TextChanged` -> `CommitCurrentValues` -> calculation/row refresh -> `StatisticsChanged` -> form statistics synchronization, applicability, findings, and UI refresh.
- Created By, Original File Name, and General Notes: every character updated the model and separately invalidated readiness.

The defect was not caused by PDF rendering, saving, threading, or a missing debounce. It was an event-boundary problem: draft keystrokes were treated as completed report edits.

## 5. Complete text-input audit

### Deferred model text

- `QaFindingItemControl.customScriptNameTextBox`
- `QaFindingItemControl.resolutionNotesTextBox`
- `QaChecklistItemControl.notesTextBox`
- `QaReportForm.createdByTextBox`
- `QaReportForm.originalFileNameTextBox`
- `QaReportForm.generalNotesTextBox`
- `QaStatisticsControl.unusualAverageRateExplanationTextBox`
- `QaStatisticsControl.unusualStayValueExplanationTextBox`
- `QaStatisticsControl.highStayValueExplanationTextBox`
- `QaStatisticsControl.rowDifferenceExplanationTextBox`
- `QaStatisticsControl.rejectedRecordsExplanationTextBox`
- Every `QaBrokenDataStatisticRowControl.explanationTextBox`

### Intentionally immediate

- Hotel/PMS selector and metadata search text: immediate local filtering is the intended UX.
- Add Hotel ID/Name and Add PMS Name: `TextChanged` only updates local button availability; the model/storage boundary remains the explicit Add action.
- NumericUpDown statistics: immediate calculation is required and retained.
- ComboBox, radio-button, checkbox, applicability, Auto/manual denominator, and resolution selections: immediate state transitions are required and retained.
- Read-only calculated values and readiness summary: programmatic UI-only text.
- Selected Hotel ID/PMS display: read-only UI text.

No metadata search/filter behavior was changed.

## 6. Deferred-commit design

Each affected child control tracks whether its visible draft differs from the last synchronized model and continues using the existing programmatic synchronization guard:

- `hasPendingCustomScriptNameEdit`
- `hasPendingResolutionNotesEdit`
- `hasPendingNotesEdit`
- `hasPendingExplanationEdit`
- `pendingExplanationTextBoxes`
- `pendingReportTextBoxes`

`TextChanged` now marks a draft dirty and performs only local prompt/readiness-stale work. Public `CommitPendingTextEdits()` methods normalize with the existing Trim-to-null rule, compare with the bound model, update only when different, clear the dirty state, and raise at most one logical child change event.

## 7. Focus-loss behavior

The controls use `Validated`, not both `Leave` and `Validated`. A normal focus change therefore commits once. Equality and dirty guards make repeated validation a no-op. Multiline Enter remains a newline; no Done action or timer was introduced.

The Check Report Readiness and Generate and Save QA Report buttons set `CausesValidation = false` and explicitly flush drafts. Their `Enter` boundary commits keyboard-Tab drafts without a UI refresh; their action then performs the one authoritative synchronization. This prevents a mouse click from causing one validation refresh followed immediately by a second readiness/save refresh.

## 8. Mandatory action-boundary flushing

`QaReportForm.CommitAllPendingTextEdits()` flushes:

- report detail text;
- every checklist note;
- every finding script name and resolution note;
- all top-level statistics explanations; and
- every Broken Data explanation.

It is invoked before readiness/save synchronization, finding refresh paths, statistics changes, file-characteristic/applicability transitions, metadata workflow transitions, tab transitions, and form closing. Generate and Save always calls the fresh readiness path, so the last focused character reaches validation, PDF generation, the save transaction, and QA index input.

## 9. Refresh batching

`isCommittingAllPendingTextEdits` suppresses child event handlers while the form collects a batch. A child still commits and raises its logical event, but the parent performs one final synchronization/refresh after all changed child controls are authoritative. Suppression is always released in `finally`.

The final harness does not add diagnostic counters to production code. It verifies batching through completed child events, preserved finding identities and counts, the absence of per-character model changes/events, stable focus/caret/selection/scroll, and final text in the model and PDF.

Five pending finding rows produced:

- model commits: 5
- `FindingChanged`: 5
- stable finding identities after the action-boundary flush
- no moved notes and no duplicate finding IDs

## 10. Dirty-edit and rebind protection

- Same-object `Bind`/refresh calls do not overwrite dirty text.
- A replacement object with the same stable Finding ID, Check ID, or statistic Field ID receives the committed text before refresh.
- Finding and Broken Data rows explicitly discard a draft only when the synchronized condition/row no longer exists.
- Resolution/status/applicability transitions commit first, then apply the existing approved clearing rules once.
- An Active finding transition still clears resolution text; an inapplicable checklist result still clears Notes; checklist Pass-to-Fail preserves Notes.
- Finding deterministic IDs and severity behavior are unchanged.

## 11. Readiness fingerprint and stale state

Source inspection confirmed the fingerprint still includes Created By, Original File Name, General Notes, checklist Notes, finding script name/resolution notes, Broken explanations, monetary explanations, and High Stay Value explanation. No text was removed from readiness.

A report-detail field invalidates readiness once when it first becomes dirty. Child text invalidates after the completed commit/batch. Readiness and save flush first, so a fingerprint, PDF, or index cannot use the pre-edit value.

## 12. Before/after event counts

| Input | Characters | Before model commits | Before change events | After while typing | After completed commit |
| --- | ---: | ---: | ---: | --- | --- |
| Finding resolution notes | 250 | 214 | 214 `FindingChanged` | 0 commits, 0 events | 1 commit, 1 event |
| Custom Script Name | 30 | 26 | 26 `FindingChanged` | 0 commits, 0 events | 1 commit, 1 event |
| Checklist Notes | 500 | 428 | 428 `ResultChanged` | 0 commits, 0 events | 1 commit, 1 event |
| Statistics explanation | 500 | 428 | 500 `StatisticsChanged` | 0 commits, 0 events | 1 commit, 1 event |

The lower pre-fix model-commit counts reflect the existing Trim-to-null normalization when a newly typed trailing space did not change the normalized model. They are not evidence of batching. In the form, each pre-fix Finding/Result/Statistics change event entered the corresponding full synchronization handler.

## 13. Performance and focus results

Direct character-loop time is retained only as a reproducibility metric; it does not represent the full parent form cost:

- Before: Finding 250 = 37.9 ms; Checklist 500 = 54.6 ms; Statistics 500 = 72.0 ms.
- Release after initial isolated run: Finding 250 = 40.4 ms; Checklist 500 = 52.7 ms; Statistics 500 = 80.1 ms, with zero model/event/pipeline work during typing. A final parallel Debug/Release confirmation produced Release values of 98.2 ms, 142.1 ms, and 105.7 ms respectively; the event/commit counts remained exactly zero while typing and one at commit.
- Visible actual-125% case: 1,099 characters in 1,606.9 ms, focus/caret/selection/scroll stable, no synchronization or finding refresh before the boundary, then readiness synchronization/refresh `1/1`.

The acceptance improvement is the removal of sustained parent work, rebinding, and repainting, not a claim that `TextBox.AppendText` itself became faster.

## 14. Data-loss and transition tests

Passed permanent STA coverage includes:

- final character and 1,000-character General Notes;
- finding notes and script name;
- multiline content;
- whitespace-only Created By -> null plus unchanged team fallback;
- same-row refresh preserving a Broken Data draft;
- resolution change committing then applying approved clearing;
- checklist Pass-to-Fail preserving Notes;
- checklist inapplicability clearing Notes once;
- five finding drafts retaining their own stable IDs/text;
- no duplicate findings;
- direct readiness while finding notes retain focus;
- Tab/Shift+Tab traversal in the existing 7/7 interaction suite.

Backspace/Delete and clearing are covered by setting an existing draft to empty/whitespace and applying the same normalized commit path. A separate Done button was not required.

## 15. Finding, checklist, report, and statistics results

- Explained and Accepted Warning: long notes retained; one commit/event; final PDF marker present.
- Handled Failure: Failure severity retained; Custom Script Name and long notes retained; status remained Pass with Warnings; both PDF markers present.
- Active Failure: resolution notes retained; status remained Fail; marker present.
- Checklist Warning: long explanation retained in checklist and warning finding/PDF; status Pass with Warnings.
- Report fields: Created By, Original File Name, and General Notes remained draft-only while typing; action flush committed all three without findings synchronization.
- Statistics: all five top-level explanation controls and all 10 applicable Broken Data rows committed in one `StatisticsChanged` batch.

## 16. Build and test results

- Baseline Debug solution build: exit 0, 0 warnings, 0 errors.
- Final Debug solution/test builds: exit 0, 0 warnings, 0 errors.
- Final Release solution/test builds: exit 0, 0 warnings, 0 errors.
- Deferred text suite: 8/8 Debug and 8/8 Release.
- Blank/Broken semantic suite: 7/7 Debug and 7/7 Release.
- Synthetic save/PDF/index: 7 existing Blank/Broken plus 5 deferred-text scenarios passed in Debug and Release.
- STA geometry/interaction: 7/7 Debug at actual `DeviceDpi=120`.

## 17. Publish and startup results

Command:

```text
dotnet publish DocumentationLoggingDashboard\DocumentationLoggingDashboard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o <new system-temporary publish folder>
```

Result: exit 0. Inventory:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `DocumentationLoggingDashboard.exe` | 118,648,213 | `B3D445A946F3DC1E74C0A24DCD465BBDC16B4024C44EC2DFD4BD576B0B259D9A` |
| `DocumentationLoggingDashboard.pdb` | 122,544 | `B8562F6C10D0278000BDCD90823F91834A64311297587A5F44CC607DB33ED221` |
| `appsettings.json` | 53 | `D8907CDD3C2440BD404B7C1A23837AAAD4449F428AE64E2A1FFF038DD6FA2E03` |

Aggregate: 118,770,810 bytes. The published app opened visibly with title `Documentation Logging Dashboard`, was responding with a nonzero main-window handle, closed normally through `CloseMainWindow`, and created no synthetic-root files. The first launch probe did not obtain a window handle and its exact isolated PID was stopped; an immediate bounded normal-window retry produced the successful result above. This output is temporary evidence, not a frozen or approved pilot package.

## 18. Synthetic PDF and index results

Five correction-specific reports were validated, rendered once, paired-saved, indexed, text-extracted, and visually inspected:

| File Month | Scenario | Status | PDF bytes | Pair SHA-256 |
| --- | --- | --- | ---: | --- |
| 2026-08 | Pass with General Notes | Pass | 99,576 | `91D4CE58BCC92E32AC3D24F8FF63752C511145849906AED935B9D21D45F93643` |
| 2026-09 | Explained and Accepted Warning | Pass with Warnings | 103,599 | `514AA6303446B8A8B71E5A958C723A5AB05D57B26A3471E41793307F6ED2DCE4` |
| 2026-10 | Handled Failure/custom script | Pass with Warnings | 102,907 | `62848DA334E90F7C23E84E7919AFC94E93A71F40C93E23B77072823FBA498402` |
| 2026-11 | Active Failure with notes | Fail | 102,697 | `F5E7600A54B9FE74DDB2DB940514D370E852B5D10AD0E0E125E380CC5D30B98E` |
| 2026-12 | Checklist Warning explanation | Pass with Warnings | 103,085 | `43886C71482171393F79E1CABE309063035FA75D0A932BCAE788B12E01D87F51` |

All Hotel/PMS pairs were byte-identical. The final combined QA index contained 12 expected entries, including the five statuses above, with SHA-256 `F8D0D362C7ED6858C660E661EE7CAEAF5CEED73EA17E5E7EC8EE676A20D8EAFE`. No `.tmp`, `.qa-tmp`, `.qa-rollback`, or `.bak` artifact remained.

Whitespace-normalized PDF extraction found every final marker. Poppler rendered 14 pages for the five reports; visual review found correct section/status placement, readable wrapped long text, intact headers/footers/page numbers, and no clipping, overlap, black boxes, or guest-level data.

## 19. GUI matrix

| Environment | Result |
| --- | --- |
| Actual Windows 125% (`DeviceDpi=120`) | Pass: initial, 880x600, maximized, multiple findings, long/multiline notes, native hit testing, Tab/Shift+Tab, scroll, direct readiness while focused |
| Real Windows 100% | Not Run |
| Real Windows 150% | Not Run |

The 1.25x/1.50x font-pressure captures are not claimed as real Windows 125%/150% scaling. The actual-125% run produced the existing nine Statistics images plus a deferred-finding long-note image.

## 20. Files changed for this correction

- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaFindingItemControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaChecklistItemControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaBrokenDataStatisticRowControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaStatisticsControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/Program.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/QaDeferredTextCommitRegressionTests.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/QaBlankBrokenSavePdfIndexEvidenceTests.cs`
- this correction record and the focused cross-reference updates in the pilot/findings/validation/release documents

`QaStatisticsControl.cs`, `QaBrokenDataStatisticRowControl.cs`, the permanent test project, and several documents already contained the separately authorized uncommitted Statistics/layout correction. Review must distinguish those pre-existing changes from the deferred-text additions.

## 21. Remaining risks and required pilot retest

Open evidence:

- real Windows 100% and 150% scaling;
- direct manual Generate and Save click from a focused textbox (the shared readiness/save code path and synthetic paired save pass, but the blocking/success dialog sequence was not automated visibly);
- real Windows 100% and 150% DPI;
- independent review of the final immutable production candidate;
- replacement package and pilot-resumption authorization.

Pilot retest must include finding resolution notes, Explained and Accepted Warning, Custom Script Name/notes, handled Failure, checklist Warning/Failure notes, General Notes, Created By, Original File Name, every statistics explanation, readiness/save while focused, PDF final text, paired identity, and QA index status.

Scenario 1 and Scenario 2 metadata evidence remains valid: metadata code/schema/path behavior was not changed. The existing Blank/Broken semantic/layout evidence remains valid subject to its own documented open gates; this correction did not change its domain rules.

## 22. Release and scope confirmation

- Final cleanup is authorized to commit and push only `v2-qa-reports`; the authoritative final SHA is recorded in the production manifest.
- No merge to `main`, tag, release, package replacement, or pilot-root modification occurred.
- The frozen pilot package, live Retry-01 root, existing backups, and V1 operational data were not modified.
- No NuGet/package reference changed.
- No PDF/index format or paired-save transaction behavior changed.
- The Debugging Log folder enhancement remains deferred and was not implemented.

Proposed future commit message after independent approval:

```text
Defer QA report text commits and batch refreshes
```
