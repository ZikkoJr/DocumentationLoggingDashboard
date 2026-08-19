# Phase 9 Paired PDF Saving, Overwrite Protection, and QA Report Index

## Purpose and boundaries

Phase 9 connects the approved in-memory Phase 8 PDF renderer to the V2 QA Report form and saves one generated payload to the two canonical QA storage locations. It also maintains the dedicated `QAReportIndex.txt` summary, detects prior reports by logical report key, requires explicit overwrite confirmation, and treats the two PDFs plus the index as one logical transaction with rollback.

The phase does not add a caller-selected path, `SaveFileDialog`, V1 destination, third PDF copy, history browser, repair screen, draft workflow, database, upload, email, spreadsheet export, or background save. The save service accepts only configured `QaStoragePaths`; callers cannot redirect an artifact outside the approved Hotel, PMS, and Index roots.

This document records the Phase 9 implementation and its focused existing-report detection correction. The original Phase 9 commit was pushed for review, but Phase 9 is not approved, merged, tagged, or released.

> **Post-Phase-10 controlled-pilot correction boundary:** The P1 Blank/Broken Statistics correction documented in `Pilot-Correction-Blank-Broken-Statistics.md` does not change Phase 9's paired-save transaction, filename, storage-path, overwrite, or index-format contracts. It does change the validation result, readiness fingerprint, calculated status, PDF content, and index status that Phase 9 consumes for affected reports. The final isolated save/PDF/index suite passed 7/7 in Debug and Release with byte-identical Hotel/PMS pairs, seven expected index statuses, and no transaction leftovers. That current evidence is recorded separately in the correction record; the historical transaction results below remain evidence only for their original save-mechanism scope.

> **Deferred-text controlled-pilot boundary:** The final P2 correction commits pending text before the readiness/save transaction begins. Five additional synthetic scenarios passed with final text in PDFs, byte-identical Hotel/PMS pairs, correct index statuses, and no transaction artifacts. The final matrix also passed blocked-output guards and logical-report overwrite replacement. The transaction and index formats are unchanged. See `Pilot-Correction-Deferred-Text-Commit.md` and `V2-Production-Readiness.md`.

> **Arrival Month controlled-pilot boundary:** The focused correction does not change the paired-save transaction, logical key, filename, overwrite, or QA index format. It changes the status and PDF content consumed by Phase 9: 30% or less outside creates no Arrival/File Month finding, above 30% with an Active statistics Failure is Fail, and a handled Failure follows the existing Pass with Warnings rule when no other Active Failure remains. Corrected paired-byte and index evidence is pending in `Pilot-Correction-Arrival-Month-Threshold.md`.

## Approved baseline

- Required and active branch: `v2-qa-reports`.
- Approved Phase 8 baseline: `e92d2f72a5015895e801b2f5af8b230a6c132bfd`.
- Pushed Phase 9 commit at the start of the detection correction: `42a5b061ae808253dea21affd1b1bdcb6e066b57`.
- Earlier Phase 8 implementation/follow-up baseline in the recorded ancestry: `59570184dbb5204d9c0917b7a0e5e8e5be087027`.
- Untouched baseline command: `dotnet build DocumentationLoggingDashboard.sln`.
- Detection-correction pre-edit build result: exit code 0, build succeeded, 0 warnings, 0 errors, in 3.83 seconds.

Phase 9 does not change the target framework, startup model, publish script, or direct package set. The existing `PDFsharp-MigraDoc-GDI` 6.2.4 dependency remains the renderer dependency introduced in Phase 8.

## Files added and modified

Added models:

- `DocumentationLoggingDashboard/QAReports/Models/QaReportExistingFiles.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportFilenameParts.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportIndexEntry.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportKey.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportSavePreparation.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportSaveRequest.cs`
- `DocumentationLoggingDashboard/QAReports/Models/QaReportSaveResult.cs`

Added services and focused exceptions:

- `DocumentationLoggingDashboard/QAReports/Services/QaReportExistingFileService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportFilenameService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportFilenameParser.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportIndexException.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportIndexService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportSaveException.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportSaveService.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaReportTransactionFileOperations.cs`

Modified integration files:

- `DocumentationLoggingDashboard/MainForm.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.cs`
- `DocumentationLoggingDashboard/QAReports/Forms/QaReportForm.Designer.cs`
- `DocumentationLoggingDashboard/QAReports/Services/QaFolderNameSanitizer.cs`

Added documentation:

- `docs/v2/Phase-9-Paired-Saving-And-Index.md`

No project file or package reference is changed by Phase 9.

## Public contracts and service APIs

The primary preparation/commit API is:

```csharp
public sealed class QaReportSaveService
{
    public QaReportSaveService(
        QaStoragePaths paths,
        QaMetadataService metadataService,
        QaFolderNameSanitizer folderNameSanitizer);

    public QaReportSavePreparation Prepare(QaReportSaveRequest request);

    public QaReportSaveResult Save(
        QaReportSavePreparation preparation,
        ReadOnlyMemory<byte> pdfBytes,
        bool overwriteConfirmed);
}
```

`Prepare` rereads authoritative metadata and the complete index, resolves canonical destinations, finds current filesystem matches, and returns everything the form needs for a bounded confirmation dialog. It performs no write and does not create a directory. `Save` validates the PDF payload, enters the process save lock, performs a fresh `Prepare`, rejects changes to the prepared authority or existing match set, and commits the refreshed preparation.

An internal constructor accepts the filename service, existing-file service, index service, and `IQaReportTransactionFileOperations`. This is a narrow deterministic failure-injection seam; production callers use the public constructor and cannot replace storage policy.

The request contract is:

```csharp
public sealed class QaReportSaveRequest
{
    public QaReportSaveRequest(
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt);

    public QaReport Report { get; }
    public QaReportValidationResult ValidationResult { get; }
    public DateTimeOffset GeneratedAt { get; }
}
```

`QaReportSavePreparation` exposes `FinalFilename`, `ReportKey`, `FinalStatus`, `GeneratedAt`, `HotelName`, `HotelId`, `PmsName`, `FileMonth`, `HotelCopyPath`, `PmsCopyPath`, `ExistingFiles`, and `RequiresOverwriteConfirmation`. Internally it retains the request, canonical metadata records, canonical directories and paths, final index entry, original index bytes, complete updated index bytes, and discovered filesystem matches.

`QaReportSaveResult` exposes `Success`, `Cancelled`, `FinalFilename`, `HotelCopyPath`, `PmsCopyPath`, `ReportKey`, `FinalStatus`, `OverwriteOccurred`, `HotelMatchesReplaced`, `PmsMatchesReplaced`, `TotalMatchesReplaced`, `IndexUpdated`, `PdfByteLength`, `SavedAtUtc`, `CleanupWarning`, and `ManualReviewRequired`. The current form returns before calling `Save` when the operator cancels; a completed service success therefore has `Cancelled == false`.

Supporting public APIs are:

```csharp
public sealed class QaReportFilenameService
{
    public const int MaximumFilenameLength = 240;

    public QaReportFilenameService(
        QaFolderNameSanitizer folderNameSanitizer);

    public string CreateFilename(
        QaReport report,
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms);
}

public sealed class QaReportFilenameParser
{
    public QaReportFilenameParser(
        QaFolderNameSanitizer folderNameSanitizer);

    public bool TryParse(
        string? filename,
        out QaReportFilenameParts? parts);
}

public sealed class QaReportExistingFileService
{
    public QaReportExistingFileService(
        QaStoragePaths paths,
        QaReportFilenameParser filenameParser);

    public QaReportExistingFiles FindExistingFiles(
        QaHotelMetadata canonicalHotel,
        QaPmsMetadata canonicalPms,
        QaReportKey reportKey);
}

public sealed class QaReportIndexService
{
    public QaReportIndexService(QaStoragePaths paths);
    public IReadOnlyList<QaReportIndexEntry> LoadEntries();
}
```

The index service also has an internal `PrepareUpdate(QaReportIndexEntry)` operation that retains exact source bytes and builds the complete replacement bytes. `QaReportKey` supports construction, `Parse`, `TryParse`, equality, hashing, and `ToString`. `QaReportFilenameParts` exposes the parsed QA Date, three sanitized components, File Month, and derived key. `QaReportExistingFiles` exposes the Hotel and PMS path lists, total count, `HasMatches`, and `IsInconsistent`.

## Final filename and date semantics

The base semantic filename, and exact invariant-culture unmarked form, is:

```text
yyyy-MM-dd_SanitizedHotelName_SanitizedHotelID_SanitizedPMS_yyyy-MM_QAReport.pdf
```

For example:

```text
2026-06-29_ExampleHotel_TEST-001_ExamplePMS_2026-04_QAReport.pdf
```

That visually unchanged form is used when none of the three sanitized components contains an underscore. When at least one component contains an underscore, the filename uses a delimiter-safe marked form. Two marker underscores follow the fixed date separator, so the filename begins `yyyy-MM-dd___`; every literal component underscore is then written as `__`, while a single underscore separates fields. For example:

```text
Hotel Name: Example_Hotel
Encoded component: Example__Hotel
Filename: 2026-06-29___Example__Hotel_TEST-001_ExamplePMS_2026-04_QAReport.pdf
```

The marker is necessary because the original unapproved unmarked format can be ambiguous when component text contains underscores. It distinguishes newly encoded filenames from those old ambiguous names without adding a migration or guessing which split was intended.

The leading `yyyy-MM-dd` is `QaReport.QaDate`: the date the QA review was performed. The later `yyyy-MM` is `QaHotelInformation.FileMonth`: the month represented by the hotel file. These values remain deliberately separate in the filename and index.

File Month, not QA Date, is part of logical replacement identity. A later QA Date for the same Hotel ID and File Month is a replacement candidate and produces a new leading date in the final filename. QA Date is retained as index information and deterministic ordering information, but it is not part of the key.

Filename inputs are limited to QA Date, canonical Hotel Name, canonical Hotel ID, canonical PMS Name, File Month, and the literal suffix. Created By, calculated status, Report ID, current clock text, random values, original path, and destination path do not enter the filename.

### Sanitizer reuse and length policy

Hotel Name, Hotel ID, and PMS Name are independently passed through the existing `QaFolderNameSanitizer.SanitizeLeafName` before delimiter encoding. That operation trims, removes all whitespace, replaces Windows-invalid filename/control characters with `_`, collapses replacement runs, removes unsafe generated edges, handles Windows device names, limits each component to 100 UTF-16 code units, and avoids splitting a surrogate pair while truncating. Thus tests involving consecutive or ordinary leading/trailing raw underscores round-trip the sanitizer's canonical result, not the discarded raw edge characters.

Hotel ID is logical identity, so it has a stricter boundary: its sanitized filename component must equal the trimmed canonical Hotel ID ordinally. A Hotel ID that whitespace removal, invalid-character replacement, device-name handling, or truncation would change is refused instead of allowing two distinct logical IDs to collide in a shared PMS directory.

Phase 9 extends the sanitizer with this backward-compatible public validation API:

```csharp
public bool IsSafeGeneratedFileName(
    string? fileName,
    int maximumLength);
```

It validates a complete generated filename without imposing the stored metadata folder's 100-character and repeated-underscore rules. The combined generation policy limits the complete filename to 240 UTF-16 code units and requires one non-rooted, path-free leaf with no whitespace or Windows-invalid/control characters, dot-directory or device-name cases, or trailing dot or space. `QaReportFilenameService` additionally requires the generated name to end in exact lowercase `.pdf`; discovery follows Windows semantics and accepts the PDF extension case-insensitively before enforcing the canonical parsed structure. There is no truncation or hash fallback for an overlong complete filename: creation is refused as `FilenameFailure`.

Every final and transaction path must also be at most 259 UTF-16 code units under the repository's explicit supported Windows path policy. The destination checks are separator-aware, normalize full paths, and use ordinal-ignore-case descendant comparison.

## Logical report key

`QaReportKey` is the trimmed canonical Hotel ID plus File Month:

```text
TEST-001|2026-04
```

Hotel ID equality and hashing use `StringComparer.OrdinalIgnoreCase`; File Month equality compares its numeric year and month. `TryParse` finds the last `|`, trims the Hotel ID, and requires exact `yyyy-MM` syntax with a valid year and month. QA Date, Hotel Name, PMS, Created By, status, and filename are excluded. The index permits exactly one entry for each key.

This means case-only Hotel ID differences are the same key, while the same Hotel ID in two different File Months is two independent reports.

## Metadata reread and mismatch refusal

Preparation never trusts only the metadata snapshot held when the form opened. It first validates fresh readiness evidence, then calls `QaMetadataService.LoadHotels()` and `LoadPmsSystems()`.

The report's trimmed Hotel ID must identify exactly one current Hotel by ordinal-ignore-case comparison. The report's trimmed Hotel ID, Hotel Name, and PMS are then compared ordinally with the trimmed canonical Hotel values. A case or content difference is refused rather than silently rewriting the report. The Hotel's current PMS must identify exactly one PMS metadata record by ordinal-ignore-case name comparison. Both persisted folder names must pass `IsSafeStoredLeafName`.

Canonical metadata supplies all filename components, the Hotel and PMS folder names, and the summary index fields. A deletion, duplicate identifier, changed Hotel Name, reassigned PMS, unsafe folder, or inaccessible/malformed metadata becomes a focused preparation failure. The user is asked to refresh the report details; Phase 9 does not repair the open report.

`Save` reruns preparation inside the process lock. It requires the logical key, exact filename, canonical destinations, status, generation timestamp, canonical metadata, and both existing-match lists to remain current. The rerun deliberately refreshes the complete index snapshot so an unrelated valid index entry added before the lock can be preserved. The refreshed source index is then checked by exact length and SHA-256 during staging and immediately before its backup.

## Canonical destinations

All destinations come from `QaStoragePaths`:

```text
QAReports/ByHotel/<canonical Hotel FolderName>/<final filename>
QAReports/ByPMS/<canonical PMS FolderName>/<final filename>
QAReports/Index/QAReportIndex.txt
```

`ResolveHotelDirectory`, `ResolvePmsDirectory`, and `QaReportIndexFilePath` remain the authority. Full descendant and supported-length checks are applied to each final path, existing match, index path, temporary path, and rollback path. Destination leaf directories are created only after the form has obtained any required overwrite confirmation and `Save` begins; `Prepare` does not create them.

There is no form path textbox, save dialog, caller path parameter, use of the V1 documentation folders, or write to an arbitrary current directory.

## Existing-report discovery and overwrite identity

The filesystem, not the index, is the source of truth for overwrite detection. `QaReportExistingFileService` enumerates only the top level of the one canonical Hotel directory and one canonical PMS directory. Each PDF candidate is passed to `QaReportFilenameParser`, which requires one safe path-free leaf, the `_QAReport.pdf` suffix, an invariant `yyyy-MM-dd` QA Date, an invariant `yyyy-MM` File Month, exactly three nonempty variable components, and a canonical delimiter encoding.

Unmarked filenames are accepted only when their three components contain no delimiter underscores, which safely supports visually unchanged prior filenames. Marked filenames decode underscore runs deterministically: `__` is one literal underscore, `_` is a field separator, and the canonical three-underscore boundary represents a separator followed by a sanitizer-produced leading underscore. Noncanonical runs, missing or extra fields, malformed dates/months, wrong suffixes, path separators, temporary/rollback names, and ambiguous unmarked legacy filenames are ignored rather than guessed.

The parsed candidate matches when and only when its decoded Hotel ID plus File Month equals the current `QaReportKey`. QA Date, Hotel Name, PMS Name, and full filename are excluded from matching identity. Consequently, a prior filename with an older QA Date, Hotel Name, or PMS Name is detected, and the Hotel and PMS destinations may contain different matching filenames. Exact Hotel IDs prevent prefix/substring false matches such as `TEST-001` versus `TEST-0010`, `XTEST-001`, or `TEST-001-OLD`.

Enumeration is top-level only, and every resulting path is normalized and required to stay in the exact expected leaf directory. Unrelated keys/months, malformed QA report filenames, non-PDF files, directories, and transaction files are ignored. Transaction files never use a `.pdf` extension. Both destination directories remain detection authority; the QA index is a strict summary, not the only source of overwrite evidence.

Matches are de-duplicated ordinal-ignore-case and sorted by filename and full path. An exact final target that already exists but cannot be parsed as a valid current-key filename is an invalid destination, not an overwrite candidate; it is never overwritten or deleted.

`QaReportExistingFiles.IsInconsistent` is true whenever a nonempty state is not exactly one matching PDF in each destination with the same filename under ordinal-ignore-case comparison. It therefore identifies one-sided copies, multiple matches, and differing Hotel/PMS filenames. It is false when no report exists and for the normal one-paired-file state.

## Overwrite and cancel behavior

When no filesystem match exists, the form proceeds without an overwrite dialog. When any match exists, the form shows exactly one bounded Yes/No warning whose default button is No. It includes canonical Hotel Name and ID, PMS, File Month, the count in each location, up to three abbreviated filenames from each location, and a specific inconsistency warning when applicable.

Choosing No returns before `QaReportSaveService.Save` is called. The rendered byte array remains only in memory: no destination directory, temporary file, backup, final PDF, or index update is created, and no success message is shown.

Choosing Yes authorizes replacement of all recognized matches in both canonical directories for that key. It does not authorize replacement of unrelated files. The transaction backs up every recognized match, writes one current Hotel copy and one current PMS copy, and replaces the key's index entry. A successful confirmed reconciliation therefore leaves exactly one current matching PDF in each destination and one index entry for the key, including when the starting state was one-sided, multiply matched, or had differing dated filenames.

The save service enforces the same boundary independently: if refreshed preparation finds matches and `overwriteConfirmed` is false, it throws `OverwriteRequired` before writing.

## Created By, readiness, and generate-once integration

The new `Generate and Save QA Report` button is added to the existing right-to-left footer flow panel. In visual order, Close remains at the right, followed by Check Report Readiness, with Generate and Save QA Report to the left. The button has one click subscription.

Every save attempt executes the existing synchronization sequence before rendering:

1. synchronize the selected Hotel;
2. synchronize report inputs;
3. commit the current statistics control values;
4. synchronize file characteristics;
5. run `QaReportValidationService.Validate(CurrentReport, hotels)`;
6. assign `CurrentReport.ReportStatus` only when ready;
7. store and display the result and select the Statistics/Readiness tab.

The gate requires `IsReady`, zero blocking errors, a calculated status, exact agreement with `CurrentReport.ReportStatus`, and a nonblank readiness fingerprint. A calculated Fail status is still a ready, valid final status and can be saved. Under the corrected Blank/Broken rules, a positive percentage through and including 50% produces a Warning, while a percentage above 50% produces a Failure. Under the Arrival Month rule, exactly 30% or less outside produces no Arrival/File Month finding, while more than 30% produces one statistics Failure. A handled Failure retains its finding severity and remains visible under Failed Checks; if no active Failure remains, the overall calculated status is Pass with Warnings. The Phase 8 renderer and Phase 9 save service independently recheck readiness/fingerprint invariants, including denominator mode and values; neither trusts an old `LastReadinessResult` by itself.

When `CurrentReport.CreatedBy` is blank, the form shows one default-No confirmation before taking the timestamp or rendering. It identifies `QaReportValidationResult.EffectiveCreatedBy`, which is the approved non-mutating fallback. Choosing No performs no render and no write. Choosing Yes does not mutate `CurrentReport.CreatedBy`; the exact effective value is used by the Phase 8 PDF and the index.

After the confirmation gates, the form reads `DateTimeOffset.UtcNow` once and calls `QaPdfGenerationService.GeneratePdf` exactly once. The same validation result and timestamp are placed in `QaReportSaveRequest`; the same returned `byte[]` is supplied as a `ReadOnlyMemory<byte>` to the paired save. The save layer never renders a PDF and never creates independent Hotel/PMS payloads.

On success, the form displays the final filename, both full canonical paths, formatted final status, whether an existing report was replaced, and any bounded cleanup warning. The form remains open. Failures are mapped to focused operator messages; complete exception details and inner exceptions are written only to debug output, not exposed as a stack trace in the dialog.

## Dedicated index format

`QAReports/Index/QAReportIndex.txt` is a deterministic UTF-8 file without a byte-order mark. Each entry is exactly 13 lines including its terminator:

```text
ReportKey: TEST-001|2026-04
QA Date: 2026-06-29
Hotel: Example Hotel
Hotel ID: TEST-001
PMS: Example PMS
File Month: 2026-04
Status: Pass with Warnings
Created By: InnoVarxi QA Team
Filename: 2026-06-29_ExampleHotel_TEST-001_ExamplePMS_2026-04_QAReport.pdf
Hotel Copy: ByHotel/TEST-001_ExampleHotel/2026-06-29_ExampleHotel_TEST-001_ExamplePMS_2026-04_QAReport.pdf
PMS Copy: ByPMS/ExamplePMS/2026-06-29_ExampleHotel_TEST-001_ExamplePMS_2026-04_QAReport.pdf
Saved At UTC: 2026-07-15T18:30:00.0000000Z
---
```

Serialization uses CRLF line endings and a final CRLF. Saved timestamps are normalized to UTC and written with seven fractional-second digits and a literal `Z`. Parsing accepts UTC timestamps with whole seconds or one through seven fractional digits.

The two copy paths are relative to `QAReports`, use forward slashes, and must contain exactly three safe segments: `ByHotel/<stored folder>/<filename>` and `ByPMS/<stored folder>/<filename>`. Full documentation-root paths are deliberately excluded.

### Strict parser and malformed-index refusal

A zero-byte initialized index is valid and represents no entries. A missing index is a focused `Missing` error; normal saving does not silently recreate it. Only the existing storage initialization path owns initial creation.

A nonempty index is refused when it contains a UTF-8 BOM, invalid UTF-8, whitespace only, a bare carriage return, incomplete blocks, an unexpected label/order/separator, blank or untrimmed field values, forbidden control/single-line separator characters, invalid key/date/month/status/timestamp, inconsistent key fields, a filename that is not the locked filename, an unsafe relative path, or duplicate logical keys. LF input and CRLF input are accepted; one optional trailing newline is handled. Malformed content is never reset, partially parsed, appended around, or automatically repaired.

Focused index categories are `Missing`, `ReadFailure`, `MalformedContent`, and `SerializationFailure`. `QaReportIndexException` retains the index path and filename while presenting a bounded reason.

### Normalization, ordering, deduplication, and privacy

New string values are trimmed. CR, LF, tab, other control whitespace, and Unicode line/paragraph separators U+2028 and U+2029 are normalized as ordinary spaces, with whitespace runs collapsed. Non-whitespace controls such as NUL are normalized to `_`. Relative path separators are normalized to `/`. The locked filename and safe relative paths are then revalidated before serialization.

Preparing an update removes every entry equal to the new `QaReportKey`, appends one normalized replacement, preserves all unrelated valid entries, and sorts deterministically by:

1. Hotel ID, ordinal-ignore-case;
2. File Month year;
3. File Month month;
4. QA Date;
5. filename, ordinal-ignore-case and then ordinal as a stable tie-breaker.

The index contains only report-level summary information. It does not contain general notes, checklist notes, finding titles or narratives, scripts, original full paths, guest names, guest emails, reservation details, payment data, credentials, or hotel-file contents.

### Index/filesystem reconciliation

Overwrite discovery is independent of the index. Consequently, a recognized filesystem PDF is protected even if the valid index lacks its key, and an index entry cannot suppress discovery of a one-sided or multiply matched pair. A successful save replaces any existing same-key index entry and reconciles all recognized same-key filesystem matches into one canonical pair.

A valid same-key index entry with missing PDFs is replaced when the user performs a new save. Unrelated valid entries are retained. A malformed or missing index stops the transaction rather than guessing at reconciliation; Phase 9 has no separate repair command or recovery UI.

## Logical transaction

`Prepare` is the read-only phase. It validates current readiness and metadata, produces the exact final filename and paths, records the existing match set, strictly parses the current index, and builds the entire replacement index in memory.

`Save` first validates that the supplied bytes are plausibly a PDF: at least 16 bytes, beginning with `%PDF-`, and containing a `%%EOF` marker. It then refreshes preparation while holding the process-wide save lock.

The commit sequence is:

1. create only the three approved destination directories;
2. confirm that the refreshed source index still has the same length and SHA-256;
3. write the exact PDF bytes to a unique Hotel temporary file with `FileMode.CreateNew`, exclusive sharing, and `Flush(true)`;
4. verify Hotel temporary length and SHA-256 against the in-memory payload;
5. write the same `ReadOnlyMemory<byte>` to a unique PMS temporary file, flush, and verify it;
6. SHA-256 both staged PDFs again and require them to be identical;
7. write the complete updated index to its unique temporary file, flush, and verify length and SHA-256;
8. recheck the exact source index and re-enumerate/recheck the Hotel and PMS match lists;
9. move every recognized Hotel and PMS old PDF to unique rollback names in its original directory;
10. move the old index to a rollback name in the Index directory;
11. atomically rename the staged Hotel PDF to its final path with overwrite disabled;
12. atomically rename the staged PMS PDF to its final path with overwrite disabled;
13. atomically rename the staged complete index to `QAReportIndex.txt` with overwrite disabled; and
14. remove owned rollback/temporary artifacts.

Temporary paths are unique `.qa-<Guid:N>.qa-tmp` names. Backups are unique `.qa-<Guid:N>.qa-rollback` names. They are in the same canonical directories as their corresponding final files, never use `.pdf`, are subject to the same root/length checks, and are created without overwrite. A collision therefore fails safely instead of taking ownership of another file.

The committed order is Hotel PDF, PMS PDF, then index. This allows rollback to remove any new partial commit and restore the exact old index and recognized report files. It is a logical multi-file transaction built from same-directory filesystem moves; Windows does not provide one atomic operation spanning all three directories.

### Rollback and cleanup

On any staging, backup, or commit exception, rollback attempts to:

- delete a newly committed index, PMS PDF, and Hotel PDF in reverse commit order;
- restore the index backup;
- restore every moved PMS and Hotel report backup in reverse move order;
- delete all transaction-owned temporary files; and
- retry cleanup of a temporary file only when the low-level writer established ownership through `CreateNew`.

Restoration moves never overwrite an unexpected destination. Unrelated files are not enumerated into the backup set and are not changed.

If all required rollback actions succeed, the original failure category is retained. `PreviousStateRestored` is true when prior PDF state was actually moved and then fully restored. If any rollback action fails, the outward category is `RollbackFailure`, the original and rollback exceptions are retained as an aggregate, `ManualReviewRequired` is true, and the canonical Hotel, PMS, and Index locations are returned for operator review. No success is reported.

After a successful commit, failure to remove one or more backup artifacts does not falsely report that the save failed. Instead, `CleanupWarning` lists up to five bounded leaf filenames and a remaining count. The final PDFs and index are already committed, and the success dialog surfaces the warning.

## Save errors and result semantics

`QaReportSaveErrorCategory` contains:

- `ReportNotReady`
- `StaleReadiness`
- `MetadataMismatch`
- `FilenameFailure`
- `InvalidDestination`
- `IndexFailure`
- `OverwriteRequired`
- `ConcurrentChange`
- `ContentVerificationFailure`
- `PermissionDenied`
- `FileInUse`
- `TransactionFailure`
- `RollbackFailure`

`QaReportSaveStage` identifies `Preparation`, `HotelStaging`, `PmsStaging`, `IndexStaging`, `ExistingReportBackup`, `IndexBackup`, `HotelCommit`, `PmsCommit`, `IndexCommit`, `Rollback`, or `Cleanup`.

`QaReportSaveException` carries category, stage, `PreviousStateRestored`, `ManualReviewRequired`, and de-duplicated manual-review locations. Operator dialogs map these categories to actionable language without presenting raw paths unnecessarily or exposing stack traces.

A successful result requires both identical PDFs and the complete index to have committed. It reports the final key, paths, status, PDF byte length, generation/save timestamp, replacement counts, and `IndexUpdated == true`. There is no partial-success result for only one PDF.

## Reentrancy and concurrency limits

The form uses `isSavingQaReport`, disables both Generate and Save QA Report and Check Report Readiness during the operation, refuses re-entry, and restores control state in `finally`. The implementation is synchronous on the UI thread; it does not permit background edits during an in-form save.

`QaReportSaveService` uses one static in-process lock shared by all service instances. Inside the lock it reruns preparation, compares the prepared canonical authority and match set, refreshes the valid index snapshot, and later rechecks both the source index bytes and current filesystem matches. All final renames use overwrite-disabled moves, providing another last-resort collision refusal.

The lock is not a cross-process mutex, distributed lease, or filesystem transaction. Another application process can still race after the final checks; the expected response is a filesystem/concurrent failure followed by rollback, not silent overwrite. Phase 9 also cannot guarantee one atomic state across a power loss between three directory commits. `Flush(true)` and same-directory renames improve durability, but they are not a power-loss or storage-device guarantee. Cleanup leftovers and an incomplete rollback remain explicit manual-review cases.

## Repository-specific design choices

- The existing sanitizer supplies component generation and now has one general full-filename validator, avoiding a second Windows invalid-character policy.
- The delimiter marker is used only when a sanitized component contains `_`; filenames without component underscores remain visually unchanged, while marked filenames can be parsed without confusing escaped underscores with field boundaries.
- Old unmarked filenames are accepted only when they contain exactly three separator-free components. Old underscore-bearing ambiguous filenames are ignored rather than guessed, and there is no migration or repair workflow.
- Canonical Hotel IDs must be losslessly representable after sanitization because filename discovery in a shared PMS directory cannot safely distinguish two logical IDs that collapse to one component.
- Preparation and commit are separate because the form must display authoritative filesystem evidence before asking for overwrite permission.
- The filesystem is overwrite authority; the text index is a strict summary and never overrides current files.
- A malformed index causes refusal rather than auto-repair because preserving unrelated entries is safer than reconstructing intent.
- The complete index is staged and replaced, not appended in place, so same-key deduplication and deterministic ordering are enforced on every save.
- `MainForm` passes the already initialized `QaStoragePaths` into `QaReportForm`; the form does not rediscover settings or construct an alternate root.
- A small internal file-operation seam supports deterministic transaction-failure verification without abstracting metadata, path policy, enumeration, or the whole filesystem.
- The UI remains synchronous and uses a static process lock; cross-process locking and background workflows are deferred rather than implied.
- No new package or project setting is needed.

## Verification record

### Detection-correction pre-edit build

Command:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

Result: exit code 0, 0 warnings, 0 errors, 3.83 seconds, with `HEAD` at `42a5b061ae808253dea21affd1b1bdcb6e066b57` and a clean worktree.

### Directly executed Phase 9 tests

Status: **Pass for the directly exercised service, renderer, filesystem, index, and transaction scope**

**Historical-evidence boundary:** The assertion counts and Pass labels in this section describe the Phase 9 baseline when they were run. They remain valid evidence for paired transaction mechanics, byte reuse/equality, overwrite reconciliation, rollback, index syntax, and storage containment. Cases whose expected status or finding content depended on the prior Blank/Broken semantics remain superseded as historical semantic evidence. The separate current correction run passed seven synthetic status/save/PDF/index scenarios; see `Pilot-Correction-Blank-Broken-Statistics.md` for its exact hashes and limitations rather than inferring a result from this Phase 9 count.

An external `net10.0-windows` harness referenced the real application project and used synthetic metadata plus temporary documentation roots outside the repository. Its build completed with 0 warnings and 0 errors. Its final run reported `PASS assertions=263` and `PASS fault-assertions=56`; the harness and all runtime roots were then removed.

The direct assertions covered real Phase 8 rendering for Pass, Pass with Warnings, and Fail; one counted render at the harness render boundary; reuse of that one payload; fresh paired save; byte-for-byte equality and SHA-256 equality; exact index content; no transaction leftovers; key casing/month semantics; QA Date versus File Month replacement; fallback Created By in PDF/index without report mutation; sanitizer, reserved-device, Unicode, 240-character filename, and 259-character full-path boundaries; normal overwrite; Hotel-only, PMS-only, multiple, and differently dated reconciliation; stale readiness and metadata refusal; strict index parsing, ordering, deduplication, normalization, privacy, malformed/missing states, and filesystem/index disagreement; real file locking; staged corruption; and cleanup.

The injected fault matrix covered Hotel and PMS staging I/O and permission categories, staged-byte corruption, PMS commit failure, index commit failure, successful restoration, an intentionally failed rollback with manual-review signaling, and a cleanup failure that correctly returned committed success plus a warning. UI cancellation/default-No and double-click behavior were not claimed by this harness.

A separate isolated correction harness then built the real application and production services with 0 warnings and 0 errors and reported `PASS scenarios=13 assertions=259`. Its configured documentation roots, build outputs, temporary `InternalsVisibleTo` source, and harness source were outside the repository and were removed afterward.

The correction harness directly verified:

- an older QA Date is detected and confirmed replacement leaves one identical current pair and one key entry;
- prior Hotel Name and prior PMS Name components are detected even though neither is part of the key;
- different valid matching filenames in Hotel and PMS destinations are returned by one preparation gate and reconciled together;
- all multiple stale same-key files are replaced while unrelated PDF paths and bytes remain unchanged;
- isolated, consecutive, leading, trailing, and repeated-ID-text underscore inputs produce deterministic sanitized/encoded components and parse back to their canonical sanitized values;
- old underscore-bearing unmarked ambiguity is refused rather than guessed;
- `TEST-001` does not match `TEST-0010`, `XTEST-001`, or `TEST-001-OLD`, and `2026-04` does not match `2026-05`;
- malformed dates, months, components, escaping, suffixes, temporary/rollback names, and non-PDF files are ignored and preserved;
- cancellation by not calling `Save`, plus the service's `overwriteConfirmed: false` guard, leaves PDFs and index byte-for-byte unchanged and creates no transaction artifact;
- confirmed replacement commits only the current pair/index, while an injected failure after all backups restores every original filename and byte array without false success;
- malformed ordinary candidates are preserved, and an exact-target directory collision fails safely; and
- sanitizer-changing Hotel IDs are refused instead of entering lossy filename identity.

### Source inspection completed

Direct source inspection confirms the contracts and code paths described in this document, including filename construction and parsing, key equality, metadata reread, top-level parsed-key discovery, strict index parsing/serialization, preparation/commit separation, generate-once form flow, hash verification, commit order, rollback construction, cleanup warnings, and in-process concurrency controls. The form contains one `DateTimeOffset.UtcNow` read and one direct `QaPdfGenerationService.GeneratePdf` call in the save handler; the same `pdfBytes` variable is passed to `Save`.

Source inspection is not a substitute for executing the transaction fault matrix, visual WinForms checks, publish checks, or end-to-end filesystem assertions.

### Post-edit Debug build

Status: **Pass for the detection correction — exit code 0, 0 warnings, 0 errors; MSBuild elapsed time 1.11 seconds**

Command:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

### Post-edit Release build

Status: **Pass for the detection correction — exit code 0, 0 warnings, 0 errors; MSBuild elapsed time 1.74 seconds**

Command:

```powershell
dotnet build DocumentationLoggingDashboard.sln -c Release
```

### Windows publish and published launch

Status: **Pass for the detection-correction publish and output inspection**

Repository command:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1
```

The correction rerun showed a genuine successful internal restore, Release build, and publish output line, with script exit code 0 and no internal error. The clean output contained only `appsettings.json`, `DocumentationLoggingDashboard.exe`, and its PDB. The fresh single-file executable was 118,631,829 bytes, and no loose PDFsharp or MigraDoc DLL was present. This confirms that the unchanged Phase 8 renderer dependency remains packaged by the corrected application.

Before publishing, the pre-existing ignored `PublishedApp/win-x64` tree was moved aside. After inspection its 20-file relative-path/length/SHA-256 manifest was restored exactly, and the fresh three-file generated output was removed. Published-GUI navigation was not rerun for this detection-only correction; the earlier Phase 9 startup probe remains separate evidence, and a complete published-GUI paired save is still not claimed.

### V1 and earlier-phase regressions

Status: **Pass for direct V1 service/output checks and protected-source inspection; manual GUI checklist not executed**

Source inspection shows no Phase 9 changes in V1 forms/services, `Program.cs`, the Phase 8 renderer implementation, the Phase 7 status/validation services, or the project/package file. `MainForm` changes only the QA Report form's dependency tuple/construction, and the sanitizer change adds a separate full-generated-filename validator without changing the existing sanitizer methods.

A separate external synthetic-data harness built with 0 warnings and 0 errors and reported `PASS v1-assertions=99`. It directly exercised the real settings, template, file, ID, and index services for all three V1 workflows: folder creation, independent `001`/`002` IDs, locked daily filenames, required headers/fields, trimming, blank optional `N/A`, multiline notes, same-day append and blank separation, UTF-8 without BOM, six seven-field index lines, multiline/pipe normalization, canonical saved paths, index privacy, and absence of QA output. Its temporary root, settings, build, and source files were removed. The Phase 8 renderer was also directly exercised by the Phase 9 harness for all three statuses. Visible V1 and Phase 5-7 form interaction remains source-inspected rather than manually executed.

### Currently unverified or intentionally untestable here

Status: **Explicitly bounded**

Not directly driven were visible WinForms layout/accessibility, the actual form handler's renderer-call counter, rapid double-click/reentrancy behavior, MessageBox default-No/cancel choices, a real ACL-denied directory, a full published-GUI paired save, and the manual V1/Phase 5-7 form checklist. The form's single renderer call, re-entry guard, disabled buttons, default-No prompts, and pre-save cancellation are source-inspected only. Real file locking and injected permission categorization were direct. Cross-process races and abrupt machine/storage power loss are explicit design limitations rather than claims of transactional coverage.

## Deferred Phase 10 and later work

This correction does not merge, tag, release, or begin Phase 10. It does not add report history/browsing, opening prior PDFs, index search/filter UI, repair/rebuild tooling, stale-artifact cleanup UI, draft/autosave, background queueing, cross-process/distributed locking, database persistence, cloud upload, email, Teams/Slack notification, spreadsheet export, scheduled automation, or migration of V1 records. Those capabilities require separately approved scope in Phase 10 or later.
