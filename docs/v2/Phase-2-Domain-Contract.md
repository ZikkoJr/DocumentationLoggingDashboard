# Phase 2 Domain Contract

## Purpose

Phase 2 defines the foundational QA report domain contract for Version 2. It adds report models, checklist metadata, stable checklist IDs, structured statistics, finding audit fields, and documentation only.

This phase does not add a working QA workflow. It does not add UI, dynamic applicability, validation, warning generation, failure generation, status calculation, custom-script evaluation, PDF generation, JSON serialization, storage, database access, raw-file parsing, spreadsheet parsing, or automation.

> **Post-Phase-10 pilot correction:** Phase 2 remains the historical foundation, but the current runtime contract now gives every Blank and Broken denominator an explicit automatic/manual state. Missing mode properties default safely to Auto. Current threshold and propagation rules are defined in `Pilot-Correction-Blank-Broken-Statistics.md`; Phase 2's original “deferred” statements remain historical scope statements, not current runtime limitations.

> **Deferred-text boundary:** The final V2 correction changes when QA report free text is committed, not the domain values or readiness fields themselves. Draft text is committed at validation or an explicit workflow/action boundary, then the existing domain synchronization runs once. Final verification is recorded in `Pilot-Correction-Deferred-Text-Commit.md` and `V2-Production-Readiness.md`.

> **Post-pilot checklist correction:** `RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH`, `RAW.REQUIRED.EMAIL_PRESENT`, and `RAW.SOURCE.COLUMN_PRESENT` are retained only as retired stable identifiers and are no longer active definitions. After adding the replacement Email and three Strategy availability checks, the active catalog contains 22 Raw File definitions and 7 Database definitions, 29 total. File Month statistics remain part of the domain contract. More than 30% outside the selected File Month creates one statistics-sourced Failure using Valid Arrival Date Count; exactly 30% or less creates no Arrival/File Month finding. See `Pilot-Correction-Arrival-Month-Threshold.md` and `../updates/Detailed-QA-Check-Changes.md`.

## Architectural Principle

The manual UI, future PDF renderer, and future automated diagnostic system must all use the same QA domain models and stable checklist identifiers. Those layers should not create separate representations of QA reports, checklist results, findings, or checklist IDs.

## Source Folders and Namespaces

Runtime contract code is isolated under:

```text
DocumentationLoggingDashboard/QAReports/Models
DocumentationLoggingDashboard/QAReports/Definitions
```

The namespaces are:

```text
DocumentationLoggingDashboard.QAReports.Models
DocumentationLoggingDashboard.QAReports.Definitions
```

No project-file change is required because the application project is SDK-style and automatically includes new `.cs` files.

## Top-Level Report

`QaReport` is the top-level report model. It contains:

- `SchemaVersion`
- `ReportId`
- `HotelInformation`
- `QaDate`
- optional `CreatedBy`
- optional `OriginalFileName`
- `FileCharacteristics`
- `ChecklistResults`
- `Statistics`
- `Findings`
- optional `ReportStatus`
- optional `GeneralNotes`

`QaReport.CurrentSchemaVersion` is `1`, and new reports default `SchemaVersion` to that value.

`QaDate` is a `DateOnly` because QA Date is a date-only review date. QA Date is separate from File Month and must not be represented by `QaFileMonth`.

`CreatedBy`, `OriginalFileName`, `ReportStatus`, and `GeneralNotes` are nullable so an incomplete draft can exist without manufactured values.

## Hotel Information and File Month

`QaHotelInformation` stores report-snapshot values:

- `HotelName`
- `HotelId`
- `PmsName`
- nullable `FileMonth`

It is not a future `hotels.json`, hotel metadata, or PMS metadata model.

`QaFileMonth` is an immutable value object that stores only `Year` and `Month`. It rejects month values below `1` or above `12` with `ArgumentOutOfRangeException`. It does not store or imply a meaningful day. `ToString()` formats using culture-independent `yyyy-MM` output, such as `2026-04` and `2026-11`. Equal year/month values have value equality.

## File Characteristics

`QaFileCharacteristics` stores input facts that later phases can use for applicability and warnings:

- `NameColumnMode`
- `HasCurrencyColumn`
- `MonetaryColumnScenario`
- `HasMultipleConfirmationNumberCandidateColumns`
- `IsCustomScriptSupportAvailable`
- `HasRejectedDatabaseRecords`

`QaNameColumnMode` supports separate First/Last Name columns and Full Name. `QaMonetaryColumnScenario` supports one monetary column, two monetary columns, and more than two monetary columns. Currency remains optional and is represented by `HasCurrencyColumn`.

The model stores characteristics only. It does not evaluate applicability, generate warnings, or generate failures.

## Checklist Results

`QaCheckResult` stores report-specific state for one stable checklist definition:

- `CheckId`
- `Status`
- optional `Notes`
- `ResultSource`
- optional `EvaluatedAt`

`CheckId` stores the stable checklist ID, not display text. `EvaluatedAt` is `DateTimeOffset?` for an optional audit timestamp.

`QaCheckStatus` values are:

- `NotEvaluated`: temporary internal draft state.
- `Pass`: check completed successfully.
- `Fail`: checklist requirement was not satisfied.
- `NotApplicable`: definition does not apply to the selected file configuration.

Warnings are not checklist statuses. `NotEvaluated` is for internal drafts; later generated reports should be based on resolved Pass, Fail, or N/A states.

`QaResultSource` supports `Manual` and `Automated`. Version 2 is manual-first, but future diagnostics can populate the same result type.

## Findings and Custom-Script Audit

`QaFinding` stores warnings and failures independently from checklist result status. It contains:

- `FindingId`
- optional `RelatedCheckId`
- `Severity`
- `Title`
- `Description`
- `Resolution`
- optional `ResolutionNotes`
- optional `CustomScriptName`
- `Source`

`RelatedCheckId` is optional because manual and statistic findings may not relate to a checklist definition.

`QaFindingSeverity` supports `Warning` and `Failure`. `QaFindingResolution` supports `Active`, `HandledByCustomScript`, and `ExplainedAndAccepted`. `QaFindingSource` supports `Checklist`, `Statistic`, `DatabaseComparison`, and `Manual`.

Resolution does not replace severity. A finding with `Failure` severity can be marked `HandledByCustomScript` and still remain a failure in the audit history. Each finding is independently resolvable. Handled and explained findings remain stored.

The models support this later custom-script workflow without implementing it:

1. Custom-script support is marked available in file characteristics.
2. Warnings and failures are stored as findings.
3. Each finding receives its own resolution.
4. A handled finding uses `HandledByCustomScript`.
5. An unhandled finding remains `Active`.
6. A manual explanation can use `ExplainedAndAccepted`.
7. Original findings and original severity remain stored.
8. Later report-status rules may use handled findings to produce Pass with Warnings rather than Fail.

No UI, automatic matching, automatic resolution, script execution, or status calculation is implemented.

## Report Status

`QaReportStatus` supports:

- `Pass`
- `PassWithWarnings`
- `Fail`

`QaReport.ReportStatus` is nullable. Phase 2 does not calculate report status.

## Statistics

`QaStatistics` groups structured statistics instead of placing them in a single notes field. Count properties use integer types. Percentage properties use `decimal` and a `0` through `100` scale, not `0` through `1` fractions.

Statistics groups are:

- `QaFileInformationStatistics`: total data rows that contain data, whether headers are present, useful headers result, and data start row. Total Data Rows excludes header and preamble rows; the layout metadata does not trigger another subtraction.
- `QaBlankValueStatistic`: extensible per-field blank statistics with stable field identifier, display name, blank count, total applicable rows, explicit automatic/manual denominator state, and blank percentage.
- `QaBrokenDataStatistic`: extensible per-field statistics for populated values containing malformed, corrupted, or otherwise incorrect data, with stable field identifier, display name, broken-value count, total applicable nonblank values, explicit automatic/manual denominator state, broken-data percentage, and optional explanation.
- `QaMultiwordNameStatistics`: multiword First Name count and percentage, plus multiword Last Name count and percentage.
- `QaFileMonthStatistics`: Arrival Dates within the selected File Month, Arrival Dates outside it, percentage within, percentage outside, and valid Arrival Date count used as the denominator.
- `QaUnusualMonetaryValueStatistics`: reusable representation for unusual Average Rate values and unusual Stay Values, including whether unusual values were found, count, percentage, and optional explanation.
- `QaHighStayValueStatistics`: number of Stay Values strictly above 10,000, percentage above 10,000, whether high values are expected, and optional explanation.
- `QaDatabaseStatistics`: imported-record count, rejected-record count, records with missing required DB values, and row-count difference.

`QaDatabaseStatistics.RawMinusImportedRecordCountDifference` is signed and means raw data rows minus imported DB records.

Blank-value entries are extensible and can represent fields such as First Name, Last Name, Full Name, Stay Value, Average Rate, Confirmation Number, Reservation Date, Arrival Date, Departure Date, and commission/source-related fields. A missing required field is not the same as blanks inside an existing field.

Broken Data Statistics are separate from Blank Value Statistics. A `QaBlankValueStatistic` measures missing cells in an applicable field. A `QaBrokenDataStatistic` measures populated cells whose contents are malformed, corrupted, or otherwise incorrect. For example, a future process could supply entries for email addresses in name fields, malformed names, invalid nonblank dates, corrupted Confirmation Numbers, or invalid nonblank monetary values.

In the corrected runtime workflow, an automatic Blank denominator follows the operator-entered Total Data Rows. That value is already the data-only count: for 120 occupied physical rows with a header on row 1 and data starting on row 2, Total Data Rows is `119`, not `120`. Headers Present and Data Start Row are descriptive metadata and do not cause another subtraction. An automatic Broken denominator follows the matching Blank denominator minus its Blank Count. A manual override is stored independently on each row and does not merge the two statistic types.

Each reusable `QaBlankValueStatistic` entry contains:

- `FieldId`: stable identifier for the applicable field.
- `DisplayName`: human-readable field name.
- `BlankValueCount`: integer count of blank cells.
- `TotalApplicableRows`: displayed applicable-row denominator.
- `UseAutomaticTotalApplicableRows`: `true` when the denominator follows Total Data Rows; `false` for a manual override.
- `BlankValuePercentage`: `decimal` percentage on the shared `0` through `100` statistics scale.

Each reusable `QaBrokenDataStatistic` entry contains:

- `FieldId`: stable identifier for the applicable field.
- `DisplayName`: human-readable field name.
- `BrokenValueCount`: integer count of populated values identified as broken.
- `TotalApplicableNonblankValues`: integer count of all applicable nonblank values for that field and the denominator represented by the entry. It does not include blank values.
- `UseAutomaticTotalApplicableNonblankValues`: `true` when the denominator follows the matching Blank-derived nonblank population; `false` when the user has supplied a manual subset.
- `BrokenDataPercentage`: `decimal` percentage on the shared `0` through `100` statistics scale.
- optional `Explanation`: additional supplied context about the broken data.

`QaStatistics.BrokenData` exposes a collection of these entries. Entries may be created for any applicable field; the contract does not hard-code a closed list of supported fields. `QaStatistics.BlankValues` remains a separate collection, so blank counts and broken-value counts cannot be confused structurally.

Phase 2 defines only the representation used to store supplied Broken Data Statistics. It does not detect broken data, validate field contents, calculate percentages, or generate warnings or failures.

Phase 2 does not calculate thresholds, warning rules, unusual-value algorithms, high-value warnings, row-count-difference warnings, out-of-month failures, or any other report rules.

## Checklist Definitions

`QaCheckDefinition` is immutable metadata for a checklist item. It contains:

- stable `Id`
- `DisplayName`
- `Description`
- `Section`
- `Applicability`
- `AppearsOnGeneratedReport`
- `MustBeResolvedBeforeReportGeneration`

It does not contain status, user notes, evaluated timestamp, finding resolution, UI controls, visibility logic, evaluation delegates, storage behavior, or service behavior.

`QaChecklistIds` contains deterministic string constants. IDs are not generated at runtime.

`QaChecklistCatalog.Definitions` exposes one deterministic read-only active catalog. It contains exactly 22 Raw File definitions, 7 Database definitions, and 29 total definitions. Raw File definitions appear first in the approved order; Database definitions appear after them in the approved order. The three retired IDs remain available for source and historical-artifact compatibility, but no active definition uses them and new reports contain no result for them.

Every Phase 2 definition has:

- `AppearsOnGeneratedReport = true`
- `MustBeResolvedBeforeReportGeneration = true`

Checklist sections are `RawFile` and `Database`.

Applicability metadata values are:

- `Always`
- `SeparateNameColumns`
- `FullNameColumn`
- `CurrencyColumnPresent`
- `OneMonetaryColumn`
- `TwoMonetaryColumns`
- `MoreThanTwoMonetaryColumns`
- `RejectedRecordsPresent`

Phase 2 stores applicability metadata but does not evaluate it. Conditional definitions will later receive a `NotApplicable` result where appropriate.

## Warning Versus Failure

A failure means a checklist requirement was not satisfied or a corrected statistics threshold was exceeded. Examples include a required field being absent, a populated-value validity condition with Broken Data above 50%, more than 30% of valid nonblank Arrival Dates being outside the selected File Month, a DB value differing from the processed raw value, a required DB value being missing, or all three Source/qualifying Rate/Market strategy columns being absent. The Arrival Month condition is represented only by the statistics finding `STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH`; it is not a checklist result or checklist-sourced Failure.

A warning means the check may pass, but a suspicious or noteworthy condition is reported separately as a finding. Corrected Blank and Broken percentages greater than 0% through 50%, inclusive, are Warnings. Other examples include Full Name used instead of separate name columns, more than two monetary columns, multiple Confirmation Number candidate columns, mixed Currency values when a Currency field exists, Stay Value above 10,000 when high values are not expected, unusual Average Rate or Stay Value, raw/DB row-count difference of 10 or more, and an explained rejected database record.

Blank cells do not fail populated-value validity checks. An email address or unrelated populated value in First Name, Last Name, or Full Name is Broken Data; at or below 50% the validity check may remain Pass, while above 50% it must be Fail or readiness reports a contradiction.

Specific catalog distinctions:

- Currency remains optional. There is no required Currency checklist definition.
- Full Name can satisfy required-name field presence while later producing a warning.
- More than two monetary fields is warning-capable and is not an automatic failed checklist definition.
- `Average Rate` does not satisfy the Source, qualifying Rate, or Market field check.

Phase 2 does not implement warning generation.

## Deferred Rules and Services

The following are deliberately deferred:

- QA WinForms UI
- dashboard integration
- dynamic applicability
- validation
- broken-data detection
- percentage calculation
- warning generation
- failure generation
- status calculation
- custom-script evaluation
- PDF generation
- JSON serialization
- storage
- database access
- raw-file parsing
- spreadsheet parsing
- automation
- QA indexing
- runtime sample data
- generated PDFs or JSON

Manual UI and future automated diagnostics should populate `QaReport`, `QaCheckResult`, `QaFinding`, `QaStatistics`, and the stable IDs from `QaChecklistIds`. The future PDF renderer should consume those same models and definitions rather than defining its own representation.

## Phase 2 Acceptance Criteria

- The QA statistics contract represents blank-value counts, denominators, automatic/manual state and percentages, broken-data counts, denominators, automatic/manual state and percentages, and optional broken-data explanations, alongside the other defined statistics groups.
- The QA statistics contract supports reusable Broken Data Statistics for any applicable field, including a stable field identifier, display name, broken-value count, total applicable nonblank values, denominator mode, broken-data percentage, and optional explanation.
