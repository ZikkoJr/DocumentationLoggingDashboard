# Pilot P1 Correction — Blank and Broken Statistics Semantics and Propagation

> **Final baseline update (2026-07-20):** This correction is included in the reviewed V2 production-candidate source baseline. Final coverage adds blocked-output guards, overwrite replacement, a 4-Hotel/3-PMS synthetic root, separate V1 regression, and preserved Release PDF/index evidence. The frozen pilot package and pilot roots remain unchanged. See `V2-Production-Readiness.md`.

## 1. Pilot defect classification

Classification: **P1 controlled-pilot defect**.

This is a post-Phase-10 correction discovered during controlled internal pilot activity. It is not Phase 11, does not reopen Phases 1–10, and does not authorize publication, replacement of the frozen pilot package, resumption of Scenario 3, merge, tag, or release activity.

## 2. Starting commit and working-tree context

Approved repository baseline commit:

```text
d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc
```

Branch: `v2-qa-reports`.

The semantic correction was started while HEAD remained at that commit. The working tree already contained the separately authorized, uncommitted Statistics & Readiness layout correction and its permanent geometry harness. The operator explicitly authorized continued work on top of that preserved local state. No reset, checkout, rebase, discard, or history rewrite was used to manufacture a clean baseline.

## 3. Root cause

The Phase 7 implementation treated manually entered denominators and threshold findings as a narrower first-pass workflow:

- Blank denominators had no explicit automatic/manual state and did not default from current Total Data Rows.
- Broken denominators had no explicit automatic/manual state, were read-only in the UI, and were always overwritten from the matching Blank row.
- Blank findings existed only above 50% and were always Warnings.
- Any positive Broken count was Failure-oriented; fields with a related checklist ID relied on the checklist Failure instead of a canonical statistics finding.
- Readiness required a related checklist Fail for any positive mapped Broken count.
- Managed finding IDs, validation expectations, and PDF ordering encoded only `WARN:STAT:BLANK:*` and `FAIL:STAT:BROKEN:*`.

The form’s existing live synchronization, finding reconciliation, readiness fingerprint, status service, PDF renderer, paired save, and index pipeline were structurally reusable. The correction extends those paths instead of adding parallel systems.

The inspected source loci for the defect and correction are the two statistic models; `QaBlankStatisticRowControl` and `QaBrokenDataStatisticRowControl` binding/commit paths; `QaStatisticsControl` reconciliation and change propagation; `QaStatisticsCalculationService.Synchronize`, `CalculatePercentage`, and `ClassifyThreshold`; `QaFindingSynchronizationService` expected-statistic builders, mapped suppression, and managed-ID set; `QaReportValidationService.ValidateBlankStatistic`, `ValidateBrokenStatistic`, expected-finding construction, and mapped consistency checks; `QaReportReadinessFingerprint.AppendStatistics`; and the existing PDF ordering path. `QaReportStatusService` was inspected and remains unchanged.

## 4. Corrected Blank thresholds

Blank percentage is calculated independently for each applicable field:

```text
Blank Count / Total Applicable Rows
```

Thresholds use the unrounded numerator and denominator; two-decimal rounding remains presentation-only.

| Blank result | Finding |
| --- | --- |
| 0% | None |
| Greater than 0% through 50%, inclusive | Warning |
| Greater than 50% | Failure |

Exactly 50% is a Warning. Blank findings do not change a required-field-presence or populated-value-validity checklist result.

## 5. Corrected Broken thresholds

Broken percentage is calculated independently for each applicable field:

```text
Broken Count / Total Applicable Nonblank Values
```

| Broken result | Finding |
| --- | --- |
| 0% | None |
| Greater than 0% through 50%, inclusive | Warning |
| Greater than 50% | Failure |

Exactly 50% is a Warning. At or below 50%, a related populated-value-validity check may remain Pass. Above 50%, the related check must be Fail or readiness reports a contradiction.

## 6. Blank-versus-Broken distinction

Blank means the field exists but a cell is empty. Broken means a populated value is malformed, corrupted, misplaced, or otherwise invalid. Blank values are never added to Broken counts, and Blank and Broken percentages are never combined.

The model, UI, PDF, findings, and validation paths continue to keep the two statistic types in separate collections and tables.

## 7. Full Name, First Name, and Last Name correction

The First Name, Last Name, and Full Name validity checks evaluate populated values only:

- a blank name cell belongs only in Blank Value Statistics;
- an email address or unrelated populated value in a name field is Broken Data;
- Broken at or below 50% may remain a passing validity check with a Warning;
- Broken above 50% requires a failed validity check or a readiness contradiction; and
- Blank percentages never force these validity checks to Fail.

The same populated-value interpretation applies to the mapped date-format check. No checklist IDs, ordering, or applicability definitions are added or removed.

## 8. Total Data Rows propagation

`Total Data Rows` is the operator-entered count of rows that contain data. Header and preamble rows are excluded from this value. `Headers Present`, `Useful Headers`, and `Data Start Row` describe the file layout; they do not cause the statistics service to subtract those rows a second time. For example, when a physical file has 120 occupied rows, row 1 is the header, and data starts on row 2, the operator enters `119` for Total Data Rows and each Auto Blank denominator becomes `119`.

Each newly applicable Blank row starts in Auto mode. In Auto mode:

```text
Total Applicable Rows = Total Data Rows
```

Changing Total Data Rows updates every still-automatic Blank row, recalculates percentages and dependent automatic Broken denominators, resynchronizes findings, refreshes the UI, and invalidates earlier readiness/status authority.

Rows removed by applicability are not cached. If they later become applicable, fresh row objects receive the current automatic defaults instead of stale hidden values.

## 9. Broken nonblank-denominator calculation

Each newly applicable Broken row starts in Auto mode. Its denominator is:

```text
Total Applicable Nonblank Values =
    matching Blank Total Applicable Rows - matching Blank Count
```

The calculation uses the current matching Blank denominator, including a valid manual Blank override. Total Data Rows is never used directly when the matching Blank count is nonzero.

## 10. Auto/manual override behavior

The model stores an explicit automatic/manual state separately for every Blank and Broken row. The current implementation uses automatic-state Boolean properties whose default initializers are `true`, so an older deserialized row without the new property receives safe automatic behavior.

- Editing a Blank denominator switches only that row to Manual.
- Editing a Broken denominator switches only that row to Manual.
- Later upstream changes preserve a valid Manual value.
- Selecting the visible Auto control immediately restores the current automatic formula.
- Percentages and findings always use the displayed denominator.
- Auto/manual mode is part of the readiness fingerprint, so a mode-only edit cannot retain stale save authority.

## 11. Finding-ID behavior and checklist deduplication

The managed deterministic families are:

```text
WARN:STAT:BLANK:<FieldId>
FAIL:STAT:BLANK:<FieldId>
WARN:STAT:BROKEN:<FieldId>
FAIL:STAT:BROKEN:<FieldId>
```

At most one Blank threshold finding and one Broken threshold finding can exist for one field at a time. Blank and Broken findings remain independent, so two Warnings for the same field are valid when both statistic types are nonzero and at or below 50%.

For a mapped Broken result above 50%, the canonical Failure is the statistics finding. Its `RelatedCheckId` points to the current failing checklist row. The generic `FAIL:<ChecklistId>` finding is suppressed only when that current `QaCheckResult.Notes` value is blank after trimming; this is the threshold-only path. When the failing checklist row has nonblank Notes, those Notes document a separate contextual checklist defect, so both the canonical `FAIL:STAT:BROKEN:<FieldId>` finding and the contextual `FAIL:<ChecklistId>` finding remain. Unrelated checklist Failures are never broadly suppressed.

Finding resolution state is not treated as evidence of cause and does not control this deduplication decision. Notes are the current model's explicit context signal: blank Notes mean no separately documented checklist defect, while nonblank Notes preserve that independently documented defect alongside the statistics threshold Failure.

Residual risk: the current model has no separate cause marker for a checklist Failure. `QaCheckResult.Notes` is therefore the only causal discriminator available to the correction. A distinct checklist defect recorded with blank Notes could be interpreted as threshold-only and its generic generated Failure could be suppressed. Pilot operators should record nonblank contextual Notes whenever a checklist Failure represents a defect separate from the mapped Broken threshold; an explicit cause marker would require a separately approved model/domain change.

## 12. Finding-resolution lifecycle

The existing reconciliation lifecycle remains authoritative:

- while the same deterministic ID remains expected, the existing finding object and valid resolution state are preserved;
- Warning to Failure removes the Warning object and creates a new Active Failure;
- Failure to Warning removes the Failure object and creates a new Active Warning;
- zero removes the managed finding and its resolution state;
- recurrence after zero creates a new Active finding; and
- handling a Failure never changes its Failure severity or moves it out of Failed Checks.

Because Warning and Failure use different deterministic IDs, resolution notes and script state never cross a threshold transition.

## 13. Validation and readiness behavior

Positive Blank or Broken counts are not blockers by themselves. Warning-only and Failure-threshold reports can both be complete and ready.

Validation continues to block genuinely invalid or incomplete state, including:

- negative counts or denominators at the model/service boundary;
- a count greater than its displayed denominator;
- a positive numerator with a zero denominator;
- an Auto Blank denominator that differs from Total Data Rows;
- an Auto Broken denominator that differs from the matching Blank-derived value;
- a Manual Blank denominator greater than Total Data Rows;
- a Manual Broken denominator greater than the available matching nonblank population;
- an unsynchronized percentage;
- duplicate, unknown, missing, or non-applicable statistic rows;
- applicable checklist rows that are not evaluated;
- missing required explanations; and
- a mapped Broken result above 50% whose related checklist result is not Fail.

Statistics edits, denominator-mode edits, applicability changes, finding changes, and related checklist changes use the existing stale-readiness path. No second validator or status calculator is introduced.

## 14. Report-status behavior

`QaReportStatusService` is unchanged:

- any blocking validation error: no calculated status;
- at least one Active Failure: `Fail`;
- no findings: `Pass`; and
- otherwise: `PassWithWarnings`.

A handled statistics Failure retains Failure severity and remains under Failed Checks, but when no Active Failure remains the approved overall status is `PassWithWarnings`.

## 15. Files changed

Provisional production file set as of this documentation draft:

- `DocumentationLoggingDashboard/QAReports/Definitions/QaChecklistCatalog.cs`
- `DocumentationLoggingDashboard/QAReports/Definitions/QaFindingIds.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaBlankStatisticRowControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaBrokenDataStatisticRowControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaStatisticsControl.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaBlankValueStatistic.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaBrokenDataStatistic.cs`
- `DocumentationLoggingDashboard/QAReports/Pdf/QaPdfDocumentBuilder.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaFindingSynchronizationService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportValidationService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaStatisticsCalculationService.cs`
- `DocumentationLoggingDashboard/QAReports/Validation/QaReportReadinessFingerprint.cs`

`QaStatisticsControl.cs` also contains the separately authorized uncommitted layout correction. The final report must distinguish the layout and semantic responsibilities in that shared file.

Documentation files changed by this documentation pass are listed below.

- `docs/v2/Phase-2-Domain-Contract.md`
- `docs/v2/Phase-6-Findings-And-Custom-Script.md`
- `docs/v2/Phase-7-Statistics-Validation-Status.md`
- `docs/v2/Phase-8-PDF-Generation.md`
- `docs/v2/Phase-9-Paired-Saving-And-Index.md`
- `docs/v2/Phase-10-Final-Validation-And-Release-Readiness.md`
- `docs/v2/V2-End-to-End-Test-Matrix.md`
- `docs/v2/V2-Pilot-Guide.md`
- `docs/v2/V2-Release-Checklist.md`
- `docs/v2/V2-Release-Notes-Draft.md`
- `docs/v2/Pilot-Correction-Blank-Broken-Statistics.md` (new)

Current permanent test file set as of this draft, subject to final diff audit:

- `tests/DocumentationLoggingDashboard.GeometryTests/DocumentationLoggingDashboard.GeometryTests.csproj`
- `tests/DocumentationLoggingDashboard.GeometryTests/Program.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/QaStatisticsControlGeometryTests.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/QaBlankBrokenStatisticsRegressionTests.cs`
- `tests/DocumentationLoggingDashboard.GeometryTests/QaBlankBrokenSavePdfIndexEvidenceTests.cs`

No change is intended for `QaReportStatusService`, `QaReportSaveService`, `QaReportIndexService`, QA index format, storage paths, metadata schemas, dependencies, or V1 production code.

## 16. Tests executed

Status: **PASS for the executed semantic, save/PDF/index, blocked-save, overwrite, STA geometry/interaction, actual-125%-scaling visible no-save, and protected V1 scopes.** Real Windows 100% and 150% scaling remain Not Run.

Required evidence includes:

- permanent automated threshold, separation, propagation, lifecycle, invalid-state, readiness, save, PDF, and index coverage;
- Full Name/First Name/Last Name and synthetic email-in-name cases;
- mapped greater-than-50% Broken cases proving blank checklist Notes suppress only the threshold-only generic Failure, nonblank Notes preserve both contextual and statistics Failures, and resolution changes alone affect neither decision;
- the exact 20-step propagation/override sequence;
- initial, maximized, and minimum-size Windows UI checks;
- 100%, 125%, and 150% scaling;
- forward/reverse keyboard navigation and scrolling; and
- a visible isolated no-save smoke test.

The required propagation/override test sequence is:

1. Set Total Data Rows to 100.
2. Confirm all applicable Auto Blank denominators become 100.
3. Confirm Auto Broken denominators equal 100 minus the matching Blank Count.
4. Set Email Blank Count to 12.
5. Confirm Email Broken denominator becomes 88.
6. Change Total Data Rows to 120.
7. Confirm Auto Blank denominators become 120.
8. Confirm Email Broken denominator becomes 108.
9. Manually override one Blank denominator.
10. Change Total Data Rows.
11. Confirm the Blank override remains.
12. Confirm its Auto Broken denominator recalculates from the overridden Blank denominator unless Broken was separately overridden.
13. Manually override one Broken denominator.
14. Change Total Data Rows and Blank Count.
15. Confirm the Broken override remains.
16. Reset both rows to Auto.
17. Confirm calculated values return immediately.
18. Make a hidden statistic row applicable.
19. Confirm it receives current automatic defaults.
20. Confirm percentages, findings, readiness, status, PDF, and index use the final displayed denominator values.

Recorded results:

```text
Automated semantic tests: Debug 7/7 Pass and Release 7/7 Pass, including header exclusion, First/Full/Last Name behavior, and finding-resolution noncausality.
Automated save/PDF/index tests: Debug 7/7 Pass and Release 7/7 Pass.
STA geometry/UI tests: Debug 7/7 Pass and Release 7/7 Pass at actual DeviceDpi=120 (Windows 125%).
Before/after geometry record: Recorded below for the 880x600 actual-125%-DPI state.
Mouse hit-testing: Pass in the permanent STA suite at DeviceDpi=120.
Forward/reverse Tab navigation: Pass in the permanent STA suite at DeviceDpi=120.
Bottom/top scrolling and scrollbar audit: Pass in the permanent STA suite at DeviceDpi=120; top-to-bottom-to-first-group return succeeded, no horizontal scrollbar appeared, and the intended outer/inner scrollbars were retained.
Dynamic applicability/propagation: Pass in the permanent STA suite; stale hidden values were discarded, fresh Auto values were applied, and no layout recursion was observed.
Visible 100% matrix: Not Run at real Windows 100% scaling.
Visible 125% matrix: Pass for the isolated nine-image no-save set at actual Windows 125% scaling.
Visible 150% matrix: Not Run at real Windows 150% scaling; font-pressure coverage is not classified as real 150% scaling.
Screenshot inventory: Pass; nine accepted PNGs under external `gui-smoke-final6-125-actual` plus published-startup screenshot `published-startup-final2.png`.
V1 regression result: Pass on a separate disposable synthetic root for DEBUG, EDIT, and CREATE entries, exact text, daily sequence, central LogIndex, current folders, QA-index isolation, and expected absence of Debugging Log Hotel/PMS routing.
```

The STA geometry suite exercised initial, minimum 880x600, larger/maximize-sized, font-change, maximum-applicability, dynamic hide/show, Auto/manual propagation, native hit-testing, Tab/Shift+Tab, and scrolling states. It is harness evidence, not a substitute for direct observation at real 100%, 125%, and 150% Windows scaling.

### Numeric layout evidence at 880x600 and actual Windows 125%

All rectangles use `(x, y, width x height)` in parent-client coordinates.

| Measurement | Baseline defect | Corrected |
| --- | --- | --- |
| Statistics viewport | `841 x 553` | `841 x 553` |
| File Information GroupBox | `(0,62,560x111)` | `(0,62,833x177)` |
| File Information display rectangle | `(10,30,540x71)` | `(10,30,813x137)` |
| File Information content | `(10,30,540x137)` | `(10,30,813x137)` |
| Data Start Row label | `(13,101,226x20)` | Fully contained in the corrected display rectangle |
| Data Start Row input | `(16,127,132x27)` | Fully contained in the corrected display rectangle |
| Blank Value Statistics top | `183` | `249` |
| Group separation | File content absolute bottom `229` crossed Blank group top `183` by `46` pixels | File group bottom `239`; Blank group top `249`; `10`-pixel gap; no intersection |

The baseline retained a one-row group height after wrapping, so the lower label/input extended outside the `71`-pixel display height and into the next group. The correction derives the `833`-pixel group width from the `841`-pixel viewport, propagates the wrapped `137`-pixel content height to the GroupBox, and positions the next group only afterward.

### Actual-125% visible no-save matrix

The accepted external folder `gui-smoke-final6-125-actual` contains:

1. default 1100x780;
2. minimum 880x600;
3. maximum applicability;
4. header row 1/data row 2 with Total Data Rows `119` and Blank Email manual `12/90`;
5. Broken Email manual `0/70`;
6. Broken Email restored to Auto `0/107`;
7. 1.25 font pressure;
8. 1.50 font pressure; and
9. maximized maximum-applicability 1.50 font pressure.

The 1.25/1.50 font-pressure captures are additional wrapping stress evidence at actual 125% Windows scaling. They are not relabeled as real Windows 150% scaling.

## 17. Build and publish results

Status: **PASS for corrected builds, isolated external publish, and visible published startup/no-save smoke.**

```text
Debug solution build: PASS — exit 0, 0 warnings, 0 errors.
Release solution build: PASS — exit 0, 0 warnings, 0 errors.
Debug permanent-test project build: PASS — exit 0, 0 warnings, 0 errors.
Release permanent-test project build: PASS — exit 0, 0 warnings, 0 errors.
Isolated external publish: PASS — output created outside the repository and protected pilot locations.
Published startup/no-save smoke: PASS — visible, responsive, normal close exit 0, isolated synthetic root remained empty.
Package/dependency/protected-state audit: PASS — production dependency declarations and deployment model are unchanged; no dashboard/test process is running; the frozen package, live Retry-01 root, V1 data, Git history, `main`, tags, and releases were not modified.
```

The final isolated publish/startup evidence folder was `publish-verification/p1corr-final-20260720`. Its final inventory was:

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `appsettings.json` | 53 | `D8907CDD3C2440BD404B7C1A23837AAAD4449F428AE64E2A1FFF038DD6FA2E03` |
| `DocumentationLoggingDashboard.exe` | 118,644,117 | `358310ABC12F9B7EED13930640E3222AF2D4A82A1E70BCB7EC271DA78CE0D547` |
| `DocumentationLoggingDashboard.pdb` | 120,108 | `F3E14CE4E03D27C7337C82C170011747321AF24A59160ED4C048A62BFC564444` |
| `user-settings.json` | 215 | `5A953EF6F3F751EA3D387625EE8993E9464A685905BBBA0AC46AC821B7EBAFEB` |

The published application opened visibly with title `Documentation Logging Dashboard`, was responsive at `1020x806`, closed normally with exit code `0`, and left its isolated synthetic documentation root empty. The accepted screenshot is `published-startup-final2.png`. This proves startup/no-save behavior only, not a published paired-save workflow.

The verification output is not an approved pilot package. One initial publish command was rejected because the output path containing spaces was not quoted; the corrected explicit command succeeded. That command-line correction is not a product defect.

Any publish used for verification must remain outside the repository, frozen pilot package, live pilot root, and preserved pilot backups. It is not the approved pilot package unless separately authorized.

## 18. Synthetic PDF and index verification

Status: **PASS for the preserved seven-scenario synthetic evidence run and representative rendered review.**

The isolated evidence root was `r-cd0f2b260fc64006` under the external Codex visualization workspace. Each Hotel/PMS pair was byte-identical, the QA index contained seven distinct logical entries with the expected status, and no transaction leftovers remained.

| Case | Outcome | Warning / Failure / Handled | PDF bytes | SHA-256 |
| --- | --- | --- | ---: | --- |
| Blank Warning | Pass with Warnings | 1 / 0 / 0 | 101,340 | `05DFD3E2B94FB754C3490E4ED6081DEB50222BC726483A64BDD52826DC98AEC5` |
| Broken Warning | Pass with Warnings | 1 / 0 / 0 | 100,899 | `30C3683894206DAA94F2D49108FEBC9AF37FE0B516BCAD36C7D5ACB961D3A04F` |
| Both Warnings | Pass with Warnings | 2 / 0 / 0 | 102,393 | `22780DEC832C94CD62282D429E2A3D1FC67470DF4D08894D4E70FDED2A9D0C7C` |
| Blank Failure | Fail | 0 / 1 / 0 | 101,995 | `162D0037C9CAE43387056F102C35783698CA18EBDFE143208010BD5E0A876BA4` |
| Mapped First Name Broken Failure | Fail | 0 / 1 / 0 | 101,485 | `7367E398424A9C7CA3892079FBE60666E125EA85E0F6B0487DDD8994C07581B3` |
| Handled mapped First Name Broken Failure | Pass with Warnings | 0 / 1 / 1 | 102,386 | `D87DC4E7788D512B9F78F1C3BD0507B2B834587BAC467150586573DA69DCACE4` |
| Zero threshold findings | Pass | 0 / 0 / 0 | 98,492 | `C2FCEFB9EA00DB211A3E9A58517872700F67371C52CF79A9BDAB7AC47C6A691D` |

The seven-entry `QAReportIndex.txt` was 3,696 bytes with SHA-256 `3324977D2763AF7F56B83293A107A2329FC2E99AA9DC6CE73321EBB6850F91F8`.

Representative PDFs for the both-Warnings, mapped-Failure, and handled-mapped-Failure cases were rendered and visually inspected. The review found separate Blank/Broken tables, expected checklist/finding/status placement, one canonical Failed Check for the blank-Notes threshold-only mapped Failure, retained Failure severity for the handled case, no clipping or overlap, and no guest-level data. Direct permanent PDF-order assertions and the nonblank-Notes two-Failure visual variant remain residual coverage gaps; the Debug and Release 7/7 semantic suites cover the underlying distinction.

The preserved seven-scenario run covers Blank Warning only, Broken Warning only, both Warnings, Blank Failure, Broken Failure, handled Broken Failure, and zero threshold findings. For every case it records expected/actual status, Warning/Failure/handled counts, both PDF paths, sizes and SHA-256 hashes, byte identity, the single logical QA index entry and status, and absence of transaction leftovers.

The representative rendered review completed the both-Warnings, blank-Notes mapped-Failure, and handled-mapped-Failure paths. A rendered nonblank-Notes two-Failure variant and a direct permanent extracted-order assertion remain bounded coverage gaps. Semantic automation nevertheless verifies the nonblank-Notes distinction and that finding resolution alone does not change the deduplication decision.

## 19. Pilot scenarios covered by final candidate verification

Final-candidate synthetic verification reran the following
correction-dependent evidence in a new isolated root:

- Scenario 3;
- every report involving Blank Data;
- every report involving Broken Data;
- Full Name, First Name, and Last Name validity;
- Warning-only statistics reports;
- Failure-threshold statistics reports;
- handled statistics Failures;
- corrected PDF visual checks;
- paired-save verification for corrected statuses; and
- QA index verification for corrected statuses.

All listed paths passed the permanent Debug/Release regression suites and the
preserved 14-report final Release matrix. This does not authorize modifying or
resuming the frozen pilot package or live pilot root; a later pilot retest
remains an operational approval step.

## 20. Pilot evidence that may remain valid

Evidence unrelated to the corrected semantics may remain useful after explicit regression review, including:

- Scenario 1 metadata initialization and persistence;
- Scenario 2 metadata search and duplicate handling;
- Hotel/PMS metadata creation and canonical selection;
- storage-root isolation and path containment;
- filename/key parsing and overwrite identity;
- transaction rollback mechanics;
- V1 workflow isolation;
- privacy guidance not dependent on the changed statistic rules; and
- previously captured layout evidence only where the same production layout code and supported scaling state remain unchanged.

Prior evidence that relied on Blank/Broken thresholds, mapped checklist behavior, finding counts/IDs, readiness, calculated status, PDF findings/status, paired save, or index status is superseded and cannot be carried forward without rerun.

## 21. Approved correction commit state

```text
Correction commit/push: AUTHORIZED FOR v2-qa-reports BY FINAL-CLEANUP REQUEST
Independent review: PASS — no P0/P1 blocker.
Authoritative final commit: recorded in the external production manifest
```

The production-candidate authorization does not authorize pilot-package replacement, Scenario 3 resumption, merge, tag, or release. Remaining evidence risks are the P2 Notes causal discriminator, absence of a direct permanent PDF-order assertion, and real Windows 100%/150% scaling not run. The permanent test project is included in the solution and the correction-specific V1 regression passes.

## 22. Merge, tag, release, and pilot-package state

As of the pre-commit protected-state audit on 2026-07-20:

- the correction was reviewed and authorized for commit/push to
  `v2-qa-reports`; the exact final pushed commit is recorded in the production
  manifest because a tracked file cannot embed its own commit SHA;
- no merge into `main` has occurred;
- no V2 release tag or GitHub release has been created;
- Scenario 3 has not been resumed;
- the frozen package and live Retry-01 pilot root have not been modified by this semantic-correction work; and
- replacement or publication of a pilot package remains separately blocked.

The audit reconfirmed the starting `v2-qa-reports` commit as
`d8f2eeeab6d7dea83c9c2924ccb2987645fc69cc`, with `0 0` divergence from its
upstream; `main` remained at
`2f95d8a12c9b772124bd688d9a331e07730e5ab0`, and the only tag remained
`v1.0.0`. The frozen package retained its pre-correction hashes and timestamps,
the live Retry-01 root retained five files with newest write
`2026-07-17 14:26:06`, and no dashboard/test process was running.

## 23. Deferred Debugging Log folder enhancement

The separate Debugging Log folder enhancement remains deferred. This P1 correction does not implement, partially implement, approve, or change the priority of that enhancement, and it makes no change to V1 Debugging Log paths, filenames, text output, IDs, index behavior, or operational data.
