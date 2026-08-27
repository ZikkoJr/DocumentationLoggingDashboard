# Legacy TXT Compatibility

## Purpose

This update creates a mixed-history system:

- historical Debugging, Script Editing, and Script Creation events remain in
  their original daily TXT files;
- all new events for those three types use Excel Running and canonical
  Hotel/PMS history workbooks; and
- both generations continue to be discoverable through the existing
  documentation `LogIndex.txt`.

The boundary is deliberate. It avoids a risky migration while preserving the
operational value and byte-level integrity of existing records.

## Historical TXT files are immutable

Existing files directly under these folders remain valid historical records:

```text
<DocumentationRoot>/DebuggingLogs/yyyy-MM-dd_DebuggingLog.txt
<DocumentationRoot>/ScriptEditingLogs/yyyy-MM-dd_ScriptEditingLog.txt
<DocumentationRoot>/ScriptCreationLogs/yyyy-MM-dd_ScriptCreationLog.txt
```

The Excel update must not:

- migrate their entries into Excel;
- append new events to them;
- rewrite line endings, encoding, whitespace, or content;
- rename, move, truncate, or delete them; or
- infer and backfill Hotel/PMS history from their free text.

Tests must compare representative historical TXT bytes before and after new
Excel saves, not merely compare parsed text.

## New output boundary

New submissions do not invoke the legacy `LogFileService.SaveEntry` append
path. A new event writes to:

- one selected `.xlsx` file beneath the applicable legacy folder's `Running`
  child;
- every applicable canonical Hotel history workbook;
- every applicable unique canonical PMS history workbook;
- one new line in `<DocumentationRoot>/Index/LogIndex.txt`; and
- versioned sequence state.

The legacy folders themselves are preserved so old TXT paths stored in the
index remain valid. `Running` is added beneath each folder; old files are not
moved into it.

## Legacy Log IDs remain authoritative transition evidence

The visible prefixes and date-based IDs do not change:

- `DEBUG-yyyyMMdd-###`
- `EDIT-yyyyMMdd-###`
- `CREATE-yyyyMMdd-###`

For a new event, sequence reconciliation examines the highest matching sequence
from three sources:

1. `Index/documentation-log-sequences.json`;
2. the applicable current-date historical TXT file, if it exists; and
3. existing `Index/LogIndex.txt` entries.

The current-date TXT scan remains necessary because the old workflow could
append a TXT entry and then fail its independent index append. The index scan
remains necessary because it contains old and new indexed history. Dedicated
state makes the sequence independent of whichever Running workbook is selected.

No old ID is renumbered. No sequence is consumed for Hotel or PMS copies.
Preview does not reserve an ID.

## LogIndex coexistence

`<DocumentationRoot>/Index/LogIndex.txt` is retained in place. Existing lines
are not migrated or normalized. Their saved-file fields can continue to point
to daily TXT files.

Each new Excel event adds one pipe-separated line in the existing broad format:

```text
Date | Time | Log ID | Log Type | Primary Item | Secondary Item | Saved File
```

The new Saved File points to the selected Running `.xlsx`, never to a Hotel or
PMS history copy. Editing and Creation may use the clearer summary `Hotel IDs:
<canonical list>`. Old `Hotel Applied To` and `Hotels That Use This Script`
wording is left untouched in historical lines.

Appending a new line must preserve all existing bytes as the prefix of the
updated index content. If a nonempty historical file lacks a final newline, the
staged update may append the necessary line separator before the new record; it
must not otherwise rewrite old lines.

## Folder and operator behavior

`Open Logs Folder` continues to open the selected documentation root, where old
TXT files remain accessible. The obsolete `Open Today's Log File` action is
retargeted to `Open Selected Log Workbook` for new work. Historical TXT files
remain available through the folder and any existing index paths.

Changing the configured documentation root affects future saves only. Neither
the legacy nor Excel workflow moves records from an earlier root.

Operators should treat the history as chronological generations:

- older entries: daily TXT plus their original index lines;
- newer entries: selected Running workbook plus Hotel/PMS history and one new
  index line.

The application does not provide a combined history browser or automatic
cross-format search in this update.

## Transaction implications

Legacy TXT is read-only input to sequence reconciliation and is not part of a
new save's writable destination set. Running/history workbooks, the index, and
sequence state are staged and committed together with rollback on ordinary
failure.

The old workflow's possible TXT-without-index partial state is not repaired.
The sequence scan merely ensures a later Excel event does not reuse that ID.

As with any multi-file transaction, an abrupt process or operating-system crash
can occur between replacements before managed rollback runs. The app must not
silently modify historical TXT in an attempt to repair such a state. It reports
manual-review paths and preserves useful staging/backup evidence when cleanup
or rollback cannot be completed safely.

## QA isolation

Historical documentation TXT, documentation Running/history workbooks, and
`Index/LogIndex.txt` are separate from:

- Quick QA Surface and `QuickQAHistory.xlsx` workbooks/settings; and
- Detailed QA PDFs and `QAReports/Index/QAReportIndex.txt`.

Neither QA workflow participates in documentation-log ID sequencing. New
documentation events do not modify QA workbook schemas, Detailed QA PDFs,
findings/status logic, File IDs, or QA indexes.

## Verification requirements

A mixed-history regression must create synthetic legacy TXT and index content,
record their bytes, perform a new Excel save, and confirm:

- every historical TXT byte is unchanged;
- every old index line is unchanged;
- the new event uses `.xlsx` destinations only;
- the new index points to the selected Running workbook;
- the next ID is above applicable legacy TXT/index IDs;
- there is exactly one new index line; and
- no migration, TXT append, or QA-index contamination occurs.

These checks remain `Not Run` until recorded in
`Excel-Documentation-Logs-Test-Matrix.md` with actual evidence.
