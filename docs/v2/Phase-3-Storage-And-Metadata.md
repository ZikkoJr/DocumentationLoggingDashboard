# Phase 3 QA Storage and Metadata Foundation

## Purpose and Scope

Phase 3 adds the isolated filesystem and persistent metadata foundation for future QA Reports. It defines QA storage paths, explicit initialization, schema-versioned hotel and PMS metadata, complete validation, deterministic folder names, atomic metadata replacement, and explicit backup-and-reset recovery.

This phase does not add a user-facing QA workflow. It does not integrate with `Program`, `MainForm`, or `SettingsService`, and it does not create or save QA reports.

## Source Layout

All runtime code added by this phase is under the existing QA-specific area.

The namespace `DocumentationLoggingDashboard.QAReports.Models` contains:

- `QaHotelMetadata.cs`: persistent hotel metadata only.
- `QaPmsMetadata.cs`: persistent PMS metadata only.
- `QaHotelMetadataDocument.cs`: schema-versioned `hotels.json` root.
- `QaPmsMetadataDocument.cs`: schema-versioned `pms-systems.json` root.
- `QaMetadataFileKind.cs`: closed selector for the two known metadata files.
- `QaMetadataRecoveryResult.cs`: immutable recovery result.

The namespace `DocumentationLoggingDashboard.QAReports.Services` contains:

- `QaStoragePaths.cs`: side-effect-free fixed path derivation and validated record-folder resolution.
- `QaStorageInitializer.cs`: explicit creation of missing fixed storage.
- `QaFolderNameSanitizer.cs`: deterministic Windows-safe leaf-name generation and validation.
- `QaMetadataService.cs`: load, add, complete-document save, validation, and recovery operations.
- `QaMetadataException.cs`: base known-file metadata error.
- `QaDuplicateMetadataException.cs`: distinguishable duplicate-key or folder-collision error.
- `QaUnsupportedMetadataSchemaException.cs`: distinguishable unsupported-schema error.
- `QaStorageInitializationException.cs`: initialization error with its original cause.
- `QaMetadataJson.cs`: shared repository-local `System.Text.Json` behavior and empty-document factories.
- `QaAtomicFileWriter.cs`: same-directory temporary-write and replace/move implementation.

No Phase 2 source file was moved, renamed, or changed. The SDK-style project includes these new `.cs` files automatically, so the project file and package references remain unchanged.

## Documentation Root Integration

`SettingsService` remains unchanged. A later UI phase can resolve the configured V1 documentation root and pass the result into the QA path model:

```csharp
string configuredRoot = settingsService.GetDocumentationRootFolder();
QaStoragePaths paths = new(configuredRoot);
```

Phase 3 does not inject or instantiate `SettingsService`. `QaStoragePaths` accepts the already-resolved or caller-supplied root string, rejects null/blank values, and normalizes it with `Path.GetFullPath`.

Constructing `QaStoragePaths`, `QaStorageInitializer`, `QaFolderNameSanitizer`, or `QaMetadataService` performs no filesystem write. Initialization occurs only when a caller explicitly calls:

```csharp
new QaStorageInitializer(paths).Initialize();
```

## Storage Paths and Directory Structure

`QaStoragePaths` exposes the normalized documentation root and strongly defined paths for the QA root, category roots, metadata root and files, index root and file, and metadata backup root. Every fixed child uses `Path.Combine`. Separator-aware, ordinal case-insensitive descendant checks ensure fixed paths stay below the configured root.

The two record-folder resolvers accept only a validated Windows-safe leaf and resolve it beneath the appropriate category root. They are not general arbitrary-path combination APIs.

Explicit initialization creates missing elements of this structure:

```text
<ConfiguredDocumentationRoot>/
+-- QAReports/
    +-- ByHotel/
    +-- ByPMS/
    +-- Metadata/
    |   +-- hotels.json
    |   +-- pms-systems.json
    +-- Index/
        +-- QAReportIndex.txt
```

`QAReports/Metadata/Backups` is not created by path construction or ordinary initialization. It is created lazily only when explicit recovery is requested for an existing selected metadata file.

## Initializer Behavior

`QaStorageInitializer.Initialize()`:

- creates missing fixed directories;
- creates a missing `hotels.json` with a valid empty schema-version-1 document;
- creates a missing `pms-systems.json` with a valid empty schema-version-1 document;
- creates a missing `QAReportIndex.txt` as a zero-byte file;
- uses same-directory GUID temporary files for new metadata documents and flushes them before moving them into place;
- attempts to remove an owned temporary file after failure;
- is safe to call repeatedly; and
- wraps failures in `QaStorageInitializationException` while retaining the original exception as `InnerException`.

Existing files and directory contents are preservation-only. The initializer does not validate, rewrite, repair, or reset an existing empty, whitespace-only, malformed, unsupported, or otherwise invalid metadata file. It also never inserts PMS, hotel, or index records.

## Persistent Metadata and JSON Schemas

Persistent metadata is deliberately separate from the Phase 2 `QaHotelInformation` report snapshot. `QaHotelInformation` includes report-specific `FileMonth`; `QaHotelMetadata` does not. Persistent metadata contains no QA date, file month, checklist result, finding, statistic, report status, or report content.

The exact empty hotel schema is:

```json
{
  "schemaVersion": 1,
  "hotels": []
}
```

A hotel record uses only:

```json
{
  "hotelId": "TEST-001",
  "hotelName": "Example Hotel",
  "pmsName": "Example PMS",
  "folderName": "TEST-001_ExampleHotel"
}
```

The exact empty PMS schema is:

```json
{
  "schemaVersion": 1,
  "pmsSystems": []
}
```

A PMS record uses only:

```json
{
  "pmsName": "Example PMS",
  "folderName": "ExamplePMS"
}
```

Both document types define `CurrentSchemaVersion = 1`. Their deserialization-facing schema and collection properties are nullable and have no default initializers. A structural JSON precheck and subsequent model validation therefore distinguish a missing property, an explicit JSON `null`, and a valid empty array. Null array entries are rejected.

The shared serializer options are:

- `WriteIndented = true`;
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`; and
- `PropertyNameCaseInsensitive = true`.

Serialization uses UTF-8 without a byte-order mark.

## Metadata Operations and Validation

`QaMetadataService` has no long-lived metadata cache. Every public load and every add operation rereads persisted metadata. Returned load results are cloned arrays, so callers do not receive an internal mutable collection or the deserialized records used by the service.

Normal PMS loading rejects:

- missing, empty, whitespace-only, or malformed JSON;
- a non-object JSON root;
- missing, null, noninteger, or unsupported `schemaVersion`;
- missing or null `pmsSystems`;
- null records;
- blank required names or folder names;
- duplicate PMS names after trimming and ordinal case-insensitive comparison;
- duplicate PMS folder names under ordinal case-insensitive comparison; and
- folder names that do not satisfy the stored Windows-safe leaf rules.

Normal hotel loading first loads and validates current PMS metadata. It then applies the corresponding document, collection, null-record, required-value, folder, Hotel ID, and folder-collision checks to `hotels.json`. Every hotel PMS reference must match a current PMS name after trimming and ordinal case-insensitive comparison. Loading accepts a relationship whose casing differs and does not silently recase or rewrite it.

Normal loading is read-only. Invalid input is not normalized, rewritten, repaired, reset, or backed up automatically, and metadata JSON content is never copied into exception messages.

`SavePmsSystems` validates the complete PMS document and checks current hotel records against the proposed PMS set before serialization, preventing a normal complete-document save from orphaning hotels. `SaveHotels` rereads PMS metadata and validates the complete hotel document and every relationship before serialization. Neither complete-document save creates record folders. Explicit one-file recovery remains intentionally separate and can reset PMS metadata even when that causes later hotel relationship validation to fail.

## PMS Add Rules

`AddPmsSystem`:

1. Rereads and validates `pms-systems.json`.
2. Trims the proposed display name and rejects a blank result.
3. Rejects a name duplicate using `StringComparer.OrdinalIgnoreCase`.
4. Generates a deterministic safe folder name from the trimmed display name.
5. Rejects a case-insensitive folder-name collision without suffixing or hashing.
6. Validates the complete updated in-memory document.
7. Creates or confirms `ByPMS/<FolderName>` only after logical validation.
8. Atomically saves the complete document.

New PMS records preserve the caller's selected display casing. `Mews`, `mews`, `MEWS`, and ` Mews ` identify the same PMS for duplicate comparison.

## Hotel Add and Relationship Rules

`AddHotel`:

1. Trims the requested PMS name, rereads PMS metadata, and finds it case-insensitively.
2. Rejects an unknown PMS and never silently creates it.
3. Rereads and validates hotel metadata, including every existing PMS relationship.
4. Trims and requires Hotel ID and Hotel Name.
5. Rejects a duplicate Hotel ID using `StringComparer.OrdinalIgnoreCase`.
6. Generates and collision-checks the deterministic hotel folder name.
7. Stores the trimmed canonical display casing from the matched PMS record.
8. Validates the complete updated in-memory hotel document.
9. Creates or confirms `ByHotel/<FolderName>` only after logical validation.
10. Atomically saves the complete hotel document.

Hotel Name is not a unique key. Different Hotel IDs may use the same Hotel Name when their resulting folder names do not collide.

## Folder-Name Convention

`QaFolderNameSanitizer` implements explicit Windows rules and does not rely solely on the host operating system's invalid-character list.

Generated names:

1. trim leading and trailing whitespace;
2. remove whitespace inside the value;
3. replace non-whitespace control characters and `< > : " / \ | ? *` with `_`;
4. collapse consecutive `_` characters;
5. remove leading replacement characters where practical and unsafe trailing periods, spaces, and replacement characters;
6. reject `.` and `..` and reject any input that produces no nonempty leaf;
7. recognize `CON`, `PRN`, `AUX`, `NUL`, `COM1` through `COM9`, and `LPT1` through `LPT9`, including those device names followed by an extension, using ordinal case-insensitive comparison;
8. make a reserved result safe deterministically by prefixing `_`; and
9. truncate deterministically to `QaFolderNameSanitizer.MaximumLeafNameLength`, which is 100 UTF-16 code units, without splitting a surrogate pair or leaving an unsafe trailing character.

PMS folders use the sanitized PMS display name. Hotel folders use:

```text
SanitizedHotelId_SanitizedHotelName
```

The 100-character limit applies to the final hotel composite. For example, `12345 / Example Hotel` produces `12345_ExampleHotel`.

Stored folder validation rejects rooted values, separators, control or Windows-invalid characters, whitespace, dot-directory names, reserved device names, overlength values, repeated replacement characters, and unsafe trailing period, space, or replacement characters. A generated reserved-name form such as `_CON` remains valid.

Sanitization can map distinct input values to the same leaf. Folder uniqueness is therefore checked case-insensitively within PMS metadata and within hotel metadata. No suffix or hash is silently added. Identical leaves are allowed across `ByPMS` and `ByHotel` because those are different roots.

## Record Directory Timing and Rollback

Successful PMS and hotel additions ensure their respective record directory exists. Simple loads and complete-document saves do not create those directories.

After all logical validation, an add operation records whether the target directory exists, creates it, and attempts the atomic metadata save. If the save fails and this operation created the directory, it attempts only `Directory.Delete(path, recursive: false)`. A pre-existing directory is never deleted. A nonempty directory is never recursively deleted.

Complete add, save, and recovery mutations are serialized by a process-local lock. This prevents separate service instances in the same application process from loading stale documents concurrently or both inferring ownership of the same newly created directory. This is not a cross-process lock.

When rollback succeeds, the original save exception is rethrown. If rollback also fails, `QaMetadataException.InnerException` retains the original save failure and `RollbackException` exposes the secondary rollback failure.

## Atomic Replacement and Concurrency

Every normal add or complete-document save validates the complete document and serializes it fully in memory before opening a temporary file. Initialization and recovery serialize trusted, explicitly constructed empty schema-version-1 documents. `QaAtomicFileWriter` then:

1. creates a GUID-named `.tmp` file in the destination metadata directory with `FileMode.CreateNew`;
2. writes UTF-8 bytes without a BOM;
3. calls `Flush(flushToDisk: true)` and closes the stream;
4. uses `File.Replace` for an existing destination;
5. uses a same-directory `File.Move` when the initializer creates a genuinely missing destination; and
6. attempts to delete only the temporary file it created after any unsuccessful operation.

The destination is never opened for incremental JSON serialization, and no partial list update is written.

Cross-process locking is not implemented. Atomic same-directory replacement reduces partial-file risk, but two application processes can still race, including around record-directory creation, and the last successful metadata writer can win. Crash and filesystem durability guarantees remain subject to the underlying Windows filesystem.

## Errors and Explicit Recovery

The public typed errors are:

- `QaMetadataException`: malformed, structurally invalid, missing, unreadable, or unwritable known-file metadata, with file kind, path, filename, and relevant original exception.
- `QaDuplicateMetadataException`: a distinguishable subtype for duplicate keys/names and case-insensitive folder collisions.
- `QaUnsupportedMetadataSchemaException`: a distinguishable subtype with actual and expected versions when available.
- `QaStorageInitializationException`: explicit initialization failure with the original exception retained.

`RecoverMetadata(QaMetadataFileKind)` accepts only the closed `Hotels` or `PmsSystems` selector. It never accepts an arbitrary path and never runs automatically.

For an existing selected file, recovery:

1. creates `Metadata/Backups` when needed;
2. copies the selected source with overwrite disabled, preserving its exact bytes;
3. names the backup as `<original filename>.<UTC yyyyMMddTHHmmssfffffffZ>.<GUID>.bak`;
4. atomically replaces only the selected file with its valid empty schema-version-1 document; and
5. returns the selected kind, original metadata path, and created backup path.

The backup remains even if the later reset fails. The other metadata file is not read, reset, or rewritten by recovery. A missing selected source produces a clear error before a backup is claimed. Recovery does not require the selected file to be invalid, so a second explicit recovery creates a distinct, non-overwriting backup.

## Verification Record

The final Phase 3 verification used a disposable `net10.0-windows` console project outside the Git worktree with an absolute project reference and synthetic PMS/hotel data only.

- Temporary harness: `C:\Users\GABRIE~1\AppData\Local\Temp\QaPhase3Verification-dec87d3762534c5b8800500fab920a94`
- Temporary runtime root: `C:\Users\GABRIE~1\AppData\Local\Temp\QaPhase3Verification-dec87d3762534c5b8800500fab920a94\Runtime-5df3910f311d4dd7b1634e85a5106b66`
- Exact run command: `$env:QA_PHASE3_HARNESS_ROOT='C:\Users\GABRIE~1\AppData\Local\Temp\QaPhase3Verification-dec87d3762534c5b8800500fab920a94'; dotnet run --no-build --project 'C:\Users\GABRIE~1\AppData\Local\Temp\QaPhase3Verification-dec87d3762534c5b8800500fab920a94\QaPhase3Verification-dec87d3762534c5b8800500fab920a94.csproj'`
- Direct result: harness build succeeded with 0 warnings and 0 errors; all 36 verification groups passed, with 0 failures and process exit code 0.

Directly executed checks covered side-effect-free construction, exact path derivation, explicit and idempotent initialization, preservation of existing files and index sentinel content, empty version-1 documents, UTF-8 without BOM, PMS and hotel add/reload/duplicate behavior, canonical PMS storage, same-name hotels, folder creation and collisions, sanitizer edge cases, complete-document saves and reloads, no-cache rereads, defensive snapshots, missing/corrupt/structurally invalid data, unchanged destination bytes on rejected operations, temporary cleanup, and one-file recovery with exact-byte backups and distinct backup names. A Windows read handle without delete sharing forced real PMS and hotel destination-replacement failures; the tests confirmed unchanged destination bytes, no `.tmp` leftovers, rollback of a newly created empty record directory, and preservation of a pre-existing sentinel directory.

Source inspection additionally confirmed that constructors contain no hidden filesystem calls, fixed children use `Path.Combine`, comparisons are ordinal, JSON is serialized completely in memory, temporary writes stay in the destination directory and flush before replace/move, failure paths attempt cleanup, recovery maps only the closed enum to known paths, no cross-process lock exists, and no deferred Phase 4 behavior is present.

Low-level serialization failure, temporary-stream failure, power-loss behavior, GUID collision, rollback failure, and concurrent-process last-writer races were not forced with production test hooks. GUI smoke testing was not performed because Phase 3 has no UI integration.

The disposable runtime root and harness were removed after verification. Neither was created in the Git worktree, no QA runtime directory was created under the repository, and the real configured documentation root was never resolved or used.

## Deferred Functionality and V1 Regression Boundary

Phase 3 does not implement or change:

- `Program` or `MainForm` startup/integration;
- any WinForms form, control, dropdown, search, or report-entry workflow;
- checklist applicability or evaluation;
- warning, failure, statistics, or report-status calculation;
- custom-script behavior;
- QA report serialization, saving, filenames, paired hotel/PMS report files, or PDFs;
- QA index entries, formats, writes, models, or services;
- raw-file, spreadsheet, or database processing;
- diagnostics, automation, monitoring, metadata editing/deleting/renaming/migration; or
- a permanent test project.

V1 source and storage behavior remain unchanged. `Program.cs`, both `MainForm` files, `DocumentationLoggingDashboard/Models`, `DocumentationLoggingDashboard/Services` including `SettingsService`, `appsettings.json`, and the Phase 2 definitions/models are untouched. Therefore V1 output folders, filenames, IDs, formatting, index behavior, required-field validation, controls, and privacy reminders receive no source-level change from Phase 3.
