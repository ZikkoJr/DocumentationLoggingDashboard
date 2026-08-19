# Detailed QA Check Changes

## Scope

The existing full QA workflow is now named `Detailed QA Report`. It continues
to own File Characteristics, Statistics, findings/resolutions, readiness, PDF
generation, paired Hotel/PMS storage, overwrite handling, and the Detailed QA
index. This update changes only the approved report identity fields and active
check semantics below.

The active catalog for newly created Detailed reports contains 29 visible
definitions: 22 Raw File and 7 Database. The catalog, not a form-level magic
number, is authoritative.

Historical schema-1 PDFs and index entries are immutable. Retired IDs remain
valid when reading historical artifacts but are not emitted in new reports.

## Mandatory File ID and schema 2

Every newly created Detailed report requires a top-level string `FileId`.

- Trim surrounding whitespace.
- Reject blank or whitespace-only values.
- Never parse the value numerically.
- Preserve leading zeroes, such as `001234`.
- Never use it as a path.
- Do not add it to the PDF filename.
- Do not add it to `QaReportKey`.
- Do not change Hotel ID + File Month overwrite identity.

File ID appears in Report Details, readiness/fingerprint state, the PDF
report-information/header area, newly serialized index entries, and relevant
successful-save/details UI. A File ID edit invalidates stale readiness evidence.
Old PDFs are not regenerated.

The rendered Detailed report schema increments from 1 to 2 because File ID and
the active checklist semantics are part of the report contract. Historical
artifacts remain schema 1 and are not migrated.

## Backward-compatible Detailed index

The historical index uses `---`-delimited entries with a fixed 13-line schema.
The new parser must parse one delimited entry at a time by known labels rather
than applying one global line count.

Required compatibility:

- historical 13-line entries without `File ID:` load unchanged;
- schema-2 entries include one `File ID:` field;
- old-only, new-only, and mixed files load;
- File ID is nullable/optional only for historical index models;
- a historical entry remains File-ID-less unless its existing Hotel ID + File
  Month report key is actually replaced by a newly saved report;
- no bulk migration or index rewrite runs;
- no historical PDF is touched;
- all historically required fields remain required;
- duplicate known labels, unknown/unexpected duplicate fields, missing required
  fields, malformed values, and invalid delimiters fail safely.

Serializer output remains single-line/whitespace-safe and deterministic. File ID
is display/index data, not a filesystem component or identity key.

## Retired and added IDs

| Area | Historical active ID | New active ID(s) | New behavior |
| --- | --- | --- | --- |
| Raw Email | `RAW.REQUIRED.EMAIL_PRESENT` | `RAW.EMAIL.COLUMN_AVAILABLE` | Missing Email is exactly one Warning, never Failure |
| Raw strategy | `RAW.SOURCE.COLUMN_PRESENT` | `RAW.STRATEGY.SOURCE_COLUMN_AVAILABLE`, `RAW.STRATEGY.RATE_COLUMN_AVAILABLE`, `RAW.STRATEGY.MARKET_COLUMN_AVAILABLE` | Three visible availability rows, centrally aggregated |
| Arrival/File Month | `RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH` | None in checklist | Retired from checklist; statistics owns the threshold |

Do not repurpose the old IDs. Their historical meaning remains readable.

## Email column availability

The new display text is `Email column available`.

| Selection | Stored check state | Generated finding | Report effect |
| --- | --- | --- | --- |
| Available | Pass | None | No effect |
| Unavailable | Equivalent failed availability state | Exactly one Warning | Alone, `Pass with Warnings` |

Missing Email must not create an individual generic Failure or a duplicate
managed finding. The existing Database Email mapping check retains its existing
applicability/behavior. Historical `RAW.REQUIRED.EMAIL_PRESENT` results remain
valid only in old artifacts.

## Source, Rate, and Market availability

The three new visible labels are:

- `Source column available`
- `Rate column available`
- `Market column available`

Accepted manual terminology is deliberately narrow:

- Source: `Source`, `Booking Source`
- Rate: `Rate Code`, `Rate Plan`, `Rate Plan Code`
- Market: `Market`, `Market Segment`

`Average Rate` is not a strategy/code Rate column. These are manual evaluations;
the app does not parse raw files. Available/Unavailable presentation may map to
the existing Detailed Pass/Fail state; the global `QaCheckStatus` enum does not
change.

Generic individual Warning/Failure findings from these rows are suppressed.
The centralized managed IDs are:

- `WARN:STRATEGY:SOURCE_RATE_MARKET`
- `FAIL:STRATEGY:SOURCE_RATE_MARKET`

All eight combinations are deterministic:

| Source | Rate | Market | Expected managed result |
| :---: | :---: | :---: | --- |
| Available | Available | Available | No strategy finding |
| Unavailable | Available | Available | One `Strategy Warning` naming Source |
| Available | Unavailable | Available | One `Strategy Warning` naming Rate |
| Available | Available | Unavailable | One `Strategy Warning` naming Market |
| Unavailable | Unavailable | Available | One `Strategy Warning` naming Source and Rate |
| Unavailable | Available | Unavailable | One `Strategy Warning` naming Source and Market |
| Available | Unavailable | Unavailable | One `Strategy Warning` naming Rate and Market |
| Unavailable | Unavailable | Unavailable | One aggregate Failure; no Strategy Warning |

One or two unavailable categories create exactly one Warning whose visible
title/summary begins `Strategy Warning`; there is no ordinary duplicate Warning
and no individual Failure. Three unavailable categories create exactly one
aggregate Failure, not three. Examples:

```text
Strategy Warning: Rate column is not available.
Strategy Warning: Source and Market columns are not available.
Source, Rate, and Market strategy columns are all unavailable.
```

Strategy Warning is neither a new severity nor a new `QaReportStatus`. Overall
statuses remain `Pass`, `Pass with Warnings`, and `Fail`.

## Arrival/File Month statistics correction

`RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH` is absent from newly created checklists,
forms, and PDFs. Arrival/File Month remains visible only through Detailed File
Month Statistics.

The current requested managed finding ID is:

```text
STAT:FAIL:ARRIVAL_OUTSIDE_FILE_MONTH
```

The rule is based on valid applicable Arrival counts, not rounded display text:

- no valid applicable Arrival dates: preserve existing applicability; do not
  manufacture a threshold Failure;
- outside percentage `<= 30%`: no finding from this condition;
- outside percentage `> 30%`: exactly one statistics-sourced Failure;
- no Warning band;
- no duplicate checklist Failure.

Exactly `3/10` is not above the threshold; `4/13` is above it. Quick QA has no
Arrival/File Month statistic or check.

Earlier dirty pilot-correction material used
`FAIL:STAT:ARRIVAL_OUTSIDE_FILE_MONTH`. That spelling is superseded for this
post-pilot implementation by the current requested ID above; source, tests, and
current documentation must use one consistent final value. Historical artifacts
that already contain an older managed ID are not rewritten.

## Detailed readiness, PDF, save, and index effects

Readiness must reject blank File ID, retired/unknown active results, stale
fingerprints, malformed statistics, and existing contradictions. File ID and
the new checklist state participate in the readiness fingerprint. The schema-2
PDF displays File ID and the active 29-definition checklist without an empty
retired Arrival row.

PDF filename and logical overwrite identity remain unchanged. Paired Hotel/PMS
PDFs still derive from the same byte payload, and the index still records one
logical entry. Successful replacement updates the matching logical entry with
the new File ID; unrelated historical entries remain untouched.

## Regression boundary

The update must not redesign or regress:

- File Characteristics or Statistics layout;
- Total Data Rows propagation;
- Blank denominators and Broken nonblank denominators;
- manual denominator overrides and Auto reset;
- independent Blank/Broken Warning and Failure thresholds;
- blank values not being counted as Broken values;
- blank names not failing populated-name checks;
- finding resolution and Detailed custom-script behavior;
- Detailed PDF content beyond approved File ID/check changes;
- paired Hotel/PMS storage, rollback, and overwrite behavior;
- old report filenames or Hotel ID + File Month identity;
- V1 storage, IDs, indexes, settings, or actions.

## Privacy and historical compatibility

Detailed finding descriptions remain at field/check level. They must not
auto-generate guest names, emails, payment information, credentials,
reservation-level PII, or copied raw rows.

No historical PDF or index migration is performed. Old IDs remain readable in
old artifacts. The deferred Debugging Log Hotel/PMS-folder enhancement remains
out of scope.

Verification requirements are tracked in
`docs/updates/Quick-QA-Test-Matrix.md`.
