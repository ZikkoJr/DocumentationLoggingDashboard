# Phase 4 Hotel and PMS Management

## Purpose and boundaries

Phase 4 adds the first user-facing QA workflow to the existing WinForms dashboard. It is limited to viewing, searching, adding, and reloading persistent PMS and hotel metadata. A new hotel must reference an existing PMS, and the PMS value returned by the Phase 3 service is the canonical saved relationship.

The approved Phase 3 baseline is commit `1fff629aff29356fc80b91fc220cdaaa1588054a` on branch `v2-qa-reports`.

This phase does not add QA Report creation, report inputs, checklist UI, statistics UI, findings, warning or status calculation, custom-script resolution, raw-file or spreadsheet parsing, database access, PDF generation or saving, QA report index writing, metadata editing or deletion, recovery UI, migration, or automation. All of that remains deferred.

## Main dashboard integration

`MainForm` adds one control:

- Control name: `manageQaHotelsPmsButton`
- Text: `Manage QA Hotels / PMS`
- Placement: the unused right side of `logTypeLayoutPanel`

The table layout retains the existing 92-pixel label column and 260-pixel log-type selector column. A third percentage-width column holds the new 180-by-30-pixel, right-anchored button. The button was not placed in the fixed, non-wrapping bottom `buttonFlowLayoutPanel` because that row already contains the six V1 actions and has limited spare width near the form minimum size. No existing V1 button was moved, renamed, resized, or reordered.

The button event is wired once in `WireButtonEvents()` and calls `OpenQaMetadataManagement()`. No designer event hookup is used.

`OpenQaMetadataManagement()` performs click-time composition in this order:

```csharp
string documentationRoot =
    settingsService.GetDocumentationRootFolder();

QaStoragePaths paths = new(documentationRoot);
QaStorageInitializer initializer = new(paths);
initializer.Initialize();

QaFolderNameSanitizer folderNameSanitizer = new();
QaMetadataService metadataService =
    new(paths, folderNameSanitizer);

IReadOnlyList<QaPmsMetadata> pmsSystems =
    metadataService.LoadPmsSystems();
IReadOnlyList<QaHotelMetadata> hotels =
    metadataService.LoadHotels();
```

Only after both initial loads succeed does `MainForm` construct `QaMetadataManagementForm` and open it with `ShowDialog(this)` inside a `using` declaration.

QA storage initialization therefore occurs only when the user chooses `Manage QA Hotels / PMS`. It does not occur in `Program`, the `MainForm` constructor, ordinary dashboard rendering, application startup, or either V1 folder-setting workflow. Local click-time composition also ensures that the current value from `SettingsService.GetDocumentationRootFolder()` is used after a Change or Reset Folder action.

The existing V1 `ShowError` method and all existing V1 callers remain unchanged. QA entry errors use a separate owned message box and write the full exception only to `Debug.WriteLine`.

## New files and dependency flow

The following files use namespace `DocumentationLoggingDashboard.QAReports.Forms`:

- `QAReports/Forms/QaMetadataManagementForm.cs`
- `QAReports/Forms/QaMetadataManagementForm.Designer.cs`
- `QAReports/Forms/AddPmsForm.cs`
- `QAReports/Forms/AddPmsForm.Designer.cs`
- `QAReports/Forms/AddHotelForm.cs`
- `QAReports/Forms/AddHotelForm.Designer.cs`
- `QAReports/Forms/QaMetadataSearch.cs`

No `.resx` file, UI framework, dependency-injection container, adapter, package, or project-file entry was added. The SDK-style project includes the new source files automatically.

The public form constructors are:

```csharp
public QaMetadataManagementForm(
    QaMetadataService metadataService,
    IReadOnlyList<QaPmsMetadata> initialPmsSystems,
    IReadOnlyList<QaHotelMetadata> initialHotels)

public AddPmsForm(QaMetadataService metadataService)

public AddHotelForm(
    QaMetadataService metadataService,
    IReadOnlyList<QaPmsMetadata> pmsSystems)
```

`MainForm` is the only Phase 4 class that constructs `QaStoragePaths`, `QaStorageInitializer`, `QaFolderNameSanitizer`, or `QaMetadataService`. It passes the concrete Phase 3 service and initial loaded snapshots into the management form. The management form passes that same service into each add dialog. The management form copies its list inputs to form-owned arrays; `AddHotelForm` also clones its supplied PMS records into a private snapshot.

## Reused Phase 3 APIs

Phase 4 calls only these Phase 3 operations:

```csharp
QaStorageInitializer.Initialize()
QaMetadataService.LoadPmsSystems()
QaMetadataService.LoadHotels()
QaMetadataService.AddPmsSystem(string pmsName)
QaMetadataService.AddHotel(
    string hotelId,
    string hotelName,
    string pmsName)
```

The UI does not call `RecoverMetadata`, `SavePmsSystems`, or `SaveHotels`. It does not construct metadata documents, access JSON, read or write files, create QA directories itself, duplicate sanitization logic, or duplicate Phase 3 comparisons. `QaFolderNameSanitizer` is used outside Phase 3 only to construct the approved `QaMetadataService`.

## Reusable search and display behavior

`QaMetadataSearch` is a side-effect-free static helper with:

```csharp
FilterPmsSystems(
    IEnumerable<QaPmsMetadata> source,
    string? searchText)

FilterHotels(
    IEnumerable<QaHotelMetadata> source,
    string? searchText)

GetHotelDisplayText(QaHotelMetadata hotel)
```

Search normalization trims surrounding whitespace and collapses each run of internal whitespace to one comparison space. Matching is ordinal, case-insensitive substring matching. PMS filtering matches `PmsName`; hotel filtering matches either `HotelId` or `HotelName`. Empty, null, or whitespace-only search text returns all source records. Source order is retained and every call returns a new array snapshot without mutating records.

Hotels display as `HotelId — Hotel Name`. The management list binds actual `QaHotelMetadata` objects and uses the WinForms `Format` event for this text. PMS selectors bind actual `QaPmsMetadata` objects and use `PmsName` as `DisplayMember`. Search text is never treated as an authoritative record or PMS value.

This helper has no filesystem, JSON, metadata-write, or UI dependencies. Phase 5 can reuse the same object-preserving filters and hotel display format in any future hotel/PMS selector without creating another search representation.

## Management form behavior

`QaMetadataManagementForm` is centered on its owner, uses font autoscaling, has a default size of 900 by 620 pixels, and a minimum size of 760 by 520 pixels. The layout includes:

- the QA-specific privacy reminder;
- PMS search, list, empty state, Add PMS action, and read-only selected name/folder details;
- hotel search, formatted list, empty state, Add Hotel action, and read-only selected ID/name/saved-PMS/folder details;
- Refresh and Close actions, with Close assigned as `CancelButton`.

The visible privacy reminder permits only Hotel ID, Hotel Name, PMS Name, and safe generated folder names. It explicitly prohibits guest names, guest emails, reservation details, payment data, credentials, raw guest records, and full hotel files.

When no PMS systems exist, Add PMS remains enabled, Add Hotel is disabled, and both sections explain that a PMS must be added first. When PMS systems exist but hotels do not, PMS selection remains usable, the hotel empty state is shown, and Add Hotel is enabled. When records exist, the first visible record is selected on initial binding. An unmatched search has no selection and clears the corresponding details. Clearing search restores the current complete snapshot.

The selected hotel's saved `PmsName` is displayed directly from the bound `QaHotelMetadata` record and is authoritative even if that PMS is filtered out of the PMS list. Hotel selection does not rewrite PMS search text or override an independently preserved PMS selection.

Read-only detail fields have explicit accessible names and remain focusable so users can copy canonical IDs, names, PMS values, and folder names. Search controls precede lists in tab order. Add dialogs set their expected Accept and Cancel buttons, support Escape cancellation, and focus the first required input when shown.

## Add PMS workflow

`AddPmsForm` accepts only PMS Name and has no folder preview. Blank or whitespace-only input disables Add. Submit calls only:

```csharp
QaPmsMetadata created =
    metadataService.AddPmsSystem(pmsNameTextBox.Text);
```

After a successful call, the dialog assigns `CreatedPms`, returns `DialogResult.OK`, and closes. Cancellation does not call the service. The management form then reloads both metadata collections through `QaMetadataService`; it never manually appends the returned record. If the current PMS search permits it, the reloaded record matching `CreatedPms.PmsName` is selected using ordinal case-insensitive comparison, so canonical casing and folder data come from storage.

Phase 3 remains responsible for trimming, safe folder generation, duplicate PMS-name checks, and folder-name collision checks. A duplicate name after trimming/case-insensitive comparison or a generated-folder collision produces the same user-facing duplicate message.

## Add Hotel workflow and canonical PMS

`AddHotelForm` requires Hotel ID, Hotel Name, and deliberate selection of one existing bound `QaPmsMetadata` record. It has no folder preview. Its PMS search uses `QaMetadataSearch.FilterPmsSystems`; unmatched free text is only a filter and is never accepted as a PMS. Filtering out a selection clears it, and clearing the filter restores the choices without manufacturing a selection.

Add is enabled only when both text inputs are nonblank and `pmsListBox.SelectedItem` is a real `QaPmsMetadata`. Submit calls only:

```csharp
QaHotelMetadata created = metadataService.AddHotel(
    hotelIdTextBox.Text,
    hotelNameTextBox.Text,
    selectedPms.PmsName);
```

The search text is never passed to the service. The returned record is assigned to `CreatedHotel`, and its `PmsName` is the canonical relationship chosen by Phase 3. The management form reloads both collections and prefers `CreatedHotel.HotelId` when the current hotel search permits that record. Phase 3 remains responsible for normalized duplicate Hotel ID checks, generated-folder collisions, safe folder naming, existing-PMS validation, and canonical PMS casing.

## Refresh and selection preservation

Manual refresh and post-add refresh use one `TryRefreshMetadata` path. It first captures preferred keys, then loads PMS systems and hotels into local variables. The form swaps neither snapshot until both loads succeed. After the successful swap it reapplies both current search strings and restores selections by PMS name and Hotel ID with ordinal case-insensitive comparison.

An explicit preferred key that is currently filtered out produces no unrelated selection. With no prior or preferred key, the first visible record is selected where reasonable. This prevents an unrelated first row from appearing to be the newly added record. Full refresh preserves PMS and hotel selections independently.

If either load fails, the last successfully loaded snapshots and visible lists remain in place. A post-add failure clearly says that persistence succeeded but the display could not be refreshed. Refresh never writes metadata and never calls recovery.

## Error handling and corruption behavior

`MainForm` catches QA entry failures in this order:

1. `QaUnsupportedMetadataSchemaException`: unsupported version; existing files were not changed.
2. `QaStorageInitializationException`: configured-folder initialization failed; check availability and write access.
3. `QaMetadataException`: invalid or inaccessible metadata; existing file was not changed.
4. `ArgumentException`, `NotSupportedException`, `PathTooLongException`, or `InvalidOperationException`: configured documentation root could not be used.
5. unexpected `Exception`: management could not be opened.

`AddPmsForm` maps duplicate metadata, unsupported schema, invalid input, other metadata save failures, and unexpected failures to the Phase 4 fixed messages. `AddHotelForm` adds a distinct mapping for `ArgumentException` with `ParamName == "pmsName"`, explaining that the selected PMS is no longer available, while other argument failures identify Hotel ID or Hotel Name. Failed adds remain open, keep a non-success result, and refocus the relevant control.

All user-facing errors omit exception messages, stack traces, file contents, and JSON. Diagnostics are retained through `Debug.WriteLine`. Message boxes are owned by their current form.

Initialization preserves every existing metadata file. Therefore an unsupported or corrupt file is not repaired or reset by opening the workflow: initialization leaves it in place, loading throws a typed error, the management form does not open, and V1 remains usable after the blocking message closes. Phase 4 never calls `RecoverMetadata`, so no implicit recovery backup or reset occurs.

## Verification record

### Git safety and baseline

Before editing:

- Active branch: `v2-qa-reports`
- Starting commit: `1fff629aff29356fc80b91fc220cdaaa1588054a`
- Phase 3, Phase 2, and Phase 1 ancestry checks: all exited successfully
- `git status --short`: empty
- `git diff --name-status`: empty

### Builds

The exact before-change command was:

```powershell
dotnet build DocumentationLoggingDashboard.sln
```

Result: exit code 0, build succeeded, 0 warnings, and 0 errors.

The same exact command was used after implementation. Result: exit code 0, build succeeded, 0 warnings, and 0 errors. The project remains a single `net10.0-windows` WinForms project with no package references.

### Temporary-directory verification

A throwaway `net10.0-windows` console/WinForms harness was created under the operating-system temporary directory and referenced the application project. It used unique temporary QA and V1 documentation roots, never the configured real documentation root. The harness completed 141 assertions and returned exit code 0.

Verified directly with synthetic data:

- fixed QA initialization and empty loads;
- `AddPmsSystem("Example PMS")`, canonical reload, normalized duplicate rejection, and no duplicate directory;
- `AddHotel("TEST-001", "Example Hotel", "Example PMS")`, persistence, canonical PMS, normalized duplicate Hotel ID rejection, unknown PMS rejection, and no rejected-add directory;
- full, partial, case-varied, empty, unmatched, trimmed, and repeated-whitespace PMS and hotel searches;
- search snapshot ordering/non-mutation and `HotelId — Hotel Name` display;
- management refresh selection preservation and filtered-out preferred-key integrity;
- corrupt metadata loading throws `QaMetadataException`, leaves the corrupt bytes unchanged, and creates no recovery backup;
- no explicit recovery;
- synthetic V1 service behavior for all three log types, IDs, headers, optional `N/A` formatting, daily filenames, saves, index appends, and seven-part index lines;
- ordinary V1 service activity and `MainForm` construction do not create QA storage.

The harness, its build output, its synthetic settings, and both synthetic data roots were removed after an absolute-path containment check. No temporary file appears in repository status.

### WinForms verification actually performed

The temporary harness constructed the forms on an STA thread and performed offscreen control-level smoke checks. It verified Add/Cancel assignments, blank-input enablement, deliberate existing-PMS selection, unmatched-filter selection clearing, management empty-state text and enablement, bound object counts, canonical saved-PMS details, normalized searches, cleared stale details, default selection behavior, refresh selection preservation, and minimum-size list/button bounds. It also constructed `MainForm`, verified the three V1 log types and existing button labels, confirmed the preview remains read-only, exercised dynamic field rendering, invoked Clear Form behavior, and confirmed the new button fits the log-type row while all existing bottom buttons fit at minimum size.

### Interactive GUI verification not performed

No human-visible Windows desktop session was available through the execution tools, so the application was not interactively clicked or visually inspected. The following remain a manual GUI checklist rather than claimed passes:

- visual appearance, wrapping, DPI behavior, and clipping at default and minimum sizes;
- clicking the dashboard QA action and observing owned modal centering;
- interactive Add PMS/Add Hotel submission, cancellation, Enter, Escape, and focus rendering;
- closing/reopening the dialog to observe persistence;
- interactive duplicate, unsupported-schema, corruption, and refresh-failure message boxes;
- selecting hotels assigned to different PMS systems and visually confirming saved-PMS updates;
- screen-reader announcements and mnemonic behavior;
- continued V1 usability after closing QA error/dialog messages;
- interactive Preview, Submit, folder picker, Open File/Folder/Index, Reset Folder, startup, and shutdown behavior.

Source inspection confirms the relevant ownership, layout, tab order, event wiring, message mapping, and state transitions, but it is not represented as an interactive GUI pass.

### V1 regression evidence

All V1 production source files are unchanged. The build, synthetic V1 service assertions, and offscreen `MainForm` checks cover startup construction, log-type selection and dynamic rendering, all three source definitions, IDs, formatting, filenames, index output, existing button labels, privacy reminder, preview read-only state, Clear Form, current-root resolution, and the absence of QA initialization during ordinary V1 activity.

Required-field warning dialogs, interactive Preview/Submit success and failure messages, shell opening, folder-picker workflows, persisted-root reopen, partial index failure, and application shutdown were not executed interactively. Their source paths are unchanged and were reviewed against `V1-Regression-Checklist.md`.

## Final scope confirmations

- UI code does not directly access JSON or the filesystem.
- `RecoverMetadata`, `SavePmsSystems`, and `SaveHotels` are never called by Phase 4.
- No Phase 2 or Phase 3 model, definition, service, or representation was changed or duplicated.
- No protected V1 model or service was changed.
- `Program.cs`, `SettingsService.cs`, and the project file are unchanged.
- V1 logging behavior remains isolated and unchanged.
- No package was added.
- No real hotel, PMS, guest, or production metadata was added.
- No QA Report form, report creation action, checklist UI, statistics UI, findings/status logic, PDF feature, report saving, or report-index writing was added.
- No scope deviation was required.
