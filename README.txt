Documentation Logging Dashboard
===============================

Documentation Logging Dashboard is a C# WinForms desktop application for three
established documentation logs and two explicit QA workflows. New Debugging,
Script Editing, and Script Creation entries use selected Running Excel
workbooks plus canonical Hotel/PMS Excel histories. Historical V1 daily .txt
files and their LogIndex.txt entries remain valid and are not migrated or
modified. Detailed QA creates paired Hotel/PMS PDF reports and a separate QA
Report index. Quick QA appends a client-facing Surface workbook and the selected
Hotel's private Quick QA history as one logical save.

QA actions
----------

The dashboard exposes two direct actions. Detailed QA Report opens the existing
full report workflow. Quick QA opens a dedicated one-page Surface QA workflow.
There is no report-type wizard. The existing three-item documentation log-type
selector remains the entry point for the Excel documentation workflows.

Detailed QA Report
------------------

Detailed QA Report supports Hotel/PMS metadata, mandatory text File ID, a
29-item checklist, separate Blank and Broken statistics, findings, readiness
validation, paired PDF saving, logical-report overwrite handling, and a
dedicated QAReportIndex.txt. File ID preserves leading zeroes and appears in new
schema-2 PDFs/index entries, but it does not change the PDF filename or existing
Hotel ID + File Month overwrite identity. Historical schema-1 PDFs and 13-line
index entries are not migrated and remain readable.

The active checklist contains 22 Raw File items and 7 Database items.
RAW.DATES.ARRIVAL_WITHIN_FILE_MONTH is retired from that catalog. File Month
statistics remain visible; more than 30% of valid nonblank Arrival Dates outside
the selected File Month creates one statistics-sourced Failure, while exactly
30% or less creates no Arrival/File Month finding.

RAW.REQUIRED.EMAIL_PRESENT is retired. The new Email column available row
produces one Warning, never a Failure, when unavailable. The old combined Source
row is replaced by separate Source, Rate, and Market availability rows. One or
two unavailable strategy categories produce exactly one Strategy Warning; all
three produce exactly one aggregate Failure.

Blank and Broken thresholds are independent:

- 0% creates no threshold finding.
- Above 0% through 50%, inclusive, creates a Warning.
- Above 50% creates a Failure.

Blank counts describe missing cells. Broken counts describe invalid populated
values, so blank cells do not fail populated-value checks. Automatic Blank
denominators follow Total Data Rows. Automatic Broken denominators follow the
matching Blank denominator minus Blank Count. Each denominator can be manually
overridden and reset to Auto.

Warning-only and handled-Failure reports save as Pass with Warnings. Active
Failures save as Fail. A handled Failure retains Failure severity and remains in
Failed Checks. Pending notes and other free text commit at validation/action
boundaries, including before readiness and save. The Hotel and PMS copies are
written from the same PDF bytes, and the QA index records the final status.

The formerly deferred Hotel/PMS documentation-log routing applies to new Excel
events only. It does not rewrite or backfill historical V1 Debugging TXT files.

Quick QA / Surface QA
---------------------

Quick QA is a fixed manual checklist with 14 Raw File and 7 Database checks. It
has no Statistics, File Characteristics, Arrival/File Month analysis, PDF,
Detailed index update, or PMS history. It uses canonical Hotel/PMS metadata,
File Month, mandatory text File ID, direct Warning/N/A choices where approved,
per-finding custom-script handling, a live Result, and a generated but editable
Summary that cannot be saved after it becomes stale.

A successful Quick QA appends exactly one row to each of:

DocumentationLogs/QAReports/SurfaceQA/<selected workbook>.xlsx
DocumentationLogs/QAReports/ByHotel/<canonical hotel>/QuickQAHistory.xlsx

The selected documentation root replaces DocumentationLogs when a custom root
is configured. Client workbooks use exact columns File Month, Hotel Name, Hotel
ID, PMS, File ID, Result, and Summary. Hotel history adds a round-trip
DateTimeOffset-style QA Timestamp. File Month is yyyy-MM, and both IDs are text.

New Surface workbooks contain worksheet Surface QA and table SurfaceQaTable.
The app also accepts only the documented seven legacy header aliases, including
Month 2026 and Pass/Fail/Warning, then canonicalizes those headers during a
successful staged append while preserving historical rows. The selected
workbook must be a direct child of QAReports/SurfaceQA. Existing files are never
silently overwritten.

Both workbook updates are staged, verified, concurrency-checked, committed, and
rolled back as one logical transaction. If Excel has either workbook open or a
destination is otherwise unavailable, the app reports a friendly failure and
does not leave an ordinary one-sided Quick QA event.

ClosedXML 0.105.1 supplies managed .xlsx support. Excel, Office Interop,
LibreOffice, browser automation, cloud APIs, external spreadsheet executables,
and runtime package installation are not required or used. Package licenses and
notices are recorded in THIRD-PARTY-NOTICES.txt.

Current design and verification contracts are in:

- docs/updates/Quick-QA-Surface-QA-Design.md
- docs/updates/Detailed-QA-Check-Changes.md
- docs/updates/Quick-QA-Test-Matrix.md
- docs/updates/Excel-Documentation-Logs-Design.md
- docs/updates/Excel-Documentation-Logs-Test-Matrix.md
- docs/updates/Legacy-TXT-Compatibility.md

Supported log types
-------------------

The documentation workflow supports exactly three log types:

- Debugging Log
- Script Editing Log
- Script Creation Log

Required fields
---------------

Debugging Log:

- One canonical Hotel selected through the searchable QA Hotel metadata picker
- Error shown on ticket
- Root cause
- Fix applied

The selected metadata supplies read-only Hotel Name, Hotel ID, and PMS values.
Saving reloads metadata and blocks a removed or stale routing selection.

Script Editing Log:

- Hotel ID(s), entered manually and resolved through current metadata
- Script Name
- Reason for edit
- Changes made

Script Creation Log:

- Hotel ID(s), entered manually and resolved through current metadata
- Script Name
- Reason for creation
- Script purpose / what it does

Editing and Creation accept comma-, semicolon-, or line-separated Hotel IDs.
Unknown IDs block the full save. IDs remain text, leading zeroes are preserved,
duplicates are removed in first-entered order, and workbook values use
`ID1; ID2; ID3`.

Optional fields
---------------

These fields can be left blank for any log type:

- Created By
- Notes / Follow-up

Blank optional fields are saved as N/A.

Automatic fields
----------------

The app automatically includes:

- Log ID
- Date/Time

Log Type is stored in the hidden workbook schema metadata and shown through the
selected workflow. It is deliberately not an Excel table column.

Log ID format
-------------

Log IDs use the log type prefix, date, and a daily sequence number:

- DEBUG-yyyyMMdd-###
- EDIT-yyyyMMdd-###
- CREATE-yyyyMMdd-###

Examples:

- DEBUG-20260618-001
- EDIT-20260618-001
- CREATE-20260618-001

Documentation log output and legacy V1 files
--------------------------------------------

Each log type has one actively selected Running `.xlsx` workbook. New files are
created only beneath the applicable Running directory, and the app remembers a
separate filename for Debugging, Script Editing, and Script Creation:

- DebuggingLogs/Running/<selected workbook>.xlsx
- ScriptEditingLogs/Running/<selected workbook>.xlsx
- ScriptCreationLogs/Running/<selected workbook>.xlsx

Each new event also updates every applicable canonical Hotel history and every
applicable unique canonical PMS history under QAReports/ByHotel and
QAReports/ByPMS. Debugging routes to one Hotel and one PMS. Editing and Creation
accept one or more Hotel IDs, write each Hotel once, and write each canonical
PMS once per event.

Historical V1 daily files such as `2026-06-18_DebuggingLog.txt` remain directly
under the three legacy folders. They stay byte-for-byte untouched, receive no
new entries, and are not moved into Running or migrated to Excel.

Output folder structure
-----------------------

The selected documentation root folder contains this structure:

DocumentationLogs/
|
+-- DebuggingLogs/
|   +-- Running/
+-- ScriptEditingLogs/
|   +-- Running/
+-- ScriptCreationLogs/
|   +-- Running/
+-- Index/
|   +-- LogIndex.txt
|   +-- documentation-log-settings.json
|   +-- documentation-log-sequences.json
+-- QAReports/
    +-- ByHotel/<canonical Hotel folder>/<type-specific LogHistory.xlsx>
    +-- ByPMS/<canonical PMS folder>/<type-specific LogHistory.xlsx>

Missing Running and deterministic history workbooks are created safely as part
of the applicable logical save. An existing corrupt, locked, wrong-type,
wrong-scope, or structurally incompatible workbook is rejected rather than
repaired or overwritten.

LogIndex.txt
------------

Each successful logical event appends exactly one pipe-separated summary line
to the existing documentation index, regardless of Hotel/PMS copy count:

DocumentationLogs/Index/LogIndex.txt

Format:

Date | Time | Log ID | Log Type | Primary Item | Secondary Item | Saved File

Summary field mapping:

- Debugging Log: Hotel: {Hotel Name} / {Hotel ID} | PMS: {PMS}
- Script Editing Log: Script: {Script Name} | Hotel IDs: {canonical Hotel IDs}
- Script Creation Log: Script: {Script Name} | Hotel IDs: {canonical Hotel IDs}

Examples:

- 2026-06-18 | 10:24 AM | DEBUG-20260618-001 | Debugging Log | Hotel: Example Hotel / 12345 | PMS: Mews | C:\CompanyDocs\DocumentationLogs\DebuggingLogs\Running\Team Debugging.xlsx
- 2026-06-18 | 10:31 AM | EDIT-20260618-001 | Script Editing Log | Script: MewsPMS.cs | Hotel IDs: 12345; 2093 | C:\CompanyDocs\DocumentationLogs\ScriptEditingLogs\Running\Script Editing.xlsx
- 2026-06-18 | 10:42 AM | CREATE-20260618-001 | Script Creation Log | Script: CustomHotelRevenueParser.cs | Hotel IDs: 12345 | C:\CompanyDocs\DocumentationLogs\ScriptCreationLogs\Running\Script Creation.xlsx

Historical index lines that point to daily TXT files remain unchanged. New
lines point only to the selected Running workbook. Running/history workbooks,
the one new index line, and sequence state are staged and committed as one
rollback-capable logical save. An abrupt process or operating-system crash can
still interrupt a multi-file commit before managed rollback runs; the app must
report manual-review paths rather than claim success in that boundary.

Changing the logs folder
------------------------

The dashboard shows the current documentation root folder.

Use Change Logs Folder to select a new root folder for future saves. The app
creates the expected folder structure in the selected location. Existing logs are
not moved when the folder changes.

Use Reset to Default Folder to return to the default DocumentationLogs location.

Settings priority
-----------------

The documentation root folder is resolved in this order:

1. user-settings.json
2. appsettings.json
3. DocumentationLogs default

Runtime folder changes are saved to user-settings.json beside the built app
instead of modifying appsettings.json. Relative paths are resolved from the
application base directory. Absolute paths are used as-is.

Privacy reminder
----------------

Do not enter guest names, emails, payment data, credentials, or full hotel files
into logs, findings, or QA Summary. Do not copy reservation-level PII or full
source rows. Use ticket IDs, hotel IDs, script names, and field/check-level
summaries instead.

How to build and run for development
------------------------------------

From the repository root, build the solution:

dotnet build DocumentationLoggingDashboard.sln

Run the complete custom regression harness after the Debug build:

dotnet run --project tests\DocumentationLoggingDashboard.GeometryTests\DocumentationLoggingDashboard.GeometryTests.csproj -c Debug --no-build --

Run the app from Visual Studio, or run the built executable for development
only from:

DocumentationLoggingDashboard/bin/Debug/net10.0-windows/DocumentationLoggingDashboard.exe

Development publishing
----------------------

The script below creates a development publish. It is not the reviewed V2
production-candidate package.

From the repository root, run:

powershell -ExecutionPolicy Bypass -File .\publish-windows.ps1

The published app is created under:

PublishedApp\win-x64

The publish must also contain THIRD-PARTY-NOTICES.txt. Published Quick QA must
be verified without relying on Excel or another external spreadsheet runtime.

The development executable can be launched from:

PublishedApp\win-x64\DocumentationLoggingDashboard.exe

For production-candidate use, follow the START-HERE.txt file in the separately
reviewed external folder named
DocumentationLoggingDashboard-V2-Production-<SHORTSHA>. Do not use the
repository, bin output, old V1 PublishedApp folder, or frozen pilot package as
the production working application folder. Release details are in
RELEASE_INSTRUCTIONS.txt.

The existing 3842298 external candidate predates the focused Arrival Month
correction described above. It is not corrected pilot evidence and must not be
used to resume the pilot. A replacement package requires completed verification,
independent review, and separate authorization. The initial focused suite and
representative PDF/save/index evidence passed, but reviewer-expanded and V1
executable reruns are currently blocked by Windows Smart App Control.
