# Phase 7 Statistics, Completion Validation, and Report Status

## Purpose and boundaries

Phase 7 completes the in-memory manual QA workflow on the existing `QaReportForm`. It adds structured statistics entry, centralized calculations, statistics-generated findings, report-completion validation, the Created By workflow fallback, readiness review, final in-memory status calculation, and invalidation of an earlier status after relevant edits.

The implementation operates directly on the approved Phase 2 `QaReport`, `QaStatistics`, `QaCheckResult`, and `QaFinding` objects. It does not introduce a report DTO, statistics DTO, second findings collection, or second finding reconciler.

Phase 7 remains entirely in memory. It does not parse a file, read a spreadsheet, access a database, run diagnostics or automation, generate or preview a PDF, save a report, choose an output filename, handle overwrites, create Hotel/PMS copies, write a QA Report index entry, or change metadata.

> **Post-Phase-10 pilot correction:** Controlled-pilot testing superseded the original Blank/Broken denominator and threshold rules documented in this phase. The current rules are summarized below and controlled by `Pilot-Correction-Blank-Broken-Statistics.md`. The historical Phase 7 assertion counts remain a record of what was tested at that time; they do not validate the corrected Auto/manual propagation, four threshold families, blank-Notes threshold-only suppression versus nonblank-Notes contextual preservation, resolution-state independence, or corrected PDF/index outcomes.

> **Deferred-text pilot correction:** Free-text statistics explanations, checklist Notes, Created By, Original File Name, and General Notes no longer run model/readiness/findings work per character. They commit on `Validated` or the form's explicit action-boundary batch. Numeric, choice, applicability, and Auto/manual controls remain immediate. The readiness fingerprint still includes every committed text value. Final verification is recorded in `Pilot-Correction-Deferred-Text-Commit.md` and `V2-Production-Readiness.md`.

> **Arrival Month pilot correction:** The active Raw File checklist no longer contains `RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH`. File Month statistics remain visible and editable. Internally valid statistics create exactly one `QaFindingSource.Statistic` Failure, `STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH`, only when the exact outside-count ratio is greater than 30% of Valid Arrival Date Count. Exactly 30% does not fail and no automatic Warning is created at or below the threshold. See `Pilot-Correction-Arrival-Month-Threshold.md`.

## Repository baseline

- Required and active branch: `v2-qa-reports`.
- Approved Phase 6 baseline: `2c599ea3ca0be6c5c434de625a95ef8c788aabd0`.
- Starting working-tree commit: `f028e31402a04aa4d20106e44a911ffaa8904c55`.
- Approved Phase 2 commit: `7d94a27a4569f8ca521aeaa6c08da0e510fc4dc7`.
- Approved Phase 1 commit: `49beaa1f45700725d328ade215419254560cb406`.

The Phase 6, Phase 2, and Phase 1 ancestry checks succeeded before editing. The worktree was initially clean, recent history was inspected, and the baseline solution built with zero warnings and zero errors. No commit or push occurred before implementation or before explicit instruction. After implementation and explicit instruction, the completed Phase 7 implementation was committed as `f61a5264336cb969b5310a2d6ba61434149f9bc6` and pushed only to `v2-qa-reports`. It was not merged into `main`, and no rebase, force-push, history rewrite, or tag modification occurred.

The project remains an SDK-style WinForms application targeting `net10.0-windows`. No project or package change is part of this phase.

## Files created and modified

Created:

- `DocumentationLoggingDashboard/QAReports/Definitions/QaStatisticFieldIds.cs`
- `DocumentationLoggingDashboard/QAReports/Definitions/QaStatisticFieldDefinition.cs`
- `DocumentationLoggingDashboard/QAReports/Definitions/QaStatisticFieldCatalog.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaBlankStatisticRowControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaBrokenDataStatisticRowControl.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/Controls/QaStatisticsControl.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaStatisticsCalculationService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportValidationService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportStatusService.cs`
- `DocumentationLoggingDashboard/QAReports/Validation/QaReportValidationResult.cs`
- `docs/v2/Phase-7-Statistics-Validation-Status.md`

Modified:

- `DocumentationLoggingDashboard/QAReports/Definitions/QaFindingIds.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.Designer.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaFindingSynchronizationService.cs`

No `.resx`, `.csproj`, MainForm, Program, Phase 2 model, Phase 3 metadata contract/service, Phase 4 metadata form/service, protected V1 model, or protected V1 service was changed.

## Statistics UI

The existing report form now contains a `Statistics & Readiness` tab between DB QA and Findings. Existing Report Details, Raw File QA, DB QA, Findings, Warnings, Failed Checks, checklist controls, and finding-resolution controls remain in the same workflow.

One reusable, vertically scrollable `QaStatisticsControl` provides these groups:

1. File Information
2. Blank Value Statistics
3. Broken Data Statistics
4. Name Statistics
5. File Month Statistics
6. Monetary Statistics
7. Database Statistics
8. Report Readiness Summary

Counts use standard `NumericUpDown` controls with `0` through `int.MaxValue` limits. Blank and Broken denominators are editable numeric controls with visible Auto state; percentages, signed difference, and absolute difference remain read-only. Editing one denominator switches only that row to Manual, while selecting Auto immediately restores its current formula. Enum and Boolean choices use fixed drop-down lists. Dynamic Blank and Broken rows are keyed by stable IDs, reused while applicable, detached and disposed when stale, and arranged in wrapping/scrolling layouts compatible with the existing minimum form size and normal WinForms font scaling.

The form action is named `Check Report Readiness`. It is separate from Close and is not a Save, Generate, Export, or PDF action. The read-only summary separates blocking errors from nonblocking workflow warnings and also shows Warning count, Failure/Failed Checks count, handled finding count, calculated status or Not Ready, and Effective Created By.

The statistics view includes the exact privacy direction:

```text
Use counts and non-sensitive summaries only. Do not enter guest names,
email addresses, payment information, credentials, or reservation-level data.
```

## Stable statistics field catalog and applicability

`QaStatisticFieldCatalog` is an immutable definition layer. It describes stable ID, display name, applicability, optional related checklist ID, and blank/broken support without duplicating statistics data.

| Stable ID | Display name | Applicability | Related broken-data check |
| --- | --- | --- | --- |
| `FIRST_NAME` | First Name | Separate Name mode | `RAW.NAMES.FIRST_NAME_VALUES_VALID` |
| `LAST_NAME` | Last Name | Separate Name mode | `RAW.NAMES.LAST_NAME_VALUES_VALID` |
| `FULL_NAME` | Full Name | Full Name mode | `RAW.NAMES.FULL_NAME_VALUES_VALID` |
| `CONFIRMATION_NUMBER` | Confirmation Number | Always | None |
| `EMAIL` | Email | Always | None |
| `RESERVATION_DATE` | Reservation Date | Always | `RAW.DATES.FORMAT_VALID` |
| `ARRIVAL_DATE` | Arrival Date | Always | `RAW.DATES.FORMAT_VALID` |
| `DEPARTURE_DATE` | Departure Date | Always | `RAW.DATES.FORMAT_VALID` |
| `AVERAGE_RATE` | Average Rate | Always | None |
| `STAY_VALUE` | Stay Value | Two or more monetary columns | None |
| `SOURCE_RATE_MARKET` | Source / Rate / Market | Always | None |

The Phase 2 characteristics do not contain a separate Source/Rate/Market existence flag, so that catalog concept is structurally applicable. Average Rate is structurally available for every approved monetary scenario. Stay Value is available for `TwoMonetaryColumns` and `MoreThanTwoMonetaryColumns` only.

Changing name mode removes the inapplicable name rows. Full Name mode also clears the non-list multiword First/Last state. Removing Stay Value applicability removes its list rows, clears unusual/high Stay Value state and explanations, and removes its managed findings. A row that later becomes applicable is created fresh in Auto mode from current values; stale hidden denominator overrides are not restored. Rejected-record applicability continues through the existing Phase 5 characteristic and checklist applicability path, with the database statistic count as the authoritative input.

## Calculation and denominator behavior

`QaStatisticsCalculationService` reconciles the approved statistics objects and centralizes derived values. It reuses existing applicable row objects, removes non-applicable rows, maintains catalog order, applies automatic propagation only to rows still in Auto mode, and calculates with decimal arithmetic:

```text
Percentage = denominator <= 0
    ? 0
    : Round(numerator * 100 / denominator, 2)
```

Percentages remain the approved non-nullable `decimal` values on a 0–100 scale and display with two decimal places. Zero denominators produce `0.00%`; no floating-point `NaN` or infinity is possible. Validation, rather than silent clamping, rejects negative values and numerators above denominators.

Each automatic Blank denominator follows File Information:

```text
Total Applicable Rows = Total Data Rows
```

`Total Data Rows` is entered as the number of rows containing data, excluding headers and any preamble. Headers Present, Useful Headers, and Data Start Row describe the file layout and do not cause a second subtraction. Thus, a file with 120 occupied physical rows, a header on row 1, and data beginning on row 2 has Total Data Rows `119` and an Auto Blank denominator of `119`.

Each automatic Broken denominator follows its matching Blank row:

```text
Applicable Nonblank Count = Total Applicable Rows - Blank Count
```

The user may override either denominator independently. A Manual Blank denominator remains the source population for its still-automatic Broken row. A Manual Broken denominator may represent a smaller inspected populated subset but cannot exceed the matching Blank-derived nonblank population. The same Blank-derived denominator is used for multiword First Name, multiword Last Name, unusual Average Rate, unusual Stay Value, and high Stay Value percentages. Validation requires the corresponding Blank row and checks Auto formulas, Manual bounds, and displayed percentages without silently clamping counts.

The database difference remains signed in the approved model:

```text
RawMinusImportedRecordCountDifference = Total Data Rows - Imported Record Count
```

The warning condition and second read-only UI value use its absolute magnitude. Neither value is user editable.

## Blank and broken data behavior

Every applicable catalog field has exactly one approved `QaBlankValueStatistic` and one `QaBrokenDataStatistic`. Display text is refreshed from the catalog, but identity is always the stable field ID.

A positive Blank percentage through 50%, inclusive, creates one Warning:

```text
WARN:STAT:BLANK:<FieldId>
```

Above 50% creates one Failure:

```text
FAIL:STAT:BLANK:<FieldId>
```

Zero creates no Blank finding. Exactly 50% is a Warning. The description includes display name, Blank count, displayed applicable-row denominator, and percentage. Blank findings use Statistic source and never force required-field-presence or populated-value-validity checks to Fail.

A positive Broken percentage through 50%, inclusive, creates one Warning:

```text
WARN:STAT:BROKEN:<FieldId>
```

Above 50% creates one Failure:

```text
FAIL:STAT:BROKEN:<FieldId>
```

Zero creates no Broken finding. Exactly 50% is a Warning. Each finding includes the field, count, displayed nonblank denominator, percentage, and optional non-sensitive explanation. It uses Statistic source and Active resolution when newly created. Failure findings can be handled by custom script under the existing Phase 6 support rules but can never be Explained and Accepted.

At or below 50%, a mapped populated-value-validity check may remain Pass. Above 50%, the related check must be Fail or readiness reports a contradiction. The canonical Failure remains `FAIL:STAT:BROKEN:<FieldId>`. Synchronization suppresses the corresponding generic `FAIL:<ChecklistId>` only when the current failing checklist result has blank Notes after trimming, identifying the threshold-only path. Nonblank checklist Notes document a separate contextual defect, so both the canonical statistics Failure and contextual checklist Failure remain. Resolution state alone is not a causal signal and does not control this choice. Several date fields may still map to the one date-format checklist row, but each affected field retains its own canonical statistics identity.

Blank and Broken findings remain independent. For example, Blank `20/100` and Broken `10/80` produce 20.00% and 12.50% and may produce two Warnings without a Failure.

## Name and File Month statistics

Multiword First/Last counts appear only in Separate Name mode. Their read-only denominators come from the applicable First/Last blank rows, and their percentages are calculated centrally. Counts above the derived nonblank values block readiness. These values are informational and create no finding. Full Name mode hides the group and clears all four stored multiword values.

File Month statistics require:

```text
Within Count + Outside Count = Valid Arrival Date Count
```

All three counts must be nonnegative. Within Count and Outside Count must each be no greater than Valid Arrival Date Count, and their sum must equal Valid Arrival Date Count. Valid Arrival Date Count cannot exceed the derived nonblank Arrival Date count.

Both percentages use Valid Arrival Date Count and safely become zero for a zero denominator. When Valid Arrival Date Count is zero, Within Count and Outside Count must also be zero. Invalid count relationships are readiness errors, not findings.

The threshold decision uses exact counts rather than the rounded percentage display:

```text
Outside Count * 100 > Valid Arrival Date Count * 30
```

With a positive denominator and valid totals, the exact greater-than comparison creates one Active Failure:

```text
STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH
```

The finding has Statistic source, Failure severity, and no related checklist ID. Its description includes the selected File Month, valid count, inside count, outside count, and a sufficiently precise outside percentage. Exactly 30% and all lower values create no Arrival/File Month finding or Warning.

While the condition remains continuously above 30%, synchronization preserves the current resolution and custom-script fields while refreshing contextual counts. When the condition falls to 30% or lower, the finding is removed and its obsolete resolution state is discarded. A later recurrence is a new Active Failure.

## Monetary statistics

Unusual Average Rate and applicable unusual Stay Value are manual Yes/No determinations; Phase 7 does not invent an unusual-value algorithm. Yes requires a positive count and a non-sensitive explanation. The percentage uses the field's derived nonblank denominator. The deterministic Warnings are:

```text
WARN:STAT:UNUSUAL:AVERAGE_RATE
WARN:STAT:UNUSUAL:STAY_VALUE
```

No clears count, percentage, explanation, and finding. These Warnings support Active, Explained and Accepted, and Handled by Custom Script under the existing resolution rules.

High Stay Value captures a manual count of values strictly above 10,000; exactly 10,000 is excluded by the UI label and documented rule. The percentage uses the derived Stay Value nonblank denominator. A positive count with High Values Expected set to No requires an explanation and creates:

```text
WARN:STAT:HIGH_STAY_VALUE
```

Expected Yes preserves the count and percentage without a Warning. Zero removes the Warning. High values alone never fail a checklist item. All Stay Value controls and values are cleared when Stay Value is not applicable.

## Database statistics

The database group captures Imported Record Count, Rejected Record Count, and Records With Missing Required DB Values. It displays the signed raw-minus-imported result and its absolute value.

An absolute difference of 10 or more, including both signed `10` and `-10`, creates:

```text
WARN:STAT:ROW_DIFFERENCE
```

The DatabaseComparison description includes raw rows, imported rows, signed difference, absolute difference, and the entered context. A non-sensitive explanation is required for readiness. Because the Phase 2 database model has no explanation property, this explanation is stored on the managed finding's existing `ResolutionNotes`; the same field remains compatible with Phase 6 resolution handling. Falling below 10 removes the finding and its state.

Rejected Record Count drives `HasRejectedDatabaseRecords`, which in turn drives `DB.REJECTED_RECORDS_ACCOUNTED_FOR` through the existing applicability evaluator. Zero makes the characteristic false, clears/NAs the checklist row and explanation, and removes stale findings. Positive makes the check applicable. Pass with a non-sensitive checklist explanation creates one DatabaseComparison Warning:

```text
WARN:STAT:REJECTED_RECORDS
```

Fail relies only on the existing checklist Failure and suppresses both the statistics Warning and the generic passed-check Warning. Because the approved database statistic has no explanation property, the explanation is the existing rejected-record checklist result's `Notes`.

A positive Records With Missing Required DB Values count requires `DB.REQUIRED_VALUES_PRESENT` to be Fail. The checklist Failure remains the single failed condition. A positive count with another status blocks readiness. A zero count with that check failed requires meaningful notes explaining a legitimate related failure.

## Deterministic findings and lifecycle

The existing `QaFindingIds` now also owns all statistic IDs and per-field builders. The existing `QaFindingSynchronizationService` remains the one authoritative reconciler for checklist Failures, passed-check Warnings, characteristic Warnings, statistics findings, and retained unrelated findings in `QaReport.Findings`.

The service validates catalog/check relationships and protects the full managed ID space from collisions. Duplicate expected or existing IDs are treated as programming/data defects. Findings are sorted deterministically.

While a condition remains continuously true, the same `QaFinding` object is reused. Generated identity, context, severity, source, title, and description are refreshed while valid resolution, resolution notes, and optional script name are preserved. Resolution never changes original severity.

When a managed condition ends, its finding is removed without a cache or tombstone. Old resolution state is discarded with that object. A later recurrence creates a new Active finding with no restored notes or script name. Failure/Warning ID prefixes and expected-rule validation also guard against severity drift. Invalid Explained and Accepted Failures and unsupported custom-script resolutions are normalized by the Phase 6 rules.

## Completion validation and workflow results

`QaReportValidationService` performs read-only validation on the synchronized current report. It returns `QaReportValidationResult`, which separates:

- `BlockingErrors`
- `WorkflowWarnings`
- `CalculatedStatus`
- `EffectiveCreatedBy`
- Warning, Failure, and handled finding counts

It validates canonical Hotel ID/Name/PMS, File Month, QA Date, file-characteristic enum values, File Information, exact active-checklist-result coverage/applicability/status/source, required notes, complete statistics coverage, numeric relationships, calculated percentages, Auto formulas, Manual denominator bounds, mapped checklist consistency above 50%, File Month count integrity and threshold finding consistency, monetary consistency, database consistency, deterministic finding coverage, finding enums and related IDs, resolution validity, stale managed findings, duplicate IDs, and explanation requirements.

Zero Total Data Rows is permitted only when a failed checklist result or current Failure coherently documents the empty/invalid file. With positive data rows, Blank applicable-row denominators normally must be entered and compatible with Total Data Rows. A zero Blank denominator is accepted only for a field whose matching required-field presence check is Fail, documenting that the field itself is absent. A Broken denominator may also be zero when the matching Blank row leaves a genuine zero nonblank population; a positive Broken count with that denominator remains invalid.

Workflow completion problems remain structured blocking errors; they do not become `QaFinding` objects. This includes missing Hotel selection, invalid File Month, incomplete active checklist items, impossible Auto or Manual denominators, File Month count inconsistencies, count/denominator errors, unsynchronized percentages, and greater-than-50% mapped Broken/checklist contradictions. Positive Blank, Broken, or outside-File-Month counts do not block merely because they are positive; ready Warning and ready Failure reports are both supported.

## Created By workflow

Created By remains optional. Null, empty, or whitespace does not block readiness and does not create a finding. It produces exactly this workflow warning:

```text
This QA report does not have an assigned QA person.
```

The effective downstream value is:

```text
InnoVarxi QA Team
```

The validation result and form expose that value without mutating `QaReport.CreatedBy`. A nonblank value is trimmed, removes the workflow warning, and becomes Effective Created By.

## Status calculation and invalidation

`QaReportStatusService` calculates no status when any blocking error exists. Otherwise:

- Active Failure finding: `Fail`.
- No active Failure and at least one current Warning or handled Failure: `PassWithWarnings`.
- No findings: `Pass`.

Handled Failures retain Failure severity and therefore produce Pass with Warnings, never Pass. Blocking completion problems produce `IsReady == false`, a null calculated status, a null `CurrentReport.ReportStatus`, and `Not Ready`; Fail is not used to mean incomplete.

The readiness action commits current controls, applies the existing applicability path, recalculates statistics, synchronizes the one findings collection, refreshes controls, validates, and assigns `CurrentReport.ReportStatus` only for a ready result. It writes no file.

One centralized invalidation method clears both `ReportStatus` and the last readiness result after relevant edits to report details, Hotel/PMS, dates, characteristics, checklist status/notes/warning selection, finding resolution fields, statistics values, explanations, or applicability. After a previous readiness result, the summary becomes:

```text
Not Ready — report changed; validate again
```

Live calculation and finding synchronization continue, but an authoritative final status returns only through `Check Report Readiness`. Existing synchronization guards prevent recursive events.

## Verification record

### Required builds

The exact pre-edit baseline command was:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

Result: exit code 0, build succeeded, 0 warnings, and 0 errors in 3.82 seconds.

The same command was run repeatedly against the integrated Phase 7 implementation during development and at the final post-documentation gate. The final build succeeded with 0 warnings and 0 errors.

### Directly executed pure-component tests

> **Superseded scope note:** The results below are retained as historical Phase 7 evidence. The original threshold and read-only-denominator assertions are not accepted as evidence for the final correction. Current evidence is tracked separately: semantic tests passed 7/7 with header exclusion, First/Full/Last Name, and resolution-noncausality coverage; STA geometry passed 7/7 at actual `DeviceDpi=120`; save/PDF/index, blocked-save, overwrite, and protected V1 regressions passed. Real 100% and 150% scaling remain Not Run. See `V2-Production-Readiness.md`.

A disposable initial harness directly exercised statistics applicability, preservation/removal of list-row objects, percentage and denominator arithmetic, signed/absolute differences, all status branches, complete Pass readiness, missing Created By fallback without model mutation, and zero-row blocking. It completed 24 assertions successfully. Its project, build output, and temporary files were removed.

The final disposable `net10.0-windows` harness completed:

```text
RESULT assertions=141 passed=141 failed=0 skipped=0
```

Of those, 122 assertions directly exercised definitions and pure calculation, applicability, synchronized finding, lifecycle, validation, and status services. Coverage included all requested threshold and recurrence boundaries; mapped and unmapped broken data; multiword and Full Name transitions; File Month equality/checklist consistency; unusual and high monetary behavior; `+9`, `+10`, and `-10` database differences; rejected-record applicability and Warning/Failure suppression; missing DB values; Created By fallback/no mutation; every status branch; Not Evaluated blocking; and duplicate, stale, and invalid-denominator validation.

The remaining 19 assertions used an STA offscreen production `QaReportForm`. They verified the five tabs and their order, statistics scrolling/minimum-size structure, privacy reminder content, `int` numeric limits, dynamic row counts, the then-current read-only Broken denominators, readiness-button navigation to the summary, ready Pass/status display, statistic-edit invalidation with the required stale text, revalidation, and Close with no storage write. The storage probe remained absent. The read-only-denominator assertion is specifically superseded and must not be treated as current expected behavior.

Both historical Phase 7 product and harness builds completed with 0 warnings and 0 errors. That harness project, `bin`/`obj`, storage probe, and process were removed; no permanent test project or test package was part of the Phase 7 checkpoint. The controlled-pilot correction adds the permanent focused project `tests/DocumentationLoggingDashboard.GeometryTests`, with semantic, geometry/interaction, and synthetic save/PDF/index files. Its completed and still-open results are recorded in `Pilot-Correction-Blank-Broken-Statistics.md`; this historical section does not approve the correction.

### Human-visible GUI testing

No claim is made that a person interacted with or visually inspected a visible WinForms window through the execution interface. A manual desktop pass remains appropriate for multi-monitor DPI behavior, real keyboard navigation, screen-reader output, and subjective visual spacing.

Manual desktop checklist:

1. Open Create QA Report and confirm the tab order is Report Details, Raw File QA, DB QA, Statistics & Readiness, and Findings.
2. Resize to the existing minimum window size and confirm every statistics group remains reachable by scrolling without clipped inputs.
3. Repeat at normal Windows 100%, 125%, and 150% text/display scaling and check label wrapping, focus order, and scroll-wheel behavior.
4. Toggle Separate Name and Full Name modes; confirm the blank/broken name rows and informational multiword group switch and clear as documented.
5. Toggle one versus two/more monetary columns; confirm all Stay Value rows, explanations, and findings hide/clear or reappear correctly.
6. Enter representative large `int` counts and verify calculated fields remain read-only and percentages show two decimal places.
7. Exercise `0`, `1`, `50`, and `51` out of `100` for both Blank and Broken values; confirm exactly 50% is Warning and 51% is Failure.
8. Exercise mapped and unmapped Broken values, including blank-Notes threshold-only suppression, nonblank-Notes preservation of both contextual and statistics Failures, resolution-state independence, and Warning/Failed Checks separation.
9. Verify visible privacy reminders are adjacent to Broken Data, Monetary, and Database free-text areas.
10. Enter rejected-record context from Statistics, switch to DB QA, and confirm the same checklist notes are visible; edit there and confirm Statistics reflects the update.
11. Check readiness for Pass, Pass with Warnings, Fail, and an incomplete report; verify summary sections and status wording.
12. Edit every input family after a successful readiness result and confirm `Not Ready — report changed; validate again` replaces the old authority.
13. Use keyboard-only navigation through statistics rows, the readiness action, Findings resolution choices, and Close.
14. Close with Escape and Close after entering data; verify no report, PDF, index entry, or Hotel/PMS copy is created.

### Source-inspection regression checks

The Phase 1–6 documentation, actual Phase 2 models/enums, checklist catalogs and applicability evaluator, Phase 5/6 form and Designer, checklist/finding controls, report synchronization paths, and the V1 regression checklist were inspected before design and implementation.

Source and final-diff review verify that Program/MainForm startup routing, the three V1 workflows and actions, protected V1 models/services/settings/output behavior, Phase 3 metadata schemas/persistence, Phase 4 Hotel/PMS management/search/canonical PMS behavior, Phase 5 report opening and dynamic checklist applicability, and Phase 6 resolution semantics are outside the changed file set. Existing checklist IDs and the Phase 2 statistics contract remain unchanged.

Human-visible V1 Preview/Submit, folder selection/reset, shell-opening actions, real metadata management, and native application shutdown are source-review-only unless explicitly reported otherwise in the final test record.

## Repository-specific design decisions

- The existing Phase 2 contract is sufficient; all missing denominators are derived from blank statistics.
- Stay Value applicability is represented by the existing monetary-column scenario: two or more monetary fields.
- Source / Rate / Market is structurally applicable because no existing characteristic records its absence.
- Average Rate is structurally applicable in all approved monetary scenarios.
- Blank and Broken denominators are automatic by default, editable per row, and return to their current formula through the visible Auto control; validation catches impossible inputs rather than masking them.
- Row-difference context uses the managed finding's `ResolutionNotes`; rejected-record context uses the related checklist result's `Notes`. These are existing approved properties and avoid a replacement DTO or Phase 2 contract change.
- Rejected Record Count is authoritative for the pre-existing rejected-record characteristic. The older characteristic radio group is retained for layout/history but disabled and clearly labelled as statistics-driven.
- Useful Headers remains informational and creates no finding.
- The form catches an unexpected readiness synchronization exception and exposes a generic structured blocker while leaving status null; diagnostic detail is written only to the debugger.

## Deferred Phase 8 work

Phase 8 may consume only a current ready validation result and its synchronized in-memory report. It should use the approved report details, statistics, findings, `QaReport.ReportStatus`, and `EffectiveCreatedBy` produced here. It must revalidate or otherwise ensure the result is not stale before generating output.

Phase 8 remains responsible for Report ID policy, PDF selection/generation/preview, filenames, saving, overwrite decisions, Hotel/PMS copies, QA Report index writing, and persistence failure handling. None exists in Phase 7.

## Final scope confirmations

- The approved Phase 2 report/statistics/finding models and status enum are reused unchanged.
- `QaReport.ReportStatus` is the only report status property; no Incomplete enum was added.
- `QaReport.Findings` remains the single existing list synchronized in place.
- The existing Phase 6 finding service was extended rather than duplicated.
- No PDF, report saving, overwrite, copy, index, parsing, spreadsheet, database, diagnostic, or automation behavior was added.
- No metadata schema/editing behavior, protected V1 model/service, Program, or MainForm behavior was changed.
- No package or project-file change was added.
- The completed Phase 7 implementation was subsequently committed as `f61a5264336cb969b5310a2d6ba61434149f9bc6` and pushed only to `v2-qa-reports` after explicit instruction. It was not merged into `main`, and no rebase, force-push, history rewrite, or tag modification occurred.
