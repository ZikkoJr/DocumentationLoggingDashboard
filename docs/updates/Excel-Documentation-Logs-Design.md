# Excel Documentation Logs Design

## Status and approved baseline

This document is the design contract for moving new Debugging, Script Editing,
and Script Creation entries from daily TXT output to running Excel workbooks
with canonical Hotel and PMS history workbooks.

- Approved baseline commit: `439cd1e725a738f7678351f03ccf4a432800bf50`
- Baseline title: `Complete Quick QA verification and release checks`
- Baseline branch: `update/quick-qa-surface-qa`
- Update branch convention: a fresh `update/*` branch descended from the exact
  approved commit
- Spreadsheet library: ClosedXML `0.105.1`, already referenced by the project
- Baseline Debug build: Pass, 0 warnings, 0 errors
- Post-implementation Debug/Release builds and full regression runs: Pass on
  2026-08-27, with 0 warnings and 0 errors in both configurations
- Windows publish: Pass on 2026-08-27 as Release, `win-x64`, self-contained,
  single-file
- Published executable process smoke: Pass; the exact published executable
  remained running for a bounded five-second hidden launch and was then stopped
  cleanly

The update must not add Excel COM/Interop, Office automation, LibreOffice,
cloud spreadsheet APIs, external spreadsheet executables, or another workbook
package.

## Scope

The existing MainForm log-type selector remains the entry point for:

- Debugging Log (`DEBUG`)
- Script Editing Log (`EDIT`)
- Script Creation Log (`CREATE`)

Every new logical event is written to one selected Running workbook, every
applicable canonical Hotel history workbook, every applicable unique canonical
PMS history workbook, one `LogIndex.txt` line, and the versioned sequence state.
Those writes form one rollback-capable logical transaction.

Historical TXT files remain historical records. They are not migrated,
rewritten, renamed, deleted, or appended by the new workflow. See
`Legacy-TXT-Compatibility.md`.

The update does not add a history browser, workbook row editing/deletion,
workbook rename/delete UI, automatic Hotel creation, Debugging multi-Hotel
selection, Editing/Creation Hotel pickers, database connectivity, raw-file
parsing, or upload/email automation.

## Storage contract

The selected documentation root contains:

```text
<DocumentationRoot>/
├── DebuggingLogs/
│   ├── <historical daily TXT files remain here>
│   └── Running/
├── ScriptEditingLogs/
│   ├── <historical daily TXT files remain here>
│   └── Running/
├── ScriptCreationLogs/
│   ├── <historical daily TXT files remain here>
│   └── Running/
└── Index/
    ├── LogIndex.txt
    ├── documentation-log-settings.json
    └── documentation-log-sequences.json
```

Selected Running workbook paths are direct children of the applicable
`Running` directory. Only a validated `.xlsx` leaf filename is persisted.

History workbooks use the existing QA metadata directories:

```text
<DocumentationRoot>/QAReports/ByHotel/<canonical Hotel folder>/DebuggingLogHistory.xlsx
<DocumentationRoot>/QAReports/ByHotel/<canonical Hotel folder>/ScriptEditingLogHistory.xlsx
<DocumentationRoot>/QAReports/ByHotel/<canonical Hotel folder>/ScriptCreationLogHistory.xlsx

<DocumentationRoot>/QAReports/ByPMS/<canonical PMS folder>/DebuggingLogHistory.xlsx
<DocumentationRoot>/QAReports/ByPMS/<canonical PMS folder>/ScriptEditingLogHistory.xlsx
<DocumentationRoot>/QAReports/ByPMS/<canonical PMS folder>/ScriptCreationLogHistory.xlsx
```

Hotel and PMS directory names come from validated `QaHotelMetadata.FolderName`
and `QaPmsMetadata.FolderName` values resolved through `QaStoragePaths`.
MainForm must not derive a folder name from display text.

## Focused architecture

The documentation-log implementation is separate from `QuickQaReport` and the
Detailed QA report model. Its focused responsibilities are:

- `DocumentationLogEvent`: one immutable event reused for every destination.
- `DocumentationLogHotel` and `DocumentationLogPms`: canonical resolved routing
  records, including their persisted folder names.
- `DocumentationLogSaveRequest`: selected log type, selected Running workbook
  leaf filename, business field values, and either the Debugging metadata
  selection or the Editing/Creation raw Hotel-ID input.
- `DocumentationLogResolvedDraft`: fully validated fields, ordered canonical
  Hotels, and unique canonical PMS systems before an ID is consumed.
- `DocumentationLogSaveResult`: the committed event, Running destination,
  Hotel/PMS destination lists, index/state paths, and any cleanup warning.
- `DocumentationLogStoragePaths`: strict root, Running, state, index, and
  canonical history path resolution.
- `DocumentationLogWorkbookSchema`: exact columns, titles, table contract, and
  schema metadata for each log type/scope.
- `DocumentationLogWorkbookService`: create, discover, validate, stage, append,
  serialize, and reopen/verify workbooks.
- `DocumentationLogWorkbookFilenameService`: strict safe-leaf validation and
  create-new path resolution.
- `DocumentationLogWorkbookPreferencesService`: three independent, root-scoped
  last-used Running filenames with atomic persistence.
- `HotelIdListParser`: text-only tokenization, normalization, and stable
  case-insensitive deduplication.
- `DocumentationLogRoutingService`: fresh metadata resolution and canonical
  Hotel/PMS destination calculation.
- `DocumentationLogSequenceService`: preview and transactional sequence-state
  preparation after reconciling all transition sources.
- `LogIndexService`: legacy-compatible line formatting plus staged index-content
  preparation; it no longer independently appends after a workbook save.
- `DocumentationLogSaveService`: application synchronization, staging,
  verification, source rechecking, commit, rollback, and focused results/errors.

The contracts use `LogType` so the existing selector, display names, prefixes,
and legacy folder mapping remain centralized without coupling documentation
logs to a QA report model.

## Immutable logical event

Validation and canonical routing complete before the save transaction consumes
an ID. Within the synchronized save operation, the service:

1. determines the actual next ID;
2. captures the timestamp once;
3. creates one immutable `DocumentationLogEvent`;
4. serializes that same instance to every workbook and the index.

The copies therefore have exactly the same Log ID, timestamp, normalized Hotel
IDs, canonical Hotel/PMS context, business content, Created By, and Notes /
Follow-up. Routing copies do not consume additional IDs. Blank optional values
are represented as `N/A` in workbook rows and the index-facing summary.

## Excel workbook contract

Every newly created documentation-log workbook has:

- row 1: the human-readable title;
- row 2: blank;
- row 3: exact table headers;
- row 4 onward: data rows;
- one real Excel Table over the header/data range, with filters enabled;
- rows frozen through the row-3 header;
- wrapped long-text cells and restrained, readable widths;
- Date/Time stored as a real Excel date/time;
- Log ID and Hotel ID/Hotel IDs stored explicitly as text;
- no merged cells inside the data table; and
- no `Log Type` data column.

A hidden `__DLD_Metadata` worksheet stores at least:

```text
schemaName = DocumentationLoggingDashboard.LogWorkbook
schemaVersion = 1
logType = DebuggingLog | ScriptEditingLog | ScriptCreationLog
scopeType = Running | Hotel | PMS
scopeId = <blank for Running, canonical Hotel ID, or canonical PMS name>
```

Before every append, the service validates the schema marker and version, log
type, scope type and ID, expected worksheet/table structure, and exact ordered
headers. A spreadsheet with matching visible headers but no valid metadata
marker is incompatible. An existing corrupt, locked, wrong-type, wrong-scope,
unsupported, or structurally changed workbook is rejected without repair,
replacement, or alteration.

### Debugging schema

```text
Date/Time
Log ID
Hotel Name
Hotel ID
PMS
Error Shown On Ticket
Root Cause
Fix Applied
Created By
Notes / Follow-up
```

Required business fields are Error Shown On Ticket, Root Cause, and Fix Applied.
The Hotel Name, Hotel ID, and PMS values come only from the selected canonical
Hotel metadata.

### Script Editing schema

```text
Date/Time
Log ID
Hotel IDs
Script Name
Reason For Edit
Changes Made
Created By
Notes / Follow-up
```

### Script Creation schema

```text
Date/Time
Log ID
Hotel IDs
Script Name
Reason For Creation
Script Purpose / What It Does
Created By
Notes / Follow-up
```

Editing and Creation history workbooks use the same business schema as their
Running workbook. Each history row retains the full original normalized Hotel
ID list; the workbook title and metadata identify its Hotel or PMS scope.

## Titles

Running titles are:

- `Debugging Log — Running Log`
- `Script Editing Log — Running Log`
- `Script Creation Log — Running Log`

Hotel history titles are `<Log Display Name> — Hotel: <Hotel Name> / <Hotel ID>`.
PMS history titles are `<Log Display Name> — PMS: <PMS Name>`.

## Canonical Hotel selection and resolution

Debugging reuses the QA Hotel selection infrastructure. Search text filters by
Hotel ID or Hotel Name but is never treated as report data. A real selected
metadata record drives read-only canonical Hotel Name, Hotel ID, and PMS
context; PMS is not manually editable.

At Submit, metadata is reread. The selected Hotel ID must still exist, and the
fresh Hotel Name, PMS relationship, Hotel folder, and PMS folder must agree
with the selection snapshot. Removal or routing change blocks the save and
requires refresh/reselection.

Editing and Creation retain a manual `Hotel ID(s)` text box rather than a
picker. The shared parser accepts commas, semicolons, and CR/LF boundaries. It:

- trims whitespace and removes empty tokens;
- always treats IDs as strings and preserves leading zeroes;
- deduplicates case-insensitively while preserving first-entered order; and
- blocks the full event if any ID cannot be resolved from current metadata.

The canonical workbook value is `ID1; ID2; ID3`. Unknown IDs are reported
concisely and are never silently discarded.

## Routing contract

Debugging writes exactly once to:

1. the selected Debugging Running workbook;
2. the selected Hotel's Debugging history; and
3. the selected Hotel's canonical PMS Debugging history.

Editing and Creation write exactly once to:

1. the selected type's Running workbook;
2. every unique resolved Hotel history; and
3. every unique resolved canonical PMS history.

For Hotels `1953 → Mews`, `2093 → Mews`, and `3001 → Opera`, one event produces
one Running row, three Hotel rows, one Mews row, and one Opera row. PMS identity
comes from canonical metadata, not free text.

## Running workbook UX and preferences

MainForm contains one reusable active Running-workbook selector. Switching the
log type repopulates it with compatible `.xlsx` files from that type's
`Running` directory. The selector never exposes an arbitrary output directory.

Create New Log File accepts a filename only. `.xlsx` is appended if omitted.
Rooted paths, path components/traversal, unsupported extensions, invalid or
reserved Windows names, excessive length, and existing file/directory
collisions are rejected. Creation uses create-new semantics, verifies the new
workbook contract, and immediately selects the new leaf filename.

`Index/documentation-log-settings.json` is versioned and stores independent
nullable last-used leaf filenames for Debugging, Editing, and Creation. It is
separate from both application `user-settings.json` and Quick QA's settings.
Writes are atomic. A missing, renamed, corrupt, locked, or incompatible
remembered workbook produces a nonfatal status and leaves the operator able to
select or create another. Clearing form fields does not clear a preference.

## Log IDs and durable sequence state

Visible IDs remain:

- `DEBUG-yyyyMMdd-###`
- `EDIT-yyyyMMdd-###`
- `CREATE-yyyyMMdd-###`

`Index/documentation-log-sequences.json` is a versioned, safely parsed internal
state document keyed by log type and date. For the requested type/date, the
next candidate is one above the highest matching sequence observed in:

1. dedicated sequence state;
2. that type's current-date historical daily TXT, if present; and
3. existing `LogIndex.txt` lines.

This preserves a legacy TXT event whose old index append failed while removing
dependence on the selected Excel workbook. Changing or creating a Running
workbook cannot reset IDs. Hotel/PMS copies do not increment the sequence.

Preview is read-only and does not reserve state. Submit recomputes the actual ID
inside application-level synchronization and includes the proposed state bytes
in the transaction. Source fingerprints are rechecked before commit so
overlapping submissions cannot silently commit the same ID.

## LogIndex contract

Historical lines in `<DocumentationRoot>/Index/LogIndex.txt` remain unchanged.
Every new logical event adds exactly one line, regardless of destination count.
The broad seven-part format remains:

```text
Date | Time | Log ID | Log Type | Primary Item | Secondary Item | Saved File
```

The Saved File points to the selected Running `.xlsx`. Debugging retains its
Hotel and PMS summary. Editing and Creation use `Hotel IDs: <canonical list>`.
The index update is staged with the workbooks and sequence state; it is not an
independent post-save append.

## Transaction and rollback

One Submit follows this boundary:

1. validate UI and business fields;
2. normalize and freshly resolve metadata;
3. deduplicate canonical PMS destinations;
4. validate the selected Running workbook;
5. enter save/reentrancy synchronization;
6. recompute the next ID;
7. capture one timestamp and construct one event;
8. resolve all destinations;
9. load existing or create missing history documents in staging;
10. apply one row per destination;
11. prepare updated index and sequence state;
12. serialize all artifacts to unique staging files;
13. reopen staged workbooks and verify the exact append;
14. recheck every existing source fingerprint and every new-file absence;
15. create backups for existing destinations;
16. commit staged artifacts;
17. on ordinary failure, restore replacements and remove newly committed files
    in reverse order;
18. clean staging/backups where possible; and
19. report success only after the complete commit.

Missing deterministic history workbooks are created in staging and receive the
event before commit. Existing history is never reset. If any existing history
is incompatible, corrupt, wrong-scope, wrong-type, changed concurrently, or
locked, the whole event fails.

Focused save errors distinguish the selected Running workbook, Hotel history,
PMS history, index, sequence state, source conflict, commit failure, and
rollback/manual-review cases. MainForm disables Submit and uses an
`isSavingDocumentationLog` guard for the entire attempt. An intentional later
identical event remains valid; there is no permanent content deduplication.

## Known crash boundary

The save service provides rollback for ordinary exceptions observed by the
running process, but Windows does not provide one atomic replacement operation
across all of these independent files. Process termination, power loss, storage
disconnect, or an operating-system crash can interrupt the commit between file
replacements before managed rollback runs.

Backups, staging paths, sequence/index reconciliation, and explicit
manual-review reporting reduce this risk but cannot prove cross-file atomicity
after an abrupt process death. A rollback can also fail because a destination
becomes unavailable while restoration is in progress. In either case the app
must not claim success; it must identify the paths requiring manual review and
must not silently delete evidence needed for recovery.

## MainForm behavior

The existing log-type selector and dynamic, scrollable field area remain.
MainForm adds one reusable Running-workbook row and retargets `Open Today's Log
File` to `Open Selected Log Workbook`.

- Debugging shows the searchable Hotel selector, read-only canonical context,
  its three required business fields, and the two optional common fields.
- Editing shows manual Hotel ID(s), Script Name, Reason For Edit, Changes Made,
  and the common fields.
- Creation shows manual Hotel ID(s), Script Name, Reason For Creation, Script
  Purpose / What It Does, and the common fields.

Switching types disposes/clears inappropriate dynamic state and preview data,
then loads that type's workbook preference. It does not leak Hotel selections,
Hotel-ID input, or business values between types. It also does not overwrite a
different type's preference. Changing the documentation root refreshes all
root-bound metadata, paths, and workbook choices.

The existing privacy reminder remains. Operational free text is allowed, but
guest names/emails, payment data, credentials, and copied raw hotel/guest data
must not be added.

## Quick QA and Detailed QA isolation

Documentation logs may reuse generic path safety, atomic/staged file patterns,
fingerprints, and canonical metadata services. They do not reuse or mutate
`QuickQaReport`, Quick QA's Surface/Hotel workbook schemas, QA findings/status,
or Quick QA settings.

Quick QA remains one Surface append plus one Hotel `QuickQAHistory.xlsx` append
with its existing transaction and formatting. It gains no PMS history.

Detailed QA remains paired Hotel/PMS PDF output with its own index, File ID,
warnings, Source/Rate/Market strategy, findings, and statistics. Documentation
Log IDs and `LogIndex.txt` remain separate from Detailed QA File IDs and
`QAReportIndex.txt`.

Any generic low-level helper extraction must retain regression evidence that
both approved QA workflows are unchanged.

## Verification evidence

Verification completed on 2026-08-27:

- Baseline Debug build: Pass, 0 warnings, 0 errors.
- Post-change Debug build: Pass, 0 warnings, 0 errors.
- Full Debug regression harness: Pass. This included the existing geometry,
  Detailed QA PDF/index, Quick QA workbook/transaction/form, Arrival Month,
  deferred-text, and updated V1 compatibility coverage, plus all five focused
  documentation-log suites.
- Focused documentation-log coverage: Pass, 33 named cases: Hotel parsing and
  routing (5), workbook/schema/preferences (4), sequence/index (6), transaction
  and rollback (14), and MainForm contract/state (4).
- Post-change Release build: Pass, 0 warnings, 0 errors.
- Full Release regression harness: Pass with the same suite coverage.
- `publish-windows.ps1`: Pass. The command published Release `win-x64` with
  `--self-contained true` and `PublishSingleFile=true`; the expected executable
  was created under `PublishedApp/win-x64`.
- Published process smoke: Pass. A bounded hidden launch of the exact published
  executable remained running for five seconds and was stopped cleanly.

The environment did not provide a safe noninteractive way to drive WinForms
dialogs or the operating-system shell associations. A visual GUI walkthrough,
the three external Open actions, and an isolated create/append flow performed
through the published GUI remain Manual Required. Workbook structure, typed
values, create/append behavior, routing, and persistence were instead exercised
against isolated synthetic roots through the Debug and Release harnesses. A
dedicated successful-save cleanup-warning injection was not run; ordinary
successful and failed transactions were verified to leave no normal staging or
backup residue, and forced rollback failure/manual-review behavior passed. See
`Excel-Documentation-Logs-Test-Matrix.md`.
