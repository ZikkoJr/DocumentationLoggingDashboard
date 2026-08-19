# Quick QA / Surface QA Design

## Purpose and authority

This document defines the first major post-pilot update to
`DocumentationLoggingDashboard`. It is the current design contract for the new
Quick QA workflow and its interaction with the existing Detailed QA workflow.
The phase and pilot documents under `docs/v2` remain historical records; this
document supersedes them only where it explicitly changes current product
behavior.

The update is additive. It must not merge Quick QA into the existing full report
model, change the three V1 logging workflows, merge into `main`, migrate old
reports, or rewrite historical evidence.

## Two explicit QA actions

The dashboard exposes two direct actions, with no report-type wizard:

| Action | Form | Responsibility | Output |
| --- | --- | --- | --- |
| `Detailed QA Report` | Existing `QaReportForm` | Full manual checklist, File Characteristics, Statistics, findings, readiness, and PDF report | Paired Hotel/PMS PDFs plus `QAReportIndex.txt` |
| `Quick QA` | Dedicated `QuickQaForm` | Fixed manual surface-level Raw/Database checklist | One appended client Surface row plus one appended Hotel history row |

Both actions reuse the existing QA root, settings, storage initialization, and
canonical Hotel/PMS metadata services. They must not register duplicate event
handlers. All V1 buttons and storage behavior remain unchanged.

## Workflow boundaries

Quick QA has its own model, definitions, form, validation, summary, workbook,
preference, finding-synchronization, and save services. It may naturally reuse
`QaHotelInformation`, `QaFileMonth`, `QaFinding`, `QaFindingSeverity`,
`QaFindingResolution`, and `QaReportStatus`, but it does not inherit from or
fabricate a Detailed `QaReport`.

Quick QA contains canonical Hotel information, File Month, a string File ID,
the fixed Quick checklist results, `Custom Script Available`, findings, final
status, and Summary. It has no Detailed Statistics, File Characteristics,
user-entered QA Date, Created By, Original Filename, PDF fields, PDF output, PMS
history, or Detailed index entry.

Quick checklist rows use a separate status contract:

- `NotEvaluated`
- `Pass`
- `Warning`
- `Fail`
- `NotApplicable`

Only definitions that explicitly allow N/A may use `NotApplicable`. The three
strategy rows use Available/Unavailable presentation and are aggregated into a
single managed finding.

## Fixed Quick checklist

Quick IDs are independent of Detailed IDs, stable, unique, and catalog ordered.
The catalog contains exactly 21 rows: 14 Raw File rows followed by 7 Database
rows.

| # | Stable ID | Display text | N/A |
| ---: | --- | --- | :---: |
| 1 | `QUICK.RAW.NAMES.AVAILABLE` | Names are available to be pulled correctly | No |
| 2 | `QUICK.RAW.CONFIRMATION_NUMBER.AVAILABLE` | Confirmation Number is available | No |
| 3 | `QUICK.RAW.RESERVATION_DATE.AVAILABLE` | Reservation/Booking Date is available | No |
| 4 | `QUICK.RAW.ARRIVAL_DATE.AVAILABLE` | Arrival Date is available | No |
| 5 | `QUICK.RAW.DEPARTURE_DATE.AVAILABLE` | Departure Date is available | No |
| 6 | `QUICK.RAW.MONETARY.AVAILABLE` | A usable monetary column is available | No |
| 7 | `QUICK.RAW.FILE.FORMAT_CONSISTENT` | File formatting is consistent throughout | No |
| 8 | `QUICK.RAW.DATES.FORMAT_VALID` | Date values use valid and interpretable formats | No |
| 9 | `QUICK.RAW.DATES.SEQUENCE_VALID` | Reservation/Booking, Arrival, and Departure dates are sequenced correctly | No |
| 10 | `QUICK.RAW.CURRENCY.CONSISTENT` | Currency values are consistent when applicable | Yes |
| 11 | `QUICK.RAW.STRATEGY.SOURCE_COLUMN_AVAILABLE` | Source column available | No |
| 12 | `QUICK.RAW.STRATEGY.RATE_COLUMN_AVAILABLE` | Rate column available | No |
| 13 | `QUICK.RAW.STRATEGY.MARKET_COLUMN_AVAILABLE` | Market column available | No |
| 14 | `QUICK.RAW.CONFIRMATION.CANDIDATES_REVIEWED` | Potential Confirmation Number fields were reviewed and the correct source identified | Yes |
| 15 | `QUICK.DB.NAMES.PULLED_CORRECTLY` | Names are being pulled correctly | No |
| 16 | `QUICK.DB.MONETARY.PULLED_CORRECTLY` | Monetary values are being pulled/calculated correctly for the available monetary type | No |
| 17 | `QUICK.DB.DATES.PULLED_CORRECTLY` | Dates are being pulled correctly | No |
| 18 | `QUICK.DB.CONFIRMATION_NUMBER.PULLED_CORRECTLY` | Confirmation Number is being pulled correctly | No |
| 19 | `QUICK.DB.REQUIRED_VALUES.PRESENT` | Required database values are present | No |
| 20 | `QUICK.DB.REJECTED_RECORDS.ACCOUNTED_FOR` | Rejected records are accounted for when applicable | Yes |
| 21 | `QUICK.DB.EMAIL.PULLED_CORRECTLY` | Email is being pulled correctly when an email source exists | Yes |

There is no Quick Email-presence check, Arrival/File Month analysis, separate
First/Last/Full Name branch, Detailed monetary arithmetic/spot-check branch,
Statistics page, File Characteristics page, or hidden statistics.

Names pass when either usable separate name fields or a usable Full Name source
is available. Monetary availability may be satisfied by one or multiple usable
monetary source types; the Database row assesses correct mapping/calculation for
the type that actually exists, without importing Detailed arithmetic UI.

## Strategy aggregation

Source terminology includes `Source` and `Booking Source`. Rate terminology
includes `Rate Code`, `Rate Plan`, and `Rate Plan Code`; `Average Rate` does not
qualify. Market terminology includes `Market` and `Market Segment`.

Quick deterministic IDs are:

- `QUICK:WARN:STRATEGY:SOURCE_RATE_MARKET`
- `QUICK:FAIL:STRATEGY:SOURCE_RATE_MARKET`

Detailed deterministic IDs are:

- `WARN:STRATEGY:SOURCE_RATE_MARKET`
- `FAIL:STRATEGY:SOURCE_RATE_MARKET`

For each workflow, zero unavailable categories creates no strategy finding; one
or two creates exactly one Warning visibly labelled `Strategy Warning`; all
three creates exactly one aggregate Failure. Individual generic findings for
these rows are suppressed. Strategy Warning is not a new severity or report
status.

## Custom-script handling and Result

`Custom Script Available` is a simple Yes/No control. No script name or script
metadata is required. When No, no Warning or Failure may remain resolved as
`HandledByCustomScript`; invalid handled state is cleared or rejected. When
Yes, each finding can be handled independently. There is no global “all
handled” toggle, and handling one finding cannot resolve another.

Original severity is retained. A handled Failure is still a Failure-severity
finding but is not Active. Result is derived from current findings:

- any Active Failure -> `Fail`;
- no Active Failure, but at least one Warning or resolved Failure ->
  `Pass with Warnings`;
- no findings -> `Pass`.

A completed `Fail` is valid historical QA and may be saved. The client and
history Result values are limited to those three strings; there is no Strategy
Warning Result.

## Quick form

`QuickQaForm` is one simple scrollable page. It reuses
`QaHotelSelectorControl`, supports Hotel ID/name search and duplicate-name
disambiguation, selects a real metadata record, and displays canonical Hotel ID
and PMS without arbitrary PMS editing. Report Details also require File Month,
File ID, a direct-child Surface workbook selection, and `Create New Surface QA
File`.

Raw and Database checks appear on the same page under visible section headers.
The page continuously displays Result, the custom-script controls, and a
multiline generated-but-editable Summary. `Save Quick QA` is disabled during a
save and guarded against double-click/reentrant invocation.

File ID is always a trimmed nonblank string. It is never parsed numerically or
used as a path, and leading zeroes are preserved.

## Summary generation and staleness

`QuickQaSummaryService` produces exactly this clean text:

```text
Raw file and Database QA passed successfully
```

For non-clean reports it emits one concise sentence per finding, one sentence
per line. Strategy messages explicitly begin `Strategy Warning`; handled
custom-script context may be appended concisely. Generated text never contains
guest-level examples or copied raw rows.

The report records whether Summary was manually edited and the QA-state
fingerprint from which it was generated. Unedited Summary may regenerate when
QA changes. If an edited Summary becomes stale, the app preserves it, marks it
visibly out of date, and blocks save until the user selects `Regenerate
Summary`. Regeneration creates current text and resets the fingerprint; the new
text may then be edited. Warning/Failure reports require a nonblank current
Summary.

## Storage layout

Quick storage extends the existing `QAReports` tree without touching V1 paths:

```text
<DocumentationRoot>/QAReports/
|-- SurfaceQA/
|   +-- <selected client workbook>.xlsx
|-- ByHotel/<canonical-hotel-folder>/
|   +-- QuickQAHistory.xlsx
+-- Metadata/
    +-- quick-qa-settings.json
```

Client workbooks are never placed under `ByHotel`, `ByPMS`, `Index`, or V1 log
directories. Hotel history uses the canonical directory returned by
`QaStoragePaths.ResolveHotelDirectory(...)`. Quick QA creates no PMS copy, PDF,
or `QAReportIndex.txt` update.

The preference file stores only the selected Surface workbook filename, never
an arbitrary absolute path. It is updated atomically after successful
create/select/save behavior and does not change the V1 `SettingsService` or
`user-settings.json` contract.

At form open, enumerate compatible direct-child `.xlsx` files, ignoring Excel
`~$` locks and internal staging/rollback names. Select a valid remembered file;
otherwise select the most recently modified compatible file only when safe and
unambiguous; otherwise require an explicit selection. Renames/deletions fall
back safely.

## Client Surface workbook contract

The canonical worksheet is `Surface QA`; the canonical Excel table is
`SurfaceQaTable`. Columns are exact and ordered:

| # | Header | Value contract |
| ---: | --- | --- |
| 1 | `File Month` | text `yyyy-MM` |
| 2 | `Hotel Name` | canonical metadata text |
| 3 | `Hotel ID` | text |
| 4 | `PMS` | canonical metadata text |
| 5 | `File ID` | text preserving leading zeroes |
| 6 | `Result` | `Pass`, `Pass with Warnings`, or `Fail` |
| 7 | `Summary` | current final multiline text |

There is no QA Timestamp or separate Raw/Database status column. New workbooks
have the table/filter enabled, row 1 frozen, wrapped Summary cells, compact
readable widths, no merged data cells, and one event per append-only row.

### Historical sample evidence

The supplied `Surface Qa list July.xlsx` was inspected read-only. Its SHA-256 at
inspection was
`7883EE468DA70A88253663CC0030A04B3BEA89D2F883ACA10FE5125C575A45F8`.
It contains one worksheet (`Sheet1`), range `A1:G30`, and 29 existing data rows.
Its raw headers include trailing spaces and normalize to:

```text
Month 2026 | Hotel Name | Hotel Id | PMS | file id | Pass/Fail/Warning | summary
```

The sample has no Excel table, filter, frozen pane, formulas, validation,
conditional formatting, or Summary wrapping. Summary is instead an extremely
wide column. Hotel ID and File ID are numeric cells, so any already-lost leading
zeroes cannot be recovered. Month and Result values vary in capitalization and
surrounding whitespace. The workbook also contains Office custom/coauthor XML
parts. At inspection it had an active `~$Surface Qa list July.xlsx` lock file,
and ordinary exclusive-style reading failed; this is representative evidence
for the locked-workbook path.

### Narrow legacy compatibility

Outer whitespace is trimmed and these aliases are compared case-insensitively:

| Approved legacy header | Canonical header |
| --- | --- |
| `Month 2026` | `File Month` |
| `Hotel Name` | `Hotel Name` |
| `Hotel Id` | `Hotel ID` |
| `PMS` | `PMS` |
| `file id` | `File ID` |
| `Pass/Fail/Warning` | `Result` |
| `summary` | `Summary` |

No arbitrary near-match is accepted. On staged append, canonicalize only those
seven header cells, preserve old row order and values, do not normalize old
Results, add/repair table/filter coverage, freeze row 1, wrap Summary, and append
the canonical row. Missing, wrong, duplicate, ambiguous, reordered, extra, or
corrupt schemas are rejected without rewriting the source. Visible business
data and usable formatting must survive; unsupported Office extension metadata
is not a business-data contract and should be specifically assessed during
ClosedXML round-trip testing.

## Safe new-workbook names

The filename routine allows legitimate spaces, appends `.xlsx` when absent, and
never accepts another extension. It rejects rooted paths, separators, traversal,
`.`/`..`, Windows reserved device names, and unreasonable lengths; sanitizes
invalid Windows filename characters predictably; trims outer whitespace and
invalid trailing dots/spaces; resolves the full path; and verifies the result is
a direct child of `SurfaceQA`.

Creation never overwrites. If the name exists, tell the user and allow selection
of that compatible workbook or a different name. A new file becomes selected
and last-used. No blank template workbook is committed to Git.

## Hotel Quick QA history

`QuickQAHistory.xlsx` uses worksheet `Quick QA History` and table
`QuickQaHistoryTable`, with these exact ordered columns:

1. `QA Timestamp`
2. `File Month`
3. `Hotel Name`
4. `Hotel ID`
5. `PMS`
6. `File ID`
7. `Result`
8. `Summary`

It is created on the first successful save and appended on every later Quick QA.
There is no overwrite/deduplication key: legitimate repeated QA events remain
distinct rows. The automatically captured timestamp is stored as invariant
round-trip ISO-8601 text with offset, equivalent to
`DateTimeOffset.ToString("O", InvariantCulture)`. One injectable clock or
`TimeProvider` supplies the single timestamp used by the whole transaction.
The client workbook never receives it.

## Atomic two-workbook transaction

`QuickQaSaveService` treats the client workbook and Hotel history as one logical
transaction:

1. validate the Quick report;
2. require a current Summary;
3. calculate current findings and final status;
4. capture one QA timestamp;
5. re-resolve canonical Hotel/PMS metadata;
6. resolve the selected direct-child Surface workbook;
7. resolve the canonical Hotel history path;
8. load and validate the client workbook;
9. load and validate history if it exists;
10. construct both complete modified workbooks in memory where possible;
11. serialize both staged payloads;
12. write temporary files in each destination directory;
13. flush staged files;
14. reopen and validate both staged workbooks;
15. verify the exact appended event in each;
16. recheck source fingerprints to detect external changes;
17. move each existing destination to its unique rollback name, acquire an
    exclusive lease on that rollback file, and verify its captured length and
    SHA-256 before either staged file commits;
18. retain those rollback leases through both commits and final verification;
19. commit both staged artifacts;
20. restore the other destination if either commit fails;
21. clean staging/rollback artifacts; and
22. report success only after both final files verify current.

Rollback restores original bytes for an existing file and removes a newly
created history file if the client commit cannot also succeed. A lock or
unavailable workbook produces no success and no ordinary one-sided event. The
message identifies Surface or Hotel history and asks the user to close it,
without exposing a stack trace. Source changes between read and commit abort
rather than overwrite external work. The retained rollback leases also close
the rename-window race: if an external edit lands after the final live-path
fingerprint check, the leased rollback copy no longer matches the captured
baseline, the commit is stopped, and the external version is restored.

The user-configured Documentation Root is the trusted storage boundary. Within
its `QAReports` child, Quick QA rejects every existing reparse-point component
through the selected Surface workbook or canonical Hotel history path. This is
a preflight containment rule; it does not attempt to defend against an actor
with sufficient privileges to replace storage directories atomically after
validation.

Rollback covers managed I/O, validation, concurrency, commit, and final-check
failures. There is no durable transaction journal or startup orphan recovery in
this update, so abrupt process or power termination between the two filesystem
commits remains a manual-recovery boundary. Hidden stage/rollback filenames and
the exception's manual-review locations are designed to make that state
identifiable without treating it as a successful save.

The UI reentrancy guard prevents one invocation from appending twice; it does
not deduplicate legitimate later invocations.

## Save validation and success

Save is blocked for missing/stale canonical Hotel metadata, PMS mismatch,
invalid File Month, blank File ID, unevaluated required rows, unauthorized N/A,
inconsistent strategy state, handled findings while Custom Script Available is
No, status/finding mismatch, blank required Summary, stale Summary, missing or
unavailable Surface selection, an out-of-root workbook, incompatible schema, or
invalid history path.

Warnings, Strategy Warnings, and a final Fail do not themselves block save.
After both workbooks verify, success identifies Hotel, File ID, final Result,
Surface filename, and the Hotel history update. The form is not reset or closed
before that confirmation.

## Privacy

Findings and Summary stay at field/check level. The UI reminder and generated
text must not encourage or include guest names, guest emails, payment data,
credentials, confirmation/reservation-level personally identifying details, or
copied source rows.

## Dependency and deployment decision

The only new spreadsheet library is `ClosedXML` `0.105.1`. It is managed,
supports `.xlsx` creation/editing, tables, filters, wrapping, and frozen panes,
and does not require Excel. ClosedXML is MIT-licensed. The final resolved package
graph and license review are recorded below and in the repository root
`THIRD-PARTY-NOTICES.txt`.

### Resolved net10 graph checkpoint

The 2026-08-18 net10 restore selected this exact graph:

| Package | Version | Role | License |
| --- | --- | --- | --- |
| `ClosedXML` | `0.105.1` | direct | MIT |
| `PDFsharp-MigraDoc-GDI` | `6.2.4` | direct | MIT |
| `ClosedXML.Parser` | `2.0.0` | transitive | MIT |
| `DocumentFormat.OpenXml` | `3.1.1` | transitive | MIT |
| `DocumentFormat.OpenXml.Framework` | `3.1.1` | transitive | MIT |
| `ExcelNumberFormat` | `1.1.0` | transitive | MIT |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `8.0.2` | transitive | MIT |
| `Microsoft.Extensions.Logging.Abstractions` | `8.0.3` | transitive | MIT |
| `PDFsharp-GDI` | `6.2.4` | transitive | MIT |
| `RBush.Signed` | `4.0.0` | transitive | MIT |
| `SixLabors.Fonts` | `1.0.0` | transitive | Apache-2.0 |

`System.IO.Packaging` appeared in the declared-range audit but was not selected
for this net10 graph. `THIRD-PARTY-NOTICES.txt` records the corresponding
permissions and attributions. Release verification must confirm this graph has
not drifted, run the transitive vulnerability check, and confirm the notice file
is copied beside the published single-file executable.

Excel Interop was rejected because it requires Excel/COM and creates deployment,
locking, and automation risks. EPPlus was rejected because its licensing model
does not match the selected permissive dependency decision. Direct low-level
Open XML manipulation was rejected because it would add substantial schema,
table, style, relationship, and preservation complexity; Open XML remains an
implementation dependency beneath ClosedXML. LibreOffice automation, browser
automation, cloud spreadsheet APIs, external executables, and runtime package
installation are prohibited.

The final Windows x64 publish must prove self-contained, single-file operation,
create/append/history behavior, Detailed PDF regression, no Excel requirement,
no unexpected native spreadsheet runtime, and inclusion of required notices.

## Historical compatibility and deferred work

Historical Detailed schema-1 PDFs and index entries remain untouched. Quick QA
does not migrate them. V1 storage/settings remain unchanged. Routing Debugging
Logs into Hotel/PMS folders remains deferred and is not part of this update.
