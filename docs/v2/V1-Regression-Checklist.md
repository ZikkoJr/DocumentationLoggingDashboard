# V1 Regression Checklist

Use this checklist after every later V2 phase to confirm the existing V1 documentation-log workflows remain intact.

Use synthetic data only. Do not use real hotel files, real guest names, email addresses, payment data, credentials, production documentation folders, or production customer data.

Result key for each item:

```text
Pass: [ ]  Fail: [ ]  Blocked: [ ]  Notes:
```

## Test Metadata

- Tester:
- Date:
- Branch:
- Commit:
- Build command:
- Build result:
- Temporary documentation root:
- Existing `user-settings.json` present before test: Yes / No
- Existing root `V1_TEST_CHECKLIST.txt` reviewed for continuity: Yes / No
- Final regression result: Pass / Fail / Blocked
- Notes:

## Test Environment

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Test on a Windows environment capable of running WinForms.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `.NET SDK 10.x` or compatible build environment is available.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Build with `dotnet build DocumentationLoggingDashboard.sln`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm build succeeds before running manual V1 checks.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no V2 QA report runtime files are required to run V1.

## Pre-Test Safety

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Use a temporary documentation root created only for this test.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Do not use production documentation folders.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Check whether `user-settings.json` exists beside the executable under test.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Preserve any pre-existing `user-settings.json` without overwriting user-owned data.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Use only synthetic ticket IDs, hotel IDs, PMS names, script names, and summarized issue text.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Do not enter real guest names, emails, payment data, credentials, or full hotel files.

## Application Startup

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Start the app.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Documentation Logging Dashboard` opens without startup errors.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the documentation root folder path is visible.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the app initially loads the persisted root from `user-settings.json`, or `appsettings.json`, or the default `DocumentationLogs` when no persisted setting exists.

## Main Dashboard

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the log type dropdown contains `Debugging Log`, `Script Editing Log`, and `Script Creation Log`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no QA Report option appears in the V1 log type dropdown.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the dynamic field panel updates when changing log type.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the preview area is read-only.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the visible V1 buttons are `Preview Entry`, `Submit Entry`, `Clear Form`, `Open Today's Log File`, `Open Logs Folder`, and `Open Log Index`.

## Debugging Log

Expected V1 source definitions:

- ID format: `DEBUG-yyyyMMdd-###`.
- Folder: `DebuggingLogs`.
- Daily filename: `yyyy-MM-dd_DebuggingLog.txt`.
- Header: `DEBUGGING LOG ENTRY`.
- Required fields: `Hotel Name`, `Hotel ID`, `PMS`, `Error Shown On Ticket`, `Root Cause`, `Fix Applied`.
- Optional fields: `Created By`, `Notes / Follow-up`.
- Index summary: `Hotel: {Hotel Name} / {Hotel ID}` and `PMS: {PMS}`.

Checklist:

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select `Debugging Log`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm all expected required fields appear.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Created By` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Notes / Follow-up` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit with one or more required fields missing and confirm validation blocks save.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter synthetic values for all required fields.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Leave optional fields blank and preview the entry.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview uses `DEBUG-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm blank optional fields display as `N/A`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit the entry and confirm success.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm file was saved under `DebuggingLogs\yyyy-MM-dd_DebuggingLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm daily text includes the expected header, ID, date/time, log type, fields, optional values, and separator lines.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Index\LogIndex.txt` received one line for this entry.

## Script Editing Log

Expected V1 source definitions:

- ID format: `EDIT-yyyyMMdd-###`.
- Folder: `ScriptEditingLogs`.
- Daily filename: `yyyy-MM-dd_ScriptEditingLog.txt`.
- Header: `SCRIPT EDITING LOG ENTRY`.
- Required fields: `Script Name`, `Reason For Edit`, `Changes Made`, `Hotel Applied To`.
- Optional fields: `Created By`, `Notes / Follow-up`.
- Index summary: `Script: {Script Name}` and `Hotel Applied To: {Hotel Applied To}`.

Checklist:

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select `Script Editing Log`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm all expected required fields appear.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Created By` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Notes / Follow-up` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit with one or more required fields missing and confirm validation blocks save.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter synthetic values for all required fields.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Preview the entry.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview uses `EDIT-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit the entry and confirm success.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm file was saved under `ScriptEditingLogs\yyyy-MM-dd_ScriptEditingLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm daily text includes the expected header, ID, date/time, log type, fields, optional values, and separator lines.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Index\LogIndex.txt` received one line for this entry.

## Script Creation Log

Expected V1 source definitions:

- ID format: `CREATE-yyyyMMdd-###`.
- Folder: `ScriptCreationLogs`.
- Daily filename: `yyyy-MM-dd_ScriptCreationLog.txt`.
- Header: `SCRIPT CREATION LOG ENTRY`.
- Required fields: `Script Name`, `Reason For Creation`, `Script Purpose / What It Does`, `Hotels That Use This Script`.
- Optional fields: `Created By`, `Notes / Follow-up`.
- Index summary: `Script: {Script Name}` and `Hotels That Use This Script: {Hotels That Use This Script}`.

Checklist:

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select `Script Creation Log`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm all expected required fields appear.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Created By` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Notes / Follow-up` appears and is optional.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit with one or more required fields missing and confirm validation blocks save.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter synthetic values for all required fields.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Preview the entry.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview uses `CREATE-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit the entry and confirm success.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm file was saved under `ScriptCreationLogs\yyyy-MM-dd_ScriptCreationLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm daily text includes the expected header, ID, date/time, log type, fields, optional values, and separator lines.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Index\LogIndex.txt` received one line for this entry.

## Required-Field Validation

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: For each V1 log type, leave every required field blank and click `Preview Entry`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the warning title is `Missing Required Fields` or equivalent current V1 wording.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm every missing required field label is listed.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no preview is generated when validation fails.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: For each V1 log type, leave a required field blank and click `Submit Entry`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no daily file or index line is written when validation fails.

## Optional Created By

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Leave `Created By` blank and confirm saved text uses `N/A`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter a synthetic tester name or initials and confirm saved text preserves the trimmed value.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Created By` is not included in the index summary fields.

## Optional Notes / Follow-up

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Leave `Notes / Follow-up` blank and confirm saved text uses `N/A`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter multiline synthetic notes and confirm saved daily text preserves readable multiline content.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Notes / Follow-up` is not included in the index summary fields.

## Preview Behavior

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Preview Entry` requires all required fields.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview includes the separator line `==================================================`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview includes `Log ID`, `Date/Time`, and `Log Type`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview text matches V1 daily text formatting except for expected timestamp or ID changes at submit time.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview does not create a daily file.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm preview does not append `LogIndex.txt`.

## Automatic Log IDs

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: For Debugging Log, confirm IDs use `DEBUG-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: For Script Editing Log, confirm IDs use `EDIT-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: For Script Creation Log, confirm IDs use `CREATE-yyyyMMdd-###`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit two entries of the same log type on the same day and confirm the second ID increments by one.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm each log type has its own daily sequence based on its own daily file.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Record whether a previewed ID changed before submit because another entry was saved first.

## Daily Filenames

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm Debugging Log writes `yyyy-MM-dd_DebuggingLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm Script Editing Log writes `yyyy-MM-dd_ScriptEditingLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm Script Creation Log writes `yyyy-MM-dd_ScriptCreationLog.txt`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm files are created under the selected temporary documentation root, not a production folder.

## Daily Text Formatting

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm each entry starts and ends with `==================================================`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the entry header matches the selected log type.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Date/Time` uses `yyyy-MM-dd h:mm tt` style.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm fields appear in the source-defined order.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm required and optional values are trimmed.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm blank optional values are saved as `N/A`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm V1 output remains byte-for-byte or meaningfully unchanged from the V1 baseline, except for expected timestamps, IDs, and synthetic values.

## Multiple Entries on the Same Day

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit two synthetic entries for the same log type.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the second entry appends to the same daily file.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm a blank line separates entries in a non-empty daily file.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm ID sequence increments from `001` to `002` for the same type and day.

## LogIndex.txt Updates

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `Index\LogIndex.txt` is created under the temporary documentation root after the first successful save.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm each successful submit appends exactly one index line.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm index format is `Date | Time | Log ID | Log Type | Primary Item | Secondary Item | Saved File`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm multiline index values are collapsed with ` / `.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm pipe characters in field values are normalized so they do not break the index separator.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm saved file path points to the actual daily text file.

## Change Logs Folder

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Change Logs Folder`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select the temporary documentation root.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the displayed root path updates.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm V1 folders are created: `DebuggingLogs`, `ScriptEditingLogs`, `ScriptCreationLogs`, and `Index`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit a new entry and confirm it writes under the changed root.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm changing the root does not move existing logs.

## Reset to Default Folder

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Reset to Default Folder`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the displayed root returns to the resolved default `DocumentationLogs` path.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the default folder structure is created if missing.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit a synthetic entry and confirm it writes under the default root.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Restore the temporary root if additional tests should continue there.

## Open Today's Log File

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select a log type that has a saved entry for today.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Open Today's Log File` and confirm the correct daily file opens.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Select a log type with no saved entry for today in the current root, if available.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Open Today's Log File` and confirm a friendly not-found message appears.

## Open Logs Folder

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Open Logs Folder`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the current configured documentation root opens.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm missing V1 subfolders are created if they did not already exist.

## Open Log Index

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Open Log Index` after at least one successful save and confirm `Index\LogIndex.txt` opens.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Test a root with no index file and confirm a friendly not-found message appears.

## Privacy Reminder

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the privacy reminder is visible on startup.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm testers do not enter guest names, emails, payment data, credentials, full hotel files, or production customer data.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm synthetic data uses only fake ticket IDs, fake hotel IDs, fake PMS names, fake script names, and summarized fake issue text.

## Clear Form

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Enter values into the selected log type fields.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Generate a preview.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Click `Clear Form`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm dynamic field text boxes are cleared.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the preview text box is cleared.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the selected log type remains selected unless V1 intentionally changed.

## Closing and Reopening

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Change the logs folder to the temporary root.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Close the application.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Reopen the application.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the displayed root path reloads from the persisted setting.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Submit another synthetic entry and confirm it writes under the reloaded root.

## Settings Persistence

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `user-settings.json` is created or updated only in the executable base directory under test.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm `appsettings.json` is not modified by changing folders in the GUI.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm relative default `DocumentationLogs` resolves beside the executable.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm absolute selected paths are used as absolute paths.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Restore any pre-existing `user-settings.json` that was preserved before testing.

## Missing-File Behavior

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: In a fresh temporary root with no saved entries, click `Open Today's Log File`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the app reports that today's log file does not exist yet.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: In a fresh temporary root with no saved entries, click `Open Log Index`.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the app reports that the log index does not exist yet.

## Invalid or Unwritable Folder Behavior

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Attempt to select or use an invalid folder path, if safely reproducible.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the app shows an error and does not change the root to an invalid path.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Attempt to use an unwritable folder, if safely reproducible without touching protected production locations.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm save failure is reported and no misleading success message appears.

## Partial Save or Index Failure

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: If safely reproducible, simulate an index append failure after daily file write.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the app reports that the daily log was saved but the index could not be updated.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm the daily file path is included in the warning.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm manual reconciliation steps are recorded if this occurs.

## Output Comparison Against V1 Baseline

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Compare new Debugging Log output against known V1 format.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Compare new Script Editing Log output against known V1 format.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Compare new Script Creation Log output against known V1 format.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm differences are limited to expected timestamps, IDs, saved paths, and synthetic values.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no QA report output appears in V1 daily text files.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no QA report output appears in V1 `Index\LogIndex.txt`.

## Cleanup

- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Close the app.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Restore any pre-existing `user-settings.json` without overwriting user data.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Remove only synthetic files and temporary folders created for this test.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Do not delete user-owned logs, production folders, or pre-existing settings.
- [ ] Pass: [ ] Fail: [ ] Blocked: [ ] Notes: Confirm no synthetic test artifact remains in the repository.

## Final Regression Result

- Overall result: Pass / Fail / Blocked
- Failed items:
- Blocked items:
- Cleanup completed: Yes / No
- Pre-existing user data restored: Yes / No / Not applicable
- V1 output unchanged except for expected timestamps, IDs, paths, and synthetic values: Yes / No
- QA-independent V1 behavior remains intact: Yes / No
- Tester signature:
- Notes:
